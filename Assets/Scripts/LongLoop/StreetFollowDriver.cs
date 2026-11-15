using EatWhat.LongLoop.Configuration;
using EatWhat.Toolkit.Input;
using Unity.Cinemachine;
using UnityEngine;

namespace EatWhat.LongLoop
{
    public enum StreetFollowDriverStatus { Inactive, MissingBinding, NonLive, InvalidConfig, InvalidClock, InvalidGameplayState, HeldTime, Applied }

    [DisallowMultipleComponent]
    public class StreetFollowDriver : CinemachineExtension
    {
        [SerializeField] private CameraFollowConfigBinder binding = null;
        [SerializeField] private StreetFollowPoint followPoint = null;
        [SerializeField] private Transform cameraTarget = null;
        [SerializeField] private CameraInterestConfig interestConfig = null;
        [SerializeField] private MonoBehaviour gameState = null;
        [SerializeField] private StreetCameraDebug debugger = null;
        private StreetConfigSet configSet;
        private CameraInterestConfig observedInterest;
        private StreetFollowSettings settings;
        private StreetFollowState pipeline;
        private StreetDynamicZoomSettings zoomSettings;
        private StreetLookFloatSettings lookSettings;
        private StreetVerticalOffsetSettings verticalSettings;
        private StreetDynamicZoomState zoomState;
        private StreetLookFloatState lookFloatState;
        private StreetLookFloatWorldState lookWorldState;
        private StreetVerticalOffsetState verticalState;
        private float lastRatio=1,lastEffectiveSize,lookMaxDistance,lookMaxSpeed;
        private bool freezeOnOverride,lensSizeKnown,dynamicLensOwned;
        private CinemachineConfiner2D lensConfiner;
        public StreetDynamicZoomState ZoomState=>zoomState;
        public StreetLookFloatState LookFloatState=>lookFloatState;
        public Vector2 LookWorldOffset=>lookWorldState.Value;
        public StreetVerticalOffsetState VerticalOffsetState=>verticalState;
        public StreetDynamicZoomResult LastZoomResult {get;private set;}
        public float LastRatio=>lastRatio;
        public float LastSize {get;private set;}
        private IStreetChannelState providerOverride;
        private float focus,safeWidth;
        private bool gate,subscribed,configReady,prepared,aimSeen,completed,notifyingWarp;
        private int lastWriteFrame=-1;
        public StreetFollowDriverStatus Status { get; private set; }
        public StreetFollowResult LastResult { get; private set; }
        public StreetFollowState PipelineState => pipeline;
        public CameraFollowConfigBinder Binding => binding;
        public StreetFollowPoint FollowPoint => followPoint;
        public Transform CameraTarget => cameraTarget;
        public CameraInterestConfig InterestConfig => interestConfig;
        public ulong EvaluationSequence { get; private set; }
        public int AppliedEvaluations { get; private set; }
        public delegate void EvaluationHandler(StreetFollowDriver source,CinemachineVirtualCameraBase owner,
            ulong sequence,CameraState snapshot,float deltaTime);
        public event EvaluationHandler EvaluationCompleted;

