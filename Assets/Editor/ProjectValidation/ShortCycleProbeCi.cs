using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using EatWhat.Cooking.ShortCycle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Batchmode entry only. The ordinary validation launcher retains ownership of Play and scene recovery.
[InitializeOnLoad]
public static class ShortCycleProbeCi
{
    private const string Key = "EatWhat.Validation.ProbeCi.State";
    private const string LauncherKey = "EatWhat.Validation.Isolation.";
    private const string Target = ShortCycleValidationBootstrap.TargetScene;
    [Serializable]
    private sealed class Run
    {
        public string taskId, runId, executionId, resultPath, isolatedRoot, startedAt, endedAt;
        public int attempt, timeoutSeconds;
        public bool injectFailure, resultPathValidated, armRequested, launcherOwned;
        public bool probeStarted, probeCompleted, probeSucceeded, auditPassed;
        public bool probePrepared, probeDetached, probeRestored, probeDispatchRequested;
        public int probeInstanceId, parentInstanceId, probeSiblingIndex;
        public Vector3 probeLocalPosition, probeLocalScale;
        public Quaternion probeLocalRotation;
        public bool probeActiveSelf, probeEnabled;
        public string initialSceneKind, initialSceneSummary, initialSceneRestoration;
        public int initialSetupCount, initialLoadedCount, initialEmptyHandle, temporaryAnchorHandle;
        public bool temporaryAnchor, temporaryAnchorOpened, temporaryAnchorRestored;
        public bool sceneRestored, isolationReleased, targetDestroyedBeforeLeaseRelease;
        public string auditReport, summary, error, phase;
        public string[] checkpoints;
    }

    static ShortCycleProbeCi()
    {
        EditorApplication.update -= Update;
        EditorApplication.update += Update;
        Application.logMessageReceived -= OnLog;
        Application.logMessageReceived += OnLog;
    }

