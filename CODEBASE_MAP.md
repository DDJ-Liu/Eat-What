# 《是啊，吃什么》代码结构与修改导航

最后完整核验：**2026-09-17**（不提高）。本次仅整理文档与核对源码归属；任务执行过程、当时的测试与许可记录移至历史，不能据历史状态启动业务。执行规则以 AGENTS.md、WORKFLOW.md 的现行优先条款为准。工具表保留原能力、使用限制与来源语义，旧候选／待验标签不代表本次重新验收。

## 现状

## 工程与场景

正式共享Build表保持五场景：Boot、ShortCycle_P0P1、CookingProcess、ShortCycle_P2、LongLoop_Street。0.1.2的Windows64 Mono非Development构建只在本次BuildPlayerOptions选Boot+Street，不改共享场景表。源码/资源定位细节见[工程入口](.ai-workspace/codebase/entry.md)。

| 对象 / 组件 | 当前保存引用或位置 |
| --- | --- |
| StreetDialogue_TUNE / NarrativeStreetReactionAdapter | Dialogue Service→同对象原Service；Target Registry→NarrativeTargetRegistry_TUNE；Street Camera→Cam_Street；Player Controller→PlayerRoot_TUNE的LongLoopPlayerController |
| NarrativeTargetRegistry_TUNE / Player | 保留PlayerRoot_TUNE/Foot_TUNE；Adapter沿父链最近控制器认定同一主角，位置仍取Foot |
| NarrativeSceneDebugPanel_TUNE / Portrait Stage | NarrativeDialogueUI/Portraits的继承Stage；原Applier/Registry/Service引用保持 |
| NarrativeDialogueUI实例 | Stage、停用无图模板和Composition引用继承原Prefab，两字段prefabOverride=false；不Apply回Prefab |
| test_street_05_TUNE / NarrativeAnchorMarker | 新根对象，anchorId=test_street_05，初值(0.4,-4.75,0)；靠近出生点且与四原锚点/两NPC错开，待用户挪动 |

## 长循环与镜头

`Assets/Scripts/LongLoop/StreetSceneEntry.cs`是街景入口；正式输入`Assets/Scripts/Tools/Input/LegacyInputProvider.cs`实现既有`IGameInput`，读取旧键鼠和现Unity Input System右摇杆。当前Provider与LongLoop玩家控制器同在PlayerRoot_TUNE，Driver经现公开父级路径取得输入。

镜头配置在`Assets/Settings/Street/CameraFollowConfig.asset`及同目录现有配置。`StreetFollowDriver`持有纯状态，`StreetDynamicZoom`推进本帧Size与Ratio，`StreetLookFloat`处理鼠标/直接右摇杆目标和世界距离/速度上限，`StreetVerticalOffset`处理纵向输入，`StreetFollowComposition`按本帧几何合成通道；原`StreetFollowPipeline`持有目标/前视/回中状态。浮动有效意图暂停Shrink/Recenter及水平前视退出，解除后从当前值重新等待各自完整Delay/曲线。FOCUS不随视野比例缩放。

Driver写独立目标节点及本帧Lens状态，Composer执行唯一位置阻尼，Confiner约束边界；不新增整机相机阻尼。N0-OPT在Driver调用原世界限幅器时仅将浮动径向上限乘同帧Ratio，速度原样，Ratio=1保持原输出；源码/离线通过，补丁视觉已于2026-11-15用户验收。`StreetParallaxLayer`在Cinemachine最终输出后读相机位置，只写所属层容器并在退出恢复，数学在`StreetParallax.cs`。街景已有六层接线及用户归层/原点配置。

正式使用/调参文档维护在[docs参数说明](docs/demo-0.1.2_参数调整说明.md)、[人工检查说明](docs/demo-0.1.2_人工检查说明.md)及[组件说明目录](组件说明文档/视觉特效/README.md)。这些是项目长期源文档，不以任务attempt中的摘录为配置权威。实际数值以用户保存资产/Inspector为准。

## 短循环

食谱与备菜的返回按钮、冰箱短点与拖放、双向拖动的托盘格预览、来源格命中，以及食谱线索板展示的当前入口，见[短循环模块](.ai-workspace/codebase/shortcycle.md)与[普通 Play 检查说明](docs/短循环前半段人工检查说明.md)。

Session/FSM入口为`Assets/Scripts/Cooking/ShortCycle/ShortCycleSessionManager.cs`、`ShortCycleStateMachine.cs`；表现由`Runtime/ShortCyclePresentationDirector.cs`等现有组件处理，P2入口`Phase2/Phase2SceneEntry.cs`。详[当前主链](.ai-workspace/codebase/shortcycle.md)、[冰箱与滚动](.ai-workspace/codebase/scroll.md)。

