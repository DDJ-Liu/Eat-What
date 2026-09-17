using System.Collections.Generic;
using UnityEngine;

namespace EatWhat.Cooking.ShortCycle
{
    /// <summary>Runtime profile shell. Persistence is intentionally deferred past CK01-C.</summary>
    public sealed class PlayerProfile : MonoBehaviour
    {
        [SerializeField] private List<string> favorite_recipe_ids = new List<string>();
        [SerializeField] private List<string> unlocked_recipe_ids = new List<string>();

        public IList<string> FavoriteRecipeIds { get { return favorite_recipe_ids.AsReadOnly(); } }

        public bool AddFavorite(string recipeId)
        {
            if (string.IsNullOrEmpty(recipeId) || favorite_recipe_ids.Contains(recipeId)) return false;
            favorite_recipe_ids.Add(recipeId);
            return true;
        }

        public bool RemoveFavorite(string recipeId)
        {
            return !string.IsNullOrEmpty(recipeId) && favorite_recipe_ids.Remove(recipeId);
        }

        public bool IsFavorite(string recipeId)
        {
            return !string.IsNullOrEmpty(recipeId) && favorite_recipe_ids.Contains(recipeId);
        }
    }
}
