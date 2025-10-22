using BepInEx;
using BepInEx.Configuration;
using HG;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Newtonsoft.Json;
using RoR2;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.UIElements;
using static RoR2.GenericPickupController;
using static RoR2.SpawnCard;

namespace RandomEvents
{
    public class EventMonsterTransform : AbstractEvent
    {
        private static ConfigEntry<bool> CustomBodyListEnabled;
        private static ConfigEntry<string> CustomBodyList;
        private static ConfigEntry<bool> CustomBodyBonusItemsListsEnabled;
        private static ConfigEntry<string> CustomBodyBonusItemsAll;
        private static Dictionary<BodyIndex, ConfigEntry<string>> CustomBodyBonusItems = new Dictionary<BodyIndex, ConfigEntry<string>>();
        private static ConfigEntry<bool> AllowFlyingMonsters;

        public List<CharacterBody> validBodyPrefabs = new List<CharacterBody>();

        private static Dictionary<string, string> BodyBonusItemsDict = new Dictionary<string, string>
            {
                { "All", "Hoof=5, BoostAttackSpeed=10, BoostDamage=10, Knurl=10" },
                { "ClayBossBody",        "AlienHead=5, BoostDamage=10, SiphonOnLowHealth" },
                { "GravekeeperBody",     "AlienHead=5, BoostDamage=30, SprintWisp" },
                { "ImpBossBody",         "AlienHead=5" },
                { "MegaConstructBody",   "AlienHead=5, Hoof=-5, Behemoth=10, MinorConstructOnKill" },
                { "RoboBallBossBody",    "AlienHead=5, BoostDamage=20, RoboBallBuddy" },
                { "TitanBody",           "AlienHead=5, BoostDamage=80, Hoof=5, Knurl=10" },
                { "VagrantBody",         "AlienHead=2, BoostDamage=10, Hoof=5" },
                { "VoidMegaCrabBody",    "AlienHead=2" },
                { "BeetleGuardBody",     "AlienHead=5, SprintWisp, BoostAttackSpeed=10, Hoof=-5, Feather=3, NearbyDamageBonus=10" },
                { "BellBody",            "AlienHead=5, BoostDamage=30, BoostAttackSpeed=10, Hoof=5" },
                { "BisonBody",           "AlienHead=5, SprintWisp, SprintBonus=15, BoostHp=10, NearbyDamageBonus=100, Hoof=5" },
                { "ClayBruiserBody",     "AlienHead=5" },
                { "ClayGrenadierBody",   "AlienHead=10, BoostAttackSpeed=10" },
                { "GolemBody",           "AlienHead=10, NearbyDamageBonus=10, BoostDamage=50, BoostAttackSpeed=10, Knurl=10" },
                { "GreaterWispBody",     "AlienHead=5, BoostDamage=50, BoostAttackSpeed=20, Hoof=5" },
                { "GupBody",             "AlienHead=5, NearbyDamageBonus=10, BoostDamage=10, Feather=4, FallBoots, DrizzlePlayerHelper " },
                { "LemurianBruiserBody", "AlienHead=3, Behemoth=3" },
                { "LunarGolemBody",      "AlienHead=5, BoostAttackSpeed=20, Behemoth=2, Hoof=-5" },
                { "LunarWispBody",       "AlienHead=10, BoostDamage=50, BoostAttackSpeed=10, Behemoth=10, Hoof=5" },
                { "NullifierBody",       "AlienHead=5, BoostDamage=10, Hoof=5, SprintWisp, ShockNearby" },
                { "ParentBody",          "AlienHead=5, NearbyDamageBonus=20, SprintWisp" },
                { "VoidJailerBody",      "AlienHead=5" },
                { "HalcyoniteBody",      "Knurl=10, BoostDamage=30" },
                { "BeetleBody",          "AlienHead=5, BoostDamage=10, NearbyDamageBonus=50, SprintWisp, Hoof=5, Feather=5, LunarSecondaryReplacement" },
                { "FlyingVerminBody",    "AlienHead=5, BoostDamage=40, BounceNearby=10, Hoof=5" },
                { "ImpBody",             "AlienHead=5, NearbyDamageBonus=10, SprintWisp" },
                { "JellyfishBody",       "NearbyDamageBonus=100, Hoof=5, BoostAttackSpeed=-10, BoostDamage=-10, Knurl=-10" },
                { "LemurianBody",        "AlienHead=5, BoostDamage=50, Feather=1, Behemoth=1, LunarSecondaryReplacement" },
                { "WispBody",            "AlienHead=10, BoostDamage=100, BoostAttackSpeed=10, Hoof=25, Behemoth=3" },
                { "LunarExploderBody",   "AlienHead=10, ChainLightning=5, BoostDamage=50, Feather=1" },
                { "MiniMushroomBody",    "AlienHead=10, Mushroom=10, BoostAttackSpeed=30, BoostDamage=20, Feather=3, SprintWisp, FireballsOnHit" },
                { "RoboBallMiniBody",    "AlienHead=5, ChainLightning=5, BoostDamage=50, Hoof=10, Behemoth=4" },
                { "VerminBody",          "AlienHead=5, NearbyDamageBonus=30, SprintWisp, Seed=50" },
                { "VultureBody",         "AlienHead=5, BoostDamage=50, BoostAttackSpeed=20, LightningStrikeOnHit, ChainLightning" },
                { "ChildBody",           "AlienHead=5, BoostDamage=70, PrimarySkillShuriken, BoostAttackSpeed=-10, Behemoth=5, FireballsOnHit, Feather" },
                { "ITBrotherBody",       "AlienHead=5, Hoof=-5" },
            };

