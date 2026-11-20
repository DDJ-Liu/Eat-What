using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using EatWhat.LongLoop;
using EatWhat.Narrative.Core;
using EatWhat.Narrative.Unity;
using EatWhat.Tools.Security;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

internal static class NarrativeSceneDebugDialogueTests
{
    static readonly List<string> details = new List<string>();
    static void Check(bool value, string reason) { if (!value) throw new Exception(reason); details.Add(reason); }
    static void Life(object value, string method) { value.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(value, null); }
    static void Set(object value, string field, object data) { value.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(value, data); }
    static GameObject Child(GameObject parent, string name) { var c = new GameObject(parent.scene, name); c.transform.SetParent(parent.transform, false); return c; }
    static void Rows(CK01GeneratedDataCatalog c, string table, params ScriptableObject[] rows) { c.Rows[table] = new List<ScriptableObject>(rows); }
    sealed class Keys : IKeyProvider { public byte[] GetKey() { return new byte[32]; } }
    static NarrativeTalkedPersistence Record(string root) { var r = NarrativeTalkedPersistence.CreateForCurrentBuild(root, new Keys(), x => { }); r.InitializeWhenAvailable(true); return r; }
    static void Draw(NarrativeSceneDebugPanel panel) { GUILayout.Labels.Clear(); GUILayout.Buttons.Clear(); GUILayout.DisabledButtons.Clear(); Life(panel, "OnGUI"); }
    static void Toggle(NarrativeSceneDebugPanel panel)
    { Keyboard.current = new Keyboard(); Keyboard.current[Key.F8].wasPressedThisFrame = true; Life(panel, "Update"); Keyboard.current[Key.F8].wasPressedThisFrame = false; }
    static List<RaycastResult> Hits(NarrativeSceneDebugPanel panel, float x, float top)
    { var hits = new List<RaycastResult>(); panel.Raycast(new PointerEventData { position = new Vector2(x, Screen.height - top) }, hits); return hits; }
    static int Main(string[] args)
    {
        try
        {
            var scene = new Scene(1601, "DebugDialogueFixture"); var root = new GameObject(scene, "manager");
            var runtime = root.AddComponent<NarrativeRuntimeEntry>(); var registry = root.AddComponent<NarrativeTargetRegistry>();
            var service = root.AddComponent<NarrativeDialogueService>(); var config = new DialoguePresentationConfig(); var catalog = new CK01GeneratedDataCatalog();
            Rows(catalog, "Scenes", new CK01SceneData { scene_id = "scn_debug", scene_asset = scene.name, default_state = "day" });
            Rows(catalog, "SceneStates", new CK01SceneStateData { scene_id = "scn_debug", state_id = "day" }, new CK01SceneStateData { scene_id = "scn_debug", state_id = "night" });
            Rows(catalog, "Characters", new CK01CharacterData { character_id = "chr_npc" });
            Rows(catalog, "Dialogues", new CK01DialogueData { dialogue_id = "dlg_own", tier = "normal", first_line = "ln_own" }, new CK01DialogueData { dialogue_id = "dlg_state", tier = "normal", first_line = "ln_state" });
            Rows(catalog, "DialogueLines", new CK01DialogueLineData { dialogue_id = "dlg_own", line_id = "ln_own", text_key = "own" }, new CK01DialogueLineData { dialogue_id = "dlg_state", line_id = "ln_state", text_key = "state" });
            Rows(catalog, "SceneStatePlacements", new CK01SceneStatePlacementData { scene_id = "scn_debug", state_id = "day", kind = "actor", target = "chr_npc#2", active = true, dialogue_id = "dlg_state", marker = "none" },
                new CK01SceneStatePlacementData { scene_id = "scn_debug", state_id = "night", kind = "actor", target = "chr_npc#2", active = true, marker = "talk" });
            runtime.Configure(config, catalog); Check(runtime.IsAvailable, "actual runtime data directory is available");
            var npc = new GameObject(scene, "npc"); npc.transform.position = new Vector3(10000, 0, 0);
            var shell = npc.AddComponent<NpcShell>(); shell.ConfigureReferences(Child(npc, "foot").transform, Child(npc, "visual").transform, Child(npc, "head").transform);
            var actor = npc.AddComponent<NarrativeActorMarker>(); actor.ConfigureId("chr_npc#2");
            var trigger = npc.AddComponent<NarrativeDialogueTrigger>(); Set(trigger, "dialogueId", "dlg_own"); trigger.ConfigureReferences(runtime, service, actor, shell);
            registry.ConfigurePlayer(root.transform); registry.CollectSceneMarkers();
            var applier = root.AddComponent<NarrativeSceneStateApplier>(); applier.Configure(runtime, registry, null, null, null);
            var panel = root.AddComponent<NarrativeSceneDebugPanel>(); panel.Configure(applier, registry);
            Check(Hits(panel, 30, 30).Count == 0, "hidden panel does not intercept pointer"); Toggle(panel);
            Draw(panel); Check(!GUILayout.Buttons.Contains("对话 chr_npc#2") && !GUILayout.Buttons.Contains("清空已聊过"), "missing service/record hides corresponding new buttons");
            Check(GUILayout.Buttons.Contains("Apply day"), "missing optional bindings preserves old state entry");
            Check(!panel.TryStartDialogue("chr_npc#2") && !panel.TryClearTalked(), "unconfigured direct new entries reject safely");
            panel.ConfigureDialogue(service); Draw(panel);
            Check(GUILayout.Buttons.Contains("对话 chr_npc#2") && !GUILayout.Buttons.Contains("清空已聊过"), "service without late record exposes talk only");
            var record = Record(args[0]); service.Talked = record; record.MarkTalked("dlg_state"); record.MarkTalked("dlg_untouched");
            GUILayout.Clicks.Add("Apply day"); Draw(panel);
            Check(applier.CurrentStateId == "day" && trigger.ActualDialogueId == "dlg_state" && trigger.ActualMarkerValue == "none", "GUI Apply calls actual application owner and reads state-bound values");
            Check(record.HasTalked("dlg_state"), "first actual application does not clear record");
            Check(GUILayout.Labels.Exists(s => s.Contains("chr_npc#2 / 对话: dlg_state / 标记: none / 已聊过: True")), "NPC row without ambient component shows actual dialogue/explicit marker/record");
            int ground = GroundSpace.Calls; GUILayout.Clicks.Add("对话 chr_npc#2"); Draw(panel);
            Check(service.Starts == 1 && service.IsRunning && service.LastDialogueId == "dlg_state" && service.InitiatorId == "chr_npc#2", "GUI talk invokes unified service with actual binding and full instance ID");
            Check(GroundSpace.Calls == ground, "debug talk does not read or fabricate distance");
            Draw(panel); Check(GUILayout.Labels.Contains("对话进行中: dlg_state"), "running status reads actual service dialogue ID");
            GUILayout.Clicks.Add("Apply night"); Draw(panel);
            Check(GUILayout.DisabledButtons.Contains("Apply night") && applier.CurrentStateId == "day" && GUI.enabled, "state buttons disabled during dialogue and GUI state restored");
            Check(!panel.TryApplyState("night") && applier.CurrentStateId == "day", "direct state entry also refuses running dialogue even without applier service wiring");
            Check(!panel.TryStartDialogue("chr_npc#2") && service.Starts == 1, "debug duplicate start does not reenter service");
            var hits = Hits(panel, 30, 30);
            Check(hits.Count == 1 && hits[0].gameObject == panel.gameObject && hits[0].module == panel, "visible panel yields its own UGUI hit before any GUI button callback");
            Check(panel.eventCamera == null && panel.sortOrderPriority == int.MaxValue && panel.renderOrderPriority == int.MaxValue, "panel raycast outranks screen-overlay dialogue raycaster");
            var click = new PointerEventData(); panel.OnPointerClick(click); Check(click.used && service.Starts == 1, "pointer click consumed by panel without invoking service again");
            Check(Hits(panel, 500, 30).Count == 0 && Hits(panel, 30, 600).Count == 0, "outside panel remains available to dialogue screen");
            Check(Hits(panel, 16, 16).Count == 1 && Hits(panel, 436, 16).Count == 0 && Hits(panel, 16, 496).Count == 0, "screen-to-IMGUI conversion matches panel half-open rectangle");
            service.End(); Check(Hits(panel, 30, 30).Count == 1, "panel interception independent of dialogue start/end timing");
            GUILayout.Clicks.Add("Apply night"); Draw(panel); Check(applier.CurrentStateId == "night" && trigger.ActualDialogueId == "dlg_own", "state switching resumes and uses own fallback binding");
            Draw(panel); Check(GUILayout.Labels.Contains("对话进行中: 否"), "idle status reads service after end");
            GUILayout.Clicks.Add("清空已聊过"); Draw(panel);
            Check(record.Count == 0 && record.LastSaveSucceeded == true, "GUI clear invokes actual01 ClearAll and immediate save");
            var cold = Record(args[0]); Check(cold.Count == 0, "cold actual Toolkit file read confirms cleared record on disk");
            var late = Record(args[0] + "-late"); late.MarkTalked("dlg_own"); service.Talked = late; Draw(panel);
            Check(GUILayout.Labels.Exists(s => s.Contains("对话: dlg_own") && s.Contains("已聊过: True")), "record queried live without caching an earlier service owner");
            service.Accept = false; Check(!panel.TryStartDialogue("chr_npc#2"), "service rejection propagated without fabricated running state");
            Check(!panel.TryStartDialogue("chr_absent"), "missing full ID never reaches service");
            npc.SetActive(false); Check(!panel.TryStartDialogue("chr_npc#2"), "inactive NPC cannot be debug-started"); npc.SetActive(true);
            var wrong = new GameObject(new Scene(1602, "Other"), "other service").AddComponent<NarrativeDialogueService>(); panel.ConfigureDialogue(wrong); Draw(panel);
            Check(!GUILayout.Buttons.Contains("对话 chr_npc#2"), "other-scene service hidden rather than cross-wired");
            Toggle(panel); Check(Hits(panel, 30, 30).Count == 0, "closing panel immediately removes interception");
            Toggle(panel); panel.enabled = false; Check(Hits(panel, 30, 30).Count == 0, "disabled panel cannot intercept"); panel.enabled = true;
            Application.isPlaying = false; Check(Hits(panel, 30, 30).Count == 0, "edit-time panel does not intercept"); Application.isPlaying = true;
            Console.WriteLine(JsonConvert.SerializeObject(new { result = "SCENE_DEBUG_DIALOGUE_PASS", checks = details.Count, details }, Formatting.Indented)); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
