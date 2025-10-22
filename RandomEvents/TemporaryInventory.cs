using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements.UIR;
using static Rewired.InputMapper;
using static Rewired.Utils.Classes.Utility.ObjectInstanceTracker;

namespace RandomEvents
{
    public class TemporaryInventory : MonoBehaviour
    {
        public Dictionary<ItemIndex, int> temporaryItems = new Dictionary<ItemIndex, int>();

        public static void Hook()
        {
            IL.RoR2.LunarSunBehavior.FixedUpdate += LunarSunBehavior_FixedUpdate;
            IL.RoR2.ScrapperController.BeginScrapping += ScrapperController_BeginScrapping;
            On.RoR2.CostTypeCatalog.LunarItemOrEquipmentCostTypeHelper.IsAffordable += (orig, costTypeDef, CostTcontext) => { using (new TemporarilyRemoveInventory(CostTcontext.activator)) { return orig(costTypeDef, CostTcontext); } };
            On.RoR2.CostTypeDef.IsAffordable += (orig, self, cost, activator) => { using (new TemporarilyRemoveInventory(activator)) { return orig(self, cost, activator); } };
            On.RoR2.PickupPickerController.SetOptionsFromInteractor += (orig, self, activator) => { using (new TemporarilyRemoveInventory(activator)) { orig(self, activator); } };
            On.RoR2.DelusionChestController.GenerateDelusionPickupIndexes += (orig, self, activator) => { using (new TemporarilyRemoveInventory(activator)) { orig(self, activator); } };
            On.RoR2.Inventory.ShrineRestackInventory += (orig, self, rng) => { using (new TemporarilyRemoveInventory(self)) { orig(self, rng); } };
            On.RoR2.ShrineCleanseBehavior.GetInteractability += (orig, self, activator) => { using (new TemporarilyRemoveInventory(activator)) { return orig(self, activator); } };
            On.RoR2.Inventory.CopyItemsFrom_Inventory_Func2 += (orig, self, otherInventory, filter) => { using (new TemporarilyRemoveInventory(otherInventory)) { orig(self, otherInventory, filter); } };
            //On.RoR2.Items.ContagiousItemManager.OnInventoryChangedGlobal += (orig, inventory) => { using (new TemporarilyRemoveInventory(inventory)) { orig(inventory); } };
            //On.RoR2.Items.ContagiousItemManager.TryForceReplacement += (orig, inventory, itemIndex) => { using (new TemporarilyRemoveInventory(inventory)) { orig(inventory, itemIndex); } };
        }

        private static void Inventory_CopyItemsFrom_Inventory_Func2(On.RoR2.Inventory.orig_CopyItemsFrom_Inventory_Func2 orig, Inventory self, Inventory other, Func<ItemIndex, bool> filter)
        {
            throw new NotImplementedException();
        }

