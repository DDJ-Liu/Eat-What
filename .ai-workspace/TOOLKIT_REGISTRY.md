# 可复用工具库登记表

2026-09-17 阶段状态：本轮145项已关闭，用户人工验收全部通过。表内旧任务的“待人工/待复验”等保留为来源语境，当前结算以队列与项目基线为准；详细版本变化已归档，不再逐条追加页首日志。

## 0. 条目模板与状态词

| 字段 | 说明 |
|---|---|
| ID | `TK-<域>-<序号>`：域 = INT 交互 / MOT 运动过渡 / LAY 排布 / RND 渲染 / DAT 数据 / LOC 本地化 / DBG 调试审计 / CMD 命令 |
| 名称 / 路径 | 类名 + 相对 `Assets/Scripts/` 路径 |
| 能力（一句话） | 只写能力，不写决策 |
| 公开面 | 关键方法 / 事件 / Inspector 字段 |
| 状态 | `沿用` 直接消费 · `扩展中` 有登记的扩展需求 · `改造` 保留思路重写 · `规划中` 未建 · `冻结` 保留不消费 · `弃用` |
| 消费方 | 已接入的模块/任务；`—` 表示尚无 |
| 来源 | 引入或最近变更的任务号 |
| 备注 | 使用约束、坑账本引用 |

**规则一句话**：状态为 `沿用` 的条目是实现同等能力的默认唯一构件（手册 C2）；需要能力先查本表，再决定复用 / 扩展 / 新建；新建或扩展必须回写本表。

---

## 1. INT · 交互（MouseInteractive 框架栈）

