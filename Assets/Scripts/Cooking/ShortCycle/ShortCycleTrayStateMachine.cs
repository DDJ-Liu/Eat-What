using System;

namespace EatWhat.Cooking.ShortCycle
{
    public abstract class ShortCycleTrayStateBase
    {
        public abstract ShortCycleTrayView View { get; }
        public virtual void EnterState(ShortCycleTrayStateMachine trayStateMachine)
        {
            if (trayStateMachine.PresentationHost != null)
                trayStateMachine.PresentationHost.SetTrayView(View);
        }
        public virtual void UpdateState(ShortCycleTrayStateMachine trayStateMachine) { }
        public virtual void ExitState(ShortCycleTrayStateMachine trayStateMachine) { }
    }

    public sealed class ShortCycleTrayCollapsedState : ShortCycleTrayStateBase
    {
        public override ShortCycleTrayView View { get { return ShortCycleTrayView.Collapsed; } }
    }

    public sealed class ShortCycleTrayHighlightState : ShortCycleTrayStateBase
    {
        public override ShortCycleTrayView View { get { return ShortCycleTrayView.Highlight; } }
    }

    public sealed class ShortCycleTrayDetailState : ShortCycleTrayStateBase
    {
        public override ShortCycleTrayView View { get { return ShortCycleTrayView.Detail; } }
    }

    /// <summary>
    /// IngredientTrayStateBase-compatible explicit sub-state machine. Visual
    /// tuning remains in C-T5; this class owns the auditable three-state table.
    /// </summary>
    public sealed class ShortCycleTrayStateMachine
    {
        private readonly IShortCyclePresentationHost presentationHost;
        private readonly ShortCycleTrayCollapsedState collapsedState = new ShortCycleTrayCollapsedState();
        private readonly ShortCycleTrayHighlightState highlightState = new ShortCycleTrayHighlightState();
        private readonly ShortCycleTrayDetailState detailState = new ShortCycleTrayDetailState();
        private ShortCycleTrayStateBase currentState;

        public event Action<ShortCycleTrayView> StateChanged;

        public ShortCycleTrayStateMachine(IShortCyclePresentationHost presentationHost = null)
        {
            this.presentationHost = presentationHost;
        }

        public IShortCyclePresentationHost PresentationHost { get { return presentationHost; } }

        public ShortCycleTrayStateBase CurrentState { get { return currentState; } }
        public ShortCycleTrayView CurrentView
        {
            get { return currentState == null ? ShortCycleTrayView.Collapsed : currentState.View; }
        }

        public void Initialize()
        {
            ChangeState(collapsedState);
        }

        public void UpdateState()
        {
            if (currentState != null)
            {
                currentState.UpdateState(this);
            }
        }

        public bool TrySetState(ShortCycleTrayView view)
        {
            var next = Resolve(view);
            if (next == null)
            {
                return false;
            }

            ChangeState(next);
            return true;
        }

        private void ChangeState(ShortCycleTrayStateBase next)
        {
            if (ReferenceEquals(currentState, next))
            {
                return;
            }

            if (currentState != null)
            {
                currentState.ExitState(this);
            }
            currentState = next;
            currentState.EnterState(this);
            var callback = StateChanged;
            if (callback != null)
            {
                callback(currentState.View);
            }
        }

        private ShortCycleTrayStateBase Resolve(ShortCycleTrayView view)
        {
            if (view == ShortCycleTrayView.Collapsed) return collapsedState;
            if (view == ShortCycleTrayView.Highlight) return highlightState;
            if (view == ShortCycleTrayView.Detail) return detailState;
            return null;
        }
    }

}
