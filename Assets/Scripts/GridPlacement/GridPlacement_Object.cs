using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 旋转等级枚举，对应 0°/90°/180°/270°。
/// </summary>
public enum RotationLevel
{
    Deg0 = 0,
    Deg90 = 1,
    Deg180 = 2,
    Deg270 = 3,
}

public class GridPosition
{
    public Vector2 gridPosition = new Vector2();
    public RotationLevel rotationLevel;
    public static GridPosition Default = new GridPosition();

    public GridPosition(Vector2 gridPosition, RotationLevel rotationLevel = RotationLevel.Deg0)
    {
        this.gridPosition = gridPosition;
        this.rotationLevel = rotationLevel;
    }

    public GridPosition()
    {
        gridPosition = new Vector2(0, 0);
        rotationLevel = RotationLevel.Deg0;
    }

}

/// <summary>
/// 可放置在网格上的物体基类。
/// 占位检测通过 Occupied 子物体的世界位置射线检测格子碰撞体，
/// 旋转时子物体位置自然跟随，无需手动计算偏移。
/// </summary>
public class GridPlacement_Object : MonoBehaviour
{
    [TextArea] public string CurrentStateIdentifier;

    [Header("组件")]
    public Button_MouseInteract myButton;
    public MouseDraggableObject myDraggable;
    public Transform GridParent;
    public Transform VisualParent;

    [Header("放置状态")]
    [HideInInspector] public LayerMask cellLayerMask;
    [HideInInspector] public GridPlacement_Grid grid;
    public GridPlacement_Cell primaryCell;

    [Header("占位")]
    [Tooltip("自动收集名称包含 Occupied 的子物体")]
    public List<Transform> occupiedMarkers = new List<Transform>();


    [Header("旋转")]
    public bool allowRotate = true;
    [SerializeField] private RotationLevel _rotation = RotationLevel.Deg0;
    public RotationLevel rotation => _rotation;

    [Header("事件")]
    public UnityEvent onPlaced;
    public UnityEvent onRemoved;
    public UnityEvent onRotated;
    public UnityEvent onPickedUp;

    // FSM 状态
    public GridObjectState currentState;
    public GridObjectState_Normal normalState = new GridObjectState_Normal();
    public GridObjectState_Placing placingState = new GridObjectState_Placing();

    // 已放置时记录的占据格子，用于 RemoveFromGrid
    private List<GridPlacement_Cell> _occupiedCells = new List<GridPlacement_Cell>();

    protected virtual void Start()
    {
        CollectOccupiedMarkers();
        // 如果已经在网格上（场景中预放置），进入 Normal 状态
        if (primaryCell != null)
        {
            ChangeState(normalState);
        }
    }

    protected virtual void Update()
    {
        CurrentStateIdentifier = currentState.GetType().Name;
        currentState?.UpdateState(this);
    }

    /// <summary>
    /// 切换 FSM 状态。
    /// </summary>
    public void ChangeState(GridObjectState newState)
    {
        if (currentState != null)
        {
            currentState.ExitState(this);
        }

        currentState = newState;

        if (currentState != null)
        {
            currentState.EnterState(this);
        }
    }

    /// <summary>
    /// 收集名称包含 "Occupied" 的子物体 Transform。
    /// </summary>
    public void CollectOccupiedMarkers()
    {
        occupiedMarkers.Clear();
        Transform parent = GridParent != null ? GridParent : transform;
        foreach (Transform child in parent)
        {
            if (child.gameObject.name.Contains("Occupied"))
            {
                occupiedMarkers.Add(child);
            }
        }
    }

    /// <summary>
    /// 通过射线检测每个 Occupied 子物体位置下的格子。
    /// 同时从找到的 Cell 更新 grid 引用。
    /// </summary>
    public List<GridPlacement_Cell> RaycastOccupiedCells()
    {
        List<GridPlacement_Cell> cells = new List<GridPlacement_Cell>();

        foreach (Transform marker in occupiedMarkers)
        {
            Collider2D hit = Physics2D.OverlapPoint(marker.position, cellLayerMask);
            if (hit != null)
            {
                var cell = hit.GetComponent<GridPlacement_Cell>();
                if (cell != null && !cells.Contains(cell))
                {
                    cells.Add(cell);
                    if (grid == null && cell.grid != null)
                    {
                        grid = cell.grid;
                    }
                }
            }
        }

        return cells;
    }

