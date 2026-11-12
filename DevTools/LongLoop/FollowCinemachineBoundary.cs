// CM lifecycle/Brain/attachment boundary only. The installed Composer and Damper algorithms are not replaced.
using System;
using UnityEngine;
namespace Unity.Cinemachine
{
    [AttributeUsage(AttributeTargets.All)] public class SaveDuringPlayAttribute:Attribute { }
    [AttributeUsage(AttributeTargets.All)] public class HideFoldoutAttribute:Attribute { }
    [AttributeUsage(AttributeTargets.All)] public class FoldoutWithEnabledButtonAttribute:Attribute { }
    [AttributeUsage(AttributeTargets.All)] public class DelayedVectorAttribute:Attribute { }
    [AttributeUsage(AttributeTargets.All)] public class CameraPipelineAttribute:Attribute { public CameraPipelineAttribute(CinemachineCore.Stage s){} }
    [AttributeUsage(AttributeTargets.All)] public class RequiredTargetAttribute:Attribute
    { public enum RequiredTargets{Tracking} public RequiredTargetAttribute(RequiredTargets r){} }
    public static class Documentation { public const string BaseURL="offline:"; }
    public static class UnityVectorExtensions
    {
        public const float Epsilon=.0001f;
        public static Vector3 ProjectOntoPlane(this Vector3 v,Vector3 normal)=>v-normal*Vector3.Dot(v,normal);
    }
    public struct LensSettings
    { public bool Orthographic;public float OrthographicSize,Aspect,FieldOfView; }
    public struct CameraState
    {
        [Flags] public enum BlendHints{None=0,InheritPosition=1}
        public LensSettings Lens;
        public Vector3 RawPosition,PositionCorrection,ReferenceLookAt,ReferenceUp;
        public Quaternion RawOrientation;
        public BlendHints BlendHint;
        public bool HasLookAt()=>false;
        public Vector3 GetCorrectedPosition()=>RawPosition+PositionCorrection;
        public Vector3 GetFinalPosition()=>GetCorrectedPosition();
        public Quaternion GetCorrectedOrientation()=>RawOrientation;
    }
    public interface ICinemachineCamera {CameraState State{get;} }
    public partial class CinemachineVirtualCameraBase:MonoBehaviour,ICinemachineCamera
    {
        public Transform Follow;
        public int DampCalls,WarpCalls;
        public CinemachineComponentBase Body;
        public void OnTargetObjectWarped(Transform target,Vector3 delta)
        {WarpCalls++;Body?.OnTargetObjectWarped(target,delta);if(Extensions!=null)foreach(var e in Extensions)e.OnTargetObjectWarped(this,target,delta);}

