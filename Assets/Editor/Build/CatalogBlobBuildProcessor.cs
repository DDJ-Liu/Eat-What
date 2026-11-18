using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using EatWhat.Tools.Persistence;
using EatWhat.Tools.Security;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Prepared builds only. Mutates build-scene copies and task-owned inputs, never generated source assets.</summary>
public sealed class CatalogBlobBuildProcessor : IPreprocessBuildWithReport, IProcessSceneWithReport, IPostprocessBuildWithReport
{
    public const string SourceCatalogPath = "Assets/Generated/DataTables/CK01GeneratedDataCatalog.asset";
    public const string BlobPath = "Assets/StreamingAssets/data/catalog.bin";
    private const string JournalPath = "Library/CatalogBlobBuild.journal.json";
    private static Context current;
    public int callbackOrder { get { return -10000; } }
    private sealed class Context
    {
        public CatalogBuildPlan Plan;
        public CK01GeneratedDataCatalog Catalog;
        public string Root;
        public Scene Guard;
        public SceneSetup[] Scenes;
        public bool CleanUntitledScene;
        public readonly List<CatalogBuildPlan.OwnedInput> Owned = new List<CatalogBuildPlan.OwnedInput>();
        public string Pending, JournalHash;
        public readonly HashSet<int> AuthoringHandles = new HashSet<int>();
    }
    private static string Hash(byte[] bytes) { using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(bytes)).Replace("-",string.Empty).ToLowerInvariant(); }
    private static string Identity(UnityEngine.Object asset)
    { string guid;long id;if(asset==null||!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset,out guid,out id))throw new BuildFailedException("Unstable catalog asset/Sprite identity.");return CatalogBlobSerializer.Key(guid+":"+id.ToString(System.Globalization.CultureInfo.InvariantCulture)); }
    private static bool IsSerializedEmptySprite(ScriptableObject owner,string fieldPath,Sprite sprite)
    { return IsSerializedEmptyReference(owner,fieldPath,sprite); }
    private static bool IsSerializedEmptyPrefab(ScriptableObject owner,string fieldPath,GameObject prefab)
    { return IsSerializedEmptyReference(owner,fieldPath,prefab); }
    private static bool IsSerializedEmptyReference(ScriptableObject owner,string fieldPath,UnityEngine.Object reference)
    {
        if(ReferenceEquals(reference,null)||reference!=null||owner==null||string.IsNullOrEmpty(fieldPath)||
           fieldPath.Any(c=>!(char.IsLetterOrDigit(c)||c=='_')))return false;
        var path=AssetDatabase.GetAssetPath(owner);
        if(string.IsNullOrEmpty(path)||!path.StartsWith("Assets/Generated/DataTables/",StringComparison.Ordinal)||
           !path.EndsWith(".asset",StringComparison.OrdinalIgnoreCase)||EditorUtility.IsDirty(owner))return false;
        string guid;long localId;
        if(!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(owner,out guid,out localId)||
           AssetDatabase.AssetPathToGUID(path)!=guid)return false;
        using(var serialized=new SerializedObject(owner)){
            var property=serialized.FindProperty(fieldPath);
            if(property==null||property.propertyType!=SerializedPropertyType.ObjectReference||
               property.objectReferenceInstanceIDValue!=0)return false;
        }
        var project=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
        var root=Path.GetFullPath(Path.Combine(project,"Assets/Generated/DataTables"))+Path.DirectorySeparatorChar;
        var full=Path.GetFullPath(Path.Combine(project,path));
        if(!full.StartsWith(root,StringComparison.OrdinalIgnoreCase)||!File.Exists(full))return false;
        for(var cursor=Path.GetDirectoryName(full);cursor!=null&&cursor.Length>=project.Length;cursor=Path.GetDirectoryName(cursor)){
            if((File.GetAttributes(cursor)&FileAttributes.ReparsePoint)!=0)return false;
            if(cursor.Equals(project,StringComparison.OrdinalIgnoreCase))break;
        }
        if((File.GetAttributes(full)&FileAttributes.ReparsePoint)!=0)return false;
        var marker="--- !u!114 &"+localId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var field="  "+fieldPath+": ";
        var within=false;var matches=0;
        foreach(var line in File.ReadLines(full)){
            if(line.StartsWith("--- !u!",StringComparison.Ordinal)){if(within)break;within=line==marker;continue;}
            if(within&&line.StartsWith(field,StringComparison.Ordinal)){
                if(line.Substring(field.Length).Trim()!="{fileID: 0}")return false;
                matches++;
            }
        }
        return matches==1;
    }
    private static JObject Capture(out List<CatalogSpriteReference> sprites)
    { List<CatalogPrefabReference> prefabs; return CaptureReferences(out sprites,out prefabs); }
    private static JObject CaptureReferences(out List<CatalogSpriteReference> sprites,out List<CatalogPrefabReference> prefabs)
    {
        var source=AssetDatabase.LoadAssetAtPath<CK01GeneratedDataCatalog>(SourceCatalogPath);if(source==null)throw new BuildFailedException("Source catalog missing.");
        var registry=new Dictionary<string,Sprite>(StringComparer.Ordinal);
        var prefabRegistry=new Dictionary<string,GameObject>(StringComparer.Ordinal);
        var universe=AssetDatabase.FindAssets("t:ScriptableObject",new[]{"Assets/Generated/DataTables"}).SelectMany(g=>AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(g))).OfType<ScriptableObject>().Where(a=>a!=source).ToArray();
        var graph=CatalogBlobSerializer.Capture(source,asset=>{var key=Identity(asset);var sprite=asset as Sprite;if(sprite!=null)registry[key]=sprite;var prefab=asset as GameObject;if(prefab!=null)prefabRegistry[key]=prefab;return key;},universe,IsSerializedEmptySprite,IsSerializedEmptyPrefab);
        prefabs=prefabRegistry.OrderBy(x=>x.Key,StringComparer.Ordinal).Select(x=>new CatalogPrefabReference(x.Key,x.Value)).ToList();
        sprites=registry.OrderBy(x=>x.Key,StringComparer.Ordinal).Select(x=>new CatalogSpriteReference(x.Key,x.Value)).ToList();return graph;
    }
    /// <summary>Explicit pre-build export only. Its generated C# must be reviewed/imported/compiled before BuildPrepared.</summary>
    public static void PrepareSnapshot(string stagingDirectory)
    {
        if(BuildPipeline.isBuildingPlayer)throw new BuildFailedException("Never generate fingerprint source in a Build callback.");
        var staging=ApprovedStaging(stagingDirectory);List<CatalogSpriteReference> sprites;List<CatalogPrefabReference> prefabs;var graph=CaptureReferences(out sprites,out prefabs);
        using(var decoded=CatalogBlobSerializer.Rehydrate(graph,key=>sprites.Single(x=>x.key==key).sprite,key=>prefabs.Single(x=>x.key==key).prefab)) { }
        CatalogBlobStaging.Export(graph,staging,Path.GetFullPath(Path.Combine(Application.dataPath,"../.ai-workspace/outputs")));
    }
    private static string ApprovedStaging(string path)
    {
        if(!Path.IsPathFullyQualified(path))throw new IOException("Absolute task staging path required.");
        var full=Path.GetFullPath(path);var root=Path.GetFullPath(Path.Combine(Application.dataPath,"../.ai-workspace/outputs"))+Path.DirectorySeparatorChar;
        if(!full.StartsWith(root,StringComparison.OrdinalIgnoreCase))throw new IOException("Staging must be under project task outputs.");return full;
    }
    public static BuildReport BuildPrepared(BuildPlayerOptions options,string stagingDirectory)
    {
        var development=(options.options&BuildOptions.Development)!=0;var original=options.extraScriptingDefines;
        options.extraScriptingDefines=CatalogBuildPlan.Defines(original,development);
        if(development)return BuildPipeline.BuildPlayer(options);
        if(current!=null||File.Exists(JournalPath))throw new BuildFailedException("Unsettled catalog build/journal; inspect and explicitly recover owned inputs first.");
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new BuildFailedException("Clean idle Edit Mode required.");
        for(var i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new BuildFailedException("Unsaved authoring scene: refuse build preparation.");
        var stage=ApprovedStaging(stagingDirectory);List<CatalogSpriteReference> sprites;List<CatalogPrefabReference> prefabs;var graph=CaptureReferences(out sprites,out prefabs);var fingerprint=CatalogBlobSerializer.Fingerprint(graph);
        if(CatalogBuildFingerprint.Expected!=fingerprint)throw new BuildFailedException("Prepare fingerprint source and compile before building; callback cannot fix it.");
        var packet=File.ReadAllBytes(Path.Combine(stage,"catalog.bin"));CatalogBlobSerializer.Authenticate(packet,fingerprint,new CatalogBlobCipher(new ObfuscatedStaticKeyProvider()));
        var plan=new CatalogBuildPlan(((string)graph["catalog"]).Substring(0,32),((JArray)graph["assets"]).Select(x=>((string)x["key"]).Substring(0,32)));
        bool cleanUntitled;
        var authoringSetup=CaptureAuthoringSetup(out cleanUntitled);
        var ctx=new Context{Plan=plan,Scenes=authoringSetup,CleanUntitledScene=cleanUntitled,Root="Assets/__CatalogBlobBuild_"+Guid.NewGuid().ToString("N")};
        for(var i=0;i<SceneManager.sceneCount;i++)ctx.AuthoringHandles.Add(SceneManager.GetSceneAt(i).handle);
        try
        {
            current=ctx;
            return CatalogBuildPlan.Execute(()=>PrepareInputs(ctx,packet,sprites,prefabs),()=>{
                var report=BuildPipeline.BuildPlayer(options);if(report==null||report.summary.result!=BuildResult.Succeeded)throw new BuildFailedException("Player build failed.");AssertReport(report,ctx.Plan);return report;
            },()=>Cleanup(ctx));
        }
        finally{options.extraScriptingDefines=original;current=null;}
    }
    private static SceneSetup[] CaptureAuthoringSetup(out bool cleanUntitled)
    {
        cleanUntitled=false;
        var setup=EditorSceneManager.GetSceneManagerSetup();
        if(setup==null||setup.Length==0||setup.Length!=SceneManager.sceneCount)
            throw new BuildFailedException("Unknown or unloaded authoring scene setup; refuse preparation.");
        var active=SceneManager.GetActiveScene();
        var activeCount=0;
        for(var i=0;i<setup.Length;i++)
        {
            var scene=SceneManager.GetSceneAt(i);
            if(!scene.IsValid()||!scene.isLoaded||scene.isDirty||!setup[i].isLoaded||
                !string.Equals(setup[i].path,scene.path,StringComparison.Ordinal)||
                setup[i].isActive!=(scene.handle==active.handle))
                throw new BuildFailedException("Authoring scene setup changed or is not clean and loaded.");
            if(setup[i].isActive)activeCount++;
            if(string.IsNullOrEmpty(scene.path))
            {
                if(setup.Length!=1||scene.GetRootGameObjects().Length!=0)
                    throw new BuildFailedException("Untitled authoring scene has objects or mixed setup; preserve.");
                cleanUntitled=true;
            }
        }
        if(activeCount!=1)throw new BuildFailedException("Authoring setup needs one active loaded scene.");
        return setup;
    }
    private static void AssertJournal(Context ctx)
    {
        if(ctx.JournalHash==null||!File.Exists(JournalPath)||Hash(File.ReadAllBytes(JournalPath))!=ctx.JournalHash)
            throw new IOException("Ownership journal missing or changed; preserve all inputs.");
    }
    private static void WriteJournal(Context ctx)
    {
        if(ctx.JournalHash!=null)AssertJournal(ctx);
        else if(File.Exists(JournalPath))throw new IOException("Unknown ownership journal; preserve.");
        var items=new JArray();
        foreach(var item in ctx.Owned)items.Add(new JObject{["path"]=item.Path,["directory"]=item.IsDirectory,["sha256"]=item.Sha256,["metaSha256"]=item.MetaSha256});
        var root=new JObject{["schema"]=CatalogBuildPlan.OwnershipSchema,["root"]=ctx.Root,["pending"]=ctx.Pending,["owned"]=items};
        File.WriteAllText(JournalPath,root.ToString(),new UTF8Encoding(false));ctx.JournalHash=Hash(File.ReadAllBytes(JournalPath));
    }
    private static void BeginCreate(Context ctx,string path)
    {
        if(ctx.Pending!=null)throw new IOException("Previous creation incomplete; preserve.");
        CatalogBuildPlan.AssertNewOwnedInput(Path.GetFullPath("."),ctx.Root,path);ctx.Pending=path;WriteJournal(ctx);
    }
    private static void FinishCreate(Context ctx,string path,bool directory)
    {
        if(ctx.Pending!=path)throw new IOException("Creation ownership mismatch.");
        ctx.Owned.Add(CatalogBuildPlan.CaptureCreatedInput(Path.GetFullPath("."),ctx.Root,path,directory));ctx.Pending=null;WriteJournal(ctx);
    }
    private static void Folder(Context ctx,string path)
    {
        if(AssetDatabase.IsValidFolder(path))return;
        var parent=Path.GetDirectoryName(path).Replace('\\','/');if(parent!="Assets")Folder(ctx,parent);
        BeginCreate(ctx,path);
        if(string.IsNullOrEmpty(AssetDatabase.CreateFolder(parent,Path.GetFileName(path))))throw new IOException("Cannot create owned input folder.");
        FinishCreate(ctx,path,true);
    }
    private static void PrepareInputs(Context ctx,byte[] packet,List<CatalogSpriteReference> sprites,List<CatalogPrefabReference> prefabs)
    {
        if(File.Exists(BlobPath)||File.Exists(BlobPath+".meta"))throw new BuildFailedException("Existing catalog.bin is not owned by this build; do not overwrite it.");
        AssertImplicitRoots(ctx.Plan);
        CatalogBuildPlan.AssertNewOwnedInput(Path.GetFullPath("."),ctx.Root,ctx.Root);WriteJournal(ctx);Folder(ctx,ctx.Root);
        var catalog=ScriptableObject.CreateInstance<CK01GeneratedDataCatalog>();catalog.name="CK01-ReleaseCatalog";catalog.ConfigureBuildReferences(sprites,prefabs);
        var path=ctx.Root+"/RuntimeCatalog.asset";BeginCreate(ctx,path);AssetDatabase.CreateAsset(catalog,path);ctx.Catalog=catalog;FinishCreate(ctx,path,false);
        var link=new System.Text.StringBuilder("<linker>");foreach(var group in CatalogBlobSerializer.AssetTypes.Concat(CatalogBlobSerializer.ValueTypes).GroupBy(t=>t.Assembly.GetName().Name))
        {link.Append("<assembly fullname=\"").Append(group.Key).Append("\">");foreach(var type in group)link.Append("<type fullname=\"").Append(type.FullName).Append("\" preserve=\"all\"/>");link.Append("</assembly>");}link.Append("</linker>");
        var linkPath=ctx.Root+"/link.xml";BeginCreate(ctx,linkPath);File.WriteAllText(linkPath,link.ToString(),new UTF8Encoding(false));AssetDatabase.ImportAsset(linkPath);FinishCreate(ctx,linkPath,false);
        Folder(ctx,"Assets/StreamingAssets/data");BeginCreate(ctx,BlobPath);using(var output=new FileStream(BlobPath,FileMode.CreateNew,FileAccess.Write)){output.Write(packet,0,packet.Length);output.Flush(true);}AssetDatabase.ImportAsset(BlobPath);FinishCreate(ctx,BlobPath,false);
        // Authoring scenes are clean and closed before build copies can be mutated. No source scene is saved.
        ctx.Guard=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
    }
    private static void AssertImplicitRoots(CatalogBuildPlan plan)
    {
        var resources=AssetDatabase.GetAllAssetPaths().Where(p=>p.Contains("/Resources/")&&!AssetDatabase.IsValidFolder(p)).ToArray();
        var preloaded=PlayerSettings.GetPreloadedAssets().Where(x=>x!=null).Select(AssetDatabase.GetAssetPath).ToArray();
        var roots=resources.Concat(preloaded).Distinct().ToArray();if(roots.Length!=0)plan.AssertNoExcluded(AssetDatabase.GetDependencies(roots,true).Select(AssetDatabase.AssetPathToGUID));
        if(Directory.Exists("Assets/StreamingAssets"))foreach(var file in Directory.EnumerateFiles("Assets/StreamingAssets","*.asset",SearchOption.AllDirectories))plan.AssertNoExcluded(new[]{AssetDatabase.AssetPathToGUID(file.Replace('\\','/'))});
    }
    public void OnPreprocessBuild(BuildReport report)
    {
        if((report.summary.options&BuildOptions.Development)!=0)return;
        if(current==null||current.Catalog==null||CatalogBuildFingerprint.Expected=="UNPREPARED")throw new BuildFailedException("Release must use prepared authenticated catalog entry; no SO fallback build.");
    }
    public void OnProcessScene(Scene scene,BuildReport report)
    {
        if(report==null||(report.summary.options&BuildOptions.Development)!=0)return;
        if(current==null||scene==current.Guard||current.AuthoringHandles.Contains(scene.handle))throw new BuildFailedException("Only loaded build-scene copies may be remapped.");
        foreach(var root in scene.GetRootGameObjects())foreach(var component in root.GetComponentsInChildren<Component>(true))
        {
            if(component==null)continue;var serialized=new SerializedObject(component);var item=serialized.GetIterator();
            while(item.Next(true))
            {
                if(item.propertyType!=SerializedPropertyType.ObjectReference||item.objectReferenceValue==null||item.propertyPath=="m_Script"||item.propertyPath=="m_CorrespondingSourceObject"||item.propertyPath=="m_PrefabInstance"||item.propertyPath=="m_PrefabAsset")continue;
                var path=AssetDatabase.GetAssetPath(item.objectReferenceValue);if(string.IsNullOrEmpty(path))continue;
                var guid=AssetDatabase.AssetPathToGUID(path);
                if(current.Plan.RemapCatalog(guid))item.objectReferenceValue=current.Catalog;
                else current.Plan.AssertNoExcluded(AssetDatabase.GetDependencies(new[]{path},true).Select(AssetDatabase.AssetPathToGUID));
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
    public void OnPostprocessBuild(BuildReport report)
    {if(current!=null&&(report.summary.options&BuildOptions.Development)==0)AssertReport(report,current.Plan);}
    private static void AssertReport(BuildReport report,CatalogBuildPlan plan)
    {foreach(var pack in report.packedAssets)plan.AssertNoExcluded(pack.contents.Select(item=>AssetDatabase.AssetPathToGUID(item.sourceAssetPath)));}
    public static void AssertExistingReport(BuildReport report,JObject authenticatedGraph)
    {
        if(report==null||report.packedAssets==null||report.packedAssets.Length==0)
            throw new BuildFailedException("Detailed original BuildReport packed assets are unavailable.");
        var plan=new CatalogBuildPlan(((string)authenticatedGraph["catalog"]).Substring(0,32),
            ((JArray)authenticatedGraph["assets"]).Select(x=>((string)x["key"]).Substring(0,32)));
        AssertReport(report,plan);
    }
    private static void Cleanup(Context ctx)
    {
        if(ctx.JournalHash==null&&ctx.Owned.Count==0&&ctx.Pending==null)return;
        AssertJournal(ctx);
        if(ctx.Guard.IsValid()&&ctx.Guard.isDirty)throw new BuildFailedException("Unknown guard-scene edits: preserve scene and journal.");
        var cleanup=CatalogBuildPlan.PlanOwnedCleanup(Path.GetFullPath("."),ctx.Root,CatalogBuildPlan.OwnershipSchema,ctx.Pending,ctx.Owned);
        RestoreAuthoring(ctx);
        cleanup.Execute(path=>{if(!AssetDatabase.DeleteAsset(path))throw new IOException("Owned input cleanup failed: "+path);});
        AssertJournal(ctx);File.Delete(JournalPath);
    }
    private static void RestoreAuthoring(Context ctx)
    {
        if(ctx.Scenes!=null&&ctx.Scenes.Length>0)
        {
            if(!ctx.Guard.IsValid()||!ctx.Guard.isLoaded)
                throw new BuildFailedException("Build guard scene disappeared; preserve authoring state and journal.");
            if(ctx.CleanUntitledScene)
            {
                // Unity cannot RestoreSceneManagerSetup with an empty path. A clean rootless
                // single active guard is the same recorded authoring state, without claiming its old handle.
                if(SceneManager.sceneCount!=1||SceneManager.GetSceneAt(0).handle!=ctx.Guard.handle||
                    SceneManager.GetActiveScene().handle!=ctx.Guard.handle||ctx.Guard.isDirty||
                    !string.IsNullOrEmpty(ctx.Guard.path)||ctx.Guard.GetRootGameObjects().Length!=0)
                    throw new BuildFailedException("Untitled authoring scene is not clean and rootless; preserve.");
            }
            else EditorSceneManager.RestoreSceneManagerSetup(ctx.Scenes);
        }
    }
    /// <summary>Never auto-recover after domain/process interruption. Recovery validates exact journal-owned paths/bytes.</summary>
    public static void RecoverOwnedInputs()
    {
        if(current!=null||BuildPipeline.isBuildingPlayer)throw new BuildFailedException("Build still active.");if(!File.Exists(JournalPath))return;
        var bytes=File.ReadAllBytes(JournalPath);var root=SaveEnvelope.ParseCanonicalObject(Encoding.UTF8.GetString(bytes));
        if(root["schema"]==null||root["schema"].Type!=JTokenType.Integer||(int)root["schema"]!=CatalogBuildPlan.OwnershipSchema||root["pending"]==null||root["pending"].Type!=JTokenType.Null||root["owned"]==null||root["owned"].Type!=JTokenType.Array||root["root"]==null||root["root"].Type!=JTokenType.String)
            throw new IOException("Old/incomplete ownership journal; preserve without guessing.");
        var ctx=new Context{Root=(string)root["root"],Scenes=new SceneSetup[0],JournalHash=Hash(bytes)};
        foreach(var token in (JArray)root["owned"])
        {
            if(token.Type!=JTokenType.Object||token["path"]==null||token["path"].Type!=JTokenType.String||token["directory"]==null||token["directory"].Type!=JTokenType.Boolean||token["sha256"]==null||token["metaSha256"]==null||token["metaSha256"].Type!=JTokenType.String||(token["sha256"].Type!=JTokenType.Null&&token["sha256"].Type!=JTokenType.String))
                throw new IOException("Incomplete ownership record; preserve.");
            ctx.Owned.Add(new CatalogBuildPlan.OwnedInput((string)token["path"],(bool)token["directory"],(string)token["sha256"],(string)token["metaSha256"]));
        }
        Cleanup(ctx);
    }
}
