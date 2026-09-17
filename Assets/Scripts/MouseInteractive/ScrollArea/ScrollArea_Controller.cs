using UnityEngine;
using UnityEngine.Events;

public class ScrollArea_Controller : MonoBehaviour
{
    public bool manualInitialize = false;
    public enum ScrollMode { Vertical, Horizontal, Composite }
    public enum ScrollStepMode { Continuous, Discrete }

    [Header("滚动模式")]
    public ScrollMode scrollMode = ScrollMode.Vertical;

    [Header("核心组件")]
    [Tooltip("ContentArea 上的 DragContainer")]
    public DragContainer contentDragContainer;

    [Tooltip("ContentArea 上的 MouseDraggableObject")]
    public MouseDraggableObject contentDraggable;

    [Tooltip("ContentArea 上的 MouseScrollableObject（滚轮事件代理）")]
    public MouseScrollableObject contentScrollable;

    [Tooltip("实际内容容器，运行时被移入 ContentArea 下")]
    public Transform content;

    [Tooltip("ViewPort 上的 DragLimit（Inner 模式）")]
    public DragLimit viewportDragLimit;

    [Header("ScrollBar（可选）")]
    public ScrollBar_Controller verticalScrollBar;
    public ScrollBar_Controller horizontalScrollBar;

    [Header("滚轮")]
    [Range(0.01f, 0.2f)] public float scrollStep = 0.05f;
    [Tooltip("Continuous preserves the legacy scrollStep behaviour; Discrete advances by fixed pages.")]
    public ScrollStepMode stepMode = ScrollStepMode.Discrete;
    [Range(0.01f, 1f)] public float stepSize = 0.5f;

    [Header("程序滚动 Transition（可选）")]
    [Tooltip("TK-MOT-07 Position behaviour used for non-instant programmatic scrolling.")]
    [SerializeField] private TransitionBehaviour_Position scrollPositionTransition = null;
    [Min(0f)]
    [SerializeField] private float scrollTransitionDuration = 0.2f;
    [SerializeField] private bool scrollTransitionUseUnscaledTime = true;

    [Header("事件")]
    public UnityEvent<Vector2> onScrollPositionChanged;

    // 防止 OnContentPositionChanged 与 ScrollBar.onValueChanged 互相触发
    private bool _updatingFromContent = false;
    private Vector3 _lastTargetPosition;
    private bool _hasLastTargetPosition;
    private int _programmaticScrollGeneration;
    private bool _programmaticCompletionPending;
    private bool _initialized;
    private bool _hasPendingInitializationScroll;
    private Vector2 _pendingInitializationNormalizedPosition;

    public Transform ContentTransform
    {
        get { return contentDragContainer == null ? null : contentDragContainer.transform; }
    }
    public Vector2 NormalizedContentPosition
    {
        get { return contentDragContainer == null ? Vector2.zero : GetNormalizedContentPosition(); }
    }
    public bool HasLastTargetPosition { get { return _hasLastTargetPosition; } }
    public Vector3 LastTargetPosition { get { return _lastTargetPosition; } }
    public bool IsProgrammaticTransitionActive
    {
        get { return scrollPositionTransition != null && scrollPositionTransition.IsAnimating; }
    }

    private void Start()
    {
        if(!manualInitialize) Initialize();
    }

    private void OnDisable() { CompletePendingProgrammaticScroll(); }
    private void OnDestroy() { CompletePendingProgrammaticScroll(); }

    // ==================== 初始化 ====================

