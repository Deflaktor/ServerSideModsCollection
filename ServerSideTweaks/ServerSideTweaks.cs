using BepInEx;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
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
    [BepInDependency("com.KingEnderBrine.ProperSave", BepInDependency.DependencyFlags.SoftDependency)]
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
        private static bool dissonanceEnabledForWave = false;

        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Def";
        public const string PluginName = "ServerSideTweaks";
        public const string PluginVersion = "2.2.1";

        public void Awake()
        {
            PInfo = Info;
            instance = this;
            Log.Init(Logger);
            BepConfig.Init();
            BepConfig.ChildReducedTeleportRange.SettingChanged += ChildReducedTeleportRange_SettingChanged;
            ChildReducedTeleportRange_SettingChanged(null, null);
            FastSimulacrumVrab.Setup();
            simulacrumLootTweaks.RegisterSaveData();
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
            // a private property, for which there is no generated hook
            finalMonsterCardsSelectionHook = new Hook(typeof(CombatDirector).GetProperty("finalMonsterCardsSelection", BindingFlags.Instance | BindingFlags.NonPublic).GetGetMethod(true),
                typeof(ServerSideTweaks).GetMethod(nameof(CombatDirector_get_finalMonsterCardsSelection), BindingFlags.Static | BindingFlags.NonPublic));
            On.RoR2.CombatDirector.Awake                                         += CombatDirector_Awake;

            On.RoR2.Items.RandomlyLunarUtils.CheckForLunarReplacementUniqueArray += RandomlyLunarUtils_CheckForLunarReplacementUniqueArray;
            On.RoR2.Items.RandomlyLunarUtils.CheckForLunarReplacement_UniquePickup_Xoroshiro128Plus += RandomlyLunarUtils_CheckForLunarReplacement_UniquePickup_Xoroshiro128Plus;
            On.RoR2.InfiniteTowerWaveController.Initialize                       += InfiniteTowerWaveController_Initialize;
            On.RoR2.InfiniteTowerWaveController.FixedUpdate                      += InfiniteTowerWaveController_FixedUpdate;
            On.RoR2.InfiniteTowerWaveController.OnAllEnemiesDefeatedServer       += InfiniteTowerWaveController_OnAllEnemiesDefeatedServer;
            On.RoR2.SceneDirector.GenerateInteractableCardSelection              += SceneDirector_GenerateInteractableCardSelection;
            
            On.RoR2.ChildMonsterController.RegisterTeleport += ChildMonsterController_RegisterTeleport;
            On.RoR2.Artifacts.MonsterTeamGainsItemsArtifactManager.OnServerCardSpawnedGlobal += MonsterTeamGainsItemsArtifactManager_OnServerCardSpawnedGlobal;
            On.RoR2.InfiniteTowerWaveController.OnCombatSquadMemberDiscovered    += InfiniteTowerWaveController_OnCombatSquadMemberDiscovered;

            simulacrumLootTweaks.Hook();
            FastSimulacrumVrab.Enable();
        }

        // the Artifact of Evolution's items
        private void MonsterTeamGainsItemsArtifactManager_OnServerCardSpawnedGlobal(On.RoR2.Artifacts.MonsterTeamGainsItemsArtifactManager.orig_OnServerCardSpawnedGlobal orig, SpawnCard.SpawnResult spawnResult)
        {
            orig(spawnResult);
            RemoveAlienHeads(spawnResult.spawnedInstance ? spawnResult.spawnedInstance.GetComponent<CharacterMaster>() : null);
        }

        // Simulacrum's items, which it gives its enemies with or without the Artifact of Evolution
        private void InfiniteTowerWaveController_OnCombatSquadMemberDiscovered(On.RoR2.InfiniteTowerWaveController.orig_OnCombatSquadMemberDiscovered orig, InfiniteTowerWaveController self, CharacterMaster master)
        {
            orig(self, master);
            if (NetworkServer.active)
            {
                RemoveAlienHeads(master);
            }
        }

        private static void RemoveAlienHeads(CharacterMaster characterMaster)
        {
            if(BepConfig.Enabled.Value && (BepConfig.VoidJailerNoCooldownReductions.Value || BepConfig.SolusInvalidatorNoCooldownReductions.Value || BepConfig.VoidlingNoCooldownReductions.Value)) {

                if ((bool)characterMaster && characterMaster.teamIndex == TeamIndex.Monster && characterMaster.bodyPrefab != null)
                {
                    string bodyName = characterMaster.bodyPrefab.name;
                    if (bodyName == "VoidJailerBody" && BepConfig.VoidJailerNoCooldownReductions.Value)
                    {
                        characterMaster.inventory.RemoveItemPermanent(RoR2Content.Items.AlienHead, 10000);
                    }
                    if (bodyName == "DefectiveUnitBody" && BepConfig.SolusInvalidatorNoCooldownReductions.Value)
                    {
                        characterMaster.inventory.RemoveItemPermanent(RoR2Content.Items.AlienHead, 10000);
                    }
                    // the Voidling fight spawns MiniVoidRaidCrabBodyPhase1 to 3
                    if ((bodyName == "VoidRaidCrabBody" || bodyName.StartsWith("MiniVoidRaidCrabBodyPhase")) && BepConfig.VoidlingNoCooldownReductions.Value)
                    {
                        characterMaster.inventory.RemoveItemPermanent(RoR2Content.Items.AlienHead, 10000);
                    }
                }
            }
        }

        private void OnDisable()
        {
            On.RoR2.Run.Start                                                    -= Run_Start;
            On.RoR2.InfiniteTowerRun.OnWaveAllEnemiesDefeatedServer              -= InfiniteTowerRun_OnWaveAllEnemiesDefeatedServer;
            On.RoR2.InfiniteTowerRun.AdvanceWave                                 -= InfiniteTowerRun_AdvanceWave;
            finalMonsterCardsSelectionHook?.Dispose();
            finalMonsterCardsSelectionHook = null;
            On.RoR2.CombatDirector.Awake                                         -= CombatDirector_Awake;

            On.RoR2.Items.RandomlyLunarUtils.CheckForLunarReplacement_UniquePickup_Xoroshiro128Plus -= RandomlyLunarUtils_CheckForLunarReplacement_UniquePickup_Xoroshiro128Plus;
            On.RoR2.Items.RandomlyLunarUtils.CheckForLunarReplacementUniqueArray -= RandomlyLunarUtils_CheckForLunarReplacementUniqueArray;
            On.RoR2.InfiniteTowerWaveController.Initialize                       -= InfiniteTowerWaveController_Initialize;
            On.RoR2.InfiniteTowerWaveController.FixedUpdate                      -= InfiniteTowerWaveController_FixedUpdate;
            On.RoR2.InfiniteTowerWaveController.OnAllEnemiesDefeatedServer       -= InfiniteTowerWaveController_OnAllEnemiesDefeatedServer;
            On.RoR2.SceneDirector.GenerateInteractableCardSelection              -= SceneDirector_GenerateInteractableCardSelection;

            On.RoR2.ChildMonsterController.RegisterTeleport -= ChildMonsterController_RegisterTeleport;
            On.RoR2.Artifacts.MonsterTeamGainsItemsArtifactManager.OnServerCardSpawnedGlobal -= MonsterTeamGainsItemsArtifactManager_OnServerCardSpawnedGlobal;
            On.RoR2.InfiniteTowerWaveController.OnCombatSquadMemberDiscovered    -= InfiniteTowerWaveController_OnCombatSquadMemberDiscovered;

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

        // while none of the wave's enemies is alive, the director earns its credits faster for the next ones
        private void InfiniteTowerWaveController_FixedUpdate(On.RoR2.InfiniteTowerWaveController.orig_FixedUpdate orig, InfiniteTowerWaveController self)
        {
            var speedup = BepConfig.SimulacrumEmptyWaveDirectorSpeedup.Value;
            if (!NetworkServer.active || !BepConfig.Enabled.Value || speedup == 1f || self.combatSquad == null || self.combatSquad.memberCount > 0)
            {
                orig(self);
                return;
            }
            var creditsPerSecond = self.creditsPerSecond;
            self.creditsPerSecond = creditsPerSecond * Math.Max(0f, speedup);
            try
            {
                orig(self);
            }
            finally
            {
                self.creditsPerSecond = creditsPerSecond;
            }
        }

        // The beacons only come back with a new body, as at the start of a stage, and the Captain's client keeps their
        // stock. So a Captain whose beacon is used up, which the synced skill mask tells, is respawned in place.
        private void InfiniteTowerWaveController_OnAllEnemiesDefeatedServer(On.RoR2.InfiniteTowerWaveController.orig_OnAllEnemiesDefeatedServer orig, InfiniteTowerWaveController self)
        {
            orig(self);
            if (!NetworkServer.active || !BepConfig.Enabled.Value || !BepConfig.SimulacrumCaptainBeaconsAfterBossWave.Value || !self.isBossWave)
            {
                return;
            }
            foreach (var pc in PlayerCharacterMasterController.instances)
            {
                var master = pc.master;
                var body = master != null ? master.GetBody() : null;
                if (body != null && body.healthComponent.alive && body.TryGetComponent(out CaptainSupplyDropController supplyDrop) && supplyDrop.netEnabledSkillsMask != 3)
                {
                    master.Respawn(body.transform.position, body.transform.rotation);
                }
            }
        }

        // how much of the interactables broken drones make up, about as much as on a normal stage
        private const float SimulacrumDronesWeight = 14f;

        // Simulacrum's own interactables have no drones, and other mods may remove those of the normal stages, e.g.
        // SimulacrumNormalStages. So they're added to the finished selection, unless it has drones.
        private WeightedSelection<DirectorCard> SceneDirector_GenerateInteractableCardSelection(On.RoR2.SceneDirector.orig_GenerateInteractableCardSelection orig, SceneDirector self)
        {
            var selection = orig(self);
            if (!NetworkServer.active || !BepConfig.Enabled.Value || !BepConfig.SimulacrumAllowDrones.Value || !(Run.instance is InfiniteTowerRun) || selection == null)
            {
                return selection;
            }
            for (int i = 0; i < selection.Count; i++)
            {
                if (selection.GetChoice(i).value?.GetSpawnCard() is InteractableSpawnCard spawnCard && spawnCard.skipSpawnWhenDevotionArtifactEnabled)
                {
                    return selection;
                }
            }
            // the more common the drone, the more often it appears
            var drones = DroneCatalog.allDroneDefs
                .Where(droneDef => droneDef != null && droneDef.droneBrokenSpawnCard != null && droneDef.CheckRequiredExpansionEnabled())
                .Select(droneDef => (droneDef.droneBrokenSpawnCard, weight: droneDef.tier == ItemTier.Tier1 ? 4f : droneDef.tier == ItemTier.Tier2 ? 2f : 1f))
                .ToList();
            var totalWeight = drones.Sum(drone => drone.weight);
            foreach (var (spawnCard, weight) in drones)
            {
                selection.AddChoice(new DirectorCard { spawnCard = spawnCard, selectionWeight = 1 }, SimulacrumDronesWeight * weight / totalWeight);
            }
            return selection;
        }

        private void Run_Start(On.RoR2.Run.orig_Start orig, Run self)
        {
            simulacrumLootTweaks.RunStart();
            ResetOverridePowerBias();
            dissonanceEnabledForWave = false;
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

#if DEBUG
        [ConCommand(commandName = "sst_power_bias", flags = ConVarFlags.ExecuteOnServer, helpText = "Shows the enemy power bias of the Simulacrum and classic directors, or sets both to the given value. Debug builds only.")]
        private static void CCPowerBias(ConCommandArgs args)
        {
            if (args.Count > 0)
            {
                BepConfig.SimulacrumDirectorEnemyPowerBias.Value = args.GetArgFloat(0);
                BepConfig.ClassicDirectorEnemyPowerBias.Value = args.GetArgFloat(0);
            }
            Debug.Log($"SimulacrumDirectorEnemyPowerBias: {BepConfig.SimulacrumDirectorEnemyPowerBias.Value}, ClassicDirectorEnemyPowerBias: {BepConfig.ClassicDirectorEnemyPowerBias.Value}");
        }

        [ConCommand(commandName = "sst_item_credit", flags = ConVarFlags.ExecuteOnServer, helpText = "Gives all players the given Simulacrum item credit, or takes it with a negative value. Debug builds only.")]
        private static void CCItemCredit(ConCommandArgs args)
        {
            args.CheckArgumentCount(1);
            simulacrumLootTweaks.GiveAllPlayersItemCredit(args.GetArgFloat(0));
        }
#endif

        // The power bias decides which monsters the director picks: each card's weight is multiplied by its cost to a
        // power that grows with the bias, 0 at 0.5 (vanilla), so strong monsters come more often towards 1 and weak ones
        // towards 0. The director keeps its own rules for when to spend, so it always spends what it earns.
        private const float PowerBiasExponent = 4f;
        // the reweighted copy of each card selection, made again when the selection or the bias changes
        private static readonly ConditionalWeakTable<WeightedSelection<DirectorCard>, BiasedSelection> biasedSelections = new ConditionalWeakTable<WeightedSelection<DirectorCard>, BiasedSelection>();

        private class BiasedSelection
        {
            public float powerBias;
            public float signature;
            public WeightedSelection<DirectorCard> selection;
        }

        private static float GetPowerBias()
        {
            if (directorEnemyPowerBiasOverride >= 0)
            {
                return directorEnemyPowerBiasOverride;
            }
            return Run.instance != null ? GetCurrentPowerBias() : 0.5f;
        }

        private static Hook finalMonsterCardsSelectionHook;

        private static WeightedSelection<DirectorCard> CombatDirector_get_finalMonsterCardsSelection(Func<CombatDirector, WeightedSelection<DirectorCard>> orig, CombatDirector self)
        {
            var selection = orig(self);
            var powerBias = GetPowerBias();
            if (selection == null || !NetworkServer.active || Mathf.Approximately(powerBias, 0.5f))
            {
                return selection;
            }
            // pools change in place, e.g. by events that filter the monsters, which this notices
            float signature = selection.Count;
            for (int i = 0; i < selection.Count; i++)
            {
                var choice = selection.GetChoice(i);
                signature = signature * 31f + choice.weight * 7f + (choice.value != null ? choice.value.cost : 0);
            }
            if (biasedSelections.TryGetValue(selection, out var biased) && biased.powerBias == powerBias && biased.signature == signature)
            {
                return biased.selection;
            }
            var exponent = PowerBiasExponent * (powerBias - 0.5f);
            var reweighted = new WeightedSelection<DirectorCard>(selection.Count);
            for (int i = 0; i < selection.Count; i++)
            {
                var choice = selection.GetChoice(i);
                var cost = choice.value != null ? Mathf.Max(1, choice.value.cost) : 1;
                reweighted.AddChoice(choice.value, choice.weight * Mathf.Pow(cost, exponent));
            }
            biasedSelections.Remove(selection);
            biasedSelections.Add(selection, new BiasedSelection { powerBias = powerBias, signature = signature, selection = reweighted });
            return reweighted;
        }

        // A director skips monsters too cheap for its credits while its deck has dearer ones, without limit. If none of
        // the dearer ones can be bought, e.g. elites that an event keeps out, it would never spend again.
        private const int MaxConsecutiveCheapSkips = 4;

        private static void CombatDirector_Awake(On.RoR2.CombatDirector.orig_Awake orig, CombatDirector self)
        {
            orig(self);
            if (BepConfig.Enabled.Value)
            {
                self.maxConsecutiveCheapSkips = Math.Min(self.maxConsecutiveCheapSkips, MaxConsecutiveCheapSkips);
            }
        }

        private void InfiniteTowerRun_OnWaveAllEnemiesDefeatedServer(On.RoR2.InfiniteTowerRun.orig_OnWaveAllEnemiesDefeatedServer orig, InfiniteTowerRun self, InfiniteTowerWaveController wc)
        {
            orig(self, wc);
            // only turn off what the tweak turned on, so Dissonance chosen in the lobby stays on
            if (dissonanceEnabledForWave && NetworkServer.active)
            {
                dissonanceEnabledForWave = false;
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

            if (IsCurrentMapCommencment() && !RunArtifactManager.instance.IsArtifactEnabled(RoR2Content.Artifacts.MixEnemy))
            {
                if (BepConfig.SimulacrumCommencementArtifactDissonanceChance.Value > RoR2Application.rng.nextNormalizedFloat)
                {
                    dissonanceEnabledForWave = true;
                    RunArtifactManager.instance.SetArtifactEnabledServer(RoR2Content.Artifacts.MixEnemy, true);
                }
            }
        }
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
            if(pickupDef != null && pickupDef.isLunar && pickupDef.itemIndex != ItemIndex.None)
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
                if (pickupDef != null && pickupDef.isLunar && pickupDef.itemIndex != ItemIndex.None)
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
