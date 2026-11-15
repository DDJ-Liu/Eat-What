using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using EatWhat.Narrative.Core;

internal static class DialoguePresentationResolverTests
{
    static int checks, defaultKeys, overrideKeys, grammarCases;
    static void Check(bool value, string reason) { checks++; if (!value) throw new Exception("Resolver: " + reason); }
    static NarrativeDialogue Dialogue(string raw = "", string text = "", string cg = "", string layout = "")
    {
        return new NarrativeDialogue("dlg_sample", NarrativeDialogueLevel.Story, null,
            new NarrativeProfileNames("", "", "", layout, "", "", text, "", cg), raw, "ln_a", "",
            new NarrativeCommandSource(NarrativeCommandParser.Parse("")));
    }
    static void ReadOnly(IList list)
    {
        Check(list.IsReadOnly, "public collection is read-only"); bool denied = false;
        try { list.Clear(); } catch (NotSupportedException) { denied = true; }
        Check(denied, "public collection refuses mutation");
    }
    static float Number(Dictionary<string, string[]> rows, string path)
    { return float.Parse(rows[path][2], CultureInfo.InvariantCulture); }
    static void Defaults(string fixture)
    {
        var rows = new Dictionary<string, string[]>();
        foreach (var line in File.ReadAllLines(fixture))
            if (line.Length > 0 && line[0] != '#') { var cells = line.Split('\t'); rows.Add(cells[0], cells); }
        var result = DialoguePresentationResolver.Resolve(new DialoguePresentationData(), Dialogue());
        Check(result.Success && result.Snapshot.Groups.Count == 10 && result.Snapshot.Parameters.Count == 69, "ten groups from the single 69-field registry");
        foreach (var entry in result.Snapshot.Parameters)
        {
            defaultKeys++; var d = entry.Definition; string path = d.Group + "[0]." + d.FieldName; var expected = rows[path]; object value = entry.Value;
            DialogueParameterDefinition same; Check(DialogueParameterRegistry.TryGet(d.Key, out same) && ReferenceEquals(d, same), "same registry definition, no duplicate registry");
            if (expected[1] == "Single") Check(value is float && (float)value == Number(rows, path), "independent D5 float " + path);
            else if (expected[1] == "Boolean") Check(value is bool && (bool)value == bool.Parse(expected[2]), "independent D5 bool " + path);
            else if (expected[1] == "String") Check((string)value == expected[2], "independent D5 string " + path);
            else if (expected[1] == "PresentationPoint")
            {
                var point = (DialoguePointSnapshot)value;
                Check(point.X == Number(rows, path + ".x") && point.Y == Number(rows, path + ".y"), "independent frozen point");
            }
            else if (expected[1] == "PresentationColor")
            {
                var color = (DialogueColorSnapshot)value;
                Check(color.R == Number(rows, path + ".r") && color.G == Number(rows, path + ".g") && color.B == Number(rows, path + ".b") && color.A == Number(rows, path + ".a"), "independent frozen color");
            }
            else if (expected[1] == "PortraitStationSet[]")
            {
                var sets = (IReadOnlyList<DialogueStationSetSnapshot>)value; Check(sets.Count == int.Parse(expected[2]), "independent station set count");
                for (int i = 0; i < sets.Count; i++)
                {
                    string setPath = path + "[" + i + "]";
                    Check(sets[i].ActorCount == int.Parse(rows[setPath + ".actorCount"][2]) && sets[i].Entries.Count == int.Parse(rows[setPath + ".entries"][2]), "independent station actors/entries");
                    for (int j = 0; j < sets[i].Entries.Count; j++)
                    {
                        var station = sets[i].Entries[j]; string p = setPath + ".entries[" + j + "]";
                        Check(station.Id == rows[p + ".id"][2] && station.Scale == Number(rows, p + ".scale") && station.Layer == int.Parse(rows[p + ".layer"][2]), "independent station identity/scale/layer");
                        Check(station.Position.X == Number(rows, p + ".position.x") && station.Position.Y == Number(rows, p + ".position.y"), "independent station coordinates");
                    }
                }
            }
            else Check(value.GetType().IsEnum && value.ToString() == expected[2], "independent D5 enum " + path);
        }
        Check(defaultKeys == 69, "all independent default parameters checked");
    }

