using UnityEngine;

namespace EatWhat.LongLoop
{
    /// <summary>Identifies an authored world-space anchor in this scene.</summary>
    [DisallowMultipleComponent]
    public sealed class NarrativeAnchorMarker : MonoBehaviour
    {
        [SerializeField, Tooltip("Anchor id, without the anchor: prefix.")]
        private string anchorId = string.Empty;
        public string AnchorId => anchorId;

        public void ConfigureId(string value)
        {
            if (anchorId == value) return;
            NarrativeTargetRegistry.Notify(this, false);
            anchorId = value;
            if (isActiveAndEnabled) NarrativeTargetRegistry.Notify(this, true);
        }

        private void OnEnable() { NarrativeTargetRegistry.Notify(this, true); }
        private void OnDisable() { NarrativeTargetRegistry.Notify(this, false); }
        private void OnDestroy() { NarrativeTargetRegistry.Notify(this, false); }
    }
}
