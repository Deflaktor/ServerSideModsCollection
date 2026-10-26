using BepInEx;
using System.Linq;

namespace ConfigPresets
{
    // Lets the player pick the presets in-game. The presets themselves are applied by the preloader patcher.
    [BepInDependency(ModCompatibilityInLobbyConfig.GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(ModCompatibilityRiskOfOptions.GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class ConfigPresetsPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Def";
        public const string PluginName = "ConfigPresets";
        public const string PluginVersion = "1.0.0";

        private void Awake()
        {
            if (Presets.Config == null)
            {
                Logger.LogError("The preloader patcher in BepInEx/patchers didn't run, so no preset is applied.");
                return;
            }
            if (!Presets.Choices.Any())
            {
                return;
            }
            if (ModCompatibilityInLobbyConfig.enabled)
            {
                ModCompatibilityInLobbyConfig.CreateFromBepInExConfigFile(Presets.Config, "Config Presets");
            }
            if (ModCompatibilityRiskOfOptions.enabled)
            {
                ModCompatibilityRiskOfOptions.AddChoices(Presets.Choices);
            }
        }
    }
}
