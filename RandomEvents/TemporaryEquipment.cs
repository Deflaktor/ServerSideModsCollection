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
using static RandomEvents.TemporaryInventory;
using static Rewired.InputMapper;
using static Rewired.Utils.Classes.Utility.ObjectInstanceTracker;

namespace RandomEvents
{
    public class TemporaryEquipment : MonoBehaviour
    {
        private EquipmentState[][] realEquipmentStateSlots;
        private EquipmentState[][] temporaryEquipmentStateSlots;
        public float cooldownScale = 1f;
        public bool doNotDisableEquipment = false;

        public static void Hook()
        {
            //On.RoR2.Inventory.SetEquipmentInternal += Inventory_SetEquipmentInternal;
            On.RoR2.CostTypeDef.IsAffordable += (orig, self, cost, activator) => { using (new TemporarilyRemoveEquipment(activator)) { return orig(self, cost, activator); } };
            On.RoR2.EquipmentDef.AttemptGrant += (orig, ref context) => { using (new TemporarilyRestoreOriginalEquipment(ref context)) { orig(ref context); } };
            On.RoR2.Inventory.CalculateEquipmentCooldownScale += Inventory_CalculateEquipmentCooldownScale;
            On.RoR2.Inventory.SetEquipmentDisabled += Inventory_SetEquipmentDisabled;
        }

        private static void Inventory_SetEquipmentDisabled(On.RoR2.Inventory.orig_SetEquipmentDisabled orig, Inventory self, bool _active)
        {
            if (self.TryGetComponent<CharacterMaster>(out var characterMaster))
            {
                if (characterMaster.bodyInstanceObject != null)
                {
                    if (characterMaster.bodyInstanceObject.TryGetComponent<CharacterBody>(out var characterBody) && characterMaster.bodyInstanceObject.TryGetComponent<TemporaryEquipment>(out var temporaryEquipment))
                    {
                        if (temporaryEquipment.doNotDisableEquipment)
                        {
                            return;
                        }
                    }
                }
                else
                {
                    Log.LogError($"characterMaster.bodyInstanceObject is null: {characterMaster}");
                }
            }
            else
            {
                Log.LogError($"Inventory does not have a CharacterMaster: {self}");
            }
            orig(self, _active);
        }

        private static float Inventory_CalculateEquipmentCooldownScale(On.RoR2.Inventory.orig_CalculateEquipmentCooldownScale orig, Inventory self)
        {
            if (self.TryGetComponent<CharacterMaster>(out var characterMaster))
            {
                if (characterMaster.bodyInstanceObject != null)
                {
                    if (characterMaster.bodyInstanceObject.TryGetComponent<CharacterBody>(out var characterBody) && characterMaster.bodyInstanceObject.TryGetComponent<TemporaryEquipment>(out var temporaryEquipment))
                    {
                        return temporaryEquipment.cooldownScale * orig(self);
                    }
                }
                else
                {
                    Log.LogError($"characterMaster.bodyInstanceObject is null: {characterMaster}");
                }
            }
            else
            {
                Log.LogError($"Inventory does not have a CharacterMaster: {self}");
            }
            return orig(self);
        }

        public class TemporarilyRemoveEquipment : IDisposable
        {
            Inventory inventory;
            TemporaryEquipment temporaryEquipment;

            public TemporarilyRemoveEquipment(Interactor activator)
            {
                inventory = activator.GetComponent<CharacterBody>().inventory;
                temporaryEquipment = activator.GetComponent<TemporaryEquipment>();
                if (temporaryEquipment != null)
                {
                    Helper.FillEquipment(inventory, EquipmentIndex.None);
                }
            }

            public void Dispose()
            {
                if (temporaryEquipment != null)
                {
                    Helper.TransferEquipmentIndex(temporaryEquipment.temporaryEquipmentStateSlots, inventory._equipmentStateSlots);
                }
            }
        }

        public class TemporarilyRestoreOriginalEquipment : IDisposable
        {
            Inventory inventory;
            TemporaryEquipment temporaryEquipment;

            public TemporarilyRestoreOriginalEquipment(ref PickupDef.GrantContext context)
            {
                inventory = context.body.inventory;
                temporaryEquipment = context.body.GetComponent<TemporaryEquipment>();
                if (temporaryEquipment != null)
                {
                    inventory._equipmentStateSlots = temporaryEquipment.realEquipmentStateSlots;
                }
            }

            public void Dispose()
            {
                if (temporaryEquipment != null)
                {
                    inventory._equipmentStateSlots = temporaryEquipment.temporaryEquipmentStateSlots;
                }
            }
        }

        public void SetTemporaryEquipment(EquipmentIndex equipmentIndex)
        {
            if (TryGetComponent(out CharacterBody body))
            {
                if (body.inventory != null && body.inventory._equipmentStateSlots != null && body.master != null)
                {
                    int maxEquipmentSlots = Helper.IsToolbotWithSwapSkill(body.master) ? 2 : 1;
                    int maxEquipmentSets = body.inventory.GetItemCountEffective(DLC3Content.Items.ExtraEquipment.itemIndex) + 1;
                    for (uint slot = 0; slot < maxEquipmentSlots; slot++)
                    {
                        for (uint set = 0; set < maxEquipmentSets; set++)
                        {
                            body.inventory.SetEquipmentIndexForSlot(equipmentIndex, slot, set);
                        }
                    }
                    temporaryEquipmentStateSlots = Helper.CopyEquipmentState(body.inventory._equipmentStateSlots);
                }
            }
        }

        private void OnEnable()
        {
            if (TryGetComponent(out CharacterBody body))
            {
                if(body.inventory != null && body.inventory._equipmentStateSlots != null) {
                    realEquipmentStateSlots = Helper.CopyEquipmentState(body.inventory._equipmentStateSlots);
                }
            }
        }

        private void OnDisable()
        {
            if (TryGetComponent(out CharacterBody body))
            {
                if (body.inventory != null)
                {
                    for (uint slot = 0; slot < temporaryEquipmentStateSlots.Length; slot++)
                    {
                        for (uint set = 0; set < temporaryEquipmentStateSlots.Length; set++)
                        {
                            if(slot < realEquipmentStateSlots.Length && set < realEquipmentStateSlots[slot].Length)
                            {
                                body.inventory.SetEquipment(realEquipmentStateSlots[slot][set], slot, set);
                            }
                            else
                            {
                                body.inventory.SetEquipmentIndexForSlot(EquipmentIndex.None, slot, set);
                            }
                        }
                    }
                }
            }
        }
    }
}
