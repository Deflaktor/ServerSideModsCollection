using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace ConfigPresets
{
    // The mods get the presets' settings, while their config files keep the player's.
    //
    // For the settings the presets that are picked have, a mod's config file holds the presets' values: when the file is
    // loaded, on every reload, for settings bound later, and when the player picks another preset. So whatever way a mod
    // or a config menu reads them, it gets the preset's value. Saving writes the player's values instead, so the file
    // never changes because of a preset, and changing such a setting in-game only lasts until the file is loaded again.
    internal static class ConfigOverride
    {
        private class FileState
        {
            public string fileName;
            // the settings the presets set in the file, as its entries have them
            public Dictionary<ConfigDefinition, string> preset = new Dictionary<ConfigDefinition, string>();
            // the player's values, as the file has them
            public Dictionary<ConfigDefinition, string> player = new Dictionary<ConfigDefinition, string>();
            public bool refreshed;
        }

        private static readonly ConditionalWeakTable<ConfigFile, FileState> files = new ConditionalWeakTable<ConfigFile, FileState>();
        private static readonly List<WeakReference<ConfigFile>> allFiles = new List<WeakReference<ConfigFile>>();
        private static Func<ConfigFile, Dictionary<ConfigDefinition, ConfigEntryBase>> getEntries;
        private static Func<ConfigFile, Dictionary<ConfigDefinition, string>> getOrphanedEntries;
        // the file being saved, whose entries write the player's values
        [ThreadStatic]
        private static FileState saving;

        public static void Apply()
        {
            getEntries = AccessTools.MethodDelegate<Func<ConfigFile, Dictionary<ConfigDefinition, ConfigEntryBase>>>(AccessTools.PropertyGetter(typeof(ConfigFile), "Entries"));
            getOrphanedEntries = AccessTools.MethodDelegate<Func<ConfigFile, Dictionary<ConfigDefinition, string>>>(AccessTools.PropertyGetter(typeof(ConfigFile), "OrphanedEntries"));
            var harmony = new Harmony(Patcher.GUID);
            harmony.Patch(AccessTools.Constructor(typeof(ConfigFile), new[] { typeof(string), typeof(bool), typeof(BepInPlugin) }),
                postfix: new HarmonyMethod(typeof(ConfigOverride), nameof(ConfigFile_Constructor)));
            harmony.Patch(AccessTools.Method(typeof(ConfigFile), "OnConfigReloaded"),
                prefix: new HarmonyMethod(typeof(ConfigOverride), nameof(ConfigFile_OnConfigReloaded)));
            harmony.Patch(AccessTools.Method(typeof(ConfigFile), nameof(ConfigFile.Save)),
                prefix: new HarmonyMethod(typeof(ConfigOverride), nameof(ConfigFile_Save)),
                finalizer: new HarmonyMethod(typeof(ConfigOverride), nameof(ConfigFile_SaveFinalizer)));
            harmony.Patch(AccessTools.Method(typeof(ConfigEntryBase), nameof(ConfigEntryBase.GetSerializedValue)),
                postfix: new HarmonyMethod(typeof(ConfigOverride), nameof(ConfigEntryBase_GetSerializedValue)));
            Presets.Changed += RefreshAll;
        }

        private static FileState Get(ConfigFile file)
        {
            if (file == null || file.ConfigFilePath == null || file == Presets.Config)
            {
                return null;
            }
            if (!files.TryGetValue(file, out var state))
            {
                state = new FileState { fileName = Path.GetFileName(file.ConfigFilePath) };
                files.Add(file, state);
                allFiles.Add(new WeakReference<ConfigFile>(file));
            }
            return state;
        }

        // a file that doesn't exist yet isn't loaded, so its settings get the presets' values here
        private static void ConfigFile_Constructor(ConfigFile __instance)
        {
            var state = Get(__instance);
            if (state != null && !state.refreshed)
            {
                Refresh(__instance, state);
            }
        }

        // the file was loaded or reloaded, before the mods hear of it
        private static void ConfigFile_OnConfigReloaded(ConfigFile __instance)
        {
            var state = Get(__instance);
            if (state != null)
            {
                Refresh(__instance, state);
            }
        }

        // the player picked another preset
        private static void RefreshAll()
        {
            allFiles.RemoveAll(reference => !reference.TryGetTarget(out _));
            foreach (var reference in allFiles)
            {
                if (reference.TryGetTarget(out var file) && files.TryGetValue(file, out var state) && state.refreshed)
                {
                    Refresh(file, state);
                }
            }
        }

        private static void Refresh(ConfigFile file, FileState state)
        {
            state.refreshed = true;
            state.player = ConfigReader.Read(file.ConfigFilePath);
            var previous = state.preset;
            var preset = state.preset = Presets.Settings(state.fileName);
            if (previous.Count == 0 && preset.Count == 0)
            {
                return;
            }
            var entries = getEntries(file);
            var orphaned = getOrphanedEntries(file);
            // the file keeps the player's values anyway
            var saveOnConfigSet = file.SaveOnConfigSet;
            file.SaveOnConfigSet = false;
            try
            {
                foreach (var definition in previous.Keys)
                {
                    if (preset.ContainsKey(definition))
                    {
                        continue;
                    }
                    if (entries.TryGetValue(definition, out var entry))
                    {
                        Set(entry, PlayerValue(state, entry));
                    }
                    else if (state.player.TryGetValue(definition, out var value))
                    {
                        orphaned[definition] = value;
                    }
                    else
                    {
                        orphaned.Remove(definition);
                    }
                }
                foreach (var (definition, value) in preset)
                {
                    if (entries.TryGetValue(definition, out var entry))
                    {
                        Set(entry, value);
                    }
                    else
                    {
                        // the mod binds it later and takes it from here
                        orphaned[definition] = value;
                    }
                }
            }
            finally
            {
                file.SaveOnConfigSet = saveOnConfigSet;
            }
        }

        private static void Set(ConfigEntryBase entry, string value)
        {
            if (entry.GetSerializedValue() != value)
            {
                entry.SetSerializedValue(value);
            }
        }

        // the player's value of a mod's entry: what the file has, or else the setting's default
        private static string PlayerValue(FileState state, ConfigEntryBase entry)
        {
            return state.player.TryGetValue(entry.Definition, out var value) ? value : TomlTypeConverter.ConvertToString(entry.DefaultValue, entry.SettingType);
        }

        // the settings not bound yet are written as the player had them, or not at all if they weren't in the file
        private static void ConfigFile_Save(ConfigFile __instance, out Dictionary<ConfigDefinition, string> __state)
        {
            __state = null;
            var state = Get(__instance);
            if (state == null || state.preset.Count == 0)
            {
                return;
            }
            saving = state;
            var orphaned = getOrphanedEntries(__instance);
            __state = new Dictionary<ConfigDefinition, string>();
            foreach (var definition in state.preset.Keys)
            {
                if (orphaned.TryGetValue(definition, out var value))
                {
                    __state[definition] = value;
                    if (state.player.TryGetValue(definition, out var playerValue))
                    {
                        orphaned[definition] = playerValue;
                    }
                    else
                    {
                        orphaned.Remove(definition);
                    }
                }
            }
        }

        private static Exception ConfigFile_SaveFinalizer(ConfigFile __instance, Dictionary<ConfigDefinition, string> __state, Exception __exception)
        {
            saving = null;
            if (__state != null)
            {
                var orphaned = getOrphanedEntries(__instance);
                foreach (var (definition, value) in __state)
                {
                    orphaned[definition] = value;
                }
            }
            return __exception;
        }

        // while its file is saved, a mod's entry that a preset sets writes the player's value
        private static void ConfigEntryBase_GetSerializedValue(ConfigEntryBase __instance, ref string __result)
        {
            var state = saving;
            if (state == null || !state.preset.ContainsKey(__instance.Definition) || !files.TryGetValue(__instance.ConfigFile, out var own) || own != state
                || !getEntries(__instance.ConfigFile).TryGetValue(__instance.Definition, out var registered) || registered != __instance)
            {
                return;
            }
            __result = PlayerValue(state, __instance);
        }
    }
}
