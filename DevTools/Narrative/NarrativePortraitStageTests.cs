using System;
using System.Collections.Generic;
using System.Reflection;
using EatWhat.Narrative.Core;
using EatWhat.Narrative.Unity;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

internal static class NarrativePortraitStageTests
{
    static readonly List<string> details = new List<string>();
    static void Check(bool value, string why) { if (!value) throw new Exception("Portrait stage: " + why); details.Add(why); }
    static void Near(float value, float expected, string why) { Check(Math.Abs(value-expected)<.001f, why+" ("+value+"/"+expected+")"); }
    static RectTransform Rect(Image image) { return (RectTransform)image.transform; }
    static readonly string[] Ids = { "chr_left", "chr_centre", "chr_right" };
    sealed class Fixture
    {
        public readonly NarrativePortraitStage Stage;
        public readonly Image Template;
        public readonly DialoguePresentationSnapshot Snapshot;
        public readonly NarrativePortraitLayoutResult Layout;
        public readonly List<NarrativePortraitImageSource> Sources = new List<NarrativePortraitImageSource>();
        public Fixture(bool fade = true, bool gradual = false, float left = -200, float centre = .5f, float right = 200)
        {
            var config = new DialoguePresentationData(); config.entrance[0].portraitSlide=120;config.entrance[0].portraitFade=fade;config.exit[0].portraitSlide=80;
            config.speaker[0].listenerDim=.4f;config.speaker[0].speakerScale=1.2f;config.speaker[0].speakerRise=20;config.speaker[0].speakerShiftInward=30;
            config.speaker[0].transition=gradual?DialogueSpeakerTransition.Fade:DialogueSpeakerTransition.Cut;config.speaker[0].transitionTime=2;
            var inputs=new List<NarrativePortraitCastInput>();float[] xs={left,centre,right};int[] orders={1,3,2};
            for(int i=0;i<3;i++)
            {
                var character=new NarrativeCharacter(Ids[i],"name","portrait",.1f,.2f,2,"","",null,null);
                inputs.Add(new NarrativePortraitCastInput(new NarrativeCastEntry(i+1,Ids[i],"",xs[i],10,1.5f,orders[i]),character,true));
                Sources.Add(new NarrativePortraitImageSource(character,new Sprite{rect=new UnityEngine.Rect(0,0,400+i*10,800+i*10)}));
            }
            var profiles=new NarrativeProfileNames("", "", "", "", "", "", "", "", "");
            var dialogue=new NarrativeDialogue("stage",NarrativeDialogueLevel.Normal,new NarrativeCastEntry[0],profiles,"","","",new NarrativeCommandSource(NarrativeCommandParser.Parse("")));
            Snapshot=DialoguePresentationResolver.Resolve(config,dialogue).Snapshot;Layout=NarrativePortraitLayoutResolver.Resolve(inputs,Snapshot);
            var root=new GameObject("Existing Portraits",typeof(RectTransform));Stage=root.AddComponent<NarrativePortraitStage>();
            var template=new GameObject("Saved portrait template",typeof(RectTransform));template.transform.SetParent(root.transform,false);template.SetActive(false);
            Template=template.AddComponent<Image>();Template.raycastTarget=false;
            typeof(NarrativePortraitStage).GetField("portraitTemplate",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(Stage,Template);
        }
        public NarrativePortraitStage.Batch Show(bool end=false) { return Stage.ShowBatch(Layout,Sources,Snapshot,end); }
    }
    static void TestExitFreeze()
    {
        var f=new Fixture(true,true);var a=f.Show(true);f.Stage.SetSpeaker(a,Ids[2]);f.Stage.Advance(.5);
        float dim=a.Images[0].color.r,scale=Rect(a.Images[2]).localScale.y,rise=Rect(a.Images[2]).anchoredPosition.y;
        f.Stage.SetExitProgress(a,0);var live=f.Show(true);f.Stage.SetSpeaker(live,Ids[0]);f.Stage.Advance(2);
        Near(a.Images[0].color.r,dim,"exit freezes current partial brightness");
        Near(Rect(a.Images[2]).localScale.y,scale,"exit freezes current partial scale");
        Near(Rect(a.Images[2]).anchoredPosition.y,rise,"exit freezes current partial rise");
        Check(live.Images[1].color.r<1,"other live batch highlighter still advances during old exit");f.Stage.Clear();
    }
    static int Main()
    {
        try
        {
            var f=new Fixture();Check(f.Stage.MirrorRightPortraits,"mirror defaults on");var batch=f.Show();
            Check(batch.IsAlive&&batch.Images.Count==3,"one clone per resolved visible portrait");
            for(int i=0;i<3;i++)
            {
                var image=batch.Images[i];var r=Rect(image);var p=f.Layout.Portraits[i];
                Check(!ReferenceEquals(image,f.Template)&&image.transform.parent==f.Stage.transform&&image.gameObject.activeSelf,"clone uses saved template and existing layer");
                Check(ReferenceEquals(image.sprite,f.Sources[i].Sprite)&&!image.raycastTarget&&!image.preserveAspect,"given sprite uses simple non-intercepting image");
                Near(r.anchorMin.x,.5f,"bottom centre anchor x");Near(r.anchorMax.y,0,"bottom centre anchor y");
                Near(r.pivot.x,p.AlignmentPoint.X,"resolved pivot x");Near(r.pivot.y,p.AlignmentPoint.Y,"resolved pivot y");
                Near(r.sizeDelta.x,400+i*10,"given sprite pixel width");Near(r.sizeDelta.y,800+i*10,"given sprite pixel height");
                Near(r.localScale.y,p.Scale,"resolved total scale y");Near(r.localScale.x,i==2?-p.Scale:p.Scale,"mirror only supplied right side");
                Near(r.anchoredPosition.x,p.DisplayAnchor.X+(int)p.Side*120,"entry zero slide follows resolved side");Near(r.anchoredPosition.y,10,"entry preserves anchor y");Near(image.color.a,0,"entry zero fade");
            }
            f.Stage.SetEntranceProgress(batch,.5f);
            Near(Rect(batch.Images[0]).anchoredPosition.x,-260,"left entry half");Near(Rect(batch.Images[1]).anchoredPosition.x,.5f,"tolerant centre never slides");Near(Rect(batch.Images[2]).anchoredPosition.x,260,"right entry half");
            foreach(var image in batch.Images)Near(image.color.a,.5f,"entry half alpha");
            f.Stage.SetEntranceProgress(batch,1);foreach(var image in batch.Images)Near(image.color.a,1,"entry end opaque");
            Near(Rect(batch.Images[0]).anchoredPosition.x,-200,"entry end left anchor");Near(Rect(batch.Images[2]).anchoredPosition.x,200,"entry end right anchor");
            f.Stage.MirrorRightPortraits=false;foreach(var image in batch.Images)Near(Rect(image).localScale.x,3,"mirror off all positive");f.Stage.MirrorRightPortraits=true;
            Check(batch.Images[0].transform.GetSiblingIndex()<batch.Images[2].transform.GetSiblingIndex()&&batch.Images[2].transform.GetSiblingIndex()<batch.Images[1].transform.GetSiblingIndex(),"base order follows numeric order");
            f.Stage.SetSpeaker(batch,Ids[0]);Near(Rect(batch.Images[0]).localScale.y,3.6f,"speaker model scale drawn");Near(Rect(batch.Images[0]).anchoredPosition.x,-170,"speaker inward shift drawn");Near(Rect(batch.Images[0]).anchoredPosition.y,30,"speaker rise drawn");
            Near(batch.Images[0].color.r,1,"speaker brightness drawn");Near(batch.Images[1].color.r,.4f,"listener brightness drawn");Near(batch.Images[1].color.g,.4f,"listener green drawn");Near(batch.Images[1].color.b,.4f,"listener blue drawn");
            Check(batch.Images[0].transform.GetSiblingIndex()>batch.Images[1].transform.GetSiblingIndex(),"model front flag draws speaker at front of batch");
            f.Stage.SetSpeaker(batch,null);Check(batch.Images[0].transform.GetSiblingIndex()<batch.Images[2].transform.GetSiblingIndex(),"finished speaker restores base order");

            var second=f.Show();f.Stage.SetExitProgress(batch,0);float start=Rect(batch.Images[0]).anchoredPosition.x;
            f.Stage.SetExitProgress(batch,.5f);Near(Rect(batch.Images[0]).anchoredPosition.x,start-40,"exit half moves outward from current position");Near(batch.Images[0].color.a,.5f,"exit half fades current alpha");
            f.Stage.SetEntranceProgress(second,.5f);Near(Rect(batch.Images[0]).anchoredPosition.x,start-40,"new batch progress does not affect outgoing position");Near(second.Images[0].color.a,.5f,"new batch progresses independently");
            var dead=new List<Image>(batch.Images);f.Stage.SetExitProgress(batch,1);Check(!batch.IsAlive&&batch.Images.Count==0,"exit one releases batch handle");foreach(var image in dead)Check(image.gameObject.Destroyed&&!image.gameObject.activeSelf,"exit destroys and immediately disables own clone");Check(second.IsAlive&&second.Images.Count==3,"other batch survives exit");
            var cleared=new List<Image>(second.Images);f.Stage.Clear();foreach(var image in cleared)Check(image.gameObject.Destroyed,"clear destroys all remaining clones");Check(!f.Template.gameObject.Destroyed&&!f.Template.gameObject.activeSelf,"clear keeps saved template inactive and untouched");

            var noFade=new Fixture(false);var opaque=noFade.Show();foreach(var image in opaque.Images)Near(image.color.a,1,"fade false entry zero opaque");noFade.Stage.SetEntranceProgress(opaque,.5f);foreach(var image in opaque.Images)Near(image.color.a,1,"fade false entry half opaque");
            float partial=Rect(opaque.Images[2]).anchoredPosition.x;noFade.Stage.SetExitProgress(opaque,0);Near(Rect(opaque.Images[2]).anchoredPosition.x,partial,"exit starts at partial entrance position");noFade.Stage.SetExitProgress(opaque,.5f);Near(Rect(opaque.Images[2]).anchoredPosition.x,partial+40,"partial entrance exits from actual captured position");noFade.Stage.Clear();

            var gradual=new Fixture(true,true);var g=gradual.Show(true);gradual.Stage.SetSpeaker(g,Ids[2]);gradual.Stage.Advance(1);
            Near(g.Images[0].color.r,.7f,"external time interpolates listener brightness");Near(Rect(g.Images[2]).localScale.x,-3.3f,"external time interpolates mirrored speaker scale");Near(Rect(g.Images[2]).anchoredPosition.x,185,"external time draws inward shift");Near(Rect(g.Images[2]).anchoredPosition.y,20,"external time draws rise");gradual.Stage.Clear();

            var tolerance=new Fixture(left:-1,centre:-.999f,right:1);var tol=tolerance.Show(true);Near(Rect(tol.Images[0]).localScale.x,3,"minus one is left");Near(Rect(tol.Images[1]).localScale.x,3,"subpixel negative centre not flipped");Near(Rect(tol.Images[2]).localScale.x,-3,"plus one is right");tolerance.Stage.Clear();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var dev=new Fixture();var first=dev.Show(true);dev.Stage.SetTemporaryFraming(Ids[0],.3f,-.2f,4);Vector3 value;
            Check(dev.Stage.TryGetTemporaryFraming(Ids[0],out value),"temporary framing can be read");Near(value.z,4,"temporary scale readback");Near(Rect(first.Images[0]).pivot.x,.8f,"temporary offset replaces character x");Near(Rect(first.Images[0]).pivot.y,-.2f,"temporary offset replaces character y unclamped");Near(Rect(first.Images[0]).localScale.y,6,"temporary scale replaces character factor preserving cast factor");
            var later=dev.Show(true);Near(Rect(later.Images[0]).pivot.x,.8f,"later batch uses stored temporary framing");
            Check(dev.Stage.ClearTemporaryFraming(Ids[0]),"temporary framing clear returns changed");Near(Rect(first.Images[0]).pivot.x,.6f,"clear immediately restores existing pivot");Near(Rect(later.Images[0]).localScale.y,3,"clear restores both batches original scale");Check(!dev.Stage.TryGetTemporaryFraming(Ids[0],out value),"cleared framing absent");
            dev.Stage.SetTemporaryFraming(Ids[0],0,0,1);dev.Stage.Clear();var again=dev.Show(true);Near(Rect(again.Images[0]).localScale.y,1.5f,"node clear keeps explicit temporary settings until cleared");dev.Stage.Clear();
#else
            foreach(string method in new[]{"SetTemporaryFraming","TryGetTemporaryFraming","ClearTemporaryFraming"})Check(typeof(NarrativePortraitStage).GetMethod(method)==null,"release excludes "+method);
            Check(typeof(NarrativePortraitStage).GetField("temporaryFraming",BindingFlags.Instance|BindingFlags.NonPublic)==null,"release excludes temporary data store");
#endif
            CheckVisibleFramingProvider();
            var missing=new GameObject("Missing template layer",typeof(RectTransform)).AddComponent<NarrativePortraitStage>();int warnings=Debug.Warnings.Count;
            Check(missing.ShowBatch(f.Layout,f.Sources,f.Snapshot)==null&&missing.ShowBatch(f.Layout,f.Sources,f.Snapshot)==null,"unbound template displays nothing without error");Check(Debug.Warnings.Count==warnings+1,"unbound template diagnoses once");
            var disabled=f.Show(true);var owned=new List<Image>(disabled.Images);typeof(NarrativePortraitStage).GetMethod("OnDisable",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(f.Stage,null);Check(!disabled.IsAlive&&owned[0].gameObject.Destroyed,"disable clears owned nodes");
            TestExitFreeze();
            Console.WriteLine(JsonConvert.SerializeObject(new{result="PORTRAIT_STAGE_PASS",checks=details.Count,details,nativeUnity=false,givenSpriteDimensions=true,realRenderingVerified=false},Formatting.Indented));return 0;
        }
        catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
    }
    static void CheckVisibleFramingProvider()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        var f=new Fixture();Check(f.Stage.GetVisibleFramings().Count==0,"provider has no rows before any batch");var a=f.Show(true);
        var original=f.Stage.GetVisibleFramings();Check(original.Count==3&&original[0].Key==Ids[0]&&original[2].Key==Ids[2],"provider lists actual source character IDs in cast order");
        Near(original[0].Value.x,.1f,"provider original character offset x");Near(original[0].Value.y,.2f,"provider original character offset y");Near(original[0].Value.z,2,"provider character scale excludes cast factor and highlight");
        bool readOnly=false;try{((IList<KeyValuePair<string,Vector3>>)original).Add(new KeyValuePair<string,Vector3>());}catch(NotSupportedException){readOnly=true;}Check(readOnly,"provider list cannot mutate stage state");
        f.Stage.SetTemporaryFraming(Ids[0],.25f,.45f,4);var tuned=f.Stage.GetVisibleFramings();Near(tuned[0].Value.x,.25f,"provider reads temporary x");Near(tuned[0].Value.y,.45f,"provider reads temporary y");Near(tuned[0].Value.z,4,"provider reads temporary character scale");Near(original[0].Value.z,2,"prior returned value snapshot stays unchanged");
        f.Show(true);Check(f.Stage.GetVisibleFramings().Count==6,"provider retains every displayed node including same character in overlapping batches");
        f.Stage.SetExitProgress(a,1);Check(f.Stage.GetVisibleFramings().Count==3,"provider drops exited nodes");
        f.Stage.SetTemporaryFraming("chr_departed",.7f,.8f,5);f.Stage.Clear();Check(f.Stage.GetVisibleFramings().Count==0,"provider has no rows after all nodes leave");Vector3 value;Check(f.Stage.TryGetTemporaryFraming("chr_departed",out value),"offstage temporary value is retained before clear-all");
        var c=f.Show(true);Near(f.Stage.GetVisibleFramings()[0].Value.z,4,"returning character retains temporary setting");
        f.Stage.ClearAllTemporaryFraming();Check(!f.Stage.TryGetTemporaryFraming(Ids[0],out value)&&!f.Stage.TryGetTemporaryFraming("chr_departed",out value),"clear-all removes onstage and offstage temporary values");
        Near(Rect(c.Images[0]).pivot.x,.6f,"clear-all redraws current original x immediately");Near(Rect(c.Images[0]).localScale.y,3,"clear-all redraws original total scale immediately");Near(f.Stage.GetVisibleFramings()[0].Value.z,2,"provider reports original character scale after clear-all");
        f.Show(true);Near(f.Stage.GetVisibleFramings()[3].Value.z,2,"new batch after clear-all uses original values");
        f.Stage.Clear();f.Stage.SetTemporaryFraming(Ids[1],.2f,.3f,3);f.Stage.ClearAllTemporaryFraming();Check(!f.Stage.TryGetTemporaryFraming(Ids[1],out value),"clear-all works with no live batches");f.Stage.ClearAllTemporaryFraming();Check(f.Stage.GetVisibleFramings().Count==0,"clear-all with empty dictionary is idempotent");
#else
        Check(typeof(NarrativePortraitStage).GetMethod("GetVisibleFramings")==null,"release excludes visible framing query");
        Check(typeof(NarrativePortraitStage).GetMethod("ClearAllTemporaryFraming")==null,"release excludes clear-all tuning API");
#endif
    }
}
