// Actual F8 panel; external stage calls, service query, GUI and clipboard transport only.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using EatWhat.LongLoop;
using EatWhat.Narrative.Unity;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

internal static class NarrativePortraitTuningPanelTests
{
    static readonly List<string> details = new List<string>();
    static void Check(bool value, string reason) { if (!value) throw new Exception(reason); details.Add(reason); }
    static void Life(object value, string method) { value.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(value, null); }
    static void Draw(NarrativeSceneDebugPanel panel) { GUILayout.Labels.Clear(); GUILayout.Buttons.Clear(); GUILayout.DisabledButtons.Clear(); Life(panel, "OnGUI"); }
    static void Rows(CK01GeneratedDataCatalog c, string table, params ScriptableObject[] rows) { c.Rows[table] = new List<ScriptableObject>(rows); }
    static bool Near(float a, float b) { return Math.Abs(a - b) < .000001f; }
    static bool Same(Vector3 a, Vector3 b) { return Near(a.x, b.x) && Near(a.y, b.y) && Near(a.z, b.z); }
    static KeyValuePair<string, Vector3> Row(string id, float x, float y, float scale) { return new KeyValuePair<string, Vector3>(id, new Vector3(x, y, scale)); }
    static void MessageOnly(NarrativeSceneDebugPanel panel, string reason)
    {
        Draw(panel);
        Check(GUILayout.Labels.Contains("立绘调参") && GUILayout.Labels.Contains("当前没有对话或立绘在显示。") &&
            !GUILayout.Buttons.Contains("复制") && !GUILayout.Buttons.Contains("偏移X+") && !GUILayout.Buttons.Contains("清除全部临时值"), reason);
    }
    static int Main()
    {
        try
        {
            var scene = new Scene(1901, "PortraitTuningFixture"); var root = new GameObject(scene, "manager");
            var runtime = root.AddComponent<NarrativeRuntimeEntry>(); var registry = root.AddComponent<NarrativeTargetRegistry>();
            var service = root.AddComponent<NarrativeDialogueService>(); var config = new DialoguePresentationConfig(); var catalog = new CK01GeneratedDataCatalog();
            Rows(catalog, "Scenes", new CK01SceneData { scene_id = "scn_tuning", scene_asset = scene.name, default_state = "day" });
            Rows(catalog, "SceneStates", new CK01SceneStateData { scene_id = "scn_tuning", state_id = "day" });
            runtime.Configure(config, catalog); Check(runtime.IsAvailable, "actual runtime data directory available");
            registry.ConfigurePlayer(root.transform); registry.CollectSceneMarkers();
            var applier = root.AddComponent<NarrativeSceneStateApplier>(); applier.Configure(runtime, registry, null, null, null);
            var panel = root.AddComponent<NarrativeSceneDebugPanel>(); panel.Configure(applier, registry); panel.ConfigureDialogue(service);
            Keyboard.current = new Keyboard(); Keyboard.current[Key.F8].wasPressedThisFrame = true; Life(panel, "Update"); Keyboard.current[Key.F8].wasPressedThisFrame = false;
            Check(panel.IsVisible, "existing F8 toggle opens panel");
            Draw(panel); Check(!GUILayout.Labels.Contains("立绘调参") && GUILayout.Buttons.Contains("Apply day"), "unbound stage hides section and preserves old UI");
            Check(panel.GetPortraitFramings().Count == 0 && !panel.TryAdjustPortraitFraming(0, 0, true) && !panel.TryCopyPortraitFraming(0) && !panel.TryClearAllPortraitFraming(), "unbound direct entries safely reject");
            var stage = root.AddComponent<NarrativePortraitStage>(); panel.ConfigurePortraitStage(stage);
            stage.Rows.Add(Row("chr_a", .1f, .2f, 1)); stage.Rows.Add(Row("chr_b", .3f, .4f, 2));
            MessageOnly(panel, "idle dialogue exposes only the section explanation even with provider rows");
            Check(panel.GetPortraitFramings().Count == 0 && !panel.TryCopyPortraitFraming(0) && !panel.TryClearAllPortraitFraming(), "idle service prevents stale portrait actions");
            string issue; service.TryStart("dlg_fixture", "chr_init#2", out issue);
            Draw(panel); var values = panel.GetPortraitFramings();
            Check(values.Count == 2 && values[0].Key == "chr_a" && values[1].Key == "chr_b", "running panel lists each provider row and role ID in order");
            Check(Same(values[0].Value, new Vector3(.1f, .2f, 1)) && Same(values[1].Value, new Vector3(.3f, .4f, 2)), "current values are read directly from stage provider");
            Check(GUILayout.Labels.Contains("chr_a") && GUILayout.Labels.Contains("chr_b") && GUILayout.Buttons.FindAll(x => x == "复制").Count == 2, "each displayed role has its own copy button");
            for (int axis = 0; axis < 3; axis++) for (int sign = -1; sign <= 1; sign += 2)
            {
                var expected = new Vector3(.3f, .4f, 2); if (axis == 0) expected.x += sign * .01f; else if (axis == 1) expected.y += sign * .01f; else expected.z += sign * .02f;
                int before = stage.Sets.Count; bool ok = panel.TryAdjustPortraitFraming(1, axis, sign > 0);
                Check(ok && stage.Sets.Count == before + 1 && stage.Sets[before].Key == "chr_b" && Same(stage.Sets[before].Value, expected), "axis " + axis + " direction " + sign + " delegates one exact step to stage setter");
            }
            int count = stage.Sets.Count;
            Check(!panel.TryAdjustPortraitFraming(-1, 0, true) && !panel.TryAdjustPortraitFraming(2, 0, true) && !panel.TryAdjustPortraitFraming(0, -1, true) && !panel.TryAdjustPortraitFraming(0, 3, true) && stage.Sets.Count == count, "invalid row or axis never calls setter");
            foreach (var button in new[] { "偏移X+", "偏移X-", "偏移Y+", "偏移Y-", "缩放+", "缩放-" })
            {
                count = stage.Sets.Count; GUILayout.Clicks.Add(button); Draw(panel);
                Check(stage.Sets.Count == count + 1 && stage.Sets[count].Key == "chr_a", "GUI " + button + " routes to its displayed row once");
            }
            stage.Rows[0] = Row("chr_a", .11f, .22f, 1.02f); Draw(panel);
            Check(Same(panel.GetPortraitFramings()[0].Value, stage.Rows[0].Value) && GUILayout.Labels.Contains("偏移X: 0.11"), "provider changes including temporary values are read live without a panel cache");
            var culture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR"); int logs = Debug.Messages.Count;
                GUILayout.Clicks.Add("复制"); Draw(panel);
                Check(GUIUtility.systemCopyBuffer == "chr_a\t0.11\t0.22\t1.02" && Debug.Messages.Count == logs + 1 && Debug.Messages[logs] == GUIUtility.systemCopyBuffer, "copy GUI writes the same invariant one-line role/x/y/scale to clipboard and log");
                Check(panel.TryCopyPortraitFraming(1) && GUIUtility.systemCopyBuffer == "chr_b\t0.3\t0.4\t2", "copy uses selected row rather than another portrait");
            }
            finally { CultureInfo.CurrentCulture = culture; }
            Check(!panel.TryCopyPortraitFraming(-1) && !panel.TryCopyPortraitFraming(2), "copy rejects stale or invalid row");
            int clears = stage.Clears; GUILayout.Clicks.Add("清除全部临时值"); Draw(panel);
            Check(stage.Clears == clears + 1, "clear GUI calls stage ClearAllTemporaryFraming exactly once");
            stage.Rows[0] = Row("chr_a", .1f, .2f, 1); Check(Near(panel.GetPortraitFramings()[0].Value.x, .1f), "restored provider base value is exposed after clear");
            stage.Rows[0] = Row("chr_a", .1f, .2f, .01f); count = stage.Sets.Count;
            Check(!panel.TryAdjustPortraitFraming(0, 2, false) && stage.Sets.Count == count, "nonpositive scale rejects the step without changing provider");
            var pointer = new PointerEventData { position = new Vector2(30, Screen.height - 30) }; var hits = new List<RaycastResult>();
            int starts = service.Starts; panel.Raycast(pointer, hits); panel.OnPointerClick(pointer);
            Check(hits.Count == 1 && hits[0].module == panel && pointer.used && panel.sortOrderPriority == int.MaxValue && service.Starts == starts, "existing window raycaster consumes panel pointer without another dialogue start");
            stage.Rows.Clear(); MessageOnly(panel, "running dialogue without portraits shows only explanation");
            Check(panel.GetPortraitFramings().Count == 0 && !panel.TryAdjustPortraitFraming(0, 0, true), "empty provider cannot be adjusted");
            stage.Rows.Add(Row("chr_a", 0, 0, 1)); service.End(); MessageOnly(panel, "ended dialogue hides lingering provider portraits");
            panel.ConfigureDialogue(null); MessageOnly(panel, "missing dialogue service exposes only explanation"); panel.ConfigureDialogue(service);
            stage.enabled = false; Draw(panel); Check(!GUILayout.Labels.Contains("立绘调参"), "disabled stage hides unavailable section"); stage.enabled = true;
            var other = new GameObject(new Scene(1902, "Other"), "other stage").AddComponent<NarrativePortraitStage>(); panel.ConfigurePortraitStage(other); Draw(panel);
            Check(!GUILayout.Labels.Contains("立绘调参") && !panel.TryClearAllPortraitFraming(), "other-scene stage cannot be cross-wired");
            panel.ConfigurePortraitStage(null); Draw(panel); Check(!GUILayout.Labels.Contains("立绘调参") && GUILayout.Buttons.Contains("Apply day"), "unbinding stage returns to unchanged optional-section UI");
            Console.WriteLine(JsonConvert.SerializeObject(new { result = "PORTRAIT_TUNING_PANEL_PASS", checks = details.Count, details, nativeUnity = false }, Formatting.Indented)); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
