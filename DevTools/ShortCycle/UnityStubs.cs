// Editor-external compile stubs for CK01-C. This file is not under Assets and
// is only consumed by Test-CK01CShortCycle.ps1.
using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine
{
    public class Object
    {
        public string name;

        public static T Instantiate<T>(T original, Transform parent) where T : Object, new()
        {
            if (original == null) return null;
            var clone = new T();
            var fields = typeof(T).GetFields(
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.DeclaredOnly);
            foreach (var field in fields) field.SetValue(clone, field.GetValue(original));
            var behaviour = clone as Behaviour;
            if (behaviour != null) behaviour.transform.SetParent(parent, false);
            return clone;
        }
    }

    public class ScriptableObject : Object
    {
        public static T CreateInstance<T>() where T : ScriptableObject, new() { return new T(); }
    }
    public class Component : Object
    {
        private GameObject attachedGameObject;
        public GameObject gameObject
        {
            get
            {
                if (attachedGameObject == null)
                {
                    var generated = new GameObject();
                    generated.AttachExistingComponent(this);
                }
                return attachedGameObject;
            }
        }
        public Transform transform { get { return gameObject.transform; } }
        internal void Attach(GameObject owner) { attachedGameObject = owner; }
        public T GetComponent<T>() where T : class { return gameObject.GetComponent<T>(); }
        public T[] GetComponentsInChildren<T>() { return gameObject.GetComponentsInChildren<T>(false); }
        public T[] GetComponentsInChildren<T>(bool includeInactive) { return gameObject.GetComponentsInChildren<T>(includeInactive); }
    }
    public class Behaviour : Component
    {
        public bool enabled = true;
        public bool isActiveAndEnabled { get { return enabled && gameObject.activeSelf; } }
    }
    public class MonoBehaviour : Behaviour
    {
        private readonly List<Coroutine> activeCoroutines = new List<Coroutine>();

        public int StartedCoroutineCount { get; private set; }
        public int StoppedCoroutineCount { get; private set; }
        public int ActiveCoroutineCount { get { return activeCoroutines.Count; } }

        protected Coroutine StartCoroutine(IEnumerator routine)
        {
            if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
                throw new InvalidOperationException("Coroutine cannot start on an inactive behaviour.");
            if (routine == null)
                throw new ArgumentNullException("routine");
            StartedCoroutineCount++;
            var coroutine = new Coroutine(routine);
            activeCoroutines.Add(coroutine);
            return coroutine;
        }

        protected void StopCoroutine(Coroutine routine)
        {
            if (routine == null || routine.IsStopped)
                return;

            StoppedCoroutineCount++;
            routine.Stop();
            activeCoroutines.Remove(routine);
        }

        public bool AdvanceCoroutinesOnce()
        {
            var snapshot = activeCoroutines.ToArray();
            var yielded = false;
            foreach (var coroutine in snapshot)
            {
                if (coroutine.IsStopped)
                {
                    activeCoroutines.Remove(coroutine);
                    continue;
                }

                if (coroutine.MoveNext())
                {
                    yielded = true;
                    continue;
                }

                activeCoroutines.Remove(coroutine);
            }

            return yielded;
        }
    }
    public class Coroutine
    {
        private readonly IEnumerator routine;

        internal Coroutine(IEnumerator routine) { this.routine = routine; }
        internal bool IsStopped { get; private set; }

        internal bool MoveNext()
        {
            return !IsStopped && routine.MoveNext();
        }

        internal void Stop()
        {
            if (IsStopped)
                return;

            IsStopped = true;
            var disposable = routine as IDisposable;
            if (disposable != null)
            {
                disposable.Dispose();
            }
        }
    }
    public class Sprite : Object { }
    public class Material : Object { }
    public class BoxCollider2D : Behaviour
    {
        public Vector2 size;
        public Vector2 offset;
    }
    public static class Shader
    {
        public static int PropertyToID(string value) { return value == null ? 0 : value.GetHashCode(); }
    }
    public sealed class MaterialPropertyBlock
    {
        public void SetColor(int propertyId, Color value) { }
        public void SetFloat(int propertyId, float value) { }
        public void SetVector(int propertyId, Vector4 value) { }
    }
    public class Renderer : Behaviour { }
    public class SpriteRenderer : Renderer
    {
        public Sprite sprite;
        public Material material;
        public Material sharedMaterial;
        public Color color;
        public int sortingLayerID;
        public int sortingOrder;
        public void GetPropertyBlock(MaterialPropertyBlock block) { }
        public void SetPropertyBlock(MaterialPropertyBlock block) { }
    }
    public class TextMesh : Behaviour
    {
        public string text;
    }
    public class GameObject : Object
    {
        private readonly List<Component> components = new List<Component>();
        public bool activeSelf = true;
        public bool activeInHierarchy
        {
            get { return activeSelf && (transform.parent == null || transform.parent.gameObject.activeInHierarchy); }
        }
        public bool ThrowOnSetActive { get; set; }
        public int SetActiveCallCount { get; private set; }
        public int layer;
        public Transform transform { get; private set; }
        public UnityEngine.SceneManagement.Scene scene;

        public GameObject()
        {
            transform = new Transform();
            AttachExistingComponent(transform);
        }

        internal void AttachExistingComponent(Component component)
        {
            if (component == null || components.Contains(component)) return;
            component.Attach(this);
            components.Add(component);
        }

        public T AddComponent<T>() where T : Component, new()
        {
            var component = new T();
            AttachExistingComponent(component);
            return component;
        }

        public T GetComponent<T>() where T : class
        {
            for (var index = 0; index < components.Count; index++)
                if (components[index] is T) return components[index] as T;
            return null;
        }

        public T[] GetComponentsInChildren<T>() { return GetComponentsInChildren<T>(false); }

        public T[] GetComponentsInChildren<T>(bool includeInactive)
        {
            var result = new List<T>();
            CollectComponentsInChildren(this, includeInactive, result);
            return result.ToArray();
        }

        private static void CollectComponentsInChildren<T>(GameObject current, bool includeInactive, List<T> result)
        {
            if (current == null || (!includeInactive && !current.activeInHierarchy)) return;
            for (var index = 0; index < current.components.Count; index++)
                if (current.components[index] is T) result.Add((T)(object)current.components[index]);
            for (var index = 0; index < current.transform.ChildTransforms.Count; index++)
                CollectComponentsInChildren(current.transform.ChildTransforms[index].gameObject, includeInactive, result);
            foreach (var child in current.transform.children)
            {
                if (child is T && !result.Contains((T)(object)child)) result.Add((T)(object)child);
            }
        }

        public void SetActive(bool value)
        {
            if (ThrowOnSetActive) throw new InvalidOperationException("SetActive is forbidden by the lifecycle test sentinel.");
            SetActiveCallCount++;
            activeSelf = value;
        }
    }
    public class Camera : MonoBehaviour
    {
        public static Camera main;
        public bool orthographic = true;
        public float orthographicSize = 7.2f;
        public float aspect = 16f / 9f;
        public float nearClipPlane = 0.3f;
        public Vector3 ViewportToWorldPoint(Vector3 point)
        {
            return new Vector3(transform.position.x + (point.x - 0.5f) * orthographicSize * 2f * aspect,
                transform.position.y + (point.y - 0.5f) * orthographicSize * 2f, transform.position.z + point.z);
        }
        public Vector3 ScreenToWorldPoint(Vector3 point) { return point; }
    }
    public struct Quaternion { public static Quaternion identity { get { return new Quaternion(); } } }
    public class Transform : Component
    {
        private Transform parentValue;
        private readonly List<Transform> childTransforms = new List<Transform>();
        public Vector3 position;
        public Vector3 localPosition;
        public Vector3 localScale;
        public Vector3 eulerAngles;
        public Vector3 localEulerAngles;
        public Transform parent
        {
            get { return parentValue; }
            set { SetParent(value, true); }
        }
        public Quaternion rotation;
        public Vector3 lossyScale { get { return localScale; } }
        public Object[] children = new Object[0];
        internal IList<Transform> ChildTransforms { get { return childTransforms; } }
        public bool IsChildOf(Transform other)
        {
            for (var node = this; node != null; node = node.parent) if (node == other) return true;
            return false;
        }

        public void SetParent(Transform value, bool worldPositionStays)
        {
            if (parentValue == value) return;
            if (parentValue != null) parentValue.childTransforms.Remove(this);
            parentValue = value;
            if (parentValue != null && !parentValue.childTransforms.Contains(this)) parentValue.childTransforms.Add(this);
        }

        public new T[] GetComponentsInChildren<T>() { return gameObject.GetComponentsInChildren<T>(false); }
        public new T[] GetComponentsInChildren<T>(bool includeInactive)
        {
            return gameObject.GetComponentsInChildren<T>(includeInactive);
        }
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
        public static implicit operator Vector2(Vector3 value) { return new Vector2(value.x, value.y); }

        public static Vector3 operator *(Vector3 value, float multiplier)
        {
            return new Vector3(value.x * multiplier, value.y * multiplier, value.z * multiplier);
        }

        public static Vector3 Lerp(Vector3 left, Vector3 right, float progress)
        {
            return LerpUnclamped(left, right, progress);
        }

        public static Vector3 LerpUnclamped(Vector3 left, Vector3 right, float progress)
        {
            return new Vector3
            {
                x = left.x + ((right.x - left.x) * progress),
                y = left.y + ((right.y - left.y) * progress),
                z = left.z + ((right.z - left.z) * progress)
            };
        }

        public static float Distance(Vector3 left, Vector3 right)
        {
            var x = left.x - right.x;
            var y = left.y - right.y;
            var z = left.z - right.z;
            return (float)Math.Sqrt((x * x) + (y * y) + (z * z));
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
        public static implicit operator Vector3(Vector2 value) { return new Vector3(value.x, value.y, 0f); }
        public static float Distance(Vector2 a, Vector2 b) { return (float)Math.Sqrt((a.x-b.x)*(a.x-b.x)+(a.y-b.y)*(a.y-b.y)); }
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
        public static Color white { get { return new Color(1f, 1f, 1f, 1f); } }
        public static Color gray { get { return new Color(0.5f, 0.5f, 0.5f, 1f); } }
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
            return new Color(value.r / 255f, value.g / 255f, value.b / 255f, value.a / 255f);
        }
    }
    public struct Keyframe
    {
        public float time;
        public float value;

        public Keyframe(float time, float value)
        {
            this.time = time;
            this.value = value;
        }
    }
    public class AnimationCurve
    {
        private readonly Keyframe[] curveKeys;

        public AnimationCurve(params Keyframe[] keys)
        {
            curveKeys = keys == null ? new Keyframe[0] : keys;
        }

        public int length { get { return curveKeys.Length; } }

        public static AnimationCurve EaseInOut(float timeStart, float valueStart, float timeEnd, float valueEnd)
        {
            return Linear(timeStart, valueStart, timeEnd, valueEnd);
        }

        public static AnimationCurve Linear(float timeStart, float valueStart, float timeEnd, float valueEnd)
        {
            return new AnimationCurve(
                new Keyframe(timeStart, valueStart),
                new Keyframe(timeEnd, valueEnd));
        }

        public float Evaluate(float time)
        {
            if (curveKeys.Length == 0) return 0f;
            if (curveKeys.Length == 1 || time <= curveKeys[0].time) return curveKeys[0].value;
            for (var index = 1; index < curveKeys.Length; index++)
            {
                var right = curveKeys[index];
                if (time > right.time) continue;
                var left = curveKeys[index - 1];
                var duration = right.time - left.time;
                if (duration <= 0f) return right.value;
                var progress = (time - left.time) / duration;
                return left.value + ((right.value - left.value) * progress);
            }
            return curveKeys[curveKeys.Length - 1].value;
        }
    }
    public static class Time
    {
        public static float unscaledDeltaTime = 0.016f;
        public static float unscaledTime;
        public static float deltaTime = 0.016f;
        public static float time;
        public static float timeScale = 1f;
        public static int frameCount;
    }
    public static class Mathf
    {
        public static float Max(float a, float b) { return Math.Max(a,b); }
        public static float Abs(float value) { return Math.Abs(value); }
        public static float Clamp01(float value) { return Math.Max(0f, Math.Min(1f, value)); }
        public static float Clamp(float value, float minimum, float maximum) { return Math.Max(minimum, Math.Min(maximum, value)); }
        public static float Lerp(float from, float to, float value) { return from + ((to - from) * Clamp01(value)); }
        public static float SmoothStep(float from, float to, float value)
        {
            var clamped = Clamp01(value);
            clamped = clamped * clamped * (3f - (2f * clamped));
            return from + ((to - from) * clamped);
        }
        public static float LerpUnclamped(float from, float to, float value) { return from + ((to - from) * value); }
        public static bool Approximately(float left, float right) { return Math.Abs(left - right) <= 0.00001f; }
        public static float DeltaAngle(float current, float target)
        {
            var delta = (target - current) % 360f;
            if (delta > 180f) delta -= 360f;
            if (delta < -180f) delta += 360f;
            return delta;
        }
    }
    public static class Application
    {
        public static bool runInBackground;
        public static bool isFocused = true;
        public static bool isPlaying = true;
        public static string dataPath = "Assets";
    }
    public static class Resources
    {
        private static readonly Dictionary<string, Object> Values = new Dictionary<string, Object>();
        public static T[] FindObjectsOfTypeAll<T>() { return new T[0]; }
        public static T Load<T>(string path) where T : Object
        {
            Object value;
            return Values.TryGetValue(path, out value) ? value as T : null;
        }
        public static void Register(string path, Object value) { Values[path] = value; }
        public static void Clear() { Values.Clear(); }
    }
    public sealed class WaitForSeconds
    {
        public WaitForSeconds(float seconds) { }
    }
    public static class Random
    {
        public static float Range(float minimum, float maximum) { return minimum; }
    }
    public static class Debug
    {
        public static readonly List<string> Messages = new List<string>();
        public static void Log(string message) { Messages.Add(message); }
        public static void Log(string message, Object context) { Messages.Add(message); }
        public static void LogWarning(string message) { }
        public static void LogWarning(string message, Object context) { }
        public static void LogError(string message) { }
        public static void LogError(string message, Object context) { }
    }

    [AttributeUsage(AttributeTargets.Field)] public sealed class SerializeField : Attribute { }
    [AttributeUsage(AttributeTargets.Field)] public sealed class HideInInspector : Attribute { }
    [AttributeUsage(AttributeTargets.Field)] public sealed class ColorUsageAttribute : Attribute
    {
        public ColorUsageAttribute(bool showAlpha, bool hdr) { }
    }
    [AttributeUsage(AttributeTargets.Field)] public sealed class HeaderAttribute : Attribute
    {
        public HeaderAttribute(string value) { }
    }
    [AttributeUsage(AttributeTargets.Field)] public sealed class TooltipAttribute : Attribute
    {
        public TooltipAttribute(string value) { }
    }
    [AttributeUsage(AttributeTargets.Field)] public sealed class RangeAttribute : Attribute
    {
        public RangeAttribute(float minimum, float maximum) { }
    }
    [AttributeUsage(AttributeTargets.Field)] public sealed class MinAttribute : Attribute
    {
        public MinAttribute(float minimum) { }
    }
    [AttributeUsage(AttributeTargets.Class)] public sealed class DisallowMultipleComponent : Attribute { }
    [AttributeUsage(AttributeTargets.Class)] public sealed class ExecuteAlways : Attribute { }
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)] public sealed class RequireComponent : Attribute
    {
        public RequireComponent(Type type) { }
    }
    [AttributeUsage(AttributeTargets.Class)] public sealed class AddComponentMenuAttribute : Attribute
    {
        public AddComponentMenuAttribute(string value) { }
    }
    [AttributeUsage(AttributeTargets.Method)] public sealed class ContextMenu : Attribute
    {
        public ContextMenu(string value) { }
    }
    [AttributeUsage(AttributeTargets.Class)] public sealed class CreateAssetMenuAttribute : Attribute
    {
        public string fileName;
        public string menuName;
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
        public OutlineMergeOverride MergeOutline { get; set; }
        public OutlineMergeOverride MergeShadow { get; set; }
        public void SetFormalSourceActive(bool active) { }
        public bool ConfigureFormal(UnityEngine.SpriteRenderer renderer, OutlineMergeOverride outlineMode,
            OutlineMergeOverride shadowMode, bool followGroupOutlineParameters,
            UnityEngine.Color outlineColor, float outlineThickness, UnityEngine.Color shadowColor,
            UnityEngine.Vector2 shadowOffset, float shadowOpacity, float shadowSoftness)
        {
            MergeOutline = outlineMode;
            MergeShadow = shadowMode;
            return false;
        }
    }

    public sealed class SpriteOutlineMergeRenderer2D : UnityEngine.MonoBehaviour
    {
        public string LastError { get; set; }
        public void Invalidate(string reason) { }
        public string ExportBudgetState() { return "stub"; }
        public bool IsReadyFor(SpriteOutlineMergeMember2D member) { return member != null; }
        public void SetFormalGroupActive(bool active) { }
        public bool ConfigureFormalGroup(bool outlineMergeEnabled, UnityEngine.Color outlineColor,
            float outlineThickness, bool shadowMergeEnabled, bool shadowEffectEnabled,
            UnityEngine.Color shadowColor, UnityEngine.Vector2 shadowOffset, float shadowOpacity,
            float shadowSoftness) { return false; }
        public bool ReplaceMembersIfChanged(IList<SpriteOutlineMergeMember2D> members) { return false; }
    }
}

