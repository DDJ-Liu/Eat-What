using System;
using Spine.Unity;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Reusable side-view controller that changes only the world X position.
/// Input priority: assigned InputActionReference, project InputManager events,
/// then direct keyboard fallback when InputManager is absent.
/// </summary>
[DisallowMultipleComponent]
public sealed class HorizontalPlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Min(0f)] private float moveSpeed = 4f;
    [SerializeField] private bool startFacingRight = true;
    [SerializeField] private Transform visualRoot;

    [Header("Optional Horizontal Bounds")]
    [SerializeField] private bool useHorizontalBounds;
    [SerializeField] private float minX = -10f;
    [SerializeField] private float maxX = 10f;

    [Header("Optional Rigidbody2D")]
    [SerializeField] private Rigidbody2D targetRigidbody;
    [SerializeField] private bool freezeVerticalRigidbodyPosition = true;
    [SerializeField] private bool freezeRigidbodyRotation = true;

    [Header("Input")]
    [Tooltip("Optional Value action. It may return either a float axis or a Vector2; only X is used.")]
    [SerializeField] private InputActionReference horizontalMoveAction;
    [Tooltip("Used only when no action is assigned.")]
    [SerializeField] private bool useProjectInputManagerFallback = true;
    [Tooltip("Used only while no InputManager instance exists and no action is assigned.")]
    [SerializeField] private bool useKeyboardFallbackWhenInputManagerMissing = true;
    [SerializeField] private Key moveLeftKey = Key.A;
    [SerializeField] private Key moveRightKey = Key.D;
    [SerializeField] private Key alternateMoveLeftKey = Key.LeftArrow;
    [SerializeField] private Key alternateMoveRightKey = Key.RightArrow;

    [Header("Optional Animator Presentation")]
    [SerializeField] private Animator animator;
    [SerializeField] private bool setAnimatorMovingBool = true;
    [SerializeField] private string movingBoolName = "IsMoving";
    [SerializeField] private bool crossFadeAnimatorStates;
    [SerializeField] private string idleAnimatorStateName;
    [SerializeField] private string walkAnimatorStateName;
    [SerializeField, Min(0f)] private float animatorCrossFadeDuration = 0.1f;

    [Header("Optional Spine SkeletonAnimation Presentation")]
    [SerializeField] private SkeletonAnimation spineAnimation;
    [SpineAnimation(dataField = "spineAnimation")]
    [SerializeField] private string spineIdleAnimation;
    [SpineAnimation(dataField = "spineAnimation")]
    [SerializeField] private string spineWalkAnimation;
    [SerializeField] private int spineTrackIndex;

    public float HorizontalInput => horizontalInput;
    public bool IsMoving => moveSpeed > 0f && !Mathf.Approximately(horizontalInput, 0f);
    public bool IsFacingRight => facingRight;
    public float MoveSpeed
    {
        get => moveSpeed;
        set => moveSpeed = Mathf.Max(0f, value);
    }

    private InputAction boundMoveAction;
    private bool enabledBoundAction;
    private bool subscribedToInputManager;
    private bool projectLeftHeld;
    private bool projectRightHeld;
    private float horizontalInput;
    private bool facingRight;
    private bool presentationInitialized;
    private bool lastPresentationMoving;
    private Transform cachedVisualRoot;
    private Vector3 visualRootScale;
    private float visualRootScaleX;
    private RigidbodyConstraints2D initialRigidbodyConstraints;
    private bool capturedRigidbodyConstraints;

    private void Awake()
    {
        if (targetRigidbody == null)
            TryGetComponent(out targetRigidbody);

        cachedVisualRoot = visualRoot != null ? visualRoot : transform;
        visualRootScale = cachedVisualRoot.localScale;
        visualRootScaleX = Mathf.Abs(visualRootScale.x);
        if (visualRootScaleX < 0.0001f)
            visualRootScaleX = 1f;

        if (targetRigidbody != null)
        {
            initialRigidbodyConstraints = targetRigidbody.constraints;
            capturedRigidbodyConstraints = true;
        }

        SetFacing(startFacingRight);
    }

    private void OnEnable()
    {
        StopMovement();
        ApplyRigidbodyConstraints();
        BindInput();
    }

    private void Start()
    {
        UpdatePresentation(force: true);
    }

    private void OnDisable()
    {
        StopMovement();
        UnbindInput();
        RestoreRigidbodyConstraints();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            StopMovement();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
            StopMovement();
    }

    private void Update()
    {
        UpdateKeyboardFallback();

        if (targetRigidbody == null)
            MoveTransform(Time.deltaTime);

        UpdatePresentation(force: false);
    }

    private void FixedUpdate()
    {
        if (targetRigidbody != null)
            MoveRigidbody();
    }

    public void SetHorizontalInput(float input)
    {
        horizontalInput = Mathf.Clamp(input, -1f, 1f);
        if (!Mathf.Approximately(horizontalInput, 0f))
            SetFacing(horizontalInput > 0f);
        else
            StopRigidbodyHorizontalVelocity();
    }

    public void StopMovement()
    {
        projectLeftHeld = false;
        projectRightHeld = false;
        horizontalInput = 0f;
        StopRigidbodyHorizontalVelocity();
    }

    public void SetHorizontalBounds(float leftBoundary, float rightBoundary)
    {
        minX = Mathf.Min(leftBoundary, rightBoundary);
        maxX = Mathf.Max(leftBoundary, rightBoundary);
        useHorizontalBounds = true;
    }

    public void ClearHorizontalBounds()
    {
        useHorizontalBounds = false;
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

            if (enabledBoundAction)
                boundMoveAction.Disable();

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
        if (boundMoveAction == null || boundMoveAction.activeControl == null)
            return 0f;

        return boundMoveAction.activeControl.valueType == typeof(Vector2)
            ? boundMoveAction.ReadValue<Vector2>().x
            : boundMoveAction.ReadValue<float>();
    }

    private static float ReadHorizontalValue(InputAction.CallbackContext context)
    {
        return context.control != null && context.control.valueType == typeof(Vector2)
            ? context.ReadValue<Vector2>().x
            : context.ReadValue<float>();
    }

    private bool OnProjectKeyPressed(Key key)
    {
        bool consumed = false;
        if (IsLeftKey(key))
        {
            projectLeftHeld = true;
            consumed = true;
        }

        if (IsRightKey(key))
        {
            projectRightHeld = true;
            consumed = true;
        }

        if (consumed)
            UpdateProjectKeyInput();

        return consumed;
    }

    private bool OnProjectKeyReleased(Key key)
    {
        bool consumed = false;
        if (IsLeftKey(key))
        {
            projectLeftHeld = false;
            consumed = true;
        }

        if (IsRightKey(key))
        {
            projectRightHeld = false;
            consumed = true;
        }

        if (consumed)
            UpdateProjectKeyInput();

        return consumed;
    }

    private void UpdateProjectKeyInput()
    {
        SetHorizontalInput((projectRightHeld ? 1f : 0f) - (projectLeftHeld ? 1f : 0f));
    }

    private void UpdateKeyboardFallback()
    {
        if (boundMoveAction != null || !useKeyboardFallbackWhenInputManagerMissing || InputManager.Instance != null)
            return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            SetHorizontalInput(0f);
            return;
        }

        bool leftHeld = keyboard[moveLeftKey].isPressed || keyboard[alternateMoveLeftKey].isPressed;
        bool rightHeld = keyboard[moveRightKey].isPressed || keyboard[alternateMoveRightKey].isPressed;
        SetHorizontalInput((rightHeld ? 1f : 0f) - (leftHeld ? 1f : 0f));
    }

    private bool IsLeftKey(Key key)
    {
        return key == moveLeftKey || key == alternateMoveLeftKey;
    }

    private bool IsRightKey(Key key)
    {
        return key == moveRightKey || key == alternateMoveRightKey;
    }

    private void MoveTransform(float deltaTime)
    {
        if (Mathf.Approximately(horizontalInput, 0f) || moveSpeed <= 0f)
            return;

        Vector3 position = transform.position;
        position.x = ClampHorizontalPosition(position.x + horizontalInput * moveSpeed * deltaTime);
        transform.position = position;
    }

    private void MoveRigidbody()
    {
        if (Mathf.Approximately(horizontalInput, 0f) || moveSpeed <= 0f)
            return;

        Vector2 position = targetRigidbody.position;
        position.x = ClampHorizontalPosition(position.x + horizontalInput * moveSpeed * Time.fixedDeltaTime);
        targetRigidbody.MovePosition(position);
    }

    private float ClampHorizontalPosition(float x)
    {
        if (!useHorizontalBounds)
            return x;

        float leftBoundary = Mathf.Min(minX, maxX);
        float rightBoundary = Mathf.Max(minX, maxX);
        return Mathf.Clamp(x, leftBoundary, rightBoundary);
    }

    private void SetFacing(bool shouldFaceRight)
    {
        facingRight = shouldFaceRight;
        if (cachedVisualRoot == null)
            return;

        Vector3 scale = visualRootScale;
        scale.x = visualRootScaleX * (facingRight ? 1f : -1f);
        cachedVisualRoot.localScale = scale;
    }

    private void UpdatePresentation(bool force)
    {
        bool moving = IsMoving;
        if (!force && presentationInitialized && moving == lastPresentationMoving)
            return;

        presentationInitialized = true;
        lastPresentationMoving = moving;
        UpdateAnimatorPresentation(moving);
        UpdateSpinePresentation(moving);
    }

    private void UpdateAnimatorPresentation(bool moving)
    {
        if (animator == null)
            return;

        if (setAnimatorMovingBool && !string.IsNullOrWhiteSpace(movingBoolName))
            animator.SetBool(movingBoolName, moving);

        if (!crossFadeAnimatorStates)
            return;

        string stateName = moving ? walkAnimatorStateName : idleAnimatorStateName;
        if (string.IsNullOrWhiteSpace(stateName))
            return;

        int stateHash = Animator.StringToHash(stateName);
        if (animator.HasState(0, stateHash))
            animator.CrossFadeInFixedTime(stateHash, animatorCrossFadeDuration, 0);
    }

    private void UpdateSpinePresentation(bool moving)
    {
        if (spineAnimation == null)
            return;

        string animationName = moving ? spineWalkAnimation : spineIdleAnimation;
        if (string.IsNullOrWhiteSpace(animationName) || spineAnimation.SkeletonDataAsset == null)
            return;

        Spine.SkeletonData skeletonData = spineAnimation.SkeletonDataAsset.GetSkeletonData(false);
        if (skeletonData == null || skeletonData.FindAnimation(animationName) == null)
        {
            Debug.LogWarning($"{nameof(HorizontalPlayerController)}: Spine animation '{animationName}' was not found on {spineAnimation.name}.", this);
            return;
        }

        spineAnimation.AnimationState.SetAnimation(spineTrackIndex, animationName, true);
    }

    private void ApplyRigidbodyConstraints()
    {
        if (targetRigidbody == null || !capturedRigidbodyConstraints)
            return;

        RigidbodyConstraints2D constraints = initialRigidbodyConstraints;
        if (freezeVerticalRigidbodyPosition)
            constraints |= RigidbodyConstraints2D.FreezePositionY;
        if (freezeRigidbodyRotation)
            constraints |= RigidbodyConstraints2D.FreezeRotation;

        targetRigidbody.constraints = constraints;
    }

    private void RestoreRigidbodyConstraints()
    {
        if (targetRigidbody != null && capturedRigidbodyConstraints)
            targetRigidbody.constraints = initialRigidbodyConstraints;
    }

    private void StopRigidbodyHorizontalVelocity()
    {
        if (targetRigidbody == null)
            return;

        Vector2 velocity = targetRigidbody.velocity;
        velocity.x = 0f;
        targetRigidbody.velocity = velocity;
    }
}
