using System;

namespace EatWhat.Narrative.Core
{
    // Ordinals match the existing serialized StreetCurvePreset; no LongLoop reference is needed.
    public enum NarrativeCurvePreset { Linear, Quadratic, SmoothStep, EaseInOut }

    public static class NarrativeCurves
    {
        public static NarrativeCurvePreset FromOrdinal(int curve)
        {
            switch (curve)
            {
                case 0: return NarrativeCurvePreset.Linear;
                case 1: return NarrativeCurvePreset.Quadratic;
                case 2: return NarrativeCurvePreset.SmoothStep;
                case 3: return NarrativeCurvePreset.EaseInOut;
                default: throw new ArgumentOutOfRangeException(nameof(curve));
            }
        }
        public static double Value(NarrativeCurvePreset curve,double u)
        {
            u=Clamp(u,0,1);
            switch(curve)
            {
                case NarrativeCurvePreset.Linear:return u;
                case NarrativeCurvePreset.Quadratic:return u*u;
                case NarrativeCurvePreset.SmoothStep:return Clamp(u*u*(3-2*u),0,1);
                case NarrativeCurvePreset.EaseInOut:return Clamp(u*u*u*(10+u*(-15+6*u)),0,1);
                default:throw new ArgumentOutOfRangeException(nameof(curve));
            }
        }
        public static double Integral(NarrativeCurvePreset curve,double u)
        {
            u=Clamp(u,0,1);
            switch(curve)
            {
                case NarrativeCurvePreset.Linear:return .5d*u*u;
                case NarrativeCurvePreset.Quadratic:return u*u*u/3d;
                case NarrativeCurvePreset.SmoothStep:return u*u*u*(1-.5d*u);
                case NarrativeCurvePreset.EaseInOut:return u*u*u*u*(2.5d+u*(-3+u));
                default:throw new ArgumentOutOfRangeException(nameof(curve));
            }
        }
        private static double Clamp(double v,double min,double max)=>Math.Max(min,Math.Min(max,v));
    }
}
