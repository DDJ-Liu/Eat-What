# 《是啊，吃什么》代码结构与修改导航

> 2026-10-12 E2自动段已启用：用户要求自动推进E2、人工检查并行；正式AUTOMATIC链AUTO-20261012-193340678仅含AI219（迁移归档/恢复验证）→AI220（长期工具保留GUID更名/引用收口），各用新可见chat。字体已提交09cd180且不返工；旧监管及WATCH仍暂停。边界见.ai-workspace/inputs/E2-20261012/授权与边界.md，实际状态以queue为准。下文MANUAL属于此前时点；本段不删除迁移副本、不新增Git提交/推送/合并，远端持久化与最终集成留待可审成果形成后处理；未宣称E2整体关闭，完整项目复核日期不变。

2026-10-12 字体收束专项：用户已确认字体、Excel验收通过，并明确授权字体范围Git提交。AI214已正式CLOSED_SUPERSEDED（由AI217完整9/9覆盖，四次失败及旧许可保留）；AI162旧预览待验已由AI207的171配对、Play/Build及R4人工结论结清为VERIFIED。六个迁移资产与已验收版本一致，当前6000.3.24f1场景clean、Console原生0错误/0警告；长期字体工具18项静态检查、真实DLL离线编译和37项提交守卫夹具通过。未发现本次字体范围未解决问题。一次性迁移停用留存、不重跑。当前MANUAL/watch-60暂停；E2具备进入准备阶段条件，归档/恢复验证、长期工具更名及迁移专属清理仍未执行。完整项目复核日期不变；下文旧时点状态保留为历史。详见DevTools/Fonts/README.md及.ai-workspace/outputs/control/FONT-R-CLOSEOUT-20261012/收尾与剩余事项.md。

2026-10-12 配表工作簿专项：修复 `Format-AuthoringWorkbook.ps1` 遗留空条件格式扩展/数据验证容器导致的 Excel 打开失败；原 xlsx 已按内容保护门修复，14 表/2087 单元格及批注样式保持，Excel 普通打开通过。未改配表业务值、CSV 或 Unity 生成资产；用户现已确认 Excel 人工检查通过，119条表头注释恢复标准矩形并经原生预览确认，AI186/AI191 正式 VERIFIED；P2及后续配置待办保留，完整复核日期不变。详见 [Authoring 说明](DevTools/DataTables/Authoring/README.md)。

