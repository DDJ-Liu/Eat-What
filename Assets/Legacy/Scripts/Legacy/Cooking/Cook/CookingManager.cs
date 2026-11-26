using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class CookingManager : MonoBehaviour
{
    /// <summary>
    /// 标识食材/工具被"拿在手上"时的来源。Room 层暂不分支处理，预留给后续垃圾区/特效路由。
    /// </summary>
    public enum InHandSource { FromInventory, FromContainer }

    #region FSM
    private CookingPhaseStateBase currentState;
    public NotCookingState defaultState = new NotCookingState();
    public KitchenChoiceState kitchenChoiceState = new KitchenChoiceState();
    public IngredientChooseState ingredientChooseState = new IngredientChooseState();
    public InteractionState interactionState = new InteractionState();
    public CustomerReviewState customerReviewState = new CustomerReviewState();
    private Stack<CookingPhaseStateBase> interruptedStates = new Stack<CookingPhaseStateBase>();

    public void ChangeState(CookingPhaseStateBase newState)
    {
        if (currentState != null)
        {
            currentState.ExitState(this);
        }

        currentState = newState;

        if (currentState != null)
        {
            currentState.EnterState(this);
        }
    }

    public CookingPhaseStateBase GetCurrentState()
    {
        return currentState;
    }

    public void InterruptState(CookingPhaseStateBase newState)
    {
        if (newState == null)
        {
            Debug.LogWarning("[CookingFSM] InterruptState called with null newState, ignored.");
            return;
        }

        if (currentState != null)
        {
            interruptedStates.Push(currentState);
        }

        currentState = newState;
        currentState.EnterState(this);
    }

    public void ReviveState()
    {
        if (interruptedStates.Count == 0)
        {
            Debug.LogWarning("[CookingFSM] ReviveState called but no interrupted state to revive.");
            return;
        }

        if (currentState != null)
        {
            currentState.ExitState(this);
        }

        currentState = interruptedStates.Pop();
        currentState.EnterState(this);
    }

    public bool HasInterruptedState()
    {
        return interruptedStates.Count > 0;
    }

    [TextArea, SerializeField] private string currentStateIdentifier;
    #endregion

    public List<IngredientData> IngredientsInPool;

    [Header("Refs")]
    public CookTableManager tableManager;

    [Header("Kitchen Choice")]
    public int currentKitchenRoomID = 0;
    public KitchenAreaData currentRoomData 
    {
        get { return roomList[currentKitchenRoomID]; }
    }
    public GameObject currentRoomObject
    {
        get { return roomObjectList[currentKitchenRoomID]; }
    }

    public List<KitchenAreaData> roomList = new List<KitchenAreaData>();
    public List<GameObject> roomObjectList = new List<GameObject>();

    public CookingRoomManager currentRoomManager;

    [Header("Spawn")]
    public Transform SpawnedIngredientParent;

    [Header("Output Generation")]
    [SerializeField] private string outputPrefabPathPrefix = "Ingredient/";
    [SerializeField] private float outputSpawnInterval = 0.25f;

    #region Recipe Logic Rework — 新增字段
    [Header("Recipe Logic")]
    [Tooltip("当前选中的菜谱（由 PrepareIngredient/KitchenChoice 阶段注入）")]
    public Recipe currentRecipe;
    /*[Tooltip("当前激活的烹饪 dropzone（由 KitchenChoice 选定厨房后注入）")]
    public CookingDropZone currentDropZone;*/
    [Tooltip("规则表（命中查询）")]
    public RuleBook ruleBook;
    #endregion

    #region ToolTag 状态追踪
    [Header("ToolTag 状态追踪")]
    [Tooltip("当前 Tool 可能使用的 ToolTag 列表")]
    public List<ToolTag> possibleTags = new List<ToolTag>();

    [Tooltip("当前实际使用的 ToolTag")]
    public ToolTag currentTag = ToolTag.None;
    #endregion

    #region 错误处理与硬做模式
    [Header("错误处理与硬做模式")]
    [Tooltip("当前加工周期的错误工具放置次数")]
    private int wrongToolPlacementCount = 0;

    [Tooltip("普通食材 UID 快照，用于检测食材变化")]
    private HashSet<int> lastNormalIngredientSnapshot = new HashSet<int>();

    [Tooltip("硬做模式产出的「不明物体」数据")]
    public ProcessedIngredientData unknownObjectOutput;

    [Tooltip("硬做模式使用的 CookingRule（可选，用于配置交互时长等）")]
    public CookingRule forcedCookingRule;
    #endregion

    #region Singleton
    public static CookingManager Instance;
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

        //加载RuleBook
    }
    #endregion

    private void Start()
    {
        initialize();
    }

    private void Update()
    {
        if (currentState != null)
        {
            currentStateIdentifier = currentState.GetType().Name;
            currentState.UpdateState(this);
        }
    }

    public void initialize()
    {
        if (ruleBook != null) ruleBook.Init();
        ChangeState(kitchenChoiceState);
    }

    public void onChooseRoom(int roomID)
    {
        currentKitchenRoomID = roomID;
        SceneSwitchManager.Instance.PlayFadesOnly(onKitchenChosen,null);
    }

    public void onKitchenChosen()
    {
        //Initialize Inventory stuff

        // 清空 ToolTag 状态（Recipe 初始化时）
        possibleTags.Clear();
        currentTag = ToolTag.None;
        Debug.Log("[ToolTag] Recipe 切换，状态已清空");

        //
        ChangeState(ingredientChooseState);
    }

    #region Recipe Logic Rework — 新增/修改方法

    /// <summary>
    /// 候选规则扫描（无 tool 上下文）：返回所有满足
    ///   - rule.containerTag ∈ zone.containerTags
    ///   - zone.contents ⊆ rule.ingredients
    /// 的规则。用于通用素材判定。
    /// </summary>
    public List<CookingRule> tryResolveRule(CookingDropZone zone)
    {
        var result = new List<CookingRule>();
        if (zone == null || currentRecipe == null || currentRecipe.whitelistRules == null)
            return result;

        foreach (var rule in currentRecipe.whitelistRules)
        {
            if (rule == null || rule.ingredients == null) continue;
            if (zone.containerTag != rule.containerTag) continue;

            bool isSubset = true;
            foreach (var ingredient in zone.contents)
            {
                if (ingredient == null || ingredient.data == null) continue;
                if (!rule.ingredients.Contains(ingredient.data)) { isSubset = false; break; }
            }
            if (isSubset) result.Add(rule);
        }
        return result;
    }

    /// <summary>
    /// CookingDropZone 内容变化的统一入口（由 CookingDropZone 在 contents 变更后直接调用）。
    /// 计算候选规则集；若存在 commonMaterial 需求则唤起 popup，否则隐藏。
    /// </summary>
    public void onDropZoneContentsChanged(CookingDropZone zone)
    {
        if (currentRecipe == null || zone == null) return;

        if (zone.contents == null || zone.contents.Count == 0)
        {
            DogManager.Instance?.commonIngredientManager?.Hide();
            return;
        }

        var candidates = tryResolveRule(zone);
        foreach (var candidate in candidates)
        {
            Debug.Log(candidate.ToString());
        }
        if (candidates.Count == 0)
        {
            DogManager.Instance?.commonIngredientManager?.Hide();
            return;
        }

        var needed = candidates
            .Where(r => r._derivedCommonMaterials != null)
            .SelectMany(r => r._derivedCommonMaterials)
            .Where(m => m != null && !zone.contents.Any(i => i != null && i.data == m))
            .Distinct()
            .ToList();

        if (needed.Count > 0)
        {
            DogManager.Instance?.OpenCommonMaterialPopup(needed);
        }
        else
        {
            DogManager.Instance?.CloseCommonMaterialPopup();
            DogManager.Instance?.commonIngredientManager?.Hide();
        }

        // 食材变化检测与错误计数重置
        HashSet<int> currentNormalIngredients = new HashSet<int>();
        foreach (var ingredient in zone.contents)
        {
            if (ingredient != null && ingredient.data != null && !ingredient.data.isCommonMaterial)
            {
                currentNormalIngredients.Add(ingredient.data.uid);
            }
        }

        // 检测食材是否发生变化
        bool ingredientsChanged = !currentNormalIngredients.SetEquals(lastNormalIngredientSnapshot);

        if (ingredientsChanged)
        {
            lastNormalIngredientSnapshot = currentNormalIngredients;

            // 食材变化时重置错误计数
            if (wrongToolPlacementCount > 0)
            {
                Debug.Log($"[CookingManager] 食材变化，重置错误计数（之前: {wrongToolPlacementCount}）");
                wrongToolPlacementCount = 0;
            }
        }
    }

    /// <summary>
    /// 食材成功放入 dropzone 的转发/记录入口。
    /// 实际 contents 维护在 <see cref="CookingDropZone"/>，本方法用于业务侧扩展（埋点、教程等）。
    /// </summary>
    /// <param name="ingredient">放入的食材数据</param>
    /// <param name="zone">目标 dropzone</param>
    /// <param name="isFirstPlacement">true = 初次放置（spawn 后首次），false = 捡起后重新放置</param>
    public void onIngredientPlacedIn(Ingredient_Cooking ingredient, CookingDropZone zone, bool isFirstPlacement)
    {
        string placementType = isFirstPlacement ? "初次放置" : "重新放置";
        Debug.Log($"[CookingManager] Ingredient placed ({placementType}): {ingredient?.name} → {zone?.name}");
        //ingredient.transform.position = zone.
    }

    /// <summary>
    /// Tool 投放后触发：消解 ToolTag → 查 RuleBook → 命中则进入 ProcessInteractionState。
    /// 由 Tool/Interaction 在工具落入 dropzone 时调用。
    /// </summary>
    public void onToolPlacedIn(Tool_Cooking tool, CookingDropZone zone)
    {
        if (tool == null || tool.data == null || zone == null || currentRecipe == null)
        {
            Debug.LogWarning("[CookingManager] onToolPlacedIn: missing tool/zone/recipe.");
            return;
        }

        var rule = ResolveRule(tool, zone);
        if (rule != null && currentRecipe.whitelistRules != null && currentRecipe.whitelistRules.Contains(rule))
        {
            interactionState.Setup(rule, zone, tool);
            SpawnInteraction();
            InterruptState(interactionState);
        }
        else
        {
            // 规则未命中 → 无条件累积错误次数
            wrongToolPlacementCount++;
            Debug.Log($"[CookingManager] 错误工具放置 #{wrongToolPlacementCount}/3");

            // 判断是否进入硬做模式
            if (wrongToolPlacementCount >= 4)
            {
                EnterForcedCookingMode(zone, tool);
            }
            else
            {
                HandleWrongToolFeedback(tool, zone, wrongToolPlacementCount);
            }
        }
    }

    /// <summary>
    /// 根据 currentTag 生成对应的 InteractionController 预制体。
    /// 预制体路径："Cook Interaction/InteractionObjects/{ToolTag}"
    /// </summary>
    private void SpawnInteraction()
    {
        if (currentTag == ToolTag.None)
        {
            Debug.LogError("[CookingManager] SpawnInteraction: currentTag is None");
            return;
        }

        if (currentRoomManager == null || currentRoomManager.InteractionParent == null)
        {
            Debug.LogError("[CookingManager] SpawnInteraction: InteractionParent is null");
            return;
        }

        string prefabPath = $"Cook Interaction/InteractionObjects/{currentTag}";

        GameObject interactionObj = Tools.LoadAndInstantiatePrefab(prefabPath, currentRoomManager.InteractionParent);

        if (interactionObj == null)
        {
            Debug.LogError($"[CookingManager] Failed to load prefab: {prefabPath}");
            return;
        }

        InteractionController controller = interactionObj.GetComponent<InteractionController>();

        if (controller == null)
        {
            Debug.LogError($"[CookingManager] Prefab missing InteractionController: {prefabPath}");
            Destroy(interactionObj);
            return;
        }

        interactionState.contextController = controller;

        ContainerTag containerTag = interactionState.contextZone != null
            ? interactionState.contextZone.containerTag
            : ContainerTag.None;

        ToolData toolData = interactionState.contextTool != null
            ? interactionState.contextTool.data
            : null;

        List<IngredientData> ingredients = interactionState.contextZone != null
            ? interactionState.contextZone.GetSortedContents().ToList()
            : new List<IngredientData>();

        List<ProcessedIngredientData> results = interactionState.contextRule != null
            ? interactionState.contextRule.outputs
            : new List<ProcessedIngredientData>();

        controller.Initialize(containerTag, toolData, currentTag, ingredients, results);

        Debug.Log($"[CookingManager] Created {currentTag} interaction controller");
    }

    /// <summary>
    /// 检查 Tool 是否被当前 Recipe 和 Zone 允许（供 CookingDropZone 准入检查用）。
    /// </summary>
    public bool CanAcceptTool(Tool_Cooking tool, CookingDropZone zone)
    {
        if (tool == null || tool.data == null || zone == null || currentRecipe == null)
            return false;

        var rule = ResolveRule(tool, zone);
        return rule != null && currentRecipe.whitelistRules != null && currentRecipe.whitelistRules.Contains(rule);
    }

    /// <summary>
    /// 候选 Rule 扫描 → 以候选 Rule 的 (containerTag, toolTag) 调 RuleBook.Query。
    /// 满足策划约束 2 时唯一命中；多 ContainerTag 的 dropzone 在此精确消解。
    /// </summary>
    private CookingRule ResolveRule(Tool_Cooking tool, CookingDropZone zone)
    {
        if (ruleBook == null || currentRecipe == null) return null;

        var allowedTags = currentRecipe.allowedToolTags;
        var contents = zone.GetSortedContents().ToList();

        // 计算可能的 ToolTag 列表
        possibleTags.Clear();
        if (tool != null && tool.data != null && tool.data.toolTags != null)
        {
            foreach (var tag in tool.data.toolTags)
            {
                if (allowedTags == null || allowedTags.Count == 0 || allowedTags.Contains(tag))
                {
                    possibleTags.Add(tag);
                }
            }
        }

        foreach (var rule in currentRecipe.whitelistRules)
        {
            if (rule == null) continue;
            if (zone.containerTag != rule.containerTag) continue;
            if (allowedTags != null && allowedTags.Count > 0 && !allowedTags.Contains(rule.toolTag)) continue;
            if (tool.data.toolTags == null || !tool.data.toolTags.Contains(rule.toolTag)) continue;

            var hit = ruleBook.Query(contents, rule.containerTag, rule.toolTag);
            if (hit == rule)
            {
                // 更新当前确定的 ToolTag
                currentTag = rule.toolTag;
                Debug.Log($"[ToolTag] 匹配成功 → currentTag: {currentTag}, possibleTags: [{string.Join(", ", possibleTags)}]");
                return hit;
            }
        }

        // 匹配失败时重置 currentTag
        currentTag = ToolTag.None;
        return null;
    }

    /// <summary>
    /// 加工完成回调：消耗 dropzone.contents，实例化 rule.output。
    /// 由 ToolInteraction.onInteractionFinished → Tool.onInteractionDone 链路调用。
    /// </summary>
    public void onInteractionDone(List<ProcessedIngredientData> results)
    {
        StartCoroutine(OnInteractionDoneCoroutine(results));
    }

    /// <summary>
    /// 交互完成的协程流程：先生成输出食材，等待展示，再执行清理和状态切换
    /// </summary>
    private IEnumerator OnInteractionDoneCoroutine(List<ProcessedIngredientData> results)
    {
        var rule = interactionState.contextRule;
        var zone = interactionState.contextZone;
        var tool = interactionState.contextTool;

        // 1. 生成输出食材
        yield return StartCoroutine(GenerateOutputsCoroutine(results));

        // 2. 等待 3 秒展示生成的食材
        yield return new WaitForSeconds(3f);

        // 3. 清理和后续流程
        if (rule != null && zone != null)
        {
            // 清理 DropZone 中的食材实例（销毁 GameObject）
            if (zone.contents != null && zone.contents.Count > 0)
            {
                foreach (var ingredient in zone.contents)
                {
                    if (ingredient != null && ingredient.gameObject != null)
                    {
                        Destroy(ingredient.gameObject);
                    }
                }
            }

            // 清空 contents 列表
            zone.ClearContents();

            // 将 Tool 放回原位
            if (tool != null)
            {
                tool.OnPlaceCancel();
            }
        }

        // 清零加工周期状态
        ResetCookingSession();

        if (rule.isFinal)
        {
            //Go To Seasoning State
        }
        else
        {
            ReviveState();
        }
    }

    public void onInteractionStarted()
    {
        ChangeState(interactionState);
    }

    /// <summary>
    /// 协程：根据传入的输出列表，间隔生成食材 prefab
    /// </summary>
    private IEnumerator GenerateOutputsCoroutine(List<ProcessedIngredientData> outputs)
    {
        if (outputs == null || outputs.Count == 0)
        {
            Debug.LogWarning("[CookingManager] Outputs is null or empty");
            yield break;
        }

        Debug.Log($"[CookingManager] Start generating {outputs.Count} outputs");

        int generatedCount = 0;
        int totalCount = outputs.Count;

        foreach (var processedIngredient in outputs)
        {
            if (processedIngredient == null)
            {
                Debug.LogWarning("[CookingManager] Null ingredient in outputs, skipping");
                continue;
            }

            // 构造 prefab 路径
            string prefabPath = $"{outputPrefabPathPrefix}/{processedIngredient.name}";

            // 加载并实例化 prefab
            GameObject outputObj = Tools.LoadAndInstantiatePrefab(
                prefabPath,
                interactionState.contextController.FinishParent.transform
            );

            if (outputObj == null)
            {
                Debug.LogError($"[CookingManager] Failed to spawn output: {prefabPath}");
                continue;
            }

            // 初始化 Ingredient_Cooking 组件
            /*var ingredientCooking = outputObj.GetComponent<Ingredient_Cooking>();
            if (ingredientCooking != null)
            {
                ingredientCooking.data = processedIngredient;
                Debug.Log($"[CookingManager] Generated output: {processedIngredient.name}");
            }
            else
            {
                Debug.LogWarning($"[CookingManager] Output prefab missing Ingredient_Cooking component: {prefabPath}");
            }*/

            generatedCount++;

            // 只在非最后一个元素时等待间隔
            if (generatedCount < totalCount)
            {
                yield return new WaitForSeconds(outputSpawnInterval);
            }
        }

        Debug.Log("[CookingManager] All outputs generated");
    }

    /// <summary>
    /// 验证给定食材在当前厨房区域中可用的容器标签。
    /// 取三集合的交集：
    ///   1. 当前厨房可用的 ContainerTag（currentRoomData.containerTags）
    ///   2. Recipe 白名单中的 ContainerTag（currentRecipe.whitelistRules）
    ///   3. 食材自身可用的 ContainerTag（ruleBook.rules 中包含该食材的规则）
    /// 唯一命中返回该 tag；0 命中或 >1 命中返回 None（>1 时附 LogWarning）。
    /// </summary>
    public ContainerTag validateContainerTag(IngredientData ingredientData)
    {
        if (ingredientData == null)
        {
            Debug.LogWarning("[CookingManager] validateContainerTag: ingredientData is null");
            return ContainerTag.None;
        }

        if (ruleBook == null || ruleBook.rules == null)
        {
            Debug.LogWarning("[CookingManager] validateContainerTag: ruleBook or rules is null");
            return ContainerTag.None;
        }

        if (currentRecipe == null || currentRecipe.whitelistRules == null)
        {
            return ContainerTag.None;
        }

        if (currentRoomData == null || currentRoomData.containerTags == null)
        {
            return ContainerTag.None;
        }

        // 1. 厨房可用 tag 集
        var roomTags = new HashSet<ContainerTag>(currentRoomData.containerTags);

        // 2. Recipe 白名单 tag 集
        var recipeTags = new HashSet<ContainerTag>();
        foreach (var rule in currentRecipe.whitelistRules)
        {
            if (rule != null) recipeTags.Add(rule.containerTag);
        }

        // 3. 食材自身可用 tag 集
        var ingredientTags = new HashSet<ContainerTag>();
        foreach (var rule in ruleBook.rules)
        {
            if (rule == null || rule.ingredients == null) continue;
            if (rule.ingredients.Contains(ingredientData)) ingredientTags.Add(rule.containerTag);
        }

        // 三集合交集
        var intersection = roomTags;
        intersection.IntersectWith(recipeTags);
        intersection.IntersectWith(ingredientTags);

        if (intersection.Count == 0) return ContainerTag.None;

        if (intersection.Count > 1)
        {
            Debug.LogWarning($"[CookingManager] validateContainerTag: 食材 {ingredientData.name} 在当前上下文有多个可用容器标签: {string.Join(", ", intersection)}（策划配表问题）");
            return ContainerTag.None;
        }

        return intersection.First();
    }

    /// <summary>
    /// 给"工具拿在手上"流程使用：基于当前激活 Container 的 contents + 工具 ToolTag，
    /// 在 Recipe 白名单中扫描候选 Rule。命中唯一规则则返回该规则的 containerTag；
    /// 0 或 >1 命中均返回 None（>1 时 Debug.LogWarning）。
    /// </summary>
    public ContainerTag validateContainerTagForTool(ToolData toolData)
    {
        if (toolData == null || toolData.toolTags == null) return ContainerTag.None;
        if (currentRecipe == null || currentRecipe.whitelistRules == null) return ContainerTag.None;
        if (currentRoomManager == null) return ContainerTag.None;

        var active = currentRoomManager.getActiveSingleContainer();
        if (active == null || active.dropZone == null) return ContainerTag.None;

        var contents = active.dropZone.GetSortedContents();

        var matched = new List<CookingRule>();
        foreach (var rule in currentRecipe.whitelistRules)
        {
            if (rule == null) continue;
            if (rule.containerTag != active.ContainerTag) continue;
            if (!toolData.toolTags.Contains(rule.toolTag)) continue;
            if (currentRecipe.allowedToolTags != null && currentRecipe.allowedToolTags.Count > 0
                && !currentRecipe.allowedToolTags.Contains(rule.toolTag)) continue;

            // active zone 的 contents 必须是 rule.ingredients 的子集
            bool isSubset = true;
            if (rule.ingredients == null) { if (contents.Count > 0) isSubset = false; }
            else
            {
                foreach (var data in contents)
                {
                    if (!rule.ingredients.Contains(data)) { isSubset = false; break; }
                }
            }
            if (!isSubset) continue;

            matched.Add(rule);
        }

        if (matched.Count == 0) return ContainerTag.None;
        if (matched.Count > 1)
        {
            Debug.LogWarning($"[CookingManager] validateContainerTagForTool: 工具 {toolData.name} 命中多条规则（策划配表问题）");
            return ContainerTag.None;
        }
        return matched[0].containerTag;
    }

    /// <summary>
    /// 计算指定食材在指定 ContainerTag 规则下的最大允许数量。
    /// 扫描 currentRecipe.whitelistRules 中所有匹配 containerTag 的规则，
    /// 返回该食材在规则中出现次数的最大值。
    /// </summary>
    public int GetMaxAllowedCount(IngredientData ingredientData, ContainerTag containerTag)
    {
        if (ingredientData == null) return 0;
        if (currentRecipe == null || currentRecipe.whitelistRules == null) return 0;

        int maxAllowed = 0;
        foreach (var rule in currentRecipe.whitelistRules)
        {
            if (rule == null || rule.ingredients == null) continue;
            if (containerTag != rule.containerTag) continue;

            int countInRule = rule.ingredients.Count(i => i != null && i.uid == ingredientData.uid);
            if (countInRule > maxAllowed) maxAllowed = countInRule;
        }
        return maxAllowed;
    }

    /// <summary>
    /// 检查指定 ContainerTag 对应的 Container 是否还能接受更多该食材。
    /// 基于当前 Recipe 白名单规则和 Container 当前内容，计算数量限制。
    /// 通用素材（isCommonMaterial）不受数量限制。
    /// </summary>
    private bool CanContainerAcceptMoreIngredient(IngredientData ingredientData, ContainerTag containerTag)
    {
        if (ingredientData == null) return false;
        if (currentRoomManager == null) return false;

        // 通用素材不受数量限制（由 popup 路径注入）
        if (ingredientData.isCommonMaterial) return true;

        // 查找对应的 Container 和 DropZone
        var container = currentRoomManager.searchForContainerByTag(containerTag);
        if (container == null || container.dropZone == null) return false;

        var dropZone = container.dropZone;
        if (dropZone.contents == null) return true;

        // 统计当前 zone 中该食材的数量
        int currentCount = dropZone.contents.Count(i => i != null && i.data != null && i.data.uid == ingredientData.uid);

        // 计算规则允许的最大数量
        int maxAllowed = GetMaxAllowedCount(ingredientData, containerTag);

        // 返回是否还有空间
        return currentCount < maxAllowed;
    }

    /// <summary>
    /// 食材"拿在手上"统一入口：解算 ContainerTag 并转交给 Room 层调度。
    /// 由 Ingredient_Cooking.IngredientInHandBehaviors 调用。
    /// </summary>
    public void onIngredientInHand(IngredientData data, InHandSource source)
    {
        if (currentRoomManager == null) return;

        // 1. 验证 ContainerTag（三集合交集：厨房、Recipe、食材）
        var tag = validateContainerTag(data);
        if (tag == ContainerTag.None)
        {
            currentRoomManager.HandleItem(ContainerTag.None, source);
            return;
        }

        // 2. 检查数量限制（通用素材会自动通过）
        if (!CanContainerAcceptMoreIngredient(data, tag))
        {
            // 数量已达上限，不打开 DropZone
            currentRoomManager.HandleItem(ContainerTag.None, source);
            return;
        }

        // 3. 通过所有检查，打开对应 Container 的 DropZone
        currentRoomManager.HandleItem(tag, source);
    }

    /// <summary>
    /// 工具"拿在手上"统一入口：解算 ContainerTag 并转交给 Room 层调度。
    /// 由 Tool_Cooking.ToolInHandBehaviors 调用。
    /// </summary>
    public void onToolInHand(ToolData data, InHandSource source)
    {
        if (currentRoomManager == null) return;
        var tag = validateContainerTagForTool(data);
        currentRoomManager.HandleItem(tag, source);
    }

    /// <summary>
    /// 玩家手中无物时的统一入口：所有 DropZone 关闭、隐藏 DropZoneHighLight，
    /// 但保持 Container 的 Highlight 状态（由 Room 层决定是否重置）。
    /// 由放置成功 / 取消放置等收尾路径调用。
    /// </summary>
    public void onItemReleased()
    {
        if (currentRoomManager == null) return;
        currentRoomManager.HandleItem(ContainerTag.None, InHandSource.FromInventory);
    }

    /// <summary>
    /// 往玩家的物品栏中添加食材
    /// </summary>
    /// <param name="Ingredient"></param>
    public void AddIngredient(ProcessedIngredientData Ingredient)
    {
        //Visual====

        //Visual====
        IngredientInventory.Instance.AddNewItemToIngredientTray(Ingredient);
    }

    private void AddIngredient_TransitionFinish(ProcessedIngredientData Ingredient)
    {
        IngredientInventory.Instance.AddNewItemToIngredientTray(Ingredient);
    }

    #endregion

    #region 错误处理方法

    /// <summary>
    /// 重置错误计数和食材快照
    /// </summary>
    private void ResetCookingSession()
    {
        wrongToolPlacementCount = 0;
        lastNormalIngredientSnapshot.Clear();

        // 清空 ToolTag 状态
        possibleTags.Clear();
        currentTag = ToolTag.None;

        Debug.Log("[CookingManager] 错误计数重置，ToolTag 状态已清空");
    }

    /// <summary>
    /// 进入硬做模式（第4次错误）
    /// </summary>
    private void EnterForcedCookingMode(CookingDropZone zone, Tool_Cooking tool)
    {
        Debug.Log("[CookingManager] 进入硬做模式（第4次错误）");

        if (forcedCookingRule == null)
        {
            Debug.LogError("[CookingManager] forcedCookingRule not configured");
            return;
        }

        currentTag = forcedCookingRule.toolTag;

        interactionState.Setup(forcedCookingRule, zone, tool);

        SpawnInteraction();

        InterruptState(interactionState);
    }

    /// <summary>
    /// 处理错误工具放置的反馈（前3次）
    /// </summary>
    private void HandleWrongToolFeedback(Tool_Cooking tool, CookingDropZone zone, int errorCount)
    {
        Debug.Log($"[CookingManager] 错误工具 {tool.data.name}，第 {errorCount}/3 次");

        // 触发 Zone 的拒绝事件（复用现有机制）
        zone.onToolRejected?.Invoke(tool.data);

        // 工具回到原位
        tool.OnPlaceCancel();
    }

    #endregion
}
