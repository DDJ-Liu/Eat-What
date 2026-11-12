using System;
using System.Collections.Generic;
using EatWhat.Toolkit;
using EatWhat.Tools.Localization;
using UnityEngine;

namespace EatWhat.Cooking.ShortCycle
{
    /// <summary>Uses authored fridge shelf anchors; no layout point is generated or moved.</summary>
    [DisallowMultipleComponent]
    public sealed class ShortCycleFridgeBinder : MonoBehaviour
    {
        [SerializeField] private FridgeCatRig fridgeCatRig = null;
        [SerializeField] private FridgeSlot[] slots = new FridgeSlot[0];
        [SerializeField] private LocalizedText[] slotNameBindings = new LocalizedText[0];
        private int currentTier;
        private int boundCapacity;
        private bool isTidied;

        public int CurrentTier { get { return currentTier; } }
        public int BoundCapacity { get { return boundCapacity; } }
        public bool IsTidied { get { return isTidied; } }
        public FridgeCatRig FridgeCatRig { get { return fridgeCatRig; } }
        public void ResetOrder() { isTidied = false; currentTier = 0; }

        internal static int GetRequiredTierCount(int slotCount)
        {
            return slotCount <= 0 ? 0 :
                (slotCount + FridgeCatTier.ShelfColumnCount - 1) / FridgeCatTier.ShelfColumnCount;
        }

        internal static int GetFilterTierCount(int visibleCount)
        {
            return Math.Max(1, GetRequiredTierCount(Math.Max(0, visibleCount)));
        }

        public int ApplyFilterView(int visibleCount)
        {
            var requiredTierCount = GetFilterTierCount(visibleCount);
            if (fridgeCatRig == null) { currentTier = 0; return requiredTierCount; }
            fridgeCatRig.SetTierCount(requiredTierCount);
            if (fridgeCatRig.TierCount <= 0) { currentTier = 0; return fridgeCatRig.TierCount; }
            currentTier = Math.Max(0, Math.Min(currentTier, fridgeCatRig.TierCount - 1));
            fridgeCatRig.ScrollToTier(currentTier);
            return fridgeCatRig.TierCount;
        }

        public bool Bind(CK01GeneratedDataCatalog catalog, IShortCycleInventoryQuery inventory,
            out string diagnostic)
        {
            return BindView(catalog, inventory, null, false, null, out diagnostic);
        }