namespace UnityEngine.Events
{
    public class UnityEvent<T>
    {
        private event Action<T> listeners;
        public int ListenerCount { get { return listeners == null ? 0 : listeners.GetInvocationList().Length; } }
        public void AddListener(Action<T> listener) { listeners += listener; }
        public void RemoveListener(Action<T> listener) { listeners -= listener; }
        public void Invoke(T value) { var callback = listeners; if (callback != null) callback(value); }
        public int GetPersistentEventCount() { return 0; }
    }
}

namespace UnityEngine.SceneManagement
{
    public static class SceneManager { public static Scene GetActiveScene() { return new Scene(); } }
    public struct Scene
    {
        public string path;
        public string name;
        public static bool operator ==(Scene left, Scene right) { return true; }
        public static bool operator !=(Scene left, Scene right) { return false; }
        public override bool Equals(object value) { return value is Scene; }
        public override int GetHashCode() { return 0; }
    }
}

namespace UnityEngine.InputSystem
{
    public sealed class Mouse
    {
        public static Mouse current;
        public readonly Controls.Vector2Control scroll = new Controls.Vector2Control();
        public readonly Controls.Vector2Control position = new Controls.Vector2Control();
    }

    public enum Key
    {
        Digit1,
        Digit2,
        Digit3,
        Digit4,
        Digit5,
        Digit6,
        Digit7,
        Digit8,
        F,
        C,
        R,
        P,
        T,
        H,
        Escape
    }
}