| ID | 名称 / 路径 | 能力 | 公开面 | 状态 | 消费方 | 来源 | 备注 |
|---|---|---|---|---|---|---|---|
| TK-INT-01 | `MouseManager` `MouseInteractive/` | 鼠标仲裁：按层 OverlapPointAll，分发按压/拖拽/投放/滚动/取消 | 由 InputManager 驱动；只读阈值/冷却、层栈安全、滚轮 raw/step/帧/序号/指针、候选/阻挡者/解析原因与派发帧 | 沿用·扩充 | ShortCycle 全部交互；TK-DBG-07 观测 | CK01-A #13 / AI-000075 / AI-000113 | `Update` 先判 null/空栈再 `Peek()`；滚轮只在当前胜出层解析，未改变 CameraPan/InputLock 所有权；Pressable 穿透仍由目标的显式同 scope 契约决定 |
| TK-INT-02 | `MouseInteractionLayer` | 交互层，Push/Remove 控制允许命中的 Physics2D 层 | `onClickNullCancel` | 沿用 | Layer_Phase0/Phase1/Modal | CK01-A #14 | 栈顶=当前可响应层（A4-1） |
| TK-INT-03 | `Button_MouseInteract` + Visuals（ColorSprite / ScaleCurve / RotationCurve / ShowGameObject / Sprite） | 点击/悬停按钮及五种视觉反馈 | `selectEvent` `delayedSelectEvent` `overEvent` `outEvent` `conditonResultEvent` | 沿用 | 13 具名按钮（C-T5a） | CK01-A #17~22 | 事件→MouseActionBinding→ActionRouter，不直连业务 |
| TK-INT-04 | `MouseDraggableObject` | 拖拽物：开始/更新/结束/取消，投放命中与落空 | `startDraggingEvent` `onDropHitEvent<DropZone>` `onDropMissedEvent` `InteractionBoundsMode` | 改造 | —（段4 F/G） | CK01-A #25 | 需补"同类优先叠放/右键回原区/单件拆分"指令层 |
| TK-INT-05 | `DropZone` | 投放区：接受/拒绝 | `onDropEvent` `onDropRejectedEvent` | 改造 | —（段4） | CK01-A #26 | 容量/状态校验由消费方做 |
| TK-INT-06 | `DragContainer` / `DragLimit` | 可拖拽视图容器与位置限制 | `moveToTargetPos(Vector3 worldTarget)` `SyncColliderFromRect()` | 改造 | ScrollArea；ScrollBar | CK01-A #23/24 / AI-000086 | 内部拖拽/惯性/弹性位置与 DragLimit 世界边界在唯一限制入口成对转换；世界几何中心和 `lossyScale` 半尺寸与 UpdateBounds 一致；Manual/三轴模式保留，双滚动区其余边界仍按原规划 |
| TK-INT-07 | `HoverTrigger` | 悬停进入/离开 | `overEvent` `outEvent` | 改造 | —（T-06 HoverCard） | CK01-A #27 | 冰箱↔线索板双向高亮预留 |
| TK-INT-08 | `HoverZone_MouseInteract` + `HoverZone_Logic`（ToggleLock） | 带进度/条件的悬停区 | `hoverProgressEvent<float>` `hoverTriggerEvent` `conditionLogicEvent` | 沿用 | — | 代码包 | 长按式悬停确认候选 |
| TK-INT-09 | `MouseHoldObject` | 长按：开始/更新/完成/取消/释放，`HoldTriggerMode` | 五事件 | 沿用 | — | 代码包 | |
| TK-INT-10 | `MouseScrollableObject` + `ShortCycleScrollAdapter` | 滚轮步进事件经 Phase1 门控进入唯一具名动作路由；可选物理方向适配与同 scope Pressable 穿透 | `scrollStepEvent<float>`；`Dispatch(float)`；`MapPhysicalStep(float)`；默认关闭 `invertPhysicalScrollDirection` / `allowScrollThroughPressables`，后者需 `pressablePassThroughScope` | 沿用·扩充/正式P0P1显式启用穿透与方向反转 | FridgeCat ScrollRegion → ActionRouter → SessionManager → FridgeBinder；TK-DBG-07 观测 | AI-000068 / AI-000075 / AI-000113 / AI-000114 / AI-000138 / AI-000139 | `MouseManager` 发出的物理 step 先归一为 ±1，仅 `MapPhysicalStep` 可按实例把符号再翻一次；`Dispatch(+1)=next` / `Dispatch(-1)=previous` 始终不受该开关影响。P0P1 仅对同属 `FridgeCat_Group` 的 Pressable 显式放行，并将物理方向开关保存为 true；无关 scope 仍阻挡。Lab 默认 false 继续作为对照。通用接入见 `组件说明文档/区域滚动_配置与复用说明.md` |
| TK-INT-11 | `ScrollArea_Controller` | 与业务无关的区域滚动数学：Vertical/Horizontal/Composite，连续或离散步进，程序滚动消费 Transition Position | `Initialize()` `ScrollTo(Vector2)` `ScrollVerticalTo` `ScrollHorizontalTo` `OnScrollStep`；显式 `contentDragContainer/content/viewportDragLimit`；只读 `ContentTransform/NormalizedContentPosition/LastTargetPosition/IsProgrammaticTransitionActive` | 沿用 | 普通区域滚动；FridgeCatRig；TK-DBG-07 观测 | AI-000063 / AI-000075 / AI-000083 / AI-000085 / AI-000138 | 输入/输出单位均为归一化 0~1；Y 轴 0=LimitMinY、1=LimitMaxY，X 轴因内容反向移动为 0=LimitMaxX、1=LimitMinX。`OnScrollStep` 以 `current-direction*step` 计算并钳制；首次 Initialize 前只保留最新请求，最终 bounds 后应用一次；Transition 缺失或宿主 inactive 时同步落点，禁用/销毁只完成最新请求一次。类本身不依赖冰箱、Session 或 ActionRouter；必要挂载和禁忌见通用配置说明 |
| TK-INT-12 | `ScrollBar_Controller` | 可见滚动条 | — | 冻结 | — | CK01-A #30 | 新视觉不需要则不接 |
| TK-INT-13 | `KeyboardListener` / `KeyboardHoldBridge` | 键盘按下/释放事件、键盘转长按 | `keyPressEvent` `keyReleaseEvent` | 沿用 | ManualVerificationDriver（经 InputManager.OnKeyPressed） | 代码包 | 键盘映射另有 D-003 文档 |
| TK-INT-14 | `ConditionReceiver` | 条件门：`checkAllowEvent` 汇总允许/拒绝 | `ConditionMode` | 沿用 | Button 条件 | 代码包 | A4-4 门控可复用其模式 |
| TK-INT-15 | `Checkbox` / `Switch` / `DropDown` | 复选/开关/下拉 | — | 沿用 | — | 代码包 | 筛选栏候选（T-03 UI 侧） |
| TK-INT-16 | `ShortCycleMouseActionBinding` `Cooking/ShortCycle/` | 框架事件→具名动作（含 `recipeId` 参数） | `actionName` `recipeId` | 沿用 | 13 按钮 | C-T4 / C-T8 | 通用化候选：去 ShortCycle 前缀迁 Tools |
| TK-INT-17 | `ShortCycleInteractionLayerStack` | 层栈推拉封装 | `Push` / `Pop` / `Restore` | 沿用 | `ShortCyclePresentationDirector` 的 Space/Modal 接管 | K-T2b / K-T3 | Director 为唯一业务调用方；场景旧 G26 自订阅已撤销 |
| TK-INT-18 | `ShortCycleStateGatedInteraction` | 按所属 PresentationSet/FSM 状态启停并恢复 MouseInteract，可叠加已选菜条件 | `SetInteractionAllowed(bool)`；Inspector `sessionManager` / `requireSelectedRecipe` | 沿用 | 既有具名按钮 Gate；`shortcycle.close_catalog` 已落地选菜门禁 | K-T2 / K-T3 / AI-000071 / AI-000072 | Presentation 门禁为外层条件；选菜条件读取唯一 Session Context；AI-000072 已实测空选菜拒绝、选菜后可返回 Browse；禁用/销毁恢复原状态 |

## 2. MOT · 运动与过渡

