using System;
using System.Collections.Generic;

namespace EatWhat.Narrative.Core
{
    public enum DialogueBoxMode { SlideUp, ScalePop }
    public enum DialogueSpeakerTransition { Fade, Cut }
    public enum DialogueBackgroundMode { DimOnly, BlurAndDim }
    public enum DialogueTurnMode { FaceSpeaker, None }
    public enum DialogueCgTransition { BlackFade, CrossFade }
    public enum DialogueControlResume { AfterBoxClosed, AfterCameraReturned }
    public enum DialogueContinuationMode { FullFlow, Seamless }

    [Serializable]
    public struct PresentationPoint
    {
        public float x, y;
        public PresentationPoint(float x, float y) { this.x = x; this.y = y; }
    }

    [Serializable]
    public struct PresentationColor
    {
        public float r, g, b, a;
        public PresentationColor(float r, float g, float b, float a)
        { this.r = r; this.g = g; this.b = b; this.a = a; }
    }

    [Serializable]
    public abstract class DialogueNamedTemplate
    {
        public string name = "default";
    }

    [Serializable]
    public sealed class DialogueTierConfig : DialogueNamedTemplate
    {
        [DialogueParameter("ratio")]
        public float normalDim = 0.40f;
        [DialogueParameter("s")]
        public float bubbleLifetime = 2.5f;
        [DialogueParameter("s")]
        public float bubbleFadeInTime = 0.2f;
        [DialogueParameter("px")]
        public float bubbleExitRise = 80f;
        [DialogueParameter("s")]
        public float bubbleExitFade = 0.4f;
        [DialogueParameter("none")]
        public NarrativeCommandExecutionMode commandExecutionMode = NarrativeCommandExecutionMode.Parallel;
    }

    [Serializable]
    public sealed class DialogueTriggerConfig : DialogueNamedTemplate
    {
        [DialogueParameter("u")]
        public float interactRadius = 2.1f;
        [DialogueParameter("u")]
        public float autoBubbleRadius = 2.9f;
        [DialogueParameter("s")]
        public float autoBubbleCooldown = 10f;
        [DialogueParameter("u")]
        public float markerBobAmplitude = 0.1f;
        [DialogueParameter("s")]
        public float markerBobPeriod = 1.2f;
        [DialogueParameter("s")]
        public float markerFadeBackTime = 0.3f;
        [DialogueParameter("RGBA")]
        public PresentationColor markerTalkedTint = new PresentationColor(0.5f, 0.5f, 0.5f, 1f);
    }

    [Serializable]
    public sealed class DialogueEntranceConfig : DialogueNamedTemplate
    {
        [DialogueParameter("none")]
        public NarrativeCommandExecutionMode order = NarrativeCommandExecutionMode.Sequential;
        [DialogueParameter("s")]
        public float letterboxTime = 0.3f;
        [DialogueParameter("s")]
        public float dimTime = 0.15f;
        [DialogueParameter("s")]
        public float portraitTime = 0.25f;
        [DialogueParameter("px")]
        public float portraitSlide = 160f;
        [DialogueParameter("bool")]
        public bool portraitFade = true;
        [DialogueParameter("none")]
        public DialogueBoxMode boxMode = DialogueBoxMode.SlideUp;
        [DialogueParameter("s")]
        public float boxTime = 0.2f;
        [DialogueParameter("px")]
        public float boxSlideDistance = 187f;
    }

    [Serializable]
    public sealed class PortraitStation
    {
        public string id;
        // Display-anchor offset from screen bottom centre, reference pixels 2560x1440.
        public PresentationPoint position;
        public float scale = 1f;
        public int layer;
        public PortraitStation() { }
        public PortraitStation(string id, float x, float y, float scale, int layer)
        { this.id = id; position = new PresentationPoint(x, y); this.scale = scale; this.layer = layer; }
    }

    [Serializable]
    public sealed class PortraitStationSet
    {
        public int actorCount;
        public PortraitStation[] entries;
        public PortraitStationSet() { }
        public PortraitStationSet(int actorCount, params PortraitStation[] entries)
        { this.actorCount = actorCount; this.entries = entries; }
    }

    [Serializable]
    public sealed class PortraitLayoutConfig : DialogueNamedTemplate
    {
        [DialogueParameter("px/×/layer")]
        public PortraitStationSet[] stations = {
            new PortraitStationSet(1, new PortraitStation("s1-center", 0, 0, 1, 0)),
            new PortraitStationSet(2, new PortraitStation("s2-left", -747, 0, 1, 0),
                new PortraitStation("s2-right", 747, 0, 1, 0)),
            new PortraitStationSet(3, new PortraitStation("s3-left", -853, 0, 1, 0),
                new PortraitStation("s3-center", 0, 0, 1, 1), new PortraitStation("s3-right", 853, 0, 1, 0)),
            new PortraitStationSet(4, new PortraitStation("s4-left-outer", -960, 0, 0.9f, 0),
                new PortraitStation("s4-left-inner", -400, 0, 1, 1),
                new PortraitStation("s4-right-inner", 400, 0, 1, 1),
                new PortraitStation("s4-right-outer", 960, 0, 0.9f, 0))
        };
        // Alignment uses the display anchor, never the opaque bounds of a portrait image.
        [DialogueParameter("bool")]
        public bool portraitBottomAlign = true;
    }

