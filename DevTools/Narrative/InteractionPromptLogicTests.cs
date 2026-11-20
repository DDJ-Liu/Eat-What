using System;
using System.Collections.Generic;
using EatWhat.Tools;
using EatWhat.Toolkit.Input;
using UnityEngine;

internal static class InteractionPromptLogicTests
{
    static readonly List<string> details = new List<string>();
    static void Check(bool value,string name){if(!value)throw new Exception("Prompt: "+name);details.Add(name);}
    static void Near(float actual,float expected,string name){Check(Math.Abs(actual-expected)<.00001f,name);}
    static void Invalid(Action work,string name){bool bad=false;try{work();}catch(ArgumentException){bad=true;}Check(bad,name);}
    sealed class DeviceInput : IGameInput
    {
        public GameInputDevice Device;
        public float Horizontal {get{throw new Exception("Prompt must not read movement.");}}
        public float Vertical {get{throw new Exception("Prompt must not read movement.");}}
        public Vector2 LookAxis {get{throw new Exception("Prompt must not read look.");}}
        public GameInputDevice CurrentDevice {get{return Device;}}
        public bool Interact {get{throw new Exception("Prompt must not consume interaction.");}}
        public Vector2 MouseScreenPos {get{throw new Exception("Prompt must not read pointer.");}}
    }
    static int Main()
    {
        var table=new LocTableAsset();table.entries.Add(new LocTableEntry{key="prompt.talk",zh_cn="交谈"});table.entries.Add(new LocTableEntry{key="prompt.exit",zh_cn="前往街道"});LocService.Configure(table);
        var a=new object();var b=new object();var input=new DeviceInput();var state=new InteractionPromptState(.2,.4,"E","A");
        Check(state.Target==null&&!state.WantsVisible&&state.Alpha==0,"initially hidden without a target");
        state.Hide();state.Advance(1,input.CurrentDevice);Check(state.Target==null,"repeated hide before show harmless");
        state.Show(a,"prompt.talk");Check(state.Target==a&&state.Text=="交谈"&&state.LocalizationKey=="prompt.talk"&&state.WantsVisible,"show actual localization text at caller target");
        Near(state.Alpha,0,"show starts transparent");state.Advance(.1,input.CurrentDevice);Near(state.Alpha,.5f,"fade in midpoint");
        state.Show(a,"prompt.talk");state.Advance(.1,input.CurrentDevice);Near(state.Alpha,1,"same show does not restart fade");
        Check(state.KeyCaption=="E","keyboard caption is configured interaction key");
        input.Device=GameInputDevice.Gamepad;string text=state.Text;object target=state.Target;float alpha=state.Alpha;
        state.Advance(0,input.CurrentDevice);Check(state.KeyCaption=="A"&&state.Device==GameInputDevice.Gamepad,"current device abstraction selects confirm placeholder");
        Check(state.Text==text&&state.Target==target&&state.Alpha==alpha,"device change preserves action text target and opacity");
        input.Device=GameInputDevice.Keyboard;state.Advance(0,input.CurrentDevice);Check(state.KeyCaption=="E"&&state.Text==text,"keyboard return preserves action text");
        state.Show(b,"prompt.exit");Check(state.Target==b&&state.Text=="前往街道"&&state.LocalizationKey=="prompt.exit","new target replaces old rather than adds a second prompt");
        Near(state.Alpha,1,"switching visible target has no alpha jump");
        state.Hide();state.Advance(.2,input.CurrentDevice);Near(state.Alpha,.5f,"hide fade midpoint");
        Check(state.Target==b&&state.Text=="前往街道"&&!state.WantsVisible,"fade out retains old content until complete");
        state.Hide();state.Advance(.2,input.CurrentDevice);Check(state.Target==null&&state.Text==""&&state.Alpha==0,"repeated hide does not restart and completion releases target");
        state.Show(a,"prompt.talk");state.Advance(.1,input.CurrentDevice);state.Hide();Near(state.Alpha,.5f,"mid-fade hide continuous");
        state.Advance(.2,input.CurrentDevice);Near(state.Alpha,.25f,"hide continues from actual amount");
        state.Show(b,"prompt.exit");Near(state.Alpha,.25f,"mid-hide target change continuous");state.Advance(.1,input.CurrentDevice);Near(state.Alpha,.625f,"new show starts from current opacity");
        state.Advance(100,input.CurrentDevice);Near(state.Alpha,1,"large advance clamps at visible");
        state.Show(a,"missing.prompt");Check(state.Text=="#missing.prompt#","missing key uses actual LocService token");
        int warnings=Debug.Warnings.Count;state.Advance(.1,input.CurrentDevice);state.Show(a,"missing.prompt");Check(Debug.Warnings.Count==warnings&&warnings==1,"actual missing-key warning deduplicated by existing catalog");
        Check(LocService.Get("missing.prompt")==state.Text,"fallback equals existing service rather than a new replacement policy");
        input.Device=GameInputDevice.Gamepad;state.Advance(0,input.CurrentDevice);Check(state.Text=="#missing.prompt#"&&state.KeyCaption=="A","device switch preserves missing token too");
        table.entries.Add(new LocTableEntry{key="missing.prompt",zh_cn="补入文字"});LocService.Configure(table);state.Advance(0,input.CurrentDevice);Check(state.Text=="补入文字","existing localization refresh reaches current prompt");
        state.Reset();Check(state.Target==null&&state.Text==""&&!state.WantsVisible&&state.Alpha==0,"disable reset clears only prompt state");
        state.Show(a,"prompt.talk");state.Hide();Check(state.Target==null,"hide before first visible tick releases zero-alpha target");
        var instant=new InteractionPromptState(0,0,"F","Confirm");instant.Show(a,"prompt.exit");Near(instant.Alpha,1,"zero fade-in immediate");
        instant.Advance(0,GameInputDevice.Keyboard);Check(instant.KeyCaption=="F"&&instant.Text=="前往街道","caller-configured keyboard key and action reusable");
        instant.Advance(0,GameInputDevice.Gamepad);Check(instant.KeyCaption=="Confirm"&&instant.Text=="前往街道","gamepad caption placeholder configurable independently");
        instant.Hide();Check(instant.Alpha==0&&instant.Target==null,"zero fade-out immediate release");
        state.Show(a,"prompt.talk");state.Advance(.1,GameInputDevice.Keyboard);
        Invalid(()=>state.Advance(-1,GameInputDevice.Keyboard),"negative time rejected");
        Invalid(()=>state.Advance(double.NaN,GameInputDevice.Keyboard),"NaN time rejected");
        Invalid(()=>state.Advance(double.PositiveInfinity,GameInputDevice.Keyboard),"infinite time rejected");
        Invalid(()=>state.Advance(0,(GameInputDevice)99),"unknown device rejected");
        Invalid(()=>state.Show(null,"prompt.talk"),"null target rejected");
        Invalid(()=>new InteractionPromptState(-1,0,"E","A"),"negative fade setting rejected");
        Near(state.Alpha,.5f,"invalid input preserves prior fade");Check(state.Target==a&&state.Text=="交谈","invalid input preserves prior target and localization");
        Console.WriteLine("INTERACTION_PROMPT_RESULT={\"passed\":true,\"checks\":"+details.Count+",\"actualStateDeviceAndLocService\":true,\"nativeUnity\":false,\"details\":[\""+string.Join("\",\"",details.ToArray())+"\"]}");return 0;
    }
}
