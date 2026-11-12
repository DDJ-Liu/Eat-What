using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using EatWhat.DataTables;
using EatWhat.Cooking.ShortCycle;
using UnityEngine;

public static class ShortCyclePhase2Tests
{
    public static IDictionary<string, string[]> VerifiedStepChains { get; private set; }
    public static IList<string> PassedCases { get; private set; }
    public static IDictionary<string, bool> SchemaBranches { get; private set; }
    private static void Check(bool condition, string id)
    { if (!condition) throw new Exception("P2L-01: " + id); PassedCases.Add(id); }
    internal static CK01GeneratedDataTable Table<T>(string root, string name, string primaryKey) where T : ScriptableObject, new()
    {
        var csv = DataTableCsvParser.Parse(File.ReadAllText(Path.Combine(root, "DataTables/Cooking/" + name + ".csv")));
        Check(csv.IsValid, "CSV parses: " + name);
        var rows = new List<CK01GeneratedDataRowReference>();
        foreach (var row in csv.Table.Rows)
        {
            var values = new Dictionary<string, string>();
            for (var i = 0; i < csv.Table.Headers.Count; i++) values[csv.Table.Headers[i]] = row.Values[i];
            var asset = new T(); asset.name = values[primaryKey];
            foreach (var field in typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                string value; if (!values.TryGetValue(field.Name, out value)) continue;
                if (field.FieldType == typeof(string)) field.SetValue(asset, value);
                else if (field.FieldType == typeof(bool)) field.SetValue(asset, value == "true");
                else if (field.FieldType == typeof(List<string>)) field.SetValue(asset, new List<string>(value.Split(new[] { name == "KitchenAreas" ? '|' : ';' }, StringSplitOptions.RemoveEmptyEntries)));
            }
            rows.Add(new CK01GeneratedDataRowReference(values[primaryKey], asset));
        }
        return new CK01GeneratedDataTable(name, null, rows);
    }
    private static ProcessingItemStack[] Items(params string[] ids)
    { return ids.Select(id => new ProcessingItemStack(id)).ToArray(); }
    private static ProcessingMatcher Matcher(string[] whitelist, AreaConstraint area, params ProcessingRecordView[] records)
    { return new ProcessingMatcher(new RecipeWhitelist("fixture-recipe", whitelist), area, records, "fixture_configured_unknown"); }
    private static void Branch(int number, bool condition, string id)
    { Check(condition, id); SchemaBranches.Add("branch" + number, condition); }
    public static string Run(string root)
    {
        PassedCases = new List<string>(); SchemaBranches = new Dictionary<string, bool>(); VerifiedStepChains = new Dictionary<string, string[]>();
        var catalog = new CK01GeneratedDataCatalog();
        catalog.ReplaceTables(new[] {
            Table<CK01RecipeData>(root,"Recipes","recipe_id"), Table<CK01ProcessingRecordData>(root,"ProcessingRecords","proc_id"),
            Table<CK01RecipeStepData>(root,"RecipeSteps","step_id"),Table<CK01ToolData>(root,"Tools","tool_id"),
            Table<KitchenAreaData>(root,"KitchenAreas","id"),Table<CK01ItemData>(root,"Items","item_id"),
            Table<CK01CookingGameConfigData>(root,"CookingGameConfig","key") });
        string diagnostic; UnknownProductResolver unknown;
        Check(catalog.TryGetUnknownProductResolver(out unknown,out diagnostic),"Unknown fallback resolves through existing T8/T4 catalog");
        var recipeIds = new[] {"rcp_qingtang_noodle","rcp_tomato_egg","rcp_kaishui_cabbage"};
        foreach (var id in recipeIds)
        {
            RecipeWhitelist whitelist; IList<ProcessingRecordView> records; IList<RecipeStepRuleView> steps;
            Check(catalog.TryGetRecipeWhitelist(id,out whitelist,out diagnostic),"Actual whitelist resolves: "+id);
            Check(catalog.TryGetProcessingRecords(whitelist.ProcessingRecordIds,out records,out diagnostic),"Whitelist record snapshots: "+id);
            Check(records.Select(x=>x.Id).SequenceEqual(whitelist.ProcessingRecordIds),"Whitelist order preserved: "+id);
            Check(catalog.TryGetStepChain(id,out steps,out diagnostic),"Existing validated step-chain adapter: "+id);
            VerifiedStepChains[id]=steps.Select(x=>x.StepId).ToArray();
            IList<CK01RecipeStepData> raw;catalog.TryGetRecipeStepChain(id,out raw,out diagnostic);
            for(var i=0;i<steps.Count;i++)
                Check(steps[i].TriggerItems.SequenceEqual(raw[i].trigger_items) && steps[i].PermanentTriggerItems.SequenceEqual(raw[i].permanent_trigger_items),"T3 trigger snapshots: "+steps[i].StepId);
            // Actual T12 records, with explicitly synthetic permissive tool/area snapshots. This does not claim current T7/T14 playability.
            var fixtureArea=new AreaConstraint("fixture-area",records.Select(x=>x.Action),records.Select(x=>x.Carrier));
            var matcher=new ProcessingMatcher(whitelist,fixtureArea,records,unknown.ItemId);
            foreach(var record in records.Take(2))
            {
                var recordResult=matcher.Match(id,"fixture-area",Items(record.Inputs.ToArray()),new ProcessingToolView("fixture-supported-tool",new[]{record.Action}),new[]{record.Carrier});
                Check(recordResult.Matched && recordResult.Record.Id==record.Id,"Actual T12 standard/alternative inputs with fixture capability: "+id+"/"+record.Id);
            }
            var progress=new StepProgress(id,steps,unknown.ItemId);var events=0;
            progress.FinalDishReady+=e=>{events++;Check(e.RecipeId==id && e.TailStepId==steps[steps.Count-1].StepId,"Actual chain tail event identity: "+id);};
            progress.OnPrepZoneChanged(Items(steps[0].TriggerItems[0]));
            Check(progress.GetStates()[0].State==StepCompletionState.Dynamic,"Actual step dynamic trigger: "+id);
            progress.OnPrepZoneChanged(Items());Check(progress.GetStates()[0].State==StepCompletionState.None,"Actual step dynamic rollback: "+id);
            var tail=steps[steps.Count-1].TriggerItems[0];progress.OnPrepZoneChanged(Items(tail));
            Check(progress.IsFinalDishReady && events==1,"Actual chain tail completion emits once: "+id);
            progress.OnPrepZoneChanged(Items(tail));Check(events==1,"Unchanged prep snapshot does not duplicate final event: "+id);
            progress.OnPrepZoneChanged(Items());Check(!progress.IsFinalDishReady,"Actual tail dynamic rollback: "+id);
            progress.OnPrepZoneChanged(Items(tail));Check(events==2,"Actual tail new completion edge emits again: "+id);
        }
        RecipeWhitelist tomato;catalog.TryGetRecipeWhitelist(recipeIds[1],out tomato,out diagnostic);IList<ProcessingRecordView> tomatoRecords;catalog.TryGetProcessingRecords(tomato.ProcessingRecordIds,out tomatoRecords,out diagnostic);
        AreaConstraint prep,stove;catalog.TryGetAreaConstraint("kitchen-prep-counter",out prep,out diagnostic);catalog.TryGetAreaConstraint("kitchen-stove",out stove,out diagnostic);
        ProcessingToolView chopsticks,knife,spatula,hand;catalog.TryGetProcessingTool("tool_chopsticks",out chopsticks,out diagnostic);catalog.TryGetProcessingTool("tool_knife",out knife,out diagnostic);catalog.TryGetProcessingTool("tool_spatula",out spatula,out diagnostic);catalog.TryGetProcessingTool("tool_hand",out hand,out diagnostic);
        var prepMatcher=new ProcessingMatcher(tomato,prep,tomatoRecords,unknown.ItemId);var stoveMatcher=new ProcessingMatcher(tomato,stove,tomatoRecords,unknown.ItemId);
        Check(prepMatcher.Match(tomato.RecipeId,prep.AreaId,Items("ing_egg"),chopsticks,new[]{"tool_bowl"}).Record.Id=="proc_egg_liquid","Actual T7/T14 whisk path");
        Check(prepMatcher.Match(tomato.RecipeId,prep.AreaId,Items("ing_tomato"),knife,new[]{"tool_board"}).Record.Id=="proc_tomato_cut","Actual T7/T14 cut path");
        Check(stoveMatcher.Match(tomato.RecipeId,stove.AreaId,Items("prd_scrambled_egg","prd_tomato_cut"),spatula,new[]{"tool_pan"}).Record.Id=="proc_tomato_egg_dish","Actual standard final path");
        Check(stoveMatcher.Match(tomato.RecipeId,stove.AreaId,Items("prd_tomato_cut","prd_egg_liquid","stp_water"),hand,new[]{"tool_pot"}).RejectionCode==ProcessingRejectionCode.ToolActionUnsupported,"Actual alternate soup cannot bypass missing boil capability in T7");
        RecipeWhitelist cabbage;catalog.TryGetRecipeWhitelist(recipeIds[2],out cabbage,out diagnostic);IList<ProcessingRecordView> cabbageRecords;catalog.TryGetProcessingRecords(cabbage.ProcessingRecordIds,out cabbageRecords,out diagnostic);
        Check(new ProcessingMatcher(cabbage,prep,cabbageRecords,unknown.ItemId).Match(cabbage.RecipeId,prep.AreaId,Items("ing_cabbage"),hand,new[]{"tool_board"}).ProducesUnknown,"Actual cabbage whitelist lacks cabbage route; no fabricated repair");
        var a=new ProcessingRecordView("a","output-a",new[]{"egg","egg"},"fry","pan",false);
        var b=new ProcessingRecordView("b","output-b",new[]{"egg","egg"},"fry","pot",true);
        var outside=new ProcessingRecordView("outside","bad",new[]{"outside-input"},"fry","pan",false);
        var area=new AreaConstraint("current",new[]{"fry"},new[]{"pan","pot"});var tool=new ProcessingToolView("test-tool",new[]{"fry"});
        var multi=Matcher(new[]{"b","a"},area,a,outside,b);
        var one=Matcher(new[]{"a"},area,a,outside,b);
        Branch(1,one.Match("fixture-recipe","current",Items("egg","egg"),tool,new[]{"pan"}).Matched && !one.Match("fixture-recipe","current",Items("egg"),tool,new[]{"pan"}).Matched,"Schema8.1 exact input multiset plus action filter");
        Branch(2,one.Match("fixture-recipe","current",Items("outside-input"),tool,new[]{"pan"}).OutputItemId=="fixture_configured_unknown","Schema8.2 whitelist intersection excludes global dictionary record; configured unknown ID");
        Branch(3,multi.Match("fixture-recipe","current",Items("egg","egg"),tool,new[]{"pan"}).Record.Id=="a","Schema8.3 current scene carrier intersection keeps only available carrier");
        Branch(4,!one.Match("fixture-recipe","current",Items("no-match"),tool,new[]{"pan"}).Matched,"Schema8.4 zero candidates -> configured unknown");
        Branch(5,one.Match("fixture-recipe","current",Items("egg","egg"),tool,new[]{"pan"}).CandidateRecordIds.Count==1,"Schema8.5 single candidate hit");
        var result=multi.Match("fixture-recipe","current",new[]{new ProcessingItemStack("egg",2)},tool,new[]{"pan","pot"});
        Branch(6,result.Record.Id=="b" && result.CandidateRecordIds.SequenceEqual(new[]{"b","a"}),"Schema8.6 multi candidate picks whitelist first, not dictionary/scene order");
        Check(one.Match("fixture-recipe","current",Items("egg","egg","egg"),tool,new[]{"pan"}).ProducesUnknown,"Multiset excess quantity does not match");
        var permuted=new ProcessingRecordView("mix","out",new[]{"egg","tomato"},"fry","",false);var noCarrier=Matcher(new[]{"mix"},area,permuted);
        Check(noCarrier.Match("fixture-recipe","current",Items("tomato","egg"),tool,new string[0]).Matched,"Column order irrelevant and blank carrier requires no fabricated carrier");
        Check(one.Match("fixture-recipe","other",Items("egg","egg"),tool,new[]{"pan"}).RejectionCode==ProcessingRejectionCode.InvalidRequest,"X5 wrong area identity rejected");
        Check(one.Match("fixture-recipe","current",Items("egg","egg"),tool,new[]{"pot"}).RejectionCode==ProcessingRejectionCode.CarrierUnavailable,"X5 no current carrier refuses; no search in other areas");
        Check(Matcher(new[]{"a"},new AreaConstraint("current",new[]{"cut"},new[]{"pan"}),a).Match("fixture-recipe","current",Items("egg","egg"),tool,new[]{"pan"}).RejectionCode==ProcessingRejectionCode.ActionNotAllowed,"Explicit area action rejection code");
        Check(Matcher(new[]{"a"},new AreaConstraint("current",new[]{"fry"},new[]{"pot"}),a).Match("fixture-recipe","current",Items("egg","egg"),tool,new[]{"pan"}).RejectionCode==ProcessingRejectionCode.CarrierNotAllowed,"Explicit area carrier rejection code");
        Check(one.Match("fixture-recipe","current",Items("egg","egg"),new ProcessingToolView("wrong-tool",new[]{"cut"}),new[]{"pan"}).RejectionCode==ProcessingRejectionCode.ToolActionUnsupported,"Tool supported_actions is mandatory");
        Check(one.Match("fixture-recipe","current",Items(),tool,new[]{"pan"}).RejectionCode==ProcessingRejectionCode.InvalidRequest,"Empty inputs rejected without unknown output");
        var rules=new[]{new RecipeStepRuleView("s1",new[]{"raw","fixture_configured_unknown"},new[]{"fold","fixture_configured_unknown"}),new RecipeStepRuleView("s2",new[]{"tail","fixture_configured_unknown"},new[]{"permanent-tail"})};
        var p=new StepProgress("r",rules,"fixture_configured_unknown");var count=0;p.FinalDishReady+=e=>count++;
        p.OnPrepZoneChanged(Items("fixture_configured_unknown"));Check(p.GetStates().All(x=>x.State==StepCompletionState.None) && count==0,"Unknown never triggers dynamic/permanent/final even if listed in rule fixture");
        p.OnPrepZoneChanged(Items("raw"));var detached=p.GetStates();Check(detached[0].State==StepCompletionState.Dynamic && count==0,"Non-tail completion does not emit final");
        p.OnPrepZoneChanged(Items());Check(p.GetStates()[0].State==StepCompletionState.None && detached[0].State==StepCompletionState.Dynamic,"States snapshot isolation and dynamic regression");
        p.OnPrepZoneChanged(Items("fold"));p.OnPrepZoneChanged(Items());Check(p.GetStates()[0].State==StepCompletionState.Permanent,"New folded product permanently completes step after removal");
        p.OnPrepZoneChanged(Items("permanent-tail"));p.OnPrepZoneChanged(Items());Check(p.IsFinalDishReady && count==1,"Permanent tail survives removal and emits once");
        IList<RecipeStepRuleView> foldChain;catalog.TryGetStepChain("rcp_tomato_egg",out foldChain,out diagnostic);var folded=new StepProgress("rcp_tomato_egg",foldChain,unknown.ItemId);
        folded.OnPrepZoneChanged(Items("prd_tomato_egg_soup"));Check(folded.GetStates().Take(3).All(x=>x.State==StepCompletionState.Permanent) && folded.IsFinalDishReady,"Actual soup product folds first three steps permanently in chain order");
        folded.OnPrepZoneChanged(Items());Check(folded.GetStates().Take(3).All(x=>x.State==StepCompletionState.Permanent) && !folded.IsFinalDishReady,"Actual folded steps retained while dynamic final regresses");
        var ids=new List<string>{"egg"};var view=new ProcessingRecordView("snap","out",ids,"fry","pan",false);ids.Clear();Check(view.Inputs.Count==1,"Processing record detaches caller inputs");
        var rejected=false;try{((IList<string>)view.Inputs).Add("bad");}catch(NotSupportedException){rejected=true;}Check(rejected,"Processing snapshot inputs read-only");
        rejected=false;try{((IList<StepProgressEntry>)detached).Clear();}catch(NotSupportedException){rejected=true;}Check(rejected,"Step state snapshot read-only");
        IList<ProcessingRecordView> missing;Check(!catalog.TryGetProcessingRecords(new[]{"proc_egg_liquid","missing"},out missing,out diagnostic) && missing.Count==0,"Catalog failed bulk lookup publishes no partial snapshot");
        CK01ToolData rawTool;catalog.TryGetTool("tool_hand",out rawTool,out diagnostic);rawTool.supported_actions.Add("fixture.changed");Check(!hand.SupportedActions.Contains("fixture.changed"),"Tool snapshot detaches serialized actions");
        CK01RecipeStepData rawStep;catalog.TryGetRow("RecipeSteps","step_te_01",out rawStep,out diagnostic);rawStep.trigger_items.Clear();Check(foldChain[0].TriggerItems.Count>0,"Step rule snapshot detaches serialized trigger lists");
        return "PASS: P2L-01 "+PassedCases.Count+" assertions; Schema8 six branches, actual CSV snapshots and explicit pure fixtures.";
    }
    public static IList<string> OperationZoneCases { get; private set; }
    private static void ZoneCheck(bool condition, string id)
    { if (!condition) throw new Exception("P2-C10: " + id); OperationZoneCases.Add(id); }

