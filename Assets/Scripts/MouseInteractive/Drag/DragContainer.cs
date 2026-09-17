using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(BoxCollider2D))]
public class DragContainer : MonoBehaviour
{
    public BoxCollider2D col;
    public Vector3 startPos;

    public enum DragMode { Vertical, Horizontal, Composite };
    public DragMode mode;

    public enum SizeSource { BoxCollider2D, RectTransform }

    [Header("Size Source")]
    [Tooltip("计算自身体积的数据源：默认沿用 BoxCollider2D；切换 RectTransform 后可通过 pivot 表达非中心锚点，collider size/offset 将由 rect 自动同步")]
    public SizeSource sizeSource = SizeSource.BoxCollider2D;

    [Tooltip("RectTransform 模式下使用的 rect；留空则使用本物体的 RectTransform")]
    public RectTransform sizeRect;

    private RectTransform _cachedRect;
    private RectTransform ActiveRect
    {
        get
        {
            if (sizeRect != null) return sizeRect;
            if (_cachedRect == null) _cachedRect = GetComponent<RectTransform>();
            return _cachedRect;
        }
    }

    [Header("Local Space")]
    [Tooltip("启用后基于 localPosition 运算，支持父物体旋转后仍沿本地轴拖拽")]
    public bool useLocalSpace = false;

    public enum LimitMode { DragLimit, Manual }
    public LimitMode limitMode = LimitMode.DragLimit;
    public UnityEvent<DragContainer> manualLimitEvent;
    public float leftLimit;
    public float rightLimit;
    public float topLimit;
    public float bottomLimit;
    public DragLimit dragLimit;

    [Header("运行时边界（只读）")]
    [SerializeField] private float _limitMinX;
    [SerializeField] private float _limitMaxX;
    [SerializeField] private float _limitMinY;
    [SerializeField] private float _limitMaxY;

    public float LimitMinX => _limitMinX;
    public float LimitMaxX => _limitMaxX;
    public float LimitMinY => _limitMinY;
    public float LimitMaxY => _limitMaxY;

    // ==================== 扩展1：惯性效果 ====================
    [Header("Inertia Settings")]
    public bool enableInertia = true;
    [Range(0f, 1f)] public float inertiaDeceleration = 0.95f;
    [Range(0f, 100f)] public float maxInertiaSpeed = 50f;
    public float minInertiaSpeed = 1f;
    public AnimationCurve inertiaCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    private Vector3 velocity = Vector3.zero;
    private Queue<Vector3> positionHistory = new Queue<Vector3>();
    private int historySize = 5;
    private bool isInertiaMoving = false;
    private Vector3 inertiaStartPosition;
    private float inertiaTimer = 0f;
    private float inertiaDuration = 0.5f;

    // ==================== 扩展2：边界弹性效果 ====================
    [Header("Elastic Boundary Settings")]
    public bool enableElasticBoundary = false;
    public float elasticFactor = 1f;
    public float elasticReturnSpeed = 8f;
    public float maxElasticOffset = 3f;

    private bool isElasticReturning = false;
    private Vector3 elasticOffset = Vector3.zero;
    private Vector3 elasticTargetPosition;

    // ==================== 扩展3：拖拽事件回调 ====================
    [Header("Drag Events")]
    public UnityEvent onDragStartEvent;
    public UnityEvent<Vector3> onDragUpdateEvent;    // 参数：当前拖拽速度
    public UnityEvent<Vector3> onDragEndEvent;       // 参数：拖拽结束时的速度
    public UnityEvent<Vector3> onDragPositionChanged; // 参数：当前位置变化量
    public UnityEvent<bool> onInertiaStateChanged;   // 参数：惯性状态（开始/结束）
    public UnityEvent<Vector3> onElasticBoundaryHit; // 参数：弹性偏移量

    private Vector3 lastPosition;
    private bool isDragging = false;

    // ==================== Local Space 辅助方法 ====================

    /// <summary>
    /// 获取内部工作坐标（useLocalSpace 时为 localPosition，否则为 world position）
    /// </summary>
    private Vector3 CurrentPosition => useLocalSpace ? transform.localPosition : transform.position;

    /// <summary>
    /// 设置内部工作坐标
    /// </summary>
    private void SetCurrentPosition(Vector3 pos)
    {
        if (useLocalSpace) transform.localPosition = pos;
        else transform.position = pos;
    }

