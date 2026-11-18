using System;
using System.Collections.Generic;
using EatWhat.LongLoop;
using EatWhat.Narrative.Core;

internal static class NarrativeSceneStateTests
{
    private static int checks;
    static void Check(bool value,string name) { checks++; if(!value)throw new Exception(name); }
    static NarrativeCharacter Character(string id,string visual)
    { return new NarrativeCharacter(id,"","",null,null,null,visual,"",null,null); }
    static NarrativeSceneStatePlacement P(string id,string state="day",string anchor="",bool active=true,string facing="",string pool="",string kind="actor")
    { return new NarrativeSceneStatePlacement("scn_street",state,kind,id,anchor,facing,active,"",pool,""); }
    static NarrativeDataDirectory Directory(IEnumerable<NarrativeSceneStatePlacement> rows)
    {
        return new NarrativeDataDirectory(characters:new[]{Character("chr_bound","Bound"),Character("chr_missing","Missing"),Character("chr_empty","")},
            scenes:new[]{new NarrativeScene("scn_street","","street",false,"Street","day")},
            states:new[]{new NarrativeSceneState("scn_street","day","","",""),new NarrativeSceneState("scn_street","night","","","")},placements:rows,
            dialogues:new[]{new NarrativeDialogue("dlg_a",NarrativeDialogueLevel.Ambient,null,new NarrativeProfileNames("","","","","","","","",""),"","ln_a","",new NarrativeCommandSource(NarrativeCommandParser.Parse("")))},
            lines:new[]{new NarrativeDialogueLine("ln_a","dlg_a","","","","text",new NarrativeCommandSource(NarrativeCommandParser.Parse("")),"","","","")},
            pools:new[]{new NarrativeAmbientPool("pool_a","",new[]{"dlg_a"}),new NarrativeAmbientPool("pool_b","",new[]{"dlg_a"})});
    }
    // Only the permitted object/index/placement and scene instantiation surfaces are substituted.
    sealed class Actor : INarrativeSceneStateActor
    {
        public bool Alive=true,Active=true,Right=true; public int Position=7,Captures,Restores,VisualWrites,PoolWrites;
        public object Visual=new object(); public string Pool="pool_a";public readonly NarrativeAmbientBubbleModel Model;
        bool savedActive,savedRight;int savedPosition;object savedVisual;string savedPool; readonly World world;
        public Actor(World w,string id){world=w;Model=new NarrativeAmbientBubbleModel(id,w.Directory);Model.ChangePool(Pool);}
        public bool IsAlive=>Alive;
        public bool RememberOriginal(out string issue){issue=null;Captures++;savedActive=Active;savedRight=Right;savedPosition=Position;savedVisual=Visual;savedPool=Pool;return true;}
        public void RestoreOriginal(){Restores++;Active=savedActive;Right=savedRight;Position=savedPosition;Visual=savedVisual;SetPool(savedPool);}
        public void ReleaseOriginal(){}
        public void SetActive(bool value){Active=value;}
        public bool PlaceAtAnchor(string id,out string issue){issue=null;int p;if(!world.Anchors.TryGetValue(id,out p)){issue="missing";return false;}Position=p;return true;}
        public void SetFacing(bool right){Right=right;}
        public void SetVisual(object value){VisualWrites++;Visual=value;}
        public void SetPool(string value){PoolWrites++;Pool=value;Model.ChangePool(value);}
    }
    sealed class World : INarrativeSceneStateWorld
    {
        public readonly INarrativeDataDirectory Directory;public readonly Dictionary<string,Actor> Actors=new Dictionary<string,Actor>();
        public readonly Dictionary<string,int> Anchors=new Dictionary<string,int>{{"anc_gate",20}};
        public readonly Dictionary<string,object> Visuals=new Dictionary<string,object>();public object Placeholder=new object();public int Spawns,Destroys;public bool CanSpawn=true;
        public World(INarrativeDataDirectory directory){Directory=directory;Visuals.Add("chr_bound",new object());}
        public Actor Add(string id){var a=new Actor(this,id);Actors.Add(id,a);return a;}
        public object PlaceholderVisual=>Placeholder;
        public bool TryFindActor(string id,out INarrativeSceneStateActor result){Actor a;bool ok=Actors.TryGetValue(id,out a)&&a.Alive;result=ok?a:null;return ok;}
        public bool TrySpawnActor(string id,out INarrativeSceneStateActor result,out string issue){result=null;issue="unassigned generic";if(!CanSpawn)return false;Spawns++;var a=new Actor(this,id);Actors[id]=a;result=a;issue=null;return true;}
        public void DestroyActor(INarrativeSceneStateActor actor){((Actor)actor).Alive=false;Destroys++;}
        public bool TryGetVisual(string id,out object value,out string issue){issue="not imported";return Visuals.TryGetValue(id,out value);}
    }
    public static int Main()
    {
        var directory=Directory(new[]{P("chr_bound#2",anchor:"anc_gate",facing:"left",pool:"pool_b"),P("chr_missing"),P("chr_empty"),P("chr_absent"),P("chr_off",active:false,anchor:"anc_gate",facing:"left",pool:"pool_b"),P("chr_new",anchor:"anc_gate",pool:"pool_b"),P("chr_no_anchor"),P("obj_door",kind:"object"),P("exit_street",kind:"exit"),P("chr_empty",state:"night",facing:"left")});
        var world=new World(directory);var bound=world.Add("chr_bound#2");var missing=world.Add("chr_missing");var empty=world.Add("chr_empty");var absent=world.Add("chr_absent");var off=world.Add("chr_off");var untouched=world.Add("chr_untouched");
        var emptyVisual=empty.Visual;var absentVisual=absent.Visual;var originalBound=bound.Visual;
        var config=new DialoguePresentationData();config.trigger[0].autoBubbleCooldown=10;config.trigger[0].autoBubbleRadius=3;
        NarrativeAmbientBubbleEmission emission;Check(bound.Model.TryTrigger(0,0,DialoguePresentationResolver.ResolveTrigger(config,null).Snapshot,n=>0,out emission),"actual model deadline seeded");
        var deadline=bound.Model.CooldownUntilSeconds;var model=bound.Model;
        using(var app=new NarrativeSceneStateApplication(directory,world))
        {
            Check(!app.ApplyDefault("Missing")&&world.Spawns==0&&bound.Captures==0,"unknown scene changes nothing");
            Check(!app.ApplyDefault("street"),"scene basename matching is exact");
            Check(app.ApplyDefault("Street"),"scene id/default state selected through actual directory");
            Check(world.Spawns==2,"only missing full ids spawned; objects/exits skipped");
            Check(bound.Captures==1&&bound.Position==20&&!bound.Right,"preplaced reused, anchor and facing applied");
            Check(ReferenceEquals(bound.Visual,world.Visuals["chr_bound"]),"instance suffix uses actual parser base id for bound visual");
            Check(ReferenceEquals(missing.Visual,world.Placeholder),"specified but unbound visual uses configured placeholder");
            Check(ReferenceEquals(empty.Visual,emptyVisual)&&empty.VisualWrites==0,"empty source preserves own visual");
            Check(ReferenceEquals(absent.Visual,absentVisual)&&absent.VisualWrites==0,"no character row preserves own visual");
            Check(!off.Active&&off.Position==7&&off.Right&&off.VisualWrites==0&&off.PoolWrites==0,"not present skips all later steps");
            Check(world.Actors["chr_new"].Position==20&&world.Actors["chr_new"].Active,"new actor reaches named anchor");
            Check(!world.Actors["chr_no_anchor"].Active,"new actor without anchor deactivated");
            Check(missing.Position==7&&empty.Position==7,"preplaced blank anchor preserves original position");
            Check(bound.Pool=="pool_b"&&empty.Pool=="pool_a","filled pool wins, empty keeps authored pool");
            Check(untouched.Captures==0&&untouched.Restores==0&&untouched.Position==7&&untouched.VisualWrites==0&&untouched.PoolWrites==0,"unlisted author object untouched");
            Check(ReferenceEquals(model,bound.Model)&&bound.Model.CooldownUntilSeconds==deadline,"pool change keeps actual NPC model/deadline");
            Check(app.Diagnostics.Count>=4,"skipped kinds, missing visual, blank-new-anchor diagnosed");
            int spawns=world.Spawns;Check(!app.Apply("Street","absent")&&world.Spawns==spawns&&world.Destroys==0,"invalid requested state preserves current objects");
            var generated=world.Actors["chr_new"];
            Check(app.Apply("Street","night")&&world.Destroys==2&&!generated.Alive,"reapply cleans only its own generated actors");
            Check(bound.Position==7&&bound.Right&&ReferenceEquals(bound.Visual,originalBound)&&bound.Pool=="pool_a","previously affected preplaced restores first authored state");
            Check(empty.Captures==1&&!empty.Right,"first backup not overwritten by next state");
            Check(untouched.Restores==0&&untouched.Alive,"unlisted object excluded from restore and cleanup");
            Check(bound.Model.CooldownUntilSeconds==deadline&&ReferenceEquals(bound.Model,model),"restore/reapply does not erase cooldown or recreate model");
            Check(app.ApplyDefault("Street")&&bound.Captures==1&&world.Spawns==4,"default can reapply without duplicate preplaced spawn");
            Check(app.ApplyDefault("Street")&&world.Destroys==4&&world.Spawns==6,"same state replaces only generated set");
        }
        Check(world.Destroys==6&&bound.Alive&&bound.Position==7&&bound.Right&&ReferenceEquals(bound.Visual,originalBound),"dispose cleans generated and restores originals");
        Check(untouched.Captures==0&&untouched.Restores==0,"dispose also leaves untouched actors alone");
        var blank=Directory(new[]{P("chr_missing"),P("chr_empty")});var w2=new World(blank);w2.Placeholder=null;var m=w2.Add("chr_missing");var e=w2.Add("chr_empty");e.Visual=null;
        using(var app=new NarrativeSceneStateApplication(blank,w2)){Check(app.ApplyDefault("Street")&&m.VisualWrites==0,"missing placeholder diagnosed without erasing current visual");Check(e.Visual==null&&e.VisualWrites==0,"empty table plus no own visual remains none");}
        var w3=new World(directory);w3.CanSpawn=false;
        using(var app=new NarrativeSceneStateApplication(directory,w3)){Check(app.ApplyDefault("Street")&&w3.Spawns==0&&app.Diagnostics.Count>0,"unassigned generic skips with diagnostic");}
        var duplicates=new NarrativeDataDirectory(scenes:new[]{new NarrativeScene("a","","",false,"Street","day"),new NarrativeScene("b","","",false,"Street","day")});
        NarrativeScene scene;string diagnostic;Check(!duplicates.TryFindSceneAsset("Street",out scene,out diagnostic)&&scene==null,"ambiguous file basename refused");
        var inactive=Directory(new[]{P("chr_empty")});var w4=new World(inactive);var preset=w4.Add("chr_empty");preset.Active=false;
        using(var app=new NarrativeSceneStateApplication(inactive,w4)){Check(app.ApplyDefault("Street")&&preset.Active&&w4.Spawns==0,"inactive preplaced indexed capability reused");}
        Check(!preset.Active,"first inactive author value restored");
        Console.WriteLine("SCENE_STATE_PASS checks="+checks);return 0;
    }
}
