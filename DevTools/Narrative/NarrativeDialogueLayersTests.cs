using System;
using System.Collections.Generic;
using System.Reflection;
using EatWhat.Narrative.Core;
using EatWhat.Narrative.Unity;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

internal static class NarrativeDialogueLayersTests
{
    static readonly List<string> details = new List<string>();
    static void Check(bool value, string name) { if (!value) throw new Exception("Dialogue layers: " + name); details.Add(name); }
    static void Near(double actual, double expected, string name) { Check(Math.Abs(actual - expected) < 1e-6, name); }
    static void Life(object target, string method) { target.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, null); }
    static void Invalid(Action work, string name) { bool bad = false; try { work(); } catch (ArgumentException) { bad = true; } Check(bad, name); }
    static DialoguePresentationSnapshot Snapshot(float amount)
    {
        var data = new DialoguePresentationData(); data.tier[0].normalDim = amount;
        var dialogue = new NarrativeDialogue("dlg_dim", NarrativeDialogueLevel.Normal, null,
            new NarrativeProfileNames("", "", "", "", "", "", "", "", ""), "", "ln_dim", "",
            new NarrativeCommandSource(NarrativeCommandParser.Parse("")));
        var result = DialoguePresentationResolver.Resolve(data, dialogue);
        Check(result.Success, "actual resolver snapshot valid amount=" + amount);
        return result.Snapshot;
    }
    static int Main()
    {
        var root = new GameObject("PortableDialogue");
        var canvas = root.AddComponent<NarrativeDialogueCanvas>();
        var font = new TMP_FontAsset { material = new Material() }; var material = font.material;
        canvas.ConfigureFont(font); canvas.EnsureCreated();
        Check(canvas.FontAsset == font && canvas.SharedFontMaterial == material && font.material == material, "existing font/material handles reused without mutation");
        var custom = new Material(); canvas.ConfigureFont(font, custom);
        Check(canvas.SharedFontMaterial == custom && font.material == material, "optional shared material does not mutate font asset");
        var overlay = canvas.RootCanvas; var scaler = overlay.GetComponent<CanvasScaler>();
        Check(overlay.renderMode == RenderMode.ScreenSpaceOverlay, "overlay has no scene camera dependency");
        Check(overlay.sortingOrder == 200 && overlay.sortingOrder > 100, "default order above current bubble order 100");
        Check(scaler.referenceResolution.x == 2560 && scaler.referenceResolution.y == 1440, "2560 by 1440 reference pixels");
        Check(scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize && scaler.matchWidthOrHeight == 1 && scaler.screenMatchMode == CanvasScaler.ScreenMatchMode.MatchWidthOrHeight, "height-only matching equals bubble convention");
        Check(overlay.transform.parent == root.transform && overlay.transform.childCount == 3, "portable root owns three runtime layers");
        Check(canvas.DimLayer.GetSiblingIndex() == 0 && canvas.PortraitLayer.GetSiblingIndex() == 1 && canvas.DialogueLayer.GetSiblingIndex() == 2, "dim then portrait then dialogue order");
        Check(canvas.PortraitLayer.childCount == 0 && canvas.DialogueLayer.childCount == 0, "portrait and dialogue slots empty in layer card");
        foreach (var layer in new[] { canvas.DimLayer, canvas.PortraitLayer, canvas.DialogueLayer })
            Check(layer.anchorMin.x == 0 && layer.anchorMin.y == 0 && layer.anchorMax.x == 1 && layer.anchorMax.y == 1 && layer.offsetMin.x == 0 && layer.offsetMax.y == 0, "full-screen stretched layer " + layer.gameObject.name);
        var image = canvas.DimLayer.GetComponent<Image>(); var group = canvas.DimLayer.GetComponent<CanvasGroup>();
        Check(image.color.r == 0 && image.color.g == 0 && image.color.b == 0 && image.color.a == 0, "initial full-screen black surface transparent");
        Check(!image.raycastTarget && !group.interactable && !group.blocksRaycasts && overlay.GetComponent<GraphicRaycaster>() == null, "dim layer never intercepts pointer input");
        canvas.EnsureCreated(); Check(canvas.RootCanvas == overlay && root.transform.childCount == 1, "initialization idempotent");
        var dim = root.AddComponent<NarrativeSceneDimmer>(); Life(dim, "OnEnable");
        var a = new object(); var b = new object();
        dim.RequestNormal(a, Snapshot(.8f), 2); Near(dim.CurrentAmount, 0, "request starts at current amount without jump");
        Near(dim.TargetAmount, .8, "normalDim consumed from real snapshot");
        dim.Advance(.5); Near(dim.CurrentAmount, .2, "quarter-duration dim amount"); Near(image.color.a, .2, "actual component applies alpha to image");
        dim.Advance(.5); Near(dim.CurrentAmount, .4, "half-duration dim amount");
        dim.Advance(1); Near(dim.CurrentAmount, .8, "dim reaches target at exact duration");
        Check(!dim.IsTransitioning, "transition completes at target"); dim.Advance(100); Near(dim.CurrentAmount, .8, "overshoot remains clamped");
        dim.RequestNormal(b, Snapshot(.3f), 0); Check(dim.RequestCount == 2, "two owner requests retained"); Near(dim.TargetAmount, .8, "multiple requests use maximum not sum");
        Check(dim.Withdraw(a, 2), "withdraw strongest request succeeds"); Near(dim.CurrentAmount, .8, "withdraw starts from current without jump");
        Near(dim.TargetAmount, .3, "withdraw falls to remaining request"); dim.Advance(1); Near(dim.CurrentAmount, .55, "fallback halfway");
        dim.Advance(1); Near(dim.CurrentAmount, .3, "fallback reaches remaining request");
        dim.Withdraw(b, 1); dim.Advance(.5); Near(dim.CurrentAmount, .15, "last withdrawal returns toward zero");
        dim.Advance(.5); Near(dim.CurrentAmount, 0, "no requests returns to zero");
        Check(dim.RequestCount == 0 && !dim.Withdraw(a, 2), "unknown withdrawal harmless");
        dim.Request(a, .8f, 4); dim.Advance(1); Near(dim.CurrentAmount, .2, "mid-transition initial amount");
        dim.Request(b, 1, 2); Near(dim.CurrentAmount, .2, "stronger mid-transition request does not jump");
        dim.Advance(1); Near(dim.CurrentAmount, .6, "new transition starts from current amount");
        dim.Withdraw(b, 2); Near(dim.CurrentAmount, .6, "mid-transition withdrawal continuous");
        dim.Advance(1); Near(dim.CurrentAmount, .7, "withdrawal continues to other maximum from current");
        dim.Request(a, .1f, 2); Check(dim.RequestCount == 1, "same owner replaces rather than duplicates");
        Near(dim.CurrentAmount, .7, "same-owner replacement continuous"); dim.Advance(1); Near(dim.CurrentAmount, .4, "replacement moves down from current");
        dim.Request(a, .65f, 0); Near(dim.CurrentAmount, .65, "zero-duration request immediate");
        dim.Withdraw(a, 0); Near(dim.CurrentAmount, 0, "zero-duration withdrawal immediate");
        dim.Request(a, .8f, 2); dim.Advance(.5); dim.Advance(0); Near(dim.CurrentAmount, .2, "zero advance preserves transition");
        Check(!dim.Withdraw(new object(), 100), "unrecognized owner does not restart transition"); dim.Advance(1.5); Near(dim.CurrentAmount, .8, "unknown withdrawal preserves original deadline");
        dim.Request(a, .4f, 2); Time.unscaledDeltaTime = 1; Life(dim, "Update"); Near(dim.CurrentAmount, .6, "runtime Update consumes unscaled delta");
        var oldRoot = canvas.RootCanvas.gameObject; Life(canvas, "OnDisable");
        Check(!oldRoot.activeSelf && oldRoot.Destroyed && canvas.RootCanvas == null, "canvas releases only owned runtime root");
        Life(canvas, "OnEnable"); Near(canvas.DimLayer.GetComponent<Image>().color.a, .6, "recreated canvas retains last applied dim render value");
        Life(dim, "OnDisable"); Check(dim.RequestCount == 0 && dim.CurrentAmount == 0 && dim.TargetAmount == 0, "disable clears own requests and amount");
        Near(canvas.DimLayer.GetComponent<Image>().color.a, 0, "disable resets rendered alpha");
        Invalid(() => dim.Request(null, .5f, 1), "null owner rejected");
        Invalid(() => dim.Request(a, -1, 1), "negative dim rejected");
        Invalid(() => dim.Request(a, 2, 1), "dim above one rejected");
        Invalid(() => dim.Request(a, float.NaN, 1), "NaN dim rejected");
        Invalid(() => dim.Request(a, .5f, -1), "negative duration rejected");
        Invalid(() => dim.Advance(double.PositiveInfinity), "infinite time rejected");
        Invalid(() => dim.RequestNormal(a, null, 1), "missing snapshot rejected");
        Check(dim.RequestCount == 0 && dim.CurrentAmount == 0, "invalid requests do not mutate state");
        Check(!typeof(INarrativeSceneBlur).IsAssignableFrom(typeof(NarrativeSceneDimmer)), "blur interface reserved without pretending implementation");
        Life(canvas, "OnDestroy"); Check(canvas.RootCanvas == null, "destroy cleanup idempotent");
        Console.WriteLine("DIALOGUE_LAYERS_RESULT={\"passed\":true,\"checks\":" + details.Count + ",\"actualComponents\":true,\"actualResolverSnapshots\":true,\"nativeUnity\":false,\"boundary\":\"external engine hierarchy UI TMP handles only\",\"details\":[\"" + string.Join("\",\"", details.ToArray()) + "\"]}");
        return 0;
    }
}
