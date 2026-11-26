using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 控制点编辑约束模式
/// </summary>
public enum MonotonicityMode
{
    /// <summary>自由模式 - 无约束</summary>
    Free,

    /// <summary>X单调模式 - 确保 x[i] < x[i+1]，每个x对应唯一y（函数）</summary>
    XMonotonic,

    /// <summary>Y单调模式 - 确保 y[i] < y[i+1]，每个y对应唯一x（反函数）</summary>
    YMonotonic
}

/// <summary>
/// 弧线路径的控制点数据
/// </summary>
[System.Serializable]
public class ArcPathControlPoint
{
    [Tooltip("控制点位置（局部坐标）")]
    public Vector2 position;

    [Tooltip("切线向量（表示斜率方向和权重）")]
    public Vector2 tangent;

    [Tooltip("是否锁定切线（锁定后不会自动调整）")]
    public bool tangentLocked = false;
}

/// <summary>
/// 弧线路径组件 - 可在Scene中编辑的Hermite样条曲线
/// 提供Evaluate(x)接口查询曲线上的y值
/// 提供EvaluateByNormalizedDistance(t)接口实现弧长均匀的路径采样
/// </summary>
public class ArcPathComponent : MonoBehaviour
{
    [Header("控制点")]
    [SerializeField] private List<ArcPathControlPoint> controlPoints = new List<ArcPathControlPoint>();

    [Header("编辑约束")]
    [HideInInspector]
    [SerializeField] private MonotonicityMode monotonicityMode = MonotonicityMode.Free;

    [Header("显示设置")]
    [Tooltip("每段曲线的细分数（影响绘制精度）")]
    [SerializeField] private int curveSegments = 20;

    [Tooltip("在非编辑模式下也显示曲线")]
    [SerializeField] private bool alwaysShowCurve = true;

    // 弧长参数化缓存
    private float[] segmentArcLengths;
    private float totalArcLength;
    private bool arcLengthCacheDirty = true;

    #region 公共 API

    /// <summary>
    /// 根据x坐标查询曲线上对应的y坐标（水平均匀模式）
    /// </summary>
    /// <param name="x">x坐标（世界坐标）</param>
    /// <param name="y">输出y坐标（世界坐标）</param>
    /// <returns>是否成功找到（x在曲线范围内）</returns>
    public bool Evaluate(float x, out float y)
    {
        y = 0f;

        if (controlPoints.Count < 2)
        {
            Debug.LogWarning("[ArcPathComponent] 控制点少于2个，无法计算曲线");
            return false;
        }

        // 转换到局部坐标
        Vector3 localPoint = transform.InverseTransformPoint(new Vector3(x, 0, 0));
        float localX = localPoint.x;

        // 边界检查（确保 minX <= maxX，支持反向曲线）
        float minX = Mathf.Min(controlPoints[0].position.x, controlPoints[controlPoints.Count - 1].position.x);
        float maxX = Mathf.Max(controlPoints[0].position.x, controlPoints[controlPoints.Count - 1].position.x);

        if (localX < minX || localX > maxX)
        {
            Debug.LogWarning($"[ArcPathComponent] Evaluate 失败 - worldX: {x:F2}, localX: {localX:F2}, 范围: [{minX:F2}, {maxX:F2}]");
            return false;
        }

        // 找到x所在的段
        int segmentIndex = FindSegmentForX(localX);
        if (segmentIndex == -1)
        {
            Debug.LogWarning($"[ArcPathComponent] FindSegmentForX 失败 - localX: {localX:F2}");
            return false;
        }

        // 二分查找参数t
        float t = BinarySearchParameterForX(segmentIndex, localX);

        // 计算y值
        Vector2 localResult = EvaluateHermiteSegment(segmentIndex, t);
        Vector3 worldResult = transform.TransformPoint(localResult);
        y = worldResult.y;

        return true;
    }

