using UnityEngine;

/// <summary>
/// 排列方向模式
/// </summary>
public enum ArrangementDirection
{
    LeftToRight,    // 从左往右：GetChild(0) 在弧左侧（顺时针侧）
    RightToLeft,    // 从右往左：GetChild(0) 在弧右侧（逆时针侧）
    CenterBalance   // 从中间平衡：GetChild(0) 在弧中心，交替向两侧排列
}

/// <summary>
/// 间隔模式
/// </summary>
public enum SpacingMode
{
    Uniform,        // 强制平均间隔：均匀分布在整个弧上（当前默认行为）
    Fixed           // 固定间隔：按照固定角度间隔排列
}

/// <summary>
/// 弧形布局组件，将子物体以弧形排列在中心点周围，并使其朝向中心。
/// 适用于箭头等需要指向中心的UI元素。
/// </summary>
public class ArcLayout : Layout
{
    [Header("弧形布局设置")]
    [Tooltip("弧形半径")]
    [SerializeField] private float radius = 2f;

    [Tooltip("弧形角度范围（度）")]
    [SerializeField] private float arcAngle = 180f;

    [Header("排列模式")]
    [Tooltip("排列方向")]
    [SerializeField] private ArrangementDirection arrangementDirection = ArrangementDirection.LeftToRight;

    [Tooltip("间隔模式")]
    [SerializeField] private SpacingMode spacingMode = SpacingMode.Uniform;

    [Tooltip("固定间隔模式下的角度间隔（度）")]
    [SerializeField] private float fixedAngleSpacing = 15f;

    [Header("旋转设置")]
    [Tooltip("是否自动调整子物体朝向使其指向中心")]
    [SerializeField] private bool adjustRotation = true;

    [Tooltip("子物体默认朝向（0=右，90=上，180=左，270=下）")]
    [SerializeField] private float defaultDirection = 270f; // 默认向下

    [Tooltip("额外旋转偏移")]
    [SerializeField] private float rotationOffset = 0f;

    [Header("随机浮动")]
    [Tooltip("是否启用随机浮动，让子物体在弧上有些微的上下浮动")]
    [SerializeField] private bool randomFloat = false;

    [Tooltip("随机浮动的最大幅度（沿径向方向）")]
    [SerializeField] private float floatAmplitude = 0.2f;

    [Header("死区")]
    [Tooltip("是否启用死区，禁止子物体排列在指定的弧段内")]
    [SerializeField] private bool enableDeadZone = false;

    [Tooltip("死区起点角度（相对弧中心，0=正上方，正值向左/逆时针）")]
    [SerializeField] private float deadZoneStart = -10f;

    [Tooltip("死区终点角度（相对弧中心，0=正上方，正值向左/逆时针）")]
    [SerializeField] private float deadZoneEnd = 10f;

    protected override void ValidateSpecificFields()
    {
        if (float.IsNaN(radius) || float.IsInfinity(radius) || radius < 0)
        {
            Debug.LogError($"[ArcLayout] {gameObject.name}: radius 无效 ({radius})，重置为默认值 2");
            radius = 2f;
        }

        if (float.IsNaN(arcAngle) || float.IsInfinity(arcAngle))
        {
            Debug.LogError($"[ArcLayout] {gameObject.name}: arcAngle 无效 ({arcAngle})，重置为默认值 180");
            arcAngle = 180f;
        }

        if (float.IsNaN(fixedAngleSpacing) || float.IsInfinity(fixedAngleSpacing))
        {
            Debug.LogError($"[ArcLayout] {gameObject.name}: fixedAngleSpacing 无效 ({fixedAngleSpacing})，重置为默认值 15");
            fixedAngleSpacing = 15f;
        }
    }

