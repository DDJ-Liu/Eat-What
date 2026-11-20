using System;
using EatWhat.Narrative.Core;

namespace EatWhat.Narrative.Unity
{
    public interface INarrativeDialogueBoxSurface
    {
        void LayoutFullLine(string finalText, string finalName, bool showName);
        void SetVisiblePrefix(int utf16End);
        void SetToggleStates(bool auto, bool fastForward);
    }

    public struct NarrativeDialogueBoxPose
    {
        public float OffsetY, Scale, Alpha;
    }

    /// <summary>Presentation adapter. Typing and input state remain in their existing Core owners.</summary>
    public sealed class NarrativeDialogueBoxPresenter
    {
        readonly INarrativeDialogueBoxSurface surface;
        readonly NarrativeAdvanceInputModel input;
        readonly NarrativeTypewriterModel typewriter = new NarrativeTypewriterModel();
        NarrativeLinePresentation line, pendingCompletion;
        public event Action AdvanceRequested;
        public event Action<NarrativeLinePresentation> TextCompleted;
        public int VisibleTextLength { get { return typewriter.VisibleTextLength; } }
        public int VisibleCharacterCount { get { return typewriter.VisibleCharacterCount; } }
        public bool IsComplete { get { return typewriter.IsComplete; } }
        public bool Active { get; set; }

        public NarrativeDialogueBoxPresenter(INarrativeDialogueBoxSurface surface, NarrativeAdvanceInputModel input)
        {
            this.surface = surface ?? throw new ArgumentNullException(nameof(surface));
            this.input = input ?? throw new ArgumentNullException(nameof(input));
            RefreshToggles();
        }

        public void PrepareEntrance(DialoguePresentationSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            line = pendingCompletion = null; typewriter.Begin("", snapshot); Active = true;
            surface.LayoutFullLine("", "", false); surface.SetVisiblePrefix(0); RefreshToggles();
        }

        public void BeginLine(NarrativeLinePresentation value, string finalLocalizedText)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            line = value; pendingCompletion = null; Active = true;
            string name = value.ShowNameBox && value.NameKey.Length != 0 ? LocService.Get(value.NameKey) : "";
            // The FSM has already selected override versus character name. Do not resolve that rule again.
            surface.LayoutFullLine(finalLocalizedText ?? "", name, name.Length != 0);
            if (typewriter.Begin(finalLocalizedText, value.Context.Snapshot)) pendingCompletion = value;
            surface.SetVisiblePrefix(typewriter.VisibleTextLength); RefreshToggles();
        }

        public void Tick(double seconds)
        {
            if (!Active) return;
            // Only a completion queued before this Tick can be reported in this Tick.
            var completed = pendingCompletion; pendingCompletion = null;
            if (line != null)
            {
                if (typewriter.Advance(seconds, input.FastForwardEnabled)) pendingCompletion = line;
                surface.SetVisiblePrefix(typewriter.VisibleTextLength);
            }
            RefreshToggles();
            if (completed != null && ReferenceEquals(line, completed)) TextCompleted?.Invoke(completed);
        }

        // UI Update owns one input sample even when a service owns time advancement.
        public void ProcessFrame(double seconds, bool advanceTime, bool interactPressed)
        {
            if (!Active) return;
            if (advanceTime) Tick(seconds);
            if (Active && interactPressed) InteractPressed();
        }

        public void CompleteLine()
        {
            if (line == null || !Active) return;
            if (typewriter.Complete()) pendingCompletion = line;
            surface.SetVisiblePrefix(typewriter.VisibleTextLength);
        }
        public void Hide() { Active = false; line = pendingCompletion = null; }
        public void ScreenClicked() { if (Active) AdvanceRequested?.Invoke(); }
        public void InteractPressed() { if (Active) AdvanceRequested?.Invoke(); }
        public void AutoClicked() { if (Active) { input.ToggleAuto(); RefreshToggles(); } }
        public void FastForwardClicked() { if (Active) { input.ToggleFastForward(); RefreshToggles(); } }
        void RefreshToggles() { surface.SetToggleStates(input.AutoEnabled, input.FastForwardEnabled); }

        public static NarrativeDialogueBoxPose EntrancePose(DialoguePresentationSnapshot snapshot, float progress)
        {
            var pose = Pose(snapshot, progress);
            if (snapshot.Get<DialogueBoxMode>("entrance.boxMode") == DialogueBoxMode.ScalePop)
            {
                float p = Math.Max(0, Math.Min(1, progress)), t = p - 1;
                // Fixed small back overshoot in the view only; Core's four curve presets stay unchanged.
                float back = 1 + 2.70158f * t * t * t + 1.70158f * t * t;
                pose.Scale = .82f + .18f * back;
            }
            return pose;
        }

        public static NarrativeDialogueBoxPose Pose(DialoguePresentationSnapshot snapshot, float progress)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (float.IsNaN(progress) || float.IsInfinity(progress)) throw new ArgumentOutOfRangeException(nameof(progress));
            float p = Math.Max(0, Math.Min(1, progress));
            bool slide = snapshot.Get<DialogueBoxMode>("entrance.boxMode") == DialogueBoxMode.SlideUp;
            return new NarrativeDialogueBoxPose { Alpha = p, Scale = slide ? 1 : .82f + .18f * p,
                OffsetY = slide ? -snapshot.Get<float>("entrance.boxSlideDistance") * (1 - p) : 0 };
        }
    }
}
