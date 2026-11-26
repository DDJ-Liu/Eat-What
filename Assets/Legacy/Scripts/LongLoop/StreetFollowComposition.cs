using System;
using UnityEngine;

namespace EatWhat.LongLoop
{
    // Lens/normalized channel snapshots. The caller supplies its preceding lens
    // ratio; composition does not own another persistent state.
    public struct StreetFollowCompositionInput
    {
        public bool DynamicZoomEnabled;
        public float Size, MaxSize, PreviousRatio, VerticalOffset;
        public Vector2 LookFloat, Interest;
        // Opt-in already scaled/limited world contribution. Legacy callers stay normalized.
        public bool LookFloatIsWorld;
        public Vector2 WorldLookFloat;
    }

    public struct StreetFollowCompositionResult
    {
        public bool Valid;
        public float Ratio;
        public StreetFollowSettings EffectiveSettings;
        public StreetFollowResult Follow;
    }

    public static class StreetFollowComposition
    {
        public static StreetFollowCompositionResult Evaluate(StreetFollowState previous,
            StreetFollowInput input, StreetFollowSettings settings, FollowClipContext clip,
            StreetFollowCompositionInput frame)
        {
            var r = new StreetFollowCompositionResult {
                Follow = new StreetFollowResult { State = previous, Target = previous.Target } };
            if (!StreetFollowPipeline.Valid(settings) || !Positive(frame.Size) || !Positive(clip.Aspect) ||
                (frame.DynamicZoomEnabled && !Positive(frame.MaxSize)) ||
                !Unit(frame.VerticalOffset) || !Unit(frame.LookFloat.x) || !Unit(frame.LookFloat.y) ||
                !StreetFollowPipeline.Finite(frame.Interest) ||
                (frame.LookFloatIsWorld && !StreetFollowPipeline.Finite(frame.WorldLookFloat)) ||
                (previous.Initialized && !Positive(frame.PreviousRatio))) return r;
            double ratio = frame.DynamicZoomEnabled ? (double)frame.Size / frame.MaxSize : 1;
            double width = 2d * frame.Size * clip.Aspect, height = 2d * frame.Size;
            if (!Positive(ratio) || !Representable(ratio) || !Positive((float)ratio) ||
                !Representable(width) || !Representable(height)) return r;
            // Store/compare the same representable ratio the caller receives.
            ratio = (float)ratio;
            var effective = settings;
            if (!Scale(settings.Lookahead, ratio, out effective.Lookahead) ||
                !Scale(settings.MaxHorizontalOffset, ratio, out effective.MaxHorizontalOffset) ||
                !Scale(settings.MaxVerticalOffset, ratio, out effective.MaxVerticalOffset)) return r;
            input.VisibleSize = new Vector2(2 * frame.Size * clip.Aspect, 2 * frame.Size);
            clip.OrthoSize = frame.Size;
            r.Ratio = (float)ratio; r.EffectiveSettings = effective;
            // Preserve the existing inactive path, including its exact arithmetic.
            if (!frame.DynamicZoomEnabled && frame.VerticalOffset == 0 &&
                frame.LookFloat.x == 0 && frame.LookFloat.y == 0 &&
                (!frame.LookFloatIsWorld || frame.WorldLookFloat.x == 0 && frame.WorldLookFloat.y == 0) &&
                frame.Interest.x == 0 && frame.Interest.y == 0 &&
                (!previous.Initialized || frame.PreviousRatio == 1))
            {
                r.Follow = StreetFollowPipeline.Evaluate(previous, input, settings, clip);
                r.Valid = r.Follow.Valid; return r;
            }

            var rebased = previous;
            if (previous.Initialized && ratio != frame.PreviousRatio &&
                !Rebase(ref rebased.Lookahead, previous.PreviousFoot.x, ratio / frame.PreviousRatio, settings)) return r;
            var f = r.Follow;
            f.State = rebased;
            f.GoalStep = StreetFollowPipeline.StepGoal(rebased, input, settings);
            if (f.GoalStep.Status == FollowStepStatus.InvalidInput) return r;
            if (f.GoalStep.Status == FollowStepStatus.HeldTime)
            { f.Valid = true; r.Follow = f; r.Valid = true; return r; }
            var next = f.GoalStep.State;
            // Actual front-state distance is scaled before advancing the existing
            // latch/carry/exit rules. Speed, time and dead-zone ratios are unchanged.
            f.Horizontal = StreetFollowPipeline.Horizontal(rebased.Lookahead, f.GoalStep, effective);
            next.Lookahead = f.Horizontal.State;
            double baseX = next.Goal.x + f.Horizontal.BaseOffset;
            double fx = frame.LookFloatIsWorld ? frame.WorldLookFloat.x : frame.LookFloat.x * (double)effective.MaxHorizontalOffset;
            double fy = frame.LookFloatIsWorld ? frame.WorldLookFloat.y : frame.LookFloat.y * (double)effective.MaxVerticalOffset;
            double vy = frame.VerticalOffset * (double)effective.MaxVerticalOffset;
            if (!Representable(baseX) || !Representable(fx) || !Representable(fy) || !Representable(vy) ||
                !ValidLookahead(next.Lookahead)) return r;
            var reference = new Vector3((float)baseX, next.Goal.y, next.Goal.z);
            clip.Foot = input.Foot; clip.TargetReference = reference;
            f.Channels = StreetFollowPipeline.SumAndClip(clip, new FollowContributions {
                Horizontal = new Vector2(f.Horizontal.Offset, 0),
                Vertical = new Vector2(0, (float)vy), LookFloat = new Vector2((float)fx, (float)fy),
                Interest = frame.Interest }, effective.MaxHorizontalOffset, effective.MaxVerticalOffset);
            if (!f.Channels.Valid) return r;
            var target = reference + new Vector3(f.Channels.ClippedSum.x, f.Channels.ClippedSum.y, 0);
            if (!StreetFollowPipeline.Finite(target)) return r;
            // Only Target receives the complete output. LastTarget/Carry/Exit stay
            // front-owned, so float/vertical/interest cannot feed back next frame.
            next.Target = target;
            f.State = next; f.Target = target; f.Valid = true;
            r.Follow = f; r.Valid = true; return r;
        }

