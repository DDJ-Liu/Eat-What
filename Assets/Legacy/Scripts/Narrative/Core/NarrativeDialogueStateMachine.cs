using System;
using System.Collections.Generic;

namespace EatWhat.Narrative.Core
{
    // Owns dialogue progress only. The caller continues ticking the scheduler, including after Inactive.
    public sealed class NarrativeDialogueStateMachine : INarrativeSceneSwitchCompletion
    {
        readonly INarrativeDataDirectory directory;
        readonly Func<NarrativeDialogue, DialoguePresentationResolution> resolve;
        readonly INarrativeDialogueView view;
        readonly INarrativeDialogueLock operationLock;
        readonly INarrativeDialogueClock clock;
        readonly INarrativeTalkedRecord talked;
        readonly NarrativeCommandScheduler scheduler;
        readonly NarrativeAdvanceInputModel input;
        readonly List<string> diagnostics = new List<string>();
        readonly List<NarrativeCommandInvocation> pendingSwitches = new List<NarrativeCommandInvocation>();
        IReadOnlyList<NarrativeDialogueLine> lines;
        int lineIndex;
        bool barsReady, cameraReady, locked, finished, switchObserved, advanceAfterCompletion;
        NarrativeCommandBatch onEndBatch;

        public NarrativeDialogueStateMachine(INarrativeDataDirectory directory,
            Func<NarrativeDialogue, DialoguePresentationResolution> resolve,
            INarrativeDialogueView view, INarrativeDialogueLock operationLock, INarrativeDialogueClock clock,
            INarrativeTalkedRecord talked, NarrativeCommandScheduler scheduler, NarrativeAdvanceInputModel input)
        {
            this.directory = directory ?? throw new ArgumentNullException(nameof(directory));
            this.resolve = resolve ?? throw new ArgumentNullException(nameof(resolve));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.operationLock = operationLock ?? throw new ArgumentNullException(nameof(operationLock));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            this.talked = talked ?? throw new ArgumentNullException(nameof(talked));
            this.scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            this.input = input ?? throw new ArgumentNullException(nameof(input));
            Diagnostics = diagnostics.AsReadOnly(); PendingSceneSwitches = pendingSwitches.AsReadOnly();
        }
        public NarrativeDialoguePhase Phase { get; private set; }
        public NarrativeDialogueContext Current { get; private set; }
        public NarrativeLinePresentation CurrentLine { get; private set; }
        public NarrativeCommandBatch CurrentLineBatch { get; private set; }
        public IReadOnlyList<string> Diagnostics { get; private set; }
        public IReadOnlyList<NarrativeCommandInvocation> PendingSceneSwitches { get; private set; }
        public bool CanEnd { get { return Phase == NarrativeDialoguePhase.LineComplete && lineIndex == lines.Count - 1; } }

        public NarrativeDialogueStartStatus Start(string dialogueId)
        {
            if (Phase != NarrativeDialoguePhase.Inactive || pendingSwitches.Count != 0) return NarrativeDialogueStartStatus.Busy;
            return Begin(dialogueId, false, Now());
        }

