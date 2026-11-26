using System;
using EatWhat.Narrative.Unity;
using UnityEngine;

namespace EatWhat.LongLoop
{
    /// <summary>NPC component providing actual scene feet/heads to the ambient bubble coordinator.</summary>
    [DisallowMultipleComponent]
    public sealed class NarrativeAmbientBubbleTrigger : MonoBehaviour, INarrativeBubbleWorld
    {
        [SerializeField] private NarrativeRuntimeEntry runtimeEntry = null;
        [SerializeField] private NarrativeTargetRegistry targetRegistry = null;
        [SerializeField] private NarrativeActorMarker actorMarker = null;
        [SerializeField] private NpcShell npcShell = null;
        [SerializeField] private NarrativeBubbleView bubbleView = null;
        [SerializeField, Tooltip("This NPC instance's ambient pool; empty disables emission. Scene state may call SetPoolId.")]
        private string poolId = string.Empty;
        [SerializeField, Tooltip("Optional caller trigger template; empty uses the original default resolver.")]
        private string triggerTemplate = string.Empty;
        [SerializeField] private NarrativeDialogueService dialogueService = null;
        private NarrativeAmbientBubbleDriver driver;
        private string boundNpcId;
        private int reportedDiagnostics;
        public bool SuppressesNewBubbles => dialogueService != null && dialogueService.IsRunning && actorMarker != null &&
            string.Equals(dialogueService.InitiatorId, actorMarker.CharacterId, StringComparison.Ordinal);
        public void ConfigureDialogueService(NarrativeDialogueService service) { dialogueService = service; }
        public string PoolId => poolId;
        public string TriggerTemplate => triggerTemplate;

        /// <summary>Bind scene capabilities before the first Update. Existing models cannot be silently replaced.</summary>
        public bool Configure(NarrativeRuntimeEntry runtime, NarrativeTargetRegistry registry, NarrativeActorMarker marker,
            NpcShell shell, NarrativeBubbleView view, string template = null)
        {
            var normalizedTemplate = template ?? string.Empty;
            if (marker == null || shell == null || marker.gameObject != gameObject || shell.gameObject != gameObject ||
                runtime == null || registry == null || runtime.gameObject.scene != gameObject.scene || registry.gameObject.scene != gameObject.scene ||
                (view != null && view.gameObject.scene != gameObject.scene))
                return false;
            if (driver != null)
                return runtimeEntry == runtime && targetRegistry == registry && actorMarker == marker && npcShell == shell &&
                    bubbleView == view && triggerTemplate == normalizedTemplate && boundNpcId == marker.CharacterId;
            runtimeEntry = runtime; targetRegistry = registry; actorMarker = marker; npcShell = shell;
            bubbleView = view; triggerTemplate = normalizedTemplate; return true;
        }

        public void SetPoolId(string value)
        { poolId = value ?? string.Empty; if (driver != null) driver.ChangePool(poolId); }

        private void Update()
        { EnsureDriver(); if (!SuppressesNewBubbles) driver.Tick(Time.timeAsDouble, count => UnityEngine.Random.Range(0, count)); ReportDiagnostics(); }

        private void EnsureDriver()
        {
            if (driver == null)
            {
                boundNpcId = actorMarker == null ? null : actorMarker.CharacterId;
                driver = new NarrativeAmbientBubbleDriver(boundNpcId, runtimeEntry, this, bubbleView, poolId, triggerTemplate);
            }
        }
        private void ReportDiagnostics()
        {
            while (reportedDiagnostics < driver.Diagnostics.Count)
                Debug.LogWarning("NarrativeAmbientBubbleTrigger: " + driver.Diagnostics[reportedDiagnostics++], this);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public bool DebugForceBubble()
        {
            if (!isActiveAndEnabled) return false;
            EnsureDriver(); if (SuppressesNewBubbles) return false; string issue;
            if (!ValidateNpcBinding(out issue)) { Debug.LogWarning("NarrativeAmbientBubbleTrigger: " + issue, this); return false; }
            bool emitted = driver.DebugForce(Time.timeAsDouble, count => UnityEngine.Random.Range(0, count));
            ReportDiagnostics(); return emitted;
        }
#endif

        public bool TryGetGroundDistance(out double distance, out string diagnostic)
        {
            distance = 0; diagnostic = null;
            if (!ValidateNpcBinding(out diagnostic)) return false;
            Transform player;
            if (!targetRegistry.TryGetPlayer(out player)) { diagnostic = "Registry has no active scene Player foot."; return false; }
            try { distance = GroundSpace.GroundDistance(player.position, npcShell.Foot.position); return true; }
            catch (InvalidOperationException error) { diagnostic = error.Message; return false; }
            catch (ArgumentOutOfRangeException error) { diagnostic = error.Message; return false; }
        }
        private bool ValidateNpcBinding(out string diagnostic)
        {
            diagnostic = null;
            if (actorMarker == null || actorMarker.CharacterId != boundNpcId || npcShell == null ||
                npcShell.gameObject != gameObject || actorMarker.gameObject != gameObject || npcShell.Foot == null ||
                targetRegistry == null || targetRegistry.gameObject.scene != gameObject.scene)
            { diagnostic = "Assign the same NPC root's stable marker/shell/foot and the scene registry; id must not change during this NPC lifetime."; return false; }
            return true;
        }

        public bool TryGetSpeakerHead(string speakerId, out Transform head, out string diagnostic)
        {
            head = null; diagnostic = null; Transform actor;
            if (targetRegistry == null || !targetRegistry.TryGetActor(speakerId, out actor))
            { diagnostic = "Speaker is not registered in this scene."; return false; }
            var shell = actor.GetComponent<NpcShell>();
            var anchor = shell == null ? null : shell.BubbleAnchor;
            if (anchor == null || !anchor.gameObject.activeInHierarchy)
            { diagnostic = "Speaker has no active NpcShell Bubble Anchor (or Head Anchor fallback)."; return false; }
            head = anchor; return true;
        }
    }
}
