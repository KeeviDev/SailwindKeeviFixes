using System;
using HarmonyLib;
using KeeviFixes.Configuration;
using KeeviFixes.Runtime;

namespace KeeviFixes.Patches
{
    /// <summary>
    /// Shows the look text of a big item at the center of its own mesh and records which item the hold text is shown
    /// for, while <see cref="ModConfig.FixRotationCenter"/> is on.
    /// </summary>
    [HarmonyPatch]
    internal static class BigItemHintPatches
    {
        /// <summary>
        /// Skips these patches, with a warning, when the game lacks the look text members they rely on.
        /// </summary>
        private static bool Prepare()
        {
            bool supported = AccessTools.Method(typeof(LookUI), nameof(LookUI.ShowLookText)) != null
                && AccessTools.Method(typeof(LookUI), nameof(LookUI.ShowHoldText)) != null
                && AccessTools.Field(typeof(GoPointer), nameof(GoPointer.lookUI)) != null;
            if (!supported)
            {
                Plugin.Log.LogWarning(
                    "Could not find the look text members - the game may have updated. " +
                    "The name and controls hint of big items may still show at their base or edge.");
            }

            return supported;
        }

        /// <summary>Moves the look text of a big item to the center of its own mesh.</summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(LookUI), nameof(LookUI.ShowLookText))]
        private static void AfterShowLookText(LookUI __instance, GoPointerButton button)
        {
            try
            {
                BigItemHint.PlaceLookText(__instance, button);
            }
            catch (Exception ex)
            {
                FixGuard.Stop(ModConfig.FixRotationCenter, ex);
            }
        }

        /// <summary>Records the item the hold text is shown for this frame.</summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(LookUI), nameof(LookUI.ShowHoldText))]
        private static void AfterShowHoldText(LookUI __instance, PickupableItem item)
        {
            BigItemHint.OnHoldTextShown(__instance, item);
        }
    }
}
