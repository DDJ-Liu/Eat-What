using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.Events;
using UnityEditor.Events;

[CustomEditor(typeof(SpriteButton_Visual))]
public class SpriteButton_VisualEditor : Button_VisualEditor
{
    private const string PrefFolderPath = "SpriteButtonVisual_FolderPath";

    private static string GetSuffix(string key, string defaultVal)
        => EditorPrefs.GetString("SpriteButtonVisual_Suffix_" + key, defaultVal);

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
                // 转为相对路径（Assets/...），AssetDatabase 需要相对路径
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
            SpriteButton_SuffixSettingsWindow.Open();
        }
        EditorGUILayout.EndHorizontal();
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
        Debug.Log($"[SpriteButton_Visual] 自动填充完成，文件夹：{folderPath}");
    }

    private static void TryAssign(SerializedProperty prop, string assetPath, string nameNoExt,
                                   string suffix, string fieldLabel)
    {
        if (!nameNoExt.EndsWith(suffix)) return;
        if (prop == null)
        {
            Debug.LogWarning($"[SpriteButton_Visual] 找不到字段 {fieldLabel}，请确认字段名是否正确");
            return;
        }
        if (prop.objectReferenceValue != null)
        {
            Debug.LogWarning($"[SpriteButton_Visual] {fieldLabel} 已有值，跳过 {assetPath}");
            return;
        }
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (sprite != null)
            prop.objectReferenceValue = sprite;
    }
}

// ──────────────────────────────────────────────────────────────
// 后缀配置窗口
// ──────────────────────────────────────────────────────────────

public class SpriteButton_SuffixSettingsWindow : EditorWindow
{
    private string _idle;
    private string _highlight;
    private string _notAllow;
    private string _pressed;

    public static void Open()
    {
        var win = GetWindow<SpriteButton_SuffixSettingsWindow>(true, "配置 Sprite 后缀", true);
        win.minSize = new Vector2(280, 160);
        win.Load();
    }

    private void Load()
    {
        _idle      = EditorPrefs.GetString("SpriteButtonVisual_Suffix_Idle",      "_Idle");
        _highlight = EditorPrefs.GetString("SpriteButtonVisual_Suffix_Highlight", "_Highlight");
        _notAllow  = EditorPrefs.GetString("SpriteButtonVisual_Suffix_NotAllow",  "_NotAllow");
        _pressed   = EditorPrefs.GetString("SpriteButtonVisual_Suffix_Pressed",   "_Pressed");
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
            EditorPrefs.SetString("SpriteButtonVisual_Suffix_Idle",      _idle);
            EditorPrefs.SetString("SpriteButtonVisual_Suffix_Highlight", _highlight);
            EditorPrefs.SetString("SpriteButtonVisual_Suffix_NotAllow",  _notAllow);
            EditorPrefs.SetString("SpriteButtonVisual_Suffix_Pressed",   _pressed);
            Debug.Log("[SpriteButton_Visual] 后缀配置已保存。");
            Close();
        }
    }
}
