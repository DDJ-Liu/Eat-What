using System;
namespace EatWhat.MiniGames
{
    /// <summary>
    /// G84 waiting-task contract; implementations run on the Unity main thread.
    /// Begin requires non-null context/callback, finite DurationSeconds >= 0, and no active run.
    /// Invalid input throws before starting. Each successful run invokes completed exactly once.
    /// Cancel ends the run with no completed callback, and is safe when idle.
    /// Teardown cancels and releases callbacks/resources permanently: no later callback or Begin.
    /// Scheduling/elapsed time belong to the future implementation, not this contract.
    /// </summary>
    public interface IMiniGame
    {
        void Begin(MiniGameContext context, Action<MiniGameResult> completed);
        void Cancel();
        void Teardown();
    }
    public sealed class MiniGameContext
    {
        public string RequestId;
        /// <summary>Seconds; zero completes on the first scheduled update.</summary>
        public float DurationSeconds;
    }
    /// <summary>Success only; cancellation has no result callback.</summary>
    public struct MiniGameResult
    {
        /// <summary>Normalized [0,1]; G84 countdown stub always reports 1.</summary>
        public float Quality;
    }
}
