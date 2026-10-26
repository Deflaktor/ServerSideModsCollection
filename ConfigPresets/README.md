# ConfigPresets

Switch between sets of mod settings while the game is running.

[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/E1E71PHUJ2)

## Motivation

Mod managers like Gale and r2modman only deal with config files before the game starts. A modpack's configs get copied over yours, and if you want other settings, you have to edit files or switch profiles and restart the game.

With ConfigPresets, modpacks and mods come with presets: ready-made settings for any mods. You pick one in the lobby or in the settings menu and it applies right away. Your own config files stay as they are, so picking Off brings your own settings back.

ConfigPresets does nothing on its own. It's for modpacks and mods that come with presets.

**It only works with mods that pick up setting changes while the game is running.** Presets are applied once the title screen shows up. If a mod reads a setting only while the game starts, that setting keeps your own value. Also it is not possible to add or remove mods (only enable/disable them if they support it).

## For players

You pick one preset for each package that has some, or Off to use your own settings. You can do that:

- in the lobby with InLobbyConfig, under Config Presets
- in the game's settings menu with Risk of Options
- in `BepInEx/config/Def.ConfigPresets.cfg`, which mod managers show in their config editor

A new pick applies right away. Before the title screen, mods always use your own settings, so anything they change while starting up, like updating an old config, is saved as usual.

Each preset shows up twice: as it is, e.g. `Hard`, and with your own changes, `Hard (custom)`. If you change a setting that the preset sets, the change goes into the custom version, and that one gets picked. Your changes are stored in `BepInEx/config/Def.ConfigPresets/<package>/<preset>/`. Setting a value back to the preset's value removes your change. Your own config files are never touched.

Settings that no preset sets work as usual and are saved to your config files.

If two picked presets set the same setting, the package that comes first alphabetically wins, and the log tells you.

## For modpack and mod authors

Add a `ConfigPreset` folder to your package with one folder per preset. The folder name is the preset name players see. Inside go config files named like the ones in `BepInEx/config`, e.g. `ConfigPreset/Hard/Def.BazaarIsMyHaven.cfg`. Only put in the settings you want to set. Then add ConfigPresets as a dependency.

Preset names can't be `Off`, can't start with a digit, `+` or `-`, can't contain a comma, can't start or end with a space, and can't end with ` (custom)`.

Players start with Off. To pick a preset by default, like a modpack would, write its name into `ConfigPreset/default.txt`.

Don't put the config files in a `config` folder. Mod managers copy that folder over the player's own configs.

Mods don't need any changes, as long as they pick up setting changes while the game runs, i.e. read the value when they need it or listen to `SettingChanged`. A setting that a mod only reads at startup, e.g. to register an item or decide which hooks to add, keeps the player's value. So check that every setting in your preset actually does something.

If a mod changes a setting that a preset sets, that counts as the player's change to the preset. It doesn't end up in the mod's config file.

## How it works

ConfigPresets comes in two parts:

- A preloader patcher in `BepInEx/patchers`. It loads before any mod, so it can reach every mod's settings, and it applies the presets once the title screen shows up.
- A plugin that tells the patcher when the title screen is up, and adds the preset choice to InLobbyConfig and Risk of Options.