2026-10-12 工作流专项：下一批次按 AGENTS 四条硬上限执行，通用续行改用 `ApproveResume(task, runs=N, scope)`；一次性脚本登记在 `DevTools/one-time-tools.json` 并退出 Final。这里只核对规则/队列及共享离线回归，未操作 Unity、未重验字体；当前 MANUAL/watch-60 暂停，完整复核日期仍为 2026-09-17。接口见 [.ai-workspace/toolkit/WF.md](.ai-workspace/toolkit/WF.md#approve-resume)。

最后完整结构复核：2026-09-17。2026-10-09专项：字体机器11/11及R4人工通过，AI207 VERIFIED；按用户决定正式退出监管，当前MANUAL、原session PAUSED_MODE，watch-60已暂停。AI214旧失败与全部待办保留，用户提交及E2待执行；F-α原功能已验收，P2/Excel仍各自待办。此次仅模式收尾，完整复核日期不变。详见[固定路径报告](.ai-workspace/outputs/control/FONT-R-PRE5c-20261005/字体问题修复路径报告.md)。

## 1. 工程入口

| 项目 | 当前事实 |
| --- | --- |
| 引擎与渲染 | Unity 6000.3.24f1、URP 17.3.0；原工作区H接棒通过 |
| 输入 | 本迁移分支Input System 1.20.0、Uniform；MIG-F01源码已统一滚轮单位并移除Windows `×120`桥接，Play/真实硬件仍待本批验收 |
| 当前 Build 列表 | ShortCycle_P0P1 index0、CookingProcess index1，Profile不覆盖，Prepare文件保留；原目录已同步 |
| 本轮开发/验收入口 | Assets/Scenes/Cooking/ShortCycle_P0P1.unity；两目录均已加入Build；原目录H通过、冻结解除，后续开发在codex/unity6-followup分支 |
| 视觉统一试验 | Assets/Scenes/ToolTests/VisualEffectsLab.unity |
| 滚轮独立试验 | Assets/Scenes/ToolTests/FridgeScrollLab.unity |
| 盘点 | Assets/Scripts 249 个 C#；Assets/Editor 63 个 C#（含4个保留迁移验证文件）；Assets/Scenes 15 个场景；Prefabs/Resources 共 71 个 Prefab |
| 配表 | 14 个 schema；Assets/Generated/DataTables 实际 122 个 asset（含 catalog、共享资源） |
| 程序集 | 项目主要使用默认 Assembly-CSharp / Assembly-CSharp-Editor；第三方程序集另计 |
| 验证 | DevTools 离线编译/契约测试、Unity 场景探针和人工验收；Assets/Tests 当前为视效夹具，不等于已有 NUnit 测试程序集 |

Packages清单/锁文件及LocalPackages/com.coplaydev.unity-mcp固定Editor包进入版本管理。DevTools/MCP记录服务端10.1.2、源码提交和uv锁文件；Editor为同提交10.1.3-beta.4，迁移目录6.3连接与实例路由已实测。自动客户端改写已用10文件本地补丁关闭；主动Configure仍可用，未提供完整服务端离线镜像。详见DevTools/MCP/README.md。原目录H接棒已恢复相同本地包及独立venv，新实例Eat-What@d5759c75ae7cb5de连接/路由通过；本轮验证服务收尾停止，未来使用重新发现实例。

以上版本表指已验收E1基线及已经完成H接棒的原工作区。独立Eat-What-U6副本已导入6000.3.24f1：URP17.3.0、Input System1.20.0、UGUI2.0.0、Cinemachine2.10.7；Coplay已收敛为工程内固定包；M4连接MIG、H重新发现并验证原目录实例，原队列未接入。API Updater把HorizontalPlayerController.cs与QuickAddForce.cs的Rigidbody2D.velocity改为linearVelocity。M2已保存Compatibility=false、无兼容宏，Windows构建通过；TMP目录已升级为84文件，M3R按N1取消忽略并随Git/LFS跟踪，保留原GUID和快照。精确变化和人工复核见[M2/M3执行结果](.ai-workspace/MIG63/M2M3_执行结果与人工验收.md)，原目录H运行回归已通过，H后格式更新已按用户最新决定随本次集成进入main；unity6.3-baseline标签保持37bb854，新业务使用codex/unity6-followup。

迁移侧验证工具变化：DevTools/Rendering/Test-OutlineMerge.ps1、Test-EtherBubbleDistortion.ps1、DevTools/ShortCycle/Test-CK01CShortCycle.ps1及CoordinateSpaceRegression/Invoke-CoordinateCompilation.ps1按项目版本定位Hub/旧Editor；泡泡测试引用URP17的2D.Runtime程序集。H磁盘接棒已同步这些工具，九组离线回归通过。原临时验证链已移除；本轮按用户决定长期保留迁移侧Assets/Editor/MIG63Validation/MIG63WheelProbe.cs、MIG63TomatoProbe.cs、MIG63SpineSceneProbe.cs和MIG63ValidationSession.cs，菜单Tools/MIG63/Validation，说明DevTools/MIG63/README.md。两侧新增DevTools/GitHooks字体工作树/index保护，最终机器结果不覆盖硬件鼠标和同分辨率全视觉对照。

引擎迁移须同时检查Assets内依赖：Spine 3.8（2021-11-10）、Text Animator 2.3.1、NuGet/EPPlus及MCP相关DLL。旧链CookingPhaseState.cs、CameraFollower.cs使用Cinemachine 2 API；自制视效通过相机回调及离屏RT调度，Renderer2D的Renderer Features为空。当前执行依据为[MIG63迁移基线](.ai-workspace/MIG63/迁移基线.md)，不沿用历史6.6候选评估作为目标。

Spine保护范围：Assets/Scenes/Spine Sample.unity和Assets/Scenes/ToolTests/HorizontalPlayerControllerTest.unity。M4在这两处保留原Prefab/对象身份，使用场景override绑定现有Tomato 3.8.99；动画控制器为Avatars/Tomato/Tomato_MIG63.controller（idle/walk/kick，Idle/Walk/Kick触发器）。三个SkeletonMecanim的MeshRenderer磁盘均为禁用，由已有SpineRuntimeMeshRendererBootstrap在Start启用，避免场景恢复旧网格产生Invalid worldAABB。CharacterEquipment采用Tomato_Non和7部位；Sample的Bottom槽保持Test_EquipBottom按钮契约。HorizontalPlayerController保留Rigidbody2D移动和Animator路径，spineAnimation仍为空；另一SkeletonAnimation用法由长期Tomato双路径探针覆盖。旧Sample/output4 3.8.75及Prefab/runtime/Shader未覆盖或删除，直接读取旧骨骼仍是已登记历史缺陷。ShortCycle/两个Lab不依赖Spine。保存场景与双路径GPU通过并不表示全部皮肤、混合模式或重打包均验收。

迁移侧滚轮专项：`Assets/Scripts/MouseInteractive/MouseManager.cs` 直接累计Input System统一滚轮单位，冷却期最多保留一步，目标/层/失焦时清理，冷却使用unscaled time；Windows专用`×120`桥接已在MIG-F01移除。手感参数来自T8 `fridge_scroll_threshold`（缺失/非法默认1）与`fridge_scroll_cooldown`（缺失/非法默认0.2秒），由`CK01GeneratedDataCatalog`按invariant culture解析，再经`ShortCycleSessionManager.Start → MouseManager.ConfigureScrollInput`单一路径应用，FridgeScrollLab不设旁路。配置人员入口见[区域滚动说明](组件说明文档/区域滚动_配置与复用说明.md)。本轮源码/静态验证完成，生成资产刷新、Play与Windows/Mac真实硬件手感由后续F3/F4及人工验收完成；旧2022回退基线不用于打开已接棒目录。Windows API保持D3D11优先/D3D12第二，Auto=false。

2026-09-23 FIX-T5 专项：`ShortCycleCameraPanController` 的原三槽序列化值保持不变并扩为 P1/P0Left/P0Center/Right；`ShortCycleSpace.viewTable[].cameraSlot` 决定 Phase0 子视图目标槽。Director 仅在目标槽变化时复用既有 Pan/输入锁/generation，Pan 完成后才激活目标视图；同槽 ①↔④ 不产生 PanStarted。Set 的逻辑激活再与正交相机世界矩形、缓存的子 Renderer 世界 bounds 并集相交，无 Renderer/无可用相机均保留逻辑可见并只发节流诊断。AI168增加TK-MOT-14 `CameraViewportAnchor` 和显式 `keepVisualActiveAcrossSpaces`：锚定控件Renderer不参与世界bounds；持续结构绕过相机裁切导致的逻辑Deactivate，但焦点与交互仍单独关闭，普通Set/Modal默认行为不变。AI156 已完成原正式场景接线：四 Marker 的 x=-25.6/-12.8/0/25.6；线索板两锚点同为(-19.2,-2.1168,-0.3)，Shared/ClueBoard_Group/ClueBoard_HitArea 复用鼠标动作/状态门控，旧实体按钮退役。AI165的④书页压暗和板外返回保持；后续AI169已完成ClueBoard持续标志和两个返回按钮viewport(.06,.08)接线，P0Left改为-9.6，并通过连续构图/Play机器回归，最终多宽高比画面仍PENDING_USER。上列-12.8为AI156历史值，不是现布局。配置入口见[镜头视口锚定说明](组件说明文档/镜头视口锚定_配置与复用说明.md)。CoverTitle例外及171行、Lab60迁移范围已由用户批准；AI170/171/172 已被 FONT-R 替代，旧失败/恢复史保留。用户2026-10-05最新取消AI203/204恢复与恢复提交，确认当前布局及字体为新基线，由控制台正常hook提交后直接AI205 R2→AI206编译→AI207 R3幂等执行。只保护Static精确字节，三现有材质和GUID保留并在R3重调UpdateShaderRatios；布局新变化保留，最终视觉仍待用户。

2026-10-05 FONT-R2 / AI205：一次性迁移入口为 `Assets/Editor/Fonts/FontSwapOnce.cs`（AI205交付141行；现行AI213为262行），菜单 `Tools/CK01/Fonts/Swap To Static (once)`；`Run(true)`只读预检，`Run(false)`复用三材质GUID，按原CSV去重171身份，使用TMP公开fontSharedMaterial→font（AI213修正顺序），源样式→Static atlas→UpdateShaderRatios，场景实例先于Prefab源保存。旧ShortCycleFontSwapTool及专属测试原字节移至 `DevTools/Fonts/deprecated/`，长期AtlasBuilder/LayoutAudit/hook保留。Fonts聚合改为Test-ShortCycleFonts + Test-FontSwapOnce；本机Unity6000.3.24f1/TMP DLL编译、436项生产核心离线断言及37项hook测试通过，生产序列化写入为零；AI206已在同工程Unity6000.3.24f1完成真实Refresh/Compile，FontSwapOnce已加载到Editor程序集，Console新增Error为0；3398个既有序列化文件SHA/时间戳不变，仅Editor导入新增该脚本meta。AI207迁移/原生ratio/截图/Build尚待执行。详见 `DevTools/Fonts/README.md`。

## 2. ShortCycle 当前主链

```text
InputManager → MouseManager → MouseInteractionLayer 栈顶
→ MouseActionBinding / ShortCycleActionRouter
→ ShortCycleSessionManager → ShortCycleStateMachine
→ IShortCyclePresentationHost → ShortCyclePresentationDirector
→ Space / PresentationSet / Modal / Traveler / Tray / CameraPan
```

源码以 `Assets/Scripts/Cooking/ShortCycle/` 为根：

| 功能 | 文件/目录 |
| --- | --- |
| 会话入口与运行数据 | ShortCycleSessionManager.cs、ShortCycleContext.cs、ShortCycleRuntimeManagers.cs |
| 三相/Phase0 子态、托盘状态 | ShortCycleStateMachine.cs、ShortCycleTrayStateMachine.cs |
| 具名动作与命令桥 | ShortCycleActionRegistry.cs、ShortCycleActionRouter.cs、ShortCycleCommandBridge.cs |
| 表现编排 | Runtime/ShortCyclePresentationDirector.cs |
| Space/Set/Traveler | Presentation/ShortCycleSpace.cs、ShortCyclePresentationSet.cs、ShortCycleTravelingElement.cs |
| 冰箱/线索板内容绑定 | Content/ShortCycleFridgeBinder.cs、ShortCycleClueBoardBinder.cs |
| 整猫与层级池 | Presentation/FridgeCatRig.cs、FridgeCatTier.cs、FridgeCatEye.cs |
| 镜头与层栈 | ShortCycleCameraPanController.cs、ShortCycleInteractionLayerStack.cs、ShortCycleInputLockController.cs |
| 滚轮物理入口 | Interaction/ShortCycleScrollAdapter.cs |
| P2 交接 | ShortCycleContracts.cs、ShortCyclePhase2HandoffPresenter.cs；TransitionAdapter 定义于 SessionManager.cs |
| 人工/自动验证 | ShortCycleManualVerificationDriver.cs、ShortCycleAutomatedProbe.cs、Debug/ |

Session 初始化顺序：Catalog 校验 → FridgeBinder.Bind → 无菜谱时跳过线索板绑定 → 本地化刷新 → FSM Enter；选菜成功后再绑定业务内容。默认菜谱调试开关不等于正式自动选菜。

跨 Space 或 P0 子视图目标槽不同时调用 CameraPan 并持有输入锁；Cover/RecipeCatalog 解析到 P0Center，RecipeBrowse/ClueBoardShell 解析到 P0Left，同槽切换同步完成且不发 PanStarted。当前正式曲线为 2-key Linear、0.45 秒；空曲线源码回退为线性，中途返向从真实当前位置重新发起，取消请求不晚到写回。跨 Space 进入带 View 路由的 P0 时由随后 ShowView 一次性确定最终槽，避免 P0Center→P0Left 重复移动。

Cover 隐藏旧书底/展开页；Catalog/Browse 恢复；目录签只在 Browse 显示；CurrentRecipeTab 保持 Browse-only。Scene 的唯一 Host 是 Managers/PresentationDirector；旧 Presenter/Builder 兼容实现不能替代此入口。

## 3. 冰箱与通用滚动

```text
MouseManager → MouseScrollableObject → ShortCycleScrollAdapter.MapPhysicalStep
→ ActionRouter → Session → FridgeBinder → FridgeCatRig → ScrollArea_Controller
```

- 正式 P0 的 ScrollRegion 启用 Invert Physical Scroll Direction；业务 Dispatch(+1)=下一层、(-1)=上一层不变，Lab 保留独立参考方向。
- 滚轮手感配置链为 `CookingGameConfig CSV/schema → CK01GeneratedDataCatalog.ResolveFridgeScrollSettings → ShortCycleSessionManager.Start → MouseManager.ConfigureScrollInput`；解析使用invariant culture，缺失/非法值逐项回退并告警。
- 绑定/重绑按 ceil(容量/5) 设置层数；demo 容量30，对应6层/30槽。正数量库存紧密绑定，真实初始库存10项。
- ApplyFilterView(visibleCount) 提供 max(1,ceil(visibleCount/5)) 契约与测试；本轮生产调用方仍未接入，shortcycle.fridge.filter 仍属后续段的桩。
- 源 Prefab：Assets/Prefabs/Cooking/ShortCycle/FridgeCat/FridgeCat_Rig.prefab；格件：Assets/Prefabs/Cooking/ShortCycle/GridPieces/FridgeSlot.prefab。
- VisualContent_TUNE 包含整猫视觉、层、双眼与随动装饰。运行时挂到 ContentArea_TUNE；ScrollController/Viewport/TierAlignmentTarget 与场景 ScrollRegion 固定。
- AnchorAlignedAutoBounds 使用真实 Anchor 计算层目标和边界；不可只靠层索引断言实际可见。
- 当前场景后15个数量标签经过 AI145 实例修复；源 FridgeSlot 默认值与已修场景覆盖并不相同，不可盲目 Revert 到源 Prefab。
- 通用组件位于 Assets/Scripts/MouseInteractive/ScrollArea/ScrollArea_Controller.cs、Drag/DragContainer.cs、Drag/DragLimit.cs；可独立复用，冰箱业务适配器是可选桥接。

配置入口、参数与探针操作见[区域滚动说明](组件说明文档/区域滚动_配置与复用说明.md)、[整猫说明](组件说明文档/视觉特效/冰箱猫整体滚动_配置与使用说明.md)。FridgeScrollLabDriver 的 SIMULATED 只调 Adapter，不伪造硬件事件；真实鼠标验收已由用户通过。

## 4. 数据与本地化

- 权威 CSV：DataTables/Cooking、DataTables/Localization；schema：DataTables/Schemas。
- CK01 业务规则已到 v2.4；JSON 中 schemaVersion=2 是格式版本，不应机械改成 2.4。
- 导入入口：Assets/Editor/DataTables/DataTableImportService.cs；人工菜单为 Tools > Data Tables > Import All，无 UI batch 入口为 `EatWhat.DataTables.DataTableImportService.ImportAllBatch`，失败写 `[DATA]` 并返回非零。
- 唯一聚合入口：Assets/Scripts/DataBase/CookingData/CK01GeneratedDataCatalog.cs；资产 Assets/Generated/DataTables/CK01GeneratedDataCatalog.asset。
- 区域真实厨具：场景中的 `CookingRoomManager.sceneAreaId` 对应 KitchenAreas.id；实际工具或固定设施对象挂 `SceneToolIdentity.toolId/areaOwner`，ID 对应 Tools.tool_id，且显式 owner 必须等于当前层级最近的 Room。运行时 `CookingRoomManager.GetSceneToolInventory(catalog)` 每次重扫，统一 `SceneToolInventory.Resolve` 排除禁用/预制资产、去重并诊断无映射/跨区。旧 `Tool_Cooking.data/ToolName`、`ContainerTag` 不自动映射到 CK01 ID。`Assets/Editor/DataTables/SceneToolInventoryExporter.cs` 的菜单从已保存干净场景导出含工程 productGUID、场景 GUID/SHA 与对象归属证据的 JSON；`Validate-DataTables.ps1 -SceneInventoryPath <json> -RequireSceneInventory` 校验新鲜度及 V-04/V-08/V-09。无清单为 UNVERIFIED，单场景仅覆盖部分区域为 PARTIAL 并列出缺项；两者 `isFullyVerified=false`，不得当作完整场景通过。AI190已检查遗留CookingProcess，其缺映射不再要求回写；用户2026-10-04将新P2接线/Editor清单/Play延期至新scene开发，AI191只作完整静态数据检查并显式报告场景UNVERIFIED。
- AI-000190 实测（2026-10-04）：`CookingProcess` 中 2 个 Room 的 `sceneAreaId` 均空，4 个旧 `Tool_Cooking` 均无 `SceneToolIdentity`，不能由旧名称、`roomData` 或 `preset_tools` 推断 CK01 ID。公开导出器生成的场景清单为 2 区域、0 个已映射工具、6 项问题；`-RequireSceneInventory` 判定 `UNVERIFIED / isFullyVerified=false`。Play Mode 可正常推进，临时探针证实去重、跨区显式归属冲突及工具禁用行为；仅禁用 Room 组件而不禁用 GameObject 时，方法仍可枚举其活跃子工具。未保存任何场景接线，详细稳定对象键与待 DATA 核对项见 `.ai-workspace/outputs/U1-R1/AI-000190/`。
- 生成目标位于 Assets/Generated/DataTables；静态 CK01 类型位于 Assets/Scripts/DataBase/CookingData/CK01*.cs。
- CookingGameConfig 中 unknown_product_item=prd_unknown，demo_fridge_capacity_level=fcap_lv2；Catalog 解析实际容量30，getter 不设静默兜底。滚轮T8键为`fridge_scroll_threshold=1.0`、`fridge_scroll_cooldown=0.2`；旧granularity/damping已退出当前CSV/schema，F4导入后须确认旧生成资产与Catalog引用无残留。
- RecipeSlots 保留 standard_action + sub_01～08 及对应数量；序列化改动须保持兼容。KitchenAreas 既有路径和字段合同保留；`preset_tools` 原列/非空值仅为兼容历史数据，不再决定实际可用厨具，`core_facility` 仍是规则要求但其场景存在性须由清单证明。
- 本地化资产/服务：Assets/Scripts/DataBase/Localization/；组件入口 Assets/Scripts/Tools/Localization/LocalizedText.cs。
- 长期回归夹具位于 DevTools/DataTables/Fixtures/；不得再让测试依赖临时 AI inputs。
- DATA 调度与轮次入口：`.ai-workspace/workers/DATA.md`、`queue.ps1 -Action RegisterDataLane/DataLaneStatus/BindDataPipeline/ConsumeDataRound`。旧注册 worker 兼容 `CONTROL_SUBAGENT/null`；控制台提供应用可达/闲置证据后把同一 worker 原子绑定为 `THREAD`/固定可见任务，routeRevision 防止旧票据跨路由，绑定损坏拒绝回退。2026-09-23 23:24控制台正式读回THREAD/01a0ce9d-8e5a-7542-82b6-6d27609d99e0/revision1；2026-09-24控制台核实AI175 SUCCEEDED/VERIFIED，实际票据领取、检查点、Complete及原样handoff闭环，6范围452文件0差异，端到端验收完成；未导表/发布或写用户DONE。导出先写 EXPORTED，`DevTools/DataTables/Set-U1RoundStatus.ps1` 显式写 DONE 后才能幂等入队；CODE 维护工具/schema/源码，DATA 执行轮次，ENGINE 只做拆出的 Unity 接线/验证。
- 隔离导表入口：`DevTools/DataTables/Run-DataPipeline.ps1`，固定调用 `EatWhat.DataTables.DataTableImportService.ImportAllBatch`，后者复用 ImportAll 并接 PRE-1 `ShortCycleFontAtlasBuilder.BuildAll`。共享全程锁、输入/源/目标哈希冲突、已验 dependencyProof 候选摘要传递与保全式回滚在 `DataPipeline.Common.ps1`；目录发布必传 ExpectedStagedHash，零 diff、incoming、交换和回退均核候选归属；工作区固定为忽略的 `DevTools/DataTables/.batch-workspace/`。正常 DATA-Rn 仍须 USER-U1 DONE；显式 `-CurrentCsvValidation` 仅用于已授权首跑并强制禁止发布/写DONE，可选 overlay 只改隔离 CSV。
- 2026-09-23 PRE-2b CODE 核验：`Test-BatchImport.ps1` 的45项离线断言覆盖准备/备份/交换/回退失败、源/目标漂移、零变化不重写和仅一项生成物变化；`Test-DataLane.ps1` 36项与导入器21源静态编译通过，生产CSV/生成物/字体哈希不变。这里只证明源码和编辑器外状态机，不声称 Unity batch、字体资产重建或 Play 已通过；真实首跑仍归 DATA AI-000164。
- 2026-09-23 AI164真实首跑：`Invoke-DataBatchCommand` 对Windows GUI Unity.exe未显式等待，实际隔离进程仍运行时已写SUCCEEDED/0并释放锁，故首跑未通过。只中止自建隔离PID，主Editor仍响应且5,715受检文件零变化。需CODE修正进程等待/退出码/完成凭据，再由DATA复验；[证据与修复清单](.ai-workspace/outputs/U1-PRE/AI-000164/AI-000164_PRE-2b_首跑阻断与修复接力.md)。
- 2026-09-23 AI166修复专项：`DataPipeline.Common.ps1` 改用明确 `System.Diagnostics.Process` 句柄隐藏启动、等待真实退出并从该句柄取码，超时只终止本次进程树；锁覆盖退出与凭据校验。`ImportAllBatch` 只在导表和 `BuildAll` 均成功后于隔离工程内写唯一runId/nonce凭据，Runner验证新鲜时间、字体manifest与项目 `DataTableCsvParser` 的238字形结果后才登记API执行；报告分列可空 `batchProcessExitCode` 和 `runnerExitCode`。输入快照改为与复制一致的 `Assets/Packages/LocalPackages/ProjectSettings/DataTables` 五根。65项离线回归含真实 Windows GUI 延迟非零/超时归属清理/成功/缺失与旧凭据、锁持续/释放；未启动 Unity，AI164仍须由DATA恢复真实复验。[AI166修复报告](.ai-workspace/outputs/U1-PRE/AI-000166/AI-000166_PRE-2b-PROCESS-R1_修复报告.md)。

导表会校验后再写资产；新增/替换美术先比对和审批最终命名路径，再由 Unity 导入，保留 GUID 和引用。数据验证通过不等于所有未来业务链已实现。

2026-09-23 AI164最终恢复核验：此前[受限宿主许可失败](.ai-workspace/outputs/U1-PRE/AI-000164/user-retry-20260923/AI-000164_用户重试与宿主许可阻断.md)保留；两个精确启动分别通过正常宿主正式审批后，Unity0/Runner0及独立nonce均通过。baseline生成目录261文件零差异；overlay只有Localization.asset一项变化，catalog和meta/GUID不变。两个catalog均14表/120有效引用，238字形，Static未重写；主五根5722文件SHA及当前测试场景clean保持。未Publish/未USER-U1 DONE，不代过字体审批。零diff时Runner缺空JSON文件的非阻碍观察已留WARNING并用独立manifest补证。详见[正常宿主两用例报告](.ai-workspace/outputs/U1-PRE/AI-000164/host-context-recovery-20260923/AI-000164_正常宿主两用例通过报告.md)。

## 5. 描边、阴影与泡泡

| 功能 | 源码入口 |
| --- | --- |
| 正式描边参数 | Assets/Scripts/Rendering/SpriteOutline2D.cs、SpriteOutlineGroup2D.cs、SpriteOutlineDefaults.cs |
| 合并后端 | Assets/Scripts/Tools/Rendering/SpriteOutlineMergeRenderer2D.cs、SpriteOutlineMergeMember2D.cs |
| Shader | Assets/Shaders/2D/ |
| 泡泡折射与池 | Assets/Scripts/Tools/Rendering/SceneColorCapture2D.cs、EtherBubbleDistortion2D.cs、EtherBubbleEmitter2D.cs |
| Lab 驱动/装配 | Assets/Scripts/Tools/Debug/VisualEffectsLabDriver.cs；Assets/Editor/VisualEffects/；Assets/Editor/Rendering/ |

逐组像素 alpha 遮罩与显式离屏调度，组内 _MergedShadow/_MergedOutline 宿主参与普通排序；每组两张 RT，尺寸按包围盒和像素边距取8倍数，无成员级 RT。组阴影并集后使用组参数，ForceOff 使用本地；描边参与合并与参数来源是两种不同开关。正式 Outline Color 支持 HDR RGB 和 Alpha，Hover 恢复完整 RGBA。

配置与限制以[视觉特效说明索引](组件说明文档/视觉特效/README.md)为准。所有视觉改动先进入统一 Lab；保留现有探针、夹具与回归场景。

## 6. 旧 Build 链与其边界

用户2026-10-04确定新P2用新scene从头开发，下列仅是遗留参考调用链，不作为本批写回或继承目标。旧链为 CookingPrepare → IngredientInventory/SceneSwitchManager → CookingProcess → CookingManager → Room/Container → Recipe 白名单 + RuleBook → InteractionController → CutDice/Stir。

主要目录：
- Assets/Scripts/Cooking/Prepare：PrepareIngredientManager、RecipeBookManager、FridgeManager。
- Assets/Scripts/Cooking/Cook：CookingManager、CookingDropZone。
- Assets/Scripts/Cooking/CookingRoom：CookingRoomManager、Container。
- Assets/Scripts/Cooking/Interaction：InteractionController、具体加工小游戏。
- Assets/Scripts/Cooking/IngredientInventory.cs：旧跨场景托盘库存。
- Assets/Scripts/Command、Dialog、DataBase：命令、对话与旧表格读取。
- Assets/Scripts/GridPlacement、Tools：独立网格、Transition、布局及路径工具。

ShortCycleDataHandoff 已携带 RecipeId 和 PrepZoneContents；当前 Phase2TransitionAdapter 仅发 Requested 事件，不代表已把 P2 生产场景、最终成菜、评分与变体保存全部接通。CookingManager 的 isFinal 分支仍只有下一阶段注释；onChooseRoom 仍只更新房间ID。旧问题不能因 P0/P1 阶段完成自动视为修复，也不能从旧报告直接建立新任务。

## 7. Resources 字符串契约

统一辅助入口 Assets/Scripts/Tools/Tools.cs。以下旧运行链契约仍须在重命名时检查：

| 用途 | Resources 路径 |
| --- | --- |
| 食材实体 | Ingredient/{IngredientData.name} |
| 加工控制器 | Cook Interaction/InteractionObjects/{ToolTag} |
| 容器/工具表现 | Cook Interaction/Container_Interaction/{ContainerTag}；Tool_Interaction/{ToolData.name} |
| 食材/结果表现 | Cook Interaction/Ingredient_Interaction/{name}_{ToolTag}；Results/{ProcessedIngredientData.name} |
| 菜谱详情/冰箱说明 | Prepare/RecipePage/{Recipe.name}；Prepare/FridgeIngredientList/{Recipe.name} |
| 命令默认绑定 | Command/DefaultCommandBinding.asset |
| 对话角色图 | DialogSystem/{dialogSheetName}/{characterName} |

这些字符串关系没有 GUID 自动重命名保护；目录存在不等于所有动态名称都能命中。关键资源变更需查调用方并在对应业务场景验证。

## 8. 验证与维护

- 数据：DevTools/DataTables/Validate-DataTables.ps1、Test-Validate-DataTables.ps1、Test-CK01BSchemas.ps1、Test-CK01BImportContracts.ps1、Test-BatchImport.ps1；真实 Unity batch 结果必须与离线回归分开记录。
- 短循环：DevTools/ShortCycle/Test-CK01CShortCycle.ps1；坐标空间另走 CoordinateSpaceRegression/Test-DragLimitCoordinateSpace.ps1。
- 视效：DevTools/Rendering/Test-SpriteOutlineShadow.ps1、Test-OutlineMerge.ps1、Test-EtherBubbleDistortion.ps1。
- 工作流：`.ai-workspace/tests/`；归档校验/查阅：`DevTools/Workflow/Archive-Stage.ps1`。任务用量入口为 `DevTools/Workflow/UsageMetrics.ps1`（队列生命周期写 `runtime/usage-YYYYMM.jsonl`）、`Initialize-TaskUsage.ps1`、`Invoke-TaskTool.ps1` 与 `Get-UsageReport.ps1`；不可见 token/model 必须为 null+原因。统一验证入口为 `DevTools/Verify-All.ps1`，按 Workflow/ShortCycle/DataTables/Rendering profile 去重调用既有 runner，中途仅增量解析，最终失败码向上传播。
- 离线测试不替代 Unity GPU、实际事件、生命周期与用户手感验收。源码修改后仍须 Compile/Console，序列化改动须先读现场、优先 MCP、复核差异和清理本任务临时变化。
- 历史未保存恢复点仅从归档提取到独立缓存；不覆盖当前生产资产。
- 新开发同步维护本文件、项目基线和适用组件说明；日常不再追加长篇过程日志到页首，过程证据写到任务输出并在阶段末归档。

H后待办归属：用户于2026-09-22选择 F-α；MIG-F04、MIG-F01、A0、A1从AI-000146起串行推进。客户端配置MIG-F03、E2功能命名MIG-F05、官方AI接口评估MIG-F07仍见E1/H报告；F-α之外不自动展开业务或清理副本。

2026-10-05 AI199 CODE 专项接口更新（非完整复核）：Assets/Editor/DataTables/DataTableImportService.cs 的 BuildBatchDependencyProof 与 DevTools/DataTables/DataPipeline.Common.ps1 的 Confirm-DataBatchDependencyProof 已统一将 localFileId 视为非零有符号 Int64，涵盖普通生成根、共享 Texture2D 主对象、唯一 Sprite 子对象及 textureLocalFileId 归属比较。入口 Test-NativeRootProof.ps1 以真实负 Sprite ID 的 JSON 往返和非法身份反例做编辑器外回归；不改变 schema2、GetDependencies 递归闭包、旧 Spine 排除或 AI197 发布摘要绑定。真实 Unity 导入/发布仍由原 DATA-R1 核验；不提升完整核验日期。
2026-10-05 01:58 DATA-R1 实际发布专项（非完整复核）：原 AI-000194 attempt4 从 USER-U1-R1 DONE 清单完成新的无 Publish 与独立 Publish 两轮 Unity 6000.3.24f1 隔离导表，均 Unity0/Runner0、同轮 nonce、171 生成根/198 依赖、catalog 与候选树证明通过。两轮原始日志各有精确身份批准的旧 Sample/output4 Spine 错误 2 条、阻断 0；真实负 Sprite localFileId -6724980738021171820 已进入共享 Texture2D+Sprite 证明。审定 49 新资产+49 新 meta+33 更新资产，无删除或既有 meta 改写；仅 Assets/Generated/DataTables 事务发布，现目录 SHA 3ED464609FE45E3C3F908D62A2129B85AF472C5274195001A161FFA6D12F0A93 与发布轮候选一致，事务备份等于原 SHA 2D20C2162D7CDEF1B98828D82C55F9487317530809103FFE9A201DDF51A58217。14 表完整静态校验 0 错误/46 警告，场景 inventory 仍 UNVERIFIED、isFullyVerified=false；P2 新场景延期、Excel 显示待验、旧 AI170 字体停线和既有三次失败/恢复史均保留。证据 `.ai-workspace/outputs/U1-R1/AI-000194/AI-000194_恢复轮次4_正式发布报告.md`；本条只更新数据生成物状态，不提升全项目完整核验日期。
2026-10-05 PRE-5b 工作流工具专项（不提升全项目完整核验日期）：队列 Complete 引入预声明 acceptance 合同与证据比较；Poll/Complete/Checkpoint 使用小回执及任务目录全文路径，跨线程接力读取 messagePath。共享 repair-budget.ps1 将根修复跨模式限两轮，light/full 监管由 watch.ps1 只读观察，持续 inbox 去重与单批次适配器；无正式桌面 run-now，未声称已上线。用量由 provider 精确事件及带 task/attempt/requestId 的 Coplay 日志补充，缺源保留 null。入口 DevTools/Workflow/USAGE_REPORTING.md、.ai-workspace/toolkit/WF.md#tk-wf-04，离线测试归 Verify-All Workflow。

### 监管回传入口补充（2026-10-05）

DevTools/Workflow/Manage-SupervisionHandoff.ps1 承接 queue/supervised 的完整结算回执，将执行器一次投递与控制台接收去重分开；使用流程见同目录 HANDOFF.md。WATCH inbox 的 PENDING 仅代表采集，不能替代投递回执或独立验收。此为工作流专项定位更新，不提升项目完整复核日期。


2026-10-05 AI208工作流专项：queue.ps1新增共用Sync-TaskTurnGate与显式ResolveControlRejection；supervised.ps1保存软限SHA交接、正式取消scope收口；Invoke-TaskTool区分实际完成与观察，WATCH软限按任务去重。Workflow新增Test-TurnSoftRepair/Test-ControlCompletedReview，原回传绑定/UTC/修复预算继续回归。仅此模块及轻量入口核对，不提升完整复核日期；无Unity资源变化。

2026-10-05 FONT-R3 / AI207 只读预检阻断：FontSwapOnce.Run(true)第84行要求三现有材质与Static默认材质使用同一Shader；实际三材质及各旧源为 TextMeshPro/Distance Field，Static默认为 TextMeshPro/Mobile/Distance Field。三源/目标均存在且clean，不是缺资源或未知dirty。Run(false)、Play、Build及截图均未开始；3399项前置序列化文件SHA/时间戳无漂移，Static精确SHA与三材质GUID不变。保留现场，仅只读诊断，不改Shader或重试。证据 .ai-workspace/outputs/FONT-R-PRE5c/AI-000207/REPORT.md；R4用户视觉与E2闸门保持。

2026-10-06 AI210：FontSwapOnce新增CheckMaterial，精确核TextMeshPro/Distance Field和Mobile/Distance Field的名称/GUID、36项共有属性及Standard额外30项，源/目标必须同Shader；允许Standard源/目标读取Mobile Static图集，保留两种同Shader组合，反向组合拒绝。Static图集身份/尺寸/有限正GradientScale与dirty逐项核验。CopyProperties→Static四属性→原生ratio及171映射/保存顺序不变；源码170行的必要例外及离线覆盖见DevTools/Fonts/README.md。AI210输出在.ai-workspace/outputs/FONT-R-PRE5c/AI-000210/；修复后真实Editor编译/Run(true)及原207生产迁移仍须后继，AI206不能代替本版编译。未改生产资源、工作流源码或原207失败/恢复状态。

2026-10-06 AI211：同工程 Unity 6000.3.24f1 已完成 AI210 当前源码真实编译（Csc/域重载与 PDB 源 SHA 对应），一次 FontSwapOnce.Run(true) 返回171；3399项序列化资源及Static精确SHA不变，三源/目标材质Shader与GUID、场景clean及临时资源释放通过。Console零Error，完整重编译记录30条既有源码/场景警告。本次仅完成编译/只读预检，现已控制独立7/7及3399资源核验；原207于01:33正式领取续行，生产ratio发布/迁移幂等/截图/Build/R4视觉及E2仍待实际完成。证据 .ai-workspace/outputs/FONT-R-PRE5c/AI-000211/REPORT.md；不提升完整复核日期。

2026-10-06控制独立核AI211：7/7与3399序列化/源码SHA通过，真实编译与一次Run(true)有效；新增queue ResumeApprovedTask/approved-resume.ps1仅为原获批失败任务保留历史的单次续行入口，原失败门不全局放松，repair与恢复配同slot，Poll重核证据/路径集合/源码资源SHA并消费许可。Workflow专项回归Test-ApprovedResume。生产迁移/R4仍未完成；完整项目复核日期不提升。

2026-10-06 AI207获批续行attempt2：fresh源码/3399资源基线通过后，唯一Run(false)返回changed40，当前171字体/材质配对匹配；但Static文件SHA从E8779503…B8EE1220变为2C531627…48D0E3D，精确文本差异仅内嵌材质_ScaleRatioA 1→0.9、_ScaleRatioC 1→0.73125。触发Static全字节保护停止；场景及五Prefab已保存，三外部材质Shader/GUID及字节不变。未二次幂等、Play、Build或完成五视图；只读诊断后保留部分成果与迁移前后备份，不恢复/重试/改保护manifest。Console0 Error、动态0/0、场景clean。详见 .ai-workspace/outputs/FONT-R-PRE5c/AI-000207/attempt2/REPORT.md；原首次失败保留，R4/E2仍未通过。

2026-10-06最新批准：用户在主控制台直接批准“批准 继续修复”，已覆盖此前AI207 attempt2草案待审状态。正式AI212登记最新ratio失败后的同根只读诊断，02:05:25实际领取；诊断后仅安排一张必要CODE修复（root207第二slot），先证明Static内存/磁盘副作用保护，再执行已批仅Static恢复及真实隔离验证，最后正式续原207。当前生产现场未恢复，旧2错误/1恢复及七份前后备份保持。授权 .ai-workspace/inputs/FONT-R-PRE5c-20261005/AI207_StaticRatio修复恢复批准_20261006.md；本轮证据 ratio-repair-approved-20261006。


2026-10-06 AI213：FontSwapOnce改为外部材质先绑定再font，并用固定E877 SHA、入口A/B/C=1和font/material/atlas内存快照保护加载、绑定及每次实际保存前后；BeginStaticGuard/Check/Around供ENGINE覆盖外部受控渲染，异常锁存禁止重采基线，BeginIsolatedStaticGuard仅供已核E877载荷的隔离原生验证。源码262行（原150未达，+92必要保护说明见Fonts README）；一次Fonts Final2/2、1726断言、258反例、59守卫用例与177 Shader合同用例通过，真实Unity6/TMP DLL离线编译通过。当前3399资源保持AI207失败现场，Static仍2C531…，本卡不恢复/不操作Editor。先控制独立核验本版代码，再由新ENGINE四边界原生验证与独立验收，之后才单Static恢复、真实编译/唯一Run(true)、正式续原207；不能用现有171全匹配跳过Bind证明修复。旧错误/attempt/预算、用户提交及R4/F/P2/Excel/E2闸门保持。证据 .ai-workspace/outputs/FONT-R-PRE5c/AI-000213/REPORT.md；仅专项核验，不提升完整复核日期。

2026-10-06 AI214 原生隔离验证：真实Bind/渲染/保存/负例等七项通过，首载和清理未通过。原MCP删除拦截保留，用户已删除旧临时物且domain清洁。原报告 .ai-workspace/outputs/FONT-R-PRE5c/AI-000214/REPORT.md；七项证据和失败史不重签。
2026-10-06 补验续行入口：.ai-workspace/approved-resume.ps1 的 APPROVED_VALIDATION_RESUME；边界见 .ai-workspace/protocol/SUPERVISED_FULL.md，真实 queue 回归 DevTools/Workflow/Test-ApprovedResume.ps1。只续原只读诊断，错误与根预算保留。

2026-10-09 FONT-R 最新：AI217原生9/9与AI218恢复12/12的控制签收保持。原207获新直接批准后，attempt4唯一第二Run(false)成功changed0（102.9880915秒），与attempt3成功首调changed0构成幂等实证；原三次失败/审批拒绝及root额度3、216已配第二根恢复保留。171配对、三GUID/原生ratio、原七备份与六文件审定diff、五视图/CoverTitle前后、SpacesFontStatic、一次Windows Build及Dynamic清空等原11项机器匹配。Build Succeeded、0 Error/17 Warning；Unity原生Console 0 Error/18 Warning，两条Shader warning被MCP错标Exception的原文保留。Static五个边界均E877，八保护对象JSON/dirty不变；3399项相对本轮基线仅Dynamic空图集单字节cd→00，最终0/0。Play和Build后场景配置不变；两份新加载OTF的NotEditable字体纹理缓存dirty保留，未保存/清标记。截图观察及成因未知记录保留。用户2026-10-09明确“人工视觉验收通过”，R4已正式签收；用户提交待完成，E2未启动。见[本轮报告](.ai-workspace/outputs/FONT-R-PRE5c/AI-000207/attempt4/REPORT.md)，正式结算/控制验收以队列回执为准。
