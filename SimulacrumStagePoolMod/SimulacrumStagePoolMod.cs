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
using static SimulacrumStagePoolMod.EnumCollection;


namespace SimulacrumStagePoolMod
{
    [BepInDependency(R2API.R2API.PluginGUID)]
    [BepInDependency("com.rune580.riskofoptions", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.KingEnderBrine.InLobbyConfig", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [R2APISubmoduleDependency()]
    [NetworkCompatibility(CompatibilityLevel.NoNeedForSync, VersionStrictness.DifferentModVersionsAreOk)]
    public class SimulacrumStagePoolMod : BaseUnityPlugin
    {
        public static PluginInfo PInfo { get; private set; }
        public static SimulacrumStagePoolMod instance;

        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Deflaktor";
        public const string PluginName = "SimulacrumStagePoolMod";
        public const string PluginVersion = "1.0.0";

        public void Awake()
        {
            PInfo = Info;
            instance = this;

            Log.Init(Logger);
            BepConfig.Init();

            On.RoR2.ArenaMissionController.OnStartServer += ArenaMissionController_OnStartServer;
            On.RoR2.VoidStageMissionController.OnEnable += VoidStageMissionController_OnEnable;
            On.RoR2.InfiniteTowerRun.OnPrePopulateSceneServer += InfiniteTowerRun_OnPrePopulateSceneServer;

            Logger.LogDebug("Setting up '"+PluginName+"' finished.");
        }

        private void VoidStageMissionController_OnEnable(On.RoR2.VoidStageMissionController.orig_OnEnable orig, VoidStageMissionController self)
        {
            if (Run.instance.GetType() == typeof(InfiniteTowerRun))
            {
                self.enabled = false;
            }
            else
            {
                orig(self);
            }
        }

        private void ArenaMissionController_OnStartServer(On.RoR2.ArenaMissionController.orig_OnStartServer orig, ArenaMissionController self)
        {
            if (Run.instance.GetType() == typeof(InfiniteTowerRun))
            {
                self.enabled = false;
            }
            else
            {
                orig(self);
            }
        }

        private void InfiniteTowerRun_OnPrePopulateSceneServer(On.RoR2.InfiniteTowerRun.orig_OnPrePopulateSceneServer orig, InfiniteTowerRun self, SceneDirector sceneDirector)
        {
            if (self.nextStageScene.cachedName == GetStageName(StageEnum.Bazaar))
            {
                self.PerformStageCleanUp();
            }
            else
            {
                if (!IsSimulacrumStage(self.nextStageScene.cachedName))
                {
                    // consume all spawn points
                    for (int i = 0; i < SpawnPoint.readOnlyInstancesList.Count; i++)
                    {
                        if (!SpawnPoint.readOnlyInstancesList[i].consumed)
                        {
                            SpawnPoint.readOnlyInstancesList[i].consumed = true;
                        }
                    }
                    // no starting monsters
                    sceneDirector.monsterCredit = 0;
                    // disable combat directors
                    var combatDirectors = CombatDirector.instancesList.ToArray();
                    foreach (var combatDirector in combatDirectors)
                    {
                        combatDirector.enabled = false;
                    }
                }
                orig(self, sceneDirector);
            }
        }
    }
}
