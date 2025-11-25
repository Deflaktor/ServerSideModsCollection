using MonoMod.Cil;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;
using static RoR2.GenericPickupController;
using static UnityEngine.ParticleSystem.PlaybackState;

namespace ServerSideTweaks
{
    public class SimulacrumLootTweaks
    {
        public GameObject mostRecentlyCreatedPickup = null;
        public float totalItemRewardCount = 0;
        private bool comingFromInfiniteTowerWaveController_DropRewards;
        public Dictionary<PlayerCharacterMasterController, float> usersItemCredit = new Dictionary<PlayerCharacterMasterController, float>();

        public void RunStart()
        {
            mostRecentlyCreatedPickup = null;
            totalItemRewardCount = 0;
            usersItemCredit.Clear();
        }

        public void Hook()
        {
            // Calls PickupDropletController.CreatePickupDroplet
            On.RoR2.InfiniteTowerWaveController.DropRewards += InfiniteTowerWaveController_DropRewards;
            // Creates a PickupDroplet and we modify it to assign it with the NonShareableItem component
            IL.RoR2.PickupDropletController.CreatePickupDroplet_CreatePickupInfo_Vector3_Vector3 += PickupDropletController_CreatePickupDroplet_CreatePickupInfo_Vector3_Vector3;
            // Creates the pickup when the droplet hits the ground -> calls either PickupDropletController.CreateCommandCube() or GenericPickupController.CreatePickup()
            On.RoR2.PickupDropletController.CreatePickup += PickupDropletController_CreatePickup;
            // The pickup droplet has hit the ground -> Transfer the NonShareableItem component to the command cube
            IL.RoR2.PickupDropletController.CreateCommandCube += PickupDropletController_CreateCommandCube;
            // The pickup droplet has hit the ground -> Transfer the NonShareableItem component to the pickup
            IL.RoR2.GenericPickupController.CreatePickup += GenericPickupController_CreatePickup;
            // The player has picked an pickup from the pickupPicker -> Transfer the NonShareableItem component to the actual created pickup
            On.RoR2.PickupPickerController.CreatePickup_UniquePickup += PickupPickerController_CreatePickup_UniquePickup;
            // The next two functions check to prevent people with insufficient credits to take the item
            On.RoR2.GenericPickupController.OnInteractionBegin += GenericPickupController_OnInteractionBegin;
            On.RoR2.PickupPickerController.OnInteractionBegin += PickupPickerController_OnInteractionBegin;
            // Reduce the credits of the player who took the item
            On.RoR2.GenericPickupController.AttemptGrant += GenericPickupController_AttemptGrant;

            if (ModCompatibilityShareSuite.enabled)
            {
                ModCompatibilityShareSuite.AddPickupEventHandler(NonShareableItemCheck);
            }
        }

        public void Unhook()
        {
            On.RoR2.InfiniteTowerWaveController.DropRewards -= InfiniteTowerWaveController_DropRewards;
            IL.RoR2.PickupDropletController.CreatePickupDroplet_CreatePickupInfo_Vector3_Vector3 -= PickupDropletController_CreatePickupDroplet_CreatePickupInfo_Vector3_Vector3;
            On.RoR2.PickupDropletController.CreatePickup -= PickupDropletController_CreatePickup;
            IL.RoR2.GenericPickupController.CreatePickup -= GenericPickupController_CreatePickup;
            On.RoR2.PickupPickerController.CreatePickup_UniquePickup -= PickupPickerController_CreatePickup_UniquePickup;
            On.RoR2.GenericPickupController.OnInteractionBegin -= GenericPickupController_OnInteractionBegin;
            On.RoR2.PickupPickerController.OnInteractionBegin -= PickupPickerController_OnInteractionBegin;
            On.RoR2.GenericPickupController.AttemptGrant -= GenericPickupController_AttemptGrant;

            if (ModCompatibilityShareSuite.enabled)
            {
                ModCompatibilityShareSuite.RemovePickupEventHandler(NonShareableItemCheck);
            }
        }

