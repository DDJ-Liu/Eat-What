using System;
using System.Collections.Generic;
using System.Reflection;
using EatWhat.LongLoop;
using EatWhat.Narrative.Core;
using EatWhat.Narrative.Unity;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
internal static class NarrativeSceneDebugTests
{
    static int checks;static void Check(bool v,string label){checks++;if(!v)throw new Exception(label);}
    static void Event(object target,string method){target.GetType().GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(target,null);}
    static GameObject Child(GameObject parent,string name){var c=new GameObject(parent.scene,name);c.transform.SetParent(parent.transform,false);return c;}
    sealed class Factory:INarrativeBubbleSurfaceFactory{public readonly List<string> Texts=new List<string>();public INarrativeBubbleSurface Create(Transform h,string k,string text,out string issue){issue=null;Texts.Add(k);return new Surface();}}
    sealed class Surface:INarrativeBubbleSurface{public bool Apply(float a,float r)=>true;public void Dispose(){}}
    static void Rows(CK01GeneratedDataCatalog c,string table,params ScriptableObject[] rows){c.Rows[table]=new List<ScriptableObject>(rows);}
    public static int Main()
    {
        var scene=new Scene(800,"LongLoop_Street");var managers=new GameObject(scene,"managers");var registry=managers.AddComponent<NarrativeTargetRegistry>();var runtime=managers.AddComponent<NarrativeRuntimeEntry>();
        var config=new DialoguePresentationConfig();config.Data.trigger[0].autoBubbleRadius=3;config.Data.trigger[0].autoBubbleCooldown=10;
        var catalog=new CK01GeneratedDataCatalog();
        Rows(catalog,"Scenes",new CK01SceneData{scene_id="scn_street",scene_asset="LongLoop_Street",default_state="day"});
        Rows(catalog,"SceneStates",new CK01SceneStateData{scene_id="scn_street",state_id="day"},new CK01SceneStateData{scene_id="scn_street",state_id="night"});
        Rows(catalog,"Characters",new CK01CharacterData{character_id="chr_vendor"});
        Rows(catalog,"SceneStatePlacements",new CK01SceneStatePlacementData{scene_id="scn_street",state_id="day",kind="actor",target="chr_vendor#2",active=true,ambient_pool="pool_a"},new CK01SceneStatePlacementData{scene_id="scn_street",state_id="night",kind="actor",target="chr_vendor#2",active=true,ambient_pool="pool_b",facing="left"});
        Rows(catalog,"Dialogues",new CK01DialogueData{dialogue_id="dlg_a",tier="ambient",first_line="ln_1"});
        Rows(catalog,"DialogueLines",new CK01DialogueLineData{line_id="ln_1",dialogue_id="dlg_a",next_line="ln_2",text_key="line.1"},new CK01DialogueLineData{line_id="ln_2",dialogue_id="dlg_a",text_key="line.2"});
        Rows(catalog,"AmbientPools",new CK01AmbientPoolData{pool_id="pool_a",dialogue_ids=new List<string>{"dlg_a"}},new CK01AmbientPoolData{pool_id="pool_b",dialogue_ids=new List<string>{"dlg_a"}});
        runtime.Configure(config,catalog);INarrativeDataDirectory directory;Check(runtime.TryGetDirectory(out directory),"real Runtime/Adapter/directory fixture initialized");
        var view=managers.AddComponent<NarrativeBubbleView>();var factory=new Factory();view.ConfigureSurfaceFactory(factory);
        var npc=new GameObject(scene,"npc");var marker=npc.AddComponent<NarrativeActorMarker>();marker.ConfigureId("chr_vendor#2");var shell=npc.AddComponent<NpcShell>();shell.ConfigureReferences(Child(npc,"foot").transform,Child(npc,"visual").transform,Child(npc,"head").transform);
        var trigger=npc.AddComponent<NarrativeAmbientBubbleTrigger>();Check(trigger.Configure(runtime,registry,marker,shell,view),"real trigger scene binding");trigger.SetPoolId("pool_a");
        var player=new GameObject(scene,"player");registry.ConfigurePlayer(player.transform);registry.CollectSceneMarkers();
        var applier=managers.AddComponent<NarrativeSceneStateApplier>();applier.Configure(runtime,registry,null,null,view);
        var panel=managers.AddComponent<NarrativeSceneDebugPanel>();panel.Configure(applier,registry);
        Check(!panel.IsVisible,"panel starts hidden");Keyboard.current=new Keyboard();
        Keyboard.current[Key.F9].wasPressedThisFrame=true;Event(panel,"Update");Keyboard.current[Key.F9].wasPressedThisFrame=false;Check(!panel.IsVisible,"different key does not toggle default F8 panel");
        Keyboard.current[Key.F8].wasPressedThisFrame=true;Event(panel,"Update");Keyboard.current[Key.F8].wasPressedThisFrame=false;Check(panel.IsVisible,"actual InputSystem F8 route toggles panel");
        Event(panel,"OnGUI");Check(GUILayout.Labels.Exists(x=>x.Contains("scn_street")&&x.Contains("<not applied>")),"real scene id, no invented current state");
        GUILayout.Clicks.Add("Apply day");Event(panel,"OnGUI");Check(applier.CurrentStateId=="day"&&applier.CurrentSceneId=="scn_street","GUI invokes original actual Applier state application");
        Time.timeAsDouble=0;Event(trigger,"Update");Check(factory.Texts.Count==1&&factory.Texts[0]=="line.1","normal path emits first actual linked line");
        var driver=(NarrativeAmbientBubbleDriver)typeof(NarrativeAmbientBubbleTrigger).GetField("driver",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(trigger);var deadline=driver.CooldownUntilSeconds;
        Check(deadline==10,"normal actual model deadline set");player.transform.position=new Vector3(100,0,0);Time.timeAsDouble=1;Event(trigger,"Update");Check(factory.Texts.Count==1,"normal distance/cooldown still blocks");
        int groundCalls=GroundSpace.Calls;GUILayout.Clicks.Add("Bubble chr_vendor#2");Event(panel,"OnGUI");Check(factory.Texts.Count==2&&factory.Texts[1]=="line.2","GUI -> Trigger -> Driver -> same Model -> real View consumes next line immediately");
        Check(GroundSpace.Calls==groundCalls,"forced route never fabricates/reads a ground distance");Check(driver.CooldownUntilSeconds==deadline,"force preserves normal cooldown deadline");
        Check(ReferenceEquals(driver,typeof(NarrativeAmbientBubbleTrigger).GetField("driver",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(trigger)),"force does not recreate driver");
        GUILayout.Clicks.Add("Apply night");Event(panel,"OnGUI");Check(applier.CurrentStateId=="night"&&trigger.PoolId=="pool_b"&&!shell.IsFacingRight,"second GUI state choice uses real nine-step owner");
        INarrativeDataDirectory again;runtime.TryGetDirectory(out again);Check(ReferenceEquals(directory,again)&&driver.CooldownUntilSeconds==deadline,"state route preserves directory/model cooldown");
        Check(GUILayout.Labels.Exists(x=>x.Contains("chr_vendor#2 / pool_b")),"NPC list reads current registry/full id and pool");
        Check(!panel.TryApplyState("missing")&&applier.CurrentStateId=="night","invalid state does not fake success or alter last successful state");
        Check(!panel.TryForceBubble("chr_absent"),"missing current NPC rejected");npc.SetActive(false);registry.CollectSceneMarkers();Check(!panel.TryForceBubble("chr_vendor#2"),"inactive NPC is not force-targetable");npc.SetActive(true);
        var unbound=managers.AddComponent<NarrativeSceneDebugPanel>();unbound.Configure(null,null);Check(!unbound.TryApplyState("day")&&!unbound.TryForceBubble("chr_vendor#2"),"missing scene bindings unavailable");
        var model=new NarrativeAmbientBubbleModel("chr_vendor#2",directory);model.ChangePool("pool_a");NarrativeAmbientBubbleEmission emission;
        Check(model.DebugForce(2,n=>0,out emission)&&emission.LineId=="ln_1"&&!model.CooldownUntilSeconds.HasValue,"force before normal emission leaves deadline unset");
        Check(model.DebugForce(3,n=>0,out emission)&&emission.LineId=="ln_2","force shares actual segment progress");
        Check(!model.DebugForce(double.NaN,n=>0,out emission),"invalid actual time rejected");model.ChangePool("missing");Check(!model.DebugForce(4,n=>0,out emission),"missing pool is not fabricated healthy");
        Event(applier,"OnDisable");Check(applier.CurrentStateId==null,"last-state query follows actual owner lifetime");
        Console.WriteLine(NarrativeAmbientBubbleTests.Run());
        Console.WriteLine("SCENE_DEBUG_PASS checks="+checks);return 0;
    }
}
