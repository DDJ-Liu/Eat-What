using UnityEngine;
namespace EatWhat.Cooking.ShortCycle
{
    /// <summary>Retains the existing scene component; recipe defaults now come from the catalog.</summary>
    public sealed class PlayerUnlockManager : MonoBehaviour
    {
        private UnlockManager recipes;
        public void Configure(CK01GeneratedDataCatalog catalog)
        { Configure(catalog, new string[0]); }
        public void Configure(CK01GeneratedDataCatalog catalog, System.Collections.Generic.IEnumerable<string> profileRecipeIds)
        { recipes = new UnlockManager(catalog, profileRecipeIds); }
        public bool IsRecipeUnlocked(string recipeId) { return recipes != null && recipes.IsUnlocked(recipeId); }
        public bool IsKitchenAreaUnlocked(string kitchenAreaId) { return true; }
    }
}
