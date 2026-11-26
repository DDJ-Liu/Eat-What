using UnityEngine;

namespace EatWhat.Cooking.ShortCycle
{
    /// <summary>New namespace-local input gate; it does not alter the frozen MouseManager stack.</summary>
    public sealed class ShortCycleInputLockController : MonoBehaviour
    {
        private readonly ShortCycleInputGate gate = new ShortCycleInputGate();

        public ShortCycleInputGate Gate
        {
            get { return gate; }
        }
    }
}
