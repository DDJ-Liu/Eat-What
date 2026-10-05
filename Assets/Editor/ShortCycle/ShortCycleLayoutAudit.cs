using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using EatWhat.Tools.Layout;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Independent read-only structural audit for the ShortCycle scene.
/// It never changes scene objects, serialized assets, transforms, or selection.
/// </summary>
public static class ShortCycleLayoutAudit
{
    private const string ShortCycleStaticFontPath =
        "Assets/Font/Heavy/ShortCycle_Heavy_Static.asset";

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

    private static readonly Regex ForbiddenName =
        new Regex(@"(^AI\d+_)|(_GuideTarget$)|(InactiveBy)", RegexOptions.CultureInvariant);

    [MenuItem("Tools/CK01/Audit ShortCycle Layout (Read Only)")]
    public static void AuditFromMenu()
    {
        Debug.Log(AuditActiveScene());
    }

    public static string AuditActiveScene()
    {
        var scene = EditorSceneManager.GetActiveScene();
        var roots = scene.GetRootGameObjects();
        var all = roots.SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Select(transform => transform.gameObject)
            .Distinct()
            .ToArray();

        var rootOrder = roots.Select(root => root.name).ToArray();
        var rootOrderValid = rootOrder.SequenceEqual(ExpectedRoots);
        var forbidden = all.Where(go => ForbiddenName.IsMatch(go.name))
            .Select(GetPath)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        var spaces = roots.FirstOrDefault(root => root.name == "Spaces");
        var spacesObjects = spaces == null
            ? new GameObject[0]
            : spaces.GetComponentsInChildren<Transform>(true)
                .Select(transform => transform.gameObject)
                .Distinct()
                .ToArray();
        var shared = roots.FirstOrDefault(root => root.name == "Shared");
        var tmpScopeObjects = new[] { spaces, shared }
            .Where(root => root != null)
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Select(transform => transform.gameObject)
            .Distinct()
            .ToArray();
        var uguiCount = spacesObjects.Sum(go =>
            go.GetComponents<Component>().Count(component =>
                component is Canvas ||
                component is TextMeshProUGUI ||
                component is Selectable ||
                (component is Graphic && !(component is TextMeshPro))));

        var missingScripts = all.Sum(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount);
        var enabledRenderers = spacesObjects
            .SelectMany(go => go.GetComponents<Renderer>())
            .Count(renderer => renderer != null && renderer.enabled && renderer.gameObject.activeInHierarchy);
        var enabledTmp = spacesObjects
            .SelectMany(go => go.GetComponents<TMP_Text>())
            .Count(label => label != null && label.enabled && label.gameObject.activeInHierarchy);
        var spacesTmp = spacesObjects
            .SelectMany(go => go.GetComponents<TMP_Text>())
            .Where(label => label != null)
            .Distinct()
            .ToArray();
        var spacesStaticFontMismatchPaths = spacesTmp
            .Where(label => AssetDatabase.GetAssetPath(label.font) != ShortCycleStaticFontPath)
            .Select(label => GetPath(label.gameObject))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var spacesStaticFontValid = spacesTmp.Length > 0 &&
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ShortCycleStaticFontPath) != null &&
            spacesStaticFontMismatchPaths.Length == 0;
        var heavyTmp = tmpScopeObjects
            .SelectMany(go => go.GetComponents<TextMeshPro>())
            .Where(label => label != null && label.font != null &&
                label.font.name == "ShortCycle_Heavy_WithFallback")
            .Distinct()
            .ToArray();
        var allWorldTmp = tmpScopeObjects
            .SelectMany(go => go.GetComponents<TextMeshPro>())
            .Where(label => label != null)
            .Distinct()
            .ToArray();
        var heavyTmpRenderers = heavyTmp
            .Select(label => label.GetComponent<MeshRenderer>())
            .ToArray();
        var heavyTmpRendererMissing = heavyTmpRenderers.Count(renderer => renderer == null);
        var heavyTmpRendererDisabledPaths = heavyTmp
            .Where(label =>
            {
                var renderer = label.GetComponent<MeshRenderer>();
                return renderer != null && !renderer.enabled;
            })
            .Select(label => GetPath(label.gameObject))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var heavyTmpRendererEnabled = heavyTmpRenderers.Count(renderer => renderer != null && renderer.enabled);
        var heavyTmpRendererEnabledValid = heavyTmp.Length > 0 &&
            heavyTmp.Length == allWorldTmp.Length &&
            heavyTmpRendererMissing == 0 && heavyTmpRendererDisabledPaths.Length == 0 &&
            heavyTmpRendererEnabled == heavyTmp.Length;

