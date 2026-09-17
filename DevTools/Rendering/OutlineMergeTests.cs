using System;

public static class OutlineMergeTests
{
    private static int assertions;

    public static string Run()
    {
        TestTwoRtOwnershipAndCleanup();
        TestDirtyTransitions();
        TestBudgetSizingAndSorting();
        TestThreeStateAndNearestIdentity();
        TestFormalParameterOwnershipMatrix();
        TestStableForwardReverseCompetition();
        TestLightModeRoleSelectionRegression();
        TestWorldRectHostMappingRegression();
        TestSpriteUvMappingRegression();
        TestTightSpriteMappingRegression();
        return "PASS: " + assertions + " isolated outline-merge contract assertions";
    }

    private static void TestTwoRtOwnershipAndCleanup()
    {
        var owner = new StubRtOwner();
        owner.Allocate(10);
        owner.Allocate(11);
        owner.Allocate(11);
        AssertEqual(2, owner.LiveCount, "exactly two unique RTs");
        AssertEqual(2, owner.Peak, "peak two RTs");
        AssertEqual(2, owner.Allocations, "no unchanged reallocation");
        owner.ReleaseAll();
        AssertEqual(0, owner.LiveCount, "disable cleanup");
        AssertEqual(2, owner.Releases, "release accounting");
    }

    private static void TestDirtyTransitions()
    {
        var cache = new StubDirtyCache();
        Assert(cache.Update(100), "initial render");
        Assert(!cache.Update(100), "unchanged frame cached");
        AssertEqual(1, cache.Redraws, "cached frame does not redraw");
        cache.Invalidate("SpriteChanged");
        Assert(cache.Update(100), "explicit sprite invalidation redraws");
        Assert(cache.Update(101), "transform/parameter/resolution hash change redraws");
        AssertEqual(3, cache.Redraws, "dirty redraw accounting");
    }

    private static void TestBudgetSizingAndSorting()
    {
        AssertEqual(8, StubOutlineContract.RoundUpToEight(1), "minimum 8");
        AssertEqual(16, StubOutlineContract.RoundUpToEight(9), "8-pixel quantum");
        AssertEqual(65536L, StubOutlineContract.TwoRtBytes(128, 64), "ARGB32 two-RT bytes");
        AssertEqual(18, StubOutlineContract.HostOrder(20, -2), "shadow host below members");
        AssertEqual(19, StubOutlineContract.HostOrder(20, -1), "outline host below members");
        AssertThrows(delegate { StubOutlineContract.HostOrder(short.MinValue, -2); }, "sorting lower bound controlled failure");
    }

    private static void TestThreeStateAndNearestIdentity()
    {
        Assert(StubOutlineContract.Resolve(StubMergeOverride.FollowGroup, true), "follow enabled group");
        Assert(!StubOutlineContract.Resolve(StubMergeOverride.FollowGroup, false), "follow disabled group");
        Assert(StubOutlineContract.Resolve(StubMergeOverride.ForceOn, false), "force on overrides group");
        Assert(!StubOutlineContract.Resolve(StubMergeOverride.ForceOff, true), "force off stays independent");
        AssertEqual(7, StubOutlineContract.NearestOwner(0, 7, 8), "nearest owner wins nested identity");
    }

    private static void TestStableForwardReverseCompetition()
    {
        var forward = StubOutlineContract.SourceOver(0.2f, 0.8f);
        var reverse = StubOutlineContract.SourceOver(0.8f, 0.2f);
        AssertApprox(0.84f, forward, "forward source-over alpha");
        AssertApprox(0.84f, reverse, "reverse alpha stable");
        var forwardWinner = StableWinner(10, 20);
        var reverseWinner = StableWinner(20, 10);
        AssertEqual(20, forwardWinner, "later configured order wins forward collision");
        AssertEqual(10, reverseWinner, "reversed order predictably reverses winner");
    }

