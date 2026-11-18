using System;
using System.Reflection;
using EatWhat.LongLoop;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class NarrativeTargetTests
{
    private static int checks;
    private static void Yes(bool condition,string name)
    { checks++; if (!condition) throw new Exception(name); }
    private static void Event(object obj,string method)
    { obj.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(obj,null); }
    private static void Field(object obj,string name,object value)
    { obj.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(obj,value); }
    private static T Component<T>(Scene scene,GameObject parent=null) where T:Component,new()
    { return new GameObject(scene,parent).AddComponent<T>(); }
    private static NarrativeActorMarker Actor(Scene scene,string id,bool enabled=true,GameObject parent=null)
    {
        var m=Component<NarrativeActorMarker>(scene,parent); m.enabled=enabled;
        Field(m,"characterId",id); return m;
    }
    private static NarrativeAnchorMarker Anchor(Scene scene,string id)
    { var m=Component<NarrativeAnchorMarker>(scene); Field(m,"anchorId",id); return m; }
    private static bool Warned(string code)
    { return Debug.Warnings.Exists(x=>x.Contains("["+code+"]")); }
    public static int Main()
    {
        var street=new Scene(1,"Street"); var room=new Scene(2,"Room");
        var first=Actor(street,"chr_vendor"); var second=Actor(street,"chr_vendor#2");
        var other=Actor(room,"chr_vendor"); var disabled=Actor(street,"chr_sleeping",false);
        var hidden=Actor(street,"chr_hidden"); hidden.gameObject.activeSelf=false;
        var parent=new GameObject(street); parent.transform.position=new Vector3(10,20,3);
        var nested=Actor(street,"chr_nested",true,parent);
        var anchor=Anchor(street,"door"); anchor.transform.position=new Vector3(3,4,5);
        // Markers may be enabled before the registry exists; initial collection must still find them.
        Event(first,"OnEnable"); Event(second,"OnEnable");
        var registry=Component<NarrativeTargetRegistry>(street); Event(registry,"OnEnable");
        Transform t; Vector3 p;
        Yes(registry.TryGetActor("chr_vendor",out t)&&t==first.transform,"initial enabled marker collection");
        Yes(registry.TryGetActor("chr_vendor#2",out t)&&t==second.transform,"instance suffix independent");
        Yes(registry.TryGetActor("chr_nested",out t)&&t==nested.transform,"nested scene object collected");
        Yes(!registry.TryGetActor("chr_sleeping",out t)&&t==null,"disabled component excluded");
        Yes(!registry.TryGetActor("chr_hidden",out t),"inactive object excluded");
        Yes(registry.TryGetAnchorPosition("door",out p)&&p.x==3&&p.y==4&&p.z==5,"anchor actual world position");
        anchor.transform.position=new Vector3(8,9,10);
        Yes(registry.TryGetAnchorPosition("door",out p)&&p.x==8&&p.y==9&&p.z==10,"anchor position read at lookup");
        Yes(registry.TryResolve("actor:chr_vendor#2",out t)&&t==second.transform,"shared actor target grammar");
        Yes(registry.TryResolve("anchor:door",out t)&&t==anchor.transform,"shared anchor target grammar");
        Yes(!registry.TryGetActor("chr_vendor#3",out t)&&t==null&&Warned("MISSING_ACTOR"),"missing instance diagnostic");
        Yes(!registry.TryGetAnchorPosition("absent",out p)&&p.x==0&&p.y==0&&p.z==0&&Warned("MISSING_ANCHOR"),"missing anchor returns false/default");
        var duplicate=Actor(street,"chr_vendor"); Event(duplicate,"OnEnable");
        Yes(registry.TryGetActor("chr_vendor",out t)&&t==first.transform&&Warned("DUPLICATE_ACTOR"),"duplicate keeps first registration");
        first.ConfigureId("chr_vendor");
        Yes(registry.TryGetActor("chr_vendor",out t)&&t==first.transform,"same id configuration preserves first registration");
        Event(first,"OnEnable"); registry.CollectSceneMarkers();
        Yes(registry.TryGetActor("chr_vendor",out t)&&t==first.transform,"repeat registration/rescan idempotent");
        duplicate.enabled=false; Event(duplicate,"OnDisable");
        Yes(registry.TryGetActor("chr_vendor",out t)&&t==first.transform,"unregister duplicate preserves first");
        duplicate.enabled=true; Event(duplicate,"OnEnable"); first.enabled=false; Event(first,"OnDisable");
        Yes(registry.TryGetActor("chr_vendor",out t)&&t==duplicate.transform,"first removed exposes next active registration");
        first.enabled=true; Event(first,"OnEnable");
        Yes(registry.TryGetActor("chr_vendor",out t)&&t==duplicate.transform,"reenable joins after existing registration");
        duplicate.ConfigureId("chr_new_vendor");
        Yes(registry.TryGetActor("chr_vendor",out t)&&t==first.transform,"runtime id change unregisters old key");
        Yes(registry.TryGetActor("chr_new_vendor",out t)&&t==duplicate.transform,"runtime id change registers new key");
        var duplicateAnchor=Anchor(street,"door"); Event(duplicateAnchor,"OnEnable");
        Yes(registry.TryResolve("anchor:door",out t)&&t==anchor.transform&&Warned("DUPLICATE_ANCHOR"),"anchor duplicate keeps first");
        anchor.enabled=false; Event(anchor,"OnDisable");
        Yes(registry.TryResolve("anchor:door",out t)&&t==duplicateAnchor.transform,"anchor unregister promotes remaining");
        duplicateAnchor.ConfigureId("new_door");
        Yes(!registry.TryResolve("anchor:door",out t)&&registry.TryResolve("anchor:new_door",out t),"anchor configure updates index");
        disabled.enabled=true; Event(disabled,"OnEnable");
        Yes(registry.TryGetActor("chr_sleeping",out t)&&t==disabled.transform,"dynamic component enable registers");
        hidden.gameObject.activeSelf=true; Event(hidden,"OnEnable");
        Yes(registry.TryGetActor("chr_hidden",out t)&&t==hidden.transform,"dynamic object activation registers");
        hidden.gameObject.activeSelf=false; Event(hidden,"OnDisable");
        Yes(!registry.TryGetActor("chr_hidden",out t),"dynamic object deactivation unregisters");
        var otherRegistry=Component<NarrativeTargetRegistry>(room); Event(otherRegistry,"OnEnable");
        Yes(otherRegistry.TryGetActor("chr_vendor",out t)&&t==other.transform,"additive scene has independent same id");
        Event(other,"OnEnable");
        Yes(registry.TryGetActor("chr_vendor",out t)&&t==first.transform,"other scene notification cannot cross register");
        Yes(!otherRegistry.TryGetActor("chr_vendor#2",out t),"other scene cannot lookup street actor");
        var player=new GameObject(street); player.transform.position=new Vector3(4,5,0);
        Yes(!registry.TryGetPlayer(out t)&&t==null&&Warned("MISSING_PLAYER"),"unbound player diagnostic");
        registry.ConfigurePlayer(player.transform);
        Yes(registry.TryGetPlayer(out t)&&t==player.transform,"explicit player binding");
        Yes(registry.TryResolve("player",out t)&&t==player.transform,"actual parser player lookup");
        player.activeSelf=false;
        Yes(!registry.TryResolve("player",out t)&&t==null,"inactive player absent");
        player.activeSelf=true; registry.ConfigurePlayer(other.transform);
        Yes(!registry.TryGetPlayer(out t)&&t==null,"foreign player rejected");
        registry.ConfigurePlayer(player.transform);
        Yes(!registry.TryResolve("object:prop",out t)&&Warned("INVALID_TARGET"),"object grammar not invented");
        Yes(!registry.TryResolve(null,out t)&&t==null,"null target diagnostic no throw");
        Yes(!registry.TryGetActor("chr_vendor#bad",out t)&&Warned("INVALID_ID"),"invalid suffix uses shared rules");
        Yes(!registry.TryGetActor("Vendor",out t),"ids remain case sensitive");
        var invalid=Actor(street,""); Event(invalid,"OnEnable");
        Yes(!registry.TryGetActor("",out t)&&t==null,"empty marker not registered");
        registry.enabled=false; Event(registry,"OnDisable");
        Yes(!registry.TryGetActor("chr_vendor",out t)&&!registry.TryGetPlayer(out t),"disabled registry serves no target");
        var late=Actor(street,"chr_late"); Event(late,"OnEnable");
        registry.enabled=true; Event(registry,"OnEnable");
        Yes(registry.TryGetActor("chr_late",out t)&&t==late.transform,"reenable collects markers enabled during downtime");
        Yes(registry.TryResolve("player",out t)&&t==player.transform,"player binding survives registry disable");
        Event(late,"OnDisable"); Event(late,"OnDestroy"); UnityEngine.Object.Destroy(late);
        Yes(!registry.TryGetActor("chr_late",out t),"disable/destroy removal idempotent");
        // Unity null semantics also protect a missed destruction callback.
        UnityEngine.Object.Destroy(second);
        Yes(!registry.TryGetActor("chr_vendor#2",out t)&&t==null,"destroyed marker cannot be returned");
        Event(otherRegistry,"OnDisable"); otherRegistry.enabled=false;
        Yes(registry.TryGetActor("chr_vendor",out t)&&t==first.transform,"other scene registry shutdown isolated");
        Console.WriteLine("PASS "+checks+" narrative target actual-source checks (Unity boundary substituted)");
        return 0;
    }
}
