using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using KeeviFixes.Configuration;
using UnityEngine;

namespace KeeviFixes.Runtime
{
    /// <summary>
    /// Keeps a save loading when parts of it can't be created, while <see cref="ModConfig.FixFailedSavesAndLoads"/>
    /// is on.
    /// </summary>
    internal static class SaveRepair
    {
        private const int NoMission = -1;

        private const float LateItemSeconds = 1f;

        private static readonly AccessTools.FieldRef<BoatLocalItems, bool> ItemsLoaded =
            AccessTools.FieldRefAccess<BoatLocalItems, bool>("itemsLoaded");

        private static SaveRepairReport loadReport;

        private static List<SavePrefabData> heldItems = new List<SavePrefabData>();

        /// <summary>
        /// Removes from <paramref name="save"/> the objects and items the game can't create and every second copy of
        /// an item, and takes goods off missions the save doesn't have. Holds back for <see cref="FinishLoad"/> the
        /// items whose item is missing from the game but whose boat is not.
        /// </summary>
        /// <returns>The removals, which later failed item loads of this save add to.</returns>
        public static SaveRepairReport RepairSave(SaveContainer save)
        {
            loadReport = new SaveRepairReport();
            heldItems = new List<SavePrefabData>();
            if (!FixGuard.IsOn(ModConfig.FixFailedSavesAndLoads))
                return loadReport;

            if (save.savedObjects != null)
                RemoveMissingObjects(save.savedObjects);

            if (save.savedPrefabs != null)
            {
                RemoveDuplicates(save.savedPrefabs);
                TakeOffMissingMissions(save.savedPrefabs, save.savedMissions != null ? save.savedMissions.Length : 0);
                HoldOrRemoveUncreatableItems(save.savedPrefabs);
            }

            return loadReport;
        }

        /// <summary>
        /// Waits until the player has been playing the loaded save for <see cref="LateItemSeconds"/>, then loads the
        /// held-back items whose item has appeared in the game since, removes the rest and shows
        /// <paramref name="report"/> to the player.
        /// </summary>
        public static IEnumerator FinishLoad(SaveRepairReport report)
        {
            while (GameState.currentlyLoading || !GameState.playing || GameState.justStarted)
                yield return null;

            yield return new WaitForSecondsRealtime(LateItemSeconds);

            try
            {
                LoadOrRemoveHeldItems();
            }
            catch (Exception ex)
            {
                FixGuard.Stop(ModConfig.FixFailedSavesAndLoads, ex);
            }

            SaveRepairNotice.Show(report);
        }

        /// <summary>
        /// Removes from <paramref name="cachedItems"/> the items the game can't create and the items that are already
        /// loaded.
        /// </summary>
        public static void RepairCache(BoatLocalItems owner, List<SavePrefabData> cachedItems)
        {
            if (cachedItems == null || !FixGuard.IsOn(ModConfig.FixFailedSavesAndLoads))
                return;

            var loaded = new HashSet<long>(SaveLoadManager.instance.GetCurrentPrefabs()
                .Where(saveable => saveable != null && saveable.instanceId != 0)
                .Select(saveable => Identity(saveable.instanceId, saveable.prefabIndex)));
            List<SavePrefabData> uncreatable = Extract(cachedItems, item => item == null || !HasPrefab(item));
            List<SavePrefabData> duplicates =
                Extract(cachedItems, item => item.instanceId != 0 && loaded.Contains(Identity(item)));
            if (uncreatable.Count == 0 && duplicates.Count == 0)
                return;

            AddToReport(uncreatable.Count, 0, duplicates.Count);
            Plugin.Log.LogWarning(string.Format(
                "{0}: skipped {1} cached item(s) missing from the game ({2}) and {3} already loaded ({4}).",
                owner.name,
                uncreatable.Count,
                DescribePrefabs(uncreatable),
                duplicates.Count,
                DescribePrefabs(duplicates)));
        }

