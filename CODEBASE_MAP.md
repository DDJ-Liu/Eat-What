# 《是啊，吃什么》代码结构与修改导航

2026-11-19 0.1.3 发布数据可随 Windows 成品加载。`CatalogBlobBuildProcessor` 将已保存生成资产的序列化信息与磁盘字段交叉核对，`CatalogBlobSerializer` 据此保留合法空角色形象。已绑定的形象沿稳定引用导出，无法证明为空的损坏引用会报错。`DevTools/Build-Demo.ps1` 使用当前快照和 `BuildPrepared` 构建启动页与街景。此次局部核对覆盖发布调用链和普通成品启动退出，详见[发布数据说明](DevTools/BuildRelease/README.md)。

2026-11-19 局部核对了 Street 剧情气泡。`NarrativeBubbleCanvasRenderer` 在停用的气泡节点上创建 `TextMeshProUGUI`，先设定 UI 正交测量，再按 `Max Text Width` 获取首选尺寸。自然触发的冷却归发起者，`DialogueLines.speaker` 决定显示角色。当前工作簿与正式 CSV 的 `test_default` 保留 `chr_test_passer_a#2` 的双人池，番茄与基础路人的池留空，土豆保自身池。`test_second` 为番茄指定单人三句池，土豆使用 `test_street_03`。本次核对范围为气泡渲染、两状态及配表，完整工程核验日期沿用原记录。见[两项复测说明](docs/demo-0.1.3_人工检查说明.md)。

2026-11-17 AI565/V013-16局部原生复核：用户已完成手工并新增Street直接预摆GenericNPC/chr_test_potato，引用同场景Runtime/Registry/View、同根Marker/Shell及三挂点，自身npc_visual_tomato和amb_test_a有效且Play保留；无Characters/placement行合法。自然scn_test_street/test_default受管精确tomato、passer_a、passer_a#2三者各活动形象1，Registry活动actor5含extra和无Shell主角，全部Shell5含inactive passer_b/活动4。第2轮原3合同CONTROL独审通过，Console新Error0/Warning6，停止后clean，14限定文件不变；第1轮CLI超时FAIL保留，验收累计2/3、根续行1/5，无生产修复。新增五列[人工检查说明](docs/demo-0.1.3_人工检查说明.md)；HeadAnchor局部0及表现、状态切换/气泡/F8仍由用户检查。原17须用户明确体验通过，未Build。完整核验日期仍2026-09-17。

