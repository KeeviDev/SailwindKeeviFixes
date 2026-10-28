using HarmonyLib;
using KeeviFixes.Runtime;

namespace KeeviFixes.Patches
{
    /// <summary>
    /// Keeps stored items with their crate when the game saves, loads and caches items; see
    /// <see cref="CrateContents"/>.
    /// </summary>
    [HarmonyPatch]
    internal static class CrateContentsPatches
    {
        private const string ReadSaveMethod = "LoadNeeds";
        private const string CacheMethod = "CacheCurrentItems";

        /// <summary>Skips these patches, with a warning, when the game lacks the members they rely on.</summary>
        private static bool Prepare()
        {
            bool supported = AccessTools.Method(typeof(SaveLoadManager), ReadSaveMethod) != null
                && AccessTools.Method(typeof(SaveLoadManager), nameof(SaveLoadManager.SaveGame)) != null
                && AccessTools.Method(typeof(BoatLocalItems), CacheMethod) != null
                && AccessTools.Method(typeof(BoatLocalItems), nameof(BoatLocalItems.CacheItemsOnSinking)) != null;
            if (!supported)
            {
                Plugin.Log.LogWarning(
                    "Could not find the save loading or boat caching members - the game may have updated. " +
                    "Items stored in crates may still fall out of them after loading.");
            }

            return supported;
        }

        /// <summary>Repairs the saved items after the game has read the save and before it places them.</summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(SaveLoadManager), ReadSaveMethod)]
        private static void AfterSaveRead(SaveContainer save)
        {
            CrateContents.RepairSave(save.savedPrefabs);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SaveLoadManager), nameof(SaveLoadManager.SaveGame))]
        private static void BeforeSaving()
        {
            CrateContents.MatchCrateParents();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(BoatLocalItems), CacheMethod)]
        private static void BeforeCaching()
        {
            CrateContents.MatchCrateParents();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(BoatLocalItems), nameof(BoatLocalItems.CacheItemsOnSinking))]
        private static void BeforeSinking()
        {
            CrateContents.MatchCrateParents();
        }
    }
}
