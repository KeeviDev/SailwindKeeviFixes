using System;
using HarmonyLib;
using KeeviFixes.Runtime;

namespace KeeviFixes.Patches
{
    /// <summary>
    /// Places the pieces of items cut with a knife; see <see cref="CutPieces"/>. The knife's alternate use cuts in the
    /// game and in mods that patch it, and every piece is registered for saving, so the pieces are the items registered
    /// during that call.
    /// </summary>
    [HarmonyPatch]
    internal static class CutPiecesPatches
    {
        /// <summary>
        /// Skips these patches, with a warning, when the game no longer has the members the fix relies on.
        /// </summary>
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
            CutPieces.BeforeCut(__instance);
        }

        /// <summary>
        /// Runs after every postfix, so the pieces mods cut in their own postfixes are placed as well.
        /// </summary>
        [HarmonyFinalizer]
        [HarmonyPatch(typeof(ShipItemKnife), nameof(ShipItemKnife.OnAltActivate))]
        private static Exception AfterCut(Exception __exception)
        {
            CutPieces.AfterCut();
            return __exception;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SaveablePrefab), nameof(SaveablePrefab.RegisterToSave))]
        private static void AfterRegisterToSave(SaveablePrefab __instance)
        {
            CutPieces.OnRegistered(__instance);
        }
    }
}
