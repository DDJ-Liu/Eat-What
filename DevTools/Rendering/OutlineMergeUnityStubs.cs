using System;
using System.Collections.Generic;

internal enum StubMergeOverride { FollowGroup, ForceOn, ForceOff }

internal sealed class StubRtOwner
{
    private readonly HashSet<int> live = new HashSet<int>();
    public int Peak { get; private set; }
    public int Allocations { get; private set; }
    public int Releases { get; private set; }
    public int LiveCount { get { return live.Count; } }
    public void Allocate(int id)
    {
        if (live.Add(id)) Allocations++;
        Peak = Math.Max(Peak, live.Count);
        if (Peak > 2) throw new InvalidOperationException("two-RT budget exceeded");
    }
    public void Release(int id) { if (live.Remove(id)) Releases++; }
    public void ReleaseAll() { foreach (var id in new List<int>(live)) Release(id); }
}

internal sealed class StubDirtyCache
{
    private int state;
    private bool valid;
    public int Redraws { get; private set; }
    public string Reason { get; private set; }
    public bool Update(int next)
    {
        if (valid && state == next) { Reason = "CachedUnchanged"; return false; }
        state = next;
        valid = true;
        Redraws++;
        Reason = "StateOrResolutionChanged";
        return true;
    }
    public void Invalidate(string reason) { valid = false; Reason = reason; }
}

internal static class StubOutlineContract
{
    public static int RoundUpToEight(int value) { return Math.Max(8, ((Math.Max(1, value) + 7) / 8) * 8); }
    public static long TwoRtBytes(int width, int height) { return (long)width * height * 4L * 2L; }
    public static bool Resolve(StubMergeOverride mode, bool group)
    {
        return mode == StubMergeOverride.ForceOn || (mode == StubMergeOverride.FollowGroup && group);
    }
    public static float ResolveOutlineParameter(float localValue, float groupValue, bool overrideGroup)
    {
        return overrideGroup ? groupValue : localValue;
    }
    public static float ResolveMergedShadowParameter(float localValue, float groupValue)
    {
        return groupValue;
    }
    public static int HostOrder(int minimum, int offset)
    {
        if (minimum < short.MinValue + 2) throw new InvalidOperationException("sorting underflow");
        return minimum + offset;
    }
    public static float SourceOver(float back, float front) { return front + back * (1f - front); }
    public static int NearestOwner(params int[] ancestors)
    {
        for (var index = 0; index < ancestors.Length; index++) if (ancestors[index] != 0) return ancestors[index];
        return 0;
    }
    public static StubPoint2 SpriteUv(StubPoint2 local, StubPoint2 localMinimum, StubPoint2 localSize,
        StubPoint2 uvMinimum, StubPoint2 uvSize, bool flipX, bool flipY)
    {
        var x = (local.X - localMinimum.X) / localSize.X;
        var y = (local.Y - localMinimum.Y) / localSize.Y;
        if (flipX) x = 1f - x;
        if (flipY) y = 1f - y;
        return new StubPoint2(uvMinimum.X + x * uvSize.X, uvMinimum.Y + y * uvSize.Y);
    }
    public static StubRect2 TextureLocalRect(StubPoint2 textureRectOffset, StubPoint2 textureRectSize,
        StubPoint2 pivot, float pixelsPerUnit, bool flipX, bool flipY)
    {
        var safePixelsPerUnit = Math.Max(1f, pixelsPerUnit);
        var size = new StubPoint2(textureRectSize.X / safePixelsPerUnit,
            textureRectSize.Y / safePixelsPerUnit);
        var minimum = new StubPoint2(
            (textureRectOffset.X - pivot.X) / safePixelsPerUnit,
            (textureRectOffset.Y - pivot.Y) / safePixelsPerUnit);
        if (flipX) minimum = new StubPoint2(-minimum.X - size.X, minimum.Y);
        if (flipY) minimum = new StubPoint2(minimum.X, -minimum.Y - size.Y);
        return new StubRect2(minimum, size);
    }
}

internal struct StubPoint2
{
    public readonly float X;
    public readonly float Y;
    public StubPoint2(float x, float y) { X = x; Y = y; }
}

