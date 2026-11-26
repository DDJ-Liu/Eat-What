using System;
using System.Collections.Generic;

namespace EatWhat.Cooking.ShortCycle
{
    /// <summary>Read-only recipe and ingredient visibility query; no persistence or unlock operation.</summary>
    public sealed class UnlockManager
    {
        private readonly CK01GeneratedDataCatalog catalog;
        private readonly HashSet<string> recipes;
        // Null means the existing demo policy: every catalog ingredient is visible.
        private readonly HashSet<string> ingredients;

        public UnlockManager(CK01GeneratedDataCatalog source, IEnumerable<string> profileRecipeIds)
            : this(source, profileRecipeIds, null) { }

        public UnlockManager(CK01GeneratedDataCatalog source, IEnumerable<string> profileRecipeIds,
            IEnumerable<string> unlockedIngredientIds)
        {
            catalog = source;
            recipes = new HashSet<string>(profileRecipeIds ?? new string[0], StringComparer.Ordinal);
            ingredients = unlockedIngredientIds == null ? null :
                new HashSet<string>(unlockedIngredientIds, StringComparer.Ordinal);
        }

        public bool IsUnlocked(string recipeId)
        {
            CK01RecipeData recipe; string diagnostic;
            return catalog != null && catalog.TryGetRecipe(recipeId, out recipe, out diagnostic) &&
                (recipe.is_unlocked_default || recipes.Contains(recipeId));
        }

        public bool IsIngredientUnlocked(string itemId)
        {
            CK01ItemData item; string diagnostic;
            return !string.IsNullOrEmpty(itemId) && catalog != null &&
                catalog.TryGetItem(itemId, out item, out diagnostic) &&
                (ingredients == null || ingredients.Contains(itemId));
        }
    }
}