# 剧情离线验证入口

## V013-10 必要生产编译与引用查询（2026-11-16）

原Python入口增加可选--compile-only：按原真实闭包构建Core/Toolkit/ShortCycle.Core/Generated/Editor.DataTables/Narrative.Unity，保存原源/引用/RSP记录后返回，不编译/运行后续Narrative用例；默认行为与所有旧断言/夹具不改。用于本工具卡必要真实DLL编译，已有UI/TMP引用保持。本卡原ImportContracts宿主最小加入现Adapter/RuntimeEntry/配置、原RuntimeUnityBoundary及新绑定用例，CompileOnly仍在旧位置返回，不把本卡扩大为整Narrative回归。

实际Unity新增按基础角色id的TryGetCharacterVisualPrefab查询，Core只保原文件名；空列、缺引用、条目/源不可用的区别及05/11真实承接见原八表/Runtime说明。没有新框架、比较器或验收门；共享stub默认行为保留，仅新用例显式配置外部资产搜索。

## V013-07 路人气泡显示/触发（2026-11-16）

原入口末尾追加NarrativeBubbleViewTests，直接执行实际RuntimeEntry/Adapter/Resolver/Core模型、LongLoop的实际Driver及Unity View生命周期；新增NarrativeBubbleUnityBoundary仅补外部Head句柄与游戏时钟，UI表面/地面距离为外部fake。原共享边界、所有旧测试/冻结夹具和原调用次序不改，551/552新39/82保持。CanvasRenderer和实际NPC组件用当前真实DLL编译，不用UI fake冒充投屏/字体效果。Unity层显式单向增加UnityEngine.UI/Unity.TextMeshPro引用，两个现runner补当前UI DLL/UIModule；没有新框架或引擎门。

配置说明固定在组件说明文档/视觉特效/剧情气泡配置与使用说明.md，13/14负责真实接线和人工路径；本卡无Unity/Play/Build。

## V013-06 气泡取句/冷却（2026-11-16）

原完整入口在保留551全部原组（含39接入检查）后追加 `NarrativeAmbientBubbleTests`，直接执行实际Core模型、目录/链与原ResolveTrigger；仅固定外部时间/地面距离/随机输入，不新增替代逻辑/宿主或修改冻结夹具。Core新源由原原生引用编译自动收录，仍以原程序集样例核无Unity和仅mscorlib；不跑其它LongLoop或全量门。原参数说明 `docs/参数调整说明_对话.md` 补同一实际消费与无表现边界。

## V013-04 运行时配置/数据接入（2026-11-16）

原入口保留全部Core、表/Adapter、作者工具与整链断言/夹具，在同一StaticCompile宿主追加 `NarrativeRuntimeEntryTests`，直接执行实际接入组件、Catalog、Adapter和Resolver。只在 `NarrativeRuntimeUnityBoundary.cs` 补原宿主缺少的MonoBehaviour/Inspector元数据，以及本测试Unity层的日志context重载，不改变UnityStubs；异常源用既有ICatalogSource验证入口投递外部读失败，保持生产源策略。配置Changed事件用例明确投递现有委托，因为原JsonUtility stub输出固定{}，不声称原生NotifyChanged检测已验。真实Unity层源码仍由原Python入口全量编译，未动该入口或原断言。

配置说明在 `docs/剧情运行时接入说明.md`。LongLoop仅增加单向Narrative.Unity引用；其原默认入口编译当前Core→Toolkit/ShortCycle.Core→Generated→Narrative.Unity真实依赖，未以旧Bee的空标记替代。此卡不改数据源、加密包、业务状态或实际场景接线。

## N0-13 无界面整链（2026-11-15局部核对）

N012-P1离线收口：唯一一次Narrative Final通过，FSM99检查、整链114检查/102行与执行前冻结新基准逐字节相同，十真实编译/运行阶段及其余原组全部通过。提前camera回报、串行请求、重复/迟到/外来消息和story无缝路径已覆盖；仅原并行断言和辅助回报顺序按新授权替代，未动调度/输入或其它实现。无基准执行后修订、无原生/新人工门，待CONTROL独审；完整项目核验仍2026-09-17。

N0-12-P1当前补正：story完整开场先请求黑边，收到有效黑边完成后才请求镜头，镜头完成才入场；镜头尚未请求时的提前完成回报拒绝且不消费。普通及同级Seamless、撤黑边/退场/命令/输入/锁/记录保持。原N013的102行基准作为初版历史保留；本补丁按独立计划修订一次并在执行前冻结新SHA 9B88ED80776EEC3A661565D22DC8F9EA65BF68B69FCC1ACC550159047BAEBEDE。6.000上黑边，6.250黑边完成后立即准备镜头，保持0.500镜头时长到6.750完成，后续时刻统一+0.250；九CSV/config/context、动作值顺序及handler/instant持续时间不变，普通段不变。无新增参数、默认或Inspector入口；原通过/失败史保持。

本卡唯一一次Narrative Final通过，真实十阶段及全部原组保持；整链114检查、102行expected/actual SHA与首次冻结值一致，基准未修订。仅自属执行包装器在正式Final调用前有一次括号语法失败，留原流并修包装后首次Final成功，整链本身无失败/修订。结论为离线使用方通过，CONTROL独审与整批签收另记。

`NarrativeHeadlessChainTests.cs`沿本页原StaticCompile宿主，读取自属`Fixtures/HeadlessChainV25`的九CSV（八新表＋T9）、config/context/actions。相同CSV原字节经原BuildValidationContext＋N009 Enable业务规则，使用同一真实N006配置对象及明确合成文件名清单，再由实际TryApplyRow/TryApplyAggregateAsset生成22行和本地化聚合，经实际Catalog/NarrativeDataAdapter/Directory→Resolver→FSM/Scheduler/Input。只把视图、命令处理器、锁、时钟和已聊记录做fake，无手造Core模型、替代parser/取值器或producer修复。

独立动作计划使用InvariantCulture、确定时刻；actor_move/shake/camera_pan/cg_show/cg_hide的fake能力耗时0.5秒，set_flag/set_scene_state即时完成。普通段三句包含无名无立绘的旁白；story按P1先等黑边完成再请求镜头，镜头完成后才入场，命令顺序执行，文字不等命令；on_end按完整scene/state键改变fake状态后自动完整续接normal后段。view请求返回后再用原消息对象报告完成，后台始终由外部scheduler.Tick驱动。ambient段/池仅实际导入和查询，不启动FSM；CG实际查询和ResolveCg也进入文字。

