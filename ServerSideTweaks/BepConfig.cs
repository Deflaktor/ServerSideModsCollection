using BepInEx.Configuration;
using RoR2;
using UnityEngine;
using R2API;
using System.Collections.Generic;
using static ServerSideTweaks.BepConfig;
using System.Linq;
using static ServerSideTweaks.EnumCollection;

namespace ServerSideTweaks
{
    public class BepConfig
    {
        public static ConfigEntry<bool> Enabled { get; set; }
        // general tweaks
        public static ConfigEntry<float> PearlReplacesLunarItemChance;
        public static ConfigEntry<float> IrradiantPearlReplacesLunarItemChance;
        public static ConfigEntry<bool> NoPearlsInBazaar;
        public static ConfigEntry<float> BazaarEliteAspectReplacesEquipmentChance;
        public static ConfigEntry<bool> ShatterspleenWorksOnBaseHealth;
        // simulacrum tweaks
        public static ConfigEntry<bool> SimulacrumNonSharedLoot;
        public static ConfigEntry<int> SimulacrumLootMaxItemDebt;
        public static ConfigEntry<bool> SimulacrumLootSkullTokens;
        public static ConfigEntry<float> SimulacrumCommencementArtifactDissonanceChance;
        public static ConfigEntry<float> SimulacrumDirectorEnemyPowerBias;
        public static ConfigEntry<int> SimulacrumMaxSquadSize;
        public static ConfigEntry<float> SimulacrumWavePeriodSecondsFactor;
        public static ConfigEntry<bool> SimulacrumFastVoidCrab;
        // classic tweaks
        public static ConfigEntry<float> ClassicDirectorEnemyPowerBias;

        public static void Init()
        {
            var config = ServerSideTweaks.instance.Config;

            Enabled = config.Bind("Main", "Enabled", true, "Enable Mod");
            // --- General ---
            {
                PearlReplacesLunarItemChance = config.Bind("Main", "Pearl replaces Lunar Item chance", 0.1f, new ConfigDescription("Adds a chance for lunar items to be replaced by a pearl."));
                IrradiantPearlReplacesLunarItemChance = config.Bind("Main", "Irradiant Pearl replaces Lunar Item chance", 0.02f, new ConfigDescription("Adds a chance for lunar items to be replaced by an irradiant pearl."));
                NoPearlsInBazaar = config.Bind("Main", "No Pearl replacements in the Bazaar between Times", true, new ConfigDescription("Prevents pearls from appearing in the Bazaar to prevent hoarding."));
                BazaarEliteAspectReplacesEquipmentChance = config.Bind("Main", "Bazaar: Elite Aspect replaces Equipment Chance", 0.2f, new ConfigDescription("Chance that an equipment item is replaced by an elite aspect (only in Bazaar between Times). Only useful in combination with BazaarIsMyHome."));
                ShatterspleenWorksOnBaseHealth = config.Bind("Main", "Shatterspleen: Works on Base Health instead of Combined Heatlh", true, new ConfigDescription("Shatterspleen normally applies dmg based on the combined max heatlh of enemies. This makes it super strong if enemies have health increasing items."));
            }
            // --- Simulacrum ---
            {
                SimulacrumNonSharedLoot = config.Bind("Simulacrum", "Non-shared loot", false, new ConfigDescription("(ShareSuite only) Forces the loot dropped at the end of each wave to be non-shared."));
                SimulacrumLootMaxItemDebt = config.Bind("Simulacrum", "Max loot debt", 2, new ConfigDescription("Prevents greedy players from taking too much of the loot dropped at the end of each wave."));
                SimulacrumLootSkullTokens = config.Bind("Simulacrum", "Max loot debt - Skull Tokens", true, new ConfigDescription("Indicate to players how many void potentials they can take by assigning Skull Tokens."));
                SimulacrumCommencementArtifactDissonanceChance = config.Bind("Simulacrum", "Commencement Artifact of Dissonance Chance", 0.5f, new ConfigDescription("The chance for Artifact of Dissonance to be activated each wave when in the commencement stage to increase enemy variety."));
                SimulacrumDirectorEnemyPowerBias = config.Bind("Simulacrum", "Director: Enemy Power Bias", 0.5f, new ConfigDescription("Bias towards many,weak enemies (=0) or few,strong enemies (=1). Value between 0 and 1, 0.5 = vanilla."));
                SimulacrumMaxSquadSize = config.Bind("Simulacrum", "Max squad size", 30, new ConfigDescription("No more than this many enemies shall be spawned at the same time (30 = vanilla)."));
                SimulacrumWavePeriodSecondsFactor = config.Bind("Simulacrum", "Wave Period Seconds Factor", 1.0f, new ConfigDescription("A factor on how long each wave takes. A value smaller than 1.0 makes the wave go faster, but also with stronger enemies. A value higher than 1.0 makes the wave take longer but the enemies are weaker. (1.0 = vanilla)."));
                SimulacrumFastVoidCrab = config.Bind("Simulacrum", "Void Crab picks up speed", false, new ConfigDescription("Makes the Void Crab faster the more waves you complete."));
            }
            // --- Classic Run ---
            {
                ClassicDirectorEnemyPowerBias = config.Bind("Classic", "Director: Enemy Power Bias", 0.5f, new ConfigDescription("Bias towards many,weak enemies (=0) or few,strong enemies (=1). Value between 0 and 1, 0.5 = vanilla."));
            }
            if (ModCompatibilityInLobbyConfig.enabled)
            {
                ModCompatibilityInLobbyConfig.CreateFromBepInExConfigFile(config, "Server-Side Tweaks");
            }
        }
    }
}
