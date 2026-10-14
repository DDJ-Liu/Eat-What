using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using EatWhat.DataTables;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace EatWhat.EditorTools.Fonts
{
    /// <summary>
    /// Builds the reviewed ShortCycle static atlas and its empty dynamic fallback.
    /// This class intentionally does not change TMP_Settings or any scene / prefab references.
    /// </summary>
    public static class ShortCycleFontAtlasBuilder
    {
        public const string LegacyFontAssetPath =
            "Assets/Font/Heavy/ShortCycle_Heavy_WithFallback.asset";
        public const string StaticFontAssetPath =
            "Assets/Font/Heavy/ShortCycle_Heavy_Static.asset";
        public const string DynamicFallbackAssetPath =
            "Assets/Font/Heavy/ShortCycle_Heavy_DynamicFallback.asset";
        public const string SourceHanFallbackAssetPath =
            "Assets/Font/Heavy/SourceHanSansSC-Heavy SDF.asset";
        public const string LocalizationCsvPath =
            "DataTables/Localization/Localization.csv";
        public const string BuildManifestPath =
            "DevTools/Fonts/ShortCycleFontBuildManifest.json";

        private const string MenuRoot = "Tools/CK01/Fonts/";

        [Serializable]
        public sealed class BuildManifest
        {
            public int schemaVersion = 1;
            public string approvalStatus = "PENDING_REVIEW";
            public string approvedBy;
            public string approvedAtUtc;
            public string generatedAtUtc;
            public string legacyAssetPath;
            public string legacyAssetSha256;
            public int legacyCharacterCount;
            public string localizationCsvPath;
            public string localizationCsvSha256;
            public int localizationCharacterCount;
            public int requiredCharacterCount;
            public string staticAssetPath;
            public string staticAssetSha256;
            public int staticCharacterCount;
            public string dynamicAssetPath;
            public string dynamicEmptyAssetSha256;
            public int dynamicCharacterCount;
            public string sourceHanFallbackAssetPath;
            public string sourceFontPath;
            public bool staticAtlasRewritten;
            public string[] requiredCodepoints;
            public string[] addedCodepoints;
        }

        [MenuItem(MenuRoot + "Build Static Atlas + Empty Dynamic Fallback")]
        public static void BuildFromMenu()
        {
            var manifest = BuildAll();
            Debug.Log("[ShortCycleFont] Build complete.\n" + JsonUtility.ToJson(manifest, true));
        }

        [MenuItem(MenuRoot + "Clear Dynamic Fallback For Commit")]
        public static void ClearDynamicFallbackFromMenu()
        {
            var manifest = ClearDynamicFallbackForCommit();
            Debug.Log("[ShortCycleFont] Dynamic fallback cleared.\n" +
                JsonUtility.ToJson(manifest, true));
        }

        /// <summary>Unity -executeMethod entry point.</summary>
        public static void BuildAllBatch()
        {
            try
            {
                var manifest = BuildAll();
                Debug.Log("[ShortCycleFont] FONT_BUILD_OK " + JsonUtility.ToJson(manifest));
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
        }

        public static BuildManifest BuildAll()
        {
            var legacy = LoadRequired<TMP_FontAsset>(LegacyFontAssetPath);
            var sourceHan = LoadRequired<TMP_FontAsset>(SourceHanFallbackAssetPath);
            if (legacy.sourceFontFile == null)
            {
                throw new InvalidOperationException(
                    "Legacy ShortCycle font asset has no source font file reference.");
            }

            var legacyCharacters = new HashSet<uint>(
                legacy.characterTable.Where(character => character != null)
                    .Select(character => character.unicode));
            var localizationCharacters = ReadLocalizationCharacters(LocalizationCsvPath);
            var requiredCharacters = new HashSet<uint>(legacyCharacters);
            requiredCharacters.UnionWith(localizationCharacters);
            var staticBefore = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(StaticFontAssetPath);
            var existingCharacters = staticBefore == null
                ? new HashSet<uint>()
                : new HashSet<uint>(staticBefore.characterTable
                    .Where(character => character != null).Select(character => character.unicode));
            var addedCodepoints = requiredCharacters.Where(value => !existingCharacters.Contains(value))
                .OrderBy(value => value).ToArray();

            var dynamicFallback = EnsureDynamicFallback(legacy, sourceHan);
            var staticAtlasRewritten = EnsureStaticFont(
                legacy,
                dynamicFallback,
                requiredCharacters.OrderBy(value => value).ToArray());

            var staticAsset = LoadRequired<TMP_FontAsset>(StaticFontAssetPath);
            if (EditorUtility.IsDirty(staticAsset)) AssetDatabase.SaveAssetIfDirty(staticAsset);
            if (EditorUtility.IsDirty(dynamicFallback)) AssetDatabase.SaveAssetIfDirty(dynamicFallback);
            var manifest = CreateManifest(
                legacyCharacters.Count,
                localizationCharacters.Count,
                requiredCharacters.Count,
                staticAtlasRewritten,
                requiredCharacters,
                addedCodepoints);
            WriteManifestIfChanged(manifest);
            return manifest;
        }

        public static BuildManifest ClearDynamicFallbackForCommit()
        {
            var dynamicFallback = LoadRequired<TMP_FontAsset>(DynamicFallbackAssetPath);
            var sourceHan = LoadRequired<TMP_FontAsset>(SourceHanFallbackAssetPath);
            dynamicFallback.ClearFontAssetData(false);
            dynamicFallback.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            SetFallbackChain(dynamicFallback, sourceHan);
            EditorUtility.SetDirty(dynamicFallback);
            AssetDatabase.SaveAssetIfDirty(dynamicFallback);

            var legacy = LoadRequired<TMP_FontAsset>(LegacyFontAssetPath);
            var legacyCount = legacy.characterTable.Count;
            var localizationCount = ReadLocalizationCharacters(LocalizationCsvPath).Count;
            var staticAsset = LoadRequired<TMP_FontAsset>(StaticFontAssetPath);
            var manifest = CreateManifest(
                legacyCount,
                localizationCount,
                staticAsset.characterTable.Count,
                false,
                null,
                null);
            WriteManifestIfChanged(manifest);
            return manifest;
        }

        private static HashSet<uint> ReadLocalizationCharacters(string path)
        {
            var fullPath = ToFullPath(path);
            if (!File.Exists(fullPath))
                throw new FileNotFoundException("Localization CSV is missing.", fullPath);

            var text = File.ReadAllText(fullPath, new UTF8Encoding(false, true));
            var parsed = DataTableCsvParser.Parse(text);
            if (!parsed.IsValid)
            {
                throw new InvalidOperationException(
                    "Localization CSV parse failed: " + string.Join(" | ", parsed.Errors));
            }

            var valueIndex = parsed.Table.GetHeaderIndex("zh_cn");
            if (valueIndex < 0)
                throw new InvalidOperationException("Localization CSV has no zh_cn column.");

            var characters = new HashSet<uint>();
            foreach (var row in parsed.Table.Rows)
            {
                if (row.Values.Count <= valueIndex) continue;
                AddUnicodeScalars(row.Values[valueIndex], characters);
            }

            return characters;
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

        private static TMP_FontAsset EnsureDynamicFallback(
            TMP_FontAsset legacy,
            TMP_FontAsset sourceHan)
        {
            var asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DynamicFallbackAssetPath);
            var created = asset == null;
            if (created)
            {
                asset = CreateAssetFromLegacy(legacy, DynamicFallbackAssetPath);
            }

            var changed = created;
            changed |= SetSourceFont(asset, legacy.sourceFontFile);
            if (asset.atlasPopulationMode != AtlasPopulationMode.Dynamic)
            {
                asset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                changed = true;
            }
            if (asset.characterTable.Count > 0 || asset.glyphTable.Count > 0)
            {
                asset.ClearFontAssetData(false);
                changed = true;
            }
            changed |= SetFallbackChain(asset, sourceHan);
            if (changed) EditorUtility.SetDirty(asset);
            AttachGeneratedSubAssets(asset);
            return asset;
        }

        private static bool EnsureStaticFont(
            TMP_FontAsset legacy,
            TMP_FontAsset dynamicFallback,
            uint[] requiredCharacters)
        {
            var asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(StaticFontAssetPath);
            var created = asset == null;
            if (created) asset = CreateAssetFromLegacy(legacy, StaticFontAssetPath);

            var existing = new HashSet<uint>(asset.characterTable
                .Where(character => character != null)
                .Select(character => character.unicode));
            var missing = requiredCharacters.Where(value => !existing.Contains(value)).ToArray();
            // TMP synthesizes layout controls such as LF without storing a glyph-table
            // entry. Keep them in the authoritative requirement count, but only ask
            // FreeType to rasterize characters TMP does not already support.
            var rasterMissing = missing.Where(value => !asset.HasCharacter((int)value)).ToArray();
            var atlasRewritten = created || rasterMissing.Length > 0;
            var metadataChanged = created;

            metadataChanged |= SetSourceFont(asset, legacy.sourceFontFile);

            if (rasterMissing.Length > 0)
            {
                asset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                uint[] unresolved;
                asset.TryAddCharacters(rasterMissing, out unresolved, true);
                var unsupported = (unresolved ?? Array.Empty<uint>())
                    .Where(value => !asset.HasCharacter((int)value))
                    .ToArray();
                if (unsupported.Length > 0)
                {
                    throw new InvalidOperationException(
                        "ShortCycle source font cannot supply required characters: " +
                        string.Join(",", unsupported.Select(value => "U+" + value.ToString("X4"))));
                }
                metadataChanged = true;
            }

            if (asset.atlasPopulationMode != AtlasPopulationMode.Static)
            {
                asset.atlasPopulationMode = AtlasPopulationMode.Static;
                metadataChanged = true;
            }
            // TMP clears the source-font object reference when switching to Static.
            // Restore it after the mode transition so the generated asset remains
            // traceable and can be deterministically extended by a later rebuild.
            metadataChanged |= SetSourceFont(asset, legacy.sourceFontFile);
            metadataChanged |= SetFallbackChain(asset, dynamicFallback);
            AttachGeneratedSubAssets(asset);
            if (metadataChanged) EditorUtility.SetDirty(asset);
            return atlasRewritten;
        }

        private static TMP_FontAsset CreateAssetFromLegacy(TMP_FontAsset legacy, string path)
        {
            var source = legacy.sourceFontFile;
            var pointSize = Mathf.Max(1, Mathf.RoundToInt(legacy.faceInfo.pointSize));
            var asset = TMP_FontAsset.CreateFontAsset(
                source,
                pointSize,
                legacy.atlasPadding,
                legacy.atlasRenderMode,
                legacy.atlasWidth,
                legacy.atlasHeight,
                AtlasPopulationMode.Dynamic,
                true);
            if (asset == null)
                throw new InvalidOperationException("TMP could not create a ShortCycle font asset.");

            asset.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(asset, path);
            AttachGeneratedSubAssets(asset);
            return asset;
        }

        private static void AttachGeneratedSubAssets(TMP_FontAsset asset)
        {
            if (asset.material != null && !EditorUtility.IsPersistent(asset.material))
                AssetDatabase.AddObjectToAsset(asset.material, asset);
            foreach (var texture in asset.atlasTextures ?? new Texture2D[0])
            {
                if (texture != null && !EditorUtility.IsPersistent(texture))
                    AssetDatabase.AddObjectToAsset(texture, asset);
            }
        }

        private static bool SetFallbackChain(TMP_FontAsset asset, TMP_FontAsset onlyFallback)
        {
            var table = asset.fallbackFontAssetTable;
            if (table != null && table.Count == 1 && table[0] == onlyFallback) return false;
            asset.fallbackFontAssetTable = new List<TMP_FontAsset> { onlyFallback };
            return true;
        }

        private static bool SetSourceFont(TMP_FontAsset asset, Font source)
        {
            if (asset.sourceFontFile == source) return false;
            var serialized = new SerializedObject(asset);
            var runtimeReference = serialized.FindProperty("m_SourceFontFile");
            var editorReference = serialized.FindProperty("m_SourceFontFile_EditorRef");
            var guid = serialized.FindProperty("m_SourceFontFileGUID");
            if (runtimeReference == null || guid == null)
                throw new InvalidOperationException("TMP source-font serialized contract changed.");
            runtimeReference.objectReferenceValue = source;
            if (editorReference != null) editorReference.objectReferenceValue = source;
            guid.stringValue = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source));
            if (!serialized.ApplyModifiedPropertiesWithoutUndo())
                throw new InvalidOperationException("TMP source-font update was not applied.");
            return true;
        }

        private static T LoadRequired<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) throw new FileNotFoundException("Required asset is missing: " + path);
            return asset;
        }

        private static BuildManifest CreateManifest(
            int legacyCount,
            int localizationCount,
            int requiredCount,
            bool staticAtlasRewritten,
            IEnumerable<uint> requiredCodepoints,
            IEnumerable<uint> addedCodepoints)
        {
            var staticAsset = LoadRequired<TMP_FontAsset>(StaticFontAssetPath);
            var dynamicAsset = LoadRequired<TMP_FontAsset>(DynamicFallbackAssetPath);
            if (dynamicAsset.characterTable.Count != 0 || dynamicAsset.glyphTable.Count != 0)
                throw new InvalidOperationException("Dynamic fallback must be empty before manifest creation.");

            return new BuildManifest
            {
                generatedAtUtc = DateTime.UtcNow.ToString("o"),
                legacyAssetPath = LegacyFontAssetPath,
                legacyAssetSha256 = Sha256File(LegacyFontAssetPath),
                legacyCharacterCount = legacyCount,
                localizationCsvPath = LocalizationCsvPath,
                localizationCsvSha256 = Sha256File(LocalizationCsvPath),
                localizationCharacterCount = localizationCount,
                requiredCharacterCount = requiredCount,
                staticAssetPath = StaticFontAssetPath,
                staticAssetSha256 = Sha256File(StaticFontAssetPath),
                staticCharacterCount = staticAsset.characterTable.Count,
                dynamicAssetPath = DynamicFallbackAssetPath,
                dynamicEmptyAssetSha256 = Sha256File(DynamicFallbackAssetPath),
                dynamicCharacterCount = dynamicAsset.characterTable.Count,
                sourceHanFallbackAssetPath = SourceHanFallbackAssetPath,
                sourceFontPath = AssetDatabase.GetAssetPath(staticAsset.sourceFontFile),
                staticAtlasRewritten = staticAtlasRewritten,
                requiredCodepoints = FormatCodepoints(requiredCodepoints),
                addedCodepoints = FormatCodepoints(addedCodepoints)
            };
        }

        private static string[] FormatCodepoints(IEnumerable<uint> codepoints)
        {
            return codepoints == null
                ? Array.Empty<string>()
                : codepoints.OrderBy(value => value)
                    .Select(value => "U+" + value.ToString("X4")).ToArray();
        }

        private static void WriteManifestIfChanged(BuildManifest manifest)
        {
            var fullPath = ToFullPath(BuildManifestPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            if (File.Exists(fullPath))
            {
                var existing = JsonUtility.FromJson<BuildManifest>(File.ReadAllText(fullPath));
                if (existing != null && SameProtectedContent(existing, manifest) &&
                    string.Equals(existing.approvalStatus, "APPROVED", StringComparison.Ordinal))
                {
                    manifest.approvalStatus = existing.approvalStatus;
                    manifest.approvedBy = existing.approvedBy;
                    manifest.approvedAtUtc = existing.approvedAtUtc;
                }
                if (existing != null && SameProtectedContent(existing, manifest) &&
                    existing.localizationCsvSha256 == manifest.localizationCsvSha256 &&
                    existing.localizationCharacterCount == manifest.localizationCharacterCount &&
                    existing.staticCharacterCount == manifest.staticCharacterCount)
                {
                    manifest.generatedAtUtc = existing.generatedAtUtc;
                }
            }
            var json = JsonUtility.ToJson(manifest, true) + Environment.NewLine;
            if (File.Exists(fullPath) && File.ReadAllText(fullPath) == json) return;
            File.WriteAllText(fullPath, json, new UTF8Encoding(false));
        }

        private static bool SameProtectedContent(BuildManifest left, BuildManifest right)
        {
            return left.legacyAssetPath == right.legacyAssetPath &&
                left.legacyAssetSha256 == right.legacyAssetSha256 &&
                left.staticAssetPath == right.staticAssetPath &&
                left.staticAssetSha256 == right.staticAssetSha256 &&
                left.dynamicAssetPath == right.dynamicAssetPath &&
                left.dynamicEmptyAssetSha256 == right.dynamicEmptyAssetSha256 &&
                left.sourceHanFallbackAssetPath == right.sourceHanFallbackAssetPath &&
                left.sourceFontPath == right.sourceFontPath &&
                left.requiredCharacterCount == right.requiredCharacterCount;
        }

        private static string Sha256File(string projectRelativePath)
        {
            using (var stream = File.OpenRead(ToFullPath(projectRelativePath)))
            using (var sha = SHA256.Create())
                return string.Concat(sha.ComputeHash(stream).Select(value => value.ToString("X2")));
        }

        private static string ToFullPath(string projectRelativePath)
        {
            var root = Directory.GetParent(Application.dataPath).FullName;
            return Path.GetFullPath(Path.Combine(root, projectRelativePath));
        }
    }
}
