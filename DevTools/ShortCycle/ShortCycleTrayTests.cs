using System;
using System.Collections.Generic;
using System.Reflection;
using EatWhat.Cooking.ShortCycle;

public static class ShortCycleTrayTests
{
    private static int assertions;

    public static string Run()
    {
        assertions = 0;
        var context = new ShortCycleContext { current_phase = ShortCyclePhase.Phase1 };
        var stack = ShortCycleTrayIdentity.ForStack("flour");
        Ok(ShortCycleTrayReservation.TryTake(context, "flour", stack, "stack", 4, 2).Succeeded, "first stack take");
        Ok(ShortCycleTrayReservation.TryTake(context, "flour", stack, "stack", 4, 2).Succeeded, "stack merge");
        Ok(context.prep_zone_contents.Count == 1 && context.prep_zone_contents[0].quantity == 2 &&
            context.placeholder_states[0].taken_count == 2, "stack uses one occupied slot");
        var before = Snapshot(context);
        Ok(!ShortCycleTrayReservation.TryTake(context, "flour", "wrong", "stack", 4, 2).Succeeded &&
            Snapshot(context) == before, "wrong stack instance is atomic");
        var single0 = ShortCycleTrayIdentity.ForUnit("tomato", 0);
        var single1 = ShortCycleTrayIdentity.ForUnit("tomato", 1);
        Ok(ShortCycleTrayReservation.TryTake(context, "tomato", single0, "single", 2, 2).Succeeded, "single unit take");
        before = Snapshot(context);
        Ok(!ShortCycleTrayReservation.TryTake(context, "tomato", single1, "single", 2, 2).Succeeded &&
            Snapshot(context) == before, "full rejection is atomic");
        Ok(ShortCycleTrayReservation.TryPutBack(context, "flour", stack).Succeeded &&
            context.prep_zone_contents.Count == 2 && context.prep_zone_contents[0].quantity == 1,
            "stack putback is one unit");
        Ok(ShortCycleTrayReservation.TryPutBack(context, "flour", stack).Succeeded &&
            context.prep_zone_contents.Count == 1 && context.prep_zone_contents[0].drag_order == 0,
            "stack removal compacts order");
        Ok(ShortCycleTrayReservation.TryTake(context, "tomato", single1, "single", 2, 2).Succeeded &&
            context.prep_zone_contents.Count == 2 &&
            context.prep_zone_contents[0].instance_id != context.prep_zone_contents[1].instance_id,
            "single units retain distinct identities");
        before = Snapshot(context);
        Ok(!ShortCycleTrayReservation.TryTake(context, "tomato", single0, "single", 2, 2).Succeeded &&
            Snapshot(context) == before, "duplicate unit is atomic");
        Ok(ShortCycleTrayReservation.TryPutBack(context, "tomato", single0).Succeeded &&
            context.prep_zone_contents.Count == 1 && context.prep_zone_contents[0].instance_id == single1 &&
            context.prep_zone_contents[0].drag_order == 0, "specific unit return retains other unit");
        before = Snapshot(context);
        Ok(!ShortCycleTrayReservation.TryTake(context, "tomato", null, "single", 1, 2).Succeeded &&
            Snapshot(context) == before, "owned count limits available units");
        context.current_phase = ShortCyclePhase.Phase0;
        before = Snapshot(context);
        Ok(!ShortCycleTrayReservation.TryTake(context, "tomato", single0, "single", 2, 2).Succeeded &&
            Snapshot(context) == before, "wrong phase rejects without write");
        context.current_phase = ShortCyclePhase.Phase1;
        Ok(ShortCycleTrayReservation.TryPutBack(context, "tomato", single1).Succeeded &&
            context.prep_zone_contents.Count == 0 && context.placeholder_states.Count == 0,
            "last return releases occupied slot");
        var pack = ShortCycleTrayIdentity.ForUnit("pack", 0);
        var box = ShortCycleTrayIdentity.ForUnit("box", 0);
        Ok(ShortCycleTrayReservation.TryTake(context, "pack", pack, "pack", 1, 2).Succeeded &&
            ShortCycleTrayReservation.TryTake(context, "box", box, "container", 1, 2).Succeeded &&
            context.prep_zone_contents.Count == 2, "pack and container each occupy one unit slot");
        Ok(ShortCycleTrayReservation.TryPutBack(context, "pack", pack).Succeeded &&
            context.prep_zone_contents.Count == 1 && context.prep_zone_contents[0].instance_id == box,
            "pack return releases one whole unit without merging container");
        Ok(ShortCycleTrayReservation.TryPutBack(context, "box", box).Succeeded &&
            context.prep_zone_contents.Count == 0, "container return releases its own whole instance");

        var route = new ShortCycleActionRouter();
        route.Configure(null);
        Ok(route.RegisteredActionNames.Count == ShortCycleActionNames.All.Count,
            "existing named-action count stays unchanged");
        var implemented = new[] {
            ShortCycleActionNames.TrayHighlightEnter, ShortCycleActionNames.TrayHighlightExit,
            ShortCycleActionNames.TrayToggleDetail, ShortCycleActionNames.FridgeFilter,
            ShortCycleActionNames.FridgeTidy, ShortCycleActionNames.InventoryTake,
            ShortCycleActionNames.InventoryPutBack, ShortCycleActionNames.EquipmentOpenEntry };
        foreach (var action in implemented)
            Ok(route.DispatchAction(action).ErrorCode != ShortCycleActionErrorCode.NotImplemented,
                "route is implemented: " + action);

        var session = new ShortCycleContext();
        session.Enter(null, ShortCycleTriggerContext.FreeTry);
        Ok(session.SelectRecipe("recipe-one", false) && session.EnterPhase1(),
            "selected recipe enters Phase1");
        Ok(ShortCycleTrayReservation.TryTake(session, "flour",
            ShortCycleTrayIdentity.ForStack("flour"), "stack", 2, 1).Succeeded,
            "session reserves before return");
        Ok(session.ReturnToPhase0() && session.prep_zone_contents.Count == 1 &&
            session.placeholder_states.Count == 1, "P1 return retains reservations");
        Ok(!session.SelectRecipe(null, false) && session.prep_zone_contents.Count == 1,
            "rejected recipe retains reservations");
        Ok(session.SelectRecipe("recipe-one", false) && session.prep_zone_contents.Count == 1,
            "same recipe retains reservations");
        Ok(session.SelectRecipe("recipe-two", false) && session.prep_zone_contents.Count == 0 &&
            session.placeholder_states.Count == 0, "different recipe clears P1 reservations");
        var fsmContext = new ShortCycleContext();
        var save = new ObservableNoOpSaveBoundary();
        var processor = new ShortCycleDataProcessor(
            new UnavailableInventoryConsumptionGateway("offline"), save,
            new UnavailablePhase2HandoffSink("offline"));
        var fsm = new ShortCycleStateMachine(fsmContext, save, processor, null);
        Ok(fsm.TryEnter(null, ShortCycleTriggerContext.FreeTry).Succeeded &&
            fsm.TryShowPhase0View(ShortCyclePhase0View.RecipeCatalog).Succeeded &&
            fsm.TrySelectRecipe("recipe-one", false).Succeeded &&
            fsm.TryStartCooking().Succeeded, "FSM reaches Phase1");
        Ok(fsm.TryOpenModal(ShortCycleModalId.Generic).Succeeded &&
            fsmContext.active_modal == ShortCycleModalId.Generic, "equipment uses Generic modal in Phase1");
        Ok(!fsm.TryAdvanceToPhase2().Succeeded &&
            fsmContext.current_phase == ShortCyclePhase.Phase1, "active equipment modal blocks P2");
        Ok(fsm.TryEscape().Succeeded && !fsmContext.active_modal.HasValue,
            "ESC closes Phase1 equipment modal");
        Ok(!fsm.TryOpenModal(ShortCycleModalId.StepDetail).Succeeded,
            "Phase1 cannot open unrelated modal");
        VerifyFridgeViews();
        VerifyRightClickBridge();
        VerifyActualInputRoutes();
        VerifyCancelVisualOnly();
        return assertions + " tray assertions passed";
    }

