# 《是啊，吃什么》代码结构与修改导航

最后结构一致性复核：2026-09-17。范围：源码入口、Build Settings、Packages、关键 Scene/Prefab/生成数据、Resources 路径和 Git 差异；Unity 只读确认正式 P0 编辑态、场景 clean、Console Error=0。本次运行既有离线回归，不重做全项目 Play Mode；本轮视觉与交互沿用已落账的用户验收。

最后专项核验：2026-09-21（MIG63 E1/H）。用户确认37bb854人工终验通过，六场景/meta及字体SHA与最终快照一致；main和unity6.3-baseline已指向该候选，backup保持起点。原工作区content已吸收同一版本，TMP84/LFS与九组离线回归通过；Unity 6接棒导入等待Spine Examples单文件API更新确认，H尚未通过、冻结尚未解除。定向重序列化安排在H后、首次内容修改前单独提交。详情见[E1/H报告](.ai-workspace/MIG63/E1H_接棒与后续项.md)。完整结构复核日期仍2026-09-17，不将本轮专项核验标成全项目重新验收。

本文件记录当前结构。整理前逐次变更与失败/恢复记录完整保存在 [.ai-workspace/archive/2026-09-17](.ai-workspace/archive/2026-09-17/README.md)，不再将历史状态堆在页首。完成度和下一轮边界见[项目基线](项目整体阅读理解与推进基线_2026-08-14.md)。

2026-09-20 资产清理专项核验：用户确认 CookingPrepare 已弃用，现有 Build 列表仍保留该场景，本次未调整发行入口。已从 Resources/Customer、Resources/DialogSystem/TestDialog、Sprites/Cooking/Customer 删除 14 张已确认的第三方游戏角色图及 meta；TitlePage、DialogManager、DropDown 三个 Prefab 的对应图片引用/对象名已清理。StreamingAssets/Dialog.dat 保留 TestDialog 表名，内容改为单行中性测试（Uid=1、DefaultNextUid=-1、无角色图片/选项）；两组 TestCustomerPlaceOrder 表不变。旧 E 盘 Dialog.xlsx 不属于本次工程清理范围，再导入前须同步去除旧测试内容；可审阅的当前数据见[Dialog.cleaned.json](.ai-workspace/outputs/control/资产转让审查_20260920/后续清理/Dialog.cleaned.json)。当前 ShortCycle P0/P1 入口与运行架构未改；核验范围和残留扫描见[处理记录](.ai-workspace/outputs/control/资产转让审查_20260920/后续清理/处理记录.md)。

## 1. 工程入口

| 项目 | 当前事实 |
| --- | --- |
| 引擎与渲染 | Unity 6000.3.24f1、URP 17.3.0；原工作区已切换同一基线，H导入中 |
| 输入 | 本迁移分支Input System 1.20.0、Uniform；Windows滚轮已签收，Mac适配见MIG-F01 |
| 当前 Build 列表 | ShortCycle_P0P1 index0、CookingProcess index1，Profile不覆盖，Prepare文件保留；原目录已同步 |
| 本轮开发/验收入口 | Assets/Scenes/Cooking/ShortCycle_P0P1.unity；两目录均已加入Build，H完成前保持业务冻结 |
| 视觉统一试验 | Assets/Scenes/ToolTests/VisualEffectsLab.unity |
| 滚轮独立试验 | Assets/Scenes/ToolTests/FridgeScrollLab.unity |
| 盘点 | Assets/Scripts 249 个 C#；Assets/Editor 63 个 C#（含4个保留迁移验证文件）；Assets/Scenes 15 个场景；Prefabs/Resources 共 71 个 Prefab |
| 配表 | 14 个 schema；Assets/Generated/DataTables 实际 122 个 asset（含 catalog、共享资源） |
| 程序集 | 项目主要使用默认 Assembly-CSharp / Assembly-CSharp-Editor；第三方程序集另计 |
| 验证 | DevTools 离线编译/契约测试、Unity 场景探针和人工验收；Assets/Tests 当前为视效夹具，不等于已有 NUnit 测试程序集 |

