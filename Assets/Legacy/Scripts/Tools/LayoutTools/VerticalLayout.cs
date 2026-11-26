using UnityEngine;

/// <summary>
/// 垂直布局 - 从上到下排列子物体
/// </summary>
public class VerticalLayout : Layout
{
    [Header("垂直布局设置")]
    [Tooltip("子物体之间的间距")]
    [SerializeField] private float spacing = 0.5f;

    [Tooltip("对齐方式")]
    [SerializeField] private VerticalAlignment alignment = VerticalAlignment.Top;

    [Tooltip("是否反转排列顺序（从下到上）")]
    [SerializeField] private bool reverseOrder = false;

    public enum VerticalAlignment
    {
        Top,        // 顶部对齐：第一个元素在 y=0
        Middle,     // 居中对齐：整体居中于父物体
        Bottom      // 底部对齐：最后一个元素在 y=0
    }

    protected override void ValidateSpecificFields()
    {
        if (float.IsNaN(spacing) || float.IsInfinity(spacing))
        {
            Debug.LogError($"[VerticalLayout] {gameObject.name}: spacing 无效 ({spacing})，重置为 0.5");
            spacing = 0.5f;
        }
    }

    protected override void PerformLayout()
    {
        int childCount = transform.childCount;

        // 步骤1：收集所有子物体的尺寸
        ChildSizeInfo[] sizes = new ChildSizeInfo[childCount];
        float totalHeight = 0f;
        bool hasInvalidSize = false;

        for (int i = 0; i < childCount; i++)
        {
            Transform child = transform.GetChild(i);
            sizes[i] = GetChildSize(child, i);

            if (!sizes[i].isValid)
            {
                hasInvalidSize = true;
                continue;
            }

            totalHeight += sizes[i].size.y;
        }

        if (hasInvalidSize)
        {
            Debug.LogError($"[VerticalLayout] {gameObject.name}: 存在无效的子物体尺寸，布局中止");
            return;
        }

        // 加上间距
        totalHeight += spacing * (childCount - 1);

        // 步骤2：根据对齐方式计算起始位置
        float startY = CalculateStartY(totalHeight);

        // 步骤3：依次排列子物体（从上到下，Y值递减）
        float currentY = startY;

        for (int i = 0; i < childCount; i++)
        {
            int actualIndex = reverseOrder ? (childCount - 1 - i) : i;
            Transform child = transform.GetChild(actualIndex);
            ChildSizeInfo sizeInfo = sizes[actualIndex];

            // 子物体中心位置
            Vector3 position = new Vector3(
                0f,
                currentY - sizeInfo.size.y * 0.5f,
                0f
            );

            PlaceChild(child, position, Quaternion.identity, actualIndex);

            // 移动到下一个位置（向下）
            currentY -= (sizeInfo.size.y + spacing);
        }
    }

    private float CalculateStartY(float totalHeight)
    {
        switch (alignment)
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
    /// 设置间距并重新布局
    /// </summary>
    public void SetSpacing(float newSpacing)
    {
        spacing = newSpacing;
        ArrangeChildren();
    }

    /// <summary>
    /// 设置对齐方式并重新布局
    /// </summary>
    public void SetAlignment(VerticalAlignment newAlignment)
    {
        alignment = newAlignment;
        ArrangeChildren();
    }

    /// <summary>
    /// 设置是否反转顺序并重新布局
    /// </summary>
    public void SetReverseOrder(bool reverse)
    {
        reverseOrder = reverse;
        ArrangeChildren();
    }
}
