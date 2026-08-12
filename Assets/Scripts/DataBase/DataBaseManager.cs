using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class DataBaseManager : MonoBehaviour
{
    public static DataBaseManager Instance;

    [SerializeField] private List<Data> loadedData = new List<Data>();
    [SerializeField] private SpreadSheetData<Data> loadedSheetData = new SpreadSheetData<Data>();

    [Header("FileNames")]
    public string DialogDataFile;

    [Header("DataBases")]
    public DialogDataBase dialogDataBase;

    private void Awake()
    {
        // 确保唯一性
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        loadedData.Clear();
        Initialize();
    }

    /// <summary>
    /// 初始化数据库
    /// </summary>
    public void Initialize()
    {
        dialogDataBase = new DialogDataBase(this, DialogDataFile);
        //Debug.Log(dialogDataBase.GetDialog("DialogName", 2).content);
    }

    /// <summary>
    /// 确保文件名带有.dat扩展名
    /// </summary>
    private string EnsureDatExtension(string fileName)
    {
        if (!fileName.EndsWith(".dat", System.StringComparison.OrdinalIgnoreCase))
            return fileName + ".dat";
        return fileName;
    }

    /// <summary>
    /// 解密并返回.dat文件数据
    /// </summary>
    [Obsolete]public List<Data> LoadData(string fileName)
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, EnsureDatExtension(fileName));
        var spreadSheet = DataEncryptor.DecryptSpreadSheetFromFile(filePath);

        // 返回第一个 sheet 的数据（向后兼容）
        foreach (var sheet in spreadSheet.sheets)
        {
            return sheet.Value;
        }

        return new List<Data>();
    }

    /// <summary>
    /// 解密并返回.dat文件的完整 SpreadSheetData
    /// </summary>
    public SpreadSheetData<Data> LoadDataAsSpreadSheet(string fileName)
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, EnsureDatExtension(fileName));
        return DataEncryptor.DecryptSpreadSheetFromFile(filePath);
    }

}
