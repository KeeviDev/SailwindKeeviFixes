using System;
using HarmonyLib;
using KeeviFixes.Runtime;

namespace KeeviFixes.Patches
{
    /// <summary>
    /// Unloads the main menu's islands once a loading save has been applied; see <see cref="MenuIslands"/>.
    /// </summary>
    [HarmonyPatch]
    internal static class MenuIslandsPatches
    {
        /// <summary>Skips these patches, with a warning, when the game lacks the members they rely on.</summary>
        private static bool Prepare()
        {
            bool supported = MenuIslands.IsSupported
                && AccessTools.Method(typeof(SaveLoadManager), nameof(SaveLoadManager.LoadGame)) != null
                && AccessTools.Method(typeof(IslandHorizon), "LateUpdate") != null
                && AccessTools.Field(typeof(IslandHorizon), nameof(IslandHorizon.startingIsland)) != null
                && AccessTools.Field(typeof(IslandHorizon), nameof(IslandHorizon.islandIndex)) != null;
            if (!supported)
            {
                Plugin.Log.LogWarning(
                    "Could not find the island loading members - the game may have updated. " +
                    "The main menu's islands may still stay loaded while a save loads.");
            }

            return supported;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SaveLoadManager), nameof(SaveLoadManager.LoadGame))]
        private static void AfterLoadGame()
        {
            MenuIslands.OnSaveApplied();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(IslandHorizon), "LateUpdate")]
        private static void BeforeLateUpdate(IslandHorizon __instance, out bool __state)
        {
            __state = MenuIslands.ShouldRelease(__instance);
            if (__state)
                __instance.startingIsland = false;
        }

        [HarmonyFinalizer]
        [HarmonyPatch(typeof(IslandHorizon), "LateUpdate")]
        private static Exception AfterLateUpdate(IslandHorizon __instance, bool __state, Exception __exception)
        {
            if (__state)
                __instance.startingIsland = true;

            return __exception;
        }
    }
}
