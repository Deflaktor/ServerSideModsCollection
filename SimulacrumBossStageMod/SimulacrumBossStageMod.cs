using BepInEx;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Newtonsoft.Json.Linq;
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
    [BepInDependency("com.KingEnderBrine.InLobbyConfig", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("Def.ServerSideTweaks", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("Def.SimulacrumNormalStages", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.KingEnderBrine.ProperSave", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class SimulacrumBossStageMod : BaseUnityPlugin
    {
        public static PluginInfo PInfo { get; private set; }
        public static SimulacrumBossStageMod instance;

        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Def";
        public const string PluginName = "SimulacrumBossStageMod";
        public const string PluginVersion = "1.3.2";

        public AsyncOperationHandle<SpawnCard> iscVoidPortal;
        // the printers that can be placed at the portal to the boss stage, with how likely each is
        private static readonly (string address, float weight)[] PortalPrinters =
        {
            ("RoR2/Base/Duplicator/iscDuplicator.asset", 6f),
            ("RoR2/Base/DuplicatorLarge/iscDuplicatorLarge.asset", 3f),
            ("RoR2/Base/DuplicatorMilitary/iscDuplicatorMilitary.asset", 1f),
            ("RoR2/Base/DuplicatorWild/iscDuplicatorWild.asset", 1f),
        };
        private static readonly string[] PortalCauldrons =
        {
            "RoR2/Base/LunarCauldrons/LunarCauldron, WhiteToGreen.prefab",
            "RoR2/Base/LunarCauldrons/LunarCauldron, GreenToRed Variant.prefab",
            "RoR2/Base/LunarCauldrons/LunarCauldron, RedToWhite Variant.prefab",
        };
        private const int PortalPrinterCount = 2;
        private const int PortalCauldronCount = 1;
        private static AsyncOperationHandle<SpawnCard> iscHallowedShrine;
        // the burst of Hallowed Concepts' curses, which shows each part of the shop appearing
        private static AsyncOperationHandle<GameObject> shopSpawnEffect;
        // the cauldrons have no spawn cards of their own, only the Bazaar's placed ones
        private static readonly Dictionary<string, InteractableSpawnCard> cauldronSpawnCards = new Dictionary<string, InteractableSpawnCard>();
        // whether the portal being spawned leads to the boss stage
        private static bool spawningBossStagePortal;
        // gives the boss stage's boss a boss health bar
        private AsyncOperationHandle<GameObject> bossCombatSquadPrefab;
        private GameObject bossSquad;
        public AsyncOperationHandle<SpawnCard> iscVoidOutroPortal;
        public AsyncOperationHandle<CharacterSpawnCard> bossSpawnCard;
        private BossEnum loadedBoss = BossEnum.None;
        public AsyncOperationHandle<EliteDef> edBead;
        public float nextBonusTime;
        public int bonusCounter;
        public bool bossStageCompleted;
        public List<GameObject> bossSpawned = new List<GameObject>();
        public SceneDef nextStageBeforeBoss;
        public float zoneRadiusBoss = 1.0f;
        public static CombatDirector.EliteTierDef poisonEliteTier;
        private readonly HashSet<ArtifactDef> artifactsEnabledByMod = new HashSet<ArtifactDef>();


        public void Awake()
        {
            PInfo = Info;
            instance = this;
            Log.Init(Logger);
            BepConfig.Init();
            // the boss stage gets its own Hallowed Shrine, with only the Hand of Life, at the portal there
            if (ModCompatibilitySimulacrumNormalStages.enabled)
            {
                ModCompatibilitySimulacrumNormalStages.SetSkipHallowedShrine(IsBossStage);
            }

            iscVoidPortal = Addressables.LoadAssetAsync<SpawnCard>("RoR2/DLC1/PortalVoid/iscVoidPortal.asset");
            iscHallowedShrine = Addressables.LoadAssetAsync<SpawnCard>("RoR2/DLC4/iscHallowedShrine.asset");
            shopSpawnEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC4/ProvUltPulseCurseBurstVFX.prefab");
            bossCombatSquadPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Core/BossCombatSquad.prefab");
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
            On.RoR2.InfiniteTowerWaveController.FixedUpdate                  += InfiniteTowerWaveController_FixedUpdate;
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
            On.RoR2.InfiniteTowerWaveController.FixedUpdate                  -= InfiniteTowerWaveController_FixedUpdate;
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
            RemoveBossSquad();
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
            // The director picks the last tier in the list that it can afford, so the tier goes before the first one that
            // costs more, e.g. Tier 2 after the first loop. At the end, it would be picked instead of those.
            if (!CombatDirector.eliteTiers.Contains(poisonEliteTier)) {
                var tiers = CombatDirector.eliteTiers.ToList();
                var index = tiers.FindIndex(1, tier => tier != null && tier.costMultiplier > poisonEliteTier.costMultiplier);
                tiers.Insert(index < 0 ? tiers.Count : index, poisonEliteTier);
                CombatDirector.eliteTiers = tiers.ToArray();
            }

            if (ModCompatibilityServerSideTweaks.enabled)
            {
                ModCompatibilityServerSideTweaks.ResetOverridePowerBias();
            }
            if (BepConfig.Enabled.Value && BepConfig.BossStageBoss.Value != BossEnum.None)
            {
                LoadBossSpawnCard();
            }
        }

        // The chosen boss, loaded ahead at run start. The mod or the boss can also be changed mid-run in InLobbyConfig.
        private AsyncOperationHandle<CharacterSpawnCard> LoadBossSpawnCard()
        {
            var boss = BepConfig.BossStageBoss.Value;
            if (boss != loadedBoss || !bossSpawnCard.IsValid())
            {
                bossSpawnCard = Addressables.LoadAssetAsync<CharacterSpawnCard>(BossNames[boss]);
                loadedBoss = boss;
            }
            return bossSpawnCard;
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
                spawningBossStagePortal = false;
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
                spawningBossStagePortal = true;
                return iscVoidPortal.WaitForCompletion();
            });

            // The void portal always leads to Void Locus by itself. Like Simulacrum's own portal, it leads to the run's next
            // stage instead, which InfiniteTowerRun_OnWaveAllEnemiesDefeatedServer sets to the boss stage.
            c.GotoNext(MoveType.After,
                x => x.MatchCallvirt<DirectorCore>(nameof(DirectorCore.TrySpawnObject))
                );
            c.Emit(OpCodes.Dup);
            c.EmitDelegate<Action<GameObject>>((portal) =>
            {
                if (portal && portal.TryGetComponent(out SceneExitController sceneExitController))
                {
                    sceneExitController.useRunNextStageScene = true;
                }
                if (portal && spawningBossStagePortal && BepConfig.BossStagePortalShop.Value)
                {
                    PlacePortalShop(portal.transform);
                }
                spawningBossStagePortal = false;
            });
        }

        // different random printers and cauldrons
        private static void PlacePortalShop(Transform portal)
        {
            var selection = new WeightedSelection<string>();
            foreach (var (address, weight) in PortalPrinters)
            {
                selection.AddChoice(address, weight);
            }
            for (int i = 0; i < PortalPrinterCount && selection.Count > 0; i++)
            {
                var choice = selection.EvaluateToChoiceIndex(Run.instance.stageRng.nextNormalizedFloat);
                PlaceNearPortal(Addressables.LoadAssetAsync<SpawnCard>(selection.GetChoice(choice).value).WaitForCompletion(), portal);
                selection.RemoveChoice(choice);
            }
            foreach (var address in PortalCauldrons.OrderBy(_ => Run.instance.stageRng.nextNormalizedFloat).Take(PortalCauldronCount))
            {
                PlaceNearPortal(GetCauldronSpawnCard(address), portal);
            }
            if (DLC4Content.DLC4 != null && Run.instance.IsExpansionEnabled(DLC4Content.DLC4))
            {
                var shrine = PlaceNearPortal(iscHallowedShrine.WaitForCompletion(), portal);
                // only the Hand of Life, which turns Hexes into Hallowed items: the shrine spawns its hands when it starts
                if (shrine != null && shrine.TryGetComponent(out HallowingShrineSplitBase splitBase) && splitBase.NodePrefabsToSpawn != null)
                {
                    splitBase.NodePrefabsToSpawn = splitBase.NodePrefabsToSpawn.Where(node => node != null && node.name.Contains("Life")).ToArray();
                }
            }
        }

        private static InteractableSpawnCard GetCauldronSpawnCard(string address)
        {
            if (!cauldronSpawnCards.TryGetValue(address, out var spawnCard))
            {
                spawnCard = ScriptableObject.CreateInstance<InteractableSpawnCard>();
                spawnCard.prefab = Addressables.LoadAssetAsync<GameObject>(address).WaitForCompletion();
                spawnCard.sendOverNetwork = true;
                spawnCard.hullSize = HullClassification.Golem;
                spawnCard.nodeGraphType = RoR2.Navigation.MapNodeGroup.GraphType.Ground;
                spawnCard.occupyPosition = true;
                spawnCard.orientToFloor = true;
                cauldronSpawnCards[address] = spawnCard;
            }
            return spawnCard;
        }

        private static GameObject PlaceNearPortal(SpawnCard spawnCard, Transform portal)
        {
            // clear of the portal, so that nothing blocks it or is entered by accident
            var spawned = DirectorCore.instance.TrySpawnObject(new DirectorSpawnRequest(spawnCard, new DirectorPlacementRule
            {
                minDistance = 15f,
                maxDistance = 40f,
                placementMode = DirectorPlacementRule.PlacementMode.Approximate,
                position = portal.position,
                spawnOnTarget = portal
            }, Run.instance.stageRng));
            if (spawned != null)
            {
                EffectManager.SpawnEffect(shopSpawnEffect.WaitForCompletion(), new EffectData
                {
                    origin = spawned.transform.position,
                    scale = 6f
                }, transmit: true);
            }
            return spawned;
        }

        // whether the current stage is the boss stage
        private static bool IsBossStage()
        {
            return Run.instance is InfiniteTowerRun && BepConfig.Enabled.Value && BepConfig.BossStage.Value != StageEnum.None
                && SceneCatalog.currentSceneDef != null && SceneCatalog.currentSceneDef == FindSceneDef(BepConfig.BossStage.Value);
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
            // A final boss wave raises the power bias, which goes back at the wave's end only if all its enemies are
            // defeated; it may end otherwise, e.g. when its time runs out. Its own Initialize raises it again after this.
            if (NetworkServer.active && ModCompatibilityServerSideTweaks.enabled)
            {
                ModCompatibilityServerSideTweaks.ResetOverridePowerBias();
            }
            orig(self, waveIndex, enemyInventory, spawnTarget);
        }

#if DEBUG
        // advancing the wave and spawning a boss as an elite are DebugToolkit's next_wave and spawn_ai
        [ConCommand(commandName = "sbsm_move_safe_ward", flags = ConVarFlags.ExecuteOnServer, helpText = "Moves Simulacrum's safe ward to a new place. Debug builds only.")]
        private static void CCMoveSafeWard(ConCommandArgs args)
        {
            if (Run.instance is InfiniteTowerRun infiniteTowerRun)
            {
                infiniteTowerRun.MoveSafeWard();
            }
            else
            {
                Debug.Log("Not in a Simulacrum run.");
            }
        }
#endif

        private bool IsBossStageStarted(int waveIndex)
        {
            return waveIndex > BepConfig.BossStageStartWave.Value && !bossStageCompleted && BepConfig.Enabled.Value;
        }
        private void InfiniteTowerRun_FixedUpdate(On.RoR2.InfiniteTowerRun.orig_FixedUpdate orig, InfiniteTowerRun self)
        {
            orig(self);
            if (!NetworkServer.active)
                return;
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
                        self.waveController.combatDirector.Spawn(LoadBossSpawnCard().WaitForCompletion(), eliteDef, self.waveController.spawnTarget.transform, DirectorCore.MonsterSpawnDistance.Far, false);
                    }
                    self.waveController.combatDirector.onSpawnedWithDirectorServer.RemoveListener(bossSpawnedListener);
                    ShowBossHealthBar();
                }
            }

            if (bossSquad && bossSquad.GetComponent<CombatSquad>().memberCount == 0)
            {
                RemoveBossSquad();
            }

            if (!bossStageCompleted)
                return;
            if (nextBonusTime == 0f)
                return;
            if (Run.instance.GetRunStopwatch() < nextBonusTime)
                return;

            zoneRadiusBoss = 1f;
            bossSpawned.Clear();
            RemoveBossSquad();
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
        // after the wave controller's own update, which sets the radius back to normal every tick outside sudden death
        private void InfiniteTowerWaveController_FixedUpdate(On.RoR2.InfiniteTowerWaveController.orig_FixedUpdate orig, InfiniteTowerWaveController self)
        {
            orig(self);
            if (!NetworkServer.active || !BepConfig.Enabled.Value || !(Run.instance is InfiniteTowerRun run) || run.waveController != self)
                return;
            if (IsBossStageStarted(run.waveIndex) && run.IsStageTransitionWave() && nextBonusTime == 0f && !bossStageCompleted && BepConfig.BossStageBoss.Value != BossEnum.None && !self.isInSuddenDeath)
            {
                // grow or shrink the safe ward zone to the boss battleground radius
                float targetRadius = BepConfig.BossStageBossRadius.Value;
                float step = self.suddenDeathRadiusConstrictingPerSecond * Time.fixedDeltaTime;
                zoneRadiusBoss = zoneRadiusBoss < targetRadius ? Math.Min(targetRadius, zoneRadiusBoss + step) : Math.Max(targetRadius, zoneRadiusBoss - step);
                self.Network_zoneRadiusPercentage = zoneRadiusBoss;
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

            if (BepConfig.BossStage.Value != StageEnum.None && !bossStageCompleted)
            {
                var bossStageScene = FindSceneDef(BepConfig.BossStage.Value);
                if (bossStageScene == null)
                {
                    Log.LogWarning($"Could not find the scene of the boss stage {BepConfig.BossStage.Value}");
                }
                else if (SceneCatalog.currentSceneDef != bossStageScene)
                {
                    if (nextStageBeforeBoss == null)
                        nextStageBeforeBoss = self.nextStageScene;
                    self.nextStageScene = bossStageScene;
                }
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

        // The boss also belongs to a boss squad, as at a teleporter, which shows its health bar to every player. The wave
        // still counts it as one of its monsters. The squad drops no extra item.
        private void ShowBossHealthBar()
        {
            if (bossSpawned.Count == 0)
            {
                return;
            }
            RemoveBossSquad();
            bossSquad = UnityEngine.Object.Instantiate(bossCombatSquadPrefab.WaitForCompletion());
            bossSquad.GetComponent<BossGroup>().dropPosition = null;
            // never defeated, which would count as a teleporter boss, e.g. for ReadyUp's vote; it's removed instead
            // once the boss is dead
            bossSquad.GetComponent<CombatSquad>().delayDefeat = true;
            NetworkServer.Spawn(bossSquad);
            var combatSquad = bossSquad.GetComponent<CombatSquad>();
            foreach (var boss in bossSpawned)
            {
                if (boss && boss.TryGetComponent(out CharacterMaster master))
                {
                    combatSquad.AddMember(master);
                }
            }
        }

        private void RemoveBossSquad()
        {
            if (bossSquad)
            {
                UnityEngine.Object.Destroy(bossSquad);
            }
            bossSquad = null;
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
