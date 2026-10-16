using BepInEx;
using HarmonyLib;
using MonoMod.Cil;
using Newtonsoft.Json.Linq;
using R2API;
using R2API.Utils;
using RoR2;
using ServerSideTweaks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;
using static SimulacrumBossStageMod.EnumCollection;

namespace SimulacrumBossStageMod
{
    [BepInDependency(R2API.R2API.PluginGUID)]
    [BepInDependency("com.KingEnderBrine.InLobbyConfig", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("Def.ServerSideTweaks", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("Def.SimulacrumNormalStages", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.KingEnderBrine.ProperSave", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [NetworkCompatibility(CompatibilityLevel.NoNeedForSync, VersionStrictness.DifferentModVersionsAreOk)]
    public class SimulacrumBossStageMod : BaseUnityPlugin
    {
        public static PluginInfo PInfo { get; private set; }
        public static SimulacrumBossStageMod instance;

        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Def";
        public const string PluginName = "SimulacrumBossStageMod";
        public const string PluginVersion = "1.3.2";

        public AsyncOperationHandle<SpawnCard> iscVoidPortal;
        public AsyncOperationHandle<SpawnCard> iscVoidOutroPortal;
        public AsyncOperationHandle<CharacterSpawnCard> bossSpawnCard;
        public AsyncOperationHandle<EliteDef> edBead;
        public float nextBonusTime;
        public int bonusCounter;
        public bool bossStageCompleted;
        public List<GameObject> bossSpawned = new List<GameObject>();
        public SceneDef nextStageBeforeBoss;
        public float zoneRadiusBoss = 1.0f;
        public static CombatDirector.EliteTierDef poisonEliteTier;
        private readonly HashSet<ArtifactDef> artifactsEnabledByMod = new HashSet<ArtifactDef>();

        public BossEnum debug_enemyTypeToSpawn = BossEnum.None;
        public EliteEnum debug_eliteTypeToSpawn = EliteEnum.None;

        public void Awake()
        {
            PInfo = Info;
            instance = this;
            Log.Init(Logger);
            BepConfig.Init();

            iscVoidPortal = Addressables.LoadAssetAsync<SpawnCard>("RoR2/DLC1/PortalVoid/iscVoidPortal.asset");
            iscVoidOutroPortal = Addressables.LoadAssetAsync<SpawnCard>("RoR2/DLC1/VoidOutroPortal/iscVoidOutroPortal.asset");
            edBead = Addressables.LoadAssetAsync<EliteDef>(key: "RoR2/DLC2/Elites/EliteBead/edBead.asset");
            RegisterSaveData();
            // bossSpawnCard = Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/DLC1/VoidMegaCrab/cscVoidMegaCrab.asset");
            // bossSpawnCard = Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/DLC1/VoidRaidCrab/cscMiniVoidRaidCrabBase.asset");
            // bossSpawnCard = Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/DLC1/VoidRaidCrab/cscMiniVoidRaidCrabPhase1.asset");
            // bossSpawnCard = Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/DLC1/VoidRaidCrab/cscMiniVoidRaidCrabPhase2.asset");

            // bossSpawnCard = Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/DLC1/VoidRaidCrab/cscVoidRaidCrab.asset");
            // bossSpawnCard = Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/DLC1/VoidRaidCrab/cscVoidRaidCrabJoint.asset");
        }
        private void OnEnable()
        {
            On.RoR2.InfiniteTowerRun.Start                                   += InfiniteTowerRun_Start;
            On.RoR2.Run.BuildDropTable                                       += Run_BuildDropTable;
            On.RoR2.InfiniteTowerRun.OnWaveAllEnemiesDefeatedServer          += InfiniteTowerRun_OnWaveAllEnemiesDefeatedServer;
            On.RoR2.InfiniteTowerRun.AdvanceWave                             += InfiniteTowerRun_AdvanceWave;
            On.RoR2.InfiniteTowerRun.FixedUpdate                             += InfiniteTowerRun_FixedUpdate;
            On.RoR2.InfiniteTowerRun.RecalculateDifficultyCoefficentInternal += InfiniteTowerRun_RecalculateDifficultyCoefficentInternal;
            IL.RoR2.InfiniteTowerRun.OnWaveAllEnemiesDefeatedServer          += InfiniteTowerRun_OnWaveAllEnemiesDefeatedServer1;
            On.RoR2.InfiniteTowerBossWaveController.Initialize               += InfiniteTowerBossWaveController_Initialize;
            On.RoR2.InfiniteTowerWaveController.Initialize                   += InfiniteTowerWaveController_Initialize;
            On.RoR2.InfiniteTowerWaveController.DropRewards                  += InfiniteTowerWaveController_DropRewards;
            On.RoR2.FogDamageController.EvaluateTeam                         += FogDamageController_EvaluateTeam;
        }

        private void OnDisable()
        {
            On.RoR2.InfiniteTowerRun.Start                                   -= InfiniteTowerRun_Start;
            On.RoR2.Run.BuildDropTable                                       -= Run_BuildDropTable;
            On.RoR2.InfiniteTowerRun.OnWaveAllEnemiesDefeatedServer          -= InfiniteTowerRun_OnWaveAllEnemiesDefeatedServer;
            On.RoR2.InfiniteTowerRun.AdvanceWave                             -= InfiniteTowerRun_AdvanceWave;
            On.RoR2.InfiniteTowerRun.FixedUpdate                             -= InfiniteTowerRun_FixedUpdate;
            On.RoR2.InfiniteTowerRun.RecalculateDifficultyCoefficentInternal -= InfiniteTowerRun_RecalculateDifficultyCoefficentInternal;
            IL.RoR2.InfiniteTowerRun.OnWaveAllEnemiesDefeatedServer          -= InfiniteTowerRun_OnWaveAllEnemiesDefeatedServer1;
            On.RoR2.InfiniteTowerBossWaveController.Initialize               -= InfiniteTowerBossWaveController_Initialize;
            On.RoR2.InfiniteTowerWaveController.Initialize                   -= InfiniteTowerWaveController_Initialize;
            On.RoR2.InfiniteTowerWaveController.DropRewards                  -= InfiniteTowerWaveController_DropRewards;
            On.RoR2.FogDamageController.EvaluateTeam                         -= FogDamageController_EvaluateTeam;
        }

        // ProperSave: without these, a save made after the boss stage would warp to it again
        private void RegisterSaveData()
        {
            ModCompatibilityProperSave.Register("SimulacrumBossStageMod.BossStageCompleted",
                () => bossStageCompleted,
                saved => bossStageCompleted = saved is bool completed && completed);
            ModCompatibilityProperSave.Register("SimulacrumBossStageMod.NextStageBeforeBoss",
                () => nextStageBeforeBoss ? nextStageBeforeBoss.cachedName : "",
                saved => nextStageBeforeBoss = saved is string sceneName && sceneName != "" ? SceneCatalog.FindSceneDef(sceneName) : null);
            ModCompatibilityProperSave.Register("SimulacrumBossStageMod.ArtifactsEnabledByMod",
                () => artifactsEnabledByMod.Select(artifact => artifact.cachedName).ToList(),
                saved =>
                {
                    artifactsEnabledByMod.Clear();
                    if (saved is List<string> artifactNames)
                    {
                        foreach (var artifactName in artifactNames)
                        {
                            var artifact = ArtifactCatalog.FindArtifactDef(artifactName);
                            if (artifact)
                            {
                                artifactsEnabledByMod.Add(artifact);
                            }
                        }
                    }
                });
        }

        private void Run_BuildDropTable(On.RoR2.Run.orig_BuildDropTable orig, Run self)
        {
            orig(self);
            if (self is InfiniteTowerRun it)
            {
                if (BepConfig.Enabled.Value)
                {
                    SetArtifacts(it);
                }
            }
        }

        private void InfiniteTowerRun_Start(On.RoR2.InfiniteTowerRun.orig_Start orig, InfiniteTowerRun self)
        {
            // before orig: the run start turns on the artifacts of the first waves
            artifactsEnabledByMod.Clear();
            orig(self);
            bonusCounter = 0;
            nextBonusTime = 0f;
            bossSpawned.Clear();
            bossStageCompleted = false;
            nextStageBeforeBoss = null;
            zoneRadiusBoss = 1.0f;

            // Add poison elite tier
            if (poisonEliteTier == null)
            {
                poisonEliteTier = new CombatDirector.EliteTierDef
                {
                    costMultiplier = CombatDirector.baseEliteCostMultiplier * 3f,
                    eliteTypes = new EliteDef[3] { RoR2Content.Elites.Poison, DLC2Content.Elites.Bead, DLC3Content.Elites.Collective },
                    isAvailable = (SpawnCard.EliteRules rules) => checkEliteAvailable(rules),
                    canSelectWithoutAvailableEliteDef = false
                };
            }
            if (!CombatDirector.eliteTiers.Contains(poisonEliteTier)) {
                Array.Resize(ref CombatDirector.eliteTiers, CombatDirector.eliteTiers.Length + 1);
                CombatDirector.eliteTiers[CombatDirector.eliteTiers.Length - 1] = poisonEliteTier;
            }

            if (ModCompatibilityServerSideTweaks.enabled)
            {
                ModCompatibilityServerSideTweaks.ResetOverridePowerBias();
            }
            if (BepConfig.Enabled.Value && BepConfig.BossStageBoss.Value != BossEnum.None)
            {
                bossSpawnCard = Addressables.LoadAssetAsync<CharacterSpawnCard>(BossNames[BepConfig.BossStageBoss.Value]);
            }
        }

        private void InfiniteTowerRun_OnWaveAllEnemiesDefeatedServer1(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            c.GotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchCall<InfiniteTowerRun>("IsStageTransitionWave")
                );
            c.Index += 1;
            c.Remove();
            c.EmitDelegate<Func<InfiniteTowerRun, bool>>((self) =>
            {
                if (IsBossStageStarted(self.waveIndex))
                    return false;
                return self.IsStageTransitionWave();
            });

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
                if (!BepConfig.Enabled.Value)
                    return self.stageTransitionPortalCard;
                if (BepConfig.BossStage.Value == StageEnum.None)
                    return self.stageTransitionPortalCard;
                if (bossStageCompleted)
                    return self.stageTransitionPortalCard;
                if (self.waveIndex < BepConfig.BossStageStartWave.Value)
                    return self.stageTransitionPortalCard;
                // increase the max spawn distance from 30f to 45f to attempt to fix issues with the portal not spawning
                self.stageTransitionPortalMaxDistance = 45f;
                return iscVoidPortal.WaitForCompletion();
            });
        }

        private static bool checkEliteAvailable(SpawnCard.EliteRules rules)
        {
            if (Run.instance.GetType() == typeof(InfiniteTowerRun))
            {
                var enabled = BepConfig.Enabled.Value && BepConfig.BossStageTier2Elite.Value && ((InfiniteTowerRun)Run.instance).waveIndex > BepConfig.BossStageStartWave.Value;
                return enabled;
            }
            else
            {
                return false;
            }
        }

        private void InfiniteTowerBossWaveController_Initialize(On.RoR2.InfiniteTowerBossWaveController.orig_Initialize orig, InfiniteTowerBossWaveController self, int waveIndex, Inventory enemyInventory, GameObject spawnTarget)
        {
            orig(self, waveIndex, enemyInventory, spawnTarget);

            bool isFinalBoss = waveIndex > BepConfig.BossStageStartWave.Value && ((InfiniteTowerRun)Run.instance).IsStageTransitionWave();

            if (BepConfig.Enabled.Value && ModCompatibilityServerSideTweaks.enabled && isFinalBoss)
            {
                float currentBias = ModCompatibilityServerSideTweaks.GetCurrentPowerBias();
                bool isFinalBossWave = ((InfiniteTowerRun)Run.instance).IsStageTransitionWave();
                float targetBias = (float)Math.Max(ModCompatibilityServerSideTweaks.GetCurrentPowerBias(), isFinalBossWave ? 1.0f : 0.9f);
                float averageBias = (float)((currentBias + targetBias) / 2.0);
                ModCompatibilityServerSideTweaks.SetOverridePowerBias(averageBias);
            }
        }

        private void InfiniteTowerWaveController_Initialize(On.RoR2.InfiniteTowerWaveController.orig_Initialize orig, InfiniteTowerWaveController self, int waveIndex, Inventory enemyInventory, GameObject spawnTarget)
        {
            if (NetworkServer.active && BepConfig.Enabled.Value)
            {
                if (IsBossStageStarted(waveIndex) && ((InfiniteTowerRun)Run.instance).IsStageTransitionWave() && !bossStageCompleted)
                {
                    self.secondsBeforeSuddenDeath *= 3f;
                    self.wavePeriodSeconds *= 2.5f;
                }
            }
            orig(self, waveIndex, enemyInventory, spawnTarget);
        }

        private bool IsBossStageStarted(int waveIndex)
        {
            return waveIndex > BepConfig.BossStageStartWave.Value && !bossStageCompleted && BepConfig.Enabled.Value;
        }
        private void InfiniteTowerRun_FixedUpdate(On.RoR2.InfiniteTowerRun.orig_FixedUpdate orig, InfiniteTowerRun self)
        {
            orig(self);
            if (!NetworkServer.active)
                return;
#if DEBUG
            if (Input.GetKeyDown(KeyCode.F2))
            {
                Log.LogDebug($"Player pressed F2. Advancing to next wave");
                self.AdvanceWave();
            }
            if(Input.GetKeyDown(KeyCode.F12))
            {
                Log.LogDebug($"Player pressed F12. Moving safe ward");
                self.MoveSafeWard();
            }
            if (Input.GetKeyDown(KeyCode.F7))
            {
                debug_enemyTypeToSpawn = DecrementEnumValue(debug_enemyTypeToSpawn);
                Log.LogDebug($"Player pressed F7. Selected enemy: " + debug_enemyTypeToSpawn.ToString() + " (" + ((int)debug_enemyTypeToSpawn) + ")");
            }
            if (Input.GetKeyDown(KeyCode.F8))
            {
                debug_enemyTypeToSpawn = IncrementEnumValue(debug_enemyTypeToSpawn);
                Log.LogDebug($"Player pressed F8. Selected enemy: " + debug_enemyTypeToSpawn.ToString() + " (" + ((int)debug_enemyTypeToSpawn) + ")");
            }
            if (Input.GetKeyDown(KeyCode.F9))
            {
                Log.LogDebug($"Player pressed F9. Spawning! " + debug_enemyTypeToSpawn.ToString() + " (" + ((int)debug_enemyTypeToSpawn) + ") as: " + debug_eliteTypeToSpawn.ToString() + " (" + ((int)debug_eliteTypeToSpawn) + ")");
                EliteDef eliteDef = null;
                if (debug_eliteTypeToSpawn != EliteEnum.None)
                {
                    eliteDef = EliteDefs[debug_eliteTypeToSpawn];
                }
                if (debug_enemyTypeToSpawn != BossEnum.None) { 
                    self.waveController.combatDirector.Spawn(Addressables.LoadAssetAsync<CharacterSpawnCard>(BossNames[debug_enemyTypeToSpawn]).WaitForCompletion(), eliteDef, self.waveController.spawnTarget.transform, DirectorCore.MonsterSpawnDistance.Far, false); 
                }
            }
            if (Input.GetKeyDown(KeyCode.F10))
            {
                debug_eliteTypeToSpawn = DecrementEnumValue(debug_eliteTypeToSpawn);
                Log.LogDebug($"Player pressed F10. Selected elite: " + debug_eliteTypeToSpawn.ToString() + " (" + ((int)debug_eliteTypeToSpawn) + ")");
            }
            if (Input.GetKeyDown(KeyCode.F11))
            {
                debug_eliteTypeToSpawn = IncrementEnumValue(debug_eliteTypeToSpawn);
                Log.LogDebug($"Player pressed F11. Selected elite: " + debug_eliteTypeToSpawn.ToString() + " (" + ((int)debug_eliteTypeToSpawn) + ")");
            }
#endif
            if (!BepConfig.Enabled.Value)
                return;

            if (IsBossStageStarted(self.waveIndex) && self.IsStageTransitionWave() && nextBonusTime == 0f && !bossStageCompleted && BepConfig.BossStageBoss.Value != BossEnum.None)
            {
                if (self.waveController.GetNormalizedProgress() > 0.8f && bossSpawned.Count == 0)
                {
                    // spawn boss
                    EliteDef eliteDef = null;
                    if (BepConfig.BossStageBossElite.Value != EliteEnum.None)
                    {
                        eliteDef = EliteDefs[BepConfig.BossStageBossElite.Value];
                    }
                    var bossSpawnedListener = new UnityAction<GameObject, CombatDirector>((spawn, combatDir) => {
                        bossSpawned.Add(spawn);
                    });
                    self.waveController.combatDirector.onSpawnedWithDirectorServer.AddListener(bossSpawnedListener);
                    for (int i = 0; i < BepConfig.BossStageBossCount.Value; i++)
                    {
                        self.waveController.combatDirector.Spawn(bossSpawnCard.WaitForCompletion(), eliteDef, self.waveController.spawnTarget.transform, DirectorCore.MonsterSpawnDistance.Far, false);
                    }
                    self.waveController.combatDirector.onSpawnedWithDirectorServer.RemoveListener(bossSpawnedListener);
                }
                // increase/decrease safe ward zone size
                if (!self.waveController.isInSuddenDeath)
                {
                    if (self.waveController._zoneRadiusPercentage < BepConfig.BossStageBossRadius.Value)
                    {
                        zoneRadiusBoss = Math.Min(BepConfig.BossStageBossRadius.Value, zoneRadiusBoss + self.waveController.suddenDeathRadiusConstrictingPerSecond * Time.fixedDeltaTime);
                        self.waveController.Network_zoneRadiusPercentage = zoneRadiusBoss;
                    } 
                    else if (self.waveController._zoneRadiusPercentage > BepConfig.BossStageBossRadius.Value)
                    {
                        zoneRadiusBoss = Math.Max(0f, zoneRadiusBoss - self.waveController.suddenDeathRadiusConstrictingPerSecond * Time.fixedDeltaTime);
                        self.waveController.Network_zoneRadiusPercentage = zoneRadiusBoss;
                    }
                }
            }

            if (!bossStageCompleted)
                return;
            if (nextBonusTime == 0f)
                return;
            if (Run.instance.GetRunStopwatch() < nextBonusTime)
                return;

            zoneRadiusBoss = 1f;
            bossSpawned.Clear();
            if (bonusCounter >= BepConfig.BossStageLunarCoinsReward.Value)
            {
                nextBonusTime = 0f;
                // Spawn Teleporter
                if ((bool)nextStageBeforeBoss)
                    self.nextStageScene = nextStageBeforeBoss;
                else
                    self.PickNextStageSceneFromCurrentSceneDestinations();

                SpawnCard teleporterSpawnCard;
                if(BepConfig.BossStageCompleteEndRun.Value)
                {
                    // Spawn end teleporter
                    teleporterSpawnCard = iscVoidOutroPortal.WaitForCompletion();
                } else
                {
                    teleporterSpawnCard = self.stageTransitionPortalCard;
                }

                DirectorCore.instance.TrySpawnObject(new DirectorSpawnRequest(teleporterSpawnCard, new DirectorPlacementRule
                {
                    minDistance = 0f,
                    maxDistance = self.stageTransitionPortalMaxDistance,
                    placementMode = DirectorPlacementRule.PlacementMode.Approximate,
                    position = self.safeWardController.transform.position,
                    spawnOnTarget = self.safeWardController.transform
                }, self.safeWardRng));
                Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                {
                    baseToken = self.stageTransitionChatToken
                });
                if ((bool)self.safeWardController)
                {
                    self.safeWardController.WaitForPortal();
                }
            }
            else
            {
                // Spawn Coins
                int num = (int)Math.Ceiling(BepConfig.BossStageLunarCoinsReward.Value / 50f);
                float angle = 360f / (float)num;
                float percentage = (float)bonusCounter / (float)BepConfig.BossStageLunarCoinsReward.Value;
                Vector3 vector = Quaternion.AngleAxis(360f * percentage + 20f * bonusCounter, Vector3.up) * (Vector3.up * (20f + 10f * percentage + bonusCounter / 10f) + Vector3.forward * (5f + 5f * percentage + bonusCounter / 50f));
                Quaternion quaternion = Quaternion.AngleAxis(angle, Vector3.up);
                Vector3 position = self.safeWardController.transform.position;
                position = new Vector3(position.x, position.y + 2f, position.z);
                int num2 = 0;
                while (num2 < num)
                {
                    PickupDropletController.CreatePickupDroplet(new UniquePickup(PickupCatalog.FindPickupIndex("LunarCoin.Coin0")), position, vector, isDuplicated: false);
                    num2++;
                    bonusCounter++;
                    vector = quaternion * vector;
                }
                if (bonusCounter >= BepConfig.BossStageLunarCoinsReward.Value)
                {
                    nextBonusTime = Run.instance.GetRunStopwatch() + 25f;
                }
                else
                {
                    nextBonusTime = Run.instance.GetRunStopwatch() + 0.5f;
                }
            }
        }
        private void InfiniteTowerRun_OnWaveAllEnemiesDefeatedServer(On.RoR2.InfiniteTowerRun.orig_OnWaveAllEnemiesDefeatedServer orig, InfiniteTowerRun self, InfiniteTowerWaveController wc)
        {
            orig(self, wc);
            if (!NetworkServer.active)
                return;
            if (!BepConfig.Enabled.Value)
                return;
            if (self.waveIndex < BepConfig.BossStageStartWave.Value)
                return;
            if (ModCompatibilityServerSideTweaks.enabled)
            {
                ModCompatibilityServerSideTweaks.ResetOverridePowerBias();
            }
            if (!self.IsStageTransitionWave())
                return;

            if (BepConfig.BossStage.Value != StageEnum.None && SceneCatalog.currentSceneDef.cachedName != GetStageName(BepConfig.BossStage.Value) && !bossStageCompleted)
            {
                if (nextStageBeforeBoss == null)
                    nextStageBeforeBoss = self.nextStageScene;
                self.nextStageScene = SceneCatalog.allStageSceneDefs.Where(s => s.cachedName == GetStageName(BepConfig.BossStage.Value)).First();
            }

            if (IsBossStageStarted(self.waveIndex))
            {
                nextBonusTime = self.GetRunStopwatch();
                bossStageCompleted = true;
                if ((bool)self.safeWardController)
                {
                    self.safeWardController.WaitForPortal();
                }
                if (BepConfig.BossStageCompleteEndRun.Value)
                {
                    // lore detail: respawn commando as void survivor
                    foreach (PlayerCharacterMasterController instance in PlayerCharacterMasterController.instances)
                    {
                        CharacterMaster master = instance.master;
                        // dead players have no body
                        if (!instance.isConnected || !master.GetBody())
                        {
                            continue;
                        }
                        if (master.GetBody().baseNameToken == "COMMANDO_BODY_NAME")
                        {
                            var pos = master.GetBody().transform.position;
                            var rot = master.GetBody().transform.rotation;
                            master.bodyPrefab = BodyCatalog.FindBodyPrefab("VoidSurvivorBody");
                            master.Respawn(pos, rot);
                            Run.instance.HandlePlayerFirstEntryAnimation(master.GetBody(), pos, rot);
                        }
                    }
                }
            }
        }
        private void InfiniteTowerWaveController_DropRewards(On.RoR2.InfiniteTowerWaveController.orig_DropRewards orig, InfiniteTowerWaveController self)
        {
            if (NetworkServer.active && BepConfig.Enabled.Value && IsBossStageStarted(self.waveIndex) && ((InfiniteTowerRun)Run.instance).IsStageTransitionWave() && !bossStageCompleted && BepConfig.BossStageCompleteEndRun.Value)
            {
                // skip dropping rewards if boss wave was completed and run is to be ended
                return;
            }
            orig(self);
        }
        private void InfiniteTowerRun_RecalculateDifficultyCoefficentInternal(On.RoR2.InfiniteTowerRun.orig_RecalculateDifficultyCoefficentInternal orig, InfiniteTowerRun self)
        {
            orig(self);
            if (!BepConfig.Enabled.Value)
                return;
            if (BepConfig.DifficultyMultiplier1StartWave.Value <= self.waveIndex && self.waveIndex <= BepConfig.DifficultyMultiplier1EndWave.Value)
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
        private void InfiniteTowerRun_AdvanceWave(On.RoR2.InfiniteTowerRun.orig_AdvanceWave orig, InfiniteTowerRun self)
        {
            orig(self);
            SetArtifacts(self);
        }

        private void SetArtifacts(InfiniteTowerRun self)
        {
            if (!NetworkServer.active)
                return;
            var wantedArtifacts = new HashSet<ArtifactDef>();
            if (BepConfig.Enabled.Value)
            {
                AddWantedArtifact(wantedArtifacts, BepConfig.Artifact1.Value, BepConfig.Artifact1StartWave.Value, BepConfig.Artifact1EndWave.Value, self.waveIndex);
                AddWantedArtifact(wantedArtifacts, BepConfig.Artifact2.Value, BepConfig.Artifact2StartWave.Value, BepConfig.Artifact2EndWave.Value, self.waveIndex);
                AddWantedArtifact(wantedArtifacts, BepConfig.Artifact3.Value, BepConfig.Artifact3StartWave.Value, BepConfig.Artifact3EndWave.Value, self.waveIndex);
                AddWantedArtifact(wantedArtifacts, BepConfig.Artifact4.Value, BepConfig.Artifact4StartWave.Value, BepConfig.Artifact4EndWave.Value, self.waveIndex);
            }
            foreach (var artifact in wantedArtifacts)
            {
                if (!RunArtifactManager.instance.IsArtifactEnabled(artifact))
                {
                    RunArtifactManager.instance.SetArtifactEnabledServer(artifact, true);
                    artifactsEnabledByMod.Add(artifact);
                }
            }
            // only turn off what this mod turned on, so artifacts chosen in the lobby stay on
            foreach (var artifact in artifactsEnabledByMod.Where(artifact => !wantedArtifacts.Contains(artifact)).ToList())
            {
                RunArtifactManager.instance.SetArtifactEnabledServer(artifact, false);
                artifactsEnabledByMod.Remove(artifact);
            }
        }

        private static void AddWantedArtifact(HashSet<ArtifactDef> wantedArtifacts, ArtifactEnum artifact, int startWave, int endWave, int waveIndex)
        {
            if (artifact != ArtifactEnum.None && startWave <= waveIndex && waveIndex <= endWave)
            {
                wantedArtifacts.Add(GetArtifactDef(artifact));
            }
        }

        private void FogDamageController_EvaluateTeam(On.RoR2.FogDamageController.orig_EvaluateTeam orig, FogDamageController self, TeamIndex teamIndex)
        {
            orig(self, teamIndex);
            foreach(var boss in bossSpawned)
            {
                // the master of a dead boss is destroyed soon after
                if (!boss)
                    continue;
                var bossMaster = boss.GetComponent<CharacterMaster>();
                if (bossMaster == null)
                    continue;
                var bossBody = bossMaster.GetBody();
                if (bossBody == null)
                    continue;
                if (self.characterBodyToStacks != null) {
                    if(self.characterBodyToStacks.TryGetValue(bossBody, out var stack))
                    {
                        self.characterBodyToStacks.Remove(bossBody);
                        if(bossBody.HasBuff(RoR2Content.Buffs.VoidFogStackCooldown))
                            bossBody.RemoveOldestTimedBuff(RoR2Content.Buffs.VoidFogStackCooldown);
                    }
                }
            }
        }
    }
}
