# 短循环交互测试场景搭建计划

## Context

项目已有基于 GameCriticalLoop.md 设计的骨架 Manager 体系（CookingSessionManager、RecipeManager、InventoryManager、RandomEventManager、EconomyManager、ScoringSystem），以及完整的状态机（CookingSessionStates.cs 中的 5 个 State）。但这些 Manager 全部是 TODO 空壳。需要搭建一个可交互的测试场景，用 Button 驱动短循环状态流转，验证整个 FSM 管线能跑通。

短循环状态流：**接单(Order) → 选替代品(Substitute) → 执行步骤(Execute) → 评分(Scoring) → 存档(SaveVariant)**

## 方案概述

创建一个新场景 `CookingTestScene`，包含：
1. 必要的基础设施 GameObject（InputManager、MouseManager、MouseInteractionLayer）
2. 所有短循环相关的 Manager 单例
3. 一个测试驱动脚本 `CookingTestDriver`，负责初始化假数据并响应按钮点击推进状态
4. UI 层：一个"下一步"按钮 + 状态信息文本显示

## 需要创建的文件

### 1. `Assets/Scripts/CookingSession/CookingTestDriver.cs` — 测试驱动脚本

职责：
- 持有对 `CookingSessionManager` 的引用
- 在 `Start()` 中用硬编码假数据初始化一个 RecipeData（3个步骤），调用 `StartSession`
- 提供 `public void OnNextStepClicked()` 方法，供按钮的 `selectEvent` 绑定
- `OnNextStepClicked` 根据当前 `CookingSessionManager.sessionPhaseVisual` 推进到下一个状态
- 持有一个 `TMP_Text` 引用，每次状态变化时更新显示当前阶段名称、步骤索引、假评分等信息

状态推进逻辑：
```
Order → 点击 → ChangeState(substituteState)
Substitute → 点击 → ChangeState(executeState)
Execute → 点击 → 如果还有下一步骤: currentStepIndex++ → ChangeState(substituteState)
                  如果所有步骤完成: ChangeState(scoringState)
Scoring → 点击 → ChangeState(saveVariantState)
SaveVariant → 点击 → 显示"流程完成"，可选重新开始
```

### 2. 修改现有文件

#### `Assets/Scripts/CookingSession/CookingSessionManager.cs`
- 实现 `StartSession(int recipeId)`：初始化 `currentRecipeId`、`currentStepIndex = 0`，实例化各 State 对象，`ChangeState(orderState)`，触发 `OnSessionStart` 事件
- 实现 `EndSession()`：`ChangeState(null)`，触发 `OnSessionEnd` 事件

#### `Assets/Scripts/CookingSession/CookingSessionStates.cs`
- 各 State 的 `EnterState` 中添加 `Debug.Log` 输出当前阶段信息（已有，保持不变）
- 不需要在 State 内部做实际逻辑，状态切换由 `CookingTestDriver` 外部驱动

### 3. 场景搭建（通过 MCP 工具）

场景层级结构：
```
CookingTestScene
├── Main Camera (Camera, 已有)
├── --- Infrastructure ---
│   ├── InputManager (InputManager + PlayerInput 组件)
│   └── MouseManager (MouseManager)
├── --- Managers ---
│   ├── CookingSessionManager (CookingSessionManager)
│   ├── RecipeManager (RecipeManager)
│   ├── InventoryManager (InventoryManager)
│   ├── RandomEventManager (RandomEventManager)
│   └── EconomyManager (EconomyManager)
├── --- Test UI ---
│   ├── MouseInteractionLayer (MouseInteractionLayer, 自动入栈)
│   ├── NextStepButton (SpriteRenderer + BoxCollider2D + Button_MouseInteract + ScaleCurveButton_Visual)
│   │   └── ButtonLabel (TextMeshPro: "下一步")
│   ├── StatusText (TextMeshPro: 显示当前状态)
│   └── CookingTestDriver (CookingTestDriver, 引用 CookingSessionManager + StatusText)
```

按钮使用项目已有的 `Button_MouseInteract` + `ScaleCurveButton_Visual` 组合，通过 `selectEvent` 绑定 `CookingTestDriver.OnNextStepClicked()`。

## 关键文件路径