        public bool BindView(CK01GeneratedDataCatalog catalog, IShortCycleInventoryQuery inventory,
            string filterTag, bool tidy, ShortCycleContext context, out string diagnostic)
        {
            if (catalog == null || inventory == null)
            {
                diagnostic = "Generated catalog and read-only InventoryManager query are required.";
                return false;
            }
            IList<CK01InitialInventoryRecord> records;
            if (!catalog.TryGetInitialInventory(out records, out diagnostic)) return false;
            CK01FridgeCapacityLevelData capacityLevel;
            if (!catalog.TryGetDemoFridgeCapacity(out capacityLevel, out diagnostic)) return false;
            var capacity = capacityLevel.capacity;
            var targets = slots ?? new FridgeSlot[0];
            if (capacity <= 0 || targets.Length < capacity)
            {
                diagnostic = "Demo fridge capacity requires " + capacity + " connected slots.";
                return false;
            }
            for (var index = 0; index < targets.Length; index++)
                if (targets[index] == null) { diagnostic = "FridgeSlot reference " + index + " is not connected."; return false; }

            var entries = new List<FridgeEntry>();
            foreach (var record in records)
            {
                if (record == null || record.Data == null)
                { diagnostic = "InitialInventory contains an unresolved row."; return false; }
                var owned = inventory.GetQuantity(record.ItemId);
                if (owned <= 0) continue;
                CK01ItemData item;
                if (!inventory.TryGetGeneratedItem(record.ItemId, out item) || item == null)
                { diagnostic = "InitialInventory item is not available through InventoryManager."; return false; }
                StackedIconShape shape;
                if (!StackedIcon.TryParseShape(item.stack_type, out shape))
                { diagnostic = "Unsupported Items.stack_type: " + item.stack_type; return false; }
                if (shape == StackedIconShape.Stack)
                    entries.Add(new FridgeEntry(record.ItemId, item,
                        ShortCycleTrayIdentity.ForStack(record.ItemId), owned, entries.Count));
                else
                    for (var unit = 0; unit < owned; unit++)
                        entries.Add(new FridgeEntry(record.ItemId, item,
                            ShortCycleTrayIdentity.ForUnit(record.ItemId, unit), 1, entries.Count));
            }
            if (entries.Count > capacity)
            {
                diagnostic = "Positive inventory instances require " + entries.Count +
                    " slots, exceeding configured fridge capacity " + capacity + ".";
                return false;
            }
            IReadOnlyList<FridgeEntry> visible = TagFilter.Apply(entries,
                entry => entry.Item.ingredient_categories,
                string.IsNullOrEmpty(filterTag) ? null : new[] { filterTag }, TagSelectionMode.Any);
            var ordered = new List<FridgeEntry>(visible);
            if (tidy) ordered.Sort((left, right) => {
                var value = left.Item.display_order_in_fridge.CompareTo(right.Item.display_order_in_fridge);
                return value != 0 ? value : left.SourceIndex.CompareTo(right.SourceIndex);
            });
            var tierCount = string.IsNullOrEmpty(filterTag) ? GetRequiredTierCount(capacity) : GetFilterTierCount(ordered.Count);
            var anchors = new Transform[ordered.Count];
            if (fridgeCatRig != null)
            {
                fridgeCatRig.SetTierCount(tierCount);
                if (fridgeCatRig.TierCount < tierCount)
                { diagnostic = "FridgeCatRig has fewer authored tiers than required."; return false; }
                for (var index = 0; index < anchors.Length; index++)
                {
                    anchors[index] = fridgeCatRig.GetShelfAnchor(index / FridgeCatTier.ShelfColumnCount,
                        index % FridgeCatTier.ShelfColumnCount);
                    if (anchors[index] == null)
                    { diagnostic = "Fridge shelf anchor is missing for slot " + index + "."; return false; }
                }
            }
            var plan = TidyLayout.Plan<FridgeEntry, Transform>(ordered, entry => true, anchors);
            var legalTier = fridgeCatRig == null ? 0 : Math.Max(0, Math.Min(currentTier, fridgeCatRig.TierCount - 1));
            if (fridgeCatRig != null && !fridgeCatRig.ScrollToTier(legalTier))
            { diagnostic = "FridgeCatRig could not restore a legal tier."; return false; }

            for (var index = 0; index < targets.Length; index++)
            {
                var occupied = index < plan.Count;
                targets[index].gameObject.SetActive(occupied);
                if (!occupied) continue;
                var entry = plan[index].Item;
                var reserved = 0;
                if (context != null && context.placeholder_states != null)
                    foreach (var state in context.placeholder_states)
                        if (state != null && state.instance_id == entry.InstanceId)
                            reserved += state.GetCommittedQuantity();
                if (fridgeCatRig != null)
                {
                    targets[index].transform.SetParent(plan[index].Point, false);
                    targets[index].transform.localPosition = Vector3.zero;
                }
                targets[index].ConfigureItem(entry.Item, entry.Quantity,
                    reserved > 0 || entry.Item.icon_sprite == null,
                    entry.ItemId, entry.InstanceId, reserved);
                var names = slotNameBindings ?? new LocalizedText[0];
                if (index < names.Length && names[index] != null)
                    names[index].Configure(entry.Item.name_key, entry.Item.name);
            }
            currentTier = legalTier;
            boundCapacity = capacity;
            isTidied = tidy;
            diagnostic = null;
            return true;
        }

        public ShortCycleActionResult ScrollTier(int sign)
        {
            if (sign == 0) return ShortCycleActionResult.Failure("Fridge scroll direction must be -1 or 1.");
            if (fridgeCatRig == null) return ShortCycleActionResult.Failure("FridgeCatRig is not connected.");
            var targetTier = currentTier + (sign < 0 ? -1 : 1);
            if (targetTier < 0 || targetTier >= fridgeCatRig.TierCount)
                return ShortCycleActionResult.Failure("Fridge tier " + targetTier + " is outside the active range.");
            if (!fridgeCatRig.ScrollToTier(targetTier))
                return ShortCycleActionResult.Failure("FridgeCatRig rejected tier " + targetTier + ".");
            currentTier = targetTier;
            return ShortCycleActionResult.Success();
        }

        private sealed class FridgeEntry
        {
            public readonly string ItemId;
            public readonly CK01ItemData Item;
            public readonly string InstanceId;
            public readonly int Quantity;
            public readonly int SourceIndex;
            public FridgeEntry(string id, CK01ItemData item, string instance, int quantity, int sourceIndex)
            { ItemId = id; Item = item; InstanceId = instance; Quantity = quantity; SourceIndex = sourceIndex; }
        }
    }
}
