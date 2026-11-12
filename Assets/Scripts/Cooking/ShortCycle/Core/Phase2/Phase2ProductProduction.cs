using System;
using System.Collections.Generic;

namespace EatWhat.Cooking.ShortCycle
{
    /// <summary>Names must already be resolved from the current catalog/localization snapshot.</summary>
    public interface IPhase2ProductNameSource
    {
        bool TryGetItemName(string itemId, out string name);
        bool TryGetToolName(string toolId, out string name);
        bool TryGetActionName(string actionId, out string name);
    }

    public sealed class Phase2ProductDraft
    {
        public string ItemId { get; }
        public string DisplayName { get; }
        public string SourceRecordId { get; }
        public bool IsUnknown { get; }
        internal Phase2ProductDraft(string itemId, string displayName, string sourceRecordId, bool isUnknown)
        { ItemId = itemId; DisplayName = displayName; SourceRecordId = sourceRecordId; IsUnknown = isUnknown; }
    }

    /// <summary>Pure naming and match validation. The caller commits through the sole inventory owner.</summary>
    public static class Phase2ProductProduction
    {
        public static bool TryCreate(ProcessingMatchResult match, OperationZoneSnapshot session,
            ProcessingToolView tool, string configuredUnknownId, IPhase2ProductNameSource names,
            out Phase2ProductDraft draft, out string diagnostic)
        {
            draft = null;
            if (match == null || session == null || !session.HasInputs || tool == null ||
                string.IsNullOrWhiteSpace(tool.Id) || names == null || string.IsNullOrWhiteSpace(configuredUnknownId))
            { diagnostic = "A current operation, match, tool, names and configured unknown ID are required."; return false; }

            if (match.ProducesUnknown)
            {
                if (match.RejectionCode != ProcessingRejectionCode.NoWhitelistedMatchInCurrentArea ||
                    !string.Equals(match.OutputItemId, configuredUnknownId, StringComparison.Ordinal))
                { diagnostic = "Unknown output does not match the configured S-04 fallback."; return false; }
                string unknownName;
                if (!names.TryGetItemName(configuredUnknownId, out unknownName) || string.IsNullOrWhiteSpace(unknownName))
                { diagnostic = "Configured unknown product name is unresolved."; return false; }
                draft = new Phase2ProductDraft(configuredUnknownId, unknownName, null, true);
                diagnostic = null; return true;
            }

            if (!match.Matched || match.RejectionCode != ProcessingRejectionCode.None ||
                string.IsNullOrWhiteSpace(match.Record.Id) || string.IsNullOrWhiteSpace(match.OutputItemId) ||
                !Contains(session.CandidateRecordIds, match.Record.Id) ||
                !string.Equals(session.CarrierId ?? string.Empty, match.Record.Carrier ?? string.Empty, StringComparison.Ordinal) ||
                !SameInputs(session.Inputs, match.Record.Inputs) ||
                !Contains(tool.SupportedActions, match.Record.Action))
            { diagnostic = "The matched record does not belong to the current locked operation."; return false; }

            var counts = new SortedDictionary<string, int>(StringComparer.Ordinal);
            foreach (var input in session.Inputs)
            {
                if (input == null || string.IsNullOrWhiteSpace(input.ItemId) || input.Quantity <= 0)
                { diagnostic = "Current operation contains invalid input."; return false; }
                int old; counts.TryGetValue(input.ItemId, out old);
                if (input.Quantity > int.MaxValue - old)
                { diagnostic = "Input quantity overflows."; return false; }
                counts[input.ItemId] = old + input.Quantity;
            }
            var parts = new List<string>();
            foreach (var pair in counts)
            {
                string name;
                if (!names.TryGetItemName(pair.Key, out name) || string.IsNullOrWhiteSpace(name))
                { diagnostic = "Input item name is unresolved: " + pair.Key; return false; }
                parts.Add(pair.Value == 1 ? name : name + " ×" + pair.Value);
            }
            string toolName, actionName;
            if (!names.TryGetToolName(tool.Id, out toolName) || string.IsNullOrWhiteSpace(toolName))
            { diagnostic = "Tool name is unresolved: " + tool.Id; return false; }
            if (!names.TryGetActionName(match.Record.Action, out actionName) || string.IsNullOrWhiteSpace(actionName))
            { diagnostic = "Action name is unresolved: " + match.Record.Action; return false; }
            // Provisional, stable display composition. Final Chinese typography belongs to UX/localization.
            draft = new Phase2ProductDraft(match.OutputItemId,
                string.Join(" + ", parts) + " · " + toolName + " · " + actionName,
                match.Record.Id, false);
            diagnostic = null; return true;
        }

        private static bool Contains(IEnumerable<string> values, string sought)
        {
            foreach (var value in values)
                if (string.Equals(value, sought, StringComparison.Ordinal)) return true;
            return false;
        }

        private static bool SameInputs(IEnumerable<ProcessingItemStack> actual, IEnumerable<string> expected)
        {
            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var input in actual)
            {
                int old; counts.TryGetValue(input.ItemId, out old);
                if (input.Quantity > int.MaxValue - old) return false;
                counts[input.ItemId] = old + input.Quantity;
            }
            var needed = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var id in expected)
            { int old; needed.TryGetValue(id, out old); needed[id] = old + 1; }
            if (counts.Count != needed.Count) return false;
            foreach (var pair in needed)
            { int actualCount; if (!counts.TryGetValue(pair.Key, out actualCount) || actualCount != pair.Value) return false; }
            return true;
        }
    }
}
