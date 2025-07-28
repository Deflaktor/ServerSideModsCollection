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
using static StartBonusMod.EnumCollection;
using UnityEngine.UIElements;

namespace StartBonusMod
{
    [BepInDependency(R2API.R2API.PluginGUID)]
    [BepInDependency("com.KingEnderBrine.InLobbyConfig", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [NetworkCompatibility(CompatibilityLevel.NoNeedForSync, VersionStrictness.DifferentModVersionsAreOk)]
    public class StartBonusMod : BaseUnityPlugin
    {
        public static PluginInfo PInfo { get; private set; }
        public static StartBonusMod instance;

        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Def";
        public const string PluginName = "StartBonusMod";
        public const string PluginVersion = "1.1.0";

        public void Awake()
        {
            PInfo = Info;
            instance = this;
            Log.Init(Logger);
            BepConfig.Init();
        }
        private void OnEnable()
        {
            IL.RoR2.Run.SetupUserCharacterMaster += Run_SetupUserCharacterMaster;
            //RoR2.NetworkUser.onPostNetworkUserStart += NetworkUser_onPostNetworkUserStart;
        }

        private void OnDisable()
        {
            IL.RoR2.Run.SetupUserCharacterMaster -= Run_SetupUserCharacterMaster;
            //RoR2.NetworkUser.onPostNetworkUserStart -= NetworkUser_onPostNetworkUserStart;
        }

        private void Run_SetupUserCharacterMaster(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            c.GotoNext(
                x => x.MatchLdloc(0),
                x => x.MatchLdarg(0),
                x => x.MatchCall<Run>("get_ruleBook"),
                x => x.MatchCallvirt<RuleBook>("get_startingMoney"),
                x => x.MatchCallvirt<CharacterMaster>("GiveMoney")
                );
            c.Index += 4;
            c.Remove();
            c.EmitDelegate((CharacterMaster characterMaster, uint startingMoney) =>
            {
                if (BepConfig.StartingCash.Value > 0)
                {
                    if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.MagnusMagnuson.StartInBazaar"))
                    {
                        characterMaster.GiveMoney((uint)Run.instance.GetDifficultyScaledCost(BepConfig.StartingCash.Value) + startingMoney);
                    }
                    else
                    {
                        characterMaster.GiveMoney((uint)Run.instance.GetDifficultyScaledCost(BepConfig.StartingCash.Value));
                    }
                }
                else
                {
                    characterMaster.GiveMoney(startingMoney);
                }
                GiveStartingItems(characterMaster.inventory);
            });
        }
        //private void NetworkUser_onPostNetworkUserStart(NetworkUser networkUser)
        //{
        //    if (NetworkServer.active && Run.instance != null && networkUser.master == null)
        //    {
        //        networkUser.master.GiveMoney((uint)BepConfig.StartingCash.Value);
        //        GiveStartingItems(networkUser.master.inventory);
        //    }
        //}

        private void GiveStartingItems(Inventory inventory)
        {
            ItemDef itemDef = ToItemDef(BepConfig.StartingItemWhite.Value);
            int count = BepConfig.StartingItemWhiteCount.Value;
            if (itemDef && count > 0)
            {
                inventory.GiveItem(itemDef, count);
            }
            itemDef = ToItemDef(BepConfig.StartingItemGreen.Value);
            count = BepConfig.StartingItemGreenCount.Value;
            if (itemDef && count > 0)
            {
                inventory.GiveItem(itemDef, count);
            }
            itemDef = ToItemDef(BepConfig.StartingItemRed.Value);
            count = BepConfig.StartingItemRedCount.Value;
            if (itemDef && count > 0)
            {
                inventory.GiveItem(itemDef, count);
            }
            itemDef = ToItemDef(BepConfig.StartingItemBoss.Value);
            count = BepConfig.StartingItemBossCount.Value;
            if (itemDef && count > 0)
            {
                inventory.GiveItem(itemDef, count);
            }
            itemDef = ToItemDef(BepConfig.StartingItemLunar.Value);
            count = BepConfig.StartingItemLunarCount.Value;
            if (itemDef && count > 0)
            {
                inventory.GiveItem(itemDef, count);
            }
            itemDef = ToItemDef(BepConfig.StartingItemVoid.Value);
            count = BepConfig.StartingItemVoidCount.Value;
            if (itemDef && count > 0)
            {
                inventory.GiveItem(itemDef, count);
            }
            var equipIndex = ToEquipIndex(BepConfig.StartingItemEquip.Value);
            if (equipIndex != EquipmentIndex.None)
            {
                inventory.SetEquipmentIndex(equipIndex);
            }
        }
    }
}