Packages清单/锁文件及LocalPackages/com.coplaydev.unity-mcp固定Editor包进入版本管理。DevTools/MCP记录服务端10.1.2、源码提交和uv锁文件；Editor为同提交10.1.3-beta.4，迁移目录6.3连接与实例路由已实测。自动客户端改写已用10文件本地补丁关闭；主动Configure仍可用，未提供完整服务端离线镜像。详见DevTools/MCP/README.md。原目录H接棒已恢复相同本地包及独立venv，待导入结束后验证新实例路由。

以上版本表指已验收E1基线及正在H接棒的原工作区。独立Eat-What-U6副本已导入6000.3.24f1：URP17.3.0、Input System1.20.0、UGUI2.0.0、Cinemachine2.10.7；Coplay已收敛为工程内固定包；仅顺序连接MIG实例，原队列未接入。API Updater把HorizontalPlayerController.cs与QuickAddForce.cs的Rigidbody2D.velocity改为linearVelocity。M2已保存Compatibility=false、无兼容宏，Windows构建通过；TMP目录已升级为84文件，M3R按N1取消忽略并随Git/LFS跟踪，保留原GUID和快照。精确变化和人工复核见[M2/M3执行结果](.ai-workspace/MIG63/M2M3_执行结果与人工验收.md)，原目录已接入相同资产，H运行回归完成前仍不宣称接棒通过。

迁移侧验证工具变化：DevTools/Rendering/Test-OutlineMerge.ps1、Test-EtherBubbleDistortion.ps1、DevTools/ShortCycle/Test-CK01CShortCycle.ps1及CoordinateSpaceRegression/Invoke-CoordinateCompilation.ps1按项目版本定位Hub/旧Editor；泡泡测试引用URP17的2D.Runtime程序集。H磁盘接棒已同步这些工具，九组离线回归通过。原临时验证链已移除；本轮按用户决定长期保留迁移侧Assets/Editor/MIG63Validation/MIG63WheelProbe.cs、MIG63TomatoProbe.cs、MIG63SpineSceneProbe.cs和MIG63ValidationSession.cs，菜单Tools/MIG63/Validation，说明DevTools/MIG63/README.md。两侧新增DevTools/GitHooks字体工作树/index保护，最终机器结果不覆盖硬件鼠标和同分辨率全视觉对照。

引擎迁移须同时检查Assets内依赖：Spine 3.8（2021-11-10）、Text Animator 2.3.1、NuGet/EPPlus及MCP相关DLL。旧链CookingPhaseState.cs、CameraFollower.cs使用Cinemachine 2 API；自制视效通过相机回调及离屏RT调度，Renderer2D的Renderer Features为空。当前执行依据为[MIG63迁移基线](.ai-workspace/MIG63/迁移基线.md)，不沿用历史6.6候选评估作为目标。

Spine保护范围：Assets/Scenes/Spine Sample.unity和Assets/Scenes/ToolTests/HorizontalPlayerControllerTest.unity。M4在这两处保留原Prefab/对象身份，使用场景override绑定现有Tomato 3.8.99；动画控制器为Avatars/Tomato/Tomato_MIG63.controller（idle/walk/kick，Idle/Walk/Kick触发器）。三个SkeletonMecanim的MeshRenderer磁盘均为禁用，由已有SpineRuntimeMeshRendererBootstrap在Start启用，避免场景恢复旧网格产生Invalid worldAABB。CharacterEquipment采用Tomato_Non和7部位；Sample的Bottom槽保持Test_EquipBottom按钮契约。HorizontalPlayerController保留Rigidbody2D移动和Animator路径，spineAnimation仍为空；另一SkeletonAnimation用法由长期Tomato双路径探针覆盖。旧Sample/output4 3.8.75及Prefab/runtime/Shader未覆盖或删除，直接读取旧骨骼仍是已登记历史缺陷。ShortCycle/两个Lab不依赖Spine。保存场景与双路径GPU通过并不表示全部皮肤、混合模式或重打包均验收。

