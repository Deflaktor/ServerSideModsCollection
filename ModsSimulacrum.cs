using BepInEx;
using EntityStates.Missions.LunarScavengerEncounter;
using IL.RoR2.Modding;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API.Utils;
using RoR2;
using RoR2.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using static ServerSideModsCollection.EnumCollection;

namespace ServerSideModsCollection
{
    public class ModsSimulacrum
    {
        public static SpawnCard iscShopPortal;
        public static SpawnCard iscVoidPortal;
        public static float bonusTimerStart = 0f;
        public static int bonusCounter = 0;

        public static void Init()
        {
            //main setup hook; this is where most of the mod's settings are applied
            On.RoR2.Run.Start += Run_Start;
            On.RoR2.InfiniteTowerRun.OnPrePopulateSceneServer += InfiniteTowerRun_OnPrePopulateSceneServer;
            On.RoR2.InfiniteTowerRun.OnWaveAllEnemiesDefeatedServer += InfiniteTowerRun_OnWaveAllEnemiesDefeatedServer;
            On.RoR2.InfiniteTowerRun.AdvanceWave += InfiniteTowerRun_AdvanceWave;
            On.RoR2.ArenaMissionController.OnStartServer += ArenaMissionController_OnStartServer;
            On.RoR2.Run.OnFixedUpdate += Run_OnFixedUpdate;
            // On.RoR2.InfiniteTowerRun.IsStageTransitionWave += InfiniteTowerRun_IsStageTransitionWave;
            //On.RoR2.ArenaMissionController.OnEnable += ArenaMissionController_OnEnable;
            On.RoR2.VoidStageMissionController.OnEnable += VoidStageMissionController_OnEnable;
            On.RoR2.InfiniteTowerRun.RecalculateDifficultyCoefficentInternal += InfiniteTowerRun_RecalculateDifficultyCoefficentInternal;
            IL.RoR2.InfiniteTowerRun.OnWaveAllEnemiesDefeatedServer += il =>
            {
                ILCursor c = new ILCursor(il);
                c.GotoNext(
                    x => x.MatchCall<DirectorCore>("get_instance"),
                    x => x.MatchLdarg(0),
                    x => x.MatchLdfld<InfiniteTowerRun>("stageTransitionPortalCard"),
                    x => x.MatchNewobj<DirectorPlacementRule>()
                    );
                c.Index += 2;
                c.Remove();
                c.EmitDelegate<Func<InfiniteTowerRun, SpawnCard>>((self) =>
                {
                    if (self.waveIndex >= BepConfig.FinalStageStartWave.Value && BepConfig.FinalStage.Value != StageEnum.None)
                    {
                        if(self.waveIndex >= BepConfig.FinalStageStartWave.Value + 1 && self.IsStageTransitionWave() && SceneCatalog.currentSceneDef.cachedName == GetStageName(BepConfig.FinalStage.Value))
                        {
                            return iscShopPortal;
                        } else
                        {
                            return iscVoidPortal;
                        }
                        
                    }
                    else
                    {
                        return self.stageTransitionPortalCard;
                    }
                });
            };

            //Coin base drop chance
            //On.RoR2.PlayerCharacterMasterController.Awake += PlayerCharacterMasterController_Awake;
            //On.RoR2.InfiniteTowerRun.OnPrePopulateSceneServer += InfiniteTowerRun_OnPrePopulateSceneServer;
            //On.RoR2.ArenaMissionController.OnStartServer += ArenaMissionController_OnStartServer;
            //On.RoR2.ArenaMissionController.OnEnable += ArenaMissionController_OnEnable;
            // On.RoR2.ArenaMissionController.Awake 
            //On.RoR2.VoidStageMissionController.RequestFog += VoidStageMissionController_RequestFog;
            //On.RoR2.FogDamageController.Start += FogDamageController_Start;
            /*
            On.RoR2.FogDamageController.
            On.RoR2.VoidStageMissionController.RequestFog += VoidStageMissionController_RequestFog;
            On.RoR2.VoidStageMissionController.OnEnable */

            //Coin drop multiplier
            /*
            BindingFlags allFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            var initDelegate = typeof(PlayerCharacterMasterController).GetNestedTypes(allFlags)[0].GetMethodCached(name: "<Init>b__72_0");
            MonoMod.RuntimeDetour.HookGen.HookEndpointManager.Modify(initDelegate, CoinDropHook);


            IL.RoR2.InfiniteTowerRun.RecalculateDifficultyCoefficentInternal += il =>
            {

                ILCursor c = new ILCursor(il);
                c.GotoNext(
                    x => x.MatchLdarg(0),
                    x => x.MatchCallvirt<DifficultyIndex>("get_selectedDifficulty"),
                    x => x.MatchStloc(0),
                    x => x.MatchLdcR4(1.5f)
                    );
                c.Index += 4;
                c.Next.Operand = 0.8f;
            };*/



        }

