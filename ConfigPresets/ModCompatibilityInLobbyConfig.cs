using BepInEx.Configuration;
using System.Runtime.CompilerServices;

namespace ConfigPresets
{
    public static class ModCompatibilityInLobbyConfig
    {
        public const string GUID = "com.KingEnderBrine.InLobbyConfig";

        public static bool enabled => BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(GUID);

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void CreateFromBepInExConfigFile(ConfigFile config, string displayName)
        {
            InLobbyConfig.ModConfigCatalog.Add(InLobbyConfig.Fields.ConfigFieldUtilities.CreateFromBepInExConfigFile(config, displayName));
        }
    }
}
