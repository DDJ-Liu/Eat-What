using System;
using System.Collections.Generic;
using UnityEngine;

namespace EatWhat.Cooking.ShortCycle
{
    internal enum FridgeCatTierScrollMappingMode
    {
        UniformTierIndex = 0,
        AnchorAlignedAutoBounds = 1
    }

    /// <summary>Pure vertical paging calculations shared by the rig and editor-external tests.</summary>
    internal static class FridgeCatTierPagingMath
    {
        private const float Epsilon = 0.00001f;

        internal static bool TryCalculateNormalizedPosition(
            float firstAnchorWorldY,
            float currentAnchorWorldY,
            float lastAnchorWorldY,
            out float normalizedPosition)
        {
            normalizedPosition = 0f;
            if (!IsFinite(firstAnchorWorldY) || !IsFinite(currentAnchorWorldY) || !IsFinite(lastAnchorWorldY))
            {
                return false;
            }

            var anchorSpan = firstAnchorWorldY - lastAnchorWorldY;
            if (anchorSpan <= Epsilon)
            {
                return false;
            }

            var rawPosition = (firstAnchorWorldY - currentAnchorWorldY) / anchorSpan;
            if (rawPosition < -Epsilon || rawPosition > 1f + Epsilon)
            {
                return false;
            }

            normalizedPosition = Mathf.Clamp01(rawPosition);
            return true;
        }

        internal static bool TryCalculateVerticalBoundsLayout(
            float viewportCenterWorldY,
            float viewportHalfHeightWorld,
            float contentTransformWorldY,
            float contentScaleWorldY,
            float firstAnchorWorldY,
            float lastAnchorWorldY,
            float alignmentTargetWorldY,
            out float contentColliderLocalHeight,
            out float contentColliderLocalOffsetY)
        {
            contentColliderLocalHeight = 0f;
            contentColliderLocalOffsetY = 0f;
            if (!IsFinite(viewportCenterWorldY) || !IsFinite(viewportHalfHeightWorld) ||
                !IsFinite(contentTransformWorldY) || !IsFinite(contentScaleWorldY) ||
                !IsFinite(firstAnchorWorldY) || !IsFinite(lastAnchorWorldY) ||
                !IsFinite(alignmentTargetWorldY) || viewportHalfHeightWorld < 0f ||
                Mathf.Abs(contentScaleWorldY) <= Epsilon)
            {
                return false;
            }

            var anchorSpanWorld = firstAnchorWorldY - lastAnchorWorldY;
            if (anchorSpanWorld < -Epsilon)
            {
                return false;
            }
            anchorSpanWorld = Mathf.Max(0f, anchorSpanWorld);

            var contentHeightWorld = (viewportHalfHeightWorld * 2f) + anchorSpanWorld;
            contentColliderLocalHeight = contentHeightWorld / Mathf.Abs(contentScaleWorldY);

            // Inner DragLimit minY is viewportTop - contentHalfHeight - geometricCenterOffset.
            // Place that min endpoint so the first paging anchor lands on the fixed target line.
            var desiredMinimumContentWorldY = alignmentTargetWorldY -
                (firstAnchorWorldY - contentTransformWorldY);
            var geometricCenterOffsetWorldY = viewportCenterWorldY -
                (anchorSpanWorld * 0.5f) - desiredMinimumContentWorldY;
            contentColliderLocalOffsetY = geometricCenterOffsetWorldY / contentScaleWorldY;
            return IsFinite(contentColliderLocalHeight) && IsFinite(contentColliderLocalOffsetY);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }

