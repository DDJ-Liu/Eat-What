using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "New RuleBook", menuName = "CookingData/RuleBook")]
public class RuleBook : ScriptableObject
{
    public List<CookingRule> rules;

    private Dictionary<string, CookingRule> _ruleDict;

    public void Init()
    {
        _ruleDict = new Dictionary<string, CookingRule>();
        foreach (var rule in rules)
        {
            rule.RefreshDerivedData();
            string key = BuildKey(rule.ingredients, rule.containerTag, rule.toolTag);
            if (!_ruleDict.ContainsKey(key))
                _ruleDict[key] = rule;
        }
    }

    public CookingRule Query(List<IngredientData> ingredients, ContainerTag containerTag, ToolTag toolTag)
    {
        string key = BuildKey(ingredients, containerTag, toolTag);
        _ruleDict.TryGetValue(key, out var result);
        return result; // null → 调用方产出"不明物体"
    }

    private string BuildKey(List<IngredientData> ingredients, ContainerTag containerTag, ToolTag toolTag)
    {
        var ids = ingredients
            .Where(i => i != null)
            .Select(i => i.uid.ToString())
            .OrderBy(s => s);
        return string.Join(",", ids) + "|" + containerTag + "|" + toolTag;
    }
}
