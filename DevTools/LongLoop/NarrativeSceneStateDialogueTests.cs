using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using EatWhat.LongLoop;
using EatWhat.Narrative.Unity;
using EatWhat.Tools.Persistence;
using EatWhat.Tools.Security;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UO=UnityEngine.Object;

internal static class NarrativeSceneStateDialogueTests
{
    static readonly List<string> details=new List<string>();
    static readonly string[] ids={"dlg_own_a","dlg_own_b","dlg_state_a","dlg_state_b","dlg_own_off","dlg_state_off","dlg_own_gen","dlg_empty_override","dlg_untouched"};
    static readonly DateTime stamp=new DateTime(2026,11,19,0,0,0,DateTimeKind.Utc);
    static void Check(bool value,string reason){if(!value)throw new Exception(reason);details.Add(reason);}
    static void Set(object owner,string name,object value){owner.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(owner,value);}
    static void Life(object owner,string name){owner.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(owner,null);}
    sealed class Keys:IKeyProvider{public byte[] GetKey(){var v=new byte[32];for(int i=0;i<v.Length;i++)v[i]=(byte)(i+1);return v;}}
    static readonly Keys keys=new Keys();
    static NarrativeTalkedPersistence Record(string dir,bool seed)
    {
        if(seed)
        {
            var store=new JsonSaveStore(dir,SaveCodecFactory.CreateForCurrentBuild(keys),new EnvelopeAuthenticator(keys));string issue;
            var payload=new JObject();payload[NarrativeTalkedPersistence.Section]=new JArray(ids);payload["unrelated_fixture_section"]=new JObject{{"keep",7}};
            Check(store.Write(NarrativeTalkedPersistence.Slot,SaveEnvelope.Create(payload,stamp),out issue),"actual Toolkit store seeds isolated signed envelope");
        }
        var value=NarrativeTalkedPersistence.CreateForCurrentBuild(dir,keys,message=>{},()=>stamp);
        Check(value.InitializeWhenAvailable(true),"actual01 persistence initializes isolated record");return value;
    }
    static void SeedAgain(NarrativeTalkedPersistence record){foreach(var id in ids)record.MarkTalked(id);}
    static string Bytes(string dir){return Convert.ToBase64String(File.ReadAllBytes(Path.Combine(dir,"long_loop.json")));}
    static GameObject Child(GameObject root,string name){var v=new GameObject(root.scene,name);v.transform.SetParent(root.transform,false);return v;}
    static GameObject Npc(Scene scene,string id,string dialogue,string marker,NarrativeRuntimeEntry runtime,NarrativeDialogueService service)
    {
        var root=new GameObject(scene,id);var shell=root.AddComponent<NpcShell>();
        shell.ConfigureReferences(Child(root,"foot").transform,Child(root,"visual").transform,Child(root,"head").transform);
        var actor=root.AddComponent<NarrativeActorMarker>();actor.ConfigureId(id);root.AddComponent<NarrativeAmbientBubbleTrigger>();
        var trigger=root.AddComponent<NarrativeDialogueTrigger>();Set(trigger,"dialogueId",dialogue);Set(trigger,"markerValue",marker);
        trigger.ConfigureReferences(runtime,service,actor,shell);return root;
    }
    static NarrativeDialogueTrigger Trigger(GameObject value){return value.GetComponent<NarrativeDialogueTrigger>();}
    static void Rows(CK01GeneratedDataCatalog catalog,string table,params ScriptableObject[] rows){catalog.Rows[table]=new List<ScriptableObject>(rows);}
    static CK01SceneStatePlacementData P(string state,string target,string dialogue="",string marker="",bool active=true,string anchor="")
    {return new CK01SceneStatePlacementData{scene_id="scn_bind",state_id=state,kind="actor",target=target,dialogue_id=dialogue,marker=marker,active=active,anchor_id=anchor};}

