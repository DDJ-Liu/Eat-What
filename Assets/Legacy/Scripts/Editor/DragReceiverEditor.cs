using UnityEngine;
using UnityEditor;
using UnityEngine.Events;
using UnityEditor.Events;

[CustomEditor(typeof(DragContainer))]
public class DragContainerEditor : Editor
{
    private static readonly string[] _inertiaFields = { "inertiaDeceleration", "maxInertiaSpeed", "minInertiaSpeed", "inertiaCurve" };
    private static readonly string[] _elasticFields = { "elasticFactor", "elasticReturnSpeed", "maxElasticOffset" };
    private static readonly string[] _manualLimitFields = { "manualLimitEvent", "leftLimit", "rightLimit", "topLimit", "bottomLimit" };
    private static readonly string[] _dragLimitFields = { "dragLimit" };

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        bool enableInertia = serializedObject.FindProperty("enableInertia").boolValue;
        bool enableElastic = serializedObject.FindProperty("enableElasticBoundary").boolValue;
        int limitMode = serializedObject.FindProperty("limitMode").enumValueIndex;
        bool isDragLimit = limitMode == (int)DragContainer.LimitMode.DragLimit;
        bool isManual = limitMode == (int)DragContainer.LimitMode.Manual;

        SerializedProperty prop = serializedObject.GetIterator();
        bool enterChildren = true;
        while (prop.NextVisible(enterChildren))
        {
            enterChildren = false;

            if (!enableInertia && System.Array.IndexOf(_inertiaFields, prop.name) >= 0)
                continue;
            if (!enableElastic && System.Array.IndexOf(_elasticFields, prop.name) >= 0)
                continue;
            if (isDragLimit && System.Array.IndexOf(_manualLimitFields, prop.name) >= 0)
                continue;
            if (isManual && System.Array.IndexOf(_dragLimitFields, prop.name) >= 0)
                continue;

            EditorGUILayout.PropertyField(prop, true);
        }

        serializedObject.ApplyModifiedProperties();

        DragContainer container = (DragContainer)target;

        GUILayout.Space(10);
        if (GUILayout.Button("🔗 将方法持久化绑定到目标交互组件", GUILayout.Height(30)))
        {
            // -------------------- 事件注册开始 --------------------
            MouseDraggableObject receiver = container.gameObject.GetComponent<MouseDraggableObject>();

            // 1. 清除目标事件上已有的持久化监听器（避免重复绑定）
            UnityEventTools.RemovePersistentListener(receiver.startDraggingEvent, new UnityAction(container.onDragStart));
            UnityEventTools.RemovePersistentListener(receiver.draggingUpdateEvent, new UnityAction(container.onDragUpdate));
            UnityEventTools.RemovePersistentListener(receiver.endDraggingEvent, new UnityAction(container.onDragEnd));

            // 2. 添加新的持久化监听器，将 visual 的方法绑定到目标事件
            UnityEventTools.AddPersistentListener(receiver.startDraggingEvent, new UnityAction(container.onDragStart));
            UnityEventTools.AddPersistentListener(receiver.draggingUpdateEvent, new UnityAction(container.onDragUpdate));
            UnityEventTools.AddPersistentListener(receiver.endDraggingEvent, new UnityAction(container.onDragEnd));

            // 3. 标记目标对象和源对象已修改，确保 Unity 保存更改
            EditorUtility.SetDirty(container);
            EditorUtility.SetDirty(receiver);
            // -------------------- 事件注册结束 --------------------

            Debug.Log($"持久化绑定完成！已将 {container.name} 的方法绑定到 {receiver.name} 的 UnityEvent。");
        }
    }
}