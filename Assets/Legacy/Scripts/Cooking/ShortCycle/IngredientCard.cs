namespace EatWhat.Cooking.ShortCycle
{
    public sealed class IngredientCard : ShortCycleGridPieceBase
    {
        public void ConfigureIngredient(CK01ItemData item, int requiredCount)
        {
            Configure(new ShortCycleGridPieceDisplay
            {
                icon_sprite = item == null ? null : item.icon_sprite,
                name_key = item == null ? null : item.name_key,
                fallback_name = item == null ? string.Empty : item.name,
                badge_text = requiredCount > 0 ? "x" + requiredCount : string.Empty,
                is_placeholder = false
            });
        }
    }
}
