using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using EatWhat.DataTables;
using EatWhat.Narrative.Core;
using EatWhat.Narrative.Unity;
using UnityEngine;

// One fixture-specific headless use of production import, adapter, resolver and FSM.
// The retained StaticCompile host supplies UnityStubs; this never persists a Unity asset.
public static class NarrativeHeadlessChainTests
{
    static int checks;
    static void Check(bool condition,string reason) { checks++;if(!condition)throw new Exception(reason); }
    static string Hash(byte[] bytes) { using(var sha=SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-",""); }
    static string F(double value) {return value.ToString("0.000",CultureInfo.InvariantCulture);}
    static string B(bool value) {return value?"true":"false";}
    static readonly BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Static;
    static void Set(object value,string field,object data){value.GetType().GetField(field).SetValue(value,data);}
    sealed class Clock : INarrativeDialogueClock {public double Value;public double NowSeconds {get{return Value;}}}
    sealed class Trace
    {
        public Clock Clock=new Clock();
        public StringBuilder Text=new StringBuilder("time|event|dialogue|line|speaker|text_key|detail\n");
        public void Add(string name,string dialogue="-",string line="-",string speaker="-",string text="-",string detail="-")
        {Text.Append(Clock.Value.ToString("000.000",CultureInfo.InvariantCulture)).Append('|').Append(name).Append('|').Append(dialogue).Append('|').Append(line).Append('|').Append(speaker.Length==0?"-":speaker).Append('|').Append(text).Append('|').Append(detail).Append('\n');}
        public void C(string name,NarrativeDialogueContext context,string detail="-"){Add(name,context.Dialogue.DialogueId,detail:detail);}
        public void L(string name,NarrativeLinePresentation line,string detail="-"){Add(name,line.Context.Dialogue.DialogueId,line.Line.LineId,line.SpeakerId,line.TextKey,detail);}
    }
    sealed class Capabilities : INarrativeDialogueView,INarrativeDialogueLock,INarrativeTalkedRecord
    {
        public Trace Log;public bool Locked;public HashSet<string> Talked=new HashSet<string>(StringComparer.Ordinal);
        public void Acquire(string id,double now){Check(!Locked,"fake operation lock cannot overlap");Locked=true;Log.Add("lock-acquire",id);}
        public void Release(string id,double now){Check(Locked,"fake operation lock was held");Locked=false;Log.Add("lock-release",id);}
        public void MarkTalked(string id){Check(Talked.Add(id),"per-dialogue record exactly once");Log.Add("talked",id);}
        public void DialogueStarted(NarrativeDialogueContext c,double now){Log.C("dialogue-start",c,"tier="+c.Dialogue.Level);}
        public void SetLetterbox(NarrativeDialogueContext c,bool visible,double now){Log.C(visible?"bars-on":"bars-off",c);}
        public void PrepareStoryCamera(NarrativeDialogueContext c,double now){Log.C("prepare-camera",c);}
        public void Enter(NarrativeDialogueContext c,double now){Log.C("enter",c,"cast="+c.Cast.Count);}
        public void CompleteEntrance(NarrativeDialogueContext c,double now){Log.C("complete-entrance",c);}
        public void UpdateCast(NarrativeDialogueContext c,double now){Log.C("update-cast",c);}
        public void SetSpeaker(NarrativeLinePresentation line,double now){Log.L("speaker",line,"name="+(line.NameKey.Length==0?"-":line.NameKey)+";nameBox="+B(line.ShowNameBox)+";allDim="+B(line.AllPortraitsDim));}
        public void ReactToLine(NarrativeLinePresentation line,double now){Log.L("react",line);}
        public void StartText(NarrativeLinePresentation line,double now){Log.L("text-start",line);}
        public void CompleteText(NarrativeLinePresentation line,double now){Log.L("complete-text-request",line);}
        public void CanEnd(NarrativeDialogueContext c,double now){Log.C("can-end",c);}
        public void Exit(NarrativeDialogueContext c,double now){Log.C("exit",c);}
        public void DialogueEnded(NarrativeDialogueContext c,double now){Log.C("dialogue-end",c);}
    }
    sealed class Handler : INarrativeCommandHandler
    {
        public NarrativeCommandInvocation Command;public NarrativeLinePresentation Line;public Trace Log;
        public double Duration;public Action BeginEffect;public bool Begun,Completed;public double Start;
        public void Begin(double now){Begun=true;Start=now;Log.L("command-begin",Line,Command.RawText);if(BeginEffect!=null)BeginEffect();}
        public bool IsComplete {get {if(!Begun||Log.Clock.Value<Start+Duration)return false;if(!Completed){Completed=true;Log.L("command-complete",Line,Command.RawText);}return true;}}
        public void CompleteImmediately(NarrativeCommandTruncation why,double now){Completed=true;Log.L("command-truncated",Line,Command.RawText+";"+why);}
        public void Cancel(double now){Completed=true;Log.L("command-cancelled",Line,Command.RawText);}
    }
    public static string Run(string root,string output)
    {
        checks=0;CultureInfo.CurrentCulture=CultureInfo.InvariantCulture;CultureInfo.CurrentUICulture=CultureInfo.InvariantCulture;
        Directory.CreateDirectory(output);
        string fixture=Path.Combine(root,"DevTools/Narrative/Fixtures/HeadlessChainV25");
        var contextInput=JObject.Parse(File.ReadAllText(Path.Combine(fixture,"context.json")));
        var config=JsonConvert.DeserializeObject<DialoguePresentationData>(File.ReadAllText(Path.Combine(fixture,"config.json")));
        var plan=JObject.Parse(File.ReadAllText(Path.Combine(fixture,"actions.json")));
        Check(((string)contextInput["source"]).StartsWith("Synthetic",StringComparison.Ordinal),"file inventory is explicitly synthetic");
        var sources=new Dictionary<string,byte[]>();var parsed=new Dictionary<string,DataTableCsvParseResult>();var schemas=new Dictionary<string,DataTableSchema>();
        var loadedType=typeof(DataTableImportService).GetNestedType("LoadedTable",BindingFlags.NonPublic);
        var loaded=(IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(loadedType));var loadedByName=new Dictionary<string,object>();
        foreach(var entry in contextInput["tables"])
        {
            string name=(string)entry["table"],relative=(string)entry["csv"],schemaPath=(string)entry["schema"];
            var bytes=File.ReadAllBytes(Path.Combine(fixture,relative));sources.Add(relative,bytes);
            var csv=DataTableCsvParser.Parse(new UTF8Encoding(false,true).GetString(bytes));parsed.Add(name,csv);
            var schema=JsonConvert.DeserializeObject<DataTableSchema>(File.ReadAllText(Path.Combine(root,"DataTables",schemaPath)));schemas.Add(name,schema);
            Check(csv.Table.Rows.Count==(int)entry["rows"],"independent declared CSV row count "+name);
            var item=Activator.CreateInstance(loadedType);Set(item,"SchemaPath",schemaPath);Set(item,"Schema",schema);Set(item,"ParseResult",csv);loaded.Add(item);loadedByName.Add(name,item);
        }
        var importReport=new DataTableImportService.DataTableImportReport();
        var validationContext=(DataTableValidationContext)typeof(DataTableImportService).GetMethod("BuildValidationContext",Private).Invoke(null,new object[]{loaded,importReport});
        NarrativeTableValidation.Enable(validationContext,config,(role,name)=>((JArray)contextInput["files"][role]).Values<string>().ToList());
        int errors=0,warnings=0,infos=0;
        var validations=new Dictionary<string,DataTableValidationResult>();
        foreach(var entry in contextInput["tables"])
        {
            string name=(string)entry["table"];var result=DataTableValidator.Validate(schemas[name],parsed[name],validationContext);validations.Add(name,result);Set(loadedByName[name],"Validation",result);
            errors+=result.Errors.Count;warnings+=result.Warnings.Count;infos+=result.Infos.Count;
        }
        Check(importReport.errors.Count==0&&errors==0&&warnings==0&&infos==0,"same original CSV bytes and explicit N009 context must have no data or narrative diagnostics");
        File.WriteAllText(Path.Combine(output,"fixture-validation.json"),JsonConvert.SerializeObject(new{errors,warnings,infos,schemas=validations.Select(x=>new{table=x.Key,errors=x.Value.Errors,warnings=x.Value.Warnings,infos=x.Value.Infos}),contextSource=(string)contextInput["source"],legacyScenePipelineExecuted=false,sceneContentVerified=false},Formatting.Indented));
        var log=new Trace();log.Add("validation",detail:"dataErrors="+errors+";warnings="+warnings+";infos="+infos+";context=synthetic");
        var tables=new List<CK01GeneratedDataTable>();int runtimeRows=0,filenameWarnings=0;
        var apply=typeof(DataTableImportService).GetMethod("TryApplyRow",Private);
        foreach(var entry in contextInput["tables"])
        {
            string name=(string)entry["table"];var schema=schemas[name];var csv=parsed[name].Table;var rows=new List<CK01GeneratedDataRowReference>();ScriptableObject aggregate=null;int rowWarnings=0;
            if(CK01ImportRegistry.Find(name).IsAggregateAsset)
            {
                aggregate=new LocTableAsset();var args=new object[]{aggregate,loadedByName[name],false,null};
                Check((bool)typeof(DataTableImportService).GetMethod("TryApplyAggregateAsset",Private).Invoke(null,args),"actual Localization aggregate import: "+args[3]);
            }
            else foreach(var row in csv.Rows)
            {
                var asset=(ScriptableObject)Activator.CreateInstance(typeof(CK01CharacterData).Assembly.GetType(schema.targetType));
                var args=new object[]{asset,schema,row,csv,false,null,null};Check((bool)apply.Invoke(null,args),"actual production row conversion: "+args[5]);
                rowWarnings+=((List<string>)args[6]).Count;rows.Add(new CK01GeneratedDataRowReference(DataTableValidator.GetRowIdentity(schema,row,csv,false),asset));runtimeRows++;
            }
            filenameWarnings+=rowWarnings;tables.Add(new CK01GeneratedDataTable(name,aggregate,rows));
            log.Add("import",detail:name+";rows="+csv.Rows.Count+";staticHostFilenameWarnings="+rowWarnings);
        }
        var catalog=new CK01GeneratedDataCatalog();catalog.ReplaceTables(tables);INarrativeDataDirectory directory;IReadOnlyList<NarrativeDataDiagnostic> adapterIssues;
        Check(NarrativeDataAdapter.TryCreate(catalog,out directory,out adapterIssues),"actual generated catalog -> actual adapter -> actual directory");
        Check(runtimeRows==22&&adapterIssues.Count==0,"independent runtime row count and adapter diagnostics");log.Add("adapt",detail:"runtimeRows="+runtimeRows+";modelKinds=8;diagnostics="+adapterIssues.Count);
        NarrativeAmbientPool pool;NarrativeDialogue ambient=null;NarrativeLineChain ambientChain;string issue;
        Check(directory.TryGetAmbientPool("amb_plaza",out pool,out issue)&&directory.TryGetDialogue("dlg_ambient",out ambient,out issue),"ambient pool and dialogue actually imported");
        Check(directory.TryGetLineChain("dlg_ambient",out ambientChain,out issue)&&ambientChain.Lines.Count==1,"ambient line actually imported");
        Check(pool.DialogueIds.Single()==ambient.DialogueId&&ambient.Level==NarrativeDialogueLevel.Ambient,"actual ambient pool reference/tier");
        log.Add("ambient-data",ambient.DialogueId,ambientChain.Lines[0].LineId,ambientChain.Lines[0].Speaker,ambientChain.Lines[0].TextKey,"pool="+pool.PoolId+";fsm=not-run");
        var caps=new Capabilities{Log=log};var input=new NarrativeAdvanceInputModel();NarrativeDialogueStateMachine machine=null;
        var handlers=new List<Handler>();var flags=new Dictionary<string,string>();string sceneState="-";
        var scheduler=new NarrativeCommandScheduler(command=>
        {
            var handler=new Handler{Command=command,Line=machine.CurrentLine,Log=log,Duration=((JArray)plan["instantCommands"]).Values<string>().Contains(command.Name)?0:(double)plan["handlerDurationSeconds"]};handlers.Add(handler);
            handler.BeginEffect=()=>
            {
                if(command.Name=="set_flag") flags[command.Arguments[0].Value]=command.Arguments[1].Value;
                if(command.Name=="cg_show")
                {
                    NarrativeCg cg;Check(directory.TryGetCg(command.Arguments[0].Value,out cg,out issue),"actual imported CG lookup");
                    var resolved=DialoguePresentationResolver.ResolveCg(machine.Current.Snapshot,cg);Check(resolved.Success,"actual CG resolver");
                    log.L("cg-snapshot",handler.Line,"id="+resolved.Snapshot.CgId+";fadeIn="+F(resolved.Snapshot.FadeIn)+";keepBox="+B(resolved.Snapshot.KeepBox));input.NotifyCgChanged(log.Clock.Value);
                }
                if(command.Name=="cg_hide")input.NotifyCgChanged(log.Clock.Value);
                if(command.Name=="set_scene_state")
                {
                    NarrativeSceneState state;Check(directory.TryGetSceneState(command.Arguments[0].Value,command.Arguments[1].Value,out state,out issue),"state command uses full imported key");
                    sceneState=state.SceneId+":"+state.StateId;log.L("scene-state",handler.Line,sceneState);
                }
            };
            return handler;
        });
        machine=new NarrativeDialogueStateMachine(directory,dialogue=>
        {
            var resolution=DialoguePresentationResolver.Resolve(config,dialogue);Check(resolution.Success&&resolution.ConfigurationDiagnostics.Count==0&&resolution.OverrideDiagnostics.Count==0,"actual N008 snapshot with same config object");var s=resolution.Snapshot;
            log.Add("snapshot",dialogue.DialogueId,detail:"mode="+s.Get<NarrativeCommandExecutionMode>("tier.commandExecutionMode")+";manual="+F(s.Get<float>("text.advanceCooldown"))+";auto="+F(s.Get<float>("text.autoWait"))+";cg="+F(s.Get<float>("cg.cgAdvanceCooldown")));return resolution;
        },caps,caps,log.Clock,caps,scheduler,input);
        foreach(var action in plan["actions"])
        {
            double time=(double)action["time"];Check(time>=log.Clock.Value,"monotonic independent action plan");log.Clock.Value=time;string value=(string)action["value"];
            switch((string)action["action"])
            {
                case "start":log.Add("driver-start",value);Check(machine.Start(value)==NarrativeDialogueStartStatus.Started,"planned start");break;
                case "enter":log.C("ack-enter",machine.Current);Check(machine.NotifyEntranceCompleted(machine.Current),"view completion only after request returns");break;
                case "text":log.L("ack-text",machine.CurrentLine);Check(machine.NotifyLineCompleted(machine.CurrentLine),"view text completion");break;
                case "click":log.L("input-click",machine.CurrentLine);Check(machine.Click().ToString()==value,"actual input intent at planned click");break;
                case "tick":log.L("driver-tick",machine.CurrentLine);scheduler.Tick(time);machine.Tick();break;
                case "bars":log.C("ack-bars",machine.Current);Check(machine.NotifyStoryBarsReady(machine.Current),"story bars readiness");break;
                case "camera":log.C("ack-camera",machine.Current);Check(machine.NotifyStoryCameraReady(machine.Current),"story camera readiness");break;
                case "exit":log.C("ack-exit",machine.Current);Check(machine.NotifyExitCompleted(machine.Current),"view exit completion");break;
                case "finish":scheduler.Tick(time);machine.Tick();log.Add("end-state",detail:"phase="+machine.Phase+";batches="+scheduler.ActiveBatchCount+";talked="+caps.Talked.Count+";scene="+sceneState);break;
                default:throw new Exception("Unknown fixture action");
            }
        }
        Check(machine.Phase==NarrativeDialoguePhase.Inactive&&machine.Diagnostics.Count==0&&scheduler.ActiveBatchCount==0&&!caps.Locked,"whole chain ended without production diagnostics or retained work/lock");
        Check(caps.Talked.SetEquals(new[]{"dlg_normal","dlg_story","dlg_after"})&&handlers.Count==7&&handlers.All(x=>x.Completed)&&flags["greeted"]=="true"&&sceneState=="scn_plaza:evening","actual fake capability effects and exact records");
        foreach(var pair in sources)Check(pair.Value.SequenceEqual(File.ReadAllBytes(Path.Combine(fixture,pair.Key))),"same CSV source bytes survived validation/import/chain "+pair.Key);
        byte[] actual=new UTF8Encoding(false).GetBytes(log.Text.ToString()),expected=File.ReadAllBytes(Path.Combine(fixture,"expected-trace.txt"));
        File.WriteAllBytes(Path.Combine(output,"actual-trace.txt"),actual);
        var comparison=new{expectedSha256=Hash(expected),actualSha256=Hash(actual),expectedLines=expected.Count(x=>x==(byte)'\n'),actualLines=actual.Count(x=>x==(byte)'\n'),byteEqual=expected.SequenceEqual(actual),utf8NoBom=expected.Length<3||expected[0]!=239||expected[1]!=187||expected[2]!=191,lfOnly=!expected.Contains((byte)'\r'),endsWithLf=expected.Length>0&&expected.Last()==10};
        File.WriteAllText(Path.Combine(output,"trace-comparison.json"),JsonConvert.SerializeObject(comparison,Formatting.Indented));
        Check(comparison.utf8NoBom&&comparison.lfOnly&&comparison.endsWithLf&&comparison.byteEqual,"exact hand-written baseline bytes (no sorting, normalization, or actual-to-expected update)");
        return JsonConvert.SerializeObject(new{passed=true,checks,fixtureTables=9,runtimeRows,ambientImportedOnly=true,filenameWarnings,validationErrors=errors,validationWarnings=warnings,validationInfos=infos,comparison,nativeUnity=false,legacyScenePipelineExecuted=false});
    }
}
