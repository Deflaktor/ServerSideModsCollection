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
    // none, in ConfigPresets' own config file. Each preset can be picked as it is or with the player's edits, which are
    // kept in BepInEx/config/Def.ConfigPresets/<package>/<preset>/.
    public static class Presets
    {
        public const string FolderName = "ConfigPreset";
        // holds the name of the preset that is picked until the player picks another
        public const string DefaultFileName = "default.txt";
        public const string Off = "Off";
        public const string CustomSuffix = " (custom)";

        private class Package
        {
            public string name;
            // Off first, then each preset as it is and with the player's edits
            public string[] choices;
            // each choice's preset, null for Off
            public string[] presets;
            public bool[] custom;
            // each choice's preset settings, by config file name, e.g. Def.BazaarIsMyHaven.cfg
            public Dictionary<string, Dictionary<ConfigDefinition, string>>[] files;
            // the player's edits of each preset, by preset name and config file name
            public Dictionary<string, Dictionary<string, Dictionary<ConfigDefinition, string>>> edits = new Dictionary<string, Dictionary<string, Dictionary<ConfigDefinition, string>>>(StringComparer.OrdinalIgnoreCase);
            public ConfigEntryBase choice;

            public int Index => Convert.ToInt32(choice.BoxedValue);

            public string EditPath(string preset, string fileName) => Path.Combine(Paths.ConfigPath, Patcher.GUID, name, preset, fileName);
        }

        private static readonly Dictionary<ConfigDefinition, string> none = new Dictionary<ConfigDefinition, string>();
        private static readonly List<Package> packages = new List<Package>();
        // the settings of the presets that are picked, by config file name
        private static Dictionary<string, Dictionary<ConfigDefinition, string>> files = new Dictionary<string, Dictionary<ConfigDefinition, string>>(StringComparer.OrdinalIgnoreCase);
        // the package whose preset sets each of those settings
        private static Dictionary<string, Dictionary<ConfigDefinition, Package>> owners = new Dictionary<string, Dictionary<ConfigDefinition, Package>>(StringComparer.OrdinalIgnoreCase);
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
                var description = new ConfigDescription($"Which of {name}'s presets the mods use, or Off for your own settings. A preset's{CustomSuffix} choice has your edits of its settings, and editing a setting of a preset picked as it is picks that. Applies from the title screen on, and right away when picked there or later; settings that a mod only reads when the game starts keep your own values. Your config files keep your own values too.");
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
            var package = new Package { name = name };
            var choices = new List<string> { Off };
            var presets = new List<string> { null };
            var presetFiles = new List<Dictionary<string, Dictionary<ConfigDefinition, string>>> { null };
            foreach (var presetDirectory in Directory.GetDirectories(directory).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                var preset = Path.GetFileName(presetDirectory);
                // an enum name, which the config file reads back
                if (preset.Trim() != preset || preset.Contains(",") || char.IsDigit(preset[0]) || preset[0] == '-' || preset[0] == '+' || preset.EndsWith(CustomSuffix, StringComparison.OrdinalIgnoreCase)
                    || choices.Contains(preset, StringComparer.OrdinalIgnoreCase))
                {
                    Patcher.Log.LogWarning($"Skipped {name}'s preset {preset}: its name must not be {Off}, start with a digit, + or -, contain a comma, start or end with a space or end with{CustomSuffix}.");
                    continue;
                }
                var settings = new Dictionary<string, Dictionary<ConfigDefinition, string>>(StringComparer.OrdinalIgnoreCase);
                foreach (var path in Directory.GetFiles(presetDirectory, "*.cfg"))
                {
                    settings[Path.GetFileName(path)] = ConfigReader.Read(path);
                }
                choices.Add(preset);
                presets.Add(preset);
                presetFiles.Add(settings);
                choices.Add(preset + CustomSuffix);
                presets.Add(preset);
                presetFiles.Add(settings);
                var edits = package.edits[preset] = new Dictionary<string, Dictionary<ConfigDefinition, string>>(StringComparer.OrdinalIgnoreCase);
                var editDirectory = Path.GetDirectoryName(package.EditPath(preset, "_"));
                if (Directory.Exists(editDirectory))
                {
                    foreach (var path in Directory.GetFiles(editDirectory, "*.cfg"))
                    {
                        edits[Path.GetFileName(path)] = ConfigReader.Read(path);
                    }
                }
            }
            if (choices.Count == 1)
            {
                Patcher.Log.LogWarning($"{name} has a {FolderName} folder without presets in it, which need a folder each.");
                return null;
            }
            package.choices = choices.ToArray();
            package.presets = presets.ToArray();
            package.custom = choices.Select(choice => choice.EndsWith(CustomSuffix)).ToArray();
            package.files = presetFiles.ToArray();
            return package;
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
        private static void Pick(bool log = true)
        {
            files = new Dictionary<string, Dictionary<ConfigDefinition, string>>(StringComparer.OrdinalIgnoreCase);
            owners = new Dictionary<string, Dictionary<ConfigDefinition, Package>>(StringComparer.OrdinalIgnoreCase);
            foreach (var package in packages)
            {
                int index = package.Index;
                if (index <= 0 || index >= package.choices.Length)
                {
                    if (log)
                    {
                        Patcher.Log.LogInfo($"{package.name}: {Off}");
                    }
                    continue;
                }
                var edits = package.custom[index] ? package.edits[package.presets[index]] : null;
                foreach (var (fileName, presetSettings) in package.files[index])
                {
                    if (!files.TryGetValue(fileName, out var settings))
                    {
                        settings = files[fileName] = new Dictionary<ConfigDefinition, string>();
                        owners[fileName] = new Dictionary<ConfigDefinition, Package>();
                    }
                    Dictionary<ConfigDefinition, string> fileEdits = null;
                    edits?.TryGetValue(fileName, out fileEdits);
                    foreach (var (definition, value) in presetSettings)
                    {
                        if (settings.ContainsKey(definition))
                        {
                            if (log)
                            {
                                Patcher.Log.LogWarning($"{package.name}'s preset {package.choices[index]} sets [{definition.Section}] {definition.Key} in {fileName}, which another preset sets already.");
                            }
                            continue;
                        }
                        settings[definition] = fileEdits != null && fileEdits.TryGetValue(definition, out var edited) ? edited : value;
                        owners[fileName][definition] = package;
                    }
                }
                if (log)
                {
                    Patcher.Log.LogInfo($"{package.name}: {package.choices[index]}");
                }
            }
        }

        // The player or a mod changed a setting that a preset sets. It's kept as an edit of that preset, which is picked
        // with the player's edits from now on.
        internal static void Edit(string fileName, ConfigEntryBase entry, Func<string, string> normalize)
        {
            if (!owners.TryGetValue(fileName, out var fileOwners) || !fileOwners.TryGetValue(entry.Definition, out var package))
            {
                return;
            }
            int index = package.Index;
            var preset = package.presets[index];
            var edits = package.edits[preset];
            if (!edits.TryGetValue(fileName, out var fileEdits))
            {
                fileEdits = edits[fileName] = new Dictionary<ConfigDefinition, string>();
            }
            var value = entry.GetSerializedValue();
            bool unedited = value == normalize(package.files[index][fileName][entry.Definition]);
            if (unedited && !package.custom[index])
            {
                return;
            }
            if (unedited)
            {
                fileEdits.Remove(entry.Definition);
            }
            else
            {
                fileEdits[entry.Definition] = value;
            }
            try
            {
                ConfigReader.Write(package.EditPath(preset, fileName), fileEdits);
            }
            catch (Exception exception)
            {
                Patcher.Log.LogError($"Could not save the edits of {package.name}'s preset {preset}: {exception}");
            }
            if (package.custom[index])
            {
                Pick(false);
                Changed?.Invoke();
            }
            else
            {
                Patcher.Log.LogInfo($"[{entry.Definition.Section}] {entry.Definition.Key} in {fileName} was edited, so {package.name}'s preset {preset} is picked with your edits.");
                // which picks the presets again
                package.choice.BoxedValue = Enum.ToObject(package.choice.SettingType, index + 1);
            }
        }

        // the settings that the presets that are picked set in a config file, by its file name
        internal static Dictionary<ConfigDefinition, string> Settings(string fileName)
        {
            return files.TryGetValue(fileName, out var settings) ? settings : none;
        }
    }
}
