using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using KeeviFixes.Configuration;
using UnityEngine;

namespace KeeviFixes.Patches
{
    /// <summary>
    /// Routes the pointer's look ray through <see cref="Raycast"/>, which leaves out the items stored in crates. The
    /// game moves stored items to the ItemInCrate layer, which collides with nothing, but its look ray still hits that
    /// layer. Stored items sit at the crate's base, so a player who stepped right up to the crate, with the ray
    /// starting inside it, picked them up without opening the crate and without taking them out of it.
    /// </summary>
    [HarmonyPatch(typeof(GoPointer), "DoRaycast")]
    internal static class LookRayPatch
    {
        private const string ItemInCrateLayerName = "ItemInCrate";

        private static readonly MethodInfo VanillaRaycast = AccessTools.Method(
            typeof(Physics),
            nameof(Physics.Raycast),
            new[] { typeof(Ray), typeof(RaycastHit).MakeByRefType(), typeof(float), typeof(int) });

        private static int itemInCrateMask;

        /// <summary>
        /// Skips this patch, with a warning, when the game no longer has the raycast or the layer the fix relies on.
        /// </summary>
        private static bool Prepare()
        {
            int layer = LayerMask.NameToLayer(ItemInCrateLayerName);
            bool supported = VanillaRaycast != null && layer >= 0;
            if (!supported)
            {
                Plugin.Log.LogWarning(
                    "Could not find the look raycast or the ItemInCrate layer - the game may have updated. " +
                    "Items stored in crates may still be picked up without opening them.");
                return false;
            }

            itemInCrateMask = 1 << layer;
            return true;
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo replacement = AccessTools.Method(typeof(LookRayPatch), nameof(Raycast));
            var codes = new List<CodeInstruction>(instructions);
            bool patched = false;

            foreach (CodeInstruction code in codes)
            {
                if (code.Calls(VanillaRaycast))
                {
                    code.operand = replacement;
                    patched = true;
                    break;
                }
            }

            if (!patched)
            {
                Plugin.Log.LogError(
                    "Could not find the look raycast to patch in GoPointer.DoRaycast - the game may have updated. " +
                    "Items stored in crates may still be picked up without opening them.");
            }

            return codes;
        }

        /// <summary>
        /// The game's look raycast, without the items stored in crates when the setting is on.
        /// </summary>
        private static bool Raycast(Ray ray, out RaycastHit hit, float maxDistance, int layerMask)
        {
            if (ModConfig.PreventReachingItemsStoredInCrates.Value)
                layerMask &= ~itemInCrateMask;

            return Physics.Raycast(ray, out hit, maxDistance, layerMask);
        }
    }
}
