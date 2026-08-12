using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DogManager))]
public class DogManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 绘制默认的 Inspector
        DrawDefaultInspector();

        EditorGUILayout.Space(10);

        DogManager dogManager = (DogManager)target;

        // 添加测试按钮
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("测试工具", EditorStyles.boldLabel);

        if (GUILayout.Button("显示样例警告文本", GUILayout.Height(30)))
        {
            if (Application.isPlaying)
            {
                dogManager.ShowTestWarningText();
            }
            else
            {
                Debug.LogWarning("[DogManagerEditor] 请在运行模式下测试警告文本显示！");
            }
        }

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("需要在运行模式（Play Mode）下才能测试警告文本显示", MessageType.Info);
        }

        EditorGUILayout.EndVertical();
    }
}
