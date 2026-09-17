using System;

namespace EatWhat.Cooking.ShortCycle
{
    public sealed class ShortCycleTransitionResult
    {
        public bool Succeeded;
        public string Error;
        public ShortCycleProcessResult ProcessResult;

        public static ShortCycleTransitionResult Success()
        {
            return new ShortCycleTransitionResult { Succeeded = true };
        }

        public static ShortCycleTransitionResult Failure(string error)
        {
            return new ShortCycleTransitionResult { Succeeded = false, Error = error };
        }
    }

    public abstract class ShortCycleSessionStateBase
    {
        public abstract ShortCyclePhase Phase { get; }
        public virtual void EnterState(ShortCycleStateMachine sessionManager) { }
        public virtual void UpdateState(ShortCycleStateMachine sessionManager) { }
        public virtual void ExitState(ShortCycleStateMachine sessionManager) { }
    }

    public abstract class ShortCyclePhase0StateBase
    {
        public abstract ShortCyclePhase0View View { get; }

        public virtual void EnterState(ShortCycleStateMachine sessionManager)
        {
            sessionManager.Context.SetPhase0View(View);
            if (sessionManager.PresentationHost != null)
                sessionManager.PresentationHost.ShowView(ShortCycleSpaceId.Phase0, View);
        }

        public virtual void UpdateState(ShortCycleStateMachine sessionManager) { }

        public virtual void ExitState(ShortCycleStateMachine sessionManager)
        {
            if (sessionManager.PresentationHost != null)
                sessionManager.PresentationHost.HideView(ShortCycleSpaceId.Phase0, View);
        }
    }

    public sealed class ShortCyclePhase0CoverState : ShortCyclePhase0StateBase
    {
        public override ShortCyclePhase0View View { get { return ShortCyclePhase0View.Cover; } }
    }

    public sealed class ShortCyclePhase0BrowseState : ShortCyclePhase0StateBase
    {
        public override ShortCyclePhase0View View { get { return ShortCyclePhase0View.RecipeBrowse; } }
    }

    public sealed class ShortCyclePhase0CatalogState : ShortCyclePhase0StateBase
    {
        public override ShortCyclePhase0View View { get { return ShortCyclePhase0View.RecipeCatalog; } }
    }

    public sealed class ShortCyclePhase0ClueBoardState : ShortCyclePhase0StateBase
    {
        public override ShortCyclePhase0View View { get { return ShortCyclePhase0View.ClueBoardShell; } }
    }

    public sealed class ShortCyclePhase0State : ShortCycleSessionStateBase
    {
        private readonly ShortCyclePhase0CoverState coverState = new ShortCyclePhase0CoverState();
        private readonly ShortCyclePhase0BrowseState browseState = new ShortCyclePhase0BrowseState();
        private readonly ShortCyclePhase0CatalogState catalogState = new ShortCyclePhase0CatalogState();
        private readonly ShortCyclePhase0ClueBoardState clueBoardState = new ShortCyclePhase0ClueBoardState();
        private ShortCyclePhase0StateBase currentState;

        public override ShortCyclePhase Phase { get { return ShortCyclePhase.Phase0; } }
        public ShortCyclePhase0StateBase CurrentState { get { return currentState; } }

        public override void EnterState(ShortCycleStateMachine sessionManager)
        {
            if (sessionManager.Context.current_phase == ShortCyclePhase.Phase1)
            {
                sessionManager.Context.ReturnToPhase0();
            }

            if (sessionManager.PresentationHost != null)
                sessionManager.PresentationHost.EnterSpace(ShortCycleSpaceId.Phase0);
            var next = Resolve(sessionManager.Context.phase0_view) ?? coverState;
            if (ReferenceEquals(currentState, next)) currentState.EnterState(sessionManager);
            else ChangeState(sessionManager, next, false);
        }

        public override void UpdateState(ShortCycleStateMachine sessionManager)
        {
            if (currentState != null) currentState.UpdateState(sessionManager);
        }

