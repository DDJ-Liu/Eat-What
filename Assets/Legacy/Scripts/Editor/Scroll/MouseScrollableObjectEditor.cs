using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;

[CustomEditor(typeof(MouseScrollableObject))]
public class MouseScrollableObjectEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        MouseScrollableObject scrollable = (MouseScrollableObject)target;

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Debug 工具", EditorStyles.boldLabel);

        if (GUILayout.Button("🔗 绑定 Debug 方法到 scrollStepEvent", GUILayout.Height(30)))
        {
            BindDebug(scrollable);
            Debug.Log($"[MouseScrollableObject] Debug 方法已绑定到 {scrollable.gameObject.name}");
        }

        if (GUILayout.Button("❌ 解绑 Debug 方法", GUILayout.Height(30)))
        {
            UnbindDebug(scrollable);
            Debug.Log($"[MouseScrollableObject] Debug 方法已从 {scrollable.gameObject.name} 解绑");
        }
    }

    private void BindDebug(MouseScrollableObject scrollable)
    {
        // 先移除避免重复
        UnityEventTools.RemovePersistentListener(scrollable.scrollStepEvent, new UnityAction<float>(scrollable.DebugLogScrollStep));
        UnityEventTools.AddPersistentListener(scrollable.scrollStepEvent, new UnityAction<float>(scrollable.DebugLogScrollStep));
        EditorUtility.SetDirty(scrollable);
    }

    private void UnbindDebug(MouseScrollableObject scrollable)
    {
        UnityEventTools.RemovePersistentListener(scrollable.scrollStepEvent, new UnityAction<float>(scrollable.DebugLogScrollStep));
        EditorUtility.SetDirty(scrollable);
    }
}
