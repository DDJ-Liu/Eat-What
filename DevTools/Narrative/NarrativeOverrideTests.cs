using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using EatWhat.Narrative.Core;

internal static class NarrativeOverrideTests
{
    private static int checks, legalKeys, badCases;
    private static void Check(bool ok, string reason)
    { checks++; if (!ok) throw new Exception("Override: " + reason); }
    private sealed class Expected { public string Type, Value; }

    // Independently transcribed D5 C.1-C.10 units; no production reflection supplies expected units.
    private const string Units = @"tier|normalDim=ratio,bubbleLifetime=s,bubbleFadeInTime=s,bubbleExitRise=px,bubbleExitFade=s,commandExecutionMode=none
trigger|interactRadius=u,autoBubbleRadius=u,autoBubbleCooldown=s,markerBobAmplitude=u,markerBobPeriod=s,markerFadeBackTime=s,markerTalkedTint=RGBA
entrance|order=none,letterboxTime=s,dimTime=s,portraitTime=s,portraitSlide=px,portraitFade=bool,boxMode=none,boxTime=s,boxSlideDistance=px
layout|stations=px/×/layer,portraitBottomAlign=bool
speaker|listenerDim=ratio,speakerScale=×,speakerRise=px,speakerShiftInward=px,transition=none,transitionTime=s
expression|crossFadeTime=s,expressionChangeTemplate=template-key,symbolOffset=px,symbolPopTime=s
text|charsPerSecond=chars/s,punctuationPause=s,punctuationSet=string,clickWhileTyping=none,clickWhileTypingFast=none,autoWait=s,fastForwardMultiplier=×,advanceCooldown=s
reaction|background=none,blurRadius=px,turnMode=none,turnDelay=s,reactionStagger=s,reactionDuration=s
cg|cgTransitionMode=none,cgFadeInTime=s,cgFadeOutTime=s,keepBoxDuringCG=bool,keepPortraitsDuringCG=bool,keepNameDuringCG=bool,kenBurnsZoom=×,kenBurnsPanX=px,kenBurnsPanY=px,kenBurnsDuration=s,kenBurnsCurve=none,cgAdvanceCooldown=s
exit|order=none,letterboxOutTime=s,boxTime=s,portraitTime=s,portraitSlide=px,undimTime=s,cameraReturnTime=s,controlResume=none,continuationMode=none";

    private static string Quote(string s)
    { return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t") + "\""; }

    private static Dictionary<string, Expected> Parity(string fixture)
    {
        var expected = new Dictionary<string, Expected>();
        foreach (var line in File.ReadAllLines(fixture))
        {
            if (line.Length == 0 || line[0] == '#') continue;
            var cells = line.Split('\t'); int split = cells[0].IndexOf("[0].", StringComparison.Ordinal);
            if (split < 0) continue;
            string field = cells[0].Substring(split + 4);
            if (field == "name" || field.IndexOf('.') >= 0 || field.IndexOf('[') >= 0) continue;
            string key = cells[0].Substring(0, split) + "." + field;
            expected.Add(key, new Expected { Type = cells[1], Value = cells[2] });
        }
        var units = new Dictionary<string, string>();
        foreach (var line in Units.Split('\n'))
        {
            var parts = line.Trim().Split('|');
            foreach (var item in parts[1].Split(','))
            { var pair = item.Split('='); units.Add(parts[0] + "." + pair[0], pair[1]); }
        }
        Check(expected.Count == 69 && units.Count == 69, "independent complete69 fields/units");
        Check(DialogueParameterRegistry.Entries.Count == 69, "registry exactly covers the existing parameter set");
        var seen = new Dictionary<string, bool>();
        foreach (var d in DialogueParameterRegistry.Entries)
        {
            Check(!seen.ContainsKey(d.Key), "globally unique qualified keys"); seen.Add(d.Key, true);
            Expected e; Check(expected.TryGetValue(d.Key, out e), "no invented or omitted parameter " + d.Key);
            Check(d.ValueType.Name == e.Type, "exact existing semantic type " + d.Key);
            Check(d.Unit == units[d.Key], "independent D5 unit " + d.Key);
            Check(d.Key == d.Group + "." + d.FieldName, "real group/field identity " + d.Key);
            DialogueParameterDefinition found; Check(DialogueParameterRegistry.TryGet(d.Key, out found) && ReferenceEquals(found, d), "lookup actual registry descriptor");
        }
        DialogueParameterDefinition absent;
        Check(!DialogueParameterRegistry.TryGet(null, out absent) && absent == null, "null lookup");
        Check(!DialogueParameterRegistry.TryGet("order", out absent), "no ambiguous short alias");
        return expected;
    }