    /// <summary>
    /// 根据y坐标查询曲线上对应的x坐标（垂直均匀模式）
    /// 适用于需要垂直移动的物体
    /// </summary>
    /// <param name="y">y坐标（世界坐标）</param>
    /// <param name="x">输出x坐标（世界坐标）</param>
    /// <returns>是否成功找到（y在曲线范围内）</returns>
    public bool EvaluateX(float y, out float x)
    {
        x = 0f;

        if (controlPoints.Count < 2)
        {
            Debug.LogWarning("[ArcPathComponent] 控制点少于2个，无法计算曲线");
            return false;
        }

        // 转换到局部坐标
        Vector3 localPoint = transform.InverseTransformPoint(new Vector3(0, y, 0));
        float localY = localPoint.y;

        // 找到y所在的段
        int segmentIndex = FindSegmentForY(localY);
        if (segmentIndex == -1)
            return false;

        // 二分查找参数t
        float t = BinarySearchParameterForY(segmentIndex, localY);

        // 计算x值
        Vector2 localResult = EvaluateHermiteSegment(segmentIndex, t);
        Vector3 worldResult = transform.TransformPoint(localResult);
        x = worldResult.x;

        return true;
    }

    /// <summary>
    /// 通过归一化距离 [0,1] 查询曲线上的位置（弧长均匀模式）
    /// </summary>
    /// <param name="normalizedDistance">归一化距离，0=起点，1=终点</param>
    /// <param name="position">输出世界坐标位置</param>
    /// <returns>是否成功（曲线有效）</returns>
    public bool EvaluateByNormalizedDistance(float normalizedDistance, out Vector3 position)
    {
        position = Vector3.zero;

        if (controlPoints.Count < 2)
        {
            Debug.LogWarning("[ArcPathComponent] 控制点少于2个，无法计算曲线");
            return false;
        }

        // 确保弧长缓存是最新的
        UpdateArcLengthCache();

        if (totalArcLength <= 0f)
            return false;

        // 钳制到 [0, 1] 范围
        normalizedDistance = Mathf.Clamp01(normalizedDistance);

        // 计算目标弧长
        float targetArcLength = normalizedDistance * totalArcLength;

        // 找到对应的段和局部参数
        float accumulatedLength = 0f;
        for (int seg = 0; seg < segmentArcLengths.Length; seg++)
        {
            float segmentLength = segmentArcLengths[seg];

            if (targetArcLength <= accumulatedLength + segmentLength)
            {
                // 目标在当前段内
                float localArcLength = targetArcLength - accumulatedLength;
                float localT = FindParameterByArcLength(seg, localArcLength, segmentLength);

                Vector2 localPos = EvaluateHermiteSegment(seg, localT);
                position = transform.TransformPoint(localPos);
                return true;
            }

            accumulatedLength += segmentLength;
        }

        // 边界情况：返回终点
        Vector2 lastPos = controlPoints[controlPoints.Count - 1].position;
        position = transform.TransformPoint(lastPos);
        return true;
    }

    /// <summary>
    /// 通过归一化距离查询位置和切线方向（弧长均匀模式 + 切线）
    /// </summary>
    /// <param name="normalizedDistance">归一化距离，0=起点，1=终点</param>
    /// <param name="position">输出世界坐标位置</param>
    /// <param name="tangent">输出世界坐标切线方向（已归一化）</param>
    /// <returns>是否成功（曲线有效）</returns>
    public bool EvaluateByNormalizedDistance(float normalizedDistance, out Vector3 position, out Vector3 tangent)
    {
        position = Vector3.zero;
        tangent = Vector3.right;

        if (controlPoints.Count < 2)
        {
            Debug.LogWarning("[ArcPathComponent] 控制点少于2个，无法计算曲线");
            return false;
        }

        // 确保弧长缓存是最新的
        UpdateArcLengthCache();

        if (totalArcLength <= 0f)
            return false;

        // 钳制到 [0, 1] 范围
        normalizedDistance = Mathf.Clamp01(normalizedDistance);

        // 计算目标弧长
        float targetArcLength = normalizedDistance * totalArcLength;

        // 找到对应的段和局部参数
        float accumulatedLength = 0f;
        for (int seg = 0; seg < segmentArcLengths.Length; seg++)
        {
            float segmentLength = segmentArcLengths[seg];

            if (targetArcLength <= accumulatedLength + segmentLength)
            {
                // 目标在当前段内
                float localArcLength = targetArcLength - accumulatedLength;
                float localT = FindParameterByArcLength(seg, localArcLength, segmentLength);

                Vector2 localPos = EvaluateHermiteSegment(seg, localT);
                Vector2 localTangent = EvaluateHermiteTangent(seg, localT);

                position = transform.TransformPoint(localPos);
                tangent = transform.TransformDirection(localTangent).normalized;
                return true;
            }

            accumulatedLength += segmentLength;
        }

        // 边界情况：返回终点
        Vector2 lastPos = controlPoints[controlPoints.Count - 1].position;
        Vector2 lastTangent = controlPoints[controlPoints.Count - 1].tangent;
        position = transform.TransformPoint(lastPos);
        tangent = transform.TransformDirection(lastTangent).normalized;
        return true;
    }

