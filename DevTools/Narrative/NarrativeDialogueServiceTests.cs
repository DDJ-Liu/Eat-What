using System;
using System.Collections.Generic;
using System.IO;
using EatWhat.Narrative.Core;
using EatWhat.Narrative.Unity;
using EatWhat.Tools.Security;
using Newtonsoft.Json;

internal static class NarrativeDialogueServiceTests
{
    static readonly List<string> details=new List<string>();static string root;
    static void Check(bool value,string name){if(!value)throw new Exception("Service: "+name);details.Add(name);}
    static void Near(double a,double b,string name){Check(Math.Abs(a-b)<1e-5,name);}
    sealed class Keys:IKeyProvider{public byte[] GetKey(){var bytes=new byte[32];for(int i=0;i<32;i++)bytes[i]=(byte)(i+1);return bytes;}}
    sealed class Surface:INarrativeDialogueBoxSurface
    {public void LayoutFullLine(string text,string name,bool visible){}public void SetVisiblePrefix(int end){}public void SetToggleStates(bool auto,bool fast){}}
    // Only geometry is fake. This uses the actual presenter/input/typewriter; no duplicated UI state.
    sealed class Box:INarrativeDialogueBoxPresentation
    {
        public NarrativeDialogueBoxPresenter Presenter;public readonly List<string> Trace;public bool Hidden;public int Shows,Lines;public double Advanced;
        public event Action<NarrativeLinePresentation> TextCompleted;
        public Box(List<string> trace){Trace=trace;}
        public void Configure(NarrativeAdvanceInputModel input){Presenter=new NarrativeDialogueBoxPresenter(new Surface(),input);Presenter.TextCompleted+=l=>TextCompleted?.Invoke(l);}
        public void SetExternalTime(bool value){}public void Show(DialoguePresentationSnapshot s){Hidden=false;Shows++;Presenter.PrepareEntrance(s);}
        public void SetBoxProgress(DialoguePresentationSnapshot s,float p){}public void SetEntranceProgress(DialoguePresentationSnapshot s,float p){}
        public void BeginLine(NarrativeLinePresentation line,string text){Lines++;Trace.Add("line:"+line.Context.Dialogue.DialogueId);Presenter.BeginLine(line,text);}
        public void CompleteLine(){Presenter.CompleteLine();}public void Advance(double dt){Advanced+=dt;Presenter.Tick(dt);}public void Hide(){Hidden=true;Presenter.Hide();}
    }
    sealed class Dim:INarrativeDialogueDimming
    {
        public readonly NarrativeSceneDimmer Actual=new NarrativeSceneDimmer();public double Advanced;
        public void SetExternalTime(bool value){Actual.SetExternalTime(value);}public void RequestNormal(object owner,DialoguePresentationSnapshot s,double time){Actual.RequestNormal(owner,s,time);}
        public bool Withdraw(object owner,double time){return Actual.Withdraw(owner,time);}public void Advance(double dt){Advanced+=dt;Actual.Advance(dt);}
    }
    sealed class Lock:INarrativeDialogueLock
    {
        public readonly HashSet<string> Held=new HashSet<string>();public List<string> Trace;public NarrativeTalkedPersistence Talked;public int Acquires,Releases;
        public void Acquire(string id,double now){Check(Held.Add(id),"real FSM acquires injected fake lock once");Acquires++;Trace.Add("lock:"+id);}
        public void Release(string id,double now){Check(Held.Remove(id),"only held dialogue lock is released");Releases++;if(Talked.HasTalked(id))Trace.Add("record:"+id);Trace.Add("unlock:"+id);}
    }
    static readonly NarrativeCommandSource Empty=new NarrativeCommandSource(NarrativeCommandParser.Parse(""));
    sealed class Rig
    {
        public bool Available=true;public int Frame=10;public readonly List<string> Trace=new List<string>(),Warnings=new List<string>();
        public readonly List<NarrativeDialogueEvent> Starts=new List<NarrativeDialogueEvent>(),Ends=new List<NarrativeDialogueEvent>();
        public readonly DialoguePresentationData Config=new DialoguePresentationData();public readonly NarrativeTalkedPersistence Talked;
        public readonly Box Box;public readonly Dim Dim=new Dim();public readonly Lock Lock=new Lock();public readonly NarrativeDialogueSession Service;public readonly string Files;
        public readonly NarrativeDataDirectory Directory;
        public Rig(string name,string next="",bool commands=false,string overrides="")
        {
            Files=Path.Combine(root,name);Talked=NarrativeTalkedPersistence.CreateForCurrentBuild(Files,new Keys(),Warnings.Add);
            Config.entrance[0].dimTime=Config.entrance[0].boxTime=Config.exit[0].boxTime=Config.exit[0].undimTime=0;
            Config.text[0].charsPerSecond=0;Config.text[0].advanceCooldown=0;Config.text[0].autoWait=9;
            var cast=new[]{new NarrativeCastEntry(1,"chr_a","",null,null,null,null)};var profiles=new NarrativeProfileNames("","","","","","","","","");
            var cmd=new NarrativeCommandSource(NarrativeCommandParser.Parse(commands?"actor_move(chr_a,player)":""));
            var dialogues=new[]{new NarrativeDialogue("a",NarrativeDialogueLevel.Normal,cast,profiles,overrides,"ln_a",next,cmd),
                new NarrativeDialogue("b",NarrativeDialogueLevel.Normal,cast,profiles,"","ln_b","",Empty),
                new NarrativeDialogue("ambient",NarrativeDialogueLevel.Ambient,cast,profiles,"","ln_ambient","",Empty),
                new NarrativeDialogue("story",NarrativeDialogueLevel.Story,cast,profiles,"","ln_story","",Empty),
                new NarrativeDialogue("bad",NarrativeDialogueLevel.Normal,cast,profiles,"","ln_bad","",Empty)};
            var lines=new List<NarrativeDialogueLine>();foreach(var id in new[]{"a","b","ambient","story","bad"})lines.Add(new NarrativeDialogueLine("ln_"+id,id,id=="bad"?"ln_bad":"","chr_a","","body",id=="a"?cmd:Empty,"","","",""));
            Directory=new NarrativeDataDirectory(characters:new[]{new NarrativeCharacter("chr_a","name","",null,null,null,"","",null,null)},dialogues:dialogues,lines:lines);
            Box=new Box(Trace);Lock.Trace=Trace;Lock.Talked=Talked;
            Service=new NarrativeDialogueSession(()=>Available?Directory:null,(d,p)=>p.Resolve(Config,d),Box,Dim,Lock,Talked,Warnings.Add);
            Box.Configure(Service.Input);Box.Presenter.AdvanceRequested+=()=>Service.RequestAdvance(Frame);
            Service.DialogueStarted+=item=>{Starts.Add(item);Trace.Add("start:"+item.DialogueId);Check(Lock.Held.Contains(item.DialogueId)&&Service.IsRunning,"start event after lock with running query set");};
            Service.DialogueEnded+=item=>{Ends.Add(item);Trace.Add((item.Aborted?"abort:":"end:")+item.DialogueId);Check(!Lock.Held.Contains(item.DialogueId)&&!Service.IsRunning,"end event after unlock with running query cleared");};
        }
        public NarrativeServiceStartStatus Start(string id="a",string initiator="chr_npc#2"){string diagnostic;return Service.TryStart(id,initiator,Frame,out diagnostic);}
        public void Step(double dt=0){Frame++;Service.Advance(dt,Frame);}
        public void Press(){Box.Presenter.InteractPressed();}
        public void Finish()
        {
            if(Service.Phase==NarrativeDialoguePhase.Entering)Step();
            if(Service.Phase==NarrativeDialoguePhase.Typing){Press();Step();}
            if(Service.Phase==NarrativeDialoguePhase.LineComplete){Press();Step();}
            if(Service.Phase==NarrativeDialoguePhase.Exiting)Step();
        }
        public NarrativeTalkedPersistence Cold(){var value=NarrativeTalkedPersistence.CreateForCurrentBuild(Files,new Keys(),Warnings.Add);value.InitializeWhenAvailable(true);return value;}
    }
    static int Main(string[] args)
    {
        try
        {
            if(args.Length!=1||!Path.IsPathRooted(args[0])||System.IO.Directory.Exists(args[0]))throw new Exception("Fresh isolated root required");root=args[0];
            var localization=new LocTableAsset();localization.entries.Add(new LocTableEntry{key="body",zh_cn="正文"});localization.entries.Add(new LocTableEntry{key="name",zh_cn="名字"});LocService.Configure(localization);
            var rejected=new Rig("rejected");string issue;rejected.Available=false;
            Check(rejected.Service.TryStart("a","",rejected.Frame,out issue)==NarrativeServiceStartStatus.Unavailable&&issue.Contains("RUNTIME"),"unavailable runtime rejection has reason");
            Check(!rejected.Talked.IsInitialized&&!System.IO.Directory.Exists(rejected.Files),"unavailable start never loads player storage");rejected.Available=true;
            Check(rejected.Service.TryStart("missing","",rejected.Frame,out issue)==NarrativeServiceStartStatus.MissingDialogue&&issue.Contains("MISSING"),"missing dialogue rejection has reason");
            Check(rejected.Service.TryStart("ambient","",rejected.Frame,out issue)==NarrativeServiceStartStatus.NotApplicable&&issue.Contains("AMBIENT"),"ambient dialogue is not applicable");
            Check(rejected.Service.TryStart("story","",rejected.Frame,out issue)==NarrativeServiceStartStatus.UnsupportedLevel&&issue.Contains("STORY"),"story dialogue is unsupported");
            Check(rejected.Service.TryStart("bad","",rejected.Frame,out issue)==NarrativeServiceStartStatus.InvalidDialogue&&rejected.Lock.Acquires==0,"invalid line chain rejected without lock or event");
            Check(rejected.Starts.Count==0&&rejected.Ends.Count==0,"rejected requests emit no lifecycle events");
            var normal=new Rig("normal");Check(normal.Start()==NarrativeServiceStartStatus.Started,"normal request starts actual FSM");
            Check(normal.Service.DialogueId=="a"&&normal.Service.InitiatorId=="chr_npc#2","running query preserves complete suffixed initiator");normal.Press();
            Check(normal.Service.Phase==NarrativeDialoguePhase.Entering&&normal.Box.Lines==0,"input in starting frame ignored without consuming entrance");normal.Step();
            Check(normal.Service.Phase==NarrativeDialoguePhase.Typing&&normal.Box.Lines==1,"later pump starts line after start event");
            Check(normal.Service.TryStart("b","",normal.Frame,out issue)==NarrativeServiceStartStatus.Busy&&issue.Contains("BUSY"),"active request rejected with busy reason");
            normal.Finish();Check(!normal.Service.IsRunning&&normal.Service.DialogueId==null&&normal.Service.InitiatorId==null,"normal end clears service query");
            Check(string.Join(",",normal.Trace)=="lock:a,start:a,line:a,record:a,unlock:a,end:a","normal order lock start line talked unlock end");
            Check(normal.Talked.HasTalked("a")&&normal.Cold().HasTalked("a"),"real FSM completion immediately reaches actual persisted record");
            Check(normal.Ends.Count==1&&!normal.Ends[0].Aborted&&normal.Ends[0].InitiatorId=="chr_npc#2","normal end event immutable identity is correct");
            normal.Step();Check(normal.Ends.Count==1&&normal.Lock.Releases==1,"extra pump cannot repeat normal end");

            var chained=new Rig("chain",next:"b",commands:true,overrides:"exit.continuationMode=Seamless");chained.Start();chained.Step();chained.Box.Presenter.AutoClicked();chained.Box.Presenter.FastForwardClicked();
            Check(chained.Service.Input.AutoEnabled&&chained.Service.Input.FastForwardEnabled,"buttons use service-owned shared input flags");chained.Finish();
            Check(chained.Service.IsRunning&&chained.Service.DialogueId=="b"&&chained.Service.InitiatorId=="chr_npc#2","FSM continuation starts next normal segment with same initiator");
            Check(chained.Starts.Count==2&&chained.Ends.Count==1&&chained.Talked.HasTalked("a")&&!chained.Talked.HasTalked("b"),"each continuation segment has separate events and talked timing");
            Check(chained.Service.Input.AutoEnabled&&chained.Service.Input.FastForwardEnabled&&chained.Box.Shows==2,"FullFlow continuation keeps toggles and performs new entrance");
            Check(chained.Service.Diagnostics.ExistsText("Seamless")&&chained.Service.Diagnostics.ExistsText("handler"),"FullFlow fallback and actual missing command handler diagnose without stalling");
            chained.Press();Check(chained.Service.Phase==NarrativeDialoguePhase.Entering,"continuation starting-frame advance is ignored too");chained.Finish();
            Check(chained.Starts.Count==2&&chained.Ends.Count==2&&chained.Cold().Count==2,"both actual segments persisted and ended once");
            Check(chained.Start("b",null)==NarrativeServiceStartStatus.Started&&chained.Service.Input.AutoEnabled&&chained.Service.Input.FastForwardEnabled,"next external dialogue keeps both flags during service lifetime");chained.Finish();
            Check(chained.Ends[chained.Ends.Count-1].InitiatorId==""&&chained.Cold().Count==2,"optional initiator is empty and duplicate talk remains unique");

            var automatic=new Rig("auto");automatic.Config.text[0].charsPerSecond=2;automatic.Config.text[0].autoWait=1;automatic.Start();automatic.Step();automatic.Service.Input.SetAuto(true);automatic.Step(1);automatic.Step();
            Check(automatic.Service.Phase==NarrativeDialoguePhase.LineComplete,"actual typewriter reports natural completion to FSM");automatic.Step(.5);Check(automatic.Service.IsRunning,"AUTO waits configured real input-model delay");automatic.Step(.5);Check(!automatic.Service.IsRunning&&automatic.Talked.HasTalked("a"),"service ticks FSM AUTO and finishes without click");
            var background=new Rig("background");background.Config.exit[0].boxTime=1;background.Config.exit[0].undimTime=2;background.Start();background.Step();background.Press();background.Step();background.Press();background.Step(1);
            Check(!background.Service.IsRunning&&background.Dim.Actual.CurrentAmount>.39,"normal early completion leaves undim alive");background.Step(1);Near(background.Dim.Actual.CurrentAmount,.2,"idle service keeps advancing background dim");background.Step(1);Near(background.Dim.Actual.CurrentAmount,0,"background dim completes after running query false");Near(background.Box.Advanced,background.Service.NowSeconds,"service never double advances box time");Near(background.Dim.Advanced,background.Service.NowSeconds,"service never double advances dim time");

            var abort=new Rig("abort");abort.Lock.Held.Add("external");abort.Start();abort.Step();abort.Service.Input.SetAuto(true);abort.Service.Input.SetFastForward(true);abort.Service.Abort();
            Check(!abort.Service.IsRunning&&abort.Box.Hidden&&abort.Dim.Actual.RequestCount==0,"abort immediately cleans view and running state");
            Check(abort.Lock.Held.SetEquals(new[]{"external"})&&abort.Lock.Releases==1,"abort releases only service's dialogue lock");
            Check(abort.Ends.Count==1&&abort.Ends[0].Aborted&&!abort.Talked.HasTalked("a")&&!File.Exists(Path.Combine(abort.Files,"long_loop.json")),"abort event once without talked or file write");abort.Service.Abort();abort.Step();Check(abort.Ends.Count==1,"repeated abort and stale pump cannot repeat event");
            Check(abort.Start()==NarrativeServiceStartStatus.Started&&abort.Service.Input.AutoEnabled&&abort.Service.Input.FastForwardEnabled,"new actual FSM after abort retains same input owner flags");abort.Finish();Check(abort.Talked.HasTalked("a"),"replacement FSM can complete normally after abort");
            var lost=new Rig("lost");lost.Start();lost.Available=false;lost.Step();Check(lost.Ends.Count==1&&lost.Ends[0].Aborted&&!lost.Talked.HasTalked("a"),"runtime unavailable during dialogue safely aborts");
            var dispose=new Rig("dispose");dispose.Start();dispose.Service.Dispose();dispose.Service.Dispose();Check(dispose.Ends.Count==1&&dispose.Ends[0].Aborted&&dispose.Lock.Releases==1,"scene destruction disposal aborts once and releases lock");Check(dispose.Start()==NarrativeServiceStartStatus.Unavailable,"disposed service cannot restart");
            var fresh=new Rig("fresh");Check(!fresh.Service.Input.AutoEnabled&&!fresh.Service.Input.FastForwardEnabled,"new service lifetime starts with toggles off");
            var unsupported=new Rig("unsupported-followup",next:"story");unsupported.Start();unsupported.Finish();Check(!unsupported.Service.IsRunning&&unsupported.Starts.Count==1&&unsupported.Ends.Count==1&&unsupported.Box.Shows==1,"unsupported Story followup cannot enter story presentation");
            var nested=new Rig("nested");nested.Service.DialogueStarted+=item=>{string why;Check(nested.Service.TryStart("b","",nested.Frame,out why)==NarrativeServiceStartStatus.Busy,"event callback cannot reenter FSM start");nested.Service.Abort();};nested.Start();nested.Step();
            Check(nested.Ends.Count==1&&nested.Ends[0].Aborted&&nested.Lock.Releases==1&&!nested.Talked.HasTalked("a")&&nested.Box.Hidden,"abort from request event is deferred until Core stack returns");
            bool badTime=false;try{fresh.Service.Advance(-1,1);}catch(ArgumentOutOfRangeException){badTime=true;}Check(badTime&&fresh.Service.NowSeconds==0,"service game clock rejects backwards delta");
            Console.WriteLine("DIALOGUE_SERVICE_RESULT "+JsonConvert.SerializeObject(new{passed=true,checks=details.Count,actualFsmSchedulerInputPresentationAndPersistence=true,nativeUnity=false,root=root,details=details}));return 0;
        }
        catch(Exception ex){Console.Error.WriteLine("DIALOGUE_SERVICE_FAILED "+ex);return 1;}
    }
    static bool ExistsText(this IReadOnlyList<string> values,string token){foreach(var x in values)if(x.IndexOf(token,StringComparison.OrdinalIgnoreCase)>=0)return true;return false;}
}
