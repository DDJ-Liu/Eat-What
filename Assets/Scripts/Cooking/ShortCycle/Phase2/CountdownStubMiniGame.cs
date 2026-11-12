using System;
using EatWhat.MiniGames;

namespace EatWhat.Cooking.ShortCycle
{
    /// <summary>G84 demo timer. Update is driven explicitly on the Unity main thread.</summary>
    public sealed class CountdownStubMiniGame : IMiniGame
    {
        private Action<MiniGameResult> completed;
        private string requestId;
        private float remainingSeconds;
        private bool active;
        private bool tornDown;

        public bool IsActive { get { return active; } }
        public bool IsTornDown { get { return tornDown; } }
        public string RequestId { get { return requestId; } }
        public float RemainingSeconds { get { return remainingSeconds; } }

        public void Begin(MiniGameContext context, Action<MiniGameResult> callback)
        {
            if (tornDown || active)
                throw new InvalidOperationException("Countdown is torn down or already active.");
            if (context == null) throw new ArgumentNullException("context");
            if (callback == null) throw new ArgumentNullException("callback");
            var snapshot = context.Snapshot();
            requestId = snapshot.RequestId;
            remainingSeconds = snapshot.DurationSeconds;
            completed = callback;
            active = true;
        }

        /// <summary>Seconds since the last main-thread update; zero duration fires here once.</summary>
        public bool Tick(float deltaSeconds)
        {
            if (float.IsNaN(deltaSeconds) || float.IsInfinity(deltaSeconds) || deltaSeconds < 0f)
                throw new ArgumentOutOfRangeException("deltaSeconds", "Delta must be finite and nonnegative seconds.");
            if (!active) return false;
            if (remainingSeconds > deltaSeconds)
            {
                remainingSeconds -= deltaSeconds;
                return false;
            }
            var callback = completed;
            active = false;
            completed = null;
            requestId = null;
            remainingSeconds = 0f;
            callback(new MiniGameResult { Quality = 1f });
            return true;
        }

        public void Cancel()
        {
            active = false;
            completed = null;
            requestId = null;
            remainingSeconds = 0f;
        }

        public void Teardown()
        {
            if (tornDown) return;
            tornDown = true;
            Cancel();
        }
    }
}