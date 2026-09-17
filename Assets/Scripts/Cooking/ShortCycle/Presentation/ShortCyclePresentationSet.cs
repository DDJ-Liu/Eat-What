using UnityEngine;

namespace EatWhat.Cooking.ShortCycle
{
    public enum DeactivateMode { ContainerInactive, RendererHidden }

    [DisallowMultipleComponent]
    public sealed class ShortCyclePresentationSet : MonoBehaviour
    {
        [SerializeField] private GameObject containerRoot = null;
        [SerializeField] private MouseInteractionLayer interactionLayer = null;
        [SerializeField] private MonoBehaviour[] gatedInteractions = new MonoBehaviour[0];
        [SerializeField] private Renderer[] overflowRenderers = new Renderer[0];
        [SerializeField] private ShortCycleOutlineGroupBinding outline = null;
        [SerializeField] private TransitionController transitionController = null;
        [SerializeField] private string enterTransition = "Show";
        [SerializeField] private string exitTransition = "Hide";
        [SerializeField] private DeactivateMode deactivateMode = DeactivateMode.ContainerInactive;

        private bool[] originalRendererEnabled = new bool[0];
        private Renderer[] hiddenRenderers = new Renderer[0];
        private bool[] originalHiddenEnabled = new bool[0];
        private bool[] originalInteractionEnabled = new bool[0];
        private bool originalContainerActive;
        private bool captured;
        private bool internalLifecycleChange;
        private bool hidden;
        private int transitionGeneration;
        private bool? requestedActive;

        public bool IsFocused { get; private set; }
        public Transform ContainerTransform { get { return containerRoot == null ? transform : containerRoot.transform; } }
        public MouseInteractionLayer InteractionLayer { get { return interactionLayer; } }

        private void OnEnable() { CaptureTransientState(); }
        private void OnDisable() { if (!internalLifecycleChange) RestoreTransientState(false); }
        private void OnDestroy() { RestoreTransientState(false); }

        internal void ApplyVisibility(bool active, bool focused)
        {
            if (!requestedActive.HasValue || requestedActive.Value != active)
            {
                if (active) Activate();
                else Deactivate();
            }
            SetFocus(active && focused);
        }

        public void Activate()
        {
            CaptureTransientState();
            requestedActive = true;
            transitionGeneration++;
            if (transitionController != null) transitionController.CancelAll();
            hidden = false;
            RestoreHiddenRenderers();
            if (containerRoot != null) containerRoot.SetActive(true);
            if (transitionController != null && !string.IsNullOrEmpty(enterTransition))
                transitionController.PlayTransition(enterTransition);
        }

        public void Deactivate()
        {
            CaptureTransientState();
            requestedActive = false;
            var request = ++transitionGeneration;
            if (transitionController != null) transitionController.CancelAll();
            SetFocus(false);
            if (deactivateMode == DeactivateMode.RendererHidden)
            {
                hidden = true;
                for (var i = 0; i < hiddenRenderers.Length; i++)
                    if (hiddenRenderers[i] != null) hiddenRenderers[i].enabled = false;
                return;
            }
            if (transitionController != null && !string.IsNullOrEmpty(exitTransition))
                transitionController.PlayTransition(exitTransition, () =>
                {
                    if (request == transitionGeneration) DeactivateContainer();
                });
            else DeactivateContainer();
        }

        public void SetFocus(bool focused)
        {
            CaptureTransientState();
            IsFocused = focused;
            var renderers = overflowRenderers ?? new Renderer[0];
            for (var index = 0; index < renderers.Length; index++)
                if (renderers[index] != null) renderers[index].enabled = !hidden && focused && originalRendererEnabled[index];

            var gates = gatedInteractions ?? new MonoBehaviour[0];
            for (var index = 0; index < gates.Length; index++)
            {
                var gate = gates[index] as IShortCycleStateGatedInteraction;
                if (gate != null) gate.SetInteractionAllowed(!hidden && focused);
                else if (gates[index] != null)
                    gates[index].enabled = !hidden && focused && originalInteractionEnabled[index];
            }
            if (outline != null) outline.ApplyDefault();
        }

        /// <summary>Restores edit/play-entry renderer and gate state on every lifecycle exit.</summary>
        public void RestoreTransientState()
        {
            RestoreTransientState(true);
        }

        private void RestoreTransientState(bool restoreContainerActiveState)
        {
            if (!captured) return;
            transitionGeneration++;
            if (transitionController != null) transitionController.CancelAll();
            hidden = false;
            RestoreHiddenRenderers();
            var renderers = overflowRenderers ?? new Renderer[0];
            for (var index = 0; index < renderers.Length && index < originalRendererEnabled.Length; index++)
                if (renderers[index] != null) renderers[index].enabled = originalRendererEnabled[index];

            var gates = gatedInteractions ?? new MonoBehaviour[0];
            for (var index = 0; index < gates.Length; index++)
            {
                var gate = gates[index] as IShortCycleStateGatedInteraction;
                if (gate != null) gate.SetInteractionAllowed(true);
                if (gates[index] != null) gates[index].enabled = originalInteractionEnabled[index];
            }
            if (restoreContainerActiveState && containerRoot != null && deactivateMode == DeactivateMode.ContainerInactive)
            {
                internalLifecycleChange = true;
                try { containerRoot.SetActive(originalContainerActive); }
                finally { internalLifecycleChange = false; }
            }
            captured = false;
            requestedActive = null;
            IsFocused = false;
        }

        private void CaptureTransientState()
        {
            if (captured) return;
            originalContainerActive = containerRoot != null && containerRoot.activeSelf;
            hiddenRenderers = deactivateMode == DeactivateMode.RendererHidden
                ? ContainerTransform.GetComponentsInChildren<Renderer>(true) : new Renderer[0];
            originalHiddenEnabled = new bool[hiddenRenderers.Length];
            for (var i = 0; i < hiddenRenderers.Length; i++)
                originalHiddenEnabled[i] = hiddenRenderers[i] != null && hiddenRenderers[i].enabled;
            var gates = gatedInteractions ?? new MonoBehaviour[0];
            originalInteractionEnabled = new bool[gates.Length];
            for (var i = 0; i < gates.Length; i++)
                originalInteractionEnabled[i] = gates[i] != null && gates[i].enabled;
            var renderers = overflowRenderers ?? new Renderer[0];
            originalRendererEnabled = new bool[renderers.Length];
            for (var index = 0; index < renderers.Length; index++)
                originalRendererEnabled[index] = renderers[index] != null && renderers[index].enabled;
            captured = true;
        }

        private void DeactivateContainer()
        {
            internalLifecycleChange = true;
            try
            {
                if (containerRoot != null) containerRoot.SetActive(false);
            }
            finally { internalLifecycleChange = false; }
        }

        private void RestoreHiddenRenderers()
        {
            for (var i = 0; i < hiddenRenderers.Length; i++)
                if (hiddenRenderers[i] != null) hiddenRenderers[i].enabled = originalHiddenEnabled[i];
        }
    }
}
