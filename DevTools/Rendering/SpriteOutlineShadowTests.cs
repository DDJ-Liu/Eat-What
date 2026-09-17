using System;
using System.Reflection;
using EatWhat.Tools.Rendering;
using UnityEngine;

public static class SpriteOutlineShadowTests
{
    private static int assertions;

    public static string Run()
    {
        TestCompatibilityAndDefaults();
        TestFiniteClamps();
        TestOutlineAlphaPropagation();
        TestPropertyBlockPreservationAndDisable();
        TestSpriteDirtyTracking();
        TestExplicitGroupCreationAndPropagation();
        TestIndependentMergeModesAndDefaults();
        TestFormalBackendOwnershipMatrix();
        TestFourCompositionCombinations();
        return "PASS: " + assertions + " runtime contract assertions";
    }

    private static void TestCompatibilityAndDefaults()
    {
        Assert(typeof(SpriteOutline2D).GetMethod("Configure", new[] { typeof(Material), typeof(Color), typeof(float) }) != null,
            "SpriteOutline2D old Configure API is missing.");
        Assert(typeof(SpriteOutlineGroup2D).GetMethod("Configure", new[] { typeof(Material), typeof(Color), typeof(float) }) != null,
            "SpriteOutlineGroup2D old Configure API is missing.");

        GameObject gameObject = new GameObject();
        gameObject.AddComponent<SpriteRenderer>();
        SpriteOutline2D outline = gameObject.AddComponent<SpriteOutline2D>();
        Assert(!outline.ShadowEnabled, "Shadow must default to disabled.");
        AssertEqual(0f, outline.ShadowOpacity, "Shadow opacity default");
        AssertEqual(0f, outline.ShadowOffset.x, "Shadow offset X default");
        AssertEqual(0f, outline.ShadowOffset.y, "Shadow offset Y default");
        AssertEqual(0f, outline.ShadowBlur, "Shadow blur default");
    }

    private static void TestFiniteClamps()
    {
        GameObject gameObject = new GameObject();
        gameObject.AddComponent<SpriteRenderer>();
        SpriteOutline2D outline = gameObject.AddComponent<SpriteOutline2D>();

        outline.ConfigureShadow(
            true,
            new Color(float.NaN, float.PositiveInfinity, 9f, float.NegativeInfinity),
            new Vector2(float.NaN, 999f),
            float.PositiveInfinity,
            -7f);

        Assert(outline.ShadowEnabled, "Valid enable flag must be retained.");
        AssertEqual(0f, outline.ShadowColor.r, "NaN color fallback");
        AssertEqual(0f, outline.ShadowColor.g, "Infinite color fallback");
        AssertEqual(8f, outline.ShadowColor.b, "HDR color upper clamp");
        AssertEqual(0f, outline.ShadowColor.a, "Infinite alpha fallback");
        AssertEqual(0f, outline.ShadowOffset.x, "NaN offset fallback");
        AssertEqual(128f, outline.ShadowOffset.y, "Offset upper clamp");
        AssertEqual(0f, outline.ShadowOpacity, "Infinite opacity fallback");
        AssertEqual(0f, outline.ShadowBlur, "Blur lower clamp");

        outline.Thickness = float.NaN;
        AssertEqual(0f, outline.Thickness, "Outline NaN fallback");
        outline.Thickness = 100f;
        AssertEqual(100f, outline.Thickness, "Legacy outline upper-range behavior");
    }

    private static void TestPropertyBlockPreservationAndDisable()
    {
        int foreignId = Shader.PropertyToID("_ForeignProperty");
        int thicknessId = Shader.PropertyToID("_OutlineThickness");
        int shadowEnabledId = Shader.PropertyToID("_ShadowEnabled");
        int shadowOpacityId = Shader.PropertyToID("_ShadowOpacity");

        GameObject gameObject = new GameObject();
        SpriteRenderer renderer = gameObject.AddComponent<SpriteRenderer>();
        MaterialPropertyBlock seed = new MaterialPropertyBlock();
        seed.SetFloat(foreignId, 73f);
        renderer.SetPropertyBlock(seed);

        SpriteOutline2D outline = gameObject.AddComponent<SpriteOutline2D>();
        outline.Configure(new Material(), new Color(1f, 0f, 0f, 1f), 7f);
        outline.ConfigureShadow(true, new Color(0f, 0f, 0f, 1f), new Vector2(3f, -2f), 0.75f, 2f);

        MaterialPropertyBlock observed = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(observed);
        AssertEqual(73f, observed.GetFloat(foreignId), "Foreign MaterialPropertyBlock value");
        AssertEqual(7f, observed.GetFloat(thicknessId), "Enabled outline thickness");
        AssertEqual(1f, observed.GetFloat(shadowEnabledId), "Enabled shadow flag");
        AssertEqual(0.75f, observed.GetFloat(shadowOpacityId), "Enabled shadow opacity");

        Invoke(outline, "OnDisable");
        renderer.GetPropertyBlock(observed);
        AssertEqual(73f, observed.GetFloat(foreignId), "Foreign value after disable");
        AssertEqual(0f, observed.GetFloat(thicknessId), "Disabled outline thickness");
        AssertEqual(0f, observed.GetFloat(shadowEnabledId), "Disabled shadow flag");
        AssertEqual(0f, observed.GetFloat(shadowOpacityId), "Disabled shadow opacity");

        Invoke(outline, "OnEnable");
        renderer.GetPropertyBlock(observed);
        AssertEqual(7f, observed.GetFloat(thicknessId), "Re-enabled outline thickness");
        AssertEqual(1f, observed.GetFloat(shadowEnabledId), "Re-enabled shadow flag");
        AssertEqual(0.75f, observed.GetFloat(shadowOpacityId), "Re-enabled shadow opacity");
    }

