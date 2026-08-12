using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using UnityEngine.UIElements;

/// <summary>
/// 对话系统 - 通过 Uid 管理对话流程，控制 UI 显示和选项交互
/// </summary>
public class DialogSystem : MonoBehaviour
{
    #region 核心状态
    [SerializeField] private int currentUid;
    private int defaultNextUid;
    private List<int> choiceNextUids = new List<int>();
    private DialogData cachedData;
    #endregion

    #region UI 引用
    [Header("UI")]
    [SerializeField] private TMP_Text mainTextBody;
    [SerializeField] private TMP_Text currentSpeakingCharacterName;
    [Header("Sprite")]
    [SerializeField] private SpriteRenderer characterSprite;
    [SerializeField] private Transform characterSpriteParent;
    [SerializeField] private List<SpriteRenderer> characterSprites = new List<SpriteRenderer>();


    [Header("Choice")]
    [SerializeField] private List<GameObject> choicesButtons;
    #endregion

    #region DialogDataBase 引用
    [SerializeField] private string dialogSheetName;
    private DialogDataBase dialogDataBase;
    #endregion

    private void Awake()
    {
        if(characterSpriteParent != null)
        {
            foreach(Transform child in  characterSpriteParent)
            {
                if(child != null && child.GetComponent<SpriteRenderer>() != null)
                {
                    characterSprites.Add(child.GetComponent<SpriteRenderer>());
                }
            }
        }
    }

    #region 可选功能
    [SerializeField] private bool enableStateMachine = false;
    [SerializeField] private bool enableHistory = false;

    public enum DialogState { Idle, Playing, WaitingChoice, Finished }
    private DialogState currentState = DialogState.Idle;
    private List<int> dialogHistory = new List<int>();
    #endregion

    #region 事件
    public UnityEvent onDialogStart;
    public UnityEvent onDialogEnd;
    public UnityEvent<int> onChoiceSelected;
    public UnityEvent<string> onSpeakerChanged;
    #endregion

    #region 公共接口
    /// <summary>
    /// 初始化对话系统 - 加载指定 sheet 的第一条对话
    /// </summary>
    public void Initialize(string dialogName)
    {
        dialogSheetName = dialogName;
        dialogDataBase = DataBaseManager.Instance.dialogDataBase;

        if (dialogDataBase == null)
        {
            Debug.LogError("DialogDataBase not initialized");
            return;
        }

        var firstDialog = dialogDataBase.GetFirstDialog(dialogName);
        if (firstDialog == null)
        {
            Debug.LogError($"No dialog found in sheet: {dialogName}");
            return;
        }

        onDialogStart?.Invoke();
        ShowNextDialog(firstDialog.uid);
    }

    /// <summary>
    /// 初始化对话系统 - 加载指定 sheet 的第一条对话
    /// </summary>
    public void Initialize()
    {
        gameObject.SetActive(true);
        dialogDataBase = DataBaseManager.Instance.dialogDataBase;

        if (dialogDataBase == null)
        {
            Debug.LogError("DialogDataBase not initialized");
            return;
        }

        var firstDialog = dialogDataBase.GetFirstDialog(dialogSheetName);
        if (firstDialog == null)
        {
            Debug.LogError($"No dialog found in sheet: {dialogSheetName}");
            return;
        }

        onDialogStart?.Invoke();
        ShowNextDialog(firstDialog.uid);
    }

    /// <summary>
    /// 跳转到指定 Uid 的对话
    /// </summary>
    public void ShowNextDialog(int uid)
    {
        if (IsEndUid(uid))
        {
            onDialogEnd?.Invoke();
            if (enableStateMachine)
                currentState = DialogState.Finished;
            return;
        }

        if (!IsValidUid(uid))
        {
            Debug.LogError($"Invalid dialog uid: {uid} in sheet: {dialogSheetName}");
            return;
        }

        if (enableStateMachine && currentState == DialogState.WaitingChoice)
        {
            Debug.LogWarning("Cannot proceed while waiting for choice");
            return;
        }

        if (enableHistory && currentUid > 0)
            dialogHistory.Add(currentUid);

        currentUid = uid;
        RefreshDialog();

        if (enableStateMachine)
        {
            currentState = cachedData.switchMode == DialogData.SwitchMode.ByChoice
                ? DialogState.WaitingChoice
                : DialogState.Playing;
        }
    }

    /// <summary>
    /// DirectNext 模式下推进到下一句对话
    /// </summary>
    public void Next()
    {
        if (cachedData == null)
        {
            Debug.LogError("当前没有可用的对话数据");
            return;
        }

        if (cachedData.switchMode == DialogData.SwitchMode.ByChoice)
        {
            Debug.LogWarning("当前对话为 ByChoice 模式，请使用 SelectChoice");
            return;
        }

        ShowNextDialog(defaultNextUid);
    }

    /// <summary>
    /// 选择第 choiceIndex 个选项，执行其效果并跳转到对应 uid
    /// </summary>
    public void SelectChoice(int choiceIndex)
    {
        if (cachedData == null || cachedData.choices == null)
        {
            Debug.LogError("当前没有可用的对话数据");
            return;
        }

        if (choiceIndex < 0 || choiceIndex >= cachedData.choices.Count)
        {
            Debug.LogError($"选项索引越界: {choiceIndex}, 当前选项数: {cachedData.choices.Count}");
            return;
        }

        OnChoiceClicked(cachedData.choices[choiceIndex], choiceIndex);
    }

