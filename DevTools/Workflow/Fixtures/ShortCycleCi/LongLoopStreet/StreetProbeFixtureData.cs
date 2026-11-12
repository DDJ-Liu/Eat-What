// Synthetic Unity/CLI boundary only. Sequence, predicates, report builder and depth/sort math are production sources.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web.Script.Serialization;
using EatWhat.EditorValidation;
using EatWhat.LongLoop;
using UnityEngine;
using Newtonsoft.Json.Linq;

public static class StreetProbeFixtureData
{
    sealed class Fields : JavaScriptConverter
    {
        public override IEnumerable<Type> SupportedTypes => new[]{typeof(StreetProbeRequest),typeof(StreetProbeStage),typeof(StreetProbeFile),typeof(StreetProbeReport),typeof(StreetProbeCheck),typeof(StreetProbeCleanup),typeof(StreetProbeObservation),typeof(Vector2),typeof(Vector3)};
        public override IDictionary<string,object> Serialize(object value,JavaScriptSerializer serializer)=>value.GetType().GetFields(BindingFlags.Public|BindingFlags.Instance).ToDictionary(f=>f.Name,f=>f.GetValue(value));
        public override object Deserialize(IDictionary<string,object> dictionary,Type type,JavaScriptSerializer serializer)=>throw new NotSupportedException("Output converter only");
    }
    public static string Json(object value)
    {var s=new JavaScriptSerializer {MaxJsonLength=10000000,RecursionLimit=100};s.RegisterConverters(new[]{new Fields()});return s.Serialize(value);}
    public static StreetProbeObservation Boundary(StreetProbeRequest r,StreetProbeSequence sequence,int frame,int local,double now)
    {
        string id=r.stages[sequence.StageIndex].id;float t=local*.125f;var input=r.stages[sequence.StageIndex].input;
        var foot=new Vector3(input.x*6.5f*t,input.y*6.5f*.6f*t,0);
        if(id=="edge-slide")foot=new Vector3(r.edgeTangent.x*t,r.edgeTangent.y*t,0);
        if(id.StartsWith("depth-"))foot=r.stages[sequence.StageIndex].destination;
        float focus=id=="focus"?2+r.focusDelta:2;
        DepthScaleMath.TryFactor(4,foot.y,.03f,out float factor);DepthScaleMath.TryScale(new Vector3(1,1,1),factor,out var scale);
        YSortMath.TryOrder(0,foot.y,100,0,out int order,out bool saturated);
        var follow=foot+new Vector3(0,focus,0);var goal=follow;
        if(id=="deadzone-hold")goal=new Vector3(0,focus,0);
        if(id=="deadzone-push")goal=new Vector3(t,focus,0);
        float stopped=id=="stopped-recenter"?t:0;
        if(id=="stopped-recenter")goal=new Vector3(Math.Max(0,1.1f-Math.Max(0,t-.6f)*4),focus,0);
        float ahead=id=="ahead-right"?1:id=="ahead-left"?-1:0;bool moving=(input.x!=0||input.y!=0)&&id!="edge-stop";
        return new StreetProbeObservation {frame=frame,elapsed=now,gameTime=now,deltaTime=.125f,fixedDeltaTime=.02f,
            speed=6.5f,verticalFactor=.6f,inputX=input.x,inputY=input.y,groundY=4,coefficient=.03f,factor=factor,
            focus=focus,originalFocus=2,foot=foot,derivedFoot=foot,visualScale=scale,baselineScale=new Vector3(1,1,1),
            followPoint=follow,camera=goal,goal=goal,brainOutput=goal,cameraOutput=goal,precision=100,order=order,
            frontOrder=order+10,backOrder=order-10,ahead=ahead,stopped=stopped,delay=.6f,recenterSpeed=4,
            recenterSpeedObserved=id=="stopped-recenter"&&t>.6f&&t<.875f?4:0,lookahead=1,lookaheadTime=.1f,cap=3,
            size=5,f01Size=5,aspect=1.7777778f,sequence=(ulong)frame,bodySequence=(ulong)frame,channelAdvances=frame,bodyNotifications=frame,
            ready=true,inputOwned=true,configsOwned=true,inside=true,boundaryBlocked=id.StartsWith("edge-"),facingRight=input.x>=0,moving=moving,animationWalk=moving,animationIdle=!moving,
            sorterTargetMatches=true,markersReady=true,channelValid=true,channelZero=true,baselineOutside=id=="f02-zero",
            observerSameFrame=true,dampingMeasured=true,confinerClearForCameraTests=true,movementPath="FIXTURE_SYNTHETIC_BOUNDARY"};
    }
    public static StreetProbeSequence Sample(StreetProbeRequest request)
    {
        var sequence=new StreetProbeSequence(request,0);int frame=0,local=0,previous=0;
        for(double now=0;!sequence.Finished&&frame<1000;now+=.125){if(previous!=sequence.StageIndex){local=0;previous=sequence.StageIndex;}
            sequence.Tick(Boundary(request,sequence,++frame,local++,now),now,s=>{});if(sequence.PendingCapture!=null)sequence.AcknowledgeCapture("sorting",now);}
        if(sequence.Error!=null)throw new Exception("Actual sequence failed: "+sequence.Error);return sequence;
    }
    public static StreetProbeReport Report(StreetProbeRequest request,StreetProbeSequence sequence)=>LongLoopProbeChecks.BuildReport(request,sequence.Samples,new StreetProbeCleanup {inputRestored=true,configsRestored=true,configOwnerRestored=true,callbacksRemoved=true,temporaryObjectsDestroyed=true,noForeignOverwrite=true});
    public static int Main(string[] args)
    {
        if(args.Length==2&&args[0]=="--compiled-context")return CompiledContextCases(args[1]);
        if(args.Length==2&&args[0]=="--validate-plan") {var plan=new JavaScriptSerializer().Deserialize<StreetProbeRequest>(File.ReadAllText(args[1]));LongLoopProbeChecks.ValidatePlan(plan);Console.WriteLine("PASS actual production plan validation sum="+plan.stages.Sum(s=>s.timeoutSeconds));return 0;}
        if(args.Length==2&&args[0]=="--source-cases"){int count=StreetConfigurationTests.Run();File.WriteAllText(args[1],StreetConfigurationTests.CasesJson());Console.WriteLine("PASS "+count+" actual native policy/source assertions, fixture only");return 0;}
        if(args.Length!=2)throw new ArgumentException("request JSON and new output directory required");
        var r=new JavaScriptSerializer().Deserialize<StreetProbeRequest>(File.ReadAllText(args[0]));var seq=Sample(r);var report=Report(r,seq);
        if(report.checks.Any(c=>!c.passed))throw new Exception("Actual predicates failed: "+string.Join(",",report.checks.Where(c=>!c.passed).Select(c=>c.id)));
        report.status="SUCCEEDED";report.passed=true;report.unityVersion="6000.3.24f1";report.capabilityVersion=LongLoopProbeChecks.CapabilityVersion;report.startedAt=DateTimeOffset.UtcNow.AddSeconds(-seq.Samples.Last().elapsed).ToString("o");report.finishedAt=DateTimeOffset.UtcNow.ToString("o");report.assemblyPath=Path.Combine(r.projectPath,"Library/ScriptAssemblies/Editor.ProjectValidation.dll");
        var auth=StreetConfigurationAuthorization.Parse(File.ReadAllText(r.authorizationPath));string mode=StreetConfigurationAuthorization.Text(auth,"configurationAuthorizationMode");
        string rp=StreetConfigurationAuthorization.Text(auth,mode=="USER_ADJUSTED"?"userAdjustmentReceiptPath":"presetReviewPath");var review=StreetConfigurationAuthorization.Parse(File.ReadAllText(rp));
        var decision=StreetConfigurationAuthorization.Validate(mode,File.ReadAllText(rp),File.ReadAllText(StreetConfigurationAuthorization.OriginalPath(mode,review)),new StreetConfigurationIdentity {taskId=r.taskId,attempt=r.attempt,executionId=r.executionId,ownerThreadId=r.ownerThreadId,runId=r.runId,projectPath=r.projectPath,scopeSha256=r.scopeSha256,geometrySha256=r.geometrySha256,controllerThreadId=(string)auth["controllerThreadId"]});
        report.configurationAuthorizationMode=decision.mode;report.configurationReviewPath=rp;report.configurationReviewSha256=StreetConfigurationAuthorization.Text(auth,mode=="USER_ADJUSTED"?"userAdjustmentReceiptSha256":"presetReviewSha256");
        report.humanTuningStatus=decision.humanTuningStatus;report.humanTuningCompleted=decision.humanTuningCompleted;report.finalVisualAccepted=false;
        report.cameraBoundsPath=r.cameraBoundsPath;report.confinerTypeName="Unity.Cinemachine.CinemachineConfiner2D";report.confinerInstanceId=-51;report.confinerBoundsInstanceId=-52;
        report.confinerGeometrySha256=(string)StreetConfigurationAuthorization.Parse(File.ReadAllText(r.geometryPath))["confinerGeometrySha256"];
        Directory.CreateDirectory(args[1]);File.WriteAllText(Path.Combine(args[1],"fixture-report.json"),Json(report));File.WriteAllLines(Path.Combine(args[1],"fixture-ledger.jsonl"),seq.Samples.Select(s=>Json(s)));
        Console.WriteLine("FIXTURE_ONLY: actual sequence and32 production predicates passed; synthetic Unity/CLI observations, no native acceptance");return 0;
    }
    private static int CompiledContextCases(string path)
    {
        var labels=new List<string>();void Check(bool v,string n){if(!v)throw new Exception(n);labels.Add(n);}
        void Reject(Action a,string n){bool bad=false;try{a();}catch(Exception){bad=true;}Check(bad,n);}
        var original=File.ReadAllBytes(path);var c=Newtonsoft.Json.JsonConvert.DeserializeObject<StreetPayloadContext>(File.ReadAllText(path));
        string sha=StreetPayloadContract.HashFile(path);var project=c.projectPath;
        var q=StreetPayloadContract.JsonFile(Path.Combine(project,".ai-workspace/runtime/queue-state.json"));
        var w=StreetPayloadContract.JsonFile(Path.Combine(project,".ai-workspace/runtime/worker-state.json"));
        var m=StreetPayloadContract.JsonFile(Path.Combine(project,".ai-workspace/runtime/mode-state.json"));
        var inputPaths=new[]{path,Path.Combine(project,".ai-workspace/runtime/queue-state.json"),Path.Combine(project,".ai-workspace/runtime/worker-state.json"),Path.Combine(project,".ai-workspace/runtime/mode-state.json")};
        var before=inputPaths.ToDictionary(p=>p,StreetPayloadContract.HashFile);
        foreach(string role in StreetPayloadContract.Roles)Check(StreetPayloadContract.ReadContext(role,path,sha,project).executionId==c.executionId,"Actual compiled context accepts "+role);
        var task=((JArray)q["tasks"]).OfType<JObject>().Single(t=>(string)t["id"]==c.taskId);
        void LiveCase(string name,Action<JObject,JObject,JObject,JObject> change){var aq=(JObject)q.DeepClone();var aw=(JObject)w.DeepClone();var am=(JObject)m.DeepClone();var at=((JArray)aq["tasks"]).OfType<JObject>().Single(t=>(string)t["id"]==c.taskId);change(aq,aw,am,at);Reject(()=>StreetPayloadContract.ValidateLive(c,aq,aw,am,DateTimeOffset.UtcNow),name);}
        LiveCase("missing task",(a,b,d,t)=>((JArray)a["tasks"]).Remove(t));
        LiveCase("duplicate task",(a,b,d,t)=>((JArray)a["tasks"]).Add(t.DeepClone()));
        LiveCase("non-running",(a,b,d,t)=>t["status"]="NEEDS_USER");
        LiveCase("wrong lane",(a,b,d,t)=>t["pipeline"]="CODE");
        LiveCase("wrong attempt",(a,b,d,t)=>t["attempts"]=c.attempt+1);
        LiveCase("wrong execution",(a,b,d,t)=>t["execution"]["executionId"]="foreign");
        LiveCase("wrong owner",(a,b,d,t)=>t["execution"]["ownerThreadId"]="foreign");
        LiveCase("wrong active task",(a,b,d,t)=>b["workers"]["ENGINE_CLI"]["activeTaskId"]="AI-999999");
        LiveCase("wrong active execution",(a,b,d,t)=>b["workers"]["ENGINE_CLI"]["activeExecutionId"]="foreign");
        LiveCase("wrong route owner",(a,b,d,t)=>b["workers"]["ENGINE_CLI"]["threadId"]="foreign");
        LiveCase("expired lease",(a,b,d,t)=>b["workers"]["ENGINE_CLI"]["leaseUntil"]="2000-01-01T00:00:00Z");
        LiveCase("lease without timezone",(a,b,d,t)=>b["workers"]["ENGINE_CLI"]["leaseUntil"]="2099-01-01T00:00:00");
        LiveCase("wrong mode",(a,b,d,t)=>d["mode"]="MANUAL");
        LiveCase("paused",(a,b,d,t)=>d["supervised"]["status"]="PAUSED");
        LiveCase("wrong session",(a,b,d,t)=>d["supervised"]["sessionId"]="foreign");
        LiveCase("stop",(a,b,d,t)=>d["supervised"]["stopRequested"]=true);
        LiveCase("exit",(a,b,d,t)=>d["supervised"]["exitRequested"]=true);
        LiveCase("out of scope",(a,b,d,t)=>d["supervised"]["taskIds"]=new JArray());
        LiveCase("turn stop",(a,b,d,t)=>t["execution"]["limitReached"]=true);
        LiveCase("hard stop",(a,b,d,t)=>t["execution"]["hardStopAt"]="now");
        LiveCase("root hard stop",(a,b,d,t)=>t["retryHardStop"]=new JObject{["kind"]="PERMISSION"});
        Reject(()=>StreetPayloadContract.ReadContext("foreign",path,sha,project),"unknown role");
        Reject(()=>StreetPayloadContract.ReadContext("preflight",path,new string('0',64),project),"context SHA drift");
        Reject(()=>StreetPayloadContract.ReadContext("preflight",path,sha,Path.Combine(project,"foreign")),"foreign actual project");
        Reject(()=>StreetPayloadContract.NoLinks("relative/../context.json"),"relative traversal");
        void HeaderCase(string name,Action<JObject> change){try{var header=StreetPayloadContract.JsonFile(path);change(header);File.WriteAllText(path,header.ToString());Reject(()=>StreetPayloadContract.ReadContext("preflight",path,StreetPayloadContract.HashFile(path),project),name);}finally{File.WriteAllBytes(path,original);}}
        HeaderCase("old interface",h=>h["version"]="old");
        HeaderCase("wrong compiled source stamp",h=>h["implementationSha256"]="wrong");
        HeaderCase("foreign root",h=>h["outputRoot"]=Path.Combine(project,"foreign"));
        HeaderCase("foreign scene",h=>h["scenePath"]="Assets/Other.unity");
        HeaderCase("foreign source",h=>h["sources"][0]["path"]=Path.Combine(Path.GetPathRoot(project),"foreign.cs"));
        HeaderCase("source SHA drift",h=>h["sources"][0]["sha256"]=new string('0',64));
        HeaderCase("duplicate source",h=>((JArray)h["sources"]).Add(h["sources"][0].DeepClone()));
        foreach(var p in inputPaths)Check(StreetPayloadContract.HashFile(p)==before[p],"Actual guard only reads "+Path.GetFileName(p));
        File.WriteAllText(path+".guards.json",Newtonsoft.Json.JsonConvert.SerializeObject(new {passed=true,checks=labels,actualProductionGuard=true,unityExecuted=false},Newtonsoft.Json.Formatting.Indented));
        Console.WriteLine("PASS "+labels.Count+" actual compiled Street context guards");return 0;
    }
}
