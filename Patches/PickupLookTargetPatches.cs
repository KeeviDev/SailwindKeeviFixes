using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using KeeviFixes.Configuration;

namespace KeeviFixes.Patches
{
    /// <summary>Clears the pointer's target after it picks an item up.</summary>
    [HarmonyPatch(typeof(GoPointer))]
    internal static class PickupLookTargetPatches
    {
        /// <summary>Pointers that picked an item up since their last late update.</summary>
        private static readonly HashSet<GoPointer> PickedUpBy = new HashSet<GoPointer>();

        /// <summary>
        /// Skips these patches, with a warning, when the game lacks the pointer members they rely on.
        /// </summary>
        private static bool Prepare()
        {
            bool supported = AccessTools.Method(typeof(GoPointer), "PickUpItem") != null
                && AccessTools.Method(typeof(GoPointer), "LateUpdate") != null
                && HasField("pointedAtButton", typeof(GoPointerButton))
                && HasField("currentLookDistance", typeof(float));
            if (!supported)
            {
                Plugin.Log.LogWarning(
                    "Could not find the GoPointer pickup or look target - the game may have updated. " +
                    "Items picked up in front of others may still not drop until you look away.");
            }

            return supported;
        }

        [HarmonyPostfix]
        [HarmonyPatch("PickUpItem")]
        private static void AfterPickUp(GoPointer __instance)
        {
            if (FixGuard.IsOn(ModConfig.FixDroppingAfterPickup))
                PickedUpBy.Add(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch("LateUpdate")]
        private static void AfterLateUpdate(
            GoPointer __instance,
            ref GoPointerButton ___pointedAtButton,
            ref float ___currentLookDistance)
        {
            if (PickedUpBy.Count == 0 || !PickedUpBy.Remove(__instance))
                return;

            ___pointedAtButton = null;
            ___currentLookDistance = 0f;
        }

        private static bool HasField(string name, Type type)
        {
            FieldInfo field = AccessTools.Field(typeof(GoPointer), name);
            return field != null && field.FieldType == type;
        }
    }
}
