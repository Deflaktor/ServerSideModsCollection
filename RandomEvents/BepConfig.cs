using BepInEx.Configuration;
using RoR2;
using UnityEngine;
using R2API;
using System.Collections.Generic;
using System.Linq;

namespace RandomEvents
{
    public class BepConfig
    {
        public static ConfigEntry<bool> Enabled { get; set; }

        public static void Init()
        {
            var config = RandomEvents.instance.Config;

            Enabled = config.Bind("Main", "Enabled", true, "Enable Mod");
            // --- Boss Stage ---
            
            if (ModCompatibilityInLobbyConfig.enabled)
            {
                ModCompatibilityInLobbyConfig.CreateFromBepInExConfigFile(config, "Random Events");
            }
        }
    }
}