    private static void VerifyRightClickBridge()
    {
        var router = new ShortCycleActionRouter();
        router.Configure(null);
        var registry = (ShortCycleActionRegistry)typeof(ShortCycleActionRouter)
            .GetField("registry", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(router);
        registry.Unregister(ShortCycleActionNames.InventoryPutBack);
        var calls = 0;
        registry.Register(ShortCycleActionNames.InventoryPutBack, request => {
            calls++;
            return ShortCycleActionResult.Success();
        });
        var slotObject = new UnityEngine.GameObject();
        var slot = slotObject.AddComponent<TraySlot>();
        slot.ConfigureItem(new CK01ItemData { name = "test" }, 1, "test", "test-unit");
        var binding = slotObject.AddComponent<ShortCycleMouseActionBinding>();
        var drag = slotObject.AddComponent<MouseDraggableObject>();
        typeof(ShortCycleMouseActionBinding).GetField("actionRouter", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(binding, router);
        typeof(ShortCycleMouseActionBinding).GetField("actionName", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(binding, ShortCycleActionNames.InventoryPutBack);
        var mouse = new MouseManager();
        MouseManager.Instance = mouse;
        mouse.mouseInteractiveLayers.Push(new MouseInteractionLayer());
        mouse.currentDraggableObject = drag;
        typeof(ShortCycleMouseActionBinding).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(binding, null);
        MouseManager.TriggerMouseCancel();
        Ok(calls == 1, "right-click hovered tray dispatches one named putback action");
        mouse.currentDraggableObject = null;
        MouseManager.TriggerMouseCancel();
        Ok(calls == 1, "right-click away from tray does not put back");
        typeof(ShortCycleMouseActionBinding).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(binding, null);
        MouseManager.Instance = null;
    }

    private sealed class InputFixture
    {
        public ShortCycleContext Context;
        public ShortCycleSessionManager Session;
        public ShortCycleActionRouter Router;
        public ShortCycleInputLockController Lock;
        public FridgeSlot Fridge;
        public TraySlot Tray;
        public ShortCycleMouseActionBinding FridgeBinding;
        public ShortCycleMouseActionBinding TrayBinding;
        public ShortCycleMouseActionBinding TargetBinding;
        public MouseDraggableObject FridgeDrag;
        public MouseDraggableObject TrayDrag;
        public Collider2D TrayRegion;
        public DropZone TrayZone;
        public DropZone OutsideZone;
    }

    private static void SetField(object owner, string name, object value)
    {
        var field = owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null) throw new InvalidOperationException("Missing field " + name);
        field.SetValue(owner, value);
    }

    private static InputFixture CreateInputFixture()
    {
        var fixture = new InputFixture();
        var flour = new CK01ItemData { name = "flour", stack_type = "stack", display_order_in_fridge = 1 };
        var tomato = new CK01ItemData { name = "tomato", stack_type = "single", display_order_in_fridge = 2 };
        var catalog = new CK01GeneratedDataCatalog();
        catalog.ReplaceTables(new[] {
            new CK01GeneratedDataTable("Items", null, new[] {
                new CK01GeneratedDataRowReference("flour", flour),
                new CK01GeneratedDataRowReference("tomato", tomato) }),
            new CK01GeneratedDataTable("InitialInventory", null, new[] {
                new CK01GeneratedDataRowReference("flour", new CK01InitialInventoryData { name = "flour", quantity = 3 }),
                new CK01GeneratedDataRowReference("tomato", new CK01InitialInventoryData { name = "tomato", quantity = 1 }) }),
            new CK01GeneratedDataTable("CookingGameConfig", null, new[] {
                new CK01GeneratedDataRowReference("prep_zone_capacity",
                    new CK01CookingGameConfigData { name = "prep_zone_capacity", value = "2" }),
                new CK01GeneratedDataRowReference(CK01CookingGameConfigData.DemoFridgeCapacityLevelKey,
                    new CK01CookingGameConfigData { name = CK01CookingGameConfigData.DemoFridgeCapacityLevelKey, value = "fcap-input" }) }),
            new CK01GeneratedDataTable("FridgeCapacityLevels", null, new[] {
                new CK01GeneratedDataRowReference("fcap-input",
                    new CK01FridgeCapacityLevelData { name = "fcap-input", capacity = 30 }) })
        });
        var inventory = new InventoryManager();
        SetField(inventory, "dataCatalog", catalog);
        string diagnostic;
        Ok(inventory.TryRebuildFromDataCatalog(out diagnostic), "actual-route inventory fixture");
        var fridgeSlots = new FridgeSlot[30];
        for (var index = 0; index < fridgeSlots.Length; index++) fridgeSlots[index] = new FridgeSlot();
        var fridgeBinder = new ShortCycleFridgeBinder();
        SetField(fridgeBinder, "slots", fridgeSlots);
        fixture.Context = new ShortCycleContext { current_phase = ShortCyclePhase.Phase1 };
        Ok(fridgeBinder.BindView(catalog, inventory, null, false, fixture.Context, out diagnostic),
            "actual-route fridge fixture");
        fixture.Fridge = fridgeSlots[0];
        Ok(fixture.Fridge.ItemId == "flour" &&
            fixture.Fridge.InstanceId == ShortCycleTrayIdentity.ForStack("flour"), "actual-route fridge identity");
        var traySlots = new[] { new TraySlot(), new TraySlot() };
        var tray = new ShortCycleTrayInteraction();
        SetField(tray, "slots", traySlots);
        SetField(tray, "capacityText", new TMPro.TMP_Text());
        SetField(tray, "trayStateController", new ShortCycleTrayStateController());
        fixture.Tray = traySlots[0];
        fixture.Session = new ShortCycleSessionManager();
        var save = new ObservableNoOpSaveBoundary();
        var processor = new ShortCycleDataProcessor(
            new UnavailableInventoryConsumptionGateway("offline"), save,
            new UnavailablePhase2HandoffSink("offline"));
        SetField(fixture.Session, "stateMachine", new ShortCycleStateMachine(fixture.Context, save, processor, null));
        SetField(fixture.Session, "inventoryManager", inventory);
        SetField(fixture.Session, "dataCatalog", catalog);
        SetField(fixture.Session, "fridgeBinder", fridgeBinder);
        SetField(fixture.Session, "trayInteraction", tray);
        fixture.Lock = new ShortCycleInputLockController();
        fixture.Router = new ShortCycleActionRouter();
        SetField(fixture.Router, "inputLockController", fixture.Lock);
        fixture.Router.Configure(fixture.Session);
        fixture.FridgeBinding = fixture.Fridge.gameObject.AddComponent<ShortCycleMouseActionBinding>();
        fixture.FridgeBinding.Configure(fixture.Router, ShortCycleActionNames.InventoryTake);
        SetField(fixture.FridgeBinding, "sessionManager", fixture.Session);
        fixture.FridgeDrag = fixture.Fridge.gameObject.AddComponent<MouseDraggableObject>();
        fixture.FridgeDrag.allowedDropTags = DropTag.FridgeIngredient;
        fixture.TrayBinding = fixture.Tray.gameObject.AddComponent<ShortCycleMouseActionBinding>();
        fixture.TrayBinding.Configure(fixture.Router, ShortCycleActionNames.InventoryPutBack);
        fixture.TrayDrag = fixture.Tray.gameObject.AddComponent<MouseDraggableObject>();
        fixture.TrayDrag.allowedDropTags = DropTag.Ingredient;
        fixture.TrayZone = new DropZone { zoneTags = DropTag.FridgeIngredient,
            onDropEvent = new UnityEngine.Events.UnityEvent<MouseDraggableObject>(),
            onDropRejectedEvent = new UnityEngine.Events.UnityEvent<MouseDraggableObject>() };
        fixture.OutsideZone = new DropZone { zoneTags = DropTag.Ingredient,
            onDropEvent = new UnityEngine.Events.UnityEvent<MouseDraggableObject>(),
            onDropRejectedEvent = new UnityEngine.Events.UnityEvent<MouseDraggableObject>() };
        fixture.TrayRegion = new Collider2D { ContainsPoint = point => point.x >= 0f && point.x <= 10f };
        SetField(fixture.TrayBinding, "trayDropRegion", fixture.TrayRegion);
        fixture.TargetBinding = new UnityEngine.GameObject().AddComponent<ShortCycleMouseActionBinding>();
        fixture.TargetBinding.Configure(fixture.Router, ShortCycleActionNames.InventoryTake);
        fixture.TrayZone.onDropEvent.AddListener(fixture.TargetBinding.DispatchFromDropEvent);
        fixture.TrayZone.onDropRejectedEvent.AddListener(fixture.TargetBinding.DispatchFromRejectedDropEvent);
        fixture.OutsideZone.onDropEvent.AddListener(fixture.TargetBinding.DispatchFromDropEvent);
        fixture.OutsideZone.onDropRejectedEvent.AddListener(fixture.TargetBinding.DispatchFromRejectedDropEvent);
        return fixture;
    }

    private static void VerifyActualInputRoutes()
    {
        var f = CreateInputFixture();
        Ok(f.FridgeBinding.Dispatch().Succeeded && f.FridgeBinding.Dispatch().Succeeded &&
            f.Context.prep_zone_contents[0].quantity == 2, "real route reserves two stack units");
        Tools.MouseWorldPosition = new UnityEngine.Vector3(5f, 0f, 0f);
        var before = Snapshot(f.Context);
        Ok(!f.TrayZone.CanAccept(f.TrayDrag), "stub rejects tray tag as real zone does");
        f.TrayZone.OnDrop(f.TrayDrag);
        f.TrayBinding.DispatchFromDropHitEvent(f.TrayZone);
        Ok(Snapshot(f.Context) == before, "tray inside rejected-zone hit cancels without return");
        f.TrayBinding.DispatchFromMissedDropEvent();
        Ok(Snapshot(f.Context) == before, "tray miss while pointer remains inside cancels");
        Tools.MouseWorldPosition = new UnityEngine.Vector3(20f, 0f, 0f);
        f.TrayBinding.DispatchFromMissedDropEvent();
        Ok(f.Context.prep_zone_contents[0].quantity == 1 &&
            f.Context.placeholder_states[0].taken_count == 1, "tray miss returns exactly one stack unit");
        f.OutsideZone.OnDrop(f.TrayDrag);
        f.TrayBinding.DispatchFromDropHitEvent(f.OutsideZone);
        Ok(f.Context.prep_zone_contents.Count == 0 && f.Context.placeholder_states.Count == 0,
            "outside zone OnDrop then source hit returns once without take");
        before = Snapshot(f.Context);
        f.FridgeBinding.DispatchFromMissedDropEvent();
        f.OutsideZone.OnDrop(f.FridgeDrag);
        f.FridgeBinding.DispatchFromDropHitEvent(f.OutsideZone);
        Ok(Snapshot(f.Context) == before, "fridge miss and rejected hit never reserve");
        Ok(f.TrayZone.CanAccept(f.FridgeDrag), "stub accepts matching fridge tag");
        f.TrayZone.OnDrop(f.FridgeDrag);
        f.FridgeBinding.DispatchFromDropHitEvent(f.TrayZone);
        Ok(f.Context.prep_zone_contents.Count == 1 && f.Context.prep_zone_contents[0].quantity == 1,
            "accepted zone then source hit takes exactly once");
        Ok(f.FridgeBinding.Dispatch().Succeeded && f.Context.prep_zone_contents[0].quantity == 2,
            "fridge left click still takes one");
        var mouse = new MouseManager();
        MouseManager.Instance = mouse;
        mouse.mouseInteractiveLayers.Push(new MouseInteractionLayer());
        mouse.currentDraggableObject = f.FridgeDrag;
        typeof(ShortCycleMouseActionBinding).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(f.FridgeBinding, null);
        try
        {
            MouseManager.TriggerMouseCancel();
            Ok(f.Context.prep_zone_contents[0].quantity == 1 &&
                f.Context.placeholder_states[0].taken_count == 1, "reserved fridge right click returns one");
            before = Snapshot(f.Context);
            mouse.dragStarted = true;
            MouseManager.TriggerMouseCancel();
            Ok(Snapshot(f.Context) == before, "drag cancel does not return");
            mouse.dragStarted = false;
            mouse.dragPerforming = true;
            MouseManager.TriggerMouseCancel();
            Ok(Snapshot(f.Context) == before, "active drag does not return");
            mouse.dragPerforming = false;
            mouse.currentDraggableObject = new MouseDraggableObject();
            MouseManager.TriggerMouseCancel();
            Ok(Snapshot(f.Context) == before, "non-hover right click does not return");
            mouse.currentDraggableObject = f.FridgeDrag;
            using (f.Lock.Gate.Acquire("test"))
            {
                MouseManager.TriggerMouseCancel();
                Ok(Snapshot(f.Context) == before, "input gate lock blocks fridge right click");
            }
            f.Context.active_modal = ShortCycleModalId.Generic;
            MouseManager.TriggerMouseCancel();
            Ok(Snapshot(f.Context) == before, "modal blocks fridge right click");
            f.Context.active_modal = null;
            f.Context.current_phase = ShortCyclePhase.Phase0;
            MouseManager.TriggerMouseCancel();
            Ok(Snapshot(f.Context) == before, "wrong phase blocks fridge right click");
            f.Context.current_phase = ShortCyclePhase.Phase1;
            f.Fridge.ConfigureItem(new CK01ItemData { name = "flour", stack_type = "stack" },
                3, true, "flour", "wrong-instance", 1);
            MouseManager.TriggerMouseCancel();
            Ok(Snapshot(f.Context) == before, "wrong instance blocks fridge right click");
            f.Fridge.ConfigureItem(new CK01ItemData { name = "flour", stack_type = "stack" },
                3, true, "flour", ShortCycleTrayIdentity.ForStack("flour"), 1);
            MouseManager.TriggerMouseCancel();
            Ok(f.Context.prep_zone_contents.Count == 0 && f.Context.placeholder_states.Count == 0,
                "second right click releases final stack unit");
            before = Snapshot(f.Context);
            MouseManager.TriggerMouseCancel();
            Ok(Snapshot(f.Context) == before, "unreserved fridge slot cannot put back");
            Ok(f.FridgeBinding.Dispatch().Succeeded && f.Context.prep_zone_contents.Count == 1,
                "unreserved fridge left click still takes");
        }
        finally
        {
            typeof(ShortCycleMouseActionBinding).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(f.FridgeBinding, null);
            MouseManager.Instance = null;
        }
    }

    private static void VerifyCancelVisualOnly()
    {
        var f = CreateInputFixture();
        Ok(f.FridgeBinding.Dispatch().Succeeded && f.FridgeBinding.Dispatch().Succeeded,
            "cancel fixture reserves two units through the real route");
        var inventory = (InventoryManager)typeof(ShortCycleSessionManager)
            .GetField("inventoryManager", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(f.Session);
        var before = Snapshot(f.Context);
        var flour = inventory.GetQuantity("flour");
        var tomato = inventory.GetQuantity("tomato");
        var registry = (ShortCycleActionRegistry)typeof(ShortCycleActionRouter)
            .GetField("registry", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(f.Router);
        var routeCalls = 0;
        registry.Unregister(ShortCycleActionNames.InventoryTake);
        registry.Unregister(ShortCycleActionNames.InventoryPutBack);
        registry.Register(ShortCycleActionNames.InventoryTake, request => {
            routeCalls++; return ShortCycleActionResult.Success();
        });
        registry.Register(ShortCycleActionNames.InventoryPutBack, request => {
            routeCalls++; return ShortCycleActionResult.Success();
        });

        var trayHost = new UnityEngine.GameObject("tray-return-host").AddComponent<TransitionController>();
        var fridgeHost = new UnityEngine.GameObject("fridge-return-host").AddComponent<TransitionController>();
        SetField(trayHost, "presetTarget", f.Tray.transform);
        SetField(fridgeHost, "presetTarget", f.Fridge.transform);
        SetField(trayHost, "presets", new List<TransitionPreset> {
            new TransitionPreset { name = "Return", applyPosition = true,
                position = f.Tray.transform.localPosition, positionSpace = TransitionCoordinateSpace.Local }
        });
        SetField(fridgeHost, "presets", new List<TransitionPreset> {
            new TransitionPreset { name = "Return", applyPosition = true,
                position = f.Fridge.transform.localPosition, positionSpace = TransitionCoordinateSpace.Local }
        });
        SetField(f.TrayBinding, "rejectedDropTransition", trayHost);
        SetField(f.FridgeBinding, "rejectedDropTransition", fridgeHost);
        f.TrayBinding.CancelDragVisual();
        Ok(trayHost.StartedCoroutineCount == 1 && fridgeHost.StartedCoroutineCount == 0 &&
            ReferenceEquals(f.Tray.transform, typeof(TransitionController)
                .GetField("presetTarget", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(trayHost)),
            "tray cancel starts only its own stable Return target");
        f.FridgeBinding.CancelDragVisual();
        Ok(trayHost.StartedCoroutineCount == 1 && fridgeHost.StartedCoroutineCount == 1 &&
            ReferenceEquals(f.Fridge.transform, typeof(TransitionController)
                .GetField("presetTarget", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(fridgeHost)),
            "fridge cancel starts only its own stable Return target");
        f.Tray.gameObject.SetActive(false);
        f.TrayBinding.CancelDragVisual();
        Ok(trayHost.StartedCoroutineCount == 2 && trayHost.gameObject.activeInHierarchy,
            "hidden tray visual can request Return from active host");
        f.TrayBinding.CancelDragVisual();
        Ok(trayHost.StartedCoroutineCount == 3,
            "repeated cancel restarts only the same visual Return");
        SetField(f.TrayBinding, "rejectedDropTransition", null);
        f.TrayBinding.CancelDragVisual();
        Ok(trayHost.StartedCoroutineCount == 3,
            "missing Return transition is safe and does not borrow another slot");
        Ok(routeCalls == 0 && Snapshot(f.Context) == before &&
            inventory.GetQuantity("flour") == flour && inventory.GetQuantity("tomato") == tomato,
            "cancel never routes take or putback and preserves prep, placeholder, inventory order and counts");
    }

    private static void VerifyFridgeViews()
    {
        var a = new CK01ItemData { name = "item-a", stack_type = "single",
            display_order_in_fridge = 20, ingredient_categories = new List<string> { "tag_veg" } };
        var b = new CK01ItemData { name = "item-b", stack_type = "stack",
            display_order_in_fridge = 10, ingredient_categories = new List<string> { "tag_fruit" } };
        var c = new CK01ItemData { name = "item-c", stack_type = "container",
            display_order_in_fridge = 30, ingredient_categories = new List<string> { "tag_veg" },
            container_capacity = 1f, container_use_step = 1f };
        var catalog = new CK01GeneratedDataCatalog();
        catalog.ReplaceTables(new[] {
            new CK01GeneratedDataTable("Items", null, new[] {
                new CK01GeneratedDataRowReference("a", a),
                new CK01GeneratedDataRowReference("b", b),
                new CK01GeneratedDataRowReference("c", c) }),
            new CK01GeneratedDataTable("InitialInventory", null, new[] {
                new CK01GeneratedDataRowReference("a", new CK01InitialInventoryData { name = "a", quantity = 2 }),
                new CK01GeneratedDataRowReference("b", new CK01InitialInventoryData { name = "b", quantity = 3 }),
                new CK01GeneratedDataRowReference("c", new CK01InitialInventoryData { name = "c", quantity = 2 }) }),
            new CK01GeneratedDataTable("CookingGameConfig", null, new[] {
                new CK01GeneratedDataRowReference(CK01CookingGameConfigData.DemoFridgeCapacityLevelKey,
                    new CK01CookingGameConfigData { name = CK01CookingGameConfigData.DemoFridgeCapacityLevelKey, value = "fcap-test" }) }),
            new CK01GeneratedDataTable("FridgeCapacityLevels", null, new[] {
                new CK01GeneratedDataRowReference("fcap-test",
                    new CK01FridgeCapacityLevelData { name = "fcap-test", capacity = 30 }) })
        });
        var inventory = new InventoryManager();
        typeof(InventoryManager).GetField("dataCatalog", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(inventory, catalog);
        string diagnostic;
        Ok(inventory.TryRebuildFromDataCatalog(out diagnostic), "fixture inventory rebuild");
        var slots = new FridgeSlot[30];
        for (var index = 0; index < slots.Length; index++) slots[index] = new FridgeSlot();
        var binder = new ShortCycleFridgeBinder();
        typeof(ShortCycleFridgeBinder).GetField("slots", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(binder, slots);
        var context = new ShortCycleContext { current_phase = ShortCyclePhase.Phase1 };
        Ok(binder.BindView(catalog, inventory, null, false, context, out diagnostic) &&
            slots[0].ItemId == "a" && slots[1].ItemId == "a" &&
            slots[0].InstanceId != slots[1].InstanceId && slots[2].ItemId == "b" &&
            slots[3].ItemId == "c" && slots[4].ItemId == "c",
            "fridge expands nonstack units without reordering original rows");
        context.placeholder_states.Add(new PlaceholderState {
            item_id = "a", instance_id = slots[0].InstanceId,
            stack_type = "single", taken_count = 1, is_taken = true });
        Ok(binder.BindView(catalog, inventory, null, false, context, out diagnostic) &&
            slots[0].Display.is_placeholder && slots[1].ItemId == "a" &&
            inventory.GetQuantity("a") == 2,
            "reservation marks one fridge instance without real deduction or reflow");
        Ok(binder.BindView(catalog, inventory, "tag_veg", false, context, out diagnostic) &&
            slots[0].ItemId == "a" && slots[1].ItemId == "a" &&
            slots[2].ItemId == "c" && slots[3].ItemId == "c" &&
            !slots[4].gameObject.activeSelf,
            "ingredient any-tag filter keeps matched unit instances");
        Ok(binder.BindView(catalog, inventory, null, true, context, out diagnostic) &&
            slots[0].ItemId == "b" && slots[1].ItemId == "a" &&
            slots[2].ItemId == "a" && slots[3].ItemId == "c" &&
            binder.IsTidied, "tidy uses stable display_order_in_fridge order");
    }
    private static string Snapshot(ShortCycleContext context)
    {
        var parts = new List<string>();
        foreach (var item in context.prep_zone_contents)
            parts.Add(item.instance_id + "/" + item.item_id + "/" + item.quantity + "/" + item.drag_order);
        parts.Add("|");
        foreach (var state in context.placeholder_states)
            parts.Add(state.instance_id + "/" + state.item_id + "/" + state.taken_count);
        return string.Join(";", parts.ToArray());
    }

    private static void Ok(bool condition, string label)
    {
        assertions++;
        if (!condition) throw new InvalidOperationException("AI325 " + label);
    }
}
