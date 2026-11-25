# 可复用工具库索引

最后完整核验：**2026-09-17**（不提高）。本次仅整理文档与核对源码归属；任务执行过程、当时的测试与许可记录移至历史，不能据历史状态启动业务。执行规则以 AGENTS.md、WORKFLOW.md 的现行优先条款为准。工具表保留原能力、使用限制与来源语义，旧候选／待验标签不代表本次重新验收。

## 现状

> 代码改动清单三问先查本表，再按域读取正文。旧入口 `.ai-workspace/TOOLKIT_REGISTRY.md` 保持兼容。

长期 Editor 场景/滚轮/Tomato 验证探针保留在 `Assets/Editor/ProjectValidation/`，手动菜单为 `Tools/Project Validation/`；使用说明见 `DevTools/ProjectValidation/README.md`。E2 仅更名并保留 GUID，历史输出已归档，见[迁移验证归档](../archive/2026-10-17-oversize-slim/README.md)。

| ID | 名称/摘要 | 域 | 正文 | 程序集／宿主 |
| --- | --- | --- | --- | --- |
| TK-INT-20 | InteractionPrompt / InteractionPromptState：被动挂点按键提示，现设备/本地化、短渐变；CODE离线候选，视觉待后继 | INT | [INT.md](INT.md#tk-int-20) | `Toolkit` |
| TK-CMD-01 | CommandManager + CommandBindingAsset Command/ | CMD | [CMD.md](CMD.md#tk-cmd-01) | `Toolkit` |
| TK-CMD-02 | ShortCycleCommandBridge | CMD | [CMD.md](CMD.md#tk-cmd-02) | `ShortCycle` |
| TK-SEC-01 | 认证加密V2、密钥provider、二进制容器与共享ISaveCodec/EncryptedJsonCodec | SEC | [SEC.md](SEC.md#tk-sec-01) | `Toolkit` |
| TK-DAT-01 | CK01GeneratedDataCatalog + 导表器（Editor） | DAT | [DAT.md](DAT.md#tk-dat-01) | `Generated.DataTables` |
| TK-DAT-02 | SerializableDictionary<K,V> DataBase/ | DAT | [DAT.md](DAT.md#tk-dat-02) | `Toolkit` |
| TK-DAT-03 | DataEncryptor | DAT | [DAT.md](DAT.md#tk-dat-03) | `Toolkit` |
| TK-DAT-04 | DataBaseManager / SpreadsheetData | DAT | [DAT.md](DAT.md#tk-dat-04) | `Toolkit` |
| TK-DAT-05 | TagFilter：稳定单/多 tag 筛选（CODE候选，待CONTROL验收/消费） | DAT | [DAT.md](DAT.md#tk-dat-05) | `Toolkit` |
| TK-DAT-06 | PlaceholderRule：Ready/Missing/Aigc 分类（CODE候选，待CONTROL验收/消费） | DAT | [DAT.md](DAT.md#tk-dat-06) | `Toolkit` |
| TK-DAT-07 | DevTools/DataTables/Authoring/ 配表往返工具 | DAT | [DAT.md](DAT.md#tk-dat-07) | 见下方已核源码归属 |
| TK-DAT-08 | SceneToolIdentity/SceneToolInventory（既有登记补索引） | DAT | [DAT.md](DAT.md#tk-dat-08) | `Generated.DataTables`、`Legacy` |
| TK-DAT-09 | Persistence版本化Envelope/HMAC/原子Store/codec选择和应用SaveBoundary | DAT | [PST.md](PST.md#tk-dat-09) | 见下方已核源码归属 |
| TK-DAT-10 | GroundSpaceMath：脚底XY椭圆地面距离纯函数 | DAT | [DAT.md](DAT.md#tk-dat-10) | `Toolkit` |
| TK-DBG-01 | ShortCycleManualVerificationDriver | DBG | [DBG.md](DBG.md#tk-dbg-01) | `ShortCycle` |
| TK-DBG-02 | ShortCycleAutomatedProbe | DBG | [DBG.md](DBG.md#tk-dbg-02) | `ShortCycle` |
| TK-DBG-03 | ShortCycleLayoutAudit（Editor） | DBG | [DBG.md](DBG.md#tk-dbg-03) | `Editor.ShortCycle` |
| TK-DBG-04 | Test-CK01CShortCycle.ps1 + ShortCycleCoreTests.cs + UnityStubs.cs；CoordinateSpaceRegression/Test-DragLimitCoordinateSpace.ps1 DevTools/ShortCycle/ | DBG | [DBG.md](DBG.md#tk-dbg-04) | PowerShell／编辑器外入口 |
| TK-DBG-05 | ShortCyclePresentationSet.overflowRenderers + 缓存 Renderer 世界 bounds | DBG | [DBG.md](DBG.md#tk-dbg-05) | `ShortCycle` |
| TK-DBG-06 | Assets/Scripts/Tools/Debug/CameraFrameGizmo.cs：运行时数据组件，显式 camera、P1/P0-Left/P0-Center/Right marker、FrameSize(Camera)/GetMarker(int)；Assets/Editor/ShortCycle/CameraFrameGizmo.cs 的静态 [DrawGizmo] 绘制器及同目录 CameraFrameGizmoMenu.cs 保留 10% 网格/刻度、±2% 安全框、四色/最近机位加粗和四项对齐菜单 | CMD | [CMD.md](CMD.md#tk-dbg-06) | `Editor.ShortCycle`、`Toolkit` |
| TK-DBG-07 | MouseManager MouseInteractive/ | INT | [INT.md](INT.md#tk-dbg-07) | `Toolkit` |
| TK-DBG-08 | VisualEffectsLabDriver + VisualEffectsLabBuilder/Audit + Outline Driver/Builder/Audit + Rendering runners | DBG | [DBG.md](DBG.md#tk-dbg-08) | `Editor.Project`、`Toolkit` |
| TK-DBG-09 | FridgeScrollLabDriver + FridgeScrollLabBuilder/Audit + ShortCycleModalScrollVerificationControl | DBG | [DBG.md](DBG.md#tk-dbg-09) | `Editor.ShortCycle`、`ShortCycle` |
| TK-INT-01 | MouseManager MouseInteractive/ | INT | [INT.md](INT.md#tk-int-01) | `Toolkit` |
| TK-INT-02 | MouseInteractionLayer | INT | [INT.md](INT.md#tk-int-02) | `Toolkit` |
| TK-INT-03 | Button_MouseInteract + Visuals（ColorSprite / ScaleCurve / RotationCurve / ShowGameObject / Sprite） | INT | [INT.md](INT.md#tk-int-03) | `Toolkit` |
| TK-INT-04 | MouseDraggableObject | INT | [INT.md](INT.md#tk-int-04) | `Toolkit` |
| TK-INT-05 | DropZone | INT | [INT.md](INT.md#tk-int-05) | `Toolkit` |
| TK-INT-06 | DragContainer / DragLimit | INT | [INT.md](INT.md#tk-int-06) | `Toolkit` |
| TK-INT-07 | HoverTrigger | INT | [INT.md](INT.md#tk-int-07) | `Toolkit` |
| TK-INT-08 | HoverZone_MouseInteract + HoverZone_Logic（ToggleLock） | INT | [INT.md](INT.md#tk-int-08) | `Toolkit` |
| TK-INT-09 | MouseHoldObject | INT | [INT.md](INT.md#tk-int-09) | `Toolkit` |
| TK-INT-10 | MouseScrollableObject + ShortCycleScrollAdapter | INT | [INT.md](INT.md#tk-int-10) | `ShortCycle`、`Toolkit` |
| TK-INT-11 | ScrollArea_Controller | INT | [INT.md](INT.md#tk-int-11) | `Toolkit` |
| TK-INT-12 | ScrollBar_Controller | INT | [INT.md](INT.md#tk-int-12) | `Toolkit` |
| TK-INT-13 | KeyboardListener / KeyboardHoldBridge | INT | [INT.md](INT.md#tk-int-13) | `Toolkit` |
| TK-INT-14 | ConditionReceiver | INT | [INT.md](INT.md#tk-int-14) | `Toolkit` |
| TK-INT-15 | Checkbox / Switch / DropDown | INT | [INT.md](INT.md#tk-int-15) | 见下方已核源码归属 |
| TK-INT-16 | ShortCycleMouseActionBinding Cooking/ShortCycle/ | INT | [INT.md](INT.md#tk-int-16) | `ShortCycle` |
| TK-INT-17 | ShortCycleInteractionLayerStack | INT | [INT.md](INT.md#tk-int-17) | `ShortCycle` |
| TK-INT-18 | ShortCycleStateGatedInteraction | INT | [INT.md](INT.md#tk-int-18) | `ShortCycle` |
| TK-LAY-01 | Layout 基类 + GridLayout / HorizontalLayout / VerticalLayout / ArcLayout / ArcLayoutStrategy + Editor Tools/LayoutTools/ | LAY | [LAY.md](LAY.md#tk-lay-01) | `Toolkit` |
| TK-LAY-02 | ShortCycleTrayAnchorLayout Cooking/ShortCycle/ | LAY | [LAY.md](LAY.md#tk-lay-02) | `ShortCycle` |
| TK-LAY-03 | ShortCycleIconScaleTarget + ShortCycleV22LayoutContracts | LAY | [LAY.md](LAY.md#tk-lay-03) | `ShortCycle` |
| TK-LAY-04 | GridPlacement（×3） GridPlacement/ | LAY | [LAY.md](LAY.md#tk-lay-04) | 见下方已核源码归属 |
| TK-LAY-05 | IgnoreParentScale / IgnoreParentRotation | LAY | [LAY.md](LAY.md#tk-lay-05) | `Toolkit` |
| TK-LAY-06 | TidyLayout：既有点位只读规划（CODE候选，待CONTROL验收/消费） | LAY | [LAY.md](LAY.md#tk-lay-06) | `Toolkit` |
| TK-LAY-07 | StackedIcon：四种 stack_type 显式配置展示计划（CODE候选，待CONTROL验收/消费） | LAY | [LAY.md](LAY.md#tk-lay-07) | `Toolkit` |
| TK-LAY-08 | FridgeCatRig + FridgeCatTier Cooking/ShortCycle/Presentation/ | LAY | [LAY.md](LAY.md#tk-lay-08) | `ShortCycle` |
| TK-LOC-01 | LocService / LocTextCatalog DataBase/Localization/ | LOC | [LOC.md](LOC.md#tk-loc-01) | `Toolkit` |
| TK-LOC-02 | LocalizedText Tools/Localization/ | LOC | [LOC.md](LOC.md#tk-loc-02) | `Toolkit` |
| TK-LOC-03 | ShortCycle静态/动态字体构建、FontSwapOnce 171身份幂等迁移；旧事务迁移器弃用 | LOC | [LOC.md](LOC.md#tk-loc-03) | `Editor.Project` |
| TK-MOT-01 | TransitionController Tools/Transition/ | MOT | [MOT.md](MOT.md#tk-mot-01) | `Toolkit` |
| TK-MOT-02 | TransitionBehaviour 基类 + _Alpha / _Scale / _ImageSequence / _SpriteSequence / _Position / _Rotation Tools/Transition/ | MOT | [MOT.md](MOT.md#tk-mot-02) | `Toolkit` |
| TK-MOT-03 | ArcPathController + ArcPathComponent + FollowArcPath_* | MOT | [MOT.md](MOT.md#tk-mot-03) | `Toolkit` |
| TK-MOT-04 | CameraFollower | MOT | [MOT.md](MOT.md#tk-mot-04) | `Toolkit` |
| TK-MOT-05 | MouseParallax | MOT | [MOT.md](MOT.md#tk-mot-05) | `Toolkit` |
| TK-MOT-06 | ShortCycleCameraPanController Cooking/ShortCycle/ | MOT | [MOT.md](MOT.md#tk-mot-06) | `ShortCycle` |
| TK-MOT-07 | **Transition 家族扩充**（替代原“ProceduralMotion 新建”方案） | MOT | [MOT.md](MOT.md#tk-mot-07) | 见下方已核源码归属 |
| TK-MOT-08 | FridgeCatBlinkScheduler + FridgeCatEye | MOT | [MOT.md](MOT.md#tk-mot-08) | `Legacy`、`ShortCycle` |
| TK-MOT-09 | ScaleLerp Tools/ | MOT | [MOT.md](MOT.md#tk-mot-09) | `Toolkit` |
| TK-MOT-10 | Button/Switch Visuals：ScaleCurve / RotationCurve / ShowGameObject / Animator | MOT | [MOT.md](MOT.md#tk-mot-10) | 见下方已核源码归属 |
| TK-MOT-11 | DragContainer 惯性/弹性回弹 | MOT | [MOT.md](MOT.md#tk-mot-11) | `Toolkit` |
| TK-MOT-12 | ShortCycleScenePresenter.MoveClueBoardRoutine | MOT | [MOT.md](MOT.md#tk-mot-12) | `ShortCycle` |
| TK-MOT-13 | 旧 Animator 表现：IngredientInventoryTray（Open/Close Trigger）/ RecipeBookManager 翻页 / Tools.AnimatorHasState | MOT | [MOT.md](MOT.md#tk-mot-13) | `Legacy`、`Toolkit` |
| TK-MOT-14 | CameraViewportAnchor Assets/Scripts/Tools/Layout/ | MOT | [MOT.md](MOT.md#tk-mot-14) | `Toolkit` |
| TK-RND-01 | SpriteOutline2D Rendering/ | RND | [RND.md](RND.md#tk-rnd-01) | `Toolkit` |
| TK-RND-02 | SpriteOutline2D Rendering/ | RND | [RND.md](RND.md#tk-rnd-02) | `Toolkit` |
| TK-RND-03 | SceneColorCapture2D Tools/Rendering/ | RND | [RND.md](RND.md#tk-rnd-03) | `Toolkit` |
| TK-RND-04 | SceneColorCapture2D Tools/Rendering/ | RND | [RND.md](RND.md#tk-rnd-04) | `Toolkit` |
| TK-RND-05 | SpriteOutlineDefaults Rendering/ | RND | [RND.md](RND.md#tk-rnd-05) | `Toolkit` |
| TK-RND-06 | SpriteOutline2DEditor + SpriteOutlineGroup2DEditor Editor/Rendering/ | RND | [RND.md](RND.md#tk-rnd-06) | `Editor.Project` |
| TK-RND-07 | SpriteOutlineGroup2D + ShortCycleOutlineGroupBinding | RND | [RND.md](RND.md#tk-rnd-07) | `ShortCycle`、`Toolkit` |
| TK-WF-01 | DevTools/Workflow/Archive-Stage.ps1 | WF | [WF.md](WF.md#tk-wf-01) | PowerShell／编辑器外入口 |
| TK-WF-02 | DATA 车道、U1轮次与隔离/事务发布管线 | WF | [WF.md](WF.md#tk-wf-02) | 见下方已核源码归属 |
| TK-WF-03 | DevTools/Workflow/UsageMetrics.ps1、Initialize-TaskUsage.ps1、Invoke-TaskTool.ps1、Get-UsageReport.ps1；DevTools/Verify-All.ps1 | WF | [WF.md](WF.md#tk-wf-03) | PowerShell／编辑器外入口 |
| TK-WF-04 | WATCH、机器验收、根修复预算及精确用量导入 | WF | [WF.md](WF.md#tk-wf-04) | 见下方已核源码归属 |
| TK-WF-05 | GitHooks显式baseline/index/工作树Git clean与LFS真实内容只读比较；定向小仓库回归 | WF | [WF.md](WF.md#tk-wf-05) | 见下方已核源码归属 |

## 0. 条目模板与状态词

| 字段 | 说明 |
|---|---|
| ID | `TK-<域>-<序号>`：域 = INT 交互 / MOT 运动过渡 / LAY 排布 / RND 渲染 / DAT 数据 / LOC 本地化 / DBG 调试审计 / CMD 命令 | 程序集／宿主 |
| 名称 / 路径 | 类名 + 相对 `Assets/Scripts/` 路径 | 见下方已核源码归属 |
| 能力（一句话） | 只写能力，不写决策 | 见下方已核源码归属 |
| 公开面 | 关键方法 / 事件 / Inspector 字段 | 见下方已核源码归属 |
| 状态 | `沿用` 直接消费 · `扩展中` 有登记的扩展需求 · `改造` 保留思路重写 · `规划中` 未建 · `冻结` 保留不消费 · `弃用` | 见下方已核源码归属 |
| 消费方 | 已接入的模块/任务；`—` 表示尚无 | 见下方已核源码归属 |
| 来源 | 引入或最近变更的任务号 | 见下方已核源码归属 |
| 备注 | 使用约束、坑账本引用 | 见下方已核源码归属 |

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

## 已核程序集与文件定位

按当前 `.asmdef`／最近 `.asmref` 解析；下列只是源码归属，不代表重新编译、运行或验收。

| 程序集 | 定义／归属入口 | 本页相关源码例 |
| --- | --- | --- |
| `Editor.Project` | [Assets/Editor/Editor.Project.asmdef](<../../Assets/Editor/Editor.Project.asmdef>) | [FontSwapOnce.cs](<../../Assets/Editor/Fonts/FontSwapOnce.cs>)；[SpriteOutline2DEditor.cs](<../../Assets/Editor/Rendering/SpriteOutline2DEditor.cs>)；[SpriteOutlineGroup2DEditor.cs](<../../Assets/Editor/Rendering/SpriteOutlineGroup2DEditor.cs>) |
| `Editor.ShortCycle` | [Assets/Editor/ShortCycle/Editor.ShortCycle.asmdef](<../../Assets/Editor/ShortCycle/Editor.ShortCycle.asmdef>) | [CameraFrameGizmo.cs](<../../Assets/Editor/ShortCycle/CameraFrameGizmo.cs>)；[CameraFrameGizmoMenu.cs](<../../Assets/Editor/ShortCycle/CameraFrameGizmoMenu.cs>)；[FridgeScrollLabBuilder.cs](<../../Assets/Editor/ShortCycle/FridgeScrollLabBuilder.cs>) |
| `Generated.DataTables` | [Assets/Scripts/DataBase/CookingData/Generated.DataTables.asmref](<../../Assets/Scripts/DataBase/CookingData/Generated.DataTables.asmref>) | [CK01GeneratedDataCatalog.cs](<../../Assets/Scripts/DataBase/CookingData/CK01GeneratedDataCatalog.cs>)；[SceneToolInventory.cs](<../../Assets/Scripts/DataBase/CookingData/SceneToolInventory.cs>) |
| `Legacy` | [Assets/Scripts/Legacy/Legacy.asmdef](<../../Assets/Scripts/Legacy/Legacy.asmdef>) | [FridgeCatBlinkScheduler.cs](<../../Assets/Scripts/Legacy/Cooking/Prepare/RecipeBook/Fridge/FridgeCatBlinkScheduler.cs>)；[IngredientInventoryTray.cs](<../../Assets/Scripts/Legacy/Cooking/Prepare/RecipeBook/Fridge/IngredientInventoryTray.cs>)；[RecipeBookManager.cs](<../../Assets/Scripts/Legacy/Cooking/Prepare/RecipeBook/RecipeBookManager.cs>) |
| `ShortCycle` | [Assets/Scripts/Cooking/ShortCycle/ShortCycle.asmdef](<../../Assets/Scripts/Cooking/ShortCycle/ShortCycle.asmdef>) | [FridgeScrollLabDriver.cs](<../../Assets/Scripts/Cooking/ShortCycle/Debug/FridgeScrollLabDriver.cs>)；[ShortCycleModalScrollVerificationControl.cs](<../../Assets/Scripts/Cooking/ShortCycle/Debug/ShortCycleModalScrollVerificationControl.cs>)；[ShortCycleScrollAdapter.cs](<../../Assets/Scripts/Cooking/ShortCycle/Interaction/ShortCycleScrollAdapter.cs>) |
| `Toolkit` | [Assets/Scripts/Tools/Toolkit.asmdef](<../../Assets/Scripts/Tools/Toolkit.asmdef>) | [ArcPathComponent.cs](<../../Assets/Scripts/Tools/ArcPathComponent.cs>)；[ArcPathController.cs](<../../Assets/Scripts/Tools/ArcPathController.cs>)；[CameraFollower.cs](<../../Assets/Scripts/Tools/CameraFollower.cs>) |
| `Toolkit` | [Assets/Scripts/MouseInteractive/Toolkit.asmref](<../../Assets/Scripts/MouseInteractive/Toolkit.asmref>) | [Button_MouseInteract.cs](<../../Assets/Scripts/MouseInteractive/Button/Button_MouseInteract.cs>)；[ConditionReceiver.cs](<../../Assets/Scripts/MouseInteractive/ConditionReceiver.cs>)；[DragContainer.cs](<../../Assets/Scripts/MouseInteractive/Drag/DragContainer.cs>) |
| `Toolkit` | [Assets/Scripts/Command/Toolkit.asmref](<../../Assets/Scripts/Command/Toolkit.asmref>) | [CommandBindingAsset.cs](<../../Assets/Scripts/Command/CommandBindingAsset.cs>)；[CommandManager.cs](<../../Assets/Scripts/Command/CommandManager.cs>) |
| `Toolkit` | [Assets/Scripts/DataBase/Toolkit.asmref](<../../Assets/Scripts/DataBase/Toolkit.asmref>) | [DataBaseManager.cs](<../../Assets/Scripts/DataBase/DataBaseManager.cs>)；[DataEncryptor.cs](<../../Assets/Scripts/DataBase/DataEncryptor.cs>)；[LocTextCatalog.cs](<../../Assets/Scripts/DataBase/Localization/LocTextCatalog.cs>) |
| `Toolkit` | [Assets/Scripts/Rendering/Toolkit.asmref](<../../Assets/Scripts/Rendering/Toolkit.asmref>) | [SpriteOutline2D.cs](<../../Assets/Scripts/Rendering/SpriteOutline2D.cs>)；[SpriteOutlineDefaults.cs](<../../Assets/Scripts/Rendering/SpriteOutlineDefaults.cs>)；[SpriteOutlineGroup2D.cs](<../../Assets/Scripts/Rendering/SpriteOutlineGroup2D.cs>) |

## 历史

[本页整理前全文与历史记录](<../../docs/archive/工程事实_20261124/history/.ai-workspace/toolkit/INDEX.md>)；[归档与原字节恢复索引](<../../docs/archive/工程事实_20261124/README.md>)；[已知不符与保留边界](<../../docs/archive/工程事实_20261124/差异与保留边界.md>)。
