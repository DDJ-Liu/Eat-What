// Task-specific event/Brain/Camera boundary, not a native engine simulation.
// Production is separately compiled against installed Unity/Cinemachine references.
using System;
using System.Reflection;
using EatWhat.LongLoop;
using UnityEngine;
using Unity.Cinemachine;

namespace UnityEngine { public sealed class Camera:MonoBehaviour { public float orthographicSize=10; } }
namespace Unity.Cinemachine
{
    public sealed class CinemachineBrain:MonoBehaviour { public Camera OutputCamera {get;set;} }
    public static class CinemachineCore
    {
        public sealed class BrainEvent
        {
            private event Action<CinemachineBrain> handlers;
            public int ListenerCount=>handlers==null?0:handlers.GetInvocationList().Length;
            public void AddListener(Action<CinemachineBrain> h)=>handlers+=h;
            public void RemoveListener(Action<CinemachineBrain> h)=>handlers-=h;
            public void Invoke(CinemachineBrain brain)=>handlers?.Invoke(brain);
        }
        public static BrainEvent CameraUpdatedEvent=new BrainEvent();
    }
}
internal static class StreetParallaxLayerTests
{
    private static int checks,scenarios;
    private static void Check(bool ok,string name){checks++;if(!ok)throw new Exception(name);}
    private static void Near(Vector3 a,Vector3 b,string name)
        =>Check((a-b).sqrMagnitude<1e-8,name+": "+a+" / "+b);
    private static void Call(StreetParallaxLayer layer,string name)
        =>typeof(StreetParallaxLayer).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(layer,null);
    private sealed class Fixture
    {
        public readonly StreetParallaxLayer Layer=new StreetParallaxLayer();
        public readonly CinemachineBrain Brain=new CinemachineBrain {OutputCamera=new Camera()};
        public readonly Transform Origin=new Transform();
        public Fixture(){
            Application.isPlaying=true;CinemachineCore.CameraUpdatedEvent=new CinemachineCore.BrainEvent();
            Layer.transform.position=new Vector3(10,20,3);Layer.CameraBrain=Brain;Layer.ParallaxOrigin=Origin;
            Layer.HorizontalCoefficient=.3f;Layer.VerticalCoefficient=.6f;
        }
        public void Enable()=>Call(Layer,"OnEnable");
        public void Emit()=>CinemachineCore.CameraUpdatedEvent.Invoke(Brain);
        public void Close()=>Call(Layer,"OnDisable");
    }
    private static void Scenario(string name,Action body){body();scenarios++;Console.WriteLine("PARALLAX_LAYER PASS "+name);}
    public static void Run()
    {
        Scenario("FinalOutputOnlyNoAccumulationAndNoSize",()=>{
            var f=new Fixture();f.Enable();
            f.Brain.transform.position=new Vector3(1000,1000,0);
            f.Brain.OutputCamera.transform.position=new Vector3(10,-5,-10);
            Near(f.Layer.transform.position,new Vector3(10,20,3),"no Update/LateUpdate write before final event");
            f.Emit();Near(f.Layer.transform.position,new Vector3(17,18,3),"read final output transform");
            f.Emit();Near(f.Layer.transform.position,new Vector3(17,18,3),"absolute placement no accumulation");
            f.Brain.OutputCamera.orthographicSize=3;f.Emit();
            Near(f.Layer.transform.position,new Vector3(17,18,3),"Size has no effect");
            f.Brain.OutputCamera.transform.position=f.Origin.position;f.Emit();
            Near(f.Layer.transform.position,new Vector3(10,20,3),"return to origin");
            f.Close();
        });
        Scenario("DisableRestoreAndFreshEnable",()=>{
            var f=new Fixture();f.Enable();f.Brain.OutputCamera.transform.position=new Vector3(10,0,0);f.Emit();
            f.Close();Near(f.Layer.transform.position,new Vector3(10,20,3),"disable restores");
            Check(CinemachineCore.CameraUpdatedEvent.ListenerCount==0,"unsubscribe on disable");
            f.Layer.transform.position=new Vector3(20,30,4);f.Enable();f.Emit();
            Near(f.Layer.transform.position,new Vector3(27,30,4),"fresh enable captures new authored position");
            f.Close();Near(f.Layer.transform.position,new Vector3(20,30,4),"fresh baseline restored");
        });
        Scenario("EditModeAndRuntimeExit",()=>{
            var f=new Fixture();Application.isPlaying=false;f.Enable();f.Emit();
            Near(f.Layer.transform.position,new Vector3(10,20,3),"edit mode untouched");
            Check(CinemachineCore.CameraUpdatedEvent.ListenerCount==0,"no edit subscription");
            Application.isPlaying=true;f.Enable();f.Brain.OutputCamera.transform.position=new Vector3(10,0,0);f.Emit();
            Application.isPlaying=false;f.Emit();
            Near(f.Layer.transform.position,new Vector3(10,20,3),"exit guard restores even after playing flag clears");
            Check(CinemachineCore.CameraUpdatedEvent.ListenerCount==0,"exit guard unsubscribes");f.Close();
        });
        Scenario("QuitDestroyAndRepeatedEnable",()=>{
            var f=new Fixture();f.Enable();f.Enable();
            Check(CinemachineCore.CameraUpdatedEvent.ListenerCount==1,"repeated enable has one listener");
            f.Brain.OutputCamera.transform.position=new Vector3(10,0,0);f.Emit();
            Call(f.Layer,"OnApplicationQuit");Near(f.Layer.transform.position,new Vector3(10,20,3),"quit restores");
            Call(f.Layer,"OnDestroy");f.Close();
            Check(CinemachineCore.CameraUpdatedEvent.ListenerCount==0,"cleanup is idempotent");
        });
        Scenario("BrainIdentityAndInvalidReferences",()=>{
            var f=new Fixture();f.Enable();var other=new CinemachineBrain {OutputCamera=new Camera()};
            other.OutputCamera.transform.position=new Vector3(100,100,0);
            CinemachineCore.CameraUpdatedEvent.Invoke(other);
            Near(f.Layer.transform.position,new Vector3(10,20,3),"ignore unrelated brain");
            f.Brain.OutputCamera.transform.position=new Vector3(10,0,0);f.Emit();
            f.Layer.HorizontalCoefficient=float.NaN;f.Emit();
            Near(f.Layer.transform.position,new Vector3(10,20,3),"invalid coefficients restore baseline");
            f.Layer.HorizontalCoefficient=.3f;f.Layer.ParallaxOrigin=null;f.Emit();
            Near(f.Layer.transform.position,new Vector3(10,20,3),"missing origin stays authored");
            f.Layer.ParallaxOrigin=f.Origin;f.Origin.parent=f.Layer.transform;f.Emit();
            Near(f.Layer.transform.position,new Vector3(10,20,3),"origin in own layer cannot feed back");
            f.Origin.parent=null;f.Brain.OutputCamera.transform.parent=f.Layer.transform;f.Emit();
            Near(f.Layer.transform.position,new Vector3(10,20,3),"cannot move own output camera indirectly");f.Close();
        });
        Scenario("ParentSpaceAndChildrenPreserved",()=>{
            var f=new Fixture();var parent=new Transform {localPosition=new Vector3(11,4,1),localScale=new Vector3(2,3,1)};
            f.Layer.transform.parent=parent;f.Layer.transform.localPosition=new Vector3(5,7,2);
            var local=f.Layer.transform.localPosition;var world=f.Layer.transform.position;
            var child=new Transform {parent=f.Layer.transform,localPosition=new Vector3(1,2,3)};
            var childLocal=child.localPosition;f.Enable();
            f.Brain.OutputCamera.transform.position=new Vector3(10,0,0);f.Emit();
            Near(f.Layer.transform.position,world+new Vector3(7,0,0),"correction remains world-space under scaled parent");
            Check(child.localPosition==childLocal,"child authored local position untouched");
            f.Close();Check(f.Layer.transform.localPosition==local,"restore exact authored local position");
            Check(child.localPosition==childLocal,"cleanup does not rewrite child local position");
        });
        Scenario("CapturedEventInstanceIsReleased",()=>{
            var f=new Fixture();f.Enable();var original=CinemachineCore.CameraUpdatedEvent;
            CinemachineCore.CameraUpdatedEvent=new CinemachineCore.BrainEvent();f.Close();
            Check(original.ListenerCount==0&&CinemachineCore.CameraUpdatedEvent.ListenerCount==0,"remove listener from actual subscribed event instance");
        });
        Application.isPlaying=true;
        Console.WriteLine("PARALLAX_LAYER PASS scenarios="+scenarios+" assertions="+checks);
    }
}
