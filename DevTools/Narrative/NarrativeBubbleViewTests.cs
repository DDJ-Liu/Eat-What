using System;
using System.Collections.Generic;
using System.Reflection;
using EatWhat.LongLoop;
using EatWhat.Narrative.Core;
using EatWhat.Narrative.Unity;
using UnityEngine;

public static class NarrativeBubbleViewTests
{
    static int checks;
    static void Check(bool value,string name) { checks++;if(!value)throw new Exception("Bubble view/trigger: "+name); }
    static bool Near(float actual,double expected) { return Math.Abs(actual-expected)<.00002; }
    sealed class Surface : INarrativeBubbleSurface
    {
        public Transform Head;public string Key,Text;public float Alpha,Rise;public int Disposed;public bool Alive=true;
        public bool Apply(float alpha,float rise) { Alpha=alpha;Rise=rise;return Alive&&Head!=null; }
        public void Dispose() { Disposed++; }
    }
    sealed class Factory : INarrativeBubbleSurfaceFactory
    {
        public readonly List<Surface> Created=new List<Surface>();public bool Available=true;
        public INarrativeBubbleSurface Create(Transform head,string key,string text,out string issue)
        {
            issue=Available?null:"synthetic external UI unavailable";if(!Available)return null;
            var surface=new Surface {Head=head,Key=key,Text=text};Created.Add(surface);return surface;
        }
    }
    sealed class World : INarrativeBubbleWorld
    {
        public double Distance;public bool GroundAvailable=true;public string LastSpeaker;
        public readonly Dictionary<string,Transform> Heads=new Dictionary<string,Transform>();
        public bool TryGetGroundDistance(out double distance,out string issue)
        { distance=Distance;issue=GroundAvailable?null:"synthetic missing ground configuration";return GroundAvailable; }
        public bool TryGetSpeakerHead(string speaker,out Transform head,out string issue)
        { LastSpeaker=speaker;bool ready=Heads.TryGetValue(speaker,out head);issue=ready?null:"synthetic unregistered speaker";return ready; }
    }
    static DialoguePresentationConfig Config()
    {
        var c=new DialoguePresentationConfig();c.Data.tier[0].bubbleFadeInTime=.4f;c.Data.tier[0].bubbleLifetime=.8f;
        c.Data.tier[0].bubbleExitRise=23f;c.Data.tier[0].bubbleExitFade=.6f;
        c.Data.trigger[0].autoBubbleRadius=3;c.Data.trigger[0].autoBubbleCooldown=2;return c;
    }
    static DialogueGroupSnapshot Tier(DialoguePresentationConfig config)
    { return DialoguePresentationResolver.Resolve(config.Data,NarrativeDataAdapter.FromRow(new CK01DialogueData {dialogue_id="dlg_tier",tier="ambient",first_line="ln_tier"})).Snapshot.GetGroup("tier"); }
    static CK01GeneratedDataCatalog Catalog()
    {
        var tables=new List<CK01GeneratedDataTable>();
        foreach(string name in new[]{"Characters","Dialogues","DialogueLines","CGs","Scenes","SceneStates","SceneStatePlacements","AmbientPools"})
        {
            var rows=new List<CK01GeneratedDataRowReference>();
            if(name=="Dialogues")foreach(string id in new[]{"dlg_view","dlg_other"})
                rows.Add(new CK01GeneratedDataRowReference(id,new CK01DialogueData {dialogue_id=id,tier="ambient",first_line=id=="dlg_view"?"ln_first":"ln_other"}));
            if(name=="DialogueLines")
            {
                rows.Add(new CK01GeneratedDataRowReference("ln_first",new CK01DialogueLineData {line_id="ln_first",dialogue_id="dlg_view",next_line="ln_second",text_key="bubble.first"}));
                rows.Add(new CK01GeneratedDataRowReference("ln_second",new CK01DialogueLineData {line_id="ln_second",dialogue_id="dlg_view",speaker="chr_other#02",text_key="bubble.second"}));
                rows.Add(new CK01GeneratedDataRowReference("ln_other",new CK01DialogueLineData {line_id="ln_other",dialogue_id="dlg_other",text_key="bubble.other"}));
            }
            if(name=="AmbientPools")foreach(string id in new[]{"amb_view","amb_other"})
                rows.Add(new CK01GeneratedDataRowReference(id,new CK01AmbientPoolData {pool_id=id,dialogue_ids=new List<string>{id=="amb_view"?"dlg_view":"dlg_other"}}));
            tables.Add(new CK01GeneratedDataTable(name,null,rows));
        }
        var catalog=new CK01GeneratedDataCatalog();catalog.ReplaceTables(tables);return catalog;
    }
    static NarrativeBubbleView View(Factory factory)
    { var view=new NarrativeBubbleView();view.ConfigureSurfaceFactory(factory);return view; }
    public static string Run()
    {
        checks=0;var loc=new LocTableAsset();
        loc.entries.Add(new LocTableEntry {key="bubble.first",zh_cn="第一句"});
        loc.entries.Add(new LocTableEntry {key="bubble.second",zh_cn="第二句"});
        loc.entries.Add(new LocTableEntry {key="bubble.other",zh_cn="另一池"});LocService.Configure(loc);
        var config=Config();var tier=Tier(config);var factory=new Factory();var view=View(factory);var head=new Transform();
        Check(view.Show("chr_a",head,"bubble.first",tier,0),"actual view accepts actual resolver tier and LocService key");
        var first=factory.Created[0];
        Check(first.Key=="bubble.first"&&first.Text=="第一句"&&ReferenceEquals(first.Head,head),"localized text/key and original head reach external UI");
        Check(first.Alpha==0&&first.Rise==0,"starts transparent without rise");
        view.Tick(.2);Check(Near(first.Alpha,.5)&&first.Rise==0,"configured fade-in midpoint");
        view.Tick(.4);Check(Near(first.Alpha,1)&&first.Disposed==0,"configured fade-in complete");
        view.Tick(1.1);Check(Near(first.Alpha,1)&&first.Rise==0,"configured lifetime starts after fade-in");
        view.Tick(1.5);Check(Near(first.Alpha,.5)&&Near(first.Rise,11.5),"configured exit seconds and reference-pixel rise");
        view.Tick(2);Check(first.Disposed==1&&view.ActiveCount==0,"configured exit completes and releases own surface once");
        view.Clear();Check(first.Disposed==1,"clear after completion cannot double-dispose");

        factory=new Factory();view=View(factory);
        view.Show("chr_a",head,"bubble.first",tier,10);view.Show("chr_b",head,"bubble.other",tier,10);
        view.Show("chr_a",head,"bubble.second",tier,10.2);view.Tick(10.3);
        Check(Near(factory.Created[0].Alpha,5.0/12)&&Near(factory.Created[0].Rise,23.0/6),"replacement exits from current partial fade alpha immediately");
        Check(Near(factory.Created[1].Alpha,.75)&&factory.Created[1].Rise==0,"different NPC remains in its own fade-in");
        Check(view.ActiveCount==3,"old surface remains for its exit animation");
        view.Show("chr_a",head,"bubble.first",tier,10.4);view.Tick(10.81);
        Check(factory.Created[0].Disposed==1,"third bubble cannot restart first bubble exit deadline");
        Check(factory.Created[1].Disposed==0&&Near(factory.Created[1].Alpha,1),"other NPC survives replacements");
        view.Tick(100);Check(view.ActiveCount==0,"large clock step uses scheduled natural exit instead of extending life");
        Check(factory.Created.TrueForAll(x=>x.Disposed==1),"each replacement independently released once");

        factory=new Factory();view=View(factory);for(int i=0;i<80;i++)view.Show("chr_"+i,head,"bubble.first",tier,0);
        Check(view.ActiveCount==80,"no global bubble cap");
        view.Clear();Check(factory.Created.TrueForAll(x=>x.Disposed==1),"clear boundary releases all NPCs");
        var zero=Config();zero.Data.tier[0].bubbleFadeInTime=0;zero.Data.tier[0].bubbleLifetime=0;zero.Data.tier[0].bubbleExitFade=0;
        Check(view.Show("chr_zero",head,"bubble.first",Tier(zero),0)&&view.ActiveCount==0,"zero lifecycle expires safely without invented minimum");
        Check(!view.Show("chr_bad",head,"missing.key",tier,1)&&view.Diagnostics[0].Contains("MISSING_LOCALIZATION"),"missing localization skips fallback token and diagnoses");
        Check(!view.Show("chr_bad",null,"bubble.first",tier,1),"missing anchor rejected");
        factory.Available=false;Check(!view.Show("chr_bad",head,"bubble.first",tier,1),"missing external UI reported");
        factory.Available=true;view.Show("chr_a",head,"bubble.first",tier,2);var alive=factory.Created[factory.Created.Count-1];
        alive.Alive=false;view.Tick(2.1);Check(alive.Disposed==1&&view.ActiveCount==0,"destroyed external anchor/UI cleans surface");
        view.Show("chr_a",head,"bubble.first",tier,3);view.Tick(3.2);float alpha=factory.Created[factory.Created.Count-1].Alpha;
        view.Tick(3.1);Check(factory.Created[factory.Created.Count-1].Alpha==alpha,"backward time cannot rewind fade");
        Check(!view.Show("chr_a",head,"bubble.first",tier,double.NaN),"invalid clock rejected");
        view.isActiveAndEnabled=false;typeof(NarrativeBubbleView).GetMethod("OnDisable",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(view,null);
        Check(view.ActiveCount==0&&!view.Show("chr_a",head,"bubble.first",tier,4),"actual component disable cleans and rejects display");

        config=Config();var runtime=new NarrativeRuntimeEntry();runtime.Configure(config,Catalog());
        factory=new Factory();view=View(factory);var world=new World {Distance=3};world.Heads.Add("chr_npc#2",head);
        var driver=new NarrativeAmbientBubbleDriver("chr_npc#2",runtime,world,view,"amb_view");
        Check(!driver.Tick(0,n=>0)&&factory.Created.Count==0&&!driver.CooldownUntilSeconds.HasValue,"external ground distance reaches real Core strict-radius check");
        world.Distance=2.999;
        Check(driver.Tick(0,n=>0)&&driver.CooldownUntilSeconds==2,"inside distance triggers actual model deadline");
        Check(factory.Created[0].Key=="bubble.first"&&world.LastSpeaker=="chr_npc#2","actual emission uses owner suffix and first line");
        Check(!driver.Tick(1,n=>0)&&factory.Created.Count==1,"same model survives every update during cooldown");
        Check(!driver.Tick(2,n=>0)&&world.LastSpeaker=="chr_other#02"&&driver.CooldownUntilSeconds==4,"missing exact speaker skips line after successful Core emission");
        Check(driver.Diagnostics[0].Contains("SPEAKER_UNAVAILABLE")&&factory.Created.Count==1,"missing speaker diagnostic without display");
        var otherHead=new Transform();world.Heads.Add("chr_other#02",otherHead);
        Check(!driver.Tick(3,n=>0),"adding speaker cannot refund skipped line cooldown");
        Check(driver.Tick(4,n=>0)&&factory.Created[1].Key=="bubble.first","skipped line progressed instead of replaying");
        Check(driver.Tick(6,n=>0)&&ReferenceEquals(factory.Created[2].Head,otherHead)&&factory.Created[2].Text=="第二句","exact other-instance head/text forwarded to actual view");
        view.Tick(6.2);Check(Near(factory.Created[2].Alpha,.5),"driver passes actual resolved custom tier to view");
        driver.ChangePool("amb_other");Check(driver.CooldownUntilSeconds==8&&!driver.Tick(7,n=>0),"pool setter delegates without resetting cooldown");
        Check(driver.Tick(8,n=>0)&&factory.Created[3].Key=="bubble.other","new pool starts at preserved deadline");
        driver.ChangePool("");Check(!driver.Tick(100,n=>0)&&driver.CooldownUntilSeconds==10,"empty pool disables without erasing deadline");
        driver.ChangePool("amb_other");world.GroundAvailable=false;
        Check(!driver.Tick(10,n=>0)&&driver.Diagnostics[driver.Diagnostics.Count-1].Contains("GROUND_UNAVAILABLE"),"missing ground capability diagnoses and skips");
        world.GroundAvailable=true;world.Distance=double.NaN;
        Check(!driver.Tick(10,n=>0)&&driver.CooldownUntilSeconds==10,"nonfinite distance rejected by actual Core");
        Check(driver.Diagnostics[driver.Diagnostics.Count-1].Contains("INVALID_INPUT"),"actual Core diagnostic retained");
        world.Distance=0;var independent=new NarrativeAmbientBubbleDriver("chr_other#02",runtime,world,View(new Factory()),"amb_other");
        Check(independent.Tick(0,n=>0)&&independent.CooldownUntilSeconds==2,"second NPC has its own model/cooldown");
        runtime.Initialize();
        Check(!driver.Tick(10,n=>0)&&driver.CooldownUntilSeconds==10,"directory reload suspends without recreating/resetting model");
        Check(driver.Diagnostics[driver.Diagnostics.Count-1].Contains("DIRECTORY_CHANGED"),"explicit directory lifetime diagnostic");
        int before=driver.Diagnostics.Count;driver.Tick(11,n=>0);Check(driver.Diagnostics.Count==before,"repeated diagnostic does not grow each frame");
        var missing=new NarrativeAmbientBubbleDriver("chr_missing",null,world,view,"amb_view");
        Check(!missing.Tick(0,n=>0)&&missing.Diagnostics[0].Contains("MISSING_BINDING"),"missing runtime invents no data/defaults");
        return "{\"passed\":true,\"checks\":"+checks+",\"actualRuntimeAdapterResolverCoreDriverAndView\":true,\"fakes\":\"external time/ground/head/UI only\",\"nativeUnity\":false}";
    }
}
