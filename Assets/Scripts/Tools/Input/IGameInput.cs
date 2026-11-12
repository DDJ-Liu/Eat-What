using System;
using UnityEngine;

namespace EatWhat.Toolkit.Input
{
    /// <summary>Minimal game input contract. Interact is a one-frame press.</summary>
    public interface IGameInput
    {
        float Horizontal { get; }
        float Vertical { get; }
        Vector2 LookAxis { get; }
        GameInputDevice CurrentDevice { get; }
        bool Interact { get; }
        Vector2 MouseScreenPos { get; }
    }

    public enum GameInputDevice { Keyboard, Gamepad }

    /// <summary>Optional one-frame navigation input. Existing IGameInput implementations need not change.</summary>
    public interface INavigationInput
    {
        bool BackPressed { get; }
    }

    public struct LookInputState
    {
        public bool HasMouse;
        public Vector2 Mouse;
        public float Width, Height;
        public GameInputDevice Device;
    }
    public struct LookInputSample
    {
        public LookInputState State;
        public Vector2 Axis;
        public GameInputDevice Device;
    }

    public static class GameInputMath
    {
        // This value is already processed by Input System; do not add another deadzone.
        public static Vector2 ClampStick(Vector2 value)
        {
            if (!Finite(value.x) || !Finite(value.y)) return Vector2.zero;
            double length = Math.Sqrt((double)value.x*value.x + (double)value.y*value.y);
            double scale = length > 1 ? 1/length : 1;
            return new Vector2((float)(value.x*scale), (float)(value.y*scale));
        }
        public static LookInputSample SelectLook(LookInputState previous, Vector2 mouse,
            float width, float height, Vector2 processedStick, bool gamepadAvailable, bool ready)
        {
            var next = previous;
            if (!ready)
            {
                next.HasMouse = false;
                return new LookInputSample { State=next, Device=next.Device };
            }
            bool known = Finite(mouse.x) && Finite(mouse.y) && Finite(width) && Finite(height) && width>0 && height>0;
            bool moved = known && previous.HasMouse && width==previous.Width && height==previous.Height &&
                (mouse.x!=previous.Mouse.x || mouse.y!=previous.Mouse.y);
            next.HasMouse=known; next.Mouse=mouse; next.Width=width; next.Height=height;
            var stick=gamepadAvailable?ClampStick(processedStick):Vector2.zero;
            if (stick.x!=0 || stick.y!=0) next.Device=GameInputDevice.Gamepad;
            else if (moved) next.Device=GameInputDevice.Keyboard;
            var axis=next.Device==GameInputDevice.Gamepad?stick:LookAxisFromScreenPosition(mouse,width,height);
            return new LookInputSample { State=next, Device=next.Device, Axis=axis };
        }

        public static float HorizontalFromKeys(bool a, bool d, bool leftArrow, bool rightArrow)
        {
            bool left = a || leftArrow;
            bool right = d || rightArrow;
            return (right ? 1f : 0f) - (left ? 1f : 0f);
        }

        public static float VerticalFromKeys(bool w, bool s, bool upArrow, bool downArrow)
        {
            bool up = w || upArrow;
            bool down = s || downArrow;
            return (up ? 1f : 0f) - (down ? 1f : 0f);
        }

        public static Vector2 LookAxisFromScreenPosition(Vector2 mouse, float width, float height)
        {
            if (!Finite(mouse.x) || !Finite(mouse.y) || !Finite(width) || !Finite(height) || width <= 0f || height <= 0f)
                return Vector2.zero;
            double x = (double)mouse.x / width * 2d - 1d;
            double y = (double)mouse.y / height * 2d - 1d;
            return new Vector2((float)Math.Max(-1d, Math.Min(1d, x)),
                (float)Math.Max(-1d, Math.Min(1d, y)));
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
