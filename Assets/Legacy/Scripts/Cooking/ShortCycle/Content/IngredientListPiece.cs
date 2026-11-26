using UnityEngine;
namespace EatWhat.Cooking.ShortCycle
{
    public sealed class IngredientListPiece : MonoBehaviour
    {
        // Ordered existing HorizontalLayout anchors, including any anchors beyond four.
        [SerializeField] private IngredientCard[] entries = new IngredientCard[0];
        public int Capacity { get { return entries.Length; } }
        public bool CanBind(int count)
        {
            if (count > entries.Length) return false;
            for (var index = 0; index < count; index++) if (entries[index] == null) return false;
            return true;
        }
        public void Apply(RecipeView view)
        {
            for (var index = 0; index < entries.Length; index++)
            {
                if (entries[index] == null) continue;
                var slot = view != null && index < view.slots.Count ? view.slots[index] : null;
                entries[index].Configure(new ShortCycleGridPieceDisplay {
                    icon_sprite = slot == null ? null : slot.standard_ingredient.icon_sprite,
                    name_key = slot == null ? null : slot.standard_ingredient.name_key,
                    badge_text = slot == null ? string.Empty : "×" + slot.standard_ingredient.required_count });
                entries[index].SetContentVisible(slot != null);
            }
        }
    }
}
