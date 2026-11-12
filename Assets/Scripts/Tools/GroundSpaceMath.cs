using System;
using UnityEngine;

namespace EatWhat.Toolkit
{
    /// <summary>Ground distance between world-space foot positions; X/Y only, Z ignored.</summary>
    public static class GroundSpaceMath
    {
        public static float GroundDistance(Vector3 a, Vector3 b, float depth)
        {
            if (!Finite(a.x) || !Finite(a.y) || !Finite(b.x) || !Finite(b.y))
                throw new ArgumentOutOfRangeException(nameof(a), "Foot X/Y coordinates must be finite.");
            if (!Finite(depth) || depth <= 0f)
                throw new ArgumentOutOfRangeException(nameof(depth), "DEPTH must be finite and positive.");

            double dx = (double)a.x - b.x;
            double dy = ((double)a.y - b.y) / depth;
            double distance = Math.Sqrt(dx * dx + dy * dy);
            if (double.IsNaN(distance) || double.IsInfinity(distance) || distance > float.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(a), "Ground distance exceeds the finite float range.");
            return (float)distance;
        }

        private static bool Finite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
