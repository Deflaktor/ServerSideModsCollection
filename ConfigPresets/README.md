# ConfigPresets

A library that lets a modpack set the settings of its mods without overwriting your own config files.

[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/E1E71PHUJ2)

## Motivation

Mod managers like Gale and r2modman install a modpack's config files by copying them over yours. Your own settings are gone, and the next update of the modpack overwrites your changes again.

With ConfigPresets, a modpack ships its settings as a preset instead. While the preset is on, the mods use its settings. Your config files keep your own values, and in-game config menus like InLobbyConfig show and change those. Turn the preset off and the mods use your values again.

ConfigPresets does nothing on its own. Only mods that bind their settings through it take part; other mods keep reading your config files as usual.

## For players

Every installed preset has its own config file in `BepInEx/config`, named after its package, e.g. `Def-SimulacrumEscapeModpack.cfg`. It has one setting:

- `Enabled`: whether the mods use the preset's settings. Needs a game restart.

If two presets that are on set the same setting, the package that comes first alphabetically wins, and the log tells you.

## For modpack authors

Put the config files in a `ConfigPreset` folder in your package, named like the mods' files in `BepInEx/config`, e.g. `ConfigPreset/Def.BazaarIsMyHaven.cfg`. Only put in the settings you want to set. Then add ConfigPresets as a dependency, since the mods themselves don't depend on it.

Don't put them in a `config` folder. Mod managers copy that folder over the player's own configs.

## For mod authors

Bind your settings with `PresetConfig.Bind` instead of `Config.Bind`, through a soft dependency (see below). It takes the same arguments plus the config file:

```csharp
// before
Enabled = Config.Bind("General", "Enabled", true, "Enables the mod.");
// after
Enabled = PresetConfig.Bind(Config, "General", "Enabled", true, "Enables the mod.");
```

The setting is bound in your config file as usual. If a preset that is on sets it, you get your own entry with the preset's value instead:

- Reloading the config file doesn't change it, and changing it isn't saved to the file.
- Its `SettingChanged` event only fires when your mod changes it, not when the player does.
- Looking the setting up in the config file, e.g. with `TryGetEntry`, gives you the player's entry.

`PresetConfig.IsPreset(entry)` tells you whether an entry has a preset's value. `PresetConfig.PlayerEntry(entry)` gives you the player's entry in the config file, e.g. to update an old config file to new settings.

Players don't need ConfigPresets for your mod, only modpacks with presets do. So make it a soft dependency, which loads it before your mod when it's installed:

```csharp
[BepInDependency(ConfigPresets.ConfigPresetsPlugin.PluginGUID, BepInDependency.DependencyFlags.SoftDependency)]
```

Then only call it when it's installed, from a method that isn't inlined, so your mod still loads without it:

```csharp
public static ConfigEntry<T> Bind<T>(ConfigFile config, string section, string key, T defaultValue, string description)
{
    return BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(ConfigPresets.ConfigPresetsPlugin.PluginGUID)
        ? BindPreset(config, section, key, defaultValue, description)
        : config.Bind(section, key, defaultValue, description);
}

[MethodImpl(MethodImplOptions.NoInlining)]
private static ConfigEntry<T> BindPreset<T>(ConfigFile config, string section, string key, T defaultValue, string description)
{
    return PresetConfig.Bind(config, section, key, defaultValue, description);
}
```