        NarrativeDialogueStartStatus Begin(string id, bool seamless, double now)
        {
            NarrativeDialogue model; string diagnostic;
            if (!directory.TryGetDialogue(id, out model, out diagnostic)) { diagnostics.Add(diagnostic); Idle(); return NarrativeDialogueStartStatus.Failed; }
            if (model.Level == NarrativeDialogueLevel.Ambient)
            { diagnostics.Add("Ambient dialogue is not applicable to the dialogue FSM: " + id); Idle(); return NarrativeDialogueStartStatus.NotApplicable; }
            NarrativeLineChain chain;
            bool chainValid = directory.TryGetLineChain(id, out chain, out diagnostic);
            foreach (var issue in chain.Diagnostics) diagnostics.Add(issue.Reason);
            if (!chainValid) { Idle(); return NarrativeDialogueStartStatus.Failed; }
            var resolution = resolve(model);
            foreach (var issue in resolution.ConfigurationDiagnostics) diagnostics.Add(issue.ToString());
            foreach (var issue in resolution.OverrideDiagnostics) diagnostics.Add(issue.ToString());
            if (!resolution.Success) { Idle(); return NarrativeDialogueStartStatus.Failed; }
            Current = new NarrativeDialogueContext(model, resolution.Snapshot, now); lines = chain.Lines;
            lineIndex = -1; CurrentLine = null; CurrentLineBatch = null; onEndBatch = null;
            barsReady = cameraReady = finished = switchObserved = advanceAfterCompletion = false;
            var snapshot = Current.Snapshot;
            input.ConfigureSettings(new NarrativeAdvanceSettings(snapshot.Get<NarrativeTypingClickAction>("text.clickWhileTyping"),
                snapshot.Get<NarrativeTypingClickAction>("text.clickWhileTypingFast"), snapshot.Get<float>("text.autoWait"),
                snapshot.Get<float>("text.advanceCooldown"), snapshot.Get<float>("cg.cgAdvanceCooldown")));
            Phase = seamless ? NarrativeDialoguePhase.Entering : model.Level == NarrativeDialogueLevel.Story ? NarrativeDialoguePhase.StoryPreparing : NarrativeDialoguePhase.Entering;
            operationLock.Acquire(model.DialogueId, now); locked = true; view.DialogueStarted(Current, now);
            if (seamless) { view.UpdateCast(Current, now); BeginLine(0, now); }
            else if (model.Level == NarrativeDialogueLevel.Story)
            { view.SetLetterbox(Current, true, now); }
            else view.Enter(Current, now);
            return NarrativeDialogueStartStatus.Started;
        }

        public bool NotifyStoryBarsReady(NarrativeDialogueContext context)
        {
            if (!ReferenceEquals(Current, context) || Phase != NarrativeDialoguePhase.StoryPreparing || barsReady) return false;
            barsReady = true; view.PrepareStoryCamera(Current, Now()); return true;
        }
        public bool NotifyStoryCameraReady(NarrativeDialogueContext context)
        {
            if (!ReferenceEquals(Current, context) || Phase != NarrativeDialoguePhase.StoryPreparing || !barsReady || cameraReady) return false;
            cameraReady = true; TryEnterStory(Now()); return true;
        }
        void TryEnterStory(double now)
        { if (barsReady && cameraReady) { Phase = NarrativeDialoguePhase.Entering; view.Enter(Current, now); } }
        public bool NotifyEntranceCompleted(NarrativeDialogueContext context)
        {
            if (!ReferenceEquals(Current, context) || Phase != NarrativeDialoguePhase.Entering) return false;
            BeginLine(0, Now()); return true;
        }
        public bool NotifyLineCompleted(NarrativeLinePresentation line)
        {
            if (!ReferenceEquals(CurrentLine, line) || Phase != NarrativeDialoguePhase.Typing) return false;
            double now = Now(); MarkLineComplete(now);
            if (advanceAfterCompletion) { advanceAfterCompletion = false; RequestEnd(now); }
            return true;
        }
        void MarkLineComplete(double now)
        {
            Phase = NarrativeDialoguePhase.LineComplete; input.NotifyLineCompleted(now);
            if (lineIndex == lines.Count - 1)
            { view.CanEnd(Current, now); if (Current.Dialogue.Level == NarrativeDialogueLevel.Story) view.SetLetterbox(Current, false, now); }
        }

        public NarrativeAdvanceIntent Click()
        { double now = Now(); var intent = input.TryClick(InputPhase(), now); Apply(intent, now); return intent; }