迁移侧滚轮专项：Assets/Scripts/MouseInteractive/MouseManager.cs 保留序列化字段，补Unity 6 Windows Uniform输入到旧阈值单位的120换算、冷却期最多一步积累、目标/层/失焦清理，冷却改用unscaled time。原工作区已同步该实现，H通过前继续冻结业务开发。配置人员入口见[区域滚动说明](组件说明文档/区域滚动_配置与复用说明.md)；用户已明确复验完成，MIG-P04关闭为批准的交互调整（一次派发一页，短促连拨可能含一次延后派发）。MIG-F01承接统一单位/手感小项，Mac阈值尚不适配，Mac使用前单独验证；旧2022回退基线没有上述新语义，不再用于打开已接棒目录。Windows API保持D3D11优先/D3D12第二，Auto=false。

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

跨 Space 才调用 CameraPan 并持有输入锁；同一 P0 下 Cover/Catalog/Browse 不新增镜头移动。当前正式曲线为 2-key Linear、0.45 秒；空曲线源码回退为线性，中途返向从真实当前位置重新发起，取消请求不晚到写回。

Cover 隐藏旧书底/展开页；Catalog/Browse 恢复；目录签只在 Browse 显示；CurrentRecipeTab 保持 Browse-only。Scene 的唯一 Host 是 Managers/PresentationDirector；旧 Presenter/Builder 兼容实现不能替代此入口。

## 3. 冰箱与通用滚动

```text
MouseManager → MouseScrollableObject → ShortCycleScrollAdapter.MapPhysicalStep
→ ActionRouter → Session → FridgeBinder → FridgeCatRig → ScrollArea_Controller
```

- 正式 P0 的 ScrollRegion 启用 Invert Physical Scroll Direction；业务 Dispatch(+1)=下一层、(-1)=上一层不变，Lab 保留独立参考方向。
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
- 导入入口：Assets/Editor/DataTables/DataTableImportService.cs，Tools > Data Tables > Import All。
- 唯一聚合入口：Assets/Scripts/DataBase/CookingData/CK01GeneratedDataCatalog.cs；资产 Assets/Generated/DataTables/CK01GeneratedDataCatalog.asset。
- 生成目标位于 Assets/Generated/DataTables；静态 CK01 类型位于 Assets/Scripts/DataBase/CookingData/CK01*.cs。
- CookingGameConfig 中 unknown_product_item=prd_unknown，demo_fridge_capacity_level=fcap_lv2；Catalog 解析实际容量30，getter 不设静默兜底。
- RecipeSlots 保留 standard_action + sub_01～08 及对应数量；序列化改动须保持兼容。KitchenAreas 既有路径和字段合同保留。
- 本地化资产/服务：Assets/Scripts/DataBase/Localization/；组件入口 Assets/Scripts/Tools/Localization/LocalizedText.cs。
- 长期回归夹具位于 DevTools/DataTables/Fixtures/；不得再让测试依赖临时 AI inputs。

导表会校验后再写资产；新增/替换美术先比对和审批最终命名路径，再由 Unity 导入，保留 GUID 和引用。数据验证通过不等于所有未来业务链已实现。

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

旧链为 CookingPrepare → IngredientInventory/SceneSwitchManager → CookingProcess → CookingManager → Room/Container → Recipe 白名单 + RuleBook → InteractionController → CutDice/Stir。

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

- 数据：DevTools/DataTables/Validate-DataTables.ps1、Test-Validate-DataTables.ps1、Test-CK01BSchemas.ps1、Test-CK01BImportContracts.ps1。
- 短循环：DevTools/ShortCycle/Test-CK01CShortCycle.ps1；坐标空间另走 CoordinateSpaceRegression/Test-DragLimitCoordinateSpace.ps1。
- 视效：DevTools/Rendering/Test-SpriteOutlineShadow.ps1、Test-OutlineMerge.ps1、Test-EtherBubbleDistortion.ps1。
- 工作流：.ai-workspace/tests/；归档校验/查阅：DevTools/Workflow/Archive-Stage.ps1。
- 离线测试不替代 Unity GPU、实际事件、生命周期与用户手感验收。源码修改后仍须 Compile/Console，序列化改动须先读现场、优先 MCP、复核差异和清理本任务临时变化。
- 历史未保存恢复点仅从归档提取到独立缓存；不覆盖当前生产资产。
- 新开发同步维护本文件、项目基线和适用组件说明；日常不再追加长篇过程日志到页首，过程证据写到任务输出并在阶段末归档。
