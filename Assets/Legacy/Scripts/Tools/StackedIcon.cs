using System;
using System.Collections.Generic;
using System.Globalization;

namespace EatWhat.Toolkit
{
    public enum StackedIconShape { Single, Stack, Pack, Container }

    public sealed class StackedIconPlan
    {
        public StackedIconShape Shape { get; private set; }
        public int Quantity { get; private set; }
        public int VisibleIconCount { get; private set; }
        public string QuantityLabel { get; private set; }

        internal StackedIconPlan(StackedIconShape shape, int quantity, int visibleIconCount, string quantityLabel)
        {
            Shape = shape;
            Quantity = quantity;
            VisibleIconCount = visibleIconCount;
            QuantityLabel = quantityLabel;
        }
    }

    /// <summary>
    /// Projects the four observed Items.stack_type values into a display plan.
    /// Visual capacity and whether to print the quantity are explicit caller inputs.
    /// </summary>
    public static class StackedIcon
    {
        public static bool TryParseShape(string stackType, out StackedIconShape shape)
        {
            switch (stackType)
            {
                case "single": shape = StackedIconShape.Single; return true;
                case "stack": shape = StackedIconShape.Stack; return true;
                case "pack": shape = StackedIconShape.Pack; return true;
                case "container": shape = StackedIconShape.Container; return true;
                default: shape = default(StackedIconShape); return false;
            }
        }

        public static StackedIconPlan Plan(
            string stackType,
            int quantity,
            IReadOnlyDictionary<StackedIconShape, int> visibleCapacityByShape,
            bool showQuantity)
        {
            StackedIconShape shape;
            if (!TryParseShape(stackType, out shape))
                throw new ArgumentException("Unknown Items.stack_type.", nameof(stackType));
            if (quantity < 0) throw new ArgumentOutOfRangeException(nameof(quantity));
            if (visibleCapacityByShape == null) throw new ArgumentNullException(nameof(visibleCapacityByShape));
            foreach (StackedIconShape required in Enum.GetValues(typeof(StackedIconShape)))
            {
                int capacity;
                if (!visibleCapacityByShape.TryGetValue(required, out capacity) || capacity <= 0)
                    throw new ArgumentException("A positive visible capacity is required for every stack shape.", nameof(visibleCapacityByShape));
            }

            var limit = visibleCapacityByShape[shape];
            return new StackedIconPlan(shape, quantity, Math.Min(quantity, limit),
                showQuantity ? quantity.ToString(CultureInfo.InvariantCulture) : null);
        }
    }
}