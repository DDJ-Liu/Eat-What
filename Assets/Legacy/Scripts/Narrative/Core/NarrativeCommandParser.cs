using System;
using System.Collections.Generic;
using System.Globalization;

namespace EatWhat.Narrative.Core
{
    public enum NarrativeCommandDiagnosticKind
    {
        Syntax, UnknownCommand, ArgumentCount, ArgumentValue, Target
    }

    public sealed class NarrativeCommandDiagnostic
    {
        internal NarrativeCommandDiagnostic(NarrativeCommandDiagnosticKind kind,
            int position, int length, string reason)
        {
            Kind = kind; Position = position; Length = length; Reason = reason;
        }
        public NarrativeCommandDiagnosticKind Kind { get; private set; }
        // Zero-based UTF-16 position in the original input; EOF diagnostics may have length zero.
        public int Position { get; private set; }
        public int Length { get; private set; }
        public string Reason { get; private set; }
    }

    public enum NarrativeCommandTargetKind { Actor, Anchor, Player }

    public sealed class NarrativeCommandTarget
    {
        internal NarrativeCommandTarget(NarrativeCommandTargetKind kind, string raw,
            string identifier, string baseId, string instanceSuffix)
        {
            Kind = kind; RawText = raw; Identifier = identifier;
            BaseId = baseId; InstanceSuffix = instanceSuffix;
        }
        public NarrativeCommandTargetKind Kind { get; private set; }
        public string RawText { get; private set; }
        public string Identifier { get; private set; }
        public string BaseId { get; private set; }
        // Decimal suffix text, without '#'; null for a non-instanced actor/anchor/player.
        public string InstanceSuffix { get; private set; }
    }

    public sealed class NarrativeCommandArgument
    {
        internal NarrativeCommandArgument(string raw, string value, int position,
            NarrativeCommandParameter parameter, object typedValue)
        {
            RawText = raw; Value = value; Position = position;
            Parameter = parameter; TypedValue = typedValue;
        }
        public string RawText { get; private set; }
        public string Value { get; private set; }
        public int Position { get; private set; }
        public NarrativeCommandParameter Parameter { get; private set; }
        public object TypedValue { get; private set; }
    }

    public sealed class NarrativeCommandInvocation
    {
        internal NarrativeCommandInvocation(string name, string raw, int position,
            NarrativeCommandSignature signature, List<NarrativeCommandArgument> arguments)
        {
            Name = name; RawText = raw; Position = position; Signature = signature;
            Arguments = new List<NarrativeCommandArgument>(arguments).AsReadOnly();
        }
        public string Name { get; private set; }
        public string RawText { get; private set; }
        public int Position { get; private set; }
        // Null only for a preserved, syntactically complete unknown invocation.
        public NarrativeCommandSignature Signature { get; private set; }
        public IReadOnlyList<NarrativeCommandArgument> Arguments { get; private set; }
    }

    public sealed class NarrativeCommandParseResult
    {
        internal NarrativeCommandParseResult(string source, List<NarrativeCommandInvocation> commands,
            List<NarrativeCommandDiagnostic> diagnostics)
        {
            SourceText = source;
            Commands = new List<NarrativeCommandInvocation>(commands).AsReadOnly();
            Diagnostics = new List<NarrativeCommandDiagnostic>(diagnostics).AsReadOnly();
        }
        public string SourceText { get; private set; }
        public IReadOnlyList<NarrativeCommandInvocation> Commands { get; private set; }
        public IReadOnlyList<NarrativeCommandDiagnostic> Diagnostics { get; private set; }
        public bool Success { get { return Diagnostics.Count == 0; } }
    }

