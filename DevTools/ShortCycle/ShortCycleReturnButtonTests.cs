using System;
using System.Collections.Generic;
using System.Reflection;
using EatWhat.Cooking.ShortCycle;
using EatWhat.Tools.Layout;
using UnityEngine;

public static class ShortCycleReturnButtonTests
{
    private sealed class Pressable : MousePressableObject { }
    // Ancillary Behaviour flag coverage; no physical keyboard/input lifecycle claim.
    private sealed class Listener : MonoBehaviour { }
    private sealed class ButtonFixture
    {
        public GameObject Root;
        public SpriteRenderer Image, Label, OriginallyHidden;
        public Pressable Mouse;
        public Collider2D Collider;
        public Listener Keyboard, DisabledListener;
        public CameraViewportAnchor Anchor;
        public ShortCycleStateGatedInteraction Gate;
        public ShortCycleMouseActionBinding Binding;
    }
    private static readonly List<string> cases = new List<string>();
    public static IReadOnlyList<string> Cases { get { return cases.AsReadOnly(); } }
    public static int Checks { get { return cases.Count; } }
    private static void Check(bool value, string message)
    { if (!value) throw new Exception("RETURN-BUTTON: " + message); cases.Add(message); }
    private static void Field(object target, string name, object value)
    { target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value); }
    private static void Invoke(object target, string method)
    { target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null); }
    private static ButtonFixture Button(GameObject parent, string action, ShortCycleActionRouter router)
    {
        var b = new ButtonFixture { Root = new GameObject(action) };
        b.Root.transform.SetParent(parent.transform, false);
        b.Root.transform.localPosition = new Vector3(3f, 4f, -.2f);
        b.Root.transform.localScale = new Vector3(.75f, .75f, 1f);
        b.Image = b.Root.AddComponent<SpriteRenderer>();
        var label = new GameObject("label"); label.transform.SetParent(b.Root.transform, false);
        b.Label = label.AddComponent<SpriteRenderer>();
        b.OriginallyHidden = label.AddComponent<SpriteRenderer>(); b.OriginallyHidden.enabled = false;
        b.Mouse = b.Root.AddComponent<Pressable>(); b.Mouse.enabled = false;
        b.Collider = b.Root.AddComponent<Collider2D>();
        b.Keyboard = b.Root.AddComponent<Listener>();
        b.DisabledListener = label.AddComponent<Listener>(); b.DisabledListener.enabled = false;
        b.Anchor = b.Root.AddComponent<CameraViewportAnchor>();
        b.Binding = b.Root.AddComponent<ShortCycleMouseActionBinding>(); b.Binding.Configure(router, action);
        b.Gate = b.Root.AddComponent<ShortCycleStateGatedInteraction>();
        Field(b.Gate, "gatedBehaviours", new Behaviour[] { b.Mouse });
        return b;
    }
    private static bool Visible(Renderer renderer)
    { return renderer.enabled && renderer.gameObject.activeInHierarchy; }
    private static void State(ButtonFixture b, bool active, string stage)
    {
        Check(Visible(b.Image) == active && Visible(b.Label) == active, stage + ": button image and label match page focus");
        Check(b.Gate.InteractionAllowed == active && b.Mouse.enabled == active, stage + ": MouseInteract gate matches page focus");
        Check((b.Collider.enabled && b.Root.activeInHierarchy) == active, stage + ": hidden button collider cannot be hit");
        Check((b.Keyboard.enabled && b.Root.activeInHierarchy) == active, stage + ": linked listener flag matches page focus");
        Check(!b.OriginallyHidden.enabled && !b.DisabledListener.enabled, stage + ": initially disabled components stay disabled");
        Check(b.Anchor.enabled && b.Binding.enabled && b.Root.activeSelf &&
            b.Root.transform.localPosition.x == 3f && b.Root.transform.localScale.x == .75f,
            stage + ": viewport anchor, binding, object and user layout remain intact");
    }
    public static string Run()
    {
        cases.Clear();
        var router = new ShortCycleActionRouter();
        var registry = (ShortCycleActionRegistry)typeof(ShortCycleActionRouter).GetField("registry", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(router);
        var escapes = 0; var returns = 0;
        registry.Register(ShortCycleActionNames.Escape, request => { escapes++; return ShortCycleActionResult.Success(); });
        registry.Register(ShortCycleActionNames.Return, request => { returns++; return ShortCycleActionResult.Success(); });
        Field(router, "dispatcher", new ShortCycleActionDispatcher(registry, new ShortCycleInputGate()));
        var bookRoot = new GameObject("BookSet"); var fridgeRoot = new GameObject("FridgeSet");
        var bookBackground = bookRoot.AddComponent<SpriteRenderer>();
        var fridgeBackground = fridgeRoot.AddComponent<SpriteRenderer>();
        var escape = Button(bookRoot, ShortCycleActionNames.Escape, router);
        var back = Button(fridgeRoot, ShortCycleActionNames.Return, router);
        var book = bookRoot.AddComponent<ShortCyclePresentationSet>();
        var fridge = fridgeRoot.AddComponent<ShortCyclePresentationSet>();
        Field(book, "containerRoot", bookRoot); Field(fridge, "containerRoot", fridgeRoot);
        Field(book, "gatedInteractions", new MonoBehaviour[] { escape.Gate });
        Field(fridge, "gatedInteractions", new MonoBehaviour[] { back.Gate });
        var p0 = new ShortCycleSpace(); var p1 = new ShortCycleSpace();
        bookRoot.transform.SetParent(p0.transform, false); fridgeRoot.transform.SetParent(p1.transform, false);
        Field(p1, "spaceId", ShortCycleSpaceId.Phase1);
        Field(p0, "structureSets", new[] { book });
        Field(p1, "structureSets", new[] { fridge });
        Field(p1, "visualOnlySets", new[] { book });
        var director = new ShortCyclePresentationDirector(); Field(director, "spaces", new[] { p0, p1 });
        director.EnterSpace(ShortCycleSpaceId.Phase0);
        State(escape, true, "initial P0"); State(back, false, "initial P0");
        Check(escape.Binding.Dispatch().Succeeded && escapes == 1, "focused P0 mouse event reaches the existing dispatcher");
        Check(!back.Binding.Dispatch().Succeeded && returns == 0, "hidden P1 stale mouse event is rejected");
        for (var cycle = 0; cycle < 3; cycle++)
        {
            director.LeaveSpace(ShortCycleSpaceId.Phase0); director.EnterSpace(ShortCycleSpaceId.Phase1);
            State(escape, false, "P1 cycle " + cycle); State(back, true, "P1 cycle " + cycle);
            Check(Visible(bookBackground) && Visible(fridgeBackground), "P1 keeps book visual-only background and fridge background");
            var previousEscapes = escapes;
            Check(!escape.Binding.Dispatch().Succeeded && escapes == previousEscapes, "hidden P0 stale event cannot dispatch in P1");
            Check(back.Binding.Dispatch().Succeeded && returns == cycle + 1, "focused P1 return reaches the existing dispatcher");
            Check(router.DispatchAction(ShortCycleActionNames.Escape).Succeeded && escapes == previousEscapes + 1,
                "global ESC route remains independent of the hidden page button");
            director.LeaveSpace(ShortCycleSpaceId.Phase1); director.EnterSpace(ShortCycleSpaceId.Phase0);
            State(escape, true, "returned P0 cycle " + cycle); State(back, false, "returned P0 cycle " + cycle);
            Check(Visible(fridgeBackground) && fridgeRoot.activeSelf, "returned P0 preserves the previously arrived fridge structure");
        }
        escape.Gate.SetInteractionAllowed(false); escape.Gate.SetInteractionAllowed(false);
        State(escape, false, "repeated focus loss");
        Invoke(escape.Gate, "OnDisable"); Invoke(escape.Gate, "OnDestroy");
        Check(escape.Image.enabled && escape.Label.enabled && escape.Collider.enabled && escape.Keyboard.enabled &&
            !escape.Mouse.enabled && !escape.OriginallyHidden.enabled && !escape.DisabledListener.enabled,
            "disable and destroy restore original per-component state idempotently");
        Invoke(escape.Gate, "OnEnable"); escape.Gate.SetInteractionAllowed(true);
        State(escape, true, "reenabled focused page");
        var ordinary = Button(bookRoot, ShortCycleActionNames.InventoryTake, router);
        ordinary.Gate.SetInteractionAllowed(false);
        Check(ordinary.Image.enabled && ordinary.Label.enabled && ordinary.Collider.enabled && ordinary.Keyboard.enabled,
            "ordinary inventory gates retain their existing renderer and ancillary behaviour semantics");
        director.ClearSessionPresentation();
        Check(bookBackground.enabled && fridgeBackground.enabled && escape.Image.enabled && back.Image.enabled,
            "session cleanup restores original visuals without removing backgrounds");
        return "PASS: return-button visibility, hit flags, stale-event rejection, repeated page changes and lifecycle restoration.";
    }
}