    // Unity clone/hierarchy/lifecycle delivery only; all IDs, binding and registration rules stay production code.
    static GameObject Clone(GameObject source,Transform parent,bool world)
    {
        var objects=new Dictionary<UO,UO>();var pairs=new List<Tuple<Component,Component>>();
        Func<GameObject,Transform,GameObject> copy=null;
        copy=(original,mount)=>
        {
            var value=new GameObject(mount==null?original.scene:mount.gameObject.scene,original.name+"(Clone)");
            value.activeSelf=original.activeSelf;value.transform.SetParent(mount,false);
            value.transform.localPosition=original.transform.localPosition;value.transform.localScale=original.transform.localScale;
            objects[original]=value;objects[original.transform]=value.transform;
            foreach(var c in original.GetComponents<Component>())if(!(c is Transform))
            {var next=value.AddComponent(c.GetType());objects[c]=next;pairs.Add(Tuple.Create(c,next));if(c is Behaviour) ((Behaviour)next).enabled=((Behaviour)c).enabled;}
            foreach(var child in original.transform.Children)copy(child.gameObject,value.transform);
            return value;
        };
        var root=copy(source,parent);
        foreach(var pair in pairs)
        foreach(var field in pair.Item1.GetType().GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic))
        {
            if(!Attribute.IsDefined(field,typeof(SerializeField)))continue;
            object value=field.GetValue(pair.Item1);var old=value as UO;UO mapped;
            if(old!=null&&objects.TryGetValue(old,out mapped))value=mapped;
            field.SetValue(pair.Item2,value);
        }
        NotifyMarkers(root,true);return root;
    }
    static void NotifyMarkers(GameObject root,bool enabled)
    {
        foreach(var marker in root.GetComponentsInChildren<NarrativeActorMarker>(true))
            if(!enabled||marker.isActiveAndEnabled)Life(marker,enabled?"OnEnable":"OnDisable");
    }
    sealed class World:IDisposable
    {
        public readonly Scene Scene;public readonly NarrativeRuntimeEntry Runtime;public readonly NarrativeDialogueService Service;
        public readonly NarrativeTargetRegistry Registry;public readonly NarrativeSceneStateApplier Applier;
        public readonly GameObject A,B,Empty,Off,Untouched;
        public World(int index)
        {
            Scene=new Scene(index,"StateBindingFixture"+index);var managers=new GameObject(Scene,"managers");
            Runtime=managers.AddComponent<NarrativeRuntimeEntry>();Service=managers.AddComponent<NarrativeDialogueService>();Registry=managers.AddComponent<NarrativeTargetRegistry>();
            var config=new DialoguePresentationConfig();var catalog=new CK01GeneratedDataCatalog();
            Rows(catalog,"Characters",new CK01CharacterData{character_id="chr_npc"});
            var dialogues=new List<ScriptableObject>();var lines=new List<ScriptableObject>();
            foreach(var id in ids){var line="ln_"+id.Substring(4);dialogues.Add(new CK01DialogueData{dialogue_id=id,tier="normal",first_line=line});lines.Add(new CK01DialogueLineData{line_id=line,dialogue_id=id,text_key=id});}
            catalog.Rows["Dialogues"]=dialogues;catalog.Rows["DialogueLines"]=lines;
            Rows(catalog,"Scenes",new CK01SceneData{scene_id="scn_bind",scene_asset=Scene.name,default_state="day",scene_type="street"});
            Rows(catalog,"SceneStates",new CK01SceneStateData{scene_id="scn_bind",state_id="day"},new CK01SceneStateData{scene_id="scn_bind",state_id="night"},new CK01SceneStateData{scene_id="scn_bind",state_id="other"});
            Rows(catalog,"SceneStatePlacements",P("day","chr_npc#2","dlg_state_a","none"),P("day","chr_npc#3",marker:"story"),P("day","chr_npc#4"),
                P("day","chr_npc#5","dlg_state_off","none",false),P("day","chr_npc#6",marker:"none",anchor:"anchor_spawn"),
                P("night","chr_npc#2"),P("night","chr_npc#3","dlg_state_b"),P("night","chr_npc#4","dlg_empty_override","talk"),
                P("other","chr_npc#3"));
            Runtime.Configure(config,catalog);Check(Runtime.IsAvailable,"actual runtime data adapter accepts isolated scene rows");
            A=Npc(Scene,"chr_npc#2","dlg_own_a","talk",Runtime,Service);B=Npc(Scene,"chr_npc#3","dlg_own_b","",Runtime,Service);
            Empty=Npc(Scene,"chr_npc#4","","",Runtime,Service);Off=Npc(Scene,"chr_npc#5","dlg_own_off","talk",Runtime,Service);
            Untouched=Npc(Scene,"chr_npc#9","dlg_untouched","talk",Runtime,Service);
            var anchor=new GameObject(Scene,"anchor").AddComponent<NarrativeAnchorMarker>();anchor.ConfigureId("anchor_spawn");
            var prefab=Npc(new Scene(index+10000,"external-prefab-capability"),"","dlg_own_gen","talk",null,null);
            Applier=managers.AddComponent<NarrativeSceneStateApplier>();
            Check(Applier.Configure(Runtime,Registry,prefab,null,managers.AddComponent<NarrativeBubbleView>()),"actual component accepts same-scene references");
        }
        public void Dispose(){Life(Applier,"OnDisable");}
    }
    static void BindingsAndRealSave(string dir)
    {
        var record=Record(dir,true);using(var w=new World(1401))
        {
            w.Service.Talked=record;Check(w.Applier.ConfigureDialogueState(w.Service),"applier shares service's actual01 record");
            string initial=Bytes(dir);Debug.Warnings.Clear();Check(w.Applier.ApplyDefault(),"first scene application succeeds");
            Check(Bytes(dir)==initial&&record.Count==ids.Length,"initial application does not clear or write talked file");
            Check(Trigger(w.A).ActualDialogueId=="dlg_state_a"&&Trigger(w.A).ActualMarkerValue=="none","explicit dialogue and none override own values");
            Check(Trigger(w.B).ActualDialogueId=="dlg_own_b"&&Trigger(w.B).ActualMarkerValue=="story","blank dialogue inherits own while explicit marker overrides");
            Check(Trigger(w.Empty).ActualDialogueId==""&&Trigger(w.Empty).ActualMarkerValue=="","both empty remain empty");
            Check(!w.Off.activeSelf&&Trigger(w.Off).ActualDialogueId=="dlg_state_off","inactive row still receives actual binding");
            Check(!Trigger(w.Untouched).HasActualBinding,"never-listed preset is not touched");
            Transform generated;Check(w.Registry.TryGetActorForSceneState("chr_npc#6",out generated),"generated actor registers complete suffix");
            Check(Trigger(generated.gameObject).ActualDialogueId=="dlg_own_gen","generated actor blank dialogue inherits prefab instance authoring");
            Check(!Debug.Warnings.Exists(x=>x.Contains("INVALID_ID")),"spawn initialization emits no empty-id registry diagnostic");
            Check(w.Applier.ApplyState("day"),"explicit same-state reapply is a switch");
            foreach(var id in new[]{"dlg_state_a","dlg_own_b","dlg_state_off","dlg_own_gen"})Check(!record.HasTalked(id),"switch clears actual target binding "+id);
            foreach(var id in new[]{"dlg_own_a","dlg_state_b","dlg_own_off","dlg_untouched"})Check(record.HasTalked(id),"switch preserves non-target binding "+id);
            Check(record.LastSaveSucceeded==true,"real clear uses immediate successful SaveNow");
            var cold=Record(dir,false);Check(!cold.HasTalked("dlg_state_a")&&cold.HasTalked("dlg_untouched"),"cold actual01 host reads cleared file and retained unrelated id");
            var store=new JsonSaveStore(dir,SaveCodecFactory.CreateForCurrentBuild(keys),new EnvelopeAuthenticator(keys));SaveEnvelope envelope;string issue;
            Check(store.TryRead(NarrativeTalkedPersistence.Slot,out envelope,out issue)&&(int)envelope.Payload["unrelated_fixture_section"]["keep"]==7,"real clear preserves unrelated signed envelope section");
            SeedAgain(record);string before=Bytes(dir);string diagnostic;w.Service.TryStart("dlg_state_a","chr_npc#2",out diagnostic);
            Check(!w.Applier.ApplyState("night")&&w.Applier.CurrentStateId=="day","running dialogue rejects switch and retains current state");
            Check(Bytes(dir)==before&&Trigger(w.A).ActualDialogueId=="dlg_state_a"&&w.Registry.TryGetActorForSceneState("chr_npc#6",out generated),"rejected switch has no binding/save/generated-actor mutation");
            Check(Debug.Warnings.Exists(x=>x.Contains("DIALOGUE_IN_PROGRESS")),"running dialogue returns explicit diagnostic");w.Service.End();
            Check(w.Applier.ApplyState("night"),"different state applies after dialogue ends");
            Check(Trigger(w.A).ActualDialogueId=="dlg_own_a"&&Trigger(w.A).ActualMarkerValue=="talk","reapply restores own defaults before empty state fields");
            Check(Trigger(w.B).ActualDialogueId=="dlg_state_b"&&Trigger(w.B).ActualMarkerValue=="","explicit dialogue with empty own marker stays empty for marker inference");
            Check(Trigger(w.Empty).ActualDialogueId=="dlg_empty_override"&&Trigger(w.Empty).ActualMarkerValue=="talk","explicit fields work on empty authored instance");
            Check(w.Off.activeSelf&&!Trigger(w.Off).HasActualBinding&&Trigger(w.Off).ActualDialogueId=="dlg_own_off","previously managed absent preset restores its authoring");
            Check(!record.HasTalked("dlg_own_a")&&!record.HasTalked("dlg_state_b")&&!record.HasTalked("dlg_empty_override"),"different target state's final bindings clear");
            Check(record.HasTalked("dlg_state_a")&&record.HasTalked("dlg_state_off")&&record.HasTalked("dlg_untouched"),"prior state and absent presets are excluded from clear set");
            Check(!w.Registry.TryGetActorForSceneState("chr_npc#6",out generated),"old generated actor removed by unchanged state lifetime");
            record.MarkTalked("dlg_own_a");Check(w.Applier.ApplyState("night")&&!record.HasTalked("dlg_own_a"),"repeated current-state request resets again");
            SeedAgain(record);Check(w.Applier.ApplyState("other"),"state omitting prior managed presets applies");
            Check(!record.HasTalked("dlg_own_b")&&record.HasTalked("dlg_own_a")&&record.HasTalked("dlg_untouched"),"only target rows clear, including fallback, not restored omitted presets");
            Check(Trigger(w.A).InstanceDialogueId=="dlg_own_a"&&Trigger(w.A).InstanceMarkerValue=="talk","runtime application never overwrites instance authored values");
            string stable=Bytes(dir);Check(!w.Applier.ApplyState("missing")&&Bytes(dir)==stable,"invalid state preserves original scene and save");
        }
    }
    static void OptionalAndLateRecords(string root)
    {
        using(var w=new World(1402))
        {Check(w.Applier.ApplyDefault()&&w.Applier.ApplyState("night"),"legacy application with no service or record retains flow");Check(Trigger(w.A).ActualDialogueId=="dlg_own_a","bindings still apply without service/record");}
        var record=Record(Path.Combine(root,"record-only"),true);using(var w=new World(1403))
        {Check(w.Applier.ConfigureDialogueState(null,record)&&w.Applier.ApplyDefault(),"record may connect independently of service");Check(w.Applier.ApplyState("night")&&!record.HasTalked("dlg_own_a"),"record-only application calls actual clear");}
        record=Record(Path.Combine(root,"late"),true);using(var w=new World(1404))
        {
            Check(w.Applier.ConfigureDialogueState(w.Service)&&w.Applier.ApplyDefault(),"service may precede creation of its record");
            w.Service.Talked=record;Check(w.Applier.ApplyState("night")&&!record.HasTalked("dlg_own_a"),"late service record resolves lazily instead of caching initial null");
        }
        record=Record(Path.Combine(root,"first-explicit"),true);using(var w=new World(1405))
        {
            w.Applier.ConfigureDialogueState(null,record);string initial=Bytes(Path.Combine(root,"first-explicit"));
            Check(w.Applier.ApplyState("night")&&Bytes(Path.Combine(root,"first-explicit"))==initial,"first successful explicit application initializes without resetting talked");
            Check(w.Applier.ApplyState("night")&&!record.HasTalked("dlg_own_a"),"subsequent explicit same-state request resets talked");
        }
    }
    static int Main(string[] args)
    {
        try
        {
            if(args.Length!=1)throw new ArgumentException("isolated save root required");
            UO.InstantiateFactory=Clone;GameObject.ActiveChanged=NotifyMarkers;
            BindingsAndRealSave(Path.Combine(args[0],"main"));OptionalAndLateRecords(args[0]);
            Console.WriteLine("SCENE_STATE_DIALOGUE_RESULT "+JsonConvert.SerializeObject(new{passed=true,checks=details.Count,actualApplierApplicationTriggerAndPersistence=true,nativeUnity=false,details}));return 0;
        }
        catch(Exception ex){Console.Error.WriteLine("SCENE_STATE_DIALOGUE_FAILED "+ex);return 1;}
        finally{UO.InstantiateFactory=null;GameObject.ActiveChanged=null;}
    }
}
