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
using System.Security.Cryptography;
using System.Collections.Generic;
using static RoR2.SceneCollection;
using Mono.Cecil.Cil;
using static ServerSideBeyondTheLimits.EnumCollection;

namespace ServerSideBeyondTheLimits
{
    [BepInDependency(R2API.R2API.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency("com.KingEnderBrine.InLobbyConfig", BepInDependency.DependencyFlags.SoftDependency)]
    [NetworkCompatibility(CompatibilityLevel.NoNeedForSync, VersionStrictness.DifferentModVersionsAreOk)]
    public class ServerSideBeyondTheLimits : BaseUnityPlugin
    {
        public static PluginInfo PInfo { get; private set; }
        public static ServerSideBeyondTheLimits instance;

        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Def";
        public const string PluginName = "ServerSideBeyondTheLimits";
        public const string PluginVersion = "1.0.0";

        public void Awake()
        {
            PInfo = Info;
            instance = this;
            Log.Init(Logger);
        }
        private void OnEnable()
        {
            On.RoR2.CharacterBody.OnEquipmentGained                              += CharacterBody_OnEquipmentGained;
            On.RoR2.CharacterBody.OnEquipmentLost                                += CharacterBody_OnEquipmentLost;
            IL.RoR2.CharacterBody.RecalculateStats                               += CharacterBody_RecalculateStats;
            IL.RoR2.CharacterBody.UpdateAllTemporaryVisualEffects                += CharacterBody_UpdateAllTemporaryVisualEffects;
            IL.RoR2.Items.SprintWispBodyBehavior.FixedUpdate                     += SprintWispBodyBehavior_FixedUpdate;
            IL.RoR2.MushroomVoidBehavior.FixedUpdate                             += MushroomVoidBehavior_FixedUpdate;
            On.RoR2.Items.RandomlyLunarUtils.CheckForLunarReplacement            += RandomlyLunarUtils_CheckForLunarReplacement;
            On.RoR2.Items.RandomlyLunarUtils.CheckForLunarReplacementUniqueArray += RandomlyLunarUtils_CheckForLunarReplacementUniqueArray;
        }

        private void OnDisable()
        {
            On.RoR2.CharacterBody.OnEquipmentGained                              -= CharacterBody_OnEquipmentGained;
            On.RoR2.CharacterBody.OnEquipmentLost                                -= CharacterBody_OnEquipmentLost;
            IL.RoR2.CharacterBody.RecalculateStats                               -= CharacterBody_RecalculateStats;
            IL.RoR2.CharacterBody.UpdateAllTemporaryVisualEffects                -= CharacterBody_UpdateAllTemporaryVisualEffects;
            IL.RoR2.Items.SprintWispBodyBehavior.FixedUpdate                     -= SprintWispBodyBehavior_FixedUpdate;
            IL.RoR2.MushroomVoidBehavior.FixedUpdate                             -= MushroomVoidBehavior_FixedUpdate;
            On.RoR2.Items.RandomlyLunarUtils.CheckForLunarReplacement            -= RandomlyLunarUtils_CheckForLunarReplacement;
            On.RoR2.Items.RandomlyLunarUtils.CheckForLunarReplacementUniqueArray -= RandomlyLunarUtils_CheckForLunarReplacementUniqueArray;
        }


        private void CharacterBody_OnEquipmentGained(On.RoR2.CharacterBody.orig_OnEquipmentGained orig, CharacterBody self, EquipmentDef equipmentDef)
        {
            if (NetworkServer.active && BepConfig.Enabled.Value && BepConfig.ImplementBeyondTheLimits.Value && equipmentDef?.equipmentIndex == EquipmentCatalog.FindEquipmentIndex("EliteSecretSpeedEquipment"))
            {
                self.AddBuff(DLC1Content.Buffs.KillMoveSpeed);
                self.AddBuff(DLC1Content.Buffs.KillMoveSpeed);
            }
            else
            {
                orig(self, equipmentDef);
            }
        }

        private void CharacterBody_OnEquipmentLost(On.RoR2.CharacterBody.orig_OnEquipmentLost orig, CharacterBody self, EquipmentDef equipmentDef)
        {
            if (NetworkServer.active && BepConfig.Enabled.Value && BepConfig.ImplementBeyondTheLimits.Value && equipmentDef?.equipmentIndex == EquipmentCatalog.FindEquipmentIndex("EliteSecretSpeedEquipment"))
            {
                self.RemoveBuff(DLC1Content.Buffs.KillMoveSpeed);
                self.RemoveBuff(DLC1Content.Buffs.KillMoveSpeed);
            }
            else
            {
                orig(self, equipmentDef);
            }
        }
        private bool isSprinting(CharacterBody characterBody)
        {
            if (NetworkServer.active && BepConfig.Enabled.Value && BepConfig.ImplementBeyondTheLimits.Value && characterBody.inventory?.currentEquipmentIndex == EquipmentCatalog.FindEquipmentIndex("EliteSecretSpeedEquipment"))
            {
                return characterBody.notMovingStopwatch == 0f;
            }
            return characterBody._isSprinting;
        }

        private void CharacterBody_UpdateAllTemporaryVisualEffects(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            var label = c.DefineLabel();
            // if (isSprinting)
            // IL_005a: ldarg.0
            // IL_005b: call instance bool RoR2.CharacterBody::get_isSprinting()
            // IL_0060: brfalse.s IL_007e
            c.GotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchCall<CharacterBody>("get_isSprinting")
            );
            c.Index += 1;
            c.Remove();
            c.EmitDelegate(isSprinting);
            Debug.Log(il.ToString());
        }

        private void MushroomVoidBehavior_FixedUpdate(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            var label = c.DefineLabel();
            // if (body.isSprinting)
            // IL_0010: ldarg.0
            // IL_0011: ldfld class RoR2.CharacterBody RoR2.CharacterBody/ItemBehavior::body
            // IL_0016: callvirt instance bool RoR2.CharacterBody::get_isSprinting()
            // IL_001b: brfalse.s IL_0050
            c.GotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<CharacterBody.ItemBehavior>("body"),
                x => x.MatchCallvirt<CharacterBody>("get_isSprinting")
            );
            c.Index += 2;
            c.Remove();
            c.EmitDelegate(isSprinting);
            Debug.Log(il.ToString());
        }

