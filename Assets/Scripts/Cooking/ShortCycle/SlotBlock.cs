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

        public void ConfigureRequirement(
            CK01ItemData standardItem,
            int heldCount,
            int requiredCount,
            Sprite actionIcon)
        {
            Configure(new ShortCycleGridPieceDisplay
            {
                icon_sprite = standardItem == null ? null : standardItem.icon_sprite,
                name_key = standardItem == null ? null : standardItem.name_key,
                fallback_name = standardItem == null ? string.Empty : standardItem.name,
                badge_text = heldCount + "/" + requiredCount,
                is_placeholder = false
            });
            SetSecondaryIcon(actionIconRenderer, actionIcon);
        }

        public bool ConfigureSlot(ShortCycleSlotBlockBinding binding, out string diagnostic)
        {
            if (binding == null || binding.StandardItem == null)
            {
                diagnostic = "SlotBlock requires a resolved standard item.";
                return false;
            }
            if (!ValidateConfiguration(out diagnostic)) return false;

            ConfigureRequirement(
                binding.StandardItem,
                binding.StandardHeldCount,
                binding.StandardRequiredCount,
                binding.ActionIcon);

            var byAnchor = new ShortCycleSubCardBinding[ShortCycleAnchorContract.SubstituteAnchorCount];
            foreach (var substitute in binding.Substitutes ?? new ShortCycleSubCardBinding[0])
            {
                if (substitute != null && substitute.AnchorIndex >= 0 && substitute.AnchorIndex < byAnchor.Length)
                {
                    byAnchor[substitute.AnchorIndex] = substitute;
                }
            }

            for (var index = 0; index < subCards.Length; index++)
            {
                if (subCards[index] == null)
                {
                    diagnostic = "SubCard reference " + ShortCycleAnchorContract.GetSubstituteAnchorName(index) + " is not connected.";
                    return false;
                }
                subCards[index].Configure(byAnchor[index]);
            }

            diagnostic = null;
            return true;
        }

        /// <summary>Clears bound content without changing this presentation object's active state.</summary>
        public void ClearSlot()
        {
            Configure(new ShortCycleGridPieceDisplay());
            SetSecondaryIcon(actionIconRenderer, null);
            foreach (var subCard in subCards ?? new ShortCycleSubCard[0])
                if (subCard != null) subCard.Clear();
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
