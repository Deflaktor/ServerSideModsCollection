using HarmonyLib;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace StartupProfiler
{
    // Times going through the types of every assembly, which takes long with many mods, MMHOOK's assemblies most of all,
    // and the lookups of the assemblies those types refer to, which BepInEx makes in its folders for each one no one
    // loaded yet.
    public static class TypeScanPatches
    {
        public const string TypeScanCategory = "Post-processing's type scan";
        public const string LookupCategory = "Assembly lookups";

        private struct Start
        {
            public double time;
            public long ticks;
        }

        private static Timeline.Timing postProcessingScan;

        // From the preloader, so that the plugins' lookups count too. BepInEx's resolver is in its own assembly, which is
        // loaded by then; the game's and Unity's aren't touched.
        public static void ApplyResolver(Harmony harmony)
        {
            var preloader = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly => assembly.GetName().Name == "BepInEx.Preloader");
            var localResolve = AccessTools.Method(preloader?.GetType("BepInEx.Preloader.PreloaderRunner"), "LocalResolve");
            if (localResolve == null)
            {
                Timeline.Log.LogWarning("Could not find BepInEx's assembly resolver; the assembly lookups are missing from the report.");
                return;
            }
            harmony.Patch(localResolve,
                prefix: new HarmonyMethod(typeof(TypeScanPatches), nameof(StartPrefix)),
                finalizer: new HarmonyMethod(typeof(TypeScanPatches), nameof(LookupFinalizer)));
        }

        // Unity's post-processing goes through all types for its effects when the first post-processing volume is enabled
        public static void ApplyGame(Harmony harmony)
        {
            var postProcessing = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly => assembly.GetName().Name == "Unity.Postprocessing.Runtime");
            var reloadBaseTypes = AccessTools.Method(postProcessing?.GetType("UnityEngine.Rendering.PostProcessing.PostProcessManager"), "ReloadBaseTypes");
            // the compiler's lambda that gets each assembly's types
            var getTypes = postProcessing?.GetType("UnityEngine.Rendering.PostProcessing.RuntimeUtilities+<>c")?
                .GetMethods(AccessTools.allDeclared).FirstOrDefault(method => method.Name.Contains("GetAllAssemblyTypes"));
            if (reloadBaseTypes == null || getTypes == null)
            {
                Timeline.Log.LogWarning("Could not find post-processing's type scan; it is missing from the report.");
                return;
            }
            postProcessingScan = Timeline.GetTiming(GamePatches.PhaseCategory, "Post-processing's type scan (PostProcessManager)", "Unity.Postprocessing.Runtime");
            harmony.Patch(reloadBaseTypes,
                prefix: new HarmonyMethod(typeof(TypeScanPatches), nameof(StartPrefix)),
                finalizer: new HarmonyMethod(typeof(TypeScanPatches), nameof(ScanFinalizer)));
            harmony.Patch(getTypes,
                prefix: new HarmonyMethod(typeof(TypeScanPatches), nameof(StartPrefix)),
                finalizer: new HarmonyMethod(typeof(TypeScanPatches), nameof(GetTypesFinalizer)));
        }

        private static void StartPrefix(out Start __state)
        {
            __state = new Start { time = Timeline.Now, ticks = Stopwatch.GetTimestamp() };
        }

        private static void LookupFinalizer(ResolveEventArgs args, Assembly __result, Start __state)
        {
            string name = args?.Name?.Split(',')[0] ?? "(no name)";
            Timeline.GetTiming(LookupCategory, __result != null ? name : $"{name}, not found", null).Add(__state.time, Stopwatch.GetTimestamp() - __state.ticks);
        }

        private static void ScanFinalizer(Start __state)
        {
            postProcessingScan.Add(__state.time, Stopwatch.GetTimestamp() - __state.ticks);
        }

        private static void GetTypesFinalizer(Assembly t, Start __state)
        {
            Timeline.GetTiming(TypeScanCategory, t?.GetName().Name ?? "(no assembly)", null).Add(__state.time, Stopwatch.GetTimestamp() - __state.ticks);
        }
    }
}
