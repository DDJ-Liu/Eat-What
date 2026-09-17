using System;
using UnityEngine;

namespace EatWhat.Tools.Rendering
{
    [Serializable]
    public struct EtherBubbleVisualSettings
    {
        [Range(-0.08f, 0.08f)] public float distortionStrength;
        [Range(0.5f, 20f)] public float waveFrequency;
        [Range(-8f, 8f)] public float waveSpeed;
        [Range(-4f, 4f)] public float flowSpeed;
        [Range(0.01f, 0.5f)] public float softness;
        [ColorUsage(false, true)] public Color rimColor;
        [Range(0f, 4f)] public float rimIntensity;
        [Range(0f, 1f)] public float rimAlpha;

        public static EtherBubbleVisualSettings Soft
        {
            get
            {
                return new EtherBubbleVisualSettings
                {
                    distortionStrength = 0.012f,
                    waveFrequency = 5f,
                    waveSpeed = 0.7f,
                    flowSpeed = 0.35f,
                    softness = 0.2f,
                    rimColor = new Color(0.55f, 0.88f, 1.15f, 1f),
                    rimIntensity = 0.55f,
                    rimAlpha = 0.28f
                };
            }
        }

        public static EtherBubbleVisualSettings Obvious
        {
            get
            {
                return new EtherBubbleVisualSettings
                {
                    distortionStrength = 0.032f,
                    waveFrequency = 8f,
                    waveSpeed = 1.4f,
                    flowSpeed = 0.75f,
                    softness = 0.14f,
                    rimColor = new Color(0.65f, 0.95f, 1.4f, 1f),
                    rimIntensity = 1.15f,
                    rimAlpha = 0.55f
                };
            }
        }

        public EtherBubbleVisualSettings Sanitized()
        {
            return new EtherBubbleVisualSettings
            {
                distortionStrength = EtherBubbleDistortion2D.ClampFinite(distortionStrength, -0.08f, 0.08f, 0f),
                waveFrequency = EtherBubbleDistortion2D.ClampFinite(waveFrequency, 0.5f, 20f, 5f),
                waveSpeed = EtherBubbleDistortion2D.ClampFinite(waveSpeed, -8f, 8f, 0f),
                flowSpeed = EtherBubbleDistortion2D.ClampFinite(flowSpeed, -4f, 4f, 0f),
                softness = EtherBubbleDistortion2D.ClampFinite(softness, 0.01f, 0.5f, 0.2f),
                rimColor = EtherBubbleDistortion2D.SanitizeColor(rimColor),
                rimIntensity = EtherBubbleDistortion2D.ClampFinite(rimIntensity, 0f, 4f, 0f),
                rimAlpha = EtherBubbleDistortion2D.ClampFinite(rimAlpha, 0f, 1f, 0f)
            };
        }
    }

    /// <summary>A pooled world-space bubble that samples one shared capture source.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    [AddComponentMenu("Eat What/Rendering/Ether Bubble Distortion 2D")]
    public sealed class EtherBubbleDistortion2D : MonoBehaviour
    {
        private const float MinimumRadius = 0.05f;
        private const float MaximumRadius = 20f;
        private const float MaximumLifetime = 120f;
        private const float MaximumSpeed = 100f;

        private static readonly int SceneColorTextureId = Shader.PropertyToID("_SceneColorTexture");
        private static readonly int CaptureValidId = Shader.PropertyToID("_CaptureValid");
        private static readonly int DistortionStrengthId = Shader.PropertyToID("_DistortionStrength");
        private static readonly int WaveFrequencyId = Shader.PropertyToID("_WaveFrequency");
        private static readonly int WaveSpeedId = Shader.PropertyToID("_WaveSpeed");
        private static readonly int FlowSpeedId = Shader.PropertyToID("_FlowSpeed");
        private static readonly int SoftnessId = Shader.PropertyToID("_Softness");
        private static readonly int RimColorId = Shader.PropertyToID("_RimColor");
        private static readonly int RimIntensityId = Shader.PropertyToID("_RimIntensity");
        private static readonly int RimAlphaId = Shader.PropertyToID("_RimAlpha");
        private static readonly int BubbleAspectId = Shader.PropertyToID("_BubbleAspect");

        [Header("Renderer")]
        [SerializeField] private Material bubbleMaterial = null;
        [SerializeField] private SceneColorCapture2D captureSource = null;

        [Header("Visual preset")]
        [SerializeField] private EtherBubbleVisualSettings visualSettings = default(EtherBubbleVisualSettings);

        [Header("Instance motion")]
        [SerializeField, Range(MinimumRadius, MaximumRadius)] private float radius = 0.8f;
        [SerializeField] private Vector2 movementVelocity = new Vector2(1.4f, 0.8f);
        [SerializeField, Range(0.05f, MaximumLifetime)] private float lifetime = 5f;

