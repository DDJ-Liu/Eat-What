using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 旋转木马组件 - 持续旋转物体，支持顺时针/逆时针方向
/// </summary>
public class MerryGoRound : MonoBehaviour
{
    public enum RotationDirection
    {
        CW,  // 顺时针 (Clockwise)
        CCW  // 逆时针 (Counter-Clockwise)
    }

    [Header("旋转设置")]
    [Tooltip("旋转速度（度/秒）")]
    public float speed = 30f;

    [Tooltip("旋转方向")]
    public RotationDirection direction = RotationDirection.CW;

    [Header("启动模式")]
    [Tooltip("是否在组件启用时自动开始旋转")]
    public bool autoStart = true;

    [Header("事件回调")]
    [Tooltip("开始旋转时触发")]
    public UnityEvent onRotationStart;

    [Tooltip("停止旋转时触发")]
    public UnityEvent onRotationEnd;

    [Tooltip("每完成一圈时触发")]
    public UnityEvent onCompleteLap;

    // 私有状态
    private bool isRotating = false;
    private float accumulatedRotation = 0f; // 累积旋转角度（用于检测完整圈数）

    private void Start()
    {
        if (autoStart)
        {
            StartRotation();
        }
    }

    private void Update()
    {
        if (!isRotating) return;

        // 计算本帧旋转角度
        float rotationThisFrame = speed * Time.deltaTime;

        // 根据方向调整旋转方向
        if (direction == RotationDirection.CCW)
        {
            rotationThisFrame = -rotationThisFrame;
        }

        // 应用旋转（绕Z轴）
        transform.Rotate(0f, 0f, rotationThisFrame);

        // 累积旋转角度（使用绝对值）
        accumulatedRotation += Mathf.Abs(rotationThisFrame);

        // 检测是否完成一圈（360度）
        if (accumulatedRotation >= 360f)
        {
            accumulatedRotation -= 360f; // 保留余数
            onCompleteLap?.Invoke();
        }
    }

    /// <summary>
    /// 开始旋转
    /// </summary>
    public void StartRotation()
    {
        if (isRotating) return;

        isRotating = true;
        accumulatedRotation = 0f;
        onRotationStart?.Invoke();
    }

    /// <summary>
    /// 停止旋转
    /// </summary>
    public void StopRotation()
    {
        if (!isRotating) return;

        isRotating = false;
        onRotationEnd?.Invoke();
    }

    /// <summary>
    /// 设置旋转速度
    /// </summary>
    /// <param name="newSpeed">新的旋转速度（度/秒）</param>
    public void SetSpeed(float newSpeed)
    {
        speed = Mathf.Max(0f, newSpeed);
    }

    /// <summary>
    /// 设置旋转方向
    /// </summary>
    /// <param name="newDirection">新的旋转方向</param>
    public void SetDirection(RotationDirection newDirection)
    {
        direction = newDirection;
    }

    /// <summary>
    /// 切换旋转方向
    /// </summary>
    public void ToggleDirection()
    {
        direction = (direction == RotationDirection.CW) ? RotationDirection.CCW : RotationDirection.CW;
    }

    /// <summary>
    /// 获取当前是否正在旋转
    /// </summary>
    public bool IsRotating()
    {
        return isRotating;
    }
}
