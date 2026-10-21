using HarmonyLib;
using KeeviFixes.Configuration;
using UnityEngine;

namespace KeeviFixes.Patches
{
    /// <summary>Skips the update of a grabbed bilge pump while the game is paused.</summary>
    [HarmonyPatch(typeof(BilgePump), "Update")]
    internal static class BilgePumpPausePatch
    {
        /// <summary>Skips this patch, with a warning, when the game lacks the pump update it relies on.</summary>
        private static bool Prepare()
        {
            bool supported = AccessTools.Method(typeof(BilgePump), "Update") != null;
            if (!supported)
            {
                Plugin.Log.LogWarning(
                    "Could not find the bilge pump update - the game may have updated. " +
                    "Pausing while holding a pump handle may still knock it off its axis.");
            }

            return supported;
        }

        private static bool Prefix(BilgePump __instance)
        {
            return !(Time.deltaTime == 0f
                && __instance.IsStickyClicked()
                && ModConfig.FixBilgePumpHandle.Value);
        }
    }
}