        private SpriteRenderer spriteRenderer;
        private MaterialPropertyBlock propertyBlock;
        private Material originalMaterial;
        private Material assignedMaterial;
        private EtherBubbleEmitter2D owner;
        private bool running;
        private bool paused;
        private bool effectEnabled = true;
        private bool propertiesDirty = true;
        private bool subscribedToCapture;
        private bool lastCaptureValid;
        private RenderTexture lastCaptureTexture;
        private float lastBubbleAspect = -1f;
        private float age;

        public SceneColorCapture2D CaptureSource { get { return captureSource; } }
        public EtherBubbleVisualSettings VisualSettings { get { return visualSettings; } }
        public float Radius { get { return radius; } }
        public Vector2 MovementVelocity { get { return movementVelocity; } }
        public float Lifetime { get { return lifetime; } }
        public float Age { get { return age; } }
        public bool IsRunning { get { return running; } }
        public bool IsPaused { get { return paused; } }
        public bool EffectEnabled { get { return effectEnabled; } }

        public void Configure(Material material, SceneColorCapture2D source)
        {
            bubbleMaterial = material;
            BindCaptureSource(source);
            propertiesDirty = true;
            ApplyRendererProperties();
        }

        public void ApplyVisualSettings(EtherBubbleVisualSettings settings)
        {
            visualSettings = settings.Sanitized();
            propertiesDirty = true;
            ApplyRendererProperties();
        }

        public void SetEffectEnabled(bool value)
        {
            if (effectEnabled == value) return;
            effectEnabled = value;
            propertiesDirty = true;
            ApplyRendererProperties();
        }

        public void SetPaused(bool value)
        {
            paused = value;
        }

        public void Activate(
            EtherBubbleEmitter2D configuredOwner,
            SceneColorCapture2D source,
            Vector3 worldPosition,
            float configuredRadius,
            Vector2 configuredVelocity,
            float configuredLifetime,
            EtherBubbleVisualSettings settings,
            bool enableEffect)
        {
            owner = configuredOwner;
            radius = ClampFinite(configuredRadius, MinimumRadius, MaximumRadius, 0.8f);
            movementVelocity = SanitizeVelocity(configuredVelocity);
            lifetime = ClampFinite(configuredLifetime, 0.05f, MaximumLifetime, 5f);
            visualSettings = settings.Sanitized();
            effectEnabled = enableEffect;
            paused = false;
            age = 0f;
            running = true;
            transform.position = worldPosition;
            transform.localScale = Vector3.one * (radius * 2f);
            BindCaptureSource(source);
            propertiesDirty = true;
            ApplyRendererProperties();
        }

        public void Tick(float deltaTime)
        {
            if (!running || paused) return;
            var safeDeltaTime = ClampFinite(deltaTime, 0f, 1f, 0f);
            if (safeDeltaTime <= 0f) return;

            age += safeDeltaTime;
            transform.position += new Vector3(movementVelocity.x, movementVelocity.y, 0f) * safeDeltaTime;
            if (age < lifetime) return;

            running = false;
            if (owner != null) owner.ReleaseBubble(this);
            else gameObject.SetActive(false);
        }

        private void Reset()
        {
            visualSettings = EtherBubbleVisualSettings.Soft;
            CacheRenderer();
            propertiesDirty = true;
            ApplyRendererProperties();
        }

        private void OnEnable()
        {
            BindCaptureSource(captureSource);
            CacheRenderer();
            propertiesDirty = true;
            ApplyRendererProperties();
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        private void LateUpdate()
        {
            ApplyRendererProperties();
        }

        private void OnDisable()
        {
            running = false;
            paused = false;
            UnsubscribeFromCapture();
            ClearOwnedRendererProperties();
            RestoreMaterial();
        }

        private void OnDestroy()
        {
            UnsubscribeFromCapture();
            ClearOwnedRendererProperties();
            RestoreMaterial();
        }

        private void OnValidate()
        {
            radius = ClampFinite(radius, MinimumRadius, MaximumRadius, 0.8f);
            movementVelocity = SanitizeVelocity(movementVelocity);
            lifetime = ClampFinite(lifetime, 0.05f, MaximumLifetime, 5f);
            visualSettings = visualSettings.Sanitized();
            propertiesDirty = true;
        }

        private void BindCaptureSource(SceneColorCapture2D source)
        {
            if (captureSource == source && subscribedToCapture) return;
            UnsubscribeFromCapture();
            captureSource = source;
            if (captureSource != null && isActiveAndEnabled)
            {
                captureSource.CaptureStateChanged += HandleCaptureStateChanged;
                subscribedToCapture = true;
            }
            propertiesDirty = true;
        }

        private void UnsubscribeFromCapture()
        {
            if (subscribedToCapture && captureSource != null)
                captureSource.CaptureStateChanged -= HandleCaptureStateChanged;
            subscribedToCapture = false;
        }

        private void HandleCaptureStateChanged()
        {
            propertiesDirty = true;
            ApplyRendererProperties();
        }

        private void CacheRenderer()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();
        }

