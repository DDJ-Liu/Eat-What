using System;
using EatWhat.Tools.Localization;
using UnityEngine;

namespace EatWhat.Cooking.ShortCycle
{
    public sealed class ShortCyclePhase2TransitionAdapter : MonoBehaviour
    {
        public event Action<ShortCycleDataHandoff> Requested;

        public void RequestTransition(ShortCycleDataHandoff handoff)
        {
            var callback = Requested;
            if (callback != null) callback(handoff);
        }
    }

    /// <summary>Scene owner for the pure-C# short-cycle FSM and its boundaries.</summary>
    public sealed class ShortCycleSessionManager : MonoBehaviour
    {
        [SerializeField] private InventoryManager inventoryManager = null;
        [SerializeField] private PlayerProfile playerProfile = null;
        [SerializeField] private PlayerUnlockManager playerUnlockManager = null;
        [SerializeField] private KitchenEquipmentManager kitchenEquipmentManager = null;
        [SerializeField] private ShortCycleActionRouter actionRouter = null;
        [SerializeField] private ShortCyclePresentationDirector presentationDirector = null;
        [SerializeField] private CK01GeneratedDataCatalog dataCatalog = null;
        [SerializeField] private ShortCycleFridgeBinder fridgeBinder = null;
        [SerializeField] private ShortCycleClueBoardBinder clueBoardBinder = null;
        [SerializeField] private string defaultRecipeId = "rcp_tomato_egg";
        [SerializeField, Tooltip("G26/VERIFY-TEMP only: opt in to the default recipe entry hint while debugging.")]
        private bool useDefaultRecipeForDebug = false;
        [SerializeField] private ShortCycleTriggerContext defaultTriggerContext = ShortCycleTriggerContext.FreeTry;
        [SerializeField] private ShortCyclePhase2HandoffPresenter phase2HandoffPresenter = null;
        [SerializeField] private ShortCycleUpstreamExitPresenter upstreamExitPresenter = null;

        private ObservableNoOpSaveBoundary saveBoundary;
        private ShortCycleStateMachine stateMachine;

        public event Action<ShortCyclePhase, ShortCyclePhase0View> StateChanged;
        public ShortCycleContext Context { get { return stateMachine == null ? null : stateMachine.Context; } }
        public PlayerProfile PlayerProfile { get { return playerProfile; } }
        public PlayerUnlockManager PlayerUnlockManager { get { return playerUnlockManager; } }
        public KitchenEquipmentManager KitchenEquipmentManager { get { return kitchenEquipmentManager; } }
        public ObservableNoOpSaveBoundary SaveBoundary { get { return saveBoundary; } }
        public ShortCyclePhase CurrentPhase { get { return Context == null ? ShortCyclePhase.None : Context.current_phase; } }
        public ShortCyclePhase0View CurrentPhase0View { get { return Context == null ? ShortCyclePhase0View.Cover : Context.phase0_view; } }
        public ShortCycleModalId? ActiveModal { get { return Context == null ? null : Context.active_modal; } }
        public string CurrentStateName { get { return stateMachine == null || stateMachine.CurrentState == null ? null : stateMachine.CurrentState.GetType().Name; } }

        private void Awake()
        {
            saveBoundary = new ObservableNoOpSaveBoundary();
            var inventoryGateway = inventoryManager == null
                ? new UnavailableInventoryConsumptionGateway("InventoryManager is not connected to generated Items assets.")
                : (IInventoryConsumptionGateway)inventoryManager;
            var phase2Sink = phase2HandoffPresenter == null
                ? new UnavailablePhase2HandoffSink("Phase 2 handoff presenter is not connected.")
                : (IShortCyclePhase2HandoffSink)phase2HandoffPresenter;
            var processor = new ShortCycleDataProcessor(inventoryGateway, saveBoundary, phase2Sink);
            stateMachine = new ShortCycleStateMachine(
                new ShortCycleContext(), saveBoundary, processor, upstreamExitPresenter, presentationDirector);
            stateMachine.StateChanged += HandleStateChanged;
            if (actionRouter != null) actionRouter.Configure(this);
        }

