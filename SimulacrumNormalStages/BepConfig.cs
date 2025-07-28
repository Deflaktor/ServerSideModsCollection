using BepInEx.Configuration;
using RoR2;
using UnityEngine;
using R2API;
using System.Collections.Generic;
using System.Linq;

namespace SimulacrumNormalStages
{
    public class BepConfig
    {
        public static ConfigEntry<bool> Enabled { get; set; }
        public static ConfigEntry<bool> PlaceVoidEradictors;
        public static ConfigEntry<bool> PlaceNewtAltars;

        public static void Init()
        {
            var config = SimulacrumNormalStages.instance.Config;

            Enabled = config.Bind("Main", "Enabled", true, "Enable Mod");
            PlaceVoidEradictors = config.Bind("Main", "Place Void Eradictors", false, new ConfigDescription("Places Void Eradictors - a shrine to erase items from the run."));
            PlaceNewtAltars = config.Bind("Main", "Place Newt Altars", false, new ConfigDescription("Places Newt Altars - in order to access the Bazaar between Times."));
            // --- Simulacrum ---
            if (ModCompatibilityInLobbyConfig.enabled)
            {
                ModCompatibilityInLobbyConfig.CreateFromBepInExConfigFile(config, "Simulacrum Normal Stages");
            }
        }
    }
}