        private void InfiniteTowerWaveController_DropRewards(On.RoR2.InfiniteTowerWaveController.orig_DropRewards orig, InfiniteTowerWaveController self)
        {
            try
            {
                comingFromInfiniteTowerWaveController_DropRewards = true;
                orig(self);
            }
            finally
            {
                comingFromInfiniteTowerWaveController_DropRewards = false;
            }
            if (BepConfig.Enabled.Value && NetworkServer.active)
            {
                // detect disconnected players
                float freeCredit = 0;
                int connectedCount = 0;
                foreach (PlayerCharacterMasterController pc in PlayerCharacterMasterController.instances)
                {
                    if (pc.isConnected)
                    {
                        connectedCount++;
                    }
                    else if (usersItemCredit.ContainsKey(pc))
                    {
                        // if the player disconnected, distribute his remaining credits to everyone else
                        freeCredit += Math.Max(0, usersItemCredit[pc]);
                        usersItemCredit[pc] = 0;
                        SetSkullCounterCount(pc.master.inventory, BepConfig.SimulacrumLootMaxItemDebt.Value, pc.body?.gameObject);
                    }
                }
                // give item credit to every connected player
                foreach (PlayerCharacterMasterController pc in PlayerCharacterMasterController.instances)
                {
                    if (pc.isConnected)
                    {
                        if (usersItemCredit.ContainsKey(pc))
                        {
                            usersItemCredit[pc] += (float)totalItemRewardCount / (float)connectedCount;
                        }
                        else
                        {
                            usersItemCredit.Add(pc, (float)totalItemRewardCount / (float)connectedCount);
                        }
                        usersItemCredit[pc] += freeCredit / Math.Max(1f, connectedCount);
                        if (connectedCount == 1)
                        {
                            // if there is no one else connected, remove the skull counters
                            SetSkullCounterCount(pc.master.inventory, 0, null);
                        }
                        else
                        {
                            SetSkullCounterCount(pc.master.inventory, (int)Math.Floor(usersItemCredit[pc] + BepConfig.SimulacrumLootMaxItemDebt.Value), pc.body?.gameObject);
                        }
                        Log.LogDebug(pc.networkUser.userName + " itemCredit: " + usersItemCredit[pc]);
                    }
                }
                totalItemRewardCount = 0;
            }
        }
        private void PickupDropletController_CreatePickupDroplet_CreatePickupInfo_Vector3_Vector3(ILContext il)
        {
            // Assign the newly created pickup droplet the NonShareableItem component (but only if we are coming from the InfiniteTowerWaveController_DropRewards method
            ILCursor c = new ILCursor(il);;
            c.GotoNext(
                x => x.MatchCall<UnityEngine.Object>("Instantiate")
            );
            c.Index += 1;
            c.EmitDelegate<Func<GameObject, GameObject>>((obj) =>
            {
                if (comingFromInfiniteTowerWaveController_DropRewards && BepConfig.Enabled.Value && NetworkServer.active)
                {
                    obj.AddComponent<NonShareableItem>();
                    totalItemRewardCount += 1f;
                }
                return obj;
            });
        }

        private void PickupDropletController_CreatePickup(On.RoR2.PickupDropletController.orig_CreatePickup orig, PickupDropletController self)
        {
            mostRecentlyCreatedPickup = null;
            orig(self);
            if (BepConfig.Enabled.Value && mostRecentlyCreatedPickup != null)
            {
                // transfer the NonShareableItem component to the actual created pickup
                NonShareableItem component = self.GetComponent<NonShareableItem>();
                if (component != null)
                {
                    mostRecentlyCreatedPickup.AddComponent<NonShareableItem>();
                }
            }
        }

        private void PickupDropletController_CreateCommandCube(ILContext il)
        {
            // The pickup droplet has hit the ground -> set the mostRecentlyCreatedPickup
            ILCursor c = new ILCursor(il); ;
            c.GotoNext(
                x => x.MatchCall<UnityEngine.Object>("Instantiate")
            );
            c.Index += 1;
            c.EmitDelegate<Func<GameObject, GameObject>>((obj) =>
            {
                mostRecentlyCreatedPickup = obj;
                return obj;
            });
        }

        private void GenericPickupController_CreatePickup(ILContext il)
        {
            // The pickup droplet has hit the ground -> set the mostRecentlyCreatedPickup
            ILCursor c = new ILCursor(il); ;
            c.GotoNext(
                x => x.MatchCall<UnityEngine.Object>("Instantiate")
            );
            c.Index += 1;
            c.EmitDelegate<Func<GameObject, GameObject>>((obj) =>
            {
                mostRecentlyCreatedPickup = obj;
                return obj;
            });
        }

        private void PickupPickerController_CreatePickup_UniquePickup(On.RoR2.PickupPickerController.orig_CreatePickup_UniquePickup orig, PickupPickerController self, UniquePickup pickupState)
        {
            // The player has picked a pickup from the pickupPicker -> transfer the NonShareableItem component to the actual created pickup
            mostRecentlyCreatedPickup = null;
            orig(self, pickupState);
            if (BepConfig.Enabled.Value && mostRecentlyCreatedPickup != null)
            {
                NonShareableItem component = self.GetComponent<NonShareableItem>();
                if ((bool)component)
                {
                    mostRecentlyCreatedPickup.AddComponent<NonShareableItem>();
                }
            }
        }