namespace UnityEngine.InputSystem.Controls
{
    public sealed class Vector2Control
    {
        public UnityEngine.Vector2 Value;
        public UnityEngine.Vector2 ReadValue() { return Value; }
    }
}

namespace UnityEngine.Rendering
{
    public class SortingGroup : UnityEngine.Behaviour { }
}

namespace TMPro
{
    public enum TextAlignmentOptions { Center, TopLeft }
    public sealed class TextRect { public UnityEngine.Vector2 pivot; }
    public class TMP_Text : UnityEngine.Behaviour
    {
        public string text;
        public float fontSize = 3f;
        public TextRect rectTransform = new TextRect();
        public TextAlignmentOptions alignment;
    }

    public sealed class TextMeshPro : TMP_Text { }
}

namespace UnityEditor
{
    [Flags]
    public enum GizmoType
    {
        Pickable = 1,
        NotInSelectionHierarchy = 2,
        Selected = 4,
        Active = 8,
        InSelectionHierarchy = 16,
        NonSelected = 32
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class DrawGizmo : Attribute
    {
        public DrawGizmo(GizmoType gizmoType) { }
        public DrawGizmo(GizmoType gizmoType, Type drawnType) { }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple=true)] public sealed class MenuItem : Attribute
    { public MenuItem(string name, bool validate=false) { } }
    public static class EditorPrefs
    {
        private static readonly Dictionary<string,bool> values = new Dictionary<string,bool>();
        public static bool GetBool(string key, bool fallback) { bool value; return values.TryGetValue(key,out value) ? value : fallback; }
        public static void SetBool(string key, bool value) { values[key]=value; }
    }
    public static class Menu { public static void SetChecked(string name, bool value) { } }
    public static class Selection { public static UnityEngine.GameObject activeGameObject; }
    public class SceneView
    {
        public static SceneView lastActiveSceneView;
        public static void RepaintAll() { }
        public void Repaint() { }
        public void LookAt(UnityEngine.Vector3 position, UnityEngine.Quaternion rotation, float size, bool ortho, bool instant) { }
    }
    public static class Handles
    {
        public static UnityEngine.Color color;
        public static void DrawAAPolyLine(float width, params UnityEngine.Vector3[] points) { }
        public static void DrawLine(UnityEngine.Vector3 a, UnityEngine.Vector3 b) { }
        public static void Label(UnityEngine.Vector3 at, string text) { }
    }
}

