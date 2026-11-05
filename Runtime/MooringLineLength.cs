using System.Collections.Generic;
using KeeviFixes.Configuration;
using UnityEngine;

namespace KeeviFixes.Runtime
{
    /// <summary>
    /// Keeps the length a mooring line was tied or set to when its boat wakes up, unless the boat lies farther away,
    /// and takes in the slack as the boat comes closer. Gives a line loaded as tied its saved length when it is tied
    /// back to the cleat under its saved end.
    /// </summary>
    internal static class MooringLineLength
    {
        private const float Tolerance = 0.01f;

        /// <summary>Longest length, in metres, a saved line length is accepted at.</summary>
        private const float LongestSavedLength = 50f;

        private static readonly Dictionary<PickupableBoatMooringRope, float> KeptLengths =
            new Dictionary<PickupableBoatMooringRope, float>();

        private static readonly Dictionary<PickupableBoatMooringRope, LoadedLine> LoadedLines =
            new Dictionary<PickupableBoatMooringRope, LoadedLine>();

        /// <summary>
        /// Remembers a line the game has just loaded as tied and left untied, with where its end was saved.
        /// </summary>
        public static void OnLoaded(SaveableObject saveable, SaveObjectData data)
        {
            if (data == null || !data.extraSetting)
                return;

            PickupableBoatMooringRope line = saveable.GetComponent<PickupableBoatMooringRope>();
            if (line != null && !line.IsMoored())
                LoadedLines[line] = new LoadedLine(saveable.sceneIndex, ToShiftingWorld(line.transform.position), data.extraValue);
        }

        /// <summary>
        /// Looks up the saved length of every line remembered while loading. Must run at the end of loading, before
        /// any of those lines is tied again.
        /// </summary>
        public static void AfterLoadGame()
        {
            Dictionary<int, float> savedLengths = MooringLineSave.IsSupported ? MooringLineSave.Read() : null;
            foreach (PickupableBoatMooringRope line in new List<PickupableBoatMooringRope>(LoadedLines.Keys))
            {
                LoadedLine loaded = LoadedLines[line];
                LoadedLines[line] = loaded.WithLength(SavedLength(loaded, savedLengths));
            }
        }

        /// <summary>
        /// Remembers the length of a line just tied. A line loaded as tied and tied back to the cleat under its saved
        /// end gets the length it was saved with, applied on its boat's next awake update; any other line keeps the
        /// length it was tied with.
        /// </summary>
        public static void OnTied(PickupableBoatMooringRope line)
        {
            SpringJoint spring = MooringLineFields.MooredSpring(line);
            if (spring == null)
                return;

            LoadedLine loaded;
            if (LoadedLines.TryGetValue(line, out loaded))
            {
                LoadedLines.Remove(line);
                if (loaded.IsUnder(ToShiftingWorld(spring.transform.position)))
                {
                    SetKeptLength(line, loaded.SavedLength);
                    MooringLineFields.IsLengthDue(line) = true;
                    return;
                }
            }

            SetKeptLength(line, spring.maxDistance);
        }

        /// <summary>
        /// Remembers the length the player just gave a tied line.
        /// </summary>
        public static void Remember(PickupableBoatMooringRope line)
        {
            SpringJoint spring = MooringLineFields.MooredSpring(line);
            if (spring != null)
                SetKeptLength(line, spring.maxDistance);
        }

        /// <summary>
        /// Gives a tied line whose boat has just woken up its kept length, or the boat's distance when that is longer.
        /// Must run before the line's own update.
        /// </summary>
        public static void OnBoatWoken(PickupableBoatMooringRope line, SpringJoint spring)
        {
            float distance = MooringLineFields.MeasureDistance(line);
            float kept = KeptLength(line);
            if (!FixGuard.IsOn(ModConfig.FixMooringLineLength) || kept <= 0f)
            {
                SetKeptLength(line, distance);
                return;
            }

            MooringLineFields.IsLengthDue(line) = false;
            MooringLineFields.SetLength(line, spring, Mathf.Max(kept, distance));
        }

        /// <summary>
        /// Shortens a tied line that is longer than its kept length as its boat comes closer, never below the kept
        /// length or the boat's distance.
        /// </summary>
        public static void TakeInSlack(PickupableBoatMooringRope line, SpringJoint spring)
        {
            float kept = KeptLength(line);
            if (!FixGuard.IsOn(ModConfig.FixMooringLineLength) || kept <= 0f || spring.maxDistance <= kept + Tolerance)
                return;

            float distance = MooringLineFields.MeasureDistance(line);
            if (distance < spring.maxDistance)
                MooringLineFields.SetLength(line, spring, Mathf.Max(kept, distance));
        }

        /// <summary>
        /// Returns the length kept for <paramref name="line"/>, or 0 when none is kept.
        /// </summary>
        public static float KeptLength(PickupableBoatMooringRope line)
        {
            float length;
            return KeptLengths.TryGetValue(line, out length) ? length : 0f;
        }

        private static void SetKeptLength(PickupableBoatMooringRope line, float length)
        {
            KeptLengths[line] = length;
        }

        /// <summary>
        /// Returns the length saved for a loaded line: from <paramref name="savedLengths"/> when the save has them,
        /// otherwise from the line's extra value; 0 when the save holds none or one above
        /// <see cref="LongestSavedLength"/>.
        /// </summary>
        private static float SavedLength(LoadedLine loaded, Dictionary<int, float> savedLengths)
        {
            float length;
            if (savedLengths == null)
                length = loaded.ExtraValueLength;
            else if (!savedLengths.TryGetValue(loaded.SceneIndex, out length))
                length = 0f;

            return length <= LongestSavedLength ? length : 0f;
        }

        private static Vector3 ToShiftingWorld(Vector3 worldPoint)
        {
            return Refs.shiftingWorld != null ? Refs.shiftingWorld.InverseTransformPoint(worldPoint) : worldPoint;
        }

        private struct LoadedLine
        {
            public readonly int SceneIndex;

            /// <summary>Holds where the line's end was loaded, in the space of the shifting world.</summary>
            public readonly Vector3 SavedEnd;

            /// <summary>Holds the length the save keeps in the line's extra value, or 0 when it keeps none.</summary>
            public readonly float ExtraValueLength;

            /// <summary>Holds the length to give the line when it is tied back, or 0 to measure it from its boat.</summary>
            public readonly float SavedLength;

            public LoadedLine(int sceneIndex, Vector3 savedEnd, float extraValueLength)
                : this(sceneIndex, savedEnd, extraValueLength, 0f)
            {
            }

            private LoadedLine(int sceneIndex, Vector3 savedEnd, float extraValueLength, float savedLength)
            {
                SceneIndex = sceneIndex;
                SavedEnd = savedEnd;
                ExtraValueLength = extraValueLength;
                SavedLength = savedLength;
            }

            public LoadedLine WithLength(float savedLength)
            {
                return new LoadedLine(SceneIndex, SavedEnd, ExtraValueLength, savedLength);
            }

            /// <summary>
            /// Returns whether <paramref name="cleat"/>, in the space of the shifting world, lies under the saved end
            /// seen from above, within <see cref="MooringRetie.MatchDistance"/>.
            /// </summary>
            public bool IsUnder(Vector3 cleat)
            {
                return new Vector2(cleat.x - SavedEnd.x, cleat.z - SavedEnd.z).magnitude <= MooringRetie.MatchDistance;
            }
        }
    }
}
