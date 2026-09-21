using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using EatWhat.Cooking.ShortCycle;

// Input System injection only; hardware focus/occlusion still require human testing.
internal static class MIG63WheelProbe
{
    static string Output => MIG63ValidationSession.Output;
    static readonly MethodInfo wheel = typeof(MouseManager).GetMethod("OnScroll", BindingFlags.Instance | BindingFlags.NonPublic);
    static T[] All<T>() where T : Component => UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(x => x.gameObject.scene.IsValid()).ToArray();
    static void Advance(int stage) => MIG63ValidationSession.Advance(stage);
    static void Check(bool condition, string message) => MIG63ValidationSession.Check(condition, message);

    [MenuItem("Tools/MIG63/Validation/Wheel Input Regression %#F9")]
    public static void Run()
    {
        if (Application.platform != RuntimePlatform.WindowsEditor || InputSystem.settings.scrollDeltaBehavior != InputSettings.ScrollDeltaBehavior.UniformAcrossAllPlatforms)
            throw new InvalidOperationException("This fixture covers the accepted Windows Uniform fix. Mac adaptation is MIG-F01, not yet validated.");
        if (Mouse.current == null || wheel == null) throw new InvalidOperationException("Requires a current mouse and production OnScroll method.");
        MIG63ValidationSession.Begin("wheel", "Assets/Scenes/ToolTests/FridgeScrollLab.unity");
    }
    static void Pulse(MouseManager manager,float value){
        InputState.Change(Mouse.current.scroll,new Vector2(0,value),InputUpdateType.Dynamic);
        InputState.Change(Mouse.current.scroll,new Vector2(0,value),InputUpdateType.Editor);
        if(Mathf.Abs(Mouse.current.scroll.ReadValue().y-value)>.00001f)throw new Exception("Injected wheel value is not visible to reader");
        wheel.Invoke(manager,null);
        File.AppendAllText(Output+"/pulses.txt","sent="+value+" read="+manager.LastRawScrollY+" pending="+manager.AccumulatedScroll+" seq="+manager.ScrollDispatchSequence+"\n");
        InputState.Change(Mouse.current.scroll,Vector2.zero,InputUpdateType.Dynamic);
        InputState.Change(Mouse.current.scroll,Vector2.zero,InputUpdateType.Editor);
    }
    internal static void Tick()
    {
            var driver=All<FridgeScrollLabDriver>().Single();var manager=MouseManager.Instance;var stage=MIG63ValidationSession.Stage;
            if(stage==0){driver.InitializeLabSession();driver.BeginSimulatedSession();manager.enabled=false;manager.currentScrollableObject=driver.ScrollAdapter.ScrollableObject;typeof(MouseManager).GetField("lastScrollTime",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(manager,-100f);MIG63ValidationSession.Sequence=manager.ScrollDispatchSequence;Advance(1);return;}
            int start=MIG63ValidationSession.Sequence;
            if(stage==1){Pulse(manager,1);Check(manager.ScrollDispatchSequence==start+1,"one normalized detent dispatches immediately");Pulse(manager,.1f);Check(manager.ScrollDispatchSequence==start+1 && manager.AccumulatedScroll>0,"cooldown retains small input");Advance(2);return;}
            if(stage==2){Pulse(manager,0);Check(manager.ScrollDispatchSequence==start+2,"pending input dispatches once after cooldown");Pulse(manager,0);Check(manager.ScrollDispatchSequence==start+2,"no runaway catch-up");Advance(3);return;}
            if(stage>=3&&stage<=7){Pulse(manager,.01f);if(stage==7)Check(manager.ScrollDispatchSequence==start+3,"five slow fractional pulses accumulate into one step");Advance(stage+1);return;}
            if(stage==8){Pulse(manager,.02f);Pulse(manager,-.05f);Check(manager.LastDispatchedStep<0 && manager.ScrollDispatchSequence==start+4,"small reversal clears opposite remainder");Pulse(manager,.1f);manager.currentScrollableObject=null;Pulse(manager,0);manager.currentScrollableObject=driver.ScrollAdapter.ScrollableObject;Advance(9);return;}
            if(stage==9){Pulse(manager,0);Check(manager.ScrollDispatchSequence==start+4 && manager.AccumulatedScroll==0,"leaving target discards pending gesture");Pulse(manager,.1f);Pulse(manager,.1f);driver.PushModalComparison();Pulse(manager,0);Check(manager.AccumulatedScroll==0,"layer change clears pending gesture");driver.PopModalComparison();manager.enabled=true;MIG63ValidationSession.Finish();}
    }
}
