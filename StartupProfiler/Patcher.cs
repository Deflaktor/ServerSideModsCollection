using Mono.Cecil;
using System.Collections.Generic;

namespace StartupProfiler
{
    // A preloader patcher, so that it starts before any plugin. It changes no assembly; BepInEx only calls Initialize
    // before the patchers run, which starts the timing, and Finish after them.
    public static class Patcher
    {
        public static IEnumerable<string> TargetDLLs { get; } = new string[0];

        public static void Patch(AssemblyDefinition assembly)
        {
        }

        public static void Initialize()
        {
            Timeline.Start();
        }

        public static void Finish()
        {
            Timeline.Mark("Patchers finished");
        }
    }
}
