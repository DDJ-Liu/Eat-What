using System;
using System.Collections.Generic;
namespace EatWhat.Cooking.ShortCycle
{
    public sealed class ProcessingItemStack
    {
        public string ItemId { get; }
        public int Quantity { get; }
        public ProcessingItemStack(string itemId, int quantity = 1)
        { if (string.IsNullOrWhiteSpace(itemId) || quantity <= 0) throw new ArgumentException("A stack needs an ID and positive quantity.");
          ItemId = itemId; Quantity = quantity; }
    }
    public sealed class ProcessingMatchResult
    {
        public ProcessingRecordView Record { get; }
        public string OutputItemId { get; }
        public ProcessingRejectionCode RejectionCode { get; }
        public IReadOnlyList<string> CandidateRecordIds { get; }
        public bool Matched { get { return Record != null; } }
        public bool ProducesUnknown { get { return !Matched && OutputItemId != null; } }
        internal ProcessingMatchResult(ProcessingRecordView record, string output, ProcessingRejectionCode rejection, IEnumerable<string> candidates)
        { Record = record; OutputItemId = output; RejectionCode = rejection;
          CandidateRecordIds = new List<string>(candidates).AsReadOnly(); }
    }
    /// <summary>S-01 pure capabilities over snapshots. Only the supplied current area is searched.</summary>
    public sealed class ProcessingMatcher
    {
        private readonly RecipeWhitelist whitelist;
        private readonly AreaConstraint area;
        private readonly Dictionary<string, ProcessingRecordView> records = new Dictionary<string, ProcessingRecordView>(StringComparer.Ordinal);
        private readonly string unknownProductItemId;
        public ProcessingMatcher(RecipeWhitelist whitelist, AreaConstraint area, IEnumerable<ProcessingRecordView> records, string unknownProductItemId)
        {
            if (whitelist == null || area == null || string.IsNullOrWhiteSpace(unknownProductItemId)) throw new ArgumentException("Resolved catalog snapshots and unknown product ID are required.");
            this.whitelist = whitelist; this.area = area; this.unknownProductItemId = unknownProductItemId;
            foreach (var record in records ?? new ProcessingRecordView[0])
                if (record != null && !string.IsNullOrEmpty(record.Id))
                { if (this.records.ContainsKey(record.Id)) throw new ArgumentException("Duplicate processing snapshot ID: " + record.Id);
                  this.records.Add(record.Id, record); }
            foreach (var id in whitelist.ProcessingRecordIds)
                if (id == null || !this.records.ContainsKey(id)) throw new ArgumentException("Whitelist snapshot is incomplete: " + id);
        }
        public ProcessingMatchResult Match(string recipeId, string areaId, IEnumerable<ProcessingItemStack> inputs,
            ProcessingToolView tool, IEnumerable<string> currentAreaCarriers)
        {
            if (recipeId != whitelist.RecipeId || areaId != area.AreaId || tool == null)
                return Reject(ProcessingRejectionCode.InvalidRequest);
            var actual = Counts(inputs);
            if (actual.Count == 0) return Reject(ProcessingRejectionCode.InvalidRequest);
            var supported = new HashSet<string>(tool.SupportedActions, StringComparer.Ordinal);
            if (supported.Count == 0) return Reject(ProcessingRejectionCode.ToolActionUnsupported);
            var present = new HashSet<string>(currentAreaCarriers ?? new string[0], StringComparer.Ordinal);
            var candidates = new List<string>(); ProcessingRecordView selected = null;
            var rejection = ProcessingRejectionCode.NoWhitelistedMatchInCurrentArea;
            foreach (var id in whitelist.ProcessingRecordIds)
            {
                var record = records[id];
                if (!SameInputs(actual, record.Inputs)) continue;
                if (!supported.Contains(record.Action)) { rejection = ProcessingRejectionCode.ToolActionUnsupported; continue; }
                var constraint = AreaConstraintValidator.Check(area, record.Action, record.Carrier, present);
                if (constraint != ProcessingRejectionCode.None) { rejection = constraint; continue; }
                if (selected == null) selected = record;
                candidates.Add(id);
            }
            if (selected != null) return new ProcessingMatchResult(selected, selected.OutputItemId, ProcessingRejectionCode.None, candidates);
            // Invalid area/tool/carrier operations are refused. A valid but unknown combination has the configured S-04 output.
            if (rejection != ProcessingRejectionCode.NoWhitelistedMatchInCurrentArea) return Reject(rejection);
            var anyAllowed = false;
            foreach (var action in supported) if (area.AllowedActions.Contains(action)) { anyAllowed = true; break; }
            return anyAllowed ? new ProcessingMatchResult(null, unknownProductItemId, rejection, candidates)
                : Reject(ProcessingRejectionCode.ActionNotAllowed);
        }
        private static ProcessingMatchResult Reject(ProcessingRejectionCode code)
        { return new ProcessingMatchResult(null, null, code, new string[0]); }
        internal static Dictionary<string, int> Counts(IEnumerable<ProcessingItemStack> inputs)
        {
            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var input in inputs ?? new ProcessingItemStack[0])
            { if (input == null) throw new ArgumentException("Null input stack.");
              int old; counts.TryGetValue(input.ItemId, out old); counts[input.ItemId] = checked(old + input.Quantity); }
            return counts;
        }
        private static bool SameInputs(Dictionary<string, int> actual, IEnumerable<string> expected)
        {
            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var id in expected) { int count; counts.TryGetValue(id, out count); counts[id] = count + 1; }
            if (actual.Count != counts.Count) return false;
            foreach (var pair in counts) { int count; if (!actual.TryGetValue(pair.Key, out count) || count != pair.Value) return false; }
            return true;
        }
    }
}
