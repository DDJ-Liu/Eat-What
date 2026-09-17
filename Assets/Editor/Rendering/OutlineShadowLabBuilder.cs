using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EatWhat.Cooking.ShortCycle.Debugging;
using EatWhat.Tools.Rendering;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace EatWhat.EditorTools.Rendering
{
    /// <summary>Explicit, idempotent increment of the shared VisualEffectsLab scene.</summary>
    public static class OutlineShadowLabBuilder
    {
        public const string ScenePath = "Assets/Scenes/ToolTests/VisualEffectsLab.unity";
        public const string LabPath = "Spaces/OutlineShadowMergeLab";
        public const string ForwardGroupPath = LabPath + "/SampleMatrix_Group/Forward_Group";
        public const string Forward2Path = ForwardGroupPath + "/Forward_2";
        public const string Forward2MarkerPath = "CameraAndStageMarkers/CameraMarker_Forward2";
        public const string PaddedFixturePath = "Assets/Tests/VisualEffects/Fixtures/VfxLabOutlinePadded.png";
        public const int PaddedFixtureSize = 128;
        public const int PaddedFixtureOpaqueSize = 64;
        public const int PaddedFixtureBorder = 32;
        public const float PaddedFixturePixelsPerUnit = 64f;
        public const float Forward2ReviewOrthographicSize = 2.5f;
        private const string OriginalFixturePath = "Assets/Tests/VisualEffects/Fixtures/VfxLabWhite.png";
        private const string ShaderName = "EatWhat/2D/Sprite Outline Merge 2D";
        private const string OriginalOutlineMaterialPath = "Assets/Materials/2D/SpriteOutline2D.mat";
        private const string DefaultsPath = "Assets/Resources/SpriteOutlineDefaults.asset";
        private static readonly string[] CatPrefabPaths =
        {
            "Assets/Prefabs/Cooking/ShortCycle/FridgeCat/FridgeCat_Rig.prefab",
            "Assets/Prefabs/Cooking/ShortCycle/FridgeCat/FridgeCat_Tier.prefab",
            "Assets/Prefabs/Cooking/ShortCycle/FridgeCat/FridgeCat_Eye.prefab"
        };

        [MenuItem("Tools/Visual Effects Lab/Create Sprite Outline Defaults (Once)")]
        public static void CreateDefaultsOnce()
        {
            SpriteOutlineDefaults existing = AssetDatabase.LoadAssetAtPath<SpriteOutlineDefaults>(DefaultsPath);
            if (existing != null)
            {
                Selection.activeObject = existing;
                Debug.Log("SPRITE-OUTLINE-DEFAULTS existing asset opened without modification: " + DefaultsPath);
                return;
            }
            EnsureAssetFolder("Assets", "Resources");
            SpriteOutlineDefaults defaults = ScriptableObject.CreateInstance<SpriteOutlineDefaults>();
            defaults.ConfigureGeneric(LoadRequired<Material>(OriginalOutlineMaterialPath),
                new Color32(48, 35, 31, 255), 6f);
            AssetDatabase.CreateAsset(defaults, DefaultsPath);
            AssetDatabase.SaveAssets();
            Selection.activeObject = defaults;
            Debug.Log("SPRITE-OUTLINE-DEFAULTS created once: " + DefaultsPath);
        }

        [MenuItem("Tools/Visual Effects Lab/Apply Forward_2 Independent Outline Fix")]
        public static void ApplyForward2IndependentOutlineFix()
        {
            Scene scene = OpenRequiredScene();
            GameObject forwardGroupObject = FindPath(scene, ForwardGroupPath);
            GameObject forward2Object = FindPath(scene, Forward2Path);
            GameObject cameraObject = FindPath(scene, "CameraAndStageMarkers/MainCamera");
            GameObject probeObject = FindPath(scene, "DebugAndReferences/Probe_OutlineShadowMergeLab");
            Camera camera = cameraObject == null ? null : cameraObject.GetComponent<Camera>();
            OutlineShadowLabDriver driver = probeObject == null ? null : probeObject.GetComponent<OutlineShadowLabDriver>();
            Material outlineMaterial = AssetDatabase.LoadAssetAtPath<Material>(OriginalOutlineMaterialPath);
            ValidateForward2Baseline(forwardGroupObject, forward2Object, camera, driver, outlineMaterial);
            ValidateExistingPaddedFixtureOwnership();

            Sprite paddedFixture = EnsurePaddedFixtureAsset();
            string fixtureError;
            if (!TryValidatePaddedFixture(out fixtureError))
                throw new InvalidOperationException("Forward_2 fixture validation failed before scene mutation: " + fixtureError);
            bool changed = EnsureForward2FormalConfiguration(forwardGroupObject, forward2Object,
                paddedFixture, outlineMaterial);
            Transform marker = EnsureForward2CameraMarker(scene, camera, forward2Object, ref changed);
            if (driver.ConfigureForward2Review(camera, marker, Forward2ReviewOrthographicSize))
            {
                EditorUtility.SetDirty(driver);
                changed = true;
            }

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                    throw new InvalidOperationException("Unity failed to save " + ScenePath);
            }

            Selection.activeGameObject = forward2Object;
            SceneView.lastActiveSceneView?.FrameSelected();
            Debug.Log(changed
                ? "FORWARD-2-FIX applied formal authority, padded FullRect fixture, and reversible Play camera."
                : "FORWARD-2-FIX already matches the approved configuration; no scene rewrite.");
        }

        [MenuItem("Tools/Visual Effects Lab/Add or Open Outline Shadow Merge Samples")]
        public static void BuildOrOpen()
        {
            var scene = OpenRequiredScene();
            var existing = FindPath(scene, LabPath);
            var cameraObject = FindPath(scene, "CameraAndStageMarkers/MainCamera");
            var sampleSource = FindPath(scene, "DebugAndReferences/Sample_BackgroundContent/OutlineAndShadowSprite");
            var camera = cameraObject == null ? null : cameraObject.GetComponent<Camera>();
            var sprite = sampleSource == null ? null : sampleSource.GetComponent<SpriteRenderer>()?.sprite;
            var shader = Shader.Find(ShaderName);
            var originalOutlineMaterial = AssetDatabase.LoadAssetAtPath<Material>(OriginalOutlineMaterialPath);
            if (camera == null || sprite == null || shader == null || originalOutlineMaterial == null)
                throw new InvalidOperationException("Camera, fixture sprite, or imported merge shader is missing.");
            if (existing != null)
            {
                bool upgraded = EnsureFormalContent(scene, existing, camera, shader, sprite, originalOutlineMaterial);
                if (upgraded)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Unity failed to save " + ScenePath);
                }
                Selection.activeGameObject = existing;
                SceneView.lastActiveSceneView?.FrameSelected();
                Debug.Log(upgraded
                    ? "OUTLINE-LAB-BUILD upgraded formal samples while preserving existing content: " + LabPath
                    : "OUTLINE-LAB-BUILD existing sample opened without modification: " + LabPath);
                return;
            }

            var spaces = FindPath(scene, "Spaces");
            if (spaces == null || cameraObject == null || sampleSource == null)
                throw new InvalidOperationException("Run the base VisualEffectsLab Builder before this incremental Builder.");

            var root = NewChild("OutlineShadowMergeLab", spaces.transform, new Vector3(0f, -8f, 0f));
            var matrix = NewChild("SampleMatrix_Group", root.transform, Vector3.zero);
            var forwardGroup = BuildGroup("Forward_Group", matrix.transform, new Vector3(-5f, 0f, 0f), camera, shader, sprite, originalOutlineMaterial, false);
            var reverseGroup = BuildGroup("Reverse_Group", matrix.transform, new Vector3(0f, 0f, 0f), camera, shader, sprite, originalOutlineMaterial, true);
            var crossingGroup = BuildGroup("DifferentGroup_Crossing", matrix.transform, new Vector3(5f, 0f, 0f), camera, shader, sprite, originalOutlineMaterial, false);
            var original = NewChild("OriginalSingleItem_Comparison", root.transform, new Vector3(-5f, -3f, 0f));
            CopySprite(sampleSource.GetComponent<SpriteRenderer>(), original.AddComponent<SpriteRenderer>(), 40);
            var originalOutline = original.AddComponent<SpriteOutline2D>();
            originalOutline.Configure(originalOutlineMaterial, new Color(0.15f, 0.9f, 1f, 1f), 2f);
            originalOutline.ConfigureShadow(true, Color.black, new Vector2(3f, -3f), 0.55f, 2f);
            var seam = NewChild("CatRig_Seam_4x_Comparison", root.transform, new Vector3(0f, -3f, 0f));
            seam.AddComponent<SpriteRenderer>().sprite = sprite;
            var unsupported = NewChild("Unsupported_SortingDomain_Counterexample", root.transform, new Vector3(5f, -3f, 0f));
            unsupported.AddComponent<SpriteRenderer>().sprite = sprite;
            unsupported.AddComponent<SortingGroup>();
            unsupported.GetComponent<SpriteRenderer>().sortingLayerName = "Default";
            unsupported.GetComponent<SpriteRenderer>().sortingOrder = short.MinValue;

            var debugRoot = FindPath(scene, "DebugAndReferences");
            var probe = NewChild("Probe_OutlineShadowMergeLab", debugRoot.transform, Vector3.zero);
            var statusObject = NewChild("OutlineShadowMerge_Status", probe.transform, new Vector3(0f, -5f, 0f));
            var status = statusObject.AddComponent<TextMeshPro>();
            status.text = "OUTLINE MERGE LAB · BUILD COMPLETE / PLAY NOT RUN";
            status.fontSize = 2f;
            var driver = probe.AddComponent<OutlineShadowLabDriver>();
            driver.Configure(forwardGroup.Renderer, crossingGroup.Renderer, forwardGroup.Members, reverseGroup.Members, original, seam, status);
            probe.SetActive(false);
            EnsureFormalContent(scene, root, camera, shader, sprite, originalOutlineMaterial);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Unity failed to save " + ScenePath);
            Selection.activeGameObject = root;
            Debug.Log("OUTLINE-LAB-BUILD PASS groups=3 physicalRtBudgetPerGroup=2; Play/GPU review not run.");
        }

        private static bool EnsureFormalContent(Scene scene, GameObject labRoot, Camera camera,
            Shader shader, Sprite sprite, Material outlineMaterial)
        {
            var formalRoot = labRoot.transform.Cast<Transform>()
                .FirstOrDefault(item => item.name == "FormalComponentMatrix_Group");
            if (formalRoot != null)
                return false;

            var root = NewChild("FormalComponentMatrix_Group", labRoot.transform, new Vector3(0f, -6f, 0f));
            FormalGroupBuildResult localFollow = BuildFormalGroup("ParametersLocal_FollowGroup", root.transform,
                new Vector3(-3f, 0f, 0f), camera, shader, sprite, outlineMaterial, true, false);
            FormalGroupBuildResult groupForceOn = BuildFormalGroup("ParametersGroup_ForceOn_Nested",
                localFollow.Group.transform, new Vector3(6f, 0f, 0f), camera, shader, sprite, outlineMaterial,
                false, true);
            var fixtures = BuildIsolatedCatFixtures(root.transform, camera, shader, outlineMaterial);
            string[] markerNames = { "CameraMarker_FormalMatrix", "CameraMarker_TierHead4x",
                "CameraMarker_EarHead4x", "CameraMarker_ForceOff" };
            for (var index = 0; index < markerNames.Length; index++)
                NewChild(markerNames[index], root.transform, new Vector3(index * 2f - 3f, -4f, 0f));

            GameObject probe = FindPath(scene, "DebugAndReferences/Probe_OutlineShadowMergeLab");
            OutlineShadowLabDriver driver = probe == null ? null : probe.GetComponent<OutlineShadowLabDriver>();
            if (driver == null)
                throw new InvalidOperationException("Existing Outline Shadow Lab driver is missing; refusing to overwrite the Lab.");
            driver.ConfigureFormal(localFollow.Group, groupForceOn.Group, fixtures);
            return true;
        }

        private static FormalGroupBuildResult BuildFormalGroup(string name, Transform parent, Vector3 position,
            Camera camera, Shader shader, Sprite sprite, Material outlineMaterial,
            bool groupMergeEnabled, bool forceOn)
        {
            var root = NewChild(name, parent, position);
            var group = root.AddComponent<SpriteOutlineGroup2D>();
            group.Configure(outlineMaterial, new Color(0.2f, 0.85f, 1f, 1f), 3f);
            group.ConfigureShadow(true, new Color(0.05f, 0.03f, 0.08f, 1f),
                new Vector2(3f, -3f), 0.55f, 2f);
            group.MergeOutlineEnabled = groupMergeEnabled;
            group.MergeShadowEnabled = groupMergeEnabled;

            var backend = root.AddComponent<SpriteOutlineMergeRenderer2D>();
            var shadowHostObject = NewChild(name + "_MergedShadow", root.transform, Vector3.zero);
            var shadowMesh = shadowHostObject.AddComponent<MeshFilter>();
            var shadowHost = shadowHostObject.AddComponent<MeshRenderer>();
            var outlineHostObject = NewChild(name + "_MergedOutline", root.transform, Vector3.zero);
            var outlineMesh = outlineHostObject.AddComponent<MeshFilter>();
            var outlineHost = outlineHostObject.AddComponent<MeshRenderer>();

            for (var index = 0; index < 2; index++)
            {
                var memberObject = NewChild((forceOn ? "GroupParams_ForceOn_" : "LocalParams_FollowGroup_") + index,
                    root.transform, new Vector3(index * 0.8f - 0.4f, 0f, 0f));
                var source = memberObject.AddComponent<SpriteRenderer>();
                source.sprite = sprite;
                source.sortingOrder = 60 + index;
                var outline = memberObject.AddComponent<SpriteOutline2D>();
                outline.Configure(outlineMaterial, index == 0 ? Color.magenta : Color.yellow, 1f + index);
                outline.ConfigureShadow(true, Color.black, new Vector2(2f, -2f), 0.45f, 1f);
                outline.SetOverrideGroup(forceOn);
                outline.SetMergeModes(forceOn ? OutlineMergeOverride.ForceOn : OutlineMergeOverride.FollowGroup,
                    index == 1 ? OutlineMergeOverride.ForceOff :
                        (forceOn ? OutlineMergeOverride.ForceOn : OutlineMergeOverride.FollowGroup));
            }

            group.CreateOrSynchronizeMergeAdapters();
            SpriteOutlineMergeMember2D[] adapters = group.GetOwnedMergeAdapters();
            backend.Configure(camera, shader, shadowHost, shadowMesh, outlineHost, outlineMesh, adapters);
            group.SynchronizeMergeBackendConfiguration();
            shadowHost.sortingOrder = 58;
            outlineHost.sortingOrder = 59;
            return new FormalGroupBuildResult(group, backend);
        }

        private static GameObject BuildIsolatedCatFixtures(Transform parent, Camera camera,
            Shader shader, Material outlineMaterial)
        {
            var fixtureRoot = NewChild("IsolatedFormalCatFixtures", parent, new Vector3(0f, -8f, 0f));
            for (var index = 0; index < CatPrefabPaths.Length; index++)
            {
                GameObject prefab = LoadRequired<GameObject>(CatPrefabPaths[index]);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, fixtureRoot.transform);
                instance.name = "Formal_" + prefab.name + "_4x";
                instance.transform.localPosition = new Vector3((index - 1) * 4f, 0f, 0f);
                instance.transform.localScale = Vector3.one * 4f;
                DisableUnrelatedBehaviours(instance);
                BuildFormalBackendOnExistingRoot(instance, camera, shader, outlineMaterial);
            }
            fixtureRoot.SetActive(false);
            return fixtureRoot;
        }

        private static void BuildFormalBackendOnExistingRoot(GameObject root, Camera camera,
            Shader shader, Material outlineMaterial)
        {
            var group = root.GetComponent<SpriteOutlineGroup2D>() ?? root.AddComponent<SpriteOutlineGroup2D>();
            group.Configure(outlineMaterial, new Color(0.2f, 0.85f, 1f, 1f), 3f);
            group.ConfigureShadow(true, Color.black, new Vector2(3f, -3f), 0.55f, 2f);
            var backend = root.GetComponent<SpriteOutlineMergeRenderer2D>() ?? root.AddComponent<SpriteOutlineMergeRenderer2D>();
            var shadowObject = NewChild(root.name + "_MergedShadow", root.transform, Vector3.zero);
            var shadowMesh = shadowObject.AddComponent<MeshFilter>();
            var shadowHost = shadowObject.AddComponent<MeshRenderer>();
            var outlineObject = NewChild(root.name + "_MergedOutline", root.transform, Vector3.zero);
            var outlineMesh = outlineObject.AddComponent<MeshFilter>();
            var outlineHost = outlineObject.AddComponent<MeshRenderer>();
            group.CreateMissingMembers();
            group.CreateOrSynchronizeMergeAdapters();
            var adapters = group.GetOwnedMergeAdapters();
            backend.Configure(camera, shader, shadowHost, shadowMesh, outlineHost, outlineMesh, adapters);
            group.SynchronizeMergeBackendConfiguration();
            int minimumOrder = adapters.Where(item => item != null && item.SourceRenderer != null)
                .Select(item => item.SourceRenderer.sortingOrder).DefaultIfEmpty(2).Min();
            if (minimumOrder < short.MinValue + 2)
                throw new InvalidOperationException("Cat fixture sorting order has no room for merge hosts: " + root.name);
            shadowHost.sortingOrder = minimumOrder - 2;
            outlineHost.sortingOrder = minimumOrder - 1;
        }

        private static void DisableUnrelatedBehaviours(GameObject root)
        {
            MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            for (var index = 0; index < behaviours.Length; index++)
                if (behaviours[index] != null) behaviours[index].enabled = false;
        }

        private static void EnsureAssetFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (AssetDatabase.IsValidFolder(path)) return;
            string guid = AssetDatabase.CreateFolder(parent, child);
            if (string.IsNullOrEmpty(guid) || !AssetDatabase.IsValidFolder(path))
                throw new InvalidOperationException("Could not create Unity folder: " + path);
        }

        private static void ValidateForward2Baseline(GameObject forwardGroupObject, GameObject forward2Object,
            Camera camera, OutlineShadowLabDriver driver, Material outlineMaterial)
        {
            if (forwardGroupObject == null || forward2Object == null || camera == null || driver == null ||
                outlineMaterial == null)
                throw new InvalidOperationException("Forward_2 exact path, Lab camera, driver, or outline material is missing.");

            SpriteOutlineMergeRenderer2D backend = forwardGroupObject.GetComponent<SpriteOutlineMergeRenderer2D>();
            if (backend == null || backend.TargetCamera != camera || !backend.IsConfigured ||
                backend.ShadowHost == null || backend.OutlineHost == null ||
                backend.ShadowHost.name != "Forward_Group_MergedShadow" ||
                backend.OutlineHost.name != "Forward_Group_MergedOutline")
                throw new InvalidOperationException("Forward_Group backend/hosts differ from the verified baseline; refusing broad repair.");

            string[] names = { "Forward_0", "Forward_1", "Forward_2" };
            for (var index = 0; index < names.Length; index++)
            {
                Transform child = forwardGroupObject.transform.Cast<Transform>()
                    .FirstOrDefault(item => item.name == names[index]);
                SpriteOutlineMergeMember2D member = child == null ? null : child.GetComponent<SpriteOutlineMergeMember2D>();
                SpriteRenderer renderer = child == null ? null : child.GetComponent<SpriteRenderer>();
                if (member == null || renderer == null || member.SourceRenderer != renderer ||
                    !backend.Members.Contains(member))
                    throw new InvalidOperationException(names[index] + " is missing its verified renderer/member registration.");

                if (index < 2 && (member.MergeOutline != OutlineMergeOverride.FollowGroup ||
                    member.MergeShadow != OutlineMergeOverride.FollowGroup))
                    throw new InvalidOperationException(names[index] + " no longer matches the verified FollowGroup baseline.");
                if (index == 2 && member.MergeShadow != OutlineMergeOverride.ForceOff)
                    throw new InvalidOperationException("Forward_2 shadow mode is not the verified ForceOff baseline.");
                if (index == 2 && member.MergeOutline != OutlineMergeOverride.ForceOn &&
                    member.MergeOutline != OutlineMergeOverride.ForceOff)
                    throw new InvalidOperationException("Forward_2 outline mode is neither the verified baseline nor the repaired state.");
            }

            SpriteRenderer forward2Renderer = forward2Object.GetComponent<SpriteRenderer>();
            SpriteOutline2D forward2Outline = forward2Object.GetComponent<SpriteOutline2D>();
            string currentSpritePath = forward2Renderer == null ? string.Empty :
                AssetDatabase.GetAssetPath(forward2Renderer.sprite);
            if (forward2Outline == null ||
                (currentSpritePath != OriginalFixturePath && currentSpritePath != PaddedFixturePath))
                throw new InvalidOperationException("Forward_2 no longer uses the verified fixture lineage; refusing to replace unknown content.");
        }

        private static void ValidateExistingPaddedFixtureOwnership()
        {
            if (!File.Exists(PaddedFixturePath)) return;
            byte[] expected = CreatePaddedFixturePng();
            byte[] existing = File.ReadAllBytes(PaddedFixturePath);
            if (!existing.SequenceEqual(expected))
                throw new InvalidOperationException("Existing padded fixture has unknown content and will not be overwritten: " +
                    PaddedFixturePath);
        }

        private static Sprite EnsurePaddedFixtureAsset()
        {
            ValidateExistingPaddedFixtureOwnership();
            if (!File.Exists(PaddedFixturePath))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Tests/VisualEffects/Fixtures"))
                    throw new InvalidOperationException("Fixture folder is missing; create it through AssetDatabase before applying the fix.");
                File.WriteAllBytes(PaddedFixturePath, CreatePaddedFixturePng());
                AssetDatabase.ImportAsset(PaddedFixturePath, ImportAssetOptions.ForceSynchronousImport);
            }

            TextureImporter importer = AssetImporter.GetAtPath(PaddedFixturePath) as TextureImporter;
            if (importer == null)
                throw new InvalidOperationException("Padded fixture TextureImporter is unavailable.");
            var currentSettings = new TextureImporterSettings();
            importer.ReadTextureSettings(currentSettings);
            bool importChanged = importer.textureType != TextureImporterType.Sprite ||
                importer.spriteImportMode != SpriteImportMode.Single ||
                !Mathf.Approximately(importer.spritePixelsPerUnit, PaddedFixturePixelsPerUnit) ||
                currentSettings.spriteAlignment != (int)SpriteAlignment.Center ||
                importer.spritePivot != new Vector2(0.5f, 0.5f) ||
                importer.alphaSource != TextureImporterAlphaSource.FromInput || !importer.alphaIsTransparency ||
                importer.mipmapEnabled || importer.textureCompression != TextureImporterCompression.Uncompressed ||
                importer.filterMode != FilterMode.Point || importer.wrapMode != TextureWrapMode.Clamp ||
                currentSettings.spriteMeshType != SpriteMeshType.FullRect;
            if (importChanged)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = PaddedFixturePixelsPerUnit;
                importer.spritePivot = new Vector2(0.5f, 0.5f);
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.filterMode = FilterMode.Point;
                importer.wrapMode = TextureWrapMode.Clamp;

                // TextureImporterSettings also owns the direct properties above. Re-read after
                // assigning them so SetTextureSettings cannot restore the pre-change snapshot.
                var updatedSettings = new TextureImporterSettings();
                importer.ReadTextureSettings(updatedSettings);
                updatedSettings.spriteAlignment = (int)SpriteAlignment.Center;
                updatedSettings.spritePivot = new Vector2(0.5f, 0.5f);
                updatedSettings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(updatedSettings);
                importer.SaveAndReimport();
            }
            return LoadRequired<Sprite>(PaddedFixturePath);
        }

        public static bool TryValidatePaddedFixture(out string error)
        {
            error = string.Empty;
            if (!File.Exists(PaddedFixturePath))
            {
                error = "PNG is missing";
                return false;
            }
            if (!File.ReadAllBytes(PaddedFixturePath).SequenceEqual(CreatePaddedFixturePng()))
            {
                error = "PNG pixels are not the deterministic 128/64/32 specification";
                return false;
            }

            TextureImporter importer = AssetImporter.GetAtPath(PaddedFixturePath) as TextureImporter;
            var settings = new TextureImporterSettings();
            if (importer == null)
            {
                error = "TextureImporter is missing";
                return false;
            }
            importer.ReadTextureSettings(settings);
            if (importer.textureType != TextureImporterType.Sprite ||
                importer.spriteImportMode != SpriteImportMode.Single ||
                !Mathf.Approximately(importer.spritePixelsPerUnit, PaddedFixturePixelsPerUnit) ||
                settings.spriteAlignment != (int)SpriteAlignment.Center ||
                importer.spritePivot != new Vector2(0.5f, 0.5f) ||
                importer.alphaSource != TextureImporterAlphaSource.FromInput || !importer.alphaIsTransparency ||
                importer.mipmapEnabled || importer.textureCompression != TextureImporterCompression.Uncompressed ||
                importer.filterMode != FilterMode.Point || importer.wrapMode != TextureWrapMode.Clamp ||
                settings.spriteMeshType != SpriteMeshType.FullRect)
            {
                error = "import settings are not Sprite/Single/PPU64/center/FullRect/uncompressed/point/clamp";
                return false;
            }

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PaddedFixturePath);
            if (sprite == null || !Mathf.Approximately(sprite.rect.width, PaddedFixtureSize) ||
                !Mathf.Approximately(sprite.rect.height, PaddedFixtureSize) ||
                !Mathf.Approximately(sprite.pixelsPerUnit, PaddedFixturePixelsPerUnit) ||
                !Mathf.Approximately(sprite.pivot.x, PaddedFixtureSize * 0.5f) ||
                !Mathf.Approximately(sprite.pivot.y, PaddedFixtureSize * 0.5f))
            {
                error = "imported Sprite rect/pivot is not 128x128 centered";
                return false;
            }
            return true;
        }

        private static byte[] CreatePaddedFixturePng()
        {
            var texture = new Texture2D(PaddedFixtureSize, PaddedFixtureSize, TextureFormat.RGBA32, false, false);
            try
            {
                var pixels = new Color32[PaddedFixtureSize * PaddedFixtureSize];
                for (var y = PaddedFixtureBorder; y < PaddedFixtureBorder + PaddedFixtureOpaqueSize; y++)
                    for (var x = PaddedFixtureBorder; x < PaddedFixtureBorder + PaddedFixtureOpaqueSize; x++)
                        pixels[y * PaddedFixtureSize + x] = new Color32(255, 255, 255, 255);
                texture.SetPixels32(pixels);
                texture.Apply(false, false);
                return texture.EncodeToPNG();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static bool EnsureForward2FormalConfiguration(GameObject forwardGroupObject,
            GameObject forward2Object, Sprite paddedFixture, Material outlineMaterial)
        {
            SpriteOutlineMergeRenderer2D backend = forwardGroupObject.GetComponent<SpriteOutlineMergeRenderer2D>();
            LegacyGroupState legacy = ReadLegacyGroupState(backend);
            SpriteOutlineGroup2D group = forwardGroupObject.GetComponent<SpriteOutlineGroup2D>();
            bool changed = false;
            if (group == null)
            {
                group = forwardGroupObject.AddComponent<SpriteOutlineGroup2D>();
                changed = true;
            }
            if (group.OutlineMaterial != outlineMaterial || group.OutlineColor != legacy.OutlineColor ||
                !Mathf.Approximately(group.Thickness, legacy.OutlineThickness))
            {
                group.Configure(outlineMaterial, legacy.OutlineColor, legacy.OutlineThickness);
                changed = true;
            }
            if (group.ShadowEnabled != legacy.ShadowEffectEnabled || group.ShadowColor != legacy.ShadowColor ||
                group.ShadowOffset != legacy.ShadowOffset ||
                !Mathf.Approximately(group.ShadowOpacity, legacy.ShadowOpacity) ||
                !Mathf.Approximately(group.ShadowBlur, legacy.ShadowSoftness))
            {
                group.ConfigureShadow(legacy.ShadowEffectEnabled, legacy.ShadowColor, legacy.ShadowOffset,
                    legacy.ShadowOpacity, legacy.ShadowSoftness);
                changed = true;
            }
            if (group.MergeOutlineEnabled != legacy.OutlineMergeEnabled)
            {
                group.MergeOutlineEnabled = legacy.OutlineMergeEnabled;
                changed = true;
            }
            if (group.MergeShadowEnabled != legacy.ShadowMergeEnabled)
            {
                group.MergeShadowEnabled = legacy.ShadowMergeEnabled;
                changed = true;
            }

            for (var index = 0; index < 2; index++)
            {
                GameObject item = forwardGroupObject.transform.Cast<Transform>()
                    .First(child => child.name == "Forward_" + index).gameObject;
                SpriteOutlineMergeMember2D adapter = item.GetComponent<SpriteOutlineMergeMember2D>();
                changed |= EnsureFormalMember(item, outlineMaterial, adapter.OutlineColor,
                    adapter.OutlineThicknessInSourcePixels, true, adapter.ShadowColor, adapter.ShadowOffsetInSourcePixels,
                    adapter.ShadowOpacity, adapter.ShadowSoftnessInSourcePixels, true,
                    OutlineMergeOverride.FollowGroup, OutlineMergeOverride.FollowGroup);
            }

            SpriteRenderer forward2Renderer = forward2Object.GetComponent<SpriteRenderer>();
            SpriteOutline2D forward2Outline = forward2Object.GetComponent<SpriteOutline2D>();
            SpriteOutlineMergeMember2D forward2Adapter = forward2Object.GetComponent<SpriteOutlineMergeMember2D>();
            Color visibleLocalColor = forward2Outline.OutlineColor;
            if (visibleLocalColor.a <= 0f)
            {
                if (Mathf.Approximately(visibleLocalColor.r, 0f) && Mathf.Approximately(visibleLocalColor.g, 0f) &&
                    Mathf.Approximately(visibleLocalColor.b, 0f))
                    visibleLocalColor = forward2Adapter.OutlineColor;
                visibleLocalColor.a = 1f;
            }
            float visibleThickness = forward2Outline.Thickness > 0f
                ? forward2Outline.Thickness
                : Mathf.Max(1f, forward2Adapter.OutlineThicknessInSourcePixels);
            changed |= EnsureFormalMember(forward2Object, outlineMaterial, visibleLocalColor, visibleThickness,
                forward2Outline.ShadowEnabled, forward2Outline.ShadowColor, forward2Outline.ShadowOffset,
                forward2Outline.ShadowOpacity, forward2Outline.ShadowBlur, false,
                OutlineMergeOverride.ForceOff, OutlineMergeOverride.ForceOff);
            if (forward2Renderer.sprite != paddedFixture)
            {
                forward2Renderer.sprite = paddedFixture;
                EditorUtility.SetDirty(forward2Renderer);
                changed = true;
            }

            int createdAdapters = group.CreateOrSynchronizeMergeAdapters();
            if (createdAdapters > 0) changed = true;
            group.SynchronizeMergeBackendConfiguration();
            if (changed)
            {
                EditorUtility.SetDirty(group);
                EditorUtility.SetDirty(backend);
            }
            return changed;
        }

        private static bool EnsureFormalMember(GameObject item, Material outlineMaterial, Color outlineColor,
            float outlineThickness, bool shadowEnabled, Color shadowColor, Vector2 shadowOffset,
            float shadowOpacity, float shadowBlur, bool overrideGroup,
            OutlineMergeOverride outlineMode, OutlineMergeOverride shadowMode)
        {
            SpriteOutline2D outline = item.GetComponent<SpriteOutline2D>();
            bool changed = false;
            if (outline == null)
            {
                outline = item.AddComponent<SpriteOutline2D>();
                changed = true;
            }
            if (outline.OutlineMaterial != outlineMaterial || outline.OutlineColor != outlineColor ||
                !Mathf.Approximately(outline.Thickness, outlineThickness))
            {
                outline.Configure(outlineMaterial, outlineColor, outlineThickness);
                changed = true;
            }
            if (outline.ShadowEnabled != shadowEnabled || outline.ShadowColor != shadowColor ||
                outline.ShadowOffset != shadowOffset || !Mathf.Approximately(outline.ShadowOpacity, shadowOpacity) ||
                !Mathf.Approximately(outline.ShadowBlur, shadowBlur))
            {
                outline.ConfigureShadow(shadowEnabled, shadowColor, shadowOffset, shadowOpacity, shadowBlur);
                changed = true;
            }
            if (outline.OverrideGroup != overrideGroup)
            {
                outline.SetOverrideGroup(overrideGroup);
                changed = true;
            }
            if (outline.MergeOutline != outlineMode || outline.MergeShadow != shadowMode)
            {
                outline.SetMergeModes(outlineMode, shadowMode);
                changed = true;
            }
            if (changed) EditorUtility.SetDirty(outline);
            return changed;
        }

        private static Transform EnsureForward2CameraMarker(Scene scene, Camera camera,
            GameObject forward2Object, ref bool changed)
        {
            GameObject markerObject = FindPath(scene, Forward2MarkerPath);
            GameObject markerRoot = FindPath(scene, "CameraAndStageMarkers");
            if (markerRoot == null)
                throw new InvalidOperationException("CameraAndStageMarkers root is missing.");
            if (markerObject == null)
            {
                markerObject = NewChild("CameraMarker_Forward2", markerRoot.transform, Vector3.zero);
                changed = true;
            }
            else if (markerObject.transform.parent != markerRoot.transform)
            {
                throw new InvalidOperationException("CameraMarker_Forward2 exists outside the approved marker root.");
            }

            Bounds targetBounds = forward2Object.GetComponent<SpriteRenderer>().bounds;
            Vector3 desiredPosition = new Vector3(targetBounds.center.x, targetBounds.center.y,
                camera.transform.position.z);
            if (markerObject.transform.position != desiredPosition ||
                markerObject.transform.rotation != camera.transform.rotation ||
                markerObject.transform.localScale != Vector3.one)
            {
                markerObject.transform.SetPositionAndRotation(desiredPosition, camera.transform.rotation);
                markerObject.transform.localScale = Vector3.one;
                changed = true;
            }
            return markerObject.transform;
        }

        private static LegacyGroupState ReadLegacyGroupState(SpriteOutlineMergeRenderer2D backend)
        {
            var serialized = new SerializedObject(backend);
            serialized.Update();
            return new LegacyGroupState(
                RequireProperty(serialized, "groupOutlineEnabled").boolValue,
                RequireProperty(serialized, "groupOutlineColor").colorValue,
                RequireProperty(serialized, "groupOutlineThicknessInSourcePixels").floatValue,
                RequireProperty(serialized, "groupShadowEnabled").boolValue,
                RequireProperty(serialized, "groupShadowEffectEnabled").boolValue,
                RequireProperty(serialized, "groupShadowColor").colorValue,
                RequireProperty(serialized, "groupShadowOffsetInSourcePixels").vector2Value,
                RequireProperty(serialized, "groupShadowOpacity").floatValue,
                RequireProperty(serialized, "groupShadowSoftnessInSourcePixels").floatValue);
        }

        private static SerializedProperty RequireProperty(SerializedObject serialized, string propertyName)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
                throw new InvalidOperationException("Verified backend property is missing: " + propertyName);
            return property;
        }

        private readonly struct LegacyGroupState
        {
            public readonly bool OutlineMergeEnabled;
            public readonly Color OutlineColor;
            public readonly float OutlineThickness;
            public readonly bool ShadowMergeEnabled;
            public readonly bool ShadowEffectEnabled;
            public readonly Color ShadowColor;
            public readonly Vector2 ShadowOffset;
            public readonly float ShadowOpacity;
            public readonly float ShadowSoftness;

            public LegacyGroupState(bool outlineMergeEnabled, Color outlineColor, float outlineThickness,
                bool shadowMergeEnabled, bool shadowEffectEnabled, Color shadowColor, Vector2 shadowOffset,
                float shadowOpacity, float shadowSoftness)
            {
                OutlineMergeEnabled = outlineMergeEnabled;
                OutlineColor = outlineColor;
                OutlineThickness = outlineThickness;
                ShadowMergeEnabled = shadowMergeEnabled;
                ShadowEffectEnabled = shadowEffectEnabled;
                ShadowColor = shadowColor;
                ShadowOffset = shadowOffset;
                ShadowOpacity = shadowOpacity;
                ShadowSoftness = shadowSoftness;
            }
        }

        private static GroupBuildResult BuildGroup(string name, Transform parent, Vector3 position, Camera camera,
            Shader shader, Sprite sprite, Material originalOutlineMaterial, bool reverse)
        {
            var group = NewChild(name, parent, position);
            var renderer = group.AddComponent<SpriteOutlineMergeRenderer2D>();
            var shadowHostObject = NewChild(name + "_MergedShadow", group.transform, Vector3.zero);
            var shadowMesh = shadowHostObject.AddComponent<MeshFilter>();
            var shadowHost = shadowHostObject.AddComponent<MeshRenderer>();
            var outlineHostObject = NewChild(name + "_MergedOutline", group.transform, Vector3.zero);
            var outlineMesh = outlineHostObject.AddComponent<MeshFilter>();
            var outlineHost = outlineHostObject.AddComponent<MeshRenderer>();

            var colors = new[] { new Color(1f, 0.35f, 0.25f, 1f), new Color(0.2f, 0.8f, 1f, 0.75f), new Color(0.9f, 0.8f, 0.2f, 0.6f) };
            var list = new List<SpriteOutlineMergeMember2D>();
            for (var index = 0; index < 3; index++)
            {
                var orderIndex = reverse ? 2 - index : index;
                var item = NewChild((reverse ? "Reverse_" : "Forward_") + orderIndex, group.transform,
                    new Vector3(index * 0.75f - 0.75f, (index % 2) * 0.35f, 0f));
                var source = item.AddComponent<SpriteRenderer>();
                source.sprite = sprite;
                source.color = new Color(1f, 1f, 1f, index == 1 ? 0.65f : 1f);
                source.sortingOrder = 20 + orderIndex;
                var member = item.AddComponent<SpriteOutlineMergeMember2D>();
                member.Configure(source,
                    index == 2 ? OutlineMergeOverride.ForceOn : OutlineMergeOverride.FollowGroup,
                    index == 2 ? OutlineMergeOverride.ForceOff : OutlineMergeOverride.FollowGroup,
                    colors[index], 1f + index, Color.black, new Vector2(2f, -2f), 0.5f, 1f);
                list.Add(member);
                if (index == 2)
                {
                    var independentShadow = item.AddComponent<SpriteOutline2D>();
                    independentShadow.Configure(originalOutlineMaterial, Color.clear, 0f);
                    independentShadow.ConfigureShadow(true, Color.black, new Vector2(2f, -2f), 0.5f, 1f);
                }
            }
            renderer.Configure(camera, shader, shadowHost, shadowMesh, outlineHost, outlineMesh, list);
            renderer.ConfigureGroup(true, new Color(0.15f, 0.9f, 1f, 1f), 2f,
                true, new Color(0.05f, 0.03f, 0.08f, 1f), new Vector2(3f, -3f), 0.55f, 2f);
            return new GroupBuildResult(renderer, list.ToArray());
        }

        private static Scene OpenRequiredScene()
        {
            if (!System.IO.File.Exists(ScenePath)) throw new InvalidOperationException("Missing shared Lab scene: " + ScenePath);
            var active = SceneManager.GetActiveScene();
            if (active.path == ScenePath) return active;
            if (active.IsValid() && active.isDirty && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                throw new OperationCanceledException("User kept the current dirty scene open.");
            return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        private static void CopySprite(SpriteRenderer source, SpriteRenderer target, int sortingOrder)
        {
            target.sprite = source.sprite;
            target.color = source.color;
            target.sortingLayerID = source.sortingLayerID;
            target.sortingOrder = sortingOrder;
            target.sharedMaterial = source.sharedMaterial;
        }

        private static GameObject NewChild(string name, Transform parent, Vector3 localPosition)
        {
            var value = new GameObject(name);
            value.transform.SetParent(parent, false);
            value.transform.localPosition = localPosition;
            return value;
        }

        private static GameObject FindPath(Scene scene, string path)
        {
            var segments = path.Split('/');
            var current = scene.GetRootGameObjects().FirstOrDefault(item => item.name == segments[0]);
            for (var index = 1; current != null && index < segments.Length; index++)
            {
                var child = current.transform.Cast<Transform>().FirstOrDefault(item => item.name == segments[index]);
                current = child == null ? null : child.gameObject;
            }
            return current;
        }

        private static T LoadRequired<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) throw new InvalidOperationException("Required asset is missing: " + path);
            return asset;
        }

        private readonly struct GroupBuildResult
        {
            public readonly SpriteOutlineMergeRenderer2D Renderer;
            public readonly SpriteOutlineMergeMember2D[] Members;
            public GroupBuildResult(SpriteOutlineMergeRenderer2D renderer, SpriteOutlineMergeMember2D[] members)
            {
                Renderer = renderer;
                Members = members;
            }
        }

        private readonly struct FormalGroupBuildResult
        {
            public readonly SpriteOutlineGroup2D Group;
            public readonly SpriteOutlineMergeRenderer2D Backend;
            public FormalGroupBuildResult(SpriteOutlineGroup2D group, SpriteOutlineMergeRenderer2D backend)
            {
                Group = group;
                Backend = backend;
            }
        }
    }
}
