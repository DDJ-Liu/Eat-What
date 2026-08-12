# CutDice 参考模板 - 快速应用指南

## 一、核心模式速查表

### 1.1 继承结构
```
InteractionController (基类)
    ↓ [继承]
CutDice (具体实现)
    - 自主管理state和阈值
    - ScrollBar驱动交互流程
    - 重写ActionCompleted()处理阶段转换
```

### 1.2 关键方法覆盖
```csharp
public class YourInteraction : InteractionController
{
    // 必须重写 - 交互触发时调用
    public override void TriggerAction()
    {
        base.TriggerAction();  // 触发所有食材动画
        // 你的自定义逻辑...
    }
    
    // 必须重写 - 交互完成时调用
    public override void ActionCompleted()
    {
        base.ActionCompleted(); // stage++
        // 阶段转换逻辑...
    }
}
```

---

## 二、从CutDice复用的代码模式

### 2.1 阈值驱动模式
```csharp
private bool _triggered = false;
private bool _done = false;
private float triggerThreshold = 0.3f;
private float doneThreshold = 0.95f;

void OnValueChanged(float value)
{
    // 触发检查
    if (!_triggered && value >= triggerThreshold)
    {
        _triggered = true;
        TriggerAction();
        StartCoroutine(AutoDriveToCompletion(slider));
    }
    
    // 完成检查
    if (!_done && value >= doneThreshold)
    {
        _done = true;
        ActionCompleted();
    }
}
```

### 2.2 自动驱动模式（用户释放后自动推进）
```csharp
private IEnumerator AutoDriveToOne(ScrollBar_Controller slider)
{
    float rate = (1f - triggerThreshold) / autoDriveDuration;
    
    while (slider.Value < 1f)
    {
        // 用户拖动时停止驱动
        bool userDragging = slider.handleDraggable != null && 
                           slider.handleDraggable.dragging;
        
        if (!userDragging)
        {
            float next = Mathf.Min(slider.Value + rate * Time.deltaTime, 1f);
            slider.SetValue(next, true);
        }
        yield return null;
    }
}
```

### 2.3 分阶段完成回调
```csharp
public override void ActionCompleted()
{
    base.ActionCompleted();
    switch(stage)
    {
        case 1:
            OnFirstStageComplete();
            break;
        case 2:
            OnSecondStageComplete();
            break;
        case 3:
            onInteractionFinished();
            break;
    }
}

private void OnFirstStageComplete()
{
    // 隐藏第一个UI，显示第二个
    UIElement1.SetActive(false);
    UIElement2.SetActive(true);
    
    // 切换工具状态
    ToolObject.GetComponent<Tool_Interaction>()
        .OpenGameObjectAndCloseOthers("ModeB");
}
```

---

## 三、数据流模板

### 3.1 CookingRule配置
```csharp
// Tomato_CutDice.asset 示例
CookingRule {
    containerTag: Bowl,
    ingredients: [Tomato],              // 输入食材
    toolTag: CutDice,
    outputs: [TomatoDice],              // 输出结果
    _derivedNormalIngredients: [Tomato],
    _derivedCommonMaterials: []
}
```

### 3.2 初始化数据流
```
[场景] 
  ↓
CookingManager.StartInteraction(rule)
  ↓
InteractionController.Initialize(container, tool, toolTag, 
                                  ingredients, outputs)
  ↓
Initialize_Spawn()
  ├─ 生成Container
  ├─ 生成Tool (CutDice刀具)
  └─ 生成Ingredients (番茄visual)
        ↓
        Ingredient_Interaction.myController = this
```

---

## 四、食材和结果的关系

### 4.1 字段定义
```csharp
public class InteractionController
{
    // 输入：这次交互涉及的原始食材
    public List<IngredientData> usedIngredients;
    
    // 输出：这次交互产生的成品
    public List<ProcessedIngredientData> resultIngredients;
}
```

### 4.2 数据来源
```
CookingRule.ingredients ──→ usedIngredients
CookingRule.outputs ─────→ resultIngredients
```

### 4.3 ProcessedIngredientData含义
```csharp
public class ProcessedIngredientData : IngredientData
{
    // 代表一个已加工的食材
    // 示例：TomatoDice (番茄骰切割后)
    // - 继承所有IngredientData属性
    // - 用于后续烹饪规则的输入或最终成品
}
```

