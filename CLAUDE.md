# CLAUDE.md

本文件为 Claude Code 在本仓库工作时提供指引。工作规范另见 [AGENTS.md](AGENTS.md)（Unity 资源安全、最小改动原则等），两者同时生效。

## 项目概览

《是啊，吃什么》(`Eat-WhatProject`) —— Unity 2D 做菜游戏，处于 Demo 开发阶段。

- Unity **2022.3.62f2**，URP 14.0.12，New Input System 1.14.2，Cinemachine，TextMeshPro，Spine
- C# 脚本走默认 `Assembly-CSharp`（无 asmdef），约 200 个 `.cs`
- 无 CLI 构建管线、无自动化测试套件；验证靠 Unity Editor + Play Mode
- Unity MCP (`com.coplaydev.unity-mcp`) 已接入，可直接操作 Editor

代码分两大部分：**通用交互框架**（`MouseInteractive/`、`Tools/`、`GridPlacement/`、`Dialog/`、`Command/`，可复用，前身是 ButtonProject）和**做菜游戏逻辑**（`Cooking/`、`DataBase/CookingData/`）。

## 目录地图

```
Assets/
  Scripts/
    MouseInteractive/   通用鼠标交互框架（Button/Switch/Checkbox/DropDown/Drag/DropZone/Scroll/Hold/HoverZone/Keyboard）
    Cooking/            做菜游戏逻辑
      Prepare/          阶段 0~1：菜谱书、冰箱、备菜、顾客
      Cook/             阶段 2：CookingManager 主状态机、CookingDropZone、DogManager（提示 NPC）
      CookingRoom/      厨房房间 CookingRoomManager 与 Container
      Ingredient/ Tool/ 食材与工具实体
      Interaction/      加工小游戏（CutDice 切丁、Stir 搅拌）
    DataBase/           数据层
      CookingData/      ScriptableObject：IngredientData / ToolData / RecipeData / CookingRule / RuleBook / KitchenAreaData
    Command/            字符串命令系统（[Command] 特性 + 绑定 SO）
    Controller/         InputManager / SceneSwitchManager / MethodSequencer
    Tools/              ArcPath 曲线路径、Layout 布局、Transition 过渡、MouseParallax 等
    GridPlacement/      网格放置系统
    Dialog/             对话系统
    Spine_Package/      Spine 角色装配
  Editor/               约 30 个自定义 Inspector / EditorWindow（与 Scripts 目录结构对应）
  Resources/            运行时按路径加载的 Prefab（见下方"Resources 路径约定"）
  Scenes/               CookingPrepare / CookingProcess / ToolTests/*
  StreamingAssets/      加密表格数据 .dat（Dialog.dat 等）
```

## 通用交互框架

### 输入管线

`InputManager`（单例，接 Input System 回调）→ `MouseManager`（单例）→ `MouseInteractionLayer` 栈顶层 → `MousePressableObject` 派生类。

`MouseManager` 负责三件事：
- **点击/悬停** —— `Physics2D.OverlapPoint` 对当前层允许的 Layer 做射线，派发 `MouseOver`/`MouseOut`/`MouseSelect`
- **拖拽** —— Input System Hold 交互检测按住，位移超过 `dragMoveThreshold` 后升级为拖拽，转交 `MouseDraggableObject`
- **滚动** —— 累积滚动 + 阈值/冷却

`MouseInteractionLayer` 用 `Stack` 管理，**只有栈顶层接收输入**。可自动管理（`Start`/`OnEnable` 压栈，`OnDisable` 出栈）或手动（`manualStackLayer = true`，调 `OnPushLayer`/`OnRemoveLayer`）。UI 点不动 / 穿透，先查这个栈。

### 交互组件模式

统一的"逻辑 + 视觉组件列表"组合模式，贯穿 Button / Switch / Checkbox / HoverZone：

```
Button_MouseInteract (MousePressableObject)
  ├─ 条件系统: checkAllowEvent → allowToUse；weakCondition 可绕过
  ├─ List<Button_Visual>   视觉反馈（可挂多个）
  ├─ UnityEvents: selectEvent / delayedSelectEvent / overEvent / outEvent / conditonLogicEvent
  └─ isPressing 按压锁，防动画期间重入
```

`Button_Visual` / `Switch_Visual` 抽象基类会自动注册到同物体上的逻辑组件。现有实现：Sprite / Image / ColorSprite / ColorImage / ColorText / Animator / ScaleCurve / RotationCurve / ShowGameObject。**新增视觉表现 = 继承基类，不改逻辑层。**

### 拖拽与放置

