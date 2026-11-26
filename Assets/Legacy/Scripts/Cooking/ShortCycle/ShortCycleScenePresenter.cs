using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using EatWhat.Tools.Localization;

namespace EatWhat.Cooking.ShortCycle
{
    /// <summary>
    /// Thin scene presenter for generated-data text and world-space transitions.
    /// State ownership lives in the explicit state classes; automated validation
    /// lives in the optional ShortCycleAutomatedProbe component.
    /// </summary>
    public sealed class ShortCycleScenePresenter : MonoBehaviour
    {
        [Header("Short-cycle runtime")]
        [SerializeField] private ShortCycleSessionManager coordinator = null;
        [SerializeField] private ShortCycleActionRouter actionRouter = null;
        [SerializeField] private ShortCycleInteractionLayerStack interactionLayerStack = null;
        [SerializeField] private MouseInteractionLayer clueBoardLayer = null;

        [Header("Generated data")]
        [SerializeField] private string recipeId = "rcp_tomato_egg";
        [SerializeField] private CK01GeneratedDataCatalog dataCatalog = null;
        [SerializeField] private InventoryManager inventoryManager = null;
        [SerializeField] private FridgeSlot[] fridgeSlots = new FridgeSlot[0];
        [SerializeField] private SlotBlock[] clueSlotBlocks = new SlotBlock[0];

        [Header("Presentation")]
        [SerializeField] private GameObject coverView = null;
        [SerializeField] private GameObject recipeBrowseView = null;
        [SerializeField] private GameObject recipeCatalogView = null;
        [SerializeField] private Transform clueBoardShell = null;
        [SerializeField] private Transform clueBoardPhase0Anchor = null;
        [SerializeField] private Transform clueBoardPhase1Anchor = null;
        [SerializeField] private GameObject clueBoardCollapsedVisual = null;
        [SerializeField] private GameObject clueBoardExpandedVisual = null;
        [SerializeField] private GameObject stepDetailModalView = null;
        [SerializeField] private TextMeshPro statusText = null;
        [SerializeField] private TextMeshPro dataText = null;
        [SerializeField] private float presentationDuration = 0.45f;

        private Coroutine clueBoardMove;
        private bool clueBoardExpanded;

        public string DataSummary { get; private set; }

        private void OnEnable()
        {
            if (coordinator != null)
            {
                coordinator.StateChanged += HandleStateChanged;
            }
        }

        private void OnDisable()
        {
            if (coordinator != null)
            {
                coordinator.StateChanged -= HandleStateChanged;
            }
        }

        private void Start()
        {
            string dataError;
            if (!TryBuildGeneratedDataSummary(out dataError))
            {
                DataSummary = dataError;
                Debug.LogError("CK01-C-DATA FAIL " + dataError, this);
                UpdateStatus("Generated data wiring failed");
                return;
            }

            if (!TryBindClueSlots(out dataError))
            {
                Debug.LogError("CK01-C-T2 FAIL " + dataError, this);
                UpdateStatus("T2 slot wiring failed");
                return;
            }

            if (!TryBindFridgeSlots(out dataError))
            {
                Debug.LogError("CK01-C-T15 FAIL " + dataError, this);
                UpdateStatus("T15 fridge wiring failed");
                return;
            }

            var unresolvedFallbackTokens = LocalizedText.RemoveFallbackTokens(gameObject.scene);
            var localizedCount = LocalizedText.RefreshAll(gameObject.scene);

            var begin = coordinator == null
                ? ShortCycleTransitionResult.Failure("ShortCycleSessionManager is not connected.")
                : coordinator.TryBeginSession(recipeId, ShortCycleTriggerContext.FreeTry);
            if (!begin.Succeeded)
            {
                Debug.LogError("CK01-C-SESSION FAIL " + begin.Error, this);
                UpdateStatus("Session entry failed");
                return;
            }

            Debug.Log("CK01-C-DATA PASS " + DataSummary +
                " localizedTmp=" + localizedCount +
                " unresolvedFallbackTmp=" + unresolvedFallbackTokens.Count +
                " tokens=" + string.Join(",", unresolvedFallbackTokens), this);
            Debug.Log("CK01-C-STATE default=Phase0 recipe=" + coordinator.Context.current_recipe_id, this);
        }

        private bool TryBindFridgeSlots(out string diagnostic)
        {
            if (dataCatalog == null || inventoryManager == null)
            {
                diagnostic = "Generated data catalog and InventoryManager are required for T15 fridge binding.";
                return false;
            }

            IList<CK01InitialInventoryRecord> inventoryRecords;
            if (!dataCatalog.TryGetInitialInventory(out inventoryRecords, out diagnostic))
            {
                return false;
            }

            var targets = fridgeSlots ?? new FridgeSlot[0];
            if (targets.Length < inventoryRecords.Count)
            {
                diagnostic = "InitialInventory requires " + inventoryRecords.Count +
                    " FridgeSlots, but only " + targets.Length + " are connected.";
                return false;
            }

            for (var index = 0; index < targets.Length; index++)
            {
                var target = targets[index];
                if (target == null)
                {
                    diagnostic = "FridgeSlot reference " + index + " is not connected.";
                    return false;
                }

                var active = index < inventoryRecords.Count;
                target.gameObject.SetActive(active);
                if (!active)
                {
                    continue;
                }

                var record = inventoryRecords[index];
                CK01ItemData item;
                if (record == null || record.Data == null ||
                    !inventoryManager.TryGetGeneratedItem(record.ItemId, out item))
                {
                    diagnostic = "InitialInventory entry " + index +
                        " cannot be resolved through InventoryManager.";
                    return false;
                }

                target.ConfigureItem(
                    item,
                    inventoryManager.GetQuantity(record.ItemId),
                    item == null || item.icon_sprite == null);
            }

            diagnostic = null;
            return true;
        }

