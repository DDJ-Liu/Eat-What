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

        public void Apply(ShortCycleTrayView view)
        {
            if (transitionController == null) return;
            transitionController.GoTo(ResolvePreset(view));
        }

        private string ResolvePreset(ShortCycleTrayView view)
        {
            if (view == ShortCycleTrayView.Highlight) return highlightPreset;
            if (view == ShortCycleTrayView.Detail) return detailPreset;
            return collapsedPreset;
        }
    }
}
