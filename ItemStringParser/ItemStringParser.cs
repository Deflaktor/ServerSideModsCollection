using BepInEx;
using BepInEx.Logging;
using RoR2;
using RoR2.EntitlementManagement;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace ItemStringParser
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class ItemStringParser : BaseUnityPlugin
    {
        public static PluginInfo PInfo { get; private set; }
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Def";
        public const string PluginName = "ItemStringParser";
        public const string PluginVersion = "2.1.0";

        // a key with its blacklist, e.g. 3xdtChest1!Bear!Crowbar
        private static readonly Regex tokenPattern = new Regex(@"^(?:(\d+)\s*x\s*)?(\w+(?:\s*(?:!\s*#?|#)\s*\w+)*)$", RegexOptions.IgnoreCase);
        private static readonly Regex filterPattern = new Regex(@"(!\s*#|!|#)\s*(\w+)");

        private static ManualLogSource Log;

        private static readonly Dictionary<string, AsyncOperationHandle<PickupDropTable>> loadedDropTables = new Dictionary<string, AsyncOperationHandle<PickupDropTable>>();
        private static Dictionary<string, string> dropTables;
        // for weighted picks and drop tables without a weighted selection; separate from the run's seeded generators.
        // Its floats stay below 1; UnityEngine.Random can return exactly 1, which WeightedSelection can't evaluate
        private static readonly Xoroshiro128Plus rng = new Xoroshiro128Plus((ulong)DateTime.UtcNow.Ticks);

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

        // Parses input, replaces outer curly braces with placeholders, gives transformed string and dict mapping placeholders to originals; false if the braces don't match
        private static bool TryReplaceOuterBraces(string input, out string replacedString, out Dictionary<string, string> replacements)
        {
            var sb = new StringBuilder();
            int depth = 0;
            List<(int start, int end)> outerBracesPositions = new List<(int start, int end)>();
            replacedString = input;
            replacements = new Dictionary<string, string>();

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
                    if (depth == 0)
                        return false;
                    depth--;
                    if (depth == 0)
                    {
                        var last = outerBracesPositions[outerBracesPositions.Count - 1];
                        outerBracesPositions[outerBracesPositions.Count - 1] = (last.start, i);
                    }
                }
            }

            if (depth != 0)
                return false;

            int currentPos = 0;

            foreach (var br in outerBracesPositions)
            {
                sb.Append(input.Substring(currentPos, br.start - currentPos));

                string original = input.Substring(br.start, br.end - br.start + 1);
                string originalWithoutBraces = original.Substring(1, original.Length - 2); // exclude {}
                // no item or drop table name looks like this
                string placeholder = $"__group{replacements.Count}__";
                replacements[placeholder] = originalWithoutBraces;
                sb.Append(placeholder);

                currentPos = br.end + 1;
            }

            sb.Append(input.Substring(currentPos));

            replacedString = sb.ToString();
            return true;
        }

        /// <summary>
        /// This method resolves a single item key into a pickup index. It supports parsing item keys that represent item tiers, drop tables, concrete items, or concrete equipment. Tiers and drop tables can be narrowed down by blacklisting items and by item tags.
        /// </summary>
        /// <param name="itemkey">A concrete item internal name, or a tier or drop table with possible !Item, #Tag and !#Tag filters.</param>
        /// <param name="availableOnly">If true will prevent concrete item names from being resolved if they are disabled or not yet unlocked. Names from a DLC that is not enabled for the run never resolve.</param>
        /// <returns>The resolved pickup index</returns>
        /// <exception cref="ArgumentException">Thrown when itemkey can not be resolved to an concrete item name, droptable or tier or when any of the blacklisted item names or tags could not be resolved.<exception>
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

        // Tier keywords, plus the run's other drop lists and modded tiers; null if the key is none of them
        private static List<PickupIndex> GetTierDropList(string tier)
        {
            var run = Run.instance;
            switch (tier.ToLowerInvariant())
            {
                case "tier1": return run.availableTier1DropList;
                case "tier2": return run.availableTier2DropList;
                case "tier3": return run.availableTier3DropList;
                case "boss": return run.availableBossDropList;
                case "lunar": return run.availableLunarCombinedDropList;
                case "lunaritem": return run.availableLunarItemDropList;
                case "lunarequipment": return run.availableLunarEquipmentDropList;
                case "equipment": return run.availableEquipmentDropList;
                case "voidtier1": return run.availableVoidTier1DropList;
                case "voidtier2": return run.availableVoidTier2DropList;
                case "voidtier3": return run.availableVoidTier3DropList;
                case "voidboss": return run.availableVoidBossDropList;
                case "foodtier": return run.availableFoodTierDropList;
                case "cursed": return run.availableCursedTierDropList;
                case "hex": return run.availableHexTierDropList;
                case "page": return run.availablePageTierDropList;
                case "powershape": return run.availablePowerShapeItemsDropList;
                case "keyitem": return run.availableKeyItemsDropList;
                case "any": return GetAnyItems(run);
            }
            var tierDef = FindTierDef(tier);
            if (!tierDef)
            {
                return null;
            }
            if (warnedTierNames.Add(tier) && (DropTables.ContainsKey(tier) || FindPickupByName(tier) != PickupIndex.none))
            {
                Log.LogWarning($"{tier} is the item tier {tierDef.name}, so the drop table or item of the same name can't be used by it.");
            }
            // like the run's other drop lists, without the items that never drop
            return run.allAvailableTierDroplists.TryGetValue(tierDef.tier, out var dropList) ? dropList : new List<PickupIndex>();
        }

        // the items of every tier, without power shapes, like the tier keywords
        private static List<PickupIndex> GetAnyItems(Run run)
        {
            return run.allAvailableTierDroplists
                .Where(pair => pair.Key != ItemTier.NoTier)
                .SelectMany(pair => pair.Value)
                .Where(pickup => !ItemCatalog.GetItemDef(pickup.pickupDef.itemIndex).ContainsTag(ItemTag.PowerShape))
                .ToList();
        }

        private static readonly HashSet<string> warnedTierNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // An item tier of the game or a mod by the name of its ItemTierDef, e.g. Tier1Def, or by that name without its
        // trailing TierDef or Def. Case and characters other than letters, digits and _ don't count, since an item string
        // can't contain them. Null if none fits, or if the short name fits several tiers
        private static ItemTierDef FindTierDef(string name)
        {
            var key = NormalizeTierName(name);
            var matches = new List<ItemTierDef>();
            foreach (var tierDef in ItemTierCatalog.allItemTierDefs)
            {
                if (!tierDef)
                    continue;
                if (NormalizeTierName(tierDef.name) == key)
                    return tierDef;
                if (ShortTierName(tierDef.name) == key)
                    matches.Add(tierDef);
            }
            if (matches.Count > 1 && warnedTierNames.Add(name))
            {
                Log.LogWarning($"{name} could be any of the item tiers {string.Join(", ", matches.Select(tierDef => tierDef.name))}; use one of these names.");
            }
            return matches.Count == 1 ? matches[0] : null;
        }

        private static string NormalizeTierName(string name)
        {
            return new string(name.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray()).ToLowerInvariant();
        }

        // The tier's name without its trailing TierDef or Def, e.g. tier1 for Tier1Def
        private static string ShortTierName(string tierDefName)
        {
            var name = NormalizeTierName(tierDefName);
            foreach (var suffix in new[] { "tierdef", "def" })
            {
                if (name.Length > suffix.Length && name.EndsWith(suffix))
                    return name.Substring(0, name.Length - suffix.Length);
            }
            return name;
        }

        // Picks from a tier's or drop table's items without the blacklisted ones. As a drop table itself, mods that change what
        // drop tables give change its picks too; they only change the picked item, which passed the blacklist
        private class SelectionDropTable : PickupDropTable
        {
            public WeightedSelection<UniquePickup> selection;

            public override int GetPickupCount()
            {
                return selection != null ? selection.Count : 0;
            }

            public override UniquePickup GeneratePickupPreReplacement(Xoroshiro128Plus rng)
            {
                return selection != null && selection.Count > 0 ? selection.Evaluate(rng.nextNormalizedFloat) : UniquePickup.none;
            }
        }

        private static SelectionDropTable selectionDropTable;

        // none if the selection is empty
        private static UniquePickup PickThroughDropTable(WeightedSelection<UniquePickup> selection)
        {
            if (selection.Count == 0)
            {
                return UniquePickup.none;
            }
            if (!selectionDropTable)
            {
                selectionDropTable = UnityEngine.ScriptableObject.CreateInstance<SelectionDropTable>();
                // the tier's or drop table's items, not lunar ones from Eulogy Zero
                selectionDropTable.canDropBeReplaced = false;
            }
            selectionDropTable.selection = selection;
            return selectionDropTable.GeneratePickup(rng);
        }

        private static bool ResolveItemKey(string itemkey, int repeat, Dictionary<PickupIndex, int> resolvedItems, ManualLogSource log, bool availableOnly)
        {
            List<PickupIndex> itemBlackList = [];
            List<ItemTag> requiredTags = [];
            List<ItemTag> bannedTags = [];
            var filters = filterPattern.Matches(itemkey);
            if (filters.Count > 0)
                itemkey = itemkey.Substring(0, filters[0].Index);
            itemkey = itemkey.Trim();
            foreach (Match filter in filters)
            {
                var name = filter.Groups[2].Value;
                if (filter.Groups[1].Value == "!")
                {
                    var pickupIndex = FindPickupByName(name);
                    if (pickupIndex != PickupIndex.none)
                    {
                        itemBlackList.Add(pickupIndex);
                    }
                    else
                    {
                        HandleWarning(log, $"ResolveItemKey: Could not get pickup from blacklisted item: {name}");
                    }
                }
                else if (TryFindTag(name, out var tag))
                {
                    (filter.Groups[1].Value == "#" ? requiredTags : bannedTags).Add(tag);
                }
                else
                {
                    HandleWarning(log, $"ResolveItemKey: Could not find item tag: {name}");
                    return false;
                }
            }
            bool Passes(PickupIndex pickupIndex) => !itemBlackList.Contains(pickupIndex) && HasTags(pickupIndex, requiredTags, bannedTags);

            var tierDropList = GetTierDropList(itemkey);
            List<PickupIndex> candidates = tierDropList?.Where(Passes).ToList();
            if (candidates != null) {
                var tierSelection = new WeightedSelection<UniquePickup>();
                foreach (var candidate in candidates)
                {
                    tierSelection.AddChoice(new UniquePickup(candidate), 1f);
                }
                while (repeat > 0)
                {
                    var pickupIndex = PickThroughDropTable(tierSelection).pickupIndex;
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
                    if (itemBlackList.Count > 0 || requiredTags.Count > 0 || bannedTags.Count > 0)
                        HandleWarning(log, $"ResolveItemKey: Droptable {itemkey} does not support blacklisting items or tags.");
                    while (repeat > 0)
                    {
                        var uniquePickup = dropTable.GeneratePickup(rng);
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
                    if (Passes(pickupIndex) && Run.instance.IsPickupAvailable(pickupIndex))
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
                    var uniquePickup = PickThroughDropTable(selection);
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
            else
            {
                var pickupIndex = FindPickupByName(itemkey);
                if (pickupIndex == PickupIndex.none)
                {
                    HandleWarning(log, $"ResolveItemKey: Could not find item key: {itemkey}");
                    return false;
                }
                // also with availableOnly false, e.g. for elite equipment
                if (IsExpansionLocked(pickupIndex))
                {
                    HandleWarning(log, $"ResolveItemKey: {itemkey} cannot be resolved as its DLC is not enabled in this run.");
                    return false;
                }
                if (availableOnly && !Run.instance.IsPickupAvailable(pickupIndex))
                {
                    HandleWarning(log, $"ResolveItemKey: {itemkey} cannot be resolved as it is disabled in this run.");
                    return false;
                }
                resolvedItems[pickupIndex] = resolvedItems.GetValueOrDefault(pickupIndex) + repeat;
                return true;
            }
        }

        // Whether the pickup belongs to a DLC that is not enabled for the run
        private static bool IsExpansionLocked(PickupIndex pickupIndex)
        {
            var run = Run.instance;
            var pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
            if (!run || pickupDef == null)
                return false;
            if (pickupDef.itemIndex != ItemIndex.None)
                return run.IsItemExpansionLocked(pickupDef.itemIndex);
            if (pickupDef.equipmentIndex != EquipmentIndex.None)
                return run.IsEquipmentExpansionLocked(pickupDef.equipmentIndex);
            if (pickupDef.droneIndex != DroneIndex.None)
                return run.IsDroneExpansionLocked(pickupDef.droneIndex);
            if (pickupDef.keyItemIndex != KeyItemIndex.None)
                return run.IsKeyItemExpansionLocked(pickupDef.keyItemIndex);
            return false;
        }

        private static bool TryFindTag(string name, out ItemTag tag)
        {
            var tagName = Enum.GetNames(typeof(ItemTag)).FirstOrDefault(tagName => tagName != nameof(ItemTag.Any) && tagName != nameof(ItemTag.Count) && string.Equals(tagName, name, StringComparison.OrdinalIgnoreCase));
            tag = tagName != null ? (ItemTag)Enum.Parse(typeof(ItemTag), tagName) : default;
            return tagName != null;
        }

        // equipment has no tags, so it only passes without required ones
        private static bool HasTags(PickupIndex pickupIndex, List<ItemTag> requiredTags, List<ItemTag> bannedTags)
        {
            var pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
            var itemDef = pickupDef != null ? ItemCatalog.GetItemDef(pickupDef.itemIndex) : null;
            if (!itemDef)
                return requiredTags.Count == 0;
            return requiredTags.All(itemDef.ContainsTag) && !bannedTags.Any(itemDef.ContainsTag);
        }

        // Internal names. Equipment comes first: BurnNearby is also the name of an unused item.
        private static PickupIndex FindPickupByName(string name)
        {
            var equipmentIndex = EquipmentCatalog.FindEquipmentIndex(name);
            if (equipmentIndex != EquipmentIndex.None)
                return PickupCatalog.FindPickupIndex(equipmentIndex);
            var itemIndex = ItemCatalog.FindItemIndex(name);
            if (itemIndex != ItemIndex.None)
                return PickupCatalog.FindPickupIndex(itemIndex);
            var droneIndex = DroneCatalog.FindDroneIndex(name);
            if (droneIndex != DroneIndex.None)
                return PickupCatalog.FindPickupIndex(droneIndex);
            var keyItemIndex = KeyItemCatalog.FindKeyItemIndex(name);
            if (keyItemIndex != KeyItemIndex.None)
                return PickupCatalog.FindPickupIndex(keyItemIndex);
            return PickupIndex.none;
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

        // skippedEntries: set when an entry of an & group could not be resolved and was left out
        private static bool ParseItemString(string itemString, Dictionary<PickupIndex, int> resolvedItems, ManualLogSource log, bool availableOnly, int index, int repeat, ref bool skippedEntries)
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
            // step 1: Replace curly brackets with placeholders -> will handle that later
            if (!TryReplaceOuterBraces(itemString, out var replacedString, out var replacements))
            {
                HandleWarning(log, $"Unbalanced braces in '{itemString}'");
                return false;
            }
            itemString = replacedString;
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

                // the game sets the current culture to its language, which may use a decimal comma
                if (tokenParts.Length > 1 && !float.TryParse(tokenParts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out entry.weight))
                {
                    HandleWarning(log, $"Cannot parse the weight in segment '{part.Trim()}'");
                    return false;
                }
                if (entry.weight < 0f)
                {
                    HandleWarning(log, $"The weight cannot be negative in segment '{part.Trim()}'");
                    return false;
                }

                if (format == ItemStringFormat.WEIGHTED_OR_ONLY)
                {
                    entry.itemKey = token;
                }
                else
                {
                    tokenParts = token.Split("*", 2);
                    token = tokenParts[0].Trim();
                    if (tokenParts.Length > 1 && !int.TryParse(tokenParts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out entry.multiplier))
                    {
                        HandleWarning(log, $"Cannot parse the multiplier in segment '{part.Trim()}'");
                        return false;
                    }

                    var match = tokenPattern.Match(token);
                    if (!match.Success)
                    {
                        HandleWarning(log, $"Cannot parse segment '{part.Trim()}'");
                        return false;
                    }

                    if (match.Groups[1].Success && !int.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out entry.repeat))
                    {
                        HandleWarning(log, $"Cannot parse the amount in segment '{part.Trim()}'");
                        return false;
                    }

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
            // a random pick needs at least one weight above 0
            if (!and && index < 0 && selection.GetTotalWeight() <= 0f)
            {
                HandleWarning(log, $"All weights are 0 in '{itemString}'");
                return false;
            }
            while (repeat > 0)
            {
                if (and)
                {
                    foreach (var entry in selectionList)
                    {
                        bool success;
                        bool entrySkippedEntries = false;
                        Dictionary<PickupIndex, int> subResolvedItems = new Dictionary<PickupIndex, int>();
                        if (replacements.ContainsKey(entry.itemKey))
                        {
                            success = ParseItemString(replacements[entry.itemKey], subResolvedItems, log, availableOnly, -1, entry.repeat, ref entrySkippedEntries);
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
                            skippedEntries |= entrySkippedEntries;
                        }
                        else
                        {
                            // the other entries are still given
                            skippedEntries = true;
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
                            entry = selection.Evaluate(rng.nextNormalizedFloat);
                        }
                        // a failed attempt is replaced by another one, so only the chosen attempt counts
                        bool attemptSkippedEntries = false;
                        Dictionary<PickupIndex, int> subResolvedItems = new Dictionary<PickupIndex, int>();
                        if (replacements.ContainsKey(entry.itemKey))
                        {
                            success = ParseItemString(replacements[entry.itemKey], subResolvedItems, log, availableOnly, -1, entry.repeat, ref attemptSkippedEntries);
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
                            skippedEntries |= attemptSkippedEntries;
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
        /// <param name="availableOnly">If true will prevent concrete item names from being resolved if they are disabled or not yet unlocked. Names from a DLC that is not enabled for the run never resolve.</param>
        /// <param name="index">Specifies if a certain entry of the top-level or-group shall be taken or if it should be picked at random. -1 is default and means random.</param>
        /// <exception cref="ArgumentException">Thrown when input can not be properly resolved for any reason, be it syntax errors or a concrete item name which does not exist or the user has not the required dlc, etc.<exception>
        public static void ParseItemStringStrict(string itemString, Dictionary<PickupIndex, int> resolvedItems, bool availableOnly = true, int index = -1)
        {
            bool skippedEntries = false;
            ParseItemString(itemString, resolvedItems, null, availableOnly, index, 1, ref skippedEntries);
        }

        /// <summary>
        /// This method interprets an item string, applying repetitions and other formatting rules, to build a collection of items/equipments with their amounts.
        /// </summary>
        /// <param name="itemString">The input string containing item definitions to parse. It includes items, operators, and formatting syntax.</param>
        /// <param name="resolvedItems">A dictionary to which parsed item entries and their amounts will be added or updated.</param>
        /// <param name="log">For logging in case the provided itemString contains syntax errors.</param>
        /// <param name="availableOnly">If true will prevent concrete item names from being resolved if they are disabled or not yet unlocked. Names from a DLC that is not enabled for the run never resolve.</param>
        /// <param name="index">Specifies if a certain entry of the top-level or-group shall be taken or if it should be picked at random. -1 is default and means random.</param>
        /// <returns>Whether the item string was resolved completely. A failure case can be when the input string had syntax error or a concrete item was selected for which the user does not have the required dlc or a droptable or tier not having enough candidates. Entries of an &amp; group that could be resolved are still added when another one fails.</returns>
        public static bool ParseItemString(string itemString, Dictionary<PickupIndex, int> resolvedItems, ManualLogSource log, bool availableOnly = true, int index = -1)
        {
            bool skippedEntries = false;
            return ParseItemString(itemString, resolvedItems, log, availableOnly, index, 1, ref skippedEntries) && !skippedEntries;
        }

        private static readonly Inventory.EquipmentLocationQueryArgs emptyEquipmentLocation = new Inventory.EquipmentLocationQueryArgs
        {
            searchAllSlots = true,
            allowEmpty = true,
            searchPriority = Inventory.EquipmentLocationQueryArgs.SearchPriority.EmptyFirst
        };

        /// <summary>
        /// Gives pickups to a character, like picking them up. Server only.
        /// Items come first, so a Functional Coupler among them adds its equipment sets before equipment is placed.
        /// Equipment fills the empty slots of all equipment slots and sets. Without one, the first equipment of this call replaces the active one, and further equipment is not given.
        /// Key items replace the current one. Drones and other pickups are granted the way the game grants them.
        /// </summary>
        /// <param name="body">The character receiving the pickups.</param>
        /// <param name="pickups">The pickups and their amounts, e.g. from <see cref="ParseItemString(string, Dictionary{PickupIndex, int}, ManualLogSource, bool, int)"/>.</param>
        /// <returns>The pickups that were replaced or not given, so the caller can drop them or hand them back.</returns>
        public static List<UniquePickup> GivePickups(CharacterBody body, Dictionary<PickupIndex, int> pickups)
        {
            var leftOver = new List<UniquePickup>();
            var inventory = body ? body.inventory : null;
            if (!NetworkServer.active || !inventory || pickups == null)
                return leftOver;

            bool gaveItems = false;
            foreach (var (pickupIndex, count) in pickups)
            {
                var pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
                if (count > 0 && pickupDef != null && pickupDef.itemIndex != ItemIndex.None)
                {
                    inventory.GiveItemPermanent(pickupDef.itemIndex, count);
                    gaveItems = true;
                }
            }
            // the equipment sets would only follow the items in the next fixed update
            if (gaveItems)
                inventory.RefreshInventoryStateServer();

            bool gaveEquipment = false;
            foreach (var (pickupIndex, count) in pickups)
            {
                var pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
                if (count <= 0 || pickupDef == null || pickupDef.itemIndex != ItemIndex.None)
                    continue;
                for (int i = 0; i < count; i++)
                {
                    if (pickupDef.equipmentIndex != EquipmentIndex.None)
                    {
                        GiveEquipment(inventory, pickupDef.equipmentIndex, ref gaveEquipment, leftOver);
                    }
                    else if (pickupDef.keyItemIndex != KeyItemIndex.None)
                    {
                        var previousKeyItem = inventory.GetKeyItemIndex();
                        inventory.SetKeyItem(pickupDef.keyItemIndex);
                        if (previousKeyItem != KeyItemIndex.None && previousKeyItem != pickupDef.keyItemIndex)
                            leftOver.Add(new UniquePickup(PickupCatalog.FindPickupIndex(previousKeyItem)));
                    }
                    else
                    {
                        // drones, coins and the like
                        var context = new PickupDef.GrantContext
                        {
                            body = body,
                            uniquePickup = new UniquePickup(pickupIndex),
                            position = body.corePosition,
                            rotation = body.transform.rotation
                        };
                        pickupDef.attemptGrant?.Invoke(ref context);
                    }
                }
            }
            return leftOver;
        }

        private static void GiveEquipment(Inventory inventory, EquipmentIndex equipmentIndex, ref bool gaveEquipment, List<UniquePickup> leftOver)
        {
            if (inventory.TryFindBestEquipmentLocation(in emptyEquipmentLocation, out var location))
            {
                inventory.SetEquipmentIndex(equipmentIndex, in location);
                gaveEquipment = true;
                return;
            }
            // replacing now could swap out equipment given just before
            if (gaveEquipment)
            {
                leftOver.Add(new UniquePickup(PickupCatalog.FindPickupIndex(equipmentIndex)));
                return;
            }
            // like picking it up: swap the active equipment
            location = inventory.FindBestEquipmentLocationOrCurrent(in Inventory.EquipmentLocationQueryArgs.defaultPickup);
            var previousEquipment = inventory.GetEquipmentIndex(in location);
            inventory.SetEquipmentIndex(equipmentIndex, in location);
            gaveEquipment = true;
            if (previousEquipment != EquipmentIndex.None)
                leftOver.Add(new UniquePickup(PickupCatalog.FindPickupIndex(previousEquipment)));
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
                writer.WriteLine("# Item Tiers");
                writer.WriteLine("Besides the tier keywords, item strings can use these names or the names without TierDef or Def at the end.");
                foreach (var tierDef in ItemTierCatalog.allItemTierDefs)
                {
                    if (tierDef)
                        writer.WriteLine($"{tierDef.name}");
                }
                writer.WriteLine("# DropTables");
                writer.WriteLine("| Drop Table Name                  | tier1 | tier2 | tier3 | boss | lunarEquipment | lunarItem | lunarCombined | equipment | voidTier1 | voidTier2 | voidTier3 | voidBoss | foodTier | powerShapes | cursed | hex | page | canDropBeReplaced | requiredItemTags | bannedItemTags |");
                writer.WriteLine("|----------------------------------|-------|-------|-------|------|----------------|-----------|---------------|-----------|-----------|-----------|-----------|----------|----------|-------------|--------|-----|------|-------------------|------------------|----------------|");
                var otherDropTables = new List<string>();
                var dropTableNames = DropTables.Keys.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToList();
                foreach (var dropTableName in dropTableNames)
                {
                    var loadedDropTable = GetDroptable(dropTableName);
                    if (loadedDropTable is not BasicPickupDropTable dropTable)
                    {
                        otherDropTables.Add($"| {dropTableName,-32} | {loadedDropTable.GetType().Name} |");
                        continue;
                    }
                    string requiredItemTags = string.Join(", ", dropTable.requiredItemTags.Select(e => e.ToString()));
                    string bannedItemTags = string.Join(", ", dropTable.bannedItemTags.Select(e => e.ToString()));
                    writer.WriteLine($"| {dropTableName,-32} | {dropTable.tier1Weight} | {dropTable.tier2Weight} | {dropTable.tier3Weight} | {dropTable.bossWeight} | {dropTable.lunarEquipmentWeight} | {dropTable.lunarItemWeight} | {dropTable.lunarCombinedWeight} | {dropTable.equipmentWeight} | {dropTable.voidTier1Weight} | {dropTable.voidTier2Weight} | {dropTable.voidTier3Weight} | {dropTable.voidBossWeight} | {dropTable.foodTierWeight} | {dropTable.powerShapesWeight} | {dropTable.cursedWeight} | {dropTable.hexWeight} | {dropTable.pageWeight} | {dropTable.canDropBeReplaced} | {requiredItemTags} | {bannedItemTags} |");
                }
                writer.WriteLine("# Other DropTables");
                writer.WriteLine("| Drop Table Name                  | Type |");
                writer.WriteLine("|----------------------------------|------|");
                foreach (var line in otherDropTables)
                {
                    writer.WriteLine(line);
                }
                string dropTableNamesEBNF = string.Join(" | ", dropTableNames.Select(name => $"\"{name}\""));
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
                foreach (var droneDef in DroneCatalog.allDroneDefs)
                {
                    itemNamesEBNF += string.IsNullOrWhiteSpace(itemNamesEBNF) ? $"\"{droneDef.name}\"" : $" | \"{droneDef.name}\"";
                }
                foreach (var keyItemDef in KeyItemCatalog.allKeyItemDefs)
                {
                    itemNamesEBNF += string.IsNullOrWhiteSpace(itemNamesEBNF) ? $"\"{keyItemDef.name}\"" : $" | \"{keyItemDef.name}\"";
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
