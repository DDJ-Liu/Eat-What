using UnityEngine;

namespace EatWhat.LongLoop.Configuration
{
    [CreateAssetMenu(menuName = "Eat What/Street/CameraInterestConfig")]
    public sealed class CameraInterestConfig : StreetConfigAsset
    {
        [SerializeField] private float safeZoneWidth = 0.70f;
        public float SafeZoneWidth { get => safeZoneWidth; set => SetValue(ref safeZoneWidth,value); }
        protected override string SerializedState() => StateKey(safeZoneWidth);
    }
}
