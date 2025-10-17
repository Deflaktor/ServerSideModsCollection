using MonoMod.Cil;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ServerSideItems
{
    internal class ShatterSpleenTweak
    {

        public void Init()
        {
            
        }

        public void Hook()
        {
            IL.RoR2.GlobalEventManager.OnCharacterDeath += GlobalEventManager_OnCharacterDeath;
        }

        public void Unhook()
        {
            IL.RoR2.GlobalEventManager.OnCharacterDeath -= GlobalEventManager_OnCharacterDeath;
        }

        private void GlobalEventManager_OnCharacterDeath(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            var label = c.DefineLabel();
            // IL_114f: callvirt instance float32 RoR2.CharacterBody::get_maxHealth()
            c.GotoNext(
            x => x.MatchCallvirt<CharacterBody>("get_maxHealth")
            );
            c.Remove();
            c.EmitDelegate<Func<CharacterBody, float>>((body) =>
            {
                if (BepConfig.Enabled.Value && BepConfig.ShatterspleenWorksOnBaseHealth.Value)
                {
                    return Math.Min(body.baseMaxHealth + body.levelMaxHealth * (body.level - 1f), body.maxHealth);
                }
                else
                {
                    return body.maxHealth;
                }
            });
        }

    }
}
