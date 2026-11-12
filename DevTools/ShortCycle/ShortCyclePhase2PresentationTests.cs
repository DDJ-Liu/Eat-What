using System;
using System.Collections.Generic;
using System.Reflection;
using EatWhat.Cooking.ShortCycle;
using UnityEngine;

public static class ShortCyclePhase2PresentationTests
{
    private static readonly List<string> cases = new List<string>();
    private static readonly List<string> finalCallbackCases = new List<string>();
    public static IList<string> FinalCallbackCases { get { return finalCallbackCases.AsReadOnly(); } }
    private static void FinalCheck(bool value, string message)
    { if (!value) throw new Exception("P2-C9 final callback: " + message); finalCallbackCases.Add(message); }
    private static readonly List<string> probeCases = new List<string>();
    public static IList<string> ProbeCases { get { return probeCases.AsReadOnly(); } }
    private static void ProbeCheck(bool value, string message)
    { if (!value) throw new Exception("P2-C9 probe: " + message); probeCases.Add(message); }
    private static readonly List<string> layerLifecycleCases = new List<string>();
    public static IList<string> LayerLifecycleCases { get { return layerLifecycleCases.AsReadOnly(); } }
    private static void LayerCheck(bool value, string message)
    { if (!value) throw new Exception("P2-C9 layer lifecycle: " + message); layerLifecycleCases.Add(message); }
    public sealed class InterruptGate : MonoBehaviour, IShortCycleStateGatedInteraction
    {
        public Action OnFocus;
        public void SetInteractionAllowed(bool allowed)
        { if (allowed && OnFocus != null) { var callback = OnFocus; OnFocus = null; callback(); } }
    }
    public static IList<string> Cases { get { return cases.AsReadOnly(); } }
    private static void Check(bool value, string message)
    { if (!value) throw new Exception("P2-C9: " + message); cases.Add(message); }
    private static void Field(object target, string name, object value)
    {
        var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null) throw new Exception("Missing field " + name);
        field.SetValue(target, value);
    }
    private static void Call(object target, string name)
    { target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null); }
    private static ShortCycleInteractionLayerStack LayerStack(MouseInteractionLayer baseLayer,
        params MouseInteractionLayer[] configured)
    {
        var stack = new GameObject("LifecycleStack").AddComponent<ShortCycleInteractionLayerStack>();
        Field(stack, "baseLayer", baseLayer);
        Field(stack, "configuredLayers", new List<MouseInteractionLayer>(configured));
        Call(stack, "Awake");
        return stack;
    }
    private static void SwitchBase(ShortCycleInteractionLayerStack stack, MouseInteractionLayer target)
    {
        typeof(ShortCycleInteractionLayerStack).GetMethod("SwitchBaseLayer",
            BindingFlags.Instance | BindingFlags.NonPublic).Invoke(stack, new object[] { target });
    }
    public static void PrepareTableFixture()
    {
        // The narrow AI397 branch skips ShortCyclePhase2Tests.Run(), which normally
        // initializes the shared CSV assertion collector used by Table<T>().
        var casesProperty = typeof(ShortCyclePhase2Tests).GetProperty("PassedCases");
        if (casesProperty.GetValue(null, null) == null)
            casesProperty.GetSetMethod(true).Invoke(null, new object[] { new List<string>() });
    }
    public static int RunLayerLifecycleCases()
    {
        layerLifecycleCases.Clear();
        try
        {
            var manager = new GameObject("MouseManager").AddComponent<MouseManager>();
            MouseManager.Instance = manager;
            var foreign = new GameObject("ForeignLayer").AddComponent<MouseInteractionLayer>();
            manager.mouseInteractiveLayers.Push(foreign);
            var firstBase = new GameObject("FirstBase").AddComponent<MouseInteractionLayer>();
            var firstSpace = new GameObject("FirstSpace").AddComponent<MouseInteractionLayer>();
            var firstModal = new GameObject("FirstModal").AddComponent<MouseInteractionLayer>();
            var first = LayerStack(firstBase, firstSpace, firstModal);
            LayerCheck(first.Count == 1 && first.Top == firstBase && manager.currentMouseLayer == foreign,
                "Awake owns base locally without disturbing foreign global layer");
            Call(first, "Start");
            LayerCheck(manager.currentMouseLayer == firstBase &&
                manager.mouseInteractiveLayers.Count == 2, "Awake-Start pushes one base");
            Call(first, "Start");
            LayerCheck(manager.mouseInteractiveLayers.Count == 2 && manager.currentMouseLayer == firstBase,
                "repeated Start does not duplicate base");
            LayerCheck(first.Push(firstSpace) && manager.currentMouseLayer == firstSpace &&
                first.Top == firstSpace, "Awake-Start-Push aligns local and global tops");
            LayerCheck(!first.Push(firstSpace) && !first.Push(null) &&
                !first.Push(foreign) && manager.mouseInteractiveLayers.Count == 3,
                "duplicate, null and unconfigured Push reject without global mutation");
            LayerCheck(first.Push(firstModal) && manager.currentMouseLayer == firstModal,
                "modal Push becomes the global top");
            LayerCheck(first.Remove(firstSpace) && first.Top == firstModal &&
                manager.currentMouseLayer == firstModal &&
                manager.mouseInteractiveLayers.Count == 3 &&
                !manager.mouseInteractiveLayers.Contains(firstSpace),
                "Remove middle layer preserves modal and foreign order");
            LayerCheck(!first.Pop(firstSpace) && first.Pop(firstModal) &&
                manager.currentMouseLayer == firstBase, "Pop accepts only exact top");
            Call(first, "OnDestroy");
            LayerCheck(manager.mouseInteractiveLayers.Count == 1 && manager.currentMouseLayer == foreign,
                "Destroy removes only owned layers after Start-first path");

            var earlyBase = new GameObject("EarlyBase").AddComponent<MouseInteractionLayer>();
            var earlySpace = new GameObject("EarlySpace").AddComponent<MouseInteractionLayer>();
            var earlyModal = new GameObject("EarlyModal").AddComponent<MouseInteractionLayer>();
            var early = LayerStack(earlyBase, earlySpace, earlyModal);
            LayerCheck(early.Push(earlySpace) && early.Push(earlyModal) &&
                manager.currentMouseLayer == earlyModal, "pre-Start handoff pushes base before space and modal");
            Call(early, "Start");
            LayerCheck(early.Top == earlyModal && manager.currentMouseLayer == earlyModal &&
                manager.mouseInteractiveLayers.Count == 4,
                "late Start keeps modal as global top rather than burying it below base");
            manager.mouseInteractiveLayers.Push(new GameObject("ExternalOverlay")
                .AddComponent<MouseInteractionLayer>());
            var externalTop = manager.currentMouseLayer;
            Call(early, "Start");
            LayerCheck(manager.currentMouseLayer == externalTop &&
                manager.mouseInteractiveLayers.Count == 5,
                "repeated late Start does not move an unrelated external top");
            early.RestoreTransientState();
            LayerCheck(early.Count == 1 && early.Top == earlyBase &&
                manager.currentMouseLayer == earlyBase &&
                manager.mouseInteractiveLayers.Contains(foreign) &&
                manager.mouseInteractiveLayers.Contains(externalTop),
                "RestoreTransientState removes owned transient layers only");
            Call(early, "OnDestroy");
            LayerCheck(manager.mouseInteractiveLayers.Count == 2 &&
                manager.mouseInteractiveLayers.Contains(foreign) &&
                manager.mouseInteractiveLayers.Contains(externalTop),
                "Destroy leaves both unrelated layers in their original order");

            var switchedBase = new GameObject("SwitchedBase").AddComponent<MouseInteractionLayer>();
            var oldBase = new GameObject("OldBase").AddComponent<MouseInteractionLayer>();
            var switched = LayerStack(oldBase, earlySpace);
            SwitchBase(switched, switchedBase);
            Call(switched, "Start");
            LayerCheck(switched.Top == switchedBase && manager.currentMouseLayer == switchedBase &&
                !manager.mouseInteractiveLayers.Contains(oldBase),
                "base switch before Start never pushes the obsolete base");
            Call(switched, "OnDestroy");

            MouseManager.Instance = null;
            var absentBase = new GameObject("AbsentBase").AddComponent<MouseInteractionLayer>();
            var absentSpace = new GameObject("AbsentSpace").AddComponent<MouseInteractionLayer>();
            var absentModal = new GameObject("AbsentModal").AddComponent<MouseInteractionLayer>();
            var absent = LayerStack(absentBase, absentSpace, absentModal);
            LayerCheck(absent.Push(absentSpace), "Push remains locally valid without MouseManager");
            Call(absent, "Start");
            LayerCheck(absent.Top == absentSpace && absent.Count == 2,
                "Start without MouseManager remains safe and keeps local order");
            var lateManager = new GameObject("LateMouseManager").AddComponent<MouseManager>();
            MouseManager.Instance = lateManager;
            LayerCheck(absent.Push(absentModal) && lateManager.currentMouseLayer == absentModal &&
                lateManager.mouseInteractiveLayers.Count == 3,
                "next legal Push after manager readiness replays owned order");
            Call(absent, "OnDestroy");
            LayerCheck(lateManager.mouseInteractiveLayers.Count == 0,
                "Destroy clears only its three owned layers after late manager readiness");
            return layerLifecycleCases.Count;
        }
        finally { MouseManager.Instance = null; }
    }
    private sealed class Fixture
    {
        public ShortCyclePresentationDirector Director;
        public ShortCycleSpace Space;
        public ShortCycleInteractionLayerStack Stack;
        public ShortCycleAreaDetailBinding Binding;
        public KitchenAreaController Controller;
        public Phase2SceneEntry Entry;
        public InventoryManager Inventory;
        public Dictionary<ShortCyclePhase2SetRole, ShortCyclePresentationSet> Sets =
            new Dictionary<ShortCyclePhase2SetRole, ShortCyclePresentationSet>();
        public Dictionary<ShortCyclePhase2SetRole, MonoBehaviour> Gates =
            new Dictionary<ShortCyclePhase2SetRole, MonoBehaviour>();
        public ShortCyclePhase2ViewSetEntry[] Rows;
        public MouseInteractionLayer ModalLayer;
        public MouseInteractionLayer SpaceLayer;
    }
    private static Fixture Build(string repoRoot, bool duplicate = false, bool wireEntry = false, Action<Fixture> beforeConsume = null)
    {
        var f = new Fixture();
        var baseLayer = new GameObject("BaseLayer").AddComponent<MouseInteractionLayer>();
        f.SpaceLayer = new GameObject("P2Layer").AddComponent<MouseInteractionLayer>();
        f.ModalLayer = new GameObject("ModalLayer").AddComponent<MouseInteractionLayer>();
        f.Stack = new GameObject("LayerStack").AddComponent<ShortCycleInteractionLayerStack>();
        Field(f.Stack, "baseLayer", baseLayer);
        Field(f.Stack, "configuredLayers", new List<MouseInteractionLayer>{f.SpaceLayer,f.ModalLayer});
        Call(f.Stack, "Awake");
        f.Space = new GameObject("Space_Phase2").AddComponent<ShortCycleSpace>();
        Field(f.Space, "spaceId", ShortCycleSpaceId.Phase2);
        Field(f.Space, "spaceLayer", f.SpaceLayer);
        Field(f.Space, "phase2ModalLayer", f.ModalLayer);
        var rows = new List<ShortCyclePhase2ViewSetEntry>();
        foreach (ShortCyclePhase2SetRole role in Enum.GetValues(typeof(ShortCyclePhase2SetRole)))
        {
            var owner = new GameObject(role + "_Set");
            owner.transform.SetParent(f.Space.transform,false);
            var set = owner.AddComponent<ShortCyclePresentationSet>();
            var container = new GameObject(role + "_Group");
            container.transform.SetParent(owner.transform, false);
            var gate = container.AddComponent<MonoBehaviour>();
            Field(set, "containerRoot", container);
            Field(set, "gatedInteractions", new MonoBehaviour[]{gate});
            f.Sets.Add(role,set); f.Gates.Add(role,gate);
            rows.Add(new ShortCyclePhase2ViewSetEntry{role=role,set=set});
        }
        f.Rows=rows.ToArray();
        if (duplicate) f.Rows[1].set=f.Rows[0].set;
        Field(f.Space,"phase2ViewTable",f.Rows);
        f.Director = new GameObject("PresentationDirector_P2").AddComponent<ShortCyclePresentationDirector>();
        Field(f.Director,"spaces",new[]{f.Space});
        Field(f.Director,"interactionLayerStack",f.Stack);
        f.Binding = new GameObject("AreaDetailBinding").AddComponent<ShortCycleAreaDetailBinding>();
        Field(f.Binding,"areaDetailSet",f.Sets[ShortCyclePhase2SetRole.AreaDetail]);
        f.Controller = new GameObject("KitchenAreaController").AddComponent<KitchenAreaController>();
        Field(f.Controller,"presentationDirector",f.Director);
        Field(f.Controller,"areaDetailBinding",f.Binding);
        f.Entry = new GameObject("Phase2SceneEntry").AddComponent<Phase2SceneEntry>();
        Field(f.Controller,"sceneEntry",f.Entry);
        if (wireEntry)
        {
            Field(f.Entry,"presentationDirector",f.Director);
            Field(f.Entry,"kitchenAreaController",f.Controller);
        }
        f.Inventory = new GameObject("Inventory").AddComponent<InventoryManager>();
        var catalog = new CK01GeneratedDataCatalog();
        catalog.ReplaceTables(new []{
            ShortCyclePhase2Tests.Table<KitchenAreaData>(repoRoot,"KitchenAreas","id")});
        Field(f.Inventory,"dataCatalog",catalog);
        if (beforeConsume != null) beforeConsume(f);
        string error;
        var consumed=f.Entry.TryConsume(new ShortCycleDataHandoff("recipe",new List<PrepZoneItem>()),
            f.Inventory,out error);
        if (wireEntry && duplicate)
            Check(!consumed && f.Inventory.Phase2Owner==null &&
                !f.Director.Phase2ViewActive,"invalid wired table rejects before inventory or presentation ownership");
        else
            Check(consumed,"P2 handoff creates current catalog owner");
        return f;
    }
    private static void Matrix(Fixture f, ShortCyclePhase2View view, bool staple)
    {
        var modal = view == ShortCyclePhase2View.Interaction || view == ShortCyclePhase2View.MiniGame;
        foreach (var row in f.Rows)
        {
            var role=row.role;
            bool active;
            switch(role)
            {
                case ShortCyclePhase2SetRole.TopBar:
                case ShortCyclePhase2SetRole.PrepTray: active=true; break;
                case ShortCyclePhase2SetRole.Overview:
                case ShortCyclePhase2SetRole.RestockEntry: active=view==ShortCyclePhase2View.Overview; break;
                case ShortCyclePhase2SetRole.AreaDetail: active=view!=ShortCyclePhase2View.Overview; break;
                case ShortCyclePhase2SetRole.HandTool: active=view==ShortCyclePhase2View.AreaDetail; break;
                case ShortCyclePhase2SetRole.InteractionModal: active=view==ShortCyclePhase2View.Interaction; break;
                case ShortCyclePhase2SetRole.MiniGameModal: active=view==ShortCyclePhase2View.MiniGame; break;
                case ShortCyclePhase2SetRole.StapleBubble: active=view==ShortCyclePhase2View.AreaDetail && staple; break;
                case ShortCyclePhase2SetRole.Escape:
                case ShortCyclePhase2SetRole.StepDetailEntry: active=!modal; break;
                default: active=false; break;
            }
            var focus=active && (!modal ||
                role==ShortCyclePhase2SetRole.InteractionModal && view==ShortCyclePhase2View.Interaction ||
                role==ShortCyclePhase2SetRole.MiniGameModal && view==ShortCyclePhase2View.MiniGame);
            Check(row.set.IsVisibilityRequested==active && row.set.IsFocused==focus &&
                f.Gates[role].enabled==focus,"matrix "+view+" "+role+" active/focus/input");
        }
        Check(f.Stack.Top==(modal?f.ModalLayer:f.SpaceLayer),"input layer "+view);
    }
    public static int RunFinalCallbackCases(string repoRoot)
    {
        finalCallbackCases.Clear();
        const string prep = "kitchen-prep-counter";
        const string stove = "kitchen-stove";
        ShortCyclePhase2ProbeResult result;
        string error;
        var final = Build(repoRoot, false, true, fixture =>
        {
            var gate = fixture.Sets[ShortCyclePhase2SetRole.Overview]
                .gameObject.AddComponent<InterruptGate>();
            Field(fixture.Sets[ShortCyclePhase2SetRole.Overview],
                "gatedInteractions", new MonoBehaviour[]{gate});
        });
        var finalProbe = new GameObject("FinalCallbackProbe").AddComponent<ShortCycleAutomatedProbe>();
        var overviewGate = final.Sets[ShortCyclePhase2SetRole.Overview]
            .gameObject.GetComponent<InterruptGate>();
        var overviewFocuses = 0;
        Action overviewCallback = null;
        overviewCallback = () =>
        {
            overviewFocuses++;
            if (overviewFocuses == 2) finalProbe.CancelPhase2MatrixProbe();
            else overviewGate.OnFocus = overviewCallback;
        };
        overviewGate.OnFocus = overviewCallback;
        FinalCheck(!finalProbe.TryRunPhase2MatrixProbe(final.Entry, final.Controller,
            final.Director, final.Space, final.Stack, prep, stove, out result) &&
            overviewFocuses == 2 && result.Attempted && !result.Succeeded &&
            result.Restored && result.Diagnostic.Contains("interrupted") &&
            final.Director.Phase2View == ShortCyclePhase2View.Overview &&
            !final.Space.Phase2Transition && final.Stack.Count == 2,
            "last S0 transition focus cancellation cannot settle success");
        FinalCheck(finalProbe.TryRunPhase2MatrixProbe(final.Entry, final.Controller,
            final.Director, final.Space, final.Stack, prep, stove, out result) &&
            result.Succeeded && result.Restored && final.Stack.Count == 2,
            "same probe is reusable after final callback cancellation");

        var restore = Build(repoRoot, false, true, fixture =>
        {
            var gate = fixture.Sets[ShortCyclePhase2SetRole.StapleBubble]
                .gameObject.AddComponent<InterruptGate>();
            Field(fixture.Sets[ShortCyclePhase2SetRole.StapleBubble],
                "gatedInteractions", new MonoBehaviour[]{gate});
        });
        var restoreProbe = new GameObject("RestoreCallbackProbe").AddComponent<ShortCycleAutomatedProbe>();
        var stapleGate = restore.Sets[ShortCyclePhase2SetRole.StapleBubble]
            .gameObject.GetComponent<InterruptGate>();
        FinalCheck(restore.Controller.TryEnterArea(stove, out error) &&
            restore.Director.TrySetPhase2StapleBubble(true, out error),
            "restore fixture starts S1 stove with staple");
        var stapleFocuses = 0;
        Action stapleCallback = null;
        stapleCallback = () =>
        {
            stapleFocuses++;
            if (stapleFocuses == 2) restoreProbe.CancelPhase2MatrixProbe();
            else stapleGate.OnFocus = stapleCallback;
        };
        stapleGate.OnFocus = stapleCallback;
        FinalCheck(!restoreProbe.TryRunPhase2MatrixProbe(restore.Entry, restore.Controller,
            restore.Director, restore.Space, restore.Stack, prep, stove, out result) &&
            stapleFocuses == 2 && result.Attempted && !result.Succeeded &&
            result.Restored && result.Diagnostic.Contains("interrupted") &&
            restore.Director.Phase2View == ShortCyclePhase2View.AreaDetail &&
            restore.Controller.CurrentAreaId == stove && restore.Space.Phase2Staple &&
            restore.Stack.Count == 2,
            "S1 restoration callback cancellation reports failure with restored origin");
        FinalCheck(restoreProbe.TryRunPhase2MatrixProbe(restore.Entry, restore.Controller,
            restore.Director, restore.Space, restore.Stack, prep, stove, out result) &&
            result.Succeeded && result.Restored && restore.Space.Phase2Staple,
            "same probe is reusable after restoration cancellation");
        stapleFocuses = 0;
        stapleCallback = null;
        stapleCallback = () =>
        {
            stapleFocuses++;
            if (stapleFocuses == 2) restoreProbe.enabled = false;
            else stapleGate.OnFocus = stapleCallback;
        };
        stapleGate.OnFocus = stapleCallback;
        FinalCheck(!restoreProbe.TryRunPhase2MatrixProbe(restore.Entry, restore.Controller,
            restore.Director, restore.Space, restore.Stack, prep, stove, out result) &&
            stapleFocuses == 2 && !restoreProbe.enabled &&
            result.Attempted && !result.Succeeded && result.Restored &&
            result.Diagnostic.Contains("interrupted") &&
            restore.Controller.CurrentAreaId == stove && restore.Stack.Count == 2,
            "S1 restoration callback disable cannot settle success");
        restoreProbe.enabled = true;
        FinalCheck(restoreProbe.TryRunPhase2MatrixProbe(restore.Entry, restore.Controller,
            restore.Director, restore.Space, restore.Stack, prep, stove, out result) &&
            result.Succeeded && restore.Stack.Count == 2,
            "re-enabled probe remains reusable after cleanup disable");

        stapleFocuses = 0;
        stapleCallback = null;
        stapleCallback = () =>
        {
            stapleFocuses++;
            if (stapleFocuses == 2) Call(restoreProbe, "OnDestroy");
            else stapleGate.OnFocus = stapleCallback;
        };
        stapleGate.OnFocus = stapleCallback;
        FinalCheck(!restoreProbe.TryRunPhase2MatrixProbe(restore.Entry, restore.Controller,
            restore.Director, restore.Space, restore.Stack, prep, stove, out result) &&
            stapleFocuses == 2 && result.Attempted && !result.Succeeded &&
            result.Restored && result.Diagnostic.Contains("interrupted") &&
            restore.Controller.CurrentAreaId == stove && restore.Stack.Count == 2,
            "S1 restoration OnDestroy hook cannot settle success");

        var exception = Build(repoRoot, false, true, fixture =>
        {
            var gate = fixture.Sets[ShortCyclePhase2SetRole.Overview]
                .gameObject.AddComponent<InterruptGate>();
            Field(fixture.Sets[ShortCyclePhase2SetRole.Overview],
                "gatedInteractions", new MonoBehaviour[]{gate});
        });
        var exceptionProbe = new GameObject("ExceptionCallbackProbe").AddComponent<ShortCycleAutomatedProbe>();
        var exceptionGate = exception.Sets[ShortCyclePhase2SetRole.Overview]
            .gameObject.GetComponent<InterruptGate>();
        var exceptionFocuses = 0;
        Action exceptionCallback = null;
        exceptionCallback = () =>
        {
            exceptionFocuses++;
            if (exceptionFocuses == 2) throw new InvalidOperationException("injected final callback");
            exceptionGate.OnFocus = exceptionCallback;
        };
        exceptionGate.OnFocus = exceptionCallback;
        FinalCheck(!exceptionProbe.TryRunPhase2MatrixProbe(exception.Entry, exception.Controller,
            exception.Director, exception.Space, exception.Stack, prep, stove, out result) &&
            exceptionFocuses == 2 && result.Attempted && !result.Succeeded &&
            result.Diagnostic.Contains("injected final callback"),
            "final callback exception remains diagnosed");
        var running = (bool)exceptionProbe.GetType().GetField("phase2ProbeRunning",
            BindingFlags.Instance | BindingFlags.NonPublic).GetValue(exceptionProbe);
        FinalCheck(!running, "probe busy gate releases after final callback exception");
        return finalCallbackCases.Count;
    }

    public static int RunProbeCases(string repoRoot)
    {
        probeCases.Clear();
        const string prep = "kitchen-prep-counter";
        const string stove = "kitchen-stove";
        ShortCyclePhase2ProbeResult result;
        string error;
        var f = Build(repoRoot, false, true);
        var probe = new GameObject("P2MatrixProbe").AddComponent<ShortCycleAutomatedProbe>();
        ProbeCheck(f.Entry.IsReady && f.Controller.IsReady &&
            f.Director.Phase2View == ShortCyclePhase2View.Overview, "live wired S0");
        var owner = f.Inventory.Phase2Owner;
        var recipe = f.Entry.RecipeId;
        var step = f.Entry.CurrentStepProgress;
        var prepContents = f.Entry.PrepZoneContents;
        var receive = f.Entry.ReceiveCount;
        ProbeCheck(!probe.TryRunPhase2MatrixProbe(f.Entry, f.Controller, f.Director,
            f.Space, f.Stack, "missing", stove, out result) &&
            !result.Attempted && result.Restored &&
            f.Director.Phase2View == ShortCyclePhase2View.Overview &&
            f.Stack.Top == f.SpaceLayer, "missing T14 refuses unchanged");
        var wrongStack = new GameObject("WrongStack").AddComponent<ShortCycleInteractionLayerStack>();
        ProbeCheck(!probe.TryRunPhase2MatrixProbe(f.Entry, f.Controller, f.Director,
            f.Space, wrongStack, prep, stove, out result) &&
            !result.Attempted && f.Stack.Top == f.SpaceLayer, "wrong stack refuses unchanged");
        var wrongEntry = new GameObject("WrongEntry").AddComponent<Phase2SceneEntry>();
        ProbeCheck(!probe.TryRunPhase2MatrixProbe(wrongEntry, f.Controller, f.Director,
            f.Space, f.Stack, prep, stove, out result) &&
            !result.Attempted && ReferenceEquals(f.Inventory.Phase2Owner, owner),
            "wrong Entry refuses unchanged");
        Field(f.Binding, "areaDetailSet", null);
        ProbeCheck(!probe.TryRunPhase2MatrixProbe(f.Entry, f.Controller, f.Director,
            f.Space, f.Stack, prep, stove, out result) &&
            !result.Attempted && f.Stack.Count == 2,
            "missing Detail binding refuses unchanged");
        Field(f.Binding, "areaDetailSet", f.Sets[ShortCyclePhase2SetRole.AreaDetail]);
        ProbeCheck(f.Stack.Push(f.ModalLayer), "foreign layer fixture pushed");
        ProbeCheck(!probe.TryRunPhase2MatrixProbe(f.Entry, f.Controller, f.Director,
            f.Space, f.Stack, prep, stove, out result) &&
            !result.Attempted && f.Stack.Top == f.ModalLayer &&
            f.Stack.Count == 3, "unknown external layer refuses without pop");
        ProbeCheck(f.Stack.Pop(f.ModalLayer), "foreign layer fixture popped");
        ProbeCheck(f.Director.TrySetPhase2Overlay(ShortCyclePhase2Overlay.StepDetail, out error),
            "external overlay entered");
        ProbeCheck(!probe.TryRunPhase2MatrixProbe(f.Entry, f.Controller, f.Director,
            f.Space, f.Stack, prep, stove, out result) &&
            !result.Attempted && f.Space.Phase2Overlay == ShortCyclePhase2Overlay.StepDetail &&
            f.Stack.Top == f.ModalLayer, "busy overlay refuses without unwind");
        ProbeCheck(f.Director.TrySetPhase2Overlay(ShortCyclePhase2Overlay.None, out error),
            "external overlay closed");
        ProbeCheck(probe.TryRunPhase2MatrixProbe(f.Entry, f.Controller, f.Director,
            f.Space, f.Stack, prep, stove, out result) &&
            result.Attempted && result.Succeeded && result.Restored &&
            result.Checks.Count >= 100 &&
            f.Director.Phase2View == ShortCyclePhase2View.Overview &&
            f.Controller.CurrentAreaId == null && f.Stack.Top == f.SpaceLayer &&
            f.Stack.Count == 2, "public probe verifies matrix and restores S0");
        ProbeCheck(probe.TryRunPhase2MatrixProbe(f.Entry, f.Controller, f.Director,
            f.Space, f.Stack, prep, stove, out result) &&
            result.Succeeded && f.Director.Phase2View == ShortCyclePhase2View.Overview &&
            f.Stack.Count == 2, "repeat invocation restores S0");
        ProbeCheck(f.Controller.TryEnterArea(stove, out error) &&
            f.Director.TrySetPhase2StapleBubble(true, out error),
            "S1 fixture starts at stove with staple");
        ProbeCheck(probe.TryRunPhase2MatrixProbe(f.Entry, f.Controller, f.Director,
            f.Space, f.Stack, prep, stove, out result) &&
            result.Succeeded && result.Restored &&
            f.Director.Phase2View == ShortCyclePhase2View.AreaDetail &&
            f.Controller.CurrentAreaId == stove && f.Binding.AreaId == stove &&
            f.Space.Phase2Staple && f.Stack.Top == f.SpaceLayer &&
            f.Stack.Count == 2, "S1 original area, binding and staple restored");
        ProbeCheck(ReferenceEquals(f.Inventory.Phase2Owner, owner) &&
            f.Entry.RecipeId == recipe && ReferenceEquals(f.Entry.CurrentStepProgress, step) &&
            f.Entry.PrepZoneContents.Count == prepContents.Count &&
            f.Entry.ReceiveCount == receive, "business owner and snapshot unchanged");
        ProbeCheck(f.Controller.TryReturnOverview(out error), "S1 fixture returns S0");
        var interrupt = Build(repoRoot, false, true, fixture =>
        {
            var gate = fixture.Sets[ShortCyclePhase2SetRole.AreaDetail]
                .gameObject.AddComponent<InterruptGate>();
            Field(fixture.Sets[ShortCyclePhase2SetRole.AreaDetail],
                "gatedInteractions", new MonoBehaviour[]{gate});
        });
        var interruptProbe = new GameObject("InterruptProbe").AddComponent<ShortCycleAutomatedProbe>();
        var interruptGate = interrupt.Sets[ShortCyclePhase2SetRole.AreaDetail]
            .gameObject.GetComponent<InterruptGate>();
        interruptGate.OnFocus = () => interruptProbe.CancelPhase2MatrixProbe();
        ProbeCheck(!interruptProbe.TryRunPhase2MatrixProbe(interrupt.Entry, interrupt.Controller,
            interrupt.Director, interrupt.Space, interrupt.Stack, prep, stove, out result) &&
            result.Attempted && !result.Succeeded && result.Restored &&
            result.Diagnostic.Contains("interrupted") &&
            interrupt.Director.Phase2View == ShortCyclePhase2View.Overview &&
            interrupt.Stack.Count == 2, "mid-run cancellation restores S0");
        var reentry = Build(repoRoot, false, true, fixture =>
        {
            var gate = fixture.Sets[ShortCyclePhase2SetRole.AreaDetail]
                .gameObject.AddComponent<InterruptGate>();
            Field(fixture.Sets[ShortCyclePhase2SetRole.AreaDetail],
                "gatedInteractions", new MonoBehaviour[]{gate});
        });
        var reentryProbe = new GameObject("ReentryProbe").AddComponent<ShortCycleAutomatedProbe>();
        var reentryGate = reentry.Sets[ShortCyclePhase2SetRole.AreaDetail]
            .gameObject.GetComponent<InterruptGate>();
        var nestedRejected = false;
        reentryGate.OnFocus = () =>
        {
            ShortCyclePhase2ProbeResult nested;
            nestedRejected = !reentryProbe.TryRunPhase2MatrixProbe(reentry.Entry,
                reentry.Controller, reentry.Director, reentry.Space, reentry.Stack,
                prep, stove, out nested) && !nested.Attempted;
        };
        ProbeCheck(reentryProbe.TryRunPhase2MatrixProbe(reentry.Entry, reentry.Controller,
            reentry.Director, reentry.Space, reentry.Stack, prep, stove, out result) &&
            result.Succeeded && nestedRejected && reentry.Stack.Count == 2,
            "nested call refuses while outer restores");
        var foreign = Build(repoRoot, false, true, fixture =>
        {
            var gate = fixture.Sets[ShortCyclePhase2SetRole.AreaDetail]
                .gameObject.AddComponent<InterruptGate>();
            Field(fixture.Sets[ShortCyclePhase2SetRole.AreaDetail],
                "gatedInteractions", new MonoBehaviour[]{gate});
        });
        var foreignProbe = new GameObject("ForeignLayerProbe").AddComponent<ShortCycleAutomatedProbe>();
        var foreignGate = foreign.Sets[ShortCyclePhase2SetRole.AreaDetail]
            .gameObject.GetComponent<InterruptGate>();
        foreignGate.OnFocus = () => foreign.Stack.Push(foreign.ModalLayer);
        ProbeCheck(!foreignProbe.TryRunPhase2MatrixProbe(foreign.Entry, foreign.Controller,
            foreign.Director, foreign.Space, foreign.Stack, prep, stove, out result) &&
            result.Attempted && !result.Restored &&
            foreign.Stack.Top == foreign.ModalLayer && foreign.Stack.Count == 3,
            "foreign mid-run layer remains owned by external caller and reports unrestored");
        probe.enabled = false;
        ProbeCheck(!probe.TryRunPhase2MatrixProbe(f.Entry, f.Controller, f.Director,
            f.Space, f.Stack, prep, stove, out result) &&
            !result.Attempted && f.Stack.Count == 2, "disabled probe refuses unchanged");
        return probeCases.Count;
    }

    public static int Run(string repoRoot)
    {
        cases.Clear();
        var bad=Build(repoRoot,true); string error;
        Check(!bad.Controller.TryInitialize(out error) && !bad.Director.Phase2ViewActive &&
            bad.Stack.Top!=bad.SpaceLayer && error.Contains("duplicate"),
            "duplicate Set rejects before any presentation or layer change");
        var wiredBad=Build(repoRoot,true,true);
        var wired=Build(repoRoot,false,true);
        Check(wired.Controller.IsReady && wired.Director.Phase2ViewActive &&
            wired.Director.Phase2View==ShortCyclePhase2View.Overview,
            "wired Entry initializes P2 S0 only after handoff");
        wired.Entry.Teardown();
        Check(!wired.Controller.IsReady && wired.Inventory.Phase2Owner==null &&
            wired.Stack.Top!=wired.SpaceLayer,"wired Entry teardown releases area and inventory owner");
        var wrong=Build(repoRoot);
        Field(wrong.Space,"spaceId",ShortCycleSpaceId.Phase1);
        Check(!wrong.Controller.TryInitialize(out error) &&
            wrong.Stack.Top!=wrong.SpaceLayer && !wrong.Director.Phase2ViewActive,
            "wrong Space rejects before layer mutation");
        var missing=Build(repoRoot);
        Field(missing.Binding,"areaDetailSet",null);
        Check(!missing.Controller.TryInitialize(out error) &&
            missing.Stack.Top!=missing.SpaceLayer,"missing AreaDetail Set rejects atomically");
        var f=Build(repoRoot);
        Check(f.Controller.TryInitialize(out error) && f.Director.Phase2ViewActive &&
            f.Director.Generation>0,"one Director enters P2 after current handoff");
        Matrix(f,ShortCyclePhase2View.Overview,false);
        Check(!f.Director.TryEnterPhase2(out error) && f.Stack.Top==f.SpaceLayer,
            "reentry rejects without stacking another layer");
        Check(f.Director.TrySetPhase2Overlay(ShortCyclePhase2Overlay.StepDetail,out error) &&
            f.Sets[ShortCyclePhase2SetRole.Overview].IsVisibilityRequested &&
            !f.Sets[ShortCyclePhase2SetRole.Overview].IsFocused &&
            f.Sets[ShortCyclePhase2SetRole.StepDetailModal].IsFocused &&
            f.Director.TrySetPhase2Overlay(ShortCyclePhase2Overlay.None,out error) &&
            f.Director.Phase2View==ShortCyclePhase2View.Overview,
            "S0 orthogonal modal closes to exact S0");
        Check(f.Director.TrySetPhase2Transition(true,out error) &&
            f.Director.TrySetPhase2Transition(false,out error) &&
            f.Director.Phase2View==ShortCyclePhase2View.Overview,
            "S0 transition restores explicit S0");
        Check(!f.Controller.TryEnterArea("missing",out error) &&
            f.Controller.CurrentAreaId==null && f.Director.Phase2View==ShortCyclePhase2View.Overview,
            "missing T14 area refuses atomically");
        Check(!f.Controller.TryEnterArea("kitchen-oven",out error) &&
            f.Controller.CurrentAreaId==null && f.Director.Phase2View==ShortCyclePhase2View.Overview,
            "locked T14 area refuses atomically");
        Check(f.Controller.TryEnterArea("kitchen-prep-counter",out error) &&
            f.Binding.AreaId=="kitchen-prep-counter" && f.Controller.CurrentAreaId=="kitchen-prep-counter",
            "one Detail binding navigates to prep");
        Matrix(f,ShortCyclePhase2View.AreaDetail,false);
        Check(f.Director.TrySetPhase2StapleBubble(true,out error),"explicit staple condition accepted");
        Matrix(f,ShortCyclePhase2View.AreaDetail,true);
        Check(f.Controller.TryEnterArea("kitchen-stove",out error) &&
            f.Binding.AreaId=="kitchen-stove" && f.Controller.CurrentAreaId=="kitchen-stove",
            "same Detail instance rebinds to stove");
        Check(!f.Sets[ShortCyclePhase2SetRole.StapleBubble].IsVisibilityRequested,
            "area swap clears old area's staple condition");
        Check(f.Director.TrySetPhase2Overlay(ShortCyclePhase2Overlay.StepDetail,out error) &&
            f.Stack.Top==f.ModalLayer &&
            f.Sets[ShortCyclePhase2SetRole.StepDetailModal].IsVisibilityRequested &&
            !f.Sets[ShortCyclePhase2SetRole.AreaDetail].IsFocused &&
            f.Sets[ShortCyclePhase2SetRole.AreaDetail].IsVisibilityRequested,
            "orthogonal StepDetail preserves base visuals and blocks input");
        Check(!f.Controller.TryReturnOverview(out error) && f.Controller.CurrentAreaId=="kitchen-stove",
            "orthogonal modal blocks regular back");
        Check(f.Director.TrySetPhase2Overlay(ShortCyclePhase2Overlay.None,out error) &&
            f.Director.Phase2View==ShortCyclePhase2View.AreaDetail &&
            f.Sets[ShortCyclePhase2SetRole.AreaDetail].IsFocused,
            "closing orthogonal modal restores exact AreaDetail focus");
        Check(f.Director.TrySetPhase2Overlay(ShortCyclePhase2Overlay.ExitConfirm,out error) &&
            f.Sets[ShortCyclePhase2SetRole.ExitConfirmModal].IsFocused,
            "ExitConfirm uses same modal input layer");
        Check(f.Director.TrySetPhase2Overlay(ShortCyclePhase2Overlay.None,out error),
            "ExitConfirm closes to AreaDetail");
        Check(f.Director.TrySetPhase2View(ShortCyclePhase2View.Interaction,out error),
            "regular processing modal opens");
        Matrix(f,ShortCyclePhase2View.Interaction,false);
        Check(!f.Director.TrySetPhase2Overlay(ShortCyclePhase2Overlay.ExitConfirm,out error) &&
            f.Director.Phase2View==ShortCyclePhase2View.Interaction,
            "ESC confirmation cannot interrupt processing");
        Check(!f.Controller.TryReturnOverview(out error) && !f.Controller.TryEnterArea("kitchen-prep-counter",out error) &&
            f.Controller.CurrentAreaId=="kitchen-stove","processing modal blocks back and area swap");
        Check(f.Director.TrySetPhase2View(ShortCyclePhase2View.AreaDetail,out error),
            "processing completion restores AreaDetail");
        Check(f.Director.TrySetPhase2View(ShortCyclePhase2View.MiniGame,out error),
            "MG0 modal opens");
        Matrix(f,ShortCyclePhase2View.MiniGame,false);
        Check(f.Director.TrySetPhase2View(ShortCyclePhase2View.AreaDetail,out error),
            "MG0 completion restores AreaDetail");
        var prior=f.Director.Phase2View;
        f.Rows[1].set=f.Rows[0].set;
        Check(!f.Director.TrySetPhase2View(ShortCyclePhase2View.Overview,out error) &&
            f.Director.Phase2View==prior && f.Stack.Top==f.SpaceLayer,
            "runtime-invalid viewTable rejects without partial navigation");
        f.Rows[1].set=f.Sets[ShortCyclePhase2SetRole.Overview];
        Check(f.Director.TrySetPhase2Transition(true,out error) &&
            f.Stack.Top==f.ModalLayer && !f.Sets[ShortCyclePhase2SetRole.TopBar].IsVisibilityRequested,
            "transition masks visuals and blocks input");
        Check(!f.Controller.TryReturnOverview(out error) &&
            f.Director.TrySetPhase2Transition(false,out error) &&
            f.Director.Phase2View==ShortCyclePhase2View.AreaDetail,
            "transition refuses navigation then restores explicit base state");
        Check(f.Controller.TryReturnOverview(out error) && !f.Binding.IsBound &&
            f.Controller.CurrentAreaId==null,"return clears single Detail binding");
        Matrix(f,ShortCyclePhase2View.Overview,false);
        f.Controller.Teardown();
        Check(!f.Director.Phase2ViewActive && !f.Binding.IsBound &&
            f.Stack.Top!=f.SpaceLayer,"teardown releases view, binding and transient input");
        return cases.Count;
    }
}
