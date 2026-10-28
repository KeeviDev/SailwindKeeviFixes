using System.Collections.Generic;
using KeeviFixes.Configuration;

namespace KeeviFixes.Runtime
{
    /// <summary>
    /// Keeps the items stored in a crate saved, cached and loaded together with that crate while
    /// <see cref="ModConfig.FixItemsFallingOutOfCrates"/> is on.
    /// </summary>
    internal static class CrateContents
    {
        /// <summary>The lowest save parent of an item that is not being cached or sunk with its boat.</summary>
        private const int LooseParent = -1;

        /// <summary>
        /// Gives every stored item in <paramref name="savedItems"/> its crate's save parent and position, and turns the
        /// stored items whose crate is not in the list into loose items.
        /// </summary>
        public static void RepairSave(List<SavePrefabData> savedItems)
        {
            if (savedItems == null || !FixGuard.IsOn(ModConfig.FixItemsFallingOutOfCrates))
                return;

            Dictionary<int, SavePrefabData> savedById = IndexByInstanceId(savedItems);
            int rejoined = 0;
            int freed = 0;
            foreach (SavePrefabData item in savedItems)
            {
                if (item == null || item.crateId <= 0)
                    continue;

                SavePrefabData crate;
                if (!savedById.TryGetValue(item.crateId, out crate))
                {
                    item.crateId = 0;
                    freed++;
                }
                else if (item.itemParentObject != crate.itemParentObject)
                {
                    item.itemParentObject = crate.itemParentObject;
                    item.position = crate.position;
                    rejoined++;
                }
            }

            if (rejoined > 0 || freed > 0)
            {
                Plugin.Log.LogInfo(string.Format(
                    "Put {0} stored item(s) back with their crate and took {1} out of crates missing from the save.",
                    rejoined,
                    freed));
            }
        }

        /// <summary>Gives every item stored in a loaded crate that crate's save parent.</summary>
        public static void MatchCrateParents()
        {
            if (!FixGuard.IsOn(ModConfig.FixItemsFallingOutOfCrates) || SaveLoadManager.instance == null)
                return;

            foreach (SaveablePrefab saveable in SaveLoadManager.instance.GetCurrentPrefabs())
            {
                if (saveable == null || saveable.GetParentObject() < LooseParent)
                    continue;

                CrateInventory crate = saveable.GetComponent<CrateInventory>();
                if (crate != null)
                    GiveParent(crate.containedItems, saveable.GetParentObject());
            }
        }

        /// <summary>
        /// Gives the items stored in <paramref name="crate"/> whose save parent is still
        /// <paramref name="previousParent"/> the crate's current save parent, when the crate has left that boat or
        /// house for no owner or another one.
        /// </summary>
        public static void FollowCrate(ShipItem crate, int previousParent)
        {
            if (previousParent <= 0 || !FixGuard.IsOn(ModConfig.FixItemsFallingOutOfCrates))
                return;

            CrateInventory inventory = crate.GetComponent<CrateInventory>();
            SaveablePrefab crateSaveable = crate.GetComponent<SaveablePrefab>();
            if (inventory == null || crateSaveable == null)
                return;

            int parent = crateSaveable.GetParentObject();
            if (parent == previousParent || parent < LooseParent)
                return;

            foreach (ShipItem item in inventory.containedItems)
            {
                if (item == null)
                    continue;

                SaveablePrefab saveable = item.GetComponent<SaveablePrefab>();
                if (saveable != null && saveable.GetParentObject() == previousParent)
                    saveable.SetParentObject(parent);
            }
        }

        /// <summary>Returns the save parent of <paramref name="crate"/> if it stores any item, otherwise zero.</summary>
        public static int ParentOfFilledCrate(ShipItem crate)
        {
            CrateInventory inventory = crate.GetComponent<CrateInventory>();
            if (inventory == null || inventory.containedItems.Count == 0)
                return 0;

            SaveablePrefab saveable = crate.GetComponent<SaveablePrefab>();
            return saveable != null ? saveable.GetParentObject() : 0;
        }

        private static void GiveParent(List<ShipItem> storedItems, int parent)
        {
            foreach (ShipItem item in storedItems)
            {
                if (item == null)
                    continue;

                SaveablePrefab saveable = item.GetComponent<SaveablePrefab>();
                if (saveable != null && saveable.GetParentObject() >= LooseParent)
                    saveable.SetParentObject(parent);
            }
        }

        private static Dictionary<int, SavePrefabData> IndexByInstanceId(List<SavePrefabData> savedItems)
        {
            var savedById = new Dictionary<int, SavePrefabData>();
            foreach (SavePrefabData saved in savedItems)
            {
                if (saved != null && !savedById.ContainsKey(saved.instanceId))
                    savedById.Add(saved.instanceId, saved);
            }

            return savedById;
        }
    }
}
