using UnityEngine;

/// <summary>
/// 网格中的单个格子，存储坐标和占用信息。
/// 对应原系统的 Tile，去除了连接检测和视觉反馈。
/// </summary>
public class GridPlacement_Cell : MonoBehaviour
{
    [HideInInspector] public GridPlacement_Grid grid;
    public Vector2Int coordinates;
    public bool isActive = true;

    [SerializeField] private GridPlacement_Object _occupant;
    public GridPlacement_Object occupant
    {
        get => _occupant;
        private set => _occupant = value;
    }

    public bool isOccupied => occupant != null;

    public bool CanAccept()
    {
        return isActive && !isOccupied;
    }

    public void SetOccupant(GridPlacement_Object obj)
    {
        occupant = obj;
    }

    public void ClearOccupant()
    {
        occupant = null;
    }

    #region 拓扑查询 API

    /// <summary>获取格子左下角点（Bottom-Left）</summary>
    public GridCorner GetCornerBL()
    {
        return grid.GetCorner(coordinates.x, coordinates.y);
    }

    /// <summary>获取格子右下角点（Bottom-Right）</summary>
    public GridCorner GetCornerBR()
    {
        return grid.GetCorner(coordinates.x + 1, coordinates.y);
    }

    /// <summary>获取格子右上角点（Top-Right）</summary>
    public GridCorner GetCornerTR()
    {
        return grid.GetCorner(coordinates.x + 1, coordinates.y + 1);
    }

    /// <summary>获取格子左上角点（Top-Left）</summary>
    public GridCorner GetCornerTL()
    {
        return grid.GetCorner(coordinates.x, coordinates.y + 1);
    }

    /// <summary>获取格子的 4 个角点（从左下角顺时针：BL, BR, TR, TL）</summary>
    public GridCorner[] GetCorners()
    {
        return new GridCorner[]
        {
            GetCornerBL(),
            GetCornerBR(),
            GetCornerTR(),
            GetCornerTL()
        };
    }

    /// <summary>获取格子底边</summary>
    public GridEdge GetEdgeBottom()
    {
        return grid.GetHorizontalEdge(coordinates.x, coordinates.y);
    }

    /// <summary>获取格子右边</summary>
    public GridEdge GetEdgeRight()
    {
        return grid.GetVerticalEdge(coordinates.x + 1, coordinates.y);
    }

    /// <summary>获取格子顶边</summary>
    public GridEdge GetEdgeTop()
    {
        return grid.GetHorizontalEdge(coordinates.x, coordinates.y + 1);
    }

    /// <summary>获取格子左边</summary>
    public GridEdge GetEdgeLeft()
    {
        return grid.GetVerticalEdge(coordinates.x, coordinates.y);
    }

    /// <summary>获取格子的 4 条边（顺序：底、右、顶、左）</summary>
    public GridEdge[] GetEdges()
    {
        return new GridEdge[]
        {
            GetEdgeBottom(),
            GetEdgeRight(),
            GetEdgeTop(),
            GetEdgeLeft()
        };
    }

    #endregion
}
