using System;
using System.Collections.Generic;
using HarmonyLib;
using KeeviFixes.Configuration;

namespace KeeviFixes.Patches
{
    /// <summary>
    /// Keeps a warehouse yard's goods to those really in the yard when the trade book reads them, while
    /// <see cref="ModConfig.FixWarehouseYard"/> is on.
    /// </summary>
    [HarmonyPatch]
    internal static class TradeBookYardPatches
    {
        private const int NoSlot = -1;

        private const int NoGood = -1;

        /// <summary>Skips these patches, with a warning, when the game lacks the members they rely on.</summary>
        private static bool Prepare()
        {
            bool supported = AccessTools.Method(
                    typeof(IslandMarketWarehouseArea),
                    nameof(IslandMarketWarehouseArea.ValidateList)) != null
                && AccessTools.Field(typeof(IslandMarketWarehouseArea), "goodsInArea")?.FieldType == typeof(List<Good>)
                && AccessTools.Field(typeof(IslandMarketWarehouseArea), "market")?.FieldType == typeof(IslandMarket)
                && AccessTools.Method(typeof(EconomyUI), nameof(EconomyUI.OpenUI)) != null
                && AccessTools.Method(typeof(IslandMarket), nameof(IslandMarket.GetWarehouseArea)) != null;
            if (!supported)
            {
                Plugin.Log.LogWarning(
                    "Could not find the warehouse yard members - the game may have updated. " +
                    "The trade book may still sell goods from the cart.");
            }

            return supported;
        }

        /// <summary>
        /// Removes from the yard's goods those destroyed or put in the cart or the inventory, and recounts the goods
        /// the trade book shows.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(IslandMarketWarehouseArea), nameof(IslandMarketWarehouseArea.ValidateList))]
        private static void BeforeValidateList(List<Good> ___goodsInArea, IslandMarket ___market)
        {
            if (___goodsInArea == null || ___market == null || !FixGuard.IsOn(ModConfig.FixWarehouseYard))
                return;

            try
            {
                if (___goodsInArea.RemoveAll(IsGone) > 0)
                    Recount(___goodsInArea, ___market.currentPlayerGoods);
            }
            catch (Exception ex)
            {
                FixGuard.Stop(ModConfig.FixWarehouseYard, ex);
            }
        }

        /// <summary>Checks the yard's goods before the trade book shows them.</summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(EconomyUI), nameof(EconomyUI.OpenUI))]
        private static void BeforeOpenUI(IslandMarket islandMarket)
        {
            if (islandMarket == null || !FixGuard.IsOn(ModConfig.FixWarehouseYard))
                return;

            try
            {
                IslandMarketWarehouseArea yard = islandMarket.GetWarehouseArea();
                if (yard != null)
                    yard.ValidateList();
            }
            catch (Exception ex)
            {
                FixGuard.Stop(ModConfig.FixWarehouseYard, ex);
            }
        }

        private static bool IsGone(Good good)
        {
            if (good == null)
                return true;

            ShipItem item = good.GetComponent<ShipItem>();
            return item == null || item.GetCurrentInventorySlot() != NoSlot;
        }

        private static void Recount(List<Good> goods, int[] counts)
        {
            Array.Clear(counts, 0, counts.Length);
            foreach (Good good in goods)
            {
                SaveablePrefab saveable = good.GetComponent<SaveablePrefab>();
                int index = saveable != null ? PrefabsDirectory.ItemToGoodIndex(saveable.prefabIndex) : NoGood;
                if (index >= 0 && index < counts.Length)
                    counts[index]++;
            }
        }
    }
}
