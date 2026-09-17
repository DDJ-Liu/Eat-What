using UnityEngine;

namespace EatWhat.Cooking.ShortCycle
{
    /// <summary>Explicit CK01 demo stub: every recipe and kitchen capability is unlocked.</summary>
    public sealed class PlayerUnlockManager : MonoBehaviour
    {
        public bool IsRecipeUnlocked(string recipeId) { return true; }
        public bool IsKitchenAreaUnlocked(string kitchenAreaId) { return true; }
    }
}
