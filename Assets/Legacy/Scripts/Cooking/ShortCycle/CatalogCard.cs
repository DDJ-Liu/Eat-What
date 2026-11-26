using UnityEngine;
namespace EatWhat.Cooking.ShortCycle
{
    public sealed class CatalogCard : ShortCycleGridPieceBase
    {
        [SerializeField] private Sprite lockedRecipeSprite = null;
        [SerializeField] private SpriteRenderer[] tagIcons = new SpriteRenderer[2];
        [SerializeField] private EatWhat.Tools.Localization.LocalizedText[] tagLabels = new EatWhat.Tools.Localization.LocalizedText[2];
        [SerializeField] private ShortCycleMouseActionBinding selectionBinding = null;
        [SerializeField] private SpriteRenderer favoriteStar = null;
        [SerializeField] private SpriteRenderer[] contentDecorations = new SpriteRenderer[0];
        private bool[] decorationEnabledBeforeHide;
        private bool decorationsHidden;
        public bool IsFavorite { get; private set; }
        public bool FavoriteVisible { get { return favoriteStar != null ? favoriteStar.enabled && favoriteStar.sprite != null : CurrentBadgeText == "★"; } }
        public void SetFavorite(bool favorite)
        {
            IsFavorite = favorite && View != null;
            if (Display != null) Display.badge_text = IsFavorite ? "★" : string.Empty;
            SetBadge(IsFavorite ? "★" : string.Empty);
            if (favoriteStar != null) favoriteStar.enabled = IsFavorite;
        }
        private ShortCycleActionRouter router;
        private bool unlocked = true;
        public RecipeView View { get; private set; }
        protected override bool ContentAllowsInteraction { get { return unlocked; } }
        public void ConfigureSelection(ShortCycleActionRouter target) { router = target; RefreshSelection(); }
        public void Apply(RecipeView view)
        {
            View = view; unlocked = view != null && view.is_unlocked;
            Configure(new ShortCycleGridPieceDisplay { icon_sprite = view == null ? null :
                (unlocked ? view.display_sprite : (HasPlaceholderRenderer ? null : lockedRecipeSprite)), name_key = view == null ? null : view.name_key,
                is_placeholder = view != null && !unlocked && HasPlaceholderRenderer });
            for (var index = 0; index < tagIcons.Length; index++)
                if (tagIcons[index] != null)
                { var tag = view != null && index < view.note_tags.Count ? view.note_tags[index] : null;
                  tagIcons[index].sprite = tag == null ? null : tag.icon_sprite;
                  tagIcons[index].enabled = tag != null; }
            for (var index = 0; index < tagLabels.Length; index++)
                if (tagLabels[index] != null) tagLabels[index].Configure(
                    view != null && index < view.note_tags.Count ? view.note_tags[index].name_key : null, null);
            SetContentVisible(view != null); SetDecorationsVisible(view != null); RefreshSelection();
            if (view == null) SetFavorite(false);
        }
        private void SetDecorationsVisible(bool visible)
        {
            var decorations = contentDecorations ?? new SpriteRenderer[0];
            if (!visible)
            {
                if (!decorationsHidden)
                {
                    decorationEnabledBeforeHide = new bool[decorations.Length];
                    for (var index = 0; index < decorations.Length; index++)
                        decorationEnabledBeforeHide[index] = decorations[index] != null && decorations[index].enabled;
                    decorationsHidden = true;
                }
                for (var index = 0; index < decorations.Length; index++)
                    if (decorations[index] != null) decorations[index].enabled = false;
            }
            else if (decorationsHidden)
            {
                for (var index = 0; index < decorations.Length; index++)
                    if (decorations[index] != null)
                        decorations[index].enabled = index < decorationEnabledBeforeHide.Length && decorationEnabledBeforeHide[index];
                decorationsHidden = false;
            }
        }
        private void RefreshSelection()
        {
            if (selectionBinding != null)
                selectionBinding.Configure(router, ShortCycleActionNames.SelectRecipe, View == null ? null : View.recipe_id);
        }
        public ShortCycleActionResult Select()
        {
            if (View == null || !View.is_unlocked)
                return ShortCycleActionResult.Failure("Catalog recipe is empty or locked.");
            return selectionBinding != null ? selectionBinding.Dispatch() : router == null
                ? ShortCycleActionResult.Failure("Catalog selection router is not connected.")
                : router.DispatchAction(new ShortCycleActionRequest { ActionName = ShortCycleActionNames.SelectRecipe, recipe_id = View.recipe_id });
        }
        // Wire each existing MouseInteract Button.selectEvent to this, once, in D-T2.
        public void SelectFromMouseEvent() { Select(); }
        public void ConfigureRecipe(CK01RecipeData recipe, CK01RecipeVariantData standardVariant, bool isUnlocked, bool favorite)
        {
            if (recipe == null) { Apply(null); return; }
            unlocked = isUnlocked;
            Configure(new ShortCycleGridPieceDisplay { icon_sprite = standardVariant == null ? null : (isUnlocked ? standardVariant.display_sprite : (HasPlaceholderRenderer ? null : lockedRecipeSprite)),
                name_key = recipe == null ? null : recipe.name_key, fallback_name = recipe == null ? string.Empty : recipe.name,
                badge_text = favorite ? "★" : string.Empty,
                is_placeholder = !isUnlocked && HasPlaceholderRenderer });
            SetContentVisible(true); SetDecorationsVisible(true);
        }
    }
}
