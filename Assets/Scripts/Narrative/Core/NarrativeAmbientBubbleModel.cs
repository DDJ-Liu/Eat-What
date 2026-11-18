using System;
using System.Collections.Generic;

namespace EatWhat.Narrative.Core
{
    public sealed class NarrativeAmbientBubbleEmission
    {
        internal NarrativeAmbientBubbleEmission(string npc, string pool, string dialogue, NarrativeDialogueLine line,
            double triggeredAt, double nextAllowed)
        {
            NpcId = npc; PoolId = pool; DialogueId = dialogue; LineId = line.LineId; TextKey = line.TextKey;
            SpeakerId = line.Speaker.Length == 0 ? npc : line.Speaker;
            TriggeredAtSeconds = triggeredAt; NextAllowedAtSeconds = nextAllowed;
        }
        public string NpcId { get; private set; }
        public string PoolId { get; private set; }
        public string DialogueId { get; private set; }
        public string LineId { get; private set; }
        public string TextKey { get; private set; }
        public string SpeakerId { get; private set; }
        public double TriggeredAtSeconds { get; private set; }
        public double NextAllowedAtSeconds { get; private set; }
    }

    public sealed class NarrativeAmbientBubbleDiagnostic
    {
        internal NarrativeAmbientBubbleDiagnostic(string code, string pool, string context, string reason)
        { Code = code; PoolId = pool; Context = context; Reason = reason; }
        public string Code { get; private set; }
        public string PoolId { get; private set; }
        public string Context { get; private set; }
        public string Reason { get; private set; }
    }

    /// <summary>One NPC's ambient pool progress and cooldown. All distance/time/random inputs are external.</summary>
    public sealed class NarrativeAmbientBubbleModel
    {
        private sealed class Segment
        {
            public NarrativeDialogue Dialogue;
            public NarrativeLineChain Chain;
        }
        private readonly INarrativeDataDirectory directory;
        private readonly List<NarrativeAmbientBubbleDiagnostic> issues = new List<NarrativeAmbientBubbleDiagnostic>();
        private readonly Dictionary<string, bool> reported = new Dictionary<string, bool>(StringComparer.Ordinal);
        private List<Segment> segments;
        private bool poolResolved;
        private Segment current;
        private int nextLine;
        private string lastDialogue;

        public NarrativeAmbientBubbleModel(string npcId, INarrativeDataDirectory directory)
        {
            if (string.IsNullOrEmpty(npcId)) throw new ArgumentException("A stable NPC id is required.", "npcId");
            if (directory == null) throw new ArgumentNullException("directory");
            NpcId = npcId; this.directory = directory;
            PoolId = string.Empty;
            Diagnostics = issues.AsReadOnly();
        }
        public string NpcId { get; private set; }
        public string PoolId { get; private set; }
        public double? CooldownUntilSeconds { get; private set; }
        // Append-only, de-duplicated for this NPC lifetime. Consumers log only newly appended entries.
        public IReadOnlyList<NarrativeAmbientBubbleDiagnostic> Diagnostics { get; private set; }

        public void ChangePool(string poolId)
        {
            string next = poolId ?? string.Empty;
            if (string.Equals(PoolId, next, StringComparison.Ordinal)) return;
            PoolId = next; segments = null; poolResolved = false;
            current = null; nextLine = 0; lastDialogue = null;
            // The previous successful trigger's deadline belongs to the NPC, not the pool.
        }

