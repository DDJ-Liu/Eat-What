using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EatWhat.Cooking.ShortCycle
{
    /// <summary>One explicit, synchronous P2 check; never starts the legacy 1-27 probe.</summary>
    public sealed class ShortCyclePhase2ProbeResult
    {
        public bool Attempted { get; private set; }
        public bool Succeeded { get; private set; }
        public bool Restored { get; private set; }
        public string Diagnostic { get; private set; }
        public IReadOnlyList<string> Checks { get; private set; }

        internal ShortCyclePhase2ProbeResult(bool attempted, bool succeeded, bool restored,
            string diagnostic, IList<string> checks)
        {
            Attempted = attempted; Succeeded = succeeded; Restored = restored;
            Diagnostic = diagnostic;
            Checks = new List<string>(checks ?? new List<string>()).AsReadOnly();
        }
    }

    /// <summary>
    /// Debug-only Play Mode probe. Keep this component under DebugAndReferences
    /// and disabled by default; production presentation has no embedded tests.
    /// </summary>
    [AddComponentMenu("Cooking/Short Cycle/Debug/Automated Probe")]
    [DisallowMultipleComponent]
    public sealed class ShortCycleAutomatedProbe : MonoBehaviour
    {
        [Header("Explicit debug wiring")]
        [SerializeField] private ShortCycleSessionManager coordinator = null;
        [SerializeField] private ShortCycleActionRouter actionRouter = null;
        [SerializeField] private ShortCyclePresentationDirector presentationDirector = null;
        [SerializeField] private ShortCycleCameraPanController cameraPanController = null;
        [SerializeField] private ShortCycleInputLockController inputLockController = null;
        [SerializeField] private ShortCycleInteractionLayerStack interactionLayerStack = null;
        [SerializeField] private MouseInteractionLayer clueBoardLayer = null;
        [SerializeField] private Camera controlledCamera = null;
        [SerializeField] private Transform phase0CameraMarker = null;
        [SerializeField] private Transform phase1CameraMarker = null;
        [SerializeField] private Transform clueBoardShell = null;
        [SerializeField] private ShortCycleClueBoardBinder clueBoardContentBinder = null;
        [SerializeField] private SlotBlock[] clueBoardProbeBlocks = new SlotBlock[0];
#pragma warning disable 0414 // Retained serialized legacy wiring; fixed-board verification no longer consumes anchors.
        [SerializeField] private Transform clueBoardPhase0Anchor = null;
        [SerializeField] private Transform clueBoardPhase1Anchor = null;
#pragma warning restore 0414
        [SerializeField] private float presentationDuration = 0.45f;
        [SerializeField] private bool runOnStart = false;
        [SerializeField] private string verificationRecipeId = "rcp_qingtang_noodle";

        public bool AutomatedProbeCompleted { get; private set; }
        public bool AutomatedProbeSucceeded { get; private set; }
        public string AutomatedProbeSummary { get; private set; }

        // Runtime evidence only; existing serialized anchor fields remain for scene compatibility.
        private readonly List<string> checkpoints = new List<string>();
        public IReadOnlyList<string> AutomatedProbeCheckpoints => checkpoints;
        private Vector3 fixedBoardPosition;
        private bool fixedBoardMoved;

        private void RecordCheckpoint(int number, string label, int previousFailures, List<string> failures)
        {
            var result = string.Format("{0:00}/13 {1} {2}", number,
                failures.Count == previousFailures ? "PASS" : "FAIL", label);
            checkpoints.Add(result);
            Debug.Log("CK01-C-PROBE CHECK " + result, this);
        }

        private void SampleFixedBoard()
        {
            if (clueBoardShell == null ||
                Vector3.Distance(clueBoardShell.position, fixedBoardPosition) >= 0.02f)
                fixedBoardMoved = true;
        }

        private void Reset()
        {
            enabled = false;
            runOnStart = false;
        }

        private void Start()
        {
            if (runOnStart)
            {
                RunProbe();
            }
        }

        public void RunProbe()
        {
            if (!enabled || AutomatedProbeCompleted)
            {
                return;
            }

            StartCoroutine(RunAutomatedProbe());
        }

        private IEnumerator RunAutomatedProbe()
        {
            var originalRunInBackground = Application.runInBackground;
            Application.runInBackground = true;
            var failures = new List<string>();
            checkpoints.Clear();
            fixedBoardMoved = false;
            fixedBoardPosition = clueBoardShell != null ? clueBoardShell.position : Vector3.zero;
            var previousFailures = 0;

            yield return null;
            yield return WaitForReady(ShortCyclePhase.Phase0, ShortCycleCameraSlot.P0Center,
                ShortCyclePhase0View.Cover, "initial Phase0", failures);
            if (presentationDirector == null) failures.Add("presentation director is not connected");
            if (!Near(controlledCamera, phase0CameraMarker))
                failures.Add("camera did not start at the P0-Center marker");
            if (clueBoardShell == null) failures.Add("fixed clue-board shell is not connected");
            SampleFixedBoard();
            RecordCheckpoint(1, "initial Phase0 wiring/camera/input", previousFailures, failures);
            previousFailures = failures.Count;

            var catalog = IsReady(ShortCyclePhase.Phase0, ShortCycleCameraSlot.P0Center, ShortCyclePhase0View.Cover)
                ? Dispatch(ShortCycleActionNames.OpenCover)
                : ShortCycleActionResult.Failure("initial Phase0 readiness prerequisite failed");
            if (catalog.Succeeded)
                yield return WaitForReady(ShortCyclePhase.Phase0, ShortCycleCameraSlot.P0Center,
                    ShortCyclePhase0View.RecipeCatalog, "cover -> catalog", failures);
            var catalogReady = catalog.Succeeded &&
                IsReady(ShortCyclePhase.Phase0, ShortCycleCameraSlot.P0Center, ShortCyclePhase0View.RecipeCatalog) &&
                !coordinator.Context.HasSelectedRecipe;
            if (!catalogReady) failures.Add("cover -> catalog state transition failed: " + catalog.Error);
            RecordCheckpoint(2, "cover -> catalog", previousFailures, failures);
            previousFailures = failures.Count;

            var selection = catalogReady ? Dispatch(new ShortCycleActionRequest
            {
                ActionName = ShortCycleActionNames.SelectRecipe,
                recipe_id = verificationRecipeId
            }) : ShortCycleActionResult.Failure("catalog readiness prerequisite failed");
            if (selection.Succeeded)
                yield return WaitForReady(ShortCyclePhase.Phase0, ShortCycleCameraSlot.P0Left,
                    ShortCyclePhase0View.RecipeBrowse, "select -> browse", failures);
            var browseReady = selection.Succeeded &&
                IsReady(ShortCyclePhase.Phase0, ShortCycleCameraSlot.P0Left, ShortCyclePhase0View.RecipeBrowse) &&
                coordinator.Context.HasSelectedRecipe;
            if (!browseReady) failures.Add("catalog -> select -> browse sequence failed: " + selection.Error);
            RecordCheckpoint(3, "select -> browse/camera settled", previousFailures, failures);
            previousFailures = failures.Count;

            var openBoard = browseReady ? Dispatch(ShortCycleActionNames.OpenClueBoard)
                : ShortCycleActionResult.Failure("browse readiness prerequisite failed");
            if (openBoard.Succeeded)
                yield return WaitForReady(ShortCyclePhase.Phase0, ShortCycleCameraSlot.P0Left,
                    ShortCyclePhase0View.ClueBoardShell, "open clue-board", failures);
            var boardOpened = openBoard.Succeeded &&
                IsReady(ShortCyclePhase.Phase0, ShortCycleCameraSlot.P0Left, ShortCyclePhase0View.ClueBoardShell) &&
                HasPhase0BaseLayer();
            if (!boardOpened) failures.Add("clue-board view/base layer open failed: " + openBoard.Error);
            SampleFixedBoard();
            RecordCheckpoint(4, "clue-board open/base layer retained", previousFailures, failures);
            previousFailures = failures.Count;

            var closeBoard = boardOpened ? Dispatch(ShortCycleActionNames.CloseClueBoard)
                : ShortCycleActionResult.Failure("clue-board open prerequisite failed");
            if (closeBoard.Succeeded)
                yield return WaitForReady(ShortCyclePhase.Phase0, ShortCycleCameraSlot.P0Left,
                    ShortCyclePhase0View.RecipeBrowse, "close clue-board", failures);
            var browseRestored = closeBoard.Succeeded &&
                IsReady(ShortCyclePhase.Phase0, ShortCycleCameraSlot.P0Left, ShortCyclePhase0View.RecipeBrowse) &&
                HasPhase0BaseLayer();
            if (!browseRestored) failures.Add("clue-board close/base layer retention failed: " + closeBoard.Error);
            SampleFixedBoard();
            RecordCheckpoint(5, "clue-board close/base layer retained", previousFailures, failures);
            previousFailures = failures.Count;

            if (browseRestored)
            {
                coordinator.Context.current_scroll_offset = 7.25f;
                coordinator.Context.current_filter_tag = "tag_ingredient_vegetable";
                coordinator.Context.prep_zone_contents.Add(new PrepZoneItem
                {
                    item_id = "ing_tomato",
                    quantity = 1,
                    drag_order = 0,
                    state = "probe"
                });
            }

            var start = browseRestored ? Dispatch(ShortCycleActionNames.StartCooking)
                : ShortCycleActionResult.Failure("restored browse prerequisite failed");
            var lockedDuringStartPan = inputLockController != null && inputLockController.Gate.IsLocked;
            // Intentionally no wait here: this action must be rejected specifically by the input lock.
            var blockedReturn = start.Succeeded ? Dispatch(ShortCycleActionNames.Return)
                : ShortCycleActionResult.Failure("start prerequisite failed");
            if (!start.Succeeded) failures.Add("start-cooking named action failed: " + start.Error);
            if (!lockedDuringStartPan) failures.Add("input gate was not locked during Phase0->Phase1 pan");
            if (blockedReturn.Succeeded || blockedReturn.ErrorCode != ShortCycleActionErrorCode.Locked)
                failures.Add("immediate return was not rejected with ErrorCode.Locked: " + blockedReturn.Error);
            RecordCheckpoint(6, "start/pan lock/Locked return rejection", previousFailures, failures);
            previousFailures = failures.Count;

            if (start.Succeeded)
                yield return WaitForReady(ShortCyclePhase.Phase1, ShortCycleCameraSlot.P1,
                    null, "enter Phase1", failures);
            var reachedPhase1 = start.Succeeded && IsReady(ShortCyclePhase.Phase1, ShortCycleCameraSlot.P1) &&
                Near(controlledCamera, phase1CameraMarker);
            if (!reachedPhase1) failures.Add("Phase1/context/camera/unlock arrival prerequisite failed");
            RecordCheckpoint(7, "Phase1/camera/unlock", previousFailures, failures);
            previousFailures = failures.Count;

            SampleFixedBoard();
            if (!reachedPhase1) failures.Add("fixed-board Phase1 check not covered: Phase1 was not reached");
            if (fixedBoardMoved) failures.Add("fixed clue-board moved before/during Phase1 pan");
            RecordCheckpoint(8, "fixed board through verified Phase1", previousFailures, failures);
            previousFailures = failures.Count;

            var back = reachedPhase1 ? Dispatch(ShortCycleActionNames.Return)
                : ShortCycleActionResult.Failure("Phase1 arrival prerequisite failed");
            var lockedDuringReturnPan = inputLockController != null && inputLockController.Gate.IsLocked;
            if (!back.Succeeded) failures.Add("return named action failed: " + back.Error);
            if (!lockedDuringReturnPan) failures.Add("input gate was not locked during Phase1->Phase0 pan");
            RecordCheckpoint(9, "return/pan lock", previousFailures, failures);
            previousFailures = failures.Count;

            if (back.Succeeded)
                yield return WaitForReady(ShortCyclePhase.Phase0, ShortCycleCameraSlot.P0Left,
                    ShortCyclePhase0View.RecipeBrowse, "return to Phase0", failures);
            var returnedFromPhase1 = reachedPhase1 && back.Succeeded &&
                IsReady(ShortCyclePhase.Phase0, ShortCycleCameraSlot.P0Left, ShortCyclePhase0View.RecipeBrowse);
            if (!returnedFromPhase1) failures.Add("verified Phase1 -> Phase0 return prerequisite failed");
            RecordCheckpoint(10, "verified return/Phase0/camera/unlock", previousFailures, failures);
            previousFailures = failures.Count;

            SampleFixedBoard();
            if (!returnedFromPhase1) failures.Add("fixed-board return check not covered: verified round trip failed");
            if (fixedBoardMoved) failures.Add("fixed clue-board moved during return");
            RecordCheckpoint(11, "fixed board through verified return", previousFailures, failures);
            previousFailures = failures.Count;

            if (!browseRestored || !returnedFromPhase1)
                failures.Add("Phase1 state retention not covered: setup/verified round trip failed");
            else if (Math.Abs(coordinator.Context.current_scroll_offset - 7.25f) > 0.001f ||
                coordinator.Context.current_filter_tag != "tag_ingredient_vegetable" ||
                coordinator.Context.prep_zone_contents.Count != 1)
                failures.Add("Phase1 temporary state was not retained on return");
            RecordCheckpoint(12, "verified Phase1 state retained", previousFailures, failures);
            previousFailures = failures.Count;

            var browseEscape = returnedFromPhase1 ? Dispatch(ShortCycleActionNames.Escape)
                : ShortCycleActionResult.Failure("verified return prerequisite failed");
            if (browseEscape.Succeeded)
                yield return WaitForReady(ShortCyclePhase.Phase0, ShortCycleCameraSlot.P0Center,
                    ShortCyclePhase0View.RecipeCatalog, "ESC browse -> catalog", failures);
            var catalogEscape = browseEscape.Succeeded &&
                IsReady(ShortCyclePhase.Phase0, ShortCycleCameraSlot.P0Center, ShortCyclePhase0View.RecipeCatalog)
                ? Dispatch(ShortCycleActionNames.Escape)
                : ShortCycleActionResult.Failure("ESC catalog readiness prerequisite failed");
            if (catalogEscape.Succeeded)
                yield return WaitForReady(ShortCyclePhase.Phase0, ShortCycleCameraSlot.P0Center,
                    ShortCyclePhase0View.Cover, "ESC catalog -> cover", failures);
            var coverEscape = catalogEscape.Succeeded &&
                IsReady(ShortCyclePhase.Phase0, ShortCycleCameraSlot.P0Center, ShortCyclePhase0View.Cover)
                ? Dispatch(ShortCycleActionNames.Escape)
                : ShortCycleActionResult.Failure("ESC cover readiness prerequisite failed");
            if (coverEscape.Succeeded)
                yield return WaitForReady(ShortCyclePhase.None, null, null, "ESC teardown", failures);
            if (!browseEscape.Succeeded || !catalogEscape.Succeeded || !coverEscape.Succeeded)
                failures.Add("layered ESC sequence failed: " + browseEscape.Error + "; " +
                    catalogEscape.Error + "; " + coverEscape.Error);
            if (!IsReady(ShortCyclePhase.None, null) || !string.IsNullOrEmpty(coordinator.Context.current_recipe_id))
                failures.Add("ESC did not settle/clear ShortCycleContext");
            RecordCheckpoint(13, "settled layered ESC/context clear", previousFailures, failures);

            yield return RunRecipeBookChecks(failures);

            yield return RunPhase2HandoffChecks(failures);

            RunTrayReservationChecks(failures);
            RunClueBindingChecks(failures);

            AutomatedProbeCompleted = true;
            AutomatedProbeSucceeded = failures.Count == 0;
            AutomatedProbeSummary = failures.Count == 0
                ? string.Join("; ", checkpoints.ToArray())
                : string.Join("; ", failures.ToArray());
            Application.runInBackground = originalRunInBackground;
            if (AutomatedProbeSucceeded)
                Debug.Log("CK01-C-PROBE PASS " + AutomatedProbeSummary, this);
            else
                Debug.LogWarning("CK01-C-PROBE NEEDS_REVIEW " + AutomatedProbeSummary, this);
        }

        // Shared by the original14..18 sequence and its bounded offline regression.
        private IEnumerator RunRecipeBookChecks(List<string> failures)
        {
            var previousFailures = failures.Count;
            var book = coordinator.RecipeBook;
            var beginBook = coordinator.BeginSession(null, ShortCycleTriggerContext.FreeTry);
            if (beginBook.Succeeded) yield return WaitForReady(ShortCyclePhase.Phase0, ShortCycleCameraSlot.P0Center,
                ShortCyclePhase0View.Cover, "D-T1a restart", failures);
            var views = beginBook.Succeeded ? coordinator.GetRecipeViews() : null;
            if (book == null || views == null || book.CatalogRecipeCount != views.Count ||
                book.VisibleCatalogCardCount != System.Math.Min(8, views.Count))
                failures.Add("D-T1a catalog count does not match T1 records");
            RecordCheckpoint(14, "catalog count equals T1 records", previousFailures, failures);
            previousFailures = failures.Count;
            var titlesMatch = true; var selectedAny = false;
            if (book == null || !beginBook.Succeeded) failures.Add("D-T1a catalog click coverage unavailable");
            else foreach (var card in book.CatalogCards)
            {
                var show = coordinator.CurrentPhase0View == ShortCyclePhase0View.RecipeCatalog
                    ? ShortCycleActionResult.Success()
                    : coordinator.CurrentPhase0View == ShortCyclePhase0View.Cover
                        ? Dispatch(ShortCycleActionNames.OpenCover)
                        : coordinator.CurrentPhase0View == ShortCyclePhase0View.RecipeBrowse
                            ? Dispatch(ShortCycleActionNames.OpenCatalog)
                            : ShortCycleActionResult.Failure("D-T1a catalog entry has an unsupported source view");
                if (show.Succeeded) yield return WaitForReady(ShortCyclePhase.Phase0, ShortCycleCameraSlot.P0Center,
                    ShortCyclePhase0View.RecipeCatalog, "D-T1a catalog click", failures);
                if (card == null || !show.Succeeded || !HasPhase0BaseLayer() || !IsReady(ShortCyclePhase.Phase0, ShortCycleCameraSlot.P0Center, ShortCyclePhase0View.RecipeCatalog))
                { failures.Add("D-T1a card/camera/layer prerequisite failed"); continue; }
                var beforeId = coordinator.Context.current_recipe_id;
                var selected = card.Select();
                if (card.View == null || !card.View.is_unlocked)
                {
                    if (selected.Succeeded || coordinator.Context.current_recipe_id != beforeId)
                        failures.Add("D-T1a locked/unused card selected a recipe");
                    continue;
                }
                if (selected.Succeeded) yield return WaitForReady(ShortCyclePhase.Phase0, ShortCycleCameraSlot.P0Left,
                    ShortCyclePhase0View.RecipeBrowse, "D-T1a browse selection", failures);
                if (!selected.Succeeded || coordinator.Context.current_recipe_id != card.View.recipe_id ||
                    !IsReady(ShortCyclePhase.Phase0, ShortCycleCameraSlot.P0Left, ShortCyclePhase0View.RecipeBrowse))
                    failures.Add("D-T1a catalog selection did not bind the clicked recipe");
                else selectedAny = true;
                if (book.DisplayedTitle != LocService.Get(card.View.name_key)) titlesMatch = false;
            }
            if (!selectedAny) failures.Add("D-T1a no selectable bound recipe was exercised");
            RecordCheckpoint(15, "all bound cards select correct id; locked/unused reject", previousFailures, failures);
            previousFailures = failures.Count;
            if (!titlesMatch || book == null || book.CurrentRecipe == null) failures.Add("D-T1a browse title differs from localized name_key");
            RecordCheckpoint(16, "browse title equals localized recipe name_key", previousFailures, failures);

            previousFailures = failures.Count;
            var detailBrowse = IsReady(ShortCyclePhase.Phase0, ShortCycleCameraSlot.P0Left, ShortCyclePhase0View.RecipeBrowse) && HasPhase0BaseLayer()
                ? ShortCycleActionResult.Success()
                : IsReady(ShortCyclePhase.Phase0, ShortCycleCameraSlot.P0Center, ShortCyclePhase0View.RecipeCatalog) && HasPhase0BaseLayer()
                ? Dispatch(new ShortCycleActionRequest { ActionName = ShortCycleActionNames.SelectRecipe, recipe_id = coordinator.Context.current_recipe_id })
                : ShortCycleActionResult.Failure("D-T1b catalog/base layer prerequisite failed");
            if (detailBrowse.Succeeded) yield return WaitForReady(ShortCyclePhase.Phase0, ShortCycleCameraSlot.P0Left,
                ShortCyclePhase0View.RecipeBrowse, "D-T1b browse prerequisite", failures);
            var openDetail = detailBrowse.Succeeded && HasPhase0BaseLayer() ? Dispatch(ShortCycleActionNames.OpenStepDetail)
                : ShortCycleActionResult.Failure("D-T1b browse/base layer prerequisite failed");
            if (openDetail.Succeeded) yield return WaitForReady(ShortCyclePhase.Phase0, ShortCycleCameraSlot.P0Left,
                ShortCyclePhase0View.RecipeBrowse, "D-T1b modal ready", failures);
            if (!openDetail.Succeeded || coordinator.ActiveModal != ShortCycleModalId.StepDetail || book == null ||
                book.StepDetails == null || book.CurrentRecipe == null ||
                book.StepDetails.BoundRowCount != book.CurrentRecipe.steps.Count)
                failures.Add("D-T1b step-detail row count differs from the current chain");
            RecordCheckpoint(17, "step-detail rows equal ordered chain length", previousFailures, failures);
            previousFailures = failures.Count;
            if (coordinator.ActiveModal == ShortCycleModalId.StepDetail)
            {
                Dispatch(ShortCycleActionNames.CloseStepDetail);
                yield return WaitForReady(ShortCyclePhase.Phase0, ShortCycleCameraSlot.P0Left,
                    ShortCyclePhase0View.RecipeBrowse, "D-T1b modal close/base layer", failures);
            }
            if (!IsReady(ShortCyclePhase.Phase0, ShortCycleCameraSlot.P0Left, ShortCyclePhase0View.RecipeBrowse) ||
                !HasPhase0BaseLayer() || coordinator.PlayerProfile == null || book == null || book.CurrentRecipe == null)
                failures.Add("D-T1b favorite prerequisite is unavailable");
            else
            {
                var id = coordinator.Context.current_recipe_id;
                var initialFavorite = coordinator.PlayerProfile.IsFavorite(id);
                for (var index = 0; index < 3; index++)
                {
                    var toggled = Dispatch(ShortCycleActionNames.ToggleFavorite);
                    var favorite = coordinator.PlayerProfile.IsFavorite(id);
                    var found = false;
                    foreach (var card in book.CatalogCards)
                        if (card != null && card.View != null && card.View.recipe_id == id)
                        { found = true; if (card.FavoriteVisible != favorite) failures.Add("D-T1b catalog favorite star differs from profile"); }
                    if (!toggled.Succeeded || !found || book.CurrentFavoriteVisible != favorite)
                        failures.Add("D-T1b current favorite star differs from profile");
                }
                if (coordinator.PlayerProfile.IsFavorite(id) != initialFavorite) Dispatch(ShortCycleActionNames.ToggleFavorite);
            }
            RecordCheckpoint(18, "favorite toggle/profile/catalog and current stars agree", previousFailures, failures);

            if (coordinator.CurrentPhase == ShortCyclePhase.Phase0 && coordinator.CurrentPhase0View == ShortCyclePhase0View.RecipeBrowse)
            {
                Dispatch(ShortCycleActionNames.Escape);
                yield return WaitForReady(ShortCyclePhase.Phase0, ShortCycleCameraSlot.P0Center,
                    ShortCyclePhase0View.RecipeCatalog, "D-T1a cleanup browse", failures);
            }
            if (coordinator.CurrentPhase == ShortCyclePhase.Phase0 && coordinator.CurrentPhase0View == ShortCyclePhase0View.RecipeCatalog)
            {
                Dispatch(ShortCycleActionNames.Escape);
                yield return WaitForReady(ShortCyclePhase.Phase0, ShortCycleCameraSlot.P0Center,
                    ShortCyclePhase0View.Cover, "D-T1a cleanup catalog", failures);
            }
            if (coordinator.CurrentPhase == ShortCyclePhase.Phase0)
            {
                Dispatch(ShortCycleActionNames.Escape);
                yield return WaitForReady(ShortCyclePhase.None, null, null, "D-T1a cleanup teardown", failures);
            }
        }

        // P2S-01: append-only 19/20. Requires the ENGINE harness's pre-Awake save isolation.
        private IEnumerator RunPhase2HandoffChecks(List<string> failures)
        {
            var segmentStart = failures.Count;
            var prior = failures.Count;
            var transition = coordinator == null ? null : coordinator.Phase2Transition;
            var ownsAttempt = false;
            var committed = false;
            var valid = coordinator != null && coordinator.PersistenceIsolationRequired && transition != null &&
                coordinator.Inventory != null && coordinator.RecipeBook != null && actionRouter != null &&
                presentationDirector != null && cameraPanController != null && controlledCamera != null &&
                inputLockController != null && interactionLayerStack != null && clueBoardLayer != null;
            if (!valid) failures.Add("P2S requires pre-Awake isolated Session and complete adapter/inventory/router/presentation wiring");
            if (valid && coordinator.CurrentPhase != ShortCyclePhase.None)
            { failures.Add("P2S appended segment requires the original probe's completed None state"); valid = false; }
            if (valid)
            {
                // Original18 owns its teardown. This segment owns only its fresh attempt from None.
                ownsAttempt = true;
                var begin = coordinator.BeginSession(null, ShortCycleTriggerContext.FreeTry);
                if (!begin.Succeeded) { failures.Add("P2S BeginSession failed: " + begin.Error); valid = false; }
                else
                {
                    yield return WaitForReady(ShortCyclePhase.Phase0, ShortCycleCameraSlot.P0Center, ShortCyclePhase0View.Cover, "P2S fresh cover", failures);
                    valid = IsReady(ShortCyclePhase.Phase0, ShortCycleCameraSlot.P0Center, ShortCyclePhase0View.Cover) && HasPhase0BaseLayer();
                    if (!valid) failures.Add("P2S fresh cover/base layer is unavailable");
                }
            }
            if (valid)
            {
                var catalog = Dispatch(ShortCycleActionNames.OpenCover);
                if (!catalog.Succeeded) { failures.Add("P2S catalog entry failed: " + catalog.Error); valid = false; }
                else
                {
                    yield return WaitForReady(ShortCyclePhase.Phase0, ShortCycleCameraSlot.P0Center, ShortCyclePhase0View.RecipeCatalog, "P2S catalog prerequisite", failures);
                    valid = IsReady(ShortCyclePhase.Phase0, ShortCycleCameraSlot.P0Center, ShortCyclePhase0View.RecipeCatalog) && HasPhase0BaseLayer();
                    if (!valid) failures.Add("P2S catalog/base layer is unavailable");
                }
            }
            string selectedId = null;
            if (valid)
            {
                foreach (var view in coordinator.GetRecipeViews())
                    if (view.is_unlocked && (string.IsNullOrEmpty(verificationRecipeId) || view.recipe_id == verificationRecipeId))
                    { selectedId = view.recipe_id; break; }
                if (selectedId == null) { failures.Add("P2S verification recipe is missing or locked; no unlock is changed"); valid = false; }
                else
                {
                    var select = Dispatch(new ShortCycleActionRequest { ActionName = ShortCycleActionNames.SelectRecipe, recipe_id = selectedId });
                    if (!select.Succeeded) { failures.Add("P2S recipe selection failed: " + select.Error); valid = false; }
                    else
                    {
                        yield return WaitForReady(ShortCyclePhase.Phase0, ShortCycleCameraSlot.P0Left, ShortCyclePhase0View.RecipeBrowse, "P2S selected browse", failures);
                        valid = IsReady(ShortCyclePhase.Phase0, ShortCycleCameraSlot.P0Left, ShortCyclePhase0View.RecipeBrowse) && HasPhase0BaseLayer() &&
                            coordinator.Context.HasSelectedRecipe && coordinator.RecipeBook.CurrentRecipe != null && coordinator.RecipeBook.CurrentRecipe.recipe_id == selectedId;
                        if (!valid) failures.Add("P2S selected recipe/browse/base layer is unavailable");
                    }
                }
            }
            if (valid)
            {
                var start = Dispatch(ShortCycleActionNames.StartCooking);
                if (!start.Succeeded) { failures.Add("P2S Phase1 prerequisite failed: " + start.Error); valid = false; }
                else
                {
                    yield return WaitForReady(ShortCyclePhase.Phase1, ShortCycleCameraSlot.P1, null, "P2S Phase1 prerequisite", failures);
                    valid = IsReady(ShortCyclePhase.Phase1, ShortCycleCameraSlot.P1) && interactionLayerStack.Top != null && !interactionLayerStack.IsTop(clueBoardLayer);
                    if (!valid) failures.Add("P2S Phase1 camera/input/layer prerequisite is unavailable");
                }
            }
            if (valid && coordinator.Context.prep_zone_contents.Count == 0)
            {
                string itemId = null;
                foreach (var slot in coordinator.RecipeBook.CurrentRecipe.slots)
                    if (slot.standard_ingredient != null && coordinator.Inventory.GetQuantity(slot.standard_ingredient.item_id) > 0)
                    { itemId = slot.standard_ingredient.item_id; break; }
                if (itemId == null) { failures.Add("P2S has no available configured ingredient for a nonempty handoff"); valid = false; }
                else
                {
                    coordinator.Context.prep_zone_contents.Add(new PrepZoneItem { item_id = itemId, quantity = 1, drag_order = 0, state = "prepared" });
                    coordinator.Context.placeholder_states.Add(new PlaceholderState { instance_id = "probe-p2s-" + itemId, item_id = itemId, taken_count = 1 });
                }
            }
            var expected = valid ? coordinator.Context.CreatePhase2Handoff() : null;
            var quantities = new Dictionary<string, int>();
            if (expected != null) foreach (var item in expected.PrepZoneContents) quantities[item.item_id] = coordinator.Inventory.GetQuantity(item.item_id);
            if (valid)
            {
                var go = Dispatch(ShortCycleActionNames.AdvanceToPhase2);
                committed = go.Succeeded;
                foreach (var pair in quantities) if (coordinator.Inventory.GetQuantity(pair.Key) != pair.Value) committed = true;
                if (!go.Succeeded) { failures.Add("P2S GO handoff failed: " + go.Error); valid = false; }
                var deadline = Time.unscaledTime + 15f;
                while (valid && transition.Status == ShortCyclePhase2TransitionStatus.Loading && Time.unscaledTime < deadline) yield return null;
                if (valid && (!transition.IsReady || !presentationDirector.Phase2TransitionActive || !SamePrep(expected, transition.SceneEntry)))
                    failures.Add("P2S did not consume the exact recipe/prep snapshot in the new scene: " + transition.LastError);
            }
            if (expected != null) foreach (var item in expected.PrepZoneContents) quantities[item.item_id] = coordinator.Inventory.GetQuantity(item.item_id);
            RecordPhase2Checkpoint(19, "additive P2 entry/exact handoff/old-space cleanup", prior, failures);
            prior = failures.Count;
            if (!valid || transition == null || !transition.ReturnToPhase1()) failures.Add("P2S return requires an active owned scene");
            else
            {
                var deadline = Time.unscaledTime + 15f;
                while (transition.Status == ShortCyclePhase2TransitionStatus.Unloading && Time.unscaledTime < deadline) yield return null;
                yield return WaitForReady(ShortCyclePhase.Phase1, ShortCycleCameraSlot.P1, null, "P2S returned Phase1", failures);
                if (!IsReady(ShortCyclePhase.Phase1, ShortCycleCameraSlot.P1) || interactionLayerStack.Top == null || interactionLayerStack.IsTop(clueBoardLayer) ||
                    coordinator.Inventory.Phase2Owner != null || transition.SceneEntry != null || presentationDirector.Phase2TransitionActive || coordinator.Context.placeholder_states.Count != 0)
                    failures.Add("P2S return did not unload/release/restore committed Phase1 state");
            }
            if (coordinator != null && coordinator.Inventory != null) foreach (var pair in quantities)
                if (coordinator.Inventory.GetQuantity(pair.Key) != pair.Value) failures.Add("P2S return changed committed inventory: " + pair.Key);
            RecordPhase2Checkpoint(20, "owned-scene teardown/inventory release/Phase1 restore", prior, failures);
            if (ownsAttempt && failures.Count != segmentStart) yield return CleanupPhase2ProbeAttempt(committed, failures);
        }
        private IEnumerator CleanupPhase2ProbeAttempt(bool committed, List<string> failures)
        {
            var transition = coordinator.Phase2Transition;
            if (transition != null && (transition.Status == ShortCyclePhase2TransitionStatus.Active || transition.Status == ShortCyclePhase2TransitionStatus.Loading)) transition.Teardown();
            var deadline = Time.unscaledTime + 15f;
            while (transition != null && transition.Status == ShortCyclePhase2TransitionStatus.Unloading && Time.unscaledTime < deadline) yield return null;
            if (coordinator.CurrentPhase == ShortCyclePhase.Phase2 || transition != null && transition.SceneEntry != null)
            { failures.Add("P2S cleanup still owns a pending scene; retain failure for explicit recovery"); yield break; }
            // A committed unit remains in prep. Clearing that session would silently lose food.
            if (committed)
            {
                if (coordinator.CurrentPhase != ShortCyclePhase.Phase1 || coordinator.Inventory.Phase2Owner != null)
                    failures.Add("P2S cleanup could not retain committed prep in a recovered Phase1");
                yield break;
            }
            if (coordinator.CurrentPhase == ShortCyclePhase.Phase1)
            {
                yield return WaitForReady(ShortCyclePhase.Phase1, null, null, "P2S cleanup input", failures);
                if (!IsReady(ShortCyclePhase.Phase1, null)) yield break;
                var result = Dispatch(ShortCycleActionNames.Return);
                if (!result.Succeeded) { failures.Add("P2S cleanup return failed: " + result.Error); yield break; }
            }
            for (var i = 0; i < 3 && coordinator.CurrentPhase == ShortCyclePhase.Phase0; i++)
            {
                yield return WaitForReady(ShortCyclePhase.Phase0, null, null, "P2S cleanup navigation", failures);
                if (!IsReady(ShortCyclePhase.Phase0, null)) yield break;
                var result = Dispatch(ShortCycleActionNames.Escape);
                if (!result.Succeeded) { failures.Add("P2S cleanup ESC failed: " + result.Error); yield break; }
            }
            if (coordinator.CurrentPhase == ShortCyclePhase.None) coordinator.RecipeBook.Clear();
            else failures.Add("P2S cleanup did not finish its uncommitted session");
        }
        private static bool SamePrep(ShortCycleDataHandoff expected, Phase2SceneEntry actual)
        {
            if (expected == null || actual == null || actual.RecipeId != expected.CurrentRecipeId) return false;
            var items = actual.PrepZoneContents; if (items.Count != expected.PrepZoneContents.Count) return false;
            for (var i = 0; i < items.Count; i++)
            { var a = items[i]; var b = expected.PrepZoneContents[i]; if (a.item_id != b.item_id || a.quantity != b.quantity || a.drag_order != b.drag_order || a.state != b.state) return false; }
            return true;
        }
        private void RecordPhase2Checkpoint(int number, string label, int previousFailures, List<string> failures)
        {
            var result = string.Format("{0:00}/20 {1} {2}", number, failures.Count == previousFailures ? "PASS" : "FAIL", label);
            checkpoints.Add(result); Debug.Log("CK01-C-PROBE CHECK " + result, this);
        }

        // Append-only 21..23; pure P1 reservation checks leave the live P2 probe session intact.
        private void RunTrayReservationChecks(List<string> failures)
        {
            var context = new ShortCycleContext { current_phase = ShortCyclePhase.Phase1 };
            var first = ShortCycleTrayIdentity.ForUnit("probe-tray", 0);
            var second = ShortCycleTrayIdentity.ForUnit("probe-tray", 1);
            var prior = failures.Count;
            var take = ShortCycleTrayReservation.TryTake(context, "probe-tray", first, "single", 2, 1);
            if (!take.Succeeded || context.prep_zone_contents.Count != 1 ||
                context.placeholder_states.Count != 1 || context.prep_zone_contents[0].instance_id != first)
                failures.Add("TRAY reserved take did not occupy one identified slot");
            RecordTrayCheckpoint(21, "identified take reserves one tray slot", prior, failures);
            prior = failures.Count;
            var rejected = ShortCycleTrayReservation.TryTake(context, "probe-tray", second, "single", 2, 1);
            if (rejected.Succeeded || rejected.Error != ShortCycleTrayReservation.CapacityFullKey ||
                context.prep_zone_contents.Count != 1 || context.placeholder_states.Count != 1 ||
                context.prep_zone_contents[0].instance_id != first)
                failures.Add("TRAY capacity rejection changed a reservation");
            RecordTrayCheckpoint(22, "full tray rejects without reservation mutation", prior, failures);
            prior = failures.Count;
            var returned = ShortCycleTrayReservation.TryPutBack(context, "probe-tray", first);
            if (!returned.Succeeded || context.prep_zone_contents.Count != 0 ||
                context.placeholder_states.Count != 0)
                failures.Add("TRAY return did not release the identified minimum unit");
            RecordTrayCheckpoint(23, "identified return frees tray slot", prior, failures);
        }

        private void RecordTrayCheckpoint(int number, string label, int previousFailures, List<string> failures)
        {
            var result = string.Format("{0:00}/23 {1} {2}", number,
                failures.Count == previousFailures ? "PASS" : "FAIL", label);
            checkpoints.Add(result);
            Debug.Log("CK01-C-PROBE CHECK " + result, this);
        }
        // Append-only 24..27; content reads and binding do not consume inventory or alter the P2 probe.
        private void RunClueBindingChecks(List<string> failures)
        {
            var previous = failures.Count;
            if (coordinator == null || coordinator.ValidationCatalog == null || coordinator.Inventory == null ||
                clueBoardContentBinder == null || clueBoardProbeBlocks == null || clueBoardProbeBlocks.Length < 3 ||
                clueBoardProbeBlocks[0] == null || clueBoardProbeBlocks[1] == null || clueBoardProbeBlocks[2] == null)
            {
                failures.Add("CLUE probe requires explicit Binder and three SlotBlock references");
                RecordClueCheckpoint(24, "clue content wiring", previous, failures);
                return;
            }
            string diagnostic;
            var catalog = coordinator.ValidationCatalog;
            var inventory = coordinator.Inventory;
            var bound = clueBoardContentBinder.Bind(catalog, "rcp_qingtang_noodle", inventory, out diagnostic);
            if (!bound || !HasAlternatives(0, 2) || !HasAlternatives(1, 3) || !HasAlternatives(2, 3) ||
                clueBoardProbeBlocks[0].Binding.StandardHeldCount !=
                    inventory.GetQuantity(clueBoardProbeBlocks[0].Binding.StandardItemId))
                failures.Add("CLUE noodle 2/3/3 slots or actual held count differ: " + diagnostic);
            RecordClueCheckpoint(24, "noodle slots and live count", previous, failures);
            previous = failures.Count;
            bound = clueBoardContentBinder.Bind(catalog, "rcp_tomato_egg", inventory, out diagnostic);
            if (!bound || !HasAlternatives(0, 1) || !HasAlternatives(1, 2) ||
                clueBoardProbeBlocks[2].Binding != null)
                failures.Add("CLUE tomato/egg 1/2 alternatives or excess block clear differ: " + diagnostic);
            RecordClueCheckpoint(25, "tomato/egg and excess clear", previous, failures);
            previous = failures.Count;
            bound = clueBoardContentBinder.Bind(catalog, "rcp_kaishui_cabbage", inventory, out diagnostic);
            if (!bound || !HasAlternatives(0, 1) || clueBoardProbeBlocks[1].Binding != null)
                failures.Add("CLUE locked cabbage recipe read or single alternative differs: " + diagnostic);
            RecordClueCheckpoint(26, "locked recipe content read", previous, failures);
            previous = failures.Count;
            bound = clueBoardContentBinder.Bind(catalog, null, inventory, out diagnostic);
            if (!bound || clueBoardContentBinder.BoundRecipeId != null ||
                clueBoardProbeBlocks[0].Binding != null || clueBoardProbeBlocks[1].Binding != null ||
                clueBoardProbeBlocks[2].Binding != null)
                failures.Add("CLUE clear retained recipe or slot content: " + diagnostic);
            RecordClueCheckpoint(27, "clue clear and unsubscribe", previous, failures);
        }

        private bool HasAlternatives(int blockIndex, int expected)
        {
            var block = clueBoardProbeBlocks[blockIndex];
            if (block == null || block.Binding == null || block.Binding.Substitutes.Count != expected) return false;
            foreach (var substitute in block.Binding.Substitutes)
                if (substitute == null || block.GetSubstituteBinding(substitute.AnchorIndex) == null) return false;
            return true;
        }

        private void RecordClueCheckpoint(int number, string label, int previousFailures, List<string> failures)
        {
            var result = string.Format("{0:00}/27 {1} {2}", number,
                failures.Count == previousFailures ? "PASS" : "FAIL", label);
            checkpoints.Add(result);
            Debug.Log("CK01-C-PROBE CHECK " + result, this);
        }
        private bool HasPhase0BaseLayer()
        {
            return clueBoardLayer != null && interactionLayerStack != null &&
                interactionLayerStack.Contains(clueBoardLayer) && interactionLayerStack.IsTop(clueBoardLayer);
        }

        private bool IsReady(ShortCyclePhase phase, ShortCycleCameraSlot? slot, ShortCyclePhase0View? view = null)
        {
            if (coordinator == null || coordinator.Context == null || coordinator.Context.current_phase != phase ||
                cameraPanController == null || controlledCamera == null || inputLockController == null ||
                cameraPanController.IsPanning || inputLockController.Gate.IsLocked)
                return false;
            if (view.HasValue && coordinator.Context.phase0_view != view.Value) return false;
            if (!slot.HasValue) return true; // Teardown has no destination marker.
            Vector3 markerPosition;
            return cameraPanController.IsAtSlot(slot.Value) &&
                cameraPanController.TryGetMarkerPosition(slot.Value, out markerPosition) &&
                Vector3.Distance(controlledCamera.transform.position, markerPosition) < 0.02f;
        }

        private IEnumerator WaitForReady(ShortCyclePhase phase, ShortCycleCameraSlot? slot,
            ShortCyclePhase0View? view, string step, List<string> failures)
        {
            var timeoutSeconds = float.IsNaN(presentationDuration) || float.IsInfinity(presentationDuration)
                ? 1f : Math.Max(0f, presentationDuration) + 1f;
            var startedAt = Time.unscaledTime;
            while (!IsReady(phase, slot, view) && Time.unscaledTime - startedAt < timeoutSeconds)
            {
                SampleFixedBoard();
                yield return null;
            }
            SampleFixedBoard();
            if (!IsReady(phase, slot, view))
                failures.Add(step + " timed out or has missing wiring: expected " + phase + "/" + view +
                    "/" + slot + ", actual phase=" +
                    (coordinator != null && coordinator.Context != null ? coordinator.Context.current_phase.ToString() : "missing") +
                    ", panning=" + (cameraPanController != null ? cameraPanController.IsPanning.ToString() : "missing") +
                    ", locked=" + (inputLockController != null ? inputLockController.Gate.IsLocked.ToString() : "missing"));
        }

        private ShortCycleActionResult Dispatch(string actionName)
        {
            return Dispatch(new ShortCycleActionRequest { ActionName = actionName });
        }

        private ShortCycleActionResult Dispatch(ShortCycleActionRequest request)
        {
            return actionRouter == null
                ? ShortCycleActionResult.Failure("action router missing")
                : actionRouter.DispatchAction(request);
        }

        private static bool Near(Camera camera, Transform marker)
        {
            return camera != null && marker != null &&
                Vector3.Distance(camera.transform.position, marker.position) < 0.02f;
        }

        // P2-C9 explicit debug entry. The old Start/RunProbe/1-27 coroutine remains untouched.
        private bool phase2ProbeRunning;
        private bool phase2ProbeInterrupted;

        public void CancelPhase2MatrixProbe()
        {
            if (phase2ProbeRunning) phase2ProbeInterrupted = true;
        }

        private void OnDisable() { CancelPhase2MatrixProbe(); }
        private void OnDestroy() { CancelPhase2MatrixProbe(); }

        public bool TryRunPhase2MatrixProbe(Phase2SceneEntry entry, KitchenAreaController areas,
            ShortCyclePresentationDirector director, ShortCycleSpace space,
            ShortCycleInteractionLayerStack layers, string firstAreaId, string secondAreaId,
            out ShortCyclePhase2ProbeResult result)
        {
            string error;
            if (!CanStartPhase2Probe(entry, areas, director, space, layers,
                firstAreaId, secondAreaId, out error))
            {
                result = new ShortCyclePhase2ProbeResult(false, false, true, error, null);
                return false;
            }

            var originalView = director.Phase2View;
            var originalArea = areas.CurrentAreaId;
            var ownedArea = originalArea;
            var originalStaple = space.Phase2Staple;
            var originalRecipe = entry.RecipeId;
            var originalReceiveCount = entry.ReceiveCount;
            var originalStep = entry.CurrentStepProgress;
            var originalOwner = entry.KitchenInventory.Phase2Owner;
            var originalPrep = entry.PrepZoneContents;
            var checks = new List<string>();
            var ownedProcessing = (ShortCyclePhase2View?)null;
            var ownedOverlay = ShortCyclePhase2Overlay.None;
            var ownedTransition = false;
            var passed = false;
            var restored = false;
            var diagnostic = (string)null;
            phase2ProbeRunning = true;
            phase2ProbeInterrupted = false;
            try
            {
                RequirePhase2ProbeLive(entry, areas, director, space, layers);
                if (originalView == ShortCyclePhase2View.AreaDetail)
                {
                    ownedArea = null;
                    Require(areas.TryReturnOverview(out error), "return to overview: " + error);
                }
                CheckPhase2Matrix(director, space, layers, ShortCyclePhase2View.Overview,
                    ShortCyclePhase2Overlay.None, false, false, checks);

                RequirePhase2ProbeLive(entry, areas, director, space, layers);
                ownedArea = firstAreaId;
                Require(areas.TryEnterArea(firstAreaId, out error), "enter first area: " + error);
                Require(areas.CurrentAreaId == firstAreaId &&
                    areas.AreaDetailBinding.AreaId == firstAreaId, "first Detail binding");
                CheckPhase2Matrix(director, space, layers, ShortCyclePhase2View.AreaDetail,
                    ShortCyclePhase2Overlay.None, false, false, checks);
                checks.Add("first T14 AreaDetail binding");

                RequirePhase2ProbeLive(entry, areas, director, space, layers);
                var sameBinding = areas.AreaDetailBinding;
                ownedArea = secondAreaId;
                Require(areas.TryEnterArea(secondAreaId, out error), "rebind second area: " + error);
                Require(ReferenceEquals(sameBinding, areas.AreaDetailBinding) &&
                    areas.CurrentAreaId == secondAreaId &&
                    sameBinding.AreaId == secondAreaId, "single Detail instance rebind");
                checks.Add("single Detail rebind");

                RequirePhase2ProbeLive(entry, areas, director, space, layers);
                Require(director.TrySetPhase2StapleBubble(true, out error),
                    "explicit staple condition: " + error);
                CheckPhase2Matrix(director, space, layers, ShortCyclePhase2View.AreaDetail,
                    ShortCyclePhase2Overlay.None, false, true, checks);
                RequirePhase2ProbeLive(entry, areas, director, space, layers);
                Require(director.TrySetPhase2StapleBubble(false, out error),
                    "clear staple condition: " + error);

                RequirePhase2ProbeLive(entry, areas, director, space, layers);
                ownedOverlay = ShortCyclePhase2Overlay.StepDetail;
                Require(director.TrySetPhase2Overlay(ownedOverlay, out error),
                    "open StepDetail overlay: " + error);
                CheckPhase2Matrix(director, space, layers, ShortCyclePhase2View.AreaDetail,
                    ownedOverlay, false, false, checks);
                RequirePhase2ProbeLive(entry, areas, director, space, layers);
                Require(director.TrySetPhase2Overlay(ShortCyclePhase2Overlay.None, out error),
                    "close StepDetail overlay: " + error);
                ownedOverlay = ShortCyclePhase2Overlay.None;

                RequirePhase2ProbeLive(entry, areas, director, space, layers);
                ownedOverlay = ShortCyclePhase2Overlay.ExitConfirm;
                Require(director.TrySetPhase2Overlay(ownedOverlay, out error),
                    "open ExitConfirm overlay: " + error);
                CheckPhase2Matrix(director, space, layers, ShortCyclePhase2View.AreaDetail,
                    ownedOverlay, false, false, checks);
                RequirePhase2ProbeLive(entry, areas, director, space, layers);
                Require(director.TrySetPhase2Overlay(ShortCyclePhase2Overlay.None, out error),
                    "close ExitConfirm overlay: " + error);
                ownedOverlay = ShortCyclePhase2Overlay.None;

                RequirePhase2ProbeLive(entry, areas, director, space, layers);
                ownedProcessing = ShortCyclePhase2View.Interaction;
                Require(director.TrySetPhase2View(ownedProcessing.Value, out error),
                    "open S2a: " + error);
                CheckPhase2Matrix(director, space, layers, ownedProcessing.Value,
                    ShortCyclePhase2Overlay.None, false, false, checks);
                RequirePhase2ProbeLive(entry, areas, director, space, layers);
                Require(director.TrySetPhase2View(ShortCyclePhase2View.AreaDetail, out error),
                    "close owned S2a without production: " + error);
                ownedProcessing = null;

                RequirePhase2ProbeLive(entry, areas, director, space, layers);
                ownedProcessing = ShortCyclePhase2View.MiniGame;
                Require(director.TrySetPhase2View(ownedProcessing.Value, out error),
                    "open S2b: " + error);
                CheckPhase2Matrix(director, space, layers, ownedProcessing.Value,
                    ShortCyclePhase2Overlay.None, false, false, checks);
                RequirePhase2ProbeLive(entry, areas, director, space, layers);
                Require(director.TrySetPhase2View(ShortCyclePhase2View.AreaDetail, out error),
                    "close owned S2b without MG0 result: " + error);
                ownedProcessing = null;

                RequirePhase2ProbeLive(entry, areas, director, space, layers);
                ownedTransition = true;
                Require(director.TrySetPhase2Transition(true, out error),
                    "start S1 transition: " + error);
                CheckPhase2Matrix(director, space, layers, ShortCyclePhase2View.AreaDetail,
                    ShortCyclePhase2Overlay.None, true, false, checks);
                RequirePhase2ProbeLive(entry, areas, director, space, layers);
                Require(director.TrySetPhase2Transition(false, out error),
                    "end S1 transition: " + error);
                ownedTransition = false;
                RequirePhase2ProbeLive(entry, areas, director, space, layers);
                ownedArea = null;
                Require(areas.TryReturnOverview(out error), "return S0: " + error);
                CheckPhase2Matrix(director, space, layers, ShortCyclePhase2View.Overview,
                    ShortCyclePhase2Overlay.None, false, false, checks);

                RequirePhase2ProbeLive(entry, areas, director, space, layers);
                ownedTransition = true;
                Require(director.TrySetPhase2Transition(true, out error),
                    "start S0 transition: " + error);
                CheckPhase2Matrix(director, space, layers, ShortCyclePhase2View.Overview,
                    ShortCyclePhase2Overlay.None, true, false, checks);
                RequirePhase2ProbeLive(entry, areas, director, space, layers);
                Require(director.TrySetPhase2Transition(false, out error),
                    "end S0 transition: " + error);
                ownedTransition = false;
                RequirePhase2ProbeLive(entry, areas, director, space, layers);
                passed = true;
            }
            catch (Exception exception)
            {
                diagnostic = exception.GetType().Name + ": " + exception.Message;
            }
            finally
            {
                string restoreError;
                restored = TryRestorePhase2Probe(entry, areas, director, space, layers,
                    originalView, originalArea, originalStaple, ownedArea, ownedProcessing,
                    ownedOverlay, ownedTransition, out restoreError);
                if (!restored)
                    diagnostic = (diagnostic == null ? "" : diagnostic + " | ") +
                        "Restoration failed: " + restoreError;
                if (!ReferenceEquals(entry.KitchenInventory == null ? null :
                        entry.KitchenInventory.Phase2Owner, originalOwner) ||
                    entry.RecipeId != originalRecipe || entry.ReceiveCount != originalReceiveCount ||
                    !ReferenceEquals(entry.CurrentStepProgress, originalStep) ||
                    !SamePhase2Prep(originalPrep, entry.PrepZoneContents))
                {
                    passed = false;
                    diagnostic = (diagnostic == null ? "" : diagnostic + " | ") +
                        "P2 business snapshot changed during presentation probe.";
                }
                if (phase2ProbeInterrupted || !enabled || !gameObject.activeInHierarchy)
                {
                    passed = false;
                    if (diagnostic == null ||
                        diagnostic.IndexOf("interrupted", StringComparison.OrdinalIgnoreCase) < 0)
                        diagnostic = (diagnostic == null ? "" : diagnostic + " | ") +
                            "P2 probe was interrupted during execution or restoration.";
                }
                phase2ProbeRunning = false;
                phase2ProbeInterrupted = false;
            }
            result = new ShortCyclePhase2ProbeResult(true, passed && restored,
                restored, diagnostic, checks);
            if (result.Succeeded) Debug.Log("P2-C9 MATRIX PROBE PASS " + checks.Count + " checks", this);
            else Debug.LogWarning("P2-C9 MATRIX PROBE FAIL " + result.Diagnostic, this);
            return result.Succeeded;
        }

        private bool CanStartPhase2Probe(Phase2SceneEntry entry, KitchenAreaController areas,
            ShortCyclePresentationDirector director, ShortCycleSpace space,
            ShortCycleInteractionLayerStack layers, string firstAreaId, string secondAreaId,
            out string error)
        {
            error = null;
            if (!enabled || !gameObject.activeInHierarchy || phase2ProbeRunning)
            { error = "P2 probe is disabled, inactive or already running."; return false; }
            if (entry == null || areas == null || director == null || space == null || layers == null ||
                !entry.IsReady || entry.KitchenInventory == null ||
                !ReferenceEquals(entry.KitchenInventory.Phase2Owner, entry) ||
                !ReferenceEquals(areas.SceneEntry, entry) ||
                !ReferenceEquals(areas.PresentationDirector, director) || !areas.IsReady ||
                !ReferenceEquals(PrivateProbeField(director, "phase2Owner"), space) ||
                !ReferenceEquals(PrivateProbeField(director, "interactionLayerStack"), layers) ||
                space.SpaceId != ShortCycleSpaceId.Phase2 || !space.Phase2Configured ||
                !director.Phase2ViewActive || space.Phase2Transition ||
                space.Phase2Overlay != ShortCyclePhase2Overlay.None ||
                (space.Phase2View != ShortCyclePhase2View.Overview &&
                 space.Phase2View != ShortCyclePhase2View.AreaDetail) ||
                layers.Count != 2 || !layers.IsTop(space.SpaceLayer) ||
                layers.Contains(space.Phase2ModalLayer))
            { error = "P2 probe requires a live owned S0/S1 scene, exact Space/LayerStack and no busy or unknown input layer."; return false; }
            if (!space.TryValidatePhase2Table(out error) ||
                areas.AreaDetailBinding == null ||
                !director.TryValidatePhase2Configuration(areas.AreaDetailBinding.AreaDetailSet, out error))
                return false;
            ShortCyclePresentationSet detail;
            if (!space.TryGetPhase2Set(ShortCyclePhase2SetRole.AreaDetail, out detail) ||
                !ReferenceEquals(detail, areas.AreaDetailBinding.AreaDetailSet))
            { error = "P2 probe AreaDetail binding does not match the active Space."; return false; }
            if (space.Phase2View == ShortCyclePhase2View.Overview
                ? (areas.CurrentAreaId != null || areas.AreaDetailBinding.IsBound)
                : (!areas.AreaDetailBinding.IsBound ||
                   areas.CurrentAreaId != areas.AreaDetailBinding.AreaId))
            { error = "P2 probe base state and area binding disagree."; return false; }
            if (string.IsNullOrWhiteSpace(firstAreaId) || string.IsNullOrWhiteSpace(secondAreaId) ||
                firstAreaId == secondAreaId)
            { error = "P2 probe requires two distinct explicit T14 area IDs."; return false; }
            var catalog = entry.KitchenInventory.DataCatalog;
            KitchenAreaData row;
            if (catalog == null ||
                !catalog.TryGetKitchenArea(firstAreaId, out row, out error) || row == null ||
                !row.is_unlocked_default ||
                !catalog.TryGetKitchenArea(secondAreaId, out row, out error) || row == null ||
                !row.is_unlocked_default)
            { error = error ?? "P2 probe area is missing or not default-unlocked."; return false; }
            if (space.Phase2View == ShortCyclePhase2View.AreaDetail &&
                (!catalog.TryGetKitchenArea(areas.CurrentAreaId, out row, out error) ||
                 row == null || !row.is_unlocked_default))
            { error = error ?? "P2 probe original area cannot be restored."; return false; }
            error = null; return true;
        }

        private static object PrivateProbeField(object owner, string name)
        {
            var field = owner.GetType().GetField(name,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            return field == null ? null : field.GetValue(owner);
        }

        private void RequirePhase2ProbeLive(Phase2SceneEntry entry, KitchenAreaController areas,
            ShortCyclePresentationDirector director, ShortCycleSpace space,
            ShortCycleInteractionLayerStack layers)
        {
            if (phase2ProbeInterrupted || !enabled || !gameObject.activeInHierarchy ||
                entry == null || !entry.IsReady || entry.KitchenInventory == null ||
                !ReferenceEquals(entry.KitchenInventory.Phase2Owner, entry) ||
                !areas.IsReady || !ReferenceEquals(PrivateProbeField(director, "phase2Owner"), space) ||
                !ReferenceEquals(PrivateProbeField(director, "interactionLayerStack"), layers) ||
                (layers.Top != space.SpaceLayer && layers.Top != space.Phase2ModalLayer))
                throw new InvalidOperationException("P2 probe was interrupted or lost its scene/input owner.");
        }

        private static void Require(bool condition, string diagnostic)
        {
            if (!condition) throw new InvalidOperationException(diagnostic);
        }

        private static bool RoleActive(ShortCyclePhase2SetRole role, ShortCyclePhase2View view,
            ShortCyclePhase2Overlay overlay, bool transition, bool staple)
        {
            if (transition) return false;
            switch (role)
            {
                case ShortCyclePhase2SetRole.TopBar:
                case ShortCyclePhase2SetRole.PrepTray: return true;
                case ShortCyclePhase2SetRole.Overview:
                case ShortCyclePhase2SetRole.RestockEntry: return view == ShortCyclePhase2View.Overview;
                case ShortCyclePhase2SetRole.AreaDetail: return view != ShortCyclePhase2View.Overview;
                case ShortCyclePhase2SetRole.HandTool: return view == ShortCyclePhase2View.AreaDetail;
                case ShortCyclePhase2SetRole.InteractionModal: return view == ShortCyclePhase2View.Interaction;
                case ShortCyclePhase2SetRole.MiniGameModal: return view == ShortCyclePhase2View.MiniGame;
                case ShortCyclePhase2SetRole.StapleBubble: return view == ShortCyclePhase2View.AreaDetail && staple;
                case ShortCyclePhase2SetRole.Escape:
                case ShortCyclePhase2SetRole.StepDetailEntry:
                    return view == ShortCyclePhase2View.Overview || view == ShortCyclePhase2View.AreaDetail;
                case ShortCyclePhase2SetRole.StepDetailModal: return overlay == ShortCyclePhase2Overlay.StepDetail;
                case ShortCyclePhase2SetRole.ExitConfirmModal: return overlay == ShortCyclePhase2Overlay.ExitConfirm;
            }
            return false;
        }

        private static bool RoleFocused(ShortCyclePhase2SetRole role, ShortCyclePhase2View view,
            ShortCyclePhase2Overlay overlay, bool active)
        {
            if (!active) return false;
            if (overlay != ShortCyclePhase2Overlay.None)
                return overlay == ShortCyclePhase2Overlay.StepDetail
                    ? role == ShortCyclePhase2SetRole.StepDetailModal
                    : role == ShortCyclePhase2SetRole.ExitConfirmModal;
            if (view == ShortCyclePhase2View.Interaction)
                return role == ShortCyclePhase2SetRole.InteractionModal;
            if (view == ShortCyclePhase2View.MiniGame)
                return role == ShortCyclePhase2SetRole.MiniGameModal;
            return true;
        }

        private static void CheckPhase2Matrix(ShortCyclePresentationDirector director,
            ShortCycleSpace space, ShortCycleInteractionLayerStack layers,
            ShortCyclePhase2View view, ShortCyclePhase2Overlay overlay,
            bool transition, bool staple, List<string> checks)
        {
            Require(director.Phase2View == view && space.Phase2View == view &&
                space.Phase2Overlay == overlay && space.Phase2Transition == transition,
                "P2 view/overlay/transition state diverged.");
            var modal = transition || overlay != ShortCyclePhase2Overlay.None ||
                view == ShortCyclePhase2View.Interaction || view == ShortCyclePhase2View.MiniGame;
            Require(layers.IsTop(modal ? space.Phase2ModalLayer : space.SpaceLayer) &&
                layers.Count == (modal ? 3 : 2), "P2 input layer is not the expected top layer.");
            foreach (ShortCyclePhase2SetRole role in Enum.GetValues(typeof(ShortCyclePhase2SetRole)))
            {
                ShortCyclePresentationSet set;
                Require(space.TryGetPhase2Set(role, out set), "P2 role is missing: " + role);
                var active = RoleActive(role, view, overlay, transition, staple);
                var focused = RoleFocused(role, view, overlay, active);
                Require(set.IsVisibilityRequested == active && set.IsFocused == focused,
                    "P2 Set active/focus mismatch: " + view + "/" + overlay + "/" + role);
                if (!focused)
                {
                    var gates = PrivateProbeField(set, "gatedInteractions") as MonoBehaviour[];
                    foreach (var gate in gates ?? new MonoBehaviour[0])
                    {
                        if (gate == null) continue;
                        var knownGate = gate as ShortCycleStateGatedInteraction;
                        if (knownGate != null)
                            Require(!knownGate.InteractionAllowed,
                                "P2 unfocused Set still accepts input: " + role);
                        else if (!(gate is IShortCycleStateGatedInteraction))
                            Require(!gate.enabled,
                                "P2 unfocused Set still accepts input: " + role);
                    }
                }
                checks.Add(view + "/" + overlay + "/" + transition + "/" + role);
            }
            checks.Add("input top " + layers.Top.name);
        }

        private static bool TryRestorePhase2Probe(Phase2SceneEntry entry,
            KitchenAreaController areas, ShortCyclePresentationDirector director,
            ShortCycleSpace space, ShortCycleInteractionLayerStack layers,
            ShortCyclePhase2View originalView, string originalArea, bool originalStaple,
            string ownedArea, ShortCyclePhase2View? ownedProcessing, ShortCyclePhase2Overlay ownedOverlay,
            bool ownedTransition, out string error)
        {
            try
            {
                if (entry == null || !entry.IsReady || entry.KitchenInventory == null ||
                    !ReferenceEquals(entry.KitchenInventory.Phase2Owner, entry) ||
                    !areas.IsReady ||
                    !ReferenceEquals(PrivateProbeField(director, "phase2Owner"), space) ||
                    !ReferenceEquals(PrivateProbeField(director, "interactionLayerStack"), layers))
                { error = "P2 ownership changed; no foreign state was unwound."; return false; }
                if (areas.CurrentAreaId != ownedArea ||
                    (ownedTransition && !space.Phase2Transition) ||
                    (ownedOverlay != ShortCyclePhase2Overlay.None &&
                     space.Phase2Overlay != ownedOverlay) ||
                    (ownedProcessing.HasValue && space.Phase2View != ownedProcessing.Value))
                { error = "Owned P2 state changed externally; restoration stopped."; return false; }
                string operationError = null;
                if (ownedTransition && space.Phase2Transition)
                {
                    if (!layers.IsTop(space.Phase2ModalLayer) ||
                        !director.TrySetPhase2Transition(false, out operationError))
                    { error = "Owned transition could not close: " + operationError; return false; }
                }
                if (ownedOverlay != ShortCyclePhase2Overlay.None &&
                    space.Phase2Overlay == ownedOverlay)
                {
                    if (!layers.IsTop(space.Phase2ModalLayer) ||
                        !director.TrySetPhase2Overlay(ShortCyclePhase2Overlay.None, out operationError))
                    { error = "Owned overlay could not close: " + operationError; return false; }
                }
                if (ownedProcessing.HasValue && space.Phase2View == ownedProcessing.Value)
                {
                    if (!layers.IsTop(space.Phase2ModalLayer) ||
                        !director.TrySetPhase2View(ShortCyclePhase2View.AreaDetail, out operationError))
                    { error = "Owned processing shell could not close: " + operationError; return false; }
                }
                if (space.Phase2Transition || space.Phase2Overlay != ShortCyclePhase2Overlay.None ||
                    (space.Phase2View != ShortCyclePhase2View.Overview &&
                     space.Phase2View != ShortCyclePhase2View.AreaDetail) ||
                    !layers.IsTop(space.SpaceLayer) || layers.Count != 2)
                { error = "P2 state or layer became unknown; restoration stopped."; return false; }
                if (originalView == ShortCyclePhase2View.Overview)
                {
                    if (space.Phase2View == ShortCyclePhase2View.AreaDetail &&
                        !areas.TryReturnOverview(out operationError))
                    { error = "Cannot restore original S0: " + operationError; return false; }
                }
                else
                {
                    if (space.Phase2View == ShortCyclePhase2View.Overview ||
                        areas.CurrentAreaId != originalArea)
                    {
                        if (!areas.TryEnterArea(originalArea, out operationError))
                        { error = "Cannot restore original S1 area: " + operationError; return false; }
                    }
                    if (!director.TrySetPhase2StapleBubble(originalStaple, out operationError))
                    { error = "Cannot restore original staple condition: " + operationError; return false; }
                }
                if (entry == null || !entry.IsReady || entry.KitchenInventory == null ||
                    !ReferenceEquals(entry.KitchenInventory.Phase2Owner, entry) ||
                    !areas.IsReady ||
                    !ReferenceEquals(PrivateProbeField(director, "phase2Owner"), space) ||
                    !ReferenceEquals(PrivateProbeField(director, "interactionLayerStack"), layers) ||
                    director.Phase2View != originalView ||
                    space.Phase2View != originalView ||
                    space.Phase2Overlay != ShortCyclePhase2Overlay.None || space.Phase2Transition ||
                    !layers.IsTop(space.SpaceLayer) || layers.Count != 2 ||
                    (originalView == ShortCyclePhase2View.Overview
                        ? (areas.CurrentAreaId != null || areas.AreaDetailBinding.IsBound)
                        : (areas.CurrentAreaId != originalArea ||
                           areas.AreaDetailBinding.AreaId != originalArea ||
                           space.Phase2Staple != originalStaple)))
                { error = "P2 original view/area/condition did not restore exactly."; return false; }
                error = null; return true;
            }
            catch (Exception exception)
            { error = exception.GetType().Name + ": " + exception.Message; return false; }
        }

        private static bool SamePhase2Prep(IList<PrepZoneItem> before, IList<PrepZoneItem> after)
        {
            if (before == null || after == null || before.Count != after.Count) return false;
            for (var index = 0; index < before.Count; index++)
            {
                var left = before[index]; var right = after[index];
                if (left == null || right == null ||
                    left.item_id != right.item_id || left.instance_id != right.instance_id ||
                    left.quantity != right.quantity || left.drag_order != right.drag_order ||
                    left.state != right.state || left.display_name != right.display_name)
                    return false;
            }
            return true;
        }

    }
}
