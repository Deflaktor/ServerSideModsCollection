using BepInEx.Configuration;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ConfigPresets
{
    public static class ModCompatibilityRiskOfOptions
    {
        public const string GUID = "com.rune580.riskofoptions";

        public static bool enabled => BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(GUID);

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void AddChoices(IEnumerable<ConfigEntryBase> choices)
        {
            foreach (var choice in choices)
            {
                RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.ChoiceOption(choice), ConfigPresetsPlugin.PluginGUID, ConfigPresetsPlugin.PluginName);
            }
            RiskOfOptions.ModSettingsManager.SetModDescription("Pick the presets of the installed modpacks and mods. Off uses your own settings.", ConfigPresetsPlugin.PluginGUID, ConfigPresetsPlugin.PluginName);
        }
    }
}
