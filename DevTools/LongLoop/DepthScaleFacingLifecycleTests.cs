using System;
using System.Reflection;
using EatWhat.LongLoop;
using EatWhat.LongLoop.Configuration;
using UnityEngine;

// Real production methods, explicit offline lifecycle ordering; not a Unity lifecycle measurement.
internal static class DepthScaleFacingLifecycleTests
{
    private static int count;
    private sealed class Case
    {
        public Transform Visual;
        public DepthProbe Depth;
        public ControllerProbe Controller;
        public TomatoSpineAvatar Avatar;
    }
    private static void Set(object target,Type type,string field,object value)
    { type.GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value); }
    private static void Near(float actual,float expected,string label)
    {
        count++;
        if(Math.Abs(actual-expected)>.0001f)
            throw new Exception(label+": expected "+expected+", actual "+actual);
    }
    private static Case Setup(bool depthFirst,float coefficient)
    {
        var c=new Case();
        c.Visual=new Transform {position=new Vector3(0f,2f,0f),localScale=new Vector3(2f,3f,1f)};
        c.Depth=new DepthProbe {transform=c.Visual,isActiveAndEnabled=false};
        c.Visual.components[typeof(DepthScale)]=c.Depth;
        c.Depth.ConfigureSources(new Transform(),new DepthScaleConfig {DepthScaleFactor=coefficient});
        c.Avatar=new TomatoSpineAvatar();
        Set(c.Avatar,typeof(TomatoSpineAvatar),"facingVisualRoot",c.Visual);
        c.Controller=new ControllerProbe();
        Set(c.Controller,typeof(LongLoopPlayerController),"visualRoot",c.Visual);
        c.Controller.ConfigureInterfaces(null,c.Avatar);
        if(depthFirst)c.Depth.Enable();
        c.Controller.Boot();
        typeof(TomatoSpineAvatar).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic)
            .Invoke(c.Avatar,null);
        if(!depthFirst)c.Depth.Enable();
        return c;
    }
    private static void Flip(Case c,bool avatar,bool right)
    {
        if(avatar)c.Avatar.SetFacing(right);
        else c.Controller.SetHorizontalInput(right ? 1f : -1f);
    }
    private static void RemoveDepth(Case c,bool destroy)
    {
        c.Depth.Disable();
        if(destroy)
        {
            c.Depth.Destroy();
            c.Visual.components.Remove(typeof(DepthScale)); // Boundary models destroyed GetComponent=null.
        }
    }
    public static int Main()
    {
        int failures=0;
        foreach(bool avatar in new[]{false,true})
        {
            string label=avatar ? "CASE:avatar-depth-before-awake-disabled-left" :
                "CASE:controller-depth-before-awake-disabled-left";
            var c=Setup(true,.03f);
            RemoveDepth(c,false);
            Flip(c,avatar,false);
            try {Near(c.Visual.localScale.x,-2f,label);}
            catch(Exception ex){failures++;Console.WriteLine(ex.Message);}
            c.Depth.Destroy();
        }
        Console.WriteLine("LIFECYCLE_REGRESSION_FAILURES="+failures);
        return failures==0 ? 0 : 1;
    }
    public static void Run()
    {
        count=0;
        foreach(bool first in new[]{false,true})
        foreach(float coefficient in new[]{.03f,.5f,.75f})
        foreach(bool avatar in new[]{false,true})
        foreach(bool destroy in new[]{false,true})
        {
            var c=Setup(first,coefficient);
            string label="order="+first+" k="+coefficient+" avatar="+avatar+" destroy="+destroy;
            Near(c.Visual.localScale.x,2f*(1f-2f*coefficient),"active "+label);
            RemoveDepth(c,destroy);
            Flip(c,avatar,false); Near(c.Visual.localScale.x,-2f,"inactive left "+label);
            Flip(c,avatar,true); Near(c.Visual.localScale.x,2f,"inactive right "+label);
            Flip(c,avatar,false);
            if(!destroy)
            {
                c.Depth.Enable();
                Near(c.Visual.localScale.x,-2f*(1f-2f*coefficient),"reenable remembers disabled facing "+label);
                for(int i=0;i<8;i++)c.Depth.Frame();
                Near(c.Visual.localScale.x,-2f*(1f-2f*coefficient),"no accumulation "+label);
                RemoveDepth(c,true);
                Flip(c,avatar,true); Near(c.Visual.localScale.x,2f,"destroy after reenable "+label);
            }
        }
        foreach(bool avatar in new[]{false,true})
        foreach(bool beforeDisable in new[]{false,true})
        foreach(bool destroy in new[]{false,true})
        {
            var c=Setup(true,.03f);
            if(beforeDisable)
            {
                c.Visual.localScale=new Vector3(7f,8f,9f);
                c.Depth.Frame();
            }
            RemoveDepth(c,destroy);
            if(!beforeDisable)c.Visual.localScale=new Vector3(7f,8f,9f);
            Flip(c,avatar,false);
            Near(c.Visual.localScale.x,-7f,"external amplitude survives fallback");
            Near(c.Visual.localScale.y,8f,"external Y untouched by fallback");
            Near(c.Visual.localScale.z,9f,"external Z untouched by fallback");
            if(!destroy)c.Depth.Destroy();
        }
        // No component has ever existed: preserve the legacy capture-once contract.
        var legacy=new Transform {localScale=new Vector3(2f,3f,1f)};
        var adapter=new TomatoSpineAvatar();
        Set(adapter,typeof(TomatoSpineAvatar),"facingVisualRoot",legacy);
        adapter.SetFacing(true); legacy.localScale=new Vector3(7f,3f,1f);
        adapter.SetFacing(false); Near(legacy.localScale.x,-2f,"no-depth avatar legacy cache");
        var controller=new ControllerProbe();
        Set(controller,typeof(LongLoopPlayerController),"visualRoot",legacy);
        controller.Boot(); legacy.localScale=new Vector3(7f,3f,1f);
        controller.SetHorizontalInput(-1f);Near(legacy.localScale.x,-2f,"no-depth controller legacy cache");
        Console.WriteLine("PASS "+count+" actual-source facing lifecycle assertions");
    }
}
