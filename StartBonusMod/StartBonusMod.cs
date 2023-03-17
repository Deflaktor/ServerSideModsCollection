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
        public const string PluginAuthor = "Deflaktor";
        public const string PluginName = "StartBonusMod";
        public const string PluginVersion = "1.0.0";

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
        }

        private void OnDisable()
        {
            IL.RoR2.Run.SetupUserCharacterMaster -= Run_SetupUserCharacterMaster;
        }
        private void Run_SetupUserCharacterMaster(ILContext il)
        {
            // TODO: Fix Normal Difficulty
            ILCursor c = new ILCursor(il);
            c.GotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchCall<Run>("get_ruleBook"),
                x => x.MatchCallvirt<RuleBook>("get_startingMoney"),
                x => x.MatchCallvirt<CharacterMaster>("GiveMoney")
                );
            c.Index += 2;
            c.Remove();
            c.EmitDelegate<Func<RuleBook, uint>>((ruleBook) =>
            {
                if (BepConfig.StartingCash.Value >= 0)
                {
                    return (uint)BepConfig.StartingCash.Value + ruleBook.startingMoney;
                }
                else
                {
                    return ruleBook.startingMoney;
                }
            });
            c.GotoNext(
                x => x.MatchLdloc(0),
                x => x.MatchCallvirt<CharacterMaster>("get_inventory"),
                x => x.MatchLdsfld("RoR2.RoR2Content/Items", "DrizzlePlayerHelper"),
                x => x.MatchLdcI4(1),
                x => x.MatchCallvirt<Inventory>("GiveItem")
                );
            c.Index += 4;
            c.Remove();
            c.EmitDelegate(GiveStartingItems);
            c.GotoNext(
                x => x.MatchLdloc(0),
                x => x.MatchCallvirt<CharacterMaster>("get_inventory"),
                x => x.MatchLdsfld("RoR2.RoR2Content/Items", "MonsoonPlayerHelper"),
                x => x.MatchLdcI4(1),
                x => x.MatchCallvirt<Inventory>("GiveItem")
                );
            c.Index += 4;
            c.Remove();
            c.EmitDelegate(GiveStartingItems);
        }

        private void GiveStartingItems(Inventory inventory, ItemDef itemDef, Int32 count)
        {
            inventory.GiveItem(itemDef, count);

            itemDef = ToItemDef(BepConfig.StartingItemWhite.Value);
            count = BepConfig.StartingItemWhiteCount.Value;
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
