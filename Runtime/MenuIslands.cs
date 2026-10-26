using HarmonyLib;
using KeeviFixes.Configuration;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KeeviFixes.Runtime
{
    /// <summary>
    /// Lets the islands the game keeps loaded for the main menu load and unload by distance alone, without waiting
    /// between the two, from the frame after the save being loaded has been applied.
    /// </summary>
    internal static class MenuIslands
    {
        private static readonly AccessTools.FieldRef<IslandHorizon, float> LoadUnloadCooldown =
            Find<float>("loadUnloadCoolown");

        private static readonly AccessTools.FieldRef<IslandHorizon, bool> SceneLoaded = Find<bool>("sceneLoaded");

        private const int NoSaveApplied = -1;

        private static int saveAppliedFrame = NoSaveApplied;

        /// <summary>Gets whether the game has the island members this class uses.</summary>
        public static bool IsSupported
        {
            get { return LoadUnloadCooldown != null && SceneLoaded != null; }
        }

        /// <summary>Records that the save being loaded has been applied in this frame.</summary>
        public static void OnSaveApplied()
        {
            saveAppliedFrame = Time.frameCount;
        }

        /// <summary>
        /// Returns whether <paramref name="island"/> should not count as a starting island in this frame's check of
        /// whether to keep its scene loaded: true for a starting island while a save loads, from the frame after the
        /// save was applied, with <see cref="ModConfig.FixMenuIslandsStayingLoaded"/> on. When it returns true and the
        /// island's scene is neither loading nor unloading, clears the island's wait between loading and unloading its
        /// scene. Forgets the applied save once no save loads.
        /// </summary>
        public static bool ShouldRelease(IslandHorizon island)
        {
            if (!GameState.currentlyLoading)
            {
                saveAppliedFrame = NoSaveApplied;
                return false;
            }

            bool release = saveAppliedFrame != NoSaveApplied
                && Time.frameCount > saveAppliedFrame
                && island.startingIsland
                && ModConfig.FixMenuIslandsStayingLoaded.Value;
            if (release && SceneLoaded(island) == SceneManager.GetSceneByBuildIndex(island.islandIndex).isLoaded)
                LoadUnloadCooldown(island) = 0f;
            return release;
        }

        private static AccessTools.FieldRef<IslandHorizon, T> Find<T>(string name)
        {
            var field = AccessTools.Field(typeof(IslandHorizon), name);
            return field != null && field.FieldType == typeof(T)
                ? AccessTools.FieldRefAccess<IslandHorizon, T>(field)
                : null;
        }
    }
}
