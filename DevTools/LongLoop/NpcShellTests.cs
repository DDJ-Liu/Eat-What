using System;
using EatWhat.LongLoop;
using EatWhat.LongLoop.Configuration;
using UnityEngine;

internal static class NpcShellTests
{
    private static int count;
    private static void Yes(bool value,string label) { count++; if (!value) throw new Exception(label); }
    private static void Near(float a,float b,string label)
    { count++; if (Math.Abs(a-b)>.0001f) throw new Exception(label+": "+a+" vs "+b); }
    private static void Same(Vector3 a,Vector3 b,string label)
    { Near(a.x,b.x,label+" x"); Near(a.y,b.y,label+" y"); Near(a.z,b.z,label+" z"); }
    private static GameObject Child(Transform parent,string name)
    { var go=new GameObject(name); go.transform.SetParent(parent,false); return go; }
    private sealed class Rig
    {
        public GameObject Root,Mount,Foot,Head;
        public NpcShell Shell;
        public Rig()
        {
            Root=new GameObject("npc"); Mount=Child(Root.transform,"visual");
            Foot=Child(Root.transform,"foot"); Head=Child(Root.transform,"head");
            Mount.transform.localPosition=new Vector3(0,2,0);
            Mount.transform.localScale=new Vector3(2,3,4);
            Foot.transform.localPosition=new Vector3(0,2,0); Head.transform.localPosition=new Vector3(0,4,0);
            Shell=Root.Add<NpcShell>(); Shell.ConfigureReferences(Foot.transform,Mount.transform,Head.transform);
        }
    }
    private sealed class DepthProbe : DepthScale
    {
        public void Enable() { isActiveAndEnabled=true; OnEnable(); }
        public void Disable() { isActiveAndEnabled=false; OnDisable(); }
        public void Release() { OnDestroy(); }
    }
    private static DepthProbe Attach(Rig r,bool active,float coefficient)
    {
        var depth=new DepthProbe {gameObject=r.Mount,isActiveAndEnabled=false};
        r.Mount.transform.Components[typeof(DepthScale)]=depth;
        depth.ConfigureSources(new GameObject("ground").transform,new DepthScaleConfig {DepthScaleFactor=coefficient});
        if (active) depth.Enable();
        return depth;
    }
    public static int Main()
    {
        var r=new Rig(); var oldA=Child(r.Mount.transform,"old-a"); var oldB=Child(r.Mount.transform,"old-b");
        oldB.SetActive(false);
        Yes(r.Shell.Foot==r.Foot.transform&&r.Shell.HeadAnchor==r.Head.transform&&r.Shell.VisualMount==r.Mount.transform,"explicit mount references");
        Yes(r.Shell.SetVisual(null)==oldA&&r.Mount.transform.childCount==2,"null preserves all authored appearances");
        Yes(oldA.activeSelf&&!oldB.activeSelf,"null preserves active state");
        var prefab=new GameObject("appearance"); prefab.transform.localPosition=new Vector3(1,2,3);
        prefab.transform.localScale=new Vector3(5,6,7); Child(prefab.transform,"mesh");
        var rootPosition=r.Root.transform.position; var footPosition=r.Foot.transform.position; var headPosition=r.Head.transform.position;
        r.Shell.SetFacing(false); var instance=r.Shell.SetVisual(prefab);
        Yes(instance!=prefab&&r.Shell.CurrentVisual==instance&&r.Mount.transform.childCount==1,"only one new immediate child");
        Yes(instance.transform.parent==r.Mount.transform&&instance.transform.childCount==1,"clone hierarchy mounted");
        Same(instance.transform.localScale,prefab.transform.localScale,"prefab local scale retained");
        Same(instance.transform.localPosition,prefab.transform.localPosition,"prefab local offset retained");
        Yes(oldA.transform.parent==null&&oldB.transform.parent==null&&!oldA.activeSelf&&!oldB.activeSelf,"old children detached and inactive before deferred destroy");
        Yes(UnityEngine.Object.PendingDestruction.Contains(oldA)&&UnityEngine.Object.PendingDestruction.Contains(oldB),"real destruction request boundary observed");
        Near(r.Mount.transform.localScale.x,-2,"stored facing carried to replacement");
        Yes(r.Shell.SetVisual(null)==instance&&r.Mount.transform.childCount==1,"null after replacement preserves instance");
        var again=r.Shell.SetVisual(prefab);
        Yes(again!=instance&&r.Mount.transform.childCount==1&&!instance.activeSelf,"repeated replacement retires previous clone");
        Same(r.Root.transform.position,rootPosition,"root position preserved"); Same(r.Foot.transform.position,footPosition,"foot preserved"); Same(r.Head.transform.position,headPosition,"head preserved");
        foreach(float x in new[]{2f,-2f,0f,.00001f,7f})
        {
            var simple=new Rig(); simple.Mount.transform.localScale=new Vector3(x,8,9);
            Yes(simple.Shell.IsFacingRight==(x>=0),"authored facing read without startup overwrite");
            simple.Shell.SetFacing(false); Near(simple.Mount.transform.localScale.x,-Math.Abs(x),"fallback preserves magnitude left");
            simple.Shell.SetFacing(true); Near(simple.Mount.transform.localScale.x,Math.Abs(x),"fallback preserves magnitude right");
            Near(simple.Mount.transform.localScale.y,8,"fallback Y preserved"); Near(simple.Mount.transform.localScale.z,9,"fallback Z preserved");
            Yes(simple.Mount.transform.childCount==0,"no appearance needed to face safely");
        }
        var missing=new GameObject("unconfigured").Add<NpcShell>();
        Yes(!missing.SetFacing(false)&&missing.SetVisual(null)==null,"no mount safe");
        Yes(missing.SetVisual(prefab)==null,"unbound replacement diagnostic");
        var bad=new Rig(); var safe=Child(bad.Mount.transform,"safe");
        bad.Shell.ConfigureReferences(bad.Foot.transform,bad.Root.transform,bad.Head.transform);
        Yes(!bad.Shell.SetFacing(false)&&bad.Shell.SetVisual(prefab)==null&&safe.transform.parent==bad.Mount.transform,"root cannot be destructively used as mount");
        bad.Shell.ConfigureReferences(bad.Foot.transform,bad.Mount.transform,safe.transform);
        Yes(bad.Shell.SetVisual(prefab)==null&&bad.Mount.transform.childCount==1,"head reference cannot be replaced as appearance");
        var edit=new Rig(); Child(edit.Mount.transform,"authored"); Application.isPlaying=false;
        Yes(edit.Shell.SetVisual(prefab)==null&&edit.Mount.transform.childCount==1,"no Edit asset mutation path"); Application.isPlaying=true;
        // Actual DepthScale with shell-first/depth-first, disable/enable and removal paths.
        foreach(bool first in new[]{false,true}) foreach(float coefficient in new[]{.03f,.1f,.25f})
        {
            var c=new Rig(); var depth=Attach(c,false,coefficient);
            if(first) depth.Enable();
            c.Shell.SetFacing(false); if(!first) depth.Enable();
            float factor=1-2*coefficient;
            Near(c.Mount.transform.localScale.x,-2*factor,"active depth left");
            Near(c.Mount.transform.localScale.y,3*factor,"active depth Y"); Near(c.Mount.transform.localScale.z,4*factor,"active depth Z");
            c.Shell.SetFacing(true); depth.Refresh();
            Near(c.Mount.transform.localScale.x,2*factor,"active depth right no compounding");
            Yes(depth.Status==DepthScaleStatus.Applied,"facing did not lose depth ownership");
            depth.Disable(); c.Shell.SetFacing(false); Near(c.Mount.transform.localScale.x,-2,"inactive depth fallback baseline");
            depth.Enable(); depth.Refresh(); Near(c.Mount.transform.localScale.x,-2*factor,"reenable uses inactive facing");
            c.Shell.SetVisual(prefab); depth.Refresh(); Near(c.Mount.transform.localScale.x,-2*factor,"replacement keeps depth output");
            Yes(depth.Status==DepthScaleStatus.Applied,"replacement does not steal scale ownership");
            depth.Disable(); depth.Release(); c.Mount.transform.Components.Remove(typeof(DepthScale));
            c.Mount.transform.localScale=new Vector3(7,8,9); c.Shell.SetFacing(false);
            Same(c.Mount.transform.localScale,new Vector3(-7,8,9),"removed depth later external magnitude preserved");
        }
        var offset=new Rig(); var d=Attach(offset,false,.1f);
        d.ConfigureFootOffset(new Vector3(.25f,-.5f,0)); d.Enable(); var worldFoot=d.FootWorldPosition;
        var beforeScale=offset.Mount.transform.localScale; offset.Shell.SetFacing(false); d.Refresh();
        Same(offset.Mount.transform.TransformPoint(new Vector3(.25f,-.5f,0)),worldFoot,"nonzero visual foot remains pinned after flip");
        Near(Math.Abs(offset.Mount.transform.localScale.x),Math.Abs(beforeScale.x),"offset flip absolute X unchanged");
        Yes(d.Status==DepthScaleStatus.Applied,"offset path keeps ownership");
        offset.Mount.transform.localScale=new Vector3(7,8,9); d.Refresh(); offset.Shell.SetFacing(true);
        Yes(d.Status==DepthScaleStatus.ExternalChange,"existing external change is retained");
        Same(offset.Mount.transform.localScale,new Vector3(7,8,9),"active lost ownership not overwritten by fallback");
        d.Disable(); d.Release();
        Yes(Debug.Warnings.Count>=4,"invalid bindings and Edit calls diagnosed");
        Console.WriteLine("PASS "+count+" NPC shell actual-source checks; Unity hierarchy/clone/destroy substituted");
        return 0;
    }
}