    public void Initialize()
    {
        if (contentDragContainer == null || viewportDragLimit == null) return;

        // 将 Content 移入 ContentArea 下，保持世界位置不变
        if (content != null)
        {
            content.SetParent(contentDragContainer.transform, true);
        }

        // 初始化 DragContainer（同步 collider 等）
        contentDragContainer.onInitialize();

        // 配置 DragContainer 模式
        switch (scrollMode)
        {
            case ScrollMode.Vertical:
                contentDragContainer.mode = DragContainer.DragMode.Vertical;
                break;
            case ScrollMode.Horizontal:
                contentDragContainer.mode = DragContainer.DragMode.Horizontal;
                break;
            case ScrollMode.Composite:
                contentDragContainer.mode = DragContainer.DragMode.Composite;
                break;
        }

        // 使用 DragLimit 模式
        contentDragContainer.limitMode = DragContainer.LimitMode.DragLimit;
        contentDragContainer.dragLimit = viewportDragLimit;

        // 计算运行时边界值
        contentDragContainer.UpdateBounds();

        // 禁用绑定的 ScrollBar 自身的滚轮（避免冲突）
        if (verticalScrollBar != null) verticalScrollBar.enableScroll = false;
        if (horizontalScrollBar != null) horizontalScrollBar.enableScroll = false;

        _initialized = true;
        if (_hasPendingInitializationScroll)
        {
            var pendingNormalizedPosition = _pendingInitializationNormalizedPosition;
            _hasPendingInitializationScroll = false;
            ApplyScrollTarget(pendingNormalizedPosition, false);
        }
    }

    // ==================== Content 位置变化 → 同步 ScrollBar ====================

    /// <summary>
    /// 绑定到 contentDragContainer.onDragPositionChanged
    /// </summary>
    public void OnContentPositionChanged(Vector3 delta)
    {
        if (contentDragContainer == null) return;
        if (_updatingFromContent) return;

        _updatingFromContent = true;

        Vector2 normalizedPos = GetNormalizedContentPosition();

        if (verticalScrollBar != null)
            verticalScrollBar.SetValue(normalizedPos.y, false);

        if (horizontalScrollBar != null)
            horizontalScrollBar.SetValue(normalizedPos.x, false);

        if (onScrollPositionChanged != null)
            onScrollPositionChanged.Invoke(normalizedPos);

        _updatingFromContent = false;
    }

    private Vector2 GetNormalizedContentPosition()
    {
        float posX = contentDragContainer.transform.position.x;
        float posY = contentDragContainer.transform.position.y;

        // Inner 模式下 ContentArea 向左移动（展示右侧内容）时 posX 减小，但 scrollbar 应增大
        // 因此 X 轴归一化值需要反转
        float normalizedX = Mathf.Approximately(contentDragContainer.LimitMaxX, contentDragContainer.LimitMinX)
            ? 0f : 1f - Mathf.Clamp01((posX - contentDragContainer.LimitMinX) / (contentDragContainer.LimitMaxX - contentDragContainer.LimitMinX));

        float normalizedY = Mathf.Approximately(contentDragContainer.LimitMaxY, contentDragContainer.LimitMinY)
            ? 0f : Mathf.Clamp01((posY - contentDragContainer.LimitMinY) / (contentDragContainer.LimitMaxY - contentDragContainer.LimitMinY));

        return new Vector2(normalizedX, normalizedY);
    }

    // ==================== 滚轮步进 ====================

    /// <summary>
    /// 滚轮事件回调，绑定到 MouseScrollableObject.scrollStepEvent
    /// </summary>
    public void OnScrollStep(float direction)
    {
        Vector2 current = GetNormalizedContentPosition();

        switch (scrollMode)
        {
            case ScrollMode.Vertical:
                ScrollTo(new Vector2(current.x, ResolveStepTarget(current.y, direction)));
                break;
            case ScrollMode.Horizontal:
                ScrollTo(new Vector2(ResolveStepTarget(current.x, direction), current.y));
                break;
            case ScrollMode.Composite:
                ScrollTo(new Vector2(current.x, ResolveStepTarget(current.y, direction)));
                break;
        }
    }

    internal static float CalculateDiscreteStepTarget(float current, float direction, float configuredStepSize)
    {
        var safeStepSize = configuredStepSize > 0f && !float.IsNaN(configuredStepSize) && !float.IsInfinity(configuredStepSize)
            ? configuredStepSize
            : 1f;
        if (direction == 0f)
        {
            return Mathf.Clamp01(current);
        }

        return Mathf.Clamp01(current - (direction * safeStepSize));
    }

    private float ResolveStepTarget(float current, float direction)
    {
        if (stepMode == ScrollStepMode.Discrete)
        {
            return CalculateDiscreteStepTarget(current, direction, stepSize);
        }

        return Mathf.Clamp01(current - (direction * scrollStep));
    }

    // ==================== 外部控制 ====================

