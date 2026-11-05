using System.Reflection;
using HarmonyLib;

namespace KeeviFixes.Runtime
{
    /// <summary>Reads and clears the wall attachment state the game keeps on items.</summary>
    internal static class WallAttachment
    {
        private static readonly AccessTools.FieldRef<ShipItem, bool> InRangeOfWall = FindInRangeOfWall();

        /// <summary>Gets whether the game has the item member this class reads.</summary>
        public static bool IsSupported
        {
            get { return InRangeOfWall != null; }
        }

        /// <summary>Returns whether <paramref name="item"/> has a wall spot to attach to in front of it.</summary>
        public static bool IsInRangeOfWall(ShipItem item)
        {
            return InRangeOfWall(item);
        }

        /// <summary>Marks <paramref name="item"/> as having no wall spot to attach to in front of it.</summary>
        public static void ClearWallSpot(ShipItem item)
        {
            InRangeOfWall(item) = false;
        }

        private static AccessTools.FieldRef<ShipItem, bool> FindInRangeOfWall()
        {
            FieldInfo field = AccessTools.Field(typeof(ShipItem), "inRangeOfWall");
            return field != null && field.FieldType == typeof(bool)
                ? AccessTools.FieldRefAccess<ShipItem, bool>(field)
                : null;
        }
    }
}
