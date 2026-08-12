using UnityEngine;

/// <summary>
/// AI_Test 隔离区演示脚本：让物体绕指定轴匀速旋转，并可选做上下浮动。
/// 完全自包含，不依赖项目任何其它代码。
/// </summary>
public class MCPDemo_Spinner : MonoBehaviour
{
    [Header("旋转")]
    [Tooltip("旋转速度（度/秒）")]
    public float rotateSpeed = 90f;

    [Tooltip("旋转轴，会自动归一化")]
    public Vector3 rotateAxis = Vector3.up;

    [Header("浮动")]
    [Tooltip("是否启用上下浮动")]
    public bool enableBobbing = true;

    [Tooltip("浮动幅度（世界单位）")]
    public float bobAmplitude = 0.5f;

    [Tooltip("浮动频率（次/秒）")]
    public float bobFrequency = 1f;

    [Tooltip("相位偏移，用于让多个物体错开节奏")]
    public float phaseOffset = 0f;

    private Vector3 startPosition;

    private void Awake()
    {
        startPosition = transform.position;
    }

    private void Update()
    {
        if (rotateAxis.sqrMagnitude > 0.0001f)
        {
            transform.Rotate(rotateAxis.normalized, rotateSpeed * Time.deltaTime, Space.Self);
        }

        if (enableBobbing)
        {
            float offset = Mathf.Sin((Time.time * bobFrequency + phaseOffset) * 2f * Mathf.PI) * bobAmplitude;
            transform.position = startPosition + Vector3.up * offset;
        }
    }

    /// <summary>
    /// 把物体复位到 Awake 时记录的初始位置。
    /// </summary>
    public void ResetPosition()
    {
        transform.position = startPosition;
    }

    /// <summary>
    /// 切换浮动开关；关闭时立即复位，避免停在半空。
    /// 通过 Edit 工具直接改源码追加，用于验证「改文件 → refresh_unity → 重新编译」回路。
    /// </summary>
    public void ToggleBobbing()
    {
        enableBobbing = !enableBobbing;
        if (!enableBobbing) ResetPosition();
    }
}
