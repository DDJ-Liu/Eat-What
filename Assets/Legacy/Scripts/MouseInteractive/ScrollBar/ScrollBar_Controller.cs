using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public enum ScrollBarDirection
{
    Horizontal,
    Vertical
}

public class ScrollBar_Controller : MonoBehaviour
{
    [Header("方向")]
    public ScrollBarDirection direction = ScrollBarDirection.Horizontal;

    [Header("子组件引用")]
    [Tooltip("滑块上的 DragContainer")]
    public DragContainer handleDragContainer;
    [Tooltip("滑块上的 MouseDraggableObject")]
    public MouseDraggableObject handleDraggable;
    [Tooltip("轨道上的 Button_MouseInteract（用于高亮联动）")]
    public Button_MouseInteract trackButton;

    [Header("轨道范围")]
    [Tooltip("轨道起点（value=0 的位置）")]
    public Transform trackStart;
    [Tooltip("轨道终点（value=1 的位置）")]
    public Transform trackEnd;

    [Header("点击与滚轮")]
    [Tooltip("整个 ScrollBar 的可交互区域（点击轨道跳转 + 滚轮响应）")]
    public Collider2D interactionArea;
    [Tooltip("滚轮事件代理（绑定到 MouseScrollableObject.scrollStepEvent）")]
    public MouseScrollableObject scrollableObject;
    public bool enableScroll = true;
    [Range(0.01f, 0.2f)] public float scrollStep = 0.05f;

    [Header("状态")]
    [SerializeField] private float _value = 0f;

    [Header("事件")]
    public UnityEvent<float> onValueChanged;

    public float Value
    {
        get => _value;
        set => SetValue(value, true);
    }

    // 防止 OnHandlePositionChanged 和 SetValue 互相触发
    private bool _updatingFromHandle = false;
    private bool _updatingFromCode = false;
    private bool _initialized = false;

    // Handle 悬浮/拖拽状态追踪
    private bool _handleHovered = false;
    private bool _handleDragging = false;
    private Collider2D _handleCollider;

    private void Start()
    {
        InitializeHandleDragContainer();
        SetValue(_value, false, true);

        if (handleDragContainer != null)
            _handleCollider = handleDragContainer.GetComponent<Collider2D>();
    }

    private void OnEnable()
    {
    }

    private void OnDisable()
    {
    }

    private void Update()
    {
        UpdateHandleHoverState();
        CheckTrackClick();
    }

    // ==================== Handle 悬浮联动 ====================

    private void UpdateHandleHoverState()
    {
        bool shouldHighlight = false;

        // 拖拽中始终高亮
        if (handleDraggable != null && handleDraggable.dragging)
        {
            _handleDragging = true;
            shouldHighlight = true;
        }
        else
        {
            _handleDragging = false;

            // 检查鼠标是否悬浮在 Handle 或 interactionArea（整个轨道区域）上
            Vector2 mousePos = Tools.getMousePos();
            if (_handleCollider != null && _handleCollider.OverlapPoint(mousePos))
            {
                shouldHighlight = true;
            }
            else if (interactionArea != null && interactionArea.OverlapPoint(mousePos))
            {
                shouldHighlight = true;
            }
        }

        // 状态变化时通过 trackButton 控制高亮
        if (shouldHighlight != _handleHovered)
        {
            _handleHovered = shouldHighlight;
            if (trackButton != null)
            {
                if (_handleHovered)
                {
                    trackButton.externalHighlightLock = true;
                    trackButton.MouseOver();
                }
                else
                {
                    trackButton.externalHighlightLock = false;
                    trackButton.MouseOut();
                }
            }
        }
    }

    // ==================== 轨道点击 ====================