| 文件 | 操作 | 说明 |
|------|------|------|
| `Assets/Scripts/CookingSession/CookingTestDriver.cs` | 新建 | 测试驱动脚本 |
| `Assets/Scripts/CookingSession/CookingSessionManager.cs` | 修改 | 实现 StartSession/EndSession |
| `Assets/Scripts/CookingSession/CookingSessionStates.cs` | 不改 | 保持现有骨架 |
| `Assets/Scenes/CookingTestScene.unity` | 新建 | 通过 MCP scene-create |

## 需要复用的现有代码

- `Button_MouseInteract`（[Assets/Scripts/MouseInteractive/Button/Button_MouseInteract.cs](Assets/Scripts/MouseInteractive/Button/Button_MouseInteract.cs)）— 按钮交互核心
- `ScaleCurveButton_Visual`（[Assets/Scripts/MouseInteractive/Button/Visuals/ScaleCurveButton_Visual.cs](Assets/Scripts/MouseInteractive/Button/Visuals/ScaleCurveButton_Visual.cs)）— 按钮缩放视觉反馈
- `MouseInteractionLayer`（[Assets/Scripts/MouseInteractive/MouseInteractionLayer.cs](Assets/Scripts/MouseInteractive/MouseInteractionLayer.cs)）— 鼠标交互层
- `MouseManager`（[Assets/Scripts/MouseInteractive/MouseManager.cs](Assets/Scripts/MouseInteractive/MouseManager.cs)）— 鼠标管理器
- `InputManager`（[Assets/Scripts/Controller/InputManager.cs](Assets/Scripts/Controller/InputManager.cs)）— 输入管理器
- `CookingSessionManager` 的 FSM 框架（ChangeState、Update 转发）
- `CookingPhaseStateBase` 及 5 个具体 State 类

## 执行步骤

1. 创建 `CookingTestDriver.cs`
2. 修改 `CookingSessionManager.cs`（实现 StartSession/EndSession + 初始化 State 实例）
3. 通过 MCP 创建新场景 `Assets/Scenes/CookingTestScene.unity`
4. 通过 MCP 在场景中创建 GameObject 层级
5. 通过 MCP 添加组件并配置引用
6. 保存场景

## 验证方式

1. 在 Unity Editor 中打开 `CookingTestScene`
2. 进入 Play Mode
3. 点击"下一步"按钮，观察：
   - Console 中 Debug.Log 输出各阶段的 EnterState/ExitState
   - StatusText 显示当前阶段名称和步骤信息
   - 按钮有 ScaleCurve 视觉反馈
4. 完整走完 Order → Substitute → Execute (×3步骤循环) → Scoring → SaveVariant → 完成
5. 确认 `OnSessionStart` 和 `OnSessionEnd` 事件正确触发

## 程序流程图

```mermaid
flowchart TD
    Start([游戏开始]) --> Init["CookingTestDriver.Start()<br/>初始化假数据<br/>注入 RecipeData / InventoryItems"]
    Init --> Idle["等待玩家点击「下一步」"]

    Idle -->|点击按钮| CheckActive{sessionActive?}

    CheckActive -->|false| StartSession["StartSession(recipeId)<br/>初始化 5 个 State 实例<br/>currentStepIndex = 0<br/>触发 OnSessionStart"]
    StartSession --> OrderState["ChangeState → OrderState<br/>接单阶段"]
    OrderState --> WaitClick1["等待点击"]

    CheckActive -->|true| ReadPhase["读取 sessionPhaseVisual"]

    ReadPhase -->|Order| ToSub["currentStepIndex = 0<br/>ChangeState → SubstituteState"]
    ToSub --> SubState["选替代品阶段<br/>显示当前步骤 & 可用替代品"]
    SubState --> WaitClick2["等待点击"]

    ReadPhase -->|Substitute| ToExec["ChangeState → ExecuteState"]
    ToExec --> ExecState["执行步骤阶段<br/>模拟随机事件"]
    ExecState --> WaitClick3["等待点击"]

    ReadPhase -->|Execute| CheckStep{"currentStepIndex + 1<br/>< totalSteps?"}
    CheckStep -->|是: 还有下一步| NextStep["currentStepIndex++<br/>ChangeState → SubstituteState"]
    NextStep --> SubState
    CheckStep -->|否: 所有步骤完成| ToScore["ChangeState → ScoringState"]
    ToScore --> ScoreState["评分阶段<br/>计算还原度/味道/卖相"]
    ScoreState --> WaitClick4["等待点击"]

    ReadPhase -->|Scoring| ToSave["ChangeState → SaveVariantState"]
    ToSave --> SaveState["存档阶段<br/>保存食谱变体"]
    SaveState --> WaitClick5["等待点击"]

    ReadPhase -->|SaveVariant| EndSession["EndSession()<br/>ChangeState(null)<br/>触发 OnSessionEnd"]
    EndSession --> Done["显示「流程完成」<br/>sessionActive = false"]
    Done --> Idle

    WaitClick1 --> CheckActive
    WaitClick2 --> CheckActive
    WaitClick3 --> CheckActive
    WaitClick4 --> CheckActive
    WaitClick5 --> CheckActive

    style Start fill:#4CAF50,color:#fff
    style Done fill:#FF9800,color:#fff
    style OrderState fill:#2196F3,color:#fff
    style SubState fill:#9C27B0,color:#fff
    style ExecState fill:#F44336,color:#fff
    style ScoreState fill:#00BCD4,color:#fff
    style SaveState fill:#795548,color:#fff
    style CheckStep fill:#FFC107,color:#000
```