        private static void LunarSunBehavior_FixedUpdate(MonoMod.Cil.ILContext il)
        {
            // Hook so that Egocentrism does not consume temporary items
            ILCursor c = new ILCursor(il);
            var label = c.DefineLabel();
            // IL_0187: ldloc.s 5
            // IL_0189: callvirt instance valuetype RoR2.ItemTier RoR2.ItemDef::get_tier()
            // IL_018e: ldc.i4.5
            c.GotoNext(
            x => x.MatchLdloc(5),
            x => x.MatchCallvirt<ItemDef>("get_tier"),
            x => x.MatchLdcI4(5)
            );
            c.Index += 1;
            c.Remove();
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<ItemDef, LunarSunBehavior, ItemTier>>((ItemDef item, LunarSunBehavior lunarSunBehavior) =>
            {
                var temporaryInventories = lunarSunBehavior.body.gameObject.GetComponents<TemporaryInventory>();
                if(temporaryInventories.Length == 0)
                    return item.tier; // vanilla
                var temporaryCount = 0;
                foreach(var tempInventory in temporaryInventories)
                {
                    temporaryCount += tempInventory.temporaryItems.GetValueOrDefault(item.itemIndex);
                }
                if (lunarSunBehavior.body.inventory.GetItemCount(item.itemIndex) <= temporaryCount)
                {
                    // notier means skip this item
                    return ItemTier.NoTier;
                }
                // vanilla
                return item.tier;
            });
        }
        private static void ScrapperController_BeginScrapping(ILContext il)
        {
            // Hook so that ScrapperController does not consume temporary items
            ILCursor c = new ILCursor(il);
            var label = c.DefineLabel();
            // int num = Mathf.Min(maxItemsToScrapAtATime, component.inventory.GetItemCount(pickupDef.itemIndex));
            // IL_006b: ldarg.0
            // IL_006c: ldfld int32 RoR2.ScrapperController::maxItemsToScrapAtATime
            // IL_0071: ldloc.1
            // IL_0072: callvirt instance class RoR2.Inventory RoR2.CharacterBody::get_inventory()
            // IL_0077: ldloc.0
            // IL_0078: ldfld valuetype RoR2.ItemIndex RoR2.PickupDef::itemIndex
            // IL_007d: callvirt instance int32 RoR2.Inventory::GetItemCount(valuetype RoR2.ItemIndex)
            // IL_0082: call int32[UnityEngine.CoreModule]UnityEngine.Mathf::Min(int32, int32)
            // IL_0087: stloc.2
            c.GotoNext(
            x => x.MatchLdloc(1),
            x => x.MatchCallvirt<CharacterBody>("get_inventory"),
            x => x.MatchLdloc(0),
            x => x.MatchLdfld<PickupDef>("itemIndex"),
            x => x.MatchCallvirt<Inventory>("GetItemCount")
            );
            c.Index += 1;
            c.Remove();
            c.Index += 2;
            c.Remove();
            c.EmitDelegate<Func<CharacterBody, ItemIndex, int>>((characterBody, itemIndex) =>
            {
                var temporaryInventories = characterBody.GetComponents<TemporaryInventory>();
                if (temporaryInventories.Length == 0)
                    return characterBody.inventory.GetItemCount(itemIndex); // vanilla
                var temporaryCount = 0;
                foreach (var tempInventory in temporaryInventories)
                {
                    temporaryCount += tempInventory.temporaryItems.GetValueOrDefault(itemIndex);
                }
                return characterBody.inventory.GetItemCount(itemIndex) - temporaryCount;
            });
        }


        private static int stackSize = 0;
        public class TemporarilyRemoveInventory : IDisposable
        {
            Inventory inventory;
            List<TemporaryInventory> temporaryInventories = new List<TemporaryInventory>();
            List<ItemIndex> itemAcquisitionOrderBackup = new List<ItemIndex>();

            private void FindInventories(GameObject gameObject)
            {
                if (gameObject.TryGetComponent<CharacterBody>(out var characterBody))
                {
                    if (characterBody != null)
                    {
                        FindInventories(characterBody);
                    }
                    else
                    {
                        Log.LogError($"CharacterBody is null for gameobject {gameObject}");
                    }
                }
                else
                {
                    Log.LogError($"Could not find characterBody for GameObject {gameObject}");
                }
            }
            private void FindInventories(CharacterBody characterBody)
            {
                if (characterBody.inventory != null)
                {
                    if (characterBody.inventory != null)
                    {
                        this.inventory = characterBody.inventory;
                        this.temporaryInventories = characterBody.GetComponents<TemporaryInventory>().ToList();
                    }
                    else
                    {
                        Log.LogError($"Inventory is null for characterbody {characterBody}.");
                    }
                }
            }
            private void FindInventories(Inventory inventory)
            {
                if (inventory.TryGetComponent<CharacterMaster>(out var characterMaster))
                {
                    if (characterMaster.bodyInstanceObject != null)
                    {
                        if (characterMaster.bodyInstanceObject.TryGetComponent<CharacterBody>(out var characterBody))
                        {
                            this.inventory = inventory;
                            FindInventories(characterBody);
                        }
                    }
                    else
                    {
                        Log.LogError($"characterMaster.bodyInstanceObject is null: {characterMaster}");
                    }
                }
                else
                {
                    Log.LogError($"Inventory does not have a CharacterMaster: {inventory}");
                }
            }
            public TemporarilyRemoveInventory(Inventory inventory)
            {
                if (stackSize == 0)
                {
                    FindInventories(inventory);
                    RemoveTemporaryInventoryTemporarily();
                }
                stackSize++;
            }
            public TemporarilyRemoveInventory(Interactor activator)
            {
                if (stackSize == 0)
                {
                    FindInventories(activator.gameObject);
                    RemoveTemporaryInventoryTemporarily();
                }
                stackSize++;
            }

