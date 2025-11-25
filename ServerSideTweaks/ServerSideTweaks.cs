using BepInEx;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using R2API.Utils;
using RoR2;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using UnityEngine.UIElements.UIR;
using static RoR2.GenericPickupController;
using static RoR2.Networking.NetworkManagerSystem;
using static UnityEngine.ResourceManagement.ResourceProviders.SceneProvider;

namespace ServerSideTweaks
{
    [BepInDependency(R2API.R2API.PluginGUID)]
    [BepInDependency("com.KingEnderBrine.InLobbyConfig", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.funkfrog_sipondo.sharesuite", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.MagnusMagnuson.BiggerBazaar", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.Lunzir.BazaarIsMyHome", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [NetworkCompatibility(CompatibilityLevel.NoNeedForSync, VersionStrictness.DifferentModVersionsAreOk)]
    public class ServerSideTweaks : BaseUnityPlugin
    {
        public static PluginInfo PInfo { get; private set; }
        public static ServerSideTweaks instance;
        public static float directorEnemyPowerBiasOverride = -1;
        public static List<EquipmentIndex> disableEquipments = new List<EquipmentIndex>();
        public static List<PickupIndex> availableEquipmentDropList_Saved = new List<PickupIndex>();
        public static List<PickupIndex> availableLunarItemDropList_Saved = new List<PickupIndex>();
        public static List<PickupIndex> availableLunarEquipmentDropList_Saved = new List<PickupIndex>();
        public static SimulacrumLootTweaks simulacrumLootTweaks = new SimulacrumLootTweaks();

        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Def";
        public const string PluginName = "ServerSideTweaks";
        public const string PluginVersion = "2.1.1";

        public void Awake()
        {
            PInfo = Info;
            instance = this;
            Log.Init(Logger);
            BepConfig.Init();
            BepConfig.ChildReducedTeleportRange.SettingChanged += ChildReducedTeleportRange_SettingChanged;
            ChildReducedTeleportRange_SettingChanged(null, null);
            FastSimulacrumVrab.Setup();
            simulacrumLootTweaks.Init();
        }

        private void ChildReducedTeleportRange_SettingChanged(object sender, EventArgs e)
        {
            if(BepConfig.ChildReducedTeleportRange.Value)
            {
                IL.EntityStates.ChildMonster.FrolicAway.TeleportAway += FrolicAway_TeleportAway;
            }
            else
            {
                IL.EntityStates.ChildMonster.FrolicAway.TeleportAway -= FrolicAway_TeleportAway;
            }
        }

        private void OnEnable()
        {
            On.RoR2.Run.Start                                                    += Run_Start;
            On.RoR2.InfiniteTowerRun.OnWaveAllEnemiesDefeatedServer              += InfiniteTowerRun_OnWaveAllEnemiesDefeatedServer;
            On.RoR2.InfiniteTowerRun.AdvanceWave                                 += InfiniteTowerRun_AdvanceWave;
            IL.RoR2.CombatDirector.AttemptSpawnOnTarget                          += CombatDirector_AttemptSpawnOnTarget;
            On.RoR2.Run.FixedUpdate                                              += Run_FixedUpdate;


            //IL.RoR2.Artifacts.CommandArtifactManager.OnDropletHitGroundServer    += CommandArtifactManager_OnDropletHitGroundServer;
            // On.RoR2.Run.PickNextStageScene                                       += Run_PickNextStageScene;
            On.RoR2.Items.RandomlyLunarUtils.CheckForLunarReplacement_UniquePickup_Xoroshiro128Plus += RandomlyLunarUtils_CheckForLunarReplacement_UniquePickup_Xoroshiro128Plus;
            On.RoR2.Items.RandomlyLunarUtils.CheckForLunarReplacementUniqueArray += RandomlyLunarUtils_CheckForLunarReplacementUniqueArray;
            On.RoR2.InfiniteTowerWaveController.Initialize                       += InfiniteTowerWaveController_Initialize;
            
            On.RoR2.ChildMonsterController.RegisterTeleport += ChildMonsterController_RegisterTeleport;

            simulacrumLootTweaks.Hook();
            FastSimulacrumVrab.Enable();
        }

        private void OnDisable()
        {
            On.RoR2.Run.Start                                                    -= Run_Start;
            On.RoR2.InfiniteTowerRun.OnWaveAllEnemiesDefeatedServer              -= InfiniteTowerRun_OnWaveAllEnemiesDefeatedServer;
            On.RoR2.InfiniteTowerRun.AdvanceWave                                 -= InfiniteTowerRun_AdvanceWave;
            IL.RoR2.CombatDirector.AttemptSpawnOnTarget                          -= CombatDirector_AttemptSpawnOnTarget;
            On.RoR2.Run.FixedUpdate                                              -= Run_FixedUpdate;

            //IL.RoR2.Artifacts.CommandArtifactManager.OnDropletHitGroundServer    -= CommandArtifactManager_OnDropletHitGroundServer;
            // On.RoR2.Run.PickNextStageScene                                       -= Run_PickNextStageScene;
            On.RoR2.Items.RandomlyLunarUtils.CheckForLunarReplacement_UniquePickup_Xoroshiro128Plus -= RandomlyLunarUtils_CheckForLunarReplacement_UniquePickup_Xoroshiro128Plus;
            On.RoR2.Items.RandomlyLunarUtils.CheckForLunarReplacementUniqueArray -= RandomlyLunarUtils_CheckForLunarReplacementUniqueArray;
            On.RoR2.InfiniteTowerWaveController.Initialize                       -= InfiniteTowerWaveController_Initialize;

            On.RoR2.ChildMonsterController.RegisterTeleport -= ChildMonsterController_RegisterTeleport;

            simulacrumLootTweaks.Unhook();
            FastSimulacrumVrab.Disable();
        }

        private void ChildMonsterController_RegisterTeleport(On.RoR2.ChildMonsterController.orig_RegisterTeleport orig, ChildMonsterController self, bool addInvincibility)
        {
            if (BepConfig.Enabled.Value && BepConfig.ChildRemoveInvincibility.Value)
            {
                orig(self, false);
            }
            else
            {
                orig(self, addInvincibility);
            }
        }

        private void FrolicAway_TeleportAway(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            // var label = c.DefineLabel();
            // IL_0033: ldc.r4 100
            // IL_0038: ldc.r4 200
            c.GotoNext(
            x => x.MatchLdcR4(100),
            x => x.MatchLdcR4(200)
            );
            c.Remove();
            c.Remove();
            c.Emit(OpCodes.Ldc_R4, 50f);
            c.Emit(OpCodes.Ldc_R4, 100f);
        }

        private void InfiniteTowerWaveController_Initialize(On.RoR2.InfiniteTowerWaveController.orig_Initialize orig, InfiniteTowerWaveController self, int waveIndex, Inventory enemyInventory, GameObject spawnTarget)
        {
            if (BepConfig.Enabled.Value)
            {
                self.maxSquadSize = BepConfig.SimulacrumMaxSquadSize.Value;
            }

            if (NetworkServer.active && BepConfig.Enabled.Value)
            {
                self.wavePeriodSeconds *= BepConfig.SimulacrumWavePeriodSecondsFactor.Value;
            }
            orig(self, waveIndex, enemyInventory, spawnTarget);
        }

        private void Run_Start(On.RoR2.Run.orig_Start orig, Run self)
        {
            simulacrumLootTweaks.RunStart();
            ResetOverridePowerBias();
            orig(self);
        }

        public static void SetOverridePowerBias(float powerBias)
        {
            directorEnemyPowerBiasOverride = powerBias;
        }

        public static float GetCurrentPowerBias()
        {
            if (Run.instance.GetType() == typeof(InfiniteTowerRun) && BepConfig.Enabled.Value)
            {
                return BepConfig.SimulacrumDirectorEnemyPowerBias.Value;
            }
            else if (BepConfig.Enabled.Value)
            {
                return BepConfig.ClassicDirectorEnemyPowerBias.Value;
            } else
            {
                return 0.5f;
            }
        }

        public static void ResetOverridePowerBias()
        {
            directorEnemyPowerBiasOverride = -1;
        }

        private void Run_FixedUpdate(On.RoR2.Run.orig_FixedUpdate orig, Run self)
        {
            orig(self);

            if (!NetworkServer.active)
                return;

#if DEBUG

            if (Input.GetKeyDown(KeyCode.F3))
            {
                BepConfig.SimulacrumDirectorEnemyPowerBias.Value -= 0.1f;
                BepConfig.ClassicDirectorEnemyPowerBias.Value -= 0.1f;
                Log.LogDebug($"Player pressed F3. SimulacrumDirectorEnemyPowerBias: " + BepConfig.SimulacrumDirectorEnemyPowerBias.Value);
                Log.LogDebug($"Player pressed F3. ClassicDirectorEnemyPowerBias: " + BepConfig.ClassicDirectorEnemyPowerBias.Value);
            }
            if (Input.GetKeyDown(KeyCode.F4))
            {
                BepConfig.SimulacrumDirectorEnemyPowerBias.Value += 0.1f;
                BepConfig.ClassicDirectorEnemyPowerBias.Value += 0.1f;
                Log.LogDebug($"Player pressed F4. SimulacrumDirectorEnemyPowerBias: " + BepConfig.SimulacrumDirectorEnemyPowerBias.Value);
                Log.LogDebug($"Player pressed F4. ClassicDirectorEnemyPowerBias: " + BepConfig.ClassicDirectorEnemyPowerBias.Value);
            }
            if (Input.GetKeyDown(KeyCode.F5))
            {
                simulacrumLootTweaks.GiveAllPlayersItemCredit(-1f);
            }
            if (Input.GetKeyDown(KeyCode.F6))
            {
                simulacrumLootTweaks.GiveAllPlayersItemCredit(+1f);
            }
#endif
        }

        #region PowerBias
        private bool SkipWithPowerBias(float costMultipliedByMaximumNumberToSpawnBeforeSkipping, CombatDirector combatDirector, float powerBias)
        {
            float cost = costMultipliedByMaximumNumberToSpawnBeforeSkipping / combatDirector.maximumNumberToSpawnBeforeSkipping;
            // bias < 0.5f: we want to skip when enemies are too expensive
            if (cost > combatDirector.mostExpensiveMonsterCostInDeck * (0.5f + powerBias) * (0.5f + powerBias) * (0.5f + powerBias))
            {
                // but only sometimes
                float safetyMargin = Math.Max(0f, 0.1f - powerBias);
                if (2f * (0.5f - powerBias) - safetyMargin > RoR2Application.rng.nextNormalizedFloat)
                    return true;
            }
            // bias > 0.5f: we want to skip more often when enemies are cheap
            // bias < 0.5f: we want to skip less often when enemies are cheap
            return costMultipliedByMaximumNumberToSpawnBeforeSkipping * (1.5f - powerBias) * (1.5f - powerBias) < combatDirector.monsterCredit;
        }
        private void CombatDirector_AttemptSpawnOnTarget(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            var label = c.DefineLabel();
            // IL_017c: ldloc.1
            // IL_017d: ldarg.0
            // IL_017e: ldfld int32 RoR2.CombatDirector::maximumNumberToSpawnBeforeSkipping
            // IL_0183: mul
            // IL_0184: conv.r4
            // IL_0185: ldarg.0
            // IL_0186: ldfld float32 RoR2.CombatDirector::monsterCredit
            c.GotoNext(
            x => x.MatchLdloc(1),
            x => x.MatchLdarg(0),
            x => x.MatchLdfld<CombatDirector>("maximumNumberToSpawnBeforeSkipping"),
            x => x.MatchMul(),
            x => x.MatchConvR4(),
            x => x.MatchLdarg(0),
            x => x.MatchLdfld<CombatDirector>("monsterCredit")
            );
            c.Index += 6;
            c.Remove();
            c.Remove();
            c.EmitDelegate<Func<float, CombatDirector, bool>>((costMultipliedByMaximumNumberToSpawnBeforeSkipping, combatDirector) =>
            {
                if(directorEnemyPowerBiasOverride >= 0)
                {
                    return SkipWithPowerBias(costMultipliedByMaximumNumberToSpawnBeforeSkipping, combatDirector, directorEnemyPowerBiasOverride);
                }
                else if (Run.instance.GetType() == typeof(InfiniteTowerRun) && BepConfig.Enabled.Value)
                {
                    return SkipWithPowerBias(costMultipliedByMaximumNumberToSpawnBeforeSkipping, combatDirector, BepConfig.SimulacrumDirectorEnemyPowerBias.Value);
                } else if(BepConfig.Enabled.Value)
                {
                    return SkipWithPowerBias(costMultipliedByMaximumNumberToSpawnBeforeSkipping, combatDirector, BepConfig.ClassicDirectorEnemyPowerBias.Value);
                }
                // vanilla
                return costMultipliedByMaximumNumberToSpawnBeforeSkipping < combatDirector.monsterCredit;
            });
            c.Emit(OpCodes.Brfalse, label);
            c.Index += 2;
            /*
            c.Index = 222;
            c.GotoNext(x => x.MatchLdarg(0));
            Debug.Log(c.ToString());
            c.GotoNext(x => x.MatchLdfld<CombatDirector>("currentMonsterCard"));
            Debug.Log(c.ToString());
            c.GotoNext(x => x.MatchCallvirt<DirectorCard>("GetSpawnCard"));
            Debug.Log(c.ToString());
            c.GotoNext(x => x.MatchStloc(4));
            Debug.Log(c.ToString());*/

            // IL_0250: ldarg.0
            // IL_0251: ldfld class RoR2.DirectorCard RoR2.CombatDirector::currentMonsterCard
            // IL_0256: callvirt instance class RoR2.SpawnCard RoR2.DirectorCard::GetSpawnCard()
            // IL_025b: stloc.s 4
            c.GotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<CombatDirector>("currentMonsterCard"),
                x => x.MatchCallvirt<DirectorCard>("GetSpawnCard"),
                x => x.MatchStloc(4)
            );
            c.MarkLabel(label);
        }
        #endregion
        #region ArtifactOfHonor
        private void InfiniteTowerRun_OnWaveAllEnemiesDefeatedServer(On.RoR2.InfiniteTowerRun.orig_OnWaveAllEnemiesDefeatedServer orig, InfiniteTowerRun self, InfiniteTowerWaveController wc)
        {
            orig(self, wc);
            if (!BepConfig.Enabled.Value || !NetworkServer.active)
                return;

            if (BepConfig.SimulacrumCommencementArtifactDissonanceChance.Value <= 0f)
                return;

            if (IsCurrentMapCommencment())
            {
                RunArtifactManager.instance.SetArtifactEnabledServer(RoR2Content.Artifacts.MixEnemy, false);
            }
        }
        private void InfiniteTowerRun_AdvanceWave(On.RoR2.InfiniteTowerRun.orig_AdvanceWave orig, InfiniteTowerRun self)
        {
            orig(self);
            if (!BepConfig.Enabled.Value || !NetworkServer.active)
                return;

            if (BepConfig.SimulacrumCommencementArtifactDissonanceChance.Value <= 0f)
                return;

            if (IsCurrentMapCommencment())
            {
                if (BepConfig.SimulacrumCommencementArtifactDissonanceChance.Value > RoR2Application.rng.nextNormalizedFloat)
                {
                    RunArtifactManager.instance.SetArtifactEnabledServer(RoR2Content.Artifacts.MixEnemy, true);
                }
            }
        }
        #endregion
        private bool IsCurrentMapInBazaar()
        {
            return SceneManager.GetActiveScene().name == "bazaar";
        }
        private bool IsCurrentMapCommencment()
        {
            return SceneManager.GetActiveScene().name == "moon" || SceneManager.GetActiveScene().name == "moon2" || SceneManager.GetActiveScene().name == "itmoon";
        }

        private UniquePickup RandomlyLunarUtils_CheckForLunarReplacement_UniquePickup_Xoroshiro128Plus(On.RoR2.Items.RandomlyLunarUtils.orig_CheckForLunarReplacement_UniquePickup_Xoroshiro128Plus orig, UniquePickup uniquePickup, Xoroshiro128Plus rng)
        {
            uniquePickup = orig(uniquePickup, rng);
            if(!BepConfig.Enabled.Value || uniquePickup.Equals(UniquePickup.none))
            {
                return uniquePickup;
            }
            PickupDef pickupDef = PickupCatalog.GetPickupDef(uniquePickup.pickupIndex);
            if(pickupDef != null && pickupDef.isLunar)
            {
                if(!BepConfig.NoPearlsInBazaar.Value || !IsCurrentMapInBazaar())
                {
                    float randomNumber = rng.nextNormalizedFloat;
                    if (randomNumber < BepConfig.IrradiantPearlReplacesLunarItemChance.Value)
                    {
                        uniquePickup = new UniquePickup(PickupCatalog.FindPickupIndex(ItemCatalog.FindItemIndex("ShinyPearl")));
                    } 
                    else if (randomNumber < BepConfig.IrradiantPearlReplacesLunarItemChance.Value + BepConfig.PearlReplacesLunarItemChance.Value)
                    {
                        uniquePickup = new UniquePickup(PickupCatalog.FindPickupIndex(ItemCatalog.FindItemIndex("Pearl")));
                    }
                    return uniquePickup;
                }
            }
            return uniquePickup;
        }


        private void RandomlyLunarUtils_CheckForLunarReplacementUniqueArray(On.RoR2.Items.RandomlyLunarUtils.orig_CheckForLunarReplacementUniqueArray orig, PickupIndex[] pickupIndices, Xoroshiro128Plus rng)
        {
            orig(pickupIndices, rng);
            if (!BepConfig.Enabled.Value)
                return;

            for (int i = 0; i < pickupIndices.Length; i++)
            {
                PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupIndices[i]);
                if (pickupDef != null && pickupDef.isLunar)
                {
                    if (!BepConfig.NoPearlsInBazaar.Value || !IsCurrentMapInBazaar())
                    {
                        float randomNumber = rng.nextNormalizedFloat;
                        if (randomNumber < BepConfig.IrradiantPearlReplacesLunarItemChance.Value)
                        {
                            pickupIndices[i] = PickupCatalog.FindPickupIndex(ItemCatalog.FindItemIndex("ShinyPearl"));
                        }
                        else if (randomNumber < BepConfig.IrradiantPearlReplacesLunarItemChance.Value + BepConfig.PearlReplacesLunarItemChance.Value)
                        {
                            pickupIndices[i] = PickupCatalog.FindPickupIndex(ItemCatalog.FindItemIndex("Pearl"));
                        }
                    }
                }
            }
        }
    }
}
