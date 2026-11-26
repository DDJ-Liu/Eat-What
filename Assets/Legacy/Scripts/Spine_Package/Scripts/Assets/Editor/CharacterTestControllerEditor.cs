using UnityEngine;
using UnityEditor;

/// <summary>
/// CharacterTestController 的自定义编辑器
/// 在 Inspector 中添加便捷按钮
/// </summary>
[CustomEditor(typeof(CharacterTestController))]
public class CharacterTestControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 绘制默认的 Inspector
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("测试功能", EditorStyles.boldLabel);

        CharacterTestController controller = (CharacterTestController)target;

        // 添加 SyncEquipment 按钮
        if (GUILayout.Button("同步装备到 EquipmentCore"))
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "需要运行时执行",
                    "SyncEquipment 方法需要在运行时（Play Mode）下执行。",
                    "确定"
                );
            }
            else
            {
                controller.SyncEquipment();
            }
        }

        // 显示提示信息
        EditorGUILayout.HelpBox(
            "在 Play Mode 下点击按钮可将 equipments 列表中的装备同步到 equipmentCore。\n\n" +
            "注意：skinName 为空或 'Non' 的装备会被跳过。如需卸下装备，请在运行时使用场景中的按钮调用 UnequipSlot 方法。",
            MessageType.Info
        );
    }
}