`expected-trace.txt`手写于首次执行前，UTF-8无BOM、LF且保末尾换行；首次冻结SHA为AE4EE21FE0C5D5AB30610F17482C6C8D11DF0745707FB2EC52B5C64815BFA1B8，共102行。测试用直接BCL字节SequenceEqual，不排序/归一化/回填基准，actual另写任务输出。首次增量114检查通过并与初稿完全相同；最终Final与原全部组一起执行，报告另附基准全文。`.gitattributes`仅追加该确切文件的LF行，未获Git提交授权，不暂存/提交。

此夹具N009通用data error、warning及info均为0。原宿主的AssetDatabase查询保留stub，因此真实TryApplyRow额外返回5条未绑定文件名提示（角色4、CG1），单独报告，不吞掉或伪报整个导入零warning；它们不替代N009提供的合成文件契约。没有执行旧14表场景依赖管线，既有工程scene UNVERIFIED不因此改变；当前测试不伪造旧管线info或场景签收。原生DLL编译与使用UnityStubs的整链执行分报，无Unity导入/Play/Build、实际场景/美术或配置资产。仅收口逻辑使用方，不代整15卡及OPT视觉签收。

## N0-09 作者静态规则（2026-11-15局部核对）

本卡最终离线记录：两次Final为失败→成功，第二次Custom Final合并Narrative、受影响的原Schemas/ImportContracts三个入口并全通过；旧1141/1483和全部Core组原断言保持。两次失败均为新增夹具期望错误（wait负值、完整状态所在行），原流与同key历史保留。之后收尾只改两处通用引用诊断ID并新增覆盖，未重复成功Final；当前Editor DLL单独真实编译和当前专项231检查/56案例均通过，当前正式CLI为0error/44warning/1info，旧七份审计保持。原有三条CS8032分析器兼容警告仍留原流；这些是离线编译，不代原生导入。最终报告与源码身份见AI544任务产物，CONTROL独审另记。

`Assets/Editor/DataTables/NarrativeTableValidation.cs`实现V13～24及14b/16b；无V25、场景加载或命令执行。`Enable(context, realPresentationData, fileLookup)`使用实际N006的Find、N007 ParseItems和N003解析/签名；`EnableProject(context, projectRoot)`供现正式导入/PS默认启用，仅提供约定目录的文件存在性。配置缺失逐行报`NARRATIVE_UNVERIFIED`，不冒称模板不存在或本地化/引擎已验。真实配置可由调用方通过Enable注入，不自动建资产或扫描猜选配置。

原DataTableValidationContext只增加具名业务策略和专项委托；未配置的generic Validate保原严格行为，旧测试通过原BuildValidationContext取得通用上下文。正式ImportService.BuildValidationReport显式启用，PS同样启用。只有八个新表的非loc引用、Schema指定的角色偏移/缩放及CG时长、说话者条件由专项接管；主键、必填、枚举、本地化、解析/type/有限数等原error保持。旧表不进入专项分支。结果仍是原报告Errors/Warnings，新增Infos；每条带表、原CSV行、列与规则，V表未单列的next_dialogue/SceneStates.scene_id引用用NARRATIVE_REFERENCE警告，缺输入的信息与规则结果分列。

V13按实际first/next链遍历，核环、失链、孤行及所有链上的归属标签；V14按schema实际cast编号扩展，不设新四人硬限，坐标为0也算已填；V15区分无立绘旁白/出场角色和ambient。V16复用原解析器，错误批不作后续命令语义判断；actor_emote、并行wait为info，wait方式读取实际tier配置与合法覆盖。V17使用九个真实Find方法和共享覆盖诊断。V18/19/20只核角色/CG/场景文件，文件存在不代导入类型；精确文件名/多匹配沿原FilenameLookupPolicy，不取第一项。V21按scene+state核引用和每场景各状态完整条目集合，anchor/facing/dialogue/ambient/marker仅actor使用；实例身份复用N003后缀语法，预摆实例与anchor必填归V25。V22核ambient池，V23按签名参数Kind核完整scene+state/对话/CG，V24核ambient续段和出场字段。

`Fixtures/ValidationV25/clean.json`是明确标记的合成八表输入，不改冻结TablesV25或正式CSV；实际CSV解析/原schema验证由新增`NarrativeValidationTests.cs`执行，沿原StaticCompile宿主接入。完整合成夹具通用error与全部V13～24诊断均应为0，各负例核独立规则、行列和warning/info。当前工程旧场景UNVERIFIED仍单列，合成输入不替代真实场景清单。Editor.DataTables追加Narrative.Core直接引用；真实DLL编译与使用UnityStubs的测试宿主分别报告。原Core/Unity源、105字段、旧断言/夹具和各默认值保持。无新增长期TK、比较框架、原生或视觉门。

2026-11-15补核N008取值/不可变快照与N012纯状态机；N004/N005/N008已接受结论、全部原组和失败史保持。

2026-11-15新增N0-05只读数据入口，当前Core新增两源、Unity新增一个适配源及各自meta；下文既有N004/其它组的来源、默认和失败记录保持。此更新仅局部源码/离线核对，不代表原生导入或整批签收。

使用 PowerShell 7：`DevTools/Verify-All.ps1 -TaskId <任务号> -Profile Narrative -Mode Final`。也可直接运行 `DevTools/Narrative/Test-Narrative.ps1 -OutputRoot <本任务新输出目录>`。聚合入口可用环境变量 `EATWHAT_NARRATIVE_OUTPUT_ROOT` 指定本次新目录；目录须位于 `.ai-workspace/outputs`，已有目录不覆盖。

复用现有LongLoop源证据工具和Bee响应文件，使用Unity6000.3.24f1安装中的Mono/csc，按DAG先编译纯Narrative.Core，再编译Toolkit、ShortCycle.Core、Generated.DataTables、Editor.DataTables及Narrative.Unity。Generated显式补入9个尚未由Editor刷新进Bee的新行/空值源；Generated和Editor的项目引用换成本轮新输出，Editor复用实际E.dag响应文件。保留输入快照/哈希、原始和派生RSP、命令、stdout/stderr、exit。未启动Unity，不等于已原生导入。

Core asmdef无项目引用、关闭自动插件引用并禁止引擎引用；Core编译只提供BCL。Unity直接项目引用仅Core、Toolkit、Generated.DataTables；Generated既有ShortCycle.Core传递关系保持。示例检查两个实际新DLL及Core只引用BCL；这些真实程序集编译不使用替身。N004另行复用现有StaticCompile主机执行实际表转换/查询，明确不覆盖原生AssetDatabase保存，见下节。

