using UnityEngine;

namespace EatWhat.Cooking.ShortCycle
{
    /// <summary>Replaceable equipment boundary for the CK01 demo.</summary>
    public sealed class KitchenEquipmentManager : MonoBehaviour, IKitchenEquipmentAvailability
    {
        [SerializeField] private bool allEquipmentAvailableForDemo = true;
        private IKitchenEquipmentAvailability replacementProvider;

        public void SetRuntimeProvider(IKitchenEquipmentAvailability provider) { replacementProvider = provider; }

        public bool IsAvailable(string equipmentId, out string reason)
        {
            if (replacementProvider != null) return replacementProvider.IsAvailable(equipmentId, out reason);
            reason = allEquipmentAvailableForDemo ? null : "Equipment configuration is not available in this demo.";
            return allEquipmentAvailableForDemo;
        }
    }
}
