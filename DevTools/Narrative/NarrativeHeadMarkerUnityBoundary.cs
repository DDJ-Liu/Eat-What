// External hierarchy, image, texture, sorting and TMP handles for the actual drawing component.
using System;
using System.Collections.Generic;
namespace UnityEngine
{
    public class Object{public string name;public bool Destroyed;public static void Destroy(Object value){if(value!=null)value.Destroyed=true;}}
    public class Component:Object{public GameObject gameObject;public Transform transform=>gameObject.transform;}
    public class MonoBehaviour:Component{public bool enabled=true;public bool isActiveAndEnabled=>enabled&&gameObject.activeInHierarchy;}
    public sealed class SerializeField:Attribute{}
    public sealed class DisallowMultipleComponent:Attribute{}
    public class Transform:Component
    {public Transform parent;public Vector3 position,localScale=Vector3.one;public Quaternion rotation;public void SetParent(Transform value,bool world){parent=value;}}
    public class RectTransform:Transform{public Vector2 sizeDelta,anchorMin,anchorMax,offsetMin,offsetMax;}
    public class GameObject:Object
    {
        readonly List<Component> components=new List<Component>();public Transform transform;public bool activeSelf=true;
        public bool activeInHierarchy=>activeSelf&&(transform.parent==null||transform.parent.gameObject.activeInHierarchy);
        public GameObject(string name,params Type[] types){this.name=name;transform=(Transform)Add(Array.IndexOf(types,typeof(RectTransform))>=0?typeof(RectTransform):typeof(Transform));foreach(var t in types)if(t!=transform.GetType())Add(t);}
        Component Add(Type type){var value=(Component)Activator.CreateInstance(type);value.gameObject=this;components.Add(value);return value;}
        public T AddComponent<T>()where T:Component,new(){return (T)Add(typeof(T));}
        public T GetComponent<T>()where T:Component{foreach(var c in components)if(c is T value)return value;return null;}
        public void SetActive(bool value){activeSelf=value;}
    }
    public struct Vector2{public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}public static Vector2 zero=>new Vector2();public static Vector2 one=>new Vector2(1,1);}
    public struct Vector3{public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}public static Vector3 one=>new Vector3(1,1,1);public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);public static Vector3 operator *(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);}
    public struct Quaternion{}
    public struct Rect{public Rect(float x,float y,float w,float h){}}
    public struct Color{public float r,g,b,a;public Color(float r,float g,float b,float a){this.r=r;this.g=g;this.b=b;this.a=a;}}
    public struct Color32{public byte r,g,b,a;public Color32(byte r,byte g,byte b,byte a){this.r=r;this.g=g;this.b=b;this.a=a;}}
    public enum TextureFormat{RGBA32}
    public class Texture2D:Object{public Color32[] Pixels;public bool MadeUnreadable;public Texture2D(int w,int h,TextureFormat format,bool mip){}public void SetPixels32(Color32[] values){Pixels=values;}public void Apply(bool mip,bool unreadable){MadeUnreadable=unreadable;}}
    public class Sprite:Object{public Texture2D Texture;public static Sprite Create(Texture2D texture,Rect rect,Vector2 pivot,float ppu){return new Sprite{Texture=texture};}}
    public class Material:Object{}
    public class Camera:Component{}
    public struct SortingLayer{public string name;public int id;public static SortingLayer[] layers={new SortingLayer{name="LL_Characters",id=4},new SortingLayer{name="LL_MidgroundFront",id=5}};}
    public enum RenderMode{WorldSpace}
    public class Canvas:Component{public RenderMode renderMode;public bool overrideSorting;public int sortingLayerID,sortingOrder;public Camera worldCamera;}
    public class CanvasGroup:Component{public float alpha;public bool interactable=true,blocksRaycasts=true;}
    public static class Debug{public static readonly List<string> Warnings=new List<string>();public static void LogWarning(string message){Warnings.Add(message);}}
}
namespace UnityEngine.UI
{
    public class Image:UnityEngine.Component{public enum Type{Simple}public Type type;public UnityEngine.Sprite sprite;public UnityEngine.Color color;public bool raycastTarget=true;}
}
namespace TMPro
{
    public enum TextAlignmentOptions{Center}public enum TextWrappingModes{NoWrap}
    public class TMP_FontAsset:UnityEngine.Object{public UnityEngine.Material material;}
    public class TextMeshProUGUI:UnityEngine.Component
    {
        public static readonly List<string> Bindings=new List<string>();
        TMP_FontAsset current;UnityEngine.Material shared;
        public TMP_FontAsset font{get=>current;set{Bindings.Add("font");current=value;}}
        public UnityEngine.Material fontSharedMaterial{get=>shared;set{Bindings.Add("material");shared=value;}}
        public bool isOrthographic,richText,raycastTarget=true;public float fontSize;public string text;
        public UnityEngine.Color color;public TextAlignmentOptions alignment;public TextWrappingModes textWrappingMode;
    }
}
