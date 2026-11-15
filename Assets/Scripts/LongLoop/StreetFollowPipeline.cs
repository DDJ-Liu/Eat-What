using System;
using EatWhat.LongLoop.Configuration;
using UnityEngine;

namespace EatWhat.LongLoop
{
    // Value snapshots supplied by the caller. No assets or scene objects are held here.
    public struct StreetFollowSettings
    {
        public float DeadZoneWidth, DeadZoneHeight, Lookahead, LookaheadTime;
        public float MaxHorizontalOffset, MaxVerticalOffset, MoveSpeedThreshold;
        public float RecenterDelay, RecenterSpeed, RecenterRampTime;
        public StreetCurvePreset RecenterCurve;
    }
    public struct StreetFollowInput
    {
        public Vector3 Foot;
        public float Focus;
        public Vector2 VisibleSize;
        public double Time, DeltaTime;
        public bool SuspendReturn;
    }
    public struct RecenterAxisState
    {
        public bool Active;
        public double Start, Target, Time, Ramp, Peak, Cruise, SpeedSetting, RampSetting;
        public StreetCurvePreset Curve;
        public double Duration => 2d*Ramp+Cruise;
    }
    public struct FollowLookaheadState
    {
        public bool HasTarget, Exiting;
        public bool Triggered;
        public int Direction;
        public double SignedWeight, Carry, LastTarget, ExitStartWeight;
        public RecenterAxisState Exit;
    }
    public struct StreetFollowState
    {
        public bool Initialized;
        public bool ReturnSuspended;
        public Vector3 Goal, Target, PreviousFoot;
        public double LastTime, StoppedSeconds;
        public RecenterAxisState X, Y;
        public FollowLookaheadState Lookahead;
    }
    public enum FollowStepStatus { Initialized, Advanced, HeldTime, TeleportReset, InvalidInput }
    public struct FollowGoalStep
    {
        public StreetFollowState State;
        public FollowStepStatus Status;
        public Vector3 Follow, BeforeRecenter;
        public Vector2 FootVelocity;
        public double Elapsed, RecenterDelta;
        public bool Moving;
        public bool ReturnSuspended, ReturnRestarted;
        public int HorizontalDirection;
        public bool HorizontalBoundary;
        public bool Advances => Status==FollowStepStatus.Advanced;
    }
    public struct FollowHorizontalResult
    {
        public FollowLookaheadState State;
        public double BaseOffset, RawOffset;
        public float Offset;
    }
    public struct FollowContributions
    { public Vector2 Horizontal, Vertical, Interest, LookFloat; }
    public struct FollowClipContext
    {
        public Vector3 Foot, TargetReference, ScreenRight;
        public float OrthoSize, Aspect, SafeZoneWidth;
        public bool Orthographic, ProjectionKnown, DialogueOrCinematic, GateOffsetsInDialogue;
    }
    public enum FollowClipInvalidReason { None, InvalidWidth, InvalidLens, InvalidProjection, InvalidState, Overflow }
    public struct FollowClipResult
    {
        public Vector2 Horizontal, Vertical, Interest, LookFloat, RawSum, ClippedSum;
        public bool Valid, GateApplied, Clipped, BaselineOutside;
        public FollowClipInvalidReason InvalidReason;
    }
    public struct StreetFollowResult
    {
        public bool Valid;
        public StreetFollowState State;
        public FollowGoalStep GoalStep;
        public FollowHorizontalResult Horizontal;
        public FollowClipResult Channels;
        public Vector3 Target;
    }

    public static class StreetFollowPipeline
    {
        public const double CmResidualTimeFactor=4.605170186;
        public static float ToCmDamping(float tau)=>(float)(tau*CmResidualTimeFactor);
        // Direct human decision, 2026-11-10: zero build time still has a finite,
        // continuous transition. Positive values retain their exact time/3 pole.
        public const double ZeroLookaheadTimeConstant=1d/60d;
        public static Vector3 FollowPoint(Vector3 foot,float focus)
            => new Vector3(foot.x,foot.y+focus,foot.z);

