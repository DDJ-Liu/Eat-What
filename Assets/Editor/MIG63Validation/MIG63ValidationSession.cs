using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Registers callbacks only. Tests start exclusively from explicit menu commands.
[InitializeOnLoad]
internal static class MIG63ValidationSession
{
    const string Key = "MIG63.Validation.";
    const string Font = "Assets/Font/Heavy/ShortCycle_Heavy_WithFallback.asset";
    const string FontHash = "1d379a7eb64de1871194614f83bf61ac52e0d323677048fe237e3fbd3b399cc2";
    static bool busy;
    internal static double Next;
    internal static bool Active => SessionState.GetBool(Key + "active", false);
    internal static string Output => SessionState.GetString(Key + "output", "");
    internal static int Stage { get => SessionState.GetInt(Key + "stage", 0); set => SessionState.SetInt(Key + "stage", value); }
    internal static int Sequence { get => SessionState.GetInt(Key + "sequence", 0); set => SessionState.SetInt(Key + "sequence", value); }
    static MIG63ValidationSession()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += StateChanged;
        Application.logMessageReceived += Log;
    }

    static string Hash(string path)
    {
        using (var stream = File.OpenRead(path))
        using (var sha = SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
    }

    internal static void Begin(string kind, string testScene)
    {
        if (!Application.unityVersion.StartsWith("6000.3.")) throw new InvalidOperationException("Requires the MIG63 Unity 6000.3 editor.");
        if (Active || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            throw new InvalidOperationException("Wait for the current editor operation/test to finish.");
        var scene = SceneManager.GetActiveScene();
        if (SceneManager.sceneCount != 1 || scene.isDirty || string.IsNullOrEmpty(scene.path))
            throw new InvalidOperationException("Requires one saved, clean scene. Preserve your edits before running the probe.");
        var root = Path.GetDirectoryName(Application.dataPath);
        if (Hash(Path.Combine(root, Font)) != FontHash)
            throw new InvalidOperationException("Font already differs from the approved baseline; preserve and inspect it first.");
        var font = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(Font);
        if (font == null || EditorUtility.IsDirty(font))
            throw new InvalidOperationException("Font has pending editor changes. Preserve and inspect them before this test.");
        var output = Path.Combine(root, ".ai-workspace/outputs/MIG63/Validation", DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + "-" + kind);
        Directory.CreateDirectory(output);
        File.Copy(Path.Combine(root, Font), Path.Combine(output, "font-before.asset"));
        File.Copy(Path.Combine(root, Font + ".meta"), Path.Combine(output, "font-before.asset.meta"));
        SessionState.SetString(Key + "output", output);
        SessionState.SetString(Key + "restore", scene.path);
        SessionState.SetString(Key + "sceneHash", Hash(scene.path));
        SessionState.SetString(Key + "fontMetaHash", Hash(Font + ".meta"));
        SessionState.SetString(Key + "kind", kind);
        SessionState.SetString(Key + "deadline", DateTime.UtcNow.AddSeconds(120).Ticks.ToString());
        SessionState.SetBool(Key + "complete", false);
        SessionState.SetBool(Key + "ready", false);
        SessionState.SetBool(Key + "finishing", false);
        SessionState.SetInt(Key + "errors", 0);
        Stage = 0;
        File.WriteAllText(Path.Combine(output, "environment.txt"),
            "unity=" + Application.unityVersion + "\nplatform=" + Application.platform +
            "\ngpu=" + SystemInfo.graphicsDeviceType + "\nscroll=" + InputSystem.settings.scrollDeltaBehavior +
            "\nwindowsAuto=" + PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64) +
            "\nwindowsAPIs=" + string.Join(",", PlayerSettings.GetGraphicsAPIs(BuildTarget.StandaloneWindows64).Select(x => x.ToString())) +
            "\nrestore=" + scene.path + "\nfontSHA256=" + FontHash + "\n");
        SessionState.SetBool(Key + "active", true);
        try
        {
            if (testScene == null) EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            else EditorSceneManager.OpenScene(testScene, OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
        }
        catch (Exception error) { Fail(error); }
    }

    static void Log(string message, string stack, LogType type)
    {
        if (!Active || (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)) return;
        SessionState.SetInt(Key + "errors", SessionState.GetInt(Key + "errors", 0) + 1);
        File.AppendAllText(Path.Combine(Output, "runtime-errors.log"), message + "\n" + stack + "\n");
    }

    internal static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        File.AppendAllText(Path.Combine(Output, "checks.txt"), "PASS " + message + "\n");
    }
    internal static void Advance(int stage, double seconds = .3) { Stage = stage; Next = EditorApplication.timeSinceStartup + seconds; }
    internal static void Finish()
    {
        SessionState.SetBool(Key + "complete", true);
        SessionState.SetBool(Key + "finishing", true);
        EditorApplication.isPlaying = false;
    }
    static void Fail(Exception error)
    {
        File.AppendAllText(Path.Combine(Output, "failure.txt"), error + "\n");
        SessionState.SetBool(Key + "finishing", true);
        SessionState.SetBool(Key + "complete", false);
        MIG63TomatoProbe.Cleanup();
        MIG63SpineSceneProbe.Cleanup();
        if (EditorApplication.isPlayingOrWillChangePlaymode) EditorApplication.isPlaying = false;
        else EditorApplication.delayCall += Restore;
    }
    static void StateChanged(PlayModeStateChange state)
    {
        if (!Active) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            Next = EditorApplication.timeSinceStartup + 3;
            SessionState.SetBool(Key + "ready", true);
        }
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            SessionState.SetBool(Key + "ready", false);
            SessionState.SetBool(Key + "finishing", true);
            MIG63TomatoProbe.Cleanup();
            MIG63SpineSceneProbe.Cleanup();
        }
        if (state == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += Restore;
    }
    static void Restore()
    {
        if (!Active || EditorApplication.isPlayingOrWillChangePlaymode) return;
        var output = Output;
        try
        {
            var restore = SessionState.GetString(Key + "restore", "");
            Check(Hash(restore) == SessionState.GetString(Key + "sceneHash", ""), "original saved scene file unchanged");
            EditorSceneManager.OpenScene(restore, OpenSceneMode.Single);
            Check(!SceneManager.GetActiveScene().isDirty, "original scene restored in clean Edit mode");
            if (Hash(Font) != FontHash)
            {
                File.Copy(Font, Path.Combine(output, "font-after.asset"), true);
                throw new InvalidOperationException("Font disk drift detected. Before/after preserved; no automatic overwrite. Use the documented Editor recovery procedure.");
            }
            Check(Hash(Font + ".meta") == SessionState.GetString(Key + "fontMetaHash", ""), "font GUID/import metadata unchanged");
            Check(Hash(Font) == FontHash, "font disk SHA-256 unchanged");
            Check(SessionState.GetBool(Key + "complete", false), "test reached its final assertion");
            Check(SessionState.GetInt(Key + "errors", 0) == 0, "no runtime Error/Exception/Assert during this run");
            File.WriteAllText(Path.Combine(output, "completed.txt"), DateTime.Now.ToString("o"));
        }
        catch (Exception error) { File.AppendAllText(Path.Combine(output, "failure.txt"), error + "\n"); }
        finally
        {
            SessionState.SetBool(Key + "active", false); // Only unlock after scene restoration.
            Debug.Log("MIG63 validation finished; inspect " + output);
        }
    }
    static void Tick()
    {
        if (!Active || busy || SessionState.GetBool(Key + "finishing", false)) return;
        busy = true;
        try
        {
            if (DateTime.UtcNow.Ticks > long.Parse(SessionState.GetString(Key + "deadline", "0")))
                throw new TimeoutException("MIG63 validation exceeded 120 seconds.");
            if (!EditorApplication.isPlaying || EditorApplication.isPaused || !SessionState.GetBool(Key + "ready", false) || EditorApplication.timeSinceStartup < Next) return;
            if (SessionState.GetString(Key + "kind", "") == "wheel") MIG63WheelProbe.Tick();
            else if (SessionState.GetString(Key + "kind", "") == "spine-scene") MIG63SpineSceneProbe.Tick();
            else MIG63TomatoProbe.Tick();
        }
        catch (Exception error) { Fail(error); }
        finally { busy = false; }
    }
}