        public override bool LoadCondition()
        {
            return true;
        }
        public override bool Condition(List<AbstractEvent> activeOtherEvents)
        {
            if (Run.instance is InfiniteTowerRun infiniteTowerRun)
            {
                // not for boss waves
                if ((infiniteTowerRun.waveIndex + 1) % 5 == 0)
                {
                    return false;
                }
            }
            return !activeOtherEvents.Any(e => e.GetEventConfigName().Equals("SkillsOnly", StringComparison.InvariantCultureIgnoreCase));
        }
        public override string GetEventConfigName()
        {
            return "MonsterTransform";
        }
        public override string GetAnnouncement()
        {
            return Language.GetString("ANNOUNCE_EVENT_MONSTER_TRANSFORM");
        }
        public override string GetDescription()
        {
            return "Players transform into monsters.";
        }
        public override string GetConditionDescription()
        {
            return "Event \"SkillsOnly\" inactive and currently not a boss wave.";
        }
        private static List<string> CalculateDefaultBodyList()
        {
            List<string> bodyStrings = new List<string>();

            var dccsMixEnemy = Addressables.LoadAssetAsync<DirectorCardCategorySelection>("RoR2/Base/MixEnemy/dccsMixEnemy.asset");
            foreach (var category in dccsMixEnemy.WaitForCompletion().categories)
            {
                foreach (var card in category.cards) {
                    var masterGameObject = card.spawnCard.prefab;
                    if(masterGameObject.TryGetComponent(out CharacterMaster master))
                    {
                        if(master.bodyPrefab.GetComponent<WormBodyPositionsDriver>() == null)
                        {
                            if (master.bodyPrefab.TryGetComponent(out CharacterBody characterBody)) { 
                                if(characterBody.baseMoveSpeed > 0 && characterBody.baseDamage > 0 && characterBody.baseMaxHealth > 0 && characterBody.baseAttackSpeed > 0) {
                                    bodyStrings.Add(master.bodyPrefab.name);
                                }
                            }
                        }
                    }
                }
            }
            bodyStrings.Remove("AcidLarvaBody");
            bodyStrings.Remove("BeetleQueen2Body");
            bodyStrings.Remove("ScavBody");
            bodyStrings.Remove("ScorchlingBody");
            // bodyStrings.Remove("JellyfishBody");
            bodyStrings.AddRange(["ITBrotherBody"]);

            //string[] survivorBodyNames = SurvivorCatalog.allSurvivorDefs.Select(survivor => survivor.bodyPrefab.name).ToArray();
            //string[] blacklist = new string[]
            //{
            //    "AcidLarvaBody", "AncientWispBody", "Assassin2Body", "AssassinBody", "BackupDroneOldBody", "BanditBody"
            //};
            //foreach (var body in BodyCatalog.allBodyPrefabs)
            //{
            //    if(survivorBodyNames.Contains(body.name) || blacklist.Contains(body.name))
            //    {
            //        continue;
            //    }
            //    var characterBody = body.GetComponent<CharacterBody>();

            //    if (characterBody.baseMoveSpeed > 0 && characterBody.baseDamage > 0 && characterBody.baseMaxHealth > 0 && characterBody.baseAttackSpeed > 0)
            //    {
            //        bodyStrings.Add(characterBody.name);
            //    }
            //}
            return bodyStrings;
        }

