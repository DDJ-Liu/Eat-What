using UnityEngine;

namespace EatWhat.LongLoop.Configuration
{
    [CreateAssetMenu(menuName = "Eat What/Street/CameraModeConfig")]
    public sealed class CameraModeConfig : StreetConfigAsset
    {
        [SerializeField] private float modeBlendTimeToCinematic = 0.4f;
        [SerializeField] private float modeBlendTimeToCloseUp = 0.35f;
        [SerializeField] private float modeBlendTimeToTransition = 0f;
        [SerializeField] private bool gateOffsetsInDialogue = true;

        public float ModeBlendTimeToCinematic { get => modeBlendTimeToCinematic; set => SetValue(ref modeBlendTimeToCinematic, value); }
        public float ModeBlendTimeToCloseUp { get => modeBlendTimeToCloseUp; set => SetValue(ref modeBlendTimeToCloseUp, value); }
        public float ModeBlendTimeToTransition { get => modeBlendTimeToTransition; set => SetValue(ref modeBlendTimeToTransition, value); }
        public bool GateOffsetsInDialogue { get => gateOffsetsInDialogue; set => SetValue(ref gateOffsetsInDialogue, value); }

        protected override string SerializedState()
        {
            return StateKey(modeBlendTimeToCinematic, modeBlendTimeToCloseUp, modeBlendTimeToTransition, gateOffsetsInDialogue);
        }
    }
}
