using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace StartBonusMod
{
    public class Helper
    {
        public static T GetRandom<T>(List<T> list, T defaultValue)
        {
            if (list == null || list.Count == 0)
            {
                return defaultValue;
            }
            return list[UnityEngine.Random.RandomRangeInt(0, list.Count)];
        }
        public static Dictionary<string, AsyncOperationHandle<BasicPickupDropTable>> loadedDropTables = new Dictionary<string, AsyncOperationHandle<BasicPickupDropTable>>();
        public static readonly Dictionary<string, string> dropTables = InitDropTables();
        public static Dictionary<string, string> InitDropTables()
        {
            var dropTables = new Dictionary<string, string>();
            dropTables.Add("dtMonsterTeamTier1Item", "RoR2/Base/MonsterTeamGainsItems/dtMonsterTeamTier1Item.asset");
            dropTables.Add("dtMonsterTeamTier2Item", "RoR2/Base/MonsterTeamGainsItems/dtMonsterTeamTier2Item.asset");
            dropTables.Add("dtMonsterTeamTier3Item", "RoR2/Base/MonsterTeamGainsItems/dtMonsterTeamTier3Item.asset");
            dropTables.Add("dtSacrificeArtifact", "RoR2/Base/Sacrifice/dtSacrificeArtifact.asset");
            dropTables.Add("dtAISafeTier1Item", "RoR2/Base/Common/dtAISafeTier1Item.asset");
            dropTables.Add("dtAISafeTier2Item", "RoR2/Base/Common/dtAISafeTier2Item.asset");
            dropTables.Add("dtAISafeTier3Item", "RoR2/Base/Common/dtAISafeTier3Item.asset");
            dropTables.Add("dtEquipment", "RoR2/Base/Common/dtEquipment.asset");
            dropTables.Add("dtTier1Item", "RoR2/Base/Common/dtTier1Item.asset");
            dropTables.Add("dtTier2Item", "RoR2/Base/Common/dtTier2Item.asset");
            dropTables.Add("dtTier3Item", "RoR2/Base/Common/dtTier3Item.asset");
            dropTables.Add("dtVoidChest", "RoR2/Base/Common/dtVoidChest.asset");
            dropTables.Add("dtCasinoChest", "RoR2/Base/CasinoChest/dtCasinoChest.asset");
            dropTables.Add("dtSmallChestDamage", "RoR2/Base/CategoryChest/dtSmallChestDamage.asset");
            dropTables.Add("dtSmallChestHealing", "RoR2/Base/CategoryChest/dtSmallChestHealing.asset");
            dropTables.Add("dtSmallChestUtility", "RoR2/Base/CategoryChest/dtSmallChestUtility.asset");
            dropTables.Add("dtChest1", "RoR2/Base/Chest1/dtChest1.asset");
            dropTables.Add("dtChest2", "RoR2/Base/Chest2/dtChest2.asset");
            dropTables.Add("dtDuplicatorTier1", "RoR2/Base/Duplicator/dtDuplicatorTier1.asset");
            dropTables.Add("dtDuplicatorTier2", "RoR2/Base/DuplicatorLarge/dtDuplicatorTier2.asset");
            dropTables.Add("dtDuplicatorTier3", "RoR2/Base/DuplicatorMilitary/dtDuplicatorTier3.asset");
            dropTables.Add("dtDuplicatorWild", "RoR2/Base/DuplicatorWild/dtDuplicatorWild.asset");
            dropTables.Add("dtGoldChest", "RoR2/Base/GoldChest/dtGoldChest.asset");
            dropTables.Add("dtLunarChest", "RoR2/Base/LunarChest/dtLunarChest.asset");
            dropTables.Add("dtShrineChance", "RoR2/Base/ShrineChance/dtShrineChance.asset");
            dropTables.Add("dtLockbox", "RoR2/Base/TreasureCache/dtLockbox.asset");
            dropTables.Add("dtITBossWave", "RoR2/DLC1/GameModes/InfiniteTowerRun/InfiniteTowerAssets/dtITBossWave.asset");
            dropTables.Add("dtITDefaultWave", "RoR2/DLC1/GameModes/InfiniteTowerRun/InfiniteTowerAssets/dtITDefaultWave.asset");
            dropTables.Add("dtITLunar", "RoR2/DLC1/GameModes/InfiniteTowerRun/InfiniteTowerAssets/dtITLunar.asset");
            dropTables.Add("dtITSpecialBossWave", "RoR2/DLC1/GameModes/InfiniteTowerRun/InfiniteTowerAssets/dtITSpecialBossWave.asset");
            dropTables.Add("dtITVoid", "RoR2/DLC1/GameModes/InfiniteTowerRun/InfiniteTowerAssets/dtITVoid.asset");
            dropTables.Add("dtCategoryChest2Damage", "RoR2/DLC1/CategoryChest2/dtCategoryChest2Damage.asset");
            dropTables.Add("dtCategoryChest2Healing", "RoR2/DLC1/CategoryChest2/dtCategoryChest2Healing.asset");
            dropTables.Add("dtCategoryChest2Utility", "RoR2/DLC1/CategoryChest2/dtCategoryChest2Utility.asset");
            dropTables.Add("dtVoidCamp", "RoR2/DLC1/VoidCamp/dtVoidCamp.asset");
            dropTables.Add("dtVoidTriple", "RoR2/DLC1/VoidTriple/dtVoidTriple.asset");
            dropTables.Add("dtVoidLockbox", "RoR2/DLC1/TreasureCacheVoid/dtVoidLockbox.asset");
            dropTables.Add("AurelioniteHeartPickupDropTable", "RoR2/DLC2/AurelioniteHeartPickupDropTable.asset");
            dropTables.Add("GeodeRewardDropTable", "RoR2/DLC2/GeodeRewardDropTable.asset");
            dropTables.Add("dtShrineHalcyoniteTier1", "RoR2/DLC2/dtShrineHalcyoniteTier1.asset");
            dropTables.Add("dtShrineHalcyoniteTier2", "RoR2/DLC2/dtShrineHalcyoniteTier2.asset");
            dropTables.Add("dtShrineHalcyoniteTier3", "RoR2/DLC2/dtShrineHalcyoniteTier3.asset");
            dropTables.Add("dtChanceDoll", "RoR2/DLC2/Items/ExtraShrineItem/dtChanceDoll.asset");
            dropTables.Add("dtSonorousEcho", "RoR2/DLC2/Items/ItemDropChanceOnKill/dtSonorousEcho.asset");
            dropTables.Add("dtCommandChest", "RoR2/CommandChest/dtCommandChest.asset");
            return dropTables;
        }

        // Generates a random string of given length (letters + digits)
        private static string GenerateRandomString(int length = 12)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new System.Random();
            var sb = new StringBuilder();
            sb.Append("Set_");
            for (int i = 0; i < length; i++)
                sb.Append(chars[random.Next(chars.Length)]);
            return sb.ToString();
        }

        // Parses input, replaces outer curly braces with random strings, returns transformed string and dict mapping random strings to originals
        public static (string replacedString, Dictionary<string, string> replacements) ReplaceOuterBraces(string input)
        {
            var sb = new StringBuilder();
            int depth = 0;
            List<(int start, int end)> outerBracesPositions = new List<(int start, int end)>();

            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == '{')
                {
                    if (depth == 0)
                        outerBracesPositions.Add((i, -1));
                    depth++;
                }
                else if (input[i] == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        var last = outerBracesPositions[outerBracesPositions.Count - 1];
                        outerBracesPositions[outerBracesPositions.Count - 1] = (last.start, i);
                    }
                }
            }

            var replacements = new Dictionary<string, string>();
            int currentPos = 0;

            foreach (var br in outerBracesPositions)
            {
                sb.Append(input.Substring(currentPos, br.start - currentPos));

                string original = input.Substring(br.start, br.end - br.start + 1);
                string originalWithoutBraces = original.Substring(1, original.Length - 2); // exclude {}
                string randomKey;
                do
                {
                    randomKey = GenerateRandomString();
                } while (replacements.ContainsKey(randomKey));

                replacements[randomKey] = originalWithoutBraces;
                sb.Append(randomKey);

                currentPos = br.end + 1;
            }

            sb.Append(input.Substring(currentPos));

            return (sb.ToString(), replacements);
        }

        public static bool ResolveItemKey(string itemkey, int repeat, Dictionary<PickupIndex, int> resolvedItems, List<PickupIndex> itemBlackList = null)
        {
            if (itemBlackList == null)
            {
                itemBlackList = [];
            }
            List<PickupIndex> candidates = null;
            switch (itemkey.ToLower())
            {
                case "tier1":
                    candidates = Run.instance.availableTier1DropList.Except(itemBlackList).ToList();
                    break;
                case "tier2":
                    candidates = Run.instance.availableTier2DropList.Except(itemBlackList).ToList();
                    break;
                case "tier3":
                    candidates = Run.instance.availableTier3DropList.Except(itemBlackList).ToList();
                    break;
                case "boss":
                    candidates = Run.instance.availableBossDropList.Except(itemBlackList).ToList();
                    break;
                case "lunar":
                    candidates = Run.instance.availableLunarCombinedDropList.Except(itemBlackList).ToList();
                    break;
                case "voidtier1":
                    candidates = Run.instance.availableVoidTier1DropList.Except(itemBlackList).ToList();
                    break;
                case "voidtier2":
                    candidates = Run.instance.availableVoidTier2DropList.Except(itemBlackList).ToList();
                    break;
                case "voidtier3":
                    candidates = Run.instance.availableVoidTier3DropList.Except(itemBlackList).ToList();
                    break;
                case "voidboss":
                    candidates = Run.instance.availableVoidBossDropList.Except(itemBlackList).ToList();
                    break;
            }
            if (candidates != null) {
                while (repeat > 0)
                {
                    var pickupIndex = GetRandom(candidates, PickupIndex.none);
                    if (pickupIndex == PickupIndex.none)
                    {
                        Log.LogWarning($"ResolveItemKey: Could not get pickup from item tier: {itemkey}, skipping it.");
                        return false;
                    }
                    resolvedItems[pickupIndex] = resolvedItems.GetValueOrDefault(pickupIndex) + 1;
                    repeat--;
                }
                return true;
            }
            if (dropTables.ContainsKey(itemkey))
            {
                loadedDropTables[itemkey] = loadedDropTables.GetValueOrDefault(itemkey, Addressables.LoadAssetAsync<BasicPickupDropTable>(dropTables[itemkey]));
                var dropTable = loadedDropTables[itemkey].WaitForCompletion();
                WeightedSelection<PickupIndex> selection = new WeightedSelection<PickupIndex>();
                foreach (var choice in dropTable.selector.choices)
                {
                    if (!itemBlackList.Contains(choice.value))
                    {
                        selection.AddChoice(choice);
                    }
                }
                while (repeat > 0)
                {
                    var pickupIndex = selection.Evaluate(UnityEngine.Random.Range(0, 1f));
                    if (pickupIndex == PickupIndex.none)
                    {
                        Log.LogWarning($"ResolveItemKey: Could not get pickup from droptable: {itemkey}, skipping it.");
                        return false;
                    }
                    resolvedItems[pickupIndex] = resolvedItems.GetValueOrDefault(pickupIndex) + 1;
                    repeat--;
                }
                return true;
            }
            else
            {
                ItemIndex itemIndex = ItemCatalog.FindItemIndex(itemkey);
                if (itemIndex != ItemIndex.None)
                {
                    var pickupIndex = PickupCatalog.FindPickupIndex(itemIndex);
                    while (repeat > 0)
                    {
                        resolvedItems[pickupIndex] = resolvedItems.GetValueOrDefault(pickupIndex) + 1;
                        repeat--;
                    }
                    return true;
                }
                else
                {
                    EquipmentIndex equipmentIndex = EquipmentCatalog.FindEquipmentIndex(itemkey);
                    if (equipmentIndex != EquipmentIndex.None)
                    {
                        var pickupIndex = PickupCatalog.FindPickupIndex(equipmentIndex);
                        while (repeat > 0)
                        {
                            resolvedItems[pickupIndex] = resolvedItems.GetValueOrDefault(pickupIndex) + 1;
                            repeat--;
                        }
                        return true;
                    }
                    else
                    {
                        Log.LogError($"ResolveItemKey: Could not find item key: {itemkey}");
                        return false;
                    }
                }
            }
        }

        private static readonly Regex tokenPattern = new Regex(@"(?:(\d+)\s*x\s*)?(\w+)", RegexOptions.IgnoreCase);

        public class ItemStringEntry
        {
            public string itemKey;
            public int repeat = 1;
            public int multiplier = 1;
            public float weight = 1f;
        }

        public static bool ParseItemStringReward(string itemString, Dictionary<PickupIndex, int> resolvedItems, List<PickupIndex> itemBlackList, int index = -1, int repeat = 1)
        {
            // "5 random whites or 3 large chest":
            // 5xRandom: 0.5, 3xdtChest2: 0.5

            // "1 random white item 5 times or 1 large chest item 3 times":
            // 5x{ Random }, 3x{ dtChest2 }

            // "AlienHead or ArmorPiercing 2 times"
            // 2x{ AlienHead | ArmorPiercing }

            // "AlienHead and ArmorPiercing 2 times"
            // 2x{ AlienHead & ArmorPiercing }

            // "AlienHead or ArmorPiercing 2 times, but AlienHead is two times more likely"
            // 2x{ AlienHead: 2 | ArmorPiercing: 1 }

            // "Either 2 AlienHead or 2 ArmorPiercing or 2 of a random"
            // 2xAlienHead, 2xArmorPiercing, 2x{ Random }

            // "Either 5 random white items or 5 items of random type"
            // 5xRandom, 5x{ Random }

            // { { 50xTier3: 0.5, 50xTier2: 0.5 }, { 30xdtChest2 } }
            // 100x{ { {Tier2 & Tier3}:0.5 | Tier1*10:0.5 }:0.75 | 2x{ 2xdtChest2 }*5:0.25 }

            // 5xTier2&Tier3&AlienHead*5:0.5
            // Tier2 + Tier3

            if (string.IsNullOrEmpty(itemString))
                return true;
            itemString = itemString.Trim();
            // step 1: Replace curly brackets with a random string -> will handle that later
            (itemString, var replacements) = ReplaceOuterBraces(itemString);
            // step 2: Handle the tokens
            string[] parts;
            bool and;
            if (itemString.Contains("&") && itemString.Contains("|"))
            {
                Log.LogError($"Cannot have & and | in the same group: '{itemString}'");
                return false;
            }

            if (itemString.Contains("|"))
            {
                parts = itemString.Split('|');
                and = false;
            }
            else
            {
                parts = itemString.Split('&');
                and = true;
            } 

            WeightedSelection<ItemStringEntry> selection = new WeightedSelection<ItemStringEntry>();
            List<ItemStringEntry> selectionList = new List<ItemStringEntry>();

            for(var i=0; i<parts.Length; i++)
            {
                var entry = new ItemStringEntry();

                var part = parts[i];
                var token = part.Trim();
                var tokenParts = token.Split(":", 2);
                token = tokenParts[0].Trim();

                if (tokenParts.Length > 1)
                    entry.weight = float.Parse(tokenParts[1].Trim());

                tokenParts = token.Split("*", 2);
                token = tokenParts[0].Trim();
                if (tokenParts.Length > 1)
                    entry.multiplier = int.Parse(tokenParts[1].Trim());

                var match = tokenPattern.Match(token);
                if (!match.Success)
                {
                    Log.LogError($"Cannot parse segment '{part.Trim()}'");
                    return false;
                }

                if (match.Groups[1].Success)
                    entry.repeat = int.Parse(match.Groups[1].Value);

                if (match.Groups[2].Success)
                    entry.itemKey = match.Groups[2].Value;
                else
                {
                    Log.LogError($"Cannot parse segment '{part.Trim()}'");
                    return false;
                }
                
                selection.AddChoice(entry, entry.weight);
                selectionList.Add(entry);
            }
            while (repeat > 0)
            {
                if(and)
                {
                    foreach(var entry in selectionList)
                    {
                        bool success;
                        Dictionary<PickupIndex, int> subResolvedItems = new Dictionary<PickupIndex, int>();
                        if (replacements.ContainsKey(entry.itemKey))
                        {
                            success = ParseItemStringReward(replacements[entry.itemKey], subResolvedItems, itemBlackList, -1, entry.repeat);
                        }
                        else
                        {
                            success = ResolveItemKey(entry.itemKey, entry.repeat, subResolvedItems, itemBlackList);
                        }
                        if (success)
                        {
                            foreach (var (pickupIndex, giveAmount) in subResolvedItems)
                            {
                                resolvedItems[pickupIndex] = resolvedItems.GetValueOrDefault(pickupIndex) + giveAmount * entry.multiplier;
                            }
                        }
                    }
                }
                else
                {
                    var success = false;
                    var attempts = 10;
                    var indexShift = 0;
                    while (!success)
                    {
                        attempts--;
                        if (attempts == 0)
                        {
                            Log.LogError($"ParseItemStringReward: Could not resolve to item rewards: {itemString}");
                            return false;
                        }
                        ItemStringEntry entry;
                        if (index >= 0)
                        {
                            // select the one at the index
                            entry = selectionList[(index + indexShift) % selectionList.Count];
                        }
                        else
                        {
                            // select at random
                            entry = selection.Evaluate(UnityEngine.Random.Range(0, 1f));
                        }
                        Dictionary<PickupIndex, int> subResolvedItems = new Dictionary<PickupIndex, int>();
                        if (replacements.ContainsKey(entry.itemKey))
                        {
                            success = ParseItemStringReward(replacements[entry.itemKey], subResolvedItems, itemBlackList, -1, entry.repeat);
                        }
                        else
                        {
                            success = ResolveItemKey(entry.itemKey, entry.repeat, subResolvedItems, itemBlackList);
                        }
                        if (success)
                        {
                            foreach (var (pickupIndex, giveAmount) in subResolvedItems)
                            {
                                resolvedItems[pickupIndex] = resolvedItems.GetValueOrDefault(pickupIndex) + giveAmount * entry.multiplier;
                            }
                        }
                        else
                        {
                            indexShift++;
                        }
                    }
                }
                repeat--;
            }
            return true;
        }
    }
}