| ID | 名称 / 路径 | 能力 | 公开面 | 状态 | 消费方 | 来源 | 备注 |
|---|---|---|---|---|---|---|---|
| TK-MOT-01 | `TransitionController` `Tools/Transition/` | 命名转场 + 回调 | 按名触发 | 沿用 | SceneSwitchManager（P1→P2） | CK01-A #10 | 不用于 P0↔P1 镜头 |
| TK-MOT-02 | `TransitionBehaviour` 基类 + `_Alpha` / `_Scale` / `_ImageSequence` / `_SpriteSequence` / `_Position` / `_Rotation` `Tools/Transition/` | 命名条目驱动的逐属性协程过渡，统一 from/to、scaled/unscaled 时间源与完成/取消语义 | `PlayTransition(name[, onComplete])` `StopActiveAnimation()` `Cancel()`；只读 `IsAnimating` | 沿用 | 封面/modal；ClueBoard 双锚点迁移；TK-DBG-07 观测 | K-T2 / K-T3 / AI-000075 | Position 的锚点在播放开始快照；`IsAnimating` 只暴露既有协程/回调状态，不改变取消与完成语义 |
| TK-MOT-03 | `ArcPathController` + `ArcPathComponent` + FollowArcPath_* | 弧线路径运动：维度/模式/返回/中断/起点五枚举，起止事件 | `onStartMoving` `onReachEnd` | 沿用 | — | 代码包（939 行） | 线索板搬移曲线候选；勿为镜头平移改造 |
| TK-MOT-04 | `CameraFollower` | 相机跟随 | — | 冻结 | — | 代码包 | P0-P1 镜头为确定性横移，不用 |
| TK-MOT-05 | `MouseParallax` | 鼠标视差 | — | 弃用 | — | CK01-A #35 | 不混入镜头状态 |
| TK-MOT-06 | `ShortCycleCameraPanController` `Cooking/ShortCycle/` | 三机位横移 + 输入锁 + PanStarted/PanCompleted | `PanTo(slot)` `CurrentSlot` `CameraX` 两事件 | 沿用 | Session 过渡 | C-T2 / C-T8 / AI-000136 / AI-000143 | 内部 `PanRoutine` 自写 Lerp+curve；活动 Pan 先取消，同槽位即时完成还须相机真实位于 Marker；中途返向/重复/禁用后 off-marker 请求均从当前位置建立唯一新 Pan，取消项不回调或晚到。null/0-key 线性回退、合法用户曲线保持，非正/非有限时长立即安全落终点；二期改消费 TK-MOT-07 `_Position`，外壳（机位/输入锁/事件）保留 |
| TK-MOT-07 | **Transition 家族扩充**（替代原“ProceduralMotion 新建”方案） | TK-MOT-02 的 Position/Rotation 与 `TransitionPreset` 已落地；Preset 覆盖位置/缩放/alpha/子件显隐并从当前值插值 | `PlayTransition(name, onComplete)` `GoTo(presetName)` `Cancel()` `CapturePreset()` | 沿用 | ClueBoard Traveler；Tray Collapsed/Highlight/Detail | K-T2 / K-T3 | K-T3 已完成真实场景消费；Transition/协程宿主必须常驻，游走元件不得把搬移协程挂在会被 `SetActive(false)` 的 ViewSet 内；镜头二期/翻页/卡片翻转/拖拽回弹仍是后续可选消费 |
| TK-MOT-08 | `FridgeCatBlinkScheduler` + `FridgeCatEye` | 单调度器按 3~5 秒同步请求两只独立分层眼；每眼用 TK-MOT-07 Preset 闭合/睁开 | `StartBlinkScheduling()` `StopBlinkScheduling()` `RequestBlink()`；`Blink()` `LookAt(Vector2)` | 沿用 | 冰箱猫组合骨架 | AI-000063 | 不依赖 Animator；两眼必须同时有显式引用，重复 Blink 取消前一 Preset 协程后重启 |
| TK-MOT-09 | `ScaleLerp` `Tools/` | 单段 start/end 缩放插值 + onComplete，autoStart | `StartLerp()` | 冻结 | 旧场景（待查） | 代码包 | 与 TK-MOT-02 `_Scale` 重叠；TK-MOT-07 落地后并入并弃用 |
| TK-MOT-10 | Button/Switch Visuals：`ScaleCurve` / `RotationCurve` / `ShowGameObject` / `Animator` | 控件内置 idle/highlight/press 曲线动画 | 继承 `Button_Visual` | 沿用 | 按钮视觉件 | CK01-A #19/20 | 控件专用，不合并进 Transition |
| TK-MOT-11 | `DragContainer` 惯性/弹性回弹 | `inertiaCurve` + Lerp 回弹 | 内嵌 | 沿用（内嵌） | 拖拽容器 | CK01-A #23 | 专用；拖拽物落空回弹另走 TK-MOT-07 |
| TK-MOT-12 | `ShortCycleScenePresenter.MoveClueBoardRoutine` | 线索板锚点间 SmoothStep 移动（unscaledTime） | 私有协程 | 改造 | Presenter | C-T2 | 迁 TravelingElement 并改消费 TK-MOT-07 `_Position` |
| TK-MOT-13 | 旧 Animator 表现：`IngredientInventoryTray`（Open/Close Trigger）/ `RecipeBookManager` 翻页 / `Tools.AnimatorHasState` | Animator 状态驱动 | Trigger | 冻结 | 旧场景 | CK01-A #2/#4 | 拍板 4：三态不走 Animator；翻页 Animator 待 D 段裁决 |