    /// <summary>
    /// 世界坐标 → 内部工作坐标（useLocalSpace 时转为父物体本地坐标）
    /// </summary>
    private Vector3 WorldToInternal(Vector3 worldPos)
    {
        return useLocalSpace && transform.parent != null
            ? transform.parent.InverseTransformPoint(worldPos)
            : worldPos;
    }

    /// <summary>
    /// 内部工作坐标 → 世界坐标（useLocalSpace 时从父物体本地坐标转换）
    /// </summary>
    private Vector3 InternalToWorld(Vector3 internalPos)
    {
        return useLocalSpace && transform.parent != null
            ? transform.parent.TransformPoint(internalPos)
            : internalPos;
    }

    /// <summary>
    /// 世界方向增量 → 内部方向增量（useLocalSpace 时投影到父物体本地轴）
    /// </summary>
    private Vector3 WorldDeltaToInternal(Vector3 worldDelta)
    {
        return useLocalSpace && transform.parent != null
            ? transform.parent.InverseTransformDirection(worldDelta)
            : worldDelta;
    }

    private void Awake()
    {
        col = GetComponent<BoxCollider2D>();
        SyncColliderFromRect();
        lastPosition = CurrentPosition;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (col == null) col = GetComponent<BoxCollider2D>();
        SyncColliderFromRect();
    }
#endif

    private void Start()
    {
        UpdateBounds();
    }

    private void Update()
    {
        // 更新惯性运动
        UpdateInertia();

        // 更新弹性边界回弹
        UpdateElasticReturn();

        // 检查位置变化并触发事件
        CheckPositionChange();
    }

    public void onInitialize()
    {
        col = GetComponent<BoxCollider2D>();
        SyncColliderFromRect();
        if(limitMode == LimitMode.Manual)
        {
            manualLimitEvent.Invoke(this);
        }
        else
        {
            UpdateBounds();
        }
    }

    /// <summary>
    /// 获取自身的半尺寸（考虑 transform scale）。
    /// BoxCollider2D 模式：col.size * |scale| / 2。
    /// RectTransform 模式：rect.width/height * |scale| / 2。
    /// </summary>
    public Vector2 GetHalfSize()
    {
        Vector3 scale = useLocalSpace ? transform.localScale : transform.lossyScale;
        return GetHalfSizeForScale(scale);
    }

    /// <summary>
    /// DragLimit 的 ClampPosition 使用世界坐标，因此传入的半尺寸也必须位于世界空间。
    /// Manual 模式继续通过 GetHalfSize 保持原有内部空间契约。
    /// </summary>
    private Vector2 GetWorldHalfSize()
    {
        return GetHalfSizeForScale(transform.lossyScale);
    }

    private Vector2 GetHalfSizeForScale(Vector3 scale)
    {
        if (sizeSource == SizeSource.RectTransform)
        {
            RectTransform r = ActiveRect;
            if (r != null)
            {
                Rect rect = r.rect;
                return new Vector2(rect.width * Mathf.Abs(scale.x) / 2f, rect.height * Mathf.Abs(scale.y) / 2f);
            }
            Debug.LogWarning($"[DragContainer] {name} sizeSource=RectTransform 但未找到 RectTransform，回退到 BoxCollider2D");
        }

        return new Vector2(col.size.x * Mathf.Abs(scale.x) / 2f, col.size.y * Mathf.Abs(scale.y) / 2f);
    }

    /// <summary>
    /// 几何中心相对 transform.position 的世界偏移向量。非中心 pivot 或 collider offset 非零时返回非零向量。
    /// </summary>
    private Vector3 GetGeometricCenterOffsetWorld()
    {
        if (sizeSource == SizeSource.RectTransform)
        {
            RectTransform r = ActiveRect;
            if (r != null)
            {
                Rect rect = r.rect;
                Vector2 pivot = r.pivot;
                Vector3 localOffset = new Vector3((0.5f - pivot.x) * rect.width, (0.5f - pivot.y) * rect.height, 0f);
                return transform.TransformVector(localOffset);
            }
        }

        if (col != null)
        {
            return transform.TransformPoint(col.offset) - transform.position;
        }
        return Vector3.zero;
    }

