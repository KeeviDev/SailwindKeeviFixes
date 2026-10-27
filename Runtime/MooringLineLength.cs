using System.Collections.Generic;
using KeeviFixes.Configuration;
using UnityEngine;

namespace KeeviFixes.Runtime
{
    /// <summary>
    /// Keeps the length a mooring line was tied or set to when its boat wakes up, unless the boat lies farther away,
    /// and takes in the slack as the boat comes closer.
    /// </summary>
    internal static class MooringLineLength
    {
        private const float Tolerance = 0.01f;

        /// <summary>Longest length, in metres, a saved line length is accepted at.</summary>
        private const float LongestSavedLength = 50f;

        private static readonly Dictionary<PickupableBoatMooringRope, float> KeptLengths =
            new Dictionary<PickupableBoatMooringRope, float>();

        /// <summary>
        /// Remembers the length the game or the player just gave a tied line.
        /// </summary>
        public static void Remember(PickupableBoatMooringRope line)
        {
            SpringJoint spring = MooringLineFields.MooredSpring(line);
            if (spring != null && MooringFix.IsAvailable)
                SetKeptLength(line, spring.maxDistance);
        }

        /// <summary>
        /// Gives a line tied again after loading the length it was saved with, applied on its boat's first awake update.
        /// </summary>
        /// <param name="savedLength">The length saved with the line. With 0, as in a save made without this fix, or a
        /// length above <see cref="LongestSavedLength"/>, the line is measured from the boat's distance as the game
        /// does.</param>
        public static void RestoreAfterLoading(PickupableBoatMooringRope line, float savedLength)
        {
            SetKeptLength(line, savedLength <= LongestSavedLength ? savedLength : 0f);
            MooringLineFields.IsLengthDue(line) = true;
        }

        /// <summary>
        /// Puts the line's end back on its cleat and applies the kept length when the line's boat has woken up, and
        /// takes in slack above the kept length. Must run before the line's own update.
        /// </summary>
        public static void BeforeLineUpdate(PickupableBoatMooringRope line)
        {
            SpringJoint spring = MooringLineFields.MooredSpring(line);
            if (spring == null || !MooringFix.IsAvailable)
                return;

            Rigidbody boat = line.GetBoatRigidbody();
            if (boat == null || boat.isKinematic)
                return;

            if (MooringLineFields.IsLengthDue(line))
            {
                MooringRetie.PutEndOnCleat(line, spring);
                ApplyOnWaking(line, spring);
            }
            else if (ModConfig.FixMooringLineLength.Value)
                TakeInSlack(line, spring);
        }

        private static void ApplyOnWaking(PickupableBoatMooringRope line, SpringJoint spring)
        {
            float distance = MooringLineFields.MeasureDistance(line);
            float kept = KeptLength(line);
            if (!ModConfig.FixMooringLineLength.Value || kept <= 0f)
            {
                SetKeptLength(line, distance);
                return;
            }

            MooringLineFields.IsLengthDue(line) = false;
            MooringLineFields.SetLength(line, spring, Mathf.Max(kept, distance));
        }

        private static void TakeInSlack(PickupableBoatMooringRope line, SpringJoint spring)
        {
            float kept = KeptLength(line);
            if (kept <= 0f || spring.maxDistance <= kept + Tolerance)
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
    }
}
