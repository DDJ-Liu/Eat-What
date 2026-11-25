using System.Collections.Generic;
using EatWhat.Tools.Layout;
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
        private Renderer[] navigationRenderers = new Renderer[0];
        private bool[] navigationRendererEnabled = new bool[0];
        private Behaviour[] navigationInputs = new Behaviour[0];
        private bool[] navigationInputEnabled = new bool[0];

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
            for (var index = 0; index < navigationInputs.Length; index++)
                if (navigationInputs[index] != null)
                    navigationInputs[index].enabled = InteractionAllowed && navigationInputEnabled[index];
            // Enabling text/visual components can touch their Renderer in OnEnable.
            for (var index = 0; index < navigationRenderers.Length; index++)
                if (navigationRenderers[index] != null)
                    navigationRenderers[index].enabled = InteractionAllowed && navigationRendererEnabled[index];
        }

        private void CaptureOriginalState()
        {
            if (captured) return;
            var targets = gatedBehaviours ?? new Behaviour[0];
            originalEnabled = new bool[targets.Length];
            for (var index = 0; index < targets.Length; index++)
                originalEnabled[index] = targets[index] != null && targets[index].enabled;
            CaptureNavigationState(targets);
            captured = true;
        }

        // The saved page buttons share background Sets and remain viewport-anchored.
        // Gate only their own subtree; keep the Set, anchor and action bridge alive.
        private void CaptureNavigationState(Behaviour[] targets)
        {
            var binding = GetComponent<ShortCycleMouseActionBinding>();
            if (binding == null || (binding.ActionName != ShortCycleActionNames.Escape &&
                binding.ActionName != ShortCycleActionNames.Return)) return;
            navigationRenderers = GetComponentsInChildren<Renderer>(true);
            navigationRendererEnabled = new bool[navigationRenderers.Length];
            for (var index = 0; index < navigationRenderers.Length; index++)
                navigationRendererEnabled[index] = navigationRenderers[index] != null && navigationRenderers[index].enabled;
            var inputs = new List<Behaviour>();
            foreach (var input in GetComponentsInChildren<Behaviour>(true))
            {
                if (input == null || input is ShortCycleStateGatedInteraction ||
                    input is ShortCycleMouseActionBinding || input is CameraViewportAnchor) continue;
                var explicitlyGated = false;
                foreach (var target in targets)
                    if (target == input) { explicitlyGated = true; break; }
                if (!explicitlyGated) inputs.Add(input);
            }
            navigationInputs = inputs.ToArray();
            navigationInputEnabled = new bool[navigationInputs.Length];
            for (var index = 0; index < navigationInputs.Length; index++)
                navigationInputEnabled[index] = navigationInputs[index].enabled;
        }

        private void RestoreOriginalState()
        {
            if (!captured) return;
            var targets = gatedBehaviours ?? new Behaviour[0];
            for (var index = 0; index < targets.Length && index < originalEnabled.Length; index++)
                if (targets[index] != null) targets[index].enabled = originalEnabled[index];
            for (var index = 0; index < navigationInputs.Length; index++)
                if (navigationInputs[index] != null)
                    navigationInputs[index].enabled = navigationInputEnabled[index];
            for (var index = 0; index < navigationRenderers.Length; index++)
                if (navigationRenderers[index] != null)
                    navigationRenderers[index].enabled = navigationRendererEnabled[index];
            navigationRenderers = new Renderer[0];
            navigationRendererEnabled = new bool[0];
            navigationInputs = new Behaviour[0];
            navigationInputEnabled = new bool[0];
            captured = false;
        }
    }
}