    /// <summary>
    /// 将 RectTransform 的尺寸与 pivot 同步到 BoxCollider2D，
    /// 保证 MouseManager 的点击检测范围始终与视觉 rect 对齐。
    /// 运行时 rect 尺寸变化后可手动调用此方法刷新 collider。
    /// </summary>
    public void SyncColliderFromRect()
    {
        if (sizeSource != SizeSource.RectTransform) return;
        if (col == null) return;
        RectTransform r = ActiveRect;
        if (r == null) return;

        Rect rect = r.rect;
        Vector2 pivot = r.pivot;
        col.size = new Vector2(rect.width, rect.height);
        col.offset = new Vector2((0.5f - pivot.x) * rect.width, (0.5f - pivot.y) * rect.height);
    }

    public void UpdateBounds()
    {
        SyncColliderFromRect();
        if (limitMode == LimitMode.DragLimit)
        {
            if (dragLimit != null)
            {
                Vector3 geoOffsetWorld = GetGeometricCenterOffsetWorld();
                var result = dragLimit.ClampPosition(transform.position + geoOffsetWorld, GetWorldHalfSize());
                // 存储的 limit 是 transform.position 能到达的范围，需要减去几何中心偏移
                _limitMinX = result.minX - geoOffsetWorld.x;
                _limitMaxX = result.maxX - geoOffsetWorld.x;
                _limitMinY = result.minY - geoOffsetWorld.y;
                _limitMaxY = result.maxY - geoOffsetWorld.y;
            }
        }
        else // Manual
        {
            if (useLocalSpace && transform.parent != null)
            {
                // 沿父物体的本地轴方向投影，将世界坐标 limit 值正确转换为本地坐标
                // 原理：找到沿本地轴移动时，世界 X（或 Y）等于 limit 值的那个点，再转为本地坐标
                Transform parent = transform.parent;
                Vector3 wp = transform.position;
                Vector3 parentRight = parent.right;
                Vector3 parentUp = parent.up;

                // 水平限制：沿 parent.right 方向投影到世界 X = leftLimit / rightLimit
                if (Mathf.Abs(parentRight.x) > 0.001f)
                {
                    float tLeft = (leftLimit - wp.x) / parentRight.x;
                    float tRight = (rightLimit - wp.x) / parentRight.x;
                    float localXLeft = parent.InverseTransformPoint(wp + parentRight * tLeft).x;
                    float localXRight = parent.InverseTransformPoint(wp + parentRight * tRight).x;
                    _limitMinX = Mathf.Min(localXLeft, localXRight);
                    _limitMaxX = Mathf.Max(localXLeft, localXRight);
                }
                else
                {
                    // 本地 X 轴与世界 X 轴垂直，世界 X 限制无法约束本地 X
                    _limitMinX = float.MinValue;
                    _limitMaxX = float.MaxValue;
                }

                // 垂直限制：沿 parent.up 方向投影到世界 Y = bottomLimit / topLimit
                if (Mathf.Abs(parentUp.y) > 0.001f)
                {
                    float tBottom = (bottomLimit - wp.y) / parentUp.y;
                    float tTop = (topLimit - wp.y) / parentUp.y;
                    float localYBottom = parent.InverseTransformPoint(wp + parentUp * tBottom).y;
                    float localYTop = parent.InverseTransformPoint(wp + parentUp * tTop).y;
                    _limitMinY = Mathf.Min(localYBottom, localYTop);
                    _limitMaxY = Mathf.Max(localYBottom, localYTop);
                }
                else
                {
                    _limitMinY = float.MinValue;
                    _limitMaxY = float.MaxValue;
                }
            }
            else
            {
                _limitMinX = leftLimit;
                _limitMaxX = rightLimit;
                _limitMinY = bottomLimit;
                _limitMaxY = topLimit;
            }
        }
    }

