using System;
using System.Collections.Generic;

namespace EatWhat.Narrative.Core
{
    public enum NarrativeCommandExecutionMode { Parallel, Sequential }
    public enum NarrativeCommandExecutionState { Pending, Running, Completed, Truncated, Cancelled, Skipped, Failed }

    // Created already bound to an invocation. Construction must not start an effect.
    // CompleteImmediately and Cancel must also work before Begin, for pending sequential commands.
    public interface INarrativeCommandHandler
    {
        void Begin(double nowSeconds);
        bool IsComplete { get; }
        void CompleteImmediately(NarrativeCommandTruncation truncation, double nowSeconds);
        void Cancel(double nowSeconds);
    }

    public sealed class NarrativeCommandExecution
    {
        internal NarrativeCommandExecution(NarrativeCommandInvocation invocation)
        { Invocation = invocation; State = NarrativeCommandExecutionState.Pending; }
        public NarrativeCommandInvocation Invocation { get; private set; }
        public NarrativeCommandExecutionState State { get; internal set; }
        public double? StartedAtSeconds { get; internal set; }
        public double? CompletedAtSeconds { get; internal set; }
        internal INarrativeCommandHandler Handler;
        internal double WaitUntil;
        internal bool IsWait { get { return Invocation.Signature.Truncation == NarrativeCommandTruncation.StopWaiting; } }
        internal bool Active { get { return State == NarrativeCommandExecutionState.Pending || State == NarrativeCommandExecutionState.Running; } }
    }

    public sealed class NarrativeCommandBatch
    {
        internal readonly List<NarrativeCommandExecution> Entries = new List<NarrativeCommandExecution>();
        internal readonly List<NarrativeCommandDiagnostic> Issues = new List<NarrativeCommandDiagnostic>();
        internal int Next;
        internal NarrativeCommandBatch(NarrativeCommandExecutionMode mode, bool detached)
        { Mode = mode; IsDetached = detached; Commands = Entries.AsReadOnly(); Diagnostics = Issues.AsReadOnly(); }
        public NarrativeCommandExecutionMode Mode { get; private set; }
        public bool IsDetached { get; internal set; }
        public IReadOnlyList<NarrativeCommandExecution> Commands { get; private set; }
        public IReadOnlyList<NarrativeCommandDiagnostic> Diagnostics { get; private set; }
        // Terminal execution state, including explicit skips/failures; not an assertion of effect success.
        public bool IsComplete
        {
            get { foreach (var entry in Entries) if (entry.Active) return false; return true; }
        }
    }

    /// <summary>
    /// Owns command execution batches only. Caller supplies monotonic seconds, starts this at text
    /// start, consumes scene-switch signals, and keeps ticking detached batches after dialogue end.
    /// Handler callbacks must not reenter the scheduler. No engine, device, text or dialogue FSM.
    /// </summary>
    public sealed class NarrativeCommandScheduler
    {
        private readonly Func<NarrativeCommandInvocation, INarrativeCommandHandler> createHandler;
        private readonly List<NarrativeCommandBatch> active = new List<NarrativeCommandBatch>();
        private readonly Queue<NarrativeCommandInvocation> sceneSwitches = new Queue<NarrativeCommandInvocation>();
        private NarrativeCommandBatch foreground;

        public NarrativeCommandScheduler(Func<NarrativeCommandInvocation, INarrativeCommandHandler> createHandler)
        { this.createHandler = createHandler; }
        public int ActiveBatchCount { get { return active.Count; } }

        // Cached N005 columns: preserve invocation/diagnostic objects without parsing SourceText again.
        public NarrativeCommandBatch StartCachedLine(NarrativeCommandSource commands, NarrativeDialogueLevel level,
            double nowSeconds, NarrativeCommandExecutionMode mode = NarrativeCommandExecutionMode.Parallel)
        { return Start(CachedResult(commands), level, nowSeconds, mode, false); }

        public NarrativeCommandBatch StartCachedOnEnd(NarrativeCommandSource commands, NarrativeDialogueLevel level,
            double nowSeconds, NarrativeCommandExecutionMode mode = NarrativeCommandExecutionMode.Parallel)
        { return Start(CachedResult(commands), level, nowSeconds, mode, true); }

        private static NarrativeCommandParseResult CachedResult(NarrativeCommandSource source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return new NarrativeCommandParseResult(source.SourceText,
                new List<NarrativeCommandInvocation>(source.Commands), new List<NarrativeCommandDiagnostic>(source.Diagnostics));
        }