本入口只包含剧情测试。N0-01 的既有业务回归仍使用原 ShortCycle 默认、DataTables ImportContracts/Schemas、LongLoop 默认入口；过期文本断言、固定样例输入及合法模式接线只按逐项用户批准的限定例外修复，原失败与批准差异保留，不声称原失败轮通过；不加入每次剧情验证的默认集合，不运行暂停的全量探针、原生 CI 或 Build。后续实际剧情测试在此目录扩展，继续使用已有编译/测试入口。

2026-11-14 收口核对：当前剧情编译/三条示例与源码漂移后的LongLoop默认99断言重新执行；ImportContracts、Schemas及ShortCycle默认/37+20行为结果按实际成功原流、独审及源/数据一致性复用。ShortCycle默认成功后的PS存在已批clueRepair身份/报告根接线差异，不能声称整份PS哈希未变。Schemas先独立完整校验当前正式数据，再以当前schema验证永久固定样例；真实warning/audit分别保存，场景UNVERIFIED不代用户U5或产品验收。原T9正则差异未被此骨架消费，本轮不修改schema或用户配表。


## N0-03 命令合同与调用

2026-11-14局部核对。公开入口位于 `Assets/Scripts/Narrative/Core/NarrativeCommandSignatures.cs` 和 `NarrativeCommandParser.cs`，命名空间 `EatWhat.Narrative.Core`。Core不访问文件、场景或游戏状态；本卡没有命令处理器、调度器、推进状态机、演出或存档效果。

- `NarrativeCommandParser.Parse(text)` 返回命令列表和诊断。语法为 `cmd(arg,arg);cmd(arg)`，容忍标点周围空白；null、空串和纯空白返回空列表。命令名区分大小写。必需参数不能空，零参数命令写 `camera_return()` / `cg_hide()`。不支持嵌套、空命令或末尾分号。
- `SourceText` 保存完整输入（null归一为空串）；命令/参数的 `RawText` 保留原文，参数 `Value` 为去两侧空白的值。位置是原输入中的零基UTF-16偏移；诊断含Kind、Position、Length、Reason，缺失字符可指向EOF且Length为0。语法错误停止继续解析，已经完整解析的前缀仍保留；未知命令及参数错误也保留完整调用供诊断。消费者必须先判断 `Success`，不能把错误结果中的命令当执行请求。
- Schema没有定义引号或转义协议，本层不扩展它：引号作为普通字面字符保留，逗号/分号仍是分隔符，不能借引号包住它们。set_flag.value无指定标量类型，`true`、`42`等仍保留string及ScalarLiteral语义，后续业务不得据此假定布尔专用或存档行为。
- `NarrativeCommandSignatures.All` / `TryGet(name,out signature)` 是唯一生产签名表。参数含名称、语义Kind和有限选项；签名含ArgumentCount、AllowedLevels/Allows、IsInstantaneous、Truncation、OnlyOnEnd、IsAvailableInVersion。集合对外只读。解析成功仅表示语法和参数语义通过；等级、on_end位置、命令是否本版可用由后续校验/调度读取同一表处理。IsAvailableInVersion是Schema版本许可，不表示本程序集实现了处理器。
- `actor_emote(actor,icon)` 保留已知签名，本版三个等级均不可用，IsAvailableInVersion=false、IsInstantaneous=null、Truncation=Unavailable。合法文本可被解析用于后续明确诊断，不产生表情效果。

例：`NarrativeCommandParser.Parse("wait(0.5);actor_move(chr_cook#2,anchor:door);set_flag(chapter.ready,42)")`。先检查Success，再读取Commands及其Signature/Arguments；位置诊断始终对应原始字符串。

### 参数与目标语义

| Kind / 参数 | 本层结果与约束 |
|---|---|
| Seconds / CameraSize | 使用InvariantCulture解析有限double；拒绝NaN、Infinity与非数字。表未定义正数/范围，本层不新增阈值 |
| FlagKey / ScalarLiteral / IconToken | 非空去两侧空白string，RawText保留原文，不做数值/布尔转换或资源查找 |
| ActorId（actor参数） | 裸角色场景ID，如chr_cook或chr_cook#2；TypedValue为角色身份数据 |
| Target | 调用下述目标解析，TypedValue为NarrativeCommandTarget |
| SceneObjectId（object/entry）/ SceneStateId | 小写ASCII字母开头，仅小写字母、数字、下划线，长度不超过40；不额外要求前缀 |
| DialogueId / SceneId / CgId | 同上ID规则，分别要求dlg_ / scn_ / cg_前缀 |
| Boolean / ShakeLevel | bool严格为小写true/false，TypedValue为bool；震动档位light/medium/heavy保留string，选项来自签名表 |

`NarrativeCommandTargetParser.TryParse(text,out target,out diagnostic)` 接受 `actor:chr_cook`（可带#2等实例）、`anchor:door`、`player`，容忍整个目标两侧空白，失败返回位置/原因而不抛业务异常。Target含Kind、RawText、Identifier、BaseId、InstanceSuffix；实例后缀是去掉#的十进制文本，避免额外引入Int32上限。角色基础ID使用chr_前缀及上述ID规则，#后的显式序号至少2；保留#02等原写法，不猜场景里是否存在该实例。锚点ID不加业务前缀，不允许#实例；player为精确小写词。这里只检查文本语义，不做场景引用校验。

### Schema来源与离线回归

独立夹具 `Fixtures/CommandSignatures.v2_5.tsv` 手工转写自 `.ai-workspace/inputs/CK01-B_配表Schema_v2_5.md` 的对话命令表（419—438行，14数据行423—436）；源SHA256为 `61CB6622AE5AE8AEC19C2A8B9B4ABAD5B72C5CBD593851932D5AF0A40F7E11C9`。ID/布尔规则对应全局条目1/6/11/12及N0-03实例要求。Kind名称是本层对表中参数语义的分类；截断枚举逐项对应表内中文行为。夹具禁止从生产表自动生成，其options以分号分参数、竖线分选项，reserved表示本版未规定瞬时性。

