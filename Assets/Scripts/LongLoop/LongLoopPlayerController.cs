using EatWhat.Toolkit.Input;
using UnityEngine;
using UnityEngine.Serialization;

namespace EatWhat.LongLoop
{
    /// <summary>Formal street player movement. Inputs and presentation enter through interfaces.</summary>
    [DisallowMultipleComponent]
    public class LongLoopPlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField, Min(0f), FormerlySerializedAs("moveSpeed")]
        private float moveSpeed_TUNE = 4f;
        [SerializeField, Min(0f)] private float verticalSpeedFactor_TUNE = 0.6f;
        [SerializeField] private bool startFacingRight = true;
        [SerializeField] private Transform visualRoot;
        [Header("Bounds")]
        [SerializeField] private Transform boundaryL_TUNE;
        [SerializeField] private Transform boundaryR_TUNE;
        [SerializeField] private WalkArea walkArea = null;
        [SerializeField] private bool useHorizontalBounds;
        [SerializeField] private float minX = -10f;
        [SerializeField] private float maxX = 10f;
        [Header("Optional Rigidbody2D")]
        [SerializeField] private Rigidbody2D targetRigidbody;
        [SerializeField] private bool freezeVerticalRigidbodyPosition = true;
        [SerializeField] private bool freezeRigidbodyRotation = true;
        [Header("Interfaces")]
        [SerializeField] private MonoBehaviour gameInputProvider;
        [SerializeField] private MonoBehaviour avatarBehaviour;
        [SerializeField] private OperationLock operationLock = null;

        private IGameInput input;
        private IPlayerAvatar avatar;
        private float horizontalInput;
        private float verticalInput;
        private bool facingRight;
        private bool hasFocus = true;
        private bool lastMoving;
        private bool avatarNotified;
        private Transform cachedVisualRoot;
        private float visualScaleX;
        private bool hasDepthScaleBaseline;
        private RigidbodyConstraints2D initialConstraints;
        private bool capturedConstraints;
        private RigidbodyConstraints2D lastAppliedConstraints;
        private bool ownConstraintsApplied;
        private bool verticalMotionOwned;

        public float HorizontalInput => horizontalInput;
        public float VerticalInput => verticalInput;
        public Vector2 MoveInput => new Vector2(horizontalInput, verticalInput);
        public bool IsFacingRight => facingRight;
        public bool IsMoving
        {
            get
            {
                if ((horizontalInput == 0f && verticalInput == 0f) || moveSpeed_TUNE <= 0f) return false;
                var effectiveInput = RigidbodyAllowedInput;
                float left, right;
                GetLimits(out left, out right);
                if (!HasActiveWalkArea)
                    return HorizontalMotion.CanAdvance(CurrentX, effectiveInput.x, moveSpeed_TUNE, left, right);
                var current = CurrentPosition;
                var next = PlanarMotion.Step(current, effectiveInput, moveSpeed_TUNE,
                    verticalSpeedFactor_TUNE, 0.02f, walkArea, left, right);
                return (next - current).sqrMagnitude > 0.00000001f;
            }
        }
        public float MoveSpeed { get => moveSpeed_TUNE; set => moveSpeed_TUNE = Mathf.Max(0f, value); }
        public float VerticalSpeedFactor
        {
            get => verticalSpeedFactor_TUNE;
            set => verticalSpeedFactor_TUNE = Mathf.Max(0f, value);
        }
        private float CurrentX => targetRigidbody != null ? targetRigidbody.position.x : transform.position.x;
        private Vector2 CurrentPosition => targetRigidbody != null
            ? targetRigidbody.position : new Vector2(transform.position.x, transform.position.y);
        private Vector2 RigidbodyAllowedInput
        {
            get
            {
                var value = MoveInput;
                if (targetRigidbody == null) return value;
                var constraints = targetRigidbody.constraints;
                if ((constraints & RigidbodyConstraints2D.FreezePositionX) != 0) value.x = 0f;
                if ((constraints & RigidbodyConstraints2D.FreezePositionY) != 0) value.y = 0f;
                return value;
            }
        }
        private bool HasActiveWalkArea
        {
            get
            {
                if (walkArea == null || !walkArea.IsUsable) return false;
                if (targetRigidbody == null) return true;
                if ((initialConstraints & RigidbodyConstraints2D.FreezePositionY) != 0) return false;
                return ownConstraintsApplied && targetRigidbody.constraints == lastAppliedConstraints ||
                    (targetRigidbody.constraints & RigidbodyConstraints2D.FreezePositionY) == 0;
            }
        }

        public void ConfigureInterfaces(IGameInput gameInput, IPlayerAvatar playerAvatar)
        {
            input = gameInput;
            avatar = playerAvatar;
            if (cachedVisualRoot != null)
            {
                avatar?.SetFacing(facingRight);
                NotifyAvatar(true);
            }
        }

        public void ConfigureOperationLock(OperationLock value) { operationLock = value; }

        public void ConfigureBoundaries(Transform left, Transform right)
        {
            boundaryL_TUNE = left;
            boundaryR_TUNE = right;
        }

        public void ConfigureWalkArea(WalkArea value)
        {
            walkArea = value;
            ApplyRigidbodyConstraints();
            NotifyAvatar(false);
        }

        public void PlaceAtSpawn(Transform spawn)
        {
            if (spawn == null) return;
            Vector3 world = transform.position;
            world.x = spawn.position.x;
            world.y = spawn.position.y;
            if (targetRigidbody != null)
                targetRigidbody.position = new Vector2(world.x, world.y);
            else
                transform.position = world;
        }

        protected virtual void Awake()
        {
            moveSpeed_TUNE = Mathf.Max(0f, moveSpeed_TUNE);
            if (targetRigidbody == null) TryGetComponent(out targetRigidbody);
            cachedVisualRoot = visualRoot != null ? visualRoot : transform;
            visualScaleX = Mathf.Abs(cachedVisualRoot.localScale.x);
            if (visualScaleX < 0.0001f) visualScaleX = 1f;
            visualScaleX = DepthScale.ResolveFacingMagnitude(cachedVisualRoot, visualScaleX,
                ref hasDepthScaleBaseline);
            if (targetRigidbody != null)
            {
                initialConstraints = targetRigidbody.constraints;
                capturedConstraints = true;
            }
            if (input == null) input = gameInputProvider as IGameInput;
            if (avatar == null) avatar = avatarBehaviour as IPlayerAvatar;
            SetFacing(startFacingRight);
        }

        protected virtual void OnEnable()
        {
            hasFocus = true;
            StopMovement();
            ApplyRigidbodyConstraints();
        }

        protected virtual void Start() { NotifyAvatar(true); }

        protected virtual void OnDisable()
        {
            StopMovement();
            RestoreRigidbodyConstraints();
        }

        protected virtual void OnApplicationFocus(bool focused)
        {
            hasFocus = focused;
            if (!focused) StopMovement();
        }

        protected virtual void OnApplicationPause(bool paused)
        {
            if (paused) { hasFocus = false; StopMovement(); }
            else hasFocus = true;
        }

        protected virtual void Update()
        {
            ApplyRigidbodyConstraints();
            if (operationLock != null && operationLock.IsLocked)
            { StopMovement(); return; }
            if (hasFocus && input != null)
                SetMoveInput(new Vector2(input.Horizontal, input.Vertical));
            else if (!hasFocus)
                StopMovement();
            if (targetRigidbody == null)
                MoveTransform(Time.deltaTime);
            NotifyAvatar(false);
        }

        protected virtual void FixedUpdate()
        {
            if (targetRigidbody != null)
            {
                ApplyRigidbodyConstraints();
                MoveRigidbody(Time.fixedDeltaTime);
            }
        }

        public virtual void SetHorizontalInput(float value)
        {
            SetMoveInput(new Vector2(value, verticalInput));
        }

        public virtual void SetMoveInput(Vector2 value)
        {
            horizontalInput = Mathf.Clamp(value.x, -1f, 1f);
            verticalInput = Mathf.Clamp(value.y, -1f, 1f);
            if (horizontalInput != 0f) SetFacing(horizontalInput > 0f);
            else StopRigidbodyHorizontalVelocity();
            if (verticalInput == 0f) StopRigidbodyOwnedVerticalVelocity();
            NotifyAvatar(false);
        }

        public virtual void StopMovement()
        {
            horizontalInput = 0f;
            verticalInput = 0f;
            StopRigidbodyHorizontalVelocity();
            StopRigidbodyOwnedVerticalVelocity();
            NotifyAvatar(false);
        }

        public void SetHorizontalBounds(float leftBoundary, float rightBoundary)
        {
            minX = Mathf.Min(leftBoundary, rightBoundary);
            maxX = Mathf.Max(leftBoundary, rightBoundary);
            useHorizontalBounds = true;
        }

        public void ClearHorizontalBounds() { useHorizontalBounds = false; }

        private void GetLimits(out float left, out float right)
        {
            if (boundaryL_TUNE != null && boundaryR_TUNE != null)
            {
                left = boundaryL_TUNE.position.x;
                right = boundaryR_TUNE.position.x;
            }
            else if (useHorizontalBounds) { left = minX; right = maxX; }
            else { left = float.NegativeInfinity; right = float.PositiveInfinity; }
        }

        private void MoveTransform(float seconds)
        {
            float left, right;
            GetLimits(out left, out right);
            Vector3 position = transform.position;
            var next = PlanarMotion.Step(new Vector2(position.x, position.y), MoveInput,
                moveSpeed_TUNE, verticalSpeedFactor_TUNE, seconds,
                HasActiveWalkArea ? walkArea : null, left, right);
            if (next.x != position.x || next.y != position.y)
            {
                position.x = next.x;
                position.y = next.y;
                transform.position = position;
            }
        }

        private void MoveRigidbody(float seconds)
        {
            float left, right;
            GetLimits(out left, out right);
            Vector2 position = targetRigidbody.position;
            var next = PlanarMotion.Step(position, RigidbodyAllowedInput, moveSpeed_TUNE,
                verticalSpeedFactor_TUNE, seconds, HasActiveWalkArea ? walkArea : null, left, right);
            if (next != position) targetRigidbody.MovePosition(next);
            if (horizontalInput == 0f || next.x == position.x) StopRigidbodyHorizontalVelocity();
            if (next.y != position.y) verticalMotionOwned = true;
            else StopRigidbodyOwnedVerticalVelocity();
        }

        private void SetFacing(bool right)
        {
            if (facingRight == right && avatarNotified) return;
            facingRight = right;
            if (cachedVisualRoot != null)
            {
                visualScaleX = DepthScale.ResolveFacingMagnitude(cachedVisualRoot, visualScaleX,
                    ref hasDepthScaleBaseline);
                if (!DepthScale.TrySetFacing(cachedVisualRoot, right))
                {
                    Vector3 scale = cachedVisualRoot.localScale;
                    scale.x = visualScaleX * (right ? 1f : -1f);
                    cachedVisualRoot.localScale = scale;
                }
            }
            avatar?.SetFacing(right);
        }

        private void NotifyAvatar(bool force)
        {
            bool moving = IsMoving;
            if (force || !avatarNotified || moving != lastMoving)
            {
                lastMoving = moving;
                avatarNotified = true;
                avatar?.SetMoving(moving);
            }
        }

        private void ApplyRigidbodyConstraints()
        {
            if (targetRigidbody == null || !capturedConstraints) return;
            if (ownConstraintsApplied)
            {
                if (targetRigidbody.constraints != lastAppliedConstraints) return;
            }
            else if (targetRigidbody.constraints != initialConstraints) return;
            RigidbodyConstraints2D value = initialConstraints;
            if (freezeVerticalRigidbodyPosition && !HasActiveWalkArea)
                value |= RigidbodyConstraints2D.FreezePositionY;
            if (freezeRigidbodyRotation) value |= RigidbodyConstraints2D.FreezeRotation;
            targetRigidbody.constraints = value;
            lastAppliedConstraints = value;
            ownConstraintsApplied = true;
        }

        private void RestoreRigidbodyConstraints()
        {
            if (targetRigidbody != null && capturedConstraints && ownConstraintsApplied &&
                targetRigidbody.constraints == lastAppliedConstraints)
                targetRigidbody.constraints = initialConstraints;
            ownConstraintsApplied = false;
        }

        private void StopRigidbodyHorizontalVelocity()
        {
            if (targetRigidbody == null) return;
            Vector2 velocity = targetRigidbody.linearVelocity;
            if (velocity.x == 0f) return;
            velocity.x = 0f;
            targetRigidbody.linearVelocity = velocity;
        }

        private void StopRigidbodyOwnedVerticalVelocity()
        {
            if (targetRigidbody == null || !verticalMotionOwned) return;
            Vector2 velocity = targetRigidbody.linearVelocity;
            velocity.y = 0f;
            targetRigidbody.linearVelocity = velocity;
            verticalMotionOwned = false;
        }

        private void OnValidate()
        {
            moveSpeed_TUNE = Mathf.Max(0f, moveSpeed_TUNE);
            verticalSpeedFactor_TUNE = Mathf.Max(0f, verticalSpeedFactor_TUNE);
        }
    }
}
