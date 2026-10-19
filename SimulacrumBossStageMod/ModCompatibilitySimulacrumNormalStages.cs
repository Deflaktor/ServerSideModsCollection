using System;
using System.Runtime.CompilerServices;

namespace SimulacrumBossStageMod
{
    public static class ModCompatibilitySimulacrumNormalStages
    {
        private static bool? _enabled;

        public static bool enabled
        {
            get
            {
                if (_enabled == null)
                {
                    _enabled = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(SimulacrumNormalStages.SimulacrumNormalStages.PluginGUID);
                }
                return (bool)_enabled;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void SetSkipHallowedShrine(Func<bool> skip)
        {
            SimulacrumNormalStages.SimulacrumNormalStages.skipHallowedShrine = skip;
        }
    }
}
