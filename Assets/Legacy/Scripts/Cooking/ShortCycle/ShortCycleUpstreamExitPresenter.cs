using UnityEngine;

namespace EatWhat.Cooking.ShortCycle
{
    /// <summary>Observable upstream-exit placeholder for the FreeTry-only ESC route.</summary>
    public sealed class ShortCycleUpstreamExitPresenter : MonoBehaviour, IShortCycleUpstreamExit
    {
        public int ExitCount { get; private set; }
        public ShortCycleTriggerContext LastTriggerContext { get; private set; }

        public void ExitToUpstream(ShortCycleTriggerContext triggerContext)
        {
            ExitCount++;
            LastTriggerContext = triggerContext;
        }
    }
}
