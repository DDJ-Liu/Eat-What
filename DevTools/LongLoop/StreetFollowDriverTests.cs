using System;
using EatWhat.LongLoop;
using EatWhat.LongLoop.Configuration;
using EatWhat.Toolkit.Input;
using Unity.Cinemachine;
#if !MOVING_LOOKAHEAD_PRODUCT
using EatWhat.EditorValidation;
#endif
using System.Linq;
using UnityEngine;

internal sealed class DriverProbe:StreetFollowDriver
{
    public void Boot(){enabled=true;base.Awake();base.OnEnable();}
    public void Stop(){enabled=false;base.OnDisable();}
}
internal sealed class DriverBindingProbe:CameraFollowConfigBinder
{
    public void Boot(){enabled=true;base.OnEnable();}
    public void Stop(){enabled=false;base.OnDisable();}
    public void Cleanup(){base.LateUpdate();}
}
internal sealed class DriverPointProbe:StreetFollowPoint
{
    public void Boot(){enabled=true;base.OnEnable();}
    public void Stop(){enabled=false;base.OnDisable();}
}
internal sealed class DriverInput:IGameInput
{
    public float Horizontal=>1;public float Vertical=>0;public Vector2 LookAxis=>Vector2.zero;
    public GameInputDevice CurrentDevice=>GameInputDevice.Keyboard;public bool Interact=>false;public Vector2 MouseScreenPos=>Vector2.zero;
}
internal sealed class DriverGameState:MonoBehaviour,IStreetChannelState
{
    public Vector3 VelocityWorld=>Vector3.right*1000;public IGameInput Input {get;}=new DriverInput();
    public System.Collections.Generic.IReadOnlyList<ChannelInterest> NearbyInterests=>null;
    public bool DialogueOrCinematic {get;set;}
}
internal static class StreetFollowDriverTests
{
    private static int checks,scenarios;private static readonly System.Collections.Generic.List<string> failures=new System.Collections.Generic.List<string>();
    private static void Check(bool good,string label){checks++;if(!good)throw new Exception(label);}
    private static void Near(double v,double expected,string label,double tolerance=.0003)=>Check(!double.IsNaN(v)&&Math.Abs(v-expected)<=tolerance,label+": "+v+" != "+expected);
    private static void Scenario(string name,Action body)
    {scenarios++;try{body();Console.WriteLine("DRIVER PASS "+name);}catch(Exception e){failures.Add(name+": "+e);Console.Error.WriteLine("DRIVER FAIL "+name+": "+e);}}
    internal sealed class Fixture:IDisposable
    {
        public Transform Foot=new Transform(),Node,Target=new Transform();
        public CameraFollowConfig Config=new CameraFollowConfig();public DepthScaleConfig Depth=new DepthScaleConfig();
        public CameraInterestConfig Interest=new CameraInterestConfig();public CinemachineCamera Camera=new CinemachineCamera();
        public DriverPointProbe Point=new DriverPointProbe();public DriverBindingProbe Binding=new DriverBindingProbe();
        public DriverProbe Driver=new DriverProbe();public CinemachinePositionComposer Composer;public StreetCameraDebug Debugger=new StreetCameraDebug();
        public CameraState State;public int Notifications;public CinemachineConfiner2D Confiner;
        public Fixture(bool confinerFirst=false)
        {
            Application.isPlaying=true;StreetConfigs.Unbind();
            Config.StoppedSpeedMultiplier=1; // Explicit AI492 regression baseline.
            Config.Lookahead=4.5f;Config.LookaheadTime=2;Config.MaxHorizontalOffset=8;Config.DampingX=.5f;Config.DampingY=.25f;
            Config.RecenterDelay=0;Config.RecenterSpeed=.4f;Interest.SafeZoneWidth=1;
            Node=new Transform {parent=Foot};Camera.transform.components[typeof(CinemachineCamera)]=Camera;Camera.Follow=Target;
            Composer=new CinemachinePositionComposer {VirtualCamera=Camera,transform=Camera.transform,CameraDistance=10,
                CenterOnActivate=false,Damping=new Vector3(3,4,7),Composition=ScreenComposerSettings.Default,TargetOffset=new Vector3(1,2,3)};
            var composition=Composer.Composition;composition.DeadZone.Size=new Vector2(.33f,.22f);Composer.Composition=composition;Camera.Body=Composer;
            StreetConfigs.Bind(Depth,new CameraModeConfig(),Config,new SceneBoundsConfig(),new Transform(),new Collider2D());
            Point.Configure(Foot,Node);Point.Boot();Binding.Configure(Camera,Composer);Binding.Boot();
            if(confinerFirst){Confiner=new CinemachineConfiner2D();Confiner.Boot(Camera.transform);}
            Driver.transform=Camera.transform;Driver.Configure(Binding,Point,Target,Interest,null,Debugger);Driver.Boot();
            Driver.EvaluationCompleted+=(source,owner,sequence,snapshot,dt)=>{Notifications++;};
            State=new CameraState {Lens=new LensSettings {Orthographic=true,OrthographicSize=5,Aspect=1,FieldOfView=60},
                RawOrientation=Quaternion.identity,RawPosition=new Vector3(-4,1,-10),ReferenceUp=Vector3.up,PositionCorrection=new Vector3(2,3,4)};
        }
        public void Step(float time,float dt=.02f,Action<Fixture> beforeCompose=null)
        {
            Time.time=time;Time.deltaTime=dt;Time.frameCount++;CinemachineCore.CurrentUpdateFrame++;
            Camera.State=State;Driver.PrePipelineMutateCameraStateCallback(Camera,ref State,dt);
            Camera.PostAll(CinemachineCore.Stage.Aim,ref State,dt);beforeCompose?.Invoke(this);Composer.MutateCameraState(ref State,dt);
            Camera.PostAll(CinemachineCore.Stage.Body,ref State,dt);Camera.PreviousStateIsValid=true;Camera.State=State;
        }
        public void Dispose(){Driver.Stop();Binding.Stop();Point.Stop();StreetConfigs.Unbind();}
    }
    public static int Main(string[] args)
    {
        if(args.Length>0&&args[0]=="--counter")
        {MovingConvergence(true);return 0;}
        if(args.Length>0&&args[0]=="--deadzone-counter")
        {DeadzoneCounterexample();return 0;}
        if(args.Length>0&&args[0]=="--motion-only")
        {
            Scenario("FinalCameraDeadzoneDirectionalLatch",DeadzoneLatch);
            Scenario("FinalComposerWorldPositionMotion",()=>MovingConvergence(false));
            Scenario("ShortWalkReturnAndReversal",ShortWalkReturn);
            Scenario("VerticalCompositionIndependentOfHorizontalDamping",VerticalOwnership);
            Console.WriteLine("MOTION_ONLY scenarios="+scenarios+" checks="+checks+" failed="+failures.Count);
            return failures.Count==0?0:1;
        }
        Scenario("FR01PureRulesRetained",()=>Check(StreetFollowPipelineTests.Main()==0,"retained pure target scenarios"));
        Scenario("EventDrivenBinderNoLeaseOrRestore",Binding);
        Scenario("NativeWorldPositionDampingBothAxes",OneDamping);
        Scenario("FinalComposerWorldPositionMotion",()=>MovingConvergence(false));
        Scenario("FinalCameraDeadzoneDirectionalLatch",DeadzoneLatch);
        Scenario("ShortWalkReturnAndReversal",ShortWalkReturn);
        Scenario("VerticalCompositionIndependentOfHorizontalDamping",VerticalOwnership);
        Scenario("StoppedSpeedDefaultEventsAndCleanup",StoppedSetting);
        Scenario("StoppedSameStateResponseAndCache",StoppedResponse);
        Scenario("StoppedConfinerOrderAndOneNativeBody",StoppedConfiner);
        Scenario("RootTargetAndDuplicateGuards",RootAndDuplicates);
        Scenario("BlockedIntentLifecycleAndConfig",Lifecycle);
        Scenario("NativeWarpOnlyOnTeleport",Warp);
        Scenario("ConfinerKeepsNativeOrder",Confiner);
        Scenario("DiagnosticsSilentByDefault",Diagnostics);
#if !MOVING_LOOKAHEAD_PRODUCT
        Scenario("LegacyProbePredicatesNotFalsePass",ProbeMigration);
#endif
        Console.WriteLine((failures.Count==0?"PASS ":"FAIL ")+scenarios+" Driver scenarios; "+checks+" checks; failed="+failures.Count+"; installed Composer/Damper/dispatcher callbacks, explicit Unity/Brain/geometry boundaries");
        return failures.Count==0?0:1;
    }
    private static void StoppedSetting()
    {
        Near(new CameraFollowConfig().StoppedSpeedMultiplier,.7,"new source default");
        using(var f=new Fixture())
        {
            int local=0,global=0;Action handler=()=>global++;f.Config.Changed+=()=>local++;StreetConfigs.Changed+=handler;
            f.Config.StoppedSpeedMultiplier=.7f;f.Config.StoppedSpeedMultiplier=.7f;
            Near(local,1,"new setter changes once");Near(global,1,"new setter reaches existing container chain");
            var field=typeof(CameraFollowConfig).GetField("stoppedSpeedMultiplier",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
            var validate=typeof(StreetConfigAsset).GetMethod("OnValidate",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
            field.SetValue(f.Config,.6f);validate.Invoke(f.Config,null);validate.Invoke(f.Config,null);
            Near(local,2,"Inspector serialized-state chain includes multiplier");Near(global,2,"Inspector chain quiet on repeat");
            StreetConfigs.Changed-=handler;
            foreach(float value in new[]{-1f,float.NaN,float.PositiveInfinity})
            {
                f.Config.StoppedSpeedMultiplier=value;f.Step(.1f,.1f);
                Check(f.Binding.Status==CameraFollowBindingStatus.InvalidConfig&&f.Driver.Status==StreetFollowDriverStatus.InvalidConfig,"invalid multiplier rejected");
                Check(float.IsNaN(value)?float.IsNaN(f.Config.StoppedSpeedMultiplier):f.Config.StoppedSpeedMultiplier==value,"invalid value not silently rewritten");
            }
            f.Config.StoppedSpeedMultiplier=.7f;var original=f.Composer.Damping;
            f.Binding.BeginStoppedResponse(.02f);Check(f.Composer.Damping.x>original.x,"temporary slower runtime response");
            f.Binding.Cleanup();Check(f.Composer.Damping.Equals(original),"missing Body cleanup restores owned runtime value");
            f.Binding.BeginStoppedResponse(.02f);f.Driver.Stop();Check(f.Composer.Damping.Equals(original),"disable restores response ownership");f.Driver.Boot();
            f.Binding.BeginStoppedResponse(.02f);var external=f.Composer.Damping;external.x=9;f.Composer.Damping=external;
            f.Binding.EndStoppedResponse();Near(f.Composer.Damping.x,9,"external runtime field change is not overwritten");f.Composer.Damping=original;
            f.Binding.BeginStoppedResponse(.02f);f.Config.DampingX=.4f;
            Near(f.Composer.Damping.x,StreetFollowPipeline.ToCmDamping(.4f),"configuration event replaces temporary response without stale restore");
            f.Binding.EndStoppedResponse();Near(f.Composer.Damping.x,StreetFollowPipeline.ToCmDamping(.4f),"old response never restores over new config");
            f.Driver.ConfigureGameState(new DriverGameState());f.Step(.2f,.1f);
            f.Foot.position=Vector3.right;f.Step(.3f,.1f,x=>Near(x.Composer.Damping.x,StreetFollowPipeline.ToCmDamping(.4f),"actual movement restores normal response"));
            f.Step(.4f,.1f,x=>Check(x.Composer.Damping.x>StreetFollowPipeline.ToCmDamping(.4f),"blocked foot with held input still gets stopped response"));
        }
    }
    private static double StopExpected(float previous,float target,float nativeTime,float dt,float multiplier)
    {
        float error=target-previous;
        // The old same-state response comes from the installed public native Damper,
        // not a second diverged trajectory. Preserve its explicit Epsilon end snap.
        double response=Damper.Damp(error,nativeTime,dt);
        if(Math.Abs(error)<.0001)return previous+response;
        double scaled=response*multiplier;
        if(Math.Abs(scaled)>Math.Abs(error))scaled=error;
        return previous+scaled;
    }
    private static void StoppedResponse()
    {
        var targetReferences=new System.Collections.Generic.Dictionary<string,System.Collections.Generic.List<Vector3>>();
        foreach(int fps in new[]{30,60,120})foreach(float multiplier in new[]{0f,.7f,1f,2f})foreach(float tau in new[]{0f,.5f})
        using(var f=new Fixture())
        {
            ConfigureCurrent(f,3.5f,tau);f.Config.LookaheadTime=2.4f;f.Config.DampingY=tau==0?0:.25f;
            f.Config.StoppedSpeedMultiplier=multiplier;float dt=1f/fps;
            var original=f.Composer.Damping;int configWrites=f.Binding.AppliedConfigurations;
            string key=fps+"/"+tau;
            if(multiplier==0)targetReferences[key]=new System.Collections.Generic.List<Vector3>();
            double maxError=0;int first=fps*3;Vector3 atStop=Vector3.zero;
            for(int i=1;i<=fps*13;i++)
            {
                var before=f.State.RawPosition;int dampCalls=f.Camera.DampCalls;
                if(i<=first)f.Foot.position=new Vector3(6.5f*i/fps,2f*i/fps,0);
                if(i==first)atStop=f.Foot.position;
                f.Step((float)i/fps,dt);
                bool stopped=i>first;
                var target=f.Target.position;float factor=stopped?multiplier:1;
                if(multiplier==0)targetReferences[key].Add(target);
                else Check(target.Equals(targetReferences[key][i-1]),"multiplier does not change target generation, Delay or return curve");
                double ex=StopExpected(before.x,target.x,original.x,dt,factor),ey=StopExpected(before.y,target.y,original.y,dt,factor);
                maxError=Math.Max(maxError,Math.Max(Math.Abs(f.State.RawPosition.x-ex),Math.Abs(f.State.RawPosition.y-ey)));
                Check(maxError<.00008,"same-state X/Y scaled response and next-frame native cache");
                Check(f.Camera.DampCalls-dampCalls==1,"one ordinary native Body, no replay");
                Check(f.Composer.Damping.Equals(original),"temporary runtime damping restored each Body");
                Near(f.State.RawPosition.z,-10,"Z distance untouched");Near(f.State.Lens.OrthographicSize,10,"Size untouched");
                if(i==first+1||i==first+fps)
                    Console.WriteLine("STOP_MULT fps="+fps+" m="+multiplier+" tau="+tau+" stopStep="+(i-first)+" dx="+(f.State.RawPosition.x-before.x)+" dy="+(f.State.RawPosition.y-before.y)+" target="+target.x+","+target.y+" maxError="+maxError);
                if(stopped&&i<first+(int)(.8f*fps))
                    Check(f.Target.position.x-atStop.x>3,"target Delay is not shortened by multiplier");
            }
            var old=f.State.RawPosition;f.Foot.position+=new Vector3(6.5f*dt,2*dt,0);f.Step(13+dt,dt);
            Near(f.State.RawPosition.x,StopExpected(old.x,f.Target.position.x,original.x,dt,1),"moving again immediately restores old response",.00008);
            Near(f.State.RawPosition.y,StopExpected(old.y,f.Target.position.y,original.y,dt,1),"moving again restores Y",.00008);
            old=f.State.RawPosition;f.Foot.position+=Vector3.up*(2*dt);f.Step(13+2*dt,dt);
            Near(f.State.RawPosition.x,StopExpected(old.x,f.Target.position.x,original.x,dt,1),"pure Y movement also restores ordinary X response",.00008);
            Near(f.Binding.AppliedConfigurations,configWrites,"per-frame response does not change config or publish Changed");
            f.Foot.position+=Vector3.right*100;f.Step(14,.1f,x=>Check(x.Composer.Damping.Equals(original),"warp has no stopped response override"));
            f.Driver.Stop();f.Driver.Boot();f.Step(15,.1f,x=>Check(x.Composer.Damping.Equals(original),"reinitialization not scaled"));
        }
    }
    private static void StoppedConfiner()
    {
        foreach(bool beforeDriver in new[]{false,true})using(var f=new Fixture(beforeDriver))
        {
            if(!beforeDriver){f.Confiner=new CinemachineConfiner2D();f.Confiner.Boot(f.Camera.transform);}
            ConfigureCurrent(f,3.5f,.5f);f.Config.LookaheadTime=2.4f;f.Config.StoppedSpeedMultiplier=.7f;
            int driverIndex=f.Camera.Extensions.IndexOf(f.Driver),confinerIndex=f.Camera.Extensions.IndexOf(f.Confiner);
            for(int i=1;i<=180;i++)
            {
                f.State.PositionCorrection=Vector3.zero;
                if(i<=60)f.Foot.position=new Vector3(i*.15f,i*.05f,0);
                int calls=f.Confiner.BodyCallbacks;f.Step(i/60f,1f/60);
                Check(f.Confiner.BodyCallbacks-calls==1,"Confiner callback exactly once");
                var final=f.State.GetFinalPosition();Check(Math.Abs(final.x)<=2.0001&&Math.Abs(final.y)<=2.0001,"full Confiner correction survives either registration order");
                Check(f.Camera.Extensions.IndexOf(f.Driver)==driverIndex&&f.Camera.Extensions.IndexOf(f.Confiner)==confinerIndex,"no registration reordering");
            }
            Console.WriteLine("STOP_CONFINER first="+beforeDriver+" callbacks="+f.Confiner.BodyCallbacks+" final="+f.State.GetFinalPosition().x+","+f.State.GetFinalPosition().y);
        }
    }
    private static void Binding()
    {
        using(var f=new Fixture())
        {
            int applied=f.Binding.AppliedConfigurations;
            Near(f.Composer.Damping.x,StreetFollowPipeline.ToCmDamping(.5f),"horizontal native damping restored");
            Near(f.Composer.Damping.y,StreetFollowPipeline.ToCmDamping(.25f),"vertical native damping retained");
            Check(!f.Composer.Composition.DeadZone.Enabled&&!f.Composer.Lookahead.Enabled,"native duplicate features off");
            Near(f.Composer.Composition.DeadZone.Size.x,.33,"saved deadzone dimensions retained");Near(f.Composer.Damping.z,7,"Z damping preserved");
            f.Step(0,-1);for(int i=1;i<=10;i++)f.Step(i*.02f);
            Near(f.Binding.AppliedConfigurations,applied,"no per-frame configuration writes");
            var damping=f.Composer.Damping;damping.x=9;f.Composer.Damping=damping;f.Step(.3f);
            Near(f.Composer.Damping.x,9,"external value not polled/restored");
            f.Depth.Focus=3;Near(f.Binding.AppliedConfigurations,applied,"unrelated config event not damping sync");
            f.Config.DampingX=.4f;Near(f.Binding.AppliedConfigurations,applied+1,"real follow Changed exactly once");
            Near(f.Composer.Damping.x,StreetFollowPipeline.ToCmDamping(.4f),"changed native horizontal damping");
            f.Config.DampingX=.4f;Near(f.Binding.AppliedConfigurations,applied+1,"same-value setter quiet");
            f.Config.DampingX=float.NaN;Check(f.Binding.Status==CameraFollowBindingStatus.InvalidConfig,"nonfinite native damping rejected");
            f.Config.DampingX=.4f;f.Binding.Stop();Near(f.Composer.Damping.x,StreetFollowPipeline.ToCmDamping(.4f),"disable preserves applied native damping");
        }
    }
    private static void OneDamping()
    {
        foreach(int fps in new[]{30,60,120})foreach(float tau in new[]{0f,.2f,.5f})using(var f=new Fixture())
        {
            f.Config.DampingX=tau;f.Step(0,-1);int applies=f.Binding.AppliedConfigurations;double error=0;
            for(int i=1;i<=fps*3;i++)
            {
                var raw=f.State.RawPosition;int before=f.Camera.DampCalls;float dt=1f/fps;
                f.Foot.position=Vector3.right*(6.5f*i/fps);f.Step((float)i/fps,dt);
                double expected=raw.x+(f.Target.position.x-raw.x)*(tau>0?1-Math.Exp(-dt/tau):1);
                error=Math.Max(error,Math.Abs(f.State.RawPosition.x-expected));
                Check(f.Camera.DampCalls-before==1,"only actual Composer invokes detached damping");
                Near(f.State.PositionCorrection.x,2,"Driver does not touch corrections");Near(f.State.Lens.OrthographicSize,5,"Size passthrough");
            }
            Check(error<.0003,"exactly one actual native-source world-position damping response");
            Near(f.Binding.AppliedConfigurations,applies,"moving updates never rewrite damping");
            Console.WriteLine("DRIVER_DAMP fps="+fps+" tau="+tau+" maxRawError="+error+" configWrites="+applies+" target="+f.Target.position.x+" raw="+f.State.RawPosition.x);
        }
    }
    private static void ConfigureCurrent(Fixture f,float lead=3.5f,float tau=.2f,float delay=.8f)
    {
        f.Config.DeadZoneWidth=.15f;f.Config.DeadZoneHeight=.2f;
        f.Config.Lookahead=lead;f.Config.DampingX=tau;f.Config.RecenterDelay=delay;
        f.Config.RecenterSpeed=.65f;f.Config.RecenterRampTime=.25f;f.Config.RecenterCurve=StreetCurvePreset.SmoothStep;
        f.State.PositionCorrection=Vector3.zero;f.State.Lens.OrthographicSize=10;f.State.Lens.Aspect=16f/9f;
        f.Depth.Focus=2;f.Step(0,-1);
    }
    private static void DeadzoneCounterexample()
    {
        foreach(int direction in new[]{-1,1})foreach(float lead in new[]{0f,3.5f})using(var f=new Fixture())
        {
            ConfigureCurrent(f,lead);f.Foot.position=Vector3.right*(direction*.65f);f.Step(.1f,.1f);
            Near(f.Debugger.LastResult.State.Goal.x,0,"counterexample still inside deadzone");
            Check(Math.Abs(f.State.GetFinalPosition().x)>.01,"old relative filter or lookahead leaks before boundary");
            Console.WriteLine("DEADZONE_OLD dir="+direction+" L="+lead+" foot="+f.Foot.position.x+
                " goal="+f.Debugger.LastResult.State.Goal.x+" target="+f.Target.position.x+" final="+f.State.GetFinalPosition().x);
        }
    }
    private static void MovingConvergence(bool old)
    {
        Console.WriteLine("MOTION version="+(old?"old-production":"world-position-damping")+" units=world-unit seconds; final=CameraState.GetFinalPosition");
        foreach(int direction in new[]{-1,1})foreach(int fps in new[]{30,60,120,0})foreach(float lead in new[]{3.5f,6f})
        using(var f=new Fixture())
        {
            float tau=lead==3.5f?.2f:.5f;ConfigureCurrent(f,lead,tau);
            double start=0;int frame=0;
            if(!old)
            {
                while(!f.Debugger.LastResult.State.Lookahead.Triggered)
                {
                    double dt=fps==0?(frame++%2==0?1d/40:1d/80):1d/fps;start+=dt;
                    f.Foot.position=Vector3.right*(float)(direction*6.5*start);f.Step((float)start,(float)dt);
                    Check(start<2,"current deadzone eventually touches");
                }
            }
            double t=0;double previousWorld=f.State.GetFinalPosition().x;
            double previousRelative=previousWorld-f.Foot.position.x;
            foreach(double end in new[]{2d,3d,16d})
            {
                double relative=0,rv=0,wv=0;
                while(t<end-1e-9)
                {
                    double dt=Math.Min(end-t,fps==0?(frame++%2==0?1d/40:1d/80):1d/fps);t+=dt;
                    f.Foot.position=Vector3.right*(float)(direction*6.5*(start+t));f.Step((float)(start+t),(float)dt);
                    double world=f.State.GetFinalPosition().x;relative=world-f.Foot.position.x;
                    Near(world,previousWorld+(f.Target.position.x-previousWorld)*(1-Math.Exp(-dt/tau)),"final CM world-position recurrence",.0003);
                    rv=(relative-previousRelative)/dt;wv=(world-previousWorld)/dt;
                    previousWorld=world;previousRelative=relative;
                }
                Console.WriteLine("MOVE dir="+direction+" fps="+fps+" L="+lead+" tau="+tau+" t="+end+" triggerAt="+start+" targetLead="+(f.Target.position.x-f.Foot.position.x)+" finalLead="+relative+" relativeVelocity="+rv+" worldVelocity="+wv);
                if(end==16)
                {
                    Near(direction*(f.Target.position.x-f.Foot.position.x),lead,"target reaches requested lead",.001);
                    if(fps!=0)Near(direction*relative,lead-6.5/fps/(Math.Exp(1d/fps/tau)-1),"native steady world lag intentionally retained",.002);
                }
            }
            double before=previousRelative,worldBefore=previousWorld;f.Step((float)(start+t+1d/60),1f/60);
            double after=f.State.GetFinalPosition().x-f.Foot.position.x;
            double stopRelativeSpeed=(after-before)*60,stopWorldSpeed=(f.State.GetFinalPosition().x-worldBefore)*60;
            Console.WriteLine("STOP dir="+direction+" fps="+fps+" L="+lead+" relativeVelocity="+stopRelativeSpeed+" worldVelocity="+stopWorldSpeed);
            Near(f.Target.position.x-f.Foot.position.x,direction*lead,"delay holds target layer",.001);
            Near(f.State.GetFinalPosition().x,worldBefore+(f.Target.position.x-worldBefore)*(1-Math.Exp(-1d/60/tau)),"stop continues native catch-up",.0003);
            Check(direction*stopRelativeSpeed>4,"world damping catch-up restored, not treated as old relative-model failure");
        }
    }
    private static void DeadzoneLatch()
    {
        foreach(int direction in new[]{-1,1})foreach(float lead in new[]{0f,3.5f})using(var f=new Fixture())
        {
            ConfigureCurrent(f,lead);f.Config.DeadZoneWidth=.25f;f.State.Lens.OrthographicSize=4;f.State.Lens.Aspect=1;
            for(int i=1;i<=9;i++)
            {
                f.Foot.position=Vector3.right*(direction*i*.1f);f.Step(i*.1f,.1f);
                Near(f.State.GetFinalPosition().x,0,"final Composer holds inside deadzone, including zero Lookahead",.00001);
                Near(f.Debugger.LastResult.State.Lookahead.SignedWeight,0,"no hidden pre-boundary build");
            }
            f.Foot.position=Vector3.right*direction;f.Step(1,.1f);
            Check(f.Debugger.LastResult.State.Lookahead.Triggered,"exact touching latches qualification");
            Check(direction*f.Debugger.LastResult.State.Lookahead.SignedWeight>0,"build begins at exact edge");
            double first=f.State.GetFinalPosition().x;
            Check(Math.Abs(first)<.5,"edge activation does not teleport to full lead");
            f.Config.DeadZoneWidth=.9f;double weight=f.Debugger.LastResult.State.Lookahead.SignedWeight;
            f.Foot.position=Vector3.right*(direction*1.1f);f.Step(1.1f,.1f);
            Check(Math.Abs(f.Debugger.LastResult.State.Lookahead.SignedWeight)>Math.Abs(weight),"qualification survives expanded deadzone returning actor inside");
            f.Config.DeadZoneWidth=.25f;
            double prior=f.State.GetFinalPosition().x;
            for(int i=1;i<=18;i++)
            {
                f.Foot.position=Vector3.right*(direction*(1.1f-i*.1f));f.Step(1.1f+i*.1f,.1f);
                Check(!f.Debugger.LastResult.State.Lookahead.Triggered,"actual reverse waits for opposite boundary");
                Check(direction*f.Debugger.LastResult.State.Lookahead.SignedWeight>=0,"opposite lookahead cannot build early");
                Check(Math.Abs(f.State.GetFinalPosition().x-prior)<1.1,"residual transitions continuously, no full-offset reset");
                prior=f.State.GetFinalPosition().x;
            }
            f.Foot.position=Vector3.right*(-direction*1.1f);f.Step(3.1f,.2f);
            Check(f.Debugger.LastResult.State.Lookahead.Triggered,"crossing opposite edge latches new direction");
            Console.WriteLine("DEADZONE_NEW dir="+direction+" L="+lead+" insideFinal=0 edgeFinal="+first+" oppositeCrossFinal="+f.State.GetFinalPosition().x);
        }
        foreach(float delay in new[]{0f,.8f})using(var f=new Fixture())
        {
            ConfigureCurrent(f,3.5f,.2f,delay);f.Foot.position=Vector3.right*3;f.Step(.5f,.5f);
            Check(f.Debugger.LastResult.State.Lookahead.Triggered,"cross edge qualification");
            f.Step(.6f,.1f);Check(f.Debugger.LastResult.State.Lookahead.Triggered,"stopping retains qualification");
            f.Foot.position+=Vector3.right*.1f;f.Step(.7f,.1f);
            Check(f.Debugger.LastResult.State.Lookahead.Triggered,"same-direction restart retains qualification");
            f.Foot.position+=Vector3.up*.1f;f.Step(.8f,.1f);
            Check(f.Debugger.LastResult.State.Lookahead.Triggered,"pure Y does not count as horizontal reversal");
            f.Driver.Stop();f.Driver.Boot();f.Step(.9f,.1f);
            Check(!f.Debugger.LastResult.State.Lookahead.Triggered,"disable/reactivate resets qualification");
            f.Foot.position+=Vector3.right*100;f.Step(1,.1f);
            Check(!f.Debugger.LastResult.State.Lookahead.Triggered,"real warp resets qualification");
        }
    }
    private static void ShortWalkReturn()
    {
        foreach(float delay in new[]{0f,.8f})foreach(int direction in new[]{-1,1})foreach(float build in new[]{0f,2f})
        using(var f=new Fixture())
        {
            ConfigureCurrent(f,3.5f,.2f,delay);f.Config.LookaheadTime=build;
            for(int i=1;i<=15;i++){f.Foot.position=Vector3.right*(direction*6.5f*i/60);f.Step(i/60f,1f/60);}
            double stopped=f.Target.position.x-f.Foot.position.x,previous=stopped;
            for(int i=16;i<=1500;i++)
            {
                double priorWorld=f.State.GetFinalPosition().x;f.Step(i/60f,1f/60);double offset=f.Target.position.x-f.Foot.position.x;
                Near(f.State.GetFinalPosition().x,priorWorld+(f.Target.position.x-priorWorld)*(1-Math.Exp(-1d/60/.2)),"stop final response follows target once",.0003);
                Check(Math.Abs(offset)<=Math.Abs(previous)+.0002,"stop moves only toward center, including a short walk still behind the actor");
                if(i<=15+(int)(delay*60)-1)Near(offset,stopped,"delay freezes target offset",.0002);
                previous=offset;
            }
            Near(previous,0,"short walk target completes recenter",.0002);
            for(int i=1501;i<=1560;i++){f.Foot.position+=Vector3.right*(direction*6.5f/60);f.Step(i/60f,1f/60);}
            double last=f.State.GetFinalPosition().x-f.Foot.position.x,world=f.State.GetFinalPosition().x;
            f.Foot.position-=Vector3.right*(direction*6.5f/60);f.Step(1561/60f,1f/60);
            double now=f.State.GetFinalPosition().x-f.Foot.position.x;
            Near(f.State.GetFinalPosition().x,world+(f.Target.position.x-world)*(1-Math.Exp(-1d/60/.2)),"reversal uses same native world-position response",.0003);
            Console.WriteLine("REVERSE dir="+direction+" delay="+delay+" build="+build+" relativeVelocity="+((now-last)*60)+" worldVelocity="+((f.State.GetFinalPosition().x-world)*60));
        }
    }
    private static void VerticalOwnership()
    {
        var expected=new System.Collections.Generic.List<float>();
        foreach(float tau in new[]{0f,.8f})using(var f=new Fixture())
        {
            ConfigureCurrent(f,3.5f,tau);
            for(int i=1;i<=600;i++)
            {
                var foot=new Vector3(i<=180?i*.07f:12.6f,i<=180?i*.015f:2.7f,0);
                f.Foot.position=foot;f.Step(i/60f,1f/60);
                if(tau==0)expected.Add(f.State.GetFinalPosition().y);
                else Near(f.State.GetFinalPosition().y,expected[i-1],"horizontal ownership preserves final native Y",.00001);
                Near(f.State.Lens.OrthographicSize,10,"horizontal ownership preserves Size");
            }
        }
    }
    private static void RootAndDuplicates()
    {
        using(var f=new Fixture())
        {
            f.Step(0,-1);f.Foot.position=Vector3.right;f.Step(.1f);
            var target=f.Target.position;int count=f.Driver.AppliedEvaluations,notes=f.Notifications;
            f.Driver.PrePipelineMutateCameraStateCallback(f.Camera,ref f.State,.02f);
            Check(f.Driver.Status==StreetFollowDriverStatus.HeldTime,"same-frame callback held");
            Check(f.Target.position.Equals(target)&&count==f.Driver.AppliedEvaluations,"duplicate target/time not integrated");
            f.Camera.PostAll(CinemachineCore.Stage.Body,ref f.State,.02f);Near(f.Notifications,notes,"duplicate completion guarded");
            f.Target.parent=f.Foot;f.Step(.2f);Check(f.Driver.Status==StreetFollowDriverStatus.MissingBinding,"player-child target rejected");
            f.Target.parent=null;f.Camera.Follow=f.Node;f.Step(.3f);Check(f.Driver.Status==StreetFollowDriverStatus.MissingBinding,"old FollowPoint tracking binding rejected");
            f.Camera.Follow=f.Target;f.Step(.4f);Check(f.Driver.Status==StreetFollowDriverStatus.Applied,"legal independent root resumes");
            var other=new CinemachineCamera();var state=f.State;count=f.Driver.AppliedEvaluations;
            f.Driver.PrePipelineMutateCameraStateCallback(other,ref state,.02f);Near(f.Driver.AppliedEvaluations,count,"unrelated vcam ignored");
        }
    }
    private static void Lifecycle()
    {
        using(var f=new Fixture())
        {
            f.Driver.ConfigureGameState(new DriverGameState());f.Step(0,-1);
            for(int i=1;i<=20;i++){f.Foot.position=Vector3.right*i*.1f;f.Step(i*.1f,.1f);}
            for(int i=21;i<=220;i++)f.Step(i*.1f,.1f);
            Check(f.Driver.PipelineState.StoppedSeconds>19,"blocked persistent input does not count as actual movement");
            Near(f.Debugger.LastMoveInput.x,1,"existing input is observed but never substituted for actual movement");
            Near(f.Target.position.x,f.Foot.position.x,"blocked input still recenters");
            var before=f.Driver.PipelineState;f.Step(21f);Check(f.Driver.Status==StreetFollowDriverStatus.InvalidClock,"backwards clock rejected");
            Check(f.Driver.PipelineState.Equals(before),"invalid clock preserves pure state");
            f.Camera.Live=false;f.Step(23f);Check(f.Driver.Status==StreetFollowDriverStatus.NonLive,"nonlive detaches target flow");
            f.Camera.Live=true;f.Step(24f);Near(f.Driver.PipelineState.Lookahead.SignedWeight,0,"reactivation starts fresh");
            StreetConfigs.Unbind();f.Step(25f);Check(f.Driver.Status!=StreetFollowDriverStatus.Applied,"unbound config cannot write target");
            StreetConfigs.Bind(f.Depth,new CameraModeConfig(),new CameraFollowConfig(),new SceneBoundsConfig(),new Transform(),new Collider2D());
            f.Step(26f);Check(f.Driver.Status==StreetFollowDriverStatus.Applied,"new config identity rebinds");
            f.Driver.Stop();f.Driver.Boot();f.Step(27f);Near(f.Driver.PipelineState.Lookahead.SignedWeight,0,"disable/enable no ghost state");
        }
    }
    private static void Warp()
    {
        using(var f=new Fixture())
        {
            f.Step(0,-1);f.Foot.position=Vector3.right;f.Step(.1f);Near(f.Camera.WarpCalls,0,"ordinary move no public warp");
            f.Foot.position=Vector3.right*100;f.Step(.2f);Near(f.Camera.WarpCalls,1,"one real teleport invokes public warp once");
            Near(f.Target.position.x,100,"teleport resets target to follow");Near(f.Driver.PipelineState.Lookahead.SignedWeight,0,"warp clears ahead");
        }
    }
    private static void Confiner()
    {
        foreach(bool first in new[]{true,false})using(var f=new Fixture(first))
        {
            if(!first){var c=new CinemachineConfiner2D();c.Boot(f.Camera.transform);}
            var list=f.Camera.Extensions;int driverIndex=list.IndexOf(f.Driver);
            var confiner=(CinemachineConfiner2D)list.Find(x=>x is CinemachineConfiner2D);confiner.MinX=-1;confiner.MaxX=1;confiner.Damping=0;
            f.State.PositionCorrection=Vector3.zero;f.Step(0,-1);f.Foot.position=Vector3.right*2;f.Step(.1f);
            Check(list.IndexOf(f.Driver)==driverIndex,"Driver never reflects/reorders extension list");
            Check(f.State.GetCorrectedPosition().x<=1.001,"actual Confiner callback still constrains final output");
        }
    }
#if !MOVING_LOOKAHEAD_PRODUCT
    private static void ProbeMigration()
    {
        StreetProbeDriverPolicy.Validate(new[]{new StreetProbeDriver {kind="StreetFollowDriver",enabled=true,sameCamera=true},
            new StreetProbeDriver {kind="CinemachineConfiner2D",enabled=true,sameCamera=true,sameBounds=true}});
        bool rejected=false;try{StreetProbeDriverPolicy.Validate(new[]{new StreetProbeDriver {kind="F01",enabled=true,sameCamera=true}});}catch(InvalidOperationException){rejected=true;}
        Check(rejected,"retired component kind cannot satisfy current binding policy");
        var rows=new[]{new StreetProbeObservation {followRTargetPipeline=true,phaseId="idle",settled=true,frame=1},
            new StreetProbeObservation {followRTargetPipeline=true,phaseId="idle",settled=true,frame=2},
            new StreetProbeObservation {followRTargetPipeline=true,phaseId="idle",settled=true,frame=3}};
        var checks=LongLoopProbeChecks.Evaluate(new StreetProbeRequest(),rows);
        var retired=checks.Where(c=>c.id.StartsWith("camera.")||c.id.StartsWith("channels.")||c.id=="move.edgeStop").ToArray();
        Check(retired.Length==12&&retired.All(c=>!c.observed&&!c.passed&&c.reason.StartsWith("NOT_APPLICABLE_FOLLOW_R")),"all twelve incompatible legacy predicates explicitly N/A, never PASS");
        Console.WriteLine("PROBE_NA count="+retired.Length+" fullProbeExecuted=false liveIdentityChanged=false");
    }
#endif
    private static void Diagnostics()
    {
        using(var f=new Fixture())
        {
            int logs=Debug.Lines.Count;f.Step(0,-1);f.Foot.position=Vector3.right;f.Step(.1f);
            Near(Debug.Lines.Count,logs,"silent default logging");Check(f.Debugger.LastResult.Target.Equals(f.Target.position),"read-only target diagnostic");
            Check(f.Debugger.LastSequence==f.Driver.EvaluationSequence,"diagnostic serial matches");
            f.Debugger.ConfigureLogging(true,1);f.Debugger.RequestSample();f.Step(.2f);Near(Debug.Lines.Count,logs+1,"explicit sample only");
            Check(f.Debugger.FormatLine().Contains("weight=")&&f.Debugger.FormatLine().Contains("goal="),"new target/phase diagnostics");
        }
    }
}
