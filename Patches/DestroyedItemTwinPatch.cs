using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using KeeviFixes.Configuration;

namespace KeeviFixes.Patches
{
    /// <summary>
    /// Takes a destroyed item's physics twin out of the physics before the item leaves its boat. Leaving the boat
    /// moves the twin from the boat's walk copy into the world at the item's place, which is inside the boat itself,
    /// and the twin lives on for one more physics step. A twin that had settled is kinematic and pushed the whole boat
    /// out of itself: cutting a fish or a barrel that had lain for a while shook the boat with a thud.
    /// </summary>
    [HarmonyPatch(typeof(ShipItem), nameof(ShipItem.DestroyItem))]
    internal static class DestroyedItemTwinPatch
    {
        /// <summary>
        /// Skips this patch, with a warning, when the game no longer has the item removal the fix relies on.
        /// </summary>
        private static bool Prepare()
        {
            bool supported = AccessTools.Method(typeof(ShipItem), nameof(ShipItem.DestroyItem)) != null
                && AccessTools.Method(typeof(ShipItem), nameof(ShipItem.GetItemRigidbody)) != null;
            if (!supported)
            {
                Plugin.Log.LogWarning(
                    "Could not find the item removal - the game may have updated. " +
                    "Cutting items on a boat may still shake it.");
            }

            return supported;
        }

        /// <summary>
        /// Puts the twin away at the start of the game's own removal, so mods that skip removing an item keep its twin.
        /// </summary>
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

        /// <summary>
        /// Deactivates the twin, as the game does to freeze an item; it is destroyed with the item at the end of the
        /// frame either way.
        /// </summary>
        private static void DeactivateTwin(ShipItem item)
        {
            if (!ModConfig.PreventBoatShakingWhenCuttingItems.Value)
                return;

            ItemRigidbody twin = item.GetItemRigidbody();
            if (twin != null)
                twin.gameObject.SetActive(false);
        }
    }
}