        private void SprintWispBodyBehavior_FixedUpdate(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            var label = c.DefineLabel();
            // if (base.body.isSprinting)
            // IL_0000: ldarg.0
            // IL_0001: call instance class RoR2.CharacterBody RoR2.Items.BaseItemBodyBehavior::get_body()
            // IL_0006: callvirt instance bool RoR2.CharacterBody::get_isSprinting()
            // IL_000b: brfalse.s IL_0068
            c.GotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchCall<RoR2.Items.BaseItemBodyBehavior>("get_body"),
                x => x.MatchCallvirt<CharacterBody>("get_isSprinting")
            );
            c.Index += 2;
            c.Remove();
            c.EmitDelegate(isSprinting);
            Debug.Log(il.ToString());
        }

        private void CharacterBody_RecalculateStats(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            var label = c.DefineLabel();
            // if (isSprinting && num22 > 0)
            // IL_0fdc: ldarg.0
            // IL_0fdd: call instance bool RoR2.CharacterBody::get_isSprinting()
            // IL_0fe2: brfalse.s IL_0ffc
            // 
            // IL_0fe4: ldloc.s 22
            // IL_0fe6: ldc.i4.0
            // IL_0fe7: ble.s IL_0ffc
            // 
            // armor += num22 * 30;
            // IL_0fe9: ldarg.0
            // IL_0fea: ldarg.0
            // IL_0feb: call instance float32 RoR2.CharacterBody::get_armor()
            // IL_0ff0: ldloc.s 22
            // IL_0ff2: ldc.i4.s 30
            // IL_0ff4: mul
            // IL_0ff5: conv.r4
            // IL_0ff6: add
            c.GotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchCall<CharacterBody>("get_isSprinting"),
                x => x.MatchBrfalse(out _),
                x => x.MatchLdloc(22),
                x => x.MatchLdcI4(0),
                x => x.MatchBle(out _),
                x => x.MatchLdarg(0),
                x => x.MatchLdarg(0),
                x => x.MatchCall<CharacterBody>("get_armor")
            ); ;
            c.Index += 1;
            c.Remove();
            c.EmitDelegate(isSprinting);
            Debug.Log(il.ToString());
        }

        private PickupIndex RandomlyLunarUtils_CheckForLunarReplacement(On.RoR2.Items.RandomlyLunarUtils.orig_CheckForLunarReplacement orig, PickupIndex pickupIndex, Xoroshiro128Plus rng)
        {
            pickupIndex = orig(pickupIndex, rng);
            if (!BepConfig.Enabled.Value)
            {
                return pickupIndex;
            }
            PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
            if (pickupDef.isLunar)
            {
                if (!BepConfig.NoPearlsInBazaar.Value || !SceneNameIsStage(SceneCatalog.currentSceneDef.cachedName, StageEnum.Bazaar))
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
                        List<PickupIndex> listEquip = GetEnabledEliteAspectsList();
                        if (listEquip != null && listEquip.Count > 0)
                        {
                            int index = rng.RangeInt(0, listEquip.Count);
                            pickupIndex = listEquip[index];
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

            List<PickupIndex> listEquip = GetEnabledEliteAspectsList();
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
                            if (!shuffled)
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
    }
}
