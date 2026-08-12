using UnityEngine;
using System.Collections;

/// <summary>
/// 示例：在协程中以指定时间走完整条路径
/// </summary>
public class FollowArcPath_Coroutine : MonoBehaviour
{
    public ArcPathComponent arcPath;
    public float duration = 5f;
    public bool loop = true;

    void Start()
    {
        if (arcPath != null)
        {
            StartCoroutine(WalkPath());
        }
    }

    IEnumerator WalkPath()
    {
        while (true)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                if (arcPath.EvaluateByNormalizedDistance(t, out Vector3 position))
                {
                    transform.position = position;

                    // 可选：计算朝向
                    if (t < 0.99f)
                    {
                        arcPath.EvaluateByNormalizedDistance(t + 0.01f, out Vector3 nextPos);
                        Vector3 direction = (nextPos - position).normalized;
                        if (direction != Vector3.zero)
                            transform.right = direction;
                    }
                }

                yield return null;
            }

            Debug.Log("路径走完！");

            if (!loop)
                break;

            // 短暂停顿后重新开始
            yield return new WaitForSeconds(1f);
        }
    }
}
