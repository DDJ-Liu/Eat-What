using UnityEngine;

namespace EatWhat.Cooking.ShortCycle
{
    /// <summary>Scene-facing P2 handoff holder for C-T2 wiring and observation.</summary>
    public sealed class ShortCyclePhase2HandoffPresenter : MonoBehaviour, IShortCyclePhase2HandoffSink
    {
        [SerializeField] private ShortCyclePhase2TransitionAdapter transitionAdapter = null;
        public ShortCycleDataHandoff LastHandoff { get; private set; }
        public ShortCyclePhase2TransitionAdapter TransitionAdapter { get { return transitionAdapter; } }
        public bool IsAvailable { get { string error; return transitionAdapter != null && transitionAdapter.CanLoad(out error); } }
        internal void Configure(ShortCycleSessionManager session, InventoryManager inventory, ShortCyclePresentationDirector director)
        { if (transitionAdapter != null) transitionAdapter.Configure(session, inventory, director); }
        internal void BeginAfterCommit() { if (transitionAdapter != null) transitionAdapter.BeginAfterCommit(); }
        internal void Teardown() { if (transitionAdapter != null) transitionAdapter.Teardown(false); }

        public bool TryReceive(ShortCycleDataHandoff handoff, out string error)
        {
            if (handoff == null || string.IsNullOrEmpty(handoff.CurrentRecipeId))
            {
                error = "Phase 2 handoff requires a recipe ID.";
                return false;
            }
            if (transitionAdapter == null) { error = "Phase2 transition adapter is not connected."; return false; }
            if (!transitionAdapter.TryRequestTransition(handoff, out error)) return false;
            LastHandoff = new ShortCycleDataHandoff(handoff.CurrentRecipeId, Phase2SceneEntry.CopyItems(handoff.PrepZoneContents));
            error = null;
            return true;
        }
    }
}