        public static StreetFollowState Initialize(Vector3 foot,float focus,double time)
        {
            Vector3 point=FollowPoint(foot,focus);
            if(!Finite(foot)||!Finite(point)||!Finite(time)||time<0) return default;
            return new StreetFollowState {Initialized=true,Goal=point,Target=point,PreviousFoot=foot,LastTime=time,
                Lookahead=new FollowLookaheadState {HasTarget=true,LastTarget=point.x}};
        }
        public static bool Valid(StreetFollowSettings s)
        {
            return Nonnegative(s.DeadZoneWidth)&&Nonnegative(s.DeadZoneHeight)&&s.DeadZoneWidth<=1&&s.DeadZoneHeight<=1&&
                Nonnegative(s.Lookahead)&&Nonnegative(s.LookaheadTime)&&Nonnegative(s.MaxHorizontalOffset)&&
                Nonnegative(s.MaxVerticalOffset)&&Nonnegative(s.MoveSpeedThreshold)&&Nonnegative(s.RecenterDelay)&&
                Nonnegative(s.RecenterSpeed)&&Nonnegative(s.RecenterRampTime)&&CurveDefined(s.RecenterCurve);
        }
        public static FollowGoalStep StepGoal(StreetFollowState previous,StreetFollowInput input,StreetFollowSettings s)
        {
            var result=new FollowGoalStep {State=previous,Status=FollowStepStatus.InvalidInput};
            var follow=FollowPoint(input.Foot,input.Focus);
            if(!Valid(s)||!Finite(input.Foot)||!Finite(follow)||!Finite(input.Time)||input.Time<0||
                !Finite(input.DeltaTime)||!Finite(input.VisibleSize)||input.VisibleSize.x<=0||input.VisibleSize.y<=0)
                return result;
            result.Follow=follow;
            if(!previous.Initialized)
            {result.State=Initialize(input.Foot,input.Focus,input.Time);result.State.ReturnSuspended=input.SuspendReturn;result.Status=FollowStepStatus.Initialized;return result;}
            if(!Finite(previous.Goal)||!Finite(previous.Target)||!Finite(previous.PreviousFoot)||
                !Finite(previous.LastTime)||!Finite(previous.StoppedSeconds)||previous.StoppedSeconds<0)
                return result;
            result.Status=FollowStepStatus.HeldTime;
            if(input.DeltaTime<=0 || input.Time<=previous.LastTime) return result;
            double dt=input.Time-previous.LastTime;
            double dx=(double)input.Foot.x-previous.PreviousFoot.x,dy=(double)input.Foot.y-previous.PreviousFoot.y;
            double screen=Math.Max(input.VisibleSize.x,input.VisibleSize.y);
            if(dx*dx+dy*dy>screen*screen)
            {result.State=Initialize(input.Foot,input.Focus,input.Time);result.State.ReturnSuspended=input.SuspendReturn;result.Status=FollowStepStatus.TeleportReset;return result;}
            var next=previous;
            double vx=dx/dt,vy=dy/dt;
            if(!Representable(vx)||!Representable(vy)) {result.Status=FollowStepStatus.InvalidInput;return result;}
            result.Elapsed=dt;result.Moving=Math.Sqrt(dx*dx+dy*dy)/dt>s.MoveSpeedThreshold;
            result.FootVelocity=new Vector2((float)vx,(float)vy);
            result.HorizontalDirection=Math.Abs(vx)>s.MoveSpeedThreshold?Math.Sign(vx):0;
            // Test the current boundary against the existing base goal, before
            // Push changes it. Compare representable positions, including equality.
            double halfWidth=.5d*input.VisibleSize.x*s.DeadZoneWidth;
            float edge=(float)(previous.Goal.x+result.HorizontalDirection*halfWidth);
            result.HorizontalBoundary=result.HorizontalDirection!=0&&
                result.HorizontalDirection*((double)follow.x-edge)>=0;
            double oldStopped=next.StoppedSeconds;
            result.ReturnSuspended=input.SuspendReturn;
            result.ReturnRestarted=previous.ReturnSuspended&&!input.SuspendReturn;
            next.ReturnSuspended=input.SuspendReturn;
            if(result.ReturnRestarted){oldStopped=0;next.X=default;next.Y=default;}
            next.StoppedSeconds=result.Moving?0:input.SuspendReturn?oldStopped:oldStopped+dt;
            next.Goal=new Vector3(Push(next.Goal.x,follow.x,.5d*input.VisibleSize.x*s.DeadZoneWidth),
                Push(next.Goal.y,follow.y,.5d*input.VisibleSize.y*s.DeadZoneHeight),follow.z);
            result.BeforeRecenter=next.Goal;
            if(result.Moving) {next.X=default;next.Y=default;}
            else if(!input.SuspendReturn&&s.RecenterSpeed>0)
            {
                result.RecenterDelta=Math.Min(dt,Math.Max(0,next.StoppedSeconds-s.RecenterDelay)-Math.Max(0,oldStopped-s.RecenterDelay));
                if(result.RecenterDelta>0)
                {
                    double progress;
                    next.Goal.x=(float)StepAxis(ref next.X,next.Goal.x,follow.x,result.RecenterDelta,s,out progress);
                    next.Goal.y=(float)StepAxis(ref next.Y,next.Goal.y,follow.y,result.RecenterDelta,s,out progress);
                }
            }
            next.PreviousFoot=input.Foot;next.LastTime=input.Time;
            result.State=next;result.Status=FollowStepStatus.Advanced;
            return result;
        }

