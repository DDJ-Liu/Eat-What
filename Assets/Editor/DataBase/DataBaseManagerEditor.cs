using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(DataBaseManager))]
public class DataBaseManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10);
        EditorGUILayout.LabelField("── Debug ──", EditorStyles.boldLabel);

        if (GUILayout.Button("加载首个 .dat 到 loadedData", GUILayout.Height(30)))
        {
            var manager = (DataBaseManager)target;
            string dir = Application.streamingAssetsPath;

            if (!Directory.Exists(dir))
            {
                Debug.LogWarning("[DataBaseManager] StreamingAssets 目录不存在");
                return;
            }

            string firstDat = Directory.GetFiles(dir, "*.dat", SearchOption.TopDirectoryOnly)
                .OrderBy(f => f).FirstOrDefault();

            if (firstDat == null)
            {
                Debug.LogWarning("[DataBaseManager] StreamingAssets 中没有 .dat 文件");
                return;
            }

            string fileName = Path.GetFileName(firstDat);
            var data = manager.LoadData(fileName);

            // 通过反射写入 loadedData（仅 Debug 用途）
            var field = typeof(DataBaseManager).GetField("loadedData",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(manager, data);
                Debug.Log($"[DataBaseManager] 已加载 {fileName}，共 {data.Count} 条");
            }

            EditorUtility.SetDirty(manager);
        }

        if (GUILayout.Button("加载首个 .dat 到 loadedSheetData", GUILayout.Height(30)))
        {
            var manager = (DataBaseManager)target;
            string dir = Application.streamingAssetsPath;

            if (!Directory.Exists(dir))
            {
                Debug.LogWarning("[DataBaseManager] StreamingAssets 目录不存在");
                return;
            }

            string firstDat = Directory.GetFiles(dir, "*.dat", SearchOption.TopDirectoryOnly)
                .OrderBy(f => f).FirstOrDefault();

            if (firstDat == null)
            {
                Debug.LogWarning("[DataBaseManager] StreamingAssets 中没有 .dat 文件");
                return;
            }

            string fileName = Path.GetFileName(firstDat);
            var sheetData = manager.LoadDataAsSpreadSheet(fileName);

            var field = typeof(DataBaseManager).GetField("loadedSheetData",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(manager, sheetData);
                Debug.Log($"[DataBaseManager] 已加载 {fileName}，共 {sheetData.sheets.Count} 个 sheet");
            }

            EditorUtility.SetDirty(manager);
        }
    }
}
