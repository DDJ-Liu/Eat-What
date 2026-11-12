using System;
namespace EatWhat.MiniGames
{
    /// <summary>Main-thread mini-game run. Begin snapshots its context and calls completed at most once.</summary>
    public interface IMiniGame
    {
        void Begin(MiniGameContext context, Action<MiniGameResult> completed);
        void Cancel();
        void Teardown();
    }

    public sealed class MiniGameContext
    {
        public string RequestId;
        /// <summary>Seconds. Zero completes on the first scheduled update, never inside Begin.</summary>
        public float DurationSeconds;

        public MiniGameContext Snapshot()
        {
            if (string.IsNullOrWhiteSpace(RequestId))
                throw new ArgumentException("Mini-game RequestId is required.");
            if (float.IsNaN(DurationSeconds) || float.IsInfinity(DurationSeconds) || DurationSeconds < 0f)
                throw new ArgumentOutOfRangeException("DurationSeconds", "Duration must be finite and nonnegative seconds.");
            return new MiniGameContext { RequestId = RequestId, DurationSeconds = DurationSeconds };
        }
    }

    /// <summary>Success only; cancellation has no callback. Quality is normalized [0,1].</summary>
    public struct MiniGameResult
    {
        public float Quality;
    }
}
