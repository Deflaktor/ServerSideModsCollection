using BepInEx.Configuration;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ConfigPresets
{
    /// <summary>
    /// Binds a mod's settings so that an installed preset can set them for the mod, while the player's config file and
    /// in-game config menus like InLobbyConfig keep the player's own values.
    /// </summary>
    public static class PresetConfig
    {
        // the player's entry of each preset entry
        private static readonly ConditionalWeakTable<ConfigEntryBase, ConfigEntryBase> playerEntries = new ConditionalWeakTable<ConfigEntryBase, ConfigEntryBase>();

        /// <summary>
        /// Like <see cref="ConfigFile.Bind{T}(ConfigDefinition, T, ConfigDescription)"/>. The setting is bound in the
        /// config file as usual. If a preset that is on sets it, the mod gets an entry of its own with the preset's value
        /// instead, which isn't in the config file: reloading the file doesn't change it, and changing it doesn't save it.
        /// </summary>
        public static ConfigEntry<T> Bind<T>(ConfigFile config, ConfigDefinition definition, T defaultValue, ConfigDescription description = null)
        {
            var entry = config.Bind(definition, defaultValue, description);
            if (!Presets.TryGetValue(config, definition, out var value))
            {
                return entry;
            }
            var presetEntry = (ConfigEntry<T>)Activator.CreateInstance(typeof(ConfigEntry<T>), BindingFlags.Instance | BindingFlags.NonPublic, null,
                new object[] { config, definition, defaultValue, description ?? ConfigDescription.Empty }, null);
            presetEntry.SetSerializedValue(value);
            playerEntries.Add(presetEntry, entry);
            return presetEntry;
        }

        /// <inheritdoc cref="Bind{T}(ConfigFile, ConfigDefinition, T, ConfigDescription)"/>
        public static ConfigEntry<T> Bind<T>(ConfigFile config, string section, string key, T defaultValue, ConfigDescription description = null)
        {
            return Bind(config, new ConfigDefinition(section, key), defaultValue, description);
        }

        /// <inheritdoc cref="Bind{T}(ConfigFile, ConfigDefinition, T, ConfigDescription)"/>
        public static ConfigEntry<T> Bind<T>(ConfigFile config, string section, string key, T defaultValue, string description)
        {
            return Bind(config, new ConfigDefinition(section, key), defaultValue, new ConfigDescription(description));
        }

        /// <summary>
        /// Whether the entry has a preset's value instead of the player's.
        /// </summary>
        public static bool IsPreset(ConfigEntryBase entry)
        {
            return entry != null && playerEntries.TryGetValue(entry, out _);
        }

        /// <summary>
        /// The entry in the config file, with the player's value, e.g. to update an older config file. The entry itself
        /// if no preset sets it.
        /// </summary>
        public static ConfigEntry<T> PlayerEntry<T>(ConfigEntry<T> entry)
        {
            return entry != null && playerEntries.TryGetValue(entry, out var playerEntry) ? (ConfigEntry<T>)playerEntry : entry;
        }
    }
}
