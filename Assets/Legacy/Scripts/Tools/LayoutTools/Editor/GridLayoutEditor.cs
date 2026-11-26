using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GridLayout))]
public class GridLayoutEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GridLayout layout = (GridLayout)target;

        EditorGUILayout.Space();

        if (GUILayout.Button("排列子物体", GUILayout.Height(30)))
        {
            Undo.RegisterFullObjectHierarchyUndo(layout.gameObject, "Arrange Grid Layout");
            layout.ArrangeChildren();
            EditorUtility.SetDirty(layout);
        }
    }
}
