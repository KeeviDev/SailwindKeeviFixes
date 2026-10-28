using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using KeeviFixes.Configuration;

namespace KeeviFixes.Patches
{
    /// <summary>Deactivates a destroyed item's physics twin before the item leaves its boat.</summary>
    [HarmonyPatch(typeof(ShipItem), nameof(ShipItem.DestroyItem))]
    internal static class DestroyedItemTwinPatch
    {
        /// <summary>
        /// Skips this patch, with a warning, when the game lacks the item removal members it relies on.
        /// </summary>
        private static bool Prepare()
        {
            bool supported = AccessTools.Method(typeof(ShipItem), nameof(ShipItem.DestroyItem)) != null
                && AccessTools.Field(typeof(ShipItem), nameof(ShipItem.itemRigidbodyC)) != null;
            if (!supported)
            {
                Plugin.Log.LogWarning(
                    "Could not find the item removal - the game may have updated. " +
                    "Cutting items on a boat may still shake it.");
            }

            return supported;
        }

        /// <summary>Inserts a call to <see cref="DeactivateTwin"/> at the start of the game's item removal.</summary>
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            var loadItem = new CodeInstruction(OpCodes.Ldarg_0);
            if (codes.Count > 0)
                loadItem.MoveLabelsFrom(codes[0]);

            codes.InsertRange(0, new[]
            {
                loadItem,
                new CodeInstruction(
                    OpCodes.Call,
                    AccessTools.Method(typeof(DestroyedItemTwinPatch), nameof(DeactivateTwin))),
            });
            return codes;
        }

        private static void DeactivateTwin(ShipItem item)
        {
            try
            {
                if (!FixGuard.IsOn(ModConfig.FixBoatShakingWhenCutting))
                    return;

                ItemRigidbody twin = item.itemRigidbodyC;
                if (twin != null)
                    twin.gameObject.SetActive(false);
            }
            catch (Exception ex)
            {
                FixGuard.Stop(ModConfig.FixBoatShakingWhenCutting, ex);
            }
        }
    }
}
