using System;
using System.Collections.Generic;
using System.Linq;
using EatWhat.Narrative.Core;
using EatWhat.Narrative.Unity;
using EatWhat.Tools.Security;
using Newtonsoft.Json;

internal static class NarrativePortraitSequenceTests
{
    static readonly List<string> details=new List<string>();
    static readonly NarrativeCommandSource Empty=new NarrativeCommandSource(NarrativeCommandParser.Parse(""));
    static void Check(bool value,string why){if(!value)throw new Exception(why);details.Add(why);}
    static void Near(double a,double b,string why){Check(Math.Abs(a-b)<.00001,why);}
    sealed class Keys:IKeyProvider{public byte[] GetKey(){return new byte[32];}}
    sealed class Gate:INarrativeDialogueLock{public void Acquire(string id,double now){}public void Release(string id,double now){}}
    sealed class Stage:INarrativePortraitPresentation
    {
        internal sealed class Batch{public bool Alive=true,Exiting;public float Entry,Exit;public NarrativeSpeakerHighlightModel Model;}
        public readonly List<Batch> Batches=new List<Batch>();public readonly List<double> Starts=new List<double>();
        public Func<double> Now;public string Speaker;public double SpeakerAt;public int Clears,SpeakerCalls;
        public object ShowBatch(NarrativePortraitLayoutResult layout,DialoguePresentationSnapshot snapshot,Func<string,NarrativeCharacter> characters,Func<string,object> sprites)
        {
            foreach(var p in layout.Portraits)Check(characters(p.CharacterId)!=null&&sprites(p.CharacterId)!=null,"stage receives original resolved character and image handle");
            var b=new Batch{Model=new NarrativeSpeakerHighlightModel(layout.Portraits.Select(p=>new NarrativeSpeakerHighlightInput(p.CharacterId,p.Side)),snapshot)};
            Batches.Add(b);Starts.Add(Now());return b;
        }
        public void SetEntranceProgress(object handle,float p){if(handle!=null)((Batch)handle).Entry=p;}
        public void SetExitProgress(object handle,float p){if(handle==null)return;var b=(Batch)handle;b.Exiting=true;b.Exit=p;if(p==1)b.Alive=false;}
        public void SetSpeaker(object handle,string id){SpeakerCalls++;Speaker=id;SpeakerAt=Now();if(handle!=null)((Batch)handle).Model.SetSpeaker(id);}
        public void Advance(double seconds){foreach(var b in Batches)if(b.Alive&&!b.Exiting)b.Model.Advance(seconds);}
        public void Clear(){Clears++;foreach(var b in Batches)b.Alive=false;}
        public int Alive=>Batches.Count(b=>b.Alive);
    }
    sealed class Box:INarrativeDialogueBoxPresentation,INarrativeDialogueNameSide
    {
        public event Action<NarrativeLinePresentation> TextCompleted;public Rig Rig;public bool Right;public int Hides;
        public readonly List<double> Starts=new List<double>();public readonly List<bool> Sides=new List<bool>();public readonly List<string> Speakers=new List<string>();
        bool opening;NarrativeLinePresentation currentLine;
        public readonly NarrativeTypewriterModel Writer=new NarrativeTypewriterModel();public int CompleteCalls;
        public void SetExternalTime(bool v){}public void Show(DialoguePresentationSnapshot s){opening=false;Right=false;}
        public void SetBoxProgress(DialoguePresentationSnapshot s,float p){}
        public void SetEntranceProgress(DialoguePresentationSnapshot s,float p){if(!opening){opening=true;Starts.Add(Rig.Now);}}
        public void SetNameSide(bool right){Right=right;}
        public void BeginLine(NarrativeLinePresentation line,string text)
        {Sides.Add(Right);Speakers.Add(line.SpeakerId);if(Rig.WithStage){Check(Rig.Stage.Speaker==line.SpeakerId,"speaker dispatch and text begin share the same call/frame");Near(Rig.Stage.SpeakerAt,Rig.Now,"speaker timestamp equals text timestamp");}currentLine=line;if(Rig.ActualText){if(Writer.Begin(text,line.Context.Snapshot))TextCompleted?.Invoke(line);}else TextCompleted?.Invoke(line);}
        public void CompleteLine(){if(Rig.ActualText){CompleteCalls++;if(Writer.Complete())TextCompleted?.Invoke(currentLine);}}
        public void Advance(double seconds){if(Rig.ActualText&&Writer.Advance(seconds,Rig.Session.Input.FastForwardEnabled))TextCompleted?.Invoke(currentLine);}
        public void Hide(){Hides++;}
    }
    sealed class Dim:INarrativeDialogueDimming
    {
        public readonly HashSet<object> Owners=new HashSet<object>();public readonly List<object> Withdrawals=new List<object>();public readonly List<double> Times=new List<double>();public Func<double> Now;
        public void SetExternalTime(bool v){}public void RequestNormal(object owner,DialoguePresentationSnapshot s,double seconds){Owners.Add(owner);}
        public bool Withdraw(object owner,double seconds){Withdrawals.Add(owner);Times.Add(Now());return Owners.Remove(owner);}
        public void Advance(double seconds){}
    }
    sealed class Composition:INarrativeDialogueComposition,INarrativeDialoguePortraitComposition
    {
        public INarrativePortraitPresentation PortraitStage{get;set;}public NarrativeDialogueEntrance Entrance;public NarrativeDialogueExit Exit;
        public void Acquire(object owner,INarrativeDialogueBoxPresentation box,INarrativeDialogueDimming dim,out NarrativeDialogueEntrance enter,out NarrativeDialogueExit exit)
        {enter=Entrance=new NarrativeDialogueEntrance(box,dim,key=>key);exit=Exit=new NarrativeDialogueExit(enter);}
        public void Release(object owner){}
    }
    sealed class Rig:IDisposable
    {
        public double Now;public int Frame,Ended,Lookups;public bool InRequest,WithStage,ActualText;
        public readonly Stage Stage=new Stage();public readonly Box Box=new Box();public readonly Dim Dim=new Dim();
        public readonly Composition Composition=new Composition();public readonly NarrativeDialogueSession Session;
        public Rig(string path,bool parallel=false,bool withStage=true,bool images=true,bool emptyCast=false,DialogueControlResume resume=DialogueControlResume.AfterBoxClosed,bool chain=false,bool multi=false,bool actualText=false,string firstSpeaker=null)
        {
            ActualText=actualText;WithStage=withStage&&images&&!emptyCast;Box.Rig=this;Stage.Now=()=>Now;Dim.Now=()=>Now;Composition.PortraitStage=withStage?Stage:null;
            var data=new DialoguePresentationData();data.entrance[0].dimTime=1;data.entrance[0].portraitTime=2;data.entrance[0].boxTime=3;
            data.exit[0].boxTime=1;data.exit[0].portraitTime=2;data.exit[0].undimTime=3;data.exit[0].controlResume=resume;
            data.entrance[0].order=data.exit[0].order=parallel?NarrativeCommandExecutionMode.Parallel:NarrativeCommandExecutionMode.Sequential;
            data.text[0].charsPerSecond=data.text[0].advanceCooldown=0;
            if(actualText){data.text[0].charsPerSecond=20;data.speaker[0].transitionTime=2;}
            var ids=new[]{"chr_r","chr_l","chr_c","chr_out"};var chars=ids.Select(id=>new NarrativeCharacter(id,id,id,null,null,1,"","",null,null)).ToArray();
            var cast=emptyCast?new NarrativeCastEntry[0]:new[]{new NarrativeCastEntry(1,ids[0],"",300,0,1,0),new NarrativeCastEntry(2,ids[1],"",-300,0,1,1),new NarrativeCastEntry(3,ids[2],"",.5f,0,1,2)};
            var profiles=new NarrativeProfileNames("","","","","","","","","");
            var lines=new List<NarrativeDialogueLine>();var speakers=multi?new[]{"chr_r","chr_l","chr_c","chr_out",""}:new[]{firstSpeaker??"chr_r"};
            for(int i=0;i<speakers.Length;i++)lines.Add(new NarrativeDialogueLine("a"+i,"a",i+1<speakers.Length?"a"+(i+1):"",speakers[i],"","body",Empty,"","","",""));
            lines.Add(new NarrativeDialogueLine("b0","b","","chr_l","","body",Empty,"","","",""));
            var directory=new NarrativeDataDirectory(chars,new[]{new NarrativeDialogue("a",NarrativeDialogueLevel.Normal,cast,profiles,"","a0",chain?"b":"",Empty),new NarrativeDialogue("b",NarrativeDialogueLevel.Normal,cast,profiles,"","b0","",Empty)},lines);
            var record=NarrativeTalkedPersistence.CreateForCurrentBuild(path,new Keys(),s=>{});
            Session=new NarrativeDialogueSession(()=>directory,(d,p)=>p.Resolve(data,d),Box,Dim,new Gate(),record,s=>{},Composition,
                id=>{Lookups++;return images?(object)id:null;});
            Session.DialogueEnded+=e=>Ended++;
            Composition.Entrance.EntranceCompleted+=c=>Check(!InRequest,"entrance report is outside the FSM request stack");
            Composition.Exit.ExitCompleted+=c=>Check(!InRequest,"exit report is outside the FSM request stack");
        }
        public void Start(){string issue;InRequest=true;try{Check(Session.TryStart("a","chr_r",Frame,out issue)==NarrativeServiceStartStatus.Started,"real FSM starts valid portrait fixture");}finally{InRequest=false;}}
        public void Tick(double seconds){Now+=seconds;Session.Advance(seconds,++Frame);}
        public void Click(){InRequest=true;try{Session.RequestAdvance(++Frame);}finally{InRequest=false;}}
        public void Open(){Start();Tick(1);Tick(2);Tick(3);}
        public void RequestExit(){Tick(0);Click();}
        public void Dispose(){Session.Dispose();}
    }
    static NarrativeSpeakerHighlightState Highlight(Stage.Batch batch,string id)
    {return batch.Model.States.Single(s=>s.CharacterId==id);}
    static void FinalSpeakerWaitCases(string path)
    {
        foreach(string completion in new[]{"natural","complete-click","fast-forward"})
        using(var r=new Rig(path+"-wait-"+completion,actualText:true))
        {
            r.Open();var batch=r.Stage.Batches.Single();var speaker=Highlight(batch,"chr_r");
            Check(r.Session.Phase==NarrativeDialoguePhase.Typing&&!r.Box.Writer.IsComplete,"real typewriter starts final named visible line "+completion);
            int calls=r.Stage.SpeakerCalls;r.Tick(.05);double elapsed=.05;
            Check(speaker.IsFront&&batch.Model.IsTransitioning,"final speaker highlight is already transitioning while typing "+completion);
            if(completion=="natural"){r.Tick(.15);elapsed=.2;}
            else if(completion=="complete-click")
            {r.Click();Check(r.Box.CompleteCalls==1&&r.Box.Writer.IsComplete&&r.Session.Phase==NarrativeDialoguePhase.Typing,"normal typing click completes real model before deferred FSM notification");r.Tick(0);}
            else {r.Session.Input.SetFastForward(true);r.Tick(.05);elapsed=.1;Check(r.Box.CompleteCalls==0,"fast-forward finishes through real typewriter advance without a completion click");}
            Check(r.Box.Writer.IsComplete&&r.Session.Phase==NarrativeDialoguePhase.LineComplete&&r.Session.IsRunning&&r.Ended==0,"completed final line actually waits for next advance "+completion);
            Check(batch.Model.CurrentSpeakerId=="chr_r"&&speaker.IsFront,"final named visible speaker retained while waiting "+completion);
            Check(r.Stage.SpeakerCalls==calls,"CanEnd does not dispatch a new speaker target "+completion);
            Near(speaker.Scale,1+.03*elapsed/2,"text completion retains current partial highlight "+completion);
            r.Tick(.2);elapsed+=.2;
            Near(speaker.Scale,1+.03*elapsed/2,"waiting continues the same scale transition without restart "+completion);
            Near(speaker.Rise,13*elapsed/2,"waiting continues the same rise transition "+completion);
            Near(speaker.ShiftX,-40*elapsed/2,"waiting continues the same inward shift "+completion);
            Near(Highlight(batch,"chr_l").Brightness,1-.5*elapsed/2,"waiting continues listener dim transition "+completion);
            r.Tick(3);Near(speaker.Scale,1.03,"waiting reaches final speaker scale "+completion);
            Check(r.Session.Phase==NarrativeDialoguePhase.LineComplete&&speaker.IsFront&&r.Ended==0,"long wait neither clears speaker nor starts exit "+completion);
            float scale=speaker.Scale,rise=speaker.Rise,shift=speaker.ShiftX,brightness=speaker.Brightness;
            r.Click();Check(r.Session.Phase==NarrativeDialoguePhase.Exiting&&batch.Exiting,"actual next advance starts original exit "+completion);
            r.Tick(.5);Check(speaker.Scale==scale&&speaker.Rise==rise&&speaker.ShiftX==shift&&speaker.Brightness==brightness,"actual exit freezes held final highlight "+completion);
            r.Tick(.5);Check(r.Ended==1&&batch.Alive&&!r.Session.IsRunning,"original early completion leaves old portrait exit alive "+completion);
            r.Tick(2);Check(!batch.Alive&&r.Stage.Alive==0&&r.Ended==1,"original background exit removes final batch once "+completion);
        }
        using(var r=new Rig(path+"-wait-chain",chain:true,actualText:true))
        {
            r.Open();r.Tick(.2);r.Tick(.3);var old=r.Stage.Batches.Single();var oldSpeaker=Highlight(old,"chr_r");float held=oldSpeaker.Scale;
            r.Click();r.Tick(1);Check(r.Session.DialogueId=="b"&&old.Exiting,"real chain begins next segment after final waiting line exits");
            r.Tick(1);Check(r.Stage.Alive==2&&oldSpeaker.Scale==held,"old partial highlight stays frozen while new batch enters");
            r.Tick(1);r.Tick(4);var next=r.Stage.Batches.Last();
            Check(!old.Alive&&next.Model.CurrentSpeakerId=="chr_l"&&Highlight(next,"chr_l").IsFront&&!Highlight(next,"chr_r").IsFront,"next real line gets its own speaker without inheriting old batch highlight");
            r.Tick(.2);Check(r.Session.Phase==NarrativeDialoguePhase.LineComplete&&next.Model.CurrentSpeakerId=="chr_l"&&Highlight(next,"chr_l").IsFront,"next segment's completed named line also retains its own speaker");
            Check(old.Model.CurrentSpeakerId=="chr_r"&&oldSpeaker.Scale==held,"next segment never rewrites detached old batch target or frozen scale");
            r.Session.Abort();Check(r.Stage.Alive==0&&!r.Session.IsRunning,"abort still clears current and detached batches after final wait");
        }
        foreach(string id in new[]{"","chr_out"})using(var r=new Rig(path+"-wait-nonvisible-"+id,actualText:true,firstSpeaker:id))
        {
            r.Open();r.Tick(.2);r.Tick(2);var batch=r.Stage.Batches.Single();
            Check(r.Session.Phase==NarrativeDialoguePhase.LineComplete&&batch.Model.States.All(s=>!s.IsFront),"narration/off-cast final wait still has no visible highlighted speaker "+id);
            Check(batch.Model.States.All(s=>Math.Abs(s.Brightness-.5)<.00001&&s.Scale==1&&s.Rise==0&&s.ShiftX==0),"narration/off-cast final wait keeps existing listener presentation "+id);
            r.Session.Abort();Check(r.Stage.Alive==0,"narration/off-cast abort still clears stage "+id);
        }
        using(var r=new Rig(path+"-wait-no-stage",withStage:false,actualText:true))
        {
            int acknowledgements=0;r.Composition.Entrance.UnsupportedCompleted+=(request,c,l)=>{if(request=="can-end")acknowledgements++;};
            r.Open();r.Tick(.2);Check(r.Session.Phase==NarrativeDialoguePhase.LineComplete&&acknowledgements==0,"no-stage CanEnd acknowledgement remains deferred");
            r.Tick(0);Check(acknowledgements==1&&r.Session.IsRunning&&r.Ended==0,"no-stage original acknowledgement is retained without ending dialogue");
        }
    }
    static int Main(string[] args)
    {
        try
        {
            using(var r=new Rig(args[0]+"-seq"))
            {r.Start();Check(r.Stage.Starts.Count==0&&r.Box.Starts.Count==0,"sequential starts only dim");r.Tick(1);Near(r.Stage.Starts[0],1,"portrait starts after dim");Check(r.Box.Starts.Count==0,"box waits for portrait duration");r.Tick(2);Near(r.Box.Starts[0],3,"box starts after portrait");r.Tick(3);Check(r.Box.Sides.Count==1&&r.Box.Sides[0],"right speaker name side set before first text");Check(r.Lookups==3,"one image/layout preparation per segment");r.RequestExit();Check(r.Ended==0,"exit request never reports synchronously");r.Tick(1);Check(r.Ended==1&&r.Stage.Alive==1,"AfterBoxClosed releases FSM while old portrait remains");r.Tick(2);Check(r.Stage.Alive==0,"background portrait completes after early report");Near(r.Dim.Times.Last(),9,"sequential undim starts after box and portrait");r.Tick(3);Check(r.Ended==1,"background completion never reports twice");}
            using(var r=new Rig(args[0]+"-parallel",true,true,true,false,DialogueControlResume.AfterCameraReturned))
            {r.Start();Near(r.Stage.Starts[0],0,"parallel portrait starts with dim");Near(r.Box.Starts[0],0,"parallel box starts with dim");r.Tick(3);Check(r.Box.Sides.Count==1&&r.Stage.Batches[0].Entry==1,"parallel entrance waits longest step");r.RequestExit();r.Tick(1);Check(r.Ended==0,"full resume waits after box closes");r.Tick(1);Check(r.Stage.Alive==0&&r.Ended==0,"full resume still waits after portrait exits");r.Tick(1);Check(r.Ended==1,"full resume reports after all parallel steps");}
            foreach(int mode in new[]{0,1,2})using(var r=new Rig(args[0]+"-empty"+mode,false,mode!=0,mode!=1,mode==2))
            {r.Start();r.Tick(1);r.Tick(3);Check(r.Box.Sides.Count==1&&r.Stage.Starts.Count==0,"no-stage/no-images/empty-cast remove portrait wait "+mode);r.RequestExit();r.Tick(1);r.Tick(3);Check(r.Ended==1,"no-portrait exit uses original durations "+mode);}
            using(var r=new Rig(args[0]+"-skip"))
            {r.Start();r.Click();Check(r.Box.Sides.Count==1&&r.Stage.Batches[0].Entry==1,"entrance skip places portrait immediately before line");r.Tick(0);Check(r.Box.Sides.Count==1,"stale deferred entrance report cannot duplicate line");r.Session.Abort();Check(r.Stage.Alive==0,"abort immediately clears stage");}
            using(var r=new Rig(args[0]+"-sides",multi:true))
            {r.Open();for(int i=0;i<4;i++){r.Tick(0);r.Click();}Check(r.Box.Sides.SequenceEqual(new[]{true,false,false,false,false}),"right/left/center/off-cast/narration side selection");Check(r.Box.Speakers.SequenceEqual(new[]{"chr_r","chr_l","chr_c","chr_out",""}),"original speaker IDs are forwarded unchanged");}
            using(var r=new Rig(args[0]+"-chain",chain:true))
            {r.Open();r.RequestExit();var ownerA=r.Composition.Entrance.DimmingOwnerToken;r.Tick(1);Check(r.Ended==1&&r.Session.DialogueId=="b"&&r.Stage.Alive==1,"actual FSM chains before old batch exits");var ownerB=r.Composition.Entrance.DimmingOwnerToken;Check(!ReferenceEquals(ownerA,ownerB),"new segment owns a separate dim request");r.Tick(1);Check(r.Stage.Alive==2,"new batch enters alongside old exit, including shared characters");r.Tick(1);Check(r.Stage.Alive==1&&r.Dim.Owners.Contains(ownerB),"old undim cannot withdraw new segment dim owner");r.Tick(4);Check(r.Box.Speakers.Last()=="chr_l","next segment starts its own line normally");r.Session.Abort();Check(r.Stage.Alive==0,"chain abort removes current and detached batches");}
            int legacyChecks=details.Count;Console.WriteLine("LEGACY_PORTRAIT_SEQUENCE_PASS checks="+legacyChecks);
            FinalSpeakerWaitCases(args[0]);
            Console.WriteLine(JsonConvert.SerializeObject(new{result="PORTRAIT_SEQUENCE_PASS",checks=details.Count,legacyChecks,details,nativeUnity=false},Formatting.Indented));return 0;
        }
        catch(Exception e){Console.Error.WriteLine(e);return 1;}
    }
}
