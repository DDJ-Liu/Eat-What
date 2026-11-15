using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using EatWhat.Narrative.Core;

internal static class NarrativeCommandTests
{
    private static int checks, grammarCases, targetCases, signatureRows;

    private static void Check(bool value, string label)
    {
        checks++;
        if (!value) throw new Exception(label);
    }

    private static NarrativeCommandParseResult Good(string text, int count)
    {
        grammarCases++;
        var result = NarrativeCommandParser.Parse(text);
        Check(result.Success, "Expected valid command text: " + text +
            (result.Diagnostics.Count == 0 ? "" : " / " + result.Diagnostics[0].Reason));
        Check(result.Commands.Count == count, "Command count: " + text);
        return result;
    }

    private static void Bad(string text, NarrativeCommandDiagnosticKind kind, int position)
    {
        grammarCases++;
        var result = NarrativeCommandParser.Parse(text);
        Check(!result.Success && result.Diagnostics.Count > 0, "Expected diagnostics: " + text);
        Check(result.SourceText == text, "Original failed source retained");
        Check(result.Diagnostics[0].Kind == kind, "Diagnostic kind: " + text);
        Check(result.Diagnostics[0].Position == position, "Diagnostic position: " + text +
            " actual=" + result.Diagnostics[0].Position + " expected=" + position);
        Check(!string.IsNullOrWhiteSpace(result.Diagnostics[0].Reason), "Diagnostic reason");
    }

    private static void BadValue(string text, string token)
    {
        Bad(text, NarrativeCommandDiagnosticKind.ArgumentValue,
            text.IndexOf(token, StringComparison.Ordinal));
    }

    private static string JoinParameters(NarrativeCommandSignature signature, bool kinds, bool options)
    {
        var values = new string[signature.Parameters.Count];
        for (int i = 0; i < values.Length; i++)
        {
            var parameter = signature.Parameters[i];
            if (options) values[i] = string.Join("|", parameter.AllowedValues);
            else values[i] = kinds ? parameter.Kind.ToString() : parameter.Name;
        }
        return string.Join(options ? ";" : ",", values);
    }

