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
internal static class ScrollWheelProbe
{
    static string Output => SceneValidationSession.Output;
    static readonly MethodInfo wheel = typeof(MouseManager).GetMethod("OnScroll", BindingFlags.Instance | BindingFlags.NonPublic);
    static readonly MethodInfo hover = typeof(MouseManager).GetMethod("setMouseHover", BindingFlags.Instance | BindingFlags.NonPublic);
    static readonly MethodInfo focus = typeof(MouseManager).GetMethod("OnApplicationFocus", BindingFlags.Instance | BindingFlags.NonPublic);
    static readonly FieldInfo lastScrollTime = typeof(MouseManager).GetField("lastScrollTime", BindingFlags.Instance | BindingFlags.NonPublic);
    static T[] All<T>() where T : Component => UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(x => x.gameObject.scene.IsValid()).ToArray();
    static void Advance(int stage) => SceneValidationSession.Advance(stage);
    static void Check(bool condition, string message) => SceneValidationSession.Check(condition, message);

    [MenuItem("Tools/Project Validation/Wheel Input Regression %#F9")]
    public static void Run()
    {
        if (Mouse.current == null || wheel == null || hover == null || focus == null || lastScrollTime == null)
            throw new InvalidOperationException("Requires a current mouse and production wheel/hover/focus methods.");
        SceneValidationSession.Begin("wheel", "Assets/Scenes/ToolTests/FridgeScrollLab.unity");
    }
    static void Pulse(MouseManager manager,float value){
        InputState.Change(Mouse.current.scroll,new Vector2(0,value),InputUpdateType.Dynamic);
        InputState.Change(Mouse.current.scroll,new Vector2(0,value),InputUpdateType.Editor);
        if(Mathf.Abs(Mouse.current.scroll.ReadValue().y-value)>.00001f)throw new Exception("Injected wheel value is not visible to reader");
        Vector2 pointerWorld = manager.currentScrollableObject == null
            ? Vector2.zero
            : (Vector2)manager.currentScrollableObject.transform.position;
        wheel.Invoke(manager,new object[]{pointerWorld});
        File.AppendAllText(Output+"/pulses.txt","sent="+value+" read="+manager.LastRawScrollY+" pending="+manager.AccumulatedScroll+" seq="+manager.ScrollDispatchSequence+"\n");
        InputState.Change(Mouse.current.scroll,Vector2.zero,InputUpdateType.Dynamic);
        InputState.Change(Mouse.current.scroll,Vector2.zero,InputUpdateType.Editor);
    }
    internal static void Tick()
    {
            var driver=All<FridgeScrollLabDriver>().Single();var manager=MouseManager.Instance;var stage=SceneValidationSession.Stage;
            if(stage==0){driver.InitializeLabSession();driver.BeginSimulatedSession();manager.enabled=false;manager.currentScrollableObject=driver.ScrollAdapter.ScrollableObject;Check(Mathf.Approximately(manager.ScrollThreshold,1f),"T8 default threshold is one uniform Input System unit");Check(Mathf.Approximately(manager.ScrollCooldown,.2f),"T8 default cooldown is 0.2 seconds");lastScrollTime.SetValue(manager,-100f);SceneValidationSession.Sequence=manager.ScrollDispatchSequence;Advance(1);return;}
            int start=SceneValidationSession.Sequence;
            if(stage==1){Pulse(manager,1);Check(manager.ScrollDispatchSequence==start+1,"one normalized detent dispatches immediately");Pulse(manager,1f);Check(manager.ScrollDispatchSequence==start+1 && Mathf.Approximately(manager.AccumulatedScroll,1f),"cooldown retains at most one pending step");Advance(2);return;}
            if(stage==2){Pulse(manager,0);Check(manager.ScrollDispatchSequence==start+2,"pending input dispatches once after cooldown");Pulse(manager,0);Check(manager.ScrollDispatchSequence==start+2,"no runaway catch-up");Advance(3);return;}
            if(stage>=3&&stage<=7){Pulse(manager,.2f);if(stage==7)Check(manager.ScrollDispatchSequence==start+3,"five slow fractional pulses accumulate into one step");Advance(stage+1);return;}
            if(stage==8){Pulse(manager,.2f);Pulse(manager,-.5f);Pulse(manager,-.5f);Check(manager.LastDispatchedStep<0 && manager.ScrollDispatchSequence==start+4,"small reversal clears opposite remainder");Pulse(manager,1f);manager.currentScrollableObject=null;Pulse(manager,0);manager.currentScrollableObject=driver.ScrollAdapter.ScrollableObject;Advance(9);return;}
            if(stage==9)
            {
                Pulse(manager,0);
                Check(manager.ScrollDispatchSequence==start+4 && manager.AccumulatedScroll==0,"leaving target discards pending gesture");

                var target=driver.ScrollAdapter.ScrollableObject;
                var adapter=driver.ScrollAdapter;
                var binder=driver.TraceProbe.FridgeBinder;
                if(target==null||adapter==null||binder==null)throw new InvalidOperationException("Modal wheel verification wiring is incomplete.");
                var pointerWorld=(Vector2)target.transform.position;
                driver.PushModalComparison();
                Check(driver.LayerStack.IsTop(driver.ModalComparisonLayer),"modal comparison layer becomes the authoritative top layer");
                hover.Invoke(manager,new object[]{pointerWorld});
                Check(manager.currentScrollableObject==null,"zero-mask modal layer removes the fridge scroll target");

                var modalSequence=manager.ScrollDispatchSequence;
                var modalReceived=adapter.ReceivedStepCount;
                var modalDispatches=adapter.DispatchCount;
                var modalTier=binder.CurrentTier;
                lastScrollTime.SetValue(manager,-100f);
                Pulse(manager,1f);
                lastScrollTime.SetValue(manager,-100f);
                Pulse(manager,-1f);
                Check(manager.ScrollDispatchSequence==modalSequence && manager.AccumulatedScroll==0,
                    "nonzero wheel input remains blocked while the modal layer stays open");
                Check(adapter.ReceivedStepCount==modalReceived && adapter.DispatchCount==modalDispatches,
                    "blocked modal wheel input never reaches the production adapter");
                Check(binder.CurrentTier==modalTier,"blocked modal wheel input does not change the fridge tier");

                driver.PopModalComparison();
                hover.Invoke(manager,new object[]{pointerWorld});
                Check(manager.currentScrollableObject==target,"closing the modal restores the fridge scroll target");
                lastScrollTime.SetValue(manager,-100f);
                Pulse(manager,-1f);
                Check(manager.ScrollDispatchSequence==modalSequence+1 && adapter.ReceivedStepCount==modalReceived+1 &&
                    adapter.DispatchCount==modalDispatches+1 && binder.CurrentTier==modalTier-1,
                    "closing the modal restores manager, adapter, and tier movement");

                lastScrollTime.SetValue(manager,-100f);
                Pulse(manager,-1f);
                Pulse(manager,-1f);
                focus.Invoke(manager,new object[]{false});
                Check(manager.ScrollDispatchSequence==modalSequence+2 && manager.AccumulatedScroll==0,
                    "focus loss discards pending gesture");
                manager.enabled=true;
                SceneValidationSession.Finish();
            }
    }
}
