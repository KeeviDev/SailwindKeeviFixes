using HarmonyLib;
using KeeviFixes.Configuration;
using KeeviFixes.Runtime;
using UnityEngine;

namespace KeeviFixes.Patches
{
    /// <summary>
    /// Hangs loaded items back on their hooks; see <see cref="HungItemRehang"/>. Items on boats and in houses load in
    /// one batch per boat or house when the player comes close, after a save load as well, and the rest of the world's
    /// items load in one batch with the save. Hooks are found at the end of a batch and taken in each item's first
    /// physics step.
    /// </summary>
    [HarmonyPatch]
    internal static class HungItemRehangPatches
    {
        /// <summary>
        /// Skips these patches, with a warning, when the game no longer has the members the fix relies on.
        /// </summary>
        private static bool Prepare()
        {
            bool supported = HungItemRehang.IsSupported
                && AccessTools.Method(typeof(SaveablePrefab), nameof(SaveablePrefab.Load)) != null
                && AccessTools.Method(typeof(BoatLocalItems), "SpawnCachedItems") != null
                && AccessTools.Method(typeof(SaveLoadManager), nameof(SaveLoadManager.LoadGame)) != null
                && AccessTools.Method(typeof(HangableItem), "FixedUpdate") != null
                && AccessTools.Method(typeof(HangableItem), nameof(HangableItem.OnTriggerEnter)) != null;
            if (!supported)
            {
                Plugin.Log.LogWarning(
                    "Could not find the item loading or hook members - the game may have updated. " +
                    "Items may still fall from hooks after loading.");
            }

            return supported;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SaveablePrefab), nameof(SaveablePrefab.Load))]
        private static void AfterLoad(SaveablePrefab __instance, SavePrefabData data)
        {
            HungItemRehang.OnLoaded(__instance, data);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(BoatLocalItems), "SpawnCachedItems")]
        private static void AfterSpawnCachedItems()
        {
            HungItemRehang.AfterBatch();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SaveLoadManager), nameof(SaveLoadManager.LoadGame))]
        private static void AfterLoadGame()
        {
            HungItemRehang.AfterBatch();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(HangableItem), "FixedUpdate")]
        private static void BeforeFixedUpdate(HangableItem __instance)
        {
            HungItemRehang.BeforeFirstPhysicsStep(__instance);
        }

        /// <summary>
        /// Keeps an item that already hangs from moving to another hook its collider touches while it loads.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(HangableItem), nameof(HangableItem.OnTriggerEnter))]
        private static bool BeforeTriggerEnter(HangableItem __instance)
        {
            return !(__instance.IsHanging() && ModConfig.FixHungItemsFalling.Value);
        }
    }
}
