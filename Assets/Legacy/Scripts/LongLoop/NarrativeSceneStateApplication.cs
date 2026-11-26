using System;
using System.Collections.Generic;
using EatWhat.Narrative.Core;

namespace EatWhat.LongLoop
{
    // Scene object capabilities only. All ordering, selection and ownership decisions are below.
    public interface INarrativeSceneStateActor
    {
        bool IsAlive { get; }
        bool RememberOriginal(out string diagnostic);
        void RestoreOriginal();
        void ReleaseOriginal();
        void SetActive(bool active);
        bool PlaceAtAnchor(string anchorId, out string diagnostic);
        void SetFacing(bool right);
        void SetVisual(object prefab);
        void SetPool(string poolId);
    }
    // Optional capability: old scene actors and their original nine steps remain unchanged.
    public interface INarrativeSceneStateDialogueActor
    {
        void SetDialogueBinding(string dialogueId, string marker);
        string ActualDialogueId { get; }
    }
    public interface INarrativeSceneStateWorld
    {
        bool TryFindActor(string fullId, out INarrativeSceneStateActor actor);
        bool TrySpawnActor(string fullId, out INarrativeSceneStateActor actor, out string diagnostic);
        void DestroyActor(INarrativeSceneStateActor actor);
        bool TryGetVisual(string baseId, out object prefab, out string diagnostic);
        object PlaceholderVisual { get; }
    }

    /// <summary>One scene's immediate state owner. No execution, save, camera or transition state.</summary>
    public sealed class NarrativeSceneStateApplication : IDisposable
    {
        private readonly INarrativeDataDirectory directory;
        private readonly INarrativeSceneStateDirectory sceneDirectory;
        private readonly INarrativeSceneStateWorld world;
        private readonly HashSet<INarrativeSceneStateActor> originals = new HashSet<INarrativeSceneStateActor>();
        private readonly List<INarrativeSceneStateActor> generated = new List<INarrativeSceneStateActor>();
        private readonly List<string> diagnostics = new List<string>();
        private readonly Action<IEnumerable<string>> clearTalked;
        private readonly Func<bool> dialogueInProgress;
        private bool disposed;
        public IReadOnlyList<string> Diagnostics => diagnostics.AsReadOnly();
        public string SceneId { get; private set; }
        public string StateId { get; private set; }

        public NarrativeSceneStateApplication(INarrativeDataDirectory directory, INarrativeSceneStateWorld world,
            Action<IEnumerable<string>> clearTalked = null, Func<bool> dialogueInProgress = null)
        {
            this.directory = directory ?? throw new ArgumentNullException(nameof(directory));
            this.world = world ?? throw new ArgumentNullException(nameof(world));
            sceneDirectory = directory as INarrativeSceneStateDirectory;
            this.clearTalked = clearTalked; this.dialogueInProgress = dialogueInProgress;
        }
        public bool ApplyDefault(string sceneAsset) { return Apply(sceneAsset, null); }
        public bool Apply(string sceneAsset, string stateId)
        {
            if (disposed) return Diagnose("DISPOSED: Scene state application has ended.");
            bool switching = SceneId != null && stateId != null;
            if (switching && dialogueInProgress != null && dialogueInProgress())
                return Diagnose("DIALOGUE_IN_PROGRESS: 对话进行中");
            var resetIds = new HashSet<string>(StringComparer.Ordinal);
            NarrativeScene scene; string issue;
            if (sceneDirectory == null) return Diagnose("MISSING_QUERY: Directory has no scene-state query capability.");
            if (!sceneDirectory.TryFindSceneAsset(sceneAsset, out scene, out issue)) return Diagnose(issue);
            var selectedState = stateId ?? scene.DefaultState;
            IReadOnlyList<NarrativeSceneStatePlacement> rows;
            if (!sceneDirectory.TryGetStatePlacements(scene.SceneId, selectedState, out rows, out issue)) return Diagnose(issue);
            // Invalid requests do not destroy a previously valid scene state.
            ClearGenerated();
            foreach (var actor in originals) if (actor.IsAlive) actor.RestoreOriginal();
            foreach (var row in rows)
            {
                if (row.Kind != "actor") { Diagnose("SKIP_KIND: " + row.Kind + "/" + row.Target); continue; }
                NarrativeCommandTarget target; NarrativeCommandDiagnostic error;
                if (!NarrativeCommandTargetParser.TryParse("actor:" + row.Target, out target, out error))
                { Diagnose("INVALID_ACTOR: " + row.Target + ": " + error.Reason); continue; }
                INarrativeSceneStateActor actor;
                bool created = !world.TryFindActor(target.Identifier, out actor);
                if (created)
                {
                    if (!world.TrySpawnActor(target.Identifier, out actor, out issue)) { Diagnose("SPAWN: " + issue); continue; }
                    generated.Add(actor);
                }
                else if (!originals.Contains(actor))
                {
                    if (!actor.RememberOriginal(out issue)) { Diagnose("BACKUP: " + issue); continue; }
                    originals.Add(actor);
                }
                var binding = actor as INarrativeSceneStateDialogueActor;
                if (binding != null)
                {
                    binding.SetDialogueBinding(row.DialogueId, row.Marker);
                    if (switching && !string.IsNullOrWhiteSpace(binding.ActualDialogueId)) resetIds.Add(binding.ActualDialogueId);
                }
                actor.SetActive(row.Active);
                if (!row.Active) continue;
                if (row.AnchorId.Length != 0)
                {
                    if (!actor.PlaceAtAnchor(row.AnchorId, out issue))
                    { Diagnose("ANCHOR: " + row.Target + ": " + issue); if (created) actor.SetActive(false); }
                }
                else if (created)
                { actor.SetActive(false); Diagnose("NEW_WITHOUT_ANCHOR: " + row.Target); }
                if (row.Facing == "left" || row.Facing == "right") actor.SetFacing(row.Facing == "right");
                else if (row.Facing.Length != 0) Diagnose("INVALID_FACING: " + row.Target + ": " + row.Facing);
                NarrativeCharacter character;
                if (directory.TryGetCharacter(target.BaseId, out character, out issue) && character.StreetVisualPrefab.Length != 0)
                {
                    object prefab;
                    if (world.TryGetVisual(target.BaseId, out prefab, out issue) && prefab != null) actor.SetVisual(prefab);
                    else
                    {
                        Diagnose("MISSING_VISUAL: " + target.BaseId + ": " + issue);
                        if (world.PlaceholderVisual != null) actor.SetVisual(world.PlaceholderVisual);
                        else Diagnose("MISSING_PLACEHOLDER: " + target.BaseId);
                    }
                }
                if (row.AmbientPool.Length != 0) actor.SetPool(row.AmbientPool);
            }
            SceneId = scene.SceneId; StateId = selectedState;
            // Only this target state's successfully applied actor rows contribute, including inactive rows.
            if (switching && resetIds.Count != 0 && clearTalked != null) clearTalked(new List<string>(resetIds));
            return true;
        }
        private void ClearGenerated()
        { foreach (var actor in generated) if (actor.IsAlive) world.DestroyActor(actor); generated.Clear(); }
        private bool Diagnose(string issue)
        { if (!diagnostics.Contains(issue)) diagnostics.Add(issue); return false; }
        public void Dispose()
        {
            if (disposed) return;
            ClearGenerated();
            foreach (var actor in originals) { if (actor.IsAlive) actor.RestoreOriginal(); actor.ReleaseOriginal(); }
            originals.Clear(); SceneId = null; StateId = null; disposed = true;
        }
    }
}