    private static void TestOutlineAlphaPropagation()
    {
        int outlineColorId = Shader.PropertyToID("_OutlineColor");
        int thicknessId = Shader.PropertyToID("_OutlineThickness");
        int shadowOpacityId = Shader.PropertyToID("_ShadowOpacity");

        GameObject localObject = new GameObject();
        SpriteRenderer localRenderer = localObject.AddComponent<SpriteRenderer>();
        SpriteOutline2D localOutline = localObject.AddComponent<SpriteOutline2D>();
        localOutline.Configure(new Material(), new Color(2f, 0.5f, 0.25f, 0f), 7f);
        localOutline.ConfigureShadow(true, Color.black, Vector2.zero, 0.4f, 1f);

        MaterialPropertyBlock observed = new MaterialPropertyBlock();
        localRenderer.GetPropertyBlock(observed);
        AssertEqual(0f, observed.GetColor(outlineColorId).a, "Local outline alpha zero propagation");
        AssertEqual(7f, observed.GetFloat(thicknessId), "Outline alpha must not change thickness");
        AssertEqual(0.4f, observed.GetFloat(shadowOpacityId), "Outline alpha must not change shadow opacity");

        localOutline.OutlineColor = new Color(2f, 0.5f, 0.25f, 0.5f);
        localRenderer.GetPropertyBlock(observed);
        AssertEqual(0.5f, observed.GetColor(outlineColorId).a, "Local outline alpha half propagation");
        AssertEqual(2f, observed.GetColor(outlineColorId).r, "HDR outline RGB must be preserved");

        localOutline.OutlineColor = new Color(2f, 0.5f, 0.25f, 1f);
        localRenderer.GetPropertyBlock(observed);
        AssertEqual(1f, observed.GetColor(outlineColorId).a, "Local outline alpha one propagation");

        GameObject root = new GameObject();
        SpriteOutlineGroup2D group = root.AddComponent<SpriteOutlineGroup2D>();
        GameObject followingChild = new GameObject();
        root.AddChild(followingChild);
        followingChild.AddComponent<SpriteRenderer>();
        SpriteOutline2D following = followingChild.AddComponent<SpriteOutline2D>();
        following.SetOverrideGroup(true);
        GameObject localChild = new GameObject();
        root.AddChild(localChild);
        localChild.AddComponent<SpriteRenderer>();
        SpriteOutline2D independent = localChild.AddComponent<SpriteOutline2D>();
        independent.Configure(null, new Color(0.1f, 0.2f, 0.3f, 0.25f), 3f);
        independent.SetOverrideGroup(false);

        group.Configure(null, new Color(1.5f, 0.4f, 0.2f, 0.5f), 9f);
        AssertEqual(0.5f, following.OutlineColor.a, "Group outline alpha propagation");
        AssertEqual(1.5f, following.OutlineColor.r, "Group HDR outline RGB propagation");
        AssertEqual(9f, following.Thickness, "Group outline thickness remains independent from alpha");
        AssertEqual(0.25f, independent.OutlineColor.a, "Local override alpha isolation");
        AssertEqual(3f, independent.Thickness, "Local override thickness isolation");

        var defaults = ScriptableObject.CreateInstance<SpriteOutlineDefaults>();
        defaults.ConfigureGeneric(null, new Color(3f, 0.5f, 0.25f, 0.5f), 6f);
        GameObject defaultObject = new GameObject();
        defaultObject.AddComponent<SpriteRenderer>();
        SpriteOutline2D defaultOutline = defaultObject.AddComponent<SpriteOutline2D>();
        Assert(defaults.ApplyTo(defaultOutline, SpriteOutlineDefaults.GenericProfileId),
            "HDR alpha defaults must be explicitly applicable.");
        AssertEqual(0.5f, defaultOutline.OutlineColor.a, "Defaults outline alpha propagation");
        AssertEqual(3f, defaultOutline.OutlineColor.r, "Defaults HDR outline RGB propagation");
    }

