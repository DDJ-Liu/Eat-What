using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Formats fridge ingredient labels without counting TMP rich-text tags as visible characters.
/// The formatter deliberately owns at most one line break so the caller can keep labels to two lines.
/// </summary>
public static class FridgeIngredientLabelTextFormatter
{
    public const int DefaultSingleLineCharacterLimit = 5;

    /// <summary>
    /// Returns a display string with no more than two lines.
    /// Text with up to <paramref name="singleLineVisibleCharacterLimit"/> visible characters stays on one line.
    /// Longer text is split as evenly as possible. The first explicit newline is retained and later newlines are
    /// removed, which makes already-formatted text idempotent and prevents newline accumulation.
    /// </summary>
    public static string Format(string sourceText, int singleLineVisibleCharacterLimit = DefaultSingleLineCharacterLimit)
    {
        if (string.IsNullOrEmpty(sourceText))
        {
            return string.Empty;
        }

        int singleLineLimit = Math.Max(1, singleLineVisibleCharacterLimit);
        List<Token> tokens = Tokenize(sourceText);
        int explicitLineBreakIndex = FindFirstExplicitLineBreak(tokens);

        if (explicitLineBreakIndex >= 0)
        {
            return BuildWithFirstExplicitLineBreak(tokens);
        }

        int visibleCharacterCount = CountVisibleCharacters(tokens);
        if (visibleCharacterCount <= singleLineLimit)
        {
            return BuildWithoutLineBreak(tokens);
        }

        int firstLineCharacterCount = (visibleCharacterCount + 1) / 2;
        return BuildWithAutomaticLineBreak(tokens, firstLineCharacterCount);
    }

    /// <summary>
    /// Counts visible Unicode scalar values. TMP rich-text tags and line-break characters are excluded.
    /// </summary>
    public static int CountVisibleCharacters(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        return CountVisibleCharacters(Tokenize(text));
    }

    private static List<Token> Tokenize(string text)
    {
        var tokens = new List<Token>(text.Length);

        for (int index = 0; index < text.Length;)
        {
            if (text[index] == '<')
            {
                int tagEnd = FindRichTextTagEnd(text, index);
                if (tagEnd >= 0)
                {
                    tokens.Add(new Token(TokenKind.RichTextTag, text.Substring(index, tagEnd - index + 1)));
                    index = tagEnd + 1;
                    continue;
                }
            }

            if (text[index] == '\r' || text[index] == '\n')
            {
                if (text[index] == '\r' && index + 1 < text.Length && text[index + 1] == '\n')
                {
                    index++;
                }

                tokens.Add(new Token(TokenKind.LineBreak, "\n"));
                index++;
                continue;
            }

            int elementLength = GetUnicodeScalarLength(text, index);
            tokens.Add(new Token(TokenKind.VisibleCharacter, text.Substring(index, elementLength)));
            index += elementLength;
        }

        return tokens;
    }

    private static int FindRichTextTagEnd(string text, int tagStart)
    {
        for (int index = tagStart + 1; index < text.Length; index++)
        {
            if (text[index] == '>')
            {
                return index;
            }
        }

        return -1;
    }

    private static int GetUnicodeScalarLength(string text, int index)
    {
        return char.IsHighSurrogate(text[index])
               && index + 1 < text.Length
               && char.IsLowSurrogate(text[index + 1])
            ? 2
            : 1;
    }

    private static int FindFirstExplicitLineBreak(List<Token> tokens)
    {
        for (int index = 0; index < tokens.Count; index++)
        {
            if (tokens[index].Kind == TokenKind.LineBreak)
            {
                return index;
            }
        }

        return -1;
    }

    private static int CountVisibleCharacters(List<Token> tokens)
    {
        int count = 0;
        for (int index = 0; index < tokens.Count; index++)
        {
            if (tokens[index].Kind == TokenKind.VisibleCharacter)
            {
                count++;
            }
        }

        return count;
    }

    private static string BuildWithoutLineBreak(List<Token> tokens)
    {
        var builder = new StringBuilder();
        for (int index = 0; index < tokens.Count; index++)
        {
            if (tokens[index].Kind != TokenKind.LineBreak)
            {
                builder.Append(tokens[index].Value);
            }
        }

        return builder.ToString();
    }

    private static string BuildWithFirstExplicitLineBreak(List<Token> tokens)
    {
        var builder = new StringBuilder();
        bool lineBreakWritten = false;

        for (int index = 0; index < tokens.Count; index++)
        {
            Token token = tokens[index];
            if (token.Kind != TokenKind.LineBreak)
            {
                builder.Append(token.Value);
                continue;
            }

            if (!lineBreakWritten)
            {
                builder.Append('\n');
                lineBreakWritten = true;
            }
        }

        return builder.ToString();
    }

    private static string BuildWithAutomaticLineBreak(List<Token> tokens, int firstLineCharacterCount)
    {
        var builder = new StringBuilder();
        int visibleCharactersWritten = 0;

        for (int index = 0; index < tokens.Count; index++)
        {
            Token token = tokens[index];
            if (token.Kind == TokenKind.LineBreak)
            {
                continue;
            }

            builder.Append(token.Value);
            if (token.Kind == TokenKind.VisibleCharacter)
            {
                visibleCharactersWritten++;
                if (visibleCharactersWritten == firstLineCharacterCount)
                {
                    builder.Append('\n');
                }
            }
        }

        return builder.ToString();
    }

    private enum TokenKind
    {
        VisibleCharacter,
        RichTextTag,
        LineBreak,
    }

    private struct Token
    {
        public readonly TokenKind Kind;
        public readonly string Value;

        public Token(TokenKind kind, string value)
        {
            Kind = kind;
            Value = value;
        }
    }
}
