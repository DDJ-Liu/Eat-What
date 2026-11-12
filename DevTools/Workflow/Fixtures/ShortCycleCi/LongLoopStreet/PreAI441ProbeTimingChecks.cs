using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace EatWhat.PreAI441TimingValidation
{
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
        public bool confinerClearForCameraTests;
        public float precision, depthOffset, ahead, stopped, delay, recenterSpeed, lookahead, lookaheadTime, cap;
        public float size, f01Size, aspect, dampingError, aheadError, delayGoalError, recenterSpeedObserved;
        public ulong sequence, bodySequence;
        public int channelAdvances, bodyNotifications;
        public bool ready, inputOwned, configsOwned, inside, boundaryBlocked, facingRight, moving, animationWalk, animationIdle;
        public bool sorterTargetMatches, markersReady, channelValid, channelZero, baselineOutside, observerSameFrame, dampingMeasured, settled;
        public string movementPath, phaseId;
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
            int f01=0,f02=0,confiner=0;
            foreach(var d in drivers??Array.Empty<StreetProbeDriver>())
            {
                if(d==null)throw new InvalidOperationException("Missing camera driver identity.");
                if(!d.enabled)continue;
                if(!d.sameCamera)throw new InvalidOperationException("Foreign camera driver owner.");
                if(d.kind=="F01")f01++;
                else if(d.kind=="F02")f02++;
                else if(d.kind=="CinemachineConfiner2D"&&d.sameBounds)confiner++;
                else throw new InvalidOperationException("Unknown driver or mismatched Confiner bounds.");
            }
            if(f01!=1||f02!=1||confiner!=1)throw new InvalidOperationException("Exactly one active F01/F02/current Confiner required.");
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
        public const string CapabilityVersion="AI436-sorting24-clock-ack-v1";
        public static float StageTimeoutLimit(StreetProbeStage stage)=>stage!=null&&stage.id=="depth-mid"&&stage.captureId=="sorting"?24:12;
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
