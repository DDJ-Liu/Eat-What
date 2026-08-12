using UnityEngine;
using System.Collections;

/// <summary>
/// 子物体尺寸获取模式
/// </summary>
public enum ChildSizeMode
{
    UniformCellSize,    // 统一尺寸：所有子物体使用相同的 cellSize
    ColliderBounds      // 碰撞体尺寸：从子物体的 Collider2D 读取
}

/// <summary>
/// 子物体尺寸数据
/// </summary>
public struct ChildSizeInfo
{
    public Vector2 size;
    public bool isValid;

    public static ChildSizeInfo Invalid => new ChildSizeInfo { isValid = false };
}

/// <summary>
/// 布局系统抽象基类
/// 提供通用的布局管理功能，子类实现具体的布局算法
/// </summary>
public abstract class Layout : MonoBehaviour
{
    // ==================== 共通字段 ====================

    [Header("基础布局设置")]
    [Tooltip("是否在Start时自动布局")]
    [SerializeField] protected bool layoutOnStart = true;

    [Header("子物体尺寸模式")]
    [Tooltip("子物体尺寸获取方式")]
    [SerializeField] protected ChildSizeMode childSizeMode = ChildSizeMode.UniformCellSize;

    [Tooltip("统一尺寸模式下的单元格大小")]
    [SerializeField] protected Vector2 cellSize = new Vector2(1f, 1f);

    [Header("动画设置")]
    [Tooltip("是否使用Lerp平滑过渡到目标位置")]
    [SerializeField] protected bool useLerp = false;

    [Tooltip("Lerp动画持续时间（秒）")]
    [SerializeField] protected float lerpDuration = 0.5f;

    [Tooltip("子物体之间的动画延迟（秒），产生波浪效果")]
    [SerializeField] protected float lerpStagger = 0.05f;

    // ==================== 生命周期 ====================

    protected virtual void Awake()
    {
        ValidateFields();
    }

    protected virtual void Start()
    {
        if (layoutOnStart)
        {
            ArrangeChildren();
        }
    }

    // ==================== 验证机制 ====================

    /// <summary>
    /// 验证并修复无效的字段值（模板方法）
    /// </summary>
    protected virtual void ValidateFields()
    {
        // 基类验证通用字段
        if (float.IsNaN(lerpDuration) || float.IsInfinity(lerpDuration) || lerpDuration < 0)
        {
            Debug.LogError($"[{GetType().Name}] {gameObject.name}: lerpDuration 无效 ({lerpDuration})，重置为 0.5");
            lerpDuration = 0.5f;
        }

        if (float.IsNaN(lerpStagger) || float.IsInfinity(lerpStagger) || lerpStagger < 0)
        {
            Debug.LogError($"[{GetType().Name}] {gameObject.name}: lerpStagger 无效 ({lerpStagger})，重置为 0.05");
            lerpStagger = 0.05f;
        }

        if (childSizeMode == ChildSizeMode.UniformCellSize)
        {
            if (cellSize.x <= 0 || cellSize.y <= 0 ||
                float.IsNaN(cellSize.x) || float.IsNaN(cellSize.y))
            {
                Debug.LogError($"[{GetType().Name}] {gameObject.name}: cellSize 无效 ({cellSize})，重置为 (1, 1)");
                cellSize = Vector2.one;
            }
        }

        // 子类可继续验证特定字段
        ValidateSpecificFields();
    }

    /// <summary>
    /// 子类验证特定字段（钩子方法）
    /// </summary>
    protected virtual void ValidateSpecificFields() { }

    // ==================== 子物体尺寸获取 ====================

    /// <summary>
    /// 获取子物体尺寸（策略模式）
    /// </summary>
    protected ChildSizeInfo GetChildSize(Transform child, int childIndex)
    {
        switch (childSizeMode)
        {
            case ChildSizeMode.UniformCellSize:
                return new ChildSizeInfo { size = cellSize, isValid = true };

            case ChildSizeMode.ColliderBounds:
                return GetColliderSize(child, childIndex);

            default:
                Debug.LogError($"[{GetType().Name}] 未知的 ChildSizeMode: {childSizeMode}");
                return ChildSizeInfo.Invalid;
        }
    }

