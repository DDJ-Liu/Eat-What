using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CookingRule))]
public class CookingRuleEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CookingRule rule = (CookingRule)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("自动派生字段（只读）", EditorStyles.boldLabel);

        EditorGUI.BeginDisabledGroup(true);
        if (rule._derivedNormalIngredients != null)
        {
            EditorGUILayout.LabelField($"派生普通食材数量: {rule._derivedNormalIngredients.Count}");
            for (int i = 0; i < rule._derivedNormalIngredients.Count; i++)
            {
                EditorGUILayout.ObjectField($"  [{i}]", rule._derivedNormalIngredients[i], typeof(IngredientData), false);
            }
        }
        if (rule._derivedCommonMaterials != null)
        {
            EditorGUILayout.LabelField($"派生通用素材数量: {rule._derivedCommonMaterials.Count}");
            for (int i = 0; i < rule._derivedCommonMaterials.Count; i++)
            {
                EditorGUILayout.ObjectField($"  [{i}]", rule._derivedCommonMaterials[i], typeof(IngredientData), false);
            }
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space();
        if (GUILayout.Button("Refresh Derived Data"))
        {
            rule.RefreshDerivedData();
            EditorUtility.SetDirty(rule);
        }
    }
}
