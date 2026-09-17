using UnityEngine;
using UnityEngine.Events;

public class MouseScrollableObject : MonoBehaviour
{
    [Header("Pressable overlap policy")]
    [SerializeField] private bool allowScrollThroughPressables = false;
    [SerializeField] private Transform pressablePassThroughScope = null;

    // /// <summary>
    // /// 连续滚轮事件：每帧有滚轮输入就触发，传入原始 scrollDelta
    // /// </summary>
    // public UnityEvent<float> scrollEvent;

    /// <summary>
    /// 离散滚轮事件：累积达到阈值后触发，传入 ±1
    /// </summary>
    public UnityEvent<float> scrollStepEvent;

    public bool AllowScrollThroughPressables { get { return allowScrollThroughPressables; } }
    public Transform PressablePassThroughScope { get { return pressablePassThroughScope; } }

    /// <summary>
    /// Allows wheel targeting through a Pressable only when this target and that Pressable are
    /// both inside the explicitly assigned scope. The default remains blocking.
    /// </summary>
    public bool CanScrollThroughPressable(MousePressableObject pressable)
    {
        if (!allowScrollThroughPressables || pressablePassThroughScope == null || pressable == null)
            return false;

        return IsInScope(transform, pressablePassThroughScope) &&
            IsInScope(pressable.transform, pressablePassThroughScope);
    }

    private static bool IsInScope(Transform value, Transform scope)
    {
        return value != null && scope != null && (value == scope || value.IsChildOf(scope));
    }

    /// <summary>
    /// Debug 用：输出滚轮事件日志，可通过 Inspector 按钮快速绑定到 scrollStepEvent
    /// </summary>
    public void DebugLogScrollStep(float direction)
    {
        Debug.Log($"[MouseScrollableObject] ScrollStep: {direction}, Object: {gameObject.name}");
    }
}
