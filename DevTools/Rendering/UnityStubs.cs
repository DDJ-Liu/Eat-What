using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.Class)] public sealed class ExecuteAlwaysAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Class)] public sealed class DisallowMultipleComponentAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Class)] public sealed class CreateAssetMenuAttribute : Attribute
    {
        public string fileName;
        public string menuName;
    }
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)] public sealed class RequireComponentAttribute : Attribute
    {
        public RequireComponentAttribute(Type type) { }
    }
    [AttributeUsage(AttributeTargets.Field)] public sealed class SerializeFieldAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Field)] public sealed class HideInInspectorAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Field)] public sealed class ColorUsageAttribute : Attribute
    {
        public ColorUsageAttribute(bool showAlpha, bool hdr) { }
    }
    [AttributeUsage(AttributeTargets.Field)] public sealed class RangeAttribute : Attribute
    {
        public RangeAttribute(float minimum, float maximum) { }
    }
    [AttributeUsage(AttributeTargets.Field)] public sealed class TooltipAttribute : Attribute
    {
        public TooltipAttribute(string text) { }
    }
    [AttributeUsage(AttributeTargets.Method)] public sealed class ContextMenuAttribute : Attribute
    {
        public ContextMenuAttribute(string itemName) { }
    }

    public class Object { public string name; }

    public class ScriptableObject : Object
    {
        public static T CreateInstance<T>() where T : ScriptableObject, new() { return new T(); }
    }

    public class Component : Object
    {
        public GameObject gameObject { get; internal set; }
        public Transform transform { get { return gameObject == null ? null : gameObject.transform; } }

        public T GetComponent<T>() where T : Component
        {
            return gameObject == null ? null : gameObject.GetComponent<T>();
        }

        public T[] GetComponentsInChildren<T>(bool includeInactive) where T : Component
        {
            return gameObject == null ? new T[0] : gameObject.GetComponentsInChildren<T>(includeInactive);
        }
    }

    public class Behaviour : Component
    {
        public bool enabled = true;
        public bool isActiveAndEnabled { get { return enabled && gameObject != null && gameObject.activeSelf; } }
    }

    public class MonoBehaviour : Behaviour { }

    public sealed class GameObject : Object
    {
        private readonly List<Component> components = new List<Component>();
        private readonly List<GameObject> children = new List<GameObject>();

        public bool activeSelf = true;
        public Transform transform { get; private set; }

        public GameObject()
        {
            transform = new Transform();
            transform.gameObject = this;
            components.Add(transform);
        }

        public T AddComponent<T>() where T : Component, new()
        {
            T component = new T();
            component.gameObject = this;
            components.Add(component);
            return component;
        }

        public T GetComponent<T>() where T : Component
        {
            return components.OfType<T>().FirstOrDefault();
        }

        public T[] GetComponents<T>() where T : Component
        {
            return components.OfType<T>().ToArray();
        }

        public void AddChild(GameObject child)
        {
            children.Add(child);
            child.transform.parent = transform;
        }

        public T[] GetComponentsInChildren<T>(bool includeInactive) where T : Component
        {
            List<T> result = new List<T>();
            if (includeInactive || activeSelf)
                result.AddRange(components.OfType<T>());

            foreach (GameObject child in children)
            {
                if (includeInactive || child.activeSelf)
                    result.AddRange(child.GetComponentsInChildren<T>(includeInactive));
            }

            return result.ToArray();
        }
    }

    public sealed class Transform : Component
    {
        public Transform parent;
        public void SetParent(Transform value, bool worldPositionStays) { parent = value; }
    }

    public class Material : Object { }
    public class Sprite : Object { }

    public static class Shader
    {
        private static readonly Dictionary<string, int> Ids = new Dictionary<string, int>();

        public static int PropertyToID(string name)
        {
            int id;
            if (!Ids.TryGetValue(name, out id))
            {
                id = Ids.Count + 1;
                Ids.Add(name, id);
            }
            return id;
        }
    }

    public struct Color
    {
        public float r;
        public float g;
        public float b;
        public float a;

        public Color(float r, float g, float b, float a)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        public static Color black { get { return new Color(0f, 0f, 0f, 1f); } }
    }

    public struct Color32
    {
        public byte r;
        public byte g;
        public byte b;
        public byte a;

        public Color32(byte r, byte g, byte b, byte a)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        public static implicit operator Color(Color32 value)
        {
            const float scale = 1f / 255f;
            return new Color(value.r * scale, value.g * scale, value.b * scale, value.a * scale);
        }
    }

    public struct Vector2
    {
        public float x;
        public float y;

        public Vector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public static Vector2 zero { get { return new Vector2(0f, 0f); } }
    }

    public struct Vector4
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public Vector4(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }
    }

    public static class Mathf
    {
        public static float Clamp(float value, float minimum, float maximum)
        {
            return value < minimum ? minimum : (value > maximum ? maximum : value);
        }

        public static bool Approximately(float left, float right)
        {
            return Math.Abs(left - right) <= 0.00001f;
        }
    }

    public static class Resources
    {
        private static readonly Dictionary<string, Object> Values = new Dictionary<string, Object>();
        public static T Load<T>(string path) where T : Object
        {
            Object value;
            return Values.TryGetValue(path, out value) ? value as T : null;
        }
        public static void Register(string path, Object value) { Values[path] = value; }
        public static void Clear() { Values.Clear(); }
    }

    public sealed class MaterialPropertyBlock
    {
        private readonly Dictionary<int, object> values = new Dictionary<int, object>();

        public void SetFloat(int id, float value) { values[id] = value; }
        public void SetColor(int id, Color value) { values[id] = value; }
        public void SetVector(int id, Vector4 value) { values[id] = value; }
        public float GetFloat(int id) { return values.ContainsKey(id) ? (float)values[id] : 0f; }
        public Color GetColor(int id) { return values.ContainsKey(id) ? (Color)values[id] : new Color(); }
        public Vector4 GetVector(int id) { return values.ContainsKey(id) ? (Vector4)values[id] : new Vector4(); }

        internal void CopyFrom(MaterialPropertyBlock source)
        {
            values.Clear();
            if (source == null)
                return;
            foreach (KeyValuePair<int, object> pair in source.values)
                values[pair.Key] = pair.Value;
        }
    }

    public class Renderer : Component
    {
        public Material sharedMaterial;
    }

    public sealed class SpriteRenderer : Renderer
    {
        private readonly MaterialPropertyBlock storedBlock = new MaterialPropertyBlock();

        public Sprite sprite;
        public int GetPropertyBlockCount { get; private set; }
        public int SetPropertyBlockCount { get; private set; }

        public void GetPropertyBlock(MaterialPropertyBlock destination)
        {
            GetPropertyBlockCount++;
            destination.CopyFrom(storedBlock);
        }

        public void SetPropertyBlock(MaterialPropertyBlock source)
        {
            SetPropertyBlockCount++;
            storedBlock.CopyFrom(source);
        }

        public void ResetPropertyBlockCounters()
        {
            GetPropertyBlockCount = 0;
            SetPropertyBlockCount = 0;
        }
    }
}

