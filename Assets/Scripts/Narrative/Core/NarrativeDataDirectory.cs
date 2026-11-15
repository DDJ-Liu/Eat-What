using System;
using System.Collections.Generic;

namespace EatWhat.Narrative.Core
{
    public enum NarrativeDataDiagnosticKind { InvalidRow, MissingKey, DuplicateKey, LineCycle, DialogueLabelMismatch }

    public sealed class NarrativeDataDiagnostic
    {
        public NarrativeDataDiagnostic(NarrativeDataDiagnosticKind kind, string table, string key, string reason)
        { Kind = kind; Table = table; Key = key; Reason = reason; }
        public NarrativeDataDiagnosticKind Kind { get; private set; }
        public string Table { get; private set; }
        public string Key { get; private set; }
        public string Reason { get; private set; }
        public bool IsError { get { return Kind != NarrativeDataDiagnosticKind.DialogueLabelMismatch; } }
    }

    public sealed class NarrativeLineChain
    {
        internal NarrativeLineChain(List<NarrativeDialogueLine> lines, List<NarrativeDataDiagnostic> diagnostics)
        { Lines = new List<NarrativeDialogueLine>(lines).AsReadOnly(); Diagnostics = new List<NarrativeDataDiagnostic>(diagnostics).AsReadOnly(); }
        public IReadOnlyList<NarrativeDialogueLine> Lines { get; private set; }
        public IReadOnlyList<NarrativeDataDiagnostic> Diagnostics { get; private set; }
    }

    public interface INarrativeDataDirectory
    {
        IReadOnlyList<NarrativeDataDiagnostic> Diagnostics { get; }
        bool TryGetCharacter(string id, out NarrativeCharacter model, out string diagnostic);
        bool TryGetDialogue(string id, out NarrativeDialogue model, out string diagnostic);
        bool TryGetDialogueLine(string id, out NarrativeDialogueLine model, out string diagnostic);
        bool TryGetCg(string id, out NarrativeCg model, out string diagnostic);
        bool TryGetScene(string id, out NarrativeScene model, out string diagnostic);
        bool TryGetSceneState(string sceneId, string stateId, out NarrativeSceneState model, out string diagnostic);
        bool TryGetSceneStatePlacement(string sceneId, string stateId, string kind, string target, out NarrativeSceneStatePlacement model, out string diagnostic);
        bool TryGetAmbientPool(string id, out NarrativeAmbientPool model, out string diagnostic);
        // On failure, chain still contains the unambiguous prefix and all traversal diagnostics.
        bool TryGetLineChain(string dialogueId, out NarrativeLineChain chain, out string diagnostic);
    }

    // Immutable indices; no execution/progress state, source rows or engine objects are retained.
    public sealed class NarrativeDataDirectory : INarrativeDataDirectory
    {
        readonly Index<string, NarrativeCharacter> characters;
        readonly Index<string, NarrativeDialogue> dialogues;
        readonly Index<string, NarrativeDialogueLine> lines;
        readonly Index<string, NarrativeCg> cgs;
        readonly Index<string, NarrativeScene> scenes;
        readonly Index<Tuple<string, string>, NarrativeSceneState> states;
        readonly Index<Tuple<string, string, string, string>, NarrativeSceneStatePlacement> placements;
        readonly Index<string, NarrativeAmbientPool> pools;

        public NarrativeDataDirectory(IEnumerable<NarrativeCharacter> characters = null,
            IEnumerable<NarrativeDialogue> dialogues = null, IEnumerable<NarrativeDialogueLine> lines = null,
            IEnumerable<NarrativeCg> cgs = null, IEnumerable<NarrativeScene> scenes = null,
            IEnumerable<NarrativeSceneState> states = null, IEnumerable<NarrativeSceneStatePlacement> placements = null,
            IEnumerable<NarrativeAmbientPool> pools = null)
        {
            var issues = new List<NarrativeDataDiagnostic>();
            this.characters = new Index<string, NarrativeCharacter>("Characters", characters, x => x.CharacterId, x => Present(x.CharacterId), issues);
            this.dialogues = new Index<string, NarrativeDialogue>("Dialogues", dialogues, x => x.DialogueId, x => Present(x.DialogueId), issues);
            this.lines = new Index<string, NarrativeDialogueLine>("DialogueLines", lines, x => x.LineId, x => Present(x.LineId), issues);
            this.cgs = new Index<string, NarrativeCg>("CGs", cgs, x => x.CgId, x => Present(x.CgId), issues);
            this.scenes = new Index<string, NarrativeScene>("Scenes", scenes, x => x.SceneId, x => Present(x.SceneId), issues);
            this.states = new Index<Tuple<string, string>, NarrativeSceneState>("SceneStates", states,
                x => Tuple.Create(x.SceneId, x.StateId), x => Present(x.SceneId, x.StateId), issues);
            this.placements = new Index<Tuple<string, string, string, string>, NarrativeSceneStatePlacement>("SceneStatePlacements", placements,
                x => Tuple.Create(x.SceneId, x.StateId, x.Kind, x.Target), x => Present(x.SceneId, x.StateId, x.Kind, x.Target), issues);
            this.pools = new Index<string, NarrativeAmbientPool>("AmbientPools", pools, x => x.PoolId, x => Present(x.PoolId), issues);
            Diagnostics = issues.AsReadOnly();
        }
        public IReadOnlyList<NarrativeDataDiagnostic> Diagnostics { get; private set; }
        static bool Present(params string[] parts) { foreach (var part in parts) if (string.IsNullOrEmpty(part)) return false; return true; }