        public NarrativeCommandBatch StartLine(NarrativeCommandParseResult commands, NarrativeDialogueLevel level,
            double nowSeconds, NarrativeCommandExecutionMode mode = NarrativeCommandExecutionMode.Parallel)
        { return Start(commands, level, nowSeconds, mode, false); }

        public NarrativeCommandBatch StartOnEnd(NarrativeCommandParseResult commands, NarrativeDialogueLevel level,
            double nowSeconds, NarrativeCommandExecutionMode mode = NarrativeCommandExecutionMode.Parallel)
        { return Start(commands, level, nowSeconds, mode, true); }

        private NarrativeCommandBatch Start(NarrativeCommandParseResult commands, NarrativeDialogueLevel level,
            double nowSeconds, NarrativeCommandExecutionMode mode, bool onEnd)
        {
            if (commands == null) throw new ArgumentNullException(nameof(commands));
            if (mode != NarrativeCommandExecutionMode.Parallel && mode != NarrativeCommandExecutionMode.Sequential)
                throw new ArgumentOutOfRangeException(nameof(mode));
            CheckTime(nowSeconds);
            if (onEnd) EndDialogue(); else TruncateCurrentLine(nowSeconds);
            var batch = new NarrativeCommandBatch(mode, onEnd);
            foreach (var diagnostic in commands.Diagnostics) batch.Issues.Add(diagnostic);
            foreach (var invocation in commands.Commands)
            {
                var entry = new NarrativeCommandExecution(invocation);
                batch.Entries.Add(entry);
                if (!commands.Success) { entry.State = NarrativeCommandExecutionState.Skipped; continue; }
                var signature = invocation.Signature;
                if (!signature.IsAvailableInVersion || !signature.Allows(level))
                {
                    Skip(batch, entry, "Command is unavailable in this version or dialogue level.");
                    continue;
                }
                // Schema V-16 is a warning for scene_switch outside on_end, not a runtime rejection.
                if (signature.OnlyOnEnd && !onEnd)
                    Diagnostic(batch, entry, "Warning: command should be placed in on_end.");
                if (entry.IsWait) continue;
                try
                {
                    entry.Handler = createHandler == null ? null : createHandler(invocation);
                    if (entry.Handler == null) Skip(batch, entry, "No command handler was supplied.");
                }
                catch (Exception error) { Fail(batch, entry, "factory", error, nowSeconds, false); }
            }
            active.Add(batch);
            if (!onEnd) foreground = batch;
            Pump(batch, nowSeconds);
            Reap();
            return batch;
        }

        public void Tick(double nowSeconds)
        {
            CheckTime(nowSeconds);
            foreach (var batch in active) Pump(batch, nowSeconds);
            Reap();
        }

        // Player advancement may call this before the next text starts. StartLine also calls it.
        public void TruncateCurrentLine(double nowSeconds)
        {
            CheckTime(nowSeconds);
            var batch = foreground;
            foreground = null;
            if (batch != null)
                foreach (var entry in batch.Entries)
                {
                    Probe(batch, entry, nowSeconds);
                    if (entry.Active) Finish(batch, entry, nowSeconds);
                }
            Reap();
        }

        // Release only the line association. All active work remains driven by Tick, including a
        // not-yet-started sequential tail. StartOnEnd/new conversations do not truncate this work.
        public void EndDialogue()
        {
            if (foreground != null) foreground.IsDetached = true;
            foreground = null;
        }

        public void CancelCameraCommands(double nowSeconds)
        {
            CheckTime(nowSeconds);
            foreach (var batch in active)
            {
                foreach (var entry in batch.Entries)
                    if (entry.Active && entry.Invocation.Signature.Truncation == NarrativeCommandTruncation.SnapCamera)
                    {
                        Probe(batch, entry, nowSeconds);
                        if (!entry.Active) continue;
                        try
                        {
                            entry.Handler.Cancel(nowSeconds);
                            entry.State = NarrativeCommandExecutionState.Cancelled;
                            entry.CompletedAtSeconds = nowSeconds;
                        }
                        catch (Exception error) { Fail(batch, entry, "Cancel", error, nowSeconds, false); }
                    }
                Pump(batch, nowSeconds);
            }
            Reap();
        }

