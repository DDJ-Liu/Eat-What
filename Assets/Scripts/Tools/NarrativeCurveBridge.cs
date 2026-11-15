using EatWhat.Narrative.Core;

namespace EatWhat.Toolkit
{
    // Primitive-only surface keeps existing LongLoop callers on their original Toolkit reference.
    public static class NarrativeCurveBridge
    {
        public static double Value(int curve, double u) => NarrativeCurves.Value(NarrativeCurves.FromOrdinal(curve), u);
        public static double Integral(int curve, double u) => NarrativeCurves.Integral(NarrativeCurves.FromOrdinal(curve), u);
    }
}