        public bool TryGetCharacter(string id, out NarrativeCharacter model, out string diagnostic) { return characters.TryGet(id ?? "", out model, out diagnostic); }
        public bool TryGetDialogue(string id, out NarrativeDialogue model, out string diagnostic) { return dialogues.TryGet(id ?? "", out model, out diagnostic); }
        public bool TryGetDialogueLine(string id, out NarrativeDialogueLine model, out string diagnostic) { return lines.TryGet(id ?? "", out model, out diagnostic); }
        public bool TryGetCg(string id, out NarrativeCg model, out string diagnostic) { return cgs.TryGet(id ?? "", out model, out diagnostic); }
        public bool TryGetScene(string id, out NarrativeScene model, out string diagnostic) { return scenes.TryGet(id ?? "", out model, out diagnostic); }
        public bool TryGetAmbientPool(string id, out NarrativeAmbientPool model, out string diagnostic) { return pools.TryGet(id ?? "", out model, out diagnostic); }
        public bool TryGetSceneState(string sceneId, string stateId, out NarrativeSceneState model, out string diagnostic)
        { return states.TryGet(Tuple.Create(sceneId ?? "", stateId ?? ""), out model, out diagnostic); }
        public bool TryGetSceneStatePlacement(string sceneId, string stateId, string kind, string target, out NarrativeSceneStatePlacement model, out string diagnostic)
        { return placements.TryGet(Tuple.Create(sceneId ?? "", stateId ?? "", kind ?? "", target ?? ""), out model, out diagnostic); }

        public bool TryGetLineChain(string dialogueId, out NarrativeLineChain chain, out string diagnostic)
        {
            var result = new List<NarrativeDialogueLine>(); var issues = new List<NarrativeDataDiagnostic>();
            NarrativeDialogue dialogue; diagnostic = null;
            if (!TryGetDialogue(dialogueId, out dialogue, out diagnostic))
                issues.Add(new NarrativeDataDiagnostic(dialogues.Problem(dialogueId ?? ""), "Dialogues", dialogueId, diagnostic));
            else if (dialogue.FirstLine.Length == 0)
            {
                diagnostic = "Dialogue has no first_line: " + dialogue.DialogueId;
                issues.Add(new NarrativeDataDiagnostic(NarrativeDataDiagnosticKind.InvalidRow, "Dialogues", dialogueId, diagnostic));
            }
            else
            {
                var visited = new Dictionary<string, bool>(StringComparer.Ordinal); string next = dialogue.FirstLine;
                while (next.Length != 0)
                {
                    if (visited.ContainsKey(next))
                    {
                        diagnostic = "Dialogue line cycle at " + next;
                        issues.Add(new NarrativeDataDiagnostic(NarrativeDataDiagnosticKind.LineCycle, "DialogueLines", next, diagnostic)); break;
                    }
                    visited.Add(next, true);
                    NarrativeDialogueLine line;
                    if (!TryGetDialogueLine(next, out line, out diagnostic))
                    { issues.Add(new NarrativeDataDiagnostic(lines.Problem(next), "DialogueLines", next, diagnostic)); break; }
                    result.Add(line);
                    if (!string.Equals(line.DialogueId, dialogue.DialogueId, StringComparison.Ordinal))
                        issues.Add(new NarrativeDataDiagnostic(NarrativeDataDiagnosticKind.DialogueLabelMismatch, "DialogueLines", next,
                            "Chain for " + dialogue.DialogueId + " contains label " + line.DialogueId + "; first_line/next_line remain authoritative."));
                    next = line.NextLine;
                }
            }
            chain = new NarrativeLineChain(result, issues); return diagnostic == null;
        }

        sealed class Index<TKey, T> where T : class
        {
            readonly string table;
            readonly Dictionary<TKey, T> values = new Dictionary<TKey, T>();
            readonly Dictionary<TKey, bool> duplicates = new Dictionary<TKey, bool>();
            internal Index(string table, IEnumerable<T> source, Func<T, TKey> key, Func<T, bool> valid, List<NarrativeDataDiagnostic> issues)
            {
                this.table = table; if (source == null) return;
                foreach (var value in source)
                {
                    if (value == null || !valid(value))
                    { issues.Add(new NarrativeDataDiagnostic(NarrativeDataDiagnosticKind.InvalidRow, table, "", "Null row or incomplete key in " + table)); continue; }
                    TKey id = key(value);
                    if (duplicates.ContainsKey(id)) continue;
                    if (values.ContainsKey(id))
                    {
                        values.Remove(id); duplicates.Add(id, true);
                        issues.Add(new NarrativeDataDiagnostic(NarrativeDataDiagnosticKind.DuplicateKey, table, id.ToString(), "Duplicate complete key in " + table));
                    }
                    else values.Add(id, value);
                }
            }
            internal NarrativeDataDiagnosticKind Problem(TKey id)
            { return duplicates.ContainsKey(id) ? NarrativeDataDiagnosticKind.DuplicateKey : NarrativeDataDiagnosticKind.MissingKey; }
            internal bool TryGet(TKey id, out T value, out string diagnostic)
            {
                value = null;
                if (duplicates.ContainsKey(id)) { diagnostic = "Duplicate complete key in " + table + ": " + id; return false; }
                if (!values.TryGetValue(id, out value)) { diagnostic = "No matching complete key in " + table + ": " + id; return false; }
                diagnostic = null; return true;
            }
        }
    }
}
