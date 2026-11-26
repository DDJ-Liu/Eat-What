using System;
using System.Collections.Generic;
using UnityEngine;

namespace EatWhat.Cooking.ShortCycle
{
    public enum ShortCyclePhase2TransitionStatus { Idle, PendingCommit, Loading, Active, Unloading, Failed }

    /// <summary>Resolves a catalog localization key to already approved display text.</summary>
    public interface IPhase2TextResolver
    {
        bool TryResolve(string nameKey, out string displayName);
    }

    public sealed class Phase2ProductReceipt
    {
        public PrepZoneItem Product { get; }
        public Phase2ProductDraft Draft { get; }
        internal Phase2ProductReceipt(PrepZoneItem product, Phase2ProductDraft draft)
        { Product = product.Clone(); Draft = draft; }
    }

    /// <summary>P2's current single-owner prep contents; all new products enter through InventoryManager.</summary>
    public sealed class Phase2SceneEntry : MonoBehaviour
    {
        [SerializeField] private ShortCyclePresentationDirector presentationDirector = null;
        [SerializeField] private KitchenAreaController kitchenAreaController = null;
        private bool tornDown;
        private List<PrepZoneItem> contents = new List<PrepZoneItem>();
        private StepProgress stepProgress;
        private UnknownProductResolver unknownProduct;
        private IPhase2TextResolver textResolver;
        // A commit is private to one TryProduce call until its synchronous observers finish.
        private bool productionBusy;
        private PrepZoneItem pendingProduct;

        public string RecipeId { get; private set; }
        public InventoryManager KitchenInventory { get; private set; }
        public bool IsReady { get; private set; }
        public int ReceiveCount { get; private set; }
        public StepProgress CurrentStepProgress { get { return stepProgress; } }
        public IList<PrepZoneItem> PrepZoneContents { get { return CopyItems(contents).AsReadOnly(); } }
        public event Action<IList<PrepZoneItem>> PrepZoneChanged;
        public event Action<FinalDishReadyEvent> FinalDishReady;

        internal static List<PrepZoneItem> CopyItems(IList<PrepZoneItem> items)
        {
            var copy = new List<PrepZoneItem>();
            foreach (var item in items ?? new List<PrepZoneItem>()) if (item != null) copy.Add(item.Clone());
            return copy;
        }

        public bool TryConsume(ShortCycleDataHandoff handoff, InventoryManager inventory, out string error)
        {
            if (tornDown || IsReady || KitchenInventory != null || handoff == null ||
                string.IsNullOrEmpty(handoff.CurrentRecipeId) || inventory == null)
            { error = "P2 entry requires one handoff and the existing inventory."; return false; }
            var copy = CopyItems(handoff.PrepZoneContents);
            foreach (var item in copy) if (string.IsNullOrEmpty(item.item_id) || item.quantity < 1)
            { error = "P2 prep handoff contains an invalid item."; return false; }
            if (kitchenAreaController != null)
            {
                if (!ReferenceEquals(kitchenAreaController.SceneEntry, this) ||
                    !ReferenceEquals(kitchenAreaController.PresentationDirector, presentationDirector))
                { error = "P2 area controller must reference this Entry and Director."; return false; }
                if (!kitchenAreaController.TryValidateWiring(inventory.DataCatalog, out error)) return false;
            }
            if (!inventory.TryTransferToPhase2(this, out error)) return false;
            RecipeId = handoff.CurrentRecipeId; contents = copy; KitchenInventory = inventory; IsReady = true; ReceiveCount++;
            if (kitchenAreaController != null)
            {
                if (!kitchenAreaController.TryInitialize(out error))
                {
                    inventory.ReleasePhase2Owner(this);
                    RecipeId = null; contents = new List<PrepZoneItem>(); KitchenInventory = null;
                    IsReady = false; ReceiveCount--;
                    return false;
                }
            }
            else if (presentationDirector != null) presentationDirector.EnterSpace(ShortCycleSpaceId.Phase2);
            Debug.Log("[P2S-01] Handoff recipe=" + RecipeId + " prepItems=" + contents.Count, this);
            error = null; return true;
        }

