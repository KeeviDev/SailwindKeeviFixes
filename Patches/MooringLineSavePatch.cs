using System;
using HarmonyLib;
using KeeviFixes.Configuration;
using KeeviFixes.Runtime;

namespace KeeviFixes.Patches
{
    /// <summary>
    /// Writes the kept mooring line lengths into each save; see <see cref="MooringLineSave"/>.
    /// </summary>
    [HarmonyPatch(typeof(SaveLoadManager), nameof(SaveLoadManager.SaveModData))]
    internal static class MooringLineSavePatch
    {
        /// <summary>Skips this patch, with a warning, when the game lacks the members it relies on.</summary>
        private static bool Prepare()
        {
            if (!MooringLineSave.IsSupported)
            {
                Plugin.Log.LogWarning(
                    "Could not find the save's mod data - the game may have updated. " +
                    "Mooring lines may not keep their length after loading.");
            }

            return MooringLineSave.IsSupported;
        }

        private static void Postfix()
        {
            try
            {
                if (MooringFix.IsAvailable)
                    MooringLineSave.Write();
            }
            catch (Exception ex)
            {
                FixGuard.Stop(ModConfig.FixMooringLineLength, ex);
            }
        }
    }
}
