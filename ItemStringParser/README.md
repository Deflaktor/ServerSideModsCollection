# ItemStringParser

This mod is a library for parsing ItemStrings. See bottom on the developer manual.

[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/E1E71PHUJ2)

## User Manual

ItemStrings are a way to describe a droptable using plain text. They let you describe what items should appear and how many in a simple, readable line. ItemStrings can include instructions for repeating items, choosing items randomly, or excluding certain items.

### ItemString Operators

ItemString operators are symbols that help you decide which items to give, repeat, or exclude in a list. They make it easy to write rules like "give this item", "skip that one", or "choose one randomly" using short codes. Each entry in the ItemString roughly follows the format:

`<repeat>x<itemkey>#<tag>!<itemname>*<multiplier>: <weight>`

Entries can be separated by `&` or `|`. Entries can be grouped with `{` and `}`.

#### `!` (Not-Operator)
- **Purpose:** Blacklist items from tiers or droptables.
- **Placement:** Directly *after* the name of the droptable or tier.
- **Example:**
  - `Tier1!Hoof` - White item but not a Hoof
  - `dtChest1!Bear!Crowbar` - Content of a Small Chest but neither Tougher Times nor Crowbar.

#### `#` (Tag-Operator)
- **Purpose:** Only pick items of tiers or droptables that have an item tag, like the category chests do. With `!#`, only items without the tag.
- **Placement:** Directly *after* the name of the droptable or tier, mixed with `!` as you like.
- **Tags:** `Damage`, `Healing`, `Utility`, `OnKillEffect`, `SprintRelated`, `LowHealth`, `EquipmentRelated`, `MobilityRelated`, `FoodRelated` and the other tags of the game. Not case-sensitive.
- **Example:**
  - `Tier2#Healing` - Green healing item
  - `Any#Damage#OnKillEffect` - Damage item of any tier that also has an on-kill effect
  - `Tier1!#Damage!Bear` - White item that is no damage item and not a Tougher Times

#### `&` (And-Operator)
- **Purpose:** Every item separated by `&` will be selected.
- **Example:**
  - `Hoof & Boss` - The player receives both a Hoof and a random Boss item.

#### `|` (Or-Operator)
- **Purpose:** Only one of the items separated by `|` will be chosen at random.
- **Weights:** You can specify selection weights using a colon `:` after the item (e.g., `Tier1: 0.5 | Tier2: 0.5`).
- **Example:**
  - `Tier1 | Tier2` - The player gets either a Tier1 or a Tier2 item (equal chance unless weighted).

#### `:` (Weight Separator)
- **Purpose:** Assigns probability weights to options with the Or-Operator (`|`).
- **Default:** If omitted, the default weight 1 is used.
- **Example:**
  - `Tier1: 0.6 | Tier2: 0.4` - Tier1 has a 60% chance, Tier2 a 40% chance.
  - `Tier1: 3.0 | Tier2` - Tier1 has a 75% chance, Tier2 a 25% chance.

#### `x` (Repeat-Operator)
- **Purpose:** Repeats the immediately following item or group a fixed number of times.
- **Placement:** Directly *before* a group or item.
- **Example:**  
  - `5xHoof` - The player gets 5 Hoof items.
  - `3x{ Tier1 | Tier2 }` - The player gets 3 times a randomly selected item from Tier1 or Tier2.

#### `*` (Multiplier-Operator)
- **Purpose:** Multiplies the amount of items of a group.
- **Placement:** Directly *after* a group or item.
- **Example:**
  - `Hoof*5` - The player gets 5 Hoof items.
  - `{ Tier1 | Tier2 }*3` - The player gets 3 copies of a single type of item from either Tier1 or Tier2.

#### `{` and `}` (Grouping Symbols)
- **Purpose:** Group items and operators together. Can use `&` and `|` within a group, but they cannot be mixed.
- **Operator precedence:** Use `{ }` to explicitly control grouping and order of operations, especially when combining repeat, and, or multiply.
- **Nested groups:** You can nest `{ }` for more complex item generation logic.
- **Example:**
  - `{ 2xdtChest1 | dtChest2 }` - Represents a choice between "2x small chest" and "1 large chest" as a single entity; useful with other operators.