    private static void Matrix(string fixture)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (string line in File.ReadAllLines(fixture))
        {
            if (line.Length == 0 || line[0] == '#' || line.StartsWith("command\t", StringComparison.Ordinal)) continue;
            string[] cells = line.Split('\t');
            Check(cells.Length == 11, "Independent fixture has eleven columns");
            Check(names.Add(cells[0]), "Independent command row unique");
            NarrativeCommandSignature signature;
            Check(NarrativeCommandSignatures.TryGet(cells[0], out signature), "Schema command exists: " + cells[0]);
            Check(JoinParameters(signature, false, false) == cells[1], cells[0] + " parameter names");
            Check(JoinParameters(signature, true, false) == cells[2], cells[0] + " parameter semantic types");
            Check(JoinParameters(signature, false, true) == cells[3], cells[0] + " parameter value options");
            Check(signature.ArgumentCount == (cells[1].Length == 0 ? 0 : cells[1].Split(',').Length),
                cells[0] + " exact arity");
            Check(signature.Allows(NarrativeDialogueLevel.Ambient) == (cells[4] == "1"), cells[0] + " ambient");
            Check(signature.Allows(NarrativeDialogueLevel.Normal) == (cells[5] == "1"), cells[0] + " normal");
            Check(signature.Allows(NarrativeDialogueLevel.Story) == (cells[6] == "1"), cells[0] + " story");
            string timing = signature.IsInstantaneous.HasValue
                ? (signature.IsInstantaneous.Value ? "1" : "0") : "reserved";
            Check(timing == cells[7], cells[0] + " instantaneous/reserved");
            Check(signature.Truncation.ToString() == cells[8], cells[0] + " truncation");
            Check(signature.OnlyOnEnd == (cells[9] == "1"), cells[0] + " on_end restriction");
            Check(signature.IsAvailableInVersion == (cells[10] == "1"), cells[0] + " version availability");
            signatureRows++;
        }
        Check(signatureRows == 14 && NarrativeCommandSignatures.All.Count == 14, "Exactly fourteen independent rows");
        NarrativeCommandSignature unknown;
        Check(!NarrativeCommandSignatures.TryGet(null, out unknown), "Null lookup is diagnostic-safe");
        Check(!NarrativeCommandSignatures.TryGet("Wait", out unknown), "Command names are ordinal");
        NarrativeCommandSignatures.TryGet("actor_emote", out unknown);
        Check(!unknown.Allows((NarrativeDialogueLevel)99), "Unknown context cannot enable a command");
        bool immutable = false;
        try { ((IList<NarrativeCommandSignature>)NarrativeCommandSignatures.All).Add(unknown); }
        catch (NotSupportedException) { immutable = true; }
        Check(immutable, "Single signature table cannot be mutated by consumers");
    }

    private static void Grammar()
    {
        Good(null, 0); Good("", 0); Good(" \t\r\n ", 0);
        string[] examples =
        {
            "wait(0.25)", "set_flag(chapter.ready,forty two)",
            "actor_move(chr_cook#2,anchor:door)", "actor_face(chr_cook,player)",
            "object_active(front_gate,false)", "shake(heavy)", "start_dialogue(dlg_intro)",
            "set_scene_state(scn_street,morning)", "scene_switch(scn_market,entry_west)",
            "camera_pan(actor:chr_cook#2,7.5)", "camera_return()", "cg_show(cg_intro)",
            "cg_hide()", "actor_emote(chr_cook,icon_smile)"
        };
        foreach (string example in examples) Good(example, 1);
        string raw = " \t wait ( 1.25 ) ; object_active( gate , true ) ; set_flag(chapter.done, 42 ) ";
        var multi = Good(raw, 3);
        Check(multi.SourceText == raw, "Original source preserved");
        Check(multi.Commands[0].RawText == "wait ( 1.25 )", "Original invocation preserved");
        Check(multi.Commands[0].Arguments[0].RawText == " 1.25 ", "Original argument whitespace preserved");
        Check(multi.Commands[0].Arguments[0].Position == raw.IndexOf("1.25", StringComparison.Ordinal),
            "Argument span is in original source");
        Check((double)multi.Commands[0].Arguments[0].TypedValue == 1.25, "Numeric semantic value");
        Check((bool)multi.Commands[1].Arguments[1].TypedValue, "Explicit boolean semantic value");
        Check(multi.Commands[2].Arguments[1].TypedValue is string &&
            (string)multi.Commands[2].Arguments[1].TypedValue == "42", "Flag value is not coerced to a numeric/bool type");
        var flag = Good("set_flag(flag,true);set_flag(flag,-2.5);set_flag(flag,\"two words\")", 3);
        Check((string)flag.Commands[0].Arguments[1].TypedValue == "true", "Boolean-looking scalar stays literal");
        Check((string)flag.Commands[1].Arguments[1].TypedValue == "-2.5", "Numeric-looking scalar stays literal");
        Check(flag.Commands[2].Arguments[1].Value == "\"two words\"", "No invented quote interpretation");
        Good("wait(-1);camera_pan(player,0)", 2); // The command table supplies no additional numeric range.
        Good("wait(1e-3);shake(light);shake(medium)", 3);
        var culture = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("fr-FR");
            Check((double)Good("wait(1.5)", 1).Commands[0].Arguments[0].TypedValue == 1.5,
                "Numeric parsing is invariant");
        }
        finally { Thread.CurrentThread.CurrentCulture = culture; }

        Bad("wait", NarrativeCommandDiagnosticKind.Syntax, 4);
        Bad("wait 1)", NarrativeCommandDiagnosticKind.Syntax, 5);
        Bad("wait(1", NarrativeCommandDiagnosticKind.Syntax, 6);
        Bad("wait(1))", NarrativeCommandDiagnosticKind.Syntax, 7);
        Bad("wait((1))", NarrativeCommandDiagnosticKind.Syntax, 5);
        Bad("wait(1;shake(light)", NarrativeCommandDiagnosticKind.Syntax, 6);
        Bad("wait(,1)", NarrativeCommandDiagnosticKind.Syntax, 5);
        Bad("wait(1,)", NarrativeCommandDiagnosticKind.Syntax, 7);
        Bad("wait(1);", NarrativeCommandDiagnosticKind.Syntax, 8);
        Bad(";wait(1)", NarrativeCommandDiagnosticKind.Syntax, 0);
        Bad("wait(1);;shake(light)", NarrativeCommandDiagnosticKind.Syntax, 8);
        Bad("wait(1)shake(light)", NarrativeCommandDiagnosticKind.Syntax, 7);
        Bad("missing()", NarrativeCommandDiagnosticKind.UnknownCommand, 0);
        Bad("Wait(1)", NarrativeCommandDiagnosticKind.UnknownCommand, 0);
        Bad("wait()", NarrativeCommandDiagnosticKind.ArgumentCount, 0);
        Bad("camera_return(1)", NarrativeCommandDiagnosticKind.ArgumentCount, 0);
        Bad("set_flag(flag,\"one,two\")", NarrativeCommandDiagnosticKind.ArgumentCount, 0);
        BadValue("wait(NaN)", "NaN"); BadValue("wait(Infinity)", "Infinity");
        BadValue("wait(two)", "two"); BadValue("camera_pan(player,big)", "big");
        BadValue("object_active(gate,TRUE)", "TRUE"); BadValue("shake(strong)", "strong");
        BadValue("start_dialogue(chr_cook)", "chr_cook"); BadValue("cg_show(dlg_intro)", "dlg_intro");
        BadValue("set_scene_state(scn_street,Morning)", "Morning");
        string invalidObject = "object_active(gate#2,true)";
        Bad(invalidObject, NarrativeCommandDiagnosticKind.ArgumentValue, invalidObject.IndexOf('#'));
        string invalidActor = "actor_move(chr_cook#1,player)";
        Bad(invalidActor, NarrativeCommandDiagnosticKind.ArgumentValue, invalidActor.IndexOf('#') + 1);
        string missingAnchor = "camera_pan(anchor:,1)";
        Bad(missingAnchor, NarrativeCommandDiagnosticKind.Target, missingAnchor.IndexOf(','));
        Check(NarrativeCommandParser.Parse("wait(1);shake(").Commands.Count == 1, "Valid prefix retained after syntax error");
        Check(NarrativeCommandParser.Parse("missing( value )").Commands[0].Arguments[0].RawText == " value ",
            "Unknown invocation arguments retained for diagnostics");

        var random = new Random(525);
        const string alphabet = "ab_();,:#12 \t汉";
        for (int i = 0; i < 160; i++)
        {
            var characters = new char[random.Next(0, 45)];
            for (int j = 0; j < characters.Length; j++) characters[j] = alphabet[random.Next(alphabet.Length)];
            string input = new string(characters);
            var result = NarrativeCommandParser.Parse(input);
            grammarCases++;
            foreach (var diagnostic in result.Diagnostics)
                Check(diagnostic.Position >= 0 && diagnostic.Position <= input.Length &&
                    !string.IsNullOrWhiteSpace(diagnostic.Reason), "Malformed input returns bounded positions/reasons");
        }
    }

    private static void Targets()
    {
        string[] valid = { "actor:chr_cook", "actor:chr_cook#2", " actor:chr_cook#02 ",
            "actor:chr_cook#999999999999999999999999999999", "anchor:door_2", "anchor:a", "player", "\tplayer " };
        foreach (string text in valid)
        {
            targetCases++;
            NarrativeCommandTarget target; NarrativeCommandDiagnostic diagnostic;
            Check(NarrativeCommandTargetParser.TryParse(text, out target, out diagnostic) && diagnostic == null,
                "Valid target: " + text);
            Check(target.RawText == text, "Target raw text preserved");
        }
        NarrativeCommandTarget actor; NarrativeCommandDiagnostic error;
        NarrativeCommandTargetParser.TryParse("actor:chr_cook#2", out actor, out error);
        Check(actor.Kind == NarrativeCommandTargetKind.Actor && actor.BaseId == "chr_cook" &&
            actor.InstanceSuffix == "2" && actor.Identifier == "chr_cook#2", "Actor instance identity");
        string maxId = "chr_" + new string('a', 36);
        Good("actor_move(" + maxId + "#2,player)", 1);
        string[] invalid = { null, "", "actor:", "anchor:", "chr_cook", "Actor:chr_cook", "player:one",
            "actor:cook", "actor:chr_cook#0", "actor:chr_cook#1", "actor:chr_cook#", "actor:chr_cook#-2",
            "actor:chr_cook#2#3", "actor:chr_cook name", "anchor:door#2", "anchor:Door", "anchor:2door",
            "anchor:" + new string('a', 41), "actor:" + maxId + "a" };
        foreach (string text in invalid)
        {
            targetCases++;
            NarrativeCommandTarget target; NarrativeCommandDiagnostic diagnostic;
            Check(!NarrativeCommandTargetParser.TryParse(text, out target, out diagnostic) &&
                target == null && diagnostic != null, "Invalid target: " + text);
            Check(diagnostic.Position >= 0 && diagnostic.Position <= (text ?? "").Length &&
                !string.IsNullOrWhiteSpace(diagnostic.Reason), "Target diagnostic position/reason");
        }
        string leading = "  actor:chr_cook#1";
        NarrativeCommandTarget rejected;
        NarrativeCommandTargetParser.TryParse(leading, out rejected, out error);
        Check(error.Position == leading.IndexOf('#') + 1, "Target diagnostic retains outer whitespace offset");
    }

    private static int Main(string[] args)
    {
        if (args.Length != 1) throw new Exception("One permanent independent signature fixture path is required.");
        Matrix(args[0]); Grammar(); Targets();
        Console.WriteLine("PASS narrative command tests: " + checks + " checks; grammarCases=" + grammarCases +
            "; targetCases=" + targetCases + "; schemaRows=" + signatureRows);
        NarrativeAdvanceInputTests.Run();
        NarrativeCurveTests.Run();
        NarrativeCommandSchedulerTests.Run();
        NarrativePresentationConfigTests.Run(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(args[0]), "PresentationDefaults.v2_5.tsv"));
        NarrativeOverrideTests.Run(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(args[0]), "PresentationDefaults.v2_5.tsv"));
        NarrativeDataDirectoryTests.Run();
        DialoguePresentationResolverTests.Run(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(args[0]), "PresentationDefaults.v2_5.tsv"));
        NarrativeDialogueStateMachineTests.Run();
        return 0;
    }
}
