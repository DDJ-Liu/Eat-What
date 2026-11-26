using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Switch 系列视觉组件的抽象基类。
/// 在 Button_Visual 的基础上新增状态感知接口。
/// 子类需同时实现带 stateIndex 参数的抽象方法，以在不同状态下呈现不同视觉。
/// </summary>
public abstract class Switch_Visual : Button_Visual
{
    /// <summary>关联的 Switch_MouseInteract 组件（Start 时自动获取）</summary>
    protected Switch_MouseInteract switchButton;

    protected override void Start()
    {
        base.Start(); // 自动注册 overEvent/outEvent/selectEvent，并设置 button 引用

        switchButton = GetComponent<Switch_MouseInteract>();
        if (switchButton == null)
        {
            Debug.LogError($"[Switch_Visual] {gameObject.name} 上未找到 Switch_MouseInteract 组件！");
            return;
        }

        // 监听状态变化，在程序化切换时同步刷新视觉
        //switchButton.onStateChanged.AddListener(OnStateChanged);

        // 初始化视觉到当前状态
        setIdle();
    }

    // ── 覆盖基类的无参方法，委托给带状态参数的版本 ──

    public override void setHighlight()
    {
        if (!enabled || switchButton == null) return;
        setHighlight(switchButton.CurrentStateIndex);
    }

    public override void setIdle()
    {
        if (!enabled || switchButton == null) return;
        setIdle(switchButton.CurrentStateIndex);
    }

    public override void OnPress()
    {
        if (!enabled || switchButton == null) return;
        OnPress(switchButton.CurrentStateIndex, switchButton.PendingStateIndex);
    }

    // ── 子类必须实现的状态感知抽象方法 ──

    /// <summary>鼠标悬停时，根据 stateIndex 显示对应的高亮视觉</summary>
    public abstract void setHighlight(int stateIndex);

    /// <summary>鼠标离开时，根据 stateIndex 显示对应的空闲视觉</summary>
    public abstract void setIdle(int stateIndex);

    /// <summary>按下动画；currentStateIndex 为当前状态，targetStateIndex 为动画结束后将切换到的目标状态</summary>
    public abstract void OnPress(int currentStateIndex, int targetStateIndex);

    // ── 状态变化回调（程序化 SetState 时调用）──

    /// <summary>
    /// 当 Switch 状态通过 SetState() 程序化改变时调用。
    /// 默认行为：若未在按压动画中，刷新视觉到新状态的 Idle。
    /// 子类可按需覆盖。
    /// </summary>
    public virtual void OnStateChanged(int newState)
    {
        if (!enabled) return;
        if (!isPressing)
        {
            setIdle(newState);
        }
    }
}
