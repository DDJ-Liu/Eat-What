using System;
using System.Linq;
using EatWhat.EditorValidation;
using UnityEngine;
internal static class StreetProbeTimingTests
{
    public static int Run()
    {
        int count=0;void Check(bool ok,string label){count++;if(!ok)throw new Exception(label);}
        void Reject(Action a,string label){bool bad=false;try{a();}catch(InvalidOperationException){bad=true;}Check(bad,label);}
        StreetProbeRequest Plan(){var p=StreetProbeChecksTests.Plan();p.stages.Single(s=>s.id=="depth-mid").timeoutSeconds=45;foreach(var s in p.stages.Where(s=>s.id!="depth-mid"))s.timeoutSeconds=new[]{"edge-approach","camera-settle","stopped-recenter"}.Contains(s.id)?6:3;return p;}
        var plan=Plan();LongLoopProbeChecks.ValidatePlan(plan);Check(plan.stages.Sum(s=>s.timeoutSeconds)+2<=120,"Only sorting45 accepted with same total clock");
        var old=new EatWhat.LegacyTimingValidation.StreetProbeRequest {maximumSeconds=plan.maximumSeconds,expectedMoveSpeed=plan.expectedMoveSpeed,expectedVerticalFactor=plan.expectedVerticalFactor,focusDelta=plan.focusDelta,edgeTangent=plan.edgeTangent,
            stages=plan.stages.Select(s=>new EatWhat.LegacyTimingValidation.StreetProbeStage {id=s.id,captureId=s.captureId,input=s.input,navigate=s.navigate,destination=s.destination,seconds=s.seconds,timeoutSeconds=s.timeoutSeconds}).ToArray()};
        Reject(()=>EatWhat.LegacyTimingValidation.LongLoopProbeChecks.ValidatePlan(old),"Actual readonly old source rejects sorting45");
        foreach(string id in new[]{"ready","x-positive","depth-low","depth-high","restore"}){var p=Plan();p.stages.Single(s=>s.id==id).timeoutSeconds=13;Reject(()=>LongLoopProbeChecks.ValidatePlan(p),"Ordinary stage cannot exceed12: "+id);}
        var preceding=new EatWhat.PreAI441TimingValidation.StreetProbeRequest {maximumSeconds=plan.maximumSeconds,expectedMoveSpeed=plan.expectedMoveSpeed,expectedVerticalFactor=plan.expectedVerticalFactor,focusDelta=plan.focusDelta,edgeTangent=plan.edgeTangent,stages=plan.stages.Select(s=>new EatWhat.PreAI441TimingValidation.StreetProbeStage {id=s.id,captureId=s.captureId,input=s.input,navigate=s.navigate,destination=s.destination,seconds=s.seconds,timeoutSeconds=s.timeoutSeconds}).ToArray()};Reject(()=>EatWhat.PreAI441TimingValidation.LongLoopProbeChecks.ValidatePlan(preceding),"Actual unchanged predecessor rejects sorting45");Check(plan.stages.Sum(s=>s.timeoutSeconds)==117,"18short3 plus3six plus45 equals117");
        var bad=Plan();bad.stages[11].timeoutSeconds=45.01f;Reject(()=>LongLoopProbeChecks.ValidatePlan(bad),"Sorting above45 rejects");
        bad=Plan();bad.stages[11].captureId="wrong";Reject(()=>LongLoopProbeChecks.ValidatePlan(bad),"Wrong capture does not gain45");
        bad=Plan();bad.stages[10].captureId="sorting";Reject(()=>LongLoopProbeChecks.ValidatePlan(bad),"Other stage cannot gain capture exception");
        bad=Plan();bad.maximumSeconds=109;Reject(()=>LongLoopProbeChecks.ValidatePlan(bad),"Stage sum plus cleanup remains bounded");
        bad=Plan();bad.maximumSeconds=120.01f;Reject(()=>LongLoopProbeChecks.ValidatePlan(bad),"Global120 unchanged");
        StreetProbeSequence Pending(out double now,out int frame)
        {
            var p=Plan();var seq=new StreetProbeSequence(p,0);now=0;frame=0;int last=0,local=0;
            while(seq.PendingCapture==null&&!seq.Finished){if(last!=seq.StageIndex){last=seq.StageIndex;local=0;}
                seq.Tick(StreetProbeFixtureData.Boundary(p,seq,++frame,local++,now),now,s=>{});now+=.125;}
            if(seq.Finished)throw new Exception("Could not reach actual sorting gate: "+seq.Error);return seq;
        }
        var sequence=Pending(out double t,out int f);double originalStart=sequence.StartedAt,stageStart=sequence.StageStartedAt;int observed=sequence.Samples.Count;
        // Three slow managed-call boundaries. Natural samples and the one production clock continue throughout.
        for(int role=0;role<3;role++)for(int step=0;step<48;step++,t+=.125)
            sequence.Tick(StreetProbeFixtureData.Boundary(sequence.Request,sequence,++f,0,t),t,s=>{});
        Check(!sequence.Finished&&sequence.PendingCapture=="sorting"&&sequence.StageIndex==11,"6s Read +6s capture +6s ACK latency fits only live45 gate");
        Check(sequence.StartedAt==originalStart&&sequence.StageStartedAt==stageStart&&sequence.Samples.Count>=observed+140,"No clock reset/pause; natural capture-hold samples saved");
        sequence.AcknowledgeCapture("sorting",t);Reject(()=>sequence.AcknowledgeCapture("sorting",t),"Duplicate ACK rejected");
        sequence.Tick(StreetProbeFixtureData.Boundary(sequence.Request,sequence,++f,0,t),t,s=>{});Check(sequence.StageIndex==12&&sequence.StartedAt==originalStart,"ACK advances same original sequence");
        sequence=Pending(out t,out f);double late=sequence.StageStartedAt+45.001;
        Reject(()=>sequence.AcknowledgeCapture("sorting",late),"Late ACK rejected even without another render tick");Check(sequence.Finished&&sequence.Error=="STAGE_TIMEOUT:depth-mid","Late ACK terminalizes actual stage deadline");
        sequence=Pending(out t,out f);sequence.CheckDeadline(sequence.StageStartedAt+45.01);Check(sequence.Finished,"Editor watch can expire waiting stage without render tick");
        sequence=Pending(out t,out f);sequence.CheckDeadline(sequence.StartedAt+120.01);Check(sequence.Error=="TOTAL_TIMEOUT_OR_CLOCK","Capture never extends global clock");
        sequence=new StreetProbeSequence(Plan(),0);Reject(()=>sequence.AcknowledgeCapture("sorting",.1),"Early ACK rejected");
        return count;
    }
}
