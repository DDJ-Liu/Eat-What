using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
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
            heavyTmpRendererEnabledValid && tmpOverflowValid && fridgeCapacityValid;

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
