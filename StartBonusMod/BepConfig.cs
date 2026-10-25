using BepInEx;
using BepInEx.Configuration;
using RoR2;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace StartBonusMod
{
    public class BepConfig
    {
        public static ConfigEntry<bool> Enabled;

        public static ConfigEntry<bool> StartingCashEnabled;
        public static ConfigEntry<int> StartingCash;

        public static ConfigEntry<bool> SimpleEnabled;
        public static Dictionary<ItemTier, ConfigEntry<string>> StartingItemByTier = new Dictionary<ItemTier, ConfigEntry<string>>();
        public static Dictionary<ItemTier, ConfigEntry<int>> StartingItemByTierAmount = new Dictionary<ItemTier, ConfigEntry<int>>();
        public static ConfigEntry<string> StartingEquipment;

        public static ConfigEntry<bool> AdvancedEnabled;
        public static ConfigEntry<string> AdvancedItemList;

        public static Dictionary<string, ItemIndex> englishNameToItemIndex = new Dictionary<string, ItemIndex>();
        public static Dictionary<string, EquipmentIndex> englishNameToEquipmentIndex = new Dictionary<string, EquipmentIndex>();
        public static Dictionary<ItemTier, List<string>> itemTierToItemEnglishNames = new Dictionary<ItemTier, List<string>>();
        // for migrating choices saved before 3.1.0
        private static Dictionary<string, string> internalNameToChoice = new Dictionary<string, string>();

        private static bool initialized = false;

        public static void Init()
        {
            if(initialized) 
                return;
            initialized = true;
            var config = StartBonusMod.instance.Config;
            englishNameToItemIndex.Add("None", ItemIndex.None);
            foreach (var itemDef in ItemCatalog.allItemDefs)
            {
                if (itemDef == null || itemDef.nameToken == null) continue;
                var englishName = GetChoiceName(itemDef.nameToken, itemDef.name);
                englishNameToItemIndex.Add(englishName, itemDef.itemIndex);
                internalNameToChoice[itemDef.name] = englishName;
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
                if (equipmentDef == null || equipmentDef.nameToken == null) continue;
                var englishName = GetChoiceName(equipmentDef.nameToken, equipmentDef.name);
                englishNameToEquipmentIndex.Add(englishName, equipmentDef.equipmentIndex);
                internalNameToChoice[equipmentDef.name] = englishName;
            }
            // --- Main ---
            Enabled = ModCompatibilityConfigPresets.Bind(config, "00 Main", "Enabled", true, "Enable Mod");

            // --- Start Cash ---
            StartingCashEnabled = ModCompatibilityConfigPresets.Bind(config, "01 Cash", "SectionEnabled", false, new ConfigDescription("If starting cash shall be given to the player."));
            StartingCash = ModCompatibilityConfigPresets.Bind(config, "01 Cash", "Cash", 0, new ConfigDescription("How much starting cash each player receives."));

            // --- Simple Item List ---
            SimpleEnabled = ModCompatibilityConfigPresets.Bind(config, "02 Simple Item List", "SectionEnabled", false, new ConfigDescription("Whether to enable Simple Start Bonus Item List"));

            var sortedEquipmentList = englishNameToEquipmentIndex.Keys.ToList();
            sortedEquipmentList.Remove("None");
            sortedEquipmentList.Sort();
            sortedEquipmentList.Insert(0, "None");
            sortedEquipmentList.Insert(1, "Random");
            StartingEquipment = BindChoice(config, "02 Simple Item List", "Equipment", "Which equipment each player shall receive at the start.", sortedEquipmentList);

            foreach (var tierDef in ItemTierCatalog.allItemTierDefs)
            {
                // a tier without items, e.g. from another mod, gets no setting
                if (!itemTierToItemEnglishNames.TryGetValue(tierDef.tier, out var tierItemNames))
                    continue;
                StartingItemByTier[tierDef.tier] = BindChoice(config, "02 Simple Item List", $"{tierDef.tier.ToString()} Item", $"Which {tierDef.tier.ToString()} item each player shall receive at the start.", tierItemNames);
                StartingItemByTierAmount[tierDef.tier] = ModCompatibilityConfigPresets.Bind(config, "02 Simple Item List", $"{tierDef.tier.ToString()} Amount", 0, new ConfigDescription($"How many of the {tierDef.tier.ToString()} item each player shall receive."));
            }

            // --- Advanced Item List ---
            AdvancedEnabled = ModCompatibilityConfigPresets.Bind(config, "03 Advanced Item List", "SectionEnabled", false, new ConfigDescription("Whether to enable Advanced Start Bonus Item List"));
            var items = "2xHealWhileSafe & Tier3 & 2xLunar & 4xdtChest2";
            var itemTiersString = "Tier1, Tier2, Tier3, Lunar, Boss, VoidTier1, VoidTier2, VoidTier3, VoidBoss, FoodTier, Cursed, Hex, Page";
            var dropListsString = "Equipment, LunarEquipment, LunarItem, PowerShape, KeyItem";
            var itemKeyWordsExplanation = $"Can use:\n - internal item, equipment, drone and key item names (see https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Developer-Reference/Items-and-Equipments-Data/)\n- item tier keywords ({itemTiersString})\n- other drop lists of the run ({dropListsString})\n- droptable names (see README.md)\nExample: {items}";
            AdvancedItemList = ModCompatibilityConfigPresets.Bind(config, "03 Advanced Item List", "ItemList", "2xdtChest1 & Tier2", $"List of items in the format <amount1>x<item1> & <amount2>x<item2>. See README.md for more detailed description. {itemKeyWordsExplanation}");

            // --- InLobbyConfig ---
            if (ModCompatibilityInLobbyConfig.enabled)
            {
                ModCompatibilityInLobbyConfig.CreateFromBepInExConfigFile(config, "Start Bonus");
            }
        }

        // "English name (internal name)", or the internal name alone. Unlike the item and equipment indices, the internal
        // name stays the same when the game or another mod adds items.
        private static string GetChoiceName(string nameToken, string internalName)
        {
            var englishName = Language.GetString(nameToken, "en");
            if (string.IsNullOrWhiteSpace(englishName) || englishName.Equals(nameToken))
            {
                return internalName;
            }
            return $"{englishName} ({internalName})";
        }

        // Before 3.1.0 a choice was saved with its index, as "English name (index: internal name)" or "index: internal name".
        // Binding with the choices would reset such a value to None, so it is read without them first.
        private static ConfigEntry<string> BindChoice(ConfigFile config, string section, string key, string description, List<string> choices)
        {
            var definition = new ConfigDefinition(section, key);
            var saved = config.Bind(definition, "None", new ConfigDescription(description)).Value;
            config.Remove(definition);
            var entry = ModCompatibilityConfigPresets.Bind(config, definition, "None", new ConfigDescription(description, new AcceptableValueList<string>(choices.ToArray())));
            if (choices.Contains(saved))
            {
                ModCompatibilityConfigPresets.PlayerEntry(entry).Value = saved;
            }
            else
            {
                var match = Regex.Match(saved ?? "", @"(?:^|\()\d+: ([^()]+)\)?$");
                if (match.Success && internalNameToChoice.TryGetValue(match.Groups[1].Value, out var choice) && choices.Contains(choice))
                {
                    ModCompatibilityConfigPresets.PlayerEntry(entry).Value = choice;
                }
            }
            return entry;
        }
    }
}