        private void Start()
        {
            var result = BeginSession(
                useDefaultRecipeForDebug ? defaultRecipeId : null,
                defaultTriggerContext);
            if (!result.Succeeded)
                Debug.LogError("CK01-K session startup failed: " + result.Error, this);
        }

        private void Update() { if (stateMachine != null) stateMachine.UpdateState(); }

        private void OnDestroy()
        {
            if (stateMachine != null) stateMachine.StateChanged -= HandleStateChanged;
        }

        public ShortCycleTransitionResult BeginSession(string recipeId, ShortCycleTriggerContext triggerContext)
        {
            var result = RunBeginSessionSequence(
                () => ValidateBeginSession(recipeId),
                () => BindFridge(),
                () => BindClueBoard(recipeId),
                () =>
                {
                    LocalizedText.RefreshAll(gameObject.scene);
                    return ShortCycleTransitionResult.Success();
                },
                () => stateMachine.TryEnter(recipeId, triggerContext),
                message => Debug.Log(message, this),
                true);
            if (result.Succeeded)
                Debug.Log("[CK01-C-SESSION] ready recipe=" + Context.current_recipe_id +
                    " phase=" + CurrentPhase + " view=" + CurrentPhase0View, this);
            return result;
        }

        public ShortCycleTransitionResult TryBeginSession(string recipeId, ShortCycleTriggerContext triggerContext)
        {
            return BeginSession(recipeId, triggerContext);
        }

        public ShortCycleTransitionResult TrySelectRecipe(string recipeId, bool isMismatchDish)
        {
            var missing = RequireStateMachine();
            if (missing != null) return missing;

            var selection = stateMachine.TrySelectRecipe(recipeId, isMismatchDish);
            if (!selection.Succeeded) return selection;

            var fridge = BindFridge();
            if (!fridge.Succeeded) return fridge;
            var clueBoard = BindClueBoard(recipeId);
            return clueBoard.Succeeded ? selection : clueBoard;
        }

        public ShortCycleTransitionResult TryShowCover() { return TryShowPhase0View(ShortCyclePhase0View.Cover); }
        public ShortCycleTransitionResult TryShowRecipeBrowse() { return TryShowPhase0View(ShortCyclePhase0View.RecipeBrowse); }
        public ShortCycleTransitionResult TryShowRecipeCatalog() { return TryShowPhase0View(ShortCyclePhase0View.RecipeCatalog); }
        public ShortCycleTransitionResult TryShowClueBoardShell() { return TryShowPhase0View(ShortCyclePhase0View.ClueBoardShell); }
        public ShortCycleTransitionResult TryShowStepDetail() { return TryShowPhase0View(ShortCyclePhase0View.StepDetail); }

        public ShortCycleTransitionResult TryStartCooking()
        {
            var missing = RequireStateMachine();
            return missing ?? stateMachine.TryStartCooking();
        }

        public ShortCycleTransitionResult TryReturnToPhase0()
        {
            var missing = RequireStateMachine();
            return missing ?? stateMachine.TryReturnToPhase0();
        }

        public ShortCycleTransitionResult TryAdvanceToPhase2()
        {
            var missing = RequireStateMachine();
            return missing ?? stateMachine.TryAdvanceToPhase2();
        }

        public ShortCycleTransitionResult TryEscape()
        {
            var missing = RequireStateMachine();
            return missing ?? stateMachine.TryEscape();
        }

        public ShortCycleTransitionResult TryCloseCatalog()
        {
            var missing = RequireStateMachine();
            return missing ?? stateMachine.TryCloseCatalog();
        }

        public ShortCycleTransitionResult TryCloseCover()
        {
            var missing = RequireStateMachine();
            return missing ?? stateMachine.TryCloseCover();
        }

        internal ShortCycleActionResult ScrollFridgeTier(int sign)
        {
            return fridgeBinder == null
                ? ShortCycleActionResult.Failure("ShortCycle fridge binder is not connected.")
                : fridgeBinder.ScrollTier(sign);
        }

        private ShortCycleTransitionResult TryShowPhase0View(ShortCyclePhase0View view)
        {
            var missing = RequireStateMachine();
            return missing ?? stateMachine.TryShowPhase0View(view);
        }

