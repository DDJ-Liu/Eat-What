using System;

namespace EatWhat.Narrative.Core
{
    public enum NarrativeTypingClickAction { CompleteLine, SkipToNext }

    // A caller-supplied context, never stored as dialogue progress by the input model.
    public enum NarrativeAdvancePhase { Inactive, Entering, Typing, LineComplete }
    public enum NarrativeAdvanceIntent { None, CompleteLine, SkipToNext, SkipEntrance }

    /// <summary>Immutable input settings, in seconds; no configuration asset is created.</summary>
    public sealed class NarrativeAdvanceSettings
    {
        public NarrativeAdvanceSettings(
            NarrativeTypingClickAction clickWhileTyping = NarrativeTypingClickAction.CompleteLine,
            NarrativeTypingClickAction clickWhileTypingFast = NarrativeTypingClickAction.SkipToNext,
            double autoWait = 1.2, double advanceCooldown = 0.3, double cgAdvanceCooldown = 1.5)
        {
            CheckAction(clickWhileTyping, nameof(clickWhileTyping));
            CheckAction(clickWhileTypingFast, nameof(clickWhileTypingFast));
            CheckDuration(autoWait, nameof(autoWait));
            CheckDuration(advanceCooldown, nameof(advanceCooldown));
            CheckDuration(cgAdvanceCooldown, nameof(cgAdvanceCooldown));
            ClickWhileTyping = clickWhileTyping;
            ClickWhileTypingFast = clickWhileTypingFast;
            AutoWait = autoWait;
            AdvanceCooldown = advanceCooldown;
            CgAdvanceCooldown = cgAdvanceCooldown;
        }

        public NarrativeTypingClickAction ClickWhileTyping { get; private set; }
        public NarrativeTypingClickAction ClickWhileTypingFast { get; private set; }
        public double AutoWait { get; private set; }
        public double AdvanceCooldown { get; private set; }
        public double CgAdvanceCooldown { get; private set; }

        private static void CheckAction(NarrativeTypingClickAction value, string name)
        {
            if (value != NarrativeTypingClickAction.CompleteLine && value != NarrativeTypingClickAction.SkipToNext)
                throw new ArgumentOutOfRangeException(name);
        }
        private static void CheckDuration(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
                throw new ArgumentOutOfRangeException(name, "Expected a finite nonnegative duration in seconds.");
        }
    }

    /// <summary>
    /// Owns input toggles, click cooldown deadlines and one line's completion wait only.
    /// The caller owns dialogue progress, applies returned intents, and supplies monotonic seconds.
    /// No real device input, text rendering, scheduler, scene, or global clock is accessed.
    /// </summary>
    public sealed class NarrativeAdvanceInputModel
    {
        private double? manualBlockedUntil, cgBlockedUntil, lineCompletedAt;
        private bool lineWaitConsumed;

        public NarrativeAdvanceInputModel(NarrativeAdvanceSettings settings = null)
        {
            Settings = settings ?? new NarrativeAdvanceSettings();
        }

        public NarrativeAdvanceSettings Settings { get; private set; }
        public bool AutoEnabled { get; private set; }
        public bool FastForwardEnabled { get; private set; }

        // Rebind a resolved segment's immutable parameters without moving/resetting this model's state.
        public void ConfigureSettings(NarrativeAdvanceSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            Settings = settings;
        }

        public void SetAuto(bool enabled) { AutoEnabled = enabled; }
        public void SetFastForward(bool enabled) { FastForwardEnabled = enabled; }
        public void ToggleAuto() { AutoEnabled = !AutoEnabled; }
        public void ToggleFastForward() { FastForwardEnabled = !FastForwardEnabled; }

        // The caller reports the actual beginning of each new line, not merely a click intention.
        // Cooldowns and both toggles carry over; only this line's AUTO waiting information is reset.
        public void NotifyLineStarted() { lineCompletedAt = null; lineWaitConsumed = false; }

        // Repeated completion notifications for the same line do not postpone its waiting deadline.
        public void NotifyLineCompleted(double nowSeconds)
        {
            CheckTime(nowSeconds);
            if (!lineWaitConsumed && !lineCompletedAt.HasValue) lineCompletedAt = nowSeconds;
        }

        // Call for every CG appearance, hide, or switch. It refreshes only the CG click deadline.
        public void NotifyCgChanged(double nowSeconds)
        {
            CheckTime(nowSeconds);
            cgBlockedUntil = nowSeconds + Settings.CgAdvanceCooldown;
        }

        public NarrativeAdvanceIntent TryClick(NarrativeAdvancePhase phase, double nowSeconds)
        {
            CheckTime(nowSeconds);
            if ((manualBlockedUntil.HasValue && nowSeconds < manualBlockedUntil.Value) ||
                (cgBlockedUntil.HasValue && nowSeconds < cgBlockedUntil.Value))
                return NarrativeAdvanceIntent.None;

            NarrativeAdvanceIntent intent;
            switch (phase)
            {
                case NarrativeAdvancePhase.Entering:
                    intent = NarrativeAdvanceIntent.SkipEntrance;
                    break;
                case NarrativeAdvancePhase.Typing:
                    var action = FastForwardEnabled ? Settings.ClickWhileTypingFast : Settings.ClickWhileTyping;
                    intent = action == NarrativeTypingClickAction.CompleteLine
                        ? NarrativeAdvanceIntent.CompleteLine : NarrativeAdvanceIntent.SkipToNext;
                    break;
                case NarrativeAdvancePhase.LineComplete:
                    intent = NarrativeAdvanceIntent.SkipToNext;
                    break;
                default: return NarrativeAdvanceIntent.None;
            }
            manualBlockedUntil = nowSeconds + Settings.AdvanceCooldown;
            if (intent != NarrativeAdvanceIntent.CompleteLine) lineCompletedAt = null;
            if (intent == NarrativeAdvanceIntent.SkipToNext) lineWaitConsumed = true;
            return intent;
        }

        public NarrativeAdvanceIntent EvaluateAuto(NarrativeAdvancePhase phase, double nowSeconds)
        {
            CheckTime(nowSeconds);
            if (!AutoEnabled || phase != NarrativeAdvancePhase.LineComplete || !lineCompletedAt.HasValue ||
                nowSeconds < lineCompletedAt.Value + Settings.AutoWait)
                return NarrativeAdvanceIntent.None;
            // One intent consumes the wait; the caller must report the next line's lifecycle.
            lineCompletedAt = null;
            lineWaitConsumed = true;
            return NarrativeAdvanceIntent.SkipToNext;
        }

        private static void CheckTime(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentOutOfRangeException(nameof(value), "Expected finite monotonic seconds from the caller.");
        }
    }
}