        /// <summary>
        /// Removes <paramref name="saveable"/> from the game unless it was registered to save before its load threw
        /// <paramref name="error"/>, and logs the error.
        /// </summary>
        public static void OnLoadFailed(SaveablePrefab saveable, Exception error)
        {
            if (SaveLoadManager.instance.GetCurrentPrefabs().Contains(saveable))
            {
                Plugin.Log.LogError(string.Format(
                    "Could not finish loading {0}; it stays in the game: {1}",
                    saveable.name,
                    error));
                return;
            }

            UnityEngine.Object.Destroy(saveable.gameObject);
            AddToReport(1, 0, 0);
            Plugin.Log.LogError(string.Format("Could not load {0}; removed it: {1}", saveable.name, error));
        }

        private static void RemoveMissingObjects(List<SaveObjectData> savedObjects)
        {
            List<SaveObjectData> missing = Extract(savedObjects, saved => saved == null || !HasObject(saved.sceneIndex));
            if (missing.Count == 0)
                return;

            List<SaveObjectData> missingBoats =
                missing.Where(saved => saved != null && saved.customization != null).ToList();
            AddToReport(0, missingBoats.Count, 0);
            Plugin.Log.LogWarning(string.Format(
                "Removed {0} saved object(s) missing from the game, {1} of them boats: scene index {2}.",
                missing.Count,
                missingBoats.Count,
                string.Join(", ", missing.Where(saved => saved != null)
                    .Select(saved => saved.sceneIndex.ToString())
                    .ToArray())));
        }

        private static void HoldOrRemoveUncreatableItems(List<SavePrefabData> savedItems)
        {
            List<SavePrefabData> uncreatable = Extract(savedItems, item => item == null || !CanCreate(item));
            heldItems = Extract(uncreatable, item => item != null && HasPlace(item));
            if (uncreatable.Count == 0)
                return;

            AddToReport(uncreatable.Count, 0, 0);
            Plugin.Log.LogWarning(string.Format(
                "Removed {0} saved item(s) whose boat is missing from the game: {1}.",
                uncreatable.Count,
                DescribePrefabs(uncreatable)));
        }

        private static void LoadOrRemoveHeldItems()
        {
            List<SavePrefabData> held = heldItems;
            heldItems = new List<SavePrefabData>();
            List<SavePrefabData> missing = Extract(held, item => !CanCreate(item));
            foreach (SavePrefabData item in held)
                LoadLate(item);

            if (held.Count > 0)
            {
                Plugin.Log.LogInfo(string.Format(
                    "Loaded {0} saved item(s) whose item appeared after the save loaded: {1}.",
                    held.Count,
                    DescribePrefabs(held)));
            }

            if (missing.Count == 0)
                return;

            AddToReport(missing.Count, 0, 0);
            Plugin.Log.LogWarning(string.Format(
                "Removed {0} saved item(s) whose item is missing from the game: {1}.",
                missing.Count,
                DescribePrefabs(missing)));
        }

        private static void LoadLate(SavePrefabData item)
        {
            if (item.itemParentObject > 0)
            {
                BoatLocalItems owner = SaveLoadManager.instance.GetObject(item.itemParentObject).localItems;
                if (!ItemsLoaded(owner))
                {
                    owner.AddCachedItemFromSaveData(item);
                    return;
                }
            }

            GameObject prefab = PrefabsDirectory.instance.directory[item.prefabIndex];
            UnityEngine.Object.Instantiate(prefab, Vector3.zero, Quaternion.identity)
                .GetComponent<SaveablePrefab>()
                .Load(item);
        }

