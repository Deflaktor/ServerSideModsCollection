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
        public static ConfigEntry<bool> PlaceVoidEradicators;
        public static ConfigEntry<bool> PlaceNewtAltars;

        public static void Init()
        {
            var config = SimulacrumNormalStages.instance.Config;

            Enabled = config.Bind("Main", "Enabled", true, "Enable Mod");
            PlaceVoidEradicators = config.Bind("Main", "Place Void Eradicators", false, new ConfigDescription("Places Void Eradicators - a shrine to erase items from the run."));
            PlaceNewtAltars = config.Bind("Main", "Place Newt Altars", false, new ConfigDescription("Places Newt Altars - in order to access the Bazaar Between Time."));
            // carry over the setting from before its name was spelled correctly
            ConfigDefinition misspelledVoidEradicators = new ConfigDefinition("Main", "Place Void Eradictors");
            if (config.Bind(misspelledVoidEradicators, false).Value)
            {
                PlaceVoidEradicators.Value = true;
            }
            config.Remove(misspelledVoidEradicators);
            config.Save();
            // --- Simulacrum ---
            if (ModCompatibilityInLobbyConfig.enabled)
            {
                ModCompatibilityInLobbyConfig.CreateFromBepInExConfigFile(config, "Simulacrum Normal Stages");
            }
        }
    }
}
