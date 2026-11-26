using UnityEngine;

namespace EatWhat.LongLoop.Configuration
{
    [CreateAssetMenu(menuName = "Eat What/Street/DepthScaleConfig")]
    public sealed class DepthScaleConfig : StreetConfigAsset
    {
        [SerializeField] private float depthScaleFactor = 0.03f;
        [SerializeField] private float DEPTH = 0.35f;
        [SerializeField] private float FOCUS = 2.0f;

        public float DepthScaleFactor { get => depthScaleFactor; set => SetValue(ref depthScaleFactor, value); }
        public float Depth { get => DEPTH; set => SetValue(ref DEPTH, value); }
        public float Focus { get => FOCUS; set => SetValue(ref FOCUS, value); }

        protected override string SerializedState()
        {
            return StateKey(depthScaleFactor, DEPTH, FOCUS);
        }
    }
}
