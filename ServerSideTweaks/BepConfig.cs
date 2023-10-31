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
        // public static ConfigEntry<bool> EliteEquipmentsInBazaar;
        // simulacrum tweaks
        public static ConfigEntry<bool> SimulacrumNonSharedLoot;
        public static ConfigEntry<int> SimulacrumLootMaxItemDebt;
        public static ConfigEntry<float> SimulacrumCommencementArtifactDissonanceChance;
        public static ConfigEntry<float> SimulacrumDirectorEnemyPowerBias;
        // classic tweaks
        public static ConfigEntry<float> ClassicDirectorEnemyPowerBias;

        public static void Init()
        {
            var config = ServerSideTweaks.instance.Config;

            Enabled = config.Bind("Main", "Enabled", true, "Enable Mod");
            // --- General ---
            {
                // EliteEquipmentsInBazaar = config.Bind("Main", "Add elite items to equipment pool during bazaar stage", true, new ConfigDescription("Adds the elite aspects equipments to the item pool during the bazaar stage. Only useful in combination with BiggerBazaar or BazaarIsMyHome."));
            }
            // --- Simulacrum ---
            {
                SimulacrumNonSharedLoot = config.Bind("Simulacrum", "Non-shared loot", false, new ConfigDescription("(ShareSuite only) Forces the loot dropped at the end of each wave to be non-shared."));
                SimulacrumLootMaxItemDebt = config.Bind("Simulacrum", "Max loot debt", 2, new ConfigDescription("Prevents greedy players from taking too much of the loot dropped at the end of each wave."));
                SimulacrumCommencementArtifactDissonanceChance = config.Bind("Simulacrum", "Commencement Artifact of Dissonance Chance", 0.5f, new ConfigDescription("The chance for Artifact of Dissonance to be activated each wave when in the commencement stage to increase enemy variety."));
                SimulacrumDirectorEnemyPowerBias = config.Bind("Simulacrum", "Director: Enemy Power Bias", 0.5f, new ConfigDescription("Bias towards many,weak enemies (=0) or few,strong enemies (=1). Value between 0 and 1, 0.5 = default."));
            }
            // --- Classic Run ---
            {
                ClassicDirectorEnemyPowerBias = config.Bind("Classic", "Director: Enemy Power Bias", 0.5f, new ConfigDescription("Bias towards many,weak enemies (=0) or few,strong enemies (=1). Value between 0 and 1, 0.5 = default."));
            }
            if (ModCompatibilityInLobbyConfig.enabled)
            {
                ModCompatibilityInLobbyConfig.CreateFromBepInExConfigFile(config, "Server-Side Tweaks");
            }
        }
    }
}
