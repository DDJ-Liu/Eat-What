// Isolated Unity API stubs for AI-000086. Unlike the broad ShortCycle stubs,
// Transform.position/localPosition share one translation/scale-aware state so
// the real DragContainer/DragLimit coordinate boundary is exercised.
using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine
{
    public class Object
    {
        public string name;
    }

    public class GameObject : Object
    {
        public bool activeSelf = true;
        public bool activeInHierarchy { get { return activeSelf; } }
    }

    public class Component : Object
    {
        public GameObject gameObject = new GameObject();
        public Transform transform = new Transform();
    }

    public class Behaviour : Component
    {
        public bool enabled = true;
        public bool isActiveAndEnabled { get { return enabled && gameObject.activeInHierarchy; } }
    }

    public class MonoBehaviour : Behaviour
    {
        protected T GetComponent<T>() where T : class { return null; }

        protected bool TryGetComponent<T>(out T component) where T : class
        {
            component = null;
            return false;
        }

        protected Coroutine StartCoroutine(IEnumerator routine)
        {
            Drain(routine);
            return new Coroutine();
        }

        protected void StopCoroutine(Coroutine routine) { }

        private static void Drain(IEnumerator routine)
        {
            while (routine.MoveNext())
            {
                var nested = routine.Current as IEnumerator;
                if (nested != null) Drain(nested);
            }
        }
    }

    public class Coroutine { }

    public class Transform : Object
    {
        private Vector3 _localPosition = Vector3.zero;
        private Vector3 _localScale = Vector3.one;
        private Transform _parent;

        public int LocalPositionWriteCount { get; private set; }
        public int WorldPositionWriteCount { get; private set; }

        public Transform parent
        {
            get { return _parent; }
            set { _parent = value; }
        }

        public Vector3 localPosition
        {
            get { return _localPosition; }
            set
            {
                _localPosition = value;
                LocalPositionWriteCount++;
            }
        }

        public Vector3 position
        {
            get { return _parent == null ? _localPosition : _parent.TransformPoint(_localPosition); }
            set
            {
                _localPosition = _parent == null ? value : _parent.InverseTransformPoint(value);
                WorldPositionWriteCount++;
            }
        }

        public Vector3 localScale
        {
            get { return _localScale; }
            set { _localScale = value; }
        }

        public Vector3 lossyScale
        {
            get
            {
                if (_parent == null) return _localScale;
                Vector3 parentScale = _parent.lossyScale;
                return new Vector3(parentScale.x * _localScale.x, parentScale.y * _localScale.y, parentScale.z * _localScale.z);
            }
        }

        public Vector3 right { get { return Vector3.right; } }
        public Vector3 up { get { return Vector3.up; } }

        public Vector3 TransformPoint(Vector3 point)
        {
            Vector3 scaled = Scale(point, _localScale);
            Vector3 inParent = _localPosition + scaled;
            return _parent == null ? inParent : _parent.TransformPoint(inParent);
        }

        public Vector3 InverseTransformPoint(Vector3 point)
        {
            Vector3 inParent = _parent == null ? point : _parent.InverseTransformPoint(point);
            return Divide(inParent - _localPosition, _localScale);
        }

        public Vector3 TransformVector(Vector3 vector)
        {
            Vector3 scaled = Scale(vector, _localScale);
            return _parent == null ? scaled : _parent.TransformVector(scaled);
        }

        public Vector3 InverseTransformVector(Vector3 vector)
        {
            Vector3 inParent = _parent == null ? vector : _parent.InverseTransformVector(vector);
            return Divide(inParent, _localScale);
        }

        public Vector3 TransformDirection(Vector3 direction)
        {
            return _parent == null ? direction : _parent.TransformDirection(direction);
        }

        public Vector3 InverseTransformDirection(Vector3 direction)
        {
            return _parent == null ? direction : _parent.InverseTransformDirection(direction);
        }

        public void ResetWriteCounts()
        {
            LocalPositionWriteCount = 0;
            WorldPositionWriteCount = 0;
        }

        private static Vector3 Scale(Vector3 value, Vector3 scale)
        {
            return new Vector3(value.x * scale.x, value.y * scale.y, value.z * scale.z);
        }

        private static Vector3 Divide(Vector3 value, Vector3 scale)
        {
            return new Vector3(value.x / scale.x, value.y / scale.y, value.z / scale.z);
        }
    }

    public class RectTransform : Transform
    {
        public Rect rect;
        public Vector2 pivot = new Vector2(0.5f, 0.5f);
    }

    public class Collider2D : Behaviour { }

    public class BoxCollider2D : Collider2D
    {
        public Vector2 size;
        public Vector2 offset;
    }

    public struct Rect
    {
        public float width;
        public float height;

        public Rect(float x, float y, float width, float height)
        {
            this.width = width;
            this.height = height;
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
        public static Vector2 up { get { return new Vector2(0f, 1f); } }
        public static Vector2 down { get { return new Vector2(0f, -1f); } }
        public static Vector2 left { get { return new Vector2(-1f, 0f); } }
        public static Vector2 right { get { return new Vector2(1f, 0f); } }
        public static implicit operator Vector3(Vector2 value) { return new Vector3(value.x, value.y, 0f); }
        public static bool operator ==(Vector2 left, Vector2 right) { return left.x == right.x && left.y == right.y; }
        public static bool operator !=(Vector2 left, Vector2 right) { return !(left == right); }
        public override bool Equals(object value) { return value is Vector2 && this == (Vector2)value; }
        public override int GetHashCode() { return x.GetHashCode() ^ y.GetHashCode(); }
    }

    public struct Vector3
    {
        public float x;
        public float y;
        public float z;

        public Vector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static Vector3 zero { get { return new Vector3(0f, 0f, 0f); } }
        public static Vector3 one { get { return new Vector3(1f, 1f, 1f); } }
        public static Vector3 right { get { return new Vector3(1f, 0f, 0f); } }
        public static Vector3 up { get { return new Vector3(0f, 1f, 0f); } }
        public float magnitude { get { return (float)Math.Sqrt((x * x) + (y * y) + (z * z)); } }
        public Vector3 normalized { get { float value = magnitude; return value <= 0f ? zero : this / value; } }
        public static implicit operator Vector2(Vector3 value) { return new Vector2(value.x, value.y); }
        public static Vector3 operator +(Vector3 left, Vector3 right) { return new Vector3(left.x + right.x, left.y + right.y, left.z + right.z); }
        public static Vector3 operator -(Vector3 left, Vector3 right) { return new Vector3(left.x - right.x, left.y - right.y, left.z - right.z); }
        public static Vector3 operator *(Vector3 value, float multiplier) { return new Vector3(value.x * multiplier, value.y * multiplier, value.z * multiplier); }
        public static Vector3 operator /(Vector3 value, float divisor) { return new Vector3(value.x / divisor, value.y / divisor, value.z / divisor); }
        public static bool operator ==(Vector3 left, Vector3 right) { return left.x == right.x && left.y == right.y && left.z == right.z; }
        public static bool operator !=(Vector3 left, Vector3 right) { return !(left == right); }
        public override bool Equals(object value) { return value is Vector3 && this == (Vector3)value; }
        public override int GetHashCode() { return x.GetHashCode() ^ y.GetHashCode() ^ z.GetHashCode(); }

        public static Vector3 ClampMagnitude(Vector3 value, float maximum)
        {
            float length = value.magnitude;
            return length > maximum && length > 0f ? value.normalized * maximum : value;
        }

        public static Vector3 Lerp(Vector3 from, Vector3 to, float progress)
        {
            return LerpUnclamped(from, to, Mathf.Clamp01(progress));
        }

        public static Vector3 LerpUnclamped(Vector3 from, Vector3 to, float progress)
        {
            return from + ((to - from) * progress);
        }

        public static float Distance(Vector3 left, Vector3 right) { return (left - right).magnitude; }
    }

    public static class Mathf
    {
        public static float Abs(float value) { return Math.Abs(value); }
        public static float Min(float left, float right) { return Math.Min(left, right); }
        public static float Max(float left, float right) { return Math.Max(left, right); }
        public static float Clamp(float value, float minimum, float maximum) { return Math.Max(minimum, Math.Min(maximum, value)); }
        public static float Clamp01(float value) { return Clamp(value, 0f, 1f); }
        public static bool Approximately(float left, float right) { return Math.Abs(left - right) <= 0.00001f; }
    }

    public class AnimationCurve
    {
        public static AnimationCurve EaseInOut(float timeStart, float valueStart, float timeEnd, float valueEnd) { return new AnimationCurve(); }
        public float Evaluate(float time) { return time; }
    }

    public static class Time
    {
        public static float deltaTime = 1f;
        public static float unscaledDeltaTime = 1f;
    }

    public static class Debug
    {
        public static readonly List<string> Messages = new List<string>();
        public static void LogWarning(string message) { Messages.Add(message); }
        public static void LogError(string message) { Messages.Add(message); }
    }

    [AttributeUsage(AttributeTargets.Class)] public sealed class RequireComponent : Attribute { public RequireComponent(Type type) { } }
    [AttributeUsage(AttributeTargets.Field)] public sealed class HeaderAttribute : Attribute { public HeaderAttribute(string value) { } }
    [AttributeUsage(AttributeTargets.Field)] public sealed class TooltipAttribute : Attribute { public TooltipAttribute(string value) { } }
    [AttributeUsage(AttributeTargets.Field)] public sealed class RangeAttribute : Attribute { public RangeAttribute(float minimum, float maximum) { } }
    [AttributeUsage(AttributeTargets.Field)] public sealed class SerializeField : Attribute { }
}

namespace UnityEngine.Events
{
    public class UnityEvent
    {
        private event Action listeners;
        public void Invoke() { var callback = listeners; if (callback != null) callback(); }
    }

    public class UnityEvent<T>
    {
        private event Action<T> listeners;
        public void Invoke(T value) { var callback = listeners; if (callback != null) callback(value); }
    }
}

public static class Tools
{
    public static UnityEngine.Vector3 MousePosition;
    public static UnityEngine.Vector3 getMousePos() { return MousePosition; }
}

public class TransitionController : UnityEngine.MonoBehaviour
{
    public void RegisterBehaviour(TransitionBehaviour behaviour) { }
}