    internal static class NarrativeCommandIdRules
    {
        internal static bool Check(string value, string prefix, out int bad, out string reason)
        {
            bad = 0; reason = null;
            if (value.Length == 0) { reason = "An ID is required."; return false; }
            if (value.Length > 40) { bad = 40; reason = "The base ID exceeds 40 characters."; return false; }
            if (value[0] < 'a' || value[0] > 'z')
            { reason = "An ID must begin with a lowercase ASCII letter."; return false; }
            for (int i = 1; i < value.Length; i++)
            {
                char c = value[i];
                if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_') continue;
                bad = i; reason = "An ID permits only lowercase ASCII letters, digits and underscore.";
                return false;
            }
            if (prefix != null && !value.StartsWith(prefix, StringComparison.Ordinal))
            { reason = "The ID must use prefix '" + prefix + "'."; return false; }
            return true;
        }

        internal static bool Actor(string value, out NarrativeCommandTarget target,
            out int bad, out string reason)
        {
            target = null;
            int hash = value.IndexOf('#');
            string baseId = hash < 0 ? value : value.Substring(0, hash);
            if (!Check(baseId, "chr_", out bad, out reason)) return false;
            string suffix = null;
            if (hash >= 0)
            {
                suffix = value.Substring(hash + 1);
                bad = hash + 1;
                if (suffix.Length == 0) { reason = "An instance suffix requires decimal digits."; return false; }
                for (int i = 0; i < suffix.Length; i++)
                {
                    if (suffix[i] >= '0' && suffix[i] <= '9') continue;
                    bad += i; reason = "An instance suffix permits only decimal digits."; return false;
                }
                string magnitude = suffix.TrimStart('0');
                if (magnitude.Length == 0 || (magnitude.Length == 1 && magnitude[0] < '2'))
                { reason = "An explicit instance suffix must be at least 2."; return false; }
            }
            target = new NarrativeCommandTarget(NarrativeCommandTargetKind.Actor,
                value, value, baseId, suffix);
            bad = 0; reason = null;
            return true;
        }
    }

    public static class NarrativeCommandTargetParser
    {
        public static bool TryParse(string source, out NarrativeCommandTarget target,
            out NarrativeCommandDiagnostic diagnostic)
        {
            target = null; diagnostic = null;
            string raw = source ?? string.Empty;
            int start = 0;
            while (start < raw.Length && char.IsWhiteSpace(raw[start])) start++;
            string value = raw.Trim();
            if (value == "player")
            {
                target = new NarrativeCommandTarget(NarrativeCommandTargetKind.Player,
                    raw, "player", string.Empty, null);
                return true;
            }
            int prefixLength;
            NarrativeCommandTargetKind kind;
            if (value.StartsWith("actor:", StringComparison.Ordinal))
            { prefixLength = 6; kind = NarrativeCommandTargetKind.Actor; }
            else if (value.StartsWith("anchor:", StringComparison.Ordinal))
            { prefixLength = 7; kind = NarrativeCommandTargetKind.Anchor; }
            else
            {
                diagnostic = new NarrativeCommandDiagnostic(NarrativeCommandDiagnosticKind.Target,
                    start, value.Length, "Expected actor:<character id>, anchor:<scene id>, or player.");
                return false;
            }
            string identifier = value.Substring(prefixLength);
            int bad; string reason;
            NarrativeCommandTarget actor = null;
            bool valid = kind == NarrativeCommandTargetKind.Actor
                ? NarrativeCommandIdRules.Actor(identifier, out actor, out bad, out reason)
                : NarrativeCommandIdRules.Check(identifier, null, out bad, out reason);
            if (!valid)
            {
                int position = start + prefixLength + bad;
                diagnostic = new NarrativeCommandDiagnostic(NarrativeCommandDiagnosticKind.Target,
                    position, position < start + value.Length ? 1 : 0, reason);
                return false;
            }
            if (kind == NarrativeCommandTargetKind.Actor)
            {
                target = new NarrativeCommandTarget(kind, raw, identifier, actor.BaseId, actor.InstanceSuffix);
            }
            else target = new NarrativeCommandTarget(kind, raw, identifier, identifier, null);
            return true;
        }
    }

