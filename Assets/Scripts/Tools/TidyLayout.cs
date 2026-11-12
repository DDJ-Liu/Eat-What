using System;
using System.Collections.Generic;

namespace EatWhat.Toolkit
{
    public sealed class TidyPlacement<TItem, TPoint>
    {
        public TItem Item { get; private set; }
        public TPoint Point { get; private set; }
        public int SourceIndex { get; private set; }
        public int PointIndex { get; private set; }

        internal TidyPlacement(TItem item, TPoint point, int sourceIndex, int pointIndex)
        {
            Item = item;
            Point = point;
            SourceIndex = sourceIndex;
            PointIndex = pointIndex;
        }
    }

    /// <summary>
    /// Plans a stable compaction onto a snapshot of existing TK-LAY-01 layout points.
    /// Does not generate coordinates, call ArrangeChildren, or mutate a Transform.
    /// </summary>
    public static class TidyLayout
    {
        public static IReadOnlyList<TidyPlacement<TItem, TPoint>> Plan<TItem, TPoint>(
            IReadOnlyList<TItem> items,
            Func<TItem, bool> isVisible,
            IReadOnlyList<TPoint> layoutPoints)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            if (isVisible == null) throw new ArgumentNullException(nameof(isVisible));
            if (layoutPoints == null) throw new ArgumentNullException(nameof(layoutPoints));

            var result = new List<TidyPlacement<TItem, TPoint>>();
            for (var index = 0; index < items.Count; index++)
            {
                var item = items[index];
                if (!isVisible(item)) continue;
                if (result.Count >= layoutPoints.Count)
                    throw new ArgumentException("Existing layout points are fewer than visible items.", nameof(layoutPoints));
                result.Add(new TidyPlacement<TItem, TPoint>(item, layoutPoints[result.Count], index, result.Count));
            }
            return result.AsReadOnly();
        }
    }
}