- `MouseDraggableObject` —— `startDraggingEvent` / `draggingUpdateEvent` / `endDraggingEvent`
- `DragContainer` —— 完整拖拽容器：Vertical/Horizontal/Composite 模式、自动或手动边界、惯性（速度历史 + 减速曲线）、弹性回弹
- `DropZone` + `DropTag` —— 放置目标与标签匹配
- `SpawnedPlaceable<T>` / `SpawnableIcon` —— 从图标生成可放置实体（`Ingredient_Cooking` 即 `SpawnedPlaceable<Ingredient_Cooking>`）

### 工具库

`Tools`（静态类）—— 鼠标世界坐标、`LoadAndInstantiatePrefab`（`Resources.Load`）、数学工具、泛型 `TryRemoveFromStack<T>`。
`Tools/LayoutTools/` —— `Layout` 基类 + Grid/Horizontal/Vertical/Arc 布局，各配自定义 Inspector。
`Tools/Transition/` —— `TransitionController` + Alpha/Scale/图序列过渡。

## 做菜游戏逻辑

### 场景与跨场景状态

两个正式场景：`CookingPrepare`（阶段 0~1 选菜谱 + 备菜）→ `CookingProcess`（阶段 2 做菜）。

- `SceneSwitchManager`（`DontDestroyOnLoad`）—— 淡入淡出与切场景；`PlayFadesOnly(callback, ...)` 用于不换场景只播过场
- `IngredientInventory`（`DontDestroyOnLoad`）—— 目前唯一跨场景携带的食材状态

> ⚠️ 已知薄弱点：阶段 1 → 阶段 2 的上下文（所选菜谱 + 备好食材）尚未形成可靠统一的传递链路。

### 做菜主状态机（阶段 2）

`CookingManager`（单例）持有 `CookingPhaseStateBase` 状态机：

```
NotCookingState → KitchenChoiceState → IngredientChooseState
                                            ↕ InterruptState / ReviveState
                                       InteractionState（加工小游戏）
                                     → ProcessSeasoningState → CustomerReviewState
```

两套切换语义，改动时务必区分：
- `ChangeState()` —— 普通切换，走 Exit → Enter
- `InterruptState()` / `ReviveState()` —— 压栈式打断，用于加工小游戏这类临时插入。**注意 `InterruptState` 不调用当前状态的 `ExitState`**

其它并列状态机（同一 `EnterState/UpdateState/ExitState(manager)` 基类模式）：`PreparePhaseState`（备菜界面）、`RecipeBookState`（菜谱书翻页）、`CustomerPhaseState`（顾客）、`IngredientFridgeState` / `IngredientTrayState`（冰箱与托盘）。

### 规则判定链路（核心）

放工具触发一次加工判定：

```
Tool_Cooking 落入 CookingDropZone
  → CookingManager.onToolPlacedIn(tool, zone)
      → ResolveRule(tool, zone)
          扫描 currentRecipe.whitelistRules，按 (containerTag, toolTag) 过滤
          → ruleBook.Query(contents, containerTag, toolTag) 精确命中
          → 命中则写入 currentTag
      → 命中: interactionState.Setup(rule, zone, tool)
               SpawnInteraction() 加载 "Cook Interaction/InteractionObjects/{ToolTag}"
               InterruptState(interactionState)
      → 未命中: wrongToolPlacementCount++ ；≥4 次进入"硬做模式"(forcedCookingRule)
  → 小游戏完成 → onInteractionDone(results)
      → 生成产出 Prefab → 销毁 zone 内食材 → 工具归位 → ReviveState()
```

配套三个校验入口，改任一个都要同步看另外两个：
- `validateContainerTag(ingredientData)` —— 厨房可用 tag ∩ Recipe 白名单 tag ∩ 食材可用 tag，**唯一命中才返回**，多命中视为配表问题
- `validateContainerTagForTool(toolData)` —— 基于当前激活 Container 内容 + 工具 ToolTag 扫描白名单
- `GetMaxAllowedCount` / `CanContainerAcceptMoreIngredient` —— 数量上限（通用素材 `isCommonMaterial` 不受限）

手持物统一入口：`onIngredientInHand` / `onToolInHand` / `onItemReleased` → `CookingRoomManager.HandleItem(tag, source)` 决定开哪个 DropZone。

> ⚠️ **架构分歧（重要）**：当前实现以 **Recipe 内白名单** 为核心，而设计文档 B1 v2.0 要求 **全局步骤表 + 菜谱步骤配置**。在现结构上继续加菜谱/关卡会积累返工成本。动这块之前先与用户确认口径。

