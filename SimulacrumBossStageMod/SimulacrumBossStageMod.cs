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
using static SimulacrumBossStageMod.EnumCollection;
using System.Linq;

namespace SimulacrumBossStageMod
{
    [BepInDependency(R2API.R2API.PluginGUID)]
    [BepInDependency("com.KingEnderBrine.InLobbyConfig", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [NetworkCompatibility(CompatibilityLevel.NoNeedForSync, VersionStrictness.DifferentModVersionsAreOk)]
    public class SimulacrumBossStageMod : BaseUnityPlugin
    {
        public static PluginInfo PInfo { get; private set; }
        public static SimulacrumBossStageMod instance;

        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Def";
        public const string PluginName = "SimulacrumBossStageMod";
        public const string PluginVersion = "1.0.0";

        public SpawnCard iscVoidPortal;
        public float nextBonusTime;
        public int bonusCounter;
        public bool bossStageCompleted;
        public SceneDef nextStageBeforeBoss;

        public void Awake()
        {
            PInfo = Info;
            instance = this;
            Log.Init(Logger);
            BepConfig.Init();
            iscVoidPortal = LegacyResourcesAPI.Load<SpawnCard>("SpawnCards/InteractableSpawnCard/iscVoidPortal");
        }
        private void OnEnable()
        {
            On.RoR2.InfiniteTowerRun.Start                                   += InfiniteTowerRun_Start;
            On.RoR2.InfiniteTowerRun.OnWaveAllEnemiesDefeatedServer          += InfiniteTowerRun_OnWaveAllEnemiesDefeatedServer;
            On.RoR2.InfiniteTowerRun.AdvanceWave                             += InfiniteTowerRun_AdvanceWave;
            On.RoR2.InfiniteTowerRun.FixedUpdate                             += InfiniteTowerRun_FixedUpdate;
            On.RoR2.InfiniteTowerRun.RecalculateDifficultyCoefficentInternal += InfiniteTowerRun_RecalculateDifficultyCoefficentInternal;
            IL.RoR2.InfiniteTowerRun.OnWaveAllEnemiesDefeatedServer          += InfiniteTowerRun_OnWaveAllEnemiesDefeatedServer1;
        }
        private void OnDisable()
        {
            On.RoR2.InfiniteTowerRun.Start                                   -= InfiniteTowerRun_Start;
            On.RoR2.InfiniteTowerRun.OnWaveAllEnemiesDefeatedServer          -= InfiniteTowerRun_OnWaveAllEnemiesDefeatedServer;
            On.RoR2.InfiniteTowerRun.AdvanceWave                             -= InfiniteTowerRun_AdvanceWave;
            On.RoR2.InfiniteTowerRun.FixedUpdate                             -= InfiniteTowerRun_FixedUpdate;
            On.RoR2.InfiniteTowerRun.RecalculateDifficultyCoefficentInternal -= InfiniteTowerRun_RecalculateDifficultyCoefficentInternal;
            IL.RoR2.InfiniteTowerRun.OnWaveAllEnemiesDefeatedServer          -= InfiniteTowerRun_OnWaveAllEnemiesDefeatedServer1;
        }
        private void InfiniteTowerRun_Start(On.RoR2.InfiniteTowerRun.orig_Start orig, InfiniteTowerRun self)
        {
            orig(self);
            bonusCounter = 0;
            nextBonusTime = 0f;
            bossStageCompleted = false;
            nextStageBeforeBoss = null;
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
                return iscVoidPortal;
            });
        }

        private bool IsBossStageStarted(int waveIndex)
        {
            return waveIndex > BepConfig.BossStageStartWave.Value && !bossStageCompleted && BepConfig.Enabled.Value;
        }
        private void InfiniteTowerRun_FixedUpdate(On.RoR2.InfiniteTowerRun.orig_FixedUpdate orig, InfiniteTowerRun self)
        {
            orig(self);
#if DEBUG
            if (Input.GetKeyDown(KeyCode.F2))
            {
                Log.LogDebug($"Player pressed F2. Advancing to next wave");
                self.AdvanceWave();
            }
#endif
            if (!BepConfig.Enabled.Value)
                return;
            if (!bossStageCompleted)
                return;
            if (nextBonusTime == 0f)
                return;
            if (Run.instance.GetRunStopwatch() < nextBonusTime)
                return;
            if (bonusCounter >= BepConfig.BossStageLunarCoinsReward.Value)
            {
                nextBonusTime = 0f;
                if (BepConfig.BossStageCompleteEndRun.Value)
                {
                    // End Run
                    Run.instance.BeginGameOver(RoR2Content.GameEndings.LimboEnding);
                }
                else
                {
                    // Spawn Teleporter
                    if ((bool)nextStageBeforeBoss)
                        self.nextStageScene = nextStageBeforeBoss;
                    else
                        self.PickNextStageSceneFromCurrentSceneDestinations();
                    DirectorCore.instance.TrySpawnObject(new DirectorSpawnRequest(self.stageTransitionPortalCard, new DirectorPlacementRule
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
                    PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex("LunarCoin.Coin0"), position, vector);
                    num2++;
                    bonusCounter++;
                    vector = quaternion * vector;
                }
                if (bonusCounter >= BepConfig.BossStageLunarCoinsReward.Value)
                {
                    nextBonusTime = Run.instance.GetRunStopwatch() + 30f;
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
            if (self.isGameOverServer)
                return;
            if (!BepConfig.Enabled.Value)
                return;
            if (self.waveIndex < BepConfig.BossStageStartWave.Value)
                return;
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
            }
        }
        private void InfiniteTowerRun_RecalculateDifficultyCoefficentInternal(On.RoR2.InfiniteTowerRun.orig_RecalculateDifficultyCoefficentInternal orig, InfiniteTowerRun self)
        {
            orig(self);
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
            if (BepConfig.Artifact1.Value != ArtifactEnum.None)
            {
                if (BepConfig.Artifact1StartWave.Value <= self.waveIndex && self.waveIndex <= BepConfig.Artifact1EndWave.Value)
                {
                    RunArtifactManager.instance.SetArtifactEnabledServer(GetArtifactDef(BepConfig.Artifact1.Value), true);
                }
                else if (self.waveIndex == BepConfig.Artifact1EndWave.Value + 1)
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
    }
}
