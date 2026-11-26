// VERIFY-TEMP: visible Phase1-only acceptance control. Remove when the formal Modal entry replaces it.
using UnityEngine;

namespace EatWhat.Cooking.ShortCycle
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Cooking/Short Cycle/Debug/Modal Scroll Verification Control")]
    public sealed class ShortCycleModalScrollVerificationControl : MonoBehaviour
    {
        private const string LogPrefix = "[VERIFY-TEMP][ModalScroll] ";

        [SerializeField] private ShortCycleSessionManager sessionManager = null;
        [SerializeField] private ShortCycleInteractionLayerStack layerStack = null;
        [SerializeField] private MouseInteractionLayer verificationLayer = null;

        private bool isOpen;

        public bool IsOpen
        {
            get { return isOpen; }
        }

        private bool IsPhase1
        {
            get { return sessionManager != null && sessionManager.CurrentPhase == ShortCyclePhase.Phase1; }
        }

        private void OnEnable()
        {
            if (sessionManager != null)
                sessionManager.StateChanged += HandleStateChanged;
        }

        private void Update()
        {
            if (isOpen && !IsPhase1)
                CloseInternal("left_phase1", false);
        }

        private void OnGUI()
        {
            if (!IsPhase1) return;

            GUI.depth = -1000;
            if (!isOpen)
            {
                if (GUI.Button(new Rect(24f, 24f, 300f, 54f), "打开 Modal 滚轮验收"))
                    OpenForVerification();
                return;
            }

            GUI.Box(
                new Rect(0f, 0f, Screen.width, Screen.height),
                "Modal 滚轮阻断验收已开启\n此时滚轮、拖拽与场景点击都应被阻断");
            if (GUI.Button(new Rect(24f, 24f, 300f, 54f), "关闭 Modal 滚轮验收"))
                CloseForVerification();
        }

        public bool OpenForVerification()
        {
            if (isOpen)
            {
                Debug.LogWarning(LogPrefix + "open_failed reason=already_open", this);
                return false;
            }
            if (!IsPhase1)
            {
                Debug.LogWarning(LogPrefix + "open_failed reason=phase1_required", this);
                return false;
            }
            if (layerStack == null || verificationLayer == null)
            {
                Debug.LogWarning(LogPrefix + "open_failed reason=missing_layer_wiring", this);
                return false;
            }
            if (!layerStack.Push(verificationLayer))
            {
                Debug.LogWarning(LogPrefix + "open_failed reason=layer_push_rejected", this);
                return false;
            }

            isOpen = true;
            Debug.Log(LogPrefix + "opened layer=" + verificationLayer.name, this);
            return true;
        }

        public bool CloseForVerification()
        {
            return CloseInternal("manual_close", true);
        }

        private bool CloseInternal(string reason, bool reportAlreadyClosed)
        {
            if (!isOpen)
            {
                if (reportAlreadyClosed)
                    Debug.LogWarning(LogPrefix + "close_failed reason=not_open", this);
                return false;
            }

            if (layerStack == null || verificationLayer == null)
            {
                Debug.LogWarning(LogPrefix + "close_failed reason=missing_layer_wiring", this);
                return false;
            }

            var removed = layerStack.Remove(verificationLayer);
            if (!removed && layerStack.Contains(verificationLayer))
            {
                Debug.LogWarning(LogPrefix + "close_failed reason=layer_remove_rejected", this);
                return false;
            }

            // A phase switch can remove every transient layer before this listener runs.
            isOpen = false;
            Debug.Log(LogPrefix + "closed reason=" + reason, this);
            return true;
        }

        private void HandleStateChanged(ShortCyclePhase phase, ShortCyclePhase0View phase0View)
        {
            if (isOpen && phase != ShortCyclePhase.Phase1)
                CloseInternal("phase_changed_to_" + phase, false);
        }

        private void OnDisable()
        {
            if (sessionManager != null)
                sessionManager.StateChanged -= HandleStateChanged;
            if (isOpen)
                CloseInternal("component_disabled", false);
        }

        private void OnDestroy()
        {
            if (isOpen)
                CloseInternal("component_destroyed", false);
        }
    }
}
