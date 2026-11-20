using System;
using System.Collections.Generic;
using System.Reflection;
using EatWhat.LongLoop;
using EatWhat.Narrative.Core;
using EatWhat.Narrative.Unity;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class NpcPresentationAnchorTests
{
    static readonly List<string> details = new List<string>();
    static void Check(bool value,string label){if(!value)throw new Exception("NPC anchors: "+label);details.Add(label);}
    static GameObject Child(GameObject parent,string name){var child=new GameObject(parent.scene,name);child.transform.SetParent(parent.transform,false);return child;}
    static void Rows(CK01GeneratedDataCatalog c,string table,params ScriptableObject[] rows){c.Rows[table]=new List<ScriptableObject>(rows);}
    static void Tick(NarrativeAmbientBubbleTrigger trigger,double seconds){Time.timeAsDouble=seconds;typeof(NarrativeAmbientBubbleTrigger).GetMethod("Update",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(trigger,null);}
    sealed class Surface:INarrativeBubbleSurface{public Transform Anchor;public bool Apply(float alpha,float rise){return true;}public void Dispose(){}}
    sealed class Factory:INarrativeBubbleSurfaceFactory
    {
        public readonly List<Surface> Created=new List<Surface>();
        public INarrativeBubbleSurface Create(Transform head,string key,string text,out string diagnostic){diagnostic=null;var surface=new Surface{Anchor=head};Created.Add(surface);return surface;}
    }
    static int Main()
    {
        var scene=new Scene(924,"AnchorFixture");var a=new GameObject(scene,"owner");var b=new GameObject(scene,"speaker");
        var shellA=a.AddComponent<NpcShell>();var shellB=b.AddComponent<NpcShell>();
        var footA=Child(a,"foot").transform;var visualA=Child(a,"visual").transform;var headA=Child(a,"head").transform;
        var headB=Child(b,"head").transform;shellA.ConfigureReferences(footA,visualA,headA);shellB.ConfigureReferences(Child(b,"foot").transform,Child(b,"visual").transform,headB);
        Check(shellA.MarkerAnchor==headA&&shellA.PromptAnchor==headA&&shellA.BubbleAnchor==headA,"all empty references fall back to existing head");
        var markerA=Child(a,"marker").transform;var promptA=Child(a,"prompt").transform;var bubbleA=Child(a,"bubble").transform;var bubbleB=Child(b,"bubble").transform;
        shellA.ConfigurePresentationAnchors(markerA,null,null);
        Check(shellA.MarkerAnchor==markerA&&shellA.PromptAnchor==headA&&shellA.BubbleAnchor==headA,"marker override independent of empty prompt and bubble");
        shellA.ConfigurePresentationAnchors(null,promptA,null);
        Check(shellA.MarkerAnchor==headA&&shellA.PromptAnchor==promptA&&shellA.BubbleAnchor==headA,"prompt override independent of empty marker and bubble");
        shellA.ConfigurePresentationAnchors(null,null,bubbleA);
        Check(shellA.MarkerAnchor==headA&&shellA.PromptAnchor==headA&&shellA.BubbleAnchor==bubbleA,"bubble override independent of empty marker and prompt");
        shellA.ConfigurePresentationAnchors(markerA,promptA,bubbleA);shellB.ConfigurePresentationAnchors(null,null,bubbleB);
        Check(shellA.MarkerAnchor==markerA&&shellA.PromptAnchor==promptA&&shellA.BubbleAnchor==bubbleA,"all three explicit points retained");
        Check(shellA.Foot==footA&&shellA.VisualMount==visualA&&shellA.HeadAnchor==headA,"new points do not replace old foot visual or head references");
        var newHead=Child(a,"other-head").transform;shellA.ConfigureReferences(footA,visualA,newHead);
        Check(shellA.MarkerAnchor==markerA&&shellA.PromptAnchor==promptA&&shellA.BubbleAnchor==bubbleA,"old ConfigureReferences preserves explicit presentation points");
        shellA.ConfigurePresentationAnchors(null,promptA,null);
        Check(shellA.MarkerAnchor==newHead&&shellA.PromptAnchor==promptA&&shellA.BubbleAnchor==newHead,"each fallback follows current head dynamically");
        shellA.ConfigureReferences(footA,visualA,headA);shellA.ConfigurePresentationAnchors(markerA,promptA,bubbleA);

        var managers=new GameObject(scene,"managers");var registry=managers.AddComponent<NarrativeTargetRegistry>();var runtime=managers.AddComponent<NarrativeRuntimeEntry>();
        var config=new DialoguePresentationConfig();config.Data.trigger[0].autoBubbleRadius=10;config.Data.trigger[0].autoBubbleCooldown=1;
        var catalog=new CK01GeneratedDataCatalog();
        Rows(catalog,"Characters",new CK01CharacterData{character_id="chr_owner"},new CK01CharacterData{character_id="chr_speaker"});
        Rows(catalog,"Dialogues",new CK01DialogueData{dialogue_id="dlg_anchor",tier="ambient",first_line="ln_owner"});
        Rows(catalog,"DialogueLines",new CK01DialogueLineData{line_id="ln_owner",dialogue_id="dlg_anchor",next_line="ln_speaker",text_key="anchor.owner"},new CK01DialogueLineData{line_id="ln_speaker",dialogue_id="dlg_anchor",speaker="chr_speaker",text_key="anchor.speaker"});
        Rows(catalog,"AmbientPools",new CK01AmbientPoolData{pool_id="amb_anchor",dialogue_ids=new List<string>{"dlg_anchor"}});
        runtime.Configure(config,catalog);INarrativeDataDirectory directory;Check(runtime.TryGetDirectory(out directory),"actual runtime adapter directory initialized");
        var actorA=a.AddComponent<NarrativeActorMarker>();actorA.ConfigureId("chr_owner#2");var actorB=b.AddComponent<NarrativeActorMarker>();actorB.ConfigureId("chr_speaker");
        registry.ConfigurePlayer(new GameObject(scene,"player").transform);registry.CollectSceneMarkers();
        var view=managers.AddComponent<NarrativeBubbleView>();var factory=new Factory();view.ConfigureSurfaceFactory(factory);
        var trigger=a.AddComponent<NarrativeAmbientBubbleTrigger>();Check(trigger.Configure(runtime,registry,actorA,shellA,view),"actual trigger shares scene registry shell runtime and view");trigger.SetPoolId("amb_anchor");
        Transform resolved;string issue;
        Check(trigger.TryGetSpeakerHead("chr_owner#2",out resolved,out issue)&&resolved==bubbleA,"owner speaker lookup uses own bubble point");
        Check(trigger.TryGetSpeakerHead("chr_speaker",out resolved,out issue)&&resolved==bubbleB,"explicit speaker lookup uses other real shell bubble point");
        Tick(trigger,0);Check(factory.Created.Count==1&&factory.Created[0].Anchor==bubbleA,"actual Trigger Driver Model View sends owner bubble point to external surface");
        Tick(trigger,.5);Check(factory.Created.Count==1,"original cooldown still blocks early next line");
        Tick(trigger,1);Check(factory.Created.Count==2&&factory.Created[1].Anchor==bubbleB,"actual second-line speaker sends speaker bubble point not pool owner point");
        shellA.ConfigurePresentationAnchors(markerA,promptA,null);Tick(trigger,2);
        Check(factory.Created.Count==3&&factory.Created[2].Anchor==headA,"owner empty bubble field preserves legacy head behavior through full chain");
        shellB.ConfigurePresentationAnchors(null,null,null);Tick(trigger,3);
        Check(factory.Created.Count==4&&factory.Created[3].Anchor==headB,"speaker empty bubble field preserves legacy head behavior through full chain");
        Check(shellA.MarkerAnchor==markerA&&shellA.PromptAnchor==promptA&&markerA.gameObject.activeInHierarchy&&promptA.gameObject.activeInHierarchy,"bubble emission and fallback never hide marker or prompt anchors");
        shellB.ConfigurePresentationAnchors(null,null,bubbleB);bubbleB.gameObject.SetActive(false);
        Check(shellB.BubbleAnchor==bubbleB&&!trigger.TryGetSpeakerHead("chr_speaker",out resolved,out issue)&&resolved==null,"inactive explicit bubble is not replaced by another presentation point");
        Check(!trigger.TryGetSpeakerHead("chr_missing",out resolved,out issue)&&issue!=null,"unknown exact speaker remains unavailable with diagnostic");
        var empty=new GameObject(scene,"empty").AddComponent<NpcShell>();Check(empty.MarkerAnchor==null&&empty.PromptAnchor==null&&empty.BubbleAnchor==null,"no configured head or override remains absent");
        Console.WriteLine("NPC_PRESENTATION_ANCHORS_RESULT={\"passed\":true,\"checks\":"+details.Count+",\"actualShellRegistryTriggerDriverModelView\":true,\"nativeUnity\":false,\"details\":[\""+string.Join("\",\"",details.ToArray())+"\"]}");return 0;
    }
}
