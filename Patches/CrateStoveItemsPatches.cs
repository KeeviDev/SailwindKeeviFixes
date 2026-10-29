using System;
using HarmonyLib;
using KeeviFixes.Configuration;
using UnityEngine;

namespace KeeviFixes.Patches
{
    /// <summary>
    /// Keeps stoves and smokers from taking food and firewood stored in a crate, while
    /// <see cref="ModConfig.FixStovesTakingCrateItems"/> is on.
    /// </summary>
    [HarmonyPatch]
    internal static class CrateStoveItemsPatches
    {
        /// <summary>Skips these patches, with a warning, when the game lacks the stove members they rely on.</summary>
        private static bool Prepare()
        {
            bool supported =
                AccessTools.Method(typeof(StoveCookTrigger), nameof(StoveCookTrigger.OnTriggerEnter)) != null
                && AccessTools.Method(typeof(StoveFuelTrigger), nameof(StoveFuelTrigger.InsertFuel)) != null
                && AccessTools.Field(typeof(SaveablePrefab), nameof(SaveablePrefab.currentCrateId)) != null;
            if (!supported)
            {
                Plugin.Log.LogWarning(
                    "Could not find the stove cook and fuel triggers - the game may have updated. " +
                    "Stoves may still take food and firewood from an open crate.");
            }

            return supported;
        }

        /// <summary>Skips the game's cooking of food stored in a crate.</summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(StoveCookTrigger), nameof(StoveCookTrigger.OnTriggerEnter))]
        private static bool BeforeCookTriggerEnter(StoveCookTrigger __instance, Collider other)
        {
            if (__instance.currentFood != null)
                return true;

            try
            {
                return !IsStored(other.GetComponent<SaveablePrefab>());
            }
            catch (Exception ex)
            {
                FixGuard.Stop(ModConfig.FixStovesTakingCrateItems, ex);
                return true;
            }
        }

        /// <summary>Skips the game's fuelling with firewood stored in a crate.</summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(StoveFuelTrigger), nameof(StoveFuelTrigger.InsertFuel))]
        private static bool BeforeInsertFuel(ShipItem item)
        {
            try
            {
                return item == null || !IsStored(item.GetComponent<SaveablePrefab>());
            }
            catch (Exception ex)
            {
                FixGuard.Stop(ModConfig.FixStovesTakingCrateItems, ex);
                return true;
            }
        }

        private static bool IsStored(SaveablePrefab saveable)
        {
            return saveable != null && saveable.currentCrateId > 0
                && FixGuard.IsOn(ModConfig.FixStovesTakingCrateItems);
        }
    }
}
