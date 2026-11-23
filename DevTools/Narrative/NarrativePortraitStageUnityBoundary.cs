// External Unity hierarchy, clone/destroy transport and given Sprite rectangles only; no stage/layout/highlight logic.
using System;
using System.Collections.Generic;
namespace UnityEngine
{
    public class Object
    {
        public string name; public bool Destroyed;
        public static void Destroy(Object value) { if (value != null) value.Destroyed = true; }
        public static T Instantiate<T>(T original, Transform parent, bool worldPositionStays) where T : Object
        {
            var source = original as UI.Image; if (source == null) throw new NotSupportedException("Boundary clones Image handles only.");
            var node = new GameObject(source.gameObject.name + "(Clone)", typeof(RectTransform)); node.transform.SetParent(parent, false);
            var from = (RectTransform)source.transform; var to = (RectTransform)node.transform;
            to.anchorMin = from.anchorMin; to.anchorMax = from.anchorMax; to.pivot = from.pivot; to.sizeDelta = from.sizeDelta;
            to.anchoredPosition = from.anchoredPosition; to.localScale = from.localScale; to.localRotation = from.localRotation;
            var image = node.AddComponent<UI.Image>(); image.sprite = source.sprite; image.color = source.color;
            image.type = source.type; image.raycastTarget = source.raycastTarget; image.preserveAspect = source.preserveAspect;
            node.SetActive(source.gameObject.activeSelf); return (T)(Object)image;
        }
    }
    public class Component : Object
    {
        public GameObject gameObject;
        public Transform transform { get { return gameObject.transform; } }
    }
    public class MonoBehaviour : Component { }
    public class Transform : Component
    {
        readonly List<Transform> children = new List<Transform>();
        public Transform parent { get; private set; }
        public Vector3 localScale = Vector3.one; public Quaternion localRotation = Quaternion.identity;
        public int childCount { get { return children.Count; } }
        public Transform GetChild(int i) { return children[i]; }
        public int GetSiblingIndex() { return parent == null ? 0 : parent.children.IndexOf(this); }
        public void SetParent(Transform next, bool worldPositionStays)
        { if (parent != null) parent.children.Remove(this); parent = next; if (next != null) next.children.Add(this); }
        public void SetAsLastSibling() { if (parent != null) { parent.children.Remove(this); parent.children.Add(this); } }
    }
    public sealed class RectTransform : Transform { public Vector2 anchorMin, anchorMax, pivot, sizeDelta, anchoredPosition; }
    public sealed class GameObject : Object
    {
        readonly Dictionary<Type, Component> components = new Dictionary<Type, Component>();
        public Transform transform { get; private set; } public bool activeSelf = true;
        public GameObject(string value, params Type[] types)
        {
            name = value; var kind = Array.IndexOf(types, typeof(RectTransform)) < 0 ? typeof(Transform) : typeof(RectTransform);
            transform = (Transform)Add(kind); foreach (var type in types) if (type != kind) Add(type);
        }
        Component Add(Type kind) { var c = (Component)Activator.CreateInstance(kind); c.gameObject = this; components.Add(kind, c); return c; }
        public T AddComponent<T>() where T : Component, new() { return (T)Add(typeof(T)); }
        public void SetActive(bool value) { activeSelf = value; }
    }
    public struct Vector2 { public float x, y; public Vector2(float x, float y) { this.x = x; this.y = y; } }
    public struct Vector3 { public float x, y, z; public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; } public static Vector3 one { get { return new Vector3(1, 1, 1); } } }
    public struct Quaternion { public static Quaternion identity { get { return new Quaternion(); } } }
    public struct Rect { public float x,y,width,height; public Rect(float x,float y,float width,float height) { this.x=x;this.y=y;this.width=width;this.height=height; } }
    public sealed class Sprite : Object { public Rect rect; }
    public struct Color { public float r,g,b,a; public Color(float r,float g,float b,float a) { this.r=r;this.g=g;this.b=b;this.a=a; } }
    public static class Debug { public static readonly List<string> Warnings = new List<string>(); public static void LogWarning(string message) { Warnings.Add(message); } }
    public sealed class SerializeField : Attribute { }
    public sealed class TooltipAttribute : Attribute { public TooltipAttribute(string text) { } }
    public sealed class DisallowMultipleComponent : Attribute { }
    public sealed class RequireComponent : Attribute { public RequireComponent(Type type) { } }
}
namespace UnityEngine.UI
{
    public sealed class Image : UnityEngine.Component
    { public enum Type { Simple } public Type type; public UnityEngine.Sprite sprite; public UnityEngine.Color color; public bool raycastTarget=true, preserveAspect; }
}