namespace UnityEngine.UI
{
    public class Image : UnityEngine.Behaviour
    {
        public UnityEngine.Sprite sprite;
    }
}

// MouseInteract event payload stubs used only by the editor-external compile.
public class MouseDraggableObject : UnityEngine.MonoBehaviour { }
public class MouseScrollableObject : UnityEngine.MonoBehaviour
{
    public UnityEngine.Events.UnityEvent<float> scrollStepEvent;
    public bool AllowScrollThroughPressables { get; set; }
}
public class Collider2D : UnityEngine.Behaviour { }
public class HiddenButtonIdentifier : UnityEngine.MonoBehaviour { }
public abstract class MousePressableObject : UnityEngine.MonoBehaviour
{
    public bool hasVisual { get; protected set; }
}
public struct LayerMask
{
    public int value;
    public static implicit operator int(LayerMask mask) { return mask.value; }
    public static implicit operator LayerMask(int value) { return new LayerMask { value = value }; }
}
public static class Physics2D
{
    public static Collider2D[] OverlapPointAll(UnityEngine.Vector2 point, LayerMask mask)
    {
        return new Collider2D[0];
    }
}
public static class SortingLayer
{
    public static int GetLayerValueFromID(int id) { return id; }
}
public class MouseManager : UnityEngine.MonoBehaviour
{
    public static event Action<float> OnScrollStep;
    public Stack<MouseInteractionLayer> mouseInteractiveLayers = new Stack<MouseInteractionLayer>();
    public MouseScrollableObject currentScrollableObject;
    public bool dragStarted;
    public bool dragPerforming;
    public MousePressableObject currentPressableHoverTarget;
    public MouseInteractionLayer currentMouseLayer { get { return mouseInteractiveLayers.Count == 0 ? null : mouseInteractiveLayers.Peek(); } }
    public float ScrollThreshold { get; set; }
    public float AccumulatedScroll { get; set; }
    public float LastScrollTime { get; set; }
    public float ScrollCooldown { get; set; }
    public bool HasUnsafeEmptyLayerUpdatePath { get; set; }
    public MouseScrollableObject LastScrollableCandidate { get; set; }
    public MousePressableObject LastScrollableBlocker { get; set; }
    public string LastScrollResolutionReason { get; set; }
    public int LastScrollResolutionFrame { get; set; }
    public int LastScrollDispatchFrame { get; set; }
    public int ScrollDispatchSequence { get; set; }
    public float LastRawScrollY { get; set; }
    public float LastDispatchedStep { get; set; }
    public UnityEngine.Vector2 LastScrollPointerWorld { get; set; }
    public static void RaiseScrollStep(float delta) { var callback = OnScrollStep; if (callback != null) callback(delta); }
}
public class DropZone : UnityEngine.MonoBehaviour { }
public struct DragLimitBoundsStub
{
    public UnityEngine.Vector2 center;
    public float halfW;
    public float halfH;
}
public class DragLimit : UnityEngine.MonoBehaviour
{
    public UnityEngine.Vector2 BoundsCenter;
    public float BoundsHalfWidth;
    public float BoundsHalfHeight;
    public DragLimitBoundsStub GetBounds()
    {
        return new DragLimitBoundsStub
        {
            center = BoundsCenter,
            halfW = BoundsHalfWidth,
            halfH = BoundsHalfHeight
        };
    }
}
public class ScrollBar_Controller : UnityEngine.MonoBehaviour
{
    public bool enableScroll;
    public float Value { get; private set; }
    public void SetValue(float value, bool notify) { Value = value; }
}
public class DragContainer : UnityEngine.MonoBehaviour
{
    public enum DragMode { Vertical, Horizontal, Composite }
    public enum LimitMode { Manual, DragLimit }
    public enum SizeSource { BoxCollider2D, RectTransform }

