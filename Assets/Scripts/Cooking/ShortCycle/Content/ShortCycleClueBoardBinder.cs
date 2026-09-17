using EatWhat.Tools.Localization;
using UnityEngine;

namespace EatWhat.Cooking.ShortCycle
{
    /// <summary>Catalog-backed clue binding; presentation activation remains owned by the FSM host.</summary>
    [DisallowMultipleComponent]
    public sealed class ShortCycleClueBoardBinder : MonoBehaviour
    {
        [SerializeField] private SlotBlock[] slotBlocks = new SlotBlock[0];
        [SerializeField] private LocalizedText[] slotNameBindings = new LocalizedText[0];

        public bool Bind(
            CK01GeneratedDataCatalog catalog,
            string recipeId,
            IShortCycleInventoryQuery inventory,
            out string diagnostic)
        {
            return TryBind(catalog, inventory, recipeId, slotBlocks, slotNameBindings, out diagnostic);
        }

        public static bool TryBind(
            CK01GeneratedDataCatalog catalog,
            IShortCycleInventoryQuery inventory,
            string recipeId,
            SlotBlock[] targets,
            LocalizedText[] localizedNames,
            out string diagnostic)
        {
            if (string.IsNullOrEmpty(recipeId))
            {
                var emptyBlocks = targets ?? new SlotBlock[0];
                for (var index = 0; index < emptyBlocks.Length; index++)
                    if (emptyBlocks[index] != null) emptyBlocks[index].ClearSlot();
                var emptyNames = localizedNames ?? new LocalizedText[0];
                for (var index = 0; index < emptyNames.Length; index++)
                    if (emptyNames[index] != null) emptyNames[index].Configure(null, null);
                diagnostic = null;
                return true;
            }
            if (catalog == null || inventory == null)
            {
                diagnostic = "Catalog and a read-only inventory query are required for clue binding.";
                return false;
            }
            CK01RecipeData recipe;
            if (!catalog.TryGetRecipe(recipeId, out recipe, out diagnostic)) return false;
            var blocks = targets ?? new SlotBlock[0];
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
                CK01RecipeSlotData slot;
                if (!catalog.TryGetRecipeSlot(recipe.slots[index], out slot, out diagnostic)) return false;
                ShortCycleSlotBlockBinding binding;
                if (!ShortCycleRecipeSlotBinding.TryResolve(
                    slot,
                    itemId => ResolveItem(catalog, itemId),
                    tagId => ResolveTag(catalog, tagId),
                    inventory.GetQuantity,
                    itemId => !string.IsNullOrEmpty(itemId),
                    null,
                    out binding,
                    out diagnostic)) return false;
                if (!blocks[index].ValidateConfiguration(out diagnostic)) return false;
                bindings[index] = binding;
            }

            var names = localizedNames ?? new LocalizedText[0];
            for (var index = 0; index < bindings.Length; index++)
            {
                var binding = bindings[index];
                if (!blocks[index].ConfigureSlot(binding, out diagnostic)) return false;
                if (index < names.Length && names[index] != null)
                    names[index].Configure(binding.StandardItem.name_key, binding.StandardItem.name);
            }
            diagnostic = null;
            return true;
        }

        private static CK01ItemData ResolveItem(CK01GeneratedDataCatalog catalog, string itemId)
        {
            CK01ItemData item;
            string ignored;
            return catalog.TryGetItem(itemId, out item, out ignored) ? item : null;
        }

        private static CK01TagData ResolveTag(CK01GeneratedDataCatalog catalog, string tagId)
        {
            CK01TagData tag;
            string ignored;
            return catalog.TryGetTag(tagId, out tag, out ignored) ? tag : null;
        }
    }
}
