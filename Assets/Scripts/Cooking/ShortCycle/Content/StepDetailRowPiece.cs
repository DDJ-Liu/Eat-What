using EatWhat.Tools.Localization;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
namespace EatWhat.Cooking.ShortCycle
{
    public sealed class StepDetailRowPiece : MonoBehaviour
    {
        [SerializeField] private TextMeshPro description = null;
        [SerializeField] private SpriteRenderer[] ingredientIcons = new SpriteRenderer[0];
        [SerializeField] private SpriteRenderer[] toolIcons = new SpriteRenderer[0];
        public RecipeStepView View { get; private set; }
        public string DisplayedText { get { return description == null ? null : description.text; } }
        public bool CanBind(RecipeStepView view)
        {
            if (description == null || ingredientIcons.Length < view.standard_ingredients.Count || toolIcons.Length < view.standard_tools.Count) return false;
            for (var i = 0; i < view.standard_ingredients.Count; i++) if (ingredientIcons[i] == null) return false;
            for (var i = 0; i < view.standard_tools.Count; i++) if (toolIcons[i] == null) return false;
            return true;
        }
        public void Apply(RecipeStepView view)
        {
            View = view;
            if (description != null) LocalizedText.ConfigureContent(description, view == null ? string.Empty : view.display_text_key, view == null ? string.Empty : LocService.Get(view.display_text_key));
            var surface = GetComponent<SpriteRenderer>(); if (surface != null) surface.enabled = view != null;
            Fill(ingredientIcons, view == null ? null : view.standard_ingredients);
            Fill(toolIcons, view == null ? null : view.standard_tools);
        }
        private static void Fill(SpriteRenderer[] targets, IReadOnlyList<RecipeIconView> icons)
        {
            for (var i = 0; i < targets.Length; i++) if (targets[i] != null)
            { var value = icons != null && i < icons.Count ? icons[i] : null;
              targets[i].sprite = value == null ? null : value.icon_sprite; targets[i].enabled = value != null; }
        }
    }
}
