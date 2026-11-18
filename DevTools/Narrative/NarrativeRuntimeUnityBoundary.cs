// Adds only component/Inspector metadata to the existing StaticCompile Unity boundary.
namespace UnityEngine
{
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public sealed class DisallowMultipleComponent : System.Attribute { }
    [System.AttributeUsage(System.AttributeTargets.Field)]
    public sealed class TooltipAttribute : System.Attribute { public TooltipAttribute(string text) { } }
    public class MonoBehaviour : Object { public bool isActiveAndEnabled = true; }
}

// Scope the missing context overload to the new runtime fixture's Unity layer only.
// The existing shared Debug stub and every production source stay unchanged.
namespace EatWhat.Narrative.Unity
{
    internal static class Debug
    {
        public static void LogWarning(string text) { UnityEngine.Debug.LogWarning(text); }
        public static void LogWarning(string text, object context) { UnityEngine.Debug.LogWarning(text); }
    }
}