原 `NarrativeAssemblyTests.cs` 三条断言保持原文；同一既有Python入口另编译/运行 `NarrativeCommandTests.cs`，逐格对比14条签名、合法/非法语法、位置、三种目标、实例与类型边界。原始编译/运行流和实际exit保留，报告新增commandTestText与独立夹具路径/SHA；Core实际引用仍须只有mscorlib。运行Profile Narrative即覆盖这些内容，无需新建测试框架或调用其它业务回归。离线通过不代Unity导入/场景或功能人工验收；本卡无画面变化，五列视觉检查不适用。


## N0-11 推进输入模型

2026-11-14局部核对。`Assets/Scripts/Narrative/Core/NarrativeAdvanceInputModel.cs` 内的 `NarrativeAdvanceInputModel` 返回输入意图，调用方负责实际视图与段句进度。`NarrativeAdvancePhase` 是每次调用传入的阶段上下文，模型不保存阶段或是否可结束状态；`NarrativeAdvanceIntent` 为None、CompleteLine、SkipToNext、SkipEntrance。SkipEntrance表示跳过入场并直接开始出字，由后续状态机执行。本卡不创建资产或正式Inspector配置入口。

`NarrativeAdvanceSettings` 是构造时传入的不可变纯C#值集合（构造参数camelCase，对应属性PascalCase）：

| 参数 / 属性 | 预设 | 来源与作用 |
|---|---|---|
| clickWhileTyping / ClickWhileTyping | CompleteLine | D5 C.7，普通出字中点击补全；另值SkipToNext |
| clickWhileTypingFast / ClickWhileTypingFast | SkipToNext | D5 C.7，快进出字中点击下一句；另值CompleteLine |
| autoWait / AutoWait | 1.2秒 | D5 C.7，整句实际出完后等待的时长 |
| advanceCooldown / AdvanceCooldown | 0.3秒 | Schema v2.5“推进冷却”，每次接受手动点击后忽略后续点击 |
| cgAdvanceCooldown / CgAdvanceCooldown | 1.5秒 | Schema v2.5“CG推进冷却”，CG出现/收起/切换后忽略点击；这里用英文API名映射原中文参数 |

这些是文档给定的开发期预设，不是当前场景样例值或已人工终调值。时长允许0，必须是有限非负数；枚举必须为上述两个合法值，构造时拒绝错误参数。调用方以后可把取值快照的这些值传入，无需本卡先创建Config资产。字速/快进倍数/标点停顿由将来的文字呈现消费，本模型只提供FastForwardEnabled，不修改速度。

调用约定：

1. 构造一个模型（默认两个开关均关）。用 `SetAuto` / `ToggleAuto` 与 `SetFastForward` / `ToggleFastForward` 独立设置；可同时开启，没有长按入口。
2. 每句实际开始时调用 `NotifyLineStarted()`，只清本句等待信息，保留开关与两冷却。调用 `TryClick(phase, nowSeconds)`：Typing按对应点击参数返回补全/下一句，LineComplete返回下一句，Entering返回跳过入场，Inactive/未知上下文返回None。每个非None手动意图（包括跳过入场）都会启动手动冷却；被忽略的点击不刷新它。
3. 视图实际出完整句时调用 `NotifyLineCompleted(nowSeconds)`。本模型按这个时点计算AUTO等待，开关切换不重新起算；已出完且等待已满时打开AUTO，可立即返回下一句。重复通知不推迟等待，也不能重新激活已经消费的同句等待；下一句须显式NotifyLineStarted。
4. 用 `EvaluateAuto(phase, nowSeconds)` 取得AUTO意图，只有AutoEnabled、LineComplete及等待已满才返回SkipToNext；一个完成通知至多产生一次AUTO意图。快进单开不会自动推进。AUTO不读取、清除或重启手动/CG冷却。
5. 每次收到CG出现、收起或切换通知调用 `NotifyCgChanged(nowSeconds)`，它只刷新CG截止时间。手动点击须两冷却都到期；等于截止时间可接受，差一点仍忽略。快进点击同样受限，AUTO豁免。

所有时间来自调用方的同一单调时钟，以double秒传入；没有sleep、系统时间或真实输入读取。NaN/Infinity时间作为调用契约错误抛ArgumentOutOfRangeException，普通冷却/阶段拒绝通过None表达。调用方应立即消费一个非None意图并更新自己的阶段，再请求后续意图；不要用缓存的旧阶段重复消费。新会话可创建新模型，本模型不替未来状态机持有段句ID。

`DevTools/Narrative/NarrativeAdvanceInputTests.cs` 是既有命令测试EXE调用的新Run组，手写预期来自N0-11四行表、D5 C.7和Schema新增冷却项：包含两个参数另值、四种开关组合、AUTO等待、独立冷却的刚好/差一点、AUTO豁免及入场接受后的冷却。既有三程序集断言与命令测试原断言保持，仍只执行一次受影响Narrative Final；未运行Unity或其它业务回归。本卡无画面变化，五列视觉检查不适用。


## N0-02 纯曲线与旧镜头桥接

2026-11-14局部核对。纯 `EatWhat.Narrative.Core.NarrativeCurves` 位于 `Assets/Scripts/Narrative/Core/NarrativeCurves.cs`，提供 `Value(NarrativeCurvePreset,double)`、`Integral(...)` 和 `FromOrdinal(int curve)`。枚举顺序为Linear、Quadratic、SmoothStep、EaseInOut（0至3），对应Schema v2.5全局条目15；序列化的StreetCurvePreset保持原文件/原值不变。未来纯Core消费者直接调用此模块，不引用引擎或上游业务程序集。

四种取值/积分算式与Clamp表达式从旧StreetFollowPipeline原样迁出。输入先按原Math.Max/Min夹到[0,1]；无穷按边界处理，NaN保持原运算传播，非法枚举仍抛ArgumentOutOfRangeException且ParamName为curve、ActualValue为空。本次不新增阈值或替换算式。

Toolkit中 `EatWhat.Toolkit.NarrativeCurveBridge.Value(int,double)` / Integral只映射并委托；其公开签名不暴露Core类型，因此LongLoop保持原Toolkit引用，无需改LongLoop.asmdef。Toolkit新增引用现有Narrative.Core；Core引用列表仍空、禁止引擎，Narrative.Unity原直接引用不变，整体DAG无环。StreetFollowPipeline原两签名保留，其它调用方、配置资产和Inspector字段均保持，配置使用说明无参数变化。

