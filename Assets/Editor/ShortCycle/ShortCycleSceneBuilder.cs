using System;
using System.Collections.Generic;
using EatWhat.Cooking.ShortCycle;
using EatWhat.DataTables;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class ShortCycleSceneBuilder
{
    public const string ScenePath = "Assets/Scenes/Cooking/ShortCycle_P0P1.unity";

    [MenuItem("Tools/CK01/Build ShortCycle P0P1 Scene")]
    public static void BuildFromMenu()
    {
        EnsureFolder("Assets/Scenes", "Cooking");
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Debug.Log("CK01-C-BUILD existing scene opened: " + ScenePath);
            return;
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var root = new GameObject("__ShortCycle_P0P1");

        var cameraGo = NewChild("Main Camera", root.transform, new Vector3(0f, 0f, -10f));
        cameraGo.tag = "MainCamera";
        var camera = cameraGo.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 7.2f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.86f, 0.88f, 0.90f, 1f);
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100f;
        cameraGo.AddComponent<AudioListener>();

        var lightGo = NewChild("Directional Light", root.transform, new Vector3(0f, 3f, -5f));
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 0.8f;
        lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        var markerRoot = NewChild("CameraAndStageMarkers", root.transform, Vector3.zero);
        var phase1Marker = NewChild("Phase1CameraMarker_Left", markerRoot.transform, new Vector3(-24f, 0f, -10f)).transform;
        var phase0Marker = NewChild("Phase0CameraMarker_Center", markerRoot.transform, new Vector3(0f, 0f, -10f)).transform;
        var rightMarker = NewChild("RightReservedCameraMarker", markerRoot.transform, new Vector3(24f, 0f, -10f)).transform;
        var clueP0Anchor = NewChild("ClueBoardAnchor_Phase0_Left", markerRoot.transform, new Vector3(-8f, -0.2f, -0.3f)).transform;
        var clueP1Anchor = NewChild("ClueBoardAnchor_Phase1_Right", markerRoot.transform, new Vector3(-16f, -0.2f, -0.3f)).transform;
        cameraGo.transform.position = phase0Marker.position;

        var managers = NewChild("Managers", root.transform, Vector3.zero);
        var inventory = managers.AddComponent<InventoryManager>();
        var profile = managers.AddComponent<PlayerProfile>();
        var unlocks = managers.AddComponent<PlayerUnlockManager>();
        var equipment = managers.AddComponent<KitchenEquipmentManager>();
        var inputLock = managers.AddComponent<ShortCycleInputLockController>();
        var actionRouter = managers.AddComponent<ShortCycleActionRouter>();
        var cameraPan = managers.AddComponent<ShortCycleCameraPanController>();
        var phase2Transition = managers.AddComponent<ShortCyclePhase2TransitionAdapter>();
        var phase2Presenter = managers.AddComponent<ShortCyclePhase2HandoffPresenter>();
        var upstream = managers.AddComponent<ShortCycleUpstreamExitPresenter>();
        var reflow = managers.AddComponent<ShortCycleInventoryReflowAdapter>();
        var coordinator = managers.AddComponent<ShortCycleSessionManager>();
        var scenePresenter = managers.AddComponent<ShortCycleScenePresenter>();

        var layerRoot = NewChild("MouseInteractionLayerStack", root.transform, Vector3.zero);
        var baseLayer = NewChild("Layer_00_Base", layerRoot.transform, Vector3.zero)
            .AddComponent<MouseInteractionLayer>();
        var clueLayer = NewChild("Layer_10_ClueBoardModal", layerRoot.transform, Vector3.zero)
            .AddComponent<MouseInteractionLayer>();
        var layerStack = layerRoot.AddComponent<ShortCycleInteractionLayerStack>();
        ConfigureInteractionLayer(baseLayer, 0, false);
        ConfigureInteractionLayer(clueLayer, 10, true);
        SetObject(layerStack, "baseLayer", baseLayer);
        SetObjects(layerStack, "configuredLayers", new UnityEngine.Object[] { baseLayer, clueLayer });

        SetObject(actionRouter, "inputLockController", inputLock);
        SetObject(cameraPan, "controlledCamera", camera);
        SetObject(cameraPan, "phase0Marker", phase0Marker);
        SetObject(cameraPan, "phase1Marker", phase1Marker);
        SetObject(cameraPan, "rightReservedMarker", rightMarker);
        SetObject(cameraPan, "inputLockController", inputLock);
        SetFloat(cameraPan, "panDuration", 0.45f);
        SetObject(phase2Presenter, "transitionAdapter", phase2Transition);

        SetObject(coordinator, "inventoryManager", inventory);
        SetObject(coordinator, "playerProfile", profile);
        SetObject(coordinator, "playerUnlockManager", unlocks);
        SetObject(coordinator, "kitchenEquipmentManager", equipment);
        SetObject(coordinator, "actionRouter", actionRouter);
        SetObject(coordinator, "cameraPanController", cameraPan);
        SetObject(coordinator, "phase2HandoffPresenter", phase2Presenter);
        SetObject(coordinator, "upstreamExitPresenter", upstream);

        var dataCatalog = LoadRequired<CK01GeneratedDataCatalog>(CK01ImportRegistry.DataCatalogAssetPath);
        SetObject(inventory, "dataCatalog", dataCatalog);
        SetObject(inventory, "tightReflowAdapter", reflow);

        var spaces = NewChild("ThreeContinuousSpaces", root.transform, Vector3.zero);
        var phase1Canvas = AddSpaceCanvas(
            "Phase1_KitchenPlaceholder_Group_Left", spaces.transform, new Vector3(-24f, 0f, 0f),
            new Color(0.76f, 0.84f, 0.82f, 1f), "PHASE 1 · KITCHEN GREYBOX", camera);
        BuildPhase1((RectTransform)phase1Canvas.transform, scenePresenter);

        var phase0Canvas = AddSpaceCanvas(
            "Phase0_RecipeBookPlaceholder_Group_Center", spaces.transform, Vector3.zero,
            new Color(0.91f, 0.86f, 0.74f, 1f), "PHASE 0 · RECIPE BOOK GREYBOX", camera);
        BuildPhase0((RectTransform)phase0Canvas.transform, scenePresenter);

        var reservedCanvas = AddSpaceCanvas(
            "Phase2_RightReserved_Empty_Group", spaces.transform, new Vector3(24f, 0f, 0f),
            new Color(0.82f, 0.82f, 0.84f, 1f), "RIGHT RESERVED · EMPTY", camera);
        AddText("ReservedNotice", (RectTransform)reservedCanvas.transform,
            "Reserved for future branch systems\n(no CK01-C implementation)", Vector2.zero,
            new Vector2(1600f, 300f), 42, new Color(0.30f, 0.30f, 0.34f, 1f));

        GameObject collapsed;
        GameObject expanded;
        var clueShell = BuildClueBoard(root.transform, camera, clueP0Anchor.position, out collapsed, out expanded);

        var statusGo = NewChild("RuntimeStatusText", cameraGo.transform, Vector3.zero);
        statusGo.transform.localPosition = new Vector3(-12.2f, 6.6f, 10f);
        var statusMesh = AddTextMesh(statusGo, 48, 0.06f, "CK01-C-T2");

        var dataGo = NewChild("GeneratedDataEvidenceText", root.transform, new Vector3(-11.4f, -6.2f, -0.5f));
        var dataMesh = AddTextMesh(dataGo, 40, 0.045f, "CSV > Generated > Manager");
        dataMesh.anchor = TextAnchor.LowerLeft;

        var eventSystemGo = NewChild("EventSystem_NewInputSystem", root.transform, Vector3.zero);
        eventSystemGo.AddComponent<EventSystem>();
        eventSystemGo.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();

        SetObject(scenePresenter, "coordinator", coordinator);
        SetObject(scenePresenter, "actionRouter", actionRouter);
        SetObject(scenePresenter, "inputLockController", inputLock);
        SetObject(scenePresenter, "interactionLayerStack", layerStack);
        SetObject(scenePresenter, "clueBoardLayer", clueLayer);
        SetObject(scenePresenter, "dataCatalog", dataCatalog);
        SetObject(scenePresenter, "controlledCamera", camera);
        SetObject(scenePresenter, "phase0CameraMarker", phase0Marker);
        SetObject(scenePresenter, "phase1CameraMarker", phase1Marker);
        SetObject(scenePresenter, "clueBoardShell", clueShell.transform);
        SetObject(scenePresenter, "clueBoardPhase0Anchor", clueP0Anchor);
        SetObject(scenePresenter, "clueBoardPhase1Anchor", clueP1Anchor);
        SetObject(scenePresenter, "clueBoardCollapsedVisual", collapsed);
        SetObject(scenePresenter, "clueBoardExpandedVisual", expanded);
        SetObject(scenePresenter, "statusText", statusMesh);
        SetObject(scenePresenter, "dataText", dataMesh);
        SetFloat(scenePresenter, "presentationDuration", 0.45f);
        SetBool(scenePresenter, "runAutomatedProbe", true);

        if (!EditorSceneManager.SaveScene(scene, ScenePath))
            throw new InvalidOperationException("Unity failed to save " + ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("CK01-C-BUILD PASS scene=" + ScenePath + " items=8 traySlots=20 spaces=3");
    }

    private static void BuildPhase1(RectTransform root, ShortCycleScenePresenter presenter)
    {
        AddImage("FridgeCatPlaceholder", root, new Vector2(-520f, 80f), new Vector2(820f, 1000f), new Color(0.18f, 0.22f, 0.24f, 1f));
        AddText("FridgeLabel", root, "FRIDGE / GENERATED ITEMS", new Vector2(-520f, 500f), new Vector2(700f, 80f), 32, Color.white);
        for (var row = 0; row < 4; row++)
        for (var column = 0; column < 5; column++)
            AddImage("FridgeSlot_R" + (row + 1).ToString("00") + "_C" + (column + 1).ToString("00"),
                root, new Vector2(-790f + column * 135f, 330f - row * 150f), new Vector2(110f, 110f),
                new Color(0.92f, 0.87f, 0.64f, 1f));

        AddImage("PrepTray_20Slots", root, new Vector2(400f, -380f), new Vector2(1120f, 360f), new Color(0.45f, 0.49f, 0.54f, 1f));
        for (var index = 0; index < 20; index++)
        {
            var row = index / 10;
            var column = index % 10;
            AddImage("TraySlot_" + index.ToString("00"), root,
                new Vector2(-75f + column * 100f, -310f - row * 110f), new Vector2(82f, 82f),
                new Color(0.82f, 0.84f, 0.86f, 1f));
        }
        AddText("Phase1Hint", root, "Generated Items → Placeholder States → P2 boundary",
            new Vector2(380f, 160f), new Vector2(1250f, 120f), 34, new Color(0.17f, 0.20f, 0.22f, 1f));
        var returnButton = AddMouseInteractButton("ReturnArrow_NamedAction", root, "← RETURN TO PHASE 0",
            new Vector2(810f, 560f), new Vector2(620f, 100f), new Color(0.98f, 0.83f, 0.35f, 1f));
        var clueButton = AddMouseInteractButton("ClueBoardToggle_Phase1", root, "CLUE BOARD",
            new Vector2(810f, 420f), new Vector2(420f, 90f), new Color(0.95f, 0.70f, 0.72f, 1f));
        UnityEventTools.AddPersistentListener(returnButton.selectEvent, presenter.OnReturnButton);
        UnityEventTools.AddPersistentListener(clueButton.selectEvent, presenter.ToggleClueBoardShell);
    }

    private static void BuildPhase0(RectTransform root, ShortCycleScenePresenter presenter)
    {
        AddImage("RecipeBookLeftPage", root, new Vector2(-590f, 20f), new Vector2(1050f, 1040f), new Color(0.98f, 0.96f, 0.88f, 1f));
        AddImage("RecipeBookRightPage", root, new Vector2(590f, 20f), new Vector2(1050f, 1040f), new Color(0.87f, 0.86f, 0.82f, 1f));
        AddText("RecipeTitle", root, "测试菜谱 / rcp_test", new Vector2(-590f, 420f), new Vector2(850f, 100f), 48, new Color(0.18f, 0.18f, 0.20f, 1f));
        AddImage("DishPolaroidPlaceholder", root, new Vector2(-650f, 80f), new Vector2(520f, 480f), new Color(0.72f, 0.76f, 0.78f, 1f));
        AddText("DishPlaceholderText", root, "DISH\nPLACEHOLDER", new Vector2(-650f, 80f), new Vector2(460f, 300f), 38, new Color(0.25f, 0.28f, 0.30f, 1f));
        for (var index = 0; index < 3; index++)
            AddImage("RecipeStepCard_" + (index + 1), root, new Vector2(450f, 300f - index * 260f), new Vector2(780f, 190f), new Color(0.97f, 0.94f, 0.82f, 1f));
        AddText("StepChainPreview", root, "1  切番茄\n2  搅拌鸡蛋\n3  装盘",
            new Vector2(450f, 40f), new Vector2(680f, 720f), 36, new Color(0.22f, 0.22f, 0.24f, 1f), TextAnchor.MiddleLeft);
        var startButton = AddMouseInteractButton("StartCooking_NamedAction", root, "START COOKING →",
            new Vector2(-260f, -530f), new Vector2(620f, 110f), new Color(0.98f, 0.76f, 0.27f, 1f));
        var escapeButton = AddMouseInteractButton("Escape_NamedAction", root, "ESC / EXIT",
            new Vector2(460f, -530f), new Vector2(420f, 100f), new Color(0.78f, 0.82f, 0.86f, 1f));
        var clueButton = AddMouseInteractButton("ClueBoardToggle_Phase0", root, "CLUE BOARD",
            new Vector2(-960f, 550f), new Vector2(360f, 90f), new Color(0.95f, 0.70f, 0.72f, 1f));
        UnityEventTools.AddPersistentListener(startButton.selectEvent, presenter.OnStartCookingButton);
        UnityEventTools.AddPersistentListener(escapeButton.selectEvent, presenter.OnEscapeButton);
        UnityEventTools.AddPersistentListener(clueButton.selectEvent, presenter.ToggleClueBoardShell);
    }

    private static GameObject BuildClueBoard(Transform parent, Camera camera, Vector3 position, out GameObject collapsed, out GameObject expanded)
    {
        var shell = new GameObject("ClueBoardShell_MovesWithPhase", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        shell.transform.SetParent(parent, false);
        var canvas = shell.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = camera;
        canvas.sortingOrder = 5;
        var rect = (RectTransform)shell.transform;
        rect.position = position;
        rect.sizeDelta = new Vector2(500f, 700f);
        rect.localScale = Vector3.one * 0.01f;
        collapsed = AddImage("CollapsedState", rect, Vector2.zero, new Vector2(180f, 620f), new Color(0.92f, 0.62f, 0.68f, 1f)).gameObject;
        AddText("CollapsedLabel", (RectTransform)collapsed.transform, "CLUE", Vector2.zero, new Vector2(160f, 520f), 34, new Color(0.18f, 0.18f, 0.20f, 1f));
        expanded = AddImage("ExpandedState_WaitingForRequirements", rect, Vector2.zero, new Vector2(500f, 700f), new Color(0.96f, 0.91f, 0.72f, 1f)).gameObject;
        AddText("ExpandedLabel", (RectTransform)expanded.transform,
            "CLUE BOARD SHELL\n\n等待需求细化\n\n(no content rendering)", Vector2.zero,
            new Vector2(430f, 620f), 30, new Color(0.18f, 0.18f, 0.20f, 1f));
        return shell;
    }

    private static Canvas AddSpaceCanvas(string name, Transform parent, Vector3 center, Color background, string title, Camera camera)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        go.transform.SetParent(parent, false);
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = camera;
        var rect = (RectTransform)go.transform;
        rect.position = center;
        rect.sizeDelta = new Vector2(2560f, 1440f);
        rect.localScale = Vector3.one * 0.01f;
        AddImage("Background", rect, Vector2.zero, rect.sizeDelta, background);
        AddText("SpaceTitle", rect, title, new Vector2(0f, 620f), new Vector2(1800f, 100f), 48, new Color(0.18f, 0.18f, 0.22f, 1f));
        return canvas;
    }

    private static RectTransform AddImage(string name, RectTransform parent, Vector2 position, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        var image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }

    private static Text AddText(string name, RectTransform parent, string value, Vector2 position, Vector2 size, int fontSize, Color color, TextAnchor anchor = TextAnchor.MiddleCenter)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        var text = go.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = anchor;
        text.text = value;
        text.raycastTarget = false;
        return text;
    }

    private static Button_MouseInteract AddMouseInteractButton(
        string name,
        RectTransform parent,
        string label,
        Vector2 position,
        Vector2 size,
        Color color)
    {
        var go = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(BoxCollider2D),
            typeof(Button_MouseInteract),
            typeof(ColorImageButton_Visual));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        var image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;

        var collider = go.GetComponent<BoxCollider2D>();
        collider.size = size;

        var button = go.GetComponent<Button_MouseInteract>();
        var visual = go.GetComponent<ColorImageButton_Visual>();
        button.visualComponents.Add(visual);
        SetObject(visual, "targetImage", image);
        SetColor(visual, "idleColor", color);
        SetColor(visual, "highLightColor", Color.Lerp(color, Color.white, 0.28f));
        SetColor(visual, "pressedColor", Color.Lerp(color, Color.black, 0.32f));
        UnityEventTools.AddPersistentListener(button.overEvent, visual.setHighlight);
        UnityEventTools.AddPersistentListener(button.outEvent, visual.setIdle);
        UnityEventTools.AddPersistentListener(button.selectEvent, visual.OnPress);

        AddText("Label", rect, label, Vector2.zero, size, 34, new Color(0.13f, 0.13f, 0.16f, 1f));
        return button;
    }

    private static void ConfigureInteractionLayer(MouseInteractionLayer layer, int layerId, bool clickBlankCancels)
    {
        layer.layerID = layerId;
        layer.manualStackLayer = true;
        layer.clickNullEqualsCancel = clickBlankCancels;
        layer.alloweInteractionLayers = new List<LayerMask> { (LayerMask)~0 };
    }

    private static TextMesh AddTextMesh(GameObject go, int fontSize, float characterSize, string value)
    {
        var mesh = go.AddComponent<TextMesh>();
        mesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        mesh.anchor = TextAnchor.UpperLeft;
        mesh.alignment = TextAlignment.Left;
        mesh.fontSize = fontSize;
        mesh.characterSize = characterSize;
        mesh.color = new Color(0.12f, 0.13f, 0.15f, 1f);
        mesh.text = value;
        return mesh;
    }

    private static GameObject NewChild(string name, Transform parent, Vector3 position)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        return go;
    }

    private static T LoadRequired<T>(string path) where T : UnityEngine.Object
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null) throw new InvalidOperationException("Missing required asset: " + path);
        return asset;
    }

    private static void EnsureFolder(string parent, string child)
    {
        var path = parent + "/" + child;
        if (AssetDatabase.IsValidFolder(path)) return;
        var guid = AssetDatabase.CreateFolder(parent, child);
        if (string.IsNullOrEmpty(guid) || !AssetDatabase.IsValidFolder(path))
            throw new InvalidOperationException("Failed to register folder: " + path);
    }

    private static void SetObject(UnityEngine.Object target, string field, UnityEngine.Object value)
    {
        var serialized = new SerializedObject(target);
        var property = serialized.FindProperty(field);
        if (property == null) throw new InvalidOperationException(target.GetType().Name + " missing field " + field);
        property.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetObjects(UnityEngine.Object target, string field, UnityEngine.Object[] values)
    {
        var serialized = new SerializedObject(target);
        var property = serialized.FindProperty(field);
        if (property == null) throw new InvalidOperationException(target.GetType().Name + " missing field " + field);
        property.arraySize = values.Length;
        for (var index = 0; index < values.Length; index++)
            property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetBool(UnityEngine.Object target, string field, bool value)
    {
        var serialized = new SerializedObject(target);
        var property = serialized.FindProperty(field);
        if (property == null) throw new InvalidOperationException(target.GetType().Name + " missing field " + field);
        property.boolValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetFloat(UnityEngine.Object target, string field, float value)
    {
        var serialized = new SerializedObject(target);
        var property = serialized.FindProperty(field);
        if (property == null) throw new InvalidOperationException(target.GetType().Name + " missing field " + field);
        property.floatValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetColor(UnityEngine.Object target, string field, Color value)
    {
        var serialized = new SerializedObject(target);
        var property = serialized.FindProperty(field);
        if (property == null) throw new InvalidOperationException(target.GetType().Name + " missing field " + field);
        property.colorValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
