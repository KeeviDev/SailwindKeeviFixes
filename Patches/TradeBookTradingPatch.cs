using System;
using System.Collections.Generic;
using HarmonyLib;
using KeeviFixes.Configuration;
using KeeviFixes.Runtime;

namespace KeeviFixes.Patches
{
    /// <summary>
    /// Gives every port market without a price report of its own an empty one when a save loads, while
    /// <see cref="ModConfig.FixTradeBookTrading"/> is on.
    /// </summary>
    [HarmonyPatch(typeof(SaveLoadManager), ModDataMethod)]
    internal static class TradeBookTradingPatch
    {
        private const string ModDataMethod = "LoadModData";

        /// <summary>Skips this patch, with a warning, when the game lacks the members it relies on.</summary>
        private static bool Prepare()
        {
            bool supported = AccessTools.Method(typeof(SaveLoadManager), ModDataMethod) != null
                && AccessTools.Field(typeof(IslandMarket), nameof(IslandMarket.knownPrices)) != null
                && AccessTools.Field(typeof(Port), nameof(Port.ports)) != null;
            if (!supported)
            {
                Plugin.Log.LogWarning(
                    "Could not find the market price reports - the game may have updated. " +
                    "Buying and selling through the trade book may still only change the price at some ports.");
            }

            return supported;
        }

        /// <summary>Repairs the markets' own price reports before mods read the save's mod data.</summary>
        [HarmonyPriority(Priority.First)]
        private static void Prefix()
        {
            if (Port.ports == null || !FixGuard.IsOn(ModConfig.FixTradeBookTrading))
                return;

            try
            {
                RepairOwnReports();
            }
            catch (Exception ex)
            {
                FixGuard.Stop(ModConfig.FixTradeBookTrading, ex);
            }
        }

        private static void RepairOwnReports()
        {
            var repaired = new List<string>();
            foreach (Port port in Port.ports)
            {
                IslandMarket market = port != null ? port.GetComponent<IslandMarket>() : null;
                if (market == null || market.knownPrices == null)
                    continue;

                int own = market.GetPortIndex();
                if (own < 0 || own >= market.knownPrices.Length || market.knownPrices[own] != null)
                    continue;

                market.knownPrices[own] = new PriceReport();
                repaired.Add(market.GetPortName());
            }

            if (repaired.Count == 0)
                return;

            SaveRepair.ReportRepairedMarkets(repaired.Count);
            Plugin.Log.LogWarning(string.Format(
                "Made an empty price report for the market of {0}, which had none of its own in the save.",
                string.Join(", ", repaired.ToArray())));
        }
    }
}
