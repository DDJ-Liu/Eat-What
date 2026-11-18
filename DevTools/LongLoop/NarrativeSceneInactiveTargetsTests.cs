using System;
using EatWhat.LongLoop;
using UnityEngine;
using UnityEngine.SceneManagement;
internal static class NarrativeSceneInactiveTargetsTests
{
    static int checks;static void Check(bool value,string name){checks++;if(!value)throw new Exception(name);}
    public static int Main()
    {
        var scene=new Scene(712,"state");var registry=new GameObject(scene).AddComponent<NarrativeTargetRegistry>();
        var preset=new GameObject(scene);var marker=preset.AddComponent<NarrativeActorMarker>();marker.ConfigureId("chr_preset#2");preset.activeSelf=false;
        Transform found;Check(!registry.TryGetActor("chr_preset#2",out found),"normal resolution remains active only");
        Check(registry.TryGetActorForSceneState("chr_preset#2",out found)&&ReferenceEquals(found,preset.transform),"state lookup finds inactive preplaced without registration");
        marker.enabled=false;Check(!registry.TryGetActorForSceneState("chr_preset#2",out found),"disabled marker excluded before deferred destroy");
        marker.enabled=true;preset.activeSelf=true;registry.CollectSceneMarkers();
        Check(registry.TryGetActor("chr_preset#2",out found)&&ReferenceEquals(found,preset.transform),"normal active registration retained");
        var otherScene=new Scene(713,"other");new GameObject(otherScene).AddComponent<NarrativeActorMarker>().ConfigureId("chr_foreign");
        Check(!registry.TryGetActorForSceneState("chr_foreign",out found),"inactive lookup remains scene-local");
        Check(!registry.TryGetActorForSceneState("bad id",out found),"same original target grammar enforced");
        Console.WriteLine("SCENE_INACTIVE_TARGETS_PASS checks="+checks);return 0;
    }
}
