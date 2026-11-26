using UnityEngine;

namespace EatWhat.Cooking.ShortCycle
{
    [DisallowMultipleComponent]
    public sealed class ShortCycleTrayPresentation : MonoBehaviour
    {
        [SerializeField] private TransitionController transitionController = null;
        [SerializeField] private string collapsedPreset = "Collapsed";
        [SerializeField] private string highlightPreset = "Highlight";
        [SerializeField] private string detailPreset = "Detail";
        private ShortCycleTrayView requestedView;
        private bool hasRequestedView;
        private bool pendingView;

        public void Apply(ShortCycleTrayView view)
        {
            if (hasRequestedView && requestedView == view && !pendingView) return;
            requestedView = view;
            hasRequestedView = true;
            pendingView = true;
            ApplyPendingView();
        }

        private void OnEnable() { ApplyPendingView(); }
        private void OnDisable() { if (hasRequestedView) pendingView = true; }

        private void ApplyPendingView()
        {
            if (!pendingView || transitionController == null || !transitionController.isActiveAndEnabled) return;
            if (transitionController.GoTo(ResolvePreset(requestedView))) pendingView = false;
        }

        private string ResolvePreset(ShortCycleTrayView view)
        {
            if (view == ShortCycleTrayView.Highlight) return highlightPreset;
            if (view == ShortCycleTrayView.Detail) return detailPreset;
            return collapsedPreset;
        }
    }
}
