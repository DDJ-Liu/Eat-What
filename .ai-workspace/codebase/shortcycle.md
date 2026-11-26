# 模块正文：2. ShortCycle 当前主链

最后完整核验：**2026-09-17**（不提高）。本次仅整理文档与核对源码归属；任务执行过程、当时的测试与许可记录移至历史，不能据历史状态启动业务。执行规则以 AGENTS.md、WORKFLOW.md 的现行优先条款为准。工具表保留原能力、使用限制与来源语义，旧候选／待验标签不代表本次重新验收。

## 食谱与备菜的返回、托盘鼠标输入

两个视口锚定的返回按钮随各自页面集合的焦点同步画面和输入，入口为 `ShortCycleStateGatedInteraction` 和 `ShortCycleMouseActionBinding`。冰箱与托盘格由后者启用松手点击，`MouseManager` 以按下时的来源区分点击与拖放，并按前景图形／排序组处理竞争命中。拖动开始时，绑定将两种来源格交给 `ShortCycleIngredientDragPreview`。预览从现有托盘格读取外观和世界尺寸，按指针移动；来源格及其碰撞框留在原位。已占用的托盘格由 `TraySlot` 将当前食材 Sprite 的导入轮廓映射到格子，作为鼠标拾取范围。图标缺失或没有轮廓时沿用原格的方形命中区域；空格不截获盘面输入。同一托盘内两个占用格绘制优先级相同且轮廓相交时，按下和拖动共用同一选择：取指针更深入其食材轮廓的格子。托盘外松手由来源格归还食材，`ShortCycleTrayInteraction.Refresh` 随即压紧或清空托盘格，下一次可直接抓取当前格的食材。用户复测及线索板状态见[短循环人工说明](../../docs/短循环前半段人工检查说明.md)。

## 现状

旧命令配置不改写；仅旧默认程序集名/未限定名称允许在白名单内唯一匹配，重名或未知类型拒绝。实际ShortCycleCommandBridge为23个命令（AI263“9项”为计数误记，原记录保留）。探针保留旧序列化anchor字段但不再用其要求板体移动；检查点列表通过AutomatedProbeCheckpoints和CK01-C-PROBE CHECK日志提供，真实Play仍待ENGINE。

## 2. ShortCycle 当前主链

```text
InputManager → MouseManager → MouseInteractionLayer 栈顶
→ MouseActionBinding / ShortCycleActionRouter
→ ShortCycleSessionManager → ShortCycleStateMachine
→ IShortCyclePresentationHost → ShortCyclePresentationDirector
→ Space / PresentationSet / Modal / Traveler / Tray / CameraPan
```

源码以 `Assets/Scripts/Cooking/ShortCycle/` 为根：

| 功能 | 文件/目录 | 程序集／宿主 |
| --- | --- | --- |
| 会话入口与运行数据 | ShortCycleSessionManager.cs、ShortCycleContext.cs、ShortCycleRuntimeManagers.cs | `ShortCycle` |
| 三相/Phase0 子态、托盘状态 | ShortCycleStateMachine.cs、ShortCycleTrayStateMachine.cs | `ShortCycle` |
| 具名动作与命令桥 | ShortCycleActionRegistry.cs、ShortCycleActionRouter.cs、ShortCycleCommandBridge.cs | `ShortCycle` |
| 表现编排 | Runtime/ShortCyclePresentationDirector.cs | `ShortCycle` |
| Space/Set/Traveler | Presentation/ShortCycleSpace.cs、ShortCyclePresentationSet.cs、ShortCycleTravelingElement.cs | `ShortCycle` |
| 冰箱/线索板内容绑定 | Content/ShortCycleFridgeBinder.cs、ShortCycleClueBoardBinder.cs | `ShortCycle` |
| 整猫与层级池 | Presentation/FridgeCatRig.cs、FridgeCatTier.cs、FridgeCatEye.cs | `ShortCycle` |
| 镜头与层栈 | ShortCycleCameraPanController.cs、ShortCycleInteractionLayerStack.cs、ShortCycleInputLockController.cs | `ShortCycle` |
| 滚轮物理入口 | Interaction/ShortCycleScrollAdapter.cs | `ShortCycle` |
| P2 交接 | ShortCycleContracts.cs、ShortCyclePhase2HandoffPresenter.cs；TransitionAdapter 位于同目录独立 ShortCyclePhase2TransitionAdapter.cs（保持原类型/API） | `ShortCycle` |
| 人工/自动验证 | ShortCycleManualVerificationDriver.cs、ShortCycleAutomatedProbe.cs、Debug/ | `ShortCycle` |

