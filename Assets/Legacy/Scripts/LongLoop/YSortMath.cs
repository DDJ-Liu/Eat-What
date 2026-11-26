using System;

namespace EatWhat.LongLoop
{
    public static class YSortMath
    {
        public const int MinimumOrder = -32768;
        public const int MaximumOrder = 32767;

        public static bool TryOrder(int baseOrder, float footWorldY, float precision,
            float depthOffset, out int order, out bool saturated)
        {
            order = baseOrder;
            saturated = false;
            if (!Finite(footWorldY) || !Finite(precision) || !Finite(depthOffset) || precision < 0f)
                return false;
            double y = (double)footWorldY + depthOffset;
            double raw = baseOrder - Math.Round(y * precision, MidpointRounding.ToEven);
            if (raw > MaximumOrder) { order = MaximumOrder; saturated = true; }
            else if (raw < MinimumOrder) { order = MinimumOrder; saturated = true; }
            else order = (int)raw;
            return true;
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