    protected override void PerformLayout()
    {
        int childCount = transform.childCount;

        if (childCount == 0)
        {
            Debug.LogWarning("ArcLayout: 没有子物体需要布局");
            return;
        }

        // 钳制 fixedAngleSpacing 最小值
        fixedAngleSpacing = Mathf.Max(0.1f, fixedAngleSpacing);

        // 确保 arcAngle 为正数
        float effectiveArcAngle = Mathf.Abs(arcAngle);
        if (effectiveArcAngle < 0.01f)
        {
            Debug.LogWarning($"ArcLayout on {gameObject.name}: arcAngle 过小或为负数 ({arcAngle})，使用绝对值或默认值 180");
            effectiveArcAngle = 180f;
        }

        // 弧的相对角度范围（相对于弧中心，0=正上方，正值向左/逆时针）
        float arcHalf = effectiveArcAngle / 2f;
        float arcMin = -arcHalf;
        float arcMax = arcHalf;

        // 判断死区是否生效
        bool useDeadZone = enableDeadZone && deadZoneStart < deadZoneEnd
            && deadZoneStart < arcMax && deadZoneEnd > arcMin;

        if (!useDeadZone)
        {
            // 原有路径：全弧均匀分布
            ArrangeOnSingleSegment(arcMin, arcMax, 0, childCount);
            return;
        }

        // CenterBalance 模式与死区冲突警告
        if (arrangementDirection == ArrangementDirection.CenterBalance)
        {
            Debug.LogWarning($"[ArcLayout] {gameObject.name}: CenterBalance 模式与死区功能组合时，将在各段内独立应用中心平衡，可能不符合全局平衡预期");
        }

        // 截断死区到弧范围内
        float dzStart = Mathf.Max(deadZoneStart, arcMin);
        float dzEnd = Mathf.Min(deadZoneEnd, arcMax);

        // 右段（顺时针侧）：[arcMin, dzStart]，长度可能为0
        // 左段（逆时针侧）：[dzEnd, arcMax]，长度可能为0
        float rightLen = Mathf.Max(0f, dzStart - arcMin);
        float leftLen = Mathf.Max(0f, arcMax - dzEnd);
        float totalLen = rightLen + leftLen;

        if (totalLen <= 0f)
        {
            Debug.LogWarning("ArcLayout: 死区完全覆盖弧形范围，无法排列子物体");
            return;
        }

        int rightCount, leftCount;

        if (spacingMode == SpacingMode.Fixed)
        {
            // 固定间隔模式：按容量分配
            int maxRightCount = rightLen > 0 ? Mathf.FloorToInt(rightLen / fixedAngleSpacing) + 1 : 0;
            int maxLeftCount = leftLen > 0 ? Mathf.FloorToInt(leftLen / fixedAngleSpacing) + 1 : 0;

            // 优先填充右段
            rightCount = Mathf.Min(childCount, maxRightCount);
            leftCount = childCount - rightCount;

            if (leftCount > maxLeftCount)
            {
                Debug.LogWarning($"[ArcLayout] {gameObject.name}: 固定间隔下死区无法容纳全部 {childCount} 个子物体");
            }
        }
        else
        {
            // 原有的比例分配逻辑（Uniform 模式）
            rightCount = Mathf.FloorToInt(childCount * (rightLen / totalLen));
            leftCount = childCount - rightCount;
        }

        // 若某段长度为0，把名额全部还给另一段
        if (rightLen <= 0f) { leftCount = childCount; rightCount = 0; }
        else if (leftLen <= 0f) { rightCount = childCount; leftCount = 0; }

        // 子物体按"右段 → 左段"顺序消费
        ArrangeOnSingleSegment(arcMin, dzStart, 0, rightCount);
        ArrangeOnSingleSegment(dzEnd, arcMax, rightCount, leftCount);
    }

    /// <summary>
    /// 计算指定数量子物体在弧段上的角度位置（从左到右顺序）
    /// </summary>
    public static float[] CalculateAngles(float relMin, float relMax, int count, SpacingMode spacingMode, float fixedAngleSpacing)
    {
        float[] angles = new float[count];

        // 特殊情况：只有一个元素，放在中心
        if (count == 1)
        {
            angles[0] = (relMin + relMax) / 2f;
            return angles;
        }

        if (spacingMode == SpacingMode.Uniform)
        {
            // 强制平均间隔：均匀分布
            float step = (relMax - relMin) / (count - 1);
            for (int i = 0; i < count; i++)
            {
                angles[i] = relMin + step * i;
            }
        }
        else // SpacingMode.Fixed
        {
            // 固定间隔：从起点开始，按固定角度排列
            float segmentLength = relMax - relMin;
            float totalRequired = fixedAngleSpacing * (count - 1);

            if (totalRequired > segmentLength)
            {
                // 超出范围：回退到均匀分布
                float step = segmentLength / (count - 1);
                for (int i = 0; i < count; i++)
                {
                    angles[i] = relMin + step * i;
                }
            }
            else
            {
                // 固定间隔排列
                for (int i = 0; i < count; i++)
                {
                    angles[i] = relMin + fixedAngleSpacing * i;
                }
            }
        }

        return angles;
    }

