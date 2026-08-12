using UnityEngine;

/// <summary>
/// 为外部 Transform 提供 ArcLayout 定位计算的适配器组件。
/// 读取同级 ArcLayout 配置，计算世界空间位置和旋转。
/// 不修改 ArcLayout 本身，保持其可复用性。
/// </summary>
public class ArcLayoutStrategy : MonoBehaviour
{
    [Header("引用")]
    [Tooltip("弧形布局配置源，留空则自动获取同级组件")]
    [SerializeField] private ArcLayout arcLayout;

    private void Awake()
    {
        if (arcLayout == null)
        {
            arcLayout = GetComponent<ArcLayout>();
            if (arcLayout == null)
            {
                Debug.LogError($"ArcLayoutStrategy on {gameObject.name} 找不到 ArcLayout 组件");
            }
        }
    }

    /// <summary>
    /// 计算指定索引位置的世界空间坐标和旋转
    /// </summary>
    /// <param name="index">元素索引（从0开始）</param>
    /// <param name="totalCount">总元素数量</param>
    /// <returns>世界空间位置和旋转</returns>
    public (Vector3 position, Quaternion rotation) GetWorldPositionAt(int index, int totalCount)
    {
        if (arcLayout == null)
        {
            Debug.LogWarning("ArcLayoutStrategy: arcLayout 未设置");
            return (transform.position, transform.rotation);
        }

        if (totalCount <= 0 || index < 0 || index >= totalCount)
        {
            Debug.LogWarning($"ArcLayoutStrategy: 无效索引 {index} / {totalCount}");
            return (transform.position, transform.rotation);
        }

        Debug.Log($"[ArcLayoutStrategy] 计算位置 index={index}, totalCount={totalCount}, ArcLayout位置={transform.position}");

        // 使用反射访问 ArcLayout 的私有字段，带有安全的 fallback 默认值
        var arcLayoutType = typeof(ArcLayout);
        float radius = GetFieldValue<float>("radius", 2f);
        float arcAngle = GetFieldValue<float>("arcAngle", 180f);
        bool enableDeadZone = GetFieldValue<bool>("enableDeadZone", false);
        float deadZoneStart = GetFieldValue<float>("deadZoneStart", 0f);
        float deadZoneEnd = GetFieldValue<float>("deadZoneEnd", 0f);
        bool randomFloat = GetFieldValue<bool>("randomFloat", false);
        float floatAmplitude = GetFieldValue<float>("floatAmplitude", 0f);
        bool adjustRotation = GetFieldValue<bool>("adjustRotation", true);
        float defaultDirection = GetFieldValue<float>("defaultDirection", 0f);
        float rotationOffset = GetFieldValue<float>("rotationOffset", 0f);
        ArrangementDirection arrangementDirection = GetFieldValue<ArrangementDirection>("arrangementDirection", ArrangementDirection.LeftToRight);
        SpacingMode spacingMode = GetFieldValue<SpacingMode>("spacingMode", SpacingMode.Uniform);
        float fixedAngleSpacing = GetFieldValue<float>("fixedAngleSpacing", 15f);

        // Layer 3: 计算前验证关键参数
        if (float.IsNaN(radius) || float.IsInfinity(radius) || radius <= 0)
        {
            Debug.LogError($"[ArcLayoutStrategy] radius 无效: {radius}，使用回退位置");
            return (transform.position, transform.rotation);
        }

        if (float.IsNaN(arcAngle) || float.IsInfinity(arcAngle))
        {
            Debug.LogError($"[ArcLayoutStrategy] arcAngle 无效: {arcAngle}，使用回退位置");
            return (transform.position, transform.rotation);
        }

        if (float.IsNaN(fixedAngleSpacing) || float.IsInfinity(fixedAngleSpacing))
        {
            Debug.LogWarning($"[ArcLayoutStrategy] fixedAngleSpacing 无效: {fixedAngleSpacing}，钳制到最小值");
            fixedAngleSpacing = 0.1f;
        }

        // 钳制 fixedAngleSpacing 最小值
        fixedAngleSpacing = Mathf.Max(0.1f, fixedAngleSpacing);

        // 确保 arcAngle 为正数
        arcAngle = Mathf.Abs(arcAngle);
        if (arcAngle < 0.01f)
        {
            Debug.LogWarning($"[ArcLayoutStrategy] arcAngle 过小 ({arcAngle})，使用默认值 180");
            arcAngle = 180f;
        }

        Debug.Log($"[ArcLayoutStrategy] 读取到配置: radius={radius}, arcAngle={arcAngle}(已转正), enableDeadZone={enableDeadZone}");

        // 计算弧的相对角度范围
        float arcHalf = arcAngle / 2f;
        float arcMin = -arcHalf;
        float arcMax = arcHalf;

        // 判断死区是否生效
        bool useDeadZone = enableDeadZone && deadZoneStart < deadZoneEnd
            && deadZoneStart < arcMax && deadZoneEnd > arcMin;

        float relativeAngle;

        if (!useDeadZone)
        {
            // 全弧排列
            float[] angles = ArcLayout.CalculateAngles(arcMin, arcMax, totalCount, spacingMode, fixedAngleSpacing);
            int angleIndex = ArcLayout.MapChildIndexToAngleIndex(index, totalCount, arrangementDirection);
            relativeAngle = angles[angleIndex];
        }
        else
        {
            // 有死区：分段分配
            float dzStart = Mathf.Max(deadZoneStart, arcMin);
            float dzEnd = Mathf.Min(deadZoneEnd, arcMax);

            float rightLen = Mathf.Max(0f, dzStart - arcMin);
            float leftLen = Mathf.Max(0f, arcMax - dzEnd);
            float totalLen = rightLen + leftLen;

            if (totalLen <= 0f)
            {
                Debug.LogWarning("ArcLayoutStrategy: 死区完全覆盖弧形范围");
                return (transform.position, transform.rotation);
            }

            int rightCount, leftCount;

            if (spacingMode == SpacingMode.Fixed)
            {
                // 固定间隔模式：按容量分配
                int maxRightCount = rightLen > 0 ? Mathf.FloorToInt(rightLen / fixedAngleSpacing) + 1 : 0;
                int maxLeftCount = leftLen > 0 ? Mathf.FloorToInt(leftLen / fixedAngleSpacing) + 1 : 0;

                rightCount = Mathf.Min(totalCount, maxRightCount);
                leftCount = totalCount - rightCount;
            }
            else
            {
                // 按段长度比例分配
                rightCount = Mathf.FloorToInt(totalCount * (rightLen / totalLen));
                leftCount = totalCount - rightCount;
            }

            if (rightLen <= 0f) { leftCount = totalCount; rightCount = 0; }
            else if (leftLen <= 0f) { rightCount = totalCount; leftCount = 0; }

            // 判断当前索引属于哪个段
            if (index < rightCount)
            {
                float[] angles = ArcLayout.CalculateAngles(arcMin, dzStart, rightCount, spacingMode, fixedAngleSpacing);
                int angleIndex = ArcLayout.MapChildIndexToAngleIndex(index, rightCount, arrangementDirection);
                relativeAngle = angles[angleIndex];
            }
            else
            {
                float[] angles = ArcLayout.CalculateAngles(dzEnd, arcMax, leftCount, spacingMode, fixedAngleSpacing);
                int angleIndex = ArcLayout.MapChildIndexToAngleIndex(index - rightCount, leftCount, arrangementDirection);
                relativeAngle = angles[angleIndex];
            }
        }

        // 转换为 Unity 角度
        float unityAngle = 90f + relativeAngle;
        float radian = unityAngle * Mathf.Deg2Rad;

        // 计算本地位置
        float r = radius + (randomFloat ? Random.Range(-floatAmplitude, floatAmplitude) : 0f);
        Vector3 localPosition = new Vector3(
            Mathf.Cos(radian) * r,
            Mathf.Sin(radian) * r,
            0f
        );

        // 计算本地旋转
        Quaternion localRotation = Quaternion.identity;
        if (adjustRotation)
        {
            Vector3 directionToCenter = -localPosition.normalized;
            float rotationAngle = Mathf.Atan2(directionToCenter.y, directionToCenter.x) * Mathf.Rad2Deg;
            localRotation = Quaternion.Euler(0, 0, rotationAngle - defaultDirection + rotationOffset);
        }

        // 转换为世界空间
        Vector3 worldPosition = transform.TransformPoint(localPosition);
        Quaternion worldRotation = transform.rotation * localRotation;

        // Layer 4: 计算后验证输出
        if (float.IsNaN(worldPosition.x) || float.IsNaN(worldPosition.y) || float.IsNaN(worldPosition.z) ||
            float.IsInfinity(worldPosition.x) || float.IsInfinity(worldPosition.y) || float.IsInfinity(worldPosition.z))
        {
            Debug.LogError($"[ArcLayoutStrategy] 计算出的世界位置无效: {worldPosition}\n" +
                           $"  localPosition={localPosition}, radius={radius}, relativeAngle={relativeAngle}\n" +
                           $"  transform.position={transform.position}");
            return (transform.position, transform.rotation);
        }

        Debug.Log($"[ArcLayoutStrategy] 本地位置={localPosition}, 世界位置={worldPosition}, 相对角度={relativeAngle}");

        return (worldPosition, worldRotation);
    }

