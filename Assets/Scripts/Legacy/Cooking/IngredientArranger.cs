using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 监听 CookingDropZone 内容变化，自动将已放置的食材按 ArcLayout 排列。
/// 使用 ArcLayoutStrategy 计算位置，动画设置直接读取 ArcLayout 配置。
/// </summary>
public class IngredientArranger : MonoBehaviour
{
    [Header("引用")]
    [Tooltip("要监听的 CookingDropZone")]
    [SerializeField] private CookingDropZone dropZone;

    [Tooltip("弧形布局策略")]
    [SerializeField] private ArcLayoutStrategy layoutStrategy;

    // 追踪每个食材的活动协程
    private Dictionary<Ingredient_Cooking, Coroutine> activeCoroutines = new Dictionary<Ingredient_Cooking, Coroutine>();

    private void Awake()
    {
        if (dropZone == null)
        {
            dropZone = GetComponent<CookingDropZone>();
        }

        if (dropZone != null)
        {
            dropZone.onContentsChanged.AddListener(OnContentsChanged);
        }
        else
        {
            Debug.LogError($"IngredientArranger on {gameObject.name} 找不到 CookingDropZone");
        }

        if (layoutStrategy == null)
        {
            Debug.LogError($"IngredientArranger on {gameObject.name} 未设置 layoutStrategy");
        }
    }

    private void OnDestroy()
    {
        if (dropZone != null)
        {
            dropZone.onContentsChanged.RemoveListener(OnContentsChanged);
        }

        // 停止所有活动协程
        StopAllCoroutines();
        activeCoroutines.Clear();
    }

    /// <summary>
    /// 当 CookingDropZone 内容变化时触发
    /// </summary>
    private void OnContentsChanged()
    {
        // 延迟到下一帧执行，确保食材状态已切换到 IdleState
        StartCoroutine(ArrangeNextFrame());
    }

    /// <summary>
    /// 延迟一帧后排列
    /// </summary>
    private IEnumerator ArrangeNextFrame()
    {
        yield return null;
        ArrangeIngredients();
    }

    /// <summary>
    /// 重新排列所有已放置的食材
    /// </summary>
    public void ArrangeIngredients()
    {
        Debug.Log($"[IngredientArranger] ArrangeIngredients 被调用 on {gameObject.name}");

        if (dropZone == null)
        {
            Debug.LogError($"[IngredientArranger] dropZone 为空");
            return;
        }

        if (layoutStrategy == null)
        {
            Debug.LogError($"[IngredientArranger] layoutStrategy 为空");
            return;
        }

        // 获取需要排列的食材（过滤掉正在拖拽的）
        List<Ingredient_Cooking> placedIngredients = GetPlacedIngredients();

        Debug.Log($"[IngredientArranger] 找到 {placedIngredients.Count} 个需要排列的食材（总共 {dropZone.contents.Count} 个）");

        if (placedIngredients.Count == 0)
        {
            // 没有食材需要排列，停止所有协程
            StopAllCoroutines();
            activeCoroutines.Clear();
            return;
        }

        // 停止不再需要排列的食材的协程
        List<Ingredient_Cooking> toRemove = new List<Ingredient_Cooking>();
        foreach (var kvp in activeCoroutines)
        {
            if (!placedIngredients.Contains(kvp.Key))
            {
                if (kvp.Value != null)
                {
                    StopCoroutine(kvp.Value);
                }
                toRemove.Add(kvp.Key);
            }
        }
        foreach (var ingredient in toRemove)
        {
            activeCoroutines.Remove(ingredient);
        }

        // 为每个食材计算目标位置并应用
        for (int i = 0; i < placedIngredients.Count; i++)
        {
            var ingredient = placedIngredients[i];
            if (ingredient == null) continue;

            var (targetPos, targetRot) = layoutStrategy.GetWorldPositionAt(i, placedIngredients.Count);

            // Layer 5: 使用前验证（最后防线）
            if (float.IsNaN(targetPos.x) || float.IsNaN(targetPos.y) || float.IsNaN(targetPos.z) ||
                float.IsInfinity(targetPos.x) || float.IsInfinity(targetPos.y) || float.IsInfinity(targetPos.z))
            {
                Debug.LogError($"[IngredientArranger] 食材 {i} ({ingredient.name}) 的目标位置无效: {targetPos}，跳过排列");
                continue;
            }

            Debug.Log($"[IngredientArranger] 食材 {i}: {ingredient.name} -> 目标位置: {targetPos}, 当前位置: {ingredient.transform.position}");

            // 停止该食材的旧协程
            if (activeCoroutines.ContainsKey(ingredient) && activeCoroutines[ingredient] != null)
            {
                StopCoroutine(activeCoroutines[ingredient]);
                activeCoroutines.Remove(ingredient);
            }

            // 读取 ArcLayout 的动画设置
            bool useLerp = layoutStrategy.UseLerp;

            Debug.Log($"[IngredientArranger] UseLerp: {useLerp}, LerpDuration: {layoutStrategy.LerpDuration}, LerpStagger: {layoutStrategy.LerpStagger}");

            if (useLerp)
            {
                float staggerDelay = i * layoutStrategy.LerpStagger;
                Coroutine coroutine = StartCoroutine(SmoothMove(ingredient, targetPos, targetRot, staggerDelay));
                activeCoroutines[ingredient] = coroutine;
            }
            else
            {
                ingredient.transform.position = targetPos;
                ingredient.transform.rotation = targetRot;
            }
        }
    }

