using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 两状态锁定执行器：HoverZone 触发后锁定自身，外部调用 Wake() 恢复。
/// </summary>
public class ToggleLockHoverZone_Logic : HoverZone_Logic
{
    private enum State { Active, Locked }

    [Header("状态事件")]
    public UnityEvent onLocked;
    public UnityEvent onWake;

    [Header("调试")]
    [SerializeField] private State currentState = State.Active;

    public override void OnHoverEnter() { }

    public override void OnHoverExit() { }

    public override void OnTrigger()
    {
        if (currentState != State.Active) return;

        currentState = State.Locked;
        if (hoverZone != null)
            hoverZone.enabled = false;
        onLocked?.Invoke();
    }

    /// <summary>
    /// 唤醒方法，由外部代码或 UnityEvent 调用以恢复 HoverZone
    /// </summary>
    public void Wake()
    {
        if (currentState != State.Locked) return;

        currentState = State.Active;
        if (hoverZone != null)
            hoverZone.enabled = true;
        onWake?.Invoke();
    }
}
