using System;
using System.Collections.Generic;
using System.Reflection;
using EatWhat.LongLoop;
using EatWhat.Narrative.Unity;
using EatWhat.Tools.Security;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;
using UO = UnityEngine.Object;

internal static class NarrativeHeadMarkerSpawnTests
{
    static readonly List<string> details = new List<string>();
    static void Check(bool value, string reason) { if (!value) throw new Exception(reason); details.Add(reason); }
    static void Near(double a, double b, string reason) { Check(Math.Abs(a-b)<1e-5, reason); }
    static object Get(object owner, string field) { return owner.GetType().GetField(field, BindingFlags.Instance|BindingFlags.NonPublic).GetValue(owner); }
    static void Set(object owner, string field, object value) { owner.GetType().GetField(field, BindingFlags.Instance|BindingFlags.NonPublic).SetValue(owner, value); }
    static void Life(object owner, string name) { owner.GetType().GetMethod(name, BindingFlags.Instance|BindingFlags.NonPublic).Invoke(owner, null); }
    static int Listeners(NarrativeDialogueService s, string name) { var value=(Delegate)Get(s,name); return value==null?0:value.GetInvocationList().Length; }
    static GameObject Child(GameObject parent,string name) { var c=new GameObject(parent.scene,name); c.transform.SetParent(parent.transform,false);return c; }
    static void Rows(CK01GeneratedDataCatalog c,string table,params ScriptableObject[] rows) { c.Rows[table]=new List<ScriptableObject>(rows); }
    sealed class Keys:IKeyProvider { public byte[] GetKey(){return new byte[32];} }
    static NarrativeTalkedPersistence Record(string path) { var r=NarrativeTalkedPersistence.CreateForCurrentBuild(path,new Keys(),x=>{});r.InitializeWhenAvailable(true);return r; }
    static CK01SceneStatePlacementData Placement(string state,string target,string dialogue)
    { return new CK01SceneStatePlacementData{scene_id="scn_spawn",state_id=state,kind="actor",target=target,active=true,dialogue_id=dialogue,anchor_id=target=="chr_npc#2"?"":"anchor_spawn"}; }
    static GameObject Npc(Scene scene,string id,bool withMarker)
    {
        var root=new GameObject(scene,id);var shell=root.AddComponent<NpcShell>();var head=Child(root,"head");
        shell.ConfigureReferences(Child(root,"foot").transform,Child(root,"visual").transform,head.transform);
        shell.ConfigurePresentationAnchors(Child(head,"marker").transform,Child(head,"prompt").transform,Child(head,"bubble").transform);
        var actor=root.AddComponent<NarrativeActorMarker>();actor.ConfigureId(id);
        root.AddComponent<NarrativeAmbientBubbleTrigger>();var trigger=root.AddComponent<NarrativeDialogueTrigger>();Set(trigger,"dialogueId","dlg_own");
        if(withMarker)
        {
            var view=shell.MarkerAnchor.gameObject.AddComponent<NarrativeHeadMarkerView>();var driver=root.AddComponent<NarrativeHeadMarkerDriver>();
            Set(driver,"markerView",view); // Prefab-local child only, never scene Runtime/Service references.
        }
        return root;
    }
    static NarrativeHeadMarkerDriver Driver(GameObject root){return root.GetComponent<NarrativeHeadMarkerDriver>();}
    static NarrativeHeadMarkerView View(GameObject root){return root.GetComponentsInChildren<NarrativeHeadMarkerView>(true)[0];}
    static NarrativeDialogueTrigger Trigger(GameObject root){return root.GetComponent<NarrativeDialogueTrigger>();}
    // External Unity Instantiate: clone hierarchy and remap SerializeField object references only.
    static GameObject Clone(GameObject source,Transform parent,bool world)
    {
        var map=new Dictionary<UO,UO>();var pairs=new List<Tuple<Component,Component>>();Func<GameObject,Transform,GameObject> copy=null;
        copy=(old,mount)=>
        {
            var next=new GameObject(mount==null?old.scene:mount.gameObject.scene,old.name+"(Clone)");next.activeSelf=old.activeSelf;
            next.transform.SetParent(mount,false);next.transform.localPosition=old.transform.localPosition;next.transform.localScale=old.transform.localScale;
            map[old]=next;map[old.transform]=next.transform;
            foreach(var component in old.GetComponents<Component>())if(!(component is Transform))
            {var c=next.AddComponent(component.GetType());map[component]=c;pairs.Add(Tuple.Create(component,c));if(component is Behaviour)((Behaviour)c).enabled=((Behaviour)component).enabled;}
            foreach(var child in old.transform.Children)copy(child.gameObject,next.transform);return next;
        };
        var root=copy(source,parent);
        foreach(var pair in pairs)foreach(var field in pair.Item1.GetType().GetFields(BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public))
        {
            if(!Attribute.IsDefined(field,typeof(SerializeField)))continue;
            object value=field.GetValue(pair.Item1);UO mapped;
            if(value is UO && map.TryGetValue((UO)value,out mapped))value=mapped;field.SetValue(pair.Item2,value);
        }
        return root;
    }
    // External Unity SetActive notifications. Production OnEnable/OnDisable and registration execute unchanged.
    static void ActiveChanged(GameObject root,bool active)
    {
        foreach(var actor in root.GetComponentsInChildren<NarrativeActorMarker>(true))
            if(!active||actor.isActiveAndEnabled)Life(actor,active?"OnEnable":"OnDisable");
        foreach(var driver in root.GetComponentsInChildren<NarrativeHeadMarkerDriver>(true))
            if(!active||driver.isActiveAndEnabled)Life(driver,active?"OnEnable":"OnDisable");
    }
    static int Main(string[] args)
    {
        try
        {
            var scene=new Scene(1592,"MarkerSpawnFixture");var root=new GameObject(scene,"manager");
            var runtime=root.AddComponent<NarrativeRuntimeEntry>();var registry=root.AddComponent<NarrativeTargetRegistry>();var service=root.AddComponent<NarrativeDialogueService>();
            service.Talked=Record(args[0]);var config=new DialoguePresentationConfig();config.Data.trigger[0].markerFadeBackTime=.4f;
            var catalog=new CK01GeneratedDataCatalog();Rows(catalog,"Characters",new CK01CharacterData{character_id="chr_npc"});
            Rows(catalog,"Scenes",new CK01SceneData{scene_id="scn_spawn",scene_asset=scene.name,default_state="day"});
            Rows(catalog,"SceneStates",new CK01SceneStateData{scene_id="scn_spawn",state_id="day"},new CK01SceneStateData{scene_id="scn_spawn",state_id="night"});
            Rows(catalog,"Dialogues",new CK01DialogueData{dialogue_id="dlg_own",tier="normal",first_line="ln_own"},new CK01DialogueData{dialogue_id="dlg_day",tier="normal",first_line="ln_day"},new CK01DialogueData{dialogue_id="dlg_story",tier="story",first_line="ln_story"});
            Rows(catalog,"DialogueLines",new CK01DialogueLineData{line_id="ln_own",dialogue_id="dlg_own",text_key="own"},new CK01DialogueLineData{line_id="ln_day",dialogue_id="dlg_day",text_key="day"},new CK01DialogueLineData{line_id="ln_story",dialogue_id="dlg_story",text_key="story"});
            Rows(catalog,"SceneStatePlacements",Placement("day","chr_npc#2","dlg_day"),Placement("day","chr_npc#3","dlg_day"),Placement("night","chr_npc#2","dlg_story"),Placement("night","chr_npc#3","dlg_story"));
            runtime.Configure(config,catalog);Check(runtime.IsAvailable,"actual runtime directory and trigger configuration available");
            var anchor=new GameObject(scene,"spawn point").AddComponent<NarrativeAnchorMarker>();anchor.ConfigureId("anchor_spawn");
            var preplaced=Npc(scene,"chr_npc#2",true);Trigger(preplaced).ConfigureReferences(runtime,service,preplaced.GetComponent<NarrativeActorMarker>(),preplaced.GetComponent<NpcShell>());
            Check(ReferenceEquals(Get(Driver(preplaced),"runtimeEntry"),runtime)&&ReferenceEquals(Get(Driver(preplaced),"dialogueService"),service),"existing trigger binding injects preplaced marker without direct driver Configure");
            Driver(preplaced).Advance(0);Check(!View(preplaced).Hidden&&View(preplaced).Glyph=="…","preplaced own binding renders through actual driver");
            var legacy=Npc(scene,"chr_npc#9",false);Trigger(legacy).ConfigureReferences(runtime,service,legacy.GetComponent<NarrativeActorMarker>(),legacy.GetComponent<NpcShell>());
            Check(Driver(legacy)==null&&Trigger(legacy).ActualDialogueId=="dlg_own","legacy NPC without marker keeps original trigger binding");
            var prefab=Npc(default(Scene),"",true);prefab.SetActive(false);
            Check(Get(Driver(prefab),"runtimeEntry")==null&&Get(Driver(prefab),"dialogueService")==null&&Get(Trigger(prefab),"runtimeEntry")==null,"template stores local pieces with no scene references");
            var bubbles=root.AddComponent<NarrativeBubbleView>();var applier=root.AddComponent<NarrativeSceneStateApplier>();
            applier.Configure(runtime,registry,prefab,null,bubbles);applier.ConfigureDialogueState(service);
            UO.InstantiateFactory=Clone;GameObject.ActiveChanged=ActiveChanged;
            Check(applier.ApplyDefault(),"actual Applier ApplyDefault instantiates inactive template then applies state");
            Transform generated;Check(registry.TryGetActor("chr_npc#3",out generated),"generated instance registered under complete ID after activation");
            var spawned=generated.gameObject;var driver=Driver(spawned);var view=View(spawned);driver.Advance(0);Driver(preplaced).Advance(0);
            Check(ReferenceEquals(Get(driver,"runtimeEntry"),runtime)&&ReferenceEquals(Get(driver,"dialogueService"),service),"normal spawn injects existing runtime and same service through trigger");
            Check(ReferenceEquals(Get(driver,"markerView"),view)&&view.gameObject!=spawned&&view.transform.IsChildOf(spawned.transform),"serialized child view remapped to generated local instance");
            Check(Trigger(spawned).HasActualBinding&&Trigger(spawned).ActualDialogueId=="dlg_day"&&!view.Hidden&&view.Glyph=="…","newly activated state-bound NPC naturally shows marker");
            Check(view.Anchor==spawned.GetComponent<NpcShell>().MarkerAnchor,"generated marker follows dedicated shell point");
            Check(Listeners(service,"DialogueStarted")==2&&Listeners(service,"DialogueEnded")==2,"preplaced and generated subscribe once each");
            string issue;service.TryStart("dlg_day","chr_npc#3",out issue);
            Check(view.Hidden&&View(preplaced).Hidden,"unified service start immediately hides both naturally wired markers");
            service.Talked.MarkTalked("dlg_day");service.End();driver.Advance(0);Driver(preplaced).Advance(0);
            Check(!view.Hidden&&view.Alpha==0&&view.Tint.r==.5f&&View(preplaced).Tint.r==.5f,"first return already uses latest actual01 grey record");
            driver.Advance(.1);Near(view.Alpha,.25,"generated marker fades with current trigger duration");
            Trigger(spawned).ConfigureReferences(runtime,service,spawned.GetComponent<NarrativeActorMarker>(),spawned.GetComponent<NpcShell>());
            driver.Advance(.1);Near(view.Alpha,.5,"identical binding does not restart active fade");
            Check(Trigger(spawned).ActualDialogueId=="dlg_day"&&Trigger(spawned).HasActualBinding&&Listeners(service,"DialogueStarted")==2,"rebinding preserves actual override and does not duplicate subscription");
            spawned.SetActive(false);Check(view.Hidden&&Listeners(service,"DialogueStarted")==1,"NPC disable unsubscribes and hides child display");
            service.TryStart("dlg_day","chr_npc#2",out issue);spawned.SetActive(true);
            Check(view.Hidden&&Listeners(service,"DialogueStarted")==2,"enable restores context and immediately honors already-running service");service.End();driver.Advance(0);
            var old=spawned;var oldView=view;int oldHides=oldView.Hides;
            Check(applier.ApplyState("night"),"actual state application destroys old generation and creates new one");
            Check(registry.TryGetActor("chr_npc#3",out generated)&&generated.gameObject!=old,"reapplication resolves new instance rather than retired generation");
            spawned=generated.gameObject;driver=Driver(spawned);view=View(spawned);driver.Advance(0);Driver(preplaced).Advance(0);
            Check(!old.activeInHierarchy&&oldView.Hidden&&!view.Hidden&&view.Glyph=="!"&&View(preplaced).Glyph=="!","new generation and preplaced actor use new story binding while old is hidden");
            Check(Listeners(service,"DialogueStarted")==2&&Listeners(service,"DialogueEnded")==2,"state replacement releases old subscriptions");
            oldHides=oldView.Hides;service.TryStart("dlg_story","chr_npc#3",out issue);
            Check(view.Hidden&&View(preplaced).Hidden&&oldView.Hides==oldHides,"retired generation receives no further service events");service.End();
            Life(applier,"OnDisable");Driver(preplaced).Advance(0);
            Check(!spawned.activeInHierarchy&&Listeners(service,"DialogueStarted")==1,"application cleanup removes generated event subscribers only");
            Check(!Trigger(preplaced).HasActualBinding&&Trigger(preplaced).ActualDialogueId=="dlg_own"&&View(preplaced).Glyph=="…","preplaced cleanup restores own binding without clearing scene dependencies");
            // Trigger can have been wired before its driver enables; no scene search or manual driver injection.
            var late=Npc(scene,"chr_npc#10",false);late.SetActive(false);Trigger(late).ConfigureReferences(runtime,service,late.GetComponent<NarrativeActorMarker>(),late.GetComponent<NpcShell>());
            var lateView=late.GetComponent<NpcShell>().MarkerAnchor.gameObject.AddComponent<NarrativeHeadMarkerView>();var lateDriver=late.AddComponent<NarrativeHeadMarkerDriver>();late.SetActive(true);lateDriver.Advance(0);
            Check(!lateView.Hidden&&ReferenceEquals(Get(lateDriver,"runtimeEntry"),runtime)&&ReferenceEquals(Get(lateDriver,"markerView"),lateView),"OnEnable recovers prebound trigger context and local child when driver initialized later");
            int count=Listeners(service,"DialogueStarted");Life(lateDriver,"OnEnable");
            Check(Listeners(service,"DialogueStarted")==count,"repeated enable notification remains single subscription");
            var replacement=root.AddComponent<NarrativeDialogueService>();replacement.Talked=Record(args[0]+"-replacement");
            Trigger(late).ConfigureReferences(runtime,replacement,late.GetComponent<NarrativeActorMarker>(),late.GetComponent<NpcShell>());
            Check(Listeners(service,"DialogueStarted")==count-1&&Listeners(replacement,"DialogueStarted")==1,"changing trigger scene service unsubscribes previous service");
            int hides=lateView.Hides;service.TryStart("dlg_own","chr_npc#2",out issue);Check(lateView.Hides==hides,"old service cannot hide rebound marker");service.End();
            replacement.TryStart("dlg_own","chr_npc#10",out issue);Check(lateView.Hidden,"new service immediately controls rebound marker");replacement.End();
            Life(lateDriver,"OnDestroy");Check(Listeners(replacement,"DialogueStarted")==0&&Listeners(replacement,"DialogueEnded")==0,"destroy removes both event handlers");
            Console.WriteLine(JsonConvert.SerializeObject(new{result="HEAD_MARKER_SPAWN_PASS",checks=details.Count,details,nativeUnity=false,directMarkerDriverConfigure=false},Formatting.Indented));return 0;
        }
        catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
        finally{UO.InstantiateFactory=null;GameObject.ActiveChanged=null;}
    }
}