    private static void TestSpriteDirtyTracking()
    {
        GameObject gameObject = new GameObject();
        SpriteRenderer renderer = gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = new Sprite();
        SpriteOutline2D outline = gameObject.AddComponent<SpriteOutline2D>();
        outline.Configure(null, new Color(1f, 1f, 1f, 1f), 3f);

        renderer.ResetPropertyBlockCounters();
        Invoke(outline, "LateUpdate");
        AssertEqual(0, renderer.SetPropertyBlockCount, "Unchanged Sprite write count");

        renderer.sprite = new Sprite();
        Invoke(outline, "LateUpdate");
        AssertEqual(1, renderer.SetPropertyBlockCount, "Changed Sprite write count");

        renderer.sprite = null;
        Invoke(outline, "LateUpdate");
        AssertEqual(2, renderer.SetPropertyBlockCount, "Null Sprite transition write count");
        Invoke(outline, "LateUpdate");
        AssertEqual(2, renderer.SetPropertyBlockCount, "Stable null Sprite write count");
    }

    private static void TestExplicitGroupCreationAndPropagation()
    {
        GameObject root = new GameObject();
        GameObject child = new GameObject();
        root.AddChild(child);
        child.AddComponent<SpriteRenderer>();
        SpriteOutlineGroup2D group = root.AddComponent<SpriteOutlineGroup2D>();

        group.Configure(new Material(), new Color(0.5f, 0.4f, 0.3f, 1f), 5f);
        group.ConfigureShadow(true, new Color(0.1f, 0.2f, 0.3f, 0.8f), new Vector2(4f, -3f), 0.6f, 2f);
        AssertEqual(0, child.GetComponents<SpriteOutline2D>().Length, "Group configuration must not auto-create members");
        AssertEqual(1, group.CreateMissingMembers(), "Explicit child component creation count");
        AssertEqual(0, group.CreateMissingMembers(), "Repeated explicit creation count");
        group.ApplyToChildren();
        group.ApplyToChildren();

        SpriteOutline2D[] outlines = child.GetComponents<SpriteOutline2D>();
        AssertEqual(1, outlines.Length, "Idempotent child component count");
        AssertEqual(5f, outlines[0].Thickness, "Group outline propagation");
        Assert(outlines[0].ShadowEnabled, "Group shadow enable propagation.");
        AssertEqual(4f, outlines[0].ShadowOffset.x, "Group shadow offset X propagation");
        AssertEqual(-3f, outlines[0].ShadowOffset.y, "Group shadow offset Y propagation");
        AssertEqual(0.6f, outlines[0].ShadowOpacity, "Group shadow opacity propagation");
        AssertEqual(2f, outlines[0].ShadowBlur, "Group shadow blur propagation");
        Assert(outlines[0].OverrideGroup, "Explicitly created members must follow group parameters.");
        SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
        renderer.ResetPropertyBlockCounters();
        group.ApplyToChildren();
        group.ApplyToChildren();
        AssertEqual(0, renderer.SetPropertyBlockCount, "Repeated unchanged group synchronization write count");
    }

    private static void TestIndependentMergeModesAndDefaults()
    {
        Assert(!SpriteOutline2D.ResolveMergeMode(OutlineMergeOverride.FollowGroup, false),
            "FollowGroup must observe false.");
        Assert(SpriteOutline2D.ResolveMergeMode(OutlineMergeOverride.FollowGroup, true),
            "FollowGroup must observe true.");
        Assert(SpriteOutline2D.ResolveMergeMode(OutlineMergeOverride.ForceOn, false),
            "ForceOn must override false.");
        Assert(!SpriteOutline2D.ResolveMergeMode(OutlineMergeOverride.ForceOff, true),
            "ForceOff must override true.");

        var defaults = ScriptableObject.CreateInstance<SpriteOutlineDefaults>();
        defaults.ConfigureGeneric(null, new Color(0.2f, 0.3f, 0.4f, 1f), 8f);
        var gameObject = new GameObject();
        gameObject.AddComponent<SpriteRenderer>();
        var outline = gameObject.AddComponent<SpriteOutline2D>();
        Assert(defaults.ApplyTo(outline, SpriteOutlineDefaults.GenericProfileId),
            "Generic defaults must be explicitly applicable.");
        AssertEqual(8f, outline.Thickness, "One-shot generic default thickness");
        outline.Thickness = 3f;
        Invoke(outline, "OnEnable");
        Invoke(outline, "OnValidate");
        AssertEqual(3f, outline.Thickness, "Lifecycle must not reapply defaults");
    }

