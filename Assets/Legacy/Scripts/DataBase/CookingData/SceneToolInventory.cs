using System;
using System.Collections.Generic;

/// <summary>Shared scene-tool identity rules. Neither names nor preset_tools confer availability.</summary>
[Serializable]
public sealed class SceneToolBinding
{
    public string objectKey;
    public string ownerObjectKey;
    public string hierarchyOwnerObjectKey;
    public string areaId;
    public string hierarchyAreaId;
    public string toolId;
    public string toolKind;
    public bool active;
    public bool sceneInstance;
    public string mappingSource;
}

public sealed class SceneToolInventoryResult
{
    public readonly Dictionary<string, HashSet<string>> toolsByArea =
        new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
    public readonly List<string> issues = new List<string>();

    public bool HasTool(string areaId, string toolId)
    {
        HashSet<string> tools;
        return toolsByArea.TryGetValue(areaId, out tools) && tools.Contains(toolId);
    }
}

public static class SceneToolInventory
{
    public const string ExplicitMapping = "SceneToolIdentity.toolId -> CK01 Tools.tool_id";

    public static SceneToolInventoryResult Resolve(IEnumerable<SceneToolBinding> bindings)
    {
        var result = new SceneToolInventoryResult();
        var seenObjects = new HashSet<string>(StringComparer.Ordinal);
        if (bindings == null) return result;
        foreach (var binding in bindings)
        {
            if (binding == null) { result.issues.Add("Null tool binding."); continue; }
            if (!binding.sceneInstance) continue; // Prefab assets are not scene inventory.
            var key = binding.objectKey ?? string.Empty;
            if (key.Length == 0 || !seenObjects.Add(key))
            {
                result.issues.Add("Missing or duplicate scene object key: '" + key + "'.");
                continue;
            }
            if (string.IsNullOrEmpty(binding.areaId) || string.IsNullOrEmpty(binding.hierarchyAreaId) ||
                string.IsNullOrEmpty(binding.ownerObjectKey) ||
                !string.Equals(binding.areaId, binding.hierarchyAreaId, StringComparison.Ordinal) ||
                !string.Equals(binding.ownerObjectKey, binding.hierarchyOwnerObjectKey, StringComparison.Ordinal))
            {
                result.issues.Add("Tool '" + key + "' has missing or conflicting explicit/hierarchy area ownership.");
                continue;
            }
            if (string.IsNullOrEmpty(binding.toolId) || string.IsNullOrEmpty(binding.toolKind) ||
                !string.Equals(binding.mappingSource, ExplicitMapping, StringComparison.Ordinal))
            {
                result.issues.Add("Tool '" + key + "' has no verified CK01 tool ID/type mapping.");
                continue;
            }
            if (!binding.active) continue;
            HashSet<string> tools;
            if (!result.toolsByArea.TryGetValue(binding.areaId, out tools))
            {
                tools = new HashSet<string>(StringComparer.Ordinal);
                result.toolsByArea.Add(binding.areaId, tools);
            }
            tools.Add(binding.toolId); // Multiple physical instances grant one tool type.
        }
        return result;
    }
}
