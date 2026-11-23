using System;
using System.Collections.Generic;
using EatWhat.Narrative.Core;

namespace EatWhat.Narrative.Unity
{
    public interface INarrativeDialogueComposition
    {
        void Acquire(object owner, INarrativeDialogueBoxPresentation box, INarrativeDialogueDimming dimmer,
            out NarrativeDialogueEntrance entrance, out NarrativeDialogueExit exit);
        void Release(object owner);
    }
    public enum NarrativeServiceStartStatus { Started, Unavailable, Busy, MissingDialogue, NotApplicable, UnsupportedLevel, InvalidDialogue }
    public sealed class NarrativeDialogueEvent
    {
        internal NarrativeDialogueEvent(string id, string initiator, bool aborted)
        { DialogueId = id; InitiatorId = initiator; Aborted = aborted; }
        public string DialogueId { get; private set; }
        public string InitiatorId { get; private set; }
        public bool Aborted { get; private set; }
    }
    public sealed class NarrativeDialogueLineEvent
    {
        internal NarrativeDialogueLineEvent(string dialogueId, string lineId, string speakerId)
        { DialogueId = dialogueId; LineId = lineId; SpeakerId = speakerId; }
        public string DialogueId { get; private set; }
        public string LineId { get; private set; }
        public string SpeakerId { get; private set; }
    }

    /// <summary>The service's owned runtime. Unity lifecycle/time/input adapters call this same implementation.</summary>
    public sealed class NarrativeDialogueSession : INarrativeDialogueView, INarrativeDialogueClock, IDisposable
    {
        sealed class OwnedLock : INarrativeDialogueLock
        {
            readonly INarrativeDialogueLock target; string held;
            public OwnedLock(INarrativeDialogueLock target) { this.target = target; }
            public void Acquire(string id, double now) { target.Acquire(id, now); held = id; }
            public void Release(string id, double now) { if (held != id) return; held = null; target.Release(id, now); }
            public void ReleaseHeld(double now) { if (held != null) Release(held, now); }
        }
        readonly Func<INarrativeDataDirectory> directory;
        readonly Func<NarrativeDialogue, NarrativeDialogueContinuationPolicy, DialoguePresentationResolution> resolve;
        readonly Action<string> warning;
        readonly NarrativeDialogueContinuationPolicy continuation;
        readonly NarrativeDialogueEntrance entrance;
        readonly NarrativeDialogueExit exit;
        readonly INarrativeDialogueComposition composition;
        readonly OwnedLock operationLock;
        readonly List<string> diagnostics = new List<string>();
        NarrativeDialogueStateMachine machine;
        NarrativeCommandScheduler scheduler;
        NarrativeDialogueContext current;
        NarrativeDialogueEvent pendingConversationEnd;
        string chainInitiator = "";
        int frame, startedFrame, depth, diagnosticsRead;
        bool abortRequested, disposeRequested, disposed;
        public NarrativeAdvanceInputModel Input { get; private set; }
        public NarrativeTalkedPersistence Talked { get; private set; }
        public bool IsRunning { get { return current != null; } }
        public string DialogueId { get { return current == null ? null : current.Dialogue.DialogueId; } }
        public string InitiatorId { get { return current == null ? null : chainInitiator; } }
        public DialoguePresentationSnapshot CurrentSnapshot { get { return current == null ? null : current.Snapshot; } }
        public string CurrentSpeakerId
        { get { return current == null || machine == null || machine.CurrentLine == null || !ReferenceEquals(machine.CurrentLine.Context, current) ? null : machine.CurrentLine.SpeakerId; } }
        public NarrativeDialoguePhase Phase { get { return machine == null ? NarrativeDialoguePhase.Inactive : machine.Phase; } }
        public double NowSeconds { get; private set; }
        public IReadOnlyList<string> Diagnostics { get { return diagnostics.AsReadOnly(); } }
        public event Action<NarrativeDialogueEvent> DialogueStarted;
        public event Action<NarrativeDialogueEvent> DialogueEnded;
        public event Action<NarrativeDialogueLineEvent> LineStarted;
        public event Action<NarrativeDialogueEvent> ConversationEnded;