    static void LayersAndErrors()
    {
        var config = new DialoguePresentationData();
        config.text[0].charsPerSecond = 12; config.text[0].autoWait = 3; config.text[0].punctuationPause = 0.3f;
        var named = new DialogueTextConfig { name = "fast", charsPerSecond = 55, autoWait = 7, punctuationPause = 0.9f };
        config.text = new[] { config.text[0], named };
        config.trigger = new[] { config.trigger[0], new DialogueTriggerConfig { name = "near", interactRadius = 9, autoBubbleCooldown = 55 } };
        var source = Dialogue("text.charsPerSecond=83;trigger.interactRadius=4", "fast");
        var resolved = DialoguePresentationResolver.Resolve(config, source, "near");
        Check(resolved.Success && resolved.Snapshot.Get<float>("text.charsPerSecond") == 83 && resolved.Snapshot.Get<float>("text.autoWait") == 7 && resolved.Snapshot.Get<float>("text.punctuationPause") == 0.9f, "default -> whole named group -> single override");
        Check(resolved.Snapshot.GetGroup("text").TemplateName == "fast" && resolved.Snapshot.GetGroup("trigger").TemplateName == "near" && resolved.Snapshot.Get<float>("trigger.interactRadius") == 4, "caller independently selects trigger before per-parameter override");
        Check(named.charsPerSecond == 55 && config.text[0].charsPerSecond == 12, "resolver does not write templates");
        var next = DialoguePresentationResolver.Resolve(config, Dialogue("", "fast"));
        Check(next.Snapshot.Get<float>("text.charsPerSecond") == 55 && next.Snapshot.Get<float>("trigger.interactRadius") == 2.1f, "override does not cross dialogue or caller trigger contexts");
        Check(DialoguePresentationResolver.Resolve(config, Dialogue()).Snapshot.Get<float>("text.charsPerSecond") == 12, "blank name uses real default");
        var unknown = DialoguePresentationResolver.Resolve(config, Dialogue("", "missing"));
        Check(unknown.Success && unknown.Snapshot.Get<float>("text.charsPerSecond") == 12 && unknown.ConfigurationDiagnostics.Count == 1 && !unknown.ConfigurationDiagnostics[0].IsError && unknown.ConfigurationDiagnostics[0].Group == "text", "unknown named template diagnoses default fallback");
        var trigger = DialoguePresentationResolver.ResolveTrigger(config, "near");
        Check(trigger.Success && trigger.Snapshot.Get<float>("interactRadius") == 9 && trigger.Snapshot.Parameters.Count == 7, "standalone trigger selection");
        var triggerMissing = DialoguePresentationResolver.ResolveTrigger(config, "missing");
        Check(triggerMissing.Success && triggerMissing.Snapshot.Get<float>("interactRadius") == 2.1f && triggerMissing.Diagnostics.Count == 1, "standalone unknown trigger fallback");
        var invalid = new DialoguePresentationData(); invalid.text = new[] { new DialogueTextConfig { name = "named" } };
        Check(!DialoguePresentationResolver.Resolve(invalid, Dialogue("", "named")).Success, "missing default fails even when requested name exists");
        invalid = new DialoguePresentationData(); invalid.text = new[] { new DialogueTextConfig(), new DialogueTextConfig() };
        Check(!DialoguePresentationResolver.Resolve(invalid, Dialogue()).Success, "duplicate defaults not guessed");
        invalid = new DialoguePresentationData(); invalid.text = new[] { new DialogueTextConfig(), new DialogueTextConfig { name = "dup" }, new DialogueTextConfig { name = "dup" } };
        Check(!DialoguePresentationResolver.Resolve(invalid, Dialogue("", "dup")).Success, "ambiguous named config fails by original validation");
        invalid = new DialoguePresentationData(); invalid.cg[0].kenBurnsZoom = float.NaN;
        Check(!DialoguePresentationResolver.Resolve(invalid, Dialogue()).Success && DialoguePresentationResolver.ResolveTrigger(invalid, "").Success, "dialogue validates its full snapshot; standalone trigger has its own consumed group");
        Check(!DialoguePresentationResolver.Resolve(null, Dialogue()).Success && !DialoguePresentationResolver.Resolve(config, null).Success && !DialoguePresentationResolver.ResolveTrigger(null, "").Success, "missing input gives explicit failure, no default table fabricated");
        invalid = new DialoguePresentationData(); invalid.trigger = new DialogueTriggerConfig[0];
        Check(!DialoguePresentationResolver.ResolveTrigger(invalid, "").Success, "standalone trigger missing default fails");
        config.text[0].charsPerSecond = 99; named.autoWait = 123; config.trigger[1].interactRadius = 17;
        Check(resolved.Snapshot.Get<float>("text.charsPerSecond") == 83 && resolved.Snapshot.Get<float>("text.autoWait") == 7 && trigger.Snapshot.Get<float>("interactRadius") == 9, "prior snapshots isolated from later authoring edits");
    }