        private static bool Rebase(ref FollowLookaheadState h, double origin, double factor, StreetFollowSettings settings)
        {
            if (!ValidLookahead(h) || !Positive(factor)) return false;
            h.LastTarget = origin + (h.LastTarget - origin) * factor;
            h.Carry *= factor;
            if (h.Exiting && h.Exit.Active && h.Exit.Time < h.Exit.Duration)
            {
                // Reproject the existing segment, preserving elapsed seconds and
                // its start weight. Recompute geometry with the original speed
                // cap/profile; a lens ramp must not restart the acceleration.
                var old = h.Exit;
                double start = origin + (old.Start - origin) * factor;
                double target = origin + (old.Target - origin) * factor;
                if (settings.RecenterSpeed > 0)
                {
                    h.Exit = StreetFollowPipeline.CreateRecenterAxis(start, target, settings);
                    h.Exit.Time = Math.Min(old.Time, h.Exit.Duration);
                }
                else { h.Exit.Start = start; h.Exit.Target = target; }
            }
            return ValidLookahead(h);
        }
        private static bool ValidLookahead(FollowLookaheadState h)
            => StreetFollowPipeline.Finite(h.LastTarget) && StreetFollowPipeline.Finite(h.Carry) &&
                StreetFollowPipeline.Finite(h.SignedWeight) && Math.Abs(h.SignedWeight) <= 1 &&
                StreetFollowPipeline.Finite(h.ExitStartWeight) &&
                StreetFollowPipeline.Finite(h.Exit.Start) && StreetFollowPipeline.Finite(h.Exit.Target) &&
                StreetFollowPipeline.Finite(h.Exit.Time) && StreetFollowPipeline.Finite(h.Exit.Peak);
        private static bool Scale(float value, double ratio, out float scaled)
        {
            double result = value * ratio; scaled = (float)result;
            return StreetFollowPipeline.Finite(value) && value >= 0 && Representable(result);
        }
        private static bool Positive(double value) => StreetFollowPipeline.Finite(value) && value > 0;
        private static bool Unit(double value) => StreetFollowPipeline.Finite(value) && Math.Abs(value) <= 1;
        private static bool Representable(double value) => StreetFollowPipeline.Finite(value) && Math.Abs(value) <= float.MaxValue;
    }
}
