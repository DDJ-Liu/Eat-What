using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 复选框控件，继承 Switch（二态）并封装勾选语义。
/// 视觉层直接复用 Switch_Visual 系列组件（state 0 = 未勾选, state 1 = 勾选）。
/// </summary>
public class Checkbox_MouseInteract : Switch_MouseInteract
{
    /// <summary>当前是否勾选（state 1 = 勾选）</summary>
    public bool isChecked => CurrentStateIndex == 1;

    [Header("Checkbox 事件")]
    public UnityEvent<bool> onCheckedChanged;

    protected override void Awake()
    {
        // 强制锁定为二态
        stateCount = 2;
        base.Awake();
    }

    protected override void Start()
    {
        onStateChanged.AddListener(OnSwitchStateChanged);
        base.Start();
    }

    private void OnSwitchStateChanged(int stateIndex)
    {
        onCheckedChanged?.Invoke(stateIndex == 1);
    }

    /// <summary>程序化设置勾选状态，不播放动画</summary>
    public void SetChecked(bool value)
    {
        SetState(value ? 1 : 0);
    }

    /// <summary>切换勾选状态（带动画）</summary>
    public void Toggle()
    {
        TriggerSelect();
    }
}
