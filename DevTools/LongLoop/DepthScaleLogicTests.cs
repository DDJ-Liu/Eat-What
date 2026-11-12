using System;
using System.Reflection;
using EatWhat.LongLoop;
using EatWhat.LongLoop.Configuration;
using UnityEngine;

// Executes production source using declared planar Unity/Spine boundaries, not native lifecycle proof.
internal sealed class DepthProbe : DepthScale
{
    public void Enable() { isActiveAndEnabled=true; base.OnEnable(); }
    public void Frame() { base.LateUpdate(); }
    public void Disable() { isActiveAndEnabled=false; base.OnDisable(); }
    public void Destroy() { base.OnDestroy(); }
}
internal sealed class DepthOverrideProbe : MonoBehaviour, IDepthOverride
{
    public bool Active;
    public float Coefficient;
    public bool TryGetDepthScaleFactor(Vector3 foot, out float value)
    { value=Coefficient; return Active; }
}
internal static class DepthScaleLogicTests
{
    private static int count;
    private static void Check(bool value,string label)
    { count++; if(!value) throw new Exception(label); }
    private static void Near(float actual,float expected,string label)
    { count++; if(Math.Abs(actual-expected)>0.0001f) throw new Exception(label+": "+actual+" != "+expected); }
    private static void Point(Vector3 a,Vector3 b,string label)
    { Near(a.x,b.x,label+" x"); Near(a.y,b.y,label+" y"); Near(a.z,b.z,label+" z"); }
    private static void Set(object target,Type type,string field,object value)
    { type.GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value); }
    private static DepthProbe NewDepth(Transform visual,Transform line,DepthScaleConfig config,Vector3 offset)
    {
        var d=new DepthProbe {transform=visual,isActiveAndEnabled=false};
        visual.components[typeof(DepthScale)]=d;
        d.ConfigureSources(line,config);
        d.ConfigureFootOffset(offset);
        return d;
    }
    private static void Bind(DepthScaleConfig config,Transform line)
    {
        StreetConfigs.Bind(config,new CameraModeConfig(),new CameraFollowConfig(),
            new SceneBoundsConfig(),line,new Collider2D());
    }
    private static int Subscriptions(object target,Type type,string field,object subscriber)
    {
        var action=(Delegate)type.GetField(field,BindingFlags.Static|BindingFlags.Instance|BindingFlags.NonPublic)
            .GetValue(target);
        int n=0;
        if(action!=null) foreach(var d in action.GetInvocationList()) if(ReferenceEquals(d.Target,subscriber)) n++;
        return n;
    }
    public static int Main()
    {
        WalkAreaLogicTests.Main(); // Existing 61 production movement assertions remain unchanged.
        StreetConfigs.Unbind();
        float f;
        Check(DepthScaleMath.TryFactor(0f,0f,.03f,out f),"baseline finite"); Near(f,1f,"baseline factor");
        DepthScaleMath.TryFactor(0f,2f,.03f,out f); Near(f,.94f,"behind factor");
        DepthScaleMath.TryFactor(0f,-2f,.03f,out f); Near(f,1.06f,"front factor");
        DepthScaleMath.TryFactor(0f,2f,1f,out f); Near(f,-1f,"negative factor is not clamped");
        DepthScaleMath.TryFactor(0f,1f,1f,out f); Near(f,0f,"zero factor allowed");
        Check(!DepthScaleMath.TryFactor(0f,0f,float.NaN,out f),"NaN coefficient rejected");
        Check(!DepthScaleMath.TryFactor(float.MaxValue,-float.MaxValue,float.MaxValue,out f),"overflow rejected");

        var config=new DepthScaleConfig();
        var line=new Transform();
        var visual=new Transform {localScale=new Vector3(2f,3f,4f),localPosition=new Vector3(0f,2f,0f)};
        var d=NewDepth(visual,line,config,Vector3.zero);
        d.Enable();
        Point(visual.localScale,new Vector3(1.88f,2.82f,3.76f),"saved nonunit baseline");
        for(int i=0;i<40;i++) d.Frame();
        Near(visual.localScale.x,1.88f,"no per-frame accumulation");
        visual.position=new Vector3(0f,-2f,0f); d.Frame();
        Near(d.AppliedFactor,1.06f,"moving independent empty object");
        config.DepthScaleFactor=.1f;
        Near(d.AppliedFactor,1.2f,"local config update event");
        d.OverrideFactor=0f; d.UseOverrideFactor=true;
        Near(d.AppliedFactor,1f,"enabled zero override differs from disabled");
        d.OverrideFactor=.2f; Near(d.AppliedFactor,1.4f,"override updates immediately");
        d.UseOverrideFactor=false; Near(d.AppliedFactor,1.2f,"disable override returns to config");
        var provider=new DepthOverrideProbe {Active=true,Coefficient=.3f};
        d.ConfigureSources(line,config,provider); Near(d.AppliedFactor,1.6f,"future interface coefficient");
        provider.Active=false; d.Frame(); Near(d.AppliedFactor,1.2f,"inactive override provider falls back");
        config.DepthScaleFactor=float.NaN;
        Check(d.Status==DepthScaleStatus.InvalidInput,"invalid config status");
        Point(visual.localScale,new Vector3(2f,3f,4f),"invalid config restores own baseline");
        config.DepthScaleFactor=.03f;
        d.Disable(); Point(visual.localScale,new Vector3(2f,3f,4f),"disable restores baseline");
        d.Enable(); d.Disable(); d.Destroy();
        Check(Subscriptions(config,typeof(StreetConfigAsset),"Changed",d)==0,"local listener removed");

        var parent=new Transform {localPosition=new Vector3(4f,7f,2f),
            localScale=new Vector3(2f,3f,1f),localRotation=Quaternion.Euler(0f,0f,20f)};
        var offset=new Vector3(.25f,-1f,0f);
        var child=new Transform {parent=parent,localPosition=new Vector3(1f,2f,0f),
            localScale=new Vector3(2f,3f,1f),localRotation=Quaternion.Euler(0f,0f,15f)};
        var initialFoot=child.TransformPoint(offset);
        line.position=new Vector3(0f,initialFoot.y+2f,0f);
        var footDepth=NewDepth(child,line,config,offset); footDepth.Enable();
        Near(footDepth.AppliedFactor,1.06f,"world foot with parent scale rotation");
        Point(child.TransformPoint(offset),initialFoot,"foot preserved through scale");
        for(int i=0;i<40;i++) footDepth.Frame();
        Point(child.TransformPoint(offset),initialFoot,"foot has no scale feedback");
        DepthScale.TrySetFacing(child,false);
        Point(child.TransformPoint(offset),initialFoot,"offcenter foot stays through facing");
        parent.localPosition=parent.localPosition+new Vector3(0f,1f,0f); footDepth.Frame();
        Point(child.TransformPoint(offset),initialFoot+new Vector3(0f,1f,0f),"parent motion stays independent");
        var parentSaved=parent.localScale;
        footDepth.Disable(); Point(parent.localScale,parentSaved,"parent scale never owned");
        Point(child.TransformPoint(offset),initialFoot+new Vector3(0f,1f,0f),"disable preserves foot");
        footDepth.Enable(); footDepth.Frame();
        Point(child.TransformPoint(offset),initialFoot+new Vector3(0f,1f,0f),"reenable foot stable");
        footDepth.Disable();

        var directlyChanged=new Transform {localScale=new Vector3(2f,3f,1f),position=new Vector3(0f,2f,0f)};
        var direct=NewDepth(directlyChanged,line,config,new Vector3(0f,-1f,0f)); direct.Enable();
        directlyChanged.localScale=new Vector3(9f,8f,7f);
        var externalPosition=directlyChanged.localPosition;
        direct.Disable();
        Point(directlyChanged.localScale,new Vector3(9f,8f,7f),"direct disable respects external scale");
        Point(directlyChanged.localPosition,externalPosition,"direct disable leaves offset when ownership unknown");

        line.position=Vector3.zero;
        var currentConfig=new DepthScaleConfig();
        Bind(currentConfig,line);
        var currentVisual=new Transform {position=new Vector3(0f,2f,0f)};
        var current=NewDepth(currentVisual,null,null,Vector3.zero); current.Enable();
        Near(current.AppliedFactor,.94f,"current config source");
        Check(Subscriptions(null,typeof(StreetConfigs),"Changed",current)==1,"one Current subscription");
        current.Enable();
        Check(Subscriptions(null,typeof(StreetConfigs),"Changed",current)==1,"repeated enable has no duplicate");
        current.ConfigureSources(null,currentConfig);
        Check(Subscriptions(currentConfig,typeof(StreetConfigAsset),"Changed",current)==0,
            "same local and Current config avoids duplicate channel");
        current.ConfigureSources(null,null);
        var replacement=new DepthScaleConfig {DepthScaleFactor=.1f};
        Bind(replacement,line); Near(current.AppliedFactor,.8f,"Current replacement consumed");
        currentConfig.DepthScaleFactor=.4f; Near(current.AppliedFactor,.8f,"old asset detached");
        StreetConfigs.Unbind();
        Check(current.Status==DepthScaleStatus.MissingGroundLine,"unbind missing line explicit");
        Near(currentVisual.localScale.x,1f,"unbind removes own multiplier");
        current.Disable();
        Check(Subscriptions(null,typeof(StreetConfigs),"Changed",current)==0,"Current subscription removed");

        // Both real facing writers share the same DepthScale without losing signed factors.
        config.DepthScaleFactor=.03f;
        var faceRoot=new Transform {position=new Vector3(0f,2f,0f),localScale=new Vector3(2f,3f,1f)};
        var depth=NewDepth(faceRoot,line,config,Vector3.zero);
        var avatar=new TomatoSpineAvatar();
        Set(avatar,typeof(TomatoSpineAvatar),"facingVisualRoot",faceRoot);
        var controller=new ControllerProbe();
        Set(controller,typeof(LongLoopPlayerController),"visualRoot",faceRoot);
        controller.ConfigureInterfaces(null,avatar); controller.Boot(); // Legacy first, depth disabled.
        depth.Enable();
        controller.SetHorizontalInput(-1f); Near(faceRoot.localScale.x,-1.88f,"controller+avatar left magnitude");
        avatar.SetFacing(true); Near(faceRoot.localScale.x,1.88f,"avatar then depth right");
        depth.Frame(); Near(faceRoot.localScale.x,1.88f,"late update preserves right magnitude");
        controller.SetHorizontalInput(-1f); depth.Frame(); avatar.SetFacing(false);
        Near(faceRoot.localScale.x,-1.88f,"different caller order keeps left factor");
        controller.SetMoveInput(new Vector2(0f,1f)); depth.Frame();
        Near(faceRoot.localScale.x,-1.88f,"vertical does not turn or erase depth");
        config.DepthScaleFactor=1f; // y=2 => factor -1, preserve inversion separately from facing.
        Near(faceRoot.localScale.x,2f,"left facing times negative factor");
        avatar.SetFacing(true); Near(faceRoot.localScale.x,-2f,"right facing preserves negative factor");
        Near(faceRoot.localScale.y,-3f,"negative factor applies to baseline Y");
        config.DepthScaleFactor=.5f; Near(faceRoot.localScale.x,0f,"zero factor safe");
        avatar.SetFacing(false); config.DepthScaleFactor=.03f;
        Near(faceRoot.localScale.x,-1.88f,"zero factor does not destroy cached baseline");
        depth.Disable(); Near(faceRoot.localScale.x,-2f,"disable keeps requested facing without depth");
        depth.Enable(); Near(faceRoot.localScale.x,-1.88f,"reenable does not compound");
        faceRoot.localScale=new Vector3(7f,8f,9f); depth.Frame();
        Check(depth.Status==DepthScaleStatus.ExternalChange,"external scale drift yields ownership");
        avatar.SetFacing(true); depth.Disable();
        Point(faceRoot.localScale,new Vector3(7f,8f,9f),"later external scale not overwritten");
        depth.Enable(); Near(faceRoot.localScale.x,6.58f,"reenable captures new external baseline");
        depth.Disable();
        var earlyRoot=new Transform {position=new Vector3(0f,2f,0f),localScale=new Vector3(2f,3f,1f)};
        var early=NewDepth(earlyRoot,line,config,Vector3.zero);
        early.isActiveAndEnabled=true;
        var earlyAvatar=new TomatoSpineAvatar();
        Set(earlyAvatar,typeof(TomatoSpineAvatar),"facingVisualRoot",earlyRoot);
        earlyAvatar.SetFacing(false); // Actual caller precedes DepthScale.OnEnable in this offline ordering.
        early.Enable(); Near(earlyRoot.localScale.x,-1.88f,"avatar before depth enable keeps baseline");
        early.Disable();
        var missing=NewDepth(new Transform(),null,null,Vector3.zero); missing.Enable();
        Check(missing.Status==DepthScaleStatus.MissingGroundLine,"missing sources clear status");
        missing.ConfigureSources(line,null);
        Check(missing.Status==DepthScaleStatus.MissingConfig,"missing config clear status");
        missing.Disable(); StreetConfigs.Unbind();
        DepthScaleFacingLifecycleTests.Run();
        Console.WriteLine("PASS "+count+" actual-source depth/facing assertions plus 61 walk assertions");
        return 0;
    }
}
