using UnityEngine;

namespace EatWhat.Cooking.ShortCycle
{
    /// <summary>Scene-facing P2 handoff holder for C-T2 wiring and observation.</summary>
    public sealed class ShortCyclePhase2HandoffPresenter : MonoBehaviour, IShortCyclePhase2HandoffSink
    {
        [SerializeField] private ShortCyclePhase2TransitionAdapter transitionAdapter = null;
        public ShortCycleDataHandoff LastHandoff { get; private set; }
        public bool IsAvailable { get { return true; } }

        public bool TryReceive(ShortCycleDataHandoff handoff, out string error)
        {
            if (handoff == null || string.IsNullOrEmpty(handoff.CurrentRecipeId))
            {
                error = "Phase 2 handoff requires a recipe ID.";
                return false;
            }
            LastHandoff = handoff;
            if (transitionAdapter != null) transitionAdapter.RequestTransition(handoff);
            error = null;
            return true;
        }
    }
}
