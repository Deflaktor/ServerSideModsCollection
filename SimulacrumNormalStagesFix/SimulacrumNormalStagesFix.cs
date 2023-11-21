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
using static SimulacrumNormalStagesFix.EnumCollection;
using System.Security.Cryptography;
using System.Collections.Generic;
using static RoR2.SceneCollection;
using Mono.Cecil.Cil;

namespace SimulacrumNormalStagesFix
{
    [BepInDependency(R2API.R2API.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [NetworkCompatibility(CompatibilityLevel.NoNeedForSync, VersionStrictness.DifferentModVersionsAreOk)]
    public class SimulacrumNormalStagesFix : BaseUnityPlugin
    {
        public static PluginInfo PInfo { get; private set; }
        public static SimulacrumNormalStagesFix instance;

        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Def";
        public const string PluginName = "SimulacrumNormalStagesFix";
        public const string PluginVersion = "1.1.0";

        public void Awake()
        {
            PInfo = Info;
            instance = this;
            Log.Init(Logger);
        }
        private void OnEnable()
        {
            IL.RoR2.ArenaMissionController.OnStartServer      += ArenaMissionController_OnStartServer;
            IL.RoR2.VoidStageMissionController.OnEnable       += VoidStageMissionController_OnEnable;
            IL.RoR2.VoidStageMissionController.Start          += VoidStageMissionController_Start;
            IL.RoR2.InfiniteTowerRun.OnPrePopulateSceneServer += InfiniteTowerRun_OnPrePopulateSceneServer;
            On.RoR2.InfiniteTowerRun.OnPrePopulateSceneServer += InfiniteTowerRun_OnPrePopulateSceneServer;
            On.RoR2.SceneCatalog.OnActiveSceneChanged         += SceneCatalog_OnActiveSceneChanged;
        }

        private void OnDisable()
        {
            IL.RoR2.ArenaMissionController.OnStartServer      -= ArenaMissionController_OnStartServer;
            IL.RoR2.VoidStageMissionController.OnEnable       -= VoidStageMissionController_OnEnable;
            IL.RoR2.VoidStageMissionController.Start          -= VoidStageMissionController_Start;
            IL.RoR2.InfiniteTowerRun.OnPrePopulateSceneServer -= InfiniteTowerRun_OnPrePopulateSceneServer;
            On.RoR2.InfiniteTowerRun.OnPrePopulateSceneServer -= InfiniteTowerRun_OnPrePopulateSceneServer;
            On.RoR2.SceneCatalog.OnActiveSceneChanged         -= SceneCatalog_OnActiveSceneChanged;
        }

        private void SceneCatalog_OnActiveSceneChanged(On.RoR2.SceneCatalog.orig_OnActiveSceneChanged orig, UnityEngine.SceneManagement.Scene oldScene, UnityEngine.SceneManagement.Scene newScene)
        {
            orig(oldScene, newScene);
            if (NetworkServer.active && Run.instance.GetType() == typeof(InfiniteTowerRun) && SceneCatalog.mostRecentSceneDef != null && SceneCatalog.mostRecentSceneDef.baseSceneName == "arena")
            {
                SceneCatalog.mostRecentSceneDef.sceneType = SceneType.Stage;
            }
        }

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
                if (self.nextStageScene.cachedName == GetStageName(StageEnum.Bazaar))
                {
                    self.PerformStageCleanUp();
                    return true;
                }
                return false;
            });
        }
        
        private void InfiniteTowerRun_OnPrePopulateSceneServer(On.RoR2.InfiniteTowerRun.orig_OnPrePopulateSceneServer orig, InfiniteTowerRun self, SceneDirector sceneDirector)
        {
            if (!IsSimulacrumStage(self.nextStageScene.cachedName))
            {
                // consume all spawn points
                for (int i = 0; i < SpawnPoint.readOnlyInstancesList.Count; i++)
                {
                    if (!SpawnPoint.readOnlyInstancesList[i].consumed)
                    {
                        SpawnPoint.readOnlyInstancesList[i].consumed = true;
                    }
                }
                // no starting monsters
                sceneDirector.monsterCredit = 0;
                // disable combat directors
                var combatDirectors = CombatDirector.instancesList.ToArray();
                foreach (var combatDirector in combatDirectors)
                {
                    combatDirector.enabled = false;
                }
            }
            orig(self, sceneDirector);
        }
    }
}
