using UnityEngine;
using System.Collections;

/// <summary>
/// 测试小球：在固定时间内沿弧线往返移动
/// 从起点到终点，再从终点返回起点，循环往复
/// </summary>
public class TestBall : MonoBehaviour
{
    [Header("弧线路径")]
    public ArcPathComponent arcPath;

    [Header("移动设置")]
    [Tooltip("从起点到终点的移动时间（秒）")]
    public float moveDuration = 3f;

    [Tooltip("到达端点后的停顿时间（秒）")]
    public float pauseDuration = 0.5f;

    [Header("调试")]
    [SerializeField] private bool isMovingForward = true;
    [SerializeField] private float currentProgress = 0f;

    private bool isPaused = false;
    private Coroutine moveCoroutine;

    void Start()
    {
        if (arcPath == null)
        {
            Debug.LogError("[TestBall] 未指定 arcPath！");
            return;
        }

        if (arcPath.GetControlPoints().Count < 2)
        {
            Debug.LogError("[TestBall] arcPath 控制点少于2个！");
            return;
        }

        // 从起点开始
        StartMovement();
    }

    public void StartMovement()
    {
        if (moveCoroutine != null)
            StopCoroutine(moveCoroutine);

        moveCoroutine = StartCoroutine(MoveAlongArc());
    }

    private IEnumerator MoveAlongArc()
    {
        while (true)
        {
            // 移动阶段
            float elapsed = 0f;
            float startProgress = isMovingForward ? 0f : 1f;
            float endProgress = isMovingForward ? 1f : 0f;

            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / moveDuration);

                // 根据方向插值
                currentProgress = Mathf.Lerp(startProgress, endProgress, t);

                // 使用弧长均匀模式获取位置和切线
                if (arcPath.EvaluateByNormalizedDistance(currentProgress, out Vector3 position, out Vector3 tangent))
                {
                    transform.position = position;

                    // 根据移动方向设置朝向
                    if (tangent != Vector3.zero)
                    {
                        Vector3 direction = isMovingForward ? tangent : -tangent;
                        transform.right = direction;
                    }
                }

                yield return null;
            }

            // 确保到达终点
            currentProgress = endProgress;
            if (arcPath.EvaluateByNormalizedDistance(currentProgress, out Vector3 finalPos, out Vector3 finalTangent))
            {
                transform.position = finalPos;

                if (finalTangent != Vector3.zero)
                {
                    Vector3 direction = isMovingForward ? finalTangent : -finalTangent;
                    transform.right = direction;
                }
            }

            // 停顿阶段
            if (pauseDuration > 0f)
            {
                isPaused = true;
                yield return new WaitForSeconds(pauseDuration);
                isPaused = false;
            }

            // 反向
            isMovingForward = !isMovingForward;
        }
    }

    private void OnDrawGizmos()
    {
        if (arcPath == null || !Application.isPlaying)
            return;

        // 绘制当前位置
        Gizmos.color = isPaused ? Color.yellow : (isMovingForward ? Color.green : Color.red);
        Gizmos.DrawWireSphere(transform.position, 0.2f);

        // 绘制方向指示
        Vector3 direction = transform.right * 0.5f;
        Gizmos.DrawLine(transform.position, transform.position + direction);
    }
}
