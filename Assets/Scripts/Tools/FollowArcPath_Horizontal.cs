using UnityEngine;

/// <summary>
/// 示例：物体的 x 坐标以恒定速度增加（水平均匀模式）
/// 适合地形轮廓等场景
/// </summary>
public class FollowArcPath_Horizontal : MonoBehaviour
{
    public ArcPathComponent arcPath;
    public float horizontalSpeed = 2f;

    private float currentX;
    private bool initialized = false;

    void Start()
    {
        if (arcPath != null && arcPath.GetControlPoints().Count > 0)
        {
            Vector3 startPos = arcPath.transform.TransformPoint(
                arcPath.GetControlPoints()[0].position);
            currentX = startPos.x;
            initialized = true;
        }
    }

    void Update()
    {
        if (!initialized || arcPath == null)
            return;

        currentX += horizontalSpeed * Time.deltaTime;

        if (arcPath.Evaluate(currentX, out float y))
        {
            transform.position = new Vector3(currentX, y, transform.position.z);
        }
    }
}
