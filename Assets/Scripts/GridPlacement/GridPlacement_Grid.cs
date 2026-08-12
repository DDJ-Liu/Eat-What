using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 网格管理器，管理 2D 格子数组，提供坐标与世界坐标转换。
/// 支持同一场景多个 Grid 实例共存。
/// </summary>
public class GridPlacement_Grid : MonoBehaviour
{
    public GridPlacement_Placer placer;

    [Header("网格尺寸")]
    public int width = 10;
    public int height = 10;

    [Header("格子设置")]
    [Tooltip("每个格子的世界空间大小")]
    public float cellSize = 1f;

    [Header("初始化")]
    [Tooltip("Awake时自动初始化网格")]
    public bool initOnAwake = true;
    [Tooltip("格子预制体（可选，留空则自动创建）")]
    public GameObject cellPrefab;

    [SerializeField] private GridPlacement_Cell[,] cells;
    public bool isInitialized { get; private set; }

    [Header("网格拓扑")]
    private GridCorner[,] _corners;           // (width+1) × (height+1)
    private GridEdge[,] _horizontalEdges;     // width × (height+1)
    private GridEdge[,] _verticalEdges;       // (width+1) × height

    public GridCorner[,] corners => _corners;
    public GridEdge[,] horizontalEdges => _horizontalEdges;
    public GridEdge[,] verticalEdges => _verticalEdges;

    private void Awake()
    {
        if (placer == null)
        {
            placer = GetComponent<GridPlacement_Placer>();
        }

        if (initOnAwake)
        {
            InitializeGrid();
        }
    }

    /// <summary>
    /// 初始化网格，创建所有格子。
    /// </summary>
    public void InitializeGrid()
    {
        ClearGrid();
        cells = new GridPlacement_Cell[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                CreateCell(x, y);
            }
        }

        GenerateTopology();

