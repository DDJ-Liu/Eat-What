using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 辅助 hover 触发器：鼠标进入/离开时触发事件。
/// 分布式自检测，主动向 MouseManager 登记，不被任何交互物压制。
/// 允许多实例同时激活，适合做不影响点击仲裁的辅助高亮。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class HoverTrigger : MonoBehaviour
{
    [Header("事件")]
    public UnityEvent overEvent;
    public UnityEvent outEvent;

    protected Collider2D _hoverCollider;
    private bool _isHovered;

    public bool IsHovered => _isHovered;

    protected virtual void Start()
    {
        if (_hoverCollider == null) _hoverCollider = GetComponent<Collider2D>();

        // 遍历 overEvent 和 outEvent，找到所有绑定的 Button_Visual 并标记为 HoverMode
        InitializeVisuals(overEvent);
        InitializeVisuals(outEvent);
    }

    /// <summary>
    /// 遍历 UnityEvent，将所有 Button_Visual 类型的监听器标记为 HoverMode
    /// </summary>
    private void InitializeVisuals(UnityEvent unityEvent)
    {
        if (unityEvent == null) return;

        int listenerCount = unityEvent.GetPersistentEventCount();
        for (int i = 0; i < listenerCount; i++)
        {
            UnityEngine.Object target = unityEvent.GetPersistentTarget(i);
            if (target is Button_Visual visual)
            {
                visual.isHoverMode = true;
            }
        }
    }

    protected virtual void Update()
    {
        if (_hoverCollider == null) return;
        if (MouseManager.Instance == null || MouseManager.Instance.currentMouseLayer == null) return;

        bool inside = _hoverCollider.OverlapPoint(Tools.getMousePos());
        bool layerActive = IsInActiveLayer();
        bool shouldHover = inside && layerActive;

        if (shouldHover != _isHovered) SetHoverInternal(shouldHover);
    }

    private bool IsInActiveLayer()
    {
        var layer = MouseManager.Instance?.currentMouseLayer;
        if (layer == null) return false;
        foreach (var mask in layer.alloweInteractionLayers)
        {
            if ((mask.value & (1 << gameObject.layer)) != 0) return true;
        }
        return false;
    }

    private void SetHoverInternal(bool hovered)
    {
        _isHovered = hovered;
        if (hovered)
        {
            MouseManager.Instance?.RegisterHoverTrigger(this);
            overEvent?.Invoke();
        }
        else
        {
            MouseManager.Instance?.UnregisterHoverTrigger(this);
            outEvent?.Invoke();
        }
    }

    protected virtual void OnDisable()
    {
        if (_isHovered) SetHoverInternal(false);
    }
}
