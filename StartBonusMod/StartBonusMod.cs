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
    [BepInDependency("com.KingEnderBrine.ProperSave", BepInDependency.DependencyFlags.SoftDependency)]
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
        // the same players by user, for ProperSave: a loaded run has new player masters
        private HashSet<string> itemGivenToUsers = new HashSet<string>();

        public void Awake()
        {
            PInfo = Info;
            instance = this;
            Log.Init(Logger);
            ModCompatibilityProperSave.Register("StartBonusMod.StartingItemsGivenTo", () => itemGivenToUsers.ToList(), LoadStartingItemsGivenTo);
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
                x => x.MatchLdcI4(0),
                x => x.MatchCallvirt<CharacterMaster>("GiveMoney")
                );
            c.Index += 4;
            c.RemoveRange(2);
            c.EmitDelegate((CharacterMaster characterMaster, uint startingMoney) =>
            {
                if (characterMaster != null)
                {
                    if (BepConfig.Enabled.Value && BepConfig.StartingCashEnabled.Value)
                    {
                        if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.MagnusMagnuson.StartInBazaar"))
                        {
                            characterMaster.GiveMoney((uint)Run.instance.GetDifficultyScaledCost(BepConfig.StartingCash.Value) + startingMoney, false);
                        }
                        else
                        {
                            characterMaster.GiveMoney((uint)Run.instance.GetDifficultyScaledCost(BepConfig.StartingCash.Value), false);
                        }
                    }
                    else
                    {
                        characterMaster.GiveMoney(startingMoney, false);
                    }
                }
            });
        }

        private void Run_Start(On.RoR2.Run.orig_Start orig, Run self)
        {
            instance.itemGivenTo.Clear();
            instance.itemGivenToUsers.Clear();
            orig(self);
        }

        // players loaded from a save have their starting items in their inventory already
        private void LoadStartingItemsGivenTo(object saved)
        {
            if (saved is not List<string> users)
            {
                return;
            }
            itemGivenToUsers.UnionWith(users);
            foreach (var pc in PlayerCharacterMasterController.instances)
            {
                if (pc.networkUser && itemGivenToUsers.Contains(GetUserKey(pc.networkUser.id)) && !itemGivenTo.Contains(pc))
                {
                    itemGivenTo.Add(pc);
                }
            }
        }

        private static string GetUserKey(NetworkUserId id)
        {
            return $"{id.value}/{id.strValue}/{id.subId}";
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
                                GiveStartingItemsAdvanced(characterBody);
                            instance.itemGivenTo.Add(master.playerCharacterMasterController);
                            if (master.playerCharacterMasterController.networkUser)
                            {
                                instance.itemGivenToUsers.Add(GetUserKey(master.playerCharacterMasterController.networkUser.id));
                            }
                        }
                    }
                }
            }
        }


        private void GiveStartingItemsAdvanced(CharacterBody body)
        {
            Dictionary<PickupIndex, int> itemsToGive = new Dictionary<PickupIndex, int>();
            ItemStringParser.ItemStringParser.ParseItemString(BepConfig.AdvancedItemList.Value, itemsToGive, instance.Logger, false);
            // equipment without a free slot is dropped next to the player
            foreach (var leftOver in ItemStringParser.ItemStringParser.GivePickups(body, itemsToGive))
            {
                PickupDropletController.CreatePickupDroplet(leftOver, body.corePosition + Vector3.up * 1.5f, Vector3.up * 15f, false);
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
                    PickupIndex pickupIndex;
                    try
                    {
                        // ItemStringParser's "Lunar" also includes lunar equipment
                        var tierKey = itemTier == ItemTier.Lunar ? "LunarItem" : itemTier.ToString();
                        pickupIndex = ItemStringParser.ItemStringParser.ResolveItemKey(tierKey);
                    }
                    catch (ArgumentException exception)
                    {
                        // e.g. a tier from another mod, which ItemStringParser has no keyword for; the other items are still given
                        Log.LogWarning($"No random {itemTier} item: {exception.Message}");
                        continue;
                    }
                    var itemIndex = PickupCatalog.GetPickupDef(pickupIndex)?.itemIndex ?? ItemIndex.None;
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
                // the active slot, in the set the game picks for a new pickup
                var set = inventory.FindBestEquipmentLocationOrCurrent(in Inventory.EquipmentLocationQueryArgs.defaultPickup).set;
                inventory.SetEquipmentIndex(equipmentIndex, new EquipmentLocation { slot = inventory.activeEquipmentSlot, set = set });
            }
        }
    }
}
