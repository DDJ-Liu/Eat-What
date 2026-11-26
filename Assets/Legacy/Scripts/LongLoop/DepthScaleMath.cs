using UnityEngine;

namespace EatWhat.LongLoop
{
    public static class DepthScaleMath
    {
        public static bool TryFactor(float groundY, float footY, float coefficient, out float factor)
        {
            factor = 1f;
            if (!Finite(groundY) || !Finite(footY) || !Finite(coefficient)) return false;
            double value = 1d + ((double)groundY - footY) * coefficient;
            if (double.IsNaN(value) || double.IsInfinity(value) ||
                value > float.MaxValue || value < -float.MaxValue) return false;
            factor = (float)value;
            return true;
        }

        public static bool TryScale(Vector3 baseline, float factor, out Vector3 scale)
        {
            scale = baseline;
            if (!Finite(baseline) || !Finite(factor)) return false;
            double x = (double)baseline.x * factor;
            double y = (double)baseline.y * factor;
            double z = (double)baseline.z * factor;
            if (!Representable(x) || !Representable(y) || !Representable(z)) return false;
            scale = new Vector3((float)x, (float)y, (float)z);
            return true;
        }

        public static bool Finite(Vector3 value) => Finite(value.x) && Finite(value.y) && Finite(value.z);
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool Representable(double value) => !double.IsNaN(value) &&
            !double.IsInfinity(value) && value >= -float.MaxValue && value <= float.MaxValue;
    }
}
