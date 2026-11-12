using System;
using System.Reflection;
using EatWhat.LongLoop;
using EatWhat.Toolkit.Input;
using UnityEngine;

internal static class LongLoopLogicTests
{
    private static int count;
    private static void Eq(float actual, float expected, string name)
    {
        count++;
        if (Math.Abs(actual - expected) > 0.0001f)
            throw new Exception(name + ": expected " + expected + ", got " + actual);
    }
    private static void Yes(bool value, string name)
    {
        count++;
        if (!value) throw new Exception(name);
    }
    public static int Main()
    {
        Eq(GameInputMath.HorizontalFromKeys(true, false, false, false), -1f, "A");
        Eq(GameInputMath.HorizontalFromKeys(false, false, true, false), -1f, "LeftArrow");
        Eq(GameInputMath.HorizontalFromKeys(false, true, false, false), 1f, "D");
        Eq(GameInputMath.HorizontalFromKeys(false, false, false, true), 1f, "RightArrow");
        Eq(GameInputMath.HorizontalFromKeys(false, false, false, false), 0f, "release");
        Eq(GameInputMath.HorizontalFromKeys(true, true, false, false), 0f, "A+D neutral");
        Eq(GameInputMath.HorizontalFromKeys(true, false, false, true), 0f, "mixed neutral");
        Eq(GameInputMath.VerticalFromKeys(true, false, false, false), 1f, "W up");
        Eq(GameInputMath.VerticalFromKeys(false, false, true, false), 1f, "UpArrow up");
        Eq(GameInputMath.VerticalFromKeys(false, true, false, false), -1f, "S down");
        Eq(GameInputMath.VerticalFromKeys(false, false, false, true), -1f, "DownArrow down");
        Eq(GameInputMath.VerticalFromKeys(false, false, false, false), 0f, "vertical released");
        Eq(GameInputMath.VerticalFromKeys(true, true, false, false), 0f, "W and S neutral");
        Eq(GameInputMath.VerticalFromKeys(true, false, false, true), 0f, "W and DownArrow neutral");
        Eq(GameInputMath.VerticalFromKeys(false, true, true, false), 0f, "S and UpArrow neutral");
        var look = GameInputMath.LookAxisFromScreenPosition(new Vector2(400f, 300f), 800f, 600f);
        Eq(look.x, 0f, "look center x"); Eq(look.y, 0f, "look center y");
        look = GameInputMath.LookAxisFromScreenPosition(new Vector2(0f, 0f), 800f, 600f);
        Eq(look.x, -1f, "look lower left x"); Eq(look.y, -1f, "look lower left y");
        look = GameInputMath.LookAxisFromScreenPosition(new Vector2(800f, 600f), 800f, 600f);
        Eq(look.x, 1f, "look upper right x"); Eq(look.y, 1f, "look upper right y");
        look = GameInputMath.LookAxisFromScreenPosition(new Vector2(800f, 0f), 800f, 600f);
        Eq(look.x, 1f, "look lower right x"); Eq(look.y, -1f, "look lower right y");
        look = GameInputMath.LookAxisFromScreenPosition(new Vector2(0f, 600f), 800f, 600f);
        Eq(look.x, -1f, "look upper left x"); Eq(look.y, 1f, "look upper left y");
        look = GameInputMath.LookAxisFromScreenPosition(new Vector2(900f, -10f), 800f, 600f);
        Eq(look.x, 1f, "look x clamped"); Eq(look.y, -1f, "look y clamped");
        look = GameInputMath.LookAxisFromScreenPosition(new Vector2(400f, 300f), 0f, 600f);
        Eq(look.x, 0f, "zero width x"); Eq(look.y, 0f, "zero width y");
        look = GameInputMath.LookAxisFromScreenPosition(new Vector2(400f, 300f), 800f, 0f);
        Eq(look.x, 0f, "zero height x"); Eq(look.y, 0f, "zero height y");
        look = GameInputMath.LookAxisFromScreenPosition(new Vector2(float.NaN, 300f), 800f, 600f);
        Eq(look.x, 0f, "nonfinite mouse x"); Eq(look.y, 0f, "nonfinite mouse y");
        look = GameInputMath.LookAxisFromScreenPosition(new Vector2(400f, 300f), float.PositiveInfinity, 600f);
        Eq(look.x, 0f, "nonfinite dimensions x"); Eq(look.y, 0f, "nonfinite dimensions y");
        var provider = new LegacyInputProvider();
        Screen.width = 800; Screen.height = 600;
        UnityEngine.Input.mousePosition = new Vector3(800f, 0f, 0f);
        UnityEngine.Input.Held.Add(KeyCode.W);
        Eq(provider.Vertical, 1f, "provider W");
        UnityEngine.Input.Held.Clear(); UnityEngine.Input.Held.Add(KeyCode.UpArrow);
        Eq(provider.Vertical, 1f, "provider UpArrow");
        UnityEngine.Input.Held.Clear(); UnityEngine.Input.Held.Add(KeyCode.S);
        Eq(provider.Vertical, -1f, "provider S");
        UnityEngine.Input.Held.Clear(); UnityEngine.Input.Held.Add(KeyCode.DownArrow);
        Eq(provider.Vertical, -1f, "provider DownArrow");
        UnityEngine.Input.Held.Add(KeyCode.W);
        Eq(provider.Vertical, 0f, "provider opposite directions");
        UnityEngine.Input.Held.Clear(); UnityEngine.Input.Held.Add(KeyCode.A);
        Eq(provider.Horizontal, -1f, "provider existing left");
        UnityEngine.Input.Held.Clear(); UnityEngine.Input.Held.Add(KeyCode.D);
        Eq(provider.Horizontal, 1f, "provider existing right");
        look = provider.LookAxis; Eq(look.x, 1f, "provider game width x"); Eq(look.y, -1f, "provider game height y");
        Yes(provider.CurrentDevice == GameInputDevice.Keyboard, "provider current keyboard");
        UnityEngine.Input.Down.Add(KeyCode.E);
        Yes(provider.Interact, "provider existing interact");
        typeof(LegacyInputProvider).GetMethod("OnApplicationFocus", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(provider, new object[] { false });
        Eq(provider.Vertical, 0f, "unfocused vertical neutral");
        look = provider.LookAxis; Eq(look.x, 0f, "unfocused look x neutral"); Eq(look.y, 0f, "unfocused look y neutral");
        Yes(!provider.Interact, "unfocused interact neutral");
        typeof(LegacyInputProvider).GetMethod("OnApplicationFocus", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(provider, new object[] { true });
        provider.isActiveAndEnabled = false;
        Eq(provider.Vertical, 0f, "disabled vertical neutral");
        look = provider.LookAxis; Eq(look.x, 0f, "disabled look x neutral"); Eq(look.y, 0f, "disabled look y neutral");
        provider.isActiveAndEnabled = true;
        typeof(LegacyInputProvider).GetMethod("OnApplicationPause", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(provider, new object[] { true });
        Eq(provider.Vertical, 0f, "paused vertical neutral");
        look = provider.LookAxis; Eq(look.x, 0f, "paused look x neutral"); Eq(look.y, 0f, "paused look y neutral");
        Eq(HorizontalMotion.Step(3f, 1f, 4f, 0.5f, -10f, 10f), 5f, "speed and dt");
        Eq(HorizontalMotion.Step(3f, 0f, 4f, 1f, -10f, 10f), 3f, "released stops");
        Eq(HorizontalMotion.Step(3f, 1f, -4f, 1f, -10f, 10f), 3f, "negative speed stops");
        Eq(HorizontalMotion.Step(9f, 1f, 4f, 1f, -10f, 10f), 10f, "right boundary");
        Eq(HorizontalMotion.Step(-9f, -1f, 4f, 1f, -10f, 10f), -10f, "left boundary");
        Eq(HorizontalMotion.Step(9f, 1f, 4f, 1f, 10f, -10f), 10f, "reversed markers");
        Eq(HorizontalMotion.Step(-20f, 0f, 4f, 1f, -10f, 10f), -10f, "spawn outside clamps");
        Eq(HorizontalMotion.Step(20f, 0f, 4f, 1f, -10f, 10f), 10f, "stopped outside clamps");
        Yes(!HorizontalMotion.CanAdvance(10f, 1f, 4f, -10f, 10f), "right edge idle");
        Yes(!HorizontalMotion.CanAdvance(-10f, -1f, 4f, -10f, 10f), "left edge idle");
        Yes(HorizontalMotion.CanAdvance(0f, -1f, 4f, -10f, 10f), "inside moving");
        Yes(HorizontalMotion.CanAdvance(10f, -1f, 4f, -10f, 10f), "can return from right edge");
        Console.WriteLine("PASS " + count + " actual-source logic assertions");
        return 0;
    }
}
