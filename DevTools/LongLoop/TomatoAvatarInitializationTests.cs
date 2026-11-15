using System;
using System.Reflection;
using EatWhat.LongLoop;

// These tiny substitutes execute the real TomatoSpineAvatar C# control flow.
// They do not claim Unity component initialization or Spine playback works natively.
namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.Class)] public sealed class DisallowMultipleComponent : Attribute {}
    [AttributeUsage(AttributeTargets.Field)] public sealed class SerializeField : Attribute {}
    public class MonoBehaviour
    {
        public string name = "test-avatar";
        protected bool TryGetComponent<T>(out T value) where T : class { value = null; return false; }
    }
    public sealed class Transform
    {
        public Vector3 localScale;
        public Transform(float x, float y, float z) { localScale = new Vector3(x, y, z); }
    }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
    }
    public static class Mathf { public static float Abs(float value) => Math.Abs(value); }
    public static class Debug
    {
        public static int Warnings;
        public static void LogWarning(string message, object context) { Warnings++; }
    }
}
namespace Spine
{
    public sealed class Animation {}
    public sealed class SkeletonData
    {
        public Animation FindAnimation(string name) =>
            name == "idle" || name == "walk" ? new Animation() : null;
    }
}
namespace Spine.Unity
{
    public sealed class SkeletonDataAsset
    {
        public Spine.SkeletonData GetSkeletonData(bool required) => new Spine.SkeletonData();
    }
    public sealed class AnimationState
    {
        public int Calls;
        public string LastName;
        public void SetAnimation(int track, string name, bool loop) { Calls++; LastName = name; }
    }
    public sealed class SkeletonAnimation : UnityEngine.MonoBehaviour
    {
        public SkeletonDataAsset SkeletonDataAsset = new SkeletonDataAsset();
        public AnimationState AnimationState = new AnimationState();
    }
}

internal static class TomatoAvatarInitializationTests
{
    private static int assertions;
    private static void Equal(float actual, float expected, string message)
    {
        assertions++;
        if (Math.Abs(actual - expected) > 0.00001f)
            throw new Exception(message + ": expected " + expected + ", got " + actual);
    }
    private static void True(bool actual, string message)
    {
        assertions++;
        if (!actual) throw new Exception(message);
    }
    private static void Set(object target, string field, object value)
    {
        typeof(TomatoSpineAvatar).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(target, value);
    }
    private static void Awake(TomatoSpineAvatar avatar)
    {
        typeof(TomatoSpineAvatar).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(avatar, null);
    }
    private static void Scale(UnityEngine.Transform root, float x, float y, float z, string label)
    {
        Equal(root.localScale.x, x, label + " x");
        Equal(root.localScale.y, y, label + " y");
        Equal(root.localScale.z, z, label + " z");
    }
    public static int Main()
    {
        // CONTROL's original counterexample: controller first has written the same root to +2.
        var root = new UnityEngine.Transform(2f, 3f, 4f);
        var avatar = new TomatoSpineAvatar();
        Set(avatar, "facingVisualRoot", root);
        avatar.SetFacing(true);             // before avatar.Awake
        Scale(root, 2f, 3f, 4f, "controller-first initial");
        Awake(avatar);
        avatar.SetFacing(false);
        Scale(root, -2f, 3f, 4f, "controller-first left");
        avatar.SetFacing(false);
        Scale(root, -2f, 3f, 4f, "controller-first repeated left");
        avatar.SetFacing(true);
        Scale(root, 2f, 3f, 4f, "controller-first right");

        var negative = new UnityEngine.Transform(-2.5f, 6f, 7f);
        var awakeFirst = new TomatoSpineAvatar();
        Set(awakeFirst, "facingVisualRoot", negative);
        Awake(awakeFirst);
        awakeFirst.SetFacing(true);
        Scale(negative, 2.5f, 6f, 7f, "adapter-first right");
        awakeFirst.SetFacing(false);
        Scale(negative, -2.5f, 6f, 7f, "adapter-first left");

        var zero = new UnityEngine.Transform(0f, 8f, 9f);
        var zeroAvatar = new TomatoSpineAvatar();
        Set(zeroAvatar, "facingVisualRoot", zero);
        Awake(zeroAvatar);
        zeroAvatar.SetFacing(true);
        Scale(zero, 0f, 8f, 9f, "zero magnitude is not invented as one");

        var empty = new TomatoSpineAvatar();
        int warningsBeforeEmptyRoot = UnityEngine.Debug.Warnings;
        empty.SetFacing(false);
        Awake(empty);
        empty.SetFacing(true);
        True(UnityEngine.Debug.Warnings == warningsBeforeEmptyRoot,
             "null root leaves controller as sole flipper without a warning");

        var animated = new TomatoSpineAvatar();
        var skeleton = new Spine.Unity.SkeletonAnimation();
        Set(animated, "skeletonAnimation", skeleton);
        Awake(animated);
        animated.SetMoving(false);
        animated.SetMoving(false);
        animated.SetMoving(true);
        animated.SetMoving(true);
        True(skeleton.AnimationState.Calls == 2, "idle/walk transitions only");
        True(skeleton.AnimationState.LastName == "walk", "last transition is walk");
        Console.WriteLine("PASS " + assertions + " real-avatar control-flow assertions");
        return 0;
    }
}


// AI531 fixture-only boundary: every visualRoot above has NEVER had a DepthScale component.
// Mirrors only the two absent-component returns from the current production API.
// Does not model a DepthScale component, depth scaling, registration, or its lifecycle.
// The real production DLL compilation uses Assets/Scripts/LongLoop/DepthScale.cs instead.
namespace EatWhat.LongLoop
{
    internal static class DepthScale
    {
        public static bool TrySetFacing(UnityEngine.Transform visual, bool right) => false;
        public static float ResolveFacingMagnitude(UnityEngine.Transform visual, float legacyMagnitude,
            ref bool hasDepthBaseline) => legacyMagnitude; // Fixture flag is never set true.
    }
}
