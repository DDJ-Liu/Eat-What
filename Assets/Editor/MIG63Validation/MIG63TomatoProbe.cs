using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Spine.Unity;

// Builds an unsaved runtime fixture from the existing Tomato data; no asset imports/builds.
internal static class MIG63TomatoProbe
{
 static string Output => MIG63ValidationSession.Output;
 static SkeletonAnimation actor; static SkeletonMecanim mecanim; static HorizontalPlayerController movement;
 static CharacterEquipment equipment; static Animator animator; static Camera camera; static RenderTexture target;
 static float previousX;
 static readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();
 static void Check(bool condition, string message) => MIG63ValidationSession.Check(condition, message);
 static void Advance(int stage) => MIG63ValidationSession.Advance(stage, .75);

 [MenuItem("Tools/MIG63/Validation/Tomato Dual Path GPU %#F10")]
 public static void Run()
 {
     var asset = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>("Assets/Scripts/Spine_Package/Avatars/Tomato/skeleton_SkeletonData.asset");
     if (asset == null || asset.GetSkeletonData(false).Version != "3.8.99")
         throw new InvalidOperationException("Requires the approved Tomato 3.8.99 fixture.");
     MIG63ValidationSession.Begin("tomato", null);
 }
 static void Set(object instance,string name,object value){instance.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(instance,value);}
 static CharacterEquipment Dress(GameObject go){var c=go.AddComponent<CharacterEquipment>();c.baseSkin="Tomato_Non";c.autoOptimize=false;c.equipmentSlots=new List<CharacterEquipment.EquipmentSlot>();foreach(var part in new[]{"Hair","Eyes","Ear","Mouth","TopCloth","BottomCloth","Shoes"})c.equipmentSlots.Add(new CharacterEquipment.EquipmentSlot(part,"Tomato_"+part+"_A"));return c;}
 static void Setup(){
  var data=AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>("Assets/Scripts/Spine_Package/Avatars/Tomato/skeleton_SkeletonData.asset");
  var go=new GameObject("Tomato SkeletonAnimation - runtime only");go.SetActive(false);go.layer=30;actor=go.AddComponent<SkeletonAnimation>();actor.skeletonDataAsset=data;actor.initialSkinName="Tomato_Non";actor.Initialize(true);equipment=Dress(go);movement=go.AddComponent<HorizontalPlayerController>();Set(movement,"useProjectInputManagerFallback",false);Set(movement,"useKeyboardFallbackWhenInputManagerMissing",false);Set(movement,"spineAnimation",actor);Set(movement,"spineIdleAnimation","idle");Set(movement,"spineWalkAnimation","walk");movement.MoveSpeed=1;go.SetActive(true);
  var mg=new GameObject("Tomato SkeletonMecanim - runtime only");mg.SetActive(false);mg.layer=30;mg.transform.position=new Vector3(5,0,0);animator=mg.AddComponent<Animator>();var controller=new AnimatorController{name="MIG63 temporary in-memory controller",hideFlags=HideFlags.DontSave};owned.Add(controller);controller.AddLayer("Base Layer");controller.AddParameter("IsMoving",AnimatorControllerParameterType.Bool);var sm=controller.layers[0].stateMachine;owned.Add(sm);var timing=new GameObject("Timing");timing.transform.SetParent(mg.transform,false);
  foreach(var name in new[]{"idle","walk","kick"}){var clip=new AnimationClip{name=name,hideFlags=HideFlags.DontSave};owned.Add(clip);clip.SetCurve("Timing",typeof(Transform),"localPosition.x",AnimationCurve.Linear(0,0,data.GetSkeletonData(false).FindAnimation(name).Duration,0));var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=true;AnimationUtility.SetAnimationClipSettings(clip,settings);var state=sm.AddState(name);owned.Add(state);state.motion=clip;if(name=="idle")sm.defaultState=state;}
  animator.runtimeAnimatorController=controller;mecanim=mg.AddComponent<SkeletonMecanim>();mecanim.skeletonDataAsset=data;mecanim.initialSkinName="Tomato_Non";mecanim.Initialize(true);Dress(mg);mg.SetActive(true);
  camera=new GameObject("MIG63 Capture Camera").AddComponent<Camera>();camera.cullingMask=1<<30;camera.orthographic=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.16f,.2f,1);camera.nearClipPlane=.1f;camera.farClipPlane=100;target=new RenderTexture(1920,1080,24);target.Create();camera.targetTexture=target;
 }
 static void Frame(){var bounds=actor.GetComponent<Renderer>().bounds;bounds.Encapsulate(mecanim.GetComponent<Renderer>().bounds);camera.transform.position=new Vector3(bounds.center.x,bounds.center.y,-10);camera.orthographicSize=Mathf.Max(bounds.size.y*.65f,bounds.size.x/1.77778f*.65f,1);}
 static void Capture(string name){var prior=RenderTexture.active;var texture=new Texture2D(target.width,target.height,TextureFormat.RGBA32,false);try{RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,target.width,target.height),0,0);texture.Apply();File.WriteAllBytes(Output+"/"+name+".png",texture.EncodeToPNG());}finally{RenderTexture.active=prior;UnityEngine.Object.DestroyImmediate(texture);}}
 internal static void Cleanup(){if(camera!=null)camera.targetTexture=null;if(target!=null){target.Release();UnityEngine.Object.DestroyImmediate(target);target=null;}if(animator!=null)animator.runtimeAnimatorController=null;foreach(var obj in owned.AsEnumerable().Reverse())if(obj!=null)UnityEngine.Object.DestroyImmediate(obj);owned.Clear();}
 internal static void Tick()
 {
  var stage = MIG63ValidationSession.Stage;
  if(stage==0){Setup();Advance(1);return;}
  if(stage==1){Check(actor.valid&&mecanim.valid,"both SkeletonAnimation and SkeletonMecanim initialize");Check(actor.GetComponent<MeshFilter>().sharedMesh.vertexCount>0&&mecanim.GetComponent<MeshFilter>().sharedMesh.vertexCount>0,"both paths generate visible attachment meshes");Check(actor.AnimationState.GetCurrent(0).Animation.Name=="idle","controller initializes idle");Frame();Advance(2);return;}
  if(stage==2){Capture("01-idle");previousX=actor.transform.position.x;movement.SetHorizontalInput(1);animator.Play("walk",0,0);Advance(3);return;}
  if(stage==3){Check(actor.transform.position.x>previousX&&movement.IsFacingRight,"horizontal controller moves right");Check(actor.AnimationState.GetCurrent(0).Animation.Name=="walk"&&actor.AnimationState.GetCurrent(0).TrackTime>0,"walk animation advances");Check(animator.GetCurrentAnimatorStateInfo(0).IsName("walk")&&animator.GetCurrentAnimatorStateInfo(0).normalizedTime>0,"Mecanim walk time advances");Frame();Advance(4);return;}
  if(stage==4){Capture("02-walk");previousX=actor.transform.position.x;movement.SetHorizontalInput(-1);Advance(5);return;}
  if(stage==5){Check(actor.transform.position.x<previousX&&!movement.IsFacingRight,"horizontal controller reverses and flips");movement.StopMovement();equipment.EquipSlotByName("Hair","Hair_B");equipment.EquipSlotByName("Eyes","Eyes_B");mecanim.GetComponent<CharacterEquipment>().EquipSlotByName("Hair","Hair_C");Advance(6);return;}
  if(stage==6){Check(actor.AnimationState.GetCurrent(0).Animation.Name=="idle","stopping restores idle");Check(actor.Skeleton.Skin.Name=="character-combined"&&mecanim.Skeleton.Skin.Name=="character-combined","CharacterEquipment applies combined skin on both paths");Frame();Advance(7);return;}
  if(stage==7){Capture("03-equipment");actor.AnimationState.SetAnimation(0,"kick",true);animator.Play("kick",0,0);Advance(8);return;}
  if(stage==8){Check(actor.AnimationState.GetCurrent(0).Animation.Name=="kick"&&animator.GetCurrentAnimatorStateInfo(0).IsName("kick"),"kick executes on both animation paths");Frame();Advance(9);return;}
  if(stage==9){Capture("04-kick");MIG63ValidationSession.Finish();}
 }
}
