// Offline boundary doubles: production FontSwapOnce.cs is compiled unchanged.
// These doubles do not claim Unity serialization, GPU or native TMP ratio equivalence.
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using Object = UnityEngine.Object;
using EatWhat.EditorTools.Fonts;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using File = System.IO.File;

// File boundary only; actual production SHA implementation is compiled unchanged.
namespace EatWhat.EditorTools.Fonts
{
    public static class File
    {
        public static byte[] ApprovedBytes, DiskBytes;
        public static int Reads;
        public static Action OnRead;
        public static string ReadAllText(string path) { return System.IO.File.ReadAllText(path); }
        public static Stream OpenRead(string path)
        {
            if (path != FontSwapOnce.StaticPath && path != "Assets/Tests/FontSwapOnce/Static.asset") throw new Exception("Unexpected protected file path");
            Reads++; if (OnRead != null) OnRead();
            return new MemoryStream(DiskBytes, false);
        }
    }
}

namespace UnityEngine
{
    public class Object { public string name = ""; static int next; readonly int id = ++next; public int GetInstanceID() { return id; } public static int Destroyed; public static void DestroyImmediate(Object value) { Destroyed++; } }
    public static class Resources
    {
        public static bool StaticLoaded = true;
        public static T[] FindObjectsOfTypeAll<T>() where T : Object { return StaticLoaded ? AssetDatabase.Objects.Values.OfType<T>().ToArray() : new T[0]; }
    }
    public class Shader : Object { public string AssetPath; public HashSet<string> Properties = new HashSet<string>(); }
    public class Texture : Object
    {
        // Minimal AI215 observation: zero JSON cache, same returned hash on both reads.
        // This is a boundary model, not Unity internals or its pixel hashing algorithm.
        public const string ObservedHash = "cd4e87eec3ba236414ab756fb86c7f93";
        public const string ZeroHash = "00000000000000000000000000000000";
        public int width = 2048, height = 2048, HashReads;
        public string CachedImageHash = ObservedHash, ContentHash = ObservedHash;
        public bool ObserveReads, Materialized = true;
        public readonly List<string> SamplingTrace = new List<string>();
        public readonly byte[] Pixels = { 0, 16, 128, 255 };
        public string imageContentsHash
        {
            get
            {
                HashReads++;
                if (ObserveReads) SamplingTrace.Add("hash");
                if (!Materialized) { CachedImageHash = ContentHash; Materialized = true; }
                return ContentHash;
            }
            set { ContentHash = value; }
        }
        public void MakeCold() { Materialized = false; CachedImageHash = ZeroHash; }
        public void ChangePixel()
        {
            Pixels[0]++;
            using (var hash = System.Security.Cryptography.MD5.Create())
                ContentHash = BitConverter.ToString(hash.ComputeHash(Pixels)).Replace("-", "").ToLowerInvariant();
            CachedImageHash = ContentHash;
        }
        public string SnapshotJson()
        {
            if (ObserveReads) SamplingTrace.Add("json");
            return "{\"Texture2D\":{\"name\":\"" + name + "\",\"width\":" + width + ",\"height\":" + height +
                ",\"m_ImageContentsHash\":{\"Hash\":\"" + CachedImageHash + "\"}}}";
        }
    }
    public class Material : Object
    {
        public Shader shader;
        public Dictionary<string, float> Floats = new Dictionary<string, float>();
        public Dictionary<string, Texture> Textures = new Dictionary<string, Texture>();
        public List<string> Trace = new List<string>();
        public static int Clones;
        public Material(Shader s) { shader = s; }
        public Material(Material other) { Clones++; shader = other.shader; name = other.name; CopyPropertiesFromMaterial(other); Trace.Clear(); }
        public void CopyPropertiesFromMaterial(Material other) { Floats = new Dictionary<string, float>(other.Floats); Textures = new Dictionary<string, Texture>(other.Textures); Trace.Add("copy"); }
        public bool HasProperty(string key) { return shader != null && shader.Properties.Contains(key); }
        public Texture GetTexture(string key) { return Textures.ContainsKey(key) ? Textures[key] : null; }
        public void SetTexture(string key, Texture value) { Textures[key] = value; Trace.Add(key); }
        public float GetFloat(string key) { return Floats.ContainsKey(key) ? Floats[key] : 0; }
        public void SetFloat(string key, float value) { Floats[key] = value; Trace.Add(key); }
    }
    public class Transform : Object
    {
        public Transform parent;
        public GameObject gameObject;
    }
    public class GameObject : Object
    {
        public Transform transform;
        public List<GameObject> Children = new List<GameObject>();
        public List<TMPro.TMP_Text> Texts = new List<TMPro.TMP_Text>();
        public GameObject(string n, GameObject parent = null)
        {
            name = n; transform = new Transform { name = n, gameObject = this, parent = parent == null ? null : parent.transform };
            if (parent != null) parent.Children.Add(this);
        }
        public T[] GetComponentsInChildren<T>(bool includeInactive) where T : class
        { return Texts.Cast<T>().Concat(Children.SelectMany(c => c.GetComponentsInChildren<T>(includeInactive))).ToArray(); }
    }
    public static class Debug { public static List<string> Messages = new List<string>(); public static void Log(object message) { Messages.Add(message.ToString()); } }
}
namespace TMPro
{
    public class TMP_FontAsset : Object { public Material material; public Texture atlasTexture; public Texture[] atlasTextures { get { return new[] { atlasTexture }; } } public string SerializedState = "approved font state"; }
    public class TMP_Text : Object
    {
        TMP_FontAsset currentFont;
        Material currentMaterial;
        public Transform transform;
        public string BusinessFields = "unchanged layout/text/size/color/sorting";
        public bool IsInstance;
        public int FontWrites, MaterialWrites;
        public static int FontLoads, DefaultFallbacks, PaddingCalls;
        public static Action<TMP_Text> AfterFont;
        public List<string> BindingTrace = new List<string>();
        public TMP_FontAsset font
        {
            get { return currentFont; }
            set
            {
                if (currentFont == value) return;
                currentFont = value; FontWrites++; FontLoads++;
                if (value != null && (currentMaterial == null || currentMaterial.GetTexture("_MainTex") == null || currentMaterial.GetTexture("_MainTex") != value.atlasTexture))
                { currentMaterial = value.material; DefaultFallbacks++; }
                BindingTrace.Add("font:" + (currentMaterial == null ? "null" : currentMaterial.name));
                Padding(); if (AfterFont != null) AfterFont(this);
            }
        }
        public Material fontSharedMaterial
        {
            get { return currentMaterial; }
            set { if (currentMaterial == value) return; currentMaterial = value; MaterialWrites++; BindingTrace.Add("material:" + value.name); Padding(); }
        }
        public void Padding() { PaddingCalls++; if (currentMaterial != null) ShaderUtilities.UpdateShaderRatios(currentMaterial); }
        // Arrangement simulates deserialization. Test actions still invoke actual production Bind/Run.
        public void Seed(TMP_FontAsset value, Material mat) { currentFont = value; currentMaterial = mat; FontWrites = MaterialWrites = 0; BindingTrace.Clear(); }
        public T[] GetComponents<T>() where T : class { return transform.gameObject.Texts.Cast<T>().ToArray(); }
    }
    public static class ShaderUtilities
    {
        public static int Calls;
        public static void UpdateShaderRatios(Material material)
        {
            Calls++; material.Trace.Add("ratios");
            // Observable shared writes and conditionals match TMP_ShaderUtilities.cs:277-343.
            // Numerical sentinel, NOT a claim of native ratio/GPU verification.
            material.Floats["_ScaleRatioA"] = material.GetFloat("_GradientScale") + material.GetFloat("_FaceDilate");
            if (material.HasProperty("_GlowOffset")) material.Floats["_ScaleRatioB"] = material.GetFloat("_GradientScale") + material.GetFloat("_FaceDilate");
            if (material.HasProperty("_UnderlayOffsetX")) material.Floats["_ScaleRatioC"] = material.GetFloat("_GradientScale") + material.GetFloat("_FaceDilate");
        }
    }
}
namespace UnityEngine.SceneManagement
{
    public class Scene
    {
        public string path;
        public bool isDirty, isLoaded;
        public GameObject[] Roots;
        public GameObject[] GetRootGameObjects() { return Roots; }
    }
    public static class SceneManager
    {
        public static Dictionary<string, Scene> Scenes = new Dictionary<string, Scene>();
        public static int sceneCount { get { return Scenes.Values.Count(s => s.isLoaded); } }
        public static Scene GetSceneAt(int i) { return Scenes.Values.Where(s => s.isLoaded).ElementAt(i); }
        public static Scene GetSceneByPath(string path) { return Scenes[path]; }
    }
}
namespace UnityEditor
{
    [AttributeUsage(AttributeTargets.Method)] public class MenuItem : Attribute { public MenuItem(string name) { } }
    public static class EditorApplication { public static bool isPlayingOrWillChangePlaymode, isCompiling; }
    public static class EditorUtility
    {
        public static HashSet<Object> Dirty = new HashSet<Object>();
        public static void SetDirty(Object o) { Dirty.Add(o); FontSwapOnceHarness.Emit("dirty", o); }
        public static bool IsDirty(Object o) { return Dirty.Contains(o); }
    }
    public static class AssetDatabase
    {
        public static Dictionary<string, Object> Objects = new Dictionary<string, Object>();
        public static Dictionary<string, string> Guids = new Dictionary<string, string>();
        public static List<string> Saved = new List<string>();
        public static string GUIDToAssetPath(string guid) { return Guids.Single(p => p.Value == guid).Key; }
        public static string AssetPathToGUID(string path) { return Guids[path]; }
        public static string GetAssetPath(Object value) { return Objects.FirstOrDefault(p => p.Value == value).Key ?? ""; }
        public static T LoadAssetAtPath<T>(string path) where T : Object { FontSwapOnceHarness.Emit("load:" + path, null); return Objects.ContainsKey(path) ? Objects[path] as T : null; }
        public static void SaveAssetIfDirty(Object o) { if (EditorUtility.IsDirty(o)) { Saved.Add(o.name); EditorUtility.Dirty.Remove(o); FontSwapOnceHarness.Emit("saved:material", o); } }
    }
    public struct GlobalObjectId
    {
        public string Id;
        public static Dictionary<string, Object> Objects = new Dictionary<string, Object>();
        public static bool TryParse(string text, out GlobalObjectId id) { id = new GlobalObjectId { Id = text }; return text.StartsWith("GlobalObjectId_V1-3-"); }
        public static Object GlobalObjectIdentifierToObjectSlow(GlobalObjectId id) { return Objects.ContainsKey(id.Id) ? Objects[id.Id] : null; }
    }
    public static class EditorJsonUtility
    {
        public static string ToJson(Object o)
        {
            var f = o as TMPro.TMP_FontAsset;
            if (f != null) return f.name + "|" + f.SerializedState + "|" + (f.material == null ? 0 : f.material.GetInstanceID()) + "|" + (f.atlasTexture == null ? 0 : f.atlasTexture.GetInstanceID());
            var texture = o as Texture;
            if (texture != null) return texture.SnapshotJson();
            var m = (Material)o;
            return m.name + "|" + m.shader.name + "|" + string.Join("|", m.Floats.OrderBy(p => p.Key).Select(p => p.Key + "=" + p.Value))
                + "|" + string.Join("|", m.Textures.OrderBy(p => p.Key).Select(p => p.Key + "=" + (p.Value == null ? "null" : p.Value.GetInstanceID().ToString())));
        }
    }
    public static class PrefabUtility
    {
        public static Dictionary<string, GameObject> Roots = new Dictionary<string, GameObject>();
        public static int Loads, Unloads, Overrides;
        public static bool IsPartOfPrefabInstance(Object o) { return ((TMPro.TMP_Text)o).IsInstance; }
        public static void RecordPrefabInstancePropertyModifications(Object o) { Overrides++; }
        public static GameObject LoadPrefabContents(string path) { Loads++; FontSwapOnceHarness.Emit("load:prefab", Roots[path]); return Roots[path]; }
        public static void UnloadPrefabContents(GameObject root) { Unloads++; }
        public static GameObject SaveAsPrefabAsset(GameObject root, string path, out bool success)
        { success = true; FontSwapOnceHarness.Saves.Add(path); FontSwapOnceHarness.Emit("saved:prefab", root); return root; }
    }
}
namespace UnityEditor.SceneManagement
{
    public enum OpenSceneMode { Additive }
    public static class PrefabStageUtility { public static object Stage; public static object GetCurrentPrefabStage() { return Stage; } }
    public static class EditorSceneManager
    {
        public static Scene OpenScene(string path, OpenSceneMode mode) { var s = SceneManager.Scenes[path]; s.isLoaded = true; FontSwapOnceHarness.Emit("load:scene", null); return s; }
        public static bool MarkSceneDirty(Scene scene) { scene.isDirty = true; FontSwapOnceHarness.Emit("dirty:scene", null); return true; }
        public static bool SaveScene(Scene scene) { FontSwapOnceHarness.Saves.Add(scene.path); scene.isDirty = false; FontSwapOnceHarness.Emit("saved:scene", null); return true; }
        public static bool CloseScene(Scene scene, bool remove) { scene.isLoaded = false; return true; }
    }
}
public static class FontSwapOnceHarness
{
    public static Action<string, Object> Injection;
    public static void Emit(string boundary, Object value) { if (Injection != null) Injection(boundary, value); }
    public static List<string> Saves = new List<string>();
    static Dictionary<string, TMPro.TMP_Text> texts;
    static FontSwapOnce.Row[] rows;
    static Material[] targets, sources;
    static TMPro.TMP_FontAsset font;
    static int assertions, negativeCases;
    static Shader standard, mobile;
    static List<string> shaderCases = new List<string>();
    static void Check(bool ok, string message) { assertions++; if (!ok) throw new Exception(message); }
    static void Reject(Action action, string message)
    {
        bool failed = false;
        try { action(); } catch (InvalidOperationException) { failed = true; }
        Check(failed, message); negativeCases++;
    }
    static void Register(string path, string guid, Object value) { AssetDatabase.Objects[path] = value; AssetDatabase.Guids[path] = guid; }
    static Shader LoadShader(string path, string guid)
    {
        string source = File.ReadAllText(path);
        var shader = new Shader { name = Regex.Match(source, "Shader \"([^\"]+)\"").Groups[1].Value, AssetPath = path };
        foreach (Match match in Regex.Matches(source.Substring(0, source.IndexOf("SubShader", StringComparison.Ordinal)), @"(?m)^\s*(_\w+)\s*\(")) shader.Properties.Add(match.Groups[1].Value);
        Register(path, guid, shader);
        return shader;
    }
    static void Setup()
    {
        Injection = null; TMPro.TMP_Text.AfterFont = null;
        // Each Setup is an independent simulated Editor domain; production exposes no latch reset.
        foreach (var type in new[] { typeof(FontSwapOnce), typeof(FontSwapOnceOldOrder), typeof(FontSwapOnceOldSnapshot) })
            type.GetField("staticGuardFailed", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).SetValue(null, false);
        EatWhat.EditorTools.Fonts.File.OnRead = null; EatWhat.EditorTools.Fonts.File.DiskBytes = EatWhat.EditorTools.Fonts.File.ApprovedBytes;
        EatWhat.EditorTools.Fonts.File.Reads = 0; Resources.StaticLoaded = true;
        TMPro.TMP_Text.FontLoads = TMPro.TMP_Text.DefaultFallbacks = TMPro.TMP_Text.PaddingCalls = 0;
        AssetDatabase.Objects.Clear(); AssetDatabase.Guids.Clear(); AssetDatabase.Saved.Clear(); GlobalObjectId.Objects.Clear();
        EditorUtility.Dirty.Clear(); SceneManager.Scenes.Clear(); PrefabUtility.Roots.Clear(); Saves.Clear(); Debug.Messages.Clear();
        EditorApplication.isPlayingOrWillChangePlaymode = false; EditorApplication.isCompiling = false;
        UnityEditor.SceneManagement.PrefabStageUtility.Stage = null; PrefabUtility.Loads = PrefabUtility.Unloads = PrefabUtility.Overrides = 0;
        standard = LoadShader("Assets/TextMesh Pro/Shaders/TMP_SDF.shader", "68e6db2ebdc24f95958faec2be5558d6");
        mobile = LoadShader("Assets/TextMesh Pro/Shaders/TMP_SDF-Mobile.shader", "fe393ace9b354375a9cb14cdbbc28be4");
        var atlas = new Material(mobile) { name = "Static default" };
        atlas.Textures["_MainTex"] = new Texture { name = "Static atlas" };
        atlas.Floats["_TextureWidth"] = 2048; atlas.Floats["_TextureHeight"] = 2048; atlas.Floats["_GradientScale"] = 10;
        atlas.Floats["_FaceDilate"] = 0;
        foreach (var key in new[] { "_ScaleRatioA", "_ScaleRatioB", "_ScaleRatioC" }) atlas.Floats[key] = 1;
        font = new TMPro.TMP_FontAsset { name = "Static", material = atlas, atlasTexture = atlas.GetTexture("_MainTex") };
        Register(FontSwapOnce.StaticPath, FontSwapOnce.StaticGuid, font);
        targets = new Material[3]; sources = new Material[3];
        string[] guids = { "8e525bf3cdd33804189d28645ca0f556", "87a7857d7b1355641aee5b8ac50c5c1b", "389683e661c8d24468fea402edba0841" };
        string[] names = { "SourceHan", "LegacyShortCycle", "Heavy" };
        string[] ids = { "GlobalObjectId_V1-3-53ffd732c8bc2034f93d5a714c25eda2-12569091968840867831-0", "GlobalObjectId_V1-3-75909fc3d0fa2c94baa5706654d37655-9052723641695353799-0", "GlobalObjectId_V1-3-ce30f77e3b8c0ad45b7b7ae26b956bb6-18028800558602920526-0" };
        for (int i = 0; i < 3; i++)
        {
            sources[i] = new Material(atlas) { name = names[i] + " source", shader = standard };
            sources[i].Textures["_MainTex"] = new Texture { name = names[i] + " old atlas" };
            sources[i].Textures["_FaceTex"] = new Texture { name = names[i] + " style texture" };
            foreach (var key in new[] { "_FaceDilate", "_OutlineWidth", "_UnderlayOffsetX", "_UnderlayOffsetY", "_UnderlaySoftness", "_WeightBold" }) sources[i].Floats[key] = i + 0.125f;
            sources[i].Floats["_GradientScale"] = 4 + i;
            GlobalObjectId.Objects[ids[i]] = sources[i];
            targets[i] = new Material(atlas) { name = "ShortCycle_Static_From" + names[i], shader = standard };
            Register("Assets/Font/Heavy/Materials/" + targets[i].name + ".mat", guids[i], targets[i]);
        }
        var legacy = new TMPro.TMP_FontAsset { name = "Legacy", material = sources[1], atlasTexture = sources[1].GetTexture("_MainTex") };
        var heavy = new TMPro.TMP_FontAsset { name = "Heavy", material = sources[2], atlasTexture = sources[2].GetTexture("_MainTex") };
        Register("legacy.asset", "75909fc3d0fa2c94baa5706654d37655", legacy);
        Register("heavy.asset", "ce30f77e3b8c0ad45b7b7ae26b956bb6", heavy);
        texts = new Dictionary<string, TMPro.TMP_Text>();
        foreach (var group in rows.GroupBy(r => r.Asset))
        {
            var roots = new Dictionary<string, GameObject>();
            foreach (var row in group)
            {
                GameObject current = null;
                foreach (var name in row.Path.Split('/'))
                {
                    var found = current == null ? (roots.ContainsKey(name) ? roots[name] : null) : current.Children.SingleOrDefault(c => c.name == name);
                    if (found == null) { found = new GameObject(name, current); if (current == null) roots[name] = found; }
                    current = found;
                }
                var text = new TMPro.TMP_Text { name = current.name, transform = current.transform, IsInstance = row.Asset.EndsWith(".unity") };
                text.Seed(row.BeforeFont == "ce30f77e3b8c0ad45b7b7ae26b956bb6" ? heavy : legacy, sources[row.Style]);
                current.Texts.Add(text); texts[row.Key] = text;
            }
            if (group.Key.EndsWith(".unity")) SceneManager.Scenes[group.Key] = new Scene { path = group.Key, Roots = roots.Values.ToArray() };
            else PrefabUtility.Roots[group.Key] = roots.Values.Single();
        }
        Material.Clones = Object.Destroyed = 0;
        if (standard.Properties.Count != 66 || mobile.Properties.Count != 36)
        {
            throw new Exception("Unexpected checked-in Shader property counts");
        }
    }
    static void RejectPreflight(string label, Action arrange, string expectedMessage)
    {
        Setup();
        arrange();
        int dirty = EditorUtility.Dirty.Count;
        string diagnostic = "";
        try { FontSwapOnce.Run(false); }
        catch (InvalidOperationException ex) { diagnostic = ex.Message; }
        Check(diagnostic.Contains(expectedMessage), label + ": wrong/missing diagnostic: " + diagnostic);
        Check(Saves.Count == 0 && AssetDatabase.Saved.Count == 0, label + ": saved before rejecting");
        Check(texts.Values.All(t => t.FontWrites == 0 && t.MaterialWrites == 0), label + ": changed a binding");
        Check(EditorUtility.Dirty.Count == dirty, label + ": changed dirty state");
        Check(PrefabUtility.Loads == PrefabUtility.Unloads && SceneManager.sceneCount == 0, label + ": leaked loaded contents");
        Check(Material.Clones == Object.Destroyed, label + ": leaked candidate material");
        negativeCases++;
        shaderCases.Add(label);
    }
    static void CheckShaderContracts()
    {
        shaderCases.Clear();
        foreach (string combination in new[] { "Standard/Mobile", "Standard/Standard", "Mobile/Mobile" })
        {
            Setup();
            if (combination == "Standard/Standard") font.material.shader = standard;
            if (combination == "Mobile/Mobile") foreach (var m in targets.Concat(sources)) m.shader = mobile;
            Shader[] originalShaders = targets.Select(m => m.shader).ToArray();
            var originalGuids = AssetDatabase.Guids.ToDictionary(p => p.Key, p => p.Value);
            string[] sourceBefore = sources.Select(EditorJsonUtility.ToJson).ToArray();
            string atlasBefore = EditorJsonUtility.ToJson(font.material);
            Check(FontSwapOnce.Run(true) == 171, combination + ": dry run");
            Check(Saves.Count == 0 && AssetDatabase.Saved.Count == 0 && EditorUtility.Dirty.Count == 0, combination + ": dry run changed assets");
            Check(FontSwapOnce.Run(false) == 171, combination + ": execution");
            Check(FontSwapOnce.Run(false) == 0, combination + ": idempotence");
            Check(targets.Select(m => m.shader).SequenceEqual(originalShaders), combination + ": target Shader changed");
            Check(originalGuids.All(p => AssetDatabase.Guids[p.Key] == p.Value), combination + ": GUID changed");
            Check(sources.Select(EditorJsonUtility.ToJson).SequenceEqual(sourceBefore) && EditorJsonUtility.ToJson(font.material) == atlasBefore, combination + ": source/Static changed");
            Check(Material.Clones == Object.Destroyed, combination + ": leaked candidates");
            shaderCases.Add(combination);
        }
        foreach (string role in new[] { "target", "source", "atlas" })
        {
            foreach (bool missing in new[] { false, true })
            {
                RejectPreflight(role + (missing ? " null Shader" : " unknown Shader"), () =>
                {
                    var m = role == "target" ? targets[2] : role == "source" ? sources[2] : font.material;
                    m.shader = missing ? null : new Shader { name = "TextMeshPro/Unregistered" };
                }, "unsupported/null shader");
            }
            RejectPreflight(role + " wrong Shader GUID", () =>
            {
                var m = role == "target" ? targets[2] : role == "source" ? sources[2] : font.material;
                AssetDatabase.Guids[m.shader.AssetPath] = "wrong-shader-guid";
            }, "shader GUID mismatch");
        }
        RejectPreflight("source-target mismatch", () => sources[2].shader = mobile, "Source/target shader mismatch");
        RejectPreflight("unregistered reverse pair", () =>
        {
            font.material.shader = standard;
            foreach (var m in targets.Concat(sources)) m.shader = mobile;
        }, "Unsupported source/Static shader pair");
        RejectPreflight("source dirty", () => EditorUtility.SetDirty(sources[2]), "Source Heavy: material dirty");
        RejectPreflight("target dirty", () => EditorUtility.SetDirty(targets[2]), "Target Heavy: material dirty");
        RejectPreflight("atlas material dirty", () => EditorUtility.SetDirty(font.material), "Static atlas material: material dirty");
        RejectPreflight("atlas texture dirty", () => EditorUtility.SetDirty(font.atlasTexture), "Static atlas texture dirty");
        RejectPreflight("atlas material missing", () => font.material = null, "Static atlas material: material missing");
        RejectPreflight("atlas missing", () => font.material.Textures["_MainTex"] = null, "Static atlas texture missing/mismatched");
        RejectPreflight("font atlas missing", () => font.atlasTexture = null, "Static atlas texture missing/mismatched");
        RejectPreflight("atlas identity mismatch", () => font.atlasTexture = new Texture(), "Static atlas texture missing/mismatched");
        foreach (float value in new[] { 0f, -1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
            RejectPreflight("invalid gradient " + value, () => font.material.Floats["_GradientScale"] = value, "GradientScale must be finite and positive");
        foreach (string dimension in new[] { "_TextureWidth", "_TextureHeight" })
            foreach (float value in new[] { 0f, -1f, 1024f, float.NaN, float.PositiveInfinity })
                RejectPreflight("invalid " + dimension + " " + value, () => font.material.Floats[dimension] = value, "Static atlas dimensions mismatch/invalid");
        RejectPreflight("empty atlas width", () => font.atlasTexture.width = 0, "Static atlas dimensions mismatch/invalid");
        RejectPreflight("empty atlas height", () => font.atlasTexture.height = 0, "Static atlas dimensions mismatch/invalid");
        Setup();
        string[] standardProperties = standard.Properties.ToArray(), mobileProperties = mobile.Properties.ToArray();
        // Property sets come from the checked-in shaders, not a duplicate of the production lists.
        foreach (string key in standardProperties)
            RejectPreflight("standard missing " + key, () => standard.Properties.Remove(key), "missing property " + key);
        foreach (string key in mobileProperties)
            RejectPreflight("atlas missing " + key, () => mobile.Properties.Remove(key), "missing property " + key);
        foreach (string key in mobileProperties)
            RejectPreflight("mobile target missing " + key, () =>
            {
                font.material.shader = standard;
                foreach (var m in targets.Concat(sources)) m.shader = mobile;
                mobile.Properties.Remove(key);
            }, "Target SourceHan: missing property " + key);
    }

    static readonly List<string> guardCases = new List<string>();
    static readonly List<string> boundaryEvidence = new List<string>();
    static int CountSaves() { return Saves.Count + AssetDatabase.Saved.Count; }
    static void AssertReleased(string label)
    {
        Check(PrefabUtility.Loads == PrefabUtility.Unloads, label + ": leaked prefab contents");
        Check(Material.Clones == Object.Destroyed, label + ": leaked candidate materials");
    }
    static string GuardReject(Action action, string diagnostic)
    {
        string message = "";
        try { action(); } catch (InvalidOperationException ex) { message = ex.Message; }
        Check(message.Contains(diagnostic), "Guard did not reject with " + diagnostic + ": " + message);
        negativeCases++;
        return message;
    }
    static void RecordGuard(string label)
    {
        guardCases.Add(label);
        boundaryEvidence.Add("{\"case\":\"" + label + "\",\"saves\":" + CountSaves() +
            ",\"diskMatchesApproved\":" + EatWhat.EditorTools.Fonts.File.DiskBytes.SequenceEqual(EatWhat.EditorTools.Fonts.File.ApprovedBytes).ToString().ToLowerInvariant() +
            ",\"defaultRatioA\":" + font.material.GetFloat("_ScaleRatioA").ToString(System.Globalization.CultureInfo.InvariantCulture) + "}");
    }
    static void Drift(string kind)
    {
        if (kind == "disk") EatWhat.EditorTools.Fonts.File.DiskBytes = new byte[] { 0, 1, 2 };
        else if (kind == "font") font.SerializedState = "unexpected serialized font change";
        else if (kind == "atlas") font.atlasTexture.imageContentsHash = "unexpected pixel change";
        else if (kind == "material") font.material.Floats["_FaceDilate"] = 0.25f;
        else TMPro.ShaderUtilities.UpdateShaderRatios(font.material);
    }
    static void CheckStaticGuards()
    {
        guardCases.Clear(); boundaryEvidence.Clear();
        Setup(); Resources.StaticLoaded = false;
        var firstLoad = FontSwapOnce.BeginStaticGuard();
        Check(!firstLoad.WasAlreadyLoaded, "first load availability must be observable");
        firstLoad.Check("clean first load"); RecordGuard("clean-first-load");
        Setup();
        GuardReject(() => FontSwapOnce.BeginIsolatedStaticGuard(FontSwapOnce.StaticPath, "fixture-guid"), "isolated fixture");
        GuardReject(() => FontSwapOnce.BeginIsolatedStaticGuard("Assets/Tests/FontSwapOnce/../wrong.asset", "fixture-guid"), "isolated fixture");
        GuardReject(() => FontSwapOnce.BeginIsolatedStaticGuard("Assets/Tests/FontSwapOnce/Static.asset", FontSwapOnce.StaticGuid), "isolated fixture");
        string isolatedPath = "Assets/Tests/FontSwapOnce/Static.asset";
        AssetDatabase.Objects.Remove(FontSwapOnce.StaticPath); AssetDatabase.Guids.Remove(FontSwapOnce.StaticPath);
        Register(isolatedPath, "fixture-guid", font);
        var isolated = FontSwapOnce.BeginIsolatedStaticGuard(isolatedPath, "fixture-guid");
        isolated.Check("isolated clean"); RecordGuard("isolated-approved-bytes-only");
        Setup();
        var identity = FontSwapOnce.BeginStaticGuard();
        font.material = new Material(font.material);
        GuardReject(() => identity.Check("material identity swap"), "Static memory changed");
        Object.DestroyImmediate(font.material);
        RecordGuard("material-identity-drift");

        Setup();
        EatWhat.EditorTools.Fonts.File.DiskBytes = new byte[] { 7 };
        int loads = 0; Injection = (b, o) => { if (b.StartsWith("load:")) loads++; };
        GuardReject(() => FontSwapOnce.Run(false), "Static disk SHA mismatch");
        Check(loads == 0 && CountSaves() == 0, "entry disk mismatch must stop before any load/save");
        RecordGuard("entry-disk-before-load");
        Setup(); Drift("ratio");
        GuardReject(() => FontSwapOnce.BeginStaticGuard(), "approved ratio mismatch");
        Check(CountSaves() == 0, "preloaded ratio contamination was adopted");
        RecordGuard("preloaded-memory-not-rebaselined");
        foreach (bool wasLoaded in new[] { true, false })
        {
            Setup(); Resources.StaticLoaded = wasLoaded;
            Injection = (b, o) => { if (b == "load:" + FontSwapOnce.StaticPath) Drift("ratio"); };
            GuardReject(() => FontSwapOnce.Run(false), "approved ratio mismatch");
            Check(CountSaves() == 0 && !EditorUtility.IsDirty(font.material), "font load memory-only failure was saved");
            RecordGuard(wasLoaded ? "font-reload-drift" : "first-font-load-not-rebaselined");
        }
        foreach (string boundary in new[] { "load:scene", "load:prefab" })
        {
            Setup();
            Injection = (b, o) => { if (b == boundary) Drift("ratio"); };
            GuardReject(() => FontSwapOnce.Run(false), "after load ");
            Check(CountSaves() == 0 && !EditorUtility.IsDirty(font.material), boundary + ": memory-only drift did not stop");
            AssertReleased(boundary); RecordGuard(boundary);
        }
        foreach (string kind in new[] { "ratio", "font", "material", "atlas", "disk" })
        {
            Setup(); var guard = FontSwapOnce.BeginStaticGuard();
            GuardReject(() => guard.Around("controlled render", () => Drift(kind)), "after controlled render");
            Check(CountSaves() == 0, "render guard unexpectedly saved");
            GuardReject(() => guard.Around("later save", () => Saves.Add("forbidden")), "already failed");
            GuardReject(() => FontSwapOnce.BeginStaticGuard(), "already failed");
            Check(CountSaves() == 0, "failed guard allowed rebaseline/next save");
            RecordGuard("render-" + kind);
        }
        Setup();
        FontSwapOnce.PrepareMaterial(targets[0], sources[0], font.material);
        var oldGuard = FontSwapOnce.BeginStaticGuard();
        string oldSnapshot = EditorJsonUtility.ToJson(font.material);
        FontSwapOnceOldOrder.Bind(texts[rows[0].Key], font, targets[0]);
        Check(TMPro.TMP_Text.DefaultFallbacks > 0 && TMPro.TMP_Text.PaddingCalls >= 2, "old-order negative did not traverse native semantics");
        Check(EditorJsonUtility.ToJson(font.material) != oldSnapshot && !EditorUtility.IsDirty(font.material), "old order did not expose shared memory-only pollution");
        GuardReject(() => oldGuard.Check("old binding"), "approved ratio mismatch");
        RecordGuard("old-production-order-pollutes");
        Setup();
        FontSwapOnce.PrepareMaterial(targets[rows[0].Style], sources[rows[0].Style], font.material);
        var newGuard = FontSwapOnce.BeginStaticGuard(); var target = texts[rows[0].Key];
        newGuard.Around("new binding", () => Check(FontSwapOnce.Bind(target, font, targets[rows[0].Style]), "new binding skipped"));
        Check(TMPro.TMP_Text.FontLoads == 1 && TMPro.TMP_Text.PaddingCalls == 2 && TMPro.TMP_Text.DefaultFallbacks == 0, "new Bind did not exercise material/atlas/font/padding");
        Check(target.BindingTrace[0].StartsWith("material:") && target.BindingTrace[1].StartsWith("font:ShortCycle_Static_From"), "new intermediate binding path wrong");
        RecordGuard("new-production-bind-protects");
        Setup();
        GuardReject(() => FontSwapOnceOldOrder.Run(false), "after binding");
        Check(CountSaves() == 3 && Saves.Count == 0, "old Run did not stop before scene/prefab save");
        AssertReleased("old Run"); RecordGuard("old-production-run-rejected");
        Setup();
        TMPro.TMP_Text.AfterFont = t => Drift("ratio");
        GuardReject(() => FontSwapOnce.Run(false), "after binding");
        Check(CountSaves() == 3 && Saves.Count == 0, "binding injection continued saving");
        AssertReleased("binding"); RecordGuard("binding-memory-only-drift");
        Setup();
        Injection = (b, o) => { if (b == "dirty" && o == targets[0]) Drift("ratio"); };
        GuardReject(() => FontSwapOnce.Run(false), "before material save");
        Check(CountSaves() == 0, "material pre-save drift not blocked");
        AssertReleased("material pre-save"); RecordGuard("before-material-save-memory-drift");
        Setup();
        Injection = (b, o) => { if (b == "dirty:scene") Drift("font"); };
        GuardReject(() => FontSwapOnce.Run(false), "before scene save");
        Check(CountSaves() == 3 && SceneManager.Scenes.Values.Single().isLoaded && SceneManager.Scenes.Values.Single().isDirty, "scene pre-save must retain unknown dirty scene");
        AssertReleased("scene pre-save"); RecordGuard("before-scene-save-memory-drift");

        // Obtain the exact guard-read position of each actual save from an unmodified production Run.
        Setup(); var saveReads = new List<int>(); var saveKinds = new List<string>();
        Injection = (b, o) => { if (b.StartsWith("saved:")) { saveReads.Add(EatWhat.EditorTools.Fonts.File.Reads); saveKinds.Add(b.Substring(6)); } };
        Check(FontSwapOnce.Run(false) == 171 && saveReads.Count == 9, "save-boundary reference run");
        AssertReleased("save-boundary reference");
        for (int saveIndex = 0; saveIndex < saveReads.Count; saveIndex++)
        {
            foreach (string kind in new[] { "ratio", "disk" })
            {
                Setup(); int atRead = saveReads[saveIndex];
                EatWhat.EditorTools.Fonts.File.OnRead = () => { if (EatWhat.EditorTools.Fonts.File.Reads == atRead) Drift(kind); };
                GuardReject(() => FontSwapOnce.Run(false), "before " + saveKinds[saveIndex] + " save");
                Check(CountSaves() == saveIndex, "pre-save drift allowed the current/next save");
                AssertReleased("pre-save"); RecordGuard("pre-save-" + saveIndex + "-" + kind);
                Setup(); int seen = 0;
                Injection = (b, o) => { if (b.StartsWith("saved:") && seen++ == saveIndex) Drift(kind); };
                GuardReject(() => FontSwapOnce.Run(false), "after " + saveKinds[saveIndex] + " save");
                Check(CountSaves() == saveIndex + 1, "post-save drift allowed a subsequent save");
                AssertReleased("post-save"); RecordGuard("post-save-" + saveIndex + "-" + kind);
            }
        }
        Setup(); var cleanGuard = FontSwapOnce.BeginStaticGuard();
        cleanGuard.Around("clean render", () => { });
        Check(FontSwapOnce.Run(false) == 171 && FontSwapOnce.Run(false) == 0, "clean guarded run/idempotence");
        cleanGuard.Check("after production Run");
        Check(PrefabUtility.Overrides == 161 && TMPro.TMP_Text.DefaultFallbacks == 0, "prefab overrides/default fallback regression");
        AssertReleased("clean");
        RecordGuard("clean-render-full-run-repeat");
        Setup(); foreach (var row in rows.Take(60)) texts[row.Key].Seed(font, targets[row.Style]);
        Check(FontSwapOnce.Run(false) == 111 && TMPro.TMP_Text.FontLoads == 111 && TMPro.TMP_Text.DefaultFallbacks == 0, "partial migration bypassed changed bindings");
        RecordGuard("partial-real-binding-111");
        Setup(); var unknown = SceneManager.Scenes.Values.Single(); unknown.isDirty = unknown.isLoaded = true;
        GuardReject(() => FontSwapOnce.Run(false), "Unsaved scene");
        Check(unknown.isDirty && unknown.isLoaded && CountSaves() == 0, "unknown dirty scene was saved/discarded");
        RecordGuard("unknown-dirty-retained");
    }

    static readonly List<string> snapshotEvidence = new List<string>();
    static void CheckSampling(Texture texture, int samples, string order, string label)
    {
        Check(texture.HashReads == samples, label + ": extra getter/prewarm/rebaseline");
        Check(string.Join(",", texture.SamplingTrace) == string.Join(",", Enumerable.Repeat(order, samples)), label + ": wrong per-Memory sampling order/count");
        snapshotEvidence.Add("{\"case\":\"" + label + "\",\"memorySamples\":" + samples + ",\"hashReads\":" + texture.HashReads +
            ",\"trace\":\"" + string.Join(",", texture.SamplingTrace) + "\"}");
    }
    static void CheckColdSnapshots()
    {
        snapshotEvidence.Clear();
        Setup(); Resources.StaticLoaded = false;
        var oldTexture = font.atlasTexture; oldTexture.MakeCold(); oldTexture.ObserveReads = true;
        Check(oldTexture.CachedImageHash == Texture.ZeroHash && oldTexture.HashReads == 0, "old cold input was warmed");
        GuardReject(() => FontSwapOnceOldSnapshot.BeginStaticGuard(), "Static guard failed at after Static load: Static memory changed");
        CheckSampling(oldTexture, 2, "json,hash", "cold-old-order-rejects-first-check");
        GuardReject(() => FontSwapOnceOldSnapshot.BeginStaticGuard(), "already failed");
        Check(oldTexture.HashReads == 2 && CountSaves() == 0, "old failure retried or wrote");
        foreach (bool wasLoaded in new[] { false, true })
        {
            Setup(); Resources.StaticLoaded = wasLoaded;
            var texture = font.atlasTexture; texture.MakeCold(); texture.ObserveReads = true;
            Check(texture.CachedImageHash == Texture.ZeroHash && texture.HashReads == 0, "new cold input was warmed");
            var guard = FontSwapOnce.BeginStaticGuard();
            Check(guard.WasAlreadyLoaded == wasLoaded && texture.CachedImageHash == Texture.ObservedHash, "initial snapshot did not retain observable load/cache state");
            int initialSamples = wasLoaded ? 3 : 2;
            CheckSampling(texture, initialSamples, "hash,json", wasLoaded ? "loaded-object-cold-cache" : "cold-new-order-passes");
            guard.Check("stable later sample");
            guard.Around("stable render", () => { });
            CheckSampling(texture, initialSamples + 3, "hash,json", wasLoaded ? "loaded-later-checks" : "cold-later-checks");
            Check(CountSaves() == 0 && EditorUtility.Dirty.Count == 0, "sampling wrote or marked dirty");
        }
        // Already materialized objects remain accepted by both real production variants.
        Setup(); var stableTexture = font.atlasTexture; stableTexture.ObserveReads = true;
        var oldStable = FontSwapOnceOldSnapshot.BeginStaticGuard();
        Check(oldStable.WasAlreadyLoaded, "old stable case not loaded");
        CheckSampling(stableTexture, 3, "json,hash", "loaded-stable-old-order");
        Setup(); var newStable = font.atlasTexture; newStable.ObserveReads = true;
        var stable = FontSwapOnce.BeginStaticGuard();
        Check(stable.WasAlreadyLoaded, "new stable case not loaded");
        CheckSampling(newStable, 3, "hash,json", "loaded-stable-new-order");

        // No field is exempt after initialization, even if only the cached JSON hash changes.
        foreach (string kind in new[] { "pixels", "imageHash", "jsonHash", "jsonHashZero", "textureMetadata", "fontJson", "materialJson", "fontIdentity", "materialIdentity", "textureIdentity" })
        {
            Setup(); Resources.StaticLoaded = false;
            var texture = font.atlasTexture; texture.MakeCold(); texture.ObserveReads = true;
            var guard = FontSwapOnce.BeginStaticGuard();
            if (kind == "pixels") { texture.ChangePixel(); Check(texture.Pixels[0] == 1 && texture.ContentHash != Texture.ObservedHash, "pixel mutation ineffective"); }
            if (kind == "imageHash") { texture.imageContentsHash = "changed-independent-image-hash"; Check(texture.CachedImageHash == Texture.ObservedHash, "independent hash also changed JSON"); }
            if (kind == "jsonHash" || kind == "jsonHashZero")
            {
                texture.CachedImageHash = kind == "jsonHashZero" ? Texture.ZeroHash : "changed-json-only-hash";
                Check(texture.ContentHash == Texture.ObservedHash, "JSON-only mutation changed independent hash");
            }
            if (kind == "textureMetadata") texture.height++;
            if (kind == "fontJson") font.SerializedState = "changed-font-json";
            if (kind == "materialJson") font.material.Floats["_FaceDilate"] = 0.2f;
            if (kind == "fontIdentity")
            {
                var replacement = new TMPro.TMP_FontAsset { name = font.name, material = font.material, atlasTexture = texture, SerializedState = font.SerializedState };
                Check(EditorJsonUtility.ToJson(replacement) == EditorJsonUtility.ToJson(font), "font identity mutant changed JSON");
                typeof(FontSwapOnce.StaticGuard).GetProperty("Font").SetValue(guard, replacement, null); // test-only fault injection
            }
            if (kind == "materialIdentity") font.material = new Material(font.material);
            if (kind == "textureIdentity")
            {
                var replacement = new Texture { name = texture.name, ObserveReads = true };
                Check(replacement.SnapshotJson() == texture.SnapshotJson(), "texture identity mutant changed JSON");
                replacement.SamplingTrace.Clear(); texture.SamplingTrace.Clear();
                font.atlasTexture = replacement; font.material.Textures["_MainTex"] = replacement;
            }
            string reason = GuardReject(() => guard.Check("later " + kind), "Static memory changed: later " + kind);
            int reads = font.atlasTexture.HashReads;
            GuardReject(() => guard.Around("forbidden save", () => Saves.Add("forbidden")), "already failed");
            GuardReject(() => FontSwapOnce.BeginStaticGuard(), "already failed");
            Check(font.atlasTexture.HashReads == reads && CountSaves() == 0 && EditorUtility.Dirty.Count == 0, kind + ": failure retried/rebaselined/saved");
            snapshotEvidence.Add("{\"case\":\"later-" + kind + "\",\"rejected\":true,\"latched\":true,\"saves\":0}");
            if (kind == "materialIdentity") Object.DestroyImmediate(font.material);
        }
    }

    public static string Run(string csv)
    {
        assertions = negativeCases = 0;
        EatWhat.EditorTools.Fonts.File.ApprovedBytes = File.ReadAllBytes(".git/lfs/objects/e8/77/e877950344a043b02be025b7c2e12e2889e9ec69d20e72a3c90785f5b8ee1220");
        rows = FontSwapOnce.ReadPlan(csv);
        Check(rows.Length == 171, "production ReadPlan must return 171");
        Check(rows.Count(r => r.Style == 0) == 86 && rows.Count(r => r.Style == 1) == 77 && rows.Count(r => r.Style == 2) == 8, "actual source category counts");
        var lines = csv.TrimEnd().Split('\n'); var duplicate = lines[1].TrimEnd('\r');
        Check(FontSwapOnce.ReadPlan(csv.TrimEnd() + "\n" + duplicate).Length == 171, "identical duplicate collapsed");
        Reject(() => FontSwapOnce.ReadPlan(string.Join("\n", lines.Take(lines.Length - 1))), "missing mapping rejected");
        Reject(() => FontSwapOnce.ReadPlan(csv.Replace("GlobalObjectId_V1-3-53ffd", "GlobalObjectId_V1-3-03ffd")), "source ambiguity rejected");
        Reject(() => FontSwapOnce.ReadPlan(csv.Replace("Spaces/Phase0_RecipeBook/Cover_Group/CoverTitle", "Spaces/Unknown")), "missing CoverTitle rejected");
        Setup();
        Check(FontSwapOnce.Run(true) == 171 && Saves.Count == 0 && AssetDatabase.Saved.Count == 0 && EditorUtility.Dirty.Count == 0, "dry run must not save/set dirty");
        Check(PrefabUtility.Loads == PrefabUtility.Unloads && SceneManager.sceneCount == 0, "dry-run resources released");
        Check(FontSwapOnce.Run(false) == 171, "first production run binds 171");
        Check(Saves.Count == 6 && Saves[0].EndsWith(".unity") && AssetDatabase.Saved.Count == 3, "instances saved before sources, exactly existing three materials");
        for (int i = 0; i < 3; i++)
        {
            Check(targets[i].GetTexture("_MainTex") == font.material.GetTexture("_MainTex"), "static atlas");
            Check(targets[i].GetTexture("_FaceTex") == sources[i].GetTexture("_FaceTex"), "style texture");
            foreach (var key in new[] { "_TextureWidth", "_TextureHeight", "_GradientScale" }) Check(targets[i].GetFloat(key) == font.material.GetFloat(key), "atlas dimensions/gradient");
            foreach (var key in new[] { "_FaceDilate", "_OutlineWidth", "_UnderlayOffsetX", "_UnderlayOffsetY", "_UnderlaySoftness", "_WeightBold" }) Check(targets[i].GetFloat(key) == sources[i].GetFloat(key), "source style " + key);
            var probe = new Material(targets[i]);
            FontSwapOnce.PrepareMaterial(probe, sources[i], font.material);
            Check(string.Join(",", probe.Trace) == "copy,_MainTex,_TextureWidth,_TextureHeight,_GradientScale,ratios", "copy -> atlas -> ratios order");
            Check(targets[i].GetFloat("_ScaleRatioA") == 10 + sources[i].GetFloat("_FaceDilate"), "ratios observe rebound atlas");
            Object.DestroyImmediate(probe);
        }
        foreach (var row in rows)
        {
            var tmp = texts[row.Key];
            Check(tmp.font == font && tmp.fontSharedMaterial == targets[row.Style], "font/material pairing for " + row.Key);
            Check(tmp.FontWrites == 1 && tmp.MaterialWrites == 1 && tmp.BusinessFields == "unchanged layout/text/size/color/sorting", "only one public binding pair");
        }
        int saves = Saves.Count + AssetDatabase.Saved.Count;
        Check(FontSwapOnce.Run(false) == 0 && Saves.Count + AssetDatabase.Saved.Count == saves, "second identical run has zero additional saves/bindings");
        Check(Debug.Messages.Any(m => m.StartsWith("CoverTitle:") && m.EndsWith("/ FromHeavy")), "CoverTitle explicitly reported");
        Setup();
        foreach (var row in rows.Take(60)) texts[row.Key].Seed(font, targets[row.Style]);
        Check(FontSwapOnce.Run(false) == 111 && FontSwapOnce.Run(false) == 0, "partially migrated objects preserve original CSV categories");
        foreach (var scenario in new[] { "dirtyScene", "dirtyMaterial", "dirtyStatic", "missingMaterial", "wrongGuid", "missingSource", "missingObject", "unknownFont", "unknownMaterial", "wrongStaticCategory", "duplicateObject", "play", "compile", "prefabStage" })
        {
            Setup();
            var first = texts[rows[0].Key]; var last = texts[rows[rows.Length - 1].Key];
            if (scenario == "dirtyScene") { var scene = SceneManager.Scenes.Values.Single(); scene.isDirty = scene.isLoaded = true; }
            if (scenario == "dirtyMaterial") EditorUtility.SetDirty(targets[2]);
            if (scenario == "dirtyStatic") EditorUtility.SetDirty(font);
            if (scenario == "missingMaterial") AssetDatabase.Objects.Remove(AssetDatabase.GUIDToAssetPath("389683e661c8d24468fea402edba0841"));
            if (scenario == "wrongGuid") AssetDatabase.Guids["Assets/Font/Heavy/Materials/ShortCycle_Static_FromHeavy.mat"] = "wrong";
            if (scenario == "missingSource") GlobalObjectId.Objects.Clear();
            if (scenario == "missingObject") last.transform.gameObject.Texts.Clear();
            if (scenario == "unknownFont") first.font = new TMPro.TMP_FontAsset { name = "Unexpected" };
            if (scenario == "unknownMaterial") first.fontSharedMaterial = new Material(font.material);
            if (scenario == "wrongStaticCategory") first.Seed(font, targets[(rows[0].Style + 1) % 3]);
            if (scenario == "duplicateObject")
            {
                var original = last.transform.gameObject; var parent = original.transform.parent.gameObject;
                var duplicateNode = new GameObject(original.name, parent);
                var tmp = new TMPro.TMP_Text { transform = duplicateNode.transform }; tmp.font = last.font; tmp.fontSharedMaterial = last.fontSharedMaterial; duplicateNode.Texts.Add(tmp);
            }
            if (scenario == "play") EditorApplication.isPlayingOrWillChangePlaymode = true;
            if (scenario == "compile") EditorApplication.isCompiling = true;
            if (scenario == "prefabStage") UnityEditor.SceneManagement.PrefabStageUtility.Stage = new object();
            Reject(() => FontSwapOnce.Run(false), "must reject " + scenario);
            Check(Saves.Count == 0 && AssetDatabase.Saved.Count == 0, "preflight must fail before saving: " + scenario);
            Check(PrefabUtility.Loads == PrefabUtility.Unloads, "prefab resources released: " + scenario);
        }
        CheckShaderContracts();
        CheckStaticGuards();
        CheckColdSnapshots();
        return "{\"success\":true,\"dryRunCount\":171,\"assertions\":" + assertions + ",\"negativeCases\":" + negativeCases +
            ",\"shaderContractCases\":" + shaderCases.Count + ",\"exactShaderPairAccepted\":true,\"unsupportedShaderRejected\":true,\"requiredPropertiesValidated\":true,\"targetShaderAndGuidPreserved\":true," +
            "\"materialAssertionsPassed\":true,\"idempotencePassed\":true,\"partialMigrationPassed\":true,\"nativeUnityExecution\":false," +
            "\"nativeBindingProtection\":true,\"entryAndMemoryGuards\":true,\"allSaveBoundaryGuards\":true,\"renderGuardContract\":true,\"guardCases\":" + guardCases.Count + ",\"guardEvidence\":[" + string.Join(",", boundaryEvidence) + "],\"coldSnapshotCases\":" + snapshotEvidence.Count + ",\"coldSnapshotEvidence\":[" + string.Join(",", snapshotEvidence) + "]}";
    }
}
