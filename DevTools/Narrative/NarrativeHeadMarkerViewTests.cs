using System;
using System.Reflection;
using System.Collections.Generic;
using EatWhat.Narrative.Unity;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
internal static class NarrativeHeadMarkerViewTests
{
    static int checks;static void Check(bool value,string reason){checks++;if(!value)throw new Exception(reason);}
    static object Get(object owner,string field){return owner.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(owner);}
    static void Set(object owner,string field,object value){owner.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(owner,value);}
    static void Near(double a,double b,string reason){Check(Math.Abs(a-b)<1e-5,reason);}
    public static int Main()
    {
        try
        {
            var owner=new GameObject("view");var view=owner.AddComponent<NarrativeHeadMarkerView>();var anchor=new GameObject("anchor").transform;
            anchor.position=new Vector3(2,3,4);anchor.localScale=new Vector3(7,7,7);
            Check(!view.Apply(anchor,"…",new Color(1,1,1,1),1,0),"missing explicit font refuses safely");
            var font=new TMP_FontAsset{material=new Material()};Set(view,"fontAsset",font);
            Check(view.Apply(anchor,"…",new Color(1,1,1,1),1,.1f),"actual renderer creates circle and glyph");
            var root=(GameObject)Get(view,"root");var circle=(Image)Get(view,"circle");var label=(TextMeshProUGUI)Get(view,"label");
            var sprite=(Sprite)Get(view,"sprite");var texture=(Texture2D)Get(view,"texture");var group=root.GetComponent<CanvasGroup>();var canvas=root.GetComponent<Canvas>();
            Check(root.transform.parent==null,"world marker is detached from NPC scale hierarchy");
            Near(root.transform.position.x,2,"world anchor x");Near(root.transform.position.y,3.1,"world bob y");Near(root.transform.position.z,4,"world anchor z");
            Near(root.transform.localScale.x*128,.85,"original V014-18 .85 world diameter");
            Check(canvas.renderMode==RenderMode.WorldSpace&&canvas.overrideSorting&&canvas.sortingLayerID==5,"explicit existing layer above characters in world space");
            Check(!group.interactable&&!group.blocksRaycasts&&!circle.raycastTarget&&!label.raycastTarget,"marker has no input interception");
            Check(TextMeshProUGUI.Bindings[0]=="material"&&TextMeshProUGUI.Bindings[1]=="font"&&ReferenceEquals(label.fontSharedMaterial,font.material),"shared material precedes font without editing asset");
            Check(texture.Pixels[0].a==0&&texture.Pixels[32*64+32].a==255&&texture.MadeUnreadable,"actual generated circle mask and upload");
            Check(label.text=="…"&&label.isOrthographic&&!label.richText,"ellipsis and orthographic UI label");
            Check(view.Apply(anchor,"…",new Color(.5f,.4f,.3f,.8f),.25f,-.1f),"same renderer accepts latest grey tint and fade");
            Check(ReferenceEquals(sprite,Get(view,"sprite"))&&ReferenceEquals(texture,Get(view,"texture")),"grey state reuses same circle artwork");
            Near(label.color.r,.5,"grey glyph r");Near(circle.color.r,.04,"circle receives same tint");Near(group.alpha,.25,"fade applies to whole marker");
            anchor.localScale=new Vector3(.1f,.1f,.1f);anchor.position=new Vector3(4,6,0);view.Apply(anchor,"!",new Color(1,.75f,.15f,1),1,0);
            Near(root.transform.localScale.x*128,.85,"depth scale changes do not change world diameter");Check(label.text=="!"&&label.color.g==.75f,"story glyph and supplied gold tint");
            view.HideImmediate();Check(!root.activeSelf&&group.alpha==0,"hide is immediate with no view-owned timer");
            Check(view.Apply(anchor,"…",new Color(1,1,1,1),1,0),"hidden view reuses resources on return");
            view.Clear();Check(root.Destroyed&&sprite.Destroyed&&texture.Destroyed&&!font.Destroyed&&!font.material.Destroyed,"clear destroys only owned world/text/circle resources");
            view.Clear();Check(Get(view,"root")==null&&Get(view,"texture")==null,"cleanup idempotent");
            view.enabled=false;Check(!view.Apply(anchor,"!",new Color(1,1,1,1),1,0),"disabled view cannot recreate resources");
            Console.WriteLine("HEAD_MARKER_VIEW_PASS checks="+checks+" nativeUnity=false actualView=true");return 0;
        }
        catch(Exception ex){Console.Error.WriteLine("HEAD_MARKER_VIEW_FAILED "+ex);return 1;}
    }
}
