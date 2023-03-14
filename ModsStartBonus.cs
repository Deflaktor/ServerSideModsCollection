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
                c.EmitDelegate<Action<Inventory, ItemDef, Int32>>((inventory, itemDef, count) =>
                {
                    inventory.GiveItem(itemDef, count);
                    var itemEnum = BepConfig.StartingItemWhite.Value;
                    if (itemEnum != ItemWhiteEnum.None)
                    {
                        inventory.GiveItem(itemDef, count);
                    }
                    
                });
            };
        }

        private static bool ShouldGiveStartingItems()
        {
            return BepConfig.StartingItemWhite.Value != ItemWhiteEnum.None ||
                   BepConfig.StartingItemGreen.Value != ItemGreenEnum.None ||
                   BepConfig.StartingItemRed.Value   != ItemRedEnum.None   ||
                   BepConfig.StartingItemBoss.Value  != ItemBossEnum.None  ||
                   BepConfig.StartingItemLunar.Value != ItemLunarEnum.None ||
                   BepConfig.StartingItemVoid.Value  != ItemVoidEnum.None  ||
                   BepConfig.StartingItemEquip.Value != ItemEquipEnum.None;
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
