# 2.2.0

- Weights like `:0.5` are read correctly when the game runs in a language with a decimal comma, such as German
- A bad number, an unbalanced brace or a negative weight gives a warning instead of an error, and the item string is skipped
- Fix a rare error when picking among weighted options, and a short stall when an item string has several `{}` groups
- `ParseItemString` returns false when an entry of an `&` group couldn't be resolved; the other entries are still given
- No longer needs R2API
- Item tiers of other mods can be used by the name of their `ItemTierDef`, with or without `TierDef` or `Def` at the end
- `Any` for an item of any tier, and `#Tag` and `!#Tag` to only pick items with or without an item tag, e.g. `Tier2#Healing`
- Mods that change what drop tables give also change the items picked from tiers and drop tables, with or without a blacklist

# 2.1.0

- Support Risk of Rain 2 1.5.0
- Find every drop table of the game, including the new ones and explicit drop tables. Drop table names are no longer case-sensitive
- Add the keywords `Cursed`, `Hex`, `Page`, `Equipment`, `LunarEquipment`, `LunarItem`, `PowerShape` and `KeyItem`
- Accept drone and key item names
- Skip item, equipment, drone and key item names from DLCs that are not enabled for the run, also with `availableOnly` set to false
- Fix the `!` blacklist, which was ignored
- `BurnNearby` now means Helfire Tincture instead of an unused item
- A segment that can't be parsed now gives a warning instead of being cut short
- Add `GivePickups()`, which gives pickups like picking them up and puts equipment into free slots of all equipment sets

# 2.0.0

- Add the `availableOnly`-Parameter
- Change the signature
- Add `ParseItemStringStrict()`
- Droptables now also check if the items are available in the run

# 1.0.1

- Small readme.md fixes

# 1.0.0

- Initial Release
