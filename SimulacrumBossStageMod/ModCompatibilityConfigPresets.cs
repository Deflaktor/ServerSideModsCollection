using BepInEx.Configuration;
using System.Runtime.CompilerServices;

namespace SimulacrumBossStageMod
{
    // Modpacks can set the settings through ConfigPresets, if it is installed
    public static class ModCompatibilityConfigPresets
    {
        private static bool? _enabled;

        public static bool enabled
        {
            get
            {
                if (_enabled == null)
                {
                    _enabled = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(ConfigPresets.ConfigPresetsPlugin.PluginGUID);
                }
                return (bool)_enabled;
            }
        }

        public static ConfigEntry<T> Bind<T>(ConfigFile config, ConfigDefinition definition, T defaultValue, ConfigDescription description = null)
        {
            return enabled ? BindPreset(config, definition, defaultValue, description) : config.Bind(definition, defaultValue, description);
        }

        public static ConfigEntry<T> Bind<T>(ConfigFile config, string section, string key, T defaultValue, ConfigDescription description = null)
        {
            return Bind(config, new ConfigDefinition(section, key), defaultValue, description);
        }

        public static ConfigEntry<T> Bind<T>(ConfigFile config, string section, string key, T defaultValue, string description)
        {
            return Bind(config, new ConfigDefinition(section, key), defaultValue, new ConfigDescription(description));
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static ConfigEntry<T> BindPreset<T>(ConfigFile config, ConfigDefinition definition, T defaultValue, ConfigDescription description)
        {
            return ConfigPresets.PresetConfig.Bind(config, definition, defaultValue, description);
        }
    }
}