## 3. LAY · 排布

2026-09-07 AI-000064 消费状态增补：TK-LAY-08 的 `FridgeCatTier` 已独立同名 MonoScript 文件，Rig/Tier 唯一 Prefab 位于 `Assets/Prefabs/Cooking/ShortCycle/FridgeCat/` 并接入 ShortCycle_P0P1；第三层池化复用和按层程序位移已测。TK-MOT-08 同目录 Rig/Eye 接线后 15 秒双眼同步 5 次已测，继续消费 TK-MOT-07 与 TK-INT-11；未新增 API。层距/眼位/接缝、实际滚轮和 Ear/Arm/Tail 动画选择交最终人工段，不以机器采样替代视觉验收。

| ID | 名称 / 路径 | 能力 | 公开面 | 状态 | 消费方 | 来源 | 备注 |
|---|---|---|---|---|---|---|---|
| TK-LAY-01 | `Layout` 基类 + `GridLayout` / `HorizontalLayout` / `VerticalLayout` / `ArcLayout` / `ArcLayoutStrategy` + Editor `Tools/LayoutTools/` | 规则阵列点位生成与管理，`ChildSizeMode`；Grid 有约束/对齐/起角/轴向枚举 | 见各类枚举 | 沿用 | 目录 8 卡 / 格架列 / 食材清单 / 步骤行 / 筛选栏 | 指导书 §五 | **托盘 20 锚点与区块 8 锚点手工定点，不用工具**（R-05） |
| TK-LAY-02 | `ShortCycleTrayAnchorLayout` `Cooking/ShortCycle/` | 20 手工锚点按序占用/释放 | 见类 | 沿用 | Tray_Group | C-T6 v2.2 | 通用化候选：AnchorSlotLayout |
| TK-LAY-03 | `ShortCycleIconScaleTarget` + `ShortCycleV22LayoutContracts` | 图标七类画布统一缩放常量 | 常量表 | 沿用 | 六 prefab | C-T6 v2.2 | 512 画布规则（G19） |
| TK-LAY-04 | `GridPlacement`（×3） `GridPlacement/` | 占位网格：cell 状态、多格、旋转 | — | 冻结 | —（P2 / 长循环） | 总档 A-6 | 复用裁决改【沿用·保留】 |
| TK-LAY-05 | `IgnoreParentScale` / `IgnoreParentRotation` | 子对象抵消父级缩放/旋转 | — | 沿用 | 猫伸展下图标稳定 | CK01-A #33/34 | |
| TK-LAY-06 | **`TidyLayout`（拟）** | 可见集合按规则重排到点位 | 拟 | 规划中 | 一键整理（B1 3.9） | 框架 v0.2 T-04 | 消费 TK-LAY-01 |
| TK-LAY-07 | **`StackedIcon`（拟）** | 四种堆叠形态按数量切换 | 拟 | 规划中 | 冰箱格件（F2-a 5.4） | T-08 | |
| TK-LAY-08 | `FridgeCatRig` + `FridgeCatTier` `Cooking/ShortCycle/Presentation/` | 固定层 + 池化扩展层的五列 Shelf 锚点排布与按层滚动；可选按真实 Anchor 间距对齐并自动拟合纵向 Content 边界 | `SetTierCount(int)` `GetShelfAnchor(row,col)` `ScrollToTier(int)` `TierCount`；序列化 `tierScrollMapping` / `tierAlignmentTarget_TUNE` / `tierAlignmentColumn_TUNE` | 沿用·扩充 | ShortCycleFridgeBinder；CAT-T2 Prefab 接线 | AI-000063 / AI-000128 | 默认 `UniformTierIndex` 保持旧行为；`AnchorAlignedAutoBounds` 需固定 Target、Vertical/BoxCollider 接线和严格下降 Anchor，按真实世界跨度及非单位缩放求解；0/1层安全，扩展层回收为 inactive，不销毁；正式P0启用/六层实测归AI129 |

## 4. RND · 渲染

