using UnityEngine;

namespace EatWhat.LongLoop.Configuration
{
    [CreateAssetMenu(menuName = "Eat What/Street/SceneBoundsConfig")]
    public sealed class SceneBoundsConfig : StreetConfigAsset
    {
        [SerializeField] private bool isSmallRoom = false;
        [SerializeField] private bool groundAlignBottom = true;
        [SerializeField] private bool sideFillEnabled = true;
        [SerializeField] private float sideFillBlur = 24f;
        [SerializeField] private float sideFillDim = 0.30f;
        [SerializeField] private float exitNearDistance = 3.2f;

        public bool IsSmallRoom { get => isSmallRoom; set => SetValue(ref isSmallRoom, value); }
        public bool GroundAlignBottom { get => groundAlignBottom; set => SetValue(ref groundAlignBottom, value); }
        public bool SideFillEnabled { get => sideFillEnabled; set => SetValue(ref sideFillEnabled, value); }
        public float SideFillBlur { get => sideFillBlur; set => SetValue(ref sideFillBlur, value); }
        public float SideFillDim { get => sideFillDim; set => SetValue(ref sideFillDim, value); }
        public float ExitNearDistance { get => exitNearDistance; set => SetValue(ref exitNearDistance, value); }

        protected override string SerializedState()
        {
            return StateKey(isSmallRoom, groundAlignBottom, sideFillEnabled, sideFillBlur, sideFillDim, exitNearDistance);
        }
    }
}
