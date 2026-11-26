using UnityEngine;

/// <summary>
/// 网格布局 - 以网格形式排列子物体
/// </summary>
public class GridLayout : Layout
{
    [Header("网格布局设置")]
    [Tooltip("网格约束模式")]
    [SerializeField] private GridConstraint constraint = GridConstraint.FixedColumnCount;

    [Tooltip("固定列数（当约束为FixedColumnCount时）")]
    [SerializeField] private int columnCount = 3;

    [Tooltip("固定行数（当约束为FixedRowCount时）")]
    [SerializeField] private int rowCount = 3;

    [Header("间距设置")]
    [Tooltip("水平间距")]
    [SerializeField] private float horizontalSpacing = 0.5f;

    [Tooltip("垂直间距")]
    [SerializeField] private float verticalSpacing = 0.5f;

    [Header("对齐设置")]
    [Tooltip("水平对齐")]
    [SerializeField] private HorizontalAlignment horizontalAlignment = HorizontalAlignment.Left;

    [Tooltip("垂直对齐")]
    [SerializeField] private VerticalAlignment verticalAlignment = VerticalAlignment.Top;

    [Tooltip("起始角")]
    [SerializeField] private GridStartCorner startCorner = GridStartCorner.TopLeft;

    [Tooltip("排列轴")]
    [SerializeField] private GridAxis axis = GridAxis.Horizontal;

    public enum GridConstraint
    {
        FixedColumnCount,   // 固定列数，行数自动
        FixedRowCount       // 固定行数，列数自动
    }

    public enum HorizontalAlignment
    {
        Left,
        Center,
        Right
    }

    public enum VerticalAlignment
    {
        Top,
        Middle,
        Bottom
    }