    /// <summary>
    /// 刷新当前对话的 UI 显示
    /// </summary>
    public void RefreshDialog()
    {
        var newData = GetDialogData(currentUid);
        if (newData == null)
        {
            Debug.LogError($"Dialog data not found for uid: {currentUid}");
            return;
        }

        if (cachedData == null || cachedData.content != newData.content)
            UpdateMainText(newData.content);

        if (cachedData == null || !ListEquals(cachedData.character_SpriteName, newData.character_SpriteName))
            UpdateCharacterSprite(newData.character_SpriteName);

        if (cachedData == null || cachedData.character_Name != newData.character_Name)
        {
            UpdateSpeakerName(newData.character_Name);
            onSpeakerChanged?.Invoke(newData.character_Name);
        }

        UpdateChoices(newData.choices);

        cachedData = newData;
        defaultNextUid = newData.defaultNextUid;
        choiceNextUids.Clear();
        foreach (var choice in newData.choices)
            choiceNextUids.Add(choice.nextUid);
    }
    #endregion

    #region UI 更新
    private void UpdateMainText(string content)
    {
        if (mainTextBody != null)
            mainTextBody.text = content;
    }

    private void UpdateSpeakerName(string character)
    {
        if (currentSpeakingCharacterName != null)
            currentSpeakingCharacterName.text = character;
    }

    private void UpdateCharacterSprite(List<string> characterNames)
    {
        // 更新每个立绘位：按索引匹配 characterSprites 列表
        for (int i = 0; i < characterSprites.Count; i++)
        {
            if (i < characterNames.Count && !string.IsNullOrEmpty(characterNames[i]))
            {
                var sprite = Resources.Load<Sprite>($"DialogSystem/{dialogSheetName}/{characterNames[i]}");
                if (sprite != null)
                {
                    characterSprites[i].sprite = sprite;
                    characterSprites[i].gameObject.SetActive(true);
                }
                else
                {
                    characterSprites[i].gameObject.SetActive(false);
                }
            }
            else
            {
                characterSprites[i].gameObject.SetActive(false);
            }
        }
    }

    private static bool ListEquals(List<string> a, List<string> b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;
        if (a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++)
            if (a[i] != b[i]) return false;
        return true;
    }

    private void UpdateChoices(List<DialogChoice> choices)
    {
        foreach (var btn in choicesButtons)
            btn.SetActive(false);

        for (int i = 0; i < choices.Count && i < choicesButtons.Count; i++)
        {
            var choice = choices[i];
            var button = choicesButtons[i];

            bool canShow = EvaluateParsedCommands(choice.conditionFunc);
            button.SetActive(canShow);

            if (canShow)
            {
                ExecuteParsedCommands(choice.BeforeClickFunc);

                var textComponent = button.GetComponentInChildren<TMP_Text>();
                if (textComponent != null)
                    textComponent.text = choice.choiceText;

                var mouseInteract = button.GetComponent<Button_MouseInteract>();
                if (mouseInteract != null)
                {
                    mouseInteract.delayedSelectEvent.RemoveAllListeners();
                    int choiceIndex = i;
                    mouseInteract.delayedSelectEvent.AddListener(() => OnChoiceClicked(choice, choiceIndex));
                }
            }
        }
    }
    #endregion

    #region Command 执行
    private void ExecuteParsedCommands(List<ParsedCommand> commands)
    {
        if (commands == null || commands.Count == 0) return;

        foreach (var cmd in commands)
        {
            try
            {
                var ctx = CommandStringParser.BuildContext(cmd);
                CommandManager.Instance.Execute(cmd.commandKey, ctx);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Command execution failed: {cmd.commandKey}, Error: {e.Message}");
            }
        }
    }

    private bool EvaluateParsedCommands(List<ParsedCommand> commands)
    {
        if (commands == null || commands.Count == 0) return true;

        foreach (var cmd in commands)
        {
            try
            {
                var ctx = CommandStringParser.BuildContext(cmd);
                CommandManager.Instance.Execute(cmd.commandKey, ctx);

                if (!ctx.HasResult || !ctx.GetResult<bool>(false))
                    return false;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Condition evaluation failed: {cmd.commandKey}, Error: {e.Message}");
                return false;
            }
        }
        return true;
    }

    private void OnChoiceClicked(DialogChoice choice, int index)
    {
        ExecuteParsedCommands(choice.AfterClickFunc);
        onChoiceSelected?.Invoke(index);
        ShowNextDialog(choice.nextUid);
    }
    #endregion

    #region 数据验证
    private bool IsValidUid(int uid)
    {
        return GetDialogData(uid) != null;
    }

    private bool IsEndUid(int uid)
    {
        return uid < 0;
    }

    private DialogData GetDialogData(int uid)
    {
        if (dialogDataBase == null) return null;
        return dialogDataBase.GetDialog(dialogSheetName, uid);
    }
    #endregion

    #region 按钮条件方法

    /// <summary>
    /// 当前对话为 DirectNext 模式时允许使用，否则不允许
    /// </summary>
    public void ConditionIsDirectNext(ConditionReceiver receiver)
    {
        receiver.ReportCondition(cachedData != null && cachedData.switchMode == DialogData.SwitchMode.DirectNext);
    }

    #endregion

    #region 对话用效果方法

    [Command("DebugLog", Group = "对话")]
    public static void DebugLog(CommandContext ctx)
    {
        string message = ctx.GetArg<string>("arg0", "");
        Debug.Log($"[Dialog] {message}");
    }

    #endregion

    #region conditionFunc 方法

    [Command("ChoiceDebugOpen", Group = "对话")]
    public static void ChoiceDebugOpen(CommandContext ctx)
    {
        bool active = ctx.GetArg<bool>("arg0", true);
        ctx.SetResult(active);
    }

    #endregion
}
