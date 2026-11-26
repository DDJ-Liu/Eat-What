using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RuleBook))]
public class RuleBookEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        RuleBook ruleBook = (RuleBook)target;

        EditorGUILayout.Space();
        if (GUILayout.Button("扫描同目录所有 CookingRule"))
        {
            CollectRulesInSameFolder(ruleBook);
        }
    }

    private void CollectRulesInSameFolder(RuleBook ruleBook)
    {
        string assetPath = AssetDatabase.GetAssetPath(ruleBook);
        if (string.IsNullOrEmpty(assetPath))
        {
            Debug.LogWarning("RuleBook 资源路径无效，请先保存为 .asset 文件。");
            return;
        }

        string folder = Path.GetDirectoryName(assetPath).Replace('\\', '/');
        string[] guids = AssetDatabase.FindAssets("t:CookingRule", new[] { folder });

        var rules = new List<CookingRule>();
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var rule = AssetDatabase.LoadAssetAtPath<CookingRule>(path);
            if (rule != null) rules.Add(rule);
        }

        Undo.RecordObject(ruleBook, "Collect CookingRules");
        ruleBook.rules = rules;
        EditorUtility.SetDirty(ruleBook);
        AssetDatabase.SaveAssetIfDirty(ruleBook);

        Debug.Log($"[RuleBook] 在 {folder} 收集到 {rules.Count} 个 CookingRule。");
    }
}
