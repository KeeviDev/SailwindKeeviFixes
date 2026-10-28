using BepInEx.Configuration;

namespace KeeviFixes.Configuration
{
    /// <summary>Holds the mod's settings. <see cref="Bind"/> must run before any setting is read.</summary>
    internal static class ModConfig
    {
        private const string AllItemsSection = "All Items";
        private const string CratesSection = "Crates";
        private const string BigItemsSection = "Big Items";
        private const string SmallItemsSection = "Small Items";
        private const string FurnitureSection = "Furniture";
        private const string HooksSection = "Hooks";
        private const string KnivesSection = "Knives";
        private const string MapsSection = "Maps";
        private const string BoatsSection = "Boats";
        private const string FishingSection = "Fishing";
        private const string SoundSection = "Sound";
        private const string SavingAndLoadingSection = "Saving and Loading";
        private const string InterfaceSection = "Interface";

        public static ConfigEntry<bool> FixRotationCenter { get; private set; }

        public static ConfigEntry<bool> FixMapDrawingsDisappearing { get; private set; }

        public static ConfigEntry<bool> FixBarrelsAfterDrinking { get; private set; }

        public static ConfigEntry<bool> FixBarrelsFromTheCart { get; private set; }

        public static ConfigEntry<bool> FixBilgePumpHandle { get; private set; }

        public static ConfigEntry<bool> FixDroppingAfterPickup { get; private set; }

        public static ConfigEntry<bool> FixDroppingWhileInteracting { get; private set; }

        public static ConfigEntry<bool> FixReachingIntoCrates { get; private set; }

        public static ConfigEntry<bool> FixItemsFallingOutOfCrates { get; private set; }

        public static ConfigEntry<bool> FixReachingThroughWalls { get; private set; }

        public static ConfigEntry<bool> FixReachingBehindFurniture { get; private set; }

        public static ConfigEntry<bool> FixStuckClicks { get; private set; }

        public static ConfigEntry<bool> FixChartingKitPlacement { get; private set; }

        public static ConfigEntry<bool> FixHungItemFacing { get; private set; }

        public static ConfigEntry<bool> FixHungItemsFalling { get; private set; }

        public static ConfigEntry<bool> FixFocusClick { get; private set; }

        public static ConfigEntry<bool> FixBoatShakingWhenCutting { get; private set; }

        public static ConfigEntry<bool> FixCutPiecePlacement { get; private set; }

        public static ConfigEntry<bool> FixMooredBoatsComingLoose { get; private set; }

        public static ConfigEntry<bool> FixMooringLineLength { get; private set; }

        public static ConfigEntry<bool> FixFishingRodsTurning { get; private set; }

        public static ConfigEntry<bool> FixSoundDirection { get; private set; }

        public static ConfigEntry<bool> FixMenuIslandsStayingLoaded { get; private set; }

        public static ConfigEntry<bool> FixSmokeOnItemSpawn { get; private set; }

