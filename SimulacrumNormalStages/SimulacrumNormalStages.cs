using BepInEx;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2.Hologram;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UIElements.UIR;
using static RoR2.SceneCollection;
using static SimulacrumNormalStages.EnumCollection;

namespace SimulacrumNormalStages
{
    [BepInDependency(R2API.R2API.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [NetworkCompatibility(CompatibilityLevel.NoNeedForSync, VersionStrictness.DifferentModVersionsAreOk)]
    public class SimulacrumNormalStages : BaseUnityPlugin
    {
        public static PluginInfo PInfo { get; private set; }
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Def";
        public const string PluginName = "SimulacrumNormalStages";
        public const string PluginVersion = "1.0.2";

        public static SimulacrumNormalStages instance;
        public static SceneCollection realityDestinations = ScriptableObject.CreateInstance<SceneCollection>();
        public static List<SceneDef> visitedScenes;
        public static bool shouldAttemptToSpawnShopPortal = false;

        public AsyncOperationHandle<SpawnCard> iscShopPortal;
        //public static SceneDef arena = Addressables.LoadAssetAsync<SceneDef>(key: "RoR2/Base/arena/arena.asset").WaitForCompletion();
        //public static SceneDef voidStage = Addressables.LoadAssetAsync<SceneDef>(key: "RoR2/DLC1/voidstage/voidstage.asset").WaitForCompletion();
        public void Awake()
        {
            PInfo = Info;
            instance = this;
            Log.Init(Logger);
            BepConfig.Init();

            iscShopPortal = Addressables.LoadAssetAsync<SpawnCard>("RoR2/Base/PortalShop/iscShopPortal.asset");
        }
        private void OnEnable()
        {
            IL.RoR2.ArenaMissionController.OnStartServer      += ArenaMissionController_OnStartServer;
            IL.RoR2.VoidStageMissionController.OnEnable       += VoidStageMissionController_OnEnable;
            IL.RoR2.VoidStageMissionController.Start          += VoidStageMissionController_Start;
            IL.RoR2.InfiniteTowerRun.OnPrePopulateSceneServer += InfiniteTowerRun_OnPrePopulateSceneServer;
            // On.RoR2.SceneCatalog.OnActiveSceneChanged         += SceneCatalog_OnActiveSceneChanged;
            IL.RoR2.Run.AdvanceStage                          += Run_AdvanceStage;
            //On.RoR2.Run.AdvanceStage                          += Run_AdvanceStage;
            //On.RoR2.SceneCatalog.GetSceneDefForCurrentScene   += SceneCatalog_GetSceneDefForCurrentScene;
            On.RoR2.PortalStatueBehavior.GrantPortalEntry     += PortalStatueBehavior_GrantPortalEntry;
            On.RoR2.InfiniteTowerRun.OnWaveAllEnemiesDefeatedServer += InfiniteTowerRun_OnWaveAllEnemiesDefeatedServer;

            // stuff from SimulacrumAdditions
            On.RoR2.InfiniteTowerRun.OnPrePopulateSceneServer += RemoveSpawnPointsAndMonsters;
            SceneDirector.onGenerateInteractableCardSelection += AddAndRemoveInteractables;
            On.RoR2.DirectorCard.IsAvailable                  += Allow_Earlier_Spawns;
            On.RoR2.SceneDirector.Start                       += DoReality;
            On.RoR2.Run.PickNextStageScene                    += ChooseRealityStages;
            On.RoR2.Run.Start                                 += Run_Start;
            On.RoR2.Run.OnDisable                             += Run_OnDisable;
            On.RoR2.ArenaMissionController.OnStartServer      += ArenaMissionController_OnStartServer;
            On.RoR2.VoidStageMissionController.Start          += VoidStageMissionController_Start;

            // fix these two not being validForRandomSelection even though they should be
            SceneDef scene = Addressables.LoadAssetAsync<SceneDef>(key: "RoR2/DLC2/habitat/habitat.asset").WaitForCompletion();
            scene.validForRandomSelection = true;
            scene = Addressables.LoadAssetAsync<SceneDef>(key: "RoR2/DLC2/helminthroost/helminthroost.asset").WaitForCompletion();
            scene.validForRandomSelection = true;
        }

        private void OnDisable()
        {
            IL.RoR2.ArenaMissionController.OnStartServer      -= ArenaMissionController_OnStartServer;
            IL.RoR2.VoidStageMissionController.OnEnable       -= VoidStageMissionController_OnEnable;
            IL.RoR2.VoidStageMissionController.Start          -= VoidStageMissionController_Start;
            IL.RoR2.InfiniteTowerRun.OnPrePopulateSceneServer -= InfiniteTowerRun_OnPrePopulateSceneServer;
            // On.RoR2.SceneCatalog.OnActiveSceneChanged         -= SceneCatalog_OnActiveSceneChanged;
            IL.RoR2.Run.AdvanceStage                          -= Run_AdvanceStage;
            //On.RoR2.Run.AdvanceStage                          -= Run_AdvanceStage;
            //On.RoR2.SceneCatalog.GetSceneDefForCurrentScene   -= SceneCatalog_GetSceneDefForCurrentScene;
            On.RoR2.PortalStatueBehavior.GrantPortalEntry     -= PortalStatueBehavior_GrantPortalEntry;
            On.RoR2.InfiniteTowerRun.OnWaveAllEnemiesDefeatedServer -= InfiniteTowerRun_OnWaveAllEnemiesDefeatedServer;

            // stuff from SimulacrumAdditions
            if (Run.instance)
            {
                RuleDef StageOrderRule = RuleCatalog.FindRuleDef("Misc.StageOrder");
                Run.instance.ruleBook.GetRuleChoice(StageOrderRule).extraData = StageOrder.Normal;
            }
            On.RoR2.InfiniteTowerRun.OnPrePopulateSceneServer -= RemoveSpawnPointsAndMonsters;
            SceneDirector.onGenerateInteractableCardSelection -= AddAndRemoveInteractables;
            On.RoR2.DirectorCard.IsAvailable                  -= Allow_Earlier_Spawns;
            On.RoR2.SceneDirector.Start                       -= DoReality;
            On.RoR2.Run.PickNextStageScene                    -= ChooseRealityStages;
            On.RoR2.Run.Start                                 -= Run_Start;
            On.RoR2.Run.OnDisable                             -= Run_OnDisable;
            On.RoR2.ArenaMissionController.OnStartServer      -= ArenaMissionController_OnStartServer;
            On.RoR2.VoidStageMissionController.Start          -= VoidStageMissionController_Start;
        }

        private void InfiniteTowerRun_OnWaveAllEnemiesDefeatedServer(On.RoR2.InfiniteTowerRun.orig_OnWaveAllEnemiesDefeatedServer orig, InfiniteTowerRun self, InfiniteTowerWaveController wc)
        {
            orig(self, wc);
            if (self.IsStageTransitionWave() && shouldAttemptToSpawnShopPortal && self.safeWardController != null)
            {
                var portal = DirectorCore.instance.TrySpawnObject(new DirectorSpawnRequest(iscShopPortal.WaitForCompletion(), new DirectorPlacementRule
                {
                    minDistance = 0f,
                    maxDistance = 30f,
                    placementMode = DirectorPlacementRule.PlacementMode.Approximate,
                    position = self.safeWardController.transform.position,
                    spawnOnTarget = self.safeWardController.transform
                }, self.safeWardRng));
                if (portal != null)
                {
                    Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                    {
                        baseToken = "PORTAL_SHOP_OPEN"
                    });
                }
            }
            shouldAttemptToSpawnShopPortal = false;
        }

        private void PortalStatueBehavior_GrantPortalEntry(On.RoR2.PortalStatueBehavior.orig_GrantPortalEntry orig, PortalStatueBehavior self) 
        {
            if(self.portalType == PortalStatueBehavior.PortalType.Shop)
            {
                shouldAttemptToSpawnShopPortal = true;
            }
            orig(self);
        }

        /*
        private void Run_AdvanceStage(On.RoR2.Run.orig_AdvanceStage orig, Run self, SceneDef nextScene)
        {
            currentlyInsideRunAdvanceStageMethod = true;
            orig(self, nextScene);
            currentlyInsideRunAdvanceStageMethod = false;


            if (voidFieldsOriginalStageType != SceneType.Invalid)
            {
                // restore the scene type of void fields in normal runs
                SceneCatalog.mostRecentSceneDef.sceneType = voidFieldsOriginalStageType;
                SceneCatalog.mostRecentSceneDef.preventStageAdvanceCounter = voidFieldsOriginalPreventStageAdvanceCounter;
            }
        }

        private SceneDef SceneCatalog_GetSceneDefForCurrentScene(On.RoR2.SceneCatalog.orig_GetSceneDefForCurrentScene orig)
        {
            SceneDef sceneDef = orig();
            if (currentlyInsideRunAdvanceStageMethod)
            {
                if (NetworkServer.active && Run.instance != null && sceneDef.baseSceneName == "arena")
                {
                    if (Run.instance.GetType() == typeof(InfiniteTowerRun))
                    {
                        if (voidFieldsOriginalStageType == SceneType.Invalid)
                        {
                            voidFieldsOriginalStageType = sceneDef.sceneType;
                            voidFieldsOriginalPreventStageAdvanceCounter = sceneDef.preventStageAdvanceCounter;
                        }
                        // make void fields count as stage in Simulacrum Mode
                        sceneDef.sceneType = SceneType.Stage;
                        sceneDef.preventStageAdvanceCounter = false;
                    }
                }
            }
            return sceneDef;
        }*/
        private void Run_AdvanceStage(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            var label = c.DefineLabel();
            // IL_0023: ldloc.0
            // IL_0024: ldfld valuetype RoR2.SceneType RoR2.SceneDef::sceneType
            // IL_0029: ldc.i4.1
            // IL_002a: beq.s IL_0035
            c.GotoNext(
            x => x.MatchLdloc(0),
            x => x.MatchLdfld<SceneDef>("sceneType"),
            x => x.MatchLdcI4(1)
            // x => x.MatchBeq()
            );
            c.EmitDelegate<Func<bool>>(() =>
            {
                SceneDef sceneDefForCurrentScene = SceneCatalog.GetSceneDefForCurrentScene();
                return sceneDefForCurrentScene.baseSceneName == "arena" && Run.instance.GetType() == typeof(InfiniteTowerRun);
            });
            c.Emit(OpCodes.Brtrue, label);
            c.Index += 2;
            // IL_003d: ldarg.0
            // IL_003e: ldarg.0
            // IL_003f: ldfld int32 RoR2.Run::stageClearCount
            c.GotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<Run>("stageClearCount")
            );
            c.MarkLabel(label);
        }
        /*
        private void SceneCatalog_OnActiveSceneChanged(On.RoR2.SceneCatalog.orig_OnActiveSceneChanged orig, UnityEngine.SceneManagement.Scene oldScene, UnityEngine.SceneManagement.Scene newScene)
        {
            orig(oldScene, newScene);
            if (NetworkServer.active && Run.instance != null && SceneCatalog.mostRecentSceneDef != null && SceneCatalog.mostRecentSceneDef.baseSceneName == "arena")
            {
                if (Run.instance.GetType() == typeof(InfiniteTowerRun))
                {
                    if(voidFieldsOriginalStageType == SceneType.Invalid) { 
                        voidFieldsOriginalStageType = SceneCatalog.mostRecentSceneDef.sceneType;
                        voidFieldsOriginalPreventStageAdvanceCounter = SceneCatalog.mostRecentSceneDef.preventStageAdvanceCounter;
                    }
                    // make void fields count as stage in Simulacrum Mode
                    SceneCatalog.mostRecentSceneDef.sceneType = SceneType.Stage;
                    // SceneCatalog.mostRecentSceneDef.preventStageAdvanceCounter = false;
                }
                else
                {
                    if (voidFieldsOriginalStageType != SceneType.Invalid)
                    {
                        // restore the scene type of void fields in normal runs
                        SceneCatalog.mostRecentSceneDef.sceneType = voidFieldsOriginalStageType;
                        SceneCatalog.mostRecentSceneDef.preventStageAdvanceCounter = voidFieldsOriginalPreventStageAdvanceCounter;
                    }
                }
            }
        }*/

        private void ReturnImmediately<T>(ILContext il, Func<T, bool> funcWhetherToReturnImmediately)
        {
            var methodContinue = il.DefineLabel();
            ILCursor c = new ILCursor(il);
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(funcWhetherToReturnImmediately);
            c.Emit(OpCodes.Brfalse, methodContinue);
            c.Emit(OpCodes.Ret);
            c.MarkLabel(methodContinue);
        }
        private void ArenaMissionController_OnStartServer(ILContext il)
        {
            ReturnImmediately(il, (ArenaMissionController self) =>
            {
                if (Run.instance.GetType() == typeof(InfiniteTowerRun))
                {
                    // remove the portal in void locus
                    GameObject obj = GameObject.Find("PortalArena");
                    if (obj != null)
                    {
                        Destroy(obj);
                    }
                    // remove the null wards in void locus
                    foreach (GameObject obj2 in self.nullWards)
                    {
                        Destroy(obj2);
                    }
                    self.nullWards = new GameObject[0];
                    self.enabled = false;
                    return true;
                }
                return false;
            });
        }
        private void VoidStageMissionController_OnEnable(ILContext il)
        {
            ReturnImmediately(il, (VoidStageMissionController self) =>
            {
                if (Run.instance.GetType() == typeof(InfiniteTowerRun))
                {
                    self.enabled = false;
                    return true;
                }
                return false;
            });
        }

        private void VoidStageMissionController_Start(ILContext il)
        {
            ReturnImmediately(il, (VoidStageMissionController self) =>
            {
                if (Run.instance.GetType() == typeof(InfiniteTowerRun))
                {
                    if ((bool)self.deepVoidPortalObjectiveProvider)
                    {
                        // disable objective
                        self.deepVoidPortalObjectiveProvider.enabled = false;
                    }
                    // return immediately to avoid spawning batteries
                    return true;
                }
                return false;
            });
        }

        private void InfiniteTowerRun_OnPrePopulateSceneServer(ILContext il)
        {
            ReturnImmediately(il, (InfiniteTowerRun self) =>
            {
                SceneDef sceneDefForCurrentScene = SceneCatalog.GetSceneDefForCurrentScene();
                if (sceneDefForCurrentScene.baseSceneName == GetStageName(StageEnum.Bazaar))
                {
                    self.PerformStageCleanUp();
                    return true;
                }
                return false;
            });
        }

        // --------------------------------- Stuff from Simulacrum Additions courtesy of Wolfo --------------------------------------------------

        private static void ArenaMissionController_OnStartServer(On.RoR2.ArenaMissionController.orig_OnStartServer orig, ArenaMissionController self)
        {
            if (Run.instance.GetType() == typeof(InfiniteTowerRun))
            {
                self.gameObject.SetActive(false);
                GameObject PortalArena = GameObject.Find("/PortalArena");
                if (PortalArena)
                {
                    PortalArena.transform.GetChild(0).gameObject.SetActive(false);
                }
                return;
            }
            orig(self);
        }

        public static void DoDestinations()
        {
            List<SceneCollection.SceneEntry> newSceneEntry = new List<SceneCollection.SceneEntry>();
            for (int i = 0; i < SceneCatalog.allStageSceneDefs.Length; i++)
            {
                SceneDef def = SceneCatalog.allStageSceneDefs[i];
                if (def.validForRandomSelection && def.hasAnyDestinations)
                {
                    float weight = 1;
                    switch (def.cachedName)
                    {
                        case "blackbeach":
                        case "blackbeach2":
                        case "golemplains":
                        case "golemplains2":
                        case "habitat":
                        case "habitatfall":
                        case "lakes":
                        case "lakesnight":
                        case "village":
                        case "villagenight":
                            weight = 0.5f;
                            break;
                    }
                    SceneCollection.SceneEntry newEntry = new SceneCollection.SceneEntry() { sceneDef = def, weight = weight };
                    newSceneEntry.Add(newEntry);
                }

            }
            realityDestinations._sceneEntries = newSceneEntry.ToArray();
        }

        private static void Run_Start(On.RoR2.Run.orig_Start orig, Run self)
        {
            if(Run.instance.GetType() == typeof(InfiniteTowerRun) && BepConfig.Enabled.Value) {
                RuleDef StageOrderRule = RuleCatalog.FindRuleDef("Misc.StageOrder");
                self.ruleBook.GetRuleChoice(StageOrderRule).extraData = StageOrder.Random;
                visitedScenes = new List<SceneDef>();
                DoDestinations();
                shouldAttemptToSpawnShopPortal = false;
            }
            orig(self);
        }

        private static void DoReality(On.RoR2.SceneDirector.orig_Start orig, SceneDirector self)
        {
            orig(self);

            if (!BepConfig.Enabled.Value || Run.instance.GetType() != typeof(InfiniteTowerRun))
                return;

            CombatDirector[] combatDirector = self.gameObject.GetComponents<CombatDirector>();
            for (int i = 0; i < combatDirector.Length; i++)
            {
                Debug.Log(combatDirector[i]);
                combatDirector[i].enabled = false;
            }

            PortalStatueBehavior[] newtList2 = UnityEngine.Object.FindObjectsOfType(typeof(PortalStatueBehavior)) as PortalStatueBehavior[];

            if (newtList2.Length > 0)
            {
                if (BepConfig.PlaceVoidEradictors.Value && !BepConfig.PlaceNewtAltars.Value)
                {
                    GameObject SupressorObject = LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/VoidSuppressor");
                    Transform MainTransform = newtList2[0].gameObject.transform.parent;
                    for (int i = 0; i < MainTransform.childCount; i++)
                    {
                        Transform NewtAltar = MainTransform.GetChild(i);
                        if (NewtAltar.GetComponent<PortalStatueBehavior>())
                        {
                            Debug.Log(NewtAltar);
                            NewtAltar.gameObject.SetActive(true);
                            if (NetworkServer.active)
                            {
                                GameObject VoidSuppressor = GameObject.Instantiate(SupressorObject, NewtAltar);
                                VoidSuppressor.transform.localPosition = new Vector3(0, -1.38f, 0);
                                PurchaseInteraction purch = VoidSuppressor.GetComponent<PurchaseInteraction>();
                                purch.costType = CostTypeIndex.None;
                                purch.cost = 0;
                                NetworkServer.Spawn(VoidSuppressor);
                                NewtAltar.GetChild(0).gameObject.SetActive(false);
                            }
                            GameObject.Destroy(NewtAltar.GetComponent<PurchaseInteraction>());
                            GameObject.Destroy(NewtAltar.GetComponent<HologramProjector>());
                            GameObject.Destroy(NewtAltar.GetComponent<PortalStatueBehavior>());
                        }
                    }
                }
                else if(!BepConfig.PlaceNewtAltars.Value)
                {
                    Transform MainTransform = newtList2[0].gameObject.transform.parent;
                    for (int i = 0; i < MainTransform.childCount; i++)
                    {
                        Transform NewtAltar = MainTransform.GetChild(i);
                        NetworkServer.Destroy(NewtAltar.gameObject);
                    }
                }
            }
            if (NetworkServer.active)
            {
                Run.instance.PickNextStageScene(null);
            }
        }

        private static void VoidStageMissionController_Start(On.RoR2.VoidStageMissionController.orig_Start orig, VoidStageMissionController self)
        {
            if (Run.instance.GetType() == typeof(InfiniteTowerRun))
            {
                self.batteryCount = 0;
            }
            orig(self);
        }

        private static void Run_OnDisable(On.RoR2.Run.orig_OnDisable orig, Run self)
        {
            RuleDef StageOrderRule = RuleCatalog.FindRuleDef("Misc.StageOrder");
            Run.instance.ruleBook.GetRuleChoice(StageOrderRule).extraData = StageOrder.Normal;
            orig(self);
        }

        private static void ChooseRealityStages(On.RoR2.Run.orig_PickNextStageScene orig, Run self, WeightedSelection<SceneDef> choices)
        {
            if (Run.instance.GetType() == typeof(InfiniteTowerRun) && BepConfig.Enabled.Value && self.ruleBook.stageOrder == StageOrder.Random)
            {
                if (visitedScenes.Count > 10)
                {
                    visitedScenes.Clear();
                    DoDestinations();
                }
                if (realityDestinations.sceneEntries.Length == 0)
                {
                    DoDestinations();
                }
                if (realityDestinations.sceneEntries.Length > 0)
                {
                    //
                    WeightedSelection<SceneDef> weightedSelection = new WeightedSelection<SceneDef>(24);
                    realityDestinations.AddToWeightedSelection(weightedSelection, new System.Func<SceneDef, bool>(self.CanPickStage));

                    //0 00-10, 1 10-20, 2 20-30

                    self.nextStageScene = weightedSelection.Evaluate(self.nextStageRng.nextNormalizedFloat);
                    visitedScenes.Add(self.nextStageScene);

                    for (int i = 0; i < realityDestinations._sceneEntries.Length; i++)
                    {
                        if (realityDestinations._sceneEntries[i].sceneDef == self.nextStageScene)
                        {
                            realityDestinations._sceneEntries[i].weight = 0;
                        }
                    }
                }
                else
                {
                    SceneDef[] array = SceneCatalog.allStageSceneDefs.Where(new System.Func<SceneDef, bool>(RealityValidSceneDefs)).ToArray<SceneDef>();
                    self.nextStageScene = self.nextStageRng.NextElementUniform<SceneDef>(array);
                }
                return;
            }
            orig(self, choices);
        }

        private static bool RealityValidSceneDefs(SceneDef sceneDef)
        {
            if (visitedScenes.Contains(sceneDef))
            {
                return false;
            }
            return sceneDef.hasAnyDestinations && sceneDef.validForRandomSelection;
        }

        private static bool Allow_Earlier_Spawns(On.RoR2.DirectorCard.orig_IsAvailable orig, DirectorCard self)
        {
            if(BepConfig.Enabled.Value && Run.instance?.GetType() == typeof(InfiniteTowerRun))
            {
                if (self.minimumStageCompletions > 2)
                {
                    self.minimumStageCompletions = 2;
                }
            }
            return orig(self);
        }

        private static void RemoveSpawnPointsAndMonsters(On.RoR2.InfiniteTowerRun.orig_OnPrePopulateSceneServer orig, InfiniteTowerRun self, SceneDirector sceneDirector)
        {
            if (Run.instance.GetType() == typeof(InfiniteTowerRun) && BepConfig.Enabled.Value)
            {
                sceneDirector.teleporterSpawnCard = null;
                sceneDirector.monsterCredit = 0;
                sceneDirector.RemoveAllExistingSpawnPoints();
            }
            orig(self, sceneDirector);
        }

        private static void AddAndRemoveInteractables(SceneDirector arg1, DirectorCardCategorySelection dccs)
        {
            if (Run.instance.GetType() == typeof(InfiniteTowerRun) && BepConfig.Enabled.Value)
            {
                int voidIndex = dccs.FindCategoryIndexByName("Void Stuff");
                if (voidIndex != -1)
                {
                    dccs.categories[voidIndex].selectionWeight *= 2 + 1f;

                    DirectorCard iscVoidChestSacrificeOn = new DirectorCard
                    {
                        spawnCard = Addressables.LoadAssetAsync<SpawnCard>(key: "5448ccdc4b91bd244a1631d60d24298d").WaitForCompletion(),
                        selectionWeight = 1,
                    };
                    DirectorCard iscVoidTriple = new DirectorCard
                    {
                        spawnCard = Addressables.LoadAssetAsync<SpawnCard>(key: "RoR2/DLC1/VoidTriple/iscVoidTriple.asset").WaitForCompletion(),
                        selectionWeight = 15,
                    };
                    DirectorCard iscVoidSuppressorIT = new DirectorCard
                    {
                        spawnCard = Addressables.LoadAssetAsync<SpawnCard>(key: "RoR2/DLC1/VoidSuppressor/iscVoidSuppressor.asset").WaitForCompletion(),
                        selectionWeight = 15,
                    };
                    DirectorCard iscVoidCoinBarrelITSacrifice = new DirectorCard
                    {
                        spawnCard = Addressables.LoadAssetAsync<SpawnCard>(key: "RoR2/DLC1/VoidCoinBarrel/iscVoidCoinBarrel.asset").WaitForCompletion(),
                        selectionWeight = 6,
                    };
                    dccs.AddCard(voidIndex, iscVoidCoinBarrelITSacrifice);
                    dccs.AddCard(voidIndex, iscVoidChestSacrificeOn);
                    if (BepConfig.PlaceVoidEradictors.Value)
                        dccs.AddCard(voidIndex, iscVoidSuppressorIT);
                    dccs.AddCard(voidIndex, iscVoidTriple);
                }
                dccs.RemoveCardsThatFailFilter(trimmer);
            }
        }

        public static System.Predicate<DirectorCard> trimmer = new System.Predicate<DirectorCard>(SimulacrumTrimmer);
        public static bool SimulacrumTrimmer(DirectorCard card)
        {
            GameObject prefab = card.spawnCard.prefab;
            if ((card.spawnCard as InteractableSpawnCard).skipSpawnWhenDevotionArtifactEnabled && !RunArtifactManager.instance.IsArtifactEnabled(CU8Content.Artifacts.Devotion))
            {
                //Skip Drones only if Devo not active
                return false;
            }
            return !(prefab.GetComponent<ShrineCombatBehavior>() | prefab.GetComponent<HalcyoniteShrineInteractable>() | prefab.GetComponent<OutsideInteractableLocker>() | prefab.GetComponent<ShrineBossBehavior>() | prefab.GetComponent<SeerStationController>() | prefab.GetComponent<PortalStatueBehavior>());
        }
    }
}