        public void Configure(CameraFollowConfigBinder explicitBinding,StreetFollowPoint point,Transform target,
            CameraInterestConfig interest,MonoBehaviour stateSource=null,StreetCameraDebug debug=null)
        {
            ReleaseLens();Detach();ResetFlow();binding=explicitBinding;followPoint=point;cameraTarget=target;
            interestConfig=interest;gameState=stateSource;debugger=debug;providerOverride=null;
            if(isActiveAndEnabled)Subscribe();
        }
        public void ConfigureGameState(IStreetChannelState source){providerOverride=source;}
        protected override void OnEnable(){base.OnEnable();ResetFlow();Subscribe();}
        protected virtual void OnDisable(){ReleaseLens();Detach();ResetFlow();Status=StreetFollowDriverStatus.Inactive;}
        protected override void OnDestroy(){ReleaseLens();Detach();ResetFlow();base.OnDestroy();}
        private void Subscribe()
        {
            if(!subscribed){StreetConfigs.Changed+=ConfigurationChanged;subscribed=true;}
            if(!ReferenceEquals(observedInterest,interestConfig))
            {
                if(observedInterest!=null)observedInterest.Changed-=ConfigurationChanged;
                observedInterest=interestConfig;
                if(observedInterest!=null)observedInterest.Changed+=ConfigurationChanged;
            }
            ConfigurationChanged();
        }
        private void Detach()
        {
            if(subscribed)StreetConfigs.Changed-=ConfigurationChanged;
            if(observedInterest!=null)observedInterest.Changed-=ConfigurationChanged;
            subscribed=false;observedInterest=null;configSet=null;configReady=false;
        }
        private void ConfigurationChanged()
        {
            var current=StreetConfigs.Current;
            if(!ReferenceEquals(configSet?.CameraFollow,current?.CameraFollow)||
                !ReferenceEquals(configSet?.DepthScale,current?.DepthScale))ResetFlow();
            configSet=current;configReady=false;
            if(current?.CameraFollow==null||current.DepthScale==null||current.CameraMode==null||observedInterest==null)return;
            var c=current.CameraFollow;
            settings=CameraFollowConfigBinder.Snapshot(c);
            zoomSettings=new StreetDynamicZoomSettings {Enabled=c.DynamicZoomEnabled,MinSize=c.OrthoSizeMin,MaxSize=c.OrthoSizeMax,
                ExpandTime=c.ExpandTime,ShrinkDelay=c.ShrinkDelay,ShrinkTime=c.ShrinkTime,ExpandCurve=c.ExpandCurve,ShrinkCurve=c.ShrinkCurve};
            lookSettings=new StreetLookFloatSettings {Enabled=c.LookFloatEnabled,EdgeZoneRatio=c.EdgeZoneRatio,
                InTime=c.LookInTime,OutTime=c.LookOutTime,ResponseCurve=c.LookResponseCurve};
            lookMaxDistance=c.LookMaxDistance;lookMaxSpeed=c.LookMaxSpeed;
            verticalSettings=new StreetVerticalOffsetSettings {SpeedThreshold=c.VerticalSpeedThreshold,
                InTime=c.VerticalOffsetInTime,OutTime=c.VerticalOffsetOutTime,Curve=c.VerticalOffsetCurve};
            freezeOnOverride=c.FreezeOnOverride;
            focus=current.DepthScale.Focus;safeWidth=observedInterest.SafeZoneWidth;gate=current.CameraMode.GateOffsetsInDialogue;
            configReady=StreetFollowPipeline.Valid(settings)&&StreetFollowPipeline.Finite(focus)&&
                StreetFollowPipeline.Finite(safeWidth)&&safeWidth>=0&&safeWidth<=1&&
                (!zoomSettings.Enabled||(StreetDynamicZoom.Valid(zoomSettings)&&Positive(zoomSettings.MinSize)))&&
                StreetLookFloat.Valid(lookSettings)&&StreetFollowPipeline.Finite(lookMaxDistance)&&lookMaxDistance>=0&&
                StreetFollowPipeline.Finite(lookMaxSpeed)&&lookMaxSpeed>=0&&StreetVerticalOffset.Valid(verticalSettings);
        }
        private void ResetFlow(bool preserveZoom=false)
        {
            binding?.EndStoppedResponse();pipeline=default;
            if(!preserveZoom)zoomState=default;
            lookFloatState=default;lookWorldState=default;verticalState=default;lastRatio=1;LastSize=0;LastZoomResult=default;
            lastWriteFrame=-1;prepared=aimSeen=completed=false;LastResult=default;
        }
        private bool BindingsValid(CinemachineVirtualCameraBase vcam)
        {
            if(binding==null||followPoint==null||cameraTarget==null||!binding.IsReady||!followPoint.isActiveAndEnabled||
                followPoint.Foot==null||followPoint.Node==null||!ReferenceEquals(vcam,binding.Camera)||
                !ReferenceEquals(ComponentOwner,vcam)||vcam.Follow!=cameraTarget||cameraTarget.parent!=null)return false;
            return cameraTarget!=transform&&cameraTarget!=vcam.transform&&cameraTarget!=followPoint.Foot&&cameraTarget!=followPoint.Node&&
                !cameraTarget.IsChildOf(followPoint.Foot)&&!cameraTarget.IsChildOf(followPoint.Node)&&
                !followPoint.Foot.IsChildOf(cameraTarget)&&!followPoint.Node.IsChildOf(cameraTarget)&&!vcam.transform.IsChildOf(cameraTarget);
        }
        public override void PrePipelineMutateCameraStateCallback(CinemachineVirtualCameraBase vcam,ref CameraState state,float deltaTime)
        {
            if(binding!=null&&!ReferenceEquals(vcam,binding.Camera))return;
            binding?.EndStoppedResponse();
            prepared=aimSeen=completed=false;
            if(!Application.isPlaying||!isActiveAndEnabled){ReleaseLens();Status=StreetFollowDriverStatus.Inactive;return;}
            if(binding!=null&&binding.Status==CameraFollowBindingStatus.InvalidConfig)
            {ReleaseLens();ResetFlow();Status=StreetFollowDriverStatus.InvalidConfig;return;}
            if(!BindingsValid(vcam)){ReleaseLens();ResetFlow();Status=StreetFollowDriverStatus.MissingBinding;return;}
            if(!CinemachineCore.IsLive(vcam))
            {ReleaseLens();ResetFlow(freezeOnOverride);Status=StreetFollowDriverStatus.NonLive;return;}
            if(!configReady){ReleaseLens();ResetFlow();Status=StreetFollowDriverStatus.InvalidConfig;return;}
            double now=CinemachineCore.CurrentTime;
            if(!StreetFollowPipeline.Finite(now)||now<0||!StreetFollowPipeline.Finite(deltaTime)||
                pipeline.Initialized&&now<pipeline.LastTime)
            {ReleaseLens();Status=StreetFollowDriverStatus.InvalidClock;return;}
            if(pipeline.Initialized&&(lastWriteFrame==Time.frameCount||now==pipeline.LastTime||deltaTime<=0))
            {
                // Cinemachine pulls its authored Lens again before every Pre callback.
                // Reapply only the successful size; do not integrate or rewrite targets.
                float incoming=state.Lens.OrthographicSize;
                if(zoomSettings.Enabled&&zoomState.Initialized&&state.Lens.Orthographic&&Positive(zoomState.Size))
                    SetSize(ref state,(float)zoomState.Size);
                UpdateLensCache(vcam,state.Lens.OrthographicSize,incoming,zoomSettings.Enabled&&zoomState.Initialized);
                Status=StreetFollowDriverStatus.HeldTime;return;
            }
            if(!state.Lens.Orthographic||!Positive(state.Lens.OrthographicSize)||
                !Positive(state.Lens.Aspect)||!followPoint.TryUpdate())
            {ReleaseLens();Status=StreetFollowDriverStatus.InvalidConfig;return;}
            var provider=providerOverride??gameState as IStreetChannelState;
            if(gameState!=null&&provider==null&&!(gameState is LongLoopPlayerController))
            {ReleaseLens();Status=StreetFollowDriverStatus.InvalidGameplayState;return;}
            IGameInput actualInput=provider?.Input;
            if(actualInput==null&&gameState!=null)actualInput=gameState.transform.GetComponentInParent<IGameInput>();
            if(actualInput==null)actualInput=followPoint.Foot.GetComponentInParent<IGameInput>();
            GameInputDevice lookDevice=actualInput==null?GameInputDevice.Keyboard:actualInput.CurrentDevice;
            Vector2 look=actualInput==null?Vector2.zero:actualInput.LookAxis;
            Vector2 move=actualInput!=null?new Vector2(actualInput.Horizontal,actualInput.Vertical):
                gameState is LongLoopPlayerController player?player.MoveInput:Vector2.zero;
            bool dialogue=provider!=null&&provider.DialogueOrCinematic;
            Vector3 foot=followPoint.Foot.position;
            bool moving=zoomState.Initialized&&zoomState.Moving;double speedY=0;
            if(pipeline.Initialized)
            {
                double elapsed=now-pipeline.LastTime,dx=(double)foot.x-pipeline.PreviousFoot.x,dy=(double)foot.y-pipeline.PreviousFoot.y;
                moving=System.Math.Sqrt(dx*dx+dy*dy)/elapsed>settings.MoveSpeedThreshold;speedY=dy/elapsed;
            }
            float channelDt=pipeline.Initialized?deltaTime:0;
            bool lookTriggered;
            StreetDynamicZoomResult zoom;StreetLookFloatResult floating;StreetVerticalOffsetResult vertical;
            if(!EvaluateChannels(false,moving,speedY,look,move.y,dialogue,channelDt,lookDevice==GameInputDevice.Gamepad,out zoom,out floating,out vertical,out lookTriggered))
            {ReleaseLens();Status=StreetFollowDriverStatus.InvalidConfig;return;}
            var originalLens=state.Lens;
            if(zoom.Size.HasValue)SetSize(ref state,zoom.Size.Value);
            var input=new StreetFollowInput {Foot=foot,Focus=focus,Time=now,DeltaTime=deltaTime,SuspendReturn=lookTriggered};
            var clip=new FollowClipContext {Orthographic=true,ProjectionKnown=true,ScreenRight=state.RawOrientation*Vector3.right,
                OrthoSize=state.Lens.OrthographicSize,Aspect=state.Lens.Aspect,SafeZoneWidth=safeWidth,
                GateOffsetsInDialogue=gate,DialogueOrCinematic=dialogue};
            var frame=new StreetFollowCompositionInput {DynamicZoomEnabled=zoomSettings.Enabled,
                Size=state.Lens.OrthographicSize,MaxSize=zoomSettings.MaxSize,PreviousRatio=lastRatio,
                LookFloat=floating.Value,VerticalOffset=vertical.Value};
            var worldLook=LimitWorldLook(frame,false,channelDt);
            if(!worldLook.Valid){state.Lens=originalLens;ReleaseLens();Status=StreetFollowDriverStatus.InvalidConfig;return;}
            frame.LookFloatIsWorld=true;frame.WorldLookFloat=worldLook.Value;
            var composition=StreetFollowComposition.Evaluate(pipeline,input,settings,clip,frame);
            if(composition.Valid&&composition.Follow.GoalStep.Status==FollowStepStatus.TeleportReset)
            {
                // Discard this frame's speculative channel steps and restart all three.
                if(!EvaluateChannels(true,false,0,look,move.y,dialogue,0,lookDevice==GameInputDevice.Gamepad,out zoom,out floating,out vertical,out lookTriggered))
                {state.Lens=originalLens;ReleaseLens();Status=StreetFollowDriverStatus.InvalidConfig;return;}
                state.Lens=originalLens;
                if(zoom.Size.HasValue)SetSize(ref state,zoom.Size.Value);
                frame.Size=state.Lens.OrthographicSize;frame.LookFloat=floating.Value;frame.VerticalOffset=vertical.Value;
                input.SuspendReturn=lookTriggered;
                worldLook=LimitWorldLook(frame,true,0);
                if(!worldLook.Valid){state.Lens=originalLens;ReleaseLens();Status=StreetFollowDriverStatus.InvalidConfig;return;}
                frame.WorldLookFloat=worldLook.Value;
                clip.OrthoSize=frame.Size;
                composition=StreetFollowComposition.Evaluate(pipeline,input,settings,clip,frame);
            }
            if(!composition.Valid)
            {state.Lens=originalLens;ReleaseLens();Status=StreetFollowDriverStatus.InvalidConfig;return;}
            var result=composition.Follow;Vector3 previousTarget=pipeline.Target;
            zoomState=zoom.State;lookFloatState=floating.State;lookWorldState=worldLook.State;verticalState=vertical.State;
            lastRatio=composition.Ratio;LastSize=state.Lens.OrthographicSize;LastZoomResult=zoom;
            UpdateLensCache(vcam,LastSize,originalLens.OrthographicSize,zoom.Size.HasValue);
            pipeline=result.State;LastResult=result;cameraTarget.position=result.Target;
            lastWriteFrame=Time.frameCount;unchecked{EvaluationSequence++;}AppliedEvaluations++;
            prepared=true;Status=StreetFollowDriverStatus.Applied;
            if(debugger!=null&&debugger.isActiveAndEnabled)debugger.Capture(result,Status.ToString(),(float)now,
                EvaluationSequence,LastSize,look,move,lastRatio);
            if(result.GoalStep.Status==FollowStepStatus.TeleportReset)
            {
                notifyingWarp=true;
                try{vcam.OnTargetObjectWarped(cameraTarget,result.Target-previousTarget);}
                finally{notifyingWarp=false;}
            }
            if(result.GoalStep.Advances&&!result.GoalStep.Moving&&vcam.PreviousStateIsValid)
                binding.BeginStoppedResponse(deltaTime);
        }
        private bool EvaluateChannels(bool reset,bool moving,double speedY,Vector2 look,float verticalInput,
            bool dialogue,float dt,bool directLook,out StreetDynamicZoomResult zoom,out StreetLookFloatResult floating,
            out StreetVerticalOffsetResult vertical,out bool lookTriggered)
        {
            // Current effective Target is intent; a fading Value must not prolong suspension.
            floating=StreetLookFloat.Evaluate(reset?default:lookFloatState,new StreetLookFloatInput {
                LookAxis=look,Gated=gate&&dialogue,DirectAxis=directLook,DeltaTime=dt },lookSettings);
            lookTriggered=floating.Valid&&lookSettings.Enabled&&!(gate&&dialogue)&&lookMaxDistance>0&&lookMaxSpeed>0&&
                (floating.Target.x!=0&&settings.MaxHorizontalOffset>0||floating.Target.y!=0&&settings.MaxVerticalOffset>0);
            zoom=StreetDynamicZoom.Evaluate(reset?default:zoomState,new StreetDynamicZoomInput {
                Moving=moving,Frozen=freezeOnOverride&&dialogue,SuspendShrink=lookTriggered,DeltaTime=dt },zoomSettings);
            vertical=StreetVerticalOffset.Evaluate(reset?default:verticalState,new StreetVerticalOffsetInput {
                VerticalInput=verticalInput,ActualVerticalSpeed=speedY,DeltaTime=dt },verticalSettings);
            return zoom.Valid&&(!zoom.Size.HasValue||Positive(zoom.Size.Value))&&floating.Valid&&vertical.Valid;
        }
        private StreetLookFloatWorldResult LimitWorldLook(StreetFollowCompositionInput frame,bool reset,float dt)
        {
            float ratio=frame.DynamicZoomEnabled?(float)((double)frame.Size/frame.MaxSize):1;
            return StreetLookFloat.LimitWorld(reset?default:lookWorldState,frame.LookFloat,
                new Vector2(settings.MaxHorizontalOffset,settings.MaxVerticalOffset),ratio,lookMaxDistance*ratio,lookMaxSpeed,dt);
        }
        private static bool Positive(double value)=>StreetFollowPipeline.Finite(value)&&value>0&&value<=float.MaxValue;
        private static void SetSize(ref CameraState state,float size)
        {var lens=state.Lens;lens.OrthographicSize=size;state.Lens=lens;}
        private void UpdateLensCache(CinemachineVirtualCameraBase owner,float effective,float incoming,bool wroteDynamic)
        {
            var current=owner.GetComponent<CinemachineConfiner2D>();
            if(!ReferenceEquals(lensConfiner,current)){lensConfiner=current;lensSizeKnown=false;}
            bool changed=lensSizeKnown?effective!=lastEffectiveSize:wroteDynamic&&effective!=incoming;
            if(lensConfiner!=null&&changed)lensConfiner.InvalidateLensCache();
            lastEffectiveSize=effective;lensSizeKnown=true;dynamicLensOwned=wroteDynamic;
        }
        private void ReleaseLens()
        {
            // The next native evaluation resumes the camera's authored Lens. The
            // driver never changes that serialized Lens or the Confiner's fields.
            if(dynamicLensOwned&&lensConfiner!=null)lensConfiner.InvalidateLensCache();
            if(dynamicLensOwned)lensSizeKnown=false;
            dynamicLensOwned=false;
        }
        protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam,CinemachineCore.Stage stage,ref CameraState state,float dt)
        {
            if(!prepared||binding==null||!ReferenceEquals(vcam,binding.Camera))return;
            if(stage==CinemachineCore.Stage.Aim)aimSeen=true;
            if(stage!=CinemachineCore.Stage.Body||!aimSeen||completed)return;
            binding.EndStoppedResponse();
            completed=true;EvaluationCompleted?.Invoke(this,vcam,EvaluationSequence,state,dt);
        }
        public override void OnTargetObjectWarped(CinemachineVirtualCameraBase vcam,Transform target,Vector3 delta)
        {
            base.OnTargetObjectWarped(vcam,target,delta);
            if(!notifyingWarp&&binding!=null&&ReferenceEquals(vcam,binding.Camera)&&
                (target==cameraTarget||followPoint!=null&&(target==followPoint.Foot||target==followPoint.Node)))ResetFlow();
        }
        public override bool OnTransitionFromCamera(ICinemachineCamera from,Vector3 up,float dt)
        {ResetFlow(freezeOnOverride);return base.OnTransitionFromCamera(from,up,dt);}
    }
}
