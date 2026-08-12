using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MouseDraggableObject))]
public class MouseDraggableObjectEditor : Editor
{
    private static readonly string[] _rectTransformFields = { "interactionBoundsRect" };
    private static readonly string[] _colliderFields = { "interactionBoundsCollider" };

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        int boundsMode = serializedObject.FindProperty("interactionBoundsMode").enumValueIndex;
        bool isNone = boundsMode == (int)MouseDraggableObject.InteractionBoundsMode.None;
        bool isRect = boundsMode == (int)MouseDraggableObject.InteractionBoundsMode.RectTransform;
        bool isCollider = boundsMode == (int)MouseDraggableObject.InteractionBoundsMode.Collider;

        SerializedProperty prop = serializedObject.GetIterator();
        bool enterChildren = true;
        while (prop.NextVisible(enterChildren))
        {
            enterChildren = false;

            // None 模式下隐藏两个引用字段
            if (isNone && (System.Array.IndexOf(_rectTransformFields, prop.name) >= 0
                        || System.Array.IndexOf(_colliderFields, prop.name) >= 0))
                continue;
            // RectTransform 模式下隐藏 Collider 字段
            if (isRect && System.Array.IndexOf(_colliderFields, prop.name) >= 0)
                continue;
            // Collider 模式下隐藏 RectTransform 字段
            if (isCollider && System.Array.IndexOf(_rectTransformFields, prop.name) >= 0)
                continue;

            EditorGUILayout.PropertyField(prop, true);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
