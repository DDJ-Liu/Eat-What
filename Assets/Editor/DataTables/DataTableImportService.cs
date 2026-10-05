using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using EatWhat.Localization;

namespace EatWhat.DataTables
{
    /// <summary>
    /// Generic, schema-driven ScriptableObject importer. CK01ImportRegistry owns
    /// the auditable table/aggregate contracts; no per-table importer exists.
    /// </summary>
    public static class DataTableImportService
    {
        private sealed class LoadedTable
        {
            public string SchemaPath;
            public DataTableSchema Schema;
            public DataTableCsvParseResult ParseResult;
            public DataTableValidationResult Validation;
            public Type TargetType;
        }

        private sealed class ImportPlanItem
        {
            public LoadedTable Loaded;
            public DataTableCsvRow Row;
            public string AssetPath;
            public ScriptableObject ExistingAsset;
            public bool IsAggregateAsset;
            public bool WasRecovered;
        }

        [Serializable]
        public sealed class DataTableSchemaImportReport
        {
            public string schemaPath;
            public List<string> errors = new List<string>();
            public List<string> warnings = new List<string>();
        }

        [Serializable]
        public sealed class DataTableImportReport
        {
            public bool success;
            public string dataCatalogAsset = CK01ImportRegistry.DataCatalogAssetPath;
            public List<string> errors = new List<string>();
            public List<string> warnings = new List<string>();
            public List<DataTableSchemaImportReport> schemas = new List<DataTableSchemaImportReport>();
            public List<string> createdAssets = new List<string>();
            public List<string> updatedAssets = new List<string>();
            public List<string> unchangedAssets = new List<string>();
        }

        [Serializable]
        public sealed class DataBatchCompletionCredential
        {
            public int schemaVersion = 1;
            public bool success;
            public string runId;
            public string nonce;
            public string completedAtUtc;
            public bool importCompleted;
            public bool fontBuildCompleted;
            public int createdCount;
            public int updatedCount;
            public int unchangedCount;
            public string[] localizationGlyphs;
            public int localizationCharacterCount;
            public int fontLocalizationCharacterCount;
            public int requiredCharacterCount;
            public int staticCharacterCount;
            public int dynamicCharacterCount;
            public string staticAssetSha256;
            public string dynamicAssetSha256;
            public bool staticAtlasRewritten;
            public DataBatchDependencyProof dependencyProof;
        }

        [Serializable]
        public sealed class DataBatchAssetEvidence
        {
            public string path;
            public string guid;
            public string typeName;
            public long localFileId;
            public DataBatchSubAssetEvidence[] subAssets;
        }

        [Serializable]
        public sealed class DataBatchSubAssetEvidence
        {
            public string name;
            public string guid;
            public string typeName;
            public long localFileId;
            public string textureGuid;
            public long textureLocalFileId;
        }

        [Serializable]
        public sealed class DataBatchFileEvidence
        {
            public string path;
            public long length;
            public string sha256;
        }

        [Serializable]
        public sealed class DataBatchDependencyProof
        {
            public int schemaVersion = 2;
            public string runId;
            public string nonce;
            public string sourceTreeSha256;
            public string inputSnapshotSha256;
            public string generatedRoot = "Assets/Generated/DataTables";
            public DataBatchAssetEvidence[] roots;
            public DataBatchAssetEvidence[] dependencies;
            public DataBatchFileEvidence[] candidateFiles;
        }

        private static string ProjectRoot
        {
            get { return Directory.GetParent(Application.dataPath).FullName; }
        }

        private static string DataTablesRoot
        {
            get { return Path.Combine(ProjectRoot, "DataTables"); }
        }

        private static string SchemaRoot
        {
            get { return Path.Combine(DataTablesRoot, "Schemas"); }
        }

        [MenuItem("Tools/Data Tables/Import All")]
        public static void ImportAllMenu()
        {
            var report = ImportAll();
            if (report.success)
            {
                Debug.Log("DataTables import complete. Created: " + report.createdAssets.Count +
                          ", updated: " + report.updatedAssets.Count +
                          ", unchanged: " + report.unchangedAssets.Count + ".");
            }
            else
            {
                Debug.LogError("DataTables import stopped before writing assets:\n" + string.Join("\n", report.errors.ToArray()));
            }
        }

