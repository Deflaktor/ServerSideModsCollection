using BepInEx;
using R2API;
using R2API.Utils;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using MonoMod.Cil;
using System;
using System.Reflection;
using UnityEngine.Networking;

namespace ServerSideModsCollection
{
    [BepInDependency(R2API.R2API.PluginGUID)]
    [BepInDependency("com.rune580.riskofoptions", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.KingEnderBrine.InLobbyConfig", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [R2APISubmoduleDependency()]
    [NetworkCompatibility(CompatibilityLevel.NoNeedForSync, VersionStrictness.DifferentModVersionsAreOk)]
    public class ServerSideModsCollection : BaseUnityPlugin
    {
        public static PluginInfo PInfo { get; private set; }
        public static ServerSideModsCollection instance;

        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Deflaktor";
        public const string PluginName = "ServerSideModsCollection";
        public const string PluginVersion = "1.0.0";

        public void Awake()
        {
            PInfo = Info;
            instance = this;

            Log.Init(Logger);
            BepConfig.Init();
            ModsSimulacrum.Init();
            ModsStartBonus.Init();
            
            Logger.LogDebug("Setting up 'Server-side Mods Collection' finished.");
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F2))
            {
                var transform = PlayerCharacterMasterController.instances[0].master.GetBodyObject().transform;
                Log.LogInfo($"Player pressed F2. Spawning our custom item at coordinates {transform.position}");
                PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex("LunarCoin.Coin0"), transform.position, transform.forward * 20f);
            }
        }
    }
}