    public enum GridStartCorner
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }

    public enum GridAxis
    {
        Horizontal,     // 先横向填充
        Vertical        // 先纵向填充
    }

    protected override void ValidateSpecificFields()
    {
        if (columnCount < 1)
        {
            Debug.LogError($"[GridLayout] {gameObject.name}: columnCount 必须 >= 1，重置为 3");
            columnCount = 3;
        }

        if (rowCount < 1)
        {
            Debug.LogError($"[GridLayout] {gameObject.name}: rowCount 必须 >= 1，重置为 3");
            rowCount = 3;
        }

        if (float.IsNaN(horizontalSpacing) || float.IsInfinity(horizontalSpacing))
        {
            Debug.LogError($"[GridLayout] {gameObject.name}: horizontalSpacing 无效，重置为 0.5");
            horizontalSpacing = 0.5f;
        }

        if (float.IsNaN(verticalSpacing) || float.IsInfinity(verticalSpacing))
        {
            Debug.LogError($"[GridLayout] {gameObject.name}: verticalSpacing 无效，重置为 0.5");
            verticalSpacing = 0.5f;
        }
    }

    protected override void PerformLayout()
    {
        int childCount = transform.childCount;

        // 步骤1：根据约束计算实际的行列数
        int actualColumns, actualRows;
        CalculateGridDimensions(childCount, out actualColumns, out actualRows);

        // 步骤2：收集所有子物体的尺寸
        ChildSizeInfo[] sizes = new ChildSizeInfo[childCount];
        bool hasInvalidSize = false;

        for (int i = 0; i < childCount; i++)
        {
            Transform child = transform.GetChild(i);
            sizes[i] = GetChildSize(child, i);

            if (!sizes[i].isValid)
            {
                hasInvalidSize = true;
            }
        }

        if (hasInvalidSize)
        {
            Debug.LogError($"[GridLayout] {gameObject.name}: 存在无效的子物体尺寸，布局中止");
            return;
        }

        // 步骤3：计算每行每列的最大尺寸
        float[] columnWidths = new float[actualColumns];
        float[] rowHeights = new float[actualRows];

        for (int i = 0; i < childCount; i++)
        {
            int col, row;
            GetGridPosition(i, actualColumns, actualRows, out col, out row);

            columnWidths[col] = Mathf.Max(columnWidths[col], sizes[i].size.x);
            rowHeights[row] = Mathf.Max(rowHeights[row], sizes[i].size.y);
        }

        // 步骤4：计算网格总尺寸
        float totalWidth = 0f;
        for (int c = 0; c < actualColumns; c++)
        {
            totalWidth += columnWidths[c];
        }
        totalWidth += horizontalSpacing * (actualColumns - 1);

        float totalHeight = 0f;
        for (int r = 0; r < actualRows; r++)
        {
            totalHeight += rowHeights[r];
        }
        totalHeight += verticalSpacing * (actualRows - 1);

        // 步骤5：根据对齐方式计算起始坐标
        float startX = CalculateStartX(totalWidth);
        float startY = CalculateStartY(totalHeight);

        // 步骤6：依次放置每个子物体
        for (int i = 0; i < childCount; i++)
        {
            Transform child = transform.GetChild(i);

            int col, row;
            GetGridPosition(i, actualColumns, actualRows, out col, out row);

            // 计算该单元格的位置
            float cellX = startX;
            for (int c = 0; c < col; c++)
            {
                cellX += columnWidths[c] + horizontalSpacing;
            }

            float cellY = startY;
            for (int r = 0; r < row; r++)
            {
                cellY -= (rowHeights[r] + verticalSpacing);
            }

            // 单元格中心位置
            Vector3 position = new Vector3(
                cellX + columnWidths[col] * 0.5f,
                cellY - rowHeights[row] * 0.5f,
                0f
            );

            PlaceChild(child, position, Quaternion.identity, i);
        }
    }

    /// <summary>
    /// 根据约束计算实际的行列数
    /// </summary>
    private void CalculateGridDimensions(int childCount, out int actualColumns, out int actualRows)
    {
        if (constraint == GridConstraint.FixedColumnCount)
        {
            actualColumns = columnCount;
            actualRows = Mathf.CeilToInt((float)childCount / columnCount);
        }
        else // FixedRowCount
        {
            actualRows = rowCount;
            actualColumns = Mathf.CeilToInt((float)childCount / rowCount);
        }
    }

    /// <summary>
    /// 根据子物体索引计算网格位置（列，行）
    /// </summary>
    private void GetGridPosition(int childIndex, int actualColumns, int actualRows, out int col, out int row)
    {
        int baseCol, baseRow;

        // 步骤1：按axis确定基础位置
        if (axis == GridAxis.Horizontal)
        {
            // 横向优先：先填满一行再到下一行
            baseCol = childIndex % actualColumns;
            baseRow = childIndex / actualColumns;
        }
        else // Vertical
        {
            // 纵向优先：先填满一列再到下一列
            baseRow = childIndex % actualRows;
            baseCol = childIndex / actualRows;
        }

        // 步骤2：根据startCorner调整位置
        switch (startCorner)
        {
            case GridStartCorner.TopLeft:
                col = baseCol;
                row = baseRow;
                break;

            case GridStartCorner.TopRight:
                col = actualColumns - 1 - baseCol;
                row = baseRow;
                break;

            case GridStartCorner.BottomLeft:
                col = baseCol;
                row = actualRows - 1 - baseRow;
                break;

            case GridStartCorner.BottomRight:
                col = actualColumns - 1 - baseCol;
                row = actualRows - 1 - baseRow;
                break;

            default:
                col = baseCol;
                row = baseRow;
                break;
        }
    }

    private float CalculateStartX(float totalWidth)
    {
        switch (horizontalAlignment)
        {
            case HorizontalAlignment.Left:
                return 0f;

            case HorizontalAlignment.Center:
                return -totalWidth * 0.5f;

            case HorizontalAlignment.Right:
                return -totalWidth;

            default:
                return 0f;
        }
    }

    private float CalculateStartY(float totalHeight)
    {
        switch (verticalAlignment)
        {
            case VerticalAlignment.Top:
                return 0f;

            case VerticalAlignment.Middle:
                return totalHeight * 0.5f;

            case VerticalAlignment.Bottom:
                return totalHeight;

            default:
                return 0f;
        }
    }

    // ==================== 公共API ====================

    /// <summary>
    /// 设置列数并重新布局
    /// </summary>
    public void SetColumnCount(int newColumnCount)
    {
        columnCount = Mathf.Max(1, newColumnCount);
        ArrangeChildren();
    }

    /// <summary>
    /// 设置行数并重新布局
    /// </summary>
    public void SetRowCount(int newRowCount)
    {
        rowCount = Mathf.Max(1, newRowCount);
        ArrangeChildren();
    }

    /// <summary>
    /// 设置约束模式并重新布局
    /// </summary>
    public void SetConstraint(GridConstraint newConstraint)
    {
        constraint = newConstraint;
        ArrangeChildren();
    }

    /// <summary>
    /// 设置间距并重新布局
    /// </summary>
    public void SetSpacing(float horizontal, float vertical)
    {
        horizontalSpacing = horizontal;
        verticalSpacing = vertical;
        ArrangeChildren();
    }

    /// <summary>
    /// 设置对齐方式并重新布局
    /// </summary>
    public void SetAlignment(HorizontalAlignment hAlign, VerticalAlignment vAlign)
    {
        horizontalAlignment = hAlign;
        verticalAlignment = vAlign;
        ArrangeChildren();
    }
}
