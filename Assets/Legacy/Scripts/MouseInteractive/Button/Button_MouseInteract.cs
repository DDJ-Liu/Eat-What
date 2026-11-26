using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Button_MouseInteract : MousePressableObject
{
    public bool canRelease;
    public bool canHold;
    [Header("组件")]
    public List<Button_Visual> visualComponents = new List<Button_Visual>();
    public List<Button_Logic> logicComponents = new List<Button_Logic>();
    [Header("条件")]
    public ConditionReceiver condition = new ConditionReceiver();
    public bool allowToUse { get => condition.allowToUse; set => condition.allowToUse = value; }
    [Tooltip("默认否，一般情况下不需要动，当按钮的条件判断为否时仍然需要执行点击方法时勾选")]
    public bool weakCondition = false;
    public UnityEvent<bool> conditonResultEvent;
    [Tooltip("严格可互动检查，当需要按钮在不可互动时禁用高亮则勾选此项，默认为否")]
    public bool preventHighlightOnFalseCondition = false;

    [Header("点击")]
    [SerializeField, Tooltip("此项用于按钮按下事件需要等待动画播放到一定程度时才触发的情况，将下面的事件留空")]
    public UnityEvent delayedSelectEvent;
    public UnityEvent selectEvent;
    // 触发条件达成，按压动画播放中。TriggerHold/MouseSelect 设为 true，
    // selectBuffer 协程结束后设为 false。在 HoldButton 中防止动画未结束时重复触发
    [SerializeField] protected bool isPressing = false;

    [Header("Hold")]
    protected MouseHoldObject holdObject;

    [Header("键盘关联")]
    [SerializeField] private KeyboardListener linkedKeyboardListener;

    [Header("高亮")]
    public UnityEvent overEvent;
    public UnityEvent outEvent;
    [HideInInspector] public bool externalHighlightLock = false;

    protected virtual void Awake()
    {
        holdObject = GetComponent<MouseHoldObject>();
        if (holdObject != null)
        {
            holdObject.holdCancelEvent.AddListener(OnHoldCancel);
        }

        if (linkedKeyboardListener == null)
            linkedKeyboardListener = GetComponent<KeyboardListener>();
    }

    protected override void Start()
    {
        base.Start(); // 初始化 _hoverCollider
    }

    private void Update()
    {
        if (!condition.RunCheck())
        {
            allowToUse = true;
        }

        conditonResultEvent?.Invoke(allowToUse);
    }

    public override void MouseCancel()
    {
        if (!isPressing)
        {
            outEvent?.Invoke();
        }
    }

    public override void MouseOver()
    {
        if (!isPressing)
        {
            overEvent?.Invoke();
        }
    }

    public override void MouseOut()
    {
        if (!isPressing && !externalHighlightLock)
        {
            outEvent?.Invoke();
        }
    }

    public override void MouseSelect()
    {
        if (holdObject != null) return;
        ExecuteSelect();
    }

    /// <summary>
    /// 外部组件调用，直接触发 button 的 select 行为（绕过 MouseManager）
    /// </summary>
    public void TriggerSelect()
    {
        ExecuteSelect();
    }

    protected void ExecuteSelect()
    {
        if (isPressing) return;

        if (condition.RunCheck())
        {
            if (!allowToUse && !weakCondition)
            {
                Debug.Log("Button condition failed");
                return;
            }
        }

        if (linkedKeyboardListener != null)
            linkedKeyboardListener.consumed = true;

        isPressing = true;
        MouseOut();
        //Debug.Log($"[Button] {gameObject.name} ExecuteSelect: invoking selectEvent");
        selectEvent?.Invoke();
        // 打印各组件初始状态
        /*foreach (var c in visualComponents)
            Debug.Log($"[Button] {gameObject.name} visual [{c.GetType().Name}] after selectEvent: isPressing={c.isPressing}");
        foreach (var c in logicComponents)
            Debug.Log($"[Button] {gameObject.name} logic [{c.GetType().Name}] after selectEvent: isPressing={c.isPressing}");*/

        // selectEvent 的监听器可能已将此对象设为 inactive，此时无法启动协程
        if (!gameObject.activeInHierarchy)
        {
            isPressing = false;
            return;
        }
        StartCoroutine(selectBuffer());
    }

    public override void MouseHold()
    {
        if (!canHold) return;
        else MouseSelect();
    }

    public override void MouseRelease()
    {
        if (!canRelease) return;
        else MouseSelect();
    }

    protected virtual void OnDestroy() { }

    protected virtual IEnumerator selectBuffer()
    {
        bool allDone = false;
        float timer = 0f;
        while (!allDone)
        {
            if (timer >= 1f)
            {
                Debug.LogWarning($"[Button] selectBuffer timeout on {gameObject.name}, force releasing isPressing.");
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
            if (allDone)
            {
                foreach (var component in logicComponents)
                {
                    if (component.isPressing)
                    {
                        allDone = false;
                        break;
                    }
                }
            }
            timer += Time.deltaTime;
            yield return null;
        }

        /*Debug.Log($"[Button] {gameObject.name} selectBuffer: all isPressing done (timer={timer:F3}s)");
        // 打印各组件最终状态
        foreach (var c in visualComponents)
            Debug.Log($"[Button] {gameObject.name} visual [{c.GetType().Name}] final: isPressing={c.isPressing}");
        foreach (var c in logicComponents)
            Debug.Log($"[Button] {gameObject.name} logic [{c.GetType().Name}] final: isPressing={c.isPressing}");

        // 统一触发 delayedSelectEvent
        Debug.Log($"[Button] {gameObject.name} selectBuffer: invoking delayedSelectEvent");*/
        delayedSelectEvent?.Invoke();

        isPressing = false;

        // delayedSelectEvent 可能导致此按钮被关闭，不再补发事件
        if (!gameObject.activeInHierarchy)
            yield break;

        // isPressing 期间可能错过了 hover 状态变化，补发对应事件
        if (_isHovered)
        {
            MouseOver();
        }
        else
        {
            MouseOut();
        }
    }
    public void delayedMouseSelect()
    {
        delayedSelectEvent?.Invoke();
        if(!_isHovered)
        {
            Debug.Log("123");
            MouseOut();
        }
        else
        {
            Debug.Log("456");
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        StopAllCoroutines();
        isPressing = false;
        if (holdObject != null)
        {
            holdObject.holdCancelEvent.RemoveListener(OnHoldCancel);
        }
    }

    protected virtual void OnHoldCancel()
    {
        MouseCancel();
        MouseOut();
    }

    /// <summary>
    /// Put A Manual Condition Notice in Event Window
    /// </summary>
    public void ManualCondition(ConditionReceiver receiver)
    {
        return;
    }

    /// <summary>
    /// for animation
    /// </summary>
    public void resetSelectedLock()
    {
        isPressing = false;
    }

    public void setConditionManual(bool value)
    {
        allowToUse = value;
    }
}