### Summary Table

| Operator | Symbol | Description                                               | Example               |
|----------|--------|-----------------------------------------------------------|-----------------------|
| Not      | `!`    | Blacklists the item from the group                        | `A!B`                 |
| And      | `&`    | All items, together                                       | `A & B`               |
| Or       | `\|`   | Only one item, random (optional weight via `:`)           | `A \| B:0.7 \| C:0.3` |
| Weight   | `:`    | Specifies probability weight for options when using `\|`  | `A: 0.6 \| B: 0.4`    |
| Repeat   | `x`    | Repeats the following item/group N times                  | `5xA` or `3x{A \| B}` |
| Multiply | `*`    | Multiplies the amount of items in the group by N          | `{A & B}*4`           |
| Grouping | `{}`   | Groups items/expressions                                  | `{A & 2xB}`           |

### Examples

A random lunar item, except for Light Flux Pauldron and Stone Flux Pauldron:
```
ItemList = Lunar!HalfSpeedDoubleHealth!HalfAttackSpeedHalfCooldowns
```

5 Hooves:
```
ItemList = 5xHoof
```

Also 5 Hooves:
```
ItemList = Hoof*5
```

5 random whites:
```
ItemList = 5xTier1
```

5 copies of the same white:
```
ItemList = Tier1*5
```

5 Hooves and 2 Armor Plates:
```
ItemList = 5xHoof & 2xArmorPlate
```

Either 5 Hooves or 2 Armor Plates:
```
ItemList = 5xHoof | 2xArmorPlate
```

Either 5 Hooves with 30% chance or 2 Armor Plates with 70% chance:
```
ItemList = 5xHoof: 0.3 | 2xArmorPlate: 0.7
```

Infusion and either 5 Hooves with 30% chance or 2 Armor Plates with 70% chance:
```
ItemList = Infusion & { 5xHoof: 0.3 | 2xArmorPlate: 0.7 }
```

Either Infusion or Feather and either 5 Hooves with 30% chance or 2 Armor Plates with 70% chance:
```
ItemList = { Infusion | Feather } & { 5xHoof: 0.3 | 2xArmorPlate: 0.7 }
```

75% chance: Infusion and feather, 25% chance: 5 hoves and 5 Armor Plates
```
ItemList = { Infusion & Feather }: 0.75 | { Hoof & ArmorPlate }*5: 0.25
```

Either 5 whites or 3 greens or 1 red:
```
ItemList = 5xTier1 | 3xTier2 | Tier3
```

Between 1 and 5 whites.
```
ItemList = 1xTier1 | 2xTier1 | 3xTier1 | 4xTier1 | 5xTier1
```

10 random items which consist of around 50% whites, around 35% greens and around 15% red:
```
ItemList = 10x{ Tier1: 0.5 | Tier2: 0.35 | Tier3: 0.15 }
```

5 times one of the following: Either the contents of 2 small chests (except for Tougher Times) or the content of 1 large chest
```
ItemList = 5x{ 2xdtChest1!Bear | dtChest2 }
```

Preon Accumulator and 10 Gesture of the Drowned or Ifrit's Distinction and 1 Ignition Tank
```
{ BFG & 10xAutoCastEquipment } | { EliteFireEquipment & StrengthenBurn }
```

### Item Keywords

