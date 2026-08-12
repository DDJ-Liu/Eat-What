using UnityEngine;
using UnityEngine.Events;

public class MouseScrollableObject : MonoBehaviour
{
    // /// <summary>
    // /// 连续滚轮事件：每帧有滚轮输入就触发，传入原始 scrollDelta
    // /// </summary>
    // public UnityEvent<float> scrollEvent;

    /// <summary>
    /// 离散滚轮事件：累积达到阈值后触发，传入 ±1
    /// </summary>
    public UnityEvent<float> scrollStepEvent;

    /// <summary>
    /// Debug 用：输出滚轮事件日志，可通过 Inspector 按钮快速绑定到 scrollStepEvent
    /// </summary>
    public void DebugLogScrollStep(float direction)
    {
        Debug.Log($"[MouseScrollableObject] ScrollStep: {direction}, Object: {gameObject.name}");
    }
}
