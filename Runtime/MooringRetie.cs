using System.Collections.Generic;
using System.Linq;
using KeeviFixes.Configuration;
using UnityEngine;

namespace KeeviFixes.Runtime
{
    /// <summary>
    /// Ties loaded mooring lines to the cleats under their ends, and attaches a boat's springs again whenever the boat
    /// is switched on.
    /// </summary>
    internal static class MooringRetie
    {
        /// <summary>Distance, in metres, seen from above, a loaded line's end may be from its cleat.</summary>
        private const float MatchDistance = 0.5f;

        private static readonly List<LoadedLine> LoadedLines = new List<LoadedLine>();

        private static bool IsEnabled
        {
            get { return ModConfig.FixMooredBoatsComingLoose.Value && MooringFix.IsAvailable; }
        }

        /// <summary>
        /// Remembers a line the game has just loaded as tied and left untied.
        /// </summary>
        public static void OnLoaded(SaveableObject saveable, SaveObjectData data)
        {
            if (data == null || !data.extraSetting)
                return;

            PickupableBoatMooringRope line = saveable.GetComponent<PickupableBoatMooringRope>();
            if (line != null && IsEnabled && IsLeftLoose(line))
                LoadedLines.Add(new LoadedLine(line, saveable.sceneIndex, data.extraValue));
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
                Dictionary<int, float> savedLengths = MooringLineSave.IsSupported ? MooringLineSave.Read() : null;
                int tied = 0;
                foreach (LoadedLine line in LoadedLines)
                {
                    if (Retie(line, cleats, savedLengths))
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
        /// Moves the end of a tied line onto the cleat it is tied to.
        /// </summary>
        public static void PutEndOnCleat(PickupableBoatMooringRope line, SpringJoint spring)
        {
            if (!IsEnabled)
                return;

            Vector3 cleat = spring.transform.position;
            if (line.transform.position != cleat)
                line.transform.position = cleat;
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

        /// <summary>Returns whether the line is off its place aboard and tied to no cleat.</summary>
        private static bool IsLeftLoose(PickupableBoatMooringRope line)
        {
            return !line.IsMoored() && line.transform.parent != MooringLineFields.HomeParent(line);
        }

        private static bool Retie(LoadedLine loaded, GPButtonDockMooring[] cleats, Dictionary<int, float> savedLengths)
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
            MooringLineLength.RestoreAfterLoading(line, SavedLength(loaded, savedLengths));
            return true;
        }

        /// <summary>
        /// Returns the length saved for the loaded line: from <paramref name="savedLengths"/> when the save has them,
        /// otherwise from the line's extra value; 0 when the save holds none.
        /// </summary>
        private static float SavedLength(LoadedLine loaded, Dictionary<int, float> savedLengths)
        {
            if (savedLengths == null)
                return loaded.ExtraValueLength;

            float length;
            return savedLengths.TryGetValue(loaded.SceneIndex, out length) ? length : 0f;
        }

        /// <summary>
        /// Returns the free cleat off the line's own boat nearest to the line's end seen from above, within <see
        /// cref="MatchDistance"/>, or null when there is none.
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

            /// <summary>The line's scene index in the save.</summary>
            public readonly int SceneIndex;

            /// <summary>The length the save holds in the line's extra value, or 0 when it holds none.</summary>
            public readonly float ExtraValueLength;

            public LoadedLine(PickupableBoatMooringRope line, int sceneIndex, float extraValueLength)
            {
                Line = line;
                SceneIndex = sceneIndex;
                ExtraValueLength = extraValueLength;
            }
        }
    }
}
