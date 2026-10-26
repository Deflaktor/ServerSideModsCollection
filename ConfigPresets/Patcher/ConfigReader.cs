using BepInEx.Configuration;
using System.Collections.Generic;
using System.IO;

namespace ConfigPresets
{
    internal static class ConfigReader
    {
        // the settings of a config file, read like BepInEx does: [Section] headers and Key = Value lines, comments left out
        public static Dictionary<ConfigDefinition, string> Read(string path)
        {
            var settings = new Dictionary<ConfigDefinition, string>();
            if (path == null || !File.Exists(path))
            {
                return settings;
            }
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
