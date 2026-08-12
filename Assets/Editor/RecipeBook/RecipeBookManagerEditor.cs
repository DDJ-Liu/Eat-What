using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RecipeBookManager))]
public class RecipeBookManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10);
        EditorGUILayout.LabelField("── Debug ──", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("上一页", GUILayout.Height(30)))
        {
            var manager = (RecipeBookManager)target;
            manager.PreviousPage();
        }

        if (GUILayout.Button("下一页", GUILayout.Height(30)))
        {
            var manager = (RecipeBookManager)target;
            manager.NextPage();
        }

        EditorGUILayout.EndHorizontal();
    }
}
