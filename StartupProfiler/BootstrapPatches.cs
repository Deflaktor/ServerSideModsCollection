using BepInEx.Bootstrap;
using HarmonyLib;
using System;
using System.Runtime.CompilerServices;

namespace StartupProfiler
{
    // Hooks BepInEx's loading of the plugins. Only once they are loaded are the game and Unity hooked: touching their
    // assemblies from the preloader would load them before BepInEx has patched them.
    public static class BootstrapPatches
    {
        public static readonly Harmony Harmony = new Harmony("com.Def.StartupProfiler");

        public static void Apply()
        {
            Harmony.Patch(AccessTools.Method(typeof(Chainloader), nameof(Chainloader.Start)),
                prefix: new HarmonyMethod(typeof(BootstrapPatches), nameof(ChainloaderStartPrefix)),
                postfix: new HarmonyMethod(typeof(BootstrapPatches), nameof(ChainloaderStartPostfix)));
        }

        private static void ChainloaderStartPrefix()
        {
            Timeline.Mark("Chainloader started: loading the plugins");
        }

        private static void ChainloaderStartPostfix()
        {
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

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void HookGame()
        {
            GamePatches.Apply(Harmony);
            FrameWatcher.Create();
            Timeline.Mark("Profiler hooked the game");
        }
    }
}
