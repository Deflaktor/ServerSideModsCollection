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
    [BepInDependency(R2API.R2API.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
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
        public const string PluginVersion = "1.0.0";

        private readonly BeyondTheLimits beyondTheLimits = new BeyondTheLimits();
        private readonly NewlyHatchedZoea newlyHatchedZoea = new NewlyHatchedZoea();
        private readonly ShatterSpleenTweak shatterSpleenTweak = new ShatterSpleenTweak();

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
            yield return new WaitForSeconds(1.0f);
            var itemIndex = DLC1Content.Items.VoidMegaCrabItem.itemIndex;
            var itemDef = ItemCatalog.GetItemDef(DLC1Content.Items.VoidMegaCrabItem.itemIndex);
            itemDef.tags = itemDef.tags.Where(s => s != ItemTag.CannotCopy).ToArray();
            if (ModCompatibilityLookingGlass.enabled)
            {
                ModCompatibilityLookingGlass.UpdateNewlyHatchedZoeaDescription();
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