        /// <summary>Bind this recipe's explicit T3 chain and configured T8 unknown once after handoff.</summary>
        public bool TryBindProduction(IPhase2TextResolver names, out string error)
        {
            if (tornDown || !IsReady || KitchenInventory == null ||
                !ReferenceEquals(KitchenInventory.Phase2Owner, this) || names == null)
            { error = "P2 production requires a live owned entry and name resolver."; return false; }
            if (stepProgress != null)
            { error = ReferenceEquals(names, textResolver) ? null : "P2 production is already bound."; return error == null; }
            var catalog = KitchenInventory.DataCatalog;
            if (catalog == null) { error = "P2 generated catalog is not connected."; return false; }
            IList<RecipeStepRuleView> chain;
            if (!catalog.TryGetStepChain(RecipeId, out chain, out error)) return false;
            UnknownProductResolver resolved;
            if (!catalog.TryGetUnknownProductResolver(out resolved, out error)) return false;
            if (resolved == null || resolved.Item == null || string.IsNullOrWhiteSpace(resolved.Item.name_key))
            { error = "Configured T8/T4 unknown product definition is incomplete."; return false; }
            var next = new StepProgress(RecipeId, chain, resolved.ItemId);
            next.OnPrepZoneChanged(ToStacks(contents));
            next.FinalDishReady += ForwardFinalDishReady;
            stepProgress = next; unknownProduct = resolved; textResolver = names;
            error = null; return true;
        }

        /// <summary>Commit once, complete C10, then publish the settled prep/step state.</summary>
        public bool TryProduce(OperationZoneSession zone, ProcessingMatcher matcher, ProcessingToolView tool,
            string areaId, IEnumerable<string> currentAreaCarriers, string instanceId, int outputQuantity,
            out Phase2ProductReceipt receipt, out string error)
        {
            receipt = null;
            if (productionBusy)
            { error = "P2 product transaction is already in progress."; return false; }
            if (tornDown || !IsReady || KitchenInventory == null ||
                !ReferenceEquals(KitchenInventory.Phase2Owner, this) ||
                stepProgress == null || unknownProduct == null || textResolver == null ||
                zone == null || matcher == null || tool == null || string.IsNullOrWhiteSpace(areaId) ||
                currentAreaCarriers == null || string.IsNullOrWhiteSpace(instanceId) || outputQuantity <= 0)
            { error = "P2 production is unbound, stale or missing an explicit request."; return false; }
            productionBusy = true;
            try
            {
                var snapshot = zone.GetSnapshot();
                if (!snapshot.HasInputs)
                { error = "The operation zone has no held input."; return false; }
                var present = new HashSet<string>(currentAreaCarriers, StringComparer.Ordinal);
                if (!string.IsNullOrEmpty(snapshot.CarrierId) && !present.Contains(snapshot.CarrierId))
                { error = "The locked carrier is not present in this area."; return false; }
                AreaConstraint area;
                if (!KitchenInventory.DataCatalog.TryGetAreaConstraint(areaId, out area, out error)) return false;
                if (!string.IsNullOrEmpty(snapshot.CarrierId) && !Contains(area.AllowedCarriers, snapshot.CarrierId))
                { error = "The locked carrier is not allowed in this area."; return false; }
                var match = matcher.Match(RecipeId, areaId, snapshot.Inputs, tool, present);
                if (!match.Matched && !match.ProducesUnknown)
                { error = "Processing rejected: " + match.RejectionCode; return false; }
                var names = new CatalogNames(KitchenInventory.DataCatalog, textResolver);
                Phase2ProductDraft draft;
                if (!Phase2ProductProduction.TryCreate(match, snapshot, tool, unknownProduct.ItemId,
                    names, out draft, out error)) return false;
                CK01ItemData definition;
                if (!KitchenInventory.DataCatalog.TryGetItem(draft.ItemId, out definition, out error) ||
                    definition == null || !string.Equals(definition.item_kind, "product", StringComparison.Ordinal) ||
                    string.IsNullOrWhiteSpace(definition.name_key))
                { error = "P2 output T4 product definition is missing: " + draft.ItemId; return false; }
                var product = new PrepZoneItem {
                    item_id = draft.ItemId, instance_id = instanceId, quantity = outputQuantity,
                    drag_order = contents.Count, state = "prepared", display_name = draft.DisplayName
                };
                pendingProduct = product;
                var committed = KitchenInventory.TryCommitPhase2Product(this, product);
                if (!committed.Succeeded) { error = committed.Error; return false; }
                // No observer runs with the original held inputs after the prep write.
                zone.Complete();
                var progress = stepProgress;
                if (!tornDown && IsReady && progress != null)
                {
                    progress.OnPrepZoneChanged(ToStacks(contents));
                    if (!tornDown && IsReady) NotifyPrepChanged();
                }
                receipt = new Phase2ProductReceipt(product, draft);
                error = null; return true;
            }
            finally
            {
                pendingProduct = null;
                productionBusy = false;
            }
        }

