using System;
using System.Collections.Generic;
using System.Reflection;
using EatWhat.LongLoop;
using EatWhat.Narrative.Core;
using EatWhat.Narrative.Unity;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class NarrativeStreetReactionAdapterTests
{
    static readonly List<string> details=new List<string>();static int sceneId=2200;
    static void Check(bool v,string s){if(!v)throw new Exception(s);details.Add(s);}
    static void Life(object v,string method){v.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(v,null);}
    static DialoguePresentationSnapshot Snapshot(float delay=.5f,DialogueTurnMode mode=DialogueTurnMode.FaceSpeaker)
    {
        var data=new DialoguePresentationData();data.reaction[0].turnDelay=delay;data.reaction[0].turnMode=mode;
        var dialogue=new NarrativeDialogue("dlg_react",NarrativeDialogueLevel.Normal,null,new NarrativeProfileNames("","","","","","","","",""),"","","",new NarrativeCommandSource(NarrativeCommandParser.Parse("")));
        var r=DialoguePresentationResolver.Resolve(data,dialogue);if(!r.Success)throw new Exception("real snapshot resolution");return r.Snapshot;
    }
    sealed class Fixture
    {
        public readonly Scene Scene; public readonly GameObject Root;
        public readonly NarrativeDialogueService Service; public readonly NarrativeTargetRegistry Registry;
        public readonly Camera Camera; public readonly NarrativeStreetReactionAdapter Adapter;
        public readonly LongLoopPlayerController Player;public readonly NpcShell Init,BaseNpc,Listener,Other,Offscreen;
        public Fixture(float delay=.5f,DialogueTurnMode mode=DialogueTurnMode.FaceSpeaker)
        {
            Scene=new Scene(++sceneId,"ReactionFixture"+sceneId);Root=new GameObject(Scene,"service");
            Service=Root.AddComponent<NarrativeDialogueService>();Service.CurrentSnapshot=Snapshot(delay,mode);
            Registry=Root.AddComponent<NarrativeTargetRegistry>();Camera=Root.AddComponent<Camera>();
            var player=new GameObject(Scene,"player");Player=player.AddComponent<LongLoopPlayerController>();Player.IsFacingRight=false;
            player.AddComponent<NarrativeActorMarker>().ConfigureId("chr_player");Registry.ConfigurePlayer(player.transform);
            Init=Npc("chr_npc#2",4,true);BaseNpc=Npc("chr_npc",-4,false);Listener=Npc("chr_listener",2,false);
            Other=Npc("chr_other",-8,true);Offscreen=Npc("chr_off",20,false);
            Camera.Project=p=>new Vector3(p.x>15?1.1f:.5f,.5f,1);
            Registry.CollectSceneMarkers();Adapter=Root.AddComponent<NarrativeStreetReactionAdapter>();Adapter.Configure(Service,Registry,Camera,Player);
        }
        public NpcShell Npc(string id,float x,bool facing)
        {
            var n=new GameObject(Scene,id);n.transform.position=new Vector3(x,0,0);
            var f=new GameObject(Scene,"foot");f.transform.SetParent(n.transform,false);f.transform.localPosition=new Vector3(0,-2,0);
            var v=new GameObject(Scene,"visual");v.transform.SetParent(n.transform,false);
            var h=new GameObject(Scene,"head");h.transform.SetParent(n.transform,false);
            var shell=n.AddComponent<NpcShell>();shell.ConfigureReferences(f.transform,v.transform,h.transform);shell.SetFacing(facing);
            n.AddComponent<NarrativeActorMarker>().ConfigureId(id);return shell;
        }
        public void Start(string id="chr_npc#2"){string issue;Service.TryStart("dlg_react",id,out issue);}
        public void Tick(float seconds){Time.deltaTime=seconds;Life(Adapter,"Update");}
        public void Close(){Life(Adapter,"OnDisable");}
    }
    static Transform BindFoot(Fixture f,Transform parent=null)
    {
        f.Player.gameObject.name="PlayerRoot_TUNE";
        var foot=new GameObject(f.Scene,"Foot_TUNE");foot.transform.SetParent(parent??f.Player.transform,false);
        f.Registry.ConfigurePlayer(foot.transform);return foot.transform;
    }
    static void FootOwnerCases()
    {
        // Same saved hierarchy, with a deliberately different foot X to expose root-position sampling.
        var authored=new Fixture(0);var foot=BindFoot(authored);
        authored.Player.transform.position=new Vector3(10,0,0);foot.localPosition=new Vector3(-12,-2,0);
        foot.gameObject.AddComponent<NarrativeActorMarker>().ConfigureId("chr_foot_decoy");
        var rootPosition=authored.Player.transform.position;var footPosition=foot.position;
        authored.Adapter.Configure(authored.Service,authored.Registry,authored.Camera,authored.Player);authored.Start();
        Check(authored.Adapter.IsReacting&&authored.Player.IsFacingRight&&!authored.Init.IsFacingRight&&authored.Player.FacingCalls==1,"authored Foot_TUNE binds its explicit PlayerRoot_TUNE controller and starts mutual facing");
        Transform registered;Check(authored.Registry.TryGetPlayer(out registered)&&registered==foot,"owner resolution preserves the registry Foot_TUNE binding");
        NarrativeStreetReactionActor actor;
        Check(authored.Adapter.TryGetActor("chr_player",out actor)&&actor.ActorId=="chr_player"&&actor.X==foot.position.x&&actor.X!=authored.Player.transform.position.x,"player sample uses owning controller marker and registry foot X rather than root or foot marker");
        Check(authored.Player.transform.position.Equals(rootPosition)&&foot.position.Equals(footPosition),"child-foot initial facing moves neither root nor foot");
        authored.Service.RaiseLine("chr_npc");
        Check(authored.Listener.IsFacingRight,"child-foot NPC speaker establishes the listener's first original facing through production rules");
        authored.Service.RaiseLine("chr_player");
        Check(!authored.Listener.IsFacingRight&&authored.Player.FacingCalls==1,"player-speaking listeners face foot position while player is not commanded again");
        authored.Service.End();Check(!authored.Adapter.IsReacting&&authored.Init.IsFacingRight&&!authored.Listener.IsFacingRight&&authored.Player.IsFacingRight&&authored.Player.FacingCalls==1,"child-foot whole end restores original NPC facing without restoring player");authored.Close();

        var automatic=new Fixture();var group=new GameObject(automatic.Scene,"ground anchors");group.transform.SetParent(automatic.Player.transform,false);
        BindFoot(automatic,group.transform);automatic.Adapter.Configure(automatic.Service,automatic.Registry,automatic.Camera,null);
        automatic.Adapter.Configure(automatic.Service,automatic.Registry,automatic.Camera);automatic.Start();
        Check(automatic.Adapter.IsReacting&&automatic.Player.FacingCalls==1,"unassigned legacy controller resolves through intermediate foot parents");automatic.Close();

        var mismatch=new Fixture();BindFoot(mismatch);
        var wrong=new GameObject(mismatch.Scene,"other protagonist").AddComponent<LongLoopPlayerController>();wrong.gameObject.AddComponent<NarrativeActorMarker>().ConfigureId("chr_player");
        mismatch.Adapter.Configure(mismatch.Service,mismatch.Registry,mismatch.Camera,wrong);mismatch.Start();mismatch.Tick(1);
        Check(!mismatch.Adapter.IsReacting&&mismatch.Player.FacingCalls==0&&wrong.FacingCalls==0&&mismatch.Init.IsFacingRight,"child-foot rejects a different same-scene controller even with the same character id");mismatch.Close();

        var noMarker=new Fixture();var missingFoot=BindFoot(noMarker);UnityEngine.Object.Destroy(noMarker.Player.GetComponent<NarrativeActorMarker>());
        missingFoot.gameObject.AddComponent<NarrativeActorMarker>().ConfigureId("chr_player");
        var outer=new GameObject(noMarker.Scene,"outer protagonist");var outerController=outer.AddComponent<LongLoopPlayerController>();outer.AddComponent<NarrativeActorMarker>().ConfigureId("chr_player");
        noMarker.Player.transform.SetParent(outer.transform,false);
        noMarker.Adapter.Configure(noMarker.Service,noMarker.Registry,noMarker.Camera,null);noMarker.Start();noMarker.Tick(1);
        Check(!noMarker.Adapter.IsReacting&&noMarker.Player.FacingCalls==0&&outerController.FacingCalls==0&&noMarker.Init.IsFacingRight,"missing nearest-owner marker cannot borrow a foot or outer-controller marker");noMarker.Close();

        var emptyMarker=new Fixture();BindFoot(emptyMarker);emptyMarker.Player.GetComponent<NarrativeActorMarker>().ConfigureId("");emptyMarker.Start();emptyMarker.Tick(1);
        Check(!emptyMarker.Adapter.IsReacting&&emptyMarker.Player.FacingCalls==0,"child-foot rejects an empty owning-controller character id");emptyMarker.Close();

        var unowned=new Fixture();var orphan=new GameObject(unowned.Scene,"unowned foot");orphan.AddComponent<NarrativeActorMarker>().ConfigureId("chr_player");unowned.Registry.ConfigurePlayer(orphan.transform);
        unowned.Adapter.Configure(unowned.Service,unowned.Registry,unowned.Camera,null);unowned.Start();unowned.Tick(1);
        Check(!unowned.Adapter.IsReacting&&unowned.Player.FacingCalls==0,"unowned foot never searches another scene root for a controller");unowned.Close();

        var foreign=new Fixture();var foreignFoot=new GameObject(new Scene(++sceneId,"ForeignFoot"),"Foot_TUNE");foreign.Registry.ConfigurePlayer(foreignFoot.transform);
        foreign.Start();foreign.Tick(1);Check(!foreign.Adapter.IsReacting&&foreign.Player.FacingCalls==0,"cross-scene registry foot remains rejected with an explicit local controller");foreign.Close();

        var inactive=new Fixture();BindFoot(inactive);inactive.Player.enabled=false;inactive.Start();inactive.Tick(1);
        Check(!inactive.Adapter.IsReacting&&inactive.Player.FacingCalls==0,"child-foot rejects its disabled owning controller");inactive.Close();
    }
    static int Main()
    {
        try
        {
            var f=new Fixture();var position=f.Player.transform.position;f.Adapter.Configure(f.Service,f.Registry,f.Camera);f.Start();
            Check(!f.Init.IsFacingRight&&f.Player.IsFacingRight&&f.Player.FacingCalls==1,"first start faces actual initiator and player once; repeated binding is idempotent");
            Check(f.Player.transform.position.Equals(position),"start facing never moves player");
            f.Service.RaiseLine("chr_npc");f.Tick(0);Check(!f.Listener.IsFacingRight,"scaled zero delta pauses pending turn");
            f.Tick(.25f);Check(!f.Listener.IsFacingRight,"positive partial delay remains pending");f.Tick(.25f);
            Check(f.Listener.IsFacingRight,"same-base speaker resolves initiating #2 instance rather than unsuffixed NPC");
            Check(!f.Init.IsFacingRight&&f.Player.FacingCalls==1,"delayed listeners exclude initiator and player");
            Check(!f.Offscreen.IsFacingRight,"offscreen foot does not turn");
            f.Service.RaiseLine("chr_other");f.Tick(.5f);Check(!f.Listener.IsFacingRight,"other speaker uses exact unsuffixed registry actor");
            f.Listener.SetFacing(true);f.Service.RaiseLine("chr_player");f.Tick(.5f);
            Check(!f.Listener.IsFacingRight&&f.Player.FacingCalls==1,"player character resolves formal player; later lines never command player");
            f.Service.End();Check(f.Init.IsFacingRight&&!f.BaseNpc.IsFacingRight&&!f.Listener.IsFacingRight&&f.Other.IsFacingRight,"whole end restores each NPC first original facing");
            Check(f.Player.IsFacingRight&&f.Player.FacingCalls==1&&!f.Adapter.IsReacting,"player excluded from restoration and rule run ends");f.Close();

            var right=new Fixture();right.Player.transform.position=new Vector3(10,0,0);right.Player.IsFacingRight=true;right.Init.SetFacing(false);right.Start();
            Check(right.Init.IsFacingRight&&!right.Player.IsFacingRight,"opposite initial positions face each other via same owners");right.Service.End(true);
            Check(!right.Init.IsFacingRight&&!right.Player.IsFacingRight,"abort restores NPC without restoring player");right.Close();

            var chain=new Fixture();chain.Start();chain.Service.RaiseLine("chr_npc");chain.Tick(.5f);chain.Init.SetFacing(true);
            chain.Service.Chain("dlg_next",Snapshot(0,DialogueTurnMode.None));
            Check(chain.Init.IsFacingRight&&chain.Listener.IsFacingRight&&chain.Player.FacingCalls==1&&chain.Adapter.IsReacting,"chained start neither restores nor repeats mutual facing");
            chain.Service.RaiseLine("chr_other");chain.Tick(1);Check(chain.Listener.IsFacingRight,"next segment reads None mode from current snapshot");
            chain.Service.Chain("dlg_third",Snapshot(0));chain.Service.RaiseLine("chr_other");
            Check(!chain.Listener.IsFacingRight,"next snapshot zero delay turns in line notification itself");
            chain.Service.End();Check(!chain.Listener.IsFacingRight&&chain.Init.IsFacingRight,"chained segments retain first original NPC values");chain.Close();

            var none=new Fixture(0,DialogueTurnMode.None);none.Start();none.Service.RaiseLine("chr_npc");none.Tick(5);
            Check(!none.Init.IsFacingRight&&none.Player.IsFacingRight&&!none.Listener.IsFacingRight,"None mode skips listener turns while retaining initial mutual facing");none.Close();

            var missing=new Fixture();missing.Npc("chr_only_suffix#3",-10,true);missing.Start();missing.Service.RaiseLine("chr_npc");missing.Tick(.25f);
            missing.Service.RaiseLine("chr_only_suffix");missing.Tick(5);Check(!missing.Listener.IsFacingRight,"missing exact speaker cancels previous timer without suffix guessing");
            missing.Service.RaiseLine("");missing.Tick(5);Check(!missing.Listener.IsFacingRight,"narration has no street actor and no turn");
            missing.Listener.SetFacing(true);missing.Service.RaiseLine("chr_npc");missing.Tick(.25f);missing.Service.RaiseLine("chr_other");missing.Tick(.25f);
            Check(missing.Listener.IsFacingRight,"speaker switch discards previous partial timer");missing.Tick(.25f);
            Check(!missing.Listener.IsFacingRight,"replacement timer turns toward latest actor");missing.Close();

            var live=new Fixture();live.Start();live.Service.RaiseLine("chr_npc");live.Tick(.25f);
            var late=live.Npc("chr_late",3,false);live.Listener.gameObject.SetActive(false);live.Tick(.25f);
            Check(late.IsFacingRight&&!live.Listener.IsFacingRight,"due turn samples newly registered NPCs and ignores inactive objects");
            live.Close();Check(!late.IsFacingRight&&live.Init.IsFacingRight,"disable restores sampled live NPC identities and initiator");
            int calls=live.Player.FacingCalls;live.Service.End();live.Start();Check(live.Player.FacingCalls==calls&&!live.Adapter.IsReacting,"disabled adapter unsubscribes without leaked start callbacks");

            var visible=new Fixture(0);visible.Start();Vector3 sampled=new Vector3();visible.Camera.Project=p=>{sampled=p;return new Vector3(.5f,.5f,1);};
            NarrativeStreetReactionActor actor;visible.Adapter.TryGetActor("chr_listener",out actor);
            Check(sampled.Equals(visible.Listener.Foot.position)&&actor.IsOnScreen,"camera receives foot point rather than visual/root");
            foreach(var view in new[]{new Vector3(-.01f,.5f,1),new Vector3(1.01f,.5f,1),new Vector3(.5f,-.01f,1),new Vector3(.5f,1.01f,1),new Vector3(.5f,.5f,-1),new Vector3(.5f,.5f,1001)})
            {visible.Camera.Project=p=>view;visible.Adapter.TryGetActor("chr_listener",out actor);Check(!actor.IsOnScreen,"viewport/clip exclusion "+details.Count);}
            visible.Camera.Project=p=>new Vector3(0,1,1);visible.Adapter.TryGetActor("chr_listener",out actor);Check(actor.IsOnScreen,"viewport boundary point remains in visible range");visible.Close();

            foreach(int binding in new[]{0,1,2})
            {
                var unavailable=new Fixture();int warnings=Debug.Warnings.FindAll(s=>s.StartsWith("NarrativeStreetReactionAdapter:")).Count;
                unavailable.Adapter.Configure(binding==0?null:unavailable.Service,binding==1?null:unavailable.Registry,binding==2?null:unavailable.Camera);
                unavailable.Tick(1);unavailable.Tick(1);unavailable.Start();
                Check(!unavailable.Adapter.IsReacting&&unavailable.Player.FacingCalls==0&&unavailable.Init.IsFacingRight,"missing binding "+binding+" leaves street unchanged");
                Check(Debug.Warnings.FindAll(s=>s.StartsWith("NarrativeStreetReactionAdapter:")).Count==warnings+1,"missing binding emits one lifetime diagnostic "+binding);unavailable.Close();
            }
            var lost=new Fixture(0);lost.Start();lost.Service.RaiseLine("chr_npc");lost.Registry.enabled=false;lost.Tick(0);
            Check(lost.Init.IsFacingRight&&!lost.Listener.IsFacingRight&&!lost.Adapter.IsReacting,"binding loss restores via cached object identities despite disabled registry");lost.Close();
            var aborted=new Fixture(0);aborted.Start();aborted.Service.RaiseLine("chr_npc");aborted.Service.End(true);
            Check(!aborted.Adapter.IsReacting&&aborted.Init.IsFacingRight&&!aborted.Listener.IsFacingRight&&aborted.Player.IsFacingRight,"aborted conversation event restores NPCs immediately and never restores player");aborted.Close();
            var explicitPlayer=new Fixture();explicitPlayer.Start();Check(explicitPlayer.Adapter.IsReacting&&explicitPlayer.Player.FacingCalls==1,"fourth explicit player reference matches registry and starts normally");explicitPlayer.Close();
            var legacy=new Fixture();legacy.Adapter.Configure(legacy.Service,legacy.Registry,legacy.Camera,null);legacy.Adapter.Configure(legacy.Service,legacy.Registry,legacy.Camera);legacy.Start();
            Check(legacy.Adapter.IsReacting&&legacy.Player.FacingCalls==1,"unassigned fourth field preserves legacy three-argument registry-based calls");legacy.Close();
            var mismatch=new Fixture();var wrong=new GameObject(mismatch.Scene,"wrong player").AddComponent<LongLoopPlayerController>();
            mismatch.Adapter.Configure(mismatch.Service,mismatch.Registry,mismatch.Camera,wrong);mismatch.Start();mismatch.Tick(1);
            Check(!mismatch.Adapter.IsReacting&&wrong.FacingCalls==0&&mismatch.Player.FacingCalls==0,"same-scene fourth reference must be registry player's controller");mismatch.Close();
            var foreign=new Fixture();var otherPlayer=new GameObject(new Scene(2999,"Foreign"),"foreign player").AddComponent<LongLoopPlayerController>();
            foreign.Adapter.Configure(foreign.Service,foreign.Registry,foreign.Camera,otherPlayer);foreign.Start();foreign.Tick(1);
            Check(!foreign.Adapter.IsReacting&&otherPlayer.FacingCalls==0&&foreign.Player.FacingCalls==0,"other-scene fourth reference is rejected without writes");foreign.Close();
            FootOwnerCases();
            Console.WriteLine(JsonConvert.SerializeObject(new{result="STREET_REACTION_ADAPTER_PASS",checks=details.Count,details,nativeUnity=false},Formatting.Indented));return 0;
        }
        catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
    }
}
