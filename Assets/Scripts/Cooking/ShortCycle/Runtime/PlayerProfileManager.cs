using System;
using System.Collections.Generic;
namespace EatWhat.Cooking.ShortCycle
{
    /// <summary>Detached immutable values for a future save adapter; this is not a disk format.</summary>
    public sealed class PlayerProfileSnapshot
    {
        public IReadOnlyList<string> FavoriteRecipeIds { get; }
        public IReadOnlyList<string> UnlockedRecipeIds { get; }
        public PlayerProfileSnapshot(IEnumerable<string> favorites, IEnumerable<string> unlocked)
        { FavoriteRecipeIds = CopyIds(favorites).AsReadOnly(); UnlockedRecipeIds = CopyIds(unlocked).AsReadOnly(); }
        internal static List<string> CopyIds(IEnumerable<string> source)
        {
            var values = new List<string>();
            foreach (var id in source ?? new string[0])
                if (!string.IsNullOrWhiteSpace(id) && !values.Contains(id)) values.Add(id);
            return values;
        }
    }
    /// <summary>Capabilities over the existing single PlayerProfile owner; no decisions, subscriptions or persistence.</summary>
    public sealed class PlayerProfileManager
    {
        private readonly PlayerProfile profile;
        public PlayerProfileManager(PlayerProfile owner)
        { if (owner == null) throw new ArgumentNullException("owner"); profile = owner; }
        public bool IsFavorite(string recipeId) { return profile.IsFavorite(recipeId); }
        public bool ToggleFavorite(string recipeId)
        {
            if (string.IsNullOrWhiteSpace(recipeId)) throw new ArgumentException("Recipe ID is required.", "recipeId");
            if (profile.IsFavorite(recipeId)) profile.RemoveFavorite(recipeId); else profile.AddFavorite(recipeId);
            return profile.IsFavorite(recipeId);
        }
        public PlayerProfileSnapshot GetSnapshot() { return profile.GetSnapshot(); }
        public void ApplySnapshot(PlayerProfileSnapshot snapshot) { profile.ApplySnapshot(snapshot); }
    }
}
