using System;
using System.Reflection;
using HarmonyLib;
using KeeviFixes.Configuration;
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
        /// Skips these patches, with a warning, when the game lacks the pointer members they rely on.
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

        /// <summary>
        /// Retargets the pointer past nailed furniture after every other look patch; see <see
        /// cref="FurnitureReach.AfterLook"/>.
        /// </summary>
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
            try
            {
                FurnitureReach.AfterLook(
                    __instance,
                    ___heldItem,
                    ___raycastRay,
                    ___hit,
                    ref ___pointedAtButton,
                    ref ___currentLookDistance);
            }
            catch (Exception ex)
            {
                FixGuard.Stop(ModConfig.FixReachingBehindFurniture, ex);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("LateUpdate")]
        private static void BeforeClicks(
            GoPointer __instance,
            PickupableItem ___heldItem,
            ref GoPointerButton ___pointedAtButton)
        {
            try
            {
                FurnitureReach.BeforeClicks(__instance, ___heldItem, ref ___pointedAtButton);
            }
            catch (Exception ex)
            {
                FixGuard.Stop(ModConfig.FixReachingBehindFurniture, ex);
            }
        }

        private static bool HasField(string name, Type type)
        {
            FieldInfo field = AccessTools.Field(typeof(GoPointer), name);
            return field != null && field.FieldType == type;
        }
    }
}