        private int TitanicKnurls(int level, CharacterBody preferredCharacterBody, CharacterBody transformedBody)
        {
            var originalSpeed = preferredCharacterBody.baseMoveSpeed + preferredCharacterBody.levelMoveSpeed * (level - 1);
            var transformedSpeed = transformedBody.baseMoveSpeed + transformedBody.levelMoveSpeed * (level - 1);
            var hoofSpeedBonus = 0.14f;
            // transformedSpeed + x * 0.14 = originalSpeed
            // x = (originalSpeed - transformedSpeed) / 0.14
            if (transformedSpeed < originalSpeed)
            {
                return (int)Math.Ceiling((originalSpeed - transformedSpeed) / hoofSpeedBonus);
            }
            return 0;
        }

        private static string GetBodyList()
        {
            if (CustomBodyListEnabled.Value)
            {
                return CustomBodyList.Value;
            }
            else
            {
                return string.Join(", ", CalculateDefaultBodyList());
            }
        }

        private static string GetBodyBonusItemAllList()
        {
            if (CustomBodyBonusItemsListsEnabled.Value)
            {
                return CustomBodyBonusItemsAll.Value;
            }
            else
            {
                return BodyBonusItemsDict["All"];
            }
        }
        private static string GetBodyBonusItemList(BodyIndex bodyIndex)
        {
            if (CustomBodyBonusItemsListsEnabled.Value)
            {
                if(CustomBodyBonusItems.ContainsKey(bodyIndex))
                {
                    return CustomBodyBonusItems[bodyIndex].Value;
                }
            }
            else
            {
                var bodyPrefab = BodyCatalog.GetBodyPrefab(bodyIndex);
                if (bodyPrefab != null)
                {
                    return BodyBonusItemsDict.GetValueOrDefault(bodyPrefab.name);
                }
            }
            return "";
        }

        protected override void AddConfig(ConfigFile config)
        {
            CustomBodyListEnabled = config.Bind(GetEventConfigName(), "CustomBodyListEnabled", false, "Whether to use the custom body list or to use the default one.");
            CustomBodyList = config.Bind(GetEventConfigName(), "CustomBodyList", string.Join(", ", CalculateDefaultBodyList()), "Comma-separated list of allowed body transforms (see README.md)");
            AllowFlyingMonsters = config.Bind(GetEventConfigName(), "AllowFlyingMonsters", true, "Whether to allow players to transform into flying monsters.");
            CustomBodyListEnabled.SettingChanged += RecalculateValidBodyList;
            CustomBodyList.SettingChanged += RecalculateValidBodyList;
            AllowFlyingMonsters.SettingChanged += RecalculateValidBodyList;

            CustomBodyBonusItemsListsEnabled = config.Bind(GetEventConfigName(), "CustomBodyBonusItemsListsEnabled", false, "Whether to use the custom body bonus item lists or to use the default ones.");
            CustomBodyBonusItemsAll = config.Bind(GetEventConfigName(), "CustomBodyBonusItems_All", BodyBonusItemsDict["All"], $"Items gained temporarily when transformed into any monster. Comma-separated list in the format keyword=amount for rewarding multiple of the item, or just the keyword for single reward. Must be internal item names (see https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Developer-Reference/Items-and-Equipments-Data/)");

            var bodies = CustomBodyList.Value.Split(",");
            foreach (string body in bodies)
            {
                var name = body.Trim();
                if (name.IsNullOrWhiteSpace())
                    continue;
                var bodyIndex = BodyCatalog.FindBodyIndex(name);
                if (bodyIndex == BodyIndex.None)
                {
                    Log.LogError($"Could not find body: {name}");
                }
                string defaultReward = null;

                if(BodyBonusItemsDict.TryGetValue(name, out defaultReward))
                {
                    ConfigEntry<string> bodyBonusItemList = config.Bind(GetEventConfigName(), $"CustomBodyBonusItems_{name}", defaultReward, $"Items gained temporarily when transformed into {name}. Comma-separated list like above.");
                    CustomBodyBonusItems.Add(bodyIndex, bodyBonusItemList);
                }
            }
        }

