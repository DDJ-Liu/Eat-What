using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 基于鼠标位置相对屏幕中心的偏移，为 GameObject 添加视差效果。
/// Adds parallax effect to GameObject based on mouse position offset from screen center.
/// </summary>
public class MouseParallax : MonoBehaviour
{
    #region Enums
    public enum ParallaxMode
    {
        Forward,  // 正向：物体跟随鼠标方向移动 / Forward: object moves with mouse
        Reverse   // 反向：物体反向于鼠标方向移动（经典深度视差效果）/ Reverse: object moves opposite to mouse (classic depth parallax)
    }

    public enum ParallaxDirection
    {
        Composite,  // 复合：同时应用水平和垂直方向 / Composite: apply both horizontal and vertical
        Horizontal, // 水平：仅应用水平方向 / Horizontal: apply horizontal only
        Vertical    // 垂直：仅应用垂直方向 / Vertical: apply vertical only
    }
    #endregion

    #region Serialized Fields
    [Header("视差效果设置 / Parallax Settings")]
    [Tooltip("是否启用鼠标视差效果 / Enable mouse parallax effect")]
    [SerializeField] public bool enableParallax = true;

    [Header("区域限制 / Area Restriction")]
    [Tooltip("可选：限制视差触发区域的 Collider2D（优先级高于 RectTransform）/ Optional: Collider2D to restrict parallax area (takes priority over RectTransform)")]
    [SerializeField] private Collider2D areaCollider;

    [Tooltip("可选：限制视差触发区域的 RectTransform / Optional: RectTransform to restrict parallax area")]
    [SerializeField] private RectTransform areaRect;

    [Header("视差参数 / Parallax Parameters")]
    [Tooltip("视差方向：Composite=水平+垂直，Horizontal=仅水平，Vertical=仅垂直 / Direction: Composite=both, Horizontal=horizontal only, Vertical=vertical only")]
    [SerializeField] private ParallaxDirection parallaxDirection = ParallaxDirection.Composite;

    [Tooltip("视差类型：Forward=跟随鼠标移动，Reverse=反向移动 / Parallax type: Forward=moves with mouse, Reverse=moves opposite")]
    [SerializeField] private ParallaxMode parallaxMode = ParallaxMode.Forward;

    [Tooltip("视差强度（世界单位）/ Parallax strength in world units")]
    [SerializeField] [Range(0f, 10f)] private float parallaxStrength = 1f;

    [Tooltip("视差平滑速度 / Parallax smoothing speed")]
    [SerializeField] [Range(0.1f, 20f)] private float parallaxSmoothSpeed = 8f;
    #endregion

    #region Private State
    private Vector3 _basePosition;
    private Vector3 _currentParallaxOffset = Vector3.zero;
    #endregion

    #region Unity Lifecycle
    void Start()
    {
        // 记录初始位置作为基准位置 / Record initial position as base position
        _basePosition = transform.position;
    }

    void OnEnable()
    {
        // 重置视差偏移，防止启用时出现跳跃
        // Reset parallax offset to prevent visual pop on enable
        _currentParallaxOffset = Vector3.zero;
        _basePosition = transform.position;
    }

    void LateUpdate()
    {
        UpdateParallax();
    }
    #endregion

    #region Core Logic
    private void UpdateParallax()
    {
        if (!enableParallax)
        {
            transform.position = _basePosition;
            return;
        }

        // 检查鼠标输入是否可用 / Check if mouse input is available
        if (Mouse.current == null)
        {
            Debug.LogError($"[MouseParallax] Mouse input unavailable on {gameObject.name}", this);
            enableParallax = false;
            return;
        }

        // 检查鼠标是否在限制区域内 / Check if mouse is within restricted area
        if (!IsMouseInArea())
        {
            // 鼠标不在区域内，平滑回到基准位置 / Mouse not in area, smoothly return to base position
            _currentParallaxOffset = Vector3.Lerp(
                _currentParallaxOffset,
                Vector3.zero,
                parallaxSmoothSpeed * Time.deltaTime
            );
            transform.position = _basePosition + _currentParallaxOffset;
            return;
        }

        Vector3 targetParallaxOffset = CalculateParallaxOffset();

        _currentParallaxOffset = Vector3.Lerp(
            _currentParallaxOffset,
            targetParallaxOffset,
            parallaxSmoothSpeed * Time.deltaTime
        );

        transform.position = _basePosition + _currentParallaxOffset;
    }

    private bool IsMouseInArea()
    {
        // 如果没有设置区域限制，默认为整个屏幕 / If no area restriction, default to entire screen
        if (areaCollider == null && areaRect == null)
            return true;

        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);

        // 优先使用 Collider2D / Prioritize Collider2D
        if (areaCollider != null)
        {
            return areaCollider.OverlapPoint(mouseWorldPos);
        }

        // 使用 RectTransform / Use RectTransform
        if (areaRect != null)
        {
            return RectTransformUtility.RectangleContainsScreenPoint(areaRect, mouseScreenPos, Camera.main);
        }

        return true;
    }

    private Vector3 CalculateParallaxOffset()
    {
        // 获取鼠标屏幕位置 / Get mouse screen position
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();

        // 计算屏幕中心 / Calculate screen center
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);

        // 计算鼠标相对屏幕中心的偏移 / Calculate mouse offset from screen center
        Vector2 mouseOffset = mouseScreenPos - screenCenter;

        // 归一化到 -1 到 +1 范围（与分辨率无关）/ Normalize to -1 to +1 (resolution independent)
        Vector2 normalizedOffset = new Vector2(
            mouseOffset.x / (Screen.width / 2f),
            mouseOffset.y / (Screen.height / 2f)
        );

        // 根据方向模式应用偏移 / Apply offset based on direction mode
        switch (parallaxDirection)
        {
            case ParallaxDirection.Horizontal:
                normalizedOffset.y = 0f; // 仅水平 / Horizontal only
                break;
            case ParallaxDirection.Vertical:
                normalizedOffset.x = 0f; // 仅垂直 / Vertical only
                break;
            case ParallaxDirection.Composite:
            default:
                // 保持两个方向 / Keep both directions
                break;
        }

        // 转换为世界空间偏移 / Convert to world space offset
        Vector3 parallaxOffset = new Vector3(
            normalizedOffset.x * parallaxStrength,
            normalizedOffset.y * parallaxStrength,
            0f // 2D游戏，Z轴为0 / 2D game, Z axis is 0
        );

        // 应用视差模式 / Apply parallax mode
        if (parallaxMode == ParallaxMode.Reverse)
        {
            parallaxOffset = -parallaxOffset;
        }

        return parallaxOffset;
    }

    /// <summary>
    /// 更新基准位置（当其他组件修改了位置时调用）
    /// Update base position (call this when other components modify position)
    /// </summary>
    public void UpdateBasePosition(Vector3 newBasePosition)
    {
        _basePosition = newBasePosition;
    }
    #endregion
}