    /// <summary>
    /// 获取曲线总弧长
    /// </summary>
    public float GetTotalArcLength()
    {
        UpdateArcLengthCache();
        return totalArcLength;
    }

    /// <summary>
    /// 根据局部x坐标查询曲线上对应的位置（世界坐标）
    /// 专为带旋转的路径设计
    /// </summary>
    /// <param name="localX">局部x坐标</param>
    /// <param name="worldPosition">输出世界坐标位置</param>
    /// <returns>是否成功找到</returns>
    public bool EvaluateLocalX(float localX, out Vector3 worldPosition)
    {
        worldPosition = Vector3.zero;

        if (controlPoints.Count < 2)
        {
            Debug.LogWarning("[ArcPathComponent] 控制点少于2个，无法计算曲线");
            return false;
        }

        // 边界检查
        float minX = Mathf.Min(controlPoints[0].position.x, controlPoints[controlPoints.Count - 1].position.x);
        float maxX = Mathf.Max(controlPoints[0].position.x, controlPoints[controlPoints.Count - 1].position.x);

        if (localX < minX || localX > maxX)
        {
            //Debug.LogWarning($"[ArcPathComponent] EvaluateLocalX 超出范围 - localX: {localX:F2}, 范围: [{minX:F2}, {maxX:F2}]");
            return false;
        }

        // 找到x所在的段
        int segmentIndex = FindSegmentForX(localX);
        if (segmentIndex == -1)
        {
            //Debug.LogWarning($"[ArcPathComponent] EvaluateLocalX FindSegmentForX 失败 - localX: {localX:F2}");
            return false;
        }

        // 二分查找参数t
        float t = BinarySearchParameterForX(segmentIndex, localX);

        // 计算局部坐标位置
        Vector2 localResult = EvaluateHermiteSegment(segmentIndex, t);

        // 转换到世界坐标
        worldPosition = transform.TransformPoint(localResult);

        return true;
    }

    /// <summary>
    /// 根据局部y坐标查询曲线上对应的位置（世界坐标）
    /// 专为带旋转的路径设计
    /// </summary>
    /// <param name="localY">局部y坐标</param>
    /// <param name="worldPosition">输出世界坐标位置</param>
    /// <returns>是否成功找到</returns>
    public bool EvaluateLocalY(float localY, out Vector3 worldPosition)
    {
        worldPosition = Vector3.zero;

        if (controlPoints.Count < 2)
        {
            Debug.LogWarning("[ArcPathComponent] 控制点少于2个，无法计算曲线");
            return false;
        }

        // 找到y所在的段
        int segmentIndex = FindSegmentForY(localY);
        if (segmentIndex == -1)
            return false;

        // 二分查找参数t
        float t = BinarySearchParameterForY(segmentIndex, localY);

        // 计算局部坐标位置
        Vector2 localResult = EvaluateHermiteSegment(segmentIndex, t);

        // 转换到世界坐标
        worldPosition = transform.TransformPoint(localResult);

        return true;
    }

    /// <summary>
    /// 添加新的控制点
    /// </summary>
    public void AddControlPoint(Vector2 localPosition)
    {
        // 按x坐标排序插入
        int insertIndex = 0;
        for (int i = 0; i < controlPoints.Count; i++)
        {
            if (localPosition.x > controlPoints[i].position.x)
                insertIndex = i + 1;
            else
                break;
        }

        ArcPathControlPoint newPoint = new ArcPathControlPoint
        {
            position = localPosition,
            tangent = Vector2.right,
            tangentLocked = false
        };

        controlPoints.Insert(insertIndex, newPoint);

        // 自动计算切线
        newPoint.tangent = CalculateAutoTangent(insertIndex);

        // 更新相邻点的切线
        if (insertIndex > 0 && !controlPoints[insertIndex - 1].tangentLocked)
            controlPoints[insertIndex - 1].tangent = CalculateAutoTangent(insertIndex - 1);

        if (insertIndex < controlPoints.Count - 1 && !controlPoints[insertIndex + 1].tangentLocked)
            controlPoints[insertIndex + 1].tangent = CalculateAutoTangent(insertIndex + 1);

        arcLengthCacheDirty = true;
    }