        private void RecalculateValidBodyList(object sender = null, EventArgs e = null)
        {
            validBodyPrefabs.Clear();
            foreach (var bodyStr in GetBodyList().Split(","))
            {
                var bodyPrefab = BodyCatalog.FindBodyPrefab(bodyStr.Trim());
                if (bodyPrefab != null)
                {
                    var characterBody = bodyPrefab.GetComponent<CharacterBody>();
                    if(AllowFlyingMonsters.Value || !characterBody.isFlying)
                    {
                        validBodyPrefabs.Add(characterBody);
                    }
                }
            }
        }

        public override void Preload()
        {
            RecalculateValidBodyList();
        }

        public override void Hook()
        {
            On.EntityStates.JellyfishMonster.JellyNova.Detonate += JellyNova_Detonate;
            //IL.RoR2.CharacterMaster.Respawn += CharacterMaster_Respawn;
        }

        private void JellyNova_Detonate(On.EntityStates.JellyfishMonster.JellyNova.orig_Detonate orig, EntityStates.JellyfishMonster.JellyNova self)
        {
            if(self.characterBody != null && self.characterBody.master != null && self.characterBody.master.playerCharacterMasterController != null && self.characterBody.master.playerCharacterMasterController.isConnected && IsActive())
            {
                self.characterBody.AddBuff(DLC2Content.Buffs.ExtraLifeBuff);
            }
            orig(self);
        }

        //private void CharacterMaster_Respawn(MonoMod.Cil.ILContext il)
        //{
        //    ILCursor c = new ILCursor(il);
        //    var label = c.DefineLabel();
        //    // IL_0073: ldarg.0
        //    // IL_0074: ldfld class [UnityEngine.CoreModule]UnityEngine.GameObject RoR2.CharacterMaster::bodyPrefab
        //    // IL_0079: callvirt instance !!0 [UnityEngine.CoreModule] UnityEngine.GameObject::GetComponent<class RoR2.CharacterBody>()
        //    // IL_007e: stloc.0
        //    c.GotoNext(
        //    x => x.MatchLdarg(0),
        //    x => x.MatchLdfld<CharacterMaster>("bodyPrefab"),
        //    x => x.MatchCallvirt<GameObject>("GetComponent"),
        //    x => x.MatchStloc(0)
        //    );
        //    c.Index += 1;
        //    c.Remove();
        //    c.EmitDelegate<Func<CharacterMaster, GameObject>>((master) =>
        //    {
        //        if (ModConfig.Enabled.Value && NetworkServer.active && master.gameObject.TryGetComponent(out MonsterTransformComponent monsterTransformComponent))
        //        {
        //            return monsterTransformComponent.monster.gameObject;
        //        }
        //        return master.bodyPrefab;
        //    });
        //}

        public CharacterBody GetRandomMonster()
        {
            return validBodyPrefabs[UnityEngine.Random.RandomRangeInt(0, validBodyPrefabs.Count)];
        }

        public void TransformIntoRandomMonster(PlayerCharacterMasterController pc)
        {
            TransformInto(pc, GetRandomMonster());
        }

        public void TransformInto(PlayerCharacterMasterController pc, CharacterBody target)
        {
            var monsterTransformComponent = pc.master.gameObject.EnsureComponent<MonsterTransformComponent>();
            monsterTransformComponent.SetMonster(target);
            var body = pc.master.GetBody();
            if (body != null) {
                pc.master.Respawn(body.transform.position, body.transform.rotation);
            }
        }

        public override void Prepare()
        {
        }

        public override void Start(List<AbstractEvent> activeOtherEvents)
        {
            foreach (var pc in PlayerCharacterMasterController.instances)
            {
                if (pc.isConnected)
                {
                    TransformIntoRandomMonster(pc);
                }
            }
        }

