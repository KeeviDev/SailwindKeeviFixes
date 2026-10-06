using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using KeeviFixes.Configuration;

namespace KeeviFixes.Patches
{
    /// <summary>
    /// Clears the pointer's target after it picks an item up. The game drops or throws a held item only while nothing
    /// is targeted, and while an item is held its raycast keeps the previous target whenever it hits an item the held
    /// one can't be used on. An item picked up in front of another stayed the target itself, and one taken from the
    /// inventory left the target the player looked at before opening it, until the player looked away.
    /// </summary>
    [HarmonyPatch(typeof(GoPointer))]
    internal static class PickupLookTargetPatches
    {
        /// <summary>
        /// Pointers that picked an item up since their last late update. The game can still use the target right
        /// after a pickup in the same frame, so it's cleared only at the end of the pointer's late update.
        /// </summary>
        private static readonly HashSet<GoPointer> PickedUpBy = new HashSet<GoPointer>();

        /// <summary>
        /// Skips these patches, with a warning, when the game no longer has the pointer members the fix relies on.
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
            if (ModConfig.DropItemsPickedUpInFrontOfOthers.Value)
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
