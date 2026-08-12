using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance;

    // 键盘按键事件广播（返回 bool：任一订阅者返回 true 表示成功消费）
    public static event Func<Key, bool> OnKeyPressed;
    public static event Func<Key, bool> OnKeyReleased;

    // 按键记录事件
    public UnityEvent OnKeyRecordStart;    // 开始记录时触发，方便外部模块在Inspector中绑定
    public KeyboardListener targetBindingListener;
    public Key RecordedKey { get; private set; }     // 最近一次记录到的按键

    [Header("Input Buffer")]
    public bool useInputBuffer = true;
    [Tooltip("按键缓冲时间（秒）。按下按键时若下游未能处理，将在此时间内每帧重试")]
    public float inputBufferTime = 0.15f;

    private PlayerInput playerInput;
    private InputAction anyKeyAction;

    // 按键记录状态
    private bool isRecordingKey = false;

    // Buffer 协程追踪
    private Dictionary<Key, Coroutine> _pressBuffers = new Dictionary<Key, Coroutine>();
    private Dictionary<Key, Coroutine> _releaseBuffers = new Dictionary<Key, Coroutine>();

    private void Awake()
    {
        // 确保唯一性
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        playerInput = GetComponent<PlayerInput>();
        var gameMap = playerInput.actions.FindActionMap("Game", true);
        anyKeyAction = gameMap.FindAction("KeyboardAnyKey", true);
    }

    private void OnEnable()
    {
        anyKeyAction.performed += OnAnyKeyPerformed;
        anyKeyAction.canceled += OnAnyKeyCanceled;
    }

    private void OnDisable()
    {
        anyKeyAction.performed -= OnAnyKeyPerformed;
        anyKeyAction.canceled -= OnAnyKeyCanceled;

        // 清理所有 buffer 协程
        foreach (var co in _pressBuffers.Values) StopCoroutine(co);
        foreach (var co in _releaseBuffers.Values) StopCoroutine(co);
        _pressBuffers.Clear();
        _releaseBuffers.Clear();
    }

    private void OnAnyKeyPerformed(InputAction.CallbackContext ctx)
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // 按键记录模式：捕获第一个按下的键后退出，不广播常规事件
        if (isRecordingKey)
        {
            foreach (var key in keyboard.allKeys)
            {
                try
                {
                    if (key.wasPressedThisFrame)
                    {
                        Key recordedKey = key.keyCode;

                        // Esc 键取消记录
                        if (recordedKey == Key.Escape)
                        {
                            Debug.Log("[InputManager] 按下 Esc，取消按键记录");
                            isRecordingKey = false;
                            RecordedKey = Key.None;
                            targetBindingListener = null;
                            return;
                        }

                        Debug.Log($"[InputManager] 记录到按键: {recordedKey}");
                        isRecordingKey = false;
                        RecordedKey = recordedKey;
                        BindKeyToListener();
                        Debug.Log($"[InputManager] 已存储记录按键: {RecordedKey}");
                        Debug.Log("[InputManager] 按键记录结束");
                        return;
                    }
                }
                catch (NullReferenceException) { }
            }
            return;
        }

        // 常规按键事件广播（带 buffer）
        foreach (var key in keyboard.allKeys)
        {
            try
            {
                if (key.wasPressedThisFrame)
                {
                    Key k = key.keyCode;
                    bool handled = InvokeKeyEvent(OnKeyPressed, k);
                    if (!handled && useInputBuffer && inputBufferTime > 0f)
                    {
                        if (_pressBuffers.TryGetValue(k, out Coroutine existing))
                            StopCoroutine(existing);
                        _pressBuffers[k] = StartCoroutine(BufferRetry(k, true));
                    }
                    else if (handled && _pressBuffers.ContainsKey(k))
                    {
                        StopCoroutine(_pressBuffers[k]);
                        _pressBuffers.Remove(k);
                    }
                }
            }
            catch (NullReferenceException) { }
        }
    }

    private void OnAnyKeyCanceled(InputAction.CallbackContext ctx)
    {
        // var keyboard = Keyboard.current;
        // if (keyboard == null) return;
        // foreach (var key in keyboard.allKeys)
        // {
        //     if (key.wasReleasedThisFrame)
        //     {
        //         OnKeyReleased?.Invoke(key.keyCode);
        //     }
        // }

        // 遍历 allKeys 并用 try-catch 保护，跳过内部状态未初始化的 key
        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        foreach (var key in keyboard.allKeys)
        {
            try
            {
                if (key.wasReleasedThisFrame)
                {
                    Key k = key.keyCode;
                    bool handled = InvokeKeyEvent(OnKeyReleased, k);
                    if (!handled && useInputBuffer && inputBufferTime > 0f)
                    {
                        if (_releaseBuffers.TryGetValue(k, out Coroutine existing))
                            StopCoroutine(existing);
                        _releaseBuffers[k] = StartCoroutine(BufferRetry(k, false));
                    }
                    else if (handled && _releaseBuffers.ContainsKey(k))
                    {
                        StopCoroutine(_releaseBuffers[k]);
                        _releaseBuffers.Remove(k);
                    }
                }
            }
            catch (NullReferenceException) { }
        }
    }

    // --- 按键记录与绑定 ---

    /// <summary>
    /// 开始记录按键。玩家按下任意键后，结果存储在 RecordedKey 属性中。
    /// </summary>
    public void StartRecordKey(KeyboardListener targetListener)
    {
        this.targetBindingListener = targetListener;
        Debug.Log("[InputManager] 开始记录按键，等待玩家输入...");
        isRecordingKey = true;
        OnKeyRecordStart?.Invoke();
    }

    /// <summary>
    /// 将 RecordedKey 绑定到目标 listener，绑定后清除记录。
    /// </summary>
    public void BindKeyToListener(KeyboardListener listener)
    {
        if (listener == null)
        {
            Debug.LogWarning("[InputManager] BindKeyToListener 失败: listener 为 null");
            return;
        }
        if (RecordedKey == Key.None)
        {
            Debug.LogWarning("[InputManager] BindKeyToListener 失败: 没有已记录的按键");
            return;
        }
        Key oldKey = listener.targetKey;
        listener.targetKey = RecordedKey;
        Debug.Log($"[InputManager] 按键绑定完成: {listener.gameObject.name} 的 targetKey 从 {oldKey} 改为 {RecordedKey}");
        RecordedKey = Key.None;
        Debug.Log("[InputManager] 已清除记录的按键");
    }

    /// <summary>
    /// 将 RecordedKey 绑定到目标 listener，绑定后清除记录。
    /// </summary>
    public void BindKeyToListener()
    {
        if (targetBindingListener == null)
        {
            Debug.LogWarning("[InputManager] BindKeyToListener 失败: listener 为 null");
            return;
        }
        if (RecordedKey == Key.None)
        {
            Debug.LogWarning("[InputManager] BindKeyToListener 失败: 没有已记录的按键");
            return;
        }
        Key oldKey = targetBindingListener.targetKey;
        targetBindingListener.targetKey = RecordedKey;
        Debug.Log($"[InputManager] 按键绑定完成: {targetBindingListener.gameObject.name} 的 targetKey 从 {oldKey} 改为 {RecordedKey}");
        RecordedKey = Key.None;
        targetBindingListener = null;
        Debug.Log("[InputManager] 已清除记录的按键");
    }

    // --- 鼠标回调（由 PlayerInput 组件通过 SendMessages 调用） ---

    void OnMouseLeftButtonPress()
    {
        MouseManager.Instance.OnMouseConfirmPress();
    }

    void OnMouseLeftButtonRelease()
    {
        MouseManager.Instance.OnMouseLeftButtonRelease();
    }

    void OnMouseRightButtonPress()
    {
        MouseManager.Instance.OnMouseCancelPress();
    }

    // --- Input Buffer ---

    /// <summary>
    /// 遍历 Func 多播委托的 invocation list，任一返回 true 即视为成功消费。
    /// </summary>
    private static bool InvokeKeyEvent(Func<Key, bool> handler, Key key)
    {
        if (handler == null) return false;
        foreach (Func<Key, bool> d in handler.GetInvocationList())
        {
            if (d(key)) return true;
        }
        return false;
    }

    /// <summary>
    /// Buffer 协程：每帧重试事件广播，直到成功或超时。
    /// </summary>
    private IEnumerator BufferRetry(Key key, bool isPress)
    {
        var dict = isPress ? _pressBuffers : _releaseBuffers;
        float elapsed = 0f;

        while (elapsed < inputBufferTime)
        {
            yield return null;
            elapsed += Time.deltaTime;

            // 按键记录模式下跳过重试
            if (isRecordingKey) continue;

            var evt = isPress ? OnKeyPressed : OnKeyReleased;
            if (InvokeKeyEvent(evt, key))
            {
                dict.Remove(key);
                yield break;
            }
        }

        // Buffer 超时，移除条目
        dict.Remove(key);
    }
}