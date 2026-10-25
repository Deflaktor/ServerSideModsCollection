using BepInEx.Configuration;
using RoR2;
using UnityEngine;
using System.Collections.Generic;
using static ServerSideTweaks.BepConfig;
using System.Linq;

namespace ServerSideTweaks
{
    public class BepConfig
    {
        public static ConfigEntry<bool> Enabled { get; set; }
        // general tweaks
        public static ConfigEntry<float> PearlReplacesLunarItemChance;
        public static ConfigEntry<float> IrradiantPearlReplacesLunarItemChance;
        public static ConfigEntry<bool> NoPearlsInBazaar;
        public static ConfigEntry<bool> ChildReducedTeleportRange;
        public static ConfigEntry<bool> ChildRemoveInvincibility;
        public static ConfigEntry<bool> VoidJailerNoCooldownReductions;
        public static ConfigEntry<bool> SolusInvalidatorNoCooldownReductions;
        public static ConfigEntry<bool> VoidlingNoCooldownReductions;
        public static ConfigEntry<float> MonsterGoldMultiplier;
        // simulacrum tweaks
        public static ConfigEntry<bool> SimulacrumNonSharedLoot;
        public static ConfigEntry<int> SimulacrumLootMaxItemDebt;
        public static ConfigEntry<bool> SimulacrumLootSkullTokens;
        public static ConfigEntry<float> SimulacrumCommencementArtifactDissonanceChance;
        public static ConfigEntry<float> SimulacrumDirectorEnemyPowerBias;
        public static ConfigEntry<int> SimulacrumMaxSquadSize;
        public static ConfigEntry<float> SimulacrumWavePeriodSecondsFactor;
        public static ConfigEntry<bool> SimulacrumFastVoidCrab;
        public static ConfigEntry<float> SimulacrumEmptyWaveDirectorSpeedup;
        public static ConfigEntry<bool> SimulacrumCaptainBeaconsAfterBossWave;
        public static ConfigEntry<bool> SimulacrumAllowDrones;
        // classic tweaks
        public static ConfigEntry<float> ClassicDirectorEnemyPowerBias;

