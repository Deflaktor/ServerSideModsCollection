# StartBonusMod

This mod gives every player a configurable set of items and starting cash at the beginning of a run. 
I made it so that I can use [StartInBazaar](https://thunderstore.io/package/MagnusMagnuson/StartInBazaar/) in combination with [BiggerBazaar](https://thunderstore.io/package/MagnusMagnuson/BiggerBazaar/) and/or [BazaarIsMyHaven](https://thunderstore.io/package/Def/BazaarIsMyHaven/).
This mod is also compatible with [InLobbyConfig](https://thunderstore.io/package/KingEnderBrine/InLobbyConfig/).

## Starting Cash

- Configure how much starting cash the player shall receive.

## Simple Start Bonus Items

- Simply choose for each tier which starting item and how many the player shall start with. 
- There is also the option for a random item.
- Easiest to configure this is using InLobbyConfig.

## Advanced Start Bonus Item List

A more involved item list which gives you powerful operators to really customize the item list you want to start with.

- `ItemList`:
  - Defines what items players start with as a plain text string.
  - Items can be individual names, tiers, drop tables, or grouped sets with operators.
  - Separate item groups into AND lists (`&` operator) or OR lists (`|` operator), which cannot mix at the same level.
	- Example: `5xTier1 & 3xTier2` - Players start with 5 whites and 3 greens.
  - Equipment and items can be both specified in the same list. If multiple equipments are resolved, only as many are given as the survivor can hold.
	- Example: `{ BFG & 10xAutoCastEquipment } | { EliteFireEquipment & StrengthenBurn }` - Players start either with a Preon Accumulator and 10 Gesture of the Drowned or with Ifrit's Distinction and 1 Ignition Tank
  - For the full documentation see [ItemStringParser](https://thunderstore.io/package/Def/ItemStringParser/).
