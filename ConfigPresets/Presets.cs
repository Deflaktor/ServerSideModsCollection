using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ConfigPresets
{
    // The installed presets: packages with a ConfigPreset folder of config files, named like the files in BepInEx/config.
    // Each preset has its own config file with an Enabled switch, named after its package's folder.
    internal static class Presets
    {
        public const string FolderName = "ConfigPreset";

        private static readonly ManualLogSource Log = Logger.CreateLogSource(ConfigPresetsPlugin.PluginName);

        // the settings of all presets that are on, by config file name, e.g. Def.BazaarIsMyHaven.cfg
        private static Dictionary<string, Dictionary<ConfigDefinition, string>> files;

        public static void Load()
        {
            if (files != null)
            {
                return;
            }
            files = new Dictionary<string, Dictionary<ConfigDefinition, string>>(StringComparer.OrdinalIgnoreCase);
            string[] directories;
            try
            {
                directories = Directory.GetDirectories(Paths.PluginPath, FolderName, SearchOption.AllDirectories);
            }
            catch (Exception exception)
            {
                Log.LogError($"Could not look for presets: {exception}");
                return;
            }
            foreach (var directory in directories.OrderBy(directory => directory, StringComparer.OrdinalIgnoreCase))
            {
                string name = Path.GetFileName(Path.GetDirectoryName(directory));
                var config = new ConfigFile(Path.Combine(Paths.ConfigPath, name + ".cfg"), true);
                var enabled = config.Bind("General", "Enabled", true, "Applies this preset's settings to the mods. Your own config files stay as they are, and in-game config menus like InLobbyConfig show and change your own values, which only take effect once this is off. Takes effect after restarting the game.");
                if (!enabled.Value)
                {
                    Log.LogInfo($"{name} is off.");
                    continue;
                }
                foreach (var path in Directory.GetFiles(directory, "*.cfg"))
                {
                    string fileName = Path.GetFileName(path);
                    if (!files.TryGetValue(fileName, out var settings))
                    {
                        settings = files[fileName] = new Dictionary<ConfigDefinition, string>();
                    }
                    foreach (var (definition, value) in Read(path))
                    {
                        if (settings.ContainsKey(definition))
                        {
                            Log.LogWarning($"{name} sets [{definition.Section}] {definition.Key} in {fileName}, which another preset sets already.");
                            continue;
                        }
                        settings[definition] = value;
                    }
                }
                Log.LogInfo($"{name} is on.");
            }
        }

        // the preset's value of a setting, if a preset that is on sets it
        public static bool TryGetValue(ConfigFile config, ConfigDefinition definition, out string value)
        {
            Load();
            value = null;
            return config.ConfigFilePath != null
                && files.TryGetValue(Path.GetFileName(config.ConfigFilePath), out var settings)
                && settings.TryGetValue(definition, out value);
        }

        // read like BepInEx does: [Section] headers and Key = Value lines, comments left out
        private static Dictionary<ConfigDefinition, string> Read(string path)
        {
            var settings = new Dictionary<ConfigDefinition, string>();
            string section = "";
            foreach (var rawLine in File.ReadAllLines(path))
            {
                var line = rawLine.Trim();
                if (line.StartsWith("#"))
                {
                    continue;
                }
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    section = line.Substring(1, line.Length - 2);
                    continue;
                }
                var split = line.Split(new[] { '=' }, 2);
                if (split.Length == 2)
                {
                    settings[new ConfigDefinition(section, split[0].Trim())] = split[1].Trim();
                }
            }
            return settings;
        }
    }
}
