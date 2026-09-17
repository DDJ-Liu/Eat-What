// VERIFY-TEMP: read-only audit for the isolated FridgeScrollLab scene.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EatWhat.Cooking.ShortCycle;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EatWhat.EditorTools.ShortCycle
{
    public static class FridgeScrollLabAudit
    {
        public const string ScenePath = "Assets/Scenes/ToolTests/FridgeScrollLab.unity";
        private const string RigPrefabPath = "Assets/Prefabs/Cooking/ShortCycle/FridgeCat/FridgeCat_Rig.prefab";
        private static readonly string[] ExpectedRoots =
        {
            "CameraAndStageMarkers", "Managers", "InteractionLayers", "Spaces", "Shared",
            "DebugAndReferences", "EventSystem"
        };

        [MenuItem("Tools/Eat What/Short Cycle/Audit Active Fridge Scroll Lab (Read Only)")]
        public static void AuditActiveScene()
        {
            string report;
            var passed = RunAudit(out report);
            if (passed) Debug.Log(report);
            else Debug.LogError(report);
        }

        public static bool RunAudit(out string report)
        {
            var scene = SceneManager.GetActiveScene();
            var builder = new StringBuilder();
            var failures = new List<string>();
            builder.AppendLine("[SCROLL-LAB-AUDIT] scene=" + scene.path + " utc=" + DateTime.UtcNow.ToString("o"));
            if (scene.path != ScenePath) failures.Add("active_scene_path_mismatch");

            var roots = scene.GetRootGameObjects();
            var rootNames = roots.Select(value => value.name).ToArray();
            builder.AppendLine("roots=" + string.Join(" > ", rootNames));
            if (!rootNames.SequenceEqual(ExpectedRoots)) failures.Add("root_order_or_set_mismatch");

            var all = roots.SelectMany(value => value.GetComponentsInChildren<Transform>(true))
                .Select(value => value.gameObject).Distinct().ToArray();
            var missingScripts = all.Sum(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount);
            var canvases = all.Sum(value => value.GetComponents<Canvas>().Length);
            var cameras = all.Sum(value => value.GetComponents<Camera>().Length);
            var mouseManagers = all.Sum(value => value.GetComponents<MouseManager>().Length);
            var drivers = all.SelectMany(value => value.GetComponents<FridgeScrollLabDriver>()).ToArray();
            var traces = all.SelectMany(value => value.GetComponents<ShortCycleFridgeScrollTraceProbe>()).ToArray();
            builder.AppendLine("structure missingScripts=" + missingScripts + " canvas=" + canvases +
                " cameras=" + cameras + " mouseManagers=" + mouseManagers +
                " drivers=" + drivers.Length + " traces=" + traces.Length);
            if (missingScripts != 0) failures.Add("missing_script_count=" + missingScripts);
            if (canvases != 0) failures.Add("canvas_forbidden=" + canvases);
            if (cameras != 1) failures.Add("camera_count=" + cameras);
            if (mouseManagers != 1) failures.Add("mouse_manager_count=" + mouseManagers);
            if (drivers.Length != 1) failures.Add("driver_count=" + drivers.Length);
            if (traces.Length != 1) failures.Add("trace_count=" + traces.Length);

            var regions = all.Where(value => value.name == "ScrollRegion").ToArray();
            if (regions.Length != 1) failures.Add("scroll_region_count=" + regions.Length);
            var region = regions.Length == 1 ? regions[0] : null;
            var collider = region == null ? null : region.GetComponent<BoxCollider2D>();
            var scrollable = region == null ? null : region.GetComponent<MouseScrollableObject>();
            var adapter = region == null ? null : region.GetComponent<ShortCycleScrollAdapter>();
            if (collider == null || scrollable == null || adapter == null) failures.Add("scroll_chain_component_missing");
            if (adapter != null && (adapter.ScrollableObject != scrollable || adapter.SessionManager == null || adapter.ActionRouter == null))
                failures.Add("scroll_adapter_reference_mismatch");

            var rigs = all.SelectMany(value => value.GetComponents<FridgeCatRig>()).ToArray();
            if (rigs.Length != 1) failures.Add("fridge_cat_rig_count=" + rigs.Length);
            var rig = rigs.FirstOrDefault();
            var renderers = rig == null ? new Renderer[0] : rig.gameObject.GetComponentsInChildren<Renderer>(true);
            Bounds rendererBounds;
            var hasRendererBounds = TryMeasureBounds(renderers, out rendererBounds);
            if (!hasRendererBounds) failures.Add("renderer_bounds_missing");
            if (collider != null && hasRendererBounds && !Contains(collider.bounds, rendererBounds))
                failures.Add("scroll_region_does_not_cover_renderer_bounds");
            builder.AppendLine("geometry rendererBounds=" + (hasRendererBounds ? Format(rendererBounds) : "missing") +
                " colliderBounds=" + (collider == null ? "missing" : Format(collider.bounds)));

            var rigSource = rig == null ? null : PrefabUtility.GetCorrespondingObjectFromSource(rig.gameObject);
            var rigSourcePath = rigSource == null ? string.Empty : AssetDatabase.GetAssetPath(rigSource);
            builder.AppendLine("rigPrefabSource=" + rigSourcePath);
            if (rigSourcePath != RigPrefabPath) failures.Add("rig_prefab_source=" + rigSourcePath);

            var driver = drivers.FirstOrDefault();
            var trace = traces.FirstOrDefault();
            if (driver != null)
            {
                if (driver.SessionManager == null || driver.ScrollAdapter != adapter || driver.TraceProbe != trace ||
                    driver.LayerStack == null || driver.ModalComparisonLayer == null || driver.StatusBoard == null)
                    failures.Add("driver_reference_mismatch");
                AuditPoints(driver.TestPoints, collider, Camera.main, builder, failures);
            }

            if (trace != null)
            {
                builder.AppendLine("trace mode=" + trace.CaptureMode + " session=" + trace.CaptureSessionId +
                    " enabled=" + trace.TraceEnabled + " raw=" + trace.LatestRawScroll.ToString("0.###") +
                    " lastNonzero=" + trace.LastNonzeroRawScroll.ToString("0.###") + "@" + trace.LastNonzeroRawFrame +
                    " frameDelta=" + trace.LatestMouseStepDelta + "/" + trace.LatestTargetEventDelta + "/" +
                    trace.LatestAdapterReceiveDelta + "/" + trace.LatestAdapterDispatchDelta);
                if (trace.ExpectedMainCamera != Camera.main || trace.MouseManager == null || trace.ScrollRegion != scrollable ||
                    trace.ScrollAdapter != adapter || trace.SessionManager == null || trace.FridgeBinder == null ||
                    trace.FridgeCatRig != rig || trace.ScrollArea == null)
                    failures.Add("trace_reference_mismatch");
            }

            var buildSettingsEntry = EditorBuildSettings.scenes.Any(value => value.path == ScenePath);
            if (buildSettingsEntry) failures.Add("lab_must_not_be_in_build_settings");
            var prohibitedTypes = new[] { "ShortCycleTrayStateController" };
            var prohibitedNames = new[] { "Phase0_RecipeBook", "RecipeBook_Group", "Tray_Group", "Book_Group" };
            foreach (var typeName in prohibitedTypes)
                if (all.Any(value => value.GetComponents<MonoBehaviour>().Any(component => component != null && component.GetType().Name == typeName)))
                    failures.Add("prohibited_component=" + typeName);
            foreach (var objectName in prohibitedNames)
                if (all.Any(value => value.name == objectName)) failures.Add("prohibited_object=" + objectName);

            var focusedWindow = EditorWindow.focusedWindow;
            builder.AppendLine("focus application=" + Application.isFocused + " editorWindow=" +
                (focusedWindow == null ? "none" : focusedWindow.GetType().Name) +
                " gameViewFocused=" + (focusedWindow != null && focusedWindow.GetType().Name == "GameView"));
            builder.AppendLine("buildSettingsEntry=" + buildSettingsEntry);
            builder.AppendLine("result=" + (failures.Count == 0 ? "PASS" : "FAIL") +
                (failures.Count == 0 ? string.Empty : " failures=" + string.Join(" | ", failures.ToArray())));
            report = builder.ToString();
            return failures.Count == 0;
        }

        private static void AuditPoints(Transform[] points, BoxCollider2D region, Camera camera,
            StringBuilder builder, List<string> failures)
        {
            var values = points ?? new Transform[0];
            if (values.Length != 4) failures.Add("test_point_count=" + values.Length);
            for (var index = 0; index < values.Length; index++)
            {
                var point = values[index];
                if (point == null)
                {
                    failures.Add("test_point_missing=" + index);
                    continue;
                }
                if (point.GetComponentsInChildren<Collider2D>(true).Length != 0 ||
                    point.GetComponentsInChildren<MousePressableObject>(true).Length != 0 ||
                    point.GetComponentsInChildren<MouseScrollableObject>(true).Length != 0 ||
                    point.GetComponentsInChildren<MouseDraggableObject>(true).Length != 0)
                    failures.Add("test_point_has_interaction_component=" + point.name);
                var viewport = camera == null ? Vector3.zero : camera.WorldToViewportPoint(point.position);
                var hits = Physics2D.OverlapPointAll(point.position);
                builder.AppendLine("point name=" + point.name + " world=" + point.position +
                    " viewport=" + (camera == null ? "camera_missing" : viewport.ToString()) +
                    " insideRegion=" + (region != null && region.OverlapPoint(point.position)) +
                    " hits=[" + string.Join(",", hits.Select(value => PathOf(value.transform)).ToArray()) + "]");
            }
        }

        private static bool TryMeasureBounds(Renderer[] renderers, out Bounds bounds)
        {
            var available = (renderers ?? new Renderer[0]).Where(value => value != null).ToArray();
            if (available.Length == 0) { bounds = new Bounds(); return false; }
            bounds = available[0].bounds;
            for (var index = 1; index < available.Length; index++) bounds.Encapsulate(available[index].bounds);
            return true;
        }

        private static bool Contains(Bounds outer, Bounds inner)
        {
            return outer.min.x <= inner.min.x && outer.min.y <= inner.min.y &&
                outer.max.x >= inner.max.x && outer.max.y >= inner.max.y;
        }

        private static string Format(Bounds value)
        {
            return "center=" + value.center + ",size=" + value.size + ",min=" + value.min + ",max=" + value.max;
        }

        private static string PathOf(Transform value)
        {
            if (value == null) return "missing";
            var names = new List<string>();
            for (var current = value; current != null; current = current.parent) names.Add(current.name);
            names.Reverse();
            return string.Join("/", names.ToArray());
        }
    }
}
