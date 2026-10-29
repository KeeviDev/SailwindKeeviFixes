using System;
using System.Reflection;
using HarmonyLib;
using KeeviFixes.Configuration;
using KeeviFixes.Runtime;

namespace KeeviFixes.Patches
{
    /// <summary>Keeps inventory items from vanishing; see <see cref="InventoryItems"/>.</summary>
    [HarmonyPatch]
    internal static class InventoryItemsPatches
    {
        private const string SpawnerUpdate = "Update";
        private const string SpawnedItem = "item";

        /// <summary>Skips these patches, with a warning, when the game lacks the members they rely on.</summary>
        private static bool Prepare()
        {
            FieldInfo spawnedItem = AccessTools.Field(typeof(WorldItemSpawner), SpawnedItem);
            bool supported = spawnedItem != null && spawnedItem.FieldType == typeof(ShipItem)
                && AccessTools.Method(typeof(WorldItemSpawner), SpawnerUpdate) != null
                && AccessTools.Method(typeof(ItemRigidbody), nameof(ItemRigidbody.EnterInventorySlot)) != null
                && AccessTools.Method(typeof(ItemRigidbody), nameof(ItemRigidbody.GetShipItem)) != null;
            if (!supported)
            {
                Plugin.Log.LogWarning(
                    "Could not find the item spawner or inventory members - the game may have updated. " +
                    "Items in the inventory may still vanish when you sail away.");
            }

            return supported;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(WorldItemSpawner), SpawnerUpdate)]
        private static void BeforeSpawnerUpdate(ShipItem ___item)
        {
            try
            {
                InventoryItems.BeforeSpawnerUpdate(___item);
            }
            catch (Exception ex)
            {
                FixGuard.Stop(ModConfig.FixInventoryItemsVanishing, ex);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ItemRigidbody), nameof(ItemRigidbody.EnterInventorySlot))]
        private static void AfterEnterInventorySlot(ItemRigidbody __instance)
        {
            try
            {
                InventoryItems.OnEnteredInventory(__instance.GetShipItem());
            }
            catch (Exception ex)
            {
                FixGuard.Stop(ModConfig.FixInventoryItemsVanishing, ex);
            }
        }
    }
}
