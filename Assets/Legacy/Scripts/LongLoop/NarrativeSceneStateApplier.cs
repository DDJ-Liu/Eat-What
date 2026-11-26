using System.Collections.Generic;
using EatWhat.Narrative.Core;
using EatWhat.Narrative.Unity;
using UnityEngine;

namespace EatWhat.LongLoop
{
    [DisallowMultipleComponent]
    public sealed class NarrativeSceneStateApplier : MonoBehaviour, INarrativeSceneStateWorld
    {
        [SerializeField] private NarrativeRuntimeEntry runtimeEntry = null;
        [SerializeField] private NarrativeTargetRegistry targetRegistry = null;
        [SerializeField] private GameObject genericNpcPrefab = null;
        [SerializeField] private GameObject placeholderVisualPrefab = null;
        [SerializeField] private NarrativeBubbleView bubbleView = null;
        [SerializeField, Tooltip("Optional parent in this scene; empty uses this object's Transform.")]
        private Transform generatedNpcParent = null;
        [SerializeField] private string triggerTemplate = string.Empty;
        [SerializeField] private bool applyDefaultOnStart = true;
        [SerializeField, Tooltip("Optional existing dialogue service; shares its talked record and refuses switches while running.")]
        private NarrativeDialogueService dialogueService = null;
        private NarrativeTalkedPersistence talkedRecord;
        private GameObject spawnInitialization;
        private NarrativeSceneStateApplication application;
        private INarrativeDataDirectory boundDirectory;
        private readonly Dictionary<GameObject, Actor> actors = new Dictionary<GameObject, Actor>();
        private int reported;
        public string CurrentSceneId => application == null ? null : application.SceneId;
        public string CurrentStateId => application == null ? null : application.StateId;

        public bool TryGetSceneStateInfo(out string sceneId, out string stateId,
            out IReadOnlyList<NarrativeSceneState> states, out string diagnostic)
        {
            sceneId = null; stateId = null; states = null; diagnostic = null; INarrativeDataDirectory directory;
            if (runtimeEntry == null || !runtimeEntry.TryGetDirectory(out directory)) { diagnostic = "Runtime data unavailable."; return false; }
            if (application != null && !ReferenceEquals(boundDirectory, directory)) { diagnostic = "Directory changed; scene lifetime must be recreated."; return false; }
            var scenes = directory as INarrativeSceneStateDirectory; var list = directory as INarrativeSceneStateList;
            NarrativeScene scene;
            if (scenes == null || list == null) { diagnostic = "Scene query capability unavailable."; return false; }
            if (!scenes.TryFindSceneAsset(gameObject.scene.name, out scene, out diagnostic) ||
                !list.TryGetSceneStates(scene.SceneId, out states, out diagnostic)) return false;
            sceneId = scene.SceneId; stateId = CurrentSceneId == sceneId ? CurrentStateId : null; return true;
        }