internal struct StubRect2
{
    public readonly StubPoint2 Minimum;
    public readonly StubPoint2 Size;
    public StubPoint2 Maximum { get { return new StubPoint2(Minimum.X + Size.X, Minimum.Y + Size.Y); } }
    public StubRect2(StubPoint2 minimum, StubPoint2 size) { Minimum = minimum; Size = size; }
}

internal sealed class StubAffine2
{
    private readonly float m00;
    private readonly float m01;
    private readonly float m10;
    private readonly float m11;
    private readonly float tx;
    private readonly float ty;

    public StubAffine2(float m00, float m01, float m10, float m11, float tx, float ty)
    {
        this.m00 = m00;
        this.m01 = m01;
        this.m10 = m10;
        this.m11 = m11;
        this.tx = tx;
        this.ty = ty;
    }

    public static StubAffine2 Trs(float tx, float ty, float radians, float scaleX, float scaleY)
    {
        var cosine = (float)Math.Cos(radians);
        var sine = (float)Math.Sin(radians);
        return new StubAffine2(cosine * scaleX, -sine * scaleY,
            sine * scaleX, cosine * scaleY, tx, ty);
    }

    public StubPoint2 TransformPoint(StubPoint2 value)
    {
        return new StubPoint2(m00 * value.X + m01 * value.Y + tx,
            m10 * value.X + m11 * value.Y + ty);
    }

    public StubPoint2 InverseTransformPoint(StubPoint2 value)
    {
        var determinant = m00 * m11 - m01 * m10;
        if (Math.Abs(determinant) < 0.000001f) throw new InvalidOperationException("singular transform");
        var x = value.X - tx;
        var y = value.Y - ty;
        return new StubPoint2((m11 * x - m01 * y) / determinant,
            (-m10 * x + m00 * y) / determinant);
    }
}

internal static class StubHostMapping
{
    public static StubPoint2 DesiredWorldCorner(StubPoint2 center, float width, float height, float u, float v)
    {
        return new StubPoint2(center.X + (u - 0.5f) * width, center.Y + (v - 0.5f) * height);
    }

    public static StubPoint2 OldConfiguredCorner(StubPoint2 center, float width, float height,
        float parentScaleX, float parentScaleY, float parentRotation, float u, float v)
    {
        var cosine = (float)Math.Cos(parentRotation);
        var sine = (float)Math.Sin(parentRotation);
        var x = (u - 0.5f) * width;
        var y = (v - 0.5f) * height;
        var localRotatedX = cosine * x + sine * y;
        var localRotatedY = -sine * x + cosine * y;
        var scaledX = parentScaleX * localRotatedX;
        var scaledY = parentScaleY * localRotatedY;
        return new StubPoint2(center.X + cosine * scaledX - sine * scaledY,
            center.Y + sine * scaledX + cosine * scaledY);
    }

    public static StubPoint2 MeshMappedCorner(StubAffine2 hostWorld, StubPoint2 desiredWorld)
    {
        return hostWorld.TransformPoint(hostWorld.InverseTransformPoint(desiredWorld));
    }
}

internal sealed class StubShaderPass
{
    public readonly string Name;
    public readonly string LightMode;
    public StubShaderPass(string name, string lightMode) { Name = name; LightMode = lightMode; }
    public string ResolvedLightMode { get { return string.IsNullOrEmpty(LightMode) ? "SRPDefaultUnlit" : LightMode; } }
}

internal static class StubUrpPassSelector
{
    private static readonly string[] Supported = { "Universal2D", "SRPDefaultUnlit" };

    public static string Select(StubShaderPass[] passes, Func<string, bool> isLightModeEnabled)
    {
        for (var supportedIndex = 0; supportedIndex < Supported.Length; supportedIndex++)
        {
            for (var passIndex = 0; passIndex < passes.Length; passIndex++)
            {
                var lightMode = passes[passIndex].ResolvedLightMode;
                if (lightMode == Supported[supportedIndex] && isLightModeEnabled(lightMode))
                    return passes[passIndex].Name;
            }
        }
        return null;
    }
}
