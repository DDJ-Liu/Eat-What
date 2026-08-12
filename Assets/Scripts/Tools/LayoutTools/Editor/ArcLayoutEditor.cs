using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ArcLayout))]
public class ArcLayoutEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ArcLayout arcLayout = (ArcLayout)target;

        // 使用反射读取私有字段进行验证
        var spacingMode = GetFieldValue<SpacingMode>(arcLayout, "spacingMode");
        var arrangementDirection = GetFieldValue<ArrangementDirection>(arcLayout, "arrangementDirection");
        var enableDeadZone = GetFieldValue<bool>(arcLayout, "enableDeadZone");
        var fixedAngleSpacing = GetFieldValue<float>(arcLayout, "fixedAngleSpacing");
        var arcAngle = GetFieldValue<float>(arcLayout, "arcAngle");

        int childCount = arcLayout.transform.childCount;

        // 警告1：固定间隔超出弧长
        if (spacingMode == SpacingMode.Fixed && childCount > 1)
        {
            float totalRequired = fixedAngleSpacing * (childCount - 1);
            float arcLength = Mathf.Abs(arcAngle);
            if (totalRequired > arcLength)
            {
                EditorGUILayout.HelpBox(
                    $"固定间隔过大：需要 {totalRequired:F1}°，但弧长只有 {arcLength:F1}°。布局时将回退到均匀分布。",
                    MessageType.Warning);
            }
        }

        // 警告2：CenterBalance + 死区
        if (arrangementDirection == ArrangementDirection.CenterBalance && enableDeadZone)
        {
            EditorGUILayout.HelpBox(
                "中心平衡模式与死区功能组合时，将在各段内独立应用中心平衡，可能不符合全局平衡预期。",
                MessageType.Info);
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("排列子物体", GUILayout.Height(30)))
        {
            Undo.RegisterFullObjectHierarchyUndo(arcLayout.gameObject, "Arrange Arc Layout");
            arcLayout.ArrangeChildren();
            EditorUtility.SetDirty(arcLayout);
        }
    }

    private T GetFieldValue<T>(object obj, string fieldName)
    {
        var field = obj.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);
        return field != null ? (T)field.GetValue(obj) : default(T);
    }
}