            public TemporarilyRemoveInventory(CharacterBody body)
            {
                if (stackSize == 0)
                {
                    FindInventories(body);
                    RemoveTemporaryInventoryTemporarily();
                }
                stackSize++;
            }

            public void Dispose()
            {
                stackSize--;
                if (stackSize == 0)
                {
                    AddTemporaryInventoryBack();
                }
            }

            private void RemoveTemporaryInventoryTemporarily()
            {
                if (inventory == null)
                    return;
                itemAcquisitionOrderBackup.Clear();
                itemAcquisitionOrderBackup.AddRange(inventory.itemAcquisitionOrder);
                foreach (var temporaryInventory in temporaryInventories)
                {
                    foreach (var (itemIndex, count) in temporaryInventory.temporaryItems)
                    {
                        inventory.itemStacks[(int)itemIndex] -= count;
                        if (inventory.itemStacks[(int)itemIndex] <= 0)
                        {
                            inventory.itemAcquisitionOrder.Remove(itemIndex);
                        }
                    }
                }
            }

            private void AddTemporaryInventoryBack()
            {
                if (inventory == null)
                    return;
                foreach (var temporaryInventory in temporaryInventories)
                {
                    foreach (var (itemIndex, count) in temporaryInventory.temporaryItems)
                    {
                        inventory.itemStacks[(int)itemIndex] += count;
                        if (inventory.itemStacks[(int)itemIndex] > 0)
                        {
                            inventory.itemAcquisitionOrder.Add(itemIndex);
                        }
                    }
                }
                // Restore the order
                var currentItems = new HashSet<ItemIndex>(inventory.itemAcquisitionOrder);

                // Clear the current list to restore order
                inventory.itemAcquisitionOrder.Clear();

                // Add items that exist in both backup and current list, preserving backup order
                foreach (var item in itemAcquisitionOrderBackup)
                {
                    if (currentItems.Remove(item)) // Remove to track which are handled
                    {
                        inventory.itemAcquisitionOrder.Add(item);
                    }
                }

                // Add remaining items that only exist in current list (not in backup) at the end
                foreach (var item in currentItems)
                {
                    inventory.itemAcquisitionOrder.Add(item);
                }
            }
        }


        public static TemporaryInventory Find(GameObject obj, TemporaryInventory candidate)
        {
            var inventories = obj.GetComponents<TemporaryInventory>();
            return inventories.FirstOrDefault(item => item == candidate);
        }

        public static TemporaryInventory Find(GameObject obj, List<TemporaryInventory> candidates)
        {
            var inventories = obj.GetComponents<TemporaryInventory>();
            return inventories.FirstOrDefault(item => candidates.Contains(item));
        }

        private void AddItemToDict(Dictionary<ItemIndex, int> items, ItemIndex item, int count = 1)
        {
            if (count == 0) return;
            if (items.ContainsKey(item))
            {
                items[item] = items[item] + count;
            }
            else
            {
                items.Add(item, count);
            }
            if (items[item] == 0)
            {
                items.Remove(item);
            }
        }

        public void GiveTemporaryItem(ItemIndex itemIndex, int count = 1)
        {
            if(count <= 0)
            {
                return;
            }
            if (TryGetComponent(out CharacterBody body))
            {
                body.inventory.GiveItem(itemIndex, count);
                AddItemToDict(temporaryItems, itemIndex, count);
            }
        }

        public void RemoveTemporaryItem(ItemIndex itemIndex, bool debtCollection, int count = 1)
        {
            if(!temporaryItems.ContainsKey(itemIndex)) {
                return;
            }
            if(count > temporaryItems[itemIndex])
            {
                count = temporaryItems[itemIndex];
            }
            if (count <= 0)
            {
                return;
            }
            if (TryGetComponent(out CharacterBody body))
            {
                var has = body.inventory.GetItemCount(itemIndex);
                if(has >= count)
                {
                    body.inventory.RemoveItem(itemIndex, count);
                    AddItemToDict(temporaryItems, itemIndex, -count);
                }
                else
                {
                    if(debtCollection)
                    {
                        CollectDebt(body.inventory, itemIndex, count);
                        AddItemToDict(temporaryItems, itemIndex, -count);
                    }
                    else
                    {
                        body.inventory.RemoveItem(itemIndex, has);
                        AddItemToDict(temporaryItems, itemIndex, -has);
                    }
                }
            }
        }