`Core/ShortCycle.Core.asmdef`只引用BCL；纯合同与Phase2 Matcher/Carrier等在Core下。Generated.DataTables与ShortCycle单向引用Core，Editor.ShortCycle有必要直接引用。既有类型与GUID保持；本轮不重构或推进短循环。

现有短循环隐藏恢复边界：`Content/ShortCycleClueBoardBinder.cs`读取同对象`ShortCyclePresentationSet.IsContentRendererHidden`并重申隐藏，避免重绑后暴露旧Renderer；页面激活/退场与空间导航仍由表现层负责。2026-11-13用户明确批准旧静态检查按此现实现保持约束，同时更新8个已实现动作、非正库存过滤及显式锚点布局的等价写法检查；本批未因此修改短循环生产代码。

## 剧情底座（N0-01 / N0-02 / N0-03 / N0-04 / N0-05 / N0-06 / N0-07 / N0-08 / N0-09 / N0-10 / N0-11 / N0-12 / N0-13）

当前原生状态（AI547，2026-11-16）：底座程序集已进引擎，正式22表Catalog已保存，新增八表均无业务行；唯一演出配置资产在`Assets/Settings/Narrative/DialoguePresentationConfig.asset`（GUID a5c9f5249dccc32429695e62f7c34de0），十组/default可原生读回。下文N0各卡的“未创建/未导入”描述是其原离线交付边界，当前以本段为准。尚无真实表现消费者、场景绑定或NPC/气泡效果验收；参数入口见原[对话参数说明](docs/参数调整说明_对话.md)。

`Assets/Scripts/Narrative/Core/Narrative.Core.asmdef` 为纯 BCL 程序集（noEngineReferences=true，项目与预编译插件引用均空）；`NarrativeCoreMarker.cs` 保留边界标记；N0-03新增纯命令合同。`Assets/Scripts/Narrative/Unity/Narrative.Unity.asmdef` 的直接项目引用为 Narrative.Core、Toolkit、Generated.DataTables，`NarrativeUnityMarker.cs` 仅标记边界。Core 不依赖 Unity、ShortCycle 或 LongLoop；Unity 不直接引用/调用短循环和 LongLoop，Generated.DataTables 既有的 ShortCycle.Core 传递依赖保持。

`DevTools/Verify-All.ps1 -Profile Narrative` → `DevTools/Narrative/Test-Narrative.ps1` → 同目录 Python 编译入口与 `NarrativeAssemblyTests.cs`。复用真实Bee/Mono编译输入和现有源证据工具，按Core→Toolkit/ShortCycle→Generated→Editor.DataTables/Unity的实际DAG编译三个依赖、两个Narrative DLL及Editor.DataTables，再保留原三条示例检查 Core 的实际引用，并运行 `NarrativeCommandTests.cs`。新测试读取独立永久夹具 `DevTools/Narrative/Fixtures/CommandSignatures.v2_5.tsv`，覆盖语法、位置诊断、目标及14行签名。编译和运行输出在本任务新目录，未调用 Unity，后续导入由获批引擎卡负责。既有业务回归仍由原入口承担，不随 Narrative profile 触发整仓回归。详 [剧情离线入口](DevTools/Narrative/README.md)。

本节为 2026-11-13 局部新增边界与离线入口登记，不提高完整核验日期。Schema v2.5 文档与现 T9 的单字符 key 正则差异保留，CONTROL 已裁决本骨架不消费该差异；N0-01阶段未改旧schema/表/校验器；N004后续新增八表接入见数据节，原14表定义/数据保持。

`Assets/Scripts/Narrative/Core/NarrativeCommandSignatures.cs` 集中定义14条命令的参数语义/枚举值、三个等级可用性、瞬时性、截断和on_end限制；`NarrativeCommandParser.cs` 提供 `NarrativeCommandParser.Parse` 与 `NarrativeCommandTargetParser.TryParse`，命名空间均为 `EatWhat.Narrative.Core`。参数保留原始文本，错误含原输入位置和原因。`actor` 参数使用裸角色ID（可带#实例），`target` 使用actor:/anchor:/player；秒数和镜头size只解析有限数值，set_flag.value保留原字面量。解析成功不代表上下文许可；后续校验/调度须读取同一签名的等级、on_end及版本可用性，actor_emote本版不可用。本轮不实现命令处理器、推进状态机或Unity接线。API与边界详见上述README；2026-11-14仅本模块局部核对。