---

## 五、Ingredient_Interaction的使用

### 5.1 职责范围（改造后简化）
```csharp
public class Ingredient_Interaction : MonoBehaviour
{
    // 保留的职责：播放动画
    public void PlayAnim(int stageIndex)
    {
        string animName = $"Resolve_{stageIndex}";
        if (Tools.AnimatorHasState(anim, animName))
        {
            anim.Play(animName);
        }
    }
    
    // 注释掉的职责：汇报完成状态
    // 改造前需要向controller汇报动画完成
    // 改造后：不需要汇报，controller自主驱动
}
```

### 5.2 使用模式
```csharp
// 在InteractionController.TriggerAction()中：
foreach (GameObject ingredientObj in IngredientObjects)
{
    Ingredient_Interaction ingredient = ingredientObj
        .GetComponent<Ingredient_Interaction>();
    
    if (ingredient != null)
    {
        ingredient.PlayAnim(nextStage); // 只播放，不等待
    }
}
// 直接返回，交互控制由CutDice/UI元素驱动
```

---

## 六、Tool_Interaction的使用

### 6.1 主要职责
```csharp
public class Tool_Interaction : MonoBehaviour
{
    // 初始化：收集所有child对象
    private void Awake()
    {
        foreach (Transform child in transform)
        {
            children.Add(child.gameObject);
        }
    }
    
    // 核心方法：独占式开启
    public void OpenGameObjectAndCloseOthers(string name)
    {
        foreach (GameObject child in children)
        {
            child.SetActive(child.name == name);
        }
    }
}
```

### 6.2 使用场景
```csharp
// CutDice中的使用：
// 第一刀时显示水平刀具
ToolObject.GetComponent<Tool_Interaction>()
    .OpenGameObjectAndCloseOthers("Horizontal");

// 第二刀时切换到竖直刀具
ToolObject.GetComponent<Tool_Interaction>()
    .OpenGameObjectAndCloseOthers("Vertical");
```

---

## 七、改造核心理念应用

### 7.1 从动画驱动 → UI驱动
**旧方式（不推荐）：**
```csharp
// 交互等待动画完成
OnIngredientAnimComplete() // Animation Event触发
    → 检查所有动画完成?
    → 推进stage
```

**新方式（推荐）：**
```csharp
// UI驱动交互，动画是装饰
ScrollBar.value >= threshold // 用户操作或自动驱动
    → TriggerAction() // 播放动画（异步，不等待）
    → ActionCompleted() // 立即推进state
    → 处理阶段转换
```

### 7.2 好处
- ✅ 逻辑清晰，不依赖Animation Events
- ✅ 响应迅速，不会卡住
- ✅ 易于调试，状态明确
- ✅ 动画可选，交互不受影响

---

## 八、创建新交互类型检查清单

```
□ 继承InteractionController
□ 实现Start()初始化UI或输入监听
□ 重写TriggerAction() - 触发动画
□ 重写ActionCompleted() - 阶段转换
□ 定义stage相关的回调(onStage1Done等)
□ 不依赖Animation Events汇报状态
□ 在onInteractionFinished()中清理资源
□ 创建对应的prefab和asset配置
□ 在CookingRule中关联ingredients/outputs
```

---

## 九、常见问题解答

**Q: Ingredient_Interaction为什么要移除汇报？**
A: 简化流程。交互逻辑由UI/计时器驱动，动画只是视觉反馈，不影响流程。

**Q: 如何保证动画播放？**
A: TriggerAction()调用PlayAnim()，动画异步执行。即使Animator有问题也不影响交互进度。

**Q: ProcessedIngredientData和IngredientData的区别？**
A: IngredientData = 任何食材类型，ProcessedIngredientData = 特定加工后的结果。在CookingRule中outputs字段强制使用ProcessedIngredientData。

**Q: stage++何时发生？**
A: ActionCompleted()调用时立即发生（改造后）。不再等待动画完成。

**Q: 如何实现多阶段交互？**
A: 在ActionCompleted()的switch中处理各stage，每个case调用对应的stageXDone()方法。


