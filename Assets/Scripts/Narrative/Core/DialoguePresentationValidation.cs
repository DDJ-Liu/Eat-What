using System;
using System.Collections.Generic;

namespace EatWhat.Narrative.Core
{
    /// <summary>Read-only validation of this configuration; never clamps or repairs authoring data.</summary>
    public static class DialoguePresentationValidation
    {
        public static IReadOnlyList<PresentationConfigDiagnostic> Validate(DialoguePresentationData data)
        {
            var issues = new List<PresentationConfigDiagnostic>();
            if (data == null)
            {
                issues.Add(new PresentationConfigDiagnostic("config", "", "data", "Configuration data is missing."));
                return issues.AsReadOnly();
            }
            Group("tier", data.tier, issues, (v, f) => {
                f.Ratio("normalDim", v.normalDim); f.Nonnegative("bubbleLifetime", v.bubbleLifetime);
                f.Nonnegative("bubbleFadeInTime", v.bubbleFadeInTime); f.Nonnegative("bubbleExitRise", v.bubbleExitRise);
                f.Nonnegative("bubbleExitFade", v.bubbleExitFade); f.Enum("commandExecutionMode", v.commandExecutionMode);
            });
            Group("trigger", data.trigger, issues, (v, f) => {
                f.Nonnegative("interactRadius", v.interactRadius); f.Nonnegative("autoBubbleRadius", v.autoBubbleRadius);
                f.Nonnegative("autoBubbleCooldown", v.autoBubbleCooldown); f.Nonnegative("markerBobAmplitude", v.markerBobAmplitude);
                f.Nonnegative("markerBobPeriod", v.markerBobPeriod); f.Nonnegative("markerFadeBackTime", v.markerFadeBackTime);
                f.Ratio("markerTalkedTint.r", v.markerTalkedTint.r); f.Ratio("markerTalkedTint.g", v.markerTalkedTint.g);
                f.Ratio("markerTalkedTint.b", v.markerTalkedTint.b); f.Ratio("markerTalkedTint.a", v.markerTalkedTint.a);
            });
            Group("entrance", data.entrance, issues, (v, f) => {
                f.Enum("order", v.order); f.Nonnegative("letterboxTime", v.letterboxTime);
                f.Nonnegative("dimTime", v.dimTime); f.Nonnegative("portraitTime", v.portraitTime);
                f.Nonnegative("portraitSlide", v.portraitSlide); f.Enum("boxMode", v.boxMode);
                f.Nonnegative("boxTime", v.boxTime); f.Nonnegative("boxSlideDistance", v.boxSlideDistance);
            });
            Group("layout", data.layout, issues, (v, f) => ValidateStations(v, f));
            Group("speaker", data.speaker, issues, (v, f) => {
                f.Ratio("listenerDim", v.listenerDim); f.Nonnegative("speakerScale", v.speakerScale);
                f.Nonnegative("speakerRise", v.speakerRise); f.Nonnegative("speakerShiftInward", v.speakerShiftInward);
                f.Enum("transition", v.transition); f.Nonnegative("transitionTime", v.transitionTime);
            });
            Group("expression", data.expression, issues, (v, f) => {
                f.Nonnegative("crossFadeTime", v.crossFadeTime); f.Finite("symbolOffset.x", v.symbolOffset.x);
                f.Finite("symbolOffset.y", v.symbolOffset.y); f.Nonnegative("symbolPopTime", v.symbolPopTime);
            });
            Group("text", data.text, issues, (v, f) => {
                f.Nonnegative("charsPerSecond", v.charsPerSecond); f.Nonnegative("punctuationPause", v.punctuationPause);
                f.Enum("clickWhileTyping", v.clickWhileTyping); f.Enum("clickWhileTypingFast", v.clickWhileTypingFast);
                f.Nonnegative("autoWait", v.autoWait); f.Nonnegative("fastForwardMultiplier", v.fastForwardMultiplier);
                f.Nonnegative("advanceCooldown", v.advanceCooldown);
            });
            Group("reaction", data.reaction, issues, (v, f) => {
                f.Enum("background", v.background); f.Nonnegative("blurRadius", v.blurRadius);
                f.Enum("turnMode", v.turnMode); f.Nonnegative("turnDelay", v.turnDelay);
                f.Nonnegative("reactionStagger", v.reactionStagger); f.Nonnegative("reactionDuration", v.reactionDuration);
            });
            Group("cg", data.cg, issues, (v, f) => {
                f.Enum("cgTransitionMode", v.cgTransitionMode); f.Nonnegative("cgFadeInTime", v.cgFadeInTime);
                f.Nonnegative("cgFadeOutTime", v.cgFadeOutTime); f.Nonnegative("kenBurnsZoom", v.kenBurnsZoom);
                f.Finite("kenBurnsPanX", v.kenBurnsPanX); f.Finite("kenBurnsPanY", v.kenBurnsPanY);
                f.Nonnegative("kenBurnsDuration", v.kenBurnsDuration); f.Enum("kenBurnsCurve", v.kenBurnsCurve);
                f.Nonnegative("cgAdvanceCooldown", v.cgAdvanceCooldown);
            });
            Group("exit", data.exit, issues, (v, f) => {
                f.Enum("order", v.order); f.Nonnegative("letterboxOutTime", v.letterboxOutTime);
                f.Nonnegative("boxTime", v.boxTime); f.Nonnegative("portraitTime", v.portraitTime);
                f.Nonnegative("portraitSlide", v.portraitSlide); f.Nonnegative("undimTime", v.undimTime);
                f.Nonnegative("cameraReturnTime", v.cameraReturnTime); f.Enum("controlResume", v.controlResume);
                f.Enum("continuationMode", v.continuationMode);
            });
            return issues.AsReadOnly();
        }

