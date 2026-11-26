using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "New Recipe", menuName = "Recipe")]
public class Recipe : ScriptableObject
{
    [Header("菜谱上下文")]
    public string recipeName;
    public Sprite recipeIcon;
    [TextArea] public string recipeHint;

    [Header("Rules 白名单（核心：本次允许命中的 Rule 集合）")]
    public List<CookingRule> whitelistRules = new List<CookingRule>();

    [Header("ToolTag 白名单（过滤 Tool 的多 Tag）")]
    public List<ToolTag> allowedToolTags = new List<ToolTag>();

    [Header("旧系统（兼容保留）")]
    public string prefabName;

    #region Hide
    public RecipeUnlockType unlock_source;
    /// <summary>
    /// Unlock Condition for Collection Book System UI, not for code trigger.
    /// </summary>
    public string unlock_condition;
    public bool is_unlocked;
    public List<RecipeVariant> variants = new List<RecipeVariant>();
    #endregion

    /// <summary>
    /// 由 whitelistRules 派生，本次允许放入 dropzone 的普通食材集合（去重）。
    /// 调用前请确保各 rule 已 RefreshDerivedData()（一般 RuleBook.Init() 会处理）。
    /// </summary>
    public IEnumerable<IngredientData> GetAllowedIngredients()
    {
        if (whitelistRules == null) yield break;
        var seen = new HashSet<IngredientData>();
        foreach (var rule in whitelistRules)
        {
            if (rule == null || rule._derivedNormalIngredients == null) continue;
            foreach (var ing in rule._derivedNormalIngredients)
                if (ing != null && seen.Add(ing))
                    yield return ing;
        }
    }

    /// <summary>
    /// 由 whitelistRules 派生，本次可能用到的通用素材集合（去重）。
    /// </summary>
    public IEnumerable<IngredientData> GetAllowedCommonMaterials()
    {
        if (whitelistRules == null) yield break;
        var seen = new HashSet<IngredientData>();
        foreach (var rule in whitelistRules)
        {
            if (rule == null || rule._derivedCommonMaterials == null) continue;
            foreach (var ing in rule._derivedCommonMaterials)
                if (ing != null && seen.Add(ing))
                    yield return ing;
        }
    }
}