| ID | 名称 / 路径 | 能力 | 公开面 | 状态 | 消费方 | 来源 | 备注 |
|---|---|---|---|---|---|---|---|
| TK-RND-01 | `SpriteOutline2D` `Rendering/` | 单 Sprite 描边/阴影、参数本地或跟随最近组、独立三态合并请求与唯一渲染所有者切换 | 旧接口保留；`OverrideGroup`、`MergeOutline/Shadow`、`NearestGroup`、`MergeAdapter`、`ResolveActualMergeOwnership`；`Outline Color` 为 HDR RGB + 0～1 Alpha | 沿用·v2正式后端接线源码完成 | 可交互件焦点；TK-RND-02/07 | CK01-A #31 / AI-000052 / AI-000091 / AI-000096 / AI-000140 | 后端真实就绪才逐效果抑制旧MPB输出，失效恢复；Reset只读默认一次，不分配材质；组覆盖会同步覆盖成员本地样式 |
| TK-RND-02 | `SpriteOutlineGroup2D` + `ShortCycleOutlineGroupBinding` | 最近祖先组只同步跟随件；显式创建/同步/列成员和同一后端adapter接线；hover快照恢复 | 旧接口保留；`CreateOrSynchronizeMergeAdapters`、`GetOwnedMergeAdapters`、`SynchronizeMergeBackendConfiguration` | 沿用·生产迁移机器门通过 | 线索板/工具组合焦点；P0P1/GridPieces/FridgeCat；TK-RND-07 | CK01-A #32 / C-T6 v2.2 / AI-000052 / AI-000091 / AI-000096 / AI-000095 | 不在 OnEnable/层级变化创建；`_Merged*`不入成员；参数源与三态分离；P0P1 仅 CatRig 保留组，局部件 hover 恢复已验证 |
| TK-RND-03 | `SceneColorCapture2D` `Tools/Rendering/` | 显式双相机/LayerMask 的当帧后景捕获，每主相机唯一自有 RT，状态恢复与释放 | `Configure` `RefreshCaptureState`；`CapturedTexture/IsFrameValid/OwnedRenderTextureCount`；RenderScale/AA | 沿用·Lab运行已验证 | VisualEffectsLab；TK-RND-04 | AI-000079 / AI-000081 | 不用 Camera.Render/GrabPass/全局 Shader 参数；Lit 后景与 GlobalLight2D 必须同在捕获层；T3 实测单 RT、相机同步、退出释放，玩家构建预算仍未测 |
| TK-RND-04 | `EtherBubbleDistortion2D` + `EtherBubbleEmitter2D` + `EtherBubbleDistortion2D.shader` | 局部后景折射、径向波/流动/边缘色与有界池化发射 | `Emit/Burst/Clear/StartEmission/StopEmission/ApplyPreset/SetPaused/SetEffectEnabled`；Soft/Obvious | 沿用·Lab运行已验证 | VisualEffectsLab | AI-000079 / AI-000081 | 共享 TK-RND-03 的 RT；每实例合并 PropertyBlock；1/8/16、暂停/重入/边缘/效果开关已通过，用户画风签收待完成 |
| TK-RND-05 | `SpriteOutlineDefaults` `Rendering/` | 单一通用/角色描边默认表，供组件 Reset 或显式迁移一次性读取 | `LoadDefault`、`TryGetStyle`、`ConfigureGeneric`、`SetRole`、`ApplyTo`；Resources 路径 `SpriteOutlineDefaults`；`Outline Color` 为 HDR RGB + 0～1 Alpha | 沿用·源码与默认资产已完成 | TK-RND-01/02；ShortCycle旧表导出 | AI-000091 / AI-000093 / AI-000140 | 不在 OnEnable/OnValidate/Play 写回；AI93 一次性创建 `Assets/Resources/SpriteOutlineDefaults.asset`，Generic 复用既有材质/厚度6/阴影关，重复入口不覆盖手调值 |
| TK-RND-06 | `SpriteOutline2DEditor` + `SpriteOutlineGroup2DEditor` `Editor/Rendering/` | 显示参数源、生效色宽、请求/实际合并、后端引用/宿主/预算；诊断ForceOff独立绘制的材质/alpha/宽度/透明留白前置、组覆盖与旧Adapter双权威 | Inspector按钮；HDR `Outline Color` 显示 Alpha；Override Group 覆盖提示；Undo/dirty；`IsReadyFor`、`ExportBudgetState` | 沿用·HDR Alpha GPU机器门通过/UI待人工 | TK-RND-01/02/07 | AI-000091 / AI-000096 / AI-000093 / AI-000094 / AI-000111 / AI-000112 / AI-000140 / AI-000141 | AI141 已完成 0/0.5/1、HDR、组/本地/三态、Defaults保存重载、Hover恢复、2RT/Stop 与 P0 兼容；正常 Inspector 拾取器因窗口捕获失败仍待用户确认 |
| TK-RND-07 | `SpriteOutlineMergeRenderer2D` + `SpriteOutlineMergeMember2D` + `SpriteOutlineMerge2D.shader` `Tools/Rendering/` | 逐组实体/阴影 alpha 并集与成员描边候选的显式两 RT 合成后端 | 原公开面加 `ConfigureFormal`、`ConfigureFormalGroup`、`ReplaceMembersIfChanged`、`IsReadyFor`；参数源独立字段 | 沿用·Lab与P0P1生产机器门通过 | VisualEffectsLab；P0P1 FridgeCat；OUT-T1 v2 组件 | AI-000097 / 098 / 099 / 101 / 102 / 103 / 104 / 096 / 094 / 105 / 095 / 106 / 107 | 最多2张持久ARGB32 AA1 RT；源Rig上限8192，P0P1默认2008×4168双RT/63.85MiB、18成员；Lab正式实例的场景added Backend独立设8192，默认7584×6104双RT/353.19MiB、14成员；两处owned/peak=2/2，人工画风/接缝签收后置 |

## 5. DAT · 数据