        private static void Group<T>(string group, T[] values, List<PresentationConfigDiagnostic> issues,
            Action<T, Fields> validate) where T : DialogueNamedTemplate
        {
            if (values == null)
            {
                issues.Add(new PresentationConfigDiagnostic(group, "default", "templates", "Template collection is missing."));
                return;
            }
            int defaults = 0;
            var names = new List<string>();
            for (int i = 0; i < values.Length; i++)
            {
                var v = values[i];
                if (v == null)
                { issues.Add(new PresentationConfigDiagnostic(group, "[" + i + "]", "template", "Template is null.")); continue; }
                var f = new Fields(group, v.name, issues);
                if (string.IsNullOrWhiteSpace(v.name)) f.Error("name", "Template name must not be empty or whitespace.");
                if (names.Contains(v.name ?? "")) f.Error("name", "Template name is duplicated (ordinal comparison).");
                names.Add(v.name ?? "");
                if (v.name == "default") defaults++;
                validate(v, f);
            }
            if (defaults != 1) issues.Add(new PresentationConfigDiagnostic(group, "default", "name",
                "Exactly one default template is required; found " + defaults + "."));
        }

        private static void ValidateStations(PortraitLayoutConfig v, Fields f)
        {
            if (v.stations == null) { f.Error("stations", "Station sets are missing."); return; }
            var counts = new List<int>();
            for (int i = 0; i < v.stations.Length; i++)
            {
                string path = "stations[" + i + "]";
                var set = v.stations[i];
                if (set == null) { f.Error(path, "Station set is null."); continue; }
                if (set.actorCount <= 0) f.Error(path + ".actorCount", "Actor count must be positive.");
                if (counts.Contains(set.actorCount)) f.Error(path + ".actorCount", "Actor count is duplicated.");
                counts.Add(set.actorCount);
                if (set.entries == null) { f.Error(path + ".entries", "Station entries are missing."); continue; }
                if (set.entries.Length != set.actorCount) f.Error(path + ".entries", "Entry count must equal actorCount.");
                var ids = new List<string>();
                for (int j = 0; j < set.entries.Length; j++)
                {
                    string entry = path + ".entries[" + j + "]";
                    var station = set.entries[j];
                    if (station == null) { f.Error(entry, "Station is null."); continue; }
                    if (string.IsNullOrWhiteSpace(station.id)) f.Error(entry + ".id", "Station id must not be empty or whitespace.");
                    if (ids.Contains(station.id ?? "")) f.Error(entry + ".id", "Station id is duplicated within this set.");
                    ids.Add(station.id ?? "");
                    f.Finite(entry + ".position.x", station.position.x); f.Finite(entry + ".position.y", station.position.y);
                    f.Nonnegative(entry + ".scale", station.scale);
                    // Layer and position coordinates are signed; never impose a duration/distance bound on them.
                }
            }
        }

        private sealed class Fields
        {
            private readonly string group, template;
            private readonly List<PresentationConfigDiagnostic> issues;
            public Fields(string group, string template, List<PresentationConfigDiagnostic> issues)
            { this.group = group; this.template = template; this.issues = issues; }
            public void Error(string field, string reason)
            { issues.Add(new PresentationConfigDiagnostic(group, template, field, reason)); }
            public void Finite(string field, float value)
            { if (float.IsNaN(value) || float.IsInfinity(value)) Error(field, "Value must be finite."); }
            public void Nonnegative(string field, float value)
            {
                if (float.IsNaN(value) || float.IsInfinity(value) || value < 0)
                    Error(field, "Value must be finite and nonnegative.");
            }
            public void Ratio(string field, float value)
            {
                if (float.IsNaN(value) || float.IsInfinity(value) || value < 0 || value > 1)
                    Error(field, "Value must be finite and within [0, 1].");
            }
            public void Enum<T>(string field, T value) where T : struct
            { if (!System.Enum.IsDefined(typeof(T), value)) Error(field, "Enum value is undefined."); }
        }
    }
}
