using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace KeeviFixes.Runtime
{
    /// <summary>
    /// Accessors for the private <see cref="GoPointer"/> fields that keep a held big item's pose in the holder's space.
    /// </summary>
    internal static class GoPointerFields
    {
        /// <summary>Where the holder intends to keep the item's pivot.</summary>
        public static readonly AccessTools.FieldRef<GoPointer, Vector3> BigItemLocalPos = Find<Vector3>("bigItemLocalPos");

        /// <summary>Where the holder draws the item's pivot after pushing it out of colliders.</summary>
        public static readonly AccessTools.FieldRef<GoPointer, Vector3> DecolLocalPos = Find<Vector3>("decolLocalPos");

        /// <summary>The item's rotation before its held rotation offset is applied.</summary>
        public static readonly AccessTools.FieldRef<GoPointer, Quaternion> BigItemLocalRot = Find<Quaternion>("bigItemLocalRot");

        /// <summary>Whether the game still has every one of these fields.</summary>
        public static bool AreFound
        {
            get { return BigItemLocalPos != null && DecolLocalPos != null && BigItemLocalRot != null; }
        }

        /// <summary>
        /// Returns an accessor for a private <see cref="GoPointer"/> field, or null when the game no longer has a
        /// field of that name and type.
        /// </summary>
        private static AccessTools.FieldRef<GoPointer, T> Find<T>(string name)
        {
            FieldInfo field = AccessTools.Field(typeof(GoPointer), name);
            return field != null && field.FieldType == typeof(T) ? AccessTools.FieldRefAccess<GoPointer, T>(field) : null;
        }
    }
}