    /// <summary>
    /// 删除指定索引的控制点
    /// </summary>
    public void RemoveControlPointAt(int index)
    {
        if (index < 0 || index >= controlPoints.Count)
            return;

        controlPoints.RemoveAt(index);

        // 更新相邻点的切线
        if (index > 0 && index - 1 < controlPoints.Count && !controlPoints[index - 1].tangentLocked)
            controlPoints[index - 1].tangent = CalculateAutoTangent(index - 1);

        if (index < controlPoints.Count && !controlPoints[index].tangentLocked)
            controlPoints[index].tangent = CalculateAutoTangent(index);

        arcLengthCacheDirty = true;
    }

    /// <summary>
    /// 获取控制点列表
    /// </summary>
    public List<ArcPathControlPoint> GetControlPoints()
    {
        return controlPoints;
    }

    /// <summary>
    /// 反转弧线方向（翻转控制点顺序和切线方向）
    /// </summary>
    public void ReverseDirection()
    {
        if (controlPoints.Count < 2)
            return;

        // 反转控制点列表
        controlPoints.Reverse();

        // 反转每个控制点的切线方向
        foreach (var point in controlPoints)
        {
            point.tangent = -point.tangent;
        }

        arcLengthCacheDirty = true;
    }

    /// <summary>
    /// 获取当前单调性模式
    /// </summary>
    public MonotonicityMode GetMonotonicityMode()
    {
        return monotonicityMode;
    }

    /// <summary>
    /// 设置单调性模式（仅编辑器调用）
    /// </summary>
    public void SetMonotonicityMode(MonotonicityMode mode)
    {
        monotonicityMode = mode;
    }

    /// <summary>
    /// 检测曲线的单调方向（基于首尾控制点）
    /// </summary>
    /// <returns>true=递增，false=递减</returns>
    private bool IsMonotonicIncreasing()
    {
        if (controlPoints.Count < 2)
            return true;

        if (monotonicityMode == MonotonicityMode.XMonotonic)
        {
            return controlPoints[0].position.x < controlPoints[controlPoints.Count - 1].position.x;
        }
        else if (monotonicityMode == MonotonicityMode.YMonotonic)
        {
            return controlPoints[0].position.y < controlPoints[controlPoints.Count - 1].position.y;
        }

        return true;
    }

