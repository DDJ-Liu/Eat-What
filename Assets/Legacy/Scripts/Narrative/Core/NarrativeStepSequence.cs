using System;
using System.Collections.Generic;

namespace EatWhat.Narrative.Core
{
    public enum NarrativeSequenceMode { Sequential, Parallel }

    /// <summary>Immutable named duration in seconds, independent of its eventual visual effect.</summary>
    public sealed class NarrativeSequenceStep
    {
        public NarrativeSequenceStep(string name, double durationSeconds)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A step needs a name.", nameof(name));
            if (double.IsNaN(durationSeconds) || double.IsInfinity(durationSeconds) || durationSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(durationSeconds));
            Name = name; DurationSeconds = durationSeconds;
        }
        public string Name { get; private set; }
        public double DurationSeconds { get; private set; }
    }

    /// <summary>Owns one run's step progress only; callers supply elapsed seconds and consume progress.</summary>
    public sealed class NarrativeStepSequence
    {
        public sealed class StepState
        {
            internal StepState(NarrativeSequenceStep step) { Name = step.Name; DurationSeconds = step.DurationSeconds; }
            public string Name { get; private set; }
            public double DurationSeconds { get; private set; }
            public double Progress { get; internal set; }
            public bool HasStarted { get; internal set; }
            public bool IsComplete { get { return Progress == 1; } }
            internal double Elapsed;
        }

        List<StepState> states = new List<StepState>();
        int cursor;
        bool started;

        public NarrativeStepSequence() { Steps = states.AsReadOnly(); }
        public IReadOnlyList<StepState> Steps { get; private set; }
        public NarrativeSequenceMode Mode { get; private set; }
        public bool IsComplete { get; private set; } = true;

        // Return true exactly once per run, on completion (including an empty/all-zero Begin).
        // No callbacks: a view must defer forwarding completion beyond its FSM request stack.
        public bool Begin(IEnumerable<NarrativeSequenceStep> steps, NarrativeSequenceMode mode,
            IEnumerable<string> removedStepNames = null)
        {
            if (steps == null) throw new ArgumentNullException(nameof(steps));
            if (mode != NarrativeSequenceMode.Sequential && mode != NarrativeSequenceMode.Parallel)
                throw new ArgumentOutOfRangeException(nameof(mode));
            var removed = new Dictionary<string, bool>(StringComparer.Ordinal);
            if (removedStepNames != null)
                foreach (string name in removedStepNames)
                {
                    if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Removed steps need names.", nameof(removedStepNames));
                    removed[name] = true;
                }
            var names = new Dictionary<string, bool>(StringComparer.Ordinal);
            var next = new List<StepState>();
            foreach (var step in steps)
            {
                if (step == null) throw new ArgumentException("A step cannot be null.", nameof(steps));
                if (names.ContainsKey(step.Name)) throw new ArgumentException("Duplicate step name: " + step.Name, nameof(steps));
                names.Add(step.Name, true);
                if (!removed.ContainsKey(step.Name)) next.Add(new StepState(step));
            }
            // Validate/snapshot before replacing a prior run, including enumerator failures.
            states = next; Steps = states.AsReadOnly(); Mode = mode;
            cursor = 0; started = true; IsComplete = false;
            if (mode == NarrativeSequenceMode.Sequential) StartNext();
            else foreach (var state in states)
            { state.HasStarted = true; if (state.DurationSeconds == 0) state.Progress = 1; }
            return FinishIfComplete();
        }

        public bool Advance(double deltaSeconds)
        {
            if (deltaSeconds <= 0) return false; // Nonpositive input is a harmless no-op, never a rewind.
            if (double.IsNaN(deltaSeconds) || double.IsInfinity(deltaSeconds))
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            if (!started || IsComplete) return false;
            if (Mode == NarrativeSequenceMode.Parallel)
                foreach (var state in states) AdvanceStep(state, deltaSeconds);
            else
            {
                double remaining = deltaSeconds;
                while (cursor < states.Count)
                {
                    var state = states[cursor];
                    double needed = state.DurationSeconds - state.Elapsed;
                    AdvanceStep(state, remaining);
                    if (!state.IsComplete) break;
                    remaining = Math.Max(0, remaining - needed);
                    cursor++; StartNext(); // Start the next step at the boundary; drain zero-duration steps too.
                    if (remaining == 0) break;
                }
            }
            return FinishIfComplete();
        }

        public bool Skip()
        {
            if (!started || IsComplete) return false;
            foreach (var state in states)
            { state.HasStarted = true; state.Elapsed = state.DurationSeconds; state.Progress = 1; }
            cursor = states.Count;
            return FinishIfComplete();
        }

        void StartNext()
        {
            while (cursor < states.Count)
            {
                var state = states[cursor]; state.HasStarted = true;
                if (state.DurationSeconds > 0) break;
                state.Progress = 1; cursor++;
            }
        }

        static void AdvanceStep(StepState state, double seconds)
        {
            if (state.IsComplete) return;
            double needed = state.DurationSeconds - state.Elapsed;
            // Relative tolerance absorbs roundoff from partitioned ticks at a duration boundary only.
            if (seconds >= needed || needed - seconds <= state.DurationSeconds * 1e-12)
            { state.Elapsed = state.DurationSeconds; state.Progress = 1; }
            else { state.Elapsed += seconds; state.Progress = state.Elapsed / state.DurationSeconds; }
        }

        bool FinishIfComplete()
        {
            foreach (var state in states) if (!state.IsComplete) return false;
            IsComplete = true; return true;
        }
    }
}
