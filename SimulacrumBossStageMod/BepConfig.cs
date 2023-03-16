using BepInEx.Configuration;
using RoR2;
using UnityEngine;
using R2API;
using RiskOfOptions.Options;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions;
using System.Collections.Generic;
using static SimulacrumBossStageMod.BepConfig;
using System.Linq;
using static SimulacrumBossStageMod.EnumCollection;

namespace SimulacrumBossStageMod
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
        public static ConfigEntry<StageEnum> BossStage;
        public static ConfigEntry<int> BossStageStartWave;
        public static ConfigEntry<int> BossStageLunarCoinsReward;
        public static ConfigEntry<bool> BossStageCompleteEndRun;

        public static void Init()
        {
            var config = SimulacrumBossStageMod.instance.Config;

            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions"))
            {
                ModSettingsManager.SetModDescription(@"
<size=200%><uppercase><align=center><color=#adf2fa>Simulacrom Boss Stage Mod</color></align></uppercase></size>
");
            }
            // --- Boss Stage ---
            {
                BossStage = config.Bind("Boss Stage", "Boss Stage", StageEnum.VoidLocus, new ConfigDescription("To which stage to warp."));
                BossStageStartWave = config.Bind("Boss Stage", "Boss Stage Warp Wave", 50, new ConfigDescription("At which wave to warp to this stage. Must be multiple of 10."));
                BossStageLunarCoinsReward = config.Bind("Boss Stage", "Boss Stage Lunar Coins Reward", 100, new ConfigDescription("How many Lunar Coins to drop once the final stage has been completed."));
                BossStageCompleteEndRun = config.Bind("Boss Stage", "Completing Boss Stage ends Run", true, new ConfigDescription("If the boss stage is completed, the run will end."));

                if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions"))
                {
                    ModSettingsManager.AddOption(new ChoiceOption(BossStage, new ChoiceConfig()));
                    ModSettingsManager.AddOption(new IntSliderOption(BossStageStartWave, new IntSliderConfig() { min = 0, max = 100, restartRequired = false, checkIfDisabled = () => BossStage.Value == StageEnum.None }));
                    ModSettingsManager.AddOption(new IntSliderOption(BossStageLunarCoinsReward, new IntSliderConfig() { min = 0, max = 200, restartRequired = false, checkIfDisabled = () => BossStage.Value == StageEnum.None }));
                    ModSettingsManager.AddOption(new CheckBoxOption(BossStageCompleteEndRun, new CheckBoxConfig()));
                }
            }
            // --- Artifacts ---
            {
                Artifact1 = config.Bind("Artifacts", "Artifact A", ArtifactEnum.Honor, new ConfigDescription("Which artifact to add."));
                Artifact1StartWave = config.Bind("Artifacts", "Artifact A Start Wave", 56, new ConfigDescription("At which wave to add this artifact."));
                Artifact1EndWave = config.Bind("Artifacts", "Artifact A End Wave", 60, new ConfigDescription("After which wave to remove this artifact."));

                Artifact2 = config.Bind("Artifacts", "Artifact B", ArtifactEnum.Dissonance, new ConfigDescription("Which artifact to add."));
                Artifact2StartWave = config.Bind("Artifacts", "Artifact B Start Wave", 56, new ConfigDescription("At which wave to add this artifact."));
                Artifact2EndWave = config.Bind("Artifacts", "Artifact B End Wave", 60, new ConfigDescription("After which wave to remove this artifact."));

                Artifact3 = config.Bind("Artifacts", "Artifact C", ArtifactEnum.None, new ConfigDescription("Which artifact to add."));
                Artifact3StartWave = config.Bind("Artifacts", "Artifact C Start Wave", 50, new ConfigDescription("At which wave to add this artifact."));
                Artifact3EndWave = config.Bind("Artifacts", "Artifact C End Wave", 50, new ConfigDescription("After which wave to remove this artifact."));

                Artifact4 = config.Bind("Artifacts", "Artifact D", ArtifactEnum.None, new ConfigDescription("Which artifact to add."));
                Artifact4StartWave = config.Bind("Artifacts", "Artifact D Start Wave", 50, new ConfigDescription("At which wave to add this artifact."));
                Artifact4EndWave = config.Bind("Artifacts", "Artifact D End Wave", 50, new ConfigDescription("After which wave to remove this artifact."));

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
                DifficultyMultiplier1 = config.Bind("Difficulty", "DifficultyMultiplier A", 1.5f, new ConfigDescription("Which DifficultyMultiplier to add."));
                DifficultyMultiplier1StartWave = config.Bind("Difficulty", "DifficultyMultiplier A Start Wave", 51, new ConfigDescription("At which wave to add this DifficultyMultiplier."));
                DifficultyMultiplier1EndWave = config.Bind("Difficulty", "DifficultyMultiplier A End Wave", 59, new ConfigDescription("After which wave to remove this DifficultyMultiplier."));

                DifficultyMultiplier2 = config.Bind("Difficulty", "DifficultyMultiplier B", 2f, new ConfigDescription("Which DifficultyMultiplier to add."));
                DifficultyMultiplier2StartWave = config.Bind("Difficulty", "DifficultyMultiplier B Start Wave", 60, new ConfigDescription("At which wave to add this DifficultyMultiplier."));
                DifficultyMultiplier2EndWave = config.Bind("Difficulty", "DifficultyMultiplier B End Wave", 100, new ConfigDescription("After which wave to remove this DifficultyMultiplier."));

                DifficultyMultiplier3 = config.Bind("Difficulty", "DifficultyMultiplier C", 1f, new ConfigDescription("Which DifficultyMultiplier to add."));
                DifficultyMultiplier3StartWave = config.Bind("Difficulty", "DifficultyMultiplier C Start Wave", 50, new ConfigDescription("At which wave to add this DifficultyMultiplier."));
                DifficultyMultiplier3EndWave = config.Bind("Difficulty", "DifficultyMultiplier C End Wave", 50, new ConfigDescription("After which wave to remove this DifficultyMultiplier."));

                DifficultyMultiplier4 = config.Bind("Difficulty", "DifficultyMultiplier D", 1f, new ConfigDescription("Which DifficultyMultiplier to add."));
                DifficultyMultiplier4StartWave = config.Bind("Difficulty", "DifficultyMultiplier D Start Wave", 50, new ConfigDescription("At which wave to add this DifficultyMultiplier."));
                DifficultyMultiplier4EndWave = config.Bind("Difficulty", "DifficultyMultiplier D End Wave", 50, new ConfigDescription("After which wave to remove this DifficultyMultiplier."));

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
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.KingEnderBrine.InLobbyConfig"))
            {
                var configEntry = InLobbyConfig.Fields.ConfigFieldUtilities.CreateFromBepInExConfigFile(config, "Simulacrum Boss Stage Mod");
                InLobbyConfig.ModConfigCatalog.Add(configEntry);
            }
        }
    }
}