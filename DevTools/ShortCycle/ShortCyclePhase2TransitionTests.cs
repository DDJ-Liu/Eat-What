using System;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using System.IO;
using System.Reflection;
using EatWhat.Cooking.ShortCycle;
using EatWhat.Tools.Persistence;
using EatWhat.Tools.Security;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class ShortCyclePhase2TransitionTests
{
    public static IList<string> PassedCases { get; private set; }
    public static IList<string> ProbeCases { get; private set; }
    public static IList<object> ProbeRuns { get; private set; }
    private static void ProbeCheck(bool value, string message) { Check(value,message); ProbeCases.Add(message); }
    private static void Check(bool value, string message) { if (!value) throw new Exception("AI281: " + message); PassedCases.Add(message); }
    private static void Field(object target, string name, object value) { target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value); }
    private static T Read<T>(object target, string name) { return (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target); }
    private static void Call(object target, string name) { try { target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null); } catch (TargetInvocationException e) { throw e.InnerException; } }
    private sealed class Keys : IKeyProvider { public byte[] GetKey() { return new byte[32]; } }
    private sealed class Lease : IShortCyclePhase2SceneLease
    {
        public Phase2SceneEntry Entry { get; set; } = new Phase2SceneEntry();
        public bool IsLoaded { get; private set; } = true;
        public int UnloadCount; private Action<string> done;
        public void Unload(Action<string> completed) { UnloadCount++; if (!IsLoaded) { completed(null); return; } done = completed; }
        public void ExternalUnload() { IsLoaded=false; if (Entry!=null) Entry.Teardown(); }
        public void Finish(string error = null) { if (error == null) IsLoaded = false; var callback = done; done = null; if (callback != null) callback(error); }
    }
    private sealed class Loader : IShortCyclePhase2SceneLoader
    {
        public int LoadCount; public bool Allow = true; public bool ThrowLoad;
        private Action<IShortCyclePhase2SceneLease,string> done;
        public bool CanLoad(out string error) { error = Allow ? null : "preflight refused"; return Allow; }
        public void LoadAdditive(Action<IShortCyclePhase2SceneLease,string> completed) { LoadCount++; if (ThrowLoad) throw new IOException("controlled load exception"); done = completed; }
        public void Finish(Lease scene, string error = null) { var callback = done; done = null; callback(scene,error); }
    }
    private sealed class Harness : IDisposable
    {
        public readonly ShortCycleSessionManager Session = new ShortCycleSessionManager();
        public readonly ShortCyclePhase2TransitionAdapter Adapter = new ShortCyclePhase2TransitionAdapter();
        public readonly ShortCyclePhase2HandoffPresenter Presenter = new ShortCyclePhase2HandoffPresenter();
        public readonly InventoryManager Inventory = new InventoryManager();
        public readonly Loader Loader = new Loader();
        public readonly ShortCyclePresentationDirector Director = new ShortCyclePresentationDirector();
        public readonly ShortCycleSpace P1 = new ShortCycleSpace();
        public readonly ShortCyclePresentationSet Normal = new ShortCyclePresentationSet();
        public readonly ShortCyclePresentationSet Board = new ShortCyclePresentationSet();
        public ShortCycleSpace P0;
        public RecipeBookBinder Book;
        public ShortCycleActionRouter Router;
        public ShortCycleCameraPanController Pan;
        public ShortCycleInputLockController Input;
        public ShortCycleInteractionLayerStack Layers;
        public MouseInteractionLayer BaseLayer;
        public Camera Camera;
        public Transform CenterMarker, LeftMarker, KitchenMarker;
        private readonly ShortCycleSaveIsolation isolation; private bool destroyed;
        public Harness(string root) : this(root,null) { }
        public Harness(string root, Action<Harness> configure)
        {
            Session.gameObject.name = "AI281Session"; Session.gameObject.scene = new Scene { path = "AI281Source", name = "AI281Source", isLoaded = true };
            Read<Dictionary<string,int>>(Inventory,"quantities")["ing_fixture"] = 10;
            Field(P1,"spaceId",ShortCycleSpaceId.Phase1);
            Normal.transform.SetParent(P1.transform,false); Board.transform.SetParent(P1.transform,false);
            Field(Board,"keepVisualActiveAcrossSpaces",true);
            Field(P1,"structureSets",new[] { Normal, Board });
            P0 = new ShortCycleSpace(); var p0 = P0; Field(p0,"visualOnlySets",new[] { Board });
            Field(Director,"spaces",new[] { p0, P1 });
            Adapter.SetSceneLoader(Loader); Field(Presenter,"transitionAdapter",Adapter);
            Field(Session,"inventoryManager",Inventory); Field(Session,"playerProfile",new PlayerProfile());
            Field(Session,"phase2HandoffPresenter",Presenter); Field(Session,"presentationDirector",Director);
            if (configure!=null) configure(this);
            isolation = ShortCycleSaveIsolation.ForTarget(Session,Path.GetFullPath(root),() => new JsonSaveStore(Path.GetFullPath(root),new PlainJsonCodec(),new EnvelopeAuthenticator(new Keys())));
            Call(Session,"Awake"); Read<SaveBoundary>(Session,"persistenceBoundary").PrepareBeforeSession(true);
            if (configure!=null) return;
            var machine = Read<ShortCycleStateMachine>(Session,"stateMachine");
            if (!machine.TryEnter(null,ShortCycleTriggerContext.FreeTry).Succeeded || !machine.TryShowPhase0View(ShortCyclePhase0View.RecipeCatalog).Succeeded ||
                !machine.TrySelectRecipe("rcp_fixture",false).Succeeded || !machine.TryStartCooking().Succeeded) throw new Exception("P2S fixture FSM precondition");
            Session.Context.prep_zone_contents.Add(new PrepZoneItem { item_id="ing_fixture", quantity=1, drag_order=7, state="prepared" });
            Session.Context.placeholder_states.Add(new PlaceholderState { instance_id="slot_fixture", item_id="ing_fixture", taken_count=1 });
            Session.Context.current_filter_tag="tag_fixture"; Session.Context.current_scroll_offset=.5f;
        }
        public void Destroy() { if (destroyed) return; destroyed = true; Call(Session,"OnDestroy"); Call(Adapter,"OnDestroy");
            if (Pan!=null) Pan.CancelPan(); if (Layers!=null) Call(Layers,"OnDisable"); Director.ClearSessionPresentation(); isolation.Dispose(); }
        public void Dispose() { Destroy(); }
    }
    // Fixture construction only. All probe state transitions and binding below use real public APIs.
    private static CK01GeneratedDataTable CsvTable<T>(string root,string name,string key) where T: ScriptableObject,new()
    { return (CK01GeneratedDataTable)typeof(ShortCycleRecipeBookTests).GetMethod("Table",BindingFlags.Static|BindingFlags.NonPublic).MakeGenericMethod(typeof(T)).Invoke(null,new object[]{root,name,key}); }
    private static void ConfigureProbeHarness(Harness h,string root)
    {
        var catalog=new CK01GeneratedDataCatalog(); catalog.ReplaceTables(new[]{
            CsvTable<CK01RecipeData>(root,"Recipes","recipe_id"),CsvTable<CK01RecipeVariantData>(root,"RecipeVariants","var_id"),
            CsvTable<CK01RecipeSlotData>(root,"RecipeSlots","slot_id"),CsvTable<CK01ItemData>(root,"Items","item_id"),
            CsvTable<CK01TagData>(root,"Tags","tag_id"),CsvTable<CK01RecipeStepData>(root,"RecipeSteps","step_id"),
            CsvTable<CK01ToolData>(root,"Tools","tool_id"),CsvTable<CK01InitialInventoryData>(root,"InitialInventory","item_id"),
            CsvTable<CK01FridgeCapacityLevelData>(root,"FridgeCapacityLevels","fcap_id"),CsvTable<CK01CookingGameConfigData>(root,"CookingGameConfig","key")});
        Field(h.Session,"dataCatalog",catalog); Field(h.Inventory,"dataCatalog",catalog);
        string diagnostic; if(!h.Inventory.TryRebuildFromDataCatalog(out diagnostic)) throw new Exception("probe inventory fixture: "+diagnostic);
        CK01FridgeCapacityLevelData capacity; if(!catalog.TryGetDemoFridgeCapacity(out capacity,out diagnostic)) throw new Exception(diagnostic);
        var fridge=new ShortCycleFridgeBinder(); var slots=new FridgeSlot[capacity.capacity];for(var i=0;i<slots.Length;i++)slots[i]=new FridgeSlot();Field(fridge,"slots",slots);
        var clue=new ShortCycleClueBoardBinder();var blocks=new SlotBlock[16];for(var i=0;i<blocks.Length;i++)
        { blocks[i]=new SlotBlock();Field(blocks[i],"centerGroup",new Transform());var anchors=new Transform[8];var cards=new ShortCycleSubCard[8];for(var k=0;k<8;k++){anchors[k]=new Transform{name=ShortCycleAnchorContract.GetSubstituteAnchorName(k)};cards[k]=new ShortCycleSubCard();}Field(blocks[i],"subSlotAnchors",anchors);Field(blocks[i],"subCards",cards); }
        Field(clue,"slotBlocks",blocks);Field(h.Session,"fridgeBinder",fridge);Field(h.Session,"clueBoardBinder",clue);
        h.Book=new RecipeBookBinder();var catalogCards=new CatalogCard[8];for(var i=0;i<8;i++)catalogCards[i]=new CatalogCard();Field(h.Book,"catalogCards",catalogCards);Field(h.Book,"browseTitle",new TextMeshPro());Field(h.Book,"browseDish",new SpriteRenderer());
        var ingredients=new IngredientListPiece();var entries=new IngredientCard[16];for(var i=0;i<16;i++)entries[i]=new IngredientCard();Field(ingredients,"entries",entries);Field(h.Book,"ingredientList",ingredients);
        var notes=new StickerNotesPiece();var stickers=new Sticker[16];for(var i=0;i<16;i++)stickers[i]=new Sticker();Field(notes,"notes",stickers);Field(h.Book,"stickerNotes",notes);
        var tab=new CurrentRecipeTabPiece();Field(tab,"title",new TextMeshPro());Field(tab,"favoriteStar",new SpriteRenderer{sprite=new Sprite()});Field(h.Book,"currentRecipeTab",tab);
        var details=new StepDetailModalPiece();var rows=new StepDetailRowPiece[10];for(var i=0;i<10;i++){rows[i]=new StepDetailRowPiece();Field(rows[i],"description",new TextMeshPro());var icons=new SpriteRenderer[16];var tools=new SpriteRenderer[16];for(var k=0;k<16;k++){icons[k]=new SpriteRenderer();tools[k]=new SpriteRenderer();}Field(rows[i],"ingredientIcons",icons);Field(rows[i],"toolIcons",tools);}Field(details,"rows",rows);Field(details,"legacyTitle",new TextMeshPro());Field(h.Book,"stepDetail",details);Field(h.Session,"recipeBookBinder",h.Book);
        h.Input=new ShortCycleInputLockController();h.Pan=new ShortCycleCameraPanController();h.Camera=new Camera();h.CenterMarker=new Transform();h.LeftMarker=new Transform{position=new Vector3(-16f,0f,0f)};h.KitchenMarker=new Transform{position=new Vector3(-32f,0f,0f)};
        Field(h.Pan,"controlledCamera",h.Camera);Field(h.Pan,"phase0Marker",h.CenterMarker);Field(h.Pan,"p0LeftMarker",h.LeftMarker);Field(h.Pan,"phase1Marker",h.KitchenMarker);Field(h.Pan,"inputLockController",h.Input);Field(h.Pan,"panDuration",.1f);Call(h.Pan,"Awake");Field(h.Director,"cameraPanController",h.Pan);
        Field(h.P0,"viewTable",new[]{new ShortCycleViewSetEntry{view=ShortCyclePhase0View.Cover,cameraSlot=ShortCycleCameraSlot.P0Center},new ShortCycleViewSetEntry{view=ShortCyclePhase0View.RecipeCatalog,cameraSlot=ShortCycleCameraSlot.P0Center},new ShortCycleViewSetEntry{view=ShortCyclePhase0View.RecipeBrowse,cameraSlot=ShortCycleCameraSlot.P0Left},new ShortCycleViewSetEntry{view=ShortCyclePhase0View.ClueBoardShell,cameraSlot=ShortCycleCameraSlot.P0Left}});
        Field(h.P1,"cameraSlot",ShortCycleCameraSlot.P1);h.BaseLayer=new MouseInteractionLayer();var kitchenLayer=new MouseInteractionLayer();h.Layers=new ShortCycleInteractionLayerStack();Field(h.Layers,"baseLayer",h.BaseLayer);Field(h.Layers,"phase1Layer",kitchenLayer);Field(h.Layers,"sessionManager",h.Session);Field(h.P0,"spaceLayer",h.BaseLayer);Field(h.P1,"spaceLayer",kitchenLayer);Field(h.Director,"interactionLayerStack",h.Layers);Call(h.Layers,"Awake");Call(h.Layers,"OnEnable");
        h.Router=new ShortCycleActionRouter();Field(h.Router,"inputLockController",h.Input);Field(h.Session,"actionRouter",h.Router);
    }
    private static void Tick(Harness h)
    { Time.unscaledDeltaTime=.05f;Time.unscaledTime+=.05f;if(h.Pan!=null)h.Pan.AdvanceCoroutinesOnce(); }
    private static void Settle(Harness h)
    { for(var i=0;h.Pan.IsPanning&&i<100;i++)Tick(h);if(h.Pan.IsPanning)throw new Exception("fixture camera did not settle"); }
    private static void DispatchSetup(Harness h,ShortCycleActionRequest request)
    { var result=h.Router.DispatchAction(request);if(!result.Succeeded)throw new Exception("fixture router: "+result.Error);Settle(h); }
    private static string StartBoundBrowse(Harness h,bool teardown)
    {
        var begin=h.Session.BeginSession(null,ShortCycleTriggerContext.FreeTry);if(!begin.Succeeded)throw new Exception("fixture BeginSession: "+begin.Error);Settle(h);
        string id=null;foreach(var view in h.Session.GetRecipeViews())if(view.is_unlocked){id=view.recipe_id;break;}
        DispatchSetup(h,new ShortCycleActionRequest{ActionName=ShortCycleActionNames.OpenCover});DispatchSetup(h,new ShortCycleActionRequest{ActionName=ShortCycleActionNames.SelectRecipe,recipe_id=id});
        if(h.Book.CurrentRecipe==null)throw new Exception("fixture did not bind actual selected recipe");
        if(teardown) for(var i=0;i<3;i++)DispatchSetup(h,new ShortCycleActionRequest{ActionName=ShortCycleActionNames.Escape});
        return id;
    }
    private static ShortCycleAutomatedProbe Probe(Harness h,string recipe)
    {
        var p=new ShortCycleAutomatedProbe();Field(p,"coordinator",h.Session);Field(p,"actionRouter",h.Router);Field(p,"presentationDirector",h.Director);Field(p,"cameraPanController",h.Pan);Field(p,"controlledCamera",h.Camera);Field(p,"inputLockController",h.Input);Field(p,"interactionLayerStack",h.Layers);Field(p,"clueBoardLayer",h.BaseLayer);Field(p,"clueBoardShell",new Transform());Field(p,"phase0CameraMarker",h.CenterMarker);Field(p,"phase1CameraMarker",h.KitchenMarker);Field(p,"verificationRecipeId",recipe);return p;
    }
    private static void Drive(IEnumerator routine,Harness h,Action frame)
    {
        var work=new Stack<IEnumerator>();work.Push(routine);var turns=0;
        while(work.Count>0)
        { if(++turns>3000)throw new Exception("actual probe coroutine did not terminate");var current=work.Peek();if(!current.MoveNext()){work.Pop();continue;}var nested=current.Current as IEnumerator;if(nested!=null){work.Push(nested);continue;}Tick(h);if(frame!=null)frame(); }
    }
    private static List<string> ExecuteActualProbe(ShortCycleAutomatedProbe probe,Harness h,string loadError=null)
    {
        var failures=new List<string>();var method=typeof(ShortCycleAutomatedProbe).GetMethod("RunPhase2HandoffChecks",BindingFlags.Instance|BindingFlags.NonPublic);
        var routine=(IEnumerator)method.Invoke(probe,new object[]{failures});Lease scene=null;var consumed=0;string recipe=null;var count=0;
        Drive(routine,h,()=>{
            if(h.Adapter.Status==ShortCyclePhase2TransitionStatus.Loading && scene==null)
            { if(loadError!=null)h.Loader.Finish(null,loadError);else {scene=new Lease();h.Loader.Finish(scene);consumed=scene.Entry.ReceiveCount;recipe=scene.Entry.RecipeId;count=scene.Entry.PrepZoneContents.Count;} }
            if(scene!=null&&h.Adapter.Status==ShortCyclePhase2TransitionStatus.Unloading&&scene.UnloadCount>0)scene.Finish();
        });
        ProbeRuns.Add(new{start="None/empty book unless explicit active-session negative",endPhase=h.Session.CurrentPhase.ToString(),loaderCalls=h.Loader.LoadCount,unloadCalls=scene==null?0:scene.UnloadCount,consumed=consumed,recipeId=recipe,prepCount=count,ownerReleased=h.Inventory.Phase2Owner==null,failures=failures.ToArray(),checkpoints=new List<string>(probe.AutomatedProbeCheckpoints).ToArray(),loadFailure=loadError});
        return failures;
    }
    private static bool TwoActualPasses(ShortCycleAutomatedProbe p)
    {return p.AutomatedProbeCheckpoints.Count==2&&p.AutomatedProbeCheckpoints[0].StartsWith("19/20 PASS")&&p.AutomatedProbeCheckpoints[1].StartsWith("20/20 PASS");}
    private static void RunPostTeardownProbeCases(string root,string projectRoot)
    {
        var oldTime=Time.unscaledTime;var oldDelta=Time.unscaledDeltaTime;
        using(var h=new Harness(Path.Combine(root,"actual-probe-success"),x=>ConfigureProbeHarness(x,projectRoot)))
        {
            var recipe=StartBoundBrowse(h,true);
            ProbeCheck(h.Session.CurrentPhase==ShortCyclePhase.None&&h.Book.CurrentRecipe==null&&h.Book.CatalogRecipeCount==0,"actual original-tail three Router ESCs reach None and clear RecipeBook");
            var quantities=new Dictionary<string,int>();foreach(var view in h.Session.GetRecipeViews())foreach(var slot in view.slots)quantities[slot.standard_ingredient.item_id]=h.Inventory.GetQuantity(slot.standard_ingredient.item_id);
            var p=Probe(h,recipe);var trace=new List<string>();h.Session.StateChanged+=(phase,view)=>trace.Add(phase+"/"+view);
            var failures=ExecuteActualProbe(p,h);
            ProbeCheck(failures.Count==0&&TwoActualPasses(p)&&h.Loader.LoadCount==1,"actual appended coroutine rebuilds from None and records real19/20 PASS with one load");
            ProbeCheck(trace.Contains("Phase0/Cover")&&trace.Contains("Phase0/RecipeCatalog")&&trace.Contains("Phase0/RecipeBrowse")&&trace.Contains("Phase1/RecipeBrowse")&&trace.Contains("Phase2/RecipeBrowse"),"actual BeginSession/Router selection drives real bound FSM states and camera waits");
            ProbeCheck(h.Book.CurrentRecipe!=null&&h.Book.CurrentRecipe.recipe_id==recipe&&h.Session.Context.current_recipe_id==recipe,"actual selected recipe remains data-bound, not reflected into production state");
            ProbeCheck(h.Session.CurrentPhase==ShortCyclePhase.Phase1&&h.Inventory.Phase2Owner==null&&h.Adapter.SceneEntry==null&&!h.Director.Phase2TransitionActive,"actual20 returns Phase1 and releases scene/inventory ownership");
            var conserved=true;foreach(var pair in quantities){var prep=0;foreach(var item in h.Session.Context.prep_zone_contents)if(item.item_id==pair.Key)prep+=item.quantity;if(h.Inventory.GetQuantity(pair.Key)+prep!=pair.Value)conserved=false;}
            ProbeCheck(conserved&&h.Session.Context.prep_zone_contents.Count==1&&h.Session.Context.placeholder_states.Count==0,"actual19/20 conserves configured stock plus committed prep without duplicate placeholders");
        }
        using(var h=new Harness(Path.Combine(root,"actual-probe-locked"),x=>ConfigureProbeHarness(x,projectRoot)))
        { StartBoundBrowse(h,true);string locked=null;foreach(var view in h.Session.GetRecipeViews())if(!view.is_unlocked){locked=view.recipe_id;break;}if(locked==null)throw new Exception("expected current configured locked recipe");var p=Probe(h,locked);var failures=ExecuteActualProbe(p,h);
          ProbeCheck(failures.Exists(x=>x.Contains("missing or locked"))&&!TwoActualPasses(p)&&h.Loader.LoadCount==0,"actual coroutine reports locked recipe explicitly, never unlocks or loads it");
          ProbeCheck(h.Session.CurrentPhase==ShortCyclePhase.None&&h.Book.CurrentRecipe==null&&h.Session.PlayerProfile.UnlockedRecipeIds.Count==0,"locked-recipe failure cleans only its uncommitted fresh session"); }
        using(var h=new Harness(Path.Combine(root,"actual-probe-missing-router"),x=>ConfigureProbeHarness(x,projectRoot)))
        { var recipe=StartBoundBrowse(h,true);var p=Probe(h,recipe);Field(p,"actionRouter",null);var failures=ExecuteActualProbe(p,h);
          ProbeCheck(failures.Count>0&&!TwoActualPasses(p)&&h.Loader.LoadCount==0&&h.Session.CurrentPhase==ShortCyclePhase.None,"unconfigured actual probe fails both checkpoints without inventing execution"); }
        using(var h=new Harness(Path.Combine(root,"actual-probe-layer"),x=>ConfigureProbeHarness(x,projectRoot)))
        { var recipe=StartBoundBrowse(h,true);var p=Probe(h,recipe);Field(p,"clueBoardLayer",new MouseInteractionLayer());var failures=ExecuteActualProbe(p,h);
          ProbeCheck(failures.Exists(x=>x.Contains("base layer"))&&h.Loader.LoadCount==0&&h.Session.CurrentPhase==ShortCyclePhase.None&&h.Book.CurrentRecipe==null,"actual layer prerequisite failure is explicit and its own new session is cleaned"); }
        using(var h=new Harness(Path.Combine(root,"actual-probe-load-failure"),x=>ConfigureProbeHarness(x,projectRoot)))
        { var recipe=StartBoundBrowse(h,true);var p=Probe(h,recipe);var failures=ExecuteActualProbe(p,h,"controlled actual probe load failure");
          ProbeCheck(failures.Exists(x=>x.Contains("controlled actual probe load failure"))&&h.Loader.LoadCount==1&&!TwoActualPasses(p),"actual asynchronous load failure remains FAIL, not skipped or hardcoded PASS");
          ProbeCheck(h.Session.CurrentPhase==ShortCyclePhase.Phase1&&h.Inventory.Phase2Owner==null&&h.Session.Context.prep_zone_contents.Count==1&&h.Session.Context.placeholder_states.Count==0,"actual failed-load cleanup preserves already committed prep instead of discarding stock"); }
        using(var h=new Harness(Path.Combine(root,"actual-probe-unowned-active"),x=>ConfigureProbeHarness(x,projectRoot)))
        { var recipe=StartBoundBrowse(h,false);var p=Probe(h,recipe);var failures=ExecuteActualProbe(p,h);
          ProbeCheck(failures.Exists(x=>x.Contains("completed None"))&&h.Loader.LoadCount==0&&h.Session.CurrentPhase==ShortCyclePhase.Phase0&&h.Book.CurrentRecipe!=null,"actual probe refuses and preserves an unrelated already-active session"); }
        Time.unscaledTime=oldTime;Time.unscaledDeltaTime=oldDelta;
    }
    public static IList<string> RecipeBookCases { get; private set; }
    public static IList<object> RecipeBookRuns { get; private set; }
    private static void RecipeCheck(bool value,string message)
    { Check(value,message); RecipeBookCases.Add(message); }
    private static List<string> ExecuteRecipeProbe(ShortCycleAutomatedProbe probe,Harness h,string scenario)
    {
        var failures=new List<string>();
        var routine=(IEnumerator)typeof(ShortCycleAutomatedProbe).GetMethod("RunRecipeBookChecks",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(probe,new object[]{failures});
        Drive(routine,h,null);
        RecipeBookRuns.Add(new{scenario=scenario,start="actual three Router ESCs -> None/empty book",endPhase=h.Session.CurrentPhase.ToString(),failures=failures.ToArray(),checkpoints=new List<string>(probe.AutomatedProbeCheckpoints).ToArray(),favorites=new List<string>(h.Session.PlayerProfile.FavoriteRecipeIds).ToArray()});
        return failures;
    }
    private static bool FiveRecipePasses(ShortCycleAutomatedProbe p)
    {
        if(p.AutomatedProbeCheckpoints.Count!=5)return false;
        for(var n=0;n<5;n++)if(!p.AutomatedProbeCheckpoints[n].StartsWith((n+14)+"/13 PASS"))return false;
        return true;
    }
    private static void ConfigureRecipeProbe(Harness h,string projectRoot)
    {
        ConfigureProbeHarness(h,projectRoot);
        foreach(var card in h.Book.CatalogCards)Field(card,"favoriteStar",new SpriteRenderer{sprite=new Sprite{name="fixture-favorite"}});
        var table=new LocTableAsset();
        foreach(var view in h.Session.GetRecipeViews())
        {
            table.entries.Add(new LocTableEntry{key=view.name_key,zh_cn="localized:"+view.recipe_id});
            foreach(var step in view.steps)table.entries.Add(new LocTableEntry{key=step.display_text_key,zh_cn="localized:"+step.step_id});
        }
        LocService.Configure(table);
    }
    private static void RunRecipeProbeCases(string root,string projectRoot)
    {
        foreach(var scenario in new[]{"tail-none/catalog-last","locked-empty-before-valid/browse-last","no-available-recipe","missing-router","wrong-layer","missing-book-binding"})
        using(var h=new Harness(Path.Combine(root,scenario.Replace('/','-')),x=>ConfigureRecipeProbe(x,projectRoot)))
        {
            var recipe=StartBoundBrowse(h,true);
            RecipeCheck(h.Session.CurrentPhase==ShortCyclePhase.None&&h.Book.CurrentRecipe==null&&h.Book.CatalogRecipeCount==0,scenario+": starts from actual13-tail None and cleared book");
            var trace=new List<string>();var order=new List<string>();
            h.Session.StateChanged+=(phase,view)=>{
                trace.Add(phase+"/"+view);
                if(scenario=="locked-empty-before-valid/browse-last"&&phase==ShortCyclePhase.Phase0&&view==ShortCyclePhase0View.Cover)
                {
                    var cards=new List<CatalogCard>();var unlocked=new List<CatalogCard>();
                    foreach(var card in h.Book.CatalogCards)
                        if(card.View!=null&&card.View.is_unlocked)unlocked.Add(card);else cards.Add(card);
                    cards.AddRange(unlocked);Field(h.Book,"catalogCards",cards.ToArray());
                    foreach(var card in cards)order.Add(card.View==null?"unused":card.View.is_unlocked?"valid":"locked");
                }
            };
            if(scenario=="no-available-recipe")
            {
                var catalog=Read<CK01GeneratedDataCatalog>(h.Session,"dataCatalog");
                foreach(var view in h.Session.GetRecipeViews()){CK01RecipeData row;string diagnostic;if(!catalog.TryGetRecipe(view.recipe_id,out row,out diagnostic))throw new Exception(diagnostic);row.is_unlocked_default=false;}
            }
            if(scenario=="missing-book-binding")Field(h.Book,"browseTitle",null);
            var probe=Probe(h,recipe);
            if(scenario=="missing-router")Field(probe,"actionRouter",null);
            if(scenario=="wrong-layer")Field(probe,"clueBoardLayer",new MouseInteractionLayer());
            var failures=ExecuteRecipeProbe(probe,h,scenario);
            if(scenario=="tail-none/catalog-last"||scenario=="locked-empty-before-valid/browse-last")
            {
                RecipeCheck(failures.Count==0&&FiveRecipePasses(probe),scenario+": actual14..18 all PASS through legal Router actions");
                RecipeCheck(trace.Contains("Phase0/Cover")&&trace.Contains("Phase0/RecipeCatalog")&&trace.Contains("Phase0/RecipeBrowse"),scenario+": actual Cover/Catalog/Browse camera and layer flow covered");
                RecipeCheck(h.Session.CurrentPhase==ShortCyclePhase.None&&h.Book.CurrentRecipe==null&&h.Book.CatalogRecipeCount==0&&h.Session.PlayerProfile.FavoriteRecipeIds.Count==0,scenario+": original cleanup and favorite restoration retained");
                if(order.Count>0)RecipeCheck(order[0]=="locked"&&order.Contains("unused")&&order[order.Count-1]=="valid","locked and unused actual bound cards precede valid recipes; final Browse admits detail without duplicate selection");
            }
            else
            {
                RecipeCheck(failures.Count>0&&!FiveRecipePasses(probe),scenario+": explicit actual coroutine FAIL, never skipped or hardcoded PASS");
                if(scenario=="no-available-recipe")RecipeCheck(failures.Exists(x=>x.Contains("no selectable bound recipe")),"no available configured recipe reports explicit selection coverage failure");
                if(scenario=="wrong-layer")RecipeCheck(failures.Exists(x=>x.Contains("card/camera/layer prerequisite")),"wrong layer rejects actual card actions");
                if(scenario=="missing-router")RecipeCheck(failures.Exists(x=>x.Contains("card/camera/layer prerequisite")),"missing Router rejects actual card actions");
                if(scenario=="missing-book-binding")RecipeCheck(probe.AutomatedProbeCheckpoints[0].StartsWith("14/13 FAIL"),"missing book binding fails actual restart/count prerequisite");
            }
        }
    }
    public static string RunJointPrerequisites(string root,string projectRoot)
    {
        PassedCases=new List<string>();ProbeCases=new List<string>();ProbeRuns=new List<object>();RecipeBookCases=new List<string>();RecipeBookRuns=new List<object>();
        Directory.CreateDirectory(root);var reads=Application.PersistentPathReads;var oldTime=Time.unscaledTime;var oldDelta=Time.unscaledDeltaTime;
        try{RunRecipeProbeCases(root,projectRoot);RunPostTeardownProbeCases(root,projectRoot);RecipeCheck(Application.PersistentPathReads==reads,"all actual14..20 cases use isolated task roots with default user path reads0");}
        finally{Time.unscaledTime=oldTime;Time.unscaledDeltaTime=oldDelta;}
        return "PASS: actual14..18 "+RecipeBookCases.Count+" assertions; existing19/20 "+ProbeCases.Count+" assertions / six scenarios";
    }
    public static string Run(string root, string projectRoot = null)
    {
        PassedCases = new List<string>(); ProbeCases = new List<string>(); ProbeRuns = new List<object>(); Directory.CreateDirectory(root); var reads = Application.PersistentPathReads;
        Check((int)ShortCyclePhase0View.Cover==0 && (int)ShortCyclePhase0View.RecipeBrowse==1 && (int)ShortCyclePhase0View.RecipeCatalog==2 && (int)ShortCyclePhase0View.ClueBoardShell==3 && (int)ShortCyclePhase0View.StepDetail==4 && (int)ShortCyclePhase0View.Phase2Transition==5,"view enum preserves 0..4 and appends transition5");
        using (var h = new Harness(Path.Combine(root,"success")))
        {
            var notices=0; h.Adapter.Requested += _ => notices++;
            Check(h.Session.TryAdvanceToPhase2().Succeeded && h.Adapter.Status==ShortCyclePhase2TransitionStatus.Loading && !h.Adapter.IsReady,"FSM commits to handoff while async scene remains explicitly loading");
            Check(h.Inventory.GetQuantity("ing_fixture")==9 && h.Loader.LoadCount==1,"existing InventoryManager performs one original deduction");
            Check(h.Director.Phase2TransitionActive && !h.Normal.IsVisibilityRequested && !h.Board.IsVisibilityRequested && !h.P1.WantsActive(h.Board),"Director transition hides normal and persistent clue-board Sets");
            Check(!h.Session.TryAdvanceToPhase2().Succeeded && h.Loader.LoadCount==1,"repeated GO while loading cannot load or commit twice");
            h.Presenter.LastHandoff.PrepZoneContents[0].quantity=99;
            var scene=new Lease(); h.Loader.Finish(scene);
            Check(h.Adapter.IsReady && scene.Entry.RecipeId=="rcp_fixture" && scene.Entry.PrepZoneContents[0].quantity==1 && scene.Entry.PrepZoneContents[0].drag_order==7 && scene.Entry.PrepZoneContents[0].state=="prepared","entry consumes detached exact recipe/prep fields, not mutated holder snapshot");
            Check(ReferenceEquals(scene.Entry.KitchenInventory,h.Inventory) && ReferenceEquals(h.Inventory.Phase2Owner,scene.Entry),"P2 owns the existing inventory instance without copying quantities");
            var copy=scene.Entry.PrepZoneContents; copy[0].quantity=42;
            Check(scene.Entry.PrepZoneContents[0].quantity==1 && h.Session.Context.prep_zone_contents[0].quantity==1,"consumer readback cannot mutate held prep or original context");
            string error;
            Check(!h.Inventory.TryCommitPlaceholderStates(new List<PlaceholderState>()).Succeeded && !h.Inventory.TryRebuildFromDataCatalog(out error) && h.Inventory.GetQuantity("ing_fixture")==9,"P1 inventory writes and rebuild are refused under P2 ownership");
            Check(!scene.Entry.TryConsume(new ShortCycleDataHandoff("again",new List<PrepZoneItem>()),h.Inventory,out error) && scene.Entry.ReceiveCount==1,"one loaded entry consumes handoff only once");
            Check(h.Adapter.ReturnToPhase1() && h.Adapter.Status==ShortCyclePhase2TransitionStatus.Unloading && h.Session.CurrentPhase==ShortCyclePhase.Phase2 && !h.Adapter.IsReady,"return waits for owned scene unload rather than claiming Phase1 early");
            Check(h.Inventory.Phase2Owner==null && scene.Entry.KitchenInventory==null && scene.UnloadCount==1,"teardown releases exact inventory owner and starts one owned unload");
            Check(!scene.Entry.TryConsume(new ShortCycleDataHandoff("stale",new List<PrepZoneItem>()),h.Inventory,out error),"torn-down entry cannot reclaim inventory before native destruction");
            scene.Finish();
            Check(h.Session.CurrentPhase==ShortCyclePhase.Phase1 && !h.Director.Phase2TransitionActive && h.Normal.IsVisibilityRequested && h.Board.IsVisibilityRequested,"successful unload restores FSM Phase1 and Director matrix");
            Check(h.Session.Context.prep_zone_contents.Count==1 && h.Session.Context.placeholder_states.Count==0 && h.Session.Context.current_filter_tag=="tag_fixture" && h.Session.Context.current_scroll_offset==.5f,"return retains prep/navigation and clears committed placeholders");
            Check(h.Session.TryAdvanceToPhase2().Succeeded && h.Inventory.GetQuantity("ing_fixture")==9,"second entry does not deduct already committed prep again");
            var scene2=new Lease(); h.Loader.Finish(scene2);
            Check(h.Adapter.IsReady && notices==2 && scene2.Entry.ReceiveCount==1 && !scene.Entry.IsReady,"repeated entry uses one consumer and one observer call per cycle");
            h.Adapter.ReturnToPhase1(); scene2.Finish();
            Check(!h.Adapter.ReturnToPhase1() && scene2.UnloadCount==1 && h.Inventory.GetQuantity("ing_fixture")==9,"repeated return adds no unload, deduction or inventory copy");
        }
        using(var h=new Harness(Path.Combine(root,"preflight")))
        { h.Loader.Allow=false; Check(!h.Session.TryAdvanceToPhase2().Succeeded && h.Session.CurrentPhase==ShortCyclePhase.Phase1 && h.Inventory.GetQuantity("ing_fixture")==10 && h.Loader.LoadCount==0,"missing/preloaded scene preflight refuses before original inventory commit"); }
        using(var h=new Harness(Path.Combine(root,"load-failure")))
        { h.Session.TryAdvanceToPhase2(); h.Loader.Finish(null,"controlled load failure");
          Check(h.Adapter.Status==ShortCyclePhase2TransitionStatus.Failed && h.Adapter.LastError=="controlled load failure" && !h.Adapter.IsReady && h.Session.CurrentPhase==ShortCyclePhase.Phase1,"async load failure is explicit and restores Phase1");
          Check(h.Inventory.GetQuantity("ing_fixture")==9 && h.Session.Context.placeholder_states.Count==0 && h.Inventory.Phase2Owner==null,"failed load keeps committed inventory once, no refund or duplicate placeholders"); }
        using(var h=new Harness(Path.Combine(root,"cancel-loading")))
        { h.Session.TryAdvanceToPhase2(); h.Adapter.Teardown();
          Check(h.Adapter.Status==ShortCyclePhase2TransitionStatus.Unloading && !h.Adapter.CanRequest,"cancelled async load blocks next use until late completion cleanup");
          var scene=new Lease(); h.Loader.Finish(scene);
          Check(scene.Entry.ReceiveCount==0 && scene.UnloadCount==1 && h.Inventory.Phase2Owner==null,"late cancelled load never consumes data and unloads only its returned lease");
          scene.Finish(); Check(h.Session.CurrentPhase==ShortCyclePhase.Phase1 && h.Adapter.LastError!=null && !h.Adapter.IsReady,"cancel finishes with observable reason and recovered Phase1"); }
        using(var h=new Harness(Path.Combine(root,"destroy-loading")))
        { h.Session.TryAdvanceToPhase2(); h.Destroy(); var scene=new Lease(); h.Loader.Finish(scene);
          Check(scene.Entry.ReceiveCount==0 && scene.UnloadCount==1,"destroyed owner still cleans late returned scene without consuming");
          scene.Finish(); Check(h.Session.PersistenceEnded && h.Inventory.Phase2Owner==null && !h.Adapter.IsReady,"late unload after owner destruction cannot restore dead Session"); }
        using(var h=new Harness(Path.Combine(root,"missing-entry")))
        { h.Session.TryAdvanceToPhase2(); var scene=new Lease { Entry=null }; h.Loader.Finish(scene,"unique entry missing");
          Check(scene.UnloadCount==1 && h.Session.CurrentPhase==ShortCyclePhase.Phase2,"bad scene is unloaded before recovery, never marked Active");
          scene.Finish(); Check(h.Session.CurrentPhase==ShortCyclePhase.Phase1 && h.Adapter.Status==ShortCyclePhase2TransitionStatus.Failed,"missing entry surfaces explicit failure and safe recovery"); }
        using(var h=new Harness(Path.Combine(root,"invalid-prep")))
        { h.Session.Context.prep_zone_contents[0].item_id=""; h.Session.TryAdvanceToPhase2(); var scene=new Lease(); h.Loader.Finish(scene);
          Check(!scene.Entry.IsReady && scene.Entry.ReceiveCount==0 && h.Inventory.Phase2Owner==null && scene.UnloadCount==1,"invalid prep is rejected before inventory ownership transfer"); scene.Finish(); }
        using(var h=new Harness(Path.Combine(root,"unload-failure")))
        { h.Session.TryAdvanceToPhase2(); var scene=new Lease(); h.Loader.Finish(scene); h.Adapter.ReturnToPhase1(); scene.Finish("controlled unload failure");
          Check(h.Adapter.Status==ShortCyclePhase2TransitionStatus.Failed && h.Adapter.SceneEntry!=null && !h.Adapter.CanRequest && h.Session.CurrentPhase==ShortCyclePhase.Phase2,"unload failure retains owned handle and blocks reentry without false recovery");
          h.Adapter.Teardown(); scene.Finish(); Check(h.Session.CurrentPhase==ShortCyclePhase.Phase1 && h.Adapter.SceneEntry==null,"explicit later cleanup releases retained handle; no automatic retry"); }
        using(var h=new Harness(Path.Combine(root,"external-owner")))
        { h.Session.TryAdvanceToPhase2(); var other=new Phase2SceneEntry(); string error; other.TryConsume(new ShortCycleDataHandoff("other",new List<PrepZoneItem>()),h.Inventory,out error);
          var scene=new Lease(); h.Loader.Finish(scene); scene.Finish();
          Check(ReferenceEquals(h.Inventory.Phase2Owner,other) && !h.Adapter.IsReady && !h.Adapter.CanRequest,"failed transfer cleanup never releases another inventory owner"); other.Teardown(); }
        using(var h=new Harness(Path.Combine(root,"observer")))
        { h.Adapter.Requested += _ => { throw new Exception("controlled observer error"); }; h.Session.TryAdvanceToPhase2(); var scene=new Lease(); h.Loader.Finish(scene);
          Check(h.Adapter.IsReady && h.Inventory.GetQuantity("ing_fixture")==9,"observer exception cannot interrupt committed handoff or duplicate deduction"); h.Adapter.ReturnToPhase1(); scene.Finish(); }
        using(var h=new Harness(Path.Combine(root,"sync-load-error")))
        { h.Loader.ThrowLoad=true; h.Session.TryAdvanceToPhase2(); Check(h.Adapter.Status==ShortCyclePhase2TransitionStatus.Failed && h.Session.CurrentPhase==ShortCyclePhase.Phase1 && h.Inventory.Phase2Owner==null,"synchronous loader exception becomes explicit failure with Phase1 restore"); }
        using(var h=new Harness(Path.Combine(root,"cancel-before-load")))
        { h.Adapter.Requested += _ => h.Adapter.Teardown(); h.Session.TryAdvanceToPhase2();
          Check(h.Loader.LoadCount==0 && h.Session.CurrentPhase==ShortCyclePhase.Phase1 && h.Adapter.Status==ShortCyclePhase2TransitionStatus.Failed && h.Inventory.GetQuantity("ing_fixture")==9,"reentrant cancellation before async start recovers after commit without loading or repeating deduction"); }
        using(var h=new Harness(Path.Combine(root,"external-unload")))
        { h.Session.TryAdvanceToPhase2(); var scene=new Lease(); h.Loader.Finish(scene); scene.ExternalUnload(); Call(h.Adapter,"Update");
          Check(h.Session.CurrentPhase==ShortCyclePhase.Phase1 && h.Inventory.Phase2Owner==null && h.Adapter.LastError!=null && !h.Adapter.IsReady,"externally removed P2 scene releases owner and restores Phase1 with explicit reason"); }
        var native=new ShortCyclePhase2SceneLoader(); string nativeError;
        Check(!native.CanLoad(out nativeError) && ShortCyclePhase2SceneLoader.ScenePath=="Assets/Scenes/ShortCycle_P2.unity","default loader uses exact new scene path and refuses absent Build entry offline");
        var preloaded=new Scene { path=ShortCyclePhase2SceneLoader.ScenePath, name="P2", isLoaded=true }; SceneManager.SetLoaded(preloaded);
        Check(!native.CanLoad(out nativeError) && nativeError.Contains("another owner"),"default loader never takes ownership of an already loaded scene"); SceneManager.RaiseUnloaded(preloaded);
        Check(Application.PersistentPathReads==reads,"all P2S scenarios use pre-Awake controlled save roots without real user path reads");
        RunPostTeardownProbeCases(root,projectRoot ?? Directory.GetCurrentDirectory());
        Check(Application.PersistentPathReads==reads,"actual appended probe also avoids all real user path reads");
        Debug.WarningMessages.Clear(); Debug.Messages.Clear();
        return "PASS: AI281 P2S transition "+PassedCases.Count+" assertions";
    }
}