        private bool TryBindClueSlots(out string diagnostic)
        {
            if (dataCatalog == null || inventoryManager == null)
            {
                diagnostic = "Generated data catalog and InventoryManager are required for T2 slot binding.";
                return false;
            }

            CK01RecipeData recipe;
            if (!dataCatalog.TryGetRecipe(recipeId, out recipe, out diagnostic))
            {
                return false;
            }

            var targets = clueSlotBlocks ?? new SlotBlock[0];
            if (targets.Length < recipe.slots.Count)
            {
                diagnostic = "Recipe '" + recipeId + "' requires " + recipe.slots.Count +
                    " SlotBlocks, but only " + targets.Length + " are connected.";
                return false;
            }

            for (var index = 0; index < targets.Length; index++)
            {
                if (targets[index] == null)
                {
                    diagnostic = "Clue SlotBlock reference " + index + " is not connected.";
                    return false;
                }

                var active = index < recipe.slots.Count;
                targets[index].gameObject.SetActive(active);
                if (!active)
                {
                    continue;
                }

                CK01RecipeSlotData slot;
                if (!dataCatalog.TryGetRecipeSlot(recipe.slots[index], out slot, out diagnostic))
                {
                    return false;
                }

                ShortCycleSlotBlockBinding binding;
                if (!ShortCycleRecipeSlotBinding.TryResolve(
                    slot,
                    ResolveItem,
                    ResolveTag,
                    inventoryManager.GetQuantity,
                    IsIngredientUnlockedInDemo,
                    null,
                    out binding,
                    out diagnostic))
                {
                    return false;
                }
                if (!targets[index].ConfigureSlot(binding, out diagnostic))
                {
                    return false;
                }
            }

            diagnostic = null;
            return true;
        }

        private CK01ItemData ResolveItem(string itemId)
        {
            CK01ItemData item;
            string ignored;
            return dataCatalog.TryGetItem(itemId, out item, out ignored) ? item : null;
        }

        private CK01TagData ResolveTag(string tagId)
        {
            CK01TagData tag;
            string ignored;
            return dataCatalog.TryGetTag(tagId, out tag, out ignored) ? tag : null;
        }

        private static bool IsIngredientUnlockedInDemo(string itemId)
        {
            // G-series demo decision: recipe, kitchen, and ingredient visibility are fully unlocked.
            return !string.IsNullOrEmpty(itemId);
        }

        public ShortCycleActionResult DispatchStartCooking()
        {
            return Dispatch(ShortCycleActionNames.StartCooking);
        }

        public void OnStartCookingButton()
        {
            DispatchStartCooking();
        }

        public ShortCycleActionResult DispatchReturnToPhase0()
        {
            return Dispatch(ShortCycleActionNames.Return);
        }

        public void OnReturnButton()
        {
            DispatchReturnToPhase0();
        }

        public ShortCycleActionResult DispatchEscape()
        {
            return Dispatch(ShortCycleActionNames.Escape);
        }

        public void OnEscapeButton()
        {
            DispatchEscape();
        }

        public void ToggleClueBoardShell()
        {
            SetClueBoardExpanded(!clueBoardExpanded);
        }

        public void SetClueBoardExpanded(bool expanded)
        {
            var result = Dispatch(expanded
                ? ShortCycleActionNames.OpenClueBoard
                : ShortCycleActionNames.CloseClueBoard);
            if (!result.Succeeded)
            {
                Debug.LogWarning("CK01-C clue-board state rejected: " + result.Error, this);
            }
        }

        private void ApplyClueBoardExpanded(bool expanded)
        {
            clueBoardExpanded = expanded;
            if (clueBoardCollapsedVisual != null)
            {
                clueBoardCollapsedVisual.SetActive(!expanded);
            }
            if (clueBoardExpandedVisual != null)
            {
                clueBoardExpandedVisual.SetActive(expanded);
            }

            if (interactionLayerStack != null && clueBoardLayer != null)
            {
                if (expanded)
                {
                    interactionLayerStack.Push(clueBoardLayer);
                }
                else if (interactionLayerStack.IsTop(clueBoardLayer))
                {
                    interactionLayerStack.Pop(clueBoardLayer);
                }
            }
        }