        private void PickupPickerController_OnInteractionBegin(On.RoR2.PickupPickerController.orig_OnInteractionBegin orig, PickupPickerController self, Interactor activator)
        {
            if (CanInteract(self.gameObject, activator))
            {
                orig(self, activator);
            }
        }
        private void GenericPickupController_OnInteractionBegin(On.RoR2.GenericPickupController.orig_OnInteractionBegin orig, GenericPickupController self, Interactor activator)
        {
            if (CanInteract(self.gameObject, activator))
            {
                orig(self, activator);
            }
        }

        private void GenericPickupController_AttemptGrant(On.RoR2.GenericPickupController.orig_AttemptGrant orig, GenericPickupController self, CharacterBody body)
        {
            if ((BepConfig.Enabled.Value && CanInteract(self.gameObject, body)) || !BepConfig.Enabled.Value)
            {
                orig(self, body);
            }
            if (!NetworkServer.active)
            {
                return;
            }
            if (BepConfig.Enabled.Value && self.TryGetComponent<NonShareableItem>(out _))
            {
                if (self.consumed)
                {
                    var master = body.master;
                    if (master != null && master.playerCharacterMasterController != null)
                    {
                        var pc = master.playerCharacterMasterController;
                        if (usersItemCredit.ContainsKey(pc))
                        {
                            usersItemCredit[pc] -= 1f;
                        }
                        else
                        {
                            usersItemCredit.Add(pc, -1f);
                        }
                        int connectedCount = PlayerCharacterMasterController.instances.Where(pc => pc.isConnected).Count();
                        if (connectedCount == 1)
                        {
                            // if there is no one else connected, remove the skull counters
                            SetSkullCounterCount(body.inventory, 0, null);
                        }
                        else
                        {
                            SetSkullCounterCount(body.inventory, (int)Math.Floor(usersItemCredit[pc] + BepConfig.SimulacrumLootMaxItemDebt.Value), body.gameObject);
                        }
                    }
                }
            }
        }

        private bool NonShareableItemCheck(GenericPickupController pickup, CharacterBody picker)
        {
            if (BepConfig.SimulacrumNonSharedLoot.Value)
                return !pickup.TryGetComponent<NonShareableItem>(out _);
            // item shareable
            return true;
        }

        private bool CanInteract(GameObject self, CharacterBody body)
        {
            if (!BepConfig.Enabled.Value)
            {
                return true;
            }
            if (self.TryGetComponent<NonShareableItem>(out _))
            {
                var master = body.master;
                if (master != null && master.playerCharacterMasterController != null)
                {
                    var pc = master.playerCharacterMasterController;
                    if (usersItemCredit.TryGetValue(pc, out float credit))
                    {
                        if (credit + BepConfig.SimulacrumLootMaxItemDebt.Value >= 1)
                        {
                            return true;
                        }
                        else
                        {
                            ChatHelper.PlayerHasTooManyItems(pc.GetDisplayName());
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        private bool CanInteract(GameObject self, Interactor activator)
        {
            return CanInteract(self, activator?.GetComponent<CharacterBody>());
        }

        private void SetSkullCounterCount(Inventory inventory, int count, GameObject playerObject)
        {
            if (BepConfig.SimulacrumLootSkullTokens.Value)
            {
                ItemIndex itemIndex = ItemCatalog.FindItemIndex("SkullCounter");
                int currentCount = inventory.GetItemCountPermanent(itemIndex);
                int diff = count - currentCount;
                if (diff != 0)
                {
                    inventory.GiveItemPermanent(itemIndex, diff);
                    var safeWardController = ((InfiniteTowerRun)Run.instance).safeWardController;
                    if (playerObject != null && safeWardController != null)
                    {
                        if (diff > 0)
                        {
                            PurchaseInteraction.CreateItemTakenOrb(safeWardController.transform.position, playerObject, itemIndex);
                        }
                        else
                        {
                            PurchaseInteraction.CreateItemTakenOrb(playerObject.transform.position, safeWardController.gameObject, itemIndex);
                        }
                    }
                }
            }
        }


        public void GiveAllPlayersItemCredit(float credit)
        {
            // Debug Method
            foreach (PlayerCharacterMasterController pc in PlayerCharacterMasterController.instances)
            {
                if (usersItemCredit.ContainsKey(pc))
                {
                    usersItemCredit[pc] += credit;
                }
                else
                {
                    usersItemCredit.Add(pc, credit);
                }
                Log.LogDebug($"{pc.networkIdentity}.ItemCredit: {usersItemCredit[pc]}");
            }
        }
    }
}
