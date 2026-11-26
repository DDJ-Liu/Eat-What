using System;
using System.Collections.Generic;
using EatWhat.Narrative.Core;
using UnityEngine;

namespace EatWhat.LongLoop
{
    /// <summary>Scene-local target inventory. Lists preserve actual registration order, including duplicates.</summary>
    [DisallowMultipleComponent]
    public sealed class NarrativeTargetRegistry : MonoBehaviour
    {
        [SerializeField, Tooltip("Bind the formal player's controller/foot Transform in this scene, not its visual child.")]
        private Transform player = null;
        private readonly List<NarrativeActorMarker> actors = new List<NarrativeActorMarker>();
        private readonly List<NarrativeAnchorMarker> anchors = new List<NarrativeAnchorMarker>();

        private void OnEnable() { CollectSceneMarkers(); }
        private void OnDisable() { actors.Clear(); anchors.Clear(); }
        private void OnDestroy() { actors.Clear(); anchors.Clear(); }

        public void ConfigurePlayer(Transform value) { player = value; }

        // Collect also supports an explicitly requested rescan. Already registered objects keep their order.
        public void CollectSceneMarkers()
        {
            if (!isActiveAndEnabled || !gameObject.scene.IsValid() || !gameObject.scene.isLoaded) return;
            foreach (var root in gameObject.scene.GetRootGameObjects())
            {
                foreach (var marker in root.GetComponentsInChildren<NarrativeActorMarker>(false)) Register(marker);
                foreach (var marker in root.GetComponentsInChildren<NarrativeAnchorMarker>(false)) Register(marker);
            }
        }

        internal static void Notify(NarrativeActorMarker marker, bool enabled)
        {
            if (marker == null) return;
            foreach (var registry in UnityEngine.Object.FindObjectsByType<NarrativeTargetRegistry>(FindObjectsSortMode.InstanceID))
            {
                if (!registry.isActiveAndEnabled) continue;
                if (enabled) registry.Register(marker); else registry.actors.Remove(marker);
            }
        }

        internal static void Notify(NarrativeAnchorMarker marker, bool enabled)
        {
            if (marker == null) return;
            foreach (var registry in UnityEngine.Object.FindObjectsByType<NarrativeTargetRegistry>(FindObjectsSortMode.InstanceID))
            {
                if (!registry.isActiveAndEnabled) continue;
                if (enabled) registry.Register(marker); else registry.anchors.Remove(marker);
            }
        }

        private bool InScene(Component value)
        {
            return value != null && gameObject.scene.IsValid() && gameObject.scene.isLoaded &&
                value.gameObject.scene == gameObject.scene;
        }

        private bool Eligible(Behaviour value)
        {
            return isActiveAndEnabled && InScene(value) && value.isActiveAndEnabled;
        }

        private bool ParseId(string prefix, string id, out string key)
        {
            NarrativeCommandTarget target; NarrativeCommandDiagnostic error;
            key = null;
            if (!NarrativeCommandTargetParser.TryParse(prefix + id, out target, out error))
            {
                Warn("INVALID_ID", prefix + id + ": " + error.Reason);
                return false;
            }
            key = target.Identifier;
            return true;
        }

        private void Register(NarrativeActorMarker marker)
        {
            if (!Eligible(marker) || actors.Contains(marker)) return;
            string key;
            if (!ParseId("actor:", marker.CharacterId, out key)) return;
            foreach (var old in actors)
                if (Eligible(old) && SameActorId(old.CharacterId, key))
                { Warn("DUPLICATE_ACTOR", "actor:" + key + "; keeping first registration."); break; }
            actors.Add(marker);
        }

        private void Register(NarrativeAnchorMarker marker)
        {
            if (!Eligible(marker) || anchors.Contains(marker)) return;
            string key;
            if (!ParseId("anchor:", marker.AnchorId, out key)) return;
            foreach (var old in anchors)
                if (Eligible(old) && SameAnchorId(old.AnchorId, key))
                { Warn("DUPLICATE_ANCHOR", "anchor:" + key + "; keeping first registration."); break; }
            anchors.Add(marker);
        }