        public override void ExitState(ShortCycleStateMachine sessionManager)
        {
            if (sessionManager.Context.active_modal.HasValue)
            {
                if (sessionManager.PresentationHost != null)
                    sessionManager.PresentationHost.PopModal(sessionManager.Context.active_modal.Value);
                sessionManager.Context.active_modal = null;
            }
            if (currentState != null) currentState.ExitState(sessionManager);
            if (sessionManager.PresentationHost != null)
                sessionManager.PresentationHost.LeaveSpace(ShortCycleSpaceId.Phase0);
        }

        public bool TryChangeState(ShortCycleStateMachine sessionManager, ShortCyclePhase0View view)
        {
            var next = Resolve(view);
            if (next == null) return false;
            ChangeState(sessionManager, next, true);
            return true;
        }

        private void ChangeState(ShortCycleStateMachine sessionManager, ShortCyclePhase0StateBase next, bool notify)
        {
            if (ReferenceEquals(currentState, next)) return;
            if (currentState != null) currentState.ExitState(sessionManager);
            currentState = next;
            currentState.EnterState(sessionManager);
            if (notify) sessionManager.NotifyStateChanged();
        }

        private ShortCyclePhase0StateBase Resolve(ShortCyclePhase0View view)
        {
            if (view == ShortCyclePhase0View.Cover) return coverState;
            if (view == ShortCyclePhase0View.RecipeBrowse) return browseState;
            if (view == ShortCyclePhase0View.RecipeCatalog) return catalogState;
            if (view == ShortCyclePhase0View.ClueBoardShell) return clueBoardState;
            return null;
        }
    }

    public sealed class ShortCyclePhase1State : ShortCycleSessionStateBase
    {
        public override ShortCyclePhase Phase { get { return ShortCyclePhase.Phase1; } }

        public override void EnterState(ShortCycleStateMachine sessionManager)
        {
            sessionManager.Context.EnterPhase1();
            if (sessionManager.PresentationHost != null)
                sessionManager.PresentationHost.EnterSpace(ShortCycleSpaceId.Phase1);
        }

        public override void ExitState(ShortCycleStateMachine sessionManager)
        {
            if (sessionManager.PresentationHost != null)
                sessionManager.PresentationHost.LeaveSpace(ShortCycleSpaceId.Phase1);
        }
    }

    public sealed class ShortCyclePhase2HandoffState : ShortCycleSessionStateBase
    {
        public override ShortCyclePhase Phase { get { return ShortCyclePhase.Phase2; } }
    }

    /// <summary>Pure-C# FSM with orthogonal base-view and modal presentation.</summary>
    public sealed class ShortCycleStateMachine
    {
        private readonly ShortCycleContext context;
        private readonly ISaveBoundary saveBoundary;
        private readonly ShortCycleDataProcessor dataProcessor;
        private readonly IShortCycleUpstreamExit upstreamExit;
        private readonly IShortCyclePresentationHost presentationHost;
        private readonly ShortCyclePhase0State phase0State = new ShortCyclePhase0State();
        private readonly ShortCyclePhase1State phase1State = new ShortCyclePhase1State();
        private readonly ShortCyclePhase2HandoffState phase2HandoffState = new ShortCyclePhase2HandoffState();
        private ShortCycleSessionStateBase currentState;

        public ShortCycleStateMachine(
            ShortCycleContext context,
            ISaveBoundary saveBoundary,
            ShortCycleDataProcessor dataProcessor,
            IShortCycleUpstreamExit upstreamExit,
            IShortCyclePresentationHost presentationHost = null)
        {
            if (context == null) throw new ArgumentNullException("context");
            if (saveBoundary == null) throw new ArgumentNullException("saveBoundary");
            if (dataProcessor == null) throw new ArgumentNullException("dataProcessor");
            this.context = context;
            this.saveBoundary = saveBoundary;
            this.dataProcessor = dataProcessor;
            this.upstreamExit = upstreamExit;
            this.presentationHost = presentationHost;
        }

        public event Action<ShortCyclePhase, ShortCyclePhase0View> StateChanged;
        public ShortCycleContext Context { get { return context; } }
        public ShortCycleSessionStateBase CurrentState { get { return currentState; } }
        public ShortCyclePhase0StateBase CurrentPhase0State { get { return phase0State.CurrentState; } }
        public IShortCyclePresentationHost PresentationHost { get { return presentationHost; } }

