using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public abstract class Button_Logic : MonoBehaviour, IButtonComponent
{
    /// <summary>
    /// 若未手动设置，将会自动查找同一GameObject上的Button_MouseInteract.
    /// </summary>
    [Tooltip("若未手动设置，将会自动查找同一GameObject上的Button_MouseInteract.")]public Button_MouseInteract button;
    public bool isPressing = false;

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

        if (button != null && !button.logicComponents.Contains(this))
        {
            button.logicComponents.Add(this);
        }
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
