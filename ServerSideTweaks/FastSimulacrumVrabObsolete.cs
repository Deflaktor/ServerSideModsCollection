using HG;
using R2API;
using RoR2;
using RoR2.Hologram;
using RoR2.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UIElements;

namespace ServerSideTweaks
{
    public class FastSimulacrumVrabObsolete
    {
        private static bool enabled;
        private static AsyncOperationHandle<GameObject> iscInfiniteTowerSafeWard;
        private static AsyncOperationHandle<Material> matNullifierSafeWardPillarGlow;
        private static AsyncOperationHandle<Material> matNullifierSafeWardPillarStars;

        public static void Setup()
        {
            iscInfiniteTowerSafeWard = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/GameModes/InfiniteTowerRun/InfiniteTowerAssets/InfiniteTowerSafeWard.prefab");
            matNullifierSafeWardPillarGlow = Addressables.LoadAssetAsync<Material>("RoR2/DLC1/Common/Void/matNullifierSafeWardPillarGlow.mat");
            matNullifierSafeWardPillarStars = Addressables.LoadAssetAsync<Material>("RoR2/DLC1/Common/Void/matNullifierSafeWardPillarStars.mat");
        }

        public static void Enable()
        {
            enabled = true;
            On.EntityStates.InfiniteTowerSafeWard.Travelling.EvaluateNextEndpoint += Travelling_EvaluateNextEndpoint;
            //On.EntityStates.InfiniteTowerSafeWard.Travelling.UpdateCurveSegmentPoints += Travelling_UpdateCurveSegmentPoints;
        }

        public static void Disable()
        {
            enabled = false;
            On.EntityStates.InfiniteTowerSafeWard.Travelling.EvaluateNextEndpoint -= Travelling_EvaluateNextEndpoint;
            //On.EntityStates.InfiniteTowerSafeWard.Travelling.UpdateCurveSegmentPoints -= Travelling_UpdateCurveSegmentPoints;
        }

        private static void Travelling_EvaluateNextEndpoint(On.EntityStates.InfiniteTowerSafeWard.Travelling.orig_EvaluateNextEndpoint orig, EntityStates.InfiniteTowerSafeWard.Travelling self)
        {
            orig(self);
            if (Run.instance.GetType() == typeof(InfiniteTowerRun))
            {
                InfiniteTowerRun run = (InfiniteTowerRun)Run.instance;

                GameObject corridor = new GameObject("Corridor");
                MeshFilter meshFilter = corridor.AddComponent<MeshFilter>();
                MeshRenderer meshRenderer = corridor.AddComponent<MeshRenderer>();
                CombineInstance[] combine = new CombineInstance[self.catmullRomPoints.Count];

                var radius = 50;
                for (int i = 0; i < self.catmullRomPoints.Count; i++)
                {
                    GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    segment.transform.localScale = new Vector3(radius, 120f, radius);
                    segment.transform.position = self.catmullRomPoints[i];
                    segment.transform.rotation = Quaternion.identity;

                    // Set the combine instance
                    MeshFilter segmentMeshFilter = segment.GetComponent<MeshFilter>();
                    combine[i].mesh = segmentMeshFilter.sharedMesh;
                    combine[i].transform = segmentMeshFilter.transform.localToWorldMatrix;

                    // Optionally, destroy the segment after combining
                    GameObject.Destroy(segment);

                    var safeZone = CreateSafeZone2(self.catmullRomPoints[i], null);
                    run.fogDamageController.AddSafeZone(safeZone);
                }

                Mesh combinedMesh = new Mesh();
                combinedMesh.CombineMeshes(combine, true, true);
                meshFilter.mesh = combinedMesh;

                // Add MeshRenderer and set properties
                meshRenderer.castShadows = true;
                meshRenderer.motionVectors = true;
                meshRenderer.useLightProbes = true;
                meshRenderer.bounds = new Bounds(Vector3.zero, new Vector3(2.00f, 120.00f, 2.00f));
                meshRenderer.localBounds = new Bounds(Vector3.zero, new Vector3(1.00f, 1.00f, 1.00f));
                meshRenderer.enabled = true;
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                meshRenderer.receiveShadows = true;
                meshRenderer.forceRenderingOff = false;
                meshRenderer.staticShadowCaster = false;
                meshRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.Object;
                meshRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.BlendProbes;
                meshRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.BlendProbes;
                meshRenderer.renderingLayerMask = 4294967295;
                meshRenderer.rendererPriority = 0;
                meshRenderer.rayTracingMode = RayTracingMode.DynamicTransform;
                meshRenderer.sortingLayerName = "Default";
                meshRenderer.sortingLayerID = 0;
                meshRenderer.sortingOrder = 0;
                meshRenderer.allowOcclusionWhenDynamic = true;

                // Set lightmap properties
                meshRenderer.lightmapIndex = -1;
                meshRenderer.realtimeLightmapIndex = -1;
                meshRenderer.lightmapScaleOffset = new Vector4(1.00f, 1.00f, 0.00f, 0.00f);
                meshRenderer.realtimeLightmapScaleOffset = new Vector4(1.00f, 1.00f, 0.00f, 0.00f);

                // Set materials
                Material materialInstance = matNullifierSafeWardPillarStars.WaitForCompletion();
                meshRenderer.material = materialInstance;
                meshRenderer.sharedMaterial = materialInstance;

                NetworkServer.Spawn(corridor);
            }
        }


