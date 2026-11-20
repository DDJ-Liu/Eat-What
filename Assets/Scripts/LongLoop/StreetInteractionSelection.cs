using System;
using System.Collections.Generic;
using EatWhat.Narrative.Core;
using UnityEngine;

namespace EatWhat.LongLoop
{
    public interface IStreetInteractable
    {
        string InteractionId { get; }
        Transform InteractionFoot { get; }
        Transform PromptAnchor { get; }
        double InteractRadius { get; }
        string PromptTextKey { get; }
        bool IsAvailable { get; }
        bool Interact();
    }
    public interface IStreetInteractionPrompt
    { bool Show(Transform anchor, string localizationKey); void Hide(); }

    /// <summary>One selector's actual target state; geometry and nearest/tie rules use the existing implementations.</summary>
    public sealed class StreetInteractionSelection
    {
        readonly Func<bool> blocked, pressed;
        readonly Func<Transform> player;
        readonly Func<IEnumerable<IStreetInteractable>> targets;
        readonly IStreetInteractionPrompt prompt;
        readonly Func<string> promptKey;
        readonly Action<string> warning;
        readonly HashSet<string> diagnostics = new HashSet<string>(StringComparer.Ordinal);
        int lastFrame = int.MinValue;
        string previousId, shownKey;
        Transform shownAnchor;
        public IStreetInteractable Selected { get; private set; }
        public StreetInteractionSelection(Func<bool> blocked, Func<Transform> player, Func<IEnumerable<IStreetInteractable>> targets,
            Func<bool> pressed, IStreetInteractionPrompt prompt, Func<string> promptKey, Action<string> warning = null)
        {
            this.blocked = blocked ?? throw new ArgumentNullException(nameof(blocked));
            this.player = player ?? throw new ArgumentNullException(nameof(player));
            this.targets = targets ?? throw new ArgumentNullException(nameof(targets));
            this.pressed = pressed ?? throw new ArgumentNullException(nameof(pressed));
            this.prompt = prompt ?? throw new ArgumentNullException(nameof(prompt));
            this.promptKey = promptKey ?? throw new ArgumentNullException(nameof(promptKey));
            this.warning = warning ?? (message => Debug.LogWarning(message)); prompt.Hide();
        }
        public void Advance(int frameId)
        {
            if (frameId == lastFrame) return; lastFrame = frameId;
            if (blocked()) { Clear(); return; }
            var foot = player();
            if (foot == null || !foot.gameObject.activeInHierarchy) { Clear(); return; }
            var candidates = new List<NarrativeInteractionCandidate>();
            var owners = new Dictionary<NarrativeInteractionCandidate, IStreetInteractable>();
            foreach (var target in targets())
            {
                if (target == null || !target.IsAvailable || target.InteractionFoot == null ||
                    !target.InteractionFoot.gameObject.activeInHierarchy || string.IsNullOrEmpty(target.InteractionId)) continue;
                try
                {
                    var candidate = new NarrativeInteractionCandidate(target.InteractionId,
                        GroundSpace.GroundDistance(foot.position, target.InteractionFoot.position), target.InteractRadius, true);
                    candidates.Add(candidate); owners.Add(candidate, target);
                }
                catch (InvalidOperationException error) { Warn("INTERACTION_GROUND: " + error.Message); }
                catch (ArgumentException error) { Warn("INTERACTION_CANDIDATE: " + error.Message); }
            }
            var chosen = NarrativeInteractionRules.SelectNearest(candidates, previousId);
            var next = chosen == null ? null : owners[chosen];
            if (next == null) { Clear(); return; }
            bool changed = !ReferenceEquals(Selected, next); Selected = next; previousId = chosen.CandidateId;
            var anchor = next.PromptAnchor;
            string key = string.IsNullOrEmpty(next.PromptTextKey) ? promptKey() : next.PromptTextKey;
            if (anchor == null || !anchor.gameObject.activeInHierarchy)
            { prompt.Hide(); shownAnchor = null; shownKey = null; }
            else if (changed || anchor != shownAnchor || key != shownKey)
            {
                if (prompt.Show(anchor, key)) { shownAnchor = anchor; shownKey = key; }
                else { prompt.Hide(); shownAnchor = null; shownKey = null; Warn("INTERACTION_PROMPT_UNAVAILABLE"); }
            }
            if (pressed() && next.Interact()) Clear(); // Exactly one existing one-frame press read per selected frame.
        }
        public void Clear()
        {
            if (Selected != null || shownAnchor != null) prompt.Hide();
            Selected = null; shownAnchor = null; shownKey = null;
        }
        void Warn(string message) { if (diagnostics.Add(message)) warning(message); }
    }
}
