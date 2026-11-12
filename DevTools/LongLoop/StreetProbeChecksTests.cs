#if STREET_FRAME_READ_TESTS
using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using UnityEngine;
using EatWhat.EditorValidation;
// Only Unity state/controller/JSON/evidence boundaries are substituted. Production SmokeRun and validation are extracted verbatim.
namespace EatWhat.EditorValidation
{
    public static partial class LongLoopAutomatedProbe
    {
        public interface IGameInput{float Horizontal{get;}float Vertical{get;}}
        public interface IPlayerAvatar{}
        public sealed class IdleInput:IGameInput{public float Horizontal=>0;public float Vertical=>0;}
        private sealed class Proxy:IGameInput{public Vector2 Move;public float Horizontal=>Move.x;public float Vertical=>Move.y;}
        public sealed class Transform{public Vector3 position;}
        public sealed class SkeletonAnimation{public object AnimationState=new object();}
        public sealed class TomatoSpineAvatar:IPlayerAvatar{public bool isActiveAndEnabled=true;public SkeletonAnimation skeletonAnimation=new SkeletonAnimation();}
        public sealed class WalkArea{public bool IsUsable=>true;public bool Contains(Vector2 value)=>true;}
        public sealed class LongLoopPlayerController
        {
            public IGameInput input=new IdleInput();public IPlayerAvatar avatar=new TomatoSpineAvatar();public Transform transform=new Transform();public bool isActiveAndEnabled=true;public int stops,configures;
            public void StopMovement(){stops++;}public void ConfigureInterfaces(IGameInput i,IPlayerAvatar a){input=i;avatar=a;configures++;}
        }
        public static class Time{public static double realtimeSinceStartupAsDouble,timeAsDouble;public static int frameCount;}
        public static class Application{public static string dataPath;public static string unityVersion="OFFLINE_UNITY_BOUNDARY";}
        public static class SceneManager{public sealed class Scene{public string path="Assets/Scenes/LongLoop/LongLoop_Street.unity";}public static Scene GetActiveScene()=>new Scene();}
        public static class EditorApplication{public static bool isPlaying=true,isCompiling,isUpdating;public static event Action update;public static void Fire()=>update?.Invoke();public static int Count=>update?.GetInvocationList().Length??0;}
        public static LongLoopPlayerController Player;public static Transform Spawn;
        public static Vector2 Input=>new Vector2(Player.input.Horizontal,Player.input.Vertical);
        private static T Resolve<T>(string path)where T:class =>(typeof(T)==typeof(LongLoopPlayerController)?(object)Player:Spawn) as T;
        private static object RuntimeField(LongLoopPlayerController player,string name,Type type)=>name=="input"?(object)player.input:player.avatar;
        private static T Ref<T>(object owner,string name)where T:class =>(typeof(T)==typeof(WalkArea)?(object)new WalkArea():((TomatoSpineAvatar)owner).skeletonAnimation) as T;
        private static void NoLinks(string path)=>StreetPayloadContract.NoLinks(path);
        private static string Hash(byte[] bytes){using(var h=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(h.ComputeHash(bytes)).Replace("-","");}
        private static void PublishProbeEvidence(string path,string text){using(var f=new FileStream(path,FileMode.CreateNew))using(var w=new StreamWriter(f))w.Write(text);}
        private static class JsonUtility
        {
            private sealed class Fields:DefaultContractResolver{protected override List<MemberInfo> GetSerializableMembers(Type t)=>t.GetFields(BindingFlags.Instance|BindingFlags.Public).Cast<MemberInfo>().ToList();}
            public static string ToJson(object value,bool pretty=false)=>JsonConvert.SerializeObject(value,pretty?Formatting.Indented:Formatting.None,new JsonSerializerSettings{ContractResolver=new Fields()});
        }
    }
}
internal static class StreetProbeChecksTests
{
    private static int assertions,serial;private static string root,flags;private static StreetProbeRequest request;
    private static void Need(bool value,string reason){assertions++;if(!value)throw new InvalidOperationException(reason);}
    private static void Wait(string name){var p=Path.Combine(flags,name);var clock=Stopwatch.StartNew();while(!File.Exists(p)){if(clock.ElapsedMilliseconds>10000)throw new TimeoutException("Fixture signal "+name);Thread.Sleep(10);}}
    private static void Signal(string name)=>File.WriteAllText(Path.Combine(flags,name),"fixture-signal");
    private static void Write(string name,string text)=>File.WriteAllText(Path.Combine(root,".ai-workspace/runtime",name),text);
    private static LongLoopAutomatedProbe.SmokeRun Start()
    {
        LongLoopAutomatedProbe.Player=new LongLoopAutomatedProbe.LongLoopPlayerController();LongLoopAutomatedProbe.Spawn=new LongLoopAutomatedProbe.Transform();LongLoopAutomatedProbe.Time.realtimeSinceStartupAsDouble=0;LongLoopAutomatedProbe.Time.timeAsDouble=0;LongLoopAutomatedProbe.Time.frameCount=0;LongLoopAutomatedProbe.EditorApplication.isPlaying=true;
        LongLoopAutomatedProbe.Application.dataPath=Path.Combine(root,"Assets");Directory.CreateDirectory(LongLoopAutomatedProbe.Application.dataPath);Directory.CreateDirectory(Path.Combine(root,".ai-workspace/runtime"));
        request=new StreetProbeRequest {suite=StreetSmokeSequence.Suite,maximumSeconds=8,taskId="AI-000999",attempt=1,executionId="fixture-execution",ownerThreadId="fixture-owner",runId="frame"+Process.GetCurrentProcess().Id+"_"+(++serial),projectPath=root,scenePath="Assets/Scenes/LongLoop/LongLoop_Street.unity",editorPid=Process.GetCurrentProcess().Id,playerPath="PlayerRoot_TUNE",spawnPath="PlayerSpawn_TUNE"};
        request.outputRoot=Path.Combine(root,".ai-workspace/outputs/PLAYABLE-DEMO011-20261104",request.taskId,"attempt1/smk-011",request.runId);Directory.CreateDirectory(request.outputRoot);
        Write("queue-state.json","{\"tasks\":[{\"id\":\"AI-000999\",\"pipeline\":\"ENGINE_CLI\",\"status\":\"RUNNING\",\"attempts\":1,\"execution\":{\"executionId\":\"fixture-execution\",\"ownerThreadId\":\"fixture-owner\"}}]}");
        var worker=JObject.Parse("{\"workers\":{\"ENGINE_CLI\":{\"status\":\"BUSY\",\"activeTaskId\":\"AI-000999\",\"activeExecutionId\":\"fixture-execution\",\"threadId\":\"fixture-owner\"}}}");worker["workers"]["ENGINE_CLI"]["leaseUntil"]=DateTimeOffset.UtcNow.AddMinutes(1).ToString("o");Write("worker-state.json",worker.ToString());
        Write("mode-state.json","{\"mode\":\"SUPERVISED\",\"supervised\":{\"status\":\"ACTIVE\",\"sessionId\":\"fixture-session\",\"taskIds\":[\"AI-000999\"],\"stopRequested\":false,\"exitRequested\":false}}");
        var run=new LongLoopAutomatedProbe.SmokeRun(request,"fixture-request");run.Start();Need(LongLoopAutomatedProbe.Input==Vector2.zero&&LongLoopAutomatedProbe.Player.stops==1,"Installed input is neutral until first fresh check");return run;
    }
    private static void Tick(double now,bool move=false)
    {
        if(move){var v=LongLoopAutomatedProbe.Input;LongLoopAutomatedProbe.Player.transform.position+=new Vector3(v.x*.1f,v.y*.1f,0);}
        LongLoopAutomatedProbe.Time.realtimeSinceStartupAsDouble=now;LongLoopAutomatedProbe.Time.timeAsDouble=now;LongLoopAutomatedProbe.Time.frameCount++;LongLoopAutomatedProbe.EditorApplication.Fire();
    }
    public static int Main(string[] args)
    {
        string scenario=args[0];root=args[1];flags=Path.GetDirectoryName(args[2]);var run=Start();File.WriteAllText(args[2],"prepared");Wait(scenario+"1.go");
        if(scenario=="A")
        {
            var clock=Stopwatch.StartNew();Tick(0);long busyMs=clock.ElapsedMilliseconds;Need(busyMs<400&&LongLoopAutomatedProbe.Input==Vector2.zero&&run.Report().samplesSaved==0&&!run.Terminal,"BUSY is immediate pending, neutral and unsampled");
            clock.Restart();bool oldTimeout=false;try{StreetPayloadContract.JsonFile(Path.Combine(root,".ai-workspace/runtime/queue-state.json"));}catch(TimeoutException){oldTimeout=true;}long oldMs=clock.ElapsedMilliseconds;Need(oldTimeout&&oldMs>=450,"Unchanged synchronous500ms negative is expected");
            Signal("A1.release");Wait("A1.ack");Tick(1.2);Need(LongLoopAutomatedProbe.Input.x==1&&run.Report().samplesSaved==0,"Fresh check starts actual first input");Tick(1.3,true);Need(!run.Report().directions[0].observed,"Initial neutral wait is excluded from .3 effective input");Tick(1.51,true);Need(run.Report().directions[0].passed,"First actual direction passes after .31 active seconds");Tick(1.61,true);
            Signal("A2.want");Wait("A2.go");Tick(1.71,true);int samples=run.Report().samplesSaved;Tick(2.71);Need(LongLoopAutomatedProbe.Input==Vector2.zero&&run.Report().samplesSaved==samples&&!run.Report().directions[1].observed,"Mid-direction busy frames do not sample or advance");Signal("A2.release");Wait("A2.ack");Tick(2.71);Tick(2.76,true);Need(!run.Report().directions[1].observed,"Paused second excludes one second neutral interval");Tick(2.82,true);Tick(3.13,true);Tick(3.44,true);
            var report=run.Report();Need(run.Terminal&&report.directions.All(d=>d.observed&&d.passed)&&report.checks[2].passed&&!report.passed&&!report.checks[3].observed&&!report.checks[4].observed&&report.cleanup.Passed,"Four actual sequence directions and cleanup pass; native gates remain NOT_RUN");Need(LongLoopAutomatedProbe.EditorApplication.Count==0&&LongLoopAutomatedProbe.Player.configures==2,"Actual update callback detached and interfaces restored once");
            Console.WriteLine(JsonConvert.SerializeObject(new {scenario="TransientBusyThenFresh",passed=true,assertions,busyMs,oldSynchronousExpectedTimeoutMs=oldMs,samples=report.samplesSaved,status=report.status,unityBoundary="TRANSPARENT_STATE_CONTROLLER_JSON"}));return 0;
        }
        if(scenario=="B")
        {
            Tick(0);Tick(.7);Tick(7.99);Need(!run.Terminal&&LongLoopAutomatedProbe.Input==Vector2.zero,"Busy remains neutral inside original clock");Tick(8);var report=run.Report();Tick(9);
            Need(run.Terminal&&report.status=="FAILED"&&report.error=="SMK_TIMEOUT_OR_CLOCK"&&report.samplesSaved==0&&report.directions.All(d=>!d.observed)&&report.cleanup.Passed,"Original8 deadline fails with exact cleanup, no false samples or directions");Need(LongLoopAutomatedProbe.EditorApplication.Count==0&&LongLoopAutomatedProbe.Player.configures==2&&!report.passed,"Terminal cleanup is not repeated");
            Console.WriteLine(JsonConvert.SerializeObject(new {scenario="BusyUntilOriginalDeadline",passed=true,assertions,status=report.status,error=report.error,samples=report.samplesSaved,controlledClock=8}));return 0;
        }
        Tick(0);Need(LongLoopAutomatedProbe.Input==Vector2.zero&&!run.Terminal,"Invalid-state case first observes BUSY only");Signal("C1.release");Wait("C1.ack");
        var labels=new List<string>();foreach(var invalid in new[]{"stop","owner","lease","duplicate","io","nolinks","foreign"})
        {
            if(invalid!="stop")run=Start();FileStream held=null;
            if(invalid=="stop"){var m=StreetPayloadContract.JsonFile(Path.Combine(root,".ai-workspace/runtime/mode-state.json"));m["supervised"]["stopRequested"]=true;Write("mode-state.json",m.ToString());}
            if(invalid=="owner"){var q=StreetPayloadContract.JsonFile(Path.Combine(root,".ai-workspace/runtime/queue-state.json"));q["tasks"][0]["execution"]["ownerThreadId"]="foreign";Write("queue-state.json",q.ToString());}
            if(invalid=="lease"){var w=StreetPayloadContract.JsonFile(Path.Combine(root,".ai-workspace/runtime/worker-state.json"));w["workers"]["ENGINE_CLI"]["leaseUntil"]=DateTimeOffset.UtcNow.AddMinutes(-1).ToString("o");Write("worker-state.json",w.ToString());}
            if(invalid=="duplicate")Write("queue-state.json","{\"tasks\":[],\"tasks\":[]}");
            if(invalid=="io")held=new FileStream(Path.Combine(root,".ai-workspace/runtime/queue-state.json"),FileMode.Open,FileAccess.Read,FileShare.None);
            if(invalid=="nolinks")request.outputRoot=Path.Combine(request.outputRoot,"..",Path.GetFileName(request.outputRoot));
            if(invalid=="foreign")LongLoopAutomatedProbe.Player.input=new LongLoopAutomatedProbe.IdleInput();
            try{Tick(.1);}finally{held?.Dispose();}var report=run.Report();Need(run.Terminal&&report.status=="FAILED"&&report.samplesSaved==0&&report.directions.All(d=>!d.observed)&&!report.passed,"Fresh "+invalid+" fails instead of pending or moving");Need(invalid=="foreign"?!report.cleanup.noForeignOverwrite:report.cleanup.Passed,"Exact owned cleanup and foreign retention");
            string expected=invalid=="duplicate"?"JsonReaderException":invalid=="nolinks"?"Literal absolute Street path required":invalid=="foreign"?"SMK_INPUT_OWNERSHIP_LOST":invalid=="io"?"IOException":"Exact live Street claim/owner/session/lease/stop required";
            Need((report.error??"").Contains(expected),"Original concrete failure retained: "+invalid);labels.Add(invalid);
        }
        Console.WriteLine(JsonConvert.SerializeObject(new {scenario="FreshInvalidStateStillFails",passed=true,assertions,rejected=labels,historicalMutexOwner="UNKNOWN",nativeUnity=false}));return 0;
    }
}
#elif STREET_STATE_READ_TESTS
using System;
using System.IO;
using System.Diagnostics;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using EatWhat.EditorValidation;
internal static class StreetProbeChecksTests
{
    private static void Need(bool value,string reason){if(!value)throw new InvalidOperationException(reason);}
    private static void Reject<T>(Action action)where T:Exception {try{action();}catch(T){return;}throw new InvalidOperationException("Expected "+typeof(T).Name);}
    private static void Wait(string path){var clock=Stopwatch.StartNew();while(!File.Exists(path)){if(clock.ElapsedMilliseconds>3000)throw new TimeoutException("Fixture release missing");Thread.Sleep(10);}}
    public static int Main(string[] args)
    {
        string role=args[0],path=args[1],marker=args[2];
        if(role=="guards")
        {
            File.WriteAllText(path,"{\"at\":\"2026-11-06T00:00:00Z\",\"generation\":1}");var first=StreetPayloadContract.JsonFile(path);
            Need(first["at"].Type==JTokenType.String&&(int)first["generation"]==1,"Explicit date string and JSON number types retained");
            File.WriteAllText(path,"{\"generation\":2}");Need((int)StreetPayloadContract.JsonFile(path)["generation"]==2,"No cross-call live cache");
            File.WriteAllText(path,"{\"same\":1,\"same\":2}");Reject<JsonReaderException>(()=>StreetPayloadContract.JsonFile(path));
            Reject<InvalidOperationException>(()=>StreetPayloadContract.JsonFile(Path.Combine(Path.GetDirectoryName(path),"..",Path.GetFileName(path))));
            var c=new StreetPayloadContext {version=StreetPayloadContract.Version,implementationSha256=StreetPayloadContract.ImplementationSha256,taskId="AI-000999",attempt=1,executionId="fixture-execution",ownerThreadId="fixture-owner",runId="state-fixture",sessionId="fixture-session"};
            var q=JObject.Parse("{\"tasks\":[{\"id\":\"AI-000999\",\"pipeline\":\"ENGINE_CLI\",\"status\":\"RUNNING\",\"attempts\":1,\"execution\":{\"executionId\":\"fixture-execution\",\"ownerThreadId\":\"fixture-owner\"}}]}");
            var w=JObject.Parse("{\"workers\":{\"ENGINE_CLI\":{\"status\":\"BUSY\",\"activeTaskId\":\"AI-000999\",\"activeExecutionId\":\"fixture-execution\",\"threadId\":\"fixture-owner\"}}}");w["workers"]["ENGINE_CLI"]["leaseUntil"]=DateTimeOffset.UtcNow.AddMinutes(1).ToString("o");
            var m=JObject.Parse("{\"mode\":\"SUPERVISED\",\"supervised\":{\"status\":\"ACTIVE\",\"sessionId\":\"fixture-session\",\"taskIds\":[\"AI-000999\"],\"stopRequested\":false,\"exitRequested\":false}}");
            StreetPayloadContract.ValidateLive(c,q,w,m,DateTimeOffset.UtcNow);
            var owner=(JObject)q.DeepClone();owner["tasks"][0]["execution"]["ownerThreadId"]="foreign";Reject<InvalidOperationException>(()=>StreetPayloadContract.ValidateLive(c,owner,w,m,DateTimeOffset.UtcNow));
            var stopped=(JObject)m.DeepClone();stopped["supervised"]["stopRequested"]=true;Reject<InvalidOperationException>(()=>StreetPayloadContract.ValidateLive(c,q,w,stopped,DateTimeOffset.UtcNow));
            var expired=(JObject)w.DeepClone();expired["workers"]["ENGINE_CLI"]["leaseUntil"]=DateTimeOffset.UtcNow.AddMinutes(-1).ToString("o");Reject<InvalidOperationException>(()=>StreetPayloadContract.ValidateLive(c,q,expired,m,DateTimeOffset.UtcNow));
            Console.WriteLine(JsonConvert.SerializeObject(new {role,passed=true,dateType=first["at"].Type.ToString(),liveOwnerStopLeaseRejected=true}));return 0;
        }
        if(role=="errors")
        {
            Reject<FileNotFoundException>(()=>StreetPayloadContract.JsonFile(path));
            File.WriteAllText(path,"{\"same\":1,\"same\":2}");Reject<JsonReaderException>(()=>StreetPayloadContract.JsonFile(path));
            File.WriteAllText(marker,"errors-returned-owner-still-alive");Wait(args[3]);
            Console.WriteLine(JsonConvert.SerializeObject(new {role,passed=true,missingAndParseErrorsPropagated=true}));return 0;
        }
        // Retain a handle solely so the real named kernel mutex survives an abandoning publisher exit.
        using(var lifetime=role=="abandoned"?new Mutex(false,@"Local\EatWhat_AI_Workspace_Queue_v1"):null)
        {
            File.WriteAllText(marker,"before-actual-JsonFile");var clock=Stopwatch.StartNew();
            if(role=="timeout")
            {
                Reject<TimeoutException>(()=>StreetPayloadContract.JsonFile(path));
                Need(clock.ElapsedMilliseconds>=450&&clock.ElapsedMilliseconds<2000,"Single 500ms wait is bounded; no retry");
                Console.WriteLine(JsonConvert.SerializeObject(new {role,passed=true,elapsedMs=clock.ElapsedMilliseconds,expectedException="TimeoutException"}));return 0;
            }
            var value=StreetPayloadContract.JsonFile(path);Need((int)value["generation"]==2,"Read the publisher's complete current snapshot");
            Console.WriteLine(JsonConvert.SerializeObject(new {role,passed=true,elapsedMs=clock.ElapsedMilliseconds,value}));return 0;
        }
    }
}
#elif STREET_REPORT_JSON_TESTS
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using EatWhat.EditorValidation;
using UnityEngine;
// Transparent offline stand-in only: native JsonUtility is not executed by this test.
namespace UnityEngine
{
    public static class JsonUtility
    {
        private sealed class PublicFields : DefaultContractResolver
        {
            protected override List<MemberInfo> GetSerializableMembers(Type type)=>type.GetFields(BindingFlags.Instance|BindingFlags.Public).Cast<MemberInfo>().ToList();
        }
        public static int Calls;public static object Last;public static bool Pretty;
        public static string ToJson(object value,bool pretty)
        {
            Calls++;Last=value;Pretty=pretty;
            return JsonConvert.SerializeObject(value,pretty?Formatting.Indented:Formatting.None,new JsonSerializerSettings {ContractResolver=new PublicFields()});
        }
    }
}
internal static class StreetProbeChecksTests
{
    private static void Need(bool value,string reason){if(!value)throw new InvalidOperationException(reason);}
    private static JObject Write(StreetProbeReport report)
    {
        int before=JsonUtility.Calls;var value=JObject.Parse(StreetMenuSerializationExpression.Write(report));
        Need(JsonUtility.Calls==before+1&&ReferenceEquals(JsonUtility.Last,report)&&JsonUtility.Pretty,"Actual menu expression passes the complete same DTO to JsonUtility with pretty=true");
        Need(value.Properties().Select(p=>p.Name).OrderBy(n=>n).SequenceEqual(typeof(StreetProbeReport).GetFields().Select(f=>f.Name).OrderBy(n=>n)),"All actual report fields retained; no property tree substituted");return value;
    }
    public static int Main()
    {
        var request=new StreetProbeRequest {suite=StreetSmokeSequence.Suite,maximumSeconds=8,taskId="OFFLINE-AI449",attempt=1,executionId="offline-only",ownerThreadId="offline-owner",runId="offline449",projectPath="offline-project",scenePath="offline-scene",outputRoot="offline-output",editorPid=123,editorStartedAt="offline-time"};
        var sample=new StreetProbeObservation {frame=7,ready=true,inside=true,inputOwned=true,movementPath="actual-dto-fixture"};
        foreach(var field in typeof(StreetProbeObservation).GetFields().Where(f=>f.FieldType==typeof(Vector3)))field.SetValue(sample,new Vector3(-2.5f,4.5f,-7));
        var sequence=new StreetSmokeSequence(request,0,sample,sample.foot,true);var running=sequence.Report(new StreetProbeCleanup());
        running.directions[0].observedSample=sample;running.directions[0].actual=new double[]{-2.5,4.5};running.requestSha256="request-fixture";
        var report=Write(running);Need((string)report["taskId"]==request.taskId&&(string)report["executionId"]==request.executionId&&(string)report["ownerThreadId"]==request.ownerThreadId&&(string)report["runId"]==request.runId&&(int)report["attempt"]==1&&(int)report["editorPid"]==123&&(string)report["requestSha256"]=="request-fixture"&&(string)report["status"]=="RUNNING"&&!(bool)report["passed"]&&report["checks"].Count()==5&&report["directions"].Count()==4,"Identity, current status and all checks/directions remain data");
        foreach(var observation in new[]{report["checks"][0]["observedSample"],report["directions"][0]["observedSample"]})foreach(var field in typeof(StreetProbeObservation).GetFields().Where(f=>f.FieldType==typeof(Vector3)))
        {
            var vector=(JObject)observation[field.Name];Need(vector.Count==3&&(float)vector["x"]==-2.5f&&(float)vector["y"]==4.5f&&(float)vector["z"]==-7,"All actual observation Vector3 fields retain their scalar coordinates");
        }
        Console.WriteLine(JsonConvert.SerializeObject(new {scenario="report-vector-routing",passed=true,json=report}));

        int calls=JsonUtility.Calls;var inspect=StreetMenuSerializationExpression.Inspect(true,new {version="offline-capability"},false);
        var ready=StreetMenuSerializationExpression.Ready(true,true,true,true,true,new Vector3(-2.5f,0,3.75f),new Vector3(0,4.5f,-7));var waiting=StreetMenuSerializationExpression.NotReady();
        foreach(var value in new[]{inspect,ready,waiting})Need(JToken.DeepEquals(JToken.Parse(StreetMenuSerializationExpression.Write(value)),JToken.Parse(JsonConvert.SerializeObject(value,Formatting.Indented))),"Anonymous actual return expressions keep the default Newtonsoft branch and values");
        var readyJson=JObject.Parse(StreetMenuSerializationExpression.Write(ready));var waitJson=JObject.Parse(StreetMenuSerializationExpression.Write(waiting));var inspectJson=JObject.Parse(StreetMenuSerializationExpression.Write(inspect));
        Need(JsonUtility.Calls==calls&&(bool)inspectJson["compiledMenu"]&&(bool)inspectJson["requestAccepted"]&&(bool)inspectJson["cleanupIdle"]&&!(bool)inspectJson["isPlaying"]&&inspectJson.Count==6&&(bool)readyJson["ready"]&&readyJson.Count==6&&(float)readyJson["playerPosition"]["x"]==-2.5f&&(float)readyJson["spawnPosition"]["z"]==-7&&((JObject)readyJson["playerPosition"]).Count==3&&((JObject)readyJson["spawnPosition"]).Count==3&&waitJson.Count==2&&!(bool)waitJson["ready"]&&(string)waitJson["reason"]=="WAIT_FOR_NORMAL_PLAY","Inspect/Ready/scalar and nonReady shapes unchanged; native flags are supplied offline inputs");
        Console.WriteLine(JsonConvert.SerializeObject(new {scenario="anonymous-menu-routing",passed=true,inspect=inspectJson,ready=readyJson,waiting=waitJson}));

        var notRun=new StreetSmokeSequence(request,0,null,Vector3.zero,false).Report(new StreetProbeCleanup());notRun.status="NOT_RUN";notRun.error="Begin not observed in this domain; do not assume success or replay.";
        var notRunJson=Write(notRun);sequence.Cancel("EXPLICIT_CANCEL");var terminal=sequence.Report(new StreetProbeCleanup {inputRestored=true,configsRestored=true,configOwnerRestored=true,callbacksRemoved=true,temporaryObjectsDestroyed=true,noForeignOverwrite=true});var terminalJson=Write(terminal);
        Need((string)notRunJson["status"]=="NOT_RUN"&&!(bool)notRunJson["passed"]&&!(bool)notRunJson["checks"][0]["observed"]&&(string)notRunJson["error"]==notRun.error&&(string)terminalJson["status"]=="FAILED"&&(string)terminalJson["error"]=="EXPLICIT_CANCEL"&&!(bool)terminalJson["passed"]&&terminal.cleanup.Passed&&!(bool)terminalJson["checks"][3]["observed"]&&!(bool)terminalJson["checks"][4]["observed"],"No run/cancel/native-not-run truth is retained");
        foreach(var field in typeof(StreetProbeCleanup).GetFields().Where(f=>f.FieldType==typeof(bool)))Need((bool)terminalJson["cleanup"][field.Name],"All six actual cleanup fields preserved");
        Console.WriteLine(JsonConvert.SerializeObject(new {scenario="not-run-and-terminal-report",passed=true,notRun=notRunJson,terminal=terminalJson}));
        Console.WriteLine(JsonConvert.SerializeObject(new {passedScenarios=3,jsonUtility="TRANSPARENT_PUBLIC_FIELD_STAND_IN",nativeJsonUtilityExecuted=false,unityNativeLaunched=false,realVectorAssembly=typeof(Vector3).Assembly.GetName().Name}));return 0;
    }
}
#elif STREET_READY_JSON_TESTS
using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using EatWhat.EditorValidation;
using UnityEngine;
internal static class StreetProbeChecksTests
{
    private static void Need(bool value,string reason){if(!value)throw new InvalidOperationException(reason);}
    public static int Main(string[] args)
    {
        if(args.Length==1&&args[0]=="--old-ready-vector")
        {
            // Actual installed Unity Vector3 and default Newtonsoft; expected uncaught loop failure is recorded separately.
            Console.WriteLine(JsonConvert.SerializeObject(new {playerPosition=new Vector3(-2.5f,0,3.75f),spawnPosition=new Vector3(0,4.5f,-7)},Formatting.Indented));return 0;
        }
        var player=new Vector3(-2.5f,0,3.75f);var spawn=new Vector3(0,4.5f,-7);
        var json=JsonConvert.SerializeObject(new {playerPosition=LongLoopProbeChecks.ReadyPosition(player),spawnPosition=LongLoopProbeChecks.ReadyPosition(spawn)},Formatting.Indented);
        var value=JObject.Parse(json);
        foreach(var pair in new[]{new {name="playerPosition",expected=player},new {name="spawnPosition",expected=spawn}})
        {
            var position=(JObject)value[pair.name];Need(position.Properties().Select(p=>p.Name).OrderBy(n=>n).SequenceEqual(new[]{"x","y","z"}),"Exactly three scalar coordinates, no derived Vector3 tree");
            Need((float)position["x"]==pair.expected.x&&(float)position["y"]==pair.expected.y&&(float)position["z"]==pair.expected.z,"Actual production projection preserves negative, zero and nonzero coordinates");
        }
        var waitingJson=JsonConvert.SerializeObject(StreetReadyNotReadyExpression.Read(),Formatting.Indented);var waiting=JObject.Parse(waitingJson);
        Need(waiting.Count==2&&waiting["ready"].Type==JTokenType.Boolean&&!(bool)waiting["ready"]&&(string)waiting["reason"]=="WAIT_FOR_NORMAL_PLAY","Actual compiled production nonReady expression keeps its shape");
        Console.WriteLine(JsonConvert.SerializeObject(new {passedScenarios=2,positionJson=json,nonReadyJson=waitingJson,unityVectorAssembly=typeof(Vector3).Assembly.GetName().Name,unityNativeLaunched=false}));return 0;
    }
}
#else
using System;
using System.Linq;
using EatWhat.EditorValidation;
using UnityEngine;
internal static class StreetProbeChecksTests
{
    private static int count;
    private static void Check(bool ok,string label){count++;if(!ok)throw new Exception(label);}
    private static void Reject(Action action,string label){bool failed=false;try{action();}catch(InvalidOperationException){failed=true;}Check(failed,label);}
    public static StreetProbeRequest Plan()
    {
        var r=new StreetProbeRequest {stages=LongLoopProbeChecks.StageIds.Select(id=>new StreetProbeStage {id=id,seconds=.5f,timeoutSeconds=4}).ToArray()};
        foreach(var s in r.stages)
        {
            if(s.id=="x-positive"||s.id=="ahead-right")s.input=new Vector2(1,0);
            if(s.id=="x-negative"||s.id=="ahead-left")s.input=new Vector2(-1,0);
            if(s.id=="y-positive")s.input=new Vector2(0,1);
            if(s.id=="y-negative")s.input=new Vector2(0,-1);
            if(s.id=="diagonal")s.input=new Vector2(1,1);
            if(s.id=="depth-mid")s.captureId="sorting";
            if(s.id=="edge-approach"||s.id.StartsWith("depth-"))s.navigate=true;
            if(s.id=="edge-slide")s.input=new Vector2(1,1);
            if(s.id=="edge-stop")s.input=new Vector2(0,1);
        }
        return r;
    }
    private static StreetProbeObservation Frame(int frame)=>new StreetProbeObservation {frame=frame,ready=true,inputOwned=true,configsOwned=true,speed=6.5f,verticalFactor=.6f};
    private static int Smoke()
    {
        int scenarios=0;
        StreetProbeRequest Request()=>new StreetProbeRequest {suite=StreetSmokeSequence.Suite,maximumSeconds=8,stages=null};
        StreetProbeObservation Observation(int frame,Vector3 foot,bool inside=true)=>new StreetProbeObservation {frame=frame,foot=foot,ready=true,inputOwned=true,inside=inside};
        StreetSmokeSequence New()=>new StreetSmokeSequence(Request(),0,Observation(0,Vector3.zero),Vector3.zero,true);
        var clean=new StreetProbeCleanup {inputRestored=true,configsRestored=true,configOwnerRestored=true,callbacksRemoved=true,temporaryObjectsDestroyed=true,noForeignOverwrite=true};
        var seq=New();var p=Vector3.zero;int persisted=0;
        for(int frame=1;frame<40&&!seq.Finished;frame++){var v=seq.Input;p+=new Vector3(v.x*.1f,v.y*.1f,0);seq.Tick(Observation(frame,p),frame*.1,s=>persisted++);}
        var report=seq.Report(clean);
        Check(seq.Finished&&seq.Error==null&&report.directions.Length==4&&report.directions.All(d=>d.observed&&d.passed),"smk.normal-four-direction-aggregate");
        Check(report.checks.Take(3).All(c=>c.observed&&c.passed)&&report.checks.Skip(3).All(c=>!c.observed&&!c.passed)&&!report.passed&&report.status=="COMPLETED","Native Console/Stop+lease remain NOT_RUN, never probe PASS");scenarios++;
        seq=New();seq.Tick(Observation(1,Vector3.zero),.4,s=>{});report=seq.Report(clean);
        Check(seq.Finished&&seq.Error.StartsWith("SMK_DIRECTION")&&!report.directions[0].passed&&report.directions.Skip(1).All(d=>!d.observed),"smk.zero-movement-fails");scenarios++;
        seq=New();seq.Tick(Observation(1,new Vector3(5,0,0),false),.1,s=>{});Check(seq.Finished&&seq.Error.Contains("INSIDE"),"smk.outside-walk-area-fails");scenarios++;
        seq=new StreetSmokeSequence(Request(),0,null,Vector3.zero,true);Check(seq.Finished&&seq.Error=="MISSING_PLAYER_OR_NOT_READY"&&seq.Report(clean).checks.All(c=>!c.passed),"smk.missing-player-not-pass");scenarios++;
        bool detached=false;var cleanup=StreetSmokeSequence.Cleanup(true,()=>{throw new InvalidOperationException("fixture restore exception");},()=>detached=true);
        Check(detached&&cleanup.callbacksRemoved&&!cleanup.Passed&&cleanup.error.Contains("restore exception"),"smk.exception-cleanup-detaches");scenarios++;
        seq=New();seq.Cancel("PLAY_EXIT");int restored=0,removed=0;cleanup=StreetSmokeSequence.Cleanup(true,()=>restored++,()=>removed++);
        Check(restored==1&&removed==1&&cleanup.Passed&&!seq.Report(cleanup).checks[2].passed,"smk.stop-cleanup-owned-only");
        cleanup=StreetSmokeSequence.Cleanup(false,()=>restored++,()=>removed++);Check(restored==1&&removed==2&&!cleanup.noForeignOverwrite&&!cleanup.Passed,"Foreign input/avatar is never overwritten");scenarios++;
        LongLoopProbeChecks.ValidatePlan(Plan());Check(LongLoopProbeChecks.RequiredIds.Length==32&&LongLoopProbeChecks.StageIds.Length==22,"smk.original-full-entry-remains32");
        Reject(()=>LongLoopProbeChecks.ValidatePlan(Request()),"Full Begin cannot silently select SMK");scenarios++;
        var context=new StreetPayloadContext {version=StreetPayloadContract.Version,implementationSha256=StreetPayloadContract.ImplementationSha256,taskId="AI-000999",attempt=1,executionId="fixture-execution",ownerThreadId="fixture-owner",runId="smoke",sessionId="fixture-session"};
        var q=Newtonsoft.Json.Linq.JObject.Parse("{\"tasks\":[{\"id\":\"AI-000999\",\"pipeline\":\"ENGINE_CLI\",\"status\":\"RUNNING\",\"attempts\":1,\"execution\":{\"executionId\":\"fixture-execution\",\"ownerThreadId\":\"fixture-owner\"}}]}");
        var w=Newtonsoft.Json.Linq.JObject.Parse("{\"workers\":{\"ENGINE_CLI\":{\"status\":\"BUSY\",\"activeTaskId\":\"AI-000999\",\"activeExecutionId\":\"fixture-execution\",\"threadId\":\"fixture-owner\"}}}");w["workers"]["ENGINE_CLI"]["leaseUntil"]=DateTimeOffset.UtcNow.AddMinutes(1).ToString("o");
        var m=Newtonsoft.Json.Linq.JObject.Parse("{\"mode\":\"SUPERVISED\",\"supervised\":{\"status\":\"ACTIVE\",\"sessionId\":\"fixture-session\",\"taskIds\":[\"AI-000999\"],\"stopRequested\":true,\"exitRequested\":false}}");
        bool inputApplied=false;Reject(()=>{StreetPayloadContract.ValidateLive(context,q,w,m,DateTimeOffset.UtcNow);inputApplied=true;},"smk.cancelled-claim-stops-before-input");Check(!inputApplied,"No simulated input after cancelled real guard");scenarios++;
        Console.WriteLine("PASS "+scenarios+" focused SMK scenarios / "+count+" assertions; Unity/Console/lease native checks NOT_RUN");return 0;
    }
    public static int Main(string[] args)
    {
        if(args.Length==1&&args[0]=="--smoke")return Smoke();
        var r=Plan();LongLoopProbeChecks.ValidatePlan(r);Check(LongLoopProbeChecks.RequiredIds.Distinct().Count()==32,"Fixed unique32 required IDs");
        var seq=new StreetProbeSequence(r,0);int persisted=0;
        seq.Tick(new StreetProbeObservation {frame=1,ready=false},1,s=>persisted++);
        Check(!seq.Finished&&seq.StageIndex==0&&persisted==0,"NotReady stays bounded ready, no skipped pass");
        seq.CheckDeadline(121);Check(seq.Finished&&seq.Error=="TOTAL_TIMEOUT_OR_CLOCK","one immutable total deadline");
        seq=new StreetProbeSequence(Plan(),0);var f=Frame(2);f.inputOwned=false;
        bool before=false;seq.Tick(f,.2,s=>{before=!seq.Finished;persisted++;});
        Check(before&&seq.Finished&&seq.Samples.Count==1,"critical ownership failure values persisted before failure");
        seq=new StreetProbeSequence(Plan(),0);seq.Cancel("EXPLICIT_CANCEL");seq.Tick(Frame(3),.2,s=>persisted++);
        Check(seq.Finished&&seq.Error=="EXPLICIT_CANCEL"&&seq.Samples.Count==0,"cancel terminal without rerun");
        seq=new StreetProbeSequence(Plan(),10);seq.CheckDeadline(9);Check(seq.Finished,"reverse clock rejects");
        r=Plan();r.stages[0].id="fake-ready";Reject(()=>LongLoopProbeChecks.ValidatePlan(r),"reordered phase rejects");
        r=Plan();r.stages[1].input=Vector2.zero;Reject(()=>LongLoopProbeChecks.ValidatePlan(r),"missing positiveX input rejects");
        r=Plan();r.edgeTangent=new Vector2(float.NaN,1);Reject(()=>LongLoopProbeChecks.ValidatePlan(r),"nonfinite tangent rejects");
        r=Plan();r.stages.Single(s=>s.id=="depth-mid").captureId=null;Reject(()=>LongLoopProbeChecks.ValidatePlan(r),"missing bounded real screenshot gate rejects");
        var empty=LongLoopProbeChecks.BuildReport(Plan(),Array.Empty<StreetProbeObservation>(),new StreetProbeCleanup());
        Check(empty.requiredCount==32&&empty.checks.Length==32&&empty.checks.All(c=>!c.passed&&!c.observed),"missing observations produce32 failed mandatory records, not PASS/skipped");
        f=Frame(1);f.foot=new Vector3(float.PositiveInfinity,0,0);Check(!LongLoopProbeChecks.FiniteObservation(f),"nonfinite raw actual rejects");
        r=Plan();foreach(var stage in r.stages){if(stage.id=="stopped-recenter")stage.seconds=2;}
        r.stages.Single(s=>s.id=="depth-low").destination=new Vector3(0,-1,0);r.stages.Single(s=>s.id=="depth-high").destination=new Vector3(0,1,0);
        var full=StreetProbeFixtureData.Sample(r);var report=StreetProbeFixtureData.Report(r,full);
        Check(full.Finished&&full.Samples.Count>60&&report.cleanup.Passed,"actual full sequence completes with owned cleanup boundary");
        foreach(var c in report.checks)Check(c.passed&&c.observed&&c.observedSample!=null,"actual predicate positive: "+c.id);
        var first=full.Samples.First(s=>s.phaseId=="x-positive");double old=first.gameTime;first.gameTime=full.Samples.Last(s=>s.phaseId=="x-positive").gameTime;
        Check(!LongLoopProbeChecks.Evaluate(r,full.Samples).Single(c=>c.id=="move.xPositive").passed,"paused actual movement clock cannot pass");first.gameTime=old;
        first=full.Samples.First(s=>s.phaseId=="depth-mid");var oldScale=first.visualScale;first.visualScale=new Vector3(1,9,1);
        Check(!LongLoopProbeChecks.Evaluate(r,full.Samples).Single(c=>c.id=="depth.mid").passed,"bad actual visual scale rejects");first.visualScale=oldScale;
        first=full.Samples.First(s=>s.phaseId=="f02-zero");first.channelZero=false;
        Check(!LongLoopProbeChecks.Evaluate(r,full.Samples).Single(c=>c.id=="channels.zero").passed,"nonzero channel rejects");
        first=full.Samples.First(s=>s.phaseId=="edge-slide");first.boundaryBlocked=false;
        Check(!LongLoopProbeChecks.Evaluate(r,full.Samples).Single(c=>c.id=="move.edgeSlide").passed,"interior tangential motion cannot masquerade as an actual boundary slide");
        seq=new StreetProbeSequence(Plan(),0);seq.Tick(null,.1,s=>{});Check(seq.Error=="MISSING_OBSERVATION","missing binding observation terminal");
        seq=new StreetProbeSequence(Plan(),0);seq.Tick(Frame(1),5,s=>{});Check(seq.Error.StartsWith("STAGE_TIMEOUT"),"perstage wait bounded independently");
        seq=new StreetProbeSequence(Plan(),0);f=Frame(1);f.configsOwned=false;seq.Tick(f,.1,s=>{});Check(seq.Finished&&seq.Samples.Count==1,"external config takeover persists then stops");
        seq=new StreetProbeSequence(Plan(),0);seq.Cancel("EXIT_PLAY_OR_RELOAD");Check(seq.Finished&&seq.Error=="EXIT_PLAY_OR_RELOAD","native exit adapter cancellation terminal");
        seq=new StreetProbeSequence(Plan(),0);seq.Tick(Frame(1),.1,s=>{});seq.Tick(Frame(1),.3,s=>{});Check(seq.Samples.Count==1,"duplicate natural frame cannot advance twice");
        first=full.Samples.First(s=>s.phaseId=="deadzone-hold");first.confinerClearForCameraTests=false;
        Check(!LongLoopProbeChecks.Evaluate(r,full.Samples).Single(c=>c.id=="camera.deadZone").passed,"Actual Confiner-influenced camera window cannot pass by disabling/categorizing away correction");
        int added=StreetConfigurationTests.Run();
        int timing=StreetProbeTimingTests.Run();
        Console.WriteLine("PASS "+(count+added+timing)+" actual Street probe/source/timing assertions; original94 retained, native pending");return 0;
    }
}
#endif
