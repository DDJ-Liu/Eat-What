using UnityEngine;
using UnityEditor;
using UnityEngine.Events;
using UnityEditor.Events;
using System.Collections.Generic;

/// <summary>
/// 全局静态类：不依赖 CustomEditor 生命周期，持续监听所有 ScrollBar 的 handle 位置变化并反向同步到 value
/// </summary>
[InitializeOnLoad]
public static class ScrollBar_HandleSync
{
    private static Dictionary<ScrollBar_Controller, Vector3> _lastPositions = new Dictionary<ScrollBar_Controller, Vector3>();

    static ScrollBar_HandleSync()
    {
        EditorApplication.update += OnEditorUpdate;
    }

    private static void OnEditorUpdate()
    {
        if (Application.isPlaying) return;

        var scrollBars = Object.FindObjectsByType<ScrollBar_Controller>(FindObjectsSortMode.None);

        // 清理已销毁的实例
        var toRemove = new List<ScrollBar_Controller>();
        foreach (var kvp in _lastPositions)
        {
            if (kvp.Key == null) toRemove.Add(kvp.Key);
        }
        foreach (var key in toRemove)
        {
            _lastPositions.Remove(key);
        }

        foreach (var scrollBar in scrollBars)
        {
            if (scrollBar.handleDragContainer == null || scrollBar.trackStart == null || scrollBar.trackEnd == null)
                continue;

            Vector3 currentPos = scrollBar.handleDragContainer.transform.position;

            if (!_lastPositions.TryGetValue(scrollBar, out Vector3 lastPos))
            {
                _lastPositions[scrollBar] = currentPos;
                continue;
            }

            if (currentPos == lastPos) continue;

            _lastPositions[scrollBar] = currentPos;

            // 反算 value
            Vector3 trackDir = scrollBar.trackEnd.position - scrollBar.trackStart.position;
            float trackLength = trackDir.magnitude;
            if (trackLength < 0.001f) continue;

            float projected = Vector3.Dot(currentPos - scrollBar.trackStart.position, trackDir.normalized);
            float newValue = Mathf.Clamp01(projected / trackLength);

            // 将 handle 钳制回轨道上
            Vector3 clampedPos = Vector3.Lerp(scrollBar.trackStart.position, scrollBar.trackEnd.position, newValue);
            if (currentPos != clampedPos)
            {
                Undo.RecordObject(scrollBar.handleDragContainer.transform, "ScrollBar Clamp Handle");
                scrollBar.handleDragContainer.transform.position = clampedPos;
                _lastPositions[scrollBar] = clampedPos;
            }

            var so = new SerializedObject(scrollBar);
            var valueProp = so.FindProperty("_value");
            if (!Mathf.Approximately(valueProp.floatValue, newValue))
            {
                valueProp.floatValue = newValue;
                so.ApplyModifiedProperties();
            }
        }
    }
}

[CustomEditor(typeof(ScrollBar_Controller))]
public class ScrollBar_ControllerEditor : Editor
{
    private SerializedProperty _valueProp;

    private void OnEnable()
    {
        _valueProp = serializedObject.FindProperty("_value");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 绘制除 _value 以外的所有属性
        SerializedProperty prop = serializedObject.GetIterator();
        bool enterChildren = true;
        while (prop.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (prop.name == "_value") continue;
            EditorGUILayout.PropertyField(prop, true);
        }

        // _value 用 Slider 绘制
        float oldValue = _valueProp.floatValue;
        EditorGUILayout.Slider(_valueProp, 0f, 1f, new GUIContent("Value"));
        serializedObject.ApplyModifiedProperties();

        float newValue = _valueProp.floatValue;
        if (!Mathf.Approximately(oldValue, newValue) && !Application.isPlaying)
        {
            SyncHandlePosition(newValue);
        }

        ScrollBar_Controller scrollBar = (ScrollBar_Controller)target;

        GUILayout.Space(10);
        if (GUILayout.Button("🔗 将方法持久化绑定到子组件", GUILayout.Height(30)))
        {
            BindEvents(scrollBar);
            Debug.Log($"[ScrollBar] 持久化绑定完成！");
        }
    }

    private void BindEvents(ScrollBar_Controller scrollBar)
    {
        // ==================== 1. MouseDraggableObject → DragContainer（标准拖拽绑定） ====================
        if (scrollBar.handleDraggable != null && scrollBar.handleDragContainer != null)
        {
            UnityEventTools.RemovePersistentListener(scrollBar.handleDraggable.startDraggingEvent, new UnityAction(scrollBar.handleDragContainer.onDragStart));
            UnityEventTools.RemovePersistentListener(scrollBar.handleDraggable.draggingUpdateEvent, new UnityAction(scrollBar.handleDragContainer.onDragUpdate));
            UnityEventTools.RemovePersistentListener(scrollBar.handleDraggable.endDraggingEvent, new UnityAction(scrollBar.handleDragContainer.onDragEnd));

            UnityEventTools.AddPersistentListener(scrollBar.handleDraggable.startDraggingEvent, new UnityAction(scrollBar.handleDragContainer.onDragStart));
            UnityEventTools.AddPersistentListener(scrollBar.handleDraggable.draggingUpdateEvent, new UnityAction(scrollBar.handleDragContainer.onDragUpdate));
            UnityEventTools.AddPersistentListener(scrollBar.handleDraggable.endDraggingEvent, new UnityAction(scrollBar.handleDragContainer.onDragEnd));

            EditorUtility.SetDirty(scrollBar.handleDraggable);
        }

        // ==================== 2. DragContainer.onDragPositionChanged → ScrollBar.OnHandlePositionChanged ====================
        if (scrollBar.handleDragContainer != null)
        {
            UnityEventTools.RemovePersistentListener(scrollBar.handleDragContainer.onDragPositionChanged, new UnityAction<Vector3>(scrollBar.OnHandlePositionChanged));
            UnityEventTools.AddPersistentListener(scrollBar.handleDragContainer.onDragPositionChanged, new UnityAction<Vector3>(scrollBar.OnHandlePositionChanged));

            EditorUtility.SetDirty(scrollBar.handleDragContainer);
        }

        // ==================== 3. MouseScrollableObject.scrollStepEvent → ScrollBar.OnScrollStep ====================
        if (scrollBar.scrollableObject != null)
        {
            UnityEventTools.RemovePersistentListener(scrollBar.scrollableObject.scrollStepEvent, new UnityAction<float>(scrollBar.OnScrollStep));
            UnityEventTools.AddPersistentListener(scrollBar.scrollableObject.scrollStepEvent, new UnityAction<float>(scrollBar.OnScrollStep));

            EditorUtility.SetDirty(scrollBar.scrollableObject);
        }

        EditorUtility.SetDirty(scrollBar);
    }

    private void SyncHandlePosition(float value)
    {
        ScrollBar_Controller scrollBar = (ScrollBar_Controller)target;
        Transform trackStart = scrollBar.trackStart;
        Transform trackEnd = scrollBar.trackEnd;
        DragContainer handle = scrollBar.handleDragContainer;

        if (trackStart == null || trackEnd == null || handle == null) return;

        Vector3 targetPos = Vector3.Lerp(trackStart.position, trackEnd.position, value);
        Undo.RecordObject(handle.transform, "ScrollBar Value Change");
        handle.transform.position = targetPos;
    }
}