        // Parse through the shared grammar; comparison itself emits no extra diagnostics for stale entries.
        private static bool SameId(string prefix, string id, string key)
        {
            NarrativeCommandTarget target; NarrativeCommandDiagnostic error;
            return NarrativeCommandTargetParser.TryParse(prefix + id, out target, out error) &&
                string.Equals(target.Identifier, key, StringComparison.Ordinal);
        }
        private static bool SameActorId(string id, string key) { return SameId("actor:", id, key); }
        private static bool SameAnchorId(string id, string key) { return SameId("anchor:", id, key); }

        public bool TryGetActor(string characterId, out Transform value)
        {
            value = null;
            string key;
            if (!ParseId("actor:", characterId, out key)) return false;
            foreach (var marker in actors)
                if (Eligible(marker) && SameActorId(marker.CharacterId, key))
                { value = marker.transform; return true; }
            Warn("MISSING_ACTOR", "actor:" + key);
            return false;
        }

        public bool TryGetAnchorPosition(string anchorId, out Vector3 position)
        {
            position = default(Vector3);
            string key;
            if (!ParseId("anchor:", anchorId, out key)) return false;
            foreach (var marker in anchors)
                if (Eligible(marker) && SameAnchorId(marker.AnchorId, key))
                { position = marker.transform.position; return true; }
            Warn("MISSING_ANCHOR", "anchor:" + key);
            return false;
        }

        // State application may reactivate a preplaced NPC. Normal target resolution remains active-only.
        public bool TryGetActorForSceneState(string characterId, out Transform value)
        {
            value = null; string key;
            if (!isActiveAndEnabled || !ParseId("actor:", characterId, out key)) return false;
            foreach (var marker in actors)
                if (Eligible(marker) && SameActorId(marker.CharacterId, key)) { value = marker.transform; return true; }
            if (!gameObject.scene.IsValid() || !gameObject.scene.isLoaded) return false;
            foreach (var root in gameObject.scene.GetRootGameObjects())
                foreach (var marker in root.GetComponentsInChildren<NarrativeActorMarker>(true))
                    if (marker != null && marker.enabled && InScene(marker) && SameActorId(marker.CharacterId, key))
                    { value = marker.transform; return true; }
            return false;
        }

        public bool TryGetPlayer(out Transform value)
        {
            value = null;
            if (isActiveAndEnabled && InScene(player) && player.gameObject.activeInHierarchy)
            { value = player; return true; }
            Warn("MISSING_PLAYER", "Bind an active player foot/controller Transform in this scene.");
            return false;
        }

        public IReadOnlyList<NarrativeActorMarker> GetActiveActors()
        {
            var result = new List<NarrativeActorMarker>(); var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var marker in actors)
            {
                string key;
                if (Eligible(marker) && ParseId("actor:", marker.CharacterId, out key) && seen.Add(key)) result.Add(marker);
            }
            return result.AsReadOnly();
        }

        public bool TryResolve(string targetText, out Transform value)
        {
            value = null;
            NarrativeCommandTarget target; NarrativeCommandDiagnostic error;
            if (!NarrativeCommandTargetParser.TryParse(targetText, out target, out error))
            { Warn("INVALID_TARGET", (targetText ?? "<null>") + ": " + error.Reason); return false; }
            if (target.Kind == NarrativeCommandTargetKind.Player) return TryGetPlayer(out value);
            if (target.Kind == NarrativeCommandTargetKind.Actor) return TryGetActor(target.Identifier, out value);
            foreach (var marker in anchors)
                if (Eligible(marker) && SameAnchorId(marker.AnchorId, target.Identifier))
                { value = marker.transform; return true; }
            Warn("MISSING_ANCHOR", "anchor:" + target.Identifier);
            return false;
        }

        private void Warn(string code, string detail)
        {
            Debug.LogWarning("NarrativeTargetRegistry [" + code + "] scene=" + gameObject.scene.name + ": " + detail, this);
        }
    }
}
