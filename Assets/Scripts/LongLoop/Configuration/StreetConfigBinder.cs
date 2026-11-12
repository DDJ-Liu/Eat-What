using UnityEngine;

namespace EatWhat.LongLoop.Configuration
{
    // Attach to the existing Street entry object. Scene references do not live in SO assets.
    [DisallowMultipleComponent]
    public sealed class StreetConfigBinder : MonoBehaviour
    {
        [SerializeField] private DepthScaleConfig depthScaleConfig = null;
        [SerializeField] private CameraModeConfig cameraModeConfig = null;
        [SerializeField] private CameraFollowConfig cameraFollowConfig = null;
        [SerializeField] private SceneBoundsConfig sceneBoundsConfig = null;
        [SerializeField] private Transform groundLine = null;
        [SerializeField] private Collider2D cameraBounds = null;

        private StreetConfigSet activeSet;

        private void OnEnable()
        {
            if (Application.isPlaying) BindOrWarn();
        }

        private void OnValidate()
        {
            if (Application.isPlaying && isActiveAndEnabled) BindOrWarn();
        }

        private void OnDisable()
        {
            if (ReferenceEquals(StreetConfigs.Current, activeSet)) StreetConfigs.Unbind();
            activeSet = null;
        }

        private void BindOrWarn()
        {
            if (depthScaleConfig == null || cameraModeConfig == null || cameraFollowConfig == null ||
                sceneBoundsConfig == null || groundLine == null || cameraBounds == null)
            {
                if (ReferenceEquals(StreetConfigs.Current, activeSet)) StreetConfigs.Unbind();
                activeSet = null;
                Debug.LogWarning("StreetConfigBinder requires four config assets, groundLine and cameraBounds.", this);
                return;
            }
            StreetConfigs.Bind(depthScaleConfig, cameraModeConfig, cameraFollowConfig,
                sceneBoundsConfig, groundLine, cameraBounds);
            activeSet = StreetConfigs.Current;
        }
    }
}
