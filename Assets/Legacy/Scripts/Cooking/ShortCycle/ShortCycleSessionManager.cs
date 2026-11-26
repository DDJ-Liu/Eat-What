using System.IO;
using EatWhat.Tools.Persistence;
using EatWhat.Tools.Security;
using System;
using System.Globalization;
using EatWhat.Tools.Localization;
using UnityEngine;

namespace EatWhat.Cooking.ShortCycle
{
    /// <summary>Scene owner for the pure-C# short-cycle FSM and its boundaries.</summary>
    public sealed class ShortCycleSessionManager : MonoBehaviour,
        IShortCycleCarrierQuery, IShortCycleCarrierWarningLog
    {
        [SerializeField] private InventoryManager inventoryManager = null;
        [SerializeField] private PlayerProfile playerProfile = null;
        [SerializeField] private PlayerUnlockManager playerUnlockManager = null;
        [SerializeField] private KitchenEquipmentManager kitchenEquipmentManager = null;
        [SerializeField] private ShortCycleActionRouter actionRouter = null;
        [SerializeField] private ShortCyclePresentationDirector presentationDirector = null;
        [SerializeField] private CK01GeneratedDataCatalog dataCatalog = null;
        [SerializeField] private ShortCycleFridgeBinder fridgeBinder = null;
        [SerializeField] private ShortCycleTrayInteraction trayInteraction = null;
        [SerializeField] private ShortCycleClueBoardBinder clueBoardBinder = null;
        [SerializeField] private RecipeBookBinder recipeBookBinder = null;
        [SerializeField] private string defaultRecipeId = "rcp_tomato_egg";
        [SerializeField, Tooltip("G26/VERIFY-TEMP only: opt in to the default recipe entry hint while debugging.")]
        private bool useDefaultRecipeForDebug = false;
        [SerializeField] private ShortCycleTriggerContext defaultTriggerContext = ShortCycleTriggerContext.FreeTry;
        [SerializeField] private ShortCyclePhase2HandoffPresenter phase2HandoffPresenter = null;
        [SerializeField] private ShortCycleUpstreamExitPresenter upstreamExitPresenter = null;

        private ObservableNoOpSaveBoundary saveBoundary;
        private SaveBoundary persistenceBoundary;
        private ShortCycleStateMachine stateMachine;
        private ShortCycleCarrierWarning carrierWarning;
        private PlayerProfileManager profileManager;
        public PlayerProfileManager ProfileManager
        { get { if (profileManager == null && playerProfile != null) profileManager = new PlayerProfileManager(playerProfile); return profileManager; } }
        public ShortCycleActionResult TryToggleFavorite()
        {
            if (CurrentPhase != ShortCyclePhase.Phase0 || Context == null || !Context.HasSelectedRecipe || ProfileManager == null)
                return ShortCycleActionResult.Failure("Favorite changes require a selected Phase0 recipe and the existing profile.");
            ProfileManager.ToggleFavorite(Context.current_recipe_id);
            RefreshFavoriteContent();
            return ShortCycleActionResult.Success();
        }
        public void RefreshFavoriteContent()
        { if (recipeBookBinder != null) recipeBookBinder.RefreshFavorites(id => ProfileManager != null && ProfileManager.IsFavorite(id)); }
        public ShortCycleTransitionResult ApplyProfileSnapshot(PlayerProfileSnapshot snapshot)
        {
            if (CurrentPhase != ShortCyclePhase.None || ProfileManager == null)
                return ShortCycleTransitionResult.Failure("Apply profile snapshots before a session starts.");
            ProfileManager.ApplySnapshot(snapshot);
            if (playerUnlockManager != null) playerUnlockManager.Configure(dataCatalog, playerProfile.UnlockedRecipeIds);
            if (recipeBookBinder != null) recipeBookBinder.Clear();
            return ShortCycleTransitionResult.Success();
        }

        public event Action<ShortCyclePhase, ShortCyclePhase0View> StateChanged;
        public ShortCycleContext Context { get { return stateMachine == null ? null : stateMachine.Context; } }
        public RecipeBookBinder RecipeBook { get { return recipeBookBinder; } }
        public System.Collections.Generic.IReadOnlyList<RecipeView> GetRecipeViews()
        {
            var result = new System.Collections.Generic.List<RecipeView>();
            foreach (var view in dataCatalog.EnumerateRecipesOrdered())
                result.Add(view.WithUnlocked(IsRecipeUnlocked(view.recipe_id)));
            return result.AsReadOnly();
        }
        public bool IsRecipeUnlocked(string recipeId)
        { return new UnlockManager(dataCatalog, playerProfile == null ? new string[0] : playerProfile.UnlockedRecipeIds).IsUnlocked(recipeId); }

        public InventoryManager Inventory { get { return inventoryManager; } }
        public ShortCycleTrayInteraction TrayInteraction { get { return trayInteraction; } }
        public PlayerProfile PlayerProfile { get { return playerProfile; } }
        public PlayerUnlockManager PlayerUnlockManager { get { return playerUnlockManager; } }
        public KitchenEquipmentManager KitchenEquipmentManager { get { return kitchenEquipmentManager; } }
        public ObservableNoOpSaveBoundary SaveBoundary { get { return saveBoundary; } }
        public ShortCyclePhase CurrentPhase { get { return Context == null ? ShortCyclePhase.None : Context.current_phase; } }
        public ShortCyclePhase0View CurrentPhase0View { get { return Context == null ? ShortCyclePhase0View.Cover : Context.phase0_view; } }
        public ShortCycleModalId? ActiveModal { get { return Context == null ? null : Context.active_modal; } }
        public string CurrentStateName { get { return stateMachine == null || stateMachine.CurrentState == null ? null : stateMachine.CurrentState.GetType().Name; } }

        internal CK01GeneratedDataCatalog ValidationCatalog { get { return dataCatalog; } }
        internal bool PersistenceAwakeStarted { get; private set; }
        internal bool PersistenceEnded { get; private set; }
        internal bool PersistenceIsolationRequired { get; private set; }
        internal void RequirePersistenceIsolation() { PersistenceIsolationRequired = true; }
        internal void DisableIsolatedStorage() { if (persistenceBoundary != null) persistenceBoundary.DisableStorage(); }
        private void Awake()
        {
            if (PersistenceAwakeStarted) throw new InvalidOperationException("Session Awake cannot run twice.");
            PersistenceAwakeStarted = true;
            try { InitializeSession(); }
            catch { PersistenceEnded = true; DisableIsolatedStorage(); ShortCycleSaveIsolation.ReleaseTarget(this); throw; }
        }
        private void InitializeSession()
        {
            saveBoundary = new ObservableNoOpSaveBoundary();
            persistenceBoundary = new SaveBoundary(saveBoundary, () => ProfileManager, ShortCycleSaveIsolation.Resolve(this, () => {
                var keys = new ObfuscatedStaticKeyProvider();
                return new JsonSaveStore(Path.Combine(Application.persistentDataPath, "saves"), SaveCodecFactory.CreateForCurrentBuild(keys), new EnvelopeAuthenticator(keys));
            }), message => Debug.LogWarning("[SAVE-01] " + message, this));
            carrierWarning = new ShortCycleCarrierWarning(this);
            var inventoryGateway = inventoryManager == null
                ? new UnavailableInventoryConsumptionGateway("InventoryManager is not connected to generated Items assets.")
                : (IInventoryConsumptionGateway)inventoryManager;
            var phase2Sink = phase2HandoffPresenter == null
                ? new UnavailablePhase2HandoffSink("Phase 2 handoff presenter is not connected.")
                : (IShortCyclePhase2HandoffSink)phase2HandoffPresenter;
            var processor = new ShortCycleDataProcessor(inventoryGateway, persistenceBoundary, phase2Sink);
            stateMachine = new ShortCycleStateMachine(
                new ShortCycleContext(), persistenceBoundary, processor, upstreamExitPresenter, presentationDirector);
            stateMachine.StateChanged += HandleStateChanged;
            if (phase2HandoffPresenter != null) phase2HandoffPresenter.Configure(this, inventoryManager, presentationDirector);
            if (actionRouter != null) actionRouter.Configure(this);
        }

        private void Start()
        {
            var scrollDiagnostic = ConfigureFridgeScrollInput(dataCatalog, MouseManager.Instance);
            if (!string.IsNullOrEmpty(scrollDiagnostic))
                Debug.LogWarning("[CK01-C-SESSION] " + scrollDiagnostic, this);

            var result = BeginSession(
                useDefaultRecipeForDebug ? defaultRecipeId : null,
                defaultTriggerContext);
            if (!result.Succeeded)
                Debug.LogError("CK01-K session startup failed: " + result.Error, this);
        }

        internal static string ConfigureFridgeScrollInput(
            CK01GeneratedDataCatalog catalog,
            MouseManager mouseManager)
        {
            string diagnostic;
            CK01FridgeScrollSettings settings;
            if (catalog == null)
            {
                settings = new CK01FridgeScrollSettings(
                    CK01CookingGameConfigData.DefaultFridgeScrollThreshold,
                    CK01CookingGameConfigData.DefaultFridgeScrollCooldown);
                diagnostic = "CookingGameConfig catalog is not connected; using fridge scroll defaults " +
                    "threshold=1 and cooldown=0.2.";
            }
            else
            {
                settings = catalog.ResolveFridgeScrollSettings(out diagnostic);
            }

            if (mouseManager == null)
            {
                var missingManager = "MouseManager is unavailable; fridge scroll settings were not applied.";
                return string.IsNullOrEmpty(diagnostic) ? missingManager : diagnostic + " " + missingManager;
            }

            mouseManager.ConfigureScrollInput(settings.Threshold, settings.Cooldown);
            return diagnostic;
        }

        private void Update() { if (stateMachine != null) stateMachine.UpdateState(); }

        private void OnDestroy()
        {
            PersistenceEnded = true;
            if (phase2HandoffPresenter != null) phase2HandoffPresenter.Teardown();
            DisableIsolatedStorage(); ShortCycleSaveIsolation.ReleaseTarget(this);
            if (stateMachine != null) stateMachine.StateChanged -= HandleStateChanged;
            if (carrierWarning != null) carrierWarning.ResetSession();
            if (recipeBookBinder != null) recipeBookBinder.Clear();
        }

        public bool TryResolveCarrier(string recipeId, string areaId, string itemId,
            System.Collections.Generic.IEnumerable<string> sceneCarriers,
            out ShortCycleCarrierResolution resolution, out string diagnostic)
        {
            resolution = null;
            if (dataCatalog == null)
            {
                diagnostic = "CK01 generated data catalog is not connected.";
                return false;
            }

            RecipeWhitelist whitelist;
            AreaConstraint area;
            if (!dataCatalog.TryGetRecipeWhitelist(recipeId, out whitelist, out diagnostic) ||
                !dataCatalog.TryGetAreaConstraint(areaId, out area, out diagnostic))
                return false;

            var records = new System.Collections.Generic.List<ShortCycleCarrierRecord>();
            foreach (var recordId in whitelist.ProcessingRecordIds)
            {
                CK01ProcessingRecordData row;
                if (!dataCatalog.TryGetProcessingRecord(recordId, out row, out diagnostic))
                    return false;
                records.Add(new ShortCycleCarrierRecord(recordId,
                    new[] { row.input_01, row.input_02, row.input_03,
                        row.input_04, row.input_05, row.input_06 },
                    row.action, row.carrier));
            }

            resolution = new ShortCycleCarrierResolver(whitelist, area, records)
                .Resolve(recipeId, areaId, itemId, sceneCarriers);
            if (carrierWarning == null) carrierWarning = new ShortCycleCarrierWarning(this);
            carrierWarning.Report(recipeId, areaId, itemId, resolution);
            diagnostic = null;
            return true;
        }

        public void LogWarning(string message)
        {
            Debug.LogWarning(message, this);
        }

        public ShortCycleTransitionResult BeginSession(string recipeId, ShortCycleTriggerContext triggerContext)
        {
            if (persistenceBoundary != null) persistenceBoundary.PrepareBeforeSession(CurrentPhase == ShortCyclePhase.None);
            var result = RunBeginSessionSequence(
                () => {
                    var validation = ValidateBeginSession(recipeId);
                    return validation.Succeeded ? BindRecipeBook(recipeId) : validation;
                },
                () => BindFridge(),
                () => BindClueBoard(recipeId),
                () =>
                {
                    LocalizedText.RefreshAll(gameObject.scene);
                    return ShortCycleTransitionResult.Success();
                },
                () => stateMachine.TryEnter(recipeId, triggerContext),
                message => Debug.Log(message, this),
                string.IsNullOrEmpty(recipeId));
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

            RecipeView view; string diagnostic;
            if (dataCatalog == null || !dataCatalog.TryGetRecipeView(recipeId, out view, out diagnostic))
                return ShortCycleTransitionResult.Failure("Recipe view is unavailable: " + recipeId);
            if (!IsRecipeUnlocked(recipeId))
                return ShortCycleTransitionResult.Failure("Recipe is locked: " + recipeId);
            if (recipeBookBinder == null || !recipeBookBinder.CanBindBrowse(view, out diagnostic))
                return ShortCycleTransitionResult.Failure("Recipe browse content is not ready.");
            if (trayInteraction != null)
            {
                int configuredCapacity;
                if (!TryGetPrepCapacity(out configuredCapacity, out diagnostic) ||
                    !trayInteraction.CanPresent(configuredCapacity, out diagnostic))
                    return ShortCycleTransitionResult.Failure(diagnostic);
            }            var recipeChanged = Context == null || !string.Equals(Context.current_recipe_id, recipeId, StringComparison.Ordinal);
            var selection = stateMachine.TrySelectRecipe(recipeId, isMismatchDish);
            if (!selection.Succeeded) return selection;
            if (recipeChanged && fridgeBinder != null) fridgeBinder.ResetOrder();

            var fridge = BindFridge();
            if (!fridge.Succeeded) return fridge;
            if (trayInteraction != null)
            {
                int prepCapacity; string prepDiagnostic;
                if (TryGetPrepCapacity(out prepCapacity, out prepDiagnostic))
                    trayInteraction.Refresh(Context, inventoryManager, prepCapacity);
            }
            recipeBookBinder.BindBrowse(view.WithUnlocked(true));
            RefreshFavoriteContent();
            var clueBoard = BindClueBoard(recipeId);
            return clueBoard.Succeeded ? selection : clueBoard;
        }

        public ShortCycleActionResult TryTakeItem(string itemId, string instanceId)
        {
            string diagnostic;
            int capacity;
            if (!TryPrepareTrayAction(itemId, out capacity, out diagnostic))
                return ShortCycleActionResult.Failure(diagnostic);
            CK01ItemData item;
            if (!inventoryManager.TryGetGeneratedItem(itemId, out item) || item == null)
                return ShortCycleActionResult.Failure("Generated Items does not contain " + itemId + ".");
            if (item.stack_type == "pack" && item.pack_size <= 0)
                return ShortCycleActionResult.Failure("Pack unit has no positive pack_size.");
            if (item.stack_type == "container" &&
                (item.container_capacity <= 0f || item.container_use_step <= 0f))
                return ShortCycleActionResult.Failure("Container unit has no positive capacity/use step.");
            var beforePrep = CopyPrep();
            var beforeStates = Context.CopyPlaceholderStates();
            var result = ShortCycleTrayReservation.TryTake(Context, itemId, instanceId,
                item.stack_type, inventoryManager.GetQuantity(itemId), capacity);
            if (!result.Succeeded) return result;
            if (!RefreshPhase1Presentation(capacity, out diagnostic))
            {
                RestoreReservations(beforePrep, beforeStates, capacity);
                return ShortCycleActionResult.Failure(diagnostic);
            }
            trayInteraction.StateController.ShowCollapsed();
            return result;
        }

        public ShortCycleActionResult TryPutBackItem(string itemId, string instanceId)
        {
            string diagnostic;
            int capacity;
            if (!TryPrepareTrayAction(itemId, out capacity, out diagnostic))
                return ShortCycleActionResult.Failure(diagnostic);
            var beforePrep = CopyPrep();
            var beforeStates = Context.CopyPlaceholderStates();
            var result = ShortCycleTrayReservation.TryPutBack(Context, itemId, instanceId);
            if (!result.Succeeded) return result;
            if (!RefreshPhase1Presentation(capacity, out diagnostic))
            {
                RestoreReservations(beforePrep, beforeStates, capacity);
                return ShortCycleActionResult.Failure(diagnostic);
            }
            trayInteraction.StateController.ShowCollapsed();
            return result;
        }

        public ShortCycleActionResult TryFilterFridge(string tagId)
        {
            if (CurrentPhase != ShortCyclePhase.Phase1 || Context == null || Context.active_modal.HasValue ||
                dataCatalog == null || inventoryManager == null || fridgeBinder == null)
                return ShortCycleActionResult.Failure("Phase1 fridge filter requires its catalog, inventory and binder.");
            var nextTag = Context.current_filter_tag == tagId ? null : tagId;
            if (!string.IsNullOrEmpty(nextTag))
            {
                CK01TagData tag; string lookup;
                if (!dataCatalog.TryGetTag(nextTag, out tag, out lookup) || tag == null ||
                    tag.tag_class != "ingredient" || tag.filter_order <= 0)
                    return ShortCycleActionResult.Failure("Fridge filter requires a configured ingredient tag.");
            }
            string diagnostic;
            if (!fridgeBinder.BindView(dataCatalog, inventoryManager, nextTag,
                fridgeBinder.IsTidied, Context, out diagnostic))
                return ShortCycleActionResult.Failure(diagnostic);
            Context.current_filter_tag = nextTag;
            Context.current_scroll_offset = fridgeBinder.CurrentTier;
            return ShortCycleActionResult.Success();
        }

        public ShortCycleActionResult TryTidyFridge()
        {
            if (CurrentPhase != ShortCyclePhase.Phase1 || Context == null || Context.active_modal.HasValue ||
                dataCatalog == null || inventoryManager == null || fridgeBinder == null)
                return ShortCycleActionResult.Failure("Phase1 tidy requires its catalog, inventory and binder.");
            string diagnostic;
            if (!fridgeBinder.BindView(dataCatalog, inventoryManager, Context.current_filter_tag,
                true, Context, out diagnostic)) return ShortCycleActionResult.Failure(diagnostic);
            Context.current_scroll_offset = fridgeBinder.CurrentTier;
            return ShortCycleActionResult.Success();
        }

        public ShortCycleActionResult TrySetTrayView(ShortCycleTrayView target)
        {
            var controller = trayInteraction == null ? null : trayInteraction.StateController;
            if (CurrentPhase != ShortCyclePhase.Phase1 || Context.active_modal.HasValue || controller == null)
                return ShortCycleActionResult.Failure("Phase1 tray state controller is not connected.");
            if (target == ShortCycleTrayView.Collapsed) controller.ShowCollapsed();
            else if (target == ShortCycleTrayView.Highlight) controller.ShowHighlight();
            else if (target == ShortCycleTrayView.Detail) controller.ShowDetail();
            else return ShortCycleActionResult.Failure("Unknown tray view.");
            return controller.CurrentView == target ? ShortCycleActionResult.Success() :
                ShortCycleActionResult.Failure("Tray presentation did not enter the requested view.");
        }

        public ShortCycleActionResult TryToggleTrayDetail()
        {
            var controller = trayInteraction == null ? null : trayInteraction.StateController;
            if (CurrentPhase != ShortCyclePhase.Phase1 || Context.active_modal.HasValue || controller == null)
                return ShortCycleActionResult.Failure("Phase1 tray state controller is not connected.");
            return TrySetTrayView(controller.CurrentView == ShortCycleTrayView.Detail
                ? ShortCycleTrayView.Collapsed : ShortCycleTrayView.Detail);
        }

        public ShortCycleTransitionResult TryOpenEquipmentEntry()
        {
            var missing = RequireStateMachine();
            return missing ?? stateMachine.TryOpenModal(ShortCycleModalId.Generic);
        }

        private bool TryGetPrepCapacity(out int capacity, out string diagnostic)
        {
            capacity = 0;
            CK01CookingGameConfigData config;
            if (dataCatalog == null ||
                !dataCatalog.TryGetCookingGameConfig("prep_zone_capacity", out config, out diagnostic) ||
                config == null || !int.TryParse(config.value, NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out capacity) || capacity <= 0)
            {
                diagnostic = "T8 prep_zone_capacity must be a positive integer.";
                return false;
            }
            diagnostic = null;
            return true;
        }
        private bool TryPrepareTrayAction(string itemId, out int capacity, out string diagnostic)
        {
            capacity = 0;
            if (CurrentPhase != ShortCyclePhase.Phase1 || Context == null ||
                inventoryManager == null || dataCatalog == null || fridgeBinder == null ||
                trayInteraction == null || Context.active_modal.HasValue || string.IsNullOrEmpty(itemId))
            {
                diagnostic = "Phase1 item action needs an item ID, inventory, catalog, fridge and tray bindings.";
                return false;
            }
            if (!TryGetPrepCapacity(out capacity, out diagnostic)) return false;
            return trayInteraction.CanPresent(capacity, out diagnostic);
        }

        private bool RefreshPhase1Presentation(int capacity, out string diagnostic)
        {
            if (!trayInteraction.Refresh(Context, inventoryManager, capacity))
            { diagnostic = trayInteraction.LastDiagnostic; return false; }
            if (!fridgeBinder.BindView(dataCatalog, inventoryManager, Context.current_filter_tag,
                fridgeBinder.IsTidied, Context, out diagnostic)) return false;
            Context.current_scroll_offset = fridgeBinder.CurrentTier;
            return true;
        }

        private System.Collections.Generic.List<PrepZoneItem> CopyPrep()
        {
            var copy = new System.Collections.Generic.List<PrepZoneItem>();
            foreach (var item in Context.prep_zone_contents) if (item != null) copy.Add(item.Clone());
            return copy;
        }

        private void RestoreReservations(System.Collections.Generic.List<PrepZoneItem> prep,
            System.Collections.Generic.IList<PlaceholderState> states, int capacity)
        {
            Context.prep_zone_contents.Clear();
            Context.prep_zone_contents.AddRange(prep);
            Context.placeholder_states.Clear();
            foreach (var state in states) Context.placeholder_states.Add(state);
            string ignored;
            RefreshPhase1Presentation(capacity, out ignored);
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
            var result = missing ?? stateMachine.TryAdvanceToPhase2();
            if (result.Succeeded && phase2HandoffPresenter != null) phase2HandoffPresenter.BeginAfterCommit();
            return result;
        }

        public ShortCyclePhase2TransitionAdapter Phase2Transition { get { return phase2HandoffPresenter == null ? null : phase2HandoffPresenter.TransitionAdapter; } }
        internal void RestorePhase1AfterHandoff() { if (stateMachine != null) stateMachine.TryRestorePhase1FromHandoff(); }

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

        private ShortCycleTransitionResult BindRecipeBook(string recipeId)
        {
            if (recipeBookBinder == null)
                return ShortCycleTransitionResult.Failure("RecipeBookBinder is not connected (D-T2 wiring required).");
            var views = GetRecipeViews(); string diagnostic;
            if (!recipeBookBinder.ValidateConfiguration(views.Count, out diagnostic))
                return ShortCycleTransitionResult.Failure(diagnostic);
            if (playerUnlockManager != null) playerUnlockManager.Configure(dataCatalog, playerProfile == null ? new string[0] : playerProfile.UnlockedRecipeIds);
            RecipeView selected = null;
            if (!string.IsNullOrEmpty(recipeId))
            {
                if (!IsRecipeUnlocked(recipeId)) return ShortCycleTransitionResult.Failure("Recipe is locked: " + recipeId);
                if (!dataCatalog.TryGetRecipeView(recipeId, out selected, out diagnostic) ||
                    !recipeBookBinder.CanBindBrowse(selected, out diagnostic))
                    return ShortCycleTransitionResult.Failure(diagnostic);
            }
            recipeBookBinder.ConfigureSelection(actionRouter);
            recipeBookBinder.BindCatalog(views);
            recipeBookBinder.BindBrowse(selected);
            RefreshFavoriteContent();
            return ShortCycleTransitionResult.Success();
        }

        private ShortCycleTransitionResult BindFridge()
        {
            string diagnostic = null;
            return fridgeBinder != null && fridgeBinder.BindView(dataCatalog, inventoryManager,
                Context == null ? null : Context.current_filter_tag, fridgeBinder.IsTidied, Context, out diagnostic)
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
            if (phase == ShortCyclePhase.None)
            {
                if (carrierWarning != null) carrierWarning.ResetSession();
                if (recipeBookBinder != null) recipeBookBinder.Clear();
                string ignored;
                if (clueBoardBinder != null) clueBoardBinder.Bind(dataCatalog, null, inventoryManager, out ignored);
            }
            var callback = StateChanged;
            if (callback != null) callback(phase, phase0View);
        }
    }
}
