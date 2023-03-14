using BepInEx.Configuration;
using RoR2;
using UnityEngine;
using R2API;
using RiskOfOptions.Options;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions;
using System.Collections.Generic;
using static ServerSideModsCollection.BepConfig;
using System.Linq;
using static ServerSideModsCollection.EnumCollection;

namespace ServerSideModsCollection
{
    public class BepConfig
    {
        // artifacts
        public static ConfigEntry<ArtifactEnum> Artifact1;
        public static ConfigEntry<int> Artifact1StartWave;
        public static ConfigEntry<int> Artifact1EndWave;

        public static ConfigEntry<ArtifactEnum> Artifact2;
        public static ConfigEntry<int> Artifact2StartWave;
        public static ConfigEntry<int> Artifact2EndWave;

        public static ConfigEntry<ArtifactEnum> Artifact3;
        public static ConfigEntry<int> Artifact3StartWave;
        public static ConfigEntry<int> Artifact3EndWave;

        public static ConfigEntry<ArtifactEnum> Artifact4;
        public static ConfigEntry<int> Artifact4StartWave;
        public static ConfigEntry<int> Artifact4EndWave;

        // difficulty
        public static ConfigEntry<float> DifficultyMultiplier1;
        public static ConfigEntry<int> DifficultyMultiplier1StartWave;
        public static ConfigEntry<int> DifficultyMultiplier1EndWave;

        public static ConfigEntry<float> DifficultyMultiplier2;
        public static ConfigEntry<int> DifficultyMultiplier2StartWave;
        public static ConfigEntry<int> DifficultyMultiplier2EndWave;

        public static ConfigEntry<float> DifficultyMultiplier3;
        public static ConfigEntry<int> DifficultyMultiplier3StartWave;
        public static ConfigEntry<int> DifficultyMultiplier3EndWave;

        public static ConfigEntry<float> DifficultyMultiplier4;
        public static ConfigEntry<int> DifficultyMultiplier4StartWave;
        public static ConfigEntry<int> DifficultyMultiplier4EndWave;

