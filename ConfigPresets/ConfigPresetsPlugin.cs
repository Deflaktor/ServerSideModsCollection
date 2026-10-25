using BepInEx;

namespace ConfigPresets
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class ConfigPresetsPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Def";
        public const string PluginName = "ConfigPresets";
        public const string PluginVersion = "1.0.0";

        private void Awake()
        {
            // the presets are read when a mod binds its first setting, which may be before this
            Presets.Load();
        }
    }
}
