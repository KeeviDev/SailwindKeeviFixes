using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace KeeviFixes.Runtime
{
    /// <summary>
    /// Accessors for the private <see cref="PickupableBoatMooringRope"/> members that hold a tied line's spring and
    /// length.
    /// </summary>
    internal static class MooringLineFields
    {
        /// <summary>The cleat's spring the line is tied with, or null while it is untied.</summary>
        public static readonly AccessTools.FieldRef<PickupableBoatMooringRope, SpringJoint> MooredSpring =
            Find<SpringJoint>("mooredToSpring");

        /// <summary>The line's parent aboard its boat.</summary>
        public static readonly AccessTools.FieldRef<PickupableBoatMooringRope, Transform> HomeParent =
            Find<Transform>("initialParent");

        /// <summary>
        /// Whether the line's boat was asleep when the line last updated; the line measures its length again on the
        /// boat's first awake update while this is set.
        /// </summary>
        public static readonly AccessTools.FieldRef<PickupableBoatMooringRope, bool> IsLengthDue =
            Find<bool>("wasKinematic");

        private static readonly Func<PickupableBoatMooringRope, float> CurrentDistanceSquared =
            FindDistanceMeasure();

        /// <summary>Whether the game still has every one of these members.</summary>
        public static bool AreFound
        {
            get { return MooredSpring != null && HomeParent != null && IsLengthDue != null && CurrentDistanceSquared != null; }
        }

        /// <summary>
        /// Returns how far the tied line's end is from its place aboard, the distance the game sets a line's length
        /// to when it measures it.
        /// </summary>
        public static float MeasureDistance(PickupableBoatMooringRope line)
        {
            return Mathf.Sqrt(CurrentDistanceSquared(line));
        }

        /// <summary>
        /// Sets the length of a tied line, for its spring and for the rope the game draws.
        /// </summary>
        public static void SetLength(PickupableBoatMooringRope line, SpringJoint spring, float length)
        {
            spring.maxDistance = length;
            line.currentRopeLengthSquared = length * length;
        }

        /// <summary>
        /// Returns an accessor for a private <see cref="PickupableBoatMooringRope"/> field, or null when the game no
        /// longer has a field of that name and type.
        /// </summary>
        private static AccessTools.FieldRef<PickupableBoatMooringRope, T> Find<T>(string name)
        {
            FieldInfo field = AccessTools.Field(typeof(PickupableBoatMooringRope), name);
            return field != null && field.FieldType == typeof(T)
                ? AccessTools.FieldRefAccess<PickupableBoatMooringRope, T>(field)
                : null;
        }

        private static Func<PickupableBoatMooringRope, float> FindDistanceMeasure()
        {
            MethodInfo method = AccessTools.Method(typeof(PickupableBoatMooringRope), "GetCurrentDistanceSquared");
            return method != null && !method.IsStatic && method.ReturnType == typeof(float) && method.GetParameters().Length == 0
                ? (Func<PickupableBoatMooringRope, float>)Delegate.CreateDelegate(typeof(Func<PickupableBoatMooringRope, float>), method)
                : null;
        }
    }
}
