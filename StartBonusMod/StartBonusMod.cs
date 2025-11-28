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
using ItemStringParser;
using RoR2BepInExPack.GameAssetPaths;
using RoR2.EntitlementManagement;

namespace StartBonusMod
{
    [BepInDependency(R2API.R2API.PluginGUID)]
    [BepInDependency("com.KingEnderBrine.InLobbyConfig", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(ItemStringParser.ItemStringParser.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [NetworkCompatibility(CompatibilityLevel.NoNeedForSync, VersionStrictness.DifferentModVersionsAreOk)]
    public class StartBonusMod : BaseUnityPlugin
    {
        public static PluginInfo PInfo { get; private set; }
        public static StartBonusMod instance;

        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Def";
        public const string PluginName = "StartBonusMod";
        public const string PluginVersion = "3.0.3";

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
                        StartCoroutine(DelayInitBepConfig());
                        
                    });
                });
            };
        }
        IEnumerator DelayInitBepConfig()
        {
            yield return new WaitForSeconds(2.0f);
            BepConfig.Init();
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
                if (characterMaster != null)
                {
                    if (BepConfig.Enabled.Value && BepConfig.StartingCashEnabled.Value)
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
            if (NetworkServer.active && BepConfig.Enabled.Value && characterBody != null)
            {
                var master = characterBody.master;
                if (master != null)
                {
                    if (master.playerCharacterMasterController != null)
                    {
                        if (!instance.itemGivenTo.Contains(master.playerCharacterMasterController))
                        {
                            if (BepConfig.SimpleEnabled.Value)
                                GiveStartingItemsSimple(master.inventory);
                            if (BepConfig.AdvancedEnabled.Value)
                                GiveStartingItemsAdvanced(master);
                            instance.itemGivenTo.Add(master.playerCharacterMasterController);
                        }
                    }
                }
            }
        }


        private void GiveStartingItemsAdvanced(CharacterMaster master)
        {
            var inventory = master.inventory;
            Dictionary<PickupIndex, int> itemsToGive = new Dictionary<PickupIndex, int>();
            ItemStringParser.ItemStringParser.ParseItemString(BepConfig.AdvancedItemList.Value, itemsToGive, instance.Logger, false);
            uint equipIndex = 0;
            foreach (var (pickupIndex, itemAmount) in itemsToGive)
            {
                var pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
                // handle items
                var itemIndex = pickupDef.itemIndex;
                var equipmentIndex = pickupDef.equipmentIndex;
                if (itemIndex != ItemIndex.None && itemAmount > 0)
                {
                    inventory.GiveItemPermanent(itemIndex, itemAmount);
                }
                // handle equipments
                int maxEquipmentSlots = master.bodyPrefab.name == "ToolbotBody" ? 2 : 1;
                int maxEquipmentSets = master.inventory.GetItemCountEffective(DLC3Content.Items.ExtraEquipment.itemIndex) + 1;
                int maxEquipmentCount = maxEquipmentSlots * maxEquipmentSets;
                var equipmentCount = itemAmount;
                while (equipmentIndex != EquipmentIndex.None && equipmentCount > 0 && equipIndex < maxEquipmentCount)
                {
                    uint slot = (uint) (equipIndex % maxEquipmentSlots);
                    uint set = (uint) (equipIndex / maxEquipmentSlots);
                    inventory.SetEquipmentIndexForSlot(equipmentIndex, slot, set);
                    equipmentCount--;
                    equipIndex++;
                }
            }
        }

        private void GiveStartingItemsSimple(Inventory inventory)
        {
            foreach(var (itemTier, configEnglishName) in BepConfig.StartingItemByTier)
            {
                var amount = BepConfig.StartingItemByTierAmount[itemTier].Value;
                if (amount <= 0)
                    continue;
                if (configEnglishName.Value.Equals("Random"))
                {
                    var pickupIndex = ItemStringParser.ItemStringParser.ResolveItemKey(itemTier.ToString());
                    var itemIndex = PickupCatalog.GetPickupDef(pickupIndex).itemIndex;
                    if (itemIndex != ItemIndex.None)
                    {
                        inventory.GiveItemPermanent(itemIndex, amount);
                    }
                }
                else
                {
                    ItemIndex itemIndex = ItemIndex.None;
                    itemIndex = BepConfig.englishNameToItemIndex[configEnglishName.Value];
                    if (itemIndex != ItemIndex.None)
                    {
                        inventory.GiveItemPermanent(itemIndex, amount);
                    }
                }
            }
            EquipmentIndex equipmentIndex = EquipmentIndex.None;
            if(BepConfig.StartingEquipment.Value.Equals("Random"))
            {
                var pickupIndex = ItemStringParser.ItemStringParser.GetRandom(Run.instance.availableEquipmentDropList, PickupIndex.none);
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
                inventory.SetEquipmentIndex(equipmentIndex, isRemovingEquipment: false);
            }
        }
    }
}
