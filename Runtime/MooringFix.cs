using BepInEx.Bootstrap;

namespace KeeviFixes.Runtime
{
    /// <summary>
    /// Decides whether the mooring line fixes may run: the game must still have the members they rely on, and
    /// MooringLineFix, which fixes the same, must not be installed.
    /// </summary>
    internal static class MooringFix
    {
        private const string MooringLineFixGuid = "DogEggz.Moorlinefix";

        private static bool isResolved;
        private static bool isAvailable;

        /// <summary>
        /// Whether the mooring line fixes may run. Read only after every plugin has loaded.
        /// </summary>
        public static bool IsAvailable
        {
            get
            {
                if (!isResolved)
                    Resolve();
                return isAvailable;
            }
        }

        private static void Resolve()
        {
            isResolved = true;
            if (Chainloader.PluginInfos.ContainsKey(MooringLineFixGuid))
            {
                Plugin.Log.LogInfo("MooringLineFix is installed, so Keevi Fixes leaves mooring lines to it.");
                return;
            }

            if (!MooringLineFields.AreFound)
            {
                Plugin.Log.LogWarning(
                    "Could not find the mooring line members - the game may have updated. " +
                    "Moored boats may still come loose, and mooring lines may still change their length.");
                return;
            }

            isAvailable = true;
        }
    }
}
