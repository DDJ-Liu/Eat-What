using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(HorizontalLayout))]
public class HorizontalLayoutEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        HorizontalLayout layout = (HorizontalLayout)target;

        EditorGUILayout.Space();

        if (GUILayout.Button("排列子物体", GUILayout.Height(30)))
        {
            Undo.RegisterFullObjectHierarchyUndo(layout.gameObject, "Arrange Horizontal Layout");
            layout.ArrangeChildren();
            EditorUtility.SetDirty(layout);
        }
    }
}
