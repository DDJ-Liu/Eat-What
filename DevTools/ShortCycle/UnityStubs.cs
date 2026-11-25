// Editor-external compile stubs for CK01-C. This file is not under Assets and
// is only consumed by Test-CK01CShortCycle.ps1.
using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine
{
    public enum RuntimeInitializeLoadType { SubsystemRegistration, BeforeSceneLoad }
    [AttributeUsage(AttributeTargets.Method)] public sealed class RuntimeInitializeOnLoadMethodAttribute : Attribute
    { public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType type) { } }
    public class Object
    {
        private static int nextInstanceId;
        private readonly int instanceId=System.Threading.Interlocked.Increment(ref nextInstanceId);
        public int GetInstanceID(){return instanceId;}
        public string name;
        public bool ValidationDestroyed {get;internal set;}
        public static void DontDestroyOnLoad(Object value) { }
        public static void Destroy(Object value){DestroyImmediate(value);}
        public static void DestroyImmediate(Object value)
        {
            var go=value as GameObject;if(go!=null){go.DestroyForValidation();return;}
            if(value!=null){value.ValidationDestroyed=true;Resources.UnregisterObject(value);}
        }

        public static T Instantiate<T>(T original) where T : Object, new()
        {
            return Instantiate(original, null);
        }

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
        public static ScriptableObject CreateInstance(Type type){return (ScriptableObject)Activator.CreateInstance(type);}
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
        public bool TryGetComponent<T>(out T value) where T : class { value = GetComponent<T>(); return value != null; }
        public T[] GetComponentsInChildren<T>() { return gameObject.GetComponentsInChildren<T>(false); }
        public T[] GetComponentsInChildren<T>(bool includeInactive) { return gameObject.GetComponentsInChildren<T>(includeInactive); }
    }
    public class Behaviour : Component
    {
        public bool enabled = true;
        public bool isActiveAndEnabled { get { return enabled && gameObject.activeInHierarchy; } }
    }
    public class MonoBehaviour : Behaviour
    {
        private readonly List<Coroutine> activeCoroutines = new List<Coroutine>();

        public void StopAllCoroutines(){StoppedCoroutineCount+=activeCoroutines.Count;activeCoroutines.Clear();}
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
    public class AsyncOperation
    {
        public bool isDone { get; private set; }
        public event Action<AsyncOperation> completed;
        public void Complete() { isDone = true; var callback = completed; if (callback != null) callback(this); }
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
    public class Sprite : Object
    {
        public Rect rect;
        public float pixelsPerUnit;
        public readonly List<Vector2[]> PhysicsShapes = new List<Vector2[]>();
        public int GetPhysicsShapeCount(){return PhysicsShapes.Count;}
        public int GetPhysicsShape(int index,List<Vector2> points){points.Clear();points.AddRange(PhysicsShapes[index]);return points.Count;}
    }
    public class AnimationClip : Object { }
    public class RuntimeAnimatorController : Object
    {
        public AnimationClip[] animationClips = new AnimationClip[0];
    }
    public class AnimatorControllerParameter
    {
        public string name;
    }
    public class Animator : Behaviour
    {
        public AnimatorControllerParameter[] parameters = new AnimatorControllerParameter[0];
        public RuntimeAnimatorController runtimeAnimatorController;
        public int layerCount;
        public bool HasState(int layerIndex, int stateHash) { return false; }
        public static int StringToHash(string value) { return value == null ? 0 : value.GetHashCode(); }
    }
    public class Canvas : Behaviour { }
    public struct Rect
    {
        public float x;
        public float y;
        public float width;
        public float height;
        public Rect(float x, float y, float width, float height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }
    }
    public static class Screen
    {
        public static int width = 1920;
        public static int height = 1080;
    }
    public static class GUI
    {
        public static int depth;
        public static bool NextButtonResult;
        public static readonly List<string> DrawnLabels = new List<string>();
        public static bool Button(Rect position, string text)
        {
            DrawnLabels.Add(text);
            var result = NextButtonResult;
            NextButtonResult = false;
            return result;
        }
        public static void Box(Rect position, string text) { DrawnLabels.Add(text); }
    }
    public class Material : Object { }
    public enum SpriteDrawMode { Simple, Sliced, Tiled }
    public enum SpriteTileMode { Continuous, Adaptive }
    public enum SpriteSortPoint { Center, Pivot }
    public enum SpriteMaskInteraction { None, VisibleInsideMask, VisibleOutsideMask }
    public class BoxCollider2D : global::Collider2D
    {
        public Vector2 size;
        public Vector2 offset;
        public override bool OverlapPoint(Vector2 point)
        {
            if (ContainsPoint != null) return base.OverlapPoint(point);
            var local=transform.InverseTransformPoint(point);
            return Math.Abs(local.x-offset.x)<=size.x*.5f && Math.Abs(local.y-offset.y)<=size.y*.5f;
        }
    }
    public class PolygonCollider2D : global::Collider2D
    {
        private readonly List<Vector2[]> paths=new List<Vector2[]>();
        public int pathCount {get{return paths.Count;}set{while(paths.Count<value)paths.Add(new Vector2[0]);while(paths.Count>value)paths.RemoveAt(paths.Count-1);}}
        public void SetPath(int index,Vector2[] points){paths[index]=points;}
        public int GetPath(int index,List<Vector2> points){points.Clear();points.AddRange(paths[index]);return points.Count;}
        public override bool OverlapPoint(Vector2 point)
        {
            var p=transform.InverseTransformPoint(point);
            foreach(var path in paths)
            {
                var inside=false;
                for(int i=0,j=path.Length-1;i<path.Length;j=i++)
                    if((path[i].y>p.y)!=(path[j].y>p.y) && p.x<(path[j].x-path[i].x)*(p.y-path[i].y)/(path[j].y-path[i].y)+path[i].x)inside=!inside;
                if(inside)return true;
            }
            return false;
        }
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
    public class Renderer : Behaviour
    {
        public Bounds bounds;
        public int sortingLayerID;
        public int sortingOrder;
    }
    public class SpriteRenderer : Renderer
    {
        public bool flipX, flipY;
        public SpriteDrawMode drawMode;
        public SpriteTileMode tileMode;
        public SpriteSortPoint spriteSortPoint;
        public SpriteMaskInteraction maskInteraction;
        public Vector2 size;
        public float adaptiveModeThreshold;
        public Sprite sprite;
        public Material material;
        public Material sharedMaterial;
        public Color color;
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

        public GameObject(string name) : this() { this.name=name; }
        internal void DestroyForValidation()
        {
            if(ValidationDestroyed)return;ValidationDestroyed=true;
            foreach(var component in components.ToArray())
            {
                component.ValidationDestroyed=true;
                var method=component.GetType().GetMethod("OnDestroy",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
                if(method!=null)method.Invoke(component,null);
                Resources.UnregisterObject(component);
            }
            components.Clear();
        }
        public GameObject()
        {
            transform = new Transform();
            transform.localScale = Vector3.one;
            AttachExistingComponent(transform);
        }

        internal void AttachExistingComponent(Component component)
        {
            if (component == null || components.Contains(component)) return;
            component.Attach(this);
            components.Add(component);
            Resources.RegisterObject(component);
        }

        public T AddComponent<T>() where T : Component, new()
        {
            var component = new T();
            AttachExistingComponent(component);
#if EATWHAT_TRAY_POINTER_PLATFORM
            var polygon=component as PolygonCollider2D;
            if(polygon!=null)global::Physics2D.Colliders.Add(polygon);
#endif
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
            var changed = activeSelf != value;
            activeSelf = value;
#if EATWHAT_DRAG_PREVIEW_PLATFORM
            if(changed) foreach(var component in components.ToArray())
            {
                var behaviour=component as Behaviour;
                if(behaviour==null || !behaviour.enabled)continue;
                var method=component.GetType().GetMethod(value?"OnEnable":"OnDisable",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
                if(method!=null)method.Invoke(component,null);
            }
#endif
        }
    }
    public class Camera : MonoBehaviour
    {
        public static Camera main;
        public int ScreenToWorldPointCallCount;
        public bool orthographic = true;
        public float orthographicSize = 7.2f;
        public float aspect = 16f / 9f;
        public float nearClipPlane = 0.3f;
        public Vector3 ViewportToWorldPoint(Vector3 point)
        {
            return new Vector3(transform.position.x + (point.x - 0.5f) * orthographicSize * 2f * aspect,
                transform.position.y + (point.y - 0.5f) * orthographicSize * 2f, transform.position.z + point.z);
        }
        public Vector3 ScreenToWorldPoint(Vector3 point)
        {
            ScreenToWorldPointCallCount++;
            return point;
        }
    }
    public struct Quaternion
    {
        public float x,y,z,w;
        public static Quaternion identity { get { return new Quaternion {w=1}; } }
        public static Quaternion Inverse(Quaternion q) { return new Quaternion {x=-q.x,y=-q.y,z=-q.z,w=q.w}; }
        public static Quaternion Euler(float x,float y,float z) { var a=z*Math.PI/360;return new Quaternion {z=(float)Math.Sin(a),w=(float)Math.Cos(a)}; }
        public static Quaternion operator *(Quaternion a,Quaternion b) { return new Quaternion {x=a.w*b.x+a.x*b.w+a.y*b.z-a.z*b.y,y=a.w*b.y-a.x*b.z+a.y*b.w+a.z*b.x,z=a.w*b.z+a.x*b.y-a.y*b.x+a.z*b.w,w=a.w*b.w-a.x*b.x-a.y*b.y-a.z*b.z}; }
        public static Vector3 operator *(Quaternion q,Vector3 v) { var p=q*new Quaternion{x=v.x,y=v.y,z=v.z}*Inverse(q);return new Vector3(p.x,p.y,p.z); }
    }
    public class Transform : Component
    {
        private Transform parentValue;
        private readonly List<Transform> childTransforms = new List<Transform>();
#if EATWHAT_DRAG_PREVIEW_PLATFORM
        private Vector3 localPositionValue;
        public Vector3 position { get {return parent==null?localPositionValue:parent.TransformPoint(localPositionValue);} set{localPositionValue=parent==null?value:parent.InverseTransformPoint(value);} }
        public Vector3 localPosition {get{return localPositionValue;}set{localPositionValue=value;}}
#else
        public Vector3 position;
        public Vector3 localPosition;
#endif
        public Vector3 localScale;
        public Vector3 eulerAngles;
        public Vector3 localEulerAngles;
        public Transform parent
        {
            get { return parentValue; }
            set { SetParent(value, true); }
        }
        public Quaternion localRotation=Quaternion.identity;
        public Quaternion rotation {get{return parent==null?localRotation:parent.rotation*localRotation;}set{localRotation=parent==null?value:Quaternion.Inverse(parent.rotation)*value;}}
        public Vector3 lossyScale { get {
#if EATWHAT_DRAG_PREVIEW_PLATFORM
            return parent==null?localScale:Vector3.Scale(parent.lossyScale,localScale);
#else
            return localScale;
#endif
        } }
        public Vector3 TransformPoint(Vector3 p){return position+TransformVector(p);}
        public Vector3 InverseTransformPoint(Vector3 p){var a=Quaternion.Inverse(rotation)*(p-position);var s=lossyScale;return new Vector3(a.x/s.x,a.y/s.y,a.z/s.z);}
        public Vector3 TransformVector(Vector3 p){return rotation*Vector3.Scale(p,lossyScale);}
        public Vector3 InverseTransformDirection(Vector3 p){return Quaternion.Inverse(rotation)*p;}
        public Vector3 TransformDirection(Vector3 p){return rotation*p;}
        public Vector3 right {get{return rotation*Vector3.right;}}
        public Vector3 up {get{return rotation*Vector3.up;}}
        public Object[] children = new Object[0];
        internal IList<Transform> ChildTransforms { get { return childTransforms; } }
        public bool IsChildOf(Transform other)
        {
            for (var node = this; node != null; node = node.parent) if (node == other) return true;
            return false;
        }

        public void SetParent(Transform value, bool worldPositionStays)
        {
            var world=position;
            if (parentValue == value) return;
            if (parentValue != null) parentValue.childTransforms.Remove(this);
            parentValue = value;
            if (parentValue != null && !parentValue.childTransforms.Contains(this)) parentValue.childTransforms.Add(this);
#if EATWHAT_DRAG_PREVIEW_PLATFORM
            if(worldPositionStays)position=world;
#endif
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
        public static Vector3 up {get{return new Vector3(0,1,0);}}
        public static Vector3 right {get{return new Vector3(1,0,0);}}
        public float magnitude {get{return Distance(this,zero);}}
        public float sqrMagnitude {get{return x*x+y*y+z*z;}}
        public Vector3 normalized {get{return magnitude==0?zero:this/magnitude;}}
        public static Vector3 Scale(Vector3 a,Vector3 b){return new Vector3(a.x*b.x,a.y*b.y,a.z*b.z);}
        public static Vector3 ClampMagnitude(Vector3 a,float b){return a.magnitude>b?a.normalized*b:a;}
        public static Vector3 operator +(Vector3 a,Vector3 b){return new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);}
        public static Vector3 operator -(Vector3 a,Vector3 b){return new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);}
        public static Vector3 operator /(Vector3 a,float b){return new Vector3(a.x/b,a.y/b,a.z/b);}
        public static bool operator ==(Vector3 a,Vector3 b){return Distance(a,b)<.00001f;}
        public static bool operator !=(Vector3 a,Vector3 b){return !(a==b);}
        public override bool Equals(object o){return o is Vector3 && this==(Vector3)o;}
        public override int GetHashCode(){return x.GetHashCode()^y.GetHashCode()^z.GetHashCode();}
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
    public struct Bounds
    {
        public Vector3 center;
        public Vector3 size;

        public Bounds(Vector3 center, Vector3 size)
        {
            this.center = center;
            this.size = size;
        }

        public Vector3 min
        {
            get { return new Vector3(center.x - size.x * 0.5f, center.y - size.y * 0.5f, center.z - size.z * 0.5f); }
        }

        public Vector3 max
        {
            get { return new Vector3(center.x + size.x * 0.5f, center.y + size.y * 0.5f, center.z + size.z * 0.5f); }
        }

        public void Encapsulate(Bounds other)
        {
            var left = min;
            var right = max;
            var otherLeft = other.min;
            var otherRight = other.max;
            var nextMin = new Vector3(Math.Min(left.x, otherLeft.x), Math.Min(left.y, otherLeft.y), Math.Min(left.z, otherLeft.z));
            var nextMax = new Vector3(Math.Max(right.x, otherRight.x), Math.Max(right.y, otherRight.y), Math.Max(right.z, otherRight.z));
            center = new Vector3((nextMin.x + nextMax.x) * 0.5f, (nextMin.y + nextMax.y) * 0.5f, (nextMin.z + nextMax.z) * 0.5f);
            size = new Vector3(nextMax.x - nextMin.x, nextMax.y - nextMin.y, nextMax.z - nextMin.z);
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
        public static Vector2 up {get{return new Vector2(0,1);}}
        public static Vector2 down {get{return new Vector2(0,-1);}}
        public static Vector2 left {get{return new Vector2(-1,0);}}
        public static Vector2 right {get{return new Vector2(1,0);}}
        public static Vector2 operator +(Vector2 a,Vector2 b){return new Vector2(a.x+b.x,a.y+b.y);}
        public static Vector2 operator -(Vector2 a,Vector2 b){return new Vector2(a.x-b.x,a.y-b.y);}
        public static Vector2 operator *(Vector2 a,float b){return new Vector2(a.x*b,a.y*b);}
        public static bool operator ==(Vector2 a,Vector2 b){return Distance(a,b)<.00001f;}
        public static bool operator !=(Vector2 a,Vector2 b){return !(a==b);}
        public override bool Equals(object o){return o is Vector2 && this==(Vector2)o;}
        public override int GetHashCode(){return x.GetHashCode()^y.GetHashCode();}
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
        public static float Min(float a,float b){return Math.Min(a,b);}
        public static int Min(int a,int b){return Math.Min(a,b);}
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
        public static bool isEditor; public static int QuitCalls; public static int LastQuitCode; public static void Quit(int code){QuitCalls++;LastQuitCode=code;}
        public static string dataPath = "Assets";
        public static string streamingAssetsPath = "StreamingAssets";
        public static int PersistentPathReads;
        public static string persistentDataPath { get { PersistentPathReads++; throw new InvalidOperationException("Real user save path is forbidden in offline fixtures; inject an isolated store."); } }
    }
    public static class Resources
    {
        private static readonly Dictionary<string, Object> Values = new Dictionary<string, Object>();
        private static readonly List<Object> Objects = new List<Object>();
        internal static void RegisterObject(Object value) { if (!Objects.Contains(value)) Objects.Add(value); }
        internal static void UnregisterObject(Object value){Objects.Remove(value);}
        public static T[] FindObjectsOfTypeAll<T>()
        { var result = new List<T>(); foreach (var value in Objects) if (value is T) result.Add((T)(object)value); return result.ToArray(); }
        public static T Load<T>(string path) where T : Object
        {
            Object value;
            return Values.TryGetValue(path, out value) ? value as T : null;
        }
        public static T[] LoadAll<T>(string path) where T : Object { return new T[0]; }
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
        public static readonly List<string> WarningMessages = new List<string>();
        public static void Log(string message) { Messages.Add(message); }
        public static void Log(string message, Object context) { Messages.Add(message); }
        public static void LogWarning(string message) { WarningMessages.Add(message); }
        public static void LogWarning(string message, Object context) { WarningMessages.Add(message); }
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
    [AttributeUsage(AttributeTargets.Class)] public sealed class DefaultExecutionOrder : Attribute
    {
        public DefaultExecutionOrder(int order) { }
    }
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
    public enum LoadSceneMode { Single, Additive }
    public static class SceneUtility { public static int GetBuildIndexByScenePath(string path) { return -1; } }
    public static class SceneManager
    {
        public static event Action<Scene> sceneUnloaded;
        public static event Action<Scene,LoadSceneMode> sceneLoaded;
        public static Scene GetActiveScene() { foreach(var value in Scenes.Values)return value;return new Scene(); }
        public static int sceneCount {get{return Scenes.Count;}} public static Scene GetSceneAt(int index){return new List<Scene>(Scenes.Values)[index];}
        public static Action<string,LoadSceneMode> ValidationLoad; public static void LoadScene(string path,LoadSceneMode mode){if(ValidationLoad==null)throw new InvalidOperationException("Controlled loader required.");ValidationLoad(path,mode);var callback=sceneLoaded;if(callback!=null)callback(GetSceneByPath(path),mode);}
        public static void ValidationClear(){Scenes.Clear();}
        public static UnityEngine.AsyncOperation LoadSceneAsync(string path, LoadSceneMode mode) { throw new InvalidOperationException("Native loading is forbidden offline; inject the controlled scene loader."); }
        public static UnityEngine.AsyncOperation UnloadSceneAsync(Scene scene) { throw new InvalidOperationException("Native unloading is forbidden offline; inject the controlled scene lease."); }
        private static readonly Dictionary<string, Scene> Scenes = new Dictionary<string, Scene>();
        public static Scene GetSceneByPath(string path) { Scene value; return Scenes.TryGetValue(path, out value) ? value : new Scene(); }
        public static void SetLoaded(Scene scene) { Scenes[scene.path] = scene; }
        public static void RaiseUnloaded(Scene scene) { Scenes.Remove(scene.path); var callback=sceneUnloaded; if(callback!=null)callback(scene); }
    }
    public struct Scene
    {
        public string path;
        public string name;
        public bool isLoaded;
        public bool isDirty; public int handle,ValidationRootCount;
        public bool IsValid() { return !string.IsNullOrEmpty(path); }
        public UnityEngine.GameObject[] GetRootGameObjects() { return new UnityEngine.GameObject[ValidationRootCount]; }
        public static bool operator ==(Scene left, Scene right) { return left.path==right.path && left.name==right.name; }
        public static bool operator !=(Scene left, Scene right) { return !(left==right); }
        public override bool Equals(object value) { return value is Scene && this==(Scene)value; }
        public override int GetHashCode() { return (path??string.Empty).GetHashCode(); }
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
    public class SortingGroup : UnityEngine.Behaviour { public int sortingLayerID; public int sortingOrder; }
}

namespace TMPro
{
    public enum TextAlignmentOptions { Center, TopLeft }
    public sealed class TextRect { public UnityEngine.Vector2 pivot; public UnityEngine.Vector2 sizeDelta; }
    public class TMP_Text : UnityEngine.Behaviour
    {
        public string text;
        public float fontSize = 3f;
        public TextRect rectTransform = new TextRect();
        public TextAlignmentOptions alignment;
        public object font,fontSharedMaterial;
        public int fontStyle,textWrappingMode,overflowMode;
        public UnityEngine.Color color;
        public bool richText,enableAutoSizing;
        public float fontSizeMin,fontSizeMax,characterSpacing,wordSpacing,lineSpacing,paragraphSpacing;
        public UnityEngine.Vector4 margin;
        public UnityEngine.Renderer renderer { get {var r=GetComponent<UnityEngine.Renderer>();return r??gameObject.AddComponent<UnityEngine.Renderer>();} }
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
[Flags]
public enum DropTag
{
    None = 0, Ingredient = 1 << 0, Tool = 1 << 1, Recipe = 1 << 2,
    Trash = 1 << 3, FridgeIngredient = 1 << 4, Everything = ~0
}
#if !EATWHAT_TRAY_POINTER_PLATFORM
public interface IMouseHitDepth
{
    object MouseHitScope {get;}
    string MouseHitKey {get;}
    bool TryGetMouseHitDepth(UnityEngine.Vector2 point,out float depth);
}
#endif
public class MouseDraggableObject : UnityEngine.MonoBehaviour
{
    public event Action<MouseDraggableObject> PreparingDrag { add{} remove{} }
    public bool dragging;
    public void cancelDrag(){dragging=false;}
    public DropTag allowedDropTags = DropTag.Everything;
}
public class MouseScrollableObject : UnityEngine.MonoBehaviour
{
    public UnityEngine.Events.UnityEvent<float> scrollStepEvent;
    public bool AllowScrollThroughPressables { get; set; }
}
public class Collider2D : UnityEngine.Behaviour
{
    public bool isTrigger;
    public Func<UnityEngine.Vector2, bool> ContainsPoint;
    public virtual bool OverlapPoint(UnityEngine.Vector2 point)
    { return ContainsPoint != null && ContainsPoint(point); }
}
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
    public UnityEngine.Vector3 dragStartPos;
    public static MouseManager Instance;
    public static event Action<float> OnScrollStep;
    public static event Action OnMouseCancel;
    public static void TriggerMouseCancel() { if (OnMouseCancel != null) OnMouseCancel(); }
    public MouseDraggableObject currentDraggableObject;
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
    public void ConfigureScrollInput(float threshold, float cooldown)
    {
        ScrollThreshold = threshold;
        ScrollCooldown = cooldown;
        AccumulatedScroll = 0f;
    }
    public static void RaiseScrollStep(float delta) { var callback = OnScrollStep; if (callback != null) callback(delta); }
}
public class DropZone : UnityEngine.MonoBehaviour
{
    public DropTag zoneTags = DropTag.Everything;
    public UnityEngine.Events.UnityEvent<MouseDraggableObject> onDropEvent;
    public UnityEngine.Events.UnityEvent<MouseDraggableObject> onDropRejectedEvent;
    public bool CanAccept(MouseDraggableObject source)
    { return source != null && (source.allowedDropTags & zoneTags) != 0; }
    public void OnDrop(MouseDraggableObject source)
    {
        if (CanAccept(source)) { if (onDropEvent != null) onDropEvent.Invoke(source); }
        else { if (onDropRejectedEvent != null) onDropRejectedEvent.Invoke(source); }
    }
}
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
    public System.Action<DragContainer> VisualDragStart { get; set; }
    public System.Action<DragContainer> VisualDragUpdate { get; set; }
    public System.Action<DragContainer> VisualDragEnd { get; set; }
    public void CancelVisualDrag(){if(VisualDragEnd!=null)VisualDragEnd(this);}
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
    public void OnPushLayer()
    {
        PushCount++;
        var manager = MouseManager.Instance;
        if (manager != null && !manager.mouseInteractiveLayers.Contains(this))
            manager.mouseInteractiveLayers.Push(this);
    }
    public void OnRemoveLayer()
    {
        RemoveCount++;
        var manager = MouseManager.Instance;
        if (manager == null || !manager.mouseInteractiveLayers.Contains(this)) return;
        var topFirst = manager.mouseInteractiveLayers.ToArray();
        manager.mouseInteractiveLayers.Clear();
        for (var i = topFirst.Length - 1; i >= 0; i--)
            if (!ReferenceEquals(topFirst[i], this)) manager.mouseInteractiveLayers.Push(topFirst[i]);
    }
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

#if !EATWHAT_COMPILE_REAL_TOOLS
public static class Tools
{
    public static UnityEngine.Sprite[] LoadSpritesFromFolder(string path) { return new UnityEngine.Sprite[0]; }
    public static bool HasMouseWorldPosition = true;
    public static UnityEngine.Vector3 MouseWorldPosition;
    public static bool TryGetMousePos(out UnityEngine.Vector3 point)
    { point = MouseWorldPosition; return HasMouseWorldPosition; }
}
#endif

namespace UnityEditor
{
    [AttributeUsage(AttributeTargets.Class)] public sealed class InitializeOnLoadAttribute : Attribute { }
    public enum PlayModeStateChange { ExitingEditMode, EnteredPlayMode, ExitingPlayMode, EnteredEditMode }
    public static class EditorApplication
    {
        public delegate void CallbackFunction();
        private static bool playing,pending;
        public static bool isCompiling,isUpdating,ValidationHoldPendingCancel;
        public static bool isPlaying
        {
            get{return playing;}
            set{if(value){if(!playing)pending=true;}else if(playing){pending=true;}else if(!ValidationHoldPendingCancel){pending=false;}}
        }
        public static bool isPlayingOrWillChangePlaymode {get{return playing||pending;}}
        public static bool ValidationPending {get{return pending;}set{pending=value;}}
        public static int EnteredEditEvents;
        public static event Action<PlayModeStateChange> playModeStateChanged;
        public static event CallbackFunction update;
        public static int ValidationUpdateCount {get{return update==null?0:update.GetInvocationList().Length;}}
        public static CallbackFunction ValidationSnapshotUpdate(){return update;}
        public static void ValidationUpdate(){var callback=update;if(callback!=null)callback();}
        public static void ValidationReset(){playing=false;pending=false;ValidationHoldPendingCancel=false;isCompiling=false;isUpdating=false;EnteredEditEvents=0;update=null;}
        public static void ValidationRaise(PlayModeStateChange state)
        {
            if(state==PlayModeStateChange.EnteredPlayMode){playing=true;pending=false;}
            if(state==PlayModeStateChange.ExitingPlayMode){playing=true;pending=true;}
            if(state==PlayModeStateChange.EnteredEditMode){playing=false;pending=false;EnteredEditEvents++;}
            var call=playModeStateChanged;if(call!=null)call(state);
        }
    }
    public static class AssemblyReloadEvents
    {public static event Action beforeAssemblyReload;public static void ValidationRaise(){var call=beforeAssemblyReload;if(call!=null)call();}}
    public static class SessionState
    {
        private static readonly Dictionary<string,string> Text=new Dictionary<string,string>();
        public static string GetString(string key,string fallback){string value;return Text.TryGetValue(key,out value)?value:fallback;}
        public static void SetString(string key,string value){Text[key]=value;}
        public static int GetInt(string key,int fallback){string v;int n;return Text.TryGetValue(key,out v)&&int.TryParse(v,out n)?n:fallback;} public static void SetInt(string key,int value){Text[key]=value.ToString(System.Globalization.CultureInfo.InvariantCulture);}
        public static bool GetBool(string key,bool fallback){string value;return Text.TryGetValue(key,out value)?value=="true":fallback;}
        public static void SetBool(string key,bool value){Text[key]=value?"true":"false";}
    }
}
namespace UnityEditor.SceneManagement
{
    public enum NewSceneSetup { EmptyScene }
    public enum NewSceneMode { Single }
    public struct SceneSetup { public string path; public bool isLoaded,isActive; }
    public static class EditorSceneManager
    {
        public static int Restores; public static bool ValidationThrowRestore; private static int sceneSerial;
        public static SceneSetup[] GetSceneManagerSetup(){return new[]{new SceneSetup{path=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,isLoaded=true,isActive=true}};}
        public static UnityEngine.SceneManagement.Scene NewScene(NewSceneSetup setup,NewSceneMode mode){UnityEngine.SceneManagement.SceneManager.ValidationClear();var scene=new UnityEngine.SceneManagement.Scene{path="",isLoaded=true,handle=++sceneSerial};UnityEngine.SceneManagement.SceneManager.SetLoaded(scene);return scene;}
        public static void RestoreSceneManagerSetup(SceneSetup[] setup){if(ValidationThrowRestore)throw new InvalidOperationException("Synthetic RestoreSceneManagerSetup failure.");Restores++;UnityEngine.SceneManagement.SceneManager.ValidationClear();foreach(var item in setup)UnityEngine.SceneManagement.SceneManager.SetLoaded(new UnityEngine.SceneManagement.Scene{path=item.path,isLoaded=item.isLoaded});}
    }
}
