using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using OfficeOpenXml;

/// <summary>
/// Excel加密导出工具面板
/// 从外部文件夹读取.xlsx（第一个sheet） → 加密为.dat → 导出到StreamingAssets
/// </summary>
public class DataEncryptorWindow : EditorWindow
{
    private const string PrefFolderPath = "DataEncryptor_ExcelFolderPath";

    [MenuItem("Tools/Data Encryptor")]
    public static void Open()
    {
        var window = GetWindow<DataEncryptorWindow>(false, "Data Encryptor", true);
        window.minSize = new Vector2(400, 150);
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        EditorGUILayout.LabelField("Excel → 加密 .dat 导出工具", EditorStyles.boldLabel);
        GUILayout.Space(8);

        // 文件夹路径
        string folderPath = EditorPrefs.GetString(PrefFolderPath, "");
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.TextField("Excel 文件夹", folderPath);
        if (GUILayout.Button("浏览", GUILayout.Width(48)))
        {
            string selected = EditorUtility.OpenFolderPanel("选择 Excel 文件夹", folderPath, "");
            if (!string.IsNullOrEmpty(selected))
            {
                EditorPrefs.SetString(PrefFolderPath, selected);
                folderPath = selected;
            }
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(12);

        // 导出按钮
        GUI.enabled = !string.IsNullOrEmpty(folderPath) && Directory.Exists(folderPath);
        if (GUILayout.Button("加密并导出到 StreamingAssets", GUILayout.Height(32)))
        {
            EncryptAllExcelFiles(folderPath);
        }
        GUI.enabled = true;
    }

    private static void EncryptAllExcelFiles(string folderPath)
    {
        string[] xlsxFiles = Directory.GetFiles(folderPath, "*.xlsx", SearchOption.TopDirectoryOnly);

        if (xlsxFiles.Length == 0)
        {
            Debug.LogWarning("[DataEncryptor] 文件夹中没有 .xlsx 文件");
            return;
        }

        // 确保 StreamingAssets 目录存在
        string outputDir = Application.streamingAssetsPath;
        if (!Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir);

        int successCount = 0;

        foreach (string xlsxPath in xlsxFiles)
        {
            string fileName = Path.GetFileNameWithoutExtension(xlsxPath);

            // 跳过Excel临时文件（以~$开头）
            if (fileName.StartsWith("~$")) continue;

            try
            {
                SpreadSheetData<Data> spreadSheet = ReadAllSheets(xlsxPath);

                if (spreadSheet.sheets.Count == 0)
                {
                    Debug.LogWarning($"[DataEncryptor] 跳过空文件: {fileName}.xlsx");
                    continue;
                }

                // 加密并写入 .dat
                string outputPath = Path.Combine(outputDir, fileName + ".dat");
                DataEncryptor.EncryptSpreadSheetToFile(spreadSheet, outputPath);

                successCount++;
                Debug.Log($"[DataEncryptor] 导出成功: {fileName}.xlsx → {fileName}.dat ({spreadSheet.sheets.Count} sheets)");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[DataEncryptor] 导出失败 {fileName}.xlsx: {e.Message}");
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"[DataEncryptor] 完成！成功导出 {successCount}/{xlsxFiles.Length} 个文件到 StreamingAssets");
    }

    /// <summary>
    /// 读取xlsx文件的所有sheet，第一行为表头，后续行为数据
    /// </summary>
    private static SpreadSheetData<Data> ReadAllSheets(string xlsxPath)
    {
        var spreadSheet = new SpreadSheetData<Data>();

        if (!File.Exists(xlsxPath))
        {
            Debug.LogError($"Excel文件未找到: {xlsxPath}");
            return spreadSheet;
        }

        using (var package = new ExcelPackage(new FileInfo(xlsxPath)))
        {
            if (package.Workbook.Worksheets.Count == 0)
            {
                Debug.LogWarning($"Excel文件无工作表: {xlsxPath}");
                return spreadSheet;
            }

            bool isFirst = true;
            foreach (var worksheet in package.Workbook.Worksheets)
            {
                var dataList = ReadSheet(worksheet);
                if (dataList.Count > 0)
                {
                    if (isFirst)
                    {
                        isFirst = false;
                    }
                    spreadSheet.sheets[worksheet.Name] = dataList;
                }
            }
        }

        return spreadSheet;
    }

    /// <summary>
    /// 读取单个 sheet 的数据
    /// </summary>
    private static List<Data> ReadSheet(ExcelWorksheet worksheet)
    {
        var dataList = new List<Data>();

        int rowCount = worksheet.Dimension?.Rows ?? 0;
        int colCount = worksheet.Dimension?.Columns ?? 0;

        if (rowCount < 2 || colCount == 0)
            return dataList;

        // 第1行 = 表头
        var headers = new List<string>();
        for (int col = 1; col <= colCount; col++)
        {
            string header = worksheet.Cells[1, col].Text?.Trim() ?? "";
            headers.Add(header);
        }

        // 第2行起 = 数据行
        for (int row = 2; row <= rowCount; row++)
        {
            bool hasValue = false;
            var data = new Data();
            data.data = new SerializableDictionary<string, string>();

            for (int col = 1; col <= colCount; col++)
            {
                string value = worksheet.Cells[row, col].Text ?? "";
                if (!string.IsNullOrEmpty(value)) hasValue = true;

                // 跳过空表头列
                if (!string.IsNullOrEmpty(headers[col - 1]))
                {
                    data.data[headers[col - 1]] = value;
                }
            }

            // 跳过全空行
            if (hasValue)
            {
                dataList.Add(data);
            }
        }

        return dataList;
    }
}
