using System;
using System.Collections.Generic;
using EatWhat.Narrative.Core;
using EatWhat.Narrative.Unity;

internal static class NarrativeDialogueEntranceTests
{
    static readonly List<string> details = new List<string>();
    static void Check(bool value, string reason) { if (!value) throw new Exception("Entrance: " + reason); details.Add(reason); }
    static void Near(double value, double expected, string reason) { Check(Math.Abs(value - expected) < 1e-5, reason); }
    sealed class Clock : INarrativeDialogueClock { public double Time; public double NowSeconds { get { return Time; } } }
    sealed class Surface : INarrativeDialogueBoxSurface
    {
        public Clock Clock; public string Body, Name; public bool NameVisible; public int End; public double NameAt;
        public void LayoutFullLine(string text, string name, bool show) { Body=text;Name=name;NameVisible=show;NameAt=Clock.Time; }
        public void SetVisiblePrefix(int end) { End=end; }
        public void SetToggleStates(bool auto, bool fast) { }
    }
    sealed class PoseCall { public double Time; public float Progress; public NarrativeDialogueBoxPose Pose; }
    // The box is fake geometry only. Typing/names/input are the actual existing production Presenter/Core owners.
    sealed class Box : INarrativeDialogueBoxPresentation
    {
        public readonly Surface Surface; public readonly NarrativeDialogueBoxPresenter Presenter; readonly Clock clock;
        public bool External, Hidden; public double BeganAt; public int Lines;
        public readonly List<PoseCall> Calls=new List<PoseCall>();
        public event Action<NarrativeLinePresentation> TextCompleted;
        public Box(Clock clock,NarrativeAdvanceInputModel input) { this.clock=clock;Surface=new Surface{Clock=clock};Presenter=new NarrativeDialogueBoxPresenter(Surface,input);Presenter.TextCompleted+=l=>TextCompleted?.Invoke(l); }
        public void SetExternalTime(bool external) { External=external; }
        public void Show(DialoguePresentationSnapshot snapshot) { Hidden=false;Presenter.PrepareEntrance(snapshot); }
        public void SetBoxProgress(DialoguePresentationSnapshot snapshot,float p) { Calls.Add(new PoseCall{Time=clock.Time,Progress=p,Pose=NarrativeDialogueBoxPresenter.Pose(snapshot,p)}); }
        public void SetEntranceProgress(DialoguePresentationSnapshot snapshot,float p) { Calls.Add(new PoseCall{Time=clock.Time,Progress=p,Pose=NarrativeDialogueBoxPresenter.EntrancePose(snapshot,p)}); }
        public void BeginLine(NarrativeLinePresentation line,string text) { BeganAt=clock.Time;Lines++;Presenter.BeginLine(line,text); }
        public void CompleteLine() { Presenter.CompleteLine(); }
        public void Advance(double seconds) { Presenter.Tick(seconds); }
        public void Hide() { Hidden=true;Presenter.Hide(); }
    }
    // Records requests/time only, no duplicate dim interpolation or request-merging implementation.
    sealed class Dim : INarrativeDialogueDimming
    {
        public Clock Clock; public bool External; public int Requests,Withdrawals; public double BeganAt,Seconds,Advanced; public object Owner;
        public void SetExternalTime(bool external) { External=external; }
        public void RequestNormal(object owner,DialoguePresentationSnapshot snapshot,double seconds) { Owner=owner;Requests++;BeganAt=Clock.Time;Seconds=seconds; }
        public bool Withdraw(object owner,double seconds) { Check(ReferenceEquals(owner,Owner)||Owner==null,"withdraw uses own stable dim token");Withdrawals++;return true; }
        public void Advance(double seconds) { Advanced+=seconds; }
    }
    sealed class Lock : INarrativeDialogueLock { public void Acquire(string id,double now){} public void Release(string id,double now){} }
    sealed class Hold : INarrativeCommandHandler
    {
        public bool IsComplete { get { return false; } }
        public void Begin(double now) { }
        public void CompleteImmediately(NarrativeCommandTruncation why,double now) { }
        public void Cancel(double now) { }
    }
    // Adapter only: later service09 owns this INarrativeDialogueView composition and callback forwarding.
    sealed class Bridge : INarrativeDialogueView
    {
        public NarrativeDialogueEntrance Driver; public bool InRequest;
        void Call(Action action) { InRequest=true;try{action();}finally{InRequest=false;} }
        public void DialogueStarted(NarrativeDialogueContext c,double t){Call(()=>Driver.DialogueStarted(c,t));}
        public void SetLetterbox(NarrativeDialogueContext c,bool v,double t){Call(()=>Driver.SetLetterbox(c,v,t));}
        public void PrepareStoryCamera(NarrativeDialogueContext c,double t){Call(()=>Driver.PrepareStoryCamera(c,t));}
        public void Enter(NarrativeDialogueContext c,double t){Call(()=>Driver.Enter(c,t));}
        public void CompleteEntrance(NarrativeDialogueContext c,double t){Call(()=>Driver.CompleteEntrance(c,t));}
        public void UpdateCast(NarrativeDialogueContext c,double t){Call(()=>Driver.UpdateCast(c,t));}
        public void SetSpeaker(NarrativeLinePresentation l,double t){Call(()=>Driver.SetSpeaker(l,t));}
        public void ReactToLine(NarrativeLinePresentation l,double t){Call(()=>Driver.ReactToLine(l,t));}
        public void StartText(NarrativeLinePresentation l,double t){Call(()=>Driver.StartText(l,t));}
        public void CompleteText(NarrativeLinePresentation l,double t){Call(()=>Driver.CompleteText(l,t));}
        public void CanEnd(NarrativeDialogueContext c,double t){Call(()=>Driver.CanEnd(c,t));}
        public void Exit(NarrativeDialogueContext c,double t){} // Exit animation/ack is explicitly V014-08, not faked as complete.
        public void DialogueEnded(NarrativeDialogueContext c,double t){Call(()=>Driver.Clear());}
    }
    sealed class Rig
    {
        public readonly Clock Clock=new Clock(); public readonly NarrativeAdvanceInputModel Input=new NarrativeAdvanceInputModel();
        public readonly DialoguePresentationData Config=new DialoguePresentationData();
        public readonly Box Box;public readonly Dim Dim;public readonly Bridge View;public readonly NarrativeDialogueEntrance Driver;
        public readonly NarrativeDialogueStateMachine Machine;public readonly NarrativeCommandScheduler Scheduler;
        public int EnterReports,TextReports,UnsupportedReports;
        public readonly List<NarrativeLinePresentation> ReportedLines=new List<NarrativeLinePresentation>();
        public Rig(NarrativeDialogueLevel level=NarrativeDialogueLevel.Normal,bool command=false)
        {
            Config.entrance[0].dimTime=2;Config.entrance[0].boxTime=3;Config.entrance[0].portraitTime=99;Config.entrance[0].letterboxTime=99;
            Config.text[0].charsPerSecond=4;Config.text[0].punctuationPause=0;Config.text[0].fastForwardMultiplier=4;Config.text[0].advanceCooldown=0;
            Box=new Box(Clock,Input);Dim=new Dim{Clock=Clock};Driver=new NarrativeDialogueEntrance(Box,Dim);View=new Bridge{Driver=Driver};
            var empty=new NarrativeCommandSource(NarrativeCommandParser.Parse(""));
            var dialogue=new NarrativeDialogue("dlg_test",level,new[]{new NarrativeCastEntry(1,"chr_a","",null,null,null,null)},new NarrativeProfileNames("","","","","","","","",""),"","ln_a","",empty);
            var first=new NarrativeDialogueLine("ln_a","dlg_test","ln_b","chr_a","","body.a",new NarrativeCommandSource(NarrativeCommandParser.Parse(command?"actor_move(chr_a,player)":"")),"","","","");
            var second=new NarrativeDialogueLine("ln_b","dlg_test","","chr_a","name.alt","body.b",empty,"","","","");
            var directory=new NarrativeDataDirectory(characters:new[]{new NarrativeCharacter("chr_a","name.a","",null,null,null,"","",null,null)},dialogues:new[]{dialogue},lines:new[]{first,second});
            Scheduler=new NarrativeCommandScheduler(c=>new Hold());
            Machine=new NarrativeDialogueStateMachine(directory,d=>DialoguePresentationResolver.Resolve(Config,d),View,new Lock(),Clock,new NarrativeTalkedRecord(),Scheduler,Input);
            Driver.EntranceCompleted+=c=>{Check(!View.InRequest,"entrance report after originating request returned");EnterReports++;Check(Machine.NotifyEntranceCompleted(c),"real FSM accepts original entrance context");};
            Driver.TextCompleted+=l=>{Check(!View.InRequest,"text report after originating request returned");TextReports++;ReportedLines.Add(l);Check(Machine.NotifyLineCompleted(l),"real FSM accepts original line token");};
            Driver.UnsupportedCompleted+=(kind,c,l)=>{Check(!View.InRequest,"unsupported acknowledgement deferred: "+kind);UnsupportedReports++;if(kind=="letterbox-show")Check(Machine.NotifyStoryBarsReady(c),"unsupported bars do not stall real story prepare");if(kind=="story-camera")Check(Machine.NotifyStoryCameraReady(c),"unsupported camera does not stall real story prepare");};
        }
        public void Start(){int enterBefore=EnterReports,textBefore=TextReports;Check(Machine.Start("dlg_test")==NarrativeDialogueStartStatus.Started,"actual FSM starts");Check(EnterReports==enterBefore&&TextReports==textBefore,"no synchronous completion from Start");}
        public void Step(double seconds){Clock.Time+=seconds;Driver.Advance(seconds);Scheduler.Tick(Clock.Time);}
        public void Instant(){Config.entrance[0].dimTime=Config.entrance[0].boxTime=0;Start();Step(0);}
    }
    static void Localization(string first="甲乙丙丁")
    {
        var table=new LocTableAsset();foreach(var pair in new[]{new[]{"body.a",first},new[]{"body.b","第二句"},new[]{"name.a","甲名"},new[]{"name.alt","改名"}})table.entries.Add(new LocTableEntry{key=pair[0],zh_cn=pair[1]});LocService.Configure(table);
    }
    static int Main()
    {
        Localization();var a=new Rig(command:true);a.Start();
        Check(a.Box.External&&a.Dim.External,"both production boundaries explicitly switch to external time");
        Check(a.Driver.EntranceSteps.Count==2&&a.Driver.EntranceSteps[0].Name=="dim"&&a.Driver.EntranceSteps[1].Name=="box","real sequence removes portrait rather than waiting99 seconds");
        Near(a.Dim.BeganAt,0,"dim starts at request time");Check(a.Box.Calls.Count==0,"sequential box animation not started with dim");
        a.Step(1);Check(a.Box.Calls.Count==0&&a.EnterReports==0,"sequential first midpoint only dim");
        a.Step(1);Near(a.Box.Calls[0].Time,2,"sequential box starts at dim boundary");Near(a.Box.Calls[0].Progress,0,"box begins at zero progress");
        a.Step(1);Near(a.Box.Calls[a.Box.Calls.Count-1].Progress,1.0/3,"box progress is linear over own duration");
        a.Step(2);Check(a.EnterReports==1&&a.Machine.Phase==NarrativeDialoguePhase.Typing,"sequential completes at sum5 with removed portrait");
        Check(a.Box.Surface.Name=="甲名"&&a.Box.Surface.NameVisible&&a.Box.Surface.Body=="甲乙丙丁","actual FSM name and localized full line reach real presenter");
        Near(a.Box.Surface.NameAt,5,"name updated at line start");Near(a.Box.BeganAt,5,"typing starts at identical external time");Near(a.Machine.CurrentLine.StartedAtSeconds,5,"real FSM line timestamp agrees");
        Check(!a.Machine.CurrentLineBatch.IsComplete,"pending actual scheduler command does not delay typing");
        a.Step(.25);Check(a.Box.Surface.End==1,"real typewriter advances from supplied delta");a.Step(.75);Check(a.TextReports==0,"typewriter completion not synchronous with request");a.Step(0);
        Check(a.TextReports==1&&ReferenceEquals(a.ReportedLines[0],a.Machine.CurrentLine),"text completes once with exact original line");a.Step(10);Check(a.TextReports==1,"natural completion does not repeat");
        a.Machine.Click();Check(a.Box.Surface.Name=="改名"&&a.Box.Surface.NameAt==a.Box.BeganAt,"second line name override and typing begin together");
        var parallel=new Rig();parallel.Config.entrance[0].order=NarrativeCommandExecutionMode.Parallel;parallel.Start();Near(parallel.Box.Calls[0].Time,0,"parallel box starts with dim");parallel.Step(2);Check(parallel.EnterReports==0,"parallel waits longest duration");parallel.Step(1);Check(parallel.EnterReports==1,"parallel finishes at max3");
        var skip=new Rig();skip.Start();skip.Machine.Click();Check(skip.Machine.Phase==NarrativeDialoguePhase.Typing,"real Core skip path itself starts line after completion request");Near(skip.Dim.Seconds,0,"skip forces own dim request to goal");Near(skip.Box.Calls[skip.Box.Calls.Count-1].Progress,1,"skip forces box to goal");skip.Step(0);Check(skip.EnterReports==0,"stale skip entrance report suppressed after Core already began line");
        var zero=new Rig();zero.Instant();Check(zero.EnterReports==1&&zero.Machine.Phase==NarrativeDialoguePhase.Typing,"all-zero entrance reports only on later Advance");
        zero.Input.SetFastForward(true);zero.Step(.125);Check(zero.Box.Presenter.VisibleCharacterCount==2,"fast flag reaches actual shared typewriter owner");zero.Input.SetFastForward(false);zero.Step(.25);Check(zero.Box.Presenter.VisibleCharacterCount==3,"fast switch off returns to base rate");
        zero.Machine.Click();Check(zero.TextReports==0&&zero.Box.Surface.End==4,"FSM manual completion reveals full line without reentry");zero.Step(0);Check(zero.TextReports==1,"manual completion reports once later");zero.Step(0);Check(zero.TextReports==1,"manual completion cannot repeat");
        Localization("");var empty=new Rig();empty.Instant();Check(empty.TextReports==0,"empty line StartText does not synchronously report");empty.Step(0);Check(empty.TextReports==1,"empty line reports once after returned request");empty.Step(0);Check(empty.TextReports==1,"empty completion once");Localization();
        var stale=new Rig();stale.Instant();var oldLine=stale.Machine.CurrentLine;stale.Box.CompleteLine();Check(stale.Machine.NotifyLineCompleted(oldLine),"set up real FSM line transition before old pending view pulse");stale.Machine.Click();var newLine=stale.Machine.CurrentLine;stale.Driver.CompleteText(oldLine,stale.Clock.Time);stale.Step(0);Check(stale.TextReports==0&&ReferenceEquals(stale.Machine.CurrentLine,newLine)&&stale.Box.Surface.End==0,"old completion/request cannot finish replacement line");
        stale.Machine.Click();stale.Step(0);Check(stale.TextReports==1&&ReferenceEquals(stale.ReportedLines[0],newLine),"replacement line completion correlated correctly");
        var current=stale.Machine.Current;stale.Machine.Click();Check(stale.Machine.NotifyExitCompleted(current),"external later exit capability can finish real context");stale.Start();stale.Driver.CompleteEntrance(current,0);stale.Driver.CompleteText(oldLine,0);stale.Driver.UpdateCast(current,0);stale.Step(0);Check(stale.EnterReports==2&&stale.TextReports==1,"old context notifications suppressed on new run");
        var cancel=new Rig();cancel.Config.entrance[0].dimTime=cancel.Config.entrance[0].boxTime=0;cancel.Start();cancel.Driver.Clear();cancel.Step(0);Check(cancel.EnterReports==0&&cancel.TextReports==0&&cancel.Box.Hidden&&cancel.Dim.Withdrawals==1,"clear cancels queued reports and cleans owned presentation");cancel.Driver.Dispose();Check(!cancel.Box.External&&!cancel.Dim.External,"dispose releases external time ownership");
        var inputRig=new Rig();inputRig.Instant();int presses=0;inputRig.Box.Presenter.AdvanceRequested+=()=>presses++;
        inputRig.Box.Presenter.ProcessFrame(100,false,true);Check(presses==1&&inputRig.Box.Surface.End==0,"external-time UI frame still forwards one Interact and does not tick");
        inputRig.Step(.25);Check(presses==1&&inputRig.Box.Surface.End==1,"service time tick neither polls input nor double advances");
        inputRig.Box.Presenter.ProcessFrame(100,false,false);Check(presses==1&&inputRig.Box.Surface.End==1,"external-time frame without press does neither action");
        inputRig.Box.Presenter.ProcessFrame(0,false,true);Check(presses==2,"next one-frame press emits exactly one additional request");
        inputRig.Box.Presenter.ProcessFrame(.25,true,false);Check(presses==2&&inputRig.Box.Surface.End==2,"unwired legacy frame still advances time once");
        var story=new Rig(NarrativeDialogueLevel.Story);story.Config.entrance[0].dimTime=story.Config.entrance[0].boxTime=0;story.Start();story.Step(0);story.Step(0);story.Step(0);Check(story.Machine.Phase==NarrativeDialoguePhase.Typing&&story.EnterReports==1,"unsupported bars/camera requests acknowledged later without configured99-second wait");story.Driver.UpdateCast(story.Machine.Current,story.Clock.Time);int before=story.UnsupportedReports;story.Step(0);Check(story.UnsupportedReports>before,"cast/speaker/reaction no-effect requests do not block line flow");
        story.Config.entrance[0].boxMode=DialogueBoxMode.ScalePop;var snap=DialoguePresentationResolver.Resolve(story.Config,story.Machine.Current.Dialogue).Snapshot;
        Near(NarrativeDialogueBoxPresenter.EntrancePose(snap,0).Scale,.82,"bounce starts at82percent");Near(NarrativeDialogueBoxPresenter.EntrancePose(snap,1).Scale,1,"bounce ends at100percent");Check(NarrativeDialogueBoxPresenter.EntrancePose(snap,.75f).Scale>1,"fixed view bounce has small overshoot");Near(NarrativeDialogueBoxPresenter.EntrancePose(snap,.75f).Alpha,.75,"bounce alpha stays linear");Near(NarrativeDialogueBoxPresenter.Pose(snap,.75f).Scale,.955,"original base pose remains unchanged");
        Console.WriteLine("ENTRANCE_RESULT={\"passed\":true,\"checks\":"+details.Count+",\"actualFsmSequenceTypewriter\":true,\"fakeBoxGeometryAndDimRequestsOnly\":true,\"nativeUnity\":false,\"realTmpGlyphPositionsVerified\":false,\"details\":[\""+string.Join("\",\"",details.ToArray())+"\"]}");return 0;
    }
}
