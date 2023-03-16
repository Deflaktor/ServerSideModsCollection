using BepInEx.Configuration;
using RoR2;
using UnityEngine;
using R2API;
using RiskOfOptions.Options;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions;
using System.Collections.Generic;
using static StartBonusMod.BepConfig;
using System.Linq;
using static StartBonusMod.EnumCollection;

namespace StartBonusMod
{
    public class BepConfig
    {
        // start bonus
        public static ConfigEntry<int> StartingCash;
        public static ConfigEntry<ItemWhiteEnum> StartingItemWhite;
        public static ConfigEntry<int> StartingItemWhiteCount;
        public static ConfigEntry<ItemGreenEnum> StartingItemGreen;
        public static ConfigEntry<int> StartingItemGreenCount;
        public static ConfigEntry<ItemRedEnum> StartingItemRed;
        public static ConfigEntry<int> StartingItemRedCount;
        public static ConfigEntry<ItemBossEnum> StartingItemBoss;
        public static ConfigEntry<int> StartingItemBossCount;
        public static ConfigEntry<ItemLunarEnum> StartingItemLunar;
        public static ConfigEntry<int> StartingItemLunarCount;
        public static ConfigEntry<ItemVoidEnum> StartingItemVoid;
        public static ConfigEntry<int> StartingItemVoidCount;
        public static ConfigEntry<ItemEquipEnum> StartingItemEquip;

        public static void Init()
        {
            var config = StartBonusMod.instance.Config;

            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions"))
            {
                ModSettingsManager.SetModDescription(@"
<size=200%><uppercase><align=center><color=#adf2fa>Start Bonus Mod</color></align></uppercase></size>
");
            }
            // --- Start Bonus ---
            {
                StartingCash = config.Bind("Start Bonus", "Cash", 500, new ConfigDescription("How much starting cash each player receives."));
                StartingItemWhite = config.Bind("Start Bonus", "White Item", ItemWhiteEnum.None, new ConfigDescription("Which white item each player shall receive at the start."));
                StartingItemWhiteCount = config.Bind("Start Bonus", "White Item Count", 1, new ConfigDescription("How many of the white item each player shall receive."));
                StartingItemGreen = config.Bind("Start Bonus", "Green Item", ItemGreenEnum.None, new ConfigDescription("Which green item each player shall receive at the start."));
                StartingItemGreenCount = config.Bind("Start Bonus", "Green Item Count", 1, new ConfigDescription("How many of the green item each player shall receive."));
                StartingItemRed = config.Bind("Start Bonus", "Red Item", ItemRedEnum.None, new ConfigDescription("Which red item each player shall receive at the start."));
                StartingItemRedCount = config.Bind("Start Bonus", "Red Item Count", 1, new ConfigDescription("How many of the red item each player shall receive."));
                StartingItemBoss = config.Bind("Start Bonus", "Boss Item", ItemBossEnum.None, new ConfigDescription("Which boss item each player shall receive at the start."));
                StartingItemBossCount = config.Bind("Start Bonus", "Boss Item Count", 1, new ConfigDescription("How many of the boss item each player shall receive."));
                StartingItemLunar = config.Bind("Start Bonus", "Lunar Item", ItemLunarEnum.None, new ConfigDescription("Which lunar item each player shall receive at the start."));
                StartingItemLunarCount = config.Bind("Start Bonus", "Lunar Item Count", 1, new ConfigDescription("How many of the lunar item each player shall receive."));
                StartingItemVoid = config.Bind("Start Bonus", "Void Item", ItemVoidEnum.None, new ConfigDescription("Which void item each player shall receive at the start."));
                StartingItemVoidCount = config.Bind("Start Bonus", "Void Item Count", 1, new ConfigDescription("How many of the void item each player shall receive."));
                StartingItemEquip = config.Bind("Start Bonus", "Equipment", ItemEquipEnum.None, new ConfigDescription("Which equipment each player shall receive at the start."));

                if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions"))
                {
                    ModSettingsManager.AddOption(new IntSliderOption(StartingCash, new IntSliderConfig() { min = -1, max = 1000, restartRequired = false } ));
                    ModSettingsManager.AddOption(new ChoiceOption(StartingItemWhite, new ChoiceConfig()));
                    ModSettingsManager.AddOption(new IntSliderOption(StartingItemWhiteCount, new IntSliderConfig() { min = 0, max = 10, restartRequired = false, checkIfDisabled = () => StartingItemWhite.Value == ItemWhiteEnum.None }));
                    ModSettingsManager.AddOption(new ChoiceOption(StartingItemGreen, new ChoiceConfig()));
                    ModSettingsManager.AddOption(new IntSliderOption(StartingItemGreenCount, new IntSliderConfig() { min = 0, max = 10, restartRequired = false, checkIfDisabled = () => StartingItemGreen.Value == ItemGreenEnum.None }));
                    ModSettingsManager.AddOption(new ChoiceOption(StartingItemRed, new ChoiceConfig()));
                    ModSettingsManager.AddOption(new IntSliderOption(StartingItemRedCount, new IntSliderConfig() { min = 0, max = 10, restartRequired = false, checkIfDisabled = () => StartingItemRed.Value == ItemRedEnum.None }));
                    ModSettingsManager.AddOption(new ChoiceOption(StartingItemBoss, new ChoiceConfig()));
                    ModSettingsManager.AddOption(new IntSliderOption(StartingItemBossCount, new IntSliderConfig() { min = 0, max = 10, restartRequired = false, checkIfDisabled = () => StartingItemBoss.Value == ItemBossEnum.None }));
                    ModSettingsManager.AddOption(new ChoiceOption(StartingItemLunar, new ChoiceConfig()));
                    ModSettingsManager.AddOption(new IntSliderOption(StartingItemLunarCount, new IntSliderConfig() { min = 0, max = 10, restartRequired = false, checkIfDisabled = () => StartingItemLunar.Value == ItemLunarEnum.None }));
                    ModSettingsManager.AddOption(new ChoiceOption(StartingItemVoid, new ChoiceConfig()));
                    ModSettingsManager.AddOption(new IntSliderOption(StartingItemVoidCount, new IntSliderConfig() { min = 0, max = 10, restartRequired = false, checkIfDisabled = () => StartingItemVoid.Value == ItemVoidEnum.None }));
                    ModSettingsManager.AddOption(new ChoiceOption(StartingItemEquip, new ChoiceConfig()));
                }
            }
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.KingEnderBrine.InLobbyConfig"))
            {
                var configEntry = InLobbyConfig.Fields.ConfigFieldUtilities.CreateFromBepInExConfigFile(config, "Server-side Mods Collection");
                InLobbyConfig.ModConfigCatalog.Add(configEntry);
            }
        }
    }
}