    /// <summary>
    /// 检测鼠标点击 interactionArea 时跳转滑块（不依赖 trackButton）
    /// </summary>
    private void CheckTrackClick()
    {
        if (interactionArea == null) return;
        if (_handleDragging) return;

        // 检测鼠标左键按下
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Tools.getMousePos();
            if (interactionArea.OverlapPoint(mousePos))
            {
                // 如果点在 Handle 上，不视为轨道点击（让拖拽处理）
                if (_handleCollider != null && _handleCollider.OverlapPoint(mousePos))
                    return;

                float newValue = WorldPosToValue(mousePos);
                SetValue(newValue, true);

                // 点击轨道后自动进入 handle 拖拽状态
                if (handleDraggable != null && MouseManager.Instance != null)
                {
                    MouseManager.Instance.currentDraggableObject = handleDraggable;
                    MouseManager.Instance.dragStarted = false;
                    MouseManager.Instance.dragPerforming = true;
                    handleDraggable.enterDrag();
                }
            }
        }
    }

    /// <summary>
    /// 外部调用的轨道点击方法（保留兼容性）
    /// </summary>
    public void OnTrackClicked()
    {
        Vector3 mouseWorld = Tools.getMousePos();
        float newValue = WorldPosToValue(mouseWorld);
        SetValue(newValue, true);
    }

    // ==================== DragContainer 初始化 ====================

    /// <summary>
    /// 根据 direction 配置 DragContainer 的模式和边界
    /// </summary>
    public void InitializeHandleDragContainer()
    {
        if (_initialized) return;
        if (handleDragContainer == null || trackStart == null || trackEnd == null) return;
        _initialized = true;

        handleDragContainer.enableInertia = false;
        handleDragContainer.enableElasticBoundary = false;
        handleDragContainer.limitMode = DragContainer.LimitMode.Manual;

        if (direction == ScrollBarDirection.Horizontal)
        {
            handleDragContainer.mode = DragContainer.DragMode.Horizontal;
            float minX = Mathf.Min(trackStart.position.x, trackEnd.position.x);
            float maxX = Mathf.Max(trackStart.position.x, trackEnd.position.x);
            handleDragContainer.leftLimit = minX;
            handleDragContainer.rightLimit = maxX;
        }
        else
        {
            handleDragContainer.mode = DragContainer.DragMode.Vertical;
            float minY = Mathf.Min(trackStart.position.y, trackEnd.position.y);
            float maxY = Mathf.Max(trackStart.position.y, trackEnd.position.y);
            handleDragContainer.bottomLimit = minY;
            handleDragContainer.topLimit = maxY;
        }
    }

    // ==================== Value 控制 ====================

    /// <summary>
    /// 设置 value 并移动滑块
    /// </summary>
    public void SetValue(float newValue, bool notify, bool force = false)
    {
        newValue = Mathf.Clamp01(newValue);
        if (!force && Mathf.Approximately(_value, newValue)) return;

        _value = newValue;

        // 通过 DragContainer 移动滑块
        if (!_updatingFromHandle && handleDragContainer != null)
        {
            _updatingFromCode = true;
            Vector3 targetPos = ValueToWorldPos(_value);
            handleDragContainer.moveToTargetPos(targetPos);
            _updatingFromCode = false;
        }

        if (notify) onValueChanged?.Invoke(_value);
    }

    /// <summary>
    /// value → 世界坐标
    /// </summary>
    private Vector3 ValueToWorldPos(float val)
    {
        return Vector3.Lerp(trackStart.position, trackEnd.position, val);
    }

    /// <summary>
    /// 世界坐标 → value（投影到轨道轴）
    /// </summary>
    private float WorldPosToValue(Vector3 worldPos)
    {
        Vector3 trackDir = trackEnd.position - trackStart.position;
        float trackLength = trackDir.magnitude;
        if (trackLength < 0.001f) return 0f;
        float projected = Vector3.Dot(worldPos - trackStart.position, trackDir.normalized);
        return Mathf.Clamp01(projected / trackLength);
    }

    // ==================== 拖拽回调 ====================

    /// <summary>
    /// Handle 位置变化时反算 value（绑定到 DragContainer.onDragPositionChanged）
    /// </summary>
    public void OnHandlePositionChanged(Vector3 delta)
    {
        if (_updatingFromCode) return;

        _updatingFromHandle = true;
        float newValue = WorldPosToValue(handleDragContainer.transform.position);
        SetValue(newValue, true);
        _updatingFromHandle = false;
    }

    // ==================== 步进方法 ====================

    /// <summary>
    /// 向 value 增大方向步进一步
    /// </summary>
    public void StepForward()
    {
        SetValue(_value + scrollStep, true);
    }

    /// <summary>
    /// 向 value 减小方向步进一步
    /// </summary>
    public void StepBackward()
    {
        SetValue(_value - scrollStep, true);
    }

    /// <summary>
    /// 按自定义步长步进（正值增大，负值减小）
    /// </summary>
    public void Step(float amount)
    {
        SetValue(_value + amount, true);
    }

    /// <summary>
    /// 滚轮事件回调：向下滚动(direction=-1)时 value 增加，向上滚动(direction=+1)时 value 减少
    /// 可直接绑定到 MouseScrollableObject.scrollStepEvent
    /// </summary>
    public void OnScrollStep(float direction)
    {
        if (!enableScroll) return;
        Step(-direction * scrollStep);
    }
}
