using BepInEx.Configuration;
using RoR2;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace ServerSideTweaks
{
    public static class ModCompatibilityServerSideTweaks
    {
        private static bool? _enabled;

        public static bool enabled
        {
            get
            {
                if (_enabled == null)
                {
                    _enabled = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("Def.ServerSideTweaks");
                }
                return (bool)_enabled;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void SetOverridePowerBias(float powerBias)
        {
            ServerSideTweaks.SetOverridePowerBias(powerBias);
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static float GetCurrentPowerBias()
        {
            return ServerSideTweaks.GetCurrentPowerBias();
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void ResetOverridePowerBias()
        {
            ServerSideTweaks.ResetOverridePowerBias();
        }
    }
}
