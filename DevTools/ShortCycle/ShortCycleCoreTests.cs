using System;
using System.Collections.Generic;
using System.Reflection;
using EatWhat.Cooking.ShortCycle;
using EatWhat.Tools.Rendering;
using UnityEngine;

namespace EatWhat.Cooking.ShortCycle.Tests
{
    public static class ShortCycleCoreTests
    {
        public static string Run()
        {
            VerifyTransitionTableAndContextSemantics();
            VerifyPresentationHostContract();
            VerifyLayeredEscapeContract();
            VerifyBeginSessionSequence();
            VerifySelectedRecipeInteractionGate();
            VerifySharedSetVisibilityAndRestoration();
            VerifyTravelerPlacementPaths();
            VerifyCameraFrameAndStatusBoard();
            VerifyTrayStateMachine();
            VerifyActionRegistryAndInputLock();
            VerifyNamedActionContractAndTransitionTable();
            VerifyCommandBridge();
            VerifyCameraSlotVisibilityContract();
            VerifyCameraPanCurveAndLifecycleContract();
            VerifyManualDriverKeyMap();
            VerifyGridPieceContracts();
            VerifyV22AnchorAndScaleContracts();
            VerifyV22SlotBindingAndSubCardState();
            VerifyV22OutlineProfiles();
            VerifyV2OutlineControls();
            VerifyFridgeCatRigContracts();
            VerifyFridgeAnchorAlignedPagingContracts();
            VerifyFridgeCatBlinkContracts();
            VerifyDiscreteScrollContracts();
            VerifyFridgeScrollBinderContracts();
            VerifyFridgeScrollAdapterGateAndSubscription();
            VerifyFridgeScrollTraceClassifierAndLifecycle();
            VerifyFridgeScrollLabTraceSessionsAndSimulationBoundary();
            VerifyMouseInteractionLayerStackBridge();
            VerifyDirectorLayerOwnership();
            VerifyGeneratedDataCatalogAndInventory();
            VerifyTrayControllerRequiresHost();
            return "PASS: CK01-C pure logic tests (FSM/session/presentation/actions/grid/data + v2.2 contracts + fridge-cat rig/blink/discrete scroll/real-wheel trace).";
        }

        private static void VerifyBeginSessionSequence()
        {
            var trace = new List<string>();
            var logs = new List<string>();
            Func<string, Func<ShortCycleTransitionResult>> successStep = name => () =>
            {
                trace.Add(name);
                return ShortCycleTransitionResult.Success();
            };

            var result = ShortCycleSessionManager.RunBeginSessionSequence(
                successStep("catalog"),
                successStep("fridge"),
                successStep("clue"),
                successStep("localization"),
                successStep("enter"), logs.Add);
            Assert(result.Succeeded, "The complete BeginSession sequence must succeed.");
            AssertTrace(trace, "catalog", "fridge", "clue", "localization", "enter");
            AssertTrace(logs, "[CK01-C-SESSION] step=1/5 Catalog ok",
                "[CK01-C-SESSION] step=2/5 FridgeBind ok", "[CK01-C-SESSION] step=3/5 ClueBoardBind ok",
                "[CK01-C-SESSION] step=4/5 LocRefresh ok", "[CK01-C-SESSION] step=5/5 FsmEnter ok");

            trace.Clear();
            logs.Clear();
            result = ShortCycleSessionManager.RunBeginSessionSequence(
                successStep("catalog"),
                successStep("fridge"),
                () => { trace.Add("clue-should-not-run"); return ShortCycleTransitionResult.Success(); },
                successStep("localization"),
                successStep("enter"),
                logs.Add,
                true);
            Assert(result.Succeeded, "A no-selection BeginSession sequence must succeed without clue binding.");
            AssertTrace(trace, "catalog", "fridge", "localization", "enter");
            AssertTrace(logs, "[CK01-C-SESSION] step=1/5 Catalog ok",
                "[CK01-C-SESSION] step=2/5 FridgeBind ok",
                "[CK01-C-SESSION] step=3/5 ClueBoardBind skipped(no recipe)",
                "[CK01-C-SESSION] step=4/5 LocRefresh ok", "[CK01-C-SESSION] step=5/5 FsmEnter ok");

            for (var failedIndex = 0; failedIndex < 5; failedIndex++)
            {
                logs.Clear();
                var calls = 0;
                var steps = new Func<ShortCycleTransitionResult>[5];
                for (var i = 0; i < 5; i++)
                {
                    var index = i;
                    steps[i] = () => { calls++; return index == failedIndex
                        ? ShortCycleTransitionResult.Failure("failure-" + index) : ShortCycleTransitionResult.Success(); };
                }
                var failed = ShortCycleSessionManager.RunBeginSessionSequence(steps[0], steps[1], steps[2], steps[3], steps[4], logs.Add);
                Assert(!failed.Succeeded && failed.Error == "failure-" + failedIndex && calls == failedIndex + 1,
                    "BeginSession must stop at each possible failing step without changing its diagnostic.");
                Assert(logs.Count == failedIndex, "A failed or unexecuted step must not emit an ok log.");
            }

            trace.Clear();
            result = ShortCycleSessionManager.RunBeginSessionSequence(
                successStep("catalog"),
                () =>
                {
                    trace.Add("fridge");
                    return ShortCycleTransitionResult.Failure("fridge failed");
                },
                successStep("clue"),
                successStep("localization"),
                successStep("enter"));
            Assert(!result.Succeeded && result.Error == "fridge failed",
                "BeginSession must return the first precise startup diagnostic.");
            AssertTrace(trace, "catalog", "fridge");
        }

