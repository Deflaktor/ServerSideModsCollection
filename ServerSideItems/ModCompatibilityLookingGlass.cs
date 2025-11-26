using BepInEx.Configuration;
using LookingGlass.ItemStatsNameSpace;
using RoR2;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace ServerSideItems
{
    public static class ModCompatibilityLookingGlass
    {
        private static bool? _enabled;

        public static bool enabled
        {
            get
            {
                if (_enabled == null)
                {
                    _enabled = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("droppod.lookingglass");
                }
                return (bool)_enabled;
            }
        }
        private static object backupItemStatsDefinition = null;

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void UpdateNewlyHatchedZoeaDescription(bool enabled)
        {
            if(enabled)
            {
                var itemStat = new ItemStatsDef();
                itemStat.descriptions.Add("Bombs: ");
                itemStat.valueTypes.Add(ItemStatsDef.ValueType.Utility);
                itemStat.measurementUnits.Add(ItemStatsDef.MeasurementUnits.Number);

                itemStat.descriptions.Add("Recharge Time: ");
                itemStat.valueTypes.Add(ItemStatsDef.ValueType.Utility);
                itemStat.measurementUnits.Add(ItemStatsDef.MeasurementUnits.Seconds);
                itemStat.calculateValuesNew = (luck, stackCount, procChance) =>
                {
                    List<float> values = new();
                    values.Add(NewlyHatchedZoea.GetBombCount(stackCount));
                    values.Add(NewlyHatchedZoea.GetRechargeDuration(stackCount));
                    return values;
                };
                backupItemStatsDefinition = LookingGlass.ItemStatsNameSpace.ItemDefinitions.allItemDefinitions[(int)DLC1Content.Items.VoidMegaCrabItem.itemIndex];
                LookingGlass.ItemStatsNameSpace.ItemDefinitions.allItemDefinitions[(int)DLC1Content.Items.VoidMegaCrabItem.itemIndex] = itemStat;
            }
            else
            {
                if (backupItemStatsDefinition != null)
                {
                    LookingGlass.ItemStatsNameSpace.ItemDefinitions.allItemDefinitions[(int)DLC1Content.Items.VoidMegaCrabItem.itemIndex] = (ItemStatsDef)backupItemStatsDefinition;
                }
            }
        }
    }
}
