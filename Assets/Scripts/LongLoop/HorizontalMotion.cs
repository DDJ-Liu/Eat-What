using System;

namespace EatWhat.LongLoop
{
    /// <summary>Side-view X calculation shared by Transform and Rigidbody2D paths.</summary>
    public static class HorizontalMotion
    {
        public static float Clamp(float x, float left, float right)
        {
            float lo = Math.Min(left, right);
            float hi = Math.Max(left, right);
            return Math.Min(hi, Math.Max(lo, x));
        }

        public static float Step(float x, float axis, float speed, float seconds, float left, float right)
        {
            float normalized = Math.Min(1f, Math.Max(-1f, axis));
            float distance = normalized * Math.Max(0f, speed) * Math.Max(0f, seconds);
            return Clamp(x + distance, left, right);
        }

        public static bool CanAdvance(float x, float axis, float speed, float left, float right)
        {
            if (speed <= 0f || axis == 0f) return false;
            float lo = Math.Min(left, right);
            float hi = Math.Max(left, right);
            return axis > 0f ? x < hi : x > lo;
        }
    }
}
