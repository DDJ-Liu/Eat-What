using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
public class InteractionController : MonoBehaviour
{
    [Header("数据源")]
    public ContainerTag usedContainer;
    public ToolTag usedToolTag;
    public ToolData usedTool;
    public List<IngredientData> usedIngredients;
    public List<ProcessedIngredientData> resultIngredients;

    [Header("生成元素")]
    public Transform IngredientParent;
    public Transform ToolParent;
    public Transform ContainerParent;
    public string relativePath = "Cook Interaction/";
    public string IngredientPath = "Ingredient_Interaction";
    public string ToolPath = "Tool_Interaction";
    public string ContainerPath = "Container_Interaction";
    public GameObject ContainerObject;
    public GameObject ToolObject;
    public List<GameObject> IngredientObjects;
    public GameObject FinishParent;

    [Header("交互状态")]
    public int stage = 0;
    //public UnityEvent ActionDoneEvent;

    // 注释掉动画完成追踪逻辑（不再需要）
    /*[Header("动画完成追踪")]
    private HashSet<Ingredient_Interaction> _completedIngredients = new HashSet<Ingredient_Interaction>();
    private int _expectedCompletionCount = 0;
    private bool _waitingForAnimations = false;

    [SerializeField] private float animationTimeout = 5f;
    private float _animationStartTime;*/

    // Start is called before the first frame update
    protected virtual void Start()
    {
        Debug.Log($"Container Path: {ContainerPath}/{usedContainer}");
        FinishParent = transform.Find("FinishParent").gameObject;
        FinishParent.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        // 注释掉动画超时检查（不再需要）
        /*if (_waitingForAnimations)
        {
            float elapsed = Time.time - _animationStartTime;
            if (elapsed > animationTimeout)
            {
                Debug.LogError($"[InteractionController] Animation timeout after {elapsed:F2}s! Expected {_expectedCompletionCount}, got {_completedIngredients.Count}");
                OnAllAnimationsComplete();
            }
        }*/
    }

    public void Initialize(ContainerTag container, ToolData tool, ToolTag toolTag, List<IngredientData> contentIngredients, List<ProcessedIngredientData> results)
    {
        usedContainer = container;
        usedTool = tool;
        usedIngredients = contentIngredients;
        resultIngredients = results;
        //Debug.Log($"Container Path: {ContainerPath}/{container}");
        Initialize_Spawn();
    }

    protected void Initialize_Spawn()
    {
        //Spawn the shits
        ContainerObject = Tools.LoadAndInstantiatePrefab(relativePath + $"{ContainerPath}/{usedContainer}", ContainerParent);
        ToolObject = Tools.LoadAndInstantiatePrefab(relativePath + $"{ToolPath}/{usedTool.name}", ToolParent);
        if (IngredientObjects.Count != 0)
        {
            foreach (GameObject i in IngredientObjects)
            {
                Destroy(i);
            }
        }
        IngredientObjects.Clear();
        for (int i = 0; i < usedIngredients.Count; i++)
        {
            string IngredientFinalPath = relativePath + $"{IngredientPath}/{usedIngredients[i].name}_{usedToolTag}";
            IngredientObjects.Add(Tools.LoadAndInstantiatePrefab(IngredientFinalPath, IngredientParent));
        }
        IngredientParent.GetComponent<Layout>().ArrangeChildren();
        foreach (GameObject i in IngredientObjects)
        {
            Ingredient_Interaction ing = i.GetComponent<Ingredient_Interaction>();
            if (ing != null)
            {
                ing.myController = this;
            }
        }
        stage = 0;
    }

    public void TriggerAction()
    {
        // 注释掉动画完成追踪逻辑
        /*if (_waitingForAnimations)
        {
            Debug.LogWarning("[InteractionController] Already waiting for animations, ignoring trigger");
            return;
        }

        // 重置追踪状态
        _completedIngredients.Clear();
        _expectedCompletionCount = 0;
        _waitingForAnimations = true;
        _animationStartTime = Time.time;*/

        int nextStage = stage + 1;
        Debug.Log($"[InteractionController] TriggerAction: broadcasting stage {nextStage} to {IngredientObjects.Count} ingredients");

        // 仅触发动画，不追踪完成
        foreach (GameObject ingredientObj in IngredientObjects)
        {
            if (ingredientObj == null) continue;

            Ingredient_Interaction ingredient = ingredientObj.GetComponent<Ingredient_Interaction>();
            if (ingredient != null)
            {
                // 注释掉完成计数
                // _expectedCompletionCount++;
                ingredient.PlayAnim(nextStage);
            }
        }

        // 注释掉边界情况检查
        /*if (_expectedCompletionCount == 0)
        {
            Debug.LogWarning("[InteractionController] No ingredients to animate, completing immediately");
            OnAllAnimationsComplete();
        }*/
    }

    // 注释掉动画完成追踪方法（不再需要）
    /*
    /// <summary>
    /// 由Ingredient_Interaction的AnimEnd()通过Animation Event调用
    /// </summary>
    public void OnIngredientAnimComplete(Ingredient_Interaction ingredient)
    {
        if (!_waitingForAnimations)
        {
            Debug.LogWarning($"[InteractionController] Received completion from {ingredient.name} but not waiting for animations");
            return;
        }

        if (_completedIngredients.Contains(ingredient))
        {
            Debug.LogWarning($"[InteractionController] {ingredient.name} already reported completion, ignoring duplicate");
            return;
        }

        _completedIngredients.Add(ingredient);
        Debug.Log($"[InteractionController] {ingredient.name} animation complete ({_completedIngredients.Count}/{_expectedCompletionCount})");

        // 检查是否所有Ingredient都完成了
        if (_completedIngredients.Count >= _expectedCompletionCount)
        {
            OnAllAnimationsComplete();
        }
    }

    /// <summary>
    /// 所有Ingredient动画完成后的处理
    /// </summary>
    private void OnAllAnimationsComplete()
    {
        if (!_waitingForAnimations) return;

        _waitingForAnimations = false;
        float duration = Time.time - _animationStartTime;
        Debug.Log($"[InteractionController] All animations complete in {duration:F2}s, advancing stage {stage} -> {stage + 1}");
        ActionCompleted();
        // Stage推进
        stage++;

        // 通知Tool_Interaction检查状态
        NotifyToolInteraction();
    }
    */

    /// <summary>
    /// 通知Tool_Interaction进行状态检查（接口设计）
    /// </summary>
    private void NotifyToolInteraction()
    {
        if (ToolObject == null) return;

        // 尝试通过组件方法调用（Tool_Interaction暂未实现）
        var toolComponent = ToolObject.GetComponent("Tool_Interaction");
        if (toolComponent != null)
        {
            // 使用反射调用CheckState方法（如果存在）
            var method = toolComponent.GetType().GetMethod("CheckState");
            if (method != null)
            {
                method.Invoke(toolComponent, new object[] { stage });
            }
        }
    }

    public virtual void ActionCompleted() { stage++; }

    public virtual void onInteractionFinished()
    {
        FinishParent.SetActive(true);
        foreach (var ingredient in resultIngredients)
        {
            CookingManager.Instance.AddIngredient(ingredient);
        }
        CookingManager.Instance.onInteractionDone(resultIngredients);
        
    }
}