    /// <summary>
    /// 统一的限制计算方法，替代原先三处重复的 limit 逻辑。
    /// </summary>
    private (Vector3 clampedPos, bool hitBoundary) CalculateLimitedPosition(Vector3 targetPos)
    {
        if (limitMode == LimitMode.DragLimit)
        {
            if (dragLimit != null)
            {
                // DragContainer 的计算链以内部工作坐标运行，而 DragLimit 明确接受世界坐标。
                // 只在该契约边界做一次往返，避免把 localPosition 与世界边界混用。
                Vector3 targetWorld = InternalToWorld(targetPos);
                Vector3 geoOffsetWorld = GetGeometricCenterOffsetWorld();
                var result = dragLimit.ClampPosition(targetWorld + geoOffsetWorld, GetWorldHalfSize());
                return (WorldToInternal(result.clampedPos - geoOffsetWorld), result.hitBoundary);
            }
            return (targetPos, false);
        }
        else // Manual
        {
            if (useLocalSpace)
            {
                // useLocalSpace 时使用已在 UpdateBounds 中转换的本地坐标边界
                Vector3 clamped = new Vector3(
                    Mathf.Clamp(targetPos.x, _limitMinX, _limitMaxX),
                    Mathf.Clamp(targetPos.y, _limitMinY, _limitMaxY),
                    targetPos.z);
                return (clamped, (Vector3)clamped != targetPos);
            }
            else
            {
                // 原始行为：直接使用世界坐标 limit 值
                Vector3 clamped = new Vector3(
                    Mathf.Clamp(targetPos.x, leftLimit, rightLimit),
                    Mathf.Clamp(targetPos.y, bottomLimit, topLimit),
                    targetPos.z);
                return (clamped, (Vector3)clamped != targetPos);
            }
        }
    }

    public void onDragStart()
    {
        startPos = Tools.getMousePos();

        // useLocalSpace 时确保边界是最新的
        if (useLocalSpace && limitMode == LimitMode.Manual)
            UpdateBounds();

        // 停止当前惯性运动
        StopInertia();

        // 停止弹性回弹
        isElasticReturning = false;

        // 清空历史位置
        positionHistory.Clear();
        RecordPosition();

        isDragging = true;

        // 触发拖拽开始事件
        onDragStartEvent?.Invoke();
    }

    public void onDragUpdate()
    {
        moveToDragPos();

        // 计算当前速度
        Vector3 currentSpeed = CalculateVelocity();

        // 触发拖拽更新事件
        onDragUpdateEvent?.Invoke(currentSpeed);
    }

    public void onDragEnd()
    {
        isDragging = false;

        // 计算最终速度
        Vector3 finalVelocity = CalculateVelocity();

        // 触发拖拽结束事件
        onDragEndEvent?.Invoke(finalVelocity);

        // 如果启用了惯性，开始惯性运动
        if (enableInertia && finalVelocity.magnitude > minInertiaSpeed)
        {
            StartInertia(finalVelocity);
        }
        else
        {
            // 如果启用了弹性边界，开始回弹
            if (enableElasticBoundary)
            {
                StartElasticReturn();
            }
        }
    }

    public void moveToDragPos()
    {
        Vector3 draggedPos = getDraggedPosition();
        Vector3 curPos = CurrentPosition;
        switch (mode)
        {
            case DragMode.Vertical:
                SetCurrentPosition(new Vector3(curPos.x, draggedPos.y, curPos.z));
                startPos = Tools.getMousePos();
                break;

            case DragMode.Horizontal:
                SetCurrentPosition(new Vector3(draggedPos.x, curPos.y, curPos.z));
                startPos = Tools.getMousePos();
                break;

            case DragMode.Composite:
                SetCurrentPosition(draggedPos);
                startPos = Tools.getMousePos();
                break;
        }
        // 记录位置用于速度计算
        RecordPosition();
    }

    private Vector3 getDraggedPosition()
    {
        // 计算鼠标在世界空间的增量，然后转换到内部工作坐标系
        Vector3 worldDelta = Tools.getMousePos() - startPos;
        Vector3 internalDelta = WorldDeltaToInternal(worldDelta);
        Vector3 targetPos = CurrentPosition + internalDelta;

        var (clampedPos, hitBoundary) = CalculateLimitedPosition(targetPos);

        // 应用弹性边界效果
        if (enableElasticBoundary && hitBoundary)
        {
            // 计算超出边界的偏移量
            Vector3 overshoot = targetPos - clampedPos;

            // 应用弹性因子
            overshoot *= elasticFactor;

            // 限制最大偏移
            overshoot = Vector3.ClampMagnitude(overshoot, maxElasticOffset);

            // 更新弹性偏移
            elasticOffset = overshoot;

            // 设置弹性目标位置
            elasticTargetPosition = clampedPos + overshoot;

            // 触发弹性边界事件
            onElasticBoundaryHit?.Invoke(overshoot);

            return elasticTargetPosition;
        }

        // 如果没有弹性效果或未触及边界，重置弹性偏移
        elasticOffset = Vector3.zero;

        return clampedPos;
    }

