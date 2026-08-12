using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

[CustomEditor(typeof(ColorImageButton_Visual))]
public class ColorImageButton_VisualEditor : Button_VisualEditor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        GUILayout.Space(12);
        if (GUILayout.Button("🔍 自动查找子物体 Canvas 下的 Image", GUILayout.Height(26)))
        {
            AutoFindImage();
        }
    }

    private void AutoFindImage()
    {
        serializedObject.Update();
        SerializedProperty prop = serializedObject.FindProperty("targetImage");
        if (prop == null)
        {
            Debug.LogWarning("[ColorImageButton_Visual] 找不到 targetImage 字段");
            return;
        }

        Button_Visual visual = (Button_Visual)target;
        Image found = ImageButtonEditorHelper.FindImageInChildCanvas(visual.gameObject);

        if (found != null)
        {
            prop.objectReferenceValue = found;
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
            Debug.Log($"[ColorImageButton_Visual] 已自动绑定 Image: {found.gameObject.name}");
        }
        else
        {
            Debug.LogWarning("[ColorImageButton_Visual] 未在子物体的 Canvas 下找到任何 Image 组件");
        }
    }
}
