using BepInEx;
using EntityStates.Missions.LunarScavengerEncounter;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API.Utils;
using RoR2;
using RoR2.Navigation;
using System;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace ServerSideModsCollection
{
    public class ModsSimulacrum
    {
        public static void Init()
        {
            //main setup hook; this is where most of the mod's settings are applied
            On.RoR2.Run.Start += Run_Start;
            On.RoR2.InfiniteTowerRun.OnPrePopulateSceneServer += InfiniteTowerRun_OnPrePopulateSceneServer;
            On.RoR2.InfiniteTowerRun.OnWaveAllEnemiesDefeatedServer += InfiniteTowerRun_OnWaveAllEnemiesDefeatedServer;
            On.RoR2.ArenaMissionController.OnStartServer += ArenaMissionController_OnStartServer;
            On.RoR2.Run.OnFixedUpdate += Run_OnFixedUpdate;
            // On.RoR2.InfiniteTowerRun.IsStageTransitionWave += InfiniteTowerRun_IsStageTransitionWave;
            //On.RoR2.ArenaMissionController.OnEnable += ArenaMissionController_OnEnable;
            On.RoR2.VoidStageMissionController.OnEnable += VoidStageMissionController_OnEnable;
            On.RoR2.InfiniteTowerRun.RecalculateDifficultyCoefficentInternal += InfiniteTowerRun_RecalculateDifficultyCoefficentInternal;

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

        private static string GetArgNameForAtrifact(ArtifactDef artifactDef)
        {
            return Regex.Replace(Language.GetString(artifactDef.nameToken), "[ '-]", String.Empty);
        }

        private static ArtifactDef GetArtifactDefFromString(string partialName)
        {
            foreach (var artifact in ArtifactCatalog.artifactDefs)
            {
                Log.LogDebug(artifact.nameToken);
            }
            return null;
        }

        private static void ToggleArtifact(ConCommandArgs args, bool? newState = null)
        {
            if (!RunArtifactManager.instance)
            {
                return;
            }
            var def = GetArtifactDefFromString(args[0]);
            if (!def)
            {
                Log.LogDebug("Artifact with a given name was not found.");
                return;
            }
            RunArtifactManager.instance.SetArtifactEnabledServer(def, newState ?? !RunArtifactManager.instance.IsArtifactEnabled(def));
        }

        private static void Run_OnFixedUpdate(On.RoR2.Run.orig_OnFixedUpdate orig, Run self)
        {
            if (Run.instance.GetType() == typeof(InfiniteTowerRun))
            {
                if (Input.GetKeyDown(KeyCode.F2))
                {
                    ((InfiniteTowerRun)InfiniteTowerRun.instance).AdvanceWave();
                    var transform = PlayerCharacterMasterController.instances[0].master.GetBodyObject().transform;
                    Log.LogDebug($"Player pressed F2. Spawning our custom item at coordinates {transform.position}");
                    PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex("LunarCoin.Coin0"), transform.position, transform.forward * 20f);
                }
            }
        }

        private static void InfiniteTowerRun_RecalculateDifficultyCoefficentInternal(On.RoR2.InfiniteTowerRun.orig_RecalculateDifficultyCoefficentInternal orig, InfiniteTowerRun self)
        {
            orig(self);
            if(SceneCatalog.GetSceneDefForCurrentScene().cachedName == "voidstage")
            {
                self.difficultyCoefficient *= 1.5f;
                self.compensatedDifficultyCoefficient = self.difficultyCoefficient;
            }
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
            if (self.waveIndex >= 50) 
            {
                self.nextStageScene = SceneCatalog.allStageSceneDefs.Where(s => s.cachedName == "voidstage").First();
            }
            if (self.waveIndex >= 55)
            {
                if ((bool)self.fogDamageController)
                {
                    UnityEngine.Object.Destroy(self.fogDamageController.gameObject);
                    self.fogDamageController = null;
                }

                var def = GetArtifactDefFromString("test");
                RunArtifactManager.instance.SetArtifactEnabledServer(def, true);
                // self.state.SetNextState(new FadeOut());
                Run.instance.BeginGameOver(RoR2Content.GameEndings.LimboEnding);
            }
        }

        private static void VoidStageMissionController_OnEnable(On.RoR2.VoidStageMissionController.orig_OnEnable orig, VoidStageMissionController self)
        {
            if (Run.instance.GetType() != typeof(InfiniteTowerRun))
            {
                orig(self);
            }
            else
            {
                Log.LogDebug("Skipping VoidStageMissionController_OnEnable because Type is InfiniteTowerRun");
                self.enabled = false;
            }
        }

        private static void ArenaMissionController_OnEnable(On.RoR2.ArenaMissionController.orig_OnEnable orig, ArenaMissionController self)
        {
            if (Run.instance.GetType() != typeof(InfiniteTowerRun))
            {
                orig(self);
            }
            else
            {
                Log.LogDebug("Skipping Arena OnEnable because Type is InfiniteTowerRun");
                self.enabled = false;
            }
        }

        private static void ArenaMissionController_OnStartServer(On.RoR2.ArenaMissionController.orig_OnStartServer orig, ArenaMissionController self)
        {
            if (Run.instance.GetType() != typeof(InfiniteTowerRun))
            {
                orig(self);
            } else
            {
                Log.LogDebug("Skipping Arena Setup because Type is InfiniteTowerRun");
                self.enabled = false;
            }
        }

        private static void InfiniteTowerRun_OnPrePopulateSceneServer(On.RoR2.InfiniteTowerRun.orig_OnPrePopulateSceneServer orig, InfiniteTowerRun self, SceneDirector sceneDirector)
        {
            if (self.nextStageScene.cachedName != "bazaar")
            {
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
            else
            {
                Log.LogDebug("PerformStageCleanUp()");
                self.InvokeMethod("PerformStageCleanUp");
                // self.PerformStageCleanUp();
            }
        }

        private static void Run_Start(On.RoR2.Run.orig_Start orig, Run self)
        {
            orig(self);
            GetArtifactDefFromString("g");
            Chat.SendBroadcastChat(new Chat.SimpleChatMessage
            {
                baseToken = "ServerSideModsCollection started."
            });
        }
    }
}
