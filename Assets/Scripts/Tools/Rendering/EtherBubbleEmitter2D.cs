using System.Collections.Generic;
using UnityEngine;

namespace EatWhat.Tools.Rendering
{
    public enum EtherBubblePresetKind
    {
        Soft,
        Obvious
    }

    /// <summary>Explicitly wired, bounded pool for the EtherBubble effect.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Eat What/Rendering/Ether Bubble Emitter 2D")]
    public sealed class EtherBubbleEmitter2D : MonoBehaviour
    {
        private const int MinimumBubbleCount = 1;
        private const int MaximumBubbleCount = 64;

        [Header("Explicit references")]
        [SerializeField] private Transform mouthAnchor = null;
        [SerializeField] private Transform spawnParent = null;
        [SerializeField] private EtherBubbleDistortion2D bubblePrefab = null;
        [SerializeField] private SceneColorCapture2D captureSource = null;

        [Header("Emission")]
        [SerializeField, Range(MinimumBubbleCount, MaximumBubbleCount)] private int maxBubbles = 16;
        [SerializeField, Range(0.05f, 10f)] private float emissionInterval = 0.7f;
        [SerializeField] private bool autoEmitOnEnable = true;

        [Header("Motion and lifetime")]
        [SerializeField, Range(0.05f, 20f)] private float bubbleRadius = 0.8f;
        [SerializeField] private Vector2 movementVelocity = new Vector2(1.4f, 0.8f);
        [SerializeField, Range(0.05f, 120f)] private float bubbleLifetime = 5f;

        [Header("Visual presets")]
        [SerializeField] private EtherBubbleVisualSettings softPreset = default(EtherBubbleVisualSettings);
        [SerializeField] private EtherBubbleVisualSettings obviousPreset = default(EtherBubbleVisualSettings);
        [SerializeField] private EtherBubblePresetKind initialPreset = EtherBubblePresetKind.Soft;

        private readonly List<EtherBubbleDistortion2D> pool = new List<EtherBubbleDistortion2D>(16);
        private EtherBubbleVisualSettings currentVisualSettings;
        private EtherBubblePresetKind currentPreset;
        private bool autoEmissionRunning;
        private bool paused;
        private bool effectEnabled = true;
        private float emissionCountdown;
        private int emittedSequence;

        public Transform MouthAnchor { get { return mouthAnchor; } }
        public Transform SpawnParent { get { return spawnParent; } }
        public EtherBubbleDistortion2D BubblePrefab { get { return bubblePrefab; } }
        public SceneColorCapture2D CaptureSource { get { return captureSource; } }
        public int MaxBubbles { get { return maxBubbles; } }
        public float EmissionInterval { get { return emissionInterval; } }
        public int PoolCount { get { return pool.Count; } }
        public bool AutoEmissionRunning { get { return autoEmissionRunning; } }
        public bool Paused { get { return paused; } }
        public bool EffectEnabled { get { return effectEnabled; } }
        public EtherBubblePresetKind CurrentPreset { get { return currentPreset; } }
        public EtherBubbleVisualSettings CurrentVisualSettings { get { return currentVisualSettings; } }
        public bool HasRequiredReferences
        {
            get
            {
                return mouthAnchor != null && spawnParent != null && bubblePrefab != null &&
                    captureSource != null;
            }
        }

        public int ActiveCount
        {
            get
            {
                var count = 0;
                for (var index = 0; index < pool.Count; index++)
                {
                    if (pool[index] != null && pool[index].gameObject.activeSelf) count++;
                }
                return count;
            }
        }

        public void Configure(
            Transform configuredMouthAnchor,
            Transform configuredSpawnParent,
            EtherBubbleDistortion2D configuredBubblePrefab,
            SceneColorCapture2D configuredCaptureSource)
        {
            mouthAnchor = configuredMouthAnchor;
            spawnParent = configuredSpawnParent;
            bubblePrefab = configuredBubblePrefab;
            captureSource = configuredCaptureSource;
        }

        public void SetMaxBubbles(int value)
        {
            maxBubbles = Mathf.Clamp(value, MinimumBubbleCount, MaximumBubbleCount);
            ReleaseAboveLimit();
        }

        public void StartEmission()
        {
            autoEmissionRunning = true;
            emissionCountdown = 0f;
        }

        public void StopEmission()
        {
            autoEmissionRunning = false;
        }

        public void SetPaused(bool value)
        {
            paused = value;
            for (var index = 0; index < pool.Count; index++)
            {
                if (pool[index] != null && pool[index].gameObject.activeSelf)
                    pool[index].SetPaused(value);
            }
        }

        public void SetEffectEnabled(bool value)
        {
            effectEnabled = value;
            for (var index = 0; index < pool.Count; index++)
            {
                if (pool[index] != null && pool[index].gameObject.activeSelf)
                    pool[index].SetEffectEnabled(value);
            }
        }

