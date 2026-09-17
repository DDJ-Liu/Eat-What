#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TransitionController))]
public sealed class TransitionControllerEditor : Editor
{
    private string presetName = string.Empty;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();
        presetName = EditorGUILayout.TextField("Preset To Capture", presetName);
        using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(presetName)))
        {
            if (!GUILayout.Button("Capture Preset")) return;
            var controller = (TransitionController)target;
            Undo.RecordObject(controller, "Capture Transition Preset");
            if (controller.CapturePreset(presetName)) EditorUtility.SetDirty(controller);
            else Debug.LogWarning("Transition preset not found or target is missing: " + presetName, controller);
        }
    }
}
#endif
