using UnityEngine;
using UnityEditor;
using UnityEngine.Events;
using UnityEditor.Events;

[CustomEditor(typeof(MouseHoldObject))]
public class MouseHoldObjectEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty holdMode = serializedObject.FindProperty("holdMode");
        bool hasTimed = holdMode.enumValueIndex == (int)HoldTriggerMode.TimedAutoTrigger
                     || holdMode.enumValueIndex == (int)HoldTriggerMode.TimedReleaseTrigger;

        SerializedProperty prop = serializedObject.GetIterator();
        bool enterChildren = true;
        while (prop.NextVisible(enterChildren))
        {
            enterChildren = false;

            // holdDuration 仅在定时模式下显示
            if (prop.name == "holdDuration" && !hasTimed)
                continue;

            EditorGUILayout.PropertyField(prop, true);
        }

        serializedObject.ApplyModifiedProperties();

        MouseHoldObject hold = (MouseHoldObject)target;

        GUILayout.Space(10);
        if (GUILayout.Button("🔗 绑定所有Debug方法到事件", GUILayout.Height(30)))
        {
            BindDebugMethods(hold);
        }

        if (GUILayout.Button("🗑 移除所有Debug方法绑定", GUILayout.Height(25)))
        {
            RemoveDebugMethods(hold);
        }
    }

    private void BindDebugMethods(MouseHoldObject hold)
    {
        // 先移除已有的，避免重复绑定
        RemoveDebugMethods(hold);

        UnityEventTools.AddPersistentListener(hold.holdStartEvent, new UnityAction(hold.DebugHoldStart));
        UnityEventTools.AddPersistentListener(hold.holdUpdateEvent, new UnityAction(hold.DebugHoldUpdate));
        UnityEventTools.AddPersistentListener(hold.holdCompleteEvent, new UnityAction(hold.DebugHoldComplete));
        UnityEventTools.AddPersistentListener(hold.holdCancelEvent, new UnityAction(hold.DebugHoldCancel));
        UnityEventTools.AddPersistentListener(hold.holdReleaseEvent, new UnityAction(hold.DebugHoldRelease));

        EditorUtility.SetDirty(hold);
        Debug.Log($"[MouseHoldObjectEditor] 已将所有Debug方法绑定到 {hold.gameObject.name} 的事件");
    }

    private void RemoveDebugMethods(MouseHoldObject hold)
    {
        UnityEventTools.RemovePersistentListener(hold.holdStartEvent, new UnityAction(hold.DebugHoldStart));
        UnityEventTools.RemovePersistentListener(hold.holdUpdateEvent, new UnityAction(hold.DebugHoldUpdate));
        UnityEventTools.RemovePersistentListener(hold.holdCompleteEvent, new UnityAction(hold.DebugHoldComplete));
        UnityEventTools.RemovePersistentListener(hold.holdCancelEvent, new UnityAction(hold.DebugHoldCancel));
        UnityEventTools.RemovePersistentListener(hold.holdReleaseEvent, new UnityAction(hold.DebugHoldRelease));

        EditorUtility.SetDirty(hold);
    }
}