        var director = all.Select(go => go.GetComponent<EatWhat.Cooking.ShortCycle.ShortCyclePresentationDirector>())
            .FirstOrDefault(component => component != null);
        var session = all.Select(go => go.GetComponent<EatWhat.Cooking.ShortCycle.ShortCycleSessionManager>())
            .FirstOrDefault(component => component != null);
        var layerStack = all.Select(go => go.GetComponent<EatWhat.Cooking.ShortCycle.ShortCycleInteractionLayerStack>())
            .FirstOrDefault(component => component != null);
        var targetSetNames = new[]
        {
            "Cover_Group", "Browse_Group", "Catalog_Group",
            "FridgeCat_Group", "Tray_Group", "ClueBoard_Group"
        };
        var targetSetCounts = targetSetNames.Select(name =>
        {
            var targets = all.Where(go => go.name == name).ToArray();
            return name + ":" + targets.Sum(target =>
                target.GetComponents<EatWhat.Cooking.ShortCycle.ShortCyclePresentationSet>().Length);
        }).ToArray();
        var targetSetsValid = targetSetCounts.All(value => value.EndsWith(":1", StringComparison.Ordinal));
        var clueBoard = all.SingleOrDefault(go => go.name == "ClueBoard_Group");
        var clueParentValid = clueBoard != null && clueBoard.transform.parent != null &&
            clueBoard.transform.parent.name == "Shared";
        var phase1Marker = all.SingleOrDefault(go => GetPath(go) ==
            "CameraAndStageMarkers/CamMarker_Phase1_Left_TUNE");
        var phase0LeftMarker = all.SingleOrDefault(go => GetPath(go) ==
            "CameraAndStageMarkers/CamMarker_Phase0_Left_TUNE");
        var phase0CenterMarker = all.SingleOrDefault(go => GetPath(go) ==
            "CameraAndStageMarkers/CamMarker_Phase0_Center_TUNE");
        var rightMarker = all.SingleOrDefault(go => GetPath(go) ==
            "CameraAndStageMarkers/CamMarker_RightReserved_TUNE");
        var fourMarkerPositionsValid =
            HasWorldX(phase1Marker, -25.6f) &&
            HasWorldX(phase0LeftMarker, -12.8f) &&
            HasWorldX(phase0CenterMarker, 0f) &&
            HasWorldX(rightMarker, 25.6f);
        var clueAnchorPhase0 = all.SingleOrDefault(go => GetPath(go) ==
            "CameraAndStageMarkers/ClueBoardAnchor_Phase0_TUNE");
        var clueAnchorPhase1 = all.SingleOrDefault(go => GetPath(go) ==
            "CameraAndStageMarkers/ClueBoardAnchor_Phase1");
        var clueAnchorsValid = HasWorldX(clueAnchorPhase0, -19.2f) &&
            HasWorldX(clueAnchorPhase1, -19.2f) &&
            Vector3.Distance(clueAnchorPhase0.transform.position,
                clueAnchorPhase1.transform.position) <= 0.0001f;
        var clueHitArea = all.SingleOrDefault(go => GetPath(go) ==
            "Shared/ClueBoard_Group/ClueBoard_HitArea");
        var clueHitBinding = clueHitArea == null
            ? null
            : clueHitArea.GetComponent<EatWhat.Cooking.ShortCycle.ShortCycleMouseActionBinding>();
        var clueHitGate = clueHitArea == null
            ? null
            : clueHitArea.GetComponent<EatWhat.Cooking.ShortCycle.ShortCycleStateGatedInteraction>();
        var clueHitAreaValid = clueHitArea != null &&
            clueHitArea.GetComponent<BoxCollider2D>() != null &&
            clueHitArea.GetComponent<Button_MouseInteract>() != null &&
            clueHitBinding != null &&
            clueHitBinding.ActionName == EatWhat.Cooking.ShortCycle.ShortCycleActionNames.OpenClueBoard &&
            clueHitGate != null &&
            clueHitArea.GetComponentsInChildren<Renderer>(true).Length == 0 &&
            clueHitArea.GetComponentsInChildren<Graphic>(true).Length == 0;
        var legacyOpenButton = all.SingleOrDefault(go => GetPath(go) ==
            "Spaces/Phase0_RecipeBook/Browse_Group/LeftPage/OpenClueBoard_Button_AIGC_Unused");
        var legacyOpenButtonRetired = legacyOpenButton != null &&
            legacyOpenButton.GetComponent<EatWhat.Cooking.ShortCycle.ShortCycleMouseActionBinding>() == null &&
            legacyOpenButton.GetComponent<EatWhat.Cooking.ShortCycle.ShortCycleStateGatedInteraction>() == null;
        var legacyGateCount = all.Sum(go =>
            go.GetComponents<EatWhat.Cooking.ShortCycle.ShortCycleActionButtonStateGate>().Length +
            go.GetComponents<EatWhat.Cooking.ShortCycle.ShortCycleOverflowVisibilityGate>().Length +
            go.GetComponents<EatWhat.Cooking.ShortCycle.ShortCycleScenePresenter>().Length);
        var stateGateCount = all.Sum(go =>
            go.GetComponents<EatWhat.Cooking.ShortCycle.ShortCycleStateGatedInteraction>().Length);
        var presentationSets = all
            .SelectMany(go => go.GetComponents<EatWhat.Cooking.ShortCycle.ShortCyclePresentationSet>())
            .Where(component => component != null)
            .ToArray();
        var presentationSetCount = presentationSets.Length;
        var duplicatePresentationSetPaths = presentationSets
            .GroupBy(component => GetPath(component.gameObject), StringComparer.Ordinal)
            .Where(group => group.Count() != 1)
            .Select(group => group.Key)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var presentationSetContainerMissingPaths = presentationSets
            .Where(component => !IsConnected(component, "containerRoot"))
            .Select(component => GetPath(component.gameObject))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var presentationSetsValid = targetSetsValid &&
            presentationSetCount >= targetSetNames.Length &&
            duplicatePresentationSetPaths.Length == 0 &&
            presentationSetContainerMissingPaths.Length == 0;
        var persistentPresentationSetPaths = presentationSets
            .Where(component => GetBool(component, "keepVisualActiveAcrossSpaces"))
            .Select(component => GetPath(component.gameObject))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var clueBoardPersistentVisual = clueBoard != null &&
            clueBoard.GetComponents<EatWhat.Cooking.ShortCycle.ShortCyclePresentationSet>()
                .Any(component => GetBool(component, "keepVisualActiveAcrossSpaces"));
        var viewportAnchors = all
            .SelectMany(go => go.GetComponents<CameraViewportAnchor>())
            .Where(component => component != null)
            .Distinct()
            .ToArray();
        var invalidViewportAnchorPaths = viewportAnchors
            .Where(anchor => !IsConnected(anchor, "targetCamera") ||
                !IsConnected(anchor, "anchoredRoot") || !HasValidViewportCoordinates(anchor))
            .Select(anchor => GetPath(anchor.gameObject))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var tmpOverflowRendererPaths = all
            .SelectMany(go => go.GetComponents<EatWhat.Cooking.ShortCycle.ShortCyclePresentationSet>())
            .SelectMany(GetTmpOverflowRendererPaths)
            .Distinct()
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var tmpOverflowValid = tmpOverflowRendererPaths.Length == 0;
        var spaceCount = all.Sum(go => go.GetComponents<EatWhat.Cooking.ShortCycle.ShortCycleSpace>().Length);
        var localizedTextCount = all.Sum(go =>
            go.GetComponents<EatWhat.Tools.Localization.LocalizedText>().Length);
        var hostCount = all.SelectMany(go => go.GetComponents<MonoBehaviour>())
            .Count(component => component is EatWhat.Cooking.ShortCycle.IShortCyclePresentationHost);
        var fridgeBinder = all
            .Select(go => go.GetComponent<EatWhat.Cooking.ShortCycle.ShortCycleFridgeBinder>())
            .FirstOrDefault(component => component != null);
        var generatedCatalog = AssetDatabase.LoadAssetAtPath<CK01GeneratedDataCatalog>(
            "Assets/Generated/DataTables/CK01GeneratedDataCatalog.asset");
        CK01FridgeCapacityLevelData demoCapacityLevel = null;
        string capacityDiagnostic = null;
        var demoCapacityResolved = generatedCatalog != null &&
            generatedCatalog.TryGetDemoFridgeCapacity(out demoCapacityLevel, out capacityDiagnostic);
        var demoCapacity = demoCapacityResolved ? demoCapacityLevel.capacity : -1;
        var fridgeSlotCount = GetArraySize(fridgeBinder, "slots");
        var fridgeSlotConnectedCount = GetConnectedArrayCount(fridgeBinder, "slots");
        var fridgeSlotUniqueCount = GetUniqueArrayReferenceCount(fridgeBinder, "slots");
        var fridgeCapacityValid = demoCapacityResolved && demoCapacity > 0 &&
            IsConnected(fridgeBinder, "fridgeCatRig") &&
            fridgeSlotCount >= demoCapacity &&
            fridgeSlotConnectedCount >= demoCapacity &&
            fridgeSlotUniqueCount >= demoCapacity;
        var directorWiringValid = director != null &&
            GetArraySize(director, "spaces") == 3 &&
            GetArraySize(director, "travelingElements") == 1 &&
            GetArraySize(director, "modals") == 3 &&
            IsConnected(director, "sharedSpace") &&
            IsConnected(director, "cameraPanController") &&
            IsConnected(director, "interactionLayerStack") &&
            IsConnected(director, "trayPresentation");
        var sessionWiringValid = session != null &&
            IsConnected(session, "presentationDirector") &&
            IsConnected(session, "dataCatalog") &&
            IsConnected(session, "fridgeBinder") &&
            IsConnected(session, "clueBoardBinder");
        var layerTakeoverValid = layerStack != null && !IsConnected(layerStack, "sessionManager");
        var takeoverValid = rootOrderValid && targetSetsValid && clueParentValid &&
            legacyGateCount == 0 && stateGateCount == 13 && presentationSetsValid &&
            spaceCount == 4 && localizedTextCount >= 19 && hostCount == 1 &&
            directorWiringValid && sessionWiringValid && layerTakeoverValid &&
            heavyTmpRendererEnabledValid && tmpOverflowValid && fridgeCapacityValid &&
            fourMarkerPositionsValid && clueAnchorsValid && clueHitAreaValid &&
            legacyOpenButtonRetired;

