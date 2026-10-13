using BepInEx;
using BepInEx.Logging;
using R2API.Utils;
using RoR2;
using RoR2.EntitlementManagement;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace ItemStringParser
{
    [BepInDependency(R2API.R2API.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [NetworkCompatibility(CompatibilityLevel.NoNeedForSync, VersionStrictness.DifferentModVersionsAreOk)]
    public class ItemStringParser : BaseUnityPlugin
    {
        public static PluginInfo PInfo { get; private set; }
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Def";
        public const string PluginName = "ItemStringParser";
        public const string PluginVersion = "2.0.0";

        private static readonly Regex tokenPattern = new Regex(@"(?:(\d+)\s*x\s*)?(\w+)", RegexOptions.IgnoreCase);

        private static ManualLogSource Log;

        private static readonly Dictionary<string, AsyncOperationHandle<PickupDropTable>> loadedDropTables = new Dictionary<string, AsyncOperationHandle<PickupDropTable>>();
        private static Dictionary<string, string> dropTables;
        // for drop tables without a weighted selection; separate from the run's seeded generators
        private static readonly Xoroshiro128Plus dropTableRng = new Xoroshiro128Plus((ulong)DateTime.UtcNow.Ticks);

        private void Awake()
        {
            Log = Logger;
        }

        // Every drop table the game's Addressables know, by file name, so new ones need no update here
        private static Dictionary<string, string> DropTables
        {
            get
            {
                if (dropTables == null)
                {
                    dropTables = FindDropTables();
                }
                return dropTables;
            }
        }

        private static Dictionary<string, string> FindDropTables()
        {
            var found = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var locator in Addressables.ResourceLocators)
            {
                foreach (var key in locator.Keys)
                {
                    if (key is not string address || !address.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (!locator.Locate(address, typeof(PickupDropTable), out var locations) || locations.Count == 0)
                        continue;

                    var name = System.IO.Path.GetFileNameWithoutExtension(address);
                    if (found.TryGetValue(name, out var existing))
                    {
                        if (existing != address)
                            Log?.LogDebug($"Drop table name {name} is used twice; {address} is ignored in favor of {existing}.");
                        continue;
                    }
                    found.Add(name, address);
                }
            }
            return found;
        }

        private static PickupDropTable GetDroptable(string droptable)
        {
            var address = DropTables[droptable];
            if (!loadedDropTables.TryGetValue(address, out var handle))
            {
                handle = Addressables.LoadAssetAsync<PickupDropTable>(address);
                loadedDropTables[address] = handle;
            }
            return handle.WaitForCompletion();
        }
        public static T GetRandom<T>(List<T> list, T defaultValue)
        {
            if (list == null || list.Count == 0)
            {
                return defaultValue;
            }
            return list[UnityEngine.Random.RandomRangeInt(0, list.Count)];
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
        private static (string replacedString, Dictionary<string, string> replacements) ReplaceOuterBraces(string input)
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

        /// <summary>
        /// This method resolves a single item key into an pickup index. It supports parsing item keys that represent item tiers, drop tables, concrete items, or concrete equipment. It also supports blacklisting certain items from selection.
        /// </summary>
        /// <param name="itemkey">A concrete item internal name or a drop table with possible not-operators or a tier with possible not-operators.</param>
        /// <param name="availableOnly">If true will prevent concrete item names from being resolved if they are disabled or not yet unlocked.</param>
        /// <returns>The resolved pickup index</returns>
        /// <exception cref="ArgumentException">Thrown when itemkey can not be resolved to an concrete item name, droptable or tier or when any of the blacklisted item names could not be resolved.<exception>
        public static PickupIndex ResolveItemKey(string itemkey, bool availableOnly = true)
        {
            var resolvedItems = new Dictionary<PickupIndex, int>();
            ResolveItemKey(itemkey, 1, resolvedItems, null, availableOnly);
            foreach (var (pickupIndex, amount) in resolvedItems)
            {
                if (amount > 0 && pickupIndex != PickupIndex.none)
                {
                    return pickupIndex;
                }
            }
            return PickupIndex.none;
        }

        private static bool ResolveItemKey(string itemkey, int repeat, Dictionary<PickupIndex, int> resolvedItems, ManualLogSource log, bool availableOnly)
        {
            List<PickupIndex> itemBlackList = [];
            var blacklistedItems = itemkey.Split("!");
            itemkey = blacklistedItems[0].Trim();
            for(var i = 1; i < blacklistedItems.Length; i++)
            {
                var blacklistedItem = blacklistedItems[i].Trim();
                var pickupIndex = PickupCatalog.FindPickupIndex(blacklistedItem);
                if (pickupIndex != PickupIndex.none)
                {
                    itemBlackList.Add(pickupIndex);
                }
                else
                {
                    HandleWarning(log, $"ResolveItemKey: Could not get pickup from blacklisted item: {blacklistedItem}");
                }
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
                case "foodtier":
                    candidates = Run.instance.availableFoodTierDropList.Except(itemBlackList).ToList();
                    break;
            }
            if (candidates != null) {
                while (repeat > 0)
                {
                    var pickupIndex = GetRandom(candidates, PickupIndex.none);
                    if (pickupIndex == PickupIndex.none)
                    {
                        HandleWarning(log, $"ResolveItemKey: Could not get pickup from item tier: {itemkey}, as all candidates are either disabled or locked behind expansion");
                        return false;
                    }
                    resolvedItems[pickupIndex] = resolvedItems.GetValueOrDefault(pickupIndex) + 1;
                    repeat--;
                }
                return true;
            }
            if (DropTables.ContainsKey(itemkey))
            {
                var dropTable = GetDroptable(itemkey);
                dropTable.EnsureInitialization();
                WeightedSelection<UniquePickup> choices = dropTable switch
                {
                    BasicPickupDropTable basicDropTable => basicDropTable.selector,
                    ExplicitPickupDropTable explicitDropTable => explicitDropTable.weightedSelection,
                    _ => null,
                };
                if (choices == null)
                {
                    // special drop tables only generate their pickups
                    if (itemBlackList.Count > 0)
                        HandleWarning(log, $"ResolveItemKey: Droptable {itemkey} does not support blacklisting items.");
                    while (repeat > 0)
                    {
                        var uniquePickup = dropTable.GeneratePickup(dropTableRng);
                        if (!uniquePickup.isValid)
                        {
                            HandleWarning(log, $"ResolveItemKey: Could not get pickup from droptable: {itemkey}");
                            return false;
                        }
                        resolvedItems[uniquePickup.pickupIndex] = resolvedItems.GetValueOrDefault(uniquePickup.pickupIndex) + 1;
                        repeat--;
                    }
                    return true;
                }
                WeightedSelection<UniquePickup> selection = new WeightedSelection<UniquePickup>();
                // the choices array has spare capacity beyond Count
                for (int i = 0; i < choices.Count; i++)
                {
                    var choice = choices.GetChoice(i);
                    var pickupIndex = choice.value.pickupIndex;
                    if (!itemBlackList.Contains(pickupIndex) && Run.instance.IsPickupAvailable(pickupIndex))
                    {
                        selection.AddChoice(choice);
                    }
                }
                if (selection.Count == 0) {
                    HandleWarning(log, $"ResolveItemKey: Could not get pickup from droptable: {itemkey}, as all candidates are either disabled or locked behind expansion");
                    return false;
                }
                while (repeat > 0)
                {
                    var uniquePickup = selection.Evaluate(UnityEngine.Random.Range(0, 1f));
                    if (uniquePickup.Equals(UniquePickup.none))
                    {
                        HandleWarning(log, $"ResolveItemKey: Could not get pickup from droptable: {itemkey}");
                        return false;
                    }
                    resolvedItems[uniquePickup.pickupIndex] = resolvedItems.GetValueOrDefault(uniquePickup.pickupIndex) + 1;
                    repeat--;
                }
                return true;
            }
            else
            {
                ItemIndex itemIndex = ItemCatalog.FindItemIndex(itemkey);
                if (itemIndex != ItemIndex.None)
                {
                    if (availableOnly && !Run.instance.IsItemAvailable(itemIndex))
                    {
                        HandleWarning(log, $"ResolveItemKey: {itemkey} cannot be resolved as it is disabled in this run.");
                        return false;
                    }
                    var pickupIndex = PickupCatalog.FindPickupIndex(itemIndex);
                    resolvedItems[pickupIndex] = resolvedItems.GetValueOrDefault(pickupIndex) + repeat;
                    return true;
                }
                else
                {
                    EquipmentIndex equipmentIndex = EquipmentCatalog.FindEquipmentIndex(itemkey);
                    if (equipmentIndex != EquipmentIndex.None)
                    {
                        if (availableOnly && !Run.instance.IsEquipmentAvailable(equipmentIndex))
                        {
                            HandleWarning(log, $"ResolveItemKey: {itemkey} cannot be resolved as it is disabled in this run.");
                            return false;
                        }
                        var pickupIndex = PickupCatalog.FindPickupIndex(equipmentIndex);
                        resolvedItems[pickupIndex] = resolvedItems.GetValueOrDefault(pickupIndex) + repeat;
                        return true;
                    }
                    else
                    {
                        HandleWarning(log, $"ResolveItemKey: Could not find item key: {itemkey}");
                        return false;
                    }
                }
            }
        }

        private class ItemStringEntry
        {
            public string itemKey;
            public int repeat = 1;
            public int multiplier = 1;
            public float weight = 1f;
        }

        public enum ItemStringFormat
        {
            DEFAULT,
            WEIGHTED_OR_ONLY
        }

        private static void HandleWarning(ManualLogSource log, string message)
        {
            if(log == null)
            {
                throw new ArgumentException(message);
            }
            else
            {
                log.LogWarning(message);
            }
        }

        private static bool ParseItemString(string itemString, Dictionary<PickupIndex, int> resolvedItems, ManualLogSource log, bool availableOnly, int index, int repeat)
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
            ItemStringFormat format = ItemStringFormat.DEFAULT;

            itemString = itemString.Trim();
            // step 1: Replace curly brackets with a random string -> will handle that later
            (itemString, var replacements) = ReplaceOuterBraces(itemString);
            // step 2: Handle the tokens
            string[] parts;
            bool and;
            if (format == ItemStringFormat.WEIGHTED_OR_ONLY)
            {
                parts = itemString.Split('|');
                and = false;
            }
            else
            {
                if (itemString.Contains("&") && itemString.Contains("|"))
                {
                    HandleWarning(log, $"ParseItemStringReward: Cannot have & and | in the same group: '{itemString}'");
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
            }

            WeightedSelection<ItemStringEntry> selection = new WeightedSelection<ItemStringEntry>();
            List<ItemStringEntry> selectionList = new List<ItemStringEntry>();

            for (var i = 0; i < parts.Length; i++)
            {
                var entry = new ItemStringEntry();

                var part = parts[i];
                var token = part.Trim();
                var tokenParts = token.Split(":", 2);
                token = tokenParts[0].Trim();

                if (tokenParts.Length > 1)
                    entry.weight = float.Parse(tokenParts[1].Trim());

                if (format == ItemStringFormat.WEIGHTED_OR_ONLY)
                {
                    entry.itemKey = token;
                }
                else
                {
                    tokenParts = token.Split("*", 2);
                    token = tokenParts[0].Trim();
                    if (tokenParts.Length > 1)
                        entry.multiplier = int.Parse(tokenParts[1].Trim());

                    var match = tokenPattern.Match(token);
                    if (!match.Success)
                    {
                        HandleWarning(log, $"Cannot parse segment '{part.Trim()}'");
                        return false;
                    }

                    if (match.Groups[1].Success)
                        entry.repeat = int.Parse(match.Groups[1].Value);

                    if (match.Groups[2].Success)
                        entry.itemKey = match.Groups[2].Value;
                    else
                    {
                        HandleWarning(log, $"Cannot parse segment '{part.Trim()}'");
                        return false;
                    }
                }

                selection.AddChoice(entry, entry.weight);
                selectionList.Add(entry);
            }
            while (repeat > 0)
            {
                if (and)
                {
                    foreach (var entry in selectionList)
                    {
                        bool success;
                        Dictionary<PickupIndex, int> subResolvedItems = new Dictionary<PickupIndex, int>();
                        if (replacements.ContainsKey(entry.itemKey))
                        {
                            success = ParseItemString(replacements[entry.itemKey], subResolvedItems, log, availableOnly, -1, entry.repeat);
                        }
                        else
                        {
                            success = ResolveItemKey(entry.itemKey, entry.repeat, subResolvedItems, log, availableOnly);
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
                            HandleWarning(log, $"Could not resolve to item rewards: {itemString}");
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
                            success = ParseItemString(replacements[entry.itemKey], subResolvedItems, log, availableOnly, -1, entry.repeat);
                        }
                        else
                        {
                            success = ResolveItemKey(entry.itemKey, entry.repeat, subResolvedItems, log, availableOnly);
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

        /// <summary>
        /// This method interprets an item string, applying repetitions and other formatting rules, to build a collection of items/equipments with their amounts. Any error in the syntax will lead to an Argum
        /// </summary>
        /// <param name="itemString">The input string containing item definitions to parse. It includes items, operators, and formatting syntax.</param>
        /// <param name="resolvedItems">A dictionary to which parsed item entries and their amounts will be added or updated.</param>
        /// <param name="availableOnly">If true will prevent concrete item names from being resolved if they are disabled or not yet unlocked.</param>
        /// <param name="index">Specifies if a certain entry of the top-level or-group shall be taken or if it should be picked at random. -1 is default and means random.</param>
        /// <exception cref="ArgumentException">Thrown when input can not be properly resolved for any reason, be it syntax errors or a concrete item name which does not exist or the user has not the required dlc, etc.<exception>
        public static void ParseItemStringStrict(string itemString, Dictionary<PickupIndex, int> resolvedItems, bool availableOnly = true, int index = -1)
        {
            ParseItemString(itemString, resolvedItems, null, availableOnly, index, 1);
        }

        /// <summary>
        /// This method interprets an item string, applying repetitions and other formatting rules, to build a collection of items/equipments with their amounts.
        /// </summary>
        /// <param name="itemString">The input string containing item definitions to parse. It includes items, operators, and formatting syntax.</param>
        /// <param name="resolvedItems">A dictionary to which parsed item entries and their amounts will be added or updated.</param>
        /// <param name="log">For logging in case the provided itemString contains syntax errors.</param>
        /// <param name="availableOnly">If true will prevent concrete item names from being resolved if they are disabled or not yet unlocked.</param>
        /// <param name="index">Specifies if a certain entry of the top-level or-group shall be taken or if it should be picked at random. -1 is default and means random.</param>
        /// <returns>Whether the item string was resolved successfully. A failure case can be when the input string had syntax error or a concrete item was selected for which the user does not have the required dlc or a droptable or tier not having enough candidates.</returns>
        public static bool ParseItemString(string itemString, Dictionary<PickupIndex, int> resolvedItems, ManualLogSource log, bool availableOnly = true, int index = -1)
        {
            return ParseItemString(itemString, resolvedItems, log, availableOnly, index, 1);
        }

        public static void WriteDropTablesMarkdownFile(string filePath)
        {
            // This will write it next to the RiskOfRain2.exe file
            using (System.IO.StreamWriter writer = new System.IO.StreamWriter(filePath))
            {
                writer.WriteLine("# Survivors");
                string[] survivorNames = SurvivorCatalog.allSurvivorDefs.Select(survivor => survivor.cachedName).ToArray();
                foreach (var entry in survivorNames)
                {
                    writer.WriteLine($"{entry}");
                }
                writer.WriteLine("# Bodies");
                string[] bodyNames = BodyCatalog.allBodyPrefabs.Select(prefab => prefab.name).ToArray();
                foreach (var entry in bodyNames)
                {
                    writer.WriteLine($"{entry}");
                }
                string dropTableNamesEBNF = "";
                writer.WriteLine("# DropTables");
                writer.WriteLine("| Drop Table Name                  | tier1 | tier2 | tier3 | boss | lunarEquipment | lunarItem | lunarCombined | equipment | voidTier1 | voidTier2 | voidTier3 | voidBoss | foodTier | powerShapes | canDropBeReplaced | requiredItemTags | bannedItemTags |");
                writer.WriteLine("|----------------------------------|-------|-------|-------|------|----------------|-----------|---------------|-----------|-----------|-----------|-----------|----------|----------|-------------|-------------------|------------------|----------------|");
                foreach (var entry in DropTables)
                {
                    var dropTableName = entry.Key;
                    if(string.IsNullOrWhiteSpace(dropTableNamesEBNF))
                    {
                        dropTableNamesEBNF = $"\"{dropTableName}\"";
                    }
                    else
                    {
                        dropTableNamesEBNF += $" | \"{dropTableName}\"";
                    }
                    if (GetDroptable(dropTableName) is not BasicPickupDropTable dropTable)
                        continue;
                    string canDropBeReplaced = dropTable.canDropBeReplaced.ToString();
                    string requiredItemTags = string.Join(", ", dropTable.requiredItemTags.Select(e => e.ToString()));
                    string bannedItemTags = string.Join(", ", dropTable.bannedItemTags.Select(e => e.ToString()));
                    string tier1Weight = dropTable.tier1Weight.ToString();
                    string tier2Weight = dropTable.tier2Weight.ToString();
                    string tier3Weight = dropTable.tier3Weight.ToString();
                    string bossWeight = dropTable.bossWeight.ToString();
                    string lunarEquipmentWeight = dropTable.lunarEquipmentWeight.ToString();
                    string lunarItemWeight = dropTable.lunarItemWeight.ToString();
                    string lunarCombinedWeight = dropTable.lunarCombinedWeight.ToString();
                    string equipmentWeight = dropTable.equipmentWeight.ToString();
                    string voidTier1Weight = dropTable.voidTier1Weight.ToString();
                    string voidTier2Weight = dropTable.voidTier2Weight.ToString();
                    string voidTier3Weight = dropTable.voidTier3Weight.ToString();
                    string voidBossWeight = dropTable.voidBossWeight.ToString();
                    string foodTierWeight = dropTable.foodTierWeight.ToString();
                    string powerShapesWeight = dropTable.powerShapesWeight.ToString();
                    writer.WriteLine($"| {dropTableName,-32} | {tier1Weight} | {tier2Weight} | {tier3Weight} | {bossWeight} | {lunarEquipmentWeight} | {lunarItemWeight} | {lunarCombinedWeight} | {equipmentWeight} | {voidTier1Weight} | {voidTier2Weight} | {voidTier3Weight} | {voidBossWeight} | {foodTierWeight} | {powerShapesWeight} | {canDropBeReplaced} | {requiredItemTags} | {bannedItemTags} |");
                }
                string itemNamesEBNF = "";
                ItemIndex itemIndex = (ItemIndex)0;
                for (ItemIndex itemCount = (ItemIndex)ItemCatalog.itemCount; itemIndex < itemCount; itemIndex++)
                {
                    ItemDef itemDef = ItemCatalog.GetItemDef(itemIndex);
                    if (!itemDef.tags.Contains(ItemTag.IgnoreForDropList))
                    {
                        if (itemDef.DoesNotContainTag(ItemTag.WorldUnique))
                        {
                            if (string.IsNullOrWhiteSpace(itemNamesEBNF))
                            {
                                itemNamesEBNF = $"\"{itemDef.name}\"";
                            }
                            else
                            {
                                itemNamesEBNF += $" | \"{itemDef.name}\"";
                            }
                        }
                    }
                }
                EquipmentIndex equipmentIndex = (EquipmentIndex)0;
                for (EquipmentIndex equipmentCount = (EquipmentIndex)EquipmentCatalog.equipmentCount; equipmentIndex < equipmentCount; equipmentIndex++)
                {
                    EquipmentDef equipmentDef = EquipmentCatalog.GetEquipmentDef(equipmentIndex);
                    if (equipmentDef.canDrop)
                    {
                        if (string.IsNullOrWhiteSpace(itemNamesEBNF))
                        {
                            itemNamesEBNF = $"\"{equipmentDef.name}\"";
                        }
                        else
                        {
                            itemNamesEBNF += $" | \"{equipmentDef.name}\"";
                        }
                    }
                }
                writer.WriteLine("# EBNF");
                writer.WriteLine("```");
                writer.WriteLine($"<itemname>  ::= {itemNamesEBNF}");
                writer.WriteLine($"<droptable> ::= {dropTableNamesEBNF}");
                writer.WriteLine("```");
            }
        }
    }
}