        private static float Push(float goal,float follow,double half)
        {double gap=(double)follow-goal;return gap>half?(float)(follow-half):gap< -half?(float)(follow+half):goal;}

        public static double StepAxis(ref RecenterAxisState axis,double current,double target,double dt,
            StreetFollowSettings s,out double progress)
        {
            progress=0;
            if(dt<=0||s.RecenterSpeed<=0) return current;
            if(!axis.Active||axis.Target!=target||axis.SpeedSetting!=s.RecenterSpeed||
                axis.RampSetting!=s.RecenterRampTime||axis.Curve!=s.RecenterCurve)
            {
                axis=CreateRecenterAxis(current,target,s);
            }
            double d=Math.Abs(axis.Target-axis.Start),duration=axis.Duration;
            axis.Time=Math.Min(duration,axis.Time+dt);
            if(d==0||axis.Time>=duration) {progress=1;return target;}
            double travel;
            if(axis.Ramp==0) travel=axis.Peak*axis.Time;
            else if(axis.Time<axis.Ramp)
                travel=axis.Peak*axis.Ramp*CurveIntegral(axis.Curve,axis.Time/axis.Ramp);
            else if(axis.Time<axis.Ramp+axis.Cruise)
                travel=axis.Peak*(axis.Ramp*CurveIntegral(axis.Curve,1)+axis.Time-axis.Ramp);
            else
            {
                double u=(axis.Time-axis.Ramp-axis.Cruise)/axis.Ramp;
                travel=axis.Peak*(axis.Ramp*CurveIntegral(axis.Curve,1)+axis.Cruise+
                    axis.Ramp*(u-CurveIntegral(axis.Curve,u)));
            }
            travel=Clamp(travel,0,d);progress=travel/d;
            return axis.Start+Math.Sign(axis.Target-axis.Start)*travel;
        }
        public static double AxisSpeed(RecenterAxisState axis)
        {
            if(!axis.Active||axis.Time>=axis.Duration) return 0;
            if(axis.Ramp==0) return axis.Peak;
            if(axis.Time<axis.Ramp) return axis.Peak*CurveValue(axis.Curve,axis.Time/axis.Ramp);
            if(axis.Time<axis.Ramp+axis.Cruise) return axis.Peak;
            return axis.Peak*(1-CurveValue(axis.Curve,(axis.Time-axis.Ramp-axis.Cruise)/axis.Ramp));
        }
        internal static RecenterAxisState CreateRecenterAxis(double current,double target,StreetFollowSettings s)
        {
            double distance=Math.Abs(target-current);
            double ramp=Math.Min(s.RecenterRampTime,Math.Sqrt(distance*s.RecenterRampTime/s.RecenterSpeed));
            double peak=ramp>0?Math.Min(s.RecenterSpeed,distance/ramp):s.RecenterSpeed;
            return new RecenterAxisState {Active=true,Start=current,Target=target,Ramp=ramp,Peak=peak,
                Cruise=peak>0?Math.Max(0,distance/peak-ramp):0,SpeedSetting=s.RecenterSpeed,
                RampSetting=s.RecenterRampTime,Curve=s.RecenterCurve};
        }
        public static bool CurveDefined(StreetCurvePreset curve)=>curve==StreetCurvePreset.Linear||
            curve==StreetCurvePreset.Quadratic||curve==StreetCurvePreset.SmoothStep||curve==StreetCurvePreset.EaseInOut;
        public static double CurveValue(StreetCurvePreset curve,double u)
        {
            return EatWhat.Toolkit.NarrativeCurveBridge.Value((int)curve,u);
        }
        public static double CurveIntegral(StreetCurvePreset curve,double u)
        {
            return EatWhat.Toolkit.NarrativeCurveBridge.Integral((int)curve,u);
        }

