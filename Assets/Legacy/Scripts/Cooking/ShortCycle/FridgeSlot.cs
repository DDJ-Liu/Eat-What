using System.Collections.Generic;
using System.Globalization;
using EatWhat.Toolkit;
using UnityEngine;

namespace EatWhat.Cooking.ShortCycle
{
    public sealed class FridgeSlot : ShortCycleGridPieceBase
    {
        [Header("GRID-01 icon layers (program initial values; U2 tuning pending)")]
        [SerializeField] private SpriteRenderer[] singleIconLayers = new SpriteRenderer[0];
        [SerializeField] private SpriteRenderer[] stackIconLayers = new SpriteRenderer[0];
        [SerializeField] private SpriteRenderer[] packIconLayers = new SpriteRenderer[0];
        [SerializeField] private SpriteRenderer[] containerIconLayers = new SpriteRenderer[0];
        [SerializeField] private int singleVisibleCapacity = 1;
        [SerializeField] private int stackVisibleCapacity = 2;
        [SerializeField] private int packVisibleCapacity = 2;
        [SerializeField] private int containerVisibleCapacity = 1;

        private StackedIconPlan iconPlan;
        private Sprite iconSprite;
        private readonly Dictionary<StackedIconShape, int> visibleCapacities =
            new Dictionary<StackedIconShape, int>(4);

        public string ItemId { get; private set; }
        public string InstanceId { get; private set; }
        public ToolkitPlaceholderState AssetState { get; private set; }
        public StackedIconPlan IconPlan { get { return iconPlan; } }
        protected override bool ContentAllowsInteraction { get { return iconPlan != null && iconPlan.Quantity > 0; } }

        public void ConfigureItem(CK01ItemData item, int quantity, bool isPlaceholder)
        {
            ConfigureItem(item, quantity, isPlaceholder, item == null ? null : item.name, null, 0);
        }

        public void ConfigureItem(CK01ItemData item, int quantity, bool isPlaceholder,
            string itemId, string instanceId, int reserved)
        {
            ItemId = item != null && quantity > 0 ? itemId : null;
            InstanceId = item != null && quantity > 0 ? instanceId : null;
            iconSprite = item == null || quantity <= 0 ? null : item.icon_sprite;
            AssetState = PlaceholderRule.Classify(iconSprite != null, iconSprite == null ? null : iconSprite.name);
            visibleCapacities[StackedIconShape.Single] = singleVisibleCapacity;
            visibleCapacities[StackedIconShape.Stack] = stackVisibleCapacity;
            visibleCapacities[StackedIconShape.Pack] = packVisibleCapacity;
            visibleCapacities[StackedIconShape.Container] = containerVisibleCapacity;
            iconPlan = item == null || quantity <= 0 ? null :
                StackedIcon.Plan(item.stack_type, quantity, visibleCapacities, true);
            var badge = iconPlan != null && quantity > 1 ? "x" + iconPlan.QuantityLabel : string.Empty;
            if (iconPlan != null && item.stack_type == "stack" && reserved > 0)
                badge = reserved.ToString(CultureInfo.InvariantCulture) + "/" + quantity.ToString(CultureInfo.InvariantCulture);
            else if (iconPlan != null && item.stack_type == "pack" && item.pack_size > 0)
                badge = item.pack_size.ToString(CultureInfo.InvariantCulture) + "/" + item.pack_size.ToString(CultureInfo.InvariantCulture);
            else if (iconPlan != null && item.stack_type == "container" && item.container_capacity > 0f)
                badge = item.container_capacity.ToString("0.##", CultureInfo.InvariantCulture) + "/" +
                    item.container_capacity.ToString("0.##", CultureInfo.InvariantCulture);
            Configure(new ShortCycleGridPieceDisplay
            {
                icon_sprite = iconSprite,
                name_key = iconPlan == null ? null : item.name_key,
                fallback_name = iconPlan == null ? string.Empty : item.name,
                badge_text = badge,
                // The binder also passes true for a missing sprite. A reservation or a
                // present sprite identifies the separate business placeholder state.
                is_placeholder = iconPlan != null && isPlaceholder && (reserved > 0 || iconSprite != null)
            });
            RefreshInteractionGate();
        }

        protected override void OnDisplayStateChanged()
        {
            ClearLayers(singleIconLayers);
            ClearLayers(stackIconLayers);
            ClearLayers(packIconLayers);
            ClearLayers(containerIconLayers);
            ClearLayer(PrimaryIconRenderer);
            var show = ContentVisible && isActiveAndEnabled && gameObject.activeInHierarchy && iconPlan != null;
            var placeholder = PlaceholderRenderer;
            if (placeholder != null)
                placeholder.enabled = show && (AssetState == ToolkitPlaceholderState.Missing ||
                    (Display != null && Display.is_placeholder));
            if (!show || iconSprite == null) return;

            var layers = GetLayers(iconPlan.Shape);
            if (layers == null || layers.Length == 0)
            {
                var primary = PrimaryIconRenderer;
                if (primary != null && iconPlan.VisibleIconCount > 0)
                {
                    primary.sprite = iconSprite;
                    primary.enabled = true;
                }
                return;
            }
            for (var index = 0; index < iconPlan.VisibleIconCount && index < layers.Length; index++)
            {
                var layer = layers[index];
                if (layer == null) continue;
                layer.sprite = iconSprite;
                layer.enabled = true;
            }
        }

        private SpriteRenderer[] GetLayers(StackedIconShape shape)
        {
            switch (shape)
            {
                case StackedIconShape.Single: return singleIconLayers;
                case StackedIconShape.Stack: return stackIconLayers;
                case StackedIconShape.Pack: return packIconLayers;
                case StackedIconShape.Container: return containerIconLayers;
                default: return null;
            }
        }

        private static void ClearLayers(SpriteRenderer[] layers)
        {
            if (layers == null) return;
            foreach (var layer in layers) ClearLayer(layer);
        }

        private static void ClearLayer(SpriteRenderer layer)
        {
            if (layer == null) return;
            layer.sprite = null;
            layer.enabled = false;
        }
    }
}
