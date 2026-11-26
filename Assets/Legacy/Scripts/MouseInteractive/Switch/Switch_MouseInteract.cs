using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Switch_MouseInteract : Button_MouseInteract
{
    [Header("Switch 状态")]
    [SerializeField] private int currentStateIndex = 0;
    [SerializeField] protected int stateCount = 2;

    [Header("Switch 事件")]
    public UnityEvent<int> onStateChanged;

    public int CurrentStateIndex => currentStateIndex;
    public int StateCount => stateCount;

    /// <summary>动画播放期间的目标状态，动画结束后才正式生效</summary>
    public int PendingStateIndex => _pendingStateIndex;
    private int _pendingStateIndex = 0;

    public override void MouseSelect()
    {
        if (isPressing) return;

        if (condition.RunCheck())
        {
            if (!allowToUse && !weakCondition)
            {
                Debug.Log("Switch condition failed");
                return;
            }
        }

        // 仅计算目标状态，不立即切换 —— 等动画播放完成后再正式切换
        _pendingStateIndex = (currentStateIndex + 1) % stateCount;

        isPressing = true;
        MouseOut();
        selectEvent?.Invoke();
        StartCoroutine(selectBuffer());
    }

    protected override IEnumerator selectBuffer()
    {
        bool allDone = false;
        float timer = 0f;
        while (!allDone)
        {
            if (timer >= 1f)
            {
                Debug.LogWarning($"[Switch] selectBuffer timeout on {gameObject.name}, force releasing isPressing.");
                break;
            }
            allDone = true;
            foreach (var component in visualComponents)
            {
                if (component.isPressing)
                {
                    allDone = false;
                    break;
                }
            }
            timer += Time.deltaTime;
            yield return null;
        }

        // 统一触发 delayedSelectEvent
        delayedSelectEvent?.Invoke();

        // 动画播放完毕，正式切换状态
        currentStateIndex = _pendingStateIndex;
        onStateChanged?.Invoke(currentStateIndex);
        isPressing = false;
    }

    /// <summary>
    /// 程序化设置状态，不播放动画，直接跳转视觉
    /// </summary>
    public void SetState(int index)
    {
        if (index < 0 || index >= stateCount)
        {
            Debug.LogWarning($"[Switch] SetState: index {index} 超出范围 [0, {stateCount - 1}]");
            return;
        }
        currentStateIndex = index;
        _pendingStateIndex = index;
        onStateChanged?.Invoke(currentStateIndex);
    }

    public int GetState() => currentStateIndex;
}