Session 初始化顺序：Catalog 校验 → FridgeBinder.Bind → 无菜谱时跳过线索板绑定 → 本地化刷新 → FSM Enter；选菜成功后再绑定业务内容。默认菜谱调试开关不等于正式自动选菜。

跨 Space 或 P0 子视图目标槽不同时调用 CameraPan 并持有输入锁；Cover/RecipeCatalog 解析到 P0Center，RecipeBrowse/ClueBoardShell 解析到 P0Left，同槽切换同步完成且不发 PanStarted。当前正式曲线为 2-key Linear、0.45 秒；空曲线源码回退为线性，中途返向从真实当前位置重新发起，取消请求不晚到写回。跨 Space 进入带 View 路由的 P0 时由随后 ShowView 一次性确定最终槽，避免 P0Center→P0Left 重复移动。

Cover 隐藏旧书底/展开页；Catalog/Browse 恢复；目录签只在 Browse 显示；CurrentRecipeTab 在Browse/ClueBoardShell显示，Kitchen以Visual Only保留背景。Scene 的唯一 Host 是 Managers/PresentationDirector；旧 Presenter/Builder 兼容实现不能替代此入口。

## 已核程序集与文件定位

按当前 `.asmdef`／最近 `.asmref` 解析；下列只是源码归属，不代表重新编译、运行或验收。

| 程序集 | 定义／归属入口 | 本页相关源码例 |
| --- | --- | --- |
| `Editor.ProjectValidation` | [Assets/Editor/ProjectValidation/Editor.ProjectValidation.asmdef](<../../Assets/Editor/ProjectValidation/Editor.ProjectValidation.asmdef>) | [ShortCycleValidationLauncher.cs](<../../Assets/Editor/ProjectValidation/ShortCycleValidationLauncher.cs>) |
| `ShortCycle` | [Assets/Scripts/Cooking/ShortCycle/ShortCycle.asmdef](<../../Assets/Scripts/Cooking/ShortCycle/ShortCycle.asmdef>) | [ShortCycleClueBoardBinder.cs](<../../Assets/Scripts/Cooking/ShortCycle/Content/ShortCycleClueBoardBinder.cs>)；[ShortCycleFridgeBinder.cs](<../../Assets/Scripts/Cooking/ShortCycle/Content/ShortCycleFridgeBinder.cs>)；[ShortCycleScrollAdapter.cs](<../../Assets/Scripts/Cooking/ShortCycle/Interaction/ShortCycleScrollAdapter.cs>) |
| `ShortCycle.Core` | [Assets/Scripts/Cooking/ShortCycle/Core/ShortCycle.Core.asmdef](<../../Assets/Scripts/Cooking/ShortCycle/Core/ShortCycle.Core.asmdef>) | [IShortCyclePresentationHost.cs](<../../Assets/Scripts/Cooking/ShortCycle/Core/IShortCyclePresentationHost.cs>) |
| `Toolkit` | [Assets/Scripts/Tools/Toolkit.asmdef](<../../Assets/Scripts/Tools/Toolkit.asmdef>) | [InputManager.cs](<../../Assets/Scripts/Tools/Input/InputManager.cs>)；[CameraViewportAnchor.cs](<../../Assets/Scripts/Tools/Layout/CameraViewportAnchor.cs>)；[EnvelopeAuthenticator.cs](<../../Assets/Scripts/Tools/Persistence/EnvelopeAuthenticator.cs>) |
| `Toolkit` | [Assets/Scripts/MouseInteractive/Toolkit.asmref](<../../Assets/Scripts/MouseInteractive/Toolkit.asmref>) | [MouseInteractionLayer.cs](<../../Assets/Scripts/MouseInteractive/MouseInteractionLayer.cs>)；[MouseManager.cs](<../../Assets/Scripts/MouseInteractive/MouseManager.cs>) |

## 历史

[本页整理前全文与历史记录](<../../docs/archive/工程事实_20261124/history/.ai-workspace/codebase/shortcycle.md>)；[归档与原字节恢复索引](<../../docs/archive/工程事实_20261124/README.md>)；[已知不符与保留边界](<../../docs/archive/工程事实_20261124/差异与保留边界.md>)。
