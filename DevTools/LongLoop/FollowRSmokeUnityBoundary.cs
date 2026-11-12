// Offline boundary for verbatim compiled short-probe lifecycle methods. No native Unity claim.
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using EatWhat.LongLoop;
using EatWhat.LongLoop.Configuration;
using EatWhat.Toolkit.Input;
using UnityEngine;
using Unity.Cinemachine;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
namespace UnityEngine
{
    public enum LogType {Error,Assert,Warning,Log,Exception}
    public static partial class Application
    {
        public static string dataPath,unityVersion="OFFLINE_BOUNDARY";public static bool isFocused=true;
        public static event Action onBeforeRender;
        public static event Action<string,string,LogType> logMessageReceived;
        public static void Render()=>onBeforeRender?.Invoke();
        public static void Error()=>logMessageReceived?.Invoke("fault","",LogType.Error);
        public static int Callbacks=>(onBeforeRender?.GetInvocationList().Length??0)+(logMessageReceived?.GetInvocationList().Length??0);
    }
    public static partial class Time {public static double? GameClockOverride,WallClockOverride;public static double timeAsDouble=>GameClockOverride??time;public static double realtimeSinceStartupAsDouble=>WallClockOverride??time;}
    public enum KeyCode {A,D,LeftArrow,RightArrow,E,Escape,W,S,UpArrow,DownArrow}
    public static class Screen {public static int width=1600,height=900;}
    public static class Input
    {
        public static readonly HashSet<KeyCode> Held=new HashSet<KeyCode>(),Down=new HashSet<KeyCode>();public static Vector3 mousePosition;
        public static bool GetKey(KeyCode key)=>Held.Contains(key);public static bool GetKeyDown(KeyCode key)=>Down.Contains(key);
    }
    public sealed class Camera:MonoBehaviour { }
    public sealed class Rigidbody2D {public Vector2 position;}
    public static class JsonUtility
    {
        private sealed class Fields:DefaultContractResolver
        {protected override IList<JsonProperty> CreateProperties(Type t,MemberSerialization m)=>base.CreateProperties(t,m).Where(p=>t.GetField(p.UnderlyingName,BindingFlags.Public|BindingFlags.Instance)!=null).ToList();}
        private static readonly JsonSerializerSettings Settings=new JsonSerializerSettings {ContractResolver=new Fields()};
        public static string ToJson(object v,bool pretty=false)=>JsonConvert.SerializeObject(v,pretty?Formatting.Indented:Formatting.None,Settings);
        public static T FromJson<T>(string text)=>JsonConvert.DeserializeObject<T>(text,Settings);
    }
}
namespace UnityEditor
{
    public static class EditorApplication
    {
        public static bool isPlaying=true,isCompiling,isUpdating;
        public static event Action update;
        public static void Watch()=>update?.Invoke();
        public static int Callbacks=>update?.GetInvocationList().Length??0;
    }
}
namespace UnityEngine.SceneManagement
{
    public struct SceneSnapshot {public string path;}
    public static class SceneManager
    {public static string CurrentPath="Assets/Scenes/LongLoop/LongLoop_Street.unity";public static SceneSnapshot GetActiveScene()=>new SceneSnapshot {path=CurrentPath};}
}
namespace Spine.Unity {public sealed class SkeletonAnimation:MonoBehaviour {public object AnimationState=new object();}}
namespace Unity.Cinemachine
{
    public sealed class CinemachineBrain:MonoBehaviour
    {public Camera OutputCamera=new Camera();public bool IsBlending;public CameraState State;}
    public sealed class BrainEvent
    {
        private event Action<CinemachineBrain> Handlers;
        public void AddListener(Action<CinemachineBrain> h){Handlers+=h;}
        public void RemoveListener(Action<CinemachineBrain> h){Handlers-=h;}
        public void Emit(CinemachineBrain b)=>Handlers?.Invoke(b);
        public int Count=>Handlers?.GetInvocationList().Length??0;
    }
    public static partial class CinemachineCore {public static readonly BrainEvent CameraUpdatedEvent=new BrainEvent();}
}
namespace EatWhat.LongLoop
{
    public interface IPlayerAvatar {void SetFacing(bool b);void SetMoving(bool b);}
    public sealed class TomatoSpineAvatar:MonoBehaviour,IPlayerAvatar
    {public Spine.Unity.SkeletonAnimation skeletonAnimation=new Spine.Unity.SkeletonAnimation();public void SetFacing(bool b){}public void SetMoving(bool b){}}
    public sealed class WalkArea {public bool IsUsable=true;public Vector2 LastPosition;public Func<Vector2,bool> Predicate;public bool Contains(Vector2 v){LastPosition=v;return Predicate==null||Predicate(v);}}
    public partial class LongLoopPlayerController
    {
        public IGameInput input;public IPlayerAvatar avatar;public WalkArea walkArea=new WalkArea();public Rigidbody2D targetRigidbody;public float MoveSpeed=6.5f;
        public void ConfigureInterfaces(IGameInput i,IPlayerAvatar a){input=i;avatar=a;}
        public void StopMovement(){MoveInput=Vector2.zero;}
    }
}
namespace EatWhat.EditorValidation
{
    public static partial class LongLoopAutomatedProbe
    {
        public static FollowRRun followRSmoke;
        private sealed class Legacy {public bool Terminal=true;public StreetProbeRequest Request;public StreetProbeReport Report()=>new StreetProbeReport();}
        private static Legacy active,smoke;
        public static readonly Dictionary<string,object> Objects=new Dictionary<string,object>();
        public static T Resolve<T>(string p) where T:class=>Objects.TryGetValue(p??"",out var v)&&v is T t?t:throw new InvalidOperationException("Missing offline object "+p);
        public static object RuntimeField(object o,string n,Type t)=>o.GetType().GetField(n)?.GetValue(o);
        public static T Ref<T>(object o,string n) where T:class=>RuntimeField(o,n,typeof(T)) as T;
        public static string Hash(byte[] b){using(var h=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(h.ComputeHash(b)).Replace("-","");}
        public static void NoLinks(string p)=>StreetPayloadContract.NoLinks(p);
        public static void WriteNew(string p,string v){using(var s=new FileStream(p,FileMode.CreateNew))using(var w=new StreamWriter(s))w.Write(v);}
        public static void PublishProbeEvidence(string p,string v)=>File.WriteAllText(p,v);
        public static bool ClaimAllowed=true;
        public static bool UseRealClaimValidation;
        public static bool ValidateSmokeRequest(StreetProbeRequest r,bool frame=false,string action="frame")=>UseRealClaimValidation?ActualValidateSmokeRequest(r,frame,action):ClaimAllowed;
        public static bool ValidateFollowRClaim(StreetProbeRequest r,string action,out DateTimeOffset lease)
        {if(UseRealClaimValidation)return ActualValidateFollowRClaim(r,action,out lease);lease=DateTimeOffset.UtcNow.AddMinutes(20);return ClaimAllowed;}
        private sealed class Proxy:IGameInput
        {public Vector2 Move;public float Horizontal=>Move.x;public float Vertical=>Move.y;public Vector2 LookAxis=>Vector2.zero;public GameInputDevice CurrentDevice=>GameInputDevice.Keyboard;public bool Interact=>false;public Vector2 MouseScreenPos=>Vector2.zero;}
    }
}
