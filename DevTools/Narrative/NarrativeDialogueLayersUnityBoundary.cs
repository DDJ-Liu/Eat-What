// External engine/TMP handles for this appended group only. No production dim/aggregation logic here.
using System;
using System.Collections.Generic;
namespace UnityEngine
{
    public class Object
    {
        public string name; public bool Destroyed;
        public static int Created; public Object(){Created++;}
        public static void Destroy(Object target) { if (target != null) target.Destroyed = true; }
    }
    public class Component : Object
    {
        public GameObject gameObject;
        public Transform transform { get { return gameObject.transform; } }
        public T GetComponent<T>() where T : Component { return gameObject.GetComponent<T>(); }
    }
    public class MonoBehaviour : Component { public bool enabled=true;public bool isActiveAndEnabled=>enabled&&gameObject!=null&&gameObject.activeInHierarchy; }
    public class ScriptableObject : Object { }
    public sealed class CreateAssetMenuAttribute : Attribute { public string fileName, menuName; }
    public static class Debug { public static readonly List<string> Warnings = new List<string>(); public static void LogWarning(string value) { Warnings.Add(value); } }
    public class Transform : Component
    {
        readonly List<Transform> children = new List<Transform>();
        public Transform parent { get; private set; }
        public Vector3 localScale=Vector3.one;
        public bool IsChildOf(Transform other){for(var p=this;p!=null;p=p.parent)if(p==other)return true;return false;}
        public int childCount { get { return children.Count; } }
        public Transform GetChild(int index) { return children[index]; }
        public int GetSiblingIndex() { return parent == null ? 0 : parent.children.IndexOf(this); }
        public void SetParent(Transform next, bool worldPositionStays)
        { if (parent != null) parent.children.Remove(this); parent = next; if (next != null) next.children.Add(this); }
    }
    public class RectTransform : Transform { public Vector2 anchorMin, anchorMax, offsetMin, offsetMax,pivot,anchoredPosition,sizeDelta; }
    public class GameObject : Object
    {
        readonly Dictionary<Type, Component> components = new Dictionary<Type, Component>();
        public Transform transform { get; private set; }
        public bool activeSelf = true;
        public bool activeInHierarchy=>activeSelf&&!Destroyed&&(transform.parent==null||transform.parent.gameObject.activeInHierarchy);
        public GameObject(string value, params Type[] types)
        {
            name = value;
            Type transformType = Array.IndexOf(types, typeof(RectTransform)) >= 0 ? typeof(RectTransform) : typeof(Transform);
            transform = (Transform)AddComponent(transformType);
            foreach (var type in types) if (type != transformType) AddComponent(type);
        }
        Component AddComponent(Type type)
        { var value = (Component)Activator.CreateInstance(type); value.gameObject = this; components.Add(type, value); return value; }
        public T AddComponent<T>() where T : Component, new() { return (T)AddComponent(typeof(T)); }
        public T GetComponent<T>() where T : Component
        { foreach (var value in components.Values) if (value is T) return (T)value; return null; }
        public void SetActive(bool value) { activeSelf = value; }
    }
    public struct Vector2
    {
        public float x, y; public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero { get { return new Vector2(0, 0); } }
        public static Vector2 one { get { return new Vector2(1, 1); } }
    }
    public struct Vector3{public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}public static Vector3 one=>new Vector3(1,1,1);public static Vector3 operator *(Vector3 v,float n)=>new Vector3(v.x*n,v.y*n,v.z*n);}
    public struct Color { public float r, g, b, a; public Color(float r, float g, float b, float a) { this.r = r; this.g = g; this.b = b; this.a = a; } public static Color clear=>new Color();public static Color white=>new Color(1,1,1,1); }
    public class Material : Object { }
    public enum RenderMode { ScreenSpaceOverlay, ScreenSpaceCamera, WorldSpace }
    public class Canvas : Component { public bool enabled=true;public RenderMode renderMode; public int sortingOrder; }
    public class CanvasGroup : Component { public float alpha=1;public bool interactable = true, blocksRaycasts = true; }
    public static class Time { public static float unscaledDeltaTime,deltaTime;public static int frameCount; }
    public static class Mathf{public static float Max(float a,float b)=>Math.Max(a,b);}
    public static class Application{public static string persistentDataPath;}
    public sealed class SerializeField : Attribute { }
    public sealed class TooltipAttribute : Attribute { public TooltipAttribute(string value) { } }
    public sealed class DisallowMultipleComponent : Attribute { }
    public sealed class RequireComponent : Attribute { public RequireComponent(Type type) { } }
}
namespace UnityEngine.UI
{
    public class CanvasScaler : UnityEngine.Component
    {
        public enum ScaleMode { ScaleWithScreenSize }
        public enum ScreenMatchMode { MatchWidthOrHeight }
        public ScaleMode uiScaleMode; public ScreenMatchMode screenMatchMode;
        public UnityEngine.Vector2 referenceResolution; public float matchWidthOrHeight;
    }
    public class Image : UnityEngine.Component { public UnityEngine.Color color; public bool raycastTarget = true; }
    public class GraphicRaycaster : UnityEngine.Component { }
}
namespace TMPro
{
    public class TMP_FontAsset : UnityEngine.Object { public UnityEngine.Material material; }
    public enum TextAlignmentOptions{TopLeft,Center}public enum TextWrappingModes{NoWrap}
    public struct TMP_LineInfo{public float baseline;}public struct TMP_CharacterInfo{public int index;}
    public class TMP_TextInfo{public int lineCount,characterCount;public TMP_LineInfo[] lineInfo=new TMP_LineInfo[0];public TMP_CharacterInfo[] characterInfo=new TMP_CharacterInfo[0];}
    public class TMP_Text:UnityEngine.Component{}
    // External TMP handles only: no text layout, glyph, baseline or wrapping algorithm is supplied.
    public class TextMeshProUGUI:TMP_Text
    {
        public static readonly List<string> Bindings=new List<string>();TMP_FontAsset f;UnityEngine.Material m;
        public TMP_FontAsset font{get=>f;set{Bindings.Add("font");f=value;}}public UnityEngine.Material fontSharedMaterial{get=>m;set{Bindings.Add("material");m=value;}}
        public string text;public int maxVisibleCharacters;public float lineSpacing,fontSize,preferredWidth;
        public bool useMaxVisibleDescender,enableAutoSizing,richText,isOrthographic,raycastTarget;public TextAlignmentOptions alignment;public TextWrappingModes textWrappingMode;public UnityEngine.Color color;
        public TMP_TextInfo textInfo=new TMP_TextInfo();public void ForceMeshUpdate(bool a,bool b){}
    }
}
namespace UnityEngine.EventSystems
{
    public class BaseEventData{}
    public class PointerEventData:BaseEventData{public enum InputButton{Left,Right}public InputButton button;}
    public enum EventTriggerType{PointerClick}
    public class EventTrigger:UnityEngine.MonoBehaviour
    {
        public class Callback{public event Action<BaseEventData> Listeners;public void AddListener(Action<BaseEventData> action){Listeners+=action;}public void Invoke(BaseEventData value){Listeners?.Invoke(value);}}
        public class Entry{public EventTriggerType eventID;public Callback callback=new Callback();}
        public List<Entry> triggers=new List<Entry>();
    }
    public class EventSystem:UnityEngine.MonoBehaviour{public static EventSystem current;public bool sendNavigationEvents;public EventSystem(){current=this;}}
    public class StandaloneInputModule:UnityEngine.MonoBehaviour{}
}
