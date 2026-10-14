# 工具库 · INT

> 从 [INDEX.md](INDEX.md) 定位；状态与公开面沿用原登记。

<a id="tk-int-01"></a>
<a id="tk-dbg-07"></a>
<a id="tk-int-02"></a>
<a id="tk-int-03"></a>
<a id="tk-int-04"></a>
<a id="tk-int-05"></a>
<a id="tk-int-06"></a>
<a id="tk-int-07"></a>
<a id="tk-int-08"></a>
<a id="tk-int-09"></a>
<a id="tk-int-10"></a>
<a id="tk-int-11"></a>
<a id="tk-int-12"></a>
<a id="tk-int-13"></a>
<a id="tk-int-14"></a>
<a id="tk-int-15"></a>
<a id="tk-int-16"></a>
<a id="tk-int-17"></a>
<a id="tk-int-18"></a>

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
| TK-INT-17 | `ShortCycleInteractionLayerStack` | 层栈推拉封装 | `Push` / `Pop` / `Remove` / `Contains` / `Restore` | 沿用·扩充 | `ShortCyclePresentationDirector` 的 Space/Modal 接管；VERIFY-TEMP 专用层清理 | K-T2b / K-T3 / AI-000153 | 正式业务仍由 Director 调用 Push/Pop；`Remove` 只供显式拥有者清理自己的非 base 瞬态层，不改变真实 Modal 行为 |
| TK-INT-18 | `ShortCycleStateGatedInteraction` | 按所属 PresentationSet/FSM 状态启停并恢复 MouseInteract，可叠加已选菜条件 | `SetInteractionAllowed(bool)`；Inspector `sessionManager` / `requireSelectedRecipe` | 沿用 | 既有具名按钮 Gate；`shortcycle.close_catalog` 已落地选菜门禁 | K-T2 / K-T3 / AI-000071 / AI-000072 / AI-000222 | Presentation 门禁为外层条件，与视觉 active 分离：镜头移动起点准备目标视觉，完成时才给目标焦点/交互；跨区常驻视觉不等于后台可点。选菜条件读取唯一 Session Context；AI-000072 已实测空选菜拒绝、选菜后可返回 Browse；禁用/销毁恢复原状态 |

