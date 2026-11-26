using System;
using System.Collections.Generic;

/// <summary>
/// 对话数据库
/// </summary>
[System.Serializable]
public class DialogDataBase
{
    private DataBaseManager manager;
    public SpreadSheetData<DialogData> DialogData = new SpreadSheetData<DialogData>();

    public DialogDataBase(DataBaseManager manager, string fileName)
    {
        this.manager = manager;
        LoadDialogData(fileName);
    }

    private void LoadDialogData(string fileName)
    {
        DialogData.Clear();
        SpreadSheetData<Data> rawData = manager.LoadDataAsSpreadSheet(fileName);
        
        //Parse RawData into DialogData
        foreach (var sheet in rawData.sheets)
        {
            var dialogList = new List<DialogData>();
            foreach (var data in sheet.Value)
            {
                var dialogData = new DialogData();
                dialogData.ParseFromData(data);
                dialogList.Add(dialogData);
            }
            DialogData[sheet.Key] = dialogList;
        }
    }

    /// <summary>
    /// 获取一条DialogData || 
    /// CharacterName = 表格中sheet的名字 || 
    /// uid = 对话的uid || 
    /// </summary>
    /// <param name="CharacterName"></param>
    /// <param name="uid"></param>
    /// <returns></returns>
    /// <summary>
    /// 获取指定 sheet 的第一条 DialogData
    /// </summary>
    public DialogData GetFirstDialog(string sheetName)
    {
        var list = DialogData[sheetName];
        if (list == null || list.Count == 0) return null;
        return list[0];
    }

    public DialogData GetDialog(string CharacterName, int uid)
    {
        var list = DialogData[CharacterName];
        if (list == null) return null;
        return list.Find(d => d.uid == uid);
    }
}
