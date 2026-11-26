using UnityEngine;
using UnityEngine.InputSystem;

namespace EatWhat.Toolkit.Input
{
    /// <summary>Existing keyboard/mouse backend plus processed Input System right-stick look.</summary>
    [DisallowMultipleComponent]
    public sealed class LegacyInputProvider : MonoBehaviour, IGameInput, INavigationInput
    {
        [SerializeField] private KeyCode interactKey = KeyCode.E;
        [SerializeField] private KeyCode moveUpKey = KeyCode.W;
        [SerializeField] private KeyCode moveDownKey = KeyCode.S;
        [SerializeField] private KeyCode alternateMoveUpKey = KeyCode.UpArrow;
        [SerializeField] private KeyCode alternateMoveDownKey = KeyCode.DownArrow;
        private bool hasFocus = true;
        private bool applicationPaused;
        private LookInputState lookState;
        private bool CanRead => isActiveAndEnabled && hasFocus && !applicationPaused;

        public float Horizontal => CanRead
            ? GameInputMath.HorizontalFromKeys(
                UnityEngine.Input.GetKey(KeyCode.A), UnityEngine.Input.GetKey(KeyCode.D),
                UnityEngine.Input.GetKey(KeyCode.LeftArrow), UnityEngine.Input.GetKey(KeyCode.RightArrow))
            : 0f;
        public float Vertical => CanRead
            ? GameInputMath.VerticalFromKeys(
                UnityEngine.Input.GetKey(moveUpKey), UnityEngine.Input.GetKey(moveDownKey),
                UnityEngine.Input.GetKey(alternateMoveUpKey), UnityEngine.Input.GetKey(alternateMoveDownKey))
            : 0f;
        public Vector2 LookAxis => SampleLook().Axis;
        public GameInputDevice CurrentDevice => SampleLook().Device;
        private LookInputSample SampleLook()
        {
            Vector2 stick=Vector2.zero; bool available=false;
            if(CanRead)
            {
                var pad=Gamepad.current;
                available=pad!=null && pad.added && pad.enabled && pad.rightStick!=null;
                if(available) stick=pad.rightStick.ReadValue();
            }
            var sample=GameInputMath.SelectLook(lookState,(Vector2)UnityEngine.Input.mousePosition,
                Screen.width,Screen.height,stick,available,CanRead);
            lookState=sample.State;return sample;
        }
        public bool Interact => CanRead && UnityEngine.Input.GetKeyDown(interactKey);
        public bool BackPressed => CanRead && UnityEngine.Input.GetKeyDown(KeyCode.Escape);
        public Vector2 MouseScreenPos => CanRead
            ? (Vector2)UnityEngine.Input.mousePosition : Vector2.zero;

        private void OnEnable() { hasFocus = Application.isFocused; applicationPaused=false; lookState=default; }
        private void OnDisable() { hasFocus = false; lookState=default; }
        private void OnApplicationFocus(bool focused) { hasFocus = focused; lookState.HasMouse=false; }
        private void OnApplicationPause(bool paused) { applicationPaused=paused; lookState.HasMouse=false; }
    }
}
