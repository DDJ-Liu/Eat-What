// Offline Unity boundary. Real installed Composer, ScreenComposerSettings and Predictor/Damper are compiled separately.
using System;
using System.Collections.Generic;
namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.All)] public class SerializeField : Attribute { }
    [AttributeUsage(AttributeTargets.All)] public class DisallowMultipleComponent : Attribute { }
    [AttributeUsage(AttributeTargets.All)] public class HeaderAttribute : Attribute { public HeaderAttribute(string s){} }
    [AttributeUsage(AttributeTargets.All)] public class TooltipAttribute : Attribute { public TooltipAttribute(string s){} }
    [AttributeUsage(AttributeTargets.All)] public class HelpURLAttribute : Attribute { public HelpURLAttribute(string s){} }
    [AttributeUsage(AttributeTargets.All)] public class AddComponentMenuAttribute : Attribute { public AddComponentMenuAttribute(string s){} }
    [AttributeUsage(AttributeTargets.All)] public class RangeAttribute : Attribute { public RangeAttribute(float a,float b){} }
    [AttributeUsage(AttributeTargets.All)] public class CreateAssetMenuAttribute : Attribute { public string menuName; }
    [AttributeUsage(AttributeTargets.All)] public class RuntimeInitializeOnLoadMethodAttribute : Attribute { }
    public class ScriptableObject { }
    public class Collider2D { }
    public static partial class Application { public static bool isPlaying=true; }
    public static partial class Time
    { public static float time,deltaTime=.016f,unscaledDeltaTime=.016f; public static bool inFixedTimeStep; public static int frameCount; }
    public class MonoBehaviour
    {
        public bool enabled=true;
        public bool isActiveAndEnabled { get=>enabled; set=>enabled=value; }
        public Transform transform=new Transform();
        protected bool TryGetComponent<T>(out T c) where T:class { c=transform.GetComponent<T>();return c!=null; }
        public T GetComponent<T>() where T:class=>transform.GetComponent<T>();
    }
    public class Transform
    {
        public Transform parent;
        public Vector3 localPosition,localScale=Vector3.one;
        public Quaternion localRotation=Quaternion.identity;
        public readonly Dictionary<Type,object> components=new Dictionary<Type,object>();
        public T GetComponent<T>() where T:class
        { foreach(var c in components.Values)if(c is T t)return t;return null; }
        public T GetComponentInParent<T>() where T:class
        { for(var p=this;p!=null;p=p.parent){var c=p.GetComponent<T>();if(c!=null)return c;}return null; }
        public bool IsChildOf(Transform other)
        { for(var p=this;p!=null;p=p.parent)if(ReferenceEquals(p,other))return true;return false; }
        public Quaternion rotation => parent==null ? localRotation : parent.rotation*localRotation;
        public Vector3 right=>rotation*Vector3.right;
        public Vector3 position
        { get=>parent==null?localPosition:parent.TransformPoint(localPosition);
          set=>localPosition=parent==null?value:parent.InverseTransformPoint(value); }
        public Vector3 TransformPoint(Vector3 value)
        { var p=localPosition+localRotation*Vector3.Scale(localScale,value);return parent==null?p:parent.TransformPoint(p); }
        public Vector3 InverseTransformPoint(Vector3 value)
        { var p=Quaternion.Inverse(localRotation)*((parent==null?value:parent.InverseTransformPoint(value))-localPosition);
          return new Vector3(p.x/localScale.x,p.y/localScale.y,p.z/localScale.z); }
    }
    public struct Vector2
    {
        public float x,y;
        public Vector2(float a,float b){x=a;y=b;}
        public static Vector2 zero=>new Vector2();
        public static Vector2 one=>new Vector2(1,1);
        public static implicit operator Vector2(Vector3 v)=>new Vector2(v.x,v.y);
        public static float Angle(Vector2 a,Vector2 b)
        {double d=Math.Sqrt((a.x*a.x+a.y*a.y)*(b.x*b.x+b.y*b.y));return d<=0?0:(float)(Math.Acos(Math.Max(-1,Math.Min(1,(a.x*b.x+a.y*b.y)/d)))*180/Math.PI);}
        public static Vector2 operator +(Vector2 a,Vector2 b)=>new Vector2(a.x+b.x,a.y+b.y);
        public static Vector2 operator -(Vector2 a,Vector2 b)=>new Vector2(a.x-b.x,a.y-b.y);
        public static Vector2 operator -(Vector2 a)=>new Vector2(-a.x,-a.y);
        public static Vector2 operator *(Vector2 a,float f)=>new Vector2(a.x*f,a.y*f);
        public static bool operator ==(Vector2 a,Vector2 b)=>a.Equals(b);
        public static bool operator !=(Vector2 a,Vector2 b)=>!a.Equals(b);
        public static Vector2 Lerp(Vector2 a,Vector2 b,float t)=>a+(b-a)*t;
        public override bool Equals(object o)=>o is Vector2 b&&x==b.x&&y==b.y;
        public override int GetHashCode()=>x.GetHashCode()^y.GetHashCode();
    }
    public struct Vector3
    {
        public float x,y,z;
        public Vector3(float a,float b,float c){x=a;y=b;z=c;}
        public static Vector3 zero=>new Vector3();
        public static Vector3 one=>new Vector3(1,1,1);
        public static Vector3 up=>new Vector3(0,1,0);
        public static Vector3 right=>new Vector3(1,0,0);
        public static Vector3 forward=>new Vector3(0,0,1);
        public static Vector3 back=>new Vector3(0,0,-1);
        public float sqrMagnitude=>x*x+y*y+z*z;
        public float magnitude=>(float)Math.Sqrt(sqrMagnitude);
        public Vector3 normalized=>magnitude>0?this/magnitude:zero;
        public float this[int i] { get=>i==0?x:i==1?y:z;set{if(i==0)x=value;else if(i==1)y=value;else z=value;} }
        public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
        public static Vector3 operator -(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);
        public static Vector3 operator -(Vector3 a)=>new Vector3(-a.x,-a.y,-a.z);
        public static Vector3 operator *(Vector3 a,float f)=>new Vector3(a.x*f,a.y*f,a.z*f);
        public static Vector3 operator *(float f,Vector3 a)=>a*f;
        public static Vector3 operator /(Vector3 a,float f)=>new Vector3(a.x/f,a.y/f,a.z/f);
        public static bool operator ==(Vector3 a,Vector3 b)=>a.Equals(b);
        public static bool operator !=(Vector3 a,Vector3 b)=>!a.Equals(b);
        public static Vector3 Scale(Vector3 a,Vector3 b)=>new Vector3(a.x*b.x,a.y*b.y,a.z*b.z);
        public static Vector3 Max(Vector3 a,Vector3 b)=>new Vector3(Math.Max(a.x,b.x),Math.Max(a.y,b.y),Math.Max(a.z,b.z));
        public static Vector3 Lerp(Vector3 a,Vector3 b,float t)=>a+(b-a)*t;
        public static float Dot(Vector3 a,Vector3 b)=>a.x*b.x+a.y*b.y+a.z*b.z;
        public static Vector3 SmoothDamp(Vector3 a,Vector3 b,ref Vector3 v,float time,float max,float dt)
            =>throw new InvalidOperationException("Native prediction must be disabled for the F01 fixed-distance path.");
        public override bool Equals(object o)=>o is Vector3 b&&x==b.x&&y==b.y&&z==b.z;
        public override int GetHashCode()=>x.GetHashCode()^y.GetHashCode()^z.GetHashCode();
    }
    public struct Quaternion
    {
        public float x,y,z,w;
        public Quaternion(float a,float b,float c,float d){x=a;y=b;z=c;w=d;}
        public static Quaternion identity=>new Quaternion(0,0,0,1);
        public static Quaternion Euler(float x,float y,float z)
        { double r=z*Math.PI/360;return new Quaternion(0,0,(float)Math.Sin(r),(float)Math.Cos(r)); }
        public static Quaternion Inverse(Quaternion q)
        { float n=q.x*q.x+q.y*q.y+q.z*q.z+q.w*q.w;return n>0?new Quaternion(-q.x/n,-q.y/n,-q.z/n,q.w/n):identity; }
        public static Quaternion operator *(Quaternion a,Quaternion b)=>new Quaternion(
            a.w*b.x+a.x*b.w+a.y*b.z-a.z*b.y,a.w*b.y-a.x*b.z+a.y*b.w+a.z*b.x,
            a.w*b.z+a.x*b.y-a.y*b.x+a.z*b.w,a.w*b.w-a.x*b.x-a.y*b.y-a.z*b.z);
        public static Vector3 operator *(Quaternion q,Vector3 v)
        {
            float a=q.x*2,b=q.y*2,c=q.z*2;
            return new Vector3((1-q.y*b-q.z*c)*v.x+(q.x*b-q.w*c)*v.y+(q.x*c+q.w*b)*v.z,
                (q.x*b+q.w*c)*v.x+(1-q.x*a-q.z*c)*v.y+(q.y*c-q.w*a)*v.z,
                (q.x*c-q.w*b)*v.x+(q.y*c+q.w*a)*v.y+(1-q.x*a-q.y*b)*v.z);
        }
    }
    public struct Rect
    {
        public float x,y,width,height;
        public Rect(Vector2 p,Vector2 s){x=p.x;y=p.y;width=s.x;height=s.y;}
        public Rect(float a,float b,float c,float d){x=a;y=b;width=c;height=d;}
        public float xMin {get=>x;set{float end=xMax;x=value;width=end-x;}}
        public float xMax {get=>x+width;set=>width=value-x;}
        public float yMin {get=>y;set{float end=yMax;y=value;height=end-y;}}
        public float yMax {get=>y+height;set=>height=value-y;}
        public Vector2 center {get=>new Vector2(x+width/2,y+height/2);set{x=value.x-width/2;y=value.y-height/2;}}
        public Vector2 position {get=>new Vector2(x,y);set{x=value.x;y=value.y;}}
    }
    public static class Mathf
    {
        public const float Deg2Rad=(float)(Math.PI/180);
        public static float Abs(float x)=>Math.Abs(x);
        public static float Min(float a,float b)=>Math.Min(a,b);
        public static float Max(float a,float b)=>Math.Max(a,b);
        public static float Clamp(float x,float a,float b)=>Math.Min(b,Math.Max(a,x));
        public static float Exp(float x)=>(float)Math.Exp(x);
        public static float Log(float x)=>(float)Math.Log(x);
        public static float Tan(float x)=>(float)Math.Tan(x);
        public static float Pow(float x,float y)=>(float)Math.Pow(x,y);
        public static int FloorToInt(float x)=>(int)Math.Floor(x);
        public static float Lerp(float a,float b,float t)=>a+(b-a)*t;
        public static bool Approximately(float a,float b)=>Math.Abs(a-b)<.00001f;
    }
}
namespace UnityEngine.Serialization
{
    public class FormerlySerializedAsAttribute:Attribute { public FormerlySerializedAsAttribute(string name){} }
}
namespace UnityEngine.SceneManagement
{ public struct Scene { } public enum LoadSceneMode {Single,Additive} }
