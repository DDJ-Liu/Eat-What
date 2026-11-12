using System;
using System.Linq;
using EatWhat.Cooking.ShortCycle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
[InitializeOnLoad]
public static class ShortCycleValidationLauncher
{
    private const string Key="EatWhat.Validation.Isolation.";
    private static EditorApplication.CallbackFunction pendingRestore;
    private static int restoreEpoch;
    public static bool IsRestored => !SessionState.GetBool(Key+"armed",false) &&
        !SessionState.GetBool(Key+"entered",false) &&
        string.IsNullOrEmpty(SessionState.GetString(Key+"restore","")) &&
        !EditorApplication.isPlayingOrWillChangePlaymode && !ShortCycleValidationBootstrap.IsRunning;
    static ShortCycleValidationLauncher()
    {
        ShortCycleValidationBootstrap.EditorRequestProvider=ReadRequest;
        ShortCycleValidationBootstrap.EditorStopRequested=()=>{EditorApplication.isPlaying=false;};
        EditorApplication.playModeStateChanged-=Changed;EditorApplication.playModeStateChanged+=Changed;
        AssemblyReloadEvents.beforeAssemblyReload-=Reloading;AssemblyReloadEvents.beforeAssemblyReload+=Reloading;
    }
    private static ShortCycleValidationBootstrap.Request ReadRequest()
    {
        if(!SessionState.GetBool(Key+"armed",false))return null;
        return new ShortCycleValidationBootstrap.Request{Enabled=true,TaskId=SessionState.GetString(Key+"task",""),RunId=SessionState.GetString(Key+"run",""),RootPath=SessionState.GetString(Key+"root","")};
    }
    public static void ArmAndPlay(string taskId,string runId,string absoluteRoot)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||SessionState.GetBool(Key+"armed",false)||!string.IsNullOrEmpty(SessionState.GetString(Key+"restore","")))
            throw new InvalidOperationException("Idle Edit Mode with no pending validation required.");
        var request=new ShortCycleValidationBootstrap.Request{Enabled=true,TaskId=taskId,RunId=runId,RootPath=absoluteRoot};
        ShortCycleValidationBootstrap.ValidateRequest(request);
        for(var i=0;i<SceneManager.sceneCount;i++){var scene=SceneManager.GetSceneAt(i);if(scene.isDirty||string.IsNullOrEmpty(scene.path))throw new InvalidOperationException("All original scenes must be saved and clean.");}
        var setup=EditorSceneManager.GetSceneManagerSetup();
        SessionState.SetString(Key+"restore",string.Join("\n",setup.Select(s=>(s.isLoaded?"1":"0")+"|"+(s.isActive?"1":"0")+"|"+Uri.EscapeDataString(s.path))));
        SessionState.SetString(Key+"task",taskId);SessionState.SetString(Key+"run",runId);SessionState.SetString(Key+"root",absoluteRoot);SessionState.SetBool(Key+"armed",true);SessionState.SetBool(Key+"entered",false);SessionState.SetInt(Key+"bridge",0);
        try
        {
            // A fresh empty bridge prevents reuse when Domain/Scene Reload are disabled.
            var bridge=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SessionState.SetInt(Key+"bridge",bridge.handle);
            EditorApplication.isPlaying=true;
        }
        catch{SessionState.SetBool(Key+"armed",false);EditorApplication.isPlaying=false;RequestPendingRestore();throw;}
    }
    [MenuItem("Tools/Eat What/Validation/Start Isolated ShortCycle From Arguments")]
    public static void StartFromCommandLine()
    {
        var request=ShortCycleValidationBootstrap.ParsePlayerArguments(Environment.GetCommandLineArgs());
        if(request==null)throw new InvalidOperationException("Pass -ewValidation and explicit task/run/root arguments.");
        ArmAndPlay(request.TaskId,request.RunId,request.RootPath);
    }
    [MenuItem("Tools/Eat What/Validation/Run Original 1-20 Probe")]
    public static void RunOriginalProbe(){ShortCycleValidationBootstrap.RunOriginalProbe();}
    [MenuItem("Tools/Eat What/Validation/Stop Isolated ShortCycle")]
    public static void Stop()
    {
        var actualPlay=EditorApplication.isPlaying||ShortCycleValidationBootstrap.IsRunning||SessionState.GetBool(Key+"entered",false);
        ShortCycleValidationBootstrap.Stop("Editor launcher stop");
        SessionState.SetBool(Key+"armed",false);EditorApplication.isPlaying=false;
        CancelPendingRestore();
        if(string.IsNullOrEmpty(SessionState.GetString(Key+"restore","")))return;
        if(actualPlay){SessionState.SetBool(Key+"entered",true);return;}
        RequestPendingRestore();
    }
    private static void CancelPendingRestore()
    {
        if(pendingRestore!=null)EditorApplication.update-=pendingRestore;
        pendingRestore=null;restoreEpoch++;
    }
    private static void RequestPendingRestore()
    {
        CancelPendingRestore();
        var record=SessionState.GetString(Key+"restore","");if(string.IsNullOrEmpty(record))return;
        var bridge=SessionState.GetInt(Key+"bridge",0);var epoch=restoreEpoch;
        pendingRestore=()=>{
            if(epoch!=restoreEpoch)return;
            if(SessionState.GetString(Key+"restore","")!=record||SessionState.GetBool(Key+"armed",false)||EditorApplication.isPlaying){CancelPendingRestore();return;}
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            CancelPendingRestore(); // One shot, including failures. Keep the recovery record until success.
            if(bridge==0||SceneManager.sceneCount!=1||SceneManager.GetSceneAt(0).handle!=bridge)
                throw new InvalidOperationException("Pending launch bridge identity changed; preserve setup and scene.");
            Restore();
        };
        EditorApplication.update+=pendingRestore;
        if(!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling&&!EditorApplication.isUpdating)pendingRestore();
    }
    private static void Reloading()
    {if(ShortCycleValidationBootstrap.IsRunning)Stop();CancelPendingRestore();}
    private static void Changed(PlayModeStateChange state)
    {
        if(state==PlayModeStateChange.EnteredPlayMode)
        {
            if(SessionState.GetBool(Key+"armed",false)||ShortCycleValidationBootstrap.IsRunning)SessionState.SetBool(Key+"entered",true);
            CancelPendingRestore();
        }
        if(state==PlayModeStateChange.ExitingPlayMode)
        {
            var owned=SessionState.GetBool(Key+"armed",false)||ShortCycleValidationBootstrap.IsRunning||SessionState.GetBool(Key+"entered",false);
            ShortCycleValidationBootstrap.Stop("exiting Play");SessionState.SetBool(Key+"armed",false);
            if(owned&&!string.IsNullOrEmpty(SessionState.GetString(Key+"restore","")))SessionState.SetBool(Key+"entered",true);
            CancelPendingRestore();
        }
        if(state==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool(Key+"entered",false))
        {
            SessionState.SetBool(Key+"entered",false);CancelPendingRestore();Restore();
        }
    }
    private static void Restore()
    {
        var text=SessionState.GetString(Key+"restore","");if(string.IsNullOrEmpty(text))return;
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating)throw new InvalidOperationException("Restore requires settled Edit Mode.");
        if(SceneManager.sceneCount!=1)throw new InvalidOperationException("Expected only the owned bridge; preserve all scenes.");
        // Preserve any unexpected edit to the temporary bridge rather than overwrite it.
        for(var i=0;i<SceneManager.sceneCount;i++){var scene=SceneManager.GetSceneAt(i);if(scene.isDirty||!string.IsNullOrEmpty(scene.path)||scene.GetRootGameObjects().Length!=0)throw new InvalidOperationException("Unexpected bridge edits: preserve and recover original scene setup explicitly.");}
        var setup=text.Split('\n').Select(line=>{
            var fields=line.Split('|');
            if(fields.Length!=3||(fields[0]!="0"&&fields[0]!="1")||(fields[1]!="0"&&fields[1]!="1"))throw new InvalidOperationException("Invalid restore record.");
            var path=Uri.UnescapeDataString(fields[2]);
            if(!(path.StartsWith("Assets/",StringComparison.Ordinal)||path.StartsWith("Packages/",StringComparison.Ordinal))||!path.EndsWith(".unity",StringComparison.Ordinal)||path.Contains("\\")||path.Split('/').Any(x=>x.Length==0||x=="."||x=="..")||Uri.EscapeDataString(path)!=fields[2])
                throw new InvalidOperationException("Invalid restore scene identity; preserve record.");
            return new SceneSetup{path=path,isLoaded=fields[0]=="1",isActive=fields[1]=="1"};
        }).ToArray();
        if(setup.Length==0||setup.Count(s=>s.isActive)!=1||setup.Any(s=>s.isActive&&!s.isLoaded)||setup.Select(s=>s.path).Distinct(StringComparer.Ordinal).Count()!=setup.Length)throw new InvalidOperationException("Invalid restore setup; preserve record.");
        EditorSceneManager.RestoreSceneManagerSetup(setup);SessionState.SetString(Key+"restore","");
    }
}