        public void UpdateState() { if (currentState != null) currentState.UpdateState(this); }

        public ShortCycleTransitionResult TryEnter(string recipeId, ShortCycleTriggerContext triggerContext)
        {
            if (currentState != null || context.current_phase != ShortCyclePhase.None)
                return ShortCycleTransitionResult.Failure("Short-cycle entry is only valid from the empty context.");
            try
            {
                context.Enter(recipeId, triggerContext);
                ChangeState(phase0State);
                saveBoundary.Reach(ShortCycleSavePoint.EnterPhase0, context);
                return ShortCycleTransitionResult.Success();
            }
            catch (ArgumentException exception)
            {
                return ShortCycleTransitionResult.Failure(exception.Message);
            }
        }

        public ShortCycleTransitionResult TrySelectRecipe(string recipeId, bool isMismatchDish)
        {
            if (!ReferenceEquals(currentState, phase0State) ||
                context.phase0_view != ShortCyclePhase0View.RecipeCatalog)
                return ShortCycleTransitionResult.Failure("Recipe selection is only valid from the Phase 0 recipe catalog.");
            if (!context.SelectRecipe(recipeId, isMismatchDish))
                return ShortCycleTransitionResult.Failure("Recipe selection requires a non-empty recipe ID.");
            phase0State.TryChangeState(this, ShortCyclePhase0View.RecipeBrowse);
            return ShortCycleTransitionResult.Success();
        }

        public ShortCycleTransitionResult TryCloseCatalog()
        {
            if (!ReferenceEquals(currentState, phase0State) ||
                context.phase0_view != ShortCyclePhase0View.RecipeCatalog)
                return ShortCycleTransitionResult.Failure("close_catalog is only valid from the Phase 0 recipe catalog.");
            if (!context.HasSelectedRecipe)
                return ShortCycleTransitionResult.Failure("no_recipe");
            phase0State.TryChangeState(this, ShortCyclePhase0View.RecipeBrowse);
            return ShortCycleTransitionResult.Success();
        }

        public ShortCycleTransitionResult TryCloseCover()
        {
            if (!ReferenceEquals(currentState, phase0State) ||
                context.phase0_view != ShortCyclePhase0View.Cover)
                return ShortCycleTransitionResult.Failure("close_cover is only valid from the Phase 0 cover.");
            return TryEscape();
        }

        public ShortCycleTransitionResult TryShowPhase0View(ShortCyclePhase0View view)
        {
            if (!ReferenceEquals(currentState, phase0State))
                return ShortCycleTransitionResult.Failure("Phase 0 views can only be entered while in Phase 0.");
            if (view == ShortCyclePhase0View.StepDetail) return TryOpenModal(ShortCycleModalId.StepDetail);
            if (context.active_modal == ShortCycleModalId.StepDetail && view == ShortCyclePhase0View.RecipeBrowse)
                return TryCloseModal(ShortCycleModalId.StepDetail);
            if (view == ShortCyclePhase0View.RecipeBrowse &&
                context.phase0_view != ShortCyclePhase0View.ClueBoardShell &&
                !context.HasSelectedRecipe)
                return ShortCycleTransitionResult.Failure("no_recipe");
            return phase0State.TryChangeState(this, view)
                ? ShortCycleTransitionResult.Success()
                : ShortCycleTransitionResult.Failure("Unsupported Phase 0 view: " + view + ".");
        }

        public ShortCycleTransitionResult TryOpenModal(ShortCycleModalId modal)
        {
            if (!ReferenceEquals(currentState, phase0State) ||
                context.phase0_view != ShortCyclePhase0View.RecipeBrowse ||
                context.active_modal.HasValue)
                return ShortCycleTransitionResult.Failure("A modal can only open once while Phase 0 is active.");
            context.active_modal = modal;
            if (presentationHost != null) presentationHost.PushModal(modal);
            NotifyStateChanged();
            return ShortCycleTransitionResult.Success();
        }

