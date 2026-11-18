using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json.Linq;
using EatWhat.Tools.Security;
using EatWhat.Tools.Persistence;
using EatWhat.Narrative.Unity;
using UnityEngine;

public static class NarrativeCatalogBlobTests
{
    private sealed class FixtureKey : IKeyProvider { public byte[] GetKey(){return Enumerable.Range(1,32).Select(x=>(byte)x).ToArray();} }
    static int checks;
    static void Check(bool ok,string name){checks++;if(!ok)throw new Exception("Narrative Blob: "+name);}
    static void Reject(Action work,string name){bool failed=false;try{work();}catch(FormatException){failed=true;}Check(failed,name);}
    static string Canonical(JObject value){return SaveEnvelope.SerializeCanonicalObject(value);}
    static ScriptableObject Row(Type type)
    {
        var row=ScriptableObject.CreateInstance(type);row.name="fixture_"+type.Name;
        foreach(var f in CatalogBlobSerializer.Fields(type))
        {
            if(f.FieldType==typeof(string))f.SetValue(row,"fixture_"+f.Name);
            else if(f.FieldType==typeof(bool))f.SetValue(row,true);
            else if(f.FieldType==typeof(CK01OptionalFloat))f.SetValue(row,new CK01OptionalFloat(1.25f));
            else if(f.FieldType==typeof(CK01OptionalInt))f.SetValue(row,new CK01OptionalInt(3));
            else if(f.FieldType==typeof(CK01OptionalBool))f.SetValue(row,new CK01OptionalBool(false));
            else if(f.FieldType==typeof(List<string>))f.SetValue(row,new List<string>{"first","second"});
        }
        return row;
    }
    static int TestEmptyPrefabSemantics()
    {
        int first=checks,proofCalls=0,emptyIdentityCalls=0;
        var catalog=new CK01GeneratedDataCatalog();var visual=new GameObject {name="BoundVisual"};
        var ghosts=new[]{new GameObject {ProofNativeAlive=false},new GameObject {ProofNativeAlive=false}};
        var rows=new[]{new CK01CharacterData {character_id="chr_bound_a",street_visual_prefab="BoundVisual",street_visual_prefab_asset=visual},
            new CK01CharacterData {character_id="chr_bound_b",street_visual_prefab="BoundVisual",street_visual_prefab_asset=visual},
            new CK01CharacterData {character_id="chr_empty_a",street_visual_prefab=""},new CK01CharacterData {character_id="chr_empty_b",street_visual_prefab=""}};
        var ids=new Dictionary<UnityEngine.Object,string>{{catalog,new string('a',32)+":11400000"},{visual,new string('b',32)+":100100000"}};
        for(int i=0;i<rows.Length;i++)ids.Add(rows[i],(i+1).ToString("x32")+":11400000");
        catalog.ReplaceTables(new[]{new CK01GeneratedDataTable("Characters",null,rows.Select(x=>new CK01GeneratedDataRowReference(x.character_id,x)))});
        Func<UnityEngine.Object,string> identity=value=>{
            if(ghosts.Any(x=>ReferenceEquals(x,value)))emptyIdentityCalls++;
            string key;if(!ids.TryGetValue(value,out key))throw new FormatException("Unstable reference identity.");return key;
        };
        // External serialized-PPtr proof input; the actual Editor source/disk proof is reviewed/compiled separately.
        Func<ScriptableObject,string,GameObject,bool> proof=(owner,path,value)=>{
            proofCalls++;
            Check(path=="street_visual_prefab_asset"&&(ReferenceEquals(owner,rows[2])||ReferenceEquals(owner,rows[3])),"empty Prefab proof receives exact owner/field");
            Check(ReferenceEquals(value,ReferenceEquals(owner,rows[2])?ghosts[0]:ghosts[1]),"empty proof receives original fake-null handle");
            using(var serialized=new UnityEditor.SerializedObject(owner)){
                var property=serialized.FindProperty(path);
                return property!=null&&property.propertyType==UnityEditor.SerializedPropertyType.ObjectReference&&property.objectReferenceInstanceIDValue==0;
            }
        };
        Func<JObject,int,JToken> field=(graph,index)=>((JArray)graph["assets"]).Single(x=>(string)x["key"]==ids[rows[index]])["fields"]["street_visual_prefab_asset"];
        try
        {
            var managed=CatalogBlobSerializer.Capture(catalog,identity,rows,null,proof);
            Check(field(managed,2).Type==JTokenType.Null&&field(managed,3).Type==JTokenType.Null&&proofCalls==0,"CLR null Prefabs keep null without proof/identity");
            for(int i=0;i<ghosts.Length;i++){
                rows[i+2].street_visual_prefab_asset=ghosts[i];
                UnityEditor.SerializedObject.ProofPptrIds[rows[i+2]]=new Dictionary<string,int>{{"street_visual_prefab_asset",0}};
            }
            Check(!ReferenceEquals(ghosts[0],null)&&ghosts[0]==null,"managed nonnull Unity-null Prefab representation exercised");
            var graph=CatalogBlobSerializer.Capture(catalog,identity,rows,null,proof);
            Check(proofCalls==2&&emptyIdentityCalls==0,"only two fake-empty fields call proof and never identity");
            Check(Canonical(graph)==Canonical(managed),"proved empty Prefabs equal CLR-null canonical graph");
            Check(CatalogBlobSerializer.Fingerprint(graph)==CatalogBlobSerializer.Fingerprint(managed),"empty representation preserves fingerprint semantics");
            Check(((JArray)graph["prefabs"]).Count==1&&(string)field(graph,0)["prefab"]==ids[visual]&&(string)field(graph,1)["prefab"]==ids[visual],"two live fields keep one shared Prefab key");
            var cipher=new CatalogBlobCipher(new FixtureKey());var packet=CatalogBlobSerializer.Seal(graph,cipher);
            using(var decoded=CatalogBlobSerializer.Open(packet,CatalogBlobSerializer.Fingerprint(graph),key=>null,key=>key==ids[visual]?visual:null,cipher)){
                var restored=decoded.Assets.Values.OfType<CK01CharacterData>().ToDictionary(x=>x.character_id);
                Check(ReferenceEquals(restored["chr_bound_a"].street_visual_prefab_asset,visual)&&ReferenceEquals(restored["chr_bound_b"].street_visual_prefab_asset,visual),"authenticated decode restores both bound fields to original shared Prefab");
                Check(ReferenceEquals(restored["chr_empty_a"].street_visual_prefab_asset,null)&&ReferenceEquals(restored["chr_empty_b"].street_visual_prefab_asset,null),"authenticated decode restores two empty fields as CLR null");
                Check(restored["chr_empty_a"].street_visual_prefab==""&&restored["chr_empty_b"].street_visual_prefab=="","empty source names preserve own appearance fallback");
            }
            Reject(()=>CatalogBlobSerializer.Capture(catalog,identity,rows),"original no-proof API rejects fake-null Prefab");
            Reject(()=>CatalogBlobSerializer.Capture(catalog,identity,rows,(Func<ScriptableObject,string,Sprite,bool>)null),"retained Sprite-proof overload does not infer empty Prefabs");
            Reject(()=>CatalogBlobSerializer.Capture(catalog,identity,rows,null,(owner,path,value)=>false),"denied proof rejects rather than dropping reference");
            UnityEditor.SerializedObject.ProofPptrIds[rows[2]]["street_visual_prefab_asset"]=42;
            Reject(()=>CatalogBlobSerializer.Capture(catalog,identity,rows,null,proof),"nonzero serialized PPtr cannot become null");
            UnityEditor.SerializedObject.ProofPptrIds.Remove(rows[2]);
            Reject(()=>CatalogBlobSerializer.Capture(catalog,identity,rows,null,proof),"missing serialized property proof rejects");
            UnityEditor.SerializedObject.ProofPptrIds[rows[2]]=new Dictionary<string,int>{{"street_visual_prefab_asset",0}};
            Reject(()=>CatalogBlobSerializer.Capture(catalog,identity,rows,null,(owner,path,value)=>{throw new FormatException("proof unavailable");}),"proof exception is propagated");
            rows[2].street_visual_prefab_asset=new GameObject {name="UnregisteredLive"};
            Reject(()=>CatalogBlobSerializer.Capture(catalog,identity,rows,null,(owner,path,value)=>true),"live unregistered Prefab cannot be swallowed by empty-proof callback");
            rows[2].street_visual_prefab_asset=ghosts[0];
            Reject(()=>CatalogBlobSerializer.Open(packet,CatalogBlobSerializer.Fingerprint(graph),key=>null,key=>null,cipher),"registered live Prefab resolver failure still rejects");
            Check(emptyIdentityCalls==0,"fake-null references never enter stable identity on rejected proof");
        }
        finally {foreach(var row in rows)UnityEditor.SerializedObject.ProofPptrIds.Remove(row);}
        return checks-first;
    }
    public static string Run(string root)
    {
        checks=0;var old=JObject.Parse(File.ReadAllText(Path.Combine(root,"DevTools/DataTables/Fixtures/NarrativeBlobV251/legacy14-export.json")));
        var sprites=((JArray)old["sprites"]).ToDictionary(x=>(string)x,x=>new Sprite {name=(string)x});
        var ids=new Dictionary<UnityEngine.Object,string>();foreach(var pair in sprites)ids.Add(pair.Value,pair.Key);
        var prefab=new GameObject {name="SharedVisual"};ids.Add(prefab,"bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb:100100000");
        int prefabDestroyed=0,dataDestroyed=0;
        UnityEngine.Object.ProofDestroy=item=>{if(ReferenceEquals(item,prefab))prefabDestroyed++;if(item is ScriptableObject)dataDestroyed++;};
        try
        {
            using(var legacy=CatalogBlobSerializer.Rehydrate(old,key=>sprites[key]))
            {
                foreach(var pair in legacy.Assets)ids.Add(pair.Value,pair.Key);
                var all=new List<ScriptableObject>(legacy.Assets.Values);var tables=new List<CK01GeneratedDataTable>(legacy.Tables);
                var character=(CK01CharacterData)Row(typeof(CK01CharacterData));character.character_id="chr_blob";character.street_visual_prefab="SharedVisual";character.street_visual_prefab_asset=prefab;
                character.portrait_offset_x=new CK01OptionalFloat {hasValue=false,value=12.5f};character.portrait_offset_y=new CK01OptionalFloat(0);character.portrait_scale=new CK01OptionalFloat(1);
                var dialogue=(CK01DialogueData)Row(typeof(CK01DialogueData));dialogue.dialogue_id="dlg_blob";dialogue.tier="ambient";dialogue.first_line="ln_blob";dialogue.next_dialogue="";dialogue.on_end="wait(0)";dialogue.overrides="";
                foreach(var f in typeof(CK01DialogueData).GetFields())if(f.FieldType==typeof(string)&&f.Name.EndsWith("_profile"))f.SetValue(dialogue,"default");
                var line=(CK01DialogueLineData)Row(typeof(CK01DialogueLineData));line.line_id="ln_blob";line.dialogue_id="dlg_blob";line.next_line="";line.speaker="chr_blob";line.commands="wait(0)";
                var cg=(CK01CGData)Row(typeof(CK01CGData));cg.cg_id="cg_blob";cg.transition_mode="cross_fade";cg.ken_burns_curve="linear";cg.keep_name=new CK01OptionalBool {hasValue=false,value=true};cg.keep_box=new CK01OptionalBool(false);
                var scene=(CK01SceneData)Row(typeof(CK01SceneData));scene.scene_id="scn_blob";scene.scene_type="street";scene.default_state="morning";
                var state=(CK01SceneStateData)Row(typeof(CK01SceneStateData));state.scene_id="scn_blob";state.state_id="morning";
                var placement=(CK01SceneStatePlacementData)Row(typeof(CK01SceneStatePlacementData));placement.scene_id="scn_blob";placement.state_id="morning";placement.kind="actor";placement.target="chr_blob";placement.facing="right";placement.marker="none";
                var pool=(CK01AmbientPoolData)Row(typeof(CK01AmbientPoolData));pool.pool_id="amb_blob";pool.dialogue_ids=new List<string>{"dlg_blob","dlg_blob"};
                dialogue.cast_01_order=new CK01OptionalInt {hasValue=false,value=-4};dialogue.cast_02_order=new CK01OptionalInt(0);
                var rows=new ScriptableObject[]{character,dialogue,line,cg,scene,state,placement,pool};
                var names=new[]{"Characters","Dialogues","DialogueLines","CGs","Scenes","SceneStates","SceneStatePlacements","AmbientPools"};
                var keys=new[]{"chr_blob","dlg_blob","ln_blob","cg_blob","scn_blob","state_fixture","placement_fixture","amb_blob"};
                int sequence=1;
                Action<ScriptableObject> register=row=>{ids.Add(row,(sequence++).ToString("x32")+":11400000");all.Add(row);};
                for(int i=0;i<rows.Length;i++){register(rows[i]);tables.Add(new CK01GeneratedDataTable(names[i],null,new[]{new CK01GeneratedDataRowReference(keys[i],rows[i])}));}
                var extra=new[]{new CK01CharacterData {character_id="chr_second",street_visual_prefab="SharedVisual",street_visual_prefab_asset=prefab},new CK01CharacterData {character_id="chr_missing",street_visual_prefab="NotImported"},new CK01CharacterData {character_id="chr_empty"}};
                foreach(var row in extra)register(row);
                var characterRows=new List<CK01GeneratedDataRowReference>{new CK01GeneratedDataRowReference("chr_blob",character)};characterRows.AddRange(extra.Select(x=>new CK01GeneratedDataRowReference(x.character_id,x)));
                tables[14]=new CK01GeneratedDataTable("Characters",null,characterRows);
                var catalog=new CK01GeneratedDataCatalog();ids.Add(catalog,(string)old["catalog"]);catalog.ReplaceTables(tables);
                var graph=CatalogBlobSerializer.Capture(catalog,x=>ids[x],all);
                Check(((JArray)graph["tables"]).Count==22,"real22 graph with eight nonempty new tables");
                foreach(string name in names)Check(((JArray)graph["tables"]).Single(x=>(string)x["name"]==name)["rows"].Count()>0,"nonempty "+name);
                foreach(var node in (JArray)old["assets"])Check(JToken.DeepEquals(node,((JArray)graph["assets"]).Single(x=>(string)x["key"]==(string)node["key"])),"legacy full fields/relations "+(string)node["key"]);
                foreach(var node in (JArray)old["tables"])Check(JToken.DeepEquals(node,((JArray)graph["tables"]).Single(x=>(string)x["name"]==(string)node["name"])),"legacy table rows "+(string)node["name"]);
                Check(JToken.DeepEquals(old["sprites"],graph["sprites"]),"all old Sprite identities unchanged");
                Check(((JArray)graph["prefabs"]).Count==1,"shared Prefab key is registered once");
                CatalogBlobSerializer.ValidateMetadata(graph);
                var cipher=new CatalogBlobCipher(new FixtureKey());var fingerprint=CatalogBlobSerializer.Fingerprint(graph);var packet=CatalogBlobSerializer.Seal(graph,cipher);
                Check(Canonical(CatalogBlobSerializer.Authenticate(packet,fingerprint,cipher))==Canonical(graph),"actual authenticated byte packet readback");
                Reject(()=>CatalogBlobSerializer.Open(packet,fingerprint,key=>sprites[key],key=>null,cipher),"missing registered Prefab refuses silent loss");
                Reject(()=>CatalogBlobSerializer.Open(packet,fingerprint,key=>sprites[key],cipher),"old overload cannot silently drop a new Prefab");
                using(var decoded=CatalogBlobSerializer.Open(packet,fingerprint,key=>sprites[key],key=>prefab,cipher))
                {
                    var restored=new CK01GeneratedDataCatalog();restored.ReplaceTables(decoded.Tables);var restoredIds=new Dictionary<UnityEngine.Object,string>();restoredIds.Add(restored,(string)old["catalog"]);
                    foreach(var pair in decoded.Assets)restoredIds.Add(pair.Value,pair.Key);foreach(var pair in sprites)restoredIds.Add(pair.Value,pair.Key);restoredIds.Add(prefab,ids[prefab]);
                    var again=CatalogBlobSerializer.Capture(restored,x=>restoredIds[x],decoded.Assets.Values);
                    Check(Canonical(again)==Canonical(graph),"every row/field/type/list/null/Optional payload and reference roundtrips exactly");
                    CK01CharacterData c;string issue;restored.TryGetCharacter("chr_blob",out c,out issue);
                    Check(!c.portrait_offset_x.hasValue&&c.portrait_offset_x.value==12.5f&&c.portrait_offset_y.hasValue&&c.portrait_offset_y.value==0,"unspecified payload and explicit zero remain distinct");
                    CK01CGData g;restored.TryGetCG("cg_blob",out g,out issue);Check(g.keep_box.hasValue&&!g.keep_box.value&&!g.keep_name.hasValue&&g.keep_name.value,"explicit false and unspecified bool payload preserved");
                }
                Check(prefabDestroyed==0&&dataDestroyed>0,"DecodedData releases own rows but never shared persistent Prefab");
                int reads=0;var source=new EncryptedBlobSource(()=>{reads++;return packet;},fingerprint,key=>sprites[key],key=>prefab,cipher);var runtimeCatalog=new CK01GeneratedDataCatalog();runtimeCatalog.InstallSourceForValidation(source);
                var runtime=new NarrativeRuntimeEntry();runtime.Configure(new DialoguePresentationConfig(),runtimeCatalog);GameObject found;string diagnostic;
                Check(runtime.TryGetCharacterVisualPrefab("chr_blob",out found,out diagnostic)&&ReferenceEquals(found,prefab),"actual encrypted source -> RuntimeEntry/Adapter -> original Prefab by base id");
                Check(runtime.TryGetCharacterVisualPrefab("chr_second",out found,out diagnostic)&&ReferenceEquals(found,prefab)&&reads==1,"shared reference and one lazy source read");
                Check(!runtime.TryGetCharacterVisualPrefab("chr_missing",out found,out diagnostic)&&diagnostic!=null,"specified/import-missing null remains explicit");
                Check(!runtime.TryGetCharacterVisualPrefab("chr_empty",out found,out diagnostic)&&diagnostic==null,"empty source remains no override");
                var carrier=new CK01GeneratedDataCatalog();carrier.ConfigureBuildReferences(sprites.Select(x=>new CatalogSpriteReference(x.Key,x.Value)),new[]{new CatalogPrefabReference(ids[prefab],prefab)});
                var registered=(List<CatalogPrefabReference>)typeof(CK01GeneratedDataCatalog).GetField("releasePrefabs",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(carrier);
                Check(registered.Count==1&&ReferenceEquals(registered[0].prefab,prefab)&&carrier.Tables.Count==0,"real build-only carrier retains Prefab reference and removes authoring tables");
                var bad=(JObject)graph.DeepClone();((JArray)bad["prefabs"]).Add(bad["prefabs"][0].DeepClone());Reject(()=>CatalogBlobSerializer.Rehydrate(bad,key=>sprites[key],key=>prefab),"duplicate Prefab registry rejected");
                source.Dispose();Check(prefabDestroyed==0,"encrypted source disposal also preserves shared Prefab");
                using(var empty=CatalogBlobSerializer.Rehydrate(CatalogBlobSerializer.Capture(new CK01GeneratedDataCatalog(),x=>new string('c',32)+":11400000"),key=>null,key=>null))Check(empty.Tables.Count==0,"empty catalog/registries roundtrip");
                using(var oldAgain=CatalogBlobSerializer.Open(CatalogBlobSerializer.Seal(old,cipher),CatalogBlobSerializer.Fingerprint(old),key=>sprites[key],cipher))Check(oldAgain.Tables.Count==14,"old2.4.1 payload remains readable through original API");
            }
            int originalChecks=checks;int emptyPrefabChecks=TestEmptyPrefabSemantics();
            return "{\"passed\":true,\"checks\":"+checks+",\"originalChecks\":"+originalChecks+",\"emptyPrefabChecks\":"+emptyPrefabChecks+",\"tables\":22,\"legacyAssetsCompared\":169,\"legacyTablesCompared\":14,\"actualEncryptedSourceAndRuntime\":true,\"nativeUnity\":false}";
        }
        finally { UnityEngine.Object.ProofDestroy=null; }
    }
}
