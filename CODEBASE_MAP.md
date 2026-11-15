# 《是啊，吃什么》代码结构与修改导航

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
