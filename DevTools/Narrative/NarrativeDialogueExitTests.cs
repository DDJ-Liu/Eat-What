using System;
using System.Collections.Generic;
using EatWhat.Narrative.Core;
using EatWhat.Narrative.Unity;

internal static class NarrativeDialogueExitTests
{
    static readonly List<string> details = new List<string>();
    static void Check(bool value,string reason) { if(!value)throw new Exception("Exit: "+reason);details.Add(reason); }
    static void Near(double value,double expected,string reason) { Check(Math.Abs(value-expected)<1e-5,reason); }
    sealed class Clock : INarrativeDialogueClock { public double Time;public double NowSeconds{get{return Time;}} }
    sealed class Surface : INarrativeDialogueBoxSurface
    {
        public void LayoutFullLine(string text,string name,bool visible){} public void SetVisiblePrefix(int end){} public void SetToggleStates(bool auto,bool fast){}
    }
    // Geometry/output handles only. The presenter, typewriter, input, dimmer and sequence are production code.
    sealed class Box : INarrativeDialogueBoxPresentation
    {
        public readonly NarrativeDialogueBoxPresenter Presenter;public float Progress;public bool Hidden;public int Hides,Shows,Poses;public double Advanced;
        public event Action<NarrativeLinePresentation> TextCompleted;
        public Box(NarrativeAdvanceInputModel input){Presenter=new NarrativeDialogueBoxPresenter(new Surface(),input);Presenter.TextCompleted+=l=>TextCompleted?.Invoke(l);}
        public void SetExternalTime(bool value){} public void Show(DialoguePresentationSnapshot s){Hidden=false;Shows++;Presenter.PrepareEntrance(s);}
        public void SetBoxProgress(DialoguePresentationSnapshot s,float p){Progress=p;Poses++;}
        public void SetEntranceProgress(DialoguePresentationSnapshot s,float p){Progress=p;Poses++;NarrativeDialogueBoxPresenter.EntrancePose(s,p);}
        public void BeginLine(NarrativeLinePresentation l,string text){Presenter.BeginLine(l,text);} public void CompleteLine(){Presenter.CompleteLine();}
        public void Advance(double dt){Advanced+=dt;Presenter.Tick(dt);} public void Hide(){Hides++;Hidden=true;Presenter.Hide();}
    }
    sealed class Dim : INarrativeDialogueDimming
    {
        public readonly NarrativeSceneDimmer Actual=new NarrativeSceneDimmer();public int Withdrawals;public double Advanced;
        public void SetExternalTime(bool v){Actual.SetExternalTime(v);} public void RequestNormal(object owner,DialoguePresentationSnapshot s,double dt){Actual.RequestNormal(owner,s,dt);}
        public bool Withdraw(object owner,double dt){Withdrawals++;return Actual.Withdraw(owner,dt);} public void Advance(double dt){Advanced+=dt;Actual.Advance(dt);}
    }
    sealed class Lock : INarrativeDialogueLock { public int Acquires,Releases;public void Acquire(string id,double t){Acquires++;}public void Release(string id,double t){Releases++;} }
    sealed class Handler : INarrativeCommandHandler { public bool IsComplete{get{return true;}}public void Begin(double t){}public void CompleteImmediately(NarrativeCommandTruncation why,double t){}public void Cancel(double t){} }
    sealed class Bridge : INarrativeDialogueView
    {
        public NarrativeDialogueEntrance Entrance;public NarrativeDialogueExit ExitDriver;public bool InRequest;public int Ends,ExitRequests,Enters;
        void Call(Action action){InRequest=true;try{action();}finally{InRequest=false;}}
        public void DialogueStarted(NarrativeDialogueContext c,double t){Call(()=>Entrance.DialogueStarted(c,t));}
        public void Enter(NarrativeDialogueContext c,double t){Enters++;Call(()=>Entrance.Enter(c,t));}
        public void CompleteEntrance(NarrativeDialogueContext c,double t){Call(()=>Entrance.CompleteEntrance(c,t));}
        public void StartText(NarrativeLinePresentation l,double t){Call(()=>Entrance.StartText(l,t));}
        public void CompleteText(NarrativeLinePresentation l,double t){Call(()=>Entrance.CompleteText(l,t));}
        public void Exit(NarrativeDialogueContext c,double t){ExitRequests++;Call(()=>ExitDriver.Exit(c,t));}
        public void DialogueEnded(NarrativeDialogueContext c,double t){Ends++;} // Segment completion is not service abort.
        public void SetLetterbox(NarrativeDialogueContext c,bool visible,double t){Call(()=>Entrance.SetLetterbox(c,visible,t));}
        public void PrepareStoryCamera(NarrativeDialogueContext c,double t){Call(()=>Entrance.PrepareStoryCamera(c,t));}
        public void UpdateCast(NarrativeDialogueContext c,double t){Call(()=>Entrance.UpdateCast(c,t));}
        public void SetSpeaker(NarrativeLinePresentation l,double t){Call(()=>Entrance.SetSpeaker(l,t));}
        public void ReactToLine(NarrativeLinePresentation l,double t){Call(()=>Entrance.ReactToLine(l,t));}
        public void CanEnd(NarrativeDialogueContext c,double t){Call(()=>Entrance.CanEnd(c,t));}
    }
    static readonly NarrativeCommandSource empty=new NarrativeCommandSource(NarrativeCommandParser.Parse(""));
    static NarrativeDialogue Dialogue(string id,string raw="",string next="")
    { return new NarrativeDialogue(id,NarrativeDialogueLevel.Normal,new[]{new NarrativeCastEntry(1,"chr_a","",null,null,null,null)},new NarrativeProfileNames("","","","","","","","",""),raw,"ln_"+id,next,empty); }
    sealed class Rig
    {
        public readonly Clock Clock=new Clock();public readonly DialoguePresentationData Config=new DialoguePresentationData();public readonly NarrativeAdvanceInputModel Input=new NarrativeAdvanceInputModel();
        public readonly Box Box;public readonly Dim Dim=new Dim();public readonly Lock Lock=new Lock();public readonly NarrativeTalkedRecord Talked=new NarrativeTalkedRecord();
        public readonly NarrativeDialogueEntrance Entrance;public readonly NarrativeDialogueExit Exit;public readonly Bridge View;public readonly NarrativeDialogueStateMachine Machine;
        public readonly List<string> Diagnostics=new List<string>();public int Reports;public NarrativeDialogueContext First,Reported;public Action BeforeReport;
        public Rig(NarrativeCommandExecutionMode order=NarrativeCommandExecutionMode.Sequential,DialogueControlResume resume=DialogueControlResume.AfterBoxClosed,double box=2,double undim=4,bool chain=false,string raw="")
        {
            Config.tier[0].normalDim=.6f;Config.entrance[0].dimTime=Config.entrance[0].boxTime=0;Config.text[0].charsPerSecond=0;Config.text[0].advanceCooldown=0;
            Config.exit[0].order=order;Config.exit[0].controlResume=resume;Config.exit[0].boxTime=(float)box;Config.exit[0].undimTime=(float)undim;
            Config.exit[0].portraitTime=Config.exit[0].cameraReturnTime=Config.exit[0].letterboxOutTime=99;
            Box=new Box(Input);Entrance=new NarrativeDialogueEntrance(Box,Dim,key=>"字");Exit=new NarrativeDialogueExit(Entrance);View=new Bridge{Entrance=Entrance,ExitDriver=Exit};
            var a=Dialogue("a",raw,chain?"b":"");var b=Dialogue("b","tier.normalDim=0.8;entrance.dimTime=2;entrance.boxTime=1");
            var directory=new NarrativeDataDirectory(characters:new[]{new NarrativeCharacter("chr_a","name.a","",null,null,null,"","",null,null)},dialogues:new[]{a,b},lines:new[]{
                new NarrativeDialogueLine("ln_a","a","","chr_a","","body.a",empty,"","","",""),new NarrativeDialogueLine("ln_b","b","","chr_a","","body.b",empty,"","","","")});
            var policy=new NarrativeDialogueContinuationPolicy(message=>Diagnostics.Add(message));
            Machine=new NarrativeDialogueStateMachine(directory,d=>policy.Resolve(Config,d),View,Lock,Clock,Talked,new NarrativeCommandScheduler(c=>new Handler()),Input);
            Entrance.EntranceCompleted+=c=>{Check(!View.InRequest,"entrance callback remains deferred");Check(Machine.NotifyEntranceCompleted(c),"real FSM accepts entrance token");};
            Entrance.TextCompleted+=l=>{Check(!View.InRequest,"text callback remains deferred");Check(Machine.NotifyLineCompleted(l),"real FSM accepts line token");};
            Exit.ExitCompleted+=c=>{Check(!View.InRequest,"exit callback after request stack returned");Reports++;Reported=c;BeforeReport?.Invoke();Check(Machine.NotifyExitCompleted(c),"real FSM accepts original exit token");};
        }
        public void StartExit(){Check(Machine.Start("a")==NarrativeDialogueStartStatus.Started,"real FSM starts segment");First=Machine.Current;Step(0);Machine.Click();Step(0);Machine.Click();Check(Machine.Phase==NarrativeDialoguePhase.Exiting&&Reports==0,"exit request never completes synchronously");}
        public void Step(double dt){Clock.Time+=dt;Exit.Advance(dt);}
    }
    static void PolicyCases()
    {
        var config=new DialoguePresentationData();var diagnostics=new List<string>();var policy=new NarrativeDialogueContinuationPolicy(s=>diagnostics.Add(s));
        var plain=Dialogue("plain");Check(policy.Resolve(config,plain).Snapshot.Get<DialogueContinuationMode>("exit.continuationMode")==DialogueContinuationMode.FullFlow&&diagnostics.Count==0,"full-flow selection is unchanged and silent");
        config.exit[0].continuationMode=DialogueContinuationMode.Seamless;config.exit[0].boxTime=.7f;config.exit[0].undimTime=.9f;config.exit[0].portraitSlide=234;config.exit[0].controlResume=DialogueControlResume.AfterCameraReturned;
        var runtime=policy.Resolve(config,plain);Check(runtime.Snapshot.Get<DialogueContinuationMode>("exit.continuationMode")==DialogueContinuationMode.FullFlow&&diagnostics.Count==1,"template Seamless falls back once per resolved segment");
        Near(runtime.Snapshot.Get<float>("exit.boxTime"),.7,"fallback preserves box duration");Near(runtime.Snapshot.Get<float>("exit.undimTime"),.9,"fallback preserves undim duration");Near(runtime.Snapshot.Get<float>("exit.portraitSlide"),234,"fallback preserves unsupported parameters");
        Check(runtime.Snapshot.Get<DialogueControlResume>("exit.controlResume")==DialogueControlResume.AfterCameraReturned,"fallback preserves resume policy");
        Check(config.exit[0].continuationMode==DialogueContinuationMode.Seamless&&DialoguePresentationResolver.Resolve(config,plain).Snapshot.Get<DialogueContinuationMode>("exit.continuationMode")==DialogueContinuationMode.Seamless,"saved authoring data and Core resolver retain Seamless");
        string raw=" text.charsPerSecond=17; exit.continuationMode = Seamless ; exit.continuationMode=FullFlow; bad.key=3";
        var model=Dialogue("override",raw);var before=DialoguePresentationResolver.Resolve(config,model);var after=policy.Resolve(config,model);
        Check(after.Snapshot.Get<DialogueContinuationMode>("exit.continuationMode")==DialogueContinuationMode.FullFlow&&model.Overrides==raw,"valid Seamless override normalized in temporary parsed input only");
        Near(after.Snapshot.Get<float>("text.charsPerSecond"),17,"other overrides retain precedence");Check(after.OverrideDiagnostics.Count==before.OverrideDiagnostics.Count&&after.OverrideDiagnostics.Count==2,"duplicate and unknown diagnostics retained");
        for(int i=0;i<before.OverrideDiagnostics.Count;i++)Check(before.OverrideDiagnostics[i].ToString()==after.OverrideDiagnostics[i].ToString(),"original unrelated diagnostic and offset preserved "+i);
        var invalid=Dialogue("invalid","exit.continuationMode=wrong;exit.continuationMode=Seamless");var invalidResult=policy.Resolve(config,invalid);
        Check(invalidResult.Snapshot.Get<DialogueContinuationMode>("exit.continuationMode")==DialogueContinuationMode.FullFlow&&invalidResult.OverrideDiagnostics.Count==2,"invalid first and later duplicate remain diagnostics while template falls back");
        int count=diagnostics.Count;var explicitFull=Dialogue("explicit","exit.continuationMode=FullFlow");policy.Resolve(config,explicitFull);Check(diagnostics.Count==count,"explicit FullFlow override suppresses template fallback diagnostic");
        config.exit[0].boxTime=-1;var bad=policy.Resolve(config,plain);Check(!bad.Success&&diagnostics.Count==count,"invalid configuration remains failed without fallback mutation");
    }
    static int Main()
    {
        PolicyCases();
        var early=new Rig();early.StartExit();Check(early.Exit.ExitSteps.Count==2&&early.Exit.ExitSteps[0].Name=="box"&&early.Exit.ExitSteps[1].Name=="undim","portrait removed from real exit sequence");
        early.Step(1);Near(early.Box.Progress,.5,"box closes with reverse entrance progress");Near(early.Dim.Actual.CurrentAmount,.6,"sequential dim retained until box closes");Check(early.Reports==0,"no report before box closure");
        early.BeforeReport=()=>Check(early.Lock.Releases==0&&!early.Talked.HasTalked("a"),"exit driver does not perform Core end effects");early.Step(1);
        Check(early.Reports==1&&ReferenceEquals(early.Reported,early.First)&&early.Box.Hidden,"AfterBoxClosed reports once using exact context");Near(early.Dim.Actual.CurrentAmount,.6,"sequential undim starts at box boundary without consuming box time");
        Check(early.Dim.Actual.RequestCount==0&&early.Dim.Actual.IsTransitioning,"own dim request withdrawn while background transition remains");Check(early.Talked.HasTalked("a")&&early.Lock.Releases==1&&early.View.Ends==1,"real FSM alone marks talked releases lock and emits end");
        early.Step(1);Near(early.Dim.Actual.CurrentAmount,.45,"undim continues after early FSM completion");early.Step(3);Near(early.Dim.Actual.CurrentAmount,0,"background undim reaches brightness");early.Step(9);Check(early.Reports==1&&early.Box.Hides==1&&early.Dim.Withdrawals==1,"finished exit never repeats callback hide or withdrawal");Near(early.Box.Advanced,early.Clock.Time,"box total time is advanced exactly once");Near(early.Dim.Advanced,early.Clock.Time,"dimmer total time is advanced exactly once");
        var all=new Rig(resume:DialogueControlResume.AfterCameraReturned);all.StartExit();all.Step(2);Check(all.Reports==0,"AfterCameraReturned waits beyond box closure");all.Step(3);Near(all.Dim.Actual.CurrentAmount,.15,"sequential all-steps policy still undims");Check(all.Reports==0,"all-steps policy waits until undim completes");all.Step(1);Check(all.Reports==1&&all.View.Ends==1,"ordinary all-steps policy ends at box plus undim without camera99 wait");
        var parallel=new Rig(order:NarrativeCommandExecutionMode.Parallel);parallel.StartExit();Check(parallel.Dim.Withdrawals==1,"parallel withdrawal begins with box request");parallel.Step(1);Near(parallel.Dim.Actual.CurrentAmount,.45,"parallel undim consumes same first second as box");parallel.Step(1);Check(parallel.Reports==1,"parallel AfterBoxClosed reports at box duration");Near(parallel.Dim.Actual.CurrentAmount,.3,"parallel background transition only halfway when box closes");parallel.Step(2);Near(parallel.Dim.Actual.CurrentAmount,0,"parallel undim completes after report");
        var parallelAll=new Rig(order:NarrativeCommandExecutionMode.Parallel,resume:DialogueControlResume.AfterCameraReturned);parallelAll.StartExit();parallelAll.Step(2);Check(parallelAll.Reports==0,"parallel all-steps waits longer undim");parallelAll.Step(2);Check(parallelAll.Reports==1,"parallel all-steps completes at max duration");
        var shortDim=new Rig(order:NarrativeCommandExecutionMode.Parallel,resume:DialogueControlResume.AfterCameraReturned,undim:1);shortDim.StartExit();shortDim.Step(1);Check(shortDim.Reports==0&&shortDim.Dim.Actual.CurrentAmount==0,"completed parallel undim still waits for box");shortDim.Step(1);Check(shortDim.Reports==1,"longer box determines all-steps completion");
        foreach(var resume in new[]{DialogueControlResume.AfterBoxClosed,DialogueControlResume.AfterCameraReturned})
        {
            var zero=new Rig(resume:resume,box:0,undim:0);zero.StartExit();Check(zero.Box.Hidden&&zero.Dim.Actual.CurrentAmount==0&&zero.Reports==0,"zero exit effects immediate but callback deferred "+resume);zero.Step(0);zero.Step(0);Check(zero.Reports==1,"zero exit reports exactly once on later pump "+resume);
        }
        var zeroBox=new Rig(box:0,undim:3);zeroBox.StartExit();zeroBox.Step(0);Check(zeroBox.Reports==1&&zeroBox.Dim.Actual.CurrentAmount>.59,"zero box reports before positive undim");zeroBox.Step(1);Near(zeroBox.Dim.Actual.CurrentAmount,.4,"zero box background transition continues");
        foreach(var order in new[]{NarrativeCommandExecutionMode.Sequential,NarrativeCommandExecutionMode.Parallel})
        {
            var next=new Rig(order:order,box:1,chain:true);next.StartExit();next.Step(1);double at=order==NarrativeCommandExecutionMode.Sequential?.6:.45;
            Check(next.Machine.Current.Dialogue.DialogueId=="b"&&next.View.Enters==2,"real FSM starts next full entrance immediately "+order);Near(next.Dim.Actual.CurrentAmount,at,"continuation retarget preserves exact current dim "+order);Near(next.Dim.Actual.TargetAmount,.8,"next segment owns new dim target "+order);
            int poses=next.Box.Poses,hides=next.Box.Hides;next.Exit.Exit(next.First,next.Clock.Time);next.Step(.5);Near(next.Dim.Actual.CurrentAmount,at+(.8-at)*.25,"next dim transition progresses from retained amount "+order);
            Check(next.Box.Poses==poses&&next.Box.Hides==hides&&!next.Box.Hidden&&next.Dim.Withdrawals==1,"old exit cannot pose hide or withdraw next segment "+order);Check(next.Reports==1&&next.Lock.Acquires==2&&next.Lock.Releases==1,"continuation end effects remain owned by Core "+order);
        }
        var carry=new Rig(box:1,chain:true);carry.StartExit();carry.Step(1.5);Near(carry.Dim.Actual.CurrentAmount,.65,"delta crossing closure applies only residual half second to next entrance");Near(carry.Box.Advanced,1.5,"split pump does not duplicate box time");Near(carry.Dim.Advanced,1.5,"split pump does not duplicate dim time");
        var seamless=new Rig(box:1,chain:true,raw:"exit.continuationMode=Seamless");seamless.StartExit();Check(seamless.View.ExitRequests==1&&seamless.Diagnostics.Count==1,"Seamless actually reaches exit instead of Core bypass");seamless.Step(1);Check(seamless.View.Enters==2&&seamless.Diagnostics.Count==1,"Seamless uses full next entrance with one fallback diagnostic");
        var abort=new Rig();abort.StartExit();abort.Step(.5);abort.Exit.Clear();Check(abort.Box.Hidden&&abort.Dim.Actual.CurrentAmount==0&&abort.Dim.Actual.RequestCount==0&&abort.Entrance.CurrentContext==null,"abort immediately clears own surfaces and context");abort.Step(100);Check(abort.Reports==0&&!abort.Talked.HasTalked("a")&&abort.Lock.Releases==0,"abort emits no completion and does not impersonate Core end");
        var pending=new Rig(box:0,undim:0);pending.StartExit();pending.Exit.Clear();pending.Step(0);Check(pending.Reports==0,"abort suppresses queued all-zero completion");
        var shared=new Rig();object other=new object();shared.Dim.Actual.Request(other,.3f,0);shared.StartExit();shared.Exit.Clear();Near(shared.Dim.Actual.CurrentAmount,.3,"abort preserves independent dim owner");Check(shared.Dim.Actual.RequestCount==1,"abort removes only dialogue request");
        bool rejected=false;try{shared.Exit.Advance(double.NaN);}catch(ArgumentOutOfRangeException){rejected=true;}Check(rejected,"invalid external delta rejected");
        Console.WriteLine("DIALOGUE_EXIT_RESULT "+UnityJson());return 0;
    }
    static string UnityJson(){return "{\"passed\":true,\"actualFsmSequenceDimmer\":true,\"nativeUnity\":false,\"checks\":"+details.Count+",\"details\":["+string.Join(",",details.ConvertAll(s=>"\""+s.Replace("\\","\\\\").Replace("\"","\\\"")+"\"").ToArray())+"]}";}
}
