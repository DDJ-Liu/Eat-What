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

    public static class GameInputMath
    {
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