        // No scheduler.Tick here: detached work and its lifetime remain with the external scheduler driver.
        public void Tick()
        {
            double now = Now(); DrainSwitches();
            if (Phase == NarrativeDialoguePhase.WaitingForSceneSwitch) { ContinueAfterSwitch(now); return; }
            if (!finished && pendingSwitches.Count != 0 && Current != null) { BeginExit(now); return; }
            Apply(input.EvaluateAuto(InputPhase(), now), now);
        }
        NarrativeAdvancePhase InputPhase()
        {
            if (Phase == NarrativeDialoguePhase.Entering) return NarrativeAdvancePhase.Entering;
            if (Phase == NarrativeDialoguePhase.Typing) return NarrativeAdvancePhase.Typing;
            if (Phase == NarrativeDialoguePhase.LineComplete) return NarrativeAdvancePhase.LineComplete;
            return NarrativeAdvancePhase.Inactive;
        }
        void Apply(NarrativeAdvanceIntent intent, double now)
        {
            if (intent == NarrativeAdvanceIntent.SkipEntrance && Phase == NarrativeDialoguePhase.Entering)
            { view.CompleteEntrance(Current, now); BeginLine(0, now); }
            else if (intent == NarrativeAdvanceIntent.CompleteLine && Phase == NarrativeDialoguePhase.Typing) view.CompleteText(CurrentLine, now);
            else if (intent == NarrativeAdvanceIntent.SkipToNext && (Phase == NarrativeDialoguePhase.Typing || Phase == NarrativeDialoguePhase.LineComplete))
            {
                if (lineIndex < lines.Count - 1)
                {
                    scheduler.TruncateCurrentLine(now); DrainSwitches();
                    if (pendingSwitches.Count != 0) BeginExit(now); else BeginLine(lineIndex + 1, now);
                }
                else if (Phase == NarrativeDialoguePhase.Typing)
                { advanceAfterCompletion = true; view.CompleteText(CurrentLine, now); }
                else RequestEnd(now);
            }
        }
        void BeginLine(int index, double now)
        {
            lineIndex = index; advanceAfterCompletion = false; var line = lines[index];
            NarrativeCharacter speaker = null; string diagnostic;
            if (!string.IsNullOrEmpty(line.Speaker) && !directory.TryGetCharacter(line.Speaker, out speaker, out diagnostic)) diagnostics.Add(diagnostic);
            string name = line.NameOverrideKey.Length != 0 ? line.NameOverrideKey : speaker == null ? "" : speaker.NameKey;
            bool inCast = false; foreach (var entry in Current.Cast) if (string.Equals(entry.CharacterId, line.Speaker, StringComparison.Ordinal)) { inCast = true; break; }
            CurrentLine = new NarrativeLinePresentation(Current, line, name, name.Length != 0, !inCast, now);
            Phase = NarrativeDialoguePhase.Typing; input.NotifyLineStarted();
            CurrentLineBatch = scheduler.StartCachedLine(line.Commands, Current.Dialogue.Level, now, Current.Snapshot.Get<NarrativeCommandExecutionMode>("tier.commandExecutionMode"));
            foreach (var issue in CurrentLineBatch.Diagnostics) diagnostics.Add(issue.Reason);
            view.SetSpeaker(CurrentLine, now); view.ReactToLine(CurrentLine, now); view.StartText(CurrentLine, now);
            DrainSwitches(); if (pendingSwitches.Count != 0) BeginExit(now);
        }

