using HarmonyLib;
using KeeviFixes.Configuration;
using KeeviFixes.Runtime;

namespace KeeviFixes.Patches
{
    /// <summary>
    /// Lets a held item that attaches to walls fly or fall when the throw key lets go of it, instead of going on the
    /// wall spot it previews, while <see cref="ModConfig.FixThrowing"/> is on.
    /// </summary>
    [HarmonyPatch]
    internal static class WallThrowPatch
    {
        /// <summary>Skips this patch, with a warning, when the game lacks the members it relies on.</summary>
        private static bool Prepare()
        {
            bool supported = WallAttachment.IsSupported
                && AccessTools.Method(typeof(ShipItem), nameof(ShipItem.OnDrop)) != null;
            if (!supported)
            {
                Plugin.Log.LogWarning(
                    "Could not find the wall attachment members - the game may have updated. " +
                    "Items that attach to walls may still be hung on a wall when you throw them near one.");
            }

            return supported;
        }

        /// <summary>Clears the wall spot of a held item that attaches to walls when the throw key drops it.</summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ShipItem), nameof(ShipItem.OnDrop))]
        [HarmonyPriority(Priority.First)]
        private static void BeforeDrop(ShipItem __instance)
        {
            if (__instance.wallAttachment
                && __instance.held != null
                && WallAttachment.IsInRangeOfWall(__instance)
                && GameInput.GetKeyUp(InputName.Throw)
                && FixGuard.IsOn(ModConfig.FixThrowing))
                WallAttachment.ClearWallSpot(__instance);
        }
    }
}
