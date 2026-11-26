using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ArcPathController))]
public class ArcPathControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        ArcPathController controller = (ArcPathController)target;

        // 开始检测变化
        EditorGUI.BeginChangeCheck();

        // 绘制默认的 Inspector，但我们需要自定义某些字段的显示
        serializedObject.Update();

        // 路径设置
        EditorGUILayout.LabelField("路径设置", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("arcPath"));

        EditorGUILayout.Space();

        // 移动设置
        EditorGUILayout.LabelField("移动设置", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("pathDimension"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("movementMode"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("speedOrDuration"));

        // 只在 AlongCurve 模式下显示 rotateAlongPath
        if (controller.pathDimension == ArcPathController.PathDimension.AlongCurve)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("rotateAlongPath"));
        }

        EditorGUILayout.Space();

        // 返回设置
        EditorGUILayout.LabelField("返回设置", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("returnMode"));

        EditorGUILayout.Space();

        // 控制设置
        EditorGUILayout.LabelField("控制设置", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("isEnabled"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("defaultInterruptBehavior"));

        EditorGUILayout.Space();

        // 起点设置
        EditorGUILayout.LabelField("起点设置", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("startPositionMode"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("startPositionTolerance"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("lerpToStartSpeed"));

        EditorGUILayout.Space();

        // 事件
        EditorGUILayout.LabelField("事件", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("onStartMoving"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("onReachEnd"));

        serializedObject.ApplyModifiedProperties();

        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(controller);
        }
    }
}
