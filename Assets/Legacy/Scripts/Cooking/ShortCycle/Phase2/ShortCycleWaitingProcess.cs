using System;
using System.Collections.Generic;
using EatWhat.MiniGames;

namespace EatWhat.Cooking.ShortCycle
{
    /// <summary>Wraps the existing modal Set and layer stack without changing their scene bindings.</summary>
    public sealed class ShortCycleMiniGameModalAdapter : IMiniGameModal
    {
        private readonly ShortCycleModalSet modalSet;
        private readonly ShortCycleInteractionLayerStack layerStack;
        private bool open;

        public ShortCycleMiniGameModalAdapter(ShortCycleModalSet modalSet, ShortCycleInteractionLayerStack layerStack)
        { this.modalSet = modalSet; this.layerStack = layerStack; }

        public bool IsOpen { get { return open; } }

        public bool TryOpen(out string diagnostic)
        {
            if (open || modalSet == null || layerStack == null || modalSet.ModalLayer == null ||
                layerStack.Count == 0 || layerStack.Top == null)
            { diagnostic = "A free modal Set, its layer, and an initialized base layer are required."; return false; }
            try
            {
                modalSet.Activate();
                if (!layerStack.Push(modalSet.ModalLayer))
                { diagnostic = "The modal input layer was refused."; modalSet.Deactivate(); return false; }
            }
            catch
            {
                modalSet.Deactivate();
                throw;
            }
            open = true;
            diagnostic = null;
            return true;
        }

        public void Close()
        {
            if (!open) return;
            open = false;
            var layer = modalSet.ModalLayer;
            try
            {
                if (!layerStack.Pop(layer) && layerStack.Contains(layer) && !layerStack.Remove(layer))
                    throw new InvalidOperationException("Mini-game modal input layer could not be released.");
            }
            finally { modalSet.Deactivate(); }
        }
    }

    public sealed class ShortCycleProcessRequest
    {
        public string RequestId;
        public string ActionId;
        public string AreaId;
        public string ProductInstanceId;
        public int OutputQuantity = 1;
        public float? WaitingDurationSeconds;
        public OperationZoneSession Zone;
        public ProcessingMatcher Matcher;
        public ProcessingToolView Tool;
        public IEnumerable<string> CurrentAreaCarriers;
    }

    public sealed class ShortCycleProcessOutcome
    {
        public string RequestId { get; private set; }
        public Phase2ProductReceipt Receipt { get; private set; }
        /// <summary>Null means regular processing or no quality result.</summary>
        public float? Quality { get; private set; }
        public string Diagnostic { get; private set; }
        public bool Succeeded { get { return Receipt != null && Diagnostic == null; } }

        internal ShortCycleProcessOutcome(string requestId, Phase2ProductReceipt receipt,
            float? quality, string diagnostic)
        { RequestId = requestId; Receipt = receipt; Quality = quality; Diagnostic = diagnostic; }
    }

    /// <summary>Routes current T11 action_kind to direct C11 production or the G84 waiting host.</summary>
    public sealed class ShortCycleWaitingProcess
    {
        public const float PlaceholderDurationSeconds = 1f;
        private readonly Phase2SceneEntry entry;
        private readonly MiniGameHost host;
        private readonly CountdownStubMiniGame countdown;
        private bool waiting;
        private bool tornDown;
        private long generation;

        public ShortCycleWaitingProcess(Phase2SceneEntry entry, MiniGameHost host, CountdownStubMiniGame countdown)
        {
            this.entry = entry ?? throw new ArgumentNullException("entry");
            this.host = host ?? throw new ArgumentNullException("host");
            this.countdown = countdown ?? throw new ArgumentNullException("countdown");
        }

        public bool IsWaiting { get { return waiting; } }
        public bool IsTornDown { get { return tornDown; } }

