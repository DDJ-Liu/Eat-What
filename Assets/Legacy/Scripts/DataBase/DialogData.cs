using System.Collections.Generic;
using System.Text.RegularExpressions;

/// <summary>
/// 对话选项数据
/// 注意：每次更新表头后需同步更新此类的字段
/// </summary>
[System.Serializable]
public class DialogChoice
{
    public string choiceText;
    public int nextUid;
    public List<ParsedCommand> conditionFunc;
    public List<ParsedCommand> BeforeClickFunc;
    public List<ParsedCommand> AfterClickFunc;
}

[System.Serializable]
public class DialogData : Data
{
    public int uid;
    public List<string> character_SpriteName = new List<string>();
    public string character_Name;
    public string content;
    public enum SwitchMode { DirectNext, ByChoice }
    public SwitchMode switchMode;
    public int defaultNextUid;
    public List<DialogChoice> choices = new List<DialogChoice>();

    public DialogData()
    {
        dataType = DataType.Dialog;
    }

    /// <summary>
    /// 从 Data 字典解析对话数据
    /// </summary>
    public void ParseFromData(Data sourceData)
    {
        data = sourceData.data;

        if (int.TryParse(this["Uid"], out int parsedUid))
            uid = parsedUid;

        // 解析角色立绘列表，表格中用英语逗号分隔
        string spritesRaw = this["CharacterSprites"];
        character_SpriteName.Clear();
        if (!string.IsNullOrEmpty(spritesRaw))
        {
            foreach (var s in spritesRaw.Split(','))
            {
                string trimmed = s.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                    character_SpriteName.Add(trimmed);
            }
        }
        character_Name = this["CharacterName"];
        content = this["Content"];
        string mode = this["SwitchMode"];
        switch(mode)
        {
            case "DirectNext":
                switchMode = SwitchMode.DirectNext;
                break;

            case "ByChoice":
                switchMode = SwitchMode.ByChoice;
                break;
            default:
                switchMode = SwitchMode.DirectNext;
                break;
        }
        if (int.TryParse(this["DefaultNextUid"], out int parsedDefault))
            defaultNextUid = parsedDefault;

        // 解析选项：扫描表头自动检测 Choice 组数
        var choiceIndices = new SortedSet<int>();
        foreach (var key in data.Keys)
        {
            var match = Regex.Match(key, @"^Choice(\d+)$");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int idx))
                choiceIndices.Add(idx);
        }

        choices.Clear();
        foreach (int i in choiceIndices)
        {
            string choiceText = this[$"Choice{i}"];
            if (string.IsNullOrEmpty(choiceText)) continue;

            var choice = new DialogChoice();
            choice.choiceText = choiceText;

            if (int.TryParse(this[$"Choice{i}_NextUid"], out int nextUid))
                choice.nextUid = nextUid;

            //Parse all choice funcs here
            choice.conditionFunc = CommandStringParser.Parse(this[$"Choice{i}_FuncShowCondition"]);
            choice.BeforeClickFunc = CommandStringParser.Parse(this[$"Choice{i}_FuncOnShow"]);
            choice.AfterClickFunc = CommandStringParser.Parse(this[$"Choice{i}_FuncAfterClick"]);

            choices.Add(choice);
        }
    }
}
