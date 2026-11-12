using System;
using System.Collections.Generic;
using EatWhat.Tools.Localization;
using TMPro;
using UnityEngine;

namespace EatWhat.Cooking.ShortCycle
{
    [Serializable]
    public sealed class ShortCycleActionIconBinding
    {
        public string actionId;
        public Sprite icon;
    }

    /// <summary>Catalog-backed clue binding; presentation activation remains owned by the FSM host.</summary>
    [DisallowMultipleComponent]
    public sealed class ShortCycleClueBoardBinder : MonoBehaviour
    {
        [SerializeField] private SlotBlock[] slotBlocks = new SlotBlock[0];
        [SerializeField] private LocalizedText[] slotNameBindings = new LocalizedText[0];
        [SerializeField] private ShortCycleActionIconBinding[] actionIcons = new ShortCycleActionIconBinding[0];
        [SerializeField] private Sprite generatedMissingSprite = null;
        [SerializeField] private Sprite missingIconReplacement = null;

        private CK01GeneratedDataCatalog boundCatalog;
        private IShortCycleInventoryQuery boundInventory;
        private IShortCycleInventoryChangeSource changeSource;
        private string boundRecipeId;
        public string BoundRecipeId { get { return boundRecipeId; } }
        public int InventoryRefreshCount { get; private set; }
        private UnlockManager ingredientUnlocks;
        private bool isDisabled;
        private bool hasContentBinding;

        /// <summary>Supply a read-only ingredient profile; null retains demo all-unlocked policy.</summary>
        public void ConfigureIngredientUnlocks(UnlockManager source)
        {
            ingredientUnlocks = source;
            if (boundCatalog != null && !string.IsNullOrEmpty(boundRecipeId))
            {
                string diagnostic;
                Bind(boundCatalog, boundRecipeId, boundInventory, out diagnostic);
            }
        }

        public bool Bind(CK01GeneratedDataCatalog catalog, string recipeId,
            IShortCycleInventoryQuery inventory, out string diagnostic)
        {
            var unlocks = ingredientUnlocks ?? new UnlockManager(catalog, null);
            if (!TryBindCore(catalog, inventory, recipeId, slotBlocks, slotNameBindings,
                unlocks.IsIngredientUnlocked, ResolveActionIcon, generatedMissingSprite,
                missingIconReplacement, true, out diagnostic)) return false;
            DetachInventory();
            boundCatalog = string.IsNullOrEmpty(recipeId) ? null : catalog;
            boundInventory = string.IsNullOrEmpty(recipeId) ? null : inventory;
            boundRecipeId = string.IsNullOrEmpty(recipeId) ? null : recipeId;
            hasContentBinding = true;
            ApplyBoundPresentationVisibility();
            if (!isDisabled) AttachInventory();
            return true;
        }

        public static bool TryBind(CK01GeneratedDataCatalog catalog, IShortCycleInventoryQuery inventory,
            string recipeId, SlotBlock[] targets, LocalizedText[] localizedNames, out string diagnostic)
        {
            var unlocks = new UnlockManager(catalog, null);
            return TryBindCore(catalog, inventory, recipeId, targets, localizedNames,
                unlocks.IsIngredientUnlocked, null, null, null, false, out diagnostic);
        }

        private static bool TryBindCore(CK01GeneratedDataCatalog catalog, IShortCycleInventoryQuery inventory,
            string recipeId, SlotBlock[] targets, LocalizedText[] localizedNames,
            Func<string, bool> isIngredientUnlocked, Func<string, Sprite> actionIcon,
            Sprite generatedMissingSprite, Sprite missingIconReplacement, bool reportMissing,
            out string diagnostic)
        {
            var blocks = targets ?? new SlotBlock[0];
            var names = localizedNames ?? new LocalizedText[0];
            if (string.IsNullOrEmpty(recipeId))
            {
                ClearExcess(blocks, names, 0);
                diagnostic = null;
                return true;
            }
            if (catalog == null || inventory == null)
            {
                diagnostic = "Catalog and a read-only inventory query are required for clue binding.";
                return false;
            }
            RecipeView recipe;
            if (!catalog.TryGetRecipeView(recipeId, out recipe, out diagnostic)) return false;
            if (blocks.Length < recipe.slots.Count)
            {
                diagnostic = "Recipe '" + recipeId + "' requires " + recipe.slots.Count + " SlotBlocks.";
                return false;
            }
            var bindings = new ShortCycleSlotBlockBinding[recipe.slots.Count];
            for (var index = 0; index < recipe.slots.Count; index++)
            {
                if (blocks[index] == null)
                {
                    diagnostic = "Clue SlotBlock reference " + index + " is not connected.";
                    return false;
                }
                if (!ShortCycleRecipeSlotBinding.TryResolve(recipe.slots[index],
                    itemId => ResolveItem(catalog, itemId), tagId => ResolveTag(catalog, tagId),
                    inventory.GetQuantity, isIngredientUnlocked, null,
                    out bindings[index], out diagnostic)) return false;
                if (!blocks[index].ValidateConfiguration(out diagnostic)) return false;
                if (actionIcon != null && !string.IsNullOrEmpty(bindings[index].StandardActionId))
                    bindings[index].ActionIcon = actionIcon(bindings[index].StandardActionId);
            }
            var notices = new List<string>();
            for (var index = 0; index < bindings.Length; index++)
            {
                string blockDiagnostic;
                if (!blocks[index].ConfigureSlot(bindings[index], generatedMissingSprite,
                    missingIconReplacement, out blockDiagnostic))
                { diagnostic = blockDiagnostic; return false; }
                blocks[index].ApplyContentPresentationVisibility(true);
                if (reportMissing && !string.IsNullOrEmpty(blockDiagnostic))
                    notices.Add("slot " + index + ": " + blockDiagnostic);
                if (index < names.Length && names[index] != null)
                    names[index].Configure(bindings[index].StandardItem.name_key, bindings[index].StandardItem.name);
            }
            ClearExcess(blocks, names, bindings.Length);
            diagnostic = notices.Count == 0 ? null : string.Join("; ", notices.ToArray());
            return true;
        }