        private static void RemoveDuplicates(List<SavePrefabData> savedItems)
        {
            var seen = new HashSet<long>();
            List<SavePrefabData> duplicates =
                Extract(savedItems, item => item.instanceId != 0 && !seen.Add(Identity(item)));
            if (duplicates.Count == 0)
                return;

            AddToReport(0, 0, duplicates.Count);
            Plugin.Log.LogWarning(string.Format(
                "Removed {0} duplicated saved item(s): {1}.",
                duplicates.Count,
                DescribePrefabs(duplicates)));
        }

        private static void TakeOffMissingMissions(List<SavePrefabData> savedItems, int missionCount)
        {
            List<SavePrefabData> goods = savedItems.Where(item => item.itemMissionIndex >= missionCount).ToList();
            if (goods.Count == 0)
                return;

            foreach (SavePrefabData good in goods)
                good.itemMissionIndex = NoMission;

            Plugin.Log.LogWarning(string.Format(
                "Took {0} saved good(s) off missions missing from the save: {1}.",
                goods.Count,
                DescribePrefabs(goods)));
        }

        private static bool CanCreate(SavePrefabData item)
        {
            return HasPrefab(item.prefabIndex) && HasPlace(item);
        }

        private static bool HasPlace(SavePrefabData item)
        {
            return item.itemParentObject <= 0 || HasLocalItems(item.itemParentObject);
        }

        private static bool HasPrefab(SavePrefabData item)
        {
            return HasPrefab(item.prefabIndex);
        }

        private static bool HasPrefab(int prefabIndex)
        {
            GameObject[] prefabs = PrefabsDirectory.instance.directory;
            return prefabIndex >= 0 && prefabIndex < prefabs.Length && prefabs[prefabIndex] != null;
        }

        private static bool HasObject(int sceneIndex)
        {
            SaveableObject[] objects = SaveLoadManager.instance.GetCurrentObjects();
            return sceneIndex >= 0 && sceneIndex < objects.Length && objects[sceneIndex] != null;
        }

        private static bool HasLocalItems(int sceneIndex)
        {
            return HasObject(sceneIndex) && SaveLoadManager.instance.GetObject(sceneIndex).localItems != null;
        }

        private static List<T> Extract<T>(List<T> entries, Func<T, bool> match)
        {
            var extracted = new List<T>();
            var kept = new List<T>(entries.Count);
            foreach (T entry in entries)
                (match(entry) ? extracted : kept).Add(entry);

            if (extracted.Count > 0)
            {
                entries.Clear();
                entries.AddRange(kept);
            }

            return extracted;
        }

        /// <summary>Adds <paramref name="count"/> repaired markets to the notice of the save being loaded.</summary>
        public static void ReportRepairedMarkets(int count)
        {
            if (loadReport != null)
                loadReport.Markets += count;
        }

        private static void AddToReport(int items, int boats, int duplicates)
        {
            if (loadReport == null)
                return;

            loadReport.Items += items;
            loadReport.Boats += boats;
            loadReport.Duplicates += duplicates;
        }

        private static long Identity(SavePrefabData item)
        {
            return Identity(item.instanceId, item.prefabIndex);
        }

        private static long Identity(int instanceId, int prefabIndex)
        {
            return ((long)instanceId << 32) | (uint)prefabIndex;
        }

        private static string DescribePrefabs(IEnumerable<SavePrefabData> items)
        {
            return string.Join(", ", items.Where(item => item != null)
                .GroupBy(item => item.prefabIndex)
                .Select(group => group.Count() == 1
                    ? DescribePrefab(group.Key)
                    : string.Format("{0} x{1}", DescribePrefab(group.Key), group.Count()))
                .ToArray());
        }

        private static string DescribePrefab(int prefabIndex)
        {
            ShipItem item = HasPrefab(prefabIndex)
                ? PrefabsDirectory.instance.directory[prefabIndex].GetComponent<ShipItem>()
                : null;
            return item != null && !string.IsNullOrEmpty(item.name)
                ? string.Format("{0} (prefab {1})", item.name, prefabIndex)
                : "prefab " + prefabIndex;
        }
    }
}
