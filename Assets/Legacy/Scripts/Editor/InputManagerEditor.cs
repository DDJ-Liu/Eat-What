using UnityEditor;

[CustomEditor(typeof(InputManager))]
public class InputManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty prop = serializedObject.GetIterator();
        bool enterChildren = true;

        while (prop.NextVisible(enterChildren))
        {
            enterChildren = false;

            // 当 useInputBuffer 为 false 时，跳过 inputBufferTime
            if (prop.name == "inputBufferTime")
            {
                SerializedProperty useBuffer = serializedObject.FindProperty("useInputBuffer");
                if (useBuffer != null && !useBuffer.boolValue)
                    continue;
            }

            EditorGUILayout.PropertyField(prop, true);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
