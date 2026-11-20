using System.Collections.Generic;
using EatWhat.Narrative.Unity;
using EatWhat.Toolkit.Input;
using UnityEngine;

namespace EatWhat.LongLoop
{
    /// <summary>Owns the camera's gameplay gates. No camera solver, input polling or scene search.</summary>
    [DisallowMultipleComponent]
    public sealed class StreetDialogueGameState : MonoBehaviour, IStreetChannelState
    {
        [SerializeField] NarrativeDialogueService dialogueService;
        [SerializeField, Tooltip("The same real foot Transform assigned to StreetFollowPoint.")]
        Transform playerFoot;
        [SerializeField, Tooltip("Existing player IGameInput component; empty uses the foot's parent chain, as the driver did.")]
        MonoBehaviour inputProvider;
        static readonly IReadOnlyList<ChannelInterest> noInterests = new List<ChannelInterest>().AsReadOnly();
        bool subscribed, dialogueInProgress, motionKnown;
        Vector3 previousFoot, velocity;
        double previousTime;
        public bool DialogueOrCinematic { get { return dialogueInProgress; } }
        public bool CameraOverridden { get { return false; } } // Story/close-up/scene-transition ownership is a later version.
        public IGameInput Input
        { get { return inputProvider as IGameInput ?? (playerFoot == null ? null : playerFoot.GetComponentInParent<IGameInput>()); } }
        public IReadOnlyList<ChannelInterest> NearbyInterests { get { return noInterests; } }
        // Compatibility data only: current driver derives its own speed from the same real foot and CM time.
        public Vector3 VelocityWorld
        {
            get
            {
                if (playerFoot == null) return Vector3.zero;
                double now = Time.time;
                if (!motionKnown || now < previousTime)
                { motionKnown = true; previousFoot = playerFoot.position; previousTime = now; velocity = Vector3.zero; }
                else if (now > previousTime)
                {
                    velocity = (playerFoot.position - previousFoot) / (float)(now - previousTime);
                    previousFoot = playerFoot.position; previousTime = now;
                }
                return velocity;
            }
        }
        public void Configure(NarrativeDialogueService service, Transform foot, MonoBehaviour input = null)
        {
            Unsubscribe(); dialogueService = service; playerFoot = foot; inputProvider = input;
            motionKnown = false; velocity = Vector3.zero;
            if (isActiveAndEnabled) Subscribe();
        }
        void OnEnable() { motionKnown = false; Subscribe(); }
        void OnDisable() { Unsubscribe(); dialogueInProgress = false; motionKnown = false; }
        void OnDestroy() { Unsubscribe(); dialogueInProgress = false; motionKnown = false; }
        void Subscribe()
        {
            dialogueInProgress = dialogueService != null && dialogueService.IsRunning;
            if (subscribed || dialogueService == null) return;
            dialogueService.DialogueStarted += Changed; dialogueService.DialogueEnded += Changed; subscribed = true;
        }
        void Unsubscribe()
        {
            if (subscribed && dialogueService != null)
            { dialogueService.DialogueStarted -= Changed; dialogueService.DialogueEnded -= Changed; }
            subscribed = false;
        }
        void Changed(NarrativeDialogueEvent item)
        { dialogueInProgress = dialogueService != null && dialogueService.IsRunning; }
    }
}