namespace EatWhat.Tools.Rendering
{
    public enum OutlineMergeOverride
    {
        FollowGroup = 0,
        ForceOn = 1,
        ForceOff = 2
    }

    public sealed class SpriteOutlineMergeMember2D : UnityEngine.MonoBehaviour
    {
        public bool FollowGroupOutlineParameters { get; private set; }
        public OutlineMergeOverride OutlineMode { get; private set; }
        public OutlineMergeOverride ShadowMode { get; private set; }
        public void SetFormalSourceActive(bool active) { }
        public bool ConfigureFormal(UnityEngine.SpriteRenderer renderer, OutlineMergeOverride outlineMode,
            OutlineMergeOverride shadowMode, bool followGroupOutlineParameters,
            UnityEngine.Color outlineColor, float outlineThickness, UnityEngine.Color shadowColor,
            UnityEngine.Vector2 shadowOffset, float shadowOpacity, float shadowSoftness)
        {
            FollowGroupOutlineParameters = followGroupOutlineParameters;
            OutlineMode = outlineMode;
            ShadowMode = shadowMode;
            return true;
        }
    }

    public sealed class SpriteOutlineMergeRenderer2D : UnityEngine.MonoBehaviour
    {
        public bool Ready = true;
        public bool IsReadyFor(SpriteOutlineMergeMember2D member) { return Ready && member != null; }
        public void SetFormalGroupActive(bool active) { }
        public bool ConfigureFormalGroup(bool outlineMergeEnabled, UnityEngine.Color outlineColor,
            float outlineThickness, bool shadowMergeEnabled, bool shadowEffectEnabled,
            UnityEngine.Color shadowColor, UnityEngine.Vector2 shadowOffset, float shadowOpacity,
            float shadowSoftness) { return false; }
        public bool ReplaceMembersIfChanged(IList<SpriteOutlineMergeMember2D> members) { return false; }
    }
}
