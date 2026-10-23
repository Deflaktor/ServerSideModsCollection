# StartupProfiler

Finds out what makes Risk of Rain 2 slow to start. It times the start-up up to the main menu and writes a report to `BepInEx/StartupProfile.txt`. A summary also goes to the log.

It is a preloader patcher, which goes into `BepInEx/patchers`, and the first one to run, so it starts before the other patchers and every plugin. It changes nothing in the game and is only needed while you look into a slow start.

## The report

- **Overview**: Unity's own start-up before any mod, BepInEx's patchers, loading the plugins, and the game loading to the main menu.
- **Plugins**: how long each plugin took to load and run its `Awake`.
- **Content pack providers**: the game's and each mod's content, by the time spent in them.
- **System initializers**: the game's and the mods' initializers, by the time spent in them, also summed up by assembly.
- **Timeline**: when each phase of the game's start-up began and how long it took.
- **Longest frames** and **longest silences between log lines**: where the game stood still, with what was logged around it, e.g. a mod hooking many methods.

CPU times count only the time spent in that code itself, not the frames it waited for.
