using System;
using UnityEngine;

namespace EatWhat.LongLoop
{
    public enum StreetParallaxPreset { Background, Buildings, MidgroundBack, Characters, MidgroundFront, Foreground }
    public struct StreetParallaxResult
    {
        public bool Valid;
        public Vector3 Correction;
    }

    public static class StreetParallax
    {
        public static float PresetCoefficient(StreetParallaxPreset preset)
        {
            switch (preset)
            {
                case StreetParallaxPreset.Background: return .3f;
                case StreetParallaxPreset.Buildings: return .6f;
                case StreetParallaxPreset.MidgroundBack: return .85f;
                case StreetParallaxPreset.Characters: return 1f;
                case StreetParallaxPreset.MidgroundFront: return 1.15f;
                case StreetParallaxPreset.Foreground: return 1.35f;
                default: throw new ArgumentOutOfRangeException(nameof(preset));
            }
        }

        // World-space XY translation only. Lens size is intentionally not an input.
        public static StreetParallaxResult Evaluate(Vector3 camera, Vector3 origin, Vector2 coefficients)
        {
            if (!StreetFollowPipeline.Finite(camera) || !StreetFollowPipeline.Finite(origin) ||
                !StreetFollowPipeline.Finite(coefficients)) return default;
            double x = ((double)camera.x - origin.x) * (1d - coefficients.x);
            double y = ((double)camera.y - origin.y) * (1d - coefficients.y);
            if (!Representable(x) || !Representable(y)) return default;
            return new StreetParallaxResult { Valid = true, Correction = new Vector3((float)x, (float)y, 0) };
        }
        private static bool Representable(double value)
            => StreetFollowPipeline.Finite(value) && Math.Abs(value) <= float.MaxValue;
    }
}