        // simulacrum tweaks
        public static ConfigEntry<bool> RemoveFogInBazaar;
        public static ConfigEntry<bool> UseNormalStages;
        public static ConfigEntry<StageEnum> FinalStage;
        public static ConfigEntry<int> FinalStageStartWave;
        public static ConfigEntry<int> FinalStageLunarCoinsReward;
        public static ConfigEntry<bool> FixSimulacrumBazaar;
        public static ConfigEntry<bool> FixSimulacrumSpawnPointsInNormalStages;
        public static ConfigEntry<bool> NoMissionsInVoidFields;
        public static ConfigEntry<bool> NoMissionsInVoidLocus;

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
            var config = ServerSideModsCollection.instance.Config;

            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions"))
            {
                ModSettingsManager.SetModDescription(@"
<size=200%><uppercase><align=center><color=#adf2fa>Server-side Mods Collection</color></align></uppercase></size>
<size=80%>Provides various server side settings to control.</size>

<b><color=#CECE00>### WARNING ###\nSettings cannot be changed during a run.</color></b>
");
            }

            // --- Artifacts ---
            {
                Artifact1 = config.Bind("Simulacrum Artifacts", "Artifact A", ArtifactEnum.Honor, new ConfigDescription("Which artifact to add."));
                Artifact1StartWave = config.Bind("Simulacrum Artifacts", "Artifact A Start Wave", 56, new ConfigDescription("At which wave to add this artifact."));
                Artifact1EndWave = config.Bind("Simulacrum Artifacts", "Artifact A End Wave", 60, new ConfigDescription("After which wave to remove this artifact."));

                Artifact2 = config.Bind("Simulacrum Artifacts", "Artifact B", ArtifactEnum.Dissonance, new ConfigDescription("Which artifact to add."));
                Artifact2StartWave = config.Bind("Simulacrum Artifacts", "Artifact B Start Wave", 56, new ConfigDescription("At which wave to add this artifact."));
                Artifact2EndWave = config.Bind("Simulacrum Artifacts", "Artifact B End Wave", 60, new ConfigDescription("After which wave to remove this artifact."));

                Artifact3 = config.Bind("Simulacrum Artifacts", "Artifact C", ArtifactEnum.None, new ConfigDescription("Which artifact to add."));
                Artifact3StartWave = config.Bind("Simulacrum Artifacts", "Artifact C Start Wave", 50, new ConfigDescription("At which wave to add this artifact."));
                Artifact3EndWave = config.Bind("Simulacrum Artifacts", "Artifact C End Wave", 50, new ConfigDescription("After which wave to remove this artifact."));

                Artifact4 = config.Bind("Simulacrum Artifacts", "Artifact D", ArtifactEnum.None, new ConfigDescription("Which artifact to add."));
                Artifact4StartWave = config.Bind("Simulacrum Artifacts", "Artifact D Start Wave", 50, new ConfigDescription("At which wave to add this artifact."));
                Artifact4EndWave = config.Bind("Simulacrum Artifacts", "Artifact D End Wave", 50, new ConfigDescription("After which wave to remove this artifact."));

                if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions"))
                {
                    ModSettingsManager.AddOption(new ChoiceOption(Artifact1, new ChoiceConfig()));
                    ModSettingsManager.AddOption(new IntSliderOption(Artifact1StartWave, new IntSliderConfig() { min = 0, max = 100, restartRequired = false, checkIfDisabled = () => Artifact1.Value == ArtifactEnum.None }));
                    ModSettingsManager.AddOption(new IntSliderOption(Artifact1EndWave, new IntSliderConfig() { min = 0, max = 100, restartRequired = false, checkIfDisabled = () => Artifact1.Value == ArtifactEnum.None }));
                    ModSettingsManager.AddOption(new ChoiceOption(Artifact2, new ChoiceConfig()));
                    ModSettingsManager.AddOption(new IntSliderOption(Artifact2StartWave, new IntSliderConfig() { min = 0, max = 100, restartRequired = false, checkIfDisabled = () => Artifact2.Value == ArtifactEnum.None }));
                    ModSettingsManager.AddOption(new IntSliderOption(Artifact2EndWave, new IntSliderConfig() { min = 0, max = 100, restartRequired = false, checkIfDisabled = () => Artifact2.Value == ArtifactEnum.None }));
                    ModSettingsManager.AddOption(new ChoiceOption(Artifact3, new ChoiceConfig()));
                    ModSettingsManager.AddOption(new IntSliderOption(Artifact3StartWave, new IntSliderConfig() { min = 0, max = 100, restartRequired = false, checkIfDisabled = () => Artifact3.Value == ArtifactEnum.None }));
                    ModSettingsManager.AddOption(new IntSliderOption(Artifact3EndWave, new IntSliderConfig() { min = 0, max = 100, restartRequired = false, checkIfDisabled = () => Artifact3.Value == ArtifactEnum.None }));
                    ModSettingsManager.AddOption(new ChoiceOption(Artifact4, new ChoiceConfig()));
                    ModSettingsManager.AddOption(new IntSliderOption(Artifact4StartWave, new IntSliderConfig() { min = 0, max = 100, restartRequired = false, checkIfDisabled = () => Artifact4.Value == ArtifactEnum.None }));
                    ModSettingsManager.AddOption(new IntSliderOption(Artifact4EndWave, new IntSliderConfig() { min = 0, max = 100, restartRequired = false, checkIfDisabled = () => Artifact4.Value == ArtifactEnum.None }));
                }
            }
            // --- Difficulty Mods ---
            {
                DifficultyMultiplier1 = config.Bind("Simulacrum Difficulty", "DifficultyMultiplier A", 1.5f, new ConfigDescription("Which DifficultyMultiplier to add."));
                DifficultyMultiplier1StartWave = config.Bind("Simulacrum Difficulty", "DifficultyMultiplier A Start Wave", 51, new ConfigDescription("At which wave to add this DifficultyMultiplier."));
                DifficultyMultiplier1EndWave = config.Bind("Simulacrum Difficulty", "DifficultyMultiplier A End Wave", 59, new ConfigDescription("After which wave to remove this DifficultyMultiplier."));

                DifficultyMultiplier2 = config.Bind("Simulacrum Difficulty", "DifficultyMultiplier B", 2f, new ConfigDescription("Which DifficultyMultiplier to add."));
                DifficultyMultiplier2StartWave = config.Bind("Simulacrum Difficulty", "DifficultyMultiplier B Start Wave", 60, new ConfigDescription("At which wave to add this DifficultyMultiplier."));
                DifficultyMultiplier2EndWave = config.Bind("Simulacrum Difficulty", "DifficultyMultiplier B End Wave", 100, new ConfigDescription("After which wave to remove this DifficultyMultiplier."));

                DifficultyMultiplier3 = config.Bind("Simulacrum Difficulty", "DifficultyMultiplier C", 1f, new ConfigDescription("Which DifficultyMultiplier to add."));
                DifficultyMultiplier3StartWave = config.Bind("Simulacrum Difficulty", "DifficultyMultiplier C Start Wave", 50, new ConfigDescription("At which wave to add this DifficultyMultiplier."));
                DifficultyMultiplier3EndWave = config.Bind("Simulacrum Difficulty", "DifficultyMultiplier C End Wave", 50, new ConfigDescription("After which wave to remove this DifficultyMultiplier."));

                DifficultyMultiplier4 = config.Bind("Simulacrum Difficulty", "DifficultyMultiplier D", 1f, new ConfigDescription("Which DifficultyMultiplier to add."));
                DifficultyMultiplier4StartWave = config.Bind("Simulacrum Difficulty", "DifficultyMultiplier D Start Wave", 50, new ConfigDescription("At which wave to add this DifficultyMultiplier."));
                DifficultyMultiplier4EndWave = config.Bind("Simulacrum Difficulty", "DifficultyMultiplier D End Wave", 50, new ConfigDescription("After which wave to remove this DifficultyMultiplier."));

                if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions"))
                {
                    ModSettingsManager.AddOption(new StepSliderOption(DifficultyMultiplier1, new StepSliderConfig() { min = 0.1f, max = 3.0f, increment = 0.1f }));
                    ModSettingsManager.AddOption(new IntSliderOption(DifficultyMultiplier1StartWave, new IntSliderConfig() { min = 0, max = 100, restartRequired = false, checkIfDisabled = () => DifficultyMultiplier1.Value == 1f }));
                    ModSettingsManager.AddOption(new IntSliderOption(DifficultyMultiplier1EndWave, new IntSliderConfig() { min = 0, max = 100, restartRequired = false, checkIfDisabled = () => DifficultyMultiplier1.Value == 1f }));
                    ModSettingsManager.AddOption(new StepSliderOption(DifficultyMultiplier2, new StepSliderConfig() { min = 0.1f, max = 3.0f, increment = 0.1f }));
                    ModSettingsManager.AddOption(new IntSliderOption(DifficultyMultiplier2StartWave, new IntSliderConfig() { min = 0, max = 100, restartRequired = false, checkIfDisabled = () => DifficultyMultiplier2.Value == 1f }));
                    ModSettingsManager.AddOption(new IntSliderOption(DifficultyMultiplier2EndWave, new IntSliderConfig() { min = 0, max = 100, restartRequired = false, checkIfDisabled = () => DifficultyMultiplier2.Value == 1f }));
                    ModSettingsManager.AddOption(new StepSliderOption(DifficultyMultiplier3, new StepSliderConfig() { min = 0.1f, max = 3.0f, increment = 0.1f }));
                    ModSettingsManager.AddOption(new IntSliderOption(DifficultyMultiplier3StartWave, new IntSliderConfig() { min = 0, max = 100, restartRequired = false, checkIfDisabled = () => DifficultyMultiplier3.Value == 1f }));
                    ModSettingsManager.AddOption(new IntSliderOption(DifficultyMultiplier3EndWave, new IntSliderConfig() { min = 0, max = 100, restartRequired = false, checkIfDisabled = () => DifficultyMultiplier3.Value == 1f }));
                    ModSettingsManager.AddOption(new StepSliderOption(DifficultyMultiplier4, new StepSliderConfig() { min = 0.1f, max = 3.0f, increment = 0.1f }));
                    ModSettingsManager.AddOption(new IntSliderOption(DifficultyMultiplier4StartWave, new IntSliderConfig() { min = 0, max = 100, restartRequired = false, checkIfDisabled = () => DifficultyMultiplier4.Value == 1f }));
                    ModSettingsManager.AddOption(new IntSliderOption(DifficultyMultiplier4EndWave, new IntSliderConfig() { min = 0, max = 100, restartRequired = false, checkIfDisabled = () => DifficultyMultiplier4.Value == 1f }));
                }
            }
            // --- Simulacrum Tweaks ---
            {
                UseNormalStages = config.Bind("Simulacrum Tweaks", "Use Normal Stages", true, new ConfigDescription("Whether to use all standard stages to playthrough in Simulacrum mode."));
                FinalStage = config.Bind("Simulacrum Tweaks", "Final Stage", StageEnum.VoidLocus, new ConfigDescription("To which stage to warp."));
                FinalStageStartWave = config.Bind("Simulacrum Tweaks", "Final Stage Warp Wave", 50, new ConfigDescription("At which wave to warp to this stage. Must be multiple of 10."));
                FinalStageLunarCoinsReward = config.Bind("Simulacrum Tweaks", "Final Stage Lunar Coins Reward", 100, new ConfigDescription("How many Lunar Coins to drop once the final stage has been completed."));

                FixSimulacrumBazaar = config.Bind("Simulacrum Tweaks", "No Void Fog in Bazaar", true, new ConfigDescription("Removes the void fog which appears when you get to the bazaar in Simulacrum mode."));
                FixSimulacrumSpawnPointsInNormalStages = config.Bind("Simulacrum Tweaks", "Fix Spawn Points in non-Simulacrum stages", true, new ConfigDescription("If you get to a non-simulacrum stage in Simulacrum mode, you will most likely spawn in the void fog. This mod forces you to spawn near the safe ward."));
                NoMissionsInVoidFields = config.Bind("Simulacrum Tweaks", "No missions in Void Fields", true, new ConfigDescription("Removes the void fog from the Void Fields (the void fog from the Simulacrum mode remains) and disables all Cell Vents (only in Simulacrum mode)."));
                NoMissionsInVoidLocus = config.Bind("Simulacrum Tweaks", "No missions in Void Locus", true, new ConfigDescription("Disables all Deep Void Signals in the Void Locus stage (only in Simulacrum mode)."));

                if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions"))
                {
                    ModSettingsManager.AddOption(new CheckBoxOption(UseNormalStages, new CheckBoxConfig()));
                    ModSettingsManager.AddOption(new ChoiceOption(FinalStage, new ChoiceConfig()));
                    ModSettingsManager.AddOption(new IntSliderOption(FinalStageStartWave, new IntSliderConfig() { min = 0, max = 100, restartRequired = false, checkIfDisabled = () => FinalStage.Value == StageEnum.None }));
                    ModSettingsManager.AddOption(new IntSliderOption(FinalStageLunarCoinsReward, new IntSliderConfig() { min = 0, max = 100, restartRequired = false, checkIfDisabled = () => FinalStage.Value == StageEnum.None }));
                    ModSettingsManager.AddOption(new CheckBoxOption(FixSimulacrumBazaar, new CheckBoxConfig()));
                    ModSettingsManager.AddOption(new CheckBoxOption(FixSimulacrumSpawnPointsInNormalStages, new CheckBoxConfig()));
                    ModSettingsManager.AddOption(new CheckBoxOption(NoMissionsInVoidFields, new CheckBoxConfig()));
                    ModSettingsManager.AddOption(new CheckBoxOption(NoMissionsInVoidLocus, new CheckBoxConfig()));
                }
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