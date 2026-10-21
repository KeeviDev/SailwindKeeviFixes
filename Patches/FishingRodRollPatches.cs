using System;
using System.Reflection;
using HarmonyLib;
using KeeviFixes.Runtime;
using UnityEngine;

namespace KeeviFixes.Patches
{
    /// <summary>
    /// Keeps fishing rods that aren't held in place while a fish pulls; see <see cref="FishingRodRoll"/>.
    /// </summary>
    [HarmonyPatch(typeof(FishingRodFish))]
    internal static class FishingRodRollPatches
    {
        /// <summary>
        /// Skips these patches, with a warning, when the game lacks the fishing rod members they rely on.
        /// </summary>
        private static bool Prepare()
        {
            bool supported = FishingRodRoll.IsSupported
                && AccessTools.Method(typeof(FishingRodFish), "Awake") != null
                && AccessTools.Method(typeof(FishingRodFish), "Update") != null
                && HasField("rodRotator", typeof(Transform))
                && HasField("rod", typeof(ShipItemFishingRod))
                && HasField("bobber", typeof(Rigidbody))
                && HasField("initialRotatorRot", typeof(Quaternion));
            if (!supported)
            {
                Plugin.Log.LogWarning(
                    "Could not find the fishing rod members - the game may have updated. " +
                    "Fishing rods that aren't held may still turn out of place when a fish bites.");
            }

            return supported;
        }

        [HarmonyPostfix]
        [HarmonyPatch("Awake")]
        private static void AfterAwake(FishingRodFish __instance, Transform ___rodRotator, ShipItemFishingRod ___rod)
        {
            FishingRodRoll.OnAwake(__instance, ___rodRotator, ___rod);
        }

        [HarmonyPrefix]
        [HarmonyPatch("Update")]
        private static void BeforeUpdate(Transform ___rodRotator, out Quaternion __state)
        {
            __state = ___rodRotator.localRotation;
        }

        [HarmonyPostfix]
        [HarmonyPatch("Update")]
        private static void AfterUpdate(
            FishingRodFish __instance,
            Transform ___rodRotator,
            ShipItemFishingRod ___rod,
            Rigidbody ___bobber,
            Quaternion ___initialRotatorRot,
            Quaternion __state)
        {
            FishingRodRoll.AfterUpdate(
                __instance, ___rodRotator, ___rod, ___bobber.transform, ___initialRotatorRot, __state);
        }

        private static bool HasField(string name, Type type)
        {
            FieldInfo field = AccessTools.Field(typeof(FishingRodFish), name);
            return field != null && field.FieldType == type;
        }
    }
}
