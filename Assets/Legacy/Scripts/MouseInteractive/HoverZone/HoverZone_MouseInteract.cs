using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 悬停区域：鼠标在区域内停留指定时间后触发事件。
/// 新架构下为 HoverTrigger 的业务宿主：检测委托给 HoverTrigger，
/// 本组件在自身 Update 里轮询 HoverTrigger.IsHovered，并查询 MouseManager
/// 的 current* 字段决定是否被其他交互物压制，压制期间 over/out/倒计时全部暂停。
/// </summary>
[RequireComponent(typeof(HoverTrigger))]
public class HoverZone_MouseInteract : MonoBehaviour
{
    [Header("条件")]
    public ConditionReceiver condition = new ConditionReceiver();
    public bool allowToUse { get => condition.allowToUse; set => condition.allowToUse = value; }
    [Tooltip("条件判断为否时仍然触发悬停事件")]
    public bool weakCondition = false;
    public UnityEvent<bool> conditionLogicEvent;

    [Header("悬停设置")]
    [Tooltip("悬停触发所需时间（秒）")]
    public float hoverDuration = 1.0f;

    [Header("悬停事件")]
    [Tooltip("悬停计时完成时触发（每次悬停仅触发一次）")]
    public UnityEvent hoverTriggerEvent;
    [Tooltip("每帧广播悬停进度 0~1，可用于驱动进度条等UI")]
    public UnityEvent<float> hoverProgressEvent;

    [Header("高亮")]
    public UnityEvent overEvent;
    public UnityEvent outEvent;

    [Header("调试")]
    [SerializeField] private float _hoverTimer = 0f;
    [SerializeField] private bool _hoverTriggered = false;

    private HoverTrigger _trigger;
    private bool _zoneHovered;

    private void Awake()
    {
        _trigger = GetComponent<HoverTrigger>();
    }

    private void OnEnable()
    {
        if (_trigger != null) _trigger.enabled = true;
    }

    private void OnDisable()
    {
        if (_trigger != null) _trigger.enabled = false;

        // 关闭时补一次 out，避免外部状态残留
        if (_zoneHovered)
        {
            _zoneHovered = false;
            outEvent?.Invoke();
        }
        _hoverTimer = 0f;
        _hoverTriggered = false;
    }

    private void Update()
    {
        if (!condition.RunCheck())
        {
            allowToUse = true;
        }
        conditionLogicEvent?.Invoke(allowToUse);

        bool triggerHovered = _trigger != null && _trigger.IsHovered;
        bool suppressed = IsSuppressedByOtherInteractives();
        bool effective = triggerHovered && !suppressed;

        if (effective && !_zoneHovered)
        {
            _zoneHovered = true;
            overEvent?.Invoke();
        }
        else if (!effective && _zoneHovered)
        {
            _zoneHovered = false;
            _hoverTimer = 0f;
            _hoverTriggered = false;
            outEvent?.Invoke();
        }

        if (_zoneHovered && !_hoverTriggered)
        {
            _hoverTimer += Time.deltaTime;
            hoverProgressEvent?.Invoke(Mathf.Clamp01(_hoverTimer / hoverDuration));

            if (_hoverTimer >= hoverDuration)
            {
                if (!allowToUse && !weakCondition) return;

                _hoverTriggered = true;
                hoverProgressEvent?.Invoke(1f);
                hoverTriggerEvent?.Invoke();
            }
        }
    }

    private bool IsSuppressedByOtherInteractives()
    {
        var mm = MouseManager.Instance;
        if (mm == null) return false;
        return mm.currentPressableHoverTarget != null
            || mm.currentDraggableObject != null
            || mm.currentHoldObject != null
            || mm.currentScrollableObject != null;
    }
}