    /// <summary>
    /// 验证控制点位置是否满足当前约束（基于局部坐标）
    /// </summary>
    /// <param name="pointIndex">要验证的控制点索引</param>
    /// <param name="newPosition">新位置（局部坐标）</param>
    /// <param name="clampedPosition">钳制后的有效位置（局部坐标）</param>
    /// <returns>位置是否被修正</returns>
    public bool ValidateAndClampPosition(int pointIndex, Vector2 newPosition, out Vector2 clampedPosition)
    {
        clampedPosition = newPosition;

        if (monotonicityMode == MonotonicityMode.Free)
            return false;

        if (pointIndex < 0 || pointIndex >= controlPoints.Count)
            return false;

        bool wasClamped = false;
        bool isIncreasing = IsMonotonicIncreasing();

        if (monotonicityMode == MonotonicityMode.XMonotonic)
        {
            // X单调：限制局部坐标保持当前方向（递增或递减）
            if (isIncreasing)
            {
                // 递增：x[i-1] < x[i] < x[i+1]
                if (pointIndex > 0)
                {
                    float minX = controlPoints[pointIndex - 1].position.x + 0.01f;
                    if (clampedPosition.x <= minX)
                    {
                        clampedPosition.x = minX;
                        wasClamped = true;
                    }
                }

                if (pointIndex < controlPoints.Count - 1)
                {
                    float maxX = controlPoints[pointIndex + 1].position.x - 0.01f;
                    if (clampedPosition.x >= maxX)
                    {
                        clampedPosition.x = maxX;
                        wasClamped = true;
                    }
                }
            }
            else
            {
                // 递减：x[i-1] > x[i] > x[i+1]
                if (pointIndex > 0)
                {
                    float maxX = controlPoints[pointIndex - 1].position.x - 0.01f;
                    if (clampedPosition.x >= maxX)
                    {
                        clampedPosition.x = maxX;
                        wasClamped = true;
                    }
                }

                if (pointIndex < controlPoints.Count - 1)
                {
                    float minX = controlPoints[pointIndex + 1].position.x + 0.01f;
                    if (clampedPosition.x <= minX)
                    {
                        clampedPosition.x = minX;
                        wasClamped = true;
                    }
                }
            }
        }
        else if (monotonicityMode == MonotonicityMode.YMonotonic)
        {
            // Y单调：限制局部坐标保持当前方向（递增或递减）
            if (isIncreasing)
            {
                // 递增：y[i-1] < y[i] < y[i+1]
                if (pointIndex > 0)
                {
                    float minY = controlPoints[pointIndex - 1].position.y + 0.01f;
                    if (clampedPosition.y <= minY)
                    {
                        clampedPosition.y = minY;
                        wasClamped = true;
                    }
                }

                if (pointIndex < controlPoints.Count - 1)
                {
                    float maxY = controlPoints[pointIndex + 1].position.y - 0.01f;
                    if (clampedPosition.y >= maxY)
                    {
                        clampedPosition.y = maxY;
                        wasClamped = true;
                    }
                }
            }
            else
            {
                // 递减：y[i-1] > y[i] > y[i+1]
                if (pointIndex > 0)
                {
                    float maxY = controlPoints[pointIndex - 1].position.y - 0.01f;
                    if (clampedPosition.y >= maxY)
                    {
                        clampedPosition.y = maxY;
                        wasClamped = true;
                    }
                }

                if (pointIndex < controlPoints.Count - 1)
                {
                    float minY = controlPoints[pointIndex + 1].position.y + 0.01f;
                    if (clampedPosition.y <= minY)
                    {
                        clampedPosition.y = minY;
                        wasClamped = true;
                    }
                }
            }
        }

        return wasClamped;
    }

    /// <summary>
    /// 检查所有控制点是否满足当前约束（基于局部坐标）
    /// </summary>
    /// <returns>违规控制点的索引列表（空列表表示全部合规）</returns>
    public List<int> CheckConstraintViolations()
    {
        List<int> violations = new List<int>();

        if (monotonicityMode == MonotonicityMode.Free || controlPoints.Count < 2)
            return violations;

        bool isIncreasing = IsMonotonicIncreasing();

        for (int i = 0; i < controlPoints.Count - 1; i++)
        {
            if (monotonicityMode == MonotonicityMode.XMonotonic)
            {
                if (isIncreasing)
                {
                    // 递增：检查 x[i] < x[i+1]
                    if (controlPoints[i].position.x >= controlPoints[i + 1].position.x)
                        violations.Add(i);
                }
                else
                {
                    // 递减：检查 x[i] > x[i+1]
                    if (controlPoints[i].position.x <= controlPoints[i + 1].position.x)
                        violations.Add(i);
                }
            }
            else if (monotonicityMode == MonotonicityMode.YMonotonic)
            {
                if (isIncreasing)
                {
                    // 递增：检查 y[i] < y[i+1]
                    if (controlPoints[i].position.y >= controlPoints[i + 1].position.y)
                        violations.Add(i);
                }
                else
                {
                    // 递减：检查 y[i] > y[i+1]
                    if (controlPoints[i].position.y <= controlPoints[i + 1].position.y)
                        violations.Add(i);
                }
            }
        }

        return violations;
    }

