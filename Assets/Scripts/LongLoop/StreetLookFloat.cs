using System;
using EatWhat.LongLoop.Configuration;
using UnityEngine;

namespace EatWhat.LongLoop
{
    public struct StreetLookFloatSettings
    {
        public bool Enabled;
        public float EdgeZoneRatio, InTime, OutTime;
        public StreetCurvePreset ResponseCurve;
    }

    public struct StreetLookFloatInput
    {
        public Vector2 LookAxis;
        public bool Gated;
        public bool DirectAxis;
        public double DeltaTime;
    }

    public struct StreetLookFloatAxisState
    {
        public bool Initialized, Entering;
        public double Value, Start, Target, Elapsed, Duration;
    }

    // The driver owns these independent value states. No scene/input objects.
    public struct StreetLookFloatState
    {
        public StreetLookFloatAxisState X, Y;
    }

    public struct StreetLookFloatResult
    {
        public bool Valid;
        public Vector2 Value, Target;
        public StreetLookFloatState State;
    }

    public struct StreetLookFloatWorldState { public Vector2 Value; }
    public struct StreetLookFloatWorldResult
    {
        public bool Valid, DistanceCorrected;
        public Vector2 Value;
        public StreetLookFloatWorldState State;
    }

    public static class StreetLookFloat
    {
        // Explicit world-unit boundary; legacy normalized Evaluate is unchanged.
        public static StreetLookFloatWorldResult LimitWorld(StreetLookFloatWorldState previous,
            Vector2 normalized, Vector2 axisLimits, float ratio, float maxDistance, float maxSpeed, double dt)
        {
            var r = new StreetLookFloatWorldResult { State = previous };
            if (!StreetFollowPipeline.Finite(previous.Value) || !Unit(normalized.x) || !Unit(normalized.y) ||
                !Nonnegative(axisLimits.x) || !Nonnegative(axisLimits.y) || !Nonnegative(maxDistance) ||
                !Nonnegative(maxSpeed) || !Nonnegative(dt) || !StreetFollowPipeline.Finite(ratio) || ratio <= 0) return r;
            double x = previous.Value.x, y = previous.Value.y;
            double length = Math.Sqrt(x*x + y*y);
            if (length > maxDistance)
            {
                double scale = maxDistance / length;
                x *= scale; y *= scale; r.DistanceCorrected = true;
            }
            double tx = normalized.x * (double)axisLimits.x * ratio;
            double ty = normalized.y * (double)axisLimits.y * ratio;
            length = Math.Sqrt(tx*tx + ty*ty);
            if (length > maxDistance) { double scale = maxDistance / length; tx *= scale; ty *= scale; }
            double dx = tx-x, dy = ty-y, distance = Math.Sqrt(dx*dx + dy*dy);
            if (dt > 0 && maxSpeed > 0 && distance > 0)
            {
                double fraction = Math.Min(1, maxSpeed * dt / distance);
                x += dx * fraction; y += dy * fraction;
            }
            r.Value = new Vector2((float)x, (float)y);
            r.State = new StreetLookFloatWorldState { Value = r.Value };
            r.Valid = StreetFollowPipeline.Finite(r.Value);
            return r;
        }

        public static bool Valid(StreetLookFloatSettings settings)
        {
            return StreetFollowPipeline.Finite(settings.EdgeZoneRatio) &&
                settings.EdgeZoneRatio > 0 && settings.EdgeZoneRatio <= .5f &&
                Nonnegative(settings.InTime) && Nonnegative(settings.OutTime) &&
                StreetFollowPipeline.CurveDefined(settings.ResponseCurve);
        }

        public static StreetLookFloatResult Evaluate(StreetLookFloatState previous,
            StreetLookFloatInput input, StreetLookFloatSettings settings)
        {
            var result = new StreetLookFloatResult { State = previous };
            if (!Valid(settings) || !StreetFollowPipeline.Finite(input.LookAxis) ||
                !Nonnegative(input.DeltaTime) || !ValidAxis(previous.X) || !ValidAxis(previous.Y)) return result;
            bool neutral = !settings.Enabled || input.Gated;
            Vector2 direct = input.DirectAxis && !neutral ? DirectTarget(input.LookAxis,settings) : Vector2.zero;
            double x = neutral ? 0 : input.DirectAxis ? direct.x : Target(input.LookAxis.x, settings);
            double y = neutral ? 0 : input.DirectAxis ? direct.y : Target(input.LookAxis.y, settings);
            var next = new StreetLookFloatState {
                X = StepAxis(previous.X, x, input.DeltaTime, settings),
                Y = StepAxis(previous.Y, y, input.DeltaTime, settings) };
            result.State = next;
            result.Value = new Vector2((float)next.X.Value, (float)next.Y.Value);
            result.Target = new Vector2((float)x, (float)y);
            result.Valid = true;
            return result;
        }

        private static Vector2 DirectTarget(Vector2 axis, StreetLookFloatSettings settings)
        {
            double length=Math.Sqrt((double)axis.x*axis.x+(double)axis.y*axis.y);
            if(length==0) return Vector2.zero;
            double scale=StreetFollowPipeline.CurveValue(settings.ResponseCurve,Math.Min(1,length))/length;
            return new Vector2((float)(axis.x*scale),(float)(axis.y*scale));
        }
        private static double Target(double axis, StreetLookFloatSettings settings)
        {
            // Ratio is a fraction of the whole visible dimension, from each edge.
            // .25 therefore leaves the normalized central interval [-.5, .5].
            double depth = 1 - (1 - Math.Min(1, Math.Abs(axis))) / (2d * settings.EdgeZoneRatio);
            if (depth <= 0) return 0;
            return Math.Sign(axis) * StreetFollowPipeline.CurveValue(settings.ResponseCurve, depth);
        }

        private static StreetLookFloatAxisState StepAxis(StreetLookFloatAxisState previous,
            double target, double dt, StreetLookFloatSettings settings)
        {
            var next = previous.Initialized ? previous : new StreetLookFloatAxisState();
            bool changed = !next.Initialized || next.Target != target;
            // Magnitude, not signed numerical order: left/right and up/down match.
            bool entering = changed ? Math.Abs(target) > Math.Abs(next.Value) : next.Entering;
            double duration = entering ? settings.InTime : settings.OutTime;
            if (changed || next.Duration != duration)
            {
                next.Initialized = true; next.Entering = entering;
                next.Start = next.Value; next.Target = target;
                next.Elapsed = 0; next.Duration = duration;
            }
            if (dt > 0)
            {
                next.Elapsed = Math.Min(next.Duration, next.Elapsed + dt);
                next.Value = next.Duration == 0 || next.Elapsed >= next.Duration ? next.Target :
                    next.Start + (next.Target - next.Start) * (next.Elapsed / next.Duration);
            }
            return next;
        }

        private static bool ValidAxis(StreetLookFloatAxisState state)
        {
            return !state.Initialized || (Unit(state.Value) && Unit(state.Start) && Unit(state.Target) &&
                Nonnegative(state.Elapsed) && Nonnegative(state.Duration) && state.Elapsed <= state.Duration);
        }
        private static bool Nonnegative(double value) => StreetFollowPipeline.Finite(value) && value >= 0;
        private static bool Unit(double value) => StreetFollowPipeline.Finite(value) && Math.Abs(value) <= 1;
    }
}
