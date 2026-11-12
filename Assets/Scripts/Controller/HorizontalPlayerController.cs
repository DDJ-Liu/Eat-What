using System;
using EatWhat.LongLoop;
using EatWhat.Toolkit.Input;
using Spine.Unity;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Original GUID and Inspector fields remain here for the prototype scene.
/// Movement and bounds live in the LongLoop base; this class adapts its old input and presentation.
/// </summary>
[DisallowMultipleComponent]
public sealed class HorizontalPlayerController : LongLoopPlayerController, IGameInput, IPlayerAvatar
{
    [Header("Prototype Input Compatibility")]
    [SerializeField] private InputActionReference horizontalMoveAction;
    [SerializeField] private bool useProjectInputManagerFallback = true;
    [SerializeField] private bool useKeyboardFallbackWhenInputManagerMissing = true;
    [SerializeField] private Key moveLeftKey = Key.A;
    [SerializeField] private Key moveRightKey = Key.D;
    [SerializeField] private Key alternateMoveLeftKey = Key.LeftArrow;
    [SerializeField] private Key alternateMoveRightKey = Key.RightArrow;

    [Header("Prototype Animator Compatibility")]
    [SerializeField] private Animator animator;
    [SerializeField] private bool setAnimatorMovingBool = true;
    [SerializeField] private string movingBoolName = "IsMoving";
    [SerializeField] private bool crossFadeAnimatorStates;
    [SerializeField] private string idleAnimatorStateName;
    [SerializeField] private string walkAnimatorStateName;
    [SerializeField, Min(0f)] private float animatorCrossFadeDuration = 0.1f;

    [Header("Prototype Spine Compatibility")]
    [SerializeField] private SkeletonAnimation spineAnimation;
    [SpineAnimation(dataField = "spineAnimation")]
    [SerializeField] private string spineIdleAnimation;
    [SpineAnimation(dataField = "spineAnimation")]
    [SerializeField] private string spineWalkAnimation;
    [SerializeField] private int spineTrackIndex;

    private InputAction boundMoveAction;
    private bool enabledBoundAction;
    private bool subscribedToInputManager;
    private bool projectLeftHeld;
    private bool projectRightHeld;
    private float prototypeHorizontal;

    public float Horizontal => prototypeHorizontal;
    public float Vertical => 0f;
    public Vector2 LookAxis => Vector2.zero;
    public GameInputDevice CurrentDevice => GameInputDevice.Keyboard;
    public bool Interact => false;
    public Vector2 MouseScreenPos => Vector2.zero;

    protected override void Awake()
    {
        ConfigureInterfaces(this, this);
        base.Awake();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        BindInput();
    }

    protected override void OnDisable()
    {
        UnbindInput();
        base.OnDisable();
    }

    protected override void OnApplicationFocus(bool focused)
    {
        if (!focused) prototypeHorizontal = 0f;
        base.OnApplicationFocus(focused);
    }

    protected override void OnApplicationPause(bool paused)
    {
        if (paused) prototypeHorizontal = 0f;
        base.OnApplicationPause(paused);
    }

    protected override void Update()
    {
        UpdateKeyboardFallback();
        base.Update();
    }

    public override void SetHorizontalInput(float value)
    {
        prototypeHorizontal = Mathf.Clamp(value, -1f, 1f);
        base.SetHorizontalInput(prototypeHorizontal);
    }

    public override void StopMovement()
    {
        prototypeHorizontal = 0f;
        projectLeftHeld = false;
        projectRightHeld = false;
        base.StopMovement();
    }

    void IPlayerAvatar.SetFacing(bool facingRight)
    {
        // LongLoopPlayerController applies the saved visualRoot scale.
    }

    void IPlayerAvatar.SetMoving(bool moving)
    {
        if (animator != null)
        {
            if (setAnimatorMovingBool && !string.IsNullOrWhiteSpace(movingBoolName))
                animator.SetBool(movingBoolName, moving);
            if (crossFadeAnimatorStates)
            {
                string state = moving ? walkAnimatorStateName : idleAnimatorStateName;
                if (!string.IsNullOrWhiteSpace(state))
                {
                    int hash = Animator.StringToHash(state);
                    if (animator.HasState(0, hash))
                        animator.CrossFadeInFixedTime(hash, animatorCrossFadeDuration, 0);
                }
            }
        }

        if (spineAnimation == null) return;
        string animationName = moving ? spineWalkAnimation : spineIdleAnimation;
        if (string.IsNullOrWhiteSpace(animationName) || spineAnimation.SkeletonDataAsset == null)
            return;
        Spine.SkeletonData data = spineAnimation.SkeletonDataAsset.GetSkeletonData(false);
        if (data == null || data.FindAnimation(animationName) == null)
        {
            Debug.LogWarning($"HorizontalPlayerController: Spine animation '{animationName}' was not found on {spineAnimation.name}.", this);
            return;
        }
        spineAnimation.AnimationState.SetAnimation(spineTrackIndex, animationName, true);
    }