        private ShortCycleActionResult Dispatch(string actionName)
        {
            return actionRouter == null
                ? ShortCycleActionResult.Failure("ShortCycleActionRouter is not connected.")
                : actionRouter.DispatchAction(actionName);
        }

        private bool TryBuildGeneratedDataSummary(out string error)
        {
            if (dataCatalog == null)
            {
                error = "Generated data catalog is not connected.";
                return false;
            }

            CK01RecipeData generatedRecipe;
            if (!dataCatalog.TryGetRecipe(recipeId, out generatedRecipe, out error))
            {
                return false;
            }

            LocTableAsset localizationTable;
            if (!dataCatalog.TryGetLocalization(out localizationTable, out error))
            {
                return false;
            }
            LocService.Configure(localizationTable);
            if (localizationTable.entries == null || localizationTable.entries.Count == 0)
            {
                error = "Generated Localization table is empty.";
                return false;
            }
            IList<CK01RecipeStepData> ordered;
            if (!dataCatalog.TryGetRecipeStepChain(recipeId, out ordered, out error))
            {
                return false;
            }

            var builder = new StringBuilder();
            builder.Append("recipe=").Append(recipeId)
                .Append(" dish=").Append(LocService.Get(generatedRecipe.name_key))
                .Append(" steps=");
            for (var index = 0; index < ordered.Count; index++)
            {
                if (index > 0) builder.Append(" -> ");
                builder.Append(ordered[index].name)
                    .Append('[').Append(LocService.Get(ordered[index].display_text_key)).Append(']');
            }

            DataSummary = builder.ToString();
            if (dataText != null)
            {
                dataText.text = "CSV > Generated > Manager\n" + DataSummary;
            }
            error = null;
            return true;
        }

        private void MoveClueBoard(ShortCyclePhase phase, bool instant)
        {
            var anchor = phase == ShortCyclePhase.Phase1
                ? clueBoardPhase1Anchor
                : clueBoardPhase0Anchor;
            if (clueBoardShell == null || anchor == null)
            {
                return;
            }

            if (clueBoardMove != null)
            {
                StopCoroutine(clueBoardMove);
                clueBoardMove = null;
            }
            if (instant)
            {
                clueBoardShell.position = anchor.position;
                return;
            }
            clueBoardMove = StartCoroutine(MoveClueBoardRoutine(anchor.position));
        }

        private IEnumerator MoveClueBoardRoutine(Vector3 destination)
        {
            var start = clueBoardShell.position;
            var startedAt = Time.unscaledTime;
            var duration = presentationDuration <= 0f ? 0.0001f : presentationDuration;
            while (Time.unscaledTime - startedAt < duration)
            {
                var progress = Mathf.Clamp01((Time.unscaledTime - startedAt) / duration);
                clueBoardShell.position = Vector3.Lerp(start, destination, Mathf.SmoothStep(0f, 1f, progress));
                yield return null;
            }
            clueBoardShell.position = destination;
            clueBoardMove = null;
        }

        private void UpdateStatus(string value)
        {
            if (statusText != null)
            {
                statusText.text = "CK01-C-T2\n" + value;
            }
        }

        private void HandleStateChanged(ShortCyclePhase phase, ShortCyclePhase0View phase0View)
        {
            // VERIFY-TEMP: 人工验收辅助。框架落地后由 CK01-K 接管，接管前不得删除。
            ApplyPhase0View(phase, phase0View);
            if (phase == ShortCyclePhase.Phase0)
            {
                MoveClueBoard(ShortCyclePhase.Phase0, false);
                ApplyClueBoardExpanded(phase0View == ShortCyclePhase0View.ClueBoardShell);
                UpdateStatus("Phase0 / " + phase0View);
            }
            else if (phase == ShortCyclePhase.Phase1)
            {
                MoveClueBoard(ShortCyclePhase.Phase1, false);
                ApplyClueBoardExpanded(false);
                UpdateStatus("Phase1 / state retained");
            }
            else if (phase == ShortCyclePhase.Phase2)
            {
                UpdateStatus("Phase2 handoff");
            }
            else
            {
                ApplyClueBoardExpanded(false);
                UpdateStatus("Context cleared / upstream exit placeholder");
            }
        }

        private void ApplyPhase0View(ShortCyclePhase phase, ShortCyclePhase0View phase0View)
        {
            var isPhase0 = phase == ShortCyclePhase.Phase0;
            if (coverView != null)
            {
                coverView.SetActive(isPhase0 && phase0View == ShortCyclePhase0View.Cover);
            }
            if (recipeBrowseView != null)
            {
                recipeBrowseView.SetActive(isPhase0 &&
                    phase0View != ShortCyclePhase0View.Cover &&
                    phase0View != ShortCyclePhase0View.RecipeCatalog);
            }
            if (recipeCatalogView != null)
            {
                recipeCatalogView.SetActive(isPhase0 && phase0View == ShortCyclePhase0View.RecipeCatalog);
            }
            if (stepDetailModalView != null)
            {
                stepDetailModalView.SetActive(isPhase0 && phase0View == ShortCyclePhase0View.StepDetail);
            }
        }

    }
}
