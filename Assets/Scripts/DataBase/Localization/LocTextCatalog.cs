using System;
using System.Collections.Generic;

namespace EatWhat.Localization
{
    [Serializable]
    public sealed class LocTextEntry
    {
        public string Key;
        public string ZhCn;
    }

    /// <summary>Unity-independent lookup used by LocService and pure-code tests.</summary>
    public sealed class LocTextCatalog
    {
        private readonly Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly HashSet<string> warnedMissing = new HashSet<string>(StringComparer.Ordinal);

        public void Replace(IEnumerable<LocTextEntry> entries)
        {
            values.Clear();
            warnedMissing.Clear();
            if (entries == null) return;
            foreach (var entry in entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.Key)) continue;
                values[entry.Key] = entry.ZhCn ?? string.Empty;
            }
        }

        public string Get(string key, Action<string> warning)
        {
            string value;
            if (!string.IsNullOrWhiteSpace(key) && values.TryGetValue(key, out value)) return value;
            var displayKey = key ?? string.Empty;
            if (warnedMissing.Add(displayKey) && warning != null) warning("Missing localization key: " + displayKey);
            return "#" + displayKey + "#";
        }
    }
}