        public bool PreviousStateIsValid,FollowTargetChanged,Live=true,Blending;
        public float FollowTargetAttachment=1;
        public CameraState State {get;set;}
        public object ParentCamera=>null;
        public void PostAll(CinemachineCore.Stage stage,ref CameraState state,float dt)
            =>InvokePostPipelineStageCallback(this,stage,ref state,dt);
        public Vector3 DetachedFollowTargetDamp(Vector3 delta,Vector3 time,float dt)
        {
            if(FollowTargetAttachment!=1f)throw new InvalidOperationException("Blend attachment is a native ENGINE check.");
            DampCalls++;return Damper.Damp(delta,time,dt);
        }
    }
    public class CinemachineCamera:CinemachineVirtualCameraBase { public LensSettings Lens=>State.Lens; }
    public static partial class CinemachineCore
    {
        public enum Stage{Body,Aim,Noise,Finalize}
        public static float CurrentTime=>Time.time;
        public static int CurrentUpdateFrame;
        public static bool IsLive(ICinemachineCamera camera)=>((CinemachineVirtualCameraBase)camera).Live;
        public static bool IsLiveInBlend(ICinemachineCamera camera)=>((CinemachineVirtualCameraBase)camera).Blending;
    }
    public abstract class CinemachineExtension:MonoBehaviour
    {
        protected const float Epsilon=UnityVectorExtensions.Epsilon;
        public CinemachineVirtualCameraBase ComponentOwner=>transform.GetComponent<CinemachineVirtualCameraBase>();
        protected virtual void Awake(){ConnectToVcam(true);}
        protected virtual void OnEnable(){}
        protected virtual void OnDestroy(){ConnectToVcam(false);}
        protected virtual void ConnectToVcam(bool connect)
        {if(ComponentOwner!=null){if(connect)ComponentOwner.AddExtension(this);else ComponentOwner.RemoveExtension(this);}}
        public virtual void PrePipelineMutateCameraStateCallback(CinemachineVirtualCameraBase c,ref CameraState s,float dt){}
        protected virtual void PostPipelineStageCallback(CinemachineVirtualCameraBase c,CinemachineCore.Stage stage,ref CameraState s,float dt){}
        public void InvokePostPipelineStageCallback(CinemachineVirtualCameraBase c,CinemachineCore.Stage stage,ref CameraState s,float dt)
            =>PostPipelineStageCallback(c,stage,ref s,dt);
        public virtual void OnTargetObjectWarped(CinemachineVirtualCameraBase c,Transform t,Vector3 delta){}
        public virtual void ForceCameraPosition(CinemachineVirtualCameraBase c,Vector3 p,Quaternion q){}
        public virtual bool OnTransitionFromCamera(ICinemachineCamera c,Vector3 up,float dt)=>false;
    }
    // The installed Confiner Post callback is compiled in the generated companion.
    // Only cache/baked polygon queries are substituted: a declared axis-aligned feasible
    // centre rectangle. This proves callback ordering/correction, never real scene baking.
    public partial class CinemachineConfiner2D:CinemachineExtension
    {
        public int BodyCallbacks;
        public float Damping,SlowingDistance;
        public float MinX=-2,MaxX=2,MinY=-2,MaxY=2;
        public object BoundingShape2D,OversizeWindow;
        private const float k_CornerAngleThreshold=10;
        private ShapeCache m_ShapeCache=new ShapeCache();
        private class MatrixBoundary {public Vector3 lossyScale=Vector3.one;public Vector3 MultiplyPoint3x4(Vector3 p)=>p;}
        private class Baked {public bool IsValid()=>true;}
        private class Oven {public Baked GetBakedSolution(float height)=>new Baked();}
        private class ShapeCache
        {
            public float AspectRatio;public MatrixBoundary DeltaWorldToBaked=new MatrixBoundary();public Oven ConfinerOven=new Oven();
            public bool ValidateCache(object shape,object oversize,float aspect,out bool changed){changed=false;return true;}
        }
        private class VcamExtraState {public Baked BakedSolution;public float FrustumHeight;public Vector3 PreviousCameraPosition,PreviousDisplacement,DampedDisplacement;}
        private readonly System.Collections.Generic.Dictionary<CinemachineVirtualCameraBase,object> extraStates=new System.Collections.Generic.Dictionary<CinemachineVirtualCameraBase,object>();
        private T GetExtraState<T>(CinemachineVirtualCameraBase c) where T:new()
        {BodyCallbacks++;if(!extraStates.ContainsKey(c))extraStates[c]=new T();return (T)extraStates[c];}
        private float CalculateHalfFrustumHeight(LensSettings lens,float z)=>lens.OrthographicSize;
        private Vector3 ConfinePoint(Vector3 p,VcamExtraState extra,Vector3 fwd)
            =>new Vector3(Math.Max(MinX,Math.Min(MaxX,p.x)),Math.Max(MinY,Math.Min(MaxY,p.y)),p.z);
        private float GetDistanceFromEdge(Vector3 p,Vector3 dir,float slowing,VcamExtraState extra,Vector3 fwd)
            =>Math.Min(slowing,Math.Max(0,Math.Min(Math.Min(p.x-MinX,MaxX-p.x),Math.Min(p.y-MinY,MaxY-p.y))));
        public void Boot(Transform owner){transform=owner;base.Awake();}
        public void Reconnect(){ConnectToVcam(true);}
    }
    public abstract class CinemachineComponentBase:MonoBehaviour
    {
        protected const float Epsilon=UnityVectorExtensions.Epsilon;
        public CinemachineVirtualCameraBase VirtualCamera;
        public Transform FollowTarget=>VirtualCamera.Follow;
        public Vector3 FollowTargetPosition=>FollowTarget.position;
        public Quaternion FollowTargetRotation=>FollowTarget.rotation;
        public abstract bool IsValid{get;}
        public abstract CinemachineCore.Stage Stage{get;}
        public virtual bool BodyAppliesAfterAim=>false;
        public virtual void MutateCameraState(ref CameraState s,float dt){}
        public virtual void OnTargetObjectWarped(Transform target,Vector3 delta){}
        public virtual void ForceCameraPosition(Vector3 p,Quaternion q){}
        public virtual float GetMaxDampTime()=>0;
        public virtual bool OnTransitionFromCamera(ICinemachineCamera c,Vector3 up,float dt)=>false;
    }
    public class CinemachineFreeLookModifier
    {
        public interface IModifiablePositionDamping{Vector3 PositionDamping{get;set;}}
        public interface IModifiableDistance{float Distance{get;set;}}
        public interface IModifiableComposition{ScreenComposerSettings Composition{get;set;}}
    }
}

namespace EatWhat.LongLoop { public partial class LongLoopPlayerController:UnityEngine.MonoBehaviour { public UnityEngine.Vector2 MoveInput {get;set;} } }