    /// <summary>
    /// 根据排列方向，将子物体索引映射到角度数组索引
    /// </summary>
    public static int MapChildIndexToAngleIndex(int childIndex, int totalCount, ArrangementDirection arrangementDirection)
    {
        switch (arrangementDirection)
        {
            case ArrangementDirection.LeftToRight:
                // GetChild(0) 在最左侧
                return childIndex;

            case ArrangementDirection.RightToLeft:
                // GetChild(0) 在最右侧
                return totalCount - 1 - childIndex;

            case ArrangementDirection.CenterBalance:
                // GetChild(0) 在中心，之后交替向两侧排列
                // 映射：0->中心, 1->右1, 2->左1, 3->右2, 4->左2...
                if (childIndex == 0)
                    return totalCount / 2;
                else if (childIndex % 2 == 1)
                    return totalCount / 2 + (childIndex + 1) / 2;  // 右侧
                else
                    return totalCount / 2 - childIndex / 2;        // 左侧

            default:
                return childIndex;
        }
    }

    /// <summary>
    /// 计算指定数量子物体在弧段上的角度位置（实例方法，调用静态方法）
    /// </summary>
    private float[] CalculateAngles(float relMin, float relMax, int count)
    {
        float[] angles = ArcLayout.CalculateAngles(relMin, relMax, count, spacingMode, fixedAngleSpacing);

        // 如果发生回退，输出警告（仅实例方法需要）
        if (spacingMode == SpacingMode.Fixed)
        {
            float segmentLength = relMax - relMin;
            float totalRequired = fixedAngleSpacing * (count - 1);
            if (totalRequired > segmentLength)
            {
                Debug.LogWarning($"[ArcLayout] {gameObject.name}: 固定间隔 {fixedAngleSpacing}° × {count-1} = {totalRequired}° 超出弧段长度 {segmentLength}°，回退到均匀分布");
            }
        }

        return angles;
    }

    /// <summary>
    /// 根据排列方向，将子物体索引映射到角度数组索引（实例方法，调用静态方法）
    /// </summary>
    private int MapChildIndexToAngleIndex(int childIndex, int totalCount)
    {
        return ArcLayout.MapChildIndexToAngleIndex(childIndex, totalCount, arrangementDirection);
    }

    /// <summary>
    /// 在指定的相对角度区间内排列指定数量的子物体
    /// </summary>
    /// <param name="relMin">区间起点（相对弧中心）</param>
    /// <param name="relMax">区间终点（相对弧中心）</param>
    /// <param name="childStartIndex">子物体起始索引</param>
    /// <param name="count">本段排列数量</param>
    private void ArrangeOnSingleSegment(float relMin, float relMax, int childStartIndex, int count)
    {
        if (count <= 0) return;

        // 单元素：放在段中点（所有模式通用）
        if (count == 1)
        {
            float relMid = (relMin + relMax) * 0.5f;
            float unityAngle = 90f + relMid;
            ArrangeChildAtAngle(transform.GetChild(childStartIndex), unityAngle, childStartIndex);
            return;
        }

        // 步骤1：计算角度列表（从左到右顺序）
        float[] angles = CalculateAngles(relMin, relMax, count);

        // 步骤2：根据排列方向映射子物体到角度
        for (int i = 0; i < count; i++)
        {
            int angleIndex = MapChildIndexToAngleIndex(i, count);
            float relAngle = angles[angleIndex];
            float unityAngle = 90f + relAngle;
            ArrangeChildAtAngle(transform.GetChild(childStartIndex + i), unityAngle, childStartIndex + i);
        }
    }

    /// <summary>
    /// 将子物体放置在指定角度位置
    /// </summary>
    private void ArrangeChildAtAngle(Transform child, float angle, int globalChildIndex)
    {
        // 转换为弧度
        float radian = angle * Mathf.Deg2Rad;

        // 计算位置（极坐标转直角坐标），可选随机浮动
        float r = radius + (randomFloat ? Random.Range(-floatAmplitude, floatAmplitude) : 0f);
        Vector3 targetPosition = new Vector3(
            Mathf.Cos(radian) * r,
            Mathf.Sin(radian) * r,
            0f
        );

        // 计算旋转
        Quaternion targetRotation = child.localRotation;
        if (adjustRotation)
        {
            // 计算指向中心的方向
            Vector3 directionToCenter = -targetPosition.normalized;
            float rotationAngle = Mathf.Atan2(directionToCenter.y, directionToCenter.x) * Mathf.Rad2Deg;
            targetRotation = Quaternion.Euler(0, 0, rotationAngle - defaultDirection + rotationOffset);
        }

        // 调用基类的PlaceChild方法处理位置设置和动画
        PlaceChild(child, targetPosition, targetRotation, globalChildIndex);
    }

