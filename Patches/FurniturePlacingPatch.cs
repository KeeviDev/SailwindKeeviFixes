using System.Reflection;
using HarmonyLib;
using KeeviFixes.Configuration;

namespace KeeviFixes.Patches
{
    /// <summary>Lets the charting kit be placed on furniture.</summary>
    [HarmonyPatch]
    internal static class FurniturePlacingPatch
    {
        private static readonly MethodInfo Target =
            AccessTools.DeclaredMethod(typeof(ShipItemInkSet), nameof(ShipItem.AllowOnItemClick));

        /// <summary>Skips this patch, with a warning, when the game lacks the override it relies on.</summary>
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

        /// <summary>Allows a small item's held use on a button that takes placed items.</summary>
        private static void Postfix(ShipItem __instance, GoPointerButton lookedAtButton, ref bool __result)
        {
            if (!__result && !__instance.big && lookedAtButton != null && lookedAtButton.allowPlacingItems
                && FixGuard.IsOn(ModConfig.FixChartingKitPlacement))
                __result = true;
        }
    }
}