        /// <summary>
        /// Headless Unity entry point used by the isolated DATA pipeline.
        /// It intentionally reuses the production importer and the reviewed PRE-1
        /// font builder instead of maintaining batch-only import logic.
        /// </summary>
        public static void ImportAllBatch()
        {
            try
            {
                var report = ImportAll();
                if (!report.success)
                {
                    throw new InvalidOperationException(
                        "DataTables validation/import failed:\n" +
                        string.Join("\n", report.errors.ToArray()));
                }

                var fontManifest = RebuildShortCycleFonts();
                var localizationGlyphs = ReadLocalizationGlyphs();
                var fontLocalizationCharacterCount = ReadRequiredIntField(
                    fontManifest,
                    "localizationCharacterCount");
                if (localizationGlyphs.Length != fontLocalizationCharacterCount)
                {
                    throw new InvalidOperationException(
                        "Localization glyph count disagrees with the font build manifest. parser=" +
                        localizationGlyphs.Length + " fontManifest=" + fontLocalizationCharacterCount);
                }

                var runId = ReadRequiredBatchEnvironment("EATWHAT_DATA_RUN_ID");
                var nonce = ReadRequiredBatchEnvironment("EATWHAT_DATA_COMPLETION_NONCE");
                var completionPath = ResolveCompletionCredentialPath(
                    ReadRequiredBatchEnvironment("EATWHAT_DATA_COMPLETION_PATH"));
                var credential = new DataBatchCompletionCredential
                {
                    success = true,
                    runId = runId,
                    nonce = nonce,
                    completedAtUtc = DateTimeOffset.Now.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture),
                    importCompleted = true,
                    fontBuildCompleted = true,
                    createdCount = report.createdAssets.Count,
                    updatedCount = report.updatedAssets.Count,
                    unchangedCount = report.unchangedAssets.Count,
                    localizationGlyphs = localizationGlyphs,
                    localizationCharacterCount = localizationGlyphs.Length,
                    fontLocalizationCharacterCount = fontLocalizationCharacterCount,
                    requiredCharacterCount = ReadRequiredIntField(fontManifest, "requiredCharacterCount"),
                    staticCharacterCount = ReadRequiredIntField(fontManifest, "staticCharacterCount"),
                    dynamicCharacterCount = ReadRequiredIntField(fontManifest, "dynamicCharacterCount"),
                    staticAssetSha256 = ReadRequiredStringField(fontManifest, "staticAssetSha256"),
                    dynamicAssetSha256 = ReadRequiredStringField(fontManifest, "dynamicEmptyAssetSha256"),
                    staticAtlasRewritten = ReadRequiredBoolField(fontManifest, "staticAtlasRewritten"),
                    dependencyProof = BuildBatchDependencyProof(runId, nonce)
                };
                WriteCompletionCredential(completionPath, credential);

                Debug.Log("[DATA] BATCH_IMPORT_OK runId=" + runId +
                          " nonce=" + nonce +
                          " credential=" + completionPath +
                          " created=" + report.createdAssets.Count +
                          " updated=" + report.updatedAssets.Count +
                          " unchanged=" + report.unchangedAssets.Count +
                          " fontEntry=EatWhat.EditorTools.Fonts.ShortCycleFontAtlasBuilder.BuildAll");
            }
            catch (Exception exception)
            {
                var diagnostic = exception is TargetInvocationException invocation &&
                                 invocation.InnerException != null
                    ? invocation.InnerException
                    : exception;
                Debug.LogError("[DATA] BATCH_IMPORT_FAILED\n" + diagnostic);
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }
                throw;
            }
        }

        private static DataBatchDependencyProof BuildBatchDependencyProof(string runId, string nonce)
        {
            const string generatedRoot = "Assets/Generated/DataTables";
            var fullRoot = Path.Combine(ProjectRoot, generatedRoot.Replace('/', Path.DirectorySeparatorChar));
            if (!Directory.Exists(fullRoot))
                throw new InvalidOperationException("Generated DATA root is missing: " + generatedRoot);

            var roots = new List<string>();
            var files = new List<DataBatchFileEvidence>();
            foreach (var file in Directory.GetFiles(fullRoot, "*", SearchOption.AllDirectories))
            {
                var relative = file.Substring(fullRoot.Length + 1).Replace('\\', '/');
                var assetPath = generatedRoot + "/" + relative;
                using (var sha = SHA256.Create())
                using (var stream = File.OpenRead(file))
                {
                    files.Add(new DataBatchFileEvidence {
                        path = relative, length = stream.Length,
                        sha256 = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "")
                    });
                }
                if (relative.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
                    roots.Add(assetPath);
            }
            roots.Sort(StringComparer.Ordinal);
            files.Sort((a, b) => StringComparer.Ordinal.Compare(a.path, b.path));
            if (roots.Count == 0 || !roots.Contains(CK01ImportRegistry.DataCatalogAssetPath))
                throw new InvalidOperationException("Generated DATA roots do not include the catalog.");

            var rootEvidence = new List<DataBatchAssetEvidence>();
            foreach (var root in roots) rootEvidence.Add(RequireBatchAssetEvidence(root, true));
            var dependencies = AssetDatabase.GetDependencies(roots.ToArray(), true);
            if (dependencies == null || dependencies.Length == 0)
                throw new InvalidOperationException("Unity returned an empty generated DATA dependency closure.");
            var dependencyEvidence = new List<DataBatchAssetEvidence>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var dependency in dependencies)
            {
                if (string.IsNullOrEmpty(dependency) || !seen.Add(dependency)) continue;
                dependencyEvidence.Add(RequireBatchAssetEvidence(dependency, false));
            }
            foreach (var root in roots)
                if (!seen.Contains(root))
                    throw new InvalidOperationException("Dependency closure omits generated root: " + root);
            dependencyEvidence.Sort((a, b) => StringComparer.Ordinal.Compare(a.path, b.path));

            return new DataBatchDependencyProof {
                runId = runId, nonce = nonce,
                sourceTreeSha256 = ReadRequiredBatchEnvironment("EATWHAT_DATA_SOURCE_TREE_SHA256"),
                inputSnapshotSha256 = ReadRequiredBatchEnvironment("EATWHAT_DATA_INPUT_SNAPSHOT_SHA256"),
                roots = rootEvidence.ToArray(), dependencies = dependencyEvidence.ToArray(),
                candidateFiles = files.ToArray()
            };
        }

        private static DataBatchAssetEvidence RequireBatchAssetEvidence(string path, bool isRoot)
        {
            if (path.StartsWith("Assets/Scripts/Spine_Package/Avatars/output4/", StringComparison.Ordinal) ||
                path.StartsWith("Assets/Scripts/Spine_Package/Avatars/Sample/", StringComparison.Ordinal))
                throw new InvalidOperationException("Generated DATA dependency reaches a legacy Spine avatar: " + path);
            var type = AssetDatabase.GetMainAssetTypeAtPath(path);
            if (type == null || typeof(Spine.Unity.AnimationReferenceAsset).IsAssignableFrom(type))
                throw new InvalidOperationException("Generated DATA dependency is unresolved or an animation reference: " + path);
            var guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid) || !string.Equals(AssetDatabase.GUIDToAssetPath(guid), path, StringComparison.Ordinal))
                throw new InvalidOperationException("Generated DATA dependency has no stable GUID: " + path);
            if (path.StartsWith("Assets/", StringComparison.Ordinal) &&
                (!File.Exists(Path.Combine(ProjectRoot, path.Replace('/', Path.DirectorySeparatorChar))) ||
                 !File.Exists(Path.Combine(ProjectRoot, path.Replace('/', Path.DirectorySeparatorChar) + ".meta"))))
                throw new InvalidOperationException("Generated DATA dependency file or meta is missing: " + path);

            var evidence = new DataBatchAssetEvidence { path = path, guid = guid, typeName = type.FullName };
            if (!isRoot) return evidence;

            var main = AssetDatabase.LoadMainAssetAtPath(path);
            var all = AssetDatabase.LoadAllAssetsAtPath(path);
            if (main == null || main.GetType() != type || all == null || all.Length == 0)
                throw new InvalidOperationException("Generated DATA root has no unambiguous main asset: " + path);
            var mainCount = 0;
            foreach (var asset in all) if (asset == main) mainCount++;
            if (mainCount != 1)
                throw new InvalidOperationException("Generated DATA root does not list its main object exactly once: " + path);
            string mainGuid;
            long mainFileId;
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(main, out mainGuid, out mainFileId) ||
                !string.Equals(mainGuid, guid, StringComparison.Ordinal) || mainFileId == 0)
                throw new InvalidOperationException("Generated DATA root main object has no stable identity: " + path);
            evidence.localFileId = mainFileId;

            if (string.Equals(path, CK01ImportRegistry.MissingSpriteTextureAssetPath, StringComparison.Ordinal))
            {
                var texture = main as Texture2D;
                if (texture == null || all.Length != 2 ||
                    !string.Equals(type.FullName, typeof(Texture2D).FullName, StringComparison.Ordinal) ||
                    !string.Equals(main.name, "CK01-MissingSpriteTexture", StringComparison.Ordinal))
                    throw new InvalidOperationException("Generated DATA shared texture has an unexpected main asset or subasset count: " + path);
                var sprite = (all[0] == main ? all[1] : all[0]) as Sprite;
                if (sprite == null || sprite.texture != texture ||
                    !string.Equals(sprite.name, "CK01-MissingSprite", StringComparison.Ordinal))
                    throw new InvalidOperationException("Generated DATA shared Sprite is missing or belongs to another texture: " + path);
                string spriteGuid;
                long spriteFileId;
                if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite, out spriteGuid, out spriteFileId) ||
                    !string.Equals(spriteGuid, guid, StringComparison.Ordinal) ||
                    spriteFileId == 0 || spriteFileId == mainFileId)
                    throw new InvalidOperationException("Generated DATA shared Sprite has no stable subasset identity: " + path);
                evidence.subAssets = new[] { new DataBatchSubAssetEvidence {
                    name = sprite.name, guid = spriteGuid, typeName = sprite.GetType().FullName,
                    localFileId = spriteFileId, textureGuid = mainGuid, textureLocalFileId = mainFileId
                } };
            }
            else
            {
                if (!typeof(ScriptableObject).IsAssignableFrom(type) || all.Length != 1)
                    throw new InvalidOperationException("Generated DATA root is not a single ScriptableObject: " + path);
                evidence.subAssets = new DataBatchSubAssetEvidence[0];
            }

            // GetDependencies omits broken serialized references. Inspect both the main
            // object and every permitted subasset; never deserialize old Spine data.
            foreach (var asset in all)
            {
                if (asset == null || typeof(Spine.Unity.AnimationReferenceAsset).IsAssignableFrom(asset.GetType()) ||
                    typeof(Spine.Unity.SkeletonDataAsset).IsAssignableFrom(asset.GetType()))
                    throw new InvalidOperationException("Generated DATA root contains an unresolved or Spine object: " + path);
                var property = new SerializedObject(asset).GetIterator();
                while (property.Next(true))
                {
                    if (property.propertyType == SerializedPropertyType.ObjectReference &&
                        property.objectReferenceValue == null && property.objectReferenceInstanceIDValue != 0)
                        throw new InvalidOperationException("Generated root has an unresolved reference: " + path + " " + property.propertyPath);
                }
            }
            return evidence;
        }

        private static object RebuildShortCycleFonts()
        {
            const string builderTypeName =
                "EatWhat.EditorTools.Fonts.ShortCycleFontAtlasBuilder";
            Type builderType = null;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                builderType = assembly.GetType(builderTypeName, false);
                if (builderType != null) break;
            }

            if (builderType == null)
            {
                throw new InvalidOperationException(
                    "PRE-1 font builder type is unavailable: " + builderTypeName);
            }

            var buildMethod = builderType.GetMethod(
                "BuildAll",
                BindingFlags.Public | BindingFlags.Static,
                null,
                Type.EmptyTypes,
                null);
            if (buildMethod == null)
            {
                throw new MissingMethodException(builderTypeName, "BuildAll");
            }
            var manifest = buildMethod.Invoke(null, null);
            if (manifest == null)
            {
                throw new InvalidOperationException(
                    "PRE-1 font builder returned no build manifest: " + builderTypeName);
            }
            return manifest;
        }

        private static string[] ReadLocalizationGlyphs()
        {
            var csvPath = Path.Combine(
                DataTablesRoot,
                "Localization",
                "Localization.csv");
            if (!File.Exists(csvPath))
            {
                throw new FileNotFoundException("Localization CSV is missing.", csvPath);
            }

            var parsed = DataTableCsvParser.Parse(ReadStrictUtf8Csv(csvPath));
            if (!parsed.IsValid)
            {
                throw new InvalidOperationException(
                    "Localization CSV parse failed: " + string.Join(" | ", parsed.Errors.ToArray()));
            }

            var valueIndex = parsed.Table.GetHeaderIndex("zh_cn");
            if (valueIndex < 0)
            {
                throw new InvalidOperationException("Localization CSV has no zh_cn column.");
            }

            var scalars = new HashSet<uint>();
            foreach (var row in parsed.Table.Rows)
            {
                if (row.Values.Count <= valueIndex) continue;
                AddUnicodeScalars(row.Values[valueIndex], scalars);
            }

            var ordered = new List<uint>(scalars);
            ordered.Sort();
            var glyphs = new string[ordered.Count];
            for (var index = 0; index < ordered.Count; index++)
            {
                glyphs[index] = "U+" + ordered[index].ToString("X4", CultureInfo.InvariantCulture);
            }
            return glyphs;
        }

        private static void AddUnicodeScalars(string value, ISet<uint> destination)
        {
            if (string.IsNullOrEmpty(value)) return;
            for (var index = 0; index < value.Length; index++)
            {
                var character = value[index];
                if (char.IsHighSurrogate(character) && index + 1 < value.Length &&
                    char.IsLowSurrogate(value[index + 1]))
                {
                    destination.Add((uint)char.ConvertToUtf32(character, value[++index]));
                }
                else if (!char.IsSurrogate(character))
                {
                    destination.Add(character);
                }
            }
        }

        private static string ReadRequiredBatchEnvironment(string name)
        {
            var value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException("Required DATA batch environment value is missing: " + name);
            }
            return value.Trim();
        }

        private static string ResolveCompletionCredentialPath(string path)
        {
            var projectRoot = Path.GetFullPath(ProjectRoot).TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar);
            var fullPath = Path.GetFullPath(path);
            var prefix = projectRoot + Path.DirectorySeparatorChar;
            if (!fullPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "DATA completion credential path must be inside the isolated project. path=" +
                    fullPath + " project=" + projectRoot);
            }
            return fullPath;
        }

        private static void WriteCompletionCredential(
            string path,
            DataBatchCompletionCredential credential)
        {
            var directory = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(directory))
            {
                throw new InvalidOperationException("DATA completion credential has no parent directory: " + path);
            }
            Directory.CreateDirectory(directory);
            var json = JsonUtility.ToJson(credential, true) + Environment.NewLine;
            var bytes = new UTF8Encoding(false).GetBytes(json);
            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }
        }

        private static FieldInfo ReadRequiredField(object target, string name)
        {
            var field = target.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance);
            if (field == null)
            {
                throw new MissingFieldException(target.GetType().FullName, name);
            }
            return field;
        }

        private static int ReadRequiredIntField(object target, string name)
        {
            return (int)ReadRequiredField(target, name).GetValue(target);
        }

        private static bool ReadRequiredBoolField(object target, string name)
        {
            return (bool)ReadRequiredField(target, name).GetValue(target);
        }

        private static string ReadRequiredStringField(object target, string name)
        {
            var value = ReadRequiredField(target, name).GetValue(target) as string;
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(
                    "Font build manifest field is empty: " + name);
            }
            return value;
        }

        /// <summary>Runs all CSV/schema and target-type checks without changing assets.</summary>
        public static DataTableImportReport ValidateAll()
        {
            List<LoadedTable> ignored;
            return BuildValidationReport(out ignored);
        }

        /// <summary>
        /// Validates every table first, then creates or updates only changed assets.
        /// There is deliberately no delete pass.
        /// </summary>
        public static DataTableImportReport ImportAll()
        {
            List<LoadedTable> tables;
            var report = BuildValidationReport(out tables);
            if (!report.success)
            {
                return report;
            }

            var plan = BuildImportPlan(tables, report);
            var dataCatalog = AssetDatabase.LoadAssetAtPath<CK01GeneratedDataCatalog>(CK01ImportRegistry.DataCatalogAssetPath);
            var dataCatalogMainAsset = AssetDatabase.LoadMainAssetAtPath(CK01ImportRegistry.DataCatalogAssetPath);
            if (dataCatalogMainAsset != null && dataCatalog == null)
            {
                report.errors.Add(CK01ImportRegistry.DataCatalogAssetPath +
                    " already exists with type '" + dataCatalogMainAsset.GetType().Name +
                    "', expected 'CK01GeneratedDataCatalog'.");
            }
            else if (dataCatalogMainAsset == null && File.Exists(Path.Combine(
                ProjectRoot,
                CK01ImportRegistry.DataCatalogAssetPath.Replace('/', Path.DirectorySeparatorChar))))
            {
                report.errors.Add(CK01ImportRegistry.DataCatalogAssetPath +
                    " exists but is not loadable. Import stopped to preserve its .meta GUID.");
            }
            if (report.errors.Count > 0)
            {
                report.success = false;
                return report;
            }

            foreach (var item in plan)
            {
                string folderError;
                var folderPath = Path.GetDirectoryName(item.AssetPath).Replace('\\', '/');
                if (!TryEnsureAssetFolder(folderPath, out folderError))
                {
                    report.errors.Add(item.AssetPath + ": " + folderError);
                }
            }

            string dataCatalogFolderError;
            var dataCatalogFolder = Path.GetDirectoryName(CK01ImportRegistry.DataCatalogAssetPath).Replace('\\', '/');
            if (!TryEnsureAssetFolder(dataCatalogFolder, out dataCatalogFolderError))
            {
                report.errors.Add(CK01ImportRegistry.DataCatalogAssetPath + ": " + dataCatalogFolderError);
            }

            if (report.errors.Count > 0)
            {
                report.success = false;
                return report;
            }

            RecoverMissingGeneratedAssets(plan, report);
            if (report.errors.Count > 0)
            {
                report.success = false;
                return report;
            }

            var anyDirtyAsset = false;
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var item in plan)
                {
                    var asset = item.ExistingAsset;
                    var isNew = asset == null;
                    if (isNew)
                    {
                        asset = ScriptableObject.CreateInstance(item.Loaded.TargetType);
                        AssetDatabase.CreateAsset(asset, item.AssetPath);
                    }

                    var expectedAssetName = Path.GetFileNameWithoutExtension(item.AssetPath);
                    var nameChanged = !string.Equals(asset.name, expectedAssetName, StringComparison.Ordinal);
                    if (nameChanged)
                    {
                        asset.name = expectedAssetName;
                    }

                    bool changed;
                    string applyError;
                    var applyWarnings = new List<string>();
                    var applied = item.IsAggregateAsset
                        ? TryApplyAggregateAsset(asset, item.Loaded, out changed, out applyError)
                        : TryApplyRow(asset, item.Loaded.Schema, item.Row, item.Loaded.Validation.Table, out changed, out applyError, out applyWarnings);
                    if (item.IsAggregateAsset)
                    {
                        applyWarnings = new List<string>();
                    }
                    if (!applied)
                    {
                        report.errors.Add(item.AssetPath + ": " + applyError);
                        continue;
                    }
                    item.ExistingAsset = asset;
                    changed |= nameChanged;
                    foreach (var warning in applyWarnings)
                    {
                        report.warnings.Add(item.AssetPath + ": " + warning);
                    }

                    if (isNew)
                    {
                        EditorUtility.SetDirty(asset);
                        report.createdAssets.Add(item.AssetPath);
                        anyDirtyAsset = true;
                    }
                    else if (changed)
                    {
                        EditorUtility.SetDirty(asset);
                        report.updatedAssets.Add(item.AssetPath);
                        anyDirtyAsset = true;
                    }
                    else if (!item.WasRecovered)
                    {
                        report.unchangedAssets.Add(item.AssetPath);
                    }
                }

                if (report.errors.Count == 0)
                {
                    List<CK01GeneratedDataTable> catalogTables;
                    string catalogError;
                    if (!TryBuildDataCatalogTables(tables, plan, out catalogTables, out catalogError))
                    {
                        report.errors.Add(CK01ImportRegistry.DataCatalogAssetPath + ": " + catalogError);
                    }
                    else
                    {
                        var isNewCatalog = dataCatalog == null;
                        if (isNewCatalog)
                        {
                            dataCatalog = ScriptableObject.CreateInstance<CK01GeneratedDataCatalog>();
                            AssetDatabase.CreateAsset(dataCatalog, CK01ImportRegistry.DataCatalogAssetPath);
                        }

                        var expectedCatalogName = Path.GetFileNameWithoutExtension(CK01ImportRegistry.DataCatalogAssetPath);
                        var catalogNameChanged = !string.Equals(dataCatalog.name, expectedCatalogName, StringComparison.Ordinal);
                        if (catalogNameChanged)
                        {
                            dataCatalog.name = expectedCatalogName;
                        }

                        var catalogChanged = dataCatalog.ReplaceTables(catalogTables) || catalogNameChanged;
                        if (isNewCatalog)
                        {
                            EditorUtility.SetDirty(dataCatalog);
                            report.createdAssets.Add(CK01ImportRegistry.DataCatalogAssetPath);
                            anyDirtyAsset = true;
                        }
                        else if (catalogChanged)
                        {
                            EditorUtility.SetDirty(dataCatalog);
                            report.updatedAssets.Add(CK01ImportRegistry.DataCatalogAssetPath);
                            anyDirtyAsset = true;
                        }
                        else
                        {
                            report.unchangedAssets.Add(CK01ImportRegistry.DataCatalogAssetPath);
                        }
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            if (report.errors.Count > 0)
            {
                report.success = false;
                return report;
            }

            if (anyDirtyAsset)
            {
                AssetDatabase.SaveAssets();
            }

            report.success = true;
            return report;
        }

        /// <summary>
        /// Recovers a generated asset whose file exists but whose main object cannot be loaded.
        /// This can happen when an old generator serialized an invalid MonoScript reference.
        /// Replacing only the .asset payload preserves the existing .meta file and GUID.
        /// </summary>
        private static void RecoverMissingGeneratedAssets(List<ImportPlanItem> plan, DataTableImportReport report)
        {
            const string repairFolder = "Assets/Generated/DataTables/__ImporterRepairTemp";
            var needsRepair = plan.FindAll(item =>
                item.ExistingAsset == null &&
                File.Exists(Path.Combine(ProjectRoot, item.AssetPath.Replace('/', Path.DirectorySeparatorChar))));
            if (needsRepair.Count == 0)
            {
                return;
            }

            string folderError;
            if (!TryEnsureAssetFolder(repairFolder, out folderError))
            {
                report.errors.Add(folderError);
                return;
            }

            try
            {
                foreach (var item in needsRepair)
                {
                    var temporaryPath = repairFolder + "/" + Guid.NewGuid().ToString("N") + ".asset";
                    ScriptableObject temporaryAsset = null;
                    try
                    {
                        temporaryAsset = ScriptableObject.CreateInstance(item.Loaded.TargetType);
                        temporaryAsset.name = Path.GetFileNameWithoutExtension(item.AssetPath);
                        AssetDatabase.CreateAsset(temporaryAsset, temporaryPath);

                        bool changed;
                        string applyError;
                        var applyWarnings = new List<string>();
                        var applied = item.IsAggregateAsset
                            ? TryApplyAggregateAsset(temporaryAsset, item.Loaded, out changed, out applyError)
                            : TryApplyRow(temporaryAsset, item.Loaded.Schema, item.Row, item.Loaded.Validation.Table, out changed, out applyError, out applyWarnings);
                        if (item.IsAggregateAsset)
                        {
                            applyWarnings = new List<string>();
                        }
                        if (!applied)
                        {
                            report.errors.Add(item.AssetPath + ": recovery payload could not be populated: " + applyError);
                            continue;
                        }
                        foreach (var warning in applyWarnings)
                        {
                            report.warnings.Add(item.AssetPath + ": " + warning);
                        }

                        EditorUtility.SetDirty(temporaryAsset);
                        AssetDatabase.SaveAssets();
                        var temporaryFullPath = Path.Combine(ProjectRoot, temporaryPath.Replace('/', Path.DirectorySeparatorChar));
                        var targetFullPath = Path.Combine(ProjectRoot, item.AssetPath.Replace('/', Path.DirectorySeparatorChar));
                        FileUtil.ReplaceFile(temporaryFullPath, targetFullPath);
                        AssetDatabase.DeleteAsset(temporaryPath);
                        AssetDatabase.ImportAsset(item.AssetPath, ImportAssetOptions.ForceUpdate);

                        item.ExistingAsset = AssetDatabase.LoadAssetAtPath(item.AssetPath, item.Loaded.TargetType) as ScriptableObject;
                        if (item.ExistingAsset == null)
                        {
                            report.errors.Add(item.AssetPath + ": recovery replacement did not produce a loadable " + item.Loaded.TargetType.Name + ".");
                            continue;
                        }

                        item.WasRecovered = true;
                        report.updatedAssets.Add(item.AssetPath);
                    }
                    catch (Exception exception)
                    {
                        report.errors.Add(item.AssetPath + ": recovery failed: " + exception.Message);
                    }
                    finally
                    {
                        if (AssetDatabase.LoadMainAssetAtPath(temporaryPath) != null)
                        {
                            AssetDatabase.DeleteAsset(temporaryPath);
                        }
                    }
                }
            }
            finally
            {
                if (AssetDatabase.IsValidFolder(repairFolder))
                {
                    AssetDatabase.DeleteAsset(repairFolder);
                }
            }
        }

        private static DataTableImportReport BuildValidationReport(out List<LoadedTable> tables)
        {
            tables = new List<LoadedTable>();
            var report = new DataTableImportReport();
            if (!Directory.Exists(SchemaRoot))
            {
                report.errors.Add("DataTables schema directory does not exist: " + SchemaRoot);
                return report;
            }

            var schemaFiles = Directory.GetFiles(SchemaRoot, "*.schema.json", SearchOption.AllDirectories);
            if (schemaFiles.Length == 0)
            {
                report.errors.Add("DataTables schema directory contains no *.schema.json files: " + SchemaRoot);
                return report;
            }

            foreach (var schemaFile in schemaFiles)
            {
                var loaded = new LoadedTable
                {
                    SchemaPath = ToDataTablesRelativePath(schemaFile),
                    ParseResult = new DataTableCsvParseResult()
                };
                tables.Add(loaded);

                try
                {
                    loaded.Schema = JsonUtility.FromJson<DataTableSchema>(File.ReadAllText(schemaFile, Encoding.UTF8));
                }
                catch (Exception exception)
                {
                    loaded.ParseResult.Errors.Add("Could not parse schema '" + loaded.SchemaPath + "': " + exception.Message);
                    continue;
                }

                string csvFile;
                if (loaded.Schema == null || !TryResolveContainedPath(DataTablesRoot, loaded.Schema.csvPath, out csvFile))
                {
                    loaded.ParseResult.Errors.Add("Schema '" + loaded.SchemaPath + "' has a CSV path outside DataTables.");
                    continue;
                }

                if (!File.Exists(csvFile))
                {
                    loaded.ParseResult.Errors.Add("Schema '" + loaded.SchemaPath + "' references missing CSV '" + loaded.Schema.csvPath + "'.");
                    continue;
                }

                try
                {
                    loaded.ParseResult = DataTableCsvParser.Parse(ReadStrictUtf8Csv(csvFile));
                }
                catch (Exception exception)
                {
                    loaded.ParseResult.Errors.Add("Could not read UTF-8 CSV '" + loaded.Schema.csvPath + "': " + exception.Message);
                }
            }

            var context = BuildValidationContext(tables, report);
            foreach (var loaded in tables)
            {
                loaded.Validation = DataTableValidator.Validate(loaded.Schema, loaded.ParseResult, context);
                var schemaReport = new DataTableSchemaImportReport { schemaPath = loaded.SchemaPath };
                schemaReport.errors.AddRange(loaded.Validation.Errors);
                schemaReport.warnings.AddRange(loaded.Validation.Warnings);
                report.schemas.Add(schemaReport);
                report.errors.AddRange(PrefixMessages(loaded.SchemaPath, loaded.Validation.Errors));
                report.warnings.AddRange(PrefixMessages(loaded.SchemaPath, loaded.Validation.Warnings));
                ValidateTargetContract(loaded, report, schemaReport);
            }

            ValidateCK01Registration(tables, report);

            report.success = report.errors.Count == 0;
            return report;
        }

        private static string ReadStrictUtf8Csv(string csvFile)
        {
            var text = new UTF8Encoding(false, true).GetString(File.ReadAllBytes(csvFile));
            return text.Length > 0 && text[0] == '\uFEFF' ? text.Substring(1) : text;
        }

        private static DataTableValidationContext BuildValidationContext(List<LoadedTable> tables, DataTableImportReport report)
        {
            var context = new DataTableValidationContext();
            foreach (var loaded in tables)
            {
                if (loaded.Schema == null || loaded.Schema.columns == null)
                {
                    continue;
                }

                foreach (var column in loaded.Schema.columns)
                {
                    if (column == null ||
                        (!string.Equals(column.type, "enum", StringComparison.Ordinal) &&
                         !string.Equals(column.type, "list<enum>", StringComparison.Ordinal)) ||
                        (column.enumValues != null && column.enumValues.Count > 0) ||
                        context.EnumValues.ContainsKey(column.enumType))
                    {
                        continue;
                    }

                    var enumType = FindType(column.enumType);
                    if (enumType == null || !enumType.IsEnum)
                    {
                        report.errors.Add(loaded.SchemaPath + ": enum type '" + column.enumType + "' cannot be resolved.");
                        continue;
                    }

                    context.EnumValues.Add(column.enumType, new HashSet<string>(Enum.GetNames(enumType), StringComparer.Ordinal));
                }
            }

            foreach (var source in tables)
            {
                if (source.Schema == null || source.Schema.references == null)
                {
                    continue;
                }

                foreach (var reference in source.Schema.references)
                {
                    if (reference == null)
                    {
                        continue;
                    }

                    var target = FindTable(tables, reference.targetSchema);
                    if (target == null || target.ParseResult == null || target.ParseResult.Table == null)
                    {
                        continue;
                    }

                    var targetColumn = DataTableValidator.FindColumn(target.Schema, reference.targetField);
                    if (targetColumn == null)
                    {
                        continue;
                    }

                    var key = DataTableValidator.ReferenceKey(reference.targetSchema, reference.targetField);
                    HashSet<string> values;
                    if (!context.ReferenceValues.TryGetValue(key, out values))
                    {
                        values = new HashSet<string>(StringComparer.Ordinal);
                        context.ReferenceValues.Add(key, values);
                    }

                    foreach (var row in target.ParseResult.Table.Rows)
                    {
                        var value = DataTableValidator.GetEffectiveValue(targetColumn, row, target.ParseResult.Table);
                        if (!string.IsNullOrEmpty(value))
                        {
                            values.Add(value);
                        }
                    }
                }
            }

            return context;
        }

        private static void ValidateTargetContract(
            LoadedTable loaded,
            DataTableImportReport report,
            DataTableSchemaImportReport schemaReport)
        {
            if (loaded.Schema == null)
            {
                return;
            }

            var registration = CK01ImportRegistry.Find(loaded.Schema.tableName);
            if (registration == null)
            {
                AddContractError(loaded.SchemaPath, "table is not registered by CK01ImportRegistry.", report, schemaReport);
                return;
            }
            if (!string.Equals(loaded.Schema.outputDirectory, registration.OutputDirectory, StringComparison.Ordinal))
            {
                AddContractError(loaded.SchemaPath, "outputDirectory differs from its CK01 registration.", report, schemaReport);
            }

            string outputPath;
            if (!TryResolveContainedPath(ProjectRoot, loaded.Schema.outputDirectory, out outputPath) ||
                !loaded.Schema.outputDirectory.Replace('\\', '/').StartsWith("Assets/", StringComparison.Ordinal))
            {
                AddContractError(loaded.SchemaPath, "outputDirectory is outside this Unity project.", report, schemaReport);
                return;
            }

            loaded.TargetType = FindType(loaded.Schema.targetType);
            if (loaded.TargetType == null || !typeof(ScriptableObject).IsAssignableFrom(loaded.TargetType))
            {
                AddContractError(loaded.SchemaPath, "targetType '" + loaded.Schema.targetType + "' is not a ScriptableObject type.", report, schemaReport);
                return;
            }

            if (registration.IsAggregateAsset)
            {
                if (loaded.TargetType != typeof(LocTableAsset))
                {
                    AddContractError(loaded.SchemaPath, "aggregate Localization table must target LocTableAsset.", report, schemaReport);
                }
                return;
            }

            foreach (var column in loaded.Schema.columns ?? new List<DataTableColumnSchema>())
            {
                if (!ShouldWriteColumn(column))
                {
                    continue;
                }

                var field = loaded.TargetType.GetField(column.name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field == null || !IsMatchingFieldType(field.FieldType, column))
                {
                    AddContractError(
                        loaded.SchemaPath,
                        "column '" + column.name + "' cannot write compatible field on '" + loaded.TargetType.Name + "'.",
                        report,
                        schemaReport);
                }
            }
        }

        private static List<ImportPlanItem> BuildImportPlan(List<LoadedTable> tables, DataTableImportReport report)
        {
            var plan = new List<ImportPlanItem>();
            var assetPaths = new HashSet<string>(StringComparer.Ordinal);
            foreach (var loaded in tables)
            {
                var registration = CK01ImportRegistry.Find(loaded.Schema.tableName);
                if (registration != null && registration.IsAggregateAsset)
                {
                    var aggregatePath = loaded.Schema.outputDirectory.Replace('\\', '/').TrimEnd('/') + "/" + CK01ImportRegistry.LocalizationAssetName + ".asset";
                    if (assetPaths.Add(aggregatePath))
                    {
                        plan.Add(new ImportPlanItem
                        {
                            Loaded = loaded,
                            AssetPath = aggregatePath,
                            ExistingAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(aggregatePath),
                            IsAggregateAsset = true
                        });
                    }
                    continue;
                }
                var assetNameColumn = DataTableValidator.FindColumn(loaded.Schema, loaded.Schema.assetNameField);
                foreach (var row in loaded.Validation.Table.Rows)
                {
                    var assetName = DataTableValidator.GetEffectiveValue(assetNameColumn, row, loaded.Validation.Table);
                    var assetPath = (loaded.Schema.outputDirectory.Replace('\\', '/').TrimEnd('/') + "/" + assetName + ".asset");
                    if (!assetPaths.Add(assetPath))
                    {
                        report.errors.Add("Multiple schemas or rows target the same output asset: " + assetPath);
                        continue;
                    }

                    var existing = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
                    if (existing != null && existing.GetType() != loaded.TargetType)
                    {
                        report.errors.Add(assetPath + " already exists with type '" + existing.GetType().Name + "', expected '" + loaded.TargetType.Name + "'.");
                        continue;
                    }

                    plan.Add(new ImportPlanItem
                    {
                        Loaded = loaded,
                        Row = row,
                        AssetPath = assetPath,
                        ExistingAsset = existing
                    });
                }
            }

            return plan;
        }

        private static bool TryBuildDataCatalogTables(
            List<LoadedTable> tables,
            List<ImportPlanItem> plan,
            out List<CK01GeneratedDataTable> catalogTables,
            out string error)
        {
            catalogTables = new List<CK01GeneratedDataTable>();
            error = null;
            foreach (var registration in CK01ImportRegistry.GetRegistrations())
            {
                var loaded = FindTable(tables, registration.SchemaPath);
                if (loaded == null)
                {
                    error = "Registered table '" + registration.TableName + "' was not loaded.";
                    return false;
                }

                ScriptableObject aggregateAsset = null;
                var rows = new List<CK01GeneratedDataRowReference>();
                var primaryKeyColumn = DataTableValidator.FindColumn(loaded.Schema, loaded.Schema.primaryKeyField);
                foreach (var item in plan)
                {
                    if (!ReferenceEquals(item.Loaded, loaded))
                    {
                        continue;
                    }
                    if (item.ExistingAsset == null)
                    {
                        error = "Generated asset '" + item.AssetPath + "' is unavailable for catalog registration.";
                        return false;
                    }

                    if (item.IsAggregateAsset)
                    {
                        if (aggregateAsset != null)
                        {
                            error = "Table '" + registration.TableName + "' produced more than one aggregate asset.";
                            return false;
                        }
                        aggregateAsset = item.ExistingAsset;
                        continue;
                    }

                    var stableId = DataTableValidator.GetEffectiveValue(
                        primaryKeyColumn,
                        item.Row,
                        loaded.Validation.Table);
                    rows.Add(new CK01GeneratedDataRowReference(stableId, item.ExistingAsset));
                }

                if (registration.IsAggregateAsset && aggregateAsset == null)
                {
                    error = "Aggregate table '" + registration.TableName + "' produced no aggregate asset.";
                    return false;
                }

                catalogTables.Add(new CK01GeneratedDataTable(registration.TableName, aggregateAsset, rows));
            }

            return true;
        }

        private static bool TryApplyAggregateAsset(ScriptableObject asset, LoadedTable loaded, out bool changed, out string error)
        {
            changed = false;
            error = null;
            var table = asset as LocTableAsset;
            if (table == null)
            {
                error = "Localization aggregate target is not LocTableAsset.";
                return false;
            }

            var entries = new List<LocTextEntry>();
            foreach (var row in loaded.Validation.Table.Rows)
            {
                entries.Add(new LocTextEntry
                {
                    Key = DataTableValidator.GetEffectiveValue(DataTableValidator.FindColumn(loaded.Schema, "loc_key"), row, loaded.Validation.Table),
                    ZhCn = DataTableValidator.GetEffectiveValue(DataTableValidator.FindColumn(loaded.Schema, "zh_cn"), row, loaded.Validation.Table)
                });
            }
            changed = table.ReplaceEntries(entries);
            return true;
        }

        private static bool TryApplyRow(
            ScriptableObject asset,
            DataTableSchema schema,
            DataTableCsvRow row,
            DataTableCsvTable table,
            out bool changed,
            out string error,
            out List<string> warnings)
        {
            changed = false;
            error = null;
            warnings = new List<string>();
            foreach (var column in schema.columns ?? new List<DataTableColumnSchema>())
            {
                if (!ShouldWriteColumn(column))
                {
                    continue;
                }

                var field = asset.GetType().GetField(column.name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                object parsedValue;
                var rawValue = DataTableValidator.GetEffectiveValue(column, row, table);
                if (field.FieldType == typeof(Sprite))
                {
                    string warning;
                    parsedValue = ResolveSprite(rawValue, out warning);
                    if (!string.IsNullOrEmpty(warning)) warnings.Add(warning);
                }
                else if (!TryConvertValue(field.FieldType, column, rawValue, out parsedValue, out error))
                {
                    return false;
                }

                var currentValue = field.GetValue(asset);
                if (!ValuesEqual(currentValue, parsedValue))
                {
                    field.SetValue(asset, parsedValue);
                    changed = true;
                }
            }

            return true;
        }

        private static bool TryConvertValue(Type fieldType, DataTableColumnSchema column, string rawValue, out object value, out string error)
        {
            error = null;
            value = null;

            // Schema validation has already rejected missing required values. Unity
            // fields cannot represent nullable value types, so an allowed empty CSV
            // cell maps to that field type's serialized default.
            if (string.IsNullOrEmpty(rawValue) && fieldType.IsValueType)
            {
                value = Activator.CreateInstance(fieldType);
                return true;
            }

            try
            {
                if (fieldType == typeof(string))
                {
                    value = rawValue;
                    return true;
                }

                if (fieldType == typeof(int))
                {
                    value = int.Parse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture);
                    return true;
                }

                if (fieldType == typeof(float))
                {
                    value = float.Parse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture);
                    return true;
                }

                if (fieldType == typeof(bool))
                {
                    value = ParseBool(rawValue);
                    return true;
                }

                if (fieldType.IsEnum)
                {
                    value = Enum.Parse(fieldType, rawValue, false);
                    return true;
                }

                if (IsEnumList(fieldType))
                {
                    var itemType = fieldType.GetGenericArguments()[0];
                    var list = (IList)Activator.CreateInstance(fieldType);
                    var separator = string.IsNullOrEmpty(column.separator) ? "|" : column.separator;
                    if (!string.IsNullOrEmpty(rawValue))
                    {
                        foreach (var token in rawValue.Split(new[] { separator }, StringSplitOptions.None))
                        {
                            list.Add(Enum.Parse(itemType, token.Trim(), false));
                        }
                    }

                    value = list;
                    return true;
                }

                if (IsStringList(fieldType))
                {
                    var list = new List<string>();
                    var separator = string.IsNullOrEmpty(column.separator) ? "|" : column.separator;
                    if (!string.IsNullOrEmpty(rawValue))
                    {
                        foreach (var token in rawValue.Split(new[] { separator }, StringSplitOptions.None))
                        {
                            var trimmed = token.Trim();
                            if (trimmed.Length > 0) list.Add(trimmed);
                        }
                    }
                    value = list;
                    return true;
                }
            }
            catch (Exception exception)
            {
                error = "Could not convert field '" + column.name + "': " + exception.Message;
                return false;
            }

            error = "Unsupported target field type '" + fieldType + "' for column '" + column.name + "'.";
            return false;
        }

        private static bool ValuesEqual(object left, object right)
        {
            var leftList = left as IList;
            var rightList = right as IList;
            if (leftList != null || rightList != null)
            {
                if (leftList == null || rightList == null || leftList.Count != rightList.Count)
                {
                    return false;
                }

                for (var index = 0; index < leftList.Count; index++)
                {
                    if (!object.Equals(leftList[index], rightList[index]))
                    {
                        return false;
                    }
                }

                return true;
            }

            return object.Equals(left, right);
        }

        private static bool IsMatchingFieldType(Type fieldType, DataTableColumnSchema column)
        {
            if (string.Equals(column.type, "string", StringComparison.Ordinal)) return fieldType == typeof(string);
            if (string.Equals(column.type, "id", StringComparison.Ordinal) ||
                string.Equals(column.type, "key", StringComparison.Ordinal) ||
                string.Equals(column.type, "loc_key", StringComparison.Ordinal) ||
                string.Equals(column.type, "ref", StringComparison.Ordinal)) return fieldType == typeof(string);
            if (string.Equals(column.type, "int", StringComparison.Ordinal)) return fieldType == typeof(int);
            if (string.Equals(column.type, "float", StringComparison.Ordinal)) return fieldType == typeof(float);
            if (string.Equals(column.type, "bool", StringComparison.Ordinal)) return fieldType == typeof(bool);
            if (string.Equals(column.type, "sprite", StringComparison.Ordinal)) return fieldType == typeof(Sprite);
            if (string.Equals(column.type, "enum", StringComparison.Ordinal))
            {
                return fieldType == typeof(string) || (fieldType.IsEnum && fieldType.Name == column.enumType);
            }
            if (string.Equals(column.type, "list<ref>", StringComparison.Ordinal)) return IsStringList(fieldType);
            if (string.Equals(column.type, "list<enum>", StringComparison.Ordinal))
            {
                return IsEnumList(fieldType) && fieldType.GetGenericArguments()[0].Name == column.enumType;
            }

            return false;
        }

        private static bool IsEnumList(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>) && type.GetGenericArguments()[0].IsEnum;
        }

        private static bool IsStringList(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>) && type.GetGenericArguments()[0] == typeof(string);
        }

        private static bool ShouldWriteColumn(DataTableColumnSchema column)
        {
            return column != null && column.writeToAsset && !string.Equals(column.name, "comment", StringComparison.Ordinal);
        }

        private static Sprite ResolveSprite(string spriteName, out string warning)
        {
            warning = null;
            var lookup = SpriteLookupPolicy.Resolve(spriteName, FindExactSpritePaths);
            if (string.IsNullOrEmpty(spriteName)) return null;
            if (!lookup.UsesPlaceholder)
            {
                var resolved = AssetDatabase.LoadAssetAtPath<Sprite>(lookup.AssetPath);
                if (resolved != null) return resolved;
                lookup.UsesPlaceholder = true;
                lookup.Warning = "Sprite '" + spriteName + "' could not be loaded after lookup.";
            }
            warning = lookup.Warning;
            return GetOrCreatePlaceholderSprite();
        }

        private static IList<string> FindExactSpritePaths(string spriteName)
        {
            var found = new List<string>();
            foreach (var directory in CK01ImportRegistry.SpriteSearchDirectories)
            {
                if (!AssetDatabase.IsValidFolder(directory)) continue;
                foreach (var guid in AssetDatabase.FindAssets(spriteName + " t:Sprite", new[] { directory }))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (string.Equals(Path.GetFileNameWithoutExtension(path), spriteName, StringComparison.Ordinal) && !found.Contains(path)) found.Add(path);
                }
            }
            return found;
        }

        private static Sprite GetOrCreatePlaceholderSprite()
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(CK01ImportRegistry.MissingSpriteTextureAssetPath);
            if (texture == null)
            {
                string error;
                if (!TryEnsureAssetFolder(Path.GetDirectoryName(CK01ImportRegistry.MissingSpriteTextureAssetPath).Replace('\\', '/'), out error))
                {
                    throw new InvalidOperationException(error);
                }
                texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                texture.name = "CK01-MissingSpriteTexture";
                texture.SetPixel(0, 0, Color.magenta);
                texture.Apply();
                AssetDatabase.CreateAsset(texture, CK01ImportRegistry.MissingSpriteTextureAssetPath);
            }
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(CK01ImportRegistry.MissingSpriteTextureAssetPath))
            {
                var sprite = asset as Sprite;
                if (sprite != null) return sprite;
            }
            var created = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            created.name = "CK01-MissingSprite";
            AssetDatabase.AddObjectToAsset(created, texture);
            EditorUtility.SetDirty(texture);
            AssetDatabase.ImportAsset(CK01ImportRegistry.MissingSpriteTextureAssetPath);
            return created;
        }

        private static bool ParseBool(string value)
        {
            if (value == "1") return true;
            if (value == "0") return false;
            return bool.Parse(value);
        }

        private static LoadedTable FindTable(List<LoadedTable> tables, string schemaPath)
        {
            var normalized = (schemaPath ?? string.Empty).Replace('\\', '/');
            if (!normalized.StartsWith("Schemas/", StringComparison.Ordinal)) normalized = "Schemas/" + normalized.TrimStart('/');
            foreach (var table in tables)
            {
                if (string.Equals(table.SchemaPath, normalized, StringComparison.Ordinal))
                {
                    return table;
                }
            }

            return null;
        }

        private static void ValidateCK01Registration(List<LoadedTable> tables, DataTableImportReport report)
        {
            var registrations = CK01ImportRegistry.GetRegistrations();
            foreach (var registration in registrations)
            {
                if (FindTable(tables, registration.SchemaPath) == null)
                {
                    report.errors.Add("CK01 import registration references missing schema '" + registration.SchemaPath + "'.");
                }
            }
            if (registrations.Count != 14)
            {
                report.errors.Add("CK01 import registration must contain exactly 14 tables.");
            }
        }

        private static bool TryEnsureAssetFolder(string assetFolderPath, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(assetFolderPath) ||
                !assetFolderPath.StartsWith("Assets/", StringComparison.Ordinal))
            {
                error = "Generated asset folder must be under Assets/.";
                return false;
            }

            var segments = assetFolderPath.Split('/');
            var currentFolder = segments[0];
            try
            {
                for (var index = 1; index < segments.Length; index++)
                {
                    var nextFolder = currentFolder + "/" + segments[index];
                    if (!AssetDatabase.IsValidFolder(nextFolder))
                    {
                        var guid = AssetDatabase.CreateFolder(currentFolder, segments[index]);
                        if (string.IsNullOrEmpty(guid) || !AssetDatabase.IsValidFolder(nextFolder))
                        {
                            error = "Could not create generated asset folder '" + nextFolder + "'.";
                            return false;
                        }
                    }

                    currentFolder = nextFolder;
                }
            }
            catch (Exception exception)
            {
                error = "Could not create generated asset folder '" + assetFolderPath + "': " + exception.Message;
                return false;
            }

            return true;
        }

        private static Type FindType(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            var direct = Type.GetType(name);
            if (direct != null)
            {
                return direct;
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var found = assembly.GetType(name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static bool TryResolveContainedPath(string root, string relativePath, out string fullPath)
        {
            fullPath = null;
            if (!DataTablePathRules.IsSafeRelativePath(relativePath))
            {
                return false;
            }

            var rootFullPath = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var candidate = Path.GetFullPath(Path.Combine(rootFullPath, relativePath));
            if (!candidate.StartsWith(rootFullPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            fullPath = candidate;
            return true;
        }

        private static string ToDataTablesRelativePath(string fullPath)
        {
            var root = Path.GetFullPath(DataTablesRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return fullPath.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Replace('\\', '/');
        }

        private static IEnumerable<string> PrefixMessages(string prefix, List<string> messages)
        {
            var prefixed = new List<string>();
            foreach (var message in messages)
            {
                prefixed.Add(prefix + ": " + message);
            }

            return prefixed;
        }

        private static void AddContractError(
            string schemaPath,
            string message,
            DataTableImportReport report,
            DataTableSchemaImportReport schemaReport)
        {
            var fullMessage = schemaPath + ": " + message;
            report.errors.Add(fullMessage);
            schemaReport.errors.Add(message);
        }
    }
}
