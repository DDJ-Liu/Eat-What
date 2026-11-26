using System;
using System.IO;
using EatWhat.Tools.Debugging;
using EatWhat.Tools.Rendering;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace EatWhat.EditorTools.VisualEffects
{
    /// <summary>
    /// Explicit-only creator for the one VisualEffectsLab scene. There are no
    /// InitializeOnLoad, AssetPostprocessor or import callbacks in this type.
    /// </summary>
    public static class VisualEffectsLabBuilder
    {
        public const string ScenePath = "Assets/Scenes/ToolTests/VisualEffectsLab.unity";
        public const string PrefabPath = "Assets/Prefabs/VisualEffects/EtherBubble.prefab";
        public const string MaterialPath = "Assets/Materials/VisualEffects/EtherBubble.mat";
        public const string FixtureSpritePath = "Assets/Tests/VisualEffects/Fixtures/VfxLabWhite.png";
        public const string ShaderName = "EatWhat/2D/Ether Bubble Distortion";

        private const string InputActionsPath = "Assets/Scripts/Controller/PlayerControl.inputactions";
        private const string OutlineMaterialPath = "Assets/Materials/2D/SpriteOutline2D.mat";
        private const string ChineseFontPath = "Assets/Font/Heavy/ShortCycle_Heavy_WithFallback.asset";

        [MenuItem("Tools/Visual Effects Lab/Build or Open Lab")]
        public static void BuildOrOpenFromMenu()
        {
            BuildOrOpenLab();
        }

        /// <summary>
        /// Creates the scene only when absent. Repeated calls open the existing Lab
        /// without overwriting _TUNE positions or later human adjustments.
        /// </summary>
        public static void BuildOrOpenLab()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before building VisualEffectsLab.");

            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid() && activeScene.isDirty)
                throw new InvalidOperationException(
                    "The active scene is dirty. Save or revert it explicitly before opening VisualEffectsLab.");

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                Debug.Log("VFX-LAB-BUILD existing scene opened without modification: " + ScenePath);
                return;
            }

            EnsureFolder("Assets/Scenes", "ToolTests");
            EnsureFolder("Assets/Prefabs", "VisualEffects");
            EnsureFolder("Assets/Materials", "VisualEffects");
            EnsureFolder("Assets", "Tests");
            EnsureFolder("Assets/Tests", "VisualEffects");
            EnsureFolder("Assets/Tests/VisualEffects", "Fixtures");

            var fixtureSprite = EnsureFixtureSprite();
            var bubbleMaterial = EnsureBubbleMaterial();
            var bubblePrefab = EnsureBubblePrefab(fixtureSprite, bubbleMaterial);
            var transparentFxLayer = LayerMask.NameToLayer("TransparentFX");
            if (transparentFxLayer < 0)
                throw new InvalidOperationException("Existing TransparentFX layer is required; TagManager is frozen.");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraAndMarkers = NewRoot("CameraAndStageMarkers");
            var managers = NewRoot("Managers");
            NewRoot("InteractionLayers");
            var spaces = NewRoot("Spaces");
            var shared = NewRoot("Shared");
            var debugAndReferences = NewRoot("DebugAndReferences");
            NewRoot("EventSystem");

            var mainCameraObject = NewChild("MainCamera", cameraAndMarkers.transform, new Vector3(0f, 0f, -10f));
            mainCameraObject.tag = "MainCamera";
            var mainCamera = mainCameraObject.AddComponent<Camera>();
            mainCamera.orthographic = true;
            mainCamera.orthographicSize = 7.2f;
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = new Color(0.075f, 0.09f, 0.13f, 1f);
            mainCamera.nearClipPlane = 0.1f;
            mainCamera.farClipPlane = 100f;
            mainCamera.depth = 0f;
            mainCamera.cullingMask = ~0;
            mainCameraObject.AddComponent<AudioListener>();
            mainCameraObject.AddComponent<UniversalAdditionalCameraData>().renderType = CameraRenderType.Base;

            var captureCameraObject = NewChild(
                "BackgroundCaptureCamera",
                cameraAndMarkers.transform,
                mainCameraObject.transform.position);
            var captureCamera = captureCameraObject.AddComponent<Camera>();
            captureCamera.orthographic = true;
            captureCamera.orthographicSize = mainCamera.orthographicSize;
            captureCamera.clearFlags = CameraClearFlags.SolidColor;
            captureCamera.backgroundColor = mainCamera.backgroundColor;
            captureCamera.nearClipPlane = mainCamera.nearClipPlane;
            captureCamera.farClipPlane = mainCamera.farClipPlane;
            captureCamera.depth = -1f;
            captureCamera.cullingMask = 1 << transparentFxLayer;
            captureCamera.forceIntoRenderTexture = true;
            captureCameraObject.AddComponent<UniversalAdditionalCameraData>().renderType = CameraRenderType.Base;

            var lightObject = NewChild("GlobalLight2D", cameraAndMarkers.transform, Vector3.zero);
            lightObject.layer = transparentFxLayer;
            var globalLight = lightObject.AddComponent<Light2D>();
            globalLight.lightType = Light2D.LightType.Global;
            globalLight.intensity = 1f;
            NewChild("ViewCenterMarker", cameraAndMarkers.transform, Vector3.zero);

            var inputManagerObject = NewChild("InputManager", managers.transform, Vector3.zero);
            var playerInput = inputManagerObject.AddComponent<PlayerInput>();
            playerInput.actions = LoadRequired<InputActionAsset>(InputActionsPath);
            playerInput.notificationBehavior = PlayerNotifications.SendMessages;
            inputManagerObject.AddComponent<InputManager>();

            var captureManagerObject = NewChild("SceneColorCapture", managers.transform, Vector3.zero);
            var capture = captureManagerObject.AddComponent<SceneColorCapture2D>();
            capture.Configure(mainCamera, captureCamera, 1 << transparentFxLayer, 1f, 1);

            var etherBubbleLab = NewChild("EtherBubbleLab", spaces.transform, Vector3.zero);
            var backgroundGroup = NewChild("BackgroundPlane_Group", etherBubbleLab.transform, Vector3.zero);
            AddSprite(
                "CaptureBackdrop_TUNE",
                backgroundGroup.transform,
                fixtureSprite,
                new Vector3(0f, 0f, 0f),
                new Vector2(24f, 13f),
                new Color(0.16f, 0.22f, 0.31f, 1f),
                -200,
                transparentFxLayer);
            var geometryGroup = NewChild("StaticGeometry_Group", backgroundGroup.transform, Vector3.zero);
            geometryGroup.layer = transparentFxLayer;
            for (var index = -5; index <= 5; index++)
            {
                AddSprite(
                    "ReferenceLine_" + (index + 6).ToString("00"),
                    geometryGroup.transform,
                    fixtureSprite,
                    new Vector3(index * 2f, 0f, 0f),
                    new Vector2(0.035f, 11.5f),
                    new Color(0.27f, 0.45f, 0.58f, 0.75f),
                    -150,
                    transparentFxLayer);
            }

            var emitterGroup = NewChild("Emitter_Group", etherBubbleLab.transform, Vector3.zero);
            var mouthAnchor = NewChild("MouthAnchor_TUNE", emitterGroup.transform, new Vector3(-8.2f, -2.4f, 0f));
            var spawnParent = NewChild("BubbleSpawnParent", emitterGroup.transform, Vector3.zero);
            var emitter = emitterGroup.AddComponent<EtherBubbleEmitter2D>();
            emitter.Configure(mouthAnchor.transform, spawnParent.transform, bubblePrefab, capture);
            emitter.SetMaxBubbles(16);

            var foregroundGroup = NewChild("ForegroundPlane_Group", etherBubbleLab.transform, Vector3.zero);
            AddSprite(
                "OcclusionBar_TUNE",
                foregroundGroup.transform,
                fixtureSprite,
                new Vector3(5.2f, 0f, -0.05f),
                new Vector2(1.35f, 8.5f),
                new Color(0.94f, 0.47f, 0.28f, 1f),
                150,
                0);

            NewChild("Reserved_Group", shared.transform, Vector3.zero);

            var backgroundSamples = NewChild("Sample_BackgroundContent", debugAndReferences.transform, Vector3.zero);
            backgroundSamples.layer = transparentFxLayer;
            var checker = NewChild("CheckerGrid", backgroundSamples.transform, new Vector3(-1.8f, 0.2f, 0f));
            checker.layer = transparentFxLayer;
            var gridLayout = checker.AddComponent<GridLayout>();
            SetInt(gridLayout, "columnCount", 8);
            SetFloat(gridLayout, "horizontalSpacing", 0.08f);
            SetFloat(gridLayout, "verticalSpacing", 0.08f);
            SetBool(gridLayout, "layoutOnStart", false);
            for (var index = 0; index < 32; index++)
            {
                var color = ((index + index / 8) & 1) == 0
                    ? new Color(0.76f, 0.86f, 0.92f, 0.8f)
                    : new Color(0.12f, 0.18f, 0.25f, 0.8f);
                AddSprite(
                    "GridCell_" + (index + 1).ToString("00"),
                    checker.transform,
                    fixtureSprite,
                    Vector3.zero,
                    new Vector2(1.15f, 1.15f),
                    color,
                    -125,
                    transparentFxLayer);
            }
            gridLayout.SetCellSize(new Vector2(1.15f, 1.15f));
            gridLayout.ArrangeChildren();

            AddSprite(
                "TransparentSprite",
                backgroundSamples.transform,
                fixtureSprite,
                new Vector3(5.5f, 2.8f, 0f),
                new Vector2(3.2f, 1.2f),
                new Color(0.3f, 0.95f, 0.8f, 0.48f),
                -115,
                transparentFxLayer);
            var animatedSample = AddSprite(
                "AnimatedSprite",
                backgroundSamples.transform,
                fixtureSprite,
                new Vector3(3.8f, -2.2f, 0f),
                new Vector2(1.4f, 1.4f),
                new Color(0.95f, 0.8f, 0.28f, 0.8f),
                -112,
                transparentFxLayer);
            var outlinedSample = AddSprite(
                "OutlineAndShadowSprite",
                backgroundSamples.transform,
                fixtureSprite,
                new Vector3(-6f, 3.2f, 0f),
                new Vector2(1.6f, 1.6f),
                Color.white,
                -110,
                transparentFxLayer);
            var outline = outlinedSample.AddComponent<SpriteOutline2D>();
            outline.Configure(LoadRequired<Material>(OutlineMaterialPath), new Color(0.08f, 0.05f, 0.04f, 1f), 5f);
            outline.ConfigureShadow(true, Color.black, new Vector2(4f, -4f), 0.45f, 2f);
            AddWorldText(
                "ChineseText",
                backgroundSamples.transform,
                "透明动画 · 醚质泡泡",
                new Vector3(-5.6f, -3.8f, 0f),
                3.2f,
                -105,
                transparentFxLayer);

            var character = NewChild("Sample_Character", debugAndReferences.transform, Vector3.zero);
            AddSprite(
                "MouthVisual_TUNE",
                character.transform,
                fixtureSprite,
                mouthAnchor.transform.position + new Vector3(-0.5f, 0f, -0.05f),
                new Vector2(1.2f, 0.6f),
                new Color(0.95f, 0.62f, 0.68f, 1f),
                120,
                0);

            var status = AddWorldText(
                "Probe_StatusText",
                debugAndReferences.transform,
                "ETHER BUBBLE LAB · VERIFY-TEMP",
                new Vector3(-11.8f, 6.4f, -0.1f),
                1.8f,
                500,
                0);
            var probe = NewChild("Probe_VisualEffectsLab", debugAndReferences.transform, Vector3.zero);
            var driver = probe.AddComponent<VisualEffectsLabDriver>();
            SetObject(driver, "emitter", emitter);
            SetObject(driver, "captureSource", capture);
            SetObject(driver, "statusText", status);
            SetObject(driver, "animatedSample", animatedSample.transform);
            driver.enabled = true;
            debugAndReferences.SetActive(false);

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException("Unity failed to save " + ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("VFX-LAB-BUILD PASS scene=" + ScenePath + " canvas=0 bubbleCap=16");
        }

        private static Sprite EnsureFixtureSprite()
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(FixtureSpritePath);
            if (sprite != null) return sprite;

            var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false, false);
            var pixels = new Color32[64 * 64];
            for (var index = 0; index < pixels.Length; index++) pixels[index] = new Color32(255, 255, 255, 255);
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            File.WriteAllBytes(FixtureSpritePath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(FixtureSpritePath, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(FixtureSpritePath) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Fixture TextureImporter is unavailable.");
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 64f;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return LoadRequired<Sprite>(FixtureSpritePath);
        }

        private static Material EnsureBubbleMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material != null) return material;
            var shader = Shader.Find(ShaderName);
            if (shader == null) throw new InvalidOperationException("Shader import is required before building: " + ShaderName);
            material = new Material(shader) { name = "EtherBubble" };
            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }

        private static EtherBubbleDistortion2D EnsureBubblePrefab(Sprite sprite, Material material)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (existing != null)
            {
                var existingBubble = existing.GetComponent<EtherBubbleDistortion2D>();
                if (existingBubble == null)
                    throw new InvalidOperationException("Existing EtherBubble prefab lacks EtherBubbleDistortion2D.");
                return existingBubble;
            }

            var root = new GameObject("EtherBubble");
            try
            {
                var renderer = root.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sharedMaterial = material;
                renderer.sortingOrder = 10;
                var bubble = root.AddComponent<EtherBubbleDistortion2D>();
                bubble.Configure(material, null);
                var saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                if (saved == null) throw new InvalidOperationException("Unity failed to save " + PrefabPath);
                return saved.GetComponent<EtherBubbleDistortion2D>();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static GameObject NewRoot(string name)
        {
            return new GameObject(name);
        }

        private static GameObject NewChild(string name, Transform parent, Vector3 localPosition)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = localPosition;
            return gameObject;
        }

        private static GameObject AddSprite(
            string name,
            Transform parent,
            Sprite sprite,
            Vector3 localPosition,
            Vector2 size,
            Color color,
            int sortingOrder,
            int layer)
        {
            var gameObject = NewChild(name, parent, localPosition);
            gameObject.layer = layer;
            gameObject.transform.localScale = new Vector3(size.x, size.y, 1f);
            var renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return gameObject;
        }

        private static TextMeshPro AddWorldText(
            string name,
            Transform parent,
            string value,
            Vector3 localPosition,
            float fontSize,
            int sortingOrder,
            int layer)
        {
            var gameObject = NewChild(name, parent, localPosition);
            gameObject.layer = layer;
            var text = gameObject.AddComponent<TextMeshPro>();
            text.font = LoadRequired<TMP_FontAsset>(ChineseFontPath);
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = false;
            var renderer = text.GetComponent<MeshRenderer>();
            if (renderer != null) renderer.sortingOrder = sortingOrder;
            return text;
        }

        private static void EnsureFolder(string parent, string child)
        {
            var path = parent + "/" + child;
            if (AssetDatabase.IsValidFolder(path)) return;
            var guid = AssetDatabase.CreateFolder(parent, child);
            if (string.IsNullOrEmpty(guid) || !AssetDatabase.IsValidFolder(path))
                throw new InvalidOperationException("Could not create Unity folder: " + path);
        }

        private static T LoadRequired<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) throw new InvalidOperationException("Required asset is missing: " + path);
            return asset;
        }

        private static SerializedProperty Property(UnityEngine.Object target, string name)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(name);
            if (property == null) throw new InvalidOperationException(target.GetType().Name + "." + name + " is missing.");
            return property;
        }

        private static void SetObject(UnityEngine.Object target, string name, UnityEngine.Object value)
        {
            var property = Property(target, name);
            property.objectReferenceValue = value;
            property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetInt(UnityEngine.Object target, string name, int value)
        {
            var property = Property(target, name);
            property.intValue = value;
            property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(UnityEngine.Object target, string name, float value)
        {
            var property = Property(target, name);
            property.floatValue = value;
            property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetBool(UnityEngine.Object target, string name, bool value)
        {
            var property = Property(target, name);
            property.boolValue = value;
            property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
