using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EatWhat.Cooking.ShortCycle
{
    public enum ShortCycleScrollTraceMode
    {
        RealInput = 0,
        Simulated = 1
    }

    public sealed class ShortCycleScrollTraceFacts
    {
        public bool TraceEnabled;
        public bool HasMouse;
        public bool IsFocused;
        public bool HasMouseManager;
        public bool MouseManagerActive;
        public bool UnsafeEmptyLayerUpdatePath;
        public int FrameworkLayerCount;
        public int SelectedMaskIndex = -1;
        public int SelectedMaskHitCount;
        public bool ScrollRegionCandidate;
        public bool BlockedByPressable;
        public bool PressablePassThroughApplied;
        public bool FrameworkTargetMatches;
        public float RawScroll;
        public float AccumulatedScroll;
        public float Threshold;
        public float CooldownRemaining;
        public int MouseStepCount;
        public int TargetEventCount;
        public bool HasAdapter;
        public bool AdapterActive;
        public bool AdapterWiredToTarget;
        public bool AdapterSubscribed;
        public int AdapterReceivedCount;
        public int AdapterDispatchCount;
        public ShortCyclePhase Phase;
        public int TierCount;
        public int CurrentTier;
        public ShortCycleActionResult LastAttemptResult;
        public ShortCycleActionResult LastDispatchResult;
    }

    public sealed class ShortCycleScrollTraceDecision
    {
        public string LastPassedLayer;
        public string BlockedAt;
        public string Reason;

        public override string ToString()
        {
            return "lastPassed=" + LastPassedLayer + "; blockedAt=" + BlockedAt + "; reason=" + Reason;
        }
    }

    /// <summary>Pure classification used by both the runtime probe and editor-external tests.</summary>
    public static class ShortCycleScrollTraceClassifier
    {
        public static ShortCycleScrollTraceDecision Evaluate(ShortCycleScrollTraceFacts facts)
        {
            if (facts == null) return Block("none", "probe", "facts_missing");
            if (!facts.TraceEnabled) return Block("none", "probe", "trace_disabled");
            if (!facts.HasMouse) return Block("probe", "device", "mouse_missing");
            if (!facts.IsFocused) return Block("device", "focus", "application_not_focused");
            if (!facts.HasMouseManager) return Block("focus", "interaction", "mouse_manager_missing");
            if (!facts.MouseManagerActive) return Block("interaction", "interaction", "mouse_manager_inactive");
            if (facts.UnsafeEmptyLayerUpdatePath)
                return Block("interaction", "layer", "empty_layer_stack_update_risk");
            if (facts.FrameworkLayerCount == 0)
                return Block("interaction", "layer", "interaction_layer_stack_empty");
            if (facts.SelectedMaskIndex < 0 || facts.SelectedMaskHitCount == 0)
                return Block("layer", "hit", "no_collider_in_allowed_masks");
            if (!facts.ScrollRegionCandidate)
                return Block("hit", "hit", "scroll_region_not_in_first_nonempty_mask");
            if (facts.BlockedByPressable && !facts.PressablePassThroughApplied)
                return Block("hit", "blocking", "higher_priority_pressable");
            if (!facts.FrameworkTargetMatches)
                return Block("blocking", "hit", "framework_selected_other_or_null_scrollable");
            if (facts.TierCount <= 1)
                return Block("hit", "boundary", "tier_count_not_scrollable");

            var sawRaw = !Mathf.Approximately(facts.RawScroll, 0f);
            if (!sawRaw && facts.MouseStepCount == 0 && facts.TargetEventCount == 0)
            {
                if (!Mathf.Approximately(facts.AccumulatedScroll, 0f))
                    return Block("device", "event", "below_scroll_threshold");
                return Block("hit", "device", "ready_waiting_for_real_wheel_input");
            }
            if (facts.MouseStepCount == 0)
            {
                if (facts.CooldownRemaining > 0f) return Block("device", "event", "scroll_cooldown");
                if (Mathf.Abs(facts.AccumulatedScroll) < facts.Threshold)
                    return Block("device", "event", "below_scroll_threshold");
                return Block("device", "event", "step_not_emitted");
            }
            if (facts.TargetEventCount < facts.MouseStepCount)
                return Block("event", "event", "target_scroll_event_not_received");
            if (!facts.HasAdapter) return Block("event", "adapter", "adapter_missing");
            if (!facts.AdapterActive) return Block("event", "adapter", "adapter_inactive");
            if (!facts.AdapterWiredToTarget) return Block("event", "adapter", "adapter_target_mismatch");
            if (!facts.AdapterSubscribed) return Block("event", "adapter", "adapter_unsubscribed");
            if (facts.AdapterReceivedCount < facts.TargetEventCount)
                return Block("event", "adapter", "adapter_did_not_receive_step");
            if (facts.Phase != ShortCyclePhase.Phase1)
                return Block("adapter", "gate", "phase1_required");
            if (facts.LastAttemptResult != null && !facts.LastAttemptResult.Succeeded)
            {
                var gate = facts.LastAttemptResult.ErrorCode == ShortCycleActionErrorCode.Locked;
                var boundary = facts.LastAttemptResult.ErrorCode == ShortCycleActionErrorCode.Rejected &&
                    facts.AdapterDispatchCount > 0;
                return Block("adapter", gate ? "gate" : (boundary ? "boundary" : "routing"),
                    ResultReason(facts.LastAttemptResult));
            }
            if (facts.AdapterDispatchCount == 0)
                return Block("adapter", "routing", "adapter_not_dispatched");
            if (facts.LastDispatchResult == null)
                return Block("adapter", "routing", "dispatch_result_missing");
            if (!facts.LastDispatchResult.Succeeded)
            {
                var boundary = facts.LastDispatchResult.ErrorCode == ShortCycleActionErrorCode.Rejected;
                return Block("routing", boundary ? "boundary" : "routing", ResultReason(facts.LastDispatchResult));
            }
            return Block("presentation", "none", "routed_success_observe_transition");
        }

        private static ShortCycleScrollTraceDecision Block(string lastPassed, string blockedAt, string reason)
        {
            return new ShortCycleScrollTraceDecision
            {
                LastPassedLayer = lastPassed,
                BlockedAt = blockedAt,
                Reason = reason
            };
        }

        private static string ResultReason(ShortCycleActionResult result)
        {
            return result.ErrorCode + ":" + (result.Error ?? string.Empty);
        }
    }

    /// <summary>
    /// Passive real-wheel trace for the Phase1 fridge. This component only samples state and
    /// subscribes to existing notifications; it never invokes the scroll event or routes an action.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Cooking/Short Cycle/Debug/Fridge Scroll Trace Probe")]
    public sealed class ShortCycleFridgeScrollTraceProbe : MonoBehaviour
    {
        private const string Prefix = "[CK01-F-SCROLL-TRACE]";

        [Header("Manual opt-in")]
        [SerializeField] private bool traceEnabled = false;
        [SerializeField] private bool logChangesToConsole = true;
        [SerializeField, Min(8)] private int bufferCapacity = 64;
        [SerializeField, Min(0.05f)] private float consoleThrottleSeconds = 0.25f;
        [SerializeField] private ShortCycleScrollTraceMode captureMode = ShortCycleScrollTraceMode.RealInput;
        [SerializeField] private string exportFolderName = "AI-000075";

        [Header("Existing input and layer framework")]
        [SerializeField] private InputManager inputManager = null;
        [SerializeField] private MouseManager mouseManager = null;
        [SerializeField] private Camera expectedMainCamera = null;
        [SerializeField] private ShortCycleInteractionLayerStack shortCycleLayerStack = null;
        [SerializeField] private MouseInteractionLayer phase0Layer = null;
        [SerializeField] private MouseInteractionLayer phase1Layer = null;
        [SerializeField] private MouseInteractionLayer[] modalLayers = new MouseInteractionLayer[0];

        [Header("Existing fridge scroll chain")]
        [SerializeField] private MouseScrollableObject scrollRegion = null;
        [SerializeField] private ShortCycleScrollAdapter scrollAdapter = null;
        [SerializeField] private ShortCycleSessionManager sessionManager = null;
        [SerializeField] private ShortCycleFridgeBinder fridgeBinder = null;
        [SerializeField] private ShortCyclePresentationSet fridgePresentationSet = null;
        [SerializeField] private ShortCycleInputLockController inputLockController = null;
        [SerializeField] private FridgeCatRig fridgeCatRig = null;
        [SerializeField] private ScrollArea_Controller scrollArea = null;
        [SerializeField] private TextMeshPro statusBoard = null;

        private readonly Queue<string> entries = new Queue<string>();
        private bool listenersAttached;
        private string lastStateKey;
        private float lastConsoleTime = float.NegativeInfinity;
        private int mouseStepCount;
        private int targetEventCount;
        private int rawScrollSampleCount;
        private int adapterReceivedBaseline;
        private int adapterDispatchBaseline;
        private int captureSessionId;
        private float latestRawScroll;
        private float lastNonzeroRawScroll;
        private float lastNonzeroRawTime = float.NegativeInfinity;
        private int lastNonzeroRawFrame = -1;
        private int previousSnapshotMouseSteps;
        private int previousSnapshotTargetEvents;
        private int previousSnapshotAdapterReceives;
        private int previousSnapshotAdapterDispatches;
        private int latestMouseStepDelta;
        private int latestTargetEventDelta;
        private int latestAdapterReceiveDelta;
        private int latestAdapterDispatchDelta;

        public bool TraceEnabled { get { return traceEnabled; } }
        public int BufferedEntryCount { get { return entries.Count; } }
        public int MouseStepCount { get { return mouseStepCount; } }
        public int TargetEventCount { get { return targetEventCount; } }
        public int RawScrollSampleCount { get { return rawScrollSampleCount; } }
        public int CaptureSessionId { get { return captureSessionId; } }
        public ShortCycleScrollTraceMode CaptureMode { get { return captureMode; } }
        public float LatestRawScroll { get { return latestRawScroll; } }
        public float LastNonzeroRawScroll { get { return lastNonzeroRawScroll; } }
        public float LastNonzeroRawTime { get { return lastNonzeroRawTime; } }
        public int LastNonzeroRawFrame { get { return lastNonzeroRawFrame; } }
        public int LatestMouseStepDelta { get { return latestMouseStepDelta; } }
        public int LatestTargetEventDelta { get { return latestTargetEventDelta; } }
        public int LatestAdapterReceiveDelta { get { return latestAdapterReceiveDelta; } }
        public int LatestAdapterDispatchDelta { get { return latestAdapterDispatchDelta; } }
        public MouseManager MouseManager { get { return mouseManager; } }
        public Camera ExpectedMainCamera { get { return expectedMainCamera; } }
        public MouseScrollableObject ScrollRegion { get { return scrollRegion; } }
        public ShortCycleScrollAdapter ScrollAdapter { get { return scrollAdapter; } }
        public ShortCycleSessionManager SessionManager { get { return sessionManager; } }
        public ShortCycleFridgeBinder FridgeBinder { get { return fridgeBinder; } }
        public FridgeCatRig FridgeCatRig { get { return fridgeCatRig; } }
        public ScrollArea_Controller ScrollArea { get { return scrollArea; } }
        public string LatestSnapshot { get; private set; }
        public string LatestConclusion { get; private set; }

        private void Reset()
        {
            traceEnabled = false;
            logChangesToConsole = true;
            bufferCapacity = 64;
            consoleThrottleSeconds = 0.25f;
            captureMode = ShortCycleScrollTraceMode.RealInput;
            exportFolderName = "AI-000075";
        }

        private void OnEnable()
        {
            if (traceEnabled) AttachListeners();
            else UpdateStatusBoard(Prefix + " disabled (manual opt-in required)");
        }

        private void OnDisable()
        {
            DetachListeners();
        }

        private void OnDestroy()
        {
            DetachListeners();
        }

        private void LateUpdate()
        {
            if (traceEnabled != listenersAttached)
            {
                if (traceEnabled) AttachListeners();
                else DetachListeners();
            }
            if (!traceEnabled) return;

            float rawScroll;
            Vector2 screenPoint;
            var mouse = Mouse.current;
            if (mouse == null)
            {
                rawScroll = 0f;
                screenPoint = Vector2.zero;
            }
            else
            {
                rawScroll = mouse.scroll.ReadValue().y;
                screenPoint = mouse.position.ReadValue();
            }
            ObserveRawScroll(rawScroll);

            var snapshot = BuildSnapshot(rawScroll, screenPoint);
            LatestSnapshot = snapshot.Text;
            LatestConclusion = snapshot.Decision.ToString();
            UpdateStatusBoard(Prefix + "\n" + LatestConclusion + "\n" + snapshot.CompactStatus);

            var stateChanged = snapshot.StateKey != lastStateKey;
            var wheelChanged = !Mathf.Approximately(rawScroll, 0f);
            if (stateChanged || wheelChanged)
            {
                lastStateKey = snapshot.StateKey;
                AppendEntry(snapshot.Text);
            }
        }

        public void SetTraceEnabled(bool value)
        {
            traceEnabled = value;
            if (value) AttachListeners();
            else
            {
                DetachListeners();
                UpdateStatusBoard(Prefix + " disabled (manual opt-in required)");
            }
        }

        public void BeginCaptureSession(ShortCycleScrollTraceMode mode)
        {
            captureMode = mode;
            ResetCaptureSession();
        }

        [ContextMenu("CK01-F Scroll Trace/Reset Capture Session")]
        public void ResetCaptureSession()
        {
            captureSessionId++;
            entries.Clear();
            lastStateKey = null;
            lastConsoleTime = float.NegativeInfinity;
            mouseStepCount = 0;
            targetEventCount = 0;
            rawScrollSampleCount = 0;
            latestRawScroll = 0f;
            lastNonzeroRawScroll = 0f;
            lastNonzeroRawTime = float.NegativeInfinity;
            lastNonzeroRawFrame = -1;
            adapterReceivedBaseline = scrollAdapter == null ? 0 : scrollAdapter.ReceivedStepCount;
            adapterDispatchBaseline = scrollAdapter == null ? 0 : scrollAdapter.DispatchCount;
            previousSnapshotMouseSteps = 0;
            previousSnapshotTargetEvents = 0;
            previousSnapshotAdapterReceives = 0;
            previousSnapshotAdapterDispatches = 0;
            latestMouseStepDelta = 0;
            latestTargetEventDelta = 0;
            latestAdapterReceiveDelta = 0;
            latestAdapterDispatchDelta = 0;
            LatestSnapshot = null;
            LatestConclusion = null;
            UpdateStatusBoard(Prefix + " session=" + captureSessionId + " mode=" + captureMode + " reset");
        }

        [ContextMenu("CK01-F Scroll Trace/Capture Snapshot")]
        public void CaptureSnapshot()
        {
            var mouse = Mouse.current;
            var raw = mouse == null ? 0f : mouse.scroll.ReadValue().y;
            var screen = mouse == null ? Vector2.zero : mouse.position.ReadValue();
            ObserveRawScroll(raw);
            var snapshot = BuildSnapshot(raw, screen);
            LatestSnapshot = snapshot.Text;
            LatestConclusion = snapshot.Decision.ToString();
            AppendEntry(snapshot.Text);
        }

        [ContextMenu("CK01-F Scroll Trace/Export Snapshot")]
        public void ExportSnapshot()
        {
            CaptureSnapshot();
            try
            {
                var assetsDirectory = new DirectoryInfo(Application.dataPath);
                var projectDirectory = assetsDirectory.Parent;
                if (projectDirectory == null) throw new InvalidOperationException("Project root cannot be resolved from Application.dataPath.");
                var folder = string.IsNullOrEmpty(exportFolderName) ? "AI-000075" : exportFolderName;
                var outputDirectory = Path.Combine(projectDirectory.FullName, "Captures", folder);
                Directory.CreateDirectory(outputDirectory);
                var path = Path.Combine(outputDirectory,
                    "fridge-scroll-trace-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".txt");
                File.WriteAllText(path, BuildExportText(), new UTF8Encoding(false));
                Debug.Log(Prefix + " exported=" + path, this);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(Prefix + " export_failed=" + exception.GetType().Name + ":" + exception.Message, this);
            }
        }

        private void AttachListeners()
        {
            if (listenersAttached) return;
            MouseManager.OnScrollStep -= HandleMouseStep;
            MouseManager.OnScrollStep += HandleMouseStep;
            if (scrollRegion != null && scrollRegion.scrollStepEvent != null)
            {
                scrollRegion.scrollStepEvent.RemoveListener(HandleTargetStep);
                scrollRegion.scrollStepEvent.AddListener(HandleTargetStep);
            }
            adapterReceivedBaseline = scrollAdapter == null ? 0 : scrollAdapter.ReceivedStepCount;
            adapterDispatchBaseline = scrollAdapter == null ? 0 : scrollAdapter.DispatchCount;
            listenersAttached = true;
        }

        private void DetachListeners()
        {
            MouseManager.OnScrollStep -= HandleMouseStep;
            if (scrollRegion != null && scrollRegion.scrollStepEvent != null)
                scrollRegion.scrollStepEvent.RemoveListener(HandleTargetStep);
            listenersAttached = false;
        }

        private void HandleMouseStep(float delta)
        {
            mouseStepCount++;
            AppendEntry(Prefix + " event=MouseManager.OnScrollStep delta=" + delta + " frame=" + Time.frameCount);
        }

        private void HandleTargetStep(float delta)
        {
            targetEventCount++;
            AppendEntry(Prefix + " event=ScrollRegion.scrollStepEvent delta=" + delta + " frame=" + Time.frameCount);
        }

        private void ObserveRawScroll(float rawScroll)
        {
            latestRawScroll = rawScroll;
            if (Mathf.Approximately(rawScroll, 0f)) return;
            rawScrollSampleCount++;
            lastNonzeroRawScroll = rawScroll;
            lastNonzeroRawTime = Time.unscaledTime;
            lastNonzeroRawFrame = Time.frameCount;
        }

        private TraceSnapshot BuildSnapshot(float rawScroll, Vector2 screenPoint)
        {
            var actualCamera = Camera.main;
            var hasWorldPoint = actualCamera != null;
            var worldPoint = hasWorldPoint
                ? actualCamera.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, actualCamera.nearClipPlane))
                : Vector3.zero;
            if (hasWorldPoint) worldPoint.z = 0f;

            var hit = InspectHits(worldPoint, hasWorldPoint);
            var managerExists = mouseManager != null;
            var managerActive = managerExists && mouseManager.isActiveAndEnabled;
            var layerCount = managerExists && mouseManager.mouseInteractiveLayers != null
                ? mouseManager.mouseInteractiveLayers.Count : 0;
            var threshold = managerExists ? mouseManager.ScrollThreshold : 0f;
            var accumulated = managerExists ? mouseManager.AccumulatedScroll : 0f;
            var cooldownRemaining = managerExists
                ? Mathf.Max(0f, mouseManager.ScrollCooldown - (Time.time - mouseManager.LastScrollTime)) : 0f;
            var rig = fridgeCatRig != null ? fridgeCatRig : (fridgeBinder == null ? null : fridgeBinder.FridgeCatRig);
            var area = scrollArea != null ? scrollArea : (rig == null ? null : rig.ScrollArea);
            var phase = sessionManager == null ? ShortCyclePhase.None : sessionManager.CurrentPhase;
            var adapterReceived = scrollAdapter == null ? 0 : Math.Max(0, scrollAdapter.ReceivedStepCount - adapterReceivedBaseline);
            var adapterDispatch = scrollAdapter == null ? 0 : Math.Max(0, scrollAdapter.DispatchCount - adapterDispatchBaseline);
            latestMouseStepDelta = Math.Max(0, mouseStepCount - previousSnapshotMouseSteps);
            latestTargetEventDelta = Math.Max(0, targetEventCount - previousSnapshotTargetEvents);
            latestAdapterReceiveDelta = Math.Max(0, adapterReceived - previousSnapshotAdapterReceives);
            latestAdapterDispatchDelta = Math.Max(0, adapterDispatch - previousSnapshotAdapterDispatches);
            previousSnapshotMouseSteps = mouseStepCount;
            previousSnapshotTargetEvents = targetEventCount;
            previousSnapshotAdapterReceives = adapterReceived;
            previousSnapshotAdapterDispatches = adapterDispatch;

            var facts = new ShortCycleScrollTraceFacts
            {
                TraceEnabled = traceEnabled,
                HasMouse = Mouse.current != null,
                IsFocused = Application.isFocused,
                HasMouseManager = managerExists,
                MouseManagerActive = managerActive,
                UnsafeEmptyLayerUpdatePath = managerExists && mouseManager.HasUnsafeEmptyLayerUpdatePath,
                FrameworkLayerCount = layerCount,
                SelectedMaskIndex = hit.SelectedMaskIndex,
                SelectedMaskHitCount = hit.SelectedMaskHitCount,
                ScrollRegionCandidate = hit.ScrollRegionCandidate,
                BlockedByPressable = hit.BlockedByPressable,
                PressablePassThroughApplied = managerExists &&
                    mouseManager.LastScrollResolutionReason == "pressable_pass_through",
                FrameworkTargetMatches = managerExists && mouseManager.currentScrollableObject == scrollRegion,
                RawScroll = rawScroll,
                AccumulatedScroll = accumulated,
                Threshold = threshold,
                CooldownRemaining = cooldownRemaining,
                MouseStepCount = mouseStepCount,
                TargetEventCount = targetEventCount,
                HasAdapter = scrollAdapter != null,
                AdapterActive = scrollAdapter != null && scrollAdapter.isActiveAndEnabled,
                AdapterWiredToTarget = scrollAdapter != null && scrollAdapter.ScrollableObject == scrollRegion,
                AdapterSubscribed = scrollAdapter != null && scrollAdapter.IsSubscribed,
                AdapterReceivedCount = adapterReceived,
                AdapterDispatchCount = adapterDispatch,
                Phase = phase,
                TierCount = rig == null ? 0 : rig.TierCount,
                CurrentTier = fridgeBinder == null ? -1 : fridgeBinder.CurrentTier,
                LastAttemptResult = scrollAdapter == null ? null : scrollAdapter.LastAttemptResult,
                LastDispatchResult = scrollAdapter == null ? null : scrollAdapter.LastResult
            };
            var decision = ShortCycleScrollTraceClassifier.Evaluate(facts);
            var text = new StringBuilder();
            var scenePath = gameObject.scene.path;
            text.Append(Prefix).Append(" session=").Append(captureSessionId)
                .Append(" mode=").Append(captureMode)
                .Append(" scene=").Append(string.IsNullOrEmpty(scenePath) ? gameObject.scene.name : scenePath)
                .Append(" frame=").Append(Time.frameCount)
                .Append(" unscaledTime=").Append(Time.unscaledTime.ToString("0.000"))
                .Append(" focus=").Append(Application.isFocused)
                .Append(" timeScale=").Append(Time.timeScale).AppendLine();
            text.Append("device mouse=").Append(Mouse.current != null)
                .Append(" rawY=").Append(rawScroll.ToString("0.###"))
                .Append(" screen=").Append(screenPoint)
                .Append(" mainCamera=").Append(NameOf(actualCamera))
                .Append(" expectedMainCameraMatches=").Append(expectedMainCamera == null || expectedMainCamera == actualCamera)
                .Append(" world=").Append(hasWorldPoint ? worldPoint.ToString() : "unavailable").AppendLine();
            text.Append("framework inputManager=").Append(StateOf(inputManager))
                .Append(" mouseManager=").Append(StateOf(mouseManager))
                .Append(" unsafeEmptyPeek=").Append(facts.UnsafeEmptyLayerUpdatePath)
                .Append(" dragStarted=").Append(managerExists && mouseManager.dragStarted)
                .Append(" dragPerforming=").Append(managerExists && mouseManager.dragPerforming)
                .Append(" pressableHover=").Append(NameOf(managerExists ? mouseManager.currentPressableHoverTarget : null))
                .Append(" frameworkLayers=").Append(DescribeFrameworkLayers())
                .Append(" localStack=").Append(DescribeLocalStack()).AppendLine();
            text.Append("hit selectedMask=").Append(hit.SelectedMaskIndex)
                .Append(" firstMaskHits=").Append(hit.SelectedMaskHitCount)
                .Append(" resolveFrame=").Append(managerExists ? mouseManager.LastScrollResolutionFrame : -1)
                .Append(" candidate=").Append(NameOf(managerExists ? mouseManager.LastScrollableCandidate : null))
                .Append(" currentScrollable=").Append(NameOf(managerExists ? mouseManager.currentScrollableObject : null))
                .Append(" blocker=").Append(NameOf(managerExists ? mouseManager.LastScrollableBlocker : null))
                .Append(" reason=").Append(managerExists ? mouseManager.LastScrollResolutionReason : "manager_missing")
                .Append(" candidates=").Append(hit.Description).AppendLine();
            text.Append("event threshold=").Append(threshold.ToString("0.###"))
                .Append(" accumulated=").Append(accumulated.ToString("0.###"))
                .Append(" cooldownRemaining=").Append(cooldownRemaining.ToString("0.###"))
                .Append(" managerSteps=").Append(mouseStepCount)
                .Append(" managerFrameDelta=").Append(latestMouseStepDelta)
                .Append(" targetReceives=").Append(targetEventCount)
                .Append(" targetFrameDelta=").Append(latestTargetEventDelta)
                .Append(" dispatchSequence=").Append(managerExists ? mouseManager.ScrollDispatchSequence : 0)
                .Append(" dispatchFrame=").Append(managerExists ? mouseManager.LastScrollDispatchFrame : -1)
                .Append(" managerRaw=").Append(managerExists ? mouseManager.LastRawScrollY.ToString("0.###") : "missing")
                .Append(" managerStep=").Append(managerExists ? mouseManager.LastDispatchedStep.ToString("0.###") : "missing")
                .Append(" managerWorld=").Append(managerExists ? mouseManager.LastScrollPointerWorld.ToString() : "missing")
                .Append(" persistentListeners=").Append(scrollRegion == null || scrollRegion.scrollStepEvent == null ? -1 : scrollRegion.scrollStepEvent.GetPersistentEventCount())
                .AppendLine();
            text.Append("adapter state=").Append(StateOf(scrollAdapter))
                .Append(" wired=").Append(facts.AdapterWiredToTarget)
                .Append(" subscribed=").Append(facts.AdapterSubscribed)
                .Append(" receives=").Append(adapterReceived)
                .Append(" receiveFrameDelta=").Append(latestAdapterReceiveDelta)
                .Append(" dispatches=").Append(adapterDispatch)
                .Append(" dispatchFrameDelta=").Append(latestAdapterDispatchDelta)
                .Append(" invertPhysical=").Append(scrollAdapter != null && scrollAdapter.InvertPhysicalScrollDirection)
                .Append(" physicalDelta=").Append(scrollAdapter == null ? 0f : scrollAdapter.LastReceivedDelta)
                .Append(" mappedDelta=").Append(scrollAdapter == null ? 0f : scrollAdapter.LastMappedDelta)
                .Append(" attempt=").Append(ResultOf(facts.LastAttemptResult))
                .Append(" result=").Append(ResultOf(facts.LastDispatchResult)).AppendLine();
            text.Append("presentation phase=").Append(phase)
                .Append(" modal=").Append(sessionManager == null ? "missing" : sessionManager.ActiveModal.ToString())
                .Append(" fridgeFocused=").Append(fridgePresentationSet == null ? "missing" : fridgePresentationSet.IsFocused.ToString())
                .Append(" inputLocked=").Append(inputLockController == null ? "missing" : inputLockController.Gate.IsLocked.ToString())
                .Append(" tier=").Append(facts.CurrentTier).Append('/').Append(facts.TierCount)
                .Append(" requestedTier=").Append(scrollAdapter == null || scrollAdapter.LastReceivedDelta == 0f
                    ? -1 : facts.CurrentTier + (scrollAdapter.LastReceivedDelta < 0f ? -1 : 1))
                .Append(" content=").Append(area == null || area.ContentTransform == null ? "missing" : area.ContentTransform.position.ToString())
                .Append(" target=").Append(area == null || !area.HasLastTargetPosition ? "unset" : area.LastTargetPosition.ToString())
                .Append(" normalized=").Append(area == null ? "missing" : area.NormalizedContentPosition.ToString())
                .Append(" transitioning=").Append(area != null && area.IsProgrammaticTransitionActive)
                .Append(" stable=").Append(area != null && !area.IsProgrammaticTransitionActive &&
                    area.ContentTransform != null && (!area.HasLastTargetPosition ||
                    Vector3.Distance(area.ContentTransform.position, area.LastTargetPosition) < 0.001f)).AppendLine();
            text.Append("conclusion ").Append(decision);
            var contentState = area == null || area.ContentTransform == null ? "missing" : area.ContentTransform.position.ToString();
            var targetState = area == null || !area.HasLastTargetPosition ? "unset" : area.LastTargetPosition.ToString();
            var transitionState = area != null && area.IsProgrammaticTransitionActive;
            var stateKey = captureSessionId + "|" + captureMode + "|" + decision + "|" + rawScroll + "|" +
                mouseStepCount + "|" + targetEventCount + "|" + adapterReceived + "|" + adapterDispatch + "|" +
                facts.CurrentTier + "|" + facts.TierCount + "|" + contentState + "|" + targetState + "|" + transitionState;
            return new TraceSnapshot
            {
                Text = text.ToString(),
                StateKey = stateKey,
                CompactStatus = "session=" + captureSessionId + " mode=" + captureMode +
                    " raw=" + rawScroll.ToString("0.###") + " lastRaw=" + lastNonzeroRawScroll.ToString("0.###") +
                    "@" + lastNonzeroRawFrame + " steps=" + mouseStepCount +
                    " event=" + targetEventCount + " adapter=" + adapterReceived + "/" + adapterDispatch +
                    " tier=" + facts.CurrentTier + "/" + facts.TierCount,
                Decision = decision
            };
        }

        private HitInspection InspectHits(Vector3 worldPoint, bool hasWorldPoint)
        {
            var result = new HitInspection { SelectedMaskIndex = -1, Description = "none" };
            if (!hasWorldPoint || mouseManager == null || mouseManager.currentMouseLayer == null ||
                mouseManager.currentMouseLayer.alloweInteractionLayers == null)
                return result;

            var descriptions = new List<string>();
            Collider2D[] selectedHits = null;
            var masks = mouseManager.currentMouseLayer.alloweInteractionLayers;
            for (var index = 0; index < masks.Count; index++)
            {
                var hits = Physics2D.OverlapPointAll(worldPoint, masks[index]);
                descriptions.Add("mask[" + index + "]=" + masks[index].value + " hits=" + hits.Length);
                if (selectedHits == null && hits.Length > 0)
                {
                    selectedHits = hits;
                    result.SelectedMaskIndex = index;
                    result.SelectedMaskHitCount = hits.Length;
                }
            }
            if (selectedHits == null)
            {
                result.Description = string.Join(" | ", descriptions.ToArray());
                return result;
            }

            var scrollCandidates = new List<Collider2D>();
            foreach (var collider in selectedHits)
            {
                var candidateScrollable = collider == null ? null : collider.gameObject.GetComponent<MouseScrollableObject>();
                if (candidateScrollable != null) scrollCandidates.Add(collider);
                var renderer = collider == null ? null : collider.gameObject.GetComponent<SpriteRenderer>();
                descriptions.Add(collider == null ? "null-collider" :
                    PathOf(collider.transform) + " active=" + collider.gameObject.activeInHierarchy +
                    " enabled=" + collider.enabled + " layer=" + collider.gameObject.layer +
                    " sorting=" + (renderer == null ? "none" : renderer.sortingLayerID + "/" + renderer.sortingOrder) +
                    " scrollable=" + (candidateScrollable != null) +
                    " pressable=" + (collider.gameObject.GetComponent<MousePressableObject>() != null));
                if (candidateScrollable == scrollRegion) result.ScrollRegionCandidate = true;
            }
            var selectedScrollableCollider = SelectHighestPriority(scrollCandidates);
            var regionCollider = FindColliderFor(scrollRegion, selectedHits);
            result.BlockedByPressable = IsBlockedByPressable(selectedHits, regionCollider);
            descriptions.Add("recomputedScrollable=" + NameOf(selectedScrollableCollider == null ? null :
                selectedScrollableCollider.gameObject.GetComponent<MouseScrollableObject>()) +
                " blockedByPressable=" + result.BlockedByPressable +
                " (observation/recompute only; framework currentScrollable above is authoritative)");
            result.Description = string.Join(" | ", descriptions.ToArray());
            return result;
        }

        private static Collider2D FindColliderFor(MouseScrollableObject target, Collider2D[] hits)
        {
            if (target == null || hits == null) return null;
            foreach (var hit in hits)
                if (hit != null && hit.gameObject.GetComponent<MouseScrollableObject>() == target) return hit;
            return null;
        }

        private static bool IsBlockedByPressable(Collider2D[] hits, Collider2D target)
        {
            if (hits == null || target == null) return false;
            foreach (var hit in hits)
            {
                if (hit == null || hit == target || hit.gameObject.GetComponent<MousePressableObject>() == null) continue;
                var pair = new List<Collider2D> { hit, target };
                if (SelectHighestPriority(pair) == hit) return true;
            }
            return false;
        }

        private static Collider2D SelectHighestPriority(IList<Collider2D> values)
        {
            if (values == null || values.Count == 0) return null;
            var copy = new List<Collider2D>(values);
            copy.Sort(CompareColliderPriority);
            return copy[0];
        }

        private static int CompareColliderPriority(Collider2D a, Collider2D b)
        {
            if (a == null || b == null) return a == null ? (b == null ? 0 : 1) : -1;
            var aHidden = a.gameObject.GetComponent<HiddenButtonIdentifier>() != null;
            var bHidden = b.gameObject.GetComponent<HiddenButtonIdentifier>() != null;
            if (aHidden != bHidden) return aHidden ? -1 : 1;
            var aPressable = a.gameObject.GetComponent<MousePressableObject>();
            var bPressable = b.gameObject.GetComponent<MousePressableObject>();
            var aRenderer = a.gameObject.GetComponent<SpriteRenderer>();
            var bRenderer = b.gameObject.GetComponent<SpriteRenderer>();
            var aVisual = aPressable != null ? aPressable.hasVisual : aRenderer != null;
            var bVisual = bPressable != null ? bPressable.hasVisual : bRenderer != null;
            if (aVisual != bVisual) return aVisual ? -1 : 1;
            if (!aVisual) return 0;
            if (aRenderer != null && bRenderer != null)
            {
                var aLayer = SortingLayer.GetLayerValueFromID(aRenderer.sortingLayerID);
                var bLayer = SortingLayer.GetLayerValueFromID(bRenderer.sortingLayerID);
                if (aLayer != bLayer) return bLayer.CompareTo(aLayer);
                if (aRenderer.sortingOrder != bRenderer.sortingOrder)
                    return bRenderer.sortingOrder.CompareTo(aRenderer.sortingOrder);
            }
            return 0;
        }

        private string DescribeFrameworkLayers()
        {
            if (mouseManager == null || mouseManager.mouseInteractiveLayers == null ||
                mouseManager.mouseInteractiveLayers.Count == 0) return "empty";
            var rows = new List<string>();
            foreach (var layer in mouseManager.mouseInteractiveLayers)
                rows.Add(NameOf(layer) + MasksOf(layer));
            return "top-to-bottom[" + string.Join(",", rows.ToArray()) + "]";
        }

        private string DescribeLocalStack()
        {
            var phase0 = shortCycleLayerStack != null && shortCycleLayerStack.IsTop(phase0Layer);
            var phase1 = shortCycleLayerStack != null && shortCycleLayerStack.IsTop(phase1Layer);
            var modal = false;
            foreach (var layer in modalLayers ?? new MouseInteractionLayer[0])
                if (shortCycleLayerStack != null && shortCycleLayerStack.IsTop(layer)) modal = true;
            return "count=" + (shortCycleLayerStack == null ? -1 : shortCycleLayerStack.Count) +
                ",top=" + NameOf(shortCycleLayerStack == null ? null : shortCycleLayerStack.Top) +
                ",phase0Top=" + phase0 + ",phase1Top=" + phase1 + ",modalTop=" + modal;
        }

        private static string MasksOf(MouseInteractionLayer layer)
        {
            if (layer == null || layer.alloweInteractionLayers == null) return " masks=missing";
            var values = new List<string>();
            foreach (var mask in layer.alloweInteractionLayers) values.Add(mask.value.ToString());
            return " masks=[" + string.Join(",", values.ToArray()) + "]";
        }

        private void AppendEntry(string message)
        {
            var capacity = Math.Max(8, bufferCapacity);
            while (entries.Count >= capacity) entries.Dequeue();
            entries.Enqueue(message);
            if (!logChangesToConsole) return;
            if (Time.unscaledTime - lastConsoleTime < consoleThrottleSeconds) return;
            lastConsoleTime = Time.unscaledTime;
            Debug.Log(message, this);
        }

        private string BuildExportText()
        {
            var builder = new StringBuilder();
            var scenePath = gameObject.scene.path;
            builder.AppendLine(Prefix + " exportUtc=" + DateTime.UtcNow.ToString("o") +
                " scene=" + (string.IsNullOrEmpty(scenePath) ? gameObject.scene.name : scenePath) +
                " mode=" + captureMode + " session=" + captureSessionId + " entries=" + entries.Count);
            builder.AppendLine("lastNonzeroRaw=" + lastNonzeroRawScroll.ToString("0.###") +
                " time=" + lastNonzeroRawTime.ToString("0.###") + " frame=" + lastNonzeroRawFrame);
            foreach (var entry in entries)
            {
                builder.AppendLine(entry);
                builder.AppendLine("---");
            }
            return builder.ToString();
        }

        private void UpdateStatusBoard(string value)
        {
            if (statusBoard != null) statusBoard.text = value;
        }

        private static string PathOf(Transform value)
        {
            if (value == null) return "missing";
            var rows = new List<string>();
            for (var current = value; current != null; current = current.parent) rows.Add(current.name);
            rows.Reverse();
            return string.Join("/", rows.ToArray());
        }

        private static string StateOf(Behaviour value)
        {
            return value == null ? "missing" : NameOf(value) + "(enabled=" + value.enabled +
                ",active=" + value.gameObject.activeInHierarchy + ")";
        }

        private static string NameOf(UnityEngine.Object value)
        {
            return value == null ? "missing" : value.name;
        }

        private static string ResultOf(ShortCycleActionResult result)
        {
            return result == null ? "none" : result.Succeeded + "/" + result.ErrorCode + "/" + (result.Error ?? string.Empty);
        }

        private sealed class HitInspection
        {
            public int SelectedMaskIndex;
            public int SelectedMaskHitCount;
            public bool ScrollRegionCandidate;
            public bool BlockedByPressable;
            public string Description;
        }

        private sealed class TraceSnapshot
        {
            public string Text;
            public string StateKey;
            public string CompactStatus;
            public ShortCycleScrollTraceDecision Decision;
        }
    }
}
