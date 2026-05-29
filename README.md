# Eidee Easy Fishing
Customizable Mod for easy fishing.

## Outline:
This Mod was created to make fishing easier. You can customize from your configuration, such as skipping minigame, minigame make easier, always found a Treasure, always double fishing.

## Config:
During the game, you can reload the configuration by pressing the F5 key. It is possible to change the keys from the config.
Starting with version 1.1.1, you can now edit the configuration in the "[Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098)"

| Property                         | Description                                                                                                                     |
| ------------------------------   | ---------------------------------------------------------------------------------------------------------------------           |
| Bite Faster                      | Waiting for fish to bite the bait is no longer necessary.                                                                       |
| Hit Automatically                | Clicking the mouse to start the minigame is not required. If "Skip Minigame" is enabled, the fish will be caught automatically. |
| Treasure Always Be Found         | Discovering treasure is guaranteed on every attempt, even with "Skip Minigame" enabled.                                         |
| Always Golden Treasure           | Whenever treasure appears, whether naturally or forced, it will always be a golden treasure chest.                              |
| Always Caught Double Fish        | Using Wild Bait always results in catching two fish.                                                                            |
| Caught Double Fish On Any Bait   | The chance to catch two fish is present with any bait used, and can be combined with "Always Caught Double Fish".               |
| Always Magic Bait                | Treats every cast as if Magic Bait were equipped, ignoring season and location restrictions on which fish can bite.             |
| Always Curiosity Lure            | Treats every cast as if a Curiosity Lure were equipped, shifting the catch distribution toward harder-to-catch fish.            |
| Always Max Cast Power            | Casting power is always at its maximum.                                                                                         |
| Skip Minigame                    | The minigame is skipped, and fish are caught automatically, always achieving a perfect catch.                                   |
| Fish Easy Caught                 | In the minigame, the fish icon automatically chases the bar.                                                                    |
| Treasure Easy Caught             | In the minigame, the treasure icon automatically chases the bar.                                                                |
| Always Sonar Bobber              | Shows the Sonar Bobber fish identity panel during the minigame, even without a Sonar Bobber tackle equipped.                    |
| Always Max Fish Quality          | Forces every caught fish to Iridium quality, ignoring Quality Bobber count, rod tier, and perfect-catch requirements.           |
| Always Max Fish Size             | Forces every caught fish to its maximum recorded length, ignoring rod tier and the per-frame size penalty during the minigame.  |
| Fish Movement Speed Multiplier   | This is a multiplier for the fish's movement speed in the minigame.                                                             |
| Progress Bar Decrease Multiplier | This modifies how quickly the capture progress bar decreases in the minigame.                                                   |
| Progress Bar Increase Multiplier | This modifies how quickly the capture progress bar increases in the minigame.                                                   |
| Treasure Catch Speed Multiplier  | This is a multiplier for the speed at which treasure is caught in the minigame.                                                 |
| Reload Config                    | This sets the key for reloading the configuration.                                                                              |

## Stardew Valley 1.6 Notes:
Easy Fishing now covers the catch-side mechanics introduced in 1.6:

- **Challenge Bait** — Skip Minigame catches the remaining `challengeBaitFishes` count instead of a single fish, matching how the bait counts down on escapes.
- **Deluxe Bait** — The +12 px bar growth applied by the constructor is read back at catch time, so the mod's bar-position math stays correct.
- **Sonar Bobber** — The fish identity panel is honored as-equipped; *Always Sonar Bobber* injects it without consuming a tackle slot.
- **Mastery (Fishing) golden treasure** — Post-added treasure (from *Treasure Always Be Found*) inherits the Mastery(1) golden roll (`< 0.25 + AverageDailyLuck`) so vanilla rolls are not changed.
- **Statue of Blessings (Blessing of Waters)** — Compatible: the mod's progress-bar multipliers stack on top of the game's reduced penalty modifier without replacing it.
- **Fish Frenzy** — Bite Faster zeros the bite timer, which already short-circuits the frenzy-time halving; the mod does not target frenzy spawns.
- **Beginner's Rod** — Skip Minigame inherits the rod's quality/size floors unless *Always Max Fish Quality* / *Always Max Fish Size* are enabled, in which case the override wins.
- **Tutorial first cast** — The 0.1 starting `distanceFromCatching` for the first-ever fish is preserved instead of being stomped to 0.3.

*Always Magic Bait* and *Always Curiosity Lure* are the only options that change **which** fish is selected. They work by inserting a synthetic Magic Bait / Curiosity Lure into the rod's bait or tackle slot during the bite-wait window, and restoring the original as soon as the BobberBar opens (or the cast ends), before any bait/tackle consumption runs. The original bait stack and tackle durability are never decremented by the substitute, and the synthetic item is also restored before save and day-end so it cannot leak into a save file. Challenge Bait's catch counter and Deluxe Bait's larger bar height are replayed onto the BobberBar after restore so those bait effects are preserved alongside the Magic Bait selection. Specific Bait targeting is suppressed for casts where Always Magic Bait is in effect. When *Always Curiosity Lure* runs on a rod whose tackle slots are all occupied, the synthetic temporarily replaces the first tackle slot for one cast — that slot's BobberBar-construction-time effect (e.g. Cork Bobber's bar growth, and similar tackle effects) is suppressed for that single cast, while the tackle item itself and its durability are still restored before any consumption. *Always Magic Bait* also suppresses Wild / Challenge / Deluxe Bait's faster bite-time multiplier on missed-bite re-rolls within the same cast (the first-bite timer is computed before the swap and uses the original bait correctly); enable *Bite Faster* if you want the bite-wait window to stay short regardless.

**Multiplayer**: the two selection-altering options (*Always Magic Bait*, *Always Curiosity Lure*) are silently disabled in multiplayer sessions. The fishing rod's attachment slots are network-synced, so swapping in a synthetic on a farmhand could be persisted by the host's save (e.g. on disconnect). All other options work normally in multiplayer.

The progress bar multipliers affect the amount the bar changes from frame to frame. They do not replace the game's own bait, lure, or penalty rules.

## Contacts:
- [Issues - GitHub](https://github.com/eideehi/sdv-easyfishing/issues)
  Only bug reports are accepted under Issues.
- [eidee.net - Discord server](https://discord.gg/DDQqxkK7s6)
  Questions, suggestions, comments, etc. can be directed here.

## Credits:
* Dependencies:
  * [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098)

# License:
Eidee Easy Fishing is developed and released under the [MIT license](./LICENSE), except for the APIs of the libraries it depends on.