    /// <summary>
    /// 自动修复违反约束的控制点（基于局部坐标）
    /// </summary>
    public void AutoFixConstraintViolations()
    {
        if (monotonicityMode == MonotonicityMode.Free || controlPoints.Count < 2)
            return;

        bool isIncreasing = IsMonotonicIncreasing();

        if (monotonicityMode == MonotonicityMode.XMonotonic)
        {
            if (isIncreasing)
            {
                // 确保局部坐标 x 值严格递增
                for (int i = 1; i < controlPoints.Count; i++)
                {
                    if (controlPoints[i].position.x <= controlPoints[i - 1].position.x)
                    {
                        controlPoints[i].position.x = controlPoints[i - 1].position.x + 0.1f;
                    }
                }
            }
            else
            {
                // 确保局部坐标 x 值严格递减
                for (int i = 1; i < controlPoints.Count; i++)
                {
                    if (controlPoints[i].position.x >= controlPoints[i - 1].position.x)
                    {
                        controlPoints[i].position.x = controlPoints[i - 1].position.x - 0.1f;
                    }
                }
            }
        }
        else if (monotonicityMode == MonotonicityMode.YMonotonic)
        {
            if (isIncreasing)
            {
                // 确保局部坐标 y 值严格递增
                for (int i = 1; i < controlPoints.Count; i++)
                {
                    if (controlPoints[i].position.y <= controlPoints[i - 1].position.y)
                    {
                        controlPoints[i].position.y = controlPoints[i - 1].position.y + 0.1f;
                    }
                }
            }
            else
            {
                // 确保局部坐标 y 值严格递减
                for (int i = 1; i < controlPoints.Count; i++)
                {
                    if (controlPoints[i].position.y >= controlPoints[i - 1].position.y)
                    {
                        controlPoints[i].position.y = controlPoints[i - 1].position.y - 0.1f;
                    }
                }
            }
        }

        arcLengthCacheDirty = true;
    }

    #endregion

    #region Hermite 样条插值

    /// <summary>
    /// 计算Hermite样条上的点
    /// </summary>
    private Vector2 EvaluateHermiteSegment(int segmentIndex, float t)
    {
        if (segmentIndex < 0 || segmentIndex >= controlPoints.Count - 1)
            return Vector2.zero;

        ArcPathControlPoint p0 = controlPoints[segmentIndex];
        ArcPathControlPoint p1 = controlPoints[segmentIndex + 1];

        // Hermite基函数
        float t2 = t * t;
        float t3 = t2 * t;

        float h00 = 2f * t3 - 3f * t2 + 1f;
        float h10 = t3 - 2f * t2 + t;
        float h01 = -2f * t3 + 3f * t2;
        float h11 = t3 - t2;

        // 插值计算
        Vector2 result = h00 * p0.position +
                         h10 * p0.tangent +
                         h01 * p1.position +
                         h11 * p1.tangent;

        return result;
    }

    /// <summary>
    /// 计算Hermite样条在指定点的切线方向（一阶导数）
    /// </summary>
    private Vector2 EvaluateHermiteTangent(int segmentIndex, float t)
    {
        if (segmentIndex < 0 || segmentIndex >= controlPoints.Count - 1)
            return Vector2.right;

        ArcPathControlPoint p0 = controlPoints[segmentIndex];
        ArcPathControlPoint p1 = controlPoints[segmentIndex + 1];

        // Hermite基函数的导数
        float t2 = t * t;

        float dh00 = 6f * t2 - 6f * t;      // d/dt(2t³ - 3t² + 1)
        float dh10 = 3f * t2 - 4f * t + 1f; // d/dt(t³ - 2t² + t)
        float dh01 = -6f * t2 + 6f * t;     // d/dt(-2t³ + 3t²)
        float dh11 = 3f * t2 - 2f * t;      // d/dt(t³ - t²)

        // 切线向量（一阶导数）
        Vector2 tangent = dh00 * p0.position +
                          dh10 * p0.tangent +
                          dh01 * p1.position +
                          dh11 * p1.tangent;

        return tangent;
    }

    #endregion

    #region Evaluate(x) 实现

    /// <summary>
    /// 找到包含指定x值的曲线段
    /// </summary>
    private int FindSegmentForX(float localX)
    {
        for (int i = 0; i < controlPoints.Count - 1; i++)
        {
            float x0 = controlPoints[i].position.x;
            float x1 = controlPoints[i + 1].position.x;

            // 支持 X 递增和递减
            float minX = Mathf.Min(x0, x1);
            float maxX = Mathf.Max(x0, x1);

            if (localX >= minX && localX <= maxX)
                return i;
        }
        return -1;
    }

