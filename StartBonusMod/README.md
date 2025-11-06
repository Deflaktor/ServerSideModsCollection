# StartBonusMod

This mod gives every player a configurable set of items and starting cash at the beginning of a run. 
I made it so that I can use [StartInBazaar](https://thunderstore.io/package/MagnusMagnuson/StartInBazaar/) in combination with [BiggerBazaar](https://thunderstore.io/package/MagnusMagnuson/BiggerBazaar/) and/or [BazaarIsMyHaven](https://thunderstore.io/package/Def/BazaarIsMyHaven/).
This mod is also compatible with [InLobbyConfig](https://thunderstore.io/package/KingEnderBrine/InLobbyConfig/).

## Advanced Start Bonus Item List

- `ItemList`:
  - Defines what items players start with.
  - Items can be individual names, tiers, drop tables, or grouped sets with operators.
  - Separate item groups into AND lists (`&` operator) or OR lists (`|` operator), which cannot mix at the same level.
  - Example: `5xTier1 & 3xTier2` - Players start with 5 whites and 3 greens.
- `EquipmentList`:
  - Same format as `ItemList`.
  - Only the first resolved equipment is granted to the player. I.e. operators `x`, `*` and `&` have no effect.
  - Example: `EliteFireEquipment: 0.1 | Recycle: 0.9` - Players start with Ifrit's Distinction with 10% chance and with 90% chance with a Recycler.
- `BlackList`: 
  - Comma-separated list of internal item names. Items listed here will not be resolved from item tiers or droptables. The blacklist has no effect on listed concrete item names.
  - Example: `LunarBadLuck, GoldOnHit`

### ItemList Operators

Each entry in the itemlist shall follow the format: `<repeat>x<itemkey>*<multiplier>: <weight>`.

#### `&` (And-Operator)
- **Purpose:** Every item separated by `&` will be granted.
- **Example:**
  `Hoof & Boss` - The player receives both a Hoof and a random Boss item.

#### `|` (Or-Operator)
- **Purpose:** Only one of the items separated by `|` will be chosen at random.
- **Weights:** You can specify selection weights using a colon `:` after the item (e.g., `Tier1: 0.5 | Tier2: 0.5`).
- **Example:**
  `Tier1 | Tier2` - The player gets either a Tier1 or a Tier2 item (equal chance unless weighted).

#### `:` (Weight Separator)
- **Purpose:** Assigns probability weights to options with the Or-Operator (`|`).
- **Example:**
  `Tier1: 0.6 | Tier2: 0.4` means Tier1 has a 60% chance, Tier2 a 40% chance.

#### `x` (Repeat-Operator)
- **Purpose:** Repeats the immediately following item or group a fixed number of times.
- **Placement:** Directly *before* a group or item.
- **Example:**  
  `5xHoof` - The player gets 5 Hoof items.
  `3x{ Tier1 | Tier2 }` - The player gets 3 times a randomly selected item from Tier1 or Tier2.

#### `*` (Multiplier-Operator)
- **Purpose:** Multiplies the amount of granted items of a group.
- **Placement:** Directly *after* a group or item.
- **Example:**
  `Hoof*5` - The player gets 5 Hoof items.
  `{ Tier1 | Tier2 }*3` - The player gets 3 copies of a single type of item from either Tier1 or Tier2.

#### `{` and `}` (Grouping Symbols)
- **Purpose:** Group items and operators together. Can use `&` and `|` within a group, but they cannot be mixed.
- **Operator precedence:** Use `{ }` to explicitly control grouping and order of operations, especially when combining repeat, and, or multiply.
- **Nested groups:** You can nest `{}` for more complex item generation logic.
- **Example:**
  `{ 2xdtChest1 | dtChest2 }` - Represents a choice between "2x small chest" and "1 large chest" as a single entity; useful with other operators.

### Summary Table

| Operator | Symbol | Description                                                                             | Example                           |
|----------|--------|-----------------------------------------------------------------------------------------|-----------------------------------|
| And      | `&`    | All items, together                                                                     | `A & B`                           |
| Or       | `|`    | Only one item, random (optional weight via `:`)                                         | `A | B:0.7 | C:0.3`               |
| Weight   | `:`    | Specifies probability weight for options when using `|`                                 | `A:0.6 | B:0.4`                   |
| Repeat   | `x`    | Repeats the following item/group N times                                                | `5xA` or `3x{A | B}`              |
| Multiply | `*`    | Multiplies the amount of granted items in the group by N                                | `{A & B}*4`                       |
| Grouping | `{}`   | Groups items/expressions to control operation order                                     | `{A & 2xB}`                       |

### Examples

The player shall get either 5 whites or 3 greens or 1 red:
```
ItemList = 5xTier1 | 3xTier2 | Tier3
```

The player shall get 10 random items which shall consist of around 50% whites, around 35% greens and around 15% red:
```
ItemList = 10x{ Tier1: 0.5 | Tier2: 0.35 | Tier3: 0.15 }
```

The player shall get 5 hooves and 1 random boss item:
```
ItemList = 5xHoof & Boss
```

The player shall get 5 times one of the following: Either the contents of two small chests or the content of 1 large chest
```
ItemList = 5x{ 2xdtChest1 | dtChest2 }
```

The player shall get a random lunar item, except for Light Flux Pauldron or Stone Flux Pauldron:
```
ItemList = Lunar
BlackList = HalfSpeedDoubleHealth, HalfAttackSpeedHalfCooldowns
```

The player shall get a random amount of whites.
```
ItemList = 1xTier1 | 2xTier1 | 3xTier1 | 4xTier1 | 5xTier1
```

The player shall get 5 copies of two random whites or 4 copies of a random green.
```
ItemList = 2xTier1*5 | Tier2*4
```

The player shall get either 5 Bison Steaks and 5 Hooves or with 1 Infusion and 1 Feather:
```
ItemList = { 5xFlatHealth & 5xHoof } | { Infusion & Feather }
```

The player shall get either 5 Bison Steaks and 5 Hooves with 75% probability or 1 Infusion and 1 Feather with 25% probability:
```
ItemList = { FlatHealth & Hoof }*5: 0.75 | { Infusion & Feather }*2: 0.25
```

## Item Keywords

You can use:
- **Internal item names** (see [R2Wiki - Items-and-Equipments-Data](https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Developer-Reference/Items-and-Equipments-Data/)).  
- **Item Tier names**: `Tier1`, `Tier2`, `Tier3`, `Lunar`, `Boss`, `VoidTier1`, `VoidTier2`, `VoidTier3`, `VoidBoss`.
- **Droptable names**: e.g. `dtChest1`, `dtLunarChest`, `dtVoidChest`. See below.

### Droptable Names

Here is a list of supported droptables:

|                                 | canDropBeReplaced | requiredItemTags | bannedItemTags | tier1Weight | tier2Weight | tier3Weight | bossWeight | lunarEquipmentWeight | lunarItemWeight | lunarCombinedWeight | equipmentWeight | voidTier1Weight | voidTier2Weight | voidTier3Weight | voidBossWeight |
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

```
<ItemList>       ::= <AND_LIST> | <OR_LIST>
<AND_LIST>       ::= <ENTRY> (" & " <ENTRY>)*
<OR_LIST>        ::= <WEIGHTED_ENTRY> (" | " <WEIGHTED_ENTRY>)*
<ENTRY>          ::= (<rep> "x")? <ITEM_KEY> ("*" <mult>)?
<WEIGHTED_ENTRY> ::= <ENTRY> (": " <weight>)?
<ITEM_KEY>       ::= <tier> | <itemname> | <droptable> | "{ " <AND_LIST> " }" | "{ " <OR_LIST> " }"

<itemname>  ::= "AlienHead" | "ArmorPlate" | "ArmorReductionOnHit" | "AttackSpeedAndMoveSpeed" | "AttackSpeedOnCrit" | "AutoCastEquipment" | "Bandolier" | "BarrierOnKill" | "BarrierOnOverHeal" | "Bear" | "BearVoid" | "BeetleGland" | "Behemoth" | "BleedOnHit" | "BleedOnHitAndExplode" | "BleedOnHitVoid" | "BonusGoldPackOnKill" | "BoostAllStats" | "BossDamageBonus" | "BounceNearby" | "ChainLightning" | "ChainLightningVoid" | "Clover" | "CloverVoid" | "CritDamage" | "CritGlasses" | "CritGlassesVoid" | "Crowbar" | "Dagger" | "DeathMark" | "DelayedDamage" | "DroneWeapons" | "ElementalRingVoid" | "EnergizedOnEquipmentUse" | "EquipmentMagazine" | "EquipmentMagazineVoid" | "ExecuteLowHealthElite" | "ExplodeOnDeath" | "ExplodeOnDeathVoid" | "ExtraLife" | "ExtraLifeVoid" | "ExtraShrineItem" | "ExtraStatsOnLevelUp" | "FallBoots" | "Feather" | "FireRing" | "FireballsOnHit" | "Firework" | "FlatHealth" | "FocusConvergence" | "FragileDamageBonus" | "FreeChest" | "GhostOnKill" | "GoldOnHit" | "GoldOnHurt" | "GoldOnStageStart" | "HalfAttackSpeedHalfCooldowns" | "HalfSpeedDoubleHealth" | "HeadHunter" | "HealOnCrit" | "HealWhileSafe" | "HealingPotion" | "Hoof" | "IceRing" | "Icicle" | "IgniteOnKill" | "ImmuneToDebuff" | "IncreaseDamageOnMultiKill" | "IncreaseHealing" | "IncreasePrimaryDamage" | "Infusion" | "JumpBoost" | "KillEliteFrenzy" | "KnockBackHitEnemies" | "Knurl" | "LaserTurbine" | "LightningStrikeOnHit" | "LowerHealthHigherDamage" | "LowerPricedChests" | "LunarBadLuck" | "LunarDagger" | "LunarPrimaryReplacement" | "LunarSecondaryReplacement" | "LunarSpecialReplacement" | "LunarSun" | "LunarTrinket" | "LunarUtilityReplacement" | "Medkit" | "MeteorAttackOnHighDamage" | "MinorConstructOnKill" | "Missile" | "MissileVoid" | "MonstersOnShrineUse" | "MoreMissile" | "MoveSpeedOnKill" | "Mushroom" | "MushroomVoid" | "NearbyDamageBonus" | "NegateAttack" | "NovaOnHeal" | "NovaOnLowHealth" | "OnLevelUpFreeUnlock" | "OutOfCombatArmor" | "ParentEgg" | "Pearl" | "PermanentDebuffOnHit" | "PersonalShield" | "Phasing" | "Plant" | "PrimarySkillShuriken" | "RandomDamageZone" | "RandomEquipmentTrigger" | "RandomlyLunar" | "RegeneratingScrap" | "RepeatHeal" | "ResetChests" | "RoboBallBuddy" | "ScrapGreen" | "ScrapRed" | "ScrapWhite" | "ScrapYellow" | "SecondarySkillMagazine" | "Seed" | "ShieldOnly" | "ShinyPearl" | "ShockNearby" | "SiphonOnLowHealth" | "SlowOnHit" | "SlowOnHitVoid" | "SprintArmor" | "SprintBonus" | "SprintOutOfCombat" | "SprintWisp" | "Squid" | "StickyBomb" | "StrengthenBurn" | "StunAndPierce" | "StunChanceOnHit" | "Syringe" | "TPHealingNova" | "Talisman" | "TeleportOnLowHealth" | "Thorns" | "TitanGoldDuringTP" | "Tooth" | "TreasureCache" | "TreasureCacheVoid" | "TriggerEnemyDebuffs" | "UtilitySkillMagazine" | "VoidMegaCrabItem" | "WarCryOnMultiKill" | "WardOnLevel"
<droptable> ::= "dtMonsterTeamTier1Item" | "dtMonsterTeamTier2Item" | "dtMonsterTeamTier3Item" | "dtSacrificeArtifact" | "dtAISafeTier1Item" | "dtAISafeTier2Item" | "dtAISafeTier3Item" | "dtEquipment" | "dtTier1Item" | "dtTier2Item" | "dtTier3Item" | "dtVoidChest" | "dtCasinoChest" | "dtSmallChestDamage" | "dtSmallChestHealing" | "dtSmallChestUtility" | "dtChest1" | "dtChest2" | "dtDuplicatorTier1" | "dtDuplicatorTier2" | "dtDuplicatorTier3" | "dtDuplicatorWild" | "dtGoldChest" | "dtLunarChest" | "dtShrineChance" | "dtLockbox" | "dtITBossWave" | "dtITDefaultWave"  | "dtITLunar" | "dtITSpecialBossWave" | "dtITVoid" | "dtCategoryChest2Damage" | "dtCategoryChest2Healing" | "dtCategoryChest2Utility" | "dtVoidCamp" | "dtVoidTriple" | "dtVoidLockbox" | "AurelioniteHeartPickupDropTable" | "GeodeRewardDropTable" | "dtShrineHalcyoniteTier1" | "dtShrineHalcyoniteTier2" | "dtShrineHalcyoniteTier3" | "dtChanceDoll" | "dtSonorousEcho" | "dtCommandChest"
<tier>      ::= "Tier1" | "Tier2" | "Tier3" | "Lunar" | "Boss" | "VoidTier1" | "VoidTier2" | "VoidTier3" | "VoidBoss"
<rep>       ::= <int>
<mult>      ::= <int>
<weight>    ::= <float>

<int>   ::= [1-9] [0-9]*
<float> ::= <pos> | <neg>
<pos>   ::= ("0" | <int>) ("." [0-9]+ )?
<neg>   ::= "-" <pos>
```