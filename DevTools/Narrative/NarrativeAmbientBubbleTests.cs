using System;
using System.Collections.Generic;
using EatWhat.Narrative.Core;

public static class NarrativeAmbientBubbleTests
{
    static int checks;
    static void Check(bool value,string label) { checks++; if(!value) throw new Exception("Ambient: "+label); }
    static NarrativeCommandSource Commands(string text="") { return new NarrativeCommandSource(NarrativeCommandParser.Parse(text)); }
    static NarrativeDialogue Dialogue(string id,string first,NarrativeDialogueLevel level=NarrativeDialogueLevel.Ambient,string onEnd="")
    { return new NarrativeDialogue(id,level,null,new NarrativeProfileNames("","","","","","","","",""),"",first,"",Commands(onEnd)); }
    static NarrativeDialogueLine Line(string id,string owner,string next="",string speaker="",string command="")
    { return new NarrativeDialogueLine(id,owner,next,speaker,"","text."+id,Commands(command),"","","",""); }
    static INarrativeDataDirectory Directory()
    {
        return new NarrativeDataDirectory(dialogues:new[]{Dialogue("dlg_a","ln_a1"),Dialogue("dlg_b","ln_b1"),Dialogue("dlg_n","ln_n",NarrativeDialogueLevel.Normal),Dialogue("dlg_bad","missing"),Dialogue("dlg_cycle","ln_cycle"),Dialogue("dlg_cmd","ln_cmd",onEnd:"wait(1)"),Dialogue("dlg_label","ln_label")},
            lines:new[]{Line("ln_a1","dlg_a","ln_a2"),Line("ln_a2","dlg_a",speaker:"chr_other#2"),Line("ln_b1","dlg_b"),Line("ln_n","dlg_n"),Line("ln_cycle","dlg_cycle","ln_cycle"),Line("ln_cmd","dlg_cmd",command:"wait(1);unknown()"),Line("ln_label","different_label")},
            pools:new[]{new NarrativeAmbientPool("pool_ab","",new[]{"dlg_a","dlg_b"}),new NarrativeAmbientPool("pool_a","",new[]{"dlg_a"}),new NarrativeAmbientPool("pool_b","",new[]{"dlg_b"}),new NarrativeAmbientPool("pool_empty","",new string[0]),new NarrativeAmbientPool("pool_missing","",new[]{"dlg_a","absent"}),new NarrativeAmbientPool("pool_normal","",new[]{"dlg_a","dlg_n"}),new NarrativeAmbientPool("pool_bad","",new[]{"dlg_bad"}),new NarrativeAmbientPool("pool_cycle","",new[]{"dlg_cycle"}),new NarrativeAmbientPool("pool_cmd","",new[]{"dlg_cmd"}),new NarrativeAmbientPool("pool_dup","",new[]{"dlg_a","dlg_a"}),new NarrativeAmbientPool("pool_label","",new[]{"dlg_label"})});
    }
    static DialogueGroupSnapshot Trigger(float cooldown=2,float radius=3,float interact=1000)
    {
        var config=new DialoguePresentationData();config.trigger[0].autoBubbleCooldown=cooldown;
        config.trigger[0].autoBubbleRadius=radius;config.trigger[0].interactRadius=interact;
        return DialoguePresentationResolver.ResolveTrigger(config,null).Snapshot;
    }
    public static string Run()
    {
        checks=0;var directory=Directory();var trigger=Trigger();var model=new NarrativeAmbientBubbleModel("chr_npc#2",directory);model.ChangePool("pool_ab");
        NarrativeAmbientBubbleEmission bubble=null;int randomCalls=0;var ranges=new List<int>();
        Func<int,int> choose=n=>{randomCalls++;ranges.Add(n);return 0;};
        Check(!model.TryTrigger(0,4,trigger,choose,out bubble)&&bubble==null,"outside radius");
        Check(!model.TryTrigger(0,3,trigger,choose,out bubble),"strict radius equality excluded");
        Check(randomCalls==0&&!model.CooldownUntilSeconds.HasValue,"range rejection consumes neither randomness nor cooldown");
        Check(model.TryTrigger(0,2.999,trigger,choose,out bubble)&&bubble.LineId=="ln_a1","inside selects first linked line");
        Check(bubble.TextKey=="text.ln_a1"&&bubble.SpeakerId=="chr_npc#2"&&bubble.NpcId=="chr_npc#2","empty speaker uses exact owner instance id");
        Check(bubble.NextAllowedAtSeconds==2&&bubble.TriggeredAtSeconds==0&&ranges[0]==2,"success starts full cooldown with external candidate range");
        Check(!model.TryTrigger(1.999,0,trigger,choose,out bubble)&&bubble==null,"just before cooldown boundary");
        Check(randomCalls==1,"cooling does not pick or advance");
        Check(model.TryTrigger(2,0,trigger,null,out bubble)&&bubble.LineId=="ln_a2"&&bubble.SpeakerId=="chr_other#2","exact boundary follows chain and explicit speaker, no random for next line");
        Check(model.TryTrigger(4,0,trigger,choose,out bubble)&&bubble.DialogueId=="dlg_b"&&bubble.LineId=="ln_b1","finished segment changes, never repeats among multiple segments");
        Check(ranges[1]==1,"previous segment excluded from random range");
        Check(model.TryTrigger(6,0,trigger,choose,out bubble)&&bubble.LineId=="ln_a1","remaining inside repeats only after full cooldown");
        Check(!model.TryTrigger(5,0,trigger,choose,out bubble),"older clock input does not bypass existing deadline");
        var small=new NarrativeAmbientBubbleModel("chr_small",directory);small.ChangePool("pool_b");
        Check(!small.TryTrigger(0,2,Trigger(radius:1,interact:1000),choose,out bubble),"interactRadius cannot enable bubbles outside autoBubbleRadius");
        Check(small.TryTrigger(0,2,Trigger(radius:3,interact:0),choose,out bubble),"interactRadius zero does not disable bubble range");
        var single=new NarrativeAmbientBubbleModel("chr_single",directory);single.ChangePool("pool_a");
        Check(single.TryTrigger(0,0,Trigger(0),choose,out bubble)&&bubble.LineId=="ln_a1","zero cooldown one result per call");
        Check(single.TryTrigger(0,0,Trigger(0),null,out bubble)&&bubble.LineId=="ln_a2","single segment line order");
        Check(single.TryTrigger(0,0,Trigger(0),choose,out bubble)&&bubble.LineId=="ln_a1","single segment may repeat");
        var swap=new NarrativeAmbientBubbleModel("chr_swap",directory);swap.ChangePool("pool_a");
        Check(swap.TryTrigger(10,0,trigger,choose,out bubble)&&bubble.LineId=="ln_a1","pool change setup");
        swap.ChangePool("pool_b");Check(swap.CooldownUntilSeconds==12,"pool change preserves exact prior deadline");
        Check(!swap.TryTrigger(11,0,trigger,choose,out bubble),"pool change cannot bypass cooldown");
        Check(swap.TryTrigger(12,0,trigger,choose,out bubble)&&bubble.LineId=="ln_b1","new pool starts its first line at old deadline");
        swap.ChangePool("pool_a");Check(swap.TryTrigger(14,0,trigger,choose,out bubble)&&bubble.LineId=="ln_a1","switch back clears previous pool progress");
        swap.ChangePool("pool_a");Check(swap.TryTrigger(16,0,trigger,choose,out bubble)&&bubble.LineId=="ln_a2","same id application preserves progress");
        var independent=new NarrativeAmbientBubbleModel("chr_independent",directory);independent.ChangePool("pool_a");
        Check(independent.TryTrigger(0,0,trigger,choose,out bubble)&&bubble.LineId=="ln_a1","another NPC has independent cooldown and progress");
        foreach(string bad in new[]{"pool_empty","pool_missing","pool_normal","pool_bad","pool_cycle","not_registered"})
        {
            var reject=new NarrativeAmbientBubbleModel("chr_bad",directory);reject.ChangePool(bad);int calls=0;
            Func<int,int> never=n=>{calls++;return 0;};
            Check(!reject.TryTrigger(0,0,trigger,never,out bubble)&&bubble==null,"bad pool rejects "+bad);
            Check(reject.Diagnostics.Count>=1&&calls==0&&!reject.CooldownUntilSeconds.HasValue,"bad pool no random/cooldown "+bad);
            int before=reject.Diagnostics.Count;
            for(int i=1;i<=4;i++) Check(!reject.TryTrigger(i,0,trigger,never,out bubble),"bad pool remains rejected");
            Check(reject.Diagnostics.Count==before,"pool diagnostics de-duplicated "+bad);
        }
        var commands=new NarrativeAmbientBubbleModel("chr_commands",directory);commands.ChangePool("pool_cmd");
        Check(commands.TryTrigger(0,0,Trigger(0),choose,out bubble)&&bubble.TextKey=="text.ln_cmd","invalid/unhandled commands never suppress bubble");
        Check(commands.Diagnostics.Count==2&&commands.Diagnostics[0].Code=="COMMANDS_IGNORED"&&commands.Diagnostics[1].Code=="ON_END_IGNORED","line and on_end ignored with explicit diagnostics");
        Check(commands.TryTrigger(1,0,Trigger(0),choose,out bubble)&&commands.Diagnostics.Count==2,"command warnings do not repeat");
        var duplicate=new NarrativeAmbientBubbleModel("chr_duplicate",directory);duplicate.ChangePool("pool_dup");int duplicateRange=0;
        Func<int,int> duplicateChoose=n=>{duplicateRange=n;return 0;};
        Check(duplicate.TryTrigger(0,0,Trigger(0),duplicateChoose,out bubble)&&duplicateRange==1,"repeated id is one distinct segment");
        duplicate.TryTrigger(0,0,Trigger(0),null,out bubble);
        Check(duplicate.TryTrigger(0,0,Trigger(0),duplicateChoose,out bubble)&&bubble.LineId=="ln_a1","duplicate reference does not deadlock nonrepeat rule");
        var labels=new NarrativeAmbientBubbleModel("chr_labels",directory);labels.ChangePool("pool_label");
        Check(labels.TryTrigger(0,0,trigger,choose,out bubble)&&bubble.LineId=="ln_label"&&labels.Diagnostics.Count==1,"actual chain label warning preserved but not promoted to error");
        var invalid=new NarrativeAmbientBubbleModel("chr_invalid",directory);invalid.ChangePool("pool_ab");
        Check(!invalid.TryTrigger(double.NaN,0,trigger,choose,out bubble),"nonfinite time rejected");
        Check(!invalid.TryTrigger(0,double.PositiveInfinity,trigger,choose,out bubble),"nonfinite distance rejected");
        Check(!invalid.TryTrigger(0,-1,trigger,choose,out bubble),"negative distance rejected");
        Check(!invalid.TryTrigger(0,0,null,choose,out bubble),"no invented trigger settings");
        Check(!invalid.TryTrigger(0,0,trigger,n=>n,out bubble)&&!invalid.CooldownUntilSeconds.HasValue,"invalid random consumes no progress/cooldown");
        Check(invalid.TryTrigger(0,0,trigger,choose,out bubble)&&bubble.LineId=="ln_a1","valid retry starts first line");
        var changedSettings=new NarrativeAmbientBubbleModel("chr_settings",directory);changedSettings.ChangePool("pool_b");
        changedSettings.TryTrigger(0,0,trigger,choose,out bubble);
        Check(!changedSettings.TryTrigger(1,0,Trigger(0),choose,out bubble),"new cooldown setting cannot rewrite prior successful deadline");
        Check(changedSettings.TryTrigger(2,0,Trigger(0),choose,out bubble)&&bubble.NextAllowedAtSeconds==2,"new setting applied on next success");
        var readonlyIssues=commands.Diagnostics as IList<NarrativeAmbientBubbleDiagnostic>;bool denied=false;
        try{readonlyIssues.Clear();}catch(NotSupportedException){denied=true;}Check(denied,"diagnostic collection read only");
        return "{\"passed\":true,\"checks\":"+checks+",\"actualCoreModel\":true,\"externalTimeDistanceRandom\":true,\"unityNative\":false}";
    }
}
