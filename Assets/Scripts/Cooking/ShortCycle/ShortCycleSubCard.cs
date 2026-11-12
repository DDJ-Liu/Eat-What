using System;
using System.Collections.Generic;
using EatWhat.Toolkit;
using TMPro;
using UnityEngine;

namespace EatWhat.Cooking.ShortCycle
{
    public sealed class ShortCycleSubstituteRequirement
    {
        public ShortCycleSubstituteRequirement(int anchorIndex, string itemId, int requiredCount)
        {
            AnchorIndex = anchorIndex;
            ItemId = itemId;
            RequiredCount = requiredCount;
        }

        public int AnchorIndex { get; private set; }
        public string ItemId { get; private set; }
        public int RequiredCount { get; private set; }
    }

    public sealed class ShortCycleSubCardBinding
    {
        public int AnchorIndex;
        public string ItemId;
        public int HeldCount;
        public int RequiredCount;
        public bool IsUnlocked;
        public string PrimaryCategoryTagId;
        public Sprite ItemIcon;
        public Sprite PrimaryCategoryTagIcon;

        public bool IsSatisfied
        {
            get { return HeldCount >= RequiredCount; }
        }

        public Sprite DisplayIcon
        {
            get { return IsUnlocked ? ItemIcon : PrimaryCategoryTagIcon; }
        }
    }

    public sealed class ShortCycleSlotBlockBinding
    {
        public string StandardItemId;
        public CK01ItemData StandardItem;
        public int StandardHeldCount;
        public int StandardRequiredCount;
        public string StandardActionId;
        public Sprite ActionIcon;
        public IList<ShortCycleSubCardBinding> Substitutes = new List<ShortCycleSubCardBinding>().AsReadOnly();
    }

    /// <summary>
    /// T2 v2.3 consumer. Field order is intentionally explicit rather than
    /// reflection-based so a schema rename cannot silently reorder the UI.
    /// </summary>
    public static class ShortCycleRecipeSlotBinding
    {
        public static IList<ShortCycleSubstituteRequirement> ReadRequirements(CK01RecipeSlotData slot)
        {
            var result = new List<ShortCycleSubstituteRequirement>(ShortCycleAnchorContract.SubstituteAnchorCount);
            if (slot == null)
            {
                return result.AsReadOnly();
            }

            Add(result, 0, slot.sub_01, slot.sub_01_count);
            Add(result, 1, slot.sub_02, slot.sub_02_count);
            Add(result, 2, slot.sub_03, slot.sub_03_count);
            Add(result, 3, slot.sub_04, slot.sub_04_count);
            Add(result, 4, slot.sub_05, slot.sub_05_count);
            Add(result, 5, slot.sub_06, slot.sub_06_count);
            Add(result, 6, slot.sub_07, slot.sub_07_count);
            Add(result, 7, slot.sub_08, slot.sub_08_count);
            return result.AsReadOnly();
        }

        public static bool TryResolve(
            CK01RecipeSlotData slot,
            Func<string, CK01ItemData> itemResolver,
            Func<string, CK01TagData> tagResolver,
            Func<string, int> heldCountResolver,
            Func<string, bool> unlockResolver,
            Sprite actionIcon,
            out ShortCycleSlotBlockBinding binding,
            out string diagnostic)
        {
            return TryResolveCore(slot == null ? null : slot.standard_ingredient, slot == null ? 0 : slot.required_count, ReadRequirements(slot), itemResolver, tagResolver,
                heldCountResolver, unlockResolver, actionIcon, slot == null ? null : slot.standard_action, out binding, out diagnostic);
        }

