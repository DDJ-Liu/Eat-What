using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using EatWhat.DataTables;
using UnityEngine;

// Executes production validation, row conversion and catalog queries using the
// existing DataTables StaticCompile host. AssetDatabase persistence is excluded.
public static class NarrativeTableFixtureTests
{
    static int checks;
    static void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
    static readonly BindingFlags PrivateStatic = BindingFlags.Static | BindingFlags.NonPublic;
    static void Set(object item, string field, object value) { item.GetType().GetField(field).SetValue(item,value); }
    static string Raw(DataTableSchema s, DataTableCsvTable t, DataTableCsvRow r, string field) { return DataTableValidator.GetEffectiveValue(DataTableValidator.FindColumn(s,field),r,t); }
    static DataTableCsvParseResult Changed(DataTableCsvParseResult source, string field, string value)
    {
        var copy=DataTableCsvParser.Parse(string.Join(",",source.Table.Headers)+"\n");
        foreach(var row in source.Table.Rows) { var r=new DataTableCsvRow(); r.LineNumber=row.LineNumber; r.Values.AddRange(row.Values); copy.Table.Rows.Add(r); }
        copy.Table.Rows[0].Values[copy.Table.GetHeaderIndex(field)]=value; return copy;
    }
    public static string Run(string root)
    {
        checks=0;
        var fixture=Path.Combine(root,"DevTools/Narrative/Fixtures/TablesV25");
        var expected=JObject.Parse(File.ReadAllText(Path.Combine(fixture,"expected-contract.json")));
        var schemas=new Dictionary<string,DataTableSchema>(); var parsed=new Dictionary<string,DataTableCsvParseResult>();
        var loadedType=typeof(DataTableImportService).GetNestedType("LoadedTable",BindingFlags.NonPublic);
        var loaded=(IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(loadedType));
        int columns=0, fields=0, warningCount=0;
        foreach(var table in ((JObject)expected["tables"]).Properties())
        {
            string folder=new[]{"Characters","Dialogues","DialogueLines","CGs"}.Contains(table.Name)?"Narrative":"Street";
            var schema=JsonConvert.DeserializeObject<DataTableSchema>(File.ReadAllText(Path.Combine(root,"DataTables/Schemas",folder,table.Name+".schema.json")));
            var csv=DataTableCsvParser.Parse(File.ReadAllText(Path.Combine(fixture,folder,table.Name+".csv")));
            schemas.Add(table.Name,schema); parsed.Add(table.Name,csv);
            var cols=(JArray)table.Value; Check(cols.Count==schema.columns.Count,table.Name+" column count");
            Check(csv.Table.Headers.SequenceEqual(cols.Select(c=>(string)c["name"])),table.Name+" independent headers");
            for(int i=0;i<cols.Count;i++)
            {
                var e=cols[i];var c=schema.columns[i];columns++;
                Check(c.name==(string)e["name"] && c.csvColumn==c.name && c.type==(string)e["type"] && c.required==(bool)e["required"],table.Name+"."+c.name+" contract");
                Check(!string.IsNullOrWhiteSpace(c.description),table.Name+"."+c.name+" Chinese purpose");
                if(e["enumValues"]!=null) Check(c.enumValues.SequenceEqual(e["enumValues"].Values<string>()),c.name+" enum options");
                Check(string.IsNullOrEmpty(c.defaultValue),c.name+" no invented default");
            }
            var refs=(JObject)expected["references"][table.Name]; Check(refs.Count==schema.references.Count,table.Name+" refs count");
            foreach(var e in refs.Properties()) { var r=schema.references.Single(x=>x.field==e.Name); var parts=((string)e.Value).Split(':'); Check(r.targetSchema==parts[0]+".schema.json"&&r.targetField==parts[1],table.Name+"."+e.Name+" ref"); }
            var record=Activator.CreateInstance(loadedType);Set(record,"SchemaPath","Schemas/"+folder+"/"+table.Name+".schema.json");Set(record,"Schema",schema);Set(record,"ParseResult",csv);Set(record,"TargetType",typeof(CK01CharacterData).Assembly.GetType(schema.targetType));loaded.Add(record);
        }
        Check(columns==105,"105 independent columns");
        var locSchema=new DataTableSchema();locSchema.tableName="Localization";locSchema.columns.Add(new DataTableColumnSchema {name="loc_key",csvColumn="loc_key",type="string"});
        var loc=Activator.CreateInstance(loadedType);Set(loc,"SchemaPath","Schemas/Localization/Localization.schema.json");Set(loc,"Schema",locSchema);Set(loc,"ParseResult",DataTableCsvParser.Parse("loc_key\nloc_name\nloc_text\nloc_scene\n"));loaded.Add(loc);
        var context=(DataTableValidationContext)typeof(DataTableImportService).GetMethod("BuildValidationContext",PrivateStatic).Invoke(null,new object[]{loaded,new DataTableImportService.DataTableImportReport()});
        var tables=new List<CK01GeneratedDataTable>();
        var apply=typeof(DataTableImportService).GetMethod("TryApplyRow",PrivateStatic);
        foreach(var pair in schemas)
        {
            var s=pair.Value;var csv=parsed[pair.Key];var validation=DataTableValidator.Validate(s,csv,context);
            Check(validation.Errors.Count==0,pair.Key+" actual validation: "+string.Join(";",validation.Errors));
            var rows=new List<CK01GeneratedDataRowReference>();
            foreach(var row in csv.Table.Rows)
            {
                var asset=(ScriptableObject)Activator.CreateInstance(typeof(CK01CharacterData).Assembly.GetType(s.targetType));
                var args=new object[]{asset,s,row,csv.Table,false,null,null};
                Check((bool)apply.Invoke(null,args),pair.Key+" actual row apply: "+args[5]); warningCount+=((List<string>)args[6]).Count;
                foreach(var col in s.columns)
                {
                    var field=asset.GetType().GetField(col.name); Check(field!=null,col.name+" declared field");
                    object value=field.GetValue(asset);string raw=Raw(s,csv.Table,row,col.name);
                    if(col.name=="comment") { Check((string)value==string.Empty,"comment remains authoring-only per existing importer"); continue; }
                    fields++;
                    if(col.preserveEmpty)
                    {
                        Check((bool)value.GetType().GetField("hasValue").GetValue(value)==(raw.Length>0),col.name+" presence");
                        if(raw.Length==0) continue;
                        value=value.GetType().GetField("value").GetValue(value);
                    }
                    if(col.type=="float") Check((float)value==float.Parse(raw,CultureInfo.InvariantCulture),col.name+" float");
                    else if(col.type=="int") Check((int)value==int.Parse(raw,CultureInfo.InvariantCulture),col.name+" int");
                    else if(col.type=="bool") Check((bool)value==bool.Parse(raw),col.name+" bool");
                    else if(col.type=="list<ref>") Check(((List<string>)value).SequenceEqual(raw.Split(';')),col.name+" list");
                    else Check((string)value==raw,col.name+" raw string");
                }
                rows.Add(new CK01GeneratedDataRowReference(DataTableValidator.GetRowIdentity(s,row,csv.Table,false),asset));
            }
            tables.Add(new CK01GeneratedDataTable(pair.Key,null,rows));
        }
        var catalog=new CK01GeneratedDataCatalog();catalog.ReplaceTables(tables);
        var methods=new Dictionary<string,string>{{"Characters","TryGetCharacter"},{"Dialogues","TryGetDialogue"},{"DialogueLines","TryGetDialogueLine"},{"CGs","TryGetCG"},{"Scenes","TryGetScene"},{"SceneStates","TryGetSceneState"},{"SceneStatePlacements","TryGetSceneStatePlacement"},{"AmbientPools","TryGetAmbientPool"}};
        foreach(var pair in schemas)
        {
            var method=typeof(CK01GeneratedDataCatalog).GetMethod(methods[pair.Key]);var schema=pair.Value;var csv=parsed[pair.Key];
            foreach(var row in csv.Table.Rows)
            {
                var keys=schema.primaryKeyFields.Count>0?schema.primaryKeyFields:new List<string>{schema.primaryKeyField};
                var args=keys.Select(k=>(object)Raw(schema,csv.Table,row,k)).Concat(new object[]{null,null}).ToArray();
                Check((bool)method.Invoke(catalog,args),pair.Key+" query success: "+args.Last());
                Check(ReferenceEquals(args[args.Length-2],tables.Single(t=>t.TableName==pair.Key).Rows[csv.Table.Rows.IndexOf(row)].Asset),pair.Key+" exact row");
                args[0]="missing";args[args.Length-2]=null;args[args.Length-1]=null;
                Check(!(bool)method.Invoke(catalog,args)&&args[args.Length-2]==null&&!string.IsNullOrEmpty((string)args.Last()),pair.Key+" missing diagnostic");
            }
        }
        var duplicate=tables.Select(t=>t.TableName=="SceneStates"?new CK01GeneratedDataTable(t.TableName,null,t.Rows.Concat(new[]{t.Rows[0]})):t).ToList();
        catalog.ReplaceTables(duplicate);CK01SceneStateData state;string diag;
        Check(!catalog.TryGetSceneState("scn_a","morning",out state,out diag)&&state==null&&!string.IsNullOrEmpty(diag),"duplicate composite query diagnostic");
        catalog.ReplaceTables(tables);Check(catalog.TryGetSceneState("scn_a","morning",out state,out diag)&&state.scene_id=="scn_a","composite cross-scene query");
        Check(catalog.TryGetSceneState("scn_b","morning",out state,out diag)&&state.scene_id=="scn_b","second same state query");
        Action<string,string,string> invalid=(name,field,value)=>Check(DataTableValidator.Validate(schemas[name],Changed(parsed[name],field,value),context).Errors.Count>0,name+" rejects "+field+"="+value);
        invalid("Characters","portrait_offset_x","0.6");invalid("Characters","portrait_offset_y","-0.1");invalid("Characters","portrait_scale","0");invalid("CGs","fade_in","-1");invalid("CGs","ken_burns_pan_x","NaN");invalid("CGs","transition_mode","unknown");
        invalid("Scenes","default_state","evening");invalid("SceneStatePlacements","state_id","evening");invalid("DialogueLines","speaker","");invalid("AmbientPools","dialogue_ids","dlg_normal");invalid("AmbientPools","dialogue_ids","");invalid("Dialogues","cast_01","chr_missing");
        var dupCsv=DataTableCsvParser.Parse(File.ReadAllText(Path.Combine(fixture,"Street/SceneStates.csv")));dupCsv.Table.Rows.Add(dupCsv.Table.Rows[0]);Check(DataTableValidator.Validate(schemas["SceneStates"],dupCsv,context).Errors.Count>0,"duplicate composite validation");
        var unique=NarrativeFilenameLookupPolicy.Resolve("portrait","cook",n=>new List<string>{"Assets/p/cook.png"});Check(unique.AssetPath=="Assets/p/cook.png"&&string.IsNullOrEmpty(unique.Warning),"filename unique");
        foreach(var name in new[]{"missing","COOK","path/cook","cook.png"}) {var r=NarrativeFilenameLookupPolicy.Resolve("portrait",name,n=>new List<string>{"Assets/p/cook.png"});Check(string.IsNullOrEmpty(r.AssetPath)&&!string.IsNullOrEmpty(r.Warning),"filename reject "+name);}
        var ambiguous=NarrativeFilenameLookupPolicy.Resolve("cg","cook",n=>new List<string>{"Assets/a/cook.png","Assets/b/cook.png"});Check(string.IsNullOrEmpty(ambiguous.AssetPath)&&!string.IsNullOrEmpty(ambiguous.Warning),"duplicate file no first match");
        Check(string.IsNullOrEmpty(NarrativeFilenameLookupPolicy.Resolve("visual","",n=>new List<string>()).Warning),"optional empty file");
        Check(CK01ImportRegistry.GetRegistrations().Count==22,"22 registered tables");
        return JsonConvert.SerializeObject(new {passed=true,checks,columns,roundTripFields=fields,rows=tables.Sum(t=>t.Rows.Count),filenameWarnings=warningCount,host="existing StaticCompile UnityStubs",nativePersistenceTested=false});
    }
}
