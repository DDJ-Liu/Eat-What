using UnityEngine;

namespace EatWhat.LongLoop
{
    /// <summary>Identifies a scene actor; it never owns movement, appearance or narrative state.</summary>
    [DisallowMultipleComponent]
    public sealed class NarrativeActorMarker : MonoBehaviour
    {
        [SerializeField, Tooltip("Character id, optionally with an instance suffix such as chr_vendor#2.")]
        private string characterId = string.Empty;
        public string CharacterId => characterId;

        public void ConfigureId(string value)
        {
            if (characterId == value) return;
            NarrativeTargetRegistry.Notify(this, false);
            characterId = value;
            if (isActiveAndEnabled) NarrativeTargetRegistry.Notify(this, true);
        }

        private void OnEnable() { NarrativeTargetRegistry.Notify(this, true); }
        private void OnDisable() { NarrativeTargetRegistry.Notify(this, false); }
        private void OnDestroy() { NarrativeTargetRegistry.Notify(this, false); }
    }
}
