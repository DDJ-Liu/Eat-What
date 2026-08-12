using UnityEngine;
using UnityEditor;
using UnityEngine.Events;
using UnityEditor.Events;

[CustomEditor(typeof(Button_Logic), true)]
public class Button_LogicEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 绘制默认 Inspector（显示 targetInteractive 字段）
        DrawDefaultInspector();

        Button_Logic logic = (Button_Logic)target;

        GUILayout.Space(10);
        if (GUILayout.Button("\ud83d\udd17 将方法持久化绑定到目标交互组件", GUILayout.Height(30)))
        {
            // -------------------- 事件注册开始 --------------------
            if (logic.button == null)
            {
                Debug.Log("正在自动配置Button绑定");
                logic.button = logic.gameObject.GetComponent<Button_MouseInteract>();
                if(logic.button == null)
                {
                    Debug.LogWarning("未找到Button！");
                    return;
                }
                else
                {
                    Debug.Log("已完成自动配置Button");
                }
            }

            Button_MouseInteract target = logic.button;

            // 1. 清除目标事件上已有的持久化监听器（避免重复绑定）
            UnityEventTools.RemovePersistentListener(target.overEvent, new UnityAction(logic.setHighlight));
            UnityEventTools.RemovePersistentListener(target.outEvent, new UnityAction(logic.setIdle));
            UnityEventTools.RemovePersistentListener(target.selectEvent, new UnityAction(logic.OnPress));

            // 2. 添加新的持久化监听器，将 logic 的方法绑定到目标事件
            UnityEventTools.AddPersistentListener(target.overEvent, new UnityAction(logic.setHighlight));
            UnityEventTools.AddPersistentListener(target.outEvent,    new UnityAction(logic.setIdle));
            UnityEventTools.AddPersistentListener(target.selectEvent,   new UnityAction(logic.OnPress));

            // 3. 标记目标对象和源对象已修改，确保 Unity 保存更改
            EditorUtility.SetDirty(target);
            EditorUtility.SetDirty(logic);
            // -------------------- 事件注册结束 --------------------

            Debug.Log($"持久化绑定完成！已将 {logic.name} 的方法绑定到 {target.name} 的 UnityEvent。");
        }
    }
}