    [Serializable]
    public sealed class SpeakerHighlightConfig : DialogueNamedTemplate
    {
        [DialogueParameter("ratio")]
        public float listenerDim = 0.5f;
        [DialogueParameter("×")]
        public float speakerScale = 1.03f;
        [DialogueParameter("px")]
        public float speakerRise = 13f;
        [DialogueParameter("px")]
        public float speakerShiftInward = 40f;
        [DialogueParameter("none")]
        public DialogueSpeakerTransition transition = DialogueSpeakerTransition.Fade;
        [DialogueParameter("s")]
        public float transitionTime = 0.15f;
    }

    // Full-version storage only. No expression or symbol presentation is implemented here.
    [Serializable]
    public sealed class ExpressionConfig : DialogueNamedTemplate
    {
        [DialogueParameter("s")]
        public float crossFadeTime = 0.1f;
        [DialogueParameter("template-key")]
        public string expressionChangeTemplate = "hop";
        [DialogueParameter("px")]
        public PresentationPoint symbolOffset = new PresentationPoint(173f, -173f);
        [DialogueParameter("s")]
        public float symbolPopTime = 0.2f;
    }

    [Serializable]
    public sealed class DialogueTextConfig : DialogueNamedTemplate
    {
        [DialogueParameter("chars/s")]
        public float charsPerSecond = 30f;
        [DialogueParameter("s")]
        public float punctuationPause = 0.1f;
        [DialogueParameter("string")]
        public string punctuationSet = "，。！？～…、；：,.!?";
        [DialogueParameter("none")]
        public NarrativeTypingClickAction clickWhileTyping = NarrativeTypingClickAction.CompleteLine;
        [DialogueParameter("none")]
        public NarrativeTypingClickAction clickWhileTypingFast = NarrativeTypingClickAction.SkipToNext;
        [DialogueParameter("s")]
        public float autoWait = 1.2f;
        [DialogueParameter("×")]
        public float fastForwardMultiplier = 4f;
        [DialogueParameter("s")]
        public float advanceCooldown = 0.3f;
    }

    [Serializable]
    public sealed class SceneReactionConfig : DialogueNamedTemplate
    {
        [DialogueParameter("none")]
        public DialogueBackgroundMode background = DialogueBackgroundMode.DimOnly;
        [DialogueParameter("px")]
        public float blurRadius = 8f;
        [DialogueParameter("none")]
        public DialogueTurnMode turnMode = DialogueTurnMode.FaceSpeaker;
        [DialogueParameter("s")]
        public float turnDelay = 0.2f;
        // Full-version fields, not consumed by any presentation logic in this task.
        [DialogueParameter("s")]
        public float reactionStagger = 0.12f;
        [DialogueParameter("s")]
        public float reactionDuration = 1.2f;
    }

    [Serializable]
    public sealed class CGPresenterConfig : DialogueNamedTemplate
    {
        [DialogueParameter("none")]
        public DialogueCgTransition cgTransitionMode = DialogueCgTransition.BlackFade;
        [DialogueParameter("s")]
        public float cgFadeInTime = 0.4f;
        [DialogueParameter("s")]
        public float cgFadeOutTime = 0.4f;
        [DialogueParameter("bool")]
        public bool keepBoxDuringCG = true;
        [DialogueParameter("bool")]
        public bool keepPortraitsDuringCG = false;
        [DialogueParameter("bool")]
        public bool keepNameDuringCG = true;
        [DialogueParameter("×")]
        public float kenBurnsZoom = 1.08f;
        [DialogueParameter("px")]
        public float kenBurnsPanX = 53f;
        [DialogueParameter("px")]
        public float kenBurnsPanY = 0f;
        [DialogueParameter("s")]
        public float kenBurnsDuration = 8f;
        [DialogueParameter("none")]
        public NarrativeCurvePreset kenBurnsCurve = NarrativeCurvePreset.Linear;
        [DialogueParameter("s")]
        public float cgAdvanceCooldown = 1.5f;
    }

    [Serializable]
    public sealed class DialogueExitConfig : DialogueNamedTemplate
    {
        [DialogueParameter("none")]
        public NarrativeCommandExecutionMode order = NarrativeCommandExecutionMode.Sequential;
        [DialogueParameter("s")]
        public float letterboxOutTime = 0.3f;
        [DialogueParameter("s")]
        public float boxTime = 0.15f;
        [DialogueParameter("s")]
        public float portraitTime = 0.2f;
        [DialogueParameter("px")]
        public float portraitSlide = 160f;
        [DialogueParameter("s")]
        public float undimTime = 0.2f;
        [DialogueParameter("s")]
        public float cameraReturnTime = 0.5f;
        [DialogueParameter("none")]
        public DialogueControlResume controlResume = DialogueControlResume.AfterBoxClosed;
        [DialogueParameter("none")]
        public DialogueContinuationMode continuationMode = DialogueContinuationMode.FullFlow;
    }