    /// <summary>
    /// Parses syntax and signature argument semantics. Context level/on_end policy remains metadata
    /// for the later validator/dispatcher. A failed result is diagnostic data, never an execution request.
    /// No string quoting/escaping extension is invented for the schema's comma/semicolon grammar.
    /// </summary>
    public static class NarrativeCommandParser
    {
        private static void White(string text, ref int position)
        {
            while (position < text.Length && char.IsWhiteSpace(text[position])) position++;
        }

        private static bool NameStart(char c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_';
        }

        private static void Syntax(List<NarrativeCommandDiagnostic> diagnostics,
            string text, int position, string reason)
        {
            diagnostics.Add(new NarrativeCommandDiagnostic(NarrativeCommandDiagnosticKind.Syntax,
                position, position < text.Length ? 1 : 0, reason));
        }

        public static NarrativeCommandParseResult Parse(string source)
        {
            string text = source ?? string.Empty;
            var commands = new List<NarrativeCommandInvocation>();
            var diagnostics = new List<NarrativeCommandDiagnostic>();
            int position = 0;
            while (true)
            {
                White(text, ref position);
                if (position == text.Length) break;
                int start = position;
                if (!NameStart(text[position]))
                {
                    Syntax(diagnostics, text, position, "Expected a command name.");
                    break;
                }
                position++;
                while (position < text.Length &&
                    (NameStart(text[position]) || (text[position] >= '0' && text[position] <= '9'))) position++;
                string name = text.Substring(start, position - start);
                White(text, ref position);
                if (position == text.Length || text[position] != '(')
                {
                    Syntax(diagnostics, text, position, "Expected '(' after the command name.");
                    break;
                }
                position++;
                var rawArguments = new List<NarrativeCommandArgument>();
                int peek = position;
                White(text, ref peek);
                bool broken = false;
                if (peek < text.Length && text[peek] == ')') position = peek + 1;
                else
                {
                    while (true)
                    {
                        int rawStart = position;
                        while (position < text.Length && text[position] != ',' && text[position] != ')')
                        {
                            if (text[position] == '(' || text[position] == ';')
                            {
                                Syntax(diagnostics, text, position,
                                    "Expected an argument separator or ')'; nested commands are not supported.");
                                broken = true;
                                break;
                            }
                            position++;
                        }
                        if (broken) break;
                        if (position == text.Length)
                        {
                            Syntax(diagnostics, text, position, "Expected ')' before the end of input.");
                            broken = true;
                            break;
                        }
                        string raw = text.Substring(rawStart, position - rawStart);
                        string value = raw.Trim();
                        if (value.Length == 0)
                        {
                            Syntax(diagnostics, text, position, "An argument cannot be empty.");
                            broken = true;
                            break;
                        }
                        int valueStart = rawStart;
                        White(text, ref valueStart);
                        rawArguments.Add(new NarrativeCommandArgument(raw, value, valueStart, null, null));
                        char separator = text[position++];
                        if (separator == ')') break;
                    }
                }
                if (broken) break;

                NarrativeCommandSignature signature;
                var arguments = new List<NarrativeCommandArgument>();
                if (!NarrativeCommandSignatures.TryGet(name, out signature))
                {
                    diagnostics.Add(new NarrativeCommandDiagnostic(NarrativeCommandDiagnosticKind.UnknownCommand,
                        start, name.Length, "Unknown command '" + name + "'."));
                    arguments.AddRange(rawArguments);
                }
                else
                {
                    if (rawArguments.Count != signature.ArgumentCount)
                        diagnostics.Add(new NarrativeCommandDiagnostic(NarrativeCommandDiagnosticKind.ArgumentCount,
                            start, name.Length, "Command '" + name + "' expects " + signature.ArgumentCount +
                            " argument(s), received " + rawArguments.Count + "."));
                    for (int i = 0; i < rawArguments.Count; i++)
                    {
                        var argument = rawArguments[i];
                        var parameter = i < signature.ArgumentCount ? signature.Parameters[i] : null;
                        object typed = null;
                        NarrativeCommandDiagnostic error;
                        if (parameter != null && !Bind(parameter, argument, out typed, out error))
                            diagnostics.Add(error);
                        arguments.Add(new NarrativeCommandArgument(argument.RawText, argument.Value,
                            argument.Position, parameter, typed));
                    }
                }
                commands.Add(new NarrativeCommandInvocation(name, text.Substring(start, position - start),
                    start, signature, arguments));
                White(text, ref position);
                if (position == text.Length) break;
                if (text[position] != ';')
                {
                    Syntax(diagnostics, text, position, "Expected ';' between commands.");
                    break;
                }
                position++;
                White(text, ref position);
                if (position == text.Length)
                {
                    Syntax(diagnostics, text, position, "Expected a command after ';'.");
                    break;
                }
            }
            return new NarrativeCommandParseResult(text, commands, diagnostics);
        }

