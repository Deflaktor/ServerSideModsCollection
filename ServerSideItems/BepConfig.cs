using BepInEx.Configuration;
using RoR2;
using UnityEngine;
using R2API;
using System.Collections.Generic;
using System.Linq;

namespace ServerSideItems
{
    public class BepConfig
    {
        public static ConfigEntry<bool> Enabled { get; set; }
        public static ConfigEntry<bool> ShatterspleenWorksOnBaseHealth;
        public static ConfigEntry<bool> ImplementBeyondTheLimits;
        public static ConfigEntry<bool> NewlyHatchedZoeaRework;

        public static void Init()
        {
            var config = ServerSideItems.instance.Config;

            Enabled = config.Bind("Main", "Enabled", true, "Enable Mod");
            // --- ShatterspleenWorksOnBaseHealth ---
            ShatterspleenWorksOnBaseHealth = config.Bind("Main", "Shatterspleen: Works on Base Health instead of Combined Health", true, new ConfigDescription("Shatterspleen normally applies dmg based on the combined max health of enemies. This makes it super strong if enemies have health increasing items."));
            // --- NewlyHatchedZoeaRework ---
            NewlyHatchedZoeaRework = config.Bind("Main", "NewlyHatchedZoea: Instead of summoning void creature, you become a creature of the void.", true, new ConfigDescription("Every 5 (-25% per stack) seconds, fire 3 (+3 per stack) nullifying bombs which immobilize enemies and deal 380% damage. Auto-targets strongest grounded enemy. Corrupts all yellow items."));
            // --- Beyond The Limits ---
            // ImplementBeyondTheLimits = config.Bind("Beyond The Limits", "SectionEnabled", false, new ConfigDescription("Makes it so that the wearer of the unused 'Beyond The Limits' aspect gains a passive 50% movement speed bonus and relaxes for the wearer the requirement for activation of certain items from 'when sprinting' to 'when moving'."));
        }
    }
}