# 3.1.0

- Updated for Risk of Rain 2 1.5.0. Requires ItemStringParser 2.1.0.
- Fix starting cash, which the update broke.
- Simple Item List: also offers the new Cursed, Hex and Page tiers. Tiers without items are skipped.
- Starting equipment goes into the slot the game would use for a pickup, also with several equipment slots.
- Advanced Item List: equipment fills empty equipment slots first; equipment that doesn't fit drops next to you.
- A random item that can't be picked no longer stops the other starting items.
- Simple Item List: choices no longer reset to None when the game or a mod adds items. Saved choices are converted.
- Fix a random Lunar starting item sometimes giving nothing.
- Works with ProperSave: loading a save no longer gives the starting items again.
- No longer needs R2API.

# 3.0.3

- Improve robustness in combination with modded items

# 3.0.2

- Fix dependency with ItemStringParser

# 3.0.1

- Fix "An item with the same key has already been added" which can happen in combination with item mods

# 3.0.0

- Updated for Alloyed Collective
- Add FoodTier
- Remove Blacklist setting
- Add `!`-Operator. Blacklist entries can now be given inside the itemstring.
- Rename config sections so they are sorted.
- Remove Equipment setting from Advanced List. It is now merged with the itemstring.
- Now also possible to give multiple equipments to the player.
- ItemStringParser extracted to its own library mod.

# 2.0.1

- Fix readme.md markup issues

# 2.0.0

- Item lists are now loaded dynamically from what is available
- Add advanced starting bonus list feature

# 1.1.0

- The missing items introduced in the Seekers of the Storm update

# 1.0.1

- Gold value now scales with difficulty

# 1.0.0

- Initial Release
