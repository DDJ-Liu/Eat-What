using System;
using UnityEngine;

namespace EatWhat.Cooking.ShortCycle
{
    /// <summary>
    /// Observable bridge for the P1 fridge view. C-T2 supplies the visual
    /// compact-reflow listener; the data layer owns only the post-deduction call.
    /// </summary>
    public sealed class ShortCycleInventoryReflowAdapter : MonoBehaviour, IInventoryTightReflow
    {
        public int RequestCount { get; private set; }
        public event Action Requested;

        public void RequestTightReflow()
        {
            RequestCount++;
            var callback = Requested;
            if (callback != null)
            {
                callback();
            }
        }
    }

}