        private static Mesh CreateCylinderMesh()
        {
            // Create a sphere mesh programmatically
            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Mesh mesh = cylinder.GetComponent<MeshFilter>().mesh;

            // Optionally, you can modify the mesh here if needed

            // Destroy the temporary sphere GameObject
            GameObject.Destroy(cylinder);

            return mesh;
        }

        private static GameObject CreateCylinder()
        {
            // Create the GameObject for the IndicatorCylinder
            GameObject indicatorCylinder = new GameObject("IndicatorCylinder");

            // Set the Transform properties
            indicatorCylinder.transform.position = new Vector3(0.00f, 0.00f, 0.00f);
            indicatorCylinder.transform.rotation = Quaternion.Euler(0.00f, 0.00f, 180.00f);
            indicatorCylinder.transform.localScale = new Vector3(2.00f, 120.00f, 2.00f);

            // Add MeshFilter and set the shared mesh to a cylinder
            MeshFilter meshFilter = indicatorCylinder.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = CreateCylinderMesh(); // Assuming CreateCylinderMesh() creates a cylinder mesh
            meshFilter.mesh = meshFilter.sharedMesh;

            // Add MeshRenderer and set properties
            MeshRenderer meshRenderer = indicatorCylinder.AddComponent<MeshRenderer>();
            meshRenderer.castShadows = true;
            meshRenderer.motionVectors = true;
            meshRenderer.useLightProbes = true;
            meshRenderer.bounds = new Bounds(Vector3.zero, new Vector3(2.00f, 120.00f, 2.00f));
            meshRenderer.localBounds = new Bounds(Vector3.zero, new Vector3(1.00f, 1.00f, 1.00f));
            meshRenderer.enabled = true;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            meshRenderer.receiveShadows = true;
            meshRenderer.forceRenderingOff = false;
            meshRenderer.staticShadowCaster = false;
            meshRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.Object;
            meshRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.BlendProbes;
            meshRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.BlendProbes;
            meshRenderer.renderingLayerMask = 4294967295;
            meshRenderer.rendererPriority = 0;
            meshRenderer.rayTracingMode = RayTracingMode.DynamicTransform;
            meshRenderer.sortingLayerName = "Default";
            meshRenderer.sortingLayerID = 0;
            meshRenderer.sortingOrder = 0;
            meshRenderer.allowOcclusionWhenDynamic = true;

            // Set lightmap properties
            meshRenderer.lightmapIndex = -1;
            meshRenderer.realtimeLightmapIndex = -1;
            meshRenderer.lightmapScaleOffset = new Vector4(1.00f, 1.00f, 0.00f, 0.00f);
            meshRenderer.realtimeLightmapScaleOffset = new Vector4(1.00f, 1.00f, 0.00f, 0.00f);

            // Set materials
            Material materialInstance = matNullifierSafeWardPillarStars.WaitForCompletion();
            meshRenderer.material = materialInstance;
            meshRenderer.sharedMaterial = materialInstance;

            // Optionally, set the position of the combined cylinder
            indicatorCylinder.transform.position = Vector3.zero; // Adjust as needed

            return indicatorCylinder;
        }