2026-11-16 AI563/V013-14已完成Street原生保存与原三合同读回：24源/meta对应12主资产导入；正式22表ImportAll一次0error/56warning/4info，新增28生成资产，八新表行数3/4/7/0/1/2/8/3。Street根新增NarrativeRuntime_TUNE（RuntimeEntry＋BubbleView/Renderer）、NarrativeTargetRegistry_TUNE、NarrativeSceneStateApplier_TUNE、NarrativeSceneDebugPanel_TUNE；依次显式接config/Catalog、真实PlayerRoot_TUNE/Foot_TUNE、GenericNPC/placeholder/View、Applier/Registry。View序列化surfaceProvider接Renderer，Renderer明确Cam_Street和现WithFallback字体，非TMP_Settings回落。玩家Marker=chr_test_player；原TomatoIdleNPC_TUNE Marker=chr_test_tomato，原位加一个自身番茄形象，Trigger场景引用齐全、池空；三test_street_01/02/03新锚点为_TUNE初值。37原对象父级/位置保持，字体/Prefab/配置参数及源表未改，Scene clean。未Initialize/ApplyState/Play或签收表现，V25/模板UNVERIFIED原报告保留；USER/15～17另行推进。实际对象表/初值/五列说明见[街景状态配置](docs/街景场景状态配置与使用说明.md#v013-14-street实际接线2026-11-16)，完整核验仍2026-09-17；下文原14待做文字属于各卡当时范围。

2026-11-16 AI562/V013-12局部数据入口：`DevTools/DataTables/Authoring/DataTables_Authoring.xlsx` 已成为 22 页含测试占位的权威源；新增 3 角色、4 ambient 段/7 句、1 场景/2 完整状态/8 placement、3 池及 11 本地化 key。对应 8 份 CSV 由现 Authoring 出口忠实派生，CGs 和 13 份 Cooking CSV 未改。正式 22 表 0 error/46 warning，新增两条 V-25 为 Street 场景标记尚未接线的 UNVERIFIED；V013-14 原生导入/接线和用户体验仍待。字段与清理路径见[八表说明](docs/剧情八表配表说明.md)，完整核验仍2026-09-17。

2026-11-16 AI561/V013-09局部入口：现 Editor/DataTables/NarrativeTableValidation.cs 内新增纯离线 NarrativeSceneContentAudit，经原 EnableProject→SceneStates 结果追加 V-25 warning。沿原 CSV/schema/有效值与精确场景名读取，只核 actor 的缺锚点、空锚点无完整ID预摆、预摆无状态条目；当前一层 GenericNPC PrefabInstance 的 GUID+整数localID/角色ID覆盖可读，空坏或超支持表示明确未核。原 valid/sceneInventory 门和 V13～V24 保持，专用长期文本夹具及真实报告回归见 [DataTables入口](DevTools/DataTables/README.md)。无场景/Prefab/表写入或 Unity 操作，14/USER未代签，完整核验仍2026-09-17。

2026-11-16 AI560/V013-08局部入口：LongLoop新增条件编译NarrativeSceneDebugPanel及meta，InputSystem F8/IMGUI路由到原Applier.ApplyState与Trigger→Driver→Model共享取句；强制只跳准入，保正常截止，不重建模型。Application发布最后成功scene/state，目录可选INarrativeSceneStateList与Registry.GetActiveActors提供真实只读列表，不建并行权威状态。三个专用强制入口与面板在Release确实不存在，原业务API保持；26路由/原Model82/三组合各6元数据及原LongLoop四门通过。见[配置与五列后验](docs/街景场景状态配置与使用说明.md)，无原生接线/GUI或包体验收，完整核验仍2026-09-17。

2026-11-16 AI556/V013-05局部入口：LongLoop新增NarrativeSceneStateApplication（唯一九步/重应用所有者）和同名可挂载NarrativeSceneStateApplier组件及meta；经原Runtime/目录的可选INarrativeSceneStateDirectory查场景/全部placement，原主接口保持。Registry兼容追加停用预摆查找，Shell追加单所有者原形象保留/恢复，Trigger公开Configure在首次Update前接场景依赖并保原模型。原默认LongLoop末尾追加实际34/6/11用例，原四模式/断言保持；[固定说明与五列人工路径](docs/街景场景状态配置与使用说明.md)。只源码/离线，无场景/Prefab/数据写，14实际接线及USER仍待；完整核验仍2026-09-17。

2026-11-16 AI559/V013-13已完成原生NPC壳/形象分层：原TomatoIdleNPC经MoveAsset改为`Assets/Prefabs/LongLoop/NPC/GenericNPC.prefab`，原GUID18482e8563931f945979fc4facbec5b8及meta字节保持；Foot/Visual原变换、DepthScale/SortingGroup/YSorter及引用保持。根已挂NpcShell/ActorMarker/AmbientBubbleTrigger，三个稳定引用绑定，HeadAnchor_TUNE默认零点待用户调，Visual挂点子数0；Trigger仅接同根marker/shell，场景引用和id/池默认空。两新形象为Visuals/npc_visual_tomato.prefab与Visuals/Placeholders/npc_visual_placeholder.prefab，番茄保原Spine/装备/材质/idle，Square占位最小白块。原三合同原生3/3、Console新增Error0、Street clean且磁盘未改；未Play/视觉签收，14场景接入未完成。当前配置入口见[街景NPC说明](组件说明文档/视觉特效/街景NPC_Prefab配置与使用说明.md)，完整核验仍2026-09-17。

2026-11-16 AI555/V013-11局部入口：CookingData/CatalogBlobSerializer写2.5.1并读旧2.4.1，AssetTypes纳入八表、ValueTypes纳入三Optional；GameObject用prefab稳定键。EncryptedBlobSource新增双resolver，Catalog新增releasePrefabs与ConfigureBuildReferences；Editor/Build原Capture兼容壳转CaptureReferences，快照校验、build-only引用及linker同步扩展，四阶段PS入口保持。原ImportContracts新增实际22非空往返/Runtime查询和旧169资产/14表/Sprite导出冻结比对；原Blob显式Legacy14与BuildRelease仍独立。见[BuildRelease](DevTools/BuildRelease/README.md)及八表/运行时说明；仅源码/离线，原生导入/包体待14/17，完整核验仍2026-09-17。

2026-11-16 AI554/V013-10局部入口：CK01CharacterData保原street_visual_prefab字符串，仅加派生GameObject street_visual_prefab_asset；DataTableImportService.TryApplyRow沿原精确基名/批准Visuals目录把唯一候选绑定，缺失/歧义/不可加载警告并清引用，空列无警告。NarrativeDataAdapter与RuntimeEntry新增TryGetCharacterVisualPrefab按基础角色id读当前Catalog源；Core无Unity/无反向依赖。实际字段/API、空值矩阵及05/11承接见[八表说明](docs/剧情八表配表说明.md)，原ImportContracts追加真实绑定用例，原Narrative Python仅新增可选生产compile-only。只源码/离线，无生成资产或原生导入，11打包恢复/14保存另做；完整核验仍2026-09-17。

2026-11-16 AI553/V013-07局部入口：LongLoop新增NarrativeAmbientBubbleTrigger（同NPC根组件）与Driver（实际协调类），调用04/06、Registry/Shell及原GroundSpace，单NPC单模型、池setter保冷却、speaker缺失跳句。Narrative.Unity新增NarrativeBubbleView生命周期及CanvasRenderer运行时Overlay/TMP实现，只新增单向UI/TMP引用，不反向依赖LongLoop；tier四字段驱动淡入/存活/上漂淡出，同NPC替换/其它独立。原Narrative入口追加实际联测，LongLoop当前依赖编译；[配置及五列人工说明](组件说明文档/视觉特效/剧情气泡配置与使用说明.md)。只源码/离线，尚未实际挂载/画面验收，13/14及USER保持；下文早期“无消费者”属当时边界，完整核验仍2026-09-17。

2026-11-16 AI552/V013-06局部入口：`Narrative/Core/NarrativeAmbientBubbleModel.cs`为每NPC唯一气泡取句/冷却所有者，复用INarrativeDataDirectory/链及Resolver trigger快照，外部传秒数、地面距离、随机能力；严格消费autoBubbleRadius/autoBubbleCooldown，换池清进度保截止时间，坏池/非ambient拒绝并去重诊断，命令忽略但照常发句。结果只含文本key/说话者等纯数据，无Unity/视图/LongLoop/普通剧情FSM或业务状态策略。原Narrative入口追加真实模型测试；说明原位补[对话参数](docs/参数调整说明_对话.md)，后续07/05消费实际接口。完整核验仍2026-09-17。

2026-11-16 AI551/V013-04局部入口：`Assets/Scripts/Narrative/Unity/NarrativeRuntimeEntry.cs`持显式演出配置/Catalog引用和必要目录快照，通过原NarrativeDataAdapter/Resolver提供目录、对话与独立trigger取值；缺失/读失败明确不可用、无业务进度状态。LongLoop只追加单向Narrative.Unity引用，Unity/Core不反向引用街道；Catalog既有编辑/开发SO与加密发布源策略、Core/Adapter及配置资产保持。原Narrative入口追加实际组件用例，原LongLoop默认入口补当前真实Unity依赖编译。说明见[运行时接入](docs/剧情运行时接入说明.md)；未做14场景接线、11八表发布或17包体，完整核验仍2026-09-17。

2026-11-16 AI550/V013-03局部入口：`Assets/Scripts/LongLoop/NpcShell.cs`及新meta仅管理稳定Foot/Visual Mount/Head Anchor引用、运行时非空形象子对象替换/null保持及朝向；直接复用原DepthScale的ResolveFacingMagnitude/TrySetFacing，保主角、缩放/排序源码和配置。默认LongLoop入口追加实际壳+DepthScale用例，原四模式及548新增44保持。AI550原交付仅代码/离线；随后AI559已原生完成通用壳和两形象Prefab，当前资产按页首及原[街景NPC说明](组件说明文档/视觉特效/街景NPC_Prefab配置与使用说明.md)，14场景接线仍未完成。完整核验仍2026-09-17。

2026-11-16 AI548/V013-02局部入口：LongLoop新增 `NarrativeActorMarker.cs`、`NarrativeAnchorMarker.cs`、`NarrativeTargetRegistry.cs`，三个同名可挂载组件及新meta。Registry独占本场景角色/锚点清单，按启停收集/注销、重复保先登记、显式本场景Player Transform及缺失诊断；LongLoop新增单向Narrative.Core引用，直接复用NarrativeCommandTargetParser，不反向引用LongLoop。原StreetSceneEntry、主角/镜头、Core语法与数据不改。默认LongLoop离线入口追加实际组件测试，原四模式保留；配置见[目标说明](docs/剧情目标标记与注册说明.md)。未做场景接线、NPC壳/状态/气泡或原生验收，完整核验仍2026-09-17；机器结果另见本卡报告。

2026-11-16 AI547 attempt2完成V013-01原生操作及原三项检查：仅导入AI549已独审的导表服务修复，正式ImportAll成功读取22表（0error/54warning/0info），Catalog含八个零行新表入口；创建并保存Assets/Settings/Narrative/DialoguePresentationConfig.asset，十组均只有一个default，数值取类型默认。编译无错误，Narrative.Core/Unity、Toolkit、LongLoop及Editor.DataTables已加载，Street仍stopped/clean、Console无新增Error。首轮旧14表硬门失败及修复/许可完整保留，旧scene UNVERIFIED未解除。证据见.ai-workspace/outputs/PLAYABLE-DEMO013-20261115/AI-000547/attempt2/REPORT.md；不代表NPC/气泡或0.1.3完成，完整核验仍2026-09-17。

2026-11-15局部补核N005/008/012数据模型、参数快照与纯对话状态机，以及N009专项静态校验、N013整链与N012-P1顺序补丁；完整核验仍2026-09-17。

2026-11-15另补核N0-05的只读模型/目录/生成行适配与相关离线入口；仅此模块局部核对，最后完整核验仍为2026-09-17。

最后完整核验仍为2026-09-17；2026-11-13仅核对本轮入口、正式Build场景、现有镜头源码/资源路径及文档结构，未冒充全仓运行复核。当前Unity6000.3.24f1、开发分支codex/unity6-followup，main用于已完成内容集成。2026-11-14仅补核N0-01骨架源码、离线入口及回归证据，并登记N0-03纯命令接口、N0-11纯推进输入模型、N0-02纯曲线抽取、N0-10纯命令调度、N0-06演出配置类型及N0-07覆盖解析，不提高完整核验日期。2026-11-15仅补核N004八表schema/通用导入/查询/作者工具、正式22表工作簿及四旧回归收口，不提高2026-09-17完整核验日期。

0.1.2长循环/可玩包已由用户整体签收；角色描边暂停。短循环的未开始规划和中断工作保留，见[下一轮承接](.ai-workspace/outputs/control/下一轮开发承接.md)。历史长页首/逐轮过程已无损留在[入口归档](.ai-workspace/archive/2026-11-13/README.md)。

## 工程与场景

正式共享Build表保持五场景：Boot、ShortCycle_P0P1、CookingProcess、ShortCycle_P2、LongLoop_Street。0.1.2的Windows64 Mono非Development构建只在本次BuildPlayerOptions选Boot+Street，不改共享场景表。源码/资源定位细节见[工程入口](.ai-workspace/codebase/entry.md)。

## 长循环与镜头

`Assets/Scripts/LongLoop/StreetSceneEntry.cs`是街景入口；正式输入`Assets/Scripts/Tools/Input/LegacyInputProvider.cs`实现既有`IGameInput`，读取旧键鼠和现Unity Input System右摇杆。当前Provider与LongLoop玩家控制器同在PlayerRoot_TUNE，Driver经现公开父级路径取得输入。

镜头配置在`Assets/Settings/Street/CameraFollowConfig.asset`及同目录现有配置。`StreetFollowDriver`持有纯状态，`StreetDynamicZoom`推进本帧Size与Ratio，`StreetLookFloat`处理鼠标/直接右摇杆目标和世界距离/速度上限，`StreetVerticalOffset`处理纵向输入，`StreetFollowComposition`按本帧几何合成通道；原`StreetFollowPipeline`持有目标/前视/回中状态。浮动有效意图暂停Shrink/Recenter及水平前视退出，解除后从当前值重新等待各自完整Delay/曲线。FOCUS不随视野比例缩放。

Driver写独立目标节点及本帧Lens状态，Composer执行唯一位置阻尼，Confiner约束边界；不新增整机相机阻尼。N0-OPT在Driver调用原世界限幅器时仅将浮动径向上限乘同帧Ratio，速度原样，Ratio=1保持原输出；源码/离线通过，补丁视觉已于2026-11-15用户验收。`StreetParallaxLayer`在Cinemachine最终输出后读相机位置，只写所属层容器并在退出恢复，数学在`StreetParallax.cs`。街景已有六层接线及用户归层/原点配置。

正式使用/调参文档维护在[docs参数说明](docs/demo-0.1.2_参数调整说明.md)、[人工检查说明](docs/demo-0.1.2_人工检查说明.md)及[组件说明目录](组件说明文档/视觉特效/README.md)。这些是项目长期源文档，不以任务attempt中的摘录为配置权威。实际数值以用户保存资产/Inspector为准。

## 短循环

Session/FSM入口为`Assets/Scripts/Cooking/ShortCycle/ShortCycleSessionManager.cs`、`ShortCycleStateMachine.cs`；表现由`Runtime/ShortCyclePresentationDirector.cs`等现有组件处理，P2入口`Phase2/Phase2SceneEntry.cs`。详[当前主链](.ai-workspace/codebase/shortcycle.md)、[冰箱与滚动](.ai-workspace/codebase/scroll.md)。

`Core/ShortCycle.Core.asmdef`只引用BCL；纯合同与Phase2 Matcher/Carrier等在Core下。Generated.DataTables与ShortCycle单向引用Core，Editor.ShortCycle有必要直接引用。既有类型与GUID保持；本轮不重构或推进短循环。

现有短循环隐藏恢复边界：`Content/ShortCycleClueBoardBinder.cs`读取同对象`ShortCyclePresentationSet.IsContentRendererHidden`并重申隐藏，避免重绑后暴露旧Renderer；页面激活/退场与空间导航仍由表现层负责。2026-11-13用户明确批准旧静态检查按此现实现保持约束，同时更新8个已实现动作、非正库存过滤及显式锚点布局的等价写法检查；本批未因此修改短循环生产代码。

N012-P1仅修改原FSM的完整story开场/准备回报及相应单测，普通/无缝等路径保持。HeadlessChainV25按独立计划修订一次：prepare-camera移到6.250 ack-bars之后，camera完成6.750，其后统一+0.250；九CSV及配置/上下文/能力时长不变。新102行基准在执行前冻结SHA 9B88ED80776EEC3A661565D22DC8F9EA65BF68B69FCC1ACC550159047BAEBEDE，旧初稿/差异和新全文在AI546报告保留，不改原历史。无原生/引擎/新人工门，完整核验仍9/17。

N012-P1离线收口：唯一一次Narrative Final通过，FSM99检查、整链114检查/102行与执行前冻结新基准逐字节相同，十真实编译/运行阶段及其余原组全部通过。提前camera回报、串行请求、重复/迟到/外来消息和story无缝路径已覆盖；仅原并行断言和辅助回报顺序按新授权替代，未动调度/输入或其它实现。无基准执行后修订、无原生/新人工门，CONTROL已独审2合同/8事实通过；完整项目核验仍2026-09-17。

## 剧情底座（N0-01 / N0-02 / N0-03 / N0-04 / N0-05 / N0-06 / N0-07 / N0-08 / N0-09 / N0-10 / N0-11 / N0-12 / N0-13）

当前原生状态（AI547，2026-11-16）：底座程序集已进引擎，正式22表Catalog已保存，新增八表均无业务行；唯一演出配置资产在`Assets/Settings/Narrative/DialoguePresentationConfig.asset`（GUID a5c9f5249dccc32429695e62f7c34de0），十组/default可原生读回。下文N0各卡的“未创建/未导入”描述是其原离线交付边界，当前以本段为准。尚无真实表现消费者、场景绑定或NPC/气泡效果验收；参数入口见原[对话参数说明](docs/参数调整说明_对话.md)。

N0-13新增`DevTools/Narrative/NarrativeHeadlessChainTests.cs`和自属HeadlessChainV25九CSV/同一配置、合成文件与动作计划。原正式N009策略验证同CSV，再实际导入生成行/本地化聚合→原Adapter/Catalog/Directory→Resolver→FSM/Scheduler/Input；fake仅外部能力，旧源码/断言/夹具保持。仓库路径手写expected-trace在首次执行前冻结，102行初稿SHA AE4EE21FE0C5D5AB30610F17482C6C8D11DF0745707FB2EC52B5C64815BFA1B8，直接字节比较，actual另留输出。首次增量和唯一Final均114检查/零N009诊断通过，CONTROL已独审2合同/8事实；5条原静态宿主资源未绑定提示单列，非原生导入。详细动作时刻、完整基准及Final证据见该任务报告与原Narrative README，不代真实场景/整批或OPT视觉。

N0-09的`Assets/Editor/DataTables/NarrativeTableValidation.cs`在原`DataTableValidationContext`显式启用v2.5作者业务规则，复用N003签名/解析、N007覆盖与N006真实配置查找；原无业务上下文的通用Validate仍严格报错。正式ImportService.BuildValidationReport和Validate-DataTables.ps1默认启用，原BuildValidationContext只构造数据/枚举上下文，保既有直接通用调用。结果进入原Errors/Warnings及新增Infos，包含规则、表、CSV行/列。新表非本地化引用及限定数值/说话者条件交专项规则管理，类型/有限数、主键/必填/枚举/本地化错误继续阻断。Editor.DataTables仅追加Core依赖，PS/Mono编译清单同步，无Core/Unity源或旧定义改变。V13～24含14b/16b，文件只核N014目录精确文件名，不证明Sprite导入；缺演出配置/文件输入明确UNVERIFIED，无V25场景内容检查。CONTROL已独审3合同/9事实通过；当前231检查/56案例，原严格API/旧断言/正式数据保持，两次真实失败留史。新增合成ValidationV25夹具与原Narrative宿主接线见[离线说明](DevTools/Narrative/README.md)，用户填表级别说明见[八表说明](docs/剧情八表配表说明.md)。

`Assets/Scripts/Narrative/Core/Narrative.Core.asmdef` 为纯 BCL 程序集（noEngineReferences=true，项目与预编译插件引用均空）；`NarrativeCoreMarker.cs` 保留边界标记；N0-03新增纯命令合同。`Assets/Scripts/Narrative/Unity/Narrative.Unity.asmdef` 的直接项目引用为 Narrative.Core、Toolkit、Generated.DataTables，`NarrativeUnityMarker.cs` 仅标记边界。Core 不依赖 Unity、ShortCycle 或 LongLoop；Unity 不直接引用/调用短循环和 LongLoop，Generated.DataTables 既有的 ShortCycle.Core 传递依赖保持。

`DevTools/Verify-All.ps1 -Profile Narrative` → `DevTools/Narrative/Test-Narrative.ps1` → 同目录 Python 编译入口与 `NarrativeAssemblyTests.cs`。复用真实Bee/Mono编译输入和现有源证据工具，按Core→Toolkit/ShortCycle→Generated→Editor.DataTables/Unity的实际DAG编译三个依赖、两个Narrative DLL及Editor.DataTables，再保留原三条示例检查 Core 的实际引用，并运行 `NarrativeCommandTests.cs`。新测试读取独立永久夹具 `DevTools/Narrative/Fixtures/CommandSignatures.v2_5.tsv`，覆盖语法、位置诊断、目标及14行签名。编译和运行输出在本任务新目录，未调用 Unity，后续导入由获批引擎卡负责。既有业务回归仍由原入口承担，不随 Narrative profile 触发整仓回归。详 [剧情离线入口](DevTools/Narrative/README.md)。

本节为 2026-11-13 局部新增边界与离线入口登记，不提高完整核验日期。Schema v2.5 文档与现 T9 的单字符 key 正则差异保留，CONTROL 已裁决本骨架不消费该差异；N0-01阶段未改旧schema/表/校验器；N004后续新增八表接入见数据节，原14表定义/数据保持。

2026-11-14 N0-01离线收口：当前Narrative真实编译/三条示例和源码更新后的LongLoop默认99断言已重跑；ImportContracts/Schemas/ShortCycle按用户批准的限定修复、实际成功原流和受测源/数据一致性复用。Schemas当前生产静态检查与永久固定样例回归分开，真实warning/audit不删；这些离线结果不代Unity导入、场景或用户U5验收。ShortCycle默认成功后的PS仅多已批clueRepair身份/报告根接线，不把不同PS SHA伪报相同；旧失败及全部例外保留。

`Assets/Scripts/Narrative/Core/NarrativeCommandSignatures.cs` 集中定义14条命令的参数语义/枚举值、三个等级可用性、瞬时性、截断和on_end限制；`NarrativeCommandParser.cs` 提供 `NarrativeCommandParser.Parse` 与 `NarrativeCommandTargetParser.TryParse`，命名空间均为 `EatWhat.Narrative.Core`。参数保留原始文本，错误含原输入位置和原因。`actor` 参数使用裸角色ID（可带#实例），`target` 使用actor:/anchor:/player；秒数和镜头size只解析有限数值，set_flag.value保留原字面量。解析成功不代表上下文许可；后续校验/调度须读取同一签名的等级、on_end及版本可用性，actor_emote本版不可用。本轮不实现命令处理器、推进状态机或Unity接线。API与边界详见上述README；2026-11-14仅本模块局部核对。

`Assets/Scripts/Narrative/Core/NarrativeAdvanceInputModel.cs`（N0-11）包含不可变 `NarrativeAdvanceSettings`、调用方阶段上下文/返回意图及 `NarrativeAdvanceInputModel`。调用方提供单调秒数和当前阶段，模型只持有AUTO/快进开关、独立的手动/CG截止时间及本句AUTO等待信息；不持有段句/是否可结束状态，不读设备或推进真实视图。`TryClick` 映射原四行表，`NotifyCgChanged` 刷新CG冷却，`NotifyLineStarted/Completed` 通知等待生命周期，`EvaluateAuto` 在出完并等满后返回下一句且豁免两冷却；快进只暴露开关。原三示例和621命令检查保留，同一既有测试EXE调用 `DevTools/Narrative/NarrativeAdvanceInputTests.cs` 的Run组。参数名、秒单位、预设和调用顺序见Narrative README；2026-11-14仅本模块局部核对，后续N0-12消费意图。

N0-02的 `Core/NarrativeCurves.cs` 提供独立同序 `NarrativeCurvePreset`、`FromOrdinal(int)`、Value/Integral。四公式及Clamp按旧StreetFollowPipeline原样迁出；`Assets/Scripts/Tools/NarrativeCurveBridge.cs` 以int/double薄桥映射，Toolkit新增对现有Narrative.Core引用。StreetFollowPipeline仅两个原函数体委托薄桥，StreetCurvePreset/配置/其它调用方与LongLoop.asmdef不变；Core仍无项目/引擎引用，引用DAG无环。曲线可直接用于后续纯演出取值，不从Core反向引用Toolkit/LongLoop。现Narrative/LongLoop两Python编译入口纳入新源/新引用；LongLoop新增依赖编译另存narrative-dependencies.json，既有业务记录索引保持。曲线逐点与独立旧公式oracle按位对照，旧业务测试原断言保留；没有参数或Inspector变更。Narrative及16861曲线检查、LongLoop默认99的成功证据保持；Avatar旧编译清单失败由直属AI531修复并经CONTROL独审，真实生产编译和原24条headless断言通过。原AI529同根续行的其余13模式现均离线通过，配置组旧shim缺Tooltip属性的首次编译失败及限定runner修复/一次重试保留；旧C#断言、配置及生产源码未因此改动。完整离线回归已由CONTROL独审3合同/7事实通过，不代表Unity原生或视觉验收。2026-11-14仅本模块局部核对。

N0-10的 `Core/NarrativeCommandScheduler.cs` 集中处理器接口、批次/命令状态及纯 `NarrativeCommandScheduler`。StartLine在出字开始时提交并默认并行，上一句由TruncateCurrentLine或下一StartLine按签名类别收尾；顺序wait用调用方秒数计时。EndDialogue只解除当前句关联、保留后台批次和顺序尾链，StartOnEnd不截断它们；调用方须继续Tick。CancelCameraCommands仅取消签名SnapCamera类，TryTakeSceneSwitch输出已执行切场景信号，均不推进真实对话FSM。复用已接受解析/签名/诊断/目标类型，等级不符/预留命令跳过诊断；没有真实命令效果或引擎接入。现测试EXE增加NarrativeCommandSchedulerTests的假处理器/调用方时间Run组，原3/621/87/16861断言保持；N0-10本卡只运行Narrative；N0-02旧业务回归由原AI529续行完成，见上节，不计入N0-10的测试执行。API及生命周期约定见原README，2026-11-14仅本模块局部核对。


N0-06新增 `Core/DialoguePresentationData.cs` 与 `DialoguePresentationValidation.cs`：一个可序列化纯配置根、十组命名模板/default、显示锚点站位/纯位置与颜色、按名回落诊断及不改值校验，复用曲线/命令执行方式/出字点击三个既有枚举。`Unity/DialoguePresentationConfig.cs` 是唯一新ScriptableObject类型，直接持有同一纯数据，不重复默认表；Changed/OnEnable/OnValidate参考既有通知语义，JsonUtility真实编译引用单列UnityEngine.JSONSerializeModule。未创建.asset、未接表现/Resolver/override注册表或状态机，原生导入/Inspector/序列化往返未验证。原Narrative EXE追加Presentation配置Run组，独立D5/Schema夹具187行，新2641检查及原3/621/87/16861/111在唯一Final中通过，Core实际仍仅mscorlib。项目配置权威说明见[对话参数说明](docs/参数调整说明_对话.md)，当前类型与离线证据已由CONTROL独审3合同/7事实通过；2026-11-14仅本模块局部核对。


N0-07的 `Core/DialogueOverrides.cs` 提供字段单位声明、`DialogueParameterRegistry` 和 `DialogueOverrideParser.Parse`。原N006的69参数只追加单位属性，默认值/类型/查找/验证字节语义保持；注册项从十组实际字段及属性反射读取，采用group.field唯一key。七类值包含实际纯位置/RGBA/站位数组，CG横纵独立；共享N006校验并报告key/问题/位置，未知/类型不符/重复使结果失败且Values为空，不写回用户配置。不实现N008取值优先级/冻结快照或表现。原EXE追加Override Run组，旧187行夹具原文保持；新776检查（69键/7类/40错误案例）和旧全部组在唯一Narrative Final通过，Core仍仅mscorlib，CONTROL已独审3合同/7事实通过。覆盖语法/复合值与单位继续在原[对话参数说明](docs/参数调整说明_对话.md)维护；2026-11-14仅本模块局部核对。

N0-08新增`Core/DialoguePresentationSnapshot.cs`与`DialoguePresentationResolver.cs`：按N006实际default→具名整组→本段逐项override生成十组/69项深层只读快照，九名从N005 Profiles取，trigger名只由调用方/独立ResolveTrigger给出。点/色/站位转独立只读类型，配置和解析复合值后续修改不影响快照；ResolveCg在具体CG消费点按各nullable列独立回落到本段CG组，显式0/false保留。原DialogueOverrideParser新增ParseItems复用唯一原scanner/Assignment/ValueReader与注册表，旧Parse继续原子失败/Values空；新入口跳过错误项，首次出现保留key，后续重复诊断不覆盖，未闭合引号尾部不猜分隔。缺失/重复default或非法配置沿N006诊断失败，未知模板有warning并回default，不复制默认常量。新增435检查覆盖69默认/69覆盖与11词法边界，原全部组在一次Narrative Final通过、真实Core仍仅mscorlib。开发与使用见原Narrative README和[对话参数说明](docs/参数调整说明_对话.md)；已由CONTROL独审3合同/8事实通过，仅覆盖约定离线范围，无FSM/处理器/配置资产或表现接入，完整核验日期不提高。

N0-12新增`Core/NarrativeDialogueContracts.cs`与`NarrativeDialogueStateMachine.cs`：仅处理normal/story十步进度，ambient明确NotApplicable；能力经视图/锁/时钟/已聊记录/切场景完成小接口注入，不实现真实能力。按N005目录链/诊断和N008快照开始，story按N012-P1先等黑边完成才请求镜头、镜头完成才入场，提前镜头回报拒绝，入场可经N011跳过；同一caller时刻提交缓存命令、speaker/name key/反应和出字，视图报告完成，末句给CanEnd/story撤黑边。下一句经N010截断，结束仅解除前台关联、story取消camera类，on_end/记录一次/解锁/结束依序，后台仍外部Tick。完整续接重新取数据/快照；同级Seamless仅省前退场/后入场并更新名单，跨级/切场景强制完整。实际scene_switch信号及延后on_end发出前均不提前开下一段，外部按原invocation回报完成；失败切场景留诊断且不假完成。Scheduler仅加StartCachedLine/OnEnd桥接而不重解析；Input仅加ConfigureSettings，旧开关/冷却字段保原位和状态。新84假能力检查及原全部Narrative组在一次Final通过，真实Core仍仅mscorlib，非原生/视觉验收；API/具体边界见原Narrative README，CONTROL已独审3合同/8事实通过，仅约定离线逻辑，完整核验仍9/17。

## 数据、资源与视效

2026-11-15仅局部补核N0-05：`Core/NarrativeDataModels.cs`定义八类深层只读模型、出场槽位/实际名单、九模板原名和缓存命令列；角色配置空值与有效偏移0/缩放1分列，CG所有空参数（含横纵轴、false与空）留待N008。`Core/NarrativeDataDirectory.cs`提供`INarrativeDataDirectory`及冻结索引，八类查询沿bool/结果/诊断；状态与条目分别使用完整二/四元组键，缺失/重复不取首条。`TryGetLineChain`只沿first_line/next_line，标签冲突告警且仍取真实后继；缺失/重复/环路返回false和只读前缀/诊断，不保存推进状态。`Unity/NarrativeDataAdapter.cs`从实际生成Catalog/行取值快照，命令列各解析一次；整体失败时保原文/诊断且可执行列表为空，目录查询复用结果。Core不反向引用生成行或引擎，Unity原引用不变；无配置资产、FSM、真实命令处理器或Resolver。现入口追加181项纯目录检查及StaticCompile宿主1483项真实适配检查，20行296运行字段在源变更前后比较592次；原组保持。首轮新HashSet引入System.Core被原mscorlib-only断言拒绝，改新目录内部Dictionary后同key一次复验通过，失败留史。已由CONTROL独审3合同/8事实通过，仅覆盖约定离线范围，使用与边界见[原Narrative README](DevTools/Narrative/README.md)；不提高2026-09-17完整核验日期。

源表/CSV、生成物、导表/字体契约见[数据与本地化](.ai-workspace/codebase/data.md)，Resources字符串路径见[资源契约](.ai-workspace/codebase/resources.md)。正式工作簿/非空内容与用户配置不得因Git或清理改值；合法大源资产沿现LFS。

2026-11-15局部核对N0-04 CODE阶段：`DataTables/Schemas/Narrative`四表及`Schemas/Street`四表共105新列，现注册22表224列；原14schema/CSV、T14.preset_tools及程序集边界保持。8个`CK01*Data`行类型和`CK01OptionalValues.cs`沿CookingData既有asmref进入Generated.DataTables。ImportContracts/ValidationCore/ImportService复用原注册、转换、保存计划和查询目录；新sprite/prefab字段存原文件名，只作精确查找/缺失/重名诊断，不绑定资源。SceneStates与Placements用完整组合键；可空数值/布尔保留空与0/false；Catalog新增8类TryGet查询，沿bool/行/诊断合同，返回已有行对象而非冻结快照。条件speaker、场景内状态及ambient池引用由schema元数据进入共享校验器；依赖预摆实例的anchor_id条件留待场景上下文。

Authoring的New入口通过`-AddSchemaTables`在原xlsx追加指定缺失表头页，沿原备份/快照/事务，仅放行明确新页清单；Update-HeaderComments的`-InitializeSchemaTables`仅给指定且没有已有注释的地址初始化，默认人工/未知注释保护保持。DATA535已正式交付八份header-only CSV及原主表22页/224条说明，105新列无业务行；旧14页所有存储单元格值/类型/公式/结构与原119条备注独立复核保持，旧14CSV/schema SHA不变。Update-HeaderComments新增显式UpdateSchemaTables与初始化名单子集限制，默认归属/全局审计保持；Resolve-Part支持包根absolute及relative/点段/反斜杠且仍拒绝越根。Node导出仅在目标缺失且baseline无配置数据时物化合法表头，已有目标/非空baseline不清空。正式22表校验0error/44warning/1info、四旧完整入口各一次exit0已由CONTROL独审；保scene UNVERIFIED与Authoring旧报告字面14，不拿字面冒22。原断言按已批准16行数量例外，其余90静态Assert条件消息次序保持，旧回归只在TEMP准备26样式故障/固定无效G10及精确legacy注释。现有Narrative profile显式补9个Bee未列新源，真实编译Generated及Editor.DataTables；分开执行现有StaticCompile宿主中的1141实际校验/转换/查询检查和真实Authoring临时工作簿49检查。无原生AssetDatabase保存、Unity/Play或发布加密接入。配置见[八表说明](docs/剧情八表配表说明.md)，开发与操作见[DataTables](DevTools/DataTables/README.md)和[Authoring](DevTools/DataTables/Authoring/README.md)。N004原卡离线逻辑/数据范围已结合CODE534的3合同8事实和DATA535的3合同7事实独审通过；不代表整批或原生/Excel客户端显示签收，不提高完整核验日期。

描边、阴影、泡泡和统一VisualEffectsLab见[视效定位](.ai-workspace/codebase/visual-effects.md)及中文组件说明。角色描边后续业务仍暂停，AI464现有样例/未完人工事项材料不清理。旧CookingPrepare/Process边界见[旧链说明](.ai-workspace/codebase/legacy-build.md)，不因清理删除Spine Sample、长期探针或Tomato预研。

## 构建、验证与Git

构建复用`DevTools/Build-Demo.ps1`、`Assets/Editor/Build/CatalogBlobBuildProcessor.cs`的BuildPrepared和当前正式ENGINE_CLI受管流程；0.1.2只更改了获批的版本标记。输入认证/owned临时事务、当前用户成果保护保留。CLI与标准预检/项目grant边界见[WF](.ai-workspace/toolkit/WF.md)和[ENGINE_CLI](.ai-workspace/workers/ENGINE_CLI.md)。

镜头离线测试入口`DevTools/LongLoop/Test-LongLoopPlayer.py`；引擎验收固定三类和累计三轮，整体体验由用户决定。FOLLOW-R旧门、全量后台探针和定时回归保持暂停。Git日常只追踪开发源、正式配置和长期文档；release/交付包/机器运行与临时证据留本地。GitHooks保持Static字体及main/backup保护，新增源边界和普通blob体积检查；详GitHooks README。
