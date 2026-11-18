// External Unity hierarchy, cloning and deferred destruction only. NpcShell/DepthScale are actual sources.
using System;
using System.Collections.Generic;

namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.Class)] public sealed class DisallowMultipleComponent : Attribute { }
    [AttributeUsage(AttributeTargets.Field)] public sealed class SerializeField : Attribute { }
    [AttributeUsage(AttributeTargets.Field)] public sealed class TooltipAttribute : Attribute { public TooltipAttribute(string text) { } }
    [AttributeUsage(AttributeTargets.Class)] public sealed class CreateAssetMenuAttribute : Attribute { public string menuName; }
    public class ScriptableObject { }
    public class Collider2D { }
    public static class Application { public static bool isPlaying = true; }
    public static class Mathf { public static float Abs(float x) => Math.Abs(x); }
    public static class Debug
    {
        public static readonly List<string> Warnings = new List<string>();
        public static void LogWarning(string text, object context) { Warnings.Add(text); }
    }
    public class Object
    {
        public static readonly List<GameObject> PendingDestruction = new List<GameObject>();
        public static GameObject Instantiate(GameObject prefab, Transform parent, bool worldPositionStays)
        {
            var result = new GameObject(prefab.name + "(Clone)");
            result.transform.localPosition = prefab.transform.localPosition;
            result.transform.localScale = prefab.transform.localScale;
            result.transform.localRotation = prefab.transform.localRotation;
            result.transform.SetParent(parent, worldPositionStays);
            result.SetActive(prefab.activeSelf);
            foreach (var child in prefab.transform.Children) Instantiate(child.gameObject, result.transform, false);
            return result;
        }
        public static void Destroy(GameObject value) { PendingDestruction.Add(value); }
    }
    public class MonoBehaviour : Object
    {
        public GameObject gameObject;
        public Transform transform => gameObject.transform;
        public bool isActiveAndEnabled = true;
    }
    public sealed class GameObject
    {
        public readonly string name;
        public readonly Transform transform;
        public bool activeSelf = true;
        public GameObject(string name) { this.name=name; transform=new Transform(this); }
        public void SetActive(bool value) { activeSelf=value; }
        public T Add<T>() where T:MonoBehaviour,new()
        {
            var value=new T { gameObject=this };
            transform.Components[typeof(T)]=value;
            return value;
        }
    }
    public sealed class Transform
    {
        public readonly GameObject gameObject;
        private Transform ancestor;
        public Transform parent => ancestor;
        public readonly List<Transform> Children = new List<Transform>();
        public readonly Dictionary<Type,object> Components = new Dictionary<Type,object>();
        public Vector3 localPosition;
        public Vector3 localScale = Vector3.one;
        public Quaternion localRotation;
        public Transform(GameObject go) { gameObject=go; }
        public int childCount => Children.Count;
        public Transform GetChild(int i) { return Children[i]; }
        public bool IsChildOf(Transform candidate)
        { for (var p=this; p!=null; p=p.parent) if (ReferenceEquals(p,candidate)) return true; return false; }
        public void SetParent(Transform value,bool worldPositionStays)
        {
            var world=position;
            if (ancestor!=null) ancestor.Children.Remove(this);
            ancestor=value;
            if (ancestor!=null) ancestor.Children.Add(this);
            if (worldPositionStays) position=world;
        }
        public T GetComponent<T>() where T:class
        { return Components.TryGetValue(typeof(T),out var result) ? result as T : null; }
        public Vector3 position
        { get => parent==null ? localPosition : parent.TransformPoint(localPosition);
          set => localPosition=parent==null ? value : parent.InverseTransformPoint(value); }
        public Vector3 TransformPoint(Vector3 p)
        {
            var value=localPosition+localRotation*Vector3.Scale(localScale,p);
            return parent==null ? value : parent.TransformPoint(value);
        }
        public Vector3 InverseTransformPoint(Vector3 p)
        {
            var v=(parent==null ? p : parent.InverseTransformPoint(p))-localPosition;
            v=new Quaternion {degrees=-localRotation.degrees}*v;
            return new Vector3(v.x/localScale.x,v.y/localScale.y,v.z/localScale.z);
        }
    }
    public struct Quaternion
    {
        public float degrees;
        public static Quaternion Euler(float x,float y,float z) => new Quaternion {degrees=z};
        public static Vector3 operator *(Quaternion q,Vector3 v)
        {
            var a=q.degrees*Math.PI/180d;
            return new Vector3((float)(v.x*Math.Cos(a)-v.y*Math.Sin(a)),(float)(v.x*Math.Sin(a)+v.y*Math.Cos(a)),v.z);
        }
    }
    public struct Vector3
    {
        public float x,y,z;
        public Vector3(float x,float y,float z) { this.x=x; this.y=y; this.z=z; }
        public static Vector3 zero => new Vector3(0,0,0);
        public static Vector3 one => new Vector3(1,1,1);
        public static Vector3 operator +(Vector3 a,Vector3 b) => new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
        public static Vector3 operator -(Vector3 a,Vector3 b) => new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);
        public static Vector3 Scale(Vector3 a,Vector3 b) => new Vector3(a.x*b.x,a.y*b.y,a.z*b.z);
        public override bool Equals(object o) => o is Vector3 v && x==v.x&&y==v.y&&z==v.z;
        public override int GetHashCode() => x.GetHashCode()^y.GetHashCode()^z.GetHashCode();
    }
}