    public static void Start()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Probe CI requires batchmode.");
        if (!string.IsNullOrEmpty(SessionState.GetString(Key, "")))
            throw new InvalidOperationException("An earlier Probe CI run is still pending recovery.");
        var args = Environment.GetCommandLineArgs();
        var run = new Run { phase = "starting", startedAt = DateTime.UtcNow.ToString("O") };
        try
        {
            run.taskId = Arg(args, "-ewCiTask");
            run.runId = Arg(args, "-ewCiRun");
            run.executionId = Arg(args, "-ewCiExecution");
            run.resultPath = Arg(args, "-ewCiResult");
            run.isolatedRoot = Arg(args, "-ewCiIsolatedRoot");
            if (!int.TryParse(Arg(args, "-ewCiAttempt"), out run.attempt) || run.attempt < 1 ||
                !int.TryParse(Arg(args, "-ewCiTimeoutSeconds"), out run.timeoutSeconds) ||
                run.timeoutSeconds < 30 || run.timeoutSeconds > 900 ||
                !Regex.IsMatch(run.executionId, "^[0-9a-f]{32}$") ||
                !Regex.IsMatch(run.taskId, "^AI-[0-9]{6}$") ||
                !Regex.IsMatch(run.runId, "^[A-Za-z0-9][A-Za-z0-9_-]{0,63}$"))
                throw new ArgumentException("Invalid Probe CI identity or timeout.");
            run.injectFailure = args.Count(a => a == "-ewCiInjectFailure") == 1;
            if (args.Count(a => a == "-ewCiInjectFailure") > 1) throw new ArgumentException("Duplicate failure injection flag.");
            var request = new ShortCycleValidationBootstrap.Request { Enabled = true, TaskId = run.taskId,
                RunId = run.runId, RootPath = run.isolatedRoot };
            run.isolatedRoot = ShortCycleValidationBootstrap.ValidateRequest(request);
            if (!string.Equals(Path.GetFullPath(Application.dataPath),
                Path.GetFullPath(Path.Combine(ShortCycleValidationBootstrap.Workspace, "Assets")), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Probe CI is bound to the approved project.");
            if (Directory.Exists(run.isolatedRoot) || File.Exists(run.isolatedRoot))
                throw new IOException("Probe CI requires a fresh isolation root.");
            var expected = Path.GetFullPath(Path.Combine(ShortCycleValidationBootstrap.Workspace,
                ".ai-workspace/outputs/BATCH2-INDEPENDENT-20261023", run.taskId, "ci-runs", run.runId, "unity-result.json"));
            if (!string.Equals(Path.GetFullPath(run.resultPath), expected, StringComparison.OrdinalIgnoreCase) ||
                File.Exists(expected) || !Directory.Exists(Path.GetDirectoryName(expected)))
                throw new IOException("Result must be a fresh file in the exact task/run output directory.");
            EnsureUnredirected(Path.GetDirectoryName(expected));
            run.resultPathValidated = true;
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating ||
                !ShortCycleValidationLauncher.IsRestored)
                throw new InvalidOperationException("A settled, restored Edit Mode is required.");
            var original = EditorSceneManager.GetSceneManagerSetup();
            ClassifyInitialScenes(run, original);
            Save(run);
            try
            {
                var opened = EditorSceneManager.OpenScene(Target);
                if (run.temporaryAnchor)
                {
                    if (!opened.isLoaded || opened.path != Target || opened.isDirty ||
                        SceneManager.sceneCount != 1 || SceneManager.GetActiveScene().handle != opened.handle)
                        throw new InvalidOperationException("Temporary saved scene anchor identity is unproven.");
                    run.temporaryAnchorHandle = opened.handle;
                    run.temporaryAnchorOpened = true;
                    Save(run);
                }
                var auditType = Type.GetType("ShortCycleLayoutAudit, Editor.ShortCycle", false);
                var auditMethod = auditType == null ? null : auditType.GetMethod("AuditActiveScene");
                if (auditMethod == null || !auditMethod.IsPublic || !auditMethod.IsStatic || auditMethod.ReturnType != typeof(string))
                    throw new MissingMethodException("Current read-only ShortCycleLayoutAudit.AuditActiveScene unavailable.");
                run.auditReport = (string)auditMethod.Invoke(null, null);
                run.auditPassed = AuditPassed(run.auditReport);
                var probes = Resources.FindObjectsOfTypeAll<ShortCycleAutomatedProbe>()
                    .Where(p => p != null && p.gameObject.scene.path == Target).ToArray();
                var autoStart = probes.Length == 1 ? new SerializedObject(probes[0]).FindProperty("runOnStart") : null;
                if (autoStart == null || autoStart.boolValue)
                    throw new InvalidOperationException("One original probe with automatic Start disabled is required.");
            }
            finally { if (!run.temporaryAnchor) EditorSceneManager.RestoreSceneManagerSetup(original); }
            Save(run);
            if (!run.auditPassed) { Fail(run, "LAYOUT_AUDIT_FAILED"); return; }
            if (SameLauncher(run))
                throw new InvalidOperationException("A stale Launcher identity already matches this CI run.");
            run.armRequested = true;
            Save(run); // Survives a reload or an exception partway through ArmAndPlay.
            try { ShortCycleValidationLauncher.ArmAndPlay(run.taskId, run.runId, run.isolatedRoot); }
            finally { if (OwnsActiveLauncher(run)) { run.launcherOwned = true; Save(run); } }
            if (!run.launcherOwned) throw new InvalidOperationException("Launcher did not retain this CI run identity.");
            run.phase = "waiting-ready";
            Save(run);
        }
        catch (Exception error)
        {
            run.error = error.GetType().Name + ": " + error.Message;
            if (!run.resultPathValidated) { EditorApplication.Exit(2); return; }
            if (run.armRequested && OwnsActiveLauncher(run)) run.launcherOwned = true;
            Save(run);
            Fail(run, run.error);
        }
    }

