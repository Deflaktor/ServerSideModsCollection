using BepInEx;
using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static StartBonusMod.BepConfig;

namespace StartBonusMod
{
    public class BepConfig
    {
        public static ConfigEntry<bool> Enabled;
        public static ConfigEntry<int> StartingCash;
        public static ConfigEntry<string> StartingEquipment;

        public static ConfigEntry<bool> SimpleEnabled;
        public static Dictionary<ItemTier, ConfigEntry<string>> StartingItemByTier = new Dictionary<ItemTier, ConfigEntry<string>>();
        public static Dictionary<ItemTier, ConfigEntry<int>> StartingItemByTierAmount = new Dictionary<ItemTier, ConfigEntry<int>>();

        public static ConfigEntry<bool> AdvancedEnabled;
        public static ConfigEntry<string> AdvancedItemList;
        public static ConfigEntry<string> AdvancedEquipmentList;
        public static ConfigEntry<string> AdvancedBlackList;

        public static Dictionary<string, ItemIndex> englishNameToItemIndex = new Dictionary<string, ItemIndex>();
        public static Dictionary<string, EquipmentIndex> englishNameToEquipmentIndex = new Dictionary<string, EquipmentIndex>();
        public static Dictionary<ItemTier, List<string>> itemTierToItemEnglishNames = new Dictionary<ItemTier, List<string>>();
        public static Dictionary<string, ItemIndex> englishNameToDropTable = new Dictionary<string, ItemIndex>();

        public static void Init()
        {
            var config = StartBonusMod.instance.Config;
            englishNameToItemIndex.Add("None", ItemIndex.None);
            foreach (var itemDef in ItemCatalog.allItemDefs)
            {
                var englishName = Language.GetString(itemDef.nameToken, "en");
                if(englishName.IsNullOrWhiteSpace() || englishName.Equals(itemDef.nameToken))
                {
                    englishName = itemDef.name;
                }
                englishNameToItemIndex.Add(englishName, itemDef.itemIndex);
                if (!itemTierToItemEnglishNames.TryGetValue(itemDef.tier, out var list))
                {
                    itemTierToItemEnglishNames[itemDef.tier] = list = new List<string>();
                }
                list.Add(englishName);
            }
            foreach (var (itemTier, englishNamesList) in itemTierToItemEnglishNames)
            {
                englishNamesList.Sort();
                englishNamesList.Insert(0, "None");
                englishNamesList.Insert(1, "Random");
            }
            englishNameToEquipmentIndex.Add("None", EquipmentIndex.None);
            foreach (var equipmentDef in EquipmentCatalog.equipmentDefs)
            {
                var englishName = Language.GetString(equipmentDef.nameToken, "en");
                if (englishName.IsNullOrWhiteSpace() || englishName.Equals(equipmentDef.nameToken))
                {
                    englishName = equipmentDef.name;
                }
                englishNameToEquipmentIndex.Add(englishName, equipmentDef.equipmentIndex);
            }
            // --- Start Bonus ---
            Enabled = config.Bind("Main", "Enabled", true, "Enable Mod");
            StartingCash = config.Bind("Main", "Cash", 500, new ConfigDescription("How much starting cash each player receives."));

            // --- Simple Item List ---
            SimpleEnabled = config.Bind("Simple Item List", "SectionEnabled", false, new ConfigDescription("Whether to enable Simple Start Bonus Item List"));

            var sortedEquipmentList = englishNameToEquipmentIndex.Keys.ToList();
            sortedEquipmentList.Remove("None");
            sortedEquipmentList.Sort();
            sortedEquipmentList.Insert(0, "None");
            sortedEquipmentList.Insert(1, "Random");
            AcceptableValueList<string> acceptableEquipmentList = new AcceptableValueList<string>(sortedEquipmentList.ToArray());
            StartingEquipment = config.Bind("Simple Item List", "Equipment", "None", new ConfigDescription("Which equipment each player shall receive at the start.", acceptableEquipmentList));

            foreach (var tierDef in ItemTierCatalog.allItemTierDefs)
            {
                AcceptableValueList<string> acceptableItemList = new AcceptableValueList<string>(itemTierToItemEnglishNames[tierDef.tier].ToArray());
                StartingItemByTier[tierDef.tier] = config.Bind("Simple Item List", $"{tierDef.tier.ToString()} Item", "None", new ConfigDescription($"Which {tierDef.tier.ToString()} item each player shall receive at the start.", acceptableItemList));
                StartingItemByTierAmount[tierDef.tier] = config.Bind("Simple Item List", $"{tierDef.tier.ToString()} Amount", 0, new ConfigDescription($"How many of the {tierDef.tier.ToString()} item each player shall receive."));
            }

            // --- Advanced Item List ---
            AdvancedEnabled = config.Bind("Advanced Item List", "SectionEnabled", false, new ConfigDescription("Whether to enable Advanced Start Bonus Item List"));
            var items = "2xHealWhileSafe & Tier3 & 2xLunar & 4xdtChest2";
            var itemTiersString = "Tier1, Tier2, Tier3, Lunar, Boss, VoidTier1, VoidTier2, VoidTier3, VoidBoss";
            var itemKeyWordsExplanation = $"Can use:\n - internal item names(see https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Developer-Reference/Items-and-Equipments-Data/)\n- item tier keywords ({itemTiersString})\n- droptable names (see README.md)\nExample: {items}";
            AdvancedItemList = config.Bind("Advanced Item List", "ItemList", "2xdtChest1 & Tier2", $"List of items in the format <amount1>x<item1> & <amount2>x<item2>. See README.md for more detailed description. {itemKeyWordsExplanation}");
            AdvancedEquipmentList = config.Bind("Advanced Item List", "EquipmentList", "EliteFireEquipment: 0.1 | Recycle: 0.9", $"List of equipments in the format <equip1>:<weight> | <equip2>:<weight>. Amount has no effect, only the first resolved equipment is used. See README.md for more detailed description. {itemKeyWordsExplanation}");
            AdvancedBlackList = config.Bind("Advanced Item List", "BlackList", "LunarBadLuck, GoldOnHit", $"Comma-separated list of internal item names. Items listed here will not be resolved from item tiers or droptables. The blacklist has no effect on listed concrete item names.");

            // --- InLobbyConfig ---
            if (ModCompatibilityInLobbyConfig.enabled)
            {
                ModCompatibilityInLobbyConfig.CreateFromBepInExConfigFile(config, "Start Bonus");
            }
        }
    }
}