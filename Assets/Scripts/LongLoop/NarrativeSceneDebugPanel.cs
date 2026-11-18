#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using EatWhat.Narrative.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EatWhat.LongLoop
{
    [DisallowMultipleComponent]
    public sealed class NarrativeSceneDebugPanel : MonoBehaviour
    {
        [SerializeField] private NarrativeSceneStateApplier stateApplier = null;
        [SerializeField] private NarrativeTargetRegistry targetRegistry = null;
        [SerializeField] private Key toggleKey_TUNE = Key.F8;
        [SerializeField] private Rect window_TUNE = new Rect(16, 16, 420, 480);
        private bool visible;
        private Vector2 scroll;
        private string lastResult = string.Empty;
        public bool IsVisible => visible;

        public void Configure(NarrativeSceneStateApplier applier, NarrativeTargetRegistry registry)
        { stateApplier = applier; targetRegistry = registry; }
        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && toggleKey_TUNE != Key.None && keyboard[toggleKey_TUNE].wasPressedThisFrame) visible = !visible;
        }
        private bool BindingsReady()
        {
            return Application.isPlaying && isActiveAndEnabled && stateApplier != null && targetRegistry != null &&
                stateApplier.isActiveAndEnabled && targetRegistry.isActiveAndEnabled &&
                stateApplier.gameObject.scene == gameObject.scene && targetRegistry.gameObject.scene == gameObject.scene;
        }
        public bool TryApplyState(string stateId)
        {
            bool result = BindingsReady() && stateApplier.ApplyState(stateId);
            lastResult = result ? "Applied state: " + stateId : "State unavailable; see scene diagnostics."; return result;
        }
        public bool TryForceBubble(string fullId)
        {
            Transform actor;
            if (!BindingsReady() || !targetRegistry.TryGetActor(fullId, out actor))
            { lastResult = "NPC unavailable."; return false; }
            var shell = actor.GetComponent<NpcShell>(); var trigger = actor.GetComponent<NarrativeAmbientBubbleTrigger>();
            bool result = shell != null && shell.isActiveAndEnabled && trigger != null && trigger.isActiveAndEnabled && trigger.DebugForceBubble();
            lastResult = result ? "Emitted: " + fullId : "No bubble emitted; see trigger/view diagnostics."; return result;
        }
        private void OnGUI()
        {
            if (!visible || !Application.isPlaying) return;
            GUILayout.BeginArea(window_TUNE, "Scene debug", GUI.skin.window);
            try
            {
                if (!BindingsReady()) { GUILayout.Label("Unavailable: assign this scene's active Applier and Registry."); return; }
                string sceneId, stateId, diagnostic; IReadOnlyList<NarrativeSceneState> states;
                if (!stateApplier.TryGetSceneStateInfo(out sceneId, out stateId, out states, out diagnostic))
                { GUILayout.Label("Unavailable: " + diagnostic); return; }
                GUILayout.Label("Scene: " + sceneId + " / State: " + (stateId ?? "<not applied>"));
                scroll = GUILayout.BeginScrollView(scroll);
                try
                {
                    GUILayout.Label("States");
                    foreach (var state in states) if (GUILayout.Button("Apply " + state.StateId)) TryApplyState(state.StateId);
                    GUILayout.Label("Active NPCs / ambient pools");
                    foreach (var marker in targetRegistry.GetActiveActors())
                    {
                        if (marker == null) continue;
                        var shell = marker.GetComponent<NpcShell>(); var trigger = marker.GetComponent<NarrativeAmbientBubbleTrigger>();
                        if (shell == null || !shell.isActiveAndEnabled || trigger == null || !trigger.isActiveAndEnabled) continue;
                        GUILayout.Label(marker.CharacterId + " / " + (string.IsNullOrEmpty(trigger.PoolId) ? "<none>" : trigger.PoolId));
                        if (GUILayout.Button("Bubble " + marker.CharacterId)) TryForceBubble(marker.CharacterId);
                    }
                    GUILayout.Label(lastResult);
                }
                finally { GUILayout.EndScrollView(); }
            }
            finally { GUILayout.EndArea(); }
        }
    }
}
#endif
