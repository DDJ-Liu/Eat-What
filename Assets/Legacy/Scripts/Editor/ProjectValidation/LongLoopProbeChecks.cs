using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json.Linq;
using EatWhat.LongLoop;

namespace EatWhat.EditorValidation
{
    [Serializable] public sealed class StreetPayloadContext
    {
        public int schemaVersion=1,attempt,editorPid;
        public string version,taskId,executionId,ownerThreadId,sessionId,projectPath,outputRoot,scenePath,runId,editorStartedAt;
        public string sceneSha256,metaSha256,confinerGeometrySha256,saveRoot,loadedModuleVersionId,implementationSha256;
        public StreetProbeFile[] sources;
    }
    // Pure guards shared by the compiled Editor reader and the existing offline suite.
    // No Unity state, run registration, callbacks or cross-call caches live here.
    public static class StreetPayloadContract
    {
        public const string Version="AI444-street-short-payload-v1";
        public const string ImplementationSha256="6C05F5FE103E80C85898581D9DC92D2AA9E75283A68C8426EEE94CE28C653914";
        public static readonly string[] Roles={"preflight","savesBefore","savesAfter","ready","register","read","captureAck","postStop","cancel"};
        public static void Need(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        public static void NoLinks(string path)
        {
            Need(System.IO.Path.IsPathRooted(path)&&!path.Split('\\','/').Any(s=>s=="."||s==".."||s.TrimEnd(' ','.')!=s),"Literal absolute Street path required.");
            var p=System.IO.Path.GetFullPath(path);
            for(var probe=p;!string.IsNullOrEmpty(probe);probe=System.IO.Path.GetDirectoryName(probe))
                if(System.IO.File.Exists(probe)||System.IO.Directory.Exists(probe))Need((System.IO.File.GetAttributes(probe)&System.IO.FileAttributes.ReparsePoint)==0,"Linked Street path refused.");
        }
        public static string HashFile(string path)
        {NoLinks(path);using(var h=System.Security.Cryptography.SHA256.Create())using(var f=System.IO.File.OpenRead(path))return BitConverter.ToString(h.ComputeHash(f)).Replace("-","");}
        public static JObject JsonFile(string path)
        {
            NoLinks(path);
            var name=System.IO.Path.GetFileName(path);
            bool liveState=System.IO.Path.GetDirectoryName(path).Replace('\\','/').EndsWith("/.ai-workspace/runtime",StringComparison.OrdinalIgnoreCase)&&
                new[]{"queue-state.json","worker-state.json","mode-state.json"}.Contains(name,StringComparer.OrdinalIgnoreCase);
            System.IO.TextReader snapshot;
            if(liveState)
            {
                string text;
                using(var mutex=new System.Threading.Mutex(false,@"Local\EatWhat_AI_Workspace_Queue_v1"))
                {
                    bool taken=false;
                    try
                    {
                        try{taken=mutex.WaitOne(500);}catch(System.Threading.AbandonedMutexException){taken=true;}
                        if(!taken)throw new TimeoutException("Street runtime state read: queue mutex busy after 500ms.");
                        text=System.IO.File.ReadAllText(path);
                    }
                    finally{if(taken)mutex.ReleaseMutex();}
                }
                // No file handle or queue ownership remains while parsing the snapshot.
                snapshot=new System.IO.StringReader(text);
            }
            else snapshot=System.IO.File.OpenText(path);
            using(snapshot)using(var reader=new Newtonsoft.Json.JsonTextReader(snapshot){DateParseHandling=Newtonsoft.Json.DateParseHandling.None})
                return JObject.Load(reader,new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error});
        }
        public static bool SamePath(string a,string b)=>string.Equals(System.IO.Path.GetFullPath(a),System.IO.Path.GetFullPath(b),StringComparison.OrdinalIgnoreCase);
        public static bool TryReadLiveState(string project,out JObject queue,out JObject workers,out JObject mode)
        {
            queue=workers=mode=null;
            var paths=new[]{"queue-state.json","worker-state.json","mode-state.json"}.Select(name=>System.IO.Path.Combine(project,".ai-workspace/runtime",name)).ToArray();
            foreach(var path in paths)NoLinks(path);
            var texts=new string[3];
            using(var mutex=new System.Threading.Mutex(false,@"Local\EatWhat_AI_Workspace_Queue_v1"))
            {
                bool taken=false;
                try
                {
                    try{taken=mutex.WaitOne(0);}catch(System.Threading.AbandonedMutexException){taken=true;}
                    if(!taken)return false;
                    for(int i=0;i<paths.Length;i++)texts[i]=System.IO.File.ReadAllText(paths[i]);
                }
                finally{if(taken)mutex.ReleaseMutex();}
            }
            JObject Parse(string text)
            {
                using(var file=new System.IO.StringReader(text))using(var reader=new Newtonsoft.Json.JsonTextReader(file){DateParseHandling=Newtonsoft.Json.DateParseHandling.None})
                    return JObject.Load(reader,new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error});
            }
            queue=Parse(texts[0]);workers=Parse(texts[1]);mode=Parse(texts[2]);return true;
        }
        private static bool Empty(JToken value)=>value==null||value.Type==JTokenType.Null;
        public static DateTimeOffset Instant(string text)
        {Need(System.Text.RegularExpressions.Regex.IsMatch(text??"",@"(?:Z|[+-]\d\d:\d\d)$"),"Explicit Street UTC offset required.");return DateTimeOffset.Parse(text,System.Globalization.CultureInfo.InvariantCulture);}
        public static void ValidateLive(StreetPayloadContext c,JObject queue,JObject workers,JObject mode,DateTimeOffset now)
        {
            Need(c!=null&&c.schemaVersion==1&&c.version==Version&&c.implementationSha256==ImplementationSha256&&c.attempt>0&&System.Text.RegularExpressions.Regex.IsMatch(c.taskId??"",@"^AI-\d{6}$")&&
                !string.IsNullOrWhiteSpace(c.executionId)&&!string.IsNullOrWhiteSpace(c.ownerThreadId)&&!string.IsNullOrWhiteSpace(c.runId),"Current Street payload identity required.");
            var tasks=((JArray)queue["tasks"]).OfType<JObject>().Where(t=>(string)t["id"]==c.taskId).ToArray();Need(tasks.Length==1,"Unique current Street task required.");
            var t=tasks[0];var e=t["execution"];var w=workers["workers"]?["ENGINE_CLI"];var s=mode["supervised"];
            Need((string)t["pipeline"]=="ENGINE_CLI"&&(string)t["status"]=="RUNNING"&&(int)t["attempts"]==c.attempt&&
                (string)e?["executionId"]==c.executionId&&(string)e?["ownerThreadId"]==c.ownerThreadId&&
                (string)w?["status"]=="BUSY"&&(string)w?["activeTaskId"]==c.taskId&&(string)w?["activeExecutionId"]==c.executionId&&
                (string)w?["threadId"]==c.ownerThreadId&&Instant((string)w?["leaseUntil"])>now&&
                (string)mode["mode"]=="SUPERVISED"&&(string)s?["status"]=="ACTIVE"&&(string)s?["sessionId"]==c.sessionId&&
                (bool?)s?["stopRequested"]==false&&(bool?)s?["exitRequested"]==false&&((JArray)s["taskIds"]).Values<string>().Contains(c.taskId)&&
                (bool?)e?["limitReached"]!=true&&Empty(e?["hardStopAt"])&&Empty(t["retryHardStop"]),"Exact live Street claim/owner/session/lease/stop required.");
        }
        public static StreetPayloadContext ReadContext(string role,string contextPath,string sha,string actualProject)
        {
            Need(Roles.Contains(role),"Unknown short Street role.");NoLinks(actualProject);NoLinks(contextPath);
            contextPath=System.IO.Path.GetFullPath(contextPath);
            var project=System.IO.Path.GetFullPath(actualProject).TrimEnd('\\','/');
            Need(contextPath.StartsWith(project+System.IO.Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)&&
                System.Text.RegularExpressions.Regex.IsMatch(sha??"","^[A-F0-9]{64}$")&&HashFile(contextPath)==sha,"Current reviewed context path/SHA required.");
            var raw=JsonFile(contextPath);var c=Newtonsoft.Json.JsonConvert.DeserializeObject<StreetPayloadContext>(raw.ToString());
            Need(SamePath(c.projectPath,project)&&SamePath(c.outputRoot,System.IO.Path.Combine(project,".ai-workspace","outputs","PLAYABLE-DEMO011-20261104",c.taskId,"attempt"+c.attempt))&&
                SamePath(contextPath,System.IO.Path.Combine(c.outputRoot,"compiled-payload-context.json"))&&c.scenePath=="Assets/Scenes/LongLoop/LongLoop_Street.unity","Exact current project/task/attempt paths required.");
            Need(!string.IsNullOrWhiteSpace(c.runId)&&System.Text.RegularExpressions.Regex.IsMatch(c.runId,"^[A-Za-z0-9_-]{1,100}$"),"Literal current Street run required.");
            ValidateLive(c,JsonFile(System.IO.Path.Combine(project,".ai-workspace/runtime/queue-state.json")),JsonFile(System.IO.Path.Combine(project,".ai-workspace/runtime/worker-state.json")),JsonFile(System.IO.Path.Combine(project,".ai-workspace/runtime/mode-state.json")),DateTimeOffset.UtcNow);
            Need(c.sources!=null&&c.sources.Length>0&&c.sources.Select(f=>f.path).Distinct(StringComparer.OrdinalIgnoreCase).Count()==c.sources.Length,"Distinct current Street source closure required.");
            foreach(var f in c.sources){NoLinks(f.path);Need(System.IO.Path.GetFullPath(f.path).StartsWith(project+System.IO.Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)&&HashFile(f.path)==f.sha256,"Street source drift.");}
            return c;
        }
    }
    [Serializable] public sealed class StreetProbeFile { public string path, sha256; }
    [Serializable] public sealed class StreetProbeStage
    {
        public string id;
        public string captureId;
        public Vector2 input;
        public bool navigate;
        public Vector3 destination;
        public float seconds=.6f, timeoutSeconds=5f;
    }
    [Serializable] public sealed class StreetProbeRequest
    {
        public string suite="LongLoopStreet",spawnPath;
        public int schemaVersion=1, attempt, editorPid;
        public string taskId, executionId, ownerThreadId, runId, projectPath, scenePath, outputRoot;
        public string editorStartedAt, contextSha256, scopeSha256, geometrySha256, authorizationPath, geometryPath;
        public string playerPath, depthPath, sorterPath, followPointPath, cameraPath, brainPath, cameraBoundsPath, configurationAuthorizationMode;
        public float maximumSeconds=120, expectedMoveSpeed=6.5f, expectedVerticalFactor=.6f;
        public Vector2 edgeTangent=new Vector2(1,0);
        public float focusDelta=.5f;
        public StreetProbeStage[] stages;
        public StreetProbeFile[] sources, configuredAssets;
    }
    [Serializable] public sealed class StreetProbeObservation
    {
        public int frame, phaseIndex, layer, expectedLayer, order, baseOrder, frontOrder, backOrder;
        public double elapsed, gameTime;
        public float deltaTime, fixedDeltaTime, speed, verticalFactor, coefficient, groundY, factor, focus, originalFocus, inputX, inputY;
        public Vector3 foot, derivedFoot, visualScale, baselineScale, followPoint, camera, goal, brainOutput, cameraOutput;
        public Vector3 f01PositionCorrection, finalPositionCorrection, f02Contribution;
        public float confinerRequestedDisplacement;
        public double horizontalCameraVelocity,horizontalDeltaVelocity,horizontalAllowedDeltaVelocity,horizontalTargetVelocityBound;
        public double cmDampingDeltaTime,producerTime,producerDeltaTime,horizontalTargetDeltaBound;
        public int producerFrame=-1;public ulong producerSequence;
        public bool confinerClearForCameraTests;
        public float precision, depthOffset, ahead, stopped, delay, recenterSpeed, lookahead, lookaheadTime, cap;
        public float size, f01Size, aspect, dampingError, aheadError, delayGoalError, recenterSpeedObserved;
        public ulong sequence, bodySequence;
        public int channelAdvances, bodyNotifications;
        public bool ready, inputOwned, configsOwned, inside, boundaryBlocked, facingRight, moving, animationWalk, animationIdle;
        public bool sorterTargetMatches, markersReady, channelValid, channelZero, baselineOutside, observerSameFrame, dampingMeasured, settled;
        public string movementPath, phaseId;
        public bool followRTargetPipeline;
        public Vector3 target;
        public float signedLookaheadWeight;
        public string targetStatus;
    }
    [Serializable] public sealed class StreetProbeCheck
    {
        public string id, group, phaseId, reason;
        public bool observed, passed;
        public int frame, sampleStart, sampleEnd;
        public double elapsed;
        public double[] actual, expected, tolerance;
        public StreetProbeObservation observedSample;
    }
    [Serializable] public sealed class StreetProbeCleanup
    {
        public bool inputRestored, configsRestored, configOwnerRestored, callbacksRemoved, temporaryObjectsDestroyed, noForeignOverwrite;
        public string error;
        public bool Passed => inputRestored && configsRestored && configOwnerRestored && callbacksRemoved && temporaryObjectsDestroyed && noForeignOverwrite && string.IsNullOrEmpty(error);
    }
    [Serializable] public sealed class StreetProbeReport
    {
        public FollowRSmokeEvidence followR;
        public StreetProbeCheck[] directions;
        public int schemaVersion=1, attempt, editorPid, samplesSaved, requiredCount;
        public string suite="LongLoopStreet", transport="ManagedEditor", taskId, executionId, ownerThreadId, runId;
        public string projectPath, scenePath, outputRoot, editorStartedAt, contextSha256, scopeSha256, geometrySha256;
        public string requestSha256, startedAt, finishedAt, status, error, sampleLedgerPath, sampleLedgerSha256;
        public string unityVersion, assemblyPath, assemblySha256;
        public string pendingCapture, captureReceiptSha256, captureWindowStartedAt, captureAcknowledgedAt;
        public string captureStageDeadlineAt, captureTotalDeadlineAt, capturePendingNotificationSha256, capabilityVersion;
        public string configurationAuthorizationMode, configurationReviewPath, configurationReviewSha256, humanTuningStatus;
        public string cameraBoundsPath, confinerTypeName, confinerGeometrySha256;
        public int confinerInstanceId, confinerBoundsInstanceId;
        public bool humanTuningCompleted, finalVisualAccepted;
        public bool passed, hardwareInputAcceptance=false, nativeRenderingAccepted=false;
        public StreetProbeCheck[] checks;
        public StreetProbeCleanup cleanup;
        public StreetProbeFile[] sources, configuredAssets;
    }
    [Serializable] public sealed class StreetProbeCapturePending
    {
        public int schemaVersion=1,attempt,editorPid,frame,phaseIndex;
        public string kind="STREET_PROBE_CAPTURE_PENDING",taskId,executionId,ownerThreadId,runId,projectPath,scenePath,editorStartedAt;
        public string phaseId="depth-mid",captureId="sorting",createdAt,stageDeadlineAt,totalDeadlineAt;
        public double elapsed,stageStartedElapsed;
    }

    [Serializable] public sealed class StreetProbeDriver
    {public string kind;public bool enabled, sameCamera, sameBounds;}
    public static class StreetProbeDriverPolicy
    {
        public static void Validate(StreetProbeDriver[] drivers)
        {
            int targetDriver=0,confiner=0;
            foreach(var d in drivers??Array.Empty<StreetProbeDriver>())
            {
                if(d==null)throw new InvalidOperationException("Missing camera driver identity.");
                if(!d.enabled)continue;
                if(!d.sameCamera)throw new InvalidOperationException("Foreign camera driver owner.");
                if(d.kind=="StreetFollowDriver")targetDriver++;
                else if(d.kind=="CinemachineConfiner2D"&&d.sameBounds)confiner++;
                else throw new InvalidOperationException("Unknown driver or mismatched Confiner bounds.");
            }
            if(targetDriver!=1||confiner!=1)throw new InvalidOperationException("Exactly one active StreetFollowDriver/current Confiner required.");
        }
    }

    public sealed class StreetConfigurationIdentity
    {
        public int attempt;
        public string taskId,executionId,ownerThreadId,runId,projectPath,scopeSha256,geometrySha256,controllerThreadId;
    }
    public sealed class StreetConfigurationDecision
    {
        public string mode,reviewPath,reviewSha256,humanTuningStatus;
        public bool humanTuningCompleted,finalVisualAccepted;
    }
    // Native Begin uses this actual validator. The host independently consumes the same explicit JSON contract.
    public static class StreetConfigurationAuthorization
    {
        public static JObject Parse(string json)=>JObject.Parse(json,new JsonLoadSettings {DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error});
        public static string Text(JObject json,string key)
        {var t=json[key];if(t==null||t.Type!=JTokenType.String||string.IsNullOrWhiteSpace((string)t))throw new InvalidOperationException("Missing/typed source string: "+key);return (string)t;}
        private static void Equal(JObject json,string key,string expected)
        {if(Text(json,key)!=expected)throw new InvalidOperationException("Configuration source identity mismatch: "+key);}
        private static void Flag(JObject json,string key,bool expected)
        {var t=json[key];if(t==null||t.Type!=JTokenType.Boolean||(bool)t!=expected)throw new InvalidOperationException("Configuration source boolean mismatch: "+key);}
        private static void Number(JObject json,string key,int expected)
        {var t=json[key];if(t==null||t.Type!=JTokenType.Integer||(int)t!=expected)throw new InvalidOperationException("Configuration source integer mismatch: "+key);}
        public static string OriginalPath(string mode,JObject review)=>Text(review,mode=="USER_ADJUSTED"?"userReplyEvidencePath":"originalUserAuthorizationPath");
        public static string OriginalSha(string mode,JObject review)=>Text(review,mode=="USER_ADJUSTED"?"userReplyEvidenceSha256":"originalUserAuthorizationSha256");
        public static StreetConfigurationDecision Validate(string mode,string reviewJson,string originalText,StreetConfigurationIdentity identity)
        {
            originalText=originalText?.TrimStart('\ufeff');
            originalText=originalText?.TrimStart('\ufeff');
            var review=Parse(reviewJson);Number(review,"schemaVersion",1);Equal(review,"geometrySha256",identity.geometrySha256);
            var result=new StreetConfigurationDecision {mode=mode,finalVisualAccepted=false};
            if(mode=="USER_ADJUSTED")
            {
                Equal(review,"kind","USERa");Equal(review,"source","direct-human-user");Equal(review,"decision","APPROVED");
                if(string.IsNullOrWhiteSpace(originalText))throw new InvalidOperationException("Original human reply is missing.");
                result.humanTuningStatus="USER_ADJUSTED";result.humanTuningCompleted=true;
            }
            else if(mode=="USER_AUTHORIZED_PRESET")
            {
                Equal(review,"kind","CONTROL_PRESET_REVIEW");Equal(review,"source","control-reviewed-preset");Equal(review,"decision","APPROVED_FOR_PRESET_VALIDATION");
                Equal(review,"controllerThreadId",identity.controllerThreadId);Equal(review,"taskId",identity.taskId);Number(review,"attempt",identity.attempt);
                Equal(review,"executionId",identity.executionId);Equal(review,"ownerThreadId",identity.ownerThreadId);Equal(review,"runId",identity.runId);
                Equal(review,"projectPath",identity.projectPath);Equal(review,"scopeSha256",identity.scopeSha256);
                Equal(review,"humanTuningStatus","DEFERRED_NONBLOCKING");Flag(review,"humanTuningCompleted",false);Flag(review,"finalVisualAccepted",false);
                var original=Parse(originalText);Number(original,"schemaVersion",1);
                Equal(original,"source","direct-human-user-message-in-current-CONTROL-chat");Equal(original,"productVersion","demo-0.1.1");
                Flag(original,"doesNotClaimHumanTuningDone",true);Equal(original,"finalHumanVisualAcceptance","PENDING");
                if(!Text(original,"authorization").StartsWith("Use reviewed presets for configuration",StringComparison.Ordinal)||
                    !Text(original,"verbatimRequest").Contains("先按照你的预设来设置后持续推进")||!Text(original,"verbatimRequest").Contains("待人工后续调整"))
                    throw new InvalidOperationException("Original explicit preset permission is missing.");
                result.humanTuningStatus="DEFERRED_NONBLOCKING";result.humanTuningCompleted=false;
            }
            else throw new InvalidOperationException("Unknown configuration authorization mode.");
            OriginalPath(mode,review);OriginalSha(mode,review);return result;
        }
    }

    // Shared production report/check logic. The offline boundary calls this same state machine, not a copied algorithm.
    public static class LongLoopProbeChecks
    {
        public const string CapabilityVersion="AI441-sorting45-clock-ack-v1";
        public static object ReadyPosition(Vector3 value)=>new {x=value.x,y=value.y,z=value.z};
        public static float StageTimeoutLimit(StreetProbeStage stage)=>stage!=null&&stage.id=="depth-mid"&&stage.captureId=="sorting"?45:12;
        public static readonly string[] StageIds={"ready","x-positive","x-negative","y-positive","y-negative","diagonal","idle","edge-approach",
            "edge-slide","edge-stop","depth-low","depth-mid","depth-high","camera-settle","deadzone-hold","deadzone-push",
            "ahead-right","ahead-left","stopped-recenter","focus","f02-zero","restore"};
        public static readonly string[] RequiredIds={
            "move.xPositive","move.xNegative","move.yPositive","move.yNegative","move.diagonal","move.idle","move.edgeSlide","move.edgeStop","move.inside",
            "depth.low","depth.mid","depth.high","depth.foot","depth.facing",
            "sort.target","sort.order","sort.relative",
            "camera.deadZone","camera.push","camera.aheadRight","camera.aheadLeft","camera.delay","camera.recenter","camera.oneDamping",
            "follow.xz","follow.focus","follow.stable","follow.restored",
            "channels.zero","channels.size","channels.sequence","channels.brain"};
        public static void ValidatePlan(StreetProbeRequest r)
        {
            if(r!=null&&r.suite!="LongLoopStreet")throw new InvalidOperationException("Use the explicit SMK-011 entry for a short suite; Begin remains the full probe.");
            if(r==null || r.schemaVersion!=1 || r.stages==null || r.stages.Length!=StageIds.Length ||
                !Finite(r.maximumSeconds) || r.maximumSeconds<=0 || r.maximumSeconds>120 ||
                !Finite(r.expectedMoveSpeed) || r.expectedMoveSpeed<=0 || !Finite(r.expectedVerticalFactor) || r.expectedVerticalFactor<=0 ||
                !Finite(r.focusDelta) || Math.Abs(r.focusDelta)<.01f) throw new InvalidOperationException("Invalid bounded Street probe plan.");
            double total=0;
            for(int i=0;i<StageIds.Length;i++)
            {
                var s=r.stages[i];
                if(s==null || s.id!=StageIds[i] || !Finite(s.input.x) || !Finite(s.input.y) || Math.Abs(s.input.x)>1 || Math.Abs(s.input.y)>1 ||
                    !Finite(s.destination) || !Finite(s.seconds) || !Finite(s.timeoutSeconds) || s.seconds<.4f || s.timeoutSeconds<s.seconds || s.timeoutSeconds>StageTimeoutLimit(s))
                    throw new InvalidOperationException("Missing, reordered or unbounded Street stage: "+StageIds[i]);
                total+=s.timeoutSeconds;
            }
            if(total+2>r.maximumSeconds) throw new InvalidOperationException("One immutable total clock must include all stage deadlines and cleanup.");
            foreach(var p in new[]{new[]{"x-positive","1","0"},new[]{"x-negative","-1","0"},new[]{"y-positive","0","1"},new[]{"y-negative","0","-1"},new[]{"diagonal","1","1"},new[]{"ahead-right","1","0"},new[]{"ahead-left","-1","0"},new[]{"idle","0","0"},new[]{"camera-settle","0","0"},new[]{"stopped-recenter","0","0"},new[]{"focus","0","0"},new[]{"restore","0","0"}})
            {var s=r.stages[Array.IndexOf(StageIds,p[0])];if(s.navigate||s.input.x!=int.Parse(p[1])||s.input.y!=int.Parse(p[2]))throw new InvalidOperationException("Required natural input vector differs: "+p[0]);}
            if(r.stages.Single(s=>s.id=="depth-mid").captureId!="sorting"||r.stages.Where(s=>s.id!="depth-mid").Any(s=>!string.IsNullOrEmpty(s.captureId)))throw new InvalidOperationException("Exactly one bounded sorting screenshot gate required.");
            foreach(var s in r.stages.Where(s=>s.id=="edge-approach"||s.id.StartsWith("depth-")))if(!s.navigate)throw new InvalidOperationException("Explicit natural navigation required: "+s.id);
            if(!Finite(r.edgeTangent.x)||!Finite(r.edgeTangent.y)||Math.Abs(r.edgeTangent.x*r.edgeTangent.x+r.edgeTangent.y*r.edgeTangent.y-1)>.001f)
                throw new InvalidOperationException("Explicit edge tangent must be a unit world-XY direction.");
        }
        public static bool Finite(float v)=>!float.IsNaN(v)&&!float.IsInfinity(v);
        public static bool Finite(Vector3 v)=>Finite(v.x)&&Finite(v.y)&&Finite(v.z);
        public static bool FiniteObservation(StreetProbeObservation s)
        {
            if(s==null || double.IsNaN(s.elapsed) || double.IsInfinity(s.elapsed) || double.IsNaN(s.gameTime) || double.IsInfinity(s.gameTime)) return false;
            foreach(var f in typeof(StreetProbeObservation).GetFields())
            {
                if(f.FieldType==typeof(float) && !Finite((float)f.GetValue(s))) return false;
                if(f.FieldType==typeof(Vector3) && !Finite((Vector3)f.GetValue(s))) return false;
            }
            return true;
        }
        private static string Group(string id)=>id.Substring(0,id.IndexOf('.'));
        public static StreetProbeCheck[] Evaluate(StreetProbeRequest request,IReadOnlyList<StreetProbeObservation> samples)
        {
            var results=new Dictionary<string,StreetProbeCheck>();
            void Add(string id,List<StreetProbeObservation> window,double[] actual,double[] expected,double[] tolerance,string reason="")
            {
                var last=window.Count==0 ? null : window[window.Count-1];
                bool valid=last!=null && window.Count>=3 && actual.Length==expected.Length && actual.Length==tolerance.Length;
                bool passed=valid;
                for(int n=0;n<actual.Length;n++) passed &= !double.IsNaN(actual[n])&&!double.IsInfinity(actual[n])&&
                    !double.IsNaN(expected[n])&&!double.IsInfinity(expected[n])&&tolerance[n]>=0&&!double.IsInfinity(tolerance[n])&&
                    Math.Abs(actual[n]-expected[n])<=tolerance[n];
                results[id]=new StreetProbeCheck {id=id,group=Group(id),phaseId=last?.phaseId,observed=valid,passed=passed,
                    frame=last?.frame??0,elapsed=last?.elapsed??0,sampleStart=window.Count==0?-1:IndexOf(samples,window[0]),
                    sampleEnd=last==null?-1:IndexOf(samples,last),actual=actual,expected=expected,tolerance=tolerance,reason=reason,observedSample=last};
            }
            List<StreetProbeObservation> Window(string phase)=>samples.Where(s=>s.phaseId==phase&&s.settled).ToList();
            void Condition(string id,List<StreetProbeObservation> w,bool condition,string reason="")=>Add(id,w,new[]{condition?1d:0d},new[]{1d},new[]{0d},reason);
            void Motion(string phase,string id,double x,double y,bool vertical=false)
            {
                var w=Window(phase);if(w.Count<3){Condition(id,w,false,"Insufficient natural motion samples");return;}
                var a=w[0];var b=w[w.Count-1];double dt=b.gameTime-a.gameTime;
                if(dt<=.01 || (x!=0&&(Math.Sign(b.foot.x-a.foot.x)!=Math.Sign(x)||Math.Abs(b.foot.x-a.foot.x)<.05)) ||
                    (y!=0&&(Math.Sign(b.foot.y-a.foot.y)!=Math.Sign(y)||Math.Abs(b.foot.y-a.foot.y)<.05)))
                {Condition(id,w,false,"Actual signed displacement/time is absent; paused clocks cannot pass");return;}
                double slack=request.expectedMoveSpeed*(w.Max(s=>s.deltaTime)+w.Max(s=>s.fixedDeltaTime));
                var actual=new[]{(double)b.foot.x-a.foot.x,(double)b.foot.y-a.foot.y,(double)b.speed,(double)b.verticalFactor,
                    w.All(s=>s.animationWalk)?1d:0d,vertical && a.facingRight!=b.facingRight?0d:1d};
                Add(id,w,actual,new[]{request.expectedMoveSpeed*x*dt,request.expectedMoveSpeed*request.expectedVerticalFactor*y*dt,
                    (double)request.expectedMoveSpeed,(double)request.expectedVerticalFactor,1d,1d},new[]{slack,slack,.0001,.0001,0d,0d},"Natural displacement; slack=speed*(max observed render dt+fixed dt), no transform placement");
            }
            Motion("x-positive","move.xPositive",1,0);Motion("x-negative","move.xNegative",-1,0);
            Motion("y-positive","move.yPositive",0,1,true);Motion("y-negative","move.yNegative",0,-1,true);Motion("diagonal","move.diagonal",1,1);
            var idle=Window("idle");Condition("move.idle",idle,idle.Count>0 && idle.All(s=>!s.moving&&s.animationIdle));
            var edge=Window("edge-slide");
            Condition("move.edgeSlide",edge,edge.Count>=3 && edge.All(s=>s.inside&&s.boundaryBlocked&&Math.Abs(s.inputX)+Math.Abs(s.inputY)>.05) &&
                Math.Abs((edge[edge.Count-1].foot.x-edge[0].foot.x)*request.edgeTangent.x+
                    (edge[edge.Count-1].foot.y-edge[0].foot.y)*request.edgeTangent.y)>.05,"Actual in-area tangential progress under sustained boundary input");
            var blocked=Window("edge-stop");Condition("move.edgeStop",blocked,blocked.Count>=3 && blocked.All(s=>!s.moving&&s.boundaryBlocked&&Math.Abs(s.inputX)+Math.Abs(s.inputY)>.05) && blocked[blocked.Count-1].ahead==0);
            var all=samples.ToList();Condition("move.inside",all,all.Count>0&&all.All(s=>s.inside));
            var depth=new List<StreetProbeObservation>();
            foreach(var pair in new[]{new[]{"depth-low","depth.low"},new[]{"depth-mid","depth.mid"},new[]{"depth-high","depth.high"}})
            {
                var w=Window(pair[0]);depth.AddRange(w);
                bool good=w.Count>=3;
                foreach(var s in w)
                {
                    double factor=1d+((double)s.groundY-s.foot.y)*s.coefficient;
                    good &= Math.Abs(s.factor-factor)<.0002 && Math.Abs(Math.Abs(s.visualScale.x)-Math.Abs(s.baselineScale.x*factor))<.002 &&
                        Math.Abs(s.visualScale.y-s.baselineScale.y*factor)<.002 && Math.Abs(s.visualScale.z-s.baselineScale.z*factor)<.002;
                }
                Condition(pair[1],w,good,"Actual visual Transform scale vs GroundLine/DepthScaleFactor; no nonexistent Near/Far/Strength fields");
            }
            Condition("depth.foot",depth,depth.Count>=9 && depth.All(s=>(s.derivedFoot-s.foot).magnitude<.003));
            Condition("depth.facing",all,all.Count>0&&all.All(s=>Math.Abs(Math.Abs(s.visualScale.x)-Math.Abs(s.baselineScale.x*s.factor))<.002));
            Condition("sort.target",all,all.Count>0&&all.All(s=>s.sorterTargetMatches&&s.layer==s.expectedLayer));
            Condition("sort.order",all,all.Count>0&&all.All(s=>s.order==(int)Math.Max(-32768,Math.Min(32767,
                (double)s.baseOrder-Math.Round(((double)s.foot.y+s.depthOffset)*s.precision,MidpointRounding.ToEven)))));
            var mid=Window("depth-mid");Condition("sort.relative",mid,mid.Count>=3&&mid.All(s=>s.markersReady&&s.frontOrder>s.order&&s.backOrder<s.order),"Numerical ordering only; actual drawing requires separately bound official PNG");
            var hold=Window("deadzone-hold");Condition("camera.deadZone",hold,hold.Count>=3&&(hold[hold.Count-1].goal-hold[0].goal).magnitude<.003&&
                (hold[hold.Count-1].cameraOutput-hold[0].cameraOutput).magnitude<.02,"Ahead0 isolated dead zone; settled final actual output tolerance .02u");
            var push=Window("deadzone-push");Condition("camera.push",push,push.Count>=3&&(push[push.Count-1].goal-push[0].goal).magnitude>.02);
            foreach(var p in new[]{new[]{"ahead-right","camera.aheadRight","1"},new[]{"ahead-left","camera.aheadLeft","-1"}})
            {
                var w=Window(p[0]);bool good=w.Count>=3;
                if(good){var last=w[w.Count-1];double target=int.Parse(p[2])*Math.Min(last.lookahead,last.cap);
                    good=Math.Sign(last.ahead)==Math.Sign(target)&&Math.Abs(last.ahead-target)<=Math.Abs(target)*.12+.003&&w.Where(s=>s.dampingMeasured).All(s=>s.aheadError<.003);}
                Condition(p[1],w,good,"Actual fixed-distance build after >=lookaheadTime; final within12% of one-distance target");
            }
            var stop=Window("stopped-recenter");
            Condition("camera.delay",stop,stop.Any(s=>s.stopped>0&&s.stopped<s.delay)&&stop.Where(s=>s.stopped<s.delay).All(s=>s.delayGoalError<.003));
            Condition("camera.recenter",stop,stop.Any(s=>s.recenterSpeedObserved>.01f)&&
                stop.Where(s=>s.recenterSpeedObserved>.01f).All(s=>Math.Abs(s.recenterSpeedObserved-s.recenterSpeed)<.03f)&&
                stop.Count>0&&stop[stop.Count-1].ahead==0&&Math.Abs(stop[stop.Count-1].goal.x-stop[stop.Count-1].followPoint.x)<.02&&
                Math.Abs(stop[stop.Count-1].goal.y-stop[stop.Count-1].followPoint.y)<.02,"Observed per-frame nonterminal Goal movement and final center, not final position only");
            var damping=Window("ahead-right").Concat(Window("ahead-left")).ToList();
            Condition("camera.oneDamping",damping,damping.Count(s=>s.dampingMeasured)>=6&&damping.Where(s=>s.dampingMeasured).All(s=>s.dampingError<.003),"Actual per-frame raw vs previous raw + (goal+ahead-previous)*(1-exp(-dt/tau))");
            Condition("follow.xz",all,all.Count>0&&all.All(s=>Math.Abs(s.followPoint.x-s.foot.x)<.002&&Math.Abs(s.followPoint.z-s.foot.z)<.002));
            var focus=Window("focus");Condition("follow.focus",focus,focus.Count>=3&&focus.All(s=>Math.Abs(s.followPoint.y-s.foot.y-s.focus)<.002&&Math.Abs(s.focus-s.originalFocus-request.focusDelta)<.002));
            Condition("follow.stable",all,all.Count>0&&all.All(s=>Math.Abs(s.followPoint.y-s.foot.y-s.focus)<.003&&(s.derivedFoot-s.foot).magnitude<.003));
            var restore=Window("restore");Condition("follow.restored",restore,restore.Count>=3&&restore.All(s=>Math.Abs(s.followPoint.y-s.foot.y-s.focus)<.003&&Math.Abs(s.focus-s.originalFocus)<.002));
            var zero=Window("f02-zero");Condition("channels.zero",zero,zero.Count>=3&&zero.All(s=>s.channelValid&&s.channelZero)&&zero.Any(s=>s.baselineOutside));
            Condition("channels.size",all,all.Count>0&&all.All(s=>s.size.Equals(s.f01Size)));
            Condition("channels.sequence",all,all.Count>0&&all.All(s=>s.observerSameFrame&&s.sequence==s.bodySequence&&s.channelAdvances==s.bodyNotifications&&s.bodyNotifications>0));
            Condition("channels.brain",all,all.Count>0&&all.All(s=>(s.brainOutput-s.cameraOutput).magnitude<.003));
            // Never attribute the known Confiner's correction to F02. Full F01 camera predicates require a clear route.
            foreach(var id in RequiredIds.Where(id=>id.StartsWith("camera.",StringComparison.Ordinal)))
            {
                var check=results[id];var window=samples.Where(s=>s.phaseId==check.phaseId).ToArray();
                if(window.Any(s=>!s.confinerClearForCameraTests)){check.passed=false;check.actual=new[]{0d};check.expected=new[]{1d};check.tolerance=new[]{0d};check.reason="Actual Confiner affects this camera window; preserve all corrections and review route, never disable it.";}
            }
            if(samples.Any(s=>s.followRTargetPipeline))
                foreach(var id in RequiredIds.Where(id=>id.StartsWith("camera.",StringComparison.Ordinal)||id.StartsWith("channels.",StringComparison.Ordinal)||id=="move.edgeStop"))
                {
                    var check=results[id];check.observed=false;check.passed=false;
                    check.reason="NOT_APPLICABLE_FOLLOW_R: legacy camera/F01/F02 predicate; target semantics require a separate reviewed probe REPAIR. This is not PASS.";
                }
            return RequiredIds.Select(id=>results.TryGetValue(id,out var check)?check:new StreetProbeCheck {id=id,group=Group(id),passed=false,observed=false,reason="Missing required observation"}).ToArray();
        }
        private static int IndexOf(IReadOnlyList<StreetProbeObservation> list,StreetProbeObservation value)
        {for(int i=0;i<list.Count;i++)if(ReferenceEquals(list[i],value))return i;return -1;}
        public static StreetProbeReport BuildReport(StreetProbeRequest request,IReadOnlyList<StreetProbeObservation> samples,StreetProbeCleanup cleanup)
            =>new StreetProbeReport {taskId=request.taskId,attempt=request.attempt,executionId=request.executionId,ownerThreadId=request.ownerThreadId,runId=request.runId,
                projectPath=request.projectPath,scenePath=request.scenePath,outputRoot=request.outputRoot,editorPid=request.editorPid,editorStartedAt=request.editorStartedAt,
                contextSha256=request.contextSha256,scopeSha256=request.scopeSha256,geometrySha256=request.geometrySha256,checks=Evaluate(request,samples),
                samplesSaved=samples.Count,requiredCount=RequiredIds.Length,cleanup=cleanup,sources=request.sources,configuredAssets=request.configuredAssets};
    }

    // SMK-011 is deliberately limited to four natural input pulses. It does not use the full stage/capture plan.
    // FOLLOW-R only. The historical SMK-011 sequence and SUP payload contract below/above remain separate.
    [Serializable] public sealed class StreetProbeInputObservation
    {
        public string inputType,reason,context,readError,wallTime,providerEnabled="UNAVAILABLE";
        public string horizontal="UNREAD",vertical="UNREAD",interact="UNREAD",backPressed="UNAVAILABLE",device="UNREAD";
        public string lookX="UNREAD",lookY="UNREAD",mouseX="UNREAD",mouseY="UNREAD",referenceLookX="UNAVAILABLE",referenceLookY="UNAVAILABLE";
        public int frame,screenWidth,screenHeight;public bool inputAvailable,complete,applicationFocused;
    }
    [Serializable] public sealed class FollowRSmokeEvidence
    {
        public string rule=FollowRSmokeSequence.Rule,phase,deadlineFormula,continuityFormula;
        public string snapshotState,snapshotValidatedLeaseUntil;
        public int snapshotFreshValidations,snapshotBusyObservations,snapshotRecoveries,snapshotValidationFailures;
        public double snapshotLastFreshWall,snapshotBusyDeadline;
        public StreetProbeInputObservation inputAtRegistration,inputLastObservation,inputAtStop;
        public int startupFrame=-1,baselineFrame=-1,failurePreviousFrame=-1,failureCurrentFrame=-1;
        public ulong startupSequence,baselineSequence,failurePreviousSequence,failureCurrentSequence;
        public string startupGameTime,baselineGameTime,failureReason,failurePreviousGameTime,failureCurrentGameTime,failureDeltaTime,failurePreviousWallTime,failureCurrentWallTime;
        public double startupWallSeconds,maxObservedGameInterval,maxObservedCmInterval,maxObservedProducerInterval;
        public double stopProducerTime,producerReturnDeadline,maxBeforeDelayTargetDrift;public int delayWaitSamples;
        public string failureCmDeltaTime,failureProducerTime,failureProducerDeltaTime;
        public double playerSpeed,visibleWidth,lookaheadTau,cameraTau,velocityBound,accelerationBound,positionEpsilon;
        public double stopGameTime,stopDistanceX,stopDistanceY,stopGoalDistanceX,returnDuration,returnDeadline,frameMargin=.25;
        public double maxSpeed,maxDeltaVelocity,maxAllowedDeltaVelocity,maxViolationRatio,previousDt,currentDt;
        public int maxDeltaFrame,velocitySamples,deltaVelocitySamples,initialPositionOnlySamples;
        public string consoleBeforePath,consoleAfterPath,consoleBeforeSha256,consoleAfterSha256,consoleSession;
        public long consoleBeforeCursor,consoleAfterCursor;
        public int consoleNewErrors,callbackErrors;
        public string consoleReason;
    }
    public sealed class FollowRSmokeSequence
    {
        public const string Suite="FOLLOW-R",Rule="FOLLOW-R-486-v1";
        public const string ReadOperation="compiled-menu-inspect-ready-read",RunOperation="compiled-menu-inspect-ready-begin-read-owned-cancel";
        public const int SampleLimit=12000;
        public const double MoveSeconds=3,ReadySeconds=.1,ClaimCheckSeconds=.5;
        public static readonly string[] RequiredIds={"followR.ready","followR.right3s","followR.targetRecenter","followR.horizontalContinuity","followR.consoleNoNewErrors"};
        private readonly StreetProbeRequest request;
        private readonly StreetFollowSettings settings;
        private readonly double started,wallStarted;
        private readonly StreetProbeObservation first;
        private readonly StreetProbeCheck[] checks=RequiredIds.Select(id=>new StreetProbeCheck {id=id,reason="NOT_OBSERVED"}).ToArray();
        private StreetProbeObservation previous,movingStart;
        private double lastWall,phaseStart,lastSpeed,lastDt,lastCoefficient,velocityEnvelope,lastCmDt,rightProducerStart;
        private bool hasVelocity,leading;
        private int samples;
        public FollowRSmokeEvidence Evidence {get;}=new FollowRSmokeEvidence();
        public bool Finished {get;private set;}
        public string Error {get;private set;}
        public Vector2 Input=>Finished||Evidence.phase!="right"?Vector2.zero:new Vector2(1,0);
        private static void Need(bool b,string why)=>StreetPayloadContract.Need(b,why);
        private static bool Empty(JToken x)=>x==null||x.Type==JTokenType.Null;
        private static bool Finite(double x)=>!double.IsNaN(x)&&!double.IsInfinity(x);
        public static string AttemptRoot(string project,StreetProbeRequest r)=>System.IO.Path.Combine(project,".ai-workspace/outputs/FOLLOW-R-20261110",r.taskId,"attempt"+r.attempt);
        // Called by the same existing request reader; no token, runtime mutation or alternate executor.
        public static void ValidateRequest(StreetProbeRequest r,string project,string action,JObject queue,JObject workers,JObject mode,DateTimeOffset now)
        {
            Need(r!=null&&r.schemaVersion==1&&r.suite==Suite&&r.attempt>0&&System.Text.RegularExpressions.Regex.IsMatch(r.taskId??"",@"^AI-\d{6}$")&&
                System.Text.RegularExpressions.Regex.IsMatch(r.runId??"","^[A-Za-z0-9_-]{1,64}$")&&!string.IsNullOrWhiteSpace(r.executionId)&&!string.IsNullOrWhiteSpace(r.ownerThreadId)&&
                LongLoopProbeChecks.Finite(r.maximumSeconds)&&r.maximumSeconds>MoveSeconds+ReadySeconds&&r.maximumSeconds<=120&&(r.stages==null||r.stages.Length==0),"Exact bounded FOLLOW-R request required.");
            Need(new[]{"inspect","ready","begin","read","cancel","frame"}.Contains(action),"Unknown FOLLOW-R action.");
            Need(StreetPayloadContract.SamePath(r.projectPath,project)&&r.scenePath=="Assets/Scenes/LongLoop/LongLoop_Street.unity"&&
                StreetPayloadContract.SamePath(r.outputRoot,System.IO.Path.Combine(AttemptRoot(project,r),"follow-r",r.runId)),"Exact FOLLOW-R claim/output paths required.");
            StreetPayloadContract.NoLinks(r.outputRoot);
            var tasks=((JArray)queue["tasks"]).OfType<JObject>().Where(t=>(string)t["id"]==r.taskId).ToArray();Need(tasks.Length==1,"Unique FOLLOW-R task required.");
            var t=tasks[0];var e=t["execution"];var w=workers["workers"]?["ENGINE_CLI"];
            Need((string)t["pipeline"]=="ENGINE_CLI"&&(string)t["status"]=="RUNNING"&&(int?)t["attempts"]==r.attempt&&(int?)e?["attempt"]==r.attempt&&
                (string)e?["executionId"]==r.executionId&&(string)e?["ownerThreadId"]==r.ownerThreadId&&(string)e?["mode"]==(string)mode["mode"]&&new[]{"MANUAL","SUPERVISED"}.Contains((string)mode["mode"])&&
                (string)w?["status"]=="BUSY"&&(string)w?["executorKind"]=="THREAD"&&(string)w?["activeTaskId"]==r.taskId&&(string)w?["activeExecutionId"]==r.executionId&&
                (string)w?["threadId"]==r.ownerThreadId&&StreetPayloadContract.Instant((string)w?["leaseUntil"])>now&&
                (bool?)e?["limitReached"]==false&&Empty(e?["hardStopAt"])&&Empty(t["retryHardStop"])&&
                (Empty(e?["softLimitAt"])||!Empty(e?["softLimitHandoff"])),"Current ENGINE claim/mode/owner/lease/stop required.");
            if((string)mode["mode"]=="SUPERVISED")
            {
                Need(!string.IsNullOrWhiteSpace((string)mode["supervised"]?["sessionId"]),"Current formal SUP session required.");
                StreetPayloadContract.ValidateLive(new StreetPayloadContext {version=StreetPayloadContract.Version,implementationSha256=StreetPayloadContract.ImplementationSha256,
                    taskId=r.taskId,attempt=r.attempt,executionId=r.executionId,ownerThreadId=r.ownerThreadId,runId=r.runId,sessionId=(string)mode["supervised"]?["sessionId"]},queue,workers,mode,now);
            }
            bool moving=action=="begin"||action=="frame"||action=="cancel";
            void Scope(JToken scope)
            {
                Need(scope is JObject&&scope["operations"] is JArray&&scope["paths"] is JArray,"Formal FOLLOW-R scope required.");
                var ops=((JArray)scope["operations"]).Values<string>().ToArray();
                Need(moving?ops.Contains(RunOperation)&&ops.Contains("single-play-and-stop"):ops.Contains(ReadOperation)||ops.Contains(RunOperation),"FOLLOW-R action exceeds formal operations.");
                var paths=((JArray)scope["paths"]).Values<string>().ToArray();
                foreach(var needed in new[]{AttemptRoot(project,r),System.IO.Path.Combine(project,r.scenePath),System.IO.Path.Combine(project,"Assets/Editor/ProjectValidation/LongLoopAutomatedProbe.cs"),System.IO.Path.Combine(project,"Assets/Editor/ProjectValidation/LongLoopProbeChecks.cs")})
                    Need(paths.Any(p=>{if(string.IsNullOrWhiteSpace(p)||p.Split('\\','/').Any(s=>s==".."||s=="."))return false;
                        string full=System.IO.Path.GetFullPath(System.IO.Path.Combine(project,p)).TrimEnd('\\','/');
                        return StreetPayloadContract.SamePath(needed,full)||System.IO.Path.GetFullPath(needed).StartsWith(full+System.IO.Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase);}),"FOLLOW-R path exceeds formal scope: "+needed);
            }
            Scope(t["businessScope"]);if(!Empty(e?["approvedScope"]))Scope(e["approvedScope"]);
            // A completed historical supervised session is irrelevant to a current MANUAL claim.
        }
        public FollowRSmokeSequence(StreetProbeRequest r,double wall,StreetProbeObservation initial,StreetFollowSettings s,float tau,float playerSpeed,float width,double? registeredWall=null)
        {
            Need(r!=null&&r.suite==Suite&&Finite(wall)&&initial!=null&&Finite(initial.gameTime),"FOLLOW-R initial observation required.");
            request=r;settings=s;started=phaseStart=initial.gameTime;wallStarted=registeredWall??wall;lastWall=wall;first=previous=initial;
            Need(Finite(wallStarted)&&wallStarted<=wall,"Original registration clock required.");
            Evidence.phase="ready";Evidence.playerSpeed=playerSpeed;Evidence.visibleWidth=width;Evidence.cameraTau=tau;
            Evidence.lookaheadTau=s.LookaheadTime>0?s.LookaheadTime/3d:StreetFollowPipeline.ZeroLookaheadTimeConstant;
            double half=.5*width*s.DeadZoneWidth,lead=Math.Min(s.Lookahead,s.MaxHorizontalOffset);
            Need(StreetFollowPipeline.Valid(s)&&Finite(tau)&&tau>0&&Finite(playerSpeed)&&playerSpeed>0&&Finite(width)&&width>0,"Finite positive continuity premises required; zero camera tau is not a smooth path.");
            Need(s.RecenterSpeed>0||half+lead==0,"Current zero recenter speed cannot satisfy target return; values retained.");
            Evidence.velocityBound=Math.Max(playerSpeed+(half+lead)/Evidence.lookaheadTau,s.RecenterSpeed);
            Evidence.accelerationBound=2*Evidence.velocityBound/tau+.01/(tau*tau);
            Evidence.positionEpsilon=2e-6*Math.Max(1,Math.Abs(initial.brainOutput.x)+3*playerSpeed+half+lead);
            Evidence.continuityFormula="X only; g=game observation interval,c=actual CM callback dt,h=producer elapsed. D=right?speed*g+(halfDeadZone+lead)/lookTau*exp(-elapsedProducerRight/lookTau)*h:recenterSpeed*h; q=(1-exp(-c/cameraTau))/g; k=q/qPrevious*exp(-cPrevious/cameraTau); allowedDeltaV=abs(k-1)*EPrevious+q*D+4*positionEpsilon*(1/gPrevious+1/g), u/s; E=abs(k)*EPrevious+q*D; first E=q*(0.01+D). Same bound as previous formula when clocks coincide. Inputs/time only, never a bad observed target/velocity.";
            Evidence.deadlineFormula="At input-zero boundary: delay+max(actual StepAxis duration(goalX,followX), StepAxis duration(goalY,followY), StepAxis duration(unclipped horizontal target,followX))+0.25s; fixed once, no extension";
            checks[0]=new StreetProbeCheck {id=RequiredIds[0],observed=true,passed=initial.ready&&initial.inside&&initial.inputOwned&&ValidSample(initial)&&Math.Abs(initial.brainOutput.x-initial.target.x)<=.01,observedSample=initial,reason="Y Confiner displacement allowed; X initial residual <=0.01u"};
            Evidence.initialPositionOnlySamples=1;
            if(!checks[0].passed)Cancel("FOLLOW_R_NOT_READY_OR_INITIAL_X_UNSETTLED");
        }
        private static bool ValidSample(StreetProbeObservation s)=>s!=null&&s.ready&&s.inputOwned&&s.inside&&s.followRTargetPipeline&&
            StreetFollowPipeline.Finite(s.foot)&&StreetFollowPipeline.Finite(s.followPoint)&&StreetFollowPipeline.Finite(s.target)&&StreetFollowPipeline.Finite(s.brainOutput)&&Finite(s.gameTime);
        public static string Clock(double value)=>value.ToString("R",System.Globalization.CultureInfo.InvariantCulture);
        public void Cancel(string error){if(!Finished){Error=error;Finished=true;Evidence.phase="finished";}}
        public void CheckDeadline(double wall)
        {if(!Finished&&(!Finite(wall)||wall<lastWall||wall-wallStarted>=request.maximumSeconds)){Evidence.failureReason="FOLLOW_R_WALL_TIMEOUT_OR_CLOCK";Evidence.failurePreviousWallTime=Clock(lastWall);Evidence.failureCurrentWallTime=Clock(wall);Cancel(Evidence.failureReason);}lastWall=wall;}
        public Vector2 Tick(StreetProbeObservation sample,double wall,StreetFollowState state,Action<StreetProbeObservation> persist)
        {
            CheckDeadline(wall);if(Finished)return Vector2.zero;
            void RejectSample(string why)
            {
                Evidence.failureReason=why;Evidence.failurePreviousFrame=previous.frame;Evidence.failureCurrentFrame=sample==null?-1:sample.frame;
                Evidence.failurePreviousSequence=previous.sequence;Evidence.failureCurrentSequence=sample==null?0:sample.sequence;
                Evidence.failurePreviousGameTime=Clock(previous.gameTime);Evidence.failureCurrentGameTime=sample==null?"MISSING":Clock(sample.gameTime);
                Evidence.failureDeltaTime=sample==null?"MISSING":Clock(sample.gameTime-previous.gameTime);Evidence.failureCmDeltaTime=sample==null?"MISSING":Clock(sample.cmDampingDeltaTime);Evidence.failureProducerTime=sample==null?"MISSING":Clock(sample.producerTime);Evidence.failureProducerDeltaTime=sample==null?"MISSING":Clock(sample.producerDeltaTime);Cancel(why);
            }
            if(!ValidSample(sample)){RejectSample("FOLLOW_R_INVALID_OR_FOREIGN_SAMPLE");return Vector2.zero;}
            if(sample.frame==previous.frame){if(sample.gameTime!=previous.gameTime)RejectSample("FOLLOW_R_DUPLICATE_FRAME_CLOCK");return Input;}
            double dt=sample.gameTime-previous.gameTime;
            string invalid=sample.frame!=previous.frame+1?"FOLLOW_R_NONCONSECUTIVE_FRAME":sample.sequence<=previous.sequence?"FOLLOW_R_STALLED_OR_REVERSED_SEQUENCE":
                !Finite(dt)||dt<=0?"FOLLOW_R_NONADVANCING_GAME_CLOCK":
                samples>=SampleLimit?"FOLLOW_R_SAMPLE_LIMIT":sample.gameTime-started>=request.maximumSeconds?"FOLLOW_R_GAME_TIMEOUT":null;
            if(invalid!=null){RejectSample(invalid);return Vector2.zero;}
            if(!Finite(sample.cmDampingDeltaTime)||sample.cmDampingDeltaTime<=0||!Finite(sample.producerTime)||!Finite(sample.producerDeltaTime)||sample.producerDeltaTime<=0||sample.producerTime<=previous.producerTime||sample.producerFrame!=sample.frame||sample.producerSequence!=sample.sequence){RejectSample("FOLLOW_R_INVALID_OR_UNPAIRED_PRODUCER_CLOCK");return Vector2.zero;}samples++;
            Evidence.maxObservedGameInterval=Math.Max(Evidence.maxObservedGameInterval,dt);Evidence.maxObservedCmInterval=Math.Max(Evidence.maxObservedCmInterval,sample.cmDampingDeltaTime);Evidence.maxObservedProducerInterval=Math.Max(Evidence.maxObservedProducerInterval,sample.producerDeltaTime);
            sample.deltaTime=(float)dt;sample.elapsed=sample.gameTime-started;sample.phaseId=Evidence.phase;
            double buildRate=(.5*Evidence.visibleWidth*settings.DeadZoneWidth+Math.Min(settings.Lookahead,settings.MaxHorizontalOffset))/Evidence.lookaheadTau*Math.Exp(-Math.Max(0,previous.producerTime-rightProducerStart)/Evidence.lookaheadTau);
            double targetDeltaBound=Evidence.phase=="right"?Evidence.playerSpeed*dt+buildRate*sample.producerDeltaTime:settings.RecenterSpeed*sample.producerDeltaTime;
            double coefficient=(1-Math.Exp(-sample.cmDampingDeltaTime/Evidence.cameraTau))/dt;
            sample.horizontalTargetDeltaBound=targetDeltaBound;sample.horizontalTargetVelocityBound=targetDeltaBound/dt;
            double speed=(sample.brainOutput.x-previous.brainOutput.x)/dt;Evidence.velocitySamples++;Evidence.maxSpeed=Math.Max(Evidence.maxSpeed,Math.Abs(speed));
            if(hasVelocity)
            {
                double k=coefficient/lastCoefficient*Math.Exp(-lastCmDt/Evidence.cameraTau);
                double dv=Math.Abs(speed-lastSpeed),allowed=Math.Abs(k-1)*velocityEnvelope+coefficient*targetDeltaBound+4*Evidence.positionEpsilon*(1/lastDt+1/dt);
                velocityEnvelope=Math.Abs(k)*velocityEnvelope+coefficient*targetDeltaBound;
                sample.horizontalDeltaVelocity=dv;sample.horizontalAllowedDeltaVelocity=allowed;
                Evidence.deltaVelocitySamples++;Evidence.maxAllowedDeltaVelocity=Math.Max(Evidence.maxAllowedDeltaVelocity,allowed);
                Evidence.maxViolationRatio=Math.Max(Evidence.maxViolationRatio,dv/allowed);
                if(dv>Evidence.maxDeltaVelocity){Evidence.maxDeltaVelocity=dv;Evidence.maxDeltaFrame=sample.frame;Evidence.previousDt=lastDt;Evidence.currentDt=dt;}
                if(dv>allowed){checks[3]=new StreetProbeCheck {id=RequiredIds[3],observed=true,passed=false,observedSample=sample,actual=new[]{dv},expected=new[]{allowed},reason="Horizontal adjacent natural-frame velocity change exceeded frozen bound (u/s)"};Cancel("FOLLOW_R_HORIZONTAL_VELOCITY_JUMP");}
            }
            if(!hasVelocity)velocityEnvelope=coefficient*(.01+targetDeltaBound);
            sample.horizontalCameraVelocity=speed;persist(sample);
            hasVelocity=true;lastSpeed=speed;lastDt=dt;lastCoefficient=coefficient;lastCmDt=sample.cmDampingDeltaTime;previous=sample;if(Finished)return Vector2.zero;
            if(Evidence.phase=="ready"&&sample.gameTime-phaseStart>=ReadySeconds)
            {
                if((sample.foot-first.foot).magnitude>.01){Cancel("FOLLOW_R_INITIAL_PLAYER_MOVING");return Vector2.zero;}
                Evidence.phase="right";phaseStart=sample.gameTime;rightProducerStart=sample.producerTime;movingStart=sample;
            }
            else if(Evidence.phase=="right")
            {
                leading|=sample.target.x-sample.followPoint.x>0;
                if(sample.gameTime-phaseStart>=MoveSeconds)
                {
                    checks[1]=new StreetProbeCheck {id=RequiredIds[1],observed=true,passed=sample.foot.x-movingStart.foot.x>.001&&leading&&sample.target.x-sample.followPoint.x>0&&sample.brainOutput.x-movingStart.brainOutput.x>.001,
                        observedSample=sample,actual=new double[]{sample.foot.x-movingStart.foot.x,sample.target.x-sample.followPoint.x,sample.brainOutput.x-movingStart.brainOutput.x},reason="Natural right input >=3s; actual foot, target lead and final Brain X direction"};
                    if(!checks[1].passed){Cancel("FOLLOW_R_RIGHT_MOVEMENT_OR_LEAD");return Vector2.zero;}
                    Evidence.phase="return";Evidence.stopGameTime=sample.gameTime;Evidence.stopDistanceX=Math.Abs(state.Lookahead.LastTarget-sample.followPoint.x);
                    Evidence.stopDistanceY=Math.Abs(state.Goal.y-sample.followPoint.y);Evidence.stopGoalDistanceX=Math.Abs(state.Goal.x-sample.followPoint.x);
                    double Duration(double from,double to){var axis=default(RecenterAxisState);double p;StreetFollowPipeline.StepAxis(ref axis,from,to,double.Epsilon,settings,out p);return axis.Duration;}
                    Evidence.returnDuration=settings.RecenterDelay+Math.Max(Duration(state.Lookahead.LastTarget,sample.followPoint.x),Math.Max(Duration(state.Goal.x,sample.followPoint.x),Duration(state.Goal.y,sample.followPoint.y)))+Evidence.frameMargin;
                    Evidence.returnDeadline=sample.gameTime+Evidence.returnDuration;Evidence.stopProducerTime=sample.producerTime;Evidence.producerReturnDeadline=sample.producerTime+Evidence.returnDuration;
                    if(!Finite(Evidence.returnDuration)||Evidence.returnDeadline-started>=request.maximumSeconds){Cancel("FOLLOW_R_CURRENT_RETURN_EXCEEDS_FIXED_TOTAL_BOUND");return Vector2.zero;}
                }
            }
            else if(Evidence.phase=="return")
            {
                if((sample.foot-previousStopFoot()).magnitude>.01){Cancel("FOLLOW_R_PLAYER_MOVED_AFTER_INPUT_ZERO");return Vector2.zero;}
                if(sample.stopped<settings.RecenterDelay)
                {
                    Evidence.delayWaitSamples++;double drift=(sample.target-checks[1].observedSample.target).magnitude;Evidence.maxBeforeDelayTargetDrift=Math.Max(Evidence.maxBeforeDelayTargetDrift,drift);
                    if(drift>=.05){checks[2]=new StreetProbeCheck {id=RequiredIds[2],observed=true,passed=false,observedSample=sample,actual=new[]{drift,(double)sample.stopped},expected=new double[]{.05,settings.RecenterDelay},reason="Target moved before configured Delay"};Cancel("FOLLOW_R_RETURN_BEFORE_CONFIGURED_DELAY");return Vector2.zero;}
                }
                double distance=(sample.target-sample.followPoint).magnitude;
                if(distance<.05)
                {
                    checks[2]=new StreetProbeCheck {id=RequiredIds[2],observed=true,passed=sample.gameTime<=Evidence.returnDeadline&&sample.producerTime<=Evidence.producerReturnDeadline&&(settings.RecenterDelay<=0||Evidence.delayWaitSamples>0),observedSample=sample,actual=new[]{distance,sample.gameTime-Evidence.stopGameTime},expected=new[]{.05,Evidence.returnDuration}};
                    checks[3]=new StreetProbeCheck {id=RequiredIds[3],observed=Evidence.deltaVelocitySamples>0,passed=Evidence.deltaVelocitySamples>0,actual=new[]{Evidence.maxDeltaVelocity,Evidence.maxViolationRatio},reason=Evidence.continuityFormula};
                    Finished=true;Evidence.phase="finished";
                }
                else if(sample.gameTime>=Evidence.returnDeadline||sample.producerTime>=Evidence.producerReturnDeadline){checks[2]=new StreetProbeCheck {id=RequiredIds[2],observed=true,passed=false,actual=new[]{distance},reason="Frozen actual-distance deadline expired"};Cancel("FOLLOW_R_TARGET_DID_NOT_RETURN");}
            }
            return Input;
        }
        private Vector3 previousStopFoot()=>checks[1].observedSample.foot;
        public StreetProbeReport Report(StreetProbeCleanup cleanup,StreetProbeCheck console)
        {
            if(console!=null)checks[4]=console;
            bool done=Finished&&Error==null&&cleanup.Passed&&checks.All(c=>c.observed&&c.passed);
            return new StreetProbeReport {suite=Suite,taskId=request.taskId,attempt=request.attempt,executionId=request.executionId,ownerThreadId=request.ownerThreadId,runId=request.runId,
                projectPath=request.projectPath,scenePath=request.scenePath,outputRoot=request.outputRoot,editorPid=request.editorPid,editorStartedAt=request.editorStartedAt,requiredCount=5,
                status=!Finished?"RUNNING":Error!=null||checks.Any(c=>c.observed&&!c.passed)||!cleanup.Passed?"FAILED":done?"COMPLETED":"AWAITING_NATIVE_CONSOLE",error=Error,passed=done,cleanup=cleanup,checks=checks,followR=Evidence};
        }
        public static StreetProbeCheck ConsoleCheck(JObject before,JObject after,DateTimeOffset start,DateTimeOffset finish,string project,FollowRSmokeEvidence evidence)
        {
            var c=new StreetProbeCheck {id=RequiredIds[4],reason="UNKNOWN_NATIVE_CONSOLE_WINDOW"};
            try
            {
                JObject Result(JObject x){Need((bool?)x?["success"]==true&&(string)x["data"]?["command"]=="console"&&StreetPayloadContract.SamePath((string)x["data"]?["target"]?["projectPath"],project),"Real official Console result/project required.");return (JObject)x["data"]["result"];}
                var b=Result(before);var a=Result(after);long lo=(long)b["cursor"],hi=(long)a["cursor"];
                evidence.consoleBeforeCursor=lo;evidence.consoleAfterCursor=hi;evidence.consoleSession=(string)b["session"];
                Need(!string.IsNullOrWhiteSpace(evidence.consoleSession)&&(string)a["session"]==evidence.consoleSession&&lo>=0&&hi>=lo&&
                    (bool?)a["dropped"]==false&&(bool?)a["reset"]==false&&(long?)after["data"]?["parameters"]?["since"]==lo&&
                    StreetPayloadContract.Instant((string)b["groundTruth"]?["sampledUtc"])<=start&&StreetPayloadContract.Instant((string)a["groundTruth"]?["sampledUtc"])>=finish,"Current uninterrupted Console cursor/session/window required.");
                var entries=(JArray)a["entries"];Need(entries!=null&&(int?)a["returned"]==entries.Count&&entries.Count==hi-lo,"Console history gap retained; never infer zero.");
                for(int i=0;i<entries.Count;i++)Need((long?)entries[i]["seq"]==lo+i+1&&(bool?)entries[i]["seeded"]==false,"Console new sequence gap/seed refused.");
                int errors=entries.Count(x=>new[]{"Error","Exception","Assert"}.Contains((string)x["logType"])||string.Equals((string)x["level"],"error",StringComparison.OrdinalIgnoreCase));
                Need(entries.All(x=>new[]{"Log","Warning","Error","Exception","Assert"}.Contains((string)x["logType"])),"Unknown Console severity.");
                evidence.consoleNewErrors=errors;evidence.consoleReason=errors==0?"COMPLETE_NATIVE_CURSOR_WINDOW":"NEW_NATIVE_CONSOLE_ERROR";
                c.observed=true;c.passed=errors==0;c.actual=new double[]{errors,lo,hi};c.expected=new double[]{0};c.reason=evidence.consoleReason;
            }
            catch(Exception ex){evidence.consoleReason=ex.Message;c.reason="UNKNOWN_NATIVE_CONSOLE_WINDOW:"+ex.Message;}
            return c;
        }
    }
    public sealed class StreetSmokeSequence
    {
        public const string Suite="SMK-011";
        public static readonly string[] RequiredIds={"smk.streetSpawn","smk.fourDirections","smk.completed","smk.consoleNoNewErrors","smk.stoppedLeaseReleased"};
        public static readonly string[] DirectionIds={"x-positive","x-negative","y-positive","y-negative"};
        private static readonly Vector2[] Inputs={new Vector2(1,0),new Vector2(-1,0),new Vector2(0,1),new Vector2(0,-1)};
        private readonly StreetProbeRequest request;
        private readonly double started;
        private double directionStarted;
        private Vector3 directionStart;
        private int index,previousFrame=-1;
        private readonly StreetProbeCheck[] directions=DirectionIds.Select(id=>new StreetProbeCheck {id=id,reason="NOT_RUN"}).ToArray();
        private readonly StreetProbeObservation initial;
        private readonly Vector3 spawn;
        private readonly bool spawnPassed;
        public bool Finished {get;private set;}
        public string Error {get;private set;}
        public bool Paused {get;private set;}
        private double pausedAt;
        public Vector2 Input=>Finished||Paused?Vector2.zero:Inputs[index];
        public StreetSmokeSequence(StreetProbeRequest r,double now,StreetProbeObservation first,Vector3 expectedSpawn,bool streetLoaded)
        {
            if(r==null||r.schemaVersion!=1||r.suite!=Suite||r.stages!=null&&r.stages.Length!=0||!LongLoopProbeChecks.Finite(r.maximumSeconds)||r.maximumSeconds<=0||r.maximumSeconds>8)
                throw new InvalidOperationException("SMK-011 requires its explicit bounded request, no full stages or capture plan.");
            request=r;started=directionStarted=now;initial=first;spawn=expectedSpawn;
            if(first==null||!first.ready){Cancel("MISSING_PLAYER_OR_NOT_READY");return;}
            directionStart=first.foot;previousFrame=first.frame;
            spawnPassed=streetLoaded&&first.inside&&LongLoopProbeChecks.Finite(first.foot)&&LongLoopProbeChecks.Finite(spawn)&&(first.foot-spawn).magnitude<=.05f;
            if(!spawnPassed)Cancel("STREET_SPAWN_OR_WALK_AREA");
        }
        public void Cancel(string reason){if(!Finished){Error=reason;Finished=true;}}
        public void CheckDeadline(double now){if(!Finished&&(double.IsNaN(now)||double.IsInfinity(now)||now<started||now-started>=request.maximumSeconds))Cancel("SMK_TIMEOUT_OR_CLOCK");}
        public void Pause(double now){CheckDeadline(now);if(!Finished&&!Paused){Paused=true;pausedAt=now;}}
        public void Resume(double now){CheckDeadline(now);if(!Finished&&Paused){directionStarted+=now-pausedAt;Paused=false;}}
        public Vector2 Tick(StreetProbeObservation sample,double now,Action<StreetProbeObservation> persist)
        {
            CheckDeadline(now);if(Finished||Paused)return Vector2.zero;
            if(sample==null){Cancel("MISSING_OBSERVATION");return Vector2.zero;}
            if(sample.frame==previousFrame)return Input;
            previousFrame=sample.frame;sample.phaseIndex=index;sample.phaseId=DirectionIds[index];sample.elapsed=now-started;persist(sample);
            if(!sample.ready||!sample.inputOwned||!sample.inside||!LongLoopProbeChecks.Finite(sample.foot)){Cancel("SMK_NOT_READY_OWNED_OR_INSIDE:"+DirectionIds[index]);return Vector2.zero;}
            if(now-directionStarted<.3)return Input;
            var delta=sample.foot-directionStart;var input=Inputs[index];var signed=delta.x*input.x+delta.y*input.y;
            directions[index]=new StreetProbeCheck {id=DirectionIds[index],observed=true,passed=signed>.0001f,actual=new double[]{delta.x,delta.y},expected=new double[]{input.x,input.y},observedSample=sample,reason=signed>.0001f?null:"ZERO_OR_WRONG_DIRECTION"};
            if(!directions[index].passed){Cancel("SMK_DIRECTION:"+DirectionIds[index]);return Vector2.zero;}
            index++;if(index==4){Finished=true;return Vector2.zero;}
            directionStart=sample.foot;directionStarted=now;return Input;
        }
        public StreetProbeReport Report(StreetProbeCleanup cleanup)
        {
            var complete=Finished&&Error==null&&cleanup.Passed;
            return new StreetProbeReport {suite=Suite,taskId=request.taskId,attempt=request.attempt,executionId=request.executionId,ownerThreadId=request.ownerThreadId,runId=request.runId,
                projectPath=request.projectPath,scenePath=request.scenePath,outputRoot=request.outputRoot,editorPid=request.editorPid,editorStartedAt=request.editorStartedAt,requiredCount=5,
                status=!Finished?"RUNNING":complete?"COMPLETED":"FAILED",error=Error,passed=false,cleanup=cleanup,directions=directions,
                checks=new[]{new StreetProbeCheck {id=RequiredIds[0],observed=initial!=null,passed=spawnPassed,observedSample=initial,actual=initial==null?null:new double[]{initial.foot.x,initial.foot.y,initial.foot.z},expected=new double[]{spawn.x,spawn.y,spawn.z}},
                    new StreetProbeCheck {id=RequiredIds[1],observed=directions.All(d=>d.observed),passed=directions.All(d=>d.observed&&d.passed),reason=string.Join(";",directions.Select(d=>d.id+":"+(!d.observed?"NOT_RUN":d.passed?"PASS":"FAIL")))},
                    new StreetProbeCheck {id=RequiredIds[2],observed=Finished,passed=complete,reason=Error},
                    new StreetProbeCheck {id=RequiredIds[3],observed=false,passed=false,reason="NOT_RUN_ENGINE_NATIVE_CONSOLE"},
                    new StreetProbeCheck {id=RequiredIds[4],observed=false,passed=false,reason="NOT_RUN_ENGINE_NATIVE_STOP_AND_LEASE"}}};
        }
        public static StreetProbeCleanup Cleanup(bool ownsInput,Action restoreInput,Action detach)
        {
            var c=new StreetProbeCleanup {configsRestored=true,configOwnerRestored=true,temporaryObjectsDestroyed=true,noForeignOverwrite=ownsInput};
            try{if(ownsInput){restoreInput();c.inputRestored=true;}else c.error="Foreign input/avatar retained.";}
            catch(Exception ex){c.error=ex.ToString();}
            finally{try{detach();c.callbacksRemoved=true;}catch(Exception ex){c.error=(c.error??"")+ex;}}
            return c;
        }
    }

    // A single immutable elapsed clock. The native adapter supplies natural observations and applies only returned input.
    public sealed class StreetProbeSequence
    {
        public readonly StreetProbeRequest Request;
        public readonly List<StreetProbeObservation> Samples=new List<StreetProbeObservation>();
        public int StageIndex { get; private set; }
        public bool Finished { get; private set; }
        public string Error { get; private set; }
        public double StartedAt { get; }
        private double stageStarted, settledAt=-1, lastSaved=-1;
        public double StageStartedAt=>stageStarted;
        public double StageSecondsRemaining(double now)=>Request.stages[StageIndex].timeoutSeconds-(now-stageStarted);
        private bool captureAcknowledged;
        public string PendingCapture { get; private set; }
        private int previousFrame=-1;
        public StreetProbeSequence(StreetProbeRequest request,double now)
        {LongLoopProbeChecks.ValidatePlan(request);Request=request;StartedAt=stageStarted=now;}
        public void Cancel(string reason) {if(!Finished){Error=reason;Finished=true;}}
        public void AcknowledgeCapture(string id,double now)
        {CheckDeadline(now);if(Finished||PendingCapture!=id||id!="sorting"||captureAcknowledged)throw new InvalidOperationException("No matching live pending capture.");captureAcknowledged=true;PendingCapture=null;}
        public void CheckDeadline(double now)
        {
            if(Finished)return;
            if(double.IsNaN(now)||double.IsInfinity(now)||now<StartedAt||now-StartedAt>Request.maximumSeconds)Cancel("TOTAL_TIMEOUT_OR_CLOCK");
            else if(now-stageStarted>Request.stages[StageIndex].timeoutSeconds)Cancel("STAGE_TIMEOUT:"+Request.stages[StageIndex].id);
        }
        public Vector2 Tick(StreetProbeObservation observation,double now,Action<StreetProbeObservation> persistBeforeAssertion)
        {
            CheckDeadline(now);if(Finished)return Vector2.zero;
            var stage=Request.stages[StageIndex];
            if(observation==null){Cancel("MISSING_OBSERVATION");return Vector2.zero;}
            observation.phaseIndex=StageIndex;observation.phaseId=stage.id;observation.elapsed=now-StartedAt;
            if(!observation.ready && StageIndex==0)return Vector2.zero; // Bounded natural readiness, not skipped coverage.
            if(observation.frame<=previousFrame)return Vector2.zero;
            previousFrame=observation.frame;
            var toTarget=stage.destination-observation.foot;
            observation.settled=!stage.navigate || Math.Sqrt((double)toTarget.x*toTarget.x+(double)toTarget.y*toTarget.y)<=.04;
            bool fatal=!LongLoopProbeChecks.FiniteObservation(observation)||!observation.ready||!observation.inputOwned||!observation.configsOwned;
            if(fatal || lastSaved<0 || now-lastSaved>=.125)
            {
                if(Samples.Count>=1200){Cancel("SAMPLE_LIMIT");return Vector2.zero;}
                persistBeforeAssertion(observation);Samples.Add(observation);lastSaved=now;
            }
            if(fatal){Cancel("NONFINITE_NOT_READY_OR_FOREIGN_OWNERSHIP");return Vector2.zero;}
            Vector2 next=stage.input;
            if(stage.navigate)
            {
                var delta=stage.destination-observation.foot;
                if(Math.Sqrt((double)delta.x*delta.x+(double)delta.y*delta.y)>.04)
                {
                    settledAt=-1;
                    float horizon=Math.Max(.02f,observation.deltaTime)*2;
                    next=new Vector2(Math.Max(-1,Math.Min(1,delta.x/(observation.speed*horizon))),
                        Math.Max(-1,Math.Min(1,delta.y/(observation.speed*observation.verticalFactor*horizon))));
                }
                else {next=Vector2.zero;if(settledAt<0)settledAt=now;}
            }
            double phaseElapsed=stage.navigate?(settledAt<0?0:now-settledAt):now-stageStarted;
            if(phaseElapsed>=stage.seconds)
            {
                if(!string.IsNullOrEmpty(stage.captureId)&&!captureAcknowledged){PendingCapture=stage.captureId;return Vector2.zero;}
                StageIndex++;stageStarted=now;settledAt=-1;
                if(StageIndex==Request.stages.Length){Finished=true;next=Vector2.zero;}
                else next=Request.stages[StageIndex].navigate?Vector2.zero:Request.stages[StageIndex].input;
            }
            return next;
        }
    }
}