        public void ApplyPreset(EtherBubblePresetKind preset)
        {
            currentPreset = preset;
            currentVisualSettings = (preset == EtherBubblePresetKind.Obvious
                ? obviousPreset
                : softPreset).Sanitized();

            for (var index = 0; index < pool.Count; index++)
            {
                if (pool[index] != null && pool[index].gameObject.activeSelf)
                    pool[index].ApplyVisualSettings(currentVisualSettings);
            }
        }

        public bool Emit()
        {
            if (!HasRequiredReferences || paused || ActiveCount >= maxBubbles) return false;

            var bubble = FindAvailableBubble();
            if (bubble == null) bubble = CreateBubble();
            if (bubble == null) return false;

            emittedSequence++;
            bubble.gameObject.name = "Bubble_" + emittedSequence.ToString("00");
            bubble.gameObject.SetActive(true);
            bubble.Activate(
                this,
                captureSource,
                mouthAnchor.position,
                bubbleRadius,
                movementVelocity,
                bubbleLifetime,
                currentVisualSettings,
                effectEnabled);
            bubble.SetPaused(paused);
            return true;
        }

        public int Burst(int requestedCount)
        {
            var safeCount = Mathf.Clamp(requestedCount, 0, maxBubbles);
            var emitted = 0;
            for (var index = 0; index < safeCount; index++)
            {
                if (!Emit()) break;
                emitted++;
            }
            return emitted;
        }

        public void Clear()
        {
            for (var index = 0; index < pool.Count; index++)
            {
                var bubble = pool[index];
                if (bubble != null && bubble.gameObject.activeSelf)
                    bubble.gameObject.SetActive(false);
            }
        }

        internal void ReleaseBubble(EtherBubbleDistortion2D bubble)
        {
            if (bubble != null && bubble.gameObject.activeSelf)
                bubble.gameObject.SetActive(false);
        }

        private void Reset()
        {
            softPreset = EtherBubbleVisualSettings.Soft;
            obviousPreset = EtherBubbleVisualSettings.Obvious;
            initialPreset = EtherBubblePresetKind.Soft;
        }

        private void Awake()
        {
            EnsurePresetDefaults();
            ApplyPreset(initialPreset);
        }

        private void OnEnable()
        {
            EnsurePresetDefaults();
            ApplyPreset(initialPreset);
            paused = false;
            autoEmissionRunning = autoEmitOnEnable;
            emissionCountdown = 0f;
        }

        private void Update()
        {
            if (!autoEmissionRunning || paused) return;
            emissionCountdown -= Time.deltaTime;
            if (emissionCountdown > 0f) return;
            Emit();
            emissionCountdown = emissionInterval;
        }

        private void OnDisable()
        {
            autoEmissionRunning = false;
            paused = false;
            Clear();
        }

        private void OnValidate()
        {
            maxBubbles = Mathf.Clamp(maxBubbles, MinimumBubbleCount, MaximumBubbleCount);
            emissionInterval = EtherBubbleDistortion2D.ClampFinite(emissionInterval, 0.05f, 10f, 0.7f);
            bubbleRadius = EtherBubbleDistortion2D.ClampFinite(bubbleRadius, 0.05f, 20f, 0.8f);
            movementVelocity = new Vector2(
                EtherBubbleDistortion2D.ClampFinite(movementVelocity.x, -100f, 100f, 0f),
                EtherBubbleDistortion2D.ClampFinite(movementVelocity.y, -100f, 100f, 0f));
            bubbleLifetime = EtherBubbleDistortion2D.ClampFinite(bubbleLifetime, 0.05f, 120f, 5f);
            EnsurePresetDefaults();
        }

        private void EnsurePresetDefaults()
        {
            if (softPreset.waveFrequency <= 0f) softPreset = EtherBubbleVisualSettings.Soft;
            else softPreset = softPreset.Sanitized();
            if (obviousPreset.waveFrequency <= 0f) obviousPreset = EtherBubbleVisualSettings.Obvious;
            else obviousPreset = obviousPreset.Sanitized();
            currentVisualSettings = (initialPreset == EtherBubblePresetKind.Obvious
                ? obviousPreset
                : softPreset).Sanitized();
            currentPreset = initialPreset;
        }

        private EtherBubbleDistortion2D FindAvailableBubble()
        {
            for (var index = 0; index < pool.Count; index++)
            {
                var candidate = pool[index];
                if (candidate != null && !candidate.gameObject.activeSelf) return candidate;
            }
            return null;
        }

        private EtherBubbleDistortion2D CreateBubble()
        {
            if (pool.Count >= maxBubbles || bubblePrefab == null || spawnParent == null) return null;
            var bubble = Instantiate(bubblePrefab, spawnParent);
            bubble.gameObject.SetActive(false);
            pool.Add(bubble);
            return bubble;
        }

        private void ReleaseAboveLimit()
        {
            var activeSeen = 0;
            for (var index = 0; index < pool.Count; index++)
            {
                var bubble = pool[index];
                if (bubble == null || !bubble.gameObject.activeSelf) continue;
                activeSeen++;
                if (activeSeen > maxBubbles) bubble.gameObject.SetActive(false);
            }
        }
    }
}
