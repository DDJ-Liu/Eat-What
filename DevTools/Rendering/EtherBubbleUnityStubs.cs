// Test-only Unity surface for EtherBubbleDistortionTests. It cannot prove GPU output.
using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.Class)] public sealed class DisallowMultipleComponentAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Class)] public sealed class DefaultExecutionOrderAttribute : Attribute
    {
        public DefaultExecutionOrderAttribute(int order) { }
    }
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)] public sealed class RequireComponentAttribute : Attribute
    {
        public RequireComponentAttribute(Type type) { }
    }
    [AttributeUsage(AttributeTargets.Class)] public sealed class AddComponentMenuAttribute : Attribute
    {
        public AddComponentMenuAttribute(string menuName) { }
    }
    [AttributeUsage(AttributeTargets.Field)] public sealed class HeaderAttribute : Attribute
    {
        public HeaderAttribute(string header) { }
    }
    [AttributeUsage(AttributeTargets.Field)] public sealed class SerializeFieldAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Field)] public sealed class TooltipAttribute : Attribute
    {
        public TooltipAttribute(string tooltip) { }
    }
    [AttributeUsage(AttributeTargets.Field)] public sealed class RangeAttribute : Attribute
    {
        public RangeAttribute(float minimum, float maximum) { }
        public RangeAttribute(int minimum, int maximum) { }
    }
    [AttributeUsage(AttributeTargets.Field)] public sealed class ColorUsageAttribute : Attribute
    {
        public ColorUsageAttribute(bool showAlpha, bool hdr) { }
    }
    [AttributeUsage(AttributeTargets.Method)] public sealed class ContextMenuAttribute : Attribute
    {
        public ContextMenuAttribute(string itemName) { }
    }

    public class Object
    {
        private static int nextId = 1;
        private readonly int instanceId = nextId++;
        public string name;

        public int GetInstanceID() { return instanceId; }
        public static void Destroy(Object target) { }
        public static void DestroyImmediate(Object target) { }

        public static T Instantiate<T>(T original, Transform parent) where T : Object
        {
            InstantiateCount++;
            var component = original as Component;
            if (component == null) throw new InvalidOperationException("Stub only clones Components.");
            var cloneObject = new GameObject(component.gameObject.name + "(Clone)");
            cloneObject.transform.SetParent(parent, false);
            return (T)(Object)cloneObject.AddComponent(component.GetType());
        }

        public static int InstantiateCount { get; private set; }
        public static void ResetInstantiateCount() { InstantiateCount = 0; }
    }

    public class Component : Object
    {
        public GameObject gameObject { get; internal set; }
        public Transform transform { get { return gameObject == null ? null : gameObject.transform; } }

        public T GetComponent<T>() where T : Component
        {
            return gameObject == null ? null : gameObject.GetComponent<T>();
        }
    }

    public class Behaviour : Component
    {
        public bool enabled = true;
        public bool isActiveAndEnabled
        {
            get { return enabled && gameObject != null && gameObject.activeInHierarchy; }
        }
    }

    public class MonoBehaviour : Behaviour { }

    public sealed class GameObject : Object
    {
        private readonly List<Component> components = new List<Component>();
        public readonly Transform transform;
        public bool activeSelf = true;
        public int layer;

        public GameObject(string configuredName = "GameObject")
        {
            name = configuredName;
            transform = new Transform { gameObject = this };
            components.Add(transform);
        }

        public bool activeInHierarchy
        {
            get
            {
                return activeSelf && (transform.parent == null || transform.parent.gameObject.activeInHierarchy);
            }
        }

        public void SetActive(bool value) { activeSelf = value; }

        public T AddComponent<T>() where T : Component, new()
        {
            var component = new T { gameObject = this };
            components.Add(component);
            return component;
        }

        public Component AddComponent(Type type)
        {
            var component = (Component)Activator.CreateInstance(type);
            component.gameObject = this;
            components.Add(component);
            return component;
        }

        public T GetComponent<T>() where T : Component
        {
            return components.OfType<T>().FirstOrDefault();
        }
    }

    public sealed class Transform : Component
    {
        private readonly List<Transform> children = new List<Transform>();
        private Transform configuredParent;
        public Vector3 localPosition;
        public Quaternion localRotation = Quaternion.identity;
        public Vector3 localScale = Vector3.one;

        public Transform parent { get { return configuredParent; } }
        public int childCount { get { return children.Count; } }

        public Vector3 position
        {
            get { return configuredParent == null ? localPosition : configuredParent.position + localPosition; }
            set { localPosition = configuredParent == null ? value : value - configuredParent.position; }
        }

        public Quaternion rotation
        {
            get { return localRotation; }
            set { localRotation = value; }
        }

        public Vector3 lossyScale
        {
            get
            {
                if (configuredParent == null) return localScale;
                var parentScale = configuredParent.lossyScale;
                return new Vector3(parentScale.x * localScale.x, parentScale.y * localScale.y, parentScale.z * localScale.z);
            }
        }

        public void SetParent(Transform value, bool worldPositionStays)
        {
            if (configuredParent != null) configuredParent.children.Remove(this);
            configuredParent = value;
            if (configuredParent != null) configuredParent.children.Add(this);
        }

        public void SetPositionAndRotation(Vector3 configuredPosition, Quaternion configuredRotation)
        {
            position = configuredPosition;
            rotation = configuredRotation;
        }
    }

    public struct Vector2
    {
        public float x;
        public float y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero { get { return new Vector2(0f, 0f); } }
    }

    public struct Vector3
    {
        public float x;
        public float y;
        public float z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero { get { return new Vector3(0f, 0f, 0f); } }
        public static Vector3 one { get { return new Vector3(1f, 1f, 1f); } }
        public static Vector3 up { get { return new Vector3(0f, 1f, 0f); } }
        public static Vector3 operator +(Vector3 left, Vector3 right) { return new Vector3(left.x + right.x, left.y + right.y, left.z + right.z); }
        public static Vector3 operator -(Vector3 left, Vector3 right) { return new Vector3(left.x - right.x, left.y - right.y, left.z - right.z); }
        public static Vector3 operator *(Vector3 value, float scale) { return new Vector3(value.x * scale, value.y * scale, value.z * scale); }
    }

    public struct Quaternion
    {
        public float x;
        public float y;
        public float z;
        public float w;
        public static Quaternion identity { get { return new Quaternion { w = 1f }; } }
        public static Quaternion Euler(float x, float y, float z) { return new Quaternion { x = x, y = y, z = z, w = 1f }; }
        public static Quaternion operator *(Quaternion left, Quaternion right)
        {
            return new Quaternion { x = left.x + right.x, y = left.y + right.y, z = left.z + right.z, w = 1f };
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
            this.r = r; this.g = g; this.b = b; this.a = a;
        }
    }

    public struct Rect
    {
        public float x;
        public float y;
        public float width;
        public float height;
        public Rect(float x, float y, float width, float height)
        {
            this.x = x; this.y = y; this.width = width; this.height = height;
        }
    }

    public struct Matrix4x4
    {
        public float marker;
        public static Matrix4x4 identity { get { return new Matrix4x4 { marker = 1f }; } }
    }

    public struct Bounds
    {
        public Vector3 size;
        public Bounds(Vector3 size) { this.size = size; }
    }

    public struct LayerMask
    {
        public int value;
        public static implicit operator LayerMask(int configuredValue) { return new LayerMask { value = configuredValue }; }
        public static implicit operator int(LayerMask configuredMask) { return configuredMask.value; }
    }

    public static class Mathf
    {
        public static float Clamp(float value, float minimum, float maximum)
        {
            return value < minimum ? minimum : (value > maximum ? maximum : value);
        }
        public static int Clamp(int value, int minimum, int maximum)
        {
            return value < minimum ? minimum : (value > maximum ? maximum : value);
        }
        public static int Max(int left, int right) { return left > right ? left : right; }
        public static int RoundToInt(float value) { return (int)Math.Round(value); }
        public static bool Approximately(float left, float right) { return Math.Abs(left - right) < 0.00001f; }
        public static float Abs(float value) { return Math.Abs(value); }
        public static float Sin(float value) { return (float)Math.Sin(value); }
    }

    public enum CameraClearFlags { Skybox, Color, SolidColor = Color, Depth, Nothing }
    public enum RenderTextureFormat { ARGB32 }
    public enum RenderTextureReadWrite { Default }
    public enum FilterMode { Point, Bilinear, Trilinear }
    public enum TextureWrapMode { Repeat, Clamp }
    public enum HideFlags { None, DontSave }

    public class Texture : Object { }

    public sealed class Texture2D : Texture
    {
        private static readonly Texture2D Black = new Texture2D();
        public static Texture2D blackTexture { get { return Black; } }
    }

    public sealed class RenderTexture : Texture
    {
        private bool created;
        public static int CreatedCount { get; private set; }
        public static int ReleasedCount { get; private set; }
        public readonly int width;
        public readonly int height;
        public int antiAliasing = 1;
        public FilterMode filterMode;
        public TextureWrapMode wrapMode;
        public bool useMipMap;
        public bool autoGenerateMips;
        public HideFlags hideFlags;

        public RenderTexture(int width, int height, int depth, RenderTextureFormat format, RenderTextureReadWrite readWrite)
        {
            this.width = width;
            this.height = height;
        }

        public bool Create()
        {
            if (!created) CreatedCount++;
            created = true;
            return true;
        }

        public bool IsCreated() { return created; }
        public void Release()
        {
            if (created) ReleasedCount++;
            created = false;
        }

        public static void ResetCounters() { CreatedCount = 0; ReleasedCount = 0; }
    }

    public sealed class Material : Object { }
    public sealed class Sprite : Object
    {
        public Bounds bounds = new Bounds(Vector3.one);
    }

    public static class Shader
    {
        private static readonly Dictionary<string, int> Ids = new Dictionary<string, int>();
        public static int PropertyToID(string name)
        {
            int id;
            if (!Ids.TryGetValue(name, out id))
            {
                id = Ids.Count + 1;
                Ids[name] = id;
            }
            return id;
        }
    }

    public sealed class MaterialPropertyBlock
    {
        private readonly Dictionary<int, object> values = new Dictionary<int, object>();
        public void SetTexture(int id, Texture value) { values[id] = value; }
        public void SetFloat(int id, float value) { values[id] = value; }
        public void SetColor(int id, Color value) { values[id] = value; }
        public Texture GetTexture(int id) { return values.ContainsKey(id) ? values[id] as Texture : null; }
        public float GetFloat(int id) { return values.ContainsKey(id) ? (float)values[id] : 0f; }
        public Color GetColor(int id) { return values.ContainsKey(id) ? (Color)values[id] : new Color(); }
        internal void CopyFrom(MaterialPropertyBlock source)
        {
            values.Clear();
            if (source == null) return;
            foreach (var pair in source.values) values[pair.Key] = pair.Value;
        }
    }

    public class Renderer : Component
    {
        public Material sharedMaterial;
        public int sortingOrder;
    }

    public sealed class SpriteRenderer : Renderer
    {
        private readonly MaterialPropertyBlock storedBlock = new MaterialPropertyBlock();
        public Sprite sprite;
        public Color color;
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
        public void ResetPropertyBlockCounters() { GetPropertyBlockCount = 0; SetPropertyBlockCount = 0; }
    }

    public sealed class Camera : Behaviour
    {
        public int pixelWidth = 800;
        public int pixelHeight = 450;
        public int cullingMask = -1;
        public float depth;
        public CameraClearFlags clearFlags;
        public Color backgroundColor;
        public RenderTexture targetTexture;
        public bool orthographic = true;
        public float orthographicSize = 5f;
        public float fieldOfView = 60f;
        public float nearClipPlane = 0.3f;
        public float farClipPlane = 1000f;
        public float aspect = 16f / 9f;
        public Rect rect = new Rect(0f, 0f, 1f, 1f);
        public Matrix4x4 projectionMatrix = Matrix4x4.identity;
        public bool allowHDR = true;
        public bool allowMSAA = true;
        public bool useOcclusionCulling = true;
        public bool forceIntoRenderTexture;
    }

    public static class Application
    {
        public static bool isPlaying = true;
    }

    public static class Time
    {
        public static int frameCount;
        public static float deltaTime = 0.016f;
        public static float unscaledDeltaTime = 0.016f;
    }
}