        private static void VerifySharedSetVisibilityAndRestoration()
        {
            var root = new UnityEngine.GameObject();
            root.SetActive(true);
            var visible = new UnityEngine.Renderer();
            var originallyHidden = new UnityEngine.Renderer { enabled = false };
            root.transform.children = new UnityEngine.Object[] { visible, originallyHidden };
            var gate = new UnityEngine.MonoBehaviour();
            var disabledGate = new UnityEngine.MonoBehaviour { enabled = false };
            var set = new ShortCyclePresentationSet();
            SetPrivateField(set, "containerRoot", root);
            SetPrivateField(set, "deactivateMode", DeactivateMode.RendererHidden);
            SetPrivateField(set, "gatedInteractions", new[] { gate, disabledGate });
            SetPrivateField(set, "overflowRenderers", new[] { visible });
            var p0 = new ShortCycleSpace();
            var p1 = new ShortCycleSpace();
            SetPrivateField(p1, "spaceId", ShortCycleSpaceId.Phase1);
            SetPrivateField(p0, "viewTable", new[] { new ShortCycleViewSetEntry
                { view = ShortCyclePhase0View.ClueBoardShell, sets = new[] { set } } });
            SetPrivateField(p1, "structureSets", new[] { set });
            var director = new ShortCyclePresentationDirector();
            SetPrivateField(director, "spaces", new[] { p0, p1 });
            director.EnterSpace(ShortCycleSpaceId.Phase0);
            Assert(root.activeSelf && !visible.enabled && !gate.enabled, "Initial P0 must hide a resident Shared Set without stopping its host.");
            director.ShowView(ShortCycleSpaceId.Phase0, ShortCyclePhase0View.ClueBoardShell);
            Assert(visible.enabled && gate.enabled && !originallyHidden.enabled && !disabledGate.enabled,
                "P0 view must survive a later inactive P1 reference and preserve originally disabled components.");
            director.HideView(ShortCycleSpaceId.Phase0, ShortCyclePhase0View.ClueBoardShell);
            Assert(!visible.enabled && !gate.enabled && root.activeSelf, "Last view hide must hide renderers and gates only.");
            director.LeaveSpace(ShortCycleSpaceId.Phase0);
            director.EnterSpace(ShortCycleSpaceId.Phase1);
            director.HideView(ShortCycleSpaceId.Phase0, ShortCyclePhase0View.ClueBoardShell);
            Assert(visible.enabled && gate.enabled, "P1 structure reference must win over a P0 hide request.");
            director.LeaveSpace(ShortCycleSpaceId.Phase1);
            director.EnterSpace(ShortCycleSpaceId.Phase0);
            Assert(!visible.enabled && root.activeSelf, "Return to P0 without ClueBoard view must hide the external structure Set.");
            var staleGeneration = director.Generation - 1;
            typeof(ShortCyclePresentationDirector).GetMethod("CompleteArrival", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(director, new object[] { staleGeneration, p1 });
            Assert(!visible.enabled, "A stale arrival must not re-show a departed space.");
            director.ClearSessionPresentation();
            Assert(visible.enabled && !originallyHidden.enabled && gate.enabled && !disabledGate.enabled && root.activeSelf,
                "Session cleanup must restore the full entry-state table.");
            foreach (var callback in new[] { "OnDisable", "OnDestroy" })
            {
                set.Deactivate();
                InvokePrivateMethod(set, callback);
                Assert(visible.enabled && !originallyHidden.enabled && gate.enabled && !disabledGate.enabled && root.activeSelf,
                    callback + " must restore RendererHidden without deactivating the host.");
                InvokePrivateMethod(set, callback);
            }
            var normal = new ShortCyclePresentationSet();
            var normalRoot = new UnityEngine.GameObject();
            normalRoot.SetActive(true);
            SetPrivateField(normal, "containerRoot", normalRoot);
            normal.Deactivate();
            Assert(!normalRoot.activeSelf, "Default ContainerInactive behavior must remain compatible.");
            normal.RestoreTransientState();
            Assert(normalRoot.activeSelf, "ContainerInactive must restore the original host state.");

            normal.Activate();
            normalRoot.SetActive(false);
            var selfDisableSetActiveCalls = normalRoot.SetActiveCallCount;
            normalRoot.ThrowOnSetActive = true;
            InvokePrivateMethod(normal, "OnDisable");
            Assert(!normalRoot.activeSelf && normalRoot.SetActiveCallCount == selfDisableSetActiveCalls,
                "OnDisable must restore transient state without re-entering SetActive on its own inactive container.");
            normalRoot.ThrowOnSetActive = false;
            normalRoot.SetActive(true);
            InvokePrivateMethod(normal, "OnEnable");

            normal.gameObject.SetActive(false);
            var parentDisableSetActiveCalls = normalRoot.SetActiveCallCount;
            normalRoot.ThrowOnSetActive = true;
            InvokePrivateMethod(normal, "OnDisable");
            Assert(normalRoot.activeSelf && normalRoot.SetActiveCallCount == parentDisableSetActiveCalls,
                "A parent-driven lifecycle exit must not rewrite the child container active state.");
            normalRoot.ThrowOnSetActive = false;
            normal.gameObject.SetActive(true);
            InvokePrivateMethod(normal, "OnEnable");

            var destroySetActiveCalls = normalRoot.SetActiveCallCount;
            normalRoot.ThrowOnSetActive = true;
            InvokePrivateMethod(normal, "OnDestroy");
            Assert(normalRoot.SetActiveCallCount == destroySetActiveCalls,
                "OnDestroy must not reactivate or deactivate the presentation container.");
            normalRoot.ThrowOnSetActive = false;
        }

        private static void VerifyCameraFrameAndStatusBoard()
        {
            Assert(EatWhat.EditorTools.CameraFrameGizmo.FrameSize(null).x == 0f &&
                EatWhat.EditorTools.CameraFrameGizmo.FrameSize(null).y == 0f,
                "A missing camera must produce an empty frame size.");
            var camera = new UnityEngine.Camera { orthographicSize = 5f, aspect = 2f };
            var size = EatWhat.EditorTools.CameraFrameGizmo.FrameSize(camera);
            Assert(size.x == 20f && size.y == 10f, "Frame dimensions must derive from camera parameters.");
            camera.aspect = 0.5f;
            size = EatWhat.EditorTools.CameraFrameGizmo.FrameSize(camera);
            Assert(size.x == 5f && size.y == 10f, "Portrait aspect must not use a hardcoded stage width.");
            var phase1Marker = new UnityEngine.Transform();
            var phase0Marker = new UnityEngine.Transform();
            var rightMarker = new UnityEngine.Transform();
            var gizmo = new EatWhat.EditorTools.CameraFrameGizmo
            {
                referenceCamera = camera,
                phase1Marker = phase1Marker,
                phase0Marker = phase0Marker,
                rightMarker = rightMarker
            };
            Assert(object.ReferenceEquals(gizmo.GetMarker(0), phase1Marker), "Marker index 0 must remain Phase1.");
            Assert(object.ReferenceEquals(gizmo.GetMarker(1), phase0Marker), "Marker index 1 must remain Phase0.");
            Assert(object.ReferenceEquals(gizmo.GetMarker(2), rightMarker), "Marker index 2 must remain Right.");
            Assert(gizmo.GetMarker(-1) == null && gizmo.GetMarker(3) == null,
                "Out-of-range marker indices must remain null.");
            var gizmoType = typeof(EatWhat.EditorTools.CameraFrameGizmo);
            Assert(gizmoType.GetField("referenceCamera").FieldType == typeof(UnityEngine.Camera),
                "CameraFrameGizmo.referenceCamera must remain a public Camera field.");
            foreach (var fieldName in new[] { "phase1Marker", "phase0Marker", "rightMarker" })
            {
                Assert(gizmoType.GetField(fieldName).FieldType == typeof(UnityEngine.Transform),
                    "CameraFrameGizmo." + fieldName + " must remain a public Transform field.");
            }
            var board = new TMPro.TextMeshPro { fontSize = 4f };
            board.rectTransform.pivot = new UnityEngine.Vector2(.5f, .5f);
            board.transform.position = new UnityEngine.Vector3(2f, 3f, 4f);
            var original = board.transform.position;
            var driver = new ShortCycleManualVerificationDriver();
            SetPrivateField(driver, "statusBoard", board);
            SetPrivateField(driver, "statusCamera", camera);
            camera.orthographicSize = 7.2f;
            InvokePrivateMethod(driver, "LateUpdate");
            var expected = camera.ViewportToWorldPoint(new UnityEngine.Vector3(.03f, .97f, 1f));
            Assert(UnityEngine.Vector3.Distance(board.transform.position, expected) < .0001f && board.fontSize == 4f,
                "Status board must occupy the camera's 3%/97% viewport anchor.");
            Assert(board.rectTransform.pivot.x == 0f && board.rectTransform.pivot.y == 1f && board.alignment == TMPro.TextAlignmentOptions.TopLeft,
                "Status board content must start at the top-left anchor rather than extending left of it.");
            camera.transform.position = new UnityEngine.Vector3(30f, 1f, 0f);
            camera.orthographicSize = 14.4f;
            InvokePrivateMethod(driver, "LateUpdate");
            expected = camera.ViewportToWorldPoint(new UnityEngine.Vector3(.03f, .97f, 1f));
            Assert(UnityEngine.Vector3.Distance(board.transform.position, expected) < .0001f && board.fontSize == 8f,
                "Status board must follow camera motion and scale without accumulating font changes.");
            InvokePrivateMethod(driver, "OnDisable");
            Assert(UnityEngine.Vector3.Distance(board.transform.position, original) < .0001f && board.fontSize == 4f,
                "Disabling the probe must restore authored board position and font size.");
            Assert(board.rectTransform.pivot.x == .5f && board.rectTransform.pivot.y == .5f && board.alignment == TMPro.TextAlignmentOptions.Center,
                "Probe cleanup must restore authored alignment and pivot.");
            InvokePrivateMethod(driver, "OnDestroy");
        }

        private static void VerifyTravelerPlacementPaths()
        {
            var moving = new UnityEngine.Transform { position = new UnityEngine.Vector3(99f, 0f, 0f) };
            var phase0Anchor = new UnityEngine.Transform { position = new UnityEngine.Vector3(2f, 3f, 4f) };
            var phase1Anchor = new UnityEngine.Transform { position = new UnityEngine.Vector3(12f, 3f, 4f) };
            var transition = new global::TransitionBehaviour_Position { targetTransform = moving };
            var traveler = new ShortCycleTravelingElement();
            SetPrivateField(traveler, "movingTransform", moving);
            SetPrivateField(traveler, "positionTransition", transition);
            SetPrivateField(traveler, "anchors", new[]
            {
                new ShortCycleTravelAnchor { space = ShortCycleSpaceId.Phase0, anchor = phase0Anchor },
                new ShortCycleTravelAnchor { space = ShortCycleSpaceId.Phase1, anchor = phase1Anchor }
            });
            var p0 = new ShortCycleSpace();
            var p1 = new ShortCycleSpace();
            SetPrivateField(p1, "spaceId", ShortCycleSpaceId.Phase1);
            var director = new ShortCyclePresentationDirector();
            SetPrivateField(director, "spaces", new[] { p0, p1 });
            SetPrivateField(director, "travelingElements", new[] { traveler });

            director.EnterSpace(ShortCycleSpaceId.Phase0);
            Assert(UnityEngine.Vector3.Distance(moving.position, phase0Anchor.position) < .0001f && transition.StartedCoroutineCount == 0,
                "Initial P0 entry must snap every Traveler without starting an animation.");

            moving.position = new UnityEngine.Vector3(-10f, -10f, 0f);
            director.ShowView(ShortCycleSpaceId.Phase0, ShortCyclePhase0View.ClueBoardShell);
            Assert(UnityEngine.Vector3.Distance(moving.position, phase0Anchor.position) < .0001f && transition.StartedCoroutineCount == 0,
                "Opening the P0 clue board must correct an off-anchor Traveler instantly.");

            director.LeaveSpace(ShortCycleSpaceId.Phase0);
            director.EnterSpace(ShortCycleSpaceId.Phase1);
            Assert(transition.StartedCoroutineCount == 1,
                "P0-to-P1 travel must keep the authored animated MoveTo path.");
            Assert(traveler.SnapTo(ShortCycleSpaceId.Phase1) && transition.StoppedCoroutineCount == 1 &&
                UnityEngine.Vector3.Distance(moving.position, phase1Anchor.position) < .0001f,
                "SnapTo must cancel an active move and use the existing Phase1 anchor.");
            director.LeaveSpace(ShortCycleSpaceId.Phase1);
            director.EnterSpace(ShortCycleSpaceId.Phase0);
            Assert(transition.StartedCoroutineCount == 2,
                "P1-to-P0 travel must also keep the animated MoveTo path.");

            var incomplete = new ShortCycleTravelingElement();
            Assert(!incomplete.SnapTo(ShortCycleSpaceId.Phase0),
                "SnapTo must fail safely when the moving Transform or anchor is unassigned.");
        }

        private static void VerifyTransitionTableAndContextSemantics()
        {
            var trace = new List<string>();
            var saveBoundary = new TraceSaveBoundary(trace);
            var inventory = new TraceInventory(trace);
            var handoff = new TraceHandoff(trace);
            var context = new ShortCycleContext();
            var processor = new ShortCycleDataProcessor(inventory, saveBoundary, handoff);
            var machine = new ShortCycleStateMachine(context, saveBoundary, processor, new TraceExit());

            Assert(!machine.TryReturnToPhase0().Succeeded, "P0 return must reject before a P1 transition.");
            Assert(!machine.TryAdvanceToPhase2().Succeeded, "P1->P2 must reject outside P1.");
            Assert(machine.TryEnter(null, ShortCycleTriggerContext.FreeTry).Succeeded,
                "Entry without a selected recipe must succeed.");
            Assert(context.current_phase == ShortCyclePhase.Phase0, "Entry must set Phase0.");
            Assert(context.phase0_view == ShortCyclePhase0View.Cover, "Entry must start at the v1.4 Cover state.");
            Assert(!context.HasSelectedRecipe && string.IsNullOrEmpty(context.current_recipe_id),
                "A new session must not claim a selected recipe.");
            Assert(machine.CurrentState is ShortCyclePhase0State, "Entry must use the explicit Phase0 state class.");
            Assert(machine.CurrentPhase0State is ShortCyclePhase0CoverState, "Entry must use the explicit Cover sub-state class.");
            Assert(trace[0] == "save:EnterPhase0", "Entry must call save point 1.");

            Assert(machine.TryShowPhase0View(ShortCyclePhase0View.RecipeCatalog).Succeeded, "Catalog transition must be available in P0.");
            Assert(machine.CurrentPhase0State is ShortCyclePhase0CatalogState, "Catalog must use an explicit sub-state class.");
            Assert(!machine.TryCloseCatalog().Succeeded && context.phase0_view == ShortCyclePhase0View.RecipeCatalog,
                "Catalog close must reject with no selected recipe and preserve the view.");
            Assert(machine.TrySelectRecipe("rcp_test", false).Succeeded,
                "Catalog recipe selection must succeed.");
            Assert(context.HasSelectedRecipe && context.current_recipe_id == "rcp_test" &&
                machine.CurrentPhase0State is ShortCyclePhase0BrowseState,
                "Successful selection must set the readable flag and enter Browse.");
            Assert(machine.TryShowPhase0View(ShortCyclePhase0View.ClueBoardShell).Succeeded, "Clue-board shell transition must be available in P0.");
            Assert(machine.CurrentPhase0State is ShortCyclePhase0ClueBoardState, "Clue board must use an explicit sub-state class.");
            Assert(!machine.TrySelectRecipe("rcp_blocked", false).Succeeded,
                "Recipe selection must reject while the clue board is expanded.");
            Assert(machine.TryShowPhase0View(ShortCyclePhase0View.RecipeBrowse).Succeeded,
                "Clue board must close back to recipe browse before opening step detail.");
            Assert(machine.TryShowPhase0View(ShortCyclePhase0View.StepDetail).Succeeded, "Step-detail transition must be available in P0.");
            Assert(machine.CurrentPhase0State is ShortCyclePhase0BrowseState,
                "Step detail must remain orthogonal to the recipe-browse base state.");
            Assert(context.active_modal == ShortCycleModalId.StepDetail,
                "Opening step detail must record the active modal without replacing the base view.");
            Assert(machine.TryShowPhase0View(ShortCyclePhase0View.RecipeBrowse).Succeeded, "Closing step detail must return to the unchanged recipe-browse base state.");
            Assert(!context.active_modal.HasValue, "Closing step detail must clear the active modal.");

            Assert(machine.TryStartCooking().Succeeded, "P0 -> P1 must succeed after entry.");
            context.prep_zone_contents.Add(new PrepZoneItem { item_id = "ing_egg", quantity = 1, drag_order = 4, state = "prepared" });
            context.placeholder_states.Add(new PlaceholderState { instance_id = "slot-egg", item_id = "ing_egg", taken_count = 1 });
            context.current_filter_tag = "tag_ingredient_egg";
            context.current_scroll_offset = 0.75f;

            Assert(machine.TryReturnToPhase0().Succeeded, "P1 -> P0 return must succeed.");
            Assert(context.prep_zone_contents.Count == 1 && context.placeholder_states.Count == 1, "P1 -> P0 must retain P1 state.");
            Assert(context.current_filter_tag == "tag_ingredient_egg" && context.current_scroll_offset == 0.75f, "P1 -> P0 must retain navigation state.");

            Assert(machine.TryStartCooking().Succeeded, "Re-entering P1 must succeed.");
            var phase2Result = machine.TryAdvanceToPhase2();
            Assert(phase2Result.Succeeded, "P1 -> P2 processing must succeed with wired gateways.");
            Assert(context.current_phase == ShortCyclePhase.Phase2, "Processing must finish in Phase2.");
            Assert(machine.CurrentState is ShortCyclePhase2HandoffState, "P2 must use the explicit handoff state class.");
            Assert(context.prep_zone_contents.Count == 1, "P2 handoff must retain prep-zone contents.");
            Assert(context.placeholder_states.Count == 0 && context.current_filter_tag == null, "P2 finalization must clear placeholders and filter.");
            Assert(phase2Result.ProcessResult.ExecutedSteps.Count == 4, "Processor must publish all four required steps.");
            Assert(trace[trace.Count - 3] == "consume" && trace[trace.Count - 2] == "save:Phase1ToPhase2" && trace[trace.Count - 1] == "handoff", "Processor order must be consume -> save2 -> handoff before context finalization.");

            context = new ShortCycleContext();
            processor = new ShortCycleDataProcessor(inventory, saveBoundary, handoff);
            machine = new ShortCycleStateMachine(context, saveBoundary, processor, new TraceExit());
            Assert(machine.TryEnter("rcp_test", ShortCycleTriggerContext.FreeTry).Succeeded, "A cleared context must accept a new entry hint.");
            Assert(!context.HasSelectedRecipe && !machine.TryStartCooking().Succeeded,
                "An entry hint must not count as a selected recipe.");
            Assert(machine.TryShowPhase0View(ShortCyclePhase0View.RecipeCatalog).Succeeded &&
                machine.TrySelectRecipe("rcp_test", false).Succeeded && machine.TryStartCooking().Succeeded,
                "Second P0 -> P1 must require catalog selection first.");
            context.prep_zone_contents.Add(new PrepZoneItem { item_id = "ing_tomato" });
            context.placeholder_states.Add(new PlaceholderState { instance_id = "slot-tomato", item_id = "ing_tomato", is_taken = true });
            context.current_filter_tag = "tag_ingredient_vegetable";
            context.current_scroll_offset = 0.5f;
            Assert(machine.TryReturnToPhase0().Succeeded, "Second P1 -> P0 must succeed.");
            Assert(machine.TryShowPhase0View(ShortCyclePhase0View.RecipeCatalog).Succeeded &&
                machine.TrySelectRecipe("rcp_other", true).Succeeded, "Changing recipes through Catalog in P0 must succeed.");
            Assert(context.prep_zone_contents.Count == 0 && context.placeholder_states.Count == 0, "Changing dishes must clear P1 data.");
            Assert(context.current_filter_tag == null && context.current_scroll_offset == 0f && context.is_mismatch_dish, "Changing dishes must reset navigation and apply mismatch state.");
            Assert(machine.TryEscape().Succeeded && context.phase0_view == ShortCyclePhase0View.RecipeCatalog,
                "Browse ESC must return to Catalog.");
            Assert(machine.TryEscape().Succeeded && context.phase0_view == ShortCyclePhase0View.Cover,
                "Catalog ESC must return to Cover.");
            Assert(machine.TryEscape().Succeeded && context.current_phase == ShortCyclePhase.None,
                "Cover ESC must clear context and exit.");
            Assert(!context.HasSelectedRecipe, "Teardown must clear the selected-recipe flag.");
            Assert(machine.CurrentState == null, "ESC must release the explicit session state.");
        }

        private static void VerifyPresentationHostContract()
        {
            var host = new TracePresentationHost();
            var trace = new List<string>();
            var context = new ShortCycleContext();
            var saveBoundary = new TraceSaveBoundary(trace);
            var processor = new ShortCycleDataProcessor(new TraceInventory(trace), saveBoundary, new TraceHandoff(trace));
            var machine = new ShortCycleStateMachine(context, saveBoundary, processor, new TraceExit(), host);

            Assert(machine.TryEnter(null, ShortCycleTriggerContext.FreeTry).Succeeded,
                "The presentation trace session must enter.");
            AssertTrace(host.Trace, "EnterSpace:Phase0", "ShowView:Phase0:Cover");

            host.Trace.Clear();
            Assert(machine.TryShowPhase0View(ShortCyclePhase0View.RecipeCatalog).Succeeded,
                "The presentation trace must reach catalog.");
            AssertTrace(host.Trace, "HideView:Phase0:Cover", "ShowView:Phase0:RecipeCatalog");

            host.Trace.Clear();
            var rejectedClose = machine.TryCloseCatalog();
            Assert(!rejectedClose.Succeeded && rejectedClose.Error == "no_recipe",
                "No-recipe close_catalog must return its stable rejection reason.");
            AssertTrace(host.Trace);

            host.Trace.Clear();
            Assert(machine.TrySelectRecipe("rcp_trace", false).Succeeded,
                "Selecting from Catalog must reach Browse.");
            AssertTrace(host.Trace, "HideView:Phase0:RecipeCatalog", "ShowView:Phase0:RecipeBrowse");

            host.Trace.Clear();
            Assert(machine.TryShowPhase0View(ShortCyclePhase0View.StepDetail).Succeeded,
                "The presentation trace must open step detail.");
            AssertTrace(host.Trace, "PushModal:StepDetail");
            Assert(machine.CurrentPhase0State is ShortCyclePhase0BrowseState,
                "Opening a modal must not replace the base-state instance.");

            host.Trace.Clear();
            Assert(machine.TryShowPhase0View(ShortCyclePhase0View.RecipeBrowse).Succeeded,
                "The presentation trace must close step detail.");
            AssertTrace(host.Trace, "PopModal:StepDetail");

            host.Trace.Clear();
            Assert(machine.TryStartCooking().Succeeded, "The presentation trace must enter Phase1.");
            AssertTrace(host.Trace,
                "HideView:Phase0:RecipeBrowse",
                "LeaveSpace:Phase0",
                "EnterSpace:Phase1");

            host.Trace.Clear();
            Assert(machine.TryReturnToPhase0().Succeeded, "The presentation trace must return to Phase0.");
            AssertTrace(host.Trace,
                "LeaveSpace:Phase1",
                "EnterSpace:Phase0",
                "ShowView:Phase0:RecipeBrowse");

            host.Trace.Clear();
            Assert(machine.TryEscape().Succeeded, "Browse escape must step back to Catalog.");
            AssertTrace(host.Trace, "HideView:Phase0:RecipeBrowse", "ShowView:Phase0:RecipeCatalog");
        }

        private static void VerifyLayeredEscapeContract()
        {
            var host = new TracePresentationHost();
            var trace = new List<string>();
            var context = new ShortCycleContext();
            var saveBoundary = new TraceSaveBoundary(trace);
            var machine = new ShortCycleStateMachine(
                context,
                saveBoundary,
                new ShortCycleDataProcessor(new TraceInventory(trace), saveBoundary, new TraceHandoff(trace)),
                new TraceExit(),
                host);

            Assert(machine.TryEnter(null, ShortCycleTriggerContext.FreeTry).Succeeded &&
                machine.TryShowPhase0View(ShortCyclePhase0View.RecipeCatalog).Succeeded &&
                machine.TrySelectRecipe("rcp_escape", false).Succeeded &&
                machine.TryShowPhase0View(ShortCyclePhase0View.StepDetail).Succeeded,
                "The layered ESC fixture must reach a Browse modal.");
            host.Trace.Clear();
            Assert(machine.TryEscape().Succeeded && context.active_modal == null &&
                context.phase0_view == ShortCyclePhase0View.RecipeBrowse,
                "First ESC must close only the active modal.");
            AssertTrace(host.Trace, "PopModal:StepDetail");

            host.Trace.Clear();
            Assert(machine.TryEscape().Succeeded && context.phase0_view == ShortCyclePhase0View.RecipeCatalog,
                "Second ESC must step Browse back to Catalog.");
            AssertTrace(host.Trace, "HideView:Phase0:RecipeBrowse", "ShowView:Phase0:RecipeCatalog");
            host.Trace.Clear();
            Assert(machine.TryEscape().Succeeded && context.phase0_view == ShortCyclePhase0View.Cover,
                "Third ESC must step Catalog back to Cover.");
            AssertTrace(host.Trace, "HideView:Phase0:RecipeCatalog", "ShowView:Phase0:Cover");
            host.Trace.Clear();
            Assert(machine.TryEscape().Succeeded && context.current_phase == ShortCyclePhase.None,
                "Fourth ESC must teardown only from Cover.");
            AssertTrace(host.Trace, "LeaveSpace:Phase0", "ClearSessionPresentation");

            host = new TracePresentationHost();
            context = new ShortCycleContext();
            saveBoundary = new TraceSaveBoundary(trace);
            machine = new ShortCycleStateMachine(
                context,
                saveBoundary,
                new ShortCycleDataProcessor(new TraceInventory(trace), saveBoundary, new TraceHandoff(trace)),
                new TraceExit(),
                host);
            Assert(machine.TryEnter(null, ShortCycleTriggerContext.FreeTry).Succeeded &&
                machine.TryShowPhase0View(ShortCyclePhase0View.RecipeCatalog).Succeeded &&
                machine.TrySelectRecipe("rcp_clue", false).Succeeded &&
                machine.TryShowPhase0View(ShortCyclePhase0View.ClueBoardShell).Succeeded,
                "The clue-board ESC fixture must reach ClueBoardShell.");
            host.Trace.Clear();
            Assert(machine.TryEscape().Succeeded && context.phase0_view == ShortCyclePhase0View.RecipeBrowse,
                "ClueBoard ESC must close only the overlay and restore Browse.");
            AssertTrace(host.Trace, "HideView:Phase0:ClueBoardShell", "ShowView:Phase0:RecipeBrowse");

            Assert(!machine.TryCloseCover().Succeeded,
                "close_cover must reject outside Cover.");
            Assert(machine.TryEscape().Succeeded && machine.TryEscape().Succeeded &&
                context.phase0_view == ShortCyclePhase0View.Cover,
                "The close-cover fixture must step back to Cover first.");
            host.Trace.Clear();
            Assert(machine.TryCloseCover().Succeeded && context.current_phase == ShortCyclePhase.None,
                "close_cover must reuse Cover ESC teardown.");
            AssertTrace(host.Trace, "LeaveSpace:Phase0", "ClearSessionPresentation");
        }

        private static void VerifySelectedRecipeInteractionGate()
        {
            var session = new ShortCycleSessionManager();
            InvokePrivateMethod(session, "Awake");
            session.Context.Enter(null, ShortCycleTriggerContext.FreeTry);
            var behaviour = new UnityEngine.Behaviour();
            var gate = new ShortCycleStateGatedInteraction();
            SetPrivateField(gate, "sessionManager", session);
            SetPrivateField(gate, "requireSelectedRecipe", true);
            SetPrivateField(gate, "gatedBehaviours", new[] { behaviour });
            InvokePrivateMethod(gate, "OnEnable");
            gate.SetInteractionAllowed(true);
            Assert(!gate.InteractionAllowed && !behaviour.enabled,
                "A selected-recipe gate must stay disabled before selection.");
            Assert(session.Context.SelectRecipe("rcp_gate", false),
                "The gate fixture must accept a valid selection.");
            gate.SetInteractionAllowed(true);
            Assert(gate.InteractionAllowed && behaviour.enabled,
                "A selected-recipe gate must allow its presentation-owned interaction after selection.");
            gate.SetInteractionAllowed(false);
            Assert(!gate.InteractionAllowed && !behaviour.enabled,
                "Presentation focus must remain the outer interaction gate.");
            InvokePrivateMethod(gate, "OnDestroy");
        }

        private static void VerifyTrayStateMachine()
        {
            var host = new TracePresentationHost();
            var stateMachine = new ShortCycleTrayStateMachine(host);
            var transitions = new List<ShortCycleTrayView>();
            stateMachine.StateChanged += transitions.Add;
            stateMachine.Initialize();
            Assert(stateMachine.CurrentState is ShortCycleTrayCollapsedState, "Tray must initialize in Collapsed state class.");
            AssertTrace(host.Trace, "SetTrayView:Collapsed");
            host.Trace.Clear();
            Assert(stateMachine.TrySetState(ShortCycleTrayView.Highlight), "Collapsed -> Highlight must succeed.");
            Assert(stateMachine.CurrentState is ShortCycleTrayHighlightState, "Highlight must use an explicit state class.");
            Assert(stateMachine.TrySetState(ShortCycleTrayView.Detail), "Highlight -> Detail must succeed.");
            Assert(stateMachine.CurrentState is ShortCycleTrayDetailState, "Detail must use an explicit state class.");
            Assert(stateMachine.TrySetState(ShortCycleTrayView.Collapsed), "Detail -> Collapsed must succeed.");
            Assert(transitions.Count == 4, "Tray state changes must be observable exactly once per real transition.");
            AssertTrace(host.Trace,
                "SetTrayView:Highlight",
                "SetTrayView:Detail",
                "SetTrayView:Collapsed");
        }

        private static void VerifyActionRegistryAndInputLock()
        {
            var registry = new ShortCycleActionRegistry();
            var callCount = 0;
            Assert(registry.Register("test.action", request => { callCount++; return ShortCycleActionResult.Success(); }), "Action registration must succeed once.");
            Assert(!registry.Register("test.action", request => ShortCycleActionResult.Success()), "Duplicate action registration must be rejected.");

            var gate = new ShortCycleInputGate();
            var dispatcher = new ShortCycleActionDispatcher(registry, gate);
            using (gate.Acquire("test"))
            {
                var lockedResult = dispatcher.Dispatch(new ShortCycleActionRequest { ActionName = "test.action" });
                Assert(!lockedResult.Succeeded && lockedResult.ErrorCode == ShortCycleActionErrorCode.Locked,
                    "Input lock must block dispatch with the Locked error code.");
            }
            Assert(dispatcher.Dispatch(new ShortCycleActionRequest { ActionName = "test.action" }).Succeeded && callCount == 1, "Unlocked dispatcher must invoke the registered action once.");
            Assert(registry.Unregister("test.action"), "Action unregistration must succeed.");
            Assert(!dispatcher.Dispatch(new ShortCycleActionRequest { ActionName = "test.action" }).Succeeded, "Unregistered action must not dispatch.");
        }

        private static void VerifyNamedActionContractAndTransitionTable()
        {
            Assert(ShortCycleActionNames.All.Count == 23,
                "The ShortCycle named-action contract must expose exactly 23 actions.");
            var uniqueNames = new HashSet<string>(ShortCycleActionNames.All, StringComparer.Ordinal);
            Assert(uniqueNames.Count == ShortCycleActionNames.All.Count,
                "Every ShortCycle named action must be unique.");

            var router = new ShortCycleActionRouter();
            router.Configure(null);
            var registered = new HashSet<string>(router.RegisteredActionNames, StringComparer.Ordinal);
            Assert(registered.SetEquals(uniqueNames),
                "ActionRouter must register the complete named-action contract one-to-one.");

            var recipeField = typeof(ShortCycleActionRequest).GetField("recipe_id");
            Assert(recipeField != null && recipeField.FieldType == typeof(string),
                "select_recipe must carry its recipe_id through ShortCycleActionRequest.");
            Assert(typeof(ShortCycleActionRequest).GetField("tag_id").FieldType == typeof(string),
                "fridge filter actions must carry tag_id through ShortCycleActionRequest.");
            Assert(typeof(ShortCycleActionRequest).GetField("item_id").FieldType == typeof(string),
                "inventory actions must carry item_id through ShortCycleActionRequest.");
            Assert(typeof(ShortCycleActionRequest).GetField("delta").FieldType == typeof(float),
                "inventory and scroll actions must carry a numeric delta through ShortCycleActionRequest.");

            var notImplementedActions = new[]
            {
                ShortCycleActionNames.TrayHighlightEnter,
                ShortCycleActionNames.TrayHighlightExit,
                ShortCycleActionNames.TrayToggleDetail,
                ShortCycleActionNames.FridgeFilter,
                ShortCycleActionNames.FridgeTidy,
                ShortCycleActionNames.InventoryTake,
                ShortCycleActionNames.InventoryPutBack,
                ShortCycleActionNames.EquipmentOpenEntry
            };
            for (var index = 0; index < notImplementedActions.Length; index++)
            {
                var result = router.DispatchAction(new ShortCycleActionRequest { ActionName = notImplementedActions[index] });
                Assert(!result.Succeeded && result.ErrorCode == ShortCycleActionErrorCode.NotImplemented,
                    notImplementedActions[index] + " must remain an explicit NotImplemented stub.");
            }
            var disconnectedFridgeScroll = router.DispatchAction(new ShortCycleActionRequest
            {
                ActionName = ShortCycleActionNames.FridgeScroll,
                delta = 1f
            });
            Assert(!disconnectedFridgeScroll.Succeeded &&
                disconnectedFridgeScroll.ErrorCode == ShortCycleActionErrorCode.Rejected,
                "fridge.scroll must use its real handler and fail safely when the session coordinator is absent.");

            AssertPhase0ActionTransition(
                ShortCycleActionNames.OpenCover,
                ShortCyclePhase0View.Cover,
                ShortCyclePhase0View.RecipeCatalog);
            AssertPhase0ActionTransition(
                ShortCycleActionNames.OpenCatalog,
                ShortCyclePhase0View.RecipeBrowse,
                ShortCyclePhase0View.RecipeCatalog);
            AssertPhase0ActionTransition(
                ShortCycleActionNames.CloseCatalog,
                ShortCyclePhase0View.RecipeCatalog,
                ShortCyclePhase0View.RecipeBrowse);
            AssertPhase0ActionTransition(
                ShortCycleActionNames.OpenClueBoard,
                ShortCyclePhase0View.RecipeBrowse,
                ShortCyclePhase0View.ClueBoardShell);
            AssertPhase0ActionTransition(
                ShortCycleActionNames.CloseClueBoard,
                ShortCyclePhase0View.ClueBoardShell,
                ShortCyclePhase0View.RecipeBrowse);
            AssertPhase0ActionTransition(
                ShortCycleActionNames.OpenStepDetail,
                ShortCyclePhase0View.RecipeBrowse,
                ShortCyclePhase0View.StepDetail);
            AssertPhase0ActionTransition(
                ShortCycleActionNames.CloseStepDetail,
                ShortCyclePhase0View.StepDetail,
                ShortCyclePhase0View.RecipeBrowse);

            ShortCyclePhase0View ignored;
            Assert(!ShortCycleActionTransitionTable.TryGetTarget(
                    ShortCycleActionNames.OpenCatalog,
                    ShortCyclePhase0View.ClueBoardShell,
                    out ignored),
                "Catalog opening must reject while the clue board is expanded.");
        }

        private static void AssertPhase0ActionTransition(
            string actionName,
            ShortCyclePhase0View from,
            ShortCyclePhase0View expected)
        {
            ShortCyclePhase0View actual;
            Assert(ShortCycleActionTransitionTable.TryGetTarget(actionName, from, out actual) && actual == expected,
                actionName + " must transition " + from + " -> " + expected + ".");
        }

        private static void VerifyCommandBridge()
        {
            var commandMethods = typeof(ShortCycleCommandBridge).GetMethods(BindingFlags.Instance | BindingFlags.Public);
            var commandCount = 0;
            foreach (var method in commandMethods)
            {
                if (method.GetCustomAttributes(typeof(CommandAttribute), false).Length > 0)
                {
                    commandCount++;
                    var parameters = method.GetParameters();
                    Assert(method.ReturnType == typeof(void) && parameters.Length == 1 &&
                        parameters[0].ParameterType == typeof(CommandContext),
                        "ShortCycle command methods must keep the project void(CommandContext) contract.");
                }
            }
            Assert(commandCount == 23, "The one-way command bridge must expose all 23 named ShortCycle actions.");
        }

        private static void VerifyCameraSlotVisibilityContract()
        {
            Assert(ShortCycleOverflowVisibilityGate.ShouldBeVisibleAtRest(
                    ShortCycleCameraSlot.Phase0,
                    ShortCycleCameraSlot.Phase0),
                "A gate must initialize visible at its home camera slot.");
            Assert(!ShortCycleOverflowVisibilityGate.ShouldBeVisibleAtRest(
                    ShortCycleCameraSlot.Phase0,
                    ShortCycleCameraSlot.Phase1),
                "A gate must initialize hidden away from its home camera slot.");
            Assert(ShortCycleOverflowVisibilityGate.ShouldHideOnPanStart(
                    ShortCycleCameraSlot.Phase0,
                    ShortCycleCameraSlot.Phase0,
                    ShortCycleCameraSlot.Phase1),
                "Leaving home must hide overflow renderers when the pan starts.");
            Assert(ShortCycleOverflowVisibilityGate.ShouldHideOnPanStart(
                    ShortCycleCameraSlot.Phase0,
                    ShortCycleCameraSlot.Phase1,
                    ShortCycleCameraSlot.Phase0),
                "Returning home must remain hidden until PanCompleted.");
            Assert(!ShortCycleOverflowVisibilityGate.ShouldHideOnPanStart(
                    ShortCycleCameraSlot.Phase0,
                    ShortCycleCameraSlot.Phase1,
                    ShortCycleCameraSlot.RightReserved),
                "A pan entirely outside home must not alter an already hidden gate.");

            var startedEvent = typeof(ShortCycleCameraPanController).GetEvent("PanStarted");
            var completedEvent = typeof(ShortCycleCameraPanController).GetEvent("PanCompleted");
            Assert(startedEvent != null &&
                startedEvent.EventHandlerType == typeof(Action<ShortCycleCameraSlot, ShortCycleCameraSlot>),
                "PanStarted must publish typed from/to camera slots.");
            Assert(completedEvent != null &&
                completedEvent.EventHandlerType == typeof(Action<ShortCycleCameraSlot>),
                "PanCompleted must publish the completed camera slot.");

            var camera = new UnityEngine.Camera();
            var phase0Marker = new UnityEngine.Transform();
            camera.transform.position = new UnityEngine.Vector3(1f, 2f, 3f);
            phase0Marker.position = camera.transform.position;
            var inputLock = new ShortCycleInputLockController();
            var pan = new ShortCycleCameraPanController();
            SetPrivateField(pan, "controlledCamera", camera);
            SetPrivateField(pan, "phase0Marker", phase0Marker);
            SetPrivateField(pan, "inputLockController", inputLock);
            InvokePrivateMethod(pan, "Awake");
            var panStartedCount = 0;
            var panCompletedCount = 0;
            var callbackCount = 0;
            pan.PanStarted += (from, to) => panStartedCount++;
            pan.PanCompleted += slot => panCompletedCount++;
            Assert(pan.PanTo(ShortCycleCameraSlot.Phase0, () => callbackCount++),
                "A same-slot camera request must be accepted.");
            Assert(callbackCount == 1 && panStartedCount == 0 && panCompletedCount == 0 && !inputLock.Gate.IsLocked,
                "A same-slot camera request must callback synchronously without events or input lock.");

            var space = new ShortCycleSpace();
            SetPrivateField(space, "spaceId", ShortCycleSpaceId.Phase0);
            SetPrivateField(space, "cameraSlot", ShortCycleCameraSlot.Phase0);
            var director = new ShortCyclePresentationDirector();
            SetPrivateField(director, "spaces", new[] { space });
            SetPrivateField(director, "cameraPanController", pan);
            director.EnterSpace(ShortCycleSpaceId.Phase0);
            director.ShowView(ShortCycleSpaceId.Phase0, ShortCyclePhase0View.Cover);
            director.HideView(ShortCycleSpaceId.Phase0, ShortCyclePhase0View.Cover);
            Assert(panStartedCount == 0,
                "Same-space Enter/Show/Hide presentation calls must not raise PanStarted.");
        }

        private static void VerifyCameraPanCurveAndLifecycleContract()
        {
            var evaluateMethod = typeof(ShortCycleCameraPanController).GetMethod(
                "EvaluatePanProgress", BindingFlags.Static | BindingFlags.NonPublic);
            var durationMethod = typeof(ShortCycleCameraPanController).GetMethod(
                "IsUsableDuration", BindingFlags.Static | BindingFlags.NonPublic);
            Assert(evaluateMethod != null && durationMethod != null,
                "Camera pan must keep deterministic curve and duration guards.");

            var emptyCurve = new AnimationCurve();
            foreach (var progress in new[] { 0f, 0.25f, 0.5f, 0.75f, 1f })
            {
                var nullValue = (float)evaluateMethod.Invoke(null, new object[] { null, progress });
                var emptyValue = (float)evaluateMethod.Invoke(null, new object[] { emptyCurve, progress });
                Assert(Mathf.Approximately(nullValue, progress) && Mathf.Approximately(emptyValue, progress),
                    "Null and zero-key camera curves must both fall back to linear progress.");
            }

            var customCurve = new AnimationCurve(new Keyframe(0f, 0.35f), new Keyframe(1f, 0.35f));
            var customValue = (float)evaluateMethod.Invoke(null, new object[] { customCurve, 0.75f });
            Assert(Mathf.Approximately(customValue, 0.35f),
                "A configured camera curve must retain its authored evaluation instead of being forced linear.");

            foreach (var invalidDuration in new[] { 0f, -1f, float.NaN, float.PositiveInfinity })
            {
                Assert(!(bool)durationMethod.Invoke(null, new object[] { invalidDuration }),
                    "Non-positive and non-finite camera durations must be rejected.");
            }
            Assert((bool)durationMethod.Invoke(null, new object[] { 0.45f }),
                "The reviewed positive camera duration must remain usable.");

            var awakePan = new ShortCycleCameraPanController();
            SetPrivateField(awakePan, "panCurve", emptyCurve);
            InvokePrivateMethod(awakePan, "Awake");
            var awakeCurve = (AnimationCurve)GetPrivateField(awakePan, "panCurve");
            Assert(awakeCurve.length == 2 && Mathf.Approximately(awakeCurve.Evaluate(0.25f), 0.25f),
                "Awake must normalize an empty Inspector curve to an explicit two-key linear curve.");
            SetPrivateField(awakePan, "panCurve", customCurve);
            InvokePrivateMethod(awakePan, "Awake");
            Assert(ReferenceEquals(GetPrivateField(awakePan, "panCurve"), customCurve),
                "Awake must preserve a valid authored camera curve.");

            var previousDelta = Time.unscaledDeltaTime;
            Time.unscaledDeltaTime = 0.25f;
            var camera = new Camera();
            camera.transform.position = new Vector3(0f, 1f, -10f);
            var destination = new Vector3(-8f, 1f, -10f);
            var inputLock = new ShortCycleInputLockController();
            var pan = new ShortCycleCameraPanController();
            SetPrivateField(pan, "controlledCamera", camera);
            SetPrivateField(pan, "inputLockController", inputLock);
            SetPrivateField(pan, "panCurve", new AnimationCurve());
            SetPrivateField(pan, "panDuration", 1f);
            var completionCount = 0;
            var callbackCount = 0;
            pan.PanCompleted += slot => completionCount++;
            var routine = (System.Collections.IEnumerator)InvokePrivateMethodWithResult(
                pan, "PanRoutine", ShortCycleCameraSlot.Phase1, destination, new Action(() => callbackCount++));
            Assert(routine.MoveNext(), "A positive-duration camera pan must yield an intermediate frame.");
            Assert(camera.transform.position.x < 0f && camera.transform.position.x > destination.x,
                "An empty-curve camera pan must move to a strict intermediate position before completion.");
            Assert(inputLock.Gate.IsLocked, "The camera pan must hold the input lock while interpolating.");
            Assert(routine.MoveNext() && routine.MoveNext() && routine.MoveNext(),
                "The reviewed one-second camera pan must produce all quarter-step samples.");
            Assert(!routine.MoveNext(), "The camera pan must terminate after reaching the destination.");
            Assert(Vector3.Distance(camera.transform.position, destination) < 0.0001f &&
                pan.CurrentSlot == ShortCycleCameraSlot.Phase1,
                "The camera pan must land exactly on the requested marker and update CurrentSlot only at completion.");
            Assert(completionCount == 1 && callbackCount == 1 && !inputLock.Gate.IsLocked,
                "A completed camera pan must release its lock and invoke each completion path exactly once.");

            var invalidCamera = new Camera();
            var invalidLock = new ShortCycleInputLockController();
            var invalidPan = new ShortCycleCameraPanController();
            SetPrivateField(invalidPan, "controlledCamera", invalidCamera);
            SetPrivateField(invalidPan, "inputLockController", invalidLock);
            SetPrivateField(invalidPan, "panCurve", customCurve);
            SetPrivateField(invalidPan, "panDuration", float.PositiveInfinity);
            var invalidCompletionCount = 0;
            var invalidCallbackCount = 0;
            invalidPan.PanCompleted += slot => invalidCompletionCount++;
            var invalidRoutine = (System.Collections.IEnumerator)InvokePrivateMethodWithResult(
                invalidPan, "PanRoutine", ShortCycleCameraSlot.Phase1, destination,
                new Action(() => invalidCallbackCount++));
            Assert(!invalidRoutine.MoveNext() && Vector3.Distance(invalidCamera.transform.position, destination) < 0.0001f,
                "An invalid duration must finish immediately instead of hanging or waiting to jump later.");
            Assert(invalidCompletionCount == 1 && invalidCallbackCount == 1 && !invalidLock.Gate.IsLocked,
                "Invalid-duration completion must still release the lock and notify exactly once.");

            var phase0Marker = new Transform();
            var phase1Marker = new Transform();
            var rightMarker = new Transform();
            phase0Marker.position = Vector3.zero;
            phase1Marker.position = new Vector3(-8f, 0f, 0f);
            rightMarker.position = new Vector3(8f, 0f, 0f);
            var cancelPan = new ShortCycleCameraPanController();
            SetPrivateField(cancelPan, "controlledCamera", new Camera());
            SetPrivateField(cancelPan, "phase0Marker", phase0Marker);
            SetPrivateField(cancelPan, "phase1Marker", phase1Marker);
            SetPrivateField(cancelPan, "rightReservedMarker", rightMarker);
            InvokePrivateMethod(cancelPan, "Awake");
            Assert(cancelPan.PanTo(ShortCycleCameraSlot.Phase1),
                "The first cross-space pan request must start.");
            var interruptedLock = new TraceDisposable();
            SetPrivateField(cancelPan, "activeInputLock", interruptedLock);
            Assert(cancelPan.PanTo(ShortCycleCameraSlot.RightReserved),
                "A rapid reverse request must replace the active pan.");
            Assert(cancelPan.StartedCoroutineCount == 2 && cancelPan.StoppedCoroutineCount == 1 &&
                interruptedLock.DisposeCount == 1 && cancelPan.CurrentSlot == ShortCycleCameraSlot.Phase0,
                "Replacing a pan must stop the old coroutine, release its lock once, and not land the old request.");
            var disableLock = new TraceDisposable();
            SetPrivateField(cancelPan, "activeInputLock", disableLock);
            InvokePrivateMethod(cancelPan, "OnDisable");
            Assert(cancelPan.StoppedCoroutineCount == 2 && disableLock.DisposeCount == 1,
                "Disabling the controller must stop the replacement pan and release its lock exactly once.");

            VerifyMidFlightReturnToRecordedSlot();
            VerifySymmetricAndRepeatedCameraPanReplacement();
            VerifyDisabledOffMarkerCameraPanRecovery();
            Time.unscaledDeltaTime = previousDelta;
        }

        private static void VerifyMidFlightReturnToRecordedSlot()
        {
            var camera = new Camera();
            var phase0Marker = new Transform();
            var phase1Marker = new Transform();
            phase0Marker.position = Vector3.zero;
            phase1Marker.position = new Vector3(-8f, 0f, 0f);
            camera.transform.position = phase0Marker.position;

            var inputLock = new ShortCycleInputLockController();
            var pan = CreateCameraPan(camera, phase0Marker, phase1Marker, inputLock);
            var startedCount = 0;
            var completedCount = 0;
            var outboundCallbackCount = 0;
            var returnCallbackCount = 0;
            pan.PanStarted += (from, to) => startedCount++;
            pan.PanCompleted += slot => completedCount++;

            Assert(pan.PanTo(ShortCycleCameraSlot.Phase1, () => outboundCallbackCount++),
                "The public API must accept the outbound P0-to-P1 request.");
            Assert(pan.AdvanceCoroutinesOnce() && camera.transform.position.x < 0f && camera.transform.position.x > -8f,
                "The public API regression must advance the outbound pan to a real intermediate position.");
            var interruptedPosition = camera.transform.position;
            Assert(inputLock.Gate.IsLocked && pan.CurrentSlot == ShortCycleCameraSlot.Phase0,
                "The outbound pan must retain the P0 completion slot and its input lock while in flight.");

            Assert(pan.PanTo(ShortCycleCameraSlot.Phase0, () => returnCallbackCount++),
                "The public API must accept a mid-flight request back to the recorded slot.");
            Assert(pan.StoppedCoroutineCount == 1 && pan.StartedCoroutineCount == 2 &&
                outboundCallbackCount == 0 && returnCallbackCount == 0 && pan.ActiveCoroutineCount == 1,
                "Returning mid-flight must cancel the old request and start one replacement without a synchronous false completion.");
            Assert(pan.AdvanceCoroutinesOnce() && camera.transform.position.x > interruptedPosition.x &&
                camera.transform.position.x < phase0Marker.position.x && inputLock.Gate.IsLocked,
                "The replacement must reverse from the interrupted position and hold the new input lock.");
            AdvanceCoroutinesToCompletion(pan);
            Assert(Vector3.Distance(camera.transform.position, phase0Marker.position) < 0.0001f &&
                pan.CurrentSlot == ShortCycleCameraSlot.Phase0,
                "The replacement must complete at P0 instead of allowing the cancelled P1 request to arrive late.");
            Assert(startedCount == 2 && completedCount == 1 && outboundCallbackCount == 0 &&
                returnCallbackCount == 1 && !inputLock.Gate.IsLocked && pan.ActiveCoroutineCount == 0,
                "Only the replacement may complete once; the cancelled request must have no callback, event, or lock leak.");

            var sameSlotCallbackCount = 0;
            Assert(pan.PanTo(ShortCycleCameraSlot.Phase0, () => sameSlotCallbackCount++) &&
                sameSlotCallbackCount == 1 && pan.StartedCoroutineCount == 2 && pan.ActiveCoroutineCount == 0,
                "A completed camera already at the requested marker must retain synchronous same-slot completion.");
        }

        private static void VerifySymmetricAndRepeatedCameraPanReplacement()
        {
            var phase0Marker = new Transform();
            var phase1Marker = new Transform();
            phase0Marker.position = Vector3.zero;
            phase1Marker.position = new Vector3(-8f, 0f, 0f);

            var reverseCamera = new Camera();
            reverseCamera.transform.position = phase1Marker.position;
            var reverseLock = new ShortCycleInputLockController();
            var reversePan = CreateCameraPan(reverseCamera, phase0Marker, phase1Marker, reverseLock);
            var abandonedCallbackCount = 0;
            var replacementCallbackCount = 0;
            Assert(reversePan.PanTo(ShortCycleCameraSlot.Phase0, () => abandonedCallbackCount++) &&
                reversePan.AdvanceCoroutinesOnce(),
                "The symmetric P1-to-P0 request must reach an intermediate frame.");
            var interruptedPosition = reverseCamera.transform.position;
            Assert(reversePan.PanTo(ShortCycleCameraSlot.Phase1, () => replacementCallbackCount++) &&
                reversePan.AdvanceCoroutinesOnce() && reverseCamera.transform.position.x < interruptedPosition.x,
                "The symmetric replacement must reverse from its current position toward P1.");
            AdvanceCoroutinesToCompletion(reversePan);
            Assert(Vector3.Distance(reverseCamera.transform.position, phase1Marker.position) < 0.0001f &&
                reversePan.CurrentSlot == ShortCycleCameraSlot.Phase1 && abandonedCallbackCount == 0 &&
                replacementCallbackCount == 1 && !reverseLock.Gate.IsLocked,
                "The symmetric replacement must suppress the old completion and finish exactly once at P1.");

            var repeatedCamera = new Camera();
            repeatedCamera.transform.position = phase0Marker.position;
            var repeatedLock = new ShortCycleInputLockController();
            var repeatedPan = CreateCameraPan(repeatedCamera, phase0Marker, phase1Marker, repeatedLock);
            var firstCallbackCount = 0;
            var latestCallbackCount = 0;
            var completedCount = 0;
            repeatedPan.PanCompleted += slot => completedCount++;
            Assert(repeatedPan.PanTo(ShortCycleCameraSlot.Phase1, () => firstCallbackCount++) &&
                repeatedPan.AdvanceCoroutinesOnce(),
                "The repeated-request regression must first reach an intermediate position.");
            Assert(repeatedPan.PanTo(ShortCycleCameraSlot.Phase1, () => latestCallbackCount++) &&
                repeatedPan.StoppedCoroutineCount == 1 && repeatedPan.ActiveCoroutineCount == 1,
                "Repeating the active target must replace the old request instead of running two pans.");
            AdvanceCoroutinesToCompletion(repeatedPan);
            Assert(firstCallbackCount == 0 && latestCallbackCount == 1 && completedCount == 1 &&
                repeatedPan.CurrentSlot == ShortCycleCameraSlot.Phase1 && !repeatedLock.Gate.IsLocked,
                "Only the latest repeated request may complete, and it must release the lock once finished.");
        }

        private static void VerifyDisabledOffMarkerCameraPanRecovery()
        {
            var camera = new Camera();
            var phase0Marker = new Transform();
            var phase1Marker = new Transform();
            phase0Marker.position = Vector3.zero;
            phase1Marker.position = new Vector3(-8f, 0f, 0f);
            camera.transform.position = phase0Marker.position;

            var inputLock = new ShortCycleInputLockController();
            var pan = CreateCameraPan(camera, phase0Marker, phase1Marker, inputLock);
            var abandonedCallbackCount = 0;
            Assert(pan.PanTo(ShortCycleCameraSlot.Phase1, () => abandonedCallbackCount++) &&
                pan.AdvanceCoroutinesOnce(),
                "The disable regression must first reach an intermediate position.");
            var disabledPosition = camera.transform.position;
            InvokePrivateMethod(pan, "OnDisable");
            Assert(pan.ActiveCoroutineCount == 0 && pan.StoppedCoroutineCount == 1 &&
                pan.CurrentSlot == ShortCycleCameraSlot.Phase0 && abandonedCallbackCount == 0 &&
                !inputLock.Gate.IsLocked,
                "Disabling mid-flight must cancel without a late completion and release the old lock.");
            Assert(!pan.AdvanceCoroutinesOnce() && Vector3.Distance(camera.transform.position, disabledPosition) < 0.0001f,
                "A disabled old coroutine must not resume or write a late position.");

            var recoveryCallbackCount = 0;
            Assert(pan.PanTo(ShortCycleCameraSlot.Phase0, () => recoveryCallbackCount++) &&
                recoveryCallbackCount == 0 && pan.ActiveCoroutineCount == 1,
                "An off-marker camera must not synchronously complete only because CurrentSlot still names that slot.");
            AdvanceCoroutinesToCompletion(pan);
            Assert(Vector3.Distance(camera.transform.position, phase0Marker.position) < 0.0001f &&
                recoveryCallbackCount == 1 && pan.CurrentSlot == ShortCycleCameraSlot.Phase0 &&
                !inputLock.Gate.IsLocked,
                "An off-marker same-slot recovery must actually reach the marker, callback once, and release its lock.");
        }

        private static ShortCycleCameraPanController CreateCameraPan(
            Camera camera,
            Transform phase0Marker,
            Transform phase1Marker,
            ShortCycleInputLockController inputLock)
        {
            var pan = new ShortCycleCameraPanController();
            SetPrivateField(pan, "controlledCamera", camera);
            SetPrivateField(pan, "phase0Marker", phase0Marker);
            SetPrivateField(pan, "phase1Marker", phase1Marker);
            SetPrivateField(pan, "inputLockController", inputLock);
            SetPrivateField(pan, "panCurve", AnimationCurve.Linear(0f, 0f, 1f, 1f));
            SetPrivateField(pan, "panDuration", 1f);
            InvokePrivateMethod(pan, "Awake");
            return pan;
        }

        private static void AdvanceCoroutinesToCompletion(MonoBehaviour behaviour)
        {
            for (var step = 0; step < 32 && behaviour.ActiveCoroutineCount > 0; step++)
            {
                behaviour.AdvanceCoroutinesOnce();
            }

            Assert(behaviour.ActiveCoroutineCount == 0,
                "The deterministic coroutine regression exceeded its bounded completion budget.");
        }

        private static void VerifyManualDriverKeyMap()
        {
            var bindings = ShortCycleManualVerificationKeyMap.Bindings;
            Assert(bindings.Count == 14,
                "The G24 keyboard map must keep the 13 baseline actions plus the routed T toggle.");
            var keys = new HashSet<UnityEngine.InputSystem.Key>();
            var actions = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < bindings.Count; index++)
            {
                Assert(keys.Add(bindings[index].Key), "G24 keyboard keys must be unique.");
                Assert(actions.Add(bindings[index].ActionName), "G24 keyboard actions must be unique.");
                string resolved;
                Assert(ShortCycleManualVerificationKeyMap.TryGetAction(bindings[index].Key, out resolved) &&
                    resolved == bindings[index].ActionName,
                    "G24 keyboard lookup must preserve the binding table.");
            }
            Assert(actions.Contains(ShortCycleActionNames.TrayToggleDetail),
                "T must route through the named tray-toggle action.");
            Assert(!keys.Contains(UnityEngine.InputSystem.Key.H),
                "H must stay a debug-only direct highlight probe, outside the production action map.");

            var expected = new Dictionary<UnityEngine.InputSystem.Key, string>
            {
                { UnityEngine.InputSystem.Key.Digit1, ShortCycleActionNames.OpenCover },
                { UnityEngine.InputSystem.Key.Digit2, ShortCycleActionNames.OpenCatalog },
                { UnityEngine.InputSystem.Key.Digit3, ShortCycleActionNames.CloseCatalog },
                { UnityEngine.InputSystem.Key.Digit4, ShortCycleActionNames.SelectRecipe },
                { UnityEngine.InputSystem.Key.Digit5, ShortCycleActionNames.OpenClueBoard },
                { UnityEngine.InputSystem.Key.Digit6, ShortCycleActionNames.CloseClueBoard },
                { UnityEngine.InputSystem.Key.Digit7, ShortCycleActionNames.OpenStepDetail },
                { UnityEngine.InputSystem.Key.Digit8, ShortCycleActionNames.CloseStepDetail },
                { UnityEngine.InputSystem.Key.F, ShortCycleActionNames.ToggleFavorite },
                { UnityEngine.InputSystem.Key.C, ShortCycleActionNames.StartCooking },
                { UnityEngine.InputSystem.Key.R, ShortCycleActionNames.Return },
                { UnityEngine.InputSystem.Key.P, ShortCycleActionNames.AdvanceToPhase2 },
                { UnityEngine.InputSystem.Key.T, ShortCycleActionNames.TrayToggleDetail },
                { UnityEngine.InputSystem.Key.Escape, ShortCycleActionNames.Escape }
            };
            foreach (var pair in expected)
            {
                string resolved;
                Assert(ShortCycleManualVerificationKeyMap.TryGetAction(pair.Key, out resolved) &&
                    resolved == pair.Value,
                    "The manual driver mapping changed for " + pair.Key + ".");
            }

            var staticFields = typeof(ShortCycleManualVerificationDriver).GetFields(
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            for (var index = 0; index < staticFields.Length; index++)
            {
                Assert(staticFields[index].FieldType != typeof(ShortCycleManualVerificationDriver),
                    "The manual verification driver must not expose a static singleton.");
            }

            InputManager.OnKeyPressed = null;
            var driver = new ShortCycleManualVerificationDriver();
            Assert(InputManager.OnKeyPressed == null,
                "An inactive manual driver must not consume keyboard input.");

            InvokePrivateMethod(driver, "OnEnable");
            InvokePrivateMethod(driver, "OnEnable");
            Assert(InputManager.OnKeyPressed != null &&
                InputManager.OnKeyPressed.GetInvocationList().Length == 1,
                "Repeated enable callbacks must leave exactly one keyboard subscription.");

            var dispatchSequence = typeof(ShortCycleManualVerificationDriver).GetField(
                "dispatchSequence",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert(dispatchSequence != null,
                "The manual driver dispatch sequence must remain observable to editor-external tests.");
            Assert(InputManager.OnKeyPressed(UnityEngine.InputSystem.Key.Digit1),
                "Digit1 must be consumed by the enabled manual driver.");
            Assert((int)dispatchSequence.GetValue(driver) == 1,
                "One keyboard event must route exactly once.");
            Assert(InputManager.OnKeyPressed(UnityEngine.InputSystem.Key.H),
                "H must remain an available debug-only highlight key.");
            Assert((int)dispatchSequence.GetValue(driver) == 1,
                "The H debug-only highlight must not dispatch a business action.");

            InvokePrivateMethod(driver, "OnDisable");
            Assert(InputManager.OnKeyPressed == null,
                "A disabled manual driver must release its keyboard subscription.");
            InvokePrivateMethod(driver, "OnEnable");
            InvokePrivateMethod(driver, "OnDestroy");
            Assert(InputManager.OnKeyPressed == null,
                "A destroyed manual driver must release its keyboard subscription.");
        }

        private static void VerifyGridPieceContracts()
        {
            var pieceTypes = new[]
            {
                typeof(FridgeSlot),
                typeof(TraySlot),
                typeof(CatalogCard),
                typeof(IngredientCard),
                typeof(SlotBlock),
                typeof(Sticker)
            };
            foreach (var pieceType in pieceTypes)
            {
                Assert(typeof(ShortCycleGridPieceBase).IsAssignableFrom(pieceType),
                    pieceType.Name + " must consume the common A5-2 world-space prefab contract.");
                Assert(typeof(IShortCycleStateGatedInteraction).IsAssignableFrom(pieceType),
                    pieceType.Name + " must expose the FSM interaction gate.");
            }
        }

        private static void VerifyV22AnchorAndScaleContracts()
        {
            Assert(ShortCycleAnchorContract.TrayAnchorNames.Count == 20, "Tray must expose exactly 20 anchors.");
            Assert(ShortCycleAnchorContract.GetTrayAnchorName(0) == "TraySlot_01_TUNE" &&
                ShortCycleAnchorContract.GetTrayAnchorName(19) == "TraySlot_20_TUNE",
                "Tray anchor names and order must be stable.");
            Assert(ShortCycleAnchorContract.SubstituteAnchorNames.Count == 8 &&
                ShortCycleAnchorContract.GetSubstituteAnchorName(0) == "SubSlot_01_TUNE" &&
                ShortCycleAnchorContract.GetSubstituteAnchorName(7) == "SubSlot_08_TUNE",
                "Substitute anchor names and order must be stable.");

            var compacted = ShortCycleAnchorContract.Compact(
                new[] { "first", null, "third", "fourth" },
                ShortCycleAnchorContract.TrayAnchorCount);
            Assert(compacted.Count == 3 && compacted[0] == "first" && compacted[1] == "third",
                "Null/returned entries must compact without leaving a visual hole.");

            var anchors = new UnityEngine.Transform[ShortCycleAnchorContract.TrayAnchorCount];
            for (var index = 0; index < anchors.Length; index++)
            {
                anchors[index] = new UnityEngine.Transform { name = ShortCycleAnchorContract.GetTrayAnchorName(index) };
            }
            var first = new UnityEngine.Transform { name = "first" };
            var second = new UnityEngine.Transform { name = "second" };
            var third = new UnityEngine.Transform { name = "third" };
            var layout = new ShortCycleTrayAnchorLayout();
            SetPrivateField(layout, "traySlotAnchors", anchors);
            string diagnostic;
            Assert(layout.TrySetItemViews(new[] { first, second, third }, out diagnostic), diagnostic);
            Assert(first.parent == anchors[0] && second.parent == anchors[1] && third.parent == anchors[2],
                "Tray fill must follow serialized anchor order.");
            Assert(layout.RemoveItemView(second) && third.parent == anchors[1],
                "Tray return/removal must tightly reflow subsequent items by anchor order.");

            Assert(ShortCycleIconScaleContract.GetScale(ShortCycleIconScaleRole.Fridge) == 0.62f &&
                ShortCycleIconScaleContract.GetScale(ShortCycleIconScaleRole.Tray) == 0.62f &&
                ShortCycleIconScaleContract.GetScale(ShortCycleIconScaleRole.IngredientList) == 0.27f &&
                ShortCycleIconScaleContract.GetScale(ShortCycleIconScaleRole.ClueCenter) == 0.60f &&
                ShortCycleIconScaleContract.GetScale(ShortCycleIconScaleRole.ClueCard) == 0.19f &&
                ShortCycleIconScaleContract.GetScale(ShortCycleIconScaleRole.CatalogDish) == 0.65f &&
                ShortCycleIconScaleContract.GetScale(ShortCycleIconScaleRole.StepDetail) == 0.25f,
                "The seven 512-canvas scale constants must match guide section 3.");
            Assert(ShortCycleIconScaleContract.GetPolicy(ShortCycleIconScaleRole.ClueCard) ==
                ShortCycleVisualScalePolicy.Canvas512Uniform,
                "Icon scaling must not normalize per-sprite alpha-visible bounds.");
        }

        private static void VerifyV22SlotBindingAndSubCardState()
        {
            var slot = new CK01RecipeSlotData
            {
                standard_ingredient = "ing_standard",
                required_count = 2,
                sub_01 = "ing_sub_a",
                sub_01_count = 1,
                sub_02 = "ing_sub_b",
                sub_02_count = 3
            };
            var requirements = ShortCycleRecipeSlotBinding.ReadRequirements(slot);
            Assert(requirements.Count == 2 && requirements[0].AnchorIndex == 0 &&
                requirements[0].ItemId == "ing_sub_a" && requirements[1].AnchorIndex == 1 &&
                requirements[1].ItemId == "ing_sub_b",
                "T2 sub_01..08 must bind in fixed field/anchor order and omit empty trailing fields.");

            var standardIcon = new UnityEngine.Sprite { name = "standard" };
            var subAIcon = new UnityEngine.Sprite { name = "sub-a" };
            var subBIcon = new UnityEngine.Sprite { name = "sub-b" };
            var vegetableTagIcon = new UnityEngine.Sprite { name = "tag-vegetable" };
            var items = new Dictionary<string, CK01ItemData>
            {
                { "ing_standard", new CK01ItemData { icon_sprite = standardIcon, ingredient_categories = new List<string> { "tag_standard" } } },
                { "ing_sub_a", new CK01ItemData { icon_sprite = subAIcon, ingredient_categories = new List<string> { "tag_vegetable", "tag_second" } } },
                { "ing_sub_b", new CK01ItemData { icon_sprite = subBIcon, ingredient_categories = new List<string> { "tag_vegetable" } } }
            };
            var tags = new Dictionary<string, CK01TagData>
            {
                { "tag_standard", new CK01TagData() },
                { "tag_vegetable", new CK01TagData { icon_sprite = vegetableTagIcon } }
            };
            var held = new Dictionary<string, int>
            {
                { "ing_standard", 2 },
                { "ing_sub_a", 1 },
                { "ing_sub_b", 1 }
            };
            ShortCycleSlotBlockBinding binding;
            string diagnostic;
            Assert(ShortCycleRecipeSlotBinding.TryResolve(
                slot,
                id => items.ContainsKey(id) ? items[id] : null,
                id => tags.ContainsKey(id) ? tags[id] : null,
                id => held.ContainsKey(id) ? held[id] : 0,
                id => id == "ing_sub_a",
                null,
                out binding,
                out diagnostic), diagnostic);
            Assert(binding.Substitutes.Count == 2 && binding.Substitutes[0].IsSatisfied &&
                !binding.Substitutes[1].IsSatisfied,
                "Each Link state must depend only on its corresponding ingredient count.");
            Assert(binding.Substitutes[1].PrimaryCategoryTagId == "tag_vegetable" &&
                ReferenceEquals(binding.Substitutes[1].DisplayIcon, vegetableTagIcon),
                "Locked SubCard must use the first T4 ingredient category Tag icon.");

            var root = new UnityEngine.GameObject();
            var iconRenderer = new UnityEngine.SpriteRenderer();
            var countLabel = new TMPro.TextMeshPro();
            var node = new UnityEngine.GameObject();
            var link = new UnityEngine.SpriteRenderer();
            var unlockedMaterial = new UnityEngine.Material { name = "unlocked" };
            var silhouetteMaterial = new UnityEngine.Material { name = "silhouette" };
            var subCard = new ShortCycleSubCard();
            SetPrivateField(subCard, "cardRoot", root);
            SetPrivateField(subCard, "iconRenderer", iconRenderer);
            SetPrivateField(subCard, "countLabel", countLabel);
            SetPrivateField(subCard, "node", node);
            SetPrivateField(subCard, "link", link);
            SetPrivateField(subCard, "unlockedMaterial", unlockedMaterial);
            SetPrivateField(subCard, "lockedSilhouetteMaterial", silhouetteMaterial);
            subCard.Configure(binding.Substitutes[1]);
            Assert(root.activeSelf && node.activeSelf && link.enabled,
                "Configured SubCard must expose card, Node and its own Link.");
            Assert(ReferenceEquals(iconRenderer.sprite, vegetableTagIcon) &&
                ReferenceEquals(iconRenderer.sharedMaterial, silhouetteMaterial),
                "Locked SubCard must apply the primary Tag icon and silhouette material interface.");
            Assert(countLabel.text == "1/3" && ColorsMatch(link.color, ShortCycleSubCard.UnsatisfiedLinkColor),
                "Unsatisfied Link must be red and display held/required count.");
            subCard.Clear();
            Assert(!root.activeSelf && !node.activeSelf && !link.enabled,
                "Empty SubSlot must hide card, Node and Link.");
        }

        private static void VerifyV22OutlineProfiles()
        {
            Assert(ShortCycleOutlineContract.All.Count == 10, "Exactly ten semantic outline profiles are required.");
            Assert(ShortCycleOutlineContract.Get(ShortCycleOutlineRole.FridgeSlots).PrimaryThickness == 13f, "FridgeSlots outline must start at 13.");
            Assert(ShortCycleOutlineContract.Get(ShortCycleOutlineRole.ClueCenter).PrimaryThickness == 10f, "ClueCenter outline must start at 10.");
            Assert(ShortCycleOutlineContract.Get(ShortCycleOutlineRole.ClueCards).PrimaryThickness == 11f, "ClueCards outline must start at 11.");
            Assert(ShortCycleOutlineContract.Get(ShortCycleOutlineRole.IngredientList).PrimaryThickness == 11f, "IngredientList outline must start at 11.");
            Assert(ShortCycleOutlineContract.Get(ShortCycleOutlineRole.DishPhoto).PrimaryThickness == 12f, "DishPhoto outline must start at 12.");
            var catalog = ShortCycleOutlineContract.Get(ShortCycleOutlineRole.CatalogCards);
            Assert(catalog.PrimaryThickness == 23f && catalog.SecondaryThickness == 12f, "CatalogCards must preserve frame/photo 23/12 values.");
            Assert(ShortCycleOutlineContract.Get(ShortCycleOutlineRole.TrayBase).PrimaryThickness == 12f, "TrayBase outline must start at 12.");
            Assert(ShortCycleOutlineContract.Get(ShortCycleOutlineRole.GoButton).PrimaryThickness == 32f, "GoButton must retain the reviewed upper-bound start value 32.");
            Assert(ShortCycleOutlineContract.Get(ShortCycleOutlineRole.ClueBoardBase).PrimaryThickness == 8f, "ClueBoardBase outline must start at 8.");
            Assert(ShortCycleOutlineContract.Get(ShortCycleOutlineRole.HoverHighlight).PrimaryThickness == 4f &&
                ShortCycleOutlineContract.GetHoverThickness(11f) == 15f,
                "HoverHighlight must add four source pixels without exceeding the current 32 limit.");
        }

        private static void VerifyV2OutlineControls()
        {
            Assert(!SpriteOutline2D.ResolveMergeMode(OutlineMergeOverride.FollowGroup, false) &&
                SpriteOutline2D.ResolveMergeMode(OutlineMergeOverride.FollowGroup, true),
                "FollowGroup must resolve only from its own group flag.");
            Assert(SpriteOutline2D.ResolveMergeMode(OutlineMergeOverride.ForceOn, false) &&
                SpriteOutline2D.ResolveMergeMode(OutlineMergeOverride.ForceOn, true),
                "ForceOn must override both group states.");
            Assert(!SpriteOutline2D.ResolveMergeMode(OutlineMergeOverride.ForceOff, false) &&
                !SpriteOutline2D.ResolveMergeMode(OutlineMergeOverride.ForceOff, true),
                "ForceOff must override both group states.");

            var root = new GameObject { name = "OutlineRoot" };
            var group = root.AddComponent<SpriteOutlineGroup2D>();
            var child = new GameObject { name = "FollowMember" };
            child.transform.SetParent(root.transform, false);
            child.AddComponent<SpriteRenderer>();
            var following = child.AddComponent<SpriteOutline2D>();
            following.SetOverrideGroup(true);
            following.SetMergeModes(OutlineMergeOverride.FollowGroup, OutlineMergeOverride.ForceOff);

            var localChild = new GameObject { name = "LocalMember" };
            localChild.transform.SetParent(root.transform, false);
            localChild.AddComponent<SpriteRenderer>();
            var local = localChild.AddComponent<SpriteOutline2D>();
            local.Configure(null, new Color(0.2f, 0.3f, 0.4f, 1f), 3f);

            group.Configure(null, new Color(0.9f, 0.1f, 0.2f, 1f), 12f);
            Assert(following.Thickness == 12f && ColorsMatch(following.OutlineColor, group.OutlineColor),
                "A follow member must copy manual group parameter changes.");
            Assert(following.OverrideGroup && following.MergeOutline == OutlineMergeOverride.FollowGroup &&
                following.MergeShadow == OutlineMergeOverride.ForceOff,
                "Group synchronization must not rewrite parameter-follow or merge tri-state choices.");
            Assert(local.Thickness == 3f && !group.SynchronizeMember(local),
                "A local member must retain its own parameters.");

            group.MergeOutlineEnabled = false;
            group.MergeShadowEnabled = true;
            following.SetMergeModes(OutlineMergeOverride.FollowGroup, OutlineMergeOverride.FollowGroup);
            Assert(!following.ResolveOutlineMergeEnabled() && following.ResolveShadowMergeEnabled(),
                "Outline and shadow merge decisions must remain independent.");

            var nested = new GameObject { name = "NestedGroup" };
            nested.transform.SetParent(root.transform, false);
            var nestedGroup = nested.AddComponent<SpriteOutlineGroup2D>();
            var nestedMemberObject = new GameObject { name = "NestedMember" };
            nestedMemberObject.transform.SetParent(nested.transform, false);
            nestedMemberObject.AddComponent<SpriteRenderer>();
            var nestedMember = nestedMemberObject.AddComponent<SpriteOutline2D>();
            nestedMember.SetOverrideGroup(true);
            Assert(nestedMember.NearestGroup == nestedGroup && nestedGroup.GetOwnedMembers().Length == 1 &&
                group.GetOwnedMembers().Length == 2,
                "Nested members must be owned by the nearest group only.");

            var missingMemberObject = new GameObject { name = "ExplicitCreationTarget" };
            missingMemberObject.transform.SetParent(root.transform, false);
            missingMemberObject.AddComponent<SpriteRenderer>();
            Assert(group.CreateMissingMembers() == 1 &&
                missingMemberObject.GetComponent<SpriteOutline2D>().OverrideGroup,
                "Explicit group creation must create one missing member and opt it into parameter following once.");

            var defaults = ScriptableObject.CreateInstance<SpriteOutlineDefaults>();
            defaults.ConfigureGeneric(null, new Color(0.1f, 0.2f, 0.3f, 1f), 7f);
            defaults.SetRole("Role.Special", null, new Color(0.4f, 0.5f, 0.6f, 1f), 9f);
            var defaultObject = new GameObject { name = "DefaultMember" };
            defaultObject.AddComponent<SpriteRenderer>();
            var defaultMember = defaultObject.AddComponent<SpriteOutline2D>();
            Assert(defaults.ApplyTo(defaultMember, "Role.Special") && defaultMember.Thickness == 9f,
                "Role defaults must be explicitly applicable with generic fallback support.");
            defaultMember.Thickness = 3f;
            InvokePrivateMethod(defaultMember, "OnEnable");
            InvokePrivateMethod(defaultMember, "OnValidate");
            Assert(defaultMember.Thickness == 3f,
                "Runtime lifecycle callbacks must never reapply the default asset.");

            var migrated = ScriptableObject.CreateInstance<SpriteOutlineDefaults>();
            ShortCycleOutlineContract.ExportToDefaults(migrated, null);
            SpriteOutlineStyle catalogSecondary;
            Assert(migrated.RoleDefaults.Count == 11 &&
                migrated.TryGetStyle("ShortCycle.CatalogCards.Secondary", out catalogSecondary) &&
                catalogSecondary.Thickness == 12f,
                "The legacy table must export once into named defaults, including the catalog secondary profile.");

            group.Configure(null, new Color(0.25f, 0.5f, 0.75f, 1f), 7f);
            var bindingObject = new GameObject { name = "HoverBinding" };
            var binding = bindingObject.AddComponent<ShortCycleOutlineGroupBinding>();
            SetPrivateField(binding, "primaryGroup", group);
            binding.ApplyHover(true);
            binding.ApplyHover(true);
            Assert(group.Thickness == 11f && ColorsMatch(group.OutlineColor, ShortCycleOutlineContract.HoverColor),
                "Repeated hover entry must be idempotent and derive from the captured value once.");
            binding.ApplyHover(false);
            binding.ApplyHover(false);
            Assert(group.Thickness == 7f && ColorsMatch(group.OutlineColor, new Color(0.25f, 0.5f, 0.75f, 1f)),
                "Hover exit must restore the exact captured pre-hover values.");
        }

        private static void VerifyFridgeCatRigContracts()
        {
            Assert(ShortCycleFridgeBinder.GetRequiredTierCount(0) == 0 &&
                ShortCycleFridgeBinder.GetRequiredTierCount(1) == 1 &&
                ShortCycleFridgeBinder.GetRequiredTierCount(5) == 1 &&
                ShortCycleFridgeBinder.GetRequiredTierCount(6) == 2 &&
                ShortCycleFridgeBinder.GetRequiredTierCount(30) == 6,
                "Fridge slot counts must map to five-column tiers with integer ceil semantics.");
            Assert(ShortCycleFridgeBinder.GetFilterTierCount(0) == 1 &&
                ShortCycleFridgeBinder.GetFilterTierCount(1) == 1 &&
                ShortCycleFridgeBinder.GetFilterTierCount(5) == 1 &&
                ShortCycleFridgeBinder.GetFilterTierCount(6) == 2 &&
                ShortCycleFridgeBinder.GetFilterTierCount(30) == 6,
                "Filter views must keep at least one tier and use the same five-column ceil rule.");

            var firstTier = CreateFridgeCatTier("row-1");
            var secondTier = CreateFridgeCatTier("row-2");
            var extensionPrefab = CreateFridgeCatTier("extension-prefab");
            var rig = new FridgeCatRig();
            SetPrivateField(rig, "fixedTiers", new[] { firstTier, secondTier });
            SetPrivateField(rig, "extensionTierPrefab", extensionPrefab);
            SetPrivateField(rig, "extensionTierRoot", new UnityEngine.Transform());
            SetPrivateField(rig, "tierSpacing_TUNE", 2f);

            rig.SetTierCount(2);
            Assert(rig.TierCount == 2 && rig.PooledExtensionTierCount == 0,
                "The initial ten-slot contract must use exactly two fixed rows and no extension allocation.");
            Assert(firstTier.gameObject.activeSelf && secondTier.gameObject.activeSelf,
                "Both fixed shenti2 rows must be active for the initial two-tier contract.");
            Assert(rig.GetShelfAnchor(0, 0) != null && rig.GetShelfAnchor(1, 4) != null &&
                rig.GetShelfAnchor(1, 5) == null,
                "Each active fridge-cat tier must expose exactly five zero-based shelf columns.");

            rig.SetTierCount(2);
            Assert(rig.PooledExtensionTierCount == 0,
                "Repeated SetTierCount(2) must not allocate an extension row.");
            rig.SetTierCount(3);
            Assert(rig.TierCount == 3 && rig.PooledExtensionTierCount == 1 &&
                rig.GetShelfAnchor(2, 4) != null,
                "SetTierCount(3) must create one reusable shenti extension with five anchors.");
            rig.SetTierCount(2);
            rig.SetTierCount(3);
            Assert(rig.PooledExtensionTierCount == 1,
                "Extension tier recycling must be idempotent and must not duplicate pooled rows.");

            rig.SetTierCount(6);
            Assert(rig.PooledExtensionTierCount == 4,
                "Six tiers must retain exactly four pooled extensions when the source rig has two fixed tiers.");
            rig.SetTierCount(2);
            rig.SetTierCount(6);
            Assert(rig.PooledExtensionTierCount == 4,
                "The 6->2->6 lifecycle must reuse all existing extension tiers without allocation.");
            rig.SetTierCount(8);
            Assert(rig.PooledExtensionTierCount == 6,
                "Expanding 6->8 may allocate only the two newly required extension tiers.");
            rig.SetTierCount(6);
            rig.SetTierCount(8);
            Assert(rig.PooledExtensionTierCount == 6,
                "The 6->8->6->8 lifecycle must not duplicate previously pooled extension tiers.");
        }

        private static void VerifyFridgeAnchorAlignedPagingContracts()
        {
            float normalized;
            Assert(FridgeCatTierPagingMath.TryCalculateNormalizedPosition(12f, 10f, -1f, out normalized) &&
                Math.Abs(normalized - (2f / 13f)) < .0001f,
                "Anchor-aligned paging must preserve an unequal first interval instead of using index/(count-1).");
            Assert(FridgeCatTierPagingMath.TryCalculateNormalizedPosition(12f, 3f, -1f, out normalized) &&
                Math.Abs(normalized - (9f / 13f)) < .0001f,
                "Anchor-aligned paging must derive every intermediate page from real anchor distance.");
            Assert(!FridgeCatTierPagingMath.TryCalculateNormalizedPosition(12f, 13f, -1f, out normalized) &&
                !FridgeCatTierPagingMath.TryCalculateNormalizedPosition(4f, 4f, 4f, out normalized),
                "Out-of-range and zero-span anchor layouts must fail rather than conceal a wiring error.");

            float contentHeight;
            float contentOffsetY;
            Assert(FridgeCatTierPagingMath.TryCalculateVerticalBoundsLayout(
                    10f, 2.5f, 4f, 2f, 7f, -5f, 11f, out contentHeight, out contentOffsetY) &&
                Math.Abs(contentHeight - 8.5f) < .0001f && Math.Abs(contentOffsetY + 2f) < .0001f,
                "Auto-bounds must include viewport plus real anchor travel and divide by non-unit content scale.");

            float sixTierHeight;
            float twoTierHeight;
            float restoredSixTierHeight;
            float eightTierHeight;
            Assert(FridgeCatTierPagingMath.TryCalculateVerticalBoundsLayout(
                    0f, 2.5f, 0f, 1.5f, 10f, -12f, 0f, out sixTierHeight, out contentOffsetY) &&
                FridgeCatTierPagingMath.TryCalculateVerticalBoundsLayout(
                    0f, 2.5f, 0f, 1.5f, 10f, 8f, 0f, out twoTierHeight, out contentOffsetY) &&
                FridgeCatTierPagingMath.TryCalculateVerticalBoundsLayout(
                    0f, 2.5f, 0f, 1.5f, 10f, -12f, 0f, out restoredSixTierHeight, out contentOffsetY) &&
                FridgeCatTierPagingMath.TryCalculateVerticalBoundsLayout(
                    0f, 2.5f, 0f, 1.5f, 10f, -25f, 0f, out eightTierHeight, out contentOffsetY) &&
                Math.Abs(sixTierHeight - 18f) < .0001f &&
                Math.Abs(twoTierHeight - (7f / 1.5f)) < .0001f &&
                Math.Abs(restoredSixTierHeight - sixTierHeight) < .0001f &&
                Math.Abs(eightTierHeight - (40f / 1.5f)) < .0001f,
                "6->2->6 and 6->8 layouts must deterministically refit from active anchors without cached counts.");

            Assert(FridgeCatTierPagingMath.TryCalculateVerticalBoundsLayout(
                    0f, 2.5f, 3f, 1f, 6f, 6f, 4f, out contentHeight, out contentOffsetY) &&
                Math.Abs(contentHeight - 5f) < .0001f,
                "A one-tier layout must collapse travel to the viewport height without division by zero.");

            var anchorWorldY = new[] { 12f, 10f, 7f, 3f, 0f, -1f };
            var fixedTiers = new FridgeCatTier[anchorWorldY.Length];
            for (var tierIndex = 0; tierIndex < fixedTiers.Length; tierIndex++)
            {
                fixedTiers[tierIndex] = CreateFridgeCatTier("anchor-row-" + tierIndex);
                for (var column = 0; column < FridgeCatTier.ShelfColumnCount; column++)
                {
                    fixedTiers[tierIndex].GetShelfAnchor(column).position =
                        new UnityEngine.Vector3(column, anchorWorldY[tierIndex], 0f);
                }
            }

            var contentCollider = new UnityEngine.BoxCollider2D
            {
                size = new UnityEngine.Vector2(14f, 10f),
                offset = new UnityEngine.Vector2(1f, -2f)
            };
            var viewport = new global::DragLimit
            {
                BoundsCenter = new UnityEngine.Vector2(0f, 10f),
                BoundsHalfHeight = 2.5f
            };
            var content = new global::DragContainer
            {
                sizeSource = global::DragContainer.SizeSource.BoxCollider2D,
                limitMode = global::DragContainer.LimitMode.DragLimit,
                dragLimit = viewport,
                col = contentCollider
            };
            content.transform.position = new UnityEngine.Vector3(0f, 4f, 0f);
            content.transform.localScale = new UnityEngine.Vector3(1f, 2f, 1f);
            var scrollArea = new global::ScrollArea_Controller
            {
                scrollMode = global::ScrollArea_Controller.ScrollMode.Vertical,
                contentDragContainer = content,
                viewportDragLimit = viewport
            };
            var target = new UnityEngine.Transform
            {
                position = new UnityEngine.Vector3(0f, 11f, 0f)
            };
            var rig = new FridgeCatRig();
            SetPrivateField(rig, "fixedTiers", fixedTiers);
            SetPrivateField(rig, "scrollArea", scrollArea);
            SetPrivateField(rig, "tierScrollMapping", FridgeCatTierScrollMappingMode.AnchorAlignedAutoBounds);
            SetPrivateField(rig, "tierAlignmentTarget_TUNE", target);
            SetPrivateField(rig, "tierAlignmentColumn_TUNE", 2);

            rig.SetTierCount(6);
            Assert(rig.TierCount == 6 && Math.Abs(contentCollider.size.x - 14f) < .0001f &&
                Math.Abs(contentCollider.offset.x - 1f) < .0001f &&
                Math.Abs(contentCollider.size.y - 9f) < .0001f &&
                Math.Abs(contentCollider.offset.y - .25f) < .0001f &&
                content.UpdateBoundsCount == 1,
                "Anchor-aligned SetTierCount must fit only the vertical collider axis from real anchors and non-unit scale.");
            rig.SetTierCount(6);
            Assert(content.UpdateBoundsCount == 1,
                "Repeating the same anchor-aligned layout must not rewrite or refresh unchanged bounds.");
            Assert(rig.ScrollToTier(1),
                "Anchor-aligned paging must accept an intermediate tier whose normalized target comes from real anchor spacing.");
        }

        private static void VerifyFridgeCatBlinkContracts()
        {
            TransitionController firstController;
            var firstEye = CreateFridgeCatEye(out firstController);
            Assert(firstEye.Blink() && firstController.StartedCoroutineCount == 1,
                "FridgeCatEye.Blink must start the TK-MOT-07 closed preset.");
            Assert(firstEye.Blink() && firstController.StartedCoroutineCount == 2 &&
                firstController.StoppedCoroutineCount == 1,
                "Repeated eye Blink must cancel the prior preset coroutine before starting another.");

            TransitionController leftController;
            TransitionController rightController;
            var leftEye = CreateFridgeCatEye(out leftController);
            var rightEye = CreateFridgeCatEye(out rightController);
            var scheduler = new global::FridgeCatBlinkScheduler();
            scheduler.gameObject.SetActive(true);
            SetPrivateField(scheduler, "leftEye", leftEye);
            SetPrivateField(scheduler, "rightEye", rightEye);
            scheduler.RequestBlink();
            Assert(leftController.StartedCoroutineCount == 1 && rightController.StartedCoroutineCount == 1,
                "One scheduler request must synchronously dispatch one Blink to each independent eye.");

            scheduler.StartBlinkScheduling();
            scheduler.StartBlinkScheduling();
            Assert(scheduler.StartedCoroutineCount == 1,
                "Repeated scheduler starts must retain exactly one random-interval coroutine.");
            scheduler.StopBlinkScheduling();
            Assert(scheduler.StoppedCoroutineCount == 1,
                "Stopping the scheduler must release its single owned coroutine.");

            SetPrivateField(scheduler, "minBlinkInterval", 5f);
            SetPrivateField(scheduler, "maxBlinkInterval", 3f);
            float minimum;
            float maximum;
            scheduler.GetBlinkIntervalRange(out minimum, out maximum);
            Assert(minimum == 3f && maximum == 5f,
                "Reversed blink bounds must normalize back to the reviewed 3-5 second range.");
            SetPrivateField(scheduler, "minBlinkInterval", float.NaN);
            SetPrivateField(scheduler, "maxBlinkInterval", -1f);
            scheduler.GetBlinkIntervalRange(out minimum, out maximum);
            Assert(minimum == 3f && maximum == 5f,
                "NaN and negative blink bounds must use the safe 3-5 second defaults.");
        }

        private static void VerifyDiscreteScrollContracts()
        {
            Assert(ScrollArea_Controller.CalculateDiscreteStepTarget(0f, -1f, 0.5f) == 0.5f &&
                ScrollArea_Controller.CalculateDiscreteStepTarget(0.5f, -1f, 0.5f) == 1f &&
                ScrollArea_Controller.CalculateDiscreteStepTarget(1f, -1f, 0.5f) == 1f &&
                ScrollArea_Controller.CalculateDiscreteStepTarget(0f, 1f, 0.5f) == 0f,
                "Discrete scroll stepping must clamp both normalized boundaries.");

            var legacyContent = new global::DragContainer
            {
                mode = global::DragContainer.DragMode.Vertical,
                LimitMinY = 0f,
                LimitMaxY = 10f
            };
            var legacyScroll = new global::ScrollArea_Controller
            {
                contentDragContainer = legacyContent,
                scrollMode = global::ScrollArea_Controller.ScrollMode.Vertical,
                stepMode = global::ScrollArea_Controller.ScrollStepMode.Discrete,
                stepSize = 0.5f
            };
            legacyScroll.OnScrollStep(-1f);
            Assert(legacyContent.transform.position.y == 5f,
                "A legacy scene without Transition wiring must retain functional ScrollTo behaviour.");
            legacyScroll.OnScrollStep(-1f);
            legacyScroll.OnScrollStep(-1f);
            Assert(legacyContent.transform.position.y == 10f,
                "Repeated discrete scroll input must stop at the upper content boundary.");

            var animatedContent = new global::DragContainer
            {
                mode = global::DragContainer.DragMode.Vertical,
                LimitMinY = 0f,
                LimitMaxY = 10f
            };
            var positionTransition = new global::TransitionBehaviour_Position();
            var animatedScroll = new global::ScrollArea_Controller
            {
                contentDragContainer = animatedContent,
                scrollMode = global::ScrollArea_Controller.ScrollMode.Vertical,
                onScrollPositionChanged = new UnityEngine.Events.UnityEvent<UnityEngine.Vector2>()
            };
            var animatedCallbackCount = 0;
            animatedScroll.onScrollPositionChanged.AddListener(value => animatedCallbackCount++);
            SetPrivateField(animatedScroll, "scrollPositionTransition", positionTransition);
            animatedScroll.ScrollVerticalTo(1f);
            Assert(animatedContent.transform.position.y == 0f && positionTransition.StartedCoroutineCount == 1,
                "Configured programmatic ScrollTo must start Transition PlayTo instead of jumping instantly.");
            animatedScroll.ScrollVerticalTo(0.5f);
            Assert(positionTransition.StartedCoroutineCount == 2 && positionTransition.StoppedCoroutineCount == 1 &&
                animatedCallbackCount == 0,
                "A repeated programmatic scroll must replace, not parallel, the active Transition coroutine.");
            positionTransition.gameObject.SetActive(false);
            InvokePrivateMethod(positionTransition, "OnDisable");
            Assert(animatedContent.transform.position.y == 5f && animatedCallbackCount == 1,
                "Disabling an active Transition host must land the latest target and notify exactly once.");
            InvokePrivateMethod(animatedScroll, "OnDisable");
            Assert(animatedCallbackCount == 1,
                "ScrollArea lifecycle cleanup must not duplicate a Transition cancellation notification.");
            positionTransition.gameObject.SetActive(true);
            animatedScroll.ScrollVerticalTo(1f);
            Assert(animatedContent.transform.position.y == 5f && positionTransition.StartedCoroutineCount == 3,
                "After re-entry, active programmatic scrolling must resume smoothly from the landed target.");

            var inactiveControllerContent = new global::DragContainer
            {
                mode = global::DragContainer.DragMode.Vertical,
                LimitMinY = 0f,
                LimitMaxY = 10f
            };
            var inactiveControllerTransition = new global::TransitionBehaviour_Position();
            var inactiveControllerScroll = new global::ScrollArea_Controller
            {
                contentDragContainer = inactiveControllerContent,
                scrollMode = global::ScrollArea_Controller.ScrollMode.Vertical,
                onScrollPositionChanged = new UnityEngine.Events.UnityEvent<UnityEngine.Vector2>()
            };
            var inactiveControllerCallbackCount = 0;
            inactiveControllerScroll.onScrollPositionChanged.AddListener(value => inactiveControllerCallbackCount++);
            SetPrivateField(inactiveControllerScroll, "scrollPositionTransition", inactiveControllerTransition);
            inactiveControllerScroll.gameObject.SetActive(false);
            inactiveControllerScroll.ScrollVerticalTo(1f);
            Assert(inactiveControllerTransition.StartedCoroutineCount == 0 &&
                inactiveControllerContent.transform.position.y == 10f &&
                inactiveControllerScroll.HasLastTargetPosition && inactiveControllerScroll.LastTargetPosition.y == 10f &&
                inactiveControllerCallbackCount == 1,
                "An inactive ScrollArea host must land the target synchronously without starting a coroutine.");

            var inactiveTransitionContent = new global::DragContainer
            {
                mode = global::DragContainer.DragMode.Vertical,
                LimitMinY = 0f,
                LimitMaxY = 10f
            };
            var inactiveTransition = new global::TransitionBehaviour_Position();
            inactiveTransition.gameObject.SetActive(false);
            var inactiveTransitionScroll = new global::ScrollArea_Controller
            {
                contentDragContainer = inactiveTransitionContent,
                scrollMode = global::ScrollArea_Controller.ScrollMode.Vertical,
                onScrollPositionChanged = new UnityEngine.Events.UnityEvent<UnityEngine.Vector2>()
            };
            var inactiveTransitionCallbackCount = 0;
            inactiveTransitionScroll.onScrollPositionChanged.AddListener(value => inactiveTransitionCallbackCount++);
            SetPrivateField(inactiveTransitionScroll, "scrollPositionTransition", inactiveTransition);
            inactiveTransitionScroll.ScrollVerticalTo(0.5f);
            Assert(inactiveTransition.StartedCoroutineCount == 0 &&
                inactiveTransitionContent.transform.position.y == 5f && inactiveTransitionCallbackCount == 1,
                "An inactive Transition host must use the same synchronous target and once-only notification fallback.");

            var initializationContent = new global::DragContainer
            {
                mode = global::DragContainer.DragMode.Vertical,
                LimitMinY = 0f,
                LimitMaxY = 10f,
                ApplyUpdatedBoundsOnUpdate = true,
                UpdatedLimitMinY = -2f,
                UpdatedLimitMaxY = 4f
            };
            var initializationTransition = new global::TransitionBehaviour_Position();
            var initializationScrollBar = new global::ScrollBar_Controller();
            var initializationScroll = new global::ScrollArea_Controller
            {
                contentDragContainer = initializationContent,
                viewportDragLimit = new global::DragLimit(),
                verticalScrollBar = initializationScrollBar,
                scrollMode = global::ScrollArea_Controller.ScrollMode.Vertical,
                onScrollPositionChanged = new UnityEngine.Events.UnityEvent<UnityEngine.Vector2>()
            };
            var initializationCallbackCount = 0;
            initializationScroll.onScrollPositionChanged.AddListener(value => initializationCallbackCount++);
            SetPrivateField(initializationScroll, "scrollPositionTransition", initializationTransition);
            initializationScroll.gameObject.SetActive(false);
            initializationScroll.ScrollVerticalTo(0.25f);
            initializationScroll.ScrollVerticalTo(0.75f);
            Assert(initializationContent.MoveToTargetCount == 0 &&
                initializationTransition.StartedCoroutineCount == 0 &&
                !initializationScroll.HasLastTargetPosition && initializationCallbackCount == 0,
                "Pre-initialize requests must retain only normalized intent without using stale bounds or starting a coroutine.");
            InvokePrivateMethod(initializationScroll, "Start");
            Assert(initializationContent.InitializeCount == 1 && initializationContent.UpdateBoundsCount == 1 &&
                initializationContent.MoveToTargetCount == 1 && initializationContent.transform.position.y == 2.5f &&
                initializationScroll.HasLastTargetPosition && initializationScroll.LastTargetPosition.y == 2.5f &&
                initializationScroll.NormalizedContentPosition.y == 0.75f && initializationScrollBar.Value == 0.75f &&
                initializationCallbackCount == 1,
                "Automatic Initialize must apply the latest deferred normalized target once against final runtime bounds.");
            initializationScroll.Initialize();
            Assert(initializationContent.InitializeCount == 2 && initializationContent.UpdateBoundsCount == 2 &&
                initializationContent.MoveToTargetCount == 1 && initializationCallbackCount == 1,
                "Repeated Initialize must not replay an expired pre-initialize request.");
            initializationScroll.gameObject.SetActive(true);
            initializationScroll.ScrollVerticalTo(1f);
            Assert(initializationTransition.StartedCoroutineCount == 1 &&
                initializationContent.transform.position.y == 2.5f,
                "After initialization and re-entry, later active requests must retain smooth Transition behaviour.");

            var noRequestContent = new global::DragContainer
            {
                LimitMinY = 0f,
                LimitMaxY = 10f,
                ApplyUpdatedBoundsOnUpdate = true,
                UpdatedLimitMinY = -3f,
                UpdatedLimitMaxY = 3f
            };
            noRequestContent.transform.position = new UnityEngine.Vector3(0f, 1f, 0f);
            var noRequestScroll = new global::ScrollArea_Controller
            {
                contentDragContainer = noRequestContent,
                viewportDragLimit = new global::DragLimit(),
                scrollMode = global::ScrollArea_Controller.ScrollMode.Vertical
            };
            noRequestScroll.Initialize();
            Assert(noRequestContent.transform.position.y == 1f && noRequestContent.MoveToTargetCount == 0 &&
                !noRequestScroll.HasLastTargetPosition,
                "Initialize without a pending request must refresh bounds without moving content.");

            var manualContent = new global::DragContainer
            {
                LimitMinY = 0f,
                LimitMaxY = 10f,
                ApplyUpdatedBoundsOnUpdate = true,
                UpdatedLimitMinY = -4f,
                UpdatedLimitMaxY = 0f
            };
            var manualScroll = new global::ScrollArea_Controller
            {
                manualInitialize = true,
                contentDragContainer = manualContent,
                viewportDragLimit = new global::DragLimit(),
                scrollMode = global::ScrollArea_Controller.ScrollMode.Vertical
            };
            manualScroll.ScrollVerticalTo(0.5f);
            InvokePrivateMethod(manualScroll, "Start");
            Assert(manualContent.InitializeCount == 0 && manualContent.MoveToTargetCount == 0,
                "manualInitialize must keep a deferred request dormant until explicit initialization.");
            manualScroll.Initialize();
            Assert(manualContent.transform.position.y == -2f && manualContent.MoveToTargetCount == 1 &&
                manualScroll.LastTargetPosition.y == -2f,
                "Explicit manual Initialize must apply the deferred request once against final bounds.");

            Assert(typeof(global::ScrollArea_Controller).GetMethod("ScrollTo") != null &&
                typeof(global::ScrollArea_Controller).GetMethod("ScrollVerticalTo") != null &&
                typeof(global::ScrollArea_Controller).GetMethod("ScrollHorizontalTo") != null,
                "The three legacy ScrollArea public methods must remain source-compatible.");
        }

        private static void VerifyFridgeScrollBinderContracts()
        {
            var content = new global::DragContainer
            {
                mode = global::DragContainer.DragMode.Vertical,
                LimitMinY = 0f,
                LimitMaxY = 9f
            };
            var scrollArea = new global::ScrollArea_Controller
            {
                contentDragContainer = content,
                scrollMode = global::ScrollArea_Controller.ScrollMode.Vertical
            };
            var rig = new FridgeCatRig();
            SetPrivateField(rig, "activeTierCount", 4);
            SetPrivateField(rig, "scrollArea", scrollArea);
            var binder = new ShortCycleFridgeBinder();
            SetPrivateField(binder, "fridgeCatRig", rig);

            var rejected = binder.ScrollTier(-1);
            Assert(!rejected.Succeeded && rejected.ErrorCode == ShortCycleActionErrorCode.Rejected &&
                binder.CurrentTier == 0 && content.transform.position.y == 0f,
                "Fridge scroll must reject the lower boundary without moving content.");
            Assert(binder.ScrollTier(9).Succeeded && binder.CurrentTier == 1 &&
                Math.Abs(content.transform.position.y - 3f) < .0001f,
                "Any positive direction must normalize to one tier and use FridgeCatRig.ScrollToTier.");
            Assert(binder.ScrollTier(1).Succeeded && binder.CurrentTier == 2 &&
                Math.Abs(content.transform.position.y - 6f) < .0001f,
                "Repeated positive steps must advance exactly one tier.");
            Assert(binder.ScrollTier(-7).Succeeded && binder.CurrentTier == 1 &&
                Math.Abs(content.transform.position.y - 3f) < .0001f,
                "Any negative direction must normalize to one tier upward.");
            SetPrivateField(rig, "activeTierCount", 2);
            var positionBeforeRejection = content.transform.position.y;
            rejected = binder.ScrollTier(1);
            Assert(!rejected.Succeeded && rejected.ErrorCode == ShortCycleActionErrorCode.Rejected &&
                binder.CurrentTier == 1 && content.transform.position.y == positionBeforeRejection,
                "A reduced TierCount must reject the upper boundary without changing the last accepted tier or position.");
            rejected = binder.ScrollTier(0);
            Assert(!rejected.Succeeded && rejected.ErrorCode == ShortCycleActionErrorCode.Rejected,
                "A zero tier direction must be rejected rather than dispatched to the rig.");
        }

        private static void VerifyFridgeScrollAdapterGateAndSubscription()
        {
            var scrollable = new global::MouseScrollableObject
            {
                scrollStepEvent = new UnityEngine.Events.UnityEvent<float>()
            };
            var adapter = new ShortCycleScrollAdapter();
            var session = new ShortCycleSessionManager();
            SetPrivateField(adapter, "scrollableObject", scrollable);
            SetPrivateField(adapter, "sessionManager", session);

            InvokePrivateMethod(adapter, "OnEnable");
            InvokePrivateMethod(adapter, "OnEnable");
            Assert(scrollable.scrollStepEvent.ListenerCount == 1 && adapter.IsSubscribed,
                "A repeated OnEnable must preserve exactly one MouseInteract scroll subscription.");
            scrollable.scrollStepEvent.Invoke(3f);
            Assert(adapter.DispatchCount == 0 && adapter.LastResult == null &&
                adapter.ReceivedStepCount == 1 && adapter.LastReceivedDelta == 3f &&
                adapter.LastAttemptResult != null &&
                adapter.LastAttemptResult.ErrorCode == ShortCycleActionErrorCode.Locked,
                "Phase1 gating must stop MouseInteract scroll before routing.");

            var locked = adapter.Dispatch(-2f);
            Assert(!locked.Succeeded && locked.ErrorCode == ShortCycleActionErrorCode.Locked &&
                adapter.DispatchCount == 0,
                "A non-Phase1 scroll must be locked without incrementing routed dispatch count.");
            Assert(adapter.Dispatch(0f) == null && adapter.DispatchCount == 0,
                "A zero wheel delta must be ignored without routing.");

            var content = new global::DragContainer
            {
                mode = global::DragContainer.DragMode.Vertical,
                LimitMinY = 0f,
                LimitMaxY = 9f
            };
            var scrollArea = new global::ScrollArea_Controller
            {
                contentDragContainer = content,
                scrollMode = global::ScrollArea_Controller.ScrollMode.Vertical
            };
            var rig = new FridgeCatRig();
            SetPrivateField(rig, "activeTierCount", 4);
            SetPrivateField(rig, "scrollArea", scrollArea);
            var binder = new ShortCycleFridgeBinder();
            SetPrivateField(binder, "fridgeCatRig", rig);
            SetPrivateField(session, "fridgeBinder", binder);

            var trace = new List<string>();
            var saveBoundary = new TraceSaveBoundary(trace);
            var processor = new ShortCycleDataProcessor(
                new TraceInventory(trace), saveBoundary, new TraceHandoff(trace));
            var stateMachine = new ShortCycleStateMachine(
                new ShortCycleContext(), saveBoundary, processor, new TraceExit());
            Assert(stateMachine.TryEnter("rcp_scroll", ShortCycleTriggerContext.FreeTry).Succeeded &&
                stateMachine.TryShowPhase0View(ShortCyclePhase0View.RecipeCatalog).Succeeded &&
                stateMachine.TrySelectRecipe("rcp_scroll", false).Succeeded &&
                stateMachine.TryStartCooking().Succeeded,
                "The fridge scroll integration fixture must enter Phase1.");
            SetPrivateField(session, "stateMachine", stateMachine);

            var router = new ShortCycleActionRouter();
            router.Configure(session);
            SetPrivateField(adapter, "actionRouter", router);

            scrollable.scrollStepEvent.Invoke(8f);
            Assert(adapter.DispatchCount == 1 && adapter.LastResult.Succeeded &&
                adapter.ReceivedStepCount == 2 && adapter.LastAttemptResult.Succeeded &&
                binder.CurrentTier == 1 && Math.Abs(content.transform.position.y - 3f) < .0001f,
                "Positive MouseInteract input must normalize to +1 and traverse Adapter -> Router -> Manager -> Binder -> Rig.");
            scrollable.scrollStepEvent.Invoke(-5f);
            Assert(adapter.DispatchCount == 2 && adapter.LastResult.Succeeded &&
                adapter.ReceivedStepCount == 3 && adapter.LastAttemptResult.Succeeded &&
                adapter.LastMappedDelta == -1f &&
                binder.CurrentTier == 0 && Math.Abs(content.transform.position.y) < .0001f,
                "Negative MouseInteract input must normalize to -1 through the same named-action route.");

            SetPrivateField(adapter, "invertPhysicalScrollDirection", true);
            scrollable.scrollStepEvent.Invoke(-1f);
            Assert(adapter.DispatchCount == 3 && adapter.LastResult.Succeeded &&
                adapter.ReceivedStepCount == 4 && adapter.LastAttemptResult == adapter.LastResult &&
                adapter.LastMappedDelta == 1f && binder.CurrentTier == 1,
                "The explicit physical-direction opt-in must map a negative wheel step to business +1.");
            Assert(adapter.Dispatch(-1f).Succeeded && adapter.DispatchCount == 4 && binder.CurrentTier == 0,
                "The public Dispatch contract must remain +1 next/-1 previous when physical mapping is enabled.");
            SetPrivateField(adapter, "invertPhysicalScrollDirection", false);
            scrollable.scrollStepEvent.Invoke(-1f);
            Assert(adapter.DispatchCount == 5 && !adapter.LastResult.Succeeded &&
                adapter.ReceivedStepCount == 5 && adapter.LastAttemptResult == adapter.LastResult &&
                adapter.LastResult.ErrorCode == ShortCycleActionErrorCode.Rejected &&
                binder.CurrentTier == 0 && Math.Abs(content.transform.position.y) < .0001f,
                "The routed lower-boundary step must reject without changing tier state or position.");

            InvokePrivateMethod(adapter, "OnDisable");
            Assert(scrollable.scrollStepEvent.ListenerCount == 0 && !adapter.IsSubscribed,
                "OnDisable must remove the MouseInteract scroll subscription.");
            scrollable.scrollStepEvent.Invoke(1f);
            Assert(adapter.DispatchCount == 5 && !adapter.LastResult.Succeeded,
                "OnDisable must unsubscribe the adapter from MouseInteract scroll events.");
        }

        private static void VerifyFridgeScrollTraceClassifierAndLifecycle()
        {
            Func<ShortCycleScrollTraceFacts> ready = () => new ShortCycleScrollTraceFacts
            {
                TraceEnabled = true,
                HasMouse = true,
                IsFocused = true,
                HasMouseManager = true,
                MouseManagerActive = true,
                FrameworkLayerCount = 1,
                SelectedMaskIndex = 0,
                SelectedMaskHitCount = 1,
                ScrollRegionCandidate = true,
                FrameworkTargetMatches = true,
                TierCount = 3,
                CurrentTier = 1,
                Threshold = 5f,
                HasAdapter = true,
                AdapterActive = true,
                AdapterWiredToTarget = true,
                AdapterSubscribed = true,
                Phase = ShortCyclePhase.Phase1
            };

            var facts = ready();
            facts.TraceEnabled = false;
            Assert(ShortCycleScrollTraceClassifier.Evaluate(facts).Reason == "trace_disabled",
                "The fridge trace must be inert until manually enabled.");
            facts = ready(); facts.HasMouse = false;
            Assert(ShortCycleScrollTraceClassifier.Evaluate(facts).Reason == "mouse_missing",
                "A missing real mouse must stop at the device layer.");
            facts = ready(); facts.FrameworkLayerCount = 1; facts.UnsafeEmptyLayerUpdatePath = true;
            Assert(ShortCycleScrollTraceClassifier.Evaluate(facts).Reason == "empty_layer_stack_update_risk",
                "A known unsafe empty-stack implementation must remain distinguishable in trace facts.");
            facts = ready(); facts.FrameworkLayerCount = 0;
            Assert(ShortCycleScrollTraceClassifier.Evaluate(facts).Reason == "interaction_layer_stack_empty",
                "A safely handled empty framework stack must report no active interaction layer.");
            facts = ready(); facts.SelectedMaskIndex = -1; facts.SelectedMaskHitCount = 0;
            Assert(ShortCycleScrollTraceClassifier.Evaluate(facts).Reason == "no_collider_in_allowed_masks",
                "No target under the real pointer must be distinguished from input loss.");
            facts = ready(); facts.BlockedByPressable = true;
            Assert(ShortCycleScrollTraceClassifier.Evaluate(facts).Reason == "higher_priority_pressable",
                "A winning Pressable must be reported at the blocking layer.");
            facts = ready(); facts.BlockedByPressable = true; facts.PressablePassThroughApplied = true;
            facts.RawScroll = 6f; facts.MouseStepCount = 1; facts.TargetEventCount = 1;
            facts.AdapterReceivedCount = 1; facts.AdapterDispatchCount = 1;
            facts.LastAttemptResult = ShortCycleActionResult.Success(); facts.LastDispatchResult = facts.LastAttemptResult;
            Assert(ShortCycleScrollTraceClassifier.Evaluate(facts).Reason == "routed_success_observe_transition",
                "An explicitly scoped pass-through must retain the same target-to-business route.");
            facts = ready(); facts.RawScroll = 1f; facts.AccumulatedScroll = 1f;
            Assert(ShortCycleScrollTraceClassifier.Evaluate(facts).Reason == "below_scroll_threshold",
                "Raw input below the configured accumulator threshold must remain an event-layer observation.");
            facts = ready(); facts.RawScroll = 6f; facts.AccumulatedScroll = 6f;
            Assert(ShortCycleScrollTraceClassifier.Evaluate(facts).Reason == "step_not_emitted",
                "Raw input reaching the threshold without a step must be explicit.");
            facts = ready(); facts.RawScroll = 6f; facts.MouseStepCount = 1; facts.TargetEventCount = 1;
            facts.AdapterReceivedCount = 0;
            Assert(ShortCycleScrollTraceClassifier.Evaluate(facts).Reason == "adapter_did_not_receive_step",
                "A target event that never reaches the adapter must not be called a device failure.");
            facts = ready(); facts.RawScroll = 6f; facts.MouseStepCount = 1; facts.TargetEventCount = 1;
            facts.AdapterReceivedCount = 1; facts.Phase = ShortCyclePhase.Phase0;
            Assert(ShortCycleScrollTraceClassifier.Evaluate(facts).Reason == "phase1_required",
                "Phase0 must be reported as a gate rather than a missing listener.");
            facts = ready(); facts.RawScroll = -6f; facts.MouseStepCount = 1; facts.TargetEventCount = 1;
            facts.AdapterReceivedCount = 1; facts.AdapterDispatchCount = 1;
            facts.LastAttemptResult = ShortCycleActionResult.Failure("Fridge tier -1 is outside the active range.");
            facts.LastDispatchResult = facts.LastAttemptResult;
            Assert(ShortCycleScrollTraceClassifier.Evaluate(facts).BlockedAt == "boundary",
                "A routed boundary rejection must be separated from adapter wiring failures.");
            facts = ready(); facts.RawScroll = 6f; facts.MouseStepCount = 1; facts.TargetEventCount = 1;
            facts.AdapterReceivedCount = 1; facts.AdapterDispatchCount = 1;
            facts.LastAttemptResult = ShortCycleActionResult.Success();
            facts.LastDispatchResult = facts.LastAttemptResult;
            Assert(ShortCycleScrollTraceClassifier.Evaluate(facts).Reason == "routed_success_observe_transition",
                "The normal real-input observation path must reach presentation without adding a parallel route.");

            var scrollable = new global::MouseScrollableObject
            {
                scrollStepEvent = new UnityEngine.Events.UnityEvent<float>()
            };
            var probe = new ShortCycleFridgeScrollTraceProbe();
            SetPrivateField(probe, "scrollRegion", scrollable);
            UnityEngine.Debug.Messages.Clear();
            InvokePrivateMethod(probe, "OnEnable");
            Assert(!probe.TraceEnabled && scrollable.scrollStepEvent.ListenerCount == 0 &&
                UnityEngine.Debug.Messages.Count == 0,
                "The default-disabled probe must attach no listener, log nothing, and cause no business effect.");
            probe.SetTraceEnabled(true);
            probe.SetTraceEnabled(true);
            Assert(scrollable.scrollStepEvent.ListenerCount == 1,
                "Repeated trace enable must keep exactly one target observation listener.");
            global::MouseManager.RaiseScrollStep(1f);
            scrollable.scrollStepEvent.Invoke(1f);
            Assert(probe.MouseStepCount == 1 && probe.TargetEventCount == 1,
                "The passive listeners must count existing events without dispatching an action.");
            probe.SetTraceEnabled(false);
            Assert(scrollable.scrollStepEvent.ListenerCount == 0,
                "Trace disable must remove the target listener.");
            global::MouseManager.RaiseScrollStep(1f);
            scrollable.scrollStepEvent.Invoke(1f);
            Assert(probe.MouseStepCount == 1 && probe.TargetEventCount == 1,
                "Disabled and destroyed trace lifecycles must remain observation-free.");
        }

        private static void VerifyFridgeScrollLabTraceSessionsAndSimulationBoundary()
        {
            var scrollable = new global::MouseScrollableObject
            {
                scrollStepEvent = new UnityEngine.Events.UnityEvent<float>()
            };
            var adapter = new ShortCycleScrollAdapter();
            SetPrivateField(adapter, "scrollableObject", scrollable);
            var probe = new ShortCycleFridgeScrollTraceProbe();
            SetPrivateField(probe, "scrollRegion", scrollable);
            SetPrivateField(probe, "scrollAdapter", adapter);
            probe.BeginCaptureSession(ShortCycleScrollTraceMode.RealInput);
            probe.SetTraceEnabled(true);

            UnityEngine.InputSystem.Mouse.current = new UnityEngine.InputSystem.Mouse();
            UnityEngine.InputSystem.Mouse.current.scroll.Value = new UnityEngine.Vector2(0f, 7f);
            UnityEngine.Time.unscaledTime = 10f;
            UnityEngine.Time.frameCount = 100;
            global::MouseManager.RaiseScrollStep(1f);
            scrollable.scrollStepEvent.Invoke(1f);
            InvokePrivateMethod(probe, "LateUpdate");
            Assert(probe.CaptureSessionId == 1 && probe.CaptureMode == ShortCycleScrollTraceMode.RealInput &&
                probe.LatestRawScroll == 7f && probe.LastNonzeroRawScroll == 7f &&
                probe.LastNonzeroRawTime == 10f && probe.LastNonzeroRawFrame == 100 &&
                probe.LatestMouseStepDelta == 1 && probe.LatestTargetEventDelta == 1,
                "A real-input capture session must preserve raw wheel facts and per-frame event deltas.");

            UnityEngine.InputSystem.Mouse.current.scroll.Value = UnityEngine.Vector2.zero;
            UnityEngine.Time.unscaledTime = 11f;
            UnityEngine.Time.frameCount = 101;
            InvokePrivateMethod(probe, "LateUpdate");
            Assert(probe.LatestRawScroll == 0f && probe.LastNonzeroRawScroll == 7f &&
                probe.LastNonzeroRawFrame == 100 && probe.LatestMouseStepDelta == 0 &&
                probe.LatestTargetEventDelta == 0,
                "A later zero-input frame must not erase the last nonzero hardware sample.");

            var driver = new FridgeScrollLabDriver();
            SetPrivateField(driver, "scrollAdapter", adapter);
            SetPrivateField(driver, "traceProbe", probe);
            driver.BeginSimulatedSession();
            var sessionId = probe.CaptureSessionId;
            driver.SimulateNextTier();
            Assert(sessionId == 2 && probe.CaptureMode == ShortCycleScrollTraceMode.Simulated &&
                probe.MouseStepCount == 0 && probe.TargetEventCount == 0 &&
                adapter.ReceivedStepCount == 0 && driver.LastSimulationResult != null &&
                driver.LastSimulationResult.ErrorCode == ShortCycleActionErrorCode.Locked,
                "Explicit simulation may call Adapter.Dispatch, but must not inject MouseManager or target events.");
            probe.SetTraceEnabled(false);
            UnityEngine.InputSystem.Mouse.current = null;
        }

        private static FridgeCatTier CreateFridgeCatTier(string prefix)
        {
            var tier = new FridgeCatTier();
            var anchors = new UnityEngine.Transform[FridgeCatTier.ShelfColumnCount];
            for (var index = 0; index < anchors.Length; index++)
            {
                anchors[index] = new UnityEngine.Transform { name = prefix + "-" + index };
            }
            SetPrivateField(tier, "shelfAnchors", anchors);
            return tier;
        }

        private static FridgeCatEye CreateFridgeCatEye(out TransitionController controller)
        {
            controller = new TransitionController();
            controller.gameObject.SetActive(true);
            SetPrivateField(controller, "presetTarget", new UnityEngine.Transform());
            SetPrivateField(controller, "presets", new List<TransitionPreset>
            {
                new TransitionPreset { name = "BlinkClosed" },
                new TransitionPreset { name = "BlinkOpen" }
            });
            var eye = new FridgeCatEye();
            SetPrivateField(eye, "eyeBase", new UnityEngine.SpriteRenderer());
            SetPrivateField(eye, "pupil", new UnityEngine.SpriteRenderer());
            SetPrivateField(eye, "highlight", new UnityEngine.SpriteRenderer());
            SetPrivateField(eye, "glow", new UnityEngine.SpriteRenderer());
            SetPrivateField(eye, "blinkTransition", controller);
            return eye;
        }

        private static bool ColorsMatch(UnityEngine.Color left, UnityEngine.Color right)
        {
            return left.r == right.r && left.g == right.g && left.b == right.b && left.a == right.a;
        }

        private static void VerifyGeneratedDataCatalogAndInventory()
        {
            var recipe = new CK01RecipeData
            {
                name = "rcp_test",
                first_step_id = "step_test_01",
                slots = new List<string> { "slot_test" },
                whitelist_proc_ids = new List<string> { "proc_test" }
            };
            var firstStep = new CK01RecipeStepData
            {
                name = "step_test_01",
                recipe_id = "rcp_test",
                next_step_id = "step_test_02"
            };
            var secondStep = new CK01RecipeStepData
            {
                name = "step_test_02",
                recipe_id = "rcp_test",
                next_step_id = string.Empty
            };
            var itemAsset = new CK01ItemData
            {
                name = "asset_name_is_not_the_runtime_id",
                name_key = "item.test.egg"
            };
            var unknownItem = new CK01ItemData
            {
                name = "prd_unknown",
                name_key = "prd_unknown.name",
                item_kind = "product"
            };
            var slotAsset = new CK01RecipeSlotData { name = "slot_test", standard_ingredient = "ing_test_egg", required_count = 1 };
            var tagAsset = new CK01TagData { name = "tag_test" };
            var initialAsset = new CK01InitialInventoryData { name = "ing_test_egg", quantity = 2 };
            var processingRecord = new CK01ProcessingRecordData { name = "proc_test", output_item = "prd_unknown" };
            var configUnknown = new CK01CookingGameConfigData { name = CK01CookingGameConfigData.UnknownProductItemKey, value = "prd_unknown" };
            var configCapacity = new CK01CookingGameConfigData { name = CK01CookingGameConfigData.DemoFridgeCapacityLevelKey, value = "fcap_lv2" };
            var capacityLevel = new CK01FridgeCapacityLevelData { name = "fcap_lv2", level_index = 2, capacity = 30 };
            var kitchenArea = new KitchenAreaData
            {
                name = "kitchen-test",
                allowed_actions = new List<string> { "act_test" },
                allowed_carriers = new List<string> { "tool_test" }
            };
            var localization = new LocTableAsset { name = "Localization" };
            var tables = new List<CK01GeneratedDataTable>
            {
                new CK01GeneratedDataTable("Recipes", null, new[]
                {
                    new CK01GeneratedDataRowReference("rcp_test", recipe)
                }),
                new CK01GeneratedDataTable("RecipeSteps", null, new[]
                {
                    new CK01GeneratedDataRowReference("step_test_01", firstStep),
                    new CK01GeneratedDataRowReference("step_test_02", secondStep)
                }),
                new CK01GeneratedDataTable("Items", null, new[]
                {
                    new CK01GeneratedDataRowReference("ing_test_egg", itemAsset),
                    new CK01GeneratedDataRowReference("prd_unknown", unknownItem)
                }),
                new CK01GeneratedDataTable("RecipeSlots", null, new[]
                {
                    new CK01GeneratedDataRowReference("slot_test", slotAsset)
                }),
                new CK01GeneratedDataTable("Tags", null, new[]
                {
                    new CK01GeneratedDataRowReference("tag_test", tagAsset)
                }),
                new CK01GeneratedDataTable("InitialInventory", null, new[]
                {
                    new CK01GeneratedDataRowReference("ing_test_egg", initialAsset)
                }),
                new CK01GeneratedDataTable("ProcessingRecords", null, new[]
                {
                    new CK01GeneratedDataRowReference("proc_test", processingRecord)
                }),
                new CK01GeneratedDataTable("CookingGameConfig", null, new[]
                {
                    new CK01GeneratedDataRowReference(CK01CookingGameConfigData.UnknownProductItemKey, configUnknown),
                    new CK01GeneratedDataRowReference(CK01CookingGameConfigData.DemoFridgeCapacityLevelKey, configCapacity)
                }),
                new CK01GeneratedDataTable("FridgeCapacityLevels", null, new[]
                {
                    new CK01GeneratedDataRowReference("fcap_lv2", capacityLevel)
                }),
                new CK01GeneratedDataTable("KitchenAreas", null, new[]
                {
                    new CK01GeneratedDataRowReference("kitchen-test", kitchenArea)
                }),
                new CK01GeneratedDataTable("Localization", localization, null)
            };

            var catalog = new CK01GeneratedDataCatalog();
            Assert(catalog.ReplaceTables(tables), "The first catalog population must change the asset payload.");
            Assert(!catalog.ReplaceTables(tables), "An identical catalog population must be idempotent.");

            CK01RecipeData resolvedRecipe;
            string diagnostic;
            Assert(catalog.TryGetRecipe("rcp_test", out resolvedRecipe, out diagnostic) &&
                ReferenceEquals(resolvedRecipe, recipe), "Recipe IDs must resolve through the catalog.");

            IList<CK01RecipeStepData> orderedSteps;
            Assert(catalog.TryGetRecipeStepChain("rcp_test", out orderedSteps, out diagnostic) &&
                orderedSteps.Count == 2 &&
                ReferenceEquals(orderedSteps[0], firstStep) &&
                ReferenceEquals(orderedSteps[1], secondStep),
                "Recipe step chains must resolve in next_step_id order.");

            CK01ItemData resolvedItem;
            Assert(catalog.TryGetItem("ing_test_egg", out resolvedItem, out diagnostic) &&
                ReferenceEquals(resolvedItem, itemAsset), "Item IDs must resolve through the catalog.");

            CK01RecipeSlotData resolvedSlot;
            Assert(catalog.TryGetRecipeSlot("slot_test", out resolvedSlot, out diagnostic) &&
                ReferenceEquals(resolvedSlot, slotAsset), "T2 slot IDs must resolve through the catalog consumer API.");
            CK01TagData resolvedTag;
            Assert(catalog.TryGetTag("tag_test", out resolvedTag, out diagnostic) &&
                ReferenceEquals(resolvedTag, tagAsset), "T6 tag IDs must resolve through the catalog consumer API.");

            CK01InitialInventoryData resolvedInitial;
            Assert(catalog.TryGetInitialInventory("ing_test_egg", out resolvedInitial, out diagnostic) &&
                resolvedInitial.quantity == 2, "Initial inventory must resolve by item ID.");

            RecipeWhitelist whitelist;
            Assert(catalog.TryGetRecipeWhitelist("rcp_test", out whitelist, out diagnostic) &&
                whitelist.RecipeId == "rcp_test" && whitelist.ProcessingRecordIds.Count == 1 &&
                whitelist.ProcessingRecordIds[0] == "proc_test",
                "RecipeWhitelist must expose a copied read-only ProcessingRecords ID list.");
            recipe.whitelist_proc_ids.Add("proc_mutated_after_resolution");
            Assert(whitelist.ProcessingRecordIds.Count == 1,
                "RecipeWhitelist must not alias the mutable serialized source list.");
            recipe.whitelist_proc_ids.Remove("proc_mutated_after_resolution");

            AreaConstraint areaConstraint;
            Assert(catalog.TryGetAreaConstraint("kitchen-test", out areaConstraint, out diagnostic) &&
                areaConstraint.AllowedActions.Count == 1 && areaConstraint.AllowedCarriers.Count == 1,
                "AreaConstraint must resolve both read-only constraint lists.");
            UnknownProductResolver unknownResolver;
            CK01ItemData resolvedUnknown;
            Assert(catalog.TryGetUnknownProductResolver(out unknownResolver, out diagnostic) &&
                unknownResolver.ItemId == "prd_unknown" && unknownResolver.TryResolve(out resolvedUnknown) &&
                ReferenceEquals(resolvedUnknown, unknownItem),
                "UnknownProductResolver must resolve the configured product without implementing P2 behavior.");
            Assert(catalog.UnknownProductItemId == "prd_unknown",
                "UnknownProductItemId must expose the configured product ID without a parallel fallback.");
            CK01FridgeCapacityLevelData resolvedCapacity;
            Assert(catalog.TryGetDemoFridgeCapacity(out resolvedCapacity, out diagnostic) && resolvedCapacity.capacity == 30,
                "Demo fridge capacity must resolve through config key to fcap_lv2=30.");
            Assert(catalog.DemoFridgeCapacityLevel == 30,
                "DemoFridgeCapacityLevel must expose the configured capacity value rather than a record count.");

            var missingConfigCatalog = new CK01GeneratedDataCatalog();
            missingConfigCatalog.ReplaceTables(new CK01GeneratedDataTable[0]);
            var missingConfigFailedExplicitly = false;
            try
            {
                missingConfigFailedExplicitly = missingConfigCatalog.UnknownProductItemId == null;
            }
            catch (InvalidOperationException exception)
            {
                missingConfigFailedExplicitly = exception.Message.Contains("CookingGameConfig");
            }
            Assert(missingConfigFailedExplicitly,
                "UnknownProductItemId must explicitly fail when its config is missing; it must not return a hardcoded fallback.");

            capacityLevel.capacity = 0;
            var invalidCapacityFailedExplicitly = false;
            try
            {
                invalidCapacityFailedExplicitly = catalog.DemoFridgeCapacityLevel < 0;
            }
            catch (InvalidOperationException exception)
            {
                invalidCapacityFailedExplicitly = exception.Message.Contains("must be positive");
            }
            finally
            {
                capacityLevel.capacity = 30;
            }
            Assert(invalidCapacityFailedExplicitly,
                "DemoFridgeCapacityLevel must explicitly fail for an invalid configured capacity; it must not return a hardcoded fallback.");

            LocTableAsset resolvedLocalization;
            Assert(catalog.TryGetLocalization(out resolvedLocalization, out diagnostic) &&
                ReferenceEquals(resolvedLocalization, localization),
                "Localization must resolve as the registered table aggregate.");

            Assert(!catalog.TryGetRecipe("rcp_missing", out resolvedRecipe, out diagnostic) &&
                diagnostic.Contains("rcp_missing"), "Missing IDs must return a diagnostic without throwing.");

            var duplicateCatalog = new CK01GeneratedDataCatalog();
            duplicateCatalog.ReplaceTables(new[]
            {
                new CK01GeneratedDataTable("Recipes", null, new[]
                {
                    new CK01GeneratedDataRowReference("rcp_duplicate", recipe),
                    new CK01GeneratedDataRowReference("rcp_duplicate", new CK01RecipeData())
                })
            });
            Assert(!duplicateCatalog.TryGetRecipe("rcp_duplicate", out resolvedRecipe, out diagnostic) &&
                diagnostic.Contains("duplicate stable ID"),
                "Duplicate stable IDs must invalidate catalog lookup with an auditable diagnostic.");

            var inventory = new InventoryManager();
            SetPrivateField(inventory, "dataCatalog", catalog);
            Assert(inventory.TryRebuildFromDataCatalog(out diagnostic),
                "Inventory must initialize from the catalog: " + diagnostic);

            CK01ItemData item;
            Assert(inventory.TryGetGeneratedItem("ing_test_egg", out item) && item != null,
                "Inventory must resolve generated Items assets through catalog stable IDs.");
            var reflow = new TraceReflow();
            inventory.SetRuntimeTightReflow(reflow);
            var commit = inventory.TryCommitPlaceholderStates(new List<PlaceholderState>
            {
                new PlaceholderState { instance_id = "egg-stack", item_id = "ing_test_egg", taken_count = 1 }
            });
            Assert(commit.Succeeded && inventory.GetQuantity("ing_test_egg") == 1, "Inventory must deduct generated-item stock exactly once.");
            Assert(reflow.Count == 1, "Successful inventory deduction must request one tight reflow.");

            VerifyBinderIdempotence(catalog, inventory, itemAsset);
        }

        private static void VerifyBinderIdempotence(
            CK01GeneratedDataCatalog catalog,
            InventoryManager inventory,
            CK01ItemData expectedItem)
        {
            var fridgeSlots = new FridgeSlot[30];
            for (var index = 0; index < fridgeSlots.Length; index++) fridgeSlots[index] = new FridgeSlot();
            var fridgeLabel = new EatWhat.Tools.Localization.LocalizedText();
            var fridgeBinder = new ShortCycleFridgeBinder();
            var firstTier = CreateFridgeCatTier("binder-row-1");
            var secondTier = CreateFridgeCatTier("binder-row-2");
            var extensionTier = CreateFridgeCatTier("binder-extension");
            var content = new global::DragContainer
            {
                mode = global::DragContainer.DragMode.Vertical,
                LimitMinY = 0f,
                LimitMaxY = 10f
            };
            var scrollArea = new global::ScrollArea_Controller
            {
                contentDragContainer = content,
                scrollMode = global::ScrollArea_Controller.ScrollMode.Vertical
            };
            var fridgeRig = new FridgeCatRig();
            SetPrivateField(fridgeRig, "fixedTiers", new[] { firstTier, secondTier });
            SetPrivateField(fridgeRig, "extensionTierPrefab", extensionTier);
            SetPrivateField(fridgeRig, "extensionTierRoot", new UnityEngine.Transform());
            SetPrivateField(fridgeRig, "scrollArea", scrollArea);
            SetPrivateField(fridgeBinder, "fridgeCatRig", fridgeRig);
            SetPrivateField(fridgeBinder, "slots", fridgeSlots);
            SetPrivateField(fridgeBinder, "slotNameBindings", new[] { fridgeLabel });

            string diagnostic;
            Assert(fridgeBinder.Bind(catalog, inventory, out diagnostic), diagnostic);
            Assert(fridgeBinder.Bind(catalog, inventory, out diagnostic),
                "Repeated fridge binding must be idempotent: " + diagnostic);
            Assert(fridgeBinder.BoundCapacity == 30 && fridgeRig.TierCount == 6 &&
                fridgeRig.PooledExtensionTierCount == 4,
                "Capacity binding must select fcap_lv2=30, six tiers, and four reusable extension rows.");
            Assert(fridgeSlots[0].gameObject.activeSelf && !fridgeSlots[1].gameObject.activeSelf &&
                !fridgeSlots[29].gameObject.activeSelf,
                "Only positive-quantity inventory records may occupy configured capacity slots.");
            Assert(ReferenceEquals(fridgeSlots[0].Display.icon_sprite, expectedItem.icon_sprite) &&
                fridgeLabel.LocalizationKey == expectedItem.name_key,
                "Fridge Binder must reuse configured slots and refresh their LocalizedText contract.");
            Assert(ReferenceEquals(fridgeSlots[0].transform.parent, firstTier.GetShelfAnchor(0)),
                "Fridge Binder must parent each data row under its deterministic rig shelf anchor.");

            for (var index = 0; index < 5; index++) Assert(fridgeBinder.ScrollTier(1).Succeeded, "Capacity tier traversal must reach tier 5.");
            Assert(fridgeBinder.CurrentTier == 5, "Capacity traversal upper bound must be the sixth zero-based tier.");
            Assert(!fridgeBinder.ScrollTier(1).Succeeded && fridgeBinder.CurrentTier == 5,
                "Capacity tier traversal must reject the upper bound without changing CurrentTier.");
            Assert(fridgeBinder.ApplyFilterView(6) == 2 && fridgeBinder.CurrentTier == 1 &&
                Math.Abs(content.transform.position.y - 10f) < .0001f,
                "Filter shrinking must clamp CurrentTier and move the real rig to the legal last tier.");
            Assert(fridgeBinder.Bind(catalog, inventory, out diagnostic) && fridgeRig.TierCount == 6 &&
                fridgeBinder.CurrentTier == 1 && Math.Abs(content.transform.position.y - 2f) < .0001f,
                "Rebinding must restore six capacity tiers and the clamped tier's legal rig position.");
            Assert(fridgeBinder.ApplyFilterView(0) == 1 && fridgeBinder.CurrentTier == 0 &&
                Math.Abs(content.transform.position.y) < .0001f,
                "A zero-result filter must retain one visible tier and reset to its only legal position.");
            var emptyCommit = inventory.TryCommitPlaceholderStates(new List<PlaceholderState>
            {
                new PlaceholderState { instance_id = "egg-stack-empty", item_id = "ing_test_egg", taken_count = 1 }
            });
            Assert(emptyCommit.Succeeded && inventory.GetQuantity("ing_test_egg") == 0,
                "Zero-quantity binder fixture must consume the final unit exactly once.");
            Assert(fridgeBinder.Bind(catalog, inventory, out diagnostic) && fridgeRig.TierCount == 6,
                "Zero-quantity rebinding must preserve capacity tiers: " + diagnostic);
            for (var index = 0; index < fridgeSlots.Length; index++)
                Assert(!fridgeSlots[index].gameObject.activeSelf, "Zero-quantity records must not occupy fridge slots.");

            var slotBlock = new SlotBlock();
            SetPrivateField(slotBlock, "centerGroup", new UnityEngine.Transform());
            var anchors = new UnityEngine.Transform[ShortCycleAnchorContract.SubstituteAnchorCount];
            var subCards = new ShortCycleSubCard[ShortCycleAnchorContract.SubstituteAnchorCount];
            for (var index = 0; index < anchors.Length; index++)
            {
                anchors[index] = new UnityEngine.Transform
                {
                    name = ShortCycleAnchorContract.GetSubstituteAnchorName(index)
                };
                subCards[index] = new ShortCycleSubCard();
            }
            SetPrivateField(slotBlock, "subSlotAnchors", anchors);
            SetPrivateField(slotBlock, "subCards", subCards);
            var clueLabel = new EatWhat.Tools.Localization.LocalizedText();
            var clueBinder = new ShortCycleClueBoardBinder();
            SetPrivateField(clueBinder, "slotBlocks", new[] { slotBlock });
            SetPrivateField(clueBinder, "slotNameBindings", new[] { clueLabel });

            Assert(clueBinder.Bind(catalog, "rcp_test", inventory, out diagnostic), diagnostic);
            Assert(clueBinder.Bind(catalog, "rcp_test", inventory, out diagnostic),
                "Repeated clue-board binding must be idempotent: " + diagnostic);
            Assert(ReferenceEquals(slotBlock.Display.icon_sprite, expectedItem.icon_sprite) &&
                clueLabel.LocalizationKey == expectedItem.name_key,
                "Clue-board Binder must reuse configured SlotBlocks and refresh their LocalizedText contract.");

            subCards[0].Configure(new ShortCycleSubCardBinding { ItemId = "stale" });
            var activeBeforeClear = slotBlock.gameObject.activeSelf;
            Assert(clueBinder.Bind(catalog, null, inventory, out diagnostic),
                "An empty recipe must clear clue content without requiring another lookup: " + diagnostic);
            Assert(slotBlock.gameObject.activeSelf == activeBeforeClear && slotBlock.Display != null &&
                slotBlock.Display.icon_sprite == null && subCards[0].Binding == null &&
                clueLabel.LocalizationKey == string.Empty,
                "Empty clue binding must clear the configured block content without changing presentation activation.");
            Assert(clueBinder.Bind(catalog, string.Empty, inventory, out diagnostic),
                "Repeated empty clue binding must remain idempotent.");
        }

        private static void VerifyMouseInteractionLayerStackBridge()
        {
            var baseLayer = new MouseInteractionLayer();
            var modalLayer = new MouseInteractionLayer();
            var stack = new ShortCycleInteractionLayerStack();
            SetPrivateField(stack, "baseLayer", baseLayer);
            SetPrivateField(stack, "configuredLayers", new List<MouseInteractionLayer> { baseLayer, modalLayer });

            InvokePrivateMethod(stack, "Awake");
            InvokePrivateMethod(stack, "Start");
            Assert(stack.Count == 1 && stack.Top == baseLayer, "Layer bridge must initialize with the configured base layer.");
            Assert(baseLayer.manualStackLayer && baseLayer.PushCount == 1, "Base layer must push through MouseInteractionLayer.OnPushLayer exactly once.");
            Assert(stack.Push(modalLayer), "Configured modal layer must push.");
            Assert(!stack.Push(modalLayer), "The same modal layer must not push twice.");
            Assert(modalLayer.manualStackLayer && modalLayer.PushCount == 1 && stack.Top == modalLayer, "Modal layer must use the project layer API and become top.");
            Assert(!stack.Pop(baseLayer), "A non-top layer must not pop.");
            Assert(stack.Pop(modalLayer) && modalLayer.RemoveCount == 1, "Modal layer must remove through MouseInteractionLayer.OnRemoveLayer exactly once.");

            InvokePrivateMethod(stack, "OnDestroy");
            Assert(baseLayer.RemoveCount == 1 && stack.Count == 0, "Destroy must remove the remaining base layer through the project API.");
        }

        private static void VerifyDirectorLayerOwnership()
        {
            var phase0Layer = new MouseInteractionLayer();
            var phase1Layer = new MouseInteractionLayer();
            var modalLayer = new MouseInteractionLayer();
            var stack = new ShortCycleInteractionLayerStack();
            SetPrivateField(stack, "baseLayer", phase0Layer);
            SetPrivateField(stack, "configuredLayers", new List<MouseInteractionLayer>
            {
                phase0Layer,
                phase1Layer,
                modalLayer
            });
            InvokePrivateMethod(stack, "Awake");
            InvokePrivateMethod(stack, "Start");

            var phase0 = new ShortCycleSpace();
            SetPrivateField(phase0, "spaceId", ShortCycleSpaceId.Phase0);
            SetPrivateField(phase0, "spaceLayer", phase0Layer);
            var phase1 = new ShortCycleSpace();
            SetPrivateField(phase1, "spaceId", ShortCycleSpaceId.Phase1);
            SetPrivateField(phase1, "spaceLayer", phase1Layer);
            var modal = new ShortCycleModalSet();
            SetPrivateField(modal, "modalId", ShortCycleModalId.StepDetail);
            SetPrivateField(modal, "modalLayer", modalLayer);

            var director = new ShortCyclePresentationDirector();
            SetPrivateField(director, "spaces", new[] { phase0, phase1 });
            SetPrivateField(director, "interactionLayerStack", stack);
            SetPrivateField(director, "modals", new[]
            {
                new ShortCycleModalEntry { modal = ShortCycleModalId.StepDetail, set = modal }
            });

            director.EnterSpace(ShortCycleSpaceId.Phase0);
            Assert(stack.Top == phase0Layer && phase0Layer.PushCount == 1,
                "Initial Phase0 entry must preserve the single project base layer.");
            director.LeaveSpace(ShortCycleSpaceId.Phase0);
            director.EnterSpace(ShortCycleSpaceId.Phase1);
            Assert(stack.Top == phase1Layer && phase1Layer.PushCount == 1,
                "Director EnterSpace must push the explicit Phase1 layer.");
            director.PushModal(ShortCycleModalId.StepDetail);
            Assert(stack.Top == modalLayer && modalLayer.PushCount == 1,
                "Director PushModal must push the explicit Layer_Modal reference.");
            director.PopModal(ShortCycleModalId.StepDetail);
            Assert(stack.Top == phase1Layer && modalLayer.RemoveCount == 1,
                "Director PopModal must pop Layer_Modal before deactivating its visuals.");
            director.LeaveSpace(ShortCycleSpaceId.Phase1);
            Assert(stack.Top == phase0Layer && phase1Layer.RemoveCount == 1,
                "Director LeaveSpace must pop the departing space layer.");
        }

        private static void VerifyTrayControllerRequiresHost()
        {
            var controller = new ShortCycleTrayStateController();
            InvokePrivateMethod(controller, "Awake");
            var stateField = typeof(ShortCycleTrayStateController).GetField(
                "stateMachine",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert(!controller.enabled && stateField != null && stateField.GetValue(controller) == null,
                "TrayStateController must disable itself without constructing an FSM when its Director host is missing.");
            controller.ShowDetail();
            Assert(stateField.GetValue(controller) == null,
                "A disabled host-less TrayStateController must not recreate a fallback FSM.");
        }

        private static void SetPrivateField(object instance, string fieldName, object value)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                throw new InvalidOperationException("Could not locate private test field '" + fieldName + "'.");
            }
            field.SetValue(instance, value);
        }

        private static object GetPrivateField(object instance, string fieldName)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                throw new InvalidOperationException("Could not locate private test field '" + fieldName + "'.");
            }
            return field.GetValue(instance);
        }

