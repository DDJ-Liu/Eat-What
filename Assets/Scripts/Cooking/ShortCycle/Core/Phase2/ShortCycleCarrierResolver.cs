using System;
using System.Collections.Generic;

namespace EatWhat.Cooking.ShortCycle
{
    /// <summary>Pure S-01/S-02 lookup; the caller supplies read-only catalog and scene snapshots.</summary>
    public sealed class ShortCycleCarrierResolver
    {
        private readonly RecipeWhitelist whitelist;
        private readonly AreaConstraint area;
        private readonly Dictionary<string, ShortCycleCarrierRecord> records;

        public ShortCycleCarrierResolver(RecipeWhitelist whitelist, AreaConstraint area,
            IEnumerable<ShortCycleCarrierRecord> records)
        {
            this.whitelist = whitelist;
            this.area = area;
            this.records = new Dictionary<string, ShortCycleCarrierRecord>(StringComparer.Ordinal);
            if (records == null) return;
            foreach (var record in records)
            {
                if (record != null && !string.IsNullOrEmpty(record.Id) &&
                    !this.records.ContainsKey(record.Id))
                    this.records.Add(record.Id, record);
            }
        }

        public ShortCycleCarrierResolution Resolve(string recipeId, string areaId,
            string itemId, IEnumerable<string> sceneCarriers)
        {
            var candidates = new List<string>();
            if (whitelist == null || area == null || string.IsNullOrEmpty(itemId) ||
                !string.Equals(whitelist.RecipeId, recipeId, StringComparison.Ordinal) ||
                !string.Equals(area.AreaId, areaId, StringComparison.Ordinal))
                return new ShortCycleCarrierResolution(candidates);

            var present = new HashSet<string>(sceneCarriers ?? new string[0], StringComparer.Ordinal);
            var actions = new HashSet<string>(area.AllowedActions, StringComparer.Ordinal);
            var carriers = new HashSet<string>(area.AllowedCarriers, StringComparer.Ordinal);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var recordId in whitelist.ProcessingRecordIds)
            {
                ShortCycleCarrierRecord record;
                if (recordId == null || !records.TryGetValue(recordId, out record) ||
                    !actions.Contains(record.Action) || string.IsNullOrEmpty(record.Carrier) ||
                    !present.Contains(record.Carrier) || !carriers.Contains(record.Carrier) ||
                    !ContainsItem(record.Inputs, itemId) || !seen.Add(record.Carrier))
                    continue;
                candidates.Add(record.Carrier);
            }
            return new ShortCycleCarrierResolution(candidates);
        }

        private static bool ContainsItem(IEnumerable<string> inputs, string itemId)
        {
            foreach (var input in inputs)
                if (string.Equals(input, itemId, StringComparison.Ordinal)) return true;
            return false;
        }
    }
}