        public bool TryStart(ShortCycleProcessRequest request, Action<ShortCycleProcessOutcome> completed,
            out string diagnostic)
        {
            if (tornDown || waiting || host.IsActive || request == null || completed == null ||
                string.IsNullOrWhiteSpace(request.RequestId) ||
                string.IsNullOrWhiteSpace(request.ActionId) ||
                string.IsNullOrWhiteSpace(request.AreaId) ||
                string.IsNullOrWhiteSpace(request.ProductInstanceId) ||
                request.OutputQuantity <= 0 || request.Zone == null || request.Matcher == null ||
                request.Tool == null || request.CurrentAreaCarriers == null)
            { diagnostic = "Waiting process requires a free host and complete current operation."; return false; }
            var inventory = entry.KitchenInventory;
            if (!entry.IsReady || inventory == null || !ReferenceEquals(inventory.Phase2Owner, entry) ||
                inventory.DataCatalog == null)
            { diagnostic = "Current P2 inventory owner is unavailable."; return false; }
            CK01ProcessActionData action;
            if (!inventory.DataCatalog.TryGetRow("ProcessActions", request.ActionId, out action, out diagnostic) ||
                action == null)
            { diagnostic = "T11 process action is missing: " + request.ActionId; return false; }
            if (action.action_kind != "regular" && action.action_kind != "waiting")
            { diagnostic = "Unknown T11 action_kind: " + action.action_kind; return false; }
            AreaConstraint area;
            if (!inventory.DataCatalog.TryGetAreaConstraint(request.AreaId, out area, out diagnostic))
                return false;
            if (!Contains(request.Tool.SupportedActions, request.ActionId) ||
                !Contains(area.AllowedActions, request.ActionId))
            { diagnostic = "Selected action is unsupported by the tool or current area."; return false; }
            var carriers = new List<string>(request.CurrentAreaCarriers);
            var held = request.Zone.GetSnapshot();
            if (!held.HasInputs)
            { diagnostic = "Current operation has no held inputs."; return false; }
            var match = request.Matcher.Match(entry.RecipeId, request.AreaId, held.Inputs,
                request.Tool, carriers);
            if (!match.Matched && !match.ProducesUnknown)
            { diagnostic = "Processing rejected: " + match.RejectionCode; return false; }
            if (match.Matched && !string.Equals(match.Record.Action, request.ActionId, StringComparison.Ordinal))
            { diagnostic = "T11 selected action differs from the matched processing record."; return false; }

            var requestId = request.RequestId;
            var areaId = request.AreaId;
            var instanceId = request.ProductInstanceId;
            var quantity = request.OutputQuantity;
            var zone = request.Zone;
            var matcher = request.Matcher;
            var tool = request.Tool;
            if (action.action_kind == "regular")
            {
                Phase2ProductReceipt receipt;
                if (!entry.TryProduce(zone, matcher, tool, areaId, carriers, instanceId,
                    quantity, out receipt, out diagnostic)) return false;
                completed(new ShortCycleProcessOutcome(requestId, receipt, null, null));
                diagnostic = null; return true;
            }

            var duration = request.WaitingDurationSeconds ?? PlaceholderDurationSeconds;
            var context = new MiniGameContext { RequestId = requestId, DurationSeconds = duration };
            try { context.Snapshot(); }
            catch (Exception exception) when (exception is ArgumentException)
            { diagnostic = exception.Message; return false; }
            var initial = held;
            var run = ++generation;
            waiting = true;
            try
            {
                host.Start(countdown, context, result =>
                {
                    if (!waiting || generation != run || tornDown) return;
                    waiting = false;
                    if (!SameOperation(initial, zone.GetSnapshot()))
                    {
                        completed(new ShortCycleProcessOutcome(requestId, null, null,
                            "Held operation changed during the waiting mini-game."));
                        return;
                    }
                    Phase2ProductReceipt receipt;
                    string commitError;
                    if (!entry.TryProduce(zone, matcher, tool, areaId, carriers, instanceId,
                        quantity, out receipt, out commitError))
                    {
                        completed(new ShortCycleProcessOutcome(requestId, null, null, commitError));
                        return;
                    }
                    receipt.Product.quality = result.Quality;
                    completed(new ShortCycleProcessOutcome(requestId, receipt, result.Quality, null));
                });
            }
            catch (Exception exception)
            {
                waiting = false;
                generation++;
                diagnostic = exception.Message;
                return false;
            }
            diagnostic = null;
            return true;
        }

        public bool Tick(float deltaSeconds)
        {
            if (tornDown) throw new InvalidOperationException("Waiting process is torn down.");
            return countdown.Tick(deltaSeconds);
        }

        public void Cancel()
        {
            if (tornDown || !waiting) return;
            waiting = false;
            generation++;
            host.Cancel();
        }

        public void Teardown()
        {
            if (tornDown) return;
            tornDown = true;
            waiting = false;
            generation++;
            try { host.Teardown(); }
            finally { countdown.Teardown(); }
        }

        private static bool Contains(IEnumerable<string> values, string sought)
        {
            foreach (var value in values)
                if (string.Equals(value, sought, StringComparison.Ordinal)) return true;
            return false;
        }

        private static bool SameOperation(OperationZoneSnapshot a, OperationZoneSnapshot b)
        {
            if (a == null || b == null || a.Inputs.Count != b.Inputs.Count ||
                !string.Equals(a.CarrierId, b.CarrierId, StringComparison.Ordinal))
                return false;
            for (var i = 0; i < a.Inputs.Count; i++)
                if (!string.Equals(a.Inputs[i].ItemId, b.Inputs[i].ItemId, StringComparison.Ordinal) ||
                    a.Inputs[i].Quantity != b.Inputs[i].Quantity) return false;
            return true;
        }
    }
}