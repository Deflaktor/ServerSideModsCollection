using BepInEx.Configuration;
using RoR2;
using UnityEngine;
using R2API;
using System.Collections.Generic;
using static SimulacrumTweaks.BepConfig;
using System.Linq;
using static SimulacrumTweaks.EnumCollection;

namespace SimulacrumTweaks
{
    public class BepConfig
    {
        public static ConfigEntry<bool> Enabled { get; set; }
        // simulacrum tweaks
        public static ConfigEntry<bool> SimulacrumNonSharedLoot;
        public static ConfigEntry<int> SimulacrumLootMaxItemDebt;
        public static ConfigEntry<float> SimulacrumCommencementArtifactDissonanceChance;
        public static ConfigEntry<float> SimulacrumDirectorEnemyPowerBias;
        public static ConfigEntry<float> SimulacrumDirectorCreditsScalingByPlayerCount;

        public static void Init()
        {
            var config = SimulacrumTweaks.instance.Config;

            Enabled = config.Bind("Main", "Enabled", true, "Enable Mod");
            // --- Simulacrum ---
            {
                SimulacrumNonSharedLoot = config.Bind("Simulacrum", "Non-shared loot", false, new ConfigDescription("(ShareSuite only) Forces the loot dropped at the end of each wave to be non-shared."));
                SimulacrumLootMaxItemDebt = config.Bind("Simulacrum", "Loot max item debt", 3, new ConfigDescription("Prevents greedy players from looting too much of the loot dropped at the end of each wave."));
                SimulacrumCommencementArtifactDissonanceChance = config.Bind("Simulacrum", "Commencement Artifact of Dissonance Chance", 0.5f, new ConfigDescription("The chance for Artifact of Dissonance to be activated each wave when in the commencement stage to increase enemy variety."));
                SimulacrumDirectorEnemyPowerBias = config.Bind("Simulacrum", "Director: Enemy power bias", 0.5f, new ConfigDescription("Bias towards stronger enemies or weaker enemies (value between 0 and 1, 0.5 = default)."));
                //SimulacrumDirectorCreditsScalingByPlayerCount = config.Bind("Simulacrum", "Director: Enemy power bias", 0.5f, new ConfigDescription("Prevents greedy players from looting too much of the loot dropped at the end of each wave."));
            }
            if (ModCompatibilityInLobbyConfig.enabled)
            {
                ModCompatibilityInLobbyConfig.CreateFromBepInExConfigFile(config, "Simulacrum Tweaks");
            }
        }
    }
}
