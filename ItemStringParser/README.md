# ItemStringParser

This mod is a library for parsing ItemStrings. See bottom on the developer manual.

[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/E1E71PHUJ2)

## User Manual

ItemStrings are a way to describe a droptable using plain text. They let you describe what items should appear and how many in a simple, readable line. ItemStrings can include instructions for repeating items, choosing items randomly, or excluding certain items.

### ItemString Operators

ItemString operators are symbols that help you decide which items to give, repeat, or exclude in a list. They make it easy to write rules like "give this item", "skip that one", or "choose one randomly" using short codes. Each entry in the ItemString roughly follows the format:

`<repeat>x<itemkey>!<itemname>*<multiplier>: <weight>`

Entries can be separated by `&` or `|`. Entries can be grouped with `{` and `}`.

#### `!` (Not-Operator)
- **Purpose:** Blacklist items from tiers or droptables.
- **Placement:** Directly *after* the name of the droptable or tier.
- **Example:**
  - `Tier1!Hoof` - White item but not a Hoof
  - `dtChest1!Bear!Crowbar` - Content of a Small Chest but neither Tougher Times nor Crowbar.

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
- **Internal item names** (see [R2Wiki - Items-and-Equipments-Data](https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Developer-Reference/Items-and-Equipments-Data/)).  
- **Item Tier names**: `Tier1`, `Tier2`, `Tier3`, `Lunar`, `Boss`, `VoidTier1`, `VoidTier2`, `VoidTier3`, `VoidBoss`, `FoodTier`
- **Droptable names**: e.g. `dtChest1`, `dtLunarChest`, `dtVoidChest`. See below.

### Droptable Names

Here is a list of supported droptables:

| Drop Table Name                  | tier1 | tier2 | tier3 | boss | lunarEquipment | lunarItem | lunarCombined | equipment | voidTier1 | voidTier2 | voidTier3 | voidBoss | foodTier | powerShapes | canDropBeReplaced | requiredItemTags | bannedItemTags |
|----------------------------------|-------|-------|-------|------|----------------|-----------|---------------|-----------|-----------|-----------|-----------|----------|----------|-------------|-------------------|------------------|----------------|
| dtCasinoChest                    | 0.7 | 0.3 | 0.01 | 0 | 0 | 0 | 0 | 0.1 | 0 | 0 | 0 | 0 | 0 | 0 | True |  |  |
| dtSmallChestDamage               | 0.8 | 0.2 | 0.01 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | True | Damage |  |
| dtSmallChestHealing              | 0.8 | 0.2 | 0.01 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | True | Healing |  |
| dtSmallChestUtility              | 0.8 | 0.2 | 0.01 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | True | Utility |  |
| dtChest1                         | 0.8 | 0.2 | 0.01 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | True |  |  |
| dtChest2                         | 0 | 0.8 | 0.2 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | True |  |  |
| dtAISafeTier1Item                | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | True |  | AIBlacklist, SprintRelated |
| dtAISafeTier2Item                | 0 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | True |  | AIBlacklist, SprintRelated |
| dtAISafeTier3Item                | 0 | 0 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | True |  | AIBlacklist, SprintRelated |
| dtEquipment                      | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | True |  |  |
| dtTier1Item                      | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | True |  |  |
| dtTier2Item                      | 0 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | True |  |  |
| dtTier3Item                      | 0 | 0 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | True |  |  |
| dtVoidChest                      | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 6 | 3 | 1 | 0 | 0 | 0 | True |  |  |
| dtDuplicatorTier1                | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | True |  | CannotDuplicate |
| dtDuplicatorTier2                | 0 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | True |  | CannotDuplicate |
| dtDuplicatorTier3                | 0 | 0 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | True |  | CannotDuplicate |
| dtDuplicatorWild                 | 0 | 0 | 0 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | True |  | WorldUnique |
| dtGoldChest                      | 0 | 0 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | True |  |  |
| dtLunarChest                     | 0 | 0 | 0 | 0 | 0 | 0 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | True |  |  |
| dtMonsterTeamTier1Item           | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | True |  | AIBlacklist, OnKillEffect, EquipmentRelated, SprintRelated |
| dtMonsterTeamTier2Item           | 0 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | True |  | AIBlacklist, OnKillEffect, EquipmentRelated, SprintRelated |
| dtMonsterTeamTier3Item           | 0 | 0 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | True |  | AIBlacklist, OnKillEffect, EquipmentRelated, SprintRelated |
| dtSacrificeArtifact              | 0.7 | 0.3 | 0.01 | 0 | 0 | 0 | 0 | 0.1 | 0 | 0 | 0 | 0 | 0 | 0 | True |  | SacrificeBlacklist |
| dtShrineChance                   | 8 | 2 | 0.2 | 0 | 0 | 0 | 0 | 2 | 0 | 0 | 0 | 0 | 0 | 0 | True |  |  |
| dtLockbox                        | 0 | 4 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | True |  |  |
| dtCommandChest                   | 0.2 | 0.2 | 0.05 | 0.05 | 0 | 0 | 0 | 0.2 | 0.1 | 0.1 | 0.05 | 0.05 | 0 | 0 | True |  | Any, Any, Any, Any, Any |
| dtCategoryChest2Damage           | 0 | 0.8 | 0.2 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | True | Damage |  |
| dtCategoryChest2Healing          | 0 | 0.8 | 0.2 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | True | Healing |  |
| dtCategoryChest2Utility          | 0 | 0.8 | 0.2 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | True | Utility |  |
| dtITBossWave                     | 0 | 80 | 7.5 | 7.5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | True |  |  |
| dtITDefaultWave                  | 80 | 10 | 0.25 | 0.25 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | True |  |  |
| dtITLunar                        | 0 | 0 | 0 | 0 | 0 | 0 | 100 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | True |  |  |
| dtITSpecialBossWave              | 0 | 0 | 80 | 20 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | True |  |  |
| dtITVoid                         | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 80 | 20 | 1 | 0 | 0 | 0 | True |  |  |
| dtVoidLockbox                    | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 5 | 5 | 2 | 0 | 0 | 0 | True |  |  |
| dtVoidCamp                       | 40 | 40 | 10 | 3 | 0 | 0 | 0 | 0 | 5.714286 | 5.714286 | 1.25 | 0 | 0 | 0 | True |  |  |
| dtVoidTriple                     | 0.8 | 0.2 | 0.01 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | True |  |  |
| AurelioniteHeartPickupDropTable  | 0 | 0 | 0.4 | 0.6 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | True |  |  |
| dtShrineHalcyoniteTier1          | 0.65 | 0.3 | 0.05 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | True |  |  |
| dtShrineHalcyoniteTier2          | 0.65 | 0.3 | 0.05 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | True | HalcyoniteShrine |  |
| dtShrineHalcyoniteTier3          | 0.65 | 0.3 | 0.05 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | True |  |  |
| GeodeRewardDropTable             | 0.8 | 0.2 | 0.01 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | True |  |  |
| dtChanceDoll                     | 0 | 0.79 | 0.2 | 0.01 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | True |  |  |
| dtSonorousEcho                   | 0.9 | 0.1 | 0.001 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | True |  |  |
| dtSalvage                        | 0.75 | 0.25 | 0.05 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | False | CanBeTemporary |  |
| dtDrifterBagChest                | 0.8 | 0.2 | 0.01 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | True |  |  |
| dtJunkDrone                      | 0.7 | 0.25 | 0.05 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | False | CanBeTemporary |  |
| dtSolusHeart                     | 0 | 0 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | True | Technology | WorldUnique, CannotCopy, FoodRelated, ObjectiveRelated |
| dtTemporaryItemsDistributor      | 0.8 | 0.2 | 0.01 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | False | CanBeTemporary |  |

