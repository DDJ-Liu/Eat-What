using System;
using System.Collections.Generic;
using EatWhat.Narrative.Core;

namespace EatWhat.Narrative.Unity
{
    /// <summary>The entrance/line half of presentation. The service forwards events to the FSM; exit belongs to V014-08.</summary>
    public sealed class NarrativeDialogueEntrance : IDisposable
    {
        enum Completion { Entrance, Text, Unsupported }
        sealed class Pending
        {
            public Completion Kind; public string Request;
            public NarrativeDialogueContext Context; public NarrativeLinePresentation Line;
        }
        readonly INarrativeDialogueBoxPresentation box;
        readonly INarrativeDialogueDimming dimmer;
        readonly Func<string, string> localize;
        readonly object dimOwner = new object();
        readonly NarrativeStepSequence entrance = new NarrativeStepSequence();
        List<Pending> pending = new List<Pending>();
        bool entering, entranceQueued, entrancePublished, textQueued, textPublished, disposed;
        public NarrativeDialogueContext CurrentContext { get; private set; }
        public NarrativeLinePresentation CurrentLine { get; private set; }
        public object DimmingOwnerToken { get { return dimOwner; } }
        internal INarrativeDialogueBoxPresentation Box { get { return box; } }
        internal INarrativeDialogueDimming Dimmer { get { return dimmer; } }
        public IReadOnlyList<NarrativeStepSequence.StepState> EntranceSteps { get { return entrance.Steps; } }
        public event Action<NarrativeDialogueContext> EntranceCompleted;
        public event Action<NarrativeLinePresentation> TextCompleted;
        // Fire-and-forget FSM requests have no completion method in Core; this is a deferred presentation acknowledgement only.
        public event Action<string, NarrativeDialogueContext, NarrativeLinePresentation> UnsupportedCompleted;

        public NarrativeDialogueEntrance(INarrativeDialogueBoxPresentation box, INarrativeDialogueDimming dimmer,
            Func<string, string> localize = null)
        {
            this.box = box ?? throw new ArgumentNullException(nameof(box));
            this.dimmer = dimmer ?? throw new ArgumentNullException(nameof(dimmer));
            this.localize = localize ?? new Func<string, string>(LocService.Get);
            box.SetExternalTime(true); dimmer.SetExternalTime(true); box.TextCompleted += OnTextCompleted;
        }
        public void DialogueStarted(NarrativeDialogueContext context, double nowSeconds)
        {
            CheckLive(); if (context == null) throw new ArgumentNullException(nameof(context));
            CurrentContext = context; CurrentLine = null; entering = entranceQueued = entrancePublished = false;
            textQueued = textPublished = false; pending.Clear();
        }
        public void Enter(NarrativeDialogueContext context, double nowSeconds)
        {
            CheckLive(); if (!Current(context)) return;
            var snapshot = context.Snapshot;
            box.Show(snapshot); dimmer.RequestNormal(dimOwner, snapshot, snapshot.Get<float>("entrance.dimTime"));
            entering = true; entranceQueued = entrancePublished = false;
            var mode = snapshot.Get<NarrativeCommandExecutionMode>("entrance.order") == NarrativeCommandExecutionMode.Parallel
                ? NarrativeSequenceMode.Parallel : NarrativeSequenceMode.Sequential;
            bool complete = entrance.Begin(new[] { new NarrativeSequenceStep("dim", snapshot.Get<float>("entrance.dimTime")),
                new NarrativeSequenceStep("portrait", 0), new NarrativeSequenceStep("box", snapshot.Get<float>("entrance.boxTime")) },
                mode, new[] { "portrait" }); // No portrait/letterbox parameters or dummy waiting in this version.
            ApplyBox(); if (complete) QueueEntrance();
        }
        public void CompleteEntrance(NarrativeDialogueContext context, double nowSeconds)
        {
            CheckLive(); if (!Current(context) || !entering) return;
            entrance.Skip(); dimmer.RequestNormal(dimOwner, context.Snapshot, 0); ApplyBox(); QueueEntrance();
        }
        public void StartText(NarrativeLinePresentation line, double nowSeconds)
        {
            CheckLive(); if (line == null || !Current(line.Context) || ReferenceEquals(CurrentLine, line)) return;
            CurrentLine = line; entering = false; entrancePublished = true;
            textQueued = textPublished = false;
            // Existing Box Presenter updates name and starts the real Typewriter in this one call.
            box.BeginLine(line, localize(line.TextKey));
        }
        public void CompleteText(NarrativeLinePresentation line, double nowSeconds)
        { CheckLive(); if (line != null && ReferenceEquals(CurrentLine, line) && Current(line.Context)) box.CompleteLine(); }

