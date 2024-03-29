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
using static ServerSideTweaks.EnumCollection;
using System.Linq;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UIElements.UIR;
using Mono.Cecil.Cil;
using static RoR2.GenericPickupController;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Collections;
using UnityEngine.SceneManagement;
using System.Security.Cryptography;
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
        public static int totalItemRewardCount = 0;
        public static float directorEnemyPowerBiasOverride = -1;
        public static GameObject mostRecentlyCreatedPickup = null;
        public static Dictionary<NetworkUserId, float> usersItemCredit = new Dictionary<NetworkUserId, float>();
        public static List<EquipmentIndex> disableEquipments = new List<EquipmentIndex>();
        public static List<PickupIndex> availableEquipmentDropList_Saved = new List<PickupIndex>();
        public static List<PickupIndex> availableLunarItemDropList_Saved = new List<PickupIndex>();
        public static List<PickupIndex> availableLunarEquipmentDropList_Saved = new List<PickupIndex>();

        public static StageEnum debug_nextStage = StageEnum.None;

        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Def";
        public const string PluginName = "ServerSideTweaks";
        public const string PluginVersion = "1.1.1";

        public void Awake()
        {
            PInfo = Info;
            instance = this;
            Log.Init(Logger);
            BepConfig.Init();
#if DEBUG
            Logger.LogWarning("You're on a debug build. If you see this after downloading from the thunderstore, panic!");
            //This is so we can connect to ourselves.
            //Instructions:
            //Step One: Assuming this line is in your codebase, start two instances of RoR2 (do this through the .exe directly)
            //Step Two: Host a game with one instance of RoR2.
            //Step Three: On the instance that isn't hosting, open up the console (ctrl + alt + tilde) and enter the command "connect localhost:7777"
            //DO NOT MAKE A MISTAKE SPELLING THE COMMAND OR YOU WILL HAVE TO RESTART THE CLIENT INSTANCE!!
            //Step Four: Test whatever you were going to test.
            On.RoR2.Networking.NetworkManagerSystem.ClientSendAuth += (orig, self, conn) => { };
#endif
        }
        private void OnEnable()
        {
            On.RoR2.Run.Start                                                    += Run_Start;
            On.RoR2.InfiniteTowerRun.OnWaveAllEnemiesDefeatedServer              += InfiniteTowerRun_OnWaveAllEnemiesDefeatedServer;
            On.RoR2.InfiniteTowerRun.AdvanceWave                                 += InfiniteTowerRun_AdvanceWave;
            IL.RoR2.CombatDirector.AttemptSpawnOnTarget                          += CombatDirector_AttemptSpawnOnTarget;
            On.RoR2.Run.FixedUpdate                                              += Run_FixedUpdate;
            IL.RoR2.InfiniteTowerWaveController.DropRewards                      += InfiniteTowerWaveController_DropRewards;
            On.RoR2.InfiniteTowerWaveController.DropRewards                      += InfiniteTowerWaveController_DropRewards1;
            On.RoR2.GenericPickupController.AttemptGrant                         += GenericPickupController_AttemptGrant;
            On.RoR2.GenericPickupController.OnInteractionBegin                   += GenericPickupController_OnInteractionBegin;
            IL.RoR2.GenericPickupController.CreatePickup                         += GenericPickupController_CreatePickup;
            On.RoR2.PickupDropletController.OnCollisionEnter                     += PickupDropletController_OnCollisionEnter;
            On.RoR2.PickupPickerController.OnInteractionBegin                    += PickupPickerController_OnInteractionBegin;
            On.RoR2.PickupPickerController.CreatePickup_PickupIndex              += PickupPickerController_CreatePickup_PickupIndex;
            IL.RoR2.Artifacts.CommandArtifactManager.OnDropletHitGroundServer    += CommandArtifactManager_OnDropletHitGroundServer;
            // On.RoR2.Run.PickNextStageScene                                       += Run_PickNextStageScene;
            On.RoR2.Items.RandomlyLunarUtils.CheckForLunarReplacement            += RandomlyLunarUtils_CheckForLunarReplacement;
            On.RoR2.Items.RandomlyLunarUtils.CheckForLunarReplacementUniqueArray += RandomlyLunarUtils_CheckForLunarReplacementUniqueArray;
            On.RoR2.InfiniteTowerWaveController.Initialize                       += InfiniteTowerWaveController_Initialize;

            if (ModCompatibilityShareSuite.enabled)
            {
                ModCompatibilityShareSuite.AddPickupEventHandler(NonShareableItemCheck);
            }
        }

        private void OnDisable()
        {
            On.RoR2.Run.Start                                                    -= Run_Start;
            On.RoR2.InfiniteTowerRun.OnWaveAllEnemiesDefeatedServer              -= InfiniteTowerRun_OnWaveAllEnemiesDefeatedServer;
            On.RoR2.InfiniteTowerRun.AdvanceWave                                 -= InfiniteTowerRun_AdvanceWave;
            IL.RoR2.CombatDirector.AttemptSpawnOnTarget                          -= CombatDirector_AttemptSpawnOnTarget;
            On.RoR2.Run.FixedUpdate                                              -= Run_FixedUpdate;
            IL.RoR2.InfiniteTowerWaveController.DropRewards                      -= InfiniteTowerWaveController_DropRewards;
            On.RoR2.InfiniteTowerWaveController.DropRewards                      -= InfiniteTowerWaveController_DropRewards1;
            On.RoR2.GenericPickupController.AttemptGrant                         -= GenericPickupController_AttemptGrant;
            On.RoR2.GenericPickupController.OnInteractionBegin                   -= GenericPickupController_OnInteractionBegin;
            IL.RoR2.GenericPickupController.CreatePickup                         -= GenericPickupController_CreatePickup;
            On.RoR2.PickupDropletController.OnCollisionEnter                     -= PickupDropletController_OnCollisionEnter;
            On.RoR2.PickupPickerController.OnInteractionBegin                    -= PickupPickerController_OnInteractionBegin;
            On.RoR2.PickupPickerController.CreatePickup_PickupIndex              -= PickupPickerController_CreatePickup_PickupIndex;
            IL.RoR2.Artifacts.CommandArtifactManager.OnDropletHitGroundServer    -= CommandArtifactManager_OnDropletHitGroundServer;
            // On.RoR2.Run.PickNextStageScene                                       -= Run_PickNextStageScene;
            On.RoR2.Items.RandomlyLunarUtils.CheckForLunarReplacement            -= RandomlyLunarUtils_CheckForLunarReplacement;
            On.RoR2.Items.RandomlyLunarUtils.CheckForLunarReplacementUniqueArray -= RandomlyLunarUtils_CheckForLunarReplacementUniqueArray;
            On.RoR2.InfiniteTowerWaveController.Initialize                       -= InfiniteTowerWaveController_Initialize;

            if (ModCompatibilityShareSuite.enabled)
            {
                ModCompatibilityShareSuite.RemovePickupEventHandler(NonShareableItemCheck);
            }
        }

        private void InfiniteTowerWaveController_Initialize(On.RoR2.InfiniteTowerWaveController.orig_Initialize orig, InfiniteTowerWaveController self, int waveIndex, Inventory enemyInventory, GameObject spawnTarget)
        {
            if (NetworkServer.active && BepConfig.Enabled.Value)
            {
                self.maxSquadSize = BepConfig.SimulacrumMaxSquadSize.Value;
            }
            orig(self, waveIndex, enemyInventory, spawnTarget);
        }

        private bool NonShareableItemCheck(GenericPickupController pickup, CharacterBody picker)
        {
            if(BepConfig.SimulacrumNonSharedLoot.Value)
                return !pickup.TryGetComponent<NonShareableItem>(out _);
            // item shareable
            return true;
        }
        private void Run_Start(On.RoR2.Run.orig_Start orig, Run self)
        {
            totalItemRewardCount = 0;
            mostRecentlyCreatedPickup = null;
            usersItemCredit.Clear();
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
                foreach (PlayerCharacterMasterController pc in PlayerCharacterMasterController.instances)
                {
                    var user = pc.networkUser;
                    if (usersItemCredit.ContainsKey(user.id))
                    {
                        usersItemCredit[user.id] -= 1f;
                    }
                    else
                    {
                        usersItemCredit.Add(user.id, -1f);
                    }
                    Log.LogDebug($"Player pressed F5. "+ user.id+".ItemCredit: " + usersItemCredit[user.id]);
                }
            }
            if (Input.GetKeyDown(KeyCode.F6))
            {
                foreach (PlayerCharacterMasterController pc in PlayerCharacterMasterController.instances)
                {
                    var user = pc.networkUser;
                    if (usersItemCredit.ContainsKey(user.id))
                    {
                        usersItemCredit[user.id] += 1f;
                    }
                    else
                    {
                        usersItemCredit.Add(user.id, +1f);
                    }
                    Log.LogDebug($"Player pressed F5. " + user.id + ".ItemCredit: " + usersItemCredit[user.id]);
                }
            }
            if (Input.GetKeyDown(KeyCode.F12))
            {
                foreach (PlayerCharacterMasterController pc in PlayerCharacterMasterController.instances)
                {
                    debug_nextStage = IncrementEnumValue(debug_nextStage);
                    Log.LogDebug($"Player pressed F12. Next stage: " + debug_nextStage.ToString() + " (" + ((int)debug_nextStage) + ")");
                }
            }
