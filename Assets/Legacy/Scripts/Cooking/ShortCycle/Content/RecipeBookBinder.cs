using EatWhat.Tools.Localization;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
namespace EatWhat.Cooking.ShortCycle
{
    /// <summary>Content only. Existing Grid/HorizontalLayout anchors retain their authored transforms.</summary>
    public sealed class RecipeBookBinder : MonoBehaviour
    {
        [SerializeField] private CatalogCard[] catalogCards = new CatalogCard[8];
        [SerializeField] private TextMeshPro browseTitle = null;
        [SerializeField] private SpriteRenderer browseDish = null;
        [SerializeField] private IngredientListPiece ingredientList = null;
        [SerializeField] private StickerNotesPiece stickerNotes = null;
        [SerializeField] private CurrentRecipeTabPiece currentRecipeTab = null;
        [SerializeField] private StepDetailModalPiece stepDetail = null;
        public StepDetailModalPiece StepDetails { get { return stepDetail; } }
        public bool CurrentFavoriteVisible { get { return currentRecipeTab != null && currentRecipeTab.FavoriteVisible; } }
        public void RefreshFavorites(System.Func<string, bool> isFavorite)
        {
            foreach (var card in catalogCards) if (card != null)
                card.SetFavorite(card.View != null && isFavorite != null && isFavorite(card.View.recipe_id));
            if (currentRecipeTab != null) currentRecipeTab.SetFavorite(CurrentRecipe != null && isFavorite != null && isFavorite(CurrentRecipe.recipe_id));
        }
        public IReadOnlyList<CatalogCard> CatalogCards { get { return System.Array.AsReadOnly(catalogCards); } }
        public int CatalogRecipeCount { get; private set; }
        public int VisibleCatalogCardCount { get; private set; }
        public bool PaginationRequired { get { return CatalogRecipeCount > 8; } }
        public RecipeView CurrentRecipe { get; private set; }
        public string DisplayedTitle { get { return browseTitle == null ? null : browseTitle.text; } }
        public bool ValidateConfiguration(int count, out string diagnostic)
        {
            if (catalogCards.Length != 8 || browseTitle == null || browseDish == null ||
                ingredientList == null || stickerNotes == null || currentRecipeTab == null)
            { diagnostic = "RecipeBookBinder requires eight catalog anchors and all browse content pieces."; return false; }
            foreach (var card in catalogCards)
                if (card == null) { diagnostic = "A catalog card anchor is not connected."; return false; }
            diagnostic = null; return true;
        }
        public void ConfigureSelection(ShortCycleActionRouter router)
        { foreach (var card in catalogCards) if (card != null) card.ConfigureSelection(router); }
        public void BindCatalog(IReadOnlyList<RecipeView> recipes)
        {
            CatalogRecipeCount = recipes == null ? 0 : recipes.Count;
            VisibleCatalogCardCount = System.Math.Min(8, System.Math.Min(catalogCards.Length, CatalogRecipeCount));
            for (var index = 0; index < catalogCards.Length; index++)
                if (catalogCards[index] != null) catalogCards[index].Apply(index < VisibleCatalogCardCount ? recipes[index] : null);
            if (PaginationRequired) Debug.LogWarning("[D-T1a] Recipe catalog exceeds eight cards; pagination remains deferred.", this);
        }
        public bool CanBindBrowse(RecipeView view, out string diagnostic)
        {
            if (view != null && (ingredientList == null || !ingredientList.CanBind(view.slots.Count) || stickerNotes == null || !stickerNotes.CanBind(view.note_tags.Count) || currentRecipeTab == null || !currentRecipeTab.IsConfigured))
            { diagnostic = "Recipe ingredient anchors do not cover all ordered slots."; return false; }
            if (view != null && (stepDetail == null || !stepDetail.CanBind(view, out diagnostic)))
            { diagnostic = "Step detail content is not connected or exceeds its row/icon capacity."; return false; }
            diagnostic = null; return true;
        }
        public void BindBrowse(RecipeView view)
        {
            CurrentRecipe = view;
            if (browseTitle != null) LocalizedText.ConfigureContent(browseTitle, view == null ? string.Empty : view.name_key, view == null ? string.Empty : LocService.Get(view.name_key));
            if (browseDish != null) browseDish.sprite = view == null ? null : view.display_sprite;
            if (ingredientList != null) ingredientList.Apply(view);
            if (stickerNotes != null) stickerNotes.Apply(view);
            if (currentRecipeTab != null) currentRecipeTab.Apply(view);
            if (stepDetail != null) stepDetail.Bind(view);
        }
        public void Clear() { BindCatalog(null); BindBrowse(null); }
    }
}
