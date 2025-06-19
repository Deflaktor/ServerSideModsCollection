using BepInEx.Configuration;
using RoR2;
using UnityEngine;
using R2API;
using System.Collections.Generic;
using System.Linq;

namespace ServerSideBeyondTheLimits
{
    public class BepConfig
    {
        public static ConfigEntry<bool> Enabled { get; set; }
        // general tweaks
        public static ConfigEntry<float> PearlReplacesLunarItemChance;
        public static ConfigEntry<float> IrradiantPearlReplacesLunarItemChance;
        public static ConfigEntry<bool> NoPearlsInBazaar;
        // bazaar tweaks
        public static ConfigEntry<float> BazaarEliteAspectReplacesEquipmentChance;
        public static ConfigEntry<bool> BazaarEliteAspectHerBitingEmbrace;
        public static ConfigEntry<bool> BazaarEliteAspectHisReassurance;
        public static ConfigEntry<bool> BazaarEliteAspectIfritsDistinction;
        public static ConfigEntry<bool> BazaarEliteAspectNkuhanasRetort;
        public static ConfigEntry<bool> BazaarEliteAspectSharedDesign;
        public static ConfigEntry<bool> BazaarEliteAspectSilenceBetweenTwoStrikes;
        public static ConfigEntry<bool> BazaarEliteAspectSpectralCirclet;
        public static ConfigEntry<bool> BazaarEliteAspectVoidTouched;
        public static ConfigEntry<bool> BazaarEliteAspectBeyondTheLimits;
        // items
        public static ConfigEntry<bool> ImplementBeyondTheLimits;
        // simulacrum tweaks
        public static ConfigEntry<bool> SimulacrumNonSharedLoot;
        public static ConfigEntry<int> SimulacrumLootMaxItemDebt;
        public static ConfigEntry<float> SimulacrumCommencementArtifactDissonanceChance;
        public static ConfigEntry<float> SimulacrumDirectorEnemyPowerBias;
        public static ConfigEntry<int> SimulacrumMaxSquadSize;
        // public static ConfigEntry<bool> SimulacrumExtendedMapPool;
        // classic tweaks
        public static ConfigEntry<float> ClassicDirectorEnemyPowerBias;

        public static void Init()
        {
            var config = ServerSideBeyondTheLimits.instance.Config;

            Enabled = config.Bind("Main", "Enabled", true, "Enable Mod");
            // --- General ---
            {
                PearlReplacesLunarItemChance = config.Bind("Main", "Pearl replaces Lunar Item chance", 0.1f, new ConfigDescription("Adds a chance for lunar items to be replaced by a pearl."));
                IrradiantPearlReplacesLunarItemChance = config.Bind("Main", "Irradiant Pearl replaces Lunar Item chance", 0.02f, new ConfigDescription("Adds a chance for lunar items to be replaced by an irradiant pearl."));
                NoPearlsInBazaar = config.Bind("Main", "No Pearl replacements in the Bazaar between Times", true, new ConfigDescription("Prevents pearls from appearing in the Bazaar to prevent hoarding."));
            }
            // --- Bazaar ---
            {
                BazaarEliteAspectReplacesEquipmentChance = config.Bind("Bazaar between Times", "Elite Aspect replaces Equipment Chance", 0.2f, new ConfigDescription("Chance that an equipment item is replaced by an elite aspect (only in Bazaar between Times). Only useful in combination with BazaarIsMyHome."));
                BazaarEliteAspectHerBitingEmbrace = config.Bind("Bazaar between Times", "Elite Aspect Pool: Her Biting Embrace", true, new ConfigDescription("Become an aspect of ice."));
                BazaarEliteAspectHisReassurance = config.Bind("Bazaar between Times", "Elite Aspect Pool: His Reassurance", true, new ConfigDescription("Become an aspect of earth."));
                BazaarEliteAspectIfritsDistinction = config.Bind("Bazaar between Times", "Elite Aspect Pool: Ifrits Distinction", true, new ConfigDescription("Become an aspect of fire."));
                BazaarEliteAspectNkuhanasRetort = config.Bind("Bazaar between Times", "Elite Aspect Pool: Nkuhanas Retort", true, new ConfigDescription("Become an aspect of corruption."));
                BazaarEliteAspectSharedDesign = config.Bind("Bazaar between Times", "Elite Aspect Pool: Shared Design", true, new ConfigDescription("Become an aspect of perfection."));
                BazaarEliteAspectSilenceBetweenTwoStrikes = config.Bind("Bazaar between Times", "Elite Aspect Pool: Silence Between Two Strikes", true, new ConfigDescription("Become an aspect of lightning."));
                BazaarEliteAspectSpectralCirclet = config.Bind("Bazaar between Times", "Elite Aspect Pool: Spectral Circlet", true, new ConfigDescription("Become an aspect of incorporeality."));
                BazaarEliteAspectVoidTouched = config.Bind("Bazaar between Times", "Elite Aspect Pool: Void Touched", false, new ConfigDescription("This item is unobtainable in vanilla. It makes the wearer have the same effect as void touched foes."));
                BazaarEliteAspectBeyondTheLimits = config.Bind("Bazaar between Times", "Elite Aspect Pool: Beyond The Limits", false, new ConfigDescription("Become an aspect of speed. This item is unused and has no effect."));
            }
            // --- Items ---
            {
                ImplementBeyondTheLimits = config.Bind("Items", "Implement Beyond The Limits", false, new ConfigDescription("Makes it so that the wearer of the unused 'Beyond The Limits' aspect gains a passive 50% movement speed bonus and relaxes for the wearer the requirement for activation of certain items from 'when sprinting' to 'when moving'."));
            }
        }
    }
}