独立永久oracle在 `Fixtures/StreetCurveOracle.cs`，保存修改前旧公式、Clamp与枚举，头部记原来源SHA。`NarrativeCurveTests.cs`逐条独立预期：4曲线各i/1000d的1001点，Value/Integral分别比较Core和实际薄桥源码输出的双精度位；另核越界、负零、无穷、NaN载荷、非法枚举异常与序号映射。原三程序集断言、621命令检查及87输入检查仍在原EXE执行，新曲线组只追加Run调用。薄桥既在真实Toolkit中编译，也以同一生产源码编入纯测试EXE，无替身数学实现。

既有两个Python入口只补实际DAG：Core先编译，Toolkit纳入新桥源/纯Core引用，LongLoop沿既有Toolkit引用。LongLoop的纯逻辑测试若直接编译Pipeline源码，同步纳入实际桥源码和新Core DLL；新增Core/隐式Toolkit依赖完整原流另记narrative-dependencies.json，保留原业务报告的记录索引。旧模式传来的已在当前Bee中的源候选按原路径去重，保留源存在性及实际引用检查，不重写业务断言。不启动Unity刷新Bee，不把编译警告当引擎验证。

本卡一次Narrative Final及现LongLoop业务模式各一次：default、avatar-init、street-config、ground-space、walk-area、depth-scale、y-sort、follow-pipeline、dynamic-zoom、look-float、vertical-offset、follow-composition、parallax、follow-driver逻辑组、follow-driver-v012。follow-driver使用既有logic-only开关，真实生产程序集由其余相关模式编译；不运行旧原生探针、历史反例、闭包打包/恢复、一次性payload编译或新补丁三模式子组。各原断言的实际结果见任务报告。本任务是保持原行为的数学抽取，没有新画面或参数，五列视觉检查不适用，机器等值不冒称新视觉签收。

本轮实际结果：Narrative/16861项曲线检查及原测试、LongLoop默认99的真实成功证据保持；当前N0-10同一次Narrative Final还覆盖新111条调度检查。Avatar旧CS0246失败由直属AI531限定修复并经CONTROL独审：真实Core/Toolkit/LongLoop生产编译通过，原24条headless控制流断言保持，缺DepthScale组件边界不冒称组件生命周期。原AI529同根续行的其余13模式现均离线通过，包含真实DepthScale/朝向生命周期及聚焦Driver和V012组。配置组旧shim缺Tooltip属性的实际首败、仅runner补Inspector属性源及一次重试均保留，旧C#测试/配置值不改；CONTROL独审3合同/7事实已通过，不代表Unity原生或视觉验收。


## N0-10 命令调度器与处理器契约

2026-11-14局部核对。`Assets/Scripts/Narrative/Core/NarrativeCommandScheduler.cs` 提供纯 `NarrativeCommandScheduler`、只读可观察的批次/命令状态和 `INarrativeCommandHandler`。本模块仅拥有命令执行状态和当前句的命令关联，不持有段句进度、是否可结束、输入冷却、参数快照、真实操作锁或引擎状态；不是对话FSM。

构造参数是 `Func<NarrativeCommandInvocation, INarrativeCommandHandler>`。工厂在提交时为可用的非wait命令创建已绑定参数的处理器，构造不能开始演出；wait不调用工厂。处理器必须实现：

| 成员 | 契约 |
|---|---|
| Begin(nowSeconds) | 开始对应命令，短调用立即返回，不阻塞文字出字 |
| IsComplete | 由处理器自己的效果更新状态，只供调度查询；瞬时命令应在Begin中落实并报告完成 |
| CompleteImmediately(truncation, nowSeconds) | 按传入签名截断类别直接落实最终状态；必须支持尚未Begin的顺序命令，不补播入场过程 |
| Cancel(nowSeconds) | 停止自身效果，允许尚未Begin；镜头接管不应用旧镜头的最终落点 |

本卡无14种真实处理器，测试文件的假实现承担所有效果。工厂/处理器回调不得重入调度器；切场景用专用待取信号避免在回调中直接重入FSM。缺失处理器跳过并诊断，回调异常记Failed，必要时尝试Cancel清理，不阻止其他可运行命令；这不证明真实效果成功。

调用顺序与参数：

1. `StartLine(parsed, level, nowSeconds, mode=Parallel)` 由未来状态机在本句开始出字的时点调用。先按签名收尾上一当前句，再提交新命令；返回批次句柄后文字可继续，不能等待它才开始出字。`TruncateCurrentLine(nowSeconds)` 也可在玩家推进时提前调用，下一StartLine不会重复收尾。
2. Parallel为Schema/D5预设，所有有效命令同一个传入时间开始；Sequential前一项完成后才开始后一项。wait按已解析double参数建立截止时间，0/负值不延后下一项，不新增数值范围规则；并行wait也可计时，但不阻塞其他命令或文字。Tick观察完成时才开始顺序下一项，不臆造未驱动时间区间的Begin时刻。
3. `Tick(nowSeconds)` 由调用方用同一单调时钟持续驱动；秒单位double，不读系统时间、不sleep/轮询设备。非有限时间为调用契约错误，抛ArgumentOutOfRangeException。完成批次从活动集合释放，返回的句柄仍可读状态。
4. 截断先观察当前运行项是否已自然完成，避免重复收尾；仍未完及尚未轮到的项按原序调用CompleteImmediately，wait直接停止。未开始项保持StartedAtSeconds为空，终结时间记录传入时点。重复截断/后续Tick不重复效果。
5. `EndDialogue()` 只解除当前句关联，批次标记为detached并继续后台执行；`StartOnEnd(...)` 自动先解除关联再提交段末批次，不截断旧工作。新对话的StartLine也不误截断旧后台批次。调用方在UI收起后仍须Tick到完成。
6. 剧情退场在新接管镜头动作注册前调用 `CancelCameraCommands(nowSeconds)`，取消全部活动批次里签名Truncation=SnapCamera的未完camera_pan/camera_return（含顺序未开始项）。其它actor/object/shake/CG命令保留并继续；这里的镜头类依据现签名类别，不另列一张命令表。
7. `TryTakeSceneSwitch(out invocation)` 在scene_switch正常完成或截断最终状态落实后给出一次信号。状态机消费信号结束本段，本调度器不替它推进段句、读取next_dialogue或切真实场景。

状态为Pending/Running/Completed/Truncated/Cancelled/Skipped/Failed；IsComplete只表示无Pending/Running，不是“效果均成功”。开始/终结时间和Invocation可观察，列表外部只读。必须传完整解析结果；解析失败时保留原诊断，连同保留的合法前缀也不派发，避免错误输入产生部分效果。

