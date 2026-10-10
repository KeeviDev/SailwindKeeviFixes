using System.Collections.Generic;
using System.Linq;
using KeeviFixes.Configuration;
using UnityEngine;

namespace KeeviFixes.Runtime
{
    /// <summary>
    /// Keeps moored boats tied. Loading puts a tied line's end back where it was saved and ties it only if it touches a
    /// switched-on cleat; a far port's cleats are switched off, and the world shifts right after loading, so those
    /// lines stay loose. They are tied here by position instead: islands only move up and down, so the cleat under a
    /// line's end is its cleat. Separately, a cleat switched on while its boat is switched off ends up with a spring
    /// attached to nothing, so a boat's springs are attached again whenever the boat is switched on.
    /// </summary>
    internal static class MooringRetie
    {
        /// <summary>How far, seen from above, a loaded line's end may be from its cleat.</summary>
        private const float MatchDistance = 0.5f;

        private static readonly List<LoadedLine> LoadedLines = new List<LoadedLine>();

        private static bool IsEnabled
        {
            get { return ModConfig.PreventMooredBoatsComingLoose.Value && MooringFix.IsAvailable; }
        }

        /// <summary>
        /// Remembers a line the game has just loaded as tied and left untied.
        /// </summary>
        public static void OnLoaded(SaveableObject saveable, SaveObjectData data)
        {
            if (data == null || !data.extraSetting || saveable.transform.parent != null)
                return;

            PickupableBoatMooringRope line = saveable.GetComponent<PickupableBoatMooringRope>();
            if (line != null && !line.IsMoored() && IsEnabled)
                LoadedLines.Add(new LoadedLine(line, data.extraValue));
        }

        /// <summary>
        /// Ties every line remembered while loading to the cleat under its end, or returns it aboard when there is
        /// none. Must run at the end of loading, after every boat has untied its own lines and before the world shifts.
        /// </summary>
        public static void AfterLoadGame()
        {
            try
            {
                if (LoadedLines.Count == 0 || !IsEnabled)
                    return;

                GPButtonDockMooring[] cleats = Resources.FindObjectsOfTypeAll<GPButtonDockMooring>()
                    .Where(cleat => cleat != null && cleat.gameObject.scene.IsValid())
                    .ToArray();
                int tied = 0;
                foreach (LoadedLine line in LoadedLines)
                {
                    if (Retie(line, cleats))
                        tied++;
                }

                Plugin.Log.LogInfo(string.Format("Tied {0} of {1} loaded mooring lines back to their cleats.", tied, LoadedLines.Count));
            }
            finally
            {
                LoadedLines.Clear();
            }
        }

        /// <summary>
        /// Makes sure a boat with a line just tied attaches its springs again whenever it is switched on.
        /// </summary>
        public static void OnTied(PickupableBoatMooringRope line)
        {
            Rigidbody boat = line.GetBoatRigidbody();
            BoatMooringRopes boatLines = boat != null ? boat.GetComponent<BoatMooringRopes>() : null;
            if (boatLines != null && MooringFix.IsAvailable)
                MooredBoat.AttachTo(boatLines);
        }

        /// <summary>
        /// Attaches the spring of each of the boat's tied lines to the boat again.
        /// </summary>
        public static void AttachSprings(BoatMooringRopes boatLines)
        {
            if (boatLines.ropes == null || !IsEnabled)
                return;

            Rigidbody boat = boatLines.GetComponent<Rigidbody>();
            foreach (PickupableBoatMooringRope line in boatLines.ropes)
            {
                SpringJoint spring = line != null ? MooringLineFields.MooredSpring(line) : null;
                if (spring == null || spring.connectedBody != boat)
                    continue;

                Vector3 anchor = spring.connectedAnchor;
                spring.connectedBody = null;
                spring.connectedBody = boat;
                spring.connectedAnchor = anchor;
            }
        }

        private static bool Retie(LoadedLine loaded, GPButtonDockMooring[] cleats)
        {
            PickupableBoatMooringRope line = loaded.Line;
            if (line == null || line.IsMoored())
                return false;

            GPButtonDockMooring cleat = FindCleatUnder(line, cleats);
            if (cleat == null)
            {
                ReturnAboard(line);
                return false;
            }

            line.MoorTo(cleat);
            MooringLineLength.RestoreAfterLoading(line, loaded.SavedLength);
            return true;
        }

        /// <summary>
        /// Returns the free cleat nearest to the line's end seen from above, within <see cref="MatchDistance"/>, or
        /// null when there is none. Cleats on the line's own boat are skipped.
        /// </summary>
        private static GPButtonDockMooring FindCleatUnder(PickupableBoatMooringRope line, GPButtonDockMooring[] cleats)
        {
            Vector3 end = line.transform.position;
            Transform boat = line.GetBoatRigidbody().transform;
            GPButtonDockMooring nearest = null;
            float nearestDistance = MatchDistance;
            foreach (GPButtonDockMooring cleat in cleats)
            {
                float distance = new Vector2(cleat.transform.position.x - end.x, cleat.transform.position.z - end.z).magnitude;
                if (distance > nearestDistance || cleat.transform.IsChildOf(boat) || !IsFree(cleat))
                    continue;

                nearest = cleat;
                nearestDistance = distance;
            }

            return nearest;
        }

        private static bool IsFree(GPButtonDockMooring cleat)
        {
            if (cleat.spring == null)
                cleat.spring = cleat.GetComponent<SpringJoint>();
            return cleat.spring != null && cleat.spring.connectedBody == null;
        }

        private static void ReturnAboard(PickupableBoatMooringRope line)
        {
            line.transform.parent = MooringLineFields.HomeParent(line);
            line.ResetRopePos();
            Plugin.Log.LogWarning(string.Format(
                "Found no free cleat under the loaded mooring line {0} of {1}, so it goes back aboard.",
                line.name,
                line.GetBoatRigidbody().name));
        }

        private struct LoadedLine
        {
            public readonly PickupableBoatMooringRope Line;

            /// <summary>The length saved with the line, or 0 for a save made without this fix.</summary>
            public readonly float SavedLength;

            public LoadedLine(PickupableBoatMooringRope line, float savedLength)
            {
                Line = line;
                SavedLength = savedLength;
            }
        }
    }
}
