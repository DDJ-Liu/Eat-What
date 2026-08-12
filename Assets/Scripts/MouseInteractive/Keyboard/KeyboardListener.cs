using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class KeyboardListener : MonoBehaviour
{
    public Key targetKey;

    public UnityEvent keyPressEvent;
    public UnityEvent keyReleaseEvent;

    /// <summary>
    /// 下游消费者在事件处理方法中将此标志设为 true，表示成功消费了输入。
    /// KeyboardListener 在每次 Invoke 前重置为 false。
    /// </summary>
    [HideInInspector] public bool consumed = false;

    private void OnEnable()
    {
        InputManager.OnKeyPressed += HandleKeyPressed;
        InputManager.OnKeyReleased += HandleKeyReleased;
    }

    private void OnDisable()
    {
        InputManager.OnKeyPressed -= HandleKeyPressed;
        InputManager.OnKeyReleased -= HandleKeyReleased;
    }

    private bool HandleKeyPressed(Key key)
    {
        if (key != targetKey) return false;
        consumed = false;
        keyPressEvent?.Invoke();
        return consumed;
    }

    private bool HandleKeyReleased(Key key)
    {
        if (key != targetKey) return false;
        consumed = false;
        keyReleaseEvent?.Invoke();
        return consumed;
    }

    // --- Debug 方法，可在 Inspector 中通过 UnityEvent 绑定 ---

    public void DebugKeyPress()
    {
        Debug.Log($"[KeyboardListener] 按键按下: {targetKey} ({gameObject.name})");
    }

    public void DebugKeyRelease()
    {
        Debug.Log($"[KeyboardListener] 按键释放: {targetKey} ({gameObject.name})");
    }

    public void bindMyKey()
    {
        if(InputManager.Instance == null)
        {
            Debug.Log("Where is your input manager?");
            return;
        }

        InputManager.Instance.StartRecordKey(this);
    }
}