        internal bool TryReceiveProductFromInventory(InventoryManager inventory, PrepZoneItem product, out string error)
        {
            if (!productionBusy || !ReferenceEquals(pendingProduct, product) ||
                tornDown || !IsReady || !ReferenceEquals(KitchenInventory, inventory) ||
                !ReferenceEquals(inventory.Phase2Owner, this) || stepProgress == null)
            { error = "P2 product commit is not the active owned transaction."; return false; }
            foreach (var item in contents)
                if (item != null && string.Equals(item.instance_id, product.instance_id, StringComparison.Ordinal))
                { error = "P2 product instance was already committed: " + product.instance_id; return false; }
            var next = CopyItems(contents);
            next.Add(product.Clone());
            try { ProcessingMatcher.Counts(ToStacks(next)); }
            catch (Exception exception) when (exception is ArgumentException || exception is OverflowException)
            { error = "P2 prep contents are invalid: " + exception.Message; return false; }
            contents.Add(product.Clone());
            pendingProduct = null;
            error = null; return true;
        }
        private void ForwardFinalDishReady(FinalDishReadyEvent value)
        {
            var listeners = FinalDishReady;
            if (listeners == null) return;
            foreach (Action<FinalDishReadyEvent> listener in listeners.GetInvocationList())
                try { listener(value); } catch (Exception exception) { Debug.LogError(exception.ToString()); }
        }

        private void NotifyPrepChanged()
        {
            var listeners = PrepZoneChanged;
            if (listeners == null) return;
            var snapshot = PrepZoneContents;
            foreach (Action<IList<PrepZoneItem>> listener in listeners.GetInvocationList())
            {
                if (tornDown || !IsReady) break;
                try { listener(snapshot); } catch (Exception exception) { Debug.LogError(exception.ToString()); }
            }
        }

        private static List<ProcessingItemStack> ToStacks(IEnumerable<PrepZoneItem> items)
        {
            var result = new List<ProcessingItemStack>();
            foreach (var item in items)
                result.Add(new ProcessingItemStack(item.item_id, item.quantity));
            return result;
        }

        private static bool Contains(IEnumerable<string> values, string sought)
        {
            foreach (var value in values)
                if (string.Equals(value, sought, StringComparison.Ordinal)) return true;
            return false;
        }

        private sealed class CatalogNames : IPhase2ProductNameSource
        {
            private readonly CK01GeneratedDataCatalog catalog;
            private readonly IPhase2TextResolver text;
            internal CatalogNames(CK01GeneratedDataCatalog catalog, IPhase2TextResolver text)
            { this.catalog = catalog; this.text = text; }
            public bool TryGetItemName(string id, out string name)
            {
                CK01ItemData row; string diagnostic;
                name = null;
                return catalog.TryGetItem(id, out row, out diagnostic) && row != null &&
                    Resolve(row.name_key, out name);
            }
            public bool TryGetToolName(string id, out string name)
            {
                CK01ToolData row; string diagnostic;
                name = null;
                return catalog.TryGetTool(id, out row, out diagnostic) && row != null &&
                    Resolve(row.name_key, out name);
            }
            public bool TryGetActionName(string id, out string name)
            {
                CK01ProcessActionData row; string diagnostic;
                name = null;
                return catalog.TryGetRow("ProcessActions", id, out row, out diagnostic) && row != null &&
                    Resolve(row.name_key, out name);
            }
            private bool Resolve(string key, out string name)
            {
                name = null;
                return !string.IsNullOrWhiteSpace(key) &&
                    text.TryResolve(key, out name) && !string.IsNullOrWhiteSpace(name);
            }
        }

        public void Teardown()
        {
            tornDown = true;
            if (kitchenAreaController != null) kitchenAreaController.Teardown();
            if (presentationDirector != null) presentationDirector.ClearSessionPresentation();
            if (KitchenInventory != null) KitchenInventory.ReleasePhase2Owner(this);
            KitchenInventory = null; RecipeId = null; contents.Clear(); IsReady = false;
            if (stepProgress != null) stepProgress.FinalDishReady -= ForwardFinalDishReady;
            stepProgress = null; unknownProduct = null; textResolver = null;
        }
        private void OnDestroy() { Teardown(); }
    }
}
