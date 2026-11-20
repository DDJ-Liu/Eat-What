// External keyboard/GUI, scene objects, row delivery and display/text capabilities only.
using System;
using System.Collections.Generic;
namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.Class)] public sealed class DisallowMultipleComponent:Attribute{}
    [AttributeUsage(AttributeTargets.Class)] public sealed class DefaultExecutionOrder:Attribute{public DefaultExecutionOrder(int value){}}
    [AttributeUsage(AttributeTargets.Class)] public sealed class CreateAssetMenuAttribute:Attribute{public string menuName;}
    [AttributeUsage(AttributeTargets.Field)] public sealed class SerializeField:Attribute{}
    [AttributeUsage(AttributeTargets.Field)] public sealed class TooltipAttribute:Attribute{public TooltipAttribute(string text){}}
    public class Object
    {
        internal static readonly List<Object> All=new List<Object>();internal bool destroyed;public string name;
        public Object(){All.Add(this);}
        public static bool operator ==(Object a,Object b){bool x=ReferenceEquals(a,null)||a.destroyed,y=ReferenceEquals(b,null)||b.destroyed;return x||y?x==y:ReferenceEquals(a,b);}
        public static bool operator !=(Object a,Object b){return !(a==b);}public override bool Equals(object v){return ReferenceEquals(this,v);}public override int GetHashCode(){return base.GetHashCode();}
        public static T[] FindObjectsByType<T>(FindObjectsSortMode sort)where T:Object{var r=new List<T>();foreach(var v in All)if(v is T t&&!t.destroyed&&(!(v is Component c)||c.gameObject.activeInHierarchy))r.Add(t);return r.ToArray();}
        public static void Destroy(Object v){if(v!=null)v.destroyed=true;}
        public static Func<GameObject,Transform,bool,GameObject> InstantiateFactory; // Optional external scene-clone capability for explicit lifecycle fixtures.
        public static GameObject Instantiate(GameObject source,Transform parent,bool world)
        {if(InstantiateFactory!=null)return InstantiateFactory(source,parent,world);var v=new GameObject(parent==null?source.scene:parent.gameObject.scene,source.name+"(Clone)");v.transform.SetParent(parent,world);v.transform.localPosition=source.transform.localPosition;v.transform.localScale=source.transform.localScale;foreach(var c in source.transform.Children)Instantiate(c.gameObject,v.transform,false);return v;}
    }
    public enum FindObjectsSortMode{InstanceID}
    public class ScriptableObject:Object{}
    public class Sprite:Object{}
    public class Component:Object{public GameObject gameObject;public Transform transform=>gameObject.transform;public T GetComponent<T>()where T:class{return gameObject.GetComponent<T>();}}
    public class Behaviour:Component{public bool enabled=true;public bool isActiveAndEnabled=>enabled&&gameObject.activeInHierarchy&&!destroyed;}
    public class MonoBehaviour:Behaviour{}
    public class GameObject:Object
    {
        public SceneManagement.Scene scene;public readonly Transform transform;public bool activeSelf=true;readonly List<Component> components=new List<Component>();
        public bool activeInHierarchy=>activeSelf&&!destroyed&&(transform.parent==null||transform.parent.gameObject.activeInHierarchy);
        public GameObject(SceneManagement.Scene scene,string name="object"){this.scene=scene;this.name=name;transform=new Transform{gameObject=this};components.Add(transform);}
        public static Action<GameObject,bool> ActiveChanged;
        public void SetActive(bool v){bool changed=activeSelf!=v;activeSelf=v;if(changed&&ActiveChanged!=null)ActiveChanged(this,v);}
        public GameObject(string name):this(default(SceneManagement.Scene),name){}
        public Component AddComponent(Type type){var c=(Component)Activator.CreateInstance(type);c.gameObject=this;components.Add(c);return c;}
        public T[] GetComponents<T>()where T:Component{var r=new List<T>();foreach(var c in components)if(c is T value&&!c.destroyed)r.Add(value);return r.ToArray();}
        public T AddComponent<T>()where T:Component,new(){var c=new T{gameObject=this};components.Add(c);return c;}
        public T GetComponent<T>()where T:class{foreach(var c in components)if(c is T t&&!c.destroyed)return t;return null;}
        public T[] GetComponentsInChildren<T>(bool inactive)where T:Component
        {var r=new List<T>();if(inactive||activeInHierarchy){foreach(var c in components)if(c is T t&&!t.destroyed)r.Add(t);foreach(var child in transform.Children)r.AddRange(child.gameObject.GetComponentsInChildren<T>(inactive));}return r.ToArray();}
    }
    public sealed class Transform:Component
    {
        public readonly List<Transform> Children=new List<Transform>();public Transform parent;public Vector3 localPosition;public Vector3 localScale=Vector3.one;public Quaternion localRotation;
        public int childCount=>Children.Count;public Transform GetChild(int i)=>Children[i];
        public bool IsChildOf(Transform t){for(var p=this;p!=null;p=p.parent)if(p==t)return true;return false;}
        public void SetParent(Transform p,bool world){var positionBefore=position;if(parent!=null)parent.Children.Remove(this);parent=p;if(p!=null){p.Children.Add(this);gameObject.scene=p.gameObject.scene;}if(world)position=positionBefore;}
        public Vector3 position{get=>parent==null?localPosition:parent.position+localPosition;set=>localPosition=parent==null?value:value-parent.position;}
    }
    public struct Vector3{public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}public static Vector3 one=>new Vector3(1,1,1);public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);public static Vector3 operator -(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);}
    public struct Quaternion{}
    public struct Vector2{public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}}
    public struct Rect{public float x,y,width,height;public Rect(float x,float y,float w,float h){this.x=x;this.y=y;width=w;height=h;}public bool Contains(Vector2 p)=>p.x>=x&&p.y>=y&&p.x<x+width&&p.y<y+height;}
    public class Camera:Behaviour{}
    public static class Screen{public static int height=1080;}
    public static class Application{public static bool isPlaying=true;}
    public static class Mathf{public static float Abs(float v)=>Math.Abs(v);}
    public static class Time{public static double timeAsDouble;public static float deltaTime;}
    public static class Random{public static int Range(int min,int max)=>min;}
    public static class JsonUtility{public static string ToJson(object value)=>"{}";}
    public static class Debug{public static readonly List<string> Warnings=new List<string>();public static void LogWarning(string s,object context=null){Warnings.Add(s);}}
    public static class GUI{public static bool enabled=true;public static readonly Skin skin=new Skin();public class Skin{public object window;}}
    public static class GUILayout
    {
        public static readonly List<string> Labels=new List<string>();public static readonly HashSet<string> Clicks=new HashSet<string>();
        public static readonly List<string> Buttons=new List<string>();public static readonly List<string> DisabledButtons=new List<string>();
        public static void BeginArea(Rect r,string title,object style){}public static void EndArea(){}
        public static Vector2 BeginScrollView(Vector2 v)=>v;public static void EndScrollView(){}
        public static void Label(string s){Labels.Add(s);}public static bool Button(string s){Buttons.Add(s);if(!GUI.enabled){DisabledButtons.Add(s);return false;}return Clicks.Remove(s);}
    }
}
namespace UnityEngine.EventSystems
{
    // External UGUI event/raycast transport only. No dialogue input, ordering or selection implementation.
    public interface IPointerClickHandler{void OnPointerClick(PointerEventData value);}
    public class PointerEventData{public UnityEngine.Vector2 position;public bool used;public void Use(){used=true;}}
    public struct RaycastResult{public UnityEngine.GameObject gameObject;public BaseRaycaster module;public float index,distance;public int depth;}
    public abstract class BaseRaycaster:UnityEngine.MonoBehaviour
    {public abstract UnityEngine.Camera eventCamera{get;}public virtual int sortOrderPriority=>int.MinValue;public virtual int renderOrderPriority=>int.MinValue;public abstract void Raycast(PointerEventData value,List<RaycastResult> hits);}
}
namespace UnityEngine.SceneManagement
{
    public struct Scene
    {
        int id;public string name;public bool isLoaded;public Scene(int id,string name){this.id=id;this.name=name;isLoaded=true;}
        public bool IsValid()=>id!=0;
        public UnityEngine.GameObject[] GetRootGameObjects(){var r=new List<UnityEngine.GameObject>();foreach(var o in UnityEngine.Object.All)if(o is UnityEngine.GameObject g&&!g.destroyed&&g.scene==this&&g.transform.parent==null)r.Add(g);return r.ToArray();}
        public static bool operator ==(Scene a,Scene b)=>a.id==b.id;public static bool operator !=(Scene a,Scene b)=>a.id!=b.id;public override bool Equals(object v)=>v is Scene s&&s.id==id;public override int GetHashCode()=>id;
    }
}
namespace UnityEngine.InputSystem
{
    public enum Key{None,F8,F9}
    public sealed class KeyControl{public bool wasPressedThisFrame;}
    public sealed class Keyboard{public static Keyboard current;readonly Dictionary<Key,KeyControl> keys=new Dictionary<Key,KeyControl>();public KeyControl this[Key value]{get{KeyControl key;if(!keys.TryGetValue(value,out key)){key=new KeyControl();keys.Add(value,key);}return key;}}}
}
public sealed class CK01GeneratedDataCatalog:UnityEngine.ScriptableObject
{
    public readonly Dictionary<string,List<UnityEngine.ScriptableObject>> Rows=new Dictionary<string,List<UnityEngine.ScriptableObject>>();public string SourceKind=>"ExternalFixtureRows";
    public bool TryGetRows<T>(string table,out IList<T> rows,out string issue)where T:UnityEngine.ScriptableObject
    {var r=new List<T>();List<UnityEngine.ScriptableObject> source;if(Rows.TryGetValue(table,out source))foreach(var v in source)r.Add((T)v);rows=r;issue=null;return true;}
    public bool TryGetCharacter(string id,out CK01CharacterData row,out string issue)
    {IList<CK01CharacterData> r;TryGetRows("Characters",out r,out issue);foreach(var x in r)if(x.character_id==id){row=x;return true;}row=null;issue="missing character";return false;}
}
public static class LocService{public static string Get(string key)=>"text:"+key;}
namespace EatWhat.LongLoop
{
    // Unchanged depth/ground capabilities are outside this feature's behavior under test.
    public static class DepthScale{public static float ResolveFacingMagnitude(UnityEngine.Transform t,float v,ref bool held)=>v;public static bool TrySetFacing(UnityEngine.Transform t,bool right)=>false;}
    public static class GroundSpace{public static int Calls;public static double GroundDistance(UnityEngine.Vector3 a,UnityEngine.Vector3 b){Calls++;var v=a-b;return Math.Sqrt(v.x*v.x+v.y*v.y+v.z*v.z);}}
}