    private static void Legal(Dictionary<string, Expected> expected)
    {
        var kinds = new Dictionary<DialogueParameterKind, bool>();
        foreach (var row in expected)
        {
            DialogueParameterDefinition d; DialogueParameterRegistry.TryGet(row.Key, out d);
            string text = row.Value.Value;
            switch (d.Kind)
            {
                case DialogueParameterKind.String: text = Quote(text); break;
                case DialogueParameterKind.Point: text = "(173,-173)"; break;
                case DialogueParameterKind.Color: text = "(0.5,0.5,0.5,1)"; break;
                case DialogueParameterKind.Stations: text = "[(2,[(\"left\",-747,0,1,-3),(\"right\",747,0,0.9,2)])]"; break;
            }
            var result = DialogueOverrideParser.Parse(row.Key + "=" + text);
            Check(result.Success && result.Values.Count == 1 && result.Diagnostics.Count == 0, "legal field " + row.Key);
            var value = result.Values[0]; Check(ReferenceEquals(value.Definition, d) && value.Value.GetType() == d.ValueType, "real typed result " + row.Key);
            if (d.Kind == DialogueParameterKind.Number)
                Check((float)value.Value == float.Parse(row.Value.Value, CultureInfo.InvariantCulture), "numeric literal " + row.Key);
            else if (d.Kind == DialogueParameterKind.Boolean)
                Check((bool)value.Value == (row.Value.Value == "true"), "boolean literal " + row.Key);
            else if (d.Kind == DialogueParameterKind.Enum || d.Kind == DialogueParameterKind.String)
                Check(value.Value.ToString() == row.Value.Value, "enum/string literal " + row.Key);
            else if (d.Kind == DialogueParameterKind.Point)
            { var point = (PresentationPoint)value.Value; Check(point.x == 173 && point.y == -173, "signed point"); }
            else if (d.Kind == DialogueParameterKind.Color)
            { var color = (PresentationColor)value.Value; Check(color.r == 0.5f && color.g == 0.5f && color.b == 0.5f && color.a == 1, "typed RGBA"); }
            else
            {
                var sets = (PortraitStationSet[])value.Value;
                Check(sets.Length == 1 && sets[0].actorCount == 2 && sets[0].entries.Length == 2, "typed station array shape");
                Check(sets[0].entries[0].id == "left" && sets[0].entries[0].position.x == -747 && sets[0].entries[0].layer == -3,
                    "station id/signed position/layer");
                Check(sets[0].entries[1].position.x == 747 && sets[0].entries[1].scale == 0.9f && sets[0].entries[1].layer == 2, "station scale and next entry");
            }
            kinds[d.Kind] = true; legalKeys++;
        }
        Check(kinds.Count == 7, "all seven concrete value kinds execute");
        string content = ";=\"\\\n\r\t";
        var escaped = DialogueOverrideParser.Parse("text.punctuationSet=" + Quote(content) + ";cg.kenBurnsPanX=-53;cg.kenBurnsPanY=14;");
        Check(escaped.Success && escaped.Values.Count == 3 && (string)escaped.Values[0].Value == content, "quoted separators/equals/escapes and trailing semicolon");
        Check((float)escaped.Values[1].Value == -53 && (float)escaped.Values[2].Value == 14, "CG separate signed keys");
        Check(DialogueOverrideParser.Parse("expression.expressionChangeTemplate=").Success, "optional empty template string");
        Check(DialogueOverrideParser.Parse("layout.stations=[]").Success, "existing validator permits empty station presets");
        var array = DialogueOverrideParser.Parse("layout.stations=[(1,[(\"a;b=\\\"c\",-1,2,0,-2)]),(2,[(\"l\",-2,0,1,0),(\"r\",2,0,1,0)])]");
        Check(array.Success && ((PortraitStationSet[])array.Values[0].Value)[0].entries[0].id == "a;b=\"c", "multiple sets and quoted station punctuation");
        string[] aliases = { "linear", "quadratic", "smooth_step", "ease_in_out" };
        NarrativeCurvePreset[] curves = { NarrativeCurvePreset.Linear, NarrativeCurvePreset.Quadratic, NarrativeCurvePreset.SmoothStep, NarrativeCurvePreset.EaseInOut };
        for (int i = 0; i < aliases.Length; i++)
        {
            var result = DialogueOverrideParser.Parse("cg.kenBurnsCurve=" + aliases[i]);
            Check(result.Success && (NarrativeCurvePreset)result.Values[0].Value == curves[i], "Schema curve spelling " + aliases[i]);
        }
        string[] alternatives = { "tier.commandExecutionMode=Sequential", "entrance.order=Parallel", "entrance.boxMode=ScalePop",
            "speaker.transition=Cut", "reaction.background=BlurAndDim", "reaction.turnMode=None", "cg.cgTransitionMode=CrossFade",
            "exit.controlResume=AfterCameraReturned", "exit.continuationMode=Seamless", "text.clickWhileTyping=SkipToNext", "text.clickWhileTypingFast=CompleteLine" };
        foreach (string assignment in alternatives) Check(DialogueOverrideParser.Parse(assignment).Success, "independent declared alternative " + assignment);
        var originalCulture = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("fr-FR");
            var result = DialogueOverrideParser.Parse("text.autoWait=1.25;cg.kenBurnsPanX=-1.5");
            Check(result.Success && (float)result.Values[0].Value == 1.25f && (float)result.Values[1].Value == -1.5f, "invariant under comma-decimal locale");
            Bad("text.autoWait=1,25", "text.autoWait", DialogueOverrideProblem.InvalidValue);
        }
        finally { Thread.CurrentThread.CurrentCulture = originalCulture; }
        var before = new DialoguePresentationData();
        DialogueOverrideParser.Parse("tier.normalDim=0.9;cg.kenBurnsPanX=-20;layout.stations=[]");
        Check(before.tier[0].normalDim == 0.4f && before.cg[0].kenBurnsPanX == 53 && before.layout[0].stations.Length == 4,
            "parser does not receive/write existing authoring data");
    }

    private static void Bad(string source, string key, DialogueOverrideProblem problem)
    {
        var result = DialogueOverrideParser.Parse(source);
        Check(!result.Success && result.Values.Count == 0, "atomic failure, no partial values: " + source);
        bool found = false;
        foreach (var d in result.Diagnostics)
        {
            Check(d.Position >= 0 && d.Position <= source.Length && !string.IsNullOrWhiteSpace(d.Reason), "diagnostic source location/reason");
            if (d.Key == key && d.Problem == problem) found = true;
        }
        Check(found, "diagnostic names exact key/problem " + key); badCases++;
    }
    private static void Errors()
    {
        foreach (var source in new[] { null, "", " \t\r\n" })
        { var r = DialogueOverrideParser.Parse(source); Check(r.Success && r.Values.Count == 0, "empty optional override"); }
        Bad("unknown=3", "unknown", DialogueOverrideProblem.UnknownKey);
        Bad("normalDim=0.2", "normalDim", DialogueOverrideProblem.UnknownKey);
        Bad("text.name=x", "text.name", DialogueOverrideProblem.UnknownKey);
        Bad("Text.autoWait=1", "Text.autoWait", DialogueOverrideProblem.UnknownKey);
        Bad("text.autoWait", "text.autoWait", DialogueOverrideProblem.Syntax);
        Bad("=1", "", DialogueOverrideProblem.Syntax);
        Bad("text.autoWait=1;;cg.kenBurnsPanX=2", "", DialogueOverrideProblem.Syntax);
        Bad("text.autoWait=1; text.autoWait =2", "text.autoWait", DialogueOverrideProblem.DuplicateKey);
        Bad("text.autoWait=bad;text.autoWait=2", "text.autoWait", DialogueOverrideProblem.DuplicateKey);
        foreach (string value in new[] { "", "-1", "NaN", "Infinity", "-Infinity", "1e90", "one", "\"1\"" })
            Bad("text.autoWait=" + value, "text.autoWait", DialogueOverrideProblem.InvalidValue);
        Bad("tier.normalDim=1.01", "tier.normalDim", DialogueOverrideProblem.InvalidValue);
        Bad("entrance.portraitFade=yes", "entrance.portraitFade", DialogueOverrideProblem.InvalidValue);
        foreach (string value in new[] { "999", "0", "linearish" }) Bad("cg.kenBurnsCurve=" + value, "cg.kenBurnsCurve", DialogueOverrideProblem.InvalidValue);
        Bad("text.punctuationSet=\"unfinished", "text.punctuationSet", DialogueOverrideProblem.InvalidValue);
        Bad("text.punctuationSet=\"\\q\"", "text.punctuationSet", DialogueOverrideProblem.InvalidValue);
        foreach (string value in new[] { "1,2", "(1)", "(1,NaN)", "(1,2,3)" })
            Bad("expression.symbolOffset=" + value, "expression.symbolOffset", DialogueOverrideProblem.InvalidValue);
        foreach (string value in new[] { "(0,0,0)", "(2,0,0,1)", "(0,0,NaN,1)" })
            Bad("trigger.markerTalkedTint=" + value, "trigger.markerTalkedTint", DialogueOverrideProblem.InvalidValue);
        foreach (string value in new[] { "null", "[(1,[])]", "[(1,[(\"a\",0,0,-1,0)])]", "[(1,[(\"a\",0,0,1,2147483648)])]",
            "[(2,[(\"a\",0,0,1,0),(\"a\",1,0,1,0)])]", "[(1,[(\"a\",0,0,1,0)]),(1,[(\"b\",0,0,1,0)])]",
            "[(1,[(\"\",0,0,1,0)])]", "[(1,[(\"a\",NaN,0,1,0)])]" })
            Bad("layout.stations=" + value, "layout.stations", DialogueOverrideProblem.InvalidValue);
        var issues = DialogueOverrideParser.Parse("text.autoWait=wrong;unknown=true;cg.kenBurnsPanY=4");
        Check(!issues.Success && issues.Diagnostics.Count == 2 && issues.Values.Count == 0, "all-key diagnostics without partial application");
        Check(DialogueOverrideParser.Parse("entrance.order=Parallel;exit.order=Sequential;entrance.boxTime=1;exit.boxTime=2").Success,
            "same field names in different groups remain independent");
    }
    public static void Run(string fixture)
    {
        var expected = Parity(fixture); Legal(expected); Errors();
        Console.WriteLine("PASS narrative overrides: " + checks + " checks; registryKeys=69; valueKinds=7; legalKeys=" + legalKeys + "; badCases=" + badCases);
    }
}