        return "CK01-C-LAYOUT-AUDIT" +
            " scene=" + scene.path +
            " roots=" + string.Join(",", rootOrder) +
            " rootOrderValid=" + rootOrderValid +
            " forbidden=" + forbidden.Length +
            " forbiddenPaths=" + string.Join("|", forbidden) +
            " spacesUGUI=" + uguiCount +
            " missingScripts=" + missingScripts +
            " enabledRenderers=" + enabledRenderers +
            " enabledTMP=" + enabledTmp +
            " spacesTMP=" + spacesTmp.Length +
            " spacesStaticFont=" + ShortCycleStaticFontPath +
            " spacesStaticFontMismatches=" + spacesStaticFontMismatchPaths.Length +
            " spacesStaticFontMismatchPaths=" + string.Join("|", spacesStaticFontMismatchPaths) +
            " spacesStaticFontValid=" + spacesStaticFontValid +
            " heavyTMP=" + heavyTmp.Length +
            " heavyTMPRenderersEnabled=" + heavyTmpRendererEnabled +
            " heavyTMPRendererMissing=" + heavyTmpRendererMissing +
            " heavyTMPRendererDisabled=" + heavyTmpRendererDisabledPaths.Length +
            " heavyTMPRendererDisabledPaths=" + string.Join("|", heavyTmpRendererDisabledPaths) +
            " heavyTMPRendererEnabledValid=" + heavyTmpRendererEnabledValid +
            " tmpOverflowRenderers=" + tmpOverflowRendererPaths.Length +
            " tmpOverflowRendererPaths=" + string.Join("|", tmpOverflowRendererPaths) +
            " tmpOverflowValid=" + tmpOverflowValid +
            " kTakeoverValid=" + takeoverValid +
            " kDirectorWiring=" + directorWiringValid +
            " kSessionWiring=" + sessionWiringValid +
            " kLayerTakeover=" + layerTakeoverValid +
            " kHosts=" + hostCount +
            " kSpaces=" + spaceCount +
            " kSets=" + presentationSetCount +
            " kSetDuplicates=" + duplicatePresentationSetPaths.Length +
            " kSetDuplicatePaths=" + string.Join("|", duplicatePresentationSetPaths) +
            " kSetContainerMissing=" + presentationSetContainerMissingPaths.Length +
            " kSetContainerMissingPaths=" + string.Join("|", presentationSetContainerMissingPaths) +
            " kSetsValid=" + presentationSetsValid +
            " fixT5PersistentSets=" + persistentPresentationSetPaths.Length +
            " fixT5PersistentSetPaths=" + string.Join("|", persistentPresentationSetPaths) +
            " fixT5CluePersistentVisual=" + clueBoardPersistentVisual +
            " fixT5ViewportAnchors=" + viewportAnchors.Length +
            " fixT5ViewportAnchorInvalid=" + invalidViewportAnchorPaths.Length +
            " fixT5ViewportAnchorInvalidPaths=" + string.Join("|", invalidViewportAnchorPaths) +
            " kTargetSets=" + string.Join("|", targetSetCounts) +
            " kStateGates=" + stateGateCount +
            " kLegacyG26=" + legacyGateCount +
            " kLocalizedText=" + localizedTextCount +
            " kDemoFridgeCapacity=" + demoCapacity +
            " kFridgeCapacityDiagnostic=" + (capacityDiagnostic ?? string.Empty) +
            " kFridgeSlots=" + fridgeSlotCount +
            " kFridgeSlotsConnected=" + fridgeSlotConnectedCount +
            " kFridgeSlotsUnique=" + fridgeSlotUniqueCount +
            " kFridgeCapacityValid=" + fridgeCapacityValid +
            " kClueParentShared=" + clueParentValid +
            " fixT5FourMarkers=" + fourMarkerPositionsValid +
            " fixT5MarkerX=" + MarkerX(phase1Marker) + "," + MarkerX(phase0LeftMarker) +
                "," + MarkerX(phase0CenterMarker) + "," + MarkerX(rightMarker) +
            " fixT5ClueAnchors=" + clueAnchorsValid +
            " fixT5ClueAnchorX=" + MarkerX(clueAnchorPhase0) + "," + MarkerX(clueAnchorPhase1) +
            " fixT5ClueHitArea=" + clueHitAreaValid +
            " fixT5LegacyOpenButtonRetired=" + legacyOpenButtonRetired +
            " sceneDirty=" + scene.isDirty;
    }

    private static IEnumerable<string> GetTmpOverflowRendererPaths(
        EatWhat.Cooking.ShortCycle.ShortCyclePresentationSet presentationSet)
    {
        if (presentationSet == null) yield break;

        var property = new SerializedObject(presentationSet).FindProperty("overflowRenderers");
        if (property == null || !property.isArray) yield break;

        for (var index = 0; index < property.arraySize; index++)
        {
            var renderer = property.GetArrayElementAtIndex(index).objectReferenceValue as Renderer;
            if (renderer == null) continue;
            if (renderer.GetComponent<TMP_Text>() == null && renderer.GetComponent<TMP_SubMesh>() == null)
                continue;

            yield return GetPath(presentationSet.gameObject) + "->" + GetPath(renderer.gameObject);
        }
    }

    private static int GetArraySize(UnityEngine.Object target, string propertyName)
    {
        if (target == null) return -1;
        var property = new SerializedObject(target).FindProperty(propertyName);
        return property != null && property.isArray ? property.arraySize : -1;
    }

    private static int GetConnectedArrayCount(UnityEngine.Object target, string propertyName)
    {
        if (target == null) return -1;
        var property = new SerializedObject(target).FindProperty(propertyName);
        if (property == null || !property.isArray) return -1;
        var connected = 0;
        for (var index = 0; index < property.arraySize; index++)
        {
            if (property.GetArrayElementAtIndex(index).objectReferenceValue != null) connected++;
        }
        return connected;
    }

    private static int GetUniqueArrayReferenceCount(UnityEngine.Object target, string propertyName)
    {
        if (target == null) return -1;
        var property = new SerializedObject(target).FindProperty(propertyName);
        if (property == null || !property.isArray) return -1;
        var references = new HashSet<int>();
        for (var index = 0; index < property.arraySize; index++)
        {
            var reference = property.GetArrayElementAtIndex(index).objectReferenceValue;
            if (reference != null) references.Add(reference.GetInstanceID());
        }
        return references.Count;
    }

    private static bool IsConnected(UnityEngine.Object target, string propertyName)
    {
        if (target == null) return false;
        var property = new SerializedObject(target).FindProperty(propertyName);
        return property != null && property.propertyType == SerializedPropertyType.ObjectReference &&
            property.objectReferenceValue != null;
    }

    private static bool GetBool(UnityEngine.Object target, string propertyName)
    {
        if (target == null) return false;
        var property = new SerializedObject(target).FindProperty(propertyName);
        return property != null && property.propertyType == SerializedPropertyType.Boolean &&
            property.boolValue;
    }

    private static bool HasValidViewportCoordinates(CameraViewportAnchor anchor)
    {
        if (anchor == null) return false;
        var serialized = new SerializedObject(anchor);
        var viewport = serialized.FindProperty("viewportAnchor");
        var inset = serialized.FindProperty("inwardViewportInset");
        if (viewport == null || inset == null) return false;
        var viewportValue = viewport.vector2Value;
        var insetValue = inset.vector2Value;
        return viewportValue.x >= 0f && viewportValue.x <= 1f &&
            viewportValue.y >= 0f && viewportValue.y <= 1f &&
            insetValue.x >= 0f && insetValue.x <= 1f &&
            insetValue.y >= 0f && insetValue.y <= 1f;
    }

    private static bool HasWorldX(GameObject target, float expected)
    {
        return target != null && Mathf.Abs(target.transform.position.x - expected) <= 0.0001f;
    }

    private static string MarkerX(GameObject target)
    {
        return target == null ? "missing" : target.transform.position.x.ToString("F4");
    }

    private static string GetPath(GameObject gameObject)
    {
        var names = new List<string>();
        for (var current = gameObject.transform; current != null; current = current.parent)
        {
            names.Add(current.name);
        }
        names.Reverse();
        return string.Join("/", names);
    }
}
