using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// 放置交互控制器，处理物体在网格上的放置流程。
/// 集成 MouseManager 输入系统，使用 MouseInteractionLayer 管理输入优先级。
/// 放置中的鼠标跟随/吸附/预览颜色由 GridObjectState_Placing 管理。
/// </summary>
public class GridPlacement_Placer : MonoBehaviour
{
    [Header("输入集成")]
    [Tooltip("Cell 所在的物理层，用于 raycast 发现 Grid")]
    public LayerMask cellLayerMask;
    [Tooltip("manualStackLayer=true 的交互层，放置时入栈")]
    public MouseInteractionLayer placementLayer;

    [Header("预览")]
    public Color validColor = new Color(0f, 1f, 0f, 0.5f);
    public Color invalidColor = new Color(1f, 0f, 0f, 0.5f);

    [Header("行为")]
    [Tooltip("勾选后：鼠标在没有 Grid 的地方松开时视为取消放置（销毁预览物体）。默认关闭，保持原有行为。")]
    public bool releaseOnEmptyEqualsCancel = false;

    [Header("状态")]
    [SerializeField] private GridPlacement_Object _currentObject;
    [SerializeField] private bool _isPlacing = false;
    public bool isPlacing => _isPlacing;
    public Transform PlacementObjectParent;

    [Header("事件")]
    public UnityEvent<GridPlacement_Object> onStartPlacing;
    public UnityEvent<GridPlacement_Object> onPlaced;
    public UnityEvent onCancelled;

    /// <summary>
    /// 开始放置已有的物体实例。
    /// </summary>
    public void StartPlacing(GridPlacement_Object obj)
    {
        if (obj == null)
        {
            Debug.LogError("GridPlacement_Placer: object is null");
            return;
        }

        if (_isPlacing)
        {
            CancelPlacement();
        }

        _currentObject = obj;
        _isPlacing = true;

        // 通过 FSM 切换到 Placing 状态（鼠标跟随/吸附/预览颜色在状态内处理）
        _currentObject.placingState.init(cellLayerMask, validColor, invalidColor);
        _currentObject.ChangeState(_currentObject.placingState);

        var draggable = _currentObject.myDraggable;
        if (draggable != null)
        {
            if (MouseManager.Instance.currentDraggableObject != null)
                MouseManager.Instance.currentDraggableObject.exitDrag();
            MouseManager.Instance.currentDraggableObject = draggable;
            MouseManager.Instance.dragStarted = false;
            MouseManager.Instance.dragPerforming = true;
            draggable.enterDrag();
            draggable.endDraggingEvent.AddListener(ConfirmPlacement);
        }

        // 入栈交互层，左键点击空白处 → ConfirmPlacement
        if (placementLayer != null)
        {
            placementLayer.clickNullEqualsCancel = true;
            placementLayer.onClickNullCancel.AddListener(ConfirmPlacement);
            placementLayer.OnPushLayer();
        }

        obj.transform.parent = PlacementObjectParent;

        onStartPlacing?.Invoke(obj);
    }

    /// <summary>
    /// 从 ObjectData 创建物体并开始放置。
    /// </summary>
    public void StartPlacing(GridPlacement_ObjectData data)
    {
        if (data == null)
        {
            Debug.LogError("GridPlacement_Placer: data is null");
            return;
        }

        GridPlacement_Object obj = data.Spawn();
        if (obj != null)
        {
            StartPlacing(obj);
        }
    }

    /// <summary>
    /// 从 ObjectData 创建物体并开始放置。
    /// </summary>
    public GameObject StartPlacing(GridPlacement_ObjectData data, bool hasReturn = true)
    {
        if (data == null)
        {
            Debug.LogError("GridPlacement_Placer: data is null");
            return null;
        }

        GridPlacement_Object obj = data.Spawn();
        if (obj != null)
        {
            StartPlacing(obj);
            return obj.gameObject;
        }

        return null;
    }

    private void Update()
    {
        if (!_isPlacing || _currentObject == null) return;

        // 输入处理：右键取消
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            CancelPlacement();
        }
    }

    /// <summary>
    /// 确认放置当前物体到悬停格子。
    /// </summary>
    public void ConfirmPlacement()
    {
        if (!_isPlacing || _currentObject == null) return;

        // 1) 空白处松开
        if (_currentObject.IsOverEmptySpace())
        {
            if (releaseOnEmptyEqualsCancel)
            {
                CancelPlacement();
            }
            return;
        }

        // 2) 正常放置路径
        if (_currentObject.CanPlace() && _currentObject.Place())
        {
            var placedObj = _currentObject;
            EndPlacing();
            onPlaced?.Invoke(placedObj);
            return;
        }

        // 3) 有格子但不可用 → 日志标记，保持放置状态
        foreach (var cell in _currentObject.RaycastOccupiedCells())
        {
            if (!cell.isActive)
            {
                Debug.Log($"[GridPlacement] 放置失败：格子 {cell.name} 未激活", cell);
            }
            else if (cell.occupant != null && cell.occupant != _currentObject)
            {
                Debug.Log($"[GridPlacement] 放置失败：格子 {cell.name} 已被 {cell.occupant.name} 占用", cell);
            }
        }
    }

    /// <summary>
    /// 取消放置，销毁当前物体。
    /// </summary>
    public void CancelPlacement()
    {
        if (!_isPlacing) return;

        if (_currentObject != null)
        {
            if (_currentObject.myDraggable != null)
            {
                _currentObject.myDraggable.endDraggingEvent.RemoveListener(ConfirmPlacement);
                _currentObject.myDraggable.exitDrag();
                if (MouseManager.Instance != null && MouseManager.Instance.currentDraggableObject == _currentObject.myDraggable)
                {
                    MouseManager.Instance.currentDraggableObject = null;
                    MouseManager.Instance.dragPerforming = false;
                }
            }
            Destroy(_currentObject.gameObject);
        }

        EndPlacing();
        onCancelled?.Invoke();
    }

    /// <summary>
    /// 旋转当前正在放置的物体。
    /// </summary>
    public void RotateCurrentObject(bool counterClockwise = false)
    {
        if (!_isPlacing || _currentObject == null) return;
        _currentObject.Rotate(counterClockwise);
    }

    public void onRotate()
    {
        RotateCurrentObject(false);
    }

    public void onRotateCCW()
    {
        RotateCurrentObject(true);
    }

    private void EndPlacing()
    {
        if (_currentObject != null && _currentObject.myDraggable != null)
        {
            _currentObject.myDraggable.endDraggingEvent.RemoveListener(ConfirmPlacement);
        }

        _currentObject = null;
        _isPlacing = false;

        if (placementLayer != null)
        {
            placementLayer.onClickNullCancel.RemoveListener(ConfirmPlacement);
            placementLayer.OnRemoveLayer();
        }
    }

    private void OnDestroy()
    {
    }

    private void OnDisable()
    {
    }
}
