# CutDice改造分析文档

## 一、项目概览

CutDice 是一个Unity烹饪交互系统的改造项目，专注于切割交互（两次切割骰子方式）。本分析记录了改造前后的架构变化。

---

## 二、找到的文件清单

### 2.1 核心脚本文件
- `Assets/Scripts/Cooking/Interaction/CutDice.cs` - 主交互逻辑
- `Assets/Scripts/Cooking/Interaction/InteractionController.cs` - 基类控制器
- `Assets/Scripts/Cooking/Ingredient/Ingredient_Interaction.cs` - 食材交互组件
- `Assets/Scripts/Cooking/Tool/Tool_Interaction.cs` - 工具交互组件
- `Assets/Scripts/Cooking/Interaction/Stir.cs` - 对比参考（搅拌交互）
- `Assets/Scripts/DataBase/CookingData/CookingRule.cs` - 烹饪规则定义
- `Assets/Scripts/DataBase/CookingData/IngredientData.cs` - 食材数据
- `Assets/Scripts/DataBase/CookingData/ProcessedIngredientData.cs` - 处理后食材数据

### 2.2 Prefab文件
- `Assets/Resources/Cook Interaction/InteractionObjects/CutDice.prefab` - CutDice交互UI
- `Assets/Resources/Cook Interaction/InteractionObjects/InteractionBase.prefab` - 基础交互界面 [修改]
- `Assets/Resources/Cook Interaction/Ingredient_Interaction/Tomato_CutDice.prefab` - 番茄CutDice食材
- `Assets/Resources/Cook Interaction/Results/TomatoDice.prefab` - 番茄骰切割结果 [修改]
- `Assets/Resources/Ingredient/TomatoDice.prefab` - 新增结果预制体

### 2.3 Asset文件
- `Assets/Scripts/DataBase/CookingData/Example/Rules/Tomato_CutDice.asset` - 番茄切割规则配置
- `Assets/Scripts/DataBase/CookingData/Example/Ingredients/TomatoDice.asset` - 番茄骰结果配置 [修改]

---

## 三、CutDice新架构设计

### 3.1 类设计

```
InteractionController (基类)
    ↓
CutDice (具体实现)
    - 两个ScrollBar控制器（水平和竖直切割）
    - 阈值管理（触发、完成）
    - 自动驱动机制
    - 分阶段完成回调
```

### 3.2 CutDice.cs 核心设计

**主要组件：**
```csharp
class CutDice : InteractionController
{
    // UI控制
    ScrollBar_Controller slice1;    // 第一刀（水平）
    ScrollBar_Controller slice2;    // 第二刀（竖直）
    
    // 参数控制
    float triggerThreshold = 0.3f;  // 触发阈值
    float doneThreshold = 0.95f;    // 完成阈值
    float autoDriveDuration = 0.6f; // 自动驱动时长
    
    // 状态追踪
    bool _slice1Triggered, _slice1Done;
    bool _slice2Triggered, _slice2Done;
}
```

**交互流程：**
1. Start() - 初始化两个切割条，激活第一个
2. OnSlice1ValueChanged() - 监听第一刀进度
   - v >= triggerThreshold → 触发第一刀动画
   - v >= doneThreshold → 切割完成，调用ActionCompleted()
3. AutoDriveToOne() - 自动驱动机制
   - 用户释放后自动推进到1.0
   - 用户拖动时停止自动驱动
4. onSlice1Done() - 第一刀完成回调
   - 隐藏slice1，激活slice2
   - 切换刀具方向（水平→竖直）
5. 重复步骤2-4处理第二刀
6. onSlice2Done() → onInteractionFinished() - 交互结束

---

## 四、改造前后的变化点

### 4.1 动画完成追踪机制的移除

**旧设计：**
```csharp
// InteractionController中的旧代码（已注释）
private HashSet<Ingredient_Interaction> _completedIngredients;
private int _expectedCompletionCount;
private bool _waitingForAnimations;

public void OnIngredientAnimComplete(Ingredient_Interaction ingredient)
{
    // 追踪每个食材的动画完成
    // 所有食材完成后才推进stage
}
```

问题：
- 复杂的同步机制
- 容易出现超时/卡死
- 依赖Animation Events的可靠性
- 动画完成汇报逻辑重复

**新设计：**
```csharp
// InteractionController中的新代码
public void TriggerAction()
{
    // 仅触发动画，不追踪完成
    foreach (GameObject ingredientObj in IngredientObjects)
    {
        ingredient.PlayAnim(nextStage);
    }
    // 直接返回，不等待完成
}

public virtual void ActionCompleted() { stage++; }
```

优点：
- 简化了控制流
- 移除了超时风险
- 交互控制权在UI层（ScrollBar/CutDice）
- 动画和交互逻辑解耦

### 4.2 分阶段交互设计

**新特点：**
- CutDice拥有自己的stage管理（slice1/slice2）
- ActionCompleted()在CutDice中重写，不再依赖动画
- 通过ScrollBar的阈值驱动交互推进，而非动画完成事件

---

## 五、Result和Ingredient的关系

### 5.1 数据结构关系

