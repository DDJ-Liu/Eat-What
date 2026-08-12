using UnityEngine;
using UnityEditor;
using UnityEngine.Events;
using UnityEditor.Events;

[CustomEditor(typeof(ShowGameObjectButton_Visual))]
public class ShowGameObjectButton_VisualEditor : Button_VisualEditor
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

            // enableFade 为 false 时隐藏淡入淡出参数
            if (!enableFade && System.Array.IndexOf(_fadeFields, prop.name) >= 0)
                continue;

            EditorGUILayout.PropertyField(prop, true);
        }

        serializedObject.ApplyModifiedProperties();

        // 绘制父类的绑定按钮
        DrawBindButton();
    }

    protected void DrawBindButton()
    {
        Button_Visual visual = (Button_Visual)target;

        GUILayout.Space(10);
        if (GUILayout.Button("🔗 将方法持久化绑定到目标交互组件", GUILayout.Height(30)))
        {
            if (visual.button == null)
            {
                Debug.Log("正在自动配置Button绑定");
                visual.button = visual.gameObject.GetComponent<Button_MouseInteract>();
                if (visual.button == null)
                {
                    Debug.LogWarning("未找到Button！");
                    return;
                }
                else
                {
                    Debug.Log("已完成自动配置Button");
                }
            }

            Button_MouseInteract t = visual.button;

            UnityEventTools.RemovePersistentListener(t.overEvent, new UnityAction(visual.setHighlight));
            UnityEventTools.RemovePersistentListener(t.outEvent, new UnityAction(visual.setIdle));
            UnityEventTools.RemovePersistentListener(t.selectEvent, new UnityAction(visual.OnPress));

            UnityEventTools.AddPersistentListener(t.overEvent, new UnityAction(visual.setHighlight));
            UnityEventTools.AddPersistentListener(t.outEvent, new UnityAction(visual.setIdle));
            UnityEventTools.AddPersistentListener(t.selectEvent, new UnityAction(visual.OnPress));

            EditorUtility.SetDirty(t);
            EditorUtility.SetDirty(visual);

            Debug.Log($"持久化绑定完成！已将 {visual.name} 的方法绑定到 {t.name} 的 UnityEvent。");
        }
    }
}