        private ShortCycleTransitionResult RequireStateMachine()
        {
            return stateMachine == null
                ? ShortCycleTransitionResult.Failure("Short-cycle session manager has not completed Awake().")
                : null;
        }

        private ShortCycleTransitionResult ValidateBeginSession(string recipeId)
        {
            var missing = RequireStateMachine();
            if (missing != null) return missing;
            if (Context != null && Context.current_phase != ShortCyclePhase.None)
                return ShortCycleTransitionResult.Failure("A short-cycle session is already active.");
            if (dataCatalog == null)
                return ShortCycleTransitionResult.Failure("CK01 generated data catalog is not connected to ShortCycleSessionManager.");
            if (inventoryManager == null || inventoryManager.DataCatalog != dataCatalog)
                return ShortCycleTransitionResult.Failure("InventoryManager must use the same CK01 generated data catalog as ShortCycleSessionManager.");
            if (fridgeBinder == null || clueBoardBinder == null)
                return ShortCycleTransitionResult.Failure("ShortCycle fridge and clue-board binders must both be connected.");
            if (presentationDirector == null)
                return ShortCycleTransitionResult.Failure("ShortCyclePresentationDirector is required before session startup.");

            if (string.IsNullOrEmpty(recipeId))
                return ShortCycleTransitionResult.Success();

            CK01RecipeData ignored;
            string diagnostic;
            return dataCatalog.TryGetRecipe(recipeId, out ignored, out diagnostic)
                ? ShortCycleTransitionResult.Success()
                : ShortCycleTransitionResult.Failure(diagnostic);
        }

        private ShortCycleTransitionResult BindFridge()
        {
            string diagnostic = null;
            return fridgeBinder != null && fridgeBinder.Bind(dataCatalog, inventoryManager, out diagnostic)
                ? ShortCycleTransitionResult.Success()
                : ShortCycleTransitionResult.Failure(diagnostic ?? "ShortCycle fridge binding is unavailable.");
        }

        private ShortCycleTransitionResult BindClueBoard(string recipeId)
        {
            string diagnostic = null;
            return clueBoardBinder != null && clueBoardBinder.Bind(dataCatalog, recipeId, inventoryManager, out diagnostic)
                ? ShortCycleTransitionResult.Success()
                : ShortCycleTransitionResult.Failure(diagnostic ?? "ShortCycle clue-board binding is unavailable.");
        }

        internal static ShortCycleTransitionResult RunBeginSessionSequence(
            Func<ShortCycleTransitionResult> catalogReady,
            Func<ShortCycleTransitionResult> bindFridge,
            Func<ShortCycleTransitionResult> bindClueBoard,
            Func<ShortCycleTransitionResult> refreshLocalization,
            Func<ShortCycleTransitionResult> enterStateMachine,
            Action<string> log = null,
            bool skipClueBoard = false)
        {
            var steps = new[] { catalogReady, bindFridge, bindClueBoard, refreshLocalization, enterStateMachine };
            var names = new[] { "Catalog", "FridgeBind", "ClueBoardBind", "LocRefresh", "FsmEnter" };
            for (var index = 0; index < steps.Length; index++)
            {
                if (index == 2 && skipClueBoard)
                {
                    if (log != null) log("[CK01-C-SESSION] step=3/5 ClueBoardBind skipped(no recipe)");
                    continue;
                }
                if (steps[index] == null)
                    return ShortCycleTransitionResult.Failure("Short-cycle startup step " + index + " is not configured.");
                var result = steps[index]();
                if (result == null || !result.Succeeded)
                    return result ?? ShortCycleTransitionResult.Failure("Short-cycle startup step " + index + " returned no result.");
                if (log != null) log("[CK01-C-SESSION] step=" + (index + 1) + "/5 " + names[index] + " ok");
            }
            return ShortCycleTransitionResult.Success();
        }

        private void HandleStateChanged(ShortCyclePhase phase, ShortCyclePhase0View phase0View)
        {
            var callback = StateChanged;
            if (callback != null) callback(phase, phase0View);
        }
    }
}