        public bool Configure(NarrativeRuntimeEntry runtime, NarrativeTargetRegistry registry, GameObject npcPrefab,
            GameObject placeholder, NarrativeBubbleView view, Transform parent = null, string template = null)
        {
            if (application != null) { Warn("End this scene application before changing its bindings."); return false; }
            runtimeEntry = runtime; targetRegistry = registry; genericNpcPrefab = npcPrefab;
            placeholderVisualPrefab = placeholder; bubbleView = view; generatedNpcParent = parent;
            triggerTemplate = template ?? string.Empty; return true;
        }
        public bool ConfigureDialogueState(NarrativeDialogueService service, NarrativeTalkedPersistence record = null)
        {
            if (application != null) { Warn("End this scene application before changing its dialogue bindings."); return false; }
            dialogueService = service; talkedRecord = record; return true;
        }
        private void ClearTalked(IEnumerable<string> ids)
        {
            // Resolve lazily: Start may precede the service's first Update/session creation.
            var record = talkedRecord ?? (dialogueService == null ? null : dialogueService.Talked);
            if (record == null || !record.InitializeWhenAvailable(runtimeEntry != null && runtimeEntry.IsAvailable)) return;
            record.Clear(ids); // Actual01 owner performs its existing immediate SaveNow.
        }
        private void Start() { if (applyDefaultOnStart && application == null) ApplyDefault(); }
        public bool ApplyDefault() { return Apply(null); }
        public bool ApplyState(string stateId)
        { if (string.IsNullOrEmpty(stateId)) { Warn("State id is required."); return false; } return Apply(stateId); }
        private bool Apply(string stateId)
        {
            INarrativeDataDirectory directory;
            if (!Application.isPlaying || !isActiveAndEnabled || runtimeEntry == null || targetRegistry == null ||
                !runtimeEntry.isActiveAndEnabled || !targetRegistry.isActiveAndEnabled ||
                targetRegistry.gameObject.scene != gameObject.scene || runtimeEntry.gameObject.scene != gameObject.scene ||
                (bubbleView != null && bubbleView.gameObject.scene != gameObject.scene) ||
                (generatedNpcParent != null && generatedNpcParent.gameObject.scene != gameObject.scene))
            { Warn("Assign the active scene Runtime/Registry and an optional parent in this scene; runtime only."); return false; }
            if (!runtimeEntry.TryGetDirectory(out directory)) { Warn("Runtime data unavailable."); return false; }
            if (application == null)
            {
                targetRegistry.CollectSceneMarkers();
                boundDirectory = directory; application = new NarrativeSceneStateApplication(directory, this, ClearTalked,
                    () => dialogueService != null && dialogueService.IsRunning);
            }
            if (!ReferenceEquals(boundDirectory, directory)) { Warn("Directory changed; recreate the scene application at an explicit lifetime boundary."); return false; }
            bool result = application.Apply(gameObject.scene.name, stateId);
            while (reported < application.Diagnostics.Count) Warn(application.Diagnostics[reported++]);
            return result;
        }
        public object PlaceholderVisual => placeholderVisualPrefab;
        public bool TryGetVisual(string baseId, out object prefab, out string diagnostic)
        { GameObject value; bool found = runtimeEntry.TryGetCharacterVisualPrefab(baseId, out value, out diagnostic); prefab = value; return found; }
        public bool TryFindActor(string fullId, out INarrativeSceneStateActor actor)
        {
            actor = null; Transform root;
            if (!targetRegistry.TryGetActorForSceneState(fullId, out root)) return false;
            Actor value;
            if (!actors.TryGetValue(root.gameObject, out value))
            { value = new Actor(this, root.gameObject); actors.Add(root.gameObject, value); }
            actor = value; return true;
        }
        public bool TrySpawnActor(string fullId, out INarrativeSceneStateActor actor, out string diagnostic)
        {
            actor = null; diagnostic = null;
            if (genericNpcPrefab == null) { diagnostic = "Generic NPC Prefab is not assigned."; return false; }
            var parent = generatedNpcParent == null ? transform : generatedNpcParent;
            if (spawnInitialization == null)
            {
                spawnInitialization = new GameObject("NpcSpawnInitialization");
                spawnInitialization.SetActive(false); spawnInitialization.transform.SetParent(parent, false);
            }
            // An inactive parent prevents OnEnable registration before the full id is authored.
            var instance = Instantiate(genericNpcPrefab, spawnInitialization.transform, false);
            instance.SetActive(false);
            var marker = instance.GetComponent<NarrativeActorMarker>(); var shell = instance.GetComponent<NpcShell>();
            var trigger = instance.GetComponent<NarrativeAmbientBubbleTrigger>();
            if (marker == null || shell == null || trigger == null || shell.Foot == null || shell.VisualMount == null || shell.HeadAnchor == null)
            { diagnostic = "Generic NPC needs its local marker, shell, trigger and stable mounts."; DiscardGenerated(instance); return false; }
            marker.ConfigureId(fullId);
            if (!trigger.Configure(runtimeEntry, targetRegistry, marker, shell, bubbleView, triggerTemplate))
            { diagnostic = "Generated NPC trigger bindings were refused."; DiscardGenerated(instance); return false; }
            var dialogue = instance.GetComponent<NarrativeDialogueTrigger>();
            if (dialogue != null) dialogue.ConfigureReferences(runtimeEntry, dialogueService, marker, shell);
            instance.transform.SetParent(parent, false);
            var value = new Actor(this, instance); actors.Add(instance, value); actor = value; return true;
        }
        public void DestroyActor(INarrativeSceneStateActor actor)
        { var value = (Actor)actor; if (value.Root != null) { actors.Remove(value.Root); DiscardGenerated(value.Root); } }
        private void DiscardGenerated(GameObject value)
        {
            // Destroy is deferred. Exclude this owned instance from the inactive-marker lookup immediately.
            var marker = value.GetComponent<NarrativeActorMarker>(); if (marker != null) marker.enabled = false;
            value.SetActive(false); Destroy(value);
        }
        private void OnDisable() { EndApplication(); }
        private void OnDestroy() { EndApplication(); }
        private void EndApplication()
        {
            if (application != null) application.Dispose(); application = null; boundDirectory = null; actors.Clear(); reported = 0;
            if (spawnInitialization != null) Destroy(spawnInitialization); spawnInitialization = null;
        }
        private void Warn(string text) { Debug.LogWarning("NarrativeSceneStateApplier: " + text, this); }

