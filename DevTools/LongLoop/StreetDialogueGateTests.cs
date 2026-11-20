using System;
using System.Collections.Generic;
using System.Reflection;
using EatWhat.LongLoop;
using EatWhat.LongLoop.Configuration;
using EatWhat.Narrative.Unity;
using EatWhat.Toolkit.Input;
using UnityEngine;
using Newtonsoft.Json;

internal static class StreetDialogueGateTests
{
    static readonly List<string> details=new List<string>();
    static void Check(bool value,string reason){if(!value)throw new Exception(reason);details.Add(reason);}
    static void Near(double a,double b,string reason){Check(Math.Abs(a-b)<1e-5,reason);}
    static void Life(object value,string name){value.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(value,null);}
    sealed class InputSource:MonoBehaviour,IGameInput
    {
        public float X=1,Y;public Vector2 Look;public float Horizontal{get{return X;}}public float Vertical{get{return Y;}}
        public Vector2 LookAxis{get{return Look;}}public GameInputDevice Device=GameInputDevice.Keyboard;public GameInputDevice CurrentDevice{get{return Device;}}
        public bool Interact{get{return false;}}public Vector2 MouseScreenPos{get{return Vector2.zero;}}
    }
    sealed class OverrideState:IStreetChannelState
    {
        public bool DialogueOrCinematic{get;set;}public bool CameraOverridden{get;set;}
        public Vector3 VelocityWorld{get{return new Vector3(10000,10000,0);}}public IGameInput Input{get;set;}
        public IReadOnlyList<ChannelInterest> NearbyInterests{get{return null;}}
    }
    static void Configure(StreetFollowDriverTests.Fixture f)
    {
        f.Config.DynamicZoomEnabled=true;f.Config.OrthoSizeMin=2;f.Config.OrthoSizeMax=4;f.Config.ExpandTime=1;f.Config.ShrinkDelay=.6f;f.Config.ShrinkTime=1;
        f.Config.ExpandCurve=f.Config.ShrinkCurve=StreetCurvePreset.Linear;f.Config.FreezeOnOverride=true;
        f.Config.LookFloatEnabled=true;f.Config.LookInTime=.2f;f.Config.LookOutTime=.3f;f.Config.LookResponseCurve=StreetCurvePreset.Linear;
        f.Config.LookMaxDistance=8;f.Config.LookMaxSpeed=100;f.Config.MaxHorizontalOffset=8;f.Config.MaxVerticalOffset=8;
        StreetConfigs.Current.CameraMode.GateOffsetsInDialogue=true;
    }
    static void Tick(StreetFollowDriverTests.Fixture f,float time,float dx=0,float dy=0,float dt=.1f)
    {
        f.Foot.position+=new Vector3(dx,dy,0);var lens=f.State.Lens;lens.OrthographicSize=5;f.State.Lens=lens;f.Step(time,dt);
        Check(f.Driver.Status==StreetFollowDriverStatus.Applied,"actual driver applies sample "+time);
    }
    sealed class Sample { public Vector3 Target;public Vector2 Look;public float Size;public bool Moving;public double Float,Wait; }
    static List<Sample> Trace(bool attach)
    {
        var result=new List<Sample>();using(var f=new StreetFollowDriverTests.Fixture())
        {
            Configure(f);var input=new InputSource();f.Foot.components[typeof(IGameInput)]=input;
            StreetDialogueGameState state=null;
            if(attach)
            {
                state=new StreetDialogueGameState();state.Configure(new NarrativeDialogueService(),f.Foot);
                Check(ReferenceEquals(state.Input,input),"state forwards exact existing foot-parent input object");
                Check(!state.DialogueOrCinematic&&!state.CameraOverridden&&state.NearbyInterests.Count==0,"new state defaults both false and empty interests");
            }
            f.Driver.Configure(f.Binding,f.Point,f.Target,f.Interest,state,f.Debugger);
            for(int i=0;i<=12;i++)
            {
                input.Look=i<6?new Vector2(.8f,.2f):Vector2.zero;input.Y=i<6?.2f:0;
                Tick(f,i*.2f,i>0&&i<6?1:0,dt:.2f);
                Check(f.Debugger.LastLookInput==input.Look,"same real look axis with attach="+attach+" sample="+i);
                result.Add(new Sample{Target=f.Target.position,Look=f.Debugger.LastLookInput,Size=f.State.Lens.OrthographicSize,
                    Moving=f.Driver.LastResult.GoalStep.Moving,Float=f.Driver.LookFloatState.X.Value,Wait=f.Driver.ZoomState.ShrinkWaitElapsed});
            }
            if(state!=null)Life(state,"OnDisable");
        }return result;
    }
    static void NeutralParity()
    {
        var plain=Trace(false);var attached=Trace(true);Check(plain.Count==attached.Count,"same neutral sample count");
        for(int i=0;i<plain.Count;i++)
        {
            var a=plain[i];var b=attached[i];Near(a.Target.x,b.Target.x,"neutral final target x "+i);Near(a.Target.y,b.Target.y,"neutral final target y "+i);
            Near(a.Target.z,b.Target.z,"neutral final target z "+i);Near(a.Size,b.Size,"neutral lens size "+i);Near(a.Float,b.Float,"neutral floating state "+i);
            Near(a.Wait,b.Wait,"neutral ShrinkDelay progress "+i);Check(a.Moving==b.Moving&&a.Look==b.Look,"neutral actual movement classification and input identical "+i);
        }
    }
    static void OrdinaryDialogue()
    {
        using(var f=new StreetFollowDriverTests.Fixture())
        {
            Configure(f);var input=new InputSource{Look=new Vector2(1,0)};var service=new NarrativeDialogueService();
            var state=new StreetDialogueGameState();state.Configure(service,f.Foot,input);f.Driver.Configure(f.Binding,f.Point,f.Target,f.Interest,state,f.Debugger);
            Check(ReferenceEquals(state.Input,input),"explicit provider forwards actual input without copying values");
            Tick(f,0);for(int i=1;i<=5;i++)Tick(f,i*.2f,1,dt:.2f);
            Near(f.State.Lens.OrthographicSize,4,"moving establishes maximum zoom");Check(f.Driver.LookFloatState.X.Value>.99,"existing floating is established before dialogue");
            service.Begin();Check(state.DialogueOrCinematic&&!state.CameraOverridden,"normal dialogue raises offsets gate only");
            Tick(f,1.1f,dt:.1f);Near(f.Driver.LookFloatState.X.Target,0,"dialogue look target zero");
            Check(f.Driver.LookFloatState.X.Value>0&&f.Driver.LookFloatState.X.Value<1,"existing float exits gradually using original OutTime");
            Check(f.Driver.LastResult.Channels.LookFloat.x>0,"existing final floating contribution exits instead of snapping zero");
            var clip=new FollowClipContext{Orthographic=true,ProjectionKnown=true,ScreenRight=Vector3.right,OrthoSize=4,Aspect=1,SafeZoneWidth=1,
                DialogueOrCinematic=state.DialogueOrCinematic,GateOffsetsInDialogue=true};
            var contributions=new FollowContributions{Interest=new Vector2(2,1),LookFloat=new Vector2(1,1)};
            var sum=StreetFollowPipeline.SumAndClip(clip,contributions,8,8);
            Check(sum.Valid&&sum.GateApplied&&sum.Interest==Vector2.zero&&sum.LookFloat==Vector2.zero,"actual interest and float sum gates retain zero targets");
            Tick(f,1.3f,dt:.2f);Near(f.Driver.LookFloatState.X.Value,0,"float completes original .3-second exit");
            Check(f.Driver.ZoomState.ShrinkWaitElapsed>0,"ordinary dialogue advances shrink delay instead of freezing");
            for(int i=0;i<9;i++)Tick(f,1.5f+i*.2f,dt:.2f);
            Near(f.State.Lens.OrthographicSize,2,"ordinary dialogue stopped zoom shrinks to minimum");Near(f.Config.ShrinkDelay,.6,"dialogue does not change configured ShrinkDelay");
            service.End();Tick(f,3.3f,dt:.2f);Check(!state.DialogueOrCinematic&&f.Driver.LookFloatState.X.Target>0&&f.Driver.LookFloatState.X.Value>0,"dialogue end restores look float normally");
            Life(state,"OnDisable");
        }
    }
    static void Takeover()
    {
        using(var f=new StreetFollowDriverTests.Fixture())
        {
            Configure(f);var input=new InputSource();var state=new OverrideState{Input=input};f.Driver.ConfigureGameState(state);
            Tick(f,0);Tick(f,.2f,1,dt:.2f);var before=f.Driver.ZoomState;
            state.CameraOverridden=true;input.Look=new Vector2(1,0);Tick(f,.4f,1,dt:.2f);
            Near(f.Driver.ZoomState.Size,before.Size,"camera takeover freezes lens without dialogue gate");
            Near(f.Driver.ZoomState.Elapsed,before.Elapsed,"camera takeover freezes transition time");
            Check(!state.DialogueOrCinematic&&f.Driver.LookFloatState.X.Target>0,"takeover does not silently become an offsets-dialogue gate");
            f.Config.FreezeOnOverride=false;Tick(f,.6f,1,dt:.2f);Check(f.Driver.ZoomState.Size>before.Size,"existing FreezeOnOverride switch still controls takeover freeze");
        }
    }
    static void EventsAndData()
    {
        var foot=new Transform();var input=new InputSource{Look=new Vector2(.4f,.3f)};foot.components[typeof(IGameInput)]=input;
        var first=new NarrativeDialogueService();var second=new NarrativeDialogueService();var state=new StreetDialogueGameState();state.Configure(first,foot);
        Check(first.Subscribers==2&&ReferenceEquals(state.Input,input),"state subscribes once to both service events and forwards parent input");
        Time.time=10;Near(state.VelocityWorld.x,0,"first actual foot speed sample initializes");foot.position=new Vector3(2,1,0);Time.time=10.5f;
        Near(state.VelocityWorld.x,4,"compatibility velocity uses real world foot displacement x");Near(state.VelocityWorld.y,2,"compatibility velocity uses real world foot displacement y");
        first.Begin();Check(state.DialogueOrCinematic&&!state.CameraOverridden,"start event sets dialogue only");first.End(true);Check(!state.DialogueOrCinematic,"aborted end clears dialogue flag");
        first.Begin();state.Configure(second,foot,input);Check(!state.DialogueOrCinematic&&first.Subscribers==0&&second.Subscribers==2,"rebind clears old state and subscriptions");first.End();Check(!state.DialogueOrCinematic,"stale service event cannot affect replacement");
        second.Begin();state.enabled=false;Life(state,"OnDisable");Check(!state.DialogueOrCinematic&&second.Subscribers==0,"disable clears state and detaches both events");
        state.enabled=true;Life(state,"OnEnable");Check(state.DialogueOrCinematic&&second.Subscribers==2,"enable synchronizes an already running service");
        Life(state,"OnDestroy");Check(second.Subscribers==0&&!state.DialogueOrCinematic,"destroy releases subscriptions and gate state");
    }
    static readonly List<object> finalTargetTraces=new List<object>();
    sealed class ReturnSample
    {
        public double Elapsed;[JsonIgnore]public Vector3 Target;[JsonIgnore]public Vector2 World;
        public float[] FinalTarget{get{return new[]{Target.x,Target.y,Target.z};}}
        public float[] WorldOffset{get{return new[]{World.x,World.y};}}
        public double NormalizedX,NormalizedY;public bool Entering;
    }
    static ReturnSample ReturnCapture(StreetFollowDriverTests.Fixture f,double elapsed)
    {
        return new ReturnSample {Elapsed=elapsed,Target=f.Target.position,World=f.Driver.LookWorldOffset,
            NormalizedX=f.Driver.LookFloatState.X.Value,NormalizedY=f.Driver.LookFloatState.Y.Value,
            Entering=f.Driver.LookFloatState.X.Entering&&f.Driver.LookFloatState.X.Elapsed<f.Driver.LookFloatState.X.Duration};
    }
    static List<ReturnSample> ReturnTrace(bool floating,int warm,float inTime,float outTime,float speed,bool gateOn,bool gamepad,int sign)
    {
        var samples=new List<ReturnSample>();using(var f=new StreetFollowDriverTests.Fixture())
        {
            Configure(f);f.Config.DynamicZoomEnabled=false;f.Config.Lookahead=0;f.Config.RecenterSpeed=0;
            f.Config.LookFloatEnabled=floating;f.Config.LookInTime=inTime;f.Config.LookOutTime=outTime;
            f.Config.LookResponseCurve=StreetCurvePreset.Quadratic;f.Config.MaxHorizontalOffset=6;f.Config.MaxVerticalOffset=1.5f;
            f.Config.LookMaxDistance=2.5f;f.Config.LookMaxSpeed=4;
            StreetConfigs.Current.CameraMode.GateOffsetsInDialogue=gateOn;
            var input=new InputSource {X=0,Look=new Vector2(sign,sign),Device=gamepad?GameInputDevice.Gamepad:GameInputDevice.Keyboard};
            var service=new NarrativeDialogueService();var state=new StreetDialogueGameState();state.Configure(service,f.Foot,input);
            f.Driver.Configure(f.Binding,f.Point,f.Target,f.Interest,state,f.Debugger);
            Tick(f,0,dt:.05f);for(int i=1;i<=warm;i++)Tick(f,i*.05f,.05f,.025f,.05f);
            samples.Add(ReturnCapture(f,0));f.Config.LookMaxSpeed=speed;service.Begin();
            for(int i=1;i<=12;i++)
            {
                Tick(f,warm*.05f+i*.1f,.1f,.05f,.1f);samples.Add(ReturnCapture(f,i*.1));
                Check(f.Driver.LastResult.Channels.Interest==Vector2.zero,"production driver interest remains zero in dialogue sample "+i);
                if(gateOn)Near(f.Driver.LookFloatState.X.Target,0,"held mouse/gamepad cannot create new dialogue look target "+i);
            }
            // If an extreme speed cap outlasts OutTime, continue using the same real driver.
            if(speed>0&&speed<1)
            {
                Tick(f,warm*.05f+31.2f,.02f,.01f,30);
                samples.Add(ReturnCapture(f,31.2));
            }
            Life(state,"OnDisable");
        }return samples;
    }
    static double Length(Vector2 v){return Math.Sqrt((double)v.x*v.x+(double)v.y*v.y);}
    static void FinalReturnCase(string label,int warm,float inTime,float outTime,float speed,bool gamepad,int sign,bool gateOn=true)
    {
        var actual=ReturnTrace(true,warm,inTime,outTime,speed,gateOn,gamepad,sign);
        var baseline=ReturnTrace(false,warm,inTime,outTime,speed,gateOn,gamepad,sign);
        // Keep both trajectories moving above the original .5 threshold, so the no-float
        // control does not enter a different return-suspension/carry branch at gate entry.
        finalTargetTraces.Add(new {label,warm,inTime,outTime,speed,gamepad,sign,gateOn,actual,baseline});
        var start=actual[0].World;Check(Length(start)>0,label+" established actual world offset");
        Near(actual[0].Target.x-baseline[0].Target.x,start.x,label+" final Target X at gate origin");
        Near(actual[0].Target.y-baseline[0].Target.y,start.y,label+" final Target Y at gate origin");
        if(warm==1)
        {
            Check(Length(start)<Math.Sqrt(Math.Pow(actual[0].NormalizedX*6,2)+Math.Pow(actual[0].NormalizedY*1.5,2)),label+" actual world offset trails normalized goal");
            Check(inTime==0||actual[0].Entering,label+" starts during enter animation or immediate intent under speed cap");
        }
        for(int i=1;i<actual.Count;i++)
        {
            var sample=actual[i];var previous=actual[i-1];double dt=sample.Elapsed-previous.Elapsed;
            double x=sample.Target.x-baseline[i].Target.x,y=sample.Target.y-baseline[i].Target.y;
            Near(x,sample.World.x,label+" final Target X relative to identical no-float trajectory "+i);
            Near(y,sample.World.y,label+" final Target Y relative to identical no-float trajectory "+i);
            Check(Length(sample.World)<=2.5+1e-5,label+" original radial world distance cap "+i);
            Check(Length(sample.World-previous.World)<=speed*dt+1e-5,label+" original world speed cap "+i);
            if(gateOn)
            {
                Check(Math.Abs(x)<=Math.Abs(previous.World.x)+1e-5&&Math.Abs(y)<=Math.Abs(previous.World.y)+1e-5,label+" no outward drift on either actual axis "+i);
                if(speed==4&&outTime>0)
                {
                    double factor=Math.Max(0,1-sample.Elapsed/outTime);
                    Near(x,start.x*factor,label+" final X uses existing exit time from actual value "+i);
                    Near(y,start.y*factor,label+" final Y uses existing exit time from actual value "+i);
                }
            }
        }
        if(gateOn&&speed==4)
        {
            Near(Length(actual[8].World),0,label+" final Target reaches baseline by endpoint");
            Near(Length(actual[12].World),0,label+" sustained input cannot restart after endpoint");
        }
        if(gateOn&&speed>0&&speed<1)
        {
            Check(Length(actual[8].World)>0,label+" hard speed cap may extend actual return beyond OutTime");
            Near(Length(actual[actual.Count-1].World),0,label+" speed-limited tail eventually reaches baseline");
        }
        if(!gateOn)Check(Length(actual[12].World)>0,label+" disabled gate permits existing look input");

    }
    static void FinalTargetReturns()
    {
        FinalReturnCase("saved-limits-both-positive-mouse",32,.35f,.8f,4,false,1);
        FinalReturnCase("saved-limits-both-negative-gamepad",32,.35f,.8f,4,true,-1);
        FinalReturnCase("during-enter-animation",1,.35f,.8f,4,false,1);
        FinalReturnCase("normalized-goal-ahead-of-world-speed",1,0,.8f,4,true,1);
        FinalReturnCase("zero-time-still-obeys-world-speed",32,.35f,0,4,false,1);
        FinalReturnCase("extreme-low-world-speed",32,.35f,.8f,.1f,true,-1);
        FinalReturnCase("zero-world-speed-holds-existing-value",32,.35f,.8f,0,true,1);
        FinalReturnCase("mode-gate-disabled",32,.35f,.8f,4,false,1,false);
        var fresh=ReturnTrace(true,0,.35f,.8f,4,true,true,1);
        for(int i=0;i<fresh.Count;i++)Near(Length(fresh[i].World),0,"dialogue from neutral rejects held gamepad "+i);
        // Genuine world residue from the actual driver, not an alternate fake channel.
        var residual=ReturnTrace(true,1,.35f,.8f,4,true,false,1)[1].World;
        var clip=new FollowClipContext {Orthographic=true,ProjectionKnown=true,ScreenRight=Vector3.right,
            OrthoSize=1,Aspect=1,SafeZoneWidth=.1f,GateOffsetsInDialogue=true,DialogueOrCinematic=true,LookInputGated=true};
        var sum=StreetFollowPipeline.SumAndClip(clip,new FollowContributions {Interest=new Vector2(2,1),LookFloat=residual},6,1.5f);
        Check(sum.Valid&&sum.GateApplied&&sum.Interest==Vector2.zero,"interest is immediately gated even with real pre-gated look residue");
        Near(sum.LookFloat.x,residual.x,"already gated actual residue remains in float channel");
        Check(sum.Clipped&&Math.Abs(sum.ClippedSum.x)<=.1+1e-5,"same final safe-zone clipping still applies to residue");
        clip.LookInputGated=false;
        sum=StreetFollowPipeline.SumAndClip(clip,new FollowContributions {Interest=new Vector2(2,1),LookFloat=residual},6,1.5f);
        Check(sum.Valid&&sum.Interest==Vector2.zero&&sum.LookFloat==Vector2.zero,"raw-input hard-gate contract remains unchanged by default");
    }
    public static int Main()
    {
        try{NeutralParity();OrdinaryDialogue();Takeover();EventsAndData();FinalTargetReturns();Console.WriteLine("DIALOGUE_CAMERA_GATE_RESULT "+JsonConvert.SerializeObject(new{passed=true,checks=details.Count,actualDriverAndCameraModels=true,nativeUnity=false,dialogueServiceBoundary="event/query handles only; production service separately compiled",finalTargetTraces=finalTargetTraces,details=details}));return 0;}
        catch(Exception ex){Console.Error.WriteLine("DIALOGUE_CAMERA_GATE_FAILED "+ex);Console.Error.WriteLine("FINAL_TARGET_FAILURE_TRACES "+JsonConvert.SerializeObject(finalTargetTraces));return 1;}
    }
}
