using BepInEx.Configuration;

namespace KeeviFixes.Configuration
{
    /// <summary>
    /// Mod's user-facing settings. <see cref="Bind"/> must run before any setting is read.
    /// </summary>
    internal static class ModConfig
    {
        private const string AllItemsSection = "All Items";
        private const string BigItemsSection = "Big Items";
        private const string MapsSection = "Maps";
        private const string BoatsSection = "Boats";

        public static ConfigEntry<bool> RotateBigItemsAroundCenter { get; private set; }

        public static ConfigEntry<bool> ShowMapDrawingsAfterLoading { get; private set; }

        public static ConfigEntry<bool> HoldBigContainersAfterDrinking { get; private set; }

        public static ConfigEntry<bool> PreventBilgePumpHandleBreaking { get; private set; }

        public static ConfigEntry<bool> DropItemsPickedUpInFrontOfOthers { get; private set; }

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

            HoldBigContainersAfterDrinking = config.Bind(
                BigItemsSection,
                "Return barrels state after drinking",
                true,
                new ConfigDescription(
                    "Fixes barrels being held like small items after you drink from them."));

            PreventBilgePumpHandleBreaking = config.Bind(
                BoatsSection,
                "Prevent bilge pump handle breaking on pause",
                true,
                new ConfigDescription("Fixes the bilge pump handle breaking when you pause while holding it."));

            DropItemsPickedUpInFrontOfOthers = config.Bind(
                AllItemsSection,
                "Allow dropping items picked up in front of others",
                true,
                new ConfigDescription(
                    "Fixes being unable to drop or throw an item you picked up in front of another one."));
        }
    }
}
