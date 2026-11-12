using System;
using EatWhat.LongLoop;
using EatWhat.LongLoop.Configuration;
using EatWhat.Toolkit.Input;

internal static class StreetVerticalOffsetTests
{
    private static int checks, scenarios;
    private static void Check(bool ok, string name)
    { checks++; if (!ok) throw new Exception(name); }
    private static void Near(double actual, double expected, string name, double epsilon = 1e-6)
        => Check(!double.IsNaN(actual) && Math.Abs(actual - expected) <= epsilon,
            name + ": " + actual + " / " + expected);
    private static StreetVerticalOffsetSettings Settings(StreetCurvePreset curve = StreetCurvePreset.SmoothStep)
        => new StreetVerticalOffsetSettings { SpeedThreshold = .5f, InTime = 1, OutTime = 2, Curve = curve };
    private static StreetVerticalOffsetResult Step(StreetVerticalOffsetState state, float input,
        double speed, double dt, StreetCurvePreset curve = StreetCurvePreset.SmoothStep)
        => StreetVerticalOffset.Evaluate(state, new StreetVerticalOffsetInput {
            VerticalInput = input, ActualVerticalSpeed = speed, DeltaTime = dt }, Settings(curve));
    private static void Scenario(string name, Action test)
    { test(); scenarios++; Console.WriteLine("VERTICAL_OFFSET PASS " + name); }
    public static int Main()
    {
        try
        {
            Scenario("BlockedInputEntersBothDirections", Enter);
            Scenario("ReleaseAndRecoveredSpeedExit", Exit);
            Scenario("StrictThresholdAndNeutral", Threshold);
            Scenario("ContinuousDirectReversal", Reversal);
            Scenario("CurvesAndFullDuration", Curves);
            Scenario("ExistingInputDirection", Input);
            Scenario("TimeConfigurationAndInvalidBoundaries", Boundaries);
            Console.WriteLine("VERTICAL_OFFSET PASS scenarios=" + scenarios + " assertions=" + checks);
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
    private static void Enter()
    {
        foreach (float direction in new[] { -1f, 1f })
        {
            var half = Step(default, direction, 0, .5);
            Check(half.Valid && half.Triggered, "blocked input triggers");
            Near(half.Target, direction, "target sign"); Near(half.Value, direction * .5, "half enter");
            var full = Step(half.State, direction, -.1, .5);
            Near(full.Value, direction, "full enter");
            Near(Step(full.State, direction, .1, 10).Value, direction, "held edge stays full");
        }
    }
    private static void Exit()
    {
        foreach (float direction in new[] { -1f, 1f })
        {
            var full = Step(default, direction, 0, 1);
            var released = Step(full.State, 0, 0, 1);
            Check(!released.Triggered, "release untriggers");
            Near(released.Value, direction * .5, "half exit after release");
            Near(Step(released.State, 0, 0, 1).Value, 0, "release complete");
            foreach (double speed in new[] { -1d, 1d })
            {
                var moving = Step(full.State, direction, speed, 1);
                Check(!moving.Triggered, "actual speed recovers");
                Near(moving.Value, direction * .5, "recovery exits using OutTime");
                Near(Step(moving.State, direction, speed, 1).Value, 0, "recovery complete");
            }
        }
    }
    private static void Threshold()
    {
        foreach (float input in new[] { -1f, 0f, 1f })
        foreach (double speed in new[] { -.5, .5, -1d, 1d })
        {
            var r = Step(default, input, speed, 10);
            Check(r.Valid && !r.Triggered, "equality and above never trigger");
            Near(r.Value, 0, "speed at or above threshold zero");
        }
        Near(Step(default, 0, 0, 10).Value, 0, "no input zero at rest");
        Check(Step(default, 1, .499, 1).Triggered, "strictly below triggers");
        var settings = Settings(); settings.SpeedThreshold = 0;
        var zero = StreetVerticalOffset.Evaluate(default, new StreetVerticalOffsetInput {
            VerticalInput = 1, ActualVerticalSpeed = 0, DeltaTime = 1 }, settings);
        Check(zero.Valid && !zero.Triggered && zero.Value == 0, "zero threshold has no below-speed case");
    }
    private static void Reversal()
    {
        foreach (StreetCurvePreset curve in Enum.GetValues(typeof(StreetCurvePreset)))
        foreach (float direction in new[] { -1f, 1f })
        {
            var partial = Step(default, direction, 0, .25, curve);
            var changed = Step(partial.State, -direction, 0, 0, curve);
            Near(changed.Value, partial.Value, "zero dt reversal preserves exact output");
            Near(changed.State.Start, partial.State.Value, "reversal rebases current value");
            Near(changed.State.Duration, 1, "reversal uses InTime");
            var tiny = Step(changed.State, -direction, 0, .00001, curve);
            Check(Math.Abs(tiny.Value - changed.Value) < .0001, "positive small step continuous");
            Near(Step(changed.State, -direction, 0, 1, curve).Value, -direction, "reverse reaches new end");
        }
    }
    private static void Curves()
    {
        // Analytical midpoint at u=.25, independent of production CurveValue.
        foreach (StreetCurvePreset curve in Enum.GetValues(typeof(StreetCurvePreset)))
        {
            double expected = curve == StreetCurvePreset.Linear ? .25 :
                curve == StreetCurvePreset.Quadratic ? .0625 :
                curve == StreetCurvePreset.SmoothStep ? .15625 : .103515625;
            Near(Step(default, 1, 0, .25, curve).Value, expected, "curve at quarter");
            foreach (int fps in new[] { 30, 60, 120 })
            {
                var state = default(StreetVerticalOffsetState);
                for (int i = 0; i < fps; i++) state = Step(state, 1, 0, 1d / fps, curve).State;
                Near(state.Value, 1, "full enter duration " + fps);
                for (int i = 0; i < fps * 2; i++) state = Step(state, 0, 0, 1d / fps, curve).State;
                Near(state.Value, 0, "full exit duration " + fps);
            }
        }
    }
    private static void Input()
    {
        Near(Step(default, GameInputMath.VerticalFromKeys(true, false, false, false), 0, 1).Value, 1, "W upward");
        Near(Step(default, GameInputMath.VerticalFromKeys(false, true, false, false), 0, 1).Value, -1, "S downward");
        Near(Step(default, GameInputMath.VerticalFromKeys(true, true, false, false), 0, 1).Value, 0, "opposing keys neutral");
        Near(Step(default, .2f, 0, 1).Value, 1, "positive input interpreted by direction");
        Near(Step(default, -.2f, 0, 1).Value, -1, "negative input interpreted by direction");
    }
    private static void Boundaries()
    {
        var settings = Settings();
        var partial = Step(default, 1, 0, .25);
        var same = StreetVerticalOffset.Evaluate(partial.State, new StreetVerticalOffsetInput {
            VerticalInput = 1, DeltaTime = 0 }, settings);
        Near(same.Value, partial.Value, "zero dt hold");
        settings.InTime = 0;
        var zero = StreetVerticalOffset.Evaluate(partial.State, new StreetVerticalOffsetInput {
            VerticalInput = -1, DeltaTime = 0 }, settings);
        Near(zero.Value, partial.Value, "zero duration zero dt hold");
        var immediate = StreetVerticalOffset.Evaluate(zero.State, new StreetVerticalOffsetInput {
            VerticalInput = -1, DeltaTime = .01 }, settings);
        Near(immediate.Value, -1, "zero enter duration endpoint");
        settings.OutTime = 0;
        Near(StreetVerticalOffset.Evaluate(immediate.State, new StreetVerticalOffsetInput {
            VerticalInput = 0, DeltaTime = .01 }, settings).Value, 0, "zero exit duration endpoint");
        settings = Settings(); settings.InTime = 2;
        var changed = StreetVerticalOffset.Evaluate(partial.State, new StreetVerticalOffsetInput {
            VerticalInput = 1, DeltaTime = 0 }, settings);
        Near(changed.Value, partial.Value, "config change hold");
        Near(changed.State.Elapsed, 0, "config change restarts time");
        Near(changed.State.Duration, 2, "new duration consumed");
        foreach (double dt in new[] { -.1, double.NaN, double.PositiveInfinity })
        {
            var bad = Step(partial.State, 1, 0, dt);
            Check(!bad.Valid && bad.State.Value == partial.State.Value, "invalid time keeps prior");
        }
        Check(!Step(partial.State, float.NaN, 0, 1).Valid, "invalid input rejected");
        Check(!Step(partial.State, 1, double.NaN, 1).Valid, "invalid speed rejected");
        settings = Settings(); settings.SpeedThreshold = -.1f;
        Check(!StreetVerticalOffset.Valid(settings), "negative threshold rejected");
        settings = Settings(); settings.OutTime = float.PositiveInfinity;
        Check(!StreetVerticalOffset.Valid(settings), "infinite duration rejected");
        settings = Settings((StreetCurvePreset)99);
        Check(!StreetVerticalOffset.Valid(settings), "unknown curve rejected");
        var invalid = partial.State; invalid.Value = 2;
        Check(!Step(invalid, 1, 0, 1).Valid, "invalid initialized state rejected");
    }
}
