namespace EatWhat.Cooking.ShortCycle
{
    public sealed class CatalogCard : ShortCycleGridPieceBase
    {
        public void ConfigureRecipe(
            CK01RecipeData recipe,
            CK01RecipeVariantData standardVariant,
            bool unlocked,
            bool favorite)
        {
            Configure(new ShortCycleGridPieceDisplay
            {
                icon_sprite = standardVariant == null ? null : standardVariant.display_sprite,
                name_key = recipe == null ? null : recipe.name_key,
                fallback_name = recipe == null ? string.Empty : recipe.name,
                badge_text = favorite ? "★" : string.Empty,
                is_placeholder = !unlocked
            });
        }
    }
}