    /// <summary>
    /// 在指定段内二分查找参数t，使得P(t).x ≈ targetX
    /// </summary>
    private float BinarySearchParameterForX(int segmentIndex, float targetX, float epsilon = 0.0001f)
    {
        float tMin = 0f;
        float tMax = 1f;

        // 检查 X 的单调性方向
        float x0 = EvaluateHermiteSegment(segmentIndex, 0f).x;
        float x1 = EvaluateHermiteSegment(segmentIndex, 1f).x;
        bool xIncreasing = x1 > x0;

        // 二分搜索（增加迭代次数以提高精度）
        for (int iter = 0; iter < 30; iter++)
        {
            float tMid = (tMin + tMax) * 0.5f;
            Vector2 posMid = EvaluateHermiteSegment(segmentIndex, tMid);
            float xMid = posMid.x;

            // 前3次调用输出详细调试
            if (segmentIndex == 0 && iter < 3)
            {
                Debug.Log($"[ArcPathComponent] BinarySearch iter={iter}, tMid={tMid:F4}, xMid={xMid:F4}, targetX={targetX:F4}, posMid={posMid}, xIncreasing={xIncreasing}");
            }

            if (Mathf.Abs(xMid - targetX) < epsilon)
                break;

            // 根据单调性方向调整搜索范围
            if (xIncreasing)
            {
                // X 递增
                if (xMid < targetX)
                    tMin = tMid;
                else
                    tMax = tMid;
            }
            else
            {
                // X 递减
                if (xMid > targetX)
                    tMin = tMid;
                else
                    tMax = tMid;
            }
        }

        return (tMin + tMax) * 0.5f;
    }

    /// <summary>
    /// 找到包含指定y值的曲线段
    /// </summary>
    private int FindSegmentForY(float localY)
    {
        // 遍历所有段，找到y值所在的段
        for (int i = 0; i < controlPoints.Count - 1; i++)
        {
            // 采样当前段，找到y值范围
            float minY = float.MaxValue;
            float maxY = float.MinValue;

            const int samples = 10;
            for (int j = 0; j <= samples; j++)
            {
                float t = j / (float)samples;
                float y = EvaluateHermiteSegment(i, t).y;
                minY = Mathf.Min(minY, y);
                maxY = Mathf.Max(maxY, y);
            }

            if (localY >= minY && localY <= maxY)
                return i;
        }
        return -1;
    }

    /// <summary>
    /// 在指定段内二分查找参数t，使得P(t).y ≈ targetY
    /// </summary>
    private float BinarySearchParameterForY(int segmentIndex, float targetY, float epsilon = 0.001f)
    {
        float tMin = 0f;
        float tMax = 1f;

        // 二分搜索
        for (int iter = 0; iter < 20; iter++)
        {
            float tMid = (tMin + tMax) * 0.5f;
            float yMid = EvaluateHermiteSegment(segmentIndex, tMid).y;

            if (Mathf.Abs(yMid - targetY) < epsilon)
                break;

            // 需要判断y的单调性方向
            float y0 = EvaluateHermiteSegment(segmentIndex, tMin).y;
            float y1 = EvaluateHermiteSegment(segmentIndex, tMax).y;

            if (y1 > y0) // y递增
            {
                if (yMid < targetY)
                    tMin = tMid;
                else
                    tMax = tMid;
            }
            else // y递减
            {
                if (yMid > targetY)
                    tMin = tMid;
                else
                    tMax = tMid;
            }
        }

        return (tMin + tMax) * 0.5f;
    }

    #endregion

    #region 弧长参数化

    /// <summary>
    /// 计算并缓存所有段的弧长
    /// </summary>
    private void UpdateArcLengthCache()
    {
        if (!arcLengthCacheDirty)
            return;

        int segmentCount = controlPoints.Count - 1;
        if (segmentCount <= 0)
        {
            totalArcLength = 0f;
            segmentArcLengths = new float[0];
            arcLengthCacheDirty = false;
            return;
        }

        segmentArcLengths = new float[segmentCount];
        totalArcLength = 0f;

        // 对每段曲线进行数值积分
        for (int seg = 0; seg < segmentCount; seg++)
        {
            float segmentLength = CalculateSegmentArcLength(seg);
            segmentArcLengths[seg] = segmentLength;
            totalArcLength += segmentLength;
        }

        arcLengthCacheDirty = false;
    }