        private static void InfiniteTowerRun_AdvanceWave(On.RoR2.InfiniteTowerRun.orig_AdvanceWave orig, InfiniteTowerRun self)
        {
            orig(self);
            if (BepConfig.Artifact1.Value != ArtifactEnum.None) {
                if (BepConfig.Artifact1StartWave.Value <= self.waveIndex && self.waveIndex <= BepConfig.Artifact1EndWave.Value)
                {
                    RunArtifactManager.instance.SetArtifactEnabledServer(GetArtifactDef(BepConfig.Artifact1.Value), true);
                } else if(self.waveIndex == BepConfig.Artifact1EndWave.Value + 1)
                {
                    RunArtifactManager.instance.SetArtifactEnabledServer(GetArtifactDef(BepConfig.Artifact1.Value), false);
                }
            }
            if (BepConfig.Artifact2.Value != ArtifactEnum.None)
            {
                if (BepConfig.Artifact2StartWave.Value <= self.waveIndex && self.waveIndex <= BepConfig.Artifact2EndWave.Value)
                {
                    RunArtifactManager.instance.SetArtifactEnabledServer(GetArtifactDef(BepConfig.Artifact2.Value), true);
                }
                else if (self.waveIndex == BepConfig.Artifact2EndWave.Value + 1)
                {
                    RunArtifactManager.instance.SetArtifactEnabledServer(GetArtifactDef(BepConfig.Artifact2.Value), false);
                }
            }
            if (BepConfig.Artifact3.Value != ArtifactEnum.None)
            {
                if (BepConfig.Artifact3StartWave.Value <= self.waveIndex && self.waveIndex <= BepConfig.Artifact3EndWave.Value)
                {
                    RunArtifactManager.instance.SetArtifactEnabledServer(GetArtifactDef(BepConfig.Artifact3.Value), true);
                }
                else if (self.waveIndex == BepConfig.Artifact3EndWave.Value + 1)
                {
                    RunArtifactManager.instance.SetArtifactEnabledServer(GetArtifactDef(BepConfig.Artifact3.Value), false);
                }
            }
            if (BepConfig.Artifact4.Value != ArtifactEnum.None)
            {
                if (BepConfig.Artifact4StartWave.Value <= self.waveIndex && self.waveIndex <= BepConfig.Artifact4EndWave.Value)
                {
                    RunArtifactManager.instance.SetArtifactEnabledServer(GetArtifactDef(BepConfig.Artifact4.Value), true);
                }
                else if (self.waveIndex == BepConfig.Artifact4EndWave.Value + 1)
                {
                    RunArtifactManager.instance.SetArtifactEnabledServer(GetArtifactDef(BepConfig.Artifact4.Value), false);
                }
            }
        }



        private static void Run_OnFixedUpdate(On.RoR2.Run.orig_OnFixedUpdate orig, Run self)
        {
            orig(self);
            if (Run.instance.GetType() == typeof(InfiniteTowerRun))
            {
                if (Input.GetKeyDown(KeyCode.F2))
                {
                    ((InfiniteTowerRun)Run.instance).AdvanceWave();
                    var transform = PlayerCharacterMasterController.instances[0].master.GetBodyObject().transform;
                    Log.LogDebug($"Player pressed F2. Spawning our custom item at coordinates {transform.position}");
                    PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex("LunarCoin.Coin0"), transform.position, transform.forward * 20f);
                }
                if(bonusTimerStart > 0f)
                {

                }
            }
        }

        private static void InfiniteTowerRun_RecalculateDifficultyCoefficentInternal(On.RoR2.InfiniteTowerRun.orig_RecalculateDifficultyCoefficentInternal orig, InfiniteTowerRun self)
        {
            orig(self);
            if(BepConfig.DifficultyMultiplier1StartWave.Value <= self.waveIndex && self.waveIndex <= BepConfig.DifficultyMultiplier1EndWave.Value) 
            {
                self.difficultyCoefficient *= BepConfig.DifficultyMultiplier1.Value;
            }
            if (BepConfig.DifficultyMultiplier2StartWave.Value <= self.waveIndex && self.waveIndex <= BepConfig.DifficultyMultiplier2EndWave.Value)
            {
                self.difficultyCoefficient *= BepConfig.DifficultyMultiplier2.Value;
            }
            if (BepConfig.DifficultyMultiplier3StartWave.Value <= self.waveIndex && self.waveIndex <= BepConfig.DifficultyMultiplier3EndWave.Value)
            {
                self.difficultyCoefficient *= BepConfig.DifficultyMultiplier3.Value;
            }
            if (BepConfig.DifficultyMultiplier4StartWave.Value <= self.waveIndex && self.waveIndex <= BepConfig.DifficultyMultiplier4EndWave.Value)
            {
                self.difficultyCoefficient *= BepConfig.DifficultyMultiplier4.Value;
            }
            self.compensatedDifficultyCoefficient = self.difficultyCoefficient;
        }

        private static bool InfiniteTowerRun_IsStageTransitionWave(On.RoR2.InfiniteTowerRun.orig_IsStageTransitionWave orig, InfiniteTowerRun self)
        {
            return true;
        }