    // ==================== 扩展1：惯性效果方法 ====================

    private void RecordPosition()
    {
        positionHistory.Enqueue(CurrentPosition);
        if (positionHistory.Count > historySize)
        {
            positionHistory.Dequeue();
        }
    }

    private Vector3 CalculateVelocity()
    {
        if (positionHistory.Count < 2)
            return Vector3.zero;

        Vector3[] positions = positionHistory.ToArray();
        Vector3 velocity = Vector3.zero;

        for (int i = 1; i < positions.Length; i++)
        {
            velocity += (positions[i] - positions[i - 1]) / Time.deltaTime;
        }

        velocity /= (positions.Length - 1);

        // 根据拖拽模式过滤速度方向
        switch (mode)
        {
            case DragMode.Vertical:
                velocity.x = 0;
                break;
            case DragMode.Horizontal:
                velocity.y = 0;
                break;
        }

        // 限制最大速度
        if (velocity.magnitude > maxInertiaSpeed)
        {
            velocity = velocity.normalized * maxInertiaSpeed;
        }

        return velocity;
    }

    private void StartInertia(Vector3 initialVelocity)
    {
        velocity = initialVelocity;
        isInertiaMoving = true;
        inertiaStartPosition = CurrentPosition;
        inertiaTimer = 0f;

        // 触发惯性状态变化事件
        onInertiaStateChanged?.Invoke(true);
    }

    private void StopInertia()
    {
        isInertiaMoving = false;
        velocity = Vector3.zero;

        // 触发惯性状态变化事件
        onInertiaStateChanged?.Invoke(false);
    }

    private void UpdateInertia()
    {
        if (!isInertiaMoving || isElasticReturning) return;

        // 更新计时器
        inertiaTimer += Time.deltaTime;

        // 计算当前惯性强度（动画曲线）
        float curveValue = inertiaCurve.Evaluate(inertiaTimer / inertiaDuration);

        // 应用惯性移动
        Vector3 moveDelta = velocity * Time.deltaTime * curveValue;

        // 根据拖拽模式过滤移动方向
        switch (mode)
        {
            case DragMode.Vertical:
                moveDelta.x = 0;
                break;
            case DragMode.Horizontal:
                moveDelta.y = 0;
                break;
        }

        // 应用限制
        Vector3 targetPos = CurrentPosition + moveDelta;
        Vector3 limitedPos = ApplyLimits(targetPos, true);

        // 应用移动
        SetCurrentPosition(limitedPos);

        // 衰减速度
        velocity *= inertiaDeceleration;

        // 检查是否应该停止惯性
        if (velocity.magnitude < minInertiaSpeed || inertiaTimer >= inertiaDuration)
        {
            StopInertia();

            // 如果启用了弹性边界，开始回弹
            if (enableElasticBoundary)
            {
                StartElasticReturn();
            }
        }
    }

    private Vector3 ApplyLimits(Vector3 targetPos, bool checkElastic = false)
    {
        var (clampedPos, _) = CalculateLimitedPosition(targetPos);
        return clampedPos;
    }

    // ==================== 扩展2：边界弹性效果方法 ====================

    private void StartElasticReturn()
    {
        if (!enableElasticBoundary || elasticOffset == Vector3.zero) return;

        isElasticReturning = true;
        elasticTargetPosition = ApplyLimits(CurrentPosition);
    }

    private void UpdateElasticReturn()
    {
        if (!isElasticReturning) return;

        // 平滑回到最近边界位置
        Vector3 curPos = CurrentPosition;
        Vector3 newPos = Vector3.Lerp(curPos, elasticTargetPosition, elasticReturnSpeed * Time.deltaTime);
        SetCurrentPosition(newPos);

        // 更新弹性偏移
        elasticOffset = CurrentPosition - elasticTargetPosition;

        // 检查是否完成回弹
        if (Vector3.Distance(CurrentPosition, elasticTargetPosition) < 0.1f)
        {
            isElasticReturning = false;
            elasticOffset = Vector3.zero;
            SetCurrentPosition(elasticTargetPosition);
        }
    }

