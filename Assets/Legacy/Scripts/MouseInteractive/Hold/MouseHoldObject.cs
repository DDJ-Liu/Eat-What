using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public enum HoldTriggerMode
{
    TimedAutoTrigger,       // 固定时间自动触发：到达holdDuration后自动触发效果
    TimedReleaseTrigger,    // 固定时间松开触发：需要按住达到holdDuration，松开时触发效果
    ManualTrigger,          // 手动触发：由外部脚本调用TriggerHoldEffect()触发
    ContinuousTrigger       // 持续触发：按住期间每帧持续触发效果
}

public class MouseHoldObject : MonoBehaviour
{
    [Header("模式")]
    public HoldTriggerMode holdMode = HoldTriggerMode.TimedAutoTrigger;

    [Header("条件")]
    public ConditionReceiver condition = new ConditionReceiver();

    [Header("设置")]
    [Tooltip("触发所需的按住时间（TimedAutoTrigger和TimedReleaseTrigger模式使用）")]
    public float holdDuration = 1f;
    [Tooltip("松散Hover模式：允许按住时鼠标离开物体区域。\n" +
             "关闭时鼠标移出区域立即取消按住；\n" +
             "开启时鼠标可以自由移出，但松开时鼠标不在区域内则视为取消")]
    public bool looseHoverMode = false;

    [Header("状态")]
    public bool isHolding = false;
    public float holdTime = 0f;
    [Tooltip("按住进度 = holdTime / holdDuration，仅定时模式有效")]
    public float holdProgress = 0f;

    [Header("事件")]
    [Tooltip("按住开始时触发")]
    public UnityEvent holdStartEvent;
    [Tooltip("按住期间每帧触发")]
    public UnityEvent holdUpdateEvent;
    [Tooltip("效果触发事件（根据模式不同在不同时机触发）")]
    public UnityEvent holdCompleteEvent;
    [Tooltip("按住被取消时触发（鼠标移出/松开时不在范围内/未达到所需时间）")]
    public UnityEvent holdCancelEvent;
    [Tooltip("鼠标松开时触发（无论完成还是取消都会触发）")]
    public UnityEvent holdReleaseEvent;

    private bool HasTimedDuration => holdMode == HoldTriggerMode.TimedAutoTrigger
                                  || holdMode == HoldTriggerMode.TimedReleaseTrigger;

    void Update()
    {
        // 每帧刷新条件状态（供外部组件读取 condition.allowToUse）
        if (!condition.RunCheck())
        {
            condition.allowToUse = true;
        }

        if (!isHolding) return;

        holdTime += Time.deltaTime;

        if (HasTimedDuration && holdDuration > 0f)
        {
            holdProgress = Mathf.Clamp01(holdTime / holdDuration);
        }

        holdUpdateEvent?.Invoke();

        switch (holdMode)
        {
            case HoldTriggerMode.TimedAutoTrigger:
                if (holdTime >= holdDuration)
                {
                    holdTime -= holdDuration;
                    holdProgress = 0f;
                    holdCompleteEvent?.Invoke();
                }
                break;
            case HoldTriggerMode.ContinuousTrigger:
                holdCompleteEvent?.Invoke();
                break;
        }
    }

    /// <summary>
    /// 开始按住，由MouseManager在鼠标按下时调用
    /// </summary>
    public void EnterHold()
    {
        if (isHolding) return;

        // 条件检查：有绑定条件且不满足时阻止按住
        if (condition.RunCheck() && !condition.allowToUse)
        {
            return;
        }

        isHolding = true;
        holdTime = 0f;
        holdProgress = 0f;
        holdStartEvent?.Invoke();
    }

    /// <summary>
    /// 松开按住，由MouseManager在鼠标松开时调用
    /// </summary>
    /// <param name="mouseIsOver">松开时鼠标是否仍在物体触发区域内</param>
    public void ReleaseHold(bool mouseIsOver)
    {
        if (!isHolding) return;

        isHolding = false;
        holdReleaseEvent?.Invoke();

        // 松散模式下松开时鼠标不在范围内 → 取消
        if (looseHoverMode && !mouseIsOver)
        {
            holdCancelEvent?.Invoke();
            ResetHoldState();
            return;
        }

        // 正常松开（非松散模式，或松散模式下鼠标在范围内）
        switch (holdMode)
        {
            case HoldTriggerMode.TimedAutoTrigger:
                // 未达到触发时间就松开，视为取消
                holdCancelEvent?.Invoke();
                break;
            case HoldTriggerMode.TimedReleaseTrigger:
                if (holdTime >= holdDuration)
                {
                    holdCompleteEvent?.Invoke();
                }
                else
                {
                    holdCancelEvent?.Invoke();
                }
                break;
            case HoldTriggerMode.ManualTrigger:
                // 手动模式下不自动处理触发
                break;
            case HoldTriggerMode.ContinuousTrigger:
                // 持续模式已在Update中每帧触发
                break;
        }

        ResetHoldState();
    }

    /// <summary>
    /// 取消按住，鼠标在非松散模式下移出触发区域时由MouseManager调用
    /// </summary>
    public void CancelHold()
    {
        if (!isHolding) return;

        isHolding = false;
        holdCancelEvent?.Invoke();
        ResetHoldState();
    }

    /// <summary>
    /// 手动触发效果，供ManualTrigger模式下外部脚本调用
    /// </summary>
    public void TriggerHoldEffect()
    {
        holdCompleteEvent?.Invoke();
    }

    private void ResetHoldState()
    {
        holdTime = 0f;
        holdProgress = 0f;
    }

    public void DebugHoldStart()
    {
        Debug.Log($"[Hold] {gameObject.name}: HoldStart");
    }

    public void DebugHoldUpdate()
    {
        Debug.Log($"[Hold] {gameObject.name}: HoldUpdate (time={holdTime:F2}, progress={holdProgress:F2})");
    }

    public void DebugHoldComplete()
    {
        Debug.Log($"============[Hold] {gameObject.name}: HoldComplete");
    }

    public void DebugHoldCancel()
    {
        Debug.Log($"[Hold] {gameObject.name}: HoldCancel");
    }

    public void DebugHoldRelease()
    {
        Debug.Log($"[Hold] {gameObject.name}: HoldRelease");
    }
}
