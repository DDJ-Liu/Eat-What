using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using EatWhat.Tools.Persistence;
using EatWhat.Tools.Security;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Explicit, single-claim Windows Mono release phases. Called only by a bound official CLI eval.</summary>
public static class BuildReleaseEntry
{
    private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);
    private static readonly string[] ScenePaths = {
        "Assets/Scenes/LongLoop/Boot.unity", "Assets/Scenes/Cooking/ShortCycle_P0P1.unity",
        "Assets/Scenes/CookingProcess.unity", "Assets/Scenes/ShortCycle_P2.unity",
        "Assets/Scenes/LongLoop/LongLoop_Street.unity"
    };
    private static readonly string[] SceneGuids = {
        "edd552a86da4d7d43a4ea6ca41e71241", "7a0c3d3c3003f5449819f0e57a50a528",
        "adcee9c3f6697f24ab1bba84a023cc0a", "531735586aec56049b6591f91066d7fd",
        "d057397b78714bc4e901b928e195a875"
    };
    private static EditorApplication.CallbackFunction pendingBuild;
    private static EditorApplication.CallbackFunction pendingPhase;

    private static string Project { get { return Path.GetFullPath(Path.Combine(Application.dataPath, "..")); } }
    private static string Hash(byte[] bytes) { using (var h = SHA256.Create()) return BitConverter.ToString(h.ComputeHash(bytes)).Replace("-", ""); }
    private static string HashFile(string path)
    {
        NoLinks(path);
        using (var file = File.OpenRead(path))
        using (var h = SHA256.Create()) return BitConverter.ToString(h.ComputeHash(file)).Replace("-", "");
    }
    private static JObject Read(string path) { NoLinks(path); return JObject.Parse(File.ReadAllText(path, Utf8)); }
    private static void WriteNew(string path, JObject value)
    {
        using (var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
        using (var writer = new StreamWriter(file, Utf8)) writer.Write(value.ToString(Formatting.None));
    }
    private static void Publish(string path, JObject value)
    {
        var pending = path + ".pending-" + Guid.NewGuid().ToString("N");
        WriteNew(pending, value);
        File.Move(pending, path);
    }
    private static string Field(JObject value, string name)
    {
        var token = value[name];
        if (token == null || token.Type != JTokenType.String || string.IsNullOrWhiteSpace((string)token)) throw new IOException("Missing " + name);
        return (string)token;
    }
    private static void Segment(string value)
    {
        if (value.Length > 64 || value == "." || value == ".." || value.EndsWith(".", StringComparison.Ordinal) ||
            !char.IsLetterOrDigit(value[0]) || value.Any(c => !(char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == '.')))
            throw new IOException("Unsafe version or build number segment.");
    }
    private static string Rel(string root, string path)
    {
        var full = Path.GetFullPath(path);
        var prefix = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!full.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) throw new IOException("Path escaped owned root.");
        return full.Substring(prefix.Length).Replace('\\', '/');
    }
    private static string AssetInventorySha()
    {
        var root = Path.Combine(Project, "Assets");
        var paths = Directory.GetFiles(root, "*", SearchOption.AllDirectories)
            .Select(p => Rel(Project, p)).OrderBy(p => p, StringComparer.Ordinal).ToArray();
        return Hash(Utf8.GetBytes(string.Join("\n", paths)));
    }
    private static string AssetInventoryWithout(HashSet<string> owned)
    {
        var root = Path.Combine(Project, "Assets");
        var paths = Directory.GetFiles(root, "*", SearchOption.AllDirectories)
            .Select(p => Rel(Project, p)).Where(p => !owned.Contains(p))
            .OrderBy(p => p, StringComparer.Ordinal).ToArray();
        return Hash(Utf8.GetBytes(string.Join("\n", paths)));
    }
    // BUILD_INPUT_SELECTION_BEGIN: the offline fixture compiles this exact production method.
    public static string[] SelectBuildInputFiles(string projectRoot, IEnumerable<string> assetPaths, bool dynamicOnly)
    {
        var selected = new HashSet<string>(StringComparer.Ordinal);
        var prefix = Path.GetFullPath(projectRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        foreach (var raw in assetPaths)
        {
            if (string.IsNullOrEmpty(raw)) throw new IOException("Empty build asset path.");
            var relative = raw.Replace('\\', '/');
            var parts = relative.Split('/');
            if (parts.Length < 2 || parts[0] != "Assets") continue;
            if (parts.Any(p => p.Length == 0 || p == "." || p == "..")) throw new IOException("Invalid build asset path: " + relative);
            if (dynamicOnly && (!parts.Skip(1).Any(p => p == "Resources" || p == "StreamingAssets") ||
                parts.Skip(1).Any(p => p == "Editor"))) continue;
            var file = Path.GetFullPath(Path.Combine(projectRoot, relative.Replace('/', Path.DirectorySeparatorChar)));
            if (!file.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) throw new IOException("Build asset escaped project: " + relative);
            NoLinks(file);
            if (Directory.Exists(file)) continue;
            if (!File.Exists(file)) throw new IOException("Build input disappeared: " + relative);
            selected.Add(relative);
        }
        return selected.OrderBy(p => p, StringComparer.Ordinal).ToArray();
    }
    // BUILD_INPUT_SELECTION_END
    private static JObject CaptureInputs(JObject request)
    {
        var paths = new HashSet<string>(StringComparer.Ordinal);
        foreach (var asset in SelectBuildInputFiles(Project, AssetDatabase.GetDependencies(ScenePaths, true), false)) paths.Add(asset);
        var resources = SelectBuildInputFiles(Project, AssetDatabase.GetAllAssetPaths(), true);
        foreach (var asset in resources) paths.Add(asset);
        foreach (var asset in SelectBuildInputFiles(Project, AssetDatabase.GetDependencies(resources, true), false)) paths.Add(asset);
        foreach (var file in Directory.GetFiles(Path.Combine(Project, "Assets"), "*", SearchOption.AllDirectories))
        {
            var relative = Rel(Project, file);
            var extension = Path.GetExtension(file).ToLowerInvariant();
            if (extension == ".cs" || extension == ".asmdef" || extension == ".asmref" ||
                extension == ".dll" || extension == ".rsp") paths.Add(relative);
        }
        paths.Add("Packages/manifest.json"); paths.Add("Packages/packages-lock.json");
        foreach (var file in Directory.GetFiles(Path.Combine(Project, "ProjectSettings"), "*", SearchOption.AllDirectories))
            if (!file.Equals(Path.Combine(Project, "ProjectSettings/ProjectSettings.asset"), StringComparison.OrdinalIgnoreCase))
                paths.Add(Rel(Project, file));
        foreach (var path in paths.ToArray())
            if (path.StartsWith("Assets/", StringComparison.Ordinal) && File.Exists(Path.Combine(Project, path + ".meta")))
                paths.Add(path + ".meta");
        var entries = new JArray();
        foreach (var relative in paths.OrderBy(p => p, StringComparer.Ordinal))
        {
            var file = Path.Combine(Project, relative);
            NoLinks(file);
            if (!File.Exists(file)) throw new IOException("Build input disappeared: " + relative);
            entries.Add(new JObject { ["path"] = relative, ["bytes"] = new FileInfo(file).Length, ["sha256"] = HashFile(file) });
        }
        return new JObject {
            ["schemaVersion"] = 1, ["taskId"] = Field(request, "taskId"), ["attempt"] = (int)request["attempt"],
            ["executionId"] = Field(request, "executionId"), ["requestSha256"] = HashFile(Path.Combine(Field(request, "evidenceRoot"), "build-request.json")),
            ["assetInventorySha256"] = AssetInventorySha(), ["projectSettingsBeforeSha256"] = HashFile(Path.Combine(Project, "ProjectSettings/ProjectSettings.asset")),
            ["files"] = entries
        };
    }
    private static string ValidateInputs(JObject request)
    {
        var path = Path.Combine(Field(request, "evidenceRoot"), "input-manifest.json");
        var manifest = Read(path);
        if ((int?)manifest["schemaVersion"] != 1 || (string)manifest["taskId"] != Field(request, "taskId") ||
            (int?)manifest["attempt"] != (int)request["attempt"] || (string)manifest["executionId"] != Field(request, "executionId") ||
            (string)manifest["requestSha256"] != HashFile(Path.Combine(Field(request, "evidenceRoot"), "build-request.json")) ||
            (string)manifest["assetInventorySha256"] != AssetInventorySha())
            throw new IOException("Build input identity or Assets inventory drift.");
        var files = manifest["files"] as JArray;
        if (files == null || files.Count == 0) throw new IOException("Build input manifest empty.");
        var previous = "";
        foreach (var token in files)
        {
            var entry = token as JObject;
            if (entry == null || entry["path"] == null || entry["path"].Type != JTokenType.String ||
                entry["bytes"] == null || entry["bytes"].Type != JTokenType.Integer ||
                entry["sha256"] == null || entry["sha256"].Type != JTokenType.String)
                throw new IOException("Build input entry type invalid.");
            var relative = (string)entry["path"];
            if (string.CompareOrdinal(previous, relative) >= 0 || relative.Contains(":") || relative.Contains("..") ||
                Path.IsPathRooted(relative)) throw new IOException("Build input path or order invalid.");
            previous = relative;
            var file = Path.Combine(Project, relative);
            Rel(Project, file); NoLinks(file);
            if (!File.Exists(file) || new FileInfo(file).Length != (long)entry["bytes"] || HashFile(file) != (string)entry["sha256"])
                throw new IOException("Build input drift: " + relative);
        }
        return HashFile(path);
    }
    private static void NoLinks(string path)
    {
        for (var node = Path.GetFullPath(path); node != null; node = Path.GetDirectoryName(node))
            if ((File.Exists(node) || Directory.Exists(node)) && (File.GetAttributes(node) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Linked path refused: " + node);
    }
    private static JObject Request(string requestPath)
    {
        NoLinks(requestPath);
        var root = Project;
        var outputBase = Path.Combine(root, ".ai-workspace", "outputs");
        Rel(outputBase, requestPath);
        var request = Read(requestPath);
        if ((int?)request["schemaVersion"] != 1 || Field(request, "project") != root.Replace('\\', '/') ||
            Field(request, "unityVersion") != "6000.3.24f1" || Application.unityVersion != "6000.3.24f1" ||
            Field(request, "taskId").Length != 9 || (int?)request["attempt"] < 1 ||
            Field(request, "executionId").Length != 32 || Field(request, "ownerThreadId").Length != 36 ||
            Field(request, "sessionId").Length != 32 ||
            Field(request, "backend") != "Mono2x" || Field(request, "target") != "StandaloneWindows64" ||
            (Field(request, "configuration") != "Release" && Field(request, "configuration") != "Dev"))
            throw new IOException("Unbound or unsupported release request.");
        var evidence = Field(request, "evidenceRoot");
        var stage = Field(request, "staging");
        var output = Field(request, "outputRoot");
        Segment(Field(request, "version")); Segment(Field(request, "buildNumber"));
        var evidenceRelative = Rel(outputBase, evidence);
        var runSuffix = "/" + Field(request, "taskId") + "/attempt" + (int)request["attempt"] + "/build-run";
        if (!("/" + evidenceRelative).EndsWith(runSuffix, StringComparison.OrdinalIgnoreCase) ||
            !Path.GetFullPath(stage).Equals(Path.GetFullPath(Path.Combine(evidence, "staging")), StringComparison.OrdinalIgnoreCase) ||
            !Path.GetFullPath(requestPath).Equals(Path.GetFullPath(Path.Combine(evidence, "build-request.json")), StringComparison.OrdinalIgnoreCase))
            throw new IOException("Release evidence must be one isolated task run.");
        var expectedOutput = Path.Combine(root, "release", "shortcycle", Field(request, "version"), Field(request, "buildNumber"), Field(request, "configuration"));
        if (!Path.GetFullPath(output).Equals(Path.GetFullPath(expectedOutput), StringComparison.OrdinalIgnoreCase) ||
            !Path.GetFullPath(Field(request, "exe")).Equals(Path.GetFullPath(Path.Combine(output, "EatWhat.exe")), StringComparison.OrdinalIgnoreCase))
            throw new IOException("Release output layout changed.");
        foreach (var path in new[] { evidence, stage, output, requestPath }) NoLinks(path);
        return request;
    }
    private static void EditorGate(JObject request, bool recovering = false)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling ||
            BuildPipeline.isBuildingPlayer || EditorUtility.scriptCompilationFailed)
            throw new BuildFailedException("Compiled idle Edit Mode required.");
        for (var i = 0; i < SceneManager.sceneCount; i++) if (SceneManager.GetSceneAt(i).isDirty) throw new BuildFailedException("Dirty authoring scene; preserve.");
        var scenes = EditorBuildSettings.scenes;
        if (scenes.Length != 5) throw new BuildFailedException("Exactly five Build scenes required.");
        for (var i = 0; i < 5; i++)
            if (!scenes[i].enabled || scenes[i].path != ScenePaths[i] || AssetDatabase.AssetPathToGUID(ScenePaths[i]) != SceneGuids[i])
                throw new BuildFailedException("Build scene order or GUID drift.");
        if (PlayerSettings.GetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone) != ScriptingImplementation.Mono2x ||
            !BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
            throw new BuildFailedException("Windows x64 Mono unavailable.");
        if (!recovering && (File.Exists(Path.Combine(Project, "Library/CatalogBlobBuild.journal.json")) ||
            File.Exists(Path.Combine(Project, CatalogBlobBuildProcessor.BlobPath)) ||
            File.Exists(Path.Combine(Project, CatalogBlobBuildProcessor.BlobPath + ".meta"))))
            throw new BuildFailedException("Unsettled or unowned catalog input; preserve.");
    }
    private static JObject Capture()
    {
        var method = typeof(CatalogBlobBuildProcessor).GetMethod("Capture", BindingFlags.NonPublic | BindingFlags.Static);
        if (method == null || method.ReturnType != typeof(JObject) || method.GetParameters().Length != 1 ||
            !method.GetParameters()[0].ParameterType.IsByRef) throw new BuildFailedException("Catalog capture API changed.");
        return (JObject)method.Invoke(null, new object[] { null });
    }
    private static string ValidateCatalog(JObject request)
    {
        var stage = Field(request, "staging");
        NoLinks(stage);
        var files = new[] { "catalog.graph.json", "catalog.bin", "CatalogBuildFingerprint.g.cs", "export.json" };
        foreach (var name in files) { var path = Path.Combine(stage, name); NoLinks(path); if (!File.Exists(path)) throw new IOException("Missing staged " + name); }
        var current = Capture();
        var staged = Read(Path.Combine(stage, files[0]));
        var fingerprint = CatalogBlobSerializer.Fingerprint(current);
        if (fingerprint != CatalogBlobSerializer.Fingerprint(staged) || fingerprint != CatalogBuildFingerprint.Expected)
            throw new BuildFailedException("Current, staged and loaded fingerprints differ.");
        var decoded = CatalogBlobSerializer.Authenticate(File.ReadAllBytes(Path.Combine(stage, files[1])), fingerprint,
            new CatalogBlobCipher(new ObfuscatedStaticKeyProvider()));
        if (SaveEnvelope.SerializeCanonicalObject(decoded) != SaveEnvelope.SerializeCanonicalObject(staged) ||
            SaveEnvelope.SerializeCanonicalObject(staged) != SaveEnvelope.SerializeCanonicalObject(current))
            throw new BuildFailedException("Authenticated staged graph differs from current graph.");
        if (File.ReadAllText(Path.Combine(stage, files[2]), Utf8) != CatalogBlobSerializer.FingerprintSource(fingerprint))
            throw new BuildFailedException("Staged fingerprint source differs from loaded Expected.");
        var export = Read(Path.Combine(stage, files[3]));
        if ((string)export["fingerprint"] != fingerprint || (bool?)export["authenticatedReadback"] != true ||
            (int?)export["dataAssets"] != ((JArray)staged["assets"]).Count ||
            (int?)export["spriteKeys"] != ((JArray)staged["sprites"]).Count)
            throw new BuildFailedException("Staging export metadata differs from authenticated graph.");
        return fingerprint;
    }
    private static JObject ValidateSealed(JObject request, string pinnedManifestSha)
    {
        if (pinnedManifestSha == null || pinnedManifestSha.Length != 64 || pinnedManifestSha.Any(c => !Uri.IsHexDigit(c)))
            throw new IOException("A compiled, pinned staging-manifest SHA is required.");
        var evidence = Field(request, "evidenceRoot");
        var path = Path.Combine(evidence, "staging-manifest.json");
        NoLinks(path);
        if (HashFile(path) != pinnedManifestSha.ToUpperInvariant()) throw new IOException("Pinned staging manifest changed.");
        var manifest = Read(path);
        if ((int?)manifest["schemaVersion"] != 1 || (string)manifest["taskId"] != Field(request, "taskId") ||
            (int?)manifest["attempt"] != (int)request["attempt"] || (string)manifest["executionId"] != Field(request, "executionId") ||
            (string)manifest["ownerThreadId"] != Field(request, "ownerThreadId") ||
            (string)manifest["sessionId"] != Field(request, "sessionId") ||
            (string)manifest["project"] != Field(request, "project") ||
            (string)manifest["version"] != Field(request, "version") ||
            (string)manifest["buildNumber"] != Field(request, "buildNumber") ||
            (string)manifest["configuration"] != Field(request, "configuration") ||
            (string)manifest["requestSha256"] != HashFile(Path.Combine(evidence, "build-request.json")))
            throw new IOException("Staging manifest identity or request SHA changed.");
        var expected = new[] { "catalog.graph.json", "catalog.bin", "CatalogBuildFingerprint.g.cs", "export.json" };
        var entries = manifest["files"] as JArray;
        if (entries == null || entries.Count != 4) throw new IOException("Exactly four staged SHA records required.");
        for (var i = 0; i < 4; i++)
        {
            var entry = entries[i] as JObject;
            var file = Path.Combine(Field(request, "staging"), expected[i]);
            if (entry == null || (string)entry["path"] != expected[i] || (long?)entry["bytes"] != new FileInfo(file).Length ||
                (string)entry["sha256"] != HashFile(file)) throw new IOException("Staged SHA/length changed: " + expected[i]);
        }
        if ((string)manifest["fingerprint"] != ValidateCatalog(request)) throw new IOException("Sealed catalog fingerprint changed.");
        if ((string)manifest["inputManifestSha256"] != ValidateInputs(request)) throw new IOException("Pinned build input manifest changed.");
        return manifest;
    }
    private static void ClaimGate(JObject request)
    {
        var runtime = Path.Combine(Project, ".ai-workspace", "runtime");
        var queue = Read(Path.Combine(runtime, "queue-state.json"));
        var mode = Read(Path.Combine(runtime, "mode-state.json"));
        var workers = Read(Path.Combine(runtime, "worker-state.json"));
        var task = ((JArray)queue["tasks"]).Single(t => (string)t["id"] == Field(request, "taskId"));
        var worker = workers["workers"]["ENGINE_CLI"];
        var execution = task["execution"];
        if ((string)task["status"] != "RUNNING" || (int?)task["attempts"] != (int)request["attempt"] ||
            (string)execution["executionId"] != Field(request, "executionId") ||
            (string)execution["ownerThreadId"] != Field(request, "ownerThreadId") ||
            (string)worker["status"] != "BUSY" || (string)worker["activeTaskId"] != Field(request, "taskId") ||
            (string)worker["activeExecutionId"] != Field(request, "executionId") ||
            DateTimeOffset.Parse((string)worker["leaseUntil"]) <= DateTimeOffset.UtcNow ||
            (string)mode["mode"] != "SUPERVISED" || (string)mode["supervised"]["status"] != "ACTIVE" ||
            (bool?)mode["supervised"]["stopRequested"] != false || (bool?)mode["supervised"]["exitRequested"] != false ||
            (string)mode["supervised"]["sessionId"] != Field(request, "sessionId") ||
            !((JArray)mode["supervised"]["taskIds"]).Any(t => (string)t == Field(request, "taskId")))
            throw new BuildFailedException("Claim, route, lease or supervision drift; no Build.");
    }
    private static string PhaseFile(JObject request, string phase, string suffix)
    {
        return Path.Combine(Field(request, "evidenceRoot"), phase.ToLowerInvariant() + "-" + suffix + ".json");
    }
    private static string PhaseResultFile(JObject request, string phase)
    {
        return Path.Combine(Field(request, "evidenceRoot"), phase == "Stage" ? "stage-result.json" :
            phase == "Prepare" ? "prepare-result.json" : phase == "Recovery" ? "recovery-result.json" : "final-readback.json");
    }
    private static string EntrySourceSha()
    {
        return HashFile(Path.Combine(Project, "Assets/Editor/Build/BuildReleaseEntry.cs"));
    }
    private static JObject RegisterPhase(string requestPath, string phase, Func<JObject, JObject> work)
    {
        var request = Request(requestPath);
        ClaimGate(request); EditorGate(request, phase == "Recovery");
        var registeredPath = PhaseFile(request, phase, "registered");
        if (pendingPhase != null || File.Exists(registeredPath) || File.Exists(PhaseResultFile(request, phase)) ||
            File.Exists(PhaseFile(request, phase, "completion")) || File.Exists(PhaseFile(request, phase, "failure")))
            throw new IOException("Existing phase invocation is unknown; no second registration.");
        var registered = new JObject {
            ["phase"] = phase, ["invocationId"] = Guid.NewGuid().ToString("N"),
            ["taskId"] = Field(request, "taskId"), ["attempt"] = (int)request["attempt"],
            ["executionId"] = Field(request, "executionId"), ["ownerThreadId"] = Field(request, "ownerThreadId"),
            ["sessionId"] = Field(request, "sessionId"), ["pid"] = System.Diagnostics.Process.GetCurrentProcess().Id,
            ["requestSha256"] = HashFile(requestPath), ["sourceSha256"] = EntrySourceSha(),
            ["registeredAt"] = DateTimeOffset.UtcNow.ToString("O")
        };
        WriteNew(registeredPath, registered);
        var notBefore = DateTime.UtcNow.AddSeconds(2); // Let the registration RPC return before main-thread work.
        EditorApplication.CallbackFunction callback = null;
        callback = () => {
            if (DateTime.UtcNow < notBefore) return;
            EditorApplication.update -= callback;
            pendingPhase = null;
            try
            {
                var fresh = Request(requestPath);
                ClaimGate(fresh); EditorGate(fresh, phase == "Recovery");
                if (HashFile(requestPath) != (string)registered["requestSha256"] ||
                    EntrySourceSha() != (string)registered["sourceSha256"] ||
                    (int)registered["pid"] != System.Diagnostics.Process.GetCurrentProcess().Id ||
                    HashFile(registeredPath) != Hash(Utf8.GetBytes(registered.ToString(Formatting.None))))
                    throw new BuildFailedException("Phase request, source, registration or Editor changed.");
                var result = work(fresh);
                var resultPath = PhaseResultFile(fresh, phase);
                if (!JToken.DeepEquals(result, Read(resultPath))) throw new IOException("Phase result changed before completion.");
                WriteNew(PhaseFile(fresh, phase, "completion"), new JObject {
                    ["phase"] = phase, ["invocationId"] = (string)registered["invocationId"],
                    ["taskId"] = Field(fresh, "taskId"), ["attempt"] = (int)fresh["attempt"],
                    ["executionId"] = Field(fresh, "executionId"), ["ownerThreadId"] = Field(fresh, "ownerThreadId"),
                    ["sessionId"] = Field(fresh, "sessionId"), ["pid"] = (int)registered["pid"],
                    ["requestSha256"] = HashFile(requestPath), ["sourceSha256"] = (string)registered["sourceSha256"],
                    ["registrationSha256"] = HashFile(registeredPath), ["resultSha256"] = HashFile(resultPath),
                    ["status"] = "SUCCEEDED", ["completedAt"] = DateTimeOffset.UtcNow.ToString("O")
                });
            }
            catch (Exception error)
            {
                try { WriteNew(PhaseFile(request, phase, "failure"), new JObject {
                    ["phase"] = phase, ["invocationId"] = (string)registered["invocationId"],
                    ["taskId"] = Field(request, "taskId"), ["executionId"] = Field(request, "executionId"),
                    ["registrationSha256"] = HashFile(registeredPath), ["errorType"] = error.GetType().FullName,
                    ["error"] = error.ToString(), ["failedAt"] = DateTimeOffset.UtcNow.ToString("O")
                }); } catch (Exception evidenceError) { Debug.LogException(evidenceError); }
                Debug.LogException(error);
            }
        };
        pendingPhase = callback;
        EditorApplication.update += callback;
        return registered;
    }
    public static JObject ConfirmPhase(string requestPath, string phase)
    {
        if (phase != "Stage" && phase != "Prepare" && phase != "Readback" && phase != "Recovery") throw new IOException("Unknown release phase.");
        var request = Request(requestPath);
        ClaimGate(request); EditorGate(request, phase == "Recovery");
        var registrationPath = PhaseFile(request, phase, "registered");
        var completionPath = PhaseFile(request, phase, "completion");
        if (File.Exists(PhaseFile(request, phase, "failure")) || !File.Exists(completionPath))
            throw new BuildFailedException("Phase failed or is incomplete; no successful managed confirmation.");
        var registration = Read(registrationPath);
        var completion = Read(completionPath);
        var resultPath = PhaseResultFile(request, phase);
        if ((string)registration["phase"] != phase || (string)completion["phase"] != phase ||
            (string)registration["invocationId"] != (string)completion["invocationId"] ||
            (string)registration["taskId"] != Field(request, "taskId") ||
            (int?)registration["attempt"] != (int)request["attempt"] ||
            (string)registration["executionId"] != Field(request, "executionId") ||
            (string)registration["ownerThreadId"] != Field(request, "ownerThreadId") ||
            (string)registration["sessionId"] != Field(request, "sessionId") ||
            (int?)registration["pid"] != System.Diagnostics.Process.GetCurrentProcess().Id ||
            (string)registration["requestSha256"] != HashFile(requestPath) ||
            (string)registration["sourceSha256"] != EntrySourceSha() ||
            (string)completion["taskId"] != Field(request, "taskId") ||
            (int?)completion["attempt"] != (int)request["attempt"] ||
            (string)completion["executionId"] != Field(request, "executionId") ||
            (string)completion["ownerThreadId"] != Field(request, "ownerThreadId") ||
            (string)completion["sessionId"] != Field(request, "sessionId") ||
            (int?)completion["pid"] != (int?)registration["pid"] ||
            (string)completion["requestSha256"] != HashFile(requestPath) ||
            (string)completion["sourceSha256"] != (string)registration["sourceSha256"] ||
            (string)completion["registrationSha256"] != HashFile(registrationPath) ||
            (string)completion["resultSha256"] != HashFile(resultPath) ||
            (string)completion["status"] != "SUCCEEDED")
            throw new BuildFailedException("Phase registration, completion or result identity changed.");
        return new JObject {
            ["phase"] = phase, ["invocationId"] = (string)registration["invocationId"],
            ["taskId"] = Field(request, "taskId"), ["attempt"] = (int)request["attempt"],
            ["executionId"] = Field(request, "executionId"), ["ownerThreadId"] = Field(request, "ownerThreadId"),
            ["sessionId"] = Field(request, "sessionId"), ["pid"] = (int)registration["pid"],
            ["requestSha256"] = HashFile(requestPath), ["registrationSha256"] = HashFile(registrationPath),
            ["completionSha256"] = HashFile(completionPath), ["resultSha256"] = HashFile(resultPath),
            ["phaseResult"] = Read(resultPath)
        };
    }
    public static JObject RegisterStage(string requestPath)
    {
        return RegisterPhase(requestPath, "Stage", fresh => StageCore(fresh, requestPath));
    }
    private static JObject StageCore(JObject request, string requestPath)
    {
        var stage = Field(request, "staging");
        if (Directory.Exists(stage) || File.Exists(Path.Combine(Field(request, "evidenceRoot"), "staging-manifest.json")))
            throw new IOException("Existing staging is unknown; do not re-export or re-sign.");
        CatalogBlobBuildProcessor.PrepareSnapshot(stage); // Export authenticates its newly written packet.
        var fingerprint = ValidateCatalog(request); // Independent direct Authenticate + canonical compare.
        var inputPath = Path.Combine(Field(request, "evidenceRoot"), "input-manifest.json");
        var inputManifest = CaptureInputs(request);
        if ((string)inputManifest["projectSettingsBeforeSha256"] != Field(request, "projectSettingsSha256") ||
            HashFile(Path.Combine(Project, "ProjectSettings/EditorBuildSettings.asset")) != Field(request, "buildSettingsSha256"))
            throw new IOException("Project settings changed since Plan; no seal.");
        WriteNew(inputPath, inputManifest);
        var names = new[] { "catalog.graph.json", "catalog.bin", "CatalogBuildFingerprint.g.cs", "export.json" };
        var entries = new JArray();
        foreach (var name in names)
        {
            var file = Path.Combine(stage, name);
            entries.Add(new JObject { ["path"] = name, ["bytes"] = new FileInfo(file).Length, ["sha256"] = HashFile(file) });
        }
        var manifest = new JObject {
            ["schemaVersion"] = 1, ["taskId"] = Field(request, "taskId"), ["attempt"] = (int)request["attempt"],
            ["executionId"] = Field(request, "executionId"), ["ownerThreadId"] = Field(request, "ownerThreadId"),
            ["sessionId"] = Field(request, "sessionId"), ["project"] = Field(request, "project"),
            ["version"] = Field(request, "version"), ["buildNumber"] = Field(request, "buildNumber"),
            ["configuration"] = Field(request, "configuration"), ["requestSha256"] = HashFile(requestPath),
            ["fingerprint"] = fingerprint, ["inputManifestSha256"] = HashFile(inputPath),
            ["files"] = entries, ["exportedAt"] = DateTimeOffset.UtcNow.ToString("O")
        };
        var manifestPath = Path.Combine(Field(request, "evidenceRoot"), "staging-manifest.json");
        WriteNew(manifestPath, manifest);
        var result = new JObject { ["taskId"] = Field(request, "taskId"), ["attempt"] = (int)request["attempt"],
            ["executionId"] = Field(request, "executionId"), ["ownerThreadId"] = Field(request, "ownerThreadId"),
            ["sessionId"] = Field(request, "sessionId"), ["requestSha256"] = HashFile(requestPath),
            ["manifestPath"] = manifestPath, ["manifestSha256"] = HashFile(manifestPath),
            ["fingerprint"] = fingerprint, ["inputManifestSha256"] = HashFile(inputPath),
            ["fourFileChecks"] = 4, ["authenticatedReadback"] = true };
        WriteNew(Path.Combine(Field(request, "evidenceRoot"), "stage-result.json"), result);
        return result;
    }
    public static JObject RegisterPrepare(string requestPath, string pinnedManifestSha)
    {
        return RegisterPhase(requestPath, "Prepare", fresh => PrepareCore(fresh, requestPath, pinnedManifestSha));
    }
    private static JObject PrepareCore(JObject request, string requestPath, string pinnedManifestSha)
    {
        var manifest = ValidateSealed(request, pinnedManifestSha); // Before version write.
        var settings = Path.Combine(Project, "ProjectSettings");
        var target = Path.Combine(settings, "ProjectSettings.asset");
        var before = File.ReadAllText(target, Utf8);
        if (HashFile(target) != (string)Read(Path.Combine(Field(request, "evidenceRoot"), "input-manifest.json"))["projectSettingsBeforeSha256"])
            throw new BuildFailedException("ProjectSettings changed since authenticated stage.");
        var others = Directory.GetFiles(settings, "*", SearchOption.AllDirectories).Where(p => p != target)
            .ToDictionary(p => p, HashFile, StringComparer.OrdinalIgnoreCase);
        var oldVersion = PlayerSettings.bundleVersion;
        var requested = Field(request, "version");
        if (oldVersion != requested)
        {
            if (oldVersion != Field(request, "expectedCurrentVersion")) throw new BuildFailedException("Unexpected current bundleVersion.");
            PlayerSettings.bundleVersion = requested;
            for (var i = 0; i < SceneManager.sceneCount; i++) if (SceneManager.GetSceneAt(i).isDirty)
                throw new BuildFailedException("Scene dirty after version setter; preserve before SaveAssets.");
            AssetDatabase.SaveAssets();
        }
        if (PlayerSettings.bundleVersion != requested) throw new BuildFailedException("bundleVersion readback changed.");
        var after = File.ReadAllText(target, Utf8);
        if (oldVersion == requested && after != before) throw new BuildFailedException("Settings changed without setter.");
        if (oldVersion != requested)
        {
            var marker = "  bundleVersion: " + oldVersion;
            if (before.Split(new[] { marker }, StringSplitOptions.None).Length != 2 ||
                after != before.Replace(marker, "  bundleVersion: " + requested))
                throw new BuildFailedException("ProjectSettings changed beyond bundleVersion; preserve.");
        }
        var otherAfter = Directory.GetFiles(settings, "*", SearchOption.AllDirectories).Where(p => p != target).ToArray();
        if (otherAfter.Length != others.Count || otherAfter.Any(p => !others.ContainsKey(p) || others[p] != HashFile(p)))
            throw new BuildFailedException("Other ProjectSettings changed; preserve.");
        ValidateSealed(request, pinnedManifestSha); // Staging cannot drift during version save.
        var scenes = new JArray();
        for (var i = 0; i < SceneManager.sceneCount; i++)
        {
            var s = SceneManager.GetSceneAt(i);
            if (s.isDirty) throw new BuildFailedException("Authoring scene dirty after preparation.");
            scenes.Add(new JObject { ["path"] = s.path, ["dirty"] = s.isDirty });
        }
        var result = new JObject {
            ["taskId"] = Field(request, "taskId"), ["attempt"] = (int)request["attempt"],
            ["executionId"] = Field(request, "executionId"), ["ownerThreadId"] = Field(request, "ownerThreadId"),
            ["sessionId"] = Field(request, "sessionId"), ["project"] = Field(request, "project"),
            ["pid"] = System.Diagnostics.Process.GetCurrentProcess().Id, ["requestSha256"] = HashFile(requestPath),
            ["manifestSha256"] = pinnedManifestSha.ToUpperInvariant(), ["fingerprint"] = (string)manifest["fingerprint"],
            ["inputManifestSha256"] = (string)manifest["inputManifestSha256"],
            ["version"] = requested, ["buildNumber"] = Field(request, "buildNumber"),
            ["configuration"] = Field(request, "configuration"), ["versionBefore"] = oldVersion,
            ["versionAfter"] = PlayerSettings.bundleVersion, ["saveAssetsCalls"] = oldVersion == requested ? 0 : 1,
            ["settingsBeforeSha256"] = Hash(Utf8.GetBytes(before)), ["settingsAfterSha256"] = HashFile(target),
            ["otherSettingsUnchanged"] = true, ["loadedScenes"] = scenes,
            ["activeScenePath"] = SceneManager.GetActiveScene().path
        };
        WriteNew(Path.Combine(Field(request, "evidenceRoot"), "prepare-result.json"), result);
        return result;
    }
    public static JObject RegisterBuild(string requestPath, string pinnedManifestSha)
    {
        var request = Request(requestPath);
        ClaimGate(request); EditorGate(request);
        var evidence = Field(request, "evidenceRoot");
        var output = Field(request, "outputRoot");
        if (Directory.Exists(output) || pendingBuild != null ||
            File.Exists(Path.Combine(evidence, "build-registered.json")) || File.Exists(Path.Combine(evidence, "build-result.json")))
            throw new IOException("Existing build or registration is unknown; no second Build.");
        var registration = new JObject {
            ["phase"] = "Build", ["invocationId"] = Guid.NewGuid().ToString("N"),
            ["taskId"] = Field(request, "taskId"), ["attempt"] = (int)request["attempt"],
            ["executionId"] = Field(request, "executionId"), ["ownerThreadId"] = Field(request, "ownerThreadId"),
            ["sessionId"] = Field(request, "sessionId"), ["sourceSha256"] = EntrySourceSha(),
            ["requestSha256"] = HashFile(requestPath),
            ["manifestSha256"] = pinnedManifestSha.ToUpperInvariant(), ["pid"] = System.Diagnostics.Process.GetCurrentProcess().Id,
            ["queuedAt"] = DateTimeOffset.UtcNow.ToString("O"), ["buildInvocationLimit"] = 1
        };
        WriteNew(Path.Combine(evidence, "build-registered.json"), registration);
        var notBefore = DateTime.UtcNow.AddSeconds(5);
        EditorApplication.CallbackFunction callback = null;
        callback = () => {
            if (DateTime.UtcNow < notBefore) return;
            EditorApplication.update -= callback;
            pendingBuild = null;
            var started = DateTimeOffset.UtcNow;
            var resultPath = Path.Combine(evidence, "build-result.json");
            try
            {
                var fresh = Request(requestPath);
                ClaimGate(fresh); EditorGate(fresh);
                if (EntrySourceSha() != (string)registration["sourceSha256"] ||
                    HashFile(requestPath) != (string)registration["requestSha256"] ||
                    (int?)registration["pid"] != System.Diagnostics.Process.GetCurrentProcess().Id ||
                    HashFile(Path.Combine(evidence, "build-registered.json")) != Hash(Utf8.GetBytes(registration.ToString(Formatting.None))))
                    throw new BuildFailedException("Build registration, request, source or Editor changed.");
                ValidateSealed(fresh, pinnedManifestSha); // Four exact SHA + direct Authenticate before BuildPrepared.
                var currentVersion = PlayerSettings.bundleVersion;
                var currentPrepared = Read(Path.Combine(evidence, "prepare-result.json"));
                if ((string)currentPrepared["requestSha256"] != HashFile(requestPath) ||
                    (string)currentPrepared["manifestSha256"] != pinnedManifestSha.ToUpperInvariant() ||
                    (string)currentPrepared["inputManifestSha256"] != (string)Read(Path.Combine(evidence, "staging-manifest.json"))["inputManifestSha256"] ||
                    (string)currentPrepared["version"] != currentVersion ||
                    (string)currentPrepared["executionId"] != Field(fresh, "executionId") ||
                    (int?)currentPrepared["pid"] != System.Diagnostics.Process.GetCurrentProcess().Id)
                    throw new BuildFailedException("Preparation or Editor identity changed.");
                if (currentVersion != Field(fresh, "version") || (string)currentPrepared["versionAfter"] != currentVersion ||
                    (string)currentPrepared["requestSha256"] != HashFile(requestPath) ||
                    (string)currentPrepared["manifestSha256"] != pinnedManifestSha.ToUpperInvariant() ||
                    (string)currentPrepared["inputManifestSha256"] != HashFile(Path.Combine(evidence, "input-manifest.json")) ||
                    (string)currentPrepared["executionId"] != Field(fresh, "executionId") ||
                    (string)currentPrepared["ownerThreadId"] != Field(fresh, "ownerThreadId") ||
                    (string)currentPrepared["sessionId"] != Field(fresh, "sessionId") ||
                    (int?)currentPrepared["attempt"] != (int)fresh["attempt"] ||
                    (int?)currentPrepared["pid"] != System.Diagnostics.Process.GetCurrentProcess().Id ||
                    (string)currentPrepared["settingsAfterSha256"] != HashFile(Path.Combine(Project, "ProjectSettings/ProjectSettings.asset")))
                    throw new BuildFailedException("Delayed version, preparation or Editor identity changed; no Build.");
                if (Directory.Exists(output)) throw new IOException("Output appeared before Build; preserve.");
                var development = Field(fresh, "configuration") == "Dev";
                var options = new BuildPlayerOptions {
                    scenes = ScenePaths, locationPathName = Field(fresh, "exe"), target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.DetailedBuildReport | (development ? BuildOptions.Development : BuildOptions.None),
                    extraScriptingDefines = new string[0]
                };
                var report = CatalogBlobBuildProcessor.BuildPrepared(options, Field(fresh, "staging"));
                if (report == null || report.summary.result != BuildResult.Succeeded || report.summary.totalErrors != 0)
                    throw new BuildFailedException("Native BuildReport failed or has errors.");
                var sdk = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Unity.Pipeline.Editor")
                    .GetType("Unity.Pipeline.Editor.Commands.Build.BuildCommand", true);
                var projector = sdk.GetMethod("Project", BindingFlags.Static | BindingFlags.NonPublic, null,
                    new[] { typeof(string), typeof(BuildReport) }, null);
                if (projector == null || projector.ReturnType.FullName != "Unity.Pipeline.Editor.Commands.Build.BuildReportResult")
                    throw new BuildFailedException("Installed SDK BuildReport projector changed.");
                var projected = projector.Invoke(null, new object[] { Field(fresh, "taskId") + "-a" + (int)fresh["attempt"], report });
                var reportBytes = Utf8.GetBytes(JObject.FromObject(projected).ToString(Formatting.None));
                var reportPath = Path.Combine(evidence, "BuildReport.json.gz");
                using (var file = new FileStream(reportPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
                using (var gzip = new GZipStream(file, CompressionMode.Compress)) gzip.Write(reportBytes, 0, reportBytes.Length);
                var map = new JObject {
                    ["taskId"] = Field(fresh, "taskId"), ["attempt"] = (int)fresh["attempt"],
                    ["executionId"] = Field(fresh, "executionId"), ["requestSha256"] = HashFile(requestPath),
                    ["manifestSha256"] = pinnedManifestSha.ToUpperInvariant(),
                    ["inputManifestSha256"] = HashFile(Path.Combine(evidence, "input-manifest.json")),
                    ["version"] = Field(fresh, "version"), ["buildNumber"] = Field(fresh, "buildNumber"),
                    ["configuration"] = Field(fresh, "configuration"), ["path"] = reportPath,
                    ["format"] = "gzip-json-utf8-lossless", ["rawBytes"] = reportBytes.Length,
                    ["rawSha256"] = Hash(reportBytes), ["compressedSha256"] = HashFile(reportPath)
                };
                WriteNew(Path.Combine(evidence, "BuildReport.map.json"), map);
                var package = new JArray();
                foreach (var file in Directory.GetFiles(output, "*", SearchOption.AllDirectories).OrderBy(p => p, StringComparer.Ordinal))
                {
                    NoLinks(file);
                    package.Add(new JObject { ["path"] = Rel(output, file), ["bytes"] = new FileInfo(file).Length, ["sha256"] = HashFile(file) });
                }
                WriteNew(Path.Combine(evidence, "package-files.json"), new JObject { ["files"] = package });
                var summary = new JObject {
                    ["taskId"] = Field(fresh, "taskId"), ["attempt"] = (int)fresh["attempt"],
                    ["executionId"] = Field(fresh, "executionId"), ["ownerThreadId"] = Field(fresh, "ownerThreadId"),
                    ["sessionId"] = Field(fresh, "sessionId"), ["requestSha256"] = HashFile(requestPath),
                    ["inputManifestSha256"] = HashFile(Path.Combine(evidence, "input-manifest.json")),
                    ["manifestSha256"] = pinnedManifestSha.ToUpperInvariant(), ["version"] = Field(fresh, "version"),
                    ["buildNumber"] = Field(fresh, "buildNumber"), ["configuration"] = Field(fresh, "configuration"),
                    ["backend"] = "Mono2x", ["scenePaths"] = new JArray(ScenePaths), ["nativeResult"] = report.summary.result.ToString(),
                    ["errors"] = report.summary.totalErrors, ["warnings"] = report.summary.totalWarnings,
                    ["sizeBytes"] = report.summary.totalSize, ["durationTicks"] = report.summary.totalTime.Ticks,
                    ["exe"] = Field(fresh, "exe"), ["exeSha256"] = HashFile(Field(fresh, "exe")),
                    ["packageFilesSha256"] = HashFile(Path.Combine(evidence, "package-files.json")),
                    ["buildReportMapSha256"] = HashFile(Path.Combine(evidence, "BuildReport.map.json")),
                    ["pid"] = System.Diagnostics.Process.GetCurrentProcess().Id, ["buildCalls"] = 1,
                    ["startedAt"] = started.ToString("O"), ["endedAt"] = DateTimeOffset.UtcNow.ToString("O"), ["success"] = true
                };
                Publish(resultPath, summary);
            }
            catch (Exception error)
            {
                if (!File.Exists(resultPath)) Publish(resultPath, new JObject {
                    ["taskId"] = Field(request, "taskId"), ["executionId"] = Field(request, "executionId"),
                    ["requestSha256"] = HashFile(requestPath), ["manifestSha256"] = pinnedManifestSha.ToUpperInvariant(),
                    ["success"] = false, ["errorType"] = error.GetType().FullName, ["error"] = error.ToString(),
                    ["startedAt"] = started.ToString("O"), ["endedAt"] = DateTimeOffset.UtcNow.ToString("O")
                });
            }
        };
        pendingBuild = callback;
        EditorApplication.update += callback;
        return registration;
    }
    private static void ValidateRecoveryInputs(JObject prior, JObject review, HashSet<string> owned)
    {
        var evidence = Field(prior, "evidenceRoot");
        var path = Path.Combine(evidence, "input-manifest.json");
        if (HashFile(path) != Field(review, "priorInputManifestSha256"))
            throw new BuildFailedException("Original input manifest changed.");
        var manifest = Read(path);
        if ((int?)manifest["schemaVersion"] != 1 || (string)manifest["taskId"] != Field(prior, "taskId") ||
            (int?)manifest["attempt"] != (int)prior["attempt"] ||
            (string)manifest["executionId"] != Field(prior, "executionId") ||
            (string)manifest["requestSha256"] != HashFile(Path.Combine(evidence, "build-request.json")) ||
            (string)manifest["assetInventorySha256"] != AssetInventoryWithout(owned))
            throw new BuildFailedException("Original build input inventory or identity changed.");
        var allowed = review["allowedEditorChanges"] as JArray;
        if (allowed == null) throw new BuildFailedException("CONTROL editor source difference list missing.");
        var differences = new Dictionary<string, JObject>(StringComparer.Ordinal);
        var exactEditors = new HashSet<string>(new[] {
            "Assets/Editor/Build/CatalogBlobBuildProcessor.cs",
            "Assets/Editor/Build/CatalogBuildPlan.cs",
            "Assets/Editor/Build/BuildReleaseEntry.cs"
        }, StringComparer.Ordinal);
        foreach (var token in allowed)
        {
            var change = token as JObject;
            if (change == null || !exactEditors.Contains(Field(change, "path")) ||
                !differences.TryAdd(Field(change, "path"), change))
                throw new BuildFailedException("Unapproved or duplicate Editor source difference.");
        }
        var entries = manifest["files"] as JArray;
        if (entries == null || entries.Count == 0) throw new BuildFailedException("Original input list missing.");
        var priorPath = "";
        foreach (var token in entries)
        {
            var entry = token as JObject;
            var relative = Field(entry, "path");
            if (string.CompareOrdinal(priorPath, relative) >= 0 || relative.Contains(":") ||
                relative.Contains("..") || Path.IsPathRooted(relative))
                throw new BuildFailedException("Original input path or order changed.");
            priorPath = relative;
            var file = Path.Combine(Project, relative);
            Rel(Project, file); NoLinks(file);
            if (!File.Exists(file)) throw new BuildFailedException("Original input disappeared: " + relative);
            var sha = HashFile(file);
            var bytes = new FileInfo(file).Length;
            JObject change;
            if (differences.TryGetValue(relative, out change))
            {
                if ((string)change["oldSha256"] != (string)entry["sha256"] ||
                    (long?)change["oldBytes"] != (long?)entry["bytes"] ||
                    (string)change["newSha256"] != sha || (long?)change["newBytes"] != bytes ||
                    (string)change["oldSha256"] == sha)
                    throw new BuildFailedException("CONTROL Editor difference changed: " + relative);
                differences.Remove(relative);
            }
            else if ((string)entry["sha256"] != sha || (long?)entry["bytes"] != bytes)
                throw new BuildFailedException("Original non-Editor input drift: " + relative);
        }
        if (differences.Count != 0) throw new BuildFailedException("CONTROL difference did not match original input list.");
    }
    private static JArray ValidateRecoveryPackage(JObject prior, JObject review)
    {
        var path = Field(review, "priorPackageManifestPath");
        Rel(Path.Combine(Project, ".ai-workspace", "outputs"), path);
        if (HashFile(path) != Field(review, "priorPackageManifestSha256"))
            throw new BuildFailedException("Original package inventory changed.");
        var package = Read(path);
        if ((string)package["taskId"] != Field(prior, "taskId") ||
            (int?)package["attempt"] != (int)prior["attempt"] ||
            (bool?)package["partialAndUnaccepted"] != true)
            throw new BuildFailedException("Original package inventory is not the failed attempt.");
        var files = package["files"] as JArray;
        var output = Field(prior, "outputRoot");
        if (files == null || files.Count == 0 || (int?)package["fileCount"] != files.Count ||
            Directory.GetFiles(output, "*", SearchOption.AllDirectories).Length != files.Count)
            throw new BuildFailedException("Original package file set changed.");
        long sum = 0;
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in files)
        {
            var relative = Field((JObject)item, "path");
            if (relative.Contains(":") || relative.Contains("..") || Path.IsPathRooted(relative) || !seen.Add(relative))
                throw new BuildFailedException("Unsafe or duplicate original package path.");
            var file = Path.Combine(output, relative);
            Rel(output, file); NoLinks(file);
            if (!File.Exists(file) || new FileInfo(file).Length != (long?)item["bytes"] ||
                HashFile(file) != (string)item["sha256"])
                throw new BuildFailedException("Original package file drift: " + relative);
            sum += (long)item["bytes"];
        }
        if (sum != (long?)package["totalBytes"]) throw new BuildFailedException("Original package total bytes changed.");
        return files;
    }
    public static JObject RegisterRecovery(string requestPath)
    {
        return RegisterPhase(requestPath, "Recovery", fresh => RecoveryCore(fresh, requestPath));
    }
    private static JObject RecoveryCore(JObject request, string requestPath)
    {
        var from = request["recoveredFrom"] as JObject;
        if (from == null) throw new BuildFailedException("Recovery source binding missing.");
        var priorPath = Field(from, "requestPath");
        var prior = Request(priorPath);
        if (HashFile(priorPath) != Field(from, "requestSha256") ||
            Field(prior, "taskId") != Field(request, "taskId") ||
            (int)prior["attempt"] != (int)request["attempt"] - 1 ||
            Field(prior, "sessionId") != Field(request, "sessionId") ||
            Field(prior, "outputRoot") != Field(request, "outputRoot") ||
            Field(prior, "version") != Field(request, "version") ||
            Field(prior, "buildNumber") != Field(request, "buildNumber") ||
            Field(prior, "configuration") != "Release")
            throw new BuildFailedException("Original build request does not bind this recovery.");
        var reviewPath = Field(from, "controlReviewPath");
        Rel(Path.Combine(Project, ".ai-workspace", "outputs", "control"), reviewPath);
        if (HashFile(reviewPath) != Field(from, "controlReviewSha256"))
            throw new BuildFailedException("CONTROL recovery review changed.");
        var review = Read(reviewPath);
        if ((int?)review["schemaVersion"] != 1 || (string)review["decision"] != "PASSED" ||
            (string)review["priorTaskId"] != Field(prior, "taskId") ||
            (int?)review["priorAttempt"] != (int)prior["attempt"] ||
            (string)review["priorExecutionId"] != Field(prior, "executionId") ||
            (string)review["recoveryTaskId"] != Field(request, "taskId") ||
            (int?)review["recoveryAttempt"] != (int)request["attempt"] ||
            (string)review["recoveryExecutionId"] != Field(request, "executionId") ||
            (string)review["priorRequestSha256"] != HashFile(priorPath))
            throw new BuildFailedException("CONTROL recovery review identity changed.");
        var oldEvidence = Field(prior, "evidenceRoot");
        if (HashFile(Path.Combine(oldEvidence, "staging-manifest.json")) != Field(review, "priorManifestSha256") ||
            HashFile(Path.Combine(oldEvidence, "prepare-result.json")) != Field(review, "priorPrepareSha256") ||
            HashFile(Path.Combine(oldEvidence, "build-registered.json")) != Field(review, "priorBuildRegistrationSha256") ||
            HashFile(Path.Combine(oldEvidence, "build-result.json")) != Field(review, "priorFailedResultSha256"))
            throw new BuildFailedException("Original stage, prepare or failed Build evidence changed.");
        var failed = Read(Path.Combine(oldEvidence, "build-result.json"));
        var registered = Read(Path.Combine(oldEvidence, "build-registered.json"));
        if ((bool?)failed["success"] != false || (string)failed["executionId"] != Field(prior, "executionId") ||
            (string)failed["requestSha256"] != HashFile(priorPath) ||
            (string)failed["manifestSha256"] != Field(review, "priorManifestSha256") ||
            (string)registered["executionId"] != Field(prior, "executionId") ||
            (int?)registered["pid"] != System.Diagnostics.Process.GetCurrentProcess().Id ||
            (string)registered["requestSha256"] != HashFile(priorPath))
            throw new BuildFailedException("Original failed callback or Editor PID changed.");
        var journalPath = Path.Combine(Project, "Library/CatalogBlobBuild.journal.json");
        if (HashFile(journalPath) != Field(review, "priorJournalSha256"))
            throw new BuildFailedException("Original ownership journal changed.");
        var journal = SaveEnvelope.ParseCanonicalObject(File.ReadAllText(journalPath, Utf8));
        if ((int?)journal["schema"] != CatalogBuildPlan.OwnershipSchema ||
            journal["pending"] == null || journal["pending"].Type != JTokenType.Null ||
            journal["owned"] == null || journal["owned"].Type != JTokenType.Array)
            throw new BuildFailedException("Original ownership journal is incomplete.");
        var owned = new List<CatalogBuildPlan.OwnedInput>();
        var ownedFiles = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in (JArray)journal["owned"])
        {
            var relative = Field((JObject)item, "path");
            owned.Add(new CatalogBuildPlan.OwnedInput(relative, (bool)item["directory"],
                (string)item["sha256"], Field((JObject)item, "metaSha256")));
            ownedFiles.Add(relative); ownedFiles.Add(relative + ".meta");
        }
        CatalogBuildPlan.PlanOwnedCleanup(Project, Field(journal, "root"),
            (int)journal["schema"], null, owned);
        ValidateRecoveryInputs(prior, review, ownedFiles);
        if (HashFile(Path.Combine(oldEvidence, "staging-manifest.json")) != Field(review, "priorManifestSha256"))
            throw new BuildFailedException("Original staging manifest changed.");
        var staged = Read(Path.Combine(Field(prior, "staging"), "catalog.graph.json"));
        var stageManifest = Read(Path.Combine(oldEvidence, "staging-manifest.json"));
        var stageNames = new[] { "catalog.graph.json", "catalog.bin", "CatalogBuildFingerprint.g.cs", "export.json" };
        var stageFiles = stageManifest["files"] as JArray;
        if (stageFiles == null || stageFiles.Count != 4 ||
            (string)stageManifest["inputManifestSha256"] != Field(review, "priorInputManifestSha256"))
            throw new BuildFailedException("Original four-stage manifest incomplete.");
        for (var i = 0; i < 4; i++)
        {
            var file = Path.Combine(Field(prior, "staging"), stageNames[i]);
            if ((string)stageFiles[i]["path"] != stageNames[i] ||
                (long?)stageFiles[i]["bytes"] != new FileInfo(file).Length ||
                (string)stageFiles[i]["sha256"] != HashFile(file))
                throw new BuildFailedException("Original staged file changed.");
        }
        if ((string)stageManifest["fingerprint"] != ValidateCatalog(prior))
            throw new BuildFailedException("Original authenticated catalog changed.");
        var package = ValidateRecoveryPackage(prior, review);
        var reportBinary = Path.Combine(Project, "Library/LastBuild.buildreport");
        if (HashFile(reportBinary) != Field(review, "priorReportSha256"))
            throw new BuildFailedException("Original Unity BuildReport binary changed.");
        var report = BuildReport.GetLatestReport();
        if (report == null || report.summary.result != BuildResult.Succeeded || report.summary.totalErrors != 0 ||
            report.summary.platform != BuildTarget.StandaloneWindows64 ||
            (report.summary.options & BuildOptions.DetailedBuildReport) == 0 ||
            (report.summary.options & BuildOptions.Development) != 0 ||
            !Path.GetFullPath(report.summary.outputPath).Equals(Path.GetFullPath(Field(prior, "exe")), StringComparison.OrdinalIgnoreCase))
            throw new BuildFailedException("Latest native BuildReport is not this successful Release.");
        var started = DateTimeOffset.Parse(Field(failed, "startedAt"));
        var ended = DateTimeOffset.Parse(Field(failed, "endedAt"));
        var reportStarted = new DateTimeOffset(report.summary.buildStartedAt.ToUniversalTime());
        var reportEnded = reportStarted + report.summary.totalTime;
        if (reportStarted < started || reportEnded > ended || reportEnded < reportStarted)
            throw new BuildFailedException("Latest BuildReport time does not bind the original callback.");
        CatalogBlobBuildProcessor.AssertExistingReport(report, staged);
        var sdk = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Unity.Pipeline.Editor")
            .GetType("Unity.Pipeline.Editor.Commands.Build.BuildCommand", true);
        var projector = sdk.GetMethod("Project", BindingFlags.Static | BindingFlags.NonPublic, null,
            new[] { typeof(string), typeof(BuildReport) }, null);
        if (projector == null || projector.ReturnType.FullName != "Unity.Pipeline.Editor.Commands.Build.BuildReportResult")
            throw new BuildFailedException("Installed SDK BuildReport projector changed.");
        var projected = JObject.FromObject(projector.Invoke(null,
            new object[] { Field(prior, "taskId") + "-a" + (int)prior["attempt"], report }));
        if ((string)projected["result"] != "Succeeded" || (int?)projected["totalErrors"] != 0 ||
            projected["packedAssets"] == null || projected["files"] == null)
            throw new BuildFailedException("Full original BuildReport projection unavailable.");
        var current = SceneManager.GetActiveScene();
        var prepared = Read(Path.Combine(oldEvidence, "prepare-result.json"));
        if ((string)prepared["executionId"] != Field(prior, "executionId") ||
            (string)prepared["requestSha256"] != HashFile(priorPath) ||
            (string)prepared["manifestSha256"] != Field(review, "priorManifestSha256") ||
            (int?)prepared["pid"] != System.Diagnostics.Process.GetCurrentProcess().Id ||
            (string)prepared["settingsAfterSha256"] != HashFile(Path.Combine(Project, "ProjectSettings/ProjectSettings.asset")) ||
            (string)prepared["activeScenePath"] != "" ||
            (prepared["loadedScenes"] as JArray)?.Count != 1 ||
            (string)prepared["loadedScenes"][0]["path"] != "" ||
            (bool?)prepared["loadedScenes"][0]["dirty"] != false ||
            SceneManager.sceneCount != 1 || !current.IsValid() || !current.isLoaded ||
            current.isDirty || current.path != "" || current.GetRootGameObjects().Length != 0)
            throw new BuildFailedException("Clean rootless current scene or original recorded setup is unproven.");
        var evidence = Field(request, "evidenceRoot");
        var raw = Utf8.GetBytes(projected.ToString(Formatting.None));
        var reportPath = Path.Combine(evidence, "BuildReport.json.gz");
        using (var file = new FileStream(reportPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
        using (var gzip = new GZipStream(file, CompressionMode.Compress)) gzip.Write(raw, 0, raw.Length);
        WriteNew(Path.Combine(evidence, "BuildReport.map.json"), new JObject {
            ["taskId"] = Field(request, "taskId"), ["attempt"] = (int)request["attempt"],
            ["executionId"] = Field(request, "executionId"), ["recoveredFromExecutionId"] = Field(prior, "executionId"),
            ["priorReportSha256"] = HashFile(reportBinary), ["requestSha256"] = HashFile(requestPath),
            ["controlReviewSha256"] = HashFile(reviewPath), ["path"] = reportPath,
            ["format"] = "gzip-json-utf8-lossless", ["rawBytes"] = raw.Length,
            ["rawSha256"] = Hash(raw), ["compressedSha256"] = HashFile(reportPath)
        });
        WriteNew(Path.Combine(evidence, "package-files.json"), new JObject { ["files"] = package.DeepClone() });
        CatalogBlobBuildProcessor.RecoverOwnedInputs();
        if (File.Exists(journalPath) || File.Exists(Path.Combine(Project, CatalogBlobBuildProcessor.BlobPath)) ||
            Directory.GetDirectories(Path.Combine(Project, "Assets"), "__CatalogBlobBuild_*").Length != 0 ||
            Directory.GetFiles(Path.Combine(Project, "Assets"), "__CatalogBlobBuild_*.meta").Length != 0)
            throw new BuildFailedException("Owned build input remains after strict recovery.");
        ValidateRecoveryInputs(prior, review, new HashSet<string>(StringComparer.Ordinal));
        ValidateRecoveryPackage(prior, review);
        if (HashFile(reportBinary) != Field(review, "priorReportSha256") ||
            SceneManager.sceneCount != 1 || SceneManager.GetActiveScene().isDirty ||
            SceneManager.GetActiveScene().GetRootGameObjects().Length != 0)
            throw new BuildFailedException("Original report, package or authoring scene changed after cleanup.");
        var result = new JObject {
            ["taskId"] = Field(request, "taskId"), ["attempt"] = (int)request["attempt"],
            ["executionId"] = Field(request, "executionId"), ["ownerThreadId"] = Field(request, "ownerThreadId"),
            ["sessionId"] = Field(request, "sessionId"), ["requestSha256"] = HashFile(requestPath),
            ["recoveredFromExecutionId"] = Field(prior, "executionId"),
            ["priorRequestSha256"] = HashFile(priorPath), ["priorFailedResultSha256"] = Field(review, "priorFailedResultSha256"),
            ["priorReportSha256"] = HashFile(reportBinary), ["controlReviewSha256"] = HashFile(reviewPath),
            ["nativeResult"] = "Succeeded", ["errors"] = 0, ["buildCalls"] = 0, ["sourceBuildCalls"] = 1,
            ["saveAssetsCalls"] = 0, ["sourceWrites"] = 0, ["journalExists"] = false,
            ["packageFilesSha256"] = HashFile(Path.Combine(evidence, "package-files.json")),
            ["buildReportMapSha256"] = HashFile(Path.Combine(evidence, "BuildReport.map.json")),
            ["settingsAfterSha256"] = HashFile(Path.Combine(Project, "ProjectSettings/ProjectSettings.asset")),
            ["sourceEditorPid"] = System.Diagnostics.Process.GetCurrentProcess().Id, ["success"] = true
        };
        WriteNew(Path.Combine(evidence, "recovery-result.json"), result);
        return result;
    }
    public static JObject RegisterReadback(string requestPath, string pinnedManifestSha)
    {
        return RegisterPhase(requestPath, "Readback", fresh => ReadbackCore(fresh, requestPath, pinnedManifestSha));
    }
    private static JObject ReadbackCore(JObject request, string requestPath, string pinnedManifestSha)
    {
        ValidateSealed(request, pinnedManifestSha);
        var evidence = Field(request, "evidenceRoot");
        var result = Read(Path.Combine(evidence, "build-result.json"));
        var mapPath = Path.Combine(evidence, "BuildReport.map.json");
        var map = Read(mapPath);
        if (!Path.GetFullPath(Field(map, "path")).Equals(Path.GetFullPath(Path.Combine(evidence, "BuildReport.json.gz")), StringComparison.OrdinalIgnoreCase))
            throw new BuildFailedException("BuildReport path escaped task evidence.");
        if ((bool?)result["success"] != true || (string)result["nativeResult"] != "Succeeded" ||
            (int?)result["errors"] != 0 || (int?)result["buildCalls"] != 1 ||
            (string)result["taskId"] != Field(request, "taskId") ||
            (int?)result["attempt"] != (int)request["attempt"] ||
            (string)result["ownerThreadId"] != Field(request, "ownerThreadId") ||
            (string)result["sessionId"] != Field(request, "sessionId") ||
            (string)result["requestSha256"] != HashFile(requestPath) ||
            (string)result["manifestSha256"] != pinnedManifestSha.ToUpperInvariant() ||
            (string)result["inputManifestSha256"] != HashFile(Path.Combine(evidence, "input-manifest.json")) ||
            (string)result["executionId"] != Field(request, "executionId") ||
            (string)result["version"] != PlayerSettings.bundleVersion ||
            (string)result["buildNumber"] != Field(request, "buildNumber") ||
            (string)result["configuration"] != Field(request, "configuration") ||
            (string)result["exeSha256"] != HashFile(Field(request, "exe")) ||
            (string)result["buildReportMapSha256"] != HashFile(mapPath) ||
            (string)map["taskId"] != Field(request, "taskId") ||
            (int?)map["attempt"] != (int)request["attempt"] ||
            (string)map["executionId"] != Field(request, "executionId") ||
            (string)map["requestSha256"] != HashFile(requestPath) ||
            (string)map["manifestSha256"] != pinnedManifestSha.ToUpperInvariant() ||
            (string)map["inputManifestSha256"] != HashFile(Path.Combine(evidence, "input-manifest.json")) ||
            (string)map["buildNumber"] != Field(request, "buildNumber") ||
            (string)map["configuration"] != Field(request, "configuration") ||
            (string)map["format"] != "gzip-json-utf8-lossless" ||
            (string)map["compressedSha256"] != HashFile(Field(map, "path")))
            throw new BuildFailedException("Native result or report evidence changed.");
        byte[] raw;
        using (var file = File.OpenRead(Field(map, "path")))
        using (var gzip = new GZipStream(file, CompressionMode.Decompress))
        using (var memory = new MemoryStream()) { gzip.CopyTo(memory); raw = memory.ToArray(); }
        if (raw.Length != (int)map["rawBytes"] || Hash(raw) != (string)map["rawSha256"] || JObject.Parse(Utf8.GetString(raw)) == null)
            throw new BuildFailedException("Lossless BuildReport readback failed.");
        var packagePath = Path.Combine(evidence, "package-files.json");
        if (HashFile(packagePath) != (string)result["packageFilesSha256"]) throw new BuildFailedException("Package manifest changed.");
        var package = Read(packagePath)["files"] as JArray;
        var output = Field(request, "outputRoot");
        if (package == null || package.Count == 0 || Directory.GetFiles(output, "*", SearchOption.AllDirectories).Length != package.Count)
            throw new BuildFailedException("Package file count changed.");
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in package)
        {
            if (!seen.Add((string)entry["path"])) throw new BuildFailedException("Duplicate package path.");
            var file = Path.Combine(output, (string)entry["path"]);
            NoLinks(file); Rel(output, file);
            if (!File.Exists(file) || new FileInfo(file).Length != (long)entry["bytes"] || HashFile(file) != (string)entry["sha256"])
                throw new BuildFailedException("Package file changed: " + (string)entry["path"]);
        }
        if (Field(request, "configuration") == "Release")
        {
            var builtCatalog = Path.Combine(output, "EatWhat_Data/StreamingAssets/data/catalog.bin");
            if (HashFile(builtCatalog) != HashFile(Path.Combine(Field(request, "staging"), "catalog.bin")))
                throw new BuildFailedException("Release catalog differs from authenticated staging.");
        }
        if (Directory.GetDirectories(Path.Combine(Project, "Assets"), "__CatalogBlobBuild_*").Length != 0 ||
            Directory.GetFiles(Path.Combine(Project, "Assets"), "__CatalogBlobBuild_*.meta").Length != 0)
            throw new BuildFailedException("Build-owned root remains; preserve.");
        var prepared = Read(Path.Combine(evidence, "prepare-result.json"));
        if ((string)prepared["settingsAfterSha256"] != HashFile(Path.Combine(Project, "ProjectSettings/ProjectSettings.asset")) ||
            (string)prepared["inputManifestSha256"] != HashFile(Path.Combine(evidence, "input-manifest.json")))
            throw new BuildFailedException("Post-Build settings or input identity changed.");
        var beforeScenes = prepared["loadedScenes"] as JArray;
        if (beforeScenes == null || beforeScenes.Count != SceneManager.sceneCount ||
            (string)prepared["activeScenePath"] != SceneManager.GetActiveScene().path)
            throw new BuildFailedException("Authoring scene setup changed.");
        for (var i = 0; i < SceneManager.sceneCount; i++)
            if ((string)beforeScenes[i]["path"] != SceneManager.GetSceneAt(i).path || SceneManager.GetSceneAt(i).isDirty)
                throw new BuildFailedException("Authoring scene changed after Build.");
        var proof = new JObject {
            ["taskId"] = Field(request, "taskId"), ["attempt"] = (int)request["attempt"],
            ["executionId"] = Field(request, "executionId"),
            ["ownerThreadId"] = Field(request, "ownerThreadId"), ["sessionId"] = Field(request, "sessionId"),
            ["requestSha256"] = HashFile(requestPath),
            ["manifestSha256"] = pinnedManifestSha.ToUpperInvariant(), ["version"] = Field(request, "version"),
            ["inputManifestSha256"] = HashFile(Path.Combine(evidence, "input-manifest.json")),
            ["settingsAfterSha256"] = (string)prepared["settingsAfterSha256"],
            ["buildNumber"] = Field(request, "buildNumber"), ["configuration"] = Field(request, "configuration"),
            ["nativeResult"] = "Succeeded", ["errors"] = 0, ["packageFiles"] = package.Count,
            ["packageFilesSha256"] = HashFile(packagePath), ["buildReportMapSha256"] = HashFile(mapPath),
            ["sourceWrites"] = 0, ["journalExists"] = false, ["readbackAt"] = DateTimeOffset.UtcNow.ToString("O")
        };
        WriteNew(Path.Combine(evidence, "final-readback.json"), proof);
        return proof;
    }
}