        internal void SuspendForExit(NarrativeDialogueContext context)
        { CheckLive(); if (!Current(context)) return; entering = false; CurrentLine = null; pending.Clear(); }

        public void Advance(double seconds)
        {
            CheckLive(); if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            dimmer.Advance(seconds);
            if (entering && !entrance.IsComplete) { bool complete = entrance.Advance(seconds); ApplyBox(); if (complete) QueueEntrance(); }
            box.Advance(seconds);
            // Every callback occurs after its request stack returned. Requests made by callbacks wait for the next Advance.
            var ready = pending; pending = new List<Pending>();
            foreach (var item in ready)
            {
                if (!Current(item.Context)) continue;
                if (item.Kind == Completion.Entrance)
                {
                    // Core's skip path itself begins the line after CompleteEntrance; suppress that now-stale report.
                    if (entrancePublished || CurrentLine != null) continue;
                    entrancePublished = true; entering = false; EntranceCompleted?.Invoke(item.Context);
                }
                else if (item.Kind == Completion.Text)
                {
                    if (!ReferenceEquals(CurrentLine, item.Line) || textPublished) continue;
                    textPublished = true; TextCompleted?.Invoke(item.Line);
                }
                else if (item.Line == null || ReferenceEquals(CurrentLine, item.Line))
                    UnsupportedCompleted?.Invoke(item.Request, item.Context, item.Line);
            }
        }
        void ApplyBox()
        {
            foreach (var step in entrance.Steps)
                if (step.Name == "box" && step.HasStarted) box.SetEntranceProgress(CurrentContext.Snapshot, (float)step.Progress);
        }
        void QueueEntrance()
        {
            if (entranceQueued || entrancePublished) return; entranceQueued = true;
            pending.Add(new Pending { Kind = Completion.Entrance, Context = CurrentContext });
        }
        void OnTextCompleted(NarrativeLinePresentation line)
        {
            if (disposed || line == null || !Current(line.Context) || !ReferenceEquals(CurrentLine, line) || textQueued || textPublished) return;
            textQueued = true; pending.Add(new Pending { Kind = Completion.Text, Context = line.Context, Line = line });
        }
        void Unsupported(string request, NarrativeDialogueContext context, NarrativeLinePresentation line = null)
        { CheckLive(); if (Current(context)) pending.Add(new Pending { Kind = Completion.Unsupported, Request = request, Context = context, Line = line }); }
        public void SetSpeaker(NarrativeLinePresentation line, double nowSeconds) { if (line != null) Unsupported("speaker", line.Context, line); }
        public void ReactToLine(NarrativeLinePresentation line, double nowSeconds) { if (line != null) Unsupported("reaction", line.Context, line); }
        public void UpdateCast(NarrativeDialogueContext context, double nowSeconds) { Unsupported("cast", context); }
        public void CanEnd(NarrativeDialogueContext context, double nowSeconds) { Unsupported("can-end", context); }
        public void SetLetterbox(NarrativeDialogueContext context, bool visible, double nowSeconds) { Unsupported(visible ? "letterbox-show" : "letterbox-hide", context); }
        public void PrepareStoryCamera(NarrativeDialogueContext context, double nowSeconds) { Unsupported("story-camera", context); }
        bool Current(NarrativeDialogueContext context) { return context != null && ReferenceEquals(CurrentContext, context); }
        void CheckLive() { if (disposed) throw new ObjectDisposedException(nameof(NarrativeDialogueEntrance)); }
        public void Clear()
        {
            CheckLive(); CurrentContext = null; CurrentLine = null; entering = false; pending.Clear();
            box.Hide(); dimmer.Withdraw(dimOwner, 0);
        }
        public void Dispose()
        {
            if (disposed) return; Clear(); box.TextCompleted -= OnTextCompleted;
            box.SetExternalTime(false); dimmer.SetExternalTime(false); disposed = true;
        }
    }
}
