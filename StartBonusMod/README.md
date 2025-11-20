# StartBonusMod

This mod gives every player a configurable set of items and starting cash at the beginning of a run. 
I made it so that I can use [StartInBazaar](https://thunderstore.io/package/MagnusMagnuson/StartInBazaar/) in combination with [BiggerBazaar](https://thunderstore.io/package/MagnusMagnuson/BiggerBazaar/) and/or [BazaarIsMyHaven](https://thunderstore.io/package/Def/BazaarIsMyHaven/).
This mod is also compatible with [InLobbyConfig](https://thunderstore.io/package/KingEnderBrine/InLobbyConfig/).

## Simple Start Bonus Items

- Simply choose for each tier which starting item and how many the player shall start with. 
- There is also the option for a random item.
- Easiest to configure this is using InLobbyConfig.

## Advanced Start Bonus Item List

A more involved item list which gives you powerful operators to really customize the item list you want to start with.

- `ItemList`:
  - Defines what items players start with.
  - Items can be individual names, tiers, drop tables, or grouped sets with operators.
  - Separate item groups into AND lists (`&` operator) or OR lists (`|` operator), which cannot mix at the same level.
	- Example: `5xTier1 & 3xTier2` - Players start with 5 whites and 3 greens.
  - Equipment and items can be both specified in the same list. Only the first resolved equipment is given to the player (or the first two in case of MUL-T).
	- Example: `{ BFG & 10xAutoCastEquipment } | { EliteFireEquipment & StrengthenBurn }` - Players start either with a Preon Accumulator and 10 Gesture of the Drowned or with Ifrit's Distinction and 1 Ignition Tank

### ItemList Operators

Each entry in the itemlist shall follow the format: `<repeat>x<itemkey>!<itemname>*<multiplier>: <weight>`. Entries can be separated by `&` or `|`. Entries can be grouped with `{` and `}`.

#### `!` (Not-Operator)
- **Purpose:** Blacklist items from tiers or droptables. Has no effect on concrete item names.
- **Placement:** Directly *after* the name of the droptable or tier.
- **Example:**
  - `Tier1!Hoof` - White item but not a Hoof
  - `dtChest1!Bear!Crowbar` - Content of a Small Chest but neither Tougher Times nor Crowbar.

#### `&` (And-Operator)
- **Purpose:** Every item separated by `&` will be granted.
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
- **Purpose:** Multiplies the amount of granted items of a group.
- **Placement:** Directly *after* a group or item.
- **Example:**
  - `Hoof*5` - The player gets 5 Hoof items.
  - `{ Tier1 | Tier2 }*3` - The player gets 3 copies of a single type of item from either Tier1 or Tier2.

#### `{` and `}` (Grouping Symbols)
- **Purpose:** Group items and operators together. Can use `&` and `|` within a group, but they cannot be mixed.
- **Operator precedence:** Use `{ }` to explicitly control grouping and order of operations, especially when combining repeat, and, or multiply.
- **Nested groups:** You can nest `{}` for more complex item generation logic.
- **Example:**
  - `{ 2xdtChest1 | dtChest2 }` - Represents a choice between "2x small chest" and "1 large chest" as a single entity; useful with other operators.

### Summary Table

| Operator | Symbol | Description                                               | Example                           |
|----------|--------|-----------------------------------------------------------|-----------------------------------|
| Not      | `!`    | Blacklists the item from the group                        | `A!B`                             |
| And      | `&`    | All items, together                                       | `A & B`                           |
| Or       | `\|`   | Only one item, random (optional weight via `:`)           | `A \| B:0.7 \| C:0.3`             |
| Weight   | `:`    | Specifies probability weight for options when using `\|`  | `A: 0.6 \| B: 0.4`                |
| Repeat   | `x`    | Repeats the following item/group N times                  | `5xA` or `3x{A \| B}`             |
| Multiply | `*`    | Multiplies the amount of granted items in the group by N  | `{A & B}*4`                       |
| Grouping | `{}`   | Groups items/expressions                                  | `{A & 2xB}`                       |

### Examples

A random lunar item, except for Light Flux Pauldron or Stone Flux Pauldron:
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

## Item Keywords

You can use:
- **Internal item names** (see [R2Wiki - Items-and-Equipments-Data](https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Developer-Reference/Items-and-Equipments-Data/)).  
- **Item Tier names**: `Tier1`, `Tier2`, `Tier3`, `Lunar`, `Boss`, `VoidTier1`, `VoidTier2`, `VoidTier3`, `VoidBoss`.
- **Droptable names**: e.g. `dtChest1`, `dtLunarChest`, `dtVoidChest`. See below.

### Droptable Names

Here is a list of supported droptables:

| Drop Table Name                 | canDropBeReplaced | requiredItemTags | bannedItemTags | tier1Weight | tier2Weight | tier3Weight | bossWeight | lunarEquipmentWeight | lunarItemWeight | lunarCombinedWeight | equipmentWeight | voidTier1Weight | voidTier2Weight | voidTier3Weight | voidBossWeight |
|---------------------------------|-------------------|------------------|----------------|-------------|-------------|-------------|------------|----------------------|-----------------|---------------------|-----------------|-----------------|-----------------|-----------------|----------------|
| dtMonsterTeamTier1Item | True |  | AIBlacklist, OnKillEffect, EquipmentRelated, SprintRelated | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| dtMonsterTeamTier2Item | True |  | AIBlacklist, OnKillEffect, EquipmentRelated, SprintRelated | 0 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| dtMonsterTeamTier3Item | True |  | AIBlacklist, OnKillEffect, EquipmentRelated, SprintRelated | 0 | 0 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| dtSacrificeArtifact | True |  | SacrificeBlacklist | 0.7 | 0.3 | 0.01 | 0 | 0 | 0 | 0 | 0.1 | 0 | 0 | 0 | 0 |
| dtAISafeTier1Item | True |  | AIBlacklist | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| dtAISafeTier2Item | True |  | AIBlacklist | 0 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| dtAISafeTier3Item | True |  | AIBlacklist | 0 | 0 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| dtEquipment | True |  |  | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 1 | 0 | 0 | 0 | 0 |
| dtTier1Item | True |  |  | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| dtTier2Item | True |  |  | 0 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| dtTier3Item | True |  |  | 0 | 0 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| dtVoidChest | True |  |  | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 6 | 3 | 1 | 0 |
| dtCasinoChest | True |  |  | 0.7 | 0.3 | 0.01 | 0 | 0 | 0 | 0 | 0.1 | 0 | 0 | 0 | 0 |
| dtSmallChestDamage | True | Damage |  | 0.8 | 0.2 | 0.01 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| dtSmallChestHealing | True | Healing |  | 0.8 | 0.2 | 0.01 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| dtSmallChestUtility | True | Utility |  | 0.8 | 0.2 | 0.01 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| dtChest1 | True |  |  | 0.8 | 0.2 | 0.01 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| dtChest2 | True |  |  | 0 | 0.8 | 0.2 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| dtDuplicatorTier1 | True |  | CannotDuplicate | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| dtDuplicatorTier2 | True |  | CannotDuplicate | 0 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| dtDuplicatorTier3 | True |  | CannotDuplicate | 0 | 0 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| dtDuplicatorWild | True |  | WorldUnique | 0 | 0 | 0 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| dtGoldChest | True |  |  | 0 | 0 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| dtLunarChest | True |  |  | 0 | 0 | 0 | 0 | 0 | 0 | 1 | 0 | 0 | 0 | 0 | 0 |
| dtShrineChance | True |  |  | 8 | 2 | 0.2 | 0 | 0 | 0 | 0 | 2 | 0 | 0 | 0 | 0 |
| dtLockbox | True |  |  | 0 | 4 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| dtITBossWave | True |  |  | 0 | 80 | 7.5 | 7.5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| dtITDefaultWave | True |  |  | 80 | 10 | 0.25 | 0.25 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| dtITLunar | True |  |  | 0 | 0 | 0 | 0 | 0 | 0 | 100 | 0 | 0 | 0 | 0 | 0 |
| dtITSpecialBossWave | True |  |  | 0 | 0 | 80 | 20 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| dtITVoid | True |  |  | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 80 | 20 | 1 | 0 |
| dtCategoryChest2Damage | True | Damage |  | 0 | 0.8 | 0.2 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| dtCategoryChest2Healing | True | Healing |  | 0 | 0.8 | 0.2 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| dtCategoryChest2Utility | True | Utility |  | 0 | 0.8 | 0.2 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| dtVoidCamp | True |  |  | 40 | 40 | 10 | 3 | 0 | 0 | 0 | 0 | 5.714286 | 5.714286 | 1.25 | 0 |
| dtVoidTriple | True |  |  | 0.8 | 0.2 | 0.01 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| dtVoidLockbox | True |  |  | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 5 | 5 | 2 | 0 |
| AurelioniteHeartPickupDropTable | True |  |  | 0 | 0 | 0.4 | 0.6 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| GeodeRewardDropTable | True |  |  | 0.8 | 0.2 | 0.01 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| dtShrineHalcyoniteTier1 | True |  |  | 0.65 | 0.3 | 0.05 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| dtShrineHalcyoniteTier2 | True | HalcyoniteShrine |  | 0.65 | 0.3 | 0.05 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| dtShrineHalcyoniteTier3 | True |  |  | 0.65 | 0.3 | 0.05 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| dtChanceDoll | True |  |  | 0 | 0.79 | 0.2 | 0.01 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| dtSonorousEcho | True |  |  | 0.9 | 0.1 | 0.001 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| dtCommandChest | True |  | Any, Any, Any, Any, Any | 0.2 | 0.2 | 0.05 | 0.05 | 0 | 0 | 0 | 0.2 | 0.1 | 0.1 | 0.05 | 0.05 |


### ENBF

You can use this ENBF to validate your item string for example at [PaulKlineLabs' BNF Playground](https://bnfplayground.pauliankline.com/?bnf=%3CItemList%3E%20%20%20%20%20%20%20%3A%3A%3D%20%3CAND_LIST%3E%20%7C%20%3COR_LIST%3E%0A%3CAND_LIST%3E%20%20%20%20%20%20%20%3A%3A%3D%20%3CENTRY%3E%20(%22%20%26%20%22%20%3CENTRY%3E)*%0A%3COR_LIST%3E%20%20%20%20%20%20%20%20%3A%3A%3D%20%3CWEIGHTED_ENTRY%3E%20(%22%20%7C%20%22%20%3CWEIGHTED_ENTRY%3E)*%0A%3CENTRY%3E%20%20%20%20%20%20%20%20%20%20%3A%3A%3D%20(%3Crep%3E%20%22x%22)%3F%20%3CITEM_KEY%3E%20(%22*%22%20%3Cmult%3E)%3F%0A%3CWEIGHTED_ENTRY%3E%20%3A%3A%3D%20%3CENTRY%3E%20(%22%3A%20%22%20%3Cweight%3E)%3F%0A%3CITEM_KEY%3E%20%20%20%20%20%20%20%3A%3A%3D%20%3Citemname%3E%20%7C%20%3Ctier%3E%20(%22!%22%20%3Citemname%3E)*%20%7C%20%3Cdroptable%3E%20(%22!%22%20%3Citemname%3E)*%20%7C%20%22%7B%20%22%20%3CAND_LIST%3E%20%22%20%7D%22%20%7C%20%22%7B%20%22%20%3COR_LIST%3E%20%22%20%7D%22%0A%0A%3Citemname%3E%20%20%3A%3A%3D%20%22AlienHead%22%20%7C%20%22ArmorPlate%22%20%7C%20%22ArmorReductionOnHit%22%20%7C%20%22AttackSpeedAndMoveSpeed%22%20%7C%20%22AttackSpeedOnCrit%22%20%7C%20%22AutoCastEquipment%22%20%7C%20%22Bandolier%22%20%7C%20%22BarrierOnKill%22%20%7C%20%22BarrierOnOverHeal%22%20%7C%20%22Bear%22%20%7C%20%22BearVoid%22%20%7C%20%22BeetleGland%22%20%7C%20%22Behemoth%22%20%7C%20%22BleedOnHit%22%20%7C%20%22BleedOnHitAndExplode%22%20%7C%20%22BleedOnHitVoid%22%20%7C%20%22BonusGoldPackOnKill%22%20%7C%20%22BoostAllStats%22%20%7C%20%22BossDamageBonus%22%20%7C%20%22BounceNearby%22%20%7C%20%22ChainLightning%22%20%7C%20%22ChainLightningVoid%22%20%7C%20%22Clover%22%20%7C%20%22CloverVoid%22%20%7C%20%22CritDamage%22%20%7C%20%22CritGlasses%22%20%7C%20%22CritGlassesVoid%22%20%7C%20%22Crowbar%22%20%7C%20%22Dagger%22%20%7C%20%22DeathMark%22%20%7C%20%22DelayedDamage%22%20%7C%20%22DroneWeapons%22%20%7C%20%22ElementalRingVoid%22%20%7C%20%22EnergizedOnEquipmentUse%22%20%7C%20%22EquipmentMagazine%22%20%7C%20%22EquipmentMagazineVoid%22%20%7C%20%22ExecuteLowHealthElite%22%20%7C%20%22ExplodeOnDeath%22%20%7C%20%22ExplodeOnDeathVoid%22%20%7C%20%22ExtraLife%22%20%7C%20%22ExtraLifeVoid%22%20%7C%20%22ExtraShrineItem%22%20%7C%20%22ExtraStatsOnLevelUp%22%20%7C%20%22FallBoots%22%20%7C%20%22Feather%22%20%7C%20%22FireRing%22%20%7C%20%22FireballsOnHit%22%20%7C%20%22Firework%22%20%7C%20%22FlatHealth%22%20%7C%20%22FocusConvergence%22%20%7C%20%22FragileDamageBonus%22%20%7C%20%22FreeChest%22%20%7C%20%22GhostOnKill%22%20%7C%20%22GoldOnHit%22%20%7C%20%22GoldOnHurt%22%20%7C%20%22GoldOnStageStart%22%20%7C%20%22HalfAttackSpeedHalfCooldowns%22%20%7C%20%22HalfSpeedDoubleHealth%22%20%7C%20%22HeadHunter%22%20%7C%20%22HealOnCrit%22%20%7C%20%22HealWhileSafe%22%20%7C%20%22HealingPotion%22%20%7C%20%22Hoof%22%20%7C%20%22IceRing%22%20%7C%20%22Icicle%22%20%7C%20%22IgniteOnKill%22%20%7C%20%22ImmuneToDebuff%22%20%7C%20%22IncreaseDamageOnMultiKill%22%20%7C%20%22IncreaseHealing%22%20%7C%20%22IncreasePrimaryDamage%22%20%7C%20%22Infusion%22%20%7C%20%22JumpBoost%22%20%7C%20%22KillEliteFrenzy%22%20%7C%20%22KnockBackHitEnemies%22%20%7C%20%22Knurl%22%20%7C%20%22LaserTurbine%22%20%7C%20%22LightningStrikeOnHit%22%20%7C%20%22LowerHealthHigherDamage%22%20%7C%20%22LowerPricedChests%22%20%7C%20%22LunarBadLuck%22%20%7C%20%22LunarDagger%22%20%7C%20%22LunarPrimaryReplacement%22%20%7C%20%22LunarSecondaryReplacement%22%20%7C%20%22LunarSpecialReplacement%22%20%7C%20%22LunarSun%22%20%7C%20%22LunarTrinket%22%20%7C%20%22LunarUtilityReplacement%22%20%7C%20%22Medkit%22%20%7C%20%22MeteorAttackOnHighDamage%22%20%7C%20%22MinorConstructOnKill%22%20%7C%20%22Missile%22%20%7C%20%22MissileVoid%22%20%7C%20%22MonstersOnShrineUse%22%20%7C%20%22MoreMissile%22%20%7C%20%22MoveSpeedOnKill%22%20%7C%20%22Mushroom%22%20%7C%20%22MushroomVoid%22%20%7C%20%22NearbyDamageBonus%22%20%7C%20%22NegateAttack%22%20%7C%20%22NovaOnHeal%22%20%7C%20%22NovaOnLowHealth%22%20%7C%20%22OnLevelUpFreeUnlock%22%20%7C%20%22OutOfCombatArmor%22%20%7C%20%22ParentEgg%22%20%7C%20%22Pearl%22%20%7C%20%22PermanentDebuffOnHit%22%20%7C%20%22PersonalShield%22%20%7C%20%22Phasing%22%20%7C%20%22Plant%22%20%7C%20%22PrimarySkillShuriken%22%20%7C%20%22RandomDamageZone%22%20%7C%20%22RandomEquipmentTrigger%22%20%7C%20%22RandomlyLunar%22%20%7C%20%22RegeneratingScrap%22%20%7C%20%22RepeatHeal%22%20%7C%20%22ResetChests%22%20%7C%20%22RoboBallBuddy%22%20%7C%20%22ScrapGreen%22%20%7C%20%22ScrapRed%22%20%7C%20%22ScrapWhite%22%20%7C%20%22ScrapYellow%22%20%7C%20%22SecondarySkillMagazine%22%20%7C%20%22Seed%22%20%7C%20%22ShieldOnly%22%20%7C%20%22ShinyPearl%22%20%7C%20%22ShockNearby%22%20%7C%20%22SiphonOnLowHealth%22%20%7C%20%22SlowOnHit%22%20%7C%20%22SlowOnHitVoid%22%20%7C%20%22SprintArmor%22%20%7C%20%22SprintBonus%22%20%7C%20%22SprintOutOfCombat%22%20%7C%20%22SprintWisp%22%20%7C%20%22Squid%22%20%7C%20%22StickyBomb%22%20%7C%20%22StrengthenBurn%22%20%7C%20%22StunAndPierce%22%20%7C%20%22StunChanceOnHit%22%20%7C%20%22Syringe%22%20%7C%20%22TPHealingNova%22%20%7C%20%22Talisman%22%20%7C%20%22TeleportOnLowHealth%22%20%7C%20%22Thorns%22%20%7C%20%22TitanGoldDuringTP%22%20%7C%20%22Tooth%22%20%7C%20%22TreasureCache%22%20%7C%20%22TreasureCacheVoid%22%20%7C%20%22TriggerEnemyDebuffs%22%20%7C%20%22UtilitySkillMagazine%22%20%7C%20%22VoidMegaCrabItem%22%20%7C%20%22WarCryOnMultiKill%22%20%7C%20%22WardOnLevel%22%0A%3Cdroptable%3E%20%3A%3A%3D%20%22dtMonsterTeamTier1Item%22%20%7C%20%22dtMonsterTeamTier2Item%22%20%7C%20%22dtMonsterTeamTier3Item%22%20%7C%20%22dtSacrificeArtifact%22%20%7C%20%22dtAISafeTier1Item%22%20%7C%20%22dtAISafeTier2Item%22%20%7C%20%22dtAISafeTier3Item%22%20%7C%20%22dtEquipment%22%20%7C%20%22dtTier1Item%22%20%7C%20%22dtTier2Item%22%20%7C%20%22dtTier3Item%22%20%7C%20%22dtVoidChest%22%20%7C%20%22dtCasinoChest%22%20%7C%20%22dtSmallChestDamage%22%20%7C%20%22dtSmallChestHealing%22%20%7C%20%22dtSmallChestUtility%22%20%7C%20%22dtChest1%22%20%7C%20%22dtChest2%22%20%7C%20%22dtDuplicatorTier1%22%20%7C%20%22dtDuplicatorTier2%22%20%7C%20%22dtDuplicatorTier3%22%20%7C%20%22dtDuplicatorWild%22%20%7C%20%22dtGoldChest%22%20%7C%20%22dtLunarChest%22%20%7C%20%22dtShrineChance%22%20%7C%20%22dtLockbox%22%20%7C%20%22dtITBossWave%22%20%7C%20%22dtITDefaultWave%22%20%20%7C%20%22dtITLunar%22%20%7C%20%22dtITSpecialBossWave%22%20%7C%20%22dtITVoid%22%20%7C%20%22dtCategoryChest2Damage%22%20%7C%20%22dtCategoryChest2Healing%22%20%7C%20%22dtCategoryChest2Utility%22%20%7C%20%22dtVoidCamp%22%20%7C%20%22dtVoidTriple%22%20%7C%20%22dtVoidLockbox%22%20%7C%20%22AurelioniteHeartPickupDropTable%22%20%7C%20%22GeodeRewardDropTable%22%20%7C%20%22dtShrineHalcyoniteTier1%22%20%7C%20%22dtShrineHalcyoniteTier2%22%20%7C%20%22dtShrineHalcyoniteTier3%22%20%7C%20%22dtChanceDoll%22%20%7C%20%22dtSonorousEcho%22%20%7C%20%22dtCommandChest%22%0A%3Ctier%3E%20%20%20%20%20%20%3A%3A%3D%20%22Tier1%22%20%7C%20%22Tier2%22%20%7C%20%22Tier3%22%20%7C%20%22Lunar%22%20%7C%20%22Boss%22%20%7C%20%22VoidTier1%22%20%7C%20%22VoidTier2%22%20%7C%20%22VoidTier3%22%20%7C%20%22VoidBoss%22%0A%3Crep%3E%20%20%20%20%20%20%20%3A%3A%3D%20%3Cint%3E%0A%3Cmult%3E%20%20%20%20%20%20%3A%3A%3D%20%3Cint%3E%0A%3Cweight%3E%20%20%20%20%3A%3A%3D%20%3Cfloat%3E%0A%0A%3Cint%3E%20%20%20%3A%3A%3D%20%5B1-9%5D%20%5B0-9%5D*%0A%3Cfloat%3E%20%3A%3A%3D%20(%220%22%20%7C%20%3Cint%3E)%20(%22.%22%20%5B0-9%5D%2B%20)%3F&name=Risk%20of%20Rain%202%20Item%20String%20Parser).

```
<ItemList>       ::= <AND_LIST> | <OR_LIST>
<AND_LIST>       ::= <ENTRY> (" & " <ENTRY>)*
<OR_LIST>        ::= <WEIGHTED_ENTRY> (" | " <WEIGHTED_ENTRY>)*
<ENTRY>          ::= (<rep> "x")? <ITEM_KEY> ("*" <mult>)?
<WEIGHTED_ENTRY> ::= <ENTRY> (": " <weight>)?
<ITEM_KEY>       ::= <itemname> | <tier> ("!" <itemname>)* | <droptable> ("!" <itemname>)* | "{ " <AND_LIST> " }" | "{ " <OR_LIST> " }"

<itemname>  ::= "AlienHead" | "ArmorPlate" | "ArmorReductionOnHit" | "AttackSpeedAndMoveSpeed" | "AttackSpeedOnCrit" | "AutoCastEquipment" | "Bandolier" | "BarrierOnKill" | "BarrierOnOverHeal" | "Bear" | "BearVoid" | "BeetleGland" | "Behemoth" | "BleedOnHit" | "BleedOnHitAndExplode" | "BleedOnHitVoid" | "BonusGoldPackOnKill" | "BoostAllStats" | "BossDamageBonus" | "BounceNearby" | "ChainLightning" | "ChainLightningVoid" | "Clover" | "CloverVoid" | "CritDamage" | "CritGlasses" | "CritGlassesVoid" | "Crowbar" | "Dagger" | "DeathMark" | "DelayedDamage" | "DroneWeapons" | "ElementalRingVoid" | "EnergizedOnEquipmentUse" | "EquipmentMagazine" | "EquipmentMagazineVoid" | "ExecuteLowHealthElite" | "ExplodeOnDeath" | "ExplodeOnDeathVoid" | "ExtraLife" | "ExtraLifeVoid" | "ExtraShrineItem" | "ExtraStatsOnLevelUp" | "FallBoots" | "Feather" | "FireRing" | "FireballsOnHit" | "Firework" | "FlatHealth" | "FocusConvergence" | "FragileDamageBonus" | "FreeChest" | "GhostOnKill" | "GoldOnHit" | "GoldOnHurt" | "GoldOnStageStart" | "HalfAttackSpeedHalfCooldowns" | "HalfSpeedDoubleHealth" | "HeadHunter" | "HealOnCrit" | "HealWhileSafe" | "HealingPotion" | "Hoof" | "IceRing" | "Icicle" | "IgniteOnKill" | "ImmuneToDebuff" | "IncreaseDamageOnMultiKill" | "IncreaseHealing" | "IncreasePrimaryDamage" | "Infusion" | "JumpBoost" | "KillEliteFrenzy" | "KnockBackHitEnemies" | "Knurl" | "LaserTurbine" | "LightningStrikeOnHit" | "LowerHealthHigherDamage" | "LowerPricedChests" | "LunarBadLuck" | "LunarDagger" | "LunarPrimaryReplacement" | "LunarSecondaryReplacement" | "LunarSpecialReplacement" | "LunarSun" | "LunarTrinket" | "LunarUtilityReplacement" | "Medkit" | "MeteorAttackOnHighDamage" | "MinorConstructOnKill" | "Missile" | "MissileVoid" | "MonstersOnShrineUse" | "MoreMissile" | "MoveSpeedOnKill" | "Mushroom" | "MushroomVoid" | "NearbyDamageBonus" | "NegateAttack" | "NovaOnHeal" | "NovaOnLowHealth" | "OnLevelUpFreeUnlock" | "OutOfCombatArmor" | "ParentEgg" | "Pearl" | "PermanentDebuffOnHit" | "PersonalShield" | "Phasing" | "Plant" | "PrimarySkillShuriken" | "RandomDamageZone" | "RandomEquipmentTrigger" | "RandomlyLunar" | "RegeneratingScrap" | "RepeatHeal" | "ResetChests" | "RoboBallBuddy" | "ScrapGreen" | "ScrapRed" | "ScrapWhite" | "ScrapYellow" | "SecondarySkillMagazine" | "Seed" | "ShieldOnly" | "ShinyPearl" | "ShockNearby" | "SiphonOnLowHealth" | "SlowOnHit" | "SlowOnHitVoid" | "SprintArmor" | "SprintBonus" | "SprintOutOfCombat" | "SprintWisp" | "Squid" | "StickyBomb" | "StrengthenBurn" | "StunAndPierce" | "StunChanceOnHit" | "Syringe" | "TPHealingNova" | "Talisman" | "TeleportOnLowHealth" | "Thorns" | "TitanGoldDuringTP" | "Tooth" | "TreasureCache" | "TreasureCacheVoid" | "TriggerEnemyDebuffs" | "UtilitySkillMagazine" | "VoidMegaCrabItem" | "WarCryOnMultiKill" | "WardOnLevel"
<droptable> ::= "dtMonsterTeamTier1Item" | "dtMonsterTeamTier2Item" | "dtMonsterTeamTier3Item" | "dtSacrificeArtifact" | "dtAISafeTier1Item" | "dtAISafeTier2Item" | "dtAISafeTier3Item" | "dtEquipment" | "dtTier1Item" | "dtTier2Item" | "dtTier3Item" | "dtVoidChest" | "dtCasinoChest" | "dtSmallChestDamage" | "dtSmallChestHealing" | "dtSmallChestUtility" | "dtChest1" | "dtChest2" | "dtDuplicatorTier1" | "dtDuplicatorTier2" | "dtDuplicatorTier3" | "dtDuplicatorWild" | "dtGoldChest" | "dtLunarChest" | "dtShrineChance" | "dtLockbox" | "dtITBossWave" | "dtITDefaultWave"  | "dtITLunar" | "dtITSpecialBossWave" | "dtITVoid" | "dtCategoryChest2Damage" | "dtCategoryChest2Healing" | "dtCategoryChest2Utility" | "dtVoidCamp" | "dtVoidTriple" | "dtVoidLockbox" | "AurelioniteHeartPickupDropTable" | "GeodeRewardDropTable" | "dtShrineHalcyoniteTier1" | "dtShrineHalcyoniteTier2" | "dtShrineHalcyoniteTier3" | "dtChanceDoll" | "dtSonorousEcho" | "dtCommandChest"
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
<droptable> ::= "dtMonsterTeamTier1Item" | "dtMonsterTeamTier2Item" | "dtMonsterTeamTier3Item" | "dtSacrificeArtifact" | "dtAISafeTier1Item" | "dtAISafeTier2Item" | "dtAISafeTier3Item" | "dtEquipment" | "dtTier1Item" | "dtTier2Item" | "dtTier3Item" | "dtVoidChest" | "dtCasinoChest" | "dtSmallChestDamage" | "dtSmallChestHealing" | "dtSmallChestUtility" | "dtChest1" | "dtChest2" | "dtDuplicatorTier1" | "dtDuplicatorTier2" | "dtDuplicatorTier3" | "dtDuplicatorWild" | "dtGoldChest" | "dtLunarChest" | "dtShrineChance" | "dtLockbox" | "dtITBossWave" | "dtITDefaultWave"  | "dtITLunar" | "dtITSpecialBossWave" | "dtITVoid" | "dtCategoryChest2Damage" | "dtCategoryChest2Healing" | "dtCategoryChest2Utility" | "dtVoidCamp" | "dtVoidTriple" | "dtVoidLockbox" | "AurelioniteHeartPickupDropTable" | "GeodeRewardDropTable" | "dtShrineHalcyoniteTier1" | "dtShrineHalcyoniteTier2" | "dtShrineHalcyoniteTier3" | "dtChanceDoll" | "dtSonorousEcho" | "dtCommandChest"
<tier>      ::= "Tier1" | "Tier2" | "Tier3" | "Lunar" | "Boss" | "VoidTier1" | "VoidTier2" | "VoidTier3" | "VoidBoss"
<rep>       ::= <int>
<mult>      ::= <int>
<weight>    ::= <float>

<int>   ::= [1-9] [0-9]*
<float> ::= ("0" | <int>) ("." [0-9]+ )?
```