等级与版本检查只消费N0-03单一签名的Allows/IsAvailableInVersion，不可用项记一条原位置诊断并跳过（含actor_emote）。复用原NarrativeCommandDiagnostic，运行上下文原因使用ArgumentValue；原解析/目标类型与断言不改。OnlyOnEnd在Schema V-16仅为warning：台词里scene_switch记on_end提示但不新造运行拒绝；V-16b的并行wait信息由后续验证层负责，不改签名规则。

永久测试 `DevTools/Narrative/NarrativeCommandSchedulerTests.cs` 手写预期来自N0-10规则表与Schema对话命令/执行规则，使用假处理器和纯调用方时间；接入原EXE的一个Run组。覆盖同刻开始、顺序真实wait边界、全部截断类别/未开始尾链原序、结束后继续、仅镜头取消、场景信号、三等级跳过及缺失/异常处理器诊断。N0-10本卡只一次受影响Narrative Final，原3程序集、621命令、87输入、16861曲线检查保持；Avatar修复及旧LongLoop其余13模式由原AI529/531另行执行并记录于N0-02，不计入N0-10执行。无配置/Inspector或画面变化，五列人工视觉检查不适用。


## N0-06 演出配置数据与单资产类型

`DialoguePresentationData`在Core提供十个具体模板数组，每组初始一份default，使用现有曲线/命令执行方式/点击动作枚举；纯位置、RGBA及站位数组可序列化，Core实际引用仍仅mscorlib。`DialoguePresentationValidation.Validate`只报告组/模板/字段，不修值。FindTier等十个具体方法按Ordinal精确查找，空profile选择default，缺名诊断并回落唯一default；default缺失/重名或请求重名会报告，不构造替代数据。Find返回可变编辑对象引用，不是后续Resolver的冻结快照。

`Narrative.Unity.DialoguePresentationConfig`直接持有同一纯数据，提供Data/Changed/ValidateData/NotifyChanged；OnEnable建立比较基线，OnValidate按实际序列化内容比较并输出校验警告，不钳制或保存。Unity层编译增加真实UnityEngine.JSONSerializeModule引用，未在Mono调用JsonUtility、CreateInstance或生命周期，也未创建.asset或场景引用。参数/入口/单位和开发期预设的长期说明在[项目对话参数说明](../../docs/参数调整说明_对话.md)，这里不另存第二份参数表。

独立永久夹具`Fixtures/PresentationDefaults.v2_5.tsv`从D5 C.1～C.10与Schema七项覆盖手写预期，含完整字段/类型/值及站位数组展开187行，不从被测类型生成。现EXE追加`NarrativePresentationConfigTests.Run`，核十组default/自定义/缺失回落与重名、合法有符号位置/偏移、非法有限/非负/比例/枚举的精确字段定位、前后位与值保持；原组原文不改。唯一受影响Narrative Final9阶段通过，新组2641检查（416数值案例、13枚举字段），原3/621/87/16861/111也通过。当前只类型/纯逻辑与真实DLL编译，CONTROL独审3合同/7事实已通过，不代原生序列化/Inspector或表现验收。


## N0-07 参数声明注册与覆盖文本解析

`Core/DialogueOverrides.cs` 将原N006字段上的 `DialogueParameterAttribute.Unit` 与实际字段类型/组关联，由 `DialogueParameterRegistry.Entries/TryGet` 生成69个Ordinal的group.field唯一key；没有复制默认值表。单位声明只添加在原数据字段旁，去掉本次属性可逐字节恢复原数据源。独立D5单位预期和原187行字段夹具在新Run组逐项核一致性，不生成同源预期。

`DialogueOverrideParser.Parse` 返回Success、只读Values列表与含Key/Problem/Position/Reason的诊断。七类值使用原实际类型；语法、引号/转义、站位固定元组格式和合法范围见[项目对话参数说明](../../docs/参数调整说明_对话.md)末章。结果错误时Values为空，不静默最后值覆盖重复。N006校验仅作用于内部新建的隔离纯数据对象，不接受或写回用户模板/资产，不承担Resolver或快照所有权；复合返回值仍是新解析的可变编辑值。

原EXE追加一个NarrativeOverrideTests.Run，新组776检查覆盖完整69 key/单位/类型、7类值、所有字段合法预设、Schema曲线别名、复合/引号/文化区和40错误案例；原3/621/87/16861/111/2641与原187行夹具保持。唯一受影响Narrative Final9阶段0，Core9源且实际仍仅mscorlib，当前交付CONTROL已独审3合同/7事实通过。没有Unity/资产创建、原生或其它业务回归。

## N0-04 八表与作者工具夹具

2026-11-15局部核对：现有Test-Narrative.ps1在真实DLL/原组之后追加两个明确分界的组。NarrativeTableFixtureTests.cs与原DataTables/Test-CK01BImportContracts.ps1的CompileOnly共用45个生产/既有宿主源，加本测试共46源；实际调用生产BuildValidationContext、DataTableValidator、TryApplyRow及Catalog查询，现有UnityStubs只提供已存在的编辑器外承载，不执行AssetDatabase保存。1141检查覆盖独立105列契约/类型/必填/枚举/引用、20行和296运行字段、空/0/false、同场景完整组合键及缺失/重复/文件名诊断；comment沿旧作者专用规则排除生成行。真实DLL编译与这一宿主分开报告，不把宿主当原生导入。

永久输入在Fixtures/TablesV25：SchemaV25-source.md为T16～23原文及源SHA，expected-contract.json独立展开105列与引用，不从生产schema推导预期；8个CSV为手写样例，不写正式DataTables。AuthoringStyles.xml/source.json保存既有批准样式片段来源，保原STYLE_BASELINE守卫；不依赖任务inputs/outputs，也不带用户值/公式。当前cast_01～04完整展开，后续扩展须同时维护schema、行类型和样例，不加永久四人上限。

NarrativeTableAuthoringTests.ps1只作为原入口调用的夹具组，调用真实New/Update/Format/Node导出及原事务核心。独立临时种子含旧14页和hidden未知页，含手工文本/数值/bool/公式。原49及后续增补合计92检查验证旧内容/类型/公式/关联/空白结构、8页105表头、重复不写、旧值拒绝、224注释初始化/默认重复/人工编辑拒绝、全局格式与八表原文导出及源SHA不写回。所有输入/候选/输出在本任务新输出根；正式八份空CSV/主xlsx22页224说明已由DATA535完成，当前正式22表0error44warning1info、四旧完整入口各一次exit0并经CONTROL独审。原报告sheetCount14字面量保留，22/23覆盖据原断言及真实工作簿，不作14表投影。

