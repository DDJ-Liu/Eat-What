using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(VerticalLayout))]
public class VerticalLayoutEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        VerticalLayout layout = (VerticalLayout)target;

        EditorGUILayout.Space();

        if (GUILayout.Button("排列子物体", GUILayout.Height(30)))
        {
            Undo.RegisterFullObjectHierarchyUndo(layout.gameObject, "Arrange Vertical Layout");
            layout.ArrangeChildren();
            EditorUtility.SetDirty(layout);
        }
    }
}