You can use:
- **Internal item, equipment, drone and key item names** (see [R2Wiki - Items-and-Equipments-Data](https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Developer-Reference/Items-and-Equipments-Data/)). `BurnNearby` is Helfire Tincture, the equipment; the item of the same name is unused. Names from a DLC that is not enabled for the run are skipped.
- **Item Tier names**: `Tier1`, `Tier2`, `Tier3`, `Lunar`, `Boss`, `VoidTier1`, `VoidTier2`, `VoidTier3`, `VoidBoss`, `FoodTier`, `Cursed`, `Hex`, `Page`. `Lunar` contains lunar items and lunar equipment.
- **Modded item tiers**: the name of the tier's `ItemTierDef`, or that name without `TierDef` or `Def` at the end, e.g. `Highlander` for `HighlanderTierDef`. Case and characters other than letters, digits and `_` don't count. If the short name fits several tiers, use the full name.
- **Other drop lists of the run**: `Equipment`, `LunarEquipment`, `LunarItem`, `PowerShape`, `KeyItem`
- **`Any`**: an item of any tier, without equipment and power shapes. Mostly useful with tags, e.g. `Any#Healing`.
- **Droptable names**: e.g. `dtChest1`, `dtLunarChest`, `dtVoidChest`. See below.

### Droptable Names

Every drop table of the game can be used by its name, also ones added by a game update. Names are not case-sensitive. Mods that change what drop tables give change the items picked from them and from tiers too, also with a blacklist. These are the most useful ones:

| Drop Table Name | Source | tier1 | tier2 | tier3 | boss | lunar | equipment | voidTier1 | voidTier2 | voidTier3 | voidBoss | cursed | page | requiredItemTags | bannedItemTags |
|-----------------|--------|-------|-------|-------|------|-------|-----------|-----------|-----------|-----------|----------|--------|------|------------------|----------------|
| dtChest1 | Chest | 0.8 | 0.2 | 0.01 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |  |  |
| dtChest2 | Large Chest | 0 | 0.8 | 0.2 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |  |  |
| dtGoldChest | Legendary Chest | 0 | 0 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |  |  |
| dtCasinoChest | Adaptive Chest | 0.7 | 0.3 | 0.01 | 0 | 0 | 0.1 | 0 | 0 | 0 | 0 | 0 | 0 |  |  |
| dtSmallChestDamage | Chest - Damage | 0.8 | 0.2 | 0.01 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | Damage |  |
| dtSmallChestHealing | Chest - Healing | 0.8 | 0.2 | 0.01 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | Healing |  |
| dtSmallChestUtility | Chest - Utility | 0.8 | 0.2 | 0.01 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | Utility |  |
| dtCategoryChest2Damage | Large Chest - Damage | 0 | 0.8 | 0.2 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | Damage |  |
| dtCategoryChest2Healing | Large Chest - Healing | 0 | 0.8 | 0.2 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | Healing |  |
| dtCategoryChest2Utility | Large Chest - Utility | 0 | 0.8 | 0.2 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | Utility |  |
| dtLunarChest | Lunar Pod | 0 | 0 | 0 | 0 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |  |  |
| dtEquipment | Equipment Barrel | 0 | 0 | 0 | 0 | 0 | 1 | 0 | 0 | 0 | 0 | 0 | 0 |  |  |
| dtShrineChance | Shrine of Chance | 8 | 2 | 0.2 | 0 | 0 | 2 | 0 | 0 | 0 | 0 | 0 | 0 |  |  |
| dtChanceDoll | Shrine of Chance with Chance Doll | 0 | 0.79 | 0.2 | 0.01 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |  |  |
| dtLockbox | Rusty Lockbox | 0 | 4 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |  |  |
| dtVoidChest | Void Cradle | 0 | 0 | 0 | 0 | 0 | 0 | 6 | 3 | 1 | 0 | 0 | 0 |  |  |
| dtVoidCamp | Void Seed | 40 | 40 | 10 | 3 | 0 | 0 | 5.714286 | 5.714286 | 1.25 | 0 | 0 | 0 |  |  |
| dtCommandChest | Artifact of Command | 0.2 | 0.2 | 0.05 | 0.05 | 0 | 0.2 | 0.1 | 0.1 | 0.05 | 0.05 | 0 | 0 |  |  |
| dtMonsterTeamTier1Item | Artifact of Evolution | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |  | AIBlacklist, OnKillEffect, EquipmentRelated, SprintRelated |
| dtMonsterTeamTier2Item | Artifact of Evolution | 0 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |  | AIBlacklist, OnKillEffect, EquipmentRelated, SprintRelated |
| dtMonsterTeamTier3Item | Artifact of Evolution | 0 | 0 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |  | AIBlacklist, OnKillEffect, EquipmentRelated, SprintRelated |
| dtITDefaultWave | Simulacrum | 80 | 10 | 0.25 | 0.25 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |  |  |
| dtITBossWave | Simulacrum | 0 | 80 | 7.5 | 7.5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |  |  |
| dtITSpecialBossWave | Simulacrum | 0 | 0 | 80 | 20 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |  |  |
| dtTemporaryItemsDistributor | Temporary Item Distributor | 0.8 | 0.2 | 0.01 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | CanBeTemporary |  |
| bdtHallowedCauldron | Hallowed Shrine | 1 | 1 | 1 | 0.25 | 0 | 0 | 0 | 0 | 0 | 0 | 15 | 0.01 |  |  |

