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
using static SimulacrumNormalStagesFix.EnumCollection;
using System.Security.Cryptography;
using System.Collections.Generic;
using static RoR2.SceneCollection;

namespace SimulacrumNormalStagesFix
{
    [BepInDependency(R2API.R2API.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [NetworkCompatibility(CompatibilityLevel.NoNeedForSync, VersionStrictness.DifferentModVersionsAreOk)]
    public class SimulacrumNormalStagesFix : BaseUnityPlugin
    {
        public static PluginInfo PInfo { get; private set; }
        public static SimulacrumNormalStagesFix instance;

        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Deflaktor";
        public const string PluginName = "SimulacrumNormalStagesFix";
        public const string PluginVersion = "1.0.0";

        public void Awake()
        {
            PInfo = Info;
            instance = this;
            Log.Init(Logger);
        }
        private void OnEnable()
        {
            On.RoR2.ArenaMissionController.OnStartServer      += ArenaMissionController_OnStartServer;
            On.RoR2.VoidStageMissionController.OnEnable       += VoidStageMissionController_OnEnable;
            On.RoR2.InfiniteTowerRun.OnPrePopulateSceneServer += InfiniteTowerRun_OnPrePopulateSceneServer;
        }
        private void OnDisable()
        {
            On.RoR2.ArenaMissionController.OnStartServer      -= ArenaMissionController_OnStartServer;
            On.RoR2.VoidStageMissionController.OnEnable       -= VoidStageMissionController_OnEnable;
            On.RoR2.InfiniteTowerRun.OnPrePopulateSceneServer -= InfiniteTowerRun_OnPrePopulateSceneServer;
        }
        /*
    #if DEBUG
            private void Run_FixedUpdate(On.RoR2.Run.orig_FixedUpdate orig, Run self)
            {
                orig(self);
                if (Input.GetKeyDown(KeyCode.F2))
                {
                    self.PickNextStageSceneFromCurrentSceneDestinations();
                }
            }
    #endif

            private void Run_PickNextStageSceneFromCurrentSceneDestinations(On.RoR2.Run.orig_PickNextStageSceneFromCurrentSceneDestinations orig, Run self)
            {
                if (Run.instance.GetType() == typeof(InfiniteTowerRun) && BepConfig.UseNormalStages.Value)
                {
                    WeightedSelection<SceneDef> weightedStagesCollection = new WeightedSelection<SceneDef>();

                    if (normalStagesCollection == null) normalStagesCollection = GetNormalStagesList();
                    foreach (var stageEntry in normalStagesCollection) weightedStagesCollection.AddChoice(stageEntry.sceneDef, stageEntry.weight);

                    self.nextStageScene = weightedStagesCollection.Evaluate(self.nextStageRng.nextNormalizedFloat);
    #if DEBUG
                    Log.LogDebug("Next Stage up: '" + self.nextStageScene.cachedName + "'");
    #endif
                }
                else
                {
                    orig(self);
                }
            }*/

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
