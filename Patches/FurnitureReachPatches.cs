using System;
using System.Reflection;
using HarmonyLib;
using KeeviFixes.Runtime;
using UnityEngine;

namespace KeeviFixes.Patches
{
    /// <summary>
    /// Lets an empty-handed pointer pick up small items behind or under nailed furniture; see
    /// <see cref="FurnitureReach"/>.
    /// </summary>
    [HarmonyPatch(typeof(GoPointer))]
    internal static class FurnitureReachPatches
    {
        /// <summary>
        /// Skips these patches, with a warning, when the game no longer has the pointer members the fix relies on.
        /// </summary>
        private static bool Prepare()
        {
            bool supported = AccessTools.Method(typeof(GoPointer), "DoRaycast") != null
                && AccessTools.Method(typeof(GoPointer), "LateUpdate") != null
                && AccessTools.Method(typeof(GoPointer), nameof(GoPointer.MainButtonDown)) != null
                && HasField("heldItem", typeof(PickupableItem))
                && HasField("raycastRay", typeof(Ray))
                && HasField("hit", typeof(RaycastHit))
                && HasField("pointedAtButton", typeof(GoPointerButton))
                && HasField("currentLookDistance", typeof(float));
            if (!supported)
            {
                Plugin.Log.LogWarning(
                    "Could not find the GoPointer look members - the game may have updated. " +
                    "Small items behind nailed furniture will stay out of reach.");
            }

            return supported;
        }

        /// <summary>Runs after every other mod's look fixes, so it sees the target they chose.</summary>
        [HarmonyPostfix]
        [HarmonyPatch("DoRaycast")]
        [HarmonyPriority(Priority.Last)]
        [HarmonyAfter("com.dizzy.sailwind.fixes", "com.dizzy.sailwind.firewoodbundle")]
        private static void AfterLook(
            GoPointer __instance,
            PickupableItem ___heldItem,
            Ray ___raycastRay,
            RaycastHit ___hit,
            ref GoPointerButton ___pointedAtButton,
            ref float ___currentLookDistance)
        {
            FurnitureReach.AfterLook(
                __instance,
                ___heldItem,
                ___raycastRay,
                ___hit,
                ref ___pointedAtButton,
                ref ___currentLookDistance);
        }

        [HarmonyPrefix]
        [HarmonyPatch("LateUpdate")]
        private static void BeforeClicks(
            GoPointer __instance,
            PickupableItem ___heldItem,
            ref GoPointerButton ___pointedAtButton)
        {
            FurnitureReach.BeforeClicks(__instance, ___heldItem, ref ___pointedAtButton);
        }

        private static bool HasField(string name, Type type)
        {
            FieldInfo field = AccessTools.Field(typeof(GoPointer), name);
            return field != null && field.FieldType == type;
        }
    }
}