---

## 十、实战案例：创建新的交互类型 (基于CutDice模板)

### 10.1 假设：创建 "Chop" 交互 (剁菜)

**步骤1：创建Chop.cs**
```csharp
public class Chop : InteractionController
{
    [SerializeField] private ScrollBar_Controller chopBar;
    [SerializeField] private float triggerThreshold = 0.4f;
    [SerializeField] private float doneThreshold = 0.9f;
    
    private bool _triggered, _done;
    
    protected override void Start()
    {
        base.Start();
        chopBar.onValueChanged.AddListener(OnChopValueChanged);
    }
    
    private void OnChopValueChanged(float value)
    {
        if (!_triggered && value >= triggerThreshold)
        {
            _triggered = true;
            TriggerAction();
        }
        
        if (!_done && value >= doneThreshold)
        {
            _done = true;
            ActionCompleted();
        }
    }
    
    public override void ActionCompleted()
    {
        base.ActionCompleted();
        onInteractionFinished(); // 单阶段交互，直接完成
    }
}
```

**步骤2：创建Chop.prefab**
- 继承InteractionBase.prefab
- 添加chopBar ScrollBar_Controller
- 关联Chop脚本

**步骤3：创建CookingRule**
- Carrot_Chop.asset
- ingredients: [Carrot]
- outputs: [ChoppedCarrot]

---

## 十一、文件清单快速查阅

### CutDice相关核心文件

| 文件 | 类型 | 用途 |
|------|------|------|
| `CutDice.cs` | Script | 交互逻辑实现 |
| `InteractionController.cs` | Script | 基类框架 |
| `Ingredient_Interaction.cs` | Script | 食材动画 |
| `Tool_Interaction.cs` | Script | 刀具管理 |
| `CutDice.prefab` | Prefab | UI界面 |
| `InteractionBase.prefab` | Prefab | 基础模板 |
| `Tomato_CutDice.prefab` | Prefab | 番茄食材实例 |
| `Tomato_CutDice.asset` | Rule | 规则配置 |
| `TomatoDice.asset` | Result | 切割结果 |

---

## 十二、改造前后对比速查表

| 功能 | 改造前 | 改造后 |
|------|-------|-------|
| **state推进** | Animation Event汇报 | UI阈值/计时器驱动 |
| **Ingredient职责** | 汇报动画完成 | 仅播放动画 |
| **超时处理** | 需要timeout机制 | 无需超时检查 |
| **stage管理** | InteractionController中央管理 + 多层回调 | 具体类自主管理 |
| **动画角色** | 控制流的一部分 | 可选的装饰 |
| **代码复杂度** | 高（多个追踪机制） | 低（直接驱动） |
| **调试难度** | 难（依赖事件) | 易（状态明确) |

---

## 十三、关键类的职责分工

```
┌─────────────────────────────────────────┐
│      CookingManager                     │
│  (场景管理，触发交互)                   │
└────────────────┬────────────────────────┘
                 │ StartInteraction(rule)
                 ▼
┌─────────────────────────────────────────┐
│    InteractionController                │
│  (基类框架，生成UI/Food/Tool)           │
│  - Initialize()                         │
│  - Initialize_Spawn()                   │
│  - TriggerAction()                      │
│  - ActionCompleted()                    │
└────────────────┬────────────────────────┘
                 │ 继承
                 ▼
┌─────────────────────────────────────────┐
│    CutDice (具体实现)                   │
│  - Start() 初始化ScrollBar              │
│  - OnSlice1/2ValueChanged()             │
│  - ActionCompleted() override           │
│  - onSlice1/2Done()                     │
└─────────────────────────────────────────┘

并行的组件：
├─ Ingredient_Interaction (食材)
│  └─ PlayAnim(stage)
├─ Tool_Interaction (刀具)
│  └─ OpenGameObjectAndCloseOthers(mode)
└─ IngredientData / ProcessedIngredientData (数据)
```

---

## 十四、常用代码片段库