    // ==================== 扩展3：拖拽事件回调方法 ====================

    private void CheckPositionChange()
    {
        Vector3 curPos = CurrentPosition;
        if (curPos != lastPosition)
        {
            Vector3 delta = curPos - lastPosition;

            // 根据拖拽模式过滤位置变化
            switch (mode)
            {
                case DragMode.Vertical:
                    delta.x = 0;
                    break;
                case DragMode.Horizontal:
                    delta.y = 0;
                    break;
            }

            if (delta != Vector3.zero)
            {
                onDragPositionChanged?.Invoke(delta);
            }

            lastPosition = curPos;
        }
    }

    // ==================== 公共方法 ====================

    /// <summary>
    /// 手动设置速度，触发惯性运动
    /// </summary>
    public void SetVelocity(Vector3 newVelocity)
    {
        if (enableInertia && newVelocity.magnitude > minInertiaSpeed)
        {
            StartInertia(newVelocity);
        }
    }

    /// <summary>
    /// 立即停止所有运动（惯性、弹性回弹）
    /// </summary>
    public void StopAllMotion()
    {
        StopInertia();
        isElasticReturning = false;
        elasticOffset = Vector3.zero;
    }

    /// <summary>
    /// 获取当前是否在惯性运动中
    /// </summary>
    public bool IsInertiaMoving()
    {
        return isInertiaMoving;
    }

    /// <summary>
    /// 获取当前是否在弹性回弹中
    /// </summary>
    public bool IsElasticReturning()
    {
        return isElasticReturning;
    }

    /// <summary>
    /// 获取当前弹性偏移量
    /// </summary>
    public Vector3 GetElasticOffset()
    {
        return elasticOffset;
    }

    private Vector3 getTargetPosition(Vector3 internalTargetPos)
    {
        var (clampedPos, _) = CalculateLimitedPosition(internalTargetPos);
        return clampedPos;
    }

    /// <summary>
    /// 将物体移动到指定的世界坐标位置（自动处理 local space 转换和边界限制）
    /// </summary>
    public void moveToTargetPos(Vector3 targetPos)
    {
        // useLocalSpace 时确保边界是最新的（ScrollBar_Controller 可能刚设置了 limits）
        if (useLocalSpace && limitMode == LimitMode.Manual)
            UpdateBounds();

        // 将世界坐标转换为内部工作坐标
        Vector3 internalTarget = WorldToInternal(targetPos);
        Vector3 curPos = CurrentPosition;

        switch (mode)
        {
            case DragMode.Vertical:
                SetCurrentPosition(new Vector3(curPos.x, getTargetPosition(internalTarget).y, curPos.z));
                break;

            case DragMode.Horizontal:
                SetCurrentPosition(new Vector3(getTargetPosition(internalTarget).x, curPos.y, curPos.z));
                break;

            case DragMode.Composite:
                SetCurrentPosition(getTargetPosition(internalTarget));
                break;
        }
    }

    /// <summary>
    /// 获取 BoxCollider2D 指定方向的边缘中点位置
    /// </summary>
    public Vector2 GetEdgePosition(BoxCollider2D targetCol, Vector2 direction)
    {
        if (targetCol == null)
        {
            Debug.LogError("BoxCollider2D 不能为空");
            return Vector2.zero;
        }

        Vector2 center = (Vector2)targetCol.transform.TransformPoint(targetCol.offset);
        Vector3 scale = targetCol.transform.lossyScale;
        float halfW = targetCol.size.x * Mathf.Abs(scale.x) / 2f;
        float halfH = targetCol.size.y * Mathf.Abs(scale.y) / 2f;

        if (direction == Vector2.up)
            return new Vector2(center.x, center.y + halfH);
        else if (direction == Vector2.down)
            return new Vector2(center.x, center.y - halfH);
        else if (direction == Vector2.left)
            return new Vector2(center.x - halfW, center.y);
        else if (direction == Vector2.right)
            return new Vector2(center.x + halfW, center.y);
        else
        {
            Debug.LogWarning($"不支持的方向: {direction}，只支持上下左右四个基本方向");
            return center;
        }
    }
}
