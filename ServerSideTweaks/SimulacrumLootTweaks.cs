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

namespace ServerSideTweaks
{
    public class SimulacrumLootTweaks
    {
        public GameObject mostRecentlyCreatedPickup = null;
        public float totalItemRewardCount = 0;
        public Dictionary<PlayerCharacterMasterController, float> usersItemCredit = new Dictionary<PlayerCharacterMasterController, float>();

        public void Init()
        {
            //NullifierDeathBombProjectile = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Nullifier/NullifierDeathBombProjectile.prefab");
            //NullifierPreBombProjectile = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Nullifier/NullifierPreBombProjectile.prefab");
            // NullifierExplosion = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Nullifier/NullifierExplosion.prefab");
        }

        public void RunStart()
        {
            mostRecentlyCreatedPickup = null;
            totalItemRewardCount = 0;
            usersItemCredit.Clear();
        }

        public void Hook()
        {
            IL.RoR2.InfiniteTowerWaveController.DropRewards += InfiniteTowerWaveController_DropRewards;
            On.RoR2.InfiniteTowerWaveController.DropRewards += InfiniteTowerWaveController_DropRewards1;
            On.RoR2.GenericPickupController.AttemptGrant += GenericPickupController_AttemptGrant;
            On.RoR2.GenericPickupController.OnInteractionBegin += GenericPickupController_OnInteractionBegin;
            IL.RoR2.GenericPickupController.CreatePickup += GenericPickupController_CreatePickup;
            On.RoR2.PickupDropletController.OnCollisionEnter += PickupDropletController_OnCollisionEnter;
            On.RoR2.PickupPickerController.OnInteractionBegin += PickupPickerController_OnInteractionBegin;
            On.RoR2.PickupPickerController.CreatePickup_PickupIndex += PickupPickerController_CreatePickup_PickupIndex;

            if (ModCompatibilityShareSuite.enabled)
            {
                ModCompatibilityShareSuite.AddPickupEventHandler(NonShareableItemCheck);
            }
        }

        public void Unhook()
        {
            IL.RoR2.InfiniteTowerWaveController.DropRewards -= InfiniteTowerWaveController_DropRewards;
            On.RoR2.InfiniteTowerWaveController.DropRewards -= InfiniteTowerWaveController_DropRewards1;
            On.RoR2.GenericPickupController.AttemptGrant -= GenericPickupController_AttemptGrant;
            On.RoR2.GenericPickupController.OnInteractionBegin -= GenericPickupController_OnInteractionBegin;
            IL.RoR2.GenericPickupController.CreatePickup -= GenericPickupController_CreatePickup;
            On.RoR2.PickupDropletController.OnCollisionEnter -= PickupDropletController_OnCollisionEnter;
            On.RoR2.PickupPickerController.OnInteractionBegin -= PickupPickerController_OnInteractionBegin;
            On.RoR2.PickupPickerController.CreatePickup_PickupIndex -= PickupPickerController_CreatePickup_PickupIndex;

            if (ModCompatibilityShareSuite.enabled)
            {
                ModCompatibilityShareSuite.RemovePickupEventHandler(NonShareableItemCheck);
            }
        }

