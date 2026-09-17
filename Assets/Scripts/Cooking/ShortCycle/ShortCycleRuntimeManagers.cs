using System;
using System.Collections.Generic;
using UnityEngine;

namespace EatWhat.Cooking.ShortCycle
{
    /// <summary>
    /// CK01 inventory boundary. The scene references only the generated catalog;
    /// item definitions and initial quantities are resolved by stable IDs.
    /// </summary>
    public sealed class InventoryManager : MonoBehaviour, IInventoryConsumptionGateway, IShortCycleInventoryQuery
    {
        [SerializeField] private CK01GeneratedDataCatalog dataCatalog = null;
        [SerializeField] private int defaultDemoQuantity = 0;
        [SerializeField] private ShortCycleInventoryReflowAdapter tightReflowAdapter = null;

        private readonly Dictionary<string, CK01ItemData> itemDefinitions = new Dictionary<string, CK01ItemData>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> quantities = new Dictionary<string, int>(StringComparer.Ordinal);
        private IInventoryTightReflow runtimeTightReflow;

        public CK01GeneratedDataCatalog DataCatalog
        {
            get { return dataCatalog; }
        }

        public string LastCatalogDiagnostic { get; private set; }
        public int DemoFridgeCapacity { get; private set; }

        private void Awake()
        {
            runtimeTightReflow = tightReflowAdapter;
            RebuildFromGeneratedAssets();
        }

        public void SetRuntimeTightReflow(IInventoryTightReflow reflow)
        {
            runtimeTightReflow = reflow;
        }

        public void RebuildFromGeneratedAssets()
        {
            string ignored;
            TryRebuildFromDataCatalog(out ignored);
        }

        public bool TryRebuildFromDataCatalog(out string diagnostic)
        {
            itemDefinitions.Clear();
            quantities.Clear();
            DemoFridgeCapacity = 0;

            if (dataCatalog == null)
            {
                diagnostic = "CK01 generated data catalog is not connected.";
                LastCatalogDiagnostic = diagnostic;
                return false;
            }

            IList<CK01GeneratedDataRecord<CK01ItemData>> items;
            if (!dataCatalog.TryGetRecords(CK01GeneratedDataCatalog.ItemsTable, out items, out diagnostic))
            {
                LastCatalogDiagnostic = diagnostic;
                return false;
            }

            foreach (var item in items)
            {
                if (item == null || item.Asset == null || string.IsNullOrEmpty(item.StableId))
                {
                    continue;
                }

                itemDefinitions[item.StableId] = item.Asset;
                quantities[item.StableId] = Math.Max(0, defaultDemoQuantity);
            }

            IList<CK01InitialInventoryRecord> initialInventory;
            if (!dataCatalog.TryGetInitialInventory(out initialInventory, out diagnostic))
            {
                itemDefinitions.Clear();
                quantities.Clear();
                LastCatalogDiagnostic = diagnostic;
                return false;
            }

            foreach (var entry in initialInventory)
            {
                if (entry == null || entry.Data == null || !itemDefinitions.ContainsKey(entry.ItemId))
                {
                    diagnostic = "InitialInventory references missing Items ID '" +
                        (entry == null ? string.Empty : entry.ItemId) + "'.";
                    itemDefinitions.Clear();
                    quantities.Clear();
                    LastCatalogDiagnostic = diagnostic;
                    return false;
                }
                quantities[entry.ItemId] = Math.Max(0, entry.Data.quantity);
            }

            CK01FridgeCapacityLevelData capacityLevel;
            if (!dataCatalog.TryGetDemoFridgeCapacity(out capacityLevel, out diagnostic))
            {
                itemDefinitions.Clear();
                quantities.Clear();
                LastCatalogDiagnostic = diagnostic;
                return false;
            }
            DemoFridgeCapacity = capacityLevel.capacity;

            diagnostic = null;
            LastCatalogDiagnostic = null;
            return true;
        }

        public bool TryGetGeneratedItem(string itemId, out CK01ItemData item)
        {
            return itemDefinitions.TryGetValue(itemId, out item);
        }

        public int GetQuantity(string itemId)
        {
            int quantity;
            return quantities.TryGetValue(itemId, out quantity) ? quantity : 0;
        }

        public InventoryCommitResult TryCommitPlaceholderStates(IList<PlaceholderState> placeholderStates)
        {
            var requested = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var state in placeholderStates ?? new List<PlaceholderState>())
            {
                if (state == null)
                {
                    continue;
                }

                var quantity = state.GetCommittedQuantity();
                if (quantity == 0)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(state.item_id))
                {
                    return InventoryCommitResult.Failure("Placeholder '" + state.instance_id + "' has no item_id bridge for inventory deduction.");
                }

                if (!requested.ContainsKey(state.item_id))
                {
                    requested.Add(state.item_id, 0);
                }
                requested[state.item_id] += quantity;
            }

            foreach (var pair in requested)
            {
                int available;
                if (!quantities.TryGetValue(pair.Key, out available))
                {
                    return InventoryCommitResult.Failure("Generated Items data does not contain '" + pair.Key + "'.");
                }
                if (available < pair.Value)
                {
                    return InventoryCommitResult.Failure("Insufficient inventory for '" + pair.Key + "'.");
                }
            }

            var consumedQuantity = 0;
            foreach (var pair in requested)
            {
                quantities[pair.Key] -= pair.Value;
                consumedQuantity += pair.Value;
            }

            if (runtimeTightReflow != null)
            {
                runtimeTightReflow.RequestTightReflow();
            }

            return InventoryCommitResult.Success(consumedQuantity);
        }
    }

    /// <summary>
    /// Observable bridge for the P1 fridge view. C-T2 supplies the visual
    /// compact-reflow listener; the data layer owns only the post-deduction call.
    /// </summary>
    public sealed class ShortCycleInventoryReflowAdapter : MonoBehaviour, IInventoryTightReflow
    {
        public int RequestCount { get; private set; }
        public event Action Requested;

        public void RequestTightReflow()
        {
            RequestCount++;
            var callback = Requested;
            if (callback != null)
            {
                callback();
            }
        }
    }

    public interface IKitchenEquipmentAvailability
    {
        bool IsAvailable(string equipmentId, out string reason);
    }
}
