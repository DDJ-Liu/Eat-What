using System;
using System.Collections.Generic;
using EatWhat.Tools.Localization;
using EatWhat.Toolkit;
using TMPro;
using UnityEngine;

namespace EatWhat.Cooking.ShortCycle
{
    public sealed class StepDetailRowPiece : MonoBehaviour
    {
        [SerializeField] private TextMeshPro description = null;
        [SerializeField] private SpriteRenderer[] ingredientIcons = new SpriteRenderer[0];
        [SerializeField] private SpriteRenderer[] toolIcons = new SpriteRenderer[0];
        [SerializeField] private Sprite generatedMissingSprite = null;
        [SerializeField] private Sprite missingIconReplacement = null;
        private const float IconMaxSideUnits = 0.6f;

        private ToolkitPlaceholderState?[] ingredientStates = new ToolkitPlaceholderState?[0];
        private ToolkitPlaceholderState?[] toolStates = new ToolkitPlaceholderState?[0];

        public RecipeStepView View { get; private set; }
        public string DisplayedText { get { return description == null ? null : description.text; } }
        public IReadOnlyList<ToolkitPlaceholderState?> IngredientIconStates { get { return Array.AsReadOnly(ingredientStates); } }
        public IReadOnlyList<ToolkitPlaceholderState?> ToolIconStates { get { return Array.AsReadOnly(toolStates); } }
        public string IconConfigurationDiagnostic { get; private set; }
        public bool HasMissingSpriteBindings
        {
            get { return generatedMissingSprite != null && missingIconReplacement != null &&
                missingIconReplacement != generatedMissingSprite; }
        }

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
            IconConfigurationDiagnostic = null;
            ingredientStates = new ToolkitPlaceholderState?[ingredientIcons.Length];
            toolStates = new ToolkitPlaceholderState?[toolIcons.Length];
            if (description != null) LocalizedText.ConfigureContent(description, view == null ? string.Empty : view.display_text_key, view == null ? string.Empty : LocService.Get(view.display_text_key));
            var surface = GetComponent<SpriteRenderer>(); if (surface != null) surface.enabled = view != null;
            Fill(ingredientIcons, view == null ? null : view.standard_ingredients, ingredientStates, "ingredient");
            Fill(toolIcons, view == null ? null : view.standard_tools, toolStates, "tool");
        }

        private void Fill(SpriteRenderer[] targets, IReadOnlyList<RecipeIconView> icons,
            ToolkitPlaceholderState?[] states, string group)
        {
            for (var i = 0; i < targets.Length; i++)
            {
                var target = targets[i];
                if (target == null) continue;
                var value = icons != null && i < icons.Count ? icons[i] : null;
                states[i] = null;
                if (value == null)
                {
                    target.sprite = null;
                    target.enabled = false;
                    continue;
                }

                var sprite = value.icon_sprite;
                var knownMissing = sprite == null || generatedMissingSprite != null && sprite == generatedMissingSprite;
                if (knownMissing)
                {
                    states[i] = PlaceholderRule.Classify(false, sprite == null ? null : sprite.name);
                    if (missingIconReplacement == null ||
                        generatedMissingSprite != null && missingIconReplacement == generatedMissingSprite)
                    {
                        target.sprite = null;
                        target.enabled = false;
                        SetDiagnostic(group, i, "Missing icon has no distinct non-missing replacement Sprite.");
                    }
                    else
                    {
                        ShowIcon(target, missingIconReplacement, group, i);
                    }
                    continue;
                }

                var classified = PlaceholderRule.Classify(true, sprite.name);
                if (generatedMissingSprite == null)
                {
                    // Without the exact generated subasset binding, an ordinary non-null sprite
                    // cannot be certified Ready. Preserve the old display until ENGINE connects it.
                    states[i] = classified == ToolkitPlaceholderState.Aigc ? classified : (ToolkitPlaceholderState?)null;
                    SetDiagnostic(group, i, "CK01-MissingSprite identity is not bound; non-AIGC art is unverified.");
                }
                else
                {
                    states[i] = classified;
                }
                ShowIcon(target, sprite, group, i);
            }
        }

        private void ShowIcon(SpriteRenderer target, Sprite sprite, string group, int index)
        {
            var rect = sprite.rect;
            var pixelsPerUnit = sprite.pixelsPerUnit;
            if (!FinitePositive(IconMaxSideUnits) || !FinitePositive(rect.width) ||
                !FinitePositive(rect.height) || !FinitePositive(pixelsPerUnit))
            {
                target.sprite = null;
                target.enabled = false;
                SetDiagnostic(group, index, "Sprite rectangle, pixels per unit, or icon maximum side is invalid.");
                return;
            }

            var spriteMaxSideUnits = Mathf.Max(rect.width, rect.height) / pixelsPerUnit;
            var scale = IconMaxSideUnits / spriteMaxSideUnits;
            if (!FinitePositive(spriteMaxSideUnits) || !FinitePositive(scale))
            {
                target.sprite = null;
                target.enabled = false;
                SetDiagnostic(group, index, "Sprite size cannot produce a finite icon scale.");
                return;
            }

            var previousScale = target.transform.localScale;
            target.transform.localScale = new Vector3(scale, scale, previousScale.z);
            target.sprite = sprite;
            target.enabled = true;
        }

        private static bool FinitePositive(float value)
        {
            return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private void SetDiagnostic(string group, int index, string reason)
        {
            if (IconConfigurationDiagnostic == null)
                IconConfigurationDiagnostic = group + " icon[" + index + "]: " + reason;
        }
    }
}