    /// <summary>
    /// 从 Collider2D 获取尺寸
    /// </summary>
    private ChildSizeInfo GetColliderSize(Transform child, int childIndex)
    {
        Collider2D collider = child.GetComponent<Collider2D>();

        if (collider == null)
        {
            Debug.LogError($"[{GetType().Name}] {gameObject.name}: 子物体 [{childIndex}] '{child.name}' " +
                          $"没有 Collider2D 组件，但使用了 ColliderBounds 模式");
            return ChildSizeInfo.Invalid;
        }

        Vector2 size = collider.bounds.size;

        // 验证尺寸有效性
        if (size.x <= 0 || size.y <= 0 ||
            float.IsNaN(size.x) || float.IsNaN(size.y) ||
            float.IsInfinity(size.x) || float.IsInfinity(size.y))
        {
            Debug.LogError($"[{GetType().Name}] {gameObject.name}: 子物体 [{childIndex}] '{child.name}' " +
                          $"的 Collider2D 尺寸无效: {size}");
            return ChildSizeInfo.Invalid;
        }

        return new ChildSizeInfo { size = size, isValid = true };
    }

    // ==================== 布局算法（抽象方法） ====================

    /// <summary>
    /// 执行布局 - 主入口（模板方法）
    /// </summary>
    public void ArrangeChildren()
    {
        // 中断正在播放的动画
        StopAllCoroutines();

        int childCount = transform.childCount;

        if (childCount == 0)
        {
            Debug.LogWarning($"[{GetType().Name}] {gameObject.name}: 没有子物体需要布局");
            return;
        }

        // 调用子类实现的布局算法
        PerformLayout();
    }

    /// <summary>
    /// 执行具体布局算法（子类实现）
    /// </summary>
    protected abstract void PerformLayout();

    // ==================== 子物体定位（共通方法） ====================

    /// <summary>
    /// 将子物体放置到指定位置和旋转
    /// </summary>
    protected void PlaceChild(Transform child, Vector3 targetLocalPosition, Quaternion targetLocalRotation, int globalChildIndex)
    {
        // 验证位置有效性
        if (float.IsNaN(targetLocalPosition.x) || float.IsNaN(targetLocalPosition.y) ||
            float.IsInfinity(targetLocalPosition.x) || float.IsInfinity(targetLocalPosition.y))
        {
            Debug.LogError($"[{GetType().Name}] {gameObject.name}: 子物体 [{globalChildIndex}] 计算出的位置无效: {targetLocalPosition}");
            return;
        }

        // 根据 useLerp 决定是直接设置还是启动动画
        if (useLerp)
        {
            float delay = globalChildIndex * lerpStagger;
            StartCoroutine(LerpChildCoroutine(child, targetLocalPosition, targetLocalRotation, delay));
        }
        else
        {
            child.localPosition = targetLocalPosition;
            child.localRotation = targetLocalRotation;
        }
    }

    /// <summary>
    /// Lerp动画协程
    /// </summary>
    private IEnumerator LerpChildCoroutine(Transform child, Vector3 targetPos, Quaternion targetRot, float delay)
    {
        // 等待stagger延迟
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        // 记录起始状态
        Vector3 startPos = child.localPosition;
        Quaternion startRot = child.localRotation;
        float elapsed = 0f;

        // 线性Lerp插值
        while (elapsed < lerpDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / lerpDuration);

            child.localPosition = Vector3.Lerp(startPos, targetPos, t);
            child.localRotation = Quaternion.Lerp(startRot, targetRot, t);

            yield return null;
        }

        // 确保精确到达目标
        child.localPosition = targetPos;
        child.localRotation = targetRot;
    }

    // ==================== 公共API ====================

    /// <summary>
    /// 设置子物体尺寸模式并重新布局
    /// </summary>
    public void SetChildSizeMode(ChildSizeMode mode, Vector2 newCellSize)
    {
        childSizeMode = mode;
        if (mode == ChildSizeMode.UniformCellSize)
        {
            cellSize = newCellSize;
        }
        ArrangeChildren();
    }

    /// <summary>
    /// 设置统一单元格尺寸并重新布局
    /// </summary>
    public void SetCellSize(Vector2 newCellSize)
    {
        cellSize = newCellSize;
        if (childSizeMode == ChildSizeMode.UniformCellSize)
        {
            ArrangeChildren();
        }
    }
}
