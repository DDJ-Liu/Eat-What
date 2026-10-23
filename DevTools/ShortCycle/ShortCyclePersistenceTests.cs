using System;
using System.Collections.Generic;
using System.IO;
using EatWhat.Cooking.ShortCycle;
using EatWhat.Tools.Persistence;
using EatWhat.Tools.Security;
using Newtonsoft.Json.Linq;
public static class ShortCyclePersistenceTests
{
    private sealed class Keys : IKeyProvider { public byte[] GetKey(){var result=new byte[32];for(var i=0;i<32;i++)result[i]=(byte)i;return result;} }
    private sealed class Inventory : IInventoryConsumptionGateway
    { public InventoryCommitResult TryCommitPlaceholderStates(IList<PlaceholderState> states){return new InventoryCommitResult{Succeeded=true};} }
    private sealed class Sink : IShortCyclePhase2HandoffSink
    {public bool IsAvailable{get{return true;}}public bool TryReceive(ShortCycleDataHandoff handoff,out string error){error=null;return true;}}
    public static IList<string> PassedCases{get;private set;}
    private static void Check(bool value,string id){if(!value)throw new Exception("SAVE-01 runtime: "+id);PassedCases.Add(id);}
    private static JsonSaveStore Store(string root){return new JsonSaveStore(root,new PlainJsonCodec(),new EnvelopeAuthenticator(new Keys()));}
    private static bool Same(byte[]a,byte[]b){if(a.Length!=b.Length)return false;for(var i=0;i<a.Length;i++)if(a[i]!=b[i])return false;return true;}
    private static ShortCycleStateMachine Machine(ShortCycleContext context,ISaveBoundary boundary)
    {return new ShortCycleStateMachine(context,boundary,new ShortCycleDataProcessor(new Inventory(),boundary,new Sink()),null);}
    private static SaveBoundary Host(ObservableNoOpSaveBoundary observed,PlayerProfileManager profile,JsonSaveStore store,List<string>warnings)
    {return new SaveBoundary(observed,()=>profile,()=>store,warnings.Add,()=>new DateTime(2026,10,19,0,0,0,DateTimeKind.Utc));}
    public static string Run(string root)
    {
        PassedCases=new List<string>();Directory.CreateDirectory(root);string diagnostic;SaveEnvelope read;
        var profile=new PlayerProfileManager(new PlayerProfile());profile.ToggleFavorite("initial");
        var observed=new ObservableNoOpSaveBoundary();var warnings=new List<string>();var store=Store(Path.Combine(root,"flow"));var host=Host(observed,profile,store,warnings);
        Check(!host.PrepareBeforeSession(false) && !host.IsPrepared,"active phase refuses profile restore");
        Check(host.PrepareBeforeSession(true) && warnings.Count==0 && profile.IsFavorite("initial"),"missing file keeps initial defaults before entry without warning");
        var context=new ShortCycleContext();var machine=Machine(context,host);
        Check(machine.TryEnter("rcp_fixture",ShortCycleTriggerContext.FreeTry).Succeeded && observed.ReachedSavePoints.Count==1 && observed.ReachedSavePoints[0]==ShortCycleSavePoint.EnterPhase0,"actual FSM entry emits exactly original save point1");
        var path=Path.Combine(store.RootPath,"default.json");var beforeToggle=File.ReadAllBytes(path);profile.ToggleFavorite("later");
        Check(Same(beforeToggle,File.ReadAllBytes(path)) && observed.ReachedSavePoints.Count==1,"favorite toggle adds no immediate persistence point");
        Check(machine.TryShowPhase0View(ShortCyclePhase0View.RecipeCatalog).Succeeded && machine.TrySelectRecipe("rcp_fixture",false).Succeeded && machine.TryStartCooking().Succeeded && machine.TryAdvanceToPhase2().Succeeded && observed.ReachedSavePoints.Count==2 && observed.ReachedSavePoints[1]==ShortCycleSavePoint.Phase1ToPhase2,"actual processor emits exactly original save point2");
        Check(store.TryRead("default",out read,out diagnostic) && ((JArray)read.Payload["player_profile"]["favorite_recipe_ids"]).Count==2,"point2 stores current copied profile section");
        var restored=new PlayerProfileManager(new PlayerProfile());var nextHost=Host(new ObservableNoOpSaveBoundary(),restored,Store(store.RootPath),new List<string>());
        Check(nextHost.PrepareBeforeSession(true) && restored.IsFavorite("initial") && restored.IsFavorite("later"),"fresh host restores snapshot before next BeginSession");
        restored.ToggleFavorite("live-after-load");Check(nextHost.PrepareBeforeSession(true) && restored.IsFavorite("live-after-load"),"same host does not reload over unsaved live favorites on session re-entry");
        var corruptRoot=Path.Combine(root,"corrupt");Directory.CreateDirectory(corruptRoot);var corruptFile=Path.Combine(corruptRoot,"default.json");File.WriteAllText(corruptFile,"{broken");var corruptBytes=File.ReadAllBytes(corruptFile);
        var defaults=new PlayerProfileManager(new PlayerProfile());defaults.ToggleFavorite("inspector-default");var warn=new List<string>();var badObs=new ObservableNoOpSaveBoundary();var badHost=Host(badObs,defaults,Store(corruptRoot),warn);
        Check(badHost.PrepareBeforeSession(true) && defaults.IsFavorite("inspector-default") && warn.Count==1,"corruption falls back to original defaults with one warning");
        var badMachine=Machine(new ShortCycleContext(),badHost);Check(badMachine.TryEnter("rcp_fixture",ShortCycleTriggerContext.FreeTry).Succeeded && badMachine.TryShowPhase0View(ShortCyclePhase0View.RecipeCatalog).Succeeded && badMachine.TrySelectRecipe("rcp_fixture",false).Succeeded && badMachine.TryStartCooking().Succeeded && badMachine.TryAdvanceToPhase2().Succeeded,"corrupt save does not block gameplay transitions");
        Check(Same(corruptBytes,File.ReadAllBytes(corruptFile)) && warn.Count==1 && badObs.ReachedSavePoints.Count==2,"both automatic save points preserve damaged original and do not warn twice");
        var futureStore=Store(Path.Combine(root,"future"));var payload=new JObject{["player_profile"]=new JObject{["favorite_recipe_ids"]=new JArray("seed"),["unlocked_recipe_ids"]=new JArray("unlock")},["future_section"]=new JObject{["keep"]=true}};
        Check(futureStore.Write("default",SaveEnvelope.Create(payload,DateTime.UtcNow),out diagnostic),"seed signed unknown payload section");
        var futureProfile=new PlayerProfileManager(new PlayerProfile());var futureHost=Host(new ObservableNoOpSaveBoundary(),futureProfile,Store(futureStore.RootPath),new List<string>());futureHost.PrepareBeforeSession(true);futureProfile.ToggleFavorite("new");
        var futureMachine=Machine(new ShortCycleContext(),futureHost);futureMachine.TryEnter("rcp_fixture",ShortCycleTriggerContext.FreeTry);var reread=Store(futureStore.RootPath);
        Check(reread.TryRead("default",out read,out diagnostic) && (bool)read.Payload["future_section"]["keep"] && futureProfile.GetSnapshot().UnlockedRecipeIds[0]=="unlock","unknown payload sections and explicit unlock IDs survive profile update");
        var invalid=Store(Path.Combine(root,"invalid-profile"));var invalidPayload=new JObject{["player_profile"]=new JObject{["favorite_recipe_ids"]="not-array",["unlocked_recipe_ids"]=new JArray()}};invalid.Write("default",SaveEnvelope.Create(invalidPayload,DateTime.UtcNow),out diagnostic);var invalidFile=Path.Combine(invalid.RootPath,"default.json");var old=File.ReadAllBytes(invalidFile);var invalidWarnings=new List<string>();var invalidHost=Host(new ObservableNoOpSaveBoundary(),new PlayerProfileManager(new PlayerProfile()),Store(invalid.RootPath),invalidWarnings);
        invalidHost.PrepareBeforeSession(true);Machine(new ShortCycleContext(),invalidHost).TryEnter("rcp_fixture",ShortCycleTriggerContext.FreeTry);
        Check(invalidWarnings.Count==1 && Same(old,File.ReadAllBytes(invalidFile)),"authenticated but malformed profile also blocks fallback overwrite");
        var forbidden=false;try{var unused=UnityEngine.Application.persistentDataPath;}catch(InvalidOperationException){forbidden=true;}Check(forbidden,"offline stub forbids all real user save path access");
        return "PASS: ShortCycle persistence "+PassedCases.Count+" assertions";
    }
}
