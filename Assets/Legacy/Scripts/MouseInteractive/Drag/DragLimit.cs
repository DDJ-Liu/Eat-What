using UnityEngine;

public class DragLimit : MonoBehaviour
{
    public enum LimitType { Outer, Inner }

    [Tooltip("Outer: drag物体整体被包含在限制范围内，边界不超出限制；Inner: 限制区域在drag内部，drag必须始终完整包裹住限制区域")]
    public LimitType limitType = LimitType.Outer;
    [Tooltip("允许超出时：Outer模式只限制drag中心点在范围内；Inner模式只要求限制中心点处于drag面积内")]
    public bool allowExceed = false;

    private RectTransform _rect;
    private BoxCollider2D _col;
    private bool _initialized;

    private void Awake()
    {
        CacheComponents();
    }

    private void CacheComponents()
    {
        if (_initialized) return;
        TryGetComponent(out _rect);
        TryGetComponent(out _col);
        _initialized = true;
    }

    /// <summary>
    /// 获取自身的边界信息（中心点、半宽、半高）。
    /// 优先使用 RectTransform，fallback 到 BoxCollider2D。
    /// </summary>
    public (Vector2 center, float halfW, float halfH) GetBounds()
    {
        CacheComponents();

        if (_rect != null)
        {
            return (
                (Vector2)_rect.position,
                _rect.rect.width / 2f,
                _rect.rect.height / 2f
            );
        }

        if (_col != null)
        {
            Vector2 center = (Vector2)transform.TransformPoint(_col.offset);
            Vector3 scale = transform.lossyScale;
            return (center, _col.size.x * Mathf.Abs(scale.x) / 2f, _col.size.y * Mathf.Abs(scale.y) / 2f);
        }

        Debug.LogError($"[DragLimit] {gameObject.name} 上既没有 RectTransform 也没有 BoxCollider2D，无法获取边界");
        return (Vector2.zero, 0f, 0f);
    }

    /// <summary>
    /// 计算给定 drag 物体在此限制范围内的 clamped 位置。
    /// </summary>
    /// <param name="targetPos">drag 物体的目标世界坐标（中心点）</param>
    /// <param name="dragHalfSize">drag 物体的半尺寸 (halfWidth, halfHeight)</param>
    /// <returns>(clampedPos, hitBoundary, minX, maxX, minY, maxY)</returns>
    public (Vector3 clampedPos, bool hitBoundary, float minX, float maxX, float minY, float maxY) ClampPosition(Vector3 targetPos, Vector2 dragHalfSize)
    {
        var (limitCenter, halfLimitW, halfLimitH) = GetBounds();

        float L_left   = limitCenter.x - halfLimitW;
        float L_right  = limitCenter.x + halfLimitW;
        float L_bottom = limitCenter.y - halfLimitH;
        float L_top    = limitCenter.y + halfLimitH;

        float halfW = dragHalfSize.x;
        float halfH = dragHalfSize.y;

        float minX, maxX, minY, maxY;

        if (limitType == LimitType.Outer)
        {
            if (allowExceed)
            {
                // Outer + exceed: 只 clamp drag 中心点到 limit 原始边界
                minX = L_left;
                maxX = L_right;
                minY = L_bottom;
                maxY = L_top;
            }
            else
            {
                // Outer: drag 整体被包含在 limit 内，边界向内收缩 drag 半尺寸
                minX = L_left + halfW;
                maxX = L_right - halfW;
                minY = L_bottom + halfH;
                maxY = L_top - halfH;

                // drag 比 limit 大时，锁定到 limit 中心
                if (minX > maxX)
                {
                    float mid = (minX + maxX) / 2f;
                    minX = mid;
                    maxX = mid;
                }
                if (minY > maxY)
                {
                    float mid = (minY + maxY) / 2f;
                    minY = mid;
                    maxY = mid;
                }
            }
        }
        else // Inner
        {
            if (allowExceed)
            {
                // Inner + exceed: limit 中心点必须始终处于 drag 面积内
                minX = limitCenter.x - halfW;
                maxX = limitCenter.x + halfW;
                minY = limitCenter.y - halfH;
                maxY = limitCenter.y + halfH;
            }
            else
            {
                // Inner: drag 必须完整包裹住 limit
                // drag 左边缘 ≤ limit 左边缘 → P.x ≤ L_left + halfW
                // drag 右边缘 ≥ limit 右边缘 → P.x ≥ L_right - halfW
                minX = L_right - halfW;
                maxX = L_left + halfW;
                minY = L_top - halfH;
                maxY = L_bottom + halfH;

                // drag 太小无法包裹 limit 时，锁定到 limit 中心
                if (minX > maxX)
                {
                    float mid = (minX + maxX) / 2f;
                    minX = mid;
                    maxX = mid;
                }
                if (minY > maxY)
                {
                    float mid = (minY + maxY) / 2f;
                    minY = mid;
                    maxY = mid;
                }
            }
        }

        Vector3 clamped = new Vector3(
            Mathf.Clamp(targetPos.x, minX, maxX),
            Mathf.Clamp(targetPos.y, minY, maxY),
            targetPos.z
        );

        return (clamped, (Vector3)clamped != targetPos, minX, maxX, minY, maxY);
    }

    /// <summary>
    /// 计算 drag 物体当前位置在此限制范围内的归一化位置 (0-1)。
    /// </summary>
    public Vector2 GetNormalizedPosition(Vector3 currentPos, Vector2 dragHalfSize)
    {
        var (limitCenter, halfLimitW, halfLimitH) = GetBounds();

        float L_left   = limitCenter.x - halfLimitW;
        float L_right  = limitCenter.x + halfLimitW;
        float L_bottom = limitCenter.y - halfLimitH;
        float L_top    = limitCenter.y + halfLimitH;

        float halfW = dragHalfSize.x;
        float halfH = dragHalfSize.y;

        float minX, maxX, minY, maxY;

        if (limitType == LimitType.Inner)
        {
            if (allowExceed)
            {
                minX = limitCenter.x - halfW;
                maxX = limitCenter.x + halfW;
                minY = limitCenter.y - halfH;
                maxY = limitCenter.y + halfH;
            }
            else
            {
                minX = L_right - halfW;
                maxX = L_left + halfW;
                minY = L_top - halfH;
                maxY = L_bottom + halfH;

                if (minX > maxX) { float mid = (minX + maxX) / 2f; minX = mid; maxX = mid; }
                if (minY > maxY) { float mid = (minY + maxY) / 2f; minY = mid; maxY = mid; }
            }
        }
        else // Outer
        {
            if (allowExceed)
            {
                minX = L_left; maxX = L_right;
                minY = L_bottom; maxY = L_top;
            }
            else
            {
                minX = L_left + halfW; maxX = L_right - halfW;
                minY = L_bottom + halfH; maxY = L_top - halfH;
                if (minX > maxX) { float mid = (minX + maxX) / 2f; minX = mid; maxX = mid; }
                if (minY > maxY) { float mid = (minY + maxY) / 2f; minY = mid; maxY = mid; }
            }
        }

        float normalizedX = Mathf.Approximately(maxX, minX) ? 0f : Mathf.Clamp01((currentPos.x - minX) / (maxX - minX));
        float normalizedY = Mathf.Approximately(maxY, minY) ? 0f : Mathf.Clamp01((currentPos.y - minY) / (maxY - minY));

        return new Vector2(normalizedX, normalizedY);
    }
}