        void RequestEnd(double now)
        {
            DrainSwitches(); NarrativeDialogue next; string ignored;
            bool seamless = Current.Snapshot.Get<DialogueContinuationMode>("exit.continuationMode") == DialogueContinuationMode.Seamless &&
                pendingSwitches.Count == 0 && !HasSwitch(CurrentLineBatch, false) && !HasCachedSwitch(Current.Dialogue.OnEnd) &&
                Current.Dialogue.NextDialogue.Length != 0 && directory.TryGetDialogue(Current.Dialogue.NextDialogue, out next, out ignored) && next.Level == Current.Dialogue.Level;
            if (seamless) FinishSegment(now, true); else BeginExit(now);
        }
        void BeginExit(double now)
        {
            if (Current == null || finished || Phase == NarrativeDialoguePhase.Exiting) return;
            Phase = NarrativeDialoguePhase.Exiting; scheduler.EndDialogue();
            if (Current.Dialogue.Level == NarrativeDialogueLevel.Story) scheduler.CancelCameraCommands(now);
            DrainSwitches(); view.Exit(Current, now);
        }
        public bool NotifyExitCompleted(NarrativeDialogueContext context)
        {
            if (!ReferenceEquals(Current, context) || Phase != NarrativeDialoguePhase.Exiting || finished) return false;
            FinishSegment(Now(), false); return true;
        }
        void FinishSegment(double now, bool seamless)
        {
            if (finished) return; finished = true; scheduler.EndDialogue();
            if (seamless && Current.Dialogue.Level == NarrativeDialogueLevel.Story) scheduler.CancelCameraCommands(now);
            if (seamless && Current.Dialogue.Level == NarrativeDialogueLevel.Story) scheduler.CancelCameraCommands(now);
            onEndBatch = scheduler.StartCachedOnEnd(Current.Dialogue.OnEnd, Current.Dialogue.Level, now, Current.Snapshot.Get<NarrativeCommandExecutionMode>("tier.commandExecutionMode"));
            foreach (var issue in onEndBatch.Diagnostics) diagnostics.Add(issue.Reason);
            talked.MarkTalked(Current.Dialogue.DialogueId);
            if (locked) { operationLock.Release(Current.Dialogue.DialogueId, now); locked = false; }
            view.DialogueEnded(Current, now); DrainSwitches();
            if (switchObserved || HasSwitch(CurrentLineBatch, false) || HasSwitch(onEndBatch, false))
            { Phase = NarrativeDialoguePhase.WaitingForSceneSwitch; ContinueAfterSwitch(now); }
            else StartFollowUp(now, seamless);
        }
        void StartFollowUp(double now, bool seamless)
        {
            string nextId = Current.Dialogue.NextDialogue; var previousLevel = Current.Dialogue.Level;
            if (nextId.Length == 0) { Idle(); return; }
            NarrativeDialogue next; string diagnostic;
            if (!directory.TryGetDialogue(nextId, out next, out diagnostic)) { diagnostics.Add(diagnostic); Idle(); return; }
            Begin(nextId, seamless && next.Level == previousLevel, now);
        }
        void DrainSwitches()
        {
            NarrativeCommandInvocation command;
            while (scheduler.TryTakeSceneSwitch(out command))
            { switchObserved = true; if (!pendingSwitches.Contains(command)) pendingSwitches.Add(command); }
        }
        public bool NotifySceneSwitchCompleted(NarrativeCommandInvocation command)
        {
            if (command == null || !pendingSwitches.Remove(command)) return false;
            if (Phase == NarrativeDialoguePhase.WaitingForSceneSwitch) { DrainSwitches(); ContinueAfterSwitch(Now()); }
            return true;
        }
        void ContinueAfterSwitch(double now)
        {
            if (pendingSwitches.Count != 0 || HasSwitch(CurrentLineBatch, true) || HasSwitch(onEndBatch, true)) return;
            if (FailedSwitch(CurrentLineBatch) || FailedSwitch(onEndBatch))
            { diagnostics.Add("Scene-switch command did not complete successfully; continuation was not started."); Idle(); return; }
            StartFollowUp(now, false);
        }
        static bool HasCachedSwitch(NarrativeCommandSource source)
        { foreach (var command in source.Commands) if (command.Name == "scene_switch") return true; return false; }
        static bool HasSwitch(NarrativeCommandBatch batch, bool onlyActive)
        {
            if (batch != null) foreach (var command in batch.Commands)
                if (command.Invocation.Name == "scene_switch" && (!onlyActive || command.State == NarrativeCommandExecutionState.Pending || command.State == NarrativeCommandExecutionState.Running)) return true;
            return false;
        }
        static bool FailedSwitch(NarrativeCommandBatch batch)
        {
            if (batch != null) foreach (var command in batch.Commands)
                if (command.Invocation.Name == "scene_switch" && (command.State == NarrativeCommandExecutionState.Failed || command.State == NarrativeCommandExecutionState.Skipped)) return true;
            return false;
        }
        void Idle() { Phase = NarrativeDialoguePhase.Inactive; Current = null; CurrentLine = null; }
        double Now()
        {
            double value = clock.NowSeconds;
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException("clock", "Expected finite monotonic seconds.");
            return value;
        }
    }
}