    private void BindInput()
    {
        if (horizontalMoveAction != null && horizontalMoveAction.action != null)
        {
            boundMoveAction = horizontalMoveAction.action;
            boundMoveAction.performed += OnMoveActionPerformed;
            boundMoveAction.canceled += OnMoveActionCanceled;
            if (!boundMoveAction.enabled)
            {
                boundMoveAction.Enable();
                enabledBoundAction = true;
            }
            SetHorizontalInput(ReadBoundActionHorizontalValue());
            return;
        }
        if (useProjectInputManagerFallback)
        {
            InputManager.OnKeyPressed += OnProjectKeyPressed;
            InputManager.OnKeyReleased += OnProjectKeyReleased;
            subscribedToInputManager = true;
        }
    }

    private void UnbindInput()
    {
        if (boundMoveAction != null)
        {
            boundMoveAction.performed -= OnMoveActionPerformed;
            boundMoveAction.canceled -= OnMoveActionCanceled;
            if (enabledBoundAction) boundMoveAction.Disable();
            boundMoveAction = null;
            enabledBoundAction = false;
        }
        if (subscribedToInputManager)
        {
            InputManager.OnKeyPressed -= OnProjectKeyPressed;
            InputManager.OnKeyReleased -= OnProjectKeyReleased;
            subscribedToInputManager = false;
        }
    }

    private void OnMoveActionPerformed(InputAction.CallbackContext context)
    {
        SetHorizontalInput(ReadHorizontalValue(context));
    }

    private void OnMoveActionCanceled(InputAction.CallbackContext context)
    {
        SetHorizontalInput(0f);
    }

    private float ReadBoundActionHorizontalValue()
    {
        if (boundMoveAction == null || boundMoveAction.activeControl == null) return 0f;
        return boundMoveAction.activeControl.valueType == typeof(Vector2)
            ? boundMoveAction.ReadValue<Vector2>().x : boundMoveAction.ReadValue<float>();
    }

    private static float ReadHorizontalValue(InputAction.CallbackContext context)
    {
        return context.control != null && context.control.valueType == typeof(Vector2)
            ? context.ReadValue<Vector2>().x : context.ReadValue<float>();
    }

    private bool OnProjectKeyPressed(Key key)
    {
        bool recognized = IsLeftKey(key) || IsRightKey(key);
        if (IsLeftKey(key)) projectLeftHeld = true;
        if (IsRightKey(key)) projectRightHeld = true;
        if (recognized) UpdateProjectKeyInput();
        return recognized;
    }

    private bool OnProjectKeyReleased(Key key)
    {
        bool recognized = IsLeftKey(key) || IsRightKey(key);
        if (IsLeftKey(key)) projectLeftHeld = false;
        if (IsRightKey(key)) projectRightHeld = false;
        if (recognized) UpdateProjectKeyInput();
        return recognized;
    }

    private void UpdateProjectKeyInput()
    {
        SetHorizontalInput(GameInputMath.HorizontalFromKeys(projectLeftHeld, projectRightHeld, false, false));
    }

    private void UpdateKeyboardFallback()
    {
        if (boundMoveAction != null || !useKeyboardFallbackWhenInputManagerMissing || InputManager.Instance != null)
            return;
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) { SetHorizontalInput(0f); return; }
        bool left = keyboard[moveLeftKey].isPressed || keyboard[alternateMoveLeftKey].isPressed;
        bool right = keyboard[moveRightKey].isPressed || keyboard[alternateMoveRightKey].isPressed;
        SetHorizontalInput(GameInputMath.HorizontalFromKeys(left, right, false, false));
    }

    private bool IsLeftKey(Key key) => key == moveLeftKey || key == alternateMoveLeftKey;
    private bool IsRightKey(Key key) => key == moveRightKey || key == alternateMoveRightKey;
}
