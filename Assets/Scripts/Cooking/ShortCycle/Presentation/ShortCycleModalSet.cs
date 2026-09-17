using UnityEngine;

namespace EatWhat.Cooking.ShortCycle
{
    [DisallowMultipleComponent]
    public sealed class ShortCycleModalSet : MonoBehaviour
    {
        [SerializeField] private ShortCycleModalId modalId = ShortCycleModalId.StepDetail;
        [SerializeField] private ShortCyclePresentationSet presentationSet = null;
        [SerializeField] private MouseInteractionLayer modalLayer = null;

        public ShortCycleModalId ModalId { get { return modalId; } }
        public MouseInteractionLayer ModalLayer { get { return modalLayer; } }

        public void Activate()
        {
            if (presentationSet != null) { presentationSet.Activate(); presentationSet.SetFocus(true); }
        }

        public void Deactivate()
        {
            if (presentationSet != null) { presentationSet.SetFocus(false); presentationSet.Deactivate(); }
        }

        public void RestoreTransientState()
        {
            if (presentationSet != null) presentationSet.RestoreTransientState();
        }
    }
}
