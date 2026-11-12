using System;
using System.Reflection;
using EatWhat.LongLoop;
using EatWhat.LongLoop.Configuration;
using UnityEngine;

internal static class StreetFollowPipelineTests
{
    private static int assertions,scenarios;
    private static readonly System.Collections.Generic.List<string> failures=new System.Collections.Generic.List<string>();
    private static void Check(bool value,string label) {assertions++;if(!value)throw new Exception(label);}
    private static void Near(double value,double expected,string label,double tolerance=1e-5)
        => Check(!double.IsNaN(value)&&Math.Abs(value-expected)<=tolerance,label+": "+value+" != "+expected);
    private static void Scenario(string name,Action test)
    {
        scenarios++;
        try {test();Console.WriteLine("SCENARIO PASS "+name);}
        catch(Exception error) {failures.Add(name+": "+error);Console.Error.WriteLine("SCENARIO FAIL "+name+": "+error);}
    }
    private static StreetFollowSettings Settings()=>new StreetFollowSettings {
        DeadZoneWidth=.15f,DeadZoneHeight=.2f,Lookahead=4.5f,LookaheadTime=2,MaxHorizontalOffset=100,
        MaxVerticalOffset=100,MoveSpeedThreshold=.5f,RecenterDelay=0,RecenterSpeed=4,RecenterRampTime=.25f,
        RecenterCurve=StreetCurvePreset.SmoothStep};
    private static StreetFollowInput Input(double time,Vector3 foot,double dt=.01)
        => new StreetFollowInput {Time=time,DeltaTime=dt,Foot=foot,Focus=2,VisibleSize=new Vector2(320f/9f,20)};
    private static FollowClipContext Clip()=>new FollowClipContext {Orthographic=true,ProjectionKnown=true,
        ScreenRight=Vector3.right,OrthoSize=10,Aspect=16f/9f,SafeZoneWidth=1,GateOffsetsInDialogue=true};
    private static StreetFollowResult Evaluate(StreetFollowState state,StreetFollowInput input,StreetFollowSettings settings)
        => StreetFollowPipeline.Evaluate(state,input,settings,Clip());
    private static StreetFollowState Moving(int fps,int direction,double seconds,StreetFollowSettings settings)
    {
        var state=StreetFollowPipeline.Initialize(Vector3.zero,2,0);
        for(int i=1;i<=(int)Math.Round(seconds*fps);i++)
        {
            double t=(double)i/fps;var r=Evaluate(state,Input(t,Vector3.right*(float)(direction*6.5*t),1d/fps),settings);
            Check(r.Valid,"moving evaluation valid");state=r.State;
        }
        return state;
    }
    public static int Main()
    {
        Scenario("OriginalConfigurationDefaultsAndEvents",()=>Check(StreetConfigLogicTests.Main()==0,"original config regression"));
        Scenario("NewConfigurationDefaultsAndChangedChain",Configuration);
        Scenario("DeadZoneHoldAndPush",DeadZone);
        Scenario("DelayZeroAndPositiveStart",Delay);
        Scenario("DefinedVelocityCurvesAndShortTrips",Curves);
        Scenario("IndependentAxisReturnBitIdentity",IndependentAxes);
        Scenario("ActorRelativeLookaheadAcrossWidthsAndFps",SteadyLookahead);
        Scenario("EvaluateCapExcludesDeadzoneReferenceDifference",CapReference);
        Scenario("OldProposalCounterexamples",Counterexamples);
        Scenario("SignedReversalStopAndRestartContinuity",Transitions);
        Scenario("CenteredBaseResidualStillExits",CenteredResidual);
        Scenario("ZeroTimeHumanDecisionAndPositivePole",ZeroTime);
        Scenario("ClockTeleportAndInvalidInputProtection",Guards);
        Scenario("ExistingSafetyAndDialogueCasesMigrated",ClippingMigration);
        Scenario("TargetSafetyAndConfiguration",TargetSafety);
        Scenario("DeadzoneThresholdZeroWidthAndViewChanges",DirectionalBoundary);
        Console.WriteLine((failures.Count==0?"PASS ":"FAIL ")+scenarios+" named scenarios; "+assertions+" assertions/frame observations; failed="+failures.Count+"; pure target source, no native execution");
        return failures.Count==0?0:1;
    }
    private static void DirectionalBoundary()
    {
        var s=Settings();s.DeadZoneWidth=.25f;
        foreach(int direction in new[]{-1,1})foreach(int fps in new[]{30,60,120})
        {
            var state=StreetFollowPipeline.Initialize(Vector3.zero,2,0);
            for(int i=1;i<=fps;i++)
            {
                double t=(double)i/fps;var input=Input(t,Vector3.right*(float)(direction*t),1d/fps);input.VisibleSize=new Vector2(8,20);
                var r=Evaluate(state,input,s);Check(r.Valid,"boundary series valid");
                if(i<fps){Near(r.Target.x,0,"unqualified base does not leak");Near(r.State.Lookahead.SignedWeight,0,"weight cannot warm up inside");}
                else Check(r.State.Lookahead.Triggered,"representable exact boundary inclusive");
                state=r.State;
            }
        }
        var initial=StreetFollowPipeline.Initialize(Vector3.zero,2,0);
        var pureY=Evaluate(initial,Input(.1,Vector3.up),s);
        Check(!pureY.State.Lookahead.Triggered&&pureY.State.Lookahead.Direction==0,"pure vertical never grants horizontal qualification");
        var slow=Evaluate(initial,Input(1,Vector3.right*.25f),s);
        Check(slow.State.Lookahead.Direction==0,"below movement threshold cannot grant direction");
        s.DeadZoneWidth=0;var immediate=Evaluate(initial,Input(.1,Vector3.right*.1f),s);
        Check(immediate.State.Lookahead.Triggered,"zero width qualifies on actual horizontal movement");
        s.DeadZoneWidth=.5f;
        var inside=Evaluate(initial,Input(.1,Vector3.right),s);Check(!inside.State.Lookahead.Triggered,"wide view still inside");
        var changed=Input(.2,Vector3.right*1.1f);changed.VisibleSize=new Vector2(4,20);
        var edge=Evaluate(inside.State,changed,s);Check(edge.State.Lookahead.Triggered,"viewport shrink uses current boundary");
    }
    private static void TargetSafety()
    {
        var s=Settings();s.Lookahead=6;s.MaxHorizontalOffset=2;s.DeadZoneWidth=0;s.RecenterDelay=.8f;
        foreach(int fps in new[]{30,60,120})
        {
            var state=StreetFollowPipeline.Initialize(Vector3.zero,2,0);var clip=Clip();clip.SafeZoneWidth=.1f;
            for(int i=1;i<=fps*16;i++)
            {
                double t=(double)i/fps;var input=Input(t,Vector3.right*(float)(6.5*t),1d/fps);
                var r=StreetFollowPipeline.Evaluate(state,input,s,clip);
                Check(r.Valid,"target safety valid");
                Check(r.Target.x-input.Foot.x<=clip.OrthoSize*clip.Aspect*clip.SafeZoneWidth+.0001,"target remains in safety interval");
                state=r.State;
            }
            Near(state.Target.x-state.PreviousFoot.x,clip.OrthoSize*clip.Aspect*clip.SafeZoneWidth,"safety truncates lead",.001);
            double offset=state.Target.x-state.PreviousFoot.x;s.RecenterDelay=1f;
            var changed=StreetFollowPipeline.Evaluate(state,Input(16.01,Vector3.right*104,.01),s,clip);
            Check(changed.Valid,"changed snapshot valid");Near(changed.Target.x-changed.State.PreviousFoot.x,offset,"config change during delay retains offset",.0001);
            var warp=StreetFollowPipeline.Evaluate(changed.State,Input(16.02,Vector3.right*1000,.01),s,clip);
            Check(warp.Valid&&warp.GoalStep.Status==FollowStepStatus.TeleportReset,"real warp resets relative state");
            Near(warp.Target.x-warp.State.PreviousFoot.x,0,"warp target resets to follow");
        }
    }
    private static void Configuration()
    {
        var cfg=new CameraFollowConfig();Near(cfg.RecenterRampTime,.25,"new ramp default");
        Check(cfg.RecenterCurve==StreetCurvePreset.SmoothStep,"new curve default");
        Check((int)StreetCurvePreset.Linear==0&&(int)StreetCurvePreset.Quadratic==1&&
            (int)StreetCurvePreset.SmoothStep==2&&(int)StreetCurvePreset.EaseInOut==3,"existing enum numbering preserved");
        StreetConfigs.Unbind();StreetConfigs.Bind(new DepthScaleConfig(),new CameraModeConfig(),cfg,new SceneBoundsConfig(),new Transform(),new Collider2D());
        int local=0,global=0;Action handler=()=>global++;cfg.Changed+=()=>local++;StreetConfigs.Changed+=handler;
        cfg.RecenterRampTime=.5f;cfg.RecenterRampTime=.5f;cfg.RecenterCurve=StreetCurvePreset.Quadratic;
        Near(local,2,"public setters notify once per change");Near(global,2,"changed reaches existing StreetConfigs chain");
        typeof(CameraFollowConfig).GetField("recenterRampTime",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(cfg,.75f);
        var validate=typeof(StreetConfigAsset).GetMethod("OnValidate",BindingFlags.Instance|BindingFlags.NonPublic);
        validate.Invoke(cfg,null);validate.Invoke(cfg,null);Near(local,3,"Inspector edit dedup");Near(global,3,"Inspector edit reaches chain");
        typeof(CameraFollowConfig).GetField("recenterCurve",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(cfg,StreetCurvePreset.EaseInOut);
        validate.Invoke(cfg,null);validate.Invoke(cfg,null);Near(local,4,"Inspector curve edit dedup");Near(global,4,"Inspector curve reaches chain");
        StreetConfigs.Changed-=handler;StreetConfigs.Unbind();
    }
    private static void DeadZone()
    {
        var s=Settings();s.Lookahead=0;s.DeadZoneWidth=.1f;
        var p=StreetFollowPipeline.FollowPoint(new Vector3(1,3,7),2);Check(p.Equals(new Vector3(1,5,7)),"explicit foot plus focus once");
        var state=StreetFollowPipeline.Initialize(Vector3.zero,2,0);
        var input=Input(.1,new Vector3(.4f,.5f,0));input.VisibleSize=new Vector2(10,10);
        var a=StreetFollowPipeline.StepGoal(state,input,s);Near(a.State.Goal.x,0,"inside X holds");Near(a.State.Goal.y,2,"inside Y holds");
        input=Input(.2,new Vector3(.8f,1.4f,0));input.VisibleSize=new Vector2(10,10);
        var b=StreetFollowPipeline.StepGoal(a.State,input,s);Near(b.State.Goal.x,.3,"X pushed to edge");Near(b.State.Goal.y,2.4,"Y pushed to edge");
    }
    private static void Delay()
    {
        var s=Settings();s.Lookahead=0;s.RecenterDelay=.5f;
        var state=StreetFollowPipeline.Initialize(Vector3.zero,2,0);state.Goal.x=-2;state.Target=state.Goal;
        var a=StreetFollowPipeline.StepGoal(state,Input(.49,Vector3.zero),s);Near(a.State.Goal.x,-2,"before delay holds");
        var b=StreetFollowPipeline.StepGoal(a.State,Input(.55,Vector3.zero),s);Near(b.RecenterDelta,.05,"cross-delay uses only eligible time",1e-7);
        Check(b.State.Goal.x> -2&&b.State.X.Time>0,"positive delay starts profile");
        s.RecenterDelay=0;var c=StreetFollowPipeline.StepGoal(state,Input(.01,Vector3.zero),s);
        Near(c.RecenterDelta,.01,"zero delay first stopped step");Check(c.State.Goal.x> -2&&c.State.X.Time==.01,"no damping/build-derived wait");
        var unchanged=s;unchanged.LookaheadTime=200;
        var d=StreetFollowPipeline.StepGoal(state,Input(.01,Vector3.zero),unchanged);
        Check(c.State.Goal.Equals(d.State.Goal),"build time cannot delay recenter");
    }
    private static void Curves()
    {
        foreach(StreetCurvePreset curve in Enum.GetValues(typeof(StreetCurvePreset)))
        {
            double last=0;Near(StreetFollowPipeline.CurveValue(curve,0),0,"curve starts zero");Near(StreetFollowPipeline.CurveValue(curve,1),1,"curve ends one");
            for(int i=0;i<=1000;i++){double c=StreetFollowPipeline.CurveValue(curve,i/1000d);Check(c>=last&&c<=1,"finite monotone speed curve");last=c;}
            var s=Settings();s.RecenterCurve=curve;
            foreach(int fps in new[]{30,60,120})
            {
                var axis=new RecenterAxisState();double x=5,peak=0,previous=x;bool cruise=false;
                for(int i=0;i<fps*3;i++)
                {
                    double progress;x=StreetFollowPipeline.StepAxis(ref axis,x,0,1d/fps,s,out progress);
                    double speed=StreetFollowPipeline.AxisSpeed(axis);peak=Math.Max(peak,speed);
                    Check(x<=previous&&x>=0&&progress>=0&&progress<=1,"monotone bounded target return");previous=x;
                    cruise|=axis.Time>axis.Ramp&&axis.Time<axis.Ramp+axis.Cruise&&speed==s.RecenterSpeed;
                }
                Near(x,0,"exact return endpoint");Near(StreetFollowPipeline.AxisSpeed(axis),0,"zero terminal speed");
                Check(cruise&&peak<=s.RecenterSpeed,"long trip cruises without stretching acceleration");
                Near(axis.Ramp,.25,"fixed long-trip ramp");
                Console.WriteLine("RETURN curve="+curve+" fps="+fps+" ramp="+axis.Ramp+" cruise="+axis.Cruise+" peak="+peak+" final="+x);
            }
            var tiny=new RecenterAxisState();double progressTiny;
            double end=StreetFollowPipeline.StepAxis(ref tiny,1e-10,0,1,s,out progressTiny);
            Near(end,0,"tiny distance exact");Check(tiny.Ramp<.25&&tiny.Peak<=s.RecenterSpeed,"short trip shrinks ramp and peak");
            s.RecenterRampTime=0;var zero=new RecenterAxisState();
            Near(StreetFollowPipeline.StepAxis(ref zero,1,0,.1,s,out progressTiny),.6,"zero ramp is bounded cruise",1e-7);
            s.RecenterSpeed=0;double before=.6;Near(StreetFollowPipeline.StepAxis(ref zero,before,0,10,s,out progressTiny),before,"zero speed disables active return");
        }
    }
    private static void IndependentAxes()
    {
        foreach(int fps in new[]{30,60,120})
        {
            var s=Settings();s.Lookahead=0;s.DeadZoneWidth=s.DeadZoneHeight=1;
            var a=StreetFollowPipeline.Initialize(Vector3.zero,2,0);a.Goal=new Vector3(-5,-3,0);a.Target=a.Goal;
            var b=a;b.Goal.x=-50;b.Target=b.Goal;var c=a;c.Goal.y=-50;c.Target=c.Goal;
            bool xSame=true,ySame=true;
            for(int i=1;i<=fps*5;i++)
            {
                var input=Input((double)i/fps,Vector3.zero,1d/fps);input.VisibleSize=new Vector2(1000,1000);
                a=StreetFollowPipeline.StepGoal(a,input,s).State;b=StreetFollowPipeline.StepGoal(b,input,s).State;c=StreetFollowPipeline.StepGoal(c,input,s).State;
                ySame&=a.Goal.y.Equals(b.Goal.y)&&a.Y.Equals(b.Y);xSame&=a.Goal.x.Equals(c.Goal.x)&&a.X.Equals(c.X);
            }
            Check(xSame&&ySame,"other axis distance does not change trajectory or duration, bit for bit");
        }
    }
    private static void SteadyLookahead()
    {
        foreach(int fps in new[]{30,60,120})foreach(int dir in new[]{1,-1})foreach(float width in new[]{0f,.15f,.9f})
        {
            var s=Settings();s.DeadZoneWidth=width;s.LookaheadTime=.5f;
            var state=Moving(fps,dir,8,s);Near(dir*(state.Target.x-state.PreviousFoot.x),s.Lookahead,"steady target lead independent of deadzone",1e-4);
            Console.WriteLine("STEADY fps="+fps+" dir="+dir+" width="+width+" foot="+state.PreviousFoot.x+" base="+state.Goal.x+" target="+state.Target.x+" weight="+state.Lookahead.SignedWeight);
        }
    }
    private static void Counterexamples()
    {
        double goal= -2,follow=0,d=4.5,w=.999;
        double before=goal+(follow+d-goal)*w,after=goal+(follow-d-goal)*w;
        Check(Math.Abs(after-before)>8.9,"old unsigned-weight sign flip is a real discontinuity");
        double centeredExitJump=d;Check(centeredExitJump==4.5,"base distance zero cannot define lookahead exit progress");
        Console.WriteLine("COUNTEREXAMPLE source=original-card-formula unsignedSignFlipJump="+Math.Abs(after-before)+" centeredBaseNaiveExitJump="+centeredExitJump+" notOldRuntimeRerun=true");
    }
    private static void CapReference()
    {
        foreach(int fps in new[]{30,60,120})foreach(int dir in new[]{1,-1})
        foreach(float width in new[]{0f,.15f,.9f})foreach(float distance in new[]{2.5f,4.5f})
        {
            var s=Settings();s.DeadZoneWidth=width;s.Lookahead=distance;s.MaxHorizontalOffset=2.5f;s.LookaheadTime=.5f;
            var state=Moving(fps,dir,8,s);
            Near(dir*(state.Target.x-state.PreviousFoot.x),2.5,"Evaluate cap applies to lookahead, not deadzone reference change",1e-4);
            Console.WriteLine("CAP_REFERENCE fps="+fps+" dir="+dir+" width="+width+" lookahead="+distance+" cap=2.5 lead="+(dir*(state.Target.x-state.PreviousFoot.x)));
        }
    }
    private static void Transitions()
    {
        const double h=1e-5;
        foreach(int fps in new[]{30,60,120})foreach(int dir in new[]{1,-1})
        {
            var s=Settings();var state=Moving(fps,dir,8,s);var before=state.Target;
            var reverse=Evaluate(state,Input(state.LastTime+h,state.PreviousFoot-Vector3.right*(float)(dir*6.5*h),h),s);
            Check(reverse.Valid&&Math.Abs(reverse.Target.x-before.x)<.01,"signed reversal no full-distance jump");
            var stop=Evaluate(state,Input(state.LastTime+h,state.PreviousFoot,h),s);
            Check(stop.Valid&&Math.Abs(stop.Target.x-before.x)<.001,"stop starts from complete target");
            double firstStopDelta=stop.Target.x-before.x;
            for(int i=1;i<=fps/4;i++)stop=Evaluate(stop.State,Input(state.LastTime+h+(double)i/fps,state.PreviousFoot,1d/fps),s);
            var restart=Evaluate(stop.State,Input(stop.State.LastTime+h,stop.State.PreviousFoot+Vector3.right*(float)(dir*6.5*h),h),s);
            Check(restart.Valid&&Math.Abs(restart.Target.x-stop.Target.x)<.01,"restart carries current target difference");
            Console.WriteLine("JOIN fps="+fps+" dir="+dir+" h="+h+" reverseDelta="+(reverse.Target.x-before.x)+" firstStopDelta="+firstStopDelta+" afterQuarterSecondDelta="+(stop.Target.x-before.x)+" restartDelta="+(restart.Target.x-stop.Target.x));
            s.RecenterDelay=.5f;var delayed=Evaluate(state,Input(state.LastTime+.25,state.PreviousFoot,.25),s);
            Near(delayed.Target.x,state.Target.x,"pre-delay stopped lookahead is held",1e-4);
        }
    }
    private static void CenteredResidual()
    {
        foreach(int fps in new[]{30,60,120})
        {
            var s=Settings();s.DeadZoneWidth=0;s.LookaheadTime=.5f;var state=Moving(fps,1,3,s);
            Near(state.Goal.x,state.PreviousFoot.x,"base already centered");Check(state.Lookahead.SignedWeight>.99,"nonzero residual lookahead");
            float start=state.Target.x;double stopAt=state.LastTime;bool saw=false;
            for(int i=1;i<=fps*3;i++)
            {
                var next=Evaluate(state,Input(stopAt+(double)i/fps,state.PreviousFoot,1d/fps),s);
                Check(next.Valid&&next.Target.x<=state.Target.x+.0001,"centered-base exit continuously approaches center");
                saw|=next.Target.x<start-.01;state=next.State;
            }
            Check(saw,"residual creates effective exit progress");Near(state.Target.x,state.PreviousFoot.x,"residual eventually zero");Near(state.Lookahead.SignedWeight,0,"weight reaches zero");
        }
    }
    private static void ZeroTime()
    {
        var s=Settings();s.LookaheadTime=0;var state=Moving(60,1,1,s);var before=state.Target;
        var reverse=Evaluate(state,Input(state.LastTime+1e-6,state.PreviousFoot-Vector3.right*6.5e-6f,1e-6),s);
        Check(Math.Abs(reverse.Target.x-before.x)<.002,"human zero-time finite-pole reversal stays continuous");
        Near(StreetFollowPipeline.ZeroLookaheadTimeConstant,1d/60,"explicit human zero pole",1e-12);
        s.LookaheadTime=2;var initial=StreetFollowPipeline.Initialize(Vector3.zero,2,0);
        var r=Evaluate(initial,Input(2,Vector3.right*13,2),s);Near(r.State.Lookahead.SignedWeight,1-Math.Exp(-3),"positive time retains exact time/3",1e-12);
        s.LookaheadTime=float.NaN;Check(!Evaluate(initial,Input(.1,Vector3.zero),s).Valid,"nonfinite time rejected");
    }
    private static void Guards()
    {
        var s=Settings();var state=Moving(60,1,1,s);
        foreach(var input in new[]{Input(state.LastTime,state.PreviousFoot,.1),Input(state.LastTime-1,state.PreviousFoot,.1),Input(state.LastTime+1,Vector3.right*10000,0)})
        {var r=Evaluate(state,input,s);Check(r.Valid&&r.State.Equals(state)&&r.Target.Equals(state.Target),"nonadvancing time holds complete state/output");}
        var warp=Evaluate(state,Input(state.LastTime+.1,Vector3.right*10000,.1),s);
        Check(warp.Valid&&warp.GoalStep.Status==FollowStepStatus.TeleportReset,"one-screen warp resets");
        Near(warp.Target.x,10000,"warp target equals follow");Near(warp.State.Lookahead.SignedWeight,0,"warp clears lookahead");
        var invalid=Input(state.LastTime+.1,state.PreviousFoot);invalid.Time=double.NaN;
        Check(!Evaluate(state,invalid,s).Valid,"NaN clock rejects");
        var initial=StreetFollowPipeline.Initialize(Vector3.zero,2,0);
        Check(!Evaluate(initial,Input(1e-300,Vector3.right*.1f,1e-300),s).Valid,"derived nonrepresentable foot velocity rejects");
        s.RecenterCurve=(StreetCurvePreset)99;Check(!Evaluate(state,Input(state.LastTime+.1,state.PreviousFoot),s).Valid,"undefined speed curve rejects");
    }
    private static void ClippingMigration()
    {
        // Frozen, explicit safety/gating expectations replace the deleted live
        // ChannelPipeline oracle. The original 60-pair comparison is archived.
        var golden=new[]{
            new[]{0f,20f,2f,7f,2f},new[]{0f,-20f,2f,-7f,2f},
            new[]{10f,-3f,2f,0f,2f},new[]{10f,3f,2f,3f,2f},
            new[]{-10f,3f,2f,0f,2f},new[]{-10f,-3f,2f,-3f,2f}};
        foreach(var row in golden)
        {
            var c=Clip();c.OrthoSize=10;c.Aspect=1;c.SafeZoneWidth=.7f;c.Foot=new Vector3(row[0],0,0);c.TargetReference=Vector3.zero;
            var output=StreetFollowPipeline.SumAndClip(c,new FollowContributions {Horizontal=new Vector2(row[1],0),Vertical=new Vector2(0,row[2])},float.MaxValue,float.MaxValue);
            Check(output.Valid,"migrated safety golden valid");Near(output.ClippedSum.x,row[3],"migrated safety golden X");Near(output.ClippedSum.y,row[4],"perpendicular component preserved");
        }
        var rotated=Clip();rotated.OrthoSize=10;rotated.Aspect=1;rotated.SafeZoneWidth=.7f;rotated.ScreenRight=new Vector3(0,1,0);
        var rotation=StreetFollowPipeline.SumAndClip(rotated,new FollowContributions {Horizontal=new Vector2(1,0),Vertical=new Vector2(0,20)},float.MaxValue,float.MaxValue);
        Near(rotation.ClippedSum.x,1,"rotated perpendicular unchanged");Near(rotation.ClippedSum.y,7,"rotated safe axis");
        var context=Clip();context.TargetReference=Vector3.zero;context.Foot=Vector3.zero;
        var contribution=new FollowContributions {Horizontal=new Vector2(4,0),Vertical=new Vector2(0,4),LookFloat=new Vector2(-3,-3)};
        var sum=StreetFollowPipeline.SumAndClip(context,contribution,2,2);Near(sum.RawSum.x,1,"horizontal shared cap after sum");Near(sum.RawSum.y,1,"vertical shared cap after sum");
        context.DialogueOrCinematic=true;sum=StreetFollowPipeline.SumAndClip(context,contribution,2,2);
        Near(sum.RawSum.x,2,"gated float cannot consume horizontal budget");Near(sum.RawSum.y,2,"gated float cannot consume vertical budget");
        foreach(float width in new[]{-.1f,1.1f,float.NaN,float.PositiveInfinity}){context.SafeZoneWidth=width;Check(!StreetFollowPipeline.SumAndClip(context,contribution,2,2).Valid,"invalid safe width");}
        context=Clip();context.Orthographic=false;Check(!StreetFollowPipeline.SumAndClip(context,contribution,2,2).Valid,"perspective rejected");
        context=Clip();context.ScreenRight=new Vector3(1,0,.2f);Check(!StreetFollowPipeline.SumAndClip(context,contribution,2,2).Valid,"unknown projection rejected");
        contribution.Horizontal.x=float.NaN;context=Clip();Check(!StreetFollowPipeline.SumAndClip(context,contribution,2,2).Valid,"nonfinite contribution rejected");
    }
}