        private void ApplyRendererProperties()
        {
            CacheRenderer();
            if (spriteRenderer == null) return;

            if (bubbleMaterial != null && spriteRenderer.sharedMaterial != bubbleMaterial)
            {
                originalMaterial = spriteRenderer.sharedMaterial;
                assignedMaterial = bubbleMaterial;
                spriteRenderer.sharedMaterial = bubbleMaterial;
            }

            var texture = captureSource == null ? null : captureSource.CapturedTexture;
            var captureValid = effectEnabled && captureSource != null && captureSource.IsFrameValid && texture != null;
            var aspect = CalculateRenderedAspect();
            if (!propertiesDirty && captureValid == lastCaptureValid && texture == lastCaptureTexture &&
                Mathf.Approximately(aspect, lastBubbleAspect))
                return;

            spriteRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetTexture(
                SceneColorTextureId,
                texture == null ? (Texture)Texture2D.blackTexture : texture);
            propertyBlock.SetFloat(CaptureValidId, captureValid ? 1f : 0f);
            propertyBlock.SetFloat(DistortionStrengthId, visualSettings.distortionStrength);
            propertyBlock.SetFloat(WaveFrequencyId, visualSettings.waveFrequency);
            propertyBlock.SetFloat(WaveSpeedId, visualSettings.waveSpeed);
            propertyBlock.SetFloat(FlowSpeedId, visualSettings.flowSpeed);
            propertyBlock.SetFloat(SoftnessId, visualSettings.softness);
            propertyBlock.SetColor(RimColorId, visualSettings.rimColor);
            propertyBlock.SetFloat(RimIntensityId, visualSettings.rimIntensity);
            propertyBlock.SetFloat(RimAlphaId, visualSettings.rimAlpha);
            propertyBlock.SetFloat(BubbleAspectId, aspect);
            spriteRenderer.SetPropertyBlock(propertyBlock);

            lastCaptureValid = captureValid;
            lastCaptureTexture = texture;
            lastBubbleAspect = aspect;
            propertiesDirty = false;
        }

        private void ClearOwnedRendererProperties()
        {
            CacheRenderer();
            if (spriteRenderer == null) return;
            spriteRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetTexture(SceneColorTextureId, Texture2D.blackTexture);
            propertyBlock.SetFloat(CaptureValidId, 0f);
            propertyBlock.SetFloat(DistortionStrengthId, 0f);
            propertyBlock.SetFloat(RimIntensityId, 0f);
            propertyBlock.SetFloat(RimAlphaId, 0f);
            spriteRenderer.SetPropertyBlock(propertyBlock);
            propertiesDirty = true;
            lastCaptureTexture = null;
            lastCaptureValid = false;
        }

        private void RestoreMaterial()
        {
            if (spriteRenderer != null && assignedMaterial != null && spriteRenderer.sharedMaterial == assignedMaterial)
                spriteRenderer.sharedMaterial = originalMaterial;
            assignedMaterial = null;
            originalMaterial = null;
        }

        private float CalculateRenderedAspect()
        {
            var scale = transform.lossyScale;
            var width = Mathf.Abs(scale.x);
            var height = Mathf.Abs(scale.y);
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                width *= Mathf.Abs(spriteRenderer.sprite.bounds.size.x);
                height *= Mathf.Abs(spriteRenderer.sprite.bounds.size.y);
            }

            if (width <= 0.0001f || height <= 0.0001f) return 1f;
            return ClampFinite(width / height, 0.01f, 100f, 1f);
        }

        private static Vector2 SanitizeVelocity(Vector2 value)
        {
            return new Vector2(
                ClampFinite(value.x, -MaximumSpeed, MaximumSpeed, 0f),
                ClampFinite(value.y, -MaximumSpeed, MaximumSpeed, 0f));
        }

        internal static float ClampFinite(float value, float minimum, float maximum, float fallback)
        {
            return float.IsNaN(value) || float.IsInfinity(value)
                ? fallback
                : Mathf.Clamp(value, minimum, maximum);
        }

        internal static Color SanitizeColor(Color value)
        {
            return new Color(
                ClampFinite(value.r, 0f, 8f, 0f),
                ClampFinite(value.g, 0f, 8f, 0f),
                ClampFinite(value.b, 0f, 8f, 0f),
                ClampFinite(value.a, 0f, 1f, 0f));
        }
    }
}
