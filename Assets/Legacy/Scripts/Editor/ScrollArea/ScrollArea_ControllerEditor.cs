using UnityEngine;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine.Events;

[CustomEditor(typeof(ScrollArea_Controller))]
public class ScrollArea_ControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        ScrollArea_Controller scrollArea = (ScrollArea_Controller)target;
        var scrollMode = (ScrollArea_Controller.ScrollMode)serializedObject.FindProperty("scrollMode").enumValueIndex;

        // 绘制所有属性，按 scrollMode 条件过滤
        EditorGUI.BeginChangeCheck();
        SerializedProperty prop = serializedObject.GetIterator();
        bool enterChildren = true;
        while (prop.NextVisible(enterChildren))
        {
            enterChildren = false;

            if (prop.name == "verticalScrollBar" && scrollMode == ScrollArea_Controller.ScrollMode.Horizontal)
                continue;
            if (prop.name == "horizontalScrollBar" && scrollMode == ScrollArea_Controller.ScrollMode.Vertical)
                continue;

            EditorGUILayout.PropertyField(prop, true);
        }

        serializedObject.ApplyModifiedProperties();

        if (EditorGUI.EndChangeCheck())
        {
            var currentScrollMode = scrollArea.scrollMode;
            UpdateScrollBarActive(scrollArea, currentScrollMode);
            SyncDragContainerMode(scrollArea, currentScrollMode);
        }

        // DragLimit 类型检查提示
        if (scrollArea.viewportDragLimit != null && scrollArea.viewportDragLimit.limitType != DragLimit.LimitType.Inner)
        {
            EditorGUILayout.HelpBox("ViewPort 的 DragLimit 应设为 Inner 模式。", MessageType.Warning);
        }

        GUILayout.Space(10);
        if (GUILayout.Button("🔗 将方法持久化绑定到子组件", GUILayout.Height(30)))
        {
            BindEvents(scrollArea);
            Debug.Log("[ScrollArea] 持久化绑定完成！");
        }
    }

    private void UpdateScrollBarActive(ScrollArea_Controller scrollArea, ScrollArea_Controller.ScrollMode scrollMode)
    {
        bool verticalActive = scrollMode != ScrollArea_Controller.ScrollMode.Horizontal;
        bool horizontalActive = scrollMode != ScrollArea_Controller.ScrollMode.Vertical;

        if (scrollArea.verticalScrollBar != null)
        {
            Undo.RecordObject(scrollArea.verticalScrollBar.gameObject, "Toggle ScrollBar Active");
            scrollArea.verticalScrollBar.gameObject.SetActive(verticalActive);
        }

        if (scrollArea.horizontalScrollBar != null)
        {
            Undo.RecordObject(scrollArea.horizontalScrollBar.gameObject, "Toggle ScrollBar Active");
            scrollArea.horizontalScrollBar.gameObject.SetActive(horizontalActive);
        }
    }

    private void SyncDragContainerMode(ScrollArea_Controller scrollArea, ScrollArea_Controller.ScrollMode scrollMode)
    {
        if (scrollArea.contentDragContainer == null) return;

        DragContainer.DragMode targetMode = scrollMode switch
        {
            ScrollArea_Controller.ScrollMode.Vertical => DragContainer.DragMode.Vertical,
            ScrollArea_Controller.ScrollMode.Horizontal => DragContainer.DragMode.Horizontal,
            ScrollArea_Controller.ScrollMode.Composite => DragContainer.DragMode.Composite,
            _ => DragContainer.DragMode.Vertical
        };

        if (scrollArea.contentDragContainer.mode != targetMode)
        {
            Undo.RecordObject(scrollArea.contentDragContainer, "Sync DragContainer Mode");
            scrollArea.contentDragContainer.mode = targetMode;
            EditorUtility.SetDirty(scrollArea.contentDragContainer);

            // 运行时切换模式需要重新初始化
            if (Application.isPlaying)
            {
                scrollArea.Initialize();
            }
        }
    }

    private void BindEvents(ScrollArea_Controller scrollArea)
    {
        // 1. MouseDraggableObject → DragContainer（标准拖拽三件套）
        if (scrollArea.contentDraggable != null && scrollArea.contentDragContainer != null)
        {
            UnityEventTools.RemovePersistentListener(scrollArea.contentDraggable.startDraggingEvent, new UnityAction(scrollArea.contentDragContainer.onDragStart));
            UnityEventTools.RemovePersistentListener(scrollArea.contentDraggable.draggingUpdateEvent, new UnityAction(scrollArea.contentDragContainer.onDragUpdate));
            UnityEventTools.RemovePersistentListener(scrollArea.contentDraggable.endDraggingEvent, new UnityAction(scrollArea.contentDragContainer.onDragEnd));

            UnityEventTools.AddPersistentListener(scrollArea.contentDraggable.startDraggingEvent, new UnityAction(scrollArea.contentDragContainer.onDragStart));
            UnityEventTools.AddPersistentListener(scrollArea.contentDraggable.draggingUpdateEvent, new UnityAction(scrollArea.contentDragContainer.onDragUpdate));
            UnityEventTools.AddPersistentListener(scrollArea.contentDraggable.endDraggingEvent, new UnityAction(scrollArea.contentDragContainer.onDragEnd));

            EditorUtility.SetDirty(scrollArea.contentDraggable);
        }

        // 2. DragContainer.onDragPositionChanged → ScrollArea.OnContentPositionChanged
        if (scrollArea.contentDragContainer != null)
        {
            UnityEventTools.RemovePersistentListener(scrollArea.contentDragContainer.onDragPositionChanged, new UnityAction<Vector3>(scrollArea.OnContentPositionChanged));
            UnityEventTools.AddPersistentListener(scrollArea.contentDragContainer.onDragPositionChanged, new UnityAction<Vector3>(scrollArea.OnContentPositionChanged));

            EditorUtility.SetDirty(scrollArea.contentDragContainer);
        }

        // 3. MouseScrollableObject.scrollStepEvent → ScrollArea.OnScrollStep
        if (scrollArea.contentScrollable != null)
        {
            UnityEventTools.RemovePersistentListener(scrollArea.contentScrollable.scrollStepEvent, new UnityAction<float>(scrollArea.OnScrollStep));
            UnityEventTools.AddPersistentListener(scrollArea.contentScrollable.scrollStepEvent, new UnityAction<float>(scrollArea.OnScrollStep));

            EditorUtility.SetDirty(scrollArea.contentScrollable);
        }

        EditorUtility.SetDirty(scrollArea);
    }
}
