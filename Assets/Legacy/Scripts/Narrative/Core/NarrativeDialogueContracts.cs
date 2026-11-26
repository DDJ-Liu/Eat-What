using System.Collections.Generic;

namespace EatWhat.Narrative.Core
{
    public enum NarrativeDialoguePhase { Inactive, StoryPreparing, Entering, Typing, LineComplete, Exiting, WaitingForSceneSwitch }
    public enum NarrativeDialogueStartStatus { Started, Busy, NotApplicable, Failed }

    public interface INarrativeDialogueClock { double NowSeconds { get; } }
    public interface INarrativeDialogueLock
    {
        void Acquire(string dialogueId, double nowSeconds);
        void Release(string dialogueId, double nowSeconds);
    }
    public interface INarrativeTalkedRecord { void MarkTalked(string dialogueId); }
    public interface INarrativeSceneSwitchCompletion
    { bool NotifySceneSwitchCompleted(NarrativeCommandInvocation command); }

    public sealed class NarrativeDialogueContext
    {
        internal NarrativeDialogueContext(NarrativeDialogue dialogue, DialoguePresentationSnapshot snapshot, double startedAt)
        { Dialogue = dialogue; Snapshot = snapshot; StartedAtSeconds = startedAt; }
        public NarrativeDialogue Dialogue { get; private set; }
        public DialoguePresentationSnapshot Snapshot { get; private set; }
        public double StartedAtSeconds { get; private set; }
        public IReadOnlyList<NarrativeCastEntry> Cast { get { return Dialogue.Cast; } }
    }
    public sealed class NarrativeLinePresentation
    {
        internal NarrativeLinePresentation(NarrativeDialogueContext context, NarrativeDialogueLine line,
            string nameKey, bool showNameBox, bool allPortraitsDim, double startedAt)
        {
            Context = context; Line = line; NameKey = nameKey; ShowNameBox = showNameBox;
            AllPortraitsDim = allPortraitsDim; StartedAtSeconds = startedAt;
        }
        public NarrativeDialogueContext Context { get; private set; }
        public NarrativeDialogueLine Line { get; private set; }
        public string SpeakerId { get { return Line.Speaker; } }
        public string TextKey { get { return Line.TextKey; } }
        public string NameKey { get; private set; }
        public bool ShowNameBox { get; private set; }
        public bool AllPortraitsDim { get; private set; }
        public double StartedAtSeconds { get; private set; }
    }

    // Requests only. A view reports completion later with the same context/message object.
    // Capability callbacks must not reenter the FSM or scheduler while a request is being issued.
    public interface INarrativeDialogueView
    {
        void DialogueStarted(NarrativeDialogueContext context, double nowSeconds);
        void SetLetterbox(NarrativeDialogueContext context, bool visible, double nowSeconds);
        void PrepareStoryCamera(NarrativeDialogueContext context, double nowSeconds);
        void Enter(NarrativeDialogueContext context, double nowSeconds);
        void CompleteEntrance(NarrativeDialogueContext context, double nowSeconds);
        void UpdateCast(NarrativeDialogueContext context, double nowSeconds);
        void SetSpeaker(NarrativeLinePresentation line, double nowSeconds);
        void ReactToLine(NarrativeLinePresentation line, double nowSeconds);
        void StartText(NarrativeLinePresentation line, double nowSeconds);
        void CompleteText(NarrativeLinePresentation line, double nowSeconds);
        void CanEnd(NarrativeDialogueContext context, double nowSeconds);
        void Exit(NarrativeDialogueContext context, double nowSeconds);
        void DialogueEnded(NarrativeDialogueContext context, double nowSeconds);
    }
}
