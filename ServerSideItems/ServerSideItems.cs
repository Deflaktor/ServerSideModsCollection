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
using System.Linq;
using UnityEngine.ResourceManagement.AsyncOperations;
using Newtonsoft.Json.Linq;
using HarmonyLib;
using System.Collections;

namespace ServerSideItems
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(R2API.R2API.PluginGUID)]
    [BepInDependency(R2API.LanguageAPI.PluginGUID)]
    [BepInDependency("com.KingEnderBrine.InLobbyConfig", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("droppod.lookingglass", BepInDependency.DependencyFlags.SoftDependency)]
    [NetworkCompatibility(CompatibilityLevel.NoNeedForSync, VersionStrictness.DifferentModVersionsAreOk)]
    public class ServerSideItems : BaseUnityPlugin
    {
        public static PluginInfo PInfo { get; private set; }
        public static ServerSideItems instance;

        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Def";
        public const string PluginName = "ServerSideItems";
        public const string PluginVersion = "1.1.0";

        private readonly BeyondTheLimits beyondTheLimits = new BeyondTheLimits();
        private readonly NewlyHatchedZoea newlyHatchedZoea = new NewlyHatchedZoea();
        private readonly ShatterSpleenTweak shatterSpleenTweak = new ShatterSpleenTweak();
        public bool canUpdateItemDescriptions = false;
        public Action newlyHatchedZoeaUpdate = null;

        public void Awake()
        {
            PInfo = Info;
            instance = this;
            Log.Init(Logger);
            BepConfig.Init();
            beyondTheLimits.Init();
            newlyHatchedZoea.Init();
            shatterSpleenTweak.Init();

            ItemCatalog.availability.CallWhenAvailable(() =>
            {
                StartCoroutine(DelayUpdatingItemDescriptions());
            });
        }

        IEnumerator DelayUpdatingItemDescriptions()
        {
            // delay to make sure this is loaded after LookingGlass
            yield return new WaitForSeconds(1.0f);
            canUpdateItemDescriptions = true;
            if(newlyHatchedZoeaUpdate != null)
            {
                newlyHatchedZoeaUpdate.Invoke();
            }
        }

        private void OnEnable()
        {
            //beyondTheLimits.Hook();
            newlyHatchedZoea.Hook();
            shatterSpleenTweak.Hook();
        }

        private void OnDisable()
        {
            //beyondTheLimits.Unhook();
            newlyHatchedZoea.Unhook();
            shatterSpleenTweak.Unhook();
        }

    }
}
