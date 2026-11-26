using UnityEngine;

namespace EatWhat.Cooking.ShortCycle
{
    /// <summary>Set-owned interaction gate used after the K-T3 scene takeover.</summary>
    [DisallowMultipleComponent]
    public sealed class ShortCycleStateGatedInteraction : MonoBehaviour, IShortCycleStateGatedInteraction
    {
        [SerializeField] private ShortCycleSessionManager sessionManager = null;
        [SerializeField] private bool requireSelectedRecipe = false;
        [SerializeField] private Behaviour[] gatedBehaviours = new Behaviour[0];
        private bool[] originalEnabled = new bool[0];
        private bool captured;
        private bool presentationAllowed = true;

        public bool InteractionAllowed { get; private set; }

        private void OnEnable()
        {
            CaptureOriginalState();
            if (sessionManager != null) sessionManager.StateChanged += HandleStateChanged;
            ApplyInteractionAllowed();
        }

        private void OnDisable()
        {
            if (sessionManager != null) sessionManager.StateChanged -= HandleStateChanged;
            RestoreOriginalState();
        }

        private void OnDestroy()
        {
            if (sessionManager != null) sessionManager.StateChanged -= HandleStateChanged;
            RestoreOriginalState();
        }

        public void SetInteractionAllowed(bool allowed)
        {
            CaptureOriginalState();
            presentationAllowed = allowed;
            ApplyInteractionAllowed();
        }

        private void HandleStateChanged(ShortCyclePhase phase, ShortCyclePhase0View phase0View)
        {
            ApplyInteractionAllowed();
        }

        private void ApplyInteractionAllowed()
        {
            var selectedRecipeAllowed = !requireSelectedRecipe ||
                (sessionManager != null && sessionManager.Context != null &&
                 sessionManager.Context.HasSelectedRecipe);
            InteractionAllowed = presentationAllowed && selectedRecipeAllowed;
            var targets = gatedBehaviours ?? new Behaviour[0];
            for (var index = 0; index < targets.Length; index++)
                if (targets[index] != null) targets[index].enabled = InteractionAllowed;
        }

        private void CaptureOriginalState()
        {
            if (captured) return;
            var targets = gatedBehaviours ?? new Behaviour[0];
            originalEnabled = new bool[targets.Length];
            for (var index = 0; index < targets.Length; index++)
                originalEnabled[index] = targets[index] != null && targets[index].enabled;
            captured = true;
        }

        private void RestoreOriginalState()
        {
            if (!captured) return;
            var targets = gatedBehaviours ?? new Behaviour[0];
            for (var index = 0; index < targets.Length && index < originalEnabled.Length; index++)
                if (targets[index] != null) targets[index].enabled = originalEnabled[index];
            captured = false;
        }
    }
}
