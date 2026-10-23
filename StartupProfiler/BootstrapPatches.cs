using HarmonyLib;
using System;
using System.Runtime.CompilerServices;

namespace StartupProfiler
{
    // Follows BepInEx's loading of the plugins by its log, as the lines come in. Only once they are loaded are the game
    // and Unity hooked: touching their assemblies from the preloader, also by hooking BepInEx's chainloader, which uses
    // Unity, would load them before BepInEx has patched them, and the game wouldn't start.
    public static class BootstrapPatches
    {
        public static readonly Harmony Harmony = new Harmony("com.Def.StartupProfiler");
        private static bool hooked;

        public static void OnLogLine(Timeline.LogLine line)
        {
            if (line.source != "BepInEx")
            {
                return;
            }
            if (line.text == "Chainloader started")
            {
                Timeline.Mark("Chainloader started: loading the plugins");
            }
            else if (line.text.StartsWith("Chainloader startup complete") && !hooked)
            {
                hooked = true;
                Timeline.Mark("Chainloader finished: all plugins loaded");
                try
                {
                    HookGame();
                }
                catch (Exception exception)
                {
                    Timeline.Log.LogError($"Could not hook the game's start-up: {exception}");
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void HookGame()
        {
            GamePatches.Apply(Harmony);
            FrameWatcher.Create();
            Timeline.Mark("Profiler hooked the game");
        }
    }
}
