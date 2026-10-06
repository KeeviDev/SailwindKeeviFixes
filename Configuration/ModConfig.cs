using BepInEx.Configuration;

namespace KeeviFixes.Configuration
{
    /// <summary>
    /// Mod's user-facing settings. <see cref="Bind"/> must run before any setting is read.
    /// </summary>
    internal static class ModConfig
    {
        private const string BigItemsSection = "Big Items";
        private const string MapsSection = "Maps";

        public static ConfigEntry<bool> RotateBigItemsAroundCenter { get; private set; }

        public static ConfigEntry<bool> ShowMapDrawingsAfterLoading { get; private set; }

        public static void Bind(ConfigFile config)
        {
            RotateBigItemsAroundCenter = config.Bind(
                BigItemsSection,
                "Rotate big items around their center",
                true,
                new ConfigDescription(
                    "Fixes the rotation point of big items: crates, barrels, furniture and other big items turn " +
                    "around their middle when you rotate them, instead of swinging around a point at their base " +
                    "or edge."));

            ShowMapDrawingsAfterLoading = config.Bind(
                MapsSection,
                "Show map drawings after loading",
                true,
                new ConfigDescription("Fixes drawings disappearing from maps."));
        }
    }
}
