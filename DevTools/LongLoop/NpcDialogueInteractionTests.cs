using System;
using System.Collections.Generic;
using System.Reflection;
using EatWhat.LongLoop;
using EatWhat.Narrative.Core;
using EatWhat.Narrative.Unity;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class NpcDialogueInteractionTests
{
    static readonly List<string> details=new List<string>();
    static void Check(bool value,string reason){if(!value)throw new Exception(reason);details.Add(reason);}
    static void Near(double a,double b,string reason){Check(Math.Abs(a-b)<1e-5,reason);}
    static void Set(object value,string name,object field){value.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(value,field);}
    static GameObject Child(GameObject parent,string name){var value=new GameObject(parent.scene,name);value.transform.SetParent(parent.transform,false);return value;}
    static void Rows(CK01GeneratedDataCatalog c,string table,params ScriptableObject[] rows){c.Rows[table]=new List<ScriptableObject>(rows);}
    sealed class Prompt:IStreetInteractionPrompt
    {
        public Transform Target;public string Key;public int Shows,Hides;public bool Accept=true;
        public bool Show(Transform anchor,string key){Shows++;if(!Accept)return false;Target=anchor;Key=key;return true;}
        public void Hide(){Target=null;Hides++;}
    }
    sealed class Input { public bool Pressed;public int Reads;public bool Read(){Reads++;return Pressed;} }
    sealed class Generic:IStreetInteractable
    {
        public Transform Foot;public int Starts;public double Radius=1;public bool Available=true;
        public string InteractionId{get{return "generic";}}public Transform InteractionFoot{get{return Foot;}}public Transform PromptAnchor{get{return Foot;}}
        public double InteractRadius{get{return Radius;}}public string PromptTextKey{get{return "fixture.open";}}public bool IsAvailable{get{return Available;}}
        public bool Interact(){Starts++;return true;}
    }
    sealed class Npc
    {
        public GameObject Root;public NpcShell Shell;public NarrativeActorMarker Marker;public NarrativeDialogueTrigger Trigger;
    }
    static Npc Make(Scene scene,NarrativeRuntimeEntry runtime,NarrativeDialogueService service,string id,string own,string marker="talk",string template="")
    {
        var root=new GameObject(scene,id);var shell=root.AddComponent<NpcShell>();shell.ConfigureReferences(Child(root,"foot").transform,Child(root,"visual").transform,Child(root,"head").transform);
        shell.ConfigurePresentationAnchors(null,Child(root,"prompt").transform,null);var actor=root.AddComponent<NarrativeActorMarker>();actor.ConfigureId(id);
        var trigger=root.AddComponent<NarrativeDialogueTrigger>();Set(trigger,"dialogueId",own);Set(trigger,"markerValue",marker);Set(trigger,"triggerTemplate",template);trigger.ConfigureReferences(runtime,service,actor,shell);
        return new Npc{Root=root,Shell=shell,Marker=actor,Trigger=trigger};
    }
    sealed class Surface:INarrativeBubbleSurface { public bool Disposed;public bool Apply(float alpha,float rise){return true;}public void Dispose(){Disposed=true;} }
    sealed class Factory:INarrativeBubbleSurfaceFactory
    {
        public readonly List<Surface> Created=new List<Surface>();
        public INarrativeBubbleSurface Create(Transform head,string key,string text,out string diagnostic){diagnostic=null;var value=new Surface();Created.Add(value);return value;}
    }
    static void BubbleTick(NarrativeAmbientBubbleTrigger value,double seconds){Time.timeAsDouble=seconds;value.GetType().GetMethod("Update",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(value,null);}
    static NarrativeAmbientBubbleDriver Driver(NarrativeAmbientBubbleTrigger value){return (NarrativeAmbientBubbleDriver)value.GetType().GetField("driver",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(value);}
    static int Main()
    {
        try
        {
            var scene=new Scene(931,"InteractionFixture");var managers=new GameObject(scene,"managers");var runtime=managers.AddComponent<NarrativeRuntimeEntry>();var service=managers.AddComponent<NarrativeDialogueService>();
            var registry=managers.AddComponent<NarrativeTargetRegistry>();var player=new GameObject(scene,"player").transform;registry.ConfigurePlayer(player);
            var config=new DialoguePresentationConfig();config.Data.trigger[0].interactRadius=2.1f;config.Data.trigger[0].autoBubbleRadius=10;config.Data.trigger[0].autoBubbleCooldown=5;
            config.Data.trigger=new[]{config.Data.trigger[0],new DialogueTriggerConfig{name="small",interactRadius=.7f}};
            var catalog=new CK01GeneratedDataCatalog();Rows(catalog,"Characters",new CK01CharacterData{character_id="chr_npc"});
            Rows(catalog,"Dialogues",new CK01DialogueData{dialogue_id="dlg_normal",tier="normal",first_line="ln_normal"},new CK01DialogueData{dialogue_id="dlg_alt",tier="normal",first_line="ln_alt"},
                new CK01DialogueData{dialogue_id="dlg_story",tier="story",first_line="ln_story"},new CK01DialogueData{dialogue_id="dlg_ambient",tier="ambient",first_line="ln_ambient"});
            Rows(catalog,"DialogueLines",new CK01DialogueLineData{line_id="ln_normal",dialogue_id="dlg_normal",text_key="normal"},new CK01DialogueLineData{line_id="ln_alt",dialogue_id="dlg_alt",text_key="alt"},
                new CK01DialogueLineData{line_id="ln_story",dialogue_id="dlg_story",text_key="story"},new CK01DialogueLineData{line_id="ln_ambient",dialogue_id="dlg_ambient",text_key="ambient"});
            Rows(catalog,"AmbientPools",new CK01AmbientPoolData{pool_id="amb_test",dialogue_ids=new List<string>{"dlg_ambient"}});
            runtime.Configure(config,catalog);Check(runtime.IsAvailable,"actual runtime/adapter/resolver available from isolated fixture rows");
            var far=Make(scene,runtime,service,"chr_npc#2","dlg_normal");var near=Make(scene,runtime,service,"chr_npc#3","dlg_normal");
            far.Shell.Foot.position=new Vector3(1.8f,0,0);near.Shell.Foot.position=new Vector3(.8f,0,0);registry.CollectSceneMarkers();
            Check(!far.Trigger.HasActualBinding&&far.Trigger.ActualDialogueId=="dlg_normal"&&far.Trigger.ActualMarkerValue=="talk","unwritten actual values equal authored instance values");
            far.Trigger.SetActualBinding("",null);Check(far.Trigger.HasActualBinding&&far.Trigger.ActualDialogueId=="dlg_normal"&&far.Trigger.ActualMarkerValue=="talk","blank scene fields inherit authored instance values");
            far.Trigger.SetActualBinding("dlg_alt","none");Check(far.Trigger.ActualDialogueId=="dlg_alt"&&far.Trigger.ActualMarkerValue=="none"&&far.Trigger.InstanceDialogueId=="dlg_normal","explicit state values override without changing instance authoring; none stays explicit");
            far.Trigger.ClearActualBinding();Check(!far.Trigger.HasActualBinding&&far.Trigger.ActualDialogueId=="dlg_normal","clearing actual binding restores instance fallback");
            Near(far.Trigger.InteractRadius,2.1,"default interact radius comes from actual trigger resolver");
            var small=Make(scene,runtime,service,"chr_npc#4","dlg_normal",template:"small");Near(small.Trigger.InteractRadius,.7,"named trigger template selects actual radius");

            var targets=new List<IStreetInteractable>{far.Trigger,near.Trigger};var prompt=new Prompt();var input=new Input();bool locked=false;int enumerations=0,playerReads=0;
            var selection=new StreetInteractionSelection(()=>locked||service.IsRunning,()=>{playerReads++;return player;},()=>{enumerations++;return targets;},input.Read,prompt,()=>"fixture.talk");
            selection.Advance(1);Check(ReferenceEquals(selection.Selected,near.Trigger)&&prompt.Target==near.Shell.PromptAnchor&&prompt.Target!=near.Shell.HeadAnchor,"overlapping NPCs choose nearest and its own dedicated prompt anchor");
            Check(prompt.Key=="fixture.talk"&&input.Reads==1,"selector-configured localization key and one input read");int lists=enumerations,feet=playerReads,reads=input.Reads;
            locked=true;selection.Advance(2);Check(selection.Selected==null&&prompt.Target==null&&enumerations==lists&&playerReads==feet&&input.Reads==reads,"lock gate clears prompt and skips all candidate/geometry/input work");
            locked=false;string issue;service.TryStart("dlg_normal","chr_npc#9",out issue);selection.Advance(3);Check(selection.Selected==null&&enumerations==lists&&input.Reads==reads,"dialogue gate likewise skips later work");service.End();
            far.Shell.Foot.position=near.Shell.Foot.position=new Vector3(10,0,0);selection.Advance(4);Check(selection.Selected==null&&prompt.Target==null&&input.Reads==reads,"outside radius hides prompt without reading Interact");
            far.Shell.Foot.position=near.Shell.Foot.position=new Vector3(1,0,0);selection.Advance(5);Check(ReferenceEquals(selection.Selected,near.Trigger),"equal ground distances keep previous stable ID through temporary absence");
            far.Shell.Foot.position=new Vector3(.5f,0,0);near.Shell.Foot.position=new Vector3(1.2f,0,0);selection.Advance(6);Check(ReferenceEquals(selection.Selected,far.Trigger)&&prompt.Target==far.Shell.PromptAnchor,"closer target changes prompt to correct NPC");
            far.Root.SetActive(false);selection.Advance(7);Check(ReferenceEquals(selection.Selected,near.Trigger),"inactive NPC is excluded");far.Root.SetActive(true);
            far.Trigger.SetActualBinding("dlg_alt","none");input.Pressed=true;int starts=service.Starts;selection.Advance(8);
            Check(service.Starts==starts+1&&service.LastDialogueId=="dlg_alt"&&service.InitiatorId=="chr_npc#2","one press starts selected actual dialogue with complete instance suffix");
            Check(selection.Selected==null&&prompt.Target==null,"successful start immediately clears interaction prompt");reads=input.Reads;selection.Advance(8);Check(service.Starts==starts+1&&input.Reads==reads,"same-frame repeated selector call cannot start or read input twice");
            service.End();input.Pressed=false;selection.Advance(9);Check(ReferenceEquals(selection.Selected,far.Trigger)&&prompt.Target==far.Shell.PromptAnchor,"interaction and prompt restore after dialogue ends");

            prompt.Accept=false;near.Shell.Foot.position=new Vector3(.25f,0,0);selection.Advance(10);
            Check(ReferenceEquals(selection.Selected,near.Trigger)&&prompt.Target==null,"failed replacement prompt hides previous NPC prompt");
            prompt.Accept=true;selection.Advance(11);Check(prompt.Target==near.Shell.PromptAnchor,"prompt recovers after failed creation without another target change");

            var empty=Make(scene,runtime,service,"chr_npc#5","");var ambient=Make(scene,runtime,service,"chr_npc#6","dlg_ambient");var story=Make(scene,runtime,service,"chr_npc#7","dlg_story","story");var missing=Make(scene,runtime,service,"chr_npc#8","missing");
            targets.Clear();targets.AddRange(new IStreetInteractable[]{empty.Trigger,ambient.Trigger,story.Trigger,missing.Trigger});selection.Advance(12);selection.Advance(13);
            Check(selection.Selected==null&&prompt.Target==null,"unbound ambient story and missing dialogue targets show no prompt");
            Check(ambient.Trigger.Diagnostics.Count==1&&missing.Trigger.Diagnostics.Count==1,"bad ambient/missing binding diagnoses once across frames");
            NarrativeDialogue model;Check(story.Trigger.TryGetBoundDialogue(out model)&&model.Level==NarrativeDialogueLevel.Story&&story.Trigger.ActualMarkerValue=="story","Story binding remains available for marker owner without key interaction");
            Check(!empty.Trigger.IsAvailable&&empty.Trigger.Diagnostics.Count==0,"empty instance binding is normally non-interactable");
            var genericFoot=new GameObject(scene,"generic-foot").transform;genericFoot.position=new Vector3(1,0,0);var generic=new Generic{Foot=genericFoot};targets.Clear();targets.Add(generic);selection.Advance(14);
            Check(selection.Selected==null,"radius boundary is strictly outside using original Core rule");genericFoot.position=new Vector3(.25f,0,0);selection.Advance(15);
            Check(ReferenceEquals(selection.Selected,generic)&&prompt.Key=="fixture.open","selector works through generic interface and custom prompt text");input.Pressed=true;selection.Advance(16);selection.Advance(16);Check(generic.Starts==1,"generic action also fires once per one-frame press");

            var view=managers.AddComponent<NarrativeBubbleView>();var factory=new Factory();view.ConfigureSurfaceFactory(factory);far.Trigger.ClearActualBinding();
            var firstBubble=far.Root.AddComponent<NarrativeAmbientBubbleTrigger>();var otherBubble=near.Root.AddComponent<NarrativeAmbientBubbleTrigger>();
            Check(firstBubble.Configure(runtime,registry,far.Marker,far.Shell,view)&&otherBubble.Configure(runtime,registry,near.Marker,near.Shell,view),"actual bubble triggers configured on stable NPC roots");
            firstBubble.SetPoolId("amb_test");otherBubble.SetPoolId("amb_test");firstBubble.ConfigureDialogueService(service);otherBubble.ConfigureDialogueService(service);
            BubbleTick(firstBubble,0);BubbleTick(otherBubble,0);Check(factory.Created.Count==2,"both original bubble chains emit before dialogue");double deadline=Driver(firstBubble).CooldownUntilSeconds.Value;
            service.TryStart("dlg_normal","chr_npc#2",out issue);Check(firstBubble.SuppressesNewBubbles&&!otherBubble.SuppressesNewBubbles,"only exact initiating instance suppressed, same-base other suffix unaffected");
            BubbleTick(firstBubble,.2);Check(factory.Created.Count==2&&!factory.Created[0].Disposed,"gate does not remove or change an already shown bubble");
            Check(!firstBubble.DebugForceBubble()&&factory.Created.Count==2,"initiator cannot force a new bubble through development entry");
            BubbleTick(otherBubble,6);Check(factory.Created.Count==3,"another NPC can naturally emit during dialogue");BubbleTick(firstBubble,6);Near(Driver(firstBubble).CooldownUntilSeconds.Value,deadline,"suppression never restarts or pauses absolute cooldown deadline");
            service.End();BubbleTick(firstBubble,6.1);Check(factory.Created.Count==4&&!firstBubble.SuppressesNewBubbles,"initiator resumes emission immediately after ended dialogue and elapsed cooldown");
            firstBubble.ConfigureDialogueService(null);service.TryStart("dlg_normal","chr_npc#2",out issue);Check(!firstBubble.SuppressesNewBubbles,"unwired service retains legacy no-dialogue bubble behavior");BubbleTick(firstBubble,12);Check(factory.Created.Count==5,"unwired original bubble behavior is preserved even if a separate service is active");
            Console.WriteLine("NPC_INTERACTION_RESULT="+Encode());return 0;
        }
        catch(Exception ex){Console.Error.WriteLine("NPC_INTERACTION_FAILED="+ex);return 1;}
    }
    static string Encode(){return "{\"passed\":true,\"checks\":"+details.Count+",\"actualCoreRulesRuntimeTriggersAndBubble\":true,\"nativeUnity\":false,\"details\":["+string.Join(",",details.ConvertAll(s=>"\""+s.Replace("\\","\\\\").Replace("\"","\\\"")+"\"").ToArray())+"]}";}
}
