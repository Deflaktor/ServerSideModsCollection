using BepInEx;
using HG;
using RoR2;
using RoR2.Navigation;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RandomEvents
{
    public class Helper
    {

        public static Vector3? RaycastToCeiling(Vector3 position, float maxDistance, bool lenient)
        {
            return RaycastTo(position, Vector3.up, maxDistance, lenient);
        }

        public static Vector3? RaycastToFloor(Vector3 position, float maxDistance, bool lenient)
        {
            return RaycastTo(position, Vector3.down, maxDistance, lenient);
        }


        private static Vector3? RaycastTo(Vector3 position, Vector3 direction, float maxDistance, bool lenient)
        {
            // only works up or down
            Vector3 lowestY = new Vector3(position.x, float.MaxValue, position.z);
            Vector3 shiftedPosition = new Vector3(position.x, position.y, position.z);
            bool foundCeiling = false;
            // 0,0
            {
                if (Physics.Raycast(new Ray(shiftedPosition, direction), out var hitInfo, maxDistance, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
                {
                    foundCeiling = true;
                    if (hitInfo.point.y < lowestY.y)
                        lowestY.y = hitInfo.point.y;
                }
            }
            if (lenient)
            {
                // 5,0
                shiftedPosition.x = position.x + 5f;
                {
                    if (Physics.Raycast(new Ray(shiftedPosition, direction), out var hitInfo, maxDistance, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
                    {
                        foundCeiling = true;
                        if (hitInfo.point.y < lowestY.y)
                            lowestY.y = hitInfo.point.y;
                    }
                }
                // -5,0
                shiftedPosition.x = position.x - 5f;
                {
                    if (Physics.Raycast(new Ray(shiftedPosition, direction), out var hitInfo, maxDistance, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
                    {
                        foundCeiling = true;
                        if (hitInfo.point.y < lowestY.y)
                            lowestY.y = hitInfo.point.y;
                    }
                }
                // 0,5
                shiftedPosition.x = position.x;
                shiftedPosition.z = position.z + 5f;
                {
                    if (Physics.Raycast(new Ray(shiftedPosition, direction), out var hitInfo, maxDistance, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
                    {
                        foundCeiling = true;
                        if (hitInfo.point.y < lowestY.y)
                            lowestY.y = hitInfo.point.y;
                    }
                }
                // 0,-5
                shiftedPosition.z = position.z - 5f;
                {
                    if (Physics.Raycast(new Ray(shiftedPosition, direction), out var hitInfo, maxDistance, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
                    {
                        foundCeiling = true;
                        if (hitInfo.point.y < lowestY.y)
                            lowestY.y = hitInfo.point.y;
                    }
                }
            }
            if (foundCeiling)
                return lowestY;
            return null;
        }


        private static Quaternion GetRotationFacingTargetPositionFromPoint(Vector3 targetPosition, Vector3 point)
        {
            point.y = targetPosition.y;
            return Util.QuaternionSafeLookRotation(targetPosition - point);
        }

        public static List<GameObject> ApproximatePlacement(GameObject prefab, Vector3 targetPosition, float minDistance, float maxDistance, int amount = 1, MapNodeGroup.GraphType nodeGraphType = MapNodeGroup.GraphType.Ground, HullClassification hullSize = HullClassification.Human, bool occupyPosition = true, NodeFlags requiredNodeFlags = NodeFlags.None, NodeFlags forbiddenNodeFlags = NodeFlags.NoCharacterSpawn, bool preventOverhead = true)
        {
            var placedObjects = new List<GameObject>();
            DirectorCore instance = DirectorCore.instance;
            Xoroshiro128Plus rng = Run.instance.runRNG;
            var nodeGraph = SceneInfo.instance.GetNodeGraph(nodeGraphType);
            List<NodeGraph.NodeIndex> list = nodeGraph.FindNodesInRangeWithFlagConditions(targetPosition, minDistance, maxDistance, (HullMask)(1 << (int)hullSize), requiredNodeFlags, forbiddenNodeFlags, preventOverhead);
            while (list.Count > 0)
            {
                int index = rng.RangeInt(0, list.Count);
                NodeGraph.NodeIndex nodeIndex = list[index];
                if (nodeGraph.GetNodePosition(nodeIndex, out var position) && CheckPositionFree(nodeGraph, nodeIndex, hullSize, nodeGraphType))
                {
                    Quaternion rotation = GetRotationFacingTargetPositionFromPoint(targetPosition, position);
                    GameObject gameObject = GameObject.Instantiate(prefab, position, rotation);
                    NetworkServer.Spawn(gameObject);
                    placedObjects.Add(gameObject);
                    if (occupyPosition)
                    {
                        instance.AddOccupiedNode(nodeGraph, nodeIndex);
                    }
                    if (placedObjects.Count >= amount)
                    {
                        break;
                    }
                }
                list.RemoveAt(index);
            }
            return placedObjects;
        }

        private static bool CheckPositionFree(NodeGraph nodeGraph, NodeGraph.NodeIndex nodeIndex, HullClassification hullSize, MapNodeGroup.GraphType nodeGraphType)
        {
            DirectorCore instance = DirectorCore.instance;
            if (Array.IndexOf(value: new DirectorCore.NodeReference(nodeGraph, nodeIndex), array: instance.occupiedNodes) != -1)
            {
                return false;
            }
            float num = HullDef.Find(hullSize).radius * 0.7f;
            nodeGraph.GetNodePosition(nodeIndex, out var position);
            if (nodeGraphType == MapNodeGroup.GraphType.Ground)
            {
                position += Vector3.up * (num + 0.25f);
            }
            return !HGPhysics.DoesOverlapSphere(position, num, (int)LayerIndex.world.mask | (int)LayerIndex.CommonMasks.characterBodiesOrDefault | (int)LayerIndex.CommonMasks.fakeActorLayers);
        }

        public struct PlayersInRadiusResult {
            public List<TeamComponent> playersInRadius;
            public List<TeamComponent> playersNotInRadius;
        }

        public static PlayersInRadiusResult GetPlayersInRadius(HoldoutZoneController.HoldoutZoneShape holdoutZoneShape, Vector3 origin, float radius, TeamIndex teamIndex)
        {
            ReadOnlyCollection<TeamComponent> teamMembers = TeamComponent.GetTeamMembers(teamIndex);
            List<TeamComponent> teamMembersInRadius = new List<TeamComponent>();
            List<TeamComponent> teamMembersNotInRadius = new List<TeamComponent>();
            for (int i = 0; i < teamMembers.Count; i++)
            {
                TeamComponent teamComponent = teamMembers[i];
                if (teamComponent.body.isPlayerControlled)
                {
                    if (IsPointInRadius(holdoutZoneShape, origin, radius, teamComponent.body.corePosition))
                    {
                        teamMembersInRadius.Add(teamComponent);
                    }
                    else
                    {
                        teamMembersNotInRadius.Add(teamComponent);
                    }
                }
            }
            var result = new PlayersInRadiusResult();
            result.playersInRadius = teamMembersInRadius;
            result.playersNotInRadius = teamMembersNotInRadius;
            return result;
        }

        public static bool IsPointInRadius(HoldoutZoneController.HoldoutZoneShape holdoutZoneShape, Vector3 origin, float radius, Vector3 point)
        {
            float radiusSqr = radius * radius;
            switch (holdoutZoneShape)
            {
                case HoldoutZoneController.HoldoutZoneShape.Sphere:
                    if ((point - origin).sqrMagnitude <= radiusSqr)
                    {
                        return true;
                    }
                    break;
                case HoldoutZoneController.HoldoutZoneShape.VerticalTube:
                    point.y = 0f;
                    origin.y = 0f;
                    if ((point - origin).sqrMagnitude <= radiusSqr)
                    {
                        return true;
                    }
                    break;
            }
            return false;
        }

        private static int FindSkillSlotIndex(BodyIndex bodyIndex, SkillFamily skillFamily)
        {
            GenericSkill[] bodyPrefabSkillSlots = BodyCatalog.GetBodyPrefabSkillSlots(bodyIndex);
            for (int i = 0; i < bodyPrefabSkillSlots.Length; i++)
            {
                if (bodyPrefabSkillSlots[i].skillFamily == skillFamily)
                {
                    return i;
                }
            }
            return -1;
        }

        private static int FindVariantIndex(SkillFamily skillFamily, SkillDef skillDef)
        {
            SkillFamily.Variant[] variants = skillFamily.variants;
            for (int i = 0; i < variants.Length; i++)
            {
                if (variants[i].skillDef == skillDef)
                {
                    return i;
                }
            }
            return -1;
        }
        public static SkillFamily FindSkillByFamilyName(string skillFamilyName)
        {
            for (int i = 0; i < SkillCatalog._allSkillFamilies.Length; i++)
            {
                if (SkillCatalog.GetSkillFamilyName(SkillCatalog._allSkillFamilies[i].catalogIndex) == skillFamilyName)
                {
                    return SkillCatalog._allSkillFamilies[i];
                }
            }
            return null;
        }

        public static bool HasSkillVariantEnabled(Loadout loadout, BodyIndex bodyIndex, SkillFamily skillFamily, SkillDef skillDef)
        {
            int num = FindSkillSlotIndex(bodyIndex, skillFamily);
            int num2 = FindVariantIndex(skillFamily, skillDef);
            if (num == -1 || num2 == -1)
            {
                return false;
            }
            return loadout.bodyLoadoutManager.GetSkillVariant(bodyIndex, num) == num2;
        }

        public static T GetRandom<T>(List<T> list, T defaultValue)
        {
            if (list == null || list.Count == 0)
            {
                return defaultValue;
            }
            return list[UnityEngine.Random.RandomRangeInt(0, list.Count)];
        }
        protected static Dictionary<string, AsyncOperationHandle<BasicPickupDropTable>> dropTables = new Dictionary<string, AsyncOperationHandle<BasicPickupDropTable>>();

        public static void InitDropTables()
        {
            dropTables.Clear();
            dropTables.Add("dtMonsterTeamTier1Item", Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/Base/MonsterTeamGainsItems/dtMonsterTeamTier1Item.asset"));
            dropTables.Add("dtMonsterTeamTier2Item", Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/Base/MonsterTeamGainsItems/dtMonsterTeamTier2Item.asset"));
            dropTables.Add("dtMonsterTeamTier3Item", Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/Base/MonsterTeamGainsItems/dtMonsterTeamTier3Item.asset"));
            dropTables.Add("dtSacrificeArtifact", Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/Base/Sacrifice/dtSacrificeArtifact.asset"));
            dropTables.Add("dtAISafeTier1Item", Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/Base/Common/dtAISafeTier1Item.asset"));
            dropTables.Add("dtAISafeTier2Item", Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/Base/Common/dtAISafeTier2Item.asset"));
            dropTables.Add("dtAISafeTier3Item", Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/Base/Common/dtAISafeTier3Item.asset"));
            dropTables.Add("dtEquipment", Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/Base/Common/dtEquipment.asset"));
            dropTables.Add("dtTier1Item", Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/Base/Common/dtTier1Item.asset"));
            dropTables.Add("dtTier2Item", Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/Base/Common/dtTier2Item.asset"));
            dropTables.Add("dtTier3Item", Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/Base/Common/dtTier3Item.asset"));
            dropTables.Add("dtVoidChest", Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/Base/Common/dtVoidChest.asset"));
            dropTables.Add("dtCasinoChest", Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/Base/CasinoChest/dtCasinoChest.asset"));
            dropTables.Add("dtSmallChestDamage", Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/Base/CategoryChest/dtSmallChestDamage.asset"));
            dropTables.Add("dtSmallChestHealing", Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/Base/CategoryChest/dtSmallChestHealing.asset"));
            dropTables.Add("dtSmallChestUtility", Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/Base/CategoryChest/dtSmallChestUtility.asset"));
            dropTables.Add("dtChest1", Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/Base/Chest1/dtChest1.asset"));
            dropTables.Add("dtChest2", Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/Base/Chest2/dtChest2.asset"));
            dropTables.Add("dtDuplicatorTier1", Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/Base/Duplicator/dtDuplicatorTier1.asset"));
            dropTables.Add("dtDuplicatorTier2", Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/Base/DuplicatorLarge/dtDuplicatorTier2.asset"));
            dropTables.Add("dtDuplicatorTier3", Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/Base/DuplicatorMilitary/dtDuplicatorTier3.asset"));
            dropTables.Add("dtDuplicatorWild", Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/Base/DuplicatorWild/dtDuplicatorWild.asset"));
            dropTables.Add("dtGoldChest", Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/Base/GoldChest/dtGoldChest.asset"));
            dropTables.Add("dtLunarChest", Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/Base/LunarChest/dtLunarChest.asset"));
            dropTables.Add("dtShrineChance", Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/Base/ShrineChance/dtShrineChance.asset"));
            dropTables.Add("dtLockbox", Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/Base/TreasureCache/dtLockbox.asset"));
            dropTables.Add("dtITBossWave", Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/DLC1/GameModes/InfiniteTowerRun/InfiniteTowerAssets/dtITBossWave.asset"));
            dropTables.Add("dtITDefaultWave", Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/DLC1/GameModes/InfiniteTowerRun/InfiniteTowerAssets/dtITDefaultWave.asset"));
            dropTables.Add("dtITLunar", Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/DLC1/GameModes/InfiniteTowerRun/InfiniteTowerAssets/dtITLunar.asset"));
            dropTables.Add("dtITSpecialBossWave", Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/DLC1/GameModes/InfiniteTowerRun/InfiniteTowerAssets/dtITSpecialBossWave.asset"));
            dropTables.Add("dtITVoid", Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/DLC1/GameModes/InfiniteTowerRun/InfiniteTowerAssets/dtITVoid.asset"));
            dropTables.Add("dtCategoryChest2Damage", Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/DLC1/CategoryChest2/dtCategoryChest2Damage.asset"));
            dropTables.Add("dtCategoryChest2Healing", Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/DLC1/CategoryChest2/dtCategoryChest2Healing.asset"));
            dropTables.Add("dtCategoryChest2Utility", Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/DLC1/CategoryChest2/dtCategoryChest2Utility.asset"));
            dropTables.Add("dtVoidCamp", Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/DLC1/VoidCamp/dtVoidCamp.asset"));
            dropTables.Add("dtVoidTriple", Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/DLC1/VoidTriple/dtVoidTriple.asset"));
            dropTables.Add("dtVoidLockbox", Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/DLC1/TreasureCacheVoid/dtVoidLockbox.asset"));
            dropTables.Add("AurelioniteHeartPickupDropTable", Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/DLC2/AurelioniteHeartPickupDropTable.asset"));
            dropTables.Add("GeodeRewardDropTable", Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/DLC2/GeodeRewardDropTable.asset"));
            dropTables.Add("dtShrineHalcyoniteTier1", Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/DLC2/dtShrineHalcyoniteTier1.asset"));
            dropTables.Add("dtShrineHalcyoniteTier2", Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/DLC2/dtShrineHalcyoniteTier2.asset"));
            dropTables.Add("dtShrineHalcyoniteTier3", Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/DLC2/dtShrineHalcyoniteTier3.asset"));
            dropTables.Add("dtChanceDoll", Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/DLC2/Items/ExtraShrineItem/dtChanceDoll.asset"));
            dropTables.Add("dtSonorousEcho", Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/DLC2/Items/ItemDropChanceOnKill/dtSonorousEcho.asset"));
            dropTables.Add("dtCommandChest", Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/CommandChest/dtCommandChest.asset"));
        }

        public static void AddItemsToDictionaryFromStringList(Dictionary<PickupIndex, int> resolvedItems, List<string> itemKeyCountStrings, int index = -1)
        {
            PickupIndex singlePickupIndex = PickupIndex.none;
            int amount = 1;

        TryAgain:

            if (itemKeyCountStrings.Count > 0)
            {
                if (index < 0)
                {
                    index = UnityEngine.Random.RandomRangeInt(0, itemKeyCountStrings.Count);
                }
                var entry = itemKeyCountStrings[index].Trim().Split("=");
                string itemkey;
                if (entry.Length == 1)
                {
                    itemkey = entry[0];
                }
                else if (entry.Length == 2)
                {
                    itemkey = entry[0].Trim();
                    if (!int.TryParse(entry[1].Trim(), out amount))
                    {
                        Log.LogError($"Could not properly parse item amount: {entry}");
                    }
                }
                else
                {
                    Log.LogError($"Could not properly parse item key: {entry}");
                    itemkey = entry[0];
                }

                var isItemTier = true;
                switch (itemkey.ToLower())
                {
                    case "tier1":
                        singlePickupIndex = GetRandom(Run.instance.availableTier1DropList, PickupIndex.none);
                        break;
                    case "tier2":
                        singlePickupIndex = GetRandom(Run.instance.availableTier2DropList, PickupIndex.none);
                        break;
                    case "tier3":
                        singlePickupIndex = GetRandom(Run.instance.availableTier3DropList, PickupIndex.none);
                        break;
                    case "boss":
                        singlePickupIndex = GetRandom(Run.instance.availableBossDropList, PickupIndex.none);
                        break;
                    case "lunar":
                        singlePickupIndex = GetRandom(Run.instance.availableLunarCombinedDropList, PickupIndex.none);
                        break;
                    case "voidtier1":
                        singlePickupIndex = GetRandom(Run.instance.availableVoidTier1DropList, PickupIndex.none);
                        break;
                    case "voidtier2":
                        singlePickupIndex = GetRandom(Run.instance.availableVoidTier2DropList, PickupIndex.none);
                        break;
                    case "voidtier3":
                        singlePickupIndex = GetRandom(Run.instance.availableVoidTier3DropList, PickupIndex.none);
                        break;
                    case "voidboss":
                        singlePickupIndex = GetRandom(Run.instance.availableVoidBossDropList, PickupIndex.none);
                        break;
                    default:
                        isItemTier = false;
                        break;
                }
                if (isItemTier)
                {
                    if (singlePickupIndex == PickupIndex.none)
                    {
                        Log.LogWarning($"ResolveItemsFromStringList: Could not get pickup from item tier: {itemkey}, skipping it.");
                        itemKeyCountStrings.RemoveAt(index);
                        goto TryAgain;
                    }
                }
                else
                {
                    if (dropTables.ContainsKey(itemkey))
                    {
                        var dropTable = dropTables[itemkey].WaitForCompletion();
                        var pickupIndices = dropTable.GenerateUniqueDrops(amount, new Xoroshiro128Plus(Run.instance.stageRng.nextUlong));
                        foreach(var generatedPickupIndex in pickupIndices)
                        {
                            resolvedItems[generatedPickupIndex] = resolvedItems.GetValueOrDefault(generatedPickupIndex) + 1;
                        }
                    }
                    else
                    {
                        ItemIndex itemIndex = ItemCatalog.FindItemIndex(itemkey);
                        if (itemIndex != ItemIndex.None)
                        {
                            singlePickupIndex = PickupCatalog.FindPickupIndex(itemIndex);
                        }
                        else
                        {
                            EquipmentIndex equipmentIndex = EquipmentCatalog.FindEquipmentIndex(itemkey);
                            if (equipmentIndex != EquipmentIndex.None)
                            {
                                singlePickupIndex = PickupCatalog.FindPickupIndex(equipmentIndex);
                            }
                            else
                            {
                                singlePickupIndex = PickupIndex.none;
                                Log.LogError($"ResolveItemsFromStringList: Could not find item key: {itemkey}");
                            }
                        }
                    }
                }
            }

            if (singlePickupIndex != PickupIndex.none) {
                resolvedItems[singlePickupIndex] = resolvedItems.GetValueOrDefault(singlePickupIndex) + amount;
            }
        }
    }
}
