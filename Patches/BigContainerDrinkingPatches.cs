using HarmonyLib;
using KeeviFixes.Runtime;

namespace KeeviFixes.Patches
{
    /// <summary>
    /// Holds barrels as big items again once the player stops drinking from them.
    /// </summary>
    internal static class BigContainerDrinkingPatches
    {
        private const string UnsupportedWarning =
            "Could not find the GoPointer fields for held big items - the game may have updated. " +
            "Barrels will stay small items after drinking until dropped.";

        [HarmonyPatch(typeof(ShipItemBottle))]
        private static class BottlePatches
        {
            private static bool Prepare()
            {
                if (!BigContainerDrinking.IsSupported)
                    Plugin.Log.LogWarning(UnsupportedWarning);

                return BigContainerDrinking.IsSupported;
            }

            [HarmonyPrefix]
            [HarmonyPatch(nameof(ShipItemBottle.OnAltHeld))]
            private static void BeforeDrinkingStep(ShipItemBottle __instance, out BigContainerDrinking __state)
            {
                __state = BigContainerDrinking.CaptureBeforeDrinkingStep(__instance);
            }

            [HarmonyPostfix]
            [HarmonyPatch(nameof(ShipItemBottle.OnAltHeld))]
            private static void AfterDrinkingStep(ShipItemBottle __instance, BigContainerDrinking __state)
            {
                BigContainerDrinking.TrackIfTurnedSmall(__instance, __state);
            }

            [HarmonyPostfix]
            [HarmonyPatch(nameof(ShipItemBottle.ExtraLateUpdate))]
            private static void AfterLateUpdate(ShipItemBottle __instance)
            {
                BigContainerDrinking.RestoreWhenDrinkingEnds(__instance, __instance.IsDrinking());
            }
        }

        /// <summary>
        /// Eases a container back into its hold after drinking. Runs after the other postfixes on the holder's late
        /// update, so it starts from the pose they leave.
        /// </summary>
        [HarmonyPatch(typeof(GoPointer), "LateUpdate")]
        private static class HolderPatches
        {
            private static bool Prepare()
            {
                return BigContainerDrinking.IsSupported;
            }

            [HarmonyPostfix]
            [HarmonyPriority(Priority.Low)]
            private static void AfterHolderLateUpdate(GoPointer __instance)
            {
                BigContainerDrinking.EaseReturningItem(__instance);
            }
        }
    }
}
