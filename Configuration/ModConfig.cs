using BepInEx.Configuration;

namespace KeeviFixes.Configuration
{
    /// <summary>
    /// Mod's user-facing settings. <see cref="Bind"/> must run before any setting is read.
    /// </summary>
    internal static class ModConfig
    {
        private const string AllItemsSection = "All Items";
        private const string CratesSection = "Crates";
        private const string BigItemsSection = "Big Items";
        private const string FurnitureSection = "Furniture";
        private const string MapsSection = "Maps";
        private const string BoatsSection = "Boats";
        private const string InterfaceSection = "Interface";

        public static ConfigEntry<bool> RotateBigItemsAroundCenter { get; private set; }

        public static ConfigEntry<bool> ShowMapDrawingsAfterLoading { get; private set; }

        public static ConfigEntry<bool> HoldBigContainersAfterDrinking { get; private set; }

        public static ConfigEntry<bool> PreventBilgePumpHandleBreaking { get; private set; }

        public static ConfigEntry<bool> DropItemsPickedUpInFrontOfOthers { get; private set; }

        public static ConfigEntry<bool> PreventDroppingHeldItemsWhileInteracting { get; private set; }

        public static ConfigEntry<bool> PreventReachingItemsStoredInCrates { get; private set; }

        public static ConfigEntry<bool> PreventReachingItemsThroughWalls { get; private set; }

        public static ConfigEntry<bool> AllowPickingUpSmallItemsBehindNailedFurniture { get; private set; }

        public static ConfigEntry<bool> PreventStuckClicksAfterChartingAndMenus { get; private set; }

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

            PreventDroppingHeldItemsWhileInteracting = config.Bind(
                AllItemsSection,
                "Prevent dropping held items while interacting",
                true,
                new ConfigDescription(
                    "Fixes dropping the item you hold when an interaction ends, such as climbing a ladder onto a " +
                    "boat or using other items and buttons."));

            PreventReachingItemsStoredInCrates = config.Bind(
                CratesSection,
                "Prevent reaching items stored in crates",
                true,
                new ConfigDescription(
                    "Fixes picking up items stored in a crate or other storage without opening it."));

            PreventReachingItemsThroughWalls = config.Bind(
                AllItemsSection,
                "Prevent reaching items through walls",
                true,
                new ConfigDescription("Fixes picking up and using items through walls, floors and ceilings on boats."));

            AllowPickingUpSmallItemsBehindNailedFurniture = config.Bind(
                FurnitureSection,
                "Allow picking up small items behind nailed furniture",
                true,
                new ConfigDescription(
                    "Lets you pick up small items behind or under nailed furniture, such as a cup that fell behind a " +
                    "table or under a bed. Needs \"Prevent reaching items stored in crates\" turned on."));

            PreventStuckClicksAfterChartingAndMenus = config.Bind(
                InterfaceSection,
                "Prevent stuck clicks after charting and menus",
                true,
                new ConfigDescription(
                    "Fixes the map staying selected after charting, and other items and buttons staying selected " +
                    "after menus, such as a winch after a pause."));

            KeepFurnitureReachDependentOnStoredItems();
        }

        /// <summary>
        /// Keeps picking up small items behind nailed furniture enabled only together with keeping stored items out of
        /// reach, since a look past nailed storage furniture would otherwise reach the items stored inside it: turning
        /// the stored items fix off turns the furniture one off, and turning the furniture one on turns it back on.
        /// </summary>
        private static void KeepFurnitureReachDependentOnStoredItems()
        {
            DisableFurnitureReachWithoutStoredItemsFix();

            PreventReachingItemsStoredInCrates.SettingChanged +=
                (sender, args) => DisableFurnitureReachWithoutStoredItemsFix();

            AllowPickingUpSmallItemsBehindNailedFurniture.SettingChanged += (sender, args) =>
            {
                if (AllowPickingUpSmallItemsBehindNailedFurniture.Value && !PreventReachingItemsStoredInCrates.Value)
                    PreventReachingItemsStoredInCrates.Value = true;
            };
        }

        private static void DisableFurnitureReachWithoutStoredItemsFix()
        {
            if (!PreventReachingItemsStoredInCrates.Value && AllowPickingUpSmallItemsBehindNailedFurniture.Value)
                AllowPickingUpSmallItemsBehindNailedFurniture.Value = false;
        }
    }
}
