using System;
using System.Collections.Generic;

namespace EatWhat.Cooking.ShortCycle
{
    public enum OperationZoneRejectionCode
    {
        None, InvalidRequest, WrongRecipe, WrongArea, NoWhitelistedPath,
        QuantityExceedsPath, ActionNotAllowed, CarrierNotAllowed,
        CarrierUnavailable, CarrierPathLocked
    }

    public sealed class OperationZoneSnapshot
    {
        public IReadOnlyList<ProcessingItemStack> Inputs { get; }
        public IReadOnlyList<string> CandidateRecordIds { get; }
        public IReadOnlyList<string> InitialCarrierCandidates { get; }
        public string CarrierId { get; }
        public bool HasInputs { get { return Inputs.Count != 0; } }
        public bool InitialCarrierAmbiguous { get { return InitialCarrierCandidates.Count > 1; } }

        internal OperationZoneSnapshot(IEnumerable<ProcessingItemStack> inputs, IEnumerable<string> records,
            IEnumerable<string> carriers, string carrierId)
        {
            Inputs = new List<ProcessingItemStack>(inputs).AsReadOnly();
            CandidateRecordIds = new List<string>(records).AsReadOnly();
            InitialCarrierCandidates = new List<string>(carriers).AsReadOnly();
            CarrierId = carrierId;
        }
    }

    public sealed class OperationZoneAddResult
    {
        public OperationZoneRejectionCode RejectionCode { get; }
        public string Diagnostic { get; }
        public OperationZoneSnapshot Snapshot { get; }
        public bool Accepted { get { return RejectionCode == OperationZoneRejectionCode.None; } }
        internal OperationZoneAddResult(OperationZoneRejectionCode code, string diagnostic, OperationZoneSnapshot snapshot)
        { RejectionCode = code; Diagnostic = diagnostic; Snapshot = snapshot; }
    }

    /// <summary>One area's prospective T12 multiset. Inventory and scene effects belong to the caller.</summary>
    public sealed class OperationZoneSession
    {
        private readonly RecipeWhitelist whitelist;
        private readonly AreaConstraint area;
        private readonly Dictionary<string, ProcessingRecordView> records;
        private readonly List<string> sceneCarriers;
        private readonly List<ProcessingItemStack> inputs = new List<ProcessingItemStack>();
        private Dictionary<string, int> counts = new Dictionary<string, int>(StringComparer.Ordinal);
        private List<string> candidateRecordIds = new List<string>();
        private List<string> initialCarrierCandidates = new List<string>();
        private string carrierId;

        public OperationZoneSession(RecipeWhitelist whitelist, AreaConstraint area,
            IEnumerable<ProcessingRecordView> records, IEnumerable<string> sceneCarriers)
        {
            if (whitelist == null || string.IsNullOrWhiteSpace(whitelist.RecipeId) ||
                area == null || string.IsNullOrWhiteSpace(area.AreaId) || records == null || sceneCarriers == null)
                throw new ArgumentException("Resolved recipe, area, records and explicit current-area carriers are required.");
            this.whitelist = new RecipeWhitelist(whitelist.RecipeId, whitelist.ProcessingRecordIds);
            this.area = new AreaConstraint(area.AreaId, area.AllowedActions, area.AllowedCarriers);
            this.sceneCarriers = new List<string>(sceneCarriers);
            this.records = new Dictionary<string, ProcessingRecordView>(StringComparer.Ordinal);
            foreach (var record in records)
            {
                if (record == null || string.IsNullOrWhiteSpace(record.Id) || this.records.ContainsKey(record.Id))
                    throw new ArgumentException("Processing record snapshots must have unique IDs.");
                this.records.Add(record.Id, new ProcessingRecordView(record.Id, record.OutputItemId,
                    record.Inputs, record.Action, record.Carrier, record.IsFinalProduct));
            }
            foreach (var id in this.whitelist.ProcessingRecordIds)
                if (string.IsNullOrWhiteSpace(id) || !this.records.ContainsKey(id))
                    throw new ArgumentException("Whitelist snapshot is incomplete: " + id);
        }

        public OperationZoneSnapshot GetSnapshot()
        { return new OperationZoneSnapshot(inputs, candidateRecordIds, initialCarrierCandidates, carrierId); }

