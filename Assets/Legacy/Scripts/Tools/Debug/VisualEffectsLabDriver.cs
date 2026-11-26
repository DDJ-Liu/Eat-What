// VERIFY-TEMP: VisualEffectsLab 人工验收辅助。正式场景接入前不得删除或降级。
using System.Text;
using EatWhat.Tools.Rendering;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EatWhat.Tools.Debugging
{
    /// <summary>
    /// Optional DebugAndReferences driver. It consumes InputManager.OnKeyPressed;
    /// it does not poll a second global input path and does not contain game logic.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Eat What/Debug/Visual Effects Lab Driver")]
    public sealed class VisualEffectsLabDriver : MonoBehaviour
    {
        [Header("Explicit references")]
        [SerializeField] private EtherBubbleEmitter2D emitter = null;
        [SerializeField] private SceneColorCapture2D captureSource = null;
        [SerializeField] private TextMeshPro statusText = null;
        [SerializeField, Tooltip("Optional TransparentFX sample animated only while this probe is enabled.")]
        private Transform animatedSample = null;

        [Header("Debug sample animation")]
        [SerializeField, Range(0f, 8f)] private float sampleAngularSpeed = 25f;
        [SerializeField, Range(0f, 2f)] private float sampleBobAmplitude = 0.25f;

        private readonly StringBuilder statusBuilder = new StringBuilder(384);
        private bool subscribed;
        private bool capturedEmitterState;
        private bool originalAutoEmission;
        private bool originalPaused;
        private bool originalEffectEnabled;
        private EtherBubblePresetKind originalPreset;
        private Vector3 originalSamplePosition;
        private Quaternion originalSampleRotation;
        private float sampleTime;
        private float statusCountdown;

        public EtherBubbleEmitter2D Emitter { get { return emitter; } }
        public SceneColorCapture2D CaptureSource { get { return captureSource; } }

        [ContextMenu("Emit One Bubble")]
        public void EmitOne()
        {
            if (emitter != null) emitter.Emit();
            RefreshStatus();
        }

        [ContextMenu("Burst 8 Bubbles")]
        public void BurstEight()
        {
            if (emitter != null) emitter.Burst(8);
            RefreshStatus();
        }

        [ContextMenu("Burst 16 Bubbles")]
        public void BurstSixteen()
        {
            if (emitter != null) emitter.Burst(16);
            RefreshStatus();
        }

        [ContextMenu("Toggle Continuous Emission")]
        public void ToggleContinuousEmission()
        {
            if (emitter == null) return;
            if (emitter.AutoEmissionRunning) emitter.StopEmission();
            else emitter.StartEmission();
            RefreshStatus();
        }

        [ContextMenu("Toggle Pause")]
        public void TogglePause()
        {
            if (emitter == null) return;
            emitter.SetPaused(!emitter.Paused);
            RefreshStatus();
        }

        [ContextMenu("Toggle Distortion")]
        public void ToggleDistortion()
        {
            if (emitter == null) return;
            emitter.SetEffectEnabled(!emitter.EffectEnabled);
            RefreshStatus();
        }

        [ContextMenu("Apply Soft Preset")]
        public void ApplySoftPreset()
        {
            if (emitter != null) emitter.ApplyPreset(EtherBubblePresetKind.Soft);
            RefreshStatus();
        }

        [ContextMenu("Apply Obvious Preset")]
        public void ApplyObviousPreset()
        {
            if (emitter != null) emitter.ApplyPreset(EtherBubblePresetKind.Obvious);
            RefreshStatus();
        }

        [ContextMenu("Clear All Bubbles")]
        public void ClearAll()
        {
            if (emitter != null) emitter.Clear();
            RefreshStatus();
        }

        private void Reset()
        {
            enabled = false;
        }

        private void OnEnable()
        {
            CaptureOriginalState();
            Subscribe();
            sampleTime = 0f;
            statusCountdown = 0f;
            RefreshStatus();
        }

        private void Update()
        {
            AnimateSample(Time.unscaledDeltaTime);
            statusCountdown -= Time.unscaledDeltaTime;
            if (statusCountdown > 0f) return;
            statusCountdown = 0.2f;
            RefreshStatus();
        }

        private void OnDisable()
        {
            Unsubscribe();
            RestoreOriginalState();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            RestoreOriginalState();
        }

        private void OnValidate()
        {
            sampleAngularSpeed = Sanitize(sampleAngularSpeed, 0f, 8f, 0f);
            sampleBobAmplitude = Sanitize(sampleBobAmplitude, 0f, 2f, 0f);
        }

        private void Subscribe()
        {
            if (subscribed) return;
            InputManager.OnKeyPressed -= HandleKeyPressed;
            InputManager.OnKeyPressed += HandleKeyPressed;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed) return;
            InputManager.OnKeyPressed -= HandleKeyPressed;
            subscribed = false;
        }

        private bool HandleKeyPressed(Key key)
        {
            switch (key)
            {
                case Key.Digit1:
                    EmitOne();
                    return true;
                case Key.Digit8:
                    BurstEight();
                    return true;
                case Key.Digit9:
                    BurstSixteen();
                    return true;
                case Key.Space:
                    ToggleContinuousEmission();
                    return true;
                case Key.P:
                    TogglePause();
                    return true;
                case Key.D:
                    ToggleDistortion();
                    return true;
                case Key.F1:
                    ApplySoftPreset();
                    return true;
                case Key.F2:
                    ApplyObviousPreset();
                    return true;
                case Key.C:
                    ClearAll();
                    return true;
                default:
                    return false;
            }
        }

        private void CaptureOriginalState()
        {
            if (!capturedEmitterState && emitter != null)
            {
                originalAutoEmission = emitter.AutoEmissionRunning;
                originalPaused = emitter.Paused;
                originalEffectEnabled = emitter.EffectEnabled;
                originalPreset = emitter.CurrentPreset;
                capturedEmitterState = true;
            }

            if (animatedSample != null)
            {
                originalSamplePosition = animatedSample.localPosition;
                originalSampleRotation = animatedSample.localRotation;
            }
        }

        private void RestoreOriginalState()
        {
            if (capturedEmitterState && emitter != null)
            {
                emitter.Clear();
                emitter.ApplyPreset(originalPreset);
                emitter.SetEffectEnabled(originalEffectEnabled);
                emitter.SetPaused(originalPaused);
                if (originalAutoEmission) emitter.StartEmission();
                else emitter.StopEmission();
            }
            capturedEmitterState = false;

            if (animatedSample != null)
            {
                animatedSample.localPosition = originalSamplePosition;
                animatedSample.localRotation = originalSampleRotation;
            }
        }

        private void AnimateSample(float deltaTime)
        {
            if (animatedSample == null) return;
            var safeDeltaTime = Sanitize(deltaTime, 0f, 1f, 0f);
            sampleTime += safeDeltaTime;
            animatedSample.localPosition = originalSamplePosition +
                Vector3.up * (Mathf.Sin(sampleTime * 1.7f) * sampleBobAmplitude);
            animatedSample.localRotation = originalSampleRotation *
                Quaternion.Euler(0f, 0f, sampleTime * sampleAngularSpeed);
        }

        private void RefreshStatus()
        {
            if (statusText == null) return;
            statusBuilder.Length = 0;
            statusBuilder.AppendLine("ETHER BUBBLE LAB · VERIFY-TEMP");
            statusBuilder.Append("Capture: ");
            statusBuilder.Append(captureSource != null && captureSource.IsFrameValid ? "VALID" : "WAITING");
            if (captureSource != null)
            {
                statusBuilder.Append("  RT=");
                statusBuilder.Append(captureSource.CaptureWidth);
                statusBuilder.Append('x');
                statusBuilder.Append(captureSource.CaptureHeight);
                statusBuilder.Append(" owned=");
                statusBuilder.Append(captureSource.OwnedRenderTextureCount);
            }
            statusBuilder.AppendLine();

            if (emitter == null)
            {
                statusBuilder.AppendLine("Emitter: MISSING");
            }
            else
            {
                statusBuilder.Append("Bubbles: ");
                statusBuilder.Append(emitter.ActiveCount);
                statusBuilder.Append('/');
                statusBuilder.Append(emitter.MaxBubbles);
                statusBuilder.Append("  pool=");
                statusBuilder.Append(emitter.PoolCount);
                statusBuilder.Append("  preset=");
                statusBuilder.Append(emitter.CurrentPreset);
                statusBuilder.Append("  distortion=");
                statusBuilder.Append(emitter.EffectEnabled ? "ON" : "OFF");
                statusBuilder.Append("  paused=");
                statusBuilder.AppendLine(emitter.Paused ? "YES" : "NO");
            }

            statusBuilder.Append("1=single  8=burst8  9=burst16  Space=emit  P=pause  D=effect  F1/F2=preset  C=clear");
            statusText.text = statusBuilder.ToString();
        }

        private static float Sanitize(float value, float minimum, float maximum, float fallback)
        {
            return float.IsNaN(value) || float.IsInfinity(value)
                ? fallback
                : Mathf.Clamp(value, minimum, maximum);
        }
    }
}
