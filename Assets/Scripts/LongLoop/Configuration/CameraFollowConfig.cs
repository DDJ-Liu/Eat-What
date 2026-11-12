using UnityEngine;

namespace EatWhat.LongLoop.Configuration
{
    public enum StreetCurvePreset { Linear, Quadratic, SmoothStep, EaseInOut }
    public enum GamepadLookAxisSource { RightStick }

    [CreateAssetMenu(menuName = "Eat What/Street/CameraFollowConfig")]
    public sealed class CameraFollowConfig : StreetConfigAsset
    {
        [SerializeField] private float deadZoneWidth = 0.10f;
        [SerializeField] private float deadZoneHeight = 0.20f;
        [SerializeField] private float dampingX = 0.25f;
        [SerializeField] private float dampingY = 0.25f;
        [SerializeField, Tooltip("停步镜头速度倍率：实际停步时普通XY跟随响应的倍率；0暂停，1原速度，默认0.7。")]
        private float stoppedSpeedMultiplier = 0.7f;
        [SerializeField] private float lookahead = 2.1f;
        [SerializeField] private float lookaheadTime = 0.5f;
        [SerializeField] private float maxHorizontalOffset = 2.5f;
        [SerializeField] private float recenterDelay = 0.6f;
        [SerializeField] private float recenterSpeed = 4.0f;
        [SerializeField] private float recenterRampTime = 0.25f;
        [SerializeField] private StreetCurvePreset recenterCurve = StreetCurvePreset.SmoothStep;
        [SerializeField] private float verticalSpeedThreshold = 0.5f;
        [SerializeField] private float maxVerticalOffset = 1.5f;
        [SerializeField] private float verticalOffsetInTime = 0.3f;
        [SerializeField] private float verticalOffsetOutTime = 0.4f;
        [SerializeField] private StreetCurvePreset verticalOffsetCurve = StreetCurvePreset.SmoothStep;
        [SerializeField] private bool dynamicZoomEnabled = true;
        [SerializeField] private float orthoSizeMin = 7.2f;
        [SerializeField] private float orthoSizeMax = 8.3f;
        [SerializeField] private float moveSpeedThreshold = 0.5f;
        [SerializeField] private float expandTime = 1.2f;
        [SerializeField] private StreetCurvePreset expandCurve = StreetCurvePreset.EaseInOut;
        [SerializeField] private float shrinkTime = 1.5f;
        [SerializeField] private StreetCurvePreset shrinkCurve = StreetCurvePreset.EaseInOut;
        [SerializeField] private bool freezeOnOverride = true;
        [SerializeField] private bool lookFloatEnabled = true;
        [SerializeField] private float edgeZoneRatio = 0.25f;
        [SerializeField] private StreetCurvePreset lookResponseCurve = StreetCurvePreset.Quadratic;
        [SerializeField] private float lookInTime = 0.35f;
        [SerializeField] private float lookOutTime = 0.5f;
        [SerializeField] private GamepadLookAxisSource gamepadLookAxis = GamepadLookAxisSource.RightStick;

        public float DeadZoneWidth { get => deadZoneWidth; set => SetValue(ref deadZoneWidth, value); }
        public float DeadZoneHeight { get => deadZoneHeight; set => SetValue(ref deadZoneHeight, value); }
        public float DampingX { get => dampingX; set => SetValue(ref dampingX, value); }
        public float DampingY { get => dampingY; set => SetValue(ref dampingY, value); }
        public float StoppedSpeedMultiplier { get => stoppedSpeedMultiplier; set => SetValue(ref stoppedSpeedMultiplier, value); }
        public float Lookahead { get => lookahead; set => SetValue(ref lookahead, value); }
        public float LookaheadTime { get => lookaheadTime; set => SetValue(ref lookaheadTime, value); }
        public float MaxHorizontalOffset { get => maxHorizontalOffset; set => SetValue(ref maxHorizontalOffset, value); }
        public float RecenterDelay { get => recenterDelay; set => SetValue(ref recenterDelay, value); }
        public float RecenterSpeed { get => recenterSpeed; set => SetValue(ref recenterSpeed, value); }
        public float RecenterRampTime { get => recenterRampTime; set => SetValue(ref recenterRampTime, value); }
        public StreetCurvePreset RecenterCurve { get => recenterCurve; set => SetValue(ref recenterCurve, value); }
        public float VerticalSpeedThreshold { get => verticalSpeedThreshold; set => SetValue(ref verticalSpeedThreshold, value); }
        public float MaxVerticalOffset { get => maxVerticalOffset; set => SetValue(ref maxVerticalOffset, value); }
        public float VerticalOffsetInTime { get => verticalOffsetInTime; set => SetValue(ref verticalOffsetInTime, value); }
        public float VerticalOffsetOutTime { get => verticalOffsetOutTime; set => SetValue(ref verticalOffsetOutTime, value); }
        public StreetCurvePreset VerticalOffsetCurve { get => verticalOffsetCurve; set => SetValue(ref verticalOffsetCurve, value); }
        public bool DynamicZoomEnabled { get => dynamicZoomEnabled; set => SetValue(ref dynamicZoomEnabled, value); }
        public float OrthoSizeMin { get => orthoSizeMin; set => SetValue(ref orthoSizeMin, value); }
        public float OrthoSizeMax { get => orthoSizeMax; set => SetValue(ref orthoSizeMax, value); }
        public float MoveSpeedThreshold { get => moveSpeedThreshold; set => SetValue(ref moveSpeedThreshold, value); }
        public float ExpandTime { get => expandTime; set => SetValue(ref expandTime, value); }
        public StreetCurvePreset ExpandCurve { get => expandCurve; set => SetValue(ref expandCurve, value); }
        public float ShrinkTime { get => shrinkTime; set => SetValue(ref shrinkTime, value); }
        public StreetCurvePreset ShrinkCurve { get => shrinkCurve; set => SetValue(ref shrinkCurve, value); }
        public bool FreezeOnOverride { get => freezeOnOverride; set => SetValue(ref freezeOnOverride, value); }
        public bool LookFloatEnabled { get => lookFloatEnabled; set => SetValue(ref lookFloatEnabled, value); }
        public float EdgeZoneRatio { get => edgeZoneRatio; set => SetValue(ref edgeZoneRatio, value); }
        public StreetCurvePreset LookResponseCurve { get => lookResponseCurve; set => SetValue(ref lookResponseCurve, value); }
        public float LookInTime { get => lookInTime; set => SetValue(ref lookInTime, value); }
        public float LookOutTime { get => lookOutTime; set => SetValue(ref lookOutTime, value); }
        public GamepadLookAxisSource GamepadLookAxis { get => gamepadLookAxis; set => SetValue(ref gamepadLookAxis, value); }

        protected override string SerializedState()
        {
            return StateKey(deadZoneWidth, deadZoneHeight, dampingX, dampingY, stoppedSpeedMultiplier, lookahead, lookaheadTime, maxHorizontalOffset, recenterDelay, recenterSpeed, recenterRampTime, recenterCurve, verticalSpeedThreshold, maxVerticalOffset, verticalOffsetInTime, verticalOffsetOutTime, verticalOffsetCurve, dynamicZoomEnabled, orthoSizeMin, orthoSizeMax, moveSpeedThreshold, expandTime, expandCurve, shrinkTime, shrinkCurve, freezeOnOverride, lookFloatEnabled, edgeZoneRatio, lookResponseCurve, lookInTime, lookOutTime, gamepadLookAxis);
        }
    }
}