    /// <summary>
    /// 只要有一个 occupied marker 未命中 Cell 就视为脱离 Grid（空白处）。
    /// 区别于"格子存在但被占用/失活"的不可放置状态。
    /// </summary>
    public bool IsOverEmptySpace()
    {
        if (occupiedMarkers.Count == 0) return false;
        return RaycastOccupiedCells().Count < occupiedMarkers.Count;
    }

    /// <summary>
    /// 检查当前位置是否可以放置（所有 Occupied 子物体都命中有效格子）。
    /// </summary>
    public bool CanPlace()
    {
        if (occupiedMarkers.Count == 0) return false;

        List<GridPlacement_Cell> cells = RaycastOccupiedCells();

        // 每个 marker 都必须命中一个格子
        if (cells.Count < occupiedMarkers.Count) return false;

        foreach (GridPlacement_Cell cell in cells)
        {
            if (!cell.isActive) return false;
            if (cell.occupant != null && cell.occupant != this) return false;
        }

        return true;
    }

    /// <summary>
    /// 在当前位置放置物体，通过射线检测占据格子。
    /// </summary>
    public bool Place()
    {
        if (!CanPlace()) return false;

        List<GridPlacement_Cell> cells = RaycastOccupiedCells();

        _occupiedCells.Clear();
        foreach (GridPlacement_Cell cell in cells)
        {
            cell.SetOccupant(this);
            _occupiedCells.Add(cell);
        }

        // 第一个命中的格子作为主格子
        if (_occupiedCells.Count > 0)
        {
            primaryCell = _occupiedCells[0];
        }

        OnPlaced();
        onPlaced?.Invoke();
        ChangeState(normalState);
        return true;
    }

    /// <summary>
    /// 从网格中移除物体。
    /// </summary>
    public void RemoveFromGrid()
    {
        if (primaryCell == null) return;

        foreach (GridPlacement_Cell cell in _occupiedCells)
        {
            if (cell != null && cell.occupant == this)
            {
                cell.ClearOccupant();
            }
        }

        _occupiedCells.Clear();
        primaryCell = null;
        ChangeState(null);
        OnRemoved();
        onRemoved?.Invoke();
    }

    /// <summary>
    /// 拾起已放置的物体，清除占用并直接进入放置流程。
    /// </summary>
    public bool PickUp(GridPlacement_Placer placer)
    {
        if (primaryCell == null || placer == null) return false;

        foreach (GridPlacement_Cell cell in _occupiedCells)
        {
            if (cell != null && cell.occupant == this)
            {
                cell.ClearOccupant();
            }
        }

        _occupiedCells.Clear();
        primaryCell = null;
        ChangeState(null);
        OnPickedUp();
        onPickedUp?.Invoke();

        placer.StartPlacing(this);
        return true;
    }

    /// <summary>
    /// 拾起已放置的物体，通过 grid.placer 进入放置流程。
    /// </summary>
    public void PickUp()
    {
        if (grid == null || grid.placer == null) return;
        PickUp(grid.placer);
    }

    /// <summary>
    /// 旋转物体 90°。已放置时验证新朝向，不可行则回滚。
    /// </summary>
    /// <summary>
    /// 获取旋转目标 Transform（GridParent 优先，回退到自身）。
    /// </summary>
    private Transform RotateTarget => GridParent != null ? GridParent : transform;

