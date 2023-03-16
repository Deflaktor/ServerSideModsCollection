using BepInEx.Configuration;
using RoR2;
using UnityEngine;
using R2API;
using RiskOfOptions.Options;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions;
using System.Collections.Generic;
using static SimulacrumStagePoolMod.BepConfig;
using System.Linq;
using static SimulacrumStagePoolMod.EnumCollection;

namespace SimulacrumStagePoolMod
{
    public class BepConfig
    {
        // simulacrum tweaks
        public static ConfigEntry<bool> UseNormalStages;

        public static void Init()
        {
            var config = SimulacrumStagePoolMod.instance.Config;

            // --- Simulacrum Tweaks ---
            UseNormalStages = config.Bind("Simulacrum Tweaks", "Use Normal Stages", false, new ConfigDescription("Whether to use all standard stages to playthrough in Simulacrum mode."));

            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions"))
            {
                ModSettingsManager.AddOption(new CheckBoxOption(UseNormalStages, new CheckBoxConfig()));
            }
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.KingEnderBrine.InLobbyConfig"))
            {
                var configEntry = InLobbyConfig.Fields.ConfigFieldUtilities.CreateFromBepInExConfigFile(config, "Server-side Mods Collection");
                InLobbyConfig.ModConfigCatalog.Add(configEntry);
            }
        }
    }
}