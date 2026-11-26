using UnityEngine;

// VERIFY-TEMP: 人工验收辅助。框架落地后由 CK01-K 接管，接管前不得删除。

namespace EatWhat.Cooking.ShortCycle
{
    /// <summary>
    /// Explicit FSM gate for world-space action buttons that are not grid pieces.
    /// It changes interaction behaviours only; functional containers and renderers
    /// remain owned by their dedicated state and camera-slot policies.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShortCycleActionButtonStateGate : MonoBehaviour
    {
        [SerializeField] private ShortCycleSessionManager sessionManager = null;
        [SerializeField] private Behaviour[] gatedBehaviours = new Behaviour[0];
        [SerializeField] private ShortCyclePhase requiredPhase = ShortCyclePhase.Phase0;
        [SerializeField] private bool requirePhase0View = false;
        [SerializeField] private ShortCyclePhase0View requiredPhase0View = ShortCyclePhase0View.RecipeBrowse;

        public bool InteractionAllowed { get; private set; }

        public void Configure(
            ShortCycleSessionManager coordinator,
            Behaviour[] behaviours,
            ShortCyclePhase phase,
            bool requireView,
            ShortCyclePhase0View phase0View)
        {
            if (sessionManager != null && isActiveAndEnabled)
            {
                sessionManager.StateChanged -= HandleStateChanged;
            }

            sessionManager = coordinator;
            gatedBehaviours = behaviours ?? new Behaviour[0];
            requiredPhase = phase;
            requirePhase0View = requireView;
            requiredPhase0View = phase0View;

            if (sessionManager != null && isActiveAndEnabled)
            {
                sessionManager.StateChanged += HandleStateChanged;
            }
            RefreshGate();
        }

        private void OnEnable()
        {
            if (sessionManager != null)
            {
                sessionManager.StateChanged += HandleStateChanged;
            }
            RefreshGate();
        }

        private void OnDisable()
        {
            if (sessionManager != null)
            {
                sessionManager.StateChanged -= HandleStateChanged;
            }
        }

        private void HandleStateChanged(ShortCyclePhase phase, ShortCyclePhase0View phase0View)
        {
            RefreshGate();
        }

        private void RefreshGate()
        {
            var allowed = sessionManager != null && sessionManager.CurrentPhase == requiredPhase;
            if (allowed && requirePhase0View)
            {
                allowed = sessionManager.CurrentPhase0View == requiredPhase0View;
            }

            InteractionAllowed = allowed;
            var targets = gatedBehaviours ?? new Behaviour[0];
            for (var index = 0; index < targets.Length; index++)
            {
                if (targets[index] != null)
                {
                    targets[index].enabled = allowed;
                }
            }
        }
    }
}
