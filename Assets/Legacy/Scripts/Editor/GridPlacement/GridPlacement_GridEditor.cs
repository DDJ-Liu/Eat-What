using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GridPlacement_Grid))]
public class GridPlacement_GridEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GridPlacement_Grid grid = (GridPlacement_Grid)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("网格工具", EditorStyles.boldLabel);

        if (grid.isInitialized)
        {
            EditorGUILayout.HelpBox($"网格已初始化: {grid.width} x {grid.height}", MessageType.Info);
        }

        if (GUILayout.Button("Initialize Grid"))
        {
            grid.InitializeGrid();
            EditorUtility.SetDirty(grid);
        }

        if (GUILayout.Button("Clear Grid"))
        {
            grid.ClearGrid();
            EditorUtility.SetDirty(grid);
        }
    }
}
