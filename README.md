<p align="left"><img src="https://github.com/user-attachments/assets/8f60be5d-ecd3-42e9-867f-2d999a3a4101" width="192" alt="Keevi Fixes logo"></p>

# Keevi Fixes

**Keevi Fixes** is a collection of fixes for bugs and rough edges in the base game of Sailwind. Each fix addresses one specific problem, is enabled by default and can be turned off separately in the settings.

## What it fixes

- Lines drawn on some maps disappeared after loading.
- Moored boats came loose and drifted away after loading a save away from them, or after coming back to them.
- Mooring lines were tightened or loosened to wherever the boat happened to be after loading, pausing or coming back to the boat.
- After loading a save, footsteps and other sounds near the player could come from one side.
- An item picked up directly in front of another item could not be dropped or thrown until the player looked away.
- Climbing a boat ladder, or releasing any other button after it stopped being targeted, dropped the held item.
- Items could be picked up and used through boat walls, decks and ceilings.
- Big items rotated around a point at their base or edge instead of their centre.
- Cutting an item that had been lying on the boat for a while shook the boat with a thud.
- Cut pieces appeared at one end or at the bottom of the item, so they could fall through the deck or push each other apart.
- When a fish bit, a fishing rod on a hook or lying on the deck swung out of its place, its handle left the hook, and picking it up still meant aiming at where it had been.
- Saving stopped when an item could not be saved.
- Loading got stuck or duplicated items when an item or boat in the save could not be loaded.
- Items hung on ceiling hooks, such as pots or Windicators anemometers, fell off after loading a save or returning to the boat.
- Hung items slowly rotated relative to the boat while it rocked in waves.
- Small items that fell behind or under nailed furniture, such as a table or a bed, could not be picked up.
- Items stored in a closed crate could be picked up without opening it in some cases.
- Items stored in a crate ended up outside it or went missing from it after loading a save or coming back to the boat.
- Items picked up on an island vanished from the inventory after sailing away.
- A barrel taken from the cart, or right-clicked before being picked up, was held like a small item.
- After drinking from a barrel, it stayed held like a small item.
- The click that switched to the game window from another window was registered in the game, for example dropping the held item.
- Food, pipes, pots and kettles gave off a puff of smoke when they appeared, such as in shops or after loading a save.
- After charting, the map stayed selected: it remained outlined through walls and opened again when the charting kit was placed next to it. Other items and buttons could stay selected after menus as well, such as a winch after a pause.
- The charting kit could not be placed on tables and other furniture.
- Big items may vanish in some cases while being carried.
- The islands shown in the main menu stayed loaded while a save loaded, adding work and sounds no one could hear to the end of loading.
- Pausing while operating the bilge pump knocked its handle off its axis.

## Rotation around the centre of big items

In the base game, rotating a crate, barrel or piece of furniture swings it around a point at its base or edge, so it lurches across your view instead of turning in place. With Keevi Fixes it turns around its own middle, just like you'd expect.

Pair it with **[Deft Hands](https://github.com/KeeviDev/SailwindDeftHands)** and rotating items becomes a different game.

## Installation

**With a mod manager:** grab Keevi Fixes from Thunderstore with r2modman or Thunderstore Mod Manager — BepInEx comes along automatically.

**By hand:**

1. Install [BepInEx 5](https://github.com/BepInEx/BepInEx/releases).
2. Extract the release zip into `Sailwind/BepInEx/plugins`.
3. Set sail.

## Settings

Every fix is on by default and can be switched off on its own. Change them in `BepInEx/config/com.keevi.fixes.cfg`, or in-game with [Configuration Manager](https://github.com/BepInEx/BepInEx.ConfigurationManager).

| Section | Setting | What it fixes |
|---|---|---|
| All Items | Fix dropping after pickup | Items picked up in front of others can be dropped right away. |
| All Items | Fix dropping while interacting | Ladders and buttons no longer make you drop what you hold. |
| All Items | Fix reaching through walls | No picking up or using items through boat walls, decks and ceilings. |
| Big Items | Fix barrels after drinking | Barrels are held like barrels again after drinking. |
| Big Items | Fix barrels from the cart | Barrels from the cart behave like big items. |
| Big Items | Fix carried items vanishing ³ | Big items no longer vanish while you carry them. |
| Big Items | Fix rotation center | Big items turn around their middle. |
| Boats | Fix bilge pump handle | Pausing no longer breaks the bilge pump handle. |
| Boats | Fix moored boats coming loose ¹ | Moored boats stay tied after loading or coming back. |
| Boats | Fix mooring line length ¹ | Mooring lines keep the length you gave them. |
| Crates | Fix items falling out of crates | Items stay in their crate after loading or coming back. |
| Crates | Fix reaching into crates | No taking items out of a closed crate without opening it. |
| Fishing | Fix fishing rods turning | Rods on hooks or the deck stay in place when a fish bites. |
| Furniture | Fix charting kit placement | The charting kit goes on tables and other furniture. |
| Furniture | Fix reaching behind furniture ² | Small items behind or under furniture can be picked up. |
| Hooks | Fix hung item facing | Hung items stop slowly spinning in waves. |
| Hooks | Fix hung items falling | Hung items stay on their hooks after loading or coming back. |
| Interface | Fix focus click | Clicking back into the game no longer acts in it. |
| Interface | Fix stuck clicks | The map and other items no longer stay selected after charting or menus. |
| Knives | Fix boat shaking when cutting | Cutting no longer shakes the boat. |
| Knives | Fix cut piece placement | Cut pieces no longer fly apart or fall through the deck. |
| Maps | Fix map drawings disappearing | Drawings no longer disappear from maps. |
| Saving and Loading | Fix failed saves and loads | Saving and loading keep working when an item can't be saved or loaded. |
| Saving and Loading | Fix menu islands staying loaded | Lighter end of loading, without sounds no one can hear. |
| Small Items | Fix inventory items vanishing | Items in your inventory no longer vanish after sailing away. |
| Small Items | Fix smoke on item spawn | Food, pipes, pots and kettles no longer puff smoke when they appear. |
| Sound | Fix sound direction ³ | Footsteps and other sounds no longer come from one side after loading. |

¹ "Fix moored boats coming loose" and "Fix mooring line length" are turned off when **Mooring Line Fix** (DogEggz.Moorlinefix) is installed.

² "Fix reaching behind furniture" requires "Fix reaching into crates" and "Fix items falling out of crates" to be enabled.

³ Changes to "Fix carried items vanishing" and "Fix sound direction" take effect when the game starts.