    /// <summary>
    /// 获取已放置的食材列表（排除正在拖拽的）
    /// </summary>
    private List<Ingredient_Cooking> GetPlacedIngredients()
    {
        var result = new List<Ingredient_Cooking>();

        if (dropZone == null || dropZone.contents == null) return result;

        foreach (var ingredient in dropZone.contents)
        {
            if (ingredient == null)
            {
                Debug.Log("[IngredientArranger] 发现空食材引用，跳过");
                continue;
            }

            // 使用状态机判断：只排列处于 IdleState 的食材
            var currentState = ingredient.GetCurrentState();
            if (currentState == null || !(currentState is IngredientState_Idle))
            {
                Debug.Log($"[IngredientArranger] {ingredient.name} 不在 IdleState（当前状态: {currentState?.GetType().Name ?? "null"}），跳过");
                continue;
            }

            result.Add(ingredient);
        }

        return result;
    }

    /// <summary>
    /// 平滑移动协程
    /// </summary>
    private IEnumerator SmoothMove(Ingredient_Cooking ingredient, Vector3 targetPos, Quaternion targetRot, float staggerDelay)
    {
        if (ingredient == null) yield break;

        // 等待交错延迟
        if (staggerDelay > 0f)
        {
            yield return new WaitForSeconds(staggerDelay);
        }

        // 再次检查食材是否仍然存在
        if (ingredient == null) yield break;

        Transform t = ingredient.transform;
        Vector3 startPos = t.position;
        Quaternion startRot = t.rotation;
        float elapsed = 0f;
        float duration = layoutStrategy.LerpDuration;

        while (elapsed < duration)
        {
            // 每帧检查食材是否仍然存在
            if (ingredient == null || t == null)
            {
                yield break;
            }

            elapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / duration);

            t.position = Vector3.Lerp(startPos, targetPos, normalizedTime);
            t.rotation = Quaternion.Lerp(startRot, targetRot, normalizedTime);

            yield return null;
        }

        // 确保最终精确到达目标位置
        if (ingredient != null && t != null)
        {
            t.position = targetPos;
            t.rotation = targetRot;
        }

        // 清理协程记录
        if (activeCoroutines.ContainsKey(ingredient))
        {
            activeCoroutines.Remove(ingredient);
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// 编辑器辅助：在场景视图中手动触发排列（用于测试）
    /// </summary>
    [ContextMenu("手动触发排列")]
    private void ManualArrange()
    {
        ArrangeIngredients();
    }
#endif
}
