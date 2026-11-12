using System;
using EatWhat.LongLoop;
using EatWhat.LongLoop.Configuration;
using UnityEngine;

internal static class StreetDynamicZoomTests
{
    private static int checks;
    private static void Check(bool ok, string label) { checks++; if (!ok) throw new Exception(label); }
    private static void Near(double actual, double expected, string label, double tolerance = 1e-5)
        => Check(!double.IsNaN(actual) && Math.Abs(actual - expected) <= tolerance, label + ": " + actual + " / " + expected);
    private static StreetDynamicZoomSettings Settings() => new StreetDynamicZoomSettings {
        Enabled = true, MinSize = 4, MaxSize = 8, ExpandTime = 1, ShrinkTime = 2,
        ExpandCurve = StreetCurvePreset.Linear, ShrinkCurve = StreetCurvePreset.Linear };
    private static StreetDynamicZoomResult Step(StreetDynamicZoomState state, StreetDynamicZoomSettings settings,
        bool moving, double dt, bool frozen = false) => StreetDynamicZoom.Evaluate(state,
            new StreetDynamicZoomInput { Moving = moving, DeltaTime = dt, Frozen = frozen }, settings);

    public static int Main()
    {
        try
        {
            EndpointsAndCurves(); Reversals(); DisabledAndConstant(); Freeze(); FrameRates(); Invalid(); Movement();
            ShrinkDelay(); DelayFreezeAndConfig(); InvalidWait();
            SuspendAndRestartShrink();
            Console.WriteLine("DYNAMIC_ZOOM PASS scenarios=11 assertions=" + checks);
            // Existing configuration suite is compiled and called unchanged.
            return StreetConfigLogicTests.Main();
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }

    private static void EndpointsAndCurves()
    {
        double[] quarter = { .25, .0625, .15625, .103515625 };
        foreach (StreetCurvePreset curve in Enum.GetValues(typeof(StreetCurvePreset)))
        {
            var s = Settings(); s.ExpandCurve = s.ShrinkCurve = curve;
            var start = Step(default, s, true, 9);
            Check(start.Valid && start.Size.HasValue, "initialized valid output");
            Near(start.Size.Value, 4, "first sample is minimum"); Near(start.Ratio, .5, "initial ratio");
            var mid = Step(start.State, s, true, .25);
            Near(mid.Size.Value, 4 + 4 * quarter[(int)curve], "expand preset quarter");
            var max = Step(mid.State, s, true, .75);
            Near(max.Size.Value, 8, "expand endpoint"); Near(max.Ratio, 1, "max ratio");
            var shrinking = Step(max.State, s, false, .5);
            Near(shrinking.Size.Value, 8 - 4 * quarter[(int)curve], "shrink preset quarter");
            var min = Step(shrinking.State, s, false, 1.5);
            Near(min.Size.Value, 4, "shrink endpoint");
            Near(Step(min.State, s, false, 100).Size.Value, 4, "large dt stays at endpoint");
        }
        var zero = Settings(); zero.ExpandTime = zero.ShrinkTime = 0;
        var state = Step(default, zero, true, .1).State;
        Near(Step(state, zero, true, 0).Size.Value, 4, "zero delta holds");
        state = Step(state, zero, true, .1).State;
        Near(state.Size, 8, "zero expand completes next advancing step");
        Near(Step(state, zero, false, .1).Size.Value, 4, "zero shrink completes");
    }

    private static void Reversals()
    {
        foreach (StreetCurvePreset curve in Enum.GetValues(typeof(StreetCurvePreset)))
        {
            var s = Settings(); s.ExpandCurve = s.ShrinkCurve = curve;
            var state = Step(default, s, true, 0).State;
            state = Step(state, s, true, .4).State;
            var reverse = Step(state, s, false, 0);
            Near(reverse.State.Size, state.Size, "stop anchors current size", 0);
            Near(reverse.State.StartSize, state.Size, "shrink start is prior value", 0);
            var shrunk = Step(reverse.State, s, false, .4);
            Check(shrunk.State.Size <= state.Size && shrunk.State.Size >= 4, "shrink does not jump to maximum");
            var restart = Step(shrunk.State, s, true, 0);
            Near(restart.State.Size, shrunk.State.Size, "restart anchors current size", 0);
            var expanded = Step(restart.State, s, true, .2);
            Check(expanded.State.Size >= restart.State.Size && expanded.State.Size <= 8, "expand does not jump to minimum");
            Near(Step(restart.State, s, true, 1).Size.Value, 8, "full interval from restart");
        }
    }

    private static void DisabledAndConstant()
    {
        var s = Settings(); var state = Step(default, s, true, 0).State;
        state = Step(state, s, true, .5).State;
        s.Enabled = false;
        var off = Step(state, s, false, 50);
        Check(off.Valid && !off.Size.HasValue && off.Ratio == 1, "disabled produces no size and unit ratio");
        Check(off.State.Equals(state), "disabled preserves caller state");
        s.Enabled = true;
        Near(Step(off.State, s, true, .25).Size.Value, 7, "reenable resumes preserved value");
        s.MinSize = s.MaxSize;
        foreach (bool moving in new[] { false, true })
        {
            var equal = Step(default, s, moving, 0);
            for (int i = 0; i < 3; i++)
            {
                equal = Step(equal.State, s, moving, .5);
                Near(equal.Size.Value, 8, "equal limits constant"); Near(equal.Ratio, 1, "equal limits unit ratio");
            }
        }
    }

    private static void Freeze()
    {
        var s = Settings(); var state = Step(default, s, true, 0).State;
        state = Step(state, s, true, .4).State;
        var frozen = Step(state, s, false, 20, true);
        Check(frozen.State.Equals(state), "frozen preserves all progress despite movement change");
        Near(frozen.Size.Value, state.Size, "frozen size");
        Near(Step(frozen.State, s, true, .1).Size.Value, 6, "same direction resumes remaining curve");
        var resumeStopped = Step(frozen.State, s, false, 0);
        Near(resumeStopped.Size.Value, state.Size, "new direction resumes frozen value");
        Near(Step(resumeStopped.State, s, false, 2).Size.Value, 4, "new direction finishes shrink");
        var firstFrozen = Step(default, s, true, 30, true);
        Near(firstFrozen.Size.Value, 4, "first frozen sample initializes minimum");
    }

    private static void FrameRates()
    {
        foreach (int fps in new[] { 30, 60, 120 })
        foreach (StreetCurvePreset curve in Enum.GetValues(typeof(StreetCurvePreset)))
        {
            var s = Settings(); s.ExpandCurve = s.ShrinkCurve = curve;
            var state = Step(default, s, true, 0).State;
            for (int frame = 1; frame <= fps; frame++)
            {
                state = Step(state, s, true, 1d / fps).State;
                if (frame == fps / 2) Near(state.Size, curve == StreetCurvePreset.Quadratic ? 5 : 6, "common half-time size");
                if (frame == fps - 1) Check(state.Size < 8, "does not finish a frame early");
            }
            Near(state.Size, 8, "all fps finish expand at 1 second");
            for (int frame = 1; frame <= 2 * fps; frame++) state = Step(state, s, false, 1d / fps).State;
            Near(state.Size, 4, "all fps finish shrink at 2 seconds");
        }
    }

    private static void Invalid()
    {
        var s = Settings(); var state = Step(default, s, true, 0).State;
        foreach (float maximum in new[] { 0, -1, float.NaN, float.PositiveInfinity })
        { var bad = s; bad.MaxSize = maximum; Rejected(state, bad); }
        var inverted = s; inverted.MinSize = 9; Rejected(state, inverted);
        var negativeExpand = s; negativeExpand.ExpandTime = -1; Rejected(state, negativeExpand);
        var negativeShrink = s; negativeShrink.ShrinkTime = -1; Rejected(state, negativeShrink);
        var badCurve = s; badCurve.ExpandCurve = (StreetCurvePreset)99; Rejected(state, badCurve);
        foreach (double dt in new[] { -1d, double.NaN, double.PositiveInfinity })
        { var bad = Step(state, s, true, dt); Check(!bad.Valid && !bad.Size.HasValue && bad.State.Equals(state), "bad delta rejected unchanged"); }
        var corrupt = state; corrupt.Elapsed = double.NaN;
        Check(!Step(corrupt, s, true, .1).Valid, "corrupt state rejected");
    }
    private static void Rejected(StreetDynamicZoomState state, StreetDynamicZoomSettings settings)
    {
        var bad = Step(state, settings, true, .1);
        Check(!bad.Valid && !bad.Size.HasValue && bad.State.Equals(state), "invalid configuration refused without state repair");
    }

    private static void Movement()
    {
        var followSettings = new StreetFollowSettings { MoveSpeedThreshold = .5f, RecenterCurve = StreetCurvePreset.Linear };
        var previous = StreetFollowPipeline.Initialize(Vector3.zero, 0, 0);
        foreach (float y in new[] { .5f, .6f, -.6f })
        {
            var step = StreetFollowPipeline.StepGoal(previous, new StreetFollowInput { Foot = new Vector3(0, y, 0),
                Time = 1, DeltaTime = 1, VisibleSize = new Vector2(16, 9) }, followSettings);
            Check(step.Status == FollowStepStatus.Advanced && step.Moving == (Math.Abs(y) > .5), "existing strict 2D foot-speed threshold");
            var s = Settings(); var initial = Step(default, s, false, 0);
            Near(Step(initial.State, s, step.Moving, 1).Size.Value, step.Moving ? 8 : 4, "vertical movement drives size input");
        }
    }

    private static void ShrinkDelay()
    {
        var s = Settings(); s.ShrinkDelay = .5f;
        var state = Step(default, s, true, 0).State;
        state = Step(state, s, true, .5).State;
        Near(state.Size, 6, "delay setup mid-expand");
        var stopped = Step(state, s, false, .25);
        Near(stopped.Size.Value, 6, "stop during expansion holds actual size");
        Near(stopped.State.ShrinkWaitElapsed, .25, "first stopped dt counts toward wait");
        Near(stopped.State.Elapsed, 0, "wait does not consume shrink time");
        var boundary = Step(stopped.State, s, false, .25);
        Near(boundary.Size.Value, 6, "exact delay boundary still holds");
        Near(boundary.State.ShrinkWaitElapsed, .5, "wait reaches delay");
        var crossing = Step(stopped.State, s, false, .75);
        Near(crossing.Size.Value, 5.5, "crossing consumes only remaining half second of shrink");
        Near(crossing.State.Elapsed, .5, "cross-boundary leftover dt");
        var resumed = Step(stopped.State, s, true, .25);
        Near(resumed.Size.Value, 6.5, "movement resumes expansion from held size");
        Near(resumed.State.ShrinkWaitElapsed, 0, "movement cancels pending wait");
        var stoppedAgain = Step(resumed.State, s, false, .25);
        Near(stoppedAgain.Size.Value, 6.5, "second stop gets a complete new delay");
        Near(stoppedAgain.State.ShrinkWaitElapsed, .25, "second stopped wait starts from zero");
        Near(Step(stoppedAgain.State, s, false, 100).Size.Value, 4, "large dt finishes wait and shrink");
        var held = Step(stopped.State, s, false, 0);
        Check(held.State.Equals(stopped.State), "zero dt holds pending wait exactly");
        s.ShrinkTime = 0;
        var instant = Step(state, s, false, .5);
        Near(instant.Size.Value, 6, "zero shrink time does not skip wait or zero remainder");
        Near(Step(instant.State, s, false, .01).Size.Value, 4, "zero shrink time completes first positive remainder");
        var constant = s; constant.MinSize = constant.MaxSize;
        var equal = Step(Step(default, constant, true, 0).State, constant, false, 10);
        Near(equal.Size.Value, 8, "equal limits remain constant with delay");
        Near(equal.State.ShrinkWaitElapsed, 0, "equal limits have no pending wait");
        Near(Step(Step(default, s, false, 0).State, s, false, .25).Size.Value, 4, "initial minimum remains minimum while waiting");
    }

    private static void DelayFreezeAndConfig()
    {
        var s = Settings(); s.ShrinkDelay = .5f;
        var state = Step(Step(default, s, true, 0).State, s, true, 1).State;
        state = Step(state, s, false, .25).State;
        var frozen = Step(state, s, true, 50, true);
        Check(frozen.State.Equals(state), "freeze preserves waiting despite changed movement");
        s.Enabled = false; var disabled = Step(state, s, true, 50);
        Check(disabled.State.Equals(state) && !disabled.Size.HasValue, "disabled preserves wait without lens write");
        s.Enabled = true;
        Near(Step(disabled.State, s, false, .5).Size.Value, 7.5, "reenable consumes remaining wait then only leftover shrink dt");
        s.ShrinkDelay = 1;
        var changed = Step(state, s, false, .25);
        Near(changed.Size.Value, 8, "delay change restarts stopped wait at current size");
        Near(changed.State.ShrinkWaitElapsed, .25, "Changed uses full new delay");
        s.ShrinkDelay = 0;
        Near(Step(changed.State, s, false, .5).Size.Value, 7, "zero delay update begins current-size shrink");
        var moving = Step(state, s, true, .25).State;
        double elapsed = moving.Elapsed;
        s.ShrinkDelay = 5;
        var unaffected = Step(moving, s, true, .25);
        Near(unaffected.State.Elapsed, elapsed + .25, "delay setting does not restart expansion");
        Near(unaffected.State.ShrinkWaitElapsed, 0, "moving never keeps stopped wait");
        foreach (int fps in new[] { 30, 60, 120 })
        {
            var config = Settings(); config.ShrinkDelay = .5f;
            var value = Step(Step(default, config, true, 0).State, config, true, 1).State;
            for (int i = 0; i < fps / 2; i++) value = Step(value, config, false, 1d / fps).State;
            Near(value.Size, 8, "all fps hold throughout delay");
            for (int i = 0; i < 2 * fps; i++) value = Step(value, config, false, 1d / fps).State;
            Near(value.Size, 4, "all fps finish delay plus original shrink duration");
        }
    }

    private static void InvalidWait()
    {
        var s = Settings(); s.ShrinkDelay = .5f;
        var state = Step(Step(default, s, true, 0).State, s, true, 1).State;
        state = Step(state, s, false, .25).State;
        foreach (float value in new[] { -1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
        { var bad = s; bad.ShrinkDelay = value; Rejected(state, bad); }
        foreach (double value in new[] { -1d, double.NaN, double.PositiveInfinity, double.NegativeInfinity, 1d })
        {
            var corrupt = state; corrupt.ShrinkWaitElapsed = value;
            var bad = Step(corrupt, s, false, .1, true);
            Check(!bad.Valid && !bad.Size.HasValue && bad.State.Equals(corrupt), "invalid waiting state rejected even frozen");
        }
        var duration = state; duration.ShrinkDelay = double.NaN;
        Check(!Step(duration, s, false, .1).Valid, "invalid remembered delay rejected");
        s.Enabled = false; s.ShrinkDelay = float.NaN;
        Check(Step(state, s, false, 50).State.Equals(state), "disabled unused invalid delay keeps existing semantics");
    }

    private static StreetDynamicZoomResult SuspendStep(StreetDynamicZoomState state,StreetDynamicZoomSettings s,
        bool moving,double dt,bool suspend,bool frozen=false)=>StreetDynamicZoom.Evaluate(state,
        new StreetDynamicZoomInput{Moving=moving,DeltaTime=dt,SuspendShrink=suspend,Frozen=frozen},s);
    private static void SuspendAndRestartShrink()
    {
        foreach(float delay in new[]{0f,.5f})
        foreach(StreetCurvePreset curve in Enum.GetValues(typeof(StreetCurvePreset)))
        {
            var s=Settings();s.ShrinkDelay=delay;s.ShrinkCurve=curve;
            var state=Step(Step(default,s,true,0).State,s,true,1).State;
            state=Step(state,s,false,delay+.25).State;
            double size=state.Size,elapsed=state.Elapsed,wait=state.ShrinkWaitElapsed;
            var paused=SuspendStep(state,s,false,50,true);
            var newlyFrozen=SuspendStep(state,s,false,50,true,true);
            Check(newlyFrozen.State.ShrinkSuspended,"new intent remembered even while zoom frozen");
            Near(newlyFrozen.State.Size,state.Size,"frozen intent does not move Size",0);
            Near(newlyFrozen.State.Elapsed,state.Elapsed,"frozen intent does not spend curve",0);
            Near(newlyFrozen.State.ShrinkWaitElapsed,state.ShrinkWaitElapsed,"frozen intent does not spend delay",0);
            var unfrozen=SuspendStep(newlyFrozen.State,s,false,0,false);
            Near(unfrozen.State.Elapsed,0,"unfreeze after release restarts full curve");
            Near(unfrozen.State.ShrinkWaitElapsed,0,"unfreeze after release restarts full delay");
            Near(paused.State.Size,size,"intent holds current shrinking size",0);
            Near(paused.State.Elapsed,elapsed,"intent does not spend shrink curve time",0);
            Near(paused.State.ShrinkWaitElapsed,wait,"intent does not spend delay",0);
            Check(paused.State.ShrinkSuspended,"suspension remembered for single release edge");
            var moving=SuspendStep(paused.State,s,true,.25,true);
            Check(moving.Size.Value>size,"real movement still expands while intent active");
            var frozen=SuspendStep(paused.State,s,false,20,true,true);
            Check(frozen.State.Equals(paused.State),"higher freeze preserves all paused progress");
            var off=s;off.Enabled=false;
            Check(SuspendStep(paused.State,off,false,20,false).State.Equals(paused.State),"disabled keeps original hold semantics");
            var resumed=SuspendStep(paused.State,s,false,delay>0?.25:.1,false);
            if(delay>0) {
                Near(resumed.Size.Value,size,"release starts full delay at current value",0);
                Near(resumed.State.ShrinkWaitElapsed,.25,"first release frame begins new delay");
                resumed=SuspendStep(resumed.State,s,false,.25,false);
                Near(resumed.Size.Value,size,"delay completes without extra shrink time",0);
                Near(resumed.State.ShrinkWaitElapsed,.5,"ordinary frames do not restart new delay");
            } else Check(resumed.Size.Value<size,"zero delay starts a fresh shrink immediately");
            var again=SuspendStep(resumed.State,s,false,10,true);
            Near(again.Size.Value,resumed.Size.Value,"retrigger holds current value",0);
            var second=SuspendStep(again.State,s,false,0,false);
            Near(second.Size.Value,again.Size.Value,"zero dt release has no endpoint jump",0);
            Near(second.State.Elapsed,0,"second release restarts full curve");
            Near(second.State.ShrinkWaitElapsed,0,"second release restarts full delay");
            var changed=s;changed.ShrinkDelay=1;
            var updated=SuspendStep(again.State,changed,false,.25,false);
            Near(updated.Size.Value,again.Size.Value,"Changed during pause uses current size");
            Near(updated.State.ShrinkWaitElapsed,.25,"Changed consumes new complete delay");
            var completed=SuspendStep(updated.State,changed,false,100,false);
            Near(completed.Size.Value,4,"resumed curve eventually reaches minimum");
        }
    }

}
