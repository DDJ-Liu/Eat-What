using UnityEngine;
using UnityEngine.Events;
using System.Collections;

/// <summary>
/// ArcPath 巡线运动通用控制器
/// 支持多种解析维度、移动方式、返回模式和中断行为
/// </summary>
public class ArcPathController : MonoBehaviour
{
    #region 枚举定义

    /// <summary>
    /// 解析维度：决定如何解析路径
    /// </summary>
    public enum PathDimension
    {
        AlongCurve,  // 沿曲线（物体沿曲线移动，不锁定维度）
        XAxis,       // 沿x轴（x轴匀速，y轴按曲线解析）
        YAxis        // 沿y轴（y轴匀速，x轴按曲线解析）
    }

    /// <summary>
    /// 移动方式：恒定速度 or 总时间
    /// </summary>
    public enum MovementMode
    {
        ConstantSpeed,  // 恒定速度（单位/秒）
        TotalTime       // 总时间（从起点到终点的总时长）
    }

    /// <summary>
    /// 返回模式：是否往返
    /// </summary>
    public enum ReturnMode
    {
        Once,           // 返回一次（起点->终点->起点，然后停止）
        Always,         // 始终返回（无限往返）
        OneWayOnly      // 仅一次（起点->终点，然后停止）
    }

    /// <summary>
    /// 中断行为：被打断时的处理方式
    /// </summary>
    public enum InterruptBehavior
    {
        ReturnToStart,  // 返回起点
        Stop,           // 停止不动（默认）
        JumpToEnd       // 跳至终点
    }

    /// <summary>
    /// 起点初始化模式：处理物体不在起点时的行为
    /// </summary>
    public enum StartPositionMode
    {
        Teleport,  // 瞬移到起点
        Lerp,      // 平滑移动到起点
        Strict     // 严格模式（不在容差内则不允许移动）
    }

    #endregion

    #region 序列化字段

    [Header("路径设置")]
    [Tooltip("关联的弧线路径组件")]
    public ArcPathComponent arcPath;

    [Header("移动设置")]
    [Tooltip("解析维度")]
    public PathDimension pathDimension = PathDimension.AlongCurve;

    [Tooltip("移动方式")]
    public MovementMode movementMode = MovementMode.ConstantSpeed;

    [Tooltip("恒定速度模式：单位/秒 | 总时间模式：总时长（秒）")]
    public float speedOrDuration = 3f;

    [Tooltip("是否根据移动方向自动旋转物体（仅对 AlongCurve 模式有效）")]
    public bool rotateAlongPath = true;

    [Header("返回设置")]
    [Tooltip("返回模式")]
    public ReturnMode returnMode = ReturnMode.OneWayOnly;

    [Header("控制设置")]
    [Tooltip("是否启用控制器")]
    public bool isEnabled = true;

    [Tooltip("中断时的默认行为")]
    public InterruptBehavior defaultInterruptBehavior = InterruptBehavior.Stop;

    [Header("起点设置")]
    [Tooltip("起点初始化模式")]
    public StartPositionMode startPositionMode = StartPositionMode.Lerp;

    [Tooltip("起点容差（圆形半径）")]
    public float startPositionTolerance = 0.5f;

    [Tooltip("Lerp模式下移动到起点的速度（单位/秒）")]
    public float lerpToStartSpeed = 5f;

    [Header("事件")]
    [Tooltip("开始巡线移动时触发")]
    public UnityEvent onStartMoving;

    [Tooltip("到达终点时触发")]
    public UnityEvent onReachEnd;

    #endregion

    #region 私有状态

    private bool isMoving = false;
    private bool isMovingForward = true;
    private float currentProgress = 0f;
    private Coroutine movementCoroutine;
    private bool hasReturned = false;  // 是否已经返回过一次

    // 轴向模式（XAxis/YAxis）专用的 FixedUpdate 状态
    private float axisVelocity = 0f;            // 轴速度（局部坐标单位/秒）
    private float axisCurrentPosition = 0f;      // 当前轴位置（局部坐标）
    private bool useFixedUpdateMode = false;     // 是否使用FixedUpdate模式

    // 用于X/Y轴模式的边界缓存（局部坐标）
    private float minX_local, maxX_local, minY_local, maxY_local;
    private bool boundsCached = false;

    #endregion

    #region 生命周期

