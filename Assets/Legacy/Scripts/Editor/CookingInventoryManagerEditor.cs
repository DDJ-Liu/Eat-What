using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

[CustomEditor(typeof(CookingInventoryUIManager))]
public class CookingInventoryUIManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10);
        EditorGUILayout.LabelField("── Debug ──", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("刷新重置", GUILayout.Height(30)))
        {
            var manager = (CookingInventoryUIManager)target;
            RecordIcons(manager, "Inventory Refresh Reset");
            manager.currentRow = 0;
            manager.Refresh();
            ApplyIconChanges(manager);
        }

        if (GUILayout.Button("上一页", GUILayout.Height(30)))
        {
            var manager = (CookingInventoryUIManager)target;
            RecordIcons(manager, "Inventory Prev Row");
            manager.PrevRow();
            ApplyIconChanges(manager);
        }

        if (GUILayout.Button("下一页", GUILayout.Height(30)))
        {
            var manager = (CookingInventoryUIManager)target;
            RecordIcons(manager, "Inventory Next Row");
            manager.NextRow();
            ApplyIconChanges(manager);
        }

        EditorGUILayout.EndHorizontal();
    }

    private static void RecordIcons(CookingInventoryUIManager manager, string label)
    {
        Undo.RecordObject(manager, label);
        foreach (var icon in manager.InventoryItemIcons)
        {
            if (icon == null) continue;
            Undo.RecordObject(icon, label);
            if (icon.ItemSprite != null) Undo.RecordObject(icon.ItemSprite, label);
        }
    }

    private static void ApplyIconChanges(CookingInventoryUIManager manager)
    {
        EditorUtility.SetDirty(manager);
        foreach (var icon in manager.InventoryItemIcons)
        {
            if (icon == null) continue;
            EditorUtility.SetDirty(icon);
            if (icon.ItemSprite != null) EditorUtility.SetDirty(icon.ItemSprite);
        }

        if (!Application.isPlaying)
            EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);

        SceneView.RepaintAll();
    }
}
