using System;
using System.Collections.Generic;
using System.Reflection;
using EatWhat.Narrative.Core;
using EatWhat.Narrative.Unity;
using EatWhat.Toolkit.Input;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UO=UnityEngine.Object;

internal static class NarrativeDialoguePrefabTests
{
    static readonly List<string> details=new List<string>();
    static void Check(bool v,string why){if(!v)throw new Exception(why);details.Add(why);}
    static void Set(object o,string n,object v){o.GetType().GetField(n,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o,v);}
    static object Get(object o,string n){return o.GetType().GetField(n,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(o);}
    static void Life(object o,string n){o.GetType().GetMethod(n,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(o,null);}
    static void Reject(Action work,string why){try{work();}catch(InvalidOperationException){Check(true,why);return;}throw new Exception(why);}
    static RectTransform Rect(string name,Transform parent){var g=new GameObject(name,typeof(RectTransform));g.transform.SetParent(parent,false);return (RectTransform)g.transform;}
    static Image Image(string name,Transform parent){return Rect(name,parent).gameObject.AddComponent<Image>();}
    static int Listeners(object o,string name){var d=(Delegate)Get(o,name);return d==null?0:d.GetInvocationList().Length;}
    static void Click(Image image,PointerEventData.InputButton button=PointerEventData.InputButton.Left)
    {foreach(var entry in image.GetComponent<EventTrigger>().triggers.ToArray())entry.callback.Invoke(new PointerEventData{button=button});}
    public sealed class Input:MonoBehaviour,IGameInput
    {public float Horizontal=>0;public float Vertical=>0;public Vector2 LookAxis=>Vector2.zero;public GameInputDevice CurrentDevice=>GameInputDevice.Keyboard;public bool Pressed;public bool Interact=>Pressed;public Vector2 MouseScreenPos=>Vector2.zero;}
    public sealed class Lock:MonoBehaviour,INarrativeDialogueLock
    {public int Held;public void Acquire(string id,double time){Held++;}public void Release(string id,double time){Held--;}}
    sealed class Ui
    {
        public readonly GameObject Root;public readonly NarrativeDialogueCanvas Canvas;public readonly NarrativeDialogueBoxView Box;public readonly NarrativeSceneDimmer Dimmer;
        public readonly RectTransform Visual,Plate,Name,Dim,Portrait,Dialogue;public readonly Image Advance,Auto,Fast;public readonly TextMeshProUGUI Body,NameText;
        public readonly NarrativeDialogueComposition Composition;public readonly Input Input;
        public Ui(bool authored)
        {
            Root=new GameObject(authored?"Authored HUD":"Fallback HUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            Canvas=Root.AddComponent<NarrativeDialogueCanvas>();Canvas.ConfigureFont(new TMP_FontAsset{material=new Material()});
            Box=Root.AddComponent<NarrativeDialogueBoxView>();Set(Box,"dialogueCanvas",Canvas);Input=Root.AddComponent<Input>();Set(Box,"inputProvider",Input);
            Dimmer=Root.AddComponent<NarrativeSceneDimmer>();Set(Dimmer,"dialogueCanvas",Canvas);
            Composition=Root.AddComponent<NarrativeDialogueComposition>();Set(Composition,"boxView",Box);Set(Composition,"sceneDimmer",Dimmer);
            if(!authored)return;
            Root.GetComponent<Canvas>().sortingOrder=345;
            Dim=Rect("Dim",Root.transform);var dimImage=Dim.gameObject.AddComponent<Image>();Portrait=Rect("Portraits",Root.transform);Dialogue=Rect("Dialogue",Root.transform);
            Set(Canvas,"authoredCanvas",Root.GetComponent<Canvas>());Set(Canvas,"authoredDimLayer",Dim);Set(Canvas,"authoredDimImage",dimImage);Set(Canvas,"authoredPortraitLayer",Portrait);Set(Canvas,"authoredDialogueLayer",Dialogue);
            Visual=Rect("Dialogue visual root",Dialogue);var group=Visual.gameObject.AddComponent<CanvasGroup>();
            Advance=Image("Advance",Visual);Plate=Rect("Box",Visual);Plate.gameObject.AddComponent<Image>();Plate.anchoredPosition=new Vector2(17,77);Plate.localScale=new Vector3(2,2,1);
            Body=Rect("Body",Plate).gameObject.AddComponent<TextMeshProUGUI>();Body.fontSize=61;
            Name=Rect("Name",Plate);Name.sizeDelta=new Vector2(288,91);Name.gameObject.AddComponent<Image>();NameText=Rect("Name text",Name).gameObject.AddComponent<TextMeshProUGUI>();
            Auto=Image("AUTO",Plate);Fast=Image("Fast",Plate);foreach(var image in new[]{Advance,Auto,Fast})image.gameObject.AddComponent<EventTrigger>();
            Set(Box,"authoredRoot",Visual);Set(Box,"authoredBox",Plate);Set(Box,"authoredNameBox",Name);Set(Box,"authoredBodyText",Body);Set(Box,"authoredNameText",NameText);
            Set(Box,"authoredAdvanceSurface",Advance);Set(Box,"authoredAutoButton",Auto);Set(Box,"authoredFastForwardButton",Fast);Set(Box,"authoredGroup",group);
        }
    }
    static DialoguePresentationData Config()
    {var c=new DialoguePresentationData();c.entrance[0].dimTime=c.entrance[0].boxTime=c.exit[0].boxTime=c.exit[0].undimTime=0;c.text[0].charsPerSecond=0;c.text[0].advanceCooldown=0;return c;}
    static NarrativeDataDirectory Directory()
    {
        var empty=new NarrativeCommandSource(NarrativeCommandParser.Parse(""));var profiles=new NarrativeProfileNames("","","","","","","","","");
        return new NarrativeDataDirectory(characters:new[]{new NarrativeCharacter("chr_a","name","",null,null,null,"","",null,null)},
            dialogues:new[]{new NarrativeDialogue("a",NarrativeDialogueLevel.Normal,new[]{new NarrativeCastEntry(1,"chr_a","",null,null,null,null)},profiles,"","line","",empty)},
            lines:new[]{new NarrativeDialogueLine("line","a","","chr_a","","body",empty,"","","","")});
    }
    static NarrativeDialogueService Service(Ui ui,bool composition)
    {
        var root=new GameObject("Service");var runtime=root.AddComponent<NarrativeRuntimeEntry>();var config=Config();runtime.Directory=Directory();runtime.Resolve=(d,p)=>p.Resolve(config,d);
        var service=root.AddComponent<NarrativeDialogueService>();Set(service,"runtimeEntry",runtime);Set(service,"operationLockProvider",root.AddComponent<Lock>());
        if(composition)Set(service,"presentationComposition",ui.Composition);else{Set(service,"boxView",ui.Box);Set(service,"sceneDimmer",ui.Dimmer);}return service;
    }
    static void Step(NarrativeDialogueService service){Time.frameCount++;Life(service,"Update");}
    static int Main(string[] args)
    {
        try
        {
            Application.persistentDataPath=args[0];new GameObject("Existing event system").AddComponent<EventSystem>();
            var ui=new Ui(true);int created=UO.Created;ui.Canvas.EnsureCreated();
            Check(ui.Canvas.RootCanvas==ui.Root.GetComponent<Canvas>()&&ui.Canvas.DimLayer==ui.Dim&&ui.Canvas.PortraitLayer==ui.Portrait&&ui.Canvas.DialogueLayer==ui.Dialogue,"authored Canvas uses exactly saved layers and empty portrait slot");
            Check(UO.Created==created&&ui.Root.GetComponent<Canvas>().sortingOrder==345,"authored Canvas creates no replacement tree or overwrites authored sorting");
            Check(!ui.Dim.GetComponent<Image>().raycastTarget,"authored dim image does not intercept clicks");
            Life(ui.Dimmer,"OnEnable");ui.Dimmer.Request(new object(),.4f,0);Check(ui.Dim.GetComponent<Image>().color.a==.4f,"real dimmer writes authored dim image");Life(ui.Dimmer,"OnDisable");
            var input=new NarrativeAdvanceInputModel();ui.Box.Configure(input,ui.Input);int advance=0;ui.Box.AdvanceRequested+=()=>advance++;
            var outside=new EventTrigger.Entry();int outsideCalls=0;outside.callback.AddListener(x=>outsideCalls++);ui.Auto.GetComponent<EventTrigger>().triggers.Add(outside);
            NarrativeDialogue dialogue;string lookupIssue;Directory().TryGetDialogue("a",out dialogue,out lookupIssue);
            var snapshot=DialoguePresentationResolver.Resolve(Config(),dialogue).Snapshot;
            created=UO.Created;ui.Box.Show(snapshot);ui.Box.SetBoxProgress(snapshot,1);Check(UO.Created==created&&Get(ui.Box,"runtimeRoot")==ui.Visual,"authored Box shows saved root without creating replacement visual nodes");
            Check(ui.Body.fontSize==61&&ui.Plate.anchoredPosition.x==17&&ui.Plate.anchoredPosition.y==77,"authored typography and box position preserved by zero entrance offset");
            Check(ui.Body.font==ui.Canvas.FontAsset&&TextMeshProUGUI.Bindings[0]=="material"&&TextMeshProUGUI.Bindings[1]=="font","authored font binding shares material before font");
            Check(!ui.Body.raycastTarget&&!ui.Plate.GetComponent<Image>().raycastTarget&&ui.Advance.raycastTarget,"author plate and text pass clicks to actual advance surface");
            ui.Box.Show(snapshot);Check(ui.Advance.GetComponent<EventTrigger>().triggers.Count==1&&ui.Auto.GetComponent<EventTrigger>().triggers.Count==2,"repeated show does not duplicate own callbacks or remove external entry");
            Click(ui.Auto);Check(input.AutoEnabled&&outsideCalls==1&&advance==0,"authored AUTO changes shared input once without screen advance");
            Click(ui.Fast);Check(input.FastForwardEnabled&&advance==0,"authored fast button changes shared model without advance");
            Click(ui.Advance,PointerEventData.InputButton.Right);Check(advance==0,"non-left pointer ignored");Click(ui.Advance);Check(advance==1,"authored hit surface produces one advance intent");
            ui.Box.Hide();Click(ui.Advance);Check(advance==1&&!ui.Visual.gameObject.activeSelf,"hidden presentation cannot emit advance");
            Life(ui.Box,"OnDisable");Check(!ui.Visual.gameObject.Destroyed&&ui.Auto.GetComponent<EventTrigger>().triggers.Count==1,"disable preserves author nodes and detaches only owned listener");
            Check(ui.Plate.anchoredPosition.x==17&&ui.Plate.localScale.x==2&&ui.Name.sizeDelta.x==288,"disable restores author pose and name dimensions");
            ui.Box.Show(snapshot);Click(ui.Advance);Check(advance==2&&ui.Advance.GetComponent<EventTrigger>().triggers.Count==1,"reenable/show reuses author tree and installs one callback");
            Life(ui.Box,"OnDestroy");Check(!ui.Visual.gameObject.Destroyed&&!ui.Root.Destroyed&&ui.Advance.GetComponent<EventTrigger>().triggers.Count==0,"destroying adapter keeps author tree and removes its callbacks");
            ui.Dimmer.Request(new object(),.6f,0);
            Life(ui.Canvas,"OnDisable");Check(!ui.Root.Destroyed&&!ui.Root.GetComponent<Canvas>().enabled,"Canvas disable hides native authored canvas without deleting author nodes");
            Life(ui.Canvas,"OnEnable");Check(ui.Root.GetComponent<Canvas>().enabled&&ui.Canvas.DialogueLayer==ui.Dialogue,"Canvas reenable reuses original hierarchy");
            Check(ui.Dimmer.CurrentAmount==.6f&&ui.Dim.GetComponent<Image>().color.a==.6f,"Canvas reenable preserves the dim amount still held by the existing owner");
            var partial=new Ui(false);Set(partial.Canvas,"authoredCanvas",partial.Root.GetComponent<Canvas>());Reject(()=>partial.Canvas.EnsureCreated(),"partial Canvas binding reports mismatch rather than silently replacing it");
            var badBox=new Ui(true);Set(badBox.Box,"authoredNameText",null);badBox.Box.Configure(new NarrativeAdvanceInputModel(),null);Reject(()=>badBox.Box.Show(snapshot),"partial Box binding reports missing author node");
            var worldCanvas=new Ui(true);worldCanvas.Root.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;Reject(()=>worldCanvas.Canvas.EnsureCreated(),"author Canvas must use the original screen overlay contract");
            var alias=new Ui(true);Set(alias.Box,"authoredNameBox",alias.Plate);alias.Box.Configure(new NarrativeAdvanceInputModel(),null);Reject(()=>alias.Box.Show(snapshot),"name and dialogue box cannot alias one rect and hide the whole body");
            // Real MonoBehaviour service and session consume the component's actual entrance/exit graph.
            var wired=new Ui(true);var service=Service(wired,true);Time.frameCount=20;string diagnostic;
            Check(service.TryStart("a","chr_npc#2",out diagnostic)==NarrativeServiceStartStatus.Started,"service starts using prefab composition references without duplicate direct surface wiring");
            var session=(NarrativeDialogueSession)Get(service,"session");
            Check(ReferenceEquals(Get(session,"entrance"),wired.Composition.Entrance)&&ReferenceEquals(Get(session,"exit"),wired.Composition.Exit),"unique Session uses same original07/08 pair exposed by mountable composition");
            Step(service);Step(service);Check(wired.Body.text!=null&&wired.Composition.Entrance.CurrentContext!=null,"actual session/entrance reaches authored body surface");
            var graph=wired.Composition.Entrance;Life(service,"Update");Check(ReferenceEquals(graph,wired.Composition.Entrance)&&Listeners(wired.Box,"AdvanceRequested")==1,"repeated service Update reuses graph and one advance subscription");
            Reject(()=>{NarrativeDialogueEntrance a;NarrativeDialogueExit b;wired.Composition.Acquire(new object(),wired.Box,wired.Dimmer,out a,out b);},"one composition rejects a second owner");
            Check(session.Phase==NarrativeDialoguePhase.Typing,"zero-speed original typewriter waits for manual completion");
            wired.Input.Pressed=true;Life(wired.Box,"Update");wired.Input.Pressed=false;Step(service);Step(service);
            Check(service.IsRunning&&session.Phase==NarrativeDialoguePhase.LineComplete&&!service.Talked.HasTalked("a"),"first authored input completes line without skipping original dialogue step");
            wired.Input.Pressed=true;Life(wired.Box,"Update");wired.Input.Pressed=false;Step(service);Step(service);
            Check(!service.IsRunning&&service.Talked.HasTalked("a"),"authored input completes real FSM and actual persistent talked owner");
            Check(!wired.Visual.gameObject.activeSelf,"original exit hides saved dialogue root");
            Step(service);Check(service.TryStart("a","chr_npc#2",out diagnostic)==NarrativeServiceStartStatus.Started&&ReferenceEquals(graph,wired.Composition.Entrance),"next dialogue reuses same composition owner and graph");
            Life(wired.Composition,"OnDisable");Check(!service.IsRunning&&!wired.Visual.gameObject.activeSelf,"composition disable aborts through existing session owner");
            wired.Composition.enabled=false;Check(service.TryStart("a","",out diagnostic)==NarrativeServiceStartStatus.Unavailable,"disabled composition refuses a new dialogue");wired.Composition.enabled=true;
            Life(service,"OnDestroy");Check(!wired.Composition.HasOwner&&wired.Composition.Entrance==null&&Listeners(wired.Box,"AdvanceRequested")==0,"session disposal releases composition claim and box subscription");
            var next=Service(wired,true);Check(next.TryStart("a","",out diagnostic)==NarrativeServiceStartStatus.Started,"released prefab composition can bind next service lifetime");Life(next,"OnDestroy");
            var mismatch=Service(wired,true);Set(mismatch,"boxView",new Ui(false).Box);Check(mismatch.TryStart("a","",out diagnostic)==NarrativeServiceStartStatus.Unavailable,"service detects conflicting direct and prefab surface bindings");
            EventSystem.current=null;var fallback=new Ui(false);var old=Service(fallback,false);
            Check(old.TryStart("a","",out diagnostic)==NarrativeServiceStartStatus.Started,"original unbound MonoBehaviour service path remains usable");
            var fallbackRoot=(RectTransform)Get(fallback.Box,"runtimeRoot");var fallbackCanvas=fallback.Canvas.RootCanvas;
            Check(fallbackRoot!=null&&fallbackRoot.gameObject.name=="DialogueBox_Runtime"&&fallbackCanvas.gameObject.name=="NarrativeDialogueCanvas_Runtime","unbound path still creates original runtime visual tree");
            Life(old,"OnDestroy");Life(fallback.Box,"OnDisable");Life(fallback.Canvas,"OnDisable");
            Check(fallbackRoot.gameObject.Destroyed&&fallbackCanvas.gameObject.Destroyed,"runtime fallback destroys only its own generated visuals");
            NarrativeNameBoxSideTests.Run();
            NarrativeDialogueNotificationTests.Run(System.IO.Path.Combine(args[0], "notifications"));
            Console.WriteLine(JsonConvert.SerializeObject(new{result="DIALOGUE_PREFAB_PASS",checks=details.Count,details,nativeUnity=false,realTmpLayoutVerified=false},Formatting.Indented));return 0;
        }
        catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
    }
}