        public static void Init()
        {
            var config = ServerSideTweaks.instance.Config;

            Enabled = ModCompatibilityConfigPresets.Bind(config, "Main", "Enabled", true, "Enable Mod");
            // --- General ---
            {
                PearlReplacesLunarItemChance = ModCompatibilityConfigPresets.Bind(config, "Main", "Pearl replaces Lunar Item chance", 0.1f, new ConfigDescription("Adds a chance for lunar items to be replaced by a pearl."));
                IrradiantPearlReplacesLunarItemChance = ModCompatibilityConfigPresets.Bind(config, "Main", "Irradiant Pearl replaces Lunar Item chance", 0.02f, new ConfigDescription("Adds a chance for lunar items to be replaced by an irradiant pearl."));
                NoPearlsInBazaar = ModCompatibilityConfigPresets.Bind(config, "Main", "No Pearl replacements in the Bazaar between Times", true, new ConfigDescription("Prevents pearls from appearing in the Bazaar to prevent hoarding."));
                ChildReducedTeleportRange = ModCompatibilityConfigPresets.Bind(config, "Main", "Child: Reduced teleport range", true, new ConfigDescription("Reduces the range children can teleport away."));
                ChildRemoveInvincibility = ModCompatibilityConfigPresets.Bind(config, "Main", "Child: Remove invincibility", true, new ConfigDescription("Removes the invincibility buff that children get before teleporting."));
                VoidJailerNoCooldownReductions = ModCompatibilityConfigPresets.Bind(config, "Main", "Void Jailer: No cooldown reductions", true, new ConfigDescription("Prevents Void Jailer from getting its cooldown reduced by items like Alien Head for balancing purposes."));
                SolusInvalidatorNoCooldownReductions = ModCompatibilityConfigPresets.Bind(config, "Main", "Solus Invalidator: No cooldown reductions", true, new ConfigDescription("Prevents Solus Invalidator from getting its cooldown reduced by items like Alien Head for balancing purposes."));
                VoidlingNoCooldownReductions = ModCompatibilityConfigPresets.Bind(config, "Main", "Voidling: No cooldown reductions", true, new ConfigDescription("Prevents Voidling from getting its cooldown reduced by items like Alien Head for balancing purposes."));
                MonsterGoldMultiplier = ModCompatibilityConfigPresets.Bind(config, "Main", "Monster gold multiplier", 1f, new ConfigDescription("Multiplies the gold monsters give when killed. A value smaller than 1.0 gives less gold. (1.0 = vanilla)."));
            }
            // --- Simulacrum ---
            {
                SimulacrumNonSharedLoot = ModCompatibilityConfigPresets.Bind(config, "Simulacrum", "Non-shared loot", false, new ConfigDescription("(ShareSuite only) Forces the loot dropped at the end of each wave to be non-shared."));
                SimulacrumLootMaxItemDebt = ModCompatibilityConfigPresets.Bind(config, "Simulacrum", "Max loot debt", 2, new ConfigDescription("Prevents greedy players from taking too much of the loot dropped at the end of each wave."));
                SimulacrumLootSkullTokens = ModCompatibilityConfigPresets.Bind(config, "Simulacrum", "Max loot debt - Skull Tokens", true, new ConfigDescription("Indicate to players how many void potentials they can take by assigning Skull Tokens."));
                SimulacrumCommencementArtifactDissonanceChance = ModCompatibilityConfigPresets.Bind(config, "Simulacrum", "Commencement Artifact of Dissonance Chance", 0.5f, new ConfigDescription("The chance for Artifact of Dissonance to be activated each wave when in the commencement stage to increase enemy variety."));
                SimulacrumDirectorEnemyPowerBias = ModCompatibilityConfigPresets.Bind(config, "Simulacrum", "Director: Enemy Power Bias", 0.5f, new ConfigDescription("Bias towards many,weak enemies (=0) or few,strong enemies (=1). Value between 0 and 1, 0.5 = vanilla."));
                SimulacrumMaxSquadSize = ModCompatibilityConfigPresets.Bind(config, "Simulacrum", "Max squad size", 30, new ConfigDescription("No more than this many enemies shall be spawned at the same time (30 = vanilla)."));
                SimulacrumWavePeriodSecondsFactor = ModCompatibilityConfigPresets.Bind(config, "Simulacrum", "Wave Period Seconds Factor", 1.0f, new ConfigDescription("A factor on how long each wave takes. A value smaller than 1.0 makes the wave go faster, but also with stronger enemies. A value higher than 1.0 makes the wave take longer but the enemies are weaker. (1.0 = vanilla)."));
                SimulacrumFastVoidCrab = ModCompatibilityConfigPresets.Bind(config, "Simulacrum", "Void Crab picks up speed", false, new ConfigDescription("Makes the Void Crab faster the more waves you complete."));
                SimulacrumEmptyWaveDirectorSpeedup = ModCompatibilityConfigPresets.Bind(config, "Simulacrum", "Director: Speed-up without enemies", 3f, new ConfigDescription("While no enemy of the wave is alive, the director earns its credits this many times faster, so the next enemies come sooner. (1.0 = vanilla)."));
                SimulacrumCaptainBeaconsAfterBossWave = ModCompatibilityConfigPresets.Bind(config, "Simulacrum", "Captain: Beacons after boss waves", true, new ConfigDescription("Gives Captains their beacons back after each boss wave. A Captain who used a beacon is respawned in place, since only a new body gets them back, which also restores their health."));
                SimulacrumAllowDrones = ModCompatibilityConfigPresets.Bind(config, "Simulacrum", "Allow drones", false, new ConfigDescription("Lets broken drones appear on Simulacrum's stages, as on normal stages, to repair with gold."));
            }
            // --- Classic Run ---
            {
                ClassicDirectorEnemyPowerBias = ModCompatibilityConfigPresets.Bind(config, "Classic", "Director: Enemy Power Bias", 0.5f, new ConfigDescription("Bias towards many,weak enemies (=0) or few,strong enemies (=1). Value between 0 and 1, 0.5 = vanilla."));
            }
            if (ModCompatibilityInLobbyConfig.enabled)
            {
                ModCompatibilityInLobbyConfig.CreateFromBepInExConfigFile(config, "Server-Side Tweaks");
            }
        }
    }
}
