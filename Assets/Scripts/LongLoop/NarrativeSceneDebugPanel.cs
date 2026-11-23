#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using System.Globalization;
using EatWhat.Narrative.Core;
using EatWhat.Narrative.Unity;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace EatWhat.LongLoop
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-1000)]
    public sealed class NarrativeSceneDebugPanel : BaseRaycaster, IPointerClickHandler
    {
        [SerializeField] private NarrativeSceneStateApplier stateApplier = null;
        [SerializeField] private NarrativeTargetRegistry targetRegistry = null;
        [SerializeField] private NarrativeDialogueService dialogueService = null;
        [SerializeField] private NarrativePortraitStage portraitStage = null;
        [SerializeField] private Key toggleKey_TUNE = Key.F8;
        [SerializeField] private Rect window_TUNE = new Rect(16, 16, 420, 480);
        private bool visible;
        private Vector2 scroll;
        private string lastResult = string.Empty;
        public bool IsVisible => visible;
        private NarrativeTalkedPersistence talkedOverride;
        private bool ServiceReady => dialogueService != null && dialogueService.isActiveAndEnabled && dialogueService.gameObject.scene == gameObject.scene;
        private bool DialogueRunning => ServiceReady && dialogueService.IsRunning;
        private bool PortraitStageReady => portraitStage != null && portraitStage.isActiveAndEnabled && portraitStage.gameObject.scene == gameObject.scene;
        private static readonly IReadOnlyList<KeyValuePair<string, Vector3>> emptyFramings = new List<KeyValuePair<string, Vector3>>().AsReadOnly();
        private NarrativeTalkedPersistence Talked => talkedOverride ?? (ServiceReady ? dialogueService.Talked : null);

        // Raycast selection precedes PointerClick dispatch. IMGUI Event.Use alone would be too late.
        public override Camera eventCamera => null;
        public override int sortOrderPriority => int.MaxValue;
        public override int renderOrderPriority => int.MaxValue;
        public override void Raycast(PointerEventData eventData, List<RaycastResult> results)
        {
            if (!visible || !Application.isPlaying || !isActiveAndEnabled || eventData == null) return;
            var point = new Vector2(eventData.position.x, Screen.height - eventData.position.y);
            if (window_TUNE.Contains(point)) results.Add(new RaycastResult
            { gameObject = gameObject, module = this, index = results.Count, distance = 0, depth = int.MaxValue });
        }
        public void OnPointerClick(PointerEventData eventData) { eventData.Use(); }

        public void Configure(NarrativeSceneStateApplier applier, NarrativeTargetRegistry registry)
        { stateApplier = applier; targetRegistry = registry; }
        public void ConfigureDialogue(NarrativeDialogueService service, NarrativeTalkedPersistence talked = null)
        { dialogueService = service; talkedOverride = talked; }
        public void ConfigurePortraitStage(NarrativePortraitStage stage) { portraitStage = stage; }
        public IReadOnlyList<KeyValuePair<string, Vector3>> GetPortraitFramings()
        { return DialogueRunning && PortraitStageReady ? portraitStage.GetVisibleFramings() : emptyFramings; }
        public bool TryAdjustPortraitFraming(int row, int axis, bool increase)
        {
            var values = GetPortraitFramings();
            if (row < 0 || row >= values.Count || axis < 0 || axis > 2) return false;
            var value = values[row].Value; float direction = increase ? 1 : -1;
            if (axis == 0) value.x += direction * .01f;
            else if (axis == 1) value.y += direction * .01f;
            else value.z += direction * .02f;
            if (!(value.z > 0)) { lastResult = "缩放必须大于0，本次未修改。"; return false; }
            portraitStage.SetTemporaryFraming(values[row].Key, value.x, value.y, value.z); return true;
        }
        public bool TryCopyPortraitFraming(int row)
        {
            var values = GetPortraitFramings(); if (row < 0 || row >= values.Count) return false;
            var value = values[row];
            string line = value.Key + "\t" + Number(value.Value.x) + "\t" + Number(value.Value.y) + "\t" + Number(value.Value.z);
            GUIUtility.systemCopyBuffer = line; Debug.Log(line); lastResult = "已复制: " + line; return true;
        }
        public bool TryClearAllPortraitFraming()
        {
            if (!DialogueRunning || !PortraitStageReady) return false;
            portraitStage.ClearAllTemporaryFraming(); lastResult = "已清除全部立绘临时值。"; return true;
        }
        static string Number(float value) { return value.ToString("R", CultureInfo.InvariantCulture); }
        void DrawPortraitTuning()
        {
            if (!PortraitStageReady) return;
            GUILayout.Label("立绘调参"); var values = GetPortraitFramings();
            if (values.Count == 0) { GUILayout.Label("当前没有对话或立绘在显示。"); return; }
            for (int row = 0; row < values.Count; row++)
            {
                GUILayout.Label(values[row].Key);
                DrawPortraitValue(row, 0, "偏移X", values[row].Value.x);
                DrawPortraitValue(row, 1, "偏移Y", values[row].Value.y);
                DrawPortraitValue(row, 2, "缩放", values[row].Value.z);
                if (GUILayout.Button("复制")) TryCopyPortraitFraming(row);
            }
            if (GUILayout.Button("清除全部临时值")) TryClearAllPortraitFraming();
        }
        void DrawPortraitValue(int row, int axis, string label, float value)
        {
            GUILayout.BeginHorizontal();
            try
            {
                GUILayout.Label(label + ": " + Number(value));
                if (GUILayout.Button(label + "-")) TryAdjustPortraitFraming(row, axis, false);
                if (GUILayout.Button(label + "+")) TryAdjustPortraitFraming(row, axis, true);
            }
            finally { GUILayout.EndHorizontal(); }
        }
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
            if (DialogueRunning) { lastResult = "对话进行中，不能切换状态。"; return false; }
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
        public bool TryStartDialogue(string fullCharacterId)
        {
            Transform actor;
            if (!BindingsReady() || !ServiceReady || DialogueRunning || !targetRegistry.TryGetActor(fullCharacterId, out actor)) return false;
            var trigger = actor.GetComponent<NarrativeDialogueTrigger>();
            if (trigger == null || !trigger.isActiveAndEnabled || string.IsNullOrEmpty(trigger.ActualDialogueId)) return false;
            string diagnostic;
            var result = dialogueService.TryStart(trigger.ActualDialogueId, fullCharacterId, out diagnostic);
            lastResult = result == NarrativeServiceStartStatus.Started ? "对话已开始: " + trigger.ActualDialogueId : "对话未开始: " + diagnostic;
            return result == NarrativeServiceStartStatus.Started;
        }
        public bool TryClearTalked()
        {
            var record = Talked;
            if (!BindingsReady() || record == null || !record.IsInitialized) return false;
            record.ClearAll();
            lastResult = record.LastSaveSucceeded == true ? "已清空已聊过记录。" : "已清空内存记录；保存失败，请检查存档诊断。";
            return record.LastSaveSucceeded == true;
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
                if (ServiceReady) GUILayout.Label(DialogueRunning ? "对话进行中: " + dialogueService.DialogueId : "对话进行中: 否");
                var record = Talked;
                if (record != null && record.IsInitialized && GUILayout.Button("清空已聊过")) TryClearTalked();
                scroll = GUILayout.BeginScrollView(scroll);
                try
                {
                    GUILayout.Label("States");
                    bool wasEnabled = GUI.enabled;
                    try
                    {
                        GUI.enabled = wasEnabled && !DialogueRunning;
                        foreach (var state in states) if (GUILayout.Button("Apply " + state.StateId)) TryApplyState(state.StateId);
                    }
                    finally { GUI.enabled = wasEnabled; }
                    GUILayout.Label("Active NPCs / ambient pools");
                    foreach (var marker in targetRegistry.GetActiveActors())
                    {
                        if (marker == null) continue;
                        var shell = marker.GetComponent<NpcShell>(); var trigger = marker.GetComponent<NarrativeAmbientBubbleTrigger>();
                        if (shell == null || !shell.isActiveAndEnabled) continue;
                        if (trigger != null && trigger.isActiveAndEnabled)
                        {
                            GUILayout.Label(marker.CharacterId + " / " + (string.IsNullOrEmpty(trigger.PoolId) ? "<none>" : trigger.PoolId));
                            if (GUILayout.Button("Bubble " + marker.CharacterId)) TryForceBubble(marker.CharacterId);
                        }
                        var dialogue = marker.GetComponent<NarrativeDialogueTrigger>();
                        string id = dialogue == null ? null : dialogue.ActualDialogueId;
                        GUILayout.Label(marker.CharacterId + " / 对话: " + (string.IsNullOrEmpty(id) ? "<none>" : id) +
                            " / 标记: " + (dialogue == null || string.IsNullOrEmpty(dialogue.ActualMarkerValue) ? "<auto>" : dialogue.ActualMarkerValue) +
                            " / 已聊过: " + (record == null || !record.IsInitialized ? "<unavailable>" : record.HasTalked(id).ToString()));
                        if (ServiceReady && GUILayout.Button("对话 " + marker.CharacterId)) TryStartDialogue(marker.CharacterId);
                    }
                    DrawPortraitTuning();
                    GUILayout.Label(lastResult);
                }
                finally { GUILayout.EndScrollView(); }
            }
            finally { GUILayout.EndArea(); }
        }
    }
}
#endif