### 14.1 多阶段交互模板
```csharp
public class MultiStageInteraction : InteractionController
{
    [SerializeField] private UI_Controller[] stageUIs;
    private int currentStage = 0;
    
    protected override void Start()
    {
        base.Start();
        ShowStage(0);
    }
    
    public override void ActionCompleted()
    {
        base.ActionCompleted(); // stage++
        currentStage = stage - 1; // 同步索引
        
        if (currentStage < stageUIs.Length)
        {
            ShowStage(currentStage);
        }
        else
        {
            onInteractionFinished();
        }
    }
    
    private void ShowStage(int index)
    {
        for (int i = 0; i < stageUIs.Length; i++)
        {
            stageUIs[i].gameObject.SetActive(i == index);
        }
    }
}
```

### 14.2 计时器驱动模板
```csharp
public class TimerInteraction : InteractionController
{
    [SerializeField] private float actionDuration = 2f;
    private float _timer = 0f;
    
    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= actionDuration)
        {
            ActionCompleted();
        }
    }
}
```

### 14.3 输入驱动模板
```csharp
public class InputInteraction : InteractionController
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TriggerAction();
            ActionCompleted();
        }
    }
}
```

---

## 十五、调试技巧

### 15.1 查看stage流程
```csharp
public override void ActionCompleted()
{
    Debug.Log($"[{name}] Stage: {stage} → {stage+1}");
    base.ActionCompleted();
    // ... 阶段处理
}
```

### 15.2 验证动画播放
```csharp
public void PlayAnim(int i)
{
    string animName = $"Resolve_{i}";
    Debug.Log($"[Ingredient] Attempting: {animName}");
    
    if (Tools.AnimatorHasState(anim, animName))
    {
        anim.Play(animName);
        Debug.Log($"[Ingredient] ✓ Playing: {animName}");
    }
    else
    {
        Debug.LogWarning($"[Ingredient] ✗ Missing animation: {animName}");
    }
}
```

### 15.3 检查数据流
```csharp
public void Initialize(...)
{
    Debug.Log($"[Init] Container: {usedContainer}");
    Debug.Log($"[Init] Tool: {usedTool.name}");
    Debug.Log($"[Init] Ingredients: {string.Join(", ", 
        usedIngredients.Select(i => i.name))}");
    Debug.Log($"[Init] Outputs: {string.Join(", ", 
        resultIngredients.Select(r => r.name))}");
    
    Initialize_Spawn();
}
```

---

## 十六、性能优化建议

1. **避免频繁查找**
   ```csharp
   // ✗ 不推荐：每帧都GetComponent
   foreach (var obj in IngredientObjects)
       obj.GetComponent<Ingredient_Interaction>().PlayAnim(stage);
   
   // ✓ 推荐：缓存引用
   private List<Ingredient_Interaction> _cachedIngredients;
   ```

2. **动画状态预检**
   ```csharp
   // 在Start中预检所有动画
   foreach (var ingredient in _cachedIngredients)
   {
       ingredient.ValidateAnimations();
   }
   ```

3. **减少协程开销**
   - CutDice中的AutoDrive仅在需要时启动
   - 合理设置yield返回频率

---

## 十七、常见问题排查

**Q: 交互卡在某个stage？**
A: 检查ActionCompleted()是否被调用。使用Debug.Log追踪stage++的发生。

**Q: 食材动画不播放？**
A: 
1. 检查Animator是否有Resolve_X状态
2. 检查Ingredient_Interaction脚本是否绑定
3. 检查PlayAnim()的animName是否正确

**Q: Tool_Interaction找不到child？**
A: 确保child GameObject的名字与查询字符串完全一致（大小写敏感）

**Q: 为什么ActionCompleted后UI没更新？**
A: onSlice1Done()等回调中应该切换UI可见性，确保逻辑完整

---

## 十八、快速参考：复制粘贴代码

### 从CutDice快速复用（复制这段，改名后使用）
```csharp
public class YourInteraction : InteractionController
{
    [SerializeField] private ScrollBar_Controller slider;
    [SerializeField] private float triggerThreshold = 0.3f;
    [SerializeField] private float doneThreshold = 0.95f;
    
    private bool _triggered, _done;
    
    protected override void Start()
    {
        base.Start();
        slider.onValueChanged.AddListener(OnValueChanged);
    }
    
    private void OnValueChanged(float v)
    {
        if (!_triggered && v >= triggerThreshold)
        {
            _triggered = true;
            TriggerAction();
        }
        if (!_done && v >= doneThreshold)
        {
            _done = true;
            ActionCompleted();
        }
    }
    
    public override void ActionCompleted()
    {
        base.ActionCompleted();
        onInteractionFinished();
    }
}
```


