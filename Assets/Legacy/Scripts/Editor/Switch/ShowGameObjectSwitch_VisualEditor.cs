using UnityEngine;
using UnityEditor;
using UnityEngine.Events;
using UnityEditor.Events;

[CustomEditor(typeof(ShowGameObjectSwitch_Visual))]
public class ShowGameObjectSwitch_VisualEditor : Switch_VisualEditor
{
    private static readonly string[] _fadeFields = { "fadeInDuration", "fadeInCurve", "fadeOutDuration", "fadeOutCurve" };

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        bool enableFade = serializedObject.FindProperty("enableFade").boolValue;

        SerializedProperty prop = serializedObject.GetIterator();
        bool enterChildren = true;
        while (prop.NextVisible(enterChildren))
        {
            enterChildren = false;

            if (!enableFade && System.Array.IndexOf(_fadeFields, prop.name) >= 0)
                continue;

            EditorGUILayout.PropertyField(prop, true);
        }

        serializedObject.ApplyModifiedProperties();

        // 绘制 Switch 绑定按钮
        Switch_Visual visual = (Switch_Visual)target;

        GUILayout.Space(10);
        if (GUILayout.Button("🔗 将方法持久化绑定到 Switch 交互组件", GUILayout.Height(30)))
        {
            if (visual.button == null)
            {
                Debug.Log("正在自动配置 Switch 绑定");
                visual.button = visual.gameObject.GetComponent<Button_MouseInteract>();
                if (visual.button == null)
                {
                    Debug.LogWarning("未找到 Button_MouseInteract！");
                    return;
                }
            }

            Switch_MouseInteract switchTarget = visual.gameObject.GetComponent<Switch_MouseInteract>();
            if (switchTarget == null)
            {
                Debug.LogWarning("未找到 Switch_MouseInteract！");
                return;
            }

            Button_MouseInteract buttonTarget = visual.button;

            UnityEventTools.RemovePersistentListener(buttonTarget.overEvent, new UnityAction(visual.setHighlight));
            UnityEventTools.RemovePersistentListener(buttonTarget.outEvent, new UnityAction(visual.setIdle));
            UnityEventTools.RemovePersistentListener(buttonTarget.selectEvent, new UnityAction(visual.OnPress));
            UnityEventTools.RemovePersistentListener(switchTarget.onStateChanged, new UnityAction<int>(visual.OnStateChanged));

            UnityEventTools.AddPersistentListener(buttonTarget.overEvent, new UnityAction(visual.setHighlight));
            UnityEventTools.AddPersistentListener(buttonTarget.outEvent, new UnityAction(visual.setIdle));
            UnityEventTools.AddPersistentListener(buttonTarget.selectEvent, new UnityAction(visual.OnPress));
            UnityEventTools.AddPersistentListener(switchTarget.onStateChanged, new UnityAction<int>(visual.OnStateChanged));

            EditorUtility.SetDirty(buttonTarget);
            EditorUtility.SetDirty(switchTarget);
            EditorUtility.SetDirty(visual);

            Debug.Log($"持久化绑定完成！已将 {visual.name} 的方法绑定到 {buttonTarget.name} 的 UnityEvent。");
        }
    }
}
