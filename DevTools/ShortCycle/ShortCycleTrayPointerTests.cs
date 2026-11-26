using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EatWhat.Cooking.ShortCycle;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

// Only the platform boundary is substituted. Gesture, hit arbitration, Button,
// Draggable, DropZone, reservation and inventory methods are production sources.
public static class ShortCycleTrayPointerTests
{
    private static readonly List<string> cases = new List<string>();
    public static IReadOnlyList<string> Cases { get { return cases.AsReadOnly(); } }
    public static int Checks { get { return cases.Count; } }
    private static void Check(bool value, string message)
    { if (!value) throw new Exception("TRAY-POINTER: " + message); cases.Add(message); }
    private static void Field(object owner, string name, object value)
    {
        for (var t = owner.GetType(); t != null; t = t.BaseType)
        { var field = t.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic); if (field != null) { field.SetValue(owner, value); return; } }
        throw new Exception("Missing fixture field " + name);
    }
    private static void Invoke(object owner, string name)
    { owner.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(owner, null); }
    private static Button_MouseInteract Button(GameObject go, Action click)
    {
        var b = go.AddComponent<Button_MouseInteract>(); b.selectEvent = new UnityEvent();
        b.selectEvent.AddListener(() => click()); Invoke(b, "Start"); return b;
    }
    private static Collider2D Collider(GameObject go, float min, float max)
    {
        var col = go.AddComponent<Collider2D>(); col.ContainsPoint = p => p.x >= min && p.x <= max;
        Physics2D.Colliders.Add(col); return col;
    }
    private static MouseDraggableObject Drag(GameObject go, DropTag tags, ShortCycleMouseActionBinding binding)
    {
        var d = go.AddComponent<MouseDraggableObject>(); d.allowedDropTags = tags;
        d.startDraggingEvent = new UnityEvent(); d.endDraggingEvent = new UnityEvent();
        d.draggingUpdateEvent = new UnityEvent(); d.cancelDraggingEvent = new UnityEvent();
        d.onDropHitEvent = new UnityEvent<DropZone>(); d.onDropMissedEvent = new UnityEvent();
        d.onDropHitEvent.AddListener(binding.DispatchFromDropHitEvent);
        d.onDropMissedEvent.AddListener(binding.DispatchFromMissedDropEvent);
        d.cancelDraggingEvent.AddListener(binding.CancelDragVisual); return d;
    }
    internal sealed class Fixture : IDisposable
    {
        public MouseManager Mouse;
        public ShortCycleContext Context;
        public ShortCycleSessionManager Session;
        public ShortCycleActionRouter Router;
        public FridgeSlot Fridge;
        public TraySlot Tray;
        public ShortCycleMouseActionBinding FridgeBinding, TrayBinding;
        public MouseDraggableObject FridgeDrag, TrayDrag;
        public Button_MouseInteract FridgeButton, TrayButton, SurfaceButton;
        public Collider2D Region;
        public int SurfaceClicks;
        public int Quantity { get { return Context.prep_zone_contents.Sum(x => x.quantity); } }
        public void Point(float x) { Tools.MouseWorldPosition = new Vector3(x, 0f, 0f); }
        public void Move(float x) { Point(x); Invoke(Mouse, "Update"); }
        public void Press(float x) { Point(x); Mouse.OnMouseConfirmPress(); Mouse.OnDragStart(default(UnityEngine.InputSystem.InputAction.CallbackContext)); }
        public void Release(float x) { Point(x); Mouse.OnMouseLeftButtonRelease(); }
        public void Reserve() { Check(FridgeBinding.Dispatch().Succeeded, "fixture reserves through actual action router"); }
        public void Dispose()
        { Invoke(FridgeBinding, "OnDisable"); Invoke(TrayBinding, "OnDisable"); Physics2D.Colliders.Clear(); MouseManager.Instance = null; UnityEngine.InputSystem.Mouse.current = null; }
    }
    internal static Fixture Create()
    {
        Physics2D.Colliders.Clear(); Tools.HasMouseWorldPosition = true;
        var f = new Fixture { Mouse = new MouseManager { dragMoveThreshold = .5f }, Context = new ShortCycleContext { current_phase = ShortCyclePhase.Phase1 } };
        MouseManager.Instance = f.Mouse;
        f.Mouse.mouseInteractiveLayers.Push(new MouseInteractionLayer { alloweInteractionLayers = new List<LayerMask> { 1 } });
        var flour = new CK01ItemData { name = "flour", stack_type = "stack", icon_sprite = new Sprite { name = "flour" } };
        var catalog = new CK01GeneratedDataCatalog();
        catalog.ReplaceTables(new[] {
            new CK01GeneratedDataTable("Items", null, new[] { new CK01GeneratedDataRowReference("flour", flour) }),
            new CK01GeneratedDataTable("InitialInventory", null, new[] { new CK01GeneratedDataRowReference("flour", new CK01InitialInventoryData { quantity=4 }) }),
            new CK01GeneratedDataTable("CookingGameConfig", null, new[] {
                new CK01GeneratedDataRowReference("prep_zone_capacity", new CK01CookingGameConfigData { name="prep_zone_capacity", value="2" }),
                new CK01GeneratedDataRowReference(CK01CookingGameConfigData.DemoFridgeCapacityLevelKey, new CK01CookingGameConfigData { name=CK01CookingGameConfigData.DemoFridgeCapacityLevelKey, value="fcap" }) }),
            new CK01GeneratedDataTable("FridgeCapacityLevels", null, new[] { new CK01GeneratedDataRowReference("fcap", new CK01FridgeCapacityLevelData { name="fcap", capacity=30 }) })
        });
        var inventory = new InventoryManager(); Field(inventory, "dataCatalog", catalog);
        string diagnostic; Check(inventory.TryRebuildFromDataCatalog(out diagnostic), "actual inventory catalog initializes");
        var fridgeSlots = Enumerable.Range(0, 30).Select(i => new FridgeSlot()).ToArray();
        var binder = new ShortCycleFridgeBinder(); Field(binder, "slots", fridgeSlots);
        f.Fridge = fridgeSlots[0]; f.Tray = new TraySlot(); var secondTray = new TraySlot();
        var tray = new ShortCycleTrayInteraction(); Field(tray, "slots", new[] { f.Tray, secondTray });
        Field(tray, "capacityText", new TMPro.TMP_Text()); Field(tray, "trayStateController", new ShortCycleTrayStateController());
        f.Session = new ShortCycleSessionManager(); var save = new ObservableNoOpSaveBoundary();
        Field(f.Session, "stateMachine", new ShortCycleStateMachine(f.Context, save, new ShortCycleDataProcessor(new UnavailableInventoryConsumptionGateway("offline"), save, new UnavailablePhase2HandoffSink("offline")), null));
        Field(f.Session, "inventoryManager", inventory); Field(f.Session, "dataCatalog", catalog);
        Field(f.Session, "fridgeBinder", binder); Field(f.Session, "trayInteraction", tray);
        foreach (var slot in fridgeSlots) Field(slot, "sessionManager", f.Session);
        Field(f.Tray, "sessionManager", f.Session); Field(secondTray, "sessionManager", f.Session);
        Check(binder.BindView(catalog, inventory, null, false, f.Context, out diagnostic), "actual fridge binder initializes");
        f.Router = new ShortCycleActionRouter(); f.Router.Configure(f.Session);
        f.Fridge.gameObject.AddComponent<SpriteRenderer>();
        f.Fridge.gameObject.AddComponent<SortingGroup>().sortingOrder = 15;
        Collider(f.Fridge.gameObject, -4f, 4f);
        f.FridgeBinding = f.Fridge.gameObject.AddComponent<ShortCycleMouseActionBinding>();
        f.FridgeBinding.Configure(f.Router, ShortCycleActionNames.InventoryTake); Field(f.FridgeBinding, "sessionManager", f.Session);
        f.FridgeButton = Button(f.Fridge.gameObject, f.FridgeBinding.DispatchFromMouseEvent);
        Field(f.Fridge, "mouseInteractBehaviours", new Behaviour[] { f.FridgeButton });
        f.FridgeDrag = Drag(f.Fridge.gameObject, DropTag.FridgeIngredient, f.FridgeBinding);
        f.Tray.gameObject.AddComponent<SpriteRenderer>(); f.Tray.gameObject.AddComponent<SortingGroup>().sortingOrder = 30;
        Collider(f.Tray.gameObject, 2f, 3f);
        f.TrayBinding = f.Tray.gameObject.AddComponent<ShortCycleMouseActionBinding>();
        f.TrayBinding.Configure(f.Router, ShortCycleActionNames.InventoryPutBack);
        f.TrayButton = Button(f.Tray.gameObject, f.TrayBinding.DispatchFromMouseEvent);
        Field(f.Tray, "mouseInteractBehaviours", new Behaviour[] { f.TrayButton });
        f.TrayDrag = Drag(f.Tray.gameObject, DropTag.Ingredient, f.TrayBinding);
        var trayBase = new GameObject("TrayBase"); trayBase.AddComponent<SpriteRenderer>().sortingOrder = 25;
        var region = new GameObject("TrayDropRegion_TUNE"); region.transform.SetParent(trayBase.transform, false);
        f.Region = Collider(region, 0f, 10f); Field(f.TrayBinding, "trayDropRegion", f.Region);
        var zone = region.AddComponent<DropZone>(); zone.zoneTags = DropTag.FridgeIngredient;
        zone.onDropEvent = new UnityEvent<MouseDraggableObject>(); zone.onDropRejectedEvent = new UnityEvent<MouseDraggableObject>();
        var dropBinding = region.AddComponent<ShortCycleMouseActionBinding>(); dropBinding.Configure(f.Router, ShortCycleActionNames.InventoryTake);
        zone.onDropEvent.AddListener(dropBinding.DispatchFromDropEvent); zone.onDropRejectedEvent.AddListener(dropBinding.DispatchFromRejectedDropEvent);
        f.SurfaceButton = Button(region, () => f.SurfaceClicks++);
        Invoke(f.FridgeBinding, "OnEnable"); Invoke(f.TrayBinding, "OnEnable");
        Check(tray.Refresh(f.Context, inventory, 2), "actual tray binder initializes empty slots");
        return f;
    }
    public static string Run()
    {
        cases.Clear();
        using (var f = Create())
        {
            f.Press(-2f); Check(f.Quantity == 0, "fridge press never reserves before release");
            f.Move(-1.8f); Check(f.Quantity == 0 && !f.Mouse.dragPerforming, "below-threshold movement remains a pending click");
            f.Release(-1.8f); Check(f.Quantity == 1, "short click reserves exactly once on release");
            f.Release(-1.8f); Check(f.Quantity == 1, "duplicate release cannot repeat a click");
        }
        using (var f = Create())
        {
            f.Move(5f); f.Press(-2f); f.Move(5f);
            Check(f.Mouse.currentDraggableObject == f.FridgeDrag && f.Mouse.dragPerforming && f.Quantity == 0, "fresh initial pointer source survives movement over a different hovered object");
            f.Release(5f); Check(f.Quantity == 1, "legal tray drop reserves once without a press click");
            f.Release(5f); Check(f.Quantity == 1, "duplicate release cannot repeat a drop");
        }
        using (var f = Create())
        {
            f.Press(-2f); f.Move(-8f); f.Release(-8f); Check(f.Quantity == 0, "fridge drag outside tray never reserves");
            f.Press(-2f); f.Move(5f); f.Mouse.OnMouseCancelPress(); f.Release(5f);
            Check(f.Quantity == 0 && !f.FridgeDrag.dragging, "right cancel ends a fridge gesture without click or take");
        }
        using (var f = Create())
        {
            f.Reserve(); f.Reserve(); f.Press(2.5f); f.Move(12f); f.Release(12f);
            Check(f.Quantity == 1, "tray unit returns once outside without hitting a fridge or zone");
            f.Release(12f); Check(f.Quantity == 1, "duplicate outside release cannot return another unit");
            f.Press(2.5f); f.Move(5f); f.Release(5f); Check(f.Quantity == 1, "tray release within tray keeps the reservation");
            f.Press(2.5f); f.Move(12f); f.Mouse.OnMouseCancelPress(); f.Release(12f);
            Check(f.Quantity == 1, "tray drag cancel preserves the reservation");
        }
        using (var f = Create())
        {
            f.Press(1f); Check(f.Quantity == 0 && f.SurfaceClicks == 1, "parent-rendered tray surface receives an ordinary press ahead of the rear fridge");
            Check(f.Mouse.currentDraggableObject == null, "tray surface blocks dragging the rear fridge");
            f.Release(1f); Check(f.Quantity == 0, "tray surface click cannot pass through on release");
            f.Reserve(); f.Point(1f); f.Mouse.OnMouseCancelPress(); Check(f.Quantity == 1, "tray surface blocks rear-fridge right click without a prior hover frame");
            f.Point(-2f); f.Mouse.OnMouseCancelPress(); Check(f.Quantity == 0, "outside tray the reserved fridge still returns by right click");
            f.Reserve(); f.Point(2.5f); f.Mouse.OnMouseCancelPress(); Check(f.Quantity == 0, "occupied tray slot wins right-click overlap over tray face and fridge");
        }
        using (var f = Create())
        {
            var ordinary = new GameObject("ordinary button"); ordinary.AddComponent<SpriteRenderer>().sortingOrder = 100;
            Collider(ordinary, 20f, 21f); var clicks = 0; Button(ordinary, () => clicks++);
            f.Press(20.5f); Check(clicks == 1, "ordinary button without draggable retains press-time selection");
            f.Release(20.5f); Check(clicks == 1, "ordinary button does not gain a second release click");
            f.Press(-2f); f.Mouse.mouseInteractiveLayers.Push(new MouseInteractionLayer { alloweInteractionLayers = new List<LayerMask> { 1 } });
            f.Release(-2f); Check(f.Quantity == 0, "switching input layer cancels the captured source");
        }
        using (var f = Create())
        {
            f.Point(-2f); f.Mouse.OnMouseConfirmPress(); f.Release(5f);
            Check(f.Quantity == 1 && !f.FridgeDrag.dragging, "fast release crosses threshold without Update or a hold-start callback");
        }
        using (var f = Create())
        {
            f.Point(-2f); f.Mouse.OnDragStart(default(UnityEngine.InputSystem.InputAction.CallbackContext));
            f.Mouse.OnMouseConfirmPress(); Check(f.Quantity == 0, "hold-start before press still waits for click release");
            f.Release(-2f); Check(f.Quantity == 1, "callback order reversal does not lose or duplicate a click");
        }
        using (var f = Create())
        {
            var ordinary = new GameObject("legacy co-located button and drag");
            ordinary.AddComponent<SpriteRenderer>().sortingOrder = 100; Collider(ordinary, 20f, 21f);
            var clicks = 0; var button = Button(ordinary, () => clicks++);
            var drag = ordinary.AddComponent<MouseDraggableObject>();
            drag.startDraggingEvent = new UnityEvent(); drag.endDraggingEvent = new UnityEvent();
            drag.cancelDraggingEvent = new UnityEvent(); drag.draggingUpdateEvent = new UnityEvent();
            f.Press(20.5f); Check(clicks == 1 && !button.DeferClickUntilDragRelease, "legacy co-located button/draggable retains press-time selection without the local opt-in");
            f.Release(20.5f); Check(clicks == 1, "legacy co-located button gets no extra release click");
            var pure = new GameObject("pure drag source"); Collider(pure, 30f, 31f);
            var pureDrag = pure.AddComponent<MouseDraggableObject>();
            pureDrag.startDraggingEvent = new UnityEvent(); pureDrag.cancelDraggingEvent = new UnityEvent();
            pureDrag.endDraggingEvent = new UnityEvent(); pureDrag.draggingUpdateEvent = new UnityEvent();
            var cancels = 0; pureDrag.cancelDraggingEvent.AddListener(() => cancels++);
            f.Press(30.5f); f.Move(35f); Check(pureDrag.dragging && f.Mouse.currentDraggableObject == pureDrag, "pure draggable without a pressable remains a valid captured source");
            f.Mouse.OnMouseCancelPress(); Check(!pureDrag.dragging && cancels == 1 && f.Quantity == 0, "pure drag cancel remains single and does not reserve inventory");
        }
        using (var f = Create())
        {
            f.Reserve(); f.Press(2.5f); f.Move(12f);
            typeof(MouseManager).GetMethod("OnApplicationFocus", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(f.Mouse, new object[] { false });
            f.Release(12f); Check(f.Quantity == 1 && !f.TrayDrag.dragging, "focus loss clears the captured tray gesture without returning a unit");
            Check(f.FridgeButton.DeferClickUntilDragRelease && f.TrayButton.DeferClickUntilDragRelease && !f.SurfaceButton.DeferClickUntilDragRelease,
                "only actual fridge/tray slot action bindings opt in, not the tray surface");
            Invoke(f.FridgeBinding, "OnDisable"); Check(!f.FridgeButton.DeferClickUntilDragRelease, "binding disable restores the original opt-in flag");
            Invoke(f.FridgeBinding, "OnEnable"); Check(f.FridgeButton.DeferClickUntilDragRelease, "binding re-enable reapplies only the current slot policy");
        }
        using (var f = Create())
        {
            var scope = new GameObject("scroll scope"); f.Fridge.transform.SetParent(scope.transform, false);
            var scrollRoot = new GameObject("scroll target"); scrollRoot.transform.SetParent(scope.transform, false);
            scrollRoot.AddComponent<SpriteRenderer>(); Collider(scrollRoot, -4f, -1f);
            var scroll = scrollRoot.AddComponent<MouseScrollableObject>(); scroll.scrollStepEvent = new UnityEvent<float>();
            var steps = 0; scroll.scrollStepEvent.AddListener(value => steps++);
            UnityEngine.InputSystem.Mouse.current = new UnityEngine.InputSystem.Mouse();
            UnityEngine.InputSystem.Mouse.current.scroll.Value = new Vector2(0f, 1f);
            Time.unscaledTime = 100f; f.Mouse.ConfigureScrollInput(1f, 0f);
            f.Move(-2f); Check(steps == 0 && f.Mouse.LastScrollResolutionReason == "pressable_blocked", "default wheel policy still blocks behind a foreground pressable");
            Field(scroll, "allowScrollThroughPressables", true); Field(scroll, "pressablePassThroughScope", scope.transform);
            f.Move(-2f); Check(steps == 1 && f.Mouse.LastScrollResolutionReason == "pressable_pass_through", "existing same-scope wheel opt-in still dispatches a step");
        }
        return "PASS: real MouseManager pointer lifecycle, framework event chain, current reservation, foreground sorting and ordinary inputs.";
    }
}

