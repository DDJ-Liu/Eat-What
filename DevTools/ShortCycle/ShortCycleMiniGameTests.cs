using System;
using System.Collections.Generic;
using System.IO;
using EatWhat.DataTables;
using System.Reflection;
using EatWhat.MiniGames;
using EatWhat.Cooking.ShortCycle;
using UnityEngine;

public static class ShortCycleMiniGameTests
{
    private static readonly List<string> cases = new List<string>();
    public static IList<string> Cases { get { return cases.AsReadOnly(); } }
    private static void Check(bool value, string message)
    { if (!value) throw new Exception("MG0: " + message); cases.Add(message); }
    private static void Field(object target, string name, object value)
    { target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value); }
    private static void Call(object target, string name)
    { target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null); }
    private sealed class ManualGame : IMiniGame
    {
        public Action<MiniGameResult> Callback;
        public Action DuringBegin;
        public MiniGameContext Context;
        public int Cancels, Teardowns;
        public void Begin(MiniGameContext context, Action<MiniGameResult> completed)
        { Context = context; Callback = completed; if (DuringBegin != null) DuringBegin(); }
        public void Cancel() { Cancels++; }
        public void Teardown() { Teardowns++; }
    }
    private sealed class Modal : IMiniGameModal
    {
        public int Opens, Closes;
        public bool TryOpen(out string diagnostic) { Opens++; diagnostic = null; return true; }
        public void Close() { Closes++; }
    }
    private sealed class OpeningModal : IMiniGameModal
    {
        public Action DuringOpen, DuringClose;
        public bool ReturnFalse, ThrowDuringOpen, Open;
        public int Opens, Closes;
        public bool TryOpen(out string diagnostic)
        {
            Opens++; Open = true;
            if (DuringOpen != null) DuringOpen();
            if (ReturnFalse || ThrowDuringOpen) Open = false;
            if (ThrowDuringOpen) throw new InvalidOperationException("Observed opening failure.");
            diagnostic = ReturnFalse ? "Declined." : null;
            return !ReturnFalse;
        }
        public void Close()
        {
            Closes++; Open = false;
            if (DuringClose != null) DuringClose();
        }
    }    private sealed class Names : IPhase2TextResolver
    {
        private readonly Dictionary<string,string> values = new Dictionary<string,string>();
        public void Add(string key, string value) { values[key] = value; }
        public bool TryResolve(string key, out string value) { return values.TryGetValue(key, out value); }
    }
    private static void Name(CK01GeneratedDataCatalog catalog, Names names, string table, string id)
    {
        string error;
        if (table == "Items") { CK01ItemData row; if (!catalog.TryGetItem(id, out row, out error)) throw new Exception(error); names.Add(row.name_key,id); }
        else if (table == "Tools") { CK01ToolData row; if (!catalog.TryGetTool(id, out row, out error)) throw new Exception(error); names.Add(row.name_key,id); }
        else { CK01ProcessActionData row; if (!catalog.TryGetRow("ProcessActions", id, out row, out error)) throw new Exception(error); names.Add(row.name_key,id); }
    }
    private static CK01GeneratedDataTable ActionTable(string root)
    {
        var csv=DataTableCsvParser.Parse(File.ReadAllText(Path.Combine(root,"DataTables/Cooking/ProcessActions.csv")));
        if(!csv.IsValid)throw new Exception("Current T11 CSV invalid");
        var rows=new List<CK01GeneratedDataRowReference>();
        foreach(var row in csv.Table.Rows)
        {
            var fields=new Dictionary<string,string>();
            for(var i=0;i<csv.Table.Headers.Count;i++)fields[csv.Table.Headers[i]]=row.Values[i];
            var asset=new CK01ProcessActionData { name=fields["act_id"],name_key=fields["name_key"],
                action_kind=fields["process_class"],process_template=fields["process_template"] };
            rows.Add(new CK01GeneratedDataRowReference(fields["act_id"],asset));
        }
        return new CK01GeneratedDataTable("ProcessActions",null,rows);
    }
    private static CK01GeneratedDataCatalog Catalog(string root)
    {
        var catalog=new CK01GeneratedDataCatalog();
        catalog.ReplaceTables(new[]{
            ShortCyclePhase2Tests.Table<CK01RecipeData>(root,"Recipes","recipe_id"),
            ShortCyclePhase2Tests.Table<CK01RecipeStepData>(root,"RecipeSteps","step_id"),
            ShortCyclePhase2Tests.Table<CK01ProcessingRecordData>(root,"ProcessingRecords","proc_id"),
            ShortCyclePhase2Tests.Table<CK01ToolData>(root,"Tools","tool_id"),ActionTable(root),
            ShortCyclePhase2Tests.Table<KitchenAreaData>(root,"KitchenAreas","id"),
            ShortCyclePhase2Tests.Table<CK01ItemData>(root,"Items","item_id"),
            ShortCyclePhase2Tests.Table<CK01CookingGameConfigData>(root,"CookingGameConfig","key")});
        return catalog;
    }
    private static void HostCases()
    {
        var modal = new Modal(); var host = new MiniGameHost(modal); var first = new ManualGame();
        var input = new MiniGameContext { RequestId="first", DurationSeconds=2f }; var completions=0;
        host.Start(first,input,_=>completions++); input.RequestId="changed"; input.DurationSeconds=80;
        Check(first.Context.RequestId=="first" && first.Context.DurationSeconds==2f && host.IsActive,"Host snapshots external context");
        bool rejected=false; try { host.Start(new ManualGame(),input,_=>{}); } catch(InvalidOperationException){rejected=true;}
        Check(rejected && modal.Opens==1,"Duplicate run rejected before opening modal");
        var stale=first.Callback; host.Cancel(); stale(new MiniGameResult{Quality=1}); host.Cancel();
        Check(first.Cancels==1 && modal.Closes==1 && completions==0 && !host.IsActive,"Cancel closes once and ignores late callback");
        var second=new ManualGame(); host.Start(second,new MiniGameContext{RequestId="second",DurationSeconds=0},_=>{
            completions++;host.Start(new ManualGame(),new MiniGameContext{RequestId="third",DurationSeconds=1},__=>{});
        });
        stale(new MiniGameResult{Quality=1});
        Check(host.ActiveRequestId=="second","Old generation cannot complete new run");
        second.Callback(new MiniGameResult{Quality=1}); second.Callback(new MiniGameResult{Quality=1});
        Check(completions==1 && host.ActiveRequestId=="third" && modal.Opens==3 && modal.Closes==2,"Completion closes before callback reentry and duplicate is ignored");
        host.Teardown(); host.Teardown();
        Check(!host.IsActive && host.IsTornDown && modal.Closes==3,"Teardown closes active run once");
        rejected=false; try{host.Start(new ManualGame(),new MiniGameContext{RequestId="late"},_=>{});}catch(InvalidOperationException){rejected=true;}
        Check(rejected,"Torn-down host never restarts");
        var timer=new CountdownStubMiniGame();var h2=new MiniGameHost(new Modal());float quality=-1;
        h2.Start(timer,new MiniGameContext{RequestId="zero",DurationSeconds=0},r=>quality=r.Quality);
        Check(quality<0 && timer.Tick(0) && quality==1f && !timer.Tick(1),"Zero duration completes on first explicit tick with quality one");
        rejected=false;try{timer.Tick(float.NaN);}catch(ArgumentOutOfRangeException){rejected=true;}
        Check(rejected,"Invalid delta rejected");
        h2.Teardown();timer.Teardown();
    }
    private static void ModalCases()
    {
        var stack=new ShortCycleInteractionLayerStack();var baseLayer=new MouseInteractionLayer();var modalLayer=new MouseInteractionLayer();
        Field(stack,"baseLayer",baseLayer);Call(stack,"Awake");
        var set=new ShortCycleModalSet();Field(set,"modalLayer",modalLayer);
        var adapter=new ShortCycleMiniGameModalAdapter(set,stack);string diagnostic;
        Check(adapter.TryOpen(out diagnostic) && adapter.IsOpen && stack.Top==modalLayer && stack.Count==2,"Real modal adapter pushes existing input layer");
        adapter.Close();adapter.Close();
        Check(!adapter.IsOpen && stack.Top==baseLayer && stack.Count==1,"Adapter removes modal layer once");
        var host=new MiniGameHost(adapter);var timer=new CountdownStubMiniGame();var calls=0;
        host.Start(timer,new MiniGameContext{RequestId="modal-complete",DurationSeconds=0},_=>calls++);
        Check(stack.Top==modalLayer && timer.Tick(0) && calls==1 && stack.Top==baseLayer,
            "Host and real modal adapter close layer on countdown completion");
        host.Start(timer,new MiniGameContext{RequestId="modal-cancel",DurationSeconds=4},_=>calls++);
        host.Cancel();
        Check(stack.Top==baseLayer && calls==1 && !timer.Tick(4),
            "Host and real modal adapter close layer on cancellation");
        host.Teardown();timer.Teardown();
    }
    private static void WaitingCases(string root)
    {
        var catalog=Catalog(root);string error;CK01ProcessActionData boil,whisk;
        var gotBoil=catalog.TryGetRow("ProcessActions","act_boil",out boil,out error);
        var boilError=error; var gotWhisk=catalog.TryGetRow("ProcessActions","act_whisk",out whisk,out error);
        Check(gotBoil && gotWhisk && boil.action_kind=="waiting" && whisk.action_kind=="regular",
            "Actual T11 kind maps waiting and regular: boil="+gotBoil+"/"+(boil==null?"null":boil.action_kind)+"/"+boilError+" whisk="+gotWhisk+"/"+(whisk==null?"null":whisk.action_kind)+"/"+error);
        RecipeWhitelist whitelist=null;IList<ProcessingRecordView> records=null;AreaConstraint area=null;
        Check(catalog.TryGetRecipeWhitelist("rcp_qingtang_noodle",out whitelist,out error) &&
            catalog.TryGetProcessingRecords(whitelist.ProcessingRecordIds,out records,out error) &&
            catalog.TryGetAreaConstraint("kitchen-stove",out area,out error),"Actual noodle T12 and T14 load");
        var names=new Names();foreach(var id in new[]{"ing_noodle_dried","stp_water","prd_unknown"})Name(catalog,names,"Items",id);
        Name(catalog,names,"Tools","tool_pot");Name(catalog,names,"Actions","act_boil");
        var inventory=new InventoryManager();Field(inventory,"dataCatalog",catalog);
        var entry=new Phase2SceneEntry();
        Check(entry.TryConsume(new ShortCycleDataHandoff("rcp_qingtang_noodle",new List<PrepZoneItem>{
            new PrepZoneItem{item_id="ing_noodle_dried",instance_id="seed",quantity=1}}),inventory,out error) &&
            entry.TryBindProduction(names,out error),"Actual P2 owner and step chain bind");
        var carriers=new[]{"tool_pot"};var zone=new OperationZoneSession(whitelist,area,records,carriers);
        Check(zone.TryAdd("rcp_qingtang_noodle","kitchen-stove","ing_noodle_dried").Accepted &&
            zone.TryAdd("rcp_qingtang_noodle","kitchen-stove","stp_water").Accepted,"Actual C10 holds boil inputs");
        var matcher=new ProcessingMatcher(whitelist,area,records,"prd_unknown");
        // U5 lacks this live capability; this explicit fixture only exercises the present T11/T12 route.
        var tool=new ProcessingToolView("tool_pot",new[]{"act_boil"});
        var adapter=new Modal();var host=new MiniGameHost(adapter);var timer=new CountdownStubMiniGame();
        var route=new ShortCycleWaitingProcess(entry,host,timer);var outcomes=new List<ShortCycleProcessOutcome>();
        var req=new ShortCycleProcessRequest{RequestId="wait-1",ActionId="act_boil",AreaId="kitchen-stove",
            ProductInstanceId="product-1",WaitingDurationSeconds=2,Zone=zone,Matcher=matcher,Tool=tool,CurrentAreaCarriers=carriers};
        Check(route.TryStart(req,outcomes.Add,out error) && host.IsActive && route.IsWaiting &&
            entry.PrepZoneContents.Count==1,"Waiting starts host without production");
        Check(!route.Tick(1) && outcomes.Count==0 && zone.GetSnapshot().HasInputs,"Partial tick does not produce");
        route.Cancel();Check(!host.IsActive && outcomes.Count==0 && entry.PrepZoneContents.Count==1 &&
            zone.GetSnapshot().HasInputs,"Cancelled wait preserves held inputs and inventory");
        req.RequestId="wait-2";
        Check(route.TryStart(req,outcomes.Add,out error) && route.Tick(2) && outcomes.Count==1 &&
            outcomes[0].Succeeded && outcomes[0].Quality==1f &&
            outcomes[0].Receipt.Product.item_id=="prd_noodle_boiled" &&
            outcomes[0].Receipt.Product.quality==1f && entry.PrepZoneContents.Count==2 &&
            !zone.GetSnapshot().HasInputs,"Completed wait commits one C11 product and receipt quality");
        Check(!route.Tick(1) && outcomes.Count==1 && entry.PrepZoneContents.Count==2,"Repeat tick cannot double produce");
        Check(outcomes[0].Receipt.Product.Clone().quality==1f && entry.PrepZoneContents[1].quality==null,
            "Quality clone remains transient to receipt; current Entry prep has no post-commit quality patch");
        route.Teardown();Check(route.IsTornDown && host.IsTornDown,"Route tears down host and timer");
        entry.Teardown();
    }
    private static void RegularCases(string root)
    {
        var catalog=Catalog(root);string error;RecipeWhitelist whitelist=null;
        IList<ProcessingRecordView> records=null;AreaConstraint area=null;ProcessingToolView tool=null;
        Check(catalog.TryGetRecipeWhitelist("rcp_tomato_egg",out whitelist,out error) &&
            catalog.TryGetProcessingRecords(whitelist.ProcessingRecordIds,out records,out error) &&
            catalog.TryGetAreaConstraint("kitchen-prep-counter",out area,out error) &&
            catalog.TryGetProcessingTool("tool_chopsticks",out tool,out error),
            "Actual regular recipe, T12, T14 and tool load");
        var names=new Names();foreach(var id in new[]{"ing_egg","prd_unknown"})Name(catalog,names,"Items",id);
        Name(catalog,names,"Tools","tool_chopsticks");Name(catalog,names,"Actions","act_whisk");
        var inventory=new InventoryManager();Field(inventory,"dataCatalog",catalog);
        var entry=new Phase2SceneEntry();
        Check(entry.TryConsume(new ShortCycleDataHandoff("rcp_tomato_egg",new List<PrepZoneItem>{
            new PrepZoneItem{item_id="ing_egg",instance_id="regular-seed",quantity=1}}),inventory,out error) &&
            entry.TryBindProduction(names,out error),"Regular P2 owner binds");
        var carriers=new[]{"tool_bowl","tool_board"};
        var zone=new OperationZoneSession(whitelist,area,records,carriers);
        Check(zone.TryAdd("rcp_tomato_egg","kitchen-prep-counter","ing_egg").Accepted,"Regular C10 holds egg");
        var modal=new Modal();var host=new MiniGameHost(modal);
        var route=new ShortCycleWaitingProcess(entry,host,new CountdownStubMiniGame());
        var matcher=new ProcessingMatcher(whitelist,area,records,"prd_unknown");
        var outcomes=new List<ShortCycleProcessOutcome>();
        var request=new ShortCycleProcessRequest{RequestId="regular-1",ActionId="act_whisk",
            AreaId="kitchen-prep-counter",ProductInstanceId="regular-product",Zone=zone,
            Matcher=matcher,Tool=tool,CurrentAreaCarriers=carriers};
        CK01ProcessActionData action;
        Check(catalog.TryGetRow("ProcessActions","act_whisk",out action,out error),"Current regular action resolves");
        action.action_kind="unexpected";
        Check(!route.TryStart(request,outcomes.Add,out error) && error.Contains("Unknown T11") &&
            outcomes.Count==0 && zone.GetSnapshot().HasInputs,"Unknown T11 kind rejects without write");
        action.action_kind="regular";
        request.ActionId="not-in-T11";
        Check(!route.TryStart(request,outcomes.Add,out error) && outcomes.Count==0 &&
            zone.GetSnapshot().HasInputs,"Missing T11 action rejects without write");
        request.ActionId="act_whisk";
        Check(route.TryStart(request,outcomes.Add,out error) && outcomes.Count==1 &&
            outcomes[0].Succeeded && outcomes[0].Quality==null &&
            outcomes[0].Receipt.Product.quality==null &&
            !route.IsWaiting && !host.IsActive && modal.Opens==0 && entry.PrepZoneContents.Count==2,
            "Regular T11 route commits directly without host or quality");
        Check(!route.TryStart(request,outcomes.Add,out error) && outcomes.Count==1 &&
            entry.PrepZoneContents.Count==2,"Cleared regular C10 cannot produce twice");
        route.Teardown();entry.Teardown();
    }
    public static string Run(string root)
    { cases.Clear();HostCases();ModalCases();WaitingCases(root);RegularCases(root);return "PASS: MG0 " + cases.Count + " assertions"; }
    public static IList<string> OpeningCases { get; private set; }
    private static void OpeningLifecycleCases()
    {
        var teardownModal=new OpeningModal();var teardownHost=new MiniGameHost(teardownModal);
        var teardownGame=new ManualGame();var completed=0;
        teardownModal.DuringOpen=teardownHost.Teardown;
        var disposed=false;
        try { teardownHost.Start(teardownGame,new MiniGameContext{RequestId="open-teardown"},_=>completed++); }
        catch(ObjectDisposedException){disposed=true;}
        Check(disposed && teardownHost.IsTornDown && !teardownHost.IsActive && !teardownModal.Open &&
            teardownModal.Closes==1 && teardownGame.Context==null && completed==0,
            "Teardown during successful opening closes owned modal and skips Begin");
        teardownHost.Teardown();teardownHost.Cancel();
        Check(teardownModal.Closes==1 && teardownGame.Teardowns==0,
            "Opening teardown is idempotent and never releases an unbegun game");
        var rejected=false;
        try{teardownHost.Start(new ManualGame(),new MiniGameContext{RequestId="after-teardown"},_=>{});}
        catch(InvalidOperationException){rejected=true;}
        Check(rejected,"Opening teardown permanently rejects later Start");

        var cancelModal=new OpeningModal();var cancelHost=new MiniGameHost(cancelModal);
        var cancelGame=new ManualGame();cancelModal.DuringOpen=cancelHost.Cancel;
        var cancelled=false;
        try{cancelHost.Start(cancelGame,new MiniGameContext{RequestId="open-cancel"},_=>completed++);}
        catch(OperationCanceledException){cancelled=true;}
        Check(cancelled && !cancelHost.IsActive && !cancelHost.IsTornDown && !cancelModal.Open &&
            cancelModal.Closes==1 && cancelGame.Context==null && completed==0,
            "Cancel during successful opening closes once and skips Begin");
        cancelModal.DuringOpen=null;
        var afterCancel=new ManualGame();
        cancelHost.Start(afterCancel,new MiniGameContext{RequestId="after-cancel"},_=>completed++);
        afterCancel.Callback(new MiniGameResult{Quality=1});
        Check(completed==1 && !cancelHost.IsActive && cancelModal.Closes==2,
            "Cancellation permits a new completed run without stale close");

        var declinedModal=new OpeningModal{ReturnFalse=true};
        var declinedHost=new MiniGameHost(declinedModal);declinedModal.DuringOpen=declinedHost.Cancel;
        var declinedGame=new ManualGame();cancelled=false;
        try{declinedHost.Start(declinedGame,new MiniGameContext{RequestId="open-declined"},_=>completed++);}
        catch(OperationCanceledException){cancelled=true;}
        Check(cancelled && !declinedHost.IsActive && !declinedModal.Open && declinedModal.Closes==0 &&
            declinedGame.Context==null,"Cancelled declined opening has no transferred modal or Begin");
        declinedModal.ReturnFalse=false;declinedModal.DuringOpen=null;
        declinedHost.Start(new ManualGame(),new MiniGameContext{RequestId="after-declined"},_=>{});
        declinedHost.Cancel();
        Check(declinedModal.Closes==1,"Host remains reusable after cancelled declined opening");

        var throwingModal=new OpeningModal{ThrowDuringOpen=true};
        var throwingHost=new MiniGameHost(throwingModal);throwingModal.DuringOpen=throwingHost.Cancel;
        var throwingGame=new ManualGame();var openingError=false;
        try{throwingHost.Start(throwingGame,new MiniGameContext{RequestId="open-error"},_=>completed++);}
        catch(InvalidOperationException ex){openingError=ex.Message.Contains("Observed opening failure");}
        Check(openingError && !throwingHost.IsActive && !throwingModal.Open && throwingModal.Closes==0 &&
            throwingGame.Context==null,"Opening exception propagates after adapter rollback and cancellation");
        throwingModal.ThrowDuringOpen=false;throwingModal.DuringOpen=null;
        throwingHost.Start(new ManualGame(),new MiniGameContext{RequestId="after-error"},_=>{});
        throwingHost.Cancel();
        Check(throwingModal.Closes==1,"Host remains reusable after opening exception");

        var beginModal=new OpeningModal();var beginHost=new MiniGameHost(beginModal);
        var beginGame=new ManualGame();beginGame.DuringBegin=beginHost.Cancel;var beginCompletions=0;
        beginHost.Start(beginGame,new MiniGameContext{RequestId="begin-cancel"},_=>beginCompletions++);
        beginGame.Callback(new MiniGameResult{Quality=1});
        Check(!beginHost.IsActive && !beginModal.Open && beginModal.Closes==1 &&
            beginGame.Cancels==1 && beginCompletions==0,
            "Cancel during Begin releases started game and ignores late callback");
        var beginNext=new ManualGame();beginHost.Start(beginNext,new MiniGameContext{RequestId="begin-next"},_=>beginCompletions++);
        beginNext.Callback(new MiniGameResult{Quality=1});
        Check(beginCompletions==1 && beginModal.Closes==2,"Begin cancellation permits later valid run");

        var destroyModal=new OpeningModal();var destroyHost=new MiniGameHost(destroyModal);
        var destroyGame=new ManualGame();destroyGame.DuringBegin=destroyHost.Teardown;
        destroyHost.Start(destroyGame,new MiniGameContext{RequestId="begin-destroy"},_=>beginCompletions++);
        destroyGame.Callback(new MiniGameResult{Quality=1});
        Check(destroyHost.IsTornDown && !destroyHost.IsActive && destroyModal.Closes==1 &&
            destroyGame.Teardowns==1 && beginCompletions==1,
            "Teardown during Begin releases game and ignores late callback");

        var closeModal=new OpeningModal();var closeHost=new MiniGameHost(closeModal);
        var closeGame=new ManualGame();var closeCompletions=0;
        closeModal.DuringClose=closeHost.Cancel;
        closeHost.Start(closeGame,new MiniGameContext{RequestId="close-cancel"},_=>closeCompletions++);
        closeGame.Callback(new MiniGameResult{Quality=1});
        Check(closeCompletions==0 && !closeHost.IsActive && closeModal.Closes==1,
            "Cancel during completion close suppresses success without double close");
        closeModal.DuringClose=null;
        var closeNext=new ManualGame();closeHost.Start(closeNext,new MiniGameContext{RequestId="close-next"},_=>closeCompletions++);
        closeNext.Callback(new MiniGameResult{Quality=1});
        Check(closeCompletions==1 && closeModal.Closes==2,
            "New completion is not closed by old settlement");
    }
    private static void AdditionalOpeningLifecycleCases()
    {
        var declined=new OpeningModal{ReturnFalse=true};
        var host=new MiniGameHost(declined);declined.DuringOpen=host.Teardown;
        var game=new ManualGame();var disposed=false;
        try{host.Start(game,new MiniGameContext{RequestId="declined-teardown"},_=>{});}
        catch(ObjectDisposedException){disposed=true;}
        Check(disposed && host.IsTornDown && !host.IsActive && !declined.Open &&
            declined.Closes==0 && game.Context==null,
            "Teardown during declined opening leaves no transferred modal or game");

        var throwing=new OpeningModal{ThrowDuringOpen=true};
        var throwingHost=new MiniGameHost(throwing);throwing.DuringOpen=throwingHost.Teardown;
        var throwingGame=new ManualGame();var originalError=false;
        try{throwingHost.Start(throwingGame,new MiniGameContext{RequestId="throw-teardown"},_=>{});}
        catch(InvalidOperationException ex){originalError=ex.Message.Contains("Observed opening failure");}
        Check(originalError && throwingHost.IsTornDown && !throwingHost.IsActive &&
            !throwing.Open && throwing.Closes==0 && throwingGame.Context==null,
            "Opening exception after teardown propagates original and leaves no run");

        var closing=new OpeningModal();var closingHost=new MiniGameHost(closing);
        closing.DuringOpen=closingHost.Cancel;
        closing.DuringClose=()=>{throw new InvalidOperationException("Observed close failure.");};
        var closeError=false;
        try{closingHost.Start(new ManualGame(),new MiniGameContext{RequestId="close-error"},_=>{});}
        catch(AggregateException ex){closeError=ex.InnerExceptions.Count==2 &&
            ex.InnerExceptions[0] is OperationCanceledException &&
            ex.InnerExceptions[1].Message.Contains("Observed close failure");}
        Check(closeError && !closingHost.IsActive && !closing.Open && closing.Closes==1,
            "Opening cancellation and close observer errors both propagate after state cleanup");

        var beginModal=new OpeningModal();var beginHost=new MiniGameHost(beginModal);
        var beginGame=new ManualGame();beginGame.DuringBegin=()=>{throw new InvalidOperationException("Observed Begin failure.");};
        var beganError=false;var completed=0;
        try{beginHost.Start(beginGame,new MiniGameContext{RequestId="begin-error"},_=>completed++);}
        catch(InvalidOperationException ex){beganError=ex.Message.Contains("Observed Begin failure");}
        beginGame.Callback(new MiniGameResult{Quality=1});
        Check(beganError && beginGame.Cancels==1 && beginModal.Closes==1 &&
            !beginHost.IsActive && completed==0,
            "Begin observer exception propagates after cancelling started resources");

        var settlementModal=new OpeningModal();var settlementHost=new MiniGameHost(settlementModal);
        var settlementGame=new ManualGame();var callbacks=0;
        settlementModal.DuringClose=settlementHost.Teardown;
        settlementHost.Start(settlementGame,new MiniGameContext{RequestId="settlement-teardown"},_=>callbacks++);
        settlementGame.Callback(new MiniGameResult{Quality=1});
        Check(settlementHost.IsTornDown && !settlementHost.IsActive &&
            settlementModal.Closes==1 && callbacks==0,
            "Teardown during completion close suppresses success and does not double close");
    }    public static string RunOpeningLifecycle()
    {
        cases.Clear();OpeningLifecycleCases();AdditionalOpeningLifecycleCases();
        OpeningCases=new List<string>(cases).AsReadOnly();
        return "PASS: MG0 opening " + cases.Count + " assertions";
    }
}
