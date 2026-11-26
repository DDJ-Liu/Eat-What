using System;
using System.Collections.Generic;
using EatWhat.Narrative.Core;
using EatWhat.Narrative.Unity;
using UnityEngine;

namespace EatWhat.LongLoop
{
    /// <summary>One scene's service-to-street adapter. Core rules own all timers and original facings.</summary>
    [DisallowMultipleComponent]
    public sealed class NarrativeStreetReactionAdapter : MonoBehaviour, INarrativeStreetReactionSource
    {
        [SerializeField] private NarrativeDialogueService dialogueService = null;
        [SerializeField] private NarrativeTargetRegistry targetRegistry = null;
        [SerializeField] private Camera streetCamera = null;
        [SerializeField] private LongLoopPlayerController playerController = null;
        private NarrativeDialogueService subscribedService;
        private NarrativeStreetReactionRules rules;
        private Transform player;
        private LongLoopPlayerController resolvedPlayerController;
        private string playerId, initiatorId;
        // Object identities only: original values and restoration decisions remain solely in Core.
        private readonly Dictionary<string, NpcShell> sampledShells = new Dictionary<string, NpcShell>(StringComparer.Ordinal);
        private bool warned;
        public bool IsReacting => rules != null && rules.IsActive;

        public void Configure(NarrativeDialogueService service, NarrativeTargetRegistry registry, Camera camera)
        { Configure(service, registry, camera, playerController); }
        public void Configure(NarrativeDialogueService service, NarrativeTargetRegistry registry, Camera camera,
            LongLoopPlayerController controller)
        {
            if (dialogueService == service && targetRegistry == registry && streetCamera == camera && playerController == controller) { TrySubscribe(); return; }
            StopRun(); Detach(); dialogueService = service; targetRegistry = registry; streetCamera = camera; playerController = controller;
            if (isActiveAndEnabled) TrySubscribe();
        }
        private void OnEnable() { TrySubscribe(); }
        private void OnDisable() { StopRun(); Detach(); }
        private void OnDestroy() { StopRun(); Detach(); }
        private bool Local(Behaviour item)
        { return item != null && item.isActiveAndEnabled && item.gameObject.scene == gameObject.scene; }
        private bool BindingsReady()
        {
            Transform found;
            if (!Local(dialogueService) || !Local(targetRegistry) || !Local(streetCamera) || !targetRegistry.TryGetPlayer(out found)) return false;
            // Registry keeps the ground point; the nearest owning controller supplies identity/facing.
            LongLoopPlayerController controller = null;
            for (var owner = found; owner != null && controller == null; owner = owner.parent)
                controller = owner.GetComponent<LongLoopPlayerController>();
            if (playerController != null && playerController != controller) return false;
            if (!Local(controller)) return false;
            var marker = controller.GetComponent<NarrativeActorMarker>();
            if (marker == null || string.IsNullOrEmpty(marker.CharacterId)) return false;
            player = found; resolvedPlayerController = controller; playerId = marker.CharacterId; return true;
        }
        private bool TrySubscribe()
        {
            if (!isActiveAndEnabled || !BindingsReady())
            {
                StopRun(); Detach();
                if (!warned) { warned = true; Debug.LogWarning("NarrativeStreetReactionAdapter: Assign this scene's active Dialogue Service, Target Registry, Street Camera and matching Player Controller/actor marker.", this); }
                return false;
            }
            if (subscribedService == dialogueService) return true;
            StopRun(); Detach();
            if (rules == null) rules = new NarrativeStreetReactionRules(this);
            subscribedService = dialogueService;
            subscribedService.DialogueStarted += OnSegment;
            subscribedService.LineStarted += OnLine;
            subscribedService.ConversationEnded += OnConversationEnded;
            if (subscribedService.IsRunning) { StartSegment(); SetSpeaker(subscribedService.CurrentSpeakerId); }
            return true;
        }
        private void Detach()
        {
            if (!ReferenceEquals(subscribedService, null))
            {
                subscribedService.DialogueStarted -= OnSegment;
                subscribedService.LineStarted -= OnLine;
                subscribedService.ConversationEnded -= OnConversationEnded;
            }
            subscribedService = null;
        }
        private void OnSegment(NarrativeDialogueEvent item) { if (BindingsReady()) StartSegment(); }
        private void StartSegment()
        {
            if (dialogueService.CurrentSnapshot == null || rules.IsActive) return;
            initiatorId = dialogueService.InitiatorId;
            NarrativeStreetReactionActor initiator; TryNpc(initiatorId, out initiator);
            Apply(rules.Begin(initiator, PlayerSample()), true);
        }
        private void OnLine(NarrativeDialogueLineEvent item)
        { if (BindingsReady()) { StartSegment(); SetSpeaker(item.SpeakerId); } }
        private void SetSpeaker(string speakerId)
        {
            var snapshot = dialogueService.CurrentSnapshot; if (snapshot == null || !rules.IsActive) return;
            NarrativeStreetReactionActor speaker = null;
            if (!string.IsNullOrEmpty(speakerId))
            {
                if (string.Equals(BaseId(speakerId), BaseId(initiatorId), StringComparison.Ordinal)) TryNpc(initiatorId, out speaker);
                else if (string.Equals(speakerId, BaseId(playerId), StringComparison.Ordinal)) speaker = PlayerSample();
                else TryNpc(speakerId, out speaker);
            }
            Apply(rules.SetSpeaker(speaker, snapshot), false);
        }
        private static string BaseId(string id)
        { if (id == null) return null; int suffix = id.IndexOf('#'); return suffix < 0 ? id : id.Substring(0, suffix); }
        private NarrativeStreetReactionActor PlayerSample()
        { return new NarrativeStreetReactionActor(playerId, player.position.x, resolvedPlayerController.IsFacingRight); }
        private bool TryNpc(string id, out NarrativeStreetReactionActor actor)
        {
            actor = null; Transform target;
            if (string.IsNullOrEmpty(id) || targetRegistry == null || !targetRegistry.TryGetActor(id, out target)) return false;
            var shell = target.GetComponent<NpcShell>();
            if (!Local(shell) || shell.Foot == null || shell.Foot.gameObject.scene != gameObject.scene) return false;
            sampledShells[id] = shell;
            actor = new NarrativeStreetReactionActor(id, shell.Foot.position.x, shell.IsFacingRight, OnScreen(shell.Foot.position)); return true;
        }
        private bool OnScreen(Vector3 point)
        {
            var view = streetCamera.WorldToViewportPoint(point);
            return view.z >= streetCamera.nearClipPlane && view.z <= streetCamera.farClipPlane &&
                view.x >= 0 && view.x <= 1 && view.y >= 0 && view.y <= 1;
        }
        public bool TryGetActor(string id, out NarrativeStreetReactionActor actor)
        {
            if (string.Equals(id, playerId, StringComparison.Ordinal) && player != null && resolvedPlayerController != null)
            { actor = PlayerSample(); return true; }
            return TryNpc(id, out actor);
        }
        public IEnumerable<NarrativeStreetReactionActor> GetCandidates()
        {
            foreach (var marker in targetRegistry.GetActiveActors())
            {
                if (string.Equals(marker.CharacterId, playerId, StringComparison.Ordinal)) continue;
                NarrativeStreetReactionActor actor; if (TryNpc(marker.CharacterId, out actor)) yield return actor;
            }
        }
        private void Apply(IReadOnlyList<NarrativeStreetFacingCommand> commands, bool allowPlayer)
        {
            foreach (var command in commands)
            {
                if (string.Equals(command.ActorId, playerId, StringComparison.Ordinal))
                { if (allowPlayer && resolvedPlayerController != null) resolvedPlayerController.SetDialogueFacing(command.FacingRight); continue; }
                NpcShell shell;
                if (sampledShells.TryGetValue(command.ActorId, out shell) && shell != null) shell.SetFacing(command.FacingRight);
            }
        }
        private void OnConversationEnded(NarrativeDialogueEvent item) { StopRun(); }
        private void StopRun()
        {
            if (rules != null && rules.IsActive) Apply(rules.Abort(), false);
            sampledShells.Clear(); initiatorId = null;
        }
        private void Update()
        {
            if (!TrySubscribe()) return;
            // User decision: same scaled game delta as NarrativeDialogueService; pause means zero.
            if (rules.IsActive) Apply(rules.Advance(Time.deltaTime), false);
        }
    }
}
