using System;
using UnityEngine;
using UnityEngine.InputSystem;

public static class MousePositionFallbackTests
{
    public static int Main()
    {
        Console.WriteLine(Run());
        return 0;
    }

    public static string Run()
    {
        Debug.WarningMessages.Clear();
        Time.frameCount = 10;
        var camera = new Camera { nearClipPlane = 0.75f };
        var mouse = new Mouse();
        mouse.position.Value = new Vector2(123f, 456f);
        Camera.main = camera;
        Mouse.current = mouse;

        Vector3 current = Vector3.zero;
        Assert(Tools.HasPointer && Tools.TryGetMousePos(out current) &&
               current.x == 123f && current.y == 456f && current.z == 0f,
            "Mouse + main-camera path must preserve conversion and the 2D z contract.");
        Assert(camera.ScreenToWorldPointCallCount == 1,
            "The normal path must convert the pointer exactly once per requested sample.");
        Assert(Vector3.Distance(Tools.getMousePos(), current) < 0.0001f,
            "The compatibility entry point must return the same valid position.");

        Mouse.current = null;
        Time.frameCount++;
        Vector3 unavailable;
        Assert(!Tools.HasPointer && !Tools.TryGetMousePos(out unavailable),
            "TryGetMousePos must report unavailable when no mouse device exists.");
        Assert(Vector3.Distance(Tools.getMousePos(), current) < 0.0001f,
            "No-mouse fallback must hold the last valid point instead of injecting a world-origin drag/hover sample.");

        Assert(Debug.WarningMessages.Count == 0,
            "A missing mouse must not be misreported as a missing main camera.");

        Camera.main = null;
        Mouse.current = mouse;
        Time.frameCount++;
        Assert(!Tools.HasPointer && !Tools.TryGetMousePos(out unavailable),
            "TryGetMousePos must report unavailable when no main camera exists.");
        Assert(Vector3.Distance(Tools.getMousePos(), current) < 0.0001f,
            "No-camera fallback must hold the last valid point instead of injecting a world-origin drag/hover sample.");
        Assert(Debug.WarningMessages.Count == 1,
            "A missing main camera must emit one clear warning for the missing episode.");
        Time.frameCount++;
        Assert(!Tools.HasPointer && !Tools.TryGetMousePos(out unavailable) &&
               Debug.WarningMessages.Count == 1,
            "Repeated missing-camera frames must not flood the Console.");

        Camera.main = camera;
        Mouse.current = mouse;
        mouse.position.Value = new Vector2(789f, 321f);
        Time.frameCount++;
        Assert(Tools.HasPointer && Tools.TryGetMousePos(out current) &&
               current.x == 789f && current.y == 321f && current.z == 0f,
            "Pointer sampling must recover after the camera and device become available again.");
        Assert(camera.ScreenToWorldPointCallCount == 3 && Debug.WarningMessages.Count == 1,
            "Recovery must reuse the restored camera without adding a warning.");

        Camera.main = null;
        Time.frameCount++;
        Assert(!Tools.TryGetMousePos(out unavailable) && Debug.WarningMessages.Count == 2,
            "A new missing-camera episode after recovery may emit exactly one new warning.");

        Camera.main = camera;
        Time.frameCount++;
        return "PASS: pointer availability, camera cache invalidation, missing-device/camera fallback, warning throttle, and recovery.";
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
