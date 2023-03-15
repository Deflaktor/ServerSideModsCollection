using System;
using System.Collections.Generic;
using System.Text;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using R2API.Utils;
using RoR2;
using RoR2.Networking;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using MonoMod.Cil;
using static ServerSideModsCollection.EnumCollection;

namespace ServerSideModsCollection
{
    public class ModsStartBonus
    {
        public static void Init()
        {
            IL.RoR2.Run.SetupUserCharacterMaster += il =>
            {
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
                    if(BepConfig.StartingCash.Value >=0) 
                    {
                        return (uint)BepConfig.StartingCash.Value;
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
            };
        }

        private static void GiveStartingItems(Inventory inventory, ItemDef itemDef, Int32 count)
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

        private void GiveEveryone(PickupIndex pickupIndex)
        {
            for (int i = 0; i < PlayerCharacterMasterController.instances.Count; i++)
            {
                try
                {
                    NetworkUser networkUser = PlayerCharacterMasterController.instances[i].networkUser;
                    if (networkUser && networkUser.isActiveAndEnabled)
                    {
                        PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
                        CharacterBody body = networkUser.master.GetBody();
                        CharacterMaster master = networkUser.master;
                        //ItemDef itemDef = ItemCatalog.GetItemDef(pickupDef.itemIndex);

                        body.inventory.GiveItem(pickupDef.itemIndex);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError(ex);
                }
                finally
                {

                }
            }
        }
    }
}