        public NarrativeDialogueSession(Func<INarrativeDataDirectory> directory,
            Func<NarrativeDialogue, NarrativeDialogueContinuationPolicy, DialoguePresentationResolution> resolve,
            INarrativeDialogueBoxPresentation box, INarrativeDialogueDimming dimmer,
            INarrativeDialogueLock operationLock, NarrativeTalkedPersistence talked, Action<string> warning = null,
            INarrativeDialogueComposition composition = null, Func<string,object> portraitSprites = null)
        {
            this.directory = directory ?? throw new ArgumentNullException(nameof(directory));
            this.resolve = resolve ?? throw new ArgumentNullException(nameof(resolve));
            this.operationLock = new OwnedLock(operationLock ?? throw new ArgumentNullException(nameof(operationLock)));
            Talked = talked ?? throw new ArgumentNullException(nameof(talked));
            this.warning = warning ?? (message => UnityEngine.Debug.LogWarning(message));
            Input = new NarrativeAdvanceInputModel(); continuation = new NarrativeDialogueContinuationPolicy(Warn);
            this.composition = composition;
            if (composition == null) { entrance = new NarrativeDialogueEntrance(box, dimmer); exit = new NarrativeDialogueExit(entrance); }
            else composition.Acquire(this, box, dimmer, out entrance, out exit);
            var portraitComposition = composition as INarrativeDialoguePortraitComposition;
            entrance.ConfigurePortraits(portraitComposition == null ? null : portraitComposition.PortraitStage,
                id => { var data = directory(); NarrativeCharacter character; string issue;
                    return data != null && data.TryGetCharacter(id, out character, out issue) ? character : null; }, portraitSprites);
            entrance.EntranceCompleted += OnEntrance; entrance.TextCompleted += OnText; exit.ExitCompleted += OnExit;
            scheduler = new NarrativeCommandScheduler(null); // No external command/scene-switch handlers in this version.
        }
        public bool InitializeWhenAvailable()
        { return !disposed && !disposeRequested && directory() != null && Talked.InitializeWhenAvailable(true); }

        public NarrativeServiceStartStatus TryStart(string id, string initiator, int frameId, out string diagnostic)
        {
            frame = frameId;
            if (disposed || disposeRequested) return Reject(NarrativeServiceStartStatus.Unavailable, "SERVICE_UNAVAILABLE", out diagnostic);
            var data = directory();
            if (data == null) return Reject(NarrativeServiceStartStatus.Unavailable, "RUNTIME_UNAVAILABLE", out diagnostic);
            if (IsRunning || depth != 0) return Reject(NarrativeServiceStartStatus.Busy, "DIALOGUE_BUSY", out diagnostic);
            Talked.InitializeWhenAvailable(true);
            NarrativeDialogue model; string issue;
            if (!data.TryGetDialogue(id, out model, out issue)) return Reject(NarrativeServiceStartStatus.MissingDialogue, "DIALOGUE_MISSING: " + issue, out diagnostic);
            if (model.Level == NarrativeDialogueLevel.Ambient) return Reject(NarrativeServiceStartStatus.NotApplicable, "AMBIENT_NOT_APPLICABLE", out diagnostic);
            if (model.Level != NarrativeDialogueLevel.Normal) return Reject(NarrativeServiceStartStatus.UnsupportedLevel, "STORY_NOT_SUPPORTED", out diagnostic);
            chainInitiator = initiator ?? "";
            {
                diagnosticsRead = 0;
                machine = new NarrativeDialogueStateMachine(data, Resolve, this, operationLock, this, Talked, scheduler, Input);
            }
            NarrativeDialogueStartStatus result = NarrativeDialogueStartStatus.Failed;
            Call(() => result = machine.Start(id));
            if (result != NarrativeDialogueStartStatus.Started) return Reject(NarrativeServiceStartStatus.InvalidDialogue, "DIALOGUE_INVALID", out diagnostic);
            diagnostic = null; return NarrativeServiceStartStatus.Started;
        }
        DialoguePresentationResolution Resolve(NarrativeDialogue model)
        {
            if (abortRequested || model.Level != NarrativeDialogueLevel.Normal)
            {
                Warn("CONTINUATION_UNAVAILABLE: only normal dialogue is supported.");
                return DialoguePresentationResolver.Resolve(null, model);
            }
            return resolve(model, continuation);
        }
        NarrativeServiceStartStatus Reject(NarrativeServiceStartStatus status, string reason, out string diagnostic)
        { diagnostic = reason; Warn(reason); return status; }
        public void RequestAdvance(int frameId)
        {
            if (disposed || disposeRequested || !IsRunning || frameId == startedFrame || depth != 0) return;
            Call(() => machine.Click());
        }
        public void Advance(double seconds, int frameId)
        {
            if (disposed) throw new ObjectDisposedException(nameof(NarrativeDialogueSession));
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0 || double.IsInfinity(NowSeconds + seconds)) throw new ArgumentOutOfRangeException(nameof(seconds));
            frame = frameId; NowSeconds += seconds;
            bool available = InitializeWhenAvailable();
            if (!available && IsRunning) Abort();
            Call(() =>
            {
                scheduler.Tick(NowSeconds);
                if (IsRunning && frame != startedFrame) machine.Tick();
                exit.Advance(seconds); // The only view time entry. Input is forwarded by the existing BoxView.
            });
        }
        void Call(Action action)
        {
            depth++;
            try { action(); }
            finally
            {
                depth--;
                if (depth == 0)
                {
                    CollectDiagnostics();
                    if (abortRequested) AbortNow();
                    if (disposeRequested) DisposeNow();
                    PublishConversationEnd();
                }
            }
        }
        void CollectDiagnostics()
        { if (machine != null) while (diagnosticsRead < machine.Diagnostics.Count) Warn(machine.Diagnostics[diagnosticsRead++]); }
        void Warn(string message) { diagnostics.Add(message); warning(message); }
        bool Current(NarrativeDialogueContext context) { return !abortRequested && ReferenceEquals(current, context); }
        void OnEntrance(NarrativeDialogueContext context) { if (Current(context)) machine.NotifyEntranceCompleted(context); }
        void OnText(NarrativeLinePresentation line) { if (Current(line.Context)) machine.NotifyLineCompleted(line); }
        void OnExit(NarrativeDialogueContext context) { if (Current(context)) machine.NotifyExitCompleted(context); }

