using KeeviFixes.Configuration;
using UnityEngine;

namespace KeeviFixes.Runtime
{
    /// <summary>
    /// Keeps items in the inventory or the cart from vanishing with the island or house they were picked up in, while
    /// <see cref="ModConfig.FixInventoryItemsVanishing"/> is on.
    /// </summary>
    internal static class InventoryItems
    {
        private const int LooseParent = -1;

        /// <summary>Moves <paramref name="spawnedItem"/> to the world once it is held.</summary>
        public static void BeforeSpawnerUpdate(ShipItem spawnedItem)
        {
            if (spawnedItem != null && spawnedItem.held != null && FixGuard.IsOn(ModConfig.FixInventoryItemsVanishing))
                MoveToWorld(spawnedItem);
        }

        /// <summary>
        /// Takes <paramref name="item"/>, which has just entered an inventory slot or the cart, off the house or
        /// boat it is saved with and moves it to the world.
        /// </summary>
        public static void OnEnteredInventory(ShipItem item)
        {
            if (item == null || !FixGuard.IsOn(ModConfig.FixInventoryItemsVanishing))
                return;

            SaveablePrefab saveable = item.Getsaveable() ?? item.GetComponent<SaveablePrefab>();
            if (saveable == null || saveable.GetParentObject() <= 0)
                return;

            saveable.SetParentObject(LooseParent);
            MoveToWorld(item);
        }

        private static void MoveToWorld(ShipItem item)
        {
            Transform world = FloatingOriginManager.instance != null ? FloatingOriginManager.instance.transform : null;
            if (world != null && item.transform.parent != world)
                item.transform.parent = world;
        }
    }
}