---

## 十九、调试技巧

### 19.1 日志输出点
```csharp
// 在关键位置添加日志
protected override void Start()
{
    Debug.Log($"[{GetType().Name}] Start - ingredients: {usedIngredients.Count}");
    base.Start();
}

public override void TriggerAction()
{
    Debug.Log($"[{GetType().Name}] TriggerAction - stage: {stage}");
    base.TriggerAction();
}

public override void ActionCompleted()
{
    Debug.Log($"[{GetType().Name}] ActionCompleted - stage: {stage} -> {stage + 1}");
    base.ActionCompleted();
}
```

### 19.2 Editor运行时监控
- 在Hierarchy中选中InteractionController的GameObject
- Inspector中观察usedIngredients和resultIngredients列表
- 使用Console窗口过滤特定交互的日志

---

## 二十、兼容性检查

### 20.1 Unity版本要求
- 最低: Unity 2020 LTS
- ScrollBar_Controller需要存在
- Animator.Play(stateName)支持
- IEnumerator协程支持

### 20.2 依赖检查
```csharp
// 确保这些类存在
- ScrollBar_Controller
- Tool_Interaction
- Ingredient_Interaction
- CookingRule / IngredientData / ProcessedIngredientData
- InteractionController (基类)
```

---

## 二十一、扩展思路

### 21.1 支持更多阶段
CutDice是两阶段，可以扩展为三阶段或多阶段：
```csharp
// 三阶段示例
public override void ActionCompleted()
{
    base.ActionCompleted();
    switch(stage)
    {
        case 1: OnStage1Done(); break;
        case 2: OnStage2Done(); break;
        case 3: OnStage3Done(); break;
        case 4: onInteractionFinished(); break;
    }
}
```

### 21.2 支持条件分支
```csharp
// 根据不同条件走不同路线
if (someCondition)
    OnPathA();
else
    OnPathB();
```

### 21.3 支持并行交互
如果多个食材需要同时处理：
```csharp
public override void TriggerAction()
{
    base.TriggerAction(); // 触发所有食材
    // 可选：分别追踪多个slider
}
```

---

## 附录：完整检查清单

```
【分析阶段】
□ 理解InteractionController的基础框架
□ 理解CutDice的两阶段设计
□ 理解ScrollBar驱动的阈值机制
□ 理解ActionCompleted的stage转换
□ 理解Tool_Interaction的UI控制
□ 理解Ingredient_Interaction的简化职责

【设计阶段】
□ 确定新交互需要多少个阶段
□ 列出每个阶段的UI元素变化
□ 列出每个阶段的工具状态变化
□ 列出输入食材和输出结果
□ 设计进度驱动方式（ScrollBar/计时器/拖动等）

【实现阶段】
□ 创建XxxInteraction类继承InteractionController
□ 在Start()中初始化UI和事件监听
□ 重写TriggerAction()触发动画
□ 重写ActionCompleted()处理阶段转换
□ 创建对应的prefab和asset
□ 在CookingRule中配置ingredients和outputs

【测试阶段】
□ 单独测试每个阶段的动画
□ 测试边界条件（快速拖动、释放等）
□ 测试多食材场景
□ 测试中断和重启
□ 检查日志输出完整性
□ 性能profiling（GC等）

【发布前】
□ 移除Debug.Log或改为条件编译
□ 检查所有引用完整
□ 验证prefab关联正确
□ 验证asset配置完整
□ 团队代码审查
```

---

## 最后的话

CutDice的改造体现了一个核心理念：
**把控制权从被动反应的动画完成事件转移到主动驱动的UI/输入层**

这样做的好处是：
- 交互逻辑集中，易于理解和维护
- 不依赖Animation Events的脆弱性
- 响应迅速，用户体验更好
- 动画是可选的装饰，不是必需的控制信号

当你创建下一个交互类型时，始终记住这个模式。不要让交互等待动画，而是让动画跟随交互。

