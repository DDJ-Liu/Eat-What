using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace EatWhat.Tools.Rendering
{
    /// <summary>
    /// Owns one background RenderTexture for one main camera. A dedicated camera
    /// renders the explicitly selected layer before the main camera; consumers use
    /// CapturedTexture directly instead of a global shader property.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Eat What/Rendering/Scene Color Capture 2D")]
    [DefaultExecutionOrder(-1000)]
    public sealed class SceneColorCapture2D : MonoBehaviour
    {
        private const float MinimumRenderScale = 0.25f;
        private const float MaximumRenderScale = 1f;

        [Header("Explicit camera references")]
        [SerializeField] private Camera mainCamera = null;
        [SerializeField] private Camera backgroundCamera = null;
        [SerializeField] private LayerMask backgroundLayerMask = 0;

        [Header("Capture quality")]
        [SerializeField, Range(MinimumRenderScale, MaximumRenderScale)]
        private float renderScale = 1f;
        [SerializeField, Tooltip("Supported values are 1, 2, 4 and 8.")]
        private int antiAliasing = 1;
        [SerializeField] private bool captureEnabled = true;

        private RenderTexture ownedTexture;
        private CameraState originalCameraState;
        private bool originalStateCaptured;
        private bool subscribed;
        private bool frameValid;
        private int completedFrame = -1;

        public event Action CaptureStateChanged;

        public Camera MainCamera { get { return mainCamera; } }
        public Camera BackgroundCamera { get { return backgroundCamera; } }
        public LayerMask BackgroundLayerMask { get { return backgroundLayerMask; } }
        public RenderTexture CapturedTexture { get { return ownedTexture; } }
        public bool IsFrameValid { get { return frameValid && ownedTexture != null && ownedTexture.IsCreated(); } }
        public int LastCompletedFrame { get { return completedFrame; } }
        public int OwnedRenderTextureCount { get { return ownedTexture == null ? 0 : 1; } }
        public int CaptureWidth { get { return ownedTexture == null ? 0 : ownedTexture.width; } }
        public int CaptureHeight { get { return ownedTexture == null ? 0 : ownedTexture.height; } }

        public bool CaptureEnabled
        {
            get { return captureEnabled; }
            set
            {
                if (captureEnabled == value) return;
                captureEnabled = value;
                if (Application.isPlaying) RefreshCaptureState();
            }
        }

        public float RenderScale
        {
            get { return renderScale; }
            set
            {
                var sanitized = SanitizeFinite(value, MinimumRenderScale, MaximumRenderScale, 1f);
                if (Mathf.Approximately(renderScale, sanitized)) return;
                renderScale = sanitized;
                if (Application.isPlaying) RefreshCaptureState();
            }
        }

        public int AntiAliasing
        {
            get { return antiAliasing; }
            set
            {
                var sanitized = NormalizeAntiAliasing(value);
                if (antiAliasing == sanitized) return;
                antiAliasing = sanitized;
                if (Application.isPlaying) RefreshCaptureState();
            }
        }

        public void Configure(
            Camera configuredMainCamera,
            Camera configuredBackgroundCamera,
            LayerMask configuredBackgroundLayerMask,
            float configuredRenderScale,
            int configuredAntiAliasing)
        {
            if (Application.isPlaying && originalStateCaptured && backgroundCamera != configuredBackgroundCamera)
            {
                ReleaseOwnedTexture();
                RestoreBackgroundCamera();
            }

            mainCamera = configuredMainCamera;
            backgroundCamera = configuredBackgroundCamera;
            backgroundLayerMask = configuredBackgroundLayerMask;
            renderScale = SanitizeFinite(
                configuredRenderScale,
                MinimumRenderScale,
                MaximumRenderScale,
                1f);
            antiAliasing = NormalizeAntiAliasing(configuredAntiAliasing);
            originalStateCaptured = false;
            SetFrameValid(false);

            if (Application.isPlaying) RefreshCaptureState();
        }

        /// <summary>Synchronizes cameras and ensures the single owned RT exists.</summary>
        public bool RefreshCaptureState()
        {
            if (!Application.isPlaying || !HasUsableConfiguration())
            {
                SetFrameValid(false);
                if (backgroundCamera != null && originalStateCaptured)
                    backgroundCamera.enabled = false;
                return false;
            }

            CaptureOriginalState();

            var sourceWidth = mainCamera.pixelWidth;
            var sourceHeight = mainCamera.pixelHeight;
            if (!captureEnabled || !mainCamera.enabled || !mainCamera.gameObject.activeInHierarchy ||
                sourceWidth <= 0 || sourceHeight <= 0)
            {
                backgroundCamera.enabled = false;
                SetFrameValid(false);
                return false;
            }

            var width = Mathf.Max(1, Mathf.RoundToInt(sourceWidth * renderScale));
            var height = Mathf.Max(1, Mathf.RoundToInt(sourceHeight * renderScale));
            EnsureOwnedTexture(width, height);
            if (ownedTexture == null || !ownedTexture.IsCreated())
            {
                backgroundCamera.enabled = false;
                SetFrameValid(false);
                return false;
            }

            SynchronizeBackgroundCamera();
            backgroundCamera.enabled = true;
            return true;
        }

        private void OnEnable()
        {
            Subscribe();
            SetFrameValid(false);
            if (Application.isPlaying) RefreshCaptureState();
        }

        private void Update()
        {
            if (Application.isPlaying) RefreshCaptureState();
        }

        private void OnDisable()
        {
            Unsubscribe();
            SetFrameValid(false);
            ReleaseOwnedTexture();
            RestoreBackgroundCamera();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            SetFrameValid(false);
            ReleaseOwnedTexture();
            RestoreBackgroundCamera();
        }

        private void OnValidate()
        {
            renderScale = SanitizeFinite(renderScale, MinimumRenderScale, MaximumRenderScale, 1f);
            antiAliasing = NormalizeAntiAliasing(antiAliasing);
            if (mainCamera == backgroundCamera) backgroundCamera = null;
        }

        private void Subscribe()
        {
            if (subscribed) return;
            RenderPipelineManager.beginCameraRendering += HandleBeginCameraRendering;
            RenderPipelineManager.endCameraRendering += HandleEndCameraRendering;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed) return;
            RenderPipelineManager.beginCameraRendering -= HandleBeginCameraRendering;
            RenderPipelineManager.endCameraRendering -= HandleEndCameraRendering;
            subscribed = false;
        }

        private void HandleBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (camera == backgroundCamera) RefreshCaptureState();
        }

        private void HandleEndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (camera != backgroundCamera || ownedTexture == null ||
                backgroundCamera.targetTexture != ownedTexture || !backgroundCamera.enabled)
                return;

            completedFrame = Time.frameCount;
            SetFrameValid(true);
        }

        private bool HasUsableConfiguration()
        {
            return captureEnabled && mainCamera != null && backgroundCamera != null &&
                mainCamera != backgroundCamera && backgroundLayerMask.value != 0;
        }

        private void CaptureOriginalState()
        {
            if (originalStateCaptured || backgroundCamera == null) return;
            originalCameraState = new CameraState(backgroundCamera);
            originalStateCaptured = true;
        }

        private void SynchronizeBackgroundCamera()
        {
            var captureTransform = backgroundCamera.transform;
            captureTransform.SetPositionAndRotation(mainCamera.transform.position, mainCamera.transform.rotation);

            backgroundCamera.orthographic = mainCamera.orthographic;
            backgroundCamera.orthographicSize = mainCamera.orthographicSize;
            backgroundCamera.fieldOfView = mainCamera.fieldOfView;
            backgroundCamera.nearClipPlane = mainCamera.nearClipPlane;
            backgroundCamera.farClipPlane = mainCamera.farClipPlane;
            backgroundCamera.aspect = mainCamera.aspect;
            backgroundCamera.rect = mainCamera.rect;
            backgroundCamera.projectionMatrix = mainCamera.projectionMatrix;
            backgroundCamera.allowHDR = mainCamera.allowHDR;
            backgroundCamera.allowMSAA = mainCamera.allowMSAA;
            backgroundCamera.useOcclusionCulling = mainCamera.useOcclusionCulling;
            backgroundCamera.forceIntoRenderTexture = true;
            backgroundCamera.clearFlags = CameraClearFlags.SolidColor;
            backgroundCamera.backgroundColor = mainCamera.backgroundColor;
            backgroundCamera.cullingMask = backgroundLayerMask.value;
            backgroundCamera.depth = mainCamera.depth - 1f;
            backgroundCamera.targetTexture = ownedTexture;
        }

        private void EnsureOwnedTexture(int width, int height)
        {
            var normalizedAntiAliasing = NormalizeAntiAliasing(antiAliasing);
            if (ownedTexture != null && ownedTexture.width == width && ownedTexture.height == height &&
                ownedTexture.antiAliasing == normalizedAntiAliasing && ownedTexture.IsCreated())
                return;

            SetFrameValid(false);
            ReleaseOwnedTexture();

            var texture = new RenderTexture(
                width,
                height,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Default)
            {
                name = "EtherBubbleSceneColor_" + mainCamera.GetInstanceID(),
                antiAliasing = normalizedAntiAliasing,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                useMipMap = false,
                autoGenerateMips = false,
                hideFlags = HideFlags.DontSave
            };

            if (!texture.Create())
            {
                DestroyOwnedTexture(texture);
                return;
            }

            ownedTexture = texture;
            backgroundCamera.targetTexture = ownedTexture;
        }

        private void ReleaseOwnedTexture()
        {
            if (ownedTexture == null) return;
            if (backgroundCamera != null && backgroundCamera.targetTexture == ownedTexture)
                backgroundCamera.targetTexture = originalStateCaptured
                    ? originalCameraState.TargetTexture
                    : null;

            ownedTexture.Release();
            DestroyOwnedTexture(ownedTexture);
            ownedTexture = null;
            completedFrame = -1;
        }

        private static void DestroyOwnedTexture(RenderTexture texture)
        {
            if (texture == null) return;
            if (Application.isPlaying) Destroy(texture);
            else DestroyImmediate(texture);
        }

        private void RestoreBackgroundCamera()
        {
            if (!originalStateCaptured || backgroundCamera == null) return;
            originalCameraState.Restore(backgroundCamera);
            originalStateCaptured = false;
        }

        private void SetFrameValid(bool value)
        {
            if (frameValid == value) return;
            frameValid = value;
            var handler = CaptureStateChanged;
            if (handler != null) handler();
        }

        internal static float SanitizeFinite(float value, float minimum, float maximum, float fallback)
        {
            return float.IsNaN(value) || float.IsInfinity(value)
                ? fallback
                : Mathf.Clamp(value, minimum, maximum);
        }

        internal static int NormalizeAntiAliasing(int value)
        {
            if (value >= 8) return 8;
            if (value >= 4) return 4;
            if (value >= 2) return 2;
            return 1;
        }

        private struct CameraState
        {
            public readonly bool Enabled;
            public readonly int CullingMask;
            public readonly float Depth;
            public readonly CameraClearFlags ClearFlags;
            public readonly Color BackgroundColor;
            public readonly RenderTexture TargetTexture;
            public readonly bool Orthographic;
            public readonly float OrthographicSize;
            public readonly float FieldOfView;
            public readonly float NearClipPlane;
            public readonly float FarClipPlane;
            public readonly float Aspect;
            public readonly Rect Rect;
            public readonly Matrix4x4 ProjectionMatrix;
            public readonly bool AllowHdr;
            public readonly bool AllowMsaa;
            public readonly bool UseOcclusionCulling;
            public readonly bool ForceIntoRenderTexture;
            public readonly Vector3 Position;
            public readonly Quaternion Rotation;

            public CameraState(Camera camera)
            {
                Enabled = camera.enabled;
                CullingMask = camera.cullingMask;
                Depth = camera.depth;
                ClearFlags = camera.clearFlags;
                BackgroundColor = camera.backgroundColor;
                TargetTexture = camera.targetTexture;
                Orthographic = camera.orthographic;
                OrthographicSize = camera.orthographicSize;
                FieldOfView = camera.fieldOfView;
                NearClipPlane = camera.nearClipPlane;
                FarClipPlane = camera.farClipPlane;
                Aspect = camera.aspect;
                Rect = camera.rect;
                ProjectionMatrix = camera.projectionMatrix;
                AllowHdr = camera.allowHDR;
                AllowMsaa = camera.allowMSAA;
                UseOcclusionCulling = camera.useOcclusionCulling;
                ForceIntoRenderTexture = camera.forceIntoRenderTexture;
                Position = camera.transform.position;
                Rotation = camera.transform.rotation;
            }

            public void Restore(Camera camera)
            {
                camera.enabled = Enabled;
                camera.cullingMask = CullingMask;
                camera.depth = Depth;
                camera.clearFlags = ClearFlags;
                camera.backgroundColor = BackgroundColor;
                camera.targetTexture = TargetTexture;
                camera.orthographic = Orthographic;
                camera.orthographicSize = OrthographicSize;
                camera.fieldOfView = FieldOfView;
                camera.nearClipPlane = NearClipPlane;
                camera.farClipPlane = FarClipPlane;
                camera.aspect = Aspect;
                camera.rect = Rect;
                camera.projectionMatrix = ProjectionMatrix;
                camera.allowHDR = AllowHdr;
                camera.allowMSAA = AllowMsaa;
                camera.useOcclusionCulling = UseOcclusionCulling;
                camera.forceIntoRenderTexture = ForceIntoRenderTexture;
                camera.transform.SetPositionAndRotation(Position, Rotation);
            }
        }
    }
}
