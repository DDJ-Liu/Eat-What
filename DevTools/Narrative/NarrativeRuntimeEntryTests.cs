using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using EatWhat.Narrative.Core;
using EatWhat.Narrative.Unity;
using UnityEngine;

public static class NarrativeRuntimeEntryTests
{
    static int checks;
    static void Check(bool value,string name) { checks++; if(!value) throw new Exception("RuntimeEntry: "+name); }
    static CK01GeneratedDataCatalog Catalog(bool rows=true)
    {
        var tables=new List<CK01GeneratedDataTable>();
        foreach(string name in new[]{"Characters","Dialogues","DialogueLines","CGs","Scenes","SceneStates","SceneStatePlacements","AmbientPools"})
        {
            var list=new List<CK01GeneratedDataRowReference>();
            if(rows && name=="Dialogues") list.Add(new CK01GeneratedDataRowReference("dlg_runtime",new CK01DialogueData {dialogue_id="dlg_runtime",tier="normal",first_line="ln_runtime"}));
            if(rows && name=="DialogueLines") list.Add(new CK01GeneratedDataRowReference("ln_runtime",new CK01DialogueLineData {line_id="ln_runtime",dialogue_id="dlg_runtime"}));
            tables.Add(new CK01GeneratedDataTable(name,null,list));
        }
        var result=new CK01GeneratedDataCatalog();result.ReplaceTables(tables);return result;
    }
    sealed class FailingSource : ICatalogSource
    {
        public int Reads;
        public string Kind => "SyntheticReadFailure";
        public IList<CK01GeneratedDataTable> Tables {get {Reads++;throw new IOException("synthetic catalog read failed");}}
    }
    sealed class FailingKindSource : ICatalogSource
    {
        public string Kind {get {throw new IOException("synthetic kind read failed");}}
        public IList<CK01GeneratedDataTable> Tables => new List<CK01GeneratedDataTable>();
    }
    static void Event(NarrativeRuntimeEntry entry,string name)
    { typeof(NarrativeRuntimeEntry).GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(entry,null); }
    static int Subscribers(DialoguePresentationConfig config)
    {
        var value=(Action)typeof(DialoguePresentationConfig).GetField("Changed",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(config);
        return value==null ? 0 : value.GetInvocationList().Length;
    }
    public static string Run()
    {
        checks=0;
        var config=new DialoguePresentationConfig();var catalog=Catalog();var entry=new NarrativeRuntimeEntry();
        entry.Configure(config,catalog);
        INarrativeDataDirectory directory=null,again=null;string diagnostic=null;DialogueTriggerResolution trigger=null;DialoguePresentationResolution presentation=null;
        Check(entry.IsAvailable&&entry.TryGetDirectory(out directory),"valid config/catalog available");
        Check(entry.SourceKind=="ScriptableObject"&&entry.Diagnostics.Count==0,"actual catalog SO policy retained");
        Check(entry.TryGetDirectory(out again)&&ReferenceEquals(directory,again),"immutable directory cache reused");
        NarrativeDialogue dialogue;
        Check(directory.TryGetDialogue("dlg_runtime",out dialogue,out diagnostic)&&dialogue.FirstLine=="ln_runtime","actual adapter row mapping");
        Check(entry.TryResolveTrigger(null,out trigger,out diagnostic)&&diagnostic==null&&trigger.Snapshot.TemplateName=="default","original trigger default resolver");
        Check(trigger.Snapshot.Get<float>("interactRadius")==config.Data.trigger[0].interactRadius,"trigger actual config value");
        config.Data.trigger=new[]{config.Data.trigger[0],new DialogueTriggerConfig {name="nearby",interactRadius=5,autoBubbleRadius=7}};
        Check(entry.Initialize()&&entry.TryResolveTrigger("nearby",out trigger,out diagnostic),"named caller trigger");
        Check(trigger.Snapshot.TemplateName=="nearby"&&trigger.Snapshot.Get<float>("interactRadius")==5,"named template snapshot");
        Check(entry.TryResolveDialogue("dlg_runtime",out presentation,out diagnostic,"nearby")&&presentation.Snapshot.GetGroup("trigger").TemplateName=="nearby","dialogue uses original resolver and independent caller trigger");
        Check(presentation.Snapshot.DialogueId=="dlg_runtime"&&presentation.Snapshot.Parameters.Count==69,"actual resolver full snapshot");
        Check(!entry.TryResolveDialogue("missing",out presentation,out diagnostic)&&presentation==null&&!string.IsNullOrEmpty(diagnostic)&&entry.IsAvailable,"missing query returns diagnostic without invalidating whole source");
        Check(Subscribers(config)==1,"exactly one config subscription after explicit reload");
        config.Data.trigger[1].interactRadius=8;
        // Retained JsonUtility.ToJson returns {}; explicitly deliver the config's existing event boundary.
        var changed=(Action)typeof(DialoguePresentationConfig).GetField("Changed",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(config);changed();
        Check(entry.TryResolveTrigger("nearby",out trigger,out diagnostic)&&trigger.Snapshot.Get<float>("interactRadius")==8,"config change callback invalidates derived cache");
        Check(Subscribers(config)==1,"reload does not duplicate event handler");
        var other=new DialoguePresentationConfig();entry.Configure(other,catalog);
        Check(Subscribers(config)==0&&entry.IsAvailable&&Subscribers(other)==1,"rebind unsubscribes old asset");
        var list=entry.Diagnostics as IList<string>;bool denied=false;
        try{list.Add("forged");}catch(NotSupportedException){denied=true;}
        Check(denied,"diagnostics are read only");
        entry.Configure(null,catalog);
        Check(!entry.IsAvailable&&!entry.TryGetDirectory(out directory)&&directory==null,"missing config unavailable no directory leak");
        Check(entry.Diagnostics.Count==1&&entry.Diagnostics[0].Contains("MISSING_CONFIG"),"missing config precise diagnostic");
        Check(!entry.TryResolveTrigger(null,out trigger,out diagnostic)&&trigger==null&&diagnostic.Contains("MISSING_CONFIG"),"missing config no invented trigger defaults");
        entry.Configure(config,null);
        Check(!entry.IsAvailable&&entry.Diagnostics[0].Contains("MISSING_CATALOG"),"missing catalog unavailable");
        Check(!entry.TryResolveDialogue("dlg_runtime",out presentation,out diagnostic)&&presentation==null,"missing data no presentation");
        entry.Configure(null,null);
        Check(!entry.IsAvailable&&entry.Diagnostics.Count==2,"both missing references retained");
        var absent=new CK01GeneratedDataCatalog();entry.Configure(config,absent);
        Check(!entry.IsAvailable&&entry.Diagnostics.Count>=1&&entry.Diagnostics[0].StartsWith("DATA:"),"missing real tables fail through adapter");
        var empty=Catalog(false);entry.Configure(config,empty);
        Check(entry.IsAvailable&&entry.TryGetDirectory(out directory),"eight valid empty tables remain available");
        Check(!directory.TryGetDialogue("dlg_runtime",out dialogue,out diagnostic),"empty tables do not invent dialogue");
        var badConfig=new DialoguePresentationConfig();badConfig.Data.trigger=new DialogueTriggerConfig[0];entry.Configure(badConfig,catalog);
        Check(!entry.IsAvailable&&entry.Diagnostics[0].StartsWith("CONFIG:"),"invalid actual config rejected no repair");
        Check(badConfig.Data.trigger.Length==0,"configuration was not clamped or replaced");
        var failed=new CK01GeneratedDataCatalog();var source=new FailingSource();failed.InstallSourceForValidation(source);entry.Configure(config,failed);
        Check(!entry.IsAvailable&&entry.SourceKind=="SyntheticReadFailure","read failure does not select another data source");
        Check(entry.Diagnostics.Count==8&&entry.Diagnostics[0].StartsWith("DATA:")&&entry.Diagnostics[0].Contains("Catalog source rejected: synthetic catalog read failed"),"catalog-normalized failures retained for all eight adapter tables");
        Check(!entry.TryGetDirectory(out directory)&&directory==null&&source.Reads==1,"failed load cached, no retry loop or stale directory");
        var failedKind=new CK01GeneratedDataCatalog();failedKind.InstallSourceForValidation(new FailingKindSource());entry.Configure(config,failedKind);
        Check(!entry.IsAvailable&&entry.Diagnostics.Count==1&&entry.Diagnostics[0].Contains("READ_FAILED: IOException: synthetic kind read failed"),"uncaught external source exception converted to unavailable");
        Check(!entry.TryGetDirectory(out directory)&&directory==null,"exception path cannot leak previous directory");
        var invalid=Catalog();IList<CK01DialogueData> rows;string issue;
        invalid.TryGetRows("Dialogues",out rows,out issue);rows[0].tier="unknown";entry.Configure(config,invalid);
        Check(!entry.IsAvailable&&entry.Diagnostics[0].Contains("Unknown dialogue tier"),"actual adapter format error remains unavailable");
        entry.Configure(config,catalog);Check(entry.IsAvailable,"explicit valid rebind recovers");
        config.ProofNativeAlive=false;
        Check(!entry.IsAvailable&&entry.Diagnostics[0].Contains("MISSING_CONFIG"),"destroyed Unity config cannot retain stale availability");config.ProofNativeAlive=true;
        entry.Configure(config,catalog);Check(entry.IsAvailable,"restored reference explicit refresh");
        entry.isActiveAndEnabled=false;Event(entry,"OnDisable");
        Check(!entry.IsAvailable&&!entry.TryGetDirectory(out directory),"disabled component unavailable");
        entry.isActiveAndEnabled=true;Event(entry,"OnEnable");Check(entry.IsAvailable,"reenable rebuilds references");
        Event(entry,"OnDestroy");Check(Subscribers(config)==0,"destroy unsubscribes config callback");
        return "{\"passed\":true,\"checks\":"+checks+",\"actualComponentAdapterResolver\":true,\"nativeUnity\":false,\"sourcePolicyChanged\":false,\"jsonNotifyLimitation\":\"original JsonUtility stub returns {}; existing Changed delegate delivered explicitly\"}";
    }
}