CODE收口只覆盖离线逻辑和实际编译；运行资源、场景预摆anchor条件、链一致性/真实命令效果、加密发布集成与Excel客户端显示未在本组验收。项目长期配置说明在[八表配表说明](../../docs/剧情八表配表说明.md)，作者入口在[Authoring README](../DataTables/Authoring/README.md)。原各组和全部失败/许可史保留；当前TaskId整体结果以正式结算证据为准，不由本说明代签原N004或整批。

## N0-05 只读数据模型与目录

生产入口为Core/NarrativeDataModels.cs、Core/NarrativeDataDirectory.cs、Unity/NarrativeDataAdapter.cs。Core八类模型不含生成行或Unity对象；所有公开模型字段通过只读属性访问，嵌套集合在构造时复制并包为只读集合。NarrativeDataAdapter.FromRow提供八个实际生成行重载；TryCreate(catalog,out INarrativeDataDirectory,out diagnostics)将八表一次转换成冻结目录，缺表、非法tier或不完整/重复键明确失败且不发布部分目录。构造后修改源行、源列表或清空源Catalog不会改变模型。

角色的ConfiguredPortraitOffsetX/Y/Scale保留空值，PortraitOffsetX/Y/Scale只应用Schema规定的0/0/1；显式0保留，不做钳制或配置修复。CG的所有可空数值和布尔均为nullable，空transition/curve为null；横纵轴互不代填，空与false/0不同，不在本卡复制演出全局默认。完整版预留文字/偏移仅存储，不执行功能；作者comment沿N004规则不进入运行模型。

对话CastSlots保存当前生成行四组完整槽位（含未分配槽位的原值），Cast只包含已分配角色且按槽序；模型接受任意数量条目，没有永久四人上限，未来扩列由生成行和适配同步维护。位置、额外缩放、层号保nullable，站位和九组Profiles保存原名，空名仍为空，表示后续N008应选default；本层不查配置或决定位置/站位/模板优先级。Overrides原文保留，不提前调用覆盖集取值器。

NarrativeCommandSource保存命令列SourceText、Success和原N003诊断，Commands只有整体解析成功才可非空。适配T17.on_end/T18.commands时各调用一次原解析器，语法/参数失败仍保完整原文和诊断位置，成功前缀不会进入可执行列表；目录查询和取链不重新解析、不执行命令。Dialogue.OnEnd、DialogueLine.Commands均返回同一缓存对象。等级、处理器、on_end位置执行规则仍属于既有后续调度/状态机职责。

INarrativeDataDirectory提供TryGetCharacter/Dialogue/DialogueLine/Cg/Scene/AmbientPool以及TryGetSceneState(sceneId,stateId)、TryGetSceneStatePlacement(sceneId,stateId,kind,target)，均返回bool、模型及诊断字符串。键精确区分大小写；内部完整元组键不依赖分隔符拼接，不会把跨场景同名状态/目标合并。缺失或重复返回false和null模型；构造时的null行/不完整键/重复保留在只读Diagnostics中，不静默挑第一项。

TryGetLineChain(dialogueId,out chain,out diagnostic)从first_line沿next_line走到空尾，忽略输入/CSV顺序；dialogue_id只是标签，冲突给DialogueLabelMismatch警告并保留链后继，完整链仍返回true。缺对话/首句为空/缺后继/重复行/环路返回false、错误字符串以及已确定的前缀和只读诊断；重复节点不再次加入。链查询不改变目录或保存对话进度，每次结果集合独立，模型和命令缓存复用。

新NarrativeDataDirectoryTests加入原Mono命令测试EXE，181检查覆盖八类查询、完整键、缺失/重复、链/环/标签、只读与空值；原示例和所有旧组断言保留。NarrativeDataAdapterTests使用永久TablesV25八表/独立105列契约，通过原TryApplyRow创建实际生成行，再运行生产适配；1483检查包含20行全部296非comment运行字段逐项对照、修改每个源字段/列表及清空源Catalog后再对照，共592次字段比较，另含hasValue空/0/false、原文/失败前缀、缓存与缺表失败。

该适配组仅在原DataTables StaticCompile宿主离线运行；额外源和宿主/引用SHA在model-host.preflight.json执行前冻结，真实Narrative.Core/Unity/Generated等DLL编译另按原Python闭包证据记录，不把此宿主当AssetDatabase原生保存。首轮新目录HashSet引入System.Core被原“仅mscorlib”边界断言拒绝；只把新目录集合改成Dictionary后同key一次复验，原断言/首败/源码闭包都保留。没有运行Unity、创建配置资产、接FSM/视图/处理器或实现N008 Resolver；无新增Inspector配置说明项。

## N0-08 Resolver接口

Core/DialoguePresentationResolver.cs复用N006 Find/Validate和69字段同源注册；Core/DialoguePresentationSnapshot.cs提供不可变结果。Resolve(data,NarrativeDialogue,triggerTemplateName=null)返回DialoguePresentationResolution：Success/Snapshot、ConfigurationDiagnostics和OverrideDiagnostics。完整快照十组/69项；九组模板名来自N005.Profiles，trigger名仅来自caller。ResolveTrigger(data,name)另返回仅trigger组的DialogueTriggerResolution，不依赖其它组值；完整Resolve会验证完整配置，非法default/配置不给快照。未知模板的warning与合法fallback由原Find提供。

DialogueOverrideParser.ParseItems是最小兼容新入口：Items含SourceText/Position/Key/Value/Success/Diagnostics，Values仅含合法独立项；原Parse仍原子失败/Values空。两入口共用一个原scanner/Assignment/ValueReader、相同注册/约束和诊断位置。首次出现保留key，后续重复跳过且诊断；未闭合引号的尾部不另写猜测分割。ResolveWithOverrides可消费缓存的ItemsResult，冻结后修改其中原复合值不影响快照。

Snapshot.Get<T>(group.field)、GetGroup(group).Get<T>(field)按实际参数类型读取；Groups/Parameters及组Parameters只读，Definition仍是原注册定义。数值/布尔/枚举/字符串保持原类型；Point使用DialoguePointSnapshot（X/Y），Color使用DialogueColorSnapshot（R/G/B/A），Stations使用IReadOnlyList<DialogueStationSetSnapshot>，每组ActorCount/Entries、条目Id/Position/Scale/Layer均只读。不会返回原authoring数组、站位对象或可变点色。

