using System;
using System.Collections.Generic;
using EatWhat.Narrative.Core;
using EatWhat.Narrative.Unity;
using UnityEngine;

namespace EatWhat.LongLoop
{
    [DisallowMultipleComponent]
    public sealed class NarrativeDialogueTrigger : MonoBehaviour, IStreetInteractable
    {
        [SerializeField] NarrativeRuntimeEntry runtimeEntry;
        [SerializeField] NarrativeDialogueService dialogueService;
        [SerializeField] NarrativeActorMarker actorMarker;
        [SerializeField] NpcShell npcShell;
        [SerializeField, Tooltip("This instance's dialogue ID. Scene-state blank values inherit it.")]
        string dialogueId = "";
        [SerializeField, Tooltip("Empty, none, talk or story. none explicitly hides the marker.")]
        string markerValue = "";
        [SerializeField, Tooltip("Trigger template name; empty uses default.")]
        string triggerTemplate = "";
        bool hasActualBinding;
        string actualDialogue, actualMarker;
        readonly List<string> diagnostics = new List<string>();
        public string InstanceDialogueId { get { return dialogueId; } }
        public string InstanceMarkerValue { get { return markerValue; } }
        public string TriggerTemplate { get { return triggerTemplate; } }
        internal NarrativeRuntimeEntry RuntimeEntry { get { return runtimeEntry; } }
        internal NarrativeDialogueService DialogueService { get { return dialogueService; } }
        public bool HasActualBinding { get { return hasActualBinding; } }
        public string ActualDialogueId { get { return hasActualBinding ? actualDialogue : dialogueId; } }
        public string ActualMarkerValue { get { return hasActualBinding ? actualMarker : markerValue; } }
        public IReadOnlyList<string> Diagnostics { get { return diagnostics.AsReadOnly(); } }
        public string InteractionId { get { return actorMarker == null ? "" : actorMarker.CharacterId; } }
        public Transform InteractionFoot { get { return npcShell == null ? null : npcShell.Foot; } }
        public Transform PromptAnchor { get { return npcShell == null ? null : npcShell.PromptAnchor; } }
        public string PromptTextKey { get { return null; } } // NPCs use the selector's one configurable localized talk key.
        public bool IsAvailable
        {
            get
            {
                NarrativeDialogue model;
                return isActiveAndEnabled && dialogueService != null && !dialogueService.IsRunning &&
                    actorMarker != null && actorMarker.gameObject == gameObject && npcShell != null && npcShell.gameObject == gameObject &&
                    InteractionFoot != null && !string.IsNullOrEmpty(InteractionId) && TryGetBoundDialogue(out model) && model.Level == NarrativeDialogueLevel.Normal;
            }
        }
        public double InteractRadius
        {
            get
            {
                DialogueTriggerResolution value; string issue;
                if (runtimeEntry != null && runtimeEntry.TryResolveTrigger(triggerTemplate, out value, out issue)) return value.Snapshot.Get<float>("interactRadius");
                Warn("TRIGGER_UNAVAILABLE: " + triggerTemplate); return 0;
            }
        }
        public void ConfigureReferences(NarrativeRuntimeEntry runtime, NarrativeDialogueService service, NarrativeActorMarker marker, NpcShell shell)
        {
            runtimeEntry = runtime; dialogueService = service; actorMarker = marker; npcShell = shell;
            // Scene dependencies enter generated and preplaced NPCs through this existing binding path.
            var driver = GetComponent<NarrativeHeadMarkerDriver>();
            if (driver != null) driver.ConfigureSceneBindings(runtime, service, this, shell);
        }
        public void SetActualBinding(string stateDialogueId, string stateMarkerValue)
        {
            hasActualBinding = true;
            actualDialogue = string.IsNullOrEmpty(stateDialogueId) ? dialogueId : stateDialogueId;
            actualMarker = string.IsNullOrEmpty(stateMarkerValue) ? markerValue : stateMarkerValue;
        }
        public void ClearActualBinding() { hasActualBinding = false; actualDialogue = actualMarker = null; }
        public bool TryGetBoundDialogue(out NarrativeDialogue model)
        {
            model = null;
            if (string.IsNullOrEmpty(ActualDialogueId) || runtimeEntry == null) return false;
            INarrativeDataDirectory data; string issue;
            if (!runtimeEntry.TryGetDirectory(out data)) return false;
            if (!data.TryGetDialogue(ActualDialogueId, out model, out issue))
            { Warn("DIALOGUE_MISSING: " + ActualDialogueId); return false; }
            if (model.Level == NarrativeDialogueLevel.Ambient)
            { Warn("AMBIENT_NOT_INTERACTABLE: " + ActualDialogueId); return false; }
            return true; // Story stays bound for marker presentation, but IsAvailable rejects key triggering.
        }
        public bool Interact()
        {
            if (!IsAvailable) return false;
            string issue; var status = dialogueService.TryStart(ActualDialogueId, InteractionId, out issue);
            if (status == NarrativeServiceStartStatus.Started) return true;
            Warn("DIALOGUE_START_REJECTED: " + issue); return false;
        }
        void Warn(string message)
        { if (!diagnostics.Contains(message)) { diagnostics.Add(message); Debug.LogWarning("NarrativeDialogueTrigger: " + message, this); } }
    }
}