### EBNF

You can use this EBNF to validate your item string, for example at [PaulKlineLabs' BNF Playground](https://bnfplayground.pauliankline.com/?bnf=%3CItemList%3E%20%20%20%20%20%20%20%3A%3A%3D%20%3CAND_LIST%3E%20%7C%20%3COR_LIST%3E%0A%3CAND_LIST%3E%20%20%20%20%20%20%20%3A%3A%3D%20%3CENTRY%3E%20%28%22%20%26%20%22%20%3CENTRY%3E%29%2a%0A%3COR_LIST%3E%20%20%20%20%20%20%20%20%3A%3A%3D%20%3CWEIGHTED_ENTRY%3E%20%28%22%20%7C%20%22%20%3CWEIGHTED_ENTRY%3E%29%2a%0A%3CENTRY%3E%20%20%20%20%20%20%20%20%20%20%3A%3A%3D%20%28%3Crep%3E%20%22x%22%29%3F%20%3CITEM_KEY%3E%20%28%22%2a%22%20%3Cmult%3E%29%3F%0A%3CWEIGHTED_ENTRY%3E%20%3A%3A%3D%20%3CENTRY%3E%20%28%22%3A%20%22%20%3Cweight%3E%29%3F%0A%3CITEM_KEY%3E%20%20%20%20%20%20%20%3A%3A%3D%20%3Cname%3E%20%7C%20%3Ctier%3E%20%3CFILTER%3E%2a%20%7C%20%3Cdroptable%3E%20%3CFILTER%3E%2a%20%7C%20%22%7B%20%22%20%3CAND_LIST%3E%20%22%20%7D%22%20%7C%20%22%7B%20%22%20%3COR_LIST%3E%20%22%20%7D%22%0A%3CFILTER%3E%20%20%20%20%20%20%20%20%20%3A%3A%3D%20%22%21%22%20%3Cname%3E%20%7C%20%22%23%22%20%3Ctag%3E%20%7C%20%22%21%23%22%20%3Ctag%3E%0A%0A%3Cname%3E%20%20%20%20%20%20%3A%3A%3D%20%28%5Ba-z%5D%20%7C%20%5BA-Z%5D%20%7C%20%5B0-9%5D%20%7C%20%22_%22%29%2B%0A%3Cdroptable%3E%20%3A%3A%3D%20%3Cname%3E%0A%3Ctier%3E%20%20%20%20%20%20%3A%3A%3D%20%22Tier1%22%20%7C%20%22Tier2%22%20%7C%20%22Tier3%22%20%7C%20%22Lunar%22%20%7C%20%22Boss%22%20%7C%20%22VoidTier1%22%20%7C%20%22VoidTier2%22%20%7C%20%22VoidTier3%22%20%7C%20%22VoidBoss%22%20%7C%20%22FoodTier%22%20%7C%20%22Cursed%22%20%7C%20%22Hex%22%20%7C%20%22Page%22%20%7C%20%22Equipment%22%20%7C%20%22LunarEquipment%22%20%7C%20%22LunarItem%22%20%7C%20%22PowerShape%22%20%7C%20%22KeyItem%22%20%7C%20%22Any%22%20%7C%20%3Cname%3E%0A%3Ctag%3E%20%20%20%20%20%20%20%3A%3A%3D%20%22Damage%22%20%7C%20%22Healing%22%20%7C%20%22Utility%22%20%7C%20%22AIBlacklist%22%20%7C%20%22Cleansable%22%20%7C%20%22OnKillEffect%22%20%7C%20%22EquipmentRelated%22%20%7C%20%22SprintRelated%22%20%7C%20%22WorldUnique%22%20%7C%20%22Scrap%22%20%7C%20%22BrotherBlacklist%22%20%7C%20%22CannotSteal%22%20%7C%20%22CannotCopy%22%20%7C%20%22PriorityScrap%22%20%7C%20%22CannotDuplicate%22%20%7C%20%22LowHealth%22%20%7C%20%22HoldoutZoneRelated%22%20%7C%20%22InteractableRelated%22%20%7C%20%22ObliterationRelated%22%20%7C%20%22OnStageBeginEffect%22%20%7C%20%22HalcyoniteShrine%22%20%7C%20%22RebirthBlacklist%22%20%7C%20%22DevotionBlacklist%22%20%7C%20%22ExtractorUnitBlacklist%22%20%7C%20%22IgnoreForDropList%22%20%7C%20%22PowerShape%22%20%7C%20%22CommandArtifactBlacklist%22%20%7C%20%22FoodRelated%22%20%7C%20%22MobilityRelated%22%20%7C%20%22AllowedForUseAsCraftingIngredient%22%20%7C%20%22CanBeTemporary%22%20%7C%20%22ObjectiveRelated%22%20%7C%20%22Technology%22%20%7C%20%22HiddenForDroneBuffIcon%22%20%7C%20%22SacrificeBlacklist%22%20%7C%20%22IgnoreSafekeep%22%20%7C%20%22Page%22%20%7C%20%22CannotMagnify%22%20%7C%20%22ReaperBoss%22%20%7C%20%22CanBeRental%22%20%7C%20%22AlchemyBlacklist%22%20%7C%20%22DeathAffixBlacklist%22%20%7C%20%22Ghost%22%20%7C%20%22CannotTransform%22%20%7C%20%22CannotDisable%22%0A%3Crep%3E%20%20%20%20%20%20%20%3A%3A%3D%20%3Cint%3E%0A%3Cmult%3E%20%20%20%20%20%20%3A%3A%3D%20%3Cint%3E%0A%3Cweight%3E%20%20%20%20%3A%3A%3D%20%3Cfloat%3E%0A%0A%3Cint%3E%20%20%20%3A%3A%3D%20%5B1-9%5D%20%5B0-9%5D%2a%0A%3Cfloat%3E%20%3A%3A%3D%20%28%220%22%20%7C%20%3Cint%3E%29%20%28%22.%22%20%5B0-9%5D%2B%20%29%3F&name=Risk%20of%20Rain%202%20ItemStringParser). `<name>` stands for the internal names of items, equipment, drones, key items and modded item tiers.

```
<ItemList>       ::= <AND_LIST> | <OR_LIST>
<AND_LIST>       ::= <ENTRY> (" & " <ENTRY>)*
<OR_LIST>        ::= <WEIGHTED_ENTRY> (" | " <WEIGHTED_ENTRY>)*
<ENTRY>          ::= (<rep> "x")? <ITEM_KEY> ("*" <mult>)?
<WEIGHTED_ENTRY> ::= <ENTRY> (": " <weight>)?
<ITEM_KEY>       ::= <name> | <tier> <FILTER>* | <droptable> <FILTER>* | "{ " <AND_LIST> " }" | "{ " <OR_LIST> " }"
<FILTER>         ::= "!" <name> | "#" <tag> | "!#" <tag>

<name>      ::= ([a-z] | [A-Z] | [0-9] | "_")+
<droptable> ::= <name>
<tier>      ::= "Tier1" | "Tier2" | "Tier3" | "Lunar" | "Boss" | "VoidTier1" | "VoidTier2" | "VoidTier3" | "VoidBoss" | "FoodTier" | "Cursed" | "Hex" | "Page" | "Equipment" | "LunarEquipment" | "LunarItem" | "PowerShape" | "KeyItem" | "Any" | <name>
<tag>       ::= "Damage" | "Healing" | "Utility" | "AIBlacklist" | "Cleansable" | "OnKillEffect" | "EquipmentRelated" | "SprintRelated" | "WorldUnique" | "Scrap" | "BrotherBlacklist" | "CannotSteal" | "CannotCopy" | "PriorityScrap" | "CannotDuplicate" | "LowHealth" | "HoldoutZoneRelated" | "InteractableRelated" | "ObliterationRelated" | "OnStageBeginEffect" | "HalcyoniteShrine" | "RebirthBlacklist" | "DevotionBlacklist" | "ExtractorUnitBlacklist" | "IgnoreForDropList" | "PowerShape" | "CommandArtifactBlacklist" | "FoodRelated" | "MobilityRelated" | "AllowedForUseAsCraftingIngredient" | "CanBeTemporary" | "ObjectiveRelated" | "Technology" | "HiddenForDroneBuffIcon" | "SacrificeBlacklist" | "IgnoreSafekeep" | "Page" | "CannotMagnify" | "ReaperBoss" | "CanBeRental" | "AlchemyBlacklist" | "DeathAffixBlacklist" | "Ghost" | "CannotTransform" | "CannotDisable"
<rep>       ::= <int>
<mult>      ::= <int>
<weight>    ::= <float>

<int>   ::= [1-9] [0-9]*
<float> ::= ("0" | <int>) ("." [0-9]+ )?
```

## Developer Manual

As a developer if you want to use the ItemStringParser in your mod, here is what you need to do.

1. Add the `ItemStringParser.dll` as dependency to your Visual Studio project. Set *Copy Local* to *No* so that the dll does not get copied to your build output. Example:

```xml
<Reference Include="ItemStringParser">
  <HintPath>libs\ItemStringParser.dll</HintPath>
  <Private>false</Private>
</Reference>
```

2. Add `[BepInDependency(ItemStringParser.ItemStringParser.PluginGUID)]` to your `UnityBasePlugin` class
3. Add dependency string `Def-ItemStringParser-2.1.0` to the dependencies list in your `manifest.json`

It provides three public methods: (1) `ParseItemString`, (2) `GivePickups` and (3) `ResolveItemKey`

*Attention:* The methods rely on the `Run.instance.available*DropList` to be populated. As such, only run the ItemStringParser after `Run.BuildDropTable()` was executed.

### ParseItemString

This is the main method you will want to use. This method interprets an item string, applying repetitions and other formatting rules, to build a collection of items/equipments with their amounts.

#### Signature

```c#
bool ItemStringParser.ItemStringParser.ParseItemString(string itemString, Dictionary<PickupIndex, int> resolvedItems, ManualLogSource log, bool availableOnly = true, int index = -1)
```

`itemString (string):` The input string containing item definitions to parse. It includes items, operators, and formatting syntax.

`resolvedItems (Dictionary<PickupIndex, int>)`: A dictionary to which parsed item entries and their amounts will be added or updated.

`log (ManualLogSource)`: For logging in case the provided itemString contains syntax errors.

`availableOnly (bool, optional)`: If true will prevent concrete item names from being resolved if they are disabled or not yet unlocked. Names from a DLC that is not enabled for the run never resolve.

`index (int, optional)`: Specifies if a certain entry of the top-level or-group shall be taken or if it should be picked at random. -1 is default and means random.

`Return Value`: Whether it was successful

#### Usage Example

```c#
var resolvedItems = new Dictionary<PickupIndex, int>();
bool success = ItemStringParser.ItemStringParser.ParseItemString("5xHoof & {Tier1 | Tier2}*3", resolvedItems, Logger);
if (success) {
    foreach (var (pickupIndex, itemAmount) in resolvedItems) {
        var pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
        var itemIndex = pickupDef.itemIndex;
        if (itemIndex != ItemIndex.None && itemAmount > 0)
        {
            inventory.GiveItemPermanent(itemIndex, itemAmount);
        }
    }
}
```

Equipment, drones and key items are also supported. `GivePickups` gives them all, see below.

Index can be used to retrieve the n-th entry of the root or-group. Example:

```c#
ItemStringParser.ItemStringParser.ParseItemString("Hoof | AlienHead", resolvedItems, Logger, 0); // results in "Hoof"
ItemStringParser.ItemStringParser.ParseItemString("Hoof | AlienHead", resolvedItems, Logger, 1); // results in "AlienHead"
ItemStringParser.ItemStringParser.ParseItemString("Hoof | AlienHead", resolvedItems, Logger, -1); // results in either "Hoof" or "AlienHead"
```

### GivePickups

Gives the resolved pickups to a character, like picking them up. Only call it on the host.

- Items are given first, so a Functional Coupler among them adds its equipment sets before any equipment is placed. Cursed Book pages are items too; the game hands out the Cursed Book on its own.
- Equipment fills the empty slots of all equipment slots and sets, e.g. both of MUL-T's slots. If there is no empty one, the first equipment replaces the active one, just like picking it up. Further equipment is not given.
- A key item replaces the current one.
- Drones and other pickups are granted the way the game grants them.

#### Signature

```c#
List<UniquePickup> ItemStringParser.ItemStringParser.GivePickups(CharacterBody body, Dictionary<PickupIndex, int> pickups)
```

`body (CharacterBody)`: The character receiving the pickups.

`pickups (Dictionary<PickupIndex, int>)`: The pickups and their amounts, e.g. from `ParseItemString`.

`Return Value`: The pickups that were replaced or not given, so you can drop them or hand them back.

#### Usage Example

```c#
var resolvedItems = new Dictionary<PickupIndex, int>();
if (ItemStringParser.ItemStringParser.ParseItemString("5xHoof & BFG & Tier1", resolvedItems, Logger))
{
    foreach (var leftOver in ItemStringParser.ItemStringParser.GivePickups(body, resolvedItems))
    {
        PickupDropletController.CreatePickupDroplet(leftOver, body.corePosition + Vector3.up * 1.5f, Vector3.up * 15f, false);
    }
}
```

### ResolveItemKey

This method resolves a single item key string into a pickup index. It supports parsing item keys that represent item tiers, drop tables, concrete items, or concrete equipment. Tiers and drop tables can be narrowed down with the `!`, `#` and `!#` operators. Other operators like `&`, `|` or `x` are not supported here, use `ParseItemString` for those.

#### Signature

```c#
PickupIndex ItemStringParser.ItemStringParser.ResolveItemKey(string itemkey, bool availableOnly = true)
```

`itemkey (string)`: The identifier string for the item or group of items to resolve. Tiers and droptables can exclude items with `!`, and only pick items with or without a tag with `#` and `!#`.

`availableOnly (bool, optional)`: If true will prevent concrete item names from being resolved if they are disabled or not yet unlocked. Names from a DLC that is not enabled for the run never resolve.

`Return Value`: The resolved pickupIndex.

`ArgumentException`: Thrown when itemkey can not be resolved to an item name, droptable or tier or when any of the blacklisted item names or tags could not be resolved.

#### Usage Example

```c#
try
{
    var pickupIndex = ItemStringParser.ResolveItemKey("Tier1!Crowbar"); // Tier1 items excluding Crowbar
    var healingItem = ItemStringParser.ResolveItemKey("Tier2#Healing"); // green healing items
}
catch(ArgumentException e)
{
    Logger.LogError(e.Message);
}
```