| ID | 名称 / 路径 | 能力 | 公开面 | 状态 | 消费方 | 来源 | 备注 |
|---|---|---|---|---|---|---|---|
| TK-DAT-01 | `CK01GeneratedDataCatalog` + 导表器（Editor） | 十四表聚合入口、稳定 ID 与 v2.4 约束/容量只读解析 | `TryGetRecipe` `TryGetInitialInventory` `TryGetRecipeWhitelist` `TryGetAreaConstraint` `TryGetUnknownProductResolver` `TryGetDemoFridgeCapacity` `UnknownProductItemId` `DemoFridgeCapacityLevel` | 沿用 | 全部 Binder；后续 P2 | C-T4B / B-T5 / AI-000077 / AI-000082 | **唯一**数据入口（G8）；新增属性复用既有 Try 链，缺失/无效配置显式抛错且无硬编码兜底；RecipeWhitelist/AreaConstraint/UnknownProductResolver 当前仅数据面，不实施 P2 行为 |
| TK-DAT-02 | `SerializableDictionary<K,V>` `DataBase/` | 可序列化字典 | — | 沿用 | — | CK01-A | |
| TK-DAT-03 | `DataEncryptor` | 存档加密 | — | 沿用 | —（SaveBoundary） | CK01-A | |
| TK-DAT-04 | `DataBaseManager` / SpreadsheetData | 旧配表体系 | — | 弃用 | 旧场景 | CK01-A #16 | 禁止新增依赖 |
| TK-DAT-05 | **`TagFilter`（拟）** | 带 tag 集合的单/多选筛选输出可见集合（纯 C#） | 拟 | 规划中 | 格架筛选 / 目录筛选 / P2 分类 | T-03 | 可单测 |
| TK-DAT-06 | **`PlaceholderRule`（拟）** | 数据到位/缺失/AIGC 三态占位统一（纯 C#） | 拟 | 规划中 | 全游戏 | T-05 | B1 3.7 |

## 6. LOC · 本地化

| ID | 名称 / 路径 | 能力 | 公开面 | 状态 | 消费方 | 来源 | 备注 |
|---|---|---|---|---|---|---|---|
| TK-LOC-01 | `LocService` / `LocTextCatalog` `DataBase/Localization/` | key→文本，回退串 | `Get(key)` | 沿用 | ShortCycleLocalizedText | B 链 | |
| TK-LOC-02 | `LocalizedText` `Tools/Localization/` | TMP 绑定 key、配置与全局刷新，缺 key 时保留可见回退 | `Configure` `RefreshText` `RefreshAll` `RemoveFallbackTokens`（只审计） | 沿用 | FridgeBinder 15 项 + ClueBoardBinder 4 项 | K-T2b / K-T3 | 已完成 GUID 保持的成对迁移；K-T3 场景 19 项接线，当前 10 个 catalog key 缺失以 warning 留账 |
| TK-LOC-03 | 字体链 `ShortCycle_Heavy_WithFallback.asset` | Heavy→Dotted→Source Han 回退 | — | 沿用 | 全部 TMP | C-T5a | 资产非代码，登记以便追踪 |

## 7. DBG · 调试与审计（G26 保护）

