using System.Collections.Generic;
using EatWhat.Tools.Localization;
using UnityEngine;

namespace EatWhat.Cooking.ShortCycle
{
    /// <summary>Data-only fridge binding. Structural spare slots are the sole Binder SetActive exception.</summary>
    [DisallowMultipleComponent]
    public sealed class ShortCycleFridgeBinder : MonoBehaviour
    {
        [SerializeField] private FridgeCatRig fridgeCatRig = null;
        [SerializeField] private FridgeSlot[] slots = new FridgeSlot[0];
        [SerializeField] private LocalizedText[] slotNameBindings = new LocalizedText[0];
        private int currentTier;
        private int boundCapacity;

        public int CurrentTier { get { return currentTier; } }
        public int BoundCapacity { get { return boundCapacity; } }
        public FridgeCatRig FridgeCatRig { get { return fridgeCatRig; } }

        internal static int GetRequiredTierCount(int slotCount)
        {
            return slotCount <= 0
                ? 0
                : (slotCount + FridgeCatTier.ShelfColumnCount - 1) / FridgeCatTier.ShelfColumnCount;
        }

        internal static int GetFilterTierCount(int visibleCount)
        {
            return System.Math.Max(1, GetRequiredTierCount(System.Math.Max(0, visibleCount)));
        }

        /// <summary>
        /// Future filter-view contract. No production caller exists in this phase;
        /// the later filter route may pass its visible result count here.
        /// </summary>
        public int ApplyFilterView(int visibleCount)
        {
            var requiredTierCount = GetFilterTierCount(visibleCount);
            if (fridgeCatRig == null)
            {
                currentTier = 0;
                return requiredTierCount;
            }

            fridgeCatRig.SetTierCount(requiredTierCount);
            if (fridgeCatRig.TierCount <= 0)
            {
                currentTier = 0;
                return fridgeCatRig.TierCount;
            }

            currentTier = System.Math.Max(0, System.Math.Min(currentTier, fridgeCatRig.TierCount - 1));
            fridgeCatRig.ScrollToTier(currentTier);
            return fridgeCatRig.TierCount;
        }

        public bool Bind(
            CK01GeneratedDataCatalog catalog,
            IShortCycleInventoryQuery inventory,
            out string diagnostic)
        {
            if (catalog == null || inventory == null)
            {
                diagnostic = "Generated data catalog and a read-only inventory query are required for fridge binding.";
                return false;
            }
            IList<CK01InitialInventoryRecord> records;
            if (!catalog.TryGetInitialInventory(out records, out diagnostic)) return false;
            CK01FridgeCapacityLevelData capacityLevel;
            if (!catalog.TryGetDemoFridgeCapacity(out capacityLevel, out diagnostic)) return false;
            var capacity = capacityLevel.capacity;
            var targets = slots ?? new FridgeSlot[0];
            if (targets.Length < capacity)
            {
                diagnostic = "Demo fridge capacity requires " + capacity + " slots, but only " + targets.Length + " are connected.";
                return false;
            }

            var visibleRecords = new List<CK01InitialInventoryRecord>();
            foreach (var record in records)
            {
                if (record != null && inventory.GetQuantity(record.ItemId) > 0) visibleRecords.Add(record);
            }
            if (visibleRecords.Count > capacity)
            {
                diagnostic = "Positive-quantity inventory requires " + visibleRecords.Count +
                    " occupied slots, exceeding configured capacity " + capacity + ".";
                return false;
            }

            var shelfAnchors = new Transform[visibleRecords.Count];
            if (fridgeCatRig != null)
            {
                var requiredTierCount = GetRequiredTierCount(capacity);
                fridgeCatRig.SetTierCount(requiredTierCount);
                if (fridgeCatRig.TierCount < requiredTierCount)
                {
                    diagnostic = "FridgeCatRig could activate only " + fridgeCatRig.TierCount +
                        " of " + requiredTierCount + " required tiers.";
                    return false;
                }

                currentTier = System.Math.Max(0, System.Math.Min(currentTier, fridgeCatRig.TierCount - 1));
                if (!fridgeCatRig.ScrollToTier(currentTier))
                {
                    diagnostic = "FridgeCatRig could not restore a legal tier position after capacity binding.";
                    return false;
                }

                for (var index = 0; index < visibleRecords.Count; index++)
                {
                    shelfAnchors[index] = fridgeCatRig.GetShelfAnchor(
                        index / FridgeCatTier.ShelfColumnCount,
                        index % FridgeCatTier.ShelfColumnCount);
                    if (shelfAnchors[index] == null)
                    {
                        diagnostic = "FridgeCatRig shelf anchor is missing for slot " + index + ".";
                        return false;
                    }
                }
            }

            for (var index = 0; index < targets.Length; index++)
            {
                if (targets[index] == null)
                {
                    diagnostic = "FridgeSlot reference " + index + " is not connected.";
                    return false;
                }
            }

            var resolvedItems = new CK01ItemData[visibleRecords.Count];
            for (var index = 0; index < visibleRecords.Count; index++)
            {
                var record = visibleRecords[index];
                CK01ItemData item;
                if (record == null || record.Data == null || !inventory.TryGetGeneratedItem(record.ItemId, out item))
                {
                    diagnostic = "InitialInventory entry " + index + " cannot be resolved through InventoryManager.";
                    return false;
                }
                resolvedItems[index] = item;
            }

            var localizedNames = slotNameBindings ?? new LocalizedText[0];
            for (var index = 0; index < targets.Length; index++)
            {
                var hasDataRow = index < visibleRecords.Count;
                targets[index].gameObject.SetActive(hasDataRow);
                if (!hasDataRow) continue;
                var record = visibleRecords[index];
                var item = resolvedItems[index];
                if (fridgeCatRig != null)
                {
                    targets[index].transform.SetParent(shelfAnchors[index], false);
                    targets[index].transform.localPosition = Vector3.zero;
                }
                targets[index].ConfigureItem(item, inventory.GetQuantity(record.ItemId), item.icon_sprite == null);
                if (index < localizedNames.Length && localizedNames[index] != null)
                    localizedNames[index].Configure(item.name_key, item.name);
            }
            diagnostic = null;
            boundCapacity = capacity;
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
    }
}
