using System;
using System.Collections.Generic;
namespace EatWhat.Cooking.ShortCycle
{
    public enum StepCompletionState { None, Dynamic, Permanent }
    public sealed class StepProgressEntry
    {
        public string StepId { get; }
        public StepCompletionState State { get; }
        internal StepProgressEntry(string id, StepCompletionState state) { StepId = id; State = state; }
    }
    public sealed class FinalDishReadyEvent
    {
        public string RecipeId { get; }
        public string TailStepId { get; }
        internal FinalDishReadyEvent(string recipeId, string tailStepId) { RecipeId = recipeId; TailStepId = tailStepId; }
    }
    /// <summary>Call on every prep-zone change (including manual drops). The owner decides what happens after a tail completion.</summary>
    public sealed class StepProgress
    {
        private readonly string recipeId;
        private readonly string unknownProductItemId;
        private readonly List<RecipeStepRuleView> chain;
        private readonly StepCompletionState[] states;
        private Dictionary<string, int> previous = new Dictionary<string, int>(StringComparer.Ordinal);
        public event Action<FinalDishReadyEvent> FinalDishReady;
        public bool IsFinalDishReady { get { return states[states.Length - 1] != StepCompletionState.None; } }
        public StepProgress(string recipeId, IEnumerable<RecipeStepRuleView> orderedChain, string unknownProductItemId)
        {
            if (string.IsNullOrWhiteSpace(recipeId) || string.IsNullOrWhiteSpace(unknownProductItemId)) throw new ArgumentException("Resolved recipe and unknown product ID are required.");
            this.recipeId = recipeId; this.unknownProductItemId = unknownProductItemId;
            chain = new List<RecipeStepRuleView>(orderedChain ?? new RecipeStepRuleView[0]);
            if (chain.Count == 0 || chain.Count > 10) throw new ArgumentException("A validated step chain of one to ten entries is required.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var step in chain) if (step == null || string.IsNullOrEmpty(step.StepId) || !ids.Add(step.StepId)) throw new ArgumentException("Invalid or duplicate step ID.");
            states = new StepCompletionState[chain.Count];
        }
        public IReadOnlyList<StepProgressEntry> GetStates()
        { var result = new List<StepProgressEntry>(); for (var i = 0; i < chain.Count; i++) result.Add(new StepProgressEntry(chain[i].StepId, states[i])); return result.AsReadOnly(); }
        public void OnPrepZoneChanged(IEnumerable<ProcessingItemStack> contents)
        {
            var current = ProcessingMatcher.Counts(contents);
            var wasReady = IsFinalDishReady;
            for (var i = 0; i < chain.Count; i++)
            {
                if (states[i] == StepCompletionState.Permanent) continue;
                var permanent = false;
                foreach (var id in chain[i].PermanentTriggerItems)
                { int now, before; current.TryGetValue(id, out now); previous.TryGetValue(id, out before);
                  if (id != unknownProductItemId && now > before) { permanent = true; break; } }
                if (permanent) { states[i] = StepCompletionState.Permanent; continue; }
                var dynamic = false;
                foreach (var id in chain[i].TriggerItems) if (id != unknownProductItemId && current.ContainsKey(id)) { dynamic = true; break; }
                states[i] = dynamic ? StepCompletionState.Dynamic : StepCompletionState.None;
            }
            previous = current;
            var handler = FinalDishReady;
            if (!wasReady && IsFinalDishReady && handler != null) handler(new FinalDishReadyEvent(recipeId, chain[chain.Count - 1].StepId));
        }
    }
}