        public static bool TryResolve(
            RecipeSlotView slot,
            Func<string, CK01ItemData> itemResolver,
            Func<string, CK01TagData> tagResolver,
            Func<string, int> heldCountResolver,
            Func<string, bool> unlockResolver,
            Sprite actionIcon,
            out ShortCycleSlotBlockBinding binding,
            out string diagnostic)
        {
            var requirements = new List<ShortCycleSubstituteRequirement>();
            if (slot != null) foreach (var sub in slot.substitutes)
                requirements.Add(new ShortCycleSubstituteRequirement(sub.anchor_index, sub.item_id, sub.required_count));
            return TryResolveCore(slot == null ? null : slot.standard_ingredient.item_id, slot == null ? 0 : slot.standard_ingredient.required_count, requirements, itemResolver, tagResolver,
                heldCountResolver, unlockResolver, actionIcon, slot == null ? null : slot.standard_action, out binding, out diagnostic);
        }

        private static bool TryResolveCore(
            string standardId, int requiredCount, IList<ShortCycleSubstituteRequirement> requirements,
            Func<string, CK01ItemData> itemResolver,
            Func<string, CK01TagData> tagResolver,
            Func<string, int> heldCountResolver,
            Func<string, bool> unlockResolver,
            Sprite actionIcon, string standardActionId,
            out ShortCycleSlotBlockBinding binding,
            out string diagnostic)
        {
            binding = null;
            if (string.IsNullOrEmpty(standardId) || itemResolver == null || tagResolver == null ||
                heldCountResolver == null || unlockResolver == null)
            {
                diagnostic = "Slot binding requires T2 data and all explicit resolvers.";
                return false;
            }

            var standard = itemResolver(standardId);
            if (standard == null)
            {
                diagnostic = "T2 standard ingredient '" + standardId + "' could not be resolved.";
                return false;
            }

            var cards = new List<ShortCycleSubCardBinding>();
            foreach (var requirement in requirements)
            {
                var item = itemResolver(requirement.ItemId);
                if (item == null)
                {
                    diagnostic = "T2 substitute '" + requirement.ItemId + "' could not be resolved.";
                    return false;
                }

                var primaryTagId = GetPrimaryCategoryTagId(item);
                var primaryTag = string.IsNullOrEmpty(primaryTagId) ? null : tagResolver(primaryTagId);
                cards.Add(new ShortCycleSubCardBinding
                {
                    AnchorIndex = requirement.AnchorIndex,
                    ItemId = requirement.ItemId,
                    HeldCount = Math.Max(0, heldCountResolver(requirement.ItemId)),
                    RequiredCount = requirement.RequiredCount,
                    IsUnlocked = unlockResolver(requirement.ItemId),
                    PrimaryCategoryTagId = primaryTagId,
                    ItemIcon = item.icon_sprite,
                    PrimaryCategoryTagIcon = primaryTag == null ? null : primaryTag.icon_sprite
                });
            }

            binding = new ShortCycleSlotBlockBinding
            {
                StandardItemId = standardId,
                StandardItem = standard,
                StandardHeldCount = Math.Max(0, heldCountResolver(standardId)),
                StandardRequiredCount = Math.Max(1, requiredCount),
                StandardActionId = standardActionId,
                ActionIcon = actionIcon,
                Substitutes = cards.AsReadOnly()
            };
            diagnostic = null;
            return true;
        }

        public static string GetPrimaryCategoryTagId(CK01ItemData item)
        {
            if (item == null || item.ingredient_categories == null || item.ingredient_categories.Count == 0)
            {
                return null;
            }
            return item.ingredient_categories[0];
        }

