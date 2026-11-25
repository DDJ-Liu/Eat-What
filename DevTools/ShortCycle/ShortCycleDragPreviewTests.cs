using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EatWhat.Cooking.ShortCycle;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using TMPro;

public static class ShortCycleDragPreviewTests
{
    private static readonly List<string> authoredCases = new List<string>();
    public static IReadOnlyList<string> AuthoredCases {get{return authoredCases.AsReadOnly();}}
    public static bool AuthoredOccupiedPickPassed {get;private set;}
    public static bool AuthoredReleaseBackgroundPassed {get;private set;}
    public static bool AuthoredVisualsPreservedPassed {get;private set;}
    public static bool ImportedOverlapSelectsFood {get;private set;}
    public static bool HitOrderIndependent {get;private set;}
    public static float EggOverlapDepth {get;private set;}
    public static float SoupOverlapDepth {get;private set;}
    private static Collider2D Rank(params Collider2D[] colliders)
    {return (Collider2D)typeof(MouseManager).GetMethod("SelectHighestPriority",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{colliders});}
    private static void AuthoredCheck(bool ok,string message){if(!ok)throw new Exception("AUTHORED-TRAY: "+message);authoredCases.Add(message);}
    private static void PickTick(TraySlot slot)
    {var m=typeof(TraySlot).GetMethod("LateUpdate",BindingFlags.Instance|BindingFlags.NonPublic);if(m!=null)m.Invoke(slot,null);}
    private static void PressAt(ShortCycleTrayPointerTests.Fixture f,Vector2 p)
    {Tools.MouseWorldPosition=p;f.Mouse.OnMouseConfirmPress();f.Mouse.OnDragStart(default(UnityEngine.InputSystem.InputAction.CallbackContext));}
    private static void MoveAt(ShortCycleTrayPointerTests.Fixture f,Vector2 p)
    {Tools.MouseWorldPosition=p;Call(f.Mouse,"Update");if(f.Mouse.currentDraggableObject!=null && f.Mouse.currentDraggableObject.dragging)Call(f.Mouse.currentDraggableObject,"Update");}
    private static void ReleaseAt(ShortCycleTrayPointerTests.Fixture f,Vector2 p)
    {Tools.MouseWorldPosition=p;f.Mouse.OnMouseLeftButtonRelease();}
    // Fixed Unity imported Sprite.m_PhysicsShape paths; see the permanent fixture for source provenance.
    private static Newtonsoft.Json.Linq.JObject importedShapes;
    private static Sprite ImportedSprite(string key,string displayName,int width,int height)
    {
        var sprite=new Sprite{name=displayName,rect=new Rect(0,0,width,height),pixelsPerUnit=100};
        foreach(var path in importedShapes["sources"][key]["paths"])
            sprite.PhysicsShapes.Add(path.Select(p=>new Vector2((float)p[0],(float)p[1])).ToArray());
        return sprite;
    }
    private static Sprite AuthoredEgg(){return ImportedSprite("ui_shicai_jidan","ui_shicai_jidan",512,512);}
    private static Sprite AuthoredFrame(){return ImportedSprite("chuju_bingxiangmao_tuopan2","tuopan2",421,317);}
    private const float OverlapNeighbourX=.12f, OverlapNeighbourY=-.73f, OverlapPointX=.1727f, OverlapPointY=.0977f;
    public static string RunAuthoredPickup(string repositoryRoot)
    {
        authoredCases.Clear();
        ImportedOverlapSelectsFood=HitOrderIndependent=false;
        importedShapes=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(System.IO.Path.Combine(repositoryRoot,"DevTools/ShortCycle/Fixtures/TrayPickupImportedShapes.json")));
        AuthoredOccupiedPickPassed=AuthoredReleaseBackgroundPassed=AuthoredVisualsPreservedPassed=false;
        using(var f=ShortCycleTrayPointerTests.Create())
        {
            var parent=new GameObject("TraySlotsRoot saved transform");parent.transform.position=new Vector3(-10.5f,-3.5f,-.3f);
            f.Tray.transform.SetParent(parent.transform,false);f.Tray.transform.localPosition=Vector3.zero;f.Tray.transform.localScale=new Vector3(.35f,.35f,1);
            var frame=f.Tray.GetComponent<SpriteRenderer>();
            Style(f.Tray,"tray");frame.sprite=AuthoredFrame();
            f.Tray.DragIcon.transform.localPosition=new Vector3(0,0,-.1f);f.Tray.DragIcon.transform.localScale=new Vector3(1.7714286f,1.7714286f,1);
            var legacy=f.Tray.GetComponent<Collider2D>();Physics2D.Colliders.Remove(legacy);legacy.enabled=false;
            var container=Wire(f.Tray,f.TrayDrag);container.col.size=new Vector2(1,.65f);container.col.offset=Vector2.zero;Physics2D.Colliders.Add(container.col);
            Set(f.Tray,"mouseInteractBehaviours",new Behaviour[]{f.TrayButton,f.TrayDrag,container,f.TrayBinding});
            var egg=AuthoredEgg();CK01ItemData item;AuthoredCheck(f.Session.Inventory.TryGetGeneratedItem("flour",out item),"actual item catalog supplies the fixture identity");item.icon_sprite=egg;
            f.Region.ContainsPoint=p=>p.x>=-14.124138f && p.x<=14.124138f && p.y>=-7.2f && p.y<=7.2f;
            f.Fridge.GetComponent<Collider2D>().ContainsPoint=p=>p.x>-14 && p.x<-7 && p.y>-7 && p.y<0;
            f.Reserve();PickTick(f.Tray);
            AuthoredCheck(f.Tray.GetComponent<PolygonCollider2D>().isTrigger && f.Tray.GetComponent<PolygonCollider2D>().pathCount==egg.GetPhysicsShapeCount(),"pickup is a trigger made only from food contours even when real frame shape exists");
            var source=new Vector2(-10.5f,-3.5f);var opaque=source+new Vector2(-.0372f,-.47f);var outside=new Vector2(20,-3.5f);
            AuthoredCheck(container.col.size.x==1 && container.col.size.y==.65f,"authored box dimensions stay unchanged for DragContainer grip and layout");
            AuthoredCheck(f.TrayButton.isActiveAndEnabled && f.TrayDrag.isActiveAndEnabled && f.TrayBinding.isActiveAndEnabled && f.Tray.InteractionAllowed,"occupied P1 refresh leaves the saved four input behaviours enabled");
            var beforeClicks=f.SurfaceClicks;PressAt(f,opaque);
            AuthoredCheck(f.Mouse.currentDraggableObject==f.TrayDrag,"saved opaque egg point captures occupied slot (surface clicks="+f.SurfaceClicks+", before="+beforeClicks+", captured="+(f.Mouse.currentDraggableObject==null?"null":f.Mouse.currentDraggableObject.gameObject.name)+")");
            MoveAt(f,outside);AuthoredCheck(f.TrayDrag.dragging && Preview(f.Tray).Visual!=null,"captured egg body crosses actual threshold into existing tray preview");
            AuthoredCheck(Preview(f.Tray).Visual.GetComponent<SortingGroup>().sortingOrder>30,"accepted foreground preview sorting is preserved");
            ReleaseAt(f,outside);ReleaseAt(f,outside);AuthoredCheck(f.Quantity==0,"first authored-point return commits once and duplicate release does nothing");
            AuthoredOccupiedPickPassed=true;
            for(var i=0;i<3;i++)
            {
                f.Reserve();PickTick(f.Tray);PressAt(f,opaque);MoveAt(f,outside);ReleaseAt(f,outside);
                AuthoredCheck(f.Quantity==0 && !f.Tray.gameObject.activeSelf,"same authored point remains reusable after refill "+i);
            }
            f.Reserve();PickTick(f.Tray);
            var transparent=source+new Vector2(-.93f,.93f);beforeClicks=f.SurfaceClicks;PressAt(f,transparent);
            AuthoredCheck(f.Mouse.currentDraggableObject!=f.TrayDrag && f.SurfaceClicks==beforeClicks+1,"transparent egg-canvas corner keeps tray background input instead of full-512 rectangle capture");ReleaseAt(f,transparent);
            var neighbour=Slots(f)[1];neighbour.transform.SetParent(parent.transform,false);
            neighbour.transform.localPosition=new Vector3(OverlapNeighbourX,OverlapNeighbourY,0);neighbour.transform.localScale=new Vector3(.35f,.35f,1);
            neighbour.gameObject.AddComponent<SpriteRenderer>();neighbour.gameObject.AddComponent<SortingGroup>().sortingOrder=30;Style(neighbour,"tray");
            neighbour.GetComponent<SpriteRenderer>().sprite=AuthoredFrame();neighbour.DragIcon.transform.localPosition=new Vector3(0,0,-.1f);neighbour.DragIcon.transform.localScale=new Vector3(1.7714286f,1.7714286f,1);
            var neighbourBinding=neighbour.gameObject.AddComponent<ShortCycleMouseActionBinding>();neighbourBinding.Configure(f.Router,ShortCycleActionNames.InventoryPutBack);
            var neighbourButton=neighbour.gameObject.AddComponent<Button_MouseInteract>();neighbourButton.selectEvent=new UnityEvent();neighbourButton.selectEvent.AddListener(neighbourBinding.DispatchFromMouseEvent);Call(neighbourButton,"Start");
            var neighbourDrag=neighbour.gameObject.AddComponent<MouseDraggableObject>();neighbourDrag.startDraggingEvent=new UnityEvent();neighbourDrag.draggingUpdateEvent=new UnityEvent();neighbourDrag.endDraggingEvent=new UnityEvent();neighbourDrag.cancelDraggingEvent=new UnityEvent();
            neighbourDrag.cancelDraggingEvent.AddListener(neighbourBinding.CancelDragVisual);neighbourDrag.onDropHitEvent=new UnityEvent<DropZone>();neighbourDrag.onDropMissedEvent=new UnityEvent();
            var neighbourContainer=Wire(neighbour,neighbourDrag);neighbourContainer.col.size=new Vector2(1,.65f);Physics2D.Colliders.Add(neighbourContainer.col);
            Set(neighbour,"mouseInteractBehaviours",new Behaviour[]{neighbourButton,neighbourDrag,neighbourContainer,neighbourBinding});
            var neighbourSprite=ImportedSprite("ui_shicai_gaotang","ui_shicai_gaotang",512,512);
            neighbour.gameObject.SetActive(true);Call(neighbourBinding,"OnEnable");Call(neighbour,"OnEnable");neighbour.ConfigureItem(new CK01ItemData{name="neighbour",icon_sprite=neighbourSprite},1,"neighbour","neighbour/0");PickTick(neighbour);
            var overlap=source+new Vector2(OverlapPointX,OverlapPointY);PressAt(f,overlap);
            AuthoredCheck(f.Mouse.currentDraggableObject==neighbourDrag,"saved adjacent slot captures opaque food where first frame is opaque but first food is transparent");
            AuthoredCheck(f.Mouse.currentPressableHoverTarget==neighbourButton,"pressable hover and drag capture agree at the same imported-contour overlap point");
            float eggDepth,soupDepth;
            AuthoredCheck(f.Tray.TryGetMouseHitDepth(overlap,out eggDepth) && neighbour.TryGetMouseHitDepth(overlap,out soupDepth),"both actual simplified Sprite contours contain the unchanged sample point");
            f.Tray.TryGetMouseHitDepth(overlap,out eggDepth);neighbour.TryGetMouseHitDepth(overlap,out soupDepth);
            EggOverlapDepth=eggDepth;SoupOverlapDepth=soupDepth;
            AuthoredCheck(soupDepth>eggDepth,"visible soup lies deeper in its imported contour than egg at the unchanged point");
            ImportedOverlapSelectsFood=true;
            MoveAt(f,overlap+new Vector2(0,1));f.Mouse.OnMouseCancelPress();ReleaseAt(f,overlap);
            Physics2D.Colliders.Reverse();PressAt(f,overlap);
            AuthoredCheck(f.Mouse.currentDraggableObject==neighbourDrag && f.Mouse.currentPressableHoverTarget==neighbourButton,"reversing physics candidate order preserves both soup drag capture and pressable winner");
            MoveAt(f,overlap+new Vector2(0,1));f.Mouse.OnMouseCancelPress();ReleaseAt(f,overlap);Physics2D.Colliders.Reverse();
            var firstCollider=f.Tray.GetComponent<PolygonCollider2D>();var secondCollider=neighbour.GetComponent<PolygonCollider2D>();
            Tools.MouseWorldPosition=overlap;f.Tray.GetComponent<SortingGroup>().sortingOrder=31;
            AuthoredCheck(Rank(secondCollider,firstCollider)==firstCollider,"existing higher visual sorting rank wins before any local depth policy");f.Tray.GetComponent<SortingGroup>().sortingOrder=30;
            Set(neighbour,"sessionManager",null);
            AuthoredCheck(Rank(firstCollider,secondCollider)==firstCollider,"unscoped tray-like object does not opt in to the product tray policy");Set(neighbour,"sessionManager",f.Session);
            var legacyA=new GameObject("legacy peer A");legacyA.AddComponent<SpriteRenderer>();var legacyACol=legacyA.AddComponent<Collider2D>();
            var legacyB=new GameObject("legacy peer B");legacyB.AddComponent<SpriteRenderer>();var legacyBCol=legacyB.AddComponent<Collider2D>();
            AuthoredCheck(Rank(legacyACol,legacyBCol)==legacyACol && Rank(legacyBCol,legacyACol)==legacyBCol,"unrelated equal-rank UI retains its original comparator behavior with no depth provider");
            HitOrderIndependent=true;
            neighbour.gameObject.SetActive(false);Call(neighbourBinding,"OnDisable");
            frame.enabled=false;f.Tray.DragIcon.enabled=false;PickTick(f.Tray);PressAt(f,opaque);
            AuthoredCheck(f.Mouse.currentDraggableObject!=f.TrayDrag && f.Mouse.currentPressableHoverTarget==f.SurfaceButton,"hidden renderers leave no invisible occupied-slot hit surface");ReleaseAt(f,opaque);
            frame.enabled=true;f.Tray.DragIcon.enabled=true;PickTick(f.Tray);PressAt(f,opaque);
            AuthoredCheck(f.Mouse.currentDraggableObject==f.TrayDrag,"restoring actual renderers restores the existing opaque pickup contour");f.Mouse.OnMouseCancelPress();ReleaseAt(f,opaque);
            f.Tray.SetInteractionAllowed(false);PickTick(f.Tray);beforeClicks=f.SurfaceClicks;PressAt(f,opaque);
            AuthoredCheck(f.Mouse.currentDraggableObject!=f.TrayDrag && f.Mouse.currentPressableHoverTarget==f.SurfaceButton,"unfocused occupied slot leaves the actual input candidate on tray background");ReleaseAt(f,opaque);
            f.Tray.SetInteractionAllowed(true);PickTick(f.Tray);PressAt(f,opaque);MoveAt(f,outside);ReleaseAt(f,outside);PickTick(f.Tray);
            beforeClicks=f.SurfaceClicks;PressAt(f,opaque);AuthoredCheck(f.Mouse.currentPressableHoverTarget==f.SurfaceButton && f.Mouse.currentDraggableObject==null,"cleared slot leaves tray background ahead of the rear fridge");ReleaseAt(f,opaque);
            AuthoredReleaseBackgroundPassed=true;
            AuthoredCheck(Vector3.Distance(f.Tray.transform.position,new Vector3(-10.5f,-3.5f,-.3f))<.0001f,"authored source transform remains stationary after repeated real callbacks");
            AuthoredCheck(frame.sprite.name=="tuopan2" && f.Tray.DragIcon.transform.localScale.x==1.7714286f,"accepted saved frame, icon scale and appearance remain unchanged");
            AuthoredVisualsPreservedPassed=true;
        }
        return "PASS: authored box/scale/hierarchy, actual opaque egg sample, transparent background and repeated production pointer lifecycle.";
    }
    private static readonly List<string> cases = new List<string>();
    public static IReadOnlyList<string> Cases {get{return cases.AsReadOnly();}}
    public static int Checks {get{return cases.Count;}}
    public static bool AppearancePassed {get;private set;}
    public static bool ReusePassed {get;private set;}
    public static bool CleanupPassed {get;private set;}
    public static bool? BaselineSourceStable {get;private set;}
    public static string BaselineObservation {get;private set;}
    private static void Check(bool value,string message){if(!value)throw new Exception("DRAG-PREVIEW: "+message);cases.Add(message);}
    private static void Set(object owner,string name,object value)
    {
        for(var t=owner.GetType();t!=null;t=t.BaseType){var f=t.GetField(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public);if(f!=null){f.SetValue(owner,value);return;}}
        throw new Exception("Missing field "+name);
    }
    private static void Call(object owner,string name){owner.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).Invoke(owner,null);}
    private static T Get<T>(object owner,string name){return (T)owner.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(owner);}
    private static TraySlot[] Slots(ShortCycleTrayPointerTests.Fixture f)
    {return (TraySlot[])typeof(ShortCycleTrayInteraction).GetField("slots",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(f.Session.TrayInteraction);}
    private static GameObject Child(Transform parent,string name)
    {var go=new GameObject(name);go.transform.SetParent(parent,false);return go;}
    private static void Style(ShortCycleGridPieceBase slot,string theme)
    {
        slot.GetComponent<SpriteRenderer>().sprite=new Sprite{name=theme+" frame"};
        slot.GetComponent<SpriteRenderer>().size=new Vector2(3,2);
        var icon=Child(slot.transform,"Icon").AddComponent<SpriteRenderer>();icon.sortingOrder=2;
        icon.transform.localScale=new Vector3(.7f,.6f,1);icon.transform.localPosition=new Vector3(.2f,.1f,0);
        var label=Child(slot.transform,"Name").AddComponent<TextMeshPro>();label.font=new object();label.fontSharedMaterial=new object();label.text=theme;
        label.rectTransform.sizeDelta=new Vector2(3,1);label.renderer.sortingOrder=3;
        var badge=Child(slot.transform,"Badge").AddComponent<TextMeshPro>();badge.text="stale x999";badge.renderer.sortingOrder=4;
        Set(slot,"iconRenderer",icon);Set(slot,"nameLabel",label);Set(slot,"badgeLabel",badge);
    }
    private static DragContainer Wire(ShortCycleGridPieceBase slot,MouseDraggableObject drag)
    {
        var box=slot.gameObject.AddComponent<BoxCollider2D>();box.size=new Vector2(1,2);
        var container=slot.gameObject.AddComponent<DragContainer>();
        container.col=box;container.mode=DragContainer.DragMode.Composite;container.limitMode=DragContainer.LimitMode.Manual;
        container.leftLimit=-100;container.rightLimit=100;container.topLimit=100;container.bottomLimit=-100;
        container.enableInertia=true;container.enableElasticBoundary=true;
        drag.startDraggingEvent.AddListener(container.onDragStart);
        drag.draggingUpdateEvent.AddListener(container.onDragUpdate);
        drag.endDraggingEvent.AddListener(container.onDragEnd);
        return container;
    }
    private static ShortCycleTrayPointerTests.Fixture Create()
    {
        var f=ShortCycleTrayPointerTests.Create();
        var slots=Slots(f);
        var root=new GameObject("actual authored tray group");root.AddComponent<ShortCyclePresentationSet>();
        root.AddComponent<SortingGroup>().sortingOrder=30;
        foreach(var slot in slots){slot.transform.SetParent(root.transform,false);if(slot.GetComponent<SpriteRenderer>()==null)slot.gameObject.AddComponent<SpriteRenderer>();Style(slot,"tray");slot.transform.localScale=new Vector3(2,3,1);}
        f.Tray.transform.position=new Vector3(2.5f,0,0);slots[1].transform.position=new Vector3(7.5f,0,0);
        f.Fridge.transform.position=new Vector3(-2,0,0);f.Fridge.transform.localScale=new Vector3(.5f,.75f,1);Style(f.Fridge,"fridge");
        // Deliberately incompatible fridge badge must never leak to a one-unit tray image.
        f.Fridge.Configure(new ShortCycleGridPieceDisplay{icon_sprite=f.Fridge.Display.icon_sprite,fallback_name="flour",badge_text="1000/1000"});
        Wire(f.Fridge,f.FridgeDrag);Wire(f.Tray,f.TrayDrag);
        f.Fridge.GetComponent<Collider2D>().ContainsPoint=p=>Vector3.Distance(p,f.Fridge.transform.position)<1;
        f.Tray.GetComponent<Collider2D>().ContainsPoint=p=>Vector3.Distance(p,f.Tray.transform.position)<1;
        return f;
    }
    private static void Move(ShortCycleTrayPointerTests.Fixture f,float x)
    {f.Move(x);if(f.Mouse.currentDraggableObject!=null && f.Mouse.currentDraggableObject.dragging)Call(f.Mouse.currentDraggableObject,"Update");}
    private static ShortCycleIngredientDragPreview Preview(ShortCycleGridPieceBase s){return s.GetComponent<ShortCycleIngredientDragPreview>();}
    private static string Badge(GameObject v){return v.GetComponentsInChildren<TextMeshPro>(true).Single(x=>x.gameObject.name=="Badge visual").text;}
    private static void Stable(ShortCycleTrayPointerTests.Fixture f,string label)
    {
        Check(Vector3.Distance(f.Fridge.transform.position,new Vector3(-2,0,0))<.0001f && Vector3.Distance(f.Tray.transform.position,new Vector3(2.5f,0,0))<.0001f,label+" keeps authored source positions");
        Check(f.Fridge.GetComponent<Collider2D>().OverlapPoint(new Vector2(-2,0)) && (!f.Tray.gameObject.activeSelf || f.Tray.GetComponent<Collider2D>().OverlapPoint(new Vector2(2.5f,0))),label+" keeps dynamic collider pickup geometry");
    }
    private static void Baseline()
    {
        var type=typeof(ShortCycleDragPreviewTests).Assembly.GetType("BeforeDragContainer");
        if(type==null){BaselineObservation="Optional immutable pre-change source not supplied.";return;}
        Physics2D.Colliders.Clear();var mouse=new MouseManager{dragMoveThreshold=.5f};MouseManager.Instance=mouse;
        mouse.mouseInteractiveLayers.Push(new MouseInteractionLayer{alloweInteractionLayers=new List<LayerMask>{1}});
        var go=new GameObject("pre-change live source");go.transform.position=new Vector3(20,0,0);
        var col=go.AddComponent<Collider2D>();col.ContainsPoint=p=>Vector3.Distance(p,go.transform.position)<1;Physics2D.Colliders.Add(col);
        var box=go.AddComponent<BoxCollider2D>();box.size=new Vector2(1,1);
        var old=(Component)Activator.CreateInstance(type);go.AttachExistingComponent(old);
        Set(old,"col",box);Set(old,"mode",Enum.Parse(type.GetNestedType("DragMode"),"Composite"));Set(old,"limitMode",Enum.Parse(type.GetNestedType("LimitMode"),"Manual"));
        foreach(var field in new[]{"leftLimit","bottomLimit"})Set(old,field,-100f);foreach(var field in new[]{"rightLimit","topLimit"})Set(old,field,100f);
        var drag=go.AddComponent<MouseDraggableObject>();drag.startDraggingEvent=new UnityEvent();drag.draggingUpdateEvent=new UnityEvent();drag.endDraggingEvent=new UnityEvent();drag.cancelDraggingEvent=new UnityEvent();
        drag.startDraggingEvent.AddListener(()=>Call(old,"onDragStart"));drag.draggingUpdateEvent.AddListener(()=>Call(old,"onDragUpdate"));drag.endDraggingEvent.AddListener(()=>Call(old,"onDragEnd"));
        Tools.MouseWorldPosition=new Vector3(20,0,0);mouse.OnMouseConfirmPress();mouse.OnDragStart(default(UnityEngine.InputSystem.InputAction.CallbackContext));
        Tools.MouseWorldPosition=new Vector3(22,0,0);Call(mouse,"Update");Call(drag,"Update");
        Tools.MouseWorldPosition=new Vector3(24,0,0);Call(mouse,"Update");Call(drag,"Update");
        BaselineSourceStable=Vector3.Distance(go.transform.position,new Vector3(20,0,0))<.0001f;
        BaselineObservation="Pre-change production DragContainer Press/Move: source.x="+go.transform.position.x+"; initial x=20; original-position pickup="+col.OverlapPoint(new Vector2(20,0));
        Check(BaselineSourceStable==false && !col.OverlapPoint(new Vector2(20,0)),"immutable pre-change callback moves the live source and loses original-position pickup (expected red)");
        mouse.OnMouseCancelPress();Physics2D.Colliders.Clear();MouseManager.Instance=null;
    }
    public static string Run()
    {
        cases.Clear();AppearancePassed=ReusePassed=CleanupPassed=false;Baseline();
        using(var f=Create())
        {
            f.Press(-2);Move(f,5);var visual=Preview(f.Fridge).Visual;
            Check(visual!=null && Vector3.Distance(visual.transform.position,new Vector3(5,0,0))<.0001f,"one-frame cross-tray drag follows current pointer from captured press origin");
            Check(visual.GetComponent<SortingGroup>().sortingOrder>30,"preview sorts above authored tray face and tray group");
            Check(visual.GetComponentsInChildren<Collider2D>(true).Length==0 && visual.GetComponentsInChildren<MouseDraggableObject>(true).Length==0 && visual.GetComponentsInChildren<ShortCycleMouseActionBinding>(true).Length==0,"render-only hierarchy cannot take input or intercept drop");
            Check(visual.GetComponentsInChildren<SpriteRenderer>(true).Any(x=>x.sprite==f.Tray.GetComponent<SpriteRenderer>().sprite) && visual.transform.lossyScale.x==2 && visual.transform.lossyScale.y==3,"empty inactive authored template supplies complete tray frame and world size");
            Check(Badge(visual)=="","first fridge preview has one-unit tray badge, without fridge inventory counts");
            Stable(f,"first fridge drag");f.Release(5);Check(f.Quantity==1 && Preview(f.Fridge).Visual==null,"first real drop reserves once and releases visual");
            f.Press(2.5f);Move(f,12);visual=Preview(f.Tray).Visual;
            Check(Badge(visual)=="" && visual.transform.lossyScale.x==2,"single-unit reverse drag uses matching occupied tray appearance");
            f.Release(12);Check(f.Quantity==0 && Preview(f.Tray).Visual==null,"reverse drop clears tray through source callback before end event");
            f.Tray.DragBadge.text="stale x777";
            f.Press(-2);Move(f,5);Check(Badge(Preview(f.Fridge).Visual)=="","cleared same-item inactive slot never reuses stale badge");f.Release(5);
            AppearancePassed=true;
            for(var i=0;i<6;i++)
            {
                f.Press(-2);Move(f,5);f.Release(5);Check(f.Quantity==2,"same-scene repeat "+i+" adds one unit to existing stack");
                f.Press(2.5f);Move(f,12);Check(f.Mouse.currentDraggableObject==f.TrayDrag && f.TrayDrag.dragging,"same-scene repeat "+i+" captures live tray again");
                Check(Badge(Preview(f.Tray).Visual)=="x2","same-scene repeat "+i+" copies actual tray stack badge");
                Stable(f,"stack return "+i);f.Release(12);f.Release(12);Check(f.Quantity==1,"same-scene repeat "+i+" returns exactly one despite duplicate release");
                f.Press(2.5f);Move(f,12);f.Release(12);Check(f.Quantity==0 && !f.Tray.gameObject.activeSelf,"same-scene repeat "+i+" empties and disables same authored slot");
                f.Press(-2);Move(f,5);f.Release(5);Check(f.Quantity==1 && f.Tray.gameObject.activeSelf,"same-scene repeat "+i+" refills same slot immediately");Stable(f,"refill "+i);
            }
            ReusePassed=true;
            f.Press(2.5f);Move(f,12);f.Mouse.OnMouseCancelPress();f.Release(12);
            Check(f.Quantity==1 && Preview(f.Tray).Visual==null,"right cancel clears preview without returning inventory");
            f.Press(2.5f);Move(f,12);typeof(MouseManager).GetMethod("OnApplicationFocus",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(f.Mouse,new object[]{false});f.Release(12);
            Check(f.Quantity==1 && Preview(f.Tray).Visual==null,"window focus loss clears preview and prevents late release submission");
            f.Press(2.5f);Move(f,12);f.Tray.ConfigureItem(new CK01ItemData{name="flour"},1,"flour",f.Tray.InstanceId);f.Release(12);
            Check(f.Quantity==1 && Preview(f.Tray).Visual==null && !f.TrayDrag.dragging,"mid-gesture source rebind cancels capture before release");
            f.Press(2.5f);Move(f,12);f.Tray.gameObject.SetActive(false);f.Release(12);
            Check(f.Quantity==1 && Preview(f.Tray).Visual==null,"source OnDisable clears preview and prevents release business action");
            f.Session.TrayInteraction.Refresh(f.Context,f.Session.Inventory,2);
            f.Press(2.5f);Move(f,12);f.Mouse.mouseInteractiveLayers.Push(new MouseInteractionLayer{alloweInteractionLayers=new List<LayerMask>{1}});f.Release(12);
            Check(f.Quantity==1 && Preview(f.Tray).Visual==null,"page input-layer switch cancels preview and capture");
            f.Mouse.mouseInteractiveLayers.Pop();f.Press(2.5f);Move(f,5);f.Release(5);
            Check(f.Quantity==1 && Preview(f.Tray).Visual==null,"in-tray release preserves reservation and disposes preview");
            f.Press(2.5f);Move(f,12);f.Tray.SetInteractionAllowed(false);f.Release(12);
            Check(f.Quantity==1 && Preview(f.Tray).Visual==null,"tray state gate cancels preview without waiting for source root deactivation");
            f.Tray.SetInteractionAllowed(true);
            f.Press(-1.8f);Move(f,5);var offset=Preview(f.Fridge).Visual.transform.position.x;
            Check(Math.Abs(offset-4.2f)<.001f,"off-center grab maps captured press offset through authored tray scale");
            Move(f,-6);Check(Math.Abs(Preview(f.Fridge).Visual.transform.position.x-(-6.8f))<.001f,"direction reversal retains only the original grip offset");
            f.Mouse.OnMouseCancelPress();f.Release(-6);
            Stable(f,"final reuse");CleanupPassed=true;
        }
        using(var f=Create())
        {
            var catalog=Get<CK01GeneratedDataCatalog>(f.Session,"dataCatalog");
            var flour=catalog.Tables.Single(t=>t.TableName=="Items").Rows.Single().Asset;
            var carrot=new CK01ItemData{name="carrot",stack_type="stack",icon_sprite=new Sprite{name="carrot"}};
            var tables=catalog.Tables.Select(t=>t.TableName=="Items"?new CK01GeneratedDataTable(t.TableName,t.AggregateAsset,new[]{new CK01GeneratedDataRowReference("flour",flour),new CK01GeneratedDataRowReference("carrot",carrot)}):t.TableName=="InitialInventory"?new CK01GeneratedDataTable(t.TableName,t.AggregateAsset,new[]{new CK01GeneratedDataRowReference("flour",new CK01InitialInventoryData{quantity=4}),new CK01GeneratedDataRowReference("carrot",new CK01InitialInventoryData{quantity=4})}):t).ToArray();
            catalog.ReplaceTables(tables);string diagnostic;Check(f.Session.Inventory.TryRebuildFromDataCatalog(out diagnostic),"second item initializes from actual catalog");
            var binder=Get<ShortCycleFridgeBinder>(f.Session,"fridgeBinder");Check(binder.BindView(catalog,f.Session.Inventory,null,false,f.Context,out diagnostic),"actual fridge rebind includes both items");
            var fridgeSlots=Get<FridgeSlot[]>(binder,"slots");var second=fridgeSlots.Single(x=>x.ItemId=="carrot");
            second.transform.position=new Vector3(-8,0,0);second.gameObject.AddComponent<SpriteRenderer>();Style(second,"other fridge");
            var b=second.gameObject.AddComponent<ShortCycleMouseActionBinding>();b.Configure(f.Router,ShortCycleActionNames.InventoryTake);
            var button=second.gameObject.AddComponent<Button_MouseInteract>();button.selectEvent=new UnityEvent();button.selectEvent.AddListener(b.DispatchFromMouseEvent);Call(button,"Start");
            var drag=second.gameObject.AddComponent<MouseDraggableObject>();drag.allowedDropTags=DropTag.FridgeIngredient;
            drag.startDraggingEvent=new UnityEvent();drag.draggingUpdateEvent=new UnityEvent();drag.endDraggingEvent=new UnityEvent();drag.cancelDraggingEvent=new UnityEvent();
            drag.cancelDraggingEvent.AddListener(b.CancelDragVisual);drag.onDropHitEvent=new UnityEvent<DropZone>();drag.onDropHitEvent.AddListener(b.DispatchFromDropHitEvent);drag.onDropMissedEvent=new UnityEvent();drag.onDropMissedEvent.AddListener(b.DispatchFromMissedDropEvent);Wire(second,drag);
            var col=second.gameObject.AddComponent<Collider2D>();col.ContainsPoint=p=>Vector3.Distance(p,second.transform.position)<1;Physics2D.Colliders.Add(col);Call(b,"OnEnable");
            f.Press(-2);Move(f,5);f.Release(5);f.Press(-8);Move(f,5);
            Check(Preview(second).Visual.GetComponentsInChildren<SpriteRenderer>(true).Any(x=>x.sprite==carrot.icon_sprite),"different source uses its own icon with the tray frame");f.Release(5);
            Check(f.Quantity==2 && Slots(f)[1].ItemId=="carrot","different sources reserve independently in the same scene");
            f.Press(2.5f);Move(f,12);f.Release(12);
            Check(f.Quantity==1 && f.Tray.ItemId=="carrot" && !Slots(f)[1].gameObject.activeSelf,"real tray refresh compacts the second item into the first authored slot");
            Check(!f.Context.placeholder_states.Any(x=>x.item_id=="flour" && x.taken_count>0) && f.Context.placeholder_states.Any(x=>x.item_id=="carrot" && x.taken_count==1),"compaction clears only first item reservation and keeps second identity reserved");
            f.Press(2.5f);Move(f,12);Check(Preview(f.Tray).Visual.GetComponentsInChildren<SpriteRenderer>(true).Any(x=>x.sprite==carrot.icon_sprite),"compacted slot is immediately pickable with new item appearance");f.Release(12);
            Check(f.Quantity==0 && Vector3.Distance(f.Tray.transform.position,new Vector3(2.5f,0,0))<.0001f,"different-item return empties without moving the reused root");
            Check(f.Context.placeholder_states.Count==0,"both original item identities are unreserved after immediate second pickup return");Call(b,"OnDisable");
        }
        return "PASS: production pointer and authored DragContainer event chain; persistent same-scene reuse and render-only tray preview.";
    }
}
