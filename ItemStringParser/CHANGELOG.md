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