        private static void Add(List<ShortCycleSubstituteRequirement> target, int index, string itemId, int requiredCount)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return;
            }
            target.Add(new ShortCycleSubstituteRequirement(index, itemId, Math.Max(1, requiredCount)));
        }
    }

    [DisallowMultipleComponent]
    public sealed class ShortCycleSubCard : MonoBehaviour
    {
        public static readonly Color SatisfiedLinkColor = new Color(80f / 255f, 173f / 255f, 92f / 255f, 1f);
        public static readonly Color UnsatisfiedLinkColor = new Color(205f / 255f, 67f / 255f, 62f / 255f, 1f);

        [Header("SubCard world-space structure")]
        [SerializeField] private GameObject cardRoot = null;
        [SerializeField] private SpriteRenderer iconRenderer = null;
        [SerializeField] private TextMeshPro countLabel = null;
        [SerializeField] private GameObject node = null;
        [SerializeField] private SpriteRenderer link = null;

        [Header("Locked silhouette material contract")]
        [SerializeField] private Material unlockedMaterial = null;
        [SerializeField] private Material lockedSilhouetteMaterial = null;

        public ShortCycleSubCardBinding Binding { get; private set; }
        public ToolkitPlaceholderState? IconState { get; private set; }
        public string IconConfigurationDiagnostic { get; private set; }

        public void Configure(ShortCycleSubCardBinding binding)
        { Configure(binding, null, null); }

        public void Configure(ShortCycleSubCardBinding binding, Sprite generatedMissingSprite,
            Sprite missingIconReplacement)
        {
            Binding = binding;
            IconState = null;
            IconConfigurationDiagnostic = null;
            var visible = binding != null;
            if (cardRoot != null) cardRoot.SetActive(visible);
            if (node != null) node.SetActive(visible);
            if (!visible)
            {
                if (iconRenderer != null)
                {
                    iconRenderer.sprite = null;
                    iconRenderer.enabled = false;
                    iconRenderer.sharedMaterial = unlockedMaterial;
                }
                if (countLabel != null) countLabel.text = string.Empty;
                if (link != null) link.enabled = false;
                return;
            }

            var sprite = binding.DisplayIcon;
            var knownMissing = sprite == null ||
                generatedMissingSprite != null && sprite == generatedMissingSprite;
            if (knownMissing)
            {
                IconState = PlaceholderRule.Classify(false, sprite == null ? null : sprite.name);
                if (missingIconReplacement == null ||
                    generatedMissingSprite != null && missingIconReplacement == generatedMissingSprite)
                {
                    sprite = null;
                    IconConfigurationDiagnostic = "SubCard icon has no distinct missing replacement Sprite.";
                }
                else sprite = missingIconReplacement;
            }
            else
            {
                var state = PlaceholderRule.Classify(true, sprite.name);
                IconState = generatedMissingSprite == null && state == ToolkitPlaceholderState.Ready
                    ? (ToolkitPlaceholderState?)null : state;
                if (!IconState.HasValue)
                    IconConfigurationDiagnostic = "CK01-MissingSprite identity is not bound; SubCard art is unverified.";
            }
            if (!binding.IsUnlocked && lockedSilhouetteMaterial == null)
                IconConfigurationDiagnostic = "Locked SubCard silhouette Material is not connected.";
            if (iconRenderer != null)
            {
                iconRenderer.sprite = sprite;
                iconRenderer.enabled = sprite != null;
                iconRenderer.sharedMaterial = binding.IsUnlocked ? unlockedMaterial : lockedSilhouetteMaterial;
            }
            else if (IconConfigurationDiagnostic == null)
                IconConfigurationDiagnostic = "SubCard icon Renderer is not connected.";
            if (countLabel != null) countLabel.text = binding.HeldCount + "/" + binding.RequiredCount;
            if (link != null)
            {
                link.enabled = true;
                link.color = binding.IsSatisfied ? SatisfiedLinkColor : UnsatisfiedLinkColor;
            }
        }

        public void ApplyContentPresentationVisibility(bool presentationVisible)
        {
            var visible = presentationVisible && Binding != null;
            if (iconRenderer != null) iconRenderer.enabled = visible && iconRenderer.sprite != null;
            if (countLabel != null) countLabel.enabled = visible;
            if (link != null) link.enabled = visible;
        }

        public void Clear()
        { Configure(null); ApplyContentPresentationVisibility(false); }
    }
}
