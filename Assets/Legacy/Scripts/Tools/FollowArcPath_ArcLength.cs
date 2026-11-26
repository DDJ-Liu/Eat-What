using UnityEngine;
using System.Collections;

/// <summary>
/// 示例：物体沿路径以恒定速度移动（弧长均匀模式 - 推荐）
/// </summary>
public class FollowArcPath_ArcLength : MonoBehaviour
{
    public ArcPathComponent arcPath;
    public float speed = 3f;  // 单位/秒

    private float normalizedDistance = 0f;

    void Update()
    {
        if (arcPath == null)
            return;

        float totalLength = arcPath.GetTotalArcLength();
        if (totalLength <= 0f)
            return;

        // 计算归一化速度
        float normalizedSpeed = speed / totalLength;
        normalizedDistance += normalizedSpeed * Time.deltaTime;

        if (arcPath.EvaluateByNormalizedDistance(normalizedDistance, out Vector3 position))
        {
            transform.position = position;

            // 可选：计算朝向（沿切线方向）
            if (normalizedDistance < 0.99f)
            {
                arcPath.EvaluateByNormalizedDistance(normalizedDistance + 0.01f, out Vector3 nextPos);
                Vector3 direction = (nextPos - position).normalized;
                if (direction != Vector3.zero)
                    transform.right = direction;  // 假设物体的 right 是前进方向
            }
        }

        // 循环
        if (normalizedDistance >= 1f)
        {
            normalizedDistance = 0f;
        }
    }
}
