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
- **Internal item, equipment, drone and key item names** (see [R2Wiki - Items-and-Equipments-Data](https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Developer-Reference/Items-and-Equipments-Data/)). `BurnNearby` is Helfire Tincture, the equipment; the item of the same name is unused. Names from a DLC that is not enabled for the run are skipped.
- **Item Tier names**: `Tier1`, `Tier2`, `Tier3`, `Lunar`, `Boss`, `VoidTier1`, `VoidTier2`, `VoidTier3`, `VoidBoss`, `FoodTier`, `Cursed`, `Hex`, `Page`. `Lunar` contains lunar items and lunar equipment.
- **Modded item tiers**: the name of the tier's `ItemTierDef`, or that name without `TierDef` or `Def` at the end, e.g. `Highlander` for `HighlanderTierDef`. Case and characters other than letters, digits and `_` don't count. If the short name fits several tiers, use the full name.
- **Other drop lists of the run**: `Equipment`, `LunarEquipment`, `LunarItem`, `PowerShape`, `KeyItem`
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

You can use this EBNF to validate your item string for example at [PaulKlineLabs' BNF Playground](https://bnfplayground.pauliankline.com/?bnf=%3CItemString%3E%20%20%20%20%20%20%20%3A%3A%3D%20%3CAND_LIST%3E%20%7C%20%3COR_LIST%3E%0A%3CAND_LIST%3E%20%20%20%20%20%20%20%3A%3A%3D%20%3CENTRY%3E%20(%22%20%26%20%22%20%3CENTRY%3E)*%0A%3COR_LIST%3E%20%20%20%20%20%20%20%20%3A%3A%3D%20%3CWEIGHTED_ENTRY%3E%20(%22%20%7C%20%22%20%3CWEIGHTED_ENTRY%3E)*%0A%3CENTRY%3E%20%20%20%20%20%20%20%20%20%20%3A%3A%3D%20(%3Crep%3E%20%22x%22)%3F%20%3CITEM_KEY%3E%20(%22*%22%20%3Cmult%3E)%3F%0A%3CWEIGHTED_ENTRY%3E%20%3A%3A%3D%20%3CENTRY%3E%20(%22%3A%20%22%20%3Cweight%3E)%3F%0A%3CITEM_KEY%3E%20%20%20%20%20%20%20%3A%3A%3D%20%3Citemname%3E%20%7C%20%3Ctier%3E%20(%22!%22%20%3Citemname%3E)*%20%7C%20%3Cdroptable%3E%20(%22!%22%20%3Citemname%3E)*%20%7C%20%22%7B%20%22%20%3CAND_LIST%3E%20%22%20%7D%22%20%7C%20%22%7B%20%22%20%3COR_LIST%3E%20%22%20%7D%22%0A%0A%3Citemname%3E%20%20%3A%3A%3D%20%22AACannon%22%20%7C%20%22AchievementRewardTest%22%20%7C%20%22AcolyteSoul%22%20%7C%20%22AdaptiveArmor%22%20%7C%20%22AlienHead%22%20%7C%20%22AllStatBoost%22%20%7C%20%22ArmorPlate%22%20%7C%20%22ArmorReductionOnHit%22%20%7C%20%22AttackSpeedAndMoveSpeed%22%20%7C%20%22AttackSpeedOnCrit%22%20%7C%20%22AttackSpeedPerNearbyAllyOrEnemy%22%20%7C%20%22AutoCastEquipment%22%20%7C%20%22Bandolier%22%20%7C%20%22BarrageOnBoss%22%20%7C%20%22BarrierEnemiesOnKill%22%20%7C%20%22BarrierOnCooldown%22%20%7C%20%22BarrierOnKill%22%20%7C%20%22BarrierOnOverHeal%22%20%7C%20%22Bear%22%20%7C%20%22BearVoid%22%20%7C%20%22BeetleGland%22%20%7C%20%22Behemoth%22%20%7C%20%22BleedOnHit%22%20%7C%20%22BleedOnHitAndExplode%22%20%7C%20%22BleedOnHitVoid%22%20%7C%20%22BlightZone%22%20%7C%20%22Blindness%22%20%7C%20%22BonusGoldPackOnKill%22%20%7C%20%22BoostAllStats%22%20%7C%20%22BoostAttackSpeed%22%20%7C%20%22BoostCharacterScale%22%20%7C%20%22BoostDamage%22%20%7C%20%22BoostEquipmentRecharge%22%20%7C%20%22BoostHp%22%20%7C%20%22BossDamageBonus%22%20%7C%20%22BounceNearby%22%20%7C%20%22BurnNearby%22%20%7C%20%22CausticPheromones%22%20%7C%20%22ChainLightning%22%20%7C%20%22ChainLightningVoid%22%20%7C%20%22Clover%22%20%7C%20%22CloverVoid%22%20%7C%20%22ConvertCritChanceToCritDamage%22%20%7C%20%22ConvertHealingToAttackSpeed%22%20%7C%20%22CooldownOnCrit%22%20%7C%20%22CrippleWardOnLevel%22%20%7C%20%22CritAtLowerElevation%22%20%7C%20%22CritDamage%22%20%7C%20%22CritGlasses%22%20%7C%20%22CritGlassesVoid%22%20%7C%20%22CritHeal%22%20%7C%20%22Crowbar%22%20%7C%20%22CutHp%22%20%7C%20%22Dagger%22%20%7C%20%22DeathMark%22%20%7C%20%22DelayedDamage%22%20%7C%20%22DestructibleSpawner%22%20%7C%20%22Doppelganger%22%20%7C%20%22DreamDemon%22%20%7C%20%22DrizzlePlayerHelper%22%20%7C%20%22DroneDynamiteDisplay%22%20%7C%20%22DroneUpgradeHidden%22%20%7C%20%22DroneWeapons%22%20%7C%20%22DroneWeaponsBoost%22%20%7C%20%22DroneWeaponsDisplay1%22%20%7C%20%22DroneWeaponsDisplay2%22%20%7C%20%22DronesDropDynamite%22%20%7C%20%22Duplicator%22%20%7C%20%22ElementalRingVoid%22%20%7C%20%22EliteDeathGhost%22%20%7C%20%22EmpowerAlways%22%20%7C%20%22EmpowerBuff%22%20%7C%20%22EmpowerMagazine%22%20%7C%20%22EmpowerOnKill%22%20%7C%20%22EmpowerRefresh%22%20%7C%20%22EnergizedOnEquipmentUse%22%20%7C%20%22EquipmentMagazine%22%20%7C%20%22EquipmentMagazineVoid%22%20%7C%20%22ExecuteLowHealthElite%22%20%7C%20%22ExplodeOnDeath%22%20%7C%20%22ExplodeOnDeathVoid%22%20%7C%20%22ExtraEquipment%22%20%7C%20%22ExtraLife%22%20%7C%20%22ExtraLifeConsumed%22%20%7C%20%22ExtraLifeVoid%22%20%7C%20%22ExtraLifeVoidConsumed%22%20%7C%20%22ExtraShrineItem%22%20%7C%20%22ExtraStatsOnLevelUp%22%20%7C%20%22FallBoots%22%20%7C%20%22Feather%22%20%7C%20%22FireRing%22%20%7C%20%22FireballsOnHit%22%20%7C%20%22Firework%22%20%7C%20%22FlatHealth%22%20%7C%20%22FocusConvergence%22%20%7C%20%22FragileDamageBonus%22%20%7C%20%22FragileDamageBonusConsumed%22%20%7C%20%22FreeChest%22%20%7C%20%22Ghost%22%20%7C%20%22GhostBlades%22%20%7C%20%22GhostOnKill%22%20%7C%20%22GoldOnHit%22%20%7C%20%22GoldOnHurt%22%20%7C%20%22GoldenGoose%22%20%7C%20%22GummyCloneIdentifier%22%20%7C%20%22HalfAttackSpeedHalfCooldowns%22%20%7C%20%22HalfSpeedDoubleHealth%22%20%7C%20%22HeadHunter%22%20%7C%20%22HealOnCrit%22%20%7C%20%22HealWhileSafe%22%20%7C%20%22HealingPotion%22%20%7C%20%22HealingPotionConsumed%22%20%7C%20%22HealthDecay%22%20%7C%20%22HexCurse%22%20%7C%20%22HexHaunting%22%20%7C%20%22HexMountain%22%20%7C%20%22HitOnJump%22%20%7C%20%22Hoof%22%20%7C%20%22IceRing%22%20%7C%20%22Icicle%22%20%7C%20%22IgniteOnKill%22%20%7C%20%22ImmuneToDebuff%22%20%7C%20%22IncreaseDamageOnMultiKill%22%20%7C%20%22IncreaseHealing%22%20%7C%20%22IncreasePrimaryDamage%22%20%7C%20%22IncreasedDifficulty%22%20%7C%20%22Incubator%22%20%7C%20%22Infusion%22%20%7C%20%22InvadingDoppelganger%22%20%7C%20%22ItemDropChanceOnKill%22%20%7C%20%22JumpBoost%22%20%7C%20%22JumpDamageStrike%22%20%7C%20%22Junk%22%20%7C%20%22Karma%22%20%7C%20%22KillEliteFrenzy%22%20%7C%20%22KnockBackHitEnemies%22%20%7C%20%22KnockBackHitEnemiesNoCooldown%22%20%7C%20%22Knurl%22%20%7C%20%22LaserTurbine%22%20%7C%20%22LemurianHarness%22%20%7C%20%22LevelBonus%22%20%7C%20%22LightningStrikeOnHit%22%20%7C%20%22LowHealthOrbSpawner%22%20%7C%20%22LowHealthOrbSpawnerConsumed%22%20%7C%20%22LowerHealing%22%20%7C%20%22LowerPricedChests%22%20%7C%20%22LowerPricedChestsConsumed%22%20%7C%20%22LunarBadLuck%22%20%7C%20%22LunarDagger%22%20%7C%20%22LunarPrimaryReplacement%22%20%7C%20%22LunarSecondaryReplacement%22%20%7C%20%22LunarSpecialReplacement%22%20%7C%20%22LunarSun%22%20%7C%20%22LunarTrinket%22%20%7C%20%22LunarUtilityReplacement%22%20%7C%20%22LunarWings%22%20%7C%20%22MageAttunement%22%20%7C%20%22MaxRegainableHealth%22%20%7C%20%22Medkit%22%20%7C%20%22MeteorAttackOnHighDamage%22%20%7C%20%22Mimicry%22%20%7C%20%22MinHealthPercentage%22%20%7C%20%22MinionLeash%22%20%7C%20%22MinorConstructOnKill%22%20%7C%20%22Missile%22%20%7C%20%22MissileVoid%22%20%7C%20%22MoneyLoan%22%20%7C%20%22MonsoonPlayerHelper%22%20%7C%20%22MonstersOnShrineUse%22%20%7C%20%22MoreMissile%22%20%7C%20%22MoveSpeedOnKill%22%20%7C%20%22MushShot%22%20%7C%20%22Mushroom%22%20%7C%20%22MushroomVoid%22%20%7C%20%22NearbyDamageBonus%22%20%7C%20%22NovaOnHeal%22%20%7C%20%22NovaOnLowHealth%22%20%7C%20%22OnLevelUpFreeUnlock%22%20%7C%20%22OutOfCombatArmor%22%20%7C%20%22OutOfTPRadius%22%20%7C%20%22PageClarity%22%20%7C%20%22PageClarityConsumed%22%20%7C%20%22PageDie%22%20%7C%20%22PageDieConsumed%22%20%7C%20%22PageDieCooldown%22%20%7C%20%22PagePurify%22%20%7C%20%22PagePurifyConsumed%22%20%7C%20%22PageSummonFeralToddlers%22%20%7C%20%22PageSummonFeralToddlersConsumed%22%20%7C%20%22PageTeamInvisibility%22%20%7C%20%22PageTeamInvisibilityConsumed%22%20%7C%20%22PageUpgradeItem%22%20%7C%20%22PageUpgradeItemConsumed%22%20%7C%20%22PageWarpGate%22%20%7C%20%22PageWarpGateConsumed%22%20%7C%20%22ParentEgg%22%20%7C%20%22PercentMaxHealth%22%20%7C%20%22PermanentDebuffOnHit%22%20%7C%20%22PersonalShield%22%20%7C%20%22Phasing%22%20%7C%20%22PhysicsProjectile%22%20%7C%20%22Plant%22%20%7C%20%22PlantOnHit%22%20%7C%20%22PlasmaCore%22%20%7C%20%22PrimarySkillShuriken%22%20%7C%20%22RandomDamageZone%22%20%7C%20%22RandomDebuffOnInteract%22%20%7C%20%22RandomEquipmentTrigger%22%20%7C%20%22RandomlyLunar%22%20%7C%20%22RechargeableJump%22%20%7C%20%22ReduceCharacterScale%22%20%7C%20%22ReduceDamage%22%20%7C%20%22RegeneratingScrap%22%20%7C%20%22RegeneratingScrapConsumed%22%20%7C%20%22RepeatHeal%22%20%7C%20%22RoamingItemDisable%22%20%7C%20%22RoboBallBuddy%22%20%7C%20%22SanguineChalice%22%20%7C%20%22SecondarySkillMagazine%22%20%7C%20%22Seed%22%20%7C%20%22ShareItemsWithEnemies%22%20%7C%20%22SharedSuffering%22%20%7C%20%22ShieldBooster%22%20%7C%20%22ShieldOnly%22%20%7C%20%22ShockDamageAura%22%20%7C%20%22ShockNearby%22%20%7C%20%22SiphonOnLowHealth%22%20%7C%20%22SkullCounter%22%20%7C%20%22SlipperyGround%22%20%7C%20%22SlowOnHit%22%20%7C%20%22SlowOnHitVoid%22%20%7C%20%22SpeedBoostPickup%22%20%7C%20%22SpeedOnPickup%22%20%7C%20%22SprintArmor%22%20%7C%20%22SprintBonus%22%20%7C%20%22SprintDamage%22%20%7C%20%22SprintOutOfCombat%22%20%7C%20%22SprintWisp%22%20%7C%20%22Squid%22%20%7C%20%22StatsFromScrap%22%20%7C%20%22StealBackHealth%22%20%7C%20%22StickyBomb%22%20%7C%20%22StrengthenBurn%22%20%7C%20%22StunAndPierce%22%20%7C%20%22StunChanceOnHit%22%20%7C%20%22SummonedEcho%22%20%7C%20%22Syringe%22%20%7C%20%22TPHealingNova%22%20%7C%20%22Talisman%22%20%7C%20%22TeamSizeDamageBonus%22%20%7C%20%22TeleportOnLowHealth%22%20%7C%20%22TeleportOnLowHealthConsumed%22%20%7C%20%22TeleportWhenOob%22%20%7C%20%22TempestOnKill%22%20%7C%20%22Thorns%22%20%7C%20%22TonicAffliction%22%20%7C%20%22Tooth%22%20%7C%20%22TrackEnemyDamageHidden%22%20%7C%20%22TransferDebuffOnHit%22%20%7C%20%22TreasureCache%22%20%7C%20%22TreasureCacheVoid%22%20%7C%20%22TriggerEnemyDebuffs%22%20%7C%20%22UrchinOnDeath%22%20%7C%20%22UseAmbientLevel%22%20%7C%20%22UtilitySkillMagazine%22%20%7C%20%22VengefulSpirit%22%20%7C%20%22VileCenser%22%20%7C%20%22VoidMegaCrabItem%22%20%7C%20%22VoidmanPassiveItem%22%20%7C%20%22WakeHiddenOverlay%22%20%7C%20%22WarCryOnCombat%22%20%7C%20%22WarCryOnMultiKill%22%20%7C%20%22WardOnLevel%22%20%7C%20%22WeakAssHamstring%22%20%7C%20%22WispOnKill%22%20%7C%20%22BFG%22%20%7C%20%22Blackhole%22%20%7C%20%22BossHunter%22%20%7C%20%22BurnNearby%22%20%7C%20%22Cleanse%22%20%7C%20%22CommandMissile%22%20%7C%20%22CrippleWard%22%20%7C%20%22CritOnUse%22%20%7C%20%22DeathProjectile%22%20%7C%20%22DroneBackup%22%20%7C%20%22FireBallDash%22%20%7C%20%22Fruit%22%20%7C%20%22GainArmor%22%20%7C%20%22Gateway%22%20%7C%20%22GoldGat%22%20%7C%20%22GummyClone%22%20%7C%20%22HealAndRevive%22%20%7C%20%22Jetpack%22%20%7C%20%22LifestealOnHit%22%20%7C%20%22Lightning%22%20%7C%20%22Meteor%22%20%7C%20%22Molotov%22%20%7C%20%22MultiShopCard%22%20%7C%20%22Parry%22%20%7C%20%22PassiveHealing%22%20%7C%20%22Recycle%22%20%7C%20%22Saw%22%20%7C%20%22Scanner%22%20%7C%20%22SpawnAltar%22%20%7C%20%22TeamWarCry%22%20%7C%20%22Tonic%22%20%7C%20%22VendingMachine%22%20%7C%20%22BackupDrone%22%20%7C%20%22BombardmentDrone%22%20%7C%20%22BomberDrone%22%20%7C%20%22CleanupDrone%22%20%7C%20%22CopycatDrone%22%20%7C%20%22DTGunnerDrone%22%20%7C%20%22DTHaulerDrone%22%20%7C%20%22DTHealDrone%22%20%7C%20%22Drone1%22%20%7C%20%22Drone2%22%20%7C%20%22DroneCommander%22%20%7C%20%22EmergencyDrone%22%20%7C%20%22EquipmentDrone%22%20%7C%20%22FlameDrone%22%20%7C%20%22HaulerDrone%22%20%7C%20%22JailerDrone%22%20%7C%20%22JunkDrone%22%20%7C%20%22MegaDrone%22%20%7C%20%22MissileDrone%22%20%7C%20%22RechargeDrone%22%20%7C%20%22Turret1%22%20%7C%20%22CursedBook%22%0A%3Cdroptable%3E%20%3A%3A%3D%20%22dtChest1%22%20%7C%20%22dtChest2%22%20%7C%20%22dtGoldChest%22%20%7C%20%22dtCasinoChest%22%20%7C%20%22dtSmallChestDamage%22%20%7C%20%22dtSmallChestHealing%22%20%7C%20%22dtSmallChestUtility%22%20%7C%20%22dtCategoryChest2Damage%22%20%7C%20%22dtCategoryChest2Healing%22%20%7C%20%22dtCategoryChest2Utility%22%20%7C%20%22dtLunarChest%22%20%7C%20%22dtEquipment%22%20%7C%20%22dtShrineChance%22%20%7C%20%22dtChanceDoll%22%20%7C%20%22dtLockbox%22%20%7C%20%22dtVoidChest%22%20%7C%20%22dtVoidCamp%22%20%7C%20%22dtCommandChest%22%20%7C%20%22dtMonsterTeamTier1Item%22%20%7C%20%22dtMonsterTeamTier2Item%22%20%7C%20%22dtMonsterTeamTier3Item%22%20%7C%20%22dtITDefaultWave%22%20%7C%20%22dtITBossWave%22%20%7C%20%22dtITSpecialBossWave%22%20%7C%20%22dtTemporaryItemsDistributor%22%20%7C%20%22bdtHallowedCauldron%22%0A%3Ctier%3E%20%20%20%20%20%20%3A%3A%3D%20%22Tier1%22%20%7C%20%22Tier2%22%20%7C%20%22Tier3%22%20%7C%20%22Lunar%22%20%7C%20%22Boss%22%20%7C%20%22VoidTier1%22%20%7C%20%22VoidTier2%22%20%7C%20%22VoidTier3%22%20%7C%20%22VoidBoss%22%20%7C%20%22FoodTier%22%20%7C%20%22Cursed%22%20%7C%20%22Hex%22%20%7C%20%22Page%22%20%7C%20%22Equipment%22%20%7C%20%22LunarEquipment%22%20%7C%20%22LunarItem%22%20%7C%20%22PowerShape%22%20%7C%20%22KeyItem%22%0A%3Crep%3E%20%20%20%20%20%20%20%3A%3A%3D%20%3Cint%3E%0A%3Cmult%3E%20%20%20%20%20%20%3A%3A%3D%20%3Cint%3E%0A%3Cweight%3E%20%20%20%20%3A%3A%3D%20%3Cfloat%3E%0A%0A%3Cint%3E%20%20%20%3A%3A%3D%20%5B1-9%5D%20%5B0-9%5D*%0A%3Cfloat%3E%20%3A%3A%3D%20(%220%22%20%7C%20%3Cint%3E)%20(%22.%22%20%5B0-9%5D%2B%20)%3F&name=Risk%20of%20Rain%202%20ItemStringParser).

```
<ItemList>       ::= <AND_LIST> | <OR_LIST>
<AND_LIST>       ::= <ENTRY> (" & " <ENTRY>)*
<OR_LIST>        ::= <WEIGHTED_ENTRY> (" | " <WEIGHTED_ENTRY>)*
<ENTRY>          ::= (<rep> "x")? <ITEM_KEY> ("*" <mult>)?
<WEIGHTED_ENTRY> ::= <ENTRY> (": " <weight>)?
<ITEM_KEY>       ::= <itemname> | <tier> ("!" <itemname>)* | <droptable> ("!" <itemname>)* | "{ " <AND_LIST> " }" | "{ " <OR_LIST> " }"

<itemname>  ::= "AACannon" | "AchievementRewardTest" | "AcolyteSoul" | "AdaptiveArmor" | "AlienHead" | "AllStatBoost" | "ArmorPlate" | "ArmorReductionOnHit" | "AttackSpeedAndMoveSpeed" | "AttackSpeedOnCrit" | "AttackSpeedPerNearbyAllyOrEnemy" | "AutoCastEquipment" | "Bandolier" | "BarrageOnBoss" | "BarrierEnemiesOnKill" | "BarrierOnCooldown" | "BarrierOnKill" | "BarrierOnOverHeal" | "Bear" | "BearVoid" | "BeetleGland" | "Behemoth" | "BleedOnHit" | "BleedOnHitAndExplode" | "BleedOnHitVoid" | "BlightZone" | "Blindness" | "BonusGoldPackOnKill" | "BoostAllStats" | "BoostAttackSpeed" | "BoostCharacterScale" | "BoostDamage" | "BoostEquipmentRecharge" | "BoostHp" | "BossDamageBonus" | "BounceNearby" | "BurnNearby" | "CausticPheromones" | "ChainLightning" | "ChainLightningVoid" | "Clover" | "CloverVoid" | "ConvertCritChanceToCritDamage" | "ConvertHealingToAttackSpeed" | "CooldownOnCrit" | "CrippleWardOnLevel" | "CritAtLowerElevation" | "CritDamage" | "CritGlasses" | "CritGlassesVoid" | "CritHeal" | "Crowbar" | "CutHp" | "Dagger" | "DeathMark" | "DelayedDamage" | "DestructibleSpawner" | "Doppelganger" | "DreamDemon" | "DrizzlePlayerHelper" | "DroneDynamiteDisplay" | "DroneUpgradeHidden" | "DroneWeapons" | "DroneWeaponsBoost" | "DroneWeaponsDisplay1" | "DroneWeaponsDisplay2" | "DronesDropDynamite" | "Duplicator" | "ElementalRingVoid" | "EliteDeathGhost" | "EmpowerAlways" | "EmpowerBuff" | "EmpowerMagazine" | "EmpowerOnKill" | "EmpowerRefresh" | "EnergizedOnEquipmentUse" | "EquipmentMagazine" | "EquipmentMagazineVoid" | "ExecuteLowHealthElite" | "ExplodeOnDeath" | "ExplodeOnDeathVoid" | "ExtraEquipment" | "ExtraLife" | "ExtraLifeConsumed" | "ExtraLifeVoid" | "ExtraLifeVoidConsumed" | "ExtraShrineItem" | "ExtraStatsOnLevelUp" | "FallBoots" | "Feather" | "FireRing" | "FireballsOnHit" | "Firework" | "FlatHealth" | "FocusConvergence" | "FragileDamageBonus" | "FragileDamageBonusConsumed" | "FreeChest" | "Ghost" | "GhostBlades" | "GhostOnKill" | "GoldOnHit" | "GoldOnHurt" | "GoldenGoose" | "GummyCloneIdentifier" | "HalfAttackSpeedHalfCooldowns" | "HalfSpeedDoubleHealth" | "HeadHunter" | "HealOnCrit" | "HealWhileSafe" | "HealingPotion" | "HealingPotionConsumed" | "HealthDecay" | "HexCurse" | "HexHaunting" | "HexMountain" | "HitOnJump" | "Hoof" | "IceRing" | "Icicle" | "IgniteOnKill" | "ImmuneToDebuff" | "IncreaseDamageOnMultiKill" | "IncreaseHealing" | "IncreasePrimaryDamage" | "IncreasedDifficulty" | "Incubator" | "Infusion" | "InvadingDoppelganger" | "ItemDropChanceOnKill" | "JumpBoost" | "JumpDamageStrike" | "Junk" | "Karma" | "KillEliteFrenzy" | "KnockBackHitEnemies" | "KnockBackHitEnemiesNoCooldown" | "Knurl" | "LaserTurbine" | "LemurianHarness" | "LevelBonus" | "LightningStrikeOnHit" | "LowHealthOrbSpawner" | "LowHealthOrbSpawnerConsumed" | "LowerHealing" | "LowerPricedChests" | "LowerPricedChestsConsumed" | "LunarBadLuck" | "LunarDagger" | "LunarPrimaryReplacement" | "LunarSecondaryReplacement" | "LunarSpecialReplacement" | "LunarSun" | "LunarTrinket" | "LunarUtilityReplacement" | "LunarWings" | "MageAttunement" | "MaxRegainableHealth" | "Medkit" | "MeteorAttackOnHighDamage" | "Mimicry" | "MinHealthPercentage" | "MinionLeash" | "MinorConstructOnKill" | "Missile" | "MissileVoid" | "MoneyLoan" | "MonsoonPlayerHelper" | "MonstersOnShrineUse" | "MoreMissile" | "MoveSpeedOnKill" | "MushShot" | "Mushroom" | "MushroomVoid" | "NearbyDamageBonus" | "NovaOnHeal" | "NovaOnLowHealth" | "OnLevelUpFreeUnlock" | "OutOfCombatArmor" | "OutOfTPRadius" | "PageClarity" | "PageClarityConsumed" | "PageDie" | "PageDieConsumed" | "PageDieCooldown" | "PagePurify" | "PagePurifyConsumed" | "PageSummonFeralToddlers" | "PageSummonFeralToddlersConsumed" | "PageTeamInvisibility" | "PageTeamInvisibilityConsumed" | "PageUpgradeItem" | "PageUpgradeItemConsumed" | "PageWarpGate" | "PageWarpGateConsumed" | "ParentEgg" | "PercentMaxHealth" | "PermanentDebuffOnHit" | "PersonalShield" | "Phasing" | "PhysicsProjectile" | "Plant" | "PlantOnHit" | "PlasmaCore" | "PrimarySkillShuriken" | "RandomDamageZone" | "RandomDebuffOnInteract" | "RandomEquipmentTrigger" | "RandomlyLunar" | "RechargeableJump" | "ReduceCharacterScale" | "ReduceDamage" | "RegeneratingScrap" | "RegeneratingScrapConsumed" | "RepeatHeal" | "RoamingItemDisable" | "RoboBallBuddy" | "SanguineChalice" | "SecondarySkillMagazine" | "Seed" | "ShareItemsWithEnemies" | "SharedSuffering" | "ShieldBooster" | "ShieldOnly" | "ShockDamageAura" | "ShockNearby" | "SiphonOnLowHealth" | "SkullCounter" | "SlipperyGround" | "SlowOnHit" | "SlowOnHitVoid" | "SpeedBoostPickup" | "SpeedOnPickup" | "SprintArmor" | "SprintBonus" | "SprintDamage" | "SprintOutOfCombat" | "SprintWisp" | "Squid" | "StatsFromScrap" | "StealBackHealth" | "StickyBomb" | "StrengthenBurn" | "StunAndPierce" | "StunChanceOnHit" | "SummonedEcho" | "Syringe" | "TPHealingNova" | "Talisman" | "TeamSizeDamageBonus" | "TeleportOnLowHealth" | "TeleportOnLowHealthConsumed" | "TeleportWhenOob" | "TempestOnKill" | "Thorns" | "TonicAffliction" | "Tooth" | "TrackEnemyDamageHidden" | "TransferDebuffOnHit" | "TreasureCache" | "TreasureCacheVoid" | "TriggerEnemyDebuffs" | "UrchinOnDeath" | "UseAmbientLevel" | "UtilitySkillMagazine" | "VengefulSpirit" | "VileCenser" | "VoidMegaCrabItem" | "VoidmanPassiveItem" | "WakeHiddenOverlay" | "WarCryOnCombat" | "WarCryOnMultiKill" | "WardOnLevel" | "WeakAssHamstring" | "WispOnKill" | "BFG" | "Blackhole" | "BossHunter" | "BurnNearby" | "Cleanse" | "CommandMissile" | "CrippleWard" | "CritOnUse" | "DeathProjectile" | "DroneBackup" | "FireBallDash" | "Fruit" | "GainArmor" | "Gateway" | "GoldGat" | "GummyClone" | "HealAndRevive" | "Jetpack" | "LifestealOnHit" | "Lightning" | "Meteor" | "Molotov" | "MultiShopCard" | "Parry" | "PassiveHealing" | "Recycle" | "Saw" | "Scanner" | "SpawnAltar" | "TeamWarCry" | "Tonic" | "VendingMachine" | "BackupDrone" | "BombardmentDrone" | "BomberDrone" | "CleanupDrone" | "CopycatDrone" | "DTGunnerDrone" | "DTHaulerDrone" | "DTHealDrone" | "Drone1" | "Drone2" | "DroneCommander" | "EmergencyDrone" | "EquipmentDrone" | "FlameDrone" | "HaulerDrone" | "JailerDrone" | "JunkDrone" | "MegaDrone" | "MissileDrone" | "RechargeDrone" | "Turret1" | "CursedBook"
<droptable> ::= "dtChest1" | "dtChest2" | "dtGoldChest" | "dtCasinoChest" | "dtSmallChestDamage" | "dtSmallChestHealing" | "dtSmallChestUtility" | "dtCategoryChest2Damage" | "dtCategoryChest2Healing" | "dtCategoryChest2Utility" | "dtLunarChest" | "dtEquipment" | "dtShrineChance" | "dtChanceDoll" | "dtLockbox" | "dtVoidChest" | "dtVoidCamp" | "dtCommandChest" | "dtMonsterTeamTier1Item" | "dtMonsterTeamTier2Item" | "dtMonsterTeamTier3Item" | "dtITDefaultWave" | "dtITBossWave" | "dtITSpecialBossWave" | "dtTemporaryItemsDistributor" | "bdtHallowedCauldron"
<tier>      ::= "Tier1" | "Tier2" | "Tier3" | "Lunar" | "Boss" | "VoidTier1" | "VoidTier2" | "VoidTier3" | "VoidBoss" | "FoodTier" | "Cursed" | "Hex" | "Page" | "Equipment" | "LunarEquipment" | "LunarItem" | "PowerShape" | "KeyItem"
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
<droptable> ::= "dtChest1" | "dtChest2" | "dtGoldChest" | "dtCasinoChest" | "dtSmallChestDamage" | "dtSmallChestHealing" | "dtSmallChestUtility" | "dtCategoryChest2Damage" | "dtCategoryChest2Healing" | "dtCategoryChest2Utility" | "dtLunarChest" | "dtEquipment" | "dtShrineChance" | "dtChanceDoll" | "dtLockbox" | "dtVoidChest" | "dtVoidCamp" | "dtCommandChest" | "dtMonsterTeamTier1Item" | "dtMonsterTeamTier2Item" | "dtMonsterTeamTier3Item" | "dtITDefaultWave" | "dtITBossWave" | "dtITSpecialBossWave" | "dtTemporaryItemsDistributor" | "bdtHallowedCauldron"
<tier>      ::= "Tier1" | "Tier2" | "Tier3" | "Lunar" | "Boss" | "VoidTier1" | "VoidTier2" | "VoidTier3" | "VoidBoss" | "FoodTier" | "Cursed" | "Hex" | "Page" | "Equipment" | "LunarEquipment" | "LunarItem" | "PowerShape" | "KeyItem"
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

This method resolves a single item key string into a pickup index. It supports parsing item keys that represent item tiers, drop tables, concrete items, or concrete equipment. It also supports blacklisting certain items from selection. As such this method only supports the `!`-operator, but no other operator.

#### Signature

```c#
bool ItemStringParser.ItemStringParser.ResolveItemKey(string itemkey, int repeat, Dictionary<PickupIndex, int> resolvedItems, ManualLogSource log)
```

`itemkey (string)`: The identifier string for the item or group of items to resolve. May exclude items from droptables or tiers with with `!`.

`availableOnly (bool, optional)`: If true will prevent concrete item names from being resolved if they are disabled or not yet unlocked. Names from a DLC that is not enabled for the run never resolve.

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

