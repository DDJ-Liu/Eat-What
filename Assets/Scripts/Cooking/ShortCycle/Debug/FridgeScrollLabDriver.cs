// VERIFY-TEMP: FridgeScrollLab-only diagnostic driver. Remove only with the whole lab contract.
using System.Collections;
using TMPro;
using UnityEngine;

namespace EatWhat.Cooking.ShortCycle
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Cooking/Short Cycle/Debug/Fridge Scroll Lab Driver")]
    public sealed class FridgeScrollLabDriver : MonoBehaviour
    {
        [SerializeField] private ShortCycleSessionManager sessionManager = null;
        [SerializeField] private ShortCycleScrollAdapter scrollAdapter = null;
        [SerializeField] private ShortCycleFridgeScrollTraceProbe traceProbe = null;
        [SerializeField] private ShortCycleInteractionLayerStack layerStack = null;
        [SerializeField] private MouseInteractionLayer modalComparisonLayer = null;
        [SerializeField] private TextMeshPro statusBoard = null;
        [SerializeField] private Transform[] testPoints = new Transform[0];
        [SerializeField] private string recipeId = "rcp_tomato_egg";
        [SerializeField] private bool initializeOnStart = true;

        private bool modalPushed;

        public ShortCycleSessionManager SessionManager { get { return sessionManager; } }
        public ShortCycleScrollAdapter ScrollAdapter { get { return scrollAdapter; } }
        public ShortCycleFridgeScrollTraceProbe TraceProbe { get { return traceProbe; } }
        public ShortCycleInteractionLayerStack LayerStack { get { return layerStack; } }
        public MouseInteractionLayer ModalComparisonLayer { get { return modalComparisonLayer; } }
        public TextMeshPro StatusBoard { get { return statusBoard; } }
        public Transform[] TestPoints { get { return testPoints; } }
        public ShortCycleActionResult LastSimulationResult { get; private set; }
        public string LastInitializationResult { get; private set; }

        private IEnumerator Start()
        {
            // Let ShortCycleSessionManager.Start establish its configured debug recipe first.
            yield return null;
            if (initializeOnStart) InitializeLabSession();
            BeginRealInputSession();
        }

        private void LateUpdate()
        {
            if (statusBoard == null) return;
            var phase = sessionManager == null ? ShortCyclePhase.None : sessionManager.CurrentPhase;
            var trace = traceProbe == null ? "trace=missing" :
                "trace=" + traceProbe.CaptureMode + "/" + traceProbe.CaptureSessionId +
                " raw=" + traceProbe.LatestRawScroll.ToString("0.###") +
                " last=" + traceProbe.LastNonzeroRawScroll.ToString("0.###") + "@" + traceProbe.LastNonzeroRawFrame;
            statusBoard.text = "FridgeScrollLab\nphase=" + phase + " modal=" + modalPushed + "\n" + trace +
                "\ninit=" + (LastInitializationResult ?? "pending") +
                "\nsim=" + ResultOf(LastSimulationResult);
        }

        [ContextMenu("Fridge Scroll Lab/Initialize Phase1")]
        public void InitializeLabSession()
        {
            if (sessionManager == null)
            {
                LastInitializationResult = "session_manager_missing";
                return;
            }

            if (sessionManager.CurrentPhase == ShortCyclePhase.None)
            {
                var begin = sessionManager.TryBeginSession(recipeId, ShortCycleTriggerContext.FreeTry);
                if (begin == null || !begin.Succeeded)
                {
                    LastInitializationResult = "begin_failed:" + (begin == null ? "no_result" : begin.Error);
                    return;
                }
            }

            if (sessionManager.CurrentPhase == ShortCyclePhase.Phase0)
            {
                if (sessionManager.Context == null)
                {
                    LastInitializationResult = "session_context_missing";
                    return;
                }
                if (!sessionManager.Context.HasSelectedRecipe)
                {
                    if (sessionManager.CurrentPhase0View != ShortCyclePhase0View.RecipeCatalog)
                    {
                        var catalog = sessionManager.TryShowRecipeCatalog();
                        if (catalog == null || !catalog.Succeeded)
                        {
                            LastInitializationResult = "catalog_failed:" + (catalog == null ? "no_result" : catalog.Error);
                            return;
                        }
                    }
                    var select = sessionManager.TrySelectRecipe(recipeId, false);
                    if (select == null || !select.Succeeded)
                    {
                        LastInitializationResult = "select_failed:" + (select == null ? "no_result" : select.Error);
                        return;
                    }
                }
                var start = sessionManager.TryStartCooking();
                LastInitializationResult = start != null && start.Succeeded
                    ? "phase1_ready" : "start_failed:" + (start == null ? "no_result" : start.Error);
                return;
            }

            LastInitializationResult = sessionManager.CurrentPhase == ShortCyclePhase.Phase1
                ? "phase1_ready" : "unexpected_phase:" + sessionManager.CurrentPhase;
        }

        [ContextMenu("Fridge Scroll Lab/Trace/Begin REAL_INPUT Session")]
        public void BeginRealInputSession()
        {
            if (traceProbe == null) return;
            traceProbe.BeginCaptureSession(ShortCycleScrollTraceMode.RealInput);
            traceProbe.SetTraceEnabled(true);
        }

        [ContextMenu("Fridge Scroll Lab/Trace/Begin SIMULATED Session")]
        public void BeginSimulatedSession()
        {
            if (traceProbe == null) return;
            traceProbe.BeginCaptureSession(ShortCycleScrollTraceMode.Simulated);
            traceProbe.SetTraceEnabled(true);
        }

        [ContextMenu("Fridge Scroll Lab/Simulated/Next Tier (+1)")]
        public void SimulateNextTier()
        {
            DispatchSimulation(1f);
        }

        [ContextMenu("Fridge Scroll Lab/Simulated/Previous Tier (-1)")]
        public void SimulatePreviousTier()
        {
            DispatchSimulation(-1f);
        }

        [ContextMenu("Fridge Scroll Lab/Layer/Push Modal Comparison")]
        public void PushModalComparison()
        {
            if (layerStack == null || modalComparisonLayer == null || modalPushed) return;
            modalPushed = layerStack.Push(modalComparisonLayer);
            if (traceProbe != null) traceProbe.CaptureSnapshot();
        }

        [ContextMenu("Fridge Scroll Lab/Layer/Pop Modal Comparison")]
        public void PopModalComparison()
        {
            if (layerStack == null || modalComparisonLayer == null || !modalPushed) return;
            modalPushed = !layerStack.Pop(modalComparisonLayer);
            if (traceProbe != null) traceProbe.CaptureSnapshot();
        }

        [ContextMenu("Fridge Scroll Lab/Trace/Reset Current Session")]
        public void ResetTraceSession()
        {
            if (traceProbe != null) traceProbe.ResetCaptureSession();
        }

        [ContextMenu("Fridge Scroll Lab/Trace/Export Snapshot")]
        public void ExportTraceSnapshot()
        {
            if (traceProbe != null) traceProbe.ExportSnapshot();
        }

        private void DispatchSimulation(float delta)
        {
            if (traceProbe != null && traceProbe.CaptureMode != ShortCycleScrollTraceMode.Simulated)
                BeginSimulatedSession();
            LastSimulationResult = scrollAdapter == null
                ? ShortCycleActionResult.Failure("FridgeScrollLab scroll adapter is not connected.")
                : scrollAdapter.Dispatch(delta);
            if (traceProbe != null) traceProbe.CaptureSnapshot();
        }

        private void OnDisable()
        {
            if (modalPushed && layerStack != null && modalComparisonLayer != null)
                layerStack.Pop(modalComparisonLayer);
            modalPushed = false;
        }

        private static string ResultOf(ShortCycleActionResult result)
        {
            return result == null ? "none" : result.Succeeded + "/" + result.ErrorCode + "/" + (result.Error ?? string.Empty);
        }
    }
}