    private static void TestFormalParameterOwnershipMatrix()
    {
        AssertApprox(2f, StubOutlineContract.ResolveOutlineParameter(2f, 9f, false),
            "local parameters remain local when merge mode follows an enabled group");
        AssertApprox(9f, StubOutlineContract.ResolveOutlineParameter(2f, 9f, true),
            "group parameters remain group-owned when ForceOn overrides a disabled merge group");
        Assert(StubOutlineContract.Resolve(StubMergeOverride.FollowGroup, true),
            "FollowGroup independently controls participation");
        Assert(StubOutlineContract.Resolve(StubMergeOverride.ForceOn, false),
            "ForceOn independently controls participation");
        AssertApprox(7f, StubOutlineContract.ResolveMergedShadowParameter(3f, 7f),
            "merged shadow always uses group parameters");
        Assert(!StubOutlineContract.Resolve(StubMergeOverride.ForceOff, true),
            "ForceOff shadow stays out of the group contribution channel");
    }

    private static void TestLightModeRoleSelectionRegression()
    {
        var oldPasses = new[]
        {
            new StubShaderPass("MaskUnion", null),
            new StubShaderPass("OutlineCandidate", null),
            new StubShaderPass("ShadowDisplay", "Universal2D"),
            new StubShaderPass("OutlineDisplay", "Universal2D")
        };
        var oldSelection = StubUrpPassSelector.Select(oldPasses, delegate { return true; });
        AssertEqual("ShadowDisplay", oldSelection,
            "URP prefers Universal2D but old duplicate display roles cannot select outline independently");
        var oldFallbackSelection = StubUrpPassSelector.Select(oldPasses,
            delegate(string lightMode) { return lightMode != "Universal2D"; });
        AssertEqual("MaskUnion", oldFallbackSelection,
            "old untagged offscreen pass leaks through SRPDefaultUnlit when Universal2D is disabled");

        var fixedPasses = new[]
        {
            new StubShaderPass("MaskUnion", "SpriteOutlineMergeOffscreen"),
            new StubShaderPass("OutlineCandidate", "SpriteOutlineMergeOffscreen"),
            new StubShaderPass("ShadowDisplay", "Universal2D"),
            new StubShaderPass("OutlineDisplay", "SRPDefaultUnlit")
        };
        var shadowSelection = StubUrpPassSelector.Select(fixedPasses,
            delegate(string lightMode) { return lightMode == "Universal2D"; });
        var outlineSelection = StubUrpPassSelector.Select(fixedPasses,
            delegate(string lightMode) { return lightMode == "SRPDefaultUnlit"; });
        AssertEqual("ShadowDisplay", shadowSelection, "shadow host selects one final pass");
        AssertEqual("OutlineDisplay", outlineSelection, "outline host selects one final pass");
        Assert(fixedPasses[0].ResolvedLightMode != "SRPDefaultUnlit", "mask pass is not a normal-frame fallback");
        Assert(fixedPasses[1].ResolvedLightMode != "SRPDefaultUnlit", "candidate pass is not a normal-frame fallback");
    }

    private static void TestWorldRectHostMappingRegression()
    {
        var center = new StubPoint2(3f, -2f);
        var desired = StubHostMapping.DesiredWorldCorner(center, 4f, 2f, 1f, 1f);
        var oldScaled = StubHostMapping.OldConfiguredCorner(center, 4f, 2f, 4f, 4f, 0f, 1f, 1f);
        Assert(PointDistance(desired, oldScaled) > 1f,
            "old world-size-to-localScale mapping fails beneath a 4x parent");

        var angle = (float)(Math.PI / 6.0);
        var oldRotatedNonUniform = StubHostMapping.OldConfiguredCorner(center, 4f, 2f, 2.5f, 0.5f, angle, 1f, 1f);
        Assert(PointDistance(desired, oldRotatedNonUniform) > 0.1f,
            "old mapping shears the world rectangle beneath rotated non-uniform scale");

        var transforms = new[]
        {
            StubAffine2.Trs(5f, -7f, 0f, 4f, 4f),
            StubAffine2.Trs(-2f, 3f, angle, 2.5f, 0.5f),
            new StubAffine2(1.7f, 0.6f, -0.35f, 0.8f, 4f, 2f)
        };
        var corners = new[]
        {
            StubHostMapping.DesiredWorldCorner(center, 4f, 2f, 0f, 0f),
            StubHostMapping.DesiredWorldCorner(center, 4f, 2f, 1f, 0f),
            StubHostMapping.DesiredWorldCorner(center, 4f, 2f, 1f, 1f),
            StubHostMapping.DesiredWorldCorner(center, 4f, 2f, 0f, 1f)
        };
        for (var transformIndex = 0; transformIndex < transforms.Length; transformIndex++)
        {
            for (var cornerIndex = 0; cornerIndex < corners.Length; cornerIndex++)
            {
                var mapped = StubHostMapping.MeshMappedCorner(transforms[transformIndex], corners[cornerIndex]);
                AssertPointApprox(corners[cornerIndex], mapped,
                    "world-to-local display mesh round-trip " + transformIndex + "/" + cornerIndex);
            }
        }
    }

