using HarmonyLib;
using RoR2;
using RoR2.ContentManagement;
using RoR2.UI.LogBook;
using RoR2.UI.MainMenu;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace StartupProfiler
{
    // Times the game's start-up: its phases, each content pack provider, the game's and the mods' alike, and each
    // system initializer. The report is written once the main menu starts.
    public static class GamePatches
    {
        public const string PhaseCategory = "Phases";
        public const string ProviderCategory = "Content pack providers";
        public const string InitializerCategory = "System initializers";

        private static readonly Dictionary<MethodBase, string> phaseNames = new Dictionary<MethodBase, string>();

        public static void Apply(Harmony harmony)
        {
            PatchPhase(harmony, AccessTools.Method(typeof(RoR2Application), "InitializeGameRoutine"), "Game start-up (RoR2Application)");
            PatchPhase(harmony, AccessTools.Method(typeof(RoR2Application), "LoadGameContent"), "Loading the game content");
            PatchPhase(harmony, AccessTools.Method(typeof(ContentManager), nameof(ContentManager.LoadContentPacks)), "Content packs");
            PatchPhase(harmony, AccessTools.Method(typeof(Language), "Init"), "Languages");
            PatchPhase(harmony, AccessTools.Method(typeof(CameraRigController), "InitializeReferences"), "Camera references");
            PatchPhase(harmony, AccessTools.Method(typeof(SystemInitializerAttribute), nameof(SystemInitializerAttribute.ExecuteCoroutine)), "System initializers");
            PatchPhase(harmony, AccessTools.Method(typeof(AchievementManager), "DoInit"), "Achievements");
            PatchPhase(harmony, AccessTools.Method(typeof(LogBookController), "Init"), "Logbook");
            Patch(harmony, AccessTools.Method(typeof(InitDuringStartupPhaseAttribute), nameof(InitDuringStartupPhaseAttribute.Execute)), postfix: nameof(StartupPhasePostfix));

            Patch(harmony, AccessTools.Method(typeof(RoR2Application), "Awake"), prefix: nameof(RoR2ApplicationAwakePrefix));
            // the game's own milestones, which it doesn't log
            Patch(harmony, AccessTools.Method(typeof(RoR2Application), nameof(RoR2Application.PrintSW)), prefix: nameof(PrintSWPrefix));
            Patch(harmony, AccessTools.Method(typeof(MainMenuController), "Start"), postfix: nameof(MainMenuStartPostfix));
            TypeScanPatches.ApplyGame(harmony);

            // each initializer, which the game calls by reflection
            var executeCoroutine = AccessTools.Method(typeof(SystemInitializerAttribute), nameof(SystemInitializerAttribute.ExecuteCoroutine));
            Patch(harmony, executeCoroutine != null ? AccessTools.EnumeratorMoveNext(executeCoroutine) : null, transpiler: nameof(InvokeTranspiler));
            Patch(harmony, AccessTools.Method(typeof(SystemInitializerAttribute), nameof(SystemInitializerAttribute.Execute)), transpiler: nameof(InvokeTranspiler));
            // RoR2BepInExPack replaces the coroutine with its own, which never calls the game's. Looked up in its own
            // assembly: AccessTools.TypeByName would go through the types of every assembly, MMHOOK's included, for a minute.
            var bepInExPack = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly => assembly.GetName().Name == "RoR2BepInExPack");
            var bepInExPackInitializers = AccessTools.Method(bepInExPack?.GetType("RoR2BepInExPack.VanillaFixes.FixSystemInitializer"), "LogBadCycle");
            if (bepInExPackInitializers != null)
            {
                PatchPhase(harmony, bepInExPackInitializers, "System initializers (RoR2BepInExPack)");
                Patch(harmony, AccessTools.EnumeratorMoveNext(bepInExPackInitializers), transpiler: nameof(InvokeTranspiler));
            }

            // each content pack provider, which the content manager calls in its nested helpers
            foreach (var method in ContentManagerMethods())
            {
                if (PatchProcessor.GetOriginalInstructions(method).Any(instruction => ProviderCalls.ContainsKey(instruction.operand as MethodInfo ?? DummyMethod)))
                {
                    Patch(harmony, method, transpiler: nameof(ProviderTranspiler));
                }
            }
        }

        private static readonly MethodInfo DummyMethod = typeof(GamePatches).GetMethod(nameof(Apply));

        private static void Patch(Harmony harmony, MethodBase method, string prefix = null, string postfix = null, string transpiler = null)
        {
            if (method == null)
            {
                Timeline.Log.LogWarning($"Could not find a method to hook for {prefix ?? postfix ?? transpiler}; that part is missing from the report.");
                return;
            }
            harmony.Patch(method,
                prefix: prefix != null ? new HarmonyMethod(typeof(GamePatches), prefix) : null,
                postfix: postfix != null ? new HarmonyMethod(typeof(GamePatches), postfix) : null,
                transpiler: transpiler != null ? new HarmonyMethod(typeof(GamePatches), transpiler) : null);
        }

        private static void PatchPhase(Harmony harmony, MethodInfo method, string name)
        {
            if (method != null)
            {
                phaseNames[method] = name;
            }
            Patch(harmony, method, postfix: nameof(PhasePostfix));
        }

        private static void PhasePostfix(MethodBase __originalMethod, ref IEnumerator __result)
        {
            if (__result != null && phaseNames.TryGetValue(__originalMethod, out var name))
            {
                __result = new TimedEnumerator(__result, Timeline.GetTiming(PhaseCategory, name, "RoR2"));
            }
        }

        private static void StartupPhasePostfix(GameInitPhase currPhase, ref IEnumerator __result)
        {
            if (__result != null)
            {
                __result = new TimedEnumerator(__result, Timeline.GetTiming(PhaseCategory, $"Start-up phase {currPhase}", "RoR2"));
            }
        }

        private static void RoR2ApplicationAwakePrefix()
        {
            Timeline.Mark("RoR2Application.Awake: the game starts loading");
        }

        private static void PrintSWPrefix(string message)
        {
            Timeline.Mark($"Game: {message}");
        }

        private static void MainMenuStartPostfix()
        {
            Timeline.Mark("Main menu started");
            Report.Write("the main menu started");
        }

        private static IEnumerable<CodeInstruction> InvokeTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var invoke = AccessTools.Method(typeof(MethodBase), nameof(MethodBase.Invoke), new[] { typeof(object), typeof(object[]) });
            var timedInvoke = AccessTools.Method(typeof(GamePatches), nameof(TimedInvoke));
            foreach (var instruction in instructions)
            {
                if (instruction.Calls(invoke))
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = timedInvoke;
                }
                yield return instruction;
            }
        }

        public static object TimedInvoke(MethodBase method, object target, object[] arguments)
        {
            var type = method.DeclaringType;
            var timing = Timeline.GetTiming(InitializerCategory, $"{type?.FullName}.{method.Name}", type?.Assembly.GetName().Name);
            double start = Timeline.Now;
            long startTicks = Stopwatch.GetTimestamp();
            object result;
            try
            {
                result = method.Invoke(target, arguments);
            }
            finally
            {
                timing.Add(start, Stopwatch.GetTimestamp() - startTicks);
            }
            return TimedEnumerator.Wrap(result, timing);
        }

        // the content manager's methods and those of its helper classes, the compiler's included
        private static IEnumerable<MethodBase> ContentManagerMethods()
        {
            var types = new Stack<Type>();
            types.Push(typeof(ContentManager));
            while (types.Count > 0)
            {
                var type = types.Pop();
                foreach (var nested in type.GetNestedTypes(AccessTools.all))
                {
                    types.Push(nested);
                }
                foreach (var method in type.GetMethods(AccessTools.allDeclared))
                {
                    if (method.GetMethodBody() != null && !method.IsGenericMethodDefinition && !type.IsGenericTypeDefinition)
                    {
                        yield return method;
                    }
                }
            }
        }

        private static Dictionary<MethodInfo, MethodInfo> providerCalls;

        // each of the provider's steps, and the timed call that replaces it
        private static Dictionary<MethodInfo, MethodInfo> ProviderCalls => providerCalls ??= new Dictionary<MethodInfo, MethodInfo>
        {
            [AccessTools.Method(typeof(IContentPackProvider), nameof(IContentPackProvider.LoadStaticContentAsync))] = AccessTools.Method(typeof(GamePatches), nameof(LoadStaticContent)),
            [AccessTools.Method(typeof(IContentPackProvider), nameof(IContentPackProvider.GenerateContentPackAsync))] = AccessTools.Method(typeof(GamePatches), nameof(GenerateContentPack)),
            [AccessTools.Method(typeof(IContentPackProvider), nameof(IContentPackProvider.FinalizeAsync))] = AccessTools.Method(typeof(GamePatches), nameof(FinalizeContentPack)),
        };

        private static IEnumerable<CodeInstruction> ProviderTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                if (instruction.operand is MethodInfo method && ProviderCalls.TryGetValue(method, out var timed))
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = timed;
                }
                yield return instruction;
            }
        }

        public static IEnumerator LoadStaticContent(IContentPackProvider provider, LoadStaticContentAsyncArgs args)
        {
            return TimeProvider(provider, "load static content", () => provider.LoadStaticContentAsync(args));
        }

        public static IEnumerator GenerateContentPack(IContentPackProvider provider, GetContentPackAsyncArgs args)
        {
            return TimeProvider(provider, "generate content pack", () => provider.GenerateContentPackAsync(args));
        }

        public static IEnumerator FinalizeContentPack(IContentPackProvider provider, FinalizeAsyncArgs args)
        {
            return TimeProvider(provider, "finalize", () => provider.FinalizeAsync(args));
        }

        private static IEnumerator TimeProvider(IContentPackProvider provider, string step, Func<IEnumerator> call)
        {
            var timing = Timeline.GetTiming(ProviderCategory, $"{provider?.identifier ?? "(no identifier)"}{Report.StepSeparator}{step}", provider?.GetType().Assembly.GetName().Name);
            double start = Timeline.Now;
            long startTicks = Stopwatch.GetTimestamp();
            IEnumerator result;
            try
            {
                result = call();
            }
            finally
            {
                timing.Add(start, Stopwatch.GetTimestamp() - startTicks);
            }
            return result != null ? new TimedEnumerator(result, timing) : null;
        }
    }
}
