using System;
using System.Collections.Generic;
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
        public CK01ItemData StandardItem;
        public int StandardHeldCount;
        public int StandardRequiredCount;
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
            binding = null;
            if (slot == null || itemResolver == null || tagResolver == null ||
                heldCountResolver == null || unlockResolver == null)
            {
                diagnostic = "Slot binding requires T2 data and all explicit resolvers.";
                return false;
            }

            var standard = itemResolver(slot.standard_ingredient);
            if (standard == null)
            {
                diagnostic = "T2 standard ingredient '" + slot.standard_ingredient + "' could not be resolved.";
                return false;
            }

            var cards = new List<ShortCycleSubCardBinding>();
            foreach (var requirement in ReadRequirements(slot))
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
                StandardItem = standard,
                StandardHeldCount = Math.Max(0, heldCountResolver(slot.standard_ingredient)),
                StandardRequiredCount = Math.Max(1, slot.required_count),
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

        public void Configure(ShortCycleSubCardBinding binding)
        {
            Binding = binding;
            var visible = binding != null;
            if (cardRoot != null)
            {
                cardRoot.SetActive(visible);
            }
            if (node != null)
            {
                node.SetActive(visible);
            }
            if (!visible)
            {
                if (link != null) link.enabled = false;
                return;
            }

            if (iconRenderer != null)
            {
                iconRenderer.sprite = binding.DisplayIcon;
                iconRenderer.sharedMaterial = binding.IsUnlocked ? unlockedMaterial : lockedSilhouetteMaterial;
            }
            if (countLabel != null)
            {
                countLabel.text = binding.HeldCount + "/" + binding.RequiredCount;
            }
            if (link != null)
            {
                link.enabled = true;
                link.color = binding.IsSatisfied ? SatisfiedLinkColor : UnsatisfiedLinkColor;
            }
        }

        public void Clear()
        {
            Configure(null);
        }
    }
}
