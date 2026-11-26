using EatWhat.Tools.Localization;
using TMPro;
using UnityEngine;
namespace EatWhat.Cooking.ShortCycle
{
    public sealed class CurrentRecipeTabPiece : MonoBehaviour
    {
        [SerializeField] private TextMeshPro title = null;
        [SerializeField] private SpriteRenderer favoriteStar = null;
        public bool IsFavorite { get; private set; }
        public bool FavoriteVisible { get { return favoriteStar != null && favoriteStar.enabled && favoriteStar.sprite != null; } }
        public bool IsConfigured { get { return title != null && favoriteStar != null && favoriteStar.sprite != null; } }
        public void SetFavorite(bool favorite)
        { IsFavorite = favorite; if (favoriteStar != null) favoriteStar.enabled = favorite; }
        public void Apply(RecipeView view)
        { if (title != null) LocalizedText.ConfigureContent(title, view == null ? string.Empty : view.name_key, view == null ? string.Empty : LocService.Get(view.name_key));
          if (view == null) SetFavorite(false); }
    }
}