ResolveCg(snapshot,NarrativeCg)返回DialogueCgResolution，在实际CG消费点独立处理每个nullable列（表明确值优先，否则本段CG组）；显式0/false和两轴空值保持，源模型不改。曲线复用原枚举解析；无CG播放/计时/表现/FSM。Snapshot的OverrideSourceText保原输入，含错误项时允许取值成功并保OverrideDiagnostics；致命配置错误不发布快照，调用方先检查Success。

新DialoguePresentationResolverTests加入原Mono EXE，435检查复用永久187行D5/Schema夹具，对69默认及69逐项覆盖、三层整组替换、caller/独立trigger、unknown/default错误、11词法边界/原子Parse兼容、源和返回复合值深只读、跨段不串、CG两轴/0/false独立回落核对。一次相关Narrative Final通过，Core实际仍只引用mscorlib，原全部组（含N005181/1483、592字段对照）保持。StaticCompile因自动纳入新增Core源扩为61源，仍与真实DLL编译分报，不当作原生保存。配置人员说明原位更新于docs/参数调整说明_对话.md；没有新Inspector入口或配置资产，整批/原生与表现验收不由本说明代签。

## N0-12 普通/剧情对话状态机

生产入口Core/NarrativeDialogueContracts.cs和NarrativeDialogueStateMachine.cs。构造注入INarrativeDataDirectory、实际N008 resolver函数、INarrativeDialogueView/Lock/Clock/TalkedRecord、既有NarrativeCommandScheduler与NarrativeAdvanceInputModel；只有接口和纯FSM，真实能力均未实现。resolver可由caller捕获配置/trigger名后调用DialoguePresentationResolver.Resolve，FSM不保存可变配置或复制默认。

Start(id)返回Started/Busy/NotApplicable/Failed。正常先取模型、first/next链与不可变快照再加锁/DialogueStarted；缺失/重复/链环或致命配置失败在锁前明确返回，标签warning保留。ambient不适用且不解析快照/持锁。story先发SetLetterbox(true)，收到同一Context的有效NotifyStoryBarsReady才发PrepareStoryCamera，之后有效NotifyStoryCameraReady才Enter；提前镜头回报返回false且不消费，重复/过期完成不重复请求或入场；normal直接Enter。进入期间Click复用N011，SkipEntrance发CompleteEntrance后直接开始首句。能力请求禁止同步重入FSM/调度器，外部在请求返回后报告完成。

每段有新NarrativeDialogueContext，每句有新NarrativeLinePresentation。外部须用原对象回报NotifyStoryBarsReady/StoryCameraReady/EntranceCompleted/ExitCompleted及NotifyLineCompleted，过期/重复对象不推进当前段。消息保原speaker/text/name key，不访问本地化；按句name_override优先，否则角色name_key，最终空则ShowNameBox=false。speaker不在实际Cast则AllPortraitsDim=true，角色主键按Schema精确匹配；多实例命令身份直接保留N003已解析对象，不另拆#后缀。

首句/后继开始时只取一次caller时刻：输入NotifyLineStarted，StartCachedLine（原N010），SetSpeaker、ReactToLine、StartText均用同一时刻。命令与出字并行开始，不等命令完成；原缓存无效列仍保诊断且不执行前缀。视图负责出字，CompleteLine只请求CompleteText，必须等原消息的完成报告；非末句SkipToNext先截断当前命令，才发下一句。末句fast跳过会先请求出完、收到报告后给CanEnd并退场；正常末句报告仅给CanEnd，等下一次有效点击或AUTO。story在CanEnd时发撤黑边。

退出由view.Exit完成报告决定；FSM不实现打字机/入退场计时。EndDialogue只解除前台关联，story取消原camera类命令，其它后台不截断；然后StartCachedOnEnd、MarkTalked一次、Release锁、DialogueEnded依序。后台与顺序尾链继续由外部scheduler.Tick(now)驱动；FSM.Tick仅接收调度信号并调用N011.EvaluateAuto，不代跑调度器，也不把后台批全部结束当解锁门。每段设置经Input.ConfigureSettings从快照更新，输入开关、手动/CG冷却截止和等待字段仍在N011；仅NotifyLineStarted重置该句等待，既有冷却不清零。

完整续接先退场/记录/解锁/结束，再重新取下一段与快照并从开始走。同级Seamless仅省前段Exit和后段Enter/准备请求，发UpdateCast后开始新句；每段的on_end/记录/解锁/结束/重新加锁/开始通知保持。story无缝结束仍取消旧camera类；不同等级一律完整走。源模型或后继链/配置出错给诊断并不持新锁，不偷偷执行错误链前缀。

Tick从原TryTakeSceneSwitch消费真实信号，丢剩余句并进入退出；回报结束/解锁后有后继也不假定场景已经完成。on_end或最后后台句存在尚未执行的scene_switch时同样等待，避免顺序wait尚未到切场景就先开下一段。PendingSceneSwitches只读列出实际invocation，外部通过INarrativeSceneSwitchCompletion.NotifySceneSwitchCompleted(原invocation)逐项确认，错误/重复身份拒绝，全部实际请求完成且不再有待发切场景才从完整流程开后继。没有handler/执行失败则留明确诊断、不伪造完成/自动开后继；锁已按原步骤释放。没有真实场景服务或完成通知实现。

Scheduler新增两个命名桥接方法StartCachedLine/StartCachedOnEnd，复用原Start，不从SourceText再解析；旧StartLine/OnEnd及全部后台/截断代码不变。Input新增仅换不可变Settings的ConfigureSettings，不移动/清除任何原状态，旧API不变。剥这两个新增桥接区及Main/Python新一行，原文件整份字节还原。

新增NarrativeDialogueStateMachineTests的84检查只使用假view/lock/clock/record/handlers，覆盖原十步、story三处、同时间、两标志/改名/旁白、缓存实例身份、跳入场/补全/fast/AUTO、截断与后台独立、两种续接/跨级强制完整、在线/延后on_end切场景及错误完成、ambient和失败锁前拒绝/记录一次。一次相关Narrative Final全部通过，真实Core仅mscorlib，63源StaticCompile及实际DLL分报。没有真实输入、视图、锁、记录、时钟、场景、资源或Inspector配置；无新增配置/视效说明项，原docs参数说明不改，不代签原生/视觉或整批。