    public sealed class PresentationConfigDiagnostic
    {
        public PresentationConfigDiagnostic(string group, string template, string field, string reason, bool isError = true)
        { Group = group; Template = template; Field = field; Reason = reason; IsError = isError; }
        public string Group { get; private set; }
        public string Template { get; private set; }
        public string Field { get; private set; }
        public string Reason { get; private set; }
        public bool IsError { get; private set; }
        public override string ToString() { return Group + "/" + Template + "/" + Field + ": " + Reason; }
    }

    /// <summary>Mutable authoring data, not a resolved runtime snapshot. Defaults exist only in these constructors.</summary>
    [Serializable]
    public sealed class DialoguePresentationData
    {
        public DialogueTierConfig[] tier = { new DialogueTierConfig() };
        public DialogueTriggerConfig[] trigger = { new DialogueTriggerConfig() };
        public DialogueEntranceConfig[] entrance = { new DialogueEntranceConfig() };
        public PortraitLayoutConfig[] layout = { new PortraitLayoutConfig() };
        public SpeakerHighlightConfig[] speaker = { new SpeakerHighlightConfig() };
        public ExpressionConfig[] expression = { new ExpressionConfig() };
        public DialogueTextConfig[] text = { new DialogueTextConfig() };
        public SceneReactionConfig[] reaction = { new SceneReactionConfig() };
        public CGPresenterConfig[] cg = { new CGPresenterConfig() };
        public DialogueExitConfig[] exit = { new DialogueExitConfig() };

        public DialogueTierConfig FindTier(string name, out IReadOnlyList<PresentationConfigDiagnostic> diagnostics)
        { return Find("tier", tier, name, out diagnostics); }
        public DialogueTriggerConfig FindTrigger(string name, out IReadOnlyList<PresentationConfigDiagnostic> diagnostics)
        { return Find("trigger", trigger, name, out diagnostics); }
        public DialogueEntranceConfig FindEntrance(string name, out IReadOnlyList<PresentationConfigDiagnostic> diagnostics)
        { return Find("entrance", entrance, name, out diagnostics); }
        public PortraitLayoutConfig FindLayout(string name, out IReadOnlyList<PresentationConfigDiagnostic> diagnostics)
        { return Find("layout", layout, name, out diagnostics); }
        public SpeakerHighlightConfig FindSpeaker(string name, out IReadOnlyList<PresentationConfigDiagnostic> diagnostics)
        { return Find("speaker", speaker, name, out diagnostics); }
        public ExpressionConfig FindExpression(string name, out IReadOnlyList<PresentationConfigDiagnostic> diagnostics)
        { return Find("expression", expression, name, out diagnostics); }
        public DialogueTextConfig FindText(string name, out IReadOnlyList<PresentationConfigDiagnostic> diagnostics)
        { return Find("text", text, name, out diagnostics); }
        public SceneReactionConfig FindReaction(string name, out IReadOnlyList<PresentationConfigDiagnostic> diagnostics)
        { return Find("reaction", reaction, name, out diagnostics); }
        public CGPresenterConfig FindCg(string name, out IReadOnlyList<PresentationConfigDiagnostic> diagnostics)
        { return Find("cg", cg, name, out diagnostics); }
        public DialogueExitConfig FindExit(string name, out IReadOnlyList<PresentationConfigDiagnostic> diagnostics)
        { return Find("exit", exit, name, out diagnostics); }

        private static T Find<T>(string group, T[] values, string requested,
            out IReadOnlyList<PresentationConfigDiagnostic> diagnostics) where T : DialogueNamedTemplate
        {
            string key = string.IsNullOrEmpty(requested) ? "default" : requested;
            var issues = new List<PresentationConfigDiagnostic>();
            T found = null, fallback = null;
            int matches = 0, defaults = 0;
            if (values != null) foreach (var value in values)
            {
                if (value == null) continue;
                if (string.Equals(value.name, key, StringComparison.Ordinal)) { found = value; matches++; }
                if (string.Equals(value.name, "default", StringComparison.Ordinal)) { fallback = value; defaults++; }
            }
            if (defaults != 1) issues.Add(new PresentationConfigDiagnostic(group, "default", "name",
                defaults == 0 ? "Required default template is missing; no values were created." : "Default template name is duplicated."));
            if (matches > 1) issues.Add(new PresentationConfigDiagnostic(group, key, "name", "Requested name is duplicated; selection is ambiguous."));
            if (matches == 0 && key != "default") issues.Add(new PresentationConfigDiagnostic(group, key, "name",
                "Template not found; falling back to default when exactly one exists.", false));
            diagnostics = issues.AsReadOnly();
            if (matches == 1) return found;
            return matches == 0 && defaults == 1 ? fallback : null;
        }
    }
}
