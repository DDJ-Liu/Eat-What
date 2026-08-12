using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitchManager : MonoBehaviour
{
    public static SceneSwitchManager Instance;
    public bool forTesting;

    [Header("Transition Settings")]
    [Tooltip("每个场景的 TransitionController 引用（场景加载后自动查找）")]
    public TransitionController transitionController;

    [Tooltip("场景切换时显示的 Canvas（切换开始时打开，结束后关闭）")]
    public Canvas transitionCanvas;

    [Header("Transition Names")]
    [Tooltip("fadeOut 转换名称")]
    public string fadeOutTransitionName = "fadeOut";

    [Tooltip("fadeIn 转换名称")]
    public string fadeInTransitionName = "fadeIn";

    [Tooltip("fadeFull 转换名称")]
    public string fadeFullTransitionName = "fadeFull";

    [Header("Full Fade Manual Timing")]
    [Tooltip("使用 fadeFull 动画时，从开始到场景切换的时间（秒）。仅用于 SwitchSceneWithFullFadeManualTiming 方法")]
    public float fadeFullHalfDuration = 0.5f;

    private bool isSwitching = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (!forTesting) DontDestroyOnLoad(gameObject);

            // 初始化时关闭 Canvas
            if (transitionCanvas != null)
            {
                transitionCanvas.gameObject.SetActive(false);
            }
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        FindTransitionController();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindTransitionController();
    }

    private void FindTransitionController()
    {
        transitionController = FindObjectOfType<TransitionController>();
        if (transitionController == null)
        {
            Debug.LogWarning($"[SceneSwitchManager] 当前场景未找到 TransitionController");
        }
    }

    /// <summary>
    /// 使用 fadeOut 和 fadeIn 两种 transition 完成场景切换
    /// </summary>
    /// <param name="sceneName">目标场景名称</param>
    /// <param name="onComplete">切换完成后的回调（可选）</param>
    public void SwitchSceneWithFade(string sceneName, Action onComplete = null)
    {
        if (isSwitching)
        {
            Debug.LogWarning("[SceneSwitchManager] 场景切换进行中，请勿重复调用");
            return;
        }

        if (transitionController == null)
        {
            Debug.LogError("[SceneSwitchManager] TransitionController 未找到，无法执行场景切换");
            LoadSceneDirectly(sceneName, onComplete);
            return;
        }

        StartCoroutine(SwitchSceneWithFadeCoroutine(sceneName, onComplete));
    }

    private IEnumerator SwitchSceneWithFadeCoroutine(string sceneName, Action onComplete)
    {
        isSwitching = true;

        // 打开 Canvas
        if (transitionCanvas != null)
        {
            transitionCanvas.gameObject.SetActive(true);
        }

        // 阶段 1: fadeOut
        bool fadeOutComplete = false;
        transitionController.PlayTransition(fadeOutTransitionName, () =>
        {
            fadeOutComplete = true;
        });

        yield return new WaitUntil(() => fadeOutComplete);

        // 阶段 2: 异步加载场景
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // 等待额外帧，确保 OnSceneLoaded 事件触发并查找 TransitionController
        yield return null;
        yield return null;

        // 阶段 3: fadeIn
        if (transitionController != null)
        {
            bool fadeInComplete = false;
            transitionController.PlayTransition(fadeInTransitionName, () =>
            {
                fadeInComplete = true;
            });

            yield return new WaitUntil(() => fadeInComplete);
        }

        // 关闭 Canvas
        if (transitionCanvas != null)
        {
            transitionCanvas.gameObject.SetActive(false);
        }

        isSwitching = false;
        onComplete?.Invoke();
    }

    /// <summary>
    /// 使用 fadeFull 一种 transition 完成场景切换（在动画中间时机切换场景）
    /// </summary>
    /// <param name="sceneName">目标场景名称</param>
    /// <param name="onComplete">切换完成后的回调（可选）</param>
    public void SwitchSceneWithFullFade(string sceneName, Action onComplete = null)
    {
        if (isSwitching)
        {
            Debug.LogWarning("[SceneSwitchManager] 场景切换进行中，请勿重复调用");
            return;
        }

        if (transitionController == null)
        {
            Debug.LogError("[SceneSwitchManager] TransitionController 未找到，无法执行场景切换");
            LoadSceneDirectly(sceneName, onComplete);
            return;
        }

        StartCoroutine(SwitchSceneWithFullFadeCoroutine(sceneName, onComplete));
    }

    private IEnumerator SwitchSceneWithFullFadeCoroutine(string sceneName, Action onComplete)
    {
        isSwitching = true;

        // 打开 Canvas
        if (transitionCanvas != null)
        {
            transitionCanvas.gameObject.SetActive(true);
        }

        // 阶段 1: fadeOut（渐黑）
        bool fadeOutComplete = false;
        transitionController.PlayTransition(fadeOutTransitionName, () =>
        {
            fadeOutComplete = true;
        });

        yield return new WaitUntil(() => fadeOutComplete);

        // 阶段 2: 异步加载场景
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // 等待额外帧，确保 OnSceneLoaded 事件触发并查找 TransitionController
        yield return null;
        yield return null;

        // 阶段 3: fadeIn（渐显）
        if (transitionController != null)
        {
            bool fadeInComplete = false;
            transitionController.PlayTransition(fadeInTransitionName, () =>
            {
                fadeInComplete = true;
            });

            yield return new WaitUntil(() => fadeInComplete);
        }

        // 关闭 Canvas
        if (transitionCanvas != null)
        {
            transitionCanvas.gameObject.SetActive(false);
        }

        isSwitching = false;
        onComplete?.Invoke();
    }

    /// <summary>
    /// 先播放 fadeOut 再播放 fadeIn，fadeOut 完成后执行 onFadeOutComplete，全部完成后执行 onTotalComplete
    /// </summary>
    /// <param name="onFadeOutComplete">fadeOut 完成后的回调（可选）</param>
    /// <param name="onTotalComplete">全部动画完成后的回调（可选）</param>
    public void PlayFadesOnly(Action onFadeOutComplete = null, Action onTotalComplete = null)
    {
        if (transitionController == null)
        {
            Debug.LogError("[SceneSwitchManager] TransitionController 未找到，无法播放动画");
            onFadeOutComplete?.Invoke();
            onTotalComplete?.Invoke();
            return;
        }

        StartCoroutine(PlayFadesOnlyCoroutine(onFadeOutComplete, onTotalComplete));
    }

    private IEnumerator PlayFadesOnlyCoroutine(Action onFadeOutComplete, Action onTotalComplete)
    {
        // 打开 Canvas
        if (transitionCanvas != null)
        {
            transitionCanvas.gameObject.SetActive(true);
        }

        // 阶段 1: fadeOut
        bool fadeOutComplete = false;
        transitionController.PlayTransition(fadeOutTransitionName, () =>
        {
            fadeOutComplete = true;
        });

        yield return new WaitUntil(() => fadeOutComplete);

        // fadeOut 完成回调
        onFadeOutComplete?.Invoke();

        // 阶段 2: fadeIn
        bool fadeInComplete = false;
        transitionController.PlayTransition(fadeInTransitionName, () =>
        {
            fadeInComplete = true;
        });

        yield return new WaitUntil(() => fadeInComplete);

        // 关闭 Canvas
        if (transitionCanvas != null)
        {
            transitionCanvas.gameObject.SetActive(false);
        }

        // 全部完成回调
        onTotalComplete?.Invoke();
    }

    /// <summary>
    /// 播放 fadeFull 动画但不切换场景，完成后执行回调
    /// </summary>
    /// <param name="onComplete">动画完成后的回调（可选）</param>
    public void PlayFullFadeOnly(Action onComplete = null)
    {
        // 打开 Canvas
        if (transitionCanvas != null)
        {
            transitionCanvas.gameObject.SetActive(true);
        }
        if (transitionController == null)
        {
            Debug.LogError("[SceneSwitchManager] TransitionController 未找到，无法播放动画");
            onComplete?.Invoke();
            return;
        }

        transitionController.PlayTransition(fadeFullTransitionName, () =>
        {
            // 关闭 Canvas
            if (transitionCanvas != null)
            {
                transitionCanvas.gameObject.SetActive(false);
            }
            onComplete?.Invoke();
        });
    }

    /// <summary>
    /// 使用 fadeFull 单一过渡动画完成场景切换（手动配置时间版本）
    /// 注意：此方法依赖 fadeFullHalfDuration 字段，需要手动配置与 fadeFull 动画时长匹配
    /// </summary>
    /// <param name="sceneName">目标场景名称</param>
    /// <param name="onComplete">切换完成后的回调（可选）</param>
    public void SwitchSceneWithFullFadeManualTiming(string sceneName, Action onComplete = null)
    {
        if (isSwitching)
        {
            Debug.LogWarning("[SceneSwitchManager] 场景切换进行中，请勿重复调用");
            return;
        }

        if (transitionController == null)
        {
            Debug.LogError("[SceneSwitchManager] TransitionController 未找到，无法执行场景切换");
            LoadSceneDirectly(sceneName, onComplete);
            return;
        }

        StartCoroutine(SwitchSceneWithFullFadeManualTimingCoroutine(sceneName, onComplete));
    }

    private IEnumerator SwitchSceneWithFullFadeManualTimingCoroutine(string sceneName, Action onComplete)
    {
        isSwitching = true;

        // 打开 Canvas
        if (transitionCanvas != null)
        {
            transitionCanvas.gameObject.SetActive(true);
        }

        // 启动 fadeFull 动画（不传回调，因为我们需要在中途切换场景）
        transitionController.PlayTransition(fadeFullTransitionName, null);

        // 等待动画进行到配置的中点时间（完全黑屏时）
        yield return new WaitForSeconds(fadeFullHalfDuration);

        // 异步加载场景
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // 等待场景加载和 TransitionController 查找完成
        yield return null;
        yield return null;

        // 等待 fadeFull 剩余部分完成
        // 注意：此时 transitionController 可能已经是新场景的对象
        // 但原来的动画会继续运行直到完成（因为 SceneSwitchManager 是 DontDestroyOnLoad）
        yield return new WaitForSeconds(fadeFullHalfDuration);

        // 关闭 Canvas
        if (transitionCanvas != null)
        {
            transitionCanvas.gameObject.SetActive(false);
        }

        isSwitching = false;
        onComplete?.Invoke();
    }

    /// <summary>
    /// 降级方案：直接加载场景，无动画
    /// </summary>
    private void LoadSceneDirectly(string sceneName, Action onComplete)
    {
        Debug.LogWarning("[SceneSwitchManager] 降级到无动画场景切换");
        StartCoroutine(LoadSceneDirectlyCoroutine(sceneName, onComplete));
    }

    private IEnumerator LoadSceneDirectlyCoroutine(string sceneName, Action onComplete)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        yield return null;
        onComplete?.Invoke();
    }

    /// <summary>
    /// 检查当前是否正在切换场景
    /// </summary>
    public bool IsSwitching() => isSwitching;

    /// <summary>
    /// 检查当前是否有可用的 TransitionController
    /// </summary>
    public bool HasTransitionController() => transitionController != null;
}