    /// <summary>
    /// 设置半径并重新布局
    /// </summary>
    public void SetRadius(float newRadius)
    {
        radius = newRadius;
        ArrangeChildren();
    }

    /// <summary>
    /// 设置弧形角度并重新布局
    /// </summary>
    public void SetArcAngle(float newArcAngle)
    {
        arcAngle = Mathf.Clamp(newArcAngle, 0f, 360f);
        ArrangeChildren();
    }

    /// <summary>
    /// 设置排列方向并重新布局
    /// </summary>
    public void SetArrangementDirection(ArrangementDirection direction)
    {
        arrangementDirection = direction;
        ArrangeChildren();
    }

    /// <summary>
    /// 设置间隔模式并重新布局
    /// </summary>
    public void SetSpacingMode(SpacingMode mode, float fixedSpacing = 15f)
    {
        spacingMode = mode;
        if (mode == SpacingMode.Fixed)
        {
            fixedAngleSpacing = fixedSpacing;
        }
        ArrangeChildren();
    }
    private void OnDrawGizmosSelected()
    {
        // 绘制弧形范围
        Gizmos.color = Color.yellow;

        float startAngle = 90f - arcAngle / 2f;
        float endAngle = 90f + arcAngle / 2f;

        Vector3 center = transform.position;
        Vector3 previousPoint = center + new Vector3(
            Mathf.Cos(startAngle * Mathf.Deg2Rad) * radius,
            Mathf.Sin(startAngle * Mathf.Deg2Rad) * radius,
            0f
        );

        // 绘制弧线
        int segments = 30;
        for (int i = 1; i <= segments; i++)
        {
            float angle = Mathf.Lerp(startAngle, endAngle, i / (float)segments);
            float radian = angle * Mathf.Deg2Rad;

            Vector3 point = center + new Vector3(
                Mathf.Cos(radian) * radius,
                Mathf.Sin(radian) * radius,
                0f
            );

            Gizmos.DrawLine(previousPoint, point);
            previousPoint = point;
        }

        // 绘制半径线
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(center, center + new Vector3(
            Mathf.Cos(startAngle * Mathf.Deg2Rad) * radius,
            Mathf.Sin(startAngle * Mathf.Deg2Rad) * radius,
            0f
        ));
        Gizmos.DrawLine(center, center + new Vector3(
            Mathf.Cos(endAngle * Mathf.Deg2Rad) * radius,
            Mathf.Sin(endAngle * Mathf.Deg2Rad) * radius,
            0f
        ));

        // 绘制中心点
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, 0.1f);

        // 绘制死区
        if (enableDeadZone && deadZoneStart < deadZoneEnd)
        {
            float arcHalf = arcAngle / 2f;
            float dzStart = Mathf.Max(deadZoneStart, -arcHalf);
            float dzEnd = Mathf.Min(deadZoneEnd, arcHalf);

            if (dzEnd > dzStart)
            {
                // 转换为 Unity 角度
                float dzStartUnity = 90f + dzStart;
                float dzEndUnity = 90f + dzEnd;

                Gizmos.color = Color.red;

                // 绘制死区弧线
                Vector3 dzPrev = center + new Vector3(
                    Mathf.Cos(dzStartUnity * Mathf.Deg2Rad) * radius,
                    Mathf.Sin(dzStartUnity * Mathf.Deg2Rad) * radius,
                    0f
                );

                int dzSegments = 20;
                for (int i = 1; i <= dzSegments; i++)
                {
                    float angle = Mathf.Lerp(dzStartUnity, dzEndUnity, i / (float)dzSegments);
                    float radian = angle * Mathf.Deg2Rad;

                    Vector3 point = center + new Vector3(
                        Mathf.Cos(radian) * radius,
                        Mathf.Sin(radian) * radius,
                        0f
                    );

                    Gizmos.DrawLine(dzPrev, point);
                    dzPrev = point;
                }

                // 绘制死区两端的半径线
                Gizmos.DrawLine(center, center + new Vector3(
                    Mathf.Cos(dzStartUnity * Mathf.Deg2Rad) * radius,
                    Mathf.Sin(dzStartUnity * Mathf.Deg2Rad) * radius,
                    0f
                ));
                Gizmos.DrawLine(center, center + new Vector3(
                    Mathf.Cos(dzEndUnity * Mathf.Deg2Rad) * radius,
                    Mathf.Sin(dzEndUnity * Mathf.Deg2Rad) * radius,
                    0f
                ));
            }
        }
    }
}
