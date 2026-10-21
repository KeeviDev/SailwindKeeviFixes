using HarmonyLib;
using KeeviFixes.Configuration;

namespace KeeviFixes.Patches
{
    /// <summary>Renders a map's lines into its own texture as soon as the game updates it.</summary>
    [HarmonyPatch(typeof(MapChartTextureRenderer), "UpdateTexture")]
    internal static class MapDrawingRenderPatch
    {
        /// <summary>
        /// Skips this patch, with a warning, when the game lacks the map renderer members it relies on.
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
