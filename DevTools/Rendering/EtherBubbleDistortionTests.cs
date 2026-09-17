using System;
using System.Reflection;
using EatWhat.Tools.Debugging;
using EatWhat.Tools.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

public static class EtherBubbleDistortionTests
{
    private static int assertions;

    public static string Run()
    {
        TestCaptureOwnershipValidityAndRestore();
        TestVisualClampsAndPropertyBlockPreservation();
        TestBubbleLifecycle();
        TestEmitterCapPoolingAndMissingReferences();
        TestDriverSubscriptionIsIdempotent();
        return "PASS: " + assertions + " runtime contract assertions";
    }

    private static void TestCaptureOwnershipValidityAndRestore()
    {
        RenderTexture.ResetCounters();
        var mainObject = new GameObject("MainCamera");
        var main = mainObject.AddComponent<Camera>();
        main.pixelWidth = 800;
        main.pixelHeight = 450;
        main.depth = 4f;
        main.backgroundColor = new Color(0.1f, 0.2f, 0.3f, 1f);
        main.transform.position = new Vector3(3f, 2f, -10f);

        var backgroundObject = new GameObject("BackgroundCamera");
        var background = backgroundObject.AddComponent<Camera>();
        background.cullingMask = 1234;
        background.depth = -8f;
        background.enabled = false;
        background.transform.position = new Vector3(-9f, -8f, -7f);
        var originalTarget = new RenderTexture(4, 4, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
        background.targetTexture = originalTarget;

        var captureObject = new GameObject("Capture");
        var capture = captureObject.AddComponent<SceneColorCapture2D>();
        Invoke(capture, "OnEnable");
        var notifications = 0;
        capture.CaptureStateChanged += delegate { notifications++; };
        capture.Configure(main, background, 1 << 1, 1f, 1);

        AssertEqual(1, capture.OwnedRenderTextureCount, "single owned RT");
        AssertEqual(800, capture.CaptureWidth, "capture width");
        AssertEqual(450, capture.CaptureHeight, "capture height");
        AssertEqual(1, RenderTexture.CreatedCount, "initial RT allocation");
        capture.RefreshCaptureState();
        AssertEqual(1, RenderTexture.CreatedCount, "stable resolution does not reallocate");
        Assert(!capture.IsFrameValid, "capture remains invalid before SRP completion");

        Time.frameCount = 12;
        RenderPipelineManager.RaiseEnd(background);
        Assert(capture.IsFrameValid, "SRP completion establishes validity");
        AssertEqual(12, capture.LastCompletedFrame, "completed frame");
        AssertEqual(1, notifications, "validity notification");
        AssertEqual(main.depth - 1f, background.depth, "camera depth order");
        AssertEqual((1 << 1), background.cullingMask, "isolated capture mask");
        AssertEqual(main.transform.position.x, background.transform.position.x, "camera X sync");

        main.pixelWidth = 400;
        main.pixelHeight = 200;
        capture.RefreshCaptureState();
        AssertEqual(2, RenderTexture.CreatedCount, "resize reallocates once");
        AssertEqual(1, RenderTexture.ReleasedCount, "resize releases prior RT");
        AssertEqual(1, capture.OwnedRenderTextureCount, "resize keeps one owner");
        Assert(!capture.IsFrameValid, "resize invalidates old frame");

        capture.CaptureEnabled = false;
        Assert(!capture.IsFrameValid, "disabled capture invalid");
        Assert(!background.enabled, "disabled capture camera");
        Invoke(capture, "OnDisable");
        AssertEqual(0, capture.OwnedRenderTextureCount, "disable releases RT");
        AssertEqual(1234, background.cullingMask, "restore culling mask");
        AssertEqual(-8f, background.depth, "restore depth");
        Assert(background.targetTexture == originalTarget, "restore target texture");
        Assert(!background.enabled, "restore enabled state");
        AssertEqual(-9f, background.transform.position.x, "restore transform");
        AssertEqual(0, RenderPipelineManager.BeginSubscriberCount, "begin callback removed");
        AssertEqual(0, RenderPipelineManager.EndSubscriberCount, "end callback removed");
    }

    private static void TestVisualClampsAndPropertyBlockPreservation()
    {
        var capture = CreateValidCapture();
        var bubbleObject = new GameObject("Bubble");
        var renderer = bubbleObject.AddComponent<SpriteRenderer>();
        renderer.sprite = new Sprite();
        var foreignId = Shader.PropertyToID("_ForeignProperty");
        var captureValidId = Shader.PropertyToID("_CaptureValid");
        var distortionId = Shader.PropertyToID("_DistortionStrength");
        var rimAlphaId = Shader.PropertyToID("_RimAlpha");
        var seed = new MaterialPropertyBlock();
        seed.SetFloat(foreignId, 73f);
        renderer.SetPropertyBlock(seed);

        var bubble = bubbleObject.AddComponent<EtherBubbleDistortion2D>();
        Invoke(bubble, "OnEnable");
        bubble.Configure(new Material(), capture);
        var invalid = new EtherBubbleVisualSettings
        {
            distortionStrength = float.PositiveInfinity,
            waveFrequency = float.NaN,
            waveSpeed = 999f,
            flowSpeed = -999f,
            softness = -2f,
            rimColor = new Color(float.NaN, 99f, -3f, float.PositiveInfinity),
            rimIntensity = 99f,
            rimAlpha = -4f
        };
        bubble.ApplyVisualSettings(invalid);
        var sanitized = bubble.VisualSettings;
        AssertEqual(0f, sanitized.distortionStrength, "nonfinite strength fallback");
        AssertEqual(5f, sanitized.waveFrequency, "nonfinite frequency fallback");
        AssertEqual(8f, sanitized.waveSpeed, "wave speed upper clamp");
        AssertEqual(-4f, sanitized.flowSpeed, "flow speed lower clamp");
        AssertEqual(0.01f, sanitized.softness, "softness lower clamp");
        AssertEqual(0f, sanitized.rimColor.r, "rim NaN fallback");
        AssertEqual(8f, sanitized.rimColor.g, "rim HDR clamp");
        AssertEqual(0f, sanitized.rimColor.b, "rim lower clamp");
        AssertEqual(0f, sanitized.rimColor.a, "rim alpha nonfinite fallback");
        AssertEqual(4f, sanitized.rimIntensity, "rim intensity clamp");
        AssertEqual(0f, sanitized.rimAlpha, "rim alpha clamp");

        var observed = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(observed);
        AssertEqual(73f, observed.GetFloat(foreignId), "foreign MPB value preserved");
        AssertEqual(1f, observed.GetFloat(captureValidId), "valid capture property");
        AssertEqual(0f, observed.GetFloat(distortionId), "neutral strength property");
        AssertEqual(0f, observed.GetFloat(rimAlphaId), "neutral rim property");

        renderer.ResetPropertyBlockCounters();
        Invoke(bubble, "LateUpdate");
        Invoke(bubble, "LateUpdate");
        AssertEqual(0, renderer.SetPropertyBlockCount, "unchanged properties do not rewrite MPB");
        capture.CaptureEnabled = false;
        AssertEqual(1, renderer.SetPropertyBlockCount, "validity transition writes once");
        renderer.GetPropertyBlock(observed);
        AssertEqual(0f, observed.GetFloat(captureValidId), "invalid capture disables sample");
        AssertEqual(73f, observed.GetFloat(foreignId), "foreign MPB survives invalidation");
    }

    private static void TestBubbleLifecycle()
    {
        var bubbleObject = new GameObject("BubbleLifecycle");
        bubbleObject.AddComponent<SpriteRenderer>().sprite = new Sprite();
        var bubble = bubbleObject.AddComponent<EtherBubbleDistortion2D>();
        bubble.ApplyVisualSettings(EtherBubbleVisualSettings.Soft);
        bubble.Activate(null, null, new Vector3(1f, 2f, 0f), 0.5f, new Vector2(2f, 1f), 1f,
            EtherBubbleVisualSettings.Soft, true);
        bubble.Tick(0.25f);
        AssertEqual(1.5f, bubble.transform.position.x, "movement X");
        AssertEqual(2.25f, bubble.transform.position.y, "movement Y");
        AssertEqual(0.25f, bubble.Age, "age progression");
        bubble.SetPaused(true);
        bubble.Tick(0.5f);
        AssertEqual(0.25f, bubble.Age, "paused age");
        bubble.SetPaused(false);
        bubble.Tick(0.75f);
        Assert(!bubble.gameObject.activeSelf, "lifetime returns standalone bubble inactive");
    }

    private static void TestEmitterCapPoolingAndMissingReferences()
    {
        UnityEngine.Object.ResetInstantiateCount();
        var capture = CreateValidCapture();
        var mouth = new GameObject("Mouth").transform;
        mouth.position = new Vector3(-3f, 1f, 0f);
        var spawn = new GameObject("Spawn").transform;
        var prefabObject = new GameObject("BubblePrefab");
        prefabObject.AddComponent<SpriteRenderer>().sprite = new Sprite();
        var prefab = prefabObject.AddComponent<EtherBubbleDistortion2D>();
        var emitterObject = new GameObject("Emitter");
        var emitter = emitterObject.AddComponent<EtherBubbleEmitter2D>();
        Invoke(emitter, "Reset");
        emitter.Configure(mouth, spawn, prefab, capture);
        emitter.SetMaxBubbles(16);
        Invoke(emitter, "Awake");
        Invoke(emitter, "OnEnable");
        emitter.StopEmission();

        AssertEqual(16, emitter.Burst(16), "burst 16");
        AssertEqual(16, emitter.ActiveCount, "active cap");
        AssertEqual(16, emitter.PoolCount, "pool cap");
        AssertEqual(16, UnityEngine.Object.InstantiateCount, "initial pool allocations");
        AssertEqual(0, emitter.Burst(8), "cap rejects extra burst");
        emitter.Clear();
        AssertEqual(0, emitter.ActiveCount, "clear deactivates all");
        AssertEqual(8, emitter.Burst(8), "reuse burst 8");
        AssertEqual(16, emitter.PoolCount, "pool size stable on reuse");
        AssertEqual(16, UnityEngine.Object.InstantiateCount, "reuse performs no Instantiate");
        emitter.SetPaused(true);
        Assert(!emitter.Emit(), "pause blocks emission");
        emitter.SetPaused(false);
        emitter.SetEffectEnabled(false);
        Assert(!emitter.EffectEnabled, "effect toggle stored");

        var missing = new GameObject("MissingEmitter").AddComponent<EtherBubbleEmitter2D>();
        Invoke(missing, "Reset");
        Invoke(missing, "Awake");
        Assert(!missing.HasRequiredReferences, "missing references reported");
        Assert(!missing.Emit(), "missing references fail safely");
    }

    private static void TestDriverSubscriptionIsIdempotent()
    {
        var driverObject = new GameObject("Driver");
        var driver = driverObject.AddComponent<VisualEffectsLabDriver>();
        Invoke(driver, "OnEnable");
        Invoke(driver, "OnEnable");
        AssertEqual(1, InputManager.SubscriberCount, "driver has one input subscription");
        Assert(InputManager.Raise(UnityEngine.InputSystem.Key.Digit1), "driver consumes mapped key");
        Invoke(driver, "OnDisable");
        AssertEqual(0, InputManager.SubscriberCount, "driver removes input subscription");
    }

    private static SceneColorCapture2D CreateValidCapture()
    {
        var mainObject = new GameObject("Main");
        var main = mainObject.AddComponent<Camera>();
        var backgroundObject = new GameObject("Background");
        var background = backgroundObject.AddComponent<Camera>();
        var captureObject = new GameObject("Capture");
        var capture = captureObject.AddComponent<SceneColorCapture2D>();
        Invoke(capture, "OnEnable");
        capture.Configure(main, background, 1 << 1, 1f, 1);
        Time.frameCount++;
        RenderPipelineManager.RaiseEnd(background);
        Assert(capture.IsFrameValid, "test capture valid");
        return capture;
    }

    private static void Invoke(object target, string methodName)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (method == null) throw new InvalidOperationException("Missing lifecycle method: " + methodName);
        method.Invoke(target, null);
    }

    private static void Assert(bool condition, string message)
    {
        assertions++;
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void AssertEqual(int expected, int actual, string label)
    {
        Assert(expected == actual, label + ": expected " + expected + ", actual " + actual + ".");
    }

    private static void AssertEqual(float expected, float actual, string label)
    {
        Assert(Math.Abs(expected - actual) < 0.0001f, label + ": expected " + expected + ", actual " + actual + ".");
    }
}
