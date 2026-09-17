using System;

namespace UnityEngine
{
    public class Object { public string name; }
    public class ScriptableObject : Object { public static ScriptableObject CreateInstance(Type type) { return (ScriptableObject)Activator.CreateInstance(type); } public static T CreateInstance<T>() where T : ScriptableObject, new() { return new T(); } }
    public class Sprite : Object { public static Sprite Create(Texture2D texture, Rect rect, Vector2 pivot, float pixelsPerUnit) { return new Sprite(); } }
    public class Texture2D : Object { public int width = 1; public int height = 1; public Texture2D(int width, int height, TextureFormat format, bool mipChain) { this.width = width; this.height = height; } public void SetPixel(int x, int y, Color color) { } public void Apply() { } }
    public enum TextureFormat { RGBA32 }
    public struct Color { public static Color magenta { get { return new Color(); } } }
    public struct Rect { public Rect(float x, float y, float width, float height) { } }
    public struct Vector2 { public Vector2(float x, float y) { } }
    public static class Application { public static string dataPath { get { return "Assets"; } }
    }
    public static class Debug { public static void Log(string message) { } public static void LogError(string message) { } public static void LogWarning(string message) { } }
    public static class JsonUtility { public static T FromJson<T>(string text) { return default(T); } }
    public sealed class CreateAssetMenuAttribute : Attribute { public string fileName; public string menuName; }
    public sealed class SerializeField : Attribute { }
}

namespace UnityEditor
{
    using UnityEngine;
    public sealed class MenuItem : Attribute { public MenuItem(string path) { } }
    public enum ImportAssetOptions { ForceUpdate }
    public static class AssetDatabase
    {
        public static void StartAssetEditing() { } public static void StopAssetEditing() { } public static void SaveAssets() { }
        public static bool IsValidFolder(string path) { return true; } public static string CreateFolder(string parent, string name) { return "stub"; }
        public static T LoadAssetAtPath<T>(string path) where T : Object { return null; } public static Object LoadAssetAtPath(string path, System.Type type) { return null; } public static Object LoadMainAssetAtPath(string path) { return null; } public static Object[] LoadAllAssetsAtPath(string path) { return new Object[0]; }
        public static void CreateAsset(Object asset, string path) { } public static void AddObjectToAsset(Object asset, Object target) { } public static bool DeleteAsset(string path) { return true; } public static void ImportAsset(string path) { } public static void ImportAsset(string path, ImportAssetOptions options) { }
        public static string[] FindAssets(string filter, string[] folders) { return new string[0]; } public static string GUIDToAssetPath(string guid) { return string.Empty; }
    }
    public static class FileUtil { public static void ReplaceFile(string source, string destination) { } }
    public static class EditorUtility { public static void SetDirty(Object target) { } }
}
