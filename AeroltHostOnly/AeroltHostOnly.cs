using BepInEx;
using R2API.Utils;

namespace AeroltHostOnly
{
    [BepInDependency(R2API.R2API.PluginGUID)]
    [BepInDependency("com.Lodington.Aerolt", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [NetworkCompatibility(CompatibilityLevel.NoNeedForSync, VersionStrictness.DifferentModVersionsAreOk)]
    public class AeroltHostOnly : BaseUnityPlugin
    {
        public static PluginInfo PInfo { get; private set; }

        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Def";
        public const string PluginName = "AeroltHostOnly";
        public const string PluginVersion = "1.0.0";

        public void Awake()
        {
            PInfo = Info;
        }
        private void OnEnable()
        {
            On.RoR2.Networking.NetworkManagerSystem.OnStartServer += NetworkManagerSystem_OnStartServer;
        }
        private void OnDisable()
        {
            On.RoR2.Networking.NetworkManagerSystem.OnStartServer -= NetworkManagerSystem_OnStartServer;
        }
        private void NetworkManagerSystem_OnStartServer(On.RoR2.Networking.NetworkManagerSystem.orig_OnStartServer orig, RoR2.Networking.NetworkManagerSystem self)
        {
            orig(self);
            UnityEngine.Networking.NetworkServer.UnregisterHandler(2004); // remove AeroIt Handler
        }
    }
}