        public static FollowHorizontalResult Horizontal(FollowLookaheadState previous,FollowGoalStep step,StreetFollowSettings s)
        {
            var h=previous;double goal=step.State.Goal.x,follow=step.Follow.x;
            double distance=Math.Min(s.Lookahead,s.MaxHorizontalOffset);
            if(step.Status==FollowStepStatus.Initialized||step.Status==FollowStepStatus.TeleportReset)
                h=new FollowLookaheadState {HasTarget=true,LastTarget=goal};
            else if(step.Advances)
            {
                if(!h.HasTarget) {h.HasTarget=true;h.LastTarget=step.BeforeRecenter.x;}
                if(step.ReturnRestarted&&!step.Moving)
                {
                    h.Exiting=false;h.Exit=default;
                    h.Carry=h.LastTarget-Blend(goal,follow,distance,h.SignedWeight);
                }
                if(step.Moving)
                {
                    if(step.HorizontalDirection!=0)
                    {
                        if(h.Direction!=step.HorizontalDirection)
                        {h.Direction=step.HorizontalDirection;h.Triggered=false;}
                        if(step.HorizontalBoundary) h.Triggered=true;
                    }
                    if(h.Exiting)
                    {
                        // Carry only an undamped target difference. This makes exit
                        // cancellation continuous without sampling any rendered pose.
                        h.Carry=h.LastTarget-Blend(goal,follow,distance,h.SignedWeight);
                        h.Exiting=false;h.Exit=default;
                    }
                    double desired=h.Triggered?step.HorizontalDirection:0;
                    double tau=s.LookaheadTime>0?s.LookaheadTime/3d:ZeroLookaheadTimeConstant;
                    double decay=Math.Exp(-step.Elapsed/tau);
                    h.SignedWeight=desired+(h.SignedWeight-desired)*decay;h.Carry*=decay;
                    h.LastTarget=Blend(goal,follow,distance,h.SignedWeight)+h.Carry;
                }
                else if(step.ReturnSuspended) { /* Keep horizontal exit position and progress. */ }
                else if(step.RecenterDelta>0)
                {
                    if(!h.Exiting)
                    {h.Exiting=true;h.Exit=default;h.ExitStartWeight=h.SignedWeight;h.Carry=0;}
                    double progress;
                    h.LastTarget=StepAxis(ref h.Exit,h.LastTarget,follow,step.RecenterDelta,s,out progress);
                    h.SignedWeight=h.ExitStartWeight*(1-progress);
                }
                else if(!h.Exiting)
                    h.LastTarget=Blend(goal,follow,distance,h.SignedWeight)+h.Carry;
            }
            // The actor-to-dead-zone interpolation is a target reference change,
            // not additional lookahead consuming the shared horizontal allowance.
            double raw=distance*h.SignedWeight;
            double baseOffset=h.HasTarget?h.LastTarget-goal-raw:0;
            return new FollowHorizontalResult {State=h,BaseOffset=baseOffset,RawOffset=raw,
                Offset=(float)Clamp(raw,-s.MaxHorizontalOffset,s.MaxHorizontalOffset)};
        }
        private static double Blend(double goal,double follow,double distance,double signedWeight)
            => goal+(follow-goal)*Math.Abs(signedWeight)+distance*signedWeight;

        // The live caller supplies zero in this release. Explicit scalar seams are
        // retained for channel composition tests and later separately reviewed producers.
        public static Vector2 Vertical()=>Vector2.zero;

