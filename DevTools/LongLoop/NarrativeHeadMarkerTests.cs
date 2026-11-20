using System;
using System.Collections.Generic;
using System.Reflection;
using EatWhat.LongLoop;
using EatWhat.Narrative.Core;
using EatWhat.Narrative.Unity;
using EatWhat.Tools.Security;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;
internal static class NarrativeHeadMarkerTests
{
    static readonly List<string> details=new List<string>();
    static void Check(bool value,string reason){if(!value)throw new Exception(reason);details.Add(reason);}
    static void Near(double a,double b,string reason){Check(Math.Abs(a-b)<1e-5,reason);}
    static void Set(object owner,string field,object value){owner.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(owner,value);}
    static void Life(object owner,string method){owner.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(owner,null);}
    static GameObject Child(GameObject root,string name){var v=new GameObject(root.scene,name);v.transform.SetParent(root.transform,false);return v;}
    static void Rows(CK01GeneratedDataCatalog catalog,string table,params ScriptableObject[] values){catalog.Rows[table]=new List<ScriptableObject>(values);}
    sealed class Keys:IKeyProvider{public byte[] GetKey(){return new byte[32];}}
    sealed class Surface:INarrativeBubbleSurface{public bool Disposed;public bool Apply(float a,float y){return true;}public void Dispose(){Disposed=true;}}
    sealed class Factory:INarrativeBubbleSurfaceFactory
    {public Surface Last;public INarrativeBubbleSurface Create(Transform head,string key,string text,out string issue){issue=null;Last=new Surface();return Last;}}
    sealed class Npc
    {public GameObject Root;public NpcShell Shell;public NarrativeDialogueTrigger Trigger;public NarrativeHeadMarkerView View;public NarrativeHeadMarkerDriver Driver;public NarrativeActorMarker Actor;}
    static Npc Make(Scene scene,NarrativeRuntimeEntry runtime,NarrativeDialogueService service,string id,string dialogue)
    {
        var root=new GameObject(scene,id);var shell=root.AddComponent<NpcShell>();shell.ConfigureReferences(Child(root,"foot").transform,Child(root,"visual").transform,Child(root,"head").transform);
        shell.ConfigurePresentationAnchors(Child(root,"marker").transform,Child(root,"prompt").transform,Child(root,"bubble").transform);
        var actor=root.AddComponent<NarrativeActorMarker>();actor.ConfigureId(id);var trigger=root.AddComponent<NarrativeDialogueTrigger>();Set(trigger,"dialogueId",dialogue);trigger.ConfigureReferences(runtime,service,actor,shell);
        var view=root.AddComponent<NarrativeHeadMarkerView>();var driver=root.AddComponent<NarrativeHeadMarkerDriver>();driver.Configure(runtime,service,trigger,shell,view);
        return new Npc{Root=root,Shell=shell,Trigger=trigger,View=view,Driver=driver,Actor=actor};
    }
    static int Main(string[] args)
    {
        try
        {
            var scene=new Scene(1501,"MarkerFixture");var manager=new GameObject(scene,"manager");var runtime=manager.AddComponent<NarrativeRuntimeEntry>();var service=manager.AddComponent<NarrativeDialogueService>();
            var config=new DialoguePresentationConfig();var trigger=config.Data.trigger[0];trigger.markerBobAmplitude=.2f;trigger.markerBobPeriod=.8f;trigger.markerFadeBackTime=.4f;trigger.markerTalkedTint=new PresentationColor(.2f,.4f,.6f,.8f);
            config.Data.trigger=new[]{trigger,new DialogueTriggerConfig{name="alternate",markerBobAmplitude=.3f,markerBobPeriod=2,markerFadeBackTime=.2f}};
            var catalog=new CK01GeneratedDataCatalog();Rows(catalog,"Characters",new CK01CharacterData{character_id="chr_npc"});
            Rows(catalog,"Dialogues",new CK01DialogueData{dialogue_id="dlg_normal",tier="normal",first_line="ln_normal"},new CK01DialogueData{dialogue_id="dlg_story",tier="story",first_line="ln_story"},new CK01DialogueData{dialogue_id="dlg_ambient",tier="ambient",first_line="ln_ambient"});
            Rows(catalog,"DialogueLines",new CK01DialogueLineData{line_id="ln_normal",dialogue_id="dlg_normal",text_key="n"},new CK01DialogueLineData{line_id="ln_story",dialogue_id="dlg_story",text_key="s"},new CK01DialogueLineData{line_id="ln_ambient",dialogue_id="dlg_ambient",text_key="a"});
            Rows(catalog,"AmbientPools",new CK01AmbientPoolData{pool_id="amb_test",dialogue_ids=new List<string>{"dlg_ambient"}});
            runtime.Configure(config,catalog);Check(runtime.IsAvailable,"actual runtime and trigger resolver ready");
            var a=Make(scene,runtime,service,"chr_npc#2","dlg_normal");var b=Make(scene,runtime,service,"chr_npc#3","dlg_story");
            a.Driver.Advance(0);b.Driver.Advance(0);Check(a.View.Hidden&&b.View.Hidden,"unprepared service record never flashes un-talked brightness");
            var talked=NarrativeTalkedPersistence.CreateForCurrentBuild(args[0],new Keys(),x=>{});talked.InitializeWhenAvailable(true);service.Talked=talked;
            a.Driver.Advance(0);b.Driver.Advance(0);
            Check(!a.View.Hidden&&a.View.Glyph=="…"&&a.View.Tint.r==1,"normal binding maps through Core04 to bright ellipsis");
            Check(!b.View.Hidden&&b.View.Glyph=="!"&&b.View.Tint.r==1&&b.View.Tint.g==.75f,"story binding maps to gold exclamation");
            Check(a.View.Anchor==a.Shell.MarkerAnchor&&a.View.Anchor!=a.Shell.PromptAnchor&&a.View.Anchor!=a.Shell.BubbleAnchor,"actual driver uses dedicated marker anchor");
            a.Driver.Advance(.2);Near(a.View.Bob,.2,"trigger amplitude at quarter period in world units");a.Driver.Advance(.4);Near(a.View.Bob,-.2,"trigger period lower quarter");
            a.Trigger.SetActualBinding("","none");a.Driver.Advance(0);Check(a.View.Hidden,"explicit none hides even with normal binding");
            a.Trigger.SetActualBinding("","story");a.Driver.Advance(0);Check(a.View.Glyph=="!"&&a.View.Tint.g==.75f,"explicit actual marker overrides inferred kind");a.Trigger.ClearActualBinding();a.Driver.Advance(0);
            var registry=manager.AddComponent<NarrativeTargetRegistry>();registry.ConfigurePlayer(a.Shell.Foot);registry.CollectSceneMarkers();
            var bubbles=manager.AddComponent<NarrativeBubbleView>();var factory=new Factory();bubbles.ConfigureSurfaceFactory(factory);var bubble=a.Root.AddComponent<NarrativeAmbientBubbleTrigger>();
            Check(bubble.Configure(runtime,registry,a.Actor,a.Shell,bubbles),"same NPC actual bubble chain configured");bubble.SetPoolId("amb_test");
            Check(bubble.DebugForceBubble()&&factory.Last!=null,"same NPC has actual Core/View bubble surface");a.Driver.Advance(0);Check(!a.View.Hidden&&!factory.Last.Disposed,"bubble and marker coexist without hiding each other");

            string issue;service.TryStart("dlg_normal","chr_npc#2",out issue);
            Check(a.View.Hidden&&b.View.Hidden,"start event immediately hides every subscribed NPC marker");
            Check(!factory.Last.Disposed,"hiding markers does not clear an existing bubble");a.Driver.Advance(.1);b.Driver.Advance(.1);Check(a.View.Hidden&&b.View.Hidden,"markers remain hidden for whole dialogue");
            talked.MarkTalked("dlg_normal");service.End();a.Driver.Advance(0);b.Driver.Advance(0);
            Check(!a.View.Hidden&&a.View.Alpha==0&&a.View.Tint.r==.2f&&a.View.Tint.b==.6f,"first return sample is already latest grey at alpha zero");
            Check(b.View.Glyph=="!"&&b.View.Tint.g==.75f,"story remains gold on return");
            a.Driver.Advance(.1);Near(a.View.Alpha,.25,"fade uses actual markerFadeBackTime");a.Driver.Advance(.3);Near(a.View.Alpha,1,"fade completes at configured duration");
            a.Root.SetActive(false);a.Driver.Advance(0);Check(a.View.Hidden,"inactive NPC hides marker");a.Root.SetActive(true);
            Set(a.Trigger,"triggerTemplate","alternate");a.Driver.Configure(runtime,service,a.Trigger,a.Shell,a.View);a.Driver.Advance(.5);Near(a.View.Bob,.3,"named template uses its own bob amplitude/period");
            Set(a.Trigger,"triggerTemplate","");trigger.markerBobPeriod=0;trigger.markerFadeBackTime=0;runtime.Configure(config,catalog);
            service.TryStart("dlg_normal","chr_npc#2",out issue);service.End(true);a.Driver.Advance(0);Near(a.View.Bob,0,"zero bob period is stationary");Near(a.View.Alpha,1,"zero fade-back is immediate");
            int hides=a.View.Hides;a.Driver.enabled=false;Life(a.Driver,"OnDisable");hides=a.View.Hides;service.TryStart("dlg_normal","chr_npc#3",out issue);
            Check(a.View.Hides==hides&&b.View.Hidden,"disabled driver unsubscribes while other marker still hides");a.Driver.enabled=true;Life(a.Driver,"OnEnable");Check(a.View.Hidden,"enable synchronizes running dialogue");service.End();a.Driver.Advance(0);
            a.Driver.Advance(-1);Check(a.View.Hidden,"invalid time safely hides");
            Life(a.Driver,"OnDestroy");Life(b.Driver,"OnDestroy");
            Console.WriteLine("HEAD_MARKER_DRIVER_RESULT "+JsonConvert.SerializeObject(new{passed=true,checks=details.Count,actualDriverCoreResolverTriggerRecordAndBubble=true,nativeUnity=false,details}));return 0;
        }
        catch(Exception ex){Console.Error.WriteLine("HEAD_MARKER_DRIVER_FAILED "+ex);return 1;}
    }
}
