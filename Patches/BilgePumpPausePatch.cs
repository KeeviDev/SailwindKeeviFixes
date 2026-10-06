using HarmonyLib;
using KeeviFixes.Configuration;
using UnityEngine;

namespace KeeviFixes.Patches
{
    /// <summary>
    /// Skips the update of a grabbed bilge pump while the game is paused. When winches are turned with keys, the pump
    /// divides the key input by the frame time, which is zero during a pause, and the resulting invalid turn knocks the
    /// handle off its axis. Everything else the update does scales with the frame time, so a paused frame has nothing
    /// else to skip.
    /// </summary>
    [HarmonyPatch(typeof(BilgePump), "Update")]
    internal static class BilgePumpPausePatch
    {
        /// <summary>
        /// Skips this patch, with a warning, when the game no longer has the pump update the fix relies on.
        /// </summary>
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
                && ModConfig.PreventBilgePumpHandleBreaking.Value);
        }
    }
}
