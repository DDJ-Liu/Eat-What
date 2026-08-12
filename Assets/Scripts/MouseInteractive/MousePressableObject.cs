using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public abstract class MousePressableObject : MonoBehaviour
{
    protected Collider2D _hoverCollider;
    protected bool _isHovered = false;
    public bool hasVisual { get; protected set; }

    protected virtual void Start()
    {
        if(_hoverCollider == null) _hoverCollider = GetComponent<Collider2D>();
        hasVisual = GetComponent<SpriteRenderer>() != null
                 || Tools.FindImageInChildCanvas(gameObject) != null;
    }

    /// <summary>
    /// 由 MouseManager 在每帧仲裁后写入几何 hover 状态。
    /// 只更新字段，不 Invoke 业务事件；业务事件由 MouseManager 单独调 MouseOver/MouseOut。
    /// </summary>
    public void SetHoverInternal(bool hovered)
    {
        _isHovered = hovered;
    }

    protected virtual void OnDisable()
    {
        _isHovered = false;
    }

    public abstract void MouseOver();

    public abstract void MouseOut();

    public abstract void MouseSelect();

    public abstract void MouseCancel();

    public abstract void MouseHold();

    public abstract void MouseRelease();
}
