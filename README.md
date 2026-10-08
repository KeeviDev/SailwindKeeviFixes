<p align="left"><img src="https://github.com/user-attachments/assets/8f60be5d-ecd3-42e9-867f-2d999a3a4101" width="192" alt="Keevi Fixes logo"></p>

# Keevi Fixes

**Keevi Fixes** is a collection of fixes for bugs and rough edges in the base game of Sailwind. Each fix addresses one specific problem, is enabled by default and can be turned off separately in the settings.

## Rotation around the centre of big items

In the base game, rotating a crate, barrel or piece of furniture swings it around a point at its base or edge, so it lurches across your view instead of turning in place. With Keevi Fixes it turns around its own middle, just like you'd expect.

Pair it with **[Deft Hands](https://github.com/KeeviDev/SailwindDeftHands)** and rotating items becomes a different game.

## What it fixes

- An item picked up directly in front of another item could not be dropped or thrown until the player looked away.
- Climbing a boat ladder, or releasing any other button after it stopped being targeted, dropped the held item.
- Items could be picked up and used through boat walls, decks and ceilings.
- Items stored in a closed crate could be picked up without opening it by standing right next to the crate.
- Big items rotated around a point at their base or edge instead of their centre.
- After drinking from a barrel, it stayed held like a small item.
- Small items that fell behind or under nailed furniture, such as a table or a bed, could not be picked up.
- The charting kit could not be placed on tables and other furniture.
- Items hung on ceiling hooks, such as pots or Windicators anemometers, fell off after loading a save or returning to the boat.
- Hung items slowly rotated relative to the boat while it rocked in waves.
- Cutting an item that had been lying on the boat for a while shook the boat with a thud.
- Cut pieces appeared at one end or at the bottom of the item, so they could fall through the deck or push each other apart.
- Lines drawn on some maps disappeared after loading.
- Pausing while operating the bilge pump knocked its handle off its axis.
- Moored boats came loose and drifted away after loading a save away from them, or after coming back to them.
- Mooring lines were tightened or loosened to wherever the boat happened to be after loading, pausing or coming back to the boat.
- The click that switched to the game window from another window was registered in the game, for example dropping the held item.
- After charting, the map stayed selected: it remained outlined through walls and opened again when the charting kit was placed next to it. Other items and buttons could stay selected after menus as well, such as a winch after a pause.

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
| All Items | Allow dropping items picked up in front of others | Items picked up in front of others can be dropped right away. |
| All Items | Prevent dropping held items while interacting | Ladders and buttons no longer make you drop what you hold. |
| All Items | Prevent reaching items through walls | No picking up or using items through boat walls, decks and ceilings. |
| Crates | Prevent reaching items stored in crates | No taking items out of a closed crate without opening it. |
| Big Items | Rotate big items around their center | Big items turn around their middle. |
| Big Items | Return barrels state after drinking | Barrels are held like barrels again after drinking. |
| Furniture | Allow picking up small items behind nailed furniture ¹ | Small items behind or under furniture can be picked up. |
| Furniture | Allow placing the charting kit on furniture | The charting kit goes on tables and other furniture. |
| Hooks | Keep hung items facing | Hung items stop slowly spinning in waves. |
| Hooks | Prevent hung items falling after loading | Hung items stay on their hooks after loading or coming back. |
| Knives | Prevent boat shaking when cutting items | Cutting no longer shakes the boat. |
| Knives | Place cut pieces neatly | Cut pieces no longer fly apart or fall through the deck. |
| Maps | Show map drawings after loading | Drawings no longer disappear from maps. |
| Boats | Prevent bilge pump handle breaking on pause | Pausing no longer breaks the bilge pump handle. |
| Boats | Prevent moored boats coming loose ² | Moored boats stay tied after loading or coming back. |
| Boats | Keep mooring line length ² | Mooring lines keep the length you gave them. |
| Interface | Ignore the click that focuses the game | Clicking back into the game no longer acts in it. |
| Interface | Prevent stuck clicks after charting and menus | The map and other items no longer stay selected after charting or menus. |

¹ The fix "Allow picking up small items behind nailed furniture" requires "Prevent reaching items stored in crates" to be enabled.

² The fixes "Prevent moored boats coming loose" and "Keep mooring line length" are turned off when **Mooring Line Fix** (DogEggz.Moorlinefix) is installed.