    static void PartialParsing()
    {
        const string raw = "text.charsPerSecond=64;unknown.key=1;text.autoWait=wrong;cg.kenBurnsPanY=42;text.punctuationSet=\";=，\\\"\\\\\\n\";text.charsPerSecond=65";
        var atomic = DialogueOverrideParser.Parse(raw); var partial = DialogueOverrideParser.ParseItems(raw);
        Check(!atomic.Success && atomic.Values.Count == 0 && !partial.Success && partial.Values.Count == 3 && partial.Diagnostics.Count == 3, "old atomic result unchanged, independent valid items retained");
        var config = new DialoguePresentationData(); var result = DialoguePresentationResolver.ResolveWithOverrides(config, Dialogue(raw), partial);
        Check(result.Success && result.OverrideDiagnostics.Count == 3 && result.Snapshot.OverrideSourceText == raw && result.Snapshot.Get<float>("text.charsPerSecond") == 64 && result.Snapshot.Get<float>("cg.kenBurnsPanY") == 42 && result.Snapshot.Get<float>("text.autoWait") == config.text[0].autoWait, "skip unknown/type/duplicate only; first occurrence wins without overwriting");
        Check(result.Snapshot.Get<string>("text.punctuationSet") == ";=，\"\\\n", "same quoted semicolon/equal/escape grammar");
        var invalidFirst = DialoguePresentationResolver.Resolve(config, Dialogue("text.autoWait=bad;text.autoWait=5;cg.kenBurnsPanY=9"));
        Check(invalidFirst.Success && invalidFirst.OverrideDiagnostics.Count == 2 && invalidFirst.Snapshot.Get<float>("text.autoWait") == config.text[0].autoWait && invalidFirst.Snapshot.Get<float>("cg.kenBurnsPanY") == 9, "invalid first occurrence reserves key; later duplicate cannot hide first error");
        var unclosed = DialoguePresentationResolver.Resolve(config, Dialogue("text.autoWait=4;text.punctuationSet=\"oops;cg.kenBurnsPanX=99"));
        Check(unclosed.Success && unclosed.OverrideDiagnostics.Count == 1 && unclosed.Snapshot.Get<float>("text.autoWait") == 4 && unclosed.Snapshot.Get<float>("cg.kenBurnsPanX") == config.cg[0].kenBurnsPanX, "unclosed quote owns its tail; no guessed recovery split");
        foreach (var text in new[] { null, "", " ", raw, "text.autoWait=2;;cg.kenBurnsPanX=0;", "text.autoWait=2;text.autoWait=3", "text.punctuationSet=\"a;b=c\";text.autoWait=7", "text.punctuationSet=\"a\\q\";text.autoWait=7", "expression.symbolOffset=(1);cg.kenBurnsPanX=0", "unknown=1;text.autoWait=4", "text.punctuationSet=\"bad;cg.kenBurnsPanX=9" })
        {
            grammarCases++; var old = DialogueOverrideParser.Parse(text); var items = DialogueOverrideParser.ParseItems(text);
            Check(old.Success == items.Success && old.Diagnostics.Count == items.Diagnostics.Count, "same scanner success and diagnostics count");
            for (int i = 0; i < old.Diagnostics.Count; i++)
                Check(old.Diagnostics[i].Key == items.Diagnostics[i].Key && old.Diagnostics[i].Problem == items.Diagnostics[i].Problem && old.Diagnostics[i].Position == items.Diagnostics[i].Position && old.Diagnostics[i].Reason == items.Diagnostics[i].Reason, "same key/UTF16 position/problem/reason");
            if (old.Success) Check(old.Values.Count == items.Values.Count, "same complete valid columns");
            else Check(old.Values.Count == 0, "original Parse still atomic on every error");
            foreach (var item in items.Items)
                Check(item.SourceText == (text ?? "").Substring(item.Position, item.SourceText.Length) && item.Success == (item.Value != null && item.Diagnostics.Count == 0), "item source location and status");
        }
    }