namespace UnityEngine.Rendering
{
    using System;
    using UnityEngine;

    public struct ScriptableRenderContext { }

    public static class RenderPipelineManager
    {
        public static event Action<ScriptableRenderContext, Camera> beginCameraRendering;
        public static event Action<ScriptableRenderContext, Camera> endCameraRendering;
        public static int BeginSubscriberCount { get { return beginCameraRendering == null ? 0 : beginCameraRendering.GetInvocationList().Length; } }
        public static int EndSubscriberCount { get { return endCameraRendering == null ? 0 : endCameraRendering.GetInvocationList().Length; } }
        public static void RaiseBegin(Camera camera)
        {
            var handler = beginCameraRendering;
            if (handler != null) handler(new ScriptableRenderContext(), camera);
        }
        public static void RaiseEnd(Camera camera)
        {
            var handler = endCameraRendering;
            if (handler != null) handler(new ScriptableRenderContext(), camera);
        }
    }
}

namespace TMPro
{
    public sealed class TextMeshPro : UnityEngine.MonoBehaviour
    {
        public string text;
    }
}

namespace UnityEngine.InputSystem
{
    public enum Key
    {
        None, Digit1, Digit8, Digit9, Space, P, D, F1, F2, C
    }
}

public static class InputManager
{
    private static Func<UnityEngine.InputSystem.Key, bool> onKeyPressed;
    public static event Func<UnityEngine.InputSystem.Key, bool> OnKeyPressed
    {
        add { onKeyPressed += value; }
        remove { onKeyPressed -= value; }
    }
    public static int SubscriberCount { get { return onKeyPressed == null ? 0 : onKeyPressed.GetInvocationList().Length; } }
    public static bool Raise(UnityEngine.InputSystem.Key key)
    {
        if (onKeyPressed == null) return false;
        foreach (Func<UnityEngine.InputSystem.Key, bool> handler in onKeyPressed.GetInvocationList())
        {
            if (handler(key)) return true;
        }
        return false;
    }
}