        public OperationZoneAddResult TryAdd(string recipeId, string areaId, string itemId, int quantity = 1)
        {
            if (!string.Equals(recipeId, whitelist.RecipeId, StringComparison.Ordinal))
                return Reject(OperationZoneRejectionCode.WrongRecipe, "Recipe identity differs.");
            if (!string.Equals(areaId, area.AreaId, StringComparison.Ordinal))
                return Reject(OperationZoneRejectionCode.WrongArea, "Current area identity differs; no cross-area fallback.");
            if (string.IsNullOrWhiteSpace(itemId) || quantity <= 0)
                return Reject(OperationZoneRejectionCode.InvalidRequest, "A nonempty item and positive quantity are required.");
            var prospective = new Dictionary<string, int>(counts, StringComparer.Ordinal);
            int prior; prospective.TryGetValue(itemId, out prior);
            if (quantity > int.MaxValue - prior)
                return Reject(OperationZoneRejectionCode.InvalidRequest, "Input quantity overflows.");
            prospective[itemId] = prior + quantity;

            var viable = new List<ProcessingRecordView>();
            var firstConstraint = ProcessingRejectionCode.None;
            var otherCarrierPath = false;
            foreach (var id in whitelist.ProcessingRecordIds)
            {
                var record = records[id];
                if (!IsSubset(prospective, record.Inputs)) continue;
                var constraint = AreaConstraintValidator.Check(area, record.Action, record.Carrier, sceneCarriers);
                if (constraint != ProcessingRejectionCode.None)
                { if (firstConstraint == ProcessingRejectionCode.None) firstConstraint = constraint; continue; }
                if (inputs.Count != 0 && !string.Equals(carrierId, CarrierKey(record.Carrier), StringComparison.Ordinal))
                { otherCarrierPath = true; continue; }
                viable.Add(record);
            }
            if (viable.Count == 0)
            {
                if (otherCarrierPath)
                    return Reject(OperationZoneRejectionCode.CarrierPathLocked, "The prospective path uses another carrier.");
                if (firstConstraint != ProcessingRejectionCode.None)
                    return Reject(Map(firstConstraint), "Current area action/carrier constraint: " + firstConstraint);
                return Reject(ExceedsAnyRecord(itemId, prospective[itemId])
                    ? OperationZoneRejectionCode.QuantityExceedsPath : OperationZoneRejectionCode.NoWhitelistedPath,
                    "No whitelisted record contains the prospective input multiset.");
            }

            var nextCarrier = carrierId;
            var nextInitialCandidates = initialCarrierCandidates;
            if (inputs.Count == 0)
            {
                var carrierRecords = new List<ShortCycleCarrierRecord>();
                foreach (var record in viable)
                    carrierRecords.Add(new ShortCycleCarrierRecord(record.Id, record.Inputs, record.Action, record.Carrier));
                var resolved = new ShortCycleCarrierResolver(whitelist, area, carrierRecords)
                    .Resolve(recipeId, areaId, itemId, sceneCarriers);
                nextInitialCandidates = new List<string>(resolved.Candidates);
                nextCarrier = resolved.CarrierId;
                if (nextCarrier == null)
                {
                    var hasBlank = false;
                    foreach (var record in viable) if (string.IsNullOrEmpty(record.Carrier)) { hasBlank = true; break; }
                    if (!hasBlank)
                        return Reject(OperationZoneRejectionCode.CarrierUnavailable, "No current-area carrier candidate.");
                }
                var locked = new List<ProcessingRecordView>();
                foreach (var record in viable)
                    if (string.Equals(nextCarrier, CarrierKey(record.Carrier), StringComparison.Ordinal)) locked.Add(record);
                viable = locked;
            }

            var nextRecords = new List<string>();
            foreach (var record in viable) nextRecords.Add(record.Id);
            counts = prospective;
            inputs.Add(new ProcessingItemStack(itemId, quantity));
            candidateRecordIds = nextRecords;
            initialCarrierCandidates = new List<string>(nextInitialCandidates);
            carrierId = nextCarrier;
            return new OperationZoneAddResult(OperationZoneRejectionCode.None, null, GetSnapshot());
        }

        /// <summary>Call only after the caller commits the completed output; no inventory write occurs here.</summary>
        public OperationZoneSnapshot Complete()
        { var completed = GetSnapshot(); Clear(); return completed; }

        public void Clear()
        {
            inputs.Clear(); counts.Clear(); candidateRecordIds.Clear();
            initialCarrierCandidates.Clear(); carrierId = null;
        }

        private OperationZoneAddResult Reject(OperationZoneRejectionCode code, string diagnostic)
        { return new OperationZoneAddResult(code, diagnostic, GetSnapshot()); }

        private bool ExceedsAnyRecord(string itemId, int quantity)
        {
            var maximum = 0;
            foreach (var id in whitelist.ProcessingRecordIds)
            {
                var count = 0;
                foreach (var input in records[id].Inputs)
                    if (string.Equals(input, itemId, StringComparison.Ordinal)) count++;
                if (count > maximum) maximum = count;
            }
            return maximum != 0 && quantity > maximum;
        }

        private static bool IsSubset(Dictionary<string, int> actual, IEnumerable<string> expected)
        {
            var available = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var id in expected)
            { int count; available.TryGetValue(id, out count); available[id] = count + 1; }
            foreach (var entry in actual)
            { int count; if (!available.TryGetValue(entry.Key, out count) || entry.Value > count) return false; }
            return true;
        }

        private static string CarrierKey(string value) { return string.IsNullOrEmpty(value) ? null : value; }

        private static OperationZoneRejectionCode Map(ProcessingRejectionCode code)
        {
            switch (code)
            {
                case ProcessingRejectionCode.ActionNotAllowed: return OperationZoneRejectionCode.ActionNotAllowed;
                case ProcessingRejectionCode.CarrierNotAllowed: return OperationZoneRejectionCode.CarrierNotAllowed;
                case ProcessingRejectionCode.CarrierUnavailable: return OperationZoneRejectionCode.CarrierUnavailable;
                default: return OperationZoneRejectionCode.InvalidRequest;
            }
        }
    }
}
