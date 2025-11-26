using BepInEx;
using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Path = System.IO.Path;

namespace ServerSideItems
{
    public class BepConfig
    {
        public static ConfigEntry<bool> Enabled { get; set; }
        public static ConfigEntry<bool> ShatterspleenWorksOnBaseHealth;
        public static ConfigEntry<bool> ImplementBeyondTheLimits;
        public static ConfigEntry<bool> NewlyHatchedZoeaRework;
        private static LanguageAPI.LanguageOverlay NewlyHatchedZoeaReworkOverlay;
        private static ItemTag[] originalItemTags = null;

        public static void Init()
        {
            var config = ServerSideItems.instance.Config;

            Enabled = config.Bind("Main", "Enabled", true, "Enable Mod");
            // --- ShatterspleenWorksOnBaseHealth ---
            ShatterspleenWorksOnBaseHealth = config.Bind("Main", "Shatterspleen: Works on Base Health instead of Combined Health", true, new ConfigDescription("Shatterspleen normally applies dmg based on the combined max health of enemies. This makes it super strong if enemies have health increasing items."));
            // --- NewlyHatchedZoeaRework ---
            NewlyHatchedZoeaRework = config.Bind("Main", "NewlyHatchedZoea: Instead of summoning void creature, you become a creature of the void.", true, new ConfigDescription("Every 5 (-25% per stack) seconds, fire 3 (+3 per stack) nullifying bombs which immobilize enemies and deal 380% damage. Auto-targets strongest grounded enemy. Corrupts all yellow items."));
            NewlyHatchedZoeaRework.SettingChanged += SetLanguageOverlay;
            SetLanguageOverlay();
            // --- Beyond The Limits ---
            // ImplementBeyondTheLimits = config.Bind("Beyond The Limits", "SectionEnabled", false, new ConfigDescription("Makes it so that the wearer of the unused 'Beyond The Limits' aspect gains a passive 50% movement speed bonus and relaxes for the wearer the requirement for activation of certain items from 'when sprinting' to 'when moving'."));

            if (ModCompatibilityInLobbyConfig.enabled)
            {
                ModCompatibilityInLobbyConfig.CreateFromBepInExConfigFile(config, "Server-Side Items");
            }
        }



        private static void SetLanguageOverlay(object sender = null, System.EventArgs e = null)
        {
            var updateAction = delegate () {
                var itemIndex = DLC1Content.Items.VoidMegaCrabItem.itemIndex;
                var itemDef = ItemCatalog.GetItemDef(DLC1Content.Items.VoidMegaCrabItem.itemIndex);
                if (NewlyHatchedZoeaRework.Value)
                {
                    // update item tag
                    originalItemTags = itemDef.tags;
                    itemDef.tags = itemDef.tags.Where(s => s != ItemTag.CannotCopy).ToArray();
                    // update description
                    var path = Path.Combine(Path.GetDirectoryName(ServerSideItems.PInfo.Location), "NewlyHatchedZoea.json");
                    NewlyHatchedZoeaReworkOverlay = LanguageAPI.AddOverlayPath(path);
                }
                else
                {
                    if(originalItemTags != null)
                    {
                        itemDef.tags = originalItemTags;
                    }
                    if (NewlyHatchedZoeaReworkOverlay != null)
                    {
                        NewlyHatchedZoeaReworkOverlay.Remove();
                    }
                }
                if (ModCompatibilityLookingGlass.enabled)
                {
                    ModCompatibilityLookingGlass.UpdateNewlyHatchedZoeaDescription(NewlyHatchedZoeaRework.Value);
                }
            };
            if (ServerSideItems.instance.canUpdateItemDescriptions)
            {
                updateAction.Invoke();
            }
            else
            {
                ServerSideItems.instance.newlyHatchedZoeaUpdate = updateAction;
            }
        }
    }
}