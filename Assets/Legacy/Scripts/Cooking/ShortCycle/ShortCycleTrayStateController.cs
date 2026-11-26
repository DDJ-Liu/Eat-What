using System;
using UnityEngine;

namespace EatWhat.Cooking.ShortCycle
{
    /// <summary>Scene component owning the tray sub-FSM through explicit wiring.</summary>
    [DisallowMultipleComponent]
    public sealed class ShortCycleTrayStateController : MonoBehaviour
    {
        [SerializeField] private ShortCyclePresentationDirector presentationDirector = null;
        private ShortCycleTrayStateMachine stateMachine;
        private bool missingHostReported;
        public event Action<ShortCycleTrayView> StateChanged;
        public ShortCycleTrayView CurrentView { get { return stateMachine == null ? ShortCycleTrayView.Collapsed : stateMachine.CurrentView; } }

        private void Awake()
        {
            EnsureInitialized();
        }

        private void Update() { if (stateMachine != null) stateMachine.UpdateState(); }
        private void OnDestroy() { if (stateMachine != null) stateMachine.StateChanged -= ForwardStateChanged; }
        public void ShowCollapsed() { TrySetState(ShortCycleTrayView.Collapsed); }
        public void ShowHighlight() { TrySetState(ShortCycleTrayView.Highlight); }
        public void ShowDetail() { TrySetState(ShortCycleTrayView.Detail); }

        private ShortCycleTrayStateMachine EnsureInitialized()
        {
            if (stateMachine == null)
            {
                if (presentationDirector == null)
                {
                    if (!missingHostReported)
                    {
                        Debug.LogError("ShortCycleTrayStateController requires ShortCyclePresentationDirector host wiring.", this);
                        missingHostReported = true;
                    }
                    enabled = false;
                    return null;
                }
                stateMachine = new ShortCycleTrayStateMachine(presentationDirector);
                stateMachine.StateChanged += ForwardStateChanged;
                stateMachine.Initialize();
            }
            return stateMachine;
        }

        private void TrySetState(ShortCycleTrayView view)
        {
            var machine = EnsureInitialized();
            if (machine != null) machine.TrySetState(view);
        }

        private void ForwardStateChanged(ShortCycleTrayView view)
        {
            var callback = StateChanged;
            if (callback != null) callback(view);
        }
    }
}
