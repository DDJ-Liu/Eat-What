using System;
using EatWhat.LongLoop;
using EatWhat.LongLoop.Configuration;
using EatWhat.Toolkit.Input;
using UnityEngine;

internal static class StreetLookFloatTests
{
    private static int checks, scenarios;
    private static void Check(bool ok, string name) { checks++; if (!ok) throw new Exception(name); }
    private static void Near(double actual, double expected, string name, double epsilon = 1e-6)
        => Check(!double.IsNaN(actual) && Math.Abs(actual - expected) <= epsilon, name + ": " + actual + " / " + expected);
    private static StreetLookFloatSettings Settings() => new StreetLookFloatSettings {
        Enabled = true, EdgeZoneRatio = .25f, ResponseCurve = StreetCurvePreset.Quadratic, InTime = 1, OutTime = 2 };
    private static StreetLookFloatResult Step(StreetLookFloatState state, Vector2 axis,
        double dt, bool gated = false, bool enabled = true)
    {
        var settings = Settings(); settings.Enabled = enabled;
        return StreetLookFloat.Evaluate(state, new StreetLookFloatInput { LookAxis = axis, DeltaTime = dt, Gated = gated }, settings);
    }
    private static void Scenario(string name, Action test)
    { test(); scenarios++; Console.WriteLine("LOOK_FLOAT PASS " + name); }
    public static int Main()
    {
        try
        {
            Scenario("CentralAndEdgeZone", Zone);
            Scenario("QuadraticIncreasingResponse", Response);
            Scenario("FullEnterAndExitDuration", Duration);
            Scenario("GateAndDisableExit", Gating);
            Scenario("IndependentAxes", Independence);
            Scenario("MirroredDirectionsAndRetarget", Symmetry);
            Scenario("ExistingNormalizedInputBoundary", InputBoundary);
            Scenario("InvalidAndTimeBoundaries", Boundaries);
            Scenario("WorldRadialDistanceSpeedAndBoundaries", WorldLimits);
            Scenario("DirectStickBypassesMouseEdges", DirectStick);
            Console.WriteLine("LOOK_FLOAT PASS scenarios=" + scenarios + " assertions=" + checks);
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
    private static void Zone()
    {
        foreach (float value in new[] { 0f, .25f, .5f, -.25f, -.5f })
        {
            var result = Step(default, new Vector2(value, value), 5);
            Check(result.Valid, "central sample valid");
            Near(result.Value.x, 0, "central X zero"); Near(result.Value.y, 0, "central Y zero");
        }
        var near = Step(default, new Vector2(.5001f, -.5001f), 1);
        Check(near.Value.x > 0 && near.Value.x < .000001, "just inside zone near zero");
        Near(near.Value.x, -near.Value.y, "both near-edge signs match");
        var corner = Step(default, Vector2.one, 1);
        Near(corner.Value.x, 1, "edge full X"); Near(corner.Value.y, 1, "edge full Y");
    }
    private static void Response()
    {
        double last = 0, lastIncrease = 0;
        foreach (float x in new[] { .6f, .7f, .8f, .9f, 1f })
        {
            var result = Step(default, new Vector2(x, 0), 1);
            double depth = (x - .5) * 2;
            Near(result.Value.x, depth * depth, "actual quadratic response");
            double increase = result.Value.x - last;
            Check(increase > lastIncrease, "equal spatial steps increase faster near edge");
            lastIncrease = increase; last = result.Value.x;
        }
        foreach (StreetCurvePreset curve in Enum.GetValues(typeof(StreetCurvePreset)))
        {
            var settings = Settings(); settings.ResponseCurve = curve;
            var result = StreetLookFloat.Evaluate(default, new StreetLookFloatInput {
                LookAxis = new Vector2(.75f, -.75f), DeltaTime = 1 }, settings);
            Near(result.Value.x, curve == StreetCurvePreset.Quadratic ? .25 : .5, "configured spatial preset");
            Near(result.Value.x, -result.Value.y, "configured response symmetric");
        }
    }
    private static void Duration()
    {
        foreach (int fps in new[] { 30, 60, 120 })
        {
            var state = new StreetLookFloatState();
            for (int i = 1; i <= fps; i++)
            {
                var result = Step(state, Vector2.one, 1d / fps); state = result.State;
                if (i == fps / 2) Near(result.Value.x, .5, "half entry duration");
                if (i == fps - 1) Check(result.Value.x < 1, "not one frame early");
            }
            Near(state.X.Value, 1, "entry endpoint at configured duration");
            for (int i = 1; i <= fps * 2; i++)
            {
                var result = Step(state, Vector2.zero, 1d / fps); state = result.State;
                if (i == fps) Near(result.Value.x, .5, "half exit duration");
            }
            Near(state.X.Value, 0, "exit endpoint at configured duration");
        }
    }
    private static void Gating()
    {
        foreach (bool gate in new[] { false, true })
        {
            var full = Step(default, new Vector2(1, -1), 1);
            var startExit = Step(full.State, Vector2.one, 0, gate, gate);
            Near(startExit.Value.x, 1, "gate/disable does not snap X");
            Near(startExit.Value.y, -1, "gate/disable does not snap Y");
            Near(startExit.Target.x, 0, "gate/disable target zero");
            var half = Step(startExit.State, Vector2.one, 1, gate, gate);
            Near(half.Value.x, .5, "gate/disable half exit");
            var done = Step(half.State, Vector2.one, 1, gate, gate);
            Near(done.Value.x, 0, "gate/disable exit reaches zero");
            Near(done.Value.y, 0, "gate/disable both axes finish");
        }
    }
    private static void Independence()
    {
        var a = Step(default, new Vector2(0, -.75f), .2).State;
        var b = a;
        for (int i = 0; i < 15; i++)
        {
            a = Step(a, new Vector2(0, -.75f), .1).State;
            b = Step(b, new Vector2(i % 2 == 0 ? 1 : -1, -.75f), .1).State;
            Check(a.Y.Equals(b.Y), "X changes cannot restart or change Y state");
        }
        Near(b.Y.Value, -.25, "Y reaches own unchanged target");
    }
    private static void Symmetry()
    {
        var a = new StreetLookFloatState(); var b = a;
        foreach (float x in new[] { 1f, 1f, .75f, -1f, -.8f, 0f, .9f, 0f })
        {
            a = Step(a, new Vector2(x, 0), .2).State;
            b = Step(b, new Vector2(-x, 0), .2).State;
            Near(a.X.Value, -b.X.Value, "mirrored trajectories", 0);
            var up = Step(default, new Vector2(0, x), 1);
            var down = Step(default, new Vector2(0, -x), 1);
            Near(up.Value.y, -down.Value.y, "up/down symmetric", 0);
        }
        var old = Step(default, Vector2.one, .4).State;
        var changed = Step(old, new Vector2(-1, .75f), 0);
        Near(changed.Value.x, old.X.Value, "retarget continuous at zero elapsed");
        Near(changed.State.X.Duration, 1, "larger magnitude uses in time");
        Near(changed.State.Y.Duration, 2, "smaller magnitude uses out time");
    }
    private static void InputBoundary()
    {
        var normalized = GameInputMath.LookAxisFromScreenPosition(new Vector2(200, -20), 100, 100);
        Near(normalized.x, 1, "existing screen provider clamps right");
        Near(normalized.y, -1, "existing screen provider clamps below");
        var clipped = Step(default, new Vector2(3, -3), 1);
        Near(clipped.Value.x, 1, "pure input clamp right"); Near(clipped.Value.y, -1, "pure input clamp below");
        var center = GameInputMath.LookAxisFromScreenPosition(new Vector2(50, 50), 100, 100);
        Near(Step(default, center, 1).Value.x, 0, "actual normalized center neutral");
    }
    private static void Boundaries()
    {
        var settings = Settings(); var input = new StreetLookFloatInput { LookAxis = Vector2.one, DeltaTime = 1 };
        foreach (float ratio in new[] { 0, -.1f, .51f, float.NaN, float.PositiveInfinity })
        { var bad = settings; bad.EdgeZoneRatio = ratio; Check(!StreetLookFloat.Evaluate(default, input, bad).Valid, "invalid zone rejected"); }
        var small = settings; small.EdgeZoneRatio = float.Epsilon;
        Near(StreetLookFloat.Evaluate(default, input, small).Value.x, 1, "tiny positive zone keeps finite edge");
        var negative = settings; negative.OutTime = -1;
        Check(!StreetLookFloat.Evaluate(default, input, negative).Valid, "negative duration rejected");
        var invalidTime = input; invalidTime.DeltaTime = double.NaN;
        Check(!StreetLookFloat.Evaluate(default, invalidTime, settings).Valid, "nonfinite dt rejected");
        var zero = settings; zero.InTime = zero.OutTime = 0;
        var full = StreetLookFloat.Evaluate(default, input, zero);
        Near(full.Value.x, 1, "zero entry immediate on advancing step");
        input.Gated = true;
        Near(StreetLookFloat.Evaluate(full.State, input, zero).Value.x, 0, "zero exit immediate");
    }

    private static double Length(Vector2 v)=>Math.Sqrt((double)v.x*v.x+(double)v.y*v.y);
    private static void WorldLimits()
    {
        foreach(StreetCurvePreset curve in Enum.GetValues(typeof(StreetCurvePreset)))
        foreach(float ratio in new[]{.1f,1f,10f})
        foreach(double dt in new[]{.001,.1,2d})
        {
            var settings=Settings();settings.ResponseCurve=curve;settings.InTime=settings.OutTime=.001f;
            var normalized=default(StreetLookFloatState);var world=default(StreetLookFloatWorldState);
            for(int i=0;i<40;i++)
            {
                var axis=i<12?new Vector2(1000,1000):i<25?new Vector2(-1000,-1000):Vector2.zero;
                var n=StreetLookFloat.Evaluate(normalized,new StreetLookFloatInput{LookAxis=axis,DeltaTime=dt},settings);
                var b=StreetLookFloat.LimitWorld(world,n.Value,new Vector2(100,100),ratio,2.5f,8,dt);
                Check(n.Valid&&b.Valid,"real normalized/world functions valid");
                Check(Length(b.Value)<=2.5+1e-5,"radial cap includes diagonal");
                Check(Length(b.Value-world.Value)<=8*dt+1e-5,"world speed bounds enter reverse exit and ratios");
                normalized=n.State;world=b.State;
            }
        }
        var state=default(StreetLookFloatWorldState);var limits=new Vector2(50,50);
        for(int i=0;i<150;i++)
        {
            var edge=Step(default,new Vector2(1,-1),1);var outside=Step(default,new Vector2(1000,-1000),1);
            Check(edge.Value==outside.Value,"finite outside raw input equals edge");
            var a=StreetLookFloat.LimitWorld(state,edge.Value,limits,3,2.5f,8,.02);
            var b=StreetLookFloat.LimitWorld(state,outside.Value,limits,3,2.5f,8,.02);
            Check(a.Valid&&b.Valid&&a.Value==b.Value,"same initial state and dt give edge equivalence");
            Check(Length(a.Value)<=2.5+1e-5,"held outside never accumulates drift");state=a.State;
        }
        state.Value=new Vector2(1,0);var target=new Vector2(-1,1);
        var held=StreetLookFloat.LimitWorld(state,target,limits,2,2.5f,8,0);
        Check(held.Valid&&held.Value==state.Value,"zero dt holds world state");
        held=StreetLookFloat.LimitWorld(state,target,limits,2,2.5f,0,10);
        Check(held.Valid&&held.Value==state.Value,"zero speed pauses normal advance");
        var corrected=StreetLookFloat.LimitWorld(state,target,limits,2,.25f,0,0);
        Check(corrected.Valid&&corrected.DistanceCorrected,"lowered limit immediately corrects old state");
        Near(Length(corrected.Value),.25,"distance correction wins over zero dt and speed");
        var zero=StreetLookFloat.LimitWorld(state,target,limits,2,0,8,.1);
        Check(zero.Valid&&zero.Value==Vector2.zero,"zero distance forbids contribution");
        foreach(float bad in new[]{-1f,float.NaN,float.PositiveInfinity,float.NegativeInfinity})
        {
            Check(!StreetLookFloat.LimitWorld(state,target,limits,1,bad,8,.1).Valid,"invalid distance rejected");
            Check(!StreetLookFloat.LimitWorld(state,target,limits,1,2.5f,bad,.1).Valid,"invalid speed rejected");
        }
        Check(!StreetLookFloat.LimitWorld(state,new Vector2(float.NaN,0),limits,1,2.5f,8,.1).Valid,"invalid normalized input rejected");
        Check(!StreetLookFloat.LimitWorld(state,target,limits,float.PositiveInfinity,2.5f,8,.1).Valid,"invalid ratio rejected");
        Check(!StreetLookFloat.LimitWorld(state,target,limits,1,2.5f,8,double.NaN).Valid,"invalid dt rejected");
        state.Value=new Vector2(float.NaN,0);
        Check(!StreetLookFloat.LimitWorld(state,target,limits,1,2.5f,8,.1).Valid,"invalid world state rejected");
    }


    private static void DirectStick()
    {
        foreach(StreetCurvePreset curve in Enum.GetValues(typeof(StreetCurvePreset)))
        {
            var settings=Settings();settings.ResponseCurve=curve;
            var small=StreetLookFloat.Evaluate(default,new StreetLookFloatInput{LookAxis=new Vector2(.2f,0),DirectAxis=true,DeltaTime=1},settings);
            Check(small.Valid&&small.Target.x>0,"weak stick target bypasses mouse edge threshold");
            Near(small.Target.x,StreetFollowPipeline.CurveValue(curve,.2f),"same response preset applied to radial strength");
            var mouse=StreetLookFloat.Evaluate(default,new StreetLookFloatInput{LookAxis=new Vector2(.2f,0),DeltaTime=1},settings);
            Near(mouse.Target.x,0,"default mouse mapping remains central neutral");
            var diagonal=StreetLookFloat.Evaluate(default,new StreetLookFloatInput{LookAxis=new Vector2(4,4),DirectAxis=true,DeltaTime=1},settings);
            Check(diagonal.Valid&&Length(diagonal.Target)<=1+1e-6,"direct target stays in unit circle for finite outside input");
            Near(diagonal.Target.x,diagonal.Target.y,"radial response preserves diagonal direction");
            var neutral=StreetLookFloat.Evaluate(small.State,new StreetLookFloatInput{DirectAxis=true,LookAxis=Vector2.zero,DeltaTime=.1},settings);
            Check(neutral.Target.x==0&&neutral.Value.x>0,"neutral Target releases immediately while Value fades");
            var state=default(StreetLookFloatWorldState);var normal=default(StreetLookFloatState);
            for(int i=0;i<100;i++) {
                var input=new StreetLookFloatInput{LookAxis=i<50?new Vector2(100,100):new Vector2(-100,-100),DirectAxis=true,DeltaTime=.1};
                var n=StreetLookFloat.Evaluate(normal,input,settings);
                var w=StreetLookFloat.LimitWorld(state,n.Value,new Vector2(100,100),3,1,.2f,.1);
                Check(w.Valid&&Length(w.Value)<=1+1e-5,"direct stick shares radial world distance cap");
                Check(Length(w.Value-state.Value)<=.02+1e-5,"direct stick shares world speed cap on reversal");
                normal=n.State;state=w.State;
            }
        }
        var invalid=StreetLookFloat.Evaluate(default,new StreetLookFloatInput{DirectAxis=true,LookAxis=new Vector2(float.NaN,0),DeltaTime=1},Settings());
        Check(!invalid.Valid,"nonfinite direct axis rejected");
    }

}
