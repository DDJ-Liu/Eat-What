using System;
using System.Reflection;
using EatWhat.LongLoop;
using EatWhat.LongLoop.Configuration;
using UnityEngine;
using UnityEngine.Rendering;

internal sealed class SortProbe : YSorter
{
    public void Enable() { isActiveAndEnabled=true; base.OnEnable(); }
    public void Begin() { base.Start(); }
    public void Frame() { base.LateUpdate(); }
    public void Disable() { isActiveAndEnabled=false; base.OnDisable(); }
    public void Destroy() { base.OnDestroy(); }
}
internal sealed class SortDepthProbe : DepthScale
{
    public void Enable() { isActiveAndEnabled=true; base.OnEnable(); }
    public void Frame() { base.LateUpdate(); }
    public void Disable() { isActiveAndEnabled=false; base.OnDisable(); }
}
internal static class YSorterLogicTests
{
    private static int count;
    private static void Check(bool condition,string label)
    { count++; if(!condition)throw new Exception(label); }
    private static void Equal(int value,int expected,string label)
    { count++;if(value!=expected)throw new Exception(label+": "+value+" != "+expected); }
    private static void Near(float value,float expected,string label)
    { count++;if(Math.Abs(value-expected)>.0001f)throw new Exception(label); }
    private static int Order(int baseline,float y,float precision=100f,float offset=0f)
    {
        int order;bool saturated;
        Check(YSortMath.TryOrder(baseline,y,precision,offset,out order,out saturated),"valid math");
        return order;
    }
    private static SortProbe Sort(Renderer renderer,SortingGroup group,Transform foot,bool bake=false,int baseline=0)
    {
        var c=new SortProbe {isActiveAndEnabled=false};
        c.ConfigureTargets(renderer,group,foot);c.BaseOrder=baseline;c.BakeOnStart=bake;
        c.Enable();c.Begin();return c;
    }
    public static int Main()
    {
        Equal(Order(10,0f),10,"ground zero");
        Equal(Order(10,1.25f),-115,"positive Y behind");
        Equal(Order(10,-1.25f),135,"negative Y in front");
        Equal(Order(10,2f,0f),10,"zero precision retains manual base");
        Equal(Order(10,.5f,1f),10,"positive even zero midpoint");
        Equal(Order(10,1.5f,1f),8,"positive midpoint to even two");
        Equal(Order(10,-.5f,1f),10,"negative even zero midpoint");
        Equal(Order(10,-1.5f,1f),12,"negative midpoint to even minus two");
        Equal(Order(10,2f,100f,1f),-290,"positive offset is farther");
        Equal(Order(10,2f,100f,-1f),-90,"negative offset is nearer");
        Equal(Order(11,2f)-Order(10,2f),1,"same Y preserves hand configured tie");
        bool sat;int value;
        Check(YSortMath.TryOrder(int.MaxValue,0f,100f,0f,out value,out sat)&&sat,"int max saturation");
        Equal(value,32767,"Unity upper range");
        Check(YSortMath.TryOrder(int.MinValue,0f,100f,0f,out value,out sat)&&sat,"int min saturation");
        Equal(value,-32768,"Unity lower range");
        Equal(Order(0,float.MaxValue,float.MaxValue),-32768,"huge positive never integer wraps");
        Equal(Order(0,-float.MaxValue,float.MaxValue),32767,"huge negative never integer wraps");
        Check(YSortMath.TryOrder(32767,0f,1f,0f,out value,out sat)&&!sat,"exact boundary not saturated");
        Check(!YSortMath.TryOrder(0,float.NaN,100f,0f,out value,out sat),"NaN foot rejected");
        Check(!YSortMath.TryOrder(0,0f,float.PositiveInfinity,0f,out value,out sat),"infinite precision rejected");
        Check(!YSortMath.TryOrder(0,0f,-1f,0f,out value,out sat),"negative precision rejected");
        Check(!YSortMath.TryOrder(0,0f,100f,float.NaN,out value,out sat),"NaN offset rejected");

        var foot=new Transform {position=new Vector3(0f,2f,0f)};
        var renderer=new Renderer {sortingOrder=17,sortingLayerID=30};
        var c=Sort(renderer,null,foot,false,6);
        Equal(renderer.sortingOrder,-194,"serialized base, not target order");
        int writes=renderer.Writes;
        for(int i=0;i<30;i++)c.Frame();
        Equal(renderer.Writes,writes,"unchanged dynamic input no writes");
        foot.position=new Vector3(0f,3f,0f);c.Frame();
        Equal(renderer.sortingOrder,-294,"dynamic changed foot");
        foot.position=new Vector3(0f,2f,0f);c.Frame();
        Equal(renderer.sortingOrder,-194,"back to foot never accumulates order");
        Equal(c.BaseOrder,6,"stable manual base retained");
        Equal(renderer.sortingLayerID,30,"renderer layer retained");
        c.Precision=float.NaN;
        Check(c.Status==YSorterStatus.InvalidInput,"invalid component status");
        Equal(renderer.sortingOrder,-194,"invalid input holds last owned order");
        c.Precision=100f;
        Check(c.Status==YSorterStatus.Applied,"same prior valid value clears invalid status");
        c.DepthOffset=1f;Equal(renderer.sortingOrder,-294,"dynamic offset change");
        c.Disable();Equal(renderer.sortingOrder,17,"disable restores original saved target");
        c.Enable();Equal(renderer.sortingOrder,-294,"reenable starts new ownership");
        c.Destroy();Equal(renderer.sortingOrder,17,"destroy releases current owned order");

        var staticRenderer=new Renderer {sortingOrder=9,sortingLayerID=11};
        foot.position=new Vector3(0f,2f,0f);
        var baked=Sort(staticRenderer,null,foot,true);
        writes=staticRenderer.Writes;
        foot.position=new Vector3(0f,4f,0f);baked.BaseOrder=5;
        for(int i=0;i<20;i++)baked.Frame();
        Equal(staticRenderer.Writes,writes,"bake once ignores later foot and parameter changes");
        Equal(staticRenderer.sortingOrder,-200,"baked original sample");
        baked.Rebake();Equal(staticRenderer.sortingOrder,-395,"explicit rebake from stable new base");
        baked.Disable();Equal(staticRenderer.sortingOrder,9,"bake release");
        baked.Enable();Equal(staticRenderer.sortingOrder,-395,"bake once on reenable");
        var replacement=new Renderer {sortingOrder=12,sortingLayerID=20};
        typeof(YSorter).GetField("targetRenderer",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(baked,replacement);
        baked.Frame();
        Equal(staticRenderer.sortingOrder,9,"target replacement releases old exact renderer");
        Equal(replacement.sortingOrder,-395,"new static target bakes once");
        baked.Disable();Equal(replacement.sortingOrder,12,"new target restore identity");

        var group=new SortingGroup {sortingOrder=21,sortingLayerID=40,enabled=false};
        var nonTarget=new Renderer {sortingOrder=7,sortingLayerID=50};
        var untouched=new Renderer {sortingOrder=99,sortingLayerID=60};
        foot.position=new Vector3(0f,1f,0f);
        var grouped=Sort(nonTarget,group,foot,false,3);
        Equal(group.sortingOrder,-97,"group priority");
        Equal(nonTarget.sortingOrder,7,"renderer ignored when group set");
        Equal(untouched.sortingOrder,99,"non target untouched");
        Equal(group.sortingLayerID,40,"group layer unchanged");
        Equal(nonTarget.sortingLayerID,50,"ignored layer unchanged");
        Check(!group.enabled,"group enabled flag unchanged");
        grouped.ConfigureTargets(nonTarget,null,foot);
        Equal(group.sortingOrder,21,"switch group releases exact target");
        Equal(nonTarget.sortingOrder,-97,"renderer selected only after explicit switch");
        nonTarget.sortingOrder=765;grouped.Frame();
        Check(grouped.Status==YSorterStatus.ExternalChange,"external order yields");
        grouped.Disable();Equal(nonTarget.sortingOrder,765,"external order not restored over");
        grouped.Enable();Equal(nonTarget.sortingOrder,-97,"explicit reenable applies manual base");
        grouped.Disable();Equal(nonTarget.sortingOrder,765,"reenable restore point is external saved value");
        var layerOwner=Sort(untouched,null,foot);
        untouched.sortingLayerID=61;layerOwner.Frame();
        Check(layerOwner.Status==YSorterStatus.ExternalChange,"external layer change yields order ownership");
        int externalLayerOrder=untouched.sortingOrder;
        layerOwner.Disable();Equal(untouched.sortingLayerID,61,"external layer never restored");
        Equal(untouched.sortingOrder,externalLayerOrder,"no order restoration into changed layer");
        var direct=Sort(nonTarget,null,foot);nonTarget.sortingOrder=777;
        direct.Disable();Equal(nonTarget.sortingOrder,777,"direct disable preserves later external order");

        var noTarget=Sort(null,null,foot);
        Check(noTarget.Status==YSorterStatus.MissingTarget,"missing target explicit");noTarget.Disable();
        var noFoot=Sort(nonTarget,null,null);
        Check(noFoot.Status==YSorterStatus.MissingFoot,"missing foot never guesses host pivot");
        Equal(nonTarget.sortingOrder,777,"missing foot no writes");noFoot.Disable();
        var saturated=Sort(nonTarget,null,new Transform {position=new Vector3(0f,float.MaxValue,0f)});
        Check(saturated.Status==YSorterStatus.Saturated,"saturation readback status");
        Equal(nonTarget.sortingOrder,-32768,"component bounded target");saturated.Disable();

        // Actual DepthScale moves only visual offset; YSorter reads a separately assigned logical foot.
        var root=new Transform {position=new Vector3(0f,2f,0f)};
        var visual=new Transform {parent=root,localPosition=new Vector3(0f,1f,0f)};
        var depth=new SortDepthProbe {transform=visual,isActiveAndEnabled=false};
        depth.ConfigureSources(new Transform(),new DepthScaleConfig());
        depth.ConfigureFootOffset(new Vector3(0f,-1f,0f));depth.Enable();
        var actualRenderer=new Renderer {sortingOrder=5,sortingLayerID=70};
        var independent=Sort(actualRenderer,null,root);independent.transform=visual;
        Near(visual.position.y,2.94f,"depth has visual position compensation");
        Equal(actualRenderer.sortingOrder,-200,"sorting reads unscaled foot");
        depth.OverrideFactor=.2f;depth.UseOverrideFactor=true;independent.Frame();
        Near(root.position.y,2f,"logical foot not moved by depth");
        Equal(actualRenderer.sortingOrder,-200,"depth parameter cannot feed back into sorting");
        root.position=new Vector3(0f,3f,0f);depth.Frame();independent.Frame();
        Equal(actualRenderer.sortingOrder,-300,"logical foot movement updates independently");
        independent.Disable();Equal(actualRenderer.sortingOrder,5,"integration renderer restored");depth.Disable();
        Console.WriteLine("PASS "+count+" actual-source Y sorting assertions");
        return 0;
    }
}
