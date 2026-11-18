using System;
using System.Collections.Generic;
using EatWhat.Narrative.Core;
using UnityEngine;

namespace EatWhat.Narrative.Unity
{
    // Street code supplies a head; the Unity view has no knowledge of street actors or input/cameras.
    public interface INarrativeBubbleSink
    {
        bool Show(string npcId, Transform head, string textKey, DialogueGroupSnapshot tier, double nowSeconds);
    }
    public interface INarrativeBubbleSurface : IDisposable
    {
        bool Apply(float alpha, float risePixels);
    }
    public interface INarrativeBubbleSurfaceFactory
    {
        INarrativeBubbleSurface Create(Transform head, string textKey, string text, out string diagnostic);
    }

    /// <summary>Owns each displayed bubble's lifetime. Surface creation/projection is the external UI boundary.</summary>
    [DisallowMultipleComponent]
    public sealed class NarrativeBubbleView : MonoBehaviour, INarrativeBubbleSink
    {
        [SerializeField, Tooltip("Component implementing INarrativeBubbleSurfaceFactory, normally NarrativeBubbleCanvasRenderer.")]
        private MonoBehaviour surfaceProvider = null;
        private INarrativeBubbleSurfaceFactory configuredFactory;
        private readonly List<Bubble> bubbles = new List<Bubble>();
        private readonly List<string> diagnostics = new List<string>();
        private double lastTime = double.NegativeInfinity;
        public int ActiveCount => bubbles.Count;
        public IReadOnlyList<string> Diagnostics => diagnostics.AsReadOnly();

        private sealed class Bubble
        {
            public string Owner;
            public INarrativeBubbleSurface Surface;
            public double Start, FadeIn, Lifetime, FadeOut, ExitAt;
            public float Rise, ExitAlpha;
            public bool Exiting;
        }

        public void ConfigureSurfaceFactory(INarrativeBubbleSurfaceFactory factory)
        {
            Clear();
            configuredFactory = factory;
        }

        public bool Show(string npcId, Transform head, string textKey, DialogueGroupSnapshot tier, double nowSeconds)
        {
            if (!isActiveAndEnabled || string.IsNullOrEmpty(npcId) || head == null || tier == null ||
                !Finite(nowSeconds) || nowSeconds < lastTime) return Diagnose("INVALID_REQUEST: Missing owner/head/tier or invalid clock.");
            float fade = tier.Get<float>("bubbleFadeInTime"), life = tier.Get<float>("bubbleLifetime"),
                rise = tier.Get<float>("bubbleExitRise"), exit = tier.Get<float>("bubbleExitFade");
            if (!Nonnegative(fade) || !Nonnegative(life) || !Nonnegative(rise) || !Nonnegative(exit))
                return Diagnose("INVALID_TIER: Bubble fields must be finite and nonnegative.");
            string text = string.IsNullOrWhiteSpace(textKey) ? null : LocService.Get(textKey);
            if (string.IsNullOrWhiteSpace(text) || text == "#" + textKey + "#")
                return Diagnose("MISSING_LOCALIZATION: " + textKey);
            var factory = configuredFactory ?? surfaceProvider as INarrativeBubbleSurfaceFactory;
            if (factory == null) return Diagnose("MISSING_SURFACE_PROVIDER: Assign the bubble Canvas renderer.");
            string issue;
            var surface = factory.Create(head, textKey, text, out issue);
            if (surface == null) return Diagnose("SURFACE_UNAVAILABLE: " + issue);
            Tick(nowSeconds);
            // A second replacement must not restart an already exiting bubble's deadline.
            foreach (var old in bubbles)
                if (old.Owner == npcId && !old.Exiting) BeginExit(old, nowSeconds, Alpha(old, nowSeconds));
            bubbles.Add(new Bubble { Owner = npcId, Surface = surface, Start = nowSeconds,
                FadeIn = fade, Lifetime = life, FadeOut = exit, Rise = rise });
            Tick(nowSeconds);
            return true;
        }

        public void Tick(double nowSeconds)
        {
            if (!Finite(nowSeconds) || nowSeconds < lastTime) return;
            lastTime = nowSeconds;
            for (int i = bubbles.Count - 1; i >= 0; i--)
            {
                var bubble = bubbles[i];
                double naturalExit = bubble.Start + bubble.FadeIn + bubble.Lifetime;
                if (!bubble.Exiting && nowSeconds >= naturalExit) BeginExit(bubble, naturalExit, 1f);
                double progress = bubble.Exiting ? Fraction(nowSeconds - bubble.ExitAt, bubble.FadeOut) : 0;
                if ((bubble.Exiting && progress >= 1) ||
                    !bubble.Surface.Apply(Alpha(bubble, nowSeconds), (float)progress * bubble.Rise))
                {
                    bubble.Surface.Dispose(); bubbles.RemoveAt(i);
                }
            }
        }
        private static void BeginExit(Bubble bubble, double at, float alpha)
        { bubble.Exiting = true; bubble.ExitAt = at; bubble.ExitAlpha = alpha; }
        private static float Alpha(Bubble bubble, double now)
        { return bubble.Exiting ? bubble.ExitAlpha * (1f - (float)Fraction(now - bubble.ExitAt, bubble.FadeOut))
            : (float)Fraction(now - bubble.Start, bubble.FadeIn); }
        private static double Fraction(double elapsed, double duration)
        { return duration <= 0 ? 1 : Math.Max(0, Math.Min(1, elapsed / duration)); }
        private static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
        private static bool Nonnegative(float value) { return Finite(value) && value >= 0; }
        private bool Diagnose(string issue)
        {
            if (!diagnostics.Contains(issue)) { diagnostics.Add(issue); Debug.LogWarning("NarrativeBubbleView: " + issue, this); }
            return false;
        }
        public void Clear()
        {
            foreach (var bubble in bubbles) bubble.Surface.Dispose();
            bubbles.Clear(); lastTime = double.NegativeInfinity;
        }
        private void LateUpdate() { Tick(Time.timeAsDouble); }
        private void OnDisable() { Clear(); }
        private void OnDestroy() { Clear(); }
    }
}