        private static void InfiniteTowerRun_OnWaveAllEnemiesDefeatedServer(On.RoR2.InfiniteTowerRun.orig_OnWaveAllEnemiesDefeatedServer orig, InfiniteTowerRun self, InfiniteTowerWaveController wc)
        {
            orig(self, wc);
            if (self.isGameOverServer)
            {
                return;
            }
            if (self.waveIndex >= BepConfig.FinalStageStartWave.Value && BepConfig.FinalStage.Value != StageEnum.None) 
            {
                self.nextStageScene = SceneCatalog.allStageSceneDefs.Where(s => s.cachedName == GetStageName(BepConfig.FinalStage.Value)).First();

                if (self.waveIndex >= BepConfig.FinalStageStartWave.Value + 1 && self.IsStageTransitionWave() && SceneCatalog.currentSceneDef.cachedName == GetStageName(BepConfig.FinalStage.Value))
                {
                    bonusTimerStart = Run.instance.GetRunStopwatch();
                    /*if ((bool)self.fogDamageController)
                    {
                        UnityEngine.Object.Destroy(self.fogDamageController.gameObject);
                        self.fogDamageController = null;
                    }

                    //var def = GetArtifactDefFromString("test");
                    //RunArtifactManager.instance.SetArtifactEnabledServer(def, true);
                    // self.state.SetNextState(new FadeOut());
                    Run.instance.BeginGameOver(RoR2Content.GameEndings.LimboEnding);*/
                }
            }
        }

        private static void VoidStageMissionController_OnEnable(On.RoR2.VoidStageMissionController.orig_OnEnable orig, VoidStageMissionController self)
        {
            if (Run.instance.GetType() == typeof(InfiniteTowerRun) && BepConfig.NoMissionsInVoidLocus.Value)
            {
                Log.LogDebug("Skipping VoidStageMissionController_OnEnable because Type is InfiniteTowerRun");
                self.enabled = false;
            }
            else
            {
                orig(self);
            }
        }

        private static void ArenaMissionController_OnStartServer(On.RoR2.ArenaMissionController.orig_OnStartServer orig, ArenaMissionController self)
        {
            if (Run.instance.GetType() == typeof(InfiniteTowerRun) && BepConfig.NoMissionsInVoidFields.Value)
            {
                Log.LogDebug("Skipping ArenaMissionController_OnStartServer because Type is InfiniteTowerRun");
                self.enabled = false;
            }
            else
            {
                orig(self);
            }
        }

        private static void InfiniteTowerRun_OnPrePopulateSceneServer(On.RoR2.InfiniteTowerRun.orig_OnPrePopulateSceneServer orig, InfiniteTowerRun self, SceneDirector sceneDirector)
        {
            if (self.nextStageScene.cachedName == "bazaar" && BepConfig.FixSimulacrumBazaar.Value)
            {
                Log.LogDebug("PerformStageCleanUp()");
                self.InvokeMethod("PerformStageCleanUp");
                // self.PerformStageCleanUp();
            }
            else
            {
                if (!IsSimulacrumStage(self.nextStageScene.cachedName) && BepConfig.FixSimulacrumSpawnPointsInNormalStages.Value)
                {
                    // consume all spawn points
                    for (int i = 0; i < SpawnPoint.readOnlyInstancesList.Count; i++)
                    {
                        if (!SpawnPoint.readOnlyInstancesList[i].consumed)
                        {
                            SpawnPoint.readOnlyInstancesList[i].consumed = true;
                        }
                    }
                }
                orig(self, sceneDirector);
                
                // if stage is voidstage, fix spawn points
                /*
                if (self.nextStageScene.cachedName == "voidstage")
                {
                    // consume all spawn points
                    for (int i = 0; i < SpawnPoint.readOnlyInstancesList.Count; i++)
                    {
                        if (!SpawnPoint.readOnlyInstancesList[i].consumed)
                        {
                            SpawnPoint.readOnlyInstancesList[i].consumed = true;
                        }
                    }
                    // add new spawn points near the safe ward
                    Vector3 position = self.safeWardController.transform.position;
                    for (int i=0;i<16;i++)
                    {
                        float radius = self.spawnMaxRadius / 2;
                        float angle = (float)(i * 2 * Math.PI / 16);
                        float offset_x = (float) Math.Cos(angle) * radius;
                        float offset_z = (float) Math.Sin(angle) * radius;
                        SpawnPoint.AddSpawnPoint(new Vector3(position.x + offset_x, position.y + 2f, position.z + offset_z), Quaternion.LookRotation(position, Vector3.up));
                    }
                }*/
            }
            iscVoidPortal = LegacyResourcesAPI.Load<SpawnCard>("SpawnCards/InteractableSpawnCard/iscVoidPortal");
            List<String> allPaths = new List<String>();
            LegacyResourcesAPI.GetAllPaths(allPaths);
            foreach(String path in allPaths)
            {
                Log.LogDebug(path);
            }
            iscShopPortal = LegacyResourcesAPI.Load<SpawnCard>("SpawnCards/InteractableSpawnCard/iscShopPortal");
            
        }

        private static void Run_Start(On.RoR2.Run.orig_Start orig, Run self)
        {
            orig(self);
            Chat.SendBroadcastChat(new Chat.SimpleChatMessage
            {
                baseToken = "ServerSideModsCollection started."
            });
        }
    }
}
