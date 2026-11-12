using System;
using EatWhat.LongLoop;
using EatWhat.LongLoop.Configuration;
using UnityEngine;

internal static class StreetFollowCompositionTests
{
    private static int assertions, scenarios;
    private static void Check(bool ok, string name) { assertions++; if (!ok) throw new Exception(name); }
    private static void Near(double a, double b, string name, double epsilon = 1e-4)
        => Check(!double.IsNaN(a) && Math.Abs(a-b) <= epsilon, name + ": " + a + " / " + b);
    private static void Scenario(string name, Action body)
    { body(); scenarios++; Console.WriteLine("COMPOSITION PASS " + name); }
    private static StreetFollowSettings Settings() => new StreetFollowSettings {
        DeadZoneWidth=.2f, DeadZoneHeight=.2f, Lookahead=5, LookaheadTime=.1f,
        MaxHorizontalOffset=10, MaxVerticalOffset=3, MoveSpeedThreshold=.5f,
        RecenterDelay=0, RecenterSpeed=4, RecenterRampTime=.25f, RecenterCurve=StreetCurvePreset.SmoothStep };
    private static FollowClipContext Clip() => new FollowClipContext {
        Orthographic=true, ProjectionKnown=true, ScreenRight=Vector3.right, OrthoSize=10,
        Aspect=2, SafeZoneWidth=1, GateOffsetsInDialogue=true };
    private static StreetFollowCompositionInput Frame(float size=10, float previousRatio=1)
        => new StreetFollowCompositionInput { DynamicZoomEnabled=true, Size=size, MaxSize=10, PreviousRatio=previousRatio };
    private static StreetFollowInput Input(StreetFollowState state, float dx, double dt=1d/60)
        => new StreetFollowInput { Foot=new Vector3(state.PreviousFoot.x+dx,state.PreviousFoot.y,0),
            Focus=2, Time=state.LastTime+dt, DeltaTime=dt, VisibleSize=new Vector2(40,20) };
    private static StreetFollowCompositionResult Step(StreetFollowState state, float dx,
        StreetFollowCompositionInput frame, StreetFollowSettings settings, FollowClipContext? context=null)
        => StreetFollowComposition.Evaluate(state, Input(state,dx), settings, context ?? Clip(), frame);
    private static StreetFollowState Warm(float size, int direction, StreetFollowSettings settings)
    {
        var state=StreetFollowPipeline.Initialize(Vector3.zero,2,0);
        float ratio=1;
        for(int i=0;i<180;i++) {
            var r=Step(state,direction*6.5f/60,Frame(size,ratio),settings);
            Check(r.Valid,"warm valid");state=r.Follow.State;ratio=r.Ratio;
        }
        return state;
    }
    public static int Main()
    {
        try {
            Scenario("SteadyScaleAndHorizontalSum",Horizontal);
            Scenario("ContinuousSizeMovingAndExiting",Continuous);
            Scenario("VerticalSumAndCancellation",Vertical);
            Scenario("FocusVisibleRangeAndSafety",Geometry);
            Scenario("GateAndExtraChannelsNeverFeedFrontState",Isolation);
            Scenario("ExactLegacyInactivePath",Legacy);
            Scenario("LensOutputsAndUnscaledParameters",Lens);
            Scenario("HeldTimeAndInvalidInput",Guards);
            Scenario("ExplicitWorldFloatNeverRescaled",WorldFloat);
            Console.WriteLine("COMPOSITION PASS scenarios="+scenarios+" assertions="+assertions);return 0;
        } catch(Exception error) {Console.Error.WriteLine(error);return 1;}
    }
    private static void Horizontal()
    {
        var s=Settings();
        foreach(int direction in new[]{-1,1})
        foreach(float size in new[]{10f,5f}) {
            float ratio=size/10;var state=Warm(size,direction,s);var frame=Frame(size,ratio);
            var plain=Step(state,direction*6.5f/60,frame,s);
            Near(direction*(plain.Follow.Target.x-plain.Follow.GoalStep.Follow.x),5*ratio,"actor-based lead 5 or 2.5");
            frame.LookFloat=new Vector2(direction,0);var same=Step(state,direction*6.5f/60,frame,s);
            Near(same.Follow.Channels.RawSum.x,direction*10*ratio,"same-direction shared cap");
            frame.LookFloat=new Vector2(-direction,0);var opposite=Step(state,direction*6.5f/60,frame,s);
            Near(opposite.Follow.Channels.RawSum.x,-direction*5*ratio,"opposite channels cancel before cap");
            Check(plain.Follow.State.Lookahead.Equals(same.Follow.State.Lookahead),"float never changes front state");
        }
    }
    private static void Continuous()
    {
        var s=Settings();var state=Warm(10,1,s);float ratio=1;double lastLead=5;
        for(int i=1;i<=90;i++) {
            float size=10-i/18f;var r=Step(state,6.5f/60,Frame(size,ratio),s);
            Check(r.Valid,"size ramp valid");
            double lead=r.Follow.Target.x-r.Follow.GoalStep.Follow.x;
            Near(lead,5*size/10,"live moving distance follows this frame size",2e-4);
            Check(Math.Abs(lead-lastLead)<.05,"continuous moving size response");
            state=r.Follow.State;ratio=r.Ratio;lastLead=lead;
        }
        // Enter real recentering before changing size again.
        for(int i=0;i<8;i++) {var r=Step(state,0,Frame(5,ratio),s);state=r.Follow.State;ratio=r.Ratio;}
        Check(state.Lookahead.Exiting,"actual exit active");
        double last=state.Target.x;
        for(int i=1;i<=60;i++) {
            float size=5+i/12f;double oldExitTime=state.Lookahead.Exit.Time;
            var r=Step(state,0,Frame(size,ratio),s);Check(r.Valid,"exit lens ramp valid");
            Check(Math.Abs(r.Follow.Target.x-last)<.15,"exit target changes continuously with size");
            Check(r.Follow.State.Lookahead.Exit.Time>=oldExitTime,"lens ramp preserves elapsed exit time");
            Check(r.Follow.State.Lookahead.Exit.Peak<=s.RecenterSpeed,"actual profile peak respects unscaled speed cap");
            Near(r.EffectiveSettings.RecenterSpeed,s.RecenterSpeed,"exit speed parameter not scaled");
            state=r.Follow.State;ratio=r.Ratio;last=state.Target.x;
        }
        for(int i=0;i<180;i++) {var r=Step(state,0,Frame(10,ratio),s);state=r.Follow.State;ratio=r.Ratio;}
        Near(state.Target.x,state.PreviousFoot.x,"exit reaches actor after size settles");
        Near(state.Lookahead.SignedWeight,0,"exit weight reaches zero");
        // A representable non-binary ratio must not rebase/restart every frame.
        state=Warm(7.2f,1,s);ratio=7.2f/10;
        for(int i=0;i<180;i++) {
            var fractional=Frame(7.2f,ratio);fractional.MaxSize=8.3f;
            var r=Step(state,0,fractional,s);state=r.Follow.State;ratio=r.Ratio;
        }
        Near(state.Target.x,state.PreviousFoot.x,"constant fractional ratio finishes exit");
    }
    private static void Vertical()
    {
        var s=Settings();
        foreach(float size in new[]{10f,5f})
        foreach(int sign in new[]{-1,1}) {
            var state=StreetFollowPipeline.Initialize(Vector3.zero,2,0);
            var frame=Frame(size);frame.VerticalOffset=sign;frame.LookFloat=new Vector2(0,sign);
            var r=Step(state,0,frame,s);
            Check(r.Valid,"vertical composition valid");
            Near(r.Follow.Channels.Vertical.y,sign*3*size/10,"vertical full distance");
            Near(r.Follow.Channels.RawSum.y,sign*3*size/10,"vertical shared cap");
            frame.LookFloat=new Vector2(0,-sign);r=Step(state,0,frame,s);
            Near(r.Follow.Channels.RawSum.y,0,"vertical opposite cancellation");
        }
    }
    private static void Geometry()
    {
        var s=Settings();
        foreach(float size in new[]{10f,5f}) {
            var r=StreetFollowComposition.Evaluate(default,new StreetFollowInput {
                Foot=new Vector3(3,4,0),Focus=2,Time=0,DeltaTime=.01,VisibleSize=new Vector2(999,999) },s,Clip(),Frame(size));
            Near(r.Follow.GoalStep.Follow.y,6,"FOCUS unchanged by lens ratio");
            var state=StreetFollowPipeline.Initialize(Vector3.zero,2,0);
            var input=Input(state,3);input.VisibleSize=new Vector2(999,999);
            r=StreetFollowComposition.Evaluate(state,input,s,Clip(),Frame(size));
            Near(r.Follow.GoalStep.State.Goal.x,size==10?0:1,"dead zone uses current visible width");
        }
        var cap=Settings();cap.Lookahead=20;cap.MaxHorizontalOffset=20;
        var warm=Warm(2,1,cap);var frame2=Frame(2,.2f);frame2.LookFloat=new Vector2(1,0);
        var clip=Clip();clip.SafeZoneWidth=.25f;
        var clipped=Step(warm,6.5f/60,frame2,cap,clip);
        Check(clipped.Valid&&clipped.Follow.Channels.Clipped,"current lens triggers safety clip");
        Near(clipped.Follow.Target.x-clipped.Follow.GoalStep.Follow.x,1,"safety half-width uses Size2 not stale Size10",2e-4);
    }
    private static void Isolation()
    {
        var s=Settings();var a=Warm(10,1,s);var b=a;float ratio=1;
        for(int i=0;i<180;i++) {
            float size=i<60?10-i/12f:5;
            var empty=Frame(size,ratio);var extra=empty;
            extra.LookFloat=new Vector2(i%2==0?1:-1,1);extra.VerticalOffset=-1;
            extra.Interest=new Vector2(.25f,0);
            var ca=Clip();if(i%3==0)ca.DialogueOrCinematic=true;
            var plain=Step(a,i<45?6.5f/60:0,empty,s,ca);
            var added=Step(b,i<45?6.5f/60:0,extra,s,ca);
            Check(plain.Valid&&added.Valid,"both state trajectories valid");
            Check(plain.Follow.State.Lookahead.Equals(added.Follow.State.Lookahead),"complete front state independent of extra channels");
            Check(plain.Follow.State.Goal.Equals(added.Follow.State.Goal),"base goal independent of extra channels");
            Near(plain.Follow.Horizontal.RawOffset,added.Follow.Horizontal.RawOffset,"front raw offset unchanged");
            if(ca.DialogueOrCinematic) {
                Near(added.Follow.Channels.LookFloat.x,0,"gated final float X zero");
                Near(added.Follow.Channels.LookFloat.y,0,"gated final float Y zero");
                Near(added.Follow.Channels.Interest.x,0,"existing interest gate preserved");
                Near(added.Follow.Channels.Vertical.y,-3*size/10,"vertical not silently gated");
            }
            a=plain.Follow.State;b=added.Follow.State;ratio=plain.Ratio;
        }
    }
    private static void Legacy()
    {
        var s=Settings();var old=StreetFollowPipeline.Initialize(Vector3.zero,2,0);var current=old;
        var frame=Frame();frame.DynamicZoomEnabled=false;
        for(int i=0;i<720;i++) {
            float dx=i<180?6.5f/60:i<360?0:i<540?-6.5f/60:0;
            var input=Input(old,dx);
            var expected=StreetFollowPipeline.Evaluate(old,input,s,Clip());
            var actual=StreetFollowComposition.Evaluate(current,input,s,Clip(),frame);
            Check(actual.Valid&&expected.Valid,"legacy valid");
            Check(actual.Follow.Equals(expected),"complete legacy result exact field equality");
            old=expected.State;current=actual.Follow.State;
        }
    }
    private static void Lens()
    {
        var zoomSettings=new StreetDynamicZoomSettings { Enabled=true, MinSize=5,MaxSize=10,
            ExpandTime=1,ShrinkTime=1,ExpandCurve=StreetCurvePreset.Linear,ShrinkCurve=StreetCurvePreset.Linear };
        var z=StreetDynamicZoom.Evaluate(default,new StreetDynamicZoomInput {Moving=true,DeltaTime=1},zoomSettings);
        z=StreetDynamicZoom.Evaluate(z.State,new StreetDynamicZoomInput {Moving=true,DeltaTime=.5},zoomSettings);
        var s=Settings();var r=Step(StreetFollowPipeline.Initialize(Vector3.zero,2,0),0,Frame(z.Size.Value),s);
        Near(r.Ratio,.75,"actual zoom output consumed before geometry");
        Near(r.EffectiveSettings.Lookahead,s.Lookahead*.75,"front distance scaled");
        Near(r.EffectiveSettings.MaxHorizontalOffset,s.MaxHorizontalOffset*.75,"horizontal cap scaled");
        Near(r.EffectiveSettings.MaxVerticalOffset,s.MaxVerticalOffset*.75,"vertical cap scaled");
        Near(r.EffectiveSettings.RecenterSpeed,s.RecenterSpeed,"return speed unchanged");
        Near(r.EffectiveSettings.RecenterRampTime,s.RecenterRampTime,"return ramp unchanged");
        Near(r.EffectiveSettings.LookaheadTime,s.LookaheadTime,"front timing unchanged");
        Near(r.EffectiveSettings.MoveSpeedThreshold,s.MoveSpeedThreshold,"speed threshold unchanged");
        Near(r.EffectiveSettings.DeadZoneWidth,s.DeadZoneWidth,"deadzone ratio unchanged");
    }
    private static void Guards()
    {
        var s=Settings();var state=Warm(10,1,s);var frame=Frame(5);
        var held=Input(state,0,0);
        var r=StreetFollowComposition.Evaluate(state,held,s,Clip(),frame);
        Check(r.Valid,"held time valid");Near(r.Follow.Target.x,state.Target.x,"held target unchanged");
        Near(r.Follow.State.Lookahead.LastTarget-state.PreviousFoot.x,
            (state.Lookahead.LastTarget-state.PreviousFoot.x)*.5,"held ratio bookkeeping matches stored ratio");
        frame=Frame();frame.LookFloat=new Vector2(2,0);
        Check(!Step(state,0,frame,s).Valid,"non-normalized float rejected");
        frame=Frame();frame.PreviousRatio=0;Check(!Step(state,0,frame,s).Valid,"missing previous ratio rejected");
        frame=Frame();frame.Size=float.NaN;Check(!Step(state,0,frame,s).Valid,"invalid size rejected");
        frame=Frame();frame.MaxSize=0;Check(!Step(state,0,frame,s).Valid,"zero maximum rejected");
        frame=Frame();frame.Interest=new Vector2(float.NaN,0);Check(!Step(state,0,frame,s).Valid,"invalid channel rejected");
    }

    private static void WorldFloat()
    {
        var s=Settings();var state=StreetFollowPipeline.Initialize(Vector3.zero,2,0);
        var frame=Frame(20);frame.LookFloat=new Vector2(1,1);
        frame.LookFloatIsWorld=true;frame.WorldLookFloat=new Vector2(1.5f,-.5f);
        var r=Step(state,0,frame,s);Check(r.Valid,"world float valid");Near(r.Ratio,2,"ratio unchanged for other channels");
        Near(r.Follow.Channels.LookFloat.x,1.5,"world X not rescaled");Near(r.Follow.Channels.LookFloat.y,-.5,"world Y not rescaled");
        Near(r.EffectiveSettings.MaxHorizontalOffset,20,"shared total cap remains independent");
        frame.LookFloatIsWorld=false;r=Step(state,0,frame,s);
        Near(r.Follow.Channels.LookFloat.x,20,"legacy normalized X unchanged");Near(r.Follow.Channels.LookFloat.y,6,"legacy normalized Y unchanged");
        frame.LookFloatIsWorld=true;frame.WorldLookFloat=new Vector2(float.NaN,0);
        Check(!Step(state,0,frame,s).Valid,"invalid world input rejected");
    }

}
