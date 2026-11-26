using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class DropDown_MouseInteract : MonoBehaviour
{
    [Header("引用")]
    [Tooltip("触发按钮，用户在 Inspector 手动将 Open() 绑定到此按钮的 selectEvent")]
    public Button_MouseInteract dropDownButton;
    public GameObject choiceParent;
    [Tooltip("手动模式交互层（manualStackLayer=true, clickNullEqualsCancel=true）")]
    public MouseInteractionLayer choiceLayer;
    public List<Button_MouseInteract> choices = new List<Button_MouseInteract>();

    [Header("显示")]
    public TMP_Text displayText;

    [Header("状态")]
    [SerializeField] private int currentChoiceIndex = -1;
    [SerializeField] private bool isOpen = false;

    [Header("事件")]
    public UnityEvent onOpen;
    public UnityEvent onClose;
    public UnityEvent<int> onChoiceChanged;

    public int CurrentChoiceIndex => currentChoiceIndex;
    public bool IsOpen => isOpen;

    private void Awake()
    {
        // 自动收集 choiceParent 下的 Button_MouseInteract 作为 choices
        if (choices.Count == 0 && choiceParent != null)
        {
            foreach (Transform child in choiceParent.transform)
            {
                var btn = child.GetComponent<Button_MouseInteract>();
                if (btn != null)
                {
                    choices.Add(btn);
                }
            }
        }

        /*for (int i = 0; i < choices.Count; i++)
        {
            int index = i;
            choices[i].delayedSelectEvent.AddListener(() => OnChoiceSelected(index));
        }*/

        choiceLayer.onClickNullCancel.AddListener(Close);
        choiceParent.SetActive(false);

        if (choices.Count > 0 && currentChoiceIndex < 0)
        {
            currentChoiceIndex = 0;
            UpdateDisplayText();
        }
    }

    public void Open()
    {
        if (isOpen) return;
        choiceParent.SetActive(true);
        choiceLayer.OnPushLayer();
        isOpen = true;
        onOpen?.Invoke();
    }

    public void Close()
    {
        if (!isOpen) return;
        choiceLayer.OnRemoveLayer();
        choiceParent.SetActive(false);
        isOpen = false;
        onClose?.Invoke();
    }

    public void OnChoiceSelected(int index)
    {
        currentChoiceIndex = index;
        UpdateDisplayText();
        onChoiceChanged?.Invoke(index);
        Close();
    }

    public int GetCurrentChoice()
    {
        return currentChoiceIndex;
    }

    /// <summary>
    /// 程序化设置选中项，触发 onChoiceChanged，不打开面板
    /// </summary>
    public void SetCurrentChoice(int index)
    {
        if (index < 0 || index >= choices.Count)
        {
            Debug.LogWarning($"[DropDown] SetCurrentChoice: index {index} 超出范围 [0, {choices.Count - 1}]");
            return;
        }
        currentChoiceIndex = index;
        UpdateDisplayText();
        onChoiceChanged?.Invoke(index);
    }

    private void UpdateDisplayText()
    {
        if (displayText != null && currentChoiceIndex >= 0 && currentChoiceIndex < choices.Count)
        {
            displayText.text = choices[currentChoiceIndex].gameObject.name;
        }
    }

    private void OnDisable()
    {
        if (isOpen)
        {
            Close();
        }
    }
}