        private void CollectDebt(Inventory inventory, ItemIndex itemIndex, int count)
        {
            var debt = count - inventory.GetItemCount(itemIndex);
            inventory.RemoveItem(itemIndex, inventory.GetItemCount(itemIndex));
            ItemDef itemDef = ItemCatalog.GetItemDef(itemIndex);
            // prefer scrap
            var scrapPickupIndex = PickupCatalog.FindScrapIndexForItemTier(itemDef.tier);
            if (scrapPickupIndex != PickupIndex.none)
            {
                var scrapCount = inventory.GetItemCount(itemIndex);
                if (scrapCount > 0)
                {
                    if (scrapCount > debt)
                        scrapCount = debt;
                    debt -= scrapCount;
                    inventory.RemoveItem(scrapPickupIndex.pickupDef.itemIndex, scrapCount);
                }
            }
            if (debt == 0)
                return;
            // still in debt -> check other items of same tier
            var otherItemsOfSameTier = GetRandomItemsOfTier(inventory, itemDef.tier, debt);
            foreach (var (itemIndex2, count2) in otherItemsOfSameTier)
            {
                inventory.RemoveItem(itemIndex2, count2);
                debt -= count2;
            }
            if (debt == 0)
                return;
            // still in debt -> try other item tiers
            ItemTier originalTier = itemDef.tier;
            ItemTier nextTier = ItemTier.NoTier;
            switch (itemDef.tier)
            {
                case ItemTier.Tier1:
                    nextTier = ItemTier.VoidTier1;
                    break;
                case ItemTier.Tier2:
                    nextTier = ItemTier.VoidTier2;
                    break;
                case ItemTier.Tier3:
                    nextTier = ItemTier.VoidTier3;
                    break;
                case ItemTier.Lunar:
                    nextTier = ItemTier.Boss;
                    break;
            }
            // prefer scrap of that new tier
            scrapPickupIndex = PickupCatalog.FindScrapIndexForItemTier(itemDef.tier);
            if (scrapPickupIndex != PickupIndex.none)
            {
                var scrapCount = inventory.GetItemCount(itemIndex);
                if (scrapCount > 0)
                {
                    if (scrapCount > debt)
                        scrapCount = debt;
                    debt -= scrapCount;
                    inventory.RemoveItem(scrapPickupIndex.pickupDef.itemIndex, scrapCount);
                }
            }
            if (debt == 0)
                return;
            // still in debt -> check other items of same tier
            var otherItems = GetRandomItemsOfTier(inventory, nextTier, debt);
            foreach (var (itemIndex3, count3) in otherItems)
            {
                inventory.RemoveItem(itemIndex3, count3);
                debt -= count3;
            }
            // still in debt -> just take any random items from the inventory
            var anyItems = GetRandomItemsOfTier(inventory, ItemTier.NoTier, debt);
            foreach (var (itemIndex4, count4) in anyItems)
            {
                inventory.RemoveItem(itemIndex4, count4);
                debt -= count4;
            }
        }

        private void OnDisable()
        {
            if (TryGetComponent(out CharacterBody body))
            {
                foreach (var (itemIndex, count) in temporaryItems)
                {
                    if (body.inventory.GetItemCount(itemIndex) >= count)
                    {
                        body.inventory.RemoveItem(itemIndex, count);
                    }
                    else
                    {
                        CollectDebt(body.inventory, itemIndex, count);
                    }
                }
            }
        }

        private Dictionary<ItemIndex, int> GetRandomItemsOfTier(Inventory inventory, ItemTier tier, int count)
        {
            Dictionary<ItemIndex, int> items = new Dictionary<ItemIndex, int>();
            if (count <= 0)
                return items;
            var rng = new Xoroshiro128Plus(Run.instance.seed);
            List<ItemIndex> inventoryItems = new List<ItemIndex>(inventory.itemAcquisitionOrder);
            Util.ShuffleList(inventoryItems, rng);
            foreach(var itemIndex in inventoryItems)
            {
                ItemDef itemDef = ItemCatalog.GetItemDef(itemIndex);
                if (itemDef.tier == ItemTier.NoTier)
                    continue;
                var itemCount = inventory.GetItemCount(itemIndex);
                if (itemDef.tier == tier || tier == ItemTier.NoTier)
                {
                    if (itemCount > count)
                        itemCount = count;
                    AddItemToDict(items, itemIndex, itemCount);
                    count -= itemCount;
                }
                if(count <= 0)
                {
                    break;
                }
            }
            return items;
        }
    }
}
