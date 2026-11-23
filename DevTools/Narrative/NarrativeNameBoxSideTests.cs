using System;
using System.Collections.Generic;
using System.Reflection;
using EatWhat.Narrative.Core;
using EatWhat.Narrative.Unity;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Actual BoxView operations against the existing external UI/TMP handles.
// preferredWidth is supplied, never calculated here; no native text layout is simulated.
internal static class NarrativeNameBoxSideTests
{
    static readonly List<string> details = new List<string>();
    static void Check(bool value, string why) { if (!value) throw new Exception("Name side: " + why); details.Add(why); }
    static void Near(float value, float expected, string why) { Check(Math.Abs(value - expected) < .001f, why); }
    static void Set(object target, string field, object value)
    { target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value); }
    static object Get(object target, string field)
    { return target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target); }
    static void Disable(NarrativeDialogueBoxView view)
    { view.GetType().GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(view, null); }
    static RectTransform Rect(string name, Transform parent)
    { var node = new GameObject(name, typeof(RectTransform)); node.transform.SetParent(parent, false); return (RectTransform)node.transform; }
    static Image Image(string name, Transform parent) { return Rect(name, parent).gameObject.AddComponent<Image>(); }
    static Image ClickSurface(string name, Transform parent)
    {
        var image = Image(name, parent);
        image.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
        return image;
    }
    sealed class Ui
    {
        public readonly NarrativeDialogueBoxView View;
        public readonly RectTransform Name;
        public readonly TextMeshProUGUI Text;
        public Ui(bool authored)
        {
            var root = new GameObject("Name side HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = root.AddComponent<NarrativeDialogueCanvas>(); canvas.ConfigureFont(new TMP_FontAsset { material = new Material() });
            View = root.AddComponent<NarrativeDialogueBoxView>(); Set(View, "dialogueCanvas", canvas);
            View.Configure(new NarrativeAdvanceInputModel(), null);
            if (!authored) return;
            var dim = Image("Dim", root.transform); var portraits = Rect("Portraits", root.transform); var dialogue = Rect("Dialogue", root.transform);
            Set(canvas, "authoredCanvas", root.GetComponent<Canvas>()); Set(canvas, "authoredDimLayer", dim.transform); Set(canvas, "authoredDimImage", dim);
            Set(canvas, "authoredPortraitLayer", portraits); Set(canvas, "authoredDialogueLayer", dialogue);
            var visual = Rect("Visual", dialogue); var group = visual.gameObject.AddComponent<CanvasGroup>();
            var plate = Rect("Box", visual); plate.sizeDelta = new Vector2(2107, 333);
            var advance = ClickSurface("Advance", visual); var body = Rect("Body", plate).gameObject.AddComponent<TextMeshProUGUI>();
            Name = Rect("Name", plate); Name.anchorMin = Name.anchorMax = new Vector2(0, 1); Name.pivot = new Vector2(0, 1);
            Name.anchoredPosition = new Vector2(81, 52); Name.sizeDelta = new Vector2(311, 95);
            Text = Rect("Name text", Name).gameObject.AddComponent<TextMeshProUGUI>();
            Set(View, "authoredRoot", visual); Set(View, "authoredBox", plate); Set(View, "authoredNameBox", Name);
            Set(View, "authoredBodyText", body); Set(View, "authoredNameText", Text); Set(View, "authoredAdvanceSurface", advance);
            Set(View, "authoredAutoButton", ClickSurface("Auto", plate)); Set(View, "authoredFastForwardButton", ClickSurface("Fast", plate)); Set(View, "authoredGroup", group);
        }
    }
    static DialoguePresentationSnapshot Snapshot()
    {
        var commands = new NarrativeCommandSource(NarrativeCommandParser.Parse(""));
        var profiles = new NarrativeProfileNames("", "", "", "", "", "", "", "", "");
        var dialogue = new NarrativeDialogue("name-side", NarrativeDialogueLevel.Normal, new NarrativeCastEntry[0], profiles, "", "", "", commands);
        return DialoguePresentationResolver.Resolve(new DialoguePresentationData(), dialogue).Snapshot;
    }
    // Rectangle bounds for this fixture's unrotated, fixed-anchor direct child of a 2107-wide box.
    static float Left(RectTransform rect) { return (rect.anchorMin.x - .5f) * 2107 + rect.anchoredPosition.x - rect.pivot.x * rect.sizeDelta.x * rect.localScale.x; }
    static float Right(RectTransform rect) { return Left(rect) + rect.sizeDelta.x * rect.localScale.x; }
    public static void Run()
    {
        if (UnityEngine.EventSystems.EventSystem.current == null)
            new GameObject("Existing name-side event system").AddComponent<UnityEngine.EventSystems.EventSystem>();
        var snapshot = Snapshot(); var ui = new Ui(true); int created = UnityEngine.Object.Created;
        ui.View.Show(snapshot);
        Check(UnityEngine.Object.Created == created && ReferenceEquals(Get(ui.View, "nameBox"), ui.Name), "saved name node reused without creating nodes");
        Near(ui.Name.anchoredPosition.x, 81, "default uses user left X"); Near(ui.Name.anchoredPosition.y, 52, "default uses user left Y");
        Near(ui.Name.anchorMin.x, 0, "default left anchor"); Near(ui.Name.pivot.x, 0, "default left pivot");
        ui.View.LayoutFullLine("body", "short", true); float left = Left(ui.Name); float width = ui.Name.sizeDelta.x;
        ui.View.SetNameSide(true);
        Near(Right(ui.Name), -left, "right edge mirrors saved left edge about box middle");
        Near(ui.Name.anchoredPosition.x, -81, "right inset comes from saved X"); Near(ui.Name.anchoredPosition.y, 52, "right Y unchanged");
        Near(ui.Name.anchorMin.x, 1, "right anchor mirrored"); Near(ui.Name.pivot.x, 1, "right pivot mirrored");
        Near(ui.Name.sizeDelta.x, width, "switch keeps current width"); Check(ui.Name.gameObject.activeSelf, "switch keeps nonempty name visible");
        float edge = Right(ui.Name); ui.Text.preferredWidth = 620; ui.View.LayoutFullLine("next", "long name", true);
        Near(ui.Name.sizeDelta.x, 652, "width consumes supplied TMP preferred width plus original padding");
        Near(Right(ui.Name), edge, "long name keeps right edge fixed"); Check(Left(ui.Name) < -left - width, "long name expands leftward");
        ui.View.SetNameSide(true); Near(Right(ui.Name), edge, "repeated right call is idempotent");
        ui.Text.preferredWidth = 40; ui.View.LayoutFullLine("next", "short", true);
        Near(ui.Name.sizeDelta.x, 311, "short name retains authored minimum width"); Near(Right(ui.Name), edge, "shrinking keeps right edge fixed");
        Near(ui.Name.sizeDelta.y, 95, "authored height preserved");
        ui.View.SetNameSide(false); Near(ui.Name.anchoredPosition.x, 81, "left restores captured X"); Near(ui.Name.anchoredPosition.y, 52, "left restores captured Y");
        Near(ui.Name.anchorMax.x, 0, "left restores captured anchor"); Near(ui.Name.pivot.x, 0, "left restores captured pivot");
        ui.View.LayoutFullLine("body", "", false); Check(!ui.Name.gameObject.activeSelf, "empty name stays hidden on left through existing surface contract");
        ui.View.SetNameSide(true); Check(!ui.Name.gameObject.activeSelf, "switch right does not reveal empty name");
        ui.View.LayoutFullLine("body", "", false); Check(!ui.Name.gameObject.activeSelf, "empty name stays hidden on right");
        ui.View.Show(snapshot); Near(ui.Name.anchoredPosition.x, 81, "next segment resets left"); Near(ui.Name.anchorMin.x, 0, "next segment resets anchor");
        Check(!ui.Name.gameObject.activeSelf, "next segment entrance retains empty-name hiding");
        ui.View.SetNameSide(true); Disable(ui.View);
        Near(ui.Name.anchoredPosition.x, 81, "disable restores captured left before next initialization"); Near(ui.Name.sizeDelta.x, 311, "disable restores authored width");
        ui.Name.anchoredPosition = new Vector2(123, 88); ui.View.Show(snapshot); ui.View.SetNameSide(true);
        Near(ui.Name.anchoredPosition.x, -123, "new initialization captures edited left position"); Near(ui.Name.anchoredPosition.y, 88, "new initialization captures edited Y"); Disable(ui.View);

        var custom = new Ui(true); custom.Name.anchorMin = custom.Name.anchorMax = new Vector2(.15f, .8f);
        custom.Name.pivot = new Vector2(.25f, .4f); custom.Name.localScale = new Vector3(1.5f, 1, 1);
        custom.View.Show(snapshot); float originalLeft = Left(custom.Name); custom.View.SetNameSide(true);
        Near(Right(custom.Name), -originalLeft, "custom anchor/pivot mirror exact horizontal bounds");
        Near(custom.Name.anchorMin.y, .8f, "custom vertical anchor preserved"); Near(custom.Name.pivot.y, .4f, "custom vertical pivot preserved");
        edge = Right(custom.Name); custom.Text.preferredWidth = 800; custom.View.LayoutFullLine("body", "long", true);
        Near(Right(custom.Name), edge, "custom pivot and scale keep right edge while growing");
        custom.View.SetNameSide(true); Near(Right(custom.Name), edge, "repeated right keeps adjusted custom pivot position");
        custom.Text.preferredWidth = 0; custom.View.LayoutFullLine("body", "short", true); Near(Right(custom.Name), edge, "custom pivot keeps right edge while shrinking");
        custom.View.SetNameSide(false); Near(custom.Name.anchoredPosition.x, 81, "custom left position restored"); Near(custom.Name.anchorMin.x, .15f, "custom left anchor restored"); Near(custom.Name.pivot.x, .25f, "custom left pivot restored"); Disable(custom.View);

        var fallback = new Ui(false); fallback.View.Show(snapshot); var name = (RectTransform)Get(fallback.View, "nameBox");
        Near(name.anchoredPosition.x, 67, "automatic path retains original left default");
        fallback.View.SetNameSide(true); Near(name.anchoredPosition.x, -67, "automatic path mirrors same node");
        fallback.View.Show(snapshot); Near(name.anchoredPosition.x, 67, "automatic path resets per segment"); Disable(fallback.View);
        Console.WriteLine(JsonConvert.SerializeObject(new { result = "NAME_BOX_SIDE_PASS", checks = details.Count, details, nativeUnity = false, realTmpLayoutVerified = false }, Formatting.Indented));
    }
}