        public void GiveAllPlayersItemCredit(float credit)
        {
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



        private void SetSkullCounterCount(Inventory inventory, int count, GameObject playerObject)
        {
            if (BepConfig.SimulacrumLootSkullTokens.Value)
            {
                ItemIndex itemIndex = ItemCatalog.FindItemIndex("SkullCounter");
                int currentCount = inventory.GetItemCount(itemIndex);
                int diff = count - currentCount;
                if (diff != 0)
                {
                    inventory.GiveItem(itemIndex, diff);
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


        private void InfiniteTowerWaveController_DropRewards(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            var label = c.DefineLabel();
            // IL_010e: ldloc.s 6
            // IL_0110: ldloc.s 4
            // IL_0112: ldloc.2
            // IL_0113: call void RoR2.PickupDropletController::CreatePickupDroplet(valuetype RoR2.GenericPickupController / CreatePickupInfo, valuetype[UnityEngine.CoreModule]UnityEngine.Vector3, valuetype[UnityEngine.CoreModule]UnityEngine.Vector3)
            c.GotoNext(
            x => x.MatchLdloc(2),
            x => x.MatchCall<PickupDropletController>("CreatePickupDroplet")
            );
            c.Index += 1;
            c.Remove();
            c.EmitDelegate<Action<CreatePickupInfo, Vector3, Vector3>>((pickupInfo, position, velocity) =>
            {
                if (BepConfig.Enabled.Value)
                {
                    GameObject obj = GameObject.Instantiate(PickupDropletController.pickupDropletPrefab, position, Quaternion.identity);
                    PickupDropletController component = obj.GetComponent<PickupDropletController>();
                    if ((bool)component)
                    {
                        component.createPickupInfo = pickupInfo;
                        component.NetworkpickupState = pickupInfo.pickup;
                    }
                    Rigidbody component2 = obj.GetComponent<Rigidbody>();
                    component2.velocity = velocity;
                    component2.AddTorque(UnityEngine.Random.Range(150f, 120f) * UnityEngine.Random.onUnitSphere);
                    obj.AddComponent<NonShareableItem>();
                    NetworkServer.Spawn(obj);
                    totalItemRewardCount += 1f;
                }
                else
                {
                    PickupDropletController.CreatePickupDroplet(pickupInfo, position, velocity);
                }
            });
        }

        private void InfiniteTowerWaveController_DropRewards1(On.RoR2.InfiniteTowerWaveController.orig_DropRewards orig, InfiniteTowerWaveController self)
        {
            orig(self);
            if (!NetworkServer.active)
            {
                return;
            }
            if (BepConfig.Enabled.Value)
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
        private void PickupDropletController_OnCollisionEnter(On.RoR2.PickupDropletController.orig_OnCollisionEnter orig, PickupDropletController self, Collision collision)
        {
            mostRecentlyCreatedPickup = null;
            orig(self, collision);
            if (BepConfig.Enabled.Value && mostRecentlyCreatedPickup != null)
            {
                NonShareableItem component = self.GetComponent<NonShareableItem>();
                if ((bool)component)
                {
                    mostRecentlyCreatedPickup.AddComponent<NonShareableItem>();
                }
            }
        }
        private void PickupPickerController_CreatePickup_PickupIndex(On.RoR2.PickupPickerController.orig_CreatePickup_PickupIndex orig, PickupPickerController self, PickupIndex pickupIndex)
        {
            mostRecentlyCreatedPickup = null;
            orig(self, pickupIndex);
            if (BepConfig.Enabled.Value && mostRecentlyCreatedPickup != null)
            {
                NonShareableItem component = self.GetComponent<NonShareableItem>();
                if ((bool)component)
                {
                    mostRecentlyCreatedPickup.AddComponent<NonShareableItem>();
                }
            }
        }



        private void CommandArtifactManager_OnDropletHitGroundServer(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            var label = c.DefineLabel();
            // IL_0032: ldarg.0
            // IL_0033: ldfld valuetype [UnityEngine.CoreModule]UnityEngine.Vector3 RoR2.GenericPickupController/CreatePickupInfo::position
            // IL_0038: ldarg.0
            // IL_0039: ldfld valuetype [UnityEngine.CoreModule]UnityEngine.Quaternion RoR2.GenericPickupController/CreatePickupInfo::rotation
            // IL_003e: call !!0 [UnityEngine.CoreModule]UnityEngine.Object::Instantiate<class [UnityEngine.CoreModule]UnityEngine.GameObject>(!!0, valuetype [UnityEngine.CoreModule]UnityEngine.Vector3, valuetype [UnityEngine.CoreModule]UnityEngine.Quaternion)
            // IL_0043: dup
            c.GotoNext(
            x => x.MatchLdarg(0),
            x => x.MatchLdfld<CreatePickupInfo>("position"),
            x => x.MatchLdarg(0),
            x => x.MatchLdfld<CreatePickupInfo>("rotation"),
            x => x.MatchCall<UnityEngine.Object>("Instantiate"),
            x => x.MatchDup()
            );
            c.Index += 5;
            c.EmitDelegate<Func<GameObject, GameObject>>((obj) =>
            {
                mostRecentlyCreatedPickup = obj;
                return obj;
            });
        }
        private void GenericPickupController_CreatePickup(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            var label = c.DefineLabel();
            // IL_000f: ldarg.0
            // IL_0010: ldfld valuetype [UnityEngine.CoreModule]UnityEngine.Vector3 RoR2.GenericPickupController/CreatePickupInfo::position
            // IL_0015: ldarg.0
            // IL_0016: ldfld valuetype [UnityEngine.CoreModule]UnityEngine.Quaternion RoR2.GenericPickupController/CreatePickupInfo::rotation
            // IL_001b: call !!0 [UnityEngine.CoreModule]UnityEngine.Object::Instantiate<class [UnityEngine.CoreModule]UnityEngine.GameObject>(!!0, valuetype [UnityEngine.CoreModule]UnityEngine.Vector3, valuetype [UnityEngine.CoreModule]UnityEngine.Quaternion)
            // IL_0020: dup
            c.GotoNext(
            x => x.MatchLdarg(0),
            x => x.MatchLdfld<CreatePickupInfo>("position"),
            x => x.MatchLdarg(0),
            x => x.MatchLdfld<CreatePickupInfo>("rotation"),
            x => x.MatchCall<UnityEngine.Object>("Instantiate")
            );
            c.Index += 5;
            c.EmitDelegate<Func<GameObject, GameObject>>((obj) =>
            {
                mostRecentlyCreatedPickup = obj;
                return obj;
            });
        }
    }
}
