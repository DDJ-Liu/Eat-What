using System;
using UnityEngine;

namespace EatWhat.LongLoop.Configuration
{
    // Scene objects remain in the scene; config assets contain only reusable values.
    public sealed class StreetConfigSet
    {
        public DepthScaleConfig DepthScale { get; }
        public CameraModeConfig CameraMode { get; }
        public CameraFollowConfig CameraFollow { get; }
        public SceneBoundsConfig SceneBounds { get; }
        public Transform GroundLine { get; }
        public Collider2D CameraBounds { get; }

        internal StreetConfigSet(DepthScaleConfig depthScale, CameraModeConfig cameraMode,
            CameraFollowConfig cameraFollow, SceneBoundsConfig sceneBounds,
            Transform groundLine, Collider2D cameraBounds)
        {
            DepthScale = depthScale;
            CameraMode = cameraMode;
            CameraFollow = cameraFollow;
            SceneBounds = sceneBounds;
            GroundLine = groundLine;
            CameraBounds = cameraBounds;
        }
    }

    public static class StreetConfigs
    {
        public static StreetConfigSet Current { get; private set; }
        public static event Action Changed;

        public static void Bind(DepthScaleConfig depthScale, CameraModeConfig cameraMode,
            CameraFollowConfig cameraFollow, SceneBoundsConfig sceneBounds,
            Transform groundLine, Collider2D cameraBounds)
        {
            if (ReferenceEquals(depthScale, null) || ReferenceEquals(cameraMode, null) ||
                ReferenceEquals(cameraFollow, null) || ReferenceEquals(sceneBounds, null) ||
                ReferenceEquals(groundLine, null) || ReferenceEquals(cameraBounds, null))
                throw new ArgumentNullException("Street config assets and scene bindings must all be supplied.");

            var old = Current;
            if (old != null && ReferenceEquals(old.DepthScale, depthScale) &&
                ReferenceEquals(old.CameraMode, cameraMode) && ReferenceEquals(old.CameraFollow, cameraFollow) &&
                ReferenceEquals(old.SceneBounds, sceneBounds) && ReferenceEquals(old.GroundLine, groundLine) &&
                ReferenceEquals(old.CameraBounds, cameraBounds)) return;

            Unsubscribe(old);
            Current = new StreetConfigSet(depthScale, cameraMode, cameraFollow, sceneBounds, groundLine, cameraBounds);
            depthScale.CaptureSerializedState();
            cameraMode.CaptureSerializedState();
            cameraFollow.CaptureSerializedState();
            sceneBounds.CaptureSerializedState();
            depthScale.Changed += OnAssetChanged;
            cameraMode.Changed += OnAssetChanged;
            cameraFollow.Changed += OnAssetChanged;
            sceneBounds.Changed += OnAssetChanged;
            Changed?.Invoke();
        }

        public static void Unbind()
        {
            if (Current == null) return;
            Unsubscribe(Current);
            Current = null;
            Changed?.Invoke();
        }

        private static void Unsubscribe(StreetConfigSet set)
        {
            if (set == null) return;
            set.DepthScale.Changed -= OnAssetChanged;
            set.CameraMode.Changed -= OnAssetChanged;
            set.CameraFollow.Changed -= OnAssetChanged;
            set.SceneBounds.Changed -= OnAssetChanged;
        }

        private static void OnAssetChanged()
        {
            Changed?.Invoke();
        }
    }
}