    public bool Rotate(bool counterClockwise = false)
    {
        if (!allowRotate) return false;

        float angle = counterClockwise ? 90f : -90f;
        RotationLevel oldRotation = _rotation;

        // 计算新旋转等级
        int next = ((int)_rotation + (counterClockwise ? -1 : 1) + 4) % 4;

        if (primaryCell != null)
        {
            // 已放置：移除占用 → 旋转 → 验证 → 重新放置或回滚
            List<GridPlacement_Cell> oldCells = new List<GridPlacement_Cell>(_occupiedCells);
            RemoveFromGrid();

            // 应用旋转（只旋转 GridParent，不影响主物体上的 UI 等）
            _rotation = (RotationLevel)next;
            RotateTarget.Rotate(0, 0, angle);

            if (CanPlace())
            {
                Place();
                // TODO: 视觉旋转回调——子类可 override OnRotated 实现替换图片等非简单旋转的视觉表现
                OnRotated(_rotation);
                onRotated?.Invoke();
                return true;
            }
            else
            {
                // 回滚旋转
                _rotation = oldRotation;
                RotateTarget.Rotate(0, 0, -angle);
                // 恢复占用
                _occupiedCells.Clear();
                foreach (GridPlacement_Cell cell in oldCells)
                {
                    if (cell != null)
                    {
                        cell.SetOccupant(this);
                        _occupiedCells.Add(cell);
                    }
                }
                if (_occupiedCells.Count > 0) primaryCell = _occupiedCells[0];
                return false;
            }
        }
        else
        {
            // 未放置：直接旋转
            _rotation = (RotationLevel)next;
            RotateTarget.Rotate(0, 0, angle);
            // TODO: 视觉旋转回调——子类可 override OnRotated 实现替换图片等非简单旋转的视觉表现
            OnRotated(_rotation);
            onRotated?.Invoke();
            return true;
        }
    }

    /// <summary>
    /// 检查是否可以旋转（不实际执行）。
    /// </summary>
    public bool CanRotate(bool counterClockwise = false)
    {
        if (!allowRotate) return false;
        if (primaryCell == null) return true;

        float angle = counterClockwise ? 90f : -90f;

        // 临时旋转检测
        List<GridPlacement_Cell> oldCells = new List<GridPlacement_Cell>(_occupiedCells);
        foreach (GridPlacement_Cell cell in oldCells)
        {
            if (cell != null && cell.occupant == this) cell.ClearOccupant();
        }

        RotateTarget.Rotate(0, 0, angle);
        bool canPlace = CanPlace();
        RotateTarget.Rotate(0, 0, -angle);

        // 恢复占用
        foreach (GridPlacement_Cell cell in oldCells)
        {
            if (cell != null) cell.SetOccupant(this);
        }

        return canPlace;
    }

    /// <summary>
    /// 将已有 primaryCell 的物体回归到该格子对齐的姿态。
    /// 默认以 Deg0 朝向；可通过 rotationOverride 指定其他朝向。
    /// 仅调整物体 position 与 GridParent 旋转，不改变占位关系。
    /// </summary>
    /// <param name="rotationOverride">可选的目标朝向；不传则使用 Deg0。</param>
    /// <returns>是否成功执行。primaryCell 为空或 occupiedMarkers 为空时返回 false。</returns>
    public bool PlaceBack(RotationLevel? rotationOverride = null)
    {
        if (primaryCell == null) return false;
        if (occupiedMarkers.Count == 0) CollectOccupiedMarkers();
        if (occupiedMarkers.Count == 0) return false;

        RotationLevel target = rotationOverride ?? RotationLevel.Deg0;

        // 旋转方向与 Rotate() 保持一致：RotationLevel 每 +1 对应 -90°
        float targetAngle = -90f * (int)target;
        RotateTarget.localEulerAngles = new Vector3(0f, 0f, targetAngle);
        _rotation = target;

        // 以 occupiedMarkers[0] 为锚点对齐到 primaryCell 中心
        Vector3 cellCenter = primaryCell.transform.position;
        Vector3 anchorPos = occupiedMarkers[0].position;
        Vector3 offset = cellCenter - anchorPos;
        offset.z = 0f;
        transform.position += offset;

        OnRotated(_rotation);
        onRotated?.Invoke();

        return true;
    }

    protected virtual void OnPlaced() { }
    protected virtual void OnRemoved() { }
    protected virtual void OnRotated(RotationLevel newRotation) { }
    protected virtual void OnPickedUp() { }

    public void onPlacedDebug()
    {
        Debug.Log("OnPlaced");
    }

    public void objectPlaced_ButtonPassedIn(ConditionReceiver receiver)
    {
        receiver.ReportCondition(currentState == normalState);
    }

}

public class GridPlacement
{
    public string PrefabName;
    public int uid;
    public GridPosition gridPosition;

    public GridPlacement(string objectName, int uid)
    {
        PrefabName = objectName;
        this.uid = uid;
        this.gridPosition = GridPosition.Default;
    }

    public GridPlacement(string objectName, int uid, GridPosition gridPosition)
    {
        PrefabName = objectName;
        this.uid = uid;
        this.gridPosition = gridPosition;
    }
}
