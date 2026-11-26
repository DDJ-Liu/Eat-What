// VERIFY-TEMP: explicit, idempotent builder for the isolated FridgeScrollLab scene only.
using System;
using System.Collections.Generic;
using System.Linq;
using EatWhat.Cooking.ShortCycle;
using EatWhat.Tools.Localization;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace EatWhat.EditorTools.ShortCycle
{
    public static class FridgeScrollLabBuilder
    {
        public const string ScenePath = "Assets/Scenes/ToolTests/FridgeScrollLab.unity";
        public const string RigPrefabPath = "Assets/Prefabs/Cooking/ShortCycle/FridgeCat/FridgeCat_Rig.prefab";
        public const string FridgeSlotPrefabPath = "Assets/Prefabs/Cooking/ShortCycle/GridPieces/FridgeSlot.prefab";
        public const string SlotBlockPrefabPath = "Assets/Prefabs/Cooking/ShortCycle/GridPieces/SlotBlock.prefab";
        public const string CatalogPath = "Assets/Generated/DataTables/CK01GeneratedDataCatalog.asset";
        public const string InputActionsPath = "Assets/Scripts/Controller/PlayerControl.inputactions";
        public const string FontPath = "Assets/Font/Heavy/ShortCycle_Heavy_WithFallback.asset";

        private static readonly string[] RootNames =
        {
            "CameraAndStageMarkers", "Managers", "InteractionLayers", "Spaces", "Shared",
            "DebugAndReferences", "EventSystem"
        };

        [MenuItem("Tools/Eat What/Short Cycle/Build Or Open Fridge Scroll Lab")]
        public static void BuildOrOpen()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("FridgeScrollLab may only be built from stable Edit Mode.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var existing = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (existing != null)
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                Debug.Log("[SCROLL-LAB] Existing lab opened without rewriting it: " + ScenePath);
                return;
            }

            EnsureFolder("Assets/Scenes", "ToolTests");
            var rigPrefab = RequireAsset<GameObject>(RigPrefabPath);
            var fridgeSlotPrefab = RequireAsset<GameObject>(FridgeSlotPrefabPath);
            var slotBlockPrefab = RequireAsset<GameObject>(SlotBlockPrefabPath);
            var catalog = RequireAsset<CK01GeneratedDataCatalog>(CatalogPath);
            var inputActions = RequireAsset<InputActionAsset>(InputActionsPath);
            var font = RequireAsset<TMP_FontAsset>(FontPath);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var roots = RootNames.Select((name, index) => CreateRoot(name, index)).ToArray();

            var camera = BuildCameraAndMarkers(roots[0].transform, font);
            var managerObjects = BuildManagers(roots[1].transform, inputActions, catalog);
            var layers = BuildLayers(roots[2].transform);
            var fridge = BuildFridgeModule(roots[3].transform, rigPrefab, fridgeSlotPrefab);
            var clueBinder = BuildReservedClueBinder(roots[4].transform, slotBlockPrefab);
            var debug = BuildDebugReferences(roots[5].transform, font, camera, managerObjects, layers, fridge);
            BuildEventSystem(roots[6]);
            WireSessionAndPresentation(managerObjects, layers, fridge.Binder, clueBinder);
            WireScrollChain(managerObjects, fridge);
            WireTrace(debug.Trace, managerObjects, layers, fridge, camera);
            WireDriver(debug.Driver, managerObjects.Session, managerObjects.LayerStack, fridge.Adapter, debug.Trace, layers, debug.Status,
                debug.TestPoints);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeGameObject = debug.Driver.gameObject;
            Debug.Log("[SCROLL-LAB] Built isolated lab. It was not added to Build Settings: " + ScenePath);
        }

        private static Camera BuildCameraAndMarkers(Transform parent, TMP_FontAsset font)
        {
            var cameraObject = CreateChild(parent, "Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 7.2f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.075f, 0.09f, 0.12f, 1f);
            cameraObject.AddComponent<AudioListener>();

            var lightObject = CreateChild(parent, "Global Light 2D");
            var light = lightObject.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1f;

            var marker = CreateChild(parent, "ViewCenterMarker_TUNE");
            marker.transform.position = Vector3.zero;
            CreateWorldText(marker.transform, "Marker", "+ VIEW CENTER", font, 0.35f, Color.cyan, Vector3.zero);
            return camera;
        }

        private static ManagerSet BuildManagers(Transform parent, InputActionAsset inputActions,
            CK01GeneratedDataCatalog catalog)
        {
            var inputObject = CreateChild(parent, "InputManager");
            var playerInput = inputObject.AddComponent<PlayerInput>();
            playerInput.actions = inputActions;
            var input = inputObject.AddComponent<InputManager>();

            var mouseObject = CreateChild(parent, "MouseManager");
            var mouse = mouseObject.AddComponent<MouseManager>();

            var runtime = CreateChild(parent, "ShortCycleRuntime");
            var reflow = runtime.AddComponent<ShortCycleInventoryReflowAdapter>();
            var inventory = runtime.AddComponent<InventoryManager>();
            var profile = runtime.AddComponent<PlayerProfile>();
            var unlock = runtime.AddComponent<PlayerUnlockManager>();
            var equipment = runtime.AddComponent<KitchenEquipmentManager>();
            var inputLock = runtime.AddComponent<ShortCycleInputLockController>();
            var router = runtime.AddComponent<ShortCycleActionRouter>();
            var session = runtime.AddComponent<ShortCycleSessionManager>();
            var presentation = runtime.AddComponent<ShortCyclePresentationDirector>();
            var layerStack = runtime.AddComponent<ShortCycleInteractionLayerStack>();

            SetObject(inventory, "dataCatalog", catalog);
            SetObject(inventory, "tightReflowAdapter", reflow);
            SetObject(router, "inputLockController", inputLock);

            var moduleHost = CreateChild(parent, "FridgeModuleHost");
            return new ManagerSet
            {
                Input = input,
                Mouse = mouse,
                Inventory = inventory,
                Profile = profile,
                Unlock = unlock,
                Equipment = equipment,
                Router = router,
                Session = session,
                Presentation = presentation,
                LayerStack = layerStack,
                ModuleHost = moduleHost
            };
        }

        private static LayerSet BuildLayers(Transform parent)
        {
            var lab = CreateChild(parent, "Layer_FridgeScrollLab").AddComponent<MouseInteractionLayer>();
            lab.manualStackLayer = true;
            lab.alloweInteractionLayers = new List<LayerMask> { (LayerMask)(~0) };
            var modal = CreateChild(parent, "Layer_Modal").AddComponent<MouseInteractionLayer>();
            modal.manualStackLayer = true;
            modal.alloweInteractionLayers = new List<LayerMask> { (LayerMask)(~0) };
            return new LayerSet { Lab = lab, Modal = modal };
        }

        private static FridgeSet BuildFridgeModule(Transform spacesRoot, GameObject rigPrefab,
            GameObject slotPrefab)
        {
            var module = CreateChild(spacesRoot, "FridgeScrollModule");
            var fridgeGroup = CreateChild(module.transform, "FridgeCat_Group");
            var placement = CreateChild(fridgeGroup.transform, "FridgeModulePlacement_TUNE");
            placement.transform.localPosition = new Vector3(-2.5f, 0f, 0f);
            var rigObject = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab, placement.transform);
            rigObject.name = "CatRig";
            var rig = rigObject.GetComponent<FridgeCatRig>();
            if (rig == null) throw new InvalidOperationException("FridgeCat_Rig prefab root has no FridgeCatRig.");

            var slotPool = CreateChild(fridgeGroup.transform, "SlotPool");
            var slots = new FridgeSlot[30];
            for (var index = 0; index < slots.Length; index++)
            {
                var slotObject = (GameObject)PrefabUtility.InstantiatePrefab(slotPrefab, slotPool.transform);
                slotObject.name = "FridgeSlot_" + (index + 1).ToString("00");
                slots[index] = slotObject.GetComponent<FridgeSlot>();
                if (slots[index] == null) throw new InvalidOperationException("FridgeSlot prefab root has no FridgeSlot.");
                slotObject.SetActive(false);
            }

            var binder = fridgeGroup.AddComponent<ShortCycleFridgeBinder>();
            SetObject(binder, "fridgeCatRig", rig);
            SetObjectArray(binder, "slots", slots.Cast<UnityEngine.Object>().ToArray());

            var scrollRegion = CreateChild(fridgeGroup.transform, "ScrollRegion");
            var box = scrollRegion.AddComponent<BoxCollider2D>();
            var scrollable = scrollRegion.AddComponent<MouseScrollableObject>();
            scrollable.scrollStepEvent = new UnityEngine.Events.UnityEvent<float>();
            var adapter = scrollRegion.AddComponent<ShortCycleScrollAdapter>();
            var renderBounds = MeasureBounds(rigObject.GetComponentsInChildren<Renderer>(true));
            var localCenter = scrollRegion.transform.InverseTransformPoint(renderBounds.center);
            var localSize = scrollRegion.transform.InverseTransformVector(renderBounds.size);
            box.offset = new Vector2(localCenter.x, localCenter.y);
            box.size = new Vector2(Mathf.Abs(localSize.x) + 0.35f, Mathf.Abs(localSize.y) + 0.35f);

            return new FridgeSet
            {
                Module = module,
                RigObject = rigObject,
                Rig = rig,
                Binder = binder,
                ScrollRegion = scrollRegion,
                Collider = box,
                Scrollable = scrollable,
                Adapter = adapter,
                RenderBounds = renderBounds
            };
        }

        private static ShortCycleClueBoardBinder BuildReservedClueBinder(Transform sharedRoot,
            GameObject slotBlockPrefab)
        {
            var reserved = CreateChild(sharedRoot, "Reserved_Group");
            var support = CreateChild(reserved.transform, "ClueBindingSupport");
            var blocks = new SlotBlock[3];
            for (var index = 0; index < blocks.Length; index++)
            {
                var blockObject = (GameObject)PrefabUtility.InstantiatePrefab(slotBlockPrefab, support.transform);
                blockObject.name = "ClueSlotBlock_" + (index + 1).ToString("00");
                blocks[index] = blockObject.GetComponent<SlotBlock>();
                if (blocks[index] == null) throw new InvalidOperationException("SlotBlock prefab root has no SlotBlock.");
            }
            var binder = reserved.AddComponent<ShortCycleClueBoardBinder>();
            SetObjectArray(binder, "slotBlocks", blocks.Cast<UnityEngine.Object>().ToArray());
            support.SetActive(false);
            return binder;
        }

        private static DebugSet BuildDebugReferences(Transform parent, TMP_FontAsset font, Camera camera,
            ManagerSet managers, LayerSet layers, FridgeSet fridge)
        {
            var instructions = CreateChild(parent, "InstructionsBoard");
            instructions.transform.position = new Vector3(4.25f, 1.8f, 0f);
            CreateWorldText(instructions.transform, "Text",
                "FRIDGE SCROLL LAB\nREAL_INPUT: use physical mouse wheel over a test point\nSIMULATED: Driver context menu only\nModal: push/pop Layer_Modal\nExport: Captures/SCROLL-LAB\nNo Canvas / no UGUI interaction",
                font, 0.34f, Color.white, Vector3.zero);

            var statusObject = CreateChild(parent, "StatusBoardAnchor_TUNE");
            statusObject.transform.position = new Vector3(4.25f, -1.35f, 0f);
            var status = CreateWorldText(statusObject.transform, "StatusBoard", "FridgeScrollLab pending",
                font, 0.29f, new Color(0.65f, 1f, 0.75f, 1f), Vector3.zero);

            var traceObject = CreateChild(parent, "Probe_FridgeScrollTrace");
            var trace = traceObject.AddComponent<ShortCycleFridgeScrollTraceProbe>();
            var driver = managers.ModuleHost.AddComponent<FridgeScrollLabDriver>();

            CreateChild(parent, "Probe_FridgeScrollLab");
            var pointsRoot = CreateChild(parent, "TestPoints_Group");
            var positions = ComputePointPositions(fridge);
            var pointNames = new[]
            {
                "Point_IngredientCenter_TUNE", "Point_RowGap_TUNE", "Point_EmptyRegion_TUNE", "Point_Outside_TUNE"
            };
            var testPoints = new Transform[pointNames.Length];
            for (var index = 0; index < pointNames.Length; index++)
            {
                var point = CreateChild(pointsRoot.transform, pointNames[index]);
                point.transform.position = positions[index];
                CreateWorldText(point.transform, "Marker", "+", font, 0.48f,
                    index == pointNames.Length - 1 ? Color.red : Color.yellow, Vector3.zero);
                testPoints[index] = point.transform;
            }
            return new DebugSet { Trace = trace, Driver = driver, Status = status, TestPoints = testPoints };
        }

        private static Vector3[] ComputePointPositions(FridgeSet fridge)
        {
            var tiers = fridge.RigObject.GetComponentsInChildren<FridgeCatTier>(true)
                .OrderByDescending(value => value.transform.position.y).ToArray();
            var ingredient = tiers.Length == 0 || tiers[0].GetShelfAnchor(2) == null
                ? fridge.RenderBounds.center : tiers[0].GetShelfAnchor(2).position;
            var rowGap = tiers.Length < 2 || tiers[0].GetShelfAnchor(2) == null || tiers[1].GetShelfAnchor(2) == null
                ? fridge.RenderBounds.center
                : (tiers[0].GetShelfAnchor(2).position + tiers[1].GetShelfAnchor(2).position) * 0.5f;
            var bounds = fridge.Collider.bounds;
            var empty = bounds.center + new Vector3(bounds.extents.x * 0.42f, -bounds.extents.y * 0.38f, 0f);
            var outside = new Vector3(bounds.max.x + Mathf.Max(0.5f, bounds.size.x * 0.1f), bounds.center.y, 0f);
            return new[] { ingredient, rowGap, empty, outside };
        }

        private static void BuildEventSystem(GameObject root)
        {
            root.AddComponent<EventSystem>();
            root.AddComponent<InputSystemUIInputModule>();
        }

        private static void WireSessionAndPresentation(ManagerSet managers, LayerSet layers,
            ShortCycleFridgeBinder fridgeBinder, ShortCycleClueBoardBinder clueBinder)
        {
            SetObject(managers.LayerStack, "sessionManager", managers.Session);
            SetObject(managers.LayerStack, "baseLayer", layers.Lab);
            SetObject(managers.LayerStack, "phase1Layer", layers.Lab);
            SetObjectArray(managers.LayerStack, "configuredLayers", new UnityEngine.Object[] { layers.Lab, layers.Modal });
            SetObject(managers.Presentation, "interactionLayerStack", managers.LayerStack);

            SetObject(managers.Session, "inventoryManager", managers.Inventory);
            SetObject(managers.Session, "playerProfile", managers.Profile);
            SetObject(managers.Session, "playerUnlockManager", managers.Unlock);
            SetObject(managers.Session, "kitchenEquipmentManager", managers.Equipment);
            SetObject(managers.Session, "actionRouter", managers.Router);
            SetObject(managers.Session, "presentationDirector", managers.Presentation);
            SetObject(managers.Session, "dataCatalog", RequireAsset<CK01GeneratedDataCatalog>(CatalogPath));
            SetObject(managers.Session, "fridgeBinder", fridgeBinder);
            SetObject(managers.Session, "clueBoardBinder", clueBinder);
            SetString(managers.Session, "defaultRecipeId", "rcp_tomato_egg");
            SetBool(managers.Session, "useDefaultRecipeForDebug", true);
        }

        private static void WireScrollChain(ManagerSet managers, FridgeSet fridge)
        {
            SetObject(fridge.Adapter, "scrollableObject", fridge.Scrollable);
            SetObject(fridge.Adapter, "actionRouter", managers.Router);
            SetObject(fridge.Adapter, "sessionManager", managers.Session);
        }

        private static void WireTrace(ShortCycleFridgeScrollTraceProbe trace, ManagerSet managers,
            LayerSet layers, FridgeSet fridge, Camera camera)
        {
            SetBool(trace, "traceEnabled", true);
            SetBool(trace, "logChangesToConsole", true);
            SetString(trace, "exportFolderName", "SCROLL-LAB");
            SetObject(trace, "inputManager", managers.Input);
            SetObject(trace, "mouseManager", managers.Mouse);
            SetObject(trace, "expectedMainCamera", camera);
            SetObject(trace, "shortCycleLayerStack", managers.LayerStack);
            SetObject(trace, "phase0Layer", layers.Lab);
            SetObject(trace, "phase1Layer", layers.Lab);
            SetObjectArray(trace, "modalLayers", new UnityEngine.Object[] { layers.Modal });
            SetObject(trace, "scrollRegion", fridge.Scrollable);
            SetObject(trace, "scrollAdapter", fridge.Adapter);
            SetObject(trace, "sessionManager", managers.Session);
            SetObject(trace, "fridgeBinder", fridge.Binder);
            SetObject(trace, "fridgeCatRig", fridge.Rig);
            SetObject(trace, "scrollArea", fridge.Rig.ScrollArea);
        }

        private static void WireDriver(FridgeScrollLabDriver driver, ShortCycleSessionManager session,
            ShortCycleInteractionLayerStack layerStack, ShortCycleScrollAdapter adapter,
            ShortCycleFridgeScrollTraceProbe trace, LayerSet layers,
            TextMeshPro status, Transform[] testPoints)
        {
            SetObject(driver, "sessionManager", session);
            SetObject(driver, "scrollAdapter", adapter);
            SetObject(driver, "traceProbe", trace);
            SetObject(driver, "layerStack", layerStack);
            SetObject(driver, "modalComparisonLayer", layers.Modal);
            SetObject(driver, "statusBoard", status);
            SetObjectArray(driver, "testPoints", testPoints.Cast<UnityEngine.Object>().ToArray());
            SetString(driver, "recipeId", "rcp_tomato_egg");
            SetBool(driver, "initializeOnStart", true);
        }

        private static TextMeshPro CreateWorldText(Transform parent, string name, string text,
            TMP_FontAsset font, float size, Color color, Vector3 localPosition)
        {
            var textObject = CreateChild(parent, name);
            textObject.transform.localPosition = localPosition;
            var label = textObject.AddComponent<TextMeshPro>();
            label.text = text;
            label.font = font;
            label.fontSize = size;
            label.color = color;
            label.alignment = TextAlignmentOptions.TopLeft;
            return label;
        }

        private static Bounds MeasureBounds(Renderer[] renderers)
        {
            var available = renderers.Where(value => value != null).ToArray();
            if (available.Length == 0) throw new InvalidOperationException("FridgeCat_Rig has no measurable Renderer bounds.");
            var bounds = available[0].bounds;
            for (var index = 1; index < available.Length; index++) bounds.Encapsulate(available[index].bounds);
            return bounds;
        }

        private static GameObject CreateRoot(string name, int siblingIndex)
        {
            var value = new GameObject(name);
            value.transform.SetSiblingIndex(siblingIndex);
            return value;
        }

        private static GameObject CreateChild(Transform parent, string name)
        {
            var value = new GameObject(name);
            value.transform.SetParent(parent, false);
            return value;
        }

        private static T RequireAsset<T>(string path) where T : UnityEngine.Object
        {
            var value = AssetDatabase.LoadAssetAtPath<T>(path);
            if (value == null) throw new InvalidOperationException("Required FridgeScrollLab source asset is missing: " + path);
            return value;
        }

        private static void EnsureFolder(string parent, string child)
        {
            var full = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(full)) AssetDatabase.CreateFolder(parent, child);
        }

        private static void SetObject(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(propertyName);
            if (property == null) throw new InvalidOperationException(target.GetType().Name + "." + propertyName + " was not found.");
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObjectArray(UnityEngine.Object target, string propertyName, UnityEngine.Object[] values)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(propertyName);
            if (property == null) throw new InvalidOperationException(target.GetType().Name + "." + propertyName + " was not found.");
            property.arraySize = values == null ? 0 : values.Length;
            for (var index = 0; index < property.arraySize; index++)
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetString(UnityEngine.Object target, string propertyName, string value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(propertyName);
            if (property == null) throw new InvalidOperationException(target.GetType().Name + "." + propertyName + " was not found.");
            property.stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetBool(UnityEngine.Object target, string propertyName, bool value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(propertyName);
            if (property == null) throw new InvalidOperationException(target.GetType().Name + "." + propertyName + " was not found.");
            property.boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private sealed class ManagerSet
        {
            public InputManager Input;
            public MouseManager Mouse;
            public InventoryManager Inventory;
            public PlayerProfile Profile;
            public PlayerUnlockManager Unlock;
            public KitchenEquipmentManager Equipment;
            public ShortCycleActionRouter Router;
            public ShortCycleSessionManager Session;
            public ShortCyclePresentationDirector Presentation;
            public ShortCycleInteractionLayerStack LayerStack;
            public GameObject ModuleHost;
        }

        private sealed class LayerSet { public MouseInteractionLayer Lab; public MouseInteractionLayer Modal; }
        private sealed class FridgeSet
        {
            public GameObject Module;
            public GameObject RigObject;
            public FridgeCatRig Rig;
            public ShortCycleFridgeBinder Binder;
            public GameObject ScrollRegion;
            public BoxCollider2D Collider;
            public MouseScrollableObject Scrollable;
            public ShortCycleScrollAdapter Adapter;
            public Bounds RenderBounds;
        }
        private sealed class DebugSet
        {
            public ShortCycleFridgeScrollTraceProbe Trace;
            public FridgeScrollLabDriver Driver;
            public TextMeshPro Status;
            public Transform[] TestPoints;
        }
    }
}
