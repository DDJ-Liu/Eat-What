// Offline Unity object/scene boundary only. Registry, grammar and marker lifecycle use actual production sources.
using System;
using System.Collections.Generic;

namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.Class)] public sealed class DisallowMultipleComponent : Attribute { }
    [AttributeUsage(AttributeTargets.Field)] public sealed class SerializeField : Attribute { }
    [AttributeUsage(AttributeTargets.Field)] public sealed class TooltipAttribute : Attribute
    { public TooltipAttribute(string text) { } }
    public enum FindObjectsSortMode { None, InstanceID }
    public class Object
    {
        internal static readonly List<Object> Objects = new List<Object>();
        internal bool destroyed;
        public Object() { Objects.Add(this); }
        public static T[] FindObjectsByType<T>(FindObjectsSortMode sort) where T : Object
        {
            var result = new List<T>();
            foreach (var value in Objects)
                if (value is T item && !item.destroyed && (!(item is Component c) || c.gameObject.activeInHierarchy)) result.Add(item);
            return result.ToArray();
        }
        public static void Destroy(Object value) { value.destroyed = true; }
        public static bool operator ==(Object a, Object b)
        {
            bool an = ReferenceEquals(a, null) || a.destroyed, bn = ReferenceEquals(b, null) || b.destroyed;
            return an || bn ? an == bn : ReferenceEquals(a, b);
        }
        public static bool operator !=(Object a, Object b) { return !(a == b); }
        public override bool Equals(object obj) { return ReferenceEquals(this, obj); }
        public override int GetHashCode() { return base.GetHashCode(); }
    }
    public class Component : Object
    {
        public GameObject gameObject;
        public Transform transform => gameObject.transform;
    }
    public class Behaviour : Component
    {
        public bool enabled = true;
        public bool isActiveAndEnabled => enabled && gameObject.activeInHierarchy && !destroyed;
    }
    public class MonoBehaviour : Behaviour { }
    public sealed class GameObject : Object
    {
        public SceneManagement.Scene scene;
        public readonly Transform transform;
        public bool activeSelf = true;
        public bool activeInHierarchy => activeSelf && (transform.parent == null || transform.parent.gameObject.activeInHierarchy);
        private readonly List<Component> components = new List<Component>();
        private readonly List<GameObject> children = new List<GameObject>();
        public GameObject(SceneManagement.Scene scene, GameObject parent = null)
        {
            this.scene = scene; transform = new Transform { gameObject = this };
            if (parent != null) { transform.parent = parent.transform; parent.children.Add(this); }
        }
        public T AddComponent<T>() where T : Component, new()
        { var value = new T { gameObject = this }; components.Add(value); return value; }
        public T[] GetComponentsInChildren<T>(bool includeInactive) where T : Component
        {
            var result = new List<T>();
            if (includeInactive || activeInHierarchy)
            {
                foreach (var value in components) if (value is T item && !item.destroyed) result.Add(item);
                foreach (var child in children) result.AddRange(child.GetComponentsInChildren<T>(includeInactive));
            }
            return result.ToArray();
        }
    }
    public sealed class Transform : Component
    {
        public Transform parent;
        public Vector3 localPosition;
        public Vector3 position
        {
            get => parent == null ? localPosition : parent.position + localPosition;
            set => localPosition = parent == null ? value : value - parent.position;
        }
    }
    public struct Vector3
    {
        public float x,y,z;
        public Vector3(float x,float y,float z) { this.x=x; this.y=y; this.z=z; }
        public static Vector3 operator +(Vector3 a,Vector3 b) { return new Vector3(a.x+b.x,a.y+b.y,a.z+b.z); }
        public static Vector3 operator -(Vector3 a,Vector3 b) { return new Vector3(a.x-b.x,a.y-b.y,a.z-b.z); }
    }
    public static class Debug
    {
        public static readonly List<string> Warnings = new List<string>();
        public static void LogWarning(string message,Object context) { Warnings.Add(message); }
    }
}
namespace UnityEngine.SceneManagement
{
    public struct Scene
    {
        private int id;
        public string name;
        public bool isLoaded;
        public Scene(int id,string name) { this.id=id; this.name=name; isLoaded=true; }
        public bool IsValid() { return id != 0; }
        public UnityEngine.GameObject[] GetRootGameObjects()
        {
            var result = new List<UnityEngine.GameObject>();
            foreach (var value in UnityEngine.Object.Objects)
                if (value is UnityEngine.GameObject go && !go.destroyed && go.scene == this && go.transform.parent == null) result.Add(go);
            return result.ToArray();
        }
        public static bool operator ==(Scene a,Scene b) { return a.id==b.id; }
        public static bool operator !=(Scene a,Scene b) { return a.id!=b.id; }
        public override bool Equals(object value) { return value is Scene s && s.id==id; }
        public override int GetHashCode() { return id; }
    }
}