        void INarrativeDialogueView.DialogueStarted(NarrativeDialogueContext context, double now)
        {
            pendingConversationEnd = null;
            current = context; startedFrame = frame; entrance.DialogueStarted(context, now);
            DialogueStarted?.Invoke(new NarrativeDialogueEvent(DialogueId, chainInitiator, false));
        }
        void INarrativeDialogueView.DialogueEnded(NarrativeDialogueContext context, double now)
        {
            if (!ReferenceEquals(current, context)) return;
            current = null;
            pendingConversationEnd = new NarrativeDialogueEvent(context.Dialogue.DialogueId, chainInitiator, false);
            DialogueEnded?.Invoke(pendingConversationEnd);
            // No Clear here: AfterBoxClosed can leave undim running while the FSM starts its next segment.
        }
        void INarrativeDialogueView.Enter(NarrativeDialogueContext c, double t) { if (Current(c)) entrance.Enter(c, t); }
        void INarrativeDialogueView.CompleteEntrance(NarrativeDialogueContext c, double t) { if (Current(c)) entrance.CompleteEntrance(c, t); }
        void INarrativeDialogueView.StartText(NarrativeLinePresentation l, double t)
        {
            if (!Current(l.Context)) return;
            bool first = !ReferenceEquals(entrance.CurrentLine, l);
            entrance.StartText(l, t);
            if (first && Current(l.Context) && ReferenceEquals(entrance.CurrentLine, l))
                LineStarted?.Invoke(new NarrativeDialogueLineEvent(l.Context.Dialogue.DialogueId, l.Line.LineId, l.SpeakerId));
        }
        void INarrativeDialogueView.CompleteText(NarrativeLinePresentation l, double t) { if (Current(l.Context)) entrance.CompleteText(l, t); }
        void INarrativeDialogueView.Exit(NarrativeDialogueContext c, double t) { if (Current(c)) exit.Exit(c, t); }
        void INarrativeDialogueView.UpdateCast(NarrativeDialogueContext c, double t) { entrance.UpdateCast(c, t); }
        void INarrativeDialogueView.SetSpeaker(NarrativeLinePresentation l, double t) { entrance.SetSpeaker(l, t); }
        void INarrativeDialogueView.ReactToLine(NarrativeLinePresentation l, double t) { entrance.ReactToLine(l, t); }
        void INarrativeDialogueView.CanEnd(NarrativeDialogueContext c, double t) { entrance.CanEnd(c, t); }
        void INarrativeDialogueView.SetLetterbox(NarrativeDialogueContext c, bool v, double t) { entrance.SetLetterbox(c, v, t); }
        void INarrativeDialogueView.PrepareStoryCamera(NarrativeDialogueContext c, double t) { entrance.PrepareStoryCamera(c, t); }
        public void Abort()
        { if (disposed) return; abortRequested = true; if (depth == 0) AbortNow(); }
        void AbortNow()
        {
            var previous = current; current = null; abortRequested = false;
            // Core has no Abort API. Discard it permanently, never report a normal completion to it.
            machine = null; scheduler = new NarrativeCommandScheduler(null); diagnosticsRead = 0;
            exit.Clear(); operationLock.ReleaseHeld(NowSeconds);
            if (previous != null)
            {
                pendingConversationEnd = new NarrativeDialogueEvent(previous.Dialogue.DialogueId, chainInitiator, true);
                DialogueEnded?.Invoke(pendingConversationEnd);
            }
            PublishConversationEnd();
        }
        void PublishConversationEnd()
        {
            // The unchanged FSM starts a follow-up after DialogueEnded returns. Wait for that stack to settle.
            if (pendingConversationEnd == null || current != null || (machine != null && machine.Phase != NarrativeDialoguePhase.Inactive)) return;
            var ended = pendingConversationEnd; pendingConversationEnd = null;
            ConversationEnded?.Invoke(ended);
        }
        public void Dispose()
        { if (disposed) return; disposeRequested = true; Abort(); if (depth == 0) DisposeNow(); }
        void DisposeNow()
        {
            if (disposed) return;
            entrance.EntranceCompleted -= OnEntrance; entrance.TextCompleted -= OnText; exit.ExitCompleted -= OnExit;
            entrance.Dispose(); composition?.Release(this); disposed = true;
        }
    }
}
