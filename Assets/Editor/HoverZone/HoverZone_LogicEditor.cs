using UnityEngine;
using UnityEditor;
using UnityEngine.Events;
using UnityEditor.Events;

[CustomEditor(typeof(HoverZone_Logic), true)]
public class HoverZone_LogicEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        HoverZone_Logic logic = (HoverZone_Logic)target;

        GUILayout.Space(10);
        if (GUILayout.Button("\ud83d\udd17 将方法持久化绑定到目标HoverZone", GUILayout.Height(30)))
        {
            if (logic.hoverZone == null)
            {
                Debug.Log("正在自动配置HoverZone绑定");
                logic.hoverZone = logic.gameObject.GetComponent<HoverZone_MouseInteract>();
                if (logic.hoverZone == null)
                {
                    Debug.LogWarning("未找到HoverZone！");
                    return;
                }
                else
                {
                    Debug.Log("已完成自动配置HoverZone");
                }
            }

            HoverZone_MouseInteract hz = logic.hoverZone;

            // 清除旧绑定
            UnityEventTools.RemovePersistentListener(hz.overEvent, new UnityAction(logic.OnHoverEnter));
            UnityEventTools.RemovePersistentListener(hz.outEvent, new UnityAction(logic.OnHoverExit));
            UnityEventTools.RemovePersistentListener(hz.hoverTriggerEvent, new UnityAction(logic.OnTrigger));

            // 添加新绑定
            UnityEventTools.AddPersistentListener(hz.overEvent, new UnityAction(logic.OnHoverEnter));
            UnityEventTools.AddPersistentListener(hz.outEvent, new UnityAction(logic.OnHoverExit));
            UnityEventTools.AddPersistentListener(hz.hoverTriggerEvent, new UnityAction(logic.OnTrigger));

            EditorUtility.SetDirty(hz);
            EditorUtility.SetDirty(logic);

            Debug.Log($"持久化绑定完成！已将 {logic.name} 的方法绑定到 {hz.name} 的 HoverZone UnityEvent。");
        }
    }
}
