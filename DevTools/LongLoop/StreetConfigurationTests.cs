using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using EatWhat.EditorValidation;
internal static class StreetConfigurationTests
{
    internal sealed class Case
    {
        public string name,mode,reviewJson,originalText;
        public StreetConfigurationIdentity identity;
        public bool expected;
    }
    internal static StreetConfigurationIdentity Identity()=>new StreetConfigurationIdentity {taskId="AI-000999",attempt=1,executionId="11111111111111111111111111111111",ownerThreadId="owner-fixture",runId="preset-case",projectPath="D:/fixture",scopeSha256=new string('A',64),geometrySha256=new string('B',64),controllerThreadId="controller-fixture"};
    internal static JObject Original()=>new JObject { ["schemaVersion"]=1,["source"]="direct-human-user-message-in-current-CONTROL-chat",["productVersion"]="demo-0.1.1",
        ["verbatimRequest"]="FIXTURE_ONLY 先按照你的预设来设置后持续推进；记录待人工后续调整。",["authorization"]="Use reviewed presets for configuration, record pending human tuning and continue; fixture only",
        ["doesNotClaimHumanTuningDone"]=true,["finalHumanVisualAcceptance"]="PENDING"};
    internal static JObject Preset(StreetConfigurationIdentity id)=>new JObject { ["schemaVersion"]=1,["kind"]="CONTROL_PRESET_REVIEW",["source"]="control-reviewed-preset",["decision"]="APPROVED_FOR_PRESET_VALIDATION",["controllerThreadId"]=id.controllerThreadId,
        ["taskId"]=id.taskId,["attempt"]=id.attempt,["executionId"]=id.executionId,["ownerThreadId"]=id.ownerThreadId,["runId"]=id.runId,["projectPath"]=id.projectPath,["scopeSha256"]=id.scopeSha256,["geometrySha256"]=id.geometrySha256,
        ["humanTuningStatus"]="DEFERRED_NONBLOCKING",["humanTuningCompleted"]=false,["finalVisualAccepted"]=false,["originalUserAuthorizationPath"]="D:/fixture/original.json",["originalUserAuthorizationSha256"]=new string('C',64)};
    public static List<Case> Cases()
    {
        var result=new List<Case>();var id=Identity();
        void Add(string name,string mode,JObject review,JObject original,bool expected)=>result.Add(new Case {name=name,mode=mode,reviewJson=review.ToString(Formatting.None),originalText=original.ToString(Formatting.None),identity=id,expected=expected});
        Add("preset-valid","USER_AUTHORIZED_PRESET",Preset(id),Original(),true);
        foreach(var key in new[]{"controllerThreadId","taskId","executionId","ownerThreadId","runId","projectPath","scopeSha256","geometrySha256","kind","source","decision"})
        {var review=Preset(id);review[key]="wrong";Add("preset-wrong-"+key,"USER_AUTHORIZED_PRESET",review,Original(),false);}
        foreach(var key in new[]{"humanTuningCompleted","finalVisualAccepted"}){var review=Preset(id);review[key]=true;Add("preset-false-human-"+key,"USER_AUTHORIZED_PRESET",review,Original(),false);
            review=Preset(id);review[key]="false";Add("preset-string-bool-"+key,"USER_AUTHORIZED_PRESET",review,Original(),false);}
        foreach(var key in new[]{"humanTuningStatus","originalUserAuthorizationPath","originalUserAuthorizationSha256"}){var review=Preset(id);review.Remove(key);Add("preset-missing-"+key,"USER_AUTHORIZED_PRESET",review,Original(),false);}
        var bad=Preset(id);bad["attempt"]="1";Add("preset-string-attempt","USER_AUTHORIZED_PRESET",bad,Original(),false);
        foreach(var key in new[]{"source","productVersion","authorization","verbatimRequest","finalHumanVisualAcceptance"}){var original=Original();original[key]="missing";Add("original-no-"+key,"USER_AUTHORIZED_PRESET",Preset(id),original,false);}
        var orig=Original();orig["doesNotClaimHumanTuningDone"]="true";Add("original-fake-bool","USER_AUTHORIZED_PRESET",Preset(id),orig,false);
        Add("unknown-mode","OTHER",Preset(id),Original(),false);
        var usera=new JObject { ["schemaVersion"]=1,["kind"]="USERa",["source"]="direct-human-user",["decision"]="APPROVED",["geometrySha256"]=id.geometrySha256,["userReplyEvidencePath"]="D:/fixture/human.txt",["userReplyEvidenceSha256"]=new string('D',64)};
        result.Add(new Case {name="usera-valid",mode="USER_ADJUSTED",reviewJson=usera.ToString(Formatting.None),originalText="FIXTURE_ONLY actual human reply boundary",identity=id,expected=true});
        var wrong=(JObject)usera.DeepClone();wrong["geometrySha256"]="wrong";result.Add(new Case {name="usera-wrong-geometry",mode="USER_ADJUSTED",reviewJson=wrong.ToString(Formatting.None),originalText="reply",identity=id,expected=false});
        result.Add(new Case {name="usera-missing-original",mode="USER_ADJUSTED",reviewJson=usera.ToString(Formatting.None),originalText="",identity=id,expected=false});
        return result;
    }
    public static string CasesJson()=>JsonConvert.SerializeObject(Cases(),Formatting.Indented);
    public static int Run()
    {
        int count=0;void Check(bool ok,string label){count++;if(!ok)throw new Exception(label);}
        foreach(var c in Cases())
        {
            bool passed;StreetConfigurationDecision decision=null;
            try{decision=StreetConfigurationAuthorization.Validate(c.mode,c.reviewJson,c.originalText,c.identity);passed=true;}catch(Exception){passed=false;}
            Check(passed==c.expected,"Actual native configuration source: "+c.name);
            if(passed)Check(!decision.finalVisualAccepted&&(c.mode=="USER_ADJUSTED"?decision.humanTuningCompleted:!decision.humanTuningCompleted&&decision.humanTuningStatus=="DEFERRED_NONBLOCKING"),"Source never forges final visual or deferred tuning");
        }
        StreetProbeDriver[] Drivers()=>new[]{new StreetProbeDriver {kind="F01",enabled=true,sameCamera=true},new StreetProbeDriver {kind="F02",enabled=true,sameCamera=true},new StreetProbeDriver {kind="CinemachineConfiner2D",enabled=true,sameCamera=true,sameBounds=true}};
        StreetProbeDriverPolicy.Validate(Drivers());Check(true,"Current original enabled Confiner accepted by exact policy");
        for(int variant=0;variant<5;variant++)
        {
            var d=Drivers();if(variant==0)d[2].kind="UNKNOWN";if(variant==1)d[2].sameCamera=false;if(variant==2)d[2].sameBounds=false;if(variant==3)d=d.Concat(new[]{d[2]}).ToArray();if(variant==4)d[2].enabled=false;
            bool failed=false;try{StreetProbeDriverPolicy.Validate(d);}catch(InvalidOperationException){failed=true;}Check(failed,"Unknown/foreign/wrong bounds/duplicate/disabled Confiner rejects "+variant);
        }
        Check(LegacyStreetProbeDriverGuard.RejectsCurrentConfiner(),"Actual readonly old guard rejects saved enabled Confiner: old-source counterexample");
        return count;
    }
}
