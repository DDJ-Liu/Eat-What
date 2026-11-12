using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using EatWhat.LongLoop;
using EatWhat.LongLoop.Configuration;
using EatWhat.EditorValidation;
using UnityEngine;
using Unity.Cinemachine;
internal static class FollowRSmokeTests
{
    private static int checks,scenarios,failed;
    private static void Check(bool v,string text){checks++;if(!v)throw new Exception(text);}
    private static void Reject(Action a,string text){bool rejected=false;try{a();}catch(Exception){rejected=true;}Check(rejected,text);}
    private static void Case(string name,Action a){scenarios++;try{a();Console.WriteLine("FOLLOW-R PASS "+name);}catch(Exception e){failed++;Console.Error.WriteLine("FOLLOW-R FAIL "+name+": "+e);}}
    private static StreetProbeCleanup Clean()=>new StreetProbeCleanup {inputRestored=true,configsRestored=true,configOwnerRestored=true,callbacksRemoved=true,temporaryObjectsDestroyed=true,noForeignOverwrite=true};
    private static StreetProbeRequest Request(string project)=>new StreetProbeRequest {suite=FollowRSmokeSequence.Suite,taskId="AI-000999",attempt=1,executionId="fixture-execution",ownerThreadId="fixture-owner",runId="right-once",maximumSeconds=60,
        projectPath=project,scenePath="Assets/Scenes/LongLoop/LongLoop_Street.unity",outputRoot=Path.Combine(project,".ai-workspace/outputs/FOLLOW-R-20261110/AI-000999/attempt1/follow-r/right-once"),playerPath="player",cameraPath="camera",brainPath="brain"};
    private static StreetProbeObservation Observe(StreetFollowDriverTests.Fixture f)=>new StreetProbeObservation {ready=true,inside=true,inputOwned=true,followRTargetPipeline=true,frame=Time.frameCount,gameTime=Time.time,
        sequence=f.Driver.EvaluationSequence,producerFrame=Time.frameCount,producerSequence=f.Driver.EvaluationSequence,cmDampingDeltaTime=Time.deltaTime,producerTime=f.Driver.PipelineState.LastTime,producerDeltaTime=f.Driver.LastResult.GoalStep.Elapsed,stopped=(float)f.Driver.PipelineState.StoppedSeconds,foot=f.Foot.position,followPoint=f.Node.position,target=f.Target.position,brainOutput=f.State.GetFinalPosition()};
    private static void Configure(StreetFollowDriverTests.Fixture f,float build=2)
    {
        f.Config.Lookahead=4.5f;f.Config.LookaheadTime=build;f.Config.MaxHorizontalOffset=8;f.Config.DeadZoneWidth=.15f;f.Config.RecenterSpeed=.4f;f.Config.RecenterDelay=0;
        f.State.Lens=new LensSettings {Orthographic=true,OrthographicSize=10f,Aspect=16f/9f};f.State.PositionCorrection=new Vector3(0,2,0);f.State.RawPosition=new Vector3(0,f.Depth.Focus,-10);
        f.Step(0,-1);f.Camera.State=f.State;
    }
    private static StreetProbeReport Trajectory(int fps,string fault=null,float build=2,bool jitter=false,float delay=0,Func<int,double> intervals=null,float cmScale=1)
    {
        using(var f=new StreetFollowDriverTests.Fixture())
        {
            Configure(f,build);f.Config.RecenterDelay=delay;var s=CameraFollowConfigBinder.Snapshot(f.Config);var seq=new FollowRSmokeSequence(Request(Path.GetFullPath(".")),0,Observe(f),s,f.Config.DampingX,6.5f,2*f.State.Lens.OrthographicSize*f.State.Lens.Aspect);
            int i=0;bool injected=false;double frozen=0,elapsed=0;Vector3 heldTarget=Vector3.zero;bool hasHeldTarget=false;var position=f.Foot.position;
            while(!seq.Finished&&i<fps*60)
            {
                i++;float dt=intervals==null?(1f/fps)*(jitter?(i%2==0?1.1f:.9f):1):(float)intervals(i);elapsed+=dt;float t=(float)elapsed;
                if(seq.Input.x>0&&fault!="not-moving")position.x+=6.5f*dt;
                f.Foot.position=position;
                f.Step(t,dt*cmScale,x=>{if(fault=="target-jump"&&!injected&&t>=2){x.Target.position+=new Vector3(4.5f,0,0);injected=true;}});
                var sample=Observe(f);
                if(fault=="not-leading")sample.target=sample.followPoint;
                if(fault=="no-return"&&seq.Evidence.phase=="return"){if(!hasHeldTarget){heldTarget=sample.target;hasHeldTarget=true;}sample.target=heldTarget;}
                if(fault=="early-return"&&seq.Evidence.phase=="return")sample.target=sample.followPoint;
                if(fault=="sustained-offset"&&seq.Evidence.phase=="return"&&sample.stopped>=f.Config.RecenterDelay)sample.target=sample.followPoint+new Vector3(1,0,0);
                if(fault=="missing-frame"&&i==fps){sample.frame++;}
                if(fault=="bad-clock"&&i==fps)sample.gameTime=t-2;
                if(fault=="foreign-input"&&i==fps)sample.inputOwned=false;
                if(fault=="physical-jump"&&!injected&&t>=3.15f){sample.brainOutput+=new Vector3(3,0,0);injected=true;}
                seq.Tick(sample,t,f.Driver.PipelineState,x=>{});
                if(seq.Evidence.returnDeadline>0){if(frozen==0)frozen=seq.Evidence.returnDeadline;else Check(seq.Evidence.returnDeadline==frozen,"deadline never extended after observation");}
            }
            var report=seq.Report(Clean(),new StreetProbeCheck {id=FollowRSmokeSequence.RequiredIds[4],observed=true,passed=true,reason="EXPLICIT_OFFLINE_CONSOLE_FIXTURE"});
            if(fault==null)
            {
                Check(report.passed,"actual target/installed CM natural trajectory: "+report.error);
                Check(report.checks.Length==5&&report.requiredCount==5,"exact five frozen checks");
                Check(report.followR.returnDuration>8,"current .4 speed uses actual residual, not Delay+2");
                Check(report.followR.maxViolationRatio<=1,"frozen continuity threshold passes actual Composer");
                Console.WriteLine("trajectory fps="+fps+" return="+report.followR.returnDuration+" maxDV="+report.followR.maxDeltaVelocity+" ratio="+report.followR.maxViolationRatio+" samples="+report.followR.deltaVelocitySamples);
            }
            else Check(!report.passed,"fault must reject: "+fault);
            return report;
        }
    }
    private static void Identity()
    {
        string root=Path.GetFullPath(".");var r=Request(root);DateTimeOffset now=DateTimeOffset.Parse("2026-11-10T12:00:00Z");
        var scope=JObject.FromObject(new {paths=new[]{".ai-workspace/outputs/FOLLOW-R-20261110/AI-000999",r.scenePath,"Assets/Editor/ProjectValidation/LongLoopAutomatedProbe.cs","Assets/Editor/ProjectValidation/LongLoopProbeChecks.cs"},operations=new[]{FollowRSmokeSequence.RunOperation,FollowRSmokeSequence.ReadOperation,"single-play-and-stop"}});
        var t=JObject.FromObject(new {id=r.taskId,pipeline="ENGINE_CLI",status="RUNNING",attempts=1,execution=new {executionId=r.executionId,ownerThreadId=r.ownerThreadId,attempt=1,mode="MANUAL",limitReached=false}});t["businessScope"]=scope;
        var queue=new JObject { ["tasks"]=new JArray(t)};
        var worker=JObject.FromObject(new {status="BUSY",activeTaskId=r.taskId,activeExecutionId=r.executionId,threadId=r.ownerThreadId,executorKind="THREAD",leaseUntil=now.AddMinutes(20).ToString("o")});
        var workers=new JObject {["workers"]=new JObject {["ENGINE_CLI"]=worker}};
        var mode=JObject.Parse("{\"mode\":\"MANUAL\",\"supervised\":{\"status\":\"COMPLETE\",\"stopRequested\":true,\"exitRequested\":true}}");
        void Validate()=>FollowRSmokeSequence.ValidateRequest(r,root,"begin",queue,workers,mode,now);
        Validate();Check(true,"first claim businessScope without approvedScope allowed; old completed SUP stop ignored");
        void Bad(string label,Action mutate){var oldT=t.DeepClone();var oldW=worker.DeepClone();var oldM=mode.DeepClone();var oldR=UnityEngine.JsonUtility.ToJson(r);mutate();Reject(Validate,label);t.RemoveAll();t.Merge(oldT);worker.RemoveAll();worker.Merge(oldW);mode.RemoveAll();mode.Merge(oldM);r=UnityEngine.JsonUtility.FromJson<StreetProbeRequest>(oldR);}
        Bad("wrong task",()=>r.taskId="AI-000998");Bad("old execution",()=>r.executionId="old");Bad("attempt",()=>r.attempt=2);Bad("other owner",()=>r.ownerThreadId="other");
        Bad("expired lease",()=>worker["leaseUntil"]=now.ToString("o"));Bad("worker other execution",()=>worker["activeExecutionId"]="other");
        Bad("mode mismatch",()=>t["execution"]["mode"]="SUPERVISED");Bad("current mode changed",()=>mode["mode"]="SUPERVISED");
        Bad("real hard stop",()=>t["execution"]["hardStopAt"]=now.ToString("o"));Bad("limit",()=>t["execution"]["limitReached"]=true);Bad("retry stop",()=>t["retryHardStop"]="STOP");
        Bad("unknown limit",()=>((JObject)t["execution"]).Remove("limitReached"));Bad("missing business scope",()=>t.Remove("businessScope"));
        Bad("narrowed approved scope",()=>t["execution"]["approvedScope"]=JObject.FromObject(new {paths=new[]{"elsewhere"},operations=new[]{FollowRSmokeSequence.RunOperation,"single-play-and-stop"}}));
        Bad("narrowed operations",()=>t["execution"]["approvedScope"]=JObject.FromObject(new {paths=new[]{"."},operations=new[]{FollowRSmokeSequence.ReadOperation}}));
        Bad("arbitrary output",()=>r.outputRoot=Path.Combine(root,"release/elsewhere"));Bad("cross suite",()=>r.suite="SMK-011");Bad("stopped task",()=>t["status"]="NEEDS_USER");
        t["execution"]["approvedScope"]=scope.DeepClone();Validate();Check(true,"valid resumed intersection allowed");
        var context=new StreetPayloadContext {version=StreetPayloadContract.Version,implementationSha256=StreetPayloadContract.ImplementationSha256,taskId=r.taskId,attempt=1,executionId=r.executionId,ownerThreadId=r.ownerThreadId,runId=r.runId,sessionId="old-session"};
        Reject(()=>StreetPayloadContract.ValidateLive(context,queue,workers,mode,now),"old SUP API still refuses MANUAL");
        mode["mode"]="SUPERVISED";mode["supervised"]=JObject.FromObject(new {status="ACTIVE",sessionId="old-session",stopRequested=false,exitRequested=false,taskIds=new[]{r.taskId}});
        StreetPayloadContract.ValidateLive(context,queue,workers,mode,now);Check(true,"old exact SUP active branch retained");mode["supervised"]["stopRequested"]=true;
        Reject(()=>StreetPayloadContract.ValidateLive(context,queue,workers,mode,now),"old SUP true stop remains rejected");
    }
    private static void ConsoleWindows()
    {
        string project=Path.GetFullPath(".");var at=DateTimeOffset.Parse("2026-11-10T12:00:00Z");
        JObject Proof(int cursor,int since,string type="Log")=>JObject.FromObject(new {success=true,data=new {command="console",parameters=new {since=since.ToString()},target=new {projectPath=project},result=new {cursor,session="native-session",returned=cursor-since,dropped=false,reset=false,groundTruth=new {sampledUtc=at.AddSeconds(cursor-11).ToString("o")},entries=cursor==since?new object[0]:new[]{(object)new {seq=cursor,seeded=false,logType=type,level=type=="Error"?"error":"log"}}}}});
        var before=Proof(10,10);var after=Proof(11,10);
        StreetProbeCheck Run(JObject a,JObject b)=>FollowRSmokeSequence.ConsoleCheck(a,b,at,at,project,new FollowRSmokeEvidence());
        Check(Run(before,after).passed,"real-shaped contiguous cursor window");
        var errors=Proof(11,10,"Error");Check(!Run(before,errors).passed&&Run(before,errors).observed,"new Error never overwritten as zero");
        after["data"]["result"]["cursor"]=15;Check(!Run(before,after).observed,"history gap unknown, not zero");
        after=Proof(11,10);after["data"]["result"]["session"]="other";Check(!Run(before,after).observed,"session change unknown");
        Check(!Run(null,null).passed,"no return is unknown");after=Proof(11,10);after["data"]["result"]["reset"]=true;Check(!Run(before,after).observed,"console reset unknown");
        after=Proof(11,10);after["data"]["result"]["entries"][0]["seq"]=20;Check(!Run(before,after).observed,"entry gap unknown");
        after=Proof(11,10);after["data"]["result"]["entries"][0]["logType"]="unknown";Check(!Run(before,after).observed,"unknown severity refused");
        Check(!Run(before,Proof(11,9)).observed,"wrong since refused");
        var empty=Proof(10,10);empty["data"]["result"]["groundTruth"]["sampledUtc"]=at.ToString("o");empty["data"]["result"]["counts"]=new JObject {["error"]=9};Check(Run(before,empty).passed,"explicit empty current cursor window preserves nine historical errors, without rewriting counts");
    }
    private static void NativeLifecycle()
    {
        string root=Path.Combine(Path.GetFullPath(Environment.GetEnvironmentVariable("FOLLOW_R_FIXTURE_ROOT")),Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
        var r=Request(root);r.sources=new[]{"LongLoopAutomatedProbe.cs","LongLoopProbeChecks.cs"}.Select(n=>{string p=Path.Combine(root,"Assets/Editor/ProjectValidation",n);Directory.CreateDirectory(Path.GetDirectoryName(p));File.WriteAllText(p,"EXPLICIT FIXTURE SOURCE IDENTITY");return new StreetProbeFile {path=p,sha256=LongLoopAutomatedProbe.Hash(File.ReadAllBytes(p))};}).ToArray();
        LongLoopAutomatedProbe.Objects.Clear();LongLoopAutomatedProbe.followRSmoke=null;
        UnityEditor.EditorApplication.isPlaying=false;
        var edit=JObject.FromObject(LongLoopAutomatedProbe.FollowRReady(r));Check((bool)edit["ready"]==false,"Edit inspect reports wiring pending without Begin/creation");
        Check(!Directory.Exists(r.outputRoot),"Inspect creates no scene or run objects");UnityEditor.EditorApplication.isPlaying=true;
        using(var f=new StreetFollowDriverTests.Fixture())
        {
            Configure(f);var original=new NeutralInput();var avatar=new TomatoSpineAvatar();var p=new LongLoopPlayerController {transform=f.Foot,input=original,avatar=avatar};
            var brain=new CinemachineBrain {State=f.State};brain.OutputCamera.transform.position=f.State.GetFinalPosition();
            f.Camera.transform.components[typeof(StreetFollowDriver)]=f.Driver;
            LongLoopAutomatedProbe.Objects["player"]=p;LongLoopAutomatedProbe.Objects["camera"]=f.Camera;LongLoopAutomatedProbe.Objects["brain"]=brain;
            var first=LongLoopAutomatedProbe.BeginFollowR(r,DateTimeOffset.UtcNow.AddMinutes(20));var owned=p.input;
            Check(first.status=="RUNNING"&&owned!=original,"verbatim Begin installs one owned proxy");
            var duplicate=LongLoopAutomatedProbe.BeginFollowR(r,DateTimeOffset.UtcNow.AddMinutes(20));Check(ReferenceEquals(p.input,owned)&&CinemachineCore.CameraUpdatedEvent.Count==1,"repeated Begin returns same run without input/callback restart");
            var other=UnityEngine.JsonUtility.FromJson<StreetProbeRequest>(UnityEngine.JsonUtility.ToJson(r));other.runId="second";other.outputRoot=Path.Combine(Path.GetDirectoryName(r.outputRoot),"second");Reject(()=>LongLoopAutomatedProbe.BeginFollowR(other,DateTimeOffset.UtcNow.AddMinutes(20)),"same execution different run refused");
            LongLoopAutomatedProbe.followRSmoke.Stop("EXPLICIT_CANCEL");var result=LongLoopAutomatedProbe.followRSmoke.Report();
            Check(result.cleanup.Passed&&ReferenceEquals(p.input,original),"cancel restores owned input");
            Check(CinemachineCore.CameraUpdatedEvent.Count==0&&Application.Callbacks==0&&UnityEditor.EditorApplication.Callbacks==0,"cancel removes all owned callbacks");
            LongLoopAutomatedProbe.followRSmoke=null;Reject(()=>LongLoopAutomatedProbe.BeginFollowR(r,DateTimeOffset.UtcNow.AddMinutes(20)),"domain-reload marker prevents replay");
            var read=LongLoopAutomatedProbe.ReadSmoke(r);Check(read.status=="FAILED"&&ReferenceEquals(p.input,original),"Read preserved cancelled report, never Begin");
        }
        using(var f=new StreetFollowDriverTests.Fixture())
        {
            Configure(f);r=Request(root);r.runId="foreign";r.outputRoot=Path.Combine(root,"foreign-owned");var original=new NeutralInput();var avatar=new TomatoSpineAvatar();var p=new LongLoopPlayerController {transform=f.Foot,input=original,avatar=avatar};var brain=new CinemachineBrain {State=f.State};brain.OutputCamera.transform.position=f.State.GetFinalPosition();
            var run=new LongLoopAutomatedProbe.FollowRRun(r,"fixture-sha",p,f.Driver,f.Camera,brain,DateTimeOffset.UtcNow.AddMinutes(20));Directory.CreateDirectory(r.outputRoot);run.Start();
            var foreign=new NeutralInput();p.ConfigureInterfaces(foreign,avatar);run.Stop("FOREIGN");Check(ReferenceEquals(p.input,foreign)&&!run.Report().cleanup.noForeignOverwrite,"foreign replacement retained");
            Check(CinemachineCore.CameraUpdatedEvent.Count==0&&Application.Callbacks==0&&UnityEditor.EditorApplication.Callbacks==0,"foreign loss still detaches owned callbacks");
        }
    }
    private static void CallbackLifecycle()
    {
        foreach(string kind in new[]{"natural","user","claim","config","error"})using(var f=new StreetFollowDriverTests.Fixture())
        {
            Configure(f);LongLoopAutomatedProbe.ClaimAllowed=true;
            string root=Path.Combine(Path.GetFullPath(Environment.GetEnvironmentVariable("FOLLOW_R_FIXTURE_ROOT")),Guid.NewGuid().ToString("N"));var r=Request(root);Directory.CreateDirectory(r.outputRoot);
            var original=new NeutralInput();var avatar=new TomatoSpineAvatar();var p=new LongLoopPlayerController {transform=f.Foot,input=original,avatar=avatar};var b=new CinemachineBrain {State=f.State};b.OutputCamera.transform.position=f.State.GetFinalPosition();
            var run=new LongLoopAutomatedProbe.FollowRRun(r,"fixture-sha",p,f.Driver,f.Camera,b,DateTimeOffset.UtcNow.AddMinutes(20));run.Start();
            for(int i=1;i<=3600&&!run.Terminal;i++)
            {
                if(i==20)
                {
                    if(kind=="user")original.Value=1;
                    if(kind=="claim")LongLoopAutomatedProbe.ClaimAllowed=false;
                    if(kind=="config")f.Config.Lookahead=3;
                    if(kind=="error"){Application.Error();run.Stop("EXPLICIT_ERROR_TEST");}
                }
                if(run.Terminal)break;
                f.Foot.position+=new Vector3(p.input.Horizontal*6.5f/60,0,0);f.Step((float)i/60,1f/60);
                b.State=f.State;b.OutputCamera.transform.position=f.State.GetFinalPosition();CinemachineCore.CameraUpdatedEvent.Emit(b);Application.Render();UnityEditor.EditorApplication.Watch();
            }
            var result=run.Report();Check(run.Terminal,"bounded actual callback run terminal: "+kind);
            if(kind=="natural")Check(result.checks.Take(4).All(c=>c.observed&&c.passed)&&!result.passed&&result.status=="AWAITING_NATIVE_CONSOLE","natural callbacks cover first four; missing real Console cannot become pass");
            if(kind=="user")Check(result.error=="FOLLOW_R_USER_INPUT","actual user input stops owned run");
            if(kind=="claim")Check(result.error=="FOLLOW_R_LIVE_STATE_BUSY_TIMEOUT","persistent unknown claim stops at finite budget, no fresh permission assumed");
            if(kind=="config")Check(result.error=="FOLLOW_R_CONFIGURATION_CHANGED","config change invalidates frozen assumptions");
            if(kind=="error")Check(result.checks[4].observed&&!result.checks[4].passed&&result.followR.callbackErrors==1,"real callback Error is never zero");
            Check(ReferenceEquals(p.input,original)&&result.cleanup.Passed,"callback cleanup restores only original interface: "+kind);
            Check(CinemachineCore.CameraUpdatedEvent.Count==0&&Application.Callbacks==0&&UnityEditor.EditorApplication.Callbacks==0,"callback cleanup detaches all: "+kind);
        }
        LongLoopAutomatedProbe.ClaimAllowed=true;
    }
    private static void ChildFootBindingAndSampling()
    {
        using(var f=new StreetFollowDriverTests.Fixture())
        {
            var controlRoot=new Transform {localPosition=new Vector3(3,-2,1)};
            f.Foot.parent=controlRoot;f.Foot.localPosition=new Vector3(2.75f,-1.25f,.2f);
            Configure(f);
            var original=new NeutralInput();var avatar=new TomatoSpineAvatar();var p=new LongLoopPlayerController {transform=controlRoot,input=original,avatar=avatar};
            controlRoot.components[typeof(LongLoopPlayerController)]=p;f.Camera.transform.components[typeof(StreetFollowDriver)]=f.Driver;
            var brain=new CinemachineBrain {State=f.State};brain.OutputCamera.transform.position=f.State.GetFinalPosition();
            LongLoopAutomatedProbe.Objects.Clear();LongLoopAutomatedProbe.Objects["player"]=p;LongLoopAutomatedProbe.Objects["camera"]=f.Camera;LongLoopAutomatedProbe.Objects["brain"]=brain;
            var r=Request(Path.Combine(Path.GetFullPath(Environment.GetEnvironmentVariable("FOLLOW_R_FIXTURE_ROOT")),Guid.NewGuid().ToString("N")));
            bool Ready(out string reason)=>LongLoopAutomatedProbe.FollowRBindings(r,out var pp,out var dd,out var cc,out var bb,out reason);
            Check(Ready(out string reason),"actual child foot with nonzero XYZ offset is ready: "+reason);
            Check(f.Point.Foot==f.Foot&&f.Point.Foot!=p.transform,"author foot reference retained, control root distinct");
            p.walkArea.Predicate=v=>Math.Abs(v.x-p.transform.position.x)<.0001&&Math.Abs(v.y-p.transform.position.y)<.0001;
            Directory.CreateDirectory(r.outputRoot);var run=new LongLoopAutomatedProbe.FollowRRun(r,"fixture-child-foot",p,f.Driver,f.Camera,brain,DateTimeOffset.UtcNow.AddMinutes(20));run.Start();CinemachineCore.CameraUpdatedEvent.Emit(brain);Application.Render();
            var sampleMethod=typeof(LongLoopAutomatedProbe.FollowRRun).GetMethod("Sample",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            StreetProbeObservation Sample()=>(StreetProbeObservation)sampleMethod.Invoke(run,null);
            var first=run.Report().checks[0].observedSample;
            Check(first.ready&&first.inside&&(first.foot-f.Foot.position).magnitude<.0001,"initial sample uses actual Driver Foot, area uses mover root");
            Check((first.foot-controlRoot.position).magnitude>1,"nonzero offset cannot hide root/foot confusion");
            controlRoot.position+=new Vector3(.02f,0,0);f.Foot.localPosition+=new Vector3(.03f,0,0);f.Step(.02f,.02f);
            var next=Sample();Check(Math.Abs((next.foot-first.foot).x-.05)<.0001,"foot displacement includes actual local-anchor motion, not only .02 root motion");
            Check((next.followPoint-f.Node.position).magnitude<.0001&&(next.target-f.Driver.LastResult.Target).magnitude<.0001,"target/return observations share actual Driver foot geometry");
            Check(next.inside&&Math.Abs(p.walkArea.LastPosition.x-controlRoot.position.x)<.0001,"WalkArea tests controller movement coordinates rather than offset foot");
            p.targetRigidbody=new Rigidbody2D {position=new Vector2(40,7)};p.walkArea.Predicate=v=>Math.Abs(v.x-40)<.0001&&Math.Abs(v.y-7)<.0001;
            var rigid=Sample();Check(rigid.inside&&Math.Abs(p.walkArea.LastPosition.x-40)<.0001&&(rigid.foot-f.Foot.position).magnitude<.0001,"Rigidbody branch uses body coordinates for area, still actual Foot for camera observations");
            p.targetRigidbody=null;p.walkArea.Predicate=null;
            void RejectFoot(Transform foot,string label)
            {
                f.Point.Configure(foot,foot==null?f.Node:new Transform {parent=foot});
                Check(!Ready(out string why)&&why=="FOLLOW_R_SCENE_WIRING_PENDING",label+" rejected at ownership binding, not a later unsettled-state test");
                Check(!Sample().ready,label+" rejected in sampling too");
            }
            RejectFoot(new Transform(),"unrelated foot");
            var otherRoot=new Transform {parent=controlRoot};var other=new LongLoopPlayerController {transform=otherRoot};otherRoot.components[typeof(LongLoopPlayerController)]=other;
            RejectFoot(new Transform {parent=otherRoot},"other controller under same outer hierarchy");RejectFoot(null,"missing foot");
            run.Stop("EXPLICIT_FOOT_FIXTURE_END");Check(run.Report().cleanup.Passed&&ReferenceEquals(p.input,original),"child foot case retains existing owned cleanup");
        }
    }
    private static void StartupAndRejectedFrameEvidence()
    {
        foreach(string kind in new[]{"before-brain","after-brain","after-render","startup-frame-gap","startup-game-interval","missing-frame","clock-reverse","clock-stalled","sequence-stalled","long-frame","startup-timeout","startup-clock-reverse","startup-wall-nan","registered-wall-deadline"})using(var f=new StreetFollowDriverTests.Fixture())
        {
            Time.GameClockOverride=Time.WallClockOverride=null;
            try
            {
                Configure(f);var r=Request(Path.Combine(Path.GetFullPath(Environment.GetEnvironmentVariable("FOLLOW_R_FIXTURE_ROOT")),Guid.NewGuid().ToString("N")));Directory.CreateDirectory(r.outputRoot);
                var original=new NeutralInput();var p=new LongLoopPlayerController {transform=f.Foot,input=original,avatar=new TomatoSpineAvatar()};var b=new CinemachineBrain {State=f.State};b.OutputCamera.transform.position=f.State.GetFinalPosition();
                if(kind=="after-brain"||kind=="after-render")CinemachineCore.CameraUpdatedEvent.Emit(b);
                if(kind=="after-render")Application.Render();
                var run=new LongLoopAutomatedProbe.FollowRRun(r,"new-startup-boundary",p,f.Driver,f.Camera,b,DateTimeOffset.UtcNow.AddMinutes(20));run.Start();
                Check(run.Report().status=="RUNNING"&&run.Report().followR.phase=="awaiting-natural-baseline"&&p.input.Horizontal==0,"Begin only registers, input stays zero: "+kind);
                Application.Render();Check(run.Report().followR.baselineFrame==-1,"Render without a post-registration Brain does not establish baseline: "+kind);
                if(kind=="startup-timeout")
                {
                    Time.WallClockOverride=61;UnityEditor.EditorApplication.Watch();Check(run.Terminal&&run.Report().error=="FOLLOW_R_STARTUP_TIMEOUT_OR_CLOCK","startup does not reset global registration deadline");Check(run.Report().cleanup.Passed&&ReferenceEquals(p.input,original)&&CinemachineCore.CameraUpdatedEvent.Count==0&&Application.Callbacks==0&&UnityEditor.EditorApplication.Callbacks==0,"startup timeout cleans every owned callback/input");continue;
                }
                if(kind=="startup-clock-reverse"||kind=="startup-wall-nan")
                {
                    if(kind=="startup-clock-reverse")Time.GameClockOverride=-.01;else Time.WallClockOverride=double.NaN;
                    CinemachineCore.CameraUpdatedEvent.Emit(b);Application.Render();var bad=run.Report();
                    Check(run.Terminal&&bad.error==(kind=="startup-clock-reverse"?"FOLLOW_R_STARTUP_REVERSED_OR_INVALID_CLOCK_SEQUENCE":"FOLLOW_R_STARTUP_TIMEOUT_OR_CLOCK"),"invalid/reversed startup clock not excused by handshake");
                    Check(bad.followR.failureCurrentGameTime!=null||bad.followR.failureCurrentWallTime=="NaN","startup clock actual value retained");
                    Check(bad.cleanup.Passed&&ReferenceEquals(p.input,original)&&CinemachineCore.CameraUpdatedEvent.Count==0&&Application.Callbacks==0&&UnityEditor.EditorApplication.Callbacks==0,"invalid startup clock cleanup");continue;
                }
                if(kind=="startup-frame-gap")Time.frameCount+=2;
                float baseline=kind=="startup-game-interval"||kind=="registered-wall-deadline"?.14f:0;
                if(kind=="startup-frame-gap"||kind=="startup-game-interval"||kind=="registered-wall-deadline"){if(baseline==0)baseline=.05f;f.Step(baseline,.016f);b.State=f.State;b.OutputCamera.transform.position=f.State.GetFinalPosition();}
                CinemachineCore.CameraUpdatedEvent.Emit(b);Application.Render();var seeded=run.Report();
                Check(!run.Terminal&&seeded.followR.baselineFrame==Time.frameCount&&seeded.followR.initialPositionOnlySamples==1&&seeded.samplesSaved==0,"first complete natural frame establishes a single real baseline: "+kind);
                int baselineFrame=seeded.followR.baselineFrame;ulong baselineSequence=seeded.followR.baselineSequence;
                Application.Render();Check(run.Report().samplesSaved==0&&run.Report().followR.baselineFrame==baselineFrame,"duplicate Render same frame never advances/reseeds");
                if(kind=="registered-wall-deadline")
                {
                    Time.WallClockOverride=60.05;UnityEditor.EditorApplication.Watch();var expired=run.Report();
                    Check(run.Terminal&&expired.error=="FOLLOW_R_WALL_TIMEOUT_OR_CLOCK"&&expired.followR.failureCurrentWallTime!=null,"first baseline did not restart original registration deadline");
                    Check(expired.cleanup.Passed&&ReferenceEquals(p.input,original)&&CinemachineCore.CameraUpdatedEvent.Count==0,"post-baseline deadline cleanup");continue;
                }
                if(kind=="missing-frame")Time.frameCount++;
                float next=baseline+(kind=="long-frame"?.14f:.016f);
                if(kind=="sequence-stalled"){Time.time=next;Time.frameCount++;}else f.Step(next,.016f);
                if(kind=="clock-reverse")Time.GameClockOverride=baseline-.01;
                if(kind=="clock-stalled")Time.GameClockOverride=baseline;
                b.State=f.State;b.OutputCamera.transform.position=f.State.GetFinalPosition();CinemachineCore.CameraUpdatedEvent.Emit(b);Application.Render();
                var result=run.Report();string expected=kind=="missing-frame"?"FOLLOW_R_NONCONSECUTIVE_FRAME":kind=="sequence-stalled"?"FOLLOW_R_STALLED_OR_REVERSED_SEQUENCE":kind.StartsWith("clock-")?"FOLLOW_R_NONADVANCING_GAME_CLOCK":null;
                if(expected==null){Check(!run.Terminal&&result.samplesSaved==1,"first subsequent strict natural frame accepted: "+kind);run.Stop("EXPLICIT_CASE_END");}
                else
                {
                    Check(run.Terminal&&result.error==expected&&result.samplesSaved==0,"true post-baseline defect still rejects without inventing samples: "+kind);
                    Check(result.followR.failurePreviousFrame==baselineFrame&&result.followR.failureCurrentFrame==Time.frameCount&&result.followR.failurePreviousSequence==baselineSequence&&result.followR.failureCurrentGameTime!=null&&result.followR.failureDeltaTime!=null,"exact rejected frame/time/sequence tuple retained: "+kind);
                }
                Check(run.Report().cleanup.Passed&&ReferenceEquals(p.input,original),"startup/error cases release owned input: "+kind);
            }
            finally{Time.GameClockOverride=Time.WallClockOverride=null;}
            Check(CinemachineCore.CameraUpdatedEvent.Count==0&&Application.Callbacks==0&&UnityEditor.EditorApplication.Callbacks==0,"startup/errors remove every owned callback");
        }
    }
    private static void CurrentSupervisedRouting()
    {
        string root=Path.GetFullPath(".");var r=Request(root);var now=DateTimeOffset.Parse("2026-11-11T00:00:00Z");
        string modePath=Environment.GetEnvironmentVariable("FOLLOW_R_LIVE_MODE_PATH");
        var mode=File.Exists(modePath)?JObject.Parse(File.ReadAllText(modePath)):JObject.Parse("{\"mode\":\"SUPERVISED\",\"supervised\":{\"status\":\"ACTIVE\",\"sessionId\":\"explicit-isolated-session\",\"stopRequested\":false,\"exitRequested\":false,\"taskIds\":[]}}");
        Check((string)mode["mode"]=="SUPERVISED"&&(string)mode["supervised"]["status"]=="ACTIVE","current captured SUP mode format, not old COMPLETE session");
        string session=(string)mode["supervised"]["sessionId"];mode=(JObject)mode.DeepClone();mode["supervised"]["taskIds"]=new JArray(r.taskId); // Isolated ENGINE claim projection, never production runtime.
        var scope=JObject.FromObject(new {paths=new[]{".ai-workspace/outputs/FOLLOW-R-20261110/AI-000999",r.scenePath,"Assets/Editor/ProjectValidation/LongLoopAutomatedProbe.cs","Assets/Editor/ProjectValidation/LongLoopProbeChecks.cs"},operations=new[]{FollowRSmokeSequence.RunOperation,FollowRSmokeSequence.ReadOperation,"single-play-and-stop"}});
        var task=JObject.FromObject(new {id=r.taskId,pipeline="ENGINE_CLI",status="RUNNING",attempts=1,execution=new {executionId=r.executionId,ownerThreadId=r.ownerThreadId,attempt=1,mode="SUPERVISED",limitReached=false}});task["businessScope"]=scope;
        var queue=new JObject {["tasks"]=new JArray(task)};var worker=JObject.FromObject(new {status="BUSY",executorKind="THREAD",activeTaskId=r.taskId,activeExecutionId=r.executionId,threadId=r.ownerThreadId,leaseUntil=now.AddMinutes(20).ToString("o")});var workers=new JObject {["workers"]=new JObject {["ENGINE_CLI"]=worker}};
        void Validate()=>FollowRSmokeSequence.ValidateRequest(r,root,"begin",queue,workers,mode,now);
        Validate();Check(true,"current ACTIVE SUP route accepted through unchanged shared ValidateLive");
        mode["supervised"]["sessionId"]=null;Reject(Validate,"SUP unknown session rejected");mode["supervised"]["sessionId"]=session;
        foreach(string key in new[]{"stopRequested","exitRequested"}){mode["supervised"][key]=true;Reject(Validate,"SUP "+key);mode["supervised"][key]=false;}
        foreach(string state in new[]{"PAUSED","COMPLETE","WAITING_USER"}){mode["supervised"]["status"]=state;Reject(Validate,"SUP inactive "+state);}mode["supervised"]["status"]="ACTIVE";
        mode["supervised"]["taskIds"]=new JArray("AI-000998");Reject(Validate,"current task outside SUP scope");mode["supervised"]["taskIds"]=new JArray(r.taskId);
        worker["threadId"]="other";Reject(Validate,"SUP wrong worker owner");worker["threadId"]=r.ownerThreadId;
        task["execution"]["executionId"]="old";Reject(Validate,"SUP old execution");task["execution"]["executionId"]=r.executionId;
        task["execution"]["mode"]="MANUAL";Reject(Validate,"SUP execution/global mode mismatch");task["execution"]["mode"]="SUPERVISED";
        worker["leaseUntil"]=now.ToString("o");Reject(Validate,"SUP expired lease");worker["leaseUntil"]=now.AddMinutes(20).ToString("o");
        task["execution"]["approvedScope"]=JObject.FromObject(new {paths=new[]{"elsewhere"},operations=new[]{FollowRSmokeSequence.RunOperation,"single-play-and-stop"}});Reject(Validate,"SUP approvedScope intersection still enforced");
        Console.WriteLine("CURRENT SUP SESSION FORMAT "+session+" tested using isolated ENGINE claim projection; no production claim or mode write");
    }
    private static void VariableNaturalTimesteps()
    {
        foreach(var profile in new Func<int,double>[]{i=>.24980769557822669,i=>.33333334,i=>i%5==0?.24980769557822669:i%3==0?1d/30:1d/120})
        {
            var report=Trajectory(60,null,2,false,.8f,profile);Check(report.checks.Take(4).All(c=>c.observed&&c.passed),"four existing product observations under legal variable/long finite frames");
            Check(report.followR.delayWaitSamples>0&&report.followR.maxBeforeDelayTargetDrift<.05,"current Delay0.8 actually held in original return item");
            Trajectory(60,"no-return",2,false,.8f,profile);Trajectory(60,"sustained-offset",2,false,.8f,profile);Trajectory(60,"physical-jump",2,false,.8f,profile);Trajectory(60,"target-jump",2,false,.8f,profile);
        }
        Trajectory(60,null,2,false,.8f,null,5);Trajectory(60,null,2,false,.8f,null,.5f);
        var early=Trajectory(60,"early-return",2,false,.8f,i=>.24980769557822669);Check(early.error=="FOLLOW_R_RETURN_BEFORE_CONFIGURED_DELAY","Delay is an observed part of original recenter criterion, not only a term in upper deadline");
        using(var f=new StreetFollowDriverTests.Fixture())
        {
            Configure(f);var initial=Observe(f);initial.frame=5269;initial.sequence=5269;initial.gameTime=43.169026927437642;initial.producerTime=(double)(float)initial.gameTime;
            var seq=new FollowRSmokeSequence(Request(Path.GetFullPath(".")),0,initial,CameraFollowConfigBinder.Snapshot(f.Config),.5f,6.5f,2*f.State.Lens.OrthographicSize*f.State.Lens.Aspect);
            var next=UnityEngine.JsonUtility.FromJson<StreetProbeObservation>(UnityEngine.JsonUtility.ToJson(initial));next.frame=next.producerFrame=5270;next.sequence=next.producerSequence=5270;next.gameTime=43.418834623015869;next.cmDampingDeltaTime=(float)(next.gameTime-initial.gameTime);next.producerTime=(double)(float)next.gameTime;next.producerDeltaTime=next.producerTime-initial.producerTime;
            int saved=0;seq.Tick(next,.24980769557822669,f.Driver.PipelineState,x=>saved++);Check(!seq.Finished&&saved==1&&Math.Abs(seq.Evidence.maxObservedGameInterval-.24980769557822669)<1e-12,"exact known a5 tuple accepted with explicit isolated default-clock premises, never inventing a5 rejected pose/CM dt");
            Console.WriteLine("A5 KNOWN GAME TUPLE ACCEPTED; CM/producer fields explicit offline projection; actual old a5 CM dt remains UNKNOWN");
        }
    }
    private static void InvalidPairedClockStillFails()
    {
        foreach(string fault in new[]{"missing-cm","nan-cm","stale-pair","producer-stall","wall-limit"})using(var f=new StreetFollowDriverTests.Fixture())
        {
            Configure(f);var seq=new FollowRSmokeSequence(Request(Path.GetFullPath(".")),0,Observe(f),CameraFollowConfigBinder.Snapshot(f.Config),.5f,6.5f,2*f.State.Lens.OrthographicSize*f.State.Lens.Aspect);
            f.Step(.25f,.25f);var sample=Observe(f);if(fault=="missing-cm")sample.cmDampingDeltaTime=0;if(fault=="nan-cm")sample.cmDampingDeltaTime=double.NaN;if(fault=="stale-pair")sample.producerFrame--;if(fault=="producer-stall")sample.producerDeltaTime=0;
            seq.Tick(sample,fault=="wall-limit"?61:.25,f.Driver.PipelineState,x=>{});Check(seq.Finished,"invalid clock/global deadline still fail: "+fault);
            Check(fault=="wall-limit"?seq.Error=="FOLLOW_R_WALL_TIMEOUT_OR_CLOCK":seq.Error=="FOLLOW_R_INVALID_OR_UNPAIRED_PRODUCER_CLOCK","specific clock reason retained: "+fault);
        }
    }
    private sealed class NeutralInput:EatWhat.Toolkit.Input.IGameInput
    {public float Value;public float Horizontal=>Value;public float Vertical=>0;public Vector2 LookAxis=>Vector2.zero;public EatWhat.Toolkit.Input.GameInputDevice CurrentDevice=>EatWhat.Toolkit.Input.GameInputDevice.Keyboard;public bool Interact=>false;public Vector2 MouseScreenPos=>Vector2.zero;}
    private static int OldStartupCounterexamples()
    {
        foreach(string kind in new[]{"startup-frame-gap","startup-game-interval"})using(var f=new StreetFollowDriverTests.Fixture())
        {
            Configure(f);var r=Request(Path.Combine(Path.GetFullPath(Environment.GetEnvironmentVariable("FOLLOW_R_FIXTURE_ROOT")),Guid.NewGuid().ToString("N")));Directory.CreateDirectory(r.outputRoot);
            var original=new NeutralInput();var p=new LongLoopPlayerController {transform=f.Foot,input=original,avatar=new TomatoSpineAvatar()};var b=new CinemachineBrain {State=f.State};b.OutputCamera.transform.position=f.State.GetFinalPosition();
            var run=new LongLoopAutomatedProbe.FollowRRun(r,"old-production-counterexample",p,f.Driver,f.Camera,b,DateTimeOffset.UtcNow.AddMinutes(20));run.Start();
            if(kind=="startup-frame-gap")Time.frameCount+=2;
            float next=kind=="startup-frame-gap"?.05f:.14f;f.Step(next,.016f);b.State=f.State;b.OutputCamera.transform.position=f.State.GetFinalPosition();CinemachineCore.CameraUpdatedEvent.Emit(b);Application.Render();
            var result=run.Report();Check(run.Terminal&&result.error=="FOLLOW_R_MISSING_FRAME_OR_CLOCK_OR_SAMPLE_LIMIT"&&result.samplesSaved==0,"old actual synchronous baseline rejects legal first complete natural frame: "+kind);
            Check(result.cleanup.Passed&&ReferenceEquals(p.input,original),"old failure still cleans owned input");
            Console.WriteLine("EXPECTED OLD STARTUP REJECTION "+kind+": "+result.error+" samples="+result.samplesSaved+"; actual production bodies; does not identify original a4 predicate");
        }
        return 0;
    }
    private static int OldTimestepCounterexamples()
    {
        using(var f=new StreetFollowDriverTests.Fixture())
        {
            Configure(f);var r=Request(Path.GetFullPath("."));var initial=Observe(f);initial.frame=5269;initial.sequence=5269;initial.gameTime=43.169026927437642;
            var seq=new FollowRSmokeSequence(r,0,initial,CameraFollowConfigBinder.Snapshot(f.Config),.5f,6.5f,2*f.State.Lens.OrthographicSize*f.State.Lens.Aspect);
            var next=UnityEngine.JsonUtility.FromJson<StreetProbeObservation>(UnityEngine.JsonUtility.ToJson(initial));next.frame=5270;next.sequence=5270;next.gameTime=43.418834623015869;
            seq.Tick(next,.24980769557822669,f.Driver.PipelineState,x=>{});
            Check(seq.Finished&&seq.Error=="FOLLOW_R_GAME_FRAME_INTERVAL_EXCEEDED","exact a5 frame/time/sequence tuple rejects solely at old .1 gate (other pose fields isolated copies, not invented native data)");
            Console.WriteLine("EXPECTED OLD A5 TUPLE REJECTION frames5269->5270 sequence5269->5270 game43.169026927437642->43.418834623015869 dt="+(next.gameTime-initial.gameTime).ToString("R")+" "+seq.Error);
        }
        using(var f=new StreetFollowDriverTests.Fixture())
        {
            Configure(f);var seq=new FollowRSmokeSequence(Request(Path.GetFullPath(".")),0,Observe(f),CameraFollowConfigBinder.Snapshot(f.Config),.5f,6.5f,2*f.State.Lens.OrthographicSize*f.State.Lens.Aspect);
            for(int i=1;i<600&&!seq.Finished;i++)
            {
                float dt=1f/60;f.Foot.position+=new Vector3(seq.Input.x*6.5f*dt,0,0);f.Step(i*dt,dt*5);seq.Tick(Observe(f),i*dt,f.Driver.PipelineState,x=>{});
            }
            Check(seq.Error=="FOLLOW_R_HORIZONTAL_VELOCITY_JUMP","old probe assumes game interval equals CM damping dt, rejects actual installed Composer under legal independent effective CM dt");
            Console.WriteLine("EXPECTED OLD CLOCK-COEFFICIENT REJECTION: "+seq.Error+"; explicit offline CM/game clock boundary, not claimed original a5 cause");
        }
        return 0;
    }
    private sealed class SnapshotFixture:IDisposable
    {
        public readonly string Root;public readonly StreetProbeRequest Request;
        public readonly JObject Queue,Workers,Mode;
        public JObject Task=>(JObject)Queue["tasks"][0];public JObject Worker=>(JObject)Workers["workers"]["ENGINE_CLI"];
        private readonly string oldData=Application.dataPath,oldScene=UnityEngine.SceneManagement.SceneManager.CurrentPath;
        public SnapshotFixture()
        {
            Root=Path.Combine(Path.GetFullPath(Environment.GetEnvironmentVariable("FOLLOW_R_FIXTURE_ROOT")),"snapshot-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(Root,"Assets"));Directory.CreateDirectory(Path.Combine(Root,".ai-workspace/runtime"));Request=FollowRSmokeTests.Request(Root);
            using(var process=System.Diagnostics.Process.GetCurrentProcess()){Request.editorPid=process.Id;Request.editorStartedAt=process.StartTime.ToUniversalTime().ToString("o");}
            var scope=JObject.FromObject(new {paths=new[]{".ai-workspace/outputs/FOLLOW-R-20261110/AI-000999",Request.scenePath,"Assets/Editor/ProjectValidation/LongLoopAutomatedProbe.cs","Assets/Editor/ProjectValidation/LongLoopProbeChecks.cs"},operations=new[]{FollowRSmokeSequence.RunOperation,FollowRSmokeSequence.ReadOperation,"single-play-and-stop"}});
            Queue=new JObject {["tasks"]=new JArray(JObject.FromObject(new {id=Request.taskId,pipeline="ENGINE_CLI",status="RUNNING",attempts=1,execution=new {executionId=Request.executionId,ownerThreadId=Request.ownerThreadId,attempt=1,mode="SUPERVISED",limitReached=false},businessScope=scope}))};
            Workers=new JObject {["workers"]=new JObject {["ENGINE_CLI"]=JObject.FromObject(new {status="BUSY",executorKind="THREAD",activeTaskId=Request.taskId,activeExecutionId=Request.executionId,threadId=Request.ownerThreadId,leaseUntil=DateTimeOffset.UtcNow.AddMinutes(20).ToString("o")})}};
            Mode=JObject.FromObject(new {mode="SUPERVISED",supervised=new {sessionId="isolated-snapshot-session",status="ACTIVE",taskIds=new[]{Request.taskId},stopRequested=false,exitRequested=false}});
            Write();Application.dataPath=Path.Combine(Root,"Assets");UnityEngine.SceneManagement.SceneManager.CurrentPath=Request.scenePath;LongLoopAutomatedProbe.UseRealClaimValidation=true;
        }
        public void Write()
        {File.WriteAllText(Path.Combine(Root,".ai-workspace/runtime/queue-state.json"),Queue.ToString());File.WriteAllText(Path.Combine(Root,".ai-workspace/runtime/worker-state.json"),Workers.ToString());File.WriteAllText(Path.Combine(Root,".ai-workspace/runtime/mode-state.json"),Mode.ToString());}
        public void Dispose(){Application.dataPath=oldData;UnityEngine.SceneManagement.SceneManager.CurrentPath=oldScene;LongLoopAutomatedProbe.UseRealClaimValidation=false;LongLoopAutomatedProbe.ClaimAllowed=true;Time.GameClockOverride=Time.WallClockOverride=null;}
    }
    private sealed class HeldQueueMutex:IDisposable
    {
        private readonly System.Threading.ManualResetEvent ready=new System.Threading.ManualResetEvent(false),release=new System.Threading.ManualResetEvent(false);
        private readonly System.Threading.Thread thread;private Exception error;
        public HeldQueueMutex()
        {
            thread=new System.Threading.Thread(()=>{using(var m=new System.Threading.Mutex(false,@"Local\EatWhat_AI_Workspace_Queue_v1")){bool taken=false;try{try{taken=m.WaitOne(2000);}catch(System.Threading.AbandonedMutexException){taken=true;}if(!taken)throw new TimeoutException("Offline holder could not acquire queue mutex");ready.Set();if(!release.WaitOne(5000))throw new TimeoutException("Offline holder bounded release timeout");}catch(Exception e){error=e;ready.Set();}finally{if(taken)m.ReleaseMutex();}}});thread.IsBackground=true;thread.Start();
            if(!ready.WaitOne(2500)||error!=null){release.Set();thread.Join(2500);throw new InvalidOperationException("Offline mutex holder setup failed",error);}
        }
        public void Dispose(){release.Set();if(!thread.Join(2500))throw new TimeoutException("Offline holder did not release");ready.Dispose();release.Dispose();if(error!=null)throw new InvalidOperationException("Offline holder failed",error);}
    }
    private static int OldSnapshotCounterexample()
    {
        using(var state=new SnapshotFixture())using(var f=new StreetFollowDriverTests.Fixture())
        {
            Configure(f);Directory.CreateDirectory(state.Request.outputRoot);
            Check(LongLoopAutomatedProbe.ActualValidateSmokeRequest(state.Request,true,"frame"),"actual old current validation succeeds before contention");
            var original=new NeutralInput();var p=new LongLoopPlayerController {transform=f.Foot,input=original,avatar=new TomatoSpineAvatar()};var b=new CinemachineBrain {State=f.State};
            var run=new LongLoopAutomatedProbe.FollowRRun(state.Request,"old-snapshot-counterexample",p,f.Driver,f.Camera,b,DateTimeOffset.UtcNow.AddMinutes(20));run.Start();
            using(var held=new HeldQueueMutex())
            {
                JObject q,w,m;Check(!StreetPayloadContract.TryReadLiveState(state.Root,out q,out w,out m)&&q==null&&w==null&&m==null,"actual production nonblocking snapshot reports busy without fabricated state");
                Time.WallClockOverride=.5;UnityEditor.EditorApplication.Watch();
                Check(run.Terminal&&run.Report().error=="FOLLOW_R_LIVE_STATE_UNAVAILABLE","old actual Watch permanently stops on transient mutex contention");
                Check(run.Report().cleanup.Passed&&ReferenceEquals(p.input,original),"old contention path cleans original input/callbacks");
            }
            Check(LongLoopAutomatedProbe.ActualValidateSmokeRequest(state.Request,true,"frame"),"same actual helper recovers immediately after release; old run stays terminal");
            Console.WriteLine("EXPECTED OLD SNAPSHOT CONTENTION: actual TryReadLiveState WaitOne(0) busy -> verbatim old Watch FOLLOW_R_LIVE_STATE_UNAVAILABLE; helper fresh after release, original run remains terminal; exact old lock owner/duration UNKNOWN");
        }
        return 0;
    }
    private static void SnapshotBusyAndFreshRecovery()
    {
        foreach(bool persistent in new[]{false,true})using(var state=new SnapshotFixture())using(var f=new StreetFollowDriverTests.Fixture())
        {
            Configure(f);Directory.CreateDirectory(state.Request.outputRoot);DateTimeOffset lease;
            Check(LongLoopAutomatedProbe.ActualValidateFollowRClaim(state.Request,"begin",out lease),"fresh actual helper validates isolated current claim/lease");
            var original=new NeutralInput();var p=new LongLoopPlayerController {transform=f.Foot,input=original,avatar=new TomatoSpineAvatar()};var b=new CinemachineBrain {State=f.State};
            var run=new LongLoopAutomatedProbe.FollowRRun(state.Request,"snapshot-budget",p,f.Driver,f.Camera,b,lease);run.Start();
            using(var held=new HeldQueueMutex())
            {
                foreach(double wall in new[]{.5,.6,.9}){Time.WallClockOverride=wall;UnityEditor.EditorApplication.Watch();var e=run.Report().followR;Check(!run.Terminal&&e.snapshotFreshValidations==1&&e.snapshotRecoveries==0&&e.snapshotBusyDeadline==1,"busy is not fresh and repeated observations cannot reset original due+.5 budget");}
                if(persistent){Time.WallClockOverride=1;UnityEditor.EditorApplication.Watch();Check(run.Terminal&&run.Report().error=="FOLLOW_R_LIVE_STATE_BUSY_TIMEOUT","persistent actual mutex contention stops at fixed budget");}
            }
            if(!persistent){Time.WallClockOverride=.95;UnityEditor.EditorApplication.Watch();var e=run.Report().followR;Check(!run.Terminal&&e.snapshotFreshValidations==2&&e.snapshotRecoveries==1&&e.snapshotLastFreshWall==.95&&e.snapshotState=="FRESH_RECOVERED","actual fresh recovery alone resets normal cadence");run.Stop("OFFLINE_RECOVERY_OBSERVED");}
            Check(run.Report().cleanup.Passed&&ReferenceEquals(p.input,original),"bounded busy/recovery cleans original input");
        }
    }
    private static void SnapshotNaturalFrames(bool terminalBusy)
    {
        using(var state=new SnapshotFixture())using(var f=new StreetFollowDriverTests.Fixture())
        {
            Configure(f);f.Config.RecenterDelay=.8f;Directory.CreateDirectory(state.Request.outputRoot);DateTimeOffset lease;Check(LongLoopAutomatedProbe.ActualValidateFollowRClaim(state.Request,"begin",out lease),"real begin lease for natural fixture");
            var original=new NeutralInput();var p=new LongLoopPlayerController {transform=f.Foot,input=original,avatar=new TomatoSpineAvatar()};var b=new CinemachineBrain {State=f.State};
            var run=new LongLoopAutomatedProbe.FollowRRun(state.Request,"snapshot-natural",p,f.Driver,f.Camera,b,lease);run.Start();HeldQueueMutex held=null;bool injected=false;
            try
            {
                for(int i=1;i<=3600&&!run.Terminal;i++)
                {
                    if(!terminalBusy&&i==30||terminalBusy&&!injected&&i>300&&(f.Target.position-f.Node.position).magnitude<.06){held=new HeldQueueMutex();injected=true;}
                    if(!terminalBusy&&i==45){held.Dispose();held=null;}
                    f.Foot.position+=new Vector3(p.input.Horizontal*6.5f/60,0,0);f.Step((float)i/60,1f/60);b.State=f.State;b.OutputCamera.transform.position=f.State.GetFinalPosition();CinemachineCore.CameraUpdatedEvent.Emit(b);Application.Render();UnityEditor.EditorApplication.Watch();
                }
            }
            finally{held?.Dispose();if(!run.Terminal)run.Stop("OFFLINE_FIXTURE_CLEANUP");}
            var result=run.Report();Check(injected&&result.cleanup.Passed&&ReferenceEquals(p.input,original),"actual callback contention fixture cleanup");
            if(terminalBusy)Check(result.error=="FOLLOW_R_TERMINAL_SNAPSHOT_BUSY"&&!result.passed,"completion cannot claim successful fresh validation while snapshot busy; no wait/extension");
            else Check(result.checks.Take(4).All(c=>c.observed&&c.passed)&&result.status=="AWAITING_NATIVE_CONSOLE"&&result.followR.snapshotRecoveries==1,"actual natural chronology and original first four survive finite contention/recovery");
            var samples=File.ReadAllLines(Path.Combine(state.Request.outputRoot,"samples.jsonl")).Select(JObject.Parse).ToArray();
            Check(samples.Length==result.samplesSaved&&samples.Length==result.followR.velocitySamples,"all actual offline callback samples retained; none invented/skipped");
            for(int i=1;i<samples.Length;i++)Check((int)samples[i]["frame"]==(int)samples[i-1]["frame"]+1,"natural consecutive samples retained across busy interval");
        }
    }
    private static void SnapshotRealRejections()
    {
        foreach(string fault in new[]{"stop","exit","paused","owner","execution","task","lease","scope","malformed","duplicate","busy-stop","editor","scene"})using(var state=new SnapshotFixture())using(var f=new StreetFollowDriverTests.Fixture())
        {
            Configure(f);Directory.CreateDirectory(state.Request.outputRoot);DateTimeOffset lease;Check(LongLoopAutomatedProbe.ActualValidateFollowRClaim(state.Request,"begin",out lease),"fresh valid before mutation: "+fault);
            var original=new NeutralInput();var p=new LongLoopPlayerController {transform=f.Foot,input=original,avatar=new TomatoSpineAvatar()};var b=new CinemachineBrain {State=f.State};
            var run=new LongLoopAutomatedProbe.FollowRRun(state.Request,"snapshot-invalid",p,f.Driver,f.Camera,b,lease);run.Start();
            if(fault=="busy-stop")using(var held=new HeldQueueMutex()){Time.WallClockOverride=.5;UnityEditor.EditorApplication.Watch();Check(!run.Terminal,"finite busy before stop recovery");}
            if(fault=="stop"||fault=="busy-stop")state.Mode["supervised"]["stopRequested"]=true;
            if(fault=="exit")state.Mode["supervised"]["exitRequested"]=true;
            if(fault=="paused")state.Mode["supervised"]["status"]="PAUSED";
            if(fault=="owner")state.Worker["threadId"]="foreign";
            if(fault=="execution")state.Task["execution"]["executionId"]="other";
            if(fault=="task")state.Task["status"]="NEEDS_USER";
            if(fault=="lease")state.Worker["leaseUntil"]=DateTimeOffset.UtcNow.AddSeconds(-1).ToString("o");
            if(fault=="scope")state.Task["businessScope"]["operations"]=new JArray(FollowRSmokeSequence.ReadOperation);
            state.Write();
            if(fault=="malformed")File.WriteAllText(Path.Combine(state.Root,".ai-workspace/runtime/queue-state.json"),"{invalid");
            if(fault=="duplicate")File.WriteAllText(Path.Combine(state.Root,".ai-workspace/runtime/mode-state.json"),"{\"mode\":\"SUPERVISED\",\"mode\":\"MANUAL\"}");
            if(fault=="editor")state.Request.editorPid++;
            if(fault=="scene")UnityEngine.SceneManagement.SceneManager.CurrentPath="other";
            Time.WallClockOverride=fault=="busy-stop"?.75:.5;UnityEditor.EditorApplication.Watch();var result=run.Report();
            Check(run.Terminal&&result.error.StartsWith("FOLLOW_R_CURRENT_VALIDATION_FAILED:")&&result.followR.snapshotValidationFailures==1&&result.followR.snapshotFreshValidations==1,"fresh real mismatch/malformed state never becomes busy grace or success: "+fault);
            Check(result.cleanup.Passed&&ReferenceEquals(p.input,original),"fresh rejection cleanup: "+fault);
        }
        using(var state=new SnapshotFixture())using(var f=new StreetFollowDriverTests.Fixture())
        {
            Configure(f);Directory.CreateDirectory(state.Request.outputRoot);state.Worker["leaseUntil"]=DateTimeOffset.UtcNow.AddSeconds(1).ToString("o");state.Write();DateTimeOffset lease;Check(LongLoopAutomatedProbe.ActualValidateFollowRClaim(state.Request,"begin",out lease),"actual finite cached lease");
            var p=new LongLoopPlayerController {transform=f.Foot,input=new NeutralInput(),avatar=new TomatoSpineAvatar()};var b=new CinemachineBrain {State=f.State};var run=new LongLoopAutomatedProbe.FollowRRun(state.Request,"snapshot-expiry",p,f.Driver,f.Camera,b,lease);run.Start();
            using(var held=new HeldQueueMutex()){Time.WallClockOverride=.5;UnityEditor.EditorApplication.Watch();Check(!run.Terminal&&run.Report().followR.snapshotBusyObservations>0,"busy initially within actual cached lease");int wait=(int)Math.Ceiling((lease-DateTimeOffset.UtcNow).TotalMilliseconds)+10;if(wait>0)System.Threading.Thread.Sleep(wait);Time.WallClockOverride=.55;UnityEditor.EditorApplication.Watch();Check(run.Terminal&&run.Report().error=="FOLLOW_R_LAST_VALIDATED_LEASE_EXPIRED","cached real UTC lease expiry stops before busy budget, without unavailable fresh read");}
            Check(run.Report().cleanup.Passed,"expired cached authority cleanup");
        }
    }
    private sealed class InputContextFixture:IDisposable
    {
        public readonly EatWhat.Toolkit.Input.LegacyInputProvider Provider=new EatWhat.Toolkit.Input.LegacyInputProvider();
        public InputContextFixture(){Screen.width=1600;Screen.height=900;Input.Held.Clear();Input.Down.Clear();Input.mousePosition=new Vector3(1200,700,0);Focus(true);}
        public void Focus(bool focused){Application.isFocused=focused;typeof(EatWhat.Toolkit.Input.LegacyInputProvider).GetMethod("OnApplicationFocus",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(Provider,new object[]{focused});}
        public void Dispose(){Application.isFocused=true;Screen.width=1600;Screen.height=900;Input.Held.Clear();Input.Down.Clear();Input.mousePosition=Vector3.zero;Time.GameClockOverride=Time.WallClockOverride=null;}
    }
    private static int OldInputCounterexamples()
    {
        foreach(string kind in new[]{"focus","size"})using(var context=new InputContextFixture())using(var f=new StreetFollowDriverTests.Fixture())
        {
            Configure(f);var r=Request(Path.Combine(Path.GetFullPath(Environment.GetEnvironmentVariable("FOLLOW_R_FIXTURE_ROOT")),Guid.NewGuid().ToString("N")));Directory.CreateDirectory(r.outputRoot);
            var p=new LongLoopPlayerController {transform=f.Foot,input=context.Provider,avatar=new TomatoSpineAvatar()};var b=new CinemachineBrain {State=f.State};var oldLook=context.Provider.LookAxis;
            var run=new LongLoopAutomatedProbe.FollowRRun(r,"old-input-context-counterexample",p,f.Driver,f.Camera,b,DateTimeOffset.UtcNow.AddMinutes(20));run.Start();
            if(kind=="focus")context.Focus(false);else {Screen.width=1920;Screen.height=1080;}
            Check(Input.Held.Count==0&&Input.Down.Count==0&&Input.mousePosition.x==1200&&Input.mousePosition.y==700,"fixture has no key or physical mouse movement");
            Check(Math.Abs(context.Provider.LookAxis.x-oldLook.x)+Math.Abs(context.Provider.LookAxis.y-oldLook.y)>.001,"actual product provider derives different normalized Look from context alone");
            Time.WallClockOverride=.01;UnityEditor.EditorApplication.Watch();
            Check(run.Terminal&&run.Report().error=="FOLLOW_R_USER_INPUT"&&run.Report().samplesSaved==0,"verbatim old production input monitor falsely marks context as user input: "+kind);
            Check(run.Report().cleanup.Passed&&ReferenceEquals(p.input,context.Provider),"old input error cleans owned interfaces/callbacks");
            Console.WriteLine("EXPECTED OLD INPUT CONTEXT FALSE POSITIVE "+kind+": actual LegacyInputProvider and old production Watch; no physical operation in isolated shell. Actual old a7 trigger remains UNKNOWN.");
        }
        return 0;
    }
    private static void InputContextSemantics()
    {
        foreach(string kind in new[]{"size","focus","stable-mouse","size-mouse","regain-mouse","focus-key","A","W","E","Escape","initial-unfocused"})using(var context=new InputContextFixture())using(var f=new StreetFollowDriverTests.Fixture())
        {
            Configure(f);if(kind=="initial-unfocused")context.Focus(false);
            var r=Request(Path.Combine(Path.GetFullPath(Environment.GetEnvironmentVariable("FOLLOW_R_FIXTURE_ROOT")),Guid.NewGuid().ToString("N")));Directory.CreateDirectory(r.outputRoot);
            var p=new LongLoopPlayerController {transform=f.Foot,input=context.Provider,avatar=new TomatoSpineAvatar()};var b=new CinemachineBrain {State=f.State};var run=new LongLoopAutomatedProbe.FollowRRun(r,"input-observation",p,f.Driver,f.Camera,b,DateTimeOffset.UtcNow.AddMinutes(20));run.Start();
            if(kind=="size"||kind=="size-mouse"){Screen.width=1920;Screen.height=1080;}
            if(kind=="focus"||kind=="regain-mouse"){context.Focus(false);Time.WallClockOverride=.01;UnityEditor.EditorApplication.Watch();Check(!run.Terminal&&run.Report().followR.inputLastObservation.context=="LEGACY_NEUTRAL_GATED_OUTPUT","actual neutral focus gating is context, not an input grace/rebase");context.Focus(true);}
            if(kind=="initial-unfocused")context.Focus(true);
            if(kind=="stable-mouse"||kind=="size-mouse"||kind=="regain-mouse")Input.mousePosition+=new Vector3(50,0,0);
            if(kind=="focus-key"){Application.isFocused=false;Input.Held.Add(KeyCode.A);}
            if(kind=="A")Input.Held.Add(KeyCode.A);if(kind=="W")Input.Held.Add(KeyCode.W);if(kind=="E")Input.Down.Add(KeyCode.E);if(kind=="Escape")Input.Down.Add(KeyCode.Escape);
            Time.WallClockOverride=.02;UnityEditor.EditorApplication.Watch();var result=run.Report();
            if(kind=="size"||kind=="focus")
            {Check(!run.Terminal&&result.followR.inputLastObservation.reason=="NO_OBSERVED_INPUT","same physical mouse with current focus/size no longer trips old Look gate: "+kind);run.Stop("OFFLINE_INPUT_CONTEXT_OBSERVED");}
            else
            {
                Check(run.Terminal&&result.error==(kind=="initial-unfocused"?"FOLLOW_R_INPUT_CONTEXT_UNAVAILABLE":"FOLLOW_R_USER_INPUT"),"real input or explicitly unobservable initial context stops: "+kind);
                string expected=kind=="A"||kind=="focus-key"?"HORIZONTAL":kind=="W"?"VERTICAL":kind=="E"?"INTERACT":kind=="Escape"?"BACK":kind=="initial-unfocused"?"NO_PRIOR_ACTIVE_MOUSE_REFERENCE":"LOOK_AXIS";
                Check(result.followR.inputAtStop.reason==expected&&result.followR.inputAtStop.complete,"exact triggering observation retained: "+kind);
            }
            Check(run.Report().cleanup.Passed&&ReferenceEquals(p.input,context.Provider),"context/input cleanup uses actual original provider: "+kind);
            Check(run.Report().followR.inputAtRegistration.inputType.Contains("LegacyInputProvider"),"actual original input type retained");
        }
    }
    private sealed class CountedInput:EatWhat.Toolkit.Input.IGameInput,EatWhat.Toolkit.Input.INavigationInput
    {
        public readonly System.Collections.Generic.Dictionary<string,int> Reads=new System.Collections.Generic.Dictionary<string,int>();
        public float H,V;public bool Press,Back,ThrowLook;public Vector2 Look,Mouse;public EatWhat.Toolkit.Input.GameInputDevice Device;
        private T Read<T>(string key,T value){Reads[key]=Reads.TryGetValue(key,out var n)?n+1:1;return value;}
        public float Horizontal=>Read("H",H);public float Vertical=>Read("V",V);public bool Interact=>Read("Interact",Press);public bool BackPressed=>Read("Back",Back);
        public Vector2 LookAxis{get{Read("Look",0);if(ThrowLook)throw new InvalidOperationException("ISOLATED_LOOK_READ_FAILURE");return Look;}}
        public Vector2 MouseScreenPos=>Read("Mouse",Mouse);public EatWhat.Toolkit.Input.GameInputDevice CurrentDevice=>Read("Device",Device);
    }
    private static void InputObservationOnceAndUnknowns()
    {
        foreach(string kind in new[]{"single-read","gamepad","unknown-keyboard","read-failure","initial-key","null"})using(var context=new InputContextFixture())using(var f=new StreetFollowDriverTests.Fixture())
        {
            Configure(f);var r=Request(Path.Combine(Path.GetFullPath(Environment.GetEnvironmentVariable("FOLLOW_R_FIXTURE_ROOT")),Guid.NewGuid().ToString("N")));Directory.CreateDirectory(r.outputRoot);
            var input=new CountedInput();if(kind=="initial-key")input.Press=true;if(kind=="gamepad")input.Device=EatWhat.Toolkit.Input.GameInputDevice.Gamepad;
            var p=new LongLoopPlayerController {transform=f.Foot,input=kind=="null"?null:input,avatar=new TomatoSpineAvatar()};var b=new CinemachineBrain {State=f.State};var run=new LongLoopAutomatedProbe.FollowRRun(r,"input-read-once",p,f.Driver,f.Camera,b,DateTimeOffset.UtcNow.AddMinutes(20));run.Start();
            if(kind=="null"||kind=="initial-key")
            {Check(run.Terminal&&run.Report().error=="FOLLOW_R_USER_INPUT"&&run.Report().followR.inputAtStop.reason==(kind=="null"?"NULL_INPUT":"INTERACT"),"initial observation never consumes and discards a true edge/null");if(kind=="null")Check(run.Report().followR.inputAtStop.horizontal=="UNREAD"&&!run.Report().followR.inputAtStop.inputAvailable,"null is not invented zero fields");continue;}
            Check(input.Reads.Count==7&&input.Reads.Values.All(x=>x==1),"each original getter once at registration");input.Reads.Clear();
            if(kind=="gamepad"||kind=="unknown-keyboard"){Application.isFocused=false;Screen.width=1920;input.Look=new Vector2(.1f,0);}
            if(kind=="read-failure")input.ThrowLook=true;
            Time.WallClockOverride=.01;UnityEditor.EditorApplication.Watch();var result=run.Report();
            Check(input.Reads.Values.All(x=>x==1),"no getter reread/short circuit ambiguity in one observation: "+kind);
            if(kind=="single-read"){Check(!run.Terminal&&input.Reads.Count==7,"single neutral snapshot complete");run.Stop("OFFLINE_GETTER_COUNTS_OBSERVED");}
            if(kind=="gamepad"||kind=="unknown-keyboard")Check(run.Terminal&&result.error=="FOLLOW_R_USER_INPUT"&&result.followR.inputAtStop.reason=="LOOK_AXIS","focus/size context never ignores other provider or gamepad Look");
            if(kind=="read-failure")Check(run.Terminal&&result.error.StartsWith("FOLLOW_R_WATCH:")&&!result.followR.inputAtStop.complete&&result.followR.inputAtStop.lookX=="UNREAD"&&result.followR.inputAtStop.mouseX=="UNREAD"&&result.followR.inputAtStop.readError.Contains("ISOLATED_LOOK_READ_FAILURE"),"partial read preserved with actual error and unknown untouched fields");
            var counts=input.Reads.Values.Sum();run.Report();Check(input.Reads.Values.Sum()==counts,"report/cleanup does not resample one-frame input");Check(run.Report().cleanup.Passed,"single snapshot stops retain cleanup");
        }
    }
    public static int Main(string[] args)
    {
        if(args.Contains("--old-input-counterexample"))return OldInputCounterexamples();
        if(args.Contains("--old-snapshot-counterexample"))return OldSnapshotCounterexample();
        if(args.Contains("--old-startup-counterexample"))return OldStartupCounterexamples();
        if(args.Contains("--old-timestep-counterexample"))return OldTimestepCounterexamples();
        Case("CurrentPipelineAndInstalledComposer30_60_120",()=>{foreach(int fps in new[]{30,60,120})Trajectory(fps);});
        Case("ZeroBuildFiniteAndVariableNaturalDt",()=>{Trajectory(60,null,0);Check(Trajectory(60,"target-jump",0).error=="FOLLOW_R_HORIZONTAL_VELOCITY_JUMP","finite zero-time still rejects a later target jump");Trajectory(60,null,2,true);Trajectory(60,null,2,false,.8f);});
        Case("TargetAndPhysicalVelocityJumpCounterexamples",()=>{foreach(int fps in new[]{30,60,120}){Check(Trajectory(fps,"target-jump").error=="FOLLOW_R_HORIZONTAL_VELOCITY_JUMP","actual target jump rejects");Trajectory(fps,"physical-jump");}});
        Case("NoMoveNoLeadNoReturn",()=>{Trajectory(60,"not-moving");Trajectory(60,"not-leading");Trajectory(60,"no-return");});
        Case("MissingClockOwnershipTimeout",()=>{Trajectory(60,"missing-frame");Trajectory(60,"bad-clock");Trajectory(60,"foreign-input");using(var f=new StreetFollowDriverTests.Fixture()){Configure(f);var s=new FollowRSmokeSequence(Request(Path.GetFullPath(".")),0,Observe(f),CameraFollowConfigBinder.Snapshot(f.Config),.5f,6.5f,25.6f);s.CheckDeadline(61);Check(s.Finished&&s.Error.Contains("TIMEOUT"),"wall deadline without render samples");}});
        Case("ZeroRecenterAndZeroCameraTauRefusedWithoutChangingValues",()=>{using(var f=new StreetFollowDriverTests.Fixture()){Configure(f);var s=CameraFollowConfigBinder.Snapshot(f.Config);s.RecenterSpeed=0;Reject(()=>new FollowRSmokeSequence(Request(Path.GetFullPath(".")),0,Observe(f),s,.5f,6.5f,25.6f),"zero recenter infeasible");s.RecenterSpeed=.4f;Reject(()=>new FollowRSmokeSequence(Request(Path.GetFullPath(".")),0,Observe(f),s,0,6.5f,25.6f),"zero camera tau cannot satisfy smoothness premise");}});
        Case("ExactManualScopeIntersectionAndUnchangedSupervisedStops",Identity);
        Case("OfficialConsoleWindowsAndGaps",ConsoleWindows);
        Case("VerbatimBeginReadCancelAndForeignCleanup",NativeLifecycle);
        Case("VerbatimNaturalBrainCallbacksAndUserStop",CallbackLifecycle);
        Case("ActualChildFootBindingAndSampling",ChildFootBindingAndSampling);
        Case("NaturalStartupAndSpecificRejectedFrameEvidence",StartupAndRejectedFrameEvidence);
        Case("CurrentSupervisedAndManualRouteCompatibility",CurrentSupervisedRouting);
        Case("VariableNaturalTimestepsAndAllOriginalNegativeCases",VariableNaturalTimesteps);
        Case("InvalidPairedClockAndGlobalDeadlineRemainStrict",InvalidPairedClockStillFails);
        Case("ActualSnapshotMutexBusyRecoveryAndFiniteBudget",SnapshotBusyAndFreshRecovery);
        Case("ActualSnapshotNaturalFramesAndFreshTerminal",()=>{SnapshotNaturalFrames(false);SnapshotNaturalFrames(true);});
        Case("ActualSnapshotStopLeaseOwnerAndMalformedRejections",SnapshotRealRejections);
        Case("ActualLegacyInputFocusSizeAndRealIntervention",InputContextSemantics);
        Case("OriginalInputSingleSnapshotAndUnknownDiagnostics",InputObservationOnceAndUnknowns);
        Console.WriteLine((failed==0?"PASS":"FAIL")+" "+scenarios+" FOLLOW-R scenarios; "+checks+" checks; failures="+failed+"; actual pipeline/installed Composer, explicit Unity lifecycle boundary");return failed==0?0:1;
    }
}