### EBNF

You can use this EBNF to validate your item string for example at [PaulKlineLabs' BNF Playground](https://bnfplayground.pauliankline.com/?bnf=%3CItemString%3E%20%20%20%20%20%20%20%3A%3A%3D%20%3CAND_LIST%3E%20%7C%20%3COR_LIST%3E%0A%3CAND_LIST%3E%20%20%20%20%20%20%20%3A%3A%3D%20%3CENTRY%3E%20(%22%20%26%20%22%20%3CENTRY%3E)*%0A%3COR_LIST%3E%20%20%20%20%20%20%20%20%3A%3A%3D%20%3CWEIGHTED_ENTRY%3E%20(%22%20%7C%20%22%20%3CWEIGHTED_ENTRY%3E)*%0A%3CENTRY%3E%20%20%20%20%20%20%20%20%20%20%3A%3A%3D%20(%3Crep%3E%20%22x%22)%3F%20%3CITEM_KEY%3E%20(%22*%22%20%3Cmult%3E)%3F%0A%3CWEIGHTED_ENTRY%3E%20%3A%3A%3D%20%3CENTRY%3E%20(%22%3A%20%22%20%3Cweight%3E)%3F%0A%3CITEM_KEY%3E%20%20%20%20%20%20%20%3A%3A%3D%20%3Citemname%3E%20%7C%20%3Ctier%3E%20(%22!%22%20%3Citemname%3E)*%20%7C%20%3Cdroptable%3E%20(%22!%22%20%3Citemname%3E)*%20%7C%20%22%7B%20%22%20%3CAND_LIST%3E%20%22%20%7D%22%20%7C%20%22%7B%20%22%20%3COR_LIST%3E%20%22%20%7D%22%0A%0A%3Citemname%3E%20%20%3A%3A%3D%20%22AACannon%22%20%7C%20%22AdaptiveArmor%22%20%7C%20%22AlienHead%22%20%7C%20%22ArmorPlate%22%20%7C%20%22ArmorReductionOnHit%22%20%7C%20%22AttackSpeedAndMoveSpeed%22%20%7C%20%22AttackSpeedOnCrit%22%20%7C%20%22AttackSpeedPerNearbyAllyOrEnemy%22%20%7C%20%22AutoCastEquipment%22%20%7C%20%22Bandolier%22%20%7C%20%22BarrageOnBoss%22%20%7C%20%22BarrierOnCooldown%22%20%7C%20%22BarrierOnKill%22%20%7C%20%22BarrierOnOverHeal%22%20%7C%20%22Bear%22%20%7C%20%22BearVoid%22%20%7C%20%22BeetleGland%22%20%7C%20%22Behemoth%22%20%7C%20%22BleedOnHit%22%20%7C%20%22BleedOnHitAndExplode%22%20%7C%20%22BleedOnHitVoid%22%20%7C%20%22BonusGoldPackOnKill%22%20%7C%20%22BonusHealthBoost%22%20%7C%20%22BoostAllStats%22%20%7C%20%22BoostAttackSpeed%22%20%7C%20%22BoostDamage%22%20%7C%20%22BoostEquipmentRecharge%22%20%7C%20%22BoostHp%22%20%7C%20%22BossDamageBonus%22%20%7C%20%22BounceNearby%22%20%7C%20%22BurnNearby%22%20%7C%20%22ChainLightning%22%20%7C%20%22ChainLightningVoid%22%20%7C%20%22Clover%22%20%7C%20%22CloverVoid%22%20%7C%20%22ConvertCritChanceToCritDamage%22%20%7C%20%22CooldownOnCrit%22%20%7C%20%22CrippleWardOnLevel%22%20%7C%20%22CritAtLowerElevation%22%20%7C%20%22CritDamage%22%20%7C%20%22CritGlasses%22%20%7C%20%22CritGlassesVoid%22%20%7C%20%22CritHeal%22%20%7C%20%22Crowbar%22%20%7C%20%22CutHp%22%20%7C%20%22Dagger%22%20%7C%20%22DeathMark%22%20%7C%20%22DelayedDamage%22%20%7C%20%22DestructibleSpawner%22%20%7C%20%22DrizzlePlayerHelper%22%20%7C%20%22DroneDynamiteDisplay%22%20%7C%20%22DroneUpgradeHidden%22%20%7C%20%22DroneWeapons%22%20%7C%20%22DroneWeaponsBoost%22%20%7C%20%22DroneWeaponsDisplay1%22%20%7C%20%22DroneWeaponsDisplay2%22%20%7C%20%22DronesDropDynamite%22%20%7C%20%22Duplicator%22%20%7C%20%22ElementalRingVoid%22%20%7C%20%22EmpowerAlways%22%20%7C%20%22EnergizedOnEquipmentUse%22%20%7C%20%22EquipmentMagazine%22%20%7C%20%22EquipmentMagazineVoid%22%20%7C%20%22ExecuteLowHealthElite%22%20%7C%20%22ExplodeOnDeath%22%20%7C%20%22ExplodeOnDeathVoid%22%20%7C%20%22ExtraEquipment%22%20%7C%20%22ExtraLife%22%20%7C%20%22ExtraLifeConsumed%22%20%7C%20%22ExtraLifeVoid%22%20%7C%20%22ExtraLifeVoidConsumed%22%20%7C%20%22ExtraShrineItem%22%20%7C%20%22ExtraStatsOnLevelUp%22%20%7C%20%22FallBoots%22%20%7C%20%22Feather%22%20%7C%20%22FireRing%22%20%7C%20%22FireballsOnHit%22%20%7C%20%22Firework%22%20%7C%20%22FlatHealth%22%20%7C%20%22FocusConvergence%22%20%7C%20%22FragileDamageBonus%22%20%7C%20%22FragileDamageBonusConsumed%22%20%7C%20%22FreeChest%22%20%7C%20%22Ghost%22%20%7C%20%22GhostOnKill%22%20%7C%20%22GoldOnHit%22%20%7C%20%22GoldOnHurt%22%20%7C%20%22GummyCloneIdentifier%22%20%7C%20%22HalfAttackSpeedHalfCooldowns%22%20%7C%20%22HalfSpeedDoubleHealth%22%20%7C%20%22HeadHunter%22%20%7C%20%22HealOnCrit%22%20%7C%20%22HealWhileSafe%22%20%7C%20%22HealingPotion%22%20%7C%20%22HealingPotionConsumed%22%20%7C%20%22HealthDecay%22%20%7C%20%22Hoof%22%20%7C%20%22IceRing%22%20%7C%20%22Icicle%22%20%7C%20%22IgniteOnKill%22%20%7C%20%22ImmuneToDebuff%22%20%7C%20%22IncreaseDamageOnMultiKill%22%20%7C%20%22IncreaseHealing%22%20%7C%20%22IncreasePrimaryDamage%22%20%7C%20%22Incubator%22%20%7C%20%22Infusion%22%20%7C%20%22InvadingDoppelganger%22%20%7C%20%22ItemDropChanceOnKill%22%20%7C%20%22JumpBoost%22%20%7C%20%22JumpDamageStrike%22%20%7C%20%22Junk%22%20%7C%20%22KillEliteFrenzy%22%20%7C%20%22KnockBackHitEnemies%22%20%7C%20%22Knurl%22%20%7C%20%22LaserTurbine%22%20%7C%20%22LemurianHarness%22%20%7C%20%22LevelBonus%22%20%7C%20%22LightningStrikeOnHit%22%20%7C%20%22LowerPricedChests%22%20%7C%20%22LowerPricedChestsConsumed%22%20%7C%20%22LunarBadLuck%22%20%7C%20%22LunarDagger%22%20%7C%20%22LunarPrimaryReplacement%22%20%7C%20%22LunarSecondaryReplacement%22%20%7C%20%22LunarSpecialReplacement%22%20%7C%20%22LunarSun%22%20%7C%20%22LunarTrinket%22%20%7C%20%22LunarUtilityReplacement%22%20%7C%20%22LunarWings%22%20%7C%20%22MageAttunement%22%20%7C%20%22Medkit%22%20%7C%20%22MeteorAttackOnHighDamage%22%20%7C%20%22MinHealthPercentage%22%20%7C%20%22MinionLeash%22%20%7C%20%22MinorConstructOnKill%22%20%7C%20%22Missile%22%20%7C%20%22MissileVoid%22%20%7C%20%22MoneyLoan%22%20%7C%20%22MonsoonPlayerHelper%22%20%7C%20%22MonstersOnShrineUse%22%20%7C%20%22MoreMissile%22%20%7C%20%22MoveSpeedOnKill%22%20%7C%20%22Mushroom%22%20%7C%20%22MushroomVoid%22%20%7C%20%22NearbyDamageBonus%22%20%7C%20%22NovaOnHeal%22%20%7C%20%22NovaOnLowHealth%22%20%7C%20%22OnLevelUpFreeUnlock%22%20%7C%20%22OutOfCombatArmor%22%20%7C%20%22ParentEgg%22%20%7C%20%22PermanentDebuffOnHit%22%20%7C%20%22PersonalShield%22%20%7C%20%22Phasing%22%20%7C%20%22PhysicsProjectile%22%20%7C%20%22Plant%22%20%7C%20%22PlantOnHit%22%20%7C%20%22PlasmaCore%22%20%7C%20%22PrimarySkillShuriken%22%20%7C%20%22RandomDamageZone%22%20%7C%20%22RandomEquipmentTrigger%22%20%7C%20%22RandomlyLunar%22%20%7C%20%22RegeneratingScrap%22%20%7C%20%22RegeneratingScrapConsumed%22%20%7C%20%22RepeatHeal%22%20%7C%20%22RoboBallBuddy%22%20%7C%20%22SecondarySkillMagazine%22%20%7C%20%22Seed%22%20%7C%20%22SharedSuffering%22%20%7C%20%22ShieldBooster%22%20%7C%20%22ShieldOnly%22%20%7C%20%22ShockDamageAura%22%20%7C%20%22ShockNearby%22%20%7C%20%22SiphonOnLowHealth%22%20%7C%20%22SkullCounter%22%20%7C%20%22SlowOnHit%22%20%7C%20%22SlowOnHitVoid%22%20%7C%20%22SpeedBoostPickup%22%20%7C%20%22SpeedOnPickup%22%20%7C%20%22SprintArmor%22%20%7C%20%22SprintBonus%22%20%7C%20%22SprintOutOfCombat%22%20%7C%20%22SprintWisp%22%20%7C%20%22Squid%22%20%7C%20%22StatsFromScrap%22%20%7C%20%22StickyBomb%22%20%7C%20%22StrengthenBurn%22%20%7C%20%22StunAndPierce%22%20%7C%20%22StunChanceOnHit%22%20%7C%20%22Syringe%22%20%7C%20%22TPHealingNova%22%20%7C%20%22Talisman%22%20%7C%20%22TeamSizeDamageBonus%22%20%7C%20%22TeleportOnLowHealth%22%20%7C%20%22TeleportOnLowHealthConsumed%22%20%7C%20%22TeleportWhenOob%22%20%7C%20%22TempestOnKill%22%20%7C%20%22Thorns%22%20%7C%20%22TonicAffliction%22%20%7C%20%22Tooth%22%20%7C%20%22TransferDebuffOnHit%22%20%7C%20%22TreasureCache%22%20%7C%20%22TreasureCacheVoid%22%20%7C%20%22TriggerEnemyDebuffs%22%20%7C%20%22UseAmbientLevel%22%20%7C%20%22UtilitySkillMagazine%22%20%7C%20%22VoidMegaCrabItem%22%20%7C%20%22VoidmanPassiveItem%22%20%7C%20%22WarCryOnCombat%22%20%7C%20%22WarCryOnMultiKill%22%20%7C%20%22WardOnLevel%22%20%7C%20%22BFG%22%20%7C%20%22Blackhole%22%20%7C%20%22BossHunter%22%20%7C%20%22BurnNearby%22%20%7C%20%22Cleanse%22%20%7C%20%22CommandMissile%22%20%7C%20%22CrippleWard%22%20%7C%20%22CritOnUse%22%20%7C%20%22DeathProjectile%22%20%7C%20%22DroneBackup%22%20%7C%20%22FireBallDash%22%20%7C%20%22Fruit%22%20%7C%20%22GainArmor%22%20%7C%20%22Gateway%22%20%7C%20%22GoldGat%22%20%7C%20%22GummyClone%22%20%7C%20%22HealAndRevive%22%20%7C%20%22Jetpack%22%20%7C%20%22LifestealOnHit%22%20%7C%20%22Lightning%22%20%7C%20%22Meteor%22%20%7C%20%22Molotov%22%20%7C%20%22MultiShopCard%22%20%7C%20%22Parry%22%20%7C%20%22PassiveHealing%22%20%7C%20%22Recycle%22%20%7C%20%22Saw%22%20%7C%20%22Scanner%22%20%7C%20%22TeamWarCry%22%20%7C%20%22Tonic%22%20%7C%20%22VendingMachine%22%0A%3Cdroptable%3E%20%3A%3A%3D%20%22dtCasinoChest%22%20%7C%20%22dtSmallChestDamage%22%20%7C%20%22dtSmallChestHealing%22%20%7C%20%22dtSmallChestUtility%22%20%7C%20%22dtChest1%22%20%7C%20%22dtChest2%22%20%7C%20%22dtAISafeTier1Item%22%20%7C%20%22dtAISafeTier2Item%22%20%7C%20%22dtAISafeTier3Item%22%20%7C%20%22dtEquipment%22%20%7C%20%22dtTier1Item%22%20%7C%20%22dtTier2Item%22%20%7C%20%22dtTier3Item%22%20%7C%20%22dtVoidChest%22%20%7C%20%22dtDuplicatorTier1%22%20%7C%20%22dtDuplicatorTier2%22%20%7C%20%22dtDuplicatorTier3%22%20%7C%20%22dtDuplicatorWild%22%20%7C%20%22dtGoldChest%22%20%7C%20%22dtLunarChest%22%20%7C%20%22dtMonsterTeamTier1Item%22%20%7C%20%22dtMonsterTeamTier2Item%22%20%7C%20%22dtMonsterTeamTier3Item%22%20%7C%20%22dtSacrificeArtifact%22%20%7C%20%22dtShrineChance%22%20%7C%20%22dtLockbox%22%20%7C%20%22dtCommandChest%22%20%7C%20%22dtCategoryChest2Damage%22%20%7C%20%22dtCategoryChest2Healing%22%20%7C%20%22dtCategoryChest2Utility%22%20%7C%20%22dtITBossWave%22%20%7C%20%22dtITDefaultWave%22%20%7C%20%22dtITLunar%22%20%7C%20%22dtITSpecialBossWave%22%20%7C%20%22dtITVoid%22%20%7C%20%22dtVoidLockbox%22%20%7C%20%22dtVoidCamp%22%20%7C%20%22dtVoidTriple%22%20%7C%20%22AurelioniteHeartPickupDropTable%22%20%7C%20%22dtShrineHalcyoniteTier1%22%20%7C%20%22dtShrineHalcyoniteTier2%22%20%7C%20%22dtShrineHalcyoniteTier3%22%20%7C%20%22GeodeRewardDropTable%22%20%7C%20%22dtChanceDoll%22%20%7C%20%22dtSonorousEcho%22%20%7C%20%22dtSalvage%22%20%7C%20%22dtDrifterBagChest%22%20%7C%20%22dtJunkDrone%22%20%7C%20%22dtSolusHeart%22%20%7C%20%22dtTemporaryItemsDistributor%22%0A%3Ctier%3E%20%20%20%20%20%20%3A%3A%3D%20%22Tier1%22%20%7C%20%22Tier2%22%20%7C%20%22Tier3%22%20%7C%20%22Lunar%22%20%7C%20%22Boss%22%20%7C%20%22VoidTier1%22%20%7C%20%22VoidTier2%22%20%7C%20%22VoidTier3%22%20%7C%20%22VoidBoss%22%0A%3Crep%3E%20%20%20%20%20%20%20%3A%3A%3D%20%3Cint%3E%0A%3Cmult%3E%20%20%20%20%20%20%3A%3A%3D%20%3Cint%3E%0A%3Cweight%3E%20%20%20%20%3A%3A%3D%20%3Cfloat%3E%0A%0A%3Cint%3E%20%20%20%3A%3A%3D%20%5B1-9%5D%20%5B0-9%5D*%0A%3Cfloat%3E%20%3A%3A%3D%20(%220%22%20%7C%20%3Cint%3E)%20(%22.%22%20%5B0-9%5D%2B%20)%3F&name=Risk%20of%20Rain%202%20ItemStringParser).

```
<ItemList>       ::= <AND_LIST> | <OR_LIST>
<AND_LIST>       ::= <ENTRY> (" & " <ENTRY>)*
<OR_LIST>        ::= <WEIGHTED_ENTRY> (" | " <WEIGHTED_ENTRY>)*
<ENTRY>          ::= (<rep> "x")? <ITEM_KEY> ("*" <mult>)?
<WEIGHTED_ENTRY> ::= <ENTRY> (": " <weight>)?
<ITEM_KEY>       ::= <itemname> | <tier> ("!" <itemname>)* | <droptable> ("!" <itemname>)* | "{ " <AND_LIST> " }" | "{ " <OR_LIST> " }"

<itemname>  ::= "AACannon" | "AdaptiveArmor" | "AlienHead" | "ArmorPlate" | "ArmorReductionOnHit" | "AttackSpeedAndMoveSpeed" | "AttackSpeedOnCrit" | "AttackSpeedPerNearbyAllyOrEnemy" | "AutoCastEquipment" | "Bandolier" | "BarrageOnBoss" | "BarrierOnCooldown" | "BarrierOnKill" | "BarrierOnOverHeal" | "Bear" | "BearVoid" | "BeetleGland" | "Behemoth" | "BleedOnHit" | "BleedOnHitAndExplode" | "BleedOnHitVoid" | "BonusGoldPackOnKill" | "BonusHealthBoost" | "BoostAllStats" | "BoostAttackSpeed" | "BoostDamage" | "BoostEquipmentRecharge" | "BoostHp" | "BossDamageBonus" | "BounceNearby" | "BurnNearby" | "ChainLightning" | "ChainLightningVoid" | "Clover" | "CloverVoid" | "ConvertCritChanceToCritDamage" | "CooldownOnCrit" | "CrippleWardOnLevel" | "CritAtLowerElevation" | "CritDamage" | "CritGlasses" | "CritGlassesVoid" | "CritHeal" | "Crowbar" | "CutHp" | "Dagger" | "DeathMark" | "DelayedDamage" | "DestructibleSpawner" | "DrizzlePlayerHelper" | "DroneDynamiteDisplay" | "DroneUpgradeHidden" | "DroneWeapons" | "DroneWeaponsBoost" | "DroneWeaponsDisplay1" | "DroneWeaponsDisplay2" | "DronesDropDynamite" | "Duplicator" | "ElementalRingVoid" | "EmpowerAlways" | "EnergizedOnEquipmentUse" | "EquipmentMagazine" | "EquipmentMagazineVoid" | "ExecuteLowHealthElite" | "ExplodeOnDeath" | "ExplodeOnDeathVoid" | "ExtraEquipment" | "ExtraLife" | "ExtraLifeConsumed" | "ExtraLifeVoid" | "ExtraLifeVoidConsumed" | "ExtraShrineItem" | "ExtraStatsOnLevelUp" | "FallBoots" | "Feather" | "FireRing" | "FireballsOnHit" | "Firework" | "FlatHealth" | "FocusConvergence" | "FragileDamageBonus" | "FragileDamageBonusConsumed" | "FreeChest" | "Ghost" | "GhostOnKill" | "GoldOnHit" | "GoldOnHurt" | "GummyCloneIdentifier" | "HalfAttackSpeedHalfCooldowns" | "HalfSpeedDoubleHealth" | "HeadHunter" | "HealOnCrit" | "HealWhileSafe" | "HealingPotion" | "HealingPotionConsumed" | "HealthDecay" | "Hoof" | "IceRing" | "Icicle" | "IgniteOnKill" | "ImmuneToDebuff" | "IncreaseDamageOnMultiKill" | "IncreaseHealing" | "IncreasePrimaryDamage" | "Incubator" | "Infusion" | "InvadingDoppelganger" | "ItemDropChanceOnKill" | "JumpBoost" | "JumpDamageStrike" | "Junk" | "KillEliteFrenzy" | "KnockBackHitEnemies" | "Knurl" | "LaserTurbine" | "LemurianHarness" | "LevelBonus" | "LightningStrikeOnHit" | "LowerPricedChests" | "LowerPricedChestsConsumed" | "LunarBadLuck" | "LunarDagger" | "LunarPrimaryReplacement" | "LunarSecondaryReplacement" | "LunarSpecialReplacement" | "LunarSun" | "LunarTrinket" | "LunarUtilityReplacement" | "LunarWings" | "MageAttunement" | "Medkit" | "MeteorAttackOnHighDamage" | "MinHealthPercentage" | "MinionLeash" | "MinorConstructOnKill" | "Missile" | "MissileVoid" | "MoneyLoan" | "MonsoonPlayerHelper" | "MonstersOnShrineUse" | "MoreMissile" | "MoveSpeedOnKill" | "Mushroom" | "MushroomVoid" | "NearbyDamageBonus" | "NovaOnHeal" | "NovaOnLowHealth" | "OnLevelUpFreeUnlock" | "OutOfCombatArmor" | "ParentEgg" | "PermanentDebuffOnHit" | "PersonalShield" | "Phasing" | "PhysicsProjectile" | "Plant" | "PlantOnHit" | "PlasmaCore" | "PrimarySkillShuriken" | "RandomDamageZone" | "RandomEquipmentTrigger" | "RandomlyLunar" | "RegeneratingScrap" | "RegeneratingScrapConsumed" | "RepeatHeal" | "RoboBallBuddy" | "SecondarySkillMagazine" | "Seed" | "SharedSuffering" | "ShieldBooster" | "ShieldOnly" | "ShockDamageAura" | "ShockNearby" | "SiphonOnLowHealth" | "SkullCounter" | "SlowOnHit" | "SlowOnHitVoid" | "SpeedBoostPickup" | "SpeedOnPickup" | "SprintArmor" | "SprintBonus" | "SprintOutOfCombat" | "SprintWisp" | "Squid" | "StatsFromScrap" | "StickyBomb" | "StrengthenBurn" | "StunAndPierce" | "StunChanceOnHit" | "Syringe" | "TPHealingNova" | "Talisman" | "TeamSizeDamageBonus" | "TeleportOnLowHealth" | "TeleportOnLowHealthConsumed" | "TeleportWhenOob" | "TempestOnKill" | "Thorns" | "TonicAffliction" | "Tooth" | "TransferDebuffOnHit" | "TreasureCache" | "TreasureCacheVoid" | "TriggerEnemyDebuffs" | "UseAmbientLevel" | "UtilitySkillMagazine" | "VoidMegaCrabItem" | "VoidmanPassiveItem" | "WarCryOnCombat" | "WarCryOnMultiKill" | "WardOnLevel" | "BFG" | "Blackhole" | "BossHunter" | "BurnNearby" | "Cleanse" | "CommandMissile" | "CrippleWard" | "CritOnUse" | "DeathProjectile" | "DroneBackup" | "FireBallDash" | "Fruit" | "GainArmor" | "Gateway" | "GoldGat" | "GummyClone" | "HealAndRevive" | "Jetpack" | "LifestealOnHit" | "Lightning" | "Meteor" | "Molotov" | "MultiShopCard" | "Parry" | "PassiveHealing" | "Recycle" | "Saw" | "Scanner" | "TeamWarCry" | "Tonic" | "VendingMachine"
<droptable> ::= "dtCasinoChest" | "dtSmallChestDamage" | "dtSmallChestHealing" | "dtSmallChestUtility" | "dtChest1" | "dtChest2" | "dtAISafeTier1Item" | "dtAISafeTier2Item" | "dtAISafeTier3Item" | "dtEquipment" | "dtTier1Item" | "dtTier2Item" | "dtTier3Item" | "dtVoidChest" | "dtDuplicatorTier1" | "dtDuplicatorTier2" | "dtDuplicatorTier3" | "dtDuplicatorWild" | "dtGoldChest" | "dtLunarChest" | "dtMonsterTeamTier1Item" | "dtMonsterTeamTier2Item" | "dtMonsterTeamTier3Item" | "dtSacrificeArtifact" | "dtShrineChance" | "dtLockbox" | "dtCommandChest" | "dtCategoryChest2Damage" | "dtCategoryChest2Healing" | "dtCategoryChest2Utility" | "dtITBossWave" | "dtITDefaultWave" | "dtITLunar" | "dtITSpecialBossWave" | "dtITVoid" | "dtVoidLockbox" | "dtVoidCamp" | "dtVoidTriple" | "AurelioniteHeartPickupDropTable" | "dtShrineHalcyoniteTier1" | "dtShrineHalcyoniteTier2" | "dtShrineHalcyoniteTier3" | "GeodeRewardDropTable" | "dtChanceDoll" | "dtSonorousEcho" | "dtSalvage" | "dtDrifterBagChest" | "dtJunkDrone" | "dtSolusHeart" | "dtTemporaryItemsDistributor"
<tier>      ::= "Tier1" | "Tier2" | "Tier3" | "Lunar" | "Boss" | "VoidTier1" | "VoidTier2" | "VoidTier3" | "VoidBoss"
<rep>       ::= <int>
<mult>      ::= <int>
<weight>    ::= <float>

<int>   ::= [1-9] [0-9]*
<float> ::= ("0" | <int>) ("." [0-9]+ )?
```

Or use this one if you have modded items:

```
<ItemList>       ::= <AND_LIST> | <OR_LIST>
<AND_LIST>       ::= <ENTRY> (" & " <ENTRY>)*
<OR_LIST>        ::= <WEIGHTED_ENTRY> (" | " <WEIGHTED_ENTRY>)*
<ENTRY>          ::= (<rep> "x")? <ITEM_KEY> ("*" <mult>)?
<WEIGHTED_ENTRY> ::= <ENTRY> (": " <weight>)?
<ITEM_KEY>       ::= <itemname> | <tier> ("!" <itemname>)* | <droptable> ("!" <itemname>)* | "{ " <AND_LIST> " }" | "{ " <OR_LIST> " }"

<itemname>  ::= [a-z]+
<droptable> ::= "dtCasinoChest" | "dtSmallChestDamage" | "dtSmallChestHealing" | "dtSmallChestUtility" | "dtChest1" | "dtChest2" | "dtAISafeTier1Item" | "dtAISafeTier2Item" | "dtAISafeTier3Item" | "dtEquipment" | "dtTier1Item" | "dtTier2Item" | "dtTier3Item" | "dtVoidChest" | "dtDuplicatorTier1" | "dtDuplicatorTier2" | "dtDuplicatorTier3" | "dtDuplicatorWild" | "dtGoldChest" | "dtLunarChest" | "dtMonsterTeamTier1Item" | "dtMonsterTeamTier2Item" | "dtMonsterTeamTier3Item" | "dtSacrificeArtifact" | "dtShrineChance" | "dtLockbox" | "dtCommandChest" | "dtCategoryChest2Damage" | "dtCategoryChest2Healing" | "dtCategoryChest2Utility" | "dtITBossWave" | "dtITDefaultWave" | "dtITLunar" | "dtITSpecialBossWave" | "dtITVoid" | "dtVoidLockbox" | "dtVoidCamp" | "dtVoidTriple" | "AurelioniteHeartPickupDropTable" | "dtShrineHalcyoniteTier1" | "dtShrineHalcyoniteTier2" | "dtShrineHalcyoniteTier3" | "GeodeRewardDropTable" | "dtChanceDoll" | "dtSonorousEcho" | "dtSalvage" | "dtDrifterBagChest" | "dtJunkDrone" | "dtSolusHeart" | "dtTemporaryItemsDistributor"
<tier>      ::= "Tier1" | "Tier2" | "Tier3" | "Lunar" | "Boss" | "VoidTier1" | "VoidTier2" | "VoidTier3" | "VoidBoss"
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
3. Add dependency string `Def-ItemStringParser-1.0.0` to the dependencies list in your `manifest.json`

It provides two public methods: (1) `ParseItemString` and (2) `ResolveItemKey`

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

`availableOnly (bool, optional)`: If true will prevent concrete item names from being resolved if they are disabled or not yet unlocked.

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

Equipments are also supported. Example:

```c#
var inventory = master.inventory;
Dictionary<PickupIndex, int> itemsToGive = new Dictionary<PickupIndex, int>();
bool success = ItemStringParser.ItemStringParser.ParseItemString(itemString, itemsToGive, Logger);
if(success) {
    uint equipIndex = 0;
    foreach (var (pickupIndex, itemAmount) in itemsToGive)
    {
        var pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
        // handle items
        var itemIndex = pickupDef.itemIndex;
        if (itemIndex != ItemIndex.None && itemAmount > 0)
        {
            inventory.GiveItemPermanent(itemIndex, itemAmount);
        }
        // handle equipments
        int maxEquipmentSlots = master.bodyPrefab.name == "ToolbotBody" ? 2 : 1;
        int maxEquipmentSets = master.inventory.GetItemCountEffective(DLC3Content.Items.ExtraEquipment.itemIndex) + 1;
        int maxEquipmentCount = maxEquipmentSlots * maxEquipmentSets;
        var equipmentCount = itemAmount;
        var equipmentIndex = pickupDef.equipmentIndex;
        while (equipmentIndex != EquipmentIndex.None && equipmentCount > 0 && equipIndex < maxEquipmentCount)
        {
            uint slot = (uint)(equipIndex % maxEquipmentSlots);
            uint set = (uint)(equipIndex / maxEquipmentSlots);
            inventory.SetEquipmentIndexForSlot(equipmentIndex, slot, set);
            equipmentCount--;
            equipIndex++;
        }
    }
}
```

Index can be used to retrieve the n-th entry of the root or-group. Example:

```c#
ItemStringParser.ItemStringParser.ParseItemString("Hoof | AlienHead", resolvedItems, Logger, 0); // results in "Hoof"
ItemStringParser.ItemStringParser.ParseItemString("Hoof | AlienHead", resolvedItems, Logger, 1); // results in "AlienHead"
ItemStringParser.ItemStringParser.ParseItemString("Hoof | AlienHead", resolvedItems, Logger, -1); // results in either "Hoof" or "AlienHead"
```

### ResolveItemKey

This method resolves a single item key string into a pickup index. It supports parsing item keys that represent item tiers, drop tables, concrete items, or concrete equipment. It also supports blacklisting certain items from selection. As such this method only supports the `!`-operator, but no other operator.

#### Signature

```c#
bool ItemStringParser.ItemStringParser.ResolveItemKey(string itemkey, int repeat, Dictionary<PickupIndex, int> resolvedItems, ManualLogSource log)
```

`itemkey (string)`: The identifier string for the item or group of items to resolve. May exclude items from droptables or tiers with with `!`.

`availableOnly (bool, optional)`: If true will prevent concrete item names from being resolved if they are disabled or not yet unlocked.

`Return Value`: The resolved pickupIndex.

`ArgumentException`: Thrown when itemkey can not be resolved to an item name, droptable or tier or when any of the blacklisted item names could not be resolved.

#### Usage Example

```c#
try
{
    var pickupIndex = ItemStringParser.ResolveItemKey("Tier1!Crowbar"); // Tier1 items excluding Crowbar
}
catch(ArgumentException e)
{
    Logger.LogError(e.Message);
}
```