        public static void Bind(ConfigFile config)
        {
            FixRotationCenter = config.Bind(
                BigItemsSection,
                "Fix rotation center",
                true,
                new ConfigDescription(
                    "Fixes the rotation point of big items: crates, barrels, furniture and other big items turn " +
                    "around their middle when you rotate them, instead of swinging around a point at their base " +
                    "or edge."));

            FixMapDrawingsDisappearing = config.Bind(
                MapsSection,
                "Fix map drawings disappearing",
                true,
                new ConfigDescription("Fixes drawings disappearing from maps."));

            FixBarrelsAfterDrinking = config.Bind(
                BigItemsSection,
                "Fix barrels after drinking",
                true,
                new ConfigDescription(
                    "Fixes barrels being held like small items after you drink from them."));

            FixBarrelsFromTheCart = config.Bind(
                BigItemsSection,
                "Fix barrels from the cart",
                true,
                new ConfigDescription(
                    "Fixes barrels taken from the cart, or right-clicked before you pick them up, being held like " +
                    "small items."));

            FixBilgePumpHandle = config.Bind(
                BoatsSection,
                "Fix bilge pump handle",
                true,
                new ConfigDescription("Fixes the bilge pump handle breaking when you pause while holding it."));

            FixDroppingAfterPickup = config.Bind(
                AllItemsSection,
                "Fix dropping after pickup",
                true,
                new ConfigDescription(
                    "Fixes being unable to drop or throw an item you picked up in front of another one."));

            FixDroppingWhileInteracting = config.Bind(
                AllItemsSection,
                "Fix dropping while interacting",
                true,
                new ConfigDescription(
                    "Fixes dropping the item you hold when an interaction ends, such as climbing a ladder onto a " +
                    "boat or using other items and buttons."));

            FixReachingIntoCrates = config.Bind(
                CratesSection,
                "Fix reaching into crates",
                true,
                new ConfigDescription(
                    "Fixes picking up items stored in a crate or other storage without opening it."));

            FixItemsFallingOutOfCrates = config.Bind(
                CratesSection,
                "Fix items falling out of crates",
                true,
                new ConfigDescription(
                    "Fixes items stored in a crate or other storage ending up outside it or missing from it after " +
                    "loading a save or coming back to your boat."));

            FixReachingThroughWalls = config.Bind(
                AllItemsSection,
                "Fix reaching through walls",
                true,
                new ConfigDescription("Fixes picking up and using items through walls, floors and ceilings on boats."));

            FixReachingBehindFurniture = config.Bind(
                FurnitureSection,
                "Fix reaching behind furniture",
                true,
                new ConfigDescription(
                    "Lets you pick up small items behind or under nailed furniture, such as a cup that fell behind a " +
                    "table or under a bed. Needs \"Fix reaching into crates\" and \"Fix items falling out of " +
                    "crates\" turned on."));

            FixStuckClicks = config.Bind(
                InterfaceSection,
                "Fix stuck clicks",
                true,
                new ConfigDescription(
                    "Fixes the map staying selected after charting, and other items and buttons staying selected " +
                    "after menus, such as a winch after a pause."));

            FixChartingKitPlacement = config.Bind(
                FurnitureSection,
                "Fix charting kit placement",
                true,
                new ConfigDescription("Fixes being unable to put the charting kit on tables and other furniture."));

            FixHungItemFacing = config.Bind(
                HooksSection,
                "Fix hung item facing",
                true,
                new ConfigDescription("Fixes items hung on hooks slowly turning around when the boat rocks in waves."));

            FixHungItemsFalling = config.Bind(
                HooksSection,
                "Fix hung items falling",
                true,
                new ConfigDescription(
                    "Fixes items falling off hooks after loading a save or coming back to your boat."));

            FixFocusClick = config.Bind(
                InterfaceSection,
                "Fix focus click",
                true,
                new ConfigDescription(
                    "Fixes the click that switches to the game window acting in game, such as dropping the item you " +
                    "hold."));

            FixBoatShakingWhenCutting = config.Bind(
                KnivesSection,
                "Fix boat shaking when cutting",
                true,
                new ConfigDescription(
                    "Fixes the boat shaking with a thud when you cut something lying on it, such as a fish or a " +
                    "barrel."));

            FixCutPiecePlacement = config.Bind(
                KnivesSection,
                "Fix cut piece placement",
                true,
                new ConfigDescription("Fixes the pieces of a cut item flying apart or falling through the deck."));

            FixMooredBoatsComingLoose = config.Bind(
                BoatsSection,
                "Fix moored boats coming loose",
                true,
                new ConfigDescription(
                    "Fixes moored boats coming loose and drifting away after loading a save or coming back to them."));

            FixMooringLineLength = config.Bind(
                BoatsSection,
                "Fix mooring line length",
                true,
                new ConfigDescription(
                    "Fixes mooring lines changing their length after loading a save, pausing or coming back to the " +
                    "boat."));

            FixFishingRodsTurning = config.Bind(
                FishingSection,
                "Fix fishing rods turning",
                true,
                new ConfigDescription(
                    "Fixes fishing rods on hooks or lying around turning out of place when a fish bites."));

            FixSoundDirection = config.Bind(
                SoundSection,
                "Fix sound direction",
                true,
                new ConfigDescription(
                    "Fixes footsteps and other sounds near you coming from one side after loading a save. Requires " +
                    "a game restart."));

            FixMenuIslandsStayingLoaded = config.Bind(
                SavingAndLoadingSection,
                "Fix menu islands staying loaded",
                true,
                new ConfigDescription(
                    "Fixes the islands shown in the main menu staying loaded while a save loads: unloading them " +
                    "early lightens the end of loading and removes sounds you can't hear."));

            FixSmokeOnItemSpawn = config.Bind(
                SmallItemsSection,
                "Fix smoke on item spawn",
                true,
                new ConfigDescription(
                    "Fixes food, pipes, pots and kettles giving off a puff of smoke when they appear, such as " +
                    "in shops or after loading a save."));

            KeepFurnitureReachDependentOnCrateFixes();
        }

        /// <summary>
        /// Keeps <see cref="FixReachingBehindFurniture"/> on only while <see cref="FixReachingIntoCrates"/> and
        /// <see cref="FixItemsFallingOutOfCrates"/> are on: turning either crate setting off turns the furniture
        /// setting off, and turning the furniture setting on turns both crate settings on.
        /// </summary>
        private static void KeepFurnitureReachDependentOnCrateFixes()
        {
            DisableFurnitureReachWithoutCrateFixes();

            FixReachingIntoCrates.SettingChanged += (sender, args) => DisableFurnitureReachWithoutCrateFixes();
            FixItemsFallingOutOfCrates.SettingChanged += (sender, args) => DisableFurnitureReachWithoutCrateFixes();

            FixReachingBehindFurniture.SettingChanged += (sender, args) =>
            {
                if (!FixReachingBehindFurniture.Value)
                    return;

                if (!FixReachingIntoCrates.Value)
                    FixReachingIntoCrates.Value = true;
                if (!FixItemsFallingOutOfCrates.Value)
                    FixItemsFallingOutOfCrates.Value = true;
            };
        }

        private static void DisableFurnitureReachWithoutCrateFixes()
        {
            if (FixReachingBehindFurniture.Value
                && (!FixReachingIntoCrates.Value || !FixItemsFallingOutOfCrates.Value))
                FixReachingBehindFurniture.Value = false;
        }
    }
}
