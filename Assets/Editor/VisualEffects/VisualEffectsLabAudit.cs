using System;
using System.Collections.Generic;
using System.Linq;
using EatWhat.Tools.Debugging;
using EatWhat.Tools.Rendering;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EatWhat.EditorTools.VisualEffects
{
    /// <summary>Independent, read-only measurements for the active Lab scene.</summary>
    public static class VisualEffectsLabAudit
    {
        private static readonly string[] ExpectedRoots =
        {
            "CameraAndStageMarkers",
            "Managers",
            "InteractionLayers",
            "Spaces",
            "Shared",
            "DebugAndReferences",
            "EventSystem"
        };

        private static readonly string[] RequiredPaths =
        {
            "CameraAndStageMarkers/MainCamera",
            "CameraAndStageMarkers/BackgroundCaptureCamera",
            "CameraAndStageMarkers/GlobalLight2D",
            "CameraAndStageMarkers/ViewCenterMarker",
            "Managers/InputManager",
            "Managers/SceneColorCapture",
            "Spaces/EtherBubbleLab/BackgroundPlane_Group/CaptureBackdrop_TUNE",
            "Spaces/EtherBubbleLab/BackgroundPlane_Group/StaticGeometry_Group",
            "Spaces/EtherBubbleLab/Emitter_Group/MouthAnchor_TUNE",
            "Spaces/EtherBubbleLab/Emitter_Group/BubbleSpawnParent",
            "Spaces/EtherBubbleLab/ForegroundPlane_Group/OcclusionBar_TUNE",
            "Shared/Reserved_Group",
            "DebugAndReferences/Sample_BackgroundContent/CheckerGrid",
            "DebugAndReferences/Sample_BackgroundContent/TransparentSprite",
            "DebugAndReferences/Sample_BackgroundContent/AnimatedSprite",
            "DebugAndReferences/Sample_BackgroundContent/OutlineAndShadowSprite",
            "DebugAndReferences/Sample_BackgroundContent/ChineseText",
            "DebugAndReferences/Sample_Character/MouthVisual_TUNE",
            "DebugAndReferences/Probe_VisualEffectsLab",
            "DebugAndReferences/Probe_StatusText"
        };

        [MenuItem("Tools/Visual Effects Lab/Audit Active Lab (Read Only)")]
        public static void AuditActiveSceneFromMenu()
        {
            var result = AuditActiveScene();
            if (result.Success) Debug.Log(result.Summary);
            else Debug.LogError(result.Summary);
        }

        public static VisualEffectsLabAuditResult AuditActiveScene()
        {
            var scene = SceneManager.GetActiveScene();
            var roots = scene.IsValid()
                ? scene.GetRootGameObjects().OrderBy(go => go.transform.GetSiblingIndex()).ToArray()
                : new GameObject[0];
            var rootNames = roots.Select(go => go.name).ToArray();
            var rootOrderValid = rootNames.SequenceEqual(ExpectedRoots);
            var allObjects = roots.SelectMany(root =>
                    root.GetComponentsInChildren<Transform>(true).Select(item => item.gameObject))
                .Distinct()
                .ToArray();
            var missingPaths = RequiredPaths.Where(path => FindPath(roots, path) == null).ToArray();
            var missingScripts = allObjects.Sum(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount);
            var canvasCount = allObjects.Sum(go => go.GetComponents<Canvas>().Length);
            var rootComponentViolations = roots
                .Where(root => root.GetComponents<Component>().Any(component => component != null && !(component is Transform)))
                .Select(root => root.name)
                .ToArray();

            var transparentLayer = LayerMask.NameToLayer("TransparentFX");
            var captureBackdrop = FindPath(roots, "Spaces/EtherBubbleLab/BackgroundPlane_Group/CaptureBackdrop_TUNE");
            var staticGeometry = FindPath(roots, "Spaces/EtherBubbleLab/BackgroundPlane_Group/StaticGeometry_Group");
            var backgroundSamples = FindPath(roots, "DebugAndReferences/Sample_BackgroundContent");
            var backgroundObjects = Descendants(captureBackdrop)
                .Concat(Descendants(staticGeometry))
                .Concat(Descendants(backgroundSamples))
                .Distinct()
                .ToArray();
            var backgroundLayerViolations = backgroundObjects
                .Where(go => transparentLayer < 0 || go.layer != transparentLayer)
                .Select(GetPath)
                .ToArray();

            var bubbleLab = FindPath(roots, "Spaces/EtherBubbleLab");
            var backgroundGroup = FindPath(roots, "Spaces/EtherBubbleLab/BackgroundPlane_Group");
            var foregroundGroup = FindPath(roots, "Spaces/EtherBubbleLab/ForegroundPlane_Group");
            var backgroundRendererOrders = Descendants(backgroundGroup)
                .SelectMany(go => go.GetComponents<Renderer>())
                .Where(renderer => renderer != null)
                .Select(renderer => renderer.sortingOrder)
                .ToArray();
            var foregroundRendererOrders = Descendants(foregroundGroup)
                .SelectMany(go => go.GetComponents<Renderer>())
                .Where(renderer => renderer != null)
                .Select(renderer => renderer.sortingOrder)
                .ToArray();
            var sortingValid = backgroundRendererOrders.Length > 0 &&
                backgroundRendererOrders.All(order => order >= -200 && order <= -100) &&
                foregroundRendererOrders.Length > 0 &&
                foregroundRendererOrders.All(order => order >= 100 && order <= 200);

            var mainCamera = ComponentAt<Camera>(roots, "CameraAndStageMarkers/MainCamera");
            var backgroundCamera = ComponentAt<Camera>(roots, "CameraAndStageMarkers/BackgroundCaptureCamera");
            var globalLightObject = FindPath(roots, "CameraAndStageMarkers/GlobalLight2D");
            var capture = ComponentAt<SceneColorCapture2D>(roots, "Managers/SceneColorCapture");
            var emitter = bubbleLab == null ? null : bubbleLab.GetComponentInChildren<EtherBubbleEmitter2D>(true);
            var driver = ComponentAt<VisualEffectsLabDriver>(roots, "DebugAndReferences/Probe_VisualEffectsLab");
            var expectedMask = transparentLayer < 0 ? 0 : 1 << transparentLayer;
            var cameraIsolationValid = mainCamera != null && backgroundCamera != null &&
                globalLightObject != null && globalLightObject.layer == transparentLayer &&
                capture != null && capture.MainCamera == mainCamera && capture.BackgroundCamera == backgroundCamera &&
                capture.BackgroundLayerMask.value == expectedMask && backgroundCamera.cullingMask == expectedMask &&
                (mainCamera.cullingMask & expectedMask) != 0 && (mainCamera.cullingMask & 1) != 0 &&
                backgroundCamera.depth < mainCamera.depth && backgroundCamera.targetTexture == null;
            var emitterWiringValid = emitter != null && emitter.MouthAnchor != null && emitter.SpawnParent != null &&
                emitter.BubblePrefab != null && emitter.CaptureSource == capture && emitter.MaxBubbles >= 16;
            var driverWiringValid = driver != null && driver.Emitter == emitter && driver.CaptureSource == capture;

            var debugRoot = roots.FirstOrDefault(root => root.name == "DebugAndReferences");
            var debugDefaultInactive = debugRoot != null && !debugRoot.activeSelf;
            var interactionRoot = roots.FirstOrDefault(root => root.name == "InteractionLayers");
            var orphanMouseManagerCount = interactionRoot == null
                ? -1
                : interactionRoot.GetComponentsInChildren<MonoBehaviour>(true)
                    .Count(component => component != null && component.GetType().Name == "MouseManager");

            var success = scene.path == VisualEffectsLabBuilder.ScenePath && rootOrderValid &&
                missingPaths.Length == 0 && missingScripts == 0 && canvasCount == 0 &&
                rootComponentViolations.Length == 0 && backgroundLayerViolations.Length == 0 && sortingValid &&
                cameraIsolationValid && emitterWiringValid && driverWiringValid && debugDefaultInactive &&
                orphanMouseManagerCount == 0 && !scene.isDirty;

            var summary = "VFX-LAB-AUDIT" +
                " success=" + success +
                " scene=" + scene.path +
                " roots=" + string.Join(",", rootNames) +
                " rootOrderValid=" + rootOrderValid +
                " missingPaths=" + string.Join("|", missingPaths) +
                " missingScripts=" + missingScripts +
                " canvas=" + canvasCount +
                " rootComponentViolations=" + string.Join("|", rootComponentViolations) +
                " transparentFxLayer=" + transparentLayer +
                " backgroundLayerViolations=" + string.Join("|", backgroundLayerViolations) +
                " sortingValid=" + sortingValid +
                " globalLightLayerValid=" + (globalLightObject != null && globalLightObject.layer == transparentLayer) +
                " cameraIsolation=" + cameraIsolationValid +
                " emitterWiring=" + emitterWiringValid +
                " driverWiring=" + driverWiringValid +
                " debugDefaultInactive=" + debugDefaultInactive +
                " orphanMouseManager=" + orphanMouseManagerCount +
                " sceneDirty=" + scene.isDirty;
            return new VisualEffectsLabAuditResult(success, summary);
        }

        private static IEnumerable<GameObject> Descendants(GameObject root)
        {
            return root == null
                ? Enumerable.Empty<GameObject>()
                : root.GetComponentsInChildren<Transform>(true).Select(item => item.gameObject);
        }

        private static T ComponentAt<T>(GameObject[] roots, string path) where T : Component
        {
            var gameObject = FindPath(roots, path);
            return gameObject == null ? null : gameObject.GetComponent<T>();
        }

        private static GameObject FindPath(GameObject[] roots, string path)
        {
            var segments = path.Split('/');
            var current = roots.FirstOrDefault(root => root.name == segments[0]);
            for (var index = 1; current != null && index < segments.Length; index++)
            {
                var child = current.transform.Cast<Transform>()
                    .FirstOrDefault(candidate => candidate.name == segments[index]);
                current = child == null ? null : child.gameObject;
            }
            return current;
        }

        private static string GetPath(GameObject gameObject)
        {
            var names = new List<string>();
            for (var current = gameObject.transform; current != null; current = current.parent)
                names.Add(current.name);
            names.Reverse();
            return string.Join("/", names);
        }
    }

    public struct VisualEffectsLabAuditResult
    {
        public readonly bool Success;
        public readonly string Summary;

        public VisualEffectsLabAuditResult(bool success, string summary)
        {
            Success = success;
            Summary = summary;
        }
    }
}
