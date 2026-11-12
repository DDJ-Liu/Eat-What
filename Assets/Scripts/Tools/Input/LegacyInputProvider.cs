using UnityEngine;

namespace EatWhat.Toolkit.Input
{
    /// <summary>Legacy Input backend. This is the only new code that reads UnityEngine.Input.</summary>
    [DisallowMultipleComponent]
    public sealed class LegacyInputProvider : MonoBehaviour, IGameInput, INavigationInput
    {
        [SerializeField] private KeyCode interactKey = KeyCode.E;
        [SerializeField] private KeyCode moveUpKey = KeyCode.W;
        [SerializeField] private KeyCode moveDownKey = KeyCode.S;
        [SerializeField] private KeyCode alternateMoveUpKey = KeyCode.UpArrow;
        [SerializeField] private KeyCode alternateMoveDownKey = KeyCode.DownArrow;
        private bool hasFocus = true;

        public float Horizontal => isActiveAndEnabled && hasFocus
            ? GameInputMath.HorizontalFromKeys(
                UnityEngine.Input.GetKey(KeyCode.A), UnityEngine.Input.GetKey(KeyCode.D),
                UnityEngine.Input.GetKey(KeyCode.LeftArrow), UnityEngine.Input.GetKey(KeyCode.RightArrow))
            : 0f;
        public float Vertical => isActiveAndEnabled && hasFocus
            ? GameInputMath.VerticalFromKeys(
                UnityEngine.Input.GetKey(moveUpKey), UnityEngine.Input.GetKey(moveDownKey),
                UnityEngine.Input.GetKey(alternateMoveUpKey), UnityEngine.Input.GetKey(alternateMoveDownKey))
            : 0f;
        public Vector2 LookAxis => isActiveAndEnabled && hasFocus
            ? GameInputMath.LookAxisFromScreenPosition((Vector2)UnityEngine.Input.mousePosition, Screen.width, Screen.height)
            : Vector2.zero;
        public GameInputDevice CurrentDevice => GameInputDevice.Keyboard;
        public bool Interact => isActiveAndEnabled && hasFocus && UnityEngine.Input.GetKeyDown(interactKey);
        public bool BackPressed => isActiveAndEnabled && hasFocus && UnityEngine.Input.GetKeyDown(KeyCode.Escape);
        public Vector2 MouseScreenPos => isActiveAndEnabled && hasFocus
            ? (Vector2)UnityEngine.Input.mousePosition : Vector2.zero;

        private void OnEnable() { hasFocus = true; }
        private void OnDisable() { hasFocus = false; }
        private void OnApplicationFocus(bool focused) { hasFocus = focused; }
        private void OnApplicationPause(bool paused) { hasFocus = !paused; }
    }
}
