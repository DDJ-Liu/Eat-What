using UnityEngine;

/// <summary>
/// 示例：物体的 y 坐标以恒定速度变化（垂直均匀模式）
/// 适合需要垂直下落或上升的物体
/// </summary>
public class FollowArcPath_Vertical : MonoBehaviour
{
    public ArcPathComponent arcPath;
    public float verticalSpeed = 2f;  // 正值=向上，负值=向下

    private float currentY;
    private bool initialized = false;

    void Start()
    {
        if (arcPath != null && arcPath.GetControlPoints().Count > 0)
        {
            Vector3 startPos = arcPath.transform.TransformPoint(
                arcPath.GetControlPoints()[0].position);
            currentY = startPos.y;
            initialized = true;
        }
    }

    void Update()
    {
        if (!initialized || arcPath == null)
            return;

        currentY += verticalSpeed * Time.deltaTime;

        if (arcPath.EvaluateX(currentY, out float x))
        {
            transform.position = new Vector3(x, currentY, transform.position.z);
        }
    }
}
