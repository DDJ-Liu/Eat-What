using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using EatWhat.Cooking.ShortCycle;
using EatWhat.Tools.Persistence;
using EatWhat.Tools.Security;
using EatWhat.Tools.Localization;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
public static class ShortCycleIsolationAndTextTests
{
    private sealed class Keys : IKeyProvider { public byte[] GetKey(){var b=new byte[32];for(var i=0;i<b.Length;i++)b[i]=(byte)i;return b;} }
    public static IList<string> PassedCases {get;private set;}
    private static void Check(bool value,string name){if(!value)throw new Exception("AI280: "+name);PassedCases.Add(name);}
    private static void Field(object obj,string name,object value){for(var t=obj.GetType();t!=null;t=t.BaseType){var f=t.GetField(name,BindingFlags.Instance|BindingFlags.NonPublic);if(f!=null){f.SetValue(obj,value);return;}}throw new Exception("Missing field "+name);}
    private static T Read<T>(object obj,string name){return (T)obj.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(obj);}
    private static void Call(object obj,string name){try{obj.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(obj,null);}catch(TargetInvocationException e){throw e.InnerException;}}
    private static void Rejected(Action action,string name){var rejected=false;try{action();}catch(InvalidOperationException){rejected=true;}catch(ArgumentException){rejected=true;}Check(rejected,name);}
    private static JsonSaveStore Store(string root){return new JsonSaveStore(root,new PlainJsonCodec(),new EnvelopeAuthenticator(new Keys()));}
    private static ShortCycleSessionManager Session(string name,string scene="AI280Target")
    {var s=new ShortCycleSessionManager();s.gameObject.name=name;s.gameObject.scene=new Scene{path=scene,name=scene,isLoaded=true};Field(s,"playerProfile",new PlayerProfile());return s;}
    private static void Destroy(ShortCycleSessionManager session){Call(session,"OnDestroy");}
    private static string ReadHostEvidence(string path)
    {using(var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite))using(var reader=new StreamReader(stream))return reader.ReadToEnd();}
    private static string HostTaskId="AI-000287";
    private static ShortCycleValidationBootstrap.Request HostRequest(string run)
    {
        return new ShortCycleValidationBootstrap.Request{Enabled=true,TaskId=HostTaskId,RunId=run,RootPath=Path.GetFullPath(Path.Combine(ShortCycleValidationBootstrap.Workspace,".ai-workspace/outputs/DETACH-B13-20261019",HostTaskId,"isolated-saves",run))};
    }
    private static ShortCycleSessionManager HostTarget(string name="SessionManager",string path=ShortCycleValidationBootstrap.TargetScene)
    {
        var session=Session(name,path);session.transform.SetParent(new GameObject("Managers").transform,false);SceneManager.SetLoaded(session.gameObject.scene);return session;
    }
    private static void HostReset()
    {
        ShortCycleValidationBootstrap.Stop("fixture");
        foreach(var s in Resources.FindObjectsOfTypeAll<ShortCycleSessionManager>())UnityEngine.Object.DestroyImmediate(s.gameObject);
        typeof(ShortCycleSaveIsolation).GetMethod("ResetLifecycle",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
        typeof(ShortCycleValidationBootstrap).GetMethod("NewLifecycle",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
        SceneManager.ValidationClear();SceneManager.ValidationLoad=null;Application.isPlaying=true;Application.isEditor=false;
    }
    public static string RunValidationHost(string runPrefix,Action<string,string> createRedirect,string taskId="AI-000287")
    {
        HostTaskId=taskId;PassedCases=new List<string>();var baseline=Application.PersistentPathReads;HostReset();
        var normal=HostTarget();Check(!ShortCycleValidationBootstrap.Begin(null)&&!normal.gameObject.ValidationDestroyed,"no explicit request leaves ordinary target untouched");
        Check(!ShortCycleValidationBootstrap.BeginPlayerArguments(new[]{"player"})&&!normal.gameObject.ValidationDestroyed,"ordinary Player args do not opt in");HostReset();
        var target=HostTarget();var request=HostRequest(runPrefix+"-correct");
        Check(ShortCycleValidationBootstrap.BeginPlayerArguments(new[]{"player","-ewValidation","-ewValidationTask",request.TaskId,"-ewValidationRun",request.RunId,"-ewValidationRoot",request.RootPath}),"delivered Player entry installs exact early target");
        Call(target,"Awake");ShortCycleValidationBootstrap.ObserveReady();target.BeginSession(null,ShortCycleTriggerContext.FreeTry);
        var store=Read<SaveBoundary>(target,"persistenceBoundary");store.Reach(ShortCycleSavePoint.EnterPhase0,new ShortCycleContext());
        Check(ShortCycleValidationBootstrap.State=="Ready"&&ShortCycleValidationBootstrap.IsolatedFactoryCalls==1&&File.Exists(Path.Combine(request.RootPath,"default.json")),"same production save point writes only approved task/run root");
        Check(ReadHostEvidence(ShortCycleValidationBootstrap.EvidencePath).Contains("defaultFactorySelected=false")&&Application.PersistentPathReads==baseline,"actual host evidence and default factory sentinel");
        var probe=new ShortCycleAutomatedProbe();probe.gameObject.scene=target.gameObject.scene;ShortCycleValidationBootstrap.RunOriginalProbe();
        Check(probe.StartedCoroutineCount==1,"delivered host calls unchanged original RunProbe coroutine entry");
        var priorSaved=File.ReadAllBytes(Path.Combine(request.RootPath,"default.json"));
        ShortCycleValidationBootstrap.Stop("normal");Check(target.gameObject.ValidationDestroyed&&!ShortCycleValidationBootstrap.IsRunning,"target destroyed before host lease released");
        store.Reach(ShortCycleSavePoint.Phase1ToPhase2,new ShortCycleContext());Check(Convert.ToBase64String(priorSaved)==Convert.ToBase64String(File.ReadAllBytes(Path.Combine(request.RootPath,"default.json"))),"released delivered host revokes already-prepared storage without new save point");
        HostTarget();var previous=File.ReadAllText(Path.Combine(request.RootPath,"validation-host.log"));Check(!ShortCycleValidationBootstrap.Begin(request)&&!ShortCycleValidationBootstrap.IsRunning&&File.ReadAllText(Path.Combine(request.RootPath,"validation-host.log"))==previous,"same run evidence collision fails closed instead of overwrite");HostReset();
        var duplicateOwner=HostTarget();Check(ShortCycleValidationBootstrap.Begin(HostRequest(runPrefix+"-owner")),"first explicit owner installed");
        Check(!ShortCycleValidationBootstrap.Begin(HostRequest(runPrefix+"-owner2"))&&duplicateOwner.gameObject.ValidationDestroyed&&!ShortCycleValidationBootstrap.IsRunning,"duplicate enabled owner fails closed and tears down existing target");HostReset();
        foreach(var kind in new[]{"relative","other-task","root","redirect","late","wrong","duplicate"})
        {
            var a=HostTarget();var r=HostRequest(runPrefix+"-"+kind);
            if(kind=="relative")r.RootPath="relative";
            if(kind=="other-task")r.RootPath=r.RootPath.Replace(HostTaskId,"AI-999999");
            if(kind=="root")r.RootPath=Path.GetPathRoot(r.RootPath);
            if(kind=="late")Call(a,"Awake");
            if(kind=="wrong")a.gameObject.name="Wrong";
            if(kind=="duplicate")HostTarget();
            if(kind=="redirect"){
                // Exercise refusal of an existing non-directory ancestor without platform link privileges.
                Directory.CreateDirectory(Path.GetDirectoryName(r.RootPath));var redirected=r.RootPath+"-target";Directory.CreateDirectory(redirected);createRedirect(r.RootPath,redirected);
            }
            Check(!ShortCycleValidationBootstrap.Begin(r)&&ShortCycleValidationBootstrap.State=="Faulted","invalid enabled "+kind+" rejected by actual host");
            Check(a.gameObject.ValidationDestroyed&&Application.PersistentPathReads==baseline,"invalid enabled "+kind+" neutralizes target before production Start");
            HostReset();
        }
        var stubborn=HostTarget();stubborn.gameObject.ThrowOnSetActive=true;var invalidStop=HostRequest(runPrefix+"-destruction-error");invalidStop.RootPath="relative";
        Check(!ShortCycleValidationBootstrap.Begin(invalidStop),"explicit invalid request remains failed when removal itself reports an error");
        Rejected(()=>Call(stubborn,"Awake"),"existing required-isolation gate blocks fallback even when destruction failed");
        Check(Application.PersistentPathReads==baseline,"destruction-error path never calls the default factory");stubborn.gameObject.ThrowOnSetActive=false;HostReset();
        var pending=HostRequest(runPrefix+"-cancel");SceneManager.ValidationLoad=(path,mode)=>{};
        Check(ShortCycleValidationBootstrap.Begin(pending),"empty bootstrap installs ForScene before controlled load");
        ShortCycleValidationBootstrap.Stop("cancel pending");
        var cancelled=HostTarget();Rejected(()=>Call(cancelled,"Awake"),"cancelled scene cannot later fall back");HostReset();
        SceneManager.ValidationLoad=(path,mode)=>{var s=HostTarget();Call(s,"Awake");};
        Check(ShortCycleValidationBootstrap.Begin(HostRequest(runPrefix+"-scene")),"persistent owner survives synchronous target load");ShortCycleValidationBootstrap.ObserveReady();
        Check(ShortCycleValidationBootstrap.State=="Ready","scene-installed lease consumed by exact Awake");
        var unloaded=Resources.FindObjectsOfTypeAll<ShortCycleSessionManager>()[0];SceneManager.RaiseUnloaded(unloaded.gameObject.scene);Check(!ShortCycleValidationBootstrap.IsRunning&&unloaded.gameObject.ValidationDestroyed,"actual target unload callback tears down owner and lease");HostReset();
        Check(!ShortCycleValidationBootstrap.BeginPlayerArguments(new[]{"-ewValidation"})&&Application.PersistentPathReads==baseline,"malformed explicit Player request fails closed");
        HostReset();var forgottenFlag=HostTarget();Check(!ShortCycleValidationBootstrap.BeginPlayerArguments(new[]{"-ewValidationRoot","somewhere"})&&forgottenFlag.gameObject.ValidationDestroyed&&Application.PersistentPathReads==baseline,"validation parameters without exact enable cannot silently launch production");
        HostReset();var misspelled=HostTarget();Check(!ShortCycleValidationBootstrap.BeginPlayerArguments(new[]{"--ewValidation"})&&misspelled.gameObject.ValidationDestroyed,"misspelled validation enable fails closed rather than default startup");
        HostReset();Application.isEditor=true;UnityEditor.EditorApplication.isPlaying=false;
        SceneManager.SetLoaded(new Scene{path="Assets/Scenes/Original.unity",isLoaded=true});
        var editor=HostRequest(runPrefix+"-editor");ShortCycleValidationLauncher.ArmAndPlay(editor.TaskId,editor.RunId,editor.RootPath);
        SceneManager.ValidationLoad=(path,mode)=>{var s=HostTarget();Call(s,"Awake");};
        Check(ShortCycleValidationBootstrap.BeginEditorRequest(),"actual Editor launcher provider survives runtime startup boundary");ShortCycleValidationBootstrap.ObserveReady();
        Check(ShortCycleValidationBootstrap.State=="Ready","Editor uses fresh bridge, not already-awakened edit target");
        UnityEditor.AssemblyReloadEvents.ValidationRaise();Check(!ShortCycleValidationBootstrap.IsRunning,"actual launcher assembly-reload hook stops target before losing static lease owner");
        UnityEditor.EditorApplication.ValidationRaise(UnityEditor.PlayModeStateChange.ExitingPlayMode);
        UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Single);
        UnityEditor.EditorApplication.ValidationRaise(UnityEditor.PlayModeStateChange.EnteredEditMode);
        Check(!ShortCycleValidationBootstrap.IsRunning&&UnityEditor.SceneManagement.EditorSceneManager.Restores==1&&!ShortCycleValidationBootstrap.BeginEditorRequest(),"exit destroys target/releases lease, restores saved setup and disarms provider");
        HostReset();Check(Application.PersistentPathReads==baseline,"entire host suite never queried default user path");
        return "PASS: validation host "+PassedCases.Count+" assertions";
    }
    private static string FileSha(string path)
    {using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
    private static ShortCycleValidationBootstrap.Request DataRequest(string run,string mode,string baseline)
    {
        var request=HostRequest(run);request.DataMode=mode;request.BaselinePath=baseline;
        request.BaselineSha256=FileSha(baseline);request.TimeoutSeconds=30;return request;
    }
    private static ShortCycleValidationBootstrap HostOwner()
    {return (ShortCycleValidationBootstrap)typeof(ShortCycleValidationBootstrap).GetField("current",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);}
    private static JObject DataResult(ShortCycleValidationBootstrap.Request request)
    {return JObject.Parse(File.ReadAllText(Path.Combine(request.RootPath,"validation-data.json")));}
    private static CK01GeneratedDataCatalog ReleaseCatalog(string blob,string fingerprint,Dictionary<string,Sprite> sprites)
    {
        var catalog=ScriptableObject.CreateInstance<CK01GeneratedDataCatalog>();
        catalog.ConfigureBuildSpriteReferences(sprites.Select(x=>new CatalogSpriteReference(x.Key,x.Value)));
        catalog.InstallSourceForValidation(new EncryptedBlobSource(()=>File.ReadAllBytes(blob),fingerprint,
            key=>{Sprite value;return sprites.TryGetValue(key,out value)?value:null;},
            new CatalogBlobCipher(new ObfuscatedStaticKeyProvider())));
        return catalog;
    }
    public static string RunPlayerDataHost(string prefix,string taskId,string baselinePath,string blobPath)
    {
        HostTaskId=taskId;PassedCases=new List<string>();var persistent=Application.PersistentPathReads;
        var graph=SaveEnvelope.ParseCanonicalObject(File.ReadAllText(baselinePath));
        CatalogBlobSerializer.ValidateMetadata(graph);
        var sprites=new Dictionary<string,Sprite>(StringComparer.Ordinal);
        foreach(var key in (JArray)graph["sprites"])sprites.Add((string)key,new Sprite());
        var decoded=CatalogBlobSerializer.Rehydrate(graph,key=>sprites[key]);
        var development=ScriptableObject.CreateInstance<CK01GeneratedDataCatalog>();
        development.ReplaceTables(decoded.Tables);
        HostReset();Application.QuitCalls=0;
        var legacyTarget=HostTarget();var legacy=HostRequest(prefix+"-legacy");
        Check(ShortCycleValidationBootstrap.Begin(legacy),"old four-parameter isolation mode still installs");
        Call(legacyTarget,"Awake");ShortCycleValidationBootstrap.ObserveReady();Call(HostOwner(),"Update");
        Check(ShortCycleValidationBootstrap.State=="Ready"&&Application.QuitCalls==0&&
            !File.Exists(Path.Combine(legacy.RootPath,"validation-data.json")),
            "old mode adds no data assertion, result file or automatic exit");
        ShortCycleValidationBootstrap.Stop("legacy fixture");HostReset();
        Application.isEditor=true;var editorTarget=HostTarget();var editorData=DataRequest(prefix+"-editor-block","development",baselinePath);
        Check(!ShortCycleValidationBootstrap.Begin(editorData)&&editorTarget.gameObject.ValidationDestroyed&&
            !Directory.Exists(editorData.RootPath),"Player data mode refuses Editor invocation before output creation");
        HostReset();
        var devTarget=HostTarget();Field(devTarget,"dataCatalog",development);
        var dev=DataRequest(prefix+"-dev","development",baselinePath);
        Check(ShortCycleValidationBootstrap.BeginPlayerArguments(new[]{"player","-ewValidation","-ewValidationTask",dev.TaskId,"-ewValidationRun",dev.RunId,
            "-ewValidationRoot",dev.RootPath,"-ewValidationData",dev.DataMode,"-ewValidationBaseline",dev.BaselinePath,
            "-ewValidationBaselineSha",dev.BaselineSha256,"-ewValidationTimeout","30"}),"explicit development data mode installs pre-Awake");
        Call(devTarget,"Awake");ShortCycleValidationBootstrap.ObserveReady();Call(HostOwner(),"Update");
        var devResult=DataResult(dev);
        Check((string)devResult["result"]=="PASS"&&(string)devResult["sourceKind"]=="ScriptableObject"&&
            (int)devResult["observedAssets"]==155&&(int)devResult["baselineAssets"]==169&&
            (int)devResult["tables"]==14&&(int)devResult["recipes"]==3&&(int)devResult["sprites"]==13&&
            devTarget.gameObject.ValidationDestroyed&&!ShortCycleValidationBootstrap.IsRunning&&Application.LastQuitCode==0,
            "actual development source graph relation projection and controlled teardown");
        HostReset();
        var replayTarget=HostTarget();
        Check(!ShortCycleValidationBootstrap.Begin(dev)&&replayTarget.gameObject.ValidationDestroyed&&
            Application.LastQuitCode==2,"data run root replay is rejected before overwrite");
        HostReset();
        var releaseTarget=HostTarget();Field(releaseTarget,"dataCatalog",ReleaseCatalog(blobPath,FileSha(baselinePath),sprites));
        var release=DataRequest(prefix+"-release","release",baselinePath);
        Check(ShortCycleValidationBootstrap.Begin(release),"explicit release data mode installs pre-Awake");
        Call(releaseTarget,"Awake");ShortCycleValidationBootstrap.ObserveReady();Call(HostOwner(),"Update");
        var releaseResult=DataResult(release);
        Check((string)releaseResult["result"]=="PASS"&&(string)releaseResult["sourceKind"]=="EncryptedBlob"&&
            (int)releaseResult["observedAssets"]==169&&(bool)releaseResult["fullAssetCoverage"]&&
            (string)releaseResult["graphFingerprint"]==FileSha(baselinePath)&&
            releaseTarget.gameObject.ValidationDestroyed&&Application.LastQuitCode==0,
            "actual authenticated release source retains all169 assets and13 Sprite identities");
        HostReset();
        SceneManager.ValidationLoad=(path,mode)=>{
            var loaded=HostTarget();Field(loaded,"dataCatalog",ReleaseCatalog(blobPath,FileSha(baselinePath),sprites));Call(loaded,"Awake");
        };
        var sceneRequest=DataRequest(prefix+"-scene-loaded","release",baselinePath);
        Check(ShortCycleValidationBootstrap.Begin(sceneRequest),"explicit data mode installs before synchronous target scene load");
        Check((string)DataResult(sceneRequest)["result"]=="PASS"&&!ShortCycleValidationBootstrap.IsRunning&&
            Application.LastQuitCode==0,"sceneLoaded validates after Awake and finishes before target Start");
        HostReset();
        var bad=Path.Combine(Path.GetDirectoryName(blobPath),prefix+"-tampered.bin");
        var damaged=File.ReadAllBytes(blobPath);damaged[damaged.Length-1]^=1;
        using(var stream=new FileStream(bad,FileMode.CreateNew,FileAccess.Write))stream.Write(damaged,0,damaged.Length);
        var badTarget=HostTarget();Field(badTarget,"dataCatalog",ReleaseCatalog(bad,FileSha(baselinePath),sprites));
        var reject=DataRequest(prefix+"-reject","reject-tampered",baselinePath);
        Check(ShortCycleValidationBootstrap.Begin(reject),"tampered source is bound to current Session before Awake");
        Call(badTarget,"Awake");ShortCycleValidationBootstrap.ObserveReady();Call(HostOwner(),"Update");
        var rejected=DataResult(reject);
        Check((string)rejected["result"]=="EXPECTED_REJECTION"&&(bool)rejected["authenticationRejected"]&&
            (int)rejected["partialDecodedAssets"]==0&&!(bool)rejected["fallbackUsed"]&&
            badTarget.gameObject.ValidationDestroyed&&Application.LastQuitCode==0,
            "current bound encrypted source rejects tampered bytes without fallback and tears down");
        HostReset();
        var acceptedTarget=HostTarget();Field(acceptedTarget,"dataCatalog",ReleaseCatalog(blobPath,FileSha(baselinePath),sprites));
        var falseReject=DataRequest(prefix+"-false-reject","reject-tampered",baselinePath);
        Check(ShortCycleValidationBootstrap.Begin(falseReject),"unexpected accepted source case installs");
        Call(acceptedTarget,"Awake");ShortCycleValidationBootstrap.ObserveReady();Call(HostOwner(),"Update");
        Check((string)DataResult(falseReject)["result"]=="FAIL"&&Application.LastQuitCode==2&&
            acceptedTarget.gameObject.ValidationDestroyed,"expect-rejection cannot pass a valid blob");
        HostReset();
        var mismatchTarget=HostTarget();Field(mismatchTarget,"dataCatalog",development);
        ((CK01RecipeData)development.Tables[0].Rows[0].Asset).name_key+="__mismatch";
        var mismatch=DataRequest(prefix+"-relation","development",baselinePath);
        Check(ShortCycleValidationBootstrap.Begin(mismatch),"relation mismatch case installs");
        Call(mismatchTarget,"Awake");ShortCycleValidationBootstrap.ObserveReady();Call(HostOwner(),"Update");
        Check((string)DataResult(mismatch)["result"]=="FAIL"&&Application.LastQuitCode==2,
            "actual development field drift is rejected");
        HostReset();
        var missingTarget=HostTarget();var missing=DataRequest(prefix+"-missing","development",baselinePath);
        Check(ShortCycleValidationBootstrap.Begin(missing),"missing Catalog case installs");
        Call(missingTarget,"Awake");ShortCycleValidationBootstrap.ObserveReady();Call(HostOwner(),"Update");
        Check((string)DataResult(missing)["result"]=="FAIL"&&Application.LastQuitCode==2,
            "missing current Session Catalog is rejected");
        HostReset();
        var waiting=HostTarget();var timeout=DataRequest(prefix+"-timeout","development",baselinePath);
        Check(ShortCycleValidationBootstrap.Begin(timeout),"bounded waiting case installs");
        Field(HostOwner(),"dataDeadline",DateTime.UtcNow.AddSeconds(-1));
        Call(HostOwner(),"Update");
        Check((string)DataResult(timeout)["result"]=="FAIL"&&waiting.gameObject.ValidationDestroyed&&Application.LastQuitCode==2,
            "deadline terminates missing Awake with bounded failure");
        HostReset();
        var stubborn=HostTarget();Field(stubborn,"dataCatalog",ReleaseCatalog(blobPath,FileSha(baselinePath),sprites));
        var cleanup=DataRequest(prefix+"-cleanup","release",baselinePath);
        Check(ShortCycleValidationBootstrap.Begin(cleanup),"cleanup failure case installs");
        Call(stubborn,"Awake");ShortCycleValidationBootstrap.ObserveReady();stubborn.gameObject.ThrowOnSetActive=true;Call(HostOwner(),"Update");
        Check((string)DataResult(cleanup)["result"]=="FAIL"&&!(bool)DataResult(cleanup)["leaseReleased"]&&
            Application.LastQuitCode==2,"failed target teardown never yields success");
        stubborn.gameObject.ThrowOnSetActive=false;HostReset();
        var parser=DataRequest(prefix+"-parser","release",baselinePath);
        var args=new[]{"-ewValidation","-ewValidationTask",parser.TaskId,"-ewValidationRun",parser.RunId,"-ewValidationRoot",parser.RootPath,
            "-ewValidationData",parser.DataMode,"-ewValidationBaseline",parser.BaselinePath,"-ewValidationBaselineSha",parser.BaselineSha256,
            "-ewValidationTimeout","30"};
        Rejected(()=>ShortCycleValidationBootstrap.ParsePlayerArguments(args.Concat(new[]{"-ewValidationData","release"}).ToArray()),
            "duplicate data mode rejected before installation");
        Rejected(()=>ShortCycleValidationBootstrap.ParsePlayerArguments(args.Concat(new[]{"-ewValidationUnknown","x"}).ToArray()),
            "unknown data parameter rejected");
        Rejected(()=>ShortCycleValidationBootstrap.ParsePlayerArguments(args.Take(args.Length-2).ToArray()),
            "missing data timeout rejected");
        parser.BaselineSha256=new string('0',64);
        HostReset();var hashTarget=HostTarget();Field(hashTarget,"dataCatalog",ReleaseCatalog(blobPath,FileSha(baselinePath),sprites));
        Check(ShortCycleValidationBootstrap.Begin(parser),"wrong baseline SHA remains syntactically bound to current task");
        Call(hashTarget,"Awake");ShortCycleValidationBootstrap.ObserveReady();Call(HostOwner(),"Update");
        Check((string)DataResult(parser)["result"]=="FAIL"&&
            ((string)DataResult(parser)["error"]).Contains("Bound baseline bytes changed")&&Application.LastQuitCode==2,
            "wrong baseline SHA bytes fail before current source read");
        Check(Application.PersistentPathReads==persistent,"all data modes avoid default user save path");
        decoded.Dispose();HostReset();
        return "PASS: Player data host "+PassedCases.Count+" assertions";
    }
    private const string LauncherKey="EatWhat.Validation.Isolation.";
    private static void ResetCancelCase()
    {
        HostReset();UnityEditor.EditorApplication.ValidationReset();Application.isEditor=true;
        UnityEditor.SessionState.SetBool(LauncherKey+"armed",false);UnityEditor.SessionState.SetBool(LauncherKey+"entered",false);
        UnityEditor.SessionState.SetString(LauncherKey+"restore","");ShortCycleValidationLauncher.Stop();
        UnityEditor.SceneManagement.EditorSceneManager.Restores=0;UnityEditor.SceneManagement.EditorSceneManager.ValidationThrowRestore=false;
        SceneManager.ValidationClear();SceneManager.SetLoaded(new Scene{path="Assets/Scenes/Original.unity",isLoaded=true,handle=500});
    }
    private static void ArmCancellation(string run)
    {var request=HostRequest(run);ShortCycleValidationLauncher.ArmAndPlay(request.TaskId,request.RunId,request.RootPath);}
    public static IList<string> CancellationCases {get;private set;}
    public static string RunLauncherCancellation(string prefix,string taskId)
    {
        HostTaskId=taskId;PassedCases=new List<string>();var reads=Application.PersistentPathReads;
        ResetCancelCase();ArmCancellation(prefix+"-immediate");
        Check(!UnityEditor.EditorApplication.isPlaying&&UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode,"stub explicitly models pending launch, not actual Play");
        ShortCycleValidationLauncher.Stop();
        Check(UnityEditor.SceneManagement.EditorSceneManager.Restores==1&&SceneManager.GetActiveScene().path=="Assets/Scenes/Original.unity","ArmAndPlay then Stop restores saved setup without EnteredEditMode");
        Check(UnityEditor.SessionState.GetString(LauncherKey+"restore","")==""&&UnityEditor.EditorApplication.EnteredEditEvents==0,"immediate cancel consumes record only on real restore, no fabricated Edit event");
        ShortCycleValidationLauncher.Stop();UnityEditor.EditorApplication.ValidationRaise(UnityEditor.PlayModeStateChange.EnteredEditMode);
        Check(UnityEditor.SceneManagement.EditorSceneManager.Restores==1,"repeat Stop and late Edit event never restore twice");
        ResetCancelCase();UnityEditor.EditorApplication.ValidationHoldPendingCancel=true;ArmCancellation(prefix+"-deferred");
        ShortCycleValidationLauncher.Stop();var stale=UnityEditor.EditorApplication.ValidationSnapshotUpdate();
        Check(UnityEditor.SceneManagement.EditorSceneManager.Restores==0&&UnityEditor.EditorApplication.ValidationUpdateCount==1,"pending cancellation waits for settled Edit with one callback");
        ShortCycleValidationLauncher.Stop();Check(UnityEditor.EditorApplication.ValidationUpdateCount==1,"repeated pending Stop replaces rather than duplicates callback");
        stale();Check(UnityEditor.SceneManagement.EditorSceneManager.Restores==0,"obsolete request callback is inert");
        UnityEditor.EditorApplication.ValidationPending=false;UnityEditor.EditorApplication.ValidationUpdate();
        Check(UnityEditor.SceneManagement.EditorSceneManager.Restores==1&&UnityEditor.EditorApplication.EnteredEditEvents==0,"actual idle update restores pending cancellation without EnteredEditMode");
        stale();UnityEditor.EditorApplication.ValidationUpdate();Check(UnityEditor.SceneManagement.EditorSceneManager.Restores==1&&UnityEditor.EditorApplication.ValidationUpdateCount==0,"callback consumed after success; late callbacks inert");
        ResetCancelCase();ArmCancellation(prefix+"-actual");UnityEditor.EditorApplication.ValidationRaise(UnityEditor.PlayModeStateChange.EnteredPlayMode);
        ShortCycleValidationLauncher.Stop();UnityEditor.EditorApplication.ValidationUpdate();
        Check(UnityEditor.SceneManagement.EditorSceneManager.Restores==0,"actual Play stop never restores before Edit exit");
        UnityEditor.EditorApplication.ValidationRaise(UnityEditor.PlayModeStateChange.ExitingPlayMode);
        Check(UnityEditor.SceneManagement.EditorSceneManager.Restores==0,"ExitingPlayMode alone cannot restore running scene");
        UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Single);
        UnityEditor.EditorApplication.ValidationRaise(UnityEditor.PlayModeStateChange.EnteredEditMode);
        Check(UnityEditor.SceneManagement.EditorSceneManager.Restores==1,"normal actual Play exit retains existing guarded restoration path");
        foreach(var kind in new[]{"dirty","objects","named","replacement-empty","corrupt","bad-flags","restore-failure"})
        {
            ResetCancelCase();ArmCancellation(prefix+"-"+kind);var scene=SceneManager.GetSceneAt(0);
            if(kind=="dirty"){scene.isDirty=true;SceneManager.SetLoaded(scene);}
            if(kind=="objects"){scene.ValidationRootCount=1;SceneManager.SetLoaded(scene);}
            if(kind=="named"){SceneManager.ValidationClear();SceneManager.SetLoaded(new Scene{path="Assets/Scenes/User.unity",isLoaded=true,handle=scene.handle});}
            if(kind=="replacement-empty")UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Single);
            if(kind=="corrupt")UnityEditor.SessionState.SetString(LauncherKey+"restore","not-a-record");
            if(kind=="bad-flags")UnityEditor.SessionState.SetString(LauncherKey+"restore","x|1|Assets%2FScenes%2FOriginal.unity");
            if(kind=="restore-failure")UnityEditor.SceneManagement.EditorSceneManager.ValidationThrowRestore=true;
            var record=UnityEditor.SessionState.GetString(LauncherKey+"restore","");var prior=SceneManager.GetSceneAt(0);
            Rejected(()=>ShortCycleValidationLauncher.Stop(),"actual Stop refuses "+kind);
            var after=SceneManager.GetSceneAt(0);
            Check(UnityEditor.SceneManagement.EditorSceneManager.Restores==0&&record==UnityEditor.SessionState.GetString(LauncherKey+"restore","")&&prior.path==after.path&&prior.handle==after.handle&&prior.isDirty==after.isDirty&&prior.ValidationRootCount==after.ValidationRootCount,"record and scene preserved after "+kind);
            Check(UnityEditor.EditorApplication.ValidationUpdateCount==0,"failure does not leave an automatic retry callback: "+kind);
        }
        ResetCancelCase();UnityEditor.EditorApplication.ValidationHoldPendingCancel=true;ArmCancellation(prefix+"-late-user");ShortCycleValidationLauncher.Stop();
        UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Single);
        UnityEditor.EditorApplication.ValidationPending=false;
        Rejected(()=>UnityEditor.EditorApplication.ValidationUpdate(),"deferred callback rejects a later user empty scene with different handle");
        Check(UnityEditor.SceneManagement.EditorSceneManager.Restores==0&&UnityEditor.SessionState.GetString(LauncherKey+"restore","")!="","later user scene and recovery evidence retained");
        ResetCancelCase();ShortCycleValidationLauncher.Stop();UnityEditor.EditorApplication.ValidationUpdate();UnityEditor.EditorApplication.ValidationRaise(UnityEditor.PlayModeStateChange.EnteredEditMode);
        Check(UnityEditor.SceneManagement.EditorSceneManager.Restores==0&&SceneManager.GetActiveScene().path=="Assets/Scenes/Original.unity","no pending ownership record means no scene action");
        Check(Application.PersistentPathReads==reads,"all cancellation regressions leave default storage untouched");
        CancellationCases=new List<string>(PassedCases);ResetCancelCase();return "PASS: launcher cancellation "+CancellationCases.Count+" assertions";
    }
    public static string Run(string root)
    {
        PassedCases=new List<string>();Directory.CreateDirectory(root);var before=Application.PersistentPathReads;
        var target=Session("Target");var isolated=Path.Combine(root,"early");var calls=0;
        var lease=ShortCycleSaveIsolation.ForTarget(target,isolated,()=>{calls++;return Store(isolated);});
        try {
            Rejected(()=>ShortCycleSaveIsolation.ForTarget(Session("Conflict"),isolated,()=>Store(isolated)),"duplicate/conflicting isolation rejected before Awake");
            var wrong=Session("Wrong");Rejected(()=>Call(wrong,"Awake"),"wrong target Awake refused rather than production fallback");Destroy(wrong);
            Call(target,"Awake");Check(calls==0,"correct isolation factory selected before host construction but remains lazy");
            target.BeginSession(null,ShortCycleTriggerContext.FreeTry);
            Check(calls==1 && Application.PersistentPathReads==before,"pre-Awake injection used on first Prepare and production path factory never called");
            var host=Read<SaveBoundary>(target,"persistenceBoundary");
            Check(host.IsPrepared,"same Session host prepared before failed content preconditions");
            host.Reach(ShortCycleSavePoint.EnterPhase0,new ShortCycleContext());
            var file=Path.Combine(isolated,"default.json");Check(File.Exists(file),"original point writes only declared isolated root");var saved=File.ReadAllBytes(file);
            lease.Dispose();lease.Dispose();host.Reach(ShortCycleSavePoint.Phase1ToPhase2,new ShortCycleContext());
            Check(Convert.ToBase64String(saved)==Convert.ToBase64String(File.ReadAllBytes(file)),"lease disposal revokes already-created store and is idempotent");
            Rejected(()=>ShortCycleSaveIsolation.ForTarget(target,isolated,()=>Store(isolated)),"late reinjection rejected after Awake");
        } finally {lease.Dispose();Destroy(target);}
        var cancelled=Session("Cancelled");var cancel=ShortCycleSaveIsolation.ForTarget(cancelled,Path.Combine(root,"cancelled"),()=>Store(Path.Combine(root,"cancelled")));cancel.Dispose();
        Rejected(()=>Call(cancelled,"Awake"),"disposed early exact-target lease cannot fall back to production");Destroy(cancelled);
        var normal=Session("Normal");Call(normal,"Awake");Rejected(()=>ShortCycleSaveIsolation.ForTarget(normal,isolated,()=>Store(isolated)),"unisolated late target cannot be patched after Awake");Destroy(normal);
        Check(Application.PersistentPathReads==before,"unisolated Awake keeps default path lazy and untouched");
        var invalid=Session("InvalidRoot");Rejected(()=>ShortCycleSaveIsolation.ForTarget(invalid,"relative",()=>Store(isolated)),"relative isolation root refused");Rejected(()=>ShortCycleSaveIsolation.ForTarget(invalid,"C:relative",()=>Store(isolated)),"drive-relative isolation root refused");Destroy(invalid);
        foreach(var kind in new[]{"throws","wrong-root"}) {
            var s=Session("FactoryFailure");var declared=Path.Combine(root,kind);
            var scope=ShortCycleSaveIsolation.ForTarget(s,declared,()=>{if(kind=="throws")throw new IOException("synthetic unavailable");return Store(Path.Combine(root,"different"));});
            try {var warnings=Debug.WarningMessages.Count;Call(s,"Awake");s.BeginSession(null,ShortCycleTriggerContext.FreeTry);Check(!scope.IsActive && Application.PersistentPathReads==before && Debug.WarningMessages.Count==warnings+1,"factory "+kind+" releases lease with one warning and no production fallback");}
            finally {scope.Dispose();Destroy(s);}
        }
        var pendingTarget=Session("PendingUnload","AI280Pending");
        var pendingTargetLease=ShortCycleSaveIsolation.ForTarget(pendingTarget,Path.Combine(root,"pending-unload"),()=>Store(Path.Combine(root,"pending-unload")));
        SceneManager.RaiseUnloaded(pendingTarget.gameObject.scene);
        Check(!pendingTargetLease.IsActive,"scene unload releases an exact-target lease even before Awake");
        Rejected(()=>Call(pendingTarget,"Awake"),"late Awake after pending target scene unload cannot fall back to production");Destroy(pendingTarget);
        var scenePath="Assets/Scenes/ToolTests/AI280Synthetic.unity";var scene=new Scene{path=scenePath,name="AI280Scene",isLoaded=true};
        using(var sceneLease=ShortCycleSaveIsolation.ForScene(scenePath,"Harness/Target",Path.Combine(root,"scene"),()=>Store(Path.Combine(root,"scene")))) {
            SceneManager.SetLoaded(scene);
            var other=Session("Target","Assets/Scenes/Other.unity");Rejected(()=>Call(other,"Awake"),"different scene rejected while isolation lease is pending");Destroy(other);
            var s=Session("Target",scenePath);s.gameObject.scene=scene;var parent=new GameObject{name="Harness"};s.transform.SetParent(parent.transform,false);
            try {Call(s,"Awake");s.BeginSession(null,ShortCycleTriggerContext.FreeTry);Check(Application.PersistentPathReads==before,"scene/hierarchy selection resolves from target Awake itself");SceneManager.RaiseUnloaded(scene);Check(!sceneLease.IsActive,"scene unload releases lease");}
            finally {Destroy(s);}
        }
        SceneManager.SetLoaded(scene);Rejected(()=>ShortCycleSaveIsolation.ForScene(scenePath,"Harness/Target",isolated,()=>Store(isolated)),"already loaded scene cannot install pre-Awake isolation");SceneManager.RaiseUnloaded(scene);
        var pending=ShortCycleSaveIsolation.ForScene(scenePath,"Target",isolated,()=>Store(isolated));pending.Dispose();
        var stale=Session("Target",scenePath);Rejected(()=>Call(stale,"Awake"),"cancelled pending scene cannot use production storage on late load");Destroy(stale);SceneManager.RaiseUnloaded(scene);
        var unused=ShortCycleSaveIsolation.ForScene(scenePath,"Target",isolated,()=>Store(isolated));SceneManager.SetLoaded(scene);SceneManager.RaiseUnloaded(scene);
        using(var next=ShortCycleSaveIsolation.ForScene(scenePath,"Target",isolated,()=>Store(isolated))) {Check(!unused.IsActive && next.IsActive,"unconsumed scene unload permits a fresh lease without stale cancellation");}SceneManager.RaiseUnloaded(scene);
        var last=Session("Fresh");using(var final=ShortCycleSaveIsolation.ForTarget(last,Path.Combine(root,"fresh"),()=>Store(Path.Combine(root,"fresh")))) {Call(last,"Awake");Destroy(last);Check(!final.IsActive,"fresh next case works and destruction releases its lease");}
        Check(Application.PersistentPathReads==before,"all isolation paths avoid real user path reads");
        TextCases();Debug.WarningMessages.Clear();Debug.Messages.Clear();return "PASS: AI280 isolation/text "+PassedCases.Count+" assertions";
    }
    private static readonly Scene TextScene=new Scene{path="AI280Text",name="AI280Text",isLoaded=true};
    private static TextMeshPro Label(bool binding=true)
    {var label=new TextMeshPro();label.gameObject.scene=TextScene;if(binding)label.gameObject.AddComponent<LocalizedText>().Configure("old.key","old fallback");return label;}
    private static RecipeView View(string id,bool unlocked=true)
    {return new RecipeView(id,"name."+id,null,1,unlocked,new[]{new RecipeTagView("tag","tag."+id,null,null)},new[]{new RecipeSlotView("slot",new RecipeIngredientView("item","item."+id,null,2),"action",new RecipeSubstituteView[0])},new[]{new RecipeStepView("step","step."+id,new RecipeIconView[0],new RecipeIconView[0])});}
    private static void TextCases()
    {
        var loc=new LocTableAsset();foreach(var id in new[]{"one","two"})foreach(var type in new[]{"name","tag","item","step"})loc.entries.Add(new LocTableEntry{key=type+"."+id,zh_cn=type+":"+id});LocService.Configure(loc);
        var title=Label();var titleBinding=title.gameObject.GetComponent<LocalizedText>();var tabTitle=Label();var tab=new CurrentRecipeTabPiece();Field(tab,"title",tabTitle);Field(tab,"favoriteStar",new SpriteRenderer{sprite=new Sprite()});
        var ingredient=new IngredientCard();var ingredientName=Label();var badge=Label();Field(ingredient,"nameLabel",ingredientName);Field(ingredient,"badgeLabel",badge);
        var list=new IngredientListPiece();Field(list,"entries",new[]{ingredient});var sticker=new Sticker();var stickerName=Label();Field(sticker,"nameLabel",stickerName);var notes=new StickerNotesPiece();Field(notes,"notes",new[]{sticker});
        var card=new CatalogCard();var cardName=Label();var cardBadge=Label();Field(card,"nameLabel",cardName);Field(card,"badgeLabel",cardBadge);
        var tags=new LocalizedText[2];for(var i=0;i<2;i++)tags[i]=Label().gameObject.GetComponent<LocalizedText>();Field(card,"tagLabels",tags);
        var row=new StepDetailRowPiece();var description=Label();Field(row,"description",description);var modal=new StepDetailModalPiece();Field(modal,"rows",new[]{row});var oldTitle=Label();var oldNumber=Label();Field(modal,"legacyTitle",oldTitle);Field(modal,"legacyNumbers",new[]{oldNumber});
        var book=new RecipeBookBinder();Field(book,"catalogCards",new[]{card});Field(book,"browseTitle",title);Field(book,"ingredientList",list);Field(book,"stickerNotes",notes);Field(book,"currentRecipeTab",tab);Field(book,"stepDetail",modal);
        foreach(var id in new[]{"one","two"}) {
            var view=View(id);book.BindCatalog(new[]{view});book.BindBrowse(view);book.RefreshFavorites(_=>true);LocalizedText.RefreshAll(TextScene);
            Check(title.text=="name:"+id && tabTitle.text==title.text && cardName.text==title.text,"switch "+id+" then RefreshAll retains title/catalog/current tab owner");
            Check(ingredientName.text=="item:"+id && stickerName.text=="tag:"+id && description.text=="step:"+id,"switch "+id+" then RefreshAll retains ingredient/sticker/step keys");
            Check(badge.text=="×2" && cardBadge.text=="★","dynamic counts and favorites cannot restore old localization");
            Check(oldTitle.text==string.Empty && oldNumber.text==string.Empty,"legacy modal clear survives global refresh");
            Check(ReferenceEquals(titleBinding,title.gameObject.GetComponent<LocalizedText>()) && title.gameObject.GetComponent<LocalizedText>().LocalizationKey==view.name_key,"existing component/key updated without removal");
        }
        book.BindCatalog(new[]{View("two",false)});LocalizedText.RefreshAll(TextScene);Check(!card.Select().Succeeded && cardName.text=="name:two","locked card content refresh keeps selection blocked");
        book.Clear();LocalizedText.RefreshAll(TextScene);Check(title.text==string.Empty && tabTitle.text==string.Empty && ingredientName.text==string.Empty && stickerName.text==string.Empty && description.text==string.Empty && badge.text==string.Empty && cardName.text==string.Empty,"clear remains empty after global localization refresh");
        var plain=Label(false);LocalizedText.ConfigureContent(plain,"missing","visible fallback");Check(plain.text=="visible fallback","without binding retains direct visible fallback");
        var bound=Label();LocalizedText.ConfigureContent(bound,"missing","visible fallback");LocalizedText.RefreshAll(TextScene);Check(bound.text=="visible fallback","bound missing key retains current fallback");
        LocService.Configure(null);
    }
}
