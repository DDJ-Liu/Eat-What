using System;
using UnityEngine;

namespace EatWhat.LongLoop
{
    /// <summary>Per-axis street displacement and projection into the active walkable union.</summary>
    public static class PlanarMotion
    {
        public static Vector2 Step(Vector2 current, Vector2 input, float speed,
            float verticalSpeedFactor, float seconds, WalkArea area, float left, float right)
        {
            if (!Finite(current.x) || !Finite(current.y) || !Finite(input.x) || !Finite(input.y) ||
                !Finite(speed) || !Finite(verticalSpeedFactor) || !Finite(seconds))
                throw new ArgumentOutOfRangeException(nameof(input), "Movement values must be finite.");

            var horizontal = Mathf.Clamp(input.x, -1f, 1f);
            var vertical = Mathf.Clamp(input.y, -1f, 1f);
            var distance = Mathf.Max(0f, speed) * Mathf.Max(0f, seconds);
            if (area == null || !area.IsUsable)
                return new Vector2(HorizontalMotion.Step(current.x, horizontal, speed, seconds, left, right),
                    current.y);
            if (distance == 0f || (horizontal == 0f && vertical == 0f)) return current;

            // Deliberately do not normalize diagonals: the approved rule scales each axis independently.
            var desired = new Vector2(current.x + horizontal * distance,
                current.y + vertical * distance * Mathf.Max(0f, verticalSpeedFactor));
            return area.Contains(desired) ? desired : area.ClosestPoint(desired);
        }

        private static bool Finite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
