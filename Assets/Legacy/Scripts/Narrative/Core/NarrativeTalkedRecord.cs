using System;
using System.Collections.Generic;

namespace EatWhat.Narrative.Core
{
    /// <summary>Owns the in-memory set only. No storage, file format, time or Unity dependencies.</summary>
    public sealed class NarrativeTalkedRecord : INarrativeTalkedRecord
    {
        // Dictionary keeps the existing Core assembly's mscorlib-only dependency contract.
        readonly Dictionary<string, byte> dialogueIds = new Dictionary<string, byte>(StringComparer.Ordinal);
        public int Count { get { return dialogueIds.Count; } }

        public void MarkTalked(string dialogueId)
        { CheckId(dialogueId); dialogueIds[dialogueId] = 0; }

        public bool HasTalked(string dialogueId)
        { return dialogueId != null && dialogueIds.ContainsKey(dialogueId); }

        public int Clear(IEnumerable<string> ids)
        {
            var requested = CopyIds(ids); int removed = 0;
            foreach (string id in requested.Keys) if (dialogueIds.Remove(id)) removed++;
            return removed;
        }

        public void ClearAll() { dialogueIds.Clear(); }

        // Detached, deterministic list. Mutating it cannot mutate the record owner.
        public List<string> Export()
        {
            var result = new List<string>(dialogueIds.Keys); result.Sort(StringComparer.Ordinal); return result;
        }

        // Validate/copy before replacing, so invalid or failed enumeration cannot partially clear live state.
        public void Restore(IEnumerable<string> ids)
        {
            var replacement = CopyIds(ids); dialogueIds.Clear();
            foreach (string id in replacement.Keys) dialogueIds.Add(id, 0);
        }

        static Dictionary<string, byte> CopyIds(IEnumerable<string> ids)
        {
            if (ids == null) throw new ArgumentNullException(nameof(ids));
            var result = new Dictionary<string, byte>(StringComparer.Ordinal);
            foreach (string id in ids) { CheckId(id); result[id] = 0; }
            return result;
        }
        static void CheckId(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A nonblank dialogue ID is required.", nameof(id));
            // Keep exact ordinal identity. Do not trim, case-fold or consult the current dialogue directory.
        }
    }
}