    static string Legal(DialogueParameterDefinition d)
    {
        switch (d.Kind)
        {
            case DialogueParameterKind.Number: return "0";
            case DialogueParameterKind.Boolean: return "false";
            case DialogueParameterKind.Enum: return Enum.GetNames(d.ValueType)[0];
            case DialogueParameterKind.String: return "\"override;文字\"";
            case DialogueParameterKind.Point: return "(1,-2)";
            case DialogueParameterKind.Color: return "(0.1,0.2,0.3,0.4)";
            case DialogueParameterKind.Stations: return "[(1,[(\"only\",7,-8,0.8,-2)])]";
            default: throw new Exception("Unexpected kind");
        }
    }
    static void EveryOverrideAndImmutability()
    {
        var config = new DialoguePresentationData();
        foreach (var d in DialogueParameterRegistry.Entries)
        {
            overrideKeys++; string raw = d.Key + "=" + Legal(d); var result = DialoguePresentationResolver.Resolve(config, Dialogue(raw));
            Check(result.Success && result.OverrideDiagnostics.Count == 0, "valid independent override " + d.Key);
            object value = result.Snapshot.Get<object>(d.Key);
            switch (d.Kind)
            {
                case DialogueParameterKind.Number: Check((float)value == 0, "number override"); break;
                case DialogueParameterKind.Boolean: Check(!(bool)value, "false override"); break;
                case DialogueParameterKind.Enum: Check(value.ToString() == Enum.GetNames(d.ValueType)[0], "typed enum override"); break;
                case DialogueParameterKind.String: Check((string)value == "override;文字", "string override"); break;
                case DialogueParameterKind.Point: Check(((DialoguePointSnapshot)value).X == 1 && ((DialoguePointSnapshot)value).Y == -2, "point override frozen"); break;
                case DialogueParameterKind.Color: Check(((DialogueColorSnapshot)value).R == 0.1f && ((DialogueColorSnapshot)value).A == 0.4f, "color override frozen"); break;
                case DialogueParameterKind.Stations: Check(((IReadOnlyList<DialogueStationSetSnapshot>)value)[0].Entries[0].Id == "only", "station override frozen"); break;
            }
        }
        Check(overrideKeys == 69, "every actual registered parameter applies without a second registry");
        const string rawComposite = "layout.stations=[(1,[(\"only\",7,-8,0.8,-2)])];expression.symbolOffset=(11,-12);trigger.markerTalkedTint=(0.1,0.2,0.3,0.4)";
        var parsed = DialogueOverrideParser.ParseItems(rawComposite); var frozen = DialoguePresentationResolver.ResolveWithOverrides(config, Dialogue(rawComposite), parsed).Snapshot;
        var sets = frozen.Get<IReadOnlyList<DialogueStationSetSnapshot>>("layout.stations");
        var sourceSets = (PortraitStationSet[])parsed.Values[0].Value; sourceSets[0].entries[0].id = "changed"; sourceSets[0].entries[0].position.x = 900; sourceSets[0].entries[0].scale = 19; sourceSets[0].entries = new PortraitStation[0]; sourceSets[0] = null;
        typeof(PresentationPoint).GetField("x").SetValue(parsed.Values[1].Value, 999f);
        typeof(PresentationColor).GetField("r").SetValue(parsed.Values[2].Value, 999f);
        Check(sets[0].Entries.Count == 1 && sets[0].Entries[0].Id == "only" && sets[0].Entries[0].Position.X == 7 && sets[0].Entries[0].Scale == 0.8f, "all parsed station nesting isolated");
        Check(frozen.Get<DialoguePointSnapshot>("expression.symbolOffset").X == 11 && frozen.Get<DialogueColorSnapshot>("trigger.markerTalkedTint").R == 0.1f, "parsed boxed point/color edits cannot mutate snapshot");
        var fromConfig = DialoguePresentationResolver.Resolve(config, Dialogue()).Snapshot;
        var originalStation = fromConfig.Get<IReadOnlyList<DialogueStationSetSnapshot>>("layout.stations")[0].Entries[0];
        config.layout[0].stations[0].entries[0].id = "source changed"; config.layout[0].stations[0].entries[0].position.x = 500; config.layout[0].stations[0].entries[0] = null; config.layout[0].stations = null;
        config.expression[0].symbolOffset.x = 999; config.trigger[0].markerTalkedTint.r = 0;
        Check(originalStation.Id == "s1-center" && originalStation.Position.X == 0 && fromConfig.Get<DialoguePointSnapshot>("expression.symbolOffset").X == 173 && fromConfig.Get<DialogueColorSnapshot>("trigger.markerTalkedTint").R == 0.5f, "all authoring composite layers isolated");
        ReadOnly((IList)sets); ReadOnly((IList)sets[0].Entries); ReadOnly((IList)frozen.Groups); ReadOnly((IList)frozen.Parameters); ReadOnly((IList)frozen.Groups[0].Parameters);
        foreach (var type in new[] { typeof(DialoguePresentationSnapshot), typeof(DialogueGroupSnapshot), typeof(ResolvedDialogueParameter), typeof(DialoguePointSnapshot), typeof(DialogueColorSnapshot), typeof(DialogueStationSnapshot), typeof(DialogueStationSetSnapshot), typeof(DialogueCgSnapshot) })
        {
            Check(type.IsSealed && type.GetFields(BindingFlags.Public | BindingFlags.Instance).Length == 0, "sealed snapshot without mutable public fields");
            foreach (var property in type.GetProperties()) Check(property.GetSetMethod() == null, "no snapshot setter " + property.Name);
        }
    }

