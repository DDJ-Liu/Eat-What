using System;
using EatWhat.LongLoop.Configuration;

namespace EatWhat.LongLoop
{
    // Caller-owned value snapshots. This module never reads assets or a camera.
    public struct StreetDynamicZoomSettings
    {
        public bool Enabled;
        public float MinSize, MaxSize, ExpandTime, ShrinkDelay, ShrinkTime;
        public StreetCurvePreset ExpandCurve, ShrinkCurve;
    }

    public struct StreetDynamicZoomInput
    {
        // Supply the existing FollowGoalStep.Moving (two-dimensional foot speed).
        public bool Moving, Frozen;
        public bool SuspendShrink;
        public double DeltaTime;
    }

    public struct StreetDynamicZoomState
    {
        public bool Initialized, Moving;
        public bool ShrinkSuspended;
        public double Size, StartSize, TargetSize, Elapsed, Duration;
        public double ShrinkDelay, ShrinkWaitElapsed;
        public StreetCurvePreset Curve;
    }

    public struct StreetDynamicZoomResult
    {
        public bool Valid;
        // Null means that the caller must not write a lens size.
        public float? Size;
        public float Ratio;
        public StreetDynamicZoomState State;
    }

    public static class StreetDynamicZoom
    {
        public static bool Valid(StreetDynamicZoomSettings settings)
        {
            return StreetFollowPipeline.Finite(settings.MinSize) &&
                StreetFollowPipeline.Finite(settings.MaxSize) && settings.MaxSize > 0 &&
                settings.MinSize <= settings.MaxSize &&
                Nonnegative(settings.ExpandTime) && Nonnegative(settings.ShrinkDelay) && Nonnegative(settings.ShrinkTime) &&
                StreetFollowPipeline.CurveDefined(settings.ExpandCurve) &&
                StreetFollowPipeline.CurveDefined(settings.ShrinkCurve);
        }

        public static StreetDynamicZoomResult Evaluate(StreetDynamicZoomState previous,
            StreetDynamicZoomInput input, StreetDynamicZoomSettings settings)
        {
            var result = new StreetDynamicZoomResult { State = previous, Ratio = 1 };
            // Disabled channels do not consume time or require unused settings.
            if (!settings.Enabled) { result.Valid = true; return result; }
            if (!Valid(settings) || !Nonnegative(input.DeltaTime) ||
                (previous.Initialized && !ValidState(previous))) return result;

            var next = previous;
            if (!next.Initialized)
            {
                next = new StreetDynamicZoomState { Initialized = true, Size = settings.MinSize,
                    StartSize = settings.MinSize, TargetSize = settings.MinSize,
                    Curve = settings.ShrinkCurve, ShrinkDelay = settings.ShrinkDelay,
                    ShrinkSuspended = input.SuspendShrink };
                // The first sample is always the minimum, even when already moving.
                return Output(result, next, settings.MaxSize);
            }
            if (input.Frozen)
            {
                // Observe intent without spending frozen time; release waits until
                // this channel can run, then restarts the complete return interval.
                if (input.SuspendShrink) next.ShrinkSuspended = true;
                return Output(result, next, settings.MaxSize);
            }
            bool restartShrink = next.ShrinkSuspended && !input.SuspendShrink && !input.Moving;
            next.ShrinkSuspended = input.SuspendShrink;
            if (input.SuspendShrink && !input.Moving)
            {
                // Hold only the return channel. Actual moving samples still expand.
                next.Moving = false;
                return Output(result, next, settings.MaxSize);
            }

            double target = input.Moving ? settings.MaxSize : settings.MinSize;
            double duration = input.Moving ? settings.ExpandTime : settings.ShrinkTime;
            var curve = input.Moving ? settings.ExpandCurve : settings.ShrinkCurve;
            bool delayChanged = next.ShrinkDelay != settings.ShrinkDelay;
            next.ShrinkDelay = settings.ShrinkDelay;
            if (settings.MinSize == settings.MaxSize)
            {
                next.Size = next.StartSize = next.TargetSize = target;
                next.Elapsed = next.Duration = 0;
                next.ShrinkWaitElapsed = 0;
                next.Moving = input.Moving; next.Curve = curve;
                return Output(result, next, settings.MaxSize);
            }
            if (next.Moving != input.Moving || next.TargetSize != target ||
                next.Duration != duration || next.Curve != curve || (!input.Moving && delayChanged) || restartShrink)
            {
                // Each changed direction/setting starts a full configured interval
                // at the current value; no endpoint reset and no extra damping.
                next.StartSize = next.Size; next.TargetSize = target;
                next.Elapsed = 0; next.Duration = duration;
                next.ShrinkWaitElapsed = 0;
                next.Moving = input.Moving; next.Curve = curve;
            }
            double advance = input.DeltaTime;
            if (!input.Moving && next.ShrinkWaitElapsed < next.ShrinkDelay)
            {
                double waited = Math.Min(advance, next.ShrinkDelay - next.ShrinkWaitElapsed);
                next.ShrinkWaitElapsed += waited;
                advance -= waited;
            }
            if (advance > 0)
            {
                next.Elapsed = Math.Min(next.Duration, next.Elapsed + advance);
                if (next.Duration == 0 || next.Elapsed >= next.Duration) next.Size = next.TargetSize;
                else next.Size = next.StartSize + (next.TargetSize - next.StartSize) *
                    StreetFollowPipeline.CurveValue(next.Curve, next.Elapsed / next.Duration);
            }
            return Output(result, next, settings.MaxSize);
        }

        private static StreetDynamicZoomResult Output(StreetDynamicZoomResult result,
            StreetDynamicZoomState state, double maximum)
        {
            double ratio = state.Size / maximum;
            if (!Representable(state.Size) || !Representable(ratio)) return result;
            result.Valid = true; result.Size = (float)state.Size;
            result.Ratio = (float)ratio; result.State = state;
            return result;
        }

        private static bool ValidState(StreetDynamicZoomState state)
        {
            return Representable(state.Size) && Representable(state.StartSize) &&
                Representable(state.TargetSize) && Nonnegative(state.Elapsed) &&
                Nonnegative(state.Duration) && state.Elapsed <= state.Duration &&
                Nonnegative(state.ShrinkDelay) && Nonnegative(state.ShrinkWaitElapsed) &&
                state.ShrinkWaitElapsed <= state.ShrinkDelay && (!state.Moving || state.ShrinkWaitElapsed == 0) &&
                StreetFollowPipeline.CurveDefined(state.Curve);
        }
        private static bool Nonnegative(double value) => StreetFollowPipeline.Finite(value) && value >= 0;
        private static bool Representable(double value) => StreetFollowPipeline.Finite(value) && Math.Abs(value) <= float.MaxValue;
    }
}
