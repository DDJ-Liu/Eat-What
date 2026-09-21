using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Spine.Unity;

// Tests saved reference scenes. All animation, input and equipment changes are Play-only.
internal static class MIG63SpineSceneProbe
{
    static SkeletonMecanim[] actors;
    static HorizontalPlayerController movement;
    static float startX;
    static Camera camera;
    static RenderTexture target;

    [MenuItem("Tools/MIG63/Validation/Spine Sample Saved Scene")]
    public static void Sample() => MIG63ValidationSession.Begin("spine-scene", "Assets/Scenes/Spine Sample.unity");

    [MenuItem("Tools/MIG63/Validation/Horizontal Saved Scene")]
    public static void Horizontal() => MIG63ValidationSession.Begin("spine-scene", "Assets/Scenes/ToolTests/HorizontalPlayerControllerTest.unity");

    static void Check(bool value, string message) => MIG63ValidationSession.Check(value, message);
    static void Next(int stage, double delay = .4) => MIG63ValidationSession.Advance(stage, delay);
    static void Input(float value)
    {
        if (movement != null) movement.SetHorizontalInput(value);
        else { var buttons = UnityEngine.Object.FindObjectsByType<Button_MouseInteract>(FindObjectsSortMode.None).Where(b => b.name == (value == 0 ? "Idle" : "Walk")).ToArray(); Check(buttons.Length == 2, "both saved animation button events present"); foreach (var button in buttons) button.TriggerSelect(); }
    }
    static void Capture(string name)
    {
        var bounds = actors[0].GetComponent<Renderer>().bounds;
        foreach (var actor in actors.Skip(1)) bounds.Encapsulate(actor.GetComponent<Renderer>().bounds);
        camera.transform.position = new Vector3(bounds.center.x, bounds.center.y, -10);
        camera.orthographicSize = Mathf.Max(bounds.size.y * .65f, bounds.size.x / (16f / 9f) * .65f, 1);
        camera.Render();
        var previous = RenderTexture.active;
        var texture = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
        try {
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            texture.Apply();
            File.WriteAllBytes(Path.Combine(MIG63ValidationSession.Output, name + ".png"), texture.EncodeToPNG());
        } finally { RenderTexture.active = previous; UnityEngine.Object.DestroyImmediate(texture); }
    }
    internal static void Cleanup()
    {
        if (camera != null) { camera.targetTexture = null; UnityEngine.Object.DestroyImmediate(camera.gameObject); }
        if (target != null) { target.Release(); UnityEngine.Object.DestroyImmediate(target); target = null; }
    }
    internal static void Tick()
    {
        int stage = MIG63ValidationSession.Stage;
        if (stage == 0) {
            actors = UnityEngine.Object.FindObjectsByType<SkeletonMecanim>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            movement = UnityEngine.Object.FindFirstObjectByType<HorizontalPlayerController>();
            Check(actors.Length == (movement == null ? 2 : 1), "saved scene retains expected actor count");
            Check(actors.All(a => a.valid && a.Skeleton.Data.Version == "3.8.99"), "all saved actors initialize Tomato 3.8.99");
            Check(actors.All(a => a.GetComponent<MeshRenderer>().enabled && a.GetComponent<MeshFilter>().sharedMesh.vertexCount > 0), "renderers enabled after Start and meshes populated");
            Check(actors.All(a => a.GetComponent<Renderer>().sharedMaterials.All(m => m != null && m.shader.isSupported && m.shader.name != "Hidden/InternalErrorShader")), "all active Spine materials supported");
            if (movement != null) {
                Check(actors[0].GetComponent<SpineRuntimeMeshRendererBootstrap>() != null, "serialized delayed Renderer bootstrap retained");
                foreach (var field in new[]{"useProjectInputManagerFallback", "useKeyboardFallbackWhenInputManagerMissing"})
                    typeof(HorizontalPlayerController).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(movement, false);
                startX = movement.transform.position.x;
            }
            camera = new GameObject("MIG63 temporary saved scene capture").AddComponent<Camera>();
            camera.orthographic = true; camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.12f, .16f, .2f, 1); camera.nearClipPlane = .1f; camera.farClipPlane = 100;
            target = new RenderTexture(1920, 1080, 24); target.Create(); camera.targetTexture = target;
            Capture("01-initial"); Input(1); Next(1, 1.3); return;
        }
        if (stage == 1) {
            Check(actors.All(a => a.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("walk")), "saved Animator enters walk");
            if (movement != null) { Check(movement.transform.position.x > startX && movement.IsFacingRight, "saved Rigidbody controller moves right"); startX = movement.transform.position.x; }
            Input(-1); Next(2, 1.3); return;
        }
        if (stage == 2) {
            if (movement != null) Check(movement.transform.position.x < startX && !movement.IsFacingRight, "saved controller reverses and flips");
            Input(0); Next(3, 1.3); return;
        }
        if (stage == 3) {
            Check(actors.All(a => a.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("idle")), "saved Animator returns to idle");
            if (movement == null) {
                var sample = UnityEngine.Object.FindObjectsByType<CharacterEquipment>(FindObjectsSortMode.None).Single(e => e.name == "Sample");
                var saved = sample.equipmentSlots.ToDictionary(e => e.slotName, e => e.skinName);
                var buttons = UnityEngine.Object.FindObjectsByType<Button_MouseInteract>(FindObjectsSortMode.None);
                buttons.Single(b => b.name == "Body").selectEvent.Invoke(); buttons.Single(b => b.name == "Body").delayedSelectEvent.Invoke();
                Check(sample.equipmentSlots.All(e => string.IsNullOrEmpty(e.skinName)), "saved Body button unequips all");
                buttons.Single(b => b.name == "HairA").selectEvent.Invoke(); buttons.Single(b => b.name == "HairA").delayedSelectEvent.Invoke();
                Check(sample.equipmentSlots.Single(e => e.slotName == "Hair").skinName == "Hair_A", "saved Hair A button binding works");
                buttons.Single(b => b.name == "HairB").selectEvent.Invoke(); buttons.Single(b => b.name == "HairB").delayedSelectEvent.Invoke();
                Check(sample.equipmentSlots.Single(e => e.slotName == "Hair").skinName == "Hair_B", "saved Hair B button binding works");
                buttons.Single(b => b.name == "Bottom").selectEvent.Invoke(); buttons.Single(b => b.name == "Bottom").delayedSelectEvent.Invoke();
                Check(sample.equipmentSlots.Single(e => e.slotName == "Bottom").skinName == "BottomCloth_A", "saved Bottom button slot contract works");
                sample.EquipMultiple(saved);
            }
            foreach (var e in UnityEngine.Object.FindObjectsByType<CharacterEquipment>(FindObjectsSortMode.None)) e.EquipSlotByName("Hair", "Hair_B");
            var test = UnityEngine.Object.FindFirstObjectByType<CharacterTestController>();
            if (test != null) test.SyncEquipment();
            Next(4); return;
        }
        if (stage == 4) {
            Check(actors.All(a => a.Skeleton.Skin.Name == "character-combined"), "saved equipment bindings apply combined skins");
            Capture("02-equipment");
            foreach (var a in actors) a.GetComponent<Animator>().SetTrigger("Kick");
            Next(5); return;
        }
        if (stage == 5) {
            Check(actors.All(a => a.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("kick")), "saved Animator enters kick");
            Capture("03-kick"); MIG63ValidationSession.Finish();
        }
    }
}