`Assets/Scripts/Narrative/Core/NarrativeAdvanceInputModel.cs`（N0-11）包含不可变 `NarrativeAdvanceSettings`、调用方阶段上下文/返回意图及 `NarrativeAdvanceInputModel`。调用方提供单调秒数和当前阶段，模型只持有AUTO/快进开关、独立的手动/CG截止时间及本句AUTO等待信息；不持有段句/是否可结束状态，不读设备或推进真实视图。`TryClick` 映射原四行表，`NotifyCgChanged` 刷新CG冷却，`NotifyLineStarted/Completed` 通知等待生命周期，`EvaluateAuto` 在出完并等满后返回下一句且豁免两冷却；快进只暴露开关。原三示例和621命令检查保留，同一既有测试EXE调用 `DevTools/Narrative/NarrativeAdvanceInputTests.cs` 的Run组。参数名、秒单位、预设和调用顺序见Narrative README；2026-11-14仅本模块局部核对，后续N0-12消费意图。

## 数据、资源与视效

源表/CSV、生成物、导表/字体契约见[数据与本地化](.ai-workspace/codebase/data.md)，Resources字符串路径见[资源契约](.ai-workspace/codebase/resources.md)。正式工作簿/非空内容与用户配置不得因Git或清理改值；合法大源资产沿现LFS。

描边、阴影、泡泡和统一VisualEffectsLab见[视效定位](.ai-workspace/codebase/visual-effects.md)及中文组件说明。角色描边后续业务仍暂停，AI464现有样例/未完人工事项材料不清理。旧CookingPrepare/Process边界见[旧链说明](.ai-workspace/codebase/legacy-build.md)，不因清理删除Spine Sample、长期探针或Tomato预研。

## 构建、验证与Git

发布有两个外层入口：`DevTools/Build-Demo.ps1` 接收显式 `-Scenes`（默认 Boot＋Street）；`DevTools/Build-Release.ps1` 保留固定五场景流程。两者共用 `CatalogBlobBuildProcessor.PrepareSnapshot` 与 `BuildPrepared(BuildPlayerOptions,string)`。正式 ENGINE_CLI 可沿已审载体把本轮显式场景数组直接交给同一 `BuildPrepared`，并消费当前身份、标准预检、项目 grant 与实际 intent；外层脚本不转交可选 grant/intent 时不省略这些权限。共享 EditorBuildSettings 保持原保存列表，构建清单由本次 `BuildPlayerOptions.scenes` 提供。输入认证、自有临时输入与 journal 清理、作者场景恢复保持；CLI/权限边界见[WF](.ai-workspace/toolkit/WF.md)和[ENGINE_CLI](.ai-workspace/workers/ENGINE_CLI.md)。

镜头离线测试入口`DevTools/LongLoop/Test-LongLoopPlayer.py`；引擎验收固定三类和累计三轮，整体体验由用户决定。FOLLOW-R旧门、全量后台探针和定时回归保持暂停。Git日常只追踪开发源、正式配置和长期文档；release/交付包/机器运行与临时证据留本地。GitHooks保持Static字体及main/backup保护，新增源边界和普通blob体积检查；详GitHooks README。

### 本地四场景试构建入口

已签收的本地试构建显式清单为 `Assets/Scenes/LongLoop/Boot.unity`、`Assets/Scenes/LongLoop/LongLoop_Street.unity`、`Assets/Scenes/Cooking/ShortCycle_P0P1.unity`、`Assets/Scenes/ShortCycle_P2.unity`，按此顺序。成品位于 `交付文件/短循环四场景试构建_20261124/`，仅本地保留；成品目前没有进入短循环的菜单。构建／启动／数据读回不代签成品内短循环游玩与视觉。

`DevTools/Build-Demo.ps1` 接收显式 `-Scenes`；`DevTools/Build-Release.ps1` 保留固定五场景。共享链仍通过 `CatalogBlobBuildProcessor.PrepareSnapshot`／`BuildPrepared` 消费本次 `BuildPlayerOptions.scenes`，不因此改共享 EditorBuildSettings。详 [Build-Demo](<DevTools/Build-Demo.ps1>) 与 [Build-Release](<DevTools/Build-Release.ps1>)。

## 已核程序集与文件定位

按当前 `.asmdef`／最近 `.asmref` 解析；下列只是源码归属，不代表重新编译、运行或验收。

