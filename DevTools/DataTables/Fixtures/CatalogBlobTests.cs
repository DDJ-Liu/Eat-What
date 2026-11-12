using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using EatWhat.Tools.Persistence;
using EatWhat.Tools.Security;
using Newtonsoft.Json.Linq;
using UnityEngine;
public static class CatalogBlobTests
{
    public static IList<string> Cases { get; private set; }
    private static void Check(bool value,string name){if(!value)throw new Exception("SEC02: "+name);Cases.Add(name);}
    private static void Reject(Action work,string name){var rejected=false;try{work();}catch(Exception e){if(e is OutOfMemoryException)throw;rejected=true;}Check(rejected,name);}
    private sealed class OwnedFixture
    {
        public string Project,Output,Root="Assets/__CatalogBlobBuild_"+new string('a',32);
        public readonly List<CatalogBuildPlan.OwnedInput> Inputs=new List<CatalogBuildPlan.OwnedInput>();
        public string Full(string asset){return Path.GetFullPath(Path.Combine(Project,asset));}
        public void Create(string asset,bool directory)
        {
            CatalogBuildPlan.AssertNewOwnedInput(Project,Root,asset);
            if(directory)Directory.CreateDirectory(Full(asset));else File.WriteAllText(Full(asset),"owned asset "+asset);
            File.WriteAllText(Full(asset)+".meta","owned metadata "+asset);
            Inputs.Add(CatalogBuildPlan.CaptureCreatedInput(Project,Root,asset,directory));
        }
        public void DeletePair(string asset)
        {
            var full=Full(asset);var prefix=Path.GetFullPath(Output).TrimEnd('\\','/')+Path.DirectorySeparatorChar;
            if(!full.StartsWith(prefix,StringComparison.OrdinalIgnoreCase))throw new IOException("Synthetic delete escaped task outputs.");
            if(Directory.Exists(full))Directory.Delete(full,false);else File.Delete(full);
            File.Delete(full+".meta");
        }
    }
    private static OwnedFixture NewOwnedFixture(string output,string name)
    {
        var f=new OwnedFixture{Project=Path.Combine(output,name),Output=output};
        if(Directory.Exists(f.Project))throw new IOException("Fixture output collision.");
        Directory.CreateDirectory(Path.Combine(f.Project,"Assets/StreamingAssets"));
        File.WriteAllText(Path.Combine(f.Project,"Assets/StreamingAssets/user.keep"),"pre-existing parent stays");
        f.Create(f.Root,true);f.Create(f.Root+"/RuntimeCatalog.asset",false);
        f.Create(f.Root+"/Child",true);f.Create(f.Root+"/Child/item.txt",false);
        f.Create("Assets/StreamingAssets/data",true);f.Create("Assets/StreamingAssets/data/catalog.bin",false);return f;
    }
    private static string OwnedSnapshot(string project)
    {
        var parts=new List<string>();var todo=new Stack<string>();todo.Push(project);
        while(todo.Count!=0)foreach(var entry in Directory.EnumerateFileSystemEntries(todo.Pop()))
        {
            var flags=File.GetAttributes(entry);
            if((flags&FileAttributes.ReparsePoint)!=0){parts.Add(entry+":LINK");continue;}
            if((flags&FileAttributes.Directory)!=0){parts.Add(entry+":DIR");todo.Push(entry);}
            else parts.Add(entry+":"+Convert.ToBase64String(File.ReadAllBytes(entry)));
        }
        return string.Join("\n",parts.OrderBy(x=>x,StringComparer.Ordinal));
    }
    public static JObject RunOwnership(string output,Action<string,string> createDirectoryRedirect)
    {
        Cases=new List<string>();output=Path.GetFullPath(output);
        var ownedOutput=Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(),".ai-workspace/outputs"))+Path.DirectorySeparatorChar;
        if(!output.StartsWith(ownedOutput,StringComparison.OrdinalIgnoreCase))throw new IOException("Ownership fixtures require their task output root.");
        Directory.CreateDirectory(output);var positive=NewOwnedFixture(output,"owned");var deleted=0;
        CatalogBuildPlan.PlanOwnedCleanup(positive.Project,positive.Root,2,null,positive.Inputs).Execute(p=>{positive.DeletePair(p);deleted++;});
        Check(deleted==6&&!Directory.Exists(positive.Full(positive.Root))&&!File.Exists(positive.Full(positive.Root)+".meta"),"all6 owned asset/folder + exact meta pairs clean through actual shared guard");
        Check(File.ReadAllText(Path.Combine(positive.Project,"Assets/StreamingAssets/user.keep"))=="pre-existing parent stays","pre-existing parent/unknown outside owned deletion scope retained");
        CatalogBuildPlan.PlanOwnedCleanup(positive.Project,positive.Root,2,null,positive.Inputs).Execute(p=>{throw new Exception("Idempotent cleanup called deletion.");});
        Check(true,"both asset and meta absent is idempotent; no deletion callback");
        var rejected=0;
        foreach(var name in new[]{"unknown orphan meta","known asset meta drift","unknown normal file","unknown empty directory","root folder meta drift","nested folder meta drift","unknown late streaming meta","known asset bytes drift","missing ownership record","old journal schema","missing journal records","pending creation","missing meta hash","path escape","alias duplicate","missing half pair","unknown redirect","known directory redirect"})
        {
            var f=NewOwnedFixture(output,"reject-"+rejected);var schema=2;string pending=null;IEnumerable<CatalogBuildPlan.OwnedInput> records=f.Inputs;
            var main=f.Root+"/RuntimeCatalog.asset";var child=f.Root+"/Child";
            switch(name)
            {
                case "unknown orphan meta":File.WriteAllText(f.Full(f.Root+"/foreign.asset.meta"),"foreign orphan bytes");break;
                case "known asset meta drift":File.AppendAllText(f.Full(main)+".meta"," drift");break;
                case "unknown normal file":File.WriteAllText(f.Full(f.Root+"/foreign.txt"),"foreign file");break;
                case "unknown empty directory":Directory.CreateDirectory(f.Full(f.Root+"/foreign"));break;
                case "root folder meta drift":File.AppendAllText(f.Full(f.Root)+".meta"," drift");break;
                case "nested folder meta drift":File.AppendAllText(f.Full(child)+".meta"," drift");break;
                case "unknown late streaming meta":File.WriteAllText(f.Full("Assets/StreamingAssets/data/foreign.asset.meta"),"late unknown");break;
                case "known asset bytes drift":File.AppendAllText(f.Full(main)," drift");break;
                case "missing ownership record":records=f.Inputs.Where(x=>x.Path!=main).ToArray();break;
                case "old journal schema":schema=1;break;
                case "missing journal records":records=null;break;
                case "pending creation":pending=f.Root+"/pending.asset";break;
                case "missing meta hash":records=f.Inputs.Select(x=>x.Path==main?new CatalogBuildPlan.OwnedInput(x.Path,x.IsDirectory,x.Sha256,null):x).ToArray();break;
                case "path escape":records=f.Inputs.Concat(new[]{new CatalogBuildPlan.OwnedInput("Assets/Generated/foreign.asset",false,new string('A',64),new string('B',64))}).ToArray();break;
                case "alias duplicate":records=f.Inputs.Concat(new[]{f.Inputs[0]}).ToArray();break;
                case "missing half pair":File.Delete(f.Full(main)+".meta");break;
                case "unknown redirect":
                case "known directory redirect":
                    var target=Path.Combine(f.Project,"redirect-target");Directory.CreateDirectory(target);File.WriteAllText(Path.Combine(target,"user.keep"),"redirect bytes retained");
                    if(name=="known directory redirect"){f.DeletePair(child+"/item.txt");var full=f.Full(child);if(!full.StartsWith(output+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new IOException("Fixture directory escaped.");Directory.Delete(full,false);}
                    createDirectoryRedirect(f.Full(name=="unknown redirect"?f.Root+"/foreign-link":child),target);break;
            }
            var before=OwnedSnapshot(f.Project);var calls=0;var failed=false;string error=null;
            try{CatalogBuildPlan.PlanOwnedCleanup(f.Project,f.Root,schema,pending,records).Execute(p=>{calls++;f.DeletePair(p);});}catch(IOException e){failed=true;error=e.Message;}catch(InvalidOperationException e){failed=true;error=e.Message;}
            Check(failed&&calls==0,"preflight before FIRST deletion rejects "+name);
            Check(before==OwnedSnapshot(f.Project),"all synthetic known/unknown bytes and links retained after "+name);
            if(string.IsNullOrEmpty(error))throw new Exception("Rejection lacks a diagnostic path/reason.");
            rejected++;
        }
        var collision=NewOwnedFixture(output,"creation-collision");var prospective=collision.Root+"/future.asset";File.WriteAllText(collision.Full(prospective)+".meta","pre-existing orphan");
        Reject(()=>CatalogBuildPlan.AssertNewOwnedInput(collision.Project,collision.Root,prospective),"creation refuses existing orphan meta before API mutation");
        Reject(()=>CatalogBuildPlan.AssertNewOwnedInput(collision.Project,collision.Root,collision.Root),"creation cannot adopt an existing directory");
        var incomplete=NewOwnedFixture(output,"incomplete");Reject(()=>CatalogBuildPlan.PlanOwnedCleanup(incomplete.Project,incomplete.Root,0,null,incomplete.Inputs),"missing schema proof rejected");
        return new JObject{["success"]=true,["assertionCount"]=Cases.Count,["rejectedBeforeDeleteCases"]=rejected,["positivePairDeletes"]=deleted,["cases"]=new JArray(Cases),["nativeOperations"]=0,["catalog234Executed"]=false};
    }
    private sealed class Model
    {
        public CK01GeneratedDataCatalog Catalog;
        public readonly Dictionary<UnityEngine.Object,string> Ids=new Dictionary<UnityEngine.Object,string>();
        public readonly Dictionary<string,Sprite> Sprites=new Dictionary<string,Sprite>(StringComparer.Ordinal);
        public readonly Dictionary<string,ScriptableObject> Assets=new Dictionary<string,ScriptableObject>(StringComparer.Ordinal);
    }
    private static string Reference(JToken raw)
    {if(raw==null||raw.Type==JTokenType.Null||raw["fileID"]==null||(string)raw["fileID"]=="0")return null;return CatalogBlobSerializer.Key((string)raw["guid"]+":"+(string)raw["fileID"]);}
    private static object RawValue(JToken raw,Type type,Model model)
    {
        if(type==typeof(string))return raw==null||raw.Type==JTokenType.Null?null:(string)raw;
        if(raw==null||raw.Type==JTokenType.Null)return null;
        if(type==typeof(Sprite)){var key=Reference(raw);if(key==null)return null;Sprite value;if(!model.Sprites.TryGetValue(key,out value)){value=new Sprite{name=key};model.Sprites.Add(key,value);model.Ids.Add(value,key);}return value;}
        if(typeof(ScriptableObject).IsAssignableFrom(type)){var key=Reference(raw);return key==null?null:model.Assets[key];}
        if(type==typeof(bool)){var s=(string)raw;if(s=="0")return false;if(s=="1")return true;return bool.Parse(s);}
        if(type.IsEnum)return Enum.ToObject(type,int.Parse((string)raw,CultureInfo.InvariantCulture));
        if(type.IsPrimitive)return Convert.ChangeType((string)raw,type,CultureInfo.InvariantCulture);
        if(type.IsGenericType&&type.GetGenericTypeDefinition()==typeof(List<>))
        {
            var list=(IList)Activator.CreateInstance(type);var element=type.GetGenericArguments()[0];
            if(raw.Type==JTokenType.String&&element.IsEnum){var hex=(string)raw;if(hex.Length%8!=0)throw new FormatException("Bad Unity packed enum list");for(var i=0;i<hex.Length;i+=8){var bytes=new byte[4];for(var k=0;k<4;k++)bytes[k]=byte.Parse(hex.Substring(i+k*2,2),NumberStyles.HexNumber);list.Add(Enum.ToObject(element,BitConverter.ToInt32(bytes,0)));}return list;}
            foreach(var item in (JArray)raw)list.Add(RawValue(item,element,model));return list;
        }
        if(type==typeof(LocTableEntry)){var item=new LocTableEntry();foreach(var field in CatalogBlobSerializer.Fields(type))field.SetValue(item,RawValue(raw[field.Name],field.FieldType,model));return item;}
        throw new FormatException("Unknown fixture field type "+type);
    }
    private static Model Load(JObject snapshot)
    {
        var model=new Model();foreach(JObject row in (JArray)snapshot["assets"]){var type=CatalogBlobSerializer.AssetTypes.Single(t=>t.FullName==(string)row["type"]);var item=ScriptableObject.CreateInstance(type);item.name=(string)row["name"];model.Assets.Add((string)row["key"],item);model.Ids.Add(item,(string)row["key"]);}
        foreach(JObject row in (JArray)snapshot["assets"]){var item=model.Assets[(string)row["key"]];var fields=(JObject)row["fields"];var expected=CatalogBlobSerializer.Fields(item.GetType());Check(fields.Properties().Select(x=>x.Name).OrderBy(x=>x).SequenceEqual(expected.Select(x=>x.Name).OrderBy(x=>x)),"actual serialized fields complete: "+(string)row["key"]);foreach(var field in expected)field.SetValue(item,RawValue(fields[field.Name],field.FieldType,model));}
        model.Catalog=new CK01GeneratedDataCatalog();model.Catalog.name=(string)snapshot["catalog"]["name"];model.Ids.Add(model.Catalog,(string)snapshot["catalog"]["key"]);
        var tables=new List<CK01GeneratedDataTable>();foreach(JObject table in (JArray)snapshot["catalog"]["fields"]["tables"]){var rows=new List<CK01GeneratedDataRowReference>();foreach(JObject row in (JArray)table["rows"])rows.Add(new CK01GeneratedDataRowReference((string)row["stableId"],model.Assets[Reference(row["asset"])]));var aggregate=Reference(table["aggregateAsset"]);tables.Add(new CK01GeneratedDataTable((string)table["tableName"],aggregate==null?null:model.Assets[aggregate],rows));}model.Catalog.ReplaceTables(tables);return model;
    }
    private static Model IdentifyDecoded(CatalogDecodedData decoded,JObject graph,Model original)
    {
        var model=new Model{Catalog=new CK01GeneratedDataCatalog()};model.Ids.Add(model.Catalog,(string)graph["catalog"]);model.Catalog.ReplaceTables(decoded.Tables);
        for(var i=0;i<decoded.Tables.Count;i++){var table=decoded.Tables[i];var node=(JObject)graph["tables"][i];if(table.AggregateAsset!=null)model.Ids[table.AggregateAsset]=(string)node["aggregate"];for(var j=0;j<table.Rows.Count;j++)model.Ids[table.Rows[j].Asset]=(string)node["rows"][j]["asset"];}
        foreach(var pair in decoded.Assets){model.Assets[pair.Key]=pair.Value;model.Ids[pair.Value]=pair.Key;}
        foreach(var pair in original.Sprites){model.Sprites.Add(pair.Key,pair.Value);model.Ids[pair.Value]=pair.Key;}return model;
    }
    private static void TestEmptySpriteSemantics(Model model)
    {
        // Synthetic Unity lifetime/PPtr behavior only; the native AI312 observation is separate evidence.
        var item=model.Assets.Values.OfType<CK01ItemData>().First(x=>x.icon_sprite!=null);
        var original=item.icon_sprite;var phantom=new Sprite { ProofNativeAlive=false };
        var pptr=new Dictionary<string,int> { ["icon_sprite"]=0 };
        UnityEditor.SerializedObject.ProofPptrIds[item]=pptr;
        var phantomIdentityCalls=0;
        Func<UnityEngine.Object,string> identity=x=>{
            if(ReferenceEquals(x,phantom)){phantomIdentityCalls++;throw new InvalidOperationException("Unstable Sprite identity");}
            string key;if(!model.Ids.TryGetValue(x,out key))throw new InvalidOperationException("Unknown nonempty Sprite identity");
            return key;
        };
        Func<ScriptableObject,string,Sprite,bool> provedEmpty=(owner,path,sprite)=>{
            if(!ReferenceEquals(owner,item)||path!="icon_sprite"||!ReferenceEquals(sprite,phantom))return false;
            using(var serialized=new UnityEditor.SerializedObject(owner)){
                var property=serialized.FindProperty(path);
                return property!=null&&property.propertyType==UnityEditor.SerializedPropertyType.ObjectReference&&
                    property.objectReferenceInstanceIDValue==0;
            }
        };
        Func<JObject,JToken> field=graph=>((JArray)graph["assets"]).Single(x=>(string)x["key"]==model.Ids[item])["fields"]["icon_sprite"];
        try
        {
            item.icon_sprite=null;
            var managed=CatalogBlobSerializer.Capture(model.Catalog,identity,model.Assets.Values,provedEmpty);
            Check(field(managed).Type==JTokenType.Null&&phantomIdentityCalls==0,"managed null Sprite serializes null without identity");
            item.icon_sprite=phantom;
            Check(!ReferenceEquals(phantom,null)&&phantom==null,"synthetic managed nonnull Unity fake-null is distinct");
            var empty=CatalogBlobSerializer.Capture(model.Catalog,identity,model.Assets.Values,provedEmpty);
            Check(field(empty).Type==JTokenType.Null&&phantomIdentityCalls==0,"proved empty PPtr serializes null without identity");
            pptr["icon_sprite"]=42;
            Reject(()=>CatalogBlobSerializer.Capture(model.Catalog,identity,model.Assets.Values,provedEmpty),"nonzero PPtr fake-null still rejects identity");
            Check(phantomIdentityCalls==1,"nonzero PPtr reached identity guard once");
            UnityEditor.SerializedObject.ProofPptrIds.Remove(item);
            Reject(()=>CatalogBlobSerializer.Capture(model.Catalog,identity,model.Assets.Values,provedEmpty),"unproven fake-null still rejects identity");
            Check(phantomIdentityCalls==2,"missing PPtr proof reached identity guard once");
            item.icon_sprite=new Sprite();
            Reject(()=>CatalogBlobSerializer.Capture(model.Catalog,identity,model.Assets.Values,provedEmpty),"unknown live Sprite identity still rejects");
            item.icon_sprite=original;
            var valid=CatalogBlobSerializer.Capture(model.Catalog,identity,model.Assets.Values,provedEmpty);
            Check((string)field(valid)["sprite"]==model.Ids[original],"existing nonempty Sprite key and closure preserved");
            Check(((JArray)valid["sprites"]).Any(x=>(string)x==model.Ids[original]),"existing live Sprite remains registered");
        }
        finally { item.icon_sprite=original; UnityEditor.SerializedObject.ProofPptrIds.Remove(item); }
    }
    public static int Main(string[] args)
    {
        try
        {
            var result=Run(args[0],args[1]);
            if(args.Length>2){CatalogBlobSerializer.Authenticate(File.ReadAllBytes(args[2]),(string)result["fingerprint"],new CatalogBlobCipher(new ObfuscatedStaticKeyProvider()));result["hostPacketAccepted"]=true;}
            Console.WriteLine("CATALOG_RESULT="+result.ToString(Newtonsoft.Json.Formatting.None));return 0;
        }
        catch(Exception e){Console.Error.WriteLine(e);return 1;}
    }
    public static JObject Run(string sourceSnapshot,string output)
    {
        Cases=new List<string>();var snapshot=SaveEnvelope.ParseCanonicalObject(File.ReadAllText(sourceSnapshot));var model=Load(snapshot);TestEmptySpriteSemantics(model);var graph=CatalogBlobSerializer.Capture(model.Catalog,x=>model.Ids[x],model.Assets.Values);var fingerprint=CatalogBlobSerializer.Fingerprint(graph);var cipher=new CatalogBlobCipher(new ObfuscatedStaticKeyProvider());
        Check(model.Assets.Count==169&&((JArray)snapshot["nativeAssets"]).Count==1,"actual169 data SO+catalog=170 exclusions; native sprite separate");
        Check(model.Catalog.Tables.Count==14&&model.Catalog.SourceKind=="ScriptableObject","SO source retains actual14 tables and default read-only API");
        Check(!model.Catalog.ReplaceTables(model.Catalog.Tables),"SO importer idempotence remains unchanged");
        var packet=CatalogBlobSerializer.Seal(graph,cipher);Check(packet[0]=='E'&&packet[1]=='W'&&packet[2]=='S'&&packet[3]=='B'&&packet[4]==2,"catalog uses authenticated EWSB V2, not GCM-labelled fallback");
        var restoredGraph=CatalogBlobSerializer.Authenticate(packet,fingerprint,cipher);Check(SaveEnvelope.SerializeCanonicalObject(restoredGraph)==SaveEnvelope.SerializeCanonicalObject(graph),"authenticated full graph exact canonical readback");
        using(var decoded=CatalogBlobSerializer.Open(packet,fingerprint,key=>model.Sprites[key],cipher))
        {
            var decodedModel=IdentifyDecoded(decoded,graph,model);var recaptured=CatalogBlobSerializer.Capture(decodedModel.Catalog,x=>decodedModel.Ids[x],decodedModel.Assets.Values);Check(SaveEnvelope.SerializeCanonicalObject(recaptured)==SaveEnvelope.SerializeCanonicalObject(graph),"all actual fields/types/lists/aggregate/row GUID and Sprite relationships roundtrip");
            var reads=0;var blob=new EncryptedBlobSource(()=>{reads++;return packet;},fingerprint,key=>model.Sprites[key],cipher);var catalog=new CK01GeneratedDataCatalog();catalog.InstallSourceForValidation(blob);
            Check(catalog.SourceKind=="EncryptedBlob"&&catalog.Tables.Count==14&&reads==1,"one lazy encrypted source, one read, no dual cache/fallback");
            var left=model.Catalog.EnumerateRecipesOrdered();var right=catalog.EnumerateRecipesOrdered();Check(left.Count==3&&right.Count==3,"all three actual recipes available through unchanged API");
            for(var i=0;i<left.Count;i++){Check(left[i].recipe_id==right[i].recipe_id&&left[i].name_key==right[i].name_key&&left[i].slots.Count==right[i].slots.Count&&left[i].steps.Count==right[i].steps.Count&&ReferenceEquals(left[i].display_sprite,right[i].display_sprite),"recipe readonly DTO equivalence "+left[i].recipe_id);for(var j=0;j<left[i].steps.Count;j++)Check(left[i].steps[j].step_id==right[i].steps[j].step_id&&left[i].steps[j].display_text_key==right[i].steps[j].display_text_key,"ordered actual step equivalence "+left[i].steps[j].step_id);}
            Check(reads==1,"repeated public API reads use same active source");Reject(()=>catalog.InstallSourceForValidation(new ScriptableObjectSource(()=>new List<CK01GeneratedDataTable>())) ,"second source cannot replace active encrypted source");Reject(()=>catalog.ReplaceTables(new List<CK01GeneratedDataTable>()),"importer cannot overwrite active blob source");blob.Dispose();Reject(()=>{var unused=blob.Tables;},"disposed blob source releases objects and refuses reuse");
        }
        var tampered=(byte[])packet.Clone();tampered[tampered.Length-1]^=1;Reject(()=>CatalogBlobSerializer.Authenticate(tampered,fingerprint,cipher),"tampered ciphertext rejects before any JSON/data exposure");tampered=(byte[])packet.Clone();tampered[4]=1;Reject(()=>CatalogBlobSerializer.Authenticate(tampered,fingerprint,cipher),"unknown packet version rejects");Reject(()=>CatalogBlobSerializer.Authenticate(packet,new string('0',64),cipher),"code baseline fingerprint mismatch rejects without SO fallback");Reject(()=>CatalogBlobSerializer.Authenticate(packet,"UNPREPARED",cipher),"uncompiled fingerprint source cannot start release");
        Reject(()=>new AesCbcHmacCipher(new ObfuscatedStaticKeyProvider()).Decrypt(EncryptedBlob.Parse(packet)),"catalog key domain differs from original generic V2 encryption domain");
        var broken=(JObject)graph.DeepClone();broken["assets"][0]["type"]="System.Object";Reject(()=>CatalogBlobSerializer.Rehydrate(broken,key=>model.Sprites[key]),"unknown injected type rejects before object creation");
        if(model.Sprites.Count!=0)Reject(()=>CatalogBlobSerializer.Rehydrate(graph,key=>null),"missing sprite registry does not silently lose art references");
        broken=(JObject)graph.DeepClone();broken["tables"][0]["rows"][0]["asset"]="ffffffffffffffffffffffffffffffff:11400000";Reject(()=>CatalogBlobSerializer.Rehydrate(broken,key=>model.Sprites[key]),"dangling actual row relation rejects");
        Check(!CatalogSourcePolicy.RequiresBlob(true,false,true)&&!CatalogSourcePolicy.RequiresBlob(false,true,true),"Editor/Dev preserve SO even when encryption define is supplied");Check(CatalogSourcePolicy.RequiresBlob(false,false,true),"prepared release requires blob");Reject(()=>CatalogSourcePolicy.RequiresBlob(false,false,false),"unprepared release build policy refuses SO fallback");
        var plan=new CatalogBuildPlan(((string)graph["catalog"]).Substring(0,32),((JArray)graph["assets"]).Select(x=>((string)x["key"]).Substring(0,32)));Check(plan.ExcludedCount==170,"build plan covers all actual170 SO GUIDs");var refs=(JArray)snapshot["sceneReferences"];Check(refs.Count>=2,"actual source scenes have catalog direct references");foreach(JObject reference in refs)Check(plan.RemapCatalog((string)reference["guid"]),"actual scene catalog reference remap decision "+(string)reference["scene"]+":"+(int)reference["line"]);
        Reject(()=>plan.RemapCatalog(((string)graph["assets"][0]["key"]).Substring(0,32)),"direct row SO cannot be kept in scene dependency graph");Reject(()=>plan.AssertNoExcluded(new[]{((string)graph["assets"][0]["key"]).Substring(0,32)}),"post-remap/resource/preloaded/report dependency audit rejects remaining SO");plan.AssertNoExcluded(model.Sprites.Keys.Select(x=>x.Substring(0,32)));Check(true,"native Sprite identity may remain in staged dependency graph");
        var original=new[]{"USER_FLAG"};var release=CatalogBuildPlan.Defines(original,false);Check(original.Length==1&&original[0]=="USER_FLAG"&&release.Contains(CatalogBuildPlan.Define)&&!CatalogBuildPlan.Defines(release,true).Contains(CatalogBuildPlan.Define),"single-build define arrays clone input and Dev clears only extra define");
        foreach(var fault in new[]{"prepare","build"}){var cleanup=false;try{CatalogBuildPlan.Execute<int>(()=>{if(fault=="prepare")throw new IOException("fault");},()=>{throw new IOException("fault");},()=>cleanup=true);}catch(IOException){}Check(cleanup,"finally cleanup runs after injected "+fault+" failure");}
        try{CatalogBuildPlan.Execute<int>(()=>{},()=>{throw new IOException("original build error");},()=>{throw new InvalidOperationException("owned cleanup error");});throw new Exception("dual failure did not throw");}
        catch(AggregateException errors){Check(errors.InnerExceptions.Count==2&&errors.InnerExceptions[0] is IOException&&errors.InnerExceptions[0].Message=="original build error"&&errors.InnerExceptions[1] is InvalidOperationException&&errors.InnerExceptions[1].Message=="owned cleanup error","original Build and cleanup diagnostics remain independently visible");}
        try{CatalogBuildPlan.Execute<int>(()=>{},()=>42,()=>{throw new InvalidOperationException("cleanup only");});throw new Exception("cleanup failure did not throw");}
        catch(InvalidOperationException error){Check(error.Message=="cleanup only","cleanup-only failure is not promoted to success");}
        var ownedRoot="Assets/__CatalogBlobBuild_"+new string('a',32);CatalogBuildPlan.ValidateOwnedPath(ownedRoot+"/RuntimeCatalog.asset",ownedRoot);
        Reject(()=>CatalogBuildPlan.ValidateOwnedPath(ownedRoot+"/../../Scripts/user.cs",ownedRoot),"recovery rejects traversal before cleanup");
        Reject(()=>CatalogBuildPlan.ValidateOwnedPath("C:/outside.asset",ownedRoot),"recovery rejects absolute path");
        Reject(()=>CatalogBuildPlan.ValidateOwnedPath("Assets/Generated/DataTables/user.asset",ownedRoot),"recovery cannot target generated source assets");
        var numeric=SaveEnvelope.ParseCanonicalObject("{\"tiny\":1e-30,\"large\":1e100,\"integer\":9007199254740993}");var numericText=SaveEnvelope.SerializeCanonicalObject(numeric);Check(SaveEnvelope.SerializeCanonicalObject(SaveEnvelope.ParseCanonicalObject(numericText))==numericText,"shared AI277 exact-number bridge keeps tiny/large/integer tokens stable");
        Directory.CreateDirectory(output);var approved=Path.GetFullPath(output);var export=Path.Combine(approved,"staging");var actual=CatalogBlobStaging.Export(graph,export,approved);Check(actual==fingerprint&&File.Exists(Path.Combine(export,"catalog.bin"))&&File.ReadAllText(Path.Combine(export,"CatalogBuildFingerprint.g.cs")).Contains(fingerprint),"explicit DATA staging exports blob/readback/fingerprint source before compile");Reject(()=>CatalogBlobStaging.Export(graph,export,approved),"staged export refuses overwrite of prior or unknown output");
        foreach(var name in new[]{"catalog.graph.json","catalog.bin","CatalogBuildFingerprint.g.cs","export.json"}){var failed=Path.Combine(approved,"fault-"+name.Replace('.','_'));Reject(()=>CatalogBlobStaging.Export(graph,failed,approved,step=>{if(step==name)throw new IOException("controlled export fault");}),"staged export fault propagates at "+name);Check(!Directory.Exists(failed),"staged export removes only owned new files after "+name);}
        Reject(()=>CatalogBlobStaging.Export(new JObject(),Path.Combine(approved,"bad-graph"),approved),"DATA opt-in validates graph schema before output");
        Reject(()=>CatalogBlobStaging.Export(graph,Path.GetFullPath(Path.Combine(approved,"../outside")),approved),"staging root escape refused");
        return new JObject{["success"]=true,["assertionCount"]=Cases.Count,["actualDataSOs"]=model.Assets.Count,["excludedSOs"]=plan.ExcludedCount,["tables"]=model.Catalog.Tables.Count,["spriteKeys"]=model.Sprites.Count,["sceneReferenceChecks"]=refs.Count,["fingerprint"]=fingerprint,["packetBytes"]=packet.Length,["cases"]=new JArray(Cases),["nativeBuildVerified"]=false,["realDataWrites"]=0,["staging"]=export};
    }
}
