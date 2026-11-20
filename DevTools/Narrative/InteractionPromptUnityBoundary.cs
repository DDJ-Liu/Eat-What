// External value/asset/log handles only. State, device enum and localization are production sources.
using System;
using System.Collections.Generic;
namespace UnityEngine
{
    public struct Vector2 { public float x,y; public Vector2(float x,float y){this.x=x;this.y=y;} public static Vector2 zero { get { return new Vector2(); } } }
    public class ScriptableObject { }
    public sealed class CreateAssetMenuAttribute : Attribute { public string fileName,menuName; }
    public static class Debug { public static readonly List<string> Warnings = new List<string>(); public static void LogWarning(string value){Warnings.Add(value);} }
}
