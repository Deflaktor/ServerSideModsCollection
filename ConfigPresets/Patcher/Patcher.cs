using BepInEx.Logging;
using Mono.Cecil;
using System.Collections.Generic;

namespace ConfigPresets
{
    // A preloader patcher, so that it hooks the config files before any plugin binds its settings. It changes no assembly;
    // BepInEx only calls Initialize before the patchers run.
    public static class Patcher
    {
        public const string GUID = "Def.ConfigPresets";
        public const string Name = "ConfigPresets";

        internal static readonly ManualLogSource Log = Logger.CreateLogSource(Name);

        public static IEnumerable<string> TargetDLLs { get; } = new string[0];

        public static void Patch(AssemblyDefinition assembly)
        {
        }

        public static void Initialize()
        {
            Presets.Load();
            ConfigOverride.Apply();
        }
    }
}
