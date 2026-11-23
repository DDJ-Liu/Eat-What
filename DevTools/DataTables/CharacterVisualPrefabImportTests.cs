using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using EatWhat.DataTables;
using EatWhat.Narrative.Unity;
using UnityEngine;
using UnityEditor;

// Production row binding + actual adapter/runtime query. Only AssetDatabase and asset handles are external fakes.
public static class CharacterVisualPrefabImportTests
{
    static int checks, searches;
    static void Check(bool value,string message) { checks++;if(!value)throw new Exception("Visual Prefab: "+message); }
    static readonly MethodInfo apply=typeof(DataTableImportService).GetMethod("TryApplyRow",BindingFlags.Static|BindingFlags.NonPublic);
    static readonly string visuals="Assets/Prefabs/LongLoop/NPC/Visuals/";
    static DataTableSchema Schema(string root,string name)
    { return JsonConvert.DeserializeObject<DataTableSchema>(File.ReadAllText(Path.Combine(root,"DataTables/Schemas/Narrative",name+".schema.json"))); }
    static List<string> Apply(ScriptableObject asset,DataTableSchema schema,string visual,out bool changed,string portrait="")
    {
        var headers=new List<string>();var cells=new List<string>();
        foreach(var column in schema.columns)
        {
            headers.Add(column.name);
            cells.Add(column.name=="character_id"?"chr_visual":column.name=="street_visual_prefab"?visual:
                column.name=="portrait_sprite"?portrait:column.name=="cg_id"?"cg_visual":column.name=="cg_sprite"?visual:"");
        }
        var csv=DataTableCsvParser.Parse(string.Join(",",headers)+"\r\n"+string.Join(",",cells)+"\r\n");
        Check(csv.Errors.Count==0,"actual CSV parser accepts synthetic source row");
        var args=new object[]{asset,schema,csv.Table.Rows[0],csv.Table,false,null,null};
        Check((bool)apply.Invoke(null,args)&&args[5]==null,"actual TryApplyRow succeeds without saving assets");
        changed=(bool)args[4];return (List<string>)args[6];
    }
    static void Asset(string path,string guid,UnityEngine.Object value)
    { AssetDatabase.ProofGuids[path]=guid;if(value!=null)AssetDatabase.ProofMain[path]=value; }
    static CK01GeneratedDataCatalog Catalog(params CK01CharacterData[] rows)
    {
        var tables=new List<CK01GeneratedDataTable>();
        foreach(string name in new[]{"Characters","Dialogues","DialogueLines","CGs","Scenes","SceneStates","SceneStatePlacements","AmbientPools"})
        {
            var entries=new List<CK01GeneratedDataRowReference>();
            if(name=="Characters")foreach(var row in rows)entries.Add(new CK01GeneratedDataRowReference(row.character_id,row));
            tables.Add(new CK01GeneratedDataTable(name,null,entries));
        }
        var catalog=new CK01GeneratedDataCatalog();catalog.ReplaceTables(tables);return catalog;
    }
    static int TestPortraitReferences(string root)
    {
        int start=checks;var schema=Schema(root,"Characters");bool changed;
        var temp=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"EatWhatPortraitReference-"+Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(temp);AssetDatabase.ResetProofFixture();
        try
        {
            var a=new Sprite {name="PortraitA"};var b=new Sprite {name="PortraitB"};var texture=new Texture2D(1,1,TextureFormat.RGBA32,false);
            string pa=Path.Combine(temp,"PortraitA.png"),pb=Path.Combine(temp,"PortraitB.png"),pm=Path.Combine(temp,"Multiple.png");
            foreach(var path in new[]{pa,pb,pm}){File.WriteAllText(path,"external texture handle");File.WriteAllText(path+".meta","TextureImporter:\n  spriteMode: "+(path==pm?2:1)+"\n");}
            Asset(pa,"portrait-a",texture);Asset(pb,"portrait-b",texture);Asset(pm,"portrait-m",texture);
            AssetDatabase.ProofAll[pa]=new UnityEngine.Object[]{texture,a};AssetDatabase.ProofAll[pb]=new UnityEngine.Object[]{texture,b};
            AssetDatabase.ProofAll[pm]=new UnityEngine.Object[]{texture,new Sprite {name="Multiple_0"},new Sprite {name="Multiple_1"}};
            var guids=new List<string>{"portrait-a","portrait-b","portrait-m"};int lookups=0;
            AssetDatabase.ProofFindAssets=(filter,folders)=>{lookups++;return guids.ToArray();};
            var row=new CK01CharacterData();var warnings=Apply(row,schema,"",out changed,"PortraitA");
            Check(changed&&warnings.Count==0&&row.portrait_sprite=="PortraitA"&&ReferenceEquals(row.portrait_sprite_asset,a),"Single texture subasset binds exact whole Sprite while retaining basename");
            Apply(row,schema,"",out changed,"PortraitA");Check(!changed,"same Single reference import is idempotent");
            Apply(row,schema,"",out changed,"PortraitB");Check(changed&&ReferenceEquals(row.portrait_sprite_asset,b),"switching portrait replaces stale Sprite");
            var replacement=new Sprite {name="PortraitB"};AssetDatabase.ProofAll[pb]=new UnityEngine.Object[]{texture,replacement};
            Apply(row,schema,"",out changed,"PortraitB");Check(changed&&ReferenceEquals(row.portrait_sprite_asset,replacement),"same basename with changed asset handle updates derived reference");
            int before=lookups;warnings=Apply(row,schema,"",out changed,"");Check(changed&&row.portrait_sprite_asset==null&&warnings.Count==0&&lookups==before,"empty portrait clears stale reference without lookup or warning");
            Apply(row,schema,"",out changed,"");Check(!changed,"empty portrait repeat is idempotent");
            row.portrait_sprite_asset=a;warnings=Apply(row,schema,"",out changed,"Missing");
            Check(changed&&row.portrait_sprite=="Missing"&&row.portrait_sprite_asset==null&&warnings.Count==1,"missing exact basename preserves source string and clears old reference");
            row.portrait_sprite_asset=a;warnings=Apply(row,schema,"",out changed,"Multiple");
            Check(changed&&row.portrait_sprite==""&&row.portrait_sprite_asset==null&&warnings.Count==1&&warnings[0].Contains("导入方式是多张子图"),"Multiple cannot bind either slice and retains original rejection warning");
            warnings=Apply(row,schema,"",out changed,"Multiple");Check(!changed&&warnings.Count==1,"rejected Multiple repeat is idempotent");
            warnings=Apply(row,schema,"",out changed,"portraita");Check(row.portrait_sprite_asset==null&&warnings.Count==1,"portrait filenames remain case exact");
            AssetDatabase.ProofAll[pa]=new UnityEngine.Object[]{texture};row.portrait_sprite_asset=a;
            warnings=Apply(row,schema,"",out changed,"PortraitA");Check(row.portrait_sprite=="PortraitA"&&row.portrait_sprite_asset==null&&warnings.Count==1&&warnings[0].Contains("Single Sprite"),"unloadable/wrong asset type clears reference with explicit warning");
            string duplicate=Path.Combine(temp,"duplicate","PortraitA.png");Directory.CreateDirectory(Path.GetDirectoryName(duplicate));File.WriteAllText(duplicate,"external duplicate");
            Asset(duplicate,"portrait-duplicate",a);guids.Add("portrait-duplicate");
            warnings=Apply(row,schema,"",out changed,"PortraitA");Check(row.portrait_sprite_asset==null&&warnings.Count==1,"duplicate exact files never select first Sprite");
            return checks-start;
        }
        finally
        {
            AssetDatabase.ResetProofFixture();
            string parent=Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
            if(!temp.StartsWith(parent,StringComparison.OrdinalIgnoreCase))throw new Exception("Fixture cleanup escaped temp root.");
            Directory.Delete(temp,true);
        }
    }
    public static string Run(string root)
    {
        checks=searches=0;AssetDatabase.ResetProofFixture();
        try
        {
            var schema=Schema(root,"Characters");var cgSchema=Schema(root,"CGs");
            var prefab=new GameObject {name="ExactVisual"};var row=new CK01CharacterData();bool changed;
            string path=visuals+"ExactVisual.prefab";
            Asset(path,"visual-guid",prefab);
            AssetDatabase.ProofFindAssets=(filter,folders)=>{
                searches++;Check(folders.Length==2&&folders[0]==visuals.TrimEnd('/'),"real importer supplies approved Visuals roots");
                return filter=="t:Prefab"?new[]{"visual-guid"}:new string[0];
            };
            var warnings=Apply(row,schema,"ExactVisual",out changed);
            Check(changed&&warnings.Count==0&&ReferenceEquals(row.street_visual_prefab_asset,prefab),"exact filename binds actual external GameObject handle");
            Check(row.street_visual_prefab=="ExactVisual","source name/case remains string");
            Apply(row,schema,"ExactVisual",out changed);Check(!changed,"same source and reference are idempotent");
            var catalog=Catalog(row);GameObject found;string diagnostic;
            Check(NarrativeDataAdapter.TryGetCharacterVisualPrefab(catalog,"chr_visual",out found,out diagnostic)&&ReferenceEquals(found,prefab)&&diagnostic==null,"actual Unity adapter queries real catalog row by character id");
            var runtime=new NarrativeRuntimeEntry();runtime.Configure(new DialoguePresentationConfig(),catalog);
            Check(runtime.TryGetCharacterVisualPrefab("chr_visual",out found,out diagnostic)&&ReferenceEquals(found,prefab),"actual RuntimeEntry delegates query with real directory availability");
            Check(!runtime.TryGetCharacterVisualPrefab("chr_visual#2",out found,out diagnostic)&&found==null&&diagnostic!=null,"exact base id contract does not guess suffix stripping");
            Check(!runtime.TryGetCharacterVisualPrefab("chr_missing",out found,out diagnostic)&&found==null,"missing character has no fallback object");
            int before=searches;warnings=Apply(row,schema,"",out changed);
            Check(changed&&row.street_visual_prefab==""&&row.street_visual_prefab_asset==null&&warnings.Count==0&&searches==before,"empty source clears stale derived reference without warning/search");
            Check(!runtime.TryGetCharacterVisualPrefab("chr_visual",out found,out diagnostic)&&found==null&&diagnostic==null,"empty column means no table override");
            row.street_visual_prefab_asset=prefab;
            Check(!runtime.TryGetCharacterVisualPrefab("chr_visual",out found,out diagnostic)&&found==null&&diagnostic==null,"empty column wins over an externally stale derived field");
            warnings=Apply(row,schema,"MissingVisual",out changed);
            Check(warnings.Count==1&&row.street_visual_prefab=="MissingVisual"&&row.street_visual_prefab_asset==null,"missing basename warns and clears reference without rejecting row");
            Check(!runtime.TryGetCharacterVisualPrefab("chr_visual",out found,out diagnostic)&&diagnostic.Contains("no bound Prefab"),"missing specified reference differs from empty column at runtime");
            warnings=Apply(row,schema,"exactvisual",out changed);
            Check(warnings.Count==1&&row.street_visual_prefab_asset==null,"wrong case is not a match");
            before=searches;warnings=Apply(row,schema,"folder/ExactVisual",out changed);
            Check(warnings.Count==1&&searches==before&&row.street_visual_prefab=="folder/ExactVisual","invalid basename keeps source string and does not bind");
            Asset(visuals+"Placeholders/ExactVisual.prefab","duplicate-guid",new GameObject());
            AssetDatabase.ProofFindAssets=(filter,folders)=>new[]{"duplicate-guid","visual-guid"};
            warnings=Apply(row,schema,"ExactVisual",out changed);
            Check(warnings.Count==1&&row.street_visual_prefab_asset==null,"duplicate exact candidates do not pick the first/placeholder");
            AssetDatabase.ProofFindAssets=(filter,folders)=>new[]{"visual-guid","visual-guid"};
            warnings=Apply(row,schema,"ExactVisual",out changed);
            Check(warnings.Count==0&&ReferenceEquals(row.street_visual_prefab_asset,prefab),"duplicate same path from nested roots deduplicates");
            AssetDatabase.ProofMain[path]=new Sprite();warnings=Apply(row,schema,"ExactVisual",out changed);
            Check(warnings.Count==1&&warnings[0].Contains("could not be loaded")&&row.street_visual_prefab_asset==null,"wrong main asset type diagnoses and clears reference");
            AssetDatabase.ProofMain.Remove(path);warnings=Apply(row,schema,"ExactVisual",out changed);
            Check(warnings.Count==1&&row.street_visual_prefab_asset==null,"file candidate with unloaded asset is nonblocking warning");
            AssetDatabase.ProofMain[path]=prefab;
            string portraitPath="Assets/Sprites/Narrative/Portraits/PortraitExact.png";
            Asset(portraitPath,"portrait-guid",new Sprite());
            AssetDatabase.ProofFindAssets=(filter,folders)=>filter=="t:Prefab"?new[]{"visual-guid"}:new[]{"portrait-guid"};
            warnings=Apply(row,schema,"ExactVisual",out changed,"PortraitExact");
            Check(warnings.Count==0&&row.portrait_sprite=="PortraitExact"&&ReferenceEquals(row.street_visual_prefab_asset,prefab),"portrait remains nameOnly existence check with original string");
            string cgPath="Assets/Sprites/Narrative/CGs/CgExact.png";Asset(cgPath,"cg-guid",new Sprite());
            AssetDatabase.ProofFindAssets=(filter,folders)=>new[]{"cg-guid"};var cg=new CK01CGData();warnings=Apply(cg,cgSchema,"CgExact",out changed);
            Check(warnings.Count==0&&cg.cg_sprite=="CgExact","CG remains string/existence-only path");
            var pure=NarrativeDataAdapter.FromRow(row);
            Check(pure.StreetVisualPrefab=="ExactVisual","pure Core model still receives only original filename");
            prefab.ProofNativeAlive=false;
            Check(!runtime.TryGetCharacterVisualPrefab("chr_visual",out found,out diagnostic)&&found==null&&diagnostic!=null,"destroyed external Unity reference is unavailable");prefab.ProofNativeAlive=true;
            runtime.Configure(null,catalog);
            Check(!runtime.TryGetCharacterVisualPrefab("chr_visual",out found,out diagnostic)&&diagnostic.Contains("MISSING_CONFIG"),"runtime unavailable keeps original config gate");
            Check(!NarrativeDataAdapter.TryGetCharacterVisualPrefab(null,"chr_visual",out found,out diagnostic)&&diagnostic!=null,"missing catalog does not invent reference");
            int originalChecks=checks,portraitReferenceChecks=TestPortraitReferences(root);
            return "{\"passed\":true,\"checks\":"+checks+",\"originalChecks\":"+originalChecks+",\"portraitReferenceChecks\":"+portraitReferenceChecks+",\"actualImportRowAdapterRuntime\":true,\"externalAssetDatabaseAndHandlesOnly\":true,\"assetPersistence\":false}";
        }
        finally { AssetDatabase.ResetProofFixture(); }
    }
}
