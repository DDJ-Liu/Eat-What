// RETIRED ONE-TIME MIGRATION (AI-000045, archived by AI-000047).
// Audit reference only: this file intentionally lives outside Assets so its
// MenuItem entry points cannot be loaded or executed by the Unity Editor.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EatWhat.Cooking.ShortCycle;
using EatWhat.DataTables;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;

public static class ShortCycleSceneMigrationV2
{
    public const string ScenePath = "Assets/Scenes/Cooking/ShortCycle_P0P1.unity";
    private const string PrefabFolder = "Assets/Prefabs/Cooking/ShortCycle/GridPieces";
    private static readonly Color Ink = new Color(0.18f, 0.16f, 0.15f, 1f);
    private static readonly Color Paper = new Color(0.94f, 0.86f, 0.66f, 1f);
    private static readonly Vector3[] TrayAnchorSeeds =
    {
        new Vector3(0.00f,  0.00f, 0f), new Vector3(1.18f,  0.04f, 0f), new Vector3(2.38f, -0.02f, 0f), new Vector3(3.57f,  0.06f, 0f), new Vector3(4.78f,  0.00f, 0f),
        new Vector3(0.12f, -0.73f, 0f), new Vector3(1.31f, -0.78f, 0f), new Vector3(2.52f, -0.70f, 0f), new Vector3(3.70f, -0.77f, 0f), new Vector3(4.90f, -0.72f, 0f),
        new Vector3(0.02f, -1.49f, 0f), new Vector3(1.21f, -1.43f, 0f), new Vector3(2.41f, -1.51f, 0f), new Vector3(3.61f, -1.44f, 0f), new Vector3(4.81f, -1.50f, 0f),
        new Vector3(0.15f, -2.20f, 0f), new Vector3(1.34f, -2.26f, 0f), new Vector3(2.54f, -2.18f, 0f), new Vector3(3.73f, -2.25f, 0f), new Vector3(4.93f, -2.20f, 0f)
    };
    private static readonly Vector3[] SubstituteAnchorSeeds =
    {
        new Vector3(-2.8f, 1.0f, 0f), new Vector3(-1.6f, 0.6f, 0f),
        new Vector3(-2.5f, 2.2f, 0f), new Vector3(-1.4f, 2.0f, 0f),
        new Vector3(1.2f, 1.7f, 0f), new Vector3(2.1f, 1.4f, 0f),
        new Vector3(1.8f, 0f, 0f), new Vector3(2.6f, 2.5f, 0f)
    };
    private static Sprite panelSprite;

    [MenuItem("Tools/CK01/Prepare ShortCycle Grid Piece Prefabs v2")]
    public static void PrepareGridPiecePrefabsFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("ShortCycle prefab preparation requires stable Edit Mode.");

        panelSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd") ?? LoadSprite("recipeSticker");
        if (panelSprite == null)
            throw new InvalidOperationException("No Sprite is available for the world-space placeholder panel.");

