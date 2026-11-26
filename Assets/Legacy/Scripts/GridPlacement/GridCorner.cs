using UnityEngine;

/// <summary>
/// 网格拓扑中的角点，由最多 4 个相邻格子共享。
/// </summary>
public class GridCorner
{
    /// <summary>角点坐标 (0 到 width, 0 到 height)</summary>
    public Vector2Int coordinates;

    /// <summary>父网格引用</summary>
    public GridPlacement_Grid grid;

    /// <summary>缓存的世界坐标</summary>
    public Vector3 worldPosition;

    /// <summary>可用性标记</summary>
    public bool isActive = true;

    /// <summary>扩展数据挂载点（用于视觉标记、寻路元数据等）</summary>
    public object userData;

    /// <summary>
    /// 获取与此角点相邻的格子坐标（最多 4 个）。
    /// </summary>
    public Vector2Int[] GetAdjacentCellCoordinates()
    {
        var adjacent = new System.Collections.Generic.List<Vector2Int>();

        // 左下格子 (x-1, y-1)
        if (coordinates.x > 0 && coordinates.y > 0)
            adjacent.Add(new Vector2Int(coordinates.x - 1, coordinates.y - 1));

        // 右下格子 (x, y-1)
        if (coordinates.x < grid.width && coordinates.y > 0)
            adjacent.Add(new Vector2Int(coordinates.x, coordinates.y - 1));

        // 右上格子 (x, y)
        if (coordinates.x < grid.width && coordinates.y < grid.height)
            adjacent.Add(new Vector2Int(coordinates.x, coordinates.y));

        // 左上格子 (x-1, y)
        if (coordinates.x > 0 && coordinates.y < grid.height)
            adjacent.Add(new Vector2Int(coordinates.x - 1, coordinates.y));

        return adjacent.ToArray();
    }
}