    static void CgFallback()
    {
        var config = new DialoguePresentationData(); config.cg[0].kenBurnsPanX = 111; config.cg[0].kenBurnsPanY = -222;
        config.cg = new[] { config.cg[0], new CGPresenterConfig { name = "cinema", kenBurnsPanX = 15, kenBurnsPanY = 25, keepBoxDuringCG = true, keepNameDuringCG = false } };
        var dialogue = DialoguePresentationResolver.Resolve(config, Dialogue("cg.kenBurnsPanX=33;cg.kenBurnsPanY=44", cg: "cinema")).Snapshot;
        var empty = new NarrativeCg("cg_empty", "sprite", null, null, null, null, null, null, null, null, null, null, null);
        var resolved = DialoguePresentationResolver.ResolveCg(dialogue, empty);
        Check(resolved.Success && resolved.Snapshot.KenBurnsPanX == 33 && resolved.Snapshot.KenBurnsPanY == 44 && resolved.Snapshot.KeepBox && !resolved.Snapshot.KeepName, "CG empty cells use this dialogue's resolved CG group");
        var xZero = new NarrativeCg("cg_x", "sprite", "cross_fade", 0, null, null, 0, null, 0, "ease_in_out", false, null, false);
        var x = DialoguePresentationResolver.ResolveCg(dialogue, xZero);
        Check(x.Success && x.Snapshot.KenBurnsPanX == 0 && x.Snapshot.KenBurnsPanY == 44 && x.Snapshot.FadeIn == 0 && x.Snapshot.KenBurnsDuration == 0 && !x.Snapshot.KeepBox && !x.Snapshot.KeepName && x.Snapshot.TransitionMode == DialogueCgTransition.CrossFade && x.Snapshot.KenBurnsCurve == NarrativeCurvePreset.EaseInOut, "CG explicit zero/false/enum and independent Y fallback");
        var yZero = new NarrativeCg("cg_y", "sprite", null, null, 0, null, null, 0, null, null, null, false, null);
        var y = DialoguePresentationResolver.ResolveCg(dialogue, yZero);
        Check(y.Success && y.Snapshot.KenBurnsPanX == 33 && y.Snapshot.KenBurnsPanY == 0 && y.Snapshot.FadeOut == 0 && !y.Snapshot.KeepPortraits, "CG independent X fallback when Y explicitly zero");
        config.cg[1].kenBurnsPanY = 999;
        Check(x.Snapshot.KenBurnsPanY == 44 && resolved.Snapshot.KenBurnsPanY == 44 && !empty.KenBurnsPanX.HasValue, "CG resolution neither changes source nullable nor aliases authoring");
        var invalid = new NarrativeCg("cg_bad", "sprite", "unknown", null, null, null, null, null, null, "unknown", null, null, null);
        var denied = DialoguePresentationResolver.ResolveCg(dialogue, invalid);
        Check(!denied.Success && denied.Diagnostics.Count == 2 && !DialoguePresentationResolver.ResolveCg(null, empty).Success, "invalid concrete CG names do not silently default");
    }

    public static void Run(string fixture)
    {
        checks = defaultKeys = overrideKeys = grammarCases = 0;
        Defaults(fixture); LayersAndErrors(); PartialParsing(); EveryOverrideAndImmutability(); CgFallback();
        Console.WriteLine("PASS narrative presentation resolver: " + checks + " checks; defaultKeys=" + defaultKeys + "; overrideKeys=" + overrideKeys + "; grammarCases=" + grammarCases);
    }
}