    private static void ClassifyInitialScenes(Run run, SceneSetup[] setup)
    {
        run.initialSetupCount = setup.Length;
        run.initialLoadedCount = SceneManager.sceneCount;
        var loaded = Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt).ToArray();
        var active = SceneManager.GetActiveScene();
        run.initialSceneSummary = "setup=" + setup.Length + "; loaded=" + loaded.Length +
            "; active=" + active.path + "; scenes=" + string.Join(",", loaded.Select(s =>
                "[" + s.path + ";dirty=" + s.isDirty + ";roots=" + s.GetRootGameObjects().Length +
                ";handle=" + s.handle + "]"));
        if (loaded.Any(s => s.isDirty))
        {
            run.initialSceneKind = "dirty-rejected";
            throw new InvalidOperationException("Dirty original scene; " + run.initialSceneSummary);
        }
        var empty = loaded.Length == 1 && string.IsNullOrEmpty(loaded[0].path) &&
            loaded[0].GetRootGameObjects().Length == 0 && active.handle == loaded[0].handle &&
            (setup.Length == 0 || setup.Length == 1 && string.IsNullOrEmpty(setup[0].path) &&
             setup[0].isLoaded && setup[0].isActive);
        if (empty)
        {
            run.initialSceneKind = "clean-empty-untitled";
            run.initialEmptyHandle = loaded[0].handle;
            run.temporaryAnchor = true;
            return;
        }
        if (loaded.Length == 0 && setup.Length == 0)
        {
            run.initialSceneKind = "no-loaded-empty";
            run.temporaryAnchor = true;
            return;
        }
        if (loaded.Length == 0)
        {
            run.initialSceneKind = "no-loaded-scene-rejected";
            throw new InvalidOperationException("No loaded scene has a provable restoration state; " + run.initialSceneSummary);
        }
        if (loaded.Any(s => string.IsNullOrEmpty(s.path)))
        {
            run.initialSceneKind = "untitled-or-mixed-rejected";
            throw new InvalidOperationException("Untitled scene contains content or mixed setup; " + run.initialSceneSummary);
        }
        if (setup.Length == 0 || setup.Count(s => s.isLoaded) != loaded.Length ||
            setup.Count(s => s.isActive) != 1 || setup.Any(s => s.isActive && !s.isLoaded) ||
            setup.Any(s => string.IsNullOrEmpty(s.path)) ||
            loaded.Any(s => !setup.Any(x => x.isLoaded && x.path == s.path)) ||
            !setup.Any(s => s.isActive && s.path == active.path))
        {
            run.initialSceneKind = "inconsistent-setup-rejected";
            throw new InvalidOperationException("Original saved scene setup is inconsistent; " + run.initialSceneSummary);
        }
        run.initialSceneKind = "saved-clean";
    }

    private static void RestoreInitialEmptyScene(Run run)
    {
        if (!run.temporaryAnchor || run.temporaryAnchorRestored) return;
        if (!ShortCycleValidationLauncher.IsRestored || EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Temporary scene anchor cannot be restored in the current Editor state.");
        if (run.initialSceneKind == "no-loaded-empty" && !run.temporaryAnchorOpened && SceneManager.sceneCount == 0)
        {
            run.initialSceneRestoration = "original-no-loaded";
            run.temporaryAnchorRestored = true;
            Save(run);
            return;
        }
        if (SceneManager.sceneCount != 1)
            throw new InvalidOperationException("Temporary scene anchor scene count changed; preserve it.");
        var current = SceneManager.GetSceneAt(0);
        if (current.handle != SceneManager.GetActiveScene().handle || current.isDirty)
            throw new InvalidOperationException("Temporary scene anchor changed or became dirty; preserve it.");
        if (string.IsNullOrEmpty(current.path) && current.GetRootGameObjects().Length == 0)
        {
            if (run.temporaryAnchorOpened || current.handle != run.initialEmptyHandle)
                throw new InvalidOperationException("A different empty scene is present; preserve its ownership.");
            run.initialSceneRestoration = "original-empty-untitled";
            run.temporaryAnchorRestored = true;
            Save(run);
            return;
        }
        if (!run.temporaryAnchorOpened || current.path != Target ||
            EditorSceneManager.GetSceneManagerSetup().Length != 1)
            throw new InvalidOperationException("Temporary scene anchor ownership is unproven; preserve it.");
        var empty = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        if (SceneManager.sceneCount != 1 || empty.handle != SceneManager.GetActiveScene().handle ||
            !string.IsNullOrEmpty(empty.path) || empty.isDirty || empty.GetRootGameObjects().Length != 0)
            throw new InvalidOperationException("Initial empty scene could not be restored semantically; preserve it.");
        run.initialSceneRestoration = run.initialSceneKind == "no-loaded-empty" ?
            "clean-empty-editor-substitute-for-no-loaded" : "clean-empty-untitled-semantic";
        run.temporaryAnchorRestored = true;
        Save(run);
    }

    private static bool SameLauncher(Run run)
    {
        return run != null &&
            string.Equals(SessionState.GetString(LauncherKey + "task", ""), run.taskId, StringComparison.Ordinal) &&
            string.Equals(SessionState.GetString(LauncherKey + "run", ""), run.runId, StringComparison.Ordinal) &&
            string.Equals(SessionState.GetString(LauncherKey + "root", ""), run.isolatedRoot, StringComparison.OrdinalIgnoreCase) &&
            (!ShortCycleValidationBootstrap.IsRunning ||
             string.Equals(ShortCycleValidationBootstrap.IsolatedRoot, run.isolatedRoot, StringComparison.OrdinalIgnoreCase));
    }

    private static bool OwnsActiveLauncher(Run run)
    {
        return run.armRequested && SameLauncher(run) &&
            (SessionState.GetBool(LauncherKey + "armed", false) ||
             SessionState.GetBool(LauncherKey + "entered", false) ||
             !string.IsNullOrEmpty(SessionState.GetString(LauncherKey + "restore", "")) ||
             ShortCycleValidationBootstrap.IsRunning);
    }

    private static void EnsureUnredirected(string path)
    {
        for (var directory = new DirectoryInfo(path); directory != null; directory = directory.Parent)
            if (directory.Exists && (directory.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Redirected Probe CI result directory.");
    }

    private static string Arg(string[] args, string name)
    {
        var indices = Enumerable.Range(0, args.Length).Where(i => args[i] == name).ToArray();
        if (indices.Length != 1 || indices[0] + 1 >= args.Length || args[indices[0] + 1].StartsWith("-", StringComparison.Ordinal))
            throw new ArgumentException("Missing or duplicate " + name);
        return args[indices[0] + 1];
    }

    private static bool AuditPassed(string report)
    {
        if (string.IsNullOrEmpty(report) || !report.StartsWith("CK01-C-LAYOUT-AUDIT scene=" + Target + " ", StringComparison.Ordinal)) return false;
        foreach (var token in new[] { "rootOrderValid=True", "forbidden=0", "missingScripts=0",
            "spacesStaticFontValid=True", "heavyTMPRendererEnabledValid=True", "tmpOverflowValid=True", "kTakeoverValid=True" })
            if (!Regex.IsMatch(report, "(?:^| )" + Regex.Escape(token) + "(?: |$)")) return false;
        return true;
    }

    private static Run Read()
    {
        var json = SessionState.GetString(Key, "");
        return string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<Run>(json);
    }
    private static void Save(Run run) { SessionState.SetString(Key, JsonUtility.ToJson(run)); }
    private static void OnLog(string condition, string stack, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
        var run = Read();
        if (run == null || run.phase == "finished" || run.phase == "stopping") return;
        if (string.IsNullOrEmpty(run.error)) run.error = "UNITY_LOG_" + type + ": " + condition + "\n" + stack;
        Save(run);
    }
    private static void Update()
    {
        if (!Application.isBatchMode) return;
        var run = Read();
        if (run == null || run.phase == "finished") return;
        try
        {
            if (run.armRequested && !run.launcherOwned)
            {
                if (!OwnsActiveLauncher(run)) { Fail(run, "LAUNCHER_OWNERSHIP_UNPROVEN"); return; }
                run.launcherOwned = true;
                if (run.phase == "starting") run.phase = "waiting-ready";
                Save(run);
            }
            if (run.launcherOwned && !SameLauncher(run)) { Fail(run, "LAUNCHER_IDENTITY_CHANGED"); return; }
            if (DateTime.UtcNow > DateTime.Parse(run.startedAt).ToUniversalTime().AddSeconds(run.timeoutSeconds))
            {
                if (run.phase == "stopping")
                {
                    run.error += "; RESTORE_TIMEOUT";
                    if (OwnsActiveLauncher(run))
                        try { ShortCycleValidationLauncher.Stop(); }
                        catch (Exception cleanup) { run.error += "; cleanup=" + cleanup; }
                    if (run.launcherOwned && SameLauncher(run) && ShortCycleValidationLauncher.IsRestored) CompleteRestoration(run);
                    else Finish(run);
                }
                else Fail(run, "PROBE_CI_TIMEOUT");
            }
            else if (run.phase == "waiting-ready")
            {
                if (!string.IsNullOrEmpty(run.error) || ShortCycleValidationBootstrap.State == "Faulted")
                    Fail(run, run.error ?? ShortCycleValidationBootstrap.LastError ?? "BOOTSTRAP_FAULTED");
                else if (ShortCycleValidationBootstrap.State == "Ready")
                {
                    PrepareOriginalProbe(run);
                    if (run.probeDispatchRequested)
                        throw new InvalidOperationException("Original probe dispatch was already requested for this run.");
                    run.probeDispatchRequested = true;
                    Save(run); // A reload must not dispatch the original probe twice.
                    ShortCycleValidationLauncher.RunOriginalProbe();
                    var observed = Read();
                    if (observed != null && !string.IsNullOrEmpty(observed.error)) run.error = observed.error;
                    run.probeStarted = true; run.phase = "running"; Save(run);
                }
            }
            else if (run.phase == "running")
            {
                if (!string.IsNullOrEmpty(run.error) || ShortCycleValidationBootstrap.State != "Ready")
                    Fail(run, run.error ?? "ISOLATION_ENDED_BEFORE_PROBE");
                else
                {
                    var probes = Resources.FindObjectsOfTypeAll<ShortCycleAutomatedProbe>()
                        .Where(p => p != null && p.gameObject.scene.path == Target).ToArray();
                    if (probes.Length != 1) Fail(run, "EXPECTED_ONE_ORIGINAL_PROBE");
                    else if (probes[0].AutomatedProbeCompleted)
                    {
                        run.probeCompleted = true;
                        run.probeSucceeded = probes[0].AutomatedProbeSucceeded;
                        run.checkpoints = probes[0].AutomatedProbeCheckpoints.ToArray();
                        run.summary = probes[0].AutomatedProbeSummary;
                        if (!run.probeSucceeded || !CompleteSequence(run.checkpoints) || string.IsNullOrEmpty(run.summary))
                            run.error = "ORIGINAL_PROBE_FAILED_OR_INCOMPLETE";
                        else if (run.injectFailure) run.error = "INJECTED_FAILURE";
                        Save(run);
                        Stop(run);
                    }
                }
            }
            else if (run.phase == "stopping")
            {
                if (run.probeDetached && !run.probeRestored && OwnsActiveLauncher(run))
                    RestoreOriginalProbe(run);
                if (ShortCycleValidationLauncher.IsRestored)
                    CompleteRestoration(run);
            }
        }
        catch (Exception error)
        {
            Fail(run, error.GetType().Name + ": " + error.Message);
        }
    }
    private static bool CompleteSequence(string[] checkpoints)
    {
        if (checkpoints == null || checkpoints.Length != 27) return false;
        for (var i = 0; i < 27; i++)
            if (!Regex.IsMatch(checkpoints[i] ?? "", "^" + (i + 1).ToString("00") + @"/\d+ PASS ")) return false;
        return true;
    }
    private static ShortCycleAutomatedProbe OwnedProbe(Run run)
    {
        if (!run.launcherOwned || !SameLauncher(run) || !ShortCycleValidationBootstrap.IsRunning ||
            SceneManager.GetActiveScene().path != Target)
            throw new InvalidOperationException("Probe scene or Launcher ownership is unproven.");
        var probes = Resources.FindObjectsOfTypeAll<ShortCycleAutomatedProbe>()
            .Where(p => p != null && p.gameObject.scene.path == Target).ToArray();
        if (probes.Length != 1 || (run.probePrepared && probes[0].GetInstanceID() != run.probeInstanceId))
            throw new InvalidOperationException("Original probe identity is unproven.");
        return probes[0];
    }
    private static void PrepareOriginalProbe(Run run)
    {
        if (ShortCycleValidationBootstrap.State != "Ready")
            throw new InvalidOperationException("Original probe is not ready.");
        var probe = OwnedProbe(run);
        if (run.probePrepared)
        {
            if (run.probeDetached && !run.probeRestored &&
                (probe.transform.parent != null || !probe.gameObject.activeInHierarchy))
                throw new InvalidOperationException("Prepared original probe hierarchy changed.");
            return;
        }
        var go = probe.gameObject;
        var autoStart = new SerializedObject(probe).FindProperty("runOnStart");
        if (autoStart == null || autoStart.boolValue || go.transform.childCount != 0 ||
            go.GetComponents<Component>().Length != 2 || go.GetComponent<Transform>() == null ||
            go.GetComponent<ShortCycleAutomatedProbe>() != probe)
            throw new InvalidOperationException("Original probe object layout changed.");
        run.probeInstanceId = probe.GetInstanceID();
        run.probePrepared = true;
        if (go.activeInHierarchy)
        {
            run.probeRestored = true;
            Save(run);
            return;
        }
        var parent = go.transform.parent;
        if (parent == null || !go.activeSelf || !probe.enabled || parent.gameObject.activeInHierarchy)
            throw new InvalidOperationException("Original probe has an unsupported inactive hierarchy.");
        run.parentInstanceId = parent.GetInstanceID();
        run.probeSiblingIndex = go.transform.GetSiblingIndex();
        run.probeLocalPosition = go.transform.localPosition;
        run.probeLocalRotation = go.transform.localRotation;
        run.probeLocalScale = go.transform.localScale;
        run.probeActiveSelf = go.activeSelf;
        run.probeEnabled = probe.enabled;
        run.probeDetached = true;
        Save(run); // Record the exact object and parent before changing hierarchy.
        go.transform.SetParent(null, true);
        if (!go.activeInHierarchy || go.scene.path != Target)
            throw new InvalidOperationException("Original probe did not become runnable.");
    }
    private static void RestoreOriginalProbe(Run run)
    {
        if (!run.probeDetached || run.probeRestored) return;
        var probe = OwnedProbe(run);
        var parent = EditorUtility.InstanceIDToObject(run.parentInstanceId) as Transform;
        if (parent == null || parent.gameObject.scene.path != Target ||
            probe.transform.parent != null && probe.transform.parent != parent)
            throw new InvalidOperationException("Original probe parent identity changed.");
        if (probe.transform.parent == null) probe.transform.SetParent(parent, true);
        probe.transform.SetSiblingIndex(run.probeSiblingIndex);
        probe.transform.localPosition = run.probeLocalPosition;
        probe.transform.localRotation = run.probeLocalRotation;
        probe.transform.localScale = run.probeLocalScale;
        probe.gameObject.SetActive(run.probeActiveSelf);
        probe.enabled = run.probeEnabled;
        if (probe.transform.parent != parent || probe.transform.GetSiblingIndex() != run.probeSiblingIndex ||
            probe.transform.localPosition != run.probeLocalPosition ||
            probe.transform.localRotation != run.probeLocalRotation ||
            probe.transform.localScale != run.probeLocalScale ||
            probe.gameObject.activeSelf != run.probeActiveSelf || probe.enabled != run.probeEnabled)
            throw new InvalidOperationException("Original probe hierarchy restoration could not be verified.");
        run.probeRestored = true;
        Save(run);
    }
    private static void Stop(Run run)
    {
        if (!run.launcherOwned || !OwnsActiveLauncher(run))
        {
            run.error = (run.error ?? "") + "; LAUNCHER_OWNERSHIP_UNPROVEN";
            Finish(run);
            return;
        }
        try { RestoreOriginalProbe(run); }
        catch (Exception restore)
        {
            run.error = (run.error ?? "") + "; probeRestore=" + restore.GetType().Name + ": " + restore.Message;
            Save(run);
        }
        run.phase = "stopping"; Save(run);
        ShortCycleValidationLauncher.Stop();
        if (ShortCycleValidationLauncher.IsRestored) CompleteRestoration(run);
    }
    private static void Fail(Run run, string reason)
    {
        if (string.IsNullOrEmpty(run.error)) run.error = reason;
        if (!run.launcherOwned && run.armRequested && OwnsActiveLauncher(run)) run.launcherOwned = true;
        if (!run.launcherOwned) { Finish(run); return; }
        if (!SameLauncher(run)) { run.error += "; LAUNCHER_IDENTITY_CHANGED"; Finish(run); return; }
        if (ShortCycleValidationLauncher.IsRestored) { CompleteRestoration(run); return; }
        if (!OwnsActiveLauncher(run)) { run.error += "; LAUNCHER_OWNERSHIP_UNPROVEN"; Finish(run); return; }
        if (run.phase != "stopping")
        {
            try
            {
                Stop(run);
            }
            catch (Exception cleanup)
            {
                run.error += "; cleanup=" + cleanup;
                if (ShortCycleValidationLauncher.IsRestored) CompleteRestoration(run);
                else Save(run);
            }
        }
        else { Save(run); }
    }
    private static void CompleteRestoration(Run run)
    {
        if (!run.launcherOwned || !SameLauncher(run) || !ShortCycleValidationLauncher.IsRestored)
        {
            run.error = (run.error ?? "") + "; LAUNCHER_RESTORATION_UNPROVEN";
            Finish(run);
            return;
        }
        try { RestoreInitialEmptyScene(run); }
        catch (Exception restore) { run.error = (run.error ?? "") + "; initialSceneRestore=" + restore.GetType().Name + ": " + restore.Message; }
        run.sceneRestored = !run.temporaryAnchor || run.temporaryAnchorRestored;
        run.isolationReleased = !ShortCycleValidationBootstrap.IsRunning;
        run.targetDestroyedBeforeLeaseRelease = run.isolationReleased &&
            File.Exists(Path.Combine(run.isolatedRoot, "validation-host.log")) &&
            File.ReadAllText(Path.Combine(run.isolatedRoot, "validation-host.log")).Contains("targetDestroyedBeforeLeaseRelease=true");
        Finish(run);
    }
    private static void Finish(Run run)
    {
        if (run.temporaryAnchor && !run.temporaryAnchorRestored)
            try { RestoreInitialEmptyScene(run); }
            catch (Exception restore) { run.error = (run.error ?? "") + "; initialSceneRestore=" + restore.GetType().Name + ": " + restore.Message; }
        run.endedAt = DateTime.UtcNow.ToString("O");
        var success = run.launcherOwned && SameLauncher(run) && ShortCycleValidationLauncher.IsRestored &&
            string.IsNullOrEmpty(run.error) && run.auditPassed && run.probeCompleted && run.probeSucceeded &&
            CompleteSequence(run.checkpoints) && run.sceneRestored && run.isolationReleased && run.targetDestroyedBeforeLeaseRelease &&
            (!run.probeDetached || run.probeRestored) && (!run.temporaryAnchor || run.temporaryAnchorRestored);
        run.phase = "finished";
        try
        {
            if (!run.resultPathValidated || string.IsNullOrEmpty(run.resultPath) ||
                !Directory.Exists(Path.GetDirectoryName(run.resultPath)))
                throw new IOException("Validated result directory unavailable.");
            var expected = Path.GetFullPath(Path.Combine(ShortCycleValidationBootstrap.Workspace,
                ".ai-workspace/outputs/BATCH2-INDEPENDENT-20261023", run.taskId, "ci-runs", run.runId, "unity-result.json"));
            if (!string.Equals(Path.GetFullPath(run.resultPath), expected, StringComparison.OrdinalIgnoreCase))
                throw new IOException("Probe CI result identity changed before write.");
            EnsureUnredirected(Path.GetDirectoryName(expected));
            using (var stream = new FileStream(run.resultPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                writer.Write(JsonUtility.ToJson(run, true));
        }
        catch (Exception output) { success = false; run.error += "; resultWrite=" + output; }
        if ((!run.probeDetached || run.probeRestored) && (!run.temporaryAnchor || run.temporaryAnchorRestored)) SessionState.EraseString(Key);
        EditorApplication.Exit(success ? 0 : 2);
    }
}
