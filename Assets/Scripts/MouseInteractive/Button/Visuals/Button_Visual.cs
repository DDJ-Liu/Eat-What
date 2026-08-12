using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public abstract class Button_Visual : MonoBehaviour, IButtonComponent
{
    /// <summary>
    /// 若未手动设置，将会自动查找同一GameObject上的Button_MouseInteractive.
    /// </summary>
    [Tooltip("若未手动设置，将会自动查找同一GameObject上的Button_MouseInteractive.")]public Button_MouseInteract button;
    public bool isPressing = false;

    /// <summary>
    /// 标记此 Visual 被 HoverTrigger 使用。
    /// 为 true 时，跳过 button.allowToUse 检查，视为总是可用。
    /// </summary>
    [HideInInspector]
    public bool isHoverMode = false;

    // 显式接口实现，桥接字段到接口属性
    Button_MouseInteract IButtonComponent.button { get => button; set => button = value; }
    bool IButtonComponent.isPressing { get => isPressing; set => isPressing = value; }
    void IButtonComponent.setHighlight() { if (enabled) setHighlight(); }
    void IButtonComponent.setIdle()      { if (enabled) setIdle(); }
    void IButtonComponent.OnPress()      { if (enabled) OnPress(); }

    protected virtual void Start()
    {
        if(button == null)
        {
            button = GetComponent<Button_MouseInteract>();
        }

        if (button != null && !button.visualComponents.Contains(this))
        {
            button.visualComponents.Add(this);
        }
    }

    /// <summary>
    /// 检查是否允许使用（考虑 HoverMode）。
    /// 子类应该使用此方法代替直接访问 button.allowToUse。
    /// </summary>
    protected bool IsAllowToUse()
    {
        if (isHoverMode) return true;  // HoverMode 下总是允许
        if (button == null) return false;  // 无 button 引用时不允许
        return button.allowToUse;
    }

    /// <summary>
    /// When button has mouse over or other same condition
    /// </summary>
    public abstract void setHighlight();
    /// <summary>
    /// When button is in Normal state, this should contain condition check.
    /// </summary>
    public abstract void setIdle();

    public abstract void OnPress();

}
