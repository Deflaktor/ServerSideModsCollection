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
        private List<EquipmentState> realEquipmentStateSlots = new List<EquipmentState>();
        private List<EquipmentState> temporaryEquipmentStateSlots = new List<EquipmentState>();
        private bool passThrough = false;
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
                    for (uint i = 0; i < inventory.equipmentStateSlots.Length; i++)
                    {
                        inventory.equipmentStateSlots[i].equipmentDef = EquipmentCatalog.GetEquipmentDef(EquipmentIndex.None);
                        inventory.equipmentStateSlots[i].equipmentIndex = EquipmentIndex.None;
                    }
                }
            }

            public void Dispose()
            {
                if (temporaryEquipment != null)
                {
                    for (uint i = 0; i < inventory.equipmentStateSlots.Length; i++)
                    {
                        inventory.equipmentStateSlots[i].equipmentDef = temporaryEquipment.temporaryEquipmentStateSlots[0].equipmentDef;
                        inventory.equipmentStateSlots[i].equipmentIndex = temporaryEquipment.temporaryEquipmentStateSlots[0].equipmentIndex;
                    }
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
                    for (uint i = 0; i < inventory.equipmentStateSlots.Length; i++)
                    {
                        var equipmentIndex = EquipmentIndex.None;
                        if (i < temporaryEquipment.realEquipmentStateSlots.Count)
                        {
                            equipmentIndex = temporaryEquipment.realEquipmentStateSlots[(int)i].equipmentIndex;
                        }
                        inventory.equipmentStateSlots[i].equipmentDef = EquipmentCatalog.GetEquipmentDef(equipmentIndex);
                        inventory.equipmentStateSlots[i].equipmentIndex = equipmentIndex;
                    }
                }
            }

            public void Dispose()
            {
                if (temporaryEquipment != null)
                {
                    for (uint i = 0; i < inventory.equipmentStateSlots.Length; i++)
                    {
                        var equipmentIndex = EquipmentIndex.None;
                        if (i < temporaryEquipment.temporaryEquipmentStateSlots.Count)
                        {
                            equipmentIndex = temporaryEquipment.temporaryEquipmentStateSlots[(int)i].equipmentIndex;
                        }
                        inventory.equipmentStateSlots[i].equipmentDef = EquipmentCatalog.GetEquipmentDef(equipmentIndex);
                        inventory.equipmentStateSlots[i].equipmentIndex = equipmentIndex;
                    }
                }
            }
        }

        private static bool Inventory_SetEquipmentInternal(On.RoR2.Inventory.orig_SetEquipmentInternal orig, Inventory self, EquipmentState equipmentState, uint slot)
        {
            //if (self.TryGetComponent<CharacterMaster>(out var characterMaster) && self.TryGetComponent<TemporaryEquipment>(out var temporaryEquipment) && !temporaryEquipment.passThrough)
            //{
            //    orig(self, equipmentState, slot);
            //    temporaryEquipment.realEquipmentStateSlots.Clear();
            //    temporaryEquipment.realEquipmentStateSlots.AddRange(self.equipmentStateSlots);
            //    return false;
            //} else { 
                return orig(self, equipmentState, slot); // vanilla
            //}
        }

        public void SetTemporaryEquipment(EquipmentIndex equipmentIndex)
        {
            if (TryGetComponent(out CharacterBody body))
            {
                if (body.inventory != null && body.inventory.equipmentStateSlots != null)
                {
                    realEquipmentStateSlots.Clear();
                    realEquipmentStateSlots.AddRange(body.inventory.equipmentStateSlots);
                    if (realEquipmentStateSlots.Count == 0)
                    {
                        body.inventory.SetEquipmentIndex(equipmentIndex);
                    }
                    else
                    {
                        for (uint i = 0; i < realEquipmentStateSlots.Count; i++)
                        {
                            try
                            {
                                passThrough = true;
                                body.inventory.SetEquipmentIndexForSlot(equipmentIndex, i);
                            }
                            finally
                            {
                                passThrough = false;
                            }
                        }
                    }
                    temporaryEquipmentStateSlots.Clear();
                    temporaryEquipmentStateSlots.AddRange(body.inventory.equipmentStateSlots);
                }
            }
        }

        public void RemoveTemporaryEquipment()
        {
            if (TryGetComponent(out CharacterBody body))
            {
                if (body.inventory != null)
                {
                    if (realEquipmentStateSlots.Count == 0)
                    {
                        passThrough = true;
                        body.inventory.SetEquipmentIndex(EquipmentIndex.None);
                        passThrough = false;
                    }
                    else
                    {
                        for (uint i = 0; i < realEquipmentStateSlots.Count; i++)
                        {
                            try
                            {
                                passThrough = true;
                                body.inventory.SetEquipment(realEquipmentStateSlots[(int)i], i);
                            }
                            finally
                            {
                                passThrough = false;
                            }
                        }
                    }
                    temporaryEquipmentStateSlots.Clear();
                }
            }
        }

        private void OnEnable()
        {
            if (TryGetComponent(out CharacterBody body))
            {
                if(body.inventory != null && body.inventory.equipmentStateSlots != null) { 
                    realEquipmentStateSlots.AddRange(body.inventory.equipmentStateSlots);
                }
            }
        }

        private void OnDisable()
        {
            RemoveTemporaryEquipment();
        }
    }
}