    private static void TestFourCompositionCombinations()
    {
        const float spriteAlpha = 0.5f;
        const float outlineCoverage = 0.25f;
        const float shadowAlpha = 0.4f;

        AssertEqual(0.5f, CompositeAlpha(spriteAlpha, 0f, 0f), "Original-only composition");
        AssertEqual(0.75f, CompositeAlpha(spriteAlpha, outlineCoverage, 0f), "Outline-only composition");
        AssertEqual(0.7f, CompositeAlpha(spriteAlpha, 0f, shadowAlpha), "Shadow-only composition");
        AssertEqual(0.85f, CompositeAlpha(spriteAlpha, outlineCoverage, shadowAlpha), "Outline-plus-shadow composition");
    }

    private static void TestFormalBackendOwnershipMatrix()
    {
        int thicknessId = Shader.PropertyToID("_OutlineThickness");
        int shadowEnabledId = Shader.PropertyToID("_ShadowEnabled");
        GameObject root = new GameObject();
        SpriteOutlineGroup2D group = root.AddComponent<SpriteOutlineGroup2D>();
        var backend = root.AddComponent<SpriteOutlineMergeRenderer2D>();
        GameObject child = new GameObject();
        root.AddChild(child);
        SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
        var adapter = child.AddComponent<SpriteOutlineMergeMember2D>();
        SpriteOutline2D outline = child.AddComponent<SpriteOutline2D>();
        outline.Configure(new Material(), new Color(1f, 0f, 1f, 1f), 7f);
        outline.ConfigureShadow(true, Color.black, new Vector2(2f, -2f), 0.5f, 1f);

        group.MergeOutlineEnabled = true;
        group.MergeShadowEnabled = true;
        outline.SetOverrideGroup(false);
        outline.SetMergeModes(OutlineMergeOverride.FollowGroup, OutlineMergeOverride.ForceOff);
        bool mergedOutline;
        bool mergedShadow;
        outline.ResolveActualMergeOwnership(out mergedOutline, out mergedShadow);
        Assert(mergedOutline && !mergedShadow,
            "Local parameters + FollowGroup outline must merge while ForceOff shadow stays independent.");
        Assert(!adapter.FollowGroupOutlineParameters,
            "Local parameter ownership must not be inferred from FollowGroup merge mode.");
        MaterialPropertyBlock observed = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(observed);
        AssertEqual(0f, observed.GetFloat(thicknessId), "Merged outline has one backend owner");
        AssertEqual(1f, observed.GetFloat(shadowEnabledId), "ForceOff shadow remains independent");

        group.MergeOutlineEnabled = false;
        group.MergeShadowEnabled = false;
        outline.SetOverrideGroup(true);
        outline.SetMergeModes(OutlineMergeOverride.ForceOn, OutlineMergeOverride.FollowGroup);
        outline.ResolveActualMergeOwnership(out mergedOutline, out mergedShadow);
        Assert(mergedOutline && !mergedShadow,
            "Group parameters + ForceOn outline must merge while FollowGroup observes group-off shadow.");
        Assert(adapter.FollowGroupOutlineParameters,
            "Group parameter ownership must remain true when merge mode is ForceOn.");

        backend.Ready = false;
        Invoke(outline, "LateUpdate");
        renderer.GetPropertyBlock(observed);
        AssertEqual(outline.Thickness, observed.GetFloat(thicknessId),
            "Independent outline recovers when backend is not ready");
        AssertEqual(1f, observed.GetFloat(shadowEnabledId),
            "Independent shadow remains enabled while backend is not ready");
    }

    private static float CompositeAlpha(float spriteAlpha, float outlineCoverage, float shadowAlpha)
    {
        float foregroundAlpha = Math.Min(1f, Math.Max(0f, spriteAlpha + outlineCoverage));
        return foregroundAlpha + shadowAlpha * (1f - foregroundAlpha);
    }

    private static void Invoke(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (method == null)
            throw new InvalidOperationException("Missing lifecycle method: " + methodName);
        method.Invoke(target, null);
    }

    private static void Assert(bool condition, string message)
    {
        assertions++;
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static void AssertEqual(float expected, float actual, string label)
    {
        Assert(Math.Abs(expected - actual) < 0.0001f, label + ": expected " + expected + ", actual " + actual + ".");
    }

    private static void AssertEqual(int expected, int actual, string label)
    {
        Assert(expected == actual, label + ": expected " + expected + ", actual " + actual + ".");
    }
}
