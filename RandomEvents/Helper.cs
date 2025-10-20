﻿using HG;
using RoR2;
using RoR2.Navigation;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

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

    }
}
