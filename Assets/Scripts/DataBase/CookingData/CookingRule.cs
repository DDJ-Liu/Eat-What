using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "New CookingRule", menuName = "CookingData/CookingRule")]
public class CookingRule : ScriptableObject
{
    [Header("手动配置")]
    public ContainerTag containerTag;
    [Tooltip("全部食材（普通食材 + 通用素材都填这里），按 IngredientData.isCommonMaterial 自动分桶")]
    public List<IngredientData> ingredients = new List<IngredientData>();
    public ToolTag toolTag;
    public List<ProcessedIngredientData> outputs;
    public bool isFinal = false;

    [Header("自动派生（只读，点击 Refresh 更新）")]
    [HideInInspector] public List<IngredientData> _derivedNormalIngredients;
    [HideInInspector] public List<IngredientData> _derivedCommonMaterials;

    /// <summary>
    /// 把 ingredients 按 isCommonMaterial 分桶到 _derivedNormalIngredients / _derivedCommonMaterials。
    /// 由 Editor 按钮或 RuleBook.Init() 调用。
    /// </summary>
    public void RefreshDerivedData()
    {
        _derivedNormalIngredients = new List<IngredientData>();
        _derivedCommonMaterials = new List<IngredientData>();

        if (ingredients == null) return;

        foreach (var ing in ingredients)
        {
            if (ing == null) continue;
            if (ing.isCommonMaterial) _derivedCommonMaterials.Add(ing);
            else _derivedNormalIngredients.Add(ing);
        }
    }

    public override string ToString()
    {
        string start = $"{name}: ";
        string body = "";
        foreach(IngredientData ingredient in ingredients)
        {
            body += ingredient.name + " + ";
        }
        return start + body;
    }
}
