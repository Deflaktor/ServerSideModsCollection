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
    [BepInDependency("com.rune580.riskofoptions", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.KingEnderBrine.InLobbyConfig", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("Deflaktor.SimulacrumStagePoolMod", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [R2APISubmoduleDependency()]
    [NetworkCompatibility(CompatibilityLevel.NoNeedForSync, VersionStrictness.DifferentModVersionsAreOk)]
    public class SimulacrumBossStageMod : BaseUnityPlugin
    {
        public static PluginInfo PInfo { get; private set; }
        public static SimulacrumBossStageMod instance;

        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Deflaktor";
        public const string PluginName = "SimulacrumBossStageMod";
        public const string PluginVersion = "1.0.0";

        public SpawnCard iscVoidPortal;
        public float nextBonusTime = 0f;
        public int bonusCounter = 0;

        public void Awake()
        {
            PInfo = Info;
            instance = this;

            Log.Init(Logger);
            BepConfig.Init();

            On.RoR2.InfiniteTowerRun.OnPrePopulateSceneServer += InfiniteTowerRun_OnPrePopulateSceneServer;
            On.RoR2.InfiniteTowerRun.OnWaveAllEnemiesDefeatedServer += InfiniteTowerRun_OnWaveAllEnemiesDefeatedServer;
            On.RoR2.InfiniteTowerRun.AdvanceWave += InfiniteTowerRun_AdvanceWave;
            On.RoR2.InfiniteTowerRun.FixedUpdate += InfiniteTowerRun_FixedUpdate;
            On.RoR2.InfiniteTowerRun.RecalculateDifficultyCoefficentInternal += InfiniteTowerRun_RecalculateDifficultyCoefficentInternal;
            IL.RoR2.InfiniteTowerRun.OnWaveAllEnemiesDefeatedServer += il =>
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
                    if (SceneCatalog.currentSceneDef.cachedName == GetStageName(BepConfig.BossStage.Value))
                    {
                        return false;
                    }
                    else
                    {
                        return self.IsStageTransitionWave();
                    }
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
                    if (self.waveIndex >= BepConfig.BossStageStartWave.Value && BepConfig.BossStage.Value != StageEnum.None)
                    {
                        return iscVoidPortal;
                    }
                    else
                    {
                        return self.stageTransitionPortalCard;
                    }
                });
            };

            Logger.LogDebug("Setting up '"+ PluginName + "' finished.");
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
            if (Run.instance.GetRunStopwatch() > nextBonusTime && nextBonusTime > 0f)
            {
                if (bonusCounter >= BepConfig.BossStageLunarCoinsReward.Value)
                {
                    Run.instance.BeginGameOver(RoR2Content.GameEndings.LimboEnding);
                }
                else
                {
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
                        nextBonusTime = Run.instance.GetRunStopwatch() + 50f;
                    }
                    else
                    {
                        nextBonusTime = Run.instance.GetRunStopwatch() + 0.5f;
                    }
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

        private void InfiniteTowerRun_OnWaveAllEnemiesDefeatedServer(On.RoR2.InfiniteTowerRun.orig_OnWaveAllEnemiesDefeatedServer orig, InfiniteTowerRun self, InfiniteTowerWaveController wc)
        {
            orig(self, wc);
            if (self.isGameOverServer)
            {
                return;
            }
            if (self.waveIndex >= BepConfig.BossStageStartWave.Value && BepConfig.BossStage.Value != StageEnum.None && self.IsStageTransitionWave())
            {
                if (SceneCatalog.currentSceneDef.cachedName == GetStageName(BepConfig.BossStage.Value))
                {
                    if (self.waveIndex >= BepConfig.BossStageStartWave.Value + 1 && nextBonusTime == 0f)
                    {
                        nextBonusTime = self.GetRunStopwatch();
                    }
                }
                else
                {
                    self.nextStageScene = SceneCatalog.allStageSceneDefs.Where(s => s.cachedName == GetStageName(BepConfig.BossStage.Value)).First();
                }
            }
        }

        private void InfiniteTowerRun_OnPrePopulateSceneServer(On.RoR2.InfiniteTowerRun.orig_OnPrePopulateSceneServer orig, InfiniteTowerRun self, SceneDirector sceneDirector)
        {
            if (BepConfig.BossStage.Value != StageEnum.None && self.nextStageScene.cachedName == GetStageName(BepConfig.BossStage.Value))
            {
                iscVoidPortal = LegacyResourcesAPI.Load<SpawnCard>("SpawnCards/InteractableSpawnCard/iscVoidPortal");
            }
        }
    }
}