        private static object InvokePrivateMethodWithResult(object instance, string methodName, params object[] arguments)
        {
            var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
            {
                throw new InvalidOperationException("Could not locate private test method '" + methodName + "'.");
            }
            return method.Invoke(instance, arguments);
        }

        private static void InvokePrivateMethod(object instance, string methodName)
        {
            var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
            {
                throw new InvalidOperationException("Could not locate private test method '" + methodName + "'.");
            }
            method.Invoke(instance, null);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void AssertTrace(IList<string> actual, params string[] expected)
        {
            Assert(actual.Count == expected.Length,
                "Presentation trace length mismatch. Expected " + expected.Length + " but got " + actual.Count +
                ": " + string.Join(",", actual));
            for (var index = 0; index < expected.Length; index++)
            {
                Assert(actual[index] == expected[index],
                    "Presentation trace mismatch at " + index + ": expected " + expected[index] +
                    " but got " + actual[index] + ".");
            }
        }

        private sealed class TraceSaveBoundary : ISaveBoundary
        {
            private readonly IList<string> trace;
            public TraceSaveBoundary(IList<string> trace) { this.trace = trace; }
            public void Reach(ShortCycleSavePoint savePoint, ShortCycleContext context) { trace.Add("save:" + savePoint); }
        }

        private sealed class TraceInventory : IInventoryConsumptionGateway
        {
            private readonly IList<string> trace;
            public TraceInventory(IList<string> trace) { this.trace = trace; }
            public InventoryCommitResult TryCommitPlaceholderStates(IList<PlaceholderState> placeholderStates)
            {
                trace.Add("consume");
                return InventoryCommitResult.Success(1);
            }
        }

        private sealed class TraceHandoff : IShortCyclePhase2HandoffSink
        {
            private readonly IList<string> trace;
            public TraceHandoff(IList<string> trace) { this.trace = trace; }
            public bool IsAvailable { get { return true; } }
            public bool TryReceive(ShortCycleDataHandoff handoff, out string error)
            {
                trace.Add("handoff");
                error = null;
                return true;
            }
        }

        private sealed class TraceExit : IShortCycleUpstreamExit
        {
            public void ExitToUpstream(ShortCycleTriggerContext triggerContext) { }
        }

        private sealed class TracePresentationHost : IShortCyclePresentationHost
        {
            public readonly List<string> Trace = new List<string>();
            public void EnterSpace(ShortCycleSpaceId space) { Trace.Add("EnterSpace:" + space); }
            public void LeaveSpace(ShortCycleSpaceId space) { Trace.Add("LeaveSpace:" + space); }
            public void ShowView(ShortCycleSpaceId space, ShortCyclePhase0View view) { Trace.Add("ShowView:" + space + ":" + view); }
            public void HideView(ShortCycleSpaceId space, ShortCyclePhase0View view) { Trace.Add("HideView:" + space + ":" + view); }
            public void PushModal(ShortCycleModalId modal) { Trace.Add("PushModal:" + modal); }
            public void PopModal(ShortCycleModalId modal) { Trace.Add("PopModal:" + modal); }
            public void SetTrayView(ShortCycleTrayView view) { Trace.Add("SetTrayView:" + view); }
            public void ClearSessionPresentation() { Trace.Add("ClearSessionPresentation"); }
        }

        private sealed class TraceReflow : IInventoryTightReflow
        {
            public int Count { get; private set; }
            public void RequestTightReflow() { Count++; }
        }

        private sealed class TraceDisposable : IDisposable
        {
            public int DisposeCount { get; private set; }
            public void Dispose() { DisposeCount++; }
        }
    }
}