    /// <summary>
    /// 将 ContentArea 移动到指定百分比位置（0~1）
    /// </summary>
    public void ScrollTo(Vector2 normalizedPosition)
    {
        if (contentDragContainer == null) return;

        var clampedNormalizedPosition = new Vector2(
            Mathf.Clamp01(normalizedPosition.x),
            Mathf.Clamp01(normalizedPosition.y));
        if (!_initialized && viewportDragLimit != null)
        {
            _pendingInitializationNormalizedPosition = clampedNormalizedPosition;
            _hasPendingInitializationScroll = true;
            return;
        }

        ApplyScrollTarget(clampedNormalizedPosition, true);
    }

    private void ApplyScrollTarget(Vector2 normalizedPosition, bool allowTransition)
    {
        // X 轴反转：normalizedPosition.x=1 对应 LimitMinX（ContentArea 最左，展示最右内容）
        var currentPosition = contentDragContainer.transform.position;
        var targetX = scrollMode == ScrollMode.Vertical
            ? currentPosition.x
            : Mathf.Lerp(contentDragContainer.LimitMaxX, contentDragContainer.LimitMinX, normalizedPosition.x);
        var targetY = scrollMode == ScrollMode.Horizontal
            ? currentPosition.y
            : Mathf.Lerp(contentDragContainer.LimitMinY, contentDragContainer.LimitMaxY, normalizedPosition.y);
        var targetPosition = new Vector3(targetX, targetY, currentPosition.z);
        _lastTargetPosition = targetPosition;
        _hasLastTargetPosition = true;
        var request = ++_programmaticScrollGeneration;

        if (allowTransition && CanAnimateProgrammaticScroll())
        {
            _programmaticCompletionPending = true;
            scrollPositionTransition.targetTransform = contentDragContainer.transform;
            var transition = scrollPositionTransition.PlayTo(
                targetPosition,
                scrollTransitionDuration,
                scrollTransitionUseUnscaledTime,
                () => CompleteProgrammaticScroll(request, false),
                () => CompleteProgrammaticScroll(request, true));
            if (transition != null)
            {
                return;
            }
            _programmaticCompletionPending = false;
        }
        else if (scrollPositionTransition != null)
        {
            // Inactive hosts cannot start coroutines. Invalidate and cancel an older request before landing this one.
            scrollPositionTransition.Cancel();
        }

        // Legacy scenes without the optional Transition wiring keep their existing API behaviour.
        CompleteScrollImmediately(targetPosition);
    }

    public void ScrollVerticalTo(float verticalPercent)
    {
        if (_updatingFromContent) return;
        Vector2 current = GetNormalizedContentPosition();
        ScrollTo(new Vector2(current.x, verticalPercent));
    }

    public void ScrollHorizontalTo(float HorizontalPercent)
    {
        if (_updatingFromContent) return;
        Vector2 current = GetNormalizedContentPosition();
        ScrollTo(new Vector2(HorizontalPercent, current.y));
    }

    private bool CanAnimateProgrammaticScroll()
    {
        return isActiveAndEnabled && gameObject.activeInHierarchy &&
            scrollPositionTransition != null && scrollPositionTransition.isActiveAndEnabled &&
            scrollPositionTransition.gameObject.activeInHierarchy;
    }

    private void CompletePendingProgrammaticScroll()
    {
        if (!_programmaticCompletionPending) return;
        var request = _programmaticScrollGeneration;
        if (scrollPositionTransition != null) scrollPositionTransition.Cancel();
        CompleteProgrammaticScroll(request, true);
    }

    private void CompleteProgrammaticScroll(int request, bool snapToTarget)
    {
        if (request != _programmaticScrollGeneration || !_programmaticCompletionPending) return;
        _programmaticCompletionPending = false;
        if (snapToTarget && contentDragContainer != null)
            contentDragContainer.moveToTargetPos(_lastTargetPosition);
        OnContentPositionChanged(Vector3.zero);
    }

    private void CompleteScrollImmediately(Vector3 targetPosition)
    {
        _programmaticCompletionPending = false;
        contentDragContainer.moveToTargetPos(targetPosition);
        OnContentPositionChanged(Vector3.zero);
    }
}
