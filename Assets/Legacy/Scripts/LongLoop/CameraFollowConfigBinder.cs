using EatWhat.LongLoop.Configuration;
using Unity.Cinemachine;
using UnityEngine;

namespace EatWhat.LongLoop
{
    public enum CameraFollowBindingStatus { Inactive, Ready, MissingConfig, WrongTarget, InvalidConfig }

    [DisallowMultipleComponent]
    public class CameraFollowConfigBinder : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera streetCamera;
        [SerializeField] private CinemachinePositionComposer composer;
        private CameraFollowConfig observedConfig;
        private bool subscribed;
        private CinemachinePositionComposer responseComposer;
        private Vector3 responseOriginal,responseTemporary;
        public CinemachineCamera Camera => streetCamera;
        public CinemachinePositionComposer Composer => composer;
        public CameraFollowConfig Config => observedConfig;
        public StreetFollowSettings Settings { get; private set; }
        public CameraFollowBindingStatus Status { get; private set; }
        public int AppliedConfigurations { get; private set; }
        public bool IsReady => Status==CameraFollowBindingStatus.Ready&&isActiveAndEnabled&&
            streetCamera!=null&&streetCamera.isActiveAndEnabled&&composer!=null&&composer.isActiveAndEnabled&&
            ReferenceEquals(composer.VirtualCamera,streetCamera);

        public void Configure(CinemachineCamera camera,CinemachinePositionComposer body)
        {EndStoppedResponse();streetCamera=camera;composer=body;ObserveConfig(true);}
        protected virtual void OnEnable()
        {
            if(!subscribed){StreetConfigs.Changed+=OnContainerChanged;subscribed=true;}
            ObserveConfig(true);
        }
        protected virtual void Start(){if(!IsReady)ObserveConfig(true);}
        protected virtual void OnDisable(){Detach();Status=CameraFollowBindingStatus.Inactive;}
        protected virtual void OnDestroy(){Detach();}
        // Exceptional/missing callback fallback. Normal ownership ends at Body.
        protected virtual void LateUpdate(){EndStoppedResponse();}
        private void Detach()
        {
            EndStoppedResponse();
            if(subscribed)StreetConfigs.Changed-=OnContainerChanged;
            if(observedConfig!=null)observedConfig.Changed-=ApplyConfiguration;
            observedConfig=null;subscribed=false;
        }
        private void OnContainerChanged(){ObserveConfig(false);}
        private void ObserveConfig(bool initialize)
        {
            var current=StreetConfigs.Current?.CameraFollow;
            if(!ReferenceEquals(current,observedConfig))
            {
                if(observedConfig!=null)observedConfig.Changed-=ApplyConfiguration;
                observedConfig=current;
                if(observedConfig!=null)observedConfig.Changed+=ApplyConfiguration;
                initialize=true;
            }
            if(initialize)ApplyConfiguration();
        }
        public static StreetFollowSettings Snapshot(CameraFollowConfig c)=>new StreetFollowSettings {
            DeadZoneWidth=c.DeadZoneWidth,DeadZoneHeight=c.DeadZoneHeight,Lookahead=c.Lookahead,
            LookaheadTime=c.LookaheadTime,MaxHorizontalOffset=c.MaxHorizontalOffset,MaxVerticalOffset=c.MaxVerticalOffset,
            MoveSpeedThreshold=c.MoveSpeedThreshold,RecenterDelay=c.RecenterDelay,RecenterSpeed=c.RecenterSpeed,
            RecenterRampTime=c.RecenterRampTime,RecenterCurve=c.RecenterCurve};
        private void ApplyConfiguration()
        {
            EndStoppedResponse();
            if(!Application.isPlaying||!isActiveAndEnabled){Status=CameraFollowBindingStatus.Inactive;return;}
            if(observedConfig==null){Status=CameraFollowBindingStatus.MissingConfig;return;}
            if(streetCamera==null||composer==null||!ReferenceEquals(composer.VirtualCamera,streetCamera))
            {Status=CameraFollowBindingStatus.WrongTarget;return;}
            var s=Snapshot(observedConfig);
            if(!StreetFollowPipeline.Valid(s)||!StreetFollowPipeline.Finite(observedConfig.DampingX)||
                !StreetFollowPipeline.Finite(observedConfig.DampingY)||observedConfig.DampingX<0||observedConfig.DampingY<0||
                !StreetFollowPipeline.Finite(observedConfig.StoppedSpeedMultiplier)||observedConfig.StoppedSpeedMultiplier<0||
                !StreetFollowPipeline.Finite(StreetFollowPipeline.ToCmDamping(observedConfig.DampingX))||
                !StreetFollowPipeline.Finite(StreetFollowPipeline.ToCmDamping(observedConfig.DampingY)))
            {Status=CameraFollowBindingStatus.InvalidConfig;return;}
            var damping=composer.Damping;
            damping.x=StreetFollowPipeline.ToCmDamping(observedConfig.DampingX);
            damping.y=StreetFollowPipeline.ToCmDamping(observedConfig.DampingY);composer.Damping=damping;
            var composition=composer.Composition;composition.DeadZone.Enabled=false;
            composition.ScreenPosition=Vector2.zero;composer.Composition=composition;
            var lookahead=composer.Lookahead;lookahead.Enabled=false;composer.Lookahead=lookahead;
            composer.TargetOffset=Vector3.zero;
            Settings=s;Status=CameraFollowBindingStatus.Ready;AppliedConfigurations++;
        }
        public void BeginStoppedResponse(float dt)
        {
            EndStoppedResponse();
            if(!IsReady||!StreetFollowPipeline.Finite(dt)||dt<=0||observedConfig.StoppedSpeedMultiplier==1)return;
            float multiplier=observedConfig.StoppedSpeedMultiplier;
            if(!StreetFollowPipeline.Finite(multiplier)||multiplier<0)return;
            var original=composer.Damping;
            if(!StreetFollowPipeline.Finite(original))return;
            var temporary=new Vector3(ResponseDamping(original.x,multiplier,dt),
                ResponseDamping(original.y,multiplier,dt),original.z);
            responseOriginal=original;responseTemporary=temporary;responseComposer=composer;
            composer.Damping=temporary;
        }
        public void EndStoppedResponse()
        {
            var owner=responseComposer;responseComposer=null;
            if(owner!=null&&owner.Damping.Equals(responseTemporary))owner.Damping=responseOriginal;
        }
        private static float ResponseDamping(float original,float multiplier,float dt)
        {
            // Same-state ordinary response: alpha'=min(1,m*alpha). Composer still
            // runs once, owns its cache and precedes Confiner in the native pipe.
            // Keep CM's native Epsilon end snap; do not add a second pose solver.
            double alpha=System.Math.Min(1d,(double)multiplier*Damper.Damp(1f,original,dt));
            if(alpha<=0)return float.MaxValue;
            if(alpha>=1)return 0;
            double time=-StreetFollowPipeline.CmResidualTimeFactor*dt/System.Math.Log(1-alpha);
            return (float)System.Math.Min(float.MaxValue,time);
        }
    }
}
