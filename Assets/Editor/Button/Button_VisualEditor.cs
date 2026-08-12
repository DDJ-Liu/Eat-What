using UnityEngine;
using UnityEditor;
using UnityEngine.Events;
using UnityEditor.Events;

[CustomEditor(typeof(Button_Visual))]
public class Button_VisualEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 绘制默认 Inspector（显示 targetInteractive 字段）
        DrawDefaultInspector();

        Button_Visual visual = (Button_Visual)target;

        GUILayout.Space(10);
        if (GUILayout.Button("🔗 将方法持久化绑定到目标交互组件", GUILayout.Height(30)))
        {
            // -------------------- 事件注册开始 --------------------
            // 在这里实现将 visual 的三个方法绑定到 targetInteractive 的 UnityEvent
            if (visual.button == null)
            {
                Debug.Log("正在自动配置Button绑定");
                visual.button = visual.gameObject.GetComponent<Button_MouseInteract>();
                if(visual.button == null)
                {
                    Debug.LogWarning("未找到Button！");
                    return;
                }
                else
                {
                    Debug.Log("已完成自动配置Button");
                }
            }

            Button_MouseInteract target = visual.button;

            // 1. 清除目标事件上已有的持久化监听器（避免重复绑定）
            UnityEventTools.RemovePersistentListener(target.overEvent, new UnityAction(visual.setHighlight));
            UnityEventTools.RemovePersistentListener(target.outEvent, new UnityAction(visual.setIdle));
            UnityEventTools.RemovePersistentListener(target.selectEvent, new UnityAction(visual.OnPress));

            // 2. 添加新的持久化监听器，将 visual 的方法绑定到目标事件
            UnityEventTools.AddPersistentListener(target.overEvent, new UnityAction(visual.setHighlight));
            UnityEventTools.AddPersistentListener(target.outEvent,    new UnityAction(visual.setIdle));
            UnityEventTools.AddPersistentListener(target.selectEvent,   new UnityAction(visual.OnPress));

            // 3. 标记目标对象和源对象已修改，确保 Unity 保存更改
            EditorUtility.SetDirty(target);
            EditorUtility.SetDirty(visual);
            // -------------------- 事件注册结束 --------------------

            Debug.Log($"持久化绑定完成！已将 {visual.name} 的方法绑定到 {target.name} 的 UnityEvent。");
        }
    }
}