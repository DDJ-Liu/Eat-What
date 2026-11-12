// Offline Unity boundary substitute for WalkAreaLogicTests. Production geometry stays in Unity Collider2D.
using System;
using System.Collections.Generic;

namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.Class)] public sealed class DisallowMultipleComponent : Attribute { }
    [AttributeUsage(AttributeTargets.Field)] public sealed class SerializeField : Attribute { }
    [AttributeUsage(AttributeTargets.Class)] public sealed class CreateAssetMenuAttribute : Attribute
    {
        public string menuName;
    }
    public class ScriptableObject { }
    public class Collider2D { }
    [AttributeUsage(AttributeTargets.Field)] public sealed class HeaderAttribute : Attribute
    {
        public HeaderAttribute(string value) { }
    }
    [AttributeUsage(AttributeTargets.Field)] public sealed class MinAttribute : Attribute
    {
        public MinAttribute(float value) { }
    }
    public static class Mathf
    {
        public static float Clamp(float v, float min, float max) => Math.Min(max, Math.Max(min, v));
        public static float Max(float a, float b) => Math.Max(a, b);
        public static float Min(float a, float b) => Math.Min(a, b);
        public static float Abs(float a) => Math.Abs(a);
    }
    public static class Time
    {
        public static float deltaTime = 1f;
        public static float fixedDeltaTime = 1f;
    }
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero => new Vector2(0f, 0f);
        public float sqrMagnitude => x * x + y * y;
        public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.x + b.x, a.y + b.y);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.x - b.x, a.y - b.y);
        public static bool operator ==(Vector2 a, Vector2 b) => a.x == b.x && a.y == b.y;
        public static bool operator !=(Vector2 a, Vector2 b) => !(a == b);
        public override bool Equals(object value) => value is Vector2 other && this == other;
        public override int GetHashCode() => x.GetHashCode() ^ y.GetHashCode();
    }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero => new Vector3(0f, 0f, 0f);
        public static Vector3 one => new Vector3(1f, 1f, 1f);
        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);
        public static Vector3 Scale(Vector3 a, Vector3 b) => new Vector3(a.x*b.x,a.y*b.y,a.z*b.z);
        public override bool Equals(object o) => o is Vector3 v && x==v.x && y==v.y && z==v.z;
        public override int GetHashCode() => x.GetHashCode() ^ y.GetHashCode() ^ z.GetHashCode();
    }
    // Planar transform substitute; native Unity matrices remain an ENGINE check.
    public struct Quaternion
    {
        public float degrees;
        public static Quaternion Euler(float x, float y, float z) => new Quaternion { degrees=z };
        public static Vector3 operator *(Quaternion q, Vector3 v)
        {
            var r=q.degrees*Math.PI/180d;
            return new Vector3((float)(v.x*Math.Cos(r)-v.y*Math.Sin(r)),
                (float)(v.x*Math.Sin(r)+v.y*Math.Cos(r)),v.z);
        }
    }
    public sealed class Transform
    {
        public Transform parent;
        public Vector3 localPosition;
        public Quaternion localRotation;
        public Vector3 localScale = new Vector3(1f, 1f, 1f);
        public readonly Dictionary<Type,object> components = new Dictionary<Type,object>();
        public T GetComponent<T>() where T:class => components.TryGetValue(typeof(T),out var o) ? o as T : null;
        public Vector3 position
        {
            get => parent == null ? localPosition : parent.TransformPoint(localPosition);
            set => localPosition = parent == null ? value : parent.InverseTransformPoint(value);
        }
        public Vector3 TransformPoint(Vector3 p)
        {
            var v=localPosition+localRotation*Vector3.Scale(localScale,p);
            return parent==null ? v : parent.TransformPoint(v);
        }
        public Vector3 InverseTransformPoint(Vector3 p)
        {
            var v=(parent==null ? p : parent.InverseTransformPoint(p))-localPosition;
            v=new Quaternion {degrees=-localRotation.degrees}*v;
            return new Vector3(v.x/localScale.x,v.y/localScale.y,v.z/localScale.z);
        }
    }
    [Flags] public enum RigidbodyConstraints2D
    {
        None = 0, FreezePositionX = 1, FreezePositionY = 2, FreezeRotation = 4
    }
    public sealed class Rigidbody2D
    {
        public Vector2 position;
        public Vector2 linearVelocity;
        public RigidbodyConstraints2D constraints;
        public int moveCalls;
        public void MovePosition(Vector2 next)
        {
            linearVelocity = new Vector2(next.x - position.x, next.y - position.y);
            position = next;
            moveCalls++;
        }
    }
    public class MonoBehaviour
    {
        public Transform transform = new Transform();
        public bool isActiveAndEnabled = true;
        public Rigidbody2D attachedRigidbody;
        protected bool TryGetComponent<T>(out T component) where T : class
        {
            component = transform.GetComponent<T>() ?? attachedRigidbody as T;
            return component != null;
        }
    }
    public static class Debug
    {
        public static void LogWarning(string message, object context) { }
    }
    public sealed class PolygonCollider2D
    {
        public Vector2[][] paths = Array.Empty<Vector2[]>();
        public bool isActiveAndEnabled = true;
        public Vector2 position;
        public Vector2 scale = new Vector2(1f, 1f);
        public float rotationDegrees;
        public int pathCount => paths.Length;

        private Vector2 World(Vector2 point)
        {
            var radians = rotationDegrees * Math.PI / 180d;
            var x = point.x * scale.x;
            var y = point.y * scale.y;
            return new Vector2((float)(position.x + x * Math.Cos(radians) - y * Math.Sin(radians)),
                (float)(position.y + x * Math.Sin(radians) + y * Math.Cos(radians)));
        }
        public bool OverlapPoint(Vector2 point)
        {
            foreach (var path in paths)
            {
                if (path == null || path.Length < 3) continue;
                var inside = false;
                for (var i = 0; i < path.Length; i++)
                {
                    var a = World(path[i]);
                    var b = World(path[(i + 1) % path.Length]);
                    if ((a.y > point.y) != (b.y > point.y) &&
                        point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x)
                        inside = !inside;
                }
                if (inside) return true;
            }
            return false;
        }
        public Vector2 ClosestPoint(Vector2 point)
        {
            if (OverlapPoint(point)) return point;
            var best = Vector2.zero;
            var distance = float.PositiveInfinity;
            foreach (var path in paths)
            {
                if (path == null || path.Length < 2) continue;
                for (var i = 0; i < path.Length; i++)
                {
                    var a = World(path[i]);
                    var b = World(path[(i + 1) % path.Length]);
                    var dx = b.x - a.x;
                    var dy = b.y - a.y;
                    var denominator = dx * dx + dy * dy;
                    var t = denominator == 0f ? 0f : Mathf.Clamp(
                        ((point.x - a.x) * dx + (point.y - a.y) * dy) / denominator, 0f, 1f);
                    var candidate = new Vector2(a.x + dx * t, a.y + dy * t);
                    var candidateDistance = (candidate - point).sqrMagnitude;
                    if (candidateDistance < distance)
                    {
                        best = candidate;
                        distance = candidateDistance;
                    }
                }
            }
            return best;
        }
    }
}

namespace UnityEngine.Serialization
{
    [AttributeUsage(AttributeTargets.Field)] public sealed class FormerlySerializedAsAttribute : Attribute
    {
        public FormerlySerializedAsAttribute(string value) { }
    }
}
