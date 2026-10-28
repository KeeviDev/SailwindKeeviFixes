using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using KeeviFixes.Configuration;
using KeeviFixes.Runtime;

namespace KeeviFixes.Patches
{
    /// <summary>Keeps saves loading when parts of them can't be created; see <see cref="SaveRepair"/>.</summary>
    [HarmonyPatch]
    internal static class FailedLoadPatches
    {
        private const string ReadSaveMethod = "LoadNeeds";
        private const string SpawnCacheMethod = "SpawnCachedItems";

        /// <summary>Skips these patches, with a warning, when the game lacks the members they rely on.</summary>
        private static bool Prepare()
        {
            FieldInfo cache = AccessTools.Field(typeof(BoatLocalItems), "cachedItems");
            FieldInfo itemsLoaded = AccessTools.Field(typeof(BoatLocalItems), "itemsLoaded");
            bool supported =
                AccessTools.Method(typeof(SaveLoadManager), ReadSaveMethod, new[] { typeof(SaveContainer) }) != null
                && AccessTools.Method(typeof(BoatLocalItems), SpawnCacheMethod) != null
                && cache != null && cache.FieldType == typeof(List<SavePrefabData>)
                && itemsLoaded != null && itemsLoaded.FieldType == typeof(bool)
                && AccessTools.Method(typeof(SaveablePrefab), nameof(SaveablePrefab.Load)) != null;
            if (!supported)
            {
                Plugin.Log.LogWarning(
                    "Could not find the save loading members - the game may have updated. " +
                    "A broken item or boat in a save may still stop it from loading.");
            }

            return supported;
        }

        /// <summary>Repairs the save after the game has read it and before it places its boats and items.</summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(SaveLoadManager), ReadSaveMethod)]
        [HarmonyPriority(Priority.High)]
        private static void AfterSaveRead(SaveLoadManager __instance, SaveContainer save)
        {
            try
            {
                __instance.StartCoroutine(SaveRepair.FinishLoad(SaveRepair.RepairSave(save)));
            }
            catch (Exception ex)
            {
                FixGuard.Stop(ModConfig.FixFailedSavesAndLoads, ex);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(BoatLocalItems), SpawnCacheMethod)]
        private static void BeforeSpawnCache(BoatLocalItems __instance, List<SavePrefabData> ___cachedItems)
        {
            try
            {
                SaveRepair.RepairCache(__instance, ___cachedItems);
            }
            catch (Exception ex)
            {
                FixGuard.Stop(ModConfig.FixFailedSavesAndLoads, ex);
            }
        }

        /// <summary>Swallows an exception from an item's load after handling the item, and rethrows any other.</summary>
        [HarmonyFinalizer]
        [HarmonyPatch(typeof(SaveablePrefab), nameof(SaveablePrefab.Load))]
        private static Exception AfterLoad(SaveablePrefab __instance, Exception __exception)
        {
            if (__exception == null || !FixGuard.IsOn(ModConfig.FixFailedSavesAndLoads))
                return __exception;

            try
            {
                SaveRepair.OnLoadFailed(__instance, __exception);
                return null;
            }
            catch (Exception ex)
            {
                FixGuard.Stop(ModConfig.FixFailedSavesAndLoads, ex);
                return __exception;
            }
        }
    }
}
