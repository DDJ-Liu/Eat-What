using System;
using EatWhat.LongLoop;
using EatWhat.LongLoop.Configuration;
using EatWhat.Toolkit;
using UnityEngine;

internal static class GroundSpaceLogicTests
{
    private static int count;
    private static void Near(float actual, float expected, string label)
    {
        count++;
        if (Math.Abs(actual - expected) > 0.0001f)
            throw new Exception(label + ": expected " + expected + ", got " + actual);
    }

    private static void Check(bool value, string label)
    {
        count++;
        if (!value) throw new Exception(label);
    }

    private static void Reject<T>(Action action, string label) where T : Exception
    {
        count++;
        try { action(); }
        catch (T) { return; }
        throw new Exception(label + ": expected " + typeof(T).Name);
    }

    public static int Main()
    {
        var origin = new Vector3(0f, 0f, 99f);
        var offset = new Vector3(3f, 4f, -200f);
        Near(GroundSpaceMath.GroundDistance(origin, offset, 1f), 5f, "DEPTH=1 Euclidean XY");
        Near(GroundSpaceMath.GroundDistance(offset, origin, 1f), 5f, "symmetric");
        Near(GroundSpaceMath.GroundDistance(origin, new Vector3(0f, 0.35f, 10f), 0.35f), 1f,
            "DEPTH=.35 vertical unit");
        Near(GroundSpaceMath.GroundDistance(new Vector3(1f, 2f, 0f),
            new Vector3(4f, 2.7f, 10f), 0.35f), (float)Math.Sqrt(13), "DEPTH=.35 diagonal");
        Near(GroundSpaceMath.GroundDistance(origin, new Vector3(-3f, -4f, 0f), 1f), 5f,
            "negative displacement");
        Near(GroundSpaceMath.GroundDistance(origin, new Vector3(0f, 0f, float.NaN), 1f), 0f,
            "coincident XY ignores Z");
        Reject<ArgumentOutOfRangeException>(() => GroundSpaceMath.GroundDistance(origin, offset, 0f),
            "zero DEPTH");
        Reject<ArgumentOutOfRangeException>(() => GroundSpaceMath.GroundDistance(origin, offset, -0.1f),
            "negative DEPTH");
        Reject<ArgumentOutOfRangeException>(() => GroundSpaceMath.GroundDistance(origin, offset, float.NaN),
            "NaN DEPTH");
        Reject<ArgumentOutOfRangeException>(() => GroundSpaceMath.GroundDistance(origin, offset, float.PositiveInfinity),
            "infinite DEPTH");
        Reject<ArgumentOutOfRangeException>(() => GroundSpaceMath.GroundDistance(
            new Vector3(float.NaN, 0f, 0f), offset, 1f), "NaN X");
        Reject<ArgumentOutOfRangeException>(() => GroundSpaceMath.GroundDistance(origin,
            new Vector3(0f, float.NegativeInfinity, 0f), 1f), "infinite Y");
        Reject<ArgumentOutOfRangeException>(() => GroundSpaceMath.GroundDistance(
            new Vector3(float.MaxValue, 0f, 0f), new Vector3(-float.MaxValue, 0f, 0f), 1f),
            "float output overflow");

        Reject<InvalidOperationException>(() => GroundSpace.FollowPoint(origin), "unbound FOCUS");
        Reject<InvalidOperationException>(() => GroundSpace.GroundDistance(origin, offset), "unbound DEPTH");
        var depth = new DepthScaleConfig();
        StreetConfigs.Bind(depth, new CameraModeConfig(), new CameraFollowConfig(),
            new SceneBoundsConfig(), new Transform(), new Collider2D());
        Check(ReferenceEquals(StreetConfigs.Current.DepthScale, depth), "bound source identity");
        Near(GroundSpace.GroundDistance(origin, new Vector3(0f, 0.35f, 0f)), 1f,
            "adapter reads preset DEPTH");
        var foot = new Vector3(7f, -2f, 13f);
        var follow = GroundSpace.FollowPoint(foot);
        Near(follow.x, 7f, "follow X preserved");
        Near(follow.y, 0f, "FOCUS=2 raises Y");
        Near(follow.z, 13f, "follow Z preserved");
        depth.Focus = 0f;
        follow = GroundSpace.FollowPoint(foot);
        Near(follow.y, foot.y, "FOCUS=0 returns foot Y");
        depth.Depth = 1f;
        Near(GroundSpace.GroundDistance(origin, offset), 5f, "runtime DEPTH update is immediate");
        var replacement = new DepthScaleConfig();
        replacement.Depth = 2f;
        replacement.Focus = 3f;
        StreetConfigs.Bind(replacement, new CameraModeConfig(), new CameraFollowConfig(),
            new SceneBoundsConfig(), new Transform(), new Collider2D());
        Near(GroundSpace.GroundDistance(origin, offset), (float)Math.Sqrt(13),
            "replacement DEPTH read on call");
        Near(GroundSpace.FollowPoint(foot).y, 1f, "replacement FOCUS read on call");
        replacement.Depth = 0f;
        Reject<ArgumentOutOfRangeException>(() => GroundSpace.GroundDistance(origin, offset),
            "invalid bound DEPTH rejected");
        replacement.Focus = float.NaN;
        Reject<ArgumentOutOfRangeException>(() => GroundSpace.FollowPoint(foot), "invalid bound FOCUS rejected");
        replacement.Focus = 2f;
        Reject<ArgumentOutOfRangeException>(() => GroundSpace.FollowPoint(
            new Vector3(1f, 2f, float.PositiveInfinity)), "invalid foot Z rejected for follow point");
        replacement.Focus = float.MaxValue;
        Reject<ArgumentOutOfRangeException>(() => GroundSpace.FollowPoint(
            new Vector3(1f, float.MaxValue, 0f)), "follow Y overflow rejected");
        StreetConfigs.Unbind();
        Check(StreetConfigs.Current == null, "unbind clears source");

        Console.WriteLine("PASS " + count + " actual-source ground assertions");
        return 0;
    }
}
