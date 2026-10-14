# 可复用工具库索引

> 代码改动清单三问先查本表，再按域读取正文。旧入口 `.ai-workspace/TOOLKIT_REGISTRY.md` 保持兼容。

长期 Editor 场景/滚轮/Tomato 验证探针保留在 `Assets/Editor/ProjectValidation/`，手动菜单为 `Tools/Project Validation/`；使用说明见 `DevTools/ProjectValidation/README.md`。E2 仅更名并保留 GUID，历史输出仍在 `.ai-workspace/outputs/MIG63/Validation/`。

| ID | 名称/摘要 | 域 | 正文 |
| --- | --- | --- | --- |
| TK-CMD-01 | CommandManager + CommandBindingAsset Command/ | CMD | [CMD.md](CMD.md#tk-cmd-01) |
| TK-CMD-02 | ShortCycleCommandBridge | CMD | [CMD.md](CMD.md#tk-cmd-02) |
| TK-DAT-01 | CK01GeneratedDataCatalog + 导表器（Editor） | DAT | [DAT.md](DAT.md#tk-dat-01) |
| TK-DAT-02 | SerializableDictionary<K,V> DataBase/ | DAT | [DAT.md](DAT.md#tk-dat-02) |
| TK-DAT-03 | DataEncryptor | DAT | [DAT.md](DAT.md#tk-dat-03) |
| TK-DAT-04 | DataBaseManager / SpreadsheetData | DAT | [DAT.md](DAT.md#tk-dat-04) |
| TK-DAT-05 | **TagFilter（拟）** | DAT | [DAT.md](DAT.md#tk-dat-05) |
| TK-DAT-06 | **PlaceholderRule（拟）** | DAT | [DAT.md](DAT.md#tk-dat-06) |
| TK-DAT-07 | DevTools/DataTables/Authoring/ 配表往返工具 | DAT | [DAT.md](DAT.md#tk-dat-07) |
| TK-DBG-01 | ShortCycleManualVerificationDriver | DBG | [DBG.md](DBG.md#tk-dbg-01) |
| TK-DBG-02 | ShortCycleAutomatedProbe | DBG | [DBG.md](DBG.md#tk-dbg-02) |
| TK-DBG-03 | ShortCycleLayoutAudit（Editor） | DBG | [DBG.md](DBG.md#tk-dbg-03) |
| TK-DBG-04 | Test-CK01CShortCycle.ps1 + ShortCycleCoreTests.cs + UnityStubs.cs；CoordinateSpaceRegression/Test-DragLimitCoordinateSpace.ps1 DevTools/ShortCycle/ | DBG | [DBG.md](DBG.md#tk-dbg-04) |
| TK-DBG-05 | ShortCyclePresentationSet.overflowRenderers + 缓存 Renderer 世界 bounds | DBG | [DBG.md](DBG.md#tk-dbg-05) |
| TK-DBG-06 | Assets/Scripts/Tools/Debug/CameraFrameGizmo.cs：运行时数据组件，显式 camera、P1/P0-Left/P0-Center/Right marker、FrameSize(Camera)/GetMarker(int)；Assets/Editor/ShortCycle/CameraFrameGizmo.cs 的静态 [DrawGizmo] 绘制器及同目录 CameraFrameGizmoMenu.cs 保留 10% 网格/刻度、±2% 安全框、四色/最近机位加粗和四项对齐菜单 | CMD | [CMD.md](CMD.md#tk-dbg-06) |
| TK-DBG-07 | MouseManager MouseInteractive/ | INT | [INT.md](INT.md#tk-dbg-07) |
| TK-DBG-08 | VisualEffectsLabDriver + VisualEffectsLabBuilder/Audit + Outline Driver/Builder/Audit + Rendering runners | DBG | [DBG.md](DBG.md#tk-dbg-08) |
| TK-DBG-09 | FridgeScrollLabDriver + FridgeScrollLabBuilder/Audit + ShortCycleModalScrollVerificationControl | DBG | [DBG.md](DBG.md#tk-dbg-09) |
| TK-INT-01 | MouseManager MouseInteractive/ | INT | [INT.md](INT.md#tk-int-01) |
| TK-INT-02 | MouseInteractionLayer | INT | [INT.md](INT.md#tk-int-02) |
| TK-INT-03 | Button_MouseInteract + Visuals（ColorSprite / ScaleCurve / RotationCurve / ShowGameObject / Sprite） | INT | [INT.md](INT.md#tk-int-03) |
| TK-INT-04 | MouseDraggableObject | INT | [INT.md](INT.md#tk-int-04) |
| TK-INT-05 | DropZone | INT | [INT.md](INT.md#tk-int-05) |
| TK-INT-06 | DragContainer / DragLimit | INT | [INT.md](INT.md#tk-int-06) |
| TK-INT-07 | HoverTrigger | INT | [INT.md](INT.md#tk-int-07) |
| TK-INT-08 | HoverZone_MouseInteract + HoverZone_Logic（ToggleLock） | INT | [INT.md](INT.md#tk-int-08) |
| TK-INT-09 | MouseHoldObject | INT | [INT.md](INT.md#tk-int-09) |
| TK-INT-10 | MouseScrollableObject + ShortCycleScrollAdapter | INT | [INT.md](INT.md#tk-int-10) |
| TK-INT-11 | ScrollArea_Controller | INT | [INT.md](INT.md#tk-int-11) |
| TK-INT-12 | ScrollBar_Controller | INT | [INT.md](INT.md#tk-int-12) |
| TK-INT-13 | KeyboardListener / KeyboardHoldBridge | INT | [INT.md](INT.md#tk-int-13) |
| TK-INT-14 | ConditionReceiver | INT | [INT.md](INT.md#tk-int-14) |
| TK-INT-15 | Checkbox / Switch / DropDown | INT | [INT.md](INT.md#tk-int-15) |
| TK-INT-16 | ShortCycleMouseActionBinding Cooking/ShortCycle/ | INT | [INT.md](INT.md#tk-int-16) |
| TK-INT-17 | ShortCycleInteractionLayerStack | INT | [INT.md](INT.md#tk-int-17) |
| TK-INT-18 | ShortCycleStateGatedInteraction | INT | [INT.md](INT.md#tk-int-18) |
| TK-LAY-01 | Layout 基类 + GridLayout / HorizontalLayout / VerticalLayout / ArcLayout / ArcLayoutStrategy + Editor Tools/LayoutTools/ | LAY | [LAY.md](LAY.md#tk-lay-01) |
| TK-LAY-02 | ShortCycleTrayAnchorLayout Cooking/ShortCycle/ | LAY | [LAY.md](LAY.md#tk-lay-02) |
| TK-LAY-03 | ShortCycleIconScaleTarget + ShortCycleV22LayoutContracts | LAY | [LAY.md](LAY.md#tk-lay-03) |
| TK-LAY-04 | GridPlacement（×3） GridPlacement/ | LAY | [LAY.md](LAY.md#tk-lay-04) |
| TK-LAY-05 | IgnoreParentScale / IgnoreParentRotation | LAY | [LAY.md](LAY.md#tk-lay-05) |
| TK-LAY-06 | **TidyLayout（拟）** | LAY | [LAY.md](LAY.md#tk-lay-06) |
| TK-LAY-07 | **StackedIcon（拟）** | LAY | [LAY.md](LAY.md#tk-lay-07) |
| TK-LAY-08 | FridgeCatRig + FridgeCatTier Cooking/ShortCycle/Presentation/ | LAY | [LAY.md](LAY.md#tk-lay-08) |
| TK-LOC-01 | LocService / LocTextCatalog DataBase/Localization/ | LOC | [LOC.md](LOC.md#tk-loc-01) |
| TK-LOC-02 | LocalizedText Tools/Localization/ | LOC | [LOC.md](LOC.md#tk-loc-02) |
| TK-LOC-03 | ShortCycle静态/动态字体构建、FontSwapOnce 171身份幂等迁移；旧事务迁移器弃用 | LOC | [LOC.md](LOC.md#tk-loc-03) |
| TK-MOT-01 | TransitionController Tools/Transition/ | MOT | [MOT.md](MOT.md#tk-mot-01) |
| TK-MOT-02 | TransitionBehaviour 基类 + _Alpha / _Scale / _ImageSequence / _SpriteSequence / _Position / _Rotation Tools/Transition/ | MOT | [MOT.md](MOT.md#tk-mot-02) |
| TK-MOT-03 | ArcPathController + ArcPathComponent + FollowArcPath_* | MOT | [MOT.md](MOT.md#tk-mot-03) |
| TK-MOT-04 | CameraFollower | MOT | [MOT.md](MOT.md#tk-mot-04) |
| TK-MOT-05 | MouseParallax | MOT | [MOT.md](MOT.md#tk-mot-05) |
| TK-MOT-06 | ShortCycleCameraPanController Cooking/ShortCycle/ | MOT | [MOT.md](MOT.md#tk-mot-06) |
| TK-MOT-07 | **Transition 家族扩充**（替代原“ProceduralMotion 新建”方案） | MOT | [MOT.md](MOT.md#tk-mot-07) |
| TK-MOT-08 | FridgeCatBlinkScheduler + FridgeCatEye | MOT | [MOT.md](MOT.md#tk-mot-08) |
| TK-MOT-09 | ScaleLerp Tools/ | MOT | [MOT.md](MOT.md#tk-mot-09) |
| TK-MOT-10 | Button/Switch Visuals：ScaleCurve / RotationCurve / ShowGameObject / Animator | MOT | [MOT.md](MOT.md#tk-mot-10) |
| TK-MOT-11 | DragContainer 惯性/弹性回弹 | MOT | [MOT.md](MOT.md#tk-mot-11) |
| TK-MOT-12 | ShortCycleScenePresenter.MoveClueBoardRoutine | MOT | [MOT.md](MOT.md#tk-mot-12) |
| TK-MOT-13 | 旧 Animator 表现：IngredientInventoryTray（Open/Close Trigger）/ RecipeBookManager 翻页 / Tools.AnimatorHasState | MOT | [MOT.md](MOT.md#tk-mot-13) |
| TK-MOT-14 | CameraViewportAnchor Assets/Scripts/Tools/Layout/ | MOT | [MOT.md](MOT.md#tk-mot-14) |
| TK-RND-01 | SpriteOutline2D Rendering/ | RND | [RND.md](RND.md#tk-rnd-01) |
| TK-RND-02 | SpriteOutline2D Rendering/ | RND | [RND.md](RND.md#tk-rnd-02) |
| TK-RND-03 | SceneColorCapture2D Tools/Rendering/ | RND | [RND.md](RND.md#tk-rnd-03) |
| TK-RND-04 | SceneColorCapture2D Tools/Rendering/ | RND | [RND.md](RND.md#tk-rnd-04) |
| TK-RND-05 | SpriteOutlineDefaults Rendering/ | RND | [RND.md](RND.md#tk-rnd-05) |
| TK-RND-06 | SpriteOutline2DEditor + SpriteOutlineGroup2DEditor Editor/Rendering/ | RND | [RND.md](RND.md#tk-rnd-06) |
| TK-RND-07 | SpriteOutlineGroup2D + ShortCycleOutlineGroupBinding | RND | [RND.md](RND.md#tk-rnd-07) |
| TK-WF-01 | DevTools/Workflow/Archive-Stage.ps1 | WF | [WF.md](WF.md#tk-wf-01) |
| TK-WF-02 | DATA 车道、U1轮次与隔离/事务发布管线 | WF | [WF.md](WF.md#tk-wf-02) |
| TK-WF-03 | DevTools/Workflow/UsageMetrics.ps1、Initialize-TaskUsage.ps1、Invoke-TaskTool.ps1、Get-UsageReport.ps1；DevTools/Verify-All.ps1 | WF | [WF.md](WF.md#tk-wf-03) |
| TK-WF-04 | WATCH、机器验收、根修复预算及精确用量导入 | WF | [WF.md](WF.md#tk-wf-04) |

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

## 9. 维护规则

1. **触发**：任一任务出现"新建工具类 / 扩充公开面 / 更名迁移 / 弃用"时，完成报告必须附本表变更行（新增或修改的整行）；控制台合并入本表并在变更记录留任务号。
2. **完成门槛**：手册"完成门槛五要素"扩为六要素——增加"工具登记影响"（无则写"无"）。
3. **先查后写**：代码任务改动清单（框架 v0.2 §六）中"工具层三问"的第一问必须引用本表 ID。
4. **规划中 → 沿用**：`规划中` 条目由建立它的任务在完成时改状态并填公开面/消费方；不允许跳过登记直接消费。
5. **快照关系**：CK01-A 复用映射表与总档 §八不再增补，需要时以本表为准；两处加一行指向本表。
6. **审计**：每次断点合并走查（如断点④）顺带核对本表 `沿用` 条目是否仍在工程内、`消费方` 是否与场景一致；差异登记坑账本。

7. **程序动画裁决（2026-09-02 代码盘点）**：工程内已有 7 处程序动画实现（Transition 家族、ScaleLerp、Button/Switch 曲线视觉件、ArcPath、DragContainer 回弹、CameraPan 协程、Presenter 线索板协程）。裁决：**以 Transition 家族为唯一基底扩充**（TK-MOT-07），不新建独立 ProceduralMotion；ScaleLerp 并入后弃用；CameraPan/线索板协程改为消费；Button 视觉件、DragContainer 回弹、ArcPath 为专用实现保留不合并。理由：Transition 已有 Controller 编排（注册/全停/全部完成回调）、命名条目、曲线参数化三项骨架，缺的只是属性种类与调用模式。
