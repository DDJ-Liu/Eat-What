using System;

namespace EatWhat.MiniGames
{
    /// <summary>The adapter owns one modal. False or an exception leaves no acquired modal.</summary>
    public interface IMiniGameModal
    {
        bool TryOpen(out string diagnostic);
        void Close();
    }

    /// <summary>Owns one run and its modal. Generation rejects late callbacks from old runs.</summary>
    public sealed class MiniGameHost
    {
        private readonly IMiniGameModal modal;
        private IMiniGame activeGame;
        private Action<MiniGameResult> activeCompletion;
        private string activeRequestId;
        private long generation;
        private bool transitioning;
        private bool opening;
        private bool openingCancelled;
        private bool settlingCompletion;
        private bool settlementCancelled;
        private bool tornDown;

        public MiniGameHost(IMiniGameModal modal)
        { this.modal = modal ?? throw new ArgumentNullException("modal"); }

        public bool IsActive { get { return activeGame != null; } }
        public string ActiveRequestId { get { return activeRequestId; } }
        public bool IsTornDown { get { return tornDown; } }

        public void Start(IMiniGame game, MiniGameContext context, Action<MiniGameResult> completed)
        {
            if (tornDown || transitioning || IsActive)
                throw new InvalidOperationException("Mini-game host is unavailable or already owns a run.");
            if (game == null) throw new ArgumentNullException("game");
            if (context == null) throw new ArgumentNullException("context");
            if (completed == null) throw new ArgumentNullException("completed");
            var snapshot = context.Snapshot();
            var opened = false;
            var run = ++generation;
            transitioning = true;
            opening = true;
            openingCancelled = false;
            try
            {
                string diagnostic;
                opened = modal.TryOpen(out diagnostic);
                if (openingCancelled || tornDown || generation != run)
                {
                    if (tornDown)
                        throw new ObjectDisposedException("MiniGameHost", "Mini-game host was torn down during modal opening.");
                    throw new OperationCanceledException("Mini-game start was cancelled during modal opening.");
                }
                if (!opened)
                    throw new InvalidOperationException("Mini-game modal did not open: " + diagnostic);
                activeGame = game;
                activeCompletion = completed;
                activeRequestId = snapshot.RequestId;
            }
            catch (Exception startError)
            {
                if (opened)
                {
                    try { modal.Close(); }
                    catch (Exception closeError) { throw new AggregateException(startError, closeError); }
                }
                throw;
            }
            finally { opening = false; openingCancelled = false; transitioning = false; }
            try { game.Begin(snapshot, result => Complete(run, result)); }
            catch (Exception beginError)
            {
                if (IsCurrent(run))
                {
                    try { Cancel(); }
                    catch (Exception closeError) { throw new AggregateException(beginError, closeError); }
                }
                throw;
            }
        }

        public void Cancel()
        {
            if (opening)
            {
                if (!openingCancelled) { openingCancelled = true; generation++; }
                return;
            }
            if (settlingCompletion) { settlementCancelled = true; return; }
            if (!IsActive) return;
            var game = activeGame;
            transitioning = true;
            ClearCurrent();
            try { game.Cancel(); }
            finally
            {
                try { modal.Close(); }
                finally { transitioning = false; }
            }
        }

        public void Teardown()
        {
            if (tornDown) return;
            tornDown = true;
            if (opening)
            {
                if (!openingCancelled) { openingCancelled = true; generation++; }
                return;
            }
            if (settlingCompletion) { settlementCancelled = true; return; }
            if (!IsActive) return;
            var game = activeGame;
            transitioning = true;
            ClearCurrent();
            try { game.Teardown(); }
            finally
            {
                try { modal.Close(); }
                finally { transitioning = false; }
            }
        }

        private bool IsCurrent(long run)
        { return IsActive && generation == run; }

        private void ClearCurrent()
        {
            generation++;
            activeGame = null;
            activeCompletion = null;
            activeRequestId = null;
        }

        private void Complete(long run, MiniGameResult result)
        {
            if (!IsCurrent(run)) return;
            var callback = activeCompletion;
            transitioning = true;
            settlingCompletion = true;
            settlementCancelled = false;
            ClearCurrent();
            try { modal.Close(); }
            finally { settlingCompletion = false; transitioning = false; }
            if (settlementCancelled || tornDown) return;
            if (float.IsNaN(result.Quality) || float.IsInfinity(result.Quality) ||
                result.Quality < 0f || result.Quality > 1f)
                throw new ArgumentOutOfRangeException("result", "Mini-game quality must be in [0,1].");
            // State and modal are settled before user code, so a callback can begin a new run.
            callback(result);
        }
    }
}
