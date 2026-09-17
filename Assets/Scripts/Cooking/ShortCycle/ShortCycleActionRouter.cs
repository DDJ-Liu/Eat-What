using UnityEngine;

namespace EatWhat.Cooking.ShortCycle
{
    /// <summary>Named-action router fed by the project MouseInteract framework.</summary>
    public sealed class ShortCycleActionRouter : MonoBehaviour
    {
        [SerializeField] private ShortCycleInputLockController inputLockController = null;
        private readonly ShortCycleActionRegistry registry = new ShortCycleActionRegistry();
        private ShortCycleActionDispatcher dispatcher;
        private ShortCycleSessionManager coordinator;

        public System.Collections.Generic.IList<string> RegisteredActionNames
        {
            get { return registry.RegisteredActionNames; }
        }

        public void Configure(ShortCycleSessionManager sessionCoordinator)
        {
            coordinator = sessionCoordinator;
            registry.Clear();
            registry.Register(ShortCycleActionNames.OpenCover, HandlePhase0ViewAction);
            registry.Register(ShortCycleActionNames.CloseCover, HandleCloseCover);
            registry.Register(ShortCycleActionNames.OpenCatalog, HandlePhase0ViewAction);
            registry.Register(ShortCycleActionNames.CloseCatalog, HandlePhase0ViewAction);
            registry.Register(ShortCycleActionNames.SelectRecipe, HandleSelectRecipe);
            registry.Register(ShortCycleActionNames.OpenClueBoard, HandlePhase0ViewAction);
            registry.Register(ShortCycleActionNames.CloseClueBoard, HandlePhase0ViewAction);
            registry.Register(ShortCycleActionNames.OpenStepDetail, HandlePhase0ViewAction);
            registry.Register(ShortCycleActionNames.CloseStepDetail, HandlePhase0ViewAction);
            registry.Register(ShortCycleActionNames.ToggleFavorite, HandleToggleFavorite);
            registry.Register(ShortCycleActionNames.StartCooking, HandleStartCooking);
            registry.Register(ShortCycleActionNames.Return, HandleReturn);
            registry.Register(ShortCycleActionNames.AdvanceToPhase2, HandleAdvanceToPhase2);
            registry.Register(ShortCycleActionNames.Escape, HandleEscape);
            registry.Register(ShortCycleActionNames.TrayHighlightEnter, HandleNotImplemented);
            registry.Register(ShortCycleActionNames.TrayHighlightExit, HandleNotImplemented);
            registry.Register(ShortCycleActionNames.TrayToggleDetail, HandleNotImplemented);
            registry.Register(ShortCycleActionNames.FridgeFilter, HandleNotImplemented);
            registry.Register(ShortCycleActionNames.FridgeTidy, HandleNotImplemented);
            registry.Register(ShortCycleActionNames.FridgeScroll, HandleFridgeScroll);
            registry.Register(ShortCycleActionNames.InventoryTake, HandleNotImplemented);
            registry.Register(ShortCycleActionNames.InventoryPutBack, HandleNotImplemented);
            registry.Register(ShortCycleActionNames.EquipmentOpenEntry, HandleNotImplemented);
            var gate = inputLockController == null ? new ShortCycleInputGate() : inputLockController.Gate;
            dispatcher = new ShortCycleActionDispatcher(registry, gate);
        }

        public ShortCycleActionResult DispatchAction(string actionName)
        {
            return DispatchAction(new ShortCycleActionRequest { ActionName = actionName });
        }

        public ShortCycleActionResult DispatchAction(ShortCycleActionRequest request)
        {
            return dispatcher == null
                ? ShortCycleActionResult.Failure("Short-cycle action router has not been configured.")
                : dispatcher.Dispatch(request);
        }

        private ShortCycleActionResult HandlePhase0ViewAction(ShortCycleActionRequest request)
        {
            if (coordinator == null)
            {
                return ShortCycleActionResult.Failure("Short-cycle session coordinator is unavailable.");
            }
            if (coordinator.CurrentPhase != ShortCyclePhase.Phase0)
            {
                return ShortCycleActionResult.Failure("Phase 0 navigation actions require Phase 0.");
            }
            if (request.ActionName == ShortCycleActionNames.CloseCatalog)
                return ToActionResult(coordinator.TryCloseCatalog());

            ShortCyclePhase0View targetView;
            if (!ShortCycleActionTransitionTable.TryGetTarget(
                request.ActionName,
                coordinator.CurrentPhase0View,
                out targetView))
            {
                return ShortCycleActionResult.Failure(
                    "Action '" + request.ActionName + "' is not valid from " + coordinator.CurrentPhase0View + ".");
            }

            return ToActionResult(ShowPhase0View(targetView));
        }

