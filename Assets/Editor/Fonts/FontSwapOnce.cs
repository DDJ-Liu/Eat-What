using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using EatWhat.DataTables;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EatWhat.EditorTools.Fonts
{
    // FONT-R2: explicit CSV identities only; screenshots and dynamic clearing belong to R3.
    public static class FontSwapOnce
    {
        public const string Mapping = ".ai-workspace/outputs/control/U1-PRE-20260923/PRE1_字体材质迁移逐项草案.csv";
        public const string StaticGuid = "9b07c6c50dde09d4b8719754c27ddc96";
        public const string StaticPath = "Assets/Font/Heavy/ShortCycle_Heavy_Static.asset";
        public const string StaticSha256 = "E877950344A043B02BE025B7C2E12E2889E9EC69D20E72A3C90785F5B8EE1220";
        public const string Cover = "Spaces/Phase0_RecipeBook/Cover_Group/CoverTitle";
        static readonly string[] Names = { "SourceHan", "LegacyShortCycle", "Heavy" };
        static readonly string[] Guids = { "8e525bf3cdd33804189d28645ca0f556", "87a7857d7b1355641aee5b8ac50c5c1b", "389683e661c8d24468fea402edba0841" };
        static readonly string[] Sources = { "GlobalObjectId_V1-3-53ffd732c8bc2034f93d5a714c25eda2-12569091968840867831-0", "GlobalObjectId_V1-3-75909fc3d0fa2c94baa5706654d37655-9052723641695353799-0", "GlobalObjectId_V1-3-ce30f77e3b8c0ad45b7b7ae26b956bb6-18028800558602920526-0" };
        static readonly string[] Assets = { "Assets/Scenes/Cooking/ShortCycle_P0P1.unity", "Assets/Prefabs/Cooking/ShortCycle/GridPieces/CatalogCard.prefab", "Assets/Prefabs/Cooking/ShortCycle/GridPieces/FridgeSlot.prefab", "Assets/Prefabs/Cooking/ShortCycle/GridPieces/IngredientCard.prefab", "Assets/Prefabs/Cooking/ShortCycle/GridPieces/Sticker.prefab", "Assets/Prefabs/Cooking/ShortCycle/GridPieces/TraySlot.prefab" };
        public sealed class Row { public string Asset, Path, BeforeFont; public int Component, Style; public string Key => Asset + "|" + Path + "|" + Component; }
        static void Require(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
        static bool staticGuardFailed;
        // One guard must span a controlled load/render/save sequence. Never rebaseline after a failure.
        public sealed class StaticGuard
        {
            public TMP_FontAsset Font { get; private set; }
            public bool WasAlreadyLoaded { get; private set; }
            readonly string baseline;
            readonly string path, guid;
            bool failed;
            internal StaticGuard(string path, string guid)
            {
                this.path = path; this.guid = guid;
                CheckDisk("before Static load");
                Font = Resources.FindObjectsOfTypeAll<TMP_FontAsset>().SingleOrDefault(f => AssetDatabase.GetAssetPath(f) == path);
                WasAlreadyLoaded = Font != null;
                string before = WasAlreadyLoaded ? Memory() : null;
                var loaded = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                Require(!WasAlreadyLoaded || loaded == Font, "Static identity changed during load");
                Font = loaded;
                string after = Memory(); // A/B/C must equal the approved disk baseline, even on first load.
                Require(before == null || before == after, "Static memory changed during initial load");
                baseline = after;
                Check("after Static load");
            }
            void CheckDisk(string boundary)
            {
                using (var sha = SHA256.Create())
                using (var stream = File.OpenRead(path))
                    Require(BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "") == StaticSha256, "Static disk SHA mismatch: " + boundary);
                Require(AssetDatabase.AssetPathToGUID(path) == guid, "Static GUID mismatch: " + boundary);
            }
            string Memory()
            {
                Require(Font != null && !EditorUtility.IsDirty(Font), "Static font missing/dirty");
                CheckMaterial(Font.material, "Static atlas material");
                foreach (var key in new[] { "_ScaleRatioA", "_ScaleRatioB", "_ScaleRatioC" })
                    Require(Font.material.GetFloat(key) == 1f, "Static approved ratio mismatch: " + key);
                var textures = Font.atlasTextures;
                Require(textures != null && textures.Length == 1 && textures[0] != null && Font.atlasTexture == textures[0] && Font.material.GetTexture("_MainTex") == textures[0], "Static atlas texture missing/mismatched");
                Require(!EditorUtility.IsDirty(textures[0]), "Static atlas texture dirty");
                // The getter can populate the serialized texture hash; sample it once before JSON.
                var imageHash = textures[0].imageContentsHash;
                return Font.GetInstanceID() + "|" + Font.material.GetInstanceID() + "|" + textures[0].GetInstanceID() + "\n" +
                    EditorJsonUtility.ToJson(Font) + "\n" + EditorJsonUtility.ToJson(Font.material) + "\n" +
                    EditorJsonUtility.ToJson(textures[0]) + "\n" + imageHash;
            }
            public void Check(string boundary)
            {
                Require(!failed && !staticGuardFailed, "Static guard already failed; preserve evidence and stop");
                try
                {
                    CheckDisk(boundary);
                    Require(baseline == Memory(), "Static memory changed: " + boundary);
                }
                catch (Exception ex) { failed = staticGuardFailed = true; throw new InvalidOperationException("Static guard failed at " + boundary + ": " + ex.Message, ex); }
            }
            public void Around(string boundary, Action action)
            {
                Check("before " + boundary);
                try { action(); }
                finally { Check("after " + boundary); }
            }
        }
        static StaticGuard BeginGuard(string path, string guid)
        {
            Require(!staticGuardFailed, "Static guard already failed; preserve evidence and stop");
            try { return new StaticGuard(path, guid); }
            catch { staticGuardFailed = true; throw; }
        }
        public static StaticGuard BeginStaticGuard() => BeginGuard(StaticPath, StaticGuid);
        // ENGINE-only isolated fixture: same approved bytes/ratios, distinct reviewed meta; no creation or writes.
        public static StaticGuard BeginIsolatedStaticGuard(string path, string guid)
        {
            Require(path != null && path.StartsWith("Assets/Tests/FontSwapOnce/", StringComparison.Ordinal) && !path.Contains("..") && !path.Contains("\\") && !string.IsNullOrEmpty(guid) && guid != StaticGuid, "Requires an isolated fixture path and distinct GUID");
            return BeginGuard(path, guid);
        }
        static string MaterialPath(int style) => "Assets/Font/Heavy/Materials/ShortCycle_Static_From" + Names[style] + ".mat";
        const string StandardShader = "TextMeshPro/Distance Field", MobileShader = "TextMeshPro/Mobile/Distance Field";
        // Properties shared by the two checked-in SDF shaders, including every native ratio input/output.
        const string CommonProperties = "_MainTex _TextureWidth _TextureHeight _GradientScale _FaceColor _FaceDilate _OutlineColor _OutlineWidth _OutlineSoftness " +
            "_UnderlayColor _UnderlayOffsetX _UnderlayOffsetY _UnderlayDilate _UnderlaySoftness _WeightNormal _WeightBold _ShaderFlags _ScaleRatioA _ScaleRatioB _ScaleRatioC " +
            "_ScaleX _ScaleY _PerspectiveFilter _Sharpness _VertexOffsetX _VertexOffsetY _ClipRect _MaskSoftnessX _MaskSoftnessY _StencilComp _Stencil _StencilOp _StencilWriteMask _StencilReadMask _CullMode _ColorMask";
        const string StandardProperties = "_FaceTex _FaceUVSpeedX _FaceUVSpeedY _OutlineTex _OutlineUVSpeedX _OutlineUVSpeedY _Bevel _BevelOffset _BevelWidth _BevelClamp _BevelRoundness " +
            "_LightAngle _SpecularColor _SpecularPower _Reflectivity _Diffuse _Ambient _BumpMap _BumpOutline _BumpFace _ReflectFaceColor _ReflectOutlineColor _Cube _EnvMatrixRotation _GlowColor _GlowOffset _GlowInner _GlowOuter _GlowPower _MaskCoord";
        static void CheckMaterial(Material material, string role)
        {
            Require(material != null, role + ": material missing");
            Require(!EditorUtility.IsDirty(material), role + ": material dirty");
            var shader = material.shader;
            Require(shader != null && (shader.name == StandardShader || shader.name == MobileShader), role + ": unsupported/null shader");
            string expectedGuid = shader.name == StandardShader ? "68e6db2ebdc24f95958faec2be5558d6" : "fe393ace9b354375a9cb14cdbbc28be4";
            Require(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(shader)) == expectedGuid, role + ": shader GUID mismatch");
            var properties = CommonProperties + (shader.name == StandardShader ? " " + StandardProperties : "");
            foreach (var key in properties.Split(' ')) Require(material.HasProperty(key), role + ": missing property " + key);
        }
        public static Row[] ReadPlan(string csv)
        {
            var parsed = DataTableCsvParser.Parse(csv);
            Require(parsed.IsValid, "Invalid mapping CSV: " + string.Join("; ", parsed.Errors));
            var result = new Dictionary<string, Row>();
            foreach (var r in parsed.Table.Rows)
            {
                Func<string, string> cell = h => r.Values[parsed.Table.GetHeaderIndex(h)];
                int style = Array.FindIndex(Names, n => cell("proposedMaterialPath") == MaterialPath(Array.IndexOf(Names, n)));
                Require(style >= 0 && Assets.Contains(cell("assetPath")) && cell("targetFontGuid") == StaticGuid, "Unknown mapping asset/material/target");
                Require(cell("currentMaterialGlobalId") == Sources[style], "Ambiguous source material");
                var row = new Row { Asset = cell("assetPath"), Path = cell("objectPath"), Component = int.Parse(cell("componentIndex")), Style = style, BeforeFont = cell("currentFontGuid") };
                Require(row.Component >= 0 && (row.BeforeFont == "75909fc3d0fa2c94baa5706654d37655" || (row.Path == Cover && row.BeforeFont == "ce30f77e3b8c0ad45b7b7ae26b956bb6")), "Unexpected original font/index");
                Require(!result.TryGetValue(row.Key, out var old) || (old.Style == row.Style && old.BeforeFont == row.BeforeFont), "Conflicting duplicate: " + row.Key);
                result[row.Key] = row;
            }
            var rows = result.Values.ToArray();
            Require(rows.Length == 171 && rows.Count(r => r.Path == Cover && r.Style == 2) == 1, "Expected 171 identities including CoverTitle -> Heavy");
            Require(rows.Select(r => r.Asset).Distinct().Count() == Assets.Length, "Incomplete asset scope");
            return rows;
        }
        public static void PrepareMaterial(Material destination, Material source, Material atlas)
        {
            destination.CopyPropertiesFromMaterial(source);
            destination.SetTexture("_MainTex", atlas.GetTexture("_MainTex"));
            foreach (var key in new[] { "_TextureWidth", "_TextureHeight", "_GradientScale" }) destination.SetFloat(key, atlas.GetFloat(key));
            ShaderUtilities.UpdateShaderRatios(destination);
        }
        public static bool Bind(TMP_Text text, TMP_FontAsset font, Material material)
        {
            Require(text != null && font != null && material != null && material != font.material && material.GetTexture("_MainTex") == font.atlasTexture, "Binding needs an external material with the target atlas");
            if (text.font == font && text.fontSharedMaterial == material) return false;
            // TMP LoadFontAsset falls back to font.material if the currently bound atlas differs.
            text.fontSharedMaterial = material;
            text.font = font;
            Require(text.font == font && text.fontSharedMaterial == material, "TMP binding did not retain the approved pair");
            EditorUtility.SetDirty(text);
            if (PrefabUtility.IsPartOfPrefabInstance(text)) PrefabUtility.RecordPrefabInstancePropertyModifications(text);
            return true;
        }
        static string ObjectPath(Transform t) => t.parent == null ? t.name : ObjectPath(t.parent) + "/" + t.name;
        [MenuItem("Tools/CK01/Fonts/Swap To Static (once)")]
        public static void Swap() { Run(false); }
        public static int Run(bool dryRun = true)
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && PrefabStageUtility.GetCurrentPrefabStage() == null, "Requires Edit mode, no compile or Prefab Stage");
            for (int i = 0; i < SceneManager.sceneCount; i++) Require(!SceneManager.GetSceneAt(i).isDirty, "Unsaved scene: stop without saving/discarding");
            var rows = ReadPlan(File.ReadAllText(Mapping));
            var guard = BeginStaticGuard();
            var font = guard.Font;
            Require(font != null, "Static font missing");
            Require(!EditorUtility.IsDirty(font), "Static font dirty");
            CheckMaterial(font.material, "Static atlas material");
            var atlas = font.material.GetTexture("_MainTex");
            Require(atlas != null && font.atlasTexture == atlas, "Static atlas texture missing/mismatched");
            Require(!EditorUtility.IsDirty(atlas), "Static atlas texture dirty");
            Require(atlas.width > 0 && atlas.height > 0 && font.material.GetFloat("_TextureWidth") == atlas.width && font.material.GetFloat("_TextureHeight") == atlas.height, "Static atlas dimensions mismatch/invalid");
            float gradient = font.material.GetFloat("_GradientScale");
            Require(gradient > 0 && !float.IsInfinity(gradient), "Static GradientScale must be finite and positive");
            var materials = new Material[3]; var candidates = new Material[3];
            var roots = new Dictionary<string, GameObject[]>(); var prefabs = new List<GameObject>(); var opened = new List<Scene>();
            try
            {
                for (int i = 0; i < 3; i++)
                {
                    guard.Check("before material/source load " + Names[i]);
                    materials[i] = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath(i));
                    Require(AssetDatabase.AssetPathToGUID(MaterialPath(i)) == Guids[i], "Target material GUID mismatch: " + Names[i]);
                    CheckMaterial(materials[i], "Target " + Names[i]);
                    Require(GlobalObjectId.TryParse(Sources[i], out var id), "Invalid source identity");
                    var source = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as Material;
                    CheckMaterial(source, "Source " + Names[i]);
                    Require(source.shader == materials[i].shader, "Source/target shader mismatch: " + Names[i]);
                    Require(source.shader == font.material.shader || (source.shader.name == StandardShader && font.material.shader.name == MobileShader), "Unsupported source/Static shader pair: " + Names[i]);
                    candidates[i] = new Material(materials[i]);
                    candidates[i].name = materials[i].name;
                    PrepareMaterial(candidates[i], source, font.material);
                    guard.Check("after material/source load " + Names[i]);
                }
                foreach (var path in Assets)
                {
                    guard.Check("before load " + path);
                    if (path.EndsWith(".unity", StringComparison.Ordinal))
                    {
                        var scene = SceneManager.GetSceneByPath(path);
                        if (!scene.isLoaded) { scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive); opened.Add(scene); }
                        roots[path] = scene.GetRootGameObjects();
                    }
                    else { var root = PrefabUtility.LoadPrefabContents(path); prefabs.Add(root); roots[path] = new[] { root }; }
                    guard.Check("after load " + path);
                }
                var targets = new Dictionary<string, TMP_Text>();
                foreach (var row in rows)
                {
                    guard.Check("before target lookup " + row.Key);
                    var matches = roots[row.Asset].SelectMany(r => r.GetComponentsInChildren<TMP_Text>(true)).Where(t => ObjectPath(t.transform) == row.Path && Array.IndexOf(t.GetComponents<TMP_Text>(), t) == row.Component).ToArray();
                    Require(matches.Length == 1, "Missing/ambiguous TMP: " + row.Key);
                    var tmp = matches[0]; var original = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(row.BeforeFont));
                    Require(tmp.font == font || (original != null && tmp.font == original), "Unknown font: " + row.Key);
                    GlobalObjectId.TryParse(Sources[row.Style], out var sourceId);
                    var source = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(sourceId) as Material;
                    Require(tmp.fontSharedMaterial == materials[row.Style] || (tmp.font != font && (tmp.fontSharedMaterial == source || tmp.fontSharedMaterial == original.material)), "Unknown material/category: " + row.Key);
                    targets.Add(row.Key, tmp);
                    guard.Check("after target lookup " + row.Key);
                }
                if (dryRun) { Debug.Log("FontSwapOnce dry-run: 171 identities; CoverTitle -> Heavy; no asset writes"); return rows.Length; }
                for (int i = 0; i < 3; i++)
                    if (EditorJsonUtility.ToJson(materials[i]) != EditorJsonUtility.ToJson(candidates[i]))
                    {
                        guard.Check("before material update " + Names[i]);
                        materials[i].CopyPropertiesFromMaterial(candidates[i]); EditorUtility.SetDirty(materials[i]);
                        guard.Around("material save " + Names[i], () => AssetDatabase.SaveAssetIfDirty(materials[i]));
                    }
                int changed = 0;
                foreach (var group in rows.GroupBy(r => r.Asset).OrderBy(g => g.Key.EndsWith(".unity", StringComparison.Ordinal) ? 0 : 1)) // Instances before sources.
                {
                    bool dirty = false;
                    foreach (var row in group)
                    {
                        var tmp = targets[row.Key];
                        if (row.Path == Cover) Debug.Log("CoverTitle: " + tmp.font.name + " -> " + font.name + " / FromHeavy");
                        guard.Around("binding " + row.Key, () => { if (Bind(tmp, font, materials[row.Style])) { dirty = true; changed++; } });
                    }
                    if (!dirty) continue;
                    if (group.Key.EndsWith(".unity", StringComparison.Ordinal))
                    { var scene = SceneManager.GetSceneByPath(group.Key); EditorSceneManager.MarkSceneDirty(scene); guard.Around("scene save " + group.Key, () => Require(EditorSceneManager.SaveScene(scene), "Scene save failed")); }
                    else guard.Around("prefab save " + group.Key, () => { PrefabUtility.SaveAsPrefabAsset(roots[group.Key][0], group.Key, out bool saved); Require(saved, "Prefab save failed"); });
                }
                guard.Check("run complete");
                Debug.Log("FontSwapOnce: 171 checked; changed TMP=" + changed + "; capture CoverTitle/five views and clear dynamic fallback in R3");
                return changed;
            }
            finally
            {
                foreach (var root in prefabs) PrefabUtility.UnloadPrefabContents(root);
                foreach (var scene in opened) if (!scene.isDirty) EditorSceneManager.CloseScene(scene, true); // Preserve unexpected unsaved state on failure.
                foreach (var candidate in candidates) if (candidate != null) UnityEngine.Object.DestroyImmediate(candidate);
            }
        }
    }
}
