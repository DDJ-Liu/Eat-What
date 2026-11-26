using System;
using System.Collections.Generic;
using UnityEngine;

namespace EatWhat.Cooking.ShortCycle
{
    /// <summary>
    /// CK01 inventory boundary. The scene references only the generated catalog;
    /// item definitions and initial quantities are resolved by stable IDs.
    /// </summary>
    public sealed class InventoryManager : MonoBehaviour, IInventoryConsumptionGateway, IShortCycleInventoryQuery, IShortCycleInventoryChangeSource
    {
        [SerializeField] private CK01GeneratedDataCatalog dataCatalog = null;
        [SerializeField] private int defaultDemoQuantity = 0;
        [SerializeField] private ShortCycleInventoryReflowAdapter tightReflowAdapter = null;

        private readonly Dictionary<string, CK01ItemData> itemDefinitions = new Dictionary<string, CK01ItemData>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> quantities = new Dictionary<string, int>(StringComparer.Ordinal);
        private IInventoryTightReflow runtimeTightReflow;

        public event Action Changed;

        public CK01GeneratedDataCatalog DataCatalog
        {
            get { return dataCatalog; }
        }

        public object Phase2Owner { get; private set; }
        internal bool TryTransferToPhase2(object owner, out string error)
        {
            if (owner == null || Phase2Owner != null) { error = "Inventory already has an owner or the target is missing."; return false; }
            Phase2Owner = owner; error = null; return true;
        }
        internal void ReleasePhase2Owner(object owner) { if (ReferenceEquals(owner, Phase2Owner)) Phase2Owner = null; }
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
            if (Phase2Owner != null) { diagnostic = "Inventory is owned by Phase2; rebuild is refused."; return false; }
            if (dataCatalog == null)
            {
                diagnostic = "CK01 generated data catalog is not connected.";
                LastCatalogDiagnostic = diagnostic;
                return false;
            }
            IList<CK01GeneratedDataRecord<CK01ItemData>> items;
            if (!dataCatalog.TryGetRecords(CK01GeneratedDataCatalog.ItemsTable, out items, out diagnostic))
            { LastCatalogDiagnostic = diagnostic; return false; }
            var nextItems = new Dictionary<string, CK01ItemData>(StringComparer.Ordinal);
            var nextQuantities = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var item in items)
            {
                if (item == null || item.Asset == null || string.IsNullOrEmpty(item.StableId)) continue;
                nextItems[item.StableId] = item.Asset;
                nextQuantities[item.StableId] = Math.Max(0, defaultDemoQuantity);
            }
            IList<CK01InitialInventoryRecord> initialInventory;
            if (!dataCatalog.TryGetInitialInventory(out initialInventory, out diagnostic))
            { LastCatalogDiagnostic = diagnostic; return false; }
            foreach (var entry in initialInventory)
            {
                if (entry == null || entry.Data == null || !nextItems.ContainsKey(entry.ItemId))
                {
                    diagnostic = "InitialInventory references missing Items ID '" +
                        (entry == null ? string.Empty : entry.ItemId) + "'.";
                    LastCatalogDiagnostic = diagnostic;
                    return false;
                }
                nextQuantities[entry.ItemId] = Math.Max(0, entry.Data.quantity);
            }
            CK01FridgeCapacityLevelData capacityLevel;
            if (!dataCatalog.TryGetDemoFridgeCapacity(out capacityLevel, out diagnostic))
            { LastCatalogDiagnostic = diagnostic; return false; }
            var changed = quantities.Count != nextQuantities.Count;
            if (!changed) foreach (var pair in nextQuantities)
            {
                int previous;
                if (!quantities.TryGetValue(pair.Key, out previous) || previous != pair.Value)
                { changed = true; break; }
            }
            itemDefinitions.Clear();
            quantities.Clear();
            foreach (var pair in nextItems) itemDefinitions.Add(pair.Key, pair.Value);
            foreach (var pair in nextQuantities) quantities.Add(pair.Key, pair.Value);
            DemoFridgeCapacity = capacityLevel.capacity;
            diagnostic = null;
            LastCatalogDiagnostic = null;
            if (changed) NotifyChanged();
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
            if (Phase2Owner != null) return InventoryCommitResult.Failure("Inventory is owned by Phase2.");
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

            if (consumedQuantity > 0) NotifyChanged();
            return InventoryCommitResult.Success(consumedQuantity);
        }
        /// <summary>P2's sole owned prep write. P1 quantities were already committed before handoff.</summary>
        internal InventoryCommitResult TryCommitPhase2Product(Phase2SceneEntry owner, PrepZoneItem product)
        {
            if (owner == null || !ReferenceEquals(Phase2Owner, owner) ||
                !owner.IsReady || !ReferenceEquals(owner.KitchenInventory, this))
                return InventoryCommitResult.Failure("P2 inventory owner is missing or stale.");
            if (product == null || string.IsNullOrWhiteSpace(product.item_id) ||
                string.IsNullOrWhiteSpace(product.instance_id) || product.quantity <= 0)
                return InventoryCommitResult.Failure("P2 product identity and positive quantity are required.");
            CK01ItemData definition; string diagnostic;
            if (dataCatalog == null || !dataCatalog.TryGetItem(product.item_id, out definition, out diagnostic) ||
                definition == null || !string.Equals(definition.item_kind, "product", StringComparison.Ordinal))
                return InventoryCommitResult.Failure("P2 product T4 definition is missing or not a product: " + product.item_id);
            if (!owner.TryReceiveProductFromInventory(this, product, out diagnostic))
                return InventoryCommitResult.Failure(diagnostic);
            // P2 prep ownership changes; P1 fridge quantities and Changed notifications do not.
            return InventoryCommitResult.Success(0);
        }
        private void NotifyChanged()
        {
            var listeners = Changed;
            if (listeners == null) return;
            foreach (Action listener in listeners.GetInvocationList())
            {
                try { listener(); }
                catch (Exception exception) { Debug.LogError(exception.ToString()); }
            }
        }
    }

    public interface IKitchenEquipmentAvailability
    {
        bool IsAvailable(string equipmentId, out string reason);
    }
}
