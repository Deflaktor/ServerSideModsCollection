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
using static UnityEngine.ParticleSystem.PlaybackState;

namespace ServerSideItems
{
    public class NewlyHatchedZoea
    {
        AsyncOperationHandle<GameObject> NullifierDeathBombProjectile;
        AsyncOperationHandle<GameObject> NullifierPreBombProjectile;
        // AsyncOperationHandle<GameObject> NullifierExplosion;

        protected System.Random RNG = new System.Random();

        Dictionary<CharacterMaster, FireProjectileStruct> fireProjectileByCharacter = new Dictionary<CharacterMaster, FireProjectileStruct>();
        Dictionary<int, List<Vector2>> polarPointsCache = new Dictionary<int, List<Vector2>>();

        internal class FireProjectileStruct
        {
            public FireProjectileStruct()
            {
                BombsFired = 0;
                FireTimer = 0f;
            }

            public Vector3? Target;
            public int BombsFired { get; set; }
            public float FireTimer { get; set; }
        }

        public void Init()
        {
            NullifierDeathBombProjectile = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Nullifier/NullifierDeathBombProjectile.prefab");
            NullifierPreBombProjectile = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Nullifier/NullifierPreBombProjectile.prefab");
            // NullifierExplosion = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Nullifier/NullifierExplosion.prefab");
        }

        public void Hook()
        {
            CharacterBody.onBodyInventoryChangedGlobal += CharacterBody_onBodyInventoryChangedGlobal;
            On.RoR2.VoidMegaCrabItemBehavior.FixedUpdate += VoidMegaCrabItemBehavior_FixedUpdate;
            On.RoR2.CharacterMaster.OnBodyDeath += CharacterMaster_OnBodyDeath;
            RoR2.Stage.onServerStageBegin += Stage_onServerStageBegin;
            On.RoR2.HealthComponent.TakeDamageProcess += HealthComponent_TakeDamageProcess;
        }

        public void Unhook()
        {
            CharacterBody.onBodyInventoryChangedGlobal -= CharacterBody_onBodyInventoryChangedGlobal;
            On.RoR2.VoidMegaCrabItemBehavior.FixedUpdate -= VoidMegaCrabItemBehavior_FixedUpdate;
            On.RoR2.CharacterMaster.OnBodyDeath -= CharacterMaster_OnBodyDeath;
            RoR2.Stage.onServerStageBegin -= Stage_onServerStageBegin;
        }
        private void Stage_onServerStageBegin(Stage obj)
        {
            fireProjectileByCharacter.Clear();
        }

        private void HealthComponent_TakeDamageProcess(On.RoR2.HealthComponent.orig_TakeDamageProcess orig, HealthComponent self, DamageInfo damageInfo)
        {
            orig(self, damageInfo);
            if (self.body.inventory.GetItemCount(DLC1Content.Items.VoidMegaCrabItem) > 0)
            {
                //if (!self.alive)
                //{
                    damageInfo.damageType |= DamageType.VoidDeath;
                //}
            }
        }

        private void CharacterMaster_OnBodyDeath(On.RoR2.CharacterMaster.orig_OnBodyDeath orig, CharacterMaster self, CharacterBody body)
        {
            if (self.inventory.GetItemCount(DLC1Content.Items.VoidMegaCrabItem) > 0 && NetworkServer.active)
            {
                FireProjectileInfo fireProjectileInfo = new FireProjectileInfo
                {
                    projectilePrefab = NullifierDeathBombProjectile.WaitForCompletion(),
                    position = body.transform.position,
                    rotation = Quaternion.identity,
                    owner = body.gameObject,
                    damage = body.damage,
                    crit = body.RollCrit()
                };
                ProjectileManager.instance.FireProjectile(fireProjectileInfo);
            }
            orig(self, body);
        }

        public static int GetBombCount(int stackCount)
        {
            return 3 * stackCount;
        }

        public static float GetRechargeDuration(int stackCount)
        {
            //var reloadSpeed = 1f + (self.stack-1) * 0.15f;
            //var reloadSpeed = Mathf.Max(1f, (self.body.attackSpeed + 1f) / 2f);
            //var reloadSpeed = 0.75f + stackCount * 0.25f;
            //return 5f / reloadSpeed;
            return 5f / (0.5f + stackCount * 0.5f);
        }

