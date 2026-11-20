using System;
using System.Collections.Generic;
using EatWhat.Narrative.Core;

namespace EatWhat.Narrative.Unity
{
    /// <summary>Exit half and the composition's time entry. All surface time still passes through entrance.Advance.</summary>
    public sealed class NarrativeDialogueExit
    {
        sealed class Run
        {
            public NarrativeDialogueContext Context;
            public readonly NarrativeStepSequence Sequence = new NarrativeStepSequence();
            public double BoxTime, BoxElapsed;
            public bool Withdrawn, Hidden, Reported;
            public DialogueControlResume Resume;
        }
        readonly NarrativeDialogueEntrance entrance;
        Run run;
        NarrativeDialogueContext lastRequest;
        public event Action<NarrativeDialogueContext> ExitCompleted;
        public IReadOnlyList<NarrativeStepSequence.StepState> ExitSteps { get { return run == null ? empty : run.Sequence.Steps; } }
        static readonly IReadOnlyList<NarrativeStepSequence.StepState> empty = new List<NarrativeStepSequence.StepState>().AsReadOnly();

        public NarrativeDialogueExit(NarrativeDialogueEntrance entrance)
        { this.entrance = entrance ?? throw new ArgumentNullException(nameof(entrance)); }

        public void Exit(NarrativeDialogueContext context, double nowSeconds)
        {
            if (!Current(context) || ReferenceEquals(lastRequest, context)) return;
            lastRequest = context; entrance.SuspendForExit(context);
            var snapshot = context.Snapshot;
            run = new Run { Context = context, BoxTime = snapshot.Get<float>("exit.boxTime"),
                Resume = snapshot.Get<DialogueControlResume>("exit.controlResume") };
            run.Sequence.Begin(new[] { new NarrativeSequenceStep("box", run.BoxTime),
                new NarrativeSequenceStep("portrait", 0), new NarrativeSequenceStep("undim", snapshot.Get<float>("exit.undimTime")) },
                snapshot.Get<NarrativeCommandExecutionMode>("exit.order") == NarrativeCommandExecutionMode.Parallel
                    ? NarrativeSequenceMode.Parallel : NarrativeSequenceMode.Sequential, new[] { "portrait" });
            Apply(run); // Even an all-zero exit is only acknowledged by a later Advance.
        }

        public void Advance(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            var active = run;
            if (active == null || !Current(active.Context))
            { run = null; entrance.Advance(seconds); return; }
            // Split at box closure so sequential undim starts at that boundary, and an early next entrance
            // consumes only the remaining time. The slices sum to seconds; no second component clock exists.
            double first = active.Hidden ? seconds : Math.Min(seconds, Math.Max(0, active.BoxTime - active.BoxElapsed));
            AdvancePart(active, first);
            double remaining = seconds - first;
            if (remaining > 0)
            {
                if (ReferenceEquals(run, active) && Current(active.Context)) AdvancePart(active, remaining);
                else entrance.Advance(remaining);
            }
        }
        void AdvancePart(Run active, double seconds)
        {
            entrance.Advance(seconds);
            if (!ReferenceEquals(run, active) || !Current(active.Context)) return;
            active.BoxElapsed = Math.Min(active.BoxTime, active.BoxElapsed + seconds);
            active.Sequence.Advance(seconds); Apply(active);
            bool ready = active.Resume == DialogueControlResume.AfterBoxClosed ? active.Hidden : active.Sequence.IsComplete;
            if (active.Sequence.IsComplete) run = null;
            if (ready && !active.Reported && Current(active.Context))
            { active.Reported = true; ExitCompleted?.Invoke(active.Context); }
        }
        void Apply(Run active)
        {
            if (!Current(active.Context)) return;
            foreach (var step in active.Sequence.Steps)
            {
                if (step.Name == "box" && step.HasStarted && !active.Hidden)
                {
                    entrance.Box.SetEntranceProgress(active.Context.Snapshot, 1 - (float)step.Progress);
                    if (step.IsComplete) { active.Hidden = true; entrance.Box.Hide(); }
                }
                if (step.Name == "undim" && step.HasStarted && !active.Withdrawn)
                {
                    active.Withdrawn = true;
                    entrance.Dimmer.Withdraw(entrance.DimmingOwnerToken, active.Context.Snapshot.Get<float>("exit.undimTime"));
                }
            }
        }
        bool Current(NarrativeDialogueContext context)
        { return context != null && ReferenceEquals(entrance.CurrentContext, context); }
        /// <summary>True service abort only. Normal segment end must leave background undim alive.</summary>
        public void Clear()
        { run = null; lastRequest = null; entrance.Clear(); }
    }
}
