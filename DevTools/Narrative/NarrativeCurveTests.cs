using System;
using EatWhat.Narrative.Core;
using EatWhat.Toolkit;

internal static class NarrativeCurveTests
{
    private static int checks;
    private static void Check(bool value, string name)
    { checks++; if (!value) throw new Exception(name); }
    private static void Bits(double expected, double actual, string name)
    { Check(BitConverter.DoubleToInt64Bits(expected) == BitConverter.DoubleToInt64Bits(actual), name); }
    private static void Compare(int curve, double u)
    {
        var old = (LegacyStreetCurvePreset)curve;
        var current = (NarrativeCurvePreset)curve;
        double value = StreetCurveOracle.CurveValue(old, u), integral = StreetCurveOracle.CurveIntegral(old, u);
        Bits(value, NarrativeCurves.Value(current, u), "Core value bit equality");
        Bits(integral, NarrativeCurves.Integral(current, u), "Core integral bit equality");
        Bits(value, NarrativeCurveBridge.Value(curve, u), "Bridge value bit equality");
        Bits(integral, NarrativeCurveBridge.Integral(curve, u), "Bridge integral bit equality");
    }
    private static void Invalid(Func<double> expected, Func<double> actual)
    {
        ArgumentOutOfRangeException before = null, after = null;
        try { expected(); } catch (ArgumentOutOfRangeException e) { before = e; }
        try { actual(); } catch (ArgumentOutOfRangeException e) { after = e; }
        Check(before != null && after != null, "Invalid enum preserves exception type");
        Check(before.ParamName == after.ParamName && after.ParamName == "curve", "Invalid enum preserves ParamName");
        Check(before.ActualValue == after.ActualValue, "Invalid enum preserves ActualValue");
    }
    public static void Run()
    {
        string[] names = { "Linear", "Quadratic", "SmoothStep", "EaseInOut" };
        string[] actual = Enum.GetNames(typeof(NarrativeCurvePreset));
        string[] legacy = Enum.GetNames(typeof(LegacyStreetCurvePreset));
        Check(actual.Length == 4 && legacy.Length == 4, "Four enum members");
        for (int c = 0; c < 4; c++)
        {
            Check(actual[c] == names[c] && legacy[c] == names[c], "Original enum order");
            Check((int)NarrativeCurves.FromOrdinal(c) == c, "Explicit ordinal mapping");
            for (int i = 0; i <= 1000; i++) Compare(c, i / 1000d);
        }
        double[] boundary = { -2, -double.Epsilon, -0d, 0, 1, 1.0000000000000002d, 2,
            double.MinValue, double.MaxValue, double.NegativeInfinity, double.PositiveInfinity, double.NaN,
            BitConverter.Int64BitsToDouble(unchecked((long)0x7ff8000000000052UL)) };
        foreach (double u in boundary) for (int c = 0; c < 4; c++) Compare(c, u);
        foreach (int c in new[] { -1, 4, int.MinValue, int.MaxValue })
        {
            bool rejected = false;
            try { NarrativeCurves.FromOrdinal(c); } catch (ArgumentOutOfRangeException e) { rejected = e.ParamName == "curve" && e.ActualValue == null; }
            Check(rejected, "Ordinal mapping rejects an undefined serialized value");
            foreach (double u in boundary)
            {
                Invalid(() => StreetCurveOracle.CurveValue((LegacyStreetCurvePreset)c, u),
                    () => NarrativeCurves.Value((NarrativeCurvePreset)c, u));
                Invalid(() => StreetCurveOracle.CurveIntegral((LegacyStreetCurvePreset)c, u),
                    () => NarrativeCurves.Integral((NarrativeCurvePreset)c, u));
                Invalid(() => StreetCurveOracle.CurveValue((LegacyStreetCurvePreset)c, u),
                    () => NarrativeCurveBridge.Value(c, u));
                Invalid(() => StreetCurveOracle.CurveIntegral((LegacyStreetCurvePreset)c, u),
                    () => NarrativeCurveBridge.Integral(c, u));
            }
        }
        Console.WriteLine("PASS narrative curve tests: " + checks + " checks; curves=4; pointsPerCurve=1001; boundaryInputs=" + boundary.Length);
    }
}
