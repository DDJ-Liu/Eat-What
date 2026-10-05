using System;

namespace UnityEngine
{
    public class Object { public string name; }
    public class ScriptableObject : Object { public static ScriptableObject CreateInstance(Type type) { return (ScriptableObject)Activator.CreateInstance(type); } public static T CreateInstance<T>() where T : ScriptableObject, new() { return new T(); } }
    public class Sprite : Object { public Texture2D texture; public static Sprite Create(Texture2D texture, Rect rect, Vector2 pivot, float pixelsPerUnit) { return new Sprite { texture = texture }; } }
    public class Texture2D : Object { public int width = 1; public int height = 1; public Texture2D(int width, int height, TextureFormat format, bool mipChain) { this.width = width; this.height = height; } public void SetPixel(int x, int y, Color color) { } public void Apply() { } }
    public enum TextureFormat { RGBA32 }
    public struct Color { public static Color magenta { get { return new Color(); } } }
    public struct Rect { public Rect(float x, float y, float width, float height) { } }
    public struct Vector2 { public Vector2(float x, float y) { } }
    public static class Application { public static string ProofDataPath = "Assets"; public static string dataPath { get { return ProofDataPath; } } public static bool isBatchMode { get { return false; } }
    }
    public static class Debug { public static void Log(string message) { } public static void LogError(string message) { } public static void LogWarning(string message) { } }
    public static class JsonUtility { public static T FromJson<T>(string text) { return default(T); } public static string ToJson(object value) { return "{}"; } public static string ToJson(object value, bool prettyPrint) { return "{}"; } }
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
        // Opt-in editor-outside fixture state; production code is compiled unchanged.
        public static readonly System.Collections.Generic.Dictionary<string, Object> ProofMain = new System.Collections.Generic.Dictionary<string, Object>();
        public static readonly System.Collections.Generic.Dictionary<string, Object[]> ProofAll = new System.Collections.Generic.Dictionary<string, Object[]>();
        public static readonly System.Collections.Generic.Dictionary<string, string> ProofGuids = new System.Collections.Generic.Dictionary<string, string>();
        public static readonly System.Collections.Generic.Dictionary<Object, long> ProofFileIds = new System.Collections.Generic.Dictionary<Object, long>();
        public static readonly System.Collections.Generic.Dictionary<Object, string> ProofObjectPaths = new System.Collections.Generic.Dictionary<Object, string>();
        public static readonly System.Collections.Generic.Dictionary<string, Type> ProofTypes = new System.Collections.Generic.Dictionary<string, Type>();
        public static readonly System.Collections.Generic.HashSet<Object> ProofBroken = new System.Collections.Generic.HashSet<Object>();
        public static string[] ProofDependencies;
        public static void ResetProofFixture() {
            ProofMain.Clear(); ProofAll.Clear(); ProofGuids.Clear(); ProofFileIds.Clear();
            ProofObjectPaths.Clear(); ProofTypes.Clear(); ProofBroken.Clear(); ProofDependencies = null;
        }
        public static void StartAssetEditing() { } public static void StopAssetEditing() { } public static void SaveAssets() { }
        public static bool IsValidFolder(string path) { return true; } public static string CreateFolder(string parent, string name) { return "stub"; }
        public static T LoadAssetAtPath<T>(string path) where T : Object { return null; } public static Object LoadAssetAtPath(string path, System.Type type) { return null; } public static Object LoadMainAssetAtPath(string path) { Object value; return ProofMain.TryGetValue(path, out value) ? value : null; } public static Object[] LoadAllAssetsAtPath(string path) { Object[] value; return ProofAll.TryGetValue(path, out value) ? value : new Object[0]; }
        public static void CreateAsset(Object asset, string path) { } public static void AddObjectToAsset(Object asset, Object target) { } public static bool DeleteAsset(string path) { return true; } public static void ImportAsset(string path) { } public static void ImportAsset(string path, ImportAssetOptions options) { }
        public static string[] FindAssets(string filter, string[] folders) { return new string[0]; } public static string GUIDToAssetPath(string guid) { foreach (var pair in ProofGuids) if (pair.Value == guid) return pair.Key; return string.Empty; }
        public static string[] GetDependencies(string[] paths, bool recursive) { return ProofDependencies ?? paths; }
        public static Type GetMainAssetTypeAtPath(string path) { Type type; if (ProofTypes.TryGetValue(path, out type)) return type; Object main; return ProofMain.TryGetValue(path, out main) ? main.GetType() : null; }
        public static string AssetPathToGUID(string path) { string guid; return ProofGuids.TryGetValue(path, out guid) ? guid : string.Empty; }
        public static bool TryGetGUIDAndLocalFileIdentifier(Object asset, out string guid, out long localFileId) {
            string path;
            if (!ProofObjectPaths.TryGetValue(asset, out path) || !ProofGuids.TryGetValue(path, out guid) ||
                !ProofFileIds.TryGetValue(asset, out localFileId)) { guid = string.Empty; localFileId = 0; return false; }
            return true;
        }
    }
    public enum SerializedPropertyType { ObjectReference }
    public sealed class SerializedProperty
    {
        public SerializedPropertyType propertyType;
        public Object objectReferenceValue;
        public int objectReferenceInstanceIDValue;
        public string propertyPath;
        public bool proofBroken;
        private bool proofVisited;
        public bool NextVisible(bool enterChildren) { if (!proofBroken || proofVisited) return false; proofVisited = true; propertyType = SerializedPropertyType.ObjectReference; objectReferenceInstanceIDValue = 42; propertyPath = "broken"; return true; }
        public bool Next(bool enterChildren) { return NextVisible(enterChildren); }
    }
    public sealed class SerializedObject
    {
        private readonly Object target;
        public SerializedObject(Object target) { this.target = target; }
        public SerializedProperty GetIterator() { return new SerializedProperty { proofBroken = AssetDatabase.ProofBroken.Contains(target) }; }
    }
    public static class FileUtil { public static void ReplaceFile(string source, string destination) { } }
    public static class EditorUtility { public static void SetDirty(Object target) { } }
    public static class EditorApplication { public static void Exit(int exitCode) { } }
}

namespace Spine.Unity
{
    public class AnimationReferenceAsset : UnityEngine.ScriptableObject { }
    public sealed class DerivedAnimationReferenceAsset : AnimationReferenceAsset { }
    public sealed class SkeletonDataAsset : UnityEngine.ScriptableObject { }
}
