using Febucci.UI.Core;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DogManager : MonoBehaviour
{
    #region Singleton
    public static DogManager Instance;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    #endregion

    public GameObject Dog;

    [Header("警告文本显示")]
    public GameObject WarningTextParent;
    public SpriteRenderer WarningBubble;
    public TypewriterCore WarningText;
    public float warningTextDisappearTime = 1f;

    [Header("警告文本过渡动画")]
    [Tooltip("用于警告文本显示/隐藏的过渡动画控制器（可选，无则降级为直接显示）")]
    public TransitionController warningTextTransitionController;

    [Tooltip("警告文本淡入动画名称")]
    public string warningFadeInTransitionName = "FadeIn";

    [Tooltip("警告文本淡出动画名称")]
    public string warningFadeOutTransitionName = "FadeOut";

    public CommonMaterialPopupManager commonIngredientManager;

    [Header("Editor 测试")]
    [TextArea(3, 5)]
    [Tooltip("用于在 Inspector 中测试显示的样例文本")]
    public string testWarningText = "这是一条测试警告文本";

    // 内部字段
    private Coroutine warningTextCoroutine; // 自动消失协程引用
    private string currentWarningMessage; // 当前要显示的警告文本

    private void Start()
    {
        commonIngredientManager.gameObject.SetActive(false);
        WarningBubble.color = new Color(WarningBubble.color.r,WarningBubble.color.g,WarningBubble.color.b,0);
        // 初始化时清空警告文本
        ClearWarningText();
    }

    #region Warning
    /// <summary>
    /// 清空警告文本内容
    /// </summary>
    private void ClearWarningText()
    {
        if (WarningText != null)
        {
            WarningText.ShowText("");
        }
    }

    /// <summary>
    /// 显示样例警告文本（用于 Editor 测试）
    /// </summary>
    public void ShowTestWarningText()
    {
        if (string.IsNullOrEmpty(testWarningText))
        {
            Debug.LogWarning("[DogManager] Test warning text is empty!");
            return;
        }

        // 直接显示测试文本
        ShowWarningTextDirect(testWarningText);
    }

    /// <summary>
    /// 直接显示指定的警告文本（不经过本地化解析）
    /// </summary>
    /// <param name="text">要显示的文本内容</param>
    public void ShowWarningTextDirect(string text)
    {
        // 1. 安全检查
        if (WarningTextParent == null)
        {
            Debug.LogWarning("[DogManager] WarningTextParent is not set!");
            return;
        }

        // 2. 如果正在显示其他警告，先停止当前的自动消失计时
        if (warningTextCoroutine != null)
        {
            StopCoroutine(warningTextCoroutine);
            warningTextCoroutine = null;
        }

        // 3. 清空文本（在显示新内容之前）
        ClearWarningText();

        // 4. 保存警告文本内容（在气泡淡入完成后显示）
        currentWarningMessage = text;

        // 5. 显示气泡（集成 Transition）
        if (warningTextTransitionController != null)
        {
            WarningTextParent.SetActive(true);
            warningTextTransitionController.PlayTransition(
                warningFadeInTransitionName,
                OnWarningBubbleFadeInComplete
            );
        }
        else
        {
            WarningTextParent.SetActive(true);
            OnWarningBubbleFadeInComplete();
        }
    }

    /// <summary>
    /// Method to resolve Warning Text, temp Method.
    /// TODO: Replace placeholder Text by Localization Text return.
    /// </summary>
    /// <param name="sheetName">本地化表名</param>
    /// <param name="key">本地化键</param>
    /// <returns>警告文本内容</returns>
    public string ResolveWarningText(string sheetName, string key)
    {
        string result = $"Warning Text: {sheetName}, {key}";
        return result;
    }

    /// <summary>
    /// 显示警告文本，并在指定时间后自动隐藏
    /// </summary>
    /// <param name="sheetName">本地化表名</param>
    /// <param name="key">本地化键</param>
    public void ShowWarningText(string sheetName, string key)
    {
        // 1. 安全检查
        if (WarningTextParent == null)
        {
            Debug.LogWarning("[DogManager] WarningTextParent is not set!");
            return;
        }

        // 2. 如果正在显示其他警告，先停止当前的自动消失计时
        if (warningTextCoroutine != null)
        {
            StopCoroutine(warningTextCoroutine);
            warningTextCoroutine = null;
        }

        // 3. 清空文本（在显示新内容之前）
        ClearWarningText();

        // 4. 保存警告文本内容（在气泡淡入完成后显示）
        currentWarningMessage = ResolveWarningText(sheetName, key);

        // 5. 显示气泡（集成 Transition）
        // 顺序：先气泡淡入 → 气泡淡入完成后文字 TypeWriter 效果
        if (warningTextTransitionController != null)
        {
            // 先激活 GameObject，然后播放气泡淡入动画
            WarningTextParent.SetActive(true);
            warningTextTransitionController.PlayTransition(
                warningFadeInTransitionName,
                OnWarningBubbleFadeInComplete
            );
        }
        else
        {
            // 降级方案：无 TransitionController 时直接显示
            WarningTextParent.SetActive(true);
            OnWarningBubbleFadeInComplete();
        }
    }

    /// <summary>
    /// 立即隐藏警告文本
    /// 可被外部调用或用于中断自动消失流程
    /// </summary>
    public void HideWarningText()
    {
        // 1. 停止自动消失协程（如果正在运行）
        if (warningTextCoroutine != null)
        {
            StopCoroutine(warningTextCoroutine);
            warningTextCoroutine = null;
        }

        // 2. 安全检查
        if (WarningTextParent == null) return;

        // 3. 隐藏警告文本（先文字消失，再气泡淡出）
        // 顺序：先文字 TypeWriter 消失 → 等待消失完成 → 气泡淡出
        if (WarningText != null)
        {
            // 订阅文字消失完成事件
            WarningText.onTextDisappeared.AddListener(OnTextDisappeared);
            // 启动文字消失动画
            WarningText.StartDisappearingText();
        }
        else
        {
            // 如果没有 TypeWriter，直接播放气泡淡出
            StartBubbleFadeOut();
        }
    }

    /// <summary>
    /// 文字消失完成后的回调
    /// </summary>
    private void OnTextDisappeared()
    {
        // 取消订阅事件，避免重复触发
        if (WarningText != null)
        {
            WarningText.onTextDisappeared.RemoveListener(OnTextDisappeared);
        }

        // 文字消失完成后，开始气泡淡出
        StartBubbleFadeOut();
    }

    /// <summary>
    /// 开始气泡淡出动画
    /// </summary>
    private void StartBubbleFadeOut()
    {
        if (warningTextTransitionController != null)
        {
            // 播放气泡淡出动画，完成后在回调中真正隐藏 GameObject
            warningTextTransitionController.PlayTransition(
                warningFadeOutTransitionName,
                OnWarningBubbleFadeOutComplete
            );
        }
        else
        {
            // 降级方案：无 TransitionController 时直接隐藏
            OnWarningBubbleFadeOutComplete();
        }
    }

    /// <summary>
    /// 自动隐藏警告文本的协程
    /// 等待 warningTextDisappearTime 秒后调用 HideWarningText
    /// </summary>
    private IEnumerator AutoHideWarningTextCoroutine()
    {
        // 等待指定的显示时间
        yield return new WaitForSeconds(warningTextDisappearTime);

        // 时间到，隐藏警告文本
        HideWarningText();

        // 标记协程已结束（遵循项目协程管理模式）
        warningTextCoroutine = null;
    }

    /// <summary>
    /// 气泡淡入动画完成后的回调
    /// 气泡淡入完成后，开始显示文字的 TypeWriter 效果
    /// </summary>
    private void OnWarningBubbleFadeInComplete()
    {
        // 气泡淡入完成后，显示文字（TypeWriter 效果）
        if (WarningText != null)
        {
            WarningText.ShowText(currentWarningMessage);
        }

        // 启动自动消失协程
        warningTextCoroutine = StartCoroutine(AutoHideWarningTextCoroutine());
    }

    /// <summary>
    /// 气泡淡出动画完成后的回调
    /// </summary>
    private void OnWarningBubbleFadeOutComplete()
    {
        // 气泡淡出动画完成后，真正隐藏 GameObject
        if (WarningTextParent != null)
        {
            WarningTextParent.SetActive(false);
        }
    }
    #endregion

    #region CommonIngredient
    /// <summary>
    /// 打开通用素材 Popup Manager。
    /// PopupManager 的内容设置由调用方自行处理。
    /// </summary>
    public void OpenCommonMaterialPopup(IList<IngredientData> needed)
    {
        if (commonIngredientManager != null)
        {
            commonIngredientManager.gameObject.SetActive(true);
            commonIngredientManager.ShowFor(needed);
        }
    }

    /// <summary>
    /// 关闭通用素材 Popup Manager。
    /// </summary>
    public void CloseCommonMaterialPopup()
    {
        commonIngredientManager.Hide();
        if (commonIngredientManager != null)
        {
            commonIngredientManager.gameObject.SetActive(false);
        }
    }
    #endregion


    /// <summary>
    /// 组件禁用时清理协程和状态
    /// </summary>
    private void OnDisable()
    {
        // 停止自动消失协程
        if (warningTextCoroutine != null)
        {
            StopCoroutine(warningTextCoroutine);
            warningTextCoroutine = null;
        }

        // 确保警告文本隐藏
        if (WarningTextParent != null)
        {
            WarningTextParent.SetActive(false);
        }
    }
}