### Command 系统

`CommandManager`（单例）+ `[Command]` 特性 + `CommandBindingAsset`(SO)，把字符串 key 映射到 C# 方法。
- `Execute(key, ctx)` / `ExecuteString("SetFlag(a,true);AddItem(sword,1)")` / `EvaluateString(...)`（AND 合并布尔结果，空串 = true）
- 实例方法通过 `FindObjectOfType` 解析并缓存
- 编辑器绑定界面：[CommandBindingWindow.cs](Assets/Editor/Command/CommandBindingWindow.cs)

### 数据层

- **ScriptableObject** —— 做菜数据主力（`Assets/Scripts/DataBase/CookingData/`）。`RuleBook` 需先 `Init()` 才能 `Query`（由 `CookingManager.initialize()` 调用）
- **加密表格** —— `DataBaseManager` 从 `StreamingAssets/*.dat` 解密读取（`DataEncryptor` + EPPlus 读 Excel），目前对话数据走这条路
- 加解密工具窗口：[DataEncryptorWindow.cs](Assets/Editor/DataBase/DataEncryptorWindow.cs)

### Resources 路径约定

运行时 `Tools.LoadAndInstantiatePrefab(path, parent)` 按字符串路径加载，**改这些 Prefab 名字会静默断链**：

| 路径 | 内容 |
| --- | --- |
| `Ingredient/{ProcessedIngredientData.name}` | 加工产出食材 |
| `Cook Interaction/InteractionObjects/{ToolTag}` | 加工小游戏控制器 |
| `Prepare/RecipePage/{Recipe.name}` | 菜谱详情页（人工维护，新增菜谱需同步） |
| `Kitchen/` `Tool/` `Customer/` `FridgeIngredient/Icon/` | 厨房、工具、顾客、冰箱图标 |

## Unity MCP 工作流

Editor 实例：`Eat-WhatProject@ad21b8d8a1f6bb9a`。单实例时无需 `set_active_instance`。

推荐节奏：

1. **先读状态** —— `mcpforunity://editor/state`（确认 `data.advice.ready_for_tools = true`、未在编译/Play）、`mcpforunity://editor/selection`、`find_gameobjects`
2. **再动手** —— 场景/GameObject/组件/Prefab/SO/材质一律走 MCP 工具，**不要手改 `.unity` / `.prefab` / `.asset` YAML**
3. **改完验证** —— `refresh_unity` → `read_console`（查编译错误）→ 必要时 `manage_editor` 进 Play Mode → `git diff`

C# 源码可以直接用 Read/Edit/Write 改；改完记得 `refresh_unity` 触发重新编译，再查 Console。

常用资源 URI（注意用斜杠，不是资源名里的下划线）：`mcpforunity://editor/state`、`mcpforunity://project/layers`、`mcpforunity://project/tags`、`mcpforunity://scene/cameras`、`mcpforunity://custom-tools`。

## 参考文档

仓库根目录的分析报告（历史产出，**代码可能已变动，引用前请核实**）：

| 文件 | 内容 |
| --- | --- |
| [做菜短循环设计文档与代码评估_2026-08-11_191051.md](做菜短循环设计文档与代码评估_2026-08-11_191051.md) | **最新最全面**：对照设计文档 B1 v2.0 / F2-a 的完成度评估与差距清单 |
| [食材交互反馈系统分析报告.md](食材交互反馈系统分析报告.md) | 食材交互反馈详细分析（59KB） |
| [CutDice_Refactoring_Analysis.md](CutDice_Refactoring_Analysis.md) / [CutDice_Reference_Template.md](CutDice_Reference_Template.md) | 切丁小游戏重构分析与模板 |
| [ChildrenManagementPatterns_Report.md](ChildrenManagementPatterns_Report.md) | 子物体管理模式梳理 |
| [Ingredient_Interaction_System_Report.md](Ingredient_Interaction_System_Report.md) | 食材交互系统概要 |

设计文档原件在仓库外：`D:/GameProject/养分合作proj/Design/`（F2-a 短循环UI框架、B1 短循环系统设计）。

## 约定

- 单例统一 `Instance` 模式，`Awake` 中重复实例 `Destroy(gameObject)` 后 `return`
- 中文注释为主，`[Header]` / `[Tooltip]` 大量用于 Inspector 分组
- 日志统一带模块前缀：`[CookingManager]`、`[ToolTag]`、`[CookingFSM]`
- UnityEvent 是主要的 Inspector 接线机制；视觉表现用组合而非继承
- 状态机基类模式统一为 `EnterState / UpdateState / ExitState(manager)`
