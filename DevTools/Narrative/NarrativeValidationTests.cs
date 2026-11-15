using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using EatWhat.DataTables;
using EatWhat.Narrative.Core;

// Synthetic authoring inputs; the formal Enable policy and production validator are exercised directly.
public static class NarrativeValidationTests
{
    static int checks, cases;
    static void Check(bool condition,string reason) { checks++; if(!condition) throw new Exception(reason); }
    sealed class Input
    {
        public Dictionary<string,DataTableSchema> Schemas=new Dictionary<string,DataTableSchema>();
        public Dictionary<string,DataTableCsvParseResult> Tables=new Dictionary<string,DataTableCsvParseResult>();
        public DialoguePresentationData Config=new DialoguePresentationData();
        public bool Files=true;
        public string AssetIssue;
        public bool DuplicateFiles;
        public Input(string root)
        {
            var data=(JObject)JObject.Parse(File.ReadAllText(Path.Combine(root,"DevTools/Narrative/Fixtures/ValidationV25/clean.json")))["tables"];
            foreach(var entry in data.Properties())
            {
                string directory=new[]{"Characters","Dialogues","DialogueLines","CGs"}.Contains(entry.Name)?"Narrative":"Street";
                var schema=JsonConvert.DeserializeObject<DataTableSchema>(File.ReadAllText(Path.Combine(root,"DataTables/Schemas",directory,entry.Name+".schema.json")));
                Schemas.Add(entry.Name,schema);
                var rows=new List<string>{string.Join(",",schema.columns.Select(x=>Quote(x.csvColumn)))};
                foreach(JObject row in entry.Value) rows.Add(string.Join(",",schema.columns.Select(x=>Quote((string)row[x.name]??""))));
                Tables.Add(entry.Name,DataTableCsvParser.Parse(string.Join("\n",rows)+"\n"));
            }
            var loc=new DataTableSchema {tableName="Localization"}; loc.columns.Add(new DataTableColumnSchema {name="loc_key",csvColumn="loc_key",type="string"});
            Schemas.Add("Localization",loc); Tables.Add("Localization",DataTableCsvParser.Parse("loc_key\nloc_name\nloc_text\nloc_scene\n"));
        }
        static string Quote(string text) { return "\""+text.Replace("\"","\"\"")+"\""; }
        public void Set(string name,int row,string field,string value) { Tables[name].Table.Rows[row].Values[Tables[name].Table.GetHeaderIndex(field)]=value; }
        public DataTableValidationContext Context(bool domain=true)
        {
            var context=new DataTableValidationContext();
            foreach(var entry in Schemas)
            {
                string directory=entry.Key=="Localization"?"Localization":new[]{"Characters","Dialogues","DialogueLines","CGs"}.Contains(entry.Key)?"Narrative":"Street";
                string path=directory+"/"+entry.Key+".schema.json";
                context.ReferenceTables[path]=new DataTableReferenceSource(entry.Value,Tables[entry.Key].Table);
                foreach(var column in entry.Value.columns)
                {
                    var values=new HashSet<string>(StringComparer.Ordinal);
                    foreach(var row in Tables[entry.Key].Table.Rows) values.Add(DataTableValidator.GetEffectiveValue(column,row,Tables[entry.Key].Table));
                    context.ReferenceValues[DataTableValidator.ReferenceKey(path,column.name)]=values;
                }
            }
            if(domain) NarrativeTableValidation.Enable(context,Config,Files?(Func<string,string,IList<string>>)FilesFor:null);
            return context;
        }
        IList<string> FilesFor(string role,string name)
        {
            var valid=new Dictionary<string,string[]> {{"portrait",new[]{"hero","other"}},{"visual",new[]{"hero_visual"}},{"cg",new[]{"background"}},{"scene",new[]{"SceneA","SceneB"}}};
            if(name==AssetIssue || !valid[role].Contains(name)) return new List<string>();
            var result=new List<string>{"synthetic/"+role+"/"+name+(role=="scene"?".unity":role=="visual"?".prefab":".png")};
            if(DuplicateFiles&&name=="hero") result.Add("synthetic/second/hero.png");
            return result;
        }
        public Dictionary<string,DataTableValidationResult> Validate(bool domain=true)
        {
            var context=Context(domain); var results=new Dictionary<string,DataTableValidationResult>();
            foreach(var pair in Schemas.Where(x=>x.Key!="Localization")) results.Add(pair.Key,DataTableValidator.Validate(pair.Value,Tables[pair.Key],context));
            return results;
        }
    }
    static string[] Messages(Dictionary<string,DataTableValidationResult> results)
    { return results.SelectMany(x=>x.Value.Warnings.Concat(x.Value.Infos)).ToArray(); }
    public static string Run(string root)
    {
        checks=cases=0; var observations=new List<object>();
        var clean=new Input(root).Validate();
        Check(clean.Values.All(x=>x.Errors.Count==0),"complete synthetic fixture generic errors zero");
        Check(Messages(clean).Length==0,"complete synthetic fixture all V13-24 and both info rules zero");
        Action<string,string,string,bool,Action<Input>> isolated=(name,rule,field,info,change)=>
        {
            var input=new Input(root); change(input); var results=input.Validate();
            Check(results.Values.All(x=>x.Errors.Count==0),name+" must remain nonblocking: "+string.Join(";",results.Values.SelectMany(x=>x.Errors)));
            var messages=Messages(results); Check(messages.Length==1,name+" expected one isolated diagnostic: "+string.Join(";",messages));
            int expectedLine=name=="command-cg-ref"?4:name=="full-state"||name.StartsWith("ambient-",StringComparison.Ordinal)?3:2;
            Check(messages[0].Contains("["+rule+"]")&&messages[0].Contains("field '"+field+"'")&&messages[0].Contains("CSV line "+expectedLine+":"),name+" exact rule/column/row");
            Check(results.Values.Sum(x=>x.Infos.Count)==(info?1:0),name+" exact severity"); cases++;
            observations.Add(new {name,rule,info,message=messages[0]});
        };
        isolated("chain-cycle","V-13","first_line",false,x=>x.Set("DialogueLines",0,"next_line","ln_normal"));
        isolated("chain-label","V-13","dialogue_id",false,x=>x.Set("DialogueLines",0,"dialogue_id","dlg_story"));
        isolated("cast-gap","V-14","cast_02",false,x=>{x.Set("Dialogues",0,"cast_01","");x.Set("Dialogues",0,"cast_02","chr_hero");});
        isolated("cast-reference","V-14","cast_01",false,x=>x.Set("Dialogues",0,"cast_01","chr_missing"));
        isolated("cast-no-portrait","V-14","cast_01",false,x=>x.Set("Dialogues",0,"cast_01","chr_narrator"));
        isolated("position-precedence","V-14b","cast_01_station",true,x=>{x.Set("Dialogues",0,"cast_01_station","left");x.Set("Dialogues",0,"cast_01_pos_x","0");});
        isolated("unused-position-precedence","V-14b","cast_01_station",true,x=>{x.Set("Dialogues",0,"cast_01","");x.Set("Dialogues",0,"cast_01_station","left");x.Set("Dialogues",0,"cast_01_pos_y","0");});
        isolated("speaker-required","V-15","speaker",false,x=>x.Set("DialogueLines",0,"speaker",""));
        isolated("speaker-reference","V-15","speaker",false,x=>x.Set("DialogueLines",0,"speaker","chr_missing"));
        isolated("speaker-outside-cast","V-15","speaker",false,x=>x.Set("DialogueLines",0,"speaker","chr_other"));
        isolated("unknown-command","V-16","commands",false,x=>x.Set("DialogueLines",0,"commands","unknown()"));
        isolated("command-syntax","V-16","commands",false,x=>x.Set("DialogueLines",0,"commands","wait("));
        isolated("command-arity","V-16","commands",false,x=>x.Set("DialogueLines",0,"commands","wait()"));
        isolated("command-value","V-16","commands",false,x=>x.Set("DialogueLines",0,"commands","object_active(obj_gate,maybe)"));
        isolated("command-tier","V-16","commands",false,x=>x.Set("DialogueLines",0,"commands","camera_return()"));
        isolated("scene-switch-line","V-16","commands",false,x=>x.Set("DialogueLines",0,"commands","scene_switch(scn_a,entry_a)"));
        isolated("reserved-emote","V-16b","commands",true,x=>x.Set("DialogueLines",0,"commands","actor_emote(chr_hero,smile)"));
        isolated("parallel-wait","V-16b","commands",true,x=>x.Set("DialogueLines",0,"commands","wait(1)"));
        isolated("override-key","V-17","overrides",false,x=>x.Set("Dialogues",0,"overrides","unknown.key=1"));
        isolated("override-type","V-17","overrides",false,x=>x.Set("Dialogues",0,"overrides","tier.normalDim=bad"));
        isolated("template-reference","V-17","tier_profile",false,x=>x.Set("Dialogues",0,"tier_profile","missing"));
        isolated("portrait-file","V-18","portrait_sprite",false,x=>x.AssetIssue="hero");
        isolated("portrait-ambiguous","V-18","portrait_sprite",false,x=>x.DuplicateFiles=true);
        isolated("visual-file","V-18","street_visual_prefab",false,x=>x.AssetIssue="hero_visual");
        isolated("portrait-x","V-18","portrait_offset_x",false,x=>x.Set("Characters",0,"portrait_offset_x","0.6"));
        isolated("portrait-y","V-18","portrait_offset_y",false,x=>x.Set("Characters",0,"portrait_offset_y","-0.1"));
        isolated("portrait-scale","V-18","portrait_scale",false,x=>x.Set("Characters",0,"portrait_scale","0"));
        isolated("cg-file","V-19","cg_sprite",false,x=>x.AssetIssue="background");
        foreach(var field in new[]{"fade_in","fade_out","ken_burns_duration"}) { var captured=field; isolated("cg-"+field,"V-19",field,false,x=>x.Set("CGs",0,captured,"-1")); }
        isolated("default-state","V-20","default_state",false,x=>x.Set("Scenes",0,"default_state","missing"));
        isolated("scene-file","V-20","scene_asset",false,x=>x.AssetIssue="SceneA");
        isolated("placement-dialogue","V-21","dialogue_id",false,x=>x.Set("SceneStatePlacements",0,"dialogue_id","dlg_missing"));
        isolated("placement-pool","V-21","ambient_pool",false,x=>x.Set("SceneStatePlacements",0,"ambient_pool","amb_missing"));
        isolated("full-state","V-21","state_id",false,x=>x.Tables["SceneStatePlacements"].Table.Rows.RemoveAt(1));
        isolated("pool-reference","V-22","dialogue_ids",false,x=>x.Set("AmbientPools",0,"dialogue_ids","dlg_missing"));
        isolated("pool-tier","V-22","dialogue_ids",false,x=>x.Set("AmbientPools",0,"dialogue_ids","dlg_normal"));
        isolated("command-dialogue-ref","V-23","on_end",false,x=>x.Set("Dialogues",0,"on_end","start_dialogue(dlg_missing)"));
        isolated("command-scene-ref","V-23","on_end",false,x=>x.Set("Dialogues",0,"on_end","scene_switch(scn_missing,entry_a)"));
        isolated("command-state-scope","V-23","on_end",false,x=>x.Set("Dialogues",0,"on_end","set_scene_state(scn_b,evening)"));
        isolated("command-cg-ref","V-23","on_end",false,x=>x.Set("Dialogues",2,"on_end","cg_show(cg_missing)"));
        isolated("ambient-continuation","V-24","next_dialogue",false,x=>x.Set("Dialogues",1,"next_dialogue","dlg_normal"));
        isolated("ambient-cast","V-24","cast_01",false,x=>x.Set("Dialogues",1,"cast_01","chr_hero"));
        isolated("general-reference","NARRATIVE_REFERENCE","next_dialogue",false,x=>x.Set("Dialogues",0,"next_dialogue","dlg_missing"));
        var generalState=new Input(root);generalState.Set("SceneStates",1,"scene_id","scn_missing");var stateResult=generalState.Validate()["SceneStates"];Check(stateResult.Errors.Count==0&&stateResult.Warnings.Count==1&&stateResult.Warnings[0].Contains("[NARRATIVE_REFERENCE]")&&stateResult.Warnings[0].Contains("CSV line 3: field 'scene_id'"),"SceneStates scene reference has its own common warning identifier");
        // Branches with multiple diagnostics all remain in their one intended rule family.
        Action<string,string,Action<Input>> family=(name,rule,change)=> {var x=new Input(root);change(x);var r=x.Validate();Check(r.Values.All(y=>y.Errors.Count==0),name+" no generic errors");var m=Messages(r);Check(m.Length>0&&m.All(y=>y.Contains("["+rule+"]")),name+" only expected rule: "+string.Join(";",m));cases++;observations.Add(new{name,rule,info=false,messages=m});};
        family("missing-chain-link","V-13",x=>x.Set("DialogueLines",0,"next_line","ln_missing"));
        family("missing-chain-entry","V-13",x=>x.Set("Dialogues",0,"first_line","ln_missing"));
        family("shared-chain-label","V-13",x=>x.Set("Dialogues",2,"first_line","ln_normal"));
        family("orphan-line","V-13",x=>{var copy=DataTableCsvParser.Parse(string.Join(",",x.Tables["DialogueLines"].Table.Headers)+"\n").Table;var row=new DataTableCsvRow{LineNumber=5};row.Values.AddRange(x.Tables["DialogueLines"].Table.Rows[0].Values);x.Tables["DialogueLines"].Table.Rows.Add(row);x.Set("DialogueLines",3,"line_id","ln_orphan");});
        family("placement-state","V-21",x=>x.Set("SceneStatePlacements",0,"state_id","missing"));
        family("placement-actor","V-21",x=>x.Set("SceneStatePlacements",0,"target","chr_missing#2"));
        foreach(string field in new[]{"anchor_id","facing","dialogue_id","ambient_pool","marker"})
        {
            var captured=field;isolated("object-only-"+field,"V-21",field,false,x=>{for(int i=0;i<3;i++){x.Set("SceneStatePlacements",i,"kind","object");x.Set("SceneStatePlacements",i,"dialogue_id","");x.Set("SceneStatePlacements",i,"ambient_pool","");} x.Set("SceneStatePlacements",0,captured,captured=="facing"?"left":captured=="dialogue_id"?"dlg_normal":captured=="ambient_pool"?"amb_one":captured=="marker"?"talk":"anchor_a");});
        }
        var sequential=new Input(root); sequential.Set("DialogueLines",0,"commands","wait(1)"); sequential.Set("Dialogues",0,"overrides","tier.commandExecutionMode=Sequential");Check(Messages(sequential.Validate()).Length==0,"N007 sequential override prevents parallel wait info");
        var named=new Input(root);named.Config.tier=new[]{new DialogueTierConfig(),new DialogueTierConfig{name="slow",commandExecutionMode=NarrativeCommandExecutionMode.Sequential}};named.Set("Dialogues",0,"tier_profile","slow");named.Set("DialogueLines",0,"commands","wait(1)");Check(Messages(named.Validate()).Length==0,"actual named N006 tier supplies sequential mode");
        var instances=new Input(root); for(int i=0;i<3;i++)instances.Set("SceneStatePlacements",i,"target","chr_hero#02"); Check(Messages(instances.Validate()).Length==0,"N003 actor suffix resolves exact base identity");
        var absent=new Input(root); absent.Config=null;var missing=Messages(absent.Validate());Check(missing.Length>0&&missing.All(x=>x.Contains("[NARRATIVE_UNVERIFIED]")),"missing real config is unverified, not missing-name warnings");
        var noFiles=new Input(root);noFiles.Files=false;Check(Messages(noFiles.Validate()).All(x=>x.Contains("[NARRATIVE_UNVERIFIED]")),"missing file input explicitly unverified");
        foreach(var bad in new[]{new[]{"Characters","portrait_offset_x","NaN"},new[]{"CGs","fade_in","oops"},new[]{"CGs","transition_mode","unknown"},new[]{"Characters","character_id","bad"},new[]{"DialogueLines","text_key","loc_missing"},new[]{"Scenes","name_key",""}})
        {var x=new Input(root);x.Set(bad[0],0,bad[1],bad[2]);Check(x.Validate().Values.Any(y=>y.Errors.Count>0),"common error retained: "+string.Join("/",bad));}
        var strict=new Input(root);strict.Set("Characters",0,"portrait_scale","0");Check(strict.Validate(false)["Characters"].Errors.Count>0,"unconfigured generic API keeps old strict range error");Check(strict.Validate()["Characters"].Errors.Count==0,"same input in formal domain uses V18 warning");
        var formalContext=strict.Context(false);NarrativeTableValidation.EnableProject(formalContext,root);var formal=DataTableValidator.Validate(strict.Schemas["Characters"],strict.Tables["Characters"],formalContext);Check(formal.Errors.Count==0&&formal.Warnings.Any(x=>x.Contains("[V-18]")&&x.Contains("field 'portrait_scale'")),"exact formal EnableProject factory enables warning policy by default");
        var duplicate=new Input(root); duplicate.Tables["SceneStates"].Table.Rows.Add(duplicate.Tables["SceneStates"].Table.Rows[0]);Check(duplicate.Validate()["SceneStates"].Errors.Count>0,"composite primary duplicate remains error");
        var oldSchema=new DataTableSchema {tableName="OldTable",csvPath="old.csv",targetType="Old",outputDirectory="Assets/Generated/Old",assetNameField="id",primaryKeyField="id"}; oldSchema.columns.Add(new DataTableColumnSchema{name="id",csvColumn="id",type="string",required=true});
        var oldCsv=DataTableCsvParser.Parse("id\n\"\"\n"); var oldStrict=DataTableValidator.Validate(oldSchema,oldCsv,new DataTableValidationContext());var enabled=new DataTableValidationContext();NarrativeTableValidation.Enable(enabled,new DialoguePresentationData(),(a,b)=>new List<string>());var oldDomain=DataTableValidator.Validate(oldSchema,oldCsv,enabled);Check(oldStrict.Errors.SequenceEqual(oldDomain.Errors)&&oldStrict.Warnings.SequenceEqual(oldDomain.Warnings)&&oldDomain.Infos.Count==0,"unrelated table diagnostics identical with domain enabled");
        return JsonConvert.SerializeObject(new {passed=true,checks,isolatedCases=cases,cleanGenericErrors=0,cleanV13Through24Diagnostics=0,syntheticInputs=true,nativeUnity=false,observations});
    }
}