        public override void Stop()
        {
            foreach (var pc in PlayerCharacterMasterController.instances)
            {
                if (pc.master != null && pc.master.TryGetComponent(out MonsterTransformComponent monsterTransformComponent))
                {
                    GameObject.Destroy(monsterTransformComponent);
                    var body = pc.master.GetBody();
                    if (body != null) { 
                        pc.master.Respawn(body.transform.position, body.transform.rotation);
                    }
                }
            }
        }

        public class MonsterTransformComponent : MonoBehaviour
        {
            TemporaryInventory inventory = null;
            private CharacterBody monster = null;

            private void FixedUpdate()
            {
                Check();
            }

            private void Check()
            {
                if (monster == null)
                    return;
                var master = GetComponent<CharacterMaster>();
                master.bodyPrefab = monster.gameObject;
                var body = master.GetBody();
                if (body == null)
                    return;
                if (master.playerCharacterMasterController == null)
                    return;
                var preferredBody = BodyCatalog.GetBodyPrefab(master.playerCharacterMasterController.networkUser.bodyIndexPreference);
                if (preferredBody == null)
                    return;
                var preferredCharacterBody = preferredBody.GetComponent<CharacterBody>();
                if (preferredCharacterBody == null)
                    return;

                var myBodyInventory = TemporaryInventory.Find(body.gameObject, inventory);
                if (myBodyInventory == null)
                {
                    if (inventory != null)
                    {
                        Destroy(inventory);
                        inventory = null;
                    }

                    var level = (int)TeamManager.instance.GetTeamLevel(master.teamIndex);
                    inventory = body.gameObject.AddComponent<TemporaryInventory>();

                    Dictionary<PickupIndex, int> resolvedItems = new Dictionary<PickupIndex, int>();
                    var bodyBonusItemsList = GetBodyBonusItemAllList().Split(',').ToList();
                    for (int i = 0; i < bodyBonusItemsList.Count; i++)
                    {
                        Helper.AddItemsToDictionaryFromStringList(resolvedItems, bodyBonusItemsList, i);
                    }
                    var list = GetBodyBonusItemList(body.bodyIndex);
                    if(!list.IsNullOrWhiteSpace()) { 
                        bodyBonusItemsList = list.Split(",").ToList();
                        for (int i = 0; i < bodyBonusItemsList.Count; i++)
                        {
                            Helper.AddItemsToDictionaryFromStringList(resolvedItems, bodyBonusItemsList, i);
                        }
                    }

                    foreach (var item in resolvedItems)
                    {
                        var itemIndex = PickupCatalog.GetPickupDef(item.Key).itemIndex;
                        var itemAmount = item.Value;
                        if (itemIndex != ItemIndex.None && itemAmount > 0) {
                            var noAdditionalItem = RoR2Content.Items.LunarPrimaryReplacement.itemIndex;
                            if (itemIndex == noAdditionalItem && body.inventory.GetItemCount(noAdditionalItem) > 0)
                            {
                                continue;
                            }
                            noAdditionalItem = RoR2Content.Items.LunarSecondaryReplacement.itemIndex;
                            if (itemIndex == noAdditionalItem && body.inventory.GetItemCount(noAdditionalItem) > 0)
                            {
                                continue;
                            }
                            noAdditionalItem = RoR2Content.Items.LunarUtilityReplacement.itemIndex;
                            if (itemIndex == noAdditionalItem && body.inventory.GetItemCount(noAdditionalItem) > 0)
                            {
                                continue;
                            }
                            noAdditionalItem = RoR2Content.Items.LunarSpecialReplacement.itemIndex;
                            if (itemIndex == noAdditionalItem && body.inventory.GetItemCount(noAdditionalItem) > 0)
                            {
                                continue;
                            }
                            inventory.GiveTemporaryItem(itemIndex, itemAmount);
                            PurchaseInteraction.CreateItemTakenOrb(body.transform.position, body.gameObject, itemIndex);
                        }
                    }
                }
            }

            public void SetMonster(CharacterBody monster)
            {
                this.monster = monster;
                Check();
            }

            private void OnDisable()
            {
                var master = GetComponent<CharacterMaster>();
                if(master.playerCharacterMasterController != null) { 
                    master.playerCharacterMasterController.SetBodyPrefabToPreference();
                }
                if (inventory != null) {
                    Destroy(inventory);
                }
            }
        }
    }
}
