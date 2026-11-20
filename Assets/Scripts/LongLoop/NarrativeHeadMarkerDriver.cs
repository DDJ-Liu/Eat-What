using System;
using System.Collections.Generic;
using EatWhat.Narrative.Core;
using EatWhat.Narrative.Unity;
using UnityEngine;

namespace EatWhat.LongLoop
{
    [DisallowMultipleComponent]
    public sealed class NarrativeHeadMarkerDriver : MonoBehaviour
    {
        [SerializeField] NarrativeRuntimeEntry runtimeEntry = null;
        [SerializeField] NarrativeDialogueService dialogueService = null;
        [SerializeField] NarrativeDialogueTrigger dialogueTrigger = null;
        [SerializeField] NpcShell npcShell = null;
        [SerializeField] NarrativeHeadMarkerView markerView = null;
        NarrativeDialogueService subscribed;
        bool wasDialogue, fading;
        double clock, fadeElapsed;
        readonly HashSet<string> warnings = new HashSet<string>();
        public void Configure(NarrativeRuntimeEntry runtime, NarrativeDialogueService service,
            NarrativeDialogueTrigger trigger, NpcShell shell, NarrativeHeadMarkerView view)
        {
            if (markerView != null) markerView.HideImmediate(); Unsubscribe();
            runtimeEntry = runtime; dialogueService = service; dialogueTrigger = trigger; npcShell = shell; markerView = view;
            clock = fadeElapsed = 0; fading = false; wasDialogue = service != null && service.IsRunning;
            if (isActiveAndEnabled) Subscribe();
        }
        internal void ConfigureSceneBindings(NarrativeRuntimeEntry runtime, NarrativeDialogueService service,
            NarrativeDialogueTrigger trigger, NpcShell shell)
        {
            var view = markerView;
            if (view == null)
                foreach (var candidate in gameObject.GetComponentsInChildren<NarrativeHeadMarkerView>(true))
                { view = candidate; break; }
            // Reapplying identical scene context must not restart bob/fade or duplicate event subscriptions.
            if (runtimeEntry == runtime && dialogueService == service && dialogueTrigger == trigger && npcShell == shell && markerView == view)
            { if (isActiveAndEnabled) Subscribe(); return; }
            Configure(runtime, service, trigger, shell, view);
        }
        void Update() { Advance(Time.deltaTime); }
        public void Advance(double deltaTime)
        {
            if (markerView == null) return;
            if (!isActiveAndEnabled || npcShell == null || dialogueTrigger == null || runtimeEntry == null ||
                npcShell.gameObject != gameObject || dialogueTrigger.gameObject != gameObject)
            { markerView.HideImmediate(); return; }
            if (double.IsNaN(deltaTime) || double.IsInfinity(deltaTime) || deltaTime < 0 || double.IsInfinity(clock + deltaTime))
            { Warn("INVALID_MARKER_TIME"); markerView.HideImmediate(); return; }
            clock += deltaTime;
            if (dialogueService != null && dialogueService.IsRunning)
            { wasDialogue = true; fading = false; fadeElapsed = 0; markerView.HideImmediate(); return; }
            if (wasDialogue) { wasDialogue = false; fading = true; fadeElapsed = 0; }
            // A missing/unloaded record is unknown, not an un-talked record. Avoid a bright startup flash.
            if (dialogueService != null && (dialogueService.Talked == null || !dialogueService.Talked.IsInitialized))
            { markerView.HideImmediate(); return; }
            DialogueTriggerResolution config; string issue;
            if (!runtimeEntry.TryResolveTrigger(dialogueTrigger.TriggerTemplate, out config, out issue))
            { Warn("MARKER_TRIGGER_UNAVAILABLE: " + issue); markerView.HideImmediate(); return; }
            NarrativeDialogue bound;
            NarrativeDialogueLevel? level = dialogueTrigger.TryGetBoundDialogue(out bound) ? (NarrativeDialogueLevel?)bound.Level : null;
            bool talked = dialogueService != null && dialogueService.Talked != null && dialogueService.Talked.HasTalked(dialogueTrigger.ActualDialogueId);
            NarrativeMarkerPresentation presentation;
            try { presentation = NarrativeInteractionRules.ResolveMarker(dialogueTrigger.ActualMarkerValue, level, talked); }
            catch (ArgumentException error) { Warn("INVALID_MARKER: " + error.Message); markerView.HideImmediate(); return; }
            float duration = config.Snapshot.Get<float>("markerFadeBackTime");
            if (fading) fadeElapsed += deltaTime;
            float alpha = !fading || duration == 0 ? 1 : (float)Math.Min(1, fadeElapsed / duration);
            if (alpha == 1) fading = false;
            if (presentation.Kind == NarrativeMarkerKind.None) { markerView.HideImmediate(); return; }
            float amplitude = config.Snapshot.Get<float>("markerBobAmplitude"), period = config.Snapshot.Get<float>("markerBobPeriod");
            float bob = period == 0 ? 0 : (float)(amplitude * Math.Sin(2 * Math.PI * (clock % period) / period));
            Color tint = presentation.Kind == NarrativeMarkerKind.Story ? new Color(1, .75f, .15f, 1) : new Color(1, 1, 1, 1);
            if (presentation.IsGrey)
            { var grey = config.Snapshot.Get<DialogueColorSnapshot>("markerTalkedTint"); tint = new Color(grey.R, grey.G, grey.B, grey.A); }
            markerView.Apply(npcShell.MarkerAnchor, presentation.Kind == NarrativeMarkerKind.Story ? "!" : "…", tint, alpha, bob);
        }
        void Started(NarrativeDialogueEvent value)
        { wasDialogue = true; fading = false; fadeElapsed = 0; if (markerView != null) markerView.HideImmediate(); }
        void Ended(NarrativeDialogueEvent value) { wasDialogue = false; fading = true; fadeElapsed = 0; }
        void Subscribe()
        {
            if (subscribed != null || dialogueService == null) return;
            subscribed = dialogueService; subscribed.DialogueStarted += Started; subscribed.DialogueEnded += Ended;
            if (subscribed.IsRunning) Started(null);
        }
        void Unsubscribe()
        { if (subscribed != null) { subscribed.DialogueStarted -= Started; subscribed.DialogueEnded -= Ended; } subscribed = null; }
        void OnEnable()
        {
            var trigger = GetComponent<NarrativeDialogueTrigger>();
            if (trigger != null && trigger.RuntimeEntry != null)
                ConfigureSceneBindings(trigger.RuntimeEntry, trigger.DialogueService, trigger, GetComponent<NpcShell>());
            else Subscribe();
        }
        void OnDisable() { Unsubscribe(); if (markerView != null) markerView.HideImmediate(); }
        void OnDestroy() { Unsubscribe(); if (markerView != null) markerView.HideImmediate(); }
        void Warn(string message) { if (warnings.Add(message)) Debug.LogWarning("NarrativeHeadMarkerDriver: " + message); }
    }
}