        private void VoidMegaCrabItemBehavior_FixedUpdate(On.RoR2.VoidMegaCrabItemBehavior.orig_FixedUpdate orig, VoidMegaCrabItemBehavior self)
        {
            if(!BepConfig.Enabled.Value)
            {
                orig(self);
                return;
            }
            self.spawnTimer += Time.fixedDeltaTime;

            if (!fireProjectileByCharacter.ContainsKey(self.body.master))
            {
                fireProjectileByCharacter.Add(self.body.master, new FireProjectileStruct());
            }
            var selfData = fireProjectileByCharacter[self.body.master];

            float maxDistance = 30f;
            var portalBombCount = GetBombCount(self.stack);
            float bombAreaRadius = 2f + Mathf.Sqrt(4f * portalBombCount / Mathf.PI);

            var fireDuration = 0.5f;
            var fireInterval = fireDuration / (float)portalBombCount;

            var reloadDuration = GetRechargeDuration(self.stack);

            if (self.spawnTimer > reloadDuration)
            {
                selfData.FireTimer -= Time.fixedDeltaTime;
                // Done Reloading -> Fire
                while(selfData.FireTimer <= 0f) {

                    // Aim
                    if(!selfData.Target.HasValue)
                    {
                        BullseyeSearch bullseyeSearch = new BullseyeSearch();
                        bullseyeSearch.viewer = self.body;
                        bullseyeSearch.searchOrigin = self.body.transform.position;
                        bullseyeSearch.searchDirection = self.body.transform.position;
                        bullseyeSearch.maxDistanceFilter = maxDistance;
                        bullseyeSearch.teamMaskFilter = TeamMask.GetEnemyTeams(TeamComponent.GetObjectTeam(self.body.gameObject));
                        bullseyeSearch.sortMode = BullseyeSearch.SortMode.Distance;
                        bullseyeSearch.RefreshCandidates();
                        var highestHealthTargets = bullseyeSearch.GetResults().OrderByDescending(result => result.healthComponent.fullCombinedHealth).ToList();
                        foreach (var target in highestHealthTargets)
                        {
                            var floor = RaycastToFloor(target.transform.position);
                            if (floor != null)
                            {
                                selfData.Target = floor;
                                break;
                            }
                        }
                    }

                    if (!selfData.Target.HasValue)
                        break;

                    // Fire
                    if (!polarPointsCache.ContainsKey(portalBombCount))
                    {
                        // cache the polar points
                        var polarPoints = GeneratePolarPoints(portalBombCount);
                        polarPoints.Reverse();
                        polarPointsCache.Add(portalBombCount, polarPoints);
                    }
                    var polarPoint = polarPointsCache[portalBombCount][selfData.BombsFired];
                    var position = selfData.Target.Value;
                    position.x += polarPoint.x * bombAreaRadius;
                    position.z += polarPoint.y * bombAreaRadius;

                    FireProjectileInfo fireProjectileInfo = new FireProjectileInfo
                    {
                        projectilePrefab = NullifierPreBombProjectile.WaitForCompletion(),
                        position = position,
                        rotation = Quaternion.identity,
                        owner = self.body.gameObject,
                        damage = self.body.damage * 3.8f,
                        force = 0,
                        crit = self.body.RollCrit(),
                    };
                    ProjectileManager.instance.FireProjectile(fireProjectileInfo);
                    // EffectManager.SimpleMuzzleFlash(NullifierExplosion.WaitForCompletion(), self.body.gameObject, "Muzzle", transmit: true);

                    selfData.BombsFired++;
                    selfData.FireTimer = fireInterval;

                    // Done Firing -> Reload
                    if(selfData.BombsFired >= portalBombCount)
                    {
                        self.spawnTimer = 0f;
                        selfData.FireTimer = 0f;
                        selfData.BombsFired = 0;
                        selfData.Target = null;
                        break;
                    }
                }
            }
        }

        private void CharacterBody_onBodyInventoryChangedGlobal(CharacterBody obj)
        {
            if (NetworkServer.active)
            {
                var stackSize = obj.inventory.GetItemCount(DLC1Content.Items.VoidMegaCrabItem);
                if (stackSize > 0)
                {
                    obj.SetBuffCount(DLC1Content.Buffs.EliteVoid.buffIndex, 1);
                }
                else
                {
                    obj.SetBuffCount(DLC1Content.Buffs.EliteVoid.buffIndex, 0);
                }
            }
        }

        private Vector3? RaycastToFloor(Vector3 position)
        {
            if (Physics.Raycast(new Ray(position, Vector3.down), out var hitInfo, 10f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
            {
                return hitInfo.point;
            }
            return null;
        }

        private List<Vector2> GeneratePolarPoints(int n)
        {
            if (n == 0)
                return new List<Vector2>();
            if (n == 1)
                return new Vector2[] {
                    new Vector2(0, 0)
                }.ToList();
            if (n == 2)
                return new Vector2[] { 
                    new Vector2(-1, 0),
                    new Vector2(1, 0)
                }.ToList();
            if (n == 3)
                return new Vector2[] { 
                    new Vector2(1, 0),
                    new Vector2(-0.5f, Mathf.Sqrt(3)/2f),
                    new Vector2(-0.5f, -Mathf.Sqrt(3)/2f)
                }.ToList();

            int nExterior = (int)Mathf.Round(Mathf.Sqrt(n));
            int nInterior = n - nExterior;

            // Generate the angles. The factor k_theta corresponds to 2*pi/phi^2.
            float kTheta = Mathf.PI * (3 - Mathf.Sqrt(5));
            float[] angles = Enumerable.Range(0, n).Select(i => kTheta * i).ToArray();

            // Generate the radii.
            float[] rInterior = new float[nInterior];
            for (int i = 0; i < nInterior; i++)
            {
                rInterior[i] = Mathf.Sqrt((float)i / (nInterior - 1)); // Normalize to [0, 1]
            }

            float[] rExterior = Enumerable.Repeat(1.0f, nExterior).ToArray();
            float[] r = rInterior.Concat(rExterior).ToArray();

            // Prepare the result array for Cartesian coordinates
            List<Vector2> result = new List<Vector2>(n);

            // Convert polar coordinates to Cartesian coordinates
            for (int i = 0; i < n; i++)
            {
                var x = r[i] * Mathf.Cos(angles[i]);
                var y = r[i] * Mathf.Sin(angles[i]);
                result.Add(new Vector2(x, y));
            }

            return result;
        }
    }
}
