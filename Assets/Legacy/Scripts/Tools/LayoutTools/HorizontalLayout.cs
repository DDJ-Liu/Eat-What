using UnityEngine;

/// <summary>
/// 水平布局 - 从左到右排列子物体
/// </summary>
public class HorizontalLayout : Layout
{
    [Header("水平布局设置")]
    [Tooltip("子物体之间的间距")]
    [SerializeField] private float spacing = 0.5f;

    [Tooltip("对齐方式")]
    [SerializeField] private HorizontalAlignment alignment = HorizontalAlignment.Left;

    [Tooltip("是否反转排列顺序（从右到左）")]
    [SerializeField] private bool reverseOrder = false;

    public enum HorizontalAlignment
    {
        Left,       // 左对齐：第一个元素在 x=0
        Center,     // 居中对齐：整体居中于父物体
        Right       // 右对齐：最后一个元素在 x=0
    }

    protected override void ValidateSpecificFields()
    {
        if (float.IsNaN(spacing) || float.IsInfinity(spacing))
        {
            Debug.LogError($"[HorizontalLayout] {gameObject.name}: spacing 无效 ({spacing})，重置为 0.5");
            spacing = 0.5f;
        }
    }

    protected override void PerformLayout()
    {
        int childCount = transform.childCount;

        // 步骤1：收集所有子物体的尺寸
        ChildSizeInfo[] sizes = new ChildSizeInfo[childCount];
        float totalWidth = 0f;
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

            totalWidth += sizes[i].size.x;
        }

        if (hasInvalidSize)
        {
            Debug.LogError($"[HorizontalLayout] {gameObject.name}: 存在无效的子物体尺寸，布局中止");
            return;
        }

        // 加上间距
        totalWidth += spacing * (childCount - 1);

        // 步骤2：根据对齐方式计算起始位置
        float startX = CalculateStartX(totalWidth);

        // 步骤3：依次排列子物体
        float currentX = startX;

        for (int i = 0; i < childCount; i++)
        {
            int actualIndex = reverseOrder ? (childCount - 1 - i) : i;
            Transform child = transform.GetChild(actualIndex);
            ChildSizeInfo sizeInfo = sizes[actualIndex];

            // 子物体中心位置（默认Transform.position是中心）
            Vector3 position = new Vector3(
                currentX + sizeInfo.size.x * 0.5f,
                0f,
                0f
            );

            PlaceChild(child, position, Quaternion.identity, actualIndex);

            // 移动到下一个位置
            currentX += sizeInfo.size.x + spacing;
        }
    }

    private float CalculateStartX(float totalWidth)
    {
        switch (alignment)
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
    public void SetAlignment(HorizontalAlignment newAlignment)
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
