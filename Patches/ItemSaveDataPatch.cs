using System;
using HarmonyLib;
using KeeviFixes.Configuration;

namespace KeeviFixes.Patches
{
    /// <summary>
    /// Keeps an item's save data when a patch on the game's save data step throws after the game has built it, while
    /// <see cref="ModConfig.FixFailedSavesAndLoads"/> is on.
    /// </summary>
    [HarmonyPatch(typeof(SaveablePrefab), nameof(SaveablePrefab.PrepareSaveData))]
    internal static class ItemSaveDataPatch
    {
        /// <summary>Skips this patch, with a warning, when the game lacks the save data step.</summary>
        private static bool Prepare()
        {
            bool supported =
                AccessTools.Method(typeof(SaveablePrefab), nameof(SaveablePrefab.PrepareSaveData)) != null;
            if (!supported)
            {
                Plugin.Log.LogWarning(
                    "Could not find the item save data step - the game may have updated. " +
                    "A mod failing to save an item may still stop saving.");
            }

            return supported;
        }

        /// <summary>
        /// Logs and swallows an exception thrown after the game built the save data, and rethrows any other.
        /// </summary>
        private static Exception Finalizer(Exception __exception, SavePrefabData __result, SaveablePrefab __instance)
        {
            if (__exception == null || __result == null || !FixGuard.IsOn(ModConfig.FixFailedSavesAndLoads))
                return __exception;

            try
            {
                Plugin.Log.LogError(string.Format(
                    "A mod failed to save {0}; it is saved without that mod's data: {1}",
                    __instance.name,
                    __exception));
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
