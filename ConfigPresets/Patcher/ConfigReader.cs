using BepInEx.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

        // writes the settings as a config file that Read reads back, or deletes the file if there are none
        public static void Write(string path, Dictionary<ConfigDefinition, string> settings)
        {
            if (settings.Count == 0)
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                return;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var lines = new List<string>();
            foreach (var section in settings.GroupBy(setting => setting.Key.Section).OrderBy(section => section.Key))
            {
                if (lines.Count > 0)
                {
                    lines.Add("");
                }
                lines.Add($"[{section.Key}]");
                lines.AddRange(section.OrderBy(setting => setting.Key.Key).Select(setting => $"{setting.Key.Key} = {setting.Value}"));
            }
            File.WriteAllLines(path, lines);
        }
    }
}
