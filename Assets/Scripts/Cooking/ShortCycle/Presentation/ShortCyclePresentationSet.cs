using EatWhat.Tools.Layout;
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
        [SerializeField] private bool keepVisualActiveAcrossSpaces = false;
        [SerializeField, Range(0f, 1f)] private float appearAtProgress = 0f;
        [SerializeField, Range(0f, 1f)] private float disappearAtProgress = 0f;

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
        private bool? requestedFocus;
        private Renderer[] boundsRenderers = new Renderer[0];
        private Bounds[] rendererBoundsSnapshot = new Bounds[0];
        private Bounds cachedWorldBounds;
        private bool hasCachedWorldBounds;
        private bool boundsDirty = true;
        private bool missingBoundsWarningIssued;

        public bool IsFocused { get; private set; }
        public Transform ContainerTransform { get { return containerRoot == null ? transform : containerRoot.transform; } }
        public MouseInteractionLayer InteractionLayer { get { return interactionLayer; } }
        public bool KeepVisualActiveAcrossSpaces { get { return keepVisualActiveAcrossSpaces; } }
        public float AppearAtProgress { get { return SafeProgress(appearAtProgress); } }
        public float DisappearAtProgress { get { return SafeProgress(disappearAtProgress); } }
        internal bool IsVisibilityRequested { get { return requestedActive == true; } }
        internal bool HasVisibilityRequest { get { return requestedActive.HasValue; } }

        private void OnEnable() { CaptureTransientState(); MarkBoundsDirty(); }
        private void OnDisable() { if (!internalLifecycleChange) RestoreTransientState(false); }
        private void OnDestroy() { RestoreTransientState(false); }
        private void OnTransformChildrenChanged() { MarkBoundsDirty(); }
        private void OnDidApplyAnimationProperties() { MarkBoundsDirty(); }
        private void OnValidate()
        {
            appearAtProgress = SafeProgress(appearAtProgress);
            disappearAtProgress = SafeProgress(disappearAtProgress);
            MarkBoundsDirty();
        }

        private static float SafeProgress(float value)
        {
            return float.IsNaN(value) || float.IsInfinity(value) ? 0f : Mathf.Clamp01(value);
        }

        public void MarkBoundsDirty()
        {
            boundsDirty = true;
        }

        public bool TryGetWorldBounds(out Bounds bounds)
        {
            RefreshBoundsIfNeeded();
            bounds = cachedWorldBounds;
            return hasCachedWorldBounds;
        }

        /// <summary>
        /// Returns true when this Set intersects an explicit orthographic camera rectangle.
        /// Renderer-less Sets stay visible by logical state and issue a throttled diagnostic.
        /// </summary>
        public bool IsInView(Rect cameraRect)
        {
            Bounds worldBounds;
            if (!TryGetWorldBounds(out worldBounds))
            {
                if (!missingBoundsWarningIssued)
                {
                    Debug.LogWarning("ShortCycle Set has no Renderer bounds; preserving logical visibility: " + name, this);
                    missingBoundsWarningIssued = true;
                }
                return true;
            }

            missingBoundsWarningIssued = false;
            var min = worldBounds.min;
            var max = worldBounds.max;
            return max.x >= cameraRect.x && min.x <= cameraRect.x + cameraRect.width &&
                max.y >= cameraRect.y && min.y <= cameraRect.y + cameraRect.height;
        }

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
            if (requestedActive == true) return;
            CaptureTransientState();
            requestedActive = true;
            requestedFocus = null;
            transitionGeneration++;
            if (transitionController != null) transitionController.CancelAll();
            hidden = false;
            RestoreHiddenRenderers();
            if (containerRoot != null && !containerRoot.activeSelf) containerRoot.SetActive(true);
            if (transitionController != null && !string.IsNullOrEmpty(enterTransition))
                transitionController.PlayTransition(enterTransition);
        }

        public void Deactivate()
        {
            if (requestedActive == false) return;
            CaptureTransientState();
            requestedActive = false;
            requestedFocus = null;
            var request = ++transitionGeneration;
            if (transitionController != null) transitionController.CancelAll();
            SetFocus(false);
            if (deactivateMode == DeactivateMode.RendererHidden)
            {
                hidden = true;
                for (var i = 0; i < hiddenRenderers.Length; i++)
                    if (hiddenRenderers[i] != null && hiddenRenderers[i].enabled)
                        hiddenRenderers[i].enabled = false;
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
            if (requestedFocus.HasValue && requestedFocus.Value == focused) return;
            requestedFocus = focused;
            IsFocused = focused;
            var renderers = overflowRenderers ?? new Renderer[0];
            for (var index = 0; index < renderers.Length; index++)
            {
                var shouldRender = !hidden && requestedActive == true && originalRendererEnabled[index];
                if (renderers[index] != null && renderers[index].enabled != shouldRender)
                    renderers[index].enabled = shouldRender;
            }

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
            requestedFocus = null;
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

        private void RefreshBoundsIfNeeded()
        {
            if (!boundsDirty && !RendererBoundsChanged()) return;

            if (boundsDirty)
            {
                var candidates = ContainerTransform.GetComponentsInChildren<Renderer>(true) ?? new Renderer[0];
                var worldRendererCount = 0;
                for (var index = 0; index < candidates.Length; index++)
                    if (candidates[index] != null &&
                        !CameraViewportAnchor.IsAnchoredTransform(candidates[index].transform))
                        worldRendererCount++;

                boundsRenderers = new Renderer[worldRendererCount];
                var destination = 0;
                for (var index = 0; index < candidates.Length; index++)
                    if (candidates[index] != null &&
                        !CameraViewportAnchor.IsAnchoredTransform(candidates[index].transform))
                        boundsRenderers[destination++] = candidates[index];
                rendererBoundsSnapshot = new Bounds[boundsRenderers.Length];
            }

            hasCachedWorldBounds = ShortCycleSpace.ComputeSetBounds(
                boundsRenderers, rendererBoundsSnapshot, out cachedWorldBounds);
            boundsDirty = false;
        }

        private bool RendererBoundsChanged()
        {
            if (rendererBoundsSnapshot.Length != boundsRenderers.Length)
            {
                boundsDirty = true;
                return true;
            }
            for (var index = 0; index < boundsRenderers.Length; index++)
            {
                if (boundsRenderers[index] == null)
                {
                    boundsDirty = true;
                    return true;
                }
                var current = boundsRenderers[index].bounds;
                if (Vector3.Distance(current.center, rendererBoundsSnapshot[index].center) > 0.0001f ||
                    Vector3.Distance(current.size, rendererBoundsSnapshot[index].size) > 0.0001f) return true;
            }
            return false;
        }

        private void DeactivateContainer()
        {
            internalLifecycleChange = true;
            try
            {
                if (containerRoot != null && containerRoot.activeSelf) containerRoot.SetActive(false);
            }
            finally { internalLifecycleChange = false; }
        }

        private void RestoreHiddenRenderers()
        {
            for (var i = 0; i < hiddenRenderers.Length; i++)
                if (hiddenRenderers[i] != null && hiddenRenderers[i].enabled != originalHiddenEnabled[i])
                    hiddenRenderers[i].enabled = originalHiddenEnabled[i];
        }
    }
}