#endif
        }

        private void PickupPickerController_OnInteractionBegin(On.RoR2.PickupPickerController.orig_OnInteractionBegin orig, PickupPickerController self, Interactor activator)
        {
            if (CanInteract(self.gameObject, activator)) 
            {
                orig(self, activator);
            }
        } 
        private void GenericPickupController_OnInteractionBegin(On.RoR2.GenericPickupController.orig_OnInteractionBegin orig, GenericPickupController self, Interactor activator)
        {
            if (CanInteract(self.gameObject, activator))
            {
                orig(self, activator);
            }
        }
        private bool CanInteract(GameObject self, Interactor activator)
        {
            if (!BepConfig.Enabled.Value)
            {
                return true;
            }
            if (self.TryGetComponent<NonShareableItem>(out _))
            {
                var body = activator?.GetComponent<CharacterBody>();
                var user = Util.LookUpBodyNetworkUser(body);
                if (user != null)
                {
                    float credit;
                    usersItemCredit.TryGetValue(user.id, out credit);
                    if (credit >= -BepConfig.SimulacrumLootMaxItemDebt.Value)
                    {
                        return true;
                    }
                    else
                    {
                        ChatHelper.PlayerHasTooManyItems(user.userName);
                        return false;
                    }
                }
            }
            return true;
        }
        private void GenericPickupController_AttemptGrant(On.RoR2.GenericPickupController.orig_AttemptGrant orig, GenericPickupController self, CharacterBody body)
        {
            orig(self, body);
            if (!NetworkServer.active)
            {
                return;
            }
            if (BepConfig.Enabled.Value && self.TryGetComponent<NonShareableItem>(out _))
            {
                if (self.consumed)
                {
                    var user = Util.LookUpBodyNetworkUser(body);
                    if (usersItemCredit.ContainsKey(user.id))
                    {
                        usersItemCredit[user.id] -= 1f;
                    }
                    else
                    {
                        usersItemCredit.Add(user.id, -1f);
                    }
                }
            }
        }
        private void InfiniteTowerWaveController_DropRewards(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            var label = c.DefineLabel();
            // IL_010e: ldloc.s 6
            // IL_0110: ldloc.s 4
            // IL_0112: ldloc.2
            // IL_0113: call void RoR2.PickupDropletController::CreatePickupDroplet(valuetype RoR2.GenericPickupController / CreatePickupInfo, valuetype[UnityEngine.CoreModule]UnityEngine.Vector3, valuetype[UnityEngine.CoreModule]UnityEngine.Vector3)
            c.GotoNext(
            x => x.MatchLdloc(6),
            x => x.MatchLdloc(4),
            x => x.MatchLdloc(2),
            x => x.MatchCall<PickupDropletController>("CreatePickupDroplet")
            );
            c.Index += 3;
            c.Remove();
            c.EmitDelegate<Action<CreatePickupInfo, Vector3, Vector3>>((pickupInfo, position, velocity) =>
            {
                if(BepConfig.Enabled.Value)
                {
                    GameObject obj = Instantiate(PickupDropletController.pickupDropletPrefab, position, Quaternion.identity);
                    PickupDropletController component = obj.GetComponent<PickupDropletController>();
                    if ((bool)component)
                    {
                        component.createPickupInfo = pickupInfo;
                        component.NetworkpickupIndex = pickupInfo.pickupIndex;
                    }
                    Rigidbody component2 = obj.GetComponent<Rigidbody>();
                    component2.velocity = velocity;
                    component2.AddTorque(UnityEngine.Random.Range(150f, 120f) * UnityEngine.Random.onUnitSphere);
                    obj.AddComponent<NonShareableItem>();
                    NetworkServer.Spawn(obj);
                    totalItemRewardCount++;
                }
                else
                {
                    PickupDropletController.CreatePickupDroplet(pickupInfo, position, velocity);
                }
            });
        }

        private void InfiniteTowerWaveController_DropRewards1(On.RoR2.InfiniteTowerWaveController.orig_DropRewards orig, InfiniteTowerWaveController self)
        {
            orig(self);
            if (!NetworkServer.active)
            {
                return;
            }
            if(BepConfig.Enabled.Value)
            {
                // give item credit to every connected player
                int connectedCount = PlayerCharacterMasterController.instances.Where(pc => pc.isConnected).Count();
                foreach (PlayerCharacterMasterController pc in PlayerCharacterMasterController.instances)
                {
                    if (pc.isConnected)
                    {
                        float credit = 0;
                        usersItemCredit.TryGetValue(pc.networkUser.id, out credit);
                        if (usersItemCredit.ContainsKey(pc.networkUser.id))
                        {
                            usersItemCredit[pc.networkUser.id] += (float)totalItemRewardCount / (float)connectedCount;
                        }
                        else
                        {
                            usersItemCredit.Add(pc.networkUser.id, (float)totalItemRewardCount / (float)connectedCount);
                        }
                        Log.LogDebug(pc.networkUser.userName + " itemCredit: " + usersItemCredit[pc.networkUser.id]);
                    }
                }
                totalItemRewardCount = 0;
            }
        }
        private void PickupDropletController_OnCollisionEnter(On.RoR2.PickupDropletController.orig_OnCollisionEnter orig, PickupDropletController self, Collision collision)
        {
            mostRecentlyCreatedPickup = null;
            orig(self, collision);
            if (BepConfig.Enabled.Value && mostRecentlyCreatedPickup != null)
            {
                NonShareableItem component = self.GetComponent<NonShareableItem>();
                if ((bool)component)
                {
                    mostRecentlyCreatedPickup.AddComponent<NonShareableItem>();
                }
            }
        }
        private void PickupPickerController_CreatePickup_PickupIndex(On.RoR2.PickupPickerController.orig_CreatePickup_PickupIndex orig, PickupPickerController self, PickupIndex pickupIndex)
        {
            mostRecentlyCreatedPickup = null;
            orig(self, pickupIndex);
            if (BepConfig.Enabled.Value && mostRecentlyCreatedPickup != null)
            {
                NonShareableItem component = self.GetComponent<NonShareableItem>();
                if ((bool)component)
                {
                    mostRecentlyCreatedPickup.AddComponent<NonShareableItem>();
                }
            }
        }
        private void CommandArtifactManager_OnDropletHitGroundServer(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            var label = c.DefineLabel();
            // IL_0032: ldarg.0
            // IL_0033: ldfld valuetype [UnityEngine.CoreModule]UnityEngine.Vector3 RoR2.GenericPickupController/CreatePickupInfo::position
            // IL_0038: ldarg.0
            // IL_0039: ldfld valuetype [UnityEngine.CoreModule]UnityEngine.Quaternion RoR2.GenericPickupController/CreatePickupInfo::rotation
            // IL_003e: call !!0 [UnityEngine.CoreModule]UnityEngine.Object::Instantiate<class [UnityEngine.CoreModule]UnityEngine.GameObject>(!!0, valuetype [UnityEngine.CoreModule]UnityEngine.Vector3, valuetype [UnityEngine.CoreModule]UnityEngine.Quaternion)
            // IL_0043: dup
            c.GotoNext(
            x => x.MatchLdarg(0),
            x => x.MatchLdfld<CreatePickupInfo>("position"),
            x => x.MatchLdarg(0),
            x => x.MatchLdfld<CreatePickupInfo>("rotation"),
            x => x.MatchCall<UnityEngine.Object>("Instantiate"),
            x => x.MatchDup()
            );
            c.Index += 5;
            c.EmitDelegate<Func<GameObject, GameObject>>((obj) =>
            {
                mostRecentlyCreatedPickup = obj;
                return obj;
            });
        }
        private void GenericPickupController_CreatePickup(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            var label = c.DefineLabel();
            // IL_000f: ldarg.0
            // IL_0010: ldfld valuetype [UnityEngine.CoreModule]UnityEngine.Vector3 RoR2.GenericPickupController/CreatePickupInfo::position
            // IL_0015: ldarg.0
            // IL_0016: ldfld valuetype [UnityEngine.CoreModule]UnityEngine.Quaternion RoR2.GenericPickupController/CreatePickupInfo::rotation
            // IL_001b: call !!0 [UnityEngine.CoreModule]UnityEngine.Object::Instantiate<class [UnityEngine.CoreModule]UnityEngine.GameObject>(!!0, valuetype [UnityEngine.CoreModule]UnityEngine.Vector3, valuetype [UnityEngine.CoreModule]UnityEngine.Quaternion)
            // IL_0020: dup
            c.GotoNext(
            x => x.MatchLdarg(0),
            x => x.MatchLdfld<CreatePickupInfo>("position"),
            x => x.MatchLdarg(0),
            x => x.MatchLdfld<CreatePickupInfo>("rotation"),
            x => x.MatchCall<UnityEngine.Object>("Instantiate"),
            x => x.MatchDup()
            );
            c.Index += 5;
            c.EmitDelegate<Func<GameObject, GameObject>>((obj) =>
            {
                mostRecentlyCreatedPickup = obj;
                return obj;
            });
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
            // IL_022e: ldarg.0
            // IL_022f: ldfld class RoR2.DirectorCard RoR2.CombatDirector::currentMonsterCard    
            // IL_0234: ldfld class RoR2.SpawnCard RoR2.DirectorCard::spawnCard
            // IL_0239: stloc.s 4
            c.GotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<CombatDirector>("currentMonsterCard"),
                x => x.MatchLdfld<DirectorCard>("spawnCard"),
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

            if (SceneNameIsStage(SceneCatalog.currentSceneDef.cachedName, StageEnum.SimulacrumCommencement) || SceneNameIsStage(SceneCatalog.currentSceneDef.cachedName, StageEnum.Commencement))
            {
                RunArtifactManager.instance.SetArtifactEnabledServer(GetArtifactDef(ArtifactEnum.Dissonance), false);
            }
        }
        private void InfiniteTowerRun_AdvanceWave(On.RoR2.InfiniteTowerRun.orig_AdvanceWave orig, InfiniteTowerRun self)
        {
            orig(self);
            if (!BepConfig.Enabled.Value || !NetworkServer.active)
                return;

            if (BepConfig.SimulacrumCommencementArtifactDissonanceChance.Value <= 0f)
                return;

            if (SceneNameIsStage(SceneCatalog.currentSceneDef.cachedName, StageEnum.SimulacrumCommencement) || SceneNameIsStage(SceneCatalog.currentSceneDef.cachedName, StageEnum.Commencement))
            {
                if (BepConfig.SimulacrumCommencementArtifactDissonanceChance.Value > RoR2Application.rng.nextNormalizedFloat)
                {
                    RunArtifactManager.instance.SetArtifactEnabledServer(GetArtifactDef(ArtifactEnum.Dissonance), true);
                }
            }
        }
        #endregion

        private PickupIndex RandomlyLunarUtils_CheckForLunarReplacement(On.RoR2.Items.RandomlyLunarUtils.orig_CheckForLunarReplacement orig, PickupIndex pickupIndex, Xoroshiro128Plus rng)
        {
            pickupIndex = orig(pickupIndex, rng);
            if(!BepConfig.Enabled.Value)
            {
                return pickupIndex;
            }
            PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
            if(pickupDef.isLunar)
            {
                if(!BepConfig.NoPearlsInBazaar.Value || !SceneNameIsStage(SceneCatalog.currentSceneDef.cachedName, StageEnum.Bazaar))
                {
                    float randomNumber = rng.nextNormalizedFloat;
                    if (randomNumber < BepConfig.IrradiantPearlReplacesLunarItemChance.Value)
                    {
                        pickupIndex = PickupCatalog.FindPickupIndex(ItemCatalog.FindItemIndex("ShinyPearl"));
                    } 
                    else if (randomNumber < BepConfig.IrradiantPearlReplacesLunarItemChance.Value + BepConfig.PearlReplacesLunarItemChance.Value)
                    {
                        pickupIndex = PickupCatalog.FindPickupIndex(ItemCatalog.FindItemIndex("Pearl"));
                    }
                    return pickupIndex;
                }
            }
            EquipmentDef equip = EquipmentCatalog.GetEquipmentDef(pickupDef.equipmentIndex);
            if (equip && !pickupDef.isLunar)
            {
                if (SceneNameIsStage(SceneCatalog.currentSceneDef.cachedName, StageEnum.Bazaar))
                {
                    float randomNumber = rng.nextNormalizedFloat;
                    if (randomNumber < BepConfig.BazaarEliteAspectReplacesEquipmentChance.Value)
                    {
                        List<PickupIndex> list = new List<PickupIndex>();
                        string[] eliteAspects = new string[] {
                            "EliteEarthEquipment",
                            "EliteFireEquipment",
                            "EliteIceEquipment",
                            "ElitePoisonEquipment",
                            "EliteLunarEquipment",
                            "EliteLightningEquipment",
                            "EliteHauntedEquipment"
                        };
                        foreach(var eliteAspect in eliteAspects)
                        {
                            var equipIndex = EquipmentCatalog.FindEquipmentIndex(eliteAspect);
                            list.Add(PickupCatalog.FindPickupIndex(equipIndex));
                        }
                        if (list != null && list.Count > 0)
                        {
                            int index = rng.RangeInt(0, list.Count);
                            pickupIndex = list[index];
                        }
                    }
                }
            }
            return pickupIndex;
        }

        private void RandomlyLunarUtils_CheckForLunarReplacementUniqueArray(On.RoR2.Items.RandomlyLunarUtils.orig_CheckForLunarReplacementUniqueArray orig, PickupIndex[] pickupIndices, Xoroshiro128Plus rng)
        {
            orig(pickupIndices, rng);
            if (!BepConfig.Enabled.Value)
                return;

            List<PickupIndex> listEquip = new List<PickupIndex>();
            string[] eliteAspects = new string[] {
                "EliteEarthEquipment",
                "EliteFireEquipment",
                "EliteIceEquipment",
                "ElitePoisonEquipment",
                "EliteLunarEquipment",
                "EliteLightningEquipment",
                "EliteHauntedEquipment"
            };
            foreach (var eliteAspect in eliteAspects)
            {
                var equipIndex = EquipmentCatalog.FindEquipmentIndex(eliteAspect);
                listEquip.Add(PickupCatalog.FindPickupIndex(equipIndex));
            }
            bool shuffled = false;

            for (int i = 0; i < pickupIndices.Length; i++)
            {
                PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupIndices[i]);
                if (pickupDef.isLunar)
                {
                    if (!BepConfig.NoPearlsInBazaar.Value || !SceneNameIsStage(SceneCatalog.currentSceneDef.cachedName, StageEnum.Bazaar))
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
                EquipmentDef equip = EquipmentCatalog.GetEquipmentDef(pickupDef.equipmentIndex);
                if (equip && !pickupDef.isLunar && listEquip != null && listEquip.Count > 0)
                {
                    if (SceneNameIsStage(SceneCatalog.currentSceneDef.cachedName, StageEnum.Bazaar))
                    {
                        float randomNumber = rng.nextNormalizedFloat;
                        if (randomNumber < BepConfig.BazaarEliteAspectReplacesEquipmentChance.Value)
                        {
                            if(!shuffled)
                            {
                                Util.ShuffleList(listEquip, rng);
                                shuffled = true;
                            }
                            pickupIndices[i] = listEquip[i % listEquip.Count];
                        }
                    }
                }
            }
        }

        #region ExtendedMaps
        /*
        private void Run_PickNextStageScene(On.RoR2.Run.orig_PickNextStageScene orig, Run self, WeightedSelection<SceneDef> choices)
        {
            if (BepConfig.Enabled.Value && BepConfig.SimulacrumExtendedMapPool.Value && Run.instance.GetType() == typeof(InfiniteTowerRun))
            {  
                StageEnum[] extendedMapPool = new StageEnum[] {
                    StageEnum.TitanicPlains,
                    StageEnum.DistantRoost,
                    StageEnum.WetlandAspect,
                    StageEnum.AbandonedAqueduct,
                    StageEnum.RallypointDelta,
                    StageEnum.ScorchedAcres,
                    StageEnum.AbyssalDepths,
                    StageEnum.SirensCall,
                    // StageEnum.GildedCoast, need to disable objectives and add crates
                    // StageEnum.MomentFractured, no spawn points
                    // StageEnum.MomentWhole, no spawn points
                    StageEnum.SkyMeadow,
                    // StageEnum.BullwarksAmbry, // do not spawn the central artifact
                    // StageEnum.Commencement, moon2 doesnt work (damage fog, seems to force teleport at start), and no crates
                    StageEnum.SunderedGrove,
                    // StageEnum.Planetarium, voidling fight
                    StageEnum.AphelianSanctuary,
                    StageEnum.SiphonedForest,
                    StageEnum.SulfurPools
                };
                // fix also: no newt altars
#if DEBUG
                if(debug_nextStage != StageEnum.None)
                {
                    extendedMapPool = new StageEnum[] { debug_nextStage };
                }
#endif
                WeightedSelection<SceneDef> weightedSelection = new WeightedSelection<SceneDef>();
                foreach (StageEnum stageEnum in extendedMapPool )
                {
                    if (SceneNameIsStage(SceneCatalog.currentSceneDef.cachedName, stageEnum))
                        continue; // skip so that we do not go back to the same map
                    List<string> scenes = new List<string>(SceneNames[stageEnum]);
                    scenes.Remove("moon2"); // moon2 doesnt seem to work properly
                    foreach (string sceneName in scenes)
                    {
                        weightedSelection.AddChoice(SceneCatalog.FindSceneDef(sceneName), 1f / (float)scenes.Count);
                    }
                }
                orig(self, weightedSelection);
            } else
            {
                orig(self, choices);
            }
        }*/
        #endregion
    }
}