        EnsureGridPiecePrefabs();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("CK01-C-T5 PREFAB PREP PASS count=6 folder=" + PrefabFolder);
    }

    [MenuItem("Tools/CK01/Upgrade ShortCycle V2.2 Contracts")]
    public static void UpgradeV22ContractsFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("ShortCycle v2.2 upgrade requires stable Edit Mode.");

        panelSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd") ?? LoadSprite("recipeSticker");
        if (panelSprite == null)
            throw new InvalidOperationException("No Sprite is available for the v2.2 prefab contract.");

        var prefabChanged = UpgradeSlotBlockPrefabV22();
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        var sceneChanged = UpgradeExistingSceneV22(scene);
        if (sceneChanged && !EditorSceneManager.SaveScene(scene, ScenePath))
            throw new InvalidOperationException("Failed to save the v2.2 ShortCycle scene upgrade.");
        if (prefabChanged)
            AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("CK01-C-T6-V2.2 UPGRADE PASS prefabChanged=" + prefabChanged + " sceneChanged=" + sceneChanged);
    }

    [MenuItem("Tools/CK01/Migrate ShortCycle To Approved Layout v2")]
    public static void MigrateFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("ShortCycle migration requires stable Edit Mode.");

        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        var import = DataTableImportService.ImportAll();
        if (!import.success)
            throw new InvalidOperationException("CK01 data import failed: " + string.Join("; ", import.errors));

        var catalog = AssetDatabase.LoadAssetAtPath<CK01GeneratedDataCatalog>(CK01ImportRegistry.DataCatalogAssetPath);
        if (catalog == null || catalog.Tables.Count != 14)
            throw new InvalidOperationException("Expected the unique 14-table CK01GeneratedDataCatalog.");

        panelSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd") ?? LoadSprite("recipeSticker");
        if (panelSprite == null)
            throw new InvalidOperationException("No Sprite is available for the world-space placeholder panel.");
        foreach (var stray in scene.GetRootGameObjects().Where(x =>
            x.GetComponent<ShortCycleGridPieceBase>() != null &&
            new[] { "FridgeSlot", "TraySlot", "CatalogCard", "IngredientCard", "SlotBlock", "Sticker" }.Contains(x.name)))
            UnityEngine.Object.DestroyImmediate(stray);
        EnsureGridPiecePrefabs();

        foreach (var root in scene.GetRootGameObjects())
            UnityEngine.Object.DestroyImmediate(root);

        var cameraRoot = Root("CameraAndStageMarkers", 0);
        var managersRoot = Root("Managers", 1);
        var layersRoot = Root("InteractionLayers", 2);
        var spacesRoot = Root("Spaces", 3);
        var sharedRoot = Root("Shared", 4);
        var debugRoot = Root("DebugAndReferences", 5);
        var eventRoot = Root("EventSystem", 6);

        var cameraGo = Child("MainCamera", cameraRoot.transform, new Vector3(0f, 0f, -10f));
        cameraGo.tag = "MainCamera";
        var camera = cameraGo.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 7.2f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.18f, 0.20f, 0.24f, 1f);
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100f;
        cameraGo.AddComponent<AudioListener>();
        var lightGo = Child("DirectionalLight", cameraRoot.transform, Vector3.zero);
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 0.8f;
        lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        var p1Marker = Child("CamMarker_Phase1_Left_TUNE", cameraRoot.transform, new Vector3(-25.6f, 0f, -10f)).transform;
        var p0Marker = Child("CamMarker_Phase0_Center_TUNE", cameraRoot.transform, new Vector3(0f, 0f, -10f)).transform;
        var rightMarker = Child("CamMarker_RightReserved_TUNE", cameraRoot.transform, new Vector3(25.6f, 0f, -10f)).transform;
        var clueP0 = Child("ClueBoardAnchor_Phase0", cameraRoot.transform, new Vector3(-10.2f, 0f, -0.4f)).transform;
        var clueP1 = Child("ClueBoardAnchor_Phase1", cameraRoot.transform, new Vector3(-18.4f, 0f, -0.4f)).transform;

        var inputGo = Child("InputManager", managersRoot.transform, Vector3.zero);
        var playerInput = inputGo.AddComponent<PlayerInput>();
        playerInput.actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Scripts/Controller/PlayerControl.inputactions");
        playerInput.defaultActionMap = "Game";
        inputGo.AddComponent<InputManager>();
        var mouseGo = Child("MouseManager", managersRoot.transform, Vector3.zero);
        mouseGo.AddComponent<MouseManager>();

        var inventoryGo = Child("InventoryManager", managersRoot.transform, Vector3.zero);
        var inventory = inventoryGo.AddComponent<InventoryManager>();
        var reflow = Child("InventoryReflowAdapter", managersRoot.transform, Vector3.zero).AddComponent<ShortCycleInventoryReflowAdapter>();
        var profile = Child("PlayerProfileManager", managersRoot.transform, Vector3.zero).AddComponent<PlayerProfile>();
        var unlocks = Child("UnlockManager", managersRoot.transform, Vector3.zero).AddComponent<PlayerUnlockManager>();
        var equipment = Child("KitchenEquipmentManager", managersRoot.transform, Vector3.zero).AddComponent<KitchenEquipmentManager>();
        var inputLock = Child("InputLockController", managersRoot.transform, Vector3.zero).AddComponent<ShortCycleInputLockController>();
        var router = Child("ActionRouter", managersRoot.transform, Vector3.zero).AddComponent<ShortCycleActionRouter>();
        var cameraPan = Child("CameraPanController_TUNE", managersRoot.transform, Vector3.zero).AddComponent<ShortCycleCameraPanController>();
        var transition = Child("Phase2TransitionAdapter", managersRoot.transform, Vector3.zero).AddComponent<ShortCyclePhase2TransitionAdapter>();
        var handoff = Child("Phase2HandoffPresenter", managersRoot.transform, Vector3.zero).AddComponent<ShortCyclePhase2HandoffPresenter>();
        var upstream = Child("UpstreamExitPresenter", managersRoot.transform, Vector3.zero).AddComponent<ShortCycleUpstreamExitPresenter>();
        Child("DataProcessor", managersRoot.transform, Vector3.zero);
        Child("SaveBoundary", managersRoot.transform, Vector3.zero);
        var trayState = Child("TrayStateController", managersRoot.transform, Vector3.zero).AddComponent<ShortCycleTrayStateController>();
        var coordinator = Child("SessionManager", managersRoot.transform, Vector3.zero).AddComponent<ShortCycleSessionManager>();
        var presenter = Child("ScenePresenter", managersRoot.transform, Vector3.zero).AddComponent<ShortCycleScenePresenter>();
        var commandBridge = Child("CommandBridge", managersRoot.transform, Vector3.zero).AddComponent<ShortCycleCommandBridge>();
        var stackController = Child("InteractionLayerStackController", managersRoot.transform, Vector3.zero).AddComponent<ShortCycleInteractionLayerStack>();

        var phase0Layer = Child("Layer_Phase0", layersRoot.transform, Vector3.zero).AddComponent<MouseInteractionLayer>();
        var phase1Layer = Child("Layer_Phase1", layersRoot.transform, Vector3.zero).AddComponent<MouseInteractionLayer>();
        var modalLayer = Child("Layer_Modal", layersRoot.transform, Vector3.zero).AddComponent<MouseInteractionLayer>();
        ConfigureLayer(phase0Layer, 0, false, false);
        ConfigureLayer(phase1Layer, 10, false, false);
        ConfigureLayer(modalLayer, 20, true, true);

        SetObject(inventory, "dataCatalog", catalog);
        SetObject(inventory, "tightReflowAdapter", reflow);
        SetObject(router, "inputLockController", inputLock);
        SetObject(cameraPan, "controlledCamera", camera);
        SetObject(cameraPan, "phase0Marker", p0Marker);
        SetObject(cameraPan, "phase1Marker", p1Marker);
        SetObject(cameraPan, "rightReservedMarker", rightMarker);
        SetObject(cameraPan, "inputLockController", inputLock);
        SetFloat(cameraPan, "panDuration", 0.45f);
        SetObject(handoff, "transitionAdapter", transition);
        SetObject(coordinator, "inventoryManager", inventory);
        SetObject(coordinator, "playerProfile", profile);
        SetObject(coordinator, "playerUnlockManager", unlocks);
        SetObject(coordinator, "kitchenEquipmentManager", equipment);
        SetObject(coordinator, "actionRouter", router);
        SetObject(coordinator, "cameraPanController", cameraPan);
        SetObject(coordinator, "phase2HandoffPresenter", handoff);
        SetObject(coordinator, "upstreamExitPresenter", upstream);
        SetObject(commandBridge, "actionRouter", router);
        SetObject(stackController, "baseLayer", phase0Layer);
        SetObjects(stackController, "configuredLayers", new UnityEngine.Object[] { phase0Layer, phase1Layer, modalLayer });

        var phase0 = Child("Phase0_RecipeBook", spacesRoot.transform, Vector3.zero);
        var phase1 = Child("Phase1_Kitchen", spacesRoot.transform, new Vector3(-25.6f, 0f, 0f));
        var right = Child("RightReserved", spacesRoot.transform, new Vector3(25.6f, 0f, 0f));
        BuildPhase0(phase0.transform, catalog, coordinator, trayState, router);
        GameObject clueCollapsed;
        GameObject clueExpanded;
        var clueShell = BuildPhase1(phase1.transform, catalog, coordinator, trayState, router, out clueCollapsed, out clueExpanded);
        Art("ReservedBackdrop", right.transform, Vector3.zero, new Vector2(22f, 12f), new Color(0.24f, 0.26f, 0.30f, 1f), null, -20);
        Text("ReservedNotice", right.transform, "RIGHT RESERVED / FUTURE BRANCH", Vector3.zero, new Vector2(14f, 2f), 1.2f, Color.white, 0);

        var stepModal = Child("StepDetailModal", sharedRoot.transform, Vector3.zero); stepModal.SetActive(false);
        Art("Backdrop", stepModal.transform, Vector3.zero, new Vector2(10f, 8f), new Color(0.12f, 0.12f, 0.14f, 0.92f), null, 50);
        Text("Title", stepModal.transform, "STEP DETAIL", new Vector3(0f, 3f, -0.1f), new Vector2(8f, 1f), 1.2f, Color.white, 51);
        var genericModal = Child("GenericModal", sharedRoot.transform, Vector3.zero); genericModal.SetActive(false);
        var hoverCard = Child("HoverCard", sharedRoot.transform, Vector3.zero); hoverCard.SetActive(false);

        debugRoot.SetActive(false);
        var probeGo = Child("Probe_ShortCycleAutomated_Disposable", debugRoot.transform, Vector3.zero);
        var probe = probeGo.AddComponent<ShortCycleAutomatedProbe>();
        probe.enabled = false;
        SetObject(probe, "coordinator", coordinator);
        SetObject(probe, "presenter", presenter);
        SetObject(probe, "inputLockController", inputLock);
        SetObject(probe, "interactionLayerStack", stackController);
        SetObject(probe, "clueBoardLayer", modalLayer);
        SetObject(probe, "controlledCamera", camera);
        SetObject(probe, "phase0CameraMarker", p0Marker);
        SetObject(probe, "phase1CameraMarker", p1Marker);
        SetObject(probe, "clueBoardShell", clueShell.transform);
        SetObject(probe, "clueBoardPhase0Anchor", clueP0);
        SetObject(probe, "clueBoardPhase1Anchor", clueP1);
        SetFloat(probe, "presentationDuration", 0.45f);
        SetBool(probe, "runOnStart", false);
        Child("Ref_Reference_P0_NonDeletable", debugRoot.transform, Vector3.zero);
        Child("Ref_Reference_P1_NonDeletable", debugRoot.transform, Vector3.zero);
        var evidence = Text("EvidenceText", debugRoot.transform, "CK01-C-T5 / v2", Vector3.zero, new Vector2(8f, 1f), 1f, Color.white, 100);

        var eventSystemGo = Child("EventSystem_NewInputSystem", eventRoot.transform, Vector3.zero);
        eventSystemGo.AddComponent<EventSystem>();
        eventSystemGo.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();

        SetObject(presenter, "coordinator", coordinator);
        SetObject(presenter, "actionRouter", router);
        SetObject(presenter, "interactionLayerStack", stackController);
        SetObject(presenter, "clueBoardLayer", modalLayer);
        SetString(presenter, "recipeId", "rcp_tomato_egg");
        SetObject(presenter, "dataCatalog", catalog);
        SetObject(presenter, "coverView", phase0.transform.Find("Cover_Group").gameObject);
        SetObject(presenter, "recipeBrowseView", phase0.transform.Find("Browse_Group").gameObject);
        SetObject(presenter, "recipeCatalogView", phase0.transform.Find("Catalog_Group").gameObject);
        SetObject(presenter, "clueBoardShell", clueShell.transform);
        SetObject(presenter, "clueBoardPhase0Anchor", clueP0);
        SetObject(presenter, "clueBoardPhase1Anchor", clueP1);
        SetObject(presenter, "clueBoardCollapsedVisual", clueCollapsed);
        SetObject(presenter, "clueBoardExpandedVisual", clueExpanded);
        SetFloat(presenter, "presentationDuration", 0.45f);
        camera.transform.position = p0Marker.position;

        if (!EditorSceneManager.SaveScene(scene, ScenePath))
            throw new InvalidOperationException("Failed to save migrated ShortCycle scene.");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("CK01-C-T5 MIGRATION PASS roots=7 spacesUGUI=0 catalog=" + AssetDatabase.AssetPathToGUID(CK01ImportRegistry.DataCatalogAssetPath));
    }

    private static void BuildPhase0(Transform root, CK01GeneratedDataCatalog catalog, ShortCycleSessionManager session, ShortCycleTrayStateController tray, ShortCycleActionRouter router)
    {
        Art("Book_Base", root, Vector3.zero, new Vector2(21.5f, 12f), Paper, LoadSprite("recipePaper"), -10);
        Art("Book_Pages", root, new Vector3(0f, 0f, -0.05f), new Vector2(20.2f, 11.2f), new Color(0.98f, 0.94f, 0.82f, 1f), LoadSprite("menuPaper"), -9);
        var cover = Child("Cover_Group", root, Vector3.zero); cover.SetActive(false);
        Art("CoverArt_MissingPlaceholder", cover.transform, Vector3.zero, new Vector2(19f, 10.5f), new Color(0.44f, 0.30f, 0.22f, 1f), null, 0);
        Text("CoverTitle", cover.transform, "WHAT SHALL WE EAT?", new Vector3(0f, 1f, -0.1f), new Vector2(12f, 2f), 1.6f, Color.white, 1);
        Child("Bubble_01_Missing", cover.transform, new Vector3(-5f, 3f, 0f));
        Child("Bubble_02_Missing", cover.transform, new Vector3(5f, -3f, 0f));

        var browse = Child("Browse_Group", root, Vector3.zero);
        var left = Child("LeftPage", browse.transform, new Vector3(-5.2f, 0f, -0.15f));
        Art("TitleBackground", left.transform, new Vector3(0f, 4.2f, 0f), new Vector2(7.5f, 1.2f), Color.white, LoadSprite("foodNamePaper"), 2);
        Text("Title", left.transform, "番茄炒蛋", new Vector3(0f, 4.2f, -0.1f), new Vector2(6.5f, 1f), 1.25f, Ink, 3);
        Art("DishPhoto_TUNE", left.transform, new Vector3(-1.5f, 1.2f, 0f), new Vector2(5.6f, 4.4f), Color.white, LoadSprite("foodPhotoNormal"), 2).transform.localRotation = Quaternion.Euler(0f, 0f, -8f);
        var ingredients = Child("IngredientList", left.transform, new Vector3(-3.5f, -2f, 0f));
        var ingredientIds = new[] { "ing_tomato", "ing_egg", "stp_oil", "stp_salt" };
        for (var i = 0; i < ingredientIds.Length; i++)
        {
            CK01ItemData item; string error;
            catalog.TryGetItem(ingredientIds[i], out item, out error);
            var card = InstantiatePiece<IngredientCard>("IngredientCard", ingredients.transform, "IngredientCard_" + (i + 1).ToString("00"), session, tray, ShortCyclePhase.Phase0);
            card.ConfigureIngredient(item, i < 2 ? 2 : 1);
        }
        var vertical = ingredients.AddComponent<VerticalLayout>(); vertical.SetCellSize(new Vector2(3.4f, 1.0f)); vertical.SetSpacing(0.18f); vertical.ArrangeChildren();
        var stickers = Child("StickerNotes_TUNE", left.transform, new Vector3(2.6f, 2.7f, 0f));
        for (var i = 0; i < 3; i++) InstantiatePiece<Sticker>("Sticker", stickers.transform, "StickerPoint_" + (i + 1).ToString("00") + "_TUNE", session, tray, ShortCyclePhase.Phase0);
        var stickerGrid = stickers.AddComponent<GridLayout>(); stickerGrid.SetColumnCount(2); stickerGrid.SetCellSize(new Vector2(1.6f, 1.1f)); stickerGrid.SetSpacing(0.2f, 0.2f); stickerGrid.ArrangeChildren();
        Child("StarNotes_Deco", left.transform, new Vector3(2f, -1.3f, 0f));
        AddActionButton("StartCooking_Button", left.transform, "START COOKING", new Vector3(2.7f, -4.1f, -0.2f), new Vector2(4.4f, 1.1f), new Color(0.94f, 0.58f, 0.18f, 1f), router, ShortCycleActionNames.StartCooking, false);
        Child("Favorite_Button_Missing", left.transform, new Vector3(3.7f, 3.6f, 0f));
        Child("OpenClueBoard_Button_Missing", left.transform, new Vector3(-4.2f, 4.2f, 0f));
        Child("StepDetail_Button", left.transform, new Vector3(3.8f, -2.2f, 0f));
        Child("RightPage_Reserved", browse.transform, new Vector3(5.2f, 0f, 0f));

        var catalogGroup = Child("Catalog_Group", root, Vector3.zero); catalogGroup.SetActive(false);
        var leftGrid = Child("CatalogGrid_Left", catalogGroup.transform, new Vector3(-5.2f, 3.3f, 0f));
        var rightGrid = Child("CatalogGrid_Right", catalogGroup.transform, new Vector3(1f, 3.3f, 0f));
        for (var i = 0; i < 8; i++)
        {
            var target = i < 4 ? leftGrid.transform : rightGrid.transform;
            InstantiatePiece<CatalogCard>("CatalogCard", target, "CatalogCard_" + (i + 1).ToString("00"), session, tray, ShortCyclePhase.Phase0);
        }
        foreach (var gridRoot in new[] { leftGrid, rightGrid })
        {
            var grid = gridRoot.AddComponent<GridLayout>(); grid.SetColumnCount(2); grid.SetCellSize(new Vector2(4.4f, 3.8f)); grid.SetSpacing(0.45f, 0.45f); grid.ArrangeChildren();
        }
        Child("FlipAnchor_L", catalogGroup.transform, new Vector3(-10f, 0f, 0f));
        Child("FlipAnchor_R", catalogGroup.transform, new Vector3(10f, 0f, 0f));
        AddActionButton("CatalogTab_Button_TUNE", root, "CATALOG", new Vector3(-10.5f, 3.6f, -0.2f), new Vector2(2.3f, 1.0f), new Color(0.88f, 0.45f, 0.35f, 1f), router, ShortCycleActionNames.StartCooking, false);
        AddActionButton("Escape_Button_TUNE", root, "ESC", new Vector3(-10.5f, -5.5f, -0.2f), new Vector2(1.8f, 0.9f), new Color(0.30f, 0.34f, 0.38f, 1f), router, ShortCycleActionNames.Escape, true);
    }

    private static GameObject BuildPhase1(Transform root, CK01GeneratedDataCatalog catalog, ShortCycleSessionManager session, ShortCycleTrayStateController tray, ShortCycleActionRouter router, out GameObject collapsed, out GameObject expanded)
    {
        var fridge = Child("FridgeCat_Group", root, new Vector3(-4.8f, 0.6f, 0f));
        Art("CatBody", fridge.transform, Vector3.zero, new Vector2(12f, 11.5f), Color.white, LoadSprite("chuju_bingxiangmao_zhutu"), -3);
        Art("EyeGlow", fridge.transform, new Vector3(0f, 0f, -0.2f), new Vector2(12f, 11.5f), Color.white, LoadSprite("chuju_bingxiangmao_yanguang"), -2);
        var deco = Child("Deco_Lines", fridge.transform, Vector3.zero);
        for (var i = 1; i <= 4; i++) Art("Line_" + i, deco.transform, Vector3.zero, new Vector2(12f, 11.5f), Color.white, LoadSprite("chuju_bingxiangmao_xian" + i), -1 + i);
        var shelves = Child("ShelfSlots_TUNE", fridge.transform, new Vector3(-2.9f, 2.6f, -0.4f));
        var ids = new[] { "ing_egg", "ing_tomato", "ing_cabbage", "ing_scallion", "ing_noodle_dried", "ing_noodle_instant", "ing_octopus_leg", "ing_ham_sausage", "ing_milk_box", "stp_salt", "stp_oil", "stp_water", "prd_clear_broth", "prd_egg_liquid", "prd_tomato_cut" };
        for (var i = 0; i < ids.Length; i++)
        {
            CK01ItemData item; string error;
            catalog.TryGetItem(ids[i], out item, out error);
            var slot = InstantiatePiece<FridgeSlot>("FridgeSlot", shelves.transform, "FridgeSlot_R" + (i / 5 + 1).ToString("00") + "_C" + (i % 5 + 1).ToString("00") + "_TUNE", session, tray, ShortCyclePhase.Phase1);
            slot.ConfigureItem(item, i % 4 + 1, item == null || item.icon_sprite == null);
        }
        var shelfGrid = shelves.AddComponent<GridLayout>(); shelfGrid.SetColumnCount(5); shelfGrid.SetCellSize(new Vector2(1.55f, 1.45f)); shelfGrid.SetSpacing(0.18f, 0.20f); shelfGrid.ArrangeChildren();
        var filter = Child("FilterBar_TUNE", fridge.transform, new Vector3(-3.8f, 5.1f, 0f));
        for (var i = 0; i < 5; i++) Child("FilterTag_" + (i + 1).ToString("00"), filter.transform, Vector3.zero);
        var horizontal = filter.AddComponent<HorizontalLayout>(); horizontal.SetCellSize(new Vector2(1.2f, 0.6f)); horizontal.SetSpacing(0.15f); horizontal.ArrangeChildren();
        Child("TidyButton_TUNE_Missing", fridge.transform, new Vector3(4.4f, 4.8f, 0f));
        Child("ScrollRegion", fridge.transform, Vector3.zero).AddComponent<MouseScrollableObject>();

        var trayGroup = Child("Tray_Group", root, new Vector3(-5f, -4.1f, 0f));
        Art("TrayBase", trayGroup.transform, Vector3.zero, new Vector2(10.5f, 4f), Color.white, LoadSprite("chuju_bingxiangmao_tuopan"), 8);
        var traySlots = Child("TraySlots", trayGroup.transform, new Vector3(-3.4f, 1.1f, -0.3f));
        var trayLayout = traySlots.AddComponent<ShortCycleTrayAnchorLayout>();
        var trayAnchors = new UnityEngine.Object[ShortCycleAnchorContract.TrayAnchorCount];
        for (var i = 0; i < ShortCycleAnchorContract.TrayAnchorCount; i++)
        {
            var anchor = Child(
                ShortCycleAnchorContract.GetTrayAnchorName(i),
                traySlots.transform,
                GetTrayAnchorSeed(i));
            InstantiatePiece<TraySlot>("TraySlot", anchor.transform, "TraySlot", session, tray, ShortCyclePhase.Phase1);
            trayAnchors[i] = anchor.transform;
        }
        SetObjects(trayLayout, "traySlotAnchors", trayAnchors);
        Text("CapacityText_TUNE", trayGroup.transform, "0 / 20", new Vector3(2.8f, -1.3f, -0.4f), new Vector2(2.4f, 0.8f), 0.9f, Color.white, 20);
        AddActionButton("GoDog_Button_TUNE", trayGroup.transform, "GO!", new Vector3(6.2f, -0.5f, -0.4f), new Vector2(2.6f, 1.5f), new Color(0.96f, 0.64f, 0.20f, 1f), router, ShortCycleActionNames.AdvanceToPhase2, false, LoadSprite("chuju_bingxiangmao_kaishigou"));
        Child("TrayStateMachine", trayGroup.transform, Vector3.zero);

        var clue = Child("ClueBoard_Group", root, new Vector3(7.2f, 0f, -0.3f));
        Art("Board", clue.transform, Vector3.zero, new Vector2(7.6f, 12.6f), Color.white, LoadSprite("chuju_bingxiangmao_ban"), 10);
        Art("Clip", clue.transform, new Vector3(0f, 5.3f, -0.1f), new Vector2(2.2f, 1.2f), Color.white, LoadSprite("chuju_bingxiangmao_ban1"), 11);
        Art("Paper", clue.transform, new Vector3(0f, 0.3f, -0.2f), new Vector2(6.6f, 10.5f), Color.white, LoadSprite("chuju_bingxiangmao_zhi"), 12);
        Art("DarkBoard", clue.transform, new Vector3(0f, -3.7f, -0.3f), new Vector2(5.6f, 2.4f), Color.white, LoadSprite("chuju_bingxiangmao_ban2"), 13);
        var content = Child("Content", clue.transform, new Vector3(-2.4f, 3.8f, -0.4f));
        var blocks = Child("SlotBlocks", content.transform, Vector3.zero);
        for (var i = 0; i < 4; i++) InstantiatePiece<SlotBlock>("SlotBlock", blocks.transform, "SlotBlock_" + (i + 1).ToString("00"), session, tray, ShortCyclePhase.Phase1);
        var blockLayout = blocks.AddComponent<VerticalLayout>(); blockLayout.SetCellSize(new Vector2(4.8f, 1.5f)); blockLayout.SetSpacing(0.25f); blockLayout.ArrangeChildren();
        Text("TipsBubble", content.transform, "MATCH THE RECIPE CLUES", new Vector3(2.4f, -7.2f, 0f), new Vector2(5.2f, 1f), 0.75f, Ink, 20);
        Child("Deco", clue.transform, Vector3.zero);
        collapsed = Child("Collapse_Group", clue.transform, Vector3.zero);
        expanded = Child("Expand_Group", clue.transform, Vector3.zero); expanded.SetActive(false);
        AddActionButton("Return_Button_TUNE", root, "RETURN", new Vector3(-10.4f, -5.5f, -0.2f), new Vector2(2.4f, 1.0f), new Color(0.92f, 0.78f, 0.34f, 1f), router, ShortCycleActionNames.Return, false, LoadSprite("chuju_bingxiangmao_zhifanhui"));
        Child("EquipmentEntry_Button_TUNE_Missing", root, new Vector3(0.4f, -5.5f, 0f));
        return clue;
    }

    private static T InstantiatePiece<T>(string prefabName, Transform parent, string name, ShortCycleSessionManager session, ShortCycleTrayStateController tray, ShortCyclePhase phase) where T : ShortCycleGridPieceBase
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/" + prefabName + ".prefab");
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.name = name;
        var piece = go.GetComponent<T>();
        SetObject(piece, "sessionManager", session);
        SetObject(piece, "trayStateController", tray);
        SetEnum(piece, "requiredPhase", (int)phase);
        return piece;
    }

    private static void EnsureGridPiecePrefabs()
    {
        EnsureFolder(PrefabFolder);
        EnsurePiecePrefab<FridgeSlot>("FridgeSlot", new Vector2(1.35f, 1.25f));
        EnsurePiecePrefab<TraySlot>("TraySlot", new Vector2(1.0f, 0.65f));
        EnsurePiecePrefab<CatalogCard>("CatalogCard", new Vector2(4.0f, 3.4f));
        EnsurePiecePrefab<IngredientCard>("IngredientCard", new Vector2(3.2f, 0.85f));
        EnsurePiecePrefab<SlotBlock>("SlotBlock", new Vector2(4.6f, 1.25f));
        EnsurePiecePrefab<Sticker>("Sticker", new Vector2(1.4f, 1.0f));
        UpgradeSlotBlockPrefabV22();
    }

    private static bool UpgradeExistingSceneV22(UnityEngine.SceneManagement.Scene scene)
    {
        var spaces = scene.GetRootGameObjects().FirstOrDefault(value => value.name == "Spaces");
        if (spaces == null)
            throw new InvalidOperationException("AI-000033 Spaces root is missing; refusing a destructive rebuild.");

        var traySlots = spaces.transform.Find("Phase1_Kitchen/Tray_Group/TraySlots");
        if (traySlots == null)
            throw new InvalidOperationException("AI-000033 TraySlots path is missing; refusing a destructive rebuild.");

        var children = new List<Transform>();
        for (var index = 0; index < traySlots.childCount; index++)
            children.Add(traySlots.GetChild(index));
        if (children.Count != ShortCycleAnchorContract.TrayAnchorCount)
            throw new InvalidOperationException("Expected exactly 20 existing tray slot instances, found " + children.Count + ".");

        var changed = false;
        var grid = traySlots.GetComponent<GridLayout>();
        if (grid != null)
        {
            UnityEngine.Object.DestroyImmediate(grid);
            changed = true;
        }

        var layout = traySlots.GetComponent<ShortCycleTrayAnchorLayout>();
        if (layout == null)
        {
            layout = traySlots.gameObject.AddComponent<ShortCycleTrayAnchorLayout>();
            changed = true;
        }

        var anchors = new UnityEngine.Object[children.Count];
        for (var index = 0; index < children.Count; index++)
        {
            var expectedName = ShortCycleAnchorContract.GetTrayAnchorName(index);
            if (children[index].name != expectedName)
            {
                children[index].name = expectedName;
                changed = true;
            }
            anchors[index] = children[index];
        }
        changed |= SetObjectsIfChanged(layout, "traySlotAnchors", anchors);
        return changed;
    }

    private static bool UpgradeSlotBlockPrefabV22()
    {
        var path = PrefabFolder + "/SlotBlock.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        if (root == null)
            throw new InvalidOperationException("SlotBlock prefab is missing at " + path + ".");

        var changed = false;
        try
        {
            var slotBlock = root.GetComponent<SlotBlock>();
            if (slotBlock == null)
                throw new InvalidOperationException("SlotBlock prefab has no SlotBlock component.");

            var center = EnsureChild("Center", root.transform, Vector3.zero, ref changed);
            var icon = root.transform.Find("Icon");
            if (icon != null && icon.parent != center.transform)
            {
                icon.SetParent(center.transform, false);
                changed = true;
            }
            var title = root.transform.Find("NameLabel");
            if (title != null && title.parent != center.transform)
            {
                title.SetParent(center.transform, false);
                changed = true;
            }
            var actionIcon = EnsureSpriteRenderer("ActionIcon", center.transform, new Vector3(1.5f, -0.5f, -0.1f), ref changed);
            changed |= SetObjectIfChanged(slotBlock, "centerGroup", center.transform);
            changed |= SetObjectIfChanged(slotBlock, "actionIconRenderer", actionIcon);

            var anchorObjects = new UnityEngine.Object[ShortCycleAnchorContract.SubstituteAnchorCount];
            var cardObjects = new UnityEngine.Object[ShortCycleAnchorContract.SubstituteAnchorCount];
            var subSlots = EnsureChild("SubSlots", root.transform, Vector3.zero, ref changed);
            for (var index = 0; index < ShortCycleAnchorContract.SubstituteAnchorCount; index++)
            {
                var anchor = EnsureChild(
                    ShortCycleAnchorContract.GetSubstituteAnchorName(index),
                    subSlots.transform,
                    GetSubstituteAnchorSeed(index),
                    ref changed);
                var cardRoot = EnsureChild("SubCard", anchor.transform, Vector3.zero, ref changed);
                var card = cardRoot.GetComponent<ShortCycleSubCard>();
                if (card == null)
                {
                    card = cardRoot.AddComponent<ShortCycleSubCard>();
                    changed = true;
                }
                var cardBackground = EnsureSpriteRenderer("Card", cardRoot.transform, Vector3.zero, ref changed);
                if (cardBackground.sprite == null)
                {
                    cardBackground.sprite = panelSprite;
                    changed = true;
                }
                var subIcon = EnsureSpriteRenderer("Icon", cardRoot.transform, new Vector3(0f, 0.1f, -0.1f), ref changed);
                var count = EnsureText("Count", cardRoot.transform, new Vector3(0f, -0.45f, -0.2f), ref changed);
                var node = EnsureChild("Node", anchor.transform, new Vector3(0f, -0.8f, 0f), ref changed);
                var link = EnsureSpriteRenderer("Link", anchor.transform, new Vector3(0f, -0.55f, 0f), ref changed);
                changed |= SetObjectIfChanged(card, "cardRoot", cardRoot);
                changed |= SetObjectIfChanged(card, "iconRenderer", subIcon);
                changed |= SetObjectIfChanged(card, "countLabel", count);
                changed |= SetObjectIfChanged(card, "node", node);
                changed |= SetObjectIfChanged(card, "link", link);
                anchorObjects[index] = anchor.transform;
                cardObjects[index] = card;
            }
            changed |= SetObjectsIfChanged(slotBlock, "subSlotAnchors", anchorObjects);
            changed |= SetObjectsIfChanged(slotBlock, "subCards", cardObjects);

            if (changed)
                PrefabUtility.SaveAsPrefabAsset(root, path);
            return changed;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static GameObject EnsureChild(string name, Transform parent, Vector3 localPosition, ref bool changed)
    {
        var existing = parent.Find(name);
        if (existing != null && existing.parent == parent)
            return existing.gameObject;
        changed = true;
        return Child(name, parent, localPosition);
    }

    private static SpriteRenderer EnsureSpriteRenderer(string name, Transform parent, Vector3 localPosition, ref bool changed)
    {
        var child = EnsureChild(name, parent, localPosition, ref changed);
        var renderer = child.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = child.AddComponent<SpriteRenderer>();
            changed = true;
        }
        return renderer;
    }

    private static TextMeshPro EnsureText(string name, Transform parent, Vector3 localPosition, ref bool changed)
    {
        var child = EnsureChild(name, parent, localPosition, ref changed);
        var text = child.GetComponent<TextMeshPro>();
        if (text == null)
        {
            text = child.AddComponent<TextMeshPro>();
            changed = true;
        }
        return text;
    }

    private static Vector3 GetTrayAnchorSeed(int index)
    {
        return TrayAnchorSeeds[index];
    }

    private static Vector3 GetSubstituteAnchorSeed(int index)
    {
        return SubstituteAnchorSeeds[index];
    }

    private static void EnsurePiecePrefab<T>(string name, Vector2 size) where T : ShortCycleGridPieceBase
    {
        var go = new GameObject(name, typeof(SortingGroup), typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(Button_MouseInteract), typeof(ColorSpriteButton_Visual), typeof(T));
        try
        {
            var renderer = go.GetComponent<SpriteRenderer>(); renderer.sprite = panelSprite; renderer.color = new Color(0.92f, 0.84f, 0.62f, 1f); Fit(renderer, size);
            go.GetComponent<BoxCollider2D>().size = size;
            var icon = Art("Icon", go.transform, new Vector3(-size.x * 0.25f, 0f, -0.1f), new Vector2(size.y * 0.65f, size.y * 0.65f), Color.white, panelSprite, 2);
            var placeholder = Art("Placeholder", go.transform, new Vector3(-size.x * 0.25f, 0f, -0.2f), new Vector2(size.y * 0.5f, size.y * 0.5f), new Color(0.85f, 0.30f, 0.36f, 0.75f), panelSprite, 3);
            placeholder.enabled = false;
            var label = Text("NameLabel", go.transform, "NAME", new Vector3(size.x * 0.16f, 0f, -0.1f), new Vector2(size.x * 0.55f, size.y * 0.6f), Math.Max(0.32f, size.y * 0.42f), Ink, 4);
            var badge = Text("BadgeLabel", go.transform, string.Empty, new Vector3(size.x * 0.34f, -size.y * 0.3f, -0.15f), new Vector2(size.x * 0.25f, size.y * 0.3f), Math.Max(0.28f, size.y * 0.32f), Ink, 5);
            var button = go.GetComponent<Button_MouseInteract>();
            var visual = go.GetComponent<ColorSpriteButton_Visual>();
            InitializeButtonEvents(button);
            button.visualComponents.Add(visual);
            SetObject(visual, "targetSprite", renderer);
            SetColor(visual, "idleColor", renderer.color);
            SetColor(visual, "highLightColor", Color.Lerp(renderer.color, Color.white, 0.35f));
            SetColor(visual, "pressedColor", Color.Lerp(renderer.color, Color.black, 0.25f));
            UnityEventTools.AddPersistentListener(button.overEvent, visual.setHighlight);
            UnityEventTools.AddPersistentListener(button.outEvent, visual.setIdle);
            UnityEventTools.AddPersistentListener(button.selectEvent, visual.OnPress);
            var piece = go.GetComponent<T>();
            SetObject(piece, "sortingGroup", go.GetComponent<SortingGroup>());
            SetObject(piece, "iconRenderer", icon);
            SetObject(piece, "nameLabel", label);
            SetObject(piece, "badgeLabel", badge);
            SetObject(piece, "placeholderRenderer", placeholder);
            SetObjects(piece, "mouseInteractBehaviours", new UnityEngine.Object[] { button });
            PrefabUtility.SaveAsPrefabAsset(go, PrefabFolder + "/" + name + ".prefab");
        }
        finally
        {
            if (go != null) UnityEngine.Object.DestroyImmediate(go);
        }
    }

    private static GameObject AddActionButton(string name, Transform parent, string label, Vector3 position, Vector2 size, Color color, ShortCycleActionRouter router, string action, bool escapeKey, Sprite sprite = null)
    {
        var go = Child(name, parent, position);
        var renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = sprite != null ? sprite : panelSprite; renderer.color = color; Fit(renderer, size); renderer.sortingOrder = 30;
        var collider = go.AddComponent<BoxCollider2D>(); collider.size = size;
        var button = go.AddComponent<Button_MouseInteract>();
        var visual = go.AddComponent<ColorSpriteButton_Visual>();
        InitializeButtonEvents(button);
        var binding = go.AddComponent<ShortCycleMouseActionBinding>(); binding.Configure(router, action);
        button.visualComponents.Add(visual);
        SetObject(visual, "targetSprite", renderer);
        SetColor(visual, "idleColor", color);
        SetColor(visual, "highLightColor", Color.Lerp(color, Color.white, 0.35f));
        SetColor(visual, "pressedColor", Color.Lerp(color, Color.black, 0.3f));
        UnityEventTools.AddPersistentListener(button.overEvent, visual.setHighlight);
        UnityEventTools.AddPersistentListener(button.outEvent, visual.setIdle);
        UnityEventTools.AddPersistentListener(button.selectEvent, visual.OnPress);
        UnityEventTools.AddPersistentListener(button.selectEvent, binding.DispatchFromMouseEvent);
        if (escapeKey)
        {
            var keyboard = go.AddComponent<KeyboardListener>(); keyboard.targetKey = Key.Escape;
            UnityEventTools.AddPersistentListener(keyboard.keyPressEvent, button.TriggerSelect);
        }
        Text("Label", go.transform, label, new Vector3(0f, 0f, -0.2f), size, Math.Max(0.5f, size.y * 0.55f), Ink, 31);
        return go;
    }

    private static GameObject Root(string name, int sibling)
    {
        var go = new GameObject(name); go.transform.SetSiblingIndex(sibling); return go;
    }
    private static GameObject Child(string name, Transform parent, Vector3 localPosition)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = localPosition; return go;
    }
    private static SpriteRenderer Art(string name, Transform parent, Vector3 position, Vector2 size, Color color, Sprite sprite, int order)
    {
        var go = Child(name, parent, position); var renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = sprite != null ? sprite : panelSprite; renderer.color = color; renderer.sortingOrder = order; Fit(renderer, size); return renderer;
    }
    private static TextMeshPro Text(string name, Transform parent, string value, Vector3 position, Vector2 size, float fontSize, Color color, int order)
    {
        var go = Child(name, parent, position); var text = go.AddComponent<TextMeshPro>(); text.text = value; text.fontSize = fontSize; text.color = color; text.alignment = TextAlignmentOptions.Center; text.enableWordWrapping = true; var rect = go.GetComponent<RectTransform>(); if (rect != null) rect.sizeDelta = size; text.sortingOrder = order; return text;
    }
    private static void Fit(SpriteRenderer renderer, Vector2 size)
    {
        var bounds = renderer.sprite == null ? Vector2.one : (Vector2)renderer.sprite.bounds.size;
        renderer.transform.localScale = new Vector3(size.x / Math.Max(0.001f, bounds.x), size.y / Math.Max(0.001f, bounds.y), 1f);
    }
    private static void InitializeButtonEvents(Button_MouseInteract button)
    {
        if (button.overEvent == null) button.overEvent = new UnityEngine.Events.UnityEvent();
        if (button.outEvent == null) button.outEvent = new UnityEngine.Events.UnityEvent();
        if (button.selectEvent == null) button.selectEvent = new UnityEngine.Events.UnityEvent();
        if (button.delayedSelectEvent == null) button.delayedSelectEvent = new UnityEngine.Events.UnityEvent();
    }
    private static Sprite LoadSprite(string name)
    {
        var guid = AssetDatabase.FindAssets(name + " t:Sprite").FirstOrDefault();
        return string.IsNullOrEmpty(guid) ? null : AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guid));
    }
    private static void ConfigureLayer(MouseInteractionLayer layer, int id, bool manual, bool cancel)
    {
        layer.layerID = id; layer.manualStackLayer = manual; layer.clickNullEqualsCancel = cancel; layer.alloweInteractionLayers = new List<LayerMask> { (LayerMask)~0 };
    }
    private static void EnsureFolder(string path)
    {
        var current = "Assets";
        foreach (var part in path.Substring("Assets/".Length).Split('/'))
        {
            var next = current + "/" + part;
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, part);
            current = next;
        }
    }
    private static SerializedProperty Prop(UnityEngine.Object target, string name) { var so = new SerializedObject(target); var p = so.FindProperty(name); if (p == null) throw new InvalidOperationException(target.GetType().Name + "." + name + " missing"); return p; }
    private static void Apply(SerializedProperty p) { p.serializedObject.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(p.serializedObject.targetObject); }
    private static void SetObject(UnityEngine.Object target, string name, UnityEngine.Object value) { var p = Prop(target, name); p.objectReferenceValue = value; Apply(p); }
    private static void SetString(UnityEngine.Object target, string name, string value) { var p = Prop(target, name); p.stringValue = value; Apply(p); }
    private static void SetFloat(UnityEngine.Object target, string name, float value) { var p = Prop(target, name); p.floatValue = value; Apply(p); }
    private static void SetBool(UnityEngine.Object target, string name, bool value) { var p = Prop(target, name); p.boolValue = value; Apply(p); }
    private static void SetEnum(UnityEngine.Object target, string name, int value) { var p = Prop(target, name); p.enumValueIndex = value; Apply(p); }
    private static void SetColor(UnityEngine.Object target, string name, Color value) { var p = Prop(target, name); p.colorValue = value; Apply(p); }
    private static void SetObjects(UnityEngine.Object target, string name, UnityEngine.Object[] values)
    {
        var p = Prop(target, name); p.arraySize = values.Length; for (var i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i]; Apply(p);
    }
    private static bool SetObjectIfChanged(UnityEngine.Object target, string name, UnityEngine.Object value)
    {
        var p = Prop(target, name);
        if (p.objectReferenceValue == value) return false;
        p.objectReferenceValue = value;
        Apply(p);
        return true;
    }
    private static bool SetObjectsIfChanged(UnityEngine.Object target, string name, UnityEngine.Object[] values)
    {
        var p = Prop(target, name);
        var changed = p.arraySize != values.Length;
        if (!changed)
        {
            for (var index = 0; index < values.Length; index++)
            {
                if (p.GetArrayElementAtIndex(index).objectReferenceValue != values[index])
                {
                    changed = true;
                    break;
                }
            }
        }
        if (!changed) return false;
        p.arraySize = values.Length;
        for (var index = 0; index < values.Length; index++)
            p.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
        Apply(p);
        return true;
    }
}