    public DragMode mode;
    public SizeSource sizeSource;
    public LimitMode limitMode;
    public DragLimit dragLimit;
    public UnityEngine.BoxCollider2D col;
    public float LimitMaxX;
    public float LimitMinX;
    public float LimitMaxY;
    public float LimitMinY;
    public bool ApplyUpdatedBoundsOnUpdate;
    public float UpdatedLimitMaxX;
    public float UpdatedLimitMinX;
    public float UpdatedLimitMaxY;
    public float UpdatedLimitMinY;
    public int InitializeCount { get; private set; }
    public int UpdateBoundsCount { get; private set; }
    public int MoveToTargetCount { get; private set; }

    public void onInitialize() { InitializeCount++; }
    public void UpdateBounds()
    {
        UpdateBoundsCount++;
        if (!ApplyUpdatedBoundsOnUpdate) return;
        LimitMaxX = UpdatedLimitMaxX;
        LimitMinX = UpdatedLimitMinX;
        LimitMaxY = UpdatedLimitMaxY;
        LimitMinY = UpdatedLimitMinY;
    }
    public void moveToTargetPos(UnityEngine.Vector3 targetPosition)
    {
        MoveToTargetCount++;
        var current = transform.position;
        switch (mode)
        {
            case DragMode.Vertical:
                transform.position = new UnityEngine.Vector3(
                    current.x,
                    UnityEngine.Mathf.Clamp(targetPosition.y, LimitMinY, LimitMaxY),
                    current.z);
                break;
            case DragMode.Horizontal:
                transform.position = new UnityEngine.Vector3(
                    UnityEngine.Mathf.Clamp(targetPosition.x, LimitMinX, LimitMaxX),
                    current.y,
                    current.z);
                break;
            default:
                transform.position = new UnityEngine.Vector3(
                    UnityEngine.Mathf.Clamp(targetPosition.x, LimitMinX, LimitMaxX),
                    UnityEngine.Mathf.Clamp(targetPosition.y, LimitMinY, LimitMaxY),
                    targetPosition.z);
                break;
        }
    }
}
public class MouseInteractionLayer : UnityEngine.MonoBehaviour
{
    public bool manualStackLayer;
    public List<LayerMask> alloweInteractionLayers = new List<LayerMask>();
    public int PushCount { get; private set; }
    public int RemoveCount { get; private set; }
    public void OnPushLayer() { PushCount++; }
    public void OnRemoveLayer() { RemoveCount++; }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class CommandAttribute : Attribute
{
    public string Name { get; private set; }
    public string Group { get; set; }
    public CommandAttribute(string name) { Name = name; Group = "Default"; }
}

public sealed class CommandContext
{
    private readonly Dictionary<string, object> values = new Dictionary<string, object>();
    private object result;
    public void SetArg<T>(string key, T value) { values[key] = value; }
    public T GetArg<T>(string key, T defaultValue = default(T))
    {
        object value;
        return values.TryGetValue(key, out value) && value is T ? (T)value : defaultValue;
    }
    public void SetResult<T>(T value) { result = value; }
    public T GetResult<T>() { return result is T ? (T)result : default(T); }
}

public class InputManager : UnityEngine.MonoBehaviour
{
    public static Func<UnityEngine.InputSystem.Key, bool> OnKeyPressed;
    public static Func<UnityEngine.InputSystem.Key, bool> OnKeyReleased;
}

public static class Tools
{
    public static UnityEngine.Sprite[] LoadSpritesFromFolder(string path) { return new UnityEngine.Sprite[0]; }
}
