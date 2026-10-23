using System;
using System.Collections.Generic;
namespace EatWhat.Cooking.ShortCycle
{
    /// <summary>Read-only unlock query. T1a injects an empty profile set; no persistence or unlock operation.</summary>
    public sealed class UnlockManager
    {
        private readonly CK01GeneratedDataCatalog catalog;
        private readonly HashSet<string> profile;
        public UnlockManager(CK01GeneratedDataCatalog source, IEnumerable<string> profileRecipeIds)
        { catalog = source; profile = new HashSet<string>(profileRecipeIds ?? new string[0], StringComparer.Ordinal); }
        public bool IsUnlocked(string recipeId)
        {
            CK01RecipeData recipe; string diagnostic;
            return catalog != null && catalog.TryGetRecipe(recipeId, out recipe, out diagnostic) &&
                (recipe.is_unlocked_default || profile.Contains(recipeId));
        }
    }
}
