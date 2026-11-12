using System.Collections.Generic;
using EatWhat.Toolkit;
using UnityEngine;

namespace EatWhat.Cooking.ShortCycle
{
    public sealed class SlotBlock : ShortCycleGridPieceBase
    {
        [Header("Center group: standard icon + action icon + title TMP")]
        [SerializeField] private Transform centerGroup = null;
        [SerializeField] private SpriteRenderer actionIconRenderer = null;

        [Header("SubSlot_01..08 fixed manual anchors")]
        [SerializeField] private Transform[] subSlotAnchors = new Transform[ShortCycleAnchorContract.SubstituteAnchorCount];
        [SerializeField] private ShortCycleSubCard[] subCards = new ShortCycleSubCard[ShortCycleAnchorContract.SubstituteAnchorCount];

        public ShortCycleSlotBlockBinding Binding { get; private set; }
        public ShortCycleSubCardBinding GetSubstituteBinding(int anchorIndex)
        { return subCards != null && anchorIndex >= 0 && anchorIndex < subCards.Length && subCards[anchorIndex] != null
            ? subCards[anchorIndex].Binding : null; }

        public ToolkitPlaceholderState? StandardIconState { get; private set; }
        public ToolkitPlaceholderState? ActionIconState { get; private set; }

        public void ConfigureRequirement(CK01ItemData standardItem, int heldCount,
            int requiredCount, Sprite actionIcon)
        {
            ConfigureRequirement(standardItem, heldCount, requiredCount, actionIcon,
                standardItem == null ? null : standardItem.icon_sprite);
        }

        private void ConfigureRequirement(CK01ItemData standardItem, int heldCount,
            int requiredCount, Sprite actionIcon, Sprite standardIcon)
        {
            Configure(new ShortCycleGridPieceDisplay
            {
                icon_sprite = standardIcon,
                name_key = standardItem == null ? null : standardItem.name_key,
                fallback_name = standardItem == null ? string.Empty : standardItem.name,
                badge_text = heldCount + "/" + requiredCount,
                is_placeholder = false
            });
            SetSecondaryIcon(actionIconRenderer, actionIcon);
        }

        public bool ConfigureSlot(ShortCycleSlotBlockBinding binding, out string diagnostic)
        {
            var configured = ConfigureSlot(binding, null, null, out diagnostic);
            if (configured) diagnostic = null;
            return configured;
        }

        public bool ConfigureSlot(ShortCycleSlotBlockBinding binding,
            Sprite generatedMissingSprite, Sprite missingIconReplacement, out string diagnostic)
        {
            if (binding == null || binding.StandardItem == null)
            {
                diagnostic = "SlotBlock requires a resolved standard item.";
                return false;
            }
            if (!ValidateConfiguration(out diagnostic)) return false;
            var notices = new List<string>();
            var standardIcon = ResolveIcon(binding.StandardItem.icon_sprite, generatedMissingSprite,
                missingIconReplacement, "standard ingredient", out var standardState, notices);
            var actionIcon = ResolveIcon(binding.ActionIcon, generatedMissingSprite,
                missingIconReplacement, "standard action '" + binding.StandardActionId + "'",
                out var actionState, notices);
            Binding = binding;
            StandardIconState = standardState;
            ActionIconState = actionState;
            ConfigureRequirement(binding.StandardItem, binding.StandardHeldCount,
                binding.StandardRequiredCount, actionIcon, standardIcon);
            if (actionIconRenderer == null && !string.IsNullOrEmpty(binding.StandardActionId))
                notices.Add("action icon Renderer is not connected");

            var byAnchor = new ShortCycleSubCardBinding[ShortCycleAnchorContract.SubstituteAnchorCount];
            foreach (var substitute in binding.Substitutes ?? new ShortCycleSubCardBinding[0])
                if (substitute != null && substitute.AnchorIndex >= 0 && substitute.AnchorIndex < byAnchor.Length)
                    byAnchor[substitute.AnchorIndex] = substitute;
            for (var index = 0; index < subCards.Length; index++)
            {
                if (subCards[index] == null)
                {
                    diagnostic = "SubCard reference " + ShortCycleAnchorContract.GetSubstituteAnchorName(index) + " is not connected.";
                    return false;
                }
                subCards[index].Configure(byAnchor[index], generatedMissingSprite, missingIconReplacement);
                if (!string.IsNullOrEmpty(subCards[index].IconConfigurationDiagnostic))
                    notices.Add(ShortCycleAnchorContract.GetSubstituteAnchorName(index) + ": " +
                        subCards[index].IconConfigurationDiagnostic);
            }
            diagnostic = notices.Count == 0 ? null : string.Join("; ", notices.ToArray());
            return true;
        }

        private static Sprite ResolveIcon(Sprite source, Sprite generatedMissingSprite,
            Sprite replacement, string label, out ToolkitPlaceholderState? state, List<string> notices)
        {
            var missing = source == null || generatedMissingSprite != null && source == generatedMissingSprite;
            if (missing)
            {
                state = PlaceholderRule.Classify(false, source == null ? null : source.name);
                notices.Add(label + " source Sprite is missing; replacement requested");
                if (replacement != null && (generatedMissingSprite == null || replacement != generatedMissingSprite))
                    return replacement;
                notices.Add(label + " has no distinct missing replacement Sprite");
                return null;
            }
            var classified = PlaceholderRule.Classify(true, source.name);
            state = generatedMissingSprite == null && classified == ToolkitPlaceholderState.Ready
                ? (ToolkitPlaceholderState?)null : classified;
            if (!state.HasValue) notices.Add("CK01-MissingSprite identity is not bound; " + label + " art is unverified");
            return source;
        }

        public void ApplyContentPresentationVisibility(bool presentationVisible)
        {
            var visible = presentationVisible && Binding != null;
            SetContentVisible(visible);
            if (actionIconRenderer != null)
                actionIconRenderer.enabled = visible && actionIconRenderer.sprite != null;
            foreach (var subCard in subCards ?? new ShortCycleSubCard[0])
                if (subCard != null) subCard.ApplyContentPresentationVisibility(visible);
        }

        /// <summary>Clears bound content without changing this presentation object's active state.</summary>
        public void ClearSlot()
        {
            Binding = null;
            StandardIconState = null;
            ActionIconState = null;
            Configure(new ShortCycleGridPieceDisplay());
            SetSecondaryIcon(actionIconRenderer, null);
            foreach (var subCard in subCards ?? new ShortCycleSubCard[0])
                if (subCard != null) subCard.Clear();
            ApplyContentPresentationVisibility(false);
        }

        public bool ValidateConfiguration(out string diagnostic)
        {
            if (centerGroup == null)
            {
                diagnostic = "SlotBlock Center group is not connected.";
                return false;
            }
            if (!ShortCycleTrayAnchorLayout.ValidateNamedAnchors(
                subSlotAnchors,
                ShortCycleAnchorContract.SubstituteAnchorNames,
                out diagnostic))
            {
                return false;
            }
            if (subCards == null || subCards.Length != ShortCycleAnchorContract.SubstituteAnchorCount)
            {
                diagnostic = "SlotBlock requires exactly eight serialized SubCard references.";
                return false;
            }
            for (var index = 0; index < subCards.Length; index++)
            {
                if (subCards[index] != null) continue;
                diagnostic = "SubCard reference " + ShortCycleAnchorContract.GetSubstituteAnchorName(index) + " is not connected.";
                return false;
            }
            diagnostic = null;
            return true;
        }
    }
}
