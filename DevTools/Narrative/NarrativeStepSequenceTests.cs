using System;
using System.Collections.Generic;
using EatWhat.Narrative.Core;

internal static class NarrativeStepSequenceTests
{
    static readonly List<string> details = new List<string>();
    static void Check(bool value, string name)
    { if (!value) throw new Exception("Sequence: " + name); details.Add(name); }
    static void Progress(NarrativeStepSequence run, int index, double expected, string name)
    { Check(Math.Abs(run.Steps[index].Progress - expected) < 1e-9, name); }
    static NarrativeSequenceStep Step(string name, double seconds) { return new NarrativeSequenceStep(name, seconds); }
    static void Invalid(Action work, string name)
    { bool rejected = false; try { work(); } catch (ArgumentException) { rejected = true; } Check(rejected, name); }

    public static void Run()
    {
        details.Clear(); var run = new NarrativeStepSequence();
        var steps = new[] { Step("dim", 2), Step("portrait", 1), Step("dialogue", 3) };
        Check(run.IsComplete && !run.Advance(1) && !run.Skip(), "unstarted run has no completion pulse");
        Check(!run.Begin(steps, NarrativeSequenceMode.Sequential) && !run.IsComplete, "sequential Begin active");
        Check(run.Steps.Count == 3 && run.Steps[0].Name == "dim" && run.Steps[2].DurationSeconds == 3, "named durations retained in order");
        Check(run.Steps[0].HasStarted && !run.Steps[1].HasStarted && !run.Steps[2].HasStarted, "only first sequential step starts at zero");
        Progress(run, 0, 0, "sequential first starts at zero");
        Check(!run.Advance(1), "sequential midpoint not complete"); Progress(run, 0, .5, "first step midpoint");
        Progress(run, 1, 0, "next step cannot advance early");
        Check(!run.Advance(1), "first boundary is not overall finish");
        Check(run.Steps[0].IsComplete && run.Steps[1].HasStarted && !run.Steps[2].HasStarted, "second starts exactly at first boundary");
        Progress(run, 1, 0, "second has zero progress at start boundary");
        run.Advance(.5); Progress(run, 1, .5, "second midpoint after own half second");
        run.Advance(.5); Check(run.Steps[1].IsComplete && run.Steps[2].HasStarted, "third starts at sum of first two durations");
        Progress(run, 2, 0, "third starts with no free time");
        run.Advance(1.5); Progress(run, 2, .5, "third midpoint");
        Check(run.Advance(1.5) && run.IsComplete, "sequential completes at sum duration six");
        foreach (var state in run.Steps) Check(state.Progress == 1, "sequential final progress " + state.Name);
        Check(!run.Advance(100) && !run.Skip(), "sequential completion cannot repeat");

        Check(!run.Begin(steps, NarrativeSequenceMode.Parallel), "parallel Begin active");
        foreach (var state in run.Steps) Check(state.HasStarted && state.Progress == 0, "parallel starts together " + state.Name);
        run.Advance(.5); Progress(run, 0, .25, "parallel dim half second"); Progress(run, 1, .5, "parallel portrait half second");
        Progress(run, 2, 1.0 / 6, "parallel dialogue half second");
        Check(!run.Advance(.5) && run.Steps[1].IsComplete, "shortest ends at one without overall finish");
        Progress(run, 0, .5, "other parallel step continues after shortest");
        Check(!run.Advance(1) && run.Steps[0].IsComplete, "second ends at two without overall finish");
        Progress(run, 2, 2.0 / 3, "longest uses its own duration");
        Check(run.Advance(1) && run.IsComplete, "parallel completes at longest duration three");
        Check(!run.Advance(1) && !run.Skip(), "parallel completion cannot repeat");

        var zeros = new[] { Step("zero-first", 0), Step("a", 1), Step("zero-middle", 0), Step("b", 2), Step("zero-last", 0) };
        run.Begin(zeros, NarrativeSequenceMode.Sequential);
        Check(run.Steps[0].IsComplete && run.Steps[1].HasStarted && !run.Steps[2].HasStarted, "leading zero drains at Begin only");
        Check(!run.Advance(1) && run.Steps[2].IsComplete && run.Steps[3].HasStarted, "middle zero drains at exact boundary");
        Progress(run, 3, 0, "zero middle does not delay next step start");
        Check(run.Advance(2) && run.Steps[4].IsComplete, "trailing zero drains on same finishing call");
        run.Begin(zeros, NarrativeSequenceMode.Parallel);
        Check(run.Steps[0].IsComplete && run.Steps[2].IsComplete && run.Steps[4].IsComplete, "all parallel zeros finish at Begin");
        Check(!run.Advance(1) && run.Advance(1), "parallel zeros add no time");
        foreach (var mode in new[] { NarrativeSequenceMode.Sequential, NarrativeSequenceMode.Parallel })
        {
            Check(run.Begin(new NarrativeSequenceStep[0], mode) && run.IsComplete, "empty completes at Begin " + mode);
            Check(!run.Advance(0) && !run.Advance(-1) && !run.Advance(1) && !run.Skip(), "empty completion once " + mode);
            Check(run.Begin(new[] { Step("zero-a", 0), Step("zero-b", 0) }, mode), "all zero completes at Begin " + mode);
            Check(run.Steps[0].IsComplete && run.Steps[1].IsComplete && !run.Skip(), "all zero reports once " + mode);
            run.Begin(steps, mode); run.Advance(.5);
            Check(run.Skip() && run.IsComplete, "skip emits completion " + mode);
            foreach (var state in run.Steps) Check(state.HasStarted && state.Progress == 1, "skip includes pending step " + mode + " " + state.Name);
            Check(!run.Skip() && !run.Advance(100), "skip completion cannot repeat " + mode);
            run.Begin(steps, mode);
            Check(run.Skip() && run.Steps[2].IsComplete, "skip before first tick " + mode);
        }

        run.Begin(steps, NarrativeSequenceMode.Sequential, new[] { "portrait" });
        Check(run.Steps.Count == 2 && run.Steps[1].Name == "dialogue", "removed step absent from progress list");
        run.Advance(2); Check(run.Steps[1].HasStarted, "removed middle step does not delay start");
        Check(run.Advance(3), "removed sequential step total is five");
        run.Begin(steps, NarrativeSequenceMode.Sequential, new[] { "dim", "dialogue", "portrait", "portrait" });
        Check(run.IsComplete && run.Steps.Count == 0 && !run.Advance(1), "all removed completes at Begin without waiting");
        run.Begin(steps, NarrativeSequenceMode.Sequential, new[] { "dim", "dialogue" });
        Check(run.Steps.Count == 1 && run.Steps[0].HasStarted && run.Advance(1), "removed first and last leave only own duration");
        run.Begin(steps, NarrativeSequenceMode.Parallel, new[] { "dialogue" });
        Check(run.Advance(2), "removing longest parallel step lowers overall duration");
        run.Begin(steps, NarrativeSequenceMode.Parallel, new[] { "missing" });
        Check(run.Steps.Count == 3 && run.Advance(3), "unknown removed name leaves real steps unchanged");
        Check(steps.Length == 3 && steps[1].Name == "portrait", "omission never mutates caller definitions");

        run.Begin(steps, NarrativeSequenceMode.Sequential); run.Advance(.5);
        Check(!run.Advance(0) && !run.Advance(-4), "nonpositive advance is harmless");
        Progress(run, 0, .25, "negative delta never rewinds or progresses");
        Check(!run.Steps[1].HasStarted && !run.IsComplete, "nonpositive delta never starts pending steps");
        run.Begin(steps, NarrativeSequenceMode.Parallel); run.Advance(.5);
        Check(!run.Advance(0) && !run.Advance(-1), "parallel nonpositive input harmless");
        Progress(run, 1, .5, "parallel negative delta preserves progress");

        foreach (var mode in new[] { NarrativeSequenceMode.Sequential, NarrativeSequenceMode.Parallel })
        {
            var whole = new NarrativeStepSequence(); var split = new NarrativeStepSequence();
            var decimalSteps = new[] { Step("a", .1), Step("z", 0), Step("b", .2), Step("c", .3) };
            whole.Begin(decimalSteps, mode); split.Begin(decimalSteps, mode);
            double duration = mode == NarrativeSequenceMode.Sequential ? .6 : .3;
            Check(whole.Advance(duration), "one tick crosses every step " + mode);
            int reports = 0;
            for (int i = 0; i < 20; i++) if (split.Advance(.05)) reports++;
            Check(reports == 1 && split.IsComplete, "partitioned decimal ticks report once " + mode);
            for (int i = 0; i < split.Steps.Count; i++)
                Check(split.Steps[i].Progress == whole.Steps[i].Progress, "partitioned final progress " + mode + " " + i);
            whole.Begin(steps, mode);
            Check(whole.Advance(double.MaxValue), "large finite tick clamps without overflow " + mode);
        }

        var authoring = new List<NarrativeSequenceStep>(steps);
        run.Begin(authoring, NarrativeSequenceMode.Sequential); var previousStates = run.Steps;
        authoring.Clear(); Check(run.Steps.Count == 3, "Begin snapshots caller collection");
        run.Advance(1); run.Begin(new[] { Step("exit", 4) }, NarrativeSequenceMode.Parallel);
        Check(run.Steps.Count == 1 && run.Steps[0].Name == "exit" && !run.IsComplete, "same component reusable for exit");
        Progress(run, 0, 0, "new run resets own progress");
        Check(previousStates[0].Progress == .5, "new run does not mutate retained prior run state");
        var other = new NarrativeStepSequence(); other.Begin(steps, NarrativeSequenceMode.Sequential);
        run.Advance(2); Check(other.Steps[0].Progress == 0, "two runs own independent progress");
        Check(run.Advance(2), "new run has no inherited time credit");
        run.Begin(new[] { Step("a", 1), Step("A", 2) }, NarrativeSequenceMode.Sequential);
        Check(run.Steps.Count == 2, "step names use ordinal exact identity");
        Invalid(() => new NarrativeSequenceStep("", 1), "empty step name rejected");
        Invalid(() => Step("bad", -1), "negative duration rejected");
        Invalid(() => Step("bad", double.NaN), "NaN duration rejected");
        Invalid(() => Step("bad", double.PositiveInfinity), "infinite duration rejected");
        run.Begin(steps, NarrativeSequenceMode.Sequential); run.Advance(.5);
        Invalid(() => run.Begin(null, NarrativeSequenceMode.Sequential), "missing definitions rejected");
        Invalid(() => run.Begin(new NarrativeSequenceStep[] { null }, NarrativeSequenceMode.Sequential), "null step rejected");
        Invalid(() => run.Begin(new[] { Step("a", 1), Step("a", 2) }, NarrativeSequenceMode.Sequential), "duplicate names rejected");
        Invalid(() => run.Begin(steps, (NarrativeSequenceMode)99), "invalid mode rejected");
        Invalid(() => run.Begin(steps, NarrativeSequenceMode.Sequential, new string[] { null }), "invalid omission rejected");
        Invalid(() => run.Advance(double.NaN), "NaN delta rejected");
        Invalid(() => run.Advance(double.PositiveInfinity), "positive infinite delta rejected");
        Check(run.Steps.Count == 3 && run.Mode == NarrativeSequenceMode.Sequential && !run.IsComplete, "invalid input preserves existing run");
        Progress(run, 0, .25, "invalid input preserves exact prior progress");
        Console.WriteLine("SEQUENCE_RESULT={\"passed\":true,\"checks\":" + details.Count + ",\"actualModel\":true,\"details\":[\"" + string.Join("\",\"", details.ToArray()) + "\"]}");
    }
}