| ID | 名称 / 路径 | 能力 | 公开面 | 状态 | 消费方 | 来源 | 备注 |
|---|---|---|---|---|---|---|---|
| TK-DBG-01 | `ShortCycleManualVerificationDriver` | 既有键位→具名动作 + 世界空间状态板 + DRIVER 日志 | 键位表见 C-T8 报告；状态含 `hasSelectedRecipe` | 沿用·保护 | 人工段 / FIX-T4b | C-T8 / AI-000071 / AI-000072 | AI-000072 已在真实 Play 完成空选菜、选菜、Catalog 往返及分层 ESC 驱动验证；键位不变，不删不降级 |
| TK-DBG-02 | `ShortCycleAutomatedProbe` | 固定序列自动探针 | Cover→Catalog→select→Browse 后继续 P0/P1 与逐层 ESC | 沿用·保护 | 回归 / FIX-T4b | A-5 / AI-000071 / AI-000072 | 使用显式 `verificationRecipeId`、按 PanCompleted 等待；AI-000072 真实 Play 返回 PASS |
| TK-DBG-03 | `ShortCycleLayoutAudit`（Editor） | 只读：根序/禁名/UGUI/Missing/启用集合/坐标审计 | 菜单项 | 沿用·保护 | C-T5a-R / 5b / 5c | C-T5a | 度量与写入分离（§7 模板 C） |
| TK-DBG-04 | `Test-CK01CShortCycle.ps1` + `ShortCycleCoreTests.cs` + `UnityStubs.cs`；`CoordinateSpaceRegression/Test-DragLimitCoordinateSpace.ps1` `DevTools/ShortCycle/` | Editor 外纯逻辑测试、静态哨兵与隔离坐标回归 | 命令行；92 源码单元 / 214 静态契约；4 组生产坐标路径 | 沿用·保护 | 每个 CODE 任务 | C-T1~C-T8 / AI-000071 / AI-000074 / AI-000075 / AI-000077 / AI-000083 / AI-000085 / AI-000086 / AI-000088 / AI-000113 | 新增物理方向默认/opt-in/公开 Dispatch 契约、空层栈安全、同 scope 穿透/无关阻挡及诊断字段哨兵；仍不等于真实 Unity/物理硬件证明 |
| TK-DBG-05 | `ShortCyclePresentationSet.overflowRenderers` | Set 级出画件显隐、退出/禁用/销毁恢复 | `SetFocus` / `RestoreTransientState()` + Inspector `overflowRenderers[]` | 沿用 | Phase0 2 项、Phase1 6 项出画件 | K-T2 / K-T3 / AI-000083 | 显式 Restore 保留入口容器恢复；OnDisable/OnDestroy 只恢复 Renderer/Gate 瞬态，禁止在 Unity 启停遍历中重入同层级 SetActive；名单不动态扩张 |
| TK-DBG-07 | `ShortCycleFridgeScrollTraceProbe` `Cooking/ShortCycle/Debug/` | 默认关闭的真实滚轮只读分层诊断：设备/焦点/InputLock→层栈/候选/阻挡/原因→raw/step/Adapter映射→层级/位移 | `SetTraceEnabled`、会话/快照/导出 API；读取 MouseManager 同帧 resolve/dispatch 权威字段、Adapter raw/mapped、Presentation focus 与 InputLock；前缀 `[CK01-F-SCROLL-TRACE]` | 沿用·扩充/P0P1机器门通过/硬件待验 | P0P1 挂载；FridgeScrollLab；用户真实鼠标复验 | AI-000075 / AI-000076 / AI-000088 / AI-000090 / AI-000113 / AI-000114 | 默认关闭且不注入事件/动作、不改 Tier；P0P1已补Fridge Set/InputLock引用并完成SIMULATED四点/链路/生命周期证据，真实硬件raw/step、方向/焦点与手感仍待用户人工验收 |
| TK-DBG-08 | `VisualEffectsLabDriver` + `VisualEffectsLabBuilder/Audit` + Outline `Driver/Builder/Audit` + Rendering runners | 单一 Lab 的 VERIFY-TEMP 手工驱动、显式幂等增量装配、独立只读审计与离线回归 | 原型/正式矩阵、隔离Cat、Defaults；Forward_2显式正规化、确定性透明边夹具、Play聚焦/恢复、接线/几何/机位审计；预算导出 | 沿用·正式Lab后端与Forward_2原点位机器门通过 | VisualEffectsLab T2/T3、OUT-L1c/T1b/OUT-T2/OUT-MEMBER | AI-000079/080/081/097/098/099/100/101/102/103/104/096/093/094/105/095/107/111/112 | 入口拒绝未知夹具覆盖；AI112已证明第二次零重写、当前Importer/scene合同、固定机位GPU A/B、两次Play复现、相机恢复及RT/材质清理，主观视觉继续后置 |
| TK-DBG-09 | `FridgeScrollLabDriver` + `FridgeScrollLabBuilder/Audit` | 独立冰箱滚轮 Lab 的合法 Phase1 薄宿主、REAL_INPUT/SIMULATED 对照、显式幂等搭建与独立只读实际审计 | Driver ContextMenu；`BuildOrOpen`；`AuditActiveScene/RunAudit` | 沿用·保护/Lab运行机器门通过/硬件待验 | `Assets/Scenes/ToolTests/FridgeScrollLab.unity` | AI-000088 / AI-000089 / AI-000090 | 不复制 P0P1、不进 Build Settings、不用 UGUI；两轮 Play 已实测 Phase1、2固定+4扩展六层、双向/双边界、替换/禁用、单监听、Modal、重入归零、trace/截图和 Console0。Lab 的 PlayerInput 持久化空引用已在备份后只接回既有 InputActions；物理鼠标四点/方向/焦点仍由用户人工验收 |

## 8. CMD · 命令

### AI-000067 扩充登记（2026-09-07，未验收检查点）