        public bool TryTrigger(double nowSeconds, double groundDistance, DialogueGroupSnapshot trigger,
            Func<int, int> chooseIndex, out NarrativeAmbientBubbleEmission emission)
        {
            emission = null;
            if (!Finite(nowSeconds) || nowSeconds < 0 || !Finite(groundDistance) || groundDistance < 0)
            { Report("INVALID_INPUT", "time/distance", "Expected finite nonnegative seconds and ground distance."); return false; }
            if (trigger == null || trigger.Group != "trigger")
            { Report("MISSING_TRIGGER", "trigger", "A resolved trigger group is required."); return false; }
            // Bubble settings are distinct from the player's interactRadius.
            float radius = trigger.Get<float>("autoBubbleRadius");
            float cooldown = trigger.Get<float>("autoBubbleCooldown");
            if (!Finite(radius) || radius < 0 || !Finite(cooldown) || cooldown < 0)
            { Report("INVALID_TRIGGER", "trigger", "Bubble radius and cooldown must be finite and nonnegative."); return false; }
            if (groundDistance >= radius || (CooldownUntilSeconds.HasValue && nowSeconds < CooldownUntilSeconds.Value)) return false;
            return TryEmit(nowSeconds, cooldown, true, chooseIndex, out emission);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public bool DebugForce(double nowSeconds, Func<int, int> chooseIndex, out NarrativeAmbientBubbleEmission emission)
        {
            emission = null;
            if (!Finite(nowSeconds) || nowSeconds < 0)
            { Report("INVALID_INPUT", "time", "Expected finite nonnegative seconds."); return false; }
            return TryEmit(nowSeconds, 0, false, chooseIndex, out emission);
        }
#endif
        private bool TryEmit(double nowSeconds, float cooldown, bool updateCooldown,
            Func<int, int> chooseIndex, out NarrativeAmbientBubbleEmission emission)
        {
            emission = null;
            if (!ResolvePool()) return false;

            Segment selected = current;
            int lineIndex = nextLine;
            if (selected == null || lineIndex >= selected.Chain.Lines.Count)
            {
                int count = segments.Count - (segments.Count > 1 && lastDialogue != null ? 1 : 0);
                if (chooseIndex == null)
                { Report("INVALID_RANDOM", "random", "An external index chooser is required for a new segment."); return false; }
                int choice;
                try { choice = chooseIndex(count); }
                catch (Exception error)
                { Report("INVALID_RANDOM", "random", "External index chooser failed: " + error.GetType().Name); return false; }
                if (choice < 0 || choice >= count)
                { Report("INVALID_RANDOM", "random", "External chooser returned an index outside the requested range."); return false; }
                foreach (var segment in segments)
                {
                    if (segments.Count > 1 && segment.Dialogue.DialogueId == lastDialogue) continue;
                    if (choice-- == 0) { selected = segment; break; }
                }
                lineIndex = 0;
            }
            double deadline = updateCooldown ? nowSeconds + cooldown : (CooldownUntilSeconds ?? nowSeconds);
            if (!Finite(deadline))
            { Report("INVALID_INPUT", "deadline", "Cooldown deadline must remain finite."); return false; }
            var line = selected.Chain.Lines[lineIndex];
            current = selected; nextLine = lineIndex + 1; lastDialogue = selected.Dialogue.DialogueId;
            if (updateCooldown) CooldownUntilSeconds = deadline;
            if (!string.IsNullOrWhiteSpace(line.Commands.SourceText))
                Report("COMMANDS_IGNORED", line.LineId, "Ambient line commands are ignored; the bubble is still emitted.");
            if (!string.IsNullOrWhiteSpace(selected.Dialogue.OnEnd.SourceText))
                Report("ON_END_IGNORED", selected.Dialogue.DialogueId, "Ambient on_end is not executed.");
            emission = new NarrativeAmbientBubbleEmission(NpcId, PoolId, selected.Dialogue.DialogueId, line, nowSeconds, deadline);
            return true;
        }

        private bool ResolvePool()
        {
            if (poolResolved) return segments != null;
            poolResolved = true;
            NarrativeAmbientPool pool; string diagnostic;
            if (!directory.TryGetAmbientPool(PoolId, out pool, out diagnostic))
            { Report("MISSING_POOL", PoolId, diagnostic); return false; }
            if (pool.DialogueIds.Count == 0)
            { Report("EMPTY_POOL", PoolId, "Ambient pool contains no dialogues."); return false; }
            var loaded = new List<Segment>();
            var seen = new Dictionary<string, bool>(StringComparer.Ordinal);
            bool invalid = false;
            foreach (string rawId in pool.DialogueIds)
            {
                string id = rawId ?? string.Empty;
                if (seen.ContainsKey(id)) continue;
                seen.Add(id, true);
                NarrativeDialogue dialogue;
                if (!directory.TryGetDialogue(id, out dialogue, out diagnostic))
                { Report("MISSING_DIALOGUE", id, diagnostic); invalid = true; continue; }
                if (dialogue.Level != NarrativeDialogueLevel.Ambient)
                { Report("NOT_AMBIENT", id, "Pool dialogue is not ambient."); invalid = true; continue; }
                NarrativeLineChain chain;
                if (!directory.TryGetLineChain(id, out chain, out diagnostic) || chain.Lines.Count == 0)
                { Report("INVALID_CHAIN", id, diagnostic ?? "Ambient dialogue has no lines."); invalid = true; continue; }
                foreach (var issue in chain.Diagnostics)
                    Report("CHAIN_DIAGNOSTIC", id + "/" + issue.Key, issue.Reason);
                loaded.Add(new Segment { Dialogue = dialogue, Chain = chain });
            }
            // A mixed good/bad pool must not occasionally look healthy based on the random choice.
            if (invalid || loaded.Count == 0) return false;
            segments = loaded;
            return true;
        }
        private void Report(string code, string context, string reason)
        {
            string key = code + "\0" + PoolId + "\0" + context;
            if (reported.ContainsKey(key)) return;
            reported.Add(key, true);
            issues.Add(new NarrativeAmbientBubbleDiagnostic(code, PoolId, context, reason));
        }
        private static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
    }
}
