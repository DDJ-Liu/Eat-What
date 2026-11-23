using System;
using System.IO;
using EatWhat.Narrative.Core;
using EatWhat.Tools.Security;
using UnityEngine;

namespace EatWhat.Narrative.Unity
{
    /// <summary>Scene lifecycle adapter. Original19 supplies references; no scene/asset search is performed.</summary>
    [DisallowMultipleComponent]
    public sealed class NarrativeDialogueService : MonoBehaviour
    {
        [SerializeField] NarrativeRuntimeEntry runtimeEntry;
        [SerializeField] NarrativeDialogueBoxView boxView;
        [SerializeField] NarrativeSceneDimmer sceneDimmer;
        [SerializeField] NarrativeDialogueComposition presentationComposition = null;
        [SerializeField, Tooltip("A component implementing INarrativeDialogueLock. The service does not depend on LongLoop.")]
        MonoBehaviour operationLockProvider;
        NarrativeDialogueSession session;
        public bool IsRunning { get { return session != null && session.IsRunning; } }
        public string DialogueId { get { return session == null ? null : session.DialogueId; } }
        public string InitiatorId { get { return session == null ? null : session.InitiatorId; } }
        public DialoguePresentationSnapshot CurrentSnapshot { get { return session == null ? null : session.CurrentSnapshot; } }
        public string CurrentSpeakerId { get { return session == null ? null : session.CurrentSpeakerId; } }
        public NarrativeTalkedPersistence Talked { get { return session == null ? null : session.Talked; } }
        public event Action<NarrativeDialogueEvent> DialogueStarted;
        public event Action<NarrativeDialogueEvent> DialogueEnded;
        public event Action<NarrativeDialogueLineEvent> LineStarted;
        public event Action<NarrativeDialogueEvent> ConversationEnded;

        bool EnsureSession()
        {
            if (presentationComposition != null && !presentationComposition.isActiveAndEnabled) return false;
            if (session != null) return true;
            if (presentationComposition != null)
            {
                var box = presentationComposition.BoxView; var dim = presentationComposition.SceneDimmer;
                if (box == null || dim == null || presentationComposition.HasOwner ||
                    (boxView != null && boxView != box) || (sceneDimmer != null && sceneDimmer != dim))
                { Warn("COMPOSITION_BINDINGS_MISMATCH: assign the same Box View / Scene Dimmer to the prefab composition and service."); return false; }
                boxView = box; sceneDimmer = dim;
            }
            var operationLock = operationLockProvider as INarrativeDialogueLock;
            if (runtimeEntry == null || boxView == null || sceneDimmer == null || operationLock == null) return false;
            var talked = NarrativeTalkedPersistence.CreateForCurrentBuild(Path.Combine(Application.persistentDataPath, "saves"),
                new ObfuscatedStaticKeyProvider(), Warn);
            session = new NarrativeDialogueSession(() =>
            {
                INarrativeDataDirectory data;
                return runtimeEntry != null && runtimeEntry.TryGetDirectory(out data) ? data : null;
            }, (dialogue, policy) => runtimeEntry.ResolveForService(dialogue, policy), boxView, sceneDimmer, operationLock, talked, Warn, presentationComposition, id => runtimeEntry.GetCharacterPortraitSprite(id));
            boxView.Configure(session.Input, null); // BoxView retains its existing Inspector Toolkit Input Provider.
            boxView.AdvanceRequested += ForwardAdvance;
            session.DialogueStarted += ForwardStarted; session.DialogueEnded += ForwardEnded;
            session.LineStarted += ForwardLineStarted; session.ConversationEnded += ForwardConversationEnded;
            return true;
        }
        public NarrativeServiceStartStatus TryStart(string dialogueId, string initiatorId, out string diagnostic)
        {
            if (!isActiveAndEnabled || !EnsureSession())
            { diagnostic = "SERVICE_UNAVAILABLE: assign runtime, box, dimmer and lock references."; Warn(diagnostic); return NarrativeServiceStartStatus.Unavailable; }
            return session.TryStart(dialogueId, initiatorId, Time.frameCount, out diagnostic);
        }
        void Update()
        { if (EnsureSession()) session.Advance(Time.deltaTime, Time.frameCount); }
        void ForwardAdvance() { if (isActiveAndEnabled && session != null) session.RequestAdvance(Time.frameCount); }
        void ForwardStarted(NarrativeDialogueEvent item) { DialogueStarted?.Invoke(item); }
        void ForwardEnded(NarrativeDialogueEvent item) { DialogueEnded?.Invoke(item); }
        void ForwardLineStarted(NarrativeDialogueLineEvent item) { LineStarted?.Invoke(item); }
        void ForwardConversationEnded(NarrativeDialogueEvent item) { ConversationEnded?.Invoke(item); }
        void Warn(string message) { Debug.LogWarning("[DIALOGUE] " + message); }
        void OnDisable() { session?.Abort(); }
        void OnDestroy()
        {
            if (session == null) return;
            if (boxView != null) boxView.AdvanceRequested -= ForwardAdvance;
            session.Dispose(); session.DialogueStarted -= ForwardStarted; session.DialogueEnded -= ForwardEnded;
            session.LineStarted -= ForwardLineStarted; session.ConversationEnded -= ForwardConversationEnded; session = null;
        }
    }
}