#if EATWHAT_TRAY_POINTER_PLATFORM
namespace UnityEngine.Events
{
    public class UnityEvent
    {
        private event Action handlers;
        public void AddListener(Action value) { handlers += value; }
        public void RemoveListener(Action value) { handlers -= value; }
        public void Invoke() { handlers?.Invoke(); }
        public int GetPersistentEventCount() { return 0; }
        public UnityEngine.Object GetPersistentTarget(int index) { return null; }
    }
}
namespace UnityEngine.EventSystems { }
namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.Field)] public sealed class TextAreaAttribute : Attribute { }
    public class RectTransform : Transform
    {
        public Rect rect;
        public Vector2 sizeDelta,pivot;
        public void GetWorldCorners(Vector3[] corners) { }
    }
}
namespace UnityEngine.InputSystem
{
    public class InputAction
    {
        public struct CallbackContext { }
        public event Action<CallbackContext> started;
        public void Disable() { }
        public void Enable() { }
        public void Raise() { started?.Invoke(default(CallbackContext)); }
    }
    public class InputActionMap { public InputAction FindAction(string name, bool required) { return new InputAction(); } }
    public class InputActionAsset { public InputActionMap FindActionMap(string name, bool required) { return new InputActionMap(); } }
    public class PlayerInput : UnityEngine.MonoBehaviour { public InputActionAsset actions = new InputActionAsset(); }
}
public static class Physics2D
{
    public static readonly List<Collider2D> Colliders = new List<Collider2D>();
    public static Collider2D[] OverlapPointAll(Vector2 point, LayerMask mask)
    { return Colliders.Where(c => c.enabled && c.gameObject.activeInHierarchy && (mask.value & (1 << c.gameObject.layer)) != 0 && c.OverlapPoint(point)).ToArray(); }
    public static Collider2D OverlapPoint(Vector2 point, LayerMask mask) { return OverlapPointAll(point, mask).FirstOrDefault(); }
}
#endif
