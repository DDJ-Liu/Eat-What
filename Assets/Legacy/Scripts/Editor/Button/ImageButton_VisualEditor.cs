using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEngine.Events;
using UnityEditor.Events;

[CustomEditor(typeof(ImageButton_Visual))]
public class ImageButton_VisualEditor : Button_VisualEditor
{
    private const string PrefFolderPath = "ImageButtonVisual_FolderPath";

    private static string GetSuffix(string key, string defaultVal)
        => EditorPrefs.GetString("ImageButtonVisual_Suffix_" + key, defaultVal);

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        GUILayout.Space(12);
        EditorGUILayout.LabelField("── 自动填充 Sprites ──", EditorStyles.boldLabel);

        // ── 文件夹路径行 ──
        string folderPath = EditorPrefs.GetString(PrefFolderPath, "");
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.TextField("文件夹路径", folderPath);
        if (GUILayout.Button("浏览", GUILayout.Width(48)))
        {
            string selected = EditorUtility.OpenFolderPanel("选择 Sprite 文件夹", folderPath, "");
            if (!string.IsNullOrEmpty(selected))
            {
                if (selected.StartsWith(Application.dataPath))
                    selected = "Assets" + selected.Substring(Application.dataPath.Length);
                EditorPrefs.SetString(PrefFolderPath, selected);
                folderPath = selected;
            }
        }
        EditorGUILayout.EndHorizontal();

        // ── 操作按钮行 ──
        EditorGUILayout.BeginHorizontal();
        GUI.enabled = !string.IsNullOrEmpty(folderPath);
        if (GUILayout.Button("自动填充 Sprites"))
        {
            AutoFillSprites(folderPath);
        }
        GUI.enabled = true;
        if (GUILayout.Button("配置后缀..."))
        {
            ImageButton_SuffixSettingsWindow.Open();
        }
        EditorGUILayout.EndHorizontal();

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
            Debug.LogWarning("[ImageButton_Visual] 找不到 targetImage 字段");
            return;
        }

        Button_Visual visual = (Button_Visual)target;
        Image found = ImageButtonEditorHelper.FindImageInChildCanvas(visual.gameObject);

        if (found != null)
        {
            prop.objectReferenceValue = found;
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
            Debug.Log($"[ImageButton_Visual] 已自动绑定 Image: {found.gameObject.name}");
        }
        else
        {
            Debug.LogWarning("[ImageButton_Visual] 未在子物体的 Canvas 下找到任何 Image 组件");
        }
    }

    private void AutoFillSprites(string folderPath)
    {
        string suffixIdle      = GetSuffix("Idle",      "_Idle");
        string suffixHighlight = GetSuffix("Highlight", "_Highlight");
        string suffixNotAllow  = GetSuffix("NotAllow",  "_NotAllow");
        string suffixPressed   = GetSuffix("Pressed",   "_Pressed");

        serializedObject.Update();
        SerializedProperty propNormal    = serializedObject.FindProperty("idleSprite");
        SerializedProperty propHighlight = serializedObject.FindProperty("highLightSprite");
        SerializedProperty propNotAllow  = serializedObject.FindProperty("NotAllowSprite");
        SerializedProperty propPressed   = serializedObject.FindProperty("pressedSprite");

        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { folderPath });
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            string nameNoExt = Path.GetFileNameWithoutExtension(assetPath);

            TryAssign(propNormal,    assetPath, nameNoExt, suffixIdle,      "idleSprite");
            TryAssign(propHighlight, assetPath, nameNoExt, suffixHighlight, "highLightSprite");
            TryAssign(propNotAllow,  assetPath, nameNoExt, suffixNotAllow,  "NotAllowSprite");
            TryAssign(propPressed,   assetPath, nameNoExt, suffixPressed,   "pressedSprite");
        }

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
        Debug.Log($"[ImageButton_Visual] 自动填充完成，文件夹：{folderPath}");
    }

    private static void TryAssign(SerializedProperty prop, string assetPath, string nameNoExt,
                                   string suffix, string fieldLabel)
    {
        if (!nameNoExt.EndsWith(suffix)) return;
        if (prop == null)
        {
            Debug.LogWarning($"[ImageButton_Visual] 找不到字段 {fieldLabel}，请确认字段名是否正确");
            return;
        }
        if (prop.objectReferenceValue != null)
        {
            Debug.LogWarning($"[ImageButton_Visual] {fieldLabel} 已有值，跳过 {assetPath}");
            return;
        }
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (sprite != null)
            prop.objectReferenceValue = sprite;
    }
}

public class ImageButton_SuffixSettingsWindow : EditorWindow
{
    private string _idle;
    private string _highlight;
    private string _notAllow;
    private string _pressed;

    public static void Open()
    {
        var win = GetWindow<ImageButton_SuffixSettingsWindow>(true, "配置 Image Sprite 后缀", true);
        win.minSize = new Vector2(280, 160);
        win.Load();
    }

    private void Load()
    {
        _idle      = EditorPrefs.GetString("ImageButtonVisual_Suffix_Idle",      "_Idle");
        _highlight = EditorPrefs.GetString("ImageButtonVisual_Suffix_Highlight", "_Highlight");
        _notAllow  = EditorPrefs.GetString("ImageButtonVisual_Suffix_NotAllow",  "_NotAllow");
        _pressed   = EditorPrefs.GetString("ImageButtonVisual_Suffix_Pressed",   "_Pressed");
    }

    private void OnGUI()
    {
        GUILayout.Space(8);
        _idle      = EditorGUILayout.TextField("Idle 后缀",      _idle);
        _highlight = EditorGUILayout.TextField("Highlight 后缀", _highlight);
        _notAllow  = EditorGUILayout.TextField("NotAllow 后缀",  _notAllow);
        _pressed   = EditorGUILayout.TextField("Pressed 后缀",   _pressed);

        GUILayout.Space(8);
        if (GUILayout.Button("保存", GUILayout.Height(28)))
        {
            EditorPrefs.SetString("ImageButtonVisual_Suffix_Idle",      _idle);
            EditorPrefs.SetString("ImageButtonVisual_Suffix_Highlight", _highlight);
            EditorPrefs.SetString("ImageButtonVisual_Suffix_NotAllow",  _notAllow);
            EditorPrefs.SetString("ImageButtonVisual_Suffix_Pressed",   _pressed);
            Debug.Log("[ImageButton_Visual] 后缀配置已保存。");
            Close();
        }
    }
}