    /// <summary>
    /// Owns the two fixed shenti2 shelf rows plus pooled shenti extension rows.
    /// Runtime content addresses rows and columns only; it never searches the hierarchy by name.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FridgeCatRig : MonoBehaviour
    {
        [Header("Fixed shenti2 rows")]
        [Tooltip("Connect ShelfRow_1 and ShelfRow_2 in order. Each row must expose five anchors.")]
        [SerializeField] private FridgeCatTier[] fixedTiers = new FridgeCatTier[0];

        [Header("Pooled shenti extension rows")]
        [SerializeField] private FridgeCatTier extensionTierPrefab = null;
        [SerializeField] private Transform extensionTierRoot = null;
        [SerializeField] private Vector3 firstExtensionLocalPosition_TUNE = Vector3.zero;
        [Min(0f)]
        [SerializeField] private float tierSpacing_TUNE = 1f;

        [Header("Scrolling")]
        [SerializeField] private ScrollArea_Controller scrollArea = null;
        [Tooltip("UniformTierIndex preserves the legacy index/(count-1) mapping. AnchorAlignedAutoBounds derives each page from real shelf-anchor spacing and fits ContentArea to the active vertical span.")]
        [SerializeField] private FridgeCatTierScrollMappingMode tierScrollMapping = FridgeCatTierScrollMappingMode.UniformTierIndex;
        [Tooltip("Fixed, non-scrolling Transform that marks where the selected shelf anchor should land in world Y.")]
        [SerializeField] private Transform tierAlignmentTarget_TUNE = null;
        [Range(0, FridgeCatTier.ShelfColumnCount - 1)]
        [SerializeField] private int tierAlignmentColumn_TUNE = 2;

        private readonly List<FridgeCatTier> extensionTierPool = new List<FridgeCatTier>();
        private int activeTierCount;

        public int TierCount { get { return activeTierCount; } }
        public ScrollArea_Controller ScrollArea { get { return scrollArea; } }

        internal int PooledExtensionTierCount { get { return extensionTierPool.Count; } }

        /// <summary>
        /// Activates the requested number of rows. Rows above the two fixed rows are pooled, so
        /// repeated calls with the same count neither allocate nor duplicate hierarchy objects.
        /// </summary>
        public void SetTierCount(int tierCount)
        {
            var requestedCount = Math.Max(0, tierCount);
            var fixedRows = fixedTiers ?? new FridgeCatTier[0];

            for (var index = 0; index < fixedRows.Length; index++)
            {
                if (fixedRows[index] != null)
                {
                    fixedRows[index].gameObject.SetActive(index < requestedCount);
                }
            }

            var requiredExtensions = Math.Max(0, requestedCount - fixedRows.Length);
            while (extensionTierPool.Count < requiredExtensions)
            {
                if (extensionTierPrefab == null)
                {
                    break;
                }

                var parent = extensionTierRoot == null ? transform : extensionTierRoot;
                var extension = Instantiate(extensionTierPrefab, parent);
                if (extension == null)
                {
                    break;
                }

                extension.gameObject.name = "Tier_" + (fixedRows.Length + extensionTierPool.Count + 1).ToString("00");
                extensionTierPool.Add(extension);
            }

            for (var index = 0; index < extensionTierPool.Count; index++)
            {
                var extension = extensionTierPool[index];
                if (extension == null)
                {
                    continue;
                }

                extension.transform.localPosition = new Vector3(
                    firstExtensionLocalPosition_TUNE.x,
                    firstExtensionLocalPosition_TUNE.y - (tierSpacing_TUNE * index),
                    firstExtensionLocalPosition_TUNE.z);
                extension.gameObject.SetActive(index < requiredExtensions);
            }

            activeTierCount = Math.Min(requestedCount, fixedRows.Length + extensionTierPool.Count);
            if (tierScrollMapping == FridgeCatTierScrollMappingMode.AnchorAlignedAutoBounds)
            {
                TryRefreshAnchorAlignedBounds();
            }
        }

        /// <summary>Returns a zero-based row/column shelf anchor, or null for incomplete wiring.</summary>
        public Transform GetShelfAnchor(int row, int column)
        {
            if (row < 0 || row >= activeTierCount || column < 0 || column >= FridgeCatTier.ShelfColumnCount)
            {
                return null;
            }

            var tier = GetTier(row);
            return tier == null ? null : tier.GetShelfAnchor(column);
        }

        /// <summary>Scrolls to a zero-based row through the configured ScrollArea transition path.</summary>
        public bool ScrollToTier(int tierIndex)
        {
            if (scrollArea == null || activeTierCount <= 0)
            {
                return false;
            }

            var clampedTier = Math.Max(0, Math.Min(tierIndex, activeTierCount - 1));
            float normalizedPosition;
            if (tierScrollMapping == FridgeCatTierScrollMappingMode.AnchorAlignedAutoBounds)
            {
                if (!TryGetAnchorAlignedNormalizedPosition(clampedTier, out normalizedPosition))
                {
                    return false;
                }
            }
            else
            {
                normalizedPosition = activeTierCount <= 1
                    ? 0f
                    : (float)clampedTier / (activeTierCount - 1);
            }
            scrollArea.ScrollVerticalTo(normalizedPosition);
            return true;
        }

        private FridgeCatTier GetTier(int row)
        {
            var fixedRows = fixedTiers ?? new FridgeCatTier[0];
            if (row < 0)
            {
                return null;
            }
            if (row < fixedRows.Length)
            {
                return fixedRows[row];
            }

            var extensionIndex = row - fixedRows.Length;
            return extensionIndex < extensionTierPool.Count ? extensionTierPool[extensionIndex] : null;
        }

        private bool TryGetPagingAnchorWorldY(int tierIndex, out float worldY)
        {
            worldY = 0f;
            var tier = GetTier(tierIndex);
            var column = Math.Max(0, Math.Min(tierAlignmentColumn_TUNE, FridgeCatTier.ShelfColumnCount - 1));
            var anchor = tier == null ? null : tier.GetShelfAnchor(column);
            if (anchor == null || float.IsNaN(anchor.position.y) || float.IsInfinity(anchor.position.y))
            {
                return false;
            }

            worldY = anchor.position.y;
            return true;
        }

        private bool TryGetAnchorAlignedNormalizedPosition(int tierIndex, out float normalizedPosition)
        {
            normalizedPosition = 0f;
            if (activeTierCount == 1)
            {
                float singleAnchorWorldY;
                return TryGetPagingAnchorWorldY(0, out singleAnchorWorldY);
            }

            float firstWorldY;
            float currentWorldY;
            float lastWorldY;
            return HasStrictlyDescendingPagingAnchors(out firstWorldY, out lastWorldY) &&
                TryGetPagingAnchorWorldY(tierIndex, out currentWorldY) &&
                FridgeCatTierPagingMath.TryCalculateNormalizedPosition(
                    firstWorldY,
                    currentWorldY,
                    lastWorldY,
                    out normalizedPosition);
        }

        private bool TryRefreshAnchorAlignedBounds()
        {
            if (activeTierCount <= 0)
            {
                return true;
            }
            if (scrollArea == null || scrollArea.scrollMode != ScrollArea_Controller.ScrollMode.Vertical ||
                scrollArea.contentDragContainer == null || scrollArea.viewportDragLimit == null ||
                tierAlignmentTarget_TUNE == null)
            {
                return false;
            }

            var contentTransform = scrollArea.contentDragContainer.transform;
            if (tierAlignmentTarget_TUNE.IsChildOf(contentTransform) ||
                (scrollArea.content != null && tierAlignmentTarget_TUNE.IsChildOf(scrollArea.content)))
            {
                return false;
            }

            float firstWorldY;
            float lastWorldY;
            if (activeTierCount == 1)
            {
                if (!TryGetPagingAnchorWorldY(0, out firstWorldY))
                {
                    return false;
                }
                lastWorldY = firstWorldY;
            }
            else if (!HasStrictlyDescendingPagingAnchors(out firstWorldY, out lastWorldY))
            {
                return false;
            }

            var content = scrollArea.contentDragContainer;
            if (content.sizeSource != DragContainer.SizeSource.BoxCollider2D)
            {
                return false;
            }
            var contentCollider = content.col == null ? content.GetComponent<BoxCollider2D>() : content.col;
            if (contentCollider == null)
            {
                return false;
            }

            var viewportBounds = scrollArea.viewportDragLimit.GetBounds();
            float localHeight;
            float localOffsetY;
            if (!FridgeCatTierPagingMath.TryCalculateVerticalBoundsLayout(
                viewportBounds.center.y,
                viewportBounds.halfH,
                contentTransform.position.y,
                contentTransform.lossyScale.y,
                firstWorldY,
                lastWorldY,
                tierAlignmentTarget_TUNE.position.y,
                out localHeight,
                out localOffsetY))
            {
                return false;
            }

            var sizeChanged = !Mathf.Approximately(contentCollider.size.y, localHeight);
            var offsetChanged = !Mathf.Approximately(contentCollider.offset.y, localOffsetY);
            if (!sizeChanged && !offsetChanged)
            {
                return true;
            }

            content.col = contentCollider;
            contentCollider.size = new Vector2(contentCollider.size.x, localHeight);
            contentCollider.offset = new Vector2(contentCollider.offset.x, localOffsetY);
            if (content.limitMode == DragContainer.LimitMode.DragLimit && content.dragLimit != null)
            {
                content.UpdateBounds();
            }
            return true;
        }

        private bool HasStrictlyDescendingPagingAnchors(out float firstWorldY, out float lastWorldY)
        {
            firstWorldY = 0f;
            lastWorldY = 0f;
            if (activeTierCount <= 1 || !TryGetPagingAnchorWorldY(0, out firstWorldY))
            {
                return false;
            }

            var previousWorldY = firstWorldY;
            for (var index = 1; index < activeTierCount; index++)
            {
                float currentWorldY;
                if (!TryGetPagingAnchorWorldY(index, out currentWorldY) || currentWorldY >= previousWorldY)
                {
                    return false;
                }
                previousWorldY = currentWorldY;
            }
            lastWorldY = previousWorldY;
            return true;
        }
    }
}
