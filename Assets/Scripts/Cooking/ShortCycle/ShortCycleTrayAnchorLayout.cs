using System;
using System.Collections.Generic;
using UnityEngine;

namespace EatWhat.Cooking.ShortCycle
{
    /// <summary>
    /// C3 adaptation of IngredientInventoryTray.Pos/AddItem/RemoveItem. The old
    /// compact-list behavior is retained, while name sorting and singleton data
    /// access are replaced with an explicit serialized 01..20 anchor order.
    /// Kept in a same-named file so Unity can persist its MonoScript reference.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShortCycleTrayAnchorLayout : MonoBehaviour
    {
        [Header("Manual TraySlot_01..20 anchors; index is authoritative")]
        [SerializeField] private Transform[] traySlotAnchors = new Transform[ShortCycleAnchorContract.TrayAnchorCount];

        private readonly List<Transform> itemViews = new List<Transform>(ShortCycleAnchorContract.TrayAnchorCount);

        public int ItemCount
        {
            get { return itemViews.Count; }
        }

        public bool ValidateAnchors(out string diagnostic)
        {
            return ValidateNamedAnchors(traySlotAnchors, ShortCycleAnchorContract.TrayAnchorNames, out diagnostic);
        }

        public bool TrySetItemViews(IEnumerable<Transform> views, out string diagnostic)
        {
            if (!ValidateAnchors(out diagnostic))
            {
                return false;
            }

            itemViews.Clear();
            itemViews.AddRange(ShortCycleAnchorContract.Compact(views, traySlotAnchors.Length));
            Reflow();
            diagnostic = null;
            return true;
        }

        public bool TryAddItemView(Transform view, out string diagnostic)
        {
            if (view == null)
            {
                diagnostic = "Tray item view is null.";
                return false;
            }
            if (!ValidateAnchors(out diagnostic))
            {
                return false;
            }
            if (itemViews.Contains(view))
            {
                diagnostic = "Tray item view is already present.";
                return false;
            }
            if (itemViews.Count >= traySlotAnchors.Length)
            {
                diagnostic = "Tray has no remaining anchor.";
                return false;
            }

            itemViews.Add(view);
            Reflow();
            diagnostic = null;
            return true;
        }

        public bool RemoveItemView(Transform view)
        {
            if (!itemViews.Remove(view))
            {
                return false;
            }
            Reflow();
            return true;
        }

        public void Reflow()
        {
            var count = Math.Min(itemViews.Count, traySlotAnchors == null ? 0 : traySlotAnchors.Length);
            for (var index = 0; index < count; index++)
            {
                var view = itemViews[index];
                var anchor = traySlotAnchors[index];
                if (view == null || anchor == null)
                {
                    continue;
                }
                view.SetParent(anchor, false);
                view.localPosition = Vector3.zero;
            }
        }

        internal static bool ValidateNamedAnchors(Transform[] anchors, IList<string> expectedNames, out string diagnostic)
        {
            if (anchors == null || anchors.Length != expectedNames.Count)
            {
                diagnostic = "Expected exactly " + expectedNames.Count + " serialized anchors.";
                return false;
            }

            for (var index = 0; index < anchors.Length; index++)
            {
                if (anchors[index] == null)
                {
                    diagnostic = "Anchor " + expectedNames[index] + " is not connected.";
                    return false;
                }
                if (!string.Equals(anchors[index].name, expectedNames[index], StringComparison.Ordinal))
                {
                    diagnostic = "Anchor index " + index + " must be named " + expectedNames[index] + ".";
                    return false;
                }
            }

            diagnostic = null;
            return true;
        }
    }
}
