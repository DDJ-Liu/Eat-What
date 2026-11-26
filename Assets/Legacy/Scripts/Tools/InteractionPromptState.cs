using System;
using EatWhat.Toolkit.Input;

namespace EatWhat.Tools
{
    /// <summary>One prompt's target/text/fade state. Localization stays in the existing LocService.</summary>
    public sealed class InteractionPromptState
    {
        readonly double fadeIn, fadeOut;
        readonly string keyboardCaption, gamepadCaption;
        double elapsed, duration, start, goal;
        public InteractionPromptState(double fadeIn, double fadeOut, string keyboardCaption, string gamepadCaption)
        {
            CheckSeconds(fadeIn); CheckSeconds(fadeOut);
            this.fadeIn = fadeIn; this.fadeOut = fadeOut;
            this.keyboardCaption = keyboardCaption ?? ""; this.gamepadCaption = gamepadCaption ?? "";
        }
        public object Target { get; private set; }
        public string LocalizationKey { get; private set; } = "";
        public string Text { get; private set; } = "";
        public float Alpha { get; private set; }
        public bool WantsVisible { get; private set; }
        public GameInputDevice Device { get; private set; }
        public string KeyCaption { get { return Device == GameInputDevice.Gamepad ? gamepadCaption : keyboardCaption; } }

        public void Show(object target, string localizationKey)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            string key = localizationKey ?? "";
            bool unchanged = ReferenceEquals(target, Target) && key == LocalizationKey && WantsVisible;
            Target = target; LocalizationKey = key; Text = LocService.Get(key); WantsVisible = true;
            if (!unchanged) FadeTo(1, fadeIn);
        }
        public void Hide()
        { if (WantsVisible) { WantsVisible = false; FadeTo(0, fadeOut); } }
        public void Reset()
        { Target = null; LocalizationKey = Text = ""; Alpha = 0; WantsVisible = false; elapsed = duration = start = goal = 0; }
        void FadeTo(double next, double seconds)
        {
            start = Alpha; goal = next; duration = seconds; elapsed = 0;
            if (seconds == 0 || Alpha == goal) { Alpha = (float)goal; if (goal == 0) Reset(); }
        }
        public void Advance(double seconds, GameInputDevice device)
        {
            CheckSeconds(seconds);
            if (device != GameInputDevice.Keyboard && device != GameInputDevice.Gamepad) throw new ArgumentOutOfRangeException(nameof(device));
            Device = device;
            if (Target != null) Text = LocService.Get(LocalizationKey);
            if (Alpha == goal || seconds == 0) return;
            elapsed = seconds >= duration - elapsed ? duration : elapsed + seconds;
            Alpha = elapsed == duration ? (float)goal : (float)(start + (goal - start) * elapsed / duration);
            if (elapsed == duration && goal == 0) Reset();
        }
        static void CheckSeconds(double value)
        { if (double.IsNaN(value) || double.IsInfinity(value) || value < 0) throw new ArgumentOutOfRangeException("seconds"); }
    }
}
