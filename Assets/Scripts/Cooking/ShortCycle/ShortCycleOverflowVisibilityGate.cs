using UnityEngine;

// VERIFY-TEMP: 人工验收辅助。框架落地后由 CK01-K 接管，接管前不得删除。

namespace EatWhat.Cooking.ShortCycle
{
    /// <summary>
    /// Camera-slot-only visibility policy for art that may otherwise overflow
    /// into an adjacent ShortCycle screen. Functional containers stay active.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShortCycleOverflowVisibilityGate : MonoBehaviour
    {
        [SerializeField] private ShortCycleCameraPanController cameraPanController = null;
        [SerializeField] private ShortCycleCameraSlot homeSlot = ShortCycleCameraSlot.Phase0;
        [SerializeField] private Renderer[] gatedRenderers = new Renderer[0];
        [SerializeField] private bool hideOnPanStart = true;
        [SerializeField] private bool showOnPanCompleted = true;

        [Header("Optional alpha transition")]
        [SerializeField] private TransitionController alphaTransition = null;
        [SerializeField] private string hideTransitionName = "Hide";
        [SerializeField] private string showTransitionName = "Show";

        public bool IsVisible { get; private set; }

        private void OnEnable()
        {
            RestoreRenderers();
            if (cameraPanController == null)
            {
                return;
            }

            cameraPanController.PanStarted += HandlePanStarted;
            cameraPanController.PanCompleted += HandlePanCompleted;
            ApplyVisibility(ShouldBeVisibleAtRest(homeSlot, cameraPanController.CurrentSlot));
        }

        private void OnDisable()
        {
            if (cameraPanController != null)
            {
                cameraPanController.PanStarted -= HandlePanStarted;
                cameraPanController.PanCompleted -= HandlePanCompleted;
            }

            RestoreRenderers();
        }

        private void OnDestroy()
        {
            RestoreRenderers();
        }

        private void HandlePanStarted(ShortCycleCameraSlot fromSlot, ShortCycleCameraSlot toSlot)
        {
            if (hideOnPanStart && ShouldHideOnPanStart(homeSlot, fromSlot, toSlot))
            {
                ApplyVisibility(false);
                PlayAlphaTransition(hideTransitionName);
            }
        }

        private void HandlePanCompleted(ShortCycleCameraSlot toSlot)
        {
            var visibleAtTarget = ShouldBeVisibleAtRest(homeSlot, toSlot);
            if (visibleAtTarget && showOnPanCompleted)
            {
                ApplyVisibility(true);
                PlayAlphaTransition(showTransitionName);
            }
            else if (!visibleAtTarget)
            {
                ApplyVisibility(false);
            }
        }

        private void ApplyVisibility(bool visible)
        {
            IsVisible = visible;
            var targets = gatedRenderers ?? new Renderer[0];
            var visibleCount = 0;
            var hiddenCount = 0;
            for (var index = 0; index < targets.Length; index++)
            {
                if (targets[index] != null)
                {
                    targets[index].enabled = visible;
                    if (visible)
                    {
                        visibleCount++;
                    }
                    else
                    {
                        hiddenCount++;
                    }
                }
            }

            Debug.Log("CK01-C-GATE home=" + homeSlot +
                " visible=" + visibleCount +
                " hidden=" + hiddenCount +
                " total=" + targets.Length, this);
        }

        private void RestoreRenderers()
        {
            IsVisible = true;
            var targets = gatedRenderers ?? new Renderer[0];
            for (var index = 0; index < targets.Length; index++)
            {
                if (targets[index] != null)
                {
                    targets[index].enabled = true;
                }
            }
        }

        private void PlayAlphaTransition(string transitionName)
        {
            if (alphaTransition != null && !string.IsNullOrEmpty(transitionName))
            {
                alphaTransition.PlayTransition(transitionName);
            }
        }

        public static bool ShouldBeVisibleAtRest(
            ShortCycleCameraSlot configuredHomeSlot,
            ShortCycleCameraSlot currentSlot)
        {
            return configuredHomeSlot == currentSlot;
        }

        public static bool ShouldHideOnPanStart(
            ShortCycleCameraSlot configuredHomeSlot,
            ShortCycleCameraSlot fromSlot,
            ShortCycleCameraSlot toSlot)
        {
            return fromSlot != toSlot &&
                (fromSlot == configuredHomeSlot || toSlot == configuredHomeSlot);
        }
    }
}