        private static GameObject CreateSphere()
        {
            // Create the parent GameObject
            GameObject indicatorSphere = new GameObject("IndicatorSphere");

            // Set the Transform properties
            Transform transform = indicatorSphere.transform;
            transform.position = new Vector3(0.0f, 0.0f, 0.0f);
            transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
            transform.localScale = new Vector3(2.0f, 2.0f, 2.0f);

            // Add MeshFilter component
            MeshFilter meshFilter = indicatorSphere.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = CreateSphereMesh();

            // Add MeshRenderer component
            MeshRenderer meshRenderer = indicatorSphere.AddComponent<MeshRenderer>();
            meshRenderer.castShadows = true;
            meshRenderer.motionVectors = true;
            meshRenderer.useLightProbes = true;
            meshRenderer.bounds = new Bounds(Vector3.zero, new Vector3(2.00f, 2.00f, 2.00f));
            meshRenderer.localBounds = new Bounds(Vector3.zero, new Vector3(1.00f, 1.00f, 1.00f));
            meshRenderer.enabled = true; // Initially disabled
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            meshRenderer.receiveShadows = true;
            meshRenderer.forceRenderingOff = false;
            meshRenderer.staticShadowCaster = false;
            meshRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.Object;
            meshRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.BlendProbes;
            meshRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.BlendProbes;
            meshRenderer.renderingLayerMask = 4294967295;
            meshRenderer.rendererPriority = 0;
            meshRenderer.rayTracingMode = RayTracingMode.DynamicTransform;
            meshRenderer.sortingLayerName = "Default";
            meshRenderer.sortingLayerID = 0;
            meshRenderer.sortingOrder = 0;
            meshRenderer.allowOcclusionWhenDynamic = true;

            // Set lightmap properties
            meshRenderer.lightmapIndex = -1;
            meshRenderer.realtimeLightmapIndex = -1;
            meshRenderer.lightmapScaleOffset = new Vector4(1.00f, 1.00f, 0.00f, 0.00f);
            meshRenderer.realtimeLightmapScaleOffset = new Vector4(1.00f, 1.00f, 0.00f, 0.00f);

            var materialInstance = matNullifierSafeWardPillarGlow.WaitForCompletion();
            meshRenderer.material = materialInstance;
            meshRenderer.sharedMaterial = materialInstance;

            // Set additional properties if needed
            meshRenderer.allowOcclusionWhenDynamic = true;

            return indicatorSphere;
        }

        private static Mesh CreateSphereMesh()
        {
            // Create a sphere mesh programmatically
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Mesh mesh = sphere.GetComponent<MeshFilter>().mesh;

            // Optionally, you can modify the mesh here if needed

            // Destroy the temporary sphere GameObject
            GameObject.Destroy(sphere);

            return mesh;
        }

        private static IZone CreateSafeZone2(Vector3 position, Transform rangeIndicator)
        {
            var gameObject = new GameObject();

            var v = gameObject.AddComponent<SphereZone>();
            v.radius = 15;
            v.rangeIndicator = rangeIndicator;
            v.indicatorSmoothTime = 0.2f;
            v.isInverted = false;
            v.rangeIndicatorScaleVelocity = 0;
            v.enabled = true;
            v.useGUILayout = true;
            v.Networkradius = 15;

            gameObject.transform.position = position;

            NetworkServer.Spawn(gameObject);

            return v;

        }

        private static IZone CreateSafeZone(Vector3 position)
        {
            var gameObject = iscInfiniteTowerSafeWard.WaitForCompletion();
            var instance = UnityEngine.Object.Instantiate(gameObject, position, Quaternion.identity);

            var holdoutZoneController = gameObject.GetComponent<HoldoutZoneController>();

            // Set the properties
            holdoutZoneController.holdoutZoneShape = HoldoutZoneController.HoldoutZoneShape.VerticalTube;
            /*holdoutZoneController.baseRadius = 15f;
            holdoutZoneController.minimumRadius = 5f;
            holdoutZoneController.chargeRadiusDelta = 0f;
            holdoutZoneController.baseChargeDuration = 0f;
            holdoutZoneController.radiusSmoothTime = 1f;
            // holdoutZoneController.healingNovaRoot = new GameObject("HealingNovaRoot").transform; // Create a new Transform for HealingNovaRoot
            //holdoutZoneController.inBoundsObjectiveToken = "OBJECTIVE_ARENA_CHARGE_CELL";
            //holdoutZoneController.outOfBoundsObjectiveToken = "OBJECTIVE_ARENA_CHARGE_CELL";
            holdoutZoneController.showObjective = false;
            holdoutZoneController.applyFocusConvergence = false;
            holdoutZoneController.applyHealingNova = false;
            holdoutZoneController.applyDevotedEvolution = false;
            holdoutZoneController.applyDelusionResetChests = false;
            holdoutZoneController.playerCountScaling = 1f;
            holdoutZoneController.dischargeRate = 0f;
            holdoutZoneController.baseIndicatorColor = new Color(0f, 0f, 0f, 0f);
            holdoutZoneController.enabled = true;
            //holdoutZoneController.chargingTeam = TeamIndex.Player;

            holdoutZoneController.useGUILayout = false;
            holdoutZoneController.currentRadius = 15f;
            holdoutZoneController.isAnyoneCharging = false;
            holdoutZoneController.charge = 0f;
            holdoutZoneController.Network_charge = 0f; */

            NetworkServer.Spawn(instance);

            return holdoutZoneController;
        }

    }
}