        private static void ClearExcess(SlotBlock[] blocks, LocalizedText[] names, int first)
        {
            for (var index = first; index < blocks.Length; index++)
                if (blocks[index] != null)
                { blocks[index].ClearSlot(); blocks[index].ApplyContentPresentationVisibility(false); }
            for (var index = first; index < names.Length; index++)
                if (names[index] != null) names[index].Configure(null, null);
        }

        private Sprite ResolveActionIcon(string actionId)
        {
            foreach (var entry in actionIcons ?? new ShortCycleActionIconBinding[0])
                if (entry != null && string.Equals(entry.actionId, actionId, StringComparison.Ordinal))
                    return entry.icon;
            return null;
        }

        private void AttachInventory()
        {
            changeSource = boundInventory as IShortCycleInventoryChangeSource;
            if (changeSource != null) changeSource.Changed += OnInventoryChanged;
        }
        private void DetachInventory()
        {
            if (changeSource != null) changeSource.Changed -= OnInventoryChanged;
            changeSource = null;
        }
        private void OnInventoryChanged()
        {
            InventoryRefreshCount++;
            if (boundCatalog == null || boundInventory == null || string.IsNullOrEmpty(boundRecipeId)) return;
            RefreshPresentationContent();
        }

        // RendererHidden keeps this GameObject active. Recompute content, then reapply the Set gate.
        internal void RefreshPresentationContent()
        {
            if (!hasContentBinding) return;
            string diagnostic;
            var success = TryBindCore(boundCatalog, boundInventory, boundRecipeId, slotBlocks, slotNameBindings,
                (ingredientUnlocks ?? new UnlockManager(boundCatalog, null)).IsIngredientUnlocked,
                ResolveActionIcon, generatedMissingSprite, missingIconReplacement, true, out diagnostic);
            ApplyBoundPresentationVisibility();
            if (!success) Debug.LogWarning("Clue board content refresh failed: " + diagnostic);
        }

        internal void ReconcilePresentationVisibility()
        { if (hasContentBinding) ApplyBoundPresentationVisibility(); }

        private void ApplyBoundPresentationVisibility()
        {
            var set = GetComponent<ShortCyclePresentationSet>();
            var visible = set == null || !set.IsContentRendererHidden;
            var blocks = slotBlocks ?? new SlotBlock[0];
            foreach (var block in blocks)
                if (block != null) block.ApplyContentPresentationVisibility(visible);
            var names = slotNameBindings ?? new LocalizedText[0];
            for (var index = 0; index < names.Length; index++)
            {
                if (names[index] == null) continue;
                var label = names[index].GetComponent<TMP_Text>();
                if (label != null)
                    label.enabled = visible && index < blocks.Length && blocks[index] != null &&
                        blocks[index].Binding != null;
            }
            if (set != null) set.ReassertRendererHidden();
        }
        private void OnEnable()
        {
            isDisabled = false;
            if (changeSource == null && boundInventory != null)
            { AttachInventory(); OnInventoryChanged(); }
        }
        private void OnDisable() { isDisabled = true; DetachInventory(); }
        private void OnDestroy() { isDisabled = true; DetachInventory(); }

        private static CK01ItemData ResolveItem(CK01GeneratedDataCatalog catalog, string itemId)
        {
            CK01ItemData item; string ignored;
            return catalog.TryGetItem(itemId, out item, out ignored) ? item : null;
        }
        private static CK01TagData ResolveTag(CK01GeneratedDataCatalog catalog, string tagId)
        {
            CK01TagData tag; string ignored;
            return catalog.TryGetTag(tagId, out tag, out ignored) ? tag : null;
        }
    }
}