        isInitialized = true;
    }

    private void CreateCell(int x, int y)
    {
        GameObject cellObj;
        if (cellPrefab != null)
        {
            cellObj = Instantiate(cellPrefab, transform);
        }
        else
        {
            cellObj = new GameObject();
            cellObj.transform.SetParent(transform);
            var col = cellObj.AddComponent<BoxCollider2D>();
            col.size = new Vector2(cellSize, cellSize);
        }

        cellObj.name = $"Cell_{x}_{y}";
        cellObj.transform.position = GridToWorld(new Vector2Int(x, y));

        var cell = cellObj.GetComponent<GridPlacement_Cell>();
        if (cell == null)
        {
            cell = cellObj.AddComponent<GridPlacement_Cell>();
        }
        cell.coordinates = new Vector2Int(x, y);
        cell.grid = this;
        cells[x, y] = cell;
    }

    /// <summary>
    /// 清除所有格子。
    /// </summary>
    public void ClearGrid()
    {
        if (cells != null)
        {
            for (int x = 0; x < cells.GetLength(0); x++)
            {
                for (int y = 0; y < cells.GetLength(1); y++)
                {
                    if (cells[x, y] != null)
                    {
                        if (Application.isPlaying)
                            Destroy(cells[x, y].gameObject);
                        else
                            DestroyImmediate(cells[x, y].gameObject);
                    }
                }
            }
        }
        cells = null;
        isInitialized = false;
    }

    public GridPlacement_Cell FindCell(int x, int y)
    {
        if (!IsInBounds(x, y)) return null;
        return cells[x, y];
    }

    public GridPlacement_Cell FindCell(Vector2Int pos)
    {
        return FindCell(pos.x, pos.y);
    }

    public bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    public bool IsInBounds(Vector2Int pos)
    {
        return IsInBounds(pos.x, pos.y);
    }

    /// <summary>
    /// 世界坐标转网格坐标（四舍五入到最近格子）。
    /// </summary>
    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        Vector3 origin = transform.position;
        int x = Mathf.RoundToInt((worldPos.x - origin.x) / cellSize);
        int y = Mathf.RoundToInt((worldPos.y - origin.y) / cellSize);
        return new Vector2Int(x, y);
    }

    /// <summary>
    /// 网格坐标转世界坐标（格子中心）。
    /// </summary>
    public Vector3 GridToWorld(Vector2Int gridPos)
    {
        Vector3 origin = transform.position;
        return new Vector3(
            origin.x + gridPos.x * cellSize,
            origin.y + gridPos.y * cellSize,
            0f
        );
    }

    /// <summary>
    /// 角点坐标转世界坐标。
    /// </summary>
    public Vector3 CornerToWorld(Vector2Int cornerCoords)
    {
        Vector3 origin = transform.position;
        return new Vector3(
            origin.x + cornerCoords.x * cellSize,
            origin.y + cornerCoords.y * cellSize,
            0f
        );
    }

    /// <summary>
    /// 生成网格拓扑（角点和边）。
    /// </summary>
    private void GenerateTopology()
    {
        // 1. 创建角点
        _corners = new GridCorner[width + 1, height + 1];
        for (int x = 0; x <= width; x++)
        {
            for (int y = 0; y <= height; y++)
            {
                _corners[x, y] = new GridCorner
                {
                    coordinates = new Vector2Int(x, y),
                    grid = this,
                    worldPosition = CornerToWorld(new Vector2Int(x, y))
                };
            }
        }

        // 2. 创建水平边
        _horizontalEdges = new GridEdge[width, height + 1];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y <= height; y++)
            {
                _horizontalEdges[x, y] = new GridEdge
                {
                    coordinates = new Vector2Int(x, y),
                    direction = EdgeDirection.Horizontal,
                    grid = this,
                    cornerA = _corners[x, y],
                    cornerB = _corners[x + 1, y],
                    length = cellSize
                };
            }
        }

        // 3. 创建垂直边
        _verticalEdges = new GridEdge[width + 1, height];
        for (int x = 0; x <= width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                _verticalEdges[x, y] = new GridEdge
                {
                    coordinates = new Vector2Int(x, y),
                    direction = EdgeDirection.Vertical,
                    grid = this,
                    cornerA = _corners[x, y],
                    cornerB = _corners[x, y + 1],
                    length = cellSize
                };
            }
        }
    }

    #region 拓扑查询 API

    /// <summary>获取指定角点</summary>
    public GridCorner GetCorner(int x, int y)
    {
        if (!IsValidCornerCoord(x, y)) return null;
        return _corners[x, y];
    }

    /// <summary>获取指定角点</summary>
    public GridCorner GetCorner(Vector2Int coords)
    {
        return GetCorner(coords.x, coords.y);
    }

    /// <summary>获取指定水平边</summary>
    public GridEdge GetHorizontalEdge(int x, int y)
    {
        if (!IsValidHorizontalEdgeCoord(x, y)) return null;
        return _horizontalEdges[x, y];
    }

    /// <summary>获取指定垂直边</summary>
    public GridEdge GetVerticalEdge(int x, int y)
    {
        if (!IsValidVerticalEdgeCoord(x, y)) return null;
        return _verticalEdges[x, y];
    }

    /// <summary>检查角点坐标是否有效</summary>
    public bool IsValidCornerCoord(int x, int y)
    {
        return x >= 0 && x <= width && y >= 0 && y <= height;
    }

    /// <summary>检查水平边坐标是否有效</summary>
    public bool IsValidHorizontalEdgeCoord(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y <= height;
    }

    /// <summary>检查垂直边坐标是否有效</summary>
    public bool IsValidVerticalEdgeCoord(int x, int y)
    {
        return x >= 0 && x <= width && y >= 0 && y < height;
    }

    /// <summary>获取所有角点</summary>
    public List<GridCorner> GetAllCorners()
    {
        var list = new List<GridCorner>();
        if (_corners == null) return list;

        for (int x = 0; x <= width; x++)
            for (int y = 0; y <= height; y++)
                list.Add(_corners[x, y]);

        return list;
    }

    /// <summary>获取所有边（水平 + 垂直）</summary>
    public List<GridEdge> GetAllEdges()
    {
        var list = new List<GridEdge>();
        list.AddRange(GetAllHorizontalEdges());
        list.AddRange(GetAllVerticalEdges());
        return list;
    }

    /// <summary>获取所有水平边</summary>
    public List<GridEdge> GetAllHorizontalEdges()
    {
        var list = new List<GridEdge>();
        if (_horizontalEdges == null) return list;

        for (int x = 0; x < width; x++)
            for (int y = 0; y <= height; y++)
                list.Add(_horizontalEdges[x, y]);

        return list;
    }

    /// <summary>获取所有垂直边</summary>
    public List<GridEdge> GetAllVerticalEdges()
    {
        var list = new List<GridEdge>();
        if (_verticalEdges == null) return list;

        for (int x = 0; x <= width; x++)
            for (int y = 0; y < height; y++)
                list.Add(_verticalEdges[x, y]);

        return list;
    }

    #endregion

}
