using KeeviFixes.Configuration;
using UnityEngine;

namespace KeeviFixes.Runtime
{
    /// <summary>
    /// Keeps the length a mooring line was tied or set to. The game sets a tied line's length to the boat's current
    /// distance whenever the boat wakes up, after being far away, paused or loaded, so a boat pressed against the dock at
    /// that moment ends up tied tighter. A waking boat instead keeps the line's length, or the boat's distance when that
    /// is longer so the spring never yanks the boat, and the line then takes in its slack as the boat comes closer. The
    /// length is saved with the line, in a value the game does not use for lines.
    /// </summary>
    internal static class MooringLineLength
    {
        private const float Tolerance = 0.01f;

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
        /// <param name="savedLength">The length saved with the line; 0 for a save made without this fix, whose line
        /// is then measured from the boat's distance as the game does.</param>
        public static void RestoreAfterLoading(PickupableBoatMooringRope line, float savedLength)
        {
            SetKeptLength(line, savedLength);
            MooringLineFields.IsLengthDue(line) = true;
        }

        /// <summary>
        /// Applies the kept length when the line's boat has woken up, and takes in slack above it. Must run before the
        /// line's own update, which would otherwise measure the length again.
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
                ApplyOnWaking(line, spring);
            else if (ModConfig.KeepMooringLineLength.Value)
                TakeInSlack(line, spring);
        }

        private static void ApplyOnWaking(PickupableBoatMooringRope line, SpringJoint spring)
        {
            float distance = MooringLineFields.MeasureDistance(line);
            float kept = KeptLength(line);
            if (!ModConfig.KeepMooringLineLength.Value || kept <= 0f)
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

        private static float KeptLength(PickupableBoatMooringRope line)
        {
            SaveableObject saveable = line.GetComponent<SaveableObject>();
            return saveable != null ? saveable.extraValue : 0f;
        }

        private static void SetKeptLength(PickupableBoatMooringRope line, float length)
        {
            SaveableObject saveable = line.GetComponent<SaveableObject>();
            if (saveable != null)
                saveable.extraValue = length;
        }
    }
}
