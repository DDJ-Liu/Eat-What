using System;
using EatWhat.LongLoop.Configuration;

namespace EatWhat.LongLoop
{
    public struct StreetVerticalOffsetSettings
    {
        public float SpeedThreshold, InTime, OutTime;
        public StreetCurvePreset Curve;
    }

    public struct StreetVerticalOffsetInput
    {
        // Direction is interpreted by sign; speed must be actual foot Y speed.
        public float VerticalInput;
        public double ActualVerticalSpeed, DeltaTime;
    }

    // Owned by the driver. This is a channel transition, not camera damping.
    public struct StreetVerticalOffsetState
    {
        public bool Initialized;
        public double Value, Start, Target, Elapsed, Duration;
        public StreetCurvePreset Curve;
    }

    public struct StreetVerticalOffsetResult
    {
        public bool Valid, Triggered;
        public float Value, Target;
        public StreetVerticalOffsetState State;
    }

    public static class StreetVerticalOffset
    {
        public static bool Valid(StreetVerticalOffsetSettings settings)
            => Nonnegative(settings.SpeedThreshold) && Nonnegative(settings.InTime) &&
                Nonnegative(settings.OutTime) && StreetFollowPipeline.CurveDefined(settings.Curve);

        public static StreetVerticalOffsetResult Evaluate(StreetVerticalOffsetState previous,
            StreetVerticalOffsetInput input, StreetVerticalOffsetSettings settings)
        {
            var result = new StreetVerticalOffsetResult { State = previous };
            if (!Valid(settings) || !StreetFollowPipeline.Finite(input.VerticalInput) ||
                !StreetFollowPipeline.Finite(input.ActualVerticalSpeed) || !Nonnegative(input.DeltaTime) ||
                (previous.Initialized && !ValidState(previous))) return result;

            bool triggered = input.VerticalInput != 0 &&
                Math.Abs(input.ActualVerticalSpeed) < settings.SpeedThreshold;
            double target = triggered ? Math.Sign(input.VerticalInput) : 0;
            double duration = triggered ? settings.InTime : settings.OutTime;
            var next = previous.Initialized ? previous : new StreetVerticalOffsetState();
            if (!next.Initialized || next.Target != target ||
                next.Duration != duration || next.Curve != settings.Curve)
            {
                // Reversals start at the current value, using the full enter time.
                next.Initialized = true; next.Start = next.Value; next.Target = target;
                next.Elapsed = 0; next.Duration = duration; next.Curve = settings.Curve;
            }
            if (input.DeltaTime > 0)
            {
                next.Elapsed = Math.Min(next.Duration, next.Elapsed + input.DeltaTime);
                next.Value = next.Duration == 0 || next.Elapsed >= next.Duration ? next.Target :
                    next.Start + (next.Target - next.Start) *
                    StreetFollowPipeline.CurveValue(next.Curve, next.Elapsed / next.Duration);
            }
            result.Valid = true; result.Triggered = triggered;
            result.Value = (float)next.Value; result.Target = (float)target; result.State = next;
            return result;
        }

        private static bool ValidState(StreetVerticalOffsetState state)
            => Unit(state.Value) && Unit(state.Start) && Unit(state.Target) &&
                Nonnegative(state.Elapsed) && Nonnegative(state.Duration) &&
                state.Elapsed <= state.Duration && StreetFollowPipeline.CurveDefined(state.Curve);
        private static bool Nonnegative(double value) => StreetFollowPipeline.Finite(value) && value >= 0;
        private static bool Unit(double value) => StreetFollowPipeline.Finite(value) && Math.Abs(value) <= 1;
    }
}
