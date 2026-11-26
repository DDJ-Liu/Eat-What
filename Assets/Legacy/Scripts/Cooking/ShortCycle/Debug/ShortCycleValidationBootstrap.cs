using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using EatWhat.Tools.Persistence;
using EatWhat.Tools.Security;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace EatWhat.Cooking.ShortCycle
{
    /// <summary>Explicit validation-only owner. Ordinary Editor/Player startup is unchanged.</summary>
    public sealed class ShortCycleValidationBootstrap : MonoBehaviour
    {
        public const string TargetScene = "Assets/Scenes/Cooking/ShortCycle_P0P1.unity";
        public const string TargetHierarchy = "Managers/SessionManager";
        public const string Workspace = "D:/GameProject/Eat-What";
        public sealed class Request
        { public bool Enabled; public string TaskId, RunId, RootPath; public string DataMode, BaselinePath, BaselineSha256; public int TimeoutSeconds; }
        public static Func<Request> EditorRequestProvider;
        public static Action EditorStopRequested;
        private static ShortCycleValidationBootstrap current;
        public static string State {get;private set;} = "Disabled";
        public static string LastError {get;private set;}
        public static string EvidencePath {get;private set;}
        public static string IsolatedRoot {get;private set;}
        public static int IsolatedFactoryCalls {get;private set;}
        public static bool IsRunning {get{return current!=null;}}
        private ShortCycleSaveIsolation lease;
        private ShortCycleSessionManager target;
        private Request request;
        private StreamWriter evidence;
        private bool stopping;
        private bool dataStarted;
        private DateTime dataDeadline;
        private ShortCycleAutomatedProbe runningProbe; private bool probeBackgroundBefore;
        public static Request ParsePlayerArguments(string[] args)
        {
            args=args??new string[0];
            if(!args.Any(x=>x.StartsWith("-ewValidation",StringComparison.OrdinalIgnoreCase)||x.StartsWith("--ewValidation",StringComparison.OrdinalIgnoreCase)))return null;
            if(args.Count(x=>x=="-ewValidation")!=1)throw new ArgumentException("Exact single -ewValidation enable flag required when any validation argument is present.");
            var values=new Dictionary<string,string>(StringComparer.Ordinal);
            for(var i=0;i<args.Length;i++)
            {
                if(args[i]=="-ewValidation")continue;
                if(!args[i].StartsWith("-ewValidation",StringComparison.OrdinalIgnoreCase)&&!args[i].StartsWith("--ewValidation",StringComparison.OrdinalIgnoreCase))continue;
                if(args[i]!="-ewValidationTask"&&args[i]!="-ewValidationRun"&&args[i]!="-ewValidationRoot"&&args[i]!="-ewValidationData"&&args[i]!="-ewValidationBaseline"&&args[i]!="-ewValidationBaselineSha"&&args[i]!="-ewValidationTimeout")throw new ArgumentException("Unknown validation argument.");
                if(values.ContainsKey(args[i])||i+1>=args.Length||args[i+1].StartsWith("-",StringComparison.Ordinal))throw new ArgumentException("Missing/duplicate validation argument.");
                values.Add(args[i],args[++i]);
            }
            if(!values.ContainsKey("-ewValidationTask")||!values.ContainsKey("-ewValidationRun")||!values.ContainsKey("-ewValidationRoot"))throw new ArgumentException("Validation task/run/root are all required.");
            var data=values.ContainsKey("-ewValidationData");
            if(values.Count!=(data?7:3))throw new ArgumentException("Data validation requires mode, baseline, hash and timeout together.");
            int seconds=0;
            if(data&&(!int.TryParse(values["-ewValidationTimeout"],out seconds)||seconds<1||seconds>120))throw new ArgumentException("Bounded data timeout must be 1..120 seconds.");
            return new Request{Enabled=true,TaskId=values["-ewValidationTask"],RunId=values["-ewValidationRun"],RootPath=values["-ewValidationRoot"],
                DataMode=data?values["-ewValidationData"]:null,BaselinePath=data?values["-ewValidationBaseline"]:null,
                BaselineSha256=data?values["-ewValidationBaselineSha"]:null,TimeoutSeconds=seconds};
        }
        public static string ValidateRequest(Request value)
        {
            if(value==null||!value.Enabled)throw new ArgumentException("Explicit validation enable required.");
            if(!Regex.IsMatch(value.TaskId??"",@"^AI-[0-9]{6}$")||value.TaskId=="AI-000000"||!Regex.IsMatch(value.RunId??"",@"^[A-Za-z0-9][A-Za-z0-9_-]{0,63}$"))
                throw new ArgumentException("Exact task and run identities required.");
            if(string.IsNullOrWhiteSpace(value.RootPath)||!Path.IsPathFullyQualified(value.RootPath)||value.RootPath.Split('/','\\').Any(x=>x=="."||x==".."||x.TrimEnd(' ','.')!=x))
                throw new ArgumentException("Canonical absolute validation root required.");
            var expected=Path.GetFullPath(Path.Combine(Workspace,".ai-workspace/outputs/DETACH-B13-20261019",value.TaskId,"isolated-saves",value.RunId));
            var full=Path.GetFullPath(value.RootPath).TrimEnd('/','\\');
            if(!string.Equals(full,expected,StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Root must match this task/run in approved batch outputs.");
            for(var dir=new DirectoryInfo(full);dir!=null;dir=dir.Parent)
                if((Directory.Exists(dir.FullName)||File.Exists(dir.FullName))&&((File.GetAttributes(dir.FullName)&FileAttributes.ReparsePoint)!=0||File.Exists(dir.FullName)))
                    throw new IOException("Redirect/non-directory validation ancestor: "+dir.FullName);
            if(!string.IsNullOrEmpty(value.DataMode))
            {
                if(value.DataMode!="release"&&value.DataMode!="development"&&value.DataMode!="reject-tampered")
                    throw new ArgumentException("Unknown data validation mode.");
                if(value.TimeoutSeconds<1||value.TimeoutSeconds>120||!Regex.IsMatch(value.BaselineSha256??"",@"^[0-9a-f]{64}$"))
                    throw new ArgumentException("Exact baseline SHA and bounded timeout required.");
                var taskRoot=Path.GetDirectoryName(Path.GetDirectoryName(full));
                var baseline=Path.GetFullPath(Path.Combine(taskRoot,"baseline/catalog.graph.json"));
                if(string.IsNullOrEmpty(value.BaselinePath)||!Path.IsPathFullyQualified(value.BaselinePath)||
                   !string.Equals(Path.GetFullPath(value.BaselinePath),baseline,StringComparison.OrdinalIgnoreCase)||
                   !File.Exists(baseline))throw new IOException("Exact current-task baseline required.");
                for(var node=new FileInfo(baseline) as FileSystemInfo;node!=null;node=(node as FileInfo)!=null?((FileInfo)node).Directory:((DirectoryInfo)node).Parent)
                    if((node.Attributes&FileAttributes.ReparsePoint)!=0)throw new IOException("Redirected baseline.");
            }
            else if(value.BaselinePath!=null||value.BaselineSha256!=null||value.TimeoutSeconds!=0)
                throw new ArgumentException("Data options require explicit data mode.");
            return full;
        }
        private static ShortCycleSessionManager[] Sessions()
        {return Resources.FindObjectsOfTypeAll<ShortCycleSessionManager>().Where(s=>s!=null&&s.gameObject.scene.IsValid()).ToArray();}
        private static string Hierarchy(Transform value)
        {
            var parts=new List<string>();
            for(var node=value;node!=null;node=node.parent){if(string.IsNullOrEmpty(node.gameObject.name)||node.gameObject.name.Contains("/"))throw new InvalidOperationException("Ambiguous target hierarchy.");parts.Insert(0,node.gameObject.name);}
            return string.Join("/",parts);
        }
        public static bool Begin(Request value)
        {
            if(value==null||!value.Enabled)return false;
            try
            {
                if(!Application.isPlaying)throw new InvalidOperationException("Runtime installation requires Play lifecycle.");
                if(!string.IsNullOrEmpty(value.DataMode)&&Application.isEditor)throw new InvalidOperationException("Data validation mode requires an isolated Player.");
                if(current!=null)throw new InvalidOperationException("Duplicate validation owner.");
                IsolatedRoot=ValidateRequest(value);
                if(!string.IsNullOrEmpty(value.DataMode)&&Directory.Exists(IsolatedRoot))
                    throw new IOException("Fresh data validation run root required.");
                var sessions=Sessions();
                if(sessions.Any(s=>s.PersistenceAwakeStarted))throw new InvalidOperationException("Fresh pre-Awake target required; never reuse an awakened object.");
                if(sessions.Length>1||sessions.Length==1&&(sessions[0].gameObject.scene.path!=TargetScene||Hierarchy(sessions[0].transform)!=TargetHierarchy))
                    throw new InvalidOperationException("Expected one exact approved Session or an empty bootstrap scene.");
                if(sessions.Length==0&&SceneManager.GetSceneByPath(TargetScene).isLoaded)throw new InvalidOperationException("Loaded target scene has no exact target.");
                var go=new GameObject("ShortCycleValidationOwner");DontDestroyOnLoad(go);
                var owner=go.AddComponent<ShortCycleValidationBootstrap>();current=owner;owner.request=value;owner.dataDeadline=DateTime.UtcNow.AddSeconds(value.TimeoutSeconds);SceneManager.sceneUnloaded+=owner.TargetUnloaded;
                if(!string.IsNullOrEmpty(value.DataMode))SceneManager.sceneLoaded+=owner.TargetLoaded;
                LastError=null;EvidencePath=null;IsolatedFactoryCalls=0;State="Installing";
                if(sessions.Length==1){owner.target=sessions[0];owner.lease=ShortCycleSaveIsolation.ForTarget(owner.target,IsolatedRoot,owner.CreateStore);}
                else owner.lease=ShortCycleSaveIsolation.ForScene(TargetScene,TargetHierarchy,IsolatedRoot,owner.CreateStore);
                ValidateRequest(value);Directory.CreateDirectory(IsolatedRoot);
                EvidencePath=Path.Combine(IsolatedRoot,"validation-host.log");
                owner.evidence=new StreamWriter(new FileStream(EvidencePath,FileMode.CreateNew,FileAccess.Write,FileShare.Read),new UTF8Encoding(false)){AutoFlush=true};
                owner.Record("enabled=true; task="+value.TaskId+"; run="+value.RunId+"; root="+IsolatedRoot+"; exact="+TargetScene+"#"+TargetHierarchy+"; installedBeforeAwake=true");
                State="WaitingForAwake";
                if(sessions.Length==0)SceneManager.LoadScene(TargetScene,LoadSceneMode.Single);
                return true;
            }
            catch(Exception error){FailClosed(error);return false;}
        }
        public static bool BeginPlayerArguments(string[] args)
        {try{return Begin(ParsePlayerArguments(args));}catch(Exception error){FailClosed(error);return false;}}
        public static bool BeginEditorRequest()
        {try{return Begin(EditorRequestProvider==null?null:EditorRequestProvider());}catch(Exception error){FailClosed(error);return false;}}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void BeforeSceneLoad()
        {
#if UNITY_EDITOR
            BeginEditorRequest();
#else
            BeginPlayerArguments(Environment.GetCommandLineArgs());
#endif
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void NewLifecycle()
        {Stop("new runtime lifecycle");State="Disabled";LastError=null;EvidencePath=null;IsolatedRoot=null;IsolatedFactoryCalls=0;}
        private JsonSaveStore CreateStore()
        {
            try
            {
                var root=ValidateRequest(request);var keys=new ObfuscatedStaticKeyProvider();
                var store=new JsonSaveStore(root,SaveCodecFactory.CreateForCurrentBuild(keys),new EnvelopeAuthenticator(keys));
                if(!string.Equals(store.RootPath,IsolatedRoot,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Factory root mismatch.");
                IsolatedFactoryCalls++;Record("isolatedFactoryCalls="+IsolatedFactoryCalls+"; store.RootPath="+store.RootPath+"; defaultFactorySelected=false");
                return store;
            }
            catch(Exception error){FailClosed(error);throw;}
        }
        public static void ObserveReady()
        {
            if(current==null)return;
            try
            {
                var owner=current;var sessions=Sessions();
                if(sessions.Length!=1||sessions[0].gameObject.scene.path!=TargetScene||Hierarchy(sessions[0].transform)!=TargetHierarchy)
                    throw new InvalidOperationException("Loaded validation target is missing/wrong/duplicated.");
                owner.target=sessions[0];
                if(owner.target.PersistenceEnded||owner.lease==null||!owner.lease.IsActive)throw new InvalidOperationException("Validation lifetime ended unexpectedly.");
                if(!owner.target.PersistenceAwakeStarted)return;
                if(!owner.target.PersistenceIsolationRequired)throw new InvalidOperationException("Target did not consume isolation.");
                if(State!="Ready"){State="Ready";owner.Record("targetAwake=true; isolationRequired=true; leaseActive=true; state=Ready");}
            }
            catch(Exception error)
            {if(current!=null&&current.request!=null&&!string.IsNullOrEmpty(current.request.DataMode))current.FailData(error);else FailClosed(error);}
        }
        private void TargetUnloaded(Scene scene)
        {if(scene.path==TargetScene){if(request!=null&&!string.IsNullOrEmpty(request.DataMode))FailData(new InvalidOperationException("Target scene unloaded before data completion."));else Stop("target scene unloaded");}}
        private void TargetLoaded(Scene scene,LoadSceneMode mode)
        {if(scene.path==TargetScene&&!dataStarted){ObserveReady();if(State=="Ready")VerifyData();}}
        private void Update()
        {
            if(!ReferenceEquals(current,this))return;
            if(request!=null&&!string.IsNullOrEmpty(request.DataMode))
            {
                if(DateTime.UtcNow>dataDeadline){FailData(new TimeoutException("Data validation deadline exceeded."));return;}
                if(State!="Ready"&&SceneManager.GetSceneByPath(TargetScene).isLoaded)ObserveReady();
                if(State=="Ready"&&!dataStarted)VerifyData();
                if(State=="Ready"&&(target==null||target.PersistenceEnded||lease==null||!lease.IsActive))
                    FailData(new InvalidOperationException("Validation target ended unexpectedly."));
                return;
            }
            if(State=="Ready"){if(target==null||target.PersistenceEnded||lease==null||!lease.IsActive)Stop("target ended");}
            else if(SceneManager.GetSceneByPath(TargetScene).isLoaded)ObserveReady();
        }
        private sealed class ReferenceComparer : IEqualityComparer<UnityEngine.Object>
        {
            public bool Equals(UnityEngine.Object a,UnityEngine.Object b){return ReferenceEquals(a,b);}
            public int GetHashCode(UnityEngine.Object value){return RuntimeHelpers.GetHashCode(value);}
        }
        private static object PrivateField(object owner,string name)
        {
            var field=owner.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic);
            if(field==null)throw new InvalidOperationException("Validation source field missing: "+name);
            return field.GetValue(owner);
        }
        private static string Sha(byte[] bytes)
        {using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
        private static void Bind(IDictionary<UnityEngine.Object,string> ids,IDictionary<string,UnityEngine.Object> reverse,UnityEngine.Object value,string key)
        {
            if(ReferenceEquals(value,null)||value==null)throw new InvalidOperationException("Missing validation object: "+key);
            CatalogBlobSerializer.Key(key);
            string prior;UnityEngine.Object other;
            if(ids.TryGetValue(value,out prior)&&prior!=key||reverse.TryGetValue(key,out other)&&!ReferenceEquals(other,value))
                throw new InvalidOperationException("Duplicate or changed validation identity: "+key);
            ids[value]=key;reverse[key]=value;
        }
        private static void LearnSprites(object value,Type type,JToken expected,IDictionary<UnityEngine.Object,string> ids,IDictionary<string,UnityEngine.Object> reverse)
        {
            if(type==typeof(Sprite))
            {
                if(expected.Type==JTokenType.Null){if(value!=null)throw new InvalidOperationException("Unexpected nonempty Sprite.");return;}
                var sprite=value as Sprite;
                if(sprite==null)throw new InvalidOperationException("Missing expected Sprite.");
                Bind(ids,reverse,sprite,(string)expected["sprite"]);return;
            }
            if(value==null)return;
            if(type.IsGenericType&&type.GetGenericTypeDefinition()==typeof(List<>))
            {
                var list=(IList)value;var array=expected as JArray;
                if(array==null||list.Count!=array.Count)throw new InvalidOperationException("Sprite relation list changed.");
                for(var i=0;i<list.Count;i++)LearnSprites(list[i],type.GetGenericArguments()[0],array[i],ids,reverse);
            }
            else if(type==typeof(LocTableEntry))
                foreach(var field in CatalogBlobSerializer.Fields(type))
                    LearnSprites(field.GetValue(value),field.FieldType,expected[field.Name],ids,reverse);
        }
        private JObject ObserveCatalog(JObject baseline,string sourceKind)
        {
            var catalog=target.ValidationCatalog;
            if(catalog==null)throw new InvalidOperationException("Exact target has no bound Catalog.");
            var sourceBefore=PrivateField(catalog,"activeSource");
            var tables=catalog.Tables; // The current Session-bound source performs its real read/authentication here.
            var sourceAfter=PrivateField(catalog,"activeSource");
            if(!ReferenceEquals(sourceBefore,sourceAfter)||tables==null||tables.Count!=14)
                throw new InvalidOperationException("Catalog source changed or fourteen tables unavailable.");
            var expectedTables=(JArray)baseline["tables"];
            if(expectedTables.Count!=tables.Count)throw new InvalidOperationException("Baseline table count changed.");
            var ids=new Dictionary<UnityEngine.Object,string>(new ReferenceComparer());
            var reverse=new Dictionary<string,UnityEngine.Object>(StringComparer.Ordinal);
            Bind(ids,reverse,catalog,(string)baseline["catalog"]);
            var rows=0;
            for(var i=0;i<tables.Count;i++)
            {
                var actual=tables[i];var expected=(JObject)expectedTables[i];var expectedRows=(JArray)expected["rows"];
                if(actual==null||actual.TableName!=(string)expected["name"]||actual.Rows.Count!=expectedRows.Count)
                    throw new InvalidOperationException("Catalog table membership changed.");
                if(expected["aggregate"].Type==JTokenType.Null){if(actual.AggregateAsset!=null)throw new InvalidOperationException("Unexpected aggregate.");}
                else Bind(ids,reverse,actual.AggregateAsset,(string)expected["aggregate"]);
                for(var j=0;j<actual.Rows.Count;j++)
                {
                    var row=actual.Rows[j];var reference=(JObject)expectedRows[j];
                    if(row==null||row.StableId!=(string)reference["id"])throw new InvalidOperationException("Catalog row identity changed.");
                    Bind(ids,reverse,row.Asset,(string)reference["asset"]);rows++;
                }
            }
            if(rows!=154)throw new InvalidOperationException("Three-recipe catalog row count changed.");
            var expectedAssets=((JArray)baseline["assets"]).ToDictionary(x=>(string)x["key"],x=>x,StringComparer.Ordinal);
            IEnumerable<ScriptableObject> universe;
            if(sourceKind=="EncryptedBlob")
            {
                var decoded=PrivateField(sourceAfter,"decoded") as CatalogDecodedData;
                if(decoded==null||decoded.Assets.Count!=expectedAssets.Count)throw new InvalidOperationException("Authenticated asset set incomplete.");
                foreach(var pair in decoded.Assets)Bind(ids,reverse,pair.Value,pair.Key);
                var registry=PrivateField(catalog,"releaseSprites") as List<CatalogSpriteReference>;
                if(registry==null||registry.Count!=((JArray)baseline["sprites"]).Count)throw new InvalidOperationException("Sprite registry count changed.");
                foreach(var item in registry)
                {
                    if(item==null)throw new InvalidOperationException("Null Sprite registry entry.");
                    Bind(ids,reverse,item.sprite,item.key);
                }
                var expectedSprites=new HashSet<string>(((JArray)baseline["sprites"]).Select(x=>(string)x),StringComparer.Ordinal);
                if(registry.Any(x=>!expectedSprites.Contains(x.key)))throw new InvalidOperationException("Unknown Sprite registry identity.");
                universe=decoded.Assets.Values;
            }
            else
            {
                foreach(var pair in reverse.ToArray())
                {
                    JToken record;
                    if(!expectedAssets.TryGetValue(pair.Key,out record))continue;
                    var asset=pair.Value as ScriptableObject;
                    if(asset==null||asset.GetType().FullName!=(string)record["type"])throw new InvalidOperationException("Development asset type changed.");
                    foreach(var field in CatalogBlobSerializer.Fields(asset.GetType()))
                        LearnSprites(field.GetValue(asset),field.FieldType,record["fields"][field.Name],ids,reverse);
                }
                universe=reverse.Values.OfType<ScriptableObject>().Where(x=>!ReferenceEquals(x,catalog)).ToArray();
            }
            Func<UnityEngine.Object,string> identity=obj=>{string key;if(!ids.TryGetValue(obj,out key))throw new InvalidOperationException("Unknown runtime catalog identity.");return key;};
            var graph=CatalogBlobSerializer.Capture(catalog,identity,universe);
            var comparison=(JObject)baseline.DeepClone();
            if(sourceKind=="ScriptableObject")
                comparison["assets"]=new JArray(((JArray)baseline["assets"]).Where(x=>reverse.ContainsKey((string)x["key"])));
            if(!JToken.DeepEquals(graph,comparison))throw new InvalidOperationException("Current Catalog graph differs from authenticated baseline.");
            var recipe=(JObject)expectedTables.Single(x=>(string)x["name"]=="Recipes");
            if(((JArray)recipe["rows"]).Count!=3)throw new InvalidOperationException("Three-recipe baseline changed.");
            return new JObject{["result"]="PASS",["sourceKind"]=sourceKind,["observedAssets"]=((JArray)graph["assets"]).Count,
                ["baselineAssets"]=expectedAssets.Count,["tables"]=tables.Count,["rows"]=rows,["sprites"]=((JArray)graph["sprites"]).Count,
                ["recipes"]=3,["graphFingerprint"]=CatalogBlobSerializer.Fingerprint(graph),["fullAssetCoverage"]=sourceKind=="EncryptedBlob",
                ["authenticationRejected"]=false,["fallbackUsed"]=false};
        }
        private JObject ReadData()
        {
            ValidateRequest(request); // Recheck root and baseline ancestry at the actual read boundary.
            var bytes=File.ReadAllBytes(request.BaselinePath);var sha=Sha(bytes);
            if(sha!=request.BaselineSha256)throw new InvalidOperationException("Bound baseline bytes changed.");
            var baseline=SaveEnvelope.ParseCanonicalObject(new UTF8Encoding(false,true).GetString(bytes));
            CatalogBlobSerializer.ValidateMetadata(baseline);
            if(((JArray)baseline["assets"]).Count!=169||((JArray)baseline["tables"]).Count!=14||
               ((JArray)baseline["sprites"]).Count!=13||CatalogBlobSerializer.Fingerprint(baseline)!=sha)
                throw new InvalidOperationException("Accepted baseline graph contract changed.");
            var catalog=target.ValidationCatalog;
            if(catalog==null)throw new InvalidOperationException("Current Session has no Catalog.");
            var expectedKind=request.DataMode=="development"?"ScriptableObject":"EncryptedBlob";
            var kind=catalog.SourceKind;
            if(kind!=expectedKind)throw new InvalidOperationException("Current Catalog source kind is wrong: "+kind);
            JObject observation;
            if(request.DataMode=="reject-tampered")
            {
                try { var ignored=catalog.Tables;throw new InvalidOperationException("Tampered current source was accepted."); }
                catch(CryptographicException e)
                {
                    if(e.Message!="Authentication failed."||catalog.SourceKind!="EncryptedBlob"||
                       PrivateField(PrivateField(catalog,"activeSource"),"decoded")!=null)
                        throw new InvalidOperationException("Authentication rejection or no-fallback proof failed.");
                    observation=new JObject{["result"]="EXPECTED_REJECTION",["sourceKind"]=kind,
                        ["authenticationRejected"]=true,["rejectionType"]=e.GetType().Name,["fallbackUsed"]=false,
                        ["partialDecodedAssets"]=0};
                }
            }
            else observation=ObserveCatalog(baseline,kind);
            observation["taskId"]=request.TaskId;observation["runId"]=request.RunId;
            observation["baselineSha256"]=sha;observation["mode"]=request.DataMode;
            return observation;
        }
        private void WriteData(JObject result)
        {
            var path=Path.Combine(IsolatedRoot,"validation-data.json");
            using(var stream=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None))
            using(var writer=new StreamWriter(stream,new UTF8Encoding(false)))writer.Write(result.ToString(Newtonsoft.Json.Formatting.None));
        }
        private void VerifyData()
        {
            if(dataStarted||!ReferenceEquals(current,this)||State!="Ready")return;
            dataStarted=true;
            try
            {
                var result=ReadData();
                Stop("data validation complete"); // Target destruction and lease disposal precede the final result.
                result["targetDestroyedBeforeLeaseRelease"]=true;result["leaseReleased"]=true;
                WriteData(result);State="Completed";Application.Quit(0);
            }
            catch(Exception error){FailData(error);}
        }
        private void FailData(Exception error)
        {
            if(dataStarted&&State=="Faulted")return;
            dataStarted=true;LastError=error.GetType().Name+": "+error.Message;
            var cleaned=false;
            try{Stop("data validation fault");cleaned=!IsRunning;}
            catch(Exception cleanup){LastError+="; cleanup="+cleanup.Message;}
            try{WriteData(new JObject{["result"]="FAIL",["taskId"]=request.TaskId,["runId"]=request.RunId,
                ["mode"]=request.DataMode,["error"]=LastError,["targetDestroyedBeforeLeaseRelease"]=cleaned,
                ["leaseReleased"]=cleaned});}
            catch(Exception output){LastError+="; output="+output.Message;}
            State="Faulted";Debug.LogError("[VALIDATION-HOST] "+LastError);
            if(Application.isEditor){if(EditorStopRequested!=null)EditorStopRequested();}
            else Application.Quit(2);
        }
        public static void RunOriginalProbe()
        {
            if(current==null||State!="Ready")throw new InvalidOperationException("Isolated target is not ready.");
            var probes=Resources.FindObjectsOfTypeAll<ShortCycleAutomatedProbe>().Where(p=>p!=null&&p.gameObject.scene.path==TargetScene).ToArray();
            if(probes.Length!=1)throw new InvalidOperationException("One original probe required.");
            current.probeBackgroundBefore=Application.runInBackground;current.runningProbe=probes[0];probes[0].enabled=true;probes[0].RunProbe();current.Record("originalProbe.RunProbe invoked; original1-20 sequence unchanged");
        }
        private void Record(string value){if(evidence!=null)evidence.WriteLine(DateTime.UtcNow.ToString("O")+" "+value);}
        private static ShortCycleSessionManager[] BlockKnownSessions()
        {
            var sessions=Sessions();
            // Existing API gates only: seal fallback and revoke captured factories before callbacks/removal.
            foreach(var session in sessions){session.RequirePersistenceIsolation();session.DisableIsolatedStorage();}
            return sessions;
        }
        private static void DestroySession(ShortCycleSessionManager session)
        {
            if(session==null)return;
            session.gameObject.SetActive(false); // Also prevents Start if immediate removal reports an error.
            DestroyImmediate(session.gameObject);
        }
        public static void Stop(string reason)
        {
            var owner=current;if(owner==null||owner.stopping)return;owner.stopping=true;
            try
            {
                var sessions=BlockKnownSessions();
                if(owner.runningProbe!=null){owner.runningProbe.StopAllCoroutines();owner.runningProbe.enabled=false;Application.runInBackground=owner.probeBackgroundBefore;owner.runningProbe=null;}
                foreach(var session in sessions)DestroySession(session);
                if(owner.lease!=null){owner.lease.Dispose();owner.lease=null;}
                SceneManager.sceneUnloaded-=owner.TargetUnloaded;SceneManager.sceneLoaded-=owner.TargetLoaded;
                owner.Record("targetDestroyedBeforeLeaseRelease=true; stop="+reason+"; isolatedFactoryCalls="+IsolatedFactoryCalls);
                if(owner.evidence!=null){owner.evidence.Dispose();owner.evidence=null;}
                current=null;State="Stopped";DestroyImmediate(owner.gameObject);
            }
            catch{owner.stopping=false;throw;} // Retain the lease if target destruction failed.
        }
        private static void FailClosed(Exception error)
        {
            LastError=error.GetType().Name+": "+error.Message;
            if(Application.isPlaying)
            {
                try
                {
                    if(current!=null)Stop("fault");
                    else foreach(var session in BlockKnownSessions())DestroySession(session);
                }
                catch(Exception cleanup){LastError+="; cleanup="+cleanup.Message;}
            }
            State="Faulted";Debug.LogError("[VALIDATION-HOST] "+LastError);
            if(Application.isEditor){if(EditorStopRequested!=null)EditorStopRequested();}
            else Application.Quit(2);
        }
        private void OnDisable(){if(ReferenceEquals(current,this))Stop("owner disabled");}
        private void OnDestroy(){if(ReferenceEquals(current,this))Stop("owner destroyed");}
        private void OnApplicationQuit(){if(ReferenceEquals(current,this))Stop("application quit");}
    }
}