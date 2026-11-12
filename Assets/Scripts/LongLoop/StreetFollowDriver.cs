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
            Detach();ResetFlow();binding=explicitBinding;followPoint=point;cameraTarget=target;
            interestConfig=interest;gameState=stateSource;debugger=debug;providerOverride=null;
            if(isActiveAndEnabled)Subscribe();
        }
        public void ConfigureGameState(IStreetChannelState source){providerOverride=source;}
        protected override void OnEnable(){base.OnEnable();ResetFlow();Subscribe();}
        protected virtual void OnDisable(){Detach();ResetFlow();Status=StreetFollowDriverStatus.Inactive;}
        protected override void OnDestroy(){Detach();ResetFlow();base.OnDestroy();}
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
            settings=CameraFollowConfigBinder.Snapshot(current.CameraFollow);
            focus=current.DepthScale.Focus;safeWidth=observedInterest.SafeZoneWidth;gate=current.CameraMode.GateOffsetsInDialogue;
            configReady=StreetFollowPipeline.Valid(settings)&&StreetFollowPipeline.Finite(focus)&&
                StreetFollowPipeline.Finite(safeWidth)&&safeWidth>=0&&safeWidth<=1;
        }
        private void ResetFlow(){binding?.EndStoppedResponse();pipeline=default;lastWriteFrame=-1;prepared=aimSeen=completed=false;LastResult=default;}
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
            if(!Application.isPlaying||!isActiveAndEnabled){Status=StreetFollowDriverStatus.Inactive;return;}
            if(binding!=null&&binding.Status==CameraFollowBindingStatus.InvalidConfig)
            {ResetFlow();Status=StreetFollowDriverStatus.InvalidConfig;return;}
            if(!BindingsValid(vcam)){ResetFlow();Status=StreetFollowDriverStatus.MissingBinding;return;}
            if(!CinemachineCore.IsLive(vcam)){ResetFlow();Status=StreetFollowDriverStatus.NonLive;return;}
            if(!configReady){ResetFlow();Status=StreetFollowDriverStatus.InvalidConfig;return;}
            double now=CinemachineCore.CurrentTime;
            if(!StreetFollowPipeline.Finite(now)||now<0||!StreetFollowPipeline.Finite(deltaTime)||
                pipeline.Initialized&&now<pipeline.LastTime)
            {Status=StreetFollowDriverStatus.InvalidClock;return;}
            if(pipeline.Initialized&&(lastWriteFrame==Time.frameCount||now==pipeline.LastTime||deltaTime<=0))
            {Status=StreetFollowDriverStatus.HeldTime;return;}
            if(!state.Lens.Orthographic||!StreetFollowPipeline.Finite(state.Lens.OrthographicSize)||
                !StreetFollowPipeline.Finite(state.Lens.Aspect)||state.Lens.OrthographicSize<=0||state.Lens.Aspect<=0||!followPoint.TryUpdate())
            {Status=StreetFollowDriverStatus.InvalidConfig;return;}
            var provider=providerOverride??gameState as IStreetChannelState;
            if(gameState!=null&&provider==null&&!(gameState is LongLoopPlayerController))
            {Status=StreetFollowDriverStatus.InvalidGameplayState;return;}
            // Inputs are diagnostic/game-state data. Actual foot displacement inside
            // the pure pipeline decides motion, including a blocked WalkArea input.
            Vector2 look=provider?.Input==null?Vector2.zero:provider.Input.LookAxis;
            Vector2 move=provider?.Input!=null?new Vector2(provider.Input.Horizontal,provider.Input.Vertical):
                gameState is LongLoopPlayerController player?player.MoveInput:Vector2.zero;
            var input=new StreetFollowInput {Foot=followPoint.Foot.position,Focus=focus,Time=now,DeltaTime=deltaTime,
                VisibleSize=new Vector2(2*state.Lens.OrthographicSize*state.Lens.Aspect,2*state.Lens.OrthographicSize)};
            var clip=new FollowClipContext {Orthographic=true,ProjectionKnown=true,ScreenRight=state.RawOrientation*Vector3.right,
                OrthoSize=state.Lens.OrthographicSize,Aspect=state.Lens.Aspect,SafeZoneWidth=safeWidth,
                GateOffsetsInDialogue=gate,DialogueOrCinematic=provider!=null&&provider.DialogueOrCinematic};
            var result=StreetFollowPipeline.Evaluate(pipeline,input,settings,clip);
            if(!result.Valid){Status=StreetFollowDriverStatus.InvalidConfig;return;}
            Vector3 previousTarget=pipeline.Target;
            pipeline=result.State;LastResult=result;cameraTarget.position=result.Target;
            lastWriteFrame=Time.frameCount;unchecked{EvaluationSequence++;}AppliedEvaluations++;
            prepared=true;Status=StreetFollowDriverStatus.Applied;
            if(debugger!=null&&debugger.isActiveAndEnabled)debugger.Capture(result,Status.ToString(),(float)now,
                EvaluationSequence,state.Lens.OrthographicSize,look,move);
            if(result.GoalStep.Status==FollowStepStatus.TeleportReset)
            {
                notifyingWarp=true;
                try{vcam.OnTargetObjectWarped(cameraTarget,result.Target-previousTarget);}
                finally{notifyingWarp=false;}
            }
            if(result.GoalStep.Advances&&!result.GoalStep.Moving&&vcam.PreviousStateIsValid)
                binding.BeginStoppedResponse(deltaTime);
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
        {ResetFlow();return base.OnTransitionFromCamera(from,up,dt);}
    }
}
