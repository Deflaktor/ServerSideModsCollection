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
using ServerSideTweaks;
using Newtonsoft.Json.Linq;
using HarmonyLib;

namespace RandomEvents
{
    [BepInDependency(R2API.R2API.PluginGUID)]
    [BepInDependency("com.KingEnderBrine.InLobbyConfig", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("Def.ServerSideTweaks", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [NetworkCompatibility(CompatibilityLevel.NoNeedForSync, VersionStrictness.DifferentModVersionsAreOk)]
    public class RandomEvents : BaseUnityPlugin
    {
        public static PluginInfo PInfo { get; private set; }
        public static RandomEvents instance;

        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Def";
        public const string PluginName = "RandomEvents";
        public const string PluginVersion = "1.0.0";

        public void Awake()
        {
            PInfo = Info;
            instance = this;
            Log.Init(Logger);
            BepConfig.Init();
        }
        private void OnEnable()
        {
            On.RoR2.InfiniteTowerRun.Start                                   += InfiniteTowerRun_Start;
            On.RoR2.InfiniteTowerRun.AdvanceWave                             += InfiniteTowerRun_AdvanceWave;
            On.RoR2.InfiniteTowerRun.RecalculateDifficultyCoefficentInternal += InfiniteTowerRun_RecalculateDifficultyCoefficentInternal;
            On.RoR2.InfiniteTowerBossWaveController.Initialize               += InfiniteTowerBossWaveController_Initialize;
            On.RoR2.InfiniteTowerWaveController.Initialize                   += InfiniteTowerWaveController_Initialize;
        }

        private void OnDisable()
        {
            On.RoR2.InfiniteTowerRun.Start                                   -= InfiniteTowerRun_Start;
            On.RoR2.InfiniteTowerRun.AdvanceWave                             -= InfiniteTowerRun_AdvanceWave;
            On.RoR2.InfiniteTowerRun.RecalculateDifficultyCoefficentInternal -= InfiniteTowerRun_RecalculateDifficultyCoefficentInternal;
            On.RoR2.InfiniteTowerBossWaveController.Initialize               -= InfiniteTowerBossWaveController_Initialize;
            On.RoR2.InfiniteTowerWaveController.Initialize                   -= InfiniteTowerWaveController_Initialize;
        }

        private void InfiniteTowerRun_Start(On.RoR2.InfiniteTowerRun.orig_Start orig, InfiniteTowerRun self)
        {
            orig(self);
            if (ModCompatibilityServerSideTweaks.enabled)
            {
                ModCompatibilityServerSideTweaks.ResetOverridePowerBias();
            }
        }
        
        private void InfiniteTowerBossWaveController_Initialize(On.RoR2.InfiniteTowerBossWaveController.orig_Initialize orig, InfiniteTowerBossWaveController self, int waveIndex, Inventory enemyInventory, GameObject spawnTarget)
        {
            orig(self, waveIndex, enemyInventory, spawnTarget);
            if (BepConfig.Enabled.Value && ModCompatibilityServerSideTweaks.enabled)
            {
                if (RoR2Application.rng.nextNormalizedFloat < 0.1f)
                {
                    if (RoR2Application.rng.nextNormalizedFloat < 0.5f)
                    {
                        ModCompatibilityServerSideTweaks.SetOverridePowerBias(0.05f);
                    }
                    else
                    {
                        ModCompatibilityServerSideTweaks.SetOverridePowerBias(0.95f);
                    }
                }
                else
                {
                    if (ModCompatibilityServerSideTweaks.enabled)
                    {
                        ModCompatibilityServerSideTweaks.ResetOverridePowerBias();
                    }
                }
            }
        }

        private void InfiniteTowerWaveController_Initialize(On.RoR2.InfiniteTowerWaveController.orig_Initialize orig, InfiniteTowerWaveController self, int waveIndex, Inventory enemyInventory, GameObject spawnTarget)
        {
            //if (NetworkServer.active && BepConfig.Enabled.Value)
            //{
            //    if (IsBossStageStarted(waveIndex) && ((InfiniteTowerRun)Run.instance).IsStageTransitionWave() && !bossStageCompleted)
            //    {
            //        self.secondsBeforeSuddenDeath *= 3f;
            //        self.wavePeriodSeconds *= 3f / 2f;
            //    }
            //    else
            //    {
            //        self.wavePeriodSeconds *= 2f / 3f;
            //    }
            //}
            orig(self, waveIndex, enemyInventory, spawnTarget);
        }
        
        private void InfiniteTowerRun_RecalculateDifficultyCoefficentInternal(On.RoR2.InfiniteTowerRun.orig_RecalculateDifficultyCoefficentInternal orig, InfiniteTowerRun self)
        {
            orig(self);
            //if (BepConfig.DifficultyMultiplier1StartWave.Value <= self.waveIndex && self.waveIndex <= BepConfig.DifficultyMultiplier1EndWave.Value)
            //{
            //    self.difficultyCoefficient *= BepConfig.DifficultyMultiplier1.Value;
            //}
        }
        private void InfiniteTowerRun_AdvanceWave(On.RoR2.InfiniteTowerRun.orig_AdvanceWave orig, InfiniteTowerRun self)
        {
            orig(self);
            if (!NetworkServer.active)
                return;
            //if (BepConfig.Artifact1.Value != ArtifactEnum.None)
            //{
            //    if (BepConfig.Artifact1StartWave.Value <= self.waveIndex && self.waveIndex <= BepConfig.Artifact1EndWave.Value)
            //    {
            //        RunArtifactManager.instance.SetArtifactEnabledServer(GetArtifactDef(BepConfig.Artifact1.Value), true);
            //    }
            //    else if (self.waveIndex == BepConfig.Artifact1EndWave.Value + 1)
            //    {
            //        RunArtifactManager.instance.SetArtifactEnabledServer(GetArtifactDef(BepConfig.Artifact1.Value), false);
            //    }
            //}
        }
    }
}