    public static string RunOperationZone()
    {
        OperationZoneCases = new List<string>();
        var standard = new ProcessingRecordView("standard","out-standard",new[]{"egg","tomato"},"cook","pan",false);
        var alternate = new ProcessingRecordView("alternate","out-alternate",new[]{"onion","egg"},"cook","pan",false);
        var doubled = new ProcessingRecordView("doubled","out-double",new[]{"egg","egg"},"cook","pan",false);
        var pot = new ProcessingRecordView("pot","out-pot",new[]{"egg","water"},"cook","pot",false);
        var blank = new ProcessingRecordView("blank","out-blank",new[]{"flour"},"cook","",false);
        var outside = new ProcessingRecordView("outside","bad",new[]{"outsider"},"cook","pan",false);
        var records = new[]{standard,alternate,doubled,pot,blank,outside};
        var whitelist = new RecipeWhitelist("recipe",new[]{"standard","alternate","doubled","pot","blank"});
        var area = new AreaConstraint("prep",new[]{"cook"},new[]{"pan","pot"});
        var scene = new List<string>{"pan","pot"};
        var zone = new OperationZoneSession(whitelist,area,records,scene);
        var first = zone.TryAdd("recipe","prep","egg");
        ZoneCheck(first.Accepted && first.Snapshot.CarrierId=="pan" &&
            first.Snapshot.CandidateRecordIds.SequenceEqual(new[]{"standard","alternate","doubled"}) &&
            first.Snapshot.InitialCarrierCandidates.SequenceEqual(new[]{"pan","pot"}) &&
            first.Snapshot.InitialCarrierAmbiguous,"First input atomically selects whitelist-first available carrier, not one proc");
        scene.Clear();
        ZoneCheck(zone.TryAdd("recipe","prep","tomato").Accepted &&
            zone.GetSnapshot().CandidateRecordIds.SequenceEqual(new[]{"standard"}),
            "Standard route narrows despite caller mutating scene inventory list");
        var completed = zone.Complete();
        ZoneCheck(completed.Inputs.Count==2 && completed.CarrierId=="pan" &&
            !zone.GetSnapshot().HasInputs && zone.GetSnapshot().CarrierId==null,
            "Complete returns detached prior state and clears carrier/inputs");
        ZoneCheck(zone.TryAdd("recipe","prep","egg").Accepted &&
            zone.TryAdd("recipe","prep","onion").Accepted &&
            zone.GetSnapshot().CandidateRecordIds.SequenceEqual(new[]{"alternate"}),
            "Alternative route remains legal after first shared ingredient");
        zone.Clear();
        ZoneCheck(zone.TryAdd("recipe","prep","onion").Accepted &&
            zone.TryAdd("recipe","prep","egg").Accepted &&
            zone.GetSnapshot().CandidateRecordIds.SequenceEqual(new[]{"alternate"}),
            "Input order has no semantic effect");
        zone.Clear();
        ZoneCheck(zone.TryAdd("recipe","prep","egg",2).Accepted &&
            zone.GetSnapshot().CandidateRecordIds.SequenceEqual(new[]{"doubled"}) &&
            zone.GetSnapshot().Inputs[0].Quantity==2,"Duplicate item quantity is a multiset");
        var before=zone.GetSnapshot();
        var excess=zone.TryAdd("recipe","prep","egg");
        ZoneCheck(!excess.Accepted && excess.RejectionCode==OperationZoneRejectionCode.QuantityExceedsPath &&
            zone.GetSnapshot().Inputs.Count==1 && zone.GetSnapshot().Inputs[0].Quantity==2 &&
            zone.GetSnapshot().CandidateRecordIds.SequenceEqual(before.CandidateRecordIds),
            "Excess rejects atomically");
        var wrong=zone.TryAdd("other","prep","tomato");
        ZoneCheck(wrong.RejectionCode==OperationZoneRejectionCode.WrongRecipe &&
            zone.GetSnapshot().CandidateRecordIds.SequenceEqual(before.CandidateRecordIds),
            "Wrong recipe rejects atomically");
        ZoneCheck(zone.TryAdd("recipe","stove","tomato").RejectionCode==OperationZoneRejectionCode.WrongArea &&
            zone.GetSnapshot().Inputs.Count==1,"X5 wrong area rejects without cross-area search");
        ZoneCheck(zone.TryAdd("recipe","prep","",1).RejectionCode==OperationZoneRejectionCode.InvalidRequest &&
            zone.TryAdd("recipe","prep","egg",0).RejectionCode==OperationZoneRejectionCode.InvalidRequest &&
            zone.GetSnapshot().Inputs.Count==1,"Invalid requests preserve state");
        zone.Clear();
        ZoneCheck(zone.TryAdd("recipe","prep","outsider").RejectionCode==OperationZoneRejectionCode.NoWhitelistedPath &&
            !zone.GetSnapshot().HasInputs,"Global record outside whitelist cannot start a session");
        var lockZone=new OperationZoneSession(new RecipeWhitelist("recipe",new[]{"pot","standard"}),
            area,records,new[]{"pot","pan"});
        ZoneCheck(lockZone.TryAdd("recipe","prep","egg").Snapshot.CarrierId=="pot" &&
            lockZone.GetSnapshot().InitialCarrierCandidates.SequenceEqual(new[]{"pot","pan"}),
            "G74 selection follows whitelist rather than scene order");
        var lockBefore=lockZone.GetSnapshot();
        var locked=lockZone.TryAdd("recipe","prep","tomato");
        ZoneCheck(locked.RejectionCode==OperationZoneRejectionCode.CarrierPathLocked &&
            lockZone.GetSnapshot().Inputs.Count==1 &&
            lockZone.GetSnapshot().CandidateRecordIds.SequenceEqual(lockBefore.CandidateRecordIds),
            "Different carrier path rejects atomically");
        ZoneCheck(lockZone.TryAdd("recipe","prep","water").Accepted &&
            lockZone.GetSnapshot().CandidateRecordIds.SequenceEqual(new[]{"pot"}),
            "Locked carrier still narrows a legal route");
        var missing=new OperationZoneSession(new RecipeWhitelist("recipe",new[]{"standard"}),
            area,records,new string[0]);
        ZoneCheck(missing.TryAdd("recipe","prep","egg").RejectionCode==OperationZoneRejectionCode.CarrierUnavailable &&
            !missing.GetSnapshot().HasInputs,"Missing current-area carrier refuses");
        var forbidden=new OperationZoneSession(new RecipeWhitelist("recipe",new[]{"standard"}),
            new AreaConstraint("prep",new[]{"cook"},new[]{"pot"}),records,new[]{"pan"});
        ZoneCheck(forbidden.TryAdd("recipe","prep","egg").RejectionCode==OperationZoneRejectionCode.CarrierNotAllowed,
            "Configured area carrier constraint refuses");
        var wrongAction=new OperationZoneSession(new RecipeWhitelist("recipe",new[]{"standard"}),
            new AreaConstraint("prep",new[]{"cut"},new[]{"pan"}),records,new[]{"pan"});
        ZoneCheck(wrongAction.TryAdd("recipe","prep","egg").RejectionCode==OperationZoneRejectionCode.ActionNotAllowed,
            "Configured area action constraint refuses");
        var carrierless=new OperationZoneSession(new RecipeWhitelist("recipe",new[]{"blank"}),
            area,records,new string[0]);
        ZoneCheck(carrierless.TryAdd("recipe","prep","flour").Accepted &&
            carrierless.GetSnapshot().CarrierId==null &&
            carrierless.GetSnapshot().InitialCarrierCandidates.Count==0,
            "Legal blank-carrier record does not fabricate scene equipment");
        var detached=carrierless.GetSnapshot();
        var readOnly=false;try{((IList<ProcessingItemStack>)detached.Inputs).Clear();}catch(NotSupportedException){readOnly=true;}
        ZoneCheck(readOnly && carrierless.GetSnapshot().Inputs.Count==1,
            "Returned input snapshot cannot mutate live session");
        readOnly=false;try{((IList<string>)detached.CandidateRecordIds).Clear();}catch(NotSupportedException){readOnly=true;}
        ZoneCheck(readOnly && carrierless.GetSnapshot().CandidateRecordIds.Count==1,
            "Returned candidate snapshot cannot mutate live session");
        carrierless.Clear();
        carrierless.Clear();
        ZoneCheck(!carrierless.GetSnapshot().HasInputs && detached.Inputs.Count==1,
            "Empty clear is safe and old snapshots remain detached");
        var incomplete=false;try{new OperationZoneSession(new RecipeWhitelist("recipe",new[]{"missing"}),
            area,records,new[]{"pan"});}catch(ArgumentException){incomplete=true;}
        ZoneCheck(incomplete,"Incomplete whitelist snapshots fail closed");
        return "PASS: P2-C10 "+OperationZoneCases.Count+" operation-zone assertions.";
    }    public static IList<string> ProductNameCases { get; private set; }
    private static void NameCheck(bool condition, string id)
    { if (!condition) throw new Exception("P2-C11: " + id); ProductNameCases.Add(id); }
    private sealed class FixtureNames : IPhase2ProductNameSource
    {
        private static readonly Dictionary<string,string> Names=new Dictionary<string,string> {
            {"egg","Egg"},{"tomato","Tomato"},{"onion","Onion"},{"carrot","Carrot"},
            {"rice","Rice"},{"unknown","Unknown"},{"knife","Knife"},{"bowl","Bowl"},
            {"pan-tool","Spatula"},{"pot-tool","Ladle"},{"cut","Cut"},{"whisk","Whisk"},
            {"stir","Stir"},{"boil","Boil"}};
        private static bool Name(string id,out string name) { return Names.TryGetValue(id,out name); }
        public bool TryGetItemName(string id,out string name) { return Name(id,out name); }
        public bool TryGetToolName(string id,out string name) { return Name(id,out name); }
        public bool TryGetActionName(string id,out string name) { return Name(id,out name); }
    }
    private static Phase2ProductDraft ProductName(string[] inputs,string action,string carrier,string toolId,
        out string diagnostic)
    {
        var record=new ProcessingRecordView("fixture-record","product",inputs,action,carrier,false);
        var area=new AreaConstraint("fixture-area",new[]{action},new[]{carrier});
        var whitelist=new RecipeWhitelist("fixture-recipe",new[]{record.Id});
        var zone=new OperationZoneSession(whitelist,area,new[]{record},new[]{carrier});
        foreach(var input in inputs)
            if(!zone.TryAdd("fixture-recipe","fixture-area",input).Accepted)
                throw new Exception("P2-C11 fixture input was rejected: "+input);
        var snapshot=zone.GetSnapshot();
        var tool=new ProcessingToolView(toolId,new[]{action});
        var match=new ProcessingMatcher(whitelist,area,new[]{record},"unknown")
            .Match("fixture-recipe","fixture-area",snapshot.Inputs,tool,new[]{carrier});
        if(!match.Matched || match.Record.Id!=record.Id)
            throw new Exception("P2-C11 fixture did not produce its public match.");
        Phase2ProductDraft draft;
        var ok=Phase2ProductProduction.TryCreate(match,snapshot,tool,"unknown",new FixtureNames(),out draft,out diagnostic);
        return ok?draft:null;
    }
    public static string RunProductNames()
    {
        ProductNameCases=new List<string>();string error;Phase2ProductDraft draft;
        draft=ProductName(new[]{"egg","tomato"},"cut","board","knife",out error);
        NameCheck(draft!=null && draft.DisplayName=="Egg + Tomato · Knife · Cut",
            "Name example 1: ordered two ingredients, knife, cut");
        draft=ProductName(new[]{"tomato","egg"},"cut","board","knife",out error);
        NameCheck(draft!=null && draft.DisplayName=="Egg + Tomato · Knife · Cut",
            "Name example 2: reverse input order keeps stable display");
        draft=ProductName(new[]{"egg","egg"},"whisk","bowl","bowl",out error);
        NameCheck(draft!=null && draft.DisplayName=="Egg ×2 · Bowl · Whisk",
            "Name example 3: repeated item retains quantity identity");
        draft=ProductName(new[]{"onion","carrot"},"stir","pan","pan-tool",out error);
        NameCheck(draft!=null && draft.DisplayName=="Carrot + Onion · Spatula · Stir",
            "Name example 4: other ingredients, tool and action");
        draft=ProductName(new[]{"rice"},"boil","pot","pot-tool",out error);
        NameCheck(draft!=null && draft.DisplayName=="Rice · Ladle · Boil",
            "Name example 5: single input with boil route");
        var area=new AreaConstraint("fixture-area",new[]{"cut"},new[]{"bowl"});
        var currentRecord=new ProcessingRecordView("fixture-record","product",new[]{"egg"},"cut","bowl",false);
        var currentWhitelist=new RecipeWhitelist("fixture-recipe",new[]{currentRecord.Id});
        var zone=new OperationZoneSession(currentWhitelist,area,new[]{currentRecord},new[]{"bowl"});
        if(!zone.TryAdd("fixture-recipe","fixture-area","egg").Accepted)
            throw new Exception("P2-C11 current operation fixture was rejected.");
        var session=zone.GetSnapshot();
        var knife=new ProcessingToolView("knife",new[]{"cut"});
        var otherInput=new ProcessingRecordView("unmatched","product",new[]{"tomato"},"cut","bowl",false);
        var unknown=new ProcessingMatcher(new RecipeWhitelist("fixture-recipe",new[]{otherInput.Id}),
            area,new[]{otherInput},"unknown")
            .Match("fixture-recipe","fixture-area",session.Inputs,knife,new[]{"bowl"});
        if(!unknown.ProducesUnknown || unknown.RejectionCode!=ProcessingRejectionCode.NoWhitelistedMatchInCurrentArea)
            throw new Exception("P2-C11 public unknown fixture was not produced.");
        NameCheck(Phase2ProductProduction.TryCreate(unknown,session,
            knife,"unknown",new FixtureNames(),out draft,out error) &&
            draft.IsUnknown && draft.DisplayName=="Unknown" && draft.SourceRecordId==null,
            "Name example 6: configured unknown uses only configured T4 name");
        var invalid=new ProcessingMatcher(currentWhitelist,area,new[]{currentRecord},"unknown")
            .Match("fixture-recipe","fixture-area",session.Inputs,knife,new string[0]);
        if(invalid.RejectionCode!=ProcessingRejectionCode.CarrierUnavailable)
            throw new Exception("P2-C11 public carrier rejection fixture was not produced.");
        NameCheck(!Phase2ProductProduction.TryCreate(invalid,session,
            knife,"unknown",new FixtureNames(),out draft,out error) &&
            draft==null && !string.IsNullOrEmpty(error),"Rejected carrier never becomes unknown");
        var otherRecord=new ProcessingRecordView("other","product",new[]{"egg"},"cut","bowl",false);
        var stale=new ProcessingMatcher(new RecipeWhitelist("fixture-recipe",new[]{otherRecord.Id}),
            area,new[]{otherRecord},"unknown")
            .Match("fixture-recipe","fixture-area",session.Inputs,knife,new[]{"bowl"});
        if(!stale.Matched || stale.Record.Id!="other")
            throw new Exception("P2-C11 public stale candidate fixture was not produced.");
        NameCheck(!Phase2ProductProduction.TryCreate(stale,session,
            knife,"unknown",new FixtureNames(),out draft,out error),
            "Matched record must belong to current C10 candidates");
        NameCheck(ProductName(new[]{"egg"},"cut","board","missing-tool",out error)==null &&
            error.Contains("Tool name"),"Missing resolved name is diagnostic, never raw ID fallback");
        return "PASS: P2-C11 "+ProductNameCases.Count+" name and rejection assertions.";
    }}
