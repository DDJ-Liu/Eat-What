using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class MouseManager : MonoBehaviour
{
    #region 事件
    public static event Action OnMouseConfirm;
    public static event Action OnMouseCancel;
    public static event Action OnMouseRelease;
    public static event Action<Vector3> OnDragStarted;
    public static event Action OnDragPerformed;
    public static event Action<float> OnScrollStep;
    #endregion

    public static MouseManager Instance;
    // public static event System.Action<float> OnScrollEvent;
    // public static event System.Action<float> OnScrollContinuousEvent;

    [TextArea, SerializeField] private string currentLayer;

    private void Awake()
    {
        // ȷ������Ψһ��
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        playerInput = InputManager.Instance.GetComponent<PlayerInput>();
        InputActionMap gameMap = playerInput.actions.FindActionMap("Game", true);
        dragStartAction = gameMap.FindAction("MouseLeftButtonHoldStart", true);
    }

    private PlayerInput playerInput;

    [Header("MousePress")]
    public Stack<MouseInteractionLayer> mouseInteractiveLayers = new Stack<MouseInteractionLayer>();
    public MouseInteractionLayer currentMouseLayer
    {
        get { return mouseInteractiveLayers.Count > 0? mouseInteractiveLayers.Peek() : null; }
    }

    [Header("MouseDrag")]
    public MouseDraggableObject currentDraggableObject;
    private InputAction dragStartAction;
    public bool dragStarted = false;
    public bool dragPerforming = false;
    public Vector3 dragStartPos;
    public float dragMoveThreshold;
    private bool pointerCaptured;
    private MouseDraggableObject pressedDraggable;
    private MousePressableObject pressedPressable;
    private MousePressableObject releaseClickTarget;
    private MouseInteractionLayer pressedLayer;

    [Header("MouseHold")]
    public MouseHoldObject currentHoldObject;
    public bool holdPerforming = false;

    [Header("MouseScroll")]
    public MouseScrollableObject currentScrollableObject;

    [Header("MousePressHover")]
    public MousePressableObject currentPressableHoverTarget;
    private MousePressableObject _prevPressableHoverTarget;

    [Header("HoverTrigger Registry")]
    public List<HoverTrigger> currentHoverTriggers = new List<HoverTrigger>();

    public void RegisterHoverTrigger(HoverTrigger t)
    {
        if (t != null && !currentHoverTriggers.Contains(t))
            currentHoverTriggers.Add(t);
    }

    public void UnregisterHoverTrigger(HoverTrigger t)
    {
        if (t != null) currentHoverTriggers.Remove(t);
    }

    [Header("MouseWheelScroll")]
    [SerializeField] private float scrollThreshold = 1.0f;
    [SerializeField] private float accumulatedScroll = 0f;
    [SerializeField] private float lastScrollTime;
    [SerializeField] private float scrollCooldown = 0.2f;
    private MouseScrollableObject scrollInputTarget;
    private MouseInteractionLayer scrollInputLayer;

    public float ScrollThreshold { get { return scrollThreshold; } }
    public float AccumulatedScroll { get { return accumulatedScroll; } }
    public float LastScrollTime { get { return lastScrollTime; } }
    public float ScrollCooldown { get { return scrollCooldown; } }
    public MouseScrollableObject LastScrollableCandidate { get; private set; }
    public MousePressableObject LastScrollableBlocker { get; private set; }
    public string LastScrollResolutionReason { get; private set; } = "not_sampled";
    public int LastScrollResolutionFrame { get; private set; } = -1;
    public int LastScrollDispatchFrame { get; private set; } = -1;
    public int ScrollDispatchSequence { get; private set; }
    public float LastRawScrollY { get; private set; }
    public float LastDispatchedStep { get; private set; }
    public Vector2 LastScrollPointerWorld { get; private set; }
    public bool HasUnsafeEmptyLayerUpdatePath
    {
        get { return false; }
    }

    public void ConfigureScrollInput(float threshold, float cooldown)
    {
        if (float.IsNaN(threshold) || float.IsInfinity(threshold) || threshold <= 0f)
            throw new ArgumentOutOfRangeException(nameof(threshold), "Scroll threshold must be finite and greater than zero.");
        if (float.IsNaN(cooldown) || float.IsInfinity(cooldown) || cooldown < 0f)
            throw new ArgumentOutOfRangeException(nameof(cooldown), "Scroll cooldown must be finite and non-negative.");

        scrollThreshold = threshold;
        scrollCooldown = cooldown;
        ResetScrollInput();
    }

    // Start is called before the first frame update
    void Start()
    {

    }
    private void OnEnable()
    {
        ResetScrollInput();
        dragStartAction.Enable();
        // ���� Hold ������ Started �׶�
        dragStartAction.started += OnDragStart;
    }

    private void OnDisable()
    {
        CancelPointerGesture();
        ResetScrollInput();
        if (dragStartAction != null) dragStartAction.Disable();
        // ȡ������
        if (dragStartAction != null) dragStartAction.started -= OnDragStart;
    }
    // Update is called once per frame
    void Update()
    {
        if (mouseInteractiveLayers == null || mouseInteractiveLayers.Count == 0)
        {
            CancelPointerGesture();
            ResetScrollInput();
            currentLayer = string.Empty;
            currentScrollableObject = null;
            LastScrollableCandidate = null;
            LastScrollableBlocker = null;
            LastScrollResolutionReason = "interaction_layer_stack_empty";
            LastScrollResolutionFrame = Time.frameCount;
            if (_prevPressableHoverTarget != null)
            {
                _prevPressableHoverTarget.SetHoverInternal(false);
                _prevPressableHoverTarget.MouseOut();
                _prevPressableHoverTarget = null;
            }
            currentPressableHoverTarget = null;
            return;
        }
        currentLayer = mouseInteractiveLayers.Peek().gameObject.name;
        Vector3 mouseWorldPosition;
        if (!Tools.TryGetMousePos(out mouseWorldPosition)) return;

        #region �����ͣ���
        if (mouseInteractiveLayers.Count > 0)
        {
            setMouseHover(mouseWorldPosition);
            if (pointerCaptured && pressedLayer != currentMouseLayer) CancelPointerGesture();
            if (CapturedDragInvalid()) CancelPointerGesture();

            if (dragStarted)
            {
                if (Vector3.Distance(dragStartPos, mouseWorldPosition) > dragMoveThreshold)
                {
                    dragStarted = false;
                    dragPerforming = true;
                    OnDragPerform();
                }
            }

            OnScroll(mouseWorldPosition);
        }
        #endregion
    }

    private void setMouseHover(Vector2 mousePos)
    {
        // layer 失效保护：切层瞬间清掉上一帧 Pressable 的 hover 状态
        if (currentMouseLayer == null)
        {
            if (_prevPressableHoverTarget != null)
            {
                _prevPressableHoverTarget.SetHoverInternal(false);
                _prevPressableHoverTarget.MouseOut();
                _prevPressableHoverTarget = null;
            }
            currentPressableHoverTarget = null;
            LastScrollableCandidate = null;
            LastScrollableBlocker = null;
            LastScrollResolutionReason = "interaction_layer_missing";
            LastScrollResolutionFrame = Time.frameCount;
            return;
        }

        //Collider2D hit = null;
        Collider2D[] hits = null;
        // 按 alloweInteractionLayers 列表顺序遍历，先命中的 LayerMask 优先级更高
        for (int i = 0; i < currentMouseLayer.alloweInteractionLayers.Count; i++)
        {
            hits = Physics2D.OverlapPointAll(mousePos, currentMouseLayer.alloweInteractionLayers[i]);
            if (hits.Length == 0) continue;
            //hit = hits.Length == 1 ? hits[0] : SelectHighestPriority(hits);
            break;
        }

        //SetDraggable
        // 只在非拖拽状态下才进行拖拽对象的检测和更新
        if (!dragStarted && !dragPerforming)
        {
            MouseDraggableObject hitDraggable = null;
            if (hits != null && hits.Length > 0)
            {
                hitDraggable = tryGetMouseObject<MouseDraggableObject>(hits);
            }
            // 交互范围检查：如果候选拖拽对象设置了交互边界，鼠标必须在边界内
            if (hitDraggable != null && !hitDraggable.IsPointInInteractionBounds(mousePos))
                hitDraggable = null;
            if (hitDraggable != null && IsBlockedByPressable(hits, hitDraggable.GetComponent<Collider2D>()))
                hitDraggable = null;

            currentDraggableObject = /*hit?.gameObject.GetComponent<MouseDraggableObject>();*/hitDraggable;
        }

        //SetHoldable
        MouseHoldObject hitHoldable = null;
        if (hits != null && hits.Length > 0)
        {
            hitHoldable = tryGetMouseObject<MouseHoldObject>(hits);
        }
        if (!holdPerforming)
        {
            currentHoldObject = /*hit?.gameObject.GetComponent<MouseHoldObject>();*/hitHoldable;
        }
        else if (currentHoldObject != null && !currentHoldObject.looseHoverMode)
        {
            // 非松散模式下，检查鼠标是否仍在按住物体上
            MouseHoldObject hitHold = /*hit?.gameObject.GetComponent<MouseHoldObject>();*/hitHoldable;
            if (hitHold != currentHoldObject)
            {
                currentHoldObject.CancelHold();
                holdPerforming = false;
            }
        }

        //SetScrollable
        MouseScrollableObject hitScrollable = null;
        if (hits != null && hits.Length > 0)
        {
            hitScrollable = tryGetMouseObject<MouseScrollableObject>(hits);
        }
        LastScrollableCandidate = hitScrollable;
        MousePressableObject passedPressable = null;
        LastScrollableBlocker = hitScrollable == null ? null : FindBlockingPressable(
            hits, hitScrollable.GetComponent<Collider2D>(), hitScrollable, out passedPressable);
        if (LastScrollableBlocker != null)
        {
            LastScrollResolutionReason = "pressable_blocked";
            hitScrollable = null;
        }
        else if (passedPressable != null)
        {
            LastScrollableBlocker = passedPressable;
            LastScrollResolutionReason = "pressable_pass_through";
        }
        else LastScrollResolutionReason = hitScrollable == null ? "no_scrollable_candidate" : "target_selected";
        LastScrollResolutionFrame = Time.frameCount;
        currentScrollableObject = /*hit?.gameObject.GetComponent<MouseScrollableObject>();*/hitScrollable;

        //SetPressableHover
        MousePressableObject hitPressable = null;
        if (hits != null && hits.Length > 0)
        {
            hitPressable = tryGetMouseObject<MousePressableObject>(hits);
        }
        currentPressableHoverTarget = hitPressable;

        if (_prevPressableHoverTarget != currentPressableHoverTarget)
        {
            if (_prevPressableHoverTarget != null)
            {
                _prevPressableHoverTarget.SetHoverInternal(false);
                _prevPressableHoverTarget.MouseOut();
            }
            if (currentPressableHoverTarget != null)
            {
                currentPressableHoverTarget.SetHoverInternal(true);
                currentPressableHoverTarget.MouseOver();
            }
            _prevPressableHoverTarget = currentPressableHoverTarget;
        }
    }

    private T tryGetMouseObject<T>(Collider2D[] cols) where T : Behaviour
    {
        if(cols == null || cols.Length == 0)
        {
            //Debug.Log("No cols");
            return default(T);
        }
        List<Collider2D> possibles = new List<Collider2D>();
        foreach(Collider2D col in cols)
        {
            if (col == null || !col.enabled || !col.gameObject.activeInHierarchy) continue;
            var candidate = col.gameObject.GetComponent<T>();
            if (candidate == null || !candidate.isActiveAndEnabled) continue;
            // Co-located drag/click components share the authored interaction gate.
            var pressable = col.GetComponent<MousePressableObject>();
            if (candidate is MouseDraggableObject && pressable != null && !pressable.isActiveAndEnabled) continue;
            if(candidate != null)
            {
                possibles.Add(col);
            }
        }

        if(possibles.Count <= 0)
        {
            //Debug.Log("No Possibles");
            return default(T);
        }

        Collider2D target = SelectHighestPriority(possibles.ToArray());
        return target.GetComponent<T>();
    }

    /// <summary>
    /// 从多个命中的Collider中选出最高优先级的。
    /// 优先级规则（高→低）：
    /// 1. 有 HiddenButtonIdentifier → 最高
    /// 2. 有视觉组件（SpriteRenderer 或子Canvas下的Image）→ 优先于没有的
    /// 3. SortingLayer 值更大 → 优先（仅 SpriteRenderer）
    /// 4. Order in Layer 更大 → 优先（仅 SpriteRenderer）
    /// </summary>
    private static Collider2D SelectHighestPriority(Collider2D[] hits)
    {
        if(hits == null || hits.Length <= 0) return null;
        Vector3 pointer;
        var hasPointer = Tools.TryGetMousePos(out pointer);
        System.Array.Sort(hits, (a, b) =>
        {
            // 规则1: HiddenButtonIdentifier 最高优先
            bool aHidden = a.GetComponent<HiddenButtonIdentifier>() != null;
            bool bHidden = b.GetComponent<HiddenButtonIdentifier>() != null;
            if (aHidden != bHidden) return aHidden ? -1 : 1;

            // 规则2: 有视觉组件（SpriteRenderer 或子Canvas下的Image）优先于没有
            int aLayer, aOrder, bLayer, bOrder;
            bool aHasVisual = TryGetVisualSorting(a, out aLayer, out aOrder);
            bool bHasVisual = TryGetVisualSorting(b, out bLayer, out bOrder);
            if (aHasVisual != bHasVisual) return aHasVisual ? -1 : 1;

            // 都没有视觉组件，视为同优先级
            if (!aHasVisual) return 0;

            // 规则3: Sorting Layer 值更大的优先（仅 SpriteRenderer）
            if (aLayer != bLayer) return bLayer.CompareTo(aLayer);
            if (aOrder != bOrder) return bOrder.CompareTo(aOrder);

            // Only explicit providers in the same product scope resolve an exact
            // visual-rank tie. Pressable blocking calls this same comparator.
            var aHit = a.GetComponent<IMouseHitDepth>();
            var bHit = b.GetComponent<IMouseHitDepth>();
            var scope = aHit == null ? null : aHit.MouseHitScope;
            float aDepth, bDepth;
            if (hasPointer && scope != null && bHit != null && object.ReferenceEquals(scope,bHit.MouseHitScope) &&
                aHit.TryGetMouseHitDepth(pointer,out aDepth) && bHit.TryGetMouseHitDepth(pointer,out bDepth))
            {
                var difference = bDepth.CompareTo(aDepth);
                if (difference != 0) return difference;
                difference = StringComparer.Ordinal.Compare(aHit.MouseHitKey,bHit.MouseHitKey);
                if (difference != 0) return difference;
                return a.gameObject.GetInstanceID().CompareTo(b.gameObject.GetInstanceID());
            }

            return 0;
        });

        return hits[0];
    }

    private static bool TryGetVisualSorting(Collider2D collider, out int layer, out int order)
    {
        layer = 0; order = 0;
        SpriteRenderer visual = null;
        SortingGroup group = null;
        for (var node = collider.transform; node != null; node = node.parent)
        {
            var candidate = node.GetComponent<SpriteRenderer>();
            if (visual == null && candidate != null && candidate.enabled && candidate.gameObject.activeInHierarchy)
                visual = candidate;
            var sorting = node.GetComponent<SortingGroup>();
            if (group == null && sorting != null && sorting.enabled) group = sorting;
        }
        if (visual != null)
        {
            layer = SortingLayer.GetLayerValueFromID(group == null ? visual.sortingLayerID : group.sortingLayerID);
            order = group == null ? visual.sortingOrder : group.sortingOrder;
            return true;
        }
        var pressable = collider.GetComponent<MousePressableObject>();
        return pressable != null && pressable.hasVisual;
    }

    private bool IsBlockedByPressable(Collider2D[] hits, Collider2D targetCol)
    {
        return FindBlockingPressable(hits, targetCol) != null;
    }

    private MousePressableObject FindBlockingPressable(Collider2D[] hits, Collider2D targetCol)
    {
        if (hits == null || targetCol == null) return null;
        foreach (Collider2D col in hits)
        {
            if (col == targetCol || col.gameObject == targetCol.gameObject) continue;
            MousePressableObject pressable = col.GetComponent<MousePressableObject>();
            if (pressable == null || !pressable.isActiveAndEnabled) continue;
            Collider2D winner = SelectHighestPriority(new Collider2D[] { col, targetCol });
            if (winner == col)
                return pressable;
        }
        return null;
    }

    private MousePressableObject FindBlockingPressable(
        Collider2D[] hits,
        Collider2D targetCol,
        MouseScrollableObject scrollable,
        out MousePressableObject passedPressable)
    {
        passedPressable = null;
        if (hits == null || targetCol == null || scrollable == null) return null;
        foreach (Collider2D col in hits)
        {
            if (col == targetCol) continue;
            MousePressableObject pressable = col.GetComponent<MousePressableObject>();
            if (pressable == null || !pressable.isActiveAndEnabled) continue;
            Collider2D winner = SelectHighestPriority(new Collider2D[] { col, targetCol });
            if (winner != col) continue;
            if (scrollable.CanScrollThroughPressable(pressable))
            {
                if (passedPressable == null) passedPressable = pressable;
                continue;
            }
            return pressable;
        }
        return null;
    }

    [Obsolete]private MousePressableObject checkMouseHover()
    {
        Vector3 mouseWorldPosition;
        if (!Tools.TryGetMousePos(out mouseWorldPosition)) return null;

        Collider2D hit = null;
        if (!(currentMouseLayer.alloweInteractionLayers.Count > 1))
        {
            hit = Physics2D.OverlapPoint(mouseWorldPosition, currentMouseLayer.alloweInteractionLayers[0]);
        }
        else
        {
            int i = 0;
            do
            {
                if (i >= currentMouseLayer.alloweInteractionLayers.Count)
                {
                    break;
                }
                hit = Physics2D.OverlapPoint(mouseWorldPosition, currentMouseLayer.alloweInteractionLayers[i]);
                i++;
            } while (hit == null || i >= currentMouseLayer.alloweInteractionLayers.Count);
        }
        if (hit != null && hit.gameObject.GetComponent<MousePressableObject>() != null)
        {
            return hit.gameObject.GetComponent<MousePressableObject>();
        }
        else
        {
            Debug.Log("No MouseInteractable");
            return null;
        }
    }

    void OnScroll(Vector2 mousePos)
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        float rawScroll = mouse.scroll.ReadValue().y;
        LastRawScrollY = rawScroll;
        var layer = currentMouseLayer;
        if (currentScrollableObject == null || layer == null)
        {
            ResetScrollInput();
            return;
        }
        LastScrollPointerWorld = mousePos;
        if (scrollInputTarget != currentScrollableObject || scrollInputLayer != layer)
        {
            ResetScrollInput();
            scrollInputTarget = currentScrollableObject;
            scrollInputLayer = layer;
        }

        float scrollValue = rawScroll;
        float threshold = Mathf.Max(0.0001f, scrollThreshold);
        if (!float.IsNaN(scrollValue) && !float.IsInfinity(scrollValue) && scrollValue != 0f)
        {
            // Reversal starts a fresh gesture instead of cancelling against stale input.
            if (accumulatedScroll != 0f && Mathf.Sign(accumulatedScroll) != Mathf.Sign(scrollValue))
                accumulatedScroll = 0f;
            // Keep one pending step during cooldown, never a long catch-up queue.
            accumulatedScroll = Mathf.Clamp(accumulatedScroll + scrollValue, -threshold, threshold);
        }

        float now = Time.unscaledTime;
        if (now - lastScrollTime < Mathf.Max(0f, scrollCooldown) || Mathf.Abs(accumulatedScroll) < threshold)
            return;

        float step = accumulatedScroll > 0f ? 1f : -1f;
        accumulatedScroll = 0f;
        lastScrollTime = now;
        LastDispatchedStep = step;
        LastScrollDispatchFrame = Time.frameCount;
        ScrollDispatchSequence++;
        Debug.Log($"[MouseManager] ScrollStep triggered: {step}");
        currentScrollableObject.scrollStepEvent?.Invoke(step);
        OnScrollStep?.Invoke(step);
    }

    private void ResetScrollInput()
    {
        accumulatedScroll = 0f;
        lastScrollTime = float.NegativeInfinity;
        scrollInputTarget = null;
        scrollInputLayer = null;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) ResetScrollInput();
        if (!hasFocus) CancelPointerGesture();
    }


    public void OnMouseConfirmPress()
    {
        Vector3 mouseWorldPosition;
        if (!Tools.TryGetMousePos(out mouseWorldPosition)) return;

        OnMouseConfirm?.Invoke();
        CapturePointer(mouseWorldPosition);
        // 一次性 raycast 查找点击目标
        MousePressableObject target = pressedPressable;
        if (target != null)
        {
            // Only an eligible co-located draggable defers its click. Ordinary
            // buttons keep press-time selection and their existing hold behavior.
            if (target.DeferClickUntilDragRelease && pressedDraggable != null &&
                target.gameObject == pressedDraggable.gameObject)
            {
                releaseClickTarget = target;
                dragStarted = true;
            }
            else target.MouseSelect();
        }
        else if (currentMouseLayer != null && currentMouseLayer.clickNullEqualsCancel)
        {
            // 仅在没有任何交互对象（Pressable / Draggable / Hold / Scrollable）位于鼠标下时，才视为点击空白
            if (!HasAnyInteractiveObject(mouseWorldPosition))
            {
                currentMouseLayer.onClickNullCancel?.Invoke();
            }
        }

        //Hold
        if (currentHoldObject != null)
        {
            currentHoldObject.EnterHold();
            holdPerforming = true;
        }
    }

    /// <summary>
    /// 点击时一次性 raycast，查找鼠标下最高优先级的 MousePressableObject
    /// </summary>
    private MousePressableObject RaycastPressable(Vector2 mousePos)
    {
        if (currentMouseLayer == null) return null;
        for (int i = 0; i < currentMouseLayer.alloweInteractionLayers.Count; i++)
        {
            var hits = Physics2D.OverlapPointAll(mousePos, currentMouseLayer.alloweInteractionLayers[i]);
            if (hits.Length == 0) continue;
            return tryGetMouseObject<MousePressableObject>(hits);
        }
        return null;
    }

    /// <summary>
    /// 检查鼠标位置下是否存在任何交互对象（Pressable / Draggable / Hold / Scrollable）。
    /// 用于"点击空白"判定：四类交互物均不存在时才视为空白。
    /// 与 RaycastPressable / setMouseHover 一致，按 alloweInteractionLayers 顺序，首个非空层即停止扫描。
    /// </summary>
    private bool HasAnyInteractiveObject(Vector2 mousePos)
    {
        if (currentMouseLayer == null) return false;

        for (int i = 0; i < currentMouseLayer.alloweInteractionLayers.Count; i++)
        {
            var hits = Physics2D.OverlapPointAll(mousePos, currentMouseLayer.alloweInteractionLayers[i]);
            if (hits.Length == 0) continue;

            if (tryGetMouseObject<MousePressableObject>(hits) != null) return true;
            if (tryGetMouseObject<MouseHoldObject>(hits) != null) return true;

            var draggable = tryGetMouseObject<MouseDraggableObject>(hits);
            if (draggable != null
                && draggable.IsPointInInteractionBounds(mousePos)
                && !IsBlockedByPressable(hits, draggable.GetComponent<Collider2D>()))
            {
                return true;
            }

            var scrollable = tryGetMouseObject<MouseScrollableObject>(hits);
            if (scrollable != null
                && !IsBlockedByPressable(hits, scrollable.GetComponent<Collider2D>()))
            {
                return true;
            }

            return false;
        }

        return false;
    }

    public void OnMouseCancelPress()
    {
        Vector3 pointer;
        if (!dragStarted && !dragPerforming && Tools.TryGetMousePos(out pointer)) setMouseHover(pointer);
        OnMouseCancel?.Invoke();
        CancelPointerGesture();
    }

    public void OnDragStart(InputAction.CallbackContext context)
    {
        Vector3 mouseWorldPosition;
        if (!Tools.TryGetMousePos(out mouseWorldPosition)) return;

        CapturePointer(mouseWorldPosition);
        if (!dragPerforming) dragStarted = true;
        OnDragStarted?.Invoke(dragStartPos);
    }

    public void OnDragPerform()
    {
        releaseClickTarget = null;
        if (pointerCaptured) currentDraggableObject = pressedDraggable;
        OnDragPerformed?.Invoke();
        if(currentDraggableObject != null && currentDraggableObject.isActiveAndEnabled)
        {
            currentDraggableObject.enterDrag();
        }
    }

    public void OnMouseLeftButtonHoldStart()
    {

    }

    private void CapturePointer(Vector3 position)
    {
        if (pointerCaptured) return;
        dragStarted = false; dragPerforming = false;
        setMouseHover(position);
        pressedDraggable = currentDraggableObject;
        pressedPressable = RaycastPressable(position);
        pressedLayer = currentMouseLayer;
        releaseClickTarget = null;
        dragStartPos = position;
        pointerCaptured = true;
    }

    private void CancelPointerGesture()
    {
        var source = pointerCaptured ? pressedDraggable : currentDraggableObject;
        var wasDragging = dragPerforming;
        pointerCaptured = false; pressedDraggable = null; pressedPressable = null;
        releaseClickTarget = null; pressedLayer = null;
        dragStarted = false; dragPerforming = false; currentDraggableObject = null;
        if (wasDragging && source != null && source.dragging) source.cancelDrag();
    }

    private bool CapturedDragInvalid()
    {
        return pointerCaptured && dragPerforming &&
            (pressedDraggable == null || !pressedDraggable.isActiveAndEnabled || !pressedDraggable.dragging);
    }

    private void CheckDropZone(MouseDraggableObject draggable, Vector2 mouseWorldPosition)
    {
        if (currentMouseLayer == null) return;
        Debug.Log("CheckDropZone");
        for (int i = 0; i < currentMouseLayer.alloweInteractionLayers.Count; i++)
        {
            Collider2D[] hits = Physics2D.OverlapPointAll(mouseWorldPosition, currentMouseLayer.alloweInteractionLayers[i]);
            if (hits.Length == 0) continue;
            DropZone zone = tryGetMouseObject<DropZone>(hits);
            if (zone != null)
            {
                zone.OnDrop(draggable);
                draggable.onDropHitEvent?.Invoke(zone);
                return;
            }
        }
        draggable.onDropMissedEvent?.Invoke();
    }

    public void OnMouseLeftButtonRelease()
    {
        Vector3 mouseWorldPosition;
        if (!Tools.TryGetMousePos(out mouseWorldPosition)) { CancelPointerGesture(); return; }

        OnMouseRelease?.Invoke();
        if (pointerCaptured && pressedLayer != currentMouseLayer) { CancelPointerGesture(); return; }
        if (CapturedDragInvalid()) { CancelPointerGesture(); return; }
        // A fast release can arrive before Update sampled the threshold crossing.
        if (pointerCaptured && dragStarted && Vector3.Distance(dragStartPos, mouseWorldPosition) > dragMoveThreshold)
        { dragStarted = false; dragPerforming = true; OnDragPerform(); }
        var source = pointerCaptured ? pressedDraggable : currentDraggableObject;
        var click = releaseClickTarget;
        var wasDragging = dragPerforming;
        pointerCaptured = false; pressedDraggable = null; pressedPressable = null;
        releaseClickTarget = null; pressedLayer = null;
        dragStarted = false; dragPerforming = false;
        //Debug.Log("release");
        //Drag
        if(wasDragging)
        {
            if(source != null && source.isActiveAndEnabled)
            {
                CheckDropZone(source, mouseWorldPosition);
                if (source != null) source.exitDrag();
                currentDraggableObject = null;
            }
        }
        else if (click != null && click.isActiveAndEnabled && RaycastPressable(mouseWorldPosition) == click)
            click.MouseSelect();
        //Hold
        if (holdPerforming && currentHoldObject != null)
        {
            Collider2D holdCollider = currentHoldObject.GetComponent<Collider2D>();
            bool mouseIsOver = holdCollider != null && holdCollider.OverlapPoint(mouseWorldPosition);
            currentHoldObject.ReleaseHold(mouseIsOver);
            holdPerforming = false;
            currentHoldObject = null;
        }
    }
}
