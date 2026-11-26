using UnityEngine;

namespace EatWhat.Cooking.ShortCycle
{
    [DisallowMultipleComponent]
    public sealed class ShortCycleIconScaleTarget : MonoBehaviour
    {
        [SerializeField] private Transform iconTransform = null;
        [SerializeField] private ShortCycleIconScaleRole role = ShortCycleIconScaleRole.Fridge;

        public ShortCycleIconScaleRole Role
        {
            get { return role; }
        }

        public void ApplyContractScale()
        {
            if (iconTransform == null)
            {
                return;
            }
            var scale = ShortCycleIconScaleContract.GetScale(role);
            iconTransform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
