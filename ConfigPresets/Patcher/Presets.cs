using BepInEx;
using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace ConfigPresets
{
    // The installed presets. A package ships them in a ConfigPreset folder, with a folder for each preset that is named
    // after it and holds config files named like those in BepInEx/config. The player picks one preset of each package, or
    // none, in ConfigPresets' own config file.
    public static class Presets
    {
        public const string FolderName = "ConfigPreset";
        // holds the name of the preset that is picked until the player picks another
        public const string DefaultFileName = "default.txt";
        public const string Off = "Off";

        private class Package
        {
            public string name;
            // Off first, then the presets
            public string[] choices;
            // each preset's settings, by config file name, e.g. Def.BazaarIsMyHaven.cfg
            public Dictionary<string, Dictionary<ConfigDefinition, string>>[] files;
            public ConfigEntryBase choice;
        }

        private static readonly Dictionary<ConfigDefinition, string> none = new Dictionary<ConfigDefinition, string>();
        private static readonly List<Package> packages = new List<Package>();
        // the settings of the presets that are picked, by config file name
        private static Dictionary<string, Dictionary<ConfigDefinition, string>> files = new Dictionary<string, Dictionary<ConfigDefinition, string>>(StringComparer.OrdinalIgnoreCase);
        // Until the title screen is shown, the mods use the player's settings. So what they do with their settings while
        // the game starts, like updating an old config, is saved.
        private static bool active;

        /// <summary>
        /// ConfigPresets' own config file, with a setting for each package that ships presets. Null until the patcher ran.
        /// </summary>
        public static ConfigFile Config { get; private set; }

        /// <summary>
        /// Whether the presets that are picked apply. They do from when the title screen is shown.
        /// </summary>
        public static bool Active => active;

        /// <summary>
        /// The setting for each package that ships presets: an enum of Off and the package's presets.
        /// </summary>
        public static IEnumerable<ConfigEntryBase> Choices => packages.Select(package => package.choice);

        // after the player picked another preset
        internal static event Action Changed;

        internal static void Load()
        {
            Config = new ConfigFile(Path.Combine(Paths.ConfigPath, Patcher.GUID + ".cfg"), true);
            string[] directories;
            try
            {
                directories = Directory.GetDirectories(Paths.PluginPath, FolderName, SearchOption.AllDirectories);
            }
            catch (Exception exception)
            {
                Patcher.Log.LogError($"Could not look for presets: {exception}");
                return;
            }
            var module = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("ConfigPresets.Choices"), AssemblyBuilderAccess.Run).DefineDynamicModule("ConfigPresets.Choices");
            foreach (var (name, directory) in directories.Select(directory => (name: PackageName(directory), directory)).OrderBy(package => package.name, StringComparer.OrdinalIgnoreCase))
            {
                if (packages.Any(package => package.name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    Patcher.Log.LogWarning($"Skipped the presets in {directory}, since another package is named {name} too.");
                    continue;
                }
                var package = LoadPackage(name, directory);
                if (package == null)
                {
                    continue;
                }
                var builder = module.DefineEnum("ConfigPresets.Choices.Package" + packages.Count, TypeAttributes.Public, typeof(int));
                for (int i = 0; i < package.choices.Length; i++)
                {
                    builder.DefineLiteral(package.choices[i], i);
                }
                var type = builder.CreateTypeInfo().AsType();
                var defaultChoice = DefaultChoice(package, directory);
                var description = new ConfigDescription($"Which of {name}'s presets the mods use, or Off for your own settings. Applies from the title screen on, and right away when picked there or later; settings that a mod only reads when the game starts keep your own values. Your config files keep your own values too; while a preset sets a setting, changing it in-game only lasts until the game restarts.");
                var bind = typeof(ConfigFile).GetMethods()
                    .Single(method => method.Name == nameof(ConfigFile.Bind) && method.GetParameters().Select(parameter => parameter.ParameterType.Name).SequenceEqual(new[] { nameof(ConfigDefinition), "T", nameof(ConfigDescription) }))
                    .MakeGenericMethod(type);
                package.choice = (ConfigEntryBase)bind.Invoke(Config, new object[] { new ConfigDefinition("Presets", KeyName(name)), Enum.ToObject(type, defaultChoice), description });
                packages.Add(package);
            }
            Config.SettingChanged += (sender, args) =>
            {
                if (active && packages.Any(package => package.choice == args.ChangedSetting))
                {
                    Pick();
                    Changed?.Invoke();
                }
            };
        }

        private static Package LoadPackage(string name, string directory)
        {
            var choices = new List<string> { Off };
            var presetFiles = new List<Dictionary<string, Dictionary<ConfigDefinition, string>>> { null };
            foreach (var presetDirectory in Directory.GetDirectories(directory).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                var preset = Path.GetFileName(presetDirectory);
                // an enum name, which the config file reads back
                if (preset.Trim() != preset || preset.Contains(",") || char.IsDigit(preset[0]) || preset[0] == '-' || preset[0] == '+' || choices.Contains(preset, StringComparer.OrdinalIgnoreCase))
                {
                    Patcher.Log.LogWarning($"Skipped {name}'s preset {preset}: its name must not be {Off}, start with a digit, + or -, contain a comma or start or end with a space.");
                    continue;
                }
                var settings = new Dictionary<string, Dictionary<ConfigDefinition, string>>(StringComparer.OrdinalIgnoreCase);
                foreach (var path in Directory.GetFiles(presetDirectory, "*.cfg"))
                {
                    settings[Path.GetFileName(path)] = ConfigReader.Read(path);
                }
                choices.Add(preset);
                presetFiles.Add(settings);
            }
            if (choices.Count == 1)
            {
                Patcher.Log.LogWarning($"{name} has a {FolderName} folder without presets in it, which need a folder each.");
                return null;
            }
            return new Package { name = name, choices = choices.ToArray(), files = presetFiles.ToArray() };
        }

        // the package's folder in BepInEx/plugins
        private static string PackageName(string directory)
        {
            var relative = directory.Substring(Paths.PluginPath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var first = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];
            return first.Equals(FolderName, StringComparison.OrdinalIgnoreCase) ? "plugins" : first;
        }

        // a setting's key can't have some characters
        private static string KeyName(string name)
        {
            foreach (var character in new[] { '=', '\n', '\t', '\\', '"', '\'', '[', ']' })
            {
                name = name.Replace(character, '_');
            }
            return name;
        }

        private static int DefaultChoice(Package package, string directory)
        {
            var path = Path.Combine(directory, DefaultFileName);
            if (!File.Exists(path))
            {
                return 0;
            }
            var preset = File.ReadAllLines(path).Select(line => line.Trim()).FirstOrDefault(line => line.Length > 0);
            var index = Array.FindIndex(package.choices, choice => choice.Equals(preset, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
            {
                Patcher.Log.LogWarning($"{package.name}'s {DefaultFileName} names the preset {preset}, which it doesn't have.");
                return 0;
            }
            return index;
        }

        /// <summary>
        /// Applies the presets that are picked, once the game has started. Called when the title screen is shown.
        /// </summary>
        public static void Activate()
        {
            if (active)
            {
                return;
            }
            active = true;
            Pick();
            Changed?.Invoke();
        }

        // gathers the settings of the presets that are picked
        private static void Pick()
        {
            files = new Dictionary<string, Dictionary<ConfigDefinition, string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var package in packages)
            {
                int index = Convert.ToInt32(package.choice.BoxedValue);
                if (index <= 0 || index >= package.choices.Length)
                {
                    Patcher.Log.LogInfo($"{package.name}: {Off}");
                    continue;
                }
                foreach (var (fileName, presetSettings) in package.files[index])
                {
                    if (!files.TryGetValue(fileName, out var settings))
                    {
                        settings = files[fileName] = new Dictionary<ConfigDefinition, string>();
                    }
                    foreach (var (definition, value) in presetSettings)
                    {
                        if (settings.ContainsKey(definition))
                        {
                            Patcher.Log.LogWarning($"{package.name}'s preset {package.choices[index]} sets [{definition.Section}] {definition.Key} in {fileName}, which another preset sets already.");
                            continue;
                        }
                        settings[definition] = value;
                    }
                }
                Patcher.Log.LogInfo($"{package.name}: {package.choices[index]}");
            }
        }

        // the settings that the presets that are picked set in a config file, by its file name
        internal static Dictionary<ConfigDefinition, string> Settings(string fileName)
        {
            return files.TryGetValue(fileName, out var settings) ? settings : none;
        }
    }
}