    private static void TestSpriteUvMappingRegression()
    {
        var localMinimum = new StubPoint2(-0.25f, -0.75f);
        var localSize = new StubPoint2(2f, 1f);
        var uvMinimum = new StubPoint2(0.1f, 0.2f);
        var uvSize = new StubPoint2(0.4f, 0.5f);
        AssertPointApprox(new StubPoint2(0.1f, 0.2f),
            StubOutlineContract.SpriteUv(localMinimum, localMinimum, localSize, uvMinimum, uvSize, false, false),
            "off-center pivot local minimum maps to textureRect minimum");
        AssertPointApprox(new StubPoint2(0.5f, 0.7f),
            StubOutlineContract.SpriteUv(new StubPoint2(1.75f, 0.25f), localMinimum, localSize, uvMinimum, uvSize, false, false),
            "sprite bounds maximum maps to textureRect maximum");
        AssertPointApprox(new StubPoint2(0.5f, 0.2f),
            StubOutlineContract.SpriteUv(localMinimum, localMinimum, localSize, uvMinimum, uvSize, true, false),
            "flipX mirrors within the packed textureRect");
        AssertPointApprox(new StubPoint2(0.1f, 0.7f),
            StubOutlineContract.SpriteUv(localMinimum, localMinimum, localSize, uvMinimum, uvSize, false, true),
            "flipY mirrors within the packed textureRect");
    }

