using BepInEx;
using MonoMod.Cil;
using R2API;
using R2API.Utils;
using RoR2;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
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
        public const string PluginVersion = "2.0.1";

        private List<PlayerCharacterMasterController> itemGivenTo = new List<PlayerCharacterMasterController>();

        public void Awake()
        {
            PInfo = Info;
            instance = this;
            Log.Init(Logger);
            RoR2.Language.onCurrentLanguageChanged += () =>
            {
                ItemCatalog.availability.CallWhenAvailable(() =>
                {
                    EquipmentCatalog.availability.CallWhenAvailable(() =>
                    {
                        BepConfig.Init();
                    });
                });
            };

        }

        private void OnEnable()
        {
            IL.RoR2.Run.SetupUserCharacterMaster += Run_SetupUserCharacterMaster;
            On.RoR2.Run.OnServerCharacterBodySpawned += Run_OnServerCharacterBodySpawned;
            On.RoR2.Run.Start += Run_Start;
            //RoR2.NetworkUser.onPostNetworkUserStart += NetworkUser_onPostNetworkUserStart;
        }

        private void OnDisable()
        {
            IL.RoR2.Run.SetupUserCharacterMaster -= Run_SetupUserCharacterMaster;
            On.RoR2.Run.OnServerCharacterBodySpawned -= Run_OnServerCharacterBodySpawned;
            On.RoR2.Run.Start -= Run_Start;
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
                if (BepConfig.StartingCash.Value > 0 && BepConfig.Enabled.Value)
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
            });
        }

        private void Run_Start(On.RoR2.Run.orig_Start orig, Run self)
        {
            instance.itemGivenTo.Clear();
            orig(self);
        }

        private void Run_OnServerCharacterBodySpawned(On.RoR2.Run.orig_OnServerCharacterBodySpawned orig, Run self, CharacterBody characterBody)
        {
            orig(self, characterBody);
            if (NetworkServer.active && BepConfig.Enabled.Value)
            {
                var master = characterBody.master;
                if (master != null)
                {
                    if (master.playerCharacterMasterController != null)
                    {
                        if (!instance.itemGivenTo.Contains(master.playerCharacterMasterController))
                        {
                            if (BepConfig.SimpleEnabled.Value)
                                GiveStartingItems(master.inventory);
                            if (BepConfig.AdvancedEnabled.Value)
                                GiveStartingItemsAdvanced(master.inventory);
                            instance.itemGivenTo.Add(master.playerCharacterMasterController);
                        }
                    }
                }
            }
        }


        private void GiveStartingItemsAdvanced(Inventory inventory)
        {
            Dictionary<PickupIndex, int> itemsToGive = new Dictionary<PickupIndex, int>();
            List<PickupIndex> blacklist = new List<PickupIndex>();
            foreach(var blacklistEntry in BepConfig.AdvancedBlackList.Value.Split(','))
            {
                var blacklistPickupIndex = PickupCatalog.FindPickupIndex(blacklistEntry);
                if(blacklistPickupIndex != PickupIndex.none)
                {
                    blacklist.Add(blacklistPickupIndex);
                }
            }
            Helper.ParseItemStringReward(BepConfig.AdvancedItemList.Value, itemsToGive, blacklist);
            foreach (var item in itemsToGive)
            {
                var itemIndex = PickupCatalog.GetPickupDef(item.Key).itemIndex;
                var itemAmount = item.Value;
                if (itemIndex != ItemIndex.None && itemAmount > 0)
                {
                    inventory.GiveItem(itemIndex, itemAmount);
                }
            }
            itemsToGive.Clear();
            Helper.ParseItemStringReward(BepConfig.AdvancedEquipmentList.Value, itemsToGive, blacklist);
            foreach (var item in itemsToGive)
            {
                var equipmentIndex = PickupCatalog.GetPickupDef(item.Key).equipmentIndex;
                var itemAmount = item.Value;
                if (equipmentIndex != EquipmentIndex.None && itemAmount > 0)
                {
                    inventory.SetEquipmentIndex(equipmentIndex);
                    break;
                }
            }
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
            foreach(var (itemTier, configEnglishName) in BepConfig.StartingItemByTier)
            {
                var amount = BepConfig.StartingItemByTierAmount[itemTier].Value;
                if (amount <= 0)
                    continue;
                if (configEnglishName.Value.Equals("Random"))
                {
                    Dictionary<PickupIndex, int> itemsToGive = new Dictionary<PickupIndex, int>();
                    Helper.ResolveItemKey(itemTier.ToString(), amount, itemsToGive);
                    foreach (var item in itemsToGive)
                    {
                        var itemIndex = PickupCatalog.GetPickupDef(item.Key).itemIndex;
                        var itemAmount = item.Value;
                        if (itemIndex != ItemIndex.None && itemAmount > 0)
                        {
                            inventory.GiveItem(itemIndex, itemAmount);
                        }
                    }
                }
                else
                {
                    ItemIndex itemIndex = ItemIndex.None;
                    itemIndex = BepConfig.englishNameToItemIndex[configEnglishName.Value];
                    if (itemIndex != ItemIndex.None)
                    {
                        inventory.GiveItem(itemIndex, amount);
                    }
                }
            }
            EquipmentIndex equipmentIndex = EquipmentIndex.None;
            if(BepConfig.StartingEquipment.Value.Equals("Random"))
            {
                var pickupIndex = Helper.GetRandom(Run.instance.availableEquipmentDropList, PickupIndex.none);
                if(pickupIndex != PickupIndex.none)
                {
                    equipmentIndex = PickupCatalog.GetPickupDef(pickupIndex).equipmentIndex;
                }
            }
            else
            {
                equipmentIndex = BepConfig.englishNameToEquipmentIndex[BepConfig.StartingEquipment.Value];
            }
            if (equipmentIndex != EquipmentIndex.None)
            {
                inventory.SetEquipmentIndex(equipmentIndex);
            }
        }
    }
}