| 程序集 | 定义／归属入口 | 本页相关源码例 |
| --- | --- | --- |
| `Editor.DataTables` | [Assets/Editor/DataTables/Editor.DataTables.asmdef](<Assets/Editor/DataTables/Editor.DataTables.asmdef>) | [DataTableImportService.cs](<Assets/Editor/DataTables/DataTableImportService.cs>)；[NarrativeTableValidation.cs](<Assets/Editor/DataTables/NarrativeTableValidation.cs>) |
| `Editor.Project` | [Assets/Editor/Editor.Project.asmdef](<Assets/Editor/Editor.Project.asmdef>) | [CatalogBlobBuildProcessor.cs](<Assets/Editor/Build/CatalogBlobBuildProcessor.cs>) |
| `Generated.DataTables` | [Assets/Scripts/DataBase/CookingData/Generated.DataTables.asmref](<Assets/Scripts/DataBase/CookingData/Generated.DataTables.asmref>) | [CK01CharacterData.cs](<Assets/Scripts/DataBase/CookingData/CK01CharacterData.cs>)；[CK01OptionalValues.cs](<Assets/Scripts/DataBase/CookingData/CK01OptionalValues.cs>)；[CatalogBlobSerializer.cs](<Assets/Scripts/DataBase/CookingData/CatalogBlobSerializer.cs>) |
| `LongLoop` | [Assets/Scripts/LongLoop/LongLoop.asmdef](<Assets/Scripts/LongLoop/LongLoop.asmdef>) | [CameraFollowConfig.cs](<Assets/Scripts/LongLoop/Configuration/CameraFollowConfig.cs>)；[DepthScale.cs](<Assets/Scripts/LongLoop/DepthScale.cs>)；[LongLoopPlayerController.cs](<Assets/Scripts/LongLoop/LongLoopPlayerController.cs>) |
| `Narrative.Core` | [Assets/Scripts/Narrative/Core/Narrative.Core.asmdef](<Assets/Scripts/Narrative/Core/Narrative.Core.asmdef>) | [DialogueOverrides.cs](<Assets/Scripts/Narrative/Core/DialogueOverrides.cs>)；[DialoguePresentationData.cs](<Assets/Scripts/Narrative/Core/DialoguePresentationData.cs>)；[DialoguePresentationResolver.cs](<Assets/Scripts/Narrative/Core/DialoguePresentationResolver.cs>) |
| `Narrative.Unity` | [Assets/Scripts/Narrative/Unity/Narrative.Unity.asmdef](<Assets/Scripts/Narrative/Unity/Narrative.Unity.asmdef>) | [DialoguePresentationConfig.cs](<Assets/Scripts/Narrative/Unity/DialoguePresentationConfig.cs>)；[NarrativeBubbleCanvasRenderer.cs](<Assets/Scripts/Narrative/Unity/NarrativeBubbleCanvasRenderer.cs>)；[NarrativeDataAdapter.cs](<Assets/Scripts/Narrative/Unity/NarrativeDataAdapter.cs>) |
| `ShortCycle` | [Assets/Scripts/Cooking/ShortCycle/ShortCycle.asmdef](<Assets/Scripts/Cooking/ShortCycle/ShortCycle.asmdef>) | [ShortCycleClueBoardBinder.cs](<Assets/Scripts/Cooking/ShortCycle/Content/ShortCycleClueBoardBinder.cs>)；[Phase2SceneEntry.cs](<Assets/Scripts/Cooking/ShortCycle/Phase2/Phase2SceneEntry.cs>)；[ShortCyclePresentationSet.cs](<Assets/Scripts/Cooking/ShortCycle/Presentation/ShortCyclePresentationSet.cs>) |
| `Toolkit` | [Assets/Scripts/DataBase/Toolkit.asmref](<Assets/Scripts/DataBase/Toolkit.asmref>) | [Data.cs](<Assets/Scripts/DataBase/Data.cs>) |
| `Toolkit` | [Assets/Scripts/Tools/Toolkit.asmdef](<Assets/Scripts/Tools/Toolkit.asmdef>) | [IGameInput.cs](<Assets/Scripts/Tools/Input/IGameInput.cs>)；[LegacyInputProvider.cs](<Assets/Scripts/Tools/Input/LegacyInputProvider.cs>)；[InteractionPrompt.cs](<Assets/Scripts/Tools/InteractionPrompt.cs>) |

## 历史

[本页整理前全文与历史记录](<docs/archive/工程事实_20261124/history/CODEBASE_MAP.md>)；[归档与原字节恢复索引](<docs/archive/工程事实_20261124/README.md>)；[已知不符与保留边界](<docs/archive/工程事实_20261124/差异与保留边界.md>)。
