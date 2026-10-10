using HarmonyLib;
using KeeviFixes.Configuration;

namespace KeeviFixes.Patches
{
    /// <summary>
    /// Renders a map's lines into its own texture as soon as the game updates it. The game draws every map through one
    /// shared camera, which renders once per frame into the map updated last, so when several maps update in the same
    /// frame, as they can when a save or a boat's items load, the others stay blank until drawn on again.
    /// </summary>
    [HarmonyPatch(typeof(MapChartTextureRenderer), "UpdateTexture")]
    internal static class MapDrawingRenderPatch
    {
        /// <summary>
        /// Skips this patch, with a warning, when the game no longer has the map renderer members the fix relies on.
        /// </summary>
        private static bool Prepare()
        {
            bool supported = AccessTools.Method(typeof(MapChartTextureRenderer), "UpdateTexture") != null
                && AccessTools.Field(typeof(MapChartTextureRenderer), "renderCam") != null;
            if (!supported)
            {
                Plugin.Log.LogWarning(
                    "Could not find the map drawing renderer - the game may have updated. " +
                    "Map drawings may stay hidden after loading.");
            }

            return supported;
        }

        private static void Postfix(MapChartTextureRenderer __instance)
        {
            if (ModConfig.FixMapDrawingsDisappearing.Value)
            {
                __instance.renderCam.Render();
            }
        }
    }
}