        public ShortCycleTransitionResult TryCloseModal(ShortCycleModalId modal)
        {
            if (!ReferenceEquals(currentState, phase0State) || context.active_modal != modal)
                return ShortCycleTransitionResult.Failure("The requested modal is not active.");
            if (presentationHost != null) presentationHost.PopModal(modal);
            context.active_modal = null;
            NotifyStateChanged();
            return ShortCycleTransitionResult.Success();
        }

        public ShortCycleTransitionResult TryStartCooking()
        {
            if (!ReferenceEquals(currentState, phase0State) || !context.HasSelectedRecipe ||
                string.IsNullOrEmpty(context.current_recipe_id))
                return ShortCycleTransitionResult.Failure("Start cooking requires a selected recipe in Phase 0.");
            ChangeState(phase1State);
            return ShortCycleTransitionResult.Success();
        }

        public ShortCycleTransitionResult TryReturnToPhase0()
        {
            if (!ReferenceEquals(currentState, phase1State))
                return ShortCycleTransitionResult.Failure("Return is only valid from Phase 1.");
            ChangeState(phase0State);
            return ShortCycleTransitionResult.Success();
        }

        public ShortCycleTransitionResult TryAdvanceToPhase2()
        {
            if (!ReferenceEquals(currentState, phase1State))
                return ShortCycleTransitionResult.Failure("P1 -> P2 processing requires the Phase 1 state.");
            var processResult = dataProcessor.TryProcess(context);
            if (!processResult.Succeeded)
                return new ShortCycleTransitionResult { Succeeded = false, Error = processResult.Error, ProcessResult = processResult };
            ChangeState(phase2HandoffState);
            return new ShortCycleTransitionResult { Succeeded = true, ProcessResult = processResult };
        }

        public ShortCycleTransitionResult TryEscape()
        {
            if (!ReferenceEquals(currentState, phase0State))
                return ShortCycleTransitionResult.Failure("ESC is only handled by the Phase 0 exit layer.");

            if (context.active_modal.HasValue)
                return TryCloseModal(context.active_modal.Value);
            if (context.phase0_view == ShortCyclePhase0View.ClueBoardShell)
            {
                phase0State.TryChangeState(this, ShortCyclePhase0View.RecipeBrowse);
                return ShortCycleTransitionResult.Success();
            }
            if (context.phase0_view == ShortCyclePhase0View.RecipeBrowse)
            {
                phase0State.TryChangeState(this, ShortCyclePhase0View.RecipeCatalog);
                return ShortCycleTransitionResult.Success();
            }
            if (context.phase0_view == ShortCyclePhase0View.RecipeCatalog)
            {
                phase0State.TryChangeState(this, ShortCyclePhase0View.Cover);
                return ShortCycleTransitionResult.Success();
            }
            if (context.phase0_view != ShortCyclePhase0View.Cover)
                return ShortCycleTransitionResult.Failure("ESC cannot resolve the active Phase 0 view.");
            if (context.trigger_context != ShortCycleTriggerContext.FreeTry)
                return ShortCycleTransitionResult.Failure("This trigger context requires a future confirmation flow.");
            Teardown();
            return ShortCycleTransitionResult.Success();
        }

        public void Teardown()
        {
            if (currentState != null)
            {
                var space = currentState.Phase == ShortCyclePhase.Phase1 ? ShortCycleSpaceId.Phase1 : ShortCycleSpaceId.Phase0;
                if (presentationHost != null)
                {
                    presentationHost.LeaveSpace(space);
                    presentationHost.ClearSessionPresentation();
                }
                currentState = null;
            }
            if (upstreamExit != null) upstreamExit.ExitToUpstream(context.trigger_context);
            context.ExitShortCycle();
            NotifyStateChanged();
        }

        internal void NotifyStateChanged()
        {
            var callback = StateChanged;
            if (callback != null) callback(context.current_phase, context.phase0_view);
        }

        private void ChangeState(ShortCycleSessionStateBase next)
        {
            if (ReferenceEquals(currentState, next)) return;
            if (currentState != null) currentState.ExitState(this);
            currentState = next;
            currentState.EnterState(this);
            NotifyStateChanged();
        }
    }
}