    void Start()
    {
        if (arcPath == null)
        {
            Debug.LogError("[ArcPathController] 未指定 arcPath！");
            return;
        }

        if (arcPath.GetControlPoints().Count < 2)
        {
            Debug.LogError("[ArcPathController] arcPath 控制点少于2个！");
            return;
        }

        CacheBounds();

        if (isEnabled)
        {
            StartMovement();
        }
    }

    void OnEnable()
    {
        // OnEnable 在 Start 之前执行，此时边界可能未缓存
        // 移动逻辑统一由 Start 触发
    }

    void OnDisable()
    {
        StopMovement(false);
    }

    void FixedUpdate()
    {
        // 只在轴向模式且正在移动时执行
        if (!useFixedUpdateMode || !isMoving || !isEnabled)
            return;

        // 使用 Time.fixedDeltaTime 推进位置（用户需求）
        axisCurrentPosition += axisVelocity * Time.fixedDeltaTime;

        // 获取当前轴的边界
        float minBound, maxBound;
        if (pathDimension == PathDimension.XAxis)
        {
            minBound = minX_local;
            maxBound = maxX_local;
        }
        else // YAxis
        {
            minBound = minY_local;
            maxBound = maxY_local;
        }

        // 检查是否到达边界
        bool reachedEnd = false;

        if (isMovingForward)
        {
            if ((axisVelocity > 0 && axisCurrentPosition >= maxBound) ||
                (axisVelocity < 0 && axisCurrentPosition <= maxBound))
            {
                axisCurrentPosition = maxBound;
                reachedEnd = true;
            }
        }
        else // 返回中
        {
            if ((axisVelocity < 0 && axisCurrentPosition <= minBound) ||
                (axisVelocity > 0 && axisCurrentPosition >= minBound))
            {
                axisCurrentPosition = minBound;
                reachedEnd = true;
            }
        }

        // 更新物体位置
        UpdatePosition_Axis_Fixed(axisCurrentPosition);

        // 处理到达终点
        if (reachedEnd)
        {
            onReachEnd?.Invoke();

            if (ShouldReturn())
            {
                // 需要返回，反转方向
                isMovingForward = !isMovingForward;
                axisVelocity = -axisVelocity;
                onStartMoving?.Invoke();

                if (!isMovingForward)
                {
                    hasReturned = true;
                }
            }
            else
            {
                // 不需要返回，停止运动
                isMoving = false;
                useFixedUpdateMode = false;
            }
        }
    }

    #endregion

    #region 公共控制方法

    /// <summary>
    /// 开始巡线移动
    /// </summary>
    public void StartMovement()
    {
        if (!isEnabled)
        {
            Debug.LogWarning($"[ArcPathController] 控制器未启用，无法开始移动");
            return;
        }

        if (isMoving)
        {
            Debug.LogWarning($"[ArcPathController] 已经在移动中，无法重复开始");
            return;
        }

        if (arcPath == null)
        {
            Debug.LogError($"[ArcPathController] arcPath 为 null，无法开始移动");
            return;
        }

        if (!boundsCached)
        {
            CacheBounds();
        }

        // XAxis 和 YAxis 模式使用 FixedUpdate，AlongCurve 模式使用协程
        if (pathDimension == PathDimension.XAxis || pathDimension == PathDimension.YAxis)
        {
            useFixedUpdateMode = true;
            InitializeAxisFixedUpdate();
        }
        else // AlongCurve
        {
            useFixedUpdateMode = false;
            if (movementCoroutine != null)
                StopCoroutine(movementCoroutine);
            movementCoroutine = StartCoroutine(MovementCoroutine());
        }
    }

    /// <summary>
    /// 停止巡线移动（应用默认中断行为）
    /// </summary>
    public void StopMovement()
    {
        StopMovement(true);
    }

    /// <summary>
    /// 中断巡线移动（支持优先级override）
    /// </summary>
    /// <param name="overrideBehavior">覆盖默认的中断行为</param>
    public void Interrupt(InterruptBehavior overrideBehavior)
    {
        if (!isMoving)
            return;

        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
            movementCoroutine = null;
        }

        // 停止 FixedUpdate
        useFixedUpdateMode = false;
        isMoving = false;

