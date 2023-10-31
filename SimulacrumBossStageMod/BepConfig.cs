using BepInEx.Configuration;
using RoR2;
using UnityEngine;
using R2API;
using System.Collections.Generic;
using static SimulacrumBossStageMod.BepConfig;
using System.Linq;
using static SimulacrumBossStageMod.EnumCollection;

namespace SimulacrumBossStageMod
{
    public class BepConfig
    {
        public static ConfigEntry<bool> Enabled { get; set; }
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
        public static ConfigEntry<bool> BossStageTier2Elite;
        public static ConfigEntry<BossEnum> BossStageBoss;
        public static ConfigEntry<EliteEnum> BossStageBossElite;
        public static ConfigEntry<int> BossStageBossCount;
        public static ConfigEntry<int> BossStageLunarCoinsReward;
        public static ConfigEntry<bool> BossStageCompleteEndRun;

        public static void Init()
        {
            var config = SimulacrumBossStageMod.instance.Config;

            Enabled = config.Bind("Main", "Enabled", true, "Enable Mod");
            // --- Boss Stage ---
            {
                BossStage = config.Bind("Boss Stage", "Boss Stage", StageEnum.VoidLocus, new ConfigDescription("To which stage to warp."));
                BossStageStartWave = config.Bind("Boss Stage", "Boss Stage Warp Wave", 40, new ConfigDescription("At which wave to warp to this stage. Must be multiple of 10."));
                BossStageTier2Elite = config.Bind("Boss Stage", "Malachite Elites", true, new ConfigDescription("Enables Malachite elites once the boss stage has been reached."));
                BossStageBoss = config.Bind("Boss Stage", "Boss", BossEnum.VoidlingPhase3, new ConfigDescription("Which boss to spawn during the final boss stage wave."));
                BossStageBossElite = config.Bind("Boss Stage", "Boss Elite Aspect", EliteEnum.None, new ConfigDescription("Which elite aspect the boss shall have."));
                BossStageBossCount = config.Bind("Boss Stage", "Boss Count", 1, new ConfigDescription("How many bosses to spawn during the final boss stage wave."));
                BossStageLunarCoinsReward = config.Bind("Boss Stage", "Boss Stage Lunar Coins Reward", 100, new ConfigDescription("How many Lunar Coins to drop once the final stage has been completed."));
                BossStageCompleteEndRun = config.Bind("Boss Stage", "Completing Boss Stage ends Run", true, new ConfigDescription("If the boss stage is completed, the run will end."));
            }
            // --- Artifacts ---
            {
                Artifact1 = config.Bind("Artifacts", "Artifact A", ArtifactEnum.Dissonance, new ConfigDescription("Which artifact to add."));
                Artifact1StartWave = config.Bind("Artifacts", "Artifact A Start Wave", 41, new ConfigDescription("At which wave to add this artifact."));
                Artifact1EndWave = config.Bind("Artifacts", "Artifact A End Wave", 45, new ConfigDescription("After which wave to remove this artifact."));

                Artifact2 = config.Bind("Artifacts", "Artifact B", ArtifactEnum.Honor, new ConfigDescription("Which artifact to add."));
                Artifact2StartWave = config.Bind("Artifacts", "Artifact B Start Wave", 45, new ConfigDescription("At which wave to add this artifact."));
                Artifact2EndWave = config.Bind("Artifacts", "Artifact B End Wave", 45, new ConfigDescription("After which wave to remove this artifact."));

                Artifact3 = config.Bind("Artifacts", "Artifact C", ArtifactEnum.Honor, new ConfigDescription("Which artifact to add."));
                Artifact3StartWave = config.Bind("Artifacts", "Artifact C Start Wave", 50, new ConfigDescription("At which wave to add this artifact."));
                Artifact3EndWave = config.Bind("Artifacts", "Artifact C End Wave", 50, new ConfigDescription("After which wave to remove this artifact."));

                Artifact4 = config.Bind("Artifacts", "Artifact D", ArtifactEnum.None, new ConfigDescription("Which artifact to add."));
                Artifact4StartWave = config.Bind("Artifacts", "Artifact D Start Wave", 50, new ConfigDescription("At which wave to add this artifact."));
                Artifact4EndWave = config.Bind("Artifacts", "Artifact D End Wave", 50, new ConfigDescription("After which wave to remove this artifact."));
            }
            // --- Difficulty Mods ---
            {
                DifficultyMultiplier1 = config.Bind("Difficulty", "DifficultyMultiplier A", 1.25f, new ConfigDescription("Which DifficultyMultiplier to add."));
                DifficultyMultiplier1StartWave = config.Bind("Difficulty", "DifficultyMultiplier A Start Wave", 41, new ConfigDescription("At which wave to add this DifficultyMultiplier."));
                DifficultyMultiplier1EndWave = config.Bind("Difficulty", "DifficultyMultiplier A End Wave", 49, new ConfigDescription("After which wave to remove this DifficultyMultiplier."));

                DifficultyMultiplier2 = config.Bind("Difficulty", "DifficultyMultiplier B", 1.5f, new ConfigDescription("Which DifficultyMultiplier to add."));
                DifficultyMultiplier2StartWave = config.Bind("Difficulty", "DifficultyMultiplier B Start Wave", 50, new ConfigDescription("At which wave to add this DifficultyMultiplier."));
                DifficultyMultiplier2EndWave = config.Bind("Difficulty", "DifficultyMultiplier B End Wave", 50, new ConfigDescription("After which wave to remove this DifficultyMultiplier."));

                DifficultyMultiplier3 = config.Bind("Difficulty", "DifficultyMultiplier C", 1f, new ConfigDescription("Which DifficultyMultiplier to add."));
                DifficultyMultiplier3StartWave = config.Bind("Difficulty", "DifficultyMultiplier C Start Wave", 50, new ConfigDescription("At which wave to add this DifficultyMultiplier."));
                DifficultyMultiplier3EndWave = config.Bind("Difficulty", "DifficultyMultiplier C End Wave", 50, new ConfigDescription("After which wave to remove this DifficultyMultiplier."));

                DifficultyMultiplier4 = config.Bind("Difficulty", "DifficultyMultiplier D", 1f, new ConfigDescription("Which DifficultyMultiplier to add."));
                DifficultyMultiplier4StartWave = config.Bind("Difficulty", "DifficultyMultiplier D Start Wave", 50, new ConfigDescription("At which wave to add this DifficultyMultiplier."));
                DifficultyMultiplier4EndWave = config.Bind("Difficulty", "DifficultyMultiplier D End Wave", 50, new ConfigDescription("After which wave to remove this DifficultyMultiplier."));
            }
            if (ModCompatibilityInLobbyConfig.enabled)
            {
                ModCompatibilityInLobbyConfig.CreateFromBepInExConfigFile(config, "Simulacrum Boss Stage");
            }
        }
    }
}