        private static bool Bind(NarrativeCommandParameter parameter, NarrativeCommandArgument argument,
            out object typed, out NarrativeCommandDiagnostic error)
        {
            typed = null; error = null;
            string value = argument.Value;
            int bad = 0; string reason = null;
            if (parameter.AllowedValues.Count != 0)
            {
                bool allowed = false;
                foreach (string option in parameter.AllowedValues)
                    if (value == option) { allowed = true; break; }
                if (!allowed) reason = "Expected one of: " + string.Join(", ", parameter.AllowedValues) + ".";
            }
            if (reason == null)
            {
                switch (parameter.Kind)
                {
                    case NarrativeCommandParameterKind.Seconds:
                    case NarrativeCommandParameterKind.CameraSize:
                        double number;
                        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out number)
                            && !double.IsNaN(number) && !double.IsInfinity(number)) typed = number;
                        else reason = "Expected a finite invariant-culture number.";
                        break;
                    case NarrativeCommandParameterKind.Boolean:
                        typed = value == "true";
                        break;
                    case NarrativeCommandParameterKind.Target:
                        NarrativeCommandTarget target;
                        NarrativeCommandDiagnostic targetError;
                        if (NarrativeCommandTargetParser.TryParse(value, out target, out targetError))
                            typed = target;
                        else
                        {
                            error = new NarrativeCommandDiagnostic(NarrativeCommandDiagnosticKind.Target,
                                argument.Position + targetError.Position, targetError.Length,
                                "Parameter '" + parameter.Name + "': " + targetError.Reason);
                            return false;
                        }
                        break;
                    case NarrativeCommandParameterKind.ActorId:
                        NarrativeCommandTarget actor;
                        if (NarrativeCommandIdRules.Actor(value, out actor, out bad, out reason)) typed = actor;
                        break;
                    case NarrativeCommandParameterKind.DialogueId:
                    case NarrativeCommandParameterKind.SceneId:
                    case NarrativeCommandParameterKind.CgId:
                    case NarrativeCommandParameterKind.SceneStateId:
                    case NarrativeCommandParameterKind.SceneObjectId:
                        string prefix = parameter.Kind == NarrativeCommandParameterKind.DialogueId ? "dlg_" :
                            parameter.Kind == NarrativeCommandParameterKind.SceneId ? "scn_" :
                            parameter.Kind == NarrativeCommandParameterKind.CgId ? "cg_" : null;
                        if (NarrativeCommandIdRules.Check(value, prefix, out bad, out reason)) typed = value;
                        break;
                    default:
                        // Flag keys/scalar literals, icon tokens and table-constrained shake values
                        // retain text; this layer invents neither persistence nor scalar conversions.
                        typed = value;
                        break;
                }
            }
            if (reason == null) return true;
            error = new NarrativeCommandDiagnostic(NarrativeCommandDiagnosticKind.ArgumentValue,
                argument.Position + bad, bad < value.Length ? 1 : 0,
                "Parameter '" + parameter.Name + "': " + reason);
            return false;
        }
    }
}
