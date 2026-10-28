using System;
using HarmonyLib;
using KeeviFixes.Configuration;
using KeeviFixes.Runtime;

namespace KeeviFixes.Patches
{
    /// <summary>Places the pieces of items cut with a knife; see <see cref="CutPieces"/>.</summary>
    [HarmonyPatch]
    internal static class CutPiecesPatches
    {
        /// <summary>Skips these patches, with a warning, when the game lacks the members they rely on.</summary>
        private static bool Prepare()
        {
            bool supported = AccessTools.Method(typeof(ShipItemKnife), nameof(ShipItemKnife.OnAltActivate)) != null
                && AccessTools.Method(typeof(SaveablePrefab), nameof(SaveablePrefab.RegisterToSave)) != null;
            if (!supported)
            {
                Plugin.Log.LogWarning(
                    "Could not find the knife cut - the game may have updated. " +
                    "Cut pieces may still appear at one end of the item.");
            }

            return supported;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ShipItemKnife), nameof(ShipItemKnife.OnAltActivate))]
        [HarmonyPriority(Priority.First)]
        private static void BeforeCut(ShipItemKnife __instance)
        {
            try
            {
                CutPieces.BeforeCut(__instance);
            }
            catch (Exception ex)
            {
                FixGuard.Stop(ModConfig.FixCutPiecePlacement, ex);
            }
        }

        /// <summary>
        /// Places the pieces registered during the cut, including pieces cut by other patches on the same method.
        /// </summary>
        [HarmonyFinalizer]
        [HarmonyPatch(typeof(ShipItemKnife), nameof(ShipItemKnife.OnAltActivate))]
        private static Exception AfterCut(Exception __exception)
        {
            try
            {
                CutPieces.AfterCut();
            }
            catch (Exception ex)
            {
                FixGuard.Stop(ModConfig.FixCutPiecePlacement, ex);
            }

            return __exception;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SaveablePrefab), nameof(SaveablePrefab.RegisterToSave))]
        private static void AfterRegisterToSave(SaveablePrefab __instance)
        {
            try
            {
                CutPieces.OnRegistered(__instance);
            }
            catch (Exception ex)
            {
                FixGuard.Stop(ModConfig.FixCutPiecePlacement, ex);
            }
        }
    }
}
