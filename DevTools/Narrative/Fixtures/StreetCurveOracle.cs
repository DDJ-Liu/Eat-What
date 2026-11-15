using System;

// Independent oracle preserved from StreetFollowPipeline before N0-02 extraction.
// Source SHA256: 80E8FF8F0F81DB7F59C4F0ADCF44CB66D9DB5F2595A191506D98CC08857CD693
// Enum ordering/expressions/Clamp/exception semantics retained; never derived from NarrativeCurves.
internal enum LegacyStreetCurvePreset { Linear, Quadratic, SmoothStep, EaseInOut }
internal static class StreetCurveOracle
{
        public static double CurveValue(LegacyStreetCurvePreset curve,double u)
        {
            u=Clamp(u,0,1);
            switch(curve)
            {
                case LegacyStreetCurvePreset.Linear:return u;
                case LegacyStreetCurvePreset.Quadratic:return u*u;
                case LegacyStreetCurvePreset.SmoothStep:return Clamp(u*u*(3-2*u),0,1);
                case LegacyStreetCurvePreset.EaseInOut:return Clamp(u*u*u*(10+u*(-15+6*u)),0,1);
                default:throw new ArgumentOutOfRangeException(nameof(curve));
            }
        }
        public static double CurveIntegral(LegacyStreetCurvePreset curve,double u)
        {
            u=Clamp(u,0,1);
            switch(curve)
            {
                case LegacyStreetCurvePreset.Linear:return .5d*u*u;
                case LegacyStreetCurvePreset.Quadratic:return u*u*u/3d;
                case LegacyStreetCurvePreset.SmoothStep:return u*u*u*(1-.5d*u);
                case LegacyStreetCurvePreset.EaseInOut:return u*u*u*u*(2.5d+u*(-3+u));
                default:throw new ArgumentOutOfRangeException(nameof(curve));
            }
        }
        private static double Clamp(double v,double min,double max)=>Math.Max(min,Math.Min(max,v));
}
