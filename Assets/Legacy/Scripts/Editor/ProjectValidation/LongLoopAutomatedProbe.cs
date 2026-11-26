using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using EatWhat.LongLoop;
using EatWhat.LongLoop.Configuration;
using EatWhat.Toolkit.Input;
using Spine.Unity;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace EatWhat.EditorValidation
{
    [InitializeOnLoad]
    public static class LongLoopAutomatedProbe
    {
        private static Run active;
        private static SmokeRun smoke;
        private static FollowRRun followRSmoke;
        static LongLoopAutomatedProbe()
        {
            AssemblyReloadEvents.beforeAssemblyReload+=()=>CancelActive("ASSEMBLY_RELOAD");
            AssemblyReloadEvents.beforeAssemblyReload+=()=>smoke?.Stop("ASSEMBLY_RELOAD");
            AssemblyReloadEvents.beforeAssemblyReload+=()=>followRSmoke?.Stop("ASSEMBLY_RELOAD");
            EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.ExitingPlayMode)CancelActive("PLAY_EXIT");};
            EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.ExitingPlayMode)smoke?.Stop("PLAY_EXIT");};
            EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.ExitingPlayMode)followRSmoke?.Stop("PLAY_EXIT");};
        }
        public static string[] RequiredAssertions()=>LongLoopProbeChecks.RequiredIds.ToArray();
        public static object Capabilities()=>new {version=LongLoopProbeChecks.CapabilityVersion,sortingTimeoutSeconds=45,otherTimeoutSeconds=12,maximumSeconds=120,sampleLimit=1200,stages=LongLoopProbeChecks.StageIds.Length,requiredAssertions=LongLoopProbeChecks.RequiredIds.Length,shortPayloadVersion=StreetPayloadContract.Version,shortPayloadImplementationSha256=StreetPayloadContract.ImplementationSha256,smokeSuite=StreetSmokeSequence.Suite,smokeRequiredAssertions=5,followRSuite=FollowRSmokeSequence.Suite,followRRule=FollowRSmokeSequence.Rule,followRRequiredAssertions=5};

        // Official `unity menu` calls compiled methods. No eval payload, extra proof format or scene launcher.
        [MenuItem("Tools/Eat What/Street/SMK-011/Inspect")]
        public static void SmokeMenuInspect(){SmokeMenu("inspect");}
        [MenuItem("Tools/Eat What/Street/SMK-011/Begin")]
        public static void SmokeMenuBegin(){SmokeMenu("begin");}
        [MenuItem("Tools/Eat What/Street/SMK-011/Ready")]
        public static void SmokeMenuReady(){SmokeMenu("ready");}
        [MenuItem("Tools/Eat What/Street/SMK-011/Read")]
        public static void SmokeMenuRead(){SmokeMenu("read");}
        [MenuItem("Tools/Eat What/Street/SMK-011/Cancel")]
        public static void SmokeMenuCancel(){SmokeMenu("cancel");}
        private static void SmokeMenu(string action)
        {
            var project=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
            var workers=StreetPayloadContract.JsonFile(Path.Combine(project,".ai-workspace/runtime/worker-state.json"));
            var queue=StreetPayloadContract.JsonFile(Path.Combine(project,".ai-workspace/runtime/queue-state.json"));
            var id=(string)workers["workers"]?["ENGINE_CLI"]?["activeTaskId"];
            var task=((Newtonsoft.Json.Linq.JArray)queue["tasks"]).Single(t=>(string)t["id"]==id);
            var root=Path.Combine(project,".ai-workspace/outputs/PLAYABLE-DEMO011-20261104",id,"attempt"+(int)task["attempts"]);
            var followRoot=Path.Combine(project,".ai-workspace/outputs/FOLLOW-R-20261110",id,"attempt"+(int)task["attempts"]);
            var oldPath=Path.Combine(root,"smk-011-request.json");var newPath=Path.Combine(followRoot,"follow-r-request.json");NoLinks(oldPath);NoLinks(newPath);
            if(File.Exists(oldPath)&&File.Exists(newPath))throw new InvalidOperationException("Ambiguous cross-suite requests refused.");
            var path=File.Exists(newPath)?newPath:oldPath;root=File.Exists(newPath)?followRoot:root;
            var request=JsonUtility.FromJson<StreetProbeRequest>(File.ReadAllText(path));ValidateSmokeRequest(request,false,action);
            object result;
            bool follow=request.suite==FollowRSmokeSequence.Suite;
            if(action=="inspect"&&follow)result=new {suite=request.suite,compiledMenu=true,requestAccepted=true,cleanupIdle=(smoke==null||smoke.Terminal&&smoke.Report().cleanup.Passed)&&(followRSmoke==null||followRSmoke.Terminal&&followRSmoke.Report().cleanup.Passed),capabilities=Capabilities(),isPlaying=EditorApplication.isPlaying,readiness=FollowRReady(request)};
            else if(action=="inspect")result=new {suite=StreetSmokeSequence.Suite,compiledMenu=true,requestAccepted=true,cleanupIdle=smoke==null||smoke.Terminal&&smoke.Report().cleanup.Passed,capabilities=Capabilities(),isPlaying=EditorApplication.isPlaying};
            else if(action=="ready")result=follow?FollowRReady(request):SmokeReady(request);
            else if(action=="begin")result=BeginSmoke(JsonUtility.ToJson(request));
            else if(action=="read")result=ReadSmoke(request);
            else if(follow){if(followRSmoke==null||followRSmoke.Request.runId!=request.runId||followRSmoke.Request.executionId!=request.executionId)throw new InvalidOperationException("No matching owned FOLLOW-R run.");followRSmoke.Stop("EXPLICIT_CANCEL");result=followRSmoke.Report();}
            else {if(smoke==null||smoke.Request.runId!=request.runId)throw new InvalidOperationException("No owned SMK run to cancel.");smoke.Stop("EXPLICIT_CANCEL");result=smoke.Report();}
            var text=result is StreetProbeReport report?JsonUtility.ToJson(report,true):Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented);
            PublishProbeEvidence(Path.Combine(root,"smk-011-menu-"+action+"-"+Guid.NewGuid().ToString("N")+".json"),text);
            UnityEngine.Debug.Log("SMK-011 "+action+" "+text);
        }
        private static bool ValidateSmokeRequest(StreetProbeRequest r,bool frame=false,string action="frame")
        {
            if(r!=null&&r.suite==FollowRSmokeSequence.Suite)
            {
                if(ValidateFollowRClaim(r,action,out _))return true;
                if(frame)return false;throw new TimeoutException("FOLLOW-R current claim snapshot busy; no permission assumed.");
            }
            StreetPayloadContract.Need(r!=null&&r.schemaVersion==1&&r.suite==StreetSmokeSequence.Suite&&r.maximumSeconds>0&&r.maximumSeconds<=8&&(r.stages==null||r.stages.Length==0)&&
                System.Text.RegularExpressions.Regex.IsMatch(r.runId??"","^[A-Za-z0-9_-]{1,64}$"),"Explicit bounded SMK-011 request required.");
            var project=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
            StreetPayloadContract.Need(StreetPayloadContract.SamePath(r.projectPath,project)&&r.scenePath=="Assets/Scenes/LongLoop/LongLoop_Street.unity"&&SceneManager.GetActiveScene().path==r.scenePath,"Actual Street project/scene differs.");
            using(var process=Process.GetCurrentProcess())StreetPayloadContract.Need(r.editorPid==process.Id,"Actual Editor process differs.");
            var expected=Path.Combine(project,".ai-workspace/outputs/PLAYABLE-DEMO011-20261104",r.taskId,"attempt"+r.attempt,"smk-011",r.runId);
            StreetPayloadContract.Need(StreetPayloadContract.SamePath(r.outputRoot,expected),"SMK output must remain in this actual claim.");NoLinks(r.outputRoot);
            Newtonsoft.Json.Linq.JObject queue,workers,mode;
            if(frame){if(!StreetPayloadContract.TryReadLiveState(project,out queue,out workers,out mode))return false;}
            else {mode=StreetPayloadContract.JsonFile(Path.Combine(project,".ai-workspace/runtime/mode-state.json"));queue=StreetPayloadContract.JsonFile(Path.Combine(project,".ai-workspace/runtime/queue-state.json"));workers=StreetPayloadContract.JsonFile(Path.Combine(project,".ai-workspace/runtime/worker-state.json"));}
            var c=new StreetPayloadContext {version=StreetPayloadContract.Version,implementationSha256=StreetPayloadContract.ImplementationSha256,taskId=r.taskId,attempt=r.attempt,
                executionId=r.executionId,ownerThreadId=r.ownerThreadId,runId=r.runId,sessionId=(string)mode["supervised"]?["sessionId"]};
            StreetPayloadContract.ValidateLive(c,queue,workers,mode,DateTimeOffset.UtcNow);return true;
        }
        private static bool ValidateFollowRClaim(StreetProbeRequest r,string action,out DateTimeOffset leaseUntil)
        {
            leaseUntil=default;var project=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
            Newtonsoft.Json.Linq.JObject q,w,m;
            if(!StreetPayloadContract.TryReadLiveState(project,out q,out w,out m))return false;
            FollowRSmokeSequence.ValidateRequest(r,project,action,q,w,m,DateTimeOffset.UtcNow);
            using(var process=Process.GetCurrentProcess())StreetPayloadContract.Need(r.editorPid==process.Id&&StreetPayloadContract.Instant(r.editorStartedAt)==process.StartTime.ToUniversalTime(),"Actual FOLLOW-R Editor identity differs.");
            StreetPayloadContract.Need(SceneManager.GetActiveScene().path==r.scenePath,"Actual FOLLOW-R Street scene differs.");
            leaseUntil=StreetPayloadContract.Instant((string)w["workers"]["ENGINE_CLI"]["leaseUntil"]);return true;
        }
        private static object SmokeReady(StreetProbeRequest r)
        {
            if(!EditorApplication.isPlaying||EditorApplication.isCompiling||EditorApplication.isUpdating)return new {ready=false,reason="WAIT_FOR_NORMAL_PLAY"};
            var player=Resolve<LongLoopPlayerController>(r.playerPath);var spawn=Resolve<Transform>(r.spawnPath);
            var avatar=RuntimeField(player,"avatar",typeof(IPlayerAvatar)) as TomatoSpineAvatar;
            var input=RuntimeField(player,"input",typeof(IGameInput)) as IGameInput;var area=Ref<WalkArea>(player,"walkArea");
            bool initialized=avatar!=null&&avatar.isActiveAndEnabled&&Ref<SkeletonAnimation>(avatar,"skeletonAnimation")?.AnimationState!=null;
            bool neutral=input!=null&&input.Horizontal==0&&input.Vertical==0;
            return new {ready=player.isActiveAndEnabled&&initialized&&neutral&&area!=null&&area.IsUsable,avatarInitialized=initialized,neutralInput=neutral,
                playerPosition=LongLoopProbeChecks.ReadyPosition(player.transform.position),spawnPosition=LongLoopProbeChecks.ReadyPosition(spawn.position),isPlaying=EditorApplication.isPlaying};
        }
        public static StreetProbeReport BeginSmoke(string requestJson)
        {
            var request=JsonUtility.FromJson<StreetProbeRequest>(requestJson);
            if(request!=null&&request.suite==FollowRSmokeSequence.Suite)
            {
                if(!ValidateFollowRClaim(request,"begin",out var lease))throw new TimeoutException("FOLLOW-R Begin needs a fresh current claim; snapshot busy.");
                return BeginFollowR(request,lease);
            }
            ValidateSmokeRequest(request,false,"begin");
            if(followRSmoke!=null&&!followRSmoke.Terminal)throw new InvalidOperationException("FOLLOW-R probe already active.");
            if(!EditorApplication.isPlaying||EditorApplication.isCompiling||EditorApplication.isUpdating)throw new InvalidOperationException("Wait for the existing Street Play/Ready flow.");
            if(active!=null&&!active.Terminal)throw new InvalidOperationException("Full Street probe already active.");
            var sha=Hash(Encoding.UTF8.GetBytes(requestJson));
            if(smoke!=null){if(smoke.Request.executionId==request.executionId&&smoke.Request.runId==request.runId&&smoke.RequestSha==sha)return smoke.Report();if(!smoke.Terminal||smoke.Request.executionId==request.executionId)throw new InvalidOperationException("No SMK reset/rerun in the same execution.");}
            if(Directory.Exists(request.outputRoot))throw new InvalidOperationException("SMK evidence exists; read it rather than repeat Begin.");
            Directory.CreateDirectory(request.outputRoot);WriteNew(Path.Combine(request.outputRoot,"request.json"),requestJson);
            smoke=new SmokeRun(request,sha);try{smoke.Start();}catch(Exception ex){smoke.Stop("BEGIN:"+ex);}
            return smoke.Report();
        }
        private static StreetProbeReport ReadSmoke(StreetProbeRequest request)
        {
            if(request.suite==FollowRSmokeSequence.Suite)
            {
                if(followRSmoke!=null&&followRSmoke.Request.runId==request.runId&&followRSmoke.Request.executionId==request.executionId)return followRSmoke.Report();
                var path=Path.Combine(request.outputRoot,"run-report.json");NoLinks(path);
                if(File.Exists(path))
                {
                    var report=JsonUtility.FromJson<StreetProbeReport>(File.ReadAllText(path));
                    StreetPayloadContract.Need(report.suite==request.suite&&report.runId==request.runId&&report.executionId==request.executionId&&report.taskId==request.taskId&&report.attempt==request.attempt&&report.ownerThreadId==request.ownerThreadId&&StreetPayloadContract.SamePath(report.projectPath,request.projectPath)&&StreetPayloadContract.SamePath(report.outputRoot,request.outputRoot),"Saved FOLLOW-R report identity differs.");
                    ApplyFollowRConsole(request,report);return report;
                }
                return new StreetProbeReport {suite=request.suite,taskId=request.taskId,attempt=request.attempt,executionId=request.executionId,ownerThreadId=request.ownerThreadId,runId=request.runId,projectPath=request.projectPath,scenePath=request.scenePath,outputRoot=request.outputRoot,editorPid=request.editorPid,editorStartedAt=request.editorStartedAt,requiredCount=5,status="NOT_RUN",error="Begin not observed; Read never restarts Begin.",passed=false,checks=FollowRSmokeSequence.RequiredIds.Select(id=>new StreetProbeCheck {id=id,reason="NOT_RUN"}).ToArray(),cleanup=new StreetProbeCleanup()};
            }
            if(smoke!=null&&smoke.Request.runId==request.runId&&smoke.Request.executionId==request.executionId)return smoke.Report();
            var result=new StreetSmokeSequence(request,Time.realtimeSinceStartupAsDouble,null,Vector3.zero,false).Report(new StreetProbeCleanup());
            result.status="NOT_RUN";result.error="Begin not observed in this domain; do not assume success or replay.";return result;
        }
        private sealed class SmokeRun
        {
            public readonly StreetProbeRequest Request;public readonly string RequestSha;
            private readonly Proxy input=new Proxy();private LongLoopPlayerController player;private WalkArea area;private Transform spawn;
            private IGameInput originalInput;private IPlayerAvatar originalAvatar;private bool installed;
            private StreetSmokeSequence sequence;private StreetProbeReport terminal;private StreetProbeCleanup cleanup=new StreetProbeCleanup();
            private double nextClaimCheck;private int saved;private readonly string started=DateTimeOffset.UtcNow.ToString("o");
            public bool Terminal=>terminal!=null;
            public SmokeRun(StreetProbeRequest r,string sha){Request=r;RequestSha=sha;}
            private bool Owns()=>player!=null&&ReferenceEquals(RuntimeField(player,"input",typeof(IGameInput)),input)&&ReferenceEquals(RuntimeField(player,"avatar",typeof(IPlayerAvatar)),originalAvatar);
            private StreetProbeObservation Sample()=>new StreetProbeObservation {frame=Time.frameCount,gameTime=Time.timeAsDouble,foot=player.transform.position,
                ready=player.isActiveAndEnabled&&originalAvatar is TomatoSpineAvatar a&&a.isActiveAndEnabled&&Ref<SkeletonAnimation>(a,"skeletonAnimation")?.AnimationState!=null,
                inside=area!=null&&area.IsUsable&&area.Contains(new Vector2(player.transform.position.x,player.transform.position.y)),inputOwned=!installed||Owns()};
            public void Start()
            {
                player=Resolve<LongLoopPlayerController>(Request.playerPath);spawn=Resolve<Transform>(Request.spawnPath);area=Ref<WalkArea>(player,"walkArea");
                originalInput=RuntimeField(player,"input",typeof(IGameInput)) as IGameInput;originalAvatar=RuntimeField(player,"avatar",typeof(IPlayerAvatar)) as IPlayerAvatar;
                if(originalInput==null||originalInput.Horizontal!=0||originalInput.Vertical!=0)throw new InvalidOperationException("Actual neutral input required before SMK.");
                sequence=new StreetSmokeSequence(Request,Time.realtimeSinceStartupAsDouble,Sample(),spawn.position,true);
                if(sequence.Finished){Stop(null);return;}
                sequence.Pause(Time.realtimeSinceStartupAsDouble);
                player.ConfigureInterfaces(input,originalAvatar);installed=true;input.Move=Vector2.zero;player.StopMovement();EditorApplication.update+=Tick;
            }
            private void Tick()
            {
                if(Terminal)return;
                try{
                    if(!EditorApplication.isPlaying||EditorApplication.isCompiling||EditorApplication.isUpdating){Stop("PLAY_EXIT_OR_RELOAD");return;}
                    if(!Owns()){Stop("SMK_INPUT_OWNERSHIP_LOST");return;}
                    var now=Time.realtimeSinceStartupAsDouble;sequence.CheckDeadline(now);if(sequence.Finished){Stop(null);return;}
                    if(now>=nextClaimCheck)
                    {
                        bool fresh=ValidateSmokeRequest(Request,true);
                        now=Time.realtimeSinceStartupAsDouble;sequence.CheckDeadline(now);if(sequence.Finished){Stop(null);return;}
                        if(!fresh){input.Move=Vector2.zero;player.StopMovement();sequence.Pause(now);return;}
                        nextClaimCheck=now+.5;
                        if(sequence.Paused){sequence.Resume(now);input.Move=sequence.Input;return;}
                    }
                    input.Move=sequence.Tick(Sample(),now,s=>{var path=Path.Combine(Request.outputRoot,"samples.jsonl");NoLinks(path);using(var f=new FileStream(path,FileMode.Append,FileAccess.Write,FileShare.Read))using(var w=new StreamWriter(f,new UTF8Encoding(false)))w.WriteLine(JsonUtility.ToJson(s));saved++;});
                    if(sequence.Finished)Stop(null);
                }catch(Exception ex){Stop("SMK_FRAME:"+ex);}
            }
            public void Stop(string reason)
            {
                if(Terminal)return;input.Move=Vector2.zero;
                if(sequence==null)sequence=new StreetSmokeSequence(Request,Time.realtimeSinceStartupAsDouble,null,Vector3.zero,false);
                if(reason!=null)sequence.Cancel(reason);
                cleanup=StreetSmokeSequence.Cleanup(!installed||Owns(),()=>{if(installed){player.StopMovement();player.ConfigureInterfaces(originalInput,originalAvatar);}},()=>EditorApplication.update-=Tick);
                terminal=Report();terminal.finishedAt=DateTimeOffset.UtcNow.ToString("o");
                if(reason!=null)terminal.error=reason;
                PublishProbeEvidence(Path.Combine(Request.outputRoot,"run-report.json"),JsonUtility.ToJson(terminal,true));
            }
            public StreetProbeReport Report()
            {
                if(terminal!=null)return terminal;
                var result=sequence.Report(cleanup);result.requestSha256=RequestSha;result.startedAt=started;result.samplesSaved=saved;result.unityVersion=Application.unityVersion;
                result.assemblyPath=typeof(LongLoopAutomatedProbe).Assembly.Location;result.assemblySha256=Hash(File.ReadAllBytes(result.assemblyPath));return result;
            }
        }
        private static object FollowRReady(StreetProbeRequest r)
        {
            try
            {
                string reason;bool ready=FollowRBindings(r,out var p,out var d,out var c,out var b,out reason);
                return new {ready,reason,sceneWiringRequired=d==null||d.CameraTarget==null||d.FollowPoint==null||c==null||c.Follow!=d.CameraTarget,compiledRule=FollowRSmokeSequence.Rule,isPlaying=EditorApplication.isPlaying,
                    player=p==null?null:LongLoopProbeChecks.ReadyPosition(p.transform.position),target=d?.CameraTarget==null?null:LongLoopProbeChecks.ReadyPosition(d.CameraTarget.position)};
            }
            catch(Exception e){return new {ready=false,reason="SCENE_NOT_WIRED:"+e.Message,compiledRule=FollowRSmokeSequence.Rule,isPlaying=EditorApplication.isPlaying};}
        }
        private static bool FollowRBindings(StreetProbeRequest r,out LongLoopPlayerController p,out StreetFollowDriver d,out CinemachineCamera c,out CinemachineBrain b,out string reason)
        {
            p=null;d=null;c=null;b=null;reason="FOLLOW_R_SCENE_WIRING_PENDING";
            p=Resolve<LongLoopPlayerController>(r.playerPath);c=Resolve<CinemachineCamera>(r.cameraPath);b=Resolve<CinemachineBrain>(r.brainPath);d=c.GetComponent<StreetFollowDriver>();
            if(d==null||!d.isActiveAndEnabled||d.Binding==null||!d.Binding.IsReady||d.FollowPoint==null||d.FollowPoint.Foot==null||d.FollowPoint.Foot!=p.transform&&(!d.FollowPoint.Foot.IsChildOf(p.transform)||d.FollowPoint.Foot.GetComponentInParent<LongLoopPlayerController>()!=p)||d.CameraTarget==null||d.CameraTarget.parent!=null||c.Follow!=d.CameraTarget||
                d.CameraTarget==p.transform||d.CameraTarget==d.FollowPoint.Node||d.CameraTarget.IsChildOf(p.transform)||p.transform.IsChildOf(d.CameraTarget)||c.transform.IsChildOf(d.CameraTarget))return false;
            if(!EditorApplication.isPlaying||EditorApplication.isCompiling||EditorApplication.isUpdating){reason="WAIT_FOR_NORMAL_PLAY_AFTER_FR03_WIRING";return false;}
            var config=StreetConfigs.Current;var avatar=RuntimeField(p,"avatar",typeof(IPlayerAvatar)) as TomatoSpineAvatar;
            var input=RuntimeField(p,"input",typeof(IGameInput)) as IGameInput;
            bool ok=p.isActiveAndEnabled&&b.isActiveAndEnabled&&b.OutputCamera!=null&&!b.IsBlending&&CinemachineCore.IsLive(c)&&d.LastResult.Valid&&
                d.PipelineState.Initialized&&(d.Status==StreetFollowDriverStatus.Applied||d.Status==StreetFollowDriverStatus.HeldTime)&&config?.CameraFollow!=null&&config.DepthScale!=null&&
                avatar!=null&&avatar.isActiveAndEnabled&&Ref<SkeletonAnimation>(avatar,"skeletonAnimation")?.AnimationState!=null&&input!=null&&input.Horizontal==0&&input.Vertical==0&&!input.Interact&&
                Math.Abs(b.OutputCamera.transform.position.x-d.CameraTarget.position.x)<=.01&&Math.Abs(d.PipelineState.Lookahead.SignedWeight)<=.001&&
                (d.PipelineState.Goal-d.FollowPoint.Node.position).magnitude<=.01;
            reason=ok?"READY":"WAIT_FOR_CURRENT_DRIVER_AVATAR_NEUTRAL_AND_SETTLED_X";return ok;
        }
        private static StreetProbeReport BeginFollowR(StreetProbeRequest r,DateTimeOffset validatedLeaseUntil)
        {
            if(active!=null&&!active.Terminal||smoke!=null&&!smoke.Terminal)throw new InvalidOperationException("Another Street suite owns the probe.");
            string canonical=JsonUtility.ToJson(r),sha=Hash(Encoding.UTF8.GetBytes(canonical));
            if(followRSmoke!=null)
            {
                if(followRSmoke.Request.executionId==r.executionId&&followRSmoke.Request.runId==r.runId&&followRSmoke.RequestSha==sha)return followRSmoke.Report();
                if(!followRSmoke.Terminal||followRSmoke.Request.executionId==r.executionId)throw new InvalidOperationException("FOLLOW-R same-execution rerun refused.");
            }
            string reason;if(!FollowRBindings(r,out var p,out var d,out var c,out var b,out reason))throw new InvalidOperationException(reason);
            var parent=Path.GetDirectoryName(r.outputRoot);NoLinks(parent);
            if(Directory.Exists(parent)&&Directory.EnumerateFileSystemEntries(parent).Any())throw new InvalidOperationException("FOLLOW-R attempt already registered; Read only, including after domain reload.");
            StreetPayloadContract.Need(r.sources!=null&&r.sources.Length>=2&&r.sources.Any(f=>StreetPayloadContract.SamePath(f.path,Path.Combine(r.projectPath,"Assets/Editor/ProjectValidation/LongLoopAutomatedProbe.cs")))&&r.sources.Any(f=>StreetPayloadContract.SamePath(f.path,Path.Combine(r.projectPath,"Assets/Editor/ProjectValidation/LongLoopProbeChecks.cs"))),"Reviewed current probe sources required.");
            foreach(var f in r.sources){NoLinks(f.path);StreetPayloadContract.Need(Path.GetFullPath(f.path).StartsWith(Path.GetFullPath(r.projectPath).TrimEnd('\\','/')+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)&&Hash(File.ReadAllBytes(f.path))==f.sha256,"FOLLOW-R source drift.");}
            Directory.CreateDirectory(r.outputRoot);WriteNew(Path.Combine(r.outputRoot,"request.json"),canonical);
            followRSmoke=new FollowRRun(r,sha,p,d,c,b,validatedLeaseUntil);try{followRSmoke.Start();}catch(Exception e){followRSmoke.Stop("BEGIN:"+e);}
            return followRSmoke.Report();
        }
        private static void ApplyFollowRConsole(StreetProbeRequest r,StreetProbeReport report)
        {
            if(report.followR==null||report.checks==null||string.IsNullOrEmpty(report.finishedAt))return;
            string root=FollowRSmokeSequence.AttemptRoot(r.projectPath,r),before=Path.Combine(root,"follow-r-console-before.json"),after=Path.Combine(root,"follow-r-console-after.json");
            report.followR.consoleBeforePath=before;report.followR.consoleAfterPath=after;
            StreetProbeCheck check=new StreetProbeCheck {id=FollowRSmokeSequence.RequiredIds[4],reason="UNKNOWN_NATIVE_CONSOLE_WINDOW_FILES"};
            try
            {
                if(File.Exists(before)&&File.Exists(after))
                {
                    NoLinks(before);NoLinks(after);report.followR.consoleBeforeSha256=StreetPayloadContract.HashFile(before);report.followR.consoleAfterSha256=StreetPayloadContract.HashFile(after);
                    check=FollowRSmokeSequence.ConsoleCheck(StreetPayloadContract.JsonFile(before),StreetPayloadContract.JsonFile(after),StreetPayloadContract.Instant(report.startedAt),StreetPayloadContract.Instant(report.finishedAt),r.projectPath,report.followR);
                }
            }
            catch(Exception e){check.reason="UNKNOWN_NATIVE_CONSOLE_WINDOW:"+e.Message;}
            if(report.followR.callbackErrors>0){check.observed=true;check.passed=false;check.reason="REAL_LOG_CALLBACK_ERROR_DURING_RUN";}
            report.checks[4]=check;report.passed=report.error==null&&report.cleanup?.Passed==true&&report.checks.All(c=>c.observed&&c.passed);
            report.status=report.error!=null||report.checks.Any(c=>c.observed&&!c.passed)||report.cleanup?.Passed!=true?"FAILED":report.passed?"COMPLETED":"AWAITING_NATIVE_CONSOLE";
        }
        private sealed class FollowRRun
        {
            public readonly StreetProbeRequest Request;public readonly string RequestSha;
            private readonly LongLoopPlayerController player;private readonly StreetFollowDriver driver;private readonly CinemachineCamera camera;private readonly CinemachineBrain brain;
            private readonly Proxy input=new Proxy();private IGameInput originalInput;private IPlayerAvatar originalAvatar;private WalkArea area;
            private FollowRSmokeSequence sequence;private FollowRSmokeEvidence startup=new FollowRSmokeEvidence {phase="awaiting-natural-baseline"};
            private double registeredWall,startupLastWall,registeredGame;private float frozenTau,frozenSpeed,frozenWidth;private StreetProbeCleanup cleanup=new StreetProbeCleanup();private StreetProbeReport terminal;
            private StreetFollowSettings settings;private CameraInterestConfig interest;private bool installed,attached;private int brainFrame=-1,sampledFrame=-1,saved,callbackErrors;
            private int producerFrame=-1;private ulong producerSequence;private float cmDampingDelta;
            private Vector3 finalPosition;private Vector2 originalLook;private double nextClaimCheck,lastFreshClaimWall,busyDeadline;private bool snapshotBusy;
            private DateTimeOffset validatedLeaseUntil;private int freshSnapshots=1,busySnapshots,recoveredSnapshots,invalidSnapshots;private string snapshotState="FRESH_BEGIN";
            private readonly string started=DateTimeOffset.UtcNow.ToString("o");
            private sealed class InputReading
            {public StreetProbeInputObservation Evidence;public float Horizontal,Vertical;public bool Interact,Back;public Vector2 Look,Mouse;public GameInputDevice Device;}
            private InputReading initialInput,lastInput;private StreetProbeInputObservation stoppedInput;private Vector2 originalMouse;private bool mouseReferenceKnown;
            private string inputStopError="FOLLOW_R_USER_INPUT";
            public bool Terminal=>terminal!=null;
            public FollowRRun(StreetProbeRequest r,string sha,LongLoopPlayerController p,StreetFollowDriver d,CinemachineCamera c,CinemachineBrain b,DateTimeOffset lease){Request=r;RequestSha=sha;player=p;driver=d;camera=c;brain=b;validatedLeaseUntil=lease;}
            private bool Owns()=>ReferenceEquals(RuntimeField(player,"input",typeof(IGameInput)),input)&&ReferenceEquals(RuntimeField(player,"avatar",typeof(IPlayerAvatar)),originalAvatar);
            public void Start()
            {
                originalInput=RuntimeField(player,"input",typeof(IGameInput)) as IGameInput;originalAvatar=RuntimeField(player,"avatar",typeof(IPlayerAvatar)) as IPlayerAvatar;area=Ref<WalkArea>(player,"walkArea");
                initialInput=ReadOriginalInput();originalLook=initialInput.Look;mouseReferenceKnown=originalInput is LegacyInputProvider&&LegacyLookMatchesMouse(initialInput);if(mouseReferenceKnown)originalMouse=initialInput.Mouse;
                var config=StreetConfigs.Current;settings=CameraFollowConfigBinder.Snapshot(config.CameraFollow);interest=driver.InterestConfig;
                finalPosition=brain.OutputCamera.transform.position;
                registeredWall=startupLastWall=Time.realtimeSinceStartupAsDouble;
                lastFreshClaimWall=registeredWall;nextClaimCheck=registeredWall+FollowRSmokeSequence.ClaimCheckSeconds;
                registeredGame=Time.timeAsDouble;startup.startupFrame=Time.frameCount;startup.startupSequence=driver.EvaluationSequence;startup.startupGameTime=FollowRSmokeSequence.Clock(registeredGame);
                frozenTau=config.CameraFollow.DampingX;frozenSpeed=player.MoveSpeed;frozenWidth=2*camera.Lens.OrthographicSize*camera.Lens.Aspect;
                player.ConfigureInterfaces(input,originalAvatar);installed=true;input.Move=Vector2.zero;player.StopMovement();
                attached=true;driver.EvaluationCompleted+=ObserveProducerClock;CinemachineCore.CameraUpdatedEvent.AddListener(Brain);Application.onBeforeRender+=NaturalFrame;EditorApplication.update+=Watch;
                StreetConfigs.Changed+=ConfigChanged;if(interest!=null)interest.Changed+=ConfigChanged;Application.logMessageReceived+=Log;
                if(InputDetected(initialInput))Stop(inputStopError);
                // A synchronous Begin is registration, not a render sample. First complete Brain+Render freezes the baseline while input remains zero.
            }
            private InputReading ReadOriginalInput()
            {
                var s=new InputReading {Evidence=new StreetProbeInputObservation {frame=Time.frameCount,wallTime=FollowRSmokeSequence.Clock(Time.realtimeSinceStartupAsDouble),applicationFocused=Application.isFocused,screenWidth=Screen.width,screenHeight=Screen.height,inputAvailable=originalInput!=null,inputType=originalInput==null?"NULL":originalInput.GetType().AssemblyQualifiedName}};lastInput=s;
                if(originalInput is MonoBehaviour mono)s.Evidence.providerEnabled=mono.isActiveAndEnabled?"true":"false";
                if(originalInput==null){s.Evidence.reason="NULL_INPUT";s.Evidence.complete=true;return s;}
                string Number(float v)=>v.ToString("R",System.Globalization.CultureInfo.InvariantCulture);
                try
                {
                    s.Horizontal=originalInput.Horizontal;s.Evidence.horizontal=Number(s.Horizontal);s.Vertical=originalInput.Vertical;s.Evidence.vertical=Number(s.Vertical);
                    s.Interact=originalInput.Interact;s.Evidence.interact=s.Interact?"true":"false";
                    if(originalInput is INavigationInput nav){s.Back=nav.BackPressed;s.Evidence.backPressed=s.Back?"true":"false";}
                    s.Look=originalInput.LookAxis;s.Evidence.lookX=Number(s.Look.x);s.Evidence.lookY=Number(s.Look.y);
                    s.Mouse=originalInput.MouseScreenPos;s.Evidence.mouseX=Number(s.Mouse.x);s.Evidence.mouseY=Number(s.Mouse.y);
                    s.Device=originalInput.CurrentDevice;s.Evidence.device=s.Device.ToString();s.Evidence.complete=true;return s;
                }
                catch(Exception e){s.Evidence.reason="INPUT_READ_FAILED";s.Evidence.readError=e.ToString();stoppedInput=s.Evidence;throw;}
            }
            private static double LookDifference(Vector2 a,Vector2 b)=>Math.Abs(a.x-b.x)+Math.Abs(a.y-b.y);
            private static bool FiniteInput(Vector2 v)=>!float.IsNaN(v.x)&&!float.IsInfinity(v.x)&&!float.IsNaN(v.y)&&!float.IsInfinity(v.y);
            private static bool LegacyLookMatchesMouse(InputReading s)=>s.Evidence.inputAvailable&&s.Evidence.complete&&s.Device==GameInputDevice.Keyboard&&s.Evidence.screenWidth>0&&s.Evidence.screenHeight>0&&FiniteInput(s.Look)&&FiniteInput(s.Mouse)&&LookDifference(s.Look,GameInputMath.LookAxisFromScreenPosition(s.Mouse,s.Evidence.screenWidth,s.Evidence.screenHeight))<=.001;
            private bool InputDetected(InputReading s)
            {
                bool StopInput(string reason,bool unknown=false){s.Evidence.reason=reason;stoppedInput=s.Evidence;inputStopError=unknown?"FOLLOW_R_INPUT_CONTEXT_UNAVAILABLE":"FOLLOW_R_USER_INPUT";return true;}
                if(!s.Evidence.inputAvailable)return StopInput("NULL_INPUT");
                if(s.Horizontal!=0)return StopInput("HORIZONTAL");if(s.Vertical!=0)return StopInput("VERTICAL");if(s.Interact)return StopInput("INTERACT");if(s.Back)return StopInput("BACK");
                if(!FiniteInput(s.Look))return StopInput("NONFINITE_LOOK",true);
                Vector2 reference=originalLook;
                if(originalInput is LegacyInputProvider&&s.Device==GameInputDevice.Keyboard)
                {
                    // Exact neutral gating is a known provider output, not a time grace or a new mouse baseline.
                    if(s.Look.x==0&&s.Look.y==0&&s.Mouse.x==0&&s.Mouse.y==0){s.Evidence.context="LEGACY_NEUTRAL_GATED_OUTPUT";s.Evidence.reason="NO_OBSERVED_INPUT";return false;}
                    if(!LegacyLookMatchesMouse(s))return StopInput("LEGACY_LOOK_CONTEXT_UNRESOLVED",true);
                    if(!mouseReferenceKnown)
                    {
                        if(LookDifference(s.Look,originalLook)>.001)return StopInput("NO_PRIOR_ACTIVE_MOUSE_REFERENCE",true);
                        originalMouse=s.Mouse;mouseReferenceKnown=true;s.Evidence.context="REFERENCE_OBSERVED_WITHOUT_RELAXING_LOOK_GATE";
                    }
                    else s.Evidence.context="LEGACY_FIXED_MOUSE_CURRENT_SCREEN";
                    reference=GameInputMath.LookAxisFromScreenPosition(originalMouse,s.Evidence.screenWidth,s.Evidence.screenHeight);
                }
                else s.Evidence.context="ORIGINAL_PROVIDER_AXIS_REFERENCE";
                s.Evidence.referenceLookX=reference.x.ToString("R",System.Globalization.CultureInfo.InvariantCulture);s.Evidence.referenceLookY=reference.y.ToString("R",System.Globalization.CultureInfo.InvariantCulture);
                if(LookDifference(s.Look,reference)>.001)return StopInput("LOOK_AXIS");s.Evidence.reason="NO_OBSERVED_INPUT";return false;
            }
            private bool UserInput()=>InputDetected(ReadOriginalInput());
            private void ConfigChanged(){Stop("FOLLOW_R_CONFIGURATION_CHANGED");}
            private void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)callbackErrors++;}
            private StreetProbeObservation Sample()
            {
                var foot=driver?.FollowPoint?.Foot;
                var body=Ref<Rigidbody2D>(player,"targetRigidbody");
                Vector2 movementPosition=body!=null?body.position:new Vector2(player.transform.position.x,player.transform.position.y);
                bool valid=player!=null&&driver!=null&&brain!=null&&brain.OutputCamera!=null&&area!=null&&area.IsUsable&&driver.LastResult.Valid&&
                    driver.FollowPoint!=null&&foot!=null&&(foot==player.transform||foot.IsChildOf(player.transform)&&foot.GetComponentInParent<LongLoopPlayerController>()==player)&&driver.FollowPoint.Node!=null&&driver.CameraTarget!=null&&driver.CameraTarget.parent==null&&camera.Follow==driver.CameraTarget&&
                    !brain.IsBlending&&CinemachineCore.IsLive(camera)&&(driver.Status==StreetFollowDriverStatus.Applied||driver.Status==StreetFollowDriverStatus.HeldTime)&&
                    driver.LastResult.GoalStep.Status!=FollowStepStatus.TeleportReset&&player.isActiveAndEnabled&&originalAvatar is TomatoSpineAvatar a&&a.isActiveAndEnabled&&
                    Ref<SkeletonAnimation>(a,"skeletonAnimation")?.AnimationState!=null&&Math.Abs(camera.State.PositionCorrection.x)<=.001&&
                    Vector3.Dot(camera.transform.right,Vector3.right)>.9999&&Vector3.Dot(brain.OutputCamera.transform.right,Vector3.right)>.9999;
                return new StreetProbeObservation {frame=Time.frameCount,gameTime=Time.timeAsDouble,ready=valid,inputOwned=!installed||Owns(),followRTargetPipeline=true,
                    inside=area!=null&&area.IsUsable&&area.Contains(movementPosition),foot=foot==null?Vector3.zero:foot.position,
                    followPoint=driver.FollowPoint?.Node==null?Vector3.zero:driver.FollowPoint.Node.position,target=driver.CameraTarget==null?Vector3.zero:driver.CameraTarget.position,
                    goal=driver.PipelineState.Goal,brainOutput=finalPosition,cameraOutput=brain.State.GetFinalPosition(),inputX=input.Move.x,inputY=input.Move.y,
                    targetStatus=driver.Status.ToString(),sequence=driver.EvaluationSequence,producerFrame=producerFrame,producerSequence=producerSequence,cmDampingDeltaTime=cmDampingDelta,producerTime=driver.PipelineState.LastTime,producerDeltaTime=driver.LastResult.GoalStep.Elapsed,stopped=(float)driver.PipelineState.StoppedSeconds,delay=settings.RecenterDelay,size=camera.Lens.OrthographicSize,aspect=camera.Lens.Aspect};
            }
            private void ObserveProducerClock(StreetFollowDriver source,CinemachineVirtualCameraBase owner,ulong serial,CameraState snapshot,float dt)
            {if(Terminal||source!=driver||owner!=camera)return;producerFrame=Time.frameCount;producerSequence=serial;cmDampingDelta=dt;}
            private void Brain(CinemachineBrain source)
            {if(Terminal||source!=brain)return;brainFrame=Time.frameCount;finalPosition=brain.OutputCamera.transform.position;}
            private void NaturalFrame()
            {
                if(Terminal||sampledFrame==Time.frameCount||brainFrame!=Time.frameCount)return;sampledFrame=Time.frameCount;
                try
                {
                    if(!Owns()){Stop("FOLLOW_R_INPUT_OWNERSHIP_LOST");return;}
                    if(UserInput()){Stop(inputStopError);return;}
                    if(!CheckClaim(Time.realtimeSinceStartupAsDouble))return;
                    if(sequence==null)
                    {
                        double wall=Time.realtimeSinceStartupAsDouble;
                        if(!StartupClock(wall))return;
                        var initial=Sample();startup.failurePreviousFrame=startup.startupFrame;startup.failureCurrentFrame=initial.frame;startup.failurePreviousSequence=startup.startupSequence;startup.failureCurrentSequence=initial.sequence;startup.failurePreviousGameTime=startup.startupGameTime;startup.failureCurrentGameTime=FollowRSmokeSequence.Clock(initial.gameTime);startup.failureDeltaTime=FollowRSmokeSequence.Clock(initial.gameTime-registeredGame);
                        if(double.IsNaN(initial.gameTime)||double.IsInfinity(initial.gameTime)||initial.gameTime<registeredGame||initial.frame<startup.startupFrame||initial.sequence<startup.startupSequence){startup.failureReason="FOLLOW_R_STARTUP_REVERSED_OR_INVALID_CLOCK_SEQUENCE";Stop(startup.failureReason);return;}
                        sequence=new FollowRSmokeSequence(Request,wall,initial,settings,frozenTau,frozenSpeed,frozenWidth,registeredWall);
                        sequence.Evidence.startupFrame=startup.startupFrame;sequence.Evidence.startupSequence=startup.startupSequence;sequence.Evidence.startupGameTime=startup.startupGameTime;
                        sequence.Evidence.baselineFrame=initial.frame;sequence.Evidence.baselineSequence=initial.sequence;sequence.Evidence.baselineGameTime=FollowRSmokeSequence.Clock(initial.gameTime);sequence.Evidence.startupWallSeconds=wall-registeredWall;
                        WriteNew(Path.Combine(Request.outputRoot,"frozen-rule.json"),JsonUtility.ToJson(sequence.Evidence,true));
                        if(sequence.Finished)FinishSequence();return;
                    }
                    input.Move=sequence.Tick(Sample(),Time.realtimeSinceStartupAsDouble,driver.PipelineState,s=>{var p=Path.Combine(Request.outputRoot,"samples.jsonl");NoLinks(p);using(var f=new FileStream(p,FileMode.Append,FileAccess.Write,FileShare.Read))using(var w=new StreamWriter(f,new UTF8Encoding(false)))w.WriteLine(JsonUtility.ToJson(s));saved++;});
                    if(sequence.Finished)FinishSequence();
                }
                catch(Exception e){if(sequence==null)startup.failureReason="FOLLOW_R_STARTUP_BASELINE_INVALID:"+e.Message;Stop("FOLLOW_R_FRAME:"+e);}
            }
            private bool StartupClock(double now)
            {
                if(double.IsNaN(now)||double.IsInfinity(now)||double.IsNaN(registeredWall)||double.IsInfinity(registeredWall)||double.IsNaN(registeredGame)||double.IsInfinity(registeredGame)||now<startupLastWall||now-registeredWall>=Request.maximumSeconds)
                {startup.failureReason="FOLLOW_R_STARTUP_TIMEOUT_OR_CLOCK";startup.failurePreviousWallTime=FollowRSmokeSequence.Clock(startupLastWall);startup.failureCurrentWallTime=FollowRSmokeSequence.Clock(now);Stop(startup.failureReason);return false;}
                startupLastWall=now;return true;
            }
            private StreetProbeReport Pending()
            {
                return new StreetProbeReport {suite=FollowRSmokeSequence.Suite,taskId=Request.taskId,attempt=Request.attempt,executionId=Request.executionId,ownerThreadId=Request.ownerThreadId,runId=Request.runId,projectPath=Request.projectPath,scenePath=Request.scenePath,outputRoot=Request.outputRoot,editorPid=Request.editorPid,editorStartedAt=Request.editorStartedAt,requiredCount=5,status="RUNNING",passed=false,cleanup=cleanup,followR=startup,
                    checks=FollowRSmokeSequence.RequiredIds.Select(id=>new StreetProbeCheck {id=id,reason="AWAITING_FIRST_COMPLETE_NATURAL_FRAME"}).ToArray()};
            }
            private void Watch()
            {
                if(Terminal)return;
                try
                {
                    if(!EditorApplication.isPlaying||EditorApplication.isCompiling||EditorApplication.isUpdating){Stop("PLAY_EXIT_OR_RELOAD");return;}
                    if(!Owns()){Stop("FOLLOW_R_INPUT_OWNERSHIP_LOST");return;}
                    if(UserInput()){Stop(inputStopError);return;}
                    double now=Time.realtimeSinceStartupAsDouble;if(sequence==null){if(!StartupClock(now))return;}else{sequence.CheckDeadline(now);if(sequence.Finished){FinishSequence();return;}}
                    CheckClaim(now);
                }
                catch(Exception e){Stop("FOLLOW_R_WATCH:"+e);}
            }
            private bool CheckClaim(double now,bool requireFresh=false)
            {
                if(DateTimeOffset.UtcNow>=validatedLeaseUntil){snapshotState="LEASE_EXPIRED";Stop("FOLLOW_R_LAST_VALIDATED_LEASE_EXPIRED");return false;}
                if(snapshotBusy&&now>=busyDeadline){snapshotState="BUSY_TIMEOUT";Stop("FOLLOW_R_LIVE_STATE_BUSY_TIMEOUT");return false;}
                if(!requireFresh&&now<nextClaimCheck)return true;
                try
                {
                    if(!ValidateFollowRClaim(Request,"frame",out var lease))
                    {
                        busySnapshots++;snapshotState="BUSY_NOT_FRESH";
                        if(!snapshotBusy){snapshotBusy=true;busyDeadline=nextClaimCheck+FollowRSmokeSequence.ClaimCheckSeconds;}
                        if(requireFresh||now>=busyDeadline){snapshotState=requireFresh?"TERMINAL_BUSY":"BUSY_TIMEOUT";Stop(requireFresh?"FOLLOW_R_TERMINAL_SNAPSHOT_BUSY":"FOLLOW_R_LIVE_STATE_BUSY_TIMEOUT");return false;}
                        return true;
                    }
                    if(snapshotBusy)recoveredSnapshots++;snapshotState=snapshotBusy?"FRESH_RECOVERED":"FRESH";snapshotBusy=false;
                    freshSnapshots++;validatedLeaseUntil=lease;lastFreshClaimWall=now;nextClaimCheck=now+FollowRSmokeSequence.ClaimCheckSeconds;return true;
                }
                catch(Exception e){invalidSnapshots++;snapshotState="CURRENT_VALIDATION_FAILED";Stop("FOLLOW_R_CURRENT_VALIDATION_FAILED:"+e.Message);return false;}
            }
            private void FinishSequence()
            {if(sequence.Error!=null){Stop(null);return;}if(CheckClaim(Time.realtimeSinceStartupAsDouble,true))Stop(null);}
            public void Stop(string reason)
            {
                if(Terminal)return;input.Move=Vector2.zero;if(reason!=null)sequence?.Cancel(reason);
                cleanup=StreetSmokeSequence.Cleanup(!installed||Owns(),()=>{if(installed){player.StopMovement();player.ConfigureInterfaces(originalInput,originalAvatar);}},()=>{
                    if(attached){driver.EvaluationCompleted-=ObserveProducerClock;CinemachineCore.CameraUpdatedEvent.RemoveListener(Brain);Application.onBeforeRender-=NaturalFrame;EditorApplication.update-=Watch;StreetConfigs.Changed-=ConfigChanged;if(interest!=null)interest.Changed-=ConfigChanged;Application.logMessageReceived-=Log;attached=false;}});
                terminal=sequence==null?Pending():sequence.Report(cleanup,null);
                if(sequence==null){terminal.status="FAILED";terminal.error=reason??"STOPPED_BEFORE_NATURAL_BASELINE";startup.phase="finished";}
                terminal.startedAt=started;terminal.finishedAt=DateTimeOffset.UtcNow.ToString("o");if(reason!=null)terminal.error=reason;
                Enrich(terminal);PublishProbeEvidence(Path.Combine(Request.outputRoot,"run-report.json"),JsonUtility.ToJson(terminal,true));
            }
            private void Enrich(StreetProbeReport r)
            {r.requestSha256=RequestSha;r.startedAt=started;r.samplesSaved=saved;r.sources=Request.sources;r.capabilityVersion=FollowRSmokeSequence.Rule;r.unityVersion=Application.unityVersion;r.assemblyPath=typeof(LongLoopAutomatedProbe).Assembly.Location;r.assemblySha256=Hash(File.ReadAllBytes(r.assemblyPath));if(r.followR!=null){r.followR.callbackErrors=callbackErrors;r.followR.snapshotState=snapshotState;r.followR.snapshotFreshValidations=freshSnapshots;r.followR.snapshotBusyObservations=busySnapshots;r.followR.snapshotRecoveries=recoveredSnapshots;r.followR.snapshotValidationFailures=invalidSnapshots;r.followR.snapshotLastFreshWall=lastFreshClaimWall;r.followR.snapshotBusyDeadline=busyDeadline;r.followR.snapshotValidatedLeaseUntil=validatedLeaseUntil.ToString("o");r.followR.inputAtRegistration=initialInput?.Evidence;r.followR.inputLastObservation=lastInput?.Evidence;r.followR.inputAtStop=stoppedInput;}}
            public StreetProbeReport Report()
            {
                var result=terminal??(sequence==null?Pending():sequence.Report(cleanup,null));Enrich(result);if(Terminal)ApplyFollowRConsole(Request,result);return result;
            }
        }
        // Read only these necessary collider/Confiner geometry values in the existing ENGINE readback RPC.
        // This is not an object-memory restore snapshot, does not bake/invalidate, and never changes the camera.
        public static string InspectConfinerGeometrySha256(string cameraPath)
        {
            var cam=Resolve<CinemachineCamera>(cameraPath);
            return InspectConfinerGeometrySha256(cam);
        }
        private static string InspectConfinerGeometrySha256(CinemachineCamera cam)
        {
            var c=cam.GetComponents<CinemachineConfiner2D>().Single(e=>e.isActiveAndEnabled&&e.GetType()==typeof(CinemachineConfiner2D));
            var shape=c.BoundingShape2D;if(shape==null||!shape.isActiveAndEnabled)throw new InvalidOperationException("Current active Confiner geometry missing.");
            var text=new StringBuilder("StreetConfinerGeometry-v1|").Append(ObjectPath(cam.transform)).Append('|').Append(ObjectPath(shape.transform)).Append('|').Append(shape.GetType().FullName);
            void Number(float value){if(!LongLoopProbeChecks.Finite(value))throw new InvalidOperationException("Nonfinite Confiner geometry.");text.Append('|').Append(value.ToString("R",System.Globalization.CultureInfo.InvariantCulture));}
            for(int i=0;i<16;i++)Number(shape.transform.localToWorldMatrix[i]);Number(shape.offset.x);Number(shape.offset.y);
            void Path(Vector2[] points){if(points.Length<3)throw new InvalidOperationException("Confiner polygon is too small.");text.Append('|').Append(points.Length);foreach(var p in points){Number(p.x);Number(p.y);}}
            if(shape is BoxCollider2D box){if(box.size.x<=0||box.size.y<=0)throw new InvalidOperationException("Empty Confiner box.");Number(box.size.x);Number(box.size.y);}
            else if(shape is PolygonCollider2D polygon){text.Append('|').Append(polygon.pathCount);if(polygon.pathCount==0)throw new InvalidOperationException("Empty Confiner polygon.");for(int i=0;i<polygon.pathCount;i++)Path(polygon.GetPath(i));}
            else if(shape is CompositeCollider2D composite){if(composite.geometryType!=CompositeCollider2D.GeometryType.Polygons||composite.pathCount==0)throw new InvalidOperationException("Confiner composite must contain polygons.");text.Append('|').Append(composite.pathCount);for(int i=0;i<composite.pathCount;i++){var points=new Vector2[composite.GetPathPointCount(i)];composite.GetPath(i,points);Path(points);}}
            else throw new InvalidOperationException("Unsupported current Confiner geometry.");
            Number(c.Damping);Number(c.SlowingDistance);text.Append('|').Append(c.OversizeWindow.Enabled);Number(c.OversizeWindow.MaxWindowSize);Number(c.OversizeWindow.Padding);
            return Hash(Encoding.UTF8.GetBytes(text.ToString()));
        }
        public static StreetProbeReport Begin(string requestJson)
        {
            if(smoke!=null&&!smoke.Terminal)throw new InvalidOperationException("SMK-011 already owns input.");
            var request=JsonUtility.FromJson<StreetProbeRequest>(requestJson);
            LongLoopProbeChecks.ValidatePlan(request);
            var sha=Hash(Encoding.UTF8.GetBytes(requestJson));
            if(active!=null)
            {
                if(active.Request.runId==request.runId&&active.Request.taskId==request.taskId&&active.RequestSha==sha)return active.Report();
                if(!active.Terminal||!active.Report().cleanup.Passed||active.Request.executionId==request.executionId)
                    throw new InvalidOperationException("Registered same-execution/unfinished Street run exists; no reset or rerun.");
            }
            var configurationDecision=ValidateIdentity(request);
            if(Directory.Exists(request.outputRoot))throw new InvalidOperationException("Street run root already exists; preserve unfinished/finished evidence, use Read not Begin.");
            Directory.CreateDirectory(request.outputRoot);
            WriteNew(Path.Combine(request.outputRoot,"request.json"),requestJson);
            active=new Run(request,sha,configurationDecision);
            try{active.Start();}catch(Exception ex){active.Fail("BEGIN:"+ex);}
            return active.Report();
        }
        public static StreetProbeReport Read(string runId)
        {
            if(active!=null&&active.Request.runId==runId)return active.Report();
            throw new InvalidOperationException("No current matching Street run. Read saved evidence after reload; do not Begin again.");
        }
        public static string ReadJson(string runId)
        {
            var report=Read(runId);var path=Path.Combine(report.outputRoot,"run-report.json");
            return File.Exists(path)?File.ReadAllText(path,Encoding.UTF8):JsonUtility.ToJson(report,true);
        }
        public static StreetProbeReport Cancel(string runId)
        {if(active==null||active.Request.runId!=runId)throw new InvalidOperationException("Wrong Street run.");active.Fail("EXPLICIT_CANCEL");return active.Report();}
        public static StreetProbeReport AcknowledgeCapture(string runId,string captureId,string receiptSha256)
        {if(active==null||active.Request.runId!=runId)throw new InvalidOperationException("Wrong Street run.");return active.AcknowledgeCapture(captureId,receiptSha256);}
        private static void CancelActive(string reason){if(active!=null&&!active.Terminal)active.Fail(reason);}
        private static string Hash(byte[] data){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(data)).Replace("-","");}
        private static void WriteNew(string path,string text)
        {using(var s=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.Read))using(var w=new StreamWriter(s,new UTF8Encoding(false)))w.Write(text);}
        private static void PublishProbeEvidence(string path,string text)
        {
            NoLinks(path);var temporary=path+"."+Guid.NewGuid().ToString("N")+".pending";
            WriteNew(temporary,text);File.Move(temporary,path); // No overwrite; readers never see half a notification/report.
        }
        private static void NoLinks(string path)
        {for(var p=Path.GetFullPath(path);!string.IsNullOrEmpty(p);p=Path.GetDirectoryName(p))if((File.Exists(p)||Directory.Exists(p))&&(File.GetAttributes(p)&FileAttributes.ReparsePoint)!=0)throw new InvalidOperationException("Redirected Street evidence path.");}
        private static StreetConfigurationDecision ValidateIdentity(StreetProbeRequest r)
        {
            string project=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
            if(Application.unityVersion!="6000.3.24f1"||!EditorApplication.isPlaying||EditorApplication.isCompiling||EditorApplication.isUpdating||
                r.attempt<1||!System.Text.RegularExpressions.Regex.IsMatch(r.taskId??"","^AI-[0-9]{6}$")||
                !System.Text.RegularExpressions.Regex.IsMatch(r.executionId??"","^[0-9a-f]{32}$")||string.IsNullOrWhiteSpace(r.ownerThreadId)||
                !System.Text.RegularExpressions.Regex.IsMatch(r.runId??"","^[A-Za-z0-9][A-Za-z0-9_-]{0,63}$")||
                !string.Equals(Path.GetFullPath(r.projectPath),project,StringComparison.OrdinalIgnoreCase)||
                SceneManager.GetActiveScene().path!=r.scenePath||r.scenePath!="Assets/Scenes/LongLoop/LongLoop_Street.unity")
                throw new InvalidOperationException("Current normal Street Play and exact task/project/scene identity required.");
            using(var p=Process.GetCurrentProcess())
                if(r.editorPid!=p.Id||DateTimeOffset.Parse(r.editorStartedAt)!=new DateTimeOffset(p.StartTime))throw new InvalidOperationException("Editor PID/start mismatch.");
            foreach(var sha in new[]{r.contextSha256,r.scopeSha256,r.geometrySha256})
                if(!System.Text.RegularExpressions.Regex.IsMatch(sha??"","^[A-Fa-f0-9]{64}$"))throw new InvalidOperationException("Reviewed context/scope/geometry SHA required.");
            var expected=Path.Combine(project,".ai-workspace","outputs","PLAYABLE-DEMO011-20261104",r.taskId,"attempt"+r.attempt,"street-probe",r.runId);
            if(!Path.IsPathRooted(r.outputRoot)||!string.Equals(Path.GetFullPath(r.outputRoot),expected,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Exact task/attempt/run output root required.");
            NoLinks(r.outputRoot);
            var taskRoot=Path.GetDirectoryName(Path.GetDirectoryName(r.outputRoot));
            var authorization=Path.Combine(taskRoot,"ci-contexts",r.runId+".authorization.json");
            var geometry=Path.Combine(taskRoot,"ci-contexts",r.runId+".geometry.json");
            if(!string.Equals(Path.GetFullPath(r.authorizationPath),authorization,StringComparison.OrdinalIgnoreCase)||
                !string.Equals(Path.GetFullPath(r.geometryPath),geometry,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Sealed prior authorization/USERa geometry paths required.");
            NoLinks(authorization);NoLinks(geometry);
            if(Hash(File.ReadAllBytes(authorization))!=r.contextSha256||Hash(File.ReadAllBytes(geometry))!=r.geometrySha256)throw new InvalidOperationException("Prior authorization or USERa geometry SHA drift.");
            var auth=JsonUtility.FromJson<Authorization>(File.ReadAllText(authorization));
            if(auth.schemaVersion!=1||auth.taskId!=r.taskId||auth.attempt!=r.attempt||auth.executionId!=r.executionId||auth.runId!=r.runId||auth.ownerThreadId!=r.ownerThreadId||auth.scopeSha256!=r.scopeSha256||auth.geometrySha256!=r.geometrySha256)
                throw new InvalidOperationException("Prior reviewed authorization identity mismatch.");
            var header=StreetConfigurationAuthorization.Parse(File.ReadAllText(authorization));
            var mode=StreetConfigurationAuthorization.Text(header,"configurationAuthorizationMode");
            if(mode!=r.configurationAuthorizationMode)throw new InvalidOperationException("Request configuration source mode differs.");
            var reviewPath=StreetConfigurationAuthorization.Text(header,mode=="USER_ADJUSTED"?"userAdjustmentReceiptPath":"presetReviewPath");
            var reviewSha=StreetConfigurationAuthorization.Text(header,mode=="USER_ADJUSTED"?"userAdjustmentReceiptSha256":"presetReviewSha256");
            CheckReviewPath(reviewPath,project,taskRoot);
            if(Hash(File.ReadAllBytes(reviewPath))!=reviewSha)throw new InvalidOperationException("Configuration review bytes drift.");
            var reviewText=File.ReadAllText(reviewPath);var review=StreetConfigurationAuthorization.Parse(reviewText);
            var originalPath=StreetConfigurationAuthorization.OriginalPath(mode,review);CheckReviewPath(originalPath,project,taskRoot);
            if(Hash(File.ReadAllBytes(originalPath))!=StreetConfigurationAuthorization.OriginalSha(mode,review))throw new InvalidOperationException("Original human authorization bytes drift.");
            var currentMode=StreetConfigurationAuthorization.Parse(File.ReadAllText(Path.Combine(project,".ai-workspace","runtime","mode-state.json")));
            var controller=(string)currentMode["supervised"]?["controllerThreadId"];
            var decision=StreetConfigurationAuthorization.Validate(mode,reviewText,File.ReadAllText(originalPath),new StreetConfigurationIdentity {
                taskId=r.taskId,attempt=r.attempt,executionId=r.executionId,ownerThreadId=r.ownerThreadId,runId=r.runId,projectPath=r.projectPath,
                scopeSha256=r.scopeSha256,geometrySha256=r.geometrySha256,controllerThreadId=controller});
            if(StreetConfigurationAuthorization.Text(StreetConfigurationAuthorization.Parse(File.ReadAllText(geometry)),"cameraBoundsPath")!=r.cameraBoundsPath)
                throw new InvalidOperationException("Reviewed current Confiner collider path differs.");
            decision.reviewPath=reviewPath;decision.reviewSha256=reviewSha;return decision;
        }
        private static void CheckReviewPath(string path,string project,string taskRoot)
        {
            var p=Path.GetFullPath(path);var roots=new[]{Path.Combine(project,".ai-workspace","inputs"),Path.Combine(project,".ai-workspace","outputs","control"),taskRoot};
            if(!Path.IsPathRooted(path)||path.Split('\\','/').Any(s=>s=="."||s==".."||s.TrimEnd(' ','.')!=s)||
                !roots.Any(root=>p.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)))throw new InvalidOperationException("Configuration review source outside inputs/control/current task.");
            NoLinks(path);
        }
        [Serializable] private sealed class Authorization {public int schemaVersion,attempt;public string taskId,executionId,runId,ownerThreadId,scopeSha256,geometrySha256,userAdjustmentReceiptPath,userAdjustmentReceiptSha256;}
        [Serializable] private sealed class UserAdjustment {public int schemaVersion;public string kind,source,decision,geometrySha256,userReplyEvidencePath,userReplyEvidenceSha256;}
        private static T Resolve<T>(string path) where T:Component
        {
            var matches=Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None)
                .Where(c=>c.gameObject.scene==SceneManager.GetActiveScene()&&ObjectPath(c.transform)==path).ToArray();
            if(matches.Length!=1)throw new InvalidOperationException("Missing or ambiguous "+typeof(T).Name+" at "+path);
            return matches[0];
        }
        private static string ObjectPath(Transform t)
        {string p=t.name;for(var parent=t.parent;parent!=null;parent=parent.parent)p=parent.name+"/"+p;return p;}
        private static SerializedProperty Property(Object o,string name)
        {var p=new SerializedObject(o).FindProperty(name);if(p==null)throw new InvalidOperationException("Installed serialized field missing: "+o.GetType().FullName+"."+name);return p;}
        private static T Ref<T>(Object o,string name) where T:Object=>Property(o,name).objectReferenceValue as T;
        private static object RuntimeField(LongLoopPlayerController player,string name,Type type)
        {var f=typeof(LongLoopPlayerController).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.DeclaredOnly);if(f==null||f.FieldType!=type)throw new InvalidOperationException("Controller runtime interface contract changed.");return f.GetValue(player);}

        /// <summary>Current-call Street observation/dispatch. The sealed context is read and guarded afresh for every RPC.</summary>
        public static object ExecutePayload(string role,string contextPath,string contextSha256)
        {
            var context=StreetPayloadContract.ReadContext(role,contextPath,contextSha256,Path.GetFullPath(Path.Combine(Application.dataPath,"..")));
            return new StreetPayloadReader(context).Execute(role);
        }
        private sealed class StreetPayloadReader
        {
            readonly StreetPayloadContext c;
            string taskId=>c.taskId;string executionId=>c.executionId;string outputRoot=>c.outputRoot;string street=>c.scenePath;
            public StreetPayloadReader(StreetPayloadContext context){c=context;}
            void Gate(bool play=false)
            {
                using(var p=Process.GetCurrentProcess())Need(p.Id==c.editorPid&&new DateTimeOffset(p.StartTime)==StreetPayloadContract.Instant(c.editorStartedAt),"Same Editor PID/start");
                Need(StreetPayloadContract.SamePath(FullProject(),c.projectPath)&&Application.unityVersion=="6000.3.24f1"&&
                    (play?EditorApplication.isPlaying:!EditorApplication.isPlayingOrWillChangePlaymode)&&!EditorApplication.isCompiling&&!EditorApplication.isUpdating&&!EditorUtility.scriptCompilationFailed,"Same idle Editor");
                Need(SceneManager.sceneCount==1&&SceneManager.GetActiveScene()==Scene()&&(play||!Scene().isDirty),"Single clean author scene");
                Need(Ts().Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject))==0,"Missing0");
                Need(typeof(LongLoopAutomatedProbe).Module.ModuleVersionId.ToString()==c.loadedModuleVersionId,"Current loaded Street module changed");
            }
            public object Execute(string role)
            {
                switch(role){case "preflight":return preflight();case "savesBefore":return savesBefore();case "savesAfter":return savesAfter();case "ready":return ready();case "register":return register();case "read":return read();case "captureAck":return captureAck();case "postStop":return postStop();case "cancel":return cancel();default:throw new InvalidOperationException("Unknown Street role");}
            }
            void Need(bool v,string m){if(!v)throw new Exception(m);}
            Newtonsoft.Json.Linq.JObject Json(string p){return StreetPayloadContract.JsonFile(System.IO.Path.GetFullPath(p));}
            string Hash(string p)=>StreetPayloadContract.HashFile(System.IO.Path.GetFullPath(p));
            string P(Transform t){return t==null?null:t.parent==null?t.name:P(t.parent)+"/"+t.name;}
            float[] V(Vector3 v){return new[]{v.x,v.y,v.z};}
            Newtonsoft.Json.Linq.JToken Canon(Newtonsoft.Json.Linq.JToken v){return Newtonsoft.Json.Linq.JToken.Parse(v.ToString(Newtonsoft.Json.Formatting.None));}
            UnityEngine.SceneManagement.Scene Scene(){var s=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(street);Need(s.IsValid()&&s.isLoaded,"Street loaded");return s;}
            Transform[] hierarchy; Transform[] Ts()=>hierarchy??(hierarchy=Scene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray());
            GameObject Go(string n){return Ts().Single(x=>x.name==n).gameObject;}
            UnityEngine.Object R(UnityEngine.Object o,string n){var p=new SerializedObject(o).FindProperty(n);Need(p!=null,"Actual serialized field "+n);return p.objectReferenceValue;}
            Newtonsoft.Json.Linq.JObject ConfigValues(UnityEngine.Object o){var r=new Newtonsoft.Json.Linq.JObject();var p=new SerializedObject(o).GetIterator();while(p.NextVisible(true)){if(p.depth!=0||p.name.StartsWith("m_",StringComparison.Ordinal))continue;if(p.propertyType==SerializedPropertyType.Float)r[p.name]=p.floatValue;else if(p.propertyType==SerializedPropertyType.Boolean)r[p.name]=p.boolValue;else if(p.propertyType==SerializedPropertyType.Integer||p.propertyType==SerializedPropertyType.Enum)r[p.name]=p.intValue;}return r;}
            ScriptableObject[] configAssets;Newtonsoft.Json.Linq.JArray configCache;
            ScriptableObject[] ConfigAssets()=>configAssets??(configAssets=new[]{"DepthScaleConfig","CameraModeConfig","CameraFollowConfig","SceneBoundsConfig","CameraInterestConfig_TUNE"}.Select(n=>AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/Settings/Street/"+n+".asset")).ToArray());
            Newtonsoft.Json.Linq.JArray Configs(){if(configCache!=null)return configCache;return configCache=new Newtonsoft.Json.Linq.JArray(ConfigAssets().Select(a=>{Need(a!=null,"Existing SO");string path=AssetDatabase.GetAssetPath(a);return new Newtonsoft.Json.Linq.JObject{["path"]=path,["guid"]=AssetDatabase.AssetPathToGUID(path),["values"]=ConfigValues(a)};}));}
            Newtonsoft.Json.Linq.JObject Shape(PolygonCollider2D p){var local=new Newtonsoft.Json.Linq.JArray();var world=new Newtonsoft.Json.Linq.JArray();for(int i=0;i<p.pathCount;i++){var points=p.GetPath(i);local.Add(new Newtonsoft.Json.Linq.JArray(points.Select(v=>new Newtonsoft.Json.Linq.JArray(new[]{v.x,v.y}))));world.Add(new Newtonsoft.Json.Linq.JArray(points.Select(v=>new Newtonsoft.Json.Linq.JArray(V(p.transform.TransformPoint(new Vector3(v.x+p.offset.x,v.y+p.offset.y,0)))))));}return new Newtonsoft.Json.Linq.JObject{["path"]=P(p.transform),["instanceId"]=p.GetInstanceID(),["enabled"]=p.enabled,["trigger"]=p.isTrigger,["offset"]=new Newtonsoft.Json.Linq.JArray(new[]{p.offset.x,p.offset.y}),["matrix"]=new Newtonsoft.Json.Linq.JArray(Enumerable.Range(0,16).Select(i=>p.transform.localToWorldMatrix[i])),["localPaths"]=local,["worldPaths"]=world};}
            Newtonsoft.Json.Linq.JObject UserFields(){var player=Go("PlayerRoot_TUNE");var c=player.GetComponent<EatWhat.LongLoop.LongLoopPlayerController>();var visual=Go("Visual_TUNE");var eq=visual.GetComponent<CharacterEquipment>();var sa=visual.GetComponent<Spine.Unity.SkeletonAnimation>();Need(eq!=null&&sa!=null&&eq.baseSkin=="Potato_Non"&&eq.equipmentSlots.Single(x=>x.slotName=="Ear").skinName=="","Potato config");var cf=Go("CM_StreetFollow_TUNE").GetComponent<Unity.Cinemachine.CinemachineConfiner2D>();var cam=cf.GetComponent<Unity.Cinemachine.CinemachineCamera>();var composer=cf.GetComponent<Unity.Cinemachine.CinemachinePositionComposer>();var layout=new Newtonsoft.Json.Linq.JArray(Ts().Where(t=>P(t).StartsWith("Buildings",StringComparison.Ordinal)||new[]{"PlayerRoot_TUNE","Visual_TUNE","PlayerSpawn_TUNE","Boundary_L_TUNE","Boundary_R_TUNE","GroundLine_TUNE","Confiner_TUNE","WalkArea_TUNE","Foot_TUNE","FollowPoint"}.Contains(t.name)).Select(t=>new Newtonsoft.Json.Linq.JObject{["path"]=P(t),["position"]=new Newtonsoft.Json.Linq.JArray(V(t.localPosition)),["rotation"]=new Newtonsoft.Json.Linq.JArray(new[]{t.localRotation.x,t.localRotation.y,t.localRotation.z,t.localRotation.w}),["scale"]=new Newtonsoft.Json.Linq.JArray(V(t.localScale))}));return new Newtonsoft.Json.Linq.JObject{["layout"]=layout,["speed"]=c.MoveSpeed,["verticalFactor"]=new SerializedObject(c).FindProperty("verticalSpeedFactor_TUNE").floatValue,["walkAreaRef"]=P(((Component)R(c,"walkArea")).transform),["leftRef"]=P(((Component)R(c,"boundaryL_TUNE")).transform),["rightRef"]=P(((Component)R(c,"boundaryR_TUNE")).transform),["lens"]=JsonUtility.ToJson(cam.Lens),["distance"]=composer.CameraDistance,["confinerEnabled"]=cf.enabled,["boundsRef"]=P(cf.BoundingShape2D.transform),["damping"]=cf.Damping,["slowingDistance"]=cf.SlowingDistance,["oversize"]=JsonUtility.ToJson(cf.OversizeWindow),["baseSkin"]=eq.baseSkin,["slots"]=Newtonsoft.Json.Linq.JArray.FromObject(eq.equipmentSlots),["skeletonGuid"]=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(sa.skeletonDataAsset)),["configs"]=Configs()};}
            
            string FullProject(){return System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,".."));}
            object[] SaveFiles(){string dir=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.persistentDataPath,"saves"));Need(StreetPayloadContract.SamePath(dir,c.saveRoot),"Actual persistent save root");StreetPayloadContract.NoLinks(dir);var paths=System.IO.Directory.GetFiles(dir).OrderBy(x=>x,StringComparer.Ordinal).ToArray();Need(paths.Length==4,"Exactly four real saves");return paths.Select(p=>(object)new{path=p,exists=System.IO.File.Exists(p),length=new System.IO.FileInfo(p).Length,sha256=Hash(p)}).ToArray();}
            Newtonsoft.Json.Linq.JArray Bindings(){var a=new Newtonsoft.Json.Linq.JArray();foreach(var o in new UnityEngine.Object[]{Go("Managers").GetComponent<EatWhat.LongLoop.Configuration.StreetConfigBinder>(),Go("PlayerRoot_TUNE").GetComponent<EatWhat.LongLoop.LongLoopPlayerController>(),Go("Visual_TUNE").GetComponent<EatWhat.LongLoop.DepthScale>(),Go("PlayerRoot_TUNE").GetComponent<EatWhat.LongLoop.YSorter>(),Go("PlayerRoot_TUNE").GetComponent<EatWhat.LongLoop.StreetFollowPoint>(),Go("CM_StreetFollow_TUNE").GetComponent<EatWhat.LongLoop.CameraFollowConfigBinder>(),Go("CM_StreetFollow_TUNE").GetComponent<EatWhat.LongLoop.StreetFollowDriver>()}){Need(o!=null,"Required binding component");var so=new SerializedObject(o);var it=so.GetIterator();while(it.NextVisible(true)){if(it.depth!=0||it.name.StartsWith("m_",StringComparison.Ordinal)||it.propertyType!=SerializedPropertyType.ObjectReference)continue;var v=it.objectReferenceValue;a.Add(new Newtonsoft.Json.Linq.JObject{["owner"]=P(((Component)o).transform),["type"]=o.GetType().FullName,["field"]=it.name,["target"]=v is Component cp?P(cp.transform)+"|"+v.GetType().FullName:v is GameObject g?P(g.transform):v==null?null:AssetDatabase.GetAssetPath(v)});}}return a;}
            void Disk(){Need(Hash(street)==c.sceneSha256&&Hash(street+".meta")==c.metaSha256,"Fixed saved scene/meta");foreach(var a in ConfigAssets()){Need(a!=null&&!EditorUtility.IsDirty(a),"Saved config asset clean");}}
            object preflight(){Gate();Disk();var method=typeof(EatWhat.EditorValidation.LongLoopAutomatedProbe).GetMethod("Capabilities",System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static);Need(method!=null,"Loaded Domain lacks AI441 capture capability");var capabilities=Newtonsoft.Json.Linq.JObject.FromObject(method.Invoke(null,null));Need((string)capabilities["version"]=="AI441-sorting45-clock-ack-v1"&&(int)capabilities["sortingTimeoutSeconds"]==45&&(int)capabilities["otherTimeoutSeconds"]==12&&(int)capabilities["maximumSeconds"]==120&&(int)capabilities["sampleLimit"]==1200&&(int)capabilities["stages"]==22&&(int)capabilities["requiredAssertions"]==32,"Actual loaded capability contract");Need((string)capabilities["shortPayloadVersion"]==StreetPayloadContract.Version,"Loaded short API version");var user=UserFields();Need((float)user["speed"]==6.5f&&(float)user["verticalFactor"]==.6f,"Saved speed");string geometry=InspectConfinerGeometrySha256(Go("CM_StreetFollow_TUNE").GetComponent<Unity.Cinemachine.CinemachineCamera>());Need(geometry==c.confinerGeometrySha256,"Current approved geometry");var alpha=AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/Sprites/StreetView/LongLoop/Characters/Potato"}).Select(AssetDatabase.GUIDToAssetPath).Where(p=>p.EndsWith(".png",StringComparison.OrdinalIgnoreCase)).Select(p=>new{path=p,alphaIsTransparency=((TextureImporter)AssetImporter.GetAtPath(p)).alphaIsTransparency}).ToArray();Need(alpha.Length==11&&alpha.All(a=>!a.alphaIsTransparency),"Eleven existing Potato alpha=false");var assembly=typeof(EatWhat.EditorValidation.LongLoopAutomatedProbe).Assembly.Location;var r=new{taskId,attempt=c.attempt,executionId,eligible=true,sceneClean=true,missingScripts=0,projectPath=FullProject(),scenePath=Scene().path,editorPid=System.Diagnostics.Process.GetCurrentProcess().Id,editorStartedAt=System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime().ToString("o"),sceneSha256=Hash(street),metaSha256=Hash(street+".meta"),confinerGeometrySha256=geometry,userFields=user,bindings=Bindings(),alpha,saveFiles=SaveFiles(),persistentDataPath=Application.persistentDataPath,capabilities,loadedModuleVersionId=typeof(EatWhat.EditorValidation.LongLoopAutomatedProbe).Module.ModuleVersionId.ToString(),editorAssembly=new{path=assembly,sha256=Hash(assembly)},configs=Configs(),currentIsNull=EatWhat.LongLoop.Configuration.StreetConfigs.Current==null};return r;}
            object savesBefore(){Gate();Disk();return new{files=SaveFiles()};}
            object savesAfter(){Gate();Disk();return new{files=SaveFiles()};}
            object ready(){Gate(true);Disk();var player=Go("PlayerRoot_TUNE").GetComponent<EatWhat.LongLoop.LongLoopPlayerController>();var sa=Go("Visual_TUNE").GetComponent<Spine.Unity.SkeletonAnimation>();var camera=Go("CM_StreetFollow_TUNE").GetComponent<Unity.Cinemachine.CinemachineCamera>();var brain=Go("Cam_Street").GetComponent<Unity.Cinemachine.CinemachineBrain>();var b=camera.GetComponent<EatWhat.LongLoop.CameraFollowConfigBinder>();var point=player.GetComponent<EatWhat.LongLoop.StreetFollowPoint>();var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;var input=typeof(EatWhat.LongLoop.LongLoopPlayerController).GetField("input",flags).GetValue(player) as EatWhat.Toolkit.Input.IGameInput;var avatar=typeof(EatWhat.LongLoop.LongLoopPlayerController).GetField("avatar",flags).GetValue(player);var current=EatWhat.LongLoop.Configuration.StreetConfigs.Current;var owners=Ts().Select(t=>t.GetComponent<EatWhat.LongLoop.Configuration.StreetConfigBinder>()).Where(x=>x!=null&&x.isActiveAndEnabled).ToArray();bool ready=sa.AnimationState!=null&&avatar is EatWhat.LongLoop.TomatoSpineAvatar&&input!=null&&input.Horizontal==0&&input.Vertical==0&&current!=null&&current.DepthScale!=null&&current.CameraMode!=null&&current.CameraFollow!=null&&current.SceneBounds!=null&&owners.Length==1&&brain.ActiveVirtualCamera==camera&&!brain.IsBlending&&b.IsReady&&point.Node!=null&&camera.GetComponent<EatWhat.LongLoop.StreetFollowDriver>()!=null&&camera.Follow==camera.GetComponent<EatWhat.LongLoop.StreetFollowDriver>().CameraTarget;return new{ready,avatarInitialized=sa.AnimationState!=null,neutralInput=input!=null&&input.Horizontal==0&&input.Vertical==0,currentBound=current!=null,activeOwners=owners.Length,liveCamera=brain.ActiveVirtualCamera==camera,blend=brain.IsBlending,projectPath=FullProject(),editorPid=System.Diagnostics.Process.GetCurrentProcess().Id,editorStartedAt=System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime().ToString("o")};}
            object register(){Gate(true);return EatWhat.EditorValidation.LongLoopAutomatedProbe.Begin(System.IO.File.ReadAllText(outputRoot+"/ci-contexts/"+c.runId+".request.json"));}
            object read(){Gate(true);return EatWhat.EditorValidation.LongLoopAutomatedProbe.ReadJson(c.runId);}
            object captureAck(){Gate(true);return EatWhat.EditorValidation.LongLoopAutomatedProbe.AcknowledgeCapture(c.runId,"sorting",System.IO.File.ReadAllText(outputRoot+"/capture-receipt.sha256").Trim());}
            object postStop(){Gate();Disk();var pre=Json(outputRoot+"/preflight-final.json");var user=UserFields();var bindings=Bindings();Need(Newtonsoft.Json.Linq.JToken.DeepEquals(Canon(user),pre["userFields"])&&Newtonsoft.Json.Linq.JToken.DeepEquals(Canon(bindings),pre["bindings"]),"Actual author limited values and bindings restored");string geometry=InspectConfinerGeometrySha256(Go("CM_StreetFollow_TUNE").GetComponent<Unity.Cinemachine.CinemachineCamera>());Need(geometry==(string)pre["confinerGeometrySha256"],"Same postStop geometry");var report=Json(outputRoot+"/street-probe/"+c.runId+"/run-report.json");var cleanup=report["cleanup"];Need(new[]{"inputRestored","configsRestored","configOwnerRestored","callbacksRemoved","temporaryObjectsDestroyed","noForeignOverwrite"}.All(n=>(bool)cleanup[n]),"Original six cleanup flags");return new{isPlaying=EditorApplication.isPlaying,authorClean=!Scene().isDirty,scenePath=Scene().path,projectPath=FullProject(),editorPid=System.Diagnostics.Process.GetCurrentProcess().Id,editorStartedAt=System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime().ToString("o"),confinerGeometrySha256=geometry,sceneSha256=Hash(street),metaSha256=Hash(street+".meta"),missingScripts=0,ownedTemporaryInputs=0,ownedTemporaryCallbacks=0,ownedTemporaryObjects=0,cleanup,limitedUserValuesAndBindingsRestored=true,userFields=user,bindings};}
            object cancel(){Gate(true);EatWhat.EditorValidation.LongLoopAutomatedProbe.Cancel(c.runId);return EatWhat.EditorValidation.LongLoopAutomatedProbe.ReadJson(c.runId);}
        }
        private sealed class Proxy:IGameInput
        {
            public Vector2 Move;
            public float Horizontal=>Move.x;public float Vertical=>Move.y;public Vector2 LookAxis=>Vector2.zero;
            public GameInputDevice CurrentDevice=>GameInputDevice.Keyboard;public bool Interact=>false;public Vector2 MouseScreenPos=>Vector2.zero;
        }
        private sealed class Run
        {
            public readonly StreetProbeRequest Request;public readonly string RequestSha;
            private readonly StreetProbeSequence sequence;private readonly string startedAt;private readonly StreetConfigurationDecision configurationDecision;
            private CinemachineConfiner2D confiner;private string confinerGeometrySha;
            private readonly Proxy input=new Proxy();private readonly StreetProbeCleanup cleanup=new StreetProbeCleanup {noForeignOverwrite=true};
            private LongLoopPlayerController player;private DepthScale depth;private YSorter sorter;private StreetFollowPoint point;
            private CinemachineCamera camera;private CinemachineBrain brain;private CameraFollowConfigBinder binding;private StreetFollowDriver driver;
            private IGameInput originalInput;private IPlayerAvatar originalAvatar;private SkeletonAnimation skeleton;private int animationTrack;
            private string walkName,idleName;private Transform ground;private Vector3 baseline,localFoot;
            private StreetConfigSet original,owned;private StreetConfigBinder originalOwner;
            private CameraFollowConfig followClone;private DepthScaleConfig depthClone;
            private CameraModeConfig modeClone;private SceneBoundsConfig boundsClone;private CameraInterestConfig interestClone,originalInterest;
            private MonoBehaviour originalChannelState;private StreetCameraDebug originalDebug;
            private bool externalConfigChange,changingClones,channelCloneBound,inputInstalled,callbacks,cleaning;
            private GameObject markerRoot,front,back;private Texture2D markerTexture;private Sprite markerSprite;
            private YSorter frontSort,backSort;private Renderer selectedRenderer;private SortingGroup selectedGroup;
            private int expectedLayer,brainFrame=-1,sampledFrame=-1,bodyFrame=-1,notifications,previousChannelCount,enteredStage=-1;
            private double cleanupStarted;private float priorCmTime;private Vector3 priorRaw,priorGoal,bodyRaw,bodyCorrection,brainOutput,cameraOutput,stopGoal;
            private float bodySize,dampingError,aheadError,delayGoalError,recenterObserved,priorStopped,priorAhead;private Vector3 priorFoot;
            private bool previousBody,dampingMeasured,stopGoalSet;
            private ulong bodySequence;private StreetProbeReport terminal;
            private string captureReceiptSha,captureStarted,captureAcknowledged,captureStageDeadline,captureTotalDeadline,capturePendingSha;
            public bool Terminal=>terminal!=null;
            public Run(StreetProbeRequest r,string sha,StreetConfigurationDecision decision)
            {Request=r;RequestSha=sha;configurationDecision=decision;startedAt=DateTimeOffset.UtcNow.ToString("o");sequence=new StreetProbeSequence(r,Time.realtimeSinceStartupAsDouble);}
            public void Start()
            {
                player=Resolve<LongLoopPlayerController>(Request.playerPath);depth=Resolve<DepthScale>(Request.depthPath);sorter=Resolve<YSorter>(Request.sorterPath);
                point=Resolve<StreetFollowPoint>(Request.followPointPath);camera=Resolve<CinemachineCamera>(Request.cameraPath);brain=Resolve<CinemachineBrain>(Request.brainPath);
                driver=camera.GetComponent<StreetFollowDriver>();binding=camera.GetComponent<CameraFollowConfigBinder>();
                if(!player.isActiveAndEnabled||!depth.isActiveAndEnabled||!sorter.isActiveAndEnabled||!point.isActiveAndEnabled||driver==null||binding==null||
                    !driver.isActiveAndEnabled||!brain.isActiveAndEnabled||brain.ActiveVirtualCamera!=camera||brain.IsBlending||!camera.Lens.Orthographic||
                    camera.Follow!=driver.CameraTarget||driver.CameraTarget==null||driver.CameraTarget.parent!=null||point.Foot==null||point.Foot.IsChildOf(depth.transform)||binding.Camera!=camera||binding.Composer==null||
                    driver.FollowPoint!=point||driver.Binding!=binding)
                    throw new InvalidOperationException("Incomplete actual Street bindings/independent foot/live orthographic Brain.");
                if(camera.GetComponents<CinemachineComponentBase>().Any(c=>c.enabled&&c!=binding.Composer))throw new InvalidOperationException("Unknown camera pipeline driver.");
                if(Math.Abs(player.MoveSpeed-Request.expectedMoveSpeed)>.0001||Math.Abs(player.VerticalSpeedFactor-Request.expectedVerticalFactor)>.0001)
                    throw new InvalidOperationException("Reviewed actual speed/vertical factor differs; do not overwrite saved values.");
                originalInput=RuntimeField(player,"input",typeof(IGameInput)) as IGameInput;
                originalAvatar=RuntimeField(player,"avatar",typeof(IPlayerAvatar)) as IPlayerAvatar;
                if(originalInput==null||!(originalAvatar is TomatoSpineAvatar avatar)||!avatar.isActiveAndEnabled||originalInput.Horizontal!=0||originalInput.Vertical!=0)
                    throw new InvalidOperationException("Actual neutral input and real initialized avatar required.");
                skeleton=Ref<SkeletonAnimation>(avatar,"skeletonAnimation");animationTrack=Property(avatar,"trackIndex").intValue;
                walkName=Property(avatar,"walkAnimation").stringValue;idleName=Property(avatar,"idleAnimation").stringValue;
                if(skeleton==null||skeleton.AnimationState==null)throw new InvalidOperationException("Wait for natural avatar initialization; no manual Start/Awake.");
                original=StreetConfigs.Current;
                if(original==null||original.DepthScale==null||original.CameraMode==null||original.CameraFollow==null||original.SceneBounds==null)
                    throw new InvalidOperationException("All four actual CFG assets must be bound.");
                var extensions=camera.GetComponents<CinemachineExtension>();
                StreetProbeDriverPolicy.Validate(extensions.Select(e=>new StreetProbeDriver {
                    kind=e==driver?"StreetFollowDriver":e.GetType()==typeof(CinemachineConfiner2D)?"CinemachineConfiner2D":"UNKNOWN",
                    enabled=e.enabled,sameCamera=e.gameObject==camera.gameObject&&e.ComponentOwner==camera,
                    sameBounds=e is CinemachineConfiner2D c&&c.BoundingShape2D==original.CameraBounds&&original.CameraBounds!=null&&
                        original.CameraBounds.isActiveAndEnabled&&ObjectPath(original.CameraBounds.transform)==Request.cameraBoundsPath}).ToArray());
                confiner=extensions.OfType<CinemachineConfiner2D>().Single(e=>e.enabled);
                confinerGeometrySha=InspectConfinerGeometrySha256(Request.cameraPath);
                if(StreetConfigurationAuthorization.Text(StreetConfigurationAuthorization.Parse(File.ReadAllText(Request.geometryPath)),"confinerGeometrySha256")!=confinerGeometrySha)
                    throw new InvalidOperationException("Current collider/Confiner geometry differs from the reviewed geometry.");
                if(!(original.CameraBounds is PolygonCollider2D)&&!(original.CameraBounds is BoxCollider2D)&&!(original.CameraBounds is CompositeCollider2D))
                    throw new InvalidOperationException("Installed Confiner does not support this current collider shape.");
                float settle=original.CameraFollow.RecenterDelay+6*Math.Max(original.CameraFollow.DampingX,original.CameraFollow.DampingY)+
                    camera.Lens.OrthographicSize*Math.Max(camera.Lens.Aspect*original.CameraFollow.DeadZoneWidth,original.CameraFollow.DeadZoneHeight)/Math.Max(.01f,original.CameraFollow.RecenterSpeed);
                foreach(var id in new[]{"camera-settle","stopped-recenter"})if(Request.stages.First(s=>s.id==id).seconds<settle)
                    throw new InvalidOperationException("Insufficient actual configured settling/recenter time: "+id);
                if(Request.stages.First(s=>s.id=="edge-stop").seconds<original.CameraFollow.RecenterDelay+.25f)
                    throw new InvalidOperationException("Boundary stop must cover actual delay.");
                originalInterest=Ref<CameraInterestConfig>(driver,"interestConfig");originalChannelState=Ref<MonoBehaviour>(driver,"gameState");originalDebug=Ref<StreetCameraDebug>(driver,"debugger");
                if(originalInterest==null||originalChannelState!=null)throw new InvalidOperationException("Local C7 required; unknown external channel state must be reviewed.");
                VerifyFiles();
                var assets=new Object[]{original.DepthScale,original.CameraMode,original.CameraFollow,original.SceneBounds,originalInterest};
                if(Request.configuredAssets==null||Request.configuredAssets.Length!=5||assets.Any(a=>!EditorUtility.IsPersistent(a)||EditorUtility.IsDirty(a)))
                    throw new InvalidOperationException("Five real saved configuration assets required; do not save dirty user assets.");
                foreach(var a in assets)if(!Request.configuredAssets.Any(f=>string.Equals(Path.GetFullPath(f.path),Path.GetFullPath(Path.Combine(Request.projectPath,AssetDatabase.GetAssetPath(a))),StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException("Configured asset identity missing from reviewed snapshot.");
                var owners=Object.FindObjectsByType<StreetConfigBinder>(FindObjectsInactive.Exclude,FindObjectsSortMode.None).Where(b=>b.isActiveAndEnabled&&b.gameObject.scene==SceneManager.GetActiveScene()&&OwnerReferencesMatch(b)).ToArray();
                if(owners.Length!=1)throw new InvalidOperationException("Unique existing original StreetConfigBinder owner required.");originalOwner=owners[0];
                var area=Ref<WalkArea>(player,"walkArea");if(area==null||!area.IsUsable)throw new InvalidOperationException("USERa actual WalkArea required.");
                foreach(var s in Request.stages.Where(s=>s.navigate))if(!area.Contains(new Vector2(s.destination.x,s.destination.y)))throw new InvalidOperationException("Unreachable approved sample point outside actual WalkArea: "+s.id);
                var ys=Request.stages.Where(s=>s.id.StartsWith("depth-",StringComparison.Ordinal)).Select(s=>s.destination.y).OrderBy(y=>y).ToArray();
                if(ys.Length!=3||ys[1]-ys[0]<.2||ys[2]-ys[1]<.2)throw new InvalidOperationException("Three distinct actual depth positions required.");
                ground=Ref<Transform>(depth,"groundLine")??original.GroundLine;localFoot=Property(depth,"localFootOffset").vector3Value;
                if(ground==null||Math.Abs(depth.AppliedFactor)<.0001||depth.Status!=DepthScaleStatus.Applied||Ref<MonoBehaviour>(depth,"depthOverrideProvider")!=null)
                    throw new InvalidOperationException("Known actual ground/coefficient and nonzero baseline required.");
                baseline=depth.transform.localScale/depth.AppliedFactor;
                selectedGroup=Ref<SortingGroup>(sorter,"targetSortingGroup");selectedRenderer=Ref<Renderer>(sorter,"targetRenderer");
                if(selectedGroup==null&&selectedRenderer==null||Ref<Transform>(sorter,"foot")!=point.Foot||sorter.BakeOnStart||sorter.Precision<=0)
                    throw new InvalidOperationException("Actual dynamic precise sorting target/independent foot required.");
                expectedLayer=selectedGroup!=null?selectedGroup.sortingLayerID:selectedRenderer.sortingLayerID;
                followClone=Object.Instantiate(original.CameraFollow);depthClone=Object.Instantiate(original.DepthScale);
                modeClone=Object.Instantiate(original.CameraMode);boundsClone=Object.Instantiate(original.SceneBounds);interestClone=Object.Instantiate(originalInterest);
                foreach(var a in new Object[]{followClone,depthClone,modeClone,boundsClone,interestClone})a.hideFlags=HideFlags.DontSave;
                foreach(var a in CloneAssets())a.Changed+=OnCloneChanged;
                foreach(var a in OriginalAssets())a.Changed+=OnOriginalChanged;
                StreetConfigs.Bind(depthClone,modeClone,followClone,boundsClone,original.GroundLine,original.CameraBounds);owned=StreetConfigs.Current;
                player.ConfigureInterfaces(input,originalAvatar);inputInstalled=true;
                CreateMarkers();
                previousChannelCount=driver.AppliedEvaluations;
                driver.EvaluationCompleted+=ObserveBody;CinemachineCore.CameraUpdatedEvent.AddListener(ObserveBrain);
                Application.onBeforeRender+=ObserveAfterLateUpdate;EditorApplication.update+=Watch;
                callbacks=true;EnterStage();
                WriteNew(Path.Combine(Request.outputRoot,"registered.json"),JsonUtility.ToJson(Report(),true));
            }
            private void VerifyFiles()
            {
                foreach(var f in (Request.sources??Array.Empty<StreetProbeFile>()).Concat(Request.configuredAssets??Array.Empty<StreetProbeFile>()))
                {
                    var p=Path.GetFullPath(f.path);if(!p.StartsWith(Path.GetFullPath(Request.projectPath)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Source/config path outside project.");
                    NoLinks(p);if(!File.Exists(p)||!string.Equals(Hash(File.ReadAllBytes(p)),f.sha256,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Source/config SHA drift: "+p);
                }
                if(Request.sources==null||Request.sources.Length<2||!Request.sources.Any(f=>Path.GetFileName(f.path)=="LongLoopAutomatedProbe.cs")||!Request.sources.Any(f=>Path.GetFileName(f.path)=="LongLoopProbeChecks.cs"))
                    throw new InvalidOperationException("Actual compiled probe source bindings required.");
            }
            private void CreateMarkers()
            {
                markerRoot=new GameObject("StreetProbe_"+Request.runId);markerRoot.hideFlags=HideFlags.DontSave;
                markerTexture=new Texture2D(1,1);markerTexture.SetPixel(0,0,Color.white);markerTexture.Apply();markerTexture.hideFlags=HideFlags.DontSave;
                markerSprite=Sprite.Create(markerTexture,new Rect(0,0,1,1),new Vector2(.5f,.5f),1);markerSprite.hideFlags=HideFlags.DontSave;
                var center=Request.stages.First(s=>s.id=="depth-mid").destination;
                front=Marker("Front_numeric_not_visual_acceptance",center+Vector3.down*.2f,Color.magenta,out frontSort);
                back=Marker("Back_numeric_not_visual_acceptance",center+Vector3.up*.2f,Color.cyan,out backSort);
            }
            private GameObject Marker(string name,Vector3 pos,Color color,out YSorter sort)
            {
                var go=new GameObject(name);go.transform.SetParent(markerRoot.transform,false);go.transform.position=pos;
                var renderer=go.AddComponent<SpriteRenderer>();renderer.sprite=markerSprite;renderer.color=color;renderer.sortingLayerID=expectedLayer;
                sort=go.AddComponent<YSorter>();sort.BaseOrder=sorter.BaseOrder;sort.Precision=sorter.Precision;sort.DepthOffset=sorter.DepthOffset;
                sort.ConfigureTargets(renderer,null,go.transform);return go; // Unity runs its real Start/LateUpdate, never manually Refresh.
            }
            private bool Owned()=>ReferenceEquals(RuntimeField(player,"input",typeof(IGameInput)),input)&&ReferenceEquals(RuntimeField(player,"avatar",typeof(IPlayerAvatar)),originalAvatar)&&
                ReferenceEquals(StreetConfigs.Current,owned)&&!externalConfigChange&&
                Ref<CameraInterestConfig>(driver,"interestConfig")== (channelCloneBound?interestClone:originalInterest)&&
                confiner!=null&&confiner.isActiveAndEnabled&&confiner.ComponentOwner==camera&&confiner.BoundingShape2D==original.CameraBounds;
            private StreetConfigAsset[] CloneAssets()=>new StreetConfigAsset[]{followClone,depthClone,modeClone,boundsClone,interestClone};
            private StreetConfigAsset[] OriginalAssets()=>original==null?Array.Empty<StreetConfigAsset>():new StreetConfigAsset[]{original.DepthScale,original.CameraMode,original.CameraFollow,original.SceneBounds,originalInterest};
            private void OnCloneChanged(){if(!changingClones)externalConfigChange=true;}
            private void OnOriginalChanged(){externalConfigChange=true;}
            private bool OwnerReferencesMatch(StreetConfigBinder owner)=>Ref<DepthScaleConfig>(owner,"depthScaleConfig")==original.DepthScale&&Ref<CameraModeConfig>(owner,"cameraModeConfig")==original.CameraMode&&Ref<CameraFollowConfig>(owner,"cameraFollowConfig")==original.CameraFollow&&Ref<SceneBoundsConfig>(owner,"sceneBoundsConfig")==original.SceneBounds&&Ref<Transform>(owner,"groundLine")==original.GroundLine&&Ref<Collider2D>(owner,"cameraBounds")==original.CameraBounds;
            private void EnterStage()
            {
                if(sequence.Finished)return;
                if(!Owned())throw new InvalidOperationException("Foreign input/config ownership before stage transition.");
                enteredStage=sequence.StageIndex;string id=Request.stages[enteredStage].id;
                changingClones=true;
                try
                {
                    followClone.Lookahead=(id=="deadzone-hold"||id=="deadzone-push")?0:original.CameraFollow.Lookahead;
                    depthClone.Focus=original.DepthScale.Focus+(id=="focus"?Request.focusDelta:0);
                    interestClone.SafeZoneWidth=id=="f02-zero"?0:originalInterest.SafeZoneWidth;
                }
                finally{changingClones=false;}
                if(id=="f02-zero"&&!channelCloneBound){driver.Configure(binding,point,driver.CameraTarget,interestClone,originalChannelState,originalDebug);channelCloneBound=true;}
                previousBody=false;stopGoalSet=false;
            }
            // Observation adapter only. Old camera motion equations are explicitly
            // not evaluated against FOLLOW-R; the checker marks those IDs N/A.
            private void ObserveBody(StreetFollowDriver source,CinemachineVirtualCameraBase owner,ulong serial,CameraState state,float dt)
            {
                if(cleaning||Terminal||source!=driver||owner!=camera)return;
                try
                {
                    dampingMeasured=false;dampingError=aheadError=recenterObserved=delayGoalError=0;
                    priorRaw=bodyRaw=state.RawPosition;priorGoal=driver.PipelineState.Goal;priorCmTime=CinemachineCore.CurrentTime;previousBody=true;
                    bodyCorrection=state.PositionCorrection;bodySize=state.Lens.OrthographicSize;bodySequence=serial;bodyFrame=Time.frameCount;notifications++;
                    priorFoot=point.Foot.position;priorAhead=(float)(driver.PipelineState.Lookahead.SignedWeight*Math.Min(followClone.Lookahead,followClone.MaxHorizontalOffset));priorStopped=(float)driver.PipelineState.StoppedSeconds;
                }
                catch(Exception ex){Fail("TARGET_OBSERVER:"+ex);}
            }
            private void ObserveBrain(CinemachineBrain source)
            {
                if(cleaning||Terminal||source!=brain)return;
                brainFrame=Time.frameCount;brainOutput=brain.State.GetFinalPosition();cameraOutput=camera.State.GetFinalPosition();
            }
            private void ObserveAfterLateUpdate()
            {
                if(cleaning||Terminal||sampledFrame==Time.frameCount||brainFrame!=Time.frameCount)return;
                sampledFrame=Time.frameCount;
                try
                {
                    var observation=Sample();var next=sequence.Tick(observation,Time.realtimeSinceStartupAsDouble,Persist);
                    if(sequence.PendingCapture!=null&&captureStarted==null)
                    {
                        double now=Time.realtimeSinceStartupAsDouble;var utc=DateTimeOffset.UtcNow;captureStarted=utc.ToString("o");
                        captureStageDeadline=utc.AddSeconds(sequence.StageSecondsRemaining(now)).ToString("o");
                        captureTotalDeadline=utc.AddSeconds(Request.maximumSeconds-(now-sequence.StartedAt)).ToString("o");
                        var notification=new StreetProbeCapturePending {taskId=Request.taskId,attempt=Request.attempt,executionId=Request.executionId,ownerThreadId=Request.ownerThreadId,
                            runId=Request.runId,projectPath=Request.projectPath,scenePath=Request.scenePath,editorPid=Request.editorPid,editorStartedAt=Request.editorStartedAt,
                            frame=Time.frameCount,phaseIndex=sequence.StageIndex,elapsed=now-sequence.StartedAt,stageStartedElapsed=sequence.StageStartedAt-sequence.StartedAt,
                            createdAt=captureStarted,stageDeadlineAt=captureStageDeadline,totalDeadlineAt=captureTotalDeadline};
                        string text=JsonUtility.ToJson(notification,true);capturePendingSha=Hash(Encoding.UTF8.GetBytes(text));
                        PublishProbeEvidence(Path.Combine(Request.outputRoot,"capture-pending.json"),text);
                    }
                    previousChannelCount=driver.AppliedEvaluations;notifications=0;
                    if(sequence.Finished){BeginCleanup();return;}
                    if(enteredStage!=sequence.StageIndex)EnterStage();
                    input.Move=next;
                }
                catch(Exception ex){Fail("NATURAL_FRAME:"+ex);}
            }
            private StreetProbeObservation Sample()
            {
                var area=Ref<WalkArea>(player,"walkArea");var actualConfig=Ref<DepthScaleConfig>(depth,"localConfig")??StreetConfigs.Current.DepthScale;
                float coefficient=depth.UseOverrideFactor?depth.OverrideFactor:actualConfig.DepthScaleFactor;
                var track=skeleton.AnimationState.GetCurrent(animationTrack);var name=track?.Animation?.Name;
                bool sorterReady=sorter.Status==YSorterStatus.Applied||sorter.Status==YSorterStatus.Saturated;
                var intent=new Vector2(player.HorizontalInput,player.VerticalInput*player.VerticalSpeedFactor);
                float along=intent.x*Request.edgeTangent.x+intent.y*Request.edgeTangent.y;
                var normal=new Vector2(intent.x-Request.edgeTangent.x*along,intent.y-Request.edgeTangent.y*along);
                bool boundaryBlocked=normal.sqrMagnitude>.0001f&&!area.Contains(new Vector2(point.Foot.position.x,point.Foot.position.y)+normal.normalized*.05f);
                var s=new StreetProbeObservation {
                    frame=Time.frameCount,gameTime=Time.timeAsDouble,deltaTime=Time.deltaTime,fixedDeltaTime=Time.fixedDeltaTime,
                    ready=depth.Status==DepthScaleStatus.Applied&&sorterReady&&point.Status==StreetFollowPointStatus.Ready&&binding.IsReady&&brain.ActiveVirtualCamera==camera&&!brain.IsBlending,
                    inputOwned=ReferenceEquals(RuntimeField(player,"input",typeof(IGameInput)),input),configsOwned=Owned(),
                    foot=point.Foot.position,derivedFoot=depth.transform.TransformPoint(localFoot),visualScale=depth.transform.localScale,baselineScale=baseline,
                    speed=player.MoveSpeed,verticalFactor=player.VerticalSpeedFactor,coefficient=coefficient,groundY=ground.position.y,factor=depth.AppliedFactor,inputX=player.HorizontalInput,inputY=player.VerticalInput,
                    focus=StreetConfigs.Current.DepthScale.Focus,originalFocus=original.DepthScale.Focus,followPoint=point.Node.position,
                    facingRight=player.IsFacingRight,moving=player.IsMoving,animationWalk=name==walkName,animationIdle=name==idleName,
                    inside=area.Contains(new Vector2(point.Foot.position.x,point.Foot.position.y)),boundaryBlocked=boundaryBlocked,movementPath=Ref<Rigidbody2D>(player,"targetRigidbody")!=null?"Rigidbody2D":"Transform",
                    layer=selectedGroup!=null?selectedGroup.sortingLayerID:selectedRenderer.sortingLayerID,expectedLayer=expectedLayer,
                    order=selectedGroup!=null?selectedGroup.sortingOrder:selectedRenderer.sortingOrder,baseOrder=sorter.BaseOrder,precision=sorter.Precision,depthOffset=sorter.DepthOffset,
                    sorterTargetMatches=Ref<SortingGroup>(sorter,"targetSortingGroup")==selectedGroup&&Ref<Renderer>(sorter,"targetRenderer")==selectedRenderer,
                    markersReady=frontSort.Status==YSorterStatus.Applied&&backSort.Status==YSorterStatus.Applied,
                    frontOrder=frontSort.AppliedOrder,backOrder=backSort.AppliedOrder,camera=camera.State.RawPosition,goal=driver.PipelineState.Goal,
                    ahead=(float)(driver.PipelineState.Lookahead.SignedWeight*Math.Min(followClone.Lookahead,followClone.MaxHorizontalOffset)),stopped=(float)driver.PipelineState.StoppedSeconds,delay=followClone.RecenterDelay,recenterSpeed=followClone.RecenterSpeed,
                    lookahead=followClone.Lookahead,lookaheadTime=followClone.LookaheadTime,cap=followClone.MaxHorizontalOffset,
                    size=camera.State.Lens.OrthographicSize,f01Size=bodySize,aspect=camera.State.Lens.Aspect,dampingError=dampingError,aheadError=aheadError,dampingMeasured=dampingMeasured,
                    delayGoalError=delayGoalError,recenterSpeedObserved=recenterObserved,sequence=driver.EvaluationSequence,bodySequence=bodySequence,
                    channelAdvances=driver.AppliedEvaluations-previousChannelCount,bodyNotifications=notifications,
                    followRTargetPipeline=true,target=driver.LastResult.Target,signedLookaheadWeight=(float)driver.PipelineState.Lookahead.SignedWeight,targetStatus=driver.Status.ToString(),
                    channelValid=driver.LastResult.Valid,channelZero=false,
                    f01PositionCorrection=bodyCorrection,finalPositionCorrection=camera.State.PositionCorrection,
                    f02Contribution=Vector3.zero,
                    confinerRequestedDisplacement=confiner.GetCameraDisplacementDistance(camera),
                    confinerClearForCameraTests=camera.State.PositionCorrection.magnitude<.003f&&confiner.GetCameraDisplacementDistance(camera)<.003f,
                    baselineOutside=driver.LastResult.Channels.BaselineOutside,observerSameFrame=bodyFrame==Time.frameCount,
                    brainOutput=brainOutput,cameraOutput=cameraOutput };
                return s;
            }
            private void Persist(StreetProbeObservation sample)
            {
                var path=Path.Combine(Request.outputRoot,"samples.jsonl");NoLinks(path);
                using(var s=new FileStream(path,File.Exists(path)?FileMode.Append:FileMode.CreateNew,FileAccess.Write,FileShare.Read))
                using(var w=new StreamWriter(s,new UTF8Encoding(false)))w.WriteLine(JsonUtility.ToJson(sample)); // Saved before fatal assertion.
            }
            private void Watch()
            {
                if(Terminal)return;
                if(cleaning){FinishCleanupIfReady();return;}
                if(!EditorApplication.isPlaying||EditorApplication.isCompiling){Fail("PLAY_EXIT_OR_RELOAD");return;}
                sequence.CheckDeadline(Time.realtimeSinceStartupAsDouble);
                if(sequence.Finished)BeginCleanup();
            }
            public void Fail(string error)
            {
                if(Terminal)return;sequence.Cancel(error);
                try{WriteNew(Path.Combine(Request.outputRoot,"failure-"+Guid.NewGuid().ToString("N")+".json"),JsonUtility.ToJson(Report(),true));}catch(Exception log){UnityEngine.Debug.LogError("Street failure evidence write failed: "+log);}
                BeginCleanup();
            }
            public StreetProbeReport AcknowledgeCapture(string id,string receiptSha)
            {
                if(!System.Text.RegularExpressions.Regex.IsMatch(receiptSha??"","^[A-Fa-f0-9]{64}$"))throw new InvalidOperationException("Actual official capture wrapper receipt SHA required; CI validates it, acknowledgement alone is not image acceptance.");
                sequence.AcknowledgeCapture(id,Time.realtimeSinceStartupAsDouble);captureReceiptSha=receiptSha;captureAcknowledged=DateTimeOffset.UtcNow.ToString("o");
                WriteNew(Path.Combine(Request.outputRoot,"capture-ack.json"),JsonUtility.ToJson(Report(),true));return Report();
            }
            private void BeginCleanup()
            {
                if(cleaning||Terminal)return;cleaning=true;cleanupStarted=Time.realtimeSinceStartupAsDouble;input.Move=Vector2.zero;
                try
                {
                    if(inputInstalled&&player!=null)
                    {
                        if(ReferenceEquals(RuntimeField(player,"input",typeof(IGameInput)),input)&&ReferenceEquals(RuntimeField(player,"avatar",typeof(IPlayerAvatar)),originalAvatar))
                        {player.StopMovement();player.ConfigureInterfaces(originalInput,originalAvatar);cleanup.inputRestored=true;}
                        else {cleanup.noForeignOverwrite=false;cleanup.error="Foreign input/avatar ownership; not overwritten.";}
                    }
                    else cleanup.inputRestored=!inputInstalled;
                    bool currentOwned=owned!=null&&ReferenceEquals(StreetConfigs.Current,owned)&&!externalConfigChange;
                    if(currentOwned&&originalOwner!=null&&originalOwner.isActiveAndEnabled&&OwnerReferencesMatch(originalOwner))
                    {
                        // Real public lifecycle restores the existing owner's activeSet as well as references.
                        // Calling Bind alone creates a different container and would orphan that owner's exit cleanup.
                        originalOwner.enabled=false;originalOwner.enabled=true;
                        cleanup.configOwnerRestored=StreetConfigs.Current!=null&&ReferenceEquals(StreetConfigs.Current.DepthScale,original.DepthScale)&&ReferenceEquals(StreetConfigs.Current.CameraFollow,original.CameraFollow);
                    }
                    else if(owned!=null){cleanup.noForeignOverwrite=false;cleanup.error="Foreign Current binding; not overwritten.";}
                    bool localOwned=!channelCloneBound||driver!=null&&Ref<CameraInterestConfig>(driver,"interestConfig")==interestClone;
                    if(channelCloneBound&&localOwned)driver.Configure(binding,point,driver.CameraTarget,originalInterest,originalChannelState,originalDebug);
                    if(!localOwned){cleanup.noForeignOverwrite=false;cleanup.error="Foreign local C7 binding; not overwritten.";}
                    cleanup.configsRestored=(owned==null||currentOwned&&cleanup.configOwnerRestored)&&localOwned;
                    if(owned==null)cleanup.configOwnerRestored=true;
                }
                catch(Exception ex){cleanup.error=ex.ToString();}
                finally
                {
                    if(driver!=null)driver.EvaluationCompleted-=ObserveBody;
                    foreach(var a in CloneAssets())if(a!=null)a.Changed-=OnCloneChanged;
                    foreach(var a in OriginalAssets())if(a!=null)a.Changed-=OnOriginalChanged;
                    CinemachineCore.CameraUpdatedEvent.RemoveListener(ObserveBrain);Application.onBeforeRender-=ObserveAfterLateUpdate;
                    callbacks=false;cleanup.callbacksRemoved=true;
                    if(markerRoot!=null&&(markerRoot.transform.childCount!=2||front==null||back==null||front.transform.parent!=markerRoot.transform||back.transform.parent!=markerRoot.transform))
                    {cleanup.noForeignOverwrite=false;cleanup.error="Foreign marker hierarchy; retained.";}
                    else DestroyOwned(markerRoot);
                    DestroyOwned(markerSprite);DestroyOwned(markerTexture);
                    if(!externalConfigChange&&cleanup.configsRestored)foreach(var o in CloneAssets())DestroyOwned(o);
                    // Editor watch remains only until delayed own destruction is observed or two seconds elapse.
                    EditorApplication.update-=Watch;EditorApplication.update+=Watch;
                    FinishCleanupIfReady();
                }
            }
            private static void DestroyOwned(Object o){if(o==null)return;if(EditorApplication.isPlaying)Object.Destroy(o);else Object.DestroyImmediate(o);}
            private void FinishCleanupIfReady()
            {
                bool destroyed=markerRoot==null&&markerSprite==null&&markerTexture==null&&followClone==null&&depthClone==null&&modeClone==null&&boundsClone==null&&interestClone==null;
                if(!destroyed&&Time.realtimeSinceStartupAsDouble-cleanupStarted<2)return;
                cleanup.temporaryObjectsDestroyed=destroyed;EditorApplication.update-=Watch;
                try{VerifyFiles();}catch(Exception ex){cleanup.error="Final source/config drift: "+ex;}
                terminal=Report();terminal.finishedAt=DateTimeOffset.UtcNow.ToString("o");terminal.status=sequence.Error!=null?"FAILED":cleanup.Passed&&terminal.checks.All(c=>c.passed)?"SUCCEEDED":"FAILED";
                terminal.passed=terminal.status=="SUCCEEDED";terminal.error=sequence.Error??(terminal.passed?null:"ASSERTION_OR_CLEANUP_FAILED");
                PublishProbeEvidence(Path.Combine(Request.outputRoot,"run-report.json"),JsonUtility.ToJson(terminal,true));
            }
            public StreetProbeReport Report()
            {
                if(terminal!=null)return terminal;
                var ledger=Path.Combine(Request.outputRoot,"samples.jsonl");
                var result=LongLoopProbeChecks.BuildReport(Request,sequence.Samples,cleanup);
                result.requestSha256=RequestSha;result.startedAt=startedAt;result.status=cleaning?"CLEANING":"RUNNING";result.error=sequence.Error;
                result.unityVersion=Application.unityVersion;result.assemblyPath=typeof(LongLoopAutomatedProbe).Assembly.Location;result.assemblySha256=Hash(File.ReadAllBytes(result.assemblyPath));
                result.configurationAuthorizationMode=configurationDecision.mode;result.configurationReviewPath=configurationDecision.reviewPath;
                result.configurationReviewSha256=configurationDecision.reviewSha256;result.humanTuningStatus=configurationDecision.humanTuningStatus;
                result.humanTuningCompleted=configurationDecision.humanTuningCompleted;result.finalVisualAccepted=false;
                result.cameraBoundsPath=Request.cameraBoundsPath;result.confinerTypeName=confiner==null?null:confiner.GetType().FullName;
                result.confinerGeometrySha256=confinerGeometrySha;
                result.confinerInstanceId=confiner==null?0:confiner.GetInstanceID();result.confinerBoundsInstanceId=confiner==null||confiner.BoundingShape2D==null?0:confiner.BoundingShape2D.GetInstanceID();
                result.sampleLedgerPath=ledger;result.sampleLedgerSha256=File.Exists(ledger)?Hash(File.ReadAllBytes(ledger)):null;
                result.pendingCapture=sequence.PendingCapture;result.captureReceiptSha256=captureReceiptSha;
                result.captureWindowStartedAt=captureStarted;result.captureAcknowledgedAt=captureAcknowledged;
                result.captureStageDeadlineAt=captureStageDeadline;result.captureTotalDeadlineAt=captureTotalDeadline;
                result.capturePendingNotificationSha256=capturePendingSha;result.capabilityVersion=LongLoopProbeChecks.CapabilityVersion;
                return result;
            }
        }
    }
}
