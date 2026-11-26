using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EatWhat.EditorTools.DataTables
{
    [Serializable]
    public sealed class SceneToolAreaRecord
    {
        public string areaId;
        public string objectKey;
        public string hierarchyPath;
        public bool active;
    }

    [Serializable]
    public sealed class SceneToolManifest
    {
        public int formatVersion = 1;
        public string projectGuid;
        public string sceneGuid;
        public string scenePath;
        public string sceneSha256;
        public string generatedUtc;
        public List<SceneToolAreaRecord> areas = new List<SceneToolAreaRecord>();
        public List<SceneToolBinding> tools = new List<SceneToolBinding>();
        public List<string> issues = new List<string>();
    }

    /// <summary>Editor-only snapshot of saved scene instances; does not write scenes or prefabs.</summary>
    public static class SceneToolInventoryExporter
    {
        private const string CatalogPath = "Assets/Generated/DataTables/CK01GeneratedDataCatalog.asset";

        [MenuItem("Tools/Data Tables/Export Active Scene Tool Inventory")]
        public static void ExportActiveScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || string.IsNullOrEmpty(scene.path) || scene.isDirty)
            {
                Debug.LogError("Save the active scene before exporting tool inventory.");
                return;
            }
            var output = EditorUtility.SaveFilePanel(
                "Export Scene Tool Inventory", Directory.GetParent(Application.dataPath).FullName,
                "scene-tool-inventory", "json");
            if (string.IsNullOrEmpty(output)) return;
            var manifest = Capture(scene);
            File.WriteAllText(output, JsonUtility.ToJson(manifest, true));
            Debug.Log("Scene tool inventory exported: " + output + " (issues: " + manifest.issues.Count + ").");
        }

        public static SceneToolManifest Capture(Scene scene)
        {
            if (!scene.IsValid() || string.IsNullOrEmpty(scene.path) || scene.isDirty)
                throw new InvalidOperationException("Scene must be saved and clean.");
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var sceneFile = Path.GetFullPath(Path.Combine(projectRoot, scene.path));
            var projectSettings = File.ReadAllText(Path.Combine(projectRoot, "ProjectSettings/ProjectSettings.asset"));
            var guidMatch = Regex.Match(projectSettings, @"(?m)^\s*productGUID:\s*([0-9a-fA-F]{32})\s*$");
            if (!guidMatch.Success) throw new InvalidOperationException("Project productGUID is missing.");
            var manifest = new SceneToolManifest {
                projectGuid = guidMatch.Groups[1].Value.ToLowerInvariant(),
                sceneGuid = AssetDatabase.AssetPathToGUID(scene.path),
                scenePath = scene.path.Replace('\\', '/'),
                sceneSha256 = Sha256(sceneFile),
                generatedUtc = DateTime.UtcNow.ToString("o")
            };
            var catalog = AssetDatabase.LoadAssetAtPath<CK01GeneratedDataCatalog>(CatalogPath);
            var roomByObject = new Dictionary<CookingRoomManager, string>();
            var seenAreas = new HashSet<string>(StringComparer.Ordinal);
            foreach (var root in scene.GetRootGameObjects())
            foreach (var room in root.GetComponentsInChildren<CookingRoomManager>(true))
            {
                var objectKey = GlobalObjectId.GetGlobalObjectIdSlow(room).ToString();
                roomByObject.Add(room, objectKey);
                manifest.areas.Add(new SceneToolAreaRecord {
                    areaId = room.sceneAreaId,
                    objectKey = objectKey,
                    hierarchyPath = HierarchyPath(room.transform),
                    active = room.isActiveAndEnabled
                });
                if (string.IsNullOrEmpty(room.sceneAreaId) || !seenAreas.Add(room.sceneAreaId))
                    manifest.issues.Add("Missing or duplicate sceneAreaId at " + objectKey);
            }
            foreach (var root in scene.GetRootGameObjects())
            foreach (var oldTool in root.GetComponentsInChildren<Tool_Cooking>(true))
            {
                if (oldTool.GetComponent<SceneToolIdentity>() == null)
                    manifest.issues.Add("Legacy Tool_Cooking has no SceneToolIdentity: " +
                        GlobalObjectId.GetGlobalObjectIdSlow(oldTool));
            }
            foreach (var root in scene.GetRootGameObjects())
            foreach (var tool in root.GetComponentsInChildren<SceneToolIdentity>(true))
            {
                var owner = tool.areaOwner;
                var ancestor = tool.transform;
                CookingRoomManager hierarchyOwner = null;
                while (ancestor != null && hierarchyOwner == null)
                {
                    hierarchyOwner = ancestor.GetComponent<CookingRoomManager>();
                    ancestor = ancestor.parent;
                }
                CK01ToolData mapped = null;
                string diagnostic = null;
                if (catalog != null && !string.IsNullOrEmpty(tool.toolId))
                    catalog.TryGetTool(tool.toolId, out mapped, out diagnostic);
                string ownerKey, hierarchyKey;
                if (owner == null || !roomByObject.TryGetValue(owner, out ownerKey)) ownerKey = string.Empty;
                if (hierarchyOwner == null || !roomByObject.TryGetValue(hierarchyOwner, out hierarchyKey))
                    hierarchyKey = string.Empty;
                manifest.tools.Add(new SceneToolBinding {
                    objectKey = GlobalObjectId.GetGlobalObjectIdSlow(tool).ToString(),
                    ownerObjectKey = ownerKey,
                    hierarchyOwnerObjectKey = hierarchyKey,
                    areaId = owner == null ? string.Empty : owner.sceneAreaId,
                    hierarchyAreaId = hierarchyOwner == null ? string.Empty : hierarchyOwner.sceneAreaId,
                    toolId = tool.toolId,
                    toolKind = mapped == null ? string.Empty : mapped.tool_kind,
                    active = tool.isActiveAndEnabled,
                    sceneInstance = !EditorUtility.IsPersistent(tool),
                    mappingSource = SceneToolInventory.ExplicitMapping
                });
            }
            manifest.issues.AddRange(SceneToolInventory.Resolve(manifest.tools).issues);
            return manifest;
        }

        private static string HierarchyPath(Transform transform)
        {
            var parts = new List<string>();
            while (transform != null)
            {
                parts.Insert(0, transform.name);
                transform = transform.parent;
            }
            return string.Join("/", parts.ToArray()); // Diagnostic only; never used as identity.
        }

        private static string Sha256(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }
    }
}