        private sealed class Actor : INarrativeSceneStateActor, INarrativeSceneStateDialogueActor
        {
            private readonly NarrativeSceneStateApplier owner;
            internal readonly GameObject Root;
            private readonly NpcShell shell;
            private readonly NarrativeAmbientBubbleTrigger trigger;
            private readonly NarrativeDialogueTrigger dialogue;
            private NpcShell.SceneStateVisuals visuals;
            private Vector3 position;
            private bool active;
            private string pool;
            internal Actor(NarrativeSceneStateApplier owner, GameObject root)
            { this.owner = owner; Root = root; shell = root.GetComponent<NpcShell>(); trigger = root.GetComponent<NarrativeAmbientBubbleTrigger>(); dialogue = root.GetComponent<NarrativeDialogueTrigger>(); }
            public bool IsAlive => Root != null;
            public bool RememberOriginal(out string diagnostic)
            {
                diagnostic = null;
                if (shell == null) { diagnostic = "Preplaced actor has no NpcShell."; return false; }
                visuals = shell.CaptureSceneStateVisuals();
                if (visuals == null) { diagnostic = "Visual Mount invalid or another scene state owner already holds it."; return false; }
                if (trigger != null && !trigger.Configure(owner.runtimeEntry, owner.targetRegistry,
                    Root.GetComponent<NarrativeActorMarker>(), shell, owner.bubbleView, trigger.TriggerTemplate))
                { visuals.Dispose(); visuals = null; diagnostic = "Preplaced trigger has different lifetime bindings; model retained, actor skipped."; return false; }
                if (dialogue != null && owner.dialogueService != null)
                    dialogue.ConfigureReferences(owner.runtimeEntry, owner.dialogueService, Root.GetComponent<NarrativeActorMarker>(), shell);
                position = Root.transform.position; active = Root.activeSelf; pool = trigger == null ? null : trigger.PoolId;
                return true;
            }
            public void RestoreOriginal()
            { Root.transform.position = position; visuals.Restore(); if (trigger != null) trigger.SetPoolId(pool); if (dialogue != null) dialogue.ClearActualBinding(); Root.SetActive(active); }
            public void ReleaseOriginal() { if (visuals != null) visuals.Dispose(); visuals = null; }
            public string ActualDialogueId => dialogue == null ? null : dialogue.ActualDialogueId;
            public void SetDialogueBinding(string id, string marker) { if (dialogue != null) dialogue.SetActualBinding(id, marker); }
            public void SetActive(bool value) { Root.SetActive(value); }
            public bool PlaceAtAnchor(string id, out string diagnostic)
            {
                Vector3 value; diagnostic = null;
                if (!owner.targetRegistry.TryGetAnchorPosition(id, out value)) { diagnostic = "Anchor not registered: " + id; return false; }
                // Move the root so the stable foot, including an authored offset, reaches the anchor.
                if (shell == null || shell.Foot == null) { diagnostic = "NPC Foot is not assigned."; return false; }
                Root.transform.position += value - shell.Foot.position; return true;
            }
            public void SetFacing(bool right) { if (shell != null) shell.SetFacing(right); }
            public void SetVisual(object prefab) { if (shell != null) shell.SetVisual((GameObject)prefab); }
            public void SetPool(string id)
            { if (trigger != null) trigger.SetPoolId(id); else owner.Warn("Actor has no ambient trigger; pool not applied."); }
        }
    }
}