    /// <summary>
    /// 计算单段曲线的弧长（数值积分）
    /// </summary>
    private float CalculateSegmentArcLength(int segmentIndex)
    {
        const int samples = 20;
        float length = 0f;

        Vector2 prevPoint = EvaluateHermiteSegment(segmentIndex, 0f);

        for (int i = 1; i <= samples; i++)
        {
            float t = i / (float)samples;
            Vector2 currentPoint = EvaluateHermiteSegment(segmentIndex, t);
            length += Vector2.Distance(prevPoint, currentPoint);
            prevPoint = currentPoint;
        }

        return length;
    }

    /// <summary>
    /// 在指定段内，通过弧长反求参数t
    /// </summary>
    private float FindParameterByArcLength(int segmentIndex, float targetArcLength, float segmentTotalLength)
    {
        // 线性估计初始值
        float t = targetArcLength / segmentTotalLength;

        // 使用牛顿法迭代优化
        const int iterations = 5;
        for (int iter = 0; iter < iterations; iter++)
        {
            float currentArcLength = CalculateArcLengthUpToT(segmentIndex, t);
            float error = currentArcLength - targetArcLength;

            if (Mathf.Abs(error) < 0.001f)
                break;

            // 计算导数（切线长度）
            float dt = 0.001f;
            float arcLengthPlus = CalculateArcLengthUpToT(segmentIndex, t + dt);
            float derivative = (arcLengthPlus - currentArcLength) / dt;

            if (derivative > 0.0001f)
                t -= error / derivative;

            t = Mathf.Clamp01(t);
        }

        return t;
    }

    /// <summary>
    /// 计算从t=0到t=targetT的弧长
    /// </summary>
    private float CalculateArcLengthUpToT(int segmentIndex, float targetT)
    {
        const int samples = 10;
        float length = 0f;

        Vector2 prevPoint = EvaluateHermiteSegment(segmentIndex, 0f);

        for (int i = 1; i <= samples; i++)
        {
            float t = (i / (float)samples) * targetT;
            Vector2 currentPoint = EvaluateHermiteSegment(segmentIndex, t);
            length += Vector2.Distance(prevPoint, currentPoint);
            prevPoint = currentPoint;
        }

        return length;
    }

    #endregion

    #region 自动切线计算

    /// <summary>
    /// 为指定控制点计算自动切线（Catmull-Rom风格）
    /// </summary>
    private Vector2 CalculateAutoTangent(int index)
    {
        if (controlPoints.Count < 2)
            return Vector2.right;

        if (index == 0)
        {
            // 第一个点：切线指向下一个点
            return (controlPoints[1].position - controlPoints[0].position) * 0.5f;
        }
        else if (index == controlPoints.Count - 1)
        {
            // 最后一个点：切线来自前一个点
            int last = controlPoints.Count - 1;
            return (controlPoints[last].position - controlPoints[last - 1].position) * 0.5f;
        }
        else
        {
            // 中间点：前后两点的中点方向（Catmull-Rom）
            return (controlPoints[index + 1].position - controlPoints[index - 1].position) * 0.5f;
        }
    }

    #endregion

    #region Gizmos 可视化

    private void OnDrawGizmos()
    {
        if (!alwaysShowCurve)
            return;

        DrawCurveGizmos();
    }

    private void OnDrawGizmosSelected()
    {
        if (alwaysShowCurve)
            return;

        DrawCurveGizmos();
    }

    private void DrawCurveGizmos()
    {
        if (controlPoints.Count < 2)
            return;

        Gizmos.color = new Color(0f, 0.8f, 1f, 0.7f);

        // 绘制曲线
        for (int seg = 0; seg < controlPoints.Count - 1; seg++)
        {
            Vector3 prevPoint = Vector3.zero;

            for (int i = 0; i <= curveSegments; i++)
            {
                float t = i / (float)curveSegments;
                Vector2 localPoint = EvaluateHermiteSegment(seg, t);
                Vector3 worldPoint = transform.TransformPoint(localPoint);

                if (i > 0)
                {
                    Gizmos.DrawLine(prevPoint, worldPoint);
                }
                prevPoint = worldPoint;
            }
        }

        // 绘制控制点
        Gizmos.color = Color.cyan;
        foreach (var point in controlPoints)
        {
            Vector3 worldPos = transform.TransformPoint(point.position);
            Gizmos.DrawWireSphere(worldPos, 0.1f);
        }
    }

    #endregion

    #region Unity 回调

    private void OnValidate()
    {
        arcLengthCacheDirty = true;
    }

    #endregion
}
