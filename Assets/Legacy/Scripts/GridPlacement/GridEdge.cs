using UnityEngine;

/// <summary>
/// 边的方向枚举
/// </summary>
public enum EdgeDirection
{
    Horizontal = 0,
    Vertical = 1
}

/// <summary>
/// 网格拓扑中的边（线段），由最多 2 个相邻格子共享。
/// </summary>
public class GridEdge
{
    /// <summary>边坐标（在各自边数组中的索引）</summary>
    public Vector2Int coordinates;

    /// <summary>水平或垂直</summary>
    public EdgeDirection direction;

    /// <summary>父网格引用</summary>
    public GridPlacement_Grid grid;

    /// <summary>起点角（水平边的左端点，垂直边的下端点）</summary>
    public GridCorner cornerA;

    /// <summary>终点角（水平边的右端点，垂直边的上端点）</summary>
    public GridCorner cornerB;

    /// <summary>边起点世界坐标</summary>
    public Vector3 worldStart => cornerA.worldPosition;

    /// <summary>边终点世界坐标</summary>
    public Vector3 worldEnd => cornerB.worldPosition;

    /// <summary>边中心世界坐标</summary>
    public Vector3 worldCenter => (worldStart + worldEnd) * 0.5f;

    /// <summary>缓存的边长度（通常等于 cellSize）</summary>
    public float length;

    /// <summary>可用性标记</summary>
    public bool isActive = true;

    /// <summary>扩展数据挂载点（用于视觉状态、逻辑标记等）</summary>
    public object userData;

    /// <summary>
    /// 获取与此边相邻的格子坐标（最多 2 个）。
    /// </summary>
    public Vector2Int[] GetAdjacentCellCoordinates()
    {
        var adjacent = new System.Collections.Generic.List<Vector2Int>();

        if (direction == EdgeDirection.Horizontal)
        {
            // 水平边：连接 corners[x, y] 到 corners[x+1, y]
            // 下方格子 (x, y-1)
            if (coordinates.y > 0 && coordinates.x < grid.width)
                adjacent.Add(new Vector2Int(coordinates.x, coordinates.y - 1));

            // 上方格子 (x, y)
            if (coordinates.y < grid.height && coordinates.x < grid.width)
                adjacent.Add(new Vector2Int(coordinates.x, coordinates.y));
        }
        else // Vertical
        {
            // 垂直边：连接 corners[x, y] 到 corners[x, y+1]
            // 左侧格子 (x-1, y)
            if (coordinates.x > 0 && coordinates.y < grid.height)
                adjacent.Add(new Vector2Int(coordinates.x - 1, coordinates.y));

            // 右侧格子 (x, y)
            if (coordinates.x < grid.width && coordinates.y < grid.height)
                adjacent.Add(new Vector2Int(coordinates.x, coordinates.y));
        }

        return adjacent.ToArray();
    }
}