        private ShortCycleTransitionResult ShowPhase0View(ShortCyclePhase0View targetView)
        {
            if (targetView == ShortCyclePhase0View.RecipeBrowse) return coordinator.TryShowRecipeBrowse();
            if (targetView == ShortCyclePhase0View.RecipeCatalog) return coordinator.TryShowRecipeCatalog();
            if (targetView == ShortCyclePhase0View.ClueBoardShell) return coordinator.TryShowClueBoardShell();
            if (targetView == ShortCyclePhase0View.StepDetail) return coordinator.TryShowStepDetail();
            return ShortCycleTransitionResult.Failure("Unsupported Phase 0 view target: " + targetView + ".");
        }

        private ShortCycleActionResult HandleSelectRecipe(ShortCycleActionRequest request)
        {
            if (coordinator == null)
            {
                return ShortCycleActionResult.Failure("Short-cycle session coordinator is unavailable.");
            }
            if (coordinator.CurrentPhase != ShortCyclePhase.Phase0)
            {
                return ShortCycleActionResult.Failure("Recipe selection requires Phase 0.");
            }
            if (coordinator.CurrentPhase0View == ShortCyclePhase0View.ClueBoardShell)
            {
                return ShortCycleActionResult.Failure("Recipe selection is blocked while the clue board is expanded.");
            }
            if (request == null || string.IsNullOrEmpty(request.recipe_id))
            {
                return ShortCycleActionResult.Failure("select_recipe requires recipe_id.");
            }
            if (coordinator.PlayerUnlockManager != null &&
                !coordinator.PlayerUnlockManager.IsRecipeUnlocked(request.recipe_id))
            {
                return ShortCycleActionResult.Failure("Recipe '" + request.recipe_id + "' is locked.");
            }

            var selection = coordinator.TrySelectRecipe(request.recipe_id, false);
            return ToActionResult(selection);
        }

        private ShortCycleActionResult HandleToggleFavorite(ShortCycleActionRequest request)
        {
            if (coordinator == null || coordinator.Context == null || coordinator.PlayerProfile == null)
            {
                return ShortCycleActionResult.Failure("PlayerProfile and an active ShortCycle session are required.");
            }
            if (coordinator.CurrentPhase != ShortCyclePhase.Phase0)
            {
                return ShortCycleActionResult.Failure("Favorite changes require Phase 0.");
            }

            var recipeId = coordinator.Context.current_recipe_id;
            if (string.IsNullOrEmpty(recipeId))
            {
                return ShortCycleActionResult.Failure("No recipe is selected.");
            }

            var profile = coordinator.PlayerProfile;
            var changed = profile.IsFavorite(recipeId)
                ? profile.RemoveFavorite(recipeId)
                : profile.AddFavorite(recipeId);
            return changed
                ? ShortCycleActionResult.Success()
                : ShortCycleActionResult.Failure("Favorite state did not change for '" + recipeId + "'.");
        }

        private ShortCycleActionResult HandleStartCooking(ShortCycleActionRequest request) { return ToActionResult(coordinator == null ? null : coordinator.TryStartCooking()); }
        private ShortCycleActionResult HandleCloseCover(ShortCycleActionRequest request) { return ToActionResult(coordinator == null ? null : coordinator.TryCloseCover()); }
        private ShortCycleActionResult HandleReturn(ShortCycleActionRequest request) { return ToActionResult(coordinator == null ? null : coordinator.TryReturnToPhase0()); }
        private ShortCycleActionResult HandleAdvanceToPhase2(ShortCycleActionRequest request) { return ToActionResult(coordinator == null ? null : coordinator.TryAdvanceToPhase2()); }
        private ShortCycleActionResult HandleEscape(ShortCycleActionRequest request) { return ToActionResult(coordinator == null ? null : coordinator.TryEscape()); }

        private ShortCycleActionResult HandleFridgeScroll(ShortCycleActionRequest request)
        {
            if (coordinator == null)
            {
                return ShortCycleActionResult.Failure("Short-cycle session coordinator is unavailable.");
            }
            if (coordinator.CurrentPhase != ShortCyclePhase.Phase1)
            {
                return ShortCycleActionResult.Locked("Fridge scrolling requires Phase1.");
            }
            if (request == null || request.delta == 0f)
            {
                return ShortCycleActionResult.Failure("fridge.scroll requires a non-zero delta.");
            }

            return coordinator.ScrollFridgeTier(request.delta < 0f ? -1 : 1);
        }

        private ShortCycleActionResult HandleNotImplemented(ShortCycleActionRequest request)
        {
            var actionName = request == null ? string.Empty : request.ActionName;
            Debug.LogWarning("CK01-K NOT_IMPLEMENTED action=" + actionName, this);
            return ShortCycleActionResult.NotImplemented(actionName);
        }

        private static ShortCycleActionResult ToActionResult(ShortCycleTransitionResult transition)
        {
            if (transition == null) return ShortCycleActionResult.Failure("Short-cycle session coordinator is unavailable.");
            return transition.Succeeded ? ShortCycleActionResult.Success() : ShortCycleActionResult.Failure(transition.Error);
        }
    }
}