## 程序流程图

```mermaid
flowchart TD
    Start([游戏开始]) --> Init["CookingTestDriver.Start()<br/>初始化测试数据<br/>注入 RecipeData + InventoryItems"]
    Init --> Idle["等待玩家点击<br/>「下一步」按钮"]

    Idle -->|"点击按钮<br/>OnNextStepClicked()"| CheckActive{sessionActive?}

    CheckActive -->|No| StartSession["StartSession(recipeId)<br/>初始化 5 个 State 实例<br/>currentStepIndex = 0<br/>触发 OnSessionStart"]
    StartSession --> OrderState

    CheckActive -->|Yes| ReadPhase["读取当前阶段<br/>sessionPhaseVisual"]

    ReadPhase --> SwitchPhase{当前阶段?}

    %% Order
    SwitchPhase -->|Order| OrderState["🟡 接单阶段<br/>CookingOrderState.EnterState()<br/>显示食谱信息"]
    OrderState --> WaitClick1["等待点击"]
    WaitClick1 -->|点击| ToSub["ChangeState(substituteState)<br/>currentStepIndex = 0"]

    %% Substitute
    ToSub --> SubState["🔵 选替代品阶段<br/>CookingSubstituteState.EnterState()<br/>显示当前步骤 + 可用替代品"]
    SubState --> WaitClick2["等待点击"]
    WaitClick2 -->|点击| ToExec["ChangeState(executeState)"]

    %% Execute
    ToExec --> ExecState["🟢 执行步骤阶段<br/>CookingExecuteState.EnterState()<br/>模拟执行 + 随机事件判定"]
    ExecState --> WaitClick3["等待点击"]
    WaitClick3 -->|点击| CheckSteps{"currentStepIndex + 1<br/>< totalSteps?"}

    CheckSteps -->|"Yes: 还有步骤"| IncStep["currentStepIndex++<br/>ChangeState(substituteState)"]
    IncStep --> SubState

    CheckSteps -->|"No: 所有步骤完成"| ToScoring["ChangeState(scoringState)"]

    %% Scoring
    ToScoring --> ScoringState["🟠 评分阶段<br/>CookingScoringState.EnterState()<br/>计算三维评分:<br/>还原度 / 味道 / 卖相"]
    ScoringState --> WaitClick4["等待点击"]
    WaitClick4 -->|点击| ToSave["ChangeState(saveVariantState)"]

    %% SaveVariant
    ToSave --> SaveState["🟣 存档阶段<br/>CookingSaveVariantState.EnterState()<br/>保存食谱变体"]
    SaveState --> WaitClick5["等待点击"]
    WaitClick5 -->|点击| EndSession["EndSession()<br/>ChangeState(null)<br/>触发 OnSessionEnd<br/>sessionActive = false"]

    EndSession --> Idle

    %% 样式
    style Start fill:#4CAF50,color:#fff
    style OrderState fill:#FFC107,color:#000
    style SubState fill:#2196F3,color:#fff
    style ExecState fill:#4CAF50,color:#fff
    style ScoringState fill:#FF9800,color:#fff
    style SaveState fill:#9C27B0,color:#fff
    style EndSession fill:#f44336,color:#fff
```
