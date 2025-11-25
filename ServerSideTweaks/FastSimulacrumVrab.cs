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
    public class FastSimulacrumVrab
    {

        public static void Setup()
        {
        }

        public static void Enable()
        {
            On.EntityStates.InfiniteTowerSafeWard.Travelling.OnEnter += Travelling_OnEnter;
        }


        public static void Disable()
        {
            On.EntityStates.InfiniteTowerSafeWard.Travelling.OnEnter -= Travelling_OnEnter;
        }

        private static void Travelling_OnEnter(On.EntityStates.InfiniteTowerSafeWard.Travelling.orig_OnEnter orig, EntityStates.InfiniteTowerSafeWard.Travelling self)
        {
            orig(self);
            // default:
            //   self.travelSpeed = 5f;
            //   self.travelHeight = 10f;
            //   self.radius = 15f;
            if (BepConfig.SimulacrumFastVoidCrab.Value)
            {
                if (Run.instance != null)
                {
                    if (Run.instance.GetType() == typeof(InfiniteTowerRun))
                    {
                        var run = (InfiniteTowerRun)Run.instance;
                        self.travelSpeed = 5f + run.waveIndex * 0.1f;
                    }
                }
            }
            if(NetworkServer.active) { 
                List<NodeGraph.NodeIndex> list = self.groundNodeGraph.FindNodesInRangeWithFlagConditions(self.transform.position, self.minDistanceToNewLocation, 500f, HullMask.Human, NodeFlags.TeleporterOK, NodeFlags.None, preventOverhead: false);
                Util.ShuffleList(list, self.rng);
                self.potentialEndNodes.AddRange(list);
            }
        }
    }
}
