using System;
using System.Reflection;
using EatWhat.LongLoop.Configuration;
using UnityEngine;

internal static class StreetConfigLogicTests
{
    private static int count;
    private static void Check(bool value, string label)
    {
        count++;
        if (!value) throw new Exception(label);
    }

    private static void Default(object target, string name, object expected)
    {
        var field = target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
        Check(field != null && field.IsPrivate && Equals(field.GetValue(target), expected),
            target.GetType().Name + "." + name + " default");
    }

    private static void InspectorEdit(object target, string name, object value)
    {
        target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
        typeof(StreetConfigAsset).GetMethod("OnValidate", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(target, null);
    }

    private static void BinderField(StreetConfigBinder binder, string name, object value)
    {
        typeof(StreetConfigBinder).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(binder, value);
    }

    private static void BinderCall(StreetConfigBinder binder, string name)
    {
        typeof(StreetConfigBinder).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(binder, null);
    }

    public static int Main()
    {
        var depth = new DepthScaleConfig();
        Default(depth, "depthScaleFactor", 0.03f);
        Default(depth, "DEPTH", 0.35f);
        Default(depth, "FOCUS", 2.0f);
        var mode = new CameraModeConfig();
        Default(mode, "modeBlendTimeToCinematic", 0.4f);
        Default(mode, "modeBlendTimeToCloseUp", 0.35f);
        Default(mode, "modeBlendTimeToTransition", 0f);
        Default(mode, "gateOffsetsInDialogue", true);
        var follow = new CameraFollowConfig();
        Default(follow, "deadZoneWidth", 0.10f);
        Default(follow, "deadZoneHeight", 0.20f);
        Default(follow, "dampingX", 0.25f);
        Default(follow, "dampingY", 0.25f);
        Default(follow, "stoppedSpeedMultiplier", 0.7f);
        Default(follow, "lookahead", 2.1f);
        Default(follow, "lookaheadTime", 0.5f);
        Default(follow, "maxHorizontalOffset", 2.5f);
        Default(follow, "recenterDelay", 0.6f);
        Default(follow, "recenterSpeed", 4.0f);
        Default(follow, "verticalSpeedThreshold", 0.5f);
        Default(follow, "maxVerticalOffset", 1.5f);
        Default(follow, "verticalOffsetInTime", 0.3f);
        Default(follow, "verticalOffsetOutTime", 0.4f);
        Default(follow, "verticalOffsetCurve", StreetCurvePreset.SmoothStep);
        Default(follow, "dynamicZoomEnabled", true);
        Default(follow, "orthoSizeMin", 7.2f);
        Default(follow, "orthoSizeMax", 8.3f);
        Default(follow, "moveSpeedThreshold", 0.5f);
        Default(follow, "expandTime", 1.2f);
        Default(follow, "expandCurve", StreetCurvePreset.EaseInOut);
        Default(follow, "shrinkTime", 1.5f);
        Default(follow, "shrinkCurve", StreetCurvePreset.EaseInOut);
        Default(follow, "freezeOnOverride", true);
        Default(follow, "lookFloatEnabled", true);
        Default(follow, "edgeZoneRatio", 0.25f);
        Default(follow, "lookResponseCurve", StreetCurvePreset.Quadratic);
        Default(follow, "lookInTime", 0.35f);
        Default(follow, "lookOutTime", 0.5f);
        Default(follow, "gamepadLookAxis", GamepadLookAxisSource.RightStick);
        var bounds = new SceneBoundsConfig();
        Default(bounds, "isSmallRoom", false);
        Default(bounds, "groundAlignBottom", true);
        Default(bounds, "sideFillEnabled", true);
        Default(bounds, "sideFillBlur", 24f);
        Default(bounds, "sideFillDim", 0.30f);
        Default(bounds, "exitNearDistance", 3.2f);

        Check(typeof(CameraInterestConfig).BaseType == typeof(StreetConfigAsset), "Interest change-aware group introduced by F02");
        Check(typeof(SceneTransitionConfig).BaseType == typeof(ScriptableObject), "Transition placeholder");
        Check(typeof(CinematicCameraConfig).BaseType == typeof(ScriptableObject), "Cinematic placeholder");
        Check(typeof(CameraEffectsConfig).BaseType == typeof(ScriptableObject), "Effects placeholder");
        Check(typeof(OcclusionConfig).BaseType == typeof(ScriptableObject), "Occlusion placeholder");
        Check(typeof(SceneConfig).BaseType == typeof(ScriptableObject), "Scene placeholder");
        Check(typeof(RoomCameraOverride).BaseType == typeof(ScriptableObject), "Room placeholder");

        var ground = new Transform();
        var polygon = new Collider2D();
        var events = 0;
        Action handler = () => events++;
        StreetConfigs.Changed += handler;
        StreetConfigs.Bind(depth, mode, follow, bounds, ground, polygon);
        Check(events == 1 && ReferenceEquals(StreetConfigs.Current.DepthScale, depth), "Initial bind");
        Check(ReferenceEquals(StreetConfigs.Current.GroundLine, ground) &&
            ReferenceEquals(StreetConfigs.Current.CameraBounds, polygon), "Scene references separate");
        StreetConfigs.Bind(depth, mode, follow, bounds, ground, polygon);
        Check(events == 1, "Repeated binding is quiet");
        depth.DepthScaleFactor = 0.04f;
        Check(events == 2 && StreetConfigs.Current.DepthScale.DepthScaleFactor == 0.04f, "Setter reads and publishes");
        depth.DepthScaleFactor = 0.04f;
        Check(events == 2, "Repeated same value is quiet");
        InspectorEdit(mode, "gateOffsetsInDialogue", false);
        Check(events == 3 && !StreetConfigs.Current.CameraMode.GateOffsetsInDialogue, "Inspector validation publishes");
        InspectorEdit(mode, "gateOffsetsInDialogue", false);
        Check(events == 3, "Repeated Inspector validation is quiet");
        follow.Lookahead = 1.5f;
        bounds.ExitNearDistance = 2.5f;
        Check(events == 5 && StreetConfigs.Current.CameraFollow.Lookahead == 1.5f &&
            StreetConfigs.Current.SceneBounds.ExitNearDistance == 2.5f, "Other groups publish");
        var nextDepth = new DepthScaleConfig();
        StreetConfigs.Bind(nextDepth, mode, follow, bounds, ground, polygon);
        Check(events == 6 && ReferenceEquals(StreetConfigs.Current.DepthScale, nextDepth), "Rebind publishes");
        depth.Depth = 0.5f;
        Check(events == 6, "Old source unsubscribed");
        nextDepth.Focus = 3f;
        Check(events == 7, "New source subscribed once");
        StreetConfigs.Unbind();
        Check(events == 8 && StreetConfigs.Current == null, "Unbind publishes");
        nextDepth.Focus = 4f;
        Check(events == 8, "Unbound source quiet");
        StreetConfigs.Unbind();
        Check(events == 8, "Repeated unbind quiet");
        StreetConfigs.Changed -= handler;
        var rejected = false;
        try { StreetConfigs.Bind(null, mode, follow, bounds, ground, polygon); }
        catch (ArgumentNullException) { rejected = true; }
        Check(rejected && StreetConfigs.Current == null, "Partial binding rejected");

        var binder = new StreetConfigBinder();
        BinderField(binder, "depthScaleConfig", depth);
        BinderField(binder, "cameraModeConfig", mode);
        BinderField(binder, "cameraFollowConfig", follow);
        BinderField(binder, "sceneBoundsConfig", bounds);
        BinderField(binder, "groundLine", ground);
        BinderField(binder, "cameraBounds", polygon);
        Application.isPlaying = true;
        BinderCall(binder, "OnEnable");
        Check(ReferenceEquals(StreetConfigs.Current.DepthScale, depth) &&
            ReferenceEquals(StreetConfigs.Current.GroundLine, ground), "Binder supplies asset and scene sources");
        BinderField(binder, "depthScaleConfig", nextDepth);
        BinderCall(binder, "OnValidate");
        Check(ReferenceEquals(StreetConfigs.Current.DepthScale, nextDepth), "Binder Inspector rebind");
        BinderCall(binder, "OnDisable");
        Check(StreetConfigs.Current == null, "Binder disable unbinds owned set");
        Application.isPlaying = false;

        ShrinkDelayConfiguration();
        LookLimitConfiguration();
        Console.WriteLine("PASS " + count + " actual-source config assertions");
        return 0;
    }

    private static void ShrinkDelayConfiguration()
    {
        var config = new CameraFollowConfig();
        Default(config, "shrinkDelay", 0f);
        var field = typeof(CameraFollowConfig).GetField("shrinkDelay", BindingFlags.NonPublic | BindingFlags.Instance);
        Check(field.IsDefined(typeof(SerializeField), false), "ShrinkDelay is serialized on existing config");
        Check(config.ShrinkDelay == 0, "old config missing field receives zero delay");
        int events = 0; config.Changed += () => events++;
        config.ShrinkDelay = .5f;
        Check(events == 1 && config.ShrinkDelay == .5f, "ShrinkDelay setter publishes once");
        config.ShrinkDelay = .5f;
        Check(events == 1, "same ShrinkDelay value is quiet");
        config.CaptureSerializedState(); InspectorEdit(config, "shrinkDelay", 1f);
        Check(events == 2 && config.ShrinkDelay == 1, "captured Inspector delay change publishes");
        InspectorEdit(config, "shrinkDelay", 1f);
        Check(events == 2, "same Inspector delay is quiet");
        config.ShrinkDelay = float.NaN;
        Check(float.IsNaN(config.ShrinkDelay), "asset does not silently clamp an invalid user delay");
    }

    private static void LookLimitConfiguration()
    {
        var c=new CameraFollowConfig();Default(c,"lookMaxDistance",2.5f);Default(c,"lookMaxSpeed",8f);
        Check(c.LookMaxDistance==2.5f&&c.LookMaxSpeed==8,"look limits public defaults");
        int events=0;c.Changed+=()=>events++;c.LookMaxDistance=1;c.LookMaxSpeed=2;
        Check(events==2&&c.LookMaxDistance==1&&c.LookMaxSpeed==2,"both setters notify");
        c.LookMaxDistance=1;c.LookMaxSpeed=2;Check(events==2,"same limits quiet");
        c.CaptureSerializedState();InspectorEdit(c,"lookMaxDistance",0f);InspectorEdit(c,"lookMaxSpeed",0f);
        Check(events==4&&c.LookMaxDistance==0&&c.LookMaxSpeed==0,"both Inspector fields participate in captured state");
        c.LookMaxSpeed=float.NaN;Check(float.IsNaN(c.LookMaxSpeed),"invalid user setting not silently repaired");
    }

}
