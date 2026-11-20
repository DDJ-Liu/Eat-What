using System;
using System.Collections.Generic;
using System.Globalization;

namespace EatWhat.Narrative.Core
{
    /// <summary>Owns one line's reveal progress. Time and fast-forward are supplied by the caller.</summary>
    public sealed class NarrativeTypewriterModel
    {
        struct Unit
        {
            public int End;
            public bool Newline, Punctuation;
        }

        readonly List<Unit> units = new List<Unit>();
        int cursor;
        bool started;
        double charsPerSecond, punctuationPause, fastForwardMultiplier, progress;

        public string Text { get; private set; } = string.Empty;
        // Counts Unicode text elements, excluding line breaks. Never split a surrogate pair/combining sequence.
        public int VisibleCharacterCount { get; private set; }
        public int TotalCharacterCount { get; private set; }
        // UTF-16 prefix length, including free line breaks; consumers need not infer it from glyph counts.
        public int VisibleTextLength { get; private set; }
        public string VisibleText { get { return Text.Substring(0, VisibleTextLength); } }
        public bool IsComplete { get; private set; } = true;
        public double PauseRemainingSeconds { get; private set; }

        // Each operation returns true only for this line's transition to complete, including empty Begin.
        // There is no callback into a view/state machine, so callers can defer their own completion report.
        public bool Begin(string finalText, DialoguePresentationSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException("snapshot");
            var text = snapshot.GetGroup("text");
            double speed = text.Get<float>("charsPerSecond"), pause = text.Get<float>("punctuationPause");
            double multiplier = text.Get<float>("fastForwardMultiplier");
            CheckNonnegative(speed, "charsPerSecond"); CheckNonnegative(pause, "punctuationPause");
            CheckNonnegative(multiplier, "fastForwardMultiplier");
            string punctuation = text.Get<string>("punctuationSet") ?? string.Empty;

            Text = finalText ?? string.Empty;
            units.Clear(); TotalCharacterCount = 0;
            var elements = StringInfo.GetTextElementEnumerator(Text);
            while (elements.MoveNext())
            {
                string element = elements.GetTextElement();
                bool newline = IsNewline(element);
                units.Add(new Unit { End = elements.ElementIndex + element.Length, Newline = newline,
                    Punctuation = !newline && punctuation.IndexOf(element, StringComparison.Ordinal) >= 0 });
                if (!newline) TotalCharacterCount++;
            }
            charsPerSecond = speed; punctuationPause = pause; fastForwardMultiplier = multiplier;
            cursor = 0; progress = 0; PauseRemainingSeconds = 0;
            VisibleCharacterCount = VisibleTextLength = 0; IsComplete = false; started = true;
            RevealNewlines();
            return cursor == units.Count && Finish();
        }

        public bool Advance(double deltaSeconds, bool fastForward)
        {
            CheckNonnegative(deltaSeconds, "deltaSeconds");
            if (!started || IsComplete) return false;
            if (fastForward) PauseRemainingSeconds = 0; // Switching on cancels an already pending pause too.
            double remaining = deltaSeconds;
            double rate = charsPerSecond * (fastForward ? fastForwardMultiplier : 1);
            while (cursor < units.Count)
            {
                if (PauseRemainingSeconds > 0)
                {
                    double used = Math.Min(remaining, PauseRemainingSeconds);
                    PauseRemainingSeconds -= used; remaining -= used;
                    if (PauseRemainingSeconds > 0) return false;
                }
                if (rate == 0) return false; // Zero speed remains manually completable; no guessed instant mode.
                double available = progress + remaining * rate;
                // Only absorb floating-point roundoff at an integer character boundary.
                if (available < 1 && 1 - available > 1e-12)
                { progress = available; return false; }
                double needed = (1 - progress) / rate;
                remaining = Math.Max(0, remaining - needed); progress = 0;
                Unit revealed = units[cursor++];
                VisibleTextLength = revealed.End; VisibleCharacterCount++;
                RevealNewlines();
                if (cursor == units.Count) return Finish(); // Terminal punctuation never creates a pause.
                if (!fastForward && revealed.Punctuation && !units[cursor].Punctuation)
                    PauseRemainingSeconds = punctuationPause;
                if (remaining == 0) return false;
            }
            return false;
        }

        public bool Complete()
        {
            if (!started || IsComplete) return false;
            cursor = units.Count; VisibleCharacterCount = TotalCharacterCount; VisibleTextLength = Text.Length;
            return Finish();
        }

        bool Finish()
        { IsComplete = true; PauseRemainingSeconds = 0; progress = 0; return true; }

        void RevealNewlines()
        {
            while (cursor < units.Count && units[cursor].Newline)
                VisibleTextLength = units[cursor++].End;
        }

        static bool IsNewline(string element)
        {
            foreach (char value in element)
                if (value != '\r' && value != '\n' && value != '\u0085' && value != '\u2028' && value != '\u2029') return false;
            return true;
        }

        static void CheckNonnegative(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
                throw new ArgumentOutOfRangeException(name, "Expected finite nonnegative seconds/rate.");
        }
    }
}