        // A command-specific signal, polled by the caller after an operation; no callbacks can
        // reenter list iteration and no dialogue state is advanced here.
        public bool TryTakeSceneSwitch(out NarrativeCommandInvocation invocation)
        {
            invocation = null;
            if (sceneSwitches.Count == 0) return false;
            invocation = sceneSwitches.Dequeue();
            return true;
        }

        private void Pump(NarrativeCommandBatch batch, double nowSeconds)
        {
            if (batch.Mode == NarrativeCommandExecutionMode.Parallel)
            {
                foreach (var entry in batch.Entries)
                    if (entry.State == NarrativeCommandExecutionState.Pending) Begin(batch, entry, nowSeconds);
                foreach (var entry in batch.Entries) Probe(batch, entry, nowSeconds);
            }
            else
                while (batch.Next < batch.Entries.Count)
                {
                    var entry = batch.Entries[batch.Next];
                    if (entry.State == NarrativeCommandExecutionState.Pending) Begin(batch, entry, nowSeconds);
                    Probe(batch, entry, nowSeconds);
                    if (entry.Active) break;
                    batch.Next++;
                }
        }

        private void Begin(NarrativeCommandBatch batch, NarrativeCommandExecution entry, double nowSeconds)
        {
            entry.State = NarrativeCommandExecutionState.Running;
            entry.StartedAtSeconds = nowSeconds;
            if (entry.IsWait)
                entry.WaitUntil = nowSeconds + (double)entry.Invocation.Arguments[0].TypedValue;
            else
                try { entry.Handler.Begin(nowSeconds); }
                catch (Exception error) { Fail(batch, entry, "Begin", error, nowSeconds, true); }
        }

        private void Probe(NarrativeCommandBatch batch, NarrativeCommandExecution entry, double nowSeconds)
        {
            if (entry.State != NarrativeCommandExecutionState.Running) return;
            try
            {
                bool done = entry.IsWait ? nowSeconds >= entry.WaitUntil : entry.Handler.IsComplete;
                if (done)
                {
                    entry.State = NarrativeCommandExecutionState.Completed;
                    entry.CompletedAtSeconds = nowSeconds;
                    SignalSceneSwitch(entry);
                }
            }
            catch (Exception error) { Fail(batch, entry, "IsComplete", error, nowSeconds, true); }
        }

        private void Finish(NarrativeCommandBatch batch, NarrativeCommandExecution entry, double nowSeconds)
        {
            try
            {
                if (!entry.IsWait)
                    entry.Handler.CompleteImmediately(entry.Invocation.Signature.Truncation, nowSeconds);
                entry.State = NarrativeCommandExecutionState.Truncated;
                entry.CompletedAtSeconds = nowSeconds;
                SignalSceneSwitch(entry);
            }
            catch (Exception error) { Fail(batch, entry, "CompleteImmediately", error, nowSeconds, true); }
        }

        private void SignalSceneSwitch(NarrativeCommandExecution entry)
        {
            if (entry.Invocation.Name == "scene_switch") sceneSwitches.Enqueue(entry.Invocation);
        }
        private static void Skip(NarrativeCommandBatch batch, NarrativeCommandExecution entry, string reason)
        {
            entry.State = NarrativeCommandExecutionState.Skipped;
            Diagnostic(batch, entry, reason);
        }
        private static void Diagnostic(NarrativeCommandBatch batch, NarrativeCommandExecution entry, string reason)
        {
            batch.Issues.Add(new NarrativeCommandDiagnostic(NarrativeCommandDiagnosticKind.ArgumentValue,
                entry.Invocation.Position, entry.Invocation.Name.Length, entry.Invocation.Name + ": " + reason));
        }
        private static void Fail(NarrativeCommandBatch batch, NarrativeCommandExecution entry, string callback,
            Exception error, double nowSeconds, bool cancel)
        {
            entry.State = NarrativeCommandExecutionState.Failed;
            entry.CompletedAtSeconds = nowSeconds;
            Diagnostic(batch, entry, "Handler " + callback + " failed: " + error.GetType().Name + ": " + error.Message);
            if (cancel && entry.Handler != null)
                try { entry.Handler.Cancel(nowSeconds); }
                catch (Exception cancelError) { Diagnostic(batch, entry, "Handler cleanup Cancel failed: " + cancelError.Message); }
        }
        private void Reap()
        {
            active.RemoveAll(batch => batch.IsComplete);
            if (foreground != null && foreground.IsComplete) foreground = null;
        }
        private static void CheckTime(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentOutOfRangeException(nameof(value), "Expected finite monotonic seconds from the caller.");
        }
    }
}