```
CookingRule (配置)
├─ ingredients: List<IngredientData>      // 输入食材
│  └─ 包含普通食材和通用素材（isCommonMaterial标记）
└─ outputs: List<ProcessedIngredientData> // 输出成品

ProcessedIngredientData
└─ 继承自 IngredientData
└─ 代表处理后的食材（如 TomatoDice）

Tomato_CutDice.asset配置示例：
{
  ingredients: [Tomato]           // 输入：番茄
  outputs: [TomatoDice]           // 输出：番茄骰
  containerTag: 1
  toolTag: 1 (CutDice)
}
```

### 5.2 交互数据流

```
CookingRule (规则)
    ↓
InteractionController.Initialize()
    ├─ usedIngredients ← rule.ingredients
    ├─ resultIngredients ← rule.outputs
    └─ Initialize_Spawn()
        ├─ 生成Container (碗)
        ├─ 生成Tool (刀)
        └─ 生成Ingredients (食材视觉)
            └─ 每个IngredientObject持有myController引用
```

### 5.3 关键变化

**TomatoDice.asset 的变化：**
```yaml
# 变化：Inv_Sprite
- 旧: guid: 7ff63191a192df94384a7d51740f23f3
+ 新: guid: 7723e394eec029b4992cf91a6791c286
```
说明：番茄骰的库存图标被更新为更准确的切割后番茄骰形象

---

## 六、InteractionBase的使用方式

### 6.1 InteractionBase.prefab 结构

从prefab文件分析，InteractionBase包含：
- **WindowBase** - 窗口背景组件
- **9-Sliced** - UI九宫切割背景
- **旋转器组件** - 用于食材展示的旋转动画

### 6.2 派生关系

```
InteractionBase.prefab (模板)
    ↓ [Prefab Instance]
CutDice.prefab (具体实现)
    ├─ ScrollBar (1) - 第一个切割条
    └─ ScrollBar (2) - 第二个切割条（初始隐藏）

Tomato_CutDice.prefab (场景实例)
    └─ 食材GameObject组
        └─ Ingredient_Interaction脚本
```

### 6.3 CutDice.prefab配置细节

**ScrollBar配置：**
- 两个ScrollBar_Controller实例
- 绑定到OnSlice1/2ValueChanged回调
- 处理.Handle的拖动交互

**Tool关联：**
```csharp
ToolObject.GetComponent<Tool_Interaction>()
    .OpenGameObjectAndCloseOthers("Horizontal"); // 第一刀
    .OpenGameObjectAndCloseOthers("Vertical");   // 第二刀
```

---

## 七、改造解决的问题

### 7.1 主要问题

1. **动画完成的不可靠性**
   - 旧系统依赖Animation Events触发汇报
   - 可能导致漏报或重复报告
   - 解决方案：移除动画追踪，改为UI驱动

2. **stage推进的复杂性**
   - 旧系统需要等待所有食材动画完成
   - 超时机制和错误恢复复杂
   - 解决方案：CutDice自主管理stage，动画成为装饰

3. **食材和交互的耦合**
   - Ingredient_Interaction依赖向上汇报
   - 解决方案：Ingredient_Interaction只负责播放动画，不汇报状态

4. **Result和Ingredient的混淆**
   - 旧系统中食材和结果没有清晰区分
   - 解决方案：
     - IngredientData = 输入食材
     - ProcessedIngredientData = 输出成品
     - CookingRule明确分离outputs字段

### 7.2 架构改进

| 方面 | 旧设计 | 新设计 |
|------|-------|-------|
| **状态管理** | 分散在多个类中 | 集中在InteractionController |
| **动画** | 动画完成是交互推进的信号 | 动画是可选装饰，UI驱动推进 |
| **Ingredient** | 需要汇报完成状态 | 仅播放动画，无汇报 |
| **食材结果** | 模糊定义 | ProcessedIngredientData明确 |
| **工具管理** | 被动响应 | Tool_Interaction主动控制子对象 |

---

## 八、参考模板用途

### 8.1 应用场景

CutDice可以作为以下交互的参考：
- **多阶段交互** - 需要分多步完成的操作
- **进度条驱动** - 基于用户拖动进度而非时间的交互
- **工具切换** - 需要在不同模式间切换的工具

### 8.2 关键代码模式

```csharp
// 模式1：分阶段触发回调
switch(stage)
{
    case 1: onSlice1Done(); break;
    case 2: onSlice2Done(); break;
}

// 模式2：阈值驱动
if (!_triggered && v >= triggerThreshold)
{
    _triggered = true;
    TriggerAction();        // 触发动画
    StartCoroutine(AutoDrive()); // 自动推进
}

// 模式3：自动驱动
IEnumerator AutoDriveToOne(ScrollBar_Controller slider)
{
    while (slider.Value < 1f)
    {
        if (!userDragging) slider.Value = Mathf.Min(...);
        yield return null;
    }
}
```

---

## 九、总结

### 核心改造理念
**从动画驱动 → UI驱动**

- 原来：UI等待动画完成
- 现在：动画跟随UI进度

### 核心设计模式
1. **InteractionController** - 基类框架，管理生成和初始化
2. **CutDice** - 具体实现，自主管理交互逻辑
3. **Ingredient_Interaction** - 被动响应，仅播放动画
4. **Tool_Interaction** - 工具管理，控制可见状态

### 扩展建议
- 新交互类型继承InteractionController
- 重写TriggerAction和ActionCompleted实现具体逻辑
- 使用ScrollBar/拖动/计时器等UI元素驱动状态推进
- 将动画视为装饰，不作为控制流的一部分