        public static FollowClipResult SumAndClip(FollowClipContext ctx,FollowContributions input,
            float maximumHorizontal,float maximumVertical)
        {
            var r=new FollowClipResult();
            if(!Finite(ctx.SafeZoneWidth)||ctx.SafeZoneWidth<0||ctx.SafeZoneWidth>1)
                return Invalid(r,FollowClipInvalidReason.InvalidWidth);
            if(!ctx.Orthographic||!Finite(ctx.OrthoSize)||!Finite(ctx.Aspect)||ctx.OrthoSize<=0||ctx.Aspect<=0)
                return Invalid(r,FollowClipInvalidReason.InvalidLens);
            Vector3 right=ctx.ScreenRight;
            if(!ctx.ProjectionKnown||!Finite(right)||Math.Abs(right.z)>.0001||
                Math.Abs((double)right.x*right.x+(double)right.y*right.y-1)>.0001)
                return Invalid(r,FollowClipInvalidReason.InvalidProjection);
            if(!Finite(ctx.Foot)||!Finite(ctx.TargetReference)||!Finite(input.Horizontal)||!Finite(input.Vertical)||
                !Finite(input.Interest)||!Finite(input.LookFloat)||!Finite(maximumHorizontal)||maximumHorizontal<0||
                !Finite(maximumVertical)||maximumVertical<0)
                return Invalid(r,FollowClipInvalidReason.InvalidState);
            r.Horizontal=input.Horizontal;r.Vertical=input.Vertical;
            r.GateApplied=ctx.GateOffsetsInDialogue&&ctx.DialogueOrCinematic;
            r.Interest=r.GateApplied?Vector2.zero:input.Interest;r.LookFloat=r.GateApplied?Vector2.zero:input.LookFloat;
            double hx=(double)r.Horizontal.x+r.LookFloat.x,vy=(double)r.Vertical.y+r.LookFloat.y;
            if(!Representable(hx)||!Representable(vy)) return Invalid(r,FollowClipInvalidReason.Overflow);
            double x=Clamp(hx,-maximumHorizontal,maximumHorizontal)+r.Vertical.x+r.Interest.x;
            double y=Clamp(vy,-maximumVertical,maximumVertical)+r.Horizontal.y+r.Interest.y;
            if(!Representable(x)||!Representable(y)) return Invalid(r,FollowClipInvalidReason.Overflow);
            r.RawSum=new Vector2((float)x,(float)y);
            double p=((double)ctx.Foot.x-ctx.TargetReference.x)*right.x+((double)ctx.Foot.y-ctx.TargetReference.y)*right.y;
            double half=(double)ctx.OrthoSize*ctx.Aspect*ctx.SafeZoneWidth,shift=x*right.x+y*right.y;
            r.BaselineOutside=Math.Abs(p)>half;
            double lower=p>half?0:p-half,upper=p< -half?0:p+half,clipped=Clamp(shift,lower,upper);
            if(clipped!=shift)
            {
                double perpendicular=-x*right.y+y*right.x;
                x=clipped*right.x-perpendicular*right.y;y=clipped*right.y+perpendicular*right.x;
            }
            if(!Representable(x)||!Representable(y)) return Invalid(r,FollowClipInvalidReason.Overflow);
            r.ClippedSum=new Vector2((float)x,(float)y);r.Clipped=clipped!=shift;r.Valid=true;
            return r;
        }
        public static StreetFollowResult Evaluate(StreetFollowState previous,StreetFollowInput input,
            StreetFollowSettings settings,FollowClipContext clip,FollowContributions additional=default)
        {
            var r=new StreetFollowResult {State=previous,Target=previous.Target};
            r.GoalStep=StepGoal(previous,input,settings);
            if(r.GoalStep.Status==FollowStepStatus.InvalidInput) return r;
            if(r.GoalStep.Status==FollowStepStatus.HeldTime) {r.Valid=true;return r;}
            var next=r.GoalStep.State;
            r.Horizontal=Horizontal(previous.Lookahead,r.GoalStep,settings);next.Lookahead=r.Horizontal.State;
            double horizontal=r.Horizontal.RawOffset+additional.Horizontal.x;
            if(!Representable(horizontal)) return r;
            additional.Horizontal.x=(float)horizontal;
            // Production has no vertical-input, float, or interest producer yet.
            Vector2 vertical=Vertical();
            additional.Vertical=new Vector2(additional.Vertical.x+vertical.x,additional.Vertical.y+vertical.y);
            double baseX=next.Goal.x+r.Horizontal.BaseOffset;
            if(!Representable(baseX)) return r;
            var reference=new Vector3((float)baseX,next.Goal.y,next.Goal.z);
            clip.Foot=input.Foot;clip.TargetReference=reference;
            r.Channels=SumAndClip(clip,additional,settings.MaxHorizontalOffset,settings.MaxVerticalOffset);
            if(!r.Channels.Valid) return r;
            var target=reference+new Vector3(r.Channels.ClippedSum.x,r.Channels.ClippedSum.y,0);
            if(!Finite(target)) return r;
            next.Target=target;
            // Resume/exit starts at the previous complete, clipped target. Otherwise
            // an out-of-range hidden contribution could delay visible recentering.
            next.Lookahead.LastTarget=target.x;
            r.Target=target;r.State=next;r.Valid=true;
            return r;
        }
        private static FollowClipResult Invalid(FollowClipResult r,FollowClipInvalidReason reason)
        {r.Valid=false;r.InvalidReason=reason;r.RawSum=r.ClippedSum=Vector2.zero;return r;}
        private static double Clamp(double v,double min,double max)=>Math.Max(min,Math.Min(max,v));
        public static bool Finite(double v)=>!double.IsNaN(v)&&!double.IsInfinity(v);
        private static bool Nonnegative(double v)=>Finite(v)&&v>=0;
        public static bool Finite(Vector2 v)=>Finite(v.x)&&Finite(v.y);
        public static bool Finite(Vector3 v)=>Finite(v.x)&&Finite(v.y)&&Finite(v.z);
        private static bool Representable(double v)=>Finite(v)&&Math.Abs(v)<=float.MaxValue;
    }
}