        // 应用中断行为
        switch (overrideBehavior)
        {
            case InterruptBehavior.ReturnToStart:
                currentProgress = 0f;
                if (pathDimension == PathDimension.XAxis)
                {
                    axisCurrentPosition = minX_local;  // 第一个控制点的 X
                    UpdatePosition_Axis_Fixed(minX_local);
                }
                else if (pathDimension == PathDimension.YAxis)
                {
                    axisCurrentPosition = minY_local;  // 第一个控制点的 Y
                    UpdatePosition_Axis_Fixed(minY_local);
                }
                else
                {
                    UpdatePosition(0f);
                }
                isMovingForward = true;
                break;

            case InterruptBehavior.Stop:
                // 保持当前位置，不做任何操作
                break;

            case InterruptBehavior.JumpToEnd:
                currentProgress = 1f;
                if (pathDimension == PathDimension.XAxis)
                {
                    axisCurrentPosition = maxX_local;  // 最后一个控制点的 X
                    UpdatePosition_Axis_Fixed(maxX_local);
                }
                else if (pathDimension == PathDimension.YAxis)
                {
                    axisCurrentPosition = maxY_local;  // 最后一个控制点的 Y
                    UpdatePosition_Axis_Fixed(maxY_local);
                }
                else
                {
                    UpdatePosition(1f);
                }
                break;
        }
    }

    /// <summary>
    /// 设置启用状态
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        isEnabled = enabled;

        if (enabled && !isMoving)
        {
            StartMovement();
        }
        else if (!enabled && isMoving)
        {
            StopMovement();
        }
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 停止巡线移动（内部使用）
    /// </summary>
    private void StopMovement(bool applyInterruptBehavior)
    {
        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
            movementCoroutine = null;
        }

        // 停止 FixedUpdate
        useFixedUpdateMode = false;
        isMoving = false;

        if (applyInterruptBehavior)
        {
            Interrupt(defaultInterruptBehavior);
        }
    }

    /// <summary>
    /// 缓存路径边界（用于X/Y轴模式）
    /// 注意：使用控制点的范围，如果曲线有超调会失败，这是预期行为
    /// </summary>
    private void CacheBounds()
    {
        if (arcPath == null || arcPath.GetControlPoints().Count < 2)
        {
            Debug.LogWarning($"[ArcPathController] CacheBounds 失败 - arcPath 无效或控制点不足");
            return;
        }

        var points = arcPath.GetControlPoints();

        // 直接使用首尾控制点的坐标
        minX_local = points[0].position.x;
        maxX_local = points[points.Count - 1].position.x;
        minY_local = points[0].position.y;
        maxY_local = points[points.Count - 1].position.y;

        boundsCached = true;
    }

    /// <summary>
    /// 初始化轴向模式（XAxis/YAxis）的 FixedUpdate 运动
    /// </summary>
    private void InitializeAxisFixedUpdate()
    {
        isMoving = true;
        isMovingForward = true;
        hasReturned = false;

        // 起点位置处理
        Vector3 startPosition = GetStartPosition();
        Debug.Log($"[ArcPathController] InitializeAxisFixedUpdate - startPosition: {startPosition}, current: {transform.position}, minX_local: {minX_local}, maxX_local: {maxX_local}");

        float distanceToStart = Vector3.Distance(transform.position, startPosition);

        if (distanceToStart > startPositionTolerance)
        {
            switch (startPositionMode)
            {
                case StartPositionMode.Teleport:
                    transform.position = startPosition;
                    Debug.Log($"[ArcPathController] Teleported to start: {startPosition}");
                    break;
                case StartPositionMode.Lerp:
                    StartCoroutine(LerpToStartPositionForAxis(startPosition));
                    return;
                case StartPositionMode.Strict:
                    Debug.Log($"[ArcPathController] 不在起点容差范围内，距离: {distanceToStart:F2}");
                    isMoving = false;
                    useFixedUpdateMode = false;
                    return;
            }
        }

        StartAxisMovement();
    }

    /// <summary>
    /// Lerp 到起点后启动轴向运动
    /// </summary>
    private IEnumerator LerpToStartPositionForAxis(Vector3 targetPosition)
    {
        yield return LerpToStartPosition(targetPosition);
        StartAxisMovement();
    }

    /// <summary>
    /// 启动轴向运动（计算速度并触发事件）
    /// </summary>
    private void StartAxisMovement()
    {
        // 根据模式确定起点和总距离
        float startPos, endPos, totalDistance;

        if (pathDimension == PathDimension.XAxis)
        {
            // 使用第一个和最后一个控制点的 X 坐标
            startPos = minX_local;  // points[0].x
            endPos = maxX_local;    // points[Count-1].x
            totalDistance = Mathf.Abs(maxX_local - minX_local);
        }
        else // YAxis
        {
            // 使用第一个和最后一个控制点的 Y 坐标
            startPos = minY_local;  // points[0].y
            endPos = maxY_local;    // points[Count-1].y
            totalDistance = Mathf.Abs(maxY_local - minY_local);
        }

        axisCurrentPosition = startPos;

        // 根据移动模式计算速度（用户需求：从 totalTime 推导速度）
        if (movementMode == MovementMode.TotalTime)
        {
            // TotalTime 模式：速度 = 距离 / 时间
            axisVelocity = totalDistance / speedOrDuration;
        }
        else // ConstantSpeed
        {
            // ConstantSpeed 模式：直接使用设定速度
            axisVelocity = speedOrDuration;
        }

        // 根据方向调整速度符号（从 startPos 到 endPos）
        if (endPos < startPos)
        {
            axisVelocity = -axisVelocity;
        }

        Debug.Log($"[ArcPathController] StartAxisMovement - startPos: {startPos}, endPos: {endPos}, velocity: {axisVelocity}, distance: {totalDistance}");

        // 立即设置初始位置（确保在第一个 FixedUpdate 之前物体在正确位置）
        UpdatePosition_Axis_Fixed(axisCurrentPosition);

        onStartMoving?.Invoke();
    }

    /// <summary>
    /// 巡线移动协程
    /// </summary>
    private IEnumerator MovementCoroutine()
    {
        isMoving = true;
        isMovingForward = true;
        currentProgress = 0f;
        hasReturned = false;

        // 起点位置检测和处理
        Vector3 startPosition = GetStartPosition();
        float distanceToStart = Vector3.Distance(transform.position, startPosition);

        if (distanceToStart > startPositionTolerance)
        {
            // 不在容差范围内，根据模式处理
            switch (startPositionMode)
            {
                case StartPositionMode.Teleport:
                    transform.position = startPosition;
                    break;

                case StartPositionMode.Lerp:
                    yield return LerpToStartPosition(startPosition);
                    break;

                case StartPositionMode.Strict:
                    Debug.Log($"[ArcPathController] 物体不在起点容差范围内（距离：{distanceToStart:F2}，容差：{startPositionTolerance:F2}），不开始移动。");
                    isMoving = false;
                    yield break;
            }
        }

        // 触发开始事件（在到达起点后才触发）
        onStartMoving?.Invoke();

        // 计算移动参数
        float duration = CalculateMovementDuration();
        Debug.Log($"[ArcPathController] 移动参数 - mode: {movementMode}, dimension: {pathDimension}, speedOrDuration: {speedOrDuration}, duration: {duration:F2}秒");

        while (true)
        {
            // 检查是否应该停止
            if (!isEnabled)
            {
                isMoving = false;
                yield break;
            }

            // 移动阶段
            float elapsed = 0f;
            float startProgress = isMovingForward ? 0f : 1f;
            float endProgress = isMovingForward ? 1f : 0f;

            while (elapsed < duration)
            {
                if (!isEnabled)
                {
                    isMoving = false;
                    yield break;
                }

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // 根据方向插值
                currentProgress = Mathf.Lerp(startProgress, endProgress, t);

                // 更新位置
                UpdatePosition(currentProgress);

                yield return null;
            }

            // 确保到达终点
            currentProgress = endProgress;
            UpdatePosition(currentProgress);

            // 触发到达终点事件
            onReachEnd?.Invoke();

            // 检查返回模式
            bool shouldReturn = ShouldReturn();

            if (!shouldReturn)
            {
                isMoving = false;
                yield break;
            }

            // 如果需要返回，触发开始事件（新的一段旅程）
            isMovingForward = !isMovingForward;
            onStartMoving?.Invoke();

            // 更新返回标记
            if (!isMovingForward)
            {
                hasReturned = true;
            }
        }
    }

    /// <summary>
    /// 计算移动持续时间
    /// </summary>
    private float CalculateMovementDuration()
    {
        if (movementMode == MovementMode.TotalTime)
        {
            return speedOrDuration;
        }
        else // ConstantSpeed
        {
            float distance = CalculatePathDistance();
            return distance / speedOrDuration;
        }
    }

    /// <summary>
    /// 计算路径距离（根据解析维度）
    /// </summary>
    private float CalculatePathDistance()
    {
        switch (pathDimension)
        {
            case PathDimension.AlongCurve:
                return arcPath.GetTotalArcLength();

            case PathDimension.XAxis:
                // 使用绝对值，支持反向路径
                return Mathf.Abs(maxX_local - minX_local);

            case PathDimension.YAxis:
                return Mathf.Abs(maxY_local - minY_local);

            default:
                return arcPath.GetTotalArcLength();
        }
    }

    /// <summary>
    /// 判断是否应该返回
    /// </summary>
    private bool ShouldReturn()
    {
        switch (returnMode)
        {
            case ReturnMode.OneWayOnly:
                return false;

            case ReturnMode.Once:
                // 如果是前进到终点，允许返回；如果已经返回过，则停止
                return isMovingForward && !hasReturned;

            case ReturnMode.Always:
                return true;

            default:
                return false;
        }
    }

    /// <summary>
    /// 根据进度更新位置（根据解析维度）
    /// </summary>
    private void UpdatePosition(float progress)
    {
        // 根据方向调整进度（用于返回时）
        float actualProgress = isMovingForward ? progress : (1f - progress);

        switch (pathDimension)
        {
            case PathDimension.AlongCurve:
                UpdatePosition_AlongCurve(actualProgress);
                break;

            case PathDimension.XAxis:
                UpdatePosition_XAxis(actualProgress);
                break;

            case PathDimension.YAxis:
                UpdatePosition_YAxis(actualProgress);
                break;
        }
    }

    /// <summary>
    /// 沿曲线模式：使用弧长均匀采样
    /// </summary>
    private void UpdatePosition_AlongCurve(float progress)
    {
        if (arcPath.EvaluateByNormalizedDistance(progress, out Vector3 position, out Vector3 tangent))
        {
            transform.position = position;

            // 根据移动方向设置朝向
            if (rotateAlongPath && tangent != Vector3.zero)
            {
                Vector3 direction = isMovingForward ? tangent : -tangent;
                transform.right = direction;
            }
        }
        else
        {
            Debug.LogWarning($"[ArcPathController] UpdatePosition_AlongCurve 失败 - progress: {progress}");
        }
    }

    // 用于 UpdatePosition_XAxis 的调试计数器
    private int updatePositionCallCount = 0;

    /// <summary>
    /// X轴模式：x轴匀速，y轴按曲线解析
    /// </summary>
    private void UpdatePosition_XAxis(float progress)
    {
        // 在局部坐标系中插值 X
        float localX = Mathf.Lerp(minX_local, maxX_local, progress);

        // 使用正确的 EvaluateLocalX API
        if (arcPath.EvaluateLocalX(localX, out Vector3 worldPosition))
        {
            transform.position = new Vector3(worldPosition.x, worldPosition.y, transform.position.z);
        }
        else
        {
            Debug.LogWarning($"[ArcPathController] EvaluateLocalX 失败 at localX={localX:F2}");
        }
    }

    /// <summary>
    /// Y轴模式：y轴匀速，x轴按曲线解析
    /// </summary>
    private void UpdatePosition_YAxis(float progress)
    {
        // 在局部坐标系中插值 Y
        float localY = Mathf.Lerp(minY_local, maxY_local, progress);

        // 使用新的 EvaluateLocalY API，直接在局部坐标系中查询
        if (arcPath.EvaluateLocalY(localY, out Vector3 worldPosition))
        {
            transform.position = new Vector3(worldPosition.x, worldPosition.y, transform.position.z);
        }
    }

    /// <summary>
    /// FixedUpdate 专用的轴向位置更新方法
    /// </summary>
    private void UpdatePosition_Axis_Fixed(float axisPosition)
    {
        Vector3 worldPosition;
        bool success = false;

        // 获取控制点
        var points = arcPath.GetControlPoints();

        if (pathDimension == PathDimension.XAxis)
        {
            // 对于端点，直接使用控制点坐标
            float epsilon = 0.001f;
            float distToStart = Mathf.Abs(axisPosition - points[0].position.x);
            float distToEnd = Mathf.Abs(axisPosition - points[points.Count - 1].position.x);

            if (distToStart < epsilon)
            {
                worldPosition = arcPath.transform.TransformPoint(points[0].position);
                success = true;
                Debug.Log($"[ArcPathController] UpdatePosition_Axis_Fixed - 使用起点");
            }
            else if (distToEnd < epsilon)
            {
                worldPosition = arcPath.transform.TransformPoint(points[points.Count - 1].position);
                success = true;
                Debug.Log($"[ArcPathController] UpdatePosition_Axis_Fixed - 使用终点");
            }
            else
            {
                // 中间位置：使用 EvaluateLocalX（Hermite 曲线）
                success = arcPath.EvaluateLocalX(axisPosition, out worldPosition);
                if (!success)
                {
                    Debug.LogWarning($"[ArcPathController] EvaluateLocalX 失败 at {axisPosition:F6}");
                }
            }
        }
        else // YAxis
        {
            float epsilon = 0.001f;
            float distToStart = Mathf.Abs(axisPosition - points[0].position.y);
            float distToEnd = Mathf.Abs(axisPosition - points[points.Count - 1].position.y);

            if (distToStart < epsilon)
            {
                worldPosition = arcPath.transform.TransformPoint(points[0].position);
                success = true;
            }
            else if (distToEnd < epsilon)
            {
                worldPosition = arcPath.transform.TransformPoint(points[points.Count - 1].position);
                success = true;
            }
            else
            {
                success = arcPath.EvaluateLocalY(axisPosition, out worldPosition);
            }
        }

        if (success)
        {
            transform.position = new Vector3(worldPosition.x, worldPosition.y, transform.position.z);
        }
        else
        {
            Debug.LogWarning($"[ArcPathController] FixedUpdate - Evaluate 失败 at {pathDimension}={axisPosition:F2}");
        }
    }

    /// <summary>
    /// 获取路径起点位置（世界坐标）
    /// </summary>
    private Vector3 GetStartPosition()
    {
        switch (pathDimension)
        {
            case PathDimension.AlongCurve:
                if (arcPath.EvaluateByNormalizedDistance(0f, out Vector3 pos))
                    return pos;
                break;

            case PathDimension.XAxis:
                // 直接使用第一个控制点的世界坐标作为起点
                var points = arcPath.GetControlPoints();
                if (points.Count > 0)
                {
                    Vector3 worldStartPos = arcPath.transform.TransformPoint(points[0].position);
                    //Debug.Log($"[ArcPathController] GetStartPosition XAxis - points[0] local: {points[0].position}, world: {worldStartPos}");
                    return worldStartPos;
                }
                break;

            case PathDimension.YAxis:
                // 直接使用第一个控制点的世界坐标作为起点
                var pointsY = arcPath.GetControlPoints();
                if (pointsY.Count > 0)
                {
                    Vector3 worldStartPosY = arcPath.transform.TransformPoint(pointsY[0].position);
                    return worldStartPosY;
                }
                break;
        }

        return transform.position;
    }

    /// <summary>
    /// 平滑移动到起点位置
    /// </summary>
    private IEnumerator LerpToStartPosition(Vector3 targetPosition)
    {
        Vector3 startPos = transform.position;
        float distance = Vector3.Distance(startPos, targetPosition);

        if (distance < 0.001f)
            yield break;

        float duration = distance / lerpToStartSpeed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (!isEnabled)
                yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.position = Vector3.Lerp(startPos, targetPosition, t);
            yield return null;
        }

        // 确保精确到达目标位置
        transform.position = targetPosition;
    }

    #endregion

    #region Gizmos

    private void OnDrawGizmos()
    {
        if (arcPath == null)
            return;

        // 原有的位置指示
        if (Application.isPlaying)
        {
            Gizmos.color = isMoving ? (isMovingForward ? Color.green : Color.red) : Color.gray;
            Gizmos.DrawWireSphere(transform.position, 0.2f);

            // 绘制方向指示
            if (isMoving)
            {
                Vector3 direction = transform.right * 0.5f;
                Gizmos.DrawLine(transform.position, transform.position + direction);
            }
        }

        // 绘制起点容差圆
        if (arcPath.GetControlPoints().Count >= 2)
        {
            Vector3 startPos = GetStartPosition();
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(startPos, startPositionTolerance);
        }
    }

    #endregion
}