| ID | 本轮公开面与消费说明 | 状态/边界 |
|---|---|---|
| TK-MOT-07 | Traveler 继续消费 `TransitionBehaviour_Position.MoveTo` 对应的既有 `PlayTo`；新增 `SnapTo(space)` 复用原锚点并取消活动移动；Set 新增 `DeactivateMode.RendererHidden`，避免宿主停用杀死运动协程 | 沿用；AI-000067 编辑器外初始 P0、P0 Clue 校正、P0↔P1 路径通过；真实场景由 FIX-T3 验收 |
| TK-DBG-01 | 既有世界空间 TMP 在 LateUpdate 跟随显式 Camera 或 Main Camera，3% 左/上边距；字号按 ortho/7.2 缩放；退出恢复位置/旋转/字号/对齐/pivot | 沿用·保护；不新增 UI 输入通道，键位不变；实际画面待引擎验收 |
| TK-DBG-04 | 增加共享引用合并、过期 arrival、防停宿主、Renderer/gate 恢复表、五步日志短路和相机/状态板测试 | 沿用·保护；使用原 runner/stubs，非 Unity 实测 |
| TK-DBG-05 | `DeactivateMode` 默认 ContainerInactive；RendererHidden 缓存并隐藏全部子 Renderer，恢复原始 enabled；Director 汇总各 Space 引用后应用 Set 激活/焦点 | 沿用；保留 overflowRenderers 名单，本 CODE 任务不做序列化改动 |
| TK-DBG-06 | `Assets/Scripts/Tools/Debug/CameraFrameGizmo.cs`：运行时数据组件，显式 camera、P1/P0/Right marker、`FrameSize(Camera)`/`GetMarker(int)`；`Assets/Editor/ShortCycle/CameraFrameGizmo.cs` 的静态 `[DrawGizmo]` 绘制器及同目录 `CameraFrameGizmoMenu.cs` 保留 10% 网格/刻度、±2% 安全框、三色/最近机位加粗和菜单能力 | 沿用·保护；AI-000074 真实 DLL 双段编译、AI-000069 真实 Unity 导入、三 Marker 各一组件/引用回读、Align P1/P0/Right 与 Show Frames SceneView 网格截图均通过；最终画面仍由人工段签收，不新增平行工具 |

本增补覆盖上述旧行中的本轮变化，其它条目及 TK-MOT-09 冻结状态不变。完整任务报告见 `.ai-workspace/outputs/CK01-K/FIX-T2_线索板可见性与调试参照框报告.md`。

2026-09-08 AI-000074 完成复核：控制台已核实运行时新文件、Editor 绘制器与菜单存在，SHA-256 分别为 `B5971C87…667E5`、`77CC07FF…A0D1`、`2C7AECCC…B4746`，并合并到唯一 TK-DBG-06 登记。源码程序集修复完成，不等同于 AI-000069 场景挂载或最终人工段通过。报告：`.ai-workspace/outputs/CK01-K/FIX-T2d_CameraFrameGizmo_程序集归属修复报告.md`。

| ID | 名称 / 路径 | 能力 | 公开面 | 状态 | 消费方 | 来源 | 备注 |
|---|---|---|---|---|---|---|---|
| TK-CMD-01 | `CommandManager` + `CommandBindingAsset` `Command/` | string key→命令 ID | — | 改造 | 提示类命令 | CK01-A #15 | 业务动作走 ActionRouter |
| TK-CMD-02 | `ShortCycleCommandBridge` | `[Command]` 反射暴露具名动作 | 23 个手写 Command，含 `close_cover` | 沿用 | 调试/驱动 | C-T4 / C-T8 / AI-000071 | 单向消费唯一 ActionRouter；与 23 个具名动作的对应由既有 runner 校验 |

---

## 9. 维护规则

1. **触发**：任一任务出现"新建工具类 / 扩充公开面 / 更名迁移 / 弃用"时，完成报告必须附本表变更行（新增或修改的整行）；控制台合并入本表并在变更记录留任务号。
2. **完成门槛**：手册"完成门槛五要素"扩为六要素——增加"工具登记影响"（无则写"无"）。
3. **先查后写**：代码任务改动清单（框架 v0.2 §六）中"工具层三问"的第一问必须引用本表 ID。
4. **规划中 → 沿用**：`规划中` 条目由建立它的任务在完成时改状态并填公开面/消费方；不允许跳过登记直接消费。
5. **快照关系**：CK01-A 复用映射表与总档 §八不再增补，需要时以本表为准；两处加一行指向本表。
6. **审计**：每次断点合并走查（如断点④）顺带核对本表 `沿用` 条目是否仍在工程内、`消费方` 是否与场景一致；差异登记坑账本。

7. **程序动画裁决（2026-09-02 代码盘点）**：工程内已有 7 处程序动画实现（Transition 家族、ScaleLerp、Button/Switch 曲线视觉件、ArcPath、DragContainer 回弹、CameraPan 协程、Presenter 线索板协程）。裁决：**以 Transition 家族为唯一基底扩充**（TK-MOT-07），不新建独立 ProceduralMotion；ScaleLerp 并入后弃用；CameraPan/线索板协程改为消费；Button 视觉件、DragContainer 回弹、ArcPath 为专用实现保留不合并。理由：Transition 已有 Controller 编排（注册/全停/全部完成回调）、命名条目、曲线参数化三项骨架，缺的只是属性种类与调用模式。

## 10. WF · 工作区与留档

| ID | 名称/路径 | 能力 | 状态 | 使用边界 |
| --- | --- | --- | --- | --- |
| TK-WF-01 | DevTools/Workflow/Archive-Stage.ps1 | 已闭合阶段的计划、打包、逐文件哈希验证、精确清理和历史提取 | 沿用 | PowerShell7；仅MANUAL闭合队列；Prune只删除已验证清单中的未变化文件，Extract只写归档缓存；不恢复生产资产或更改任务状态 |

2026-09-17：增加阶段归档工具；配表v1.2兼容夹具固定到DevTools/DataTables/Fixtures，AI65历史审计改为按需提取图证。完整旧登记表在阶段ZIP中，所有原有TK编号及当前能力表保留。