    /// <summary>
    /// 使用反射获取 ArcLayout 私有字段值，带有 NaN/Infinity 验证和异常处理
    /// </summary>
    private T GetFieldValue<T>(string fieldName, T fallbackValue = default(T))
    {
        try
        {
            if (arcLayout == null)
            {
                Debug.LogWarning($"[ArcLayoutStrategy] arcLayout 为 null，字段 '{fieldName}' 使用回退值");
                return fallbackValue;
            }

            var field = typeof(ArcLayout).GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            if (field == null)
            {
                Debug.LogWarning($"[ArcLayoutStrategy] 找不到字段 '{fieldName}'，使用回退值");
                return fallbackValue;
            }

            var rawValue = field.GetValue(arcLayout);
            if (rawValue == null)
            {
                Debug.LogWarning($"[ArcLayoutStrategy] 字段 '{fieldName}' 值为 null，使用回退值");
                return fallbackValue;
            }

            // 对 float 类型进行 NaN/Infinity 检查
            if (typeof(T) == typeof(float))
            {
                float floatValue = (float)rawValue;
                if (float.IsNaN(floatValue) || float.IsInfinity(floatValue))
                {
                    Debug.LogError($"[ArcLayoutStrategy] 字段 '{fieldName}' 值无效 ({floatValue})，使用回退值 ({fallbackValue})");
                    return fallbackValue;
                }
            }

            return (T)rawValue;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ArcLayoutStrategy] 反射获取字段 '{fieldName}' 失败: {e.Message}，使用回退值");
            return fallbackValue;
        }
    }

    /// <summary>
    /// 获取 ArcLayout 的动画配置
    /// </summary>
    public bool UseLerp => GetFieldValue<bool>("useLerp");
    public float LerpDuration => GetFieldValue<float>("lerpDuration");
    public float LerpStagger => GetFieldValue<float>("lerpStagger");
}
