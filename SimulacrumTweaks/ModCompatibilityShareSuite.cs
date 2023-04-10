using BepInEx.Configuration;
using RoR2;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace SimulacrumTweaks
{
    public static class ModCompatibilityShareSuite
    {
        private static bool? _enabled;

        public static bool enabled
        {
            get
            {
                if (_enabled == null)
                {
                    _enabled = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.funkfrog_sipondo.sharesuite");
                }
                return (bool)_enabled;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void AddPickupEventHandler(Func<GenericPickupController, CharacterBody, bool> f)
        {
            ShareSuite.ItemSharingHooks.AdditionalPickupValidityChecks += f;
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void RemovePickupEventHandler(Func<GenericPickupController, CharacterBody, bool> f)
        {
            ShareSuite.ItemSharingHooks.AdditionalPickupValidityChecks -= f;
        }
    }
}
