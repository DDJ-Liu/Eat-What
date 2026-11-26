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
            public double BoxTime, BoxElapsed, PortraitTime, UndimTime;
            public object PortraitBatch, DimOwner;
            public bool HasPortrait;
            public bool Withdrawn, Hidden, Reported;
            public DialogueControlResume Resume;
        }
        readonly NarrativeDialogueEntrance entrance;
        Run run;
        readonly List<Run> portraitRuns = new List<Run>();
        NarrativeDialogueContext lastRequest;
        public event Action<NarrativeDialogueContext> ExitCompleted;
        public IReadOnlyList<NarrativeStepSequence.StepState> ExitSteps { get { return run == null ? empty : run.Sequence.Steps; } }
        static readonly IReadOnlyList<NarrativeStepSequence.StepState> empty = new List<NarrativeStepSequence.StepState>().AsReadOnly();

        public NarrativeDialogueExit(NarrativeDialogueEntrance entrance)
        { this.entrance = entrance ?? throw new ArgumentNullException(nameof(entrance)); }

        public void Exit(NarrativeDialogueContext context, double nowSeconds)
        {
            if (!Current(context) || ReferenceEquals(lastRequest, context)) return;
            if(entrance.PortraitStage!=null){BeginPortraitExit(context);return;}
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
            if(entrance.PortraitStage!=null){AdvancePortraits(seconds);return;}
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
        void BeginPortraitExit(NarrativeDialogueContext context)
        {
            lastRequest=context;entrance.SuspendForExit(context);var snapshot=context.Snapshot;
            var active=new Run { Context=context,BoxTime=snapshot.Get<float>("exit.boxTime"),
                PortraitTime=snapshot.Get<float>("exit.portraitTime"),UndimTime=snapshot.Get<float>("exit.undimTime"),
                Resume=snapshot.Get<DialogueControlResume>("exit.controlResume"),HasPortrait=entrance.HasPortraits,
                PortraitBatch=entrance.PortraitBatch,DimOwner=entrance.DimmingOwnerToken };
            active.Sequence.Begin(new[]{new NarrativeSequenceStep("box",active.BoxTime),new NarrativeSequenceStep("portrait",active.PortraitTime),new NarrativeSequenceStep("undim",active.UndimTime)},
                snapshot.Get<NarrativeCommandExecutionMode>("exit.order")==NarrativeCommandExecutionMode.Parallel?NarrativeSequenceMode.Parallel:NarrativeSequenceMode.Sequential,
                active.HasPortrait?null:new[]{"portrait"});
            portraitRuns.Add(active);run=active;
            // Freeze at exit request; progress zero preserves current geometry/brightness until its sequence step starts.
            if(active.HasPortrait)entrance.PortraitStage.SetExitProgress(active.PortraitBatch,0);
            ApplyPortraitExit(active);
        }
        double NextBoundary(Run active)
        {
            double boundary=double.PositiveInfinity;
            foreach(var step in active.Sequence.Steps)if(step.HasStarted&&!step.IsComplete)
            {
                double duration=step.Name=="box"?active.BoxTime:step.Name=="portrait"?active.PortraitTime:active.UndimTime;
                boundary=Math.Min(boundary,duration*(1-step.Progress));
            }
            return boundary;
        }
        void AdvancePortraits(double seconds)
        {
            double remaining=seconds;
            do
            {
                double boundary=entrance.NextPortraitBoundary;
                foreach(var active in portraitRuns)boundary=Math.Min(boundary,NextBoundary(active));
                double part=boundary>0?Math.Min(remaining,boundary):remaining;
                var ready=portraitRuns.ToArray();
                entrance.Advance(part);
                foreach(var active in ready)
                {
                    if(!portraitRuns.Contains(active))continue;
                    active.Sequence.Advance(part);ApplyPortraitExit(active);
                    bool report=active.Resume==DialogueControlResume.AfterBoxClosed?active.Hidden:active.Sequence.IsComplete;
                    if(active.Sequence.IsComplete){portraitRuns.Remove(active);if(ReferenceEquals(run,active))run=null;}
                    if(report&&!active.Reported){active.Reported=true;ExitCompleted?.Invoke(active.Context);}
                }
                remaining-=part;
            }while(remaining>0);
        }
        void ApplyPortraitExit(Run active)
        {
            foreach(var step in active.Sequence.Steps)
            {
                if(step.Name=="box"&&step.HasStarted&&!active.Hidden&&Current(active.Context))
                {
                    entrance.Box.SetEntranceProgress(active.Context.Snapshot,1-(float)step.Progress);
                    if(step.IsComplete){active.Hidden=true;entrance.Box.Hide();}
                }
                if(step.Name=="portrait"&&step.HasStarted&&active.HasPortrait)
                    entrance.PortraitStage.SetExitProgress(active.PortraitBatch,(float)step.Progress);
                if(step.Name=="undim"&&step.HasStarted&&!active.Withdrawn)
                {active.Withdrawn=true;entrance.Dimmer.Withdraw(active.DimOwner,active.UndimTime);}
            }
        }
        bool Current(NarrativeDialogueContext context)
        { return context != null && ReferenceEquals(entrance.CurrentContext, context); }
        /// <summary>True service abort only. Normal segment end must leave background undim alive.</summary>
        public void Clear()
        {
            foreach(var active in portraitRuns)entrance.Dimmer.Withdraw(active.DimOwner,0);
            portraitRuns.Clear();run=null;lastRequest=null;entrance.Clear();
        }
    }
}
