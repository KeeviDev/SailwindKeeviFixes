using System.Reflection;
using HarmonyLib;
using KeeviFixes.Configuration;

namespace KeeviFixes.Patches
{
    /// <summary>
    /// Lets the charting kit be placed on furniture. Every other small item allows its held use on furniture that
    /// takes placed items, but the kit overrides that check without it, so furniture can't become its target. Looking
    /// at a table then kept whatever was targeted before, so a click next to a map opened the map view again instead
    /// of putting the kit down.
    /// </summary>
    [HarmonyPatch]
    internal static class FurniturePlacingPatch
    {
        private static readonly MethodInfo Target =
            AccessTools.DeclaredMethod(typeof(ShipItemInkSet), nameof(ShipItem.AllowOnItemClick));

        /// <summary>
        /// Skips this patch, with a warning, when the game no longer has the override the fix relies on.
        /// </summary>
        private static bool Prepare()
        {
            if (Target == null)
            {
                Plugin.Log.LogWarning(
                    "Could not find the charting kit click check - the game may have updated. " +
                    "The charting kit may still not go on furniture.");
            }

            return Target != null;
        }

        private static MethodBase TargetMethod()
        {
            return Target;
        }

        /// <summary>
        /// The check every other small item makes: a small item goes on furniture that takes placed items.
        /// </summary>
        private static void Postfix(ShipItem __instance, GoPointerButton lookedAtButton, ref bool __result)
        {
            if (!__result && !__instance.big && lookedAtButton != null && lookedAtButton.allowPlacingItems
                && ModConfig.FixChartingKitPlacement.Value)
                __result = true;
        }
    }
}
