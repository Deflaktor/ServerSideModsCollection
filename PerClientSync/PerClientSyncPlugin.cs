using BepInEx;
using BepInEx.Logging;

namespace PerClientSync
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class PerClientSyncPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Def";
        public const string PluginName = "PerClientSync";
        public const string PluginVersion = "1.0.0";

        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            Replication.Hook();
        }
    }
}
