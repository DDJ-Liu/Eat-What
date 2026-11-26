using UnityEngine;

/// <summary>
/// 将 KeyboardListener 的按键事件桥接到 MouseHoldObject，
/// 实现键盘按住 = 鼠标在目标对象上按住的效果。
/// 所有引用通过 Inspector 拖拽或代码手动设置，不做自动查找。
/// </summary>
public class KeyboardHoldBridge : MonoBehaviour
{
    [Header("引用（Inspector 拖拽或代码设置）")]
    [SerializeField] private KeyboardListener keyboardListener;
    [SerializeField] private MouseHoldObject holdObject;
    [SerializeField] private Button_MouseInteract buttonInteract; // 可选，用于视觉反馈

    private bool isKeyboardHolding = false;

    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
        if (isKeyboardHolding && holdObject != null)
        {
            holdObject.CancelHold();
            isKeyboardHolding = false;
        }
    }

    /// <summary>
    /// 运行时绑定 KeyboardListener
    /// </summary>
    public void SetKeyboardListener(KeyboardListener listener)
    {
        Unsubscribe();
        keyboardListener = listener;
        if (enabled) Subscribe();
    }

    /// <summary>
    /// 运行时绑定 MouseHoldObject
    /// </summary>
    public void SetHoldObject(MouseHoldObject hold)
    {
        if (isKeyboardHolding && holdObject != null)
        {
            holdObject.CancelHold();
            isKeyboardHolding = false;
        }
        holdObject = hold;
    }

    private void Subscribe()
    {
        if (keyboardListener == null) return;
        keyboardListener.keyPressEvent.AddListener(OnKeyPress);
        keyboardListener.keyReleaseEvent.AddListener(OnKeyRelease);
    }

    private void Unsubscribe()
    {
        if (keyboardListener == null) return;
        keyboardListener.keyPressEvent.RemoveListener(OnKeyPress);
        keyboardListener.keyReleaseEvent.RemoveListener(OnKeyRelease);
    }

    private void OnKeyPress()
    {
        if (holdObject == null || holdObject.isHolding) return;

        if (buttonInteract != null)
            buttonInteract.MouseOver();

        holdObject.EnterHold();
        isKeyboardHolding = true;

        if (keyboardListener != null)
            keyboardListener.consumed = true;
    }

    private void OnKeyRelease()
    {
        if (!isKeyboardHolding || holdObject == null) return;

        // 键盘触发时虚拟光标始终"在对象上"，传 true
        holdObject.ReleaseHold(true);
        isKeyboardHolding = false;

        if (buttonInteract != null)
            buttonInteract.MouseOut();

        if (keyboardListener != null)
            keyboardListener.consumed = true;
    }
}
