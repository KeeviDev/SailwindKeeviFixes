# Changelog

### Changed

- "Fix reaching behind furniture" now also needs "Fix items falling out of crates" turned on
- A fix that runs into an error now turns itself off until you restart the game and writes the error to the log, instead of breaking what the game was doing

### Fixed

- Footsteps and other sounds near you no longer come from one side (can be turned off with the new "Fix sound direction" setting)
- Fishing rods on hooks or lying around no longer swing out of place when a fish bites; they now stay put and bend toward the fish ("Fix fishing rods turning")
- Food, pipes, pots and kettles no longer give off a puff of smoke when they appear, such as in shops or after loading a save ("Fix smoke on item spawn")
- Barrels taken from the cart, or right-clicked before you pick them up, are no longer held like small items ("Fix barrels from the cart")
- Items stored in a crate no longer end up outside it or go missing from it after loading a save or coming back to your boat ("Fix items falling out of crates")
- Food and firewood in an open crate are no longer pulled into a nearby stove or smoker ("Fix stoves taking crate items")
- Items in your inventory no longer vanish when you sail away from an island where you picked them up ("Fix inventory items vanishing")
- The trade book no longer shows the wrong goods in the warehouse yard or fails when you sell them ("Fix warehouse yard")
- Buying or selling through the trade book no longer only changes the price ("Fix trade book trading")
- Big items no longer occasionally vanish while you carry them ("Fix carried items vanishing")
- The game no longer stops saving, gets stuck loading or duplicates items when an item or boat can't be saved or loaded, such as one from a mod you removed ("Fix failed saves and loads")
- The controls hint of the item you hold, or of the last thing you looked at, no longer shows on a ladder, door or anything else you look at ("Fix leftover controls hint")
- The islands shown in the main menu no longer stay loaded while a save loads; the end of loading is lighter and no longer plays sounds you can't hear ("Fix menu islands staying loaded")

## [1.0.4] - 2026-11-04

### Changed

- Mooring line lengths are now saved apart from the game's own data, so a future game update can't overwrite them; saves from earlier versions keep their lengths

### Fixed

- Fixed a bug in "Fix moored boats coming loose": mooring lines no longer disappear after loading a save, leaving the boat loose

## [1.0.3] - 2026-11-04

### Fixed

- Updated "Fix moored boats coming loose" for Sailwind 0.40.1
- Fixed a bug in "Fix reaching through walls": items stuck in boat walls, decks or floors can be picked up again

## [1.0.2] - 2026-10-11

### Fixed

- Fixed a bug in "Fix boat shaking when cutting": new games with Sailwind Difficulty no longer start without the starter supplies

## [1.0.1] - 2026-10-10

### Changed

- Shorter setting names

## [1.0.0] - 2026-10-10

### Initial release

- Big items rotate around their centre instead of a point at their base or edge
- Items picked up in front of others can be dropped or thrown right away
- Held items are kept when climbing a ladder or using other items and buttons
- No picking up or using items through boat walls, decks and ceilings
- No taking items out of a closed crate without opening it
- Barrels are held like barrels again after drinking from them
- Small items behind or under nailed furniture can be picked up
- The charting kit can be placed on tables and other furniture
- Hung items stay on their hooks after loading a save or coming back to the boat
- Hung items no longer slowly spin while the boat rocks in waves
- Cutting items no longer shakes the boat
- Cut pieces land side by side instead of falling through the deck or flying apart
- Drawings no longer disappear from maps after loading
- Pausing no longer breaks the bilge pump handle
- Moored boats stay tied after loading a save or coming back to them
- Mooring lines keep the length you gave them
- The click that brings the game window back no longer acts in the game
- The map and other items no longer stay selected after charting or menus
- Every fix has its own switch in the config, grouped into sections

[1.0.4]: https://github.com/KeeviDev/SailwindKeeviFixes/releases/tag/v1.0.4
[1.0.3]: https://github.com/KeeviDev/SailwindKeeviFixes/releases/tag/v1.0.3
[1.0.2]: https://github.com/KeeviDev/SailwindKeeviFixes/releases/tag/v1.0.2
[1.0.1]: https://github.com/KeeviDev/SailwindKeeviFixes/releases/tag/v1.0.1
[1.0.0]: https://github.com/KeeviDev/SailwindKeeviFixes/releases/tag/v1.0.0