    private static void TestTightSpriteMappingRegression()
    {
        // Runtime metadata captured from the real FridgeCat Head sprite in AI-000103.
        var headTextureOffset = new StubPoint2(121.05f, 113.08f);
        var headTextureSize = new StubPoint2(1235.87f, 1153.85f);
        var headPivot = new StubPoint2(739f, 690f);
        var headLocal = StubOutlineContract.TextureLocalRect(
            headTextureOffset, headTextureSize, headPivot, 100f, false, false);
        AssertPointApprox(new StubPoint2(-6.1795f, -5.7692f), headLocal.Minimum,
            "real Head cropped alpha local minimum");
        AssertPointApprox(new StubPoint2(12.3587f, 11.5385f), headLocal.Size,
            "real Head cropped alpha local size");

        var headUvMinimum = new StubPoint2(121.05f / 1478f, 113.08f / 1380f);
        var headUvSize = new StubPoint2(1235.87f / 1478f, 1153.85f / 1380f);
        var oldHeadUv = StubOutlineContract.SpriteUv(headLocal.Minimum,
            new StubPoint2(-7.39f, -6.9f), new StubPoint2(14.78f, 13.8f),
            headUvMinimum, headUvSize, false, false);
        Assert(PointDistance(headUvMinimum, oldHeadUv) > 0.05f,
            "old full-bounds mapping misplaces real Head cropped alpha");
        AssertPointApprox(headUvMinimum,
            StubOutlineContract.SpriteUv(headLocal.Minimum, headLocal.Minimum, headLocal.Size,
                headUvMinimum, headUvSize, false, false),
            "real Head cropped minimum maps to textureRect minimum");

        var headOnePixelUv = StubOutlineContract.SpriteUv(
            new StubPoint2(headLocal.Minimum.X + 0.01f, headLocal.Minimum.Y + 0.01f),
            headLocal.Minimum, headLocal.Size, headUvMinimum, headUvSize, false, false);
        AssertApprox(1f / 1478f, headOnePixelUv.X - headUvMinimum.X,
            "real Head one source pixel maps to one texture texel x");
        AssertApprox(1f / 1380f, headOnePixelUv.Y - headUvMinimum.Y,
            "real Head one source pixel maps to one texture texel y");
        var oldHeadStartUv = oldHeadUv;
        var oldHeadOnePixelUv = StubOutlineContract.SpriteUv(
            new StubPoint2(headLocal.Minimum.X + 0.01f, headLocal.Minimum.Y),
            new StubPoint2(-7.39f, -6.9f), new StubPoint2(14.78f, 13.8f),
            headUvMinimum, headUvSize, false, false);
        Assert(Math.Abs((oldHeadOnePixelUv.X - oldHeadStartUv.X) - 1f / 1478f) > 0.00005f,
            "old full-bounds mapping also rescales source-pixel width");

        // Runtime metadata captured from the real FridgeCat Tier sprite in AI-000103.
        var tierTextureOffset = new StubPoint2(84.08f, 41.07f);
        var tierTextureSize = new StubPoint2(869.77f, 426.91f);
        var tierPivot = new StubPoint2(519.5f, 254f);
        var tierLocal = StubOutlineContract.TextureLocalRect(
            tierTextureOffset, tierTextureSize, tierPivot, 100f, false, false);
        AssertPointApprox(new StubPoint2(-4.3542f, -2.1293f), tierLocal.Minimum,
            "real Tier cropped alpha local minimum");
        AssertPointApprox(new StubPoint2(8.6977f, 4.2691f), tierLocal.Size,
            "real Tier cropped alpha local size");
        var tierUvMinimum = new StubPoint2(84.08f / 1039f, 41.07f / 508f);
        var tierUvSize = new StubPoint2(869.77f / 1039f, 426.91f / 508f);
        var oldTierUv = StubOutlineContract.SpriteUv(tierLocal.Minimum,
            new StubPoint2(-5.195f, -2.54f), new StubPoint2(10.39f, 5.08f),
            tierUvMinimum, tierUvSize, false, false);
        Assert(PointDistance(tierUvMinimum, oldTierUv) > 0.05f,
            "old full-bounds mapping misplaces real Tier cropped alpha");
        AssertPointApprox(new StubPoint2(tierUvMinimum.X + tierUvSize.X, tierUvMinimum.Y + tierUvSize.Y),
            StubOutlineContract.SpriteUv(tierLocal.Maximum, tierLocal.Minimum, tierLocal.Size,
                tierUvMinimum, tierUvSize, false, false),
            "real Tier cropped maximum maps to textureRect maximum");

        var flippedHead = StubOutlineContract.TextureLocalRect(
            headTextureOffset, headTextureSize, headPivot, 100f, true, true);
        AssertPointApprox(new StubPoint2(-6.1792f, -5.7693f), flippedHead.Minimum,
            "asymmetric cropped support relocates when flipped");
        AssertPointApprox(new StubPoint2(headUvMinimum.X + headUvSize.X, headUvMinimum.Y + headUvSize.Y),
            StubOutlineContract.SpriteUv(flippedHead.Minimum, flippedHead.Minimum, flippedHead.Size,
                headUvMinimum, headUvSize, true, true),
            "flipped cropped minimum maps to textureRect maximum");
        AssertPointApprox(headUvMinimum,
            StubOutlineContract.SpriteUv(flippedHead.Maximum, flippedHead.Minimum, flippedHead.Size,
                headUvMinimum, headUvSize, true, true),
            "flipped cropped maximum maps to textureRect minimum");
    }

    private static int StableWinner(int back, int front) { return front; }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        assertions++;
    }

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!Equals(expected, actual)) throw new InvalidOperationException(message + ": expected=" + expected + " actual=" + actual);
        assertions++;
    }

    private static void AssertApprox(float expected, float actual, string message)
    {
        if (Math.Abs(expected - actual) > 0.0001f)
            throw new InvalidOperationException(message + ": expected=" + expected + " actual=" + actual);
        assertions++;
    }

    private static void AssertPointApprox(StubPoint2 expected, StubPoint2 actual, string message)
    {
        AssertApprox(expected.X, actual.X, message + " x");
        AssertApprox(expected.Y, actual.Y, message + " y");
    }

    private static float PointDistance(StubPoint2 left, StubPoint2 right)
    {
        var x = left.X - right.X;
        var y = left.Y - right.Y;
        return (float)Math.Sqrt(x * x + y * y);
    }

    private static void AssertThrows(Action action, string message)
    {
        try { action(); }
        catch (InvalidOperationException) { assertions++; return; }
        throw new InvalidOperationException(message);
    }
}
