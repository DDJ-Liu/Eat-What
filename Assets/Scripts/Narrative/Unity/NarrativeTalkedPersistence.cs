using System;
using System.Collections.Generic;
using EatWhat.Narrative.Core;
using EatWhat.Tools.Persistence;
using EatWhat.Tools.Security;
using Newtonsoft.Json.Linq;

namespace EatWhat.Narrative.Unity
{
    /// <summary>Application host for the existing record and Toolkit store; owns no second ID set or codec.</summary>
    public sealed class NarrativeTalkedPersistence : INarrativeTalkedRecord
    {
        public const string Slot = "long_loop";
        public const string Section = "dialogue_talked";
        readonly NarrativeTalkedRecord record = new NarrativeTalkedRecord();
        readonly JsonSaveStore store;
        readonly Action<string> diagnostic;
        readonly Func<DateTime> utcNow;
        JObject sections = new JObject();
        string createdUtc;
        bool blocked, writeWarned;
        public bool IsInitialized { get; private set; }
        public bool IsWriteBlocked { get { return blocked || store.IsWriteBlocked(Slot); } }
        public bool? LastSaveSucceeded { get; private set; }
        public int Count { get { return record.Count; } }

        public NarrativeTalkedPersistence(JsonSaveStore store, Action<string> diagnostic = null, Func<DateTime> utcNow = null)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.diagnostic = diagnostic ?? (message => UnityEngine.Debug.LogWarning(message));
            this.utcNow = utcNow ?? (() => DateTime.UtcNow);
        }
        public static NarrativeTalkedPersistence CreateForCurrentBuild(string root, IKeyProvider keys,
            Action<string> diagnostic = null, Func<DateTime> utcNow = null)
        {
            if (keys == null) throw new ArgumentNullException(nameof(keys));
            return new NarrativeTalkedPersistence(new JsonSaveStore(root, SaveCodecFactory.CreateForCurrentBuild(keys),
                new EnvelopeAuthenticator(keys)), diagnostic, utcNow);
        }

        /// <summary>09 calls this after its runtime entry is available. An unavailable entry performs no IO.</summary>
        public bool InitializeWhenAvailable(bool runtimeAvailable)
        {
            if (IsInitialized) return true;
            if (!runtimeAvailable) return false;
            IsInitialized = true;
            SaveEnvelope envelope; string issue;
            if (!store.TryRead(Slot, out envelope, out issue))
            {
                if (issue == null) diagnostic("TALKED_FILE_MISSING: starting with an empty dialogue record.");
                else Block("TALKED_LOAD_FAILED: empty dialogue record; original file is preserved and writes are blocked.");
                return true; // Storage faults never prevent the service from using the empty memory record.
            }
            try
            {
                var loaded = envelope.Payload;
                JToken value;
                if (loaded.TryGetValue(Section, out value))
                {
                    var array = value as JArray;
                    if (array == null) throw new FormatException("Dialogue record section must be a list.");
                    var ids = new List<string>();
                    foreach (var item in array)
                    {
                        if (item.Type != JTokenType.String || string.IsNullOrWhiteSpace((string)item))
                            throw new FormatException("Dialogue record IDs must be nonblank strings.");
                        ids.Add((string)item);
                    }
                    record.Restore(ids);
                } // A valid file without this newly added section means an empty record, without warning.
                sections = loaded; createdUtc = envelope.CreatedUtc;
            }
            catch (FormatException)
            { Block("TALKED_SECTION_INVALID: empty dialogue record; original file is preserved and writes are blocked."); }
            return true;
        }
        public bool HasTalked(string id) { return record.HasTalked(id); }
        public List<string> Export() { return record.Export(); }
        public void MarkTalked(string id) { CheckInitialized(); record.MarkTalked(id); SaveNow(); }
        public int Clear(IEnumerable<string> ids) { CheckInitialized(); int removed = record.Clear(ids); SaveNow(); return removed; }
        public void ClearAll() { CheckInitialized(); record.ClearAll(); SaveNow(); }
        public void Restore(IEnumerable<string> ids) { CheckInitialized(); record.Restore(ids); SaveNow(); }

        public bool SaveNow()
        {
            CheckInitialized();
            if (IsWriteBlocked) { LastSaveSucceeded = false; return false; }
            var next = (JObject)sections.DeepClone();
            next[Section] = new JArray(record.Export());
            var envelope = createdUtc == null ? SaveEnvelope.Create(next, utcNow()) :
                new SaveEnvelope(SaveEnvelope.CurrentFormatVersion, SaveEnvelope.CurrentSchemaVersion, createdUtc, next, "");
            string issue;
            bool saved = store.Write(Slot, envelope, out issue);
            LastSaveSucceeded = saved;
            if (saved) { sections = next; createdUtc = envelope.CreatedUtc; }
            else
            {
                blocked = true; store.BlockSlot(Slot);
                if (!writeWarned)
                { writeWarned = true; diagnostic("TALKED_WRITE_FAILED: memory record remains usable; original file is preserved and further writes are blocked."); }
            }
            return saved;
        }
        void Block(string message)
        { blocked = true; store.BlockSlot(Slot); diagnostic(message); }
        void CheckInitialized()
        { if (!IsInitialized) throw new InvalidOperationException("Initialize after the runtime entry becomes available before changing dialogue records."); }
    }
}
