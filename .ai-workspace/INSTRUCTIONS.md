# AI 指令账本

本文件保留活动登记入口与未关闭内容，后续由既有 queue/监管入口正常追加。实际任务、模式、编号和失败/续行历史以 runtime 正式状态为准；历史授权不触发当前业务。

- 已关闭 AI-000001～145：[原阶段归档](archive/2026-09-17/README.md)。
- 已关闭 AI-000146～262：2026-10-18原字节归档，见[本批记录](archive/2026-10-18-instructions-slim/README.md)和[永久账本索引](archive/tracking/INSTRUCTIONS_ARCHIVE_INDEX.json)。任务/修订/监管事件按manifest中的UTF-8字节位置查询；无需日常全文读取。
- 每批瘦身依[规范第16条](DOC_MAINTENANCE.md#严格规范)：先核关闭及活动依赖，整份原件入档并验证，保留未结完整块后轮转；不得清零队列、错误或许可史。

以下USER块按原字节留存。表内TODO是原始登记时的状态，不代表当前状态；最新结果只读[USER_WORK_STATUS.md](inputs/USER_WORK_STATUS.md)（目前U2～U6未结）。

## [USER] 伪车道（G64，不占 AI 编号、不派发）

权威状态源：[USER_WORK_STATUS.md](inputs/USER_WORK_STATUS.md)。下表是依赖阅读入口；状态更新必须写回权威源。AI 后置支撑任务领取前检查对应 USER 项，未 DONE 时等待且不计失败。

| 编号 | 工作项 | dependsOn | 当前状态 |
|---|---|---|---|
| USER-U1 | 正式配表内容配置（14 表 demo 口径） | F-3/A0 工具交付 | TODO |
| USER-U2 | 排布·P0 食谱本 | A2a 前置支撑 | TODO |
| USER-U3 | 排布·P1 托盘与格架 | A2a 前置支撑 | TODO |
| USER-U4 | 排布·线索板与镜头 | F2-a ④展开态补图、A2a | TODO |
| USER-U5 | 白名单与分区约束复核 | USER-U1 | TODO |
| USER-U6 | 数量与文案定稿（可选） | 无 | TODO |


## AI-000263 | [CODE] [SC] [DETACH-B13-20261019] ASM-01 CODE程序集落地包与窗口①清单

- 序号：263
- 提交时间：2026-10-19T02:26:23.5867377+08:00
- 模块 ID：SC
- 初始状态：QUEUED
- 前置依赖：AI-000243, AI-000244
- 允许越过：False
- 最大尝试次数：1
- 复杂度/轮次预算：L / 120

### 指令

本卡是原 ASM-01 的 CODE 交付阶段，原卡完整目标仍保留，完成本阶段不代表 E-1/ASM-01 已落地。用户已明确接收派发包并进入监管，依赖AI243/244的CODE-R01已关闭。阅读 CORE、workers/CODE、SUPERVISED/HANDOFF 的执行/回传节、CODEBASE_MAP/项目基线页首及相关模块、toolkit/INDEX与本卡对应P项；不通读整个派发包或历史报告。原卡：.ai-workspace/inputs/派发/DETACH-B13-20261019/cards/ASM-01.md；本轮优先执行边界：同目录上级的执行登记.md（ASM首次范围段）。总档v1.5.4优先于原包旧路由。固定CODE独占+unity-project互斥。
交付一个完整可审查的程序集落地包：1) 据现有源码/asmdef/asmref/Editor引用核对七类程序集，标明真实路径、现有反向依赖、第三方边界及不能在不改既有.cs下成立的点；2) 在本任务outputs/staged中生成可实施的asmdef/asmref候选与明确的新接口草案，按实际覆盖关系选择最小方案，未定项显式阻止投放，不伪造已安装；3) 进行相称离线JSON/引用图/候选范围验证，提供引擎落地和后继DATA一次幂等所需的实际输入清单与验证命令草案；4) 完整精确列出窗口①Legacy源→目标、GUID、备份与保持项，并核实卡中“人为引编译错误仍可Play”的可行性及验收口径冲突，形成可供用户决定的最小选项；5) 交付简短报告、evidence.json与acceptance.json。正文不复述全卡。
允许本任务outputs中的源码/配置候选与说明，无生产写入：不向Assets/DevTools投放，不改已有.cs/namespace，不改.meta，不做文件移动，不开Editor/Play/导表，不提交/推送，不调整_TUNE。图分析用于准备经审批的真实落地，并非用静态通过代替后继Editor/Play验收；不在主工程注入编译错误。已有脏文件完整保留。既有.cs与新接口的冲突要列清，不自行扩大业务范围。原用户窗口①保留，本阶段机器过后等待CONTROL精确清单审查及窗口授权，ENGINE_CLI承接移动和Editor验证，DATA承接幂等；其余CODE卡待整个E-1落地后才入队。
本卡complexity L/120沿原卡。验收预声明：.ai-workspace/inputs/acceptance/本TaskId.json，领取后用实际TaskId定位。输出根 .ai-workspace/outputs/DETACH-B13-20261019/本TaskId。六项基于实际产物：dependencyAuditComplete、candidateBundleValidated、window1ManifestReady、limitationsExplicit均true，existingCsWrites=0，productionAssetWrites=0；证据保留真实分析明细、候选校验与diff核对，不把结论布尔自填当验证。若目标内准备工作真实失败按Fail，不将需求待审判为运行失败。完成只Complete这个CODE交付，待人工事项写明，HumanGateAfter已登记；不声称ASM整体成功、不自行领取后继。
SUPERVISED凭本次TaskId/DispatchToken/OwnerThreadId领取一次；每批写入前SupervisionCheckpoint，长任务及时RenewLease。Complete/Fail/Yield按长期授权即时Prepare→读取完整messagePath→一次实际app send到当前CONTROL 01a13ef5-eda9-7882-b1c9-eca47a6470d9→Record后结束。未知计费unknown，失败恢复只能用户明确次数范围ApproveResume，保持历史与四条硬上限。

> SUPERVISED POLICY at 2026-10-19T02:26:42.9121019+08:00: {"taskId":"AI-000263","policy":{"resourceKeys":["unity-project","documentation"],"reviewDependsOn":[],"humanGateAfter":true,"gateApproved":false,"reason":"原ASM-01窗口①：CODE落地包完成不代表E-1完工，具体Legacy移动与MIG-F02参数保留用户审批；独占CODE及工程","updatedAt":"2026-10-19T02:26:42.8329070+08:00","supervisionLevel":"full"}}

> SUPERVISED START at 2026-10-19T02:26:43.6258014+08:00: {"sessionId":"beefe968cb5e48e0bea0b8b47260abd9","status":"ACTIVE","controllerThreadId":"01a13ef5-eda9-7882-b1c9-eca47a6470d9","taskIds":["AI-000263"],"heartbeatAutomationId":"watch-60","createdAt":"2026-10-19T02:26:43.6173781+08:00","stopRequested":false,"exitRequested":false,"reservations":[],"lastSweepAt":null,"delegatedAuthority":"Task-scoped project execution and necessary repair; not human review, product decisions, or platform permission bypass.","heartbeatMinutes":60,"maxRepairsPerProblem":3,"maxRepairsPerTask":6}

> SUPERVISED AUTOMATION_BOUND at 2026-10-19T02:27:17.6066658+08:00: {"automationId":"watch-60","heartbeatMinutes":60}
## AI-000264 | [CODE] [SC] [DETACH-B13-20261019] ASM-01 窗口①兼容实施与可安装包

- 序号：264
- 提交时间：2026-10-19T13:27:39.6120234+08:00
- 模块 ID：SC
- 初始状态：QUEUED
- 前置依赖：AI-000263
- 允许越过：False
- 最大尝试次数：1
- 复杂度/轮次预算：L / 120

### 指令

ASM-01窗口①已获用户明确批准。交付可安装的程序集拆分兼容代码和候选包，承接AI263准备成果，不重跑准备卡。范围：Assets/Scripts/Command/{CommandRegistry,CommandEntry,CommandManager}.cs的必要最小修改，限制发现已审自有程序集并兼容现有保存的旧命令ID/类型名；不能扫描任意第三方或改用户配置。Assets/Scripts/Cooking/ShortCycle/ShortCycleAutomatedProbe.cs仅对齐G68/E-06固定线索板与可追踪检查点，G26保留，不引入新产品行为。四个runner可按审查单修改路径/默认DLL定位，迁移前后均有清晰可用路径，不无声跳过验证。测试可在这些既有runner或本任务output加入必要定向用例，不建新长期工具。
读取AI263的REPORT、candidate-manifest、window1-manifest、limitations、engine-data-handoff、documentation-impact；仅按需补依赖明细。原42个Legacy和1个InputManager移动已批准，但只由ENGINE_CLI执行。CODE不得移动生产源码/meta，不向Assets安装asmdef/asmref/新MG0接口，不开Editor。将AI263候选复制为本任务staged派生包，记录原manifest身份；根据实际兼容修订核定10asmdef/11asmref及MG0接口。MG0接口签名由实现层按G84（计时秒、stub quality=1、取消无成功回调、单次完成、Teardown后无回调）和P2现行指导书收敛，不做正式小游戏或改变产品流程；新接口由ENGINE在安装阶段投放。
验证必须包含实际拆分后的程序集编译（使用精确映射和当前Unity/package引用，不以合并Roslyn零诊断冒充），命令9项发现/旧ID兼容及最小失败路径；已批准隔离编译输入人为使LongLoop失败，再证独立Toolkit/Generated/ShortCycle编译成功，恢复隔离输入。不得在主工程引入错误；不用复制Unity Library或整项目冷加载。空LongLoop需要的仅隔离测试源留output，不投放生产。runner定向验证；不跑字体重建或一次性工具。产物/证据和CODEBASE_MAP/toolkit对应变更说明准确区分“兼容代码已改、程序集待ENGINE落地”。
交接ENGINE需给精确安装manifest、staged完整文件、允许的移动清单、当前源版本及新.cs允许差异、最低原生验证清单；不能继续沿AI263整个源码SHA断言已批准修改的源完全不变；GUID不变仍必须验证。不重复审计全项目，不把生产Editor/Play宣称通过。合同：commandCompatibilityPassed、fixedBoardProbeAligned、runnerPathsCompatible、splitAssemblyCompilationPassed、isolationExperimentPassed、handoffBundleReady=true；unauthorizedProductionWrites=0、productionMoves=0。

共同规则：用户批准原件 inputs/派发/DETACH-B13-20261019/窗口①用户批准.json；本批执行登记与窗口①控制审查为范围依据。CORE→本线worker→SUPERVISED/HANDOFF→本卡涉及源码与P/TK，按标题读取，不加载AI263整份审计/全量diff。固定分支codex/unity6-followup，保留所有已有变更；不提交推送。旧失败/许可/次数保留，任何正式失败只按新ApproveResume续行。每批写前Checkpoint，长任务续租；Complete/Fail/Yield后立即Prepare→一次实际Send→Record回当前CONTROL。OwnerThreadId、ExecutionId随领取回执显式传递。输出 .ai-workspace/outputs/DETACH-B13-20261019/<TaskId>/，预声明 inputs/acceptance/<TaskId>.json，evidence.json与acceptance.json须来自实际证据。报告标明原逻辑卡ASM-01阶段；原建议总预算120的阶段累计不因分线清零，未知轮次/token保持unknown。证据目标1MB/硬上限10MB，超过1MB说明必要性；只保存任务范围diff、一次必要基线，不复制整份13MB工作区diff，不重复全路径SHA。大原生/机器审计必要时无损压缩后引用，不丢失败史；任何未结原件不擅自删除。
## AI-000265 | [ENGINE_CLI] [SC] [DETACH-B13-20261019] ASM-01 引擎落地与MIG-F02身份配置

- 序号：265
- 提交时间：2026-10-19T13:27:39.9870172+08:00
- 模块 ID：SC
- 初始状态：QUEUED
- 前置依赖：AI-000264
- 允许越过：False
- 最大尝试次数：1
- 复杂度/轮次预算：L / 120

### 指令

ASM-01窗口①正式落地 + MIG-F02，同一引擎阶段，依赖本批CODE兼容实施成功并经CONTROL独立验收。使用当前PIPELINES固定ENGINE_CLI与Unity官方CLI；领取前从正式前置result定位CODE安装包，不使用AI263未更新候选直接安装。不切Coplay。先核唯一项目/6000.3.24f1/Editor PID和心跳、已保存/未保存状态；实际调用经Invoke-TaskTool；未知dirty、目标歧义或新失败停止，不覆盖用户未保存成果。
用户已批准42对Legacy移动+1对InputManager，具体路径/GUID见AI263 window1-manifest与本轮授权。先保全当前字节至任务专属备份，再逐文件AssetDatabase.MoveAsset；不搬整个目录、不删除Legacy、保留GUID和G26/ProjectValidation/Spine预研。同一受控导入批次安装CODE交付的asmdef/asmref及MG0接口，目录经Editor建立、新meta交Unity生成；不人工改YAML。按CODE交付manifest核当前源漂移，允许AI264已获批准的真实修改；不能按旧SHA拒绝合法前置成果。生产新功能只到程序集分层和最小接口。
MIG-F02通过Editor PlayerSettings.companyName设为精确“Trollemma”，productName设为精确“EatWhat”；记录修改前后真实persistentDataPath，ProjectSettings这部分diff只允许这两个字段。备份当前值DefaultCompany/Button Project；不迁移/删除旧路径存档，不把新身份当保存系统完成。
原生验证：编译0 Error；七类归属按实际Assembly输出核定（空LongLoop不伪报DLL）；43个移动GUID无差异；命令9项发现及已保存命令/事件解析正常；ShortCycle现有探针按已批准固定线索板口径运行、CookingProcess仍Build index1且可加载无运行错误；保留Build列表、_TUNE、字体/第三方和其它用户成果。临时场景变化按UNITY_SCENE_CLEANUP归属恢复，不用MarkSceneClean掩盖。不要重复做CODE隔离错误实验，正式工程必须0Error后Play。若确需额外序列化字段修订先记录精确对象/字段差异交CONTROL，不未经审查批量重绑；已批准移动过程的正常导入元数据变化应具体留证，不自动判未知失败。
同步实际程序集/路径及MIG-F02工程事实到现有CODEBASE_MAP相关模块、toolkit对应条目和项目状态受影响节，页首只说明本次核验范围；不提高完整全项目核验日期。交DATA一份ENGINE落地后的Generated/currentCSV真实基线与包输入，不发布生成物、不重建字体。合同：editorCompileErrors=0,movedSourceCount=43,movedGuidMismatchCount=0,candidateInstallComplete/commandCompatibilityPassed/shortCycleProbePassed/companyIdentityCorrect=true,cookingProcessRuntimeErrors=0,settingsChangedFieldCount=2,unexpectedSerializedChanges=0。ASM整体还需DATA幂等，人工A+C保留。

共同规则：用户批准原件 inputs/派发/DETACH-B13-20261019/窗口①用户批准.json；本批执行登记与窗口①控制审查为范围依据。CORE→本线worker→SUPERVISED/HANDOFF→本卡涉及源码与P/TK，按标题读取，不加载AI263整份审计/全量diff。固定分支codex/unity6-followup，保留所有已有变更；不提交推送。旧失败/许可/次数保留，任何正式失败只按新ApproveResume续行。每批写前Checkpoint，长任务续租；Complete/Fail/Yield后立即Prepare→一次实际Send→Record回当前CONTROL。OwnerThreadId、ExecutionId随领取回执显式传递。输出 .ai-workspace/outputs/DETACH-B13-20261019/<TaskId>/，预声明 inputs/acceptance/<TaskId>.json，evidence.json与acceptance.json须来自实际证据。报告标明原逻辑卡ASM-01阶段；原建议总预算120的阶段累计不因分线清零，未知轮次/token保持unknown。证据目标1MB/硬上限10MB，超过1MB说明必要性；只保存任务范围diff、一次必要基线，不复制整份13MB工作区diff，不重复全路径SHA。大原生/机器审计必要时无损压缩后引用，不丢失败史；任何未结原件不擅自删除。
## AI-000266 | [DATA] [SC] [DETACH-B13-20261019] ASM-01 落地后一次导表幂等验证

- 序号：266
- 提交时间：2026-10-19T13:27:40.1346839+08:00
- 模块 ID：SC
- 初始状态：QUEUED
- 前置依赖：AI-000265
- 允许越过：False
- 最大尝试次数：1
- 复杂度/轮次预算：M / 80

### 指令

ASM-01落地后的唯一一次DATA幂等验证，依赖本批ENGINE阶段成功且CONTROL独立验收。当前固定DATA以PIPELINES/runtime正式绑定为准，旧workers页中2026-09 threadId已过时，不重新绑定或使用旧thread。先读CORE、DATA worker、DevTools/DataTables/DATA_PIPELINE及CurrentCsvValidation节。
沿ENGINE落地后交付的基线与实际current CSV/schema，在unity-project/data-batch互斥和既有data-publication.lock内，使用现有Run-DataPipeline.ps1 -CurrentCsvValidation -ValidationSource本任务明确标识 -TaskId实际号 -UnityPath当前6000.3.24f1。只一次真实隔离运行，不带-Publish、不消费旧DONE轮次、不改人工xlsx/CSV、生产Generated或字体；本卡并非新一轮导出/格式维护，不碰未授权非空内容。允许现有工具必须的隔离生成，副本不得复制Library/Temp/Logs/obj/.git。旧字体批准不适用。
核当前nonce/run/退出码/严格日志/dependencyProof及候选Generated与ENGINE后置基线一致；若程序集序列化名称确有预期外差异，报告原差异，不发布、不再跑一遍制造零差异。失败按正式Fail，原任务留存，续行要用户批准。运行后按既有工具流程释放锁，保留完整报告与必要恢复证据，避免再次积累整项目副本；未结输出不擅自删除。
合同：batchExitCode=0,currentRunIdentityVerified=true,generatedIdempotent=true,productionValueChanges=0,publishInvocations=0,realImportRuns=1。若通过报告“ASM-01 DATA幂等阶段通过”，由CONTROL综合CODE/ENGINE/DATA决定E-1已落地并安排后继；不得自动领取新任务或宣称A+C人工验收已过。

共同规则：用户批准原件 inputs/派发/DETACH-B13-20261019/窗口①用户批准.json；本批执行登记与窗口①控制审查为范围依据。CORE→本线worker→SUPERVISED/HANDOFF→本卡涉及源码与P/TK，按标题读取，不加载AI263整份审计/全量diff。固定分支codex/unity6-followup，保留所有已有变更；不提交推送。旧失败/许可/次数保留，任何正式失败只按新ApproveResume续行。每批写前Checkpoint，长任务续租；Complete/Fail/Yield后立即Prepare→一次实际Send→Record回当前CONTROL。OwnerThreadId、ExecutionId随领取回执显式传递。输出 .ai-workspace/outputs/DETACH-B13-20261019/<TaskId>/，预声明 inputs/acceptance/<TaskId>.json，evidence.json与acceptance.json须来自实际证据。报告标明原逻辑卡ASM-01阶段；原建议总预算120的阶段累计不因分线清零，未知轮次/token保持unknown。证据目标1MB/硬上限10MB，超过1MB说明必要性；只保存任务范围diff、一次必要基线，不复制整份13MB工作区diff，不重复全路径SHA。大原生/机器审计必要时无损压缩后引用，不丢失败史；任何未结原件不擅自删除。

> SUPERVISED POLICY at 2026-10-19T13:27:40.3611471+08:00: {"taskId":"AI-000264","policy":{"resourceKeys":["unity-project","documentation"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"用户已批准窗口①和必要兼容修订；按CODE→ENGINE_CLI→DATA依赖串行，保留窗口②/A+C与真实失败门","updatedAt":"2026-10-19T13:27:40.3057300+08:00","supervisionLevel":"full"}}

> SUPERVISED POLICY at 2026-10-19T13:27:40.4772152+08:00: {"taskId":"AI-000265","policy":{"resourceKeys":["unity-project","documentation"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"用户已批准窗口①和必要兼容修订；按CODE→ENGINE_CLI→DATA依赖串行，保留窗口②/A+C与真实失败门","updatedAt":"2026-10-19T13:27:40.4262027+08:00","supervisionLevel":"full"}}

> SUPERVISED POLICY at 2026-10-19T13:27:40.6135001+08:00: {"taskId":"AI-000266","policy":{"resourceKeys":["unity-project","data-batch"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"用户已批准窗口①和必要兼容修订；按CODE→ENGINE_CLI→DATA依赖串行，保留窗口②/A+C与真实失败门","updatedAt":"2026-10-19T13:27:40.5589161+08:00","supervisionLevel":"full"}}

> SUPERVISED EXTEND at 2026-10-19T13:27:40.7732461+08:00: {"reason":"用户明确按推荐范围批准并继续；CompanyName=Trollemma/ProductName=EatWhat；授权原件inputs/派发/DETACH-B13-20261019/窗口①用户批准.json","taskIds":["AI-000264","AI-000265","AI-000266"]}

> SUPERVISED USER_GATE_APPROVED at 2026-10-19T13:27:40.9026813+08:00: {"userDecision":"用户原话：按推荐范围批准并继续；MIG-F02值：Trollemma/EatWhat。完整问题与范围见窗口①控制审查.md，批准原件inputs/派发/DETACH-B13-20261019/窗口①用户批准.json。","taskId":"AI-000263"}
> Instruction revision for AI-000265 at 2026-10-19T13:49:12.1094924+08:00

ASM-01窗口①正式落地 + MIG-F02，同一引擎阶段，依赖本批CODE兼容实施成功并经CONTROL独立验收。使用当前PIPELINES固定ENGINE_CLI与Unity官方CLI；领取前从正式前置result定位CODE安装包，不使用AI263未更新候选直接安装。不切Coplay。先核唯一项目/6000.3.24f1/Editor PID和心跳、已保存/未保存状态；实际调用经Invoke-TaskTool；未知dirty、目标歧义或新失败停止，不覆盖用户未保存成果。
用户已批准42对Legacy移动+1对InputManager，具体路径/GUID见AI263 window1-manifest与本轮授权。先保全当前字节至任务专属备份，再逐文件AssetDatabase.MoveAsset；不搬整个目录、不删除Legacy、保留GUID和G26/ProjectValidation/Spine预研。同一受控导入批次安装CODE交付的asmdef/asmref及MG0接口，目录经Editor建立、新meta交Unity生成；不人工改YAML。按CODE交付manifest核当前源漂移，允许AI264已获批准的真实修改；不能按旧SHA拒绝合法前置成果。生产新功能只到程序集分层和最小接口。
MIG-F02通过Editor PlayerSettings.companyName设为精确“Trollemma”，productName设为精确“EatWhat”；记录修改前后真实persistentDataPath，ProjectSettings这部分diff只允许这两个字段。备份当前值DefaultCompany/Button Project；不迁移/删除旧路径存档，不把新身份当保存系统完成。
原生验证：编译0 Error；七类归属按实际Assembly输出核定（空LongLoop不伪报DLL）；43个移动GUID无差异；实际23项ShortCycle命令发现及已保存命令/事件解析正常（AI264依据源码修正AI263的9项误记）；ShortCycle现有探针按已批准固定线索板口径运行、CookingProcess仍Build index1且可加载无运行错误；保留Build列表、_TUNE、字体/第三方和其它用户成果。临时场景变化按UNITY_SCENE_CLEANUP归属恢复，不用MarkSceneClean掩盖。不要重复做CODE隔离错误实验，正式工程必须0Error后Play。若确需额外序列化字段修订先记录精确对象/字段差异交CONTROL，不未经审查批量重绑；已批准移动过程的正常导入元数据变化应具体留证，不自动判未知失败。
同步实际程序集/路径及MIG-F02工程事实到现有CODEBASE_MAP相关模块、toolkit对应条目和项目状态受影响节，页首只说明本次核验范围；不提高完整全项目核验日期。交DATA一份ENGINE落地后的Generated/currentCSV真实基线与包输入，不发布生成物、不重建字体。合同：editorCompileErrors=0,movedSourceCount=43,movedGuidMismatchCount=0,candidateInstallComplete/commandCompatibilityPassed/shortCycleProbePassed/companyIdentityCorrect=true,cookingProcessRuntimeErrors=0,settingsChangedFieldCount=2,unexpectedSerializedChanges=0。ASM整体还需DATA幂等，人工A+C保留。

共同规则：用户批准原件 inputs/派发/DETACH-B13-20261019/窗口①用户批准.json；本批执行登记与窗口①控制审查为范围依据。CORE→本线worker→SUPERVISED/HANDOFF→本卡涉及源码与P/TK，按标题读取，不加载AI263整份审计/全量diff。固定分支codex/unity6-followup，保留所有已有变更；不提交推送。旧失败/许可/次数保留，任何正式失败只按新ApproveResume续行。每批写前Checkpoint，长任务续租；Complete/Fail/Yield后立即Prepare→一次实际Send→Record回当前CONTROL。OwnerThreadId、ExecutionId随领取回执显式传递。输出 .ai-workspace/outputs/DETACH-B13-20261019/<TaskId>/，预声明 inputs/acceptance/<TaskId>.json，evidence.json与acceptance.json须来自实际证据。报告标明原逻辑卡ASM-01阶段；原建议总预算120的阶段累计不因分线清零，未知轮次/token保持unknown。证据目标1MB/硬上限10MB，超过1MB说明必要性；只保存任务范围diff、一次必要基线，不复制整份13MB工作区diff，不重复全路径SHA。大原生/机器审计必要时无损压缩后引用，不丢失败史；任何未结原件不擅自删除。

CONTROL交接核定：AI264独立预声明8/8及产物10/10复核通过，证据outputs/control/DETACH-B13-20261019/independent-review-264.json。安装源唯一为outputs/DETACH-B13-20261019/AI-000264/install-manifest.json及其window1-manifest.json，先读ENGINE-HANDOFF.md。全部23个命令、2个旧Dialog ID按现有源码/证据核验；不要只测旧误记9项。

> SUPERVISED RESUME_APPROVED at 2026-10-19T14:50:16.5175564+08:00: {"taskId":"AI-000265","runs":1,"usedRuns":0,"approvalId":"call_d55addde997849c98aaefb8136d7896c:0","userDecision":"批准原范围续行 1 次；原问题范围为应用已准备目录顺序补丁并离线核验，再完成43项移动、22项安装、Trollemma/EatWhat及既定验证。原话与范围落盘inputs/派发/DETACH-B13-20261019/AI265_续行1_用户批准.json。","scope":{"paths":[".ai-workspace/outputs/control/DETACH-B13-20261019/AI265-folder-order.patch",".ai-workspace/outputs/DETACH-B13-20261019/AI-000264",".ai-workspace/outputs/DETACH-B13-20261019/AI-000265",".ai-workspace/toolkit","项目整体阅读理解与推进基线_2026-08-14.md","Assets/Editor","Assets/Generated/DataTables","Assets/Scripts","CODEBASE_MAP.md","ProjectSettings/ProjectSettings.asset"],"operations":["apply-reviewed-folder-order-patch","AssetDatabase-CreateFolder","capture-evidence","idempotence-check","install-AI264-22-files","offline-script-compile","PlayerSettings-companyName-productName","read-only-preflight","restore-owned-temporary-test-state","update-affected-documents","verify-native-compile-and-play","window1-43-approved-AssetDatabase-MoveAsset"]}}
## AI-000267 | [CODE] [SC] [DETACH-B13-20261019] AI265探针失败只读诊断

- 序号：267
- 提交时间：2026-10-19T15:13:13.0312837+08:00
- 模块 ID：SC
- 初始状态：QUEUED
- 前置依赖：AI-000264
- 允许越过：False
- 最大尝试次数：1
- 复杂度/轮次预算：S / 40

### 指令

原AI-000265 attempt2的正式同根READONLY_DIAGNOSIS前置。用户已批准探针最小修复及原ENGINE补验1次，原件inputs/派发/DETACH-B13-20261019/AI265_探针修复及补验1次_用户批准.json。此卡只验证诊断与形成可执行最小修复方案，不修改生产源码/资源，不控制Editor，不重跑安装/探针/测试，不领取后继。CONTROL已独立复核的报告：outputs/control/DETACH-B13-20261019/AI265第二次失败复核与补验方案.md和independent-failure-review-265-attempt2.json；仅按报告相关路径读当前代码/场景字段与AI265 attempt2/probe-result.json，避免重审安装包或全仓。
只核两项：1. probe选菜后立即发OpenClueBoard/Close/Start，镜头P0Center→P0Left输入锁，以及连续ESC缺少镜头等待；2. probe.clueBoardLayer与当前Phase0.spaceLayer/baseLayer同fileID1530143313，旧弹出基础层断言和未到达Phase1仍PASS的8/11/12检查不符当前实际架构。查实则将精确源码入口/场景字段、修复边界与可复用离线验证方案写入诊断报告；有差异如实纠正，不迎合控制结论。修复方案只针对现有ShortCycleAutomatedProbe.cs和相关说明：有界事件/状态等待、真实View+基础层保留断言、到达/返回前置条件；不改业务输入锁/状态机/场景，不新增长期工具/专属夹具；保留13检查点及显式输入锁负向验证。
正式根任务AI265、最新失败key ASM01-SHORTCYCLE-PROBE-INPUT-LOCK-SEQUENCE，旧两失败、1/1消费和预算保持。complexity S/40，两项只读认领；不消耗ENGINE补验授权，不把CODE诊断记作原生验证。输出outputs/DETACH-B13-20261019/<TaskId>/，预声明inputs/acceptance/<TaskId>.json；合同checks.diagnosisVerified/repairPlanBounded=true, productionWrites/editorCalls=0。读取CORE→worker/SUPERVISED/HANDOFF及点名P/TK，按标题补读，不通读所有历史。PowerShell7、固定OwnerThreadId、领取一次且仅CLAIMED执行；生产写0仍保留正式结算/证据。Complete/Fail后即刻Prepare→一次实际app Send→Record回绑定CONTROL，禁止自己派后继。报告六要素，费用unknown不估算，证据目标1MB/硬限10MB，无新Git提交推送。

> SUPERVISED POLICY at 2026-10-19T15:13:13.5559551+08:00: {"taskId":"AI-000267","policy":{"resourceKeys":["unity-project","documentation"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"已获批最小探针修复的同根只读诊断前置；保留原AI265和一次补验额度，CODE只读不调用Editor","updatedAt":"2026-10-19T15:13:13.4828336+08:00","supervisionLevel":"full"}}

> SUPERVISED EXTEND at 2026-10-19T15:13:13.6648520+08:00: {"taskIds":["AI-000267"],"reason":"用户已批准探针修复并补验1次；正式同根只读诊断是修复卡必要前置，无新增业务"}
## AI-000268 | [CODE] [SC] [DETACH-B13-20261019] AI265探针时序与真实前置条件最小修复

- 序号：268
- 提交时间：2026-10-19T15:21:07.7746409+08:00
- 模块 ID：SC
- 初始状态：QUEUED
- 前置依赖：AI-000267
- 允许越过：False
- 最大尝试次数：1
- 复杂度/轮次预算：M / 80

### 指令

原AI265探针失败的同根最小REPAIR，承接正式AI267只读诊断和CONTROL独立核验；用户已明确“批准探针修复并补验1次”，原件AI265_探针修复及补验1次_用户批准.json。本卡只做CODE修复，ENGINE那1次补验仍由CONTROL在本卡通过后正式ApproveResume发放；不得触发Editor/Play或消耗其额度。
实施只改Assets/Scripts/Cooking/ShortCycle/ShortCycleAutomatedProbe.cs及现有toolkit/DBG相关说明、必要既有验证入口；不改业务ActionDispatcher、InputGate、StateMachine、PresentationDirector，不改scene/prefab/asset/meta接线和既有字段名字/值，不重做43移动22安装身份设置。基于实际诊断做到：
1. 选菜后和分层ESC之间，等待当前镜头到达目标、停止移动且输入解锁再发后继动作；有界超时明确记失败，不能用固定任意睡眠、强行清锁或降级断言。保留StartCooking后立即Return应被锁拦截的负向验证，不在这一刻提前等解锁；该Return必须明确ErrorCode.Locked，不能把其它失败当输入锁有效。
2. 线索板打开/关闭真实核action成功、Context.phase0_view=ClueBoardShell/RecipeBrowse和当前Phase0基础层保留；不再把场景fileID1530143313基础层当临时层弹出。保留旧序列化字段避免重新接线；13检查点标签准确反映实际含义，不能靠当前层已在顶来假定动作已成功。
3. Phase1/返回未实际到达时，第8、10、11、12项及板稳定/状态保留等后继断言不能误记PASS；第10项不能仅凭当前仍在Phase0而通过；缺引用也不能默认为通过。镜头等待读取真实位置/IsPanning/输入锁，采样固定板世界位置；保留13检查点和失败细节，方便原生一次验证定位。
离线仅当前已安装程序集的相称编译和必要定向时序/断言复核，复用现有工具，不造Unity替身、专属续行入口或新夹具，不重复全套Final/故障隔离/全路径哈希。离线通过不声称Play通过。额外产品逻辑缺陷/接线变动如发现只报告精确差异，不扩大业务。新编译自验证问题沿CODE RecordCodeValidation记录预算，不隐瞒失败。
产物包含最小源码diff、离线编译证据、13检查点对应说明、给原AI265的仅补验交接（禁止再执行install.cs）。CONTROL验收通过后，ENGINE仅核当前已安装基线/编译/Console、ShortCycle现有探针1次、CookingProcess原定运行与自有临时清理；DATA AI266依赖保持。原两失败、已消费许可和根修复预算不清零，人工A+C/窗口2保留，无Git提交推送。
读取CORE/本线worker/SUPERVISED/HANDOFF及相关P/TK，诊断从AI267正式result定位，不通读全量历史。原task root265、problemKey ASM01-SHORTCYCLE-PROBE-INPUT-LOCK-SEQUENCE。复杂度M/80：虽主改1个源码，但包含3类认领修订与离线核验；不重置原ASM累计。输出outputs/DETACH-B13-20261019/<TaskId>/，预声明inputs/acceptance/<TaskId>.json；checks.boundedInputWaits/viewLayerAssertionsAligned/phasePrerequisitesEnforced/retained13CheckpointContract/offlineCompilationPassed=true，productionSceneWrites/businessRuntimeChanges/editorCalls=0。每批源码写前Checkpoint、PowerShell7、Owner/ExecutionId显式，Complete/Fail后立即Prepare→一次实际Send→Record回控制台后停止。报告六要素，unknown成本不估算，证据目标1MB硬上限10MB；只保存必要diff，不复制全工作区或重复前置包。

> SUPERVISED POLICY at 2026-10-19T15:21:08.2219125+08:00: {"taskId":"AI-000268","policy":{"resourceKeys":["unity-project","documentation"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"用户已批准最小探针修复和仅补验；诊断267已独立4/4合同及6项review通过，无新增人工门","updatedAt":"2026-10-19T15:21:08.1672641+08:00","supervisionLevel":"full"}}

> SUPERVISED EXTEND at 2026-10-19T15:21:08.3141898+08:00: {"taskIds":["AI-000268"],"reason":"同一人类授权的同根CODE最小修复，保留AI265原失败和补验1次上限"}

> SUPERVISED RESUME_APPROVED at 2026-10-19T15:34:08.9314251+08:00: {"approvalId":"call_055b3d501257487fbbc77d6aa3e8f851:0","scope":{"paths":[".ai-workspace/outputs/DETACH-B13-20261019/AI-000265",".ai-workspace/toolkit/DBG.md","项目整体阅读理解与推进基线_2026-08-14.md","Assets/Scenes/Cooking/ShortCycle_P0P1.unity","Assets/Scenes/CookingProcess.unity","Assets/Scripts/Cooking/ShortCycle/ShortCycleAutomatedProbe.cs","CODEBASE_MAP.md","ProjectSettings/EditorBuildSettings.asset","ProjectSettings/ProjectSettings.asset"],"operations":["capture-evidence","CookingProcess-existing-runtime-validation-once","idempotence-check","read-only-installed-baseline-and-editor-preflight","restore-owned-temporary-test-state","ShortCycle-probe-once","update-affected-documents","verify-native-compile-and-console"]},"runs":1,"taskId":"AI-000265","usedRuns":0,"userDecision":"批准探针修复并补验 1 次。AI268修复经CONTROL独立合同8/8、产物10/10通过；原AI265仅核现已安装基线/编译Console、ShortCycle探针1次、CookingProcess原定运行1次及自有临时状态恢复，不重做43移动/22安装/身份设置。原话见inputs/派发/DETACH-B13-20261019/AI265_探针修复及补验1次_用户批准.json。"}

> Verification resolved for AI-000265 at 2026-10-19T15:52:59.8553742+08:00: decision=VERIFIED; summary=CONTROL针对性复核：用户批准的探针修复及单次补验已完成，原attempt2 TEST_OBSERVATION已由attempt3实际13/13通过消解；不代表窗口②或A+C人工批准。; evidence=依据protocol/WORKFLOW_FULL.md测试问题复核条款。原合同10/10与CONTROL独立13/13通过；原始CLI探针、CookingProcess实际Play约29秒、增量Console零错误和临时状态清理证据见outputs/control/DETACH-B13-20261019/independent-contract-265.json及independent-review-265.json，原件outputs/DETACH-B13-20261019/AI-000265/attempt3/。原失败、issueHistory和许可保持，人工窗口②/A+C仍未批准。
## AI-000269 | [CODE] [SC] D-T1a 食谱本数据绑定与最小解锁：源码及离线交付

- 序号：269
- 提交时间：2026-10-19T15:59:27.4303403+08:00
- 模块 ID：SC
- 初始状态：QUEUED
- 前置依赖：AI-000266
- 允许越过：False
- 最大尝试次数：1
- 复杂度/轮次预算：L / 120

### 指令

批次DETACH-B13-20261019，D-T1a源码与离线交付阶段；用户已明确下发整个派发包并启动监管，原卡cards/D-T1a.md标注用户2026-10-15已通过。complexity L/预算120，FEATURE，非原AI265续行。前置AI266成功且CONTROL独立验收；本次先登记依赖等待，不在前置完成前执行。USER-U1权威状态DONE。固定CODE，不新开chat、不执行Unity/导表/生产发布。

读取CORE、workers/CODE、项目两入口页首及shortcycle/data相关模块、原卡cards/D-T1a.md全文、Schema稳定入口的§四/§八与T1/T2/T4/T6/T13相关条目、总档v1.5.4相关G18/G65/G75/G78/框架Binder边界和工具三问。先核源码及已有TK-LAY/MouseInteractive/LocService/数据接口，避免同名重复实现。受影响路径核定后在报告写精确清单。

落实原卡的RecipeBookBinder、只读RecipeView、Catalog只读枚举/TryGet接口、最小UnlockManager、目录卡/食材清单/便签/当前菜签内容绑定、Session生命周期及ClueBoard数据复用；保留FSM唯一订阅、Binder只填内容、不启停容器、不改Set显隐、不改_TUNE Transform。未解锁点击不进①，PlaceholderRule按原卡最小内联不提升长期工具。无存档流程，本轮profile解锁集合恒空；不要抢做D-T1b/SAVE/P2。

程序集/实际路径说明：AI263 dependency-audit及AI265安装后事实显示Catalog实际为Assets/Scripts/DataBase/CookingData/CK01GeneratedDataCatalog.cs，所属Generated.DataTables，通过asmref归属；ShortCycle依赖Generated，不能反向引用。原卡把RecipeView画在ShortCycle/Content只是逻辑路径，需要在既有Generated数据层定义纯只读DTO供ShortCycle引用（同名公共面保持），不得为照搬路径引入Generated→ShortCycle回边或第二数据源。实际类为CatalogCard.cs、ShortCycleSessionManager.cs、Content/ShortCycleClueBoardBinder.cs；先查原实现再最小修改，不平行建同职能类。源码相关新增/修改限Assets/Scripts/Cooking/ShortCycle、Assets/Scripts/DataBase/CookingData及必要既有DevTools/ShortCycle离线runner/夹具、项目定位/TK复用文档、本任务outputs。不改Unity场景/prefab/asset/meta、生产xlsx/CSV/Generated资产/字体、程序集引用方向或其它业务；禁止恢复无关dirty。

CODE需完成可运行源码和相称离线runner：真实CSV/fixture核枚举数量与排序、三菜切换所用key/slots/tags映射、解锁默认及拒绝选择、生命周期Clear/重绑、只读Catalog唯一数据源与无反向程序集依赖；扩充现有ShortCycleAutomatedProbe的原卡3断言并保留原13项，不能再弱化实际阶段/镜头等待/基础层检查。新断言真实Play由后续ENGINE验收，不把离线通过写成Play通过。按既有Verify-All Custom受影响runner执行一次最终验证；内部失败按RecordCodeValidation登记，正式Fail后不自续行。无必要不得扩大为全仓/全路径hash/快照。

已预拆的后继ENGINE阶段为同包D-T2（cards/D-T2.md）：依赖D-T1a/b交付并经窗口②明确批准后，由固定ENGINE_CLI完成接线、原13+新增探针真实验证和Console/布局保护；节点A+C人工验收保持，CODE不代过。交付列精确组件接线/序列化字段清单和实际场景路径Assets/Scenes/Cooking/ShortCycle_P0P1.unity，供窗口②审查。本CODE阶段完成不等于D-T1a整卡人工/运行验收完成。缺图/key只报告并登记DATA/ART后继，不擅自导入或改生产值。

输出.ai-workspace/outputs/DETACH-B13-20261019/AI-000269，含简短REPORT.md、实际变更清单、离线证据evidence.json、acceptance.json和ENGINE-HANDOFF.json。合同inputs/acceptance/AI-000269.json，checks须有可独立追溯产物和运行证据，不只写true。任务包目标1MB、硬10MB，完整文本保留。必要文档随实际更新且不改全项目复核日期；无新增长期工具则只登记复用。每批生产源码写前SupervisionCheckpoint；失效/未知写入/失败按正式规则停。结算即时Prepare→一次真实send绑定CONTROL→Record，禁止执行器领取后继；原历史失败/许可/计数不清零。

> SUPERVISED POLICY at 2026-10-19T15:59:42.4724710+08:00: {"taskId":"AI-000269","policy":{"resourceKeys":["unity-project","documentation","data-batch"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"用户已下发同一批次D-T1a；仅提前登记依赖等待，AI266完成及CONTROL独立验收后派发。CODE源码离线交付，不代窗口②/D-T2/A+C人工与原生验收；与DATA同unity-project/data-batch互斥。","updatedAt":"2026-10-19T15:59:42.3936491+08:00","supervisionLevel":"full"}}

> SUPERVISED EXTEND at 2026-10-19T15:59:55.4617625+08:00: {"taskIds":["AI-000269"],"reason":"同一用户已授权DETACH-B13批次的下一卡D-T1a，提前登记依赖等待以避免ASM子链完成误收整个批次；仅AI266独立验收后派发。窗口②及A+C保持，未扩大业务范围。"}

> SUPERVISED RESUME_APPROVED at 2026-10-19T16:40:42.3658566+08:00: {"usedRuns":0,"userDecision":"批准原范围续行 1 次；固定DATA通过正常沙箱权限升级审查运行同一隔离CurrentCsvValidation，不发布、不修改ACL/认证/许可配置或生产数据。来源inputs/派发/DETACH-B13-20261019/AI266_续行1_用户批准.json。","approvalId":"call_336e18fc5544443490ce837d82812eaf:0","taskId":"AI-000266","scope":{"paths":[".ai-workspace/outputs/DETACH-B13-20261019/AI-000266","DevTools/DataTables/.batch-workspace"],"operations":["capture-evidence","cleanup-owned-process-and-release-lock","CurrentCsvValidation-isolated-existing-pipeline-once","idempotence-check","read-only-preflight"]},"runs":1}
## AI-000270 | [CODE] [SC] [DETACH-B13-20261019] AI266报告目录与分模式续行只读诊断

- 序号：270
- 提交时间：2026-10-19T17:00:06.8094530+08:00
- 模块 ID：SC
- 初始状态：QUEUED
- 前置依赖：AI-000265
- 允许越过：False
- 最大尝试次数：1
- 复杂度/轮次预算：S / 40

### 指令

原AI-000266 attempt2同根READONLY_DIAGNOSIS。用户已直接批准本次报告目录最小修复及原DATA续行1次，并明确长期MANUAL1/AUTOMATIC3/SUPERVISED5；原话inputs/续行许可分模式_用户长期授权_20261019.json。此卡只读核实并交付可执行修复方案，不实施源码/文档规则修改，不调用Unity/导表/PlanOnly全项目复制/发布，不消费原DATA续行。允许写自己的任务报告与正式队列结算。
两项认领：(1) 核outputs/control/DETACH-B13-20261019/AI266第二次失败修复与续行方案.md及independent-failure-review-266-attempt2.json、AI266-report-guard-reproduction.json；对Run-DataPipeline.ps1/DataPipeline.Common.ps1/Test-BatchImport.ps1提最小方案：保留runtime默认，支持严格当前TaskId所属outputs/<批次>/<TaskId>及子目录；parent/runId共用边界；拒绝其它任务、前缀伪装、越界/reparse，保持唯一run/nonce/碰撞合同。(2) 只读梳理现有approved-resume.ps1/repair-budget.ps1/queue.ps1/supervised.ps1和既有模式回归：将长期1/3/5授权落地到真实领取/修复预算/失败停止、控制调度、AUDIT及文档。首次执行不算重试；当前AI266已消费1次，不能抹去，明确本次再1次与默认5不重复叠加；同轮修复+对应根续行只计一个周期；错误key/新修复卡/线程/模式变化不能清零。同一ApproveResume入口，区分人类单次/长期策略来源，不编造新批准；到上限才人工追加。SCHEDULED不扩大；平台真实拒绝、未知写入、用户stop/exit、业务人工门及120/240保护仍有效。旧根修复limit2和CODE自验证limit2不能意外抵消新政策；说明内部验证和根重试的合理边界，不做无限重试。
产出精确修改清单、计数语义、最小API兼容方案和既有离线用例矩阵，建议一次REPAIR内完成并按文件数给L/120预算；如两目标需要分开明确依赖。不新增一次性入口/专用夹具，不触发旧任务/旧session。输入按CORE→CODE worker/SUPERVISED/HANDOFF及点名节读取，不全仓扫描或通读历史。当前诊断complexity S/40，两项只读认领，MaxAttempts1过渡。原task266/key=data_report_directory_scope_conflict，原两次失败、旧1/1许可、AI265已验收与AI269依赖等待保持；窗口②/A+C不变。输出outputs/DETACH-B13-20261019/<TaskId>/，合同inputs/acceptance/<TaskId>.json：diagnosisVerified/repairPlanBounded=true, productionWrites/editorCalls=0。Complete/Fail立即Prepare→一次实际app send→Record回绑定CONTROL，勿自行派后继；unknown用量不估算，报告目标1MB硬限10MB，无提交推送。

> SUPERVISED POLICY at 2026-10-19T17:00:26.0718619+08:00: {"taskId":"AI-000270","policy":{"resourceKeys":["workflow","documentation","data-batch","unity-project"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"用户批准报告路径修复及长期模式重试机制；同根只读诊断前置，禁止CODE运行Unity/导表","updatedAt":"2026-10-19T17:00:25.9906154+08:00","supervisionLevel":"full"}}

> SUPERVISED EXTEND at 2026-10-19T17:00:26.3638177+08:00: {"reason":"当前用户批准原AI266修复与续行并调整长期1/3/5，加入同根必要只读诊断","taskIds":["AI-000270"]}
## AI-000271 | [CODE] [SC] [DETACH-B13-20261019] AI266报告路径及分模式1-3-5续行机制修复

- 序号：271
- 提交时间：2026-10-19T17:19:36.3414181+08:00
- 模块 ID：SC
- 初始状态：QUEUED
- 前置依赖：AI-000270
- 允许越过：False
- 最大尝试次数：1
- 复杂度/轮次预算：L / 120

### 指令

本卡为原AI-000266同根REPAIR，承接AI270只读诊断和CONTROL独立4项合同/8项复核；依据用户call_37410ba043344e9f81b4ce3ef3472a18:0现有批准，不再索取同意。权威原话inputs/续行许可分模式_用户长期授权_20261019.json。一次L/120目标：报告目录与MANUAL1/AUTOMATIC3/SUPERVISED5真实续行机制一致落地，离线验证后交CONTROL；原DATA attempt3仍须CONTROL正式ApproveResume并固定DATA执行，CODE不触发Unity或导表。
实施基线是outputs/DETACH-B13-20261019/AI-000270/REPAIR_PLAN.md（含末尾过渡绑定与cwd补充），只按目标节补读；32项矩阵test-matrix.json是待执行用例，不是通过证据。CONTROL复核outputs/control/DETACH-B13-20261019/independent-review-270.json。诊断的字段名/API可选参数是建议，可简化为兼容现有实现的最小方案，但不能省真实计数/领取/失败/控制调度链。不要复制一套队列或新增长期runner。
允许文件：DevTools/DataTables/Run-DataPipeline.ps1、DataPipeline.Common.ps1、Test-BatchImport.ps1、DATA_PIPELINE.md；.ai-workspace/approved-resume.ps1、repair-budget.ps1、queue.ps1、supervised.ps1，以及control-administration.ps1仅确有影响时最小适配；既有DevTools/Workflow/Test-ApprovedResume.ps1、Test-Pre5c.ps1和.ai-workspace/tests/Test-RepairBudget.ps1、Test-WorkflowModes.ps1、Test-SupervisedMode.ps1。文档只同步AGENTS/CORE/WORKFLOW/SUPERVISED/CONTROL/MODES及相关protocol、workers CODE/DATA、TK-WF/DAT，按实际影响补CODEBASE_MAP及项目基线对应条款，CLAUDE仅核指针。用户原话原件和历史不改写；本任务输出与正式queue/usage/handoff可写，runtime仅正式API。保留当前未提交改动；不切分支、提交/推送，不修改Assets/ProjectSettings/DataTables生产内容。
路径：保留runtime默认/合法子目录，新增严格当前TaskId的outputs/<单一批次>/<精确AI-xxxxxx>及子目录；相对路径按RepositoryRoot解析。parent/runRoot同一边界，TaskId不清洗伪造，拒绝其它任务、前缀、dot/ADS/Windows别名、外部/控制区与reparse祖先；纯解析不创建，创建前核重定向/碰撞。原唯一run/nonce及严格完成日志合同不变，不能扩大整个outputs或将runtime加入DATA Scope。
政策：首次根执行不计重试，诊断0；同周期CODE修复和对应原根续行只扣一次，第一次实际claim消费，修复失败也消耗。root家族跨key/卡/线程/mode保留，旧claims/repair/recovery只按确证关系去重，不猜配或退款。AI266当前已消费1；本AI271修复与原attempt3配同cycle2，合计2/默认5，余3，不算成7或双扣成3。此卡在旧字段下领取的过渡绑定，须在原根ApproveResume以正式repairContext、真实人类批准和PairedRepairTaskId原子登记，不让运行中卡重Poll、不手改runtime、不重置历史。
仍用唯一ApproveResume入口；MANUAL每次人批Runs1（2拒绝）；AUTOMATIC默认3、SUPERVISED默认5，额度内CONTROL可引用已核长期政策自主发下一周期，明确HUMAN/POLICY来源，不编造新用户批准；默认耗尽后才人类追加，稳定ID幂等、同批预留/领取不超额、不叠加。SCHEDULED和旧完成/停止session不扩大。grant仍绑定mode/session/route/owner/fingerprint/失败史/expectedAttempt/精确各线scope；不能把CODE修复权限合并给DATA。原代码maxAttempts/failureStopLimit、root limit2、监管3/6不能提前拦住合法政策周期；未获合法周期依然停止。CODE局部验证2次上限只控制同周期内部自验证，耗尽正式停止；原记录保留，新有效根周期可继续，禁止换key清零或无限内部重试。
失败先正式结算/立即回传，结构化给CONTROL“范围内有余量可修复续行”或“需人类追加/硬停”；不能仅因为旧WAITING_USER就把可续行当人工停点。控制协议/WATCH消费条款必须可执行，AUTOMATIC返回正常同任务后继而非新任务；执行器不能擅自绕过CONTROL再启动。人类业务门、用户stop/exit、GLOBAL/未知写入、真实平台拒绝、120/240回合与工具安全边界不放宽。正常提权审批仍真实走平台。实际scope及配对阶段必须分开核，额度不是新业务授权。
验证只扩既有测试、临时隔离state，按32项矩阵落实真实queue claim/Fail/Approve/Dispatch证据，包括manual1/2、auto前三/第四、supervised前五/第六、额外批准、重复/并发/配对/历史迁移、模式变动和硬停。每条用例对应证据，不只断言常量。运行一次受影响Custom集合，失败修正后只重跑必要项，不全量Final/停用字体工具/全仓哈希。禁止Unity/PlanOnly复制/导表/发布。真实业务原AI266仅在本卡独立通过后执行，不拿合成测试冒充。
合同inputs/acceptance/<TaskId>.json：reportPathOwnershipVerified/runtimeReportCompatibility/modeRetryLimitsVerified/rootCycleAccountingVerified/approvalIdempotenceVerified/lifecycleAndStopsVerified/legacyCompatibilityVerified/offlineRegressionPassed/documentationAligned=true；editorCalls/productionDataWrites=0。证据须附实际改变文件清单、既有runner真实回执、AI266过渡配对可用的精确正式命令/参数及预期计数，不能预写成功。范围复杂度L/120，累计120/240不清零；每批源码写前SupervisionCheckpoint，实际自验证问题按正式RecordCodeValidation，核原类调用关系后最小改动。Complete/Fail即时Prepare→一次实际app Send→Record到绑定CONTROL并停止，不自派后继。费用unknown不估，完整文本报告目标1MB硬10MB；不复制大源码/隔离工程包。

> SUPERVISED POLICY at 2026-10-19T17:19:59.8099684+08:00: {"taskId":"AI-000271","policy":{"resourceKeys":["workflow","documentation","data-batch","unity-project"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"用户已批准报告路径和长期1/3/5机制修复；AI270独立验收4合同/8复核通过，CODE仅源码及隔离离线测试","updatedAt":"2026-10-19T17:19:59.7326054+08:00","supervisionLevel":"full"}}

> SUPERVISED EXTEND at 2026-10-19T17:20:00.0762558+08:00: {"reason":"原AI266同根修复承接AI270，使用已有明确人类批准，原DATA本次1次未发放","taskIds":["AI-000271"]}

> SUPERVISED RESUME_APPROVED at 2026-10-19T18:08:04.9886639+08:00: {"reserved":0,"taskId":"AI-000271","sourceApprovalId":"call_37410ba043344e9f81b4ce3ef3472a18:0","authorizationSource":"POLICY","remaining":2,"grantId":"2f1c7243a18f4a3f94af0ae396851a59","verification":"CONTROL independent-failure-review-271.json 8/8 confirms local validation stop, original root two consumed cycles of five and three remaining; fresh app CODE idle/completed cursor05743119-ec27-4c2c-af67-4f452fba193b:156. User standing POLICY source exact. Same CODE task scoped offline repair only; no DATA/Unity. Candidate cycle reservation accounting issue is bounded and must be fixed; formal claim branch imports approved-resume before scope check.","used":3,"cycles":["AI266:CODE271:retry3"],"scope":{"paths":[".ai-workspace/approved-resume.ps1",".ai-workspace/CONTROL_CHAT.md",".ai-workspace/CORE.md",".ai-workspace/MODES.md",".ai-workspace/outputs/DETACH-B13-20261019/AI-000271",".ai-workspace/protocol/CONTROL_CHAT_FULL.md",".ai-workspace/protocol/SUPERVISED_FULL.md",".ai-workspace/protocol/WORKFLOW_FULL.md",".ai-workspace/queue.ps1",".ai-workspace/repair-budget.ps1",".ai-workspace/SUPERVISED.md",".ai-workspace/supervised.ps1",".ai-workspace/tests/Test-RepairBudget.ps1",".ai-workspace/tests/Test-SupervisedMode.ps1",".ai-workspace/tests/Test-WorkflowModes.ps1",".ai-workspace/toolkit/DAT.md",".ai-workspace/toolkit/WF.md",".ai-workspace/workers/CODE.md",".ai-workspace/workers/DATA.md",".ai-workspace/WORKFLOW.md","项目整体阅读理解与推进基线_2026-08-14.md","AGENTS.md","CODEBASE_MAP.md","DevTools/DataTables/DATA_PIPELINE.md","DevTools/DataTables/DataPipeline.Common.ps1","DevTools/DataTables/Run-DataPipeline.ps1","DevTools/DataTables/Test-BatchImport.ps1","DevTools/Workflow/Test-ApprovedResume.ps1","DevTools/Workflow/Test-Pre5c.ps1"],"operations":["capture-evidence","documentation-update","idempotence-check","isolated-offline-regression","source-repair"]},"rootTaskId":"AI-000266"}

> SUPERVISED RESUME_APPROVED at 2026-10-19T18:35:53.2017022+08:00: {"reserved":0,"sourceApprovalId":"call_37410ba043344e9f81b4ce3ef3472a18:0","rootTaskId":"AI-000266","verification":"CONTROL independently accepted AI271 attempt2 execution 83da88badb7e43798d4bd5c5d56f586e: 11 contracts, 32 cases/126 original assertion labels, six regression receipts, 22 source hashes, exact AI266 attempt3 pure report path. Review outputs/control/DETACH-B13-20261019/independent-review-271-attempt2.json. Fresh app wait_threads DATA idle/completed cursor 9b7e6353-95ca-49e0-a544-01da04fb9d4b:7; live SUPERVISED ACTIVE beefe968cb5e48e0bea0b8b47260abd9 no stop/exit. Latest successful cycle AI266:CODE271:retry3 pairing remains used3/5 remaining2; original read-only scope and manual gates unchanged.","cycles":["AI266:CODE271:retry3"],"taskId":"AI-000266","authorizationSource":"POLICY","scope":{"paths":[".ai-workspace/outputs/DETACH-B13-20261019/AI-000266","DevTools/DataTables/.batch-workspace"],"operations":["capture-evidence","cleanup-owned-process-and-release-lock","CurrentCsvValidation-isolated-existing-pipeline-once","idempotence-check","read-only-preflight"]},"remaining":2,"used":3,"grantId":"e4bc89c8a0de4884a3429fdf07b044cc"}
## AI-000272 | [CODE] [SC] D-T1b 步骤详情数据驱动与收藏内存态（源码/离线）

- 序号：272
- 提交时间：2026-10-19T18:50:18.7088434+08:00
- 模块 ID：SC
- 初始状态：QUEUED
- 前置依赖：AI-000269
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：M / 80

### 指令

批次DETACH-B13-20261019，用户已明确下发批次1+3并启动监管；原卡cards/D-T1b.md全文为依据。本卡D-T1b源码与离线阶段，FEATURE，固定CODE，不新chat；按用户明确complexity M/80保留，实际规模与预算差异留账。依赖AI269源码阶段成功且CONTROL独立验收后才能派发；预先登记只依赖等待。真实场景/Play及新增探针由原包D-T2窗口②处理，不把CODE阶段完成当整卡人工验收完成。

领取后只读CORE、workers/CODE、项目两入口页首及ShortCycle/data相关模块；原卡D-T1b、AI269最新REPORT与ENGINE-HANDOFF/实际变更清单；Schema稳定入口§四、T1/T3/T4/T工具相关节及现行动作23项。必须以AI269实际源码为起点，核RecipeView实际位置、Catalog只读接口、StepDetailModal及收藏星标组件、现有toggle_favorite命令/行为，避免平行系统。

原卡范围：在AI269只读RecipeView/既有Catalog数据层扩充按T3 first_step_id→next链序的steps，含display_text_key与standard_ingredients/tools图标；复用现有数据及校验器链序清单，不写生产表值、不建第二数据源。⑤步骤行无标题无编号、≤10，复用TK-LAY Vertical；内容绑定沿既有架构，未接场景字段写入精确ENGINE-HANDOFF。最小内存PlayerProfileManager及PlayerProfile收藏/解锁集合，ToggleFavorite、GetSnapshot/ApplySnapshot；无落盘、无加密，ISaveBoundary只是SAVE-01后继边界。与AI269最小UnlockManager对接保持T1默认解锁并集；不得抢实现SAVE-01/D-T2。收藏切换同步星标，三菜步骤与三次切换离线验证。若现有23动作全集没有可用收藏动作，按原卡停止并报设计端，禁止新加动作绕过。Profile只提供能力，FSM/Director仍为唯一决策/订阅边界。

允许源码Assets/Scripts/Cooking/ShortCycle、Assets/Scripts/DataBase/CookingData及必要既有DevTools/ShortCycle离线runner/夹具、相关项目/TK文档和本任务outputs。RecipeView保持在Generated数据层可用的纯DTO位置，不引入Generated→ShortCycle回边。不改场景/prefab/asset/meta、_TUNE Transform/显隐、生产xlsx/CSV/Generated资产/字体、ProjectSettings/程序集方向，不恢复无关dirty、不执行Unity/导表/Publish/提交推送。

扩充现有探针新增⑤页行数与收藏一致性2断言，保留AI269全部既有13+新增3项，不弱化探针前提/等待/基础层；真实Play留D-T2。相称既有离线runner验证三菜链序/key/图标映射、收藏×3、快照隔离与Apply/clear、唯一数据源、无磁盘持久化/反向程序集、全探针保留及新增；通过现有Verify-All Custom一次最终验证，不为每小步建任务/入口或全仓hash。

输出.ai-workspace/outputs/DETACH-B13-20261019/AI-000272：简短REPORT.md、实际变更清单、evidence.json/acceptance.json、ENGINE-HANDOFF.json及SAVE-HANDOFF.json（接口/字段及未实施持久化边界）。合同inputs/acceptance/AI-000272.json，检查须对应真实证据，不只写true。项目定位/TK复用随实际更新，不提升全项目核验日期。完整文本目标1MB硬10MB，超1MB说明必要性。每批源码写前SupervisionCheckpoint；内部失败按RecordCodeValidation，正式失败先结算立即Prepare→一次实际Send→Record回CONTROL，执行器不自行续行或领后继。窗口②与A+C保持，模式与用户停止优先，续行按既有1/3/5正式入口，失败历史不清零。

> SUPERVISED POLICY at 2026-10-19T18:50:19.1200502+08:00: {"policy":{"resourceKeys":["unity-project","documentation","data-batch"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"Same user-authorized DETACH batch D-T1b source/offline stage; dependsOn AI269 and independent CONTROL acceptance. Original M80 retained. D-T2 windowII and A+C remain separate manual gates.","updatedAt":"2026-10-19T18:50:19.0513405+08:00","supervisionLevel":"full"},"taskId":"AI-000272"}

> SUPERVISED EXTEND at 2026-10-19T18:50:19.2160639+08:00: {"taskIds":["AI-000272"],"reason":"User-approved batch1+3 next D-T1b card, only dependency waiting behind AI269; prevent premature subchain closeout. No new business scope or human-gate waiver."}
## AI-000273 | [CODE] [SC] P2L-01 加工匹配、分区限制与步骤推进（纯逻辑）

- 序号：273
- 提交时间：2026-10-19T19:24:35.6951314+08:00
- 模块 ID：SC
- 初始状态：QUEUED
- 前置依赖：AI-000266, AI-000243, AI-000244, AI-000272
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：M / 80

### 指令

原批次DETACH-B13-20261019 P2L-01源码与纯逻辑阶段，用户已下发批次1+3；原卡cards/P2L-01.md全文为依据。固定CODE、FEATURE、用户原定complexity M/80保持，真实规模/预算差异留账。正式依赖AI266（ASM DATA已独立通过）、AI243/244（CODE-R01源码/Editor已VERIFIED）、AI272（复用D-T1b步骤链且保持批次顺序）。只预先登记等待，须所有依赖成功且CONTROL独立验收才派发。

读取CORE/CODE worker、项目两入口页首和shortcycle/data模块、原卡P2L-01、Schema稳定入口当前v2.4.1§四/§八及T1/T3/T7/T8/T14相关条目、总档v1.5.4相关G40/G74/G81～85和工具三问。先核AI272交付/真实源码、现有Content/ShortCycleCarrierResolver、DataBase/CookingData/CK01DataModels.cs的全局RecipeWhitelist/AreaConstraint/UnknownProductResolver和Catalog只读API；已有AreaConstraint纯模型不能同名重复新建或为照搬逻辑路径破坏S-02调用。具体实现按最小兼容扩展和明确命名，原ASM引用方向保持。

实现原卡：纯C# ProcessingMatcher按输入多重集、工具supported_actions、可用carrier、recipe whitelist顺序匹配；多候选取白名单首条，不能新增priority或读取preset_tools；当前分区无命中即拒绝，不遍历其它分区（X5/G83）。加工无命中走既有Catalog解析的unknown_product_item，禁止硬编码替换实际配置ID；不明物体不触发步骤完成。分区allowed_actions/allowed_carriers检查返回明确拒绝码，复用既有AreaConstraint语义；StepProgress保留none/dynamic/permanent、备菜区变更触发链序计算、动态回退、折叠永久完成和队尾FinalDishReady事件。Core零UnityEngine类型/命名空间、无场景/表现，输入纯只读快照，不直接读取ScriptableObject或Unity现场。Catalog适配侧扩充/复用TryGetProcessingRecords(ids)/TryGetStepChain，复用AI272链序，不另建数据源或重复算法。

允许源码Assets/Scripts/Cooking/ShortCycle、Assets/Scripts/DataBase/CookingData、必要既有DevTools/ShortCycle离线runner/夹具、相关项目/TK文档、本任务outputs。优先扩充既有ShortCycle测试入口，不因原卡逻辑Tests路径新建平行长期工具；本卡ProcessingMatcher只留Core，TK-DAT-08提升留未授权批次2。不改场景/prefab/asset/meta/Transform、生产xlsx/CSV/Generated资产/字体、程序集依赖方向，不Unity/Play/导表/Publish/提交推送，不恢复无关dirty。

真实现有CSV/demo与纯夹具对照：三菜标准与替代路径、多重集重复数、白名单外回退、白名单首条、分区拒绝/不跨区、工具/载具限制、unknown不触发、链序与校验器一致、折叠永久及动态回退、FinalDishReady只在满足队尾条件时产生。覆盖Schema§八六分支，每项给可追溯断言；保留既有S-02和D-T1a/b测试，原18项探针不弱化，本卡无新Play声明。用既有Verify-All Custom相称一次最终验证，内部失败正式RecordCodeValidation，正式失败先停并回传CONTROL。

配表只读回填两项：T7是否含tool_hand；demo三菜白名单记录是否含stp_输入。逐表/字段/行证据写DATA-FOLLOWUPS.json并指向USER-U5，区分observed/unknown；不擅增行改值、不声称Schema v2.5或U5已经批准。未完成U5不被本纯逻辑验收代过；发现实现规则与已确认Schema冲突时保全并报CONTROL。

输出.ai-workspace/outputs/DETACH-B13-20261019/AI-000273：简短REPORT.md、changed-files.json、evidence.json/acceptance.json、真实离线日志、DATA-FOLLOWUPS.json、P2S-HANDOFF.json（接口/事件/适配边界及未做场景内容）。8项合同inputs/acceptance/AI-000273.json。更新实际定位/工具复用与风险，不能提高全项目复核日期或把逻辑阶段当P2S场景完成。包目标1MB硬10MB，超1MB说明必要性。每批源码写前Checkpoint；结算立即Prepare→一次真实Send→Record，执行器不自行续行/领取后继；窗口②和A+C、U2～U6等人工门保持。

> SUPERVISED POLICY at 2026-10-19T19:24:36.1284337+08:00: {"taskId":"AI-000273","policy":{"resourceKeys":["unity-project","documentation","data-batch"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"Original batch P2L-01 pure logic only, explicit M80; CODE-R01 and ASM accepted; reuse pending D-T1b chain. No USER-U5/windowII/A+C waiver.","updatedAt":"2026-10-19T19:24:36.0483857+08:00","supervisionLevel":"full"}}

> SUPERVISED EXTEND at 2026-10-19T19:24:36.2675996+08:00: {"reason":"Original authorized batch successor only dependency waiting; preserve order and avoid premature subchain completion. No dispatch until independent AI272 acceptance.","taskIds":["AI-000273"]}
## AI-000274 | [CODE] [SC] SEC-01 通用认证加密与共享存档编解码接口（源码/离线）

- 序号：274
- 提交时间：2026-10-19T19:55:34.9752330+08:00
- 模块 ID：SC
- 初始状态：QUEUED
- 前置依赖：AI-000266, AI-000272, AI-000273
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：M / 80

### 指令

原批次DETACH-B13-20261019 SEC-01源码/离线阶段，用户已下发批次1+3。以cards/SEC-01.md全文及SAVE-01.md接口要求为依据；固定CODE、FEATURE、module SC，用户原卡明确complexity M/80保持，虽新TK-SEC-01通常按GOV-05需L，记录用户原卡覆盖与真实规模，不隐藏超预算。正式dependsOn AI266/AI272/AI273（ASM落地、profile快照与既定单线顺序）；预先登记等待，CONTROL独立验收前不领取。

先读CORE/CODE worker、项目入口页首和tools/data/shortcycle相关索引、G76/G77、TK三问、既有Toolkit.asmdef及实际JSON/加密源码。已初查ISaveCodec/AesGcmCipher/IKeyProvider/EncryptedJsonCodec未实现，旧Assets/Scripts/DataBase/DataEncryptor.cs使用JsonUtility，不能把旧数据加密代码当已满足G77，也不能重写现有生产读取。本卡先在Tools/Persistence确定唯一通用ISaveCodec接口（以可供SAVE-01现有JSON文本/字节编解码替换为边界），交付接口源码、EncryptedJsonCodec与SAVE-HANDOFF，避免为了加密引入JsonElement/System.Text.Json或新的序列化依赖。PlainJsonCodec/SaveEnvelope/实际存储及SaveBoundary宿主在SAVE-01实现；如既有JSON依赖与原卡不符，写明真实事实与最小兼容接口，不凭卡中[核]直接加包。

完成原卡安全能力：Tools/Security的IKeyProvider、拆分常量运行时组合的ObfuscatedStaticKeyProvider、认证加解密、版本化EncryptedBlob；Toolkit不引用ShortCycle/Generated/上游业务。优先AES-256-GCM，核Unity6000.3.24f1实际reference和目标runtime能力，不将PS7/.NET主机成功冒充Mono/IL2CPP可用。不可用时按原卡明确允许的AES-CBC+HMAC-SHA256回退实现，独立子密钥、随机IV、先认证再解密、恒定时间MAC比较；不得无认证降级。GCM每次安全随机12B nonce/16Btag；CBC回退按真实IV/MAC长度使用单独明确版本，不把16BIV/32BMAC挤入GCM12/16格式，不静默误读版本。容器严格检查magic/version/长度与截断，认证覆盖必要头部，异常不泄露部分明文。无第三方密码库、反射、动态加载；密钥不进CSV/JSON/场景，禁止输出实际密钥。客户端内置密钥仅提高提取门槛，不声称DRM。

允许Assets/Scripts/Tools/Security、Tools/Persistence/ISaveCodec与EncryptedJsonCodec、必要既有DevTools离线统一Verify-All Custom接入及明确所属Security夹具/runner、相关工具/工程/项目文档和本任务outputs；不修改Toolkit.asmdef引用方向、Unity场景/prefab/asset/meta、Packages/ProjectSettings、生产CSV/xlsx/Generated/字体、旧DataEncryptor生产链或实际用户存档。只源码/隔离临时测试，不Unity/Play/Build/导表/Publish/提交推送。不写persistentDataPath，不接入实际SaveBoundary，不提前做SEC-02发布切换。

相称离线验证：空/非空/UTF8往返；错误key、单字节篡改（头/nonce或IV/tag或MAC/cipher）、截断/错误magic/version拒绝；安全随机nonce/IV多次不复用；容器确定字段长度与跨平台顺序；共享ISaveCodec替换；无上游依赖/无密钥资产。用实际Unity引用离线编译并记录目标/引用，主机加密执行注明主机与算法；有条件核官方原始文档，不把参考API存在当运行支持。IL2CPP Windows x64 Development Build单测场景留ENGINE-HANDOFF为待真实Build验证，CODE阶段不能代签；窗口②与A+C保持。不得新增单次重试入口/平行Final，Final重复按现有规则说明原因，稳定错误key及正式失败回传保持。

登记TK-SEC-01和相关工程惯例（密钥与IV不进生产CSV/JSON/场景），记录与现有Toolkit和SAVE-01/SEC-02的实际边界；保留原18探针与P2L实现，不提高全项目完整核验日期。输出.ai-workspace/outputs/DETACH-B13-20261019/AI-000274的REPORT.md、changed-files.json、evidence/acceptance、实际测试日志、RUNTIME-COMPATIBILITY.json、SAVE-HANDOFF.json（确定接口和示例）、ENGINE-HANDOFF.json（native/IL2CPP未验/计划）、工具三问。8项预声明合同inputs/acceptance/AI-000274.json，仅验CODE阶段。包目标1MB/硬10MB，超1MB说明必要性。每批源码写前Checkpoint；Complete/Fail/Yield立即Prepare→一次真实Send→Record回绑定CONTROL，执行器不自行重试/领取后继。

> SUPERVISED POLICY at 2026-10-19T19:55:52.8468996+08:00: {"policy":{"resourceKeys":["unity-project","documentation","data-batch"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"Original SEC-01 CODE source/offline phase only; native IL2CPP acceptance deferred to ENGINE; existing window2 and A+C remain.","updatedAt":"2026-10-19T19:55:52.7710771+08:00","supervisionLevel":"full"},"taskId":"AI-000274"}

> SUPERVISED EXTEND at 2026-10-19T19:55:53.1337460+08:00: {"reason":"Original user-authorized DETACH batch1+3 SEC-01 CODE source/offline; follows AI273. Pre-register dependency waiting; native/IL2CPP/window2 human gates not waived, no dispatch now.","taskIds":["AI-000274"]}
## AI-000275 | [CODE] [SC] SAVE-01 版本化存档与保存边界（源码/隔离离线）

- 序号：275
- 提交时间：2026-10-19T20:14:34.1558635+08:00
- 模块 ID：SC
- 初始状态：QUEUED
- 前置依赖：AI-000266, AI-000272, AI-000274
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：M / 80

### 指令

原批次DETACH-B13-20261019 SAVE-01源码/离线阶段，原卡cards/SAVE-01.md与G76/G77为依据。固定CODE、FEATURE、module SC；用户原卡M80覆盖新TK通常L建议，真实规模/预算差异留账。dependsOn AI266/AI272/AI274（ASM/MIG-F02和profile已通过、共享codec由SEC确定）。仅登记等待，AI274成功且CONTROL独立验收后派发。

先读CORE/CODE worker、项目入口页首及实际相关模块、原卡、AI272 SAVE-HANDOFF、AI274 REPORT/RUNTIME-COMPATIBILITY/SAVE-HANDOFF与实际ISaveCodec。当前已核packages-lock包含com.unity.nuget.newtonsoft-json，manifest未直接列出；须核实际已安装版本/程序集可用性，不能只凭锁文件宣称可引用。沿用工程既有JSON库，不下载新依赖、不改Packages。若用Newtonsoft，其JToken等应合理映射原卡payload分节而非强行混入System.Text.Json.JsonElement；外部JSON字段与语义保持，不引第二套JSON库。仅引用当前Toolkit可见依赖，发现程序集需额外引用/安全边界冲突须先报告而非修改asmdef或放宽引用方向。

实现SaveEnvelope（format_version/schema_version/created_utc/payload/hmac）、JsonSaveStore、PlainJsonCodec及唯一ShortCycle Runtime SaveBoundary宿主接入。原共享ISaveCodec精确复用AI274，不重建同名或改为不兼容接口；EncryptedJsonCodec在发布构建注入，Editor/Development明文按原编译条件；原认证算法/版本与key provider沿用SEC。记录schema版本来源为当前已确认版本，不臆定v2.5。Envelope分节首节player_profile，快照复用AI272的既有owner/不可变复制接口。Toolkit.Persistence不引用ShortCycle、场景/Transform或业务Manager；不写PlayerPrefs。

实际保存边界使用现有ISaveBoundary.Reach(ShortCycleSavePoint,ShortCycleContext)：存档点1进P0成功后、存档点2 P1→P2处理后；恢复仅BeginSession前/None阶段。保留现有ObservableNoOpSaveBoundary的观察契约/既有测试与18探针，不加每帧/收藏即时写盘等新保存点，不把纯Core拉入Unity/IO。Runtime宿主获取Application.persistentDataPath并注入store根路径，不能硬编码用户目录或旧DefaultCompany/Button Project；生产目标仍Trollemma/EatWhat下saves/<slot>.json。可测试路径注入只在隔离夹具使用，本轮不得触碰真实用户存档目录。

JsonSaveStore做受限slot/根内路径、原子临时写+安全替换、失败诊断。损坏、未知format/schema、校验失败时不抛出阻止进游戏，退默认profile并一次warning；读失败不修改/删除原文件，随后的自动保存不得无声覆盖唯一损坏原件（明确保留原件或停止该slot写入并诊断），不迁移旧目录。hmac须有真实完整性语义与稳定被认证字节定义，不能空字段/普通hash冒充HMAC；复用SEC安全能力，不重复维护密钥。首次无文件属于正常默认，不伪报损坏；保留未知payload节/版本策略在文档写清，不猜业务值。

允许Tools/Persistence源码、必要最小Tools/Security复用扩展（不削弱AI274）、ShortCycle Runtime及Session/既有契约的最小宿主集成、既有DevTools离线统一验证与所属Persistence夹具、相关项目/TK/工程文档、本任务outputs。禁止改生产场景/prefab/asset/meta/ProjectSettings/Packages/程序集方向、CSV/xlsx/Generated/字体和用户真实存档；不Unity/Play/Build/导表/Publish/提交推送。场景接线/原生编译/收藏跨会话与损坏文件Play验证交ENGINE后续窗口②，源码接入不声称已实际可玩。

隔离临时目录进行真实文件IO定向验证：写读往返/多次幂等、profile复制恢复、slot越界拒绝、原子替换保留旧文件的失败路径、损坏及版本不符diagnostic且原件完整、缺文件默认、只warning一次、恢复在BeginSession前、两个现有保存点各一次、Plain/Encrypted codec条件与可替换、无新增写盘入口。测试不触达persistentDataPath，继承安全与ShortCycle原测试，遵守验证成本与Final重复规则；先准确核已有runner再扩充，正式失败按稳定key结算回传，不自行重启。

登记TK-DAT-07 Persistence及真实Toolkit边界/编解码/HMAC/版本/回退说明，相关项目文档定向维护，不提高全项目完整复核日期。输出.ai-workspace/outputs/DETACH-B13-20261019/AI-000275：REPORT.md、changed-files.json、evidence/acceptance、真实离线日志、SAVE-FORMAT.json与ENGINE-HANDOFF.json（运行路径、两保存点、原生/Play待验、受损样本保护与清理）。8项合同inputs/acceptance/AI-000275.json仅验CODE阶段。包目标1MB硬10MB；每批写前Checkpoint，结算立即Prepare→一次真实Send→Record；窗口②/A+C/U2～U6保持，不自行派后继。

> SUPERVISED POLICY at 2026-10-19T20:14:34.5680952+08:00: {"taskId":"AI-000275","policy":{"resourceKeys":["unity-project","documentation","data-batch"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"Original SAVE-01 source and isolated offline file IO only; actual user save/Editor/Play/window2 and A+C not authorized by this phase.","updatedAt":"2026-10-19T20:14:34.4947905+08:00","supervisionLevel":"full"}}

> SUPERVISED EXTEND at 2026-10-19T20:14:34.7002087+08:00: {"taskIds":["AI-000275"],"reason":"Continue original DETACH batch1+3 after SEC codec independent acceptance, dependency waiting only; original native/human gates retained."}
## AI-000276 | [CODE] [WF] MIG-F08 文本资产 Git 规范化保护比较

- 序号：276
- 提交时间：2026-10-19T20:50:00.5181316+08:00
- 模块 ID：WF
- 初始状态：QUEUED
- 前置依赖：AI-000266, AI-000275
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：S / 40

### 指令

原批次 DETACH-B13-20261019 MIG-F08，固定 CODE/DevTools，FEATURE，S/40 按用户原卡执行；实际文件数/新工具登记与预算差异如实解释。依赖 AI266（E-1已通过）与AI275（同线串行及文档写入顺序）。本卡尚仅登记等待，不提前领取。

先读CORE/CODE worker、项目入口页首相关模块、cards/FILL_MIG-F02_F04_F08.md、MIG63/H后分支切换与控制台移交.md的MIG-F08条目，以及实际 DevTools/GitHooks/{README.md,check-mig63-font.sh,test-font-guard.py}、MIG63/pre-commit.sh、.gitattributes与相关现有保护工具。E2已解除临时迁移冻结，现行pre-commit只有永久字体与main/backup保护；不能恢复旧冻结或凭空给所有业务文本加禁止修改门。先定位仍有效的文本比较消费者；不存在时交付本卡所需的显式基线/显式路径只读比较入口及使用说明，禁止把新比较默认全仓启用。既有GitHooks工具族最小扩充，不造新队列/保护架构。

比较明确基线提交中的blob、index和按当前.gitattributes及Git clean规则规范化后的工作树内容，逐项给出基线/index/工作树身份及比较结果。不能只查HEAD/index漏检未暂存变化，也不能只删除CR字符假装Git规则。LF/CRLF且语义相同应等价；真实字段变化、仅未暂存变化、缺失路径、无法读取/未合并必须失败关闭并给出原因。读参数路径限定仓库内明确路径，未知/失败不能报通过；不写原仓库index/object/config/hooks或改行尾。原始SHA保留为同机备份完整性证据，与文本规范化身份分列。基线身份必须来自明确既存commit，不能以当前工作树更新基线迎合通过。

LFS及字体继续使用真实内容OID/SHA，不把LFS指针blob当字体字节，也不将字体或manifest的-text改成文本规范化；保留STATIC_ONLY已批准清单、真实静态字节、工作树/index双检、main/backup保护和旧许可。不要重建字体/改批准SHA/改本地hook或git配置；本轮只修改可审阅的受版本管理工具与文档。若一般文本比较无现有消费者，报告此事实和显式调用点，不声称生产hook自动安装生效。

验证只用小型隔离临时Git仓库及同一提交的两个clone（可为夹具创建本地提交，无网络/原项目提交），真实设置不同换行检出，直接调用交付的比较入口。覆盖等价行尾、真实字段变化、未暂存变化、缺失/不可读、基线/index/工作树三方与LFS真实内容；原font-guard定向回归保留。禁止克隆整个Unity工程/全仓归一化/全仓SHA、Unity/Play/Build/导表/Publish。遵守现有Verify-All的定向与缓存复用机制，不为小工具重跑不相关业务Final。

允许 DevTools/GitHooks 下最小实现/既有或必要定向夹具、现有验证登记接入（确有必要）、相关README/MIG63/工具索引定向更新及本卡outputs。禁止修改生产Assets/Packages/ProjectSettings/字体/CSV/xlsx/Generated、用户存档、.git或运行时队列数据；不提交/推送/切分支。不覆盖其他工作成果。具体新TK编号先查现有登记，不能复用已占用编号；原卡S40与实际规模差异留账。

输出.ai-workspace/outputs/DETACH-B13-20261019/AI-000276：REPORT.md、changed-files.json、evidence.json、acceptance.json、实际双clone/保护回归日志及调用说明（真实入口/基线身份/规则/生产安装状态）。合同inputs/acceptance/AI-000276.json为5个事实布尔量及productionAssetIndexConfigHookWrites=0。包目标1MB硬10MB；每批写前Checkpoint。真实失败按稳定key结算回传，不自行重启；1/3/5只由CONTROL经正式ApproveResume组织。结算立即Prepare→一次实际Send→Record，窗口②/A+C保留，不派后继。

> SUPERVISED POLICY at 2026-10-19T20:50:00.9403218+08:00: {"policy":{"resourceKeys":["workflow-devtools","documentation","unity-project"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"Original MIG-F08 tool/source and isolated fixture scope only; no production hook installation, asset or project Git metadata writes; preserve window2 and A+C.","updatedAt":"2026-10-19T20:50:00.8804802+08:00","supervisionLevel":"full"},"taskId":"AI-000276"}

> SUPERVISED EXTEND at 2026-10-19T20:50:24.6535313+08:00: {"taskIds":["AI-000276"],"reason":"User-authorized DETACH B1+3 MIG-F08 original card, source/tool phase; wait for AI275 acceptance before same fixed CODE dispatch. MIG-F04 reuses accepted AI146/147/152 scope."}

> Verification rejected for AI-000275 at 2026-10-19T21:27:47.1276563+08:00: decision=REJECTED; reason=Independent isolated counterexample: finite JSON1e-30 and1e100 save successfully then fail own authenticated read and block the slot; repair before accepting source phase.
## AI-000277 | [CODE] [SC] SAVE-01 数字规范化与认证往返修复

- 序号：277
- 提交时间：2026-10-19T21:29:16.3281707+08:00
- 模块 ID：SC
- 初始状态：QUEUED
- 前置依赖：无
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：M / 80

### 指令

AI277是原AI275同根REPAIR，稳定key SAVE01_NUMERIC_CANONICAL_ROUNDTRIP。用户2026-10-19长期SUPERVISED5周期政策来源inputs/续行许可分模式_用户长期授权_20261019.json；本次首个修复周期AI275:CODE277:retry1，首次实际claim计1/5，原275成功报告/自验失败和CONTROL拒验完整保留。M80按最小实现及必要文档实际规模登记，不新建业务根、不重跑原275整个实现。用户原窗口②/A+C、真实存档/Unity禁止保持。领取必须原固定CODE/同session，按派发提供的Scope传Poll/Checkpoint，不二次Poll。

先读CORE/CODE worker、原AI275 REPORT/SAVE-FORMAT/changed-files及CONTROL的outputs/control/DETACH-B13-20261019/independent-review-275.json、AI275-numeric-roundtrip-review.json。原2758合同自报通过、26源码/文档SHA和3runner真实日志已核，但CONTROL使用交付的Persistence-release.exe、合成测试key和控制输出隔离目录实测：finite double1.25写读通过；1e-30和1e100均Write=true而TryRead=false，slot被blocked。根因候选是SaveEnvelope.Parse的FloatParseHandling.Decimal，对签名阶段接受的double范围发生下溢/溢出；请对源码和实际捆绑Mono确认，不重新跑整个业务诊断。

目标：所有被成功写入的当前合法Envelope，重新解析后保持数值语义和稳定认证字节，可经Plain/Encrypted两codec读回；不允许Write成功后把自身文件变坏。最小修正数字规范化/解析一致性，支持当前通用JSON声明和未来未知节；必须覆盖1e-30/1e100及普通double、decimal高精度、大整数和整型/浮点边界。不能只把Decimal换Double造成decimal/大整数静默精度损失，也不能关闭HMAC、删unknown节、把数字无声改字符串/零或在fixture忽略失败。若存在明确不支持的输入，必须在原文件被改动前明确诊断拒绝写入，文档定义范围；不得将支持限制伪报为成功往返。现有可读存档兼容、profile字符串数组、UTC/版本、坏件保留、原子写/漂移门、原两点/只warning一次、密钥标签/原V2包/codec接口不变，不能自动迁移真实用户文件。

只准最小修改已审批Scope中的SaveEnvelope/JsonSaveStore/EnvelopeAuthenticator与既有Persistence fixture/runner、对应PST说明和必要项目定位页首，实际不需要的文件不碰。优先复用原测试组补数字反例，不新增专用续行入口或另一套JSON库。CONTROL原275证据和合成坏件不改删；实际修复前把本卡触及源码起点保存在AI277输出，并记录before/after SHA及差异。

验收：针对既有Persistence runner真实主机及捆绑Mono(release/UNITY_EDITOR/DEVELOPMENT_BUILD)隔离IO，逐值比较语义/类型边界、二次进程或新store读回和HMAC；Plain与V2Encrypted均覆盖。加入精度边界和NaN/Infinity拒绝、既有文件不变反例；使用合成key，真实用户路径禁用。已有原子失败/损坏保留/共享codec/AI274包等回归保持；仅重跑真实受影响Persistence及必要ShortCycle相关组，不重复SEC或全域Final/全仓哈希。不Unity/Play/Build/生产数据/安装/存档接触/提交推送。

交付outputs/DETACH-B13-20261019/AI-000277中的REPORT、changed-files、evidence/acceptance、数字行为矩阵(输入类型/值、写结果、读结果、读回语义、是否拒绝前保留原件)、真实日志、对原275合同补充映射、ENGINE交接限制。6合同见inputs/acceptance/AI-000277.json。目标1MB硬10MB，超1MB说明必要性并尽量精简。完成立即Prepare→一次实际Send→Record给CONTROL；CONTROL独立复核后才能ResolveControlRejection原275，不能自行改原结果/领取AI276。新失败正式结算，CONTROL经ApproveResume按剩余额度处理，不自行重试业务或换key。

> SUPERVISED POLICY at 2026-10-19T21:30:01.9919447+08:00: {"policy":{"resourceKeys":["unity-project","documentation","data-batch"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"Same-root AI275 control rejection; one POLICY repair cycle, minimal numeric canonicalization and isolated tests only; no real user save or Editor.","updatedAt":"2026-10-19T21:30:01.9044698+08:00","supervisionLevel":"full"},"taskId":"AI-000277"}

> SUPERVISED EXTEND at 2026-10-19T21:30:02.2337046+08:00: {"reason":"Same-root minimal repair for independently rejected AI275 within existing SUPERVISED default5 policy; AI276 remains dependent on rejection resolution.","taskIds":["AI-000277"]}
> Control rejection resolved AI-000275 : {"at":"2026-10-19T21:53:26.5581984+08:00","decision":"CONTROL_REJECTION_RESOLVED","problemKey":"SAVE01_NUMERIC_CANONICAL_ROUNDTRIP","repairTaskId":"AI-000277","controllerThreadId":"01a13ef5-eda9-7882-b1c9-eca47a6470d9","userDecision":"批准本次修复和续行，另外，对相关的续行许可次数限制进行放宽：手动模式时，每次需要人工续行不变，上限保持为1；自动模式时，允许3次的重复修复尝试后，再进行人工询问和额外次数的批准；监管模式下则默认允许5次自主重复尝试","verification":"6 declared contracts and 15 independent checks passed; original tiny/large plus precise decimal/BigInteger in both codecs, fresh-store authenticated reads; original rejection retained; native verification remains pending.","evidencePath":"D:\\GameProject\\Eat-What\\.ai-workspace\\outputs\\control\\DETACH-B13-20261019\\AI275-control-rejection-resolution.json","evidenceSha256":"9715255D2B255A2668CF4B6242B3489BF61E03F389D5629A34A8DADDB8CA4B39","rejectedResultSha256":"070F5A83414B3BDFEAD30A878706335FCC607DCAB6C41C281085EF55A55BE6AB","repairResultSha256":"1B544647C460121A04C5FD2E4CAD92A708C5295A4A6D017B445CD5EE6FCFDEFF"}
## AI-000278 | [ENGINE_CLI] [SC] 窗口② D-T2/SEC02/P2S 只读核查与精确审批包

- 序号：278
- 提交时间：2026-10-19T21:58:29.7916718+08:00
- 模块 ID：SC
- 初始状态：QUEUED
- 前置依赖：AI-000266, AI-000269, AI-000272, AI-000273, AI-000274, AI-000275, AI-000276, AI-000277
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：M / 80

### 指令

原批次 DETACH-B13-20261019 的窗口②只读准备，固定 ENGINE_CLI，FEATURE/M80（4组相关核查和审阅材料）。dependsOn AI266/269/272/273/274/275/276/277；只有前置结算且CONTROL独立通过后领取。此卡产物用于原D-T2/SEC-02/P2S-01的精确人工窗口审批，HumanGateAfter=true；准备成功不批准窗口、不等于12逻辑卡完成。

先读CORE/ENGINE_CLI worker、项目入口页首和相关模块、原cards/{D-T2,SEC-02,P2S-01}.md；P2原件inputs/排布指导书_ShortCycle_P2_v0_1_1_20261015.md只读§二矩阵/§三对象树；D-T1a/b、P2L、SEC01、SAVE01/277的ENGINE-HANDOFF及control/DETACH-B13-20261019/{D-T2-content-followups,P2L-data-followups}.json按相关条读取。原D-T2卡场景路径漏了Cooking，实际为Assets/Scenes/Cooking/ShortCycle_P0P1.unity；此处校正不授权写场景。

四组交付：
1. 原生身份与安全基线：通过已登记Unity官方CLI、既有Invoke-TaskTool包装器先核唯一D:/GameProject/Eat-What / 6000.3.24f1 / PID/port和新鲜editor_status。status与editor_status每项最多一次、保持30s/35s上限；随后仅使用已存在且明确只读的场景/组件/Console查询。记录当前scene、dirty/Play/compile状态、实际Console Error。不得Refresh/Compile/Import/Play/OpenScene/Save/修改设置/执行自建脚本；不要以诊断名义生成或安装Editor工具。只读入口不足时用现有场景文件只读定位，明确磁盘与内存观测差异及unknown，不冒充已查询。未知写入/平台拒绝/错目标正式停止，不换MCP。
2. D-T2精确绑定清单：逐目标CatalogCard×8、Title、DishPhoto_TUNE、IngredientList、StickerNotes、CurrentRecipeTab、StepDetail容器、FlipAnchor_L/R列实际层级/组件/字段/当前引用/拟引用及GUID或子资产身份、允许修改项、保持Transform/_TUNE/Sorting/字体/Tag/Layer不变项、备份/回退和验收方法。需新增组件与仅改字段要分开，原卡不明确授权的组件增补列入审批差异，不提前执行。核原18条探针实际定义及新增5条尚未Play事实、LayoutAudit及3份可见区v2现有入口。9空图形字段/5文件名未命中须针对性核GUID/子资产，不把命名未命中写成缺资产；×字形只报可观测情况，字体修改归DATA。输出审批清单，不导入美术、不改CSV。
3. P2S精确范围：对照P2指导书§二/三，列新Assets/Scenes/ShortCycle_P2.unity空壳对象树、Cam_P2(0,0,-10)正交7.2、Managers与七组空容器/DebugAndReferences、P0P1 viewTable增行与Build末尾index2。核当前Build路径/index和P2路径是否已存在，不能覆盖用户同名工作；保留旧CookingProcess。列现有Phase2TransitionAdapter/Handoff/KitchenInventory入口、原18探针后追加2条、Teardown回退与既有23动作预留/MG0核查，不加波次C内容或私增动作。这里只列候选，不新建场景/代码/Build项。
4. SEC02与原生补验边界：核实际Mono/IL2CPP安装与构建后端配置、现有Generated SO路径/Resources引用/构建消费者、StreamingAssets/data/catalog.bin与Assets/Editor/Build目标现状。给方案A具体候选写路径/临时生成物/恢复策略与CODE→DATA→ENGINE职责，SEC01真实GCM Mono不支持，V2 CBC+HMAC回退需列给窗口审批，不能悄悄改原卡GCM要求。以既有生成SO真实范围拟排除策略；只有候选，不导表/Publish/Build/修改production加载。列原生SEC/SAVE所需隔离存档注入与两个保存点验证，禁止读写真实用户存档、不得通过直接Play误触真实持久化。

输出仅本卡outputs/DETACH-B13-20261019/AI-000278：REPORT.md、window2-review.md（用户可审阅的精确路径/对象/差异/动作/回退/成本/人工保留项）、window2-scope.json、target-and-observations.json、evidence.json、acceptance.json及短原始查询日志。4项合同inputs/acceptance/AI-000278.json；必要unknown逐条列原因，未知不写通过。productionWrites=0为硬界限，package目标1MB硬10MB。现有读取/身份查询按Observation，不假称生产进展。无新增运行时工具/新TK、无全仓哈希/内存转储、无真实业务执行。必要工程状态无法确认则正式Fail并完整回传，CONTROL按根策略组织，不执行器自重试。Complete时只表示审批准备完成，HumanGateAfter保持未批准；立即Prepare→一次Send→Record绑定CONTROL后停止，不自行下发后继。

> SUPERVISED POLICY at 2026-10-19T21:58:30.2219309+08:00: {"taskId":"AI-000278","policy":{"resourceKeys":["unity-project","documentation","read-only"],"reviewDependsOn":[],"humanGateAfter":true,"gateApproved":false,"reason":"Authorized batch1+3 windowII prerequisite review only; no scene/code/Build/data writes. Actual window approval remains a hard human gate.","updatedAt":"2026-10-19T21:58:30.1652087+08:00","supervisionLevel":"full"}}

> SUPERVISED EXTEND at 2026-10-19T21:58:30.3754167+08:00: {"taskIds":["AI-000278"],"reason":"Continue original12logicalcards by preparing concrete windowII review after source chain; AI276 alone does not complete batch."}

> SUPERVISED APPROVED_CONTINUATION_REFUSED at 2026-10-19T22:09:09.3281585+08:00: {"controllerThreadId":"01a13ef5-eda9-7882-b1c9-eca47a6470d9","problemKey":"","reason":"Original task thread must remain bound.","taskId":"AI-000276"}

> SUPERVISED RESUME_APPROVED at 2026-10-19T22:09:56.4761521+08:00: {"cycles":["AI276:retry1"],"sourceApprovalId":"call_37410ba043344e9f81b4ce3ef3472a18:0","used":0,"authorizationSource":"POLICY","verification":"Fresh app revision165 CODE idle; formal attempt1 failure independently verified5checks; explicit per-clone core.eol/autocrlf before first real checkout; no production mutations. Independent-failure-review-276.json; original scope, first policy cycle,0of5 consumed before claim.","remaining":4,"reserved":1,"rootTaskId":"AI-000276","scope":{"paths":[".ai-workspace/MIG63/H后分支切换与控制台移交.md",".ai-workspace/outputs/DETACH-B13-20261019/AI-000276",".ai-workspace/TOOLKIT_REGISTRY.md",".ai-workspace/toolkit/INDEX.md",".ai-workspace/toolkit/WF.md","DevTools/GitHooks/compare-git-content.py","DevTools/GitHooks/README.md","DevTools/GitHooks/test-git-content.py","DevTools/GitHooks/Test-GitContent.ps1"],"operations":["documentation-update","evidence-write","idempotence-check","offline-validation","source-repair"]},"grantId":"f75fd89fea4e41fca8bee2c9cb63fe18","taskId":"AI-000276"}

> SUPERVISED RESUME_APPROVED at 2026-10-19T22:21:33.5505208+08:00: {"taskId":"AI-000276","scope":{"paths":[".ai-workspace/MIG63/H后分支切换与控制台移交.md",".ai-workspace/outputs/DETACH-B13-20261019/AI-000276",".ai-workspace/TOOLKIT_REGISTRY.md",".ai-workspace/toolkit/INDEX.md",".ai-workspace/toolkit/WF.md","DevTools/GitHooks/compare-git-content.py","DevTools/GitHooks/README.md","DevTools/GitHooks/test-git-content.py","DevTools/GitHooks/Test-GitContent.ps1"],"operations":["documentation-update","evidence-write","idempotence-check","offline-validation","source-repair"]},"remaining":3,"authorizationSource":"POLICY","grantId":"1fd438e58e8348f4bfdf378d9331aa0b","reserved":1,"rootTaskId":"AI-000276","used":1,"verification":"Fresh app revision166 CODE idle completed01a15480-242e-71a1-91b8-cef73751d6ab; independent failure review7checks confirmsEOLfix/unchangedprotectedSHA/log and currenttwo safe.directory keys. Minimal validated indexed-config classification, original scope, no global config/env/production mutations. Root used1of5, newcycle retry2.","sourceApprovalId":"call_37410ba043344e9f81b4ce3ef3472a18:0","cycles":["AI276:retry2"]}

> SUPERVISED RESUME_APPROVED at 2026-10-19T22:31:14.0895434+08:00: {"rootTaskId":"AI-000276","remaining":2,"verification":"Fresh CODE idle revision167 completed01a1548a-9eee-7f41-af52-a28f6f8c3c62; independent failure review7checks confirmslocalized GBK JSON versus strict UTF8 consumer. Minimal locale-independent lossless emit/targeted Unicode regression, originalscope. Root used2of5 beforeclaim, no platform or business human gate bypass.","cycles":["AI276:retry3"],"reserved":1,"grantId":"32081b38e05c498cbe04ba064444aa89","sourceApprovalId":"call_37410ba043344e9f81b4ce3ef3472a18:0","authorizationSource":"POLICY","taskId":"AI-000276","used":2,"scope":{"paths":[".ai-workspace/MIG63/H后分支切换与控制台移交.md",".ai-workspace/outputs/DETACH-B13-20261019/AI-000276",".ai-workspace/TOOLKIT_REGISTRY.md",".ai-workspace/toolkit/INDEX.md",".ai-workspace/toolkit/WF.md","DevTools/GitHooks/compare-git-content.py","DevTools/GitHooks/README.md","DevTools/GitHooks/test-git-content.py","DevTools/GitHooks/Test-GitContent.ps1"],"operations":["documentation-update","evidence-write","idempotence-check","offline-validation","source-repair"]}}

> SUPERVISED APPROVED_CONTINUATION_REFUSED at 2026-10-19T22:43:47.9261467+08:00: {"problemKey":"","taskId":"AI-000276","reason":"Requested path exceeds approved scope.","controllerThreadId":"01a13ef5-eda9-7882-b1c9-eca47a6470d9"}

> SUPERVISED RESUME_APPROVED at 2026-10-19T23:35:57.6874579+08:00: {"rootTaskId":"AI-000276","reserved":1,"sourceApprovalId":"call_3d3fb0c5a62447dfba9402fcbdb6a825:0","grantId":"0992d7f1d2e1428f98b1ade56fa151f2","authorizationSource":"HUMAN","used":3,"scope":{"paths":[".ai-workspace/MIG63/H后分支切换与控制台移交.md",".ai-workspace/outputs/DETACH-B13-20261019/AI-000276",".ai-workspace/TOOLKIT_REGISTRY.md",".ai-workspace/toolkit/INDEX.md",".ai-workspace/toolkit/WF.md","DevTools/GitHooks/compare-git-content.py","DevTools/GitHooks/README.md","DevTools/GitHooks/test-font-guard.py","DevTools/GitHooks/test-git-content.py","DevTools/GitHooks/Test-GitContent.ps1"],"operations":["documentation-update","evidence-write","idempotence-check","offline-validation","source-repair"]},"cycles":["AI276:retry4"],"remaining":1,"taskId":"AI-000276","verification":"User explicitly approved one existing-fixture path addition and one continuation within current limit5. Exact authority AI276_范围调整_用户批准_20261019.json. CONTROL independent-failure-review-276-attempt4.json 8/8; current source SHA still matches prepared patch. Fresh CODE idle app revision168 completed turn01a15493-a8d9-7810-b859-19d861b3b5c2; snapshot code-idle-276-retry4-approved.json. ACTIVE same session, no stop/exit. AI278 and window2 human gate remain blocked."}

> Verification rejected for AI-000278 at 2026-10-20T00:20:47.1045939+08:00: decision=REJECTED; reason=CONTROL核场景8卡已有16个TagPoint及TMP/LocalizedText，审批包按Prefab缺失误提新增，接线合同不成立；保留原件，修正只读审批包后独立复核，窗口②未批准。
## AI-000279 | [ENGINE_CLI] [SC] 窗口②审批包场景新增节点映射修正（磁盘只读）

- 序号：279
- 提交时间：2026-10-20T00:21:46.1472654+08:00
- 模块 ID：SC
- 初始状态：QUEUED
- 前置依赖：无
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：M / 80

### 指令

AI279：原AI278的同根只读审批包修复，固定ENGINE_CLI，REPAIR/M80，稳定key WINDOW2_SCENE_ADDED_TAG_BINDINGS，cycle AI278:ENGINE279:retry1。原278成功但CONTROL拒验，旧结果/审批包/查询日志完整保留；本卡不能重新领取278或批准窗口②。长期SUPERVISED5授权来源inputs/续行许可分模式_用户长期授权_20261019.json，首次实际领取计同根第1周期，不重置历史。按AI279_SCOPE.json原样传Poll及Checkpoint。

只做磁盘只读核查与新输出包修订，生产写入0、额外Unity CLI/MCP调用0。不重新身份心跳、不Refresh/Compile/Import/Play/场景开存/Build/导表/Publish/实际存档、不改源码/资源/输入/原278输出。已有278时刻的只读观测可引用但必须保留原时间，不能冒充当前。只写本卡outputs/DETACH-B13-20261019/AI-000279。原窗口②、A+C、USER-U5等门保持。

先读CORE/本线规范和CONTROL独立证据outputs/control/DETACH-B13-20261019/{independent-review-278,AI278-existing-card-tags-review,AI278-visible-v2-source-recovery}.json，再按需读原278三份主审批文档及相关事实JSON。CONTROL已核8个CatalogCard scene instance每卡都有TagPoint_01/02，16个场景新增子节点均登记在PrefabInstance.m_AddedGameObjects，具有SpriteRenderer和Label上的TMP/LocalizedText。原文“旧Prefab只有Icon/Badge/Placeholder/Name，不能声称已有2标签点”把Prefab缺失混同场景实例缺失，会误导新增，须实质修正。

修复交付：
1. 逐8卡核场景新增子节点、stripped Transform和m_AddedGameObjects，给现有16个tagIcons和tagLabels准确scene fileID/脚本GUID/当前引用与推荐复用映射；核源CatalogCard真实字段类型。保持所有原Transform/_TUNE/排序/字体/Tag/Layer。不得只用名字/Prefab树断言缺失。对favorite以及Ingredient/Sticker等已经提到的Prefab实例，同样补核scene-added children，防止同类遗漏；没有证据时标unknown，不编造新增需求。
2. 更正完整window2-review.md、window2-scope.json、D-T2-approval-bindings.json（新卡输出内保存完整替代版，不原地覆盖278）；所有对象表/unknown/审批项同步，不残留误提16个新标签。确需新增组件/步骤行等保持候选，现有可复用与缺失新建分开。未定geometry不假称可直接生产执行，先形成明确有依据的候选/待设计项，不以本修复获得扩权。
3. 将CONTROL找回三份v2的archive entry/SHA、560/459/202行、现有只读collector/evaluator入口接入审批包。历史Unity2022结果只作来源/结构参考；当前Unity6已有用户布局/字体为基线，不执行旧写入脚本/全量归档提取/全仓哈希，不复原旧布局。不伪报已做本轮v2回归。
4. 保留原278已核P2 enum缺口、Build index2候选、SEC02 GCM→V2显式变更、170SO/1原生占位区分、pre-Awake隔离存档前置、缺失IL2CPP模块、9空/5MissingSprite真实子资产等边界。全部仍仅审批准备。

4组验收合同见inputs/acceptance/AI-000279.json。输出REPORT、上述3份完整更正稿、scene-added-object-evidence.json（必要fileID引用，不复制整个场景）、evidence/acceptance及correction-map.json（原278被更正项与仍成立项，绑定原拒验证据SHA）。验证成本限定为相关磁盘对象映射/文档一致性，不新造工具/测试或全项目保护；目标1MB硬10MB。正式失败先结算回传，CONTROL决定续行；不能执行器自行重试。Complete立即Prepare→一次实际Send→Record给绑定CONTROL。CONTROL独立复核后才ResolveControlRejection原278，原HumanGateAfter仍不批准，不派生产后继。

> SUPERVISED POLICY at 2026-10-20T00:22:00.2281588+08:00: {"taskId":"AI-000279","policy":{"resourceKeys":["unity-project","documentation","read-only"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"同根278审批包修正，仅磁盘只读和新卡报告输出；保留278窗口②人工门，不执行生产阶段。","updatedAt":"2026-10-20T00:22:00.1330133+08:00","supervisionLevel":"full"}}

> SUPERVISED EXTEND at 2026-10-20T00:22:00.5106971+08:00: {"reason":"原278独立拒验后的范围内审批材料修复；不批准原278窗口②。","taskIds":["AI-000279"]}
> Control rejection resolved AI-000278 : {"at":"2026-10-20T00:38:46.2713849+08:00","decision":"CONTROL_REJECTION_RESOLVED","problemKey":"WINDOW2_SCENE_ADDED_TAG_BINDINGS","repairTaskId":"AI-000279","controllerThreadId":"01a13ef5-eda9-7882-b1c9-eca47a6470d9","userDecision":"批准本次修复和续行，另外，对相关的续行许可次数限制进行放宽：手动模式时，每次需要人工续行不变，上限保持为1；自动模式时，允许3次的重复修复尝试后，再进行人工询问和额外次数的批准；监管模式下则默认允许5次自主重复尝试","verification":"4合同/14独立复核通过，16组准确场景标签/LocalizedText映射、15相关实例、02选择组件复用、三份历史v2来源及原件完整；0生产写入/0额外Unity，人工窗口保持未批准。","evidencePath":"D:\\GameProject\\Eat-What\\.ai-workspace\\outputs\\control\\DETACH-B13-20261019\\AI278-control-rejection-resolution.json","evidenceSha256":"59BFDCD62A07C31966F101FE3F9100770632CB1836EEDFE95175CEF954522C39","rejectedResultSha256":"60F17E3F58581B0A65CC68C751D55361F16FA678A230A8EF61C4391B05A5484E","repairResultSha256":"BC4678FB24D87EB23D994F888AC3083EF1733B957F9F300AA16DB0F66672FA6C"}
## AI-000280 | [CODE] [SC] D-T2/SAVE 隔离存档与动态文本前置（源码）

- 序号：280
- 提交时间：2026-10-20T00:44:03.6442073+08:00
- 模块 ID：SC
- 初始状态：QUEUED
- 前置依赖：AI-000275, AI-000277, AI-000279
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：M / 80

### 指令

AI-000280 / D-T2/SAVE 隔离存档与动态文本前置（源码）

用户已直接批准窗口②阶段①及算法调整，原话/ID/SHA见inputs/派发/DETACH-B13-20261019/窗口②阶段①用户批准.json（call_a8f7addab9304ac69c44dc698807bff9:0）。只批准源码/离线与只读布局候选，生产场景、导表/Publish、原生Compile/Play/Build和A+C仍未批准。原AI278 HumanGateAfter=true、gateApproved=false保持，后继卡根据本次独立阶段①批准安排，不能把父级机器解拒验当阶段②许可。不提交推送，不复原用户dirty，不写真实存档。先CORE、对应worker、项目两入口页首及本卡相关模块；AI279修正版只按相关条读取，避免重读全部历史。固定chat复用。每批写前正式Checkpoint；FEATURE首次不计重试，正式失败先Fail回传，CONTROL根据真实1/3/5根余量决定ApproveResume，不执行器自重启。Complete/Fail/Yield立即Prepare→一次真实app发送→Record给绑定CONTROL，只有CONTROL独立验收才推进依赖。输出完整文本目标1MB硬10MB，超1MB说明必要性；原失败/许可/验收证据保留。定向测试与改动相称，不重复全域Final/全仓哈希或创建一次性续行入口。原生/视觉/阶段②未验如实记录。

本卡M80，FEATURE，仅源码/离线。先核SessionManager.Awake当前构造JsonSaveStore(Path.Combine(Application.persistentDataPath,"saves"))与Runtime/SaveBoundary的PrepareBeforeSession和原两个保存点；读AI275/277必要接口和AI279 localizationOwnershipReview，不重跑旧数字全矩阵/安装。

一、提供显式、可作用于正确目标Session、在Awake初始构造前生效的验收存档隔离入口。默认生产路径和版本/codec/key/两保存点不变；离线与后续ENGINE验收必须明确注入本卡/后续任务输出下绝对隔离目录或受控storeFactory，未授权不得触摸真实用户目录。不能通过Awake后反射替换私有字段；不能依靠一个未验证的组件执行顺序声明安全。防止跨场景/跨用例泄漏、晚注入、重复/冲突注入、异常后未清理；交付准确pre-Awake安装与释放顺序，适用于正常Editor未来场景加载和隔离原生harness，不提前运行。若需要新辅助C#，只在ShortCycle/Runtime并明确ownership和销毁/恢复路径，复用JsonSaveStore构造注入，不新增另一套持久化系统或真实路径探测。
二、核RecipeBookBinder、ShortCycleGridPieceBase、Ingredient/Sticker/CurrentRecipeTab的动态TMP写入与现有LocalizedText.RefreshAll/Configure。保留既有组件和本地化key/fallback语义；内容更新走明确唯一writer，优先复用Configure，避免后续RefreshAll将新菜内容覆盖回旧值。16个tagLabels本来就是LocalizedText[]，不改成TMP。保持Inspector既有序列化字段兼容，不能无声删禁组件/删除用户配置；空/切菜/锁卡/收藏/clear与本地化刷新行为须一致。先追调用顺序，局部合理最小改动，不重构Toolkit或全项目本地化。
三、用已有ShortCycle/Persistence受影响runner及fixtures完成真实离线编译/行为验证：早注入生效且默认路径factory未被调用、晚/重复/错目标拒绝与释放、两个保存点/首次None恢复/坏件保护保持；已接LocalizedText与无该组件fallback、换菜再RefreshAll/clear不回旧文案。使用合成key及控制隔离临时根，不读写/枚举用户存档，不把stub通过当真实Awake/Play验证。保留原18探针，运行成本与真实改动相称。
交付REPORT、changed-files/evidence/acceptance、真实离线日志与ENGINE-HANDOFF.json，后者给隔离入口安装/复原、文本writer与实际接线变化、原生仍待验证项；指出本卡未修改任何生产引用。更新实际受影响项目/TK定位，复用TK-DAT-09/现LOC，不仅改日期。

精确允许范围见AI280_SCOPE.json；领取及Checkpoint带Scope。验收合同inputs/acceptance/AI-000280.json，只能以真实证据填值。全部输出到.ai-workspace/outputs/DETACH-B13-20261019/AI-000280，每项合同必须能回到实际文件/日志。

> SUPERVISED POLICY at 2026-10-20T00:44:04.1306365+08:00: {"taskId":"AI-000280","policy":{"resourceKeys":["unity-project","documentation","data-batch"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"用户call_a8f7addab9304ac69c44dc698807bff9:0明确批准阶段①；仅源码离线或只读候选，原278阶段②生产gate不批准。","updatedAt":"2026-10-20T00:44:04.0776879+08:00","supervisionLevel":"full"}}
## AI-000281 | [CODE] [SC] P2S-01 过渡交接与返回（源码/离线）

- 序号：281
- 提交时间：2026-10-20T00:44:04.2547600+08:00
- 模块 ID：SC
- 初始状态：QUEUED
- 前置依赖：AI-000280, AI-000273, AI-000279
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：M / 80

### 指令

AI-000281 / P2S-01 过渡交接与返回（源码/离线）

用户已直接批准窗口②阶段①及算法调整，原话/ID/SHA见inputs/派发/DETACH-B13-20261019/窗口②阶段①用户批准.json（call_a8f7addab9304ac69c44dc698807bff9:0）。只批准源码/离线与只读布局候选，生产场景、导表/Publish、原生Compile/Play/Build和A+C仍未批准。原AI278 HumanGateAfter=true、gateApproved=false保持，后继卡根据本次独立阶段①批准安排，不能把父级机器解拒验当阶段②许可。不提交推送，不复原用户dirty，不写真实存档。先CORE、对应worker、项目两入口页首及本卡相关模块；AI279修正版只按相关条读取，避免重读全部历史。固定chat复用。每批写前正式Checkpoint；FEATURE首次不计重试，正式失败先Fail回传，CONTROL根据真实1/3/5根余量决定ApproveResume，不执行器自重启。Complete/Fail/Yield立即Prepare→一次真实app发送→Record给绑定CONTROL，只有CONTROL独立验收才推进依赖。输出完整文本目标1MB硬10MB，超1MB说明必要性；原失败/许可/验收证据保留。定向测试与改动相称，不重复全域Final/全仓哈希或创建一次性续行入口。原生/视觉/阶段②未验如实记录。

原cards/P2S-01.md的CODE阶段，沿用用户原M80，实际规模差异留账，不冒称整逻辑卡完成。AI280须CONTROL独立验收后才派，复用其安全隔离入口与最新源码。
核真实现有SessionManager内ShortCyclePhase2TransitionAdapter、ShortCycleSpace.viewTable(ShortCyclePhase0View)、ShortCyclePhase2HandoffPresenter、ShortCycleDataHandoff与InventoryManager读写面。原卡路径是设计名称，按当前声明位置最小实现，不创建同名并行类。Phase2Transition路由保持所有现有枚举整数/旧序列化行兼容，禁止用未经支持的YAML数字。复用FSM/Director作为唯一状态/显隐owner，按P2指导书§二矩阵把P1 Set和线索板收尾，Additive加载审定路径后消费recipeId/prepZoneContents并移交既有Inventory所有权；Phase2/Phase2SceneEntry只空壳/可观察状态，无分区/工具/美术内容。
Teardown须卸载任务所有的P2、退订事件、解除旧owner、回P1恢复状态，重复进入/退出不叠订阅、不丢/复制库存；加载失败或中断能明确失败和恢复，不伪报已到P2。不让字符串场景名加载旧CookingProcess；由后续ENGINE配置新场景Buildindex2。原18探针逐字/语义保持，在后追加19/20交接和返回，禁止为让旧探针过而改断言。原23动作未含4个P2预留只报告扩全集申请，不补动作/CSV。MiniGames.Core契约存在只核不实现小游戏。
只写允许C#、既有定向测试/runner和文档。禁止生成.unity/.prefab/.asset/.meta、改Build/ProjectSettings、真正Editor/CLI/MCP、导表、真实存档、生产数据。离线验证交接数据/ownership、矩阵路由、异步成功失败、Teardown/重复进出及现有相关行为；Unity真实加载/布局仍由ENGINE阶段②。交付REPORT/变更/日志/evidence/acceptance、ENGINE-HANDOFF精确新组件/字段/入口及未验项，不用离线通过宣称可玩。

精确允许范围见AI281_SCOPE.json；领取及Checkpoint带Scope。验收合同inputs/acceptance/AI-000281.json，只能以真实证据填值。全部输出到.ai-workspace/outputs/DETACH-B13-20261019/AI-000281，每项合同必须能回到实际文件/日志。

> SUPERVISED POLICY at 2026-10-20T00:44:04.4772564+08:00: {"taskId":"AI-000281","policy":{"resourceKeys":["unity-project","documentation","data-batch"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"用户call_a8f7addab9304ac69c44dc698807bff9:0明确批准阶段①；仅源码离线或只读候选，原278阶段②生产gate不批准。","updatedAt":"2026-10-20T00:44:04.4150340+08:00","supervisionLevel":"full"}}
## AI-000282 | [CODE] [SC] SEC-02 发布数据源与构建管线（源码/离线）

- 序号：282
- 提交时间：2026-10-20T00:44:04.6142919+08:00
- 模块 ID：SC
- 初始状态：QUEUED
- 前置依赖：AI-000280, AI-000274, AI-000269, AI-000279
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：L / 120

### 指令

AI-000282 / SEC-02 发布数据源与构建管线（源码/离线）

用户已直接批准窗口②阶段①及算法调整，原话/ID/SHA见inputs/派发/DETACH-B13-20261019/窗口②阶段①用户批准.json（call_a8f7addab9304ac69c44dc698807bff9:0）。只批准源码/离线与只读布局候选，生产场景、导表/Publish、原生Compile/Play/Build和A+C仍未批准。原AI278 HumanGateAfter=true、gateApproved=false保持，后继卡根据本次独立阶段①批准安排，不能把父级机器解拒验当阶段②许可。不提交推送，不复原用户dirty，不写真实存档。先CORE、对应worker、项目两入口页首及本卡相关模块；AI279修正版只按相关条读取，避免重读全部历史。固定chat复用。每批写前正式Checkpoint；FEATURE首次不计重试，正式失败先Fail回传，CONTROL根据真实1/3/5根余量决定ApproveResume，不执行器自重启。Complete/Fail/Yield立即Prepare→一次真实app发送→Record给绑定CONTROL，只有CONTROL独立验收才推进依赖。输出完整文本目标1MB硬10MB，超1MB说明必要性；原失败/许可/验收证据保留。定向测试与改动相称，不重复全域Final/全仓哈希或创建一次性续行入口。原生/视觉/阶段②未验如实记录。

原cards/SEC-02.md CODE阶段，L120，方案A不变；用户本次明确批准GCM改用已验SEC01 EWSB V2 AES256-CBC/PKCS7+HMACSHA256 Encrypt-then-MAC、HKDF分域。先核真实SEC01/AI274、AI277数值规范化、Catalog现有只读API与Generated数据结构。不能以改算法为名重建key provider/减认证/把嵌入key称不可提取。
复用Catalog统一只读面，设计ICatalogSource/ScriptableObjectSource（Editor/Development）/EncryptedBlobSource（Release），任一时刻单一活动source，现有DTO/只读签名与程序集方向保持。catalog全量序列化含真实数据关系/GUID稳定Sprite注册表，解码在运行时不得依赖AssetDatabase。当前171.asset中170是SO，1是原生MissingSprite；详见AI278 generated-build-scope及AI279修正版。生产场景直引Catalog，因此只移Resources不够；构建过程用受控临时blob-backed catalog与BuildScene处理/依赖图隔离170SO，Editor/Dev和源资产保留，不永久改引用/删除搬迁源。测试真实受影响序列化关系和代码引用，不简单按路径宣称资产被排除。
新增/扩充Editor/Build/CatalogBlobBuildProcessor.cs及必要同目录辅助，指纹源在Build前准备并编译，不能在Build回调生成C#触发reload；CK01_ENCRYPTED_DATA仅单次构建参数，非Development才选密文，恢复finally路径和异常残留保护明确。生产目标StreamingAssets/data/catalog.bin本阶段只列未来路径，离线样本写本卡输出/staging，不提前创建/发布。DevTools/DataTables既有runner仅增加被明确开关启用的blob导出/回读能力，默认生产导表流程不新增执行/副作用；CODE修工具，真正生产导表由DATA后续授权卡。禁止改xlsx/CSV非空值/Generated资产/ProjectSettings/Packages/场景/字体/真实存档。
依既有Security/Persistence与数据序列化验证入口编译真实Unity引用并离线核全量字段/关系/三菜与反序列化一致、V2认证与指纹不匹配拒绝、SO与blob只读面等价、Dev/Editor策略及构建输入恢复逻辑；必要新fixture须作为既有runner的长期测试归属，不新造一次性入口。只对受影响边界跑一次最终组，不重跑全工程/旧数据发布。真实BuildReport/包内170SO排除、Player与篡改启动留ENGINE，不用离线模拟声称通过。Windows Mono为后续建议，不安装IL2CPP。
交付REPORT/changed-files/evidence/acceptance、CODE→DATA和ENGINE-HANDOFF，明确下一阶段严格导出命令/staging、场景依赖处理、生产生成物清理和原生未验项。发现无法在已批准架构/范围内保证源兼容或SO排除，正式说明/Fail不偷换仅加密敏感表的否决B方案。

精确允许范围见AI282_SCOPE.json；领取及Checkpoint带Scope。验收合同inputs/acceptance/AI-000282.json，只能以真实证据填值。全部输出到.ai-workspace/outputs/DETACH-B13-20261019/AI-000282，每项合同必须能回到实际文件/日志。

> SUPERVISED POLICY at 2026-10-20T00:44:04.8572341+08:00: {"taskId":"AI-000282","policy":{"resourceKeys":["unity-project","documentation","data-batch"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"用户call_a8f7addab9304ac69c44dc698807bff9:0明确批准阶段①；仅源码离线或只读候选，原278阶段②生产gate不批准。","updatedAt":"2026-10-20T00:44:04.7787735+08:00","supervisionLevel":"full"}}
## AI-000283 | [ENGINE_CLI] [SC] D-T2 新节点精确布局候选（只读）

- 序号：283
- 提交时间：2026-10-20T00:44:05.0071469+08:00
- 模块 ID：SC
- 初始状态：QUEUED
- 前置依赖：AI-000280, AI-000279
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：M / 80

### 指令

AI-000283 / D-T2 新节点精确布局候选（只读）

用户已直接批准窗口②阶段①及算法调整，原话/ID/SHA见inputs/派发/DETACH-B13-20261019/窗口②阶段①用户批准.json（call_a8f7addab9304ac69c44dc698807bff9:0）。只批准源码/离线与只读布局候选，生产场景、导表/Publish、原生Compile/Play/Build和A+C仍未批准。原AI278 HumanGateAfter=true、gateApproved=false保持，后继卡根据本次独立阶段①批准安排，不能把父级机器解拒验当阶段②许可。不提交推送，不复原用户dirty，不写真实存档。先CORE、对应worker、项目两入口页首及本卡相关模块；AI279修正版只按相关条读取，避免重读全部历史。固定chat复用。每批写前正式Checkpoint；FEATURE首次不计重试，正式失败先Fail回传，CONTROL根据真实1/3/5根余量决定ApproveResume，不执行器自重启。Complete/Fail/Yield立即Prepare→一次真实app发送→Record给绑定CONTROL，只有CONTROL独立验收才推进依赖。输出完整文本目标1MB硬10MB，超1MB说明必要性；原失败/许可/验收证据保留。定向测试与改动相称，不重复全域Final/全仓哈希或创建一次性续行入口。原生/视觉/阶段②未验如实记录。

本卡属于用户批准阶段①，M80，仅磁盘只读与本卡输出。AI280已独立验收后才能派，核其ENGINE-HANDOFF最新文字/隔离接口；不重新审读所有旧卡。只读AI279完整修正版相关字段、实际scene/prefab/素材元数据、现行原设计图/指导书，现有Unity6用户布局为基线。Unity CLI/MCP调用0；不做身份/Console心跳，不编辑资源，不截图真实Editor，不安装/运行旧审计脚本。
为真正新增的9个收藏星标（8卡与CurrentRecipeTab）、便签04/05、步骤Modal的10行/描述及必要图标，输出逐对象父级/候选名称/局部坐标/尺寸/间隔/排序/复用字体素材GUID-fileID/依据。用既有VerticalLayout契约推导新行布局，不改旧容器/已保存Transform/_TUNE；空间不足时明确列冲突/需用户改变的边界，不能暗改锚点或声称布局必然可用。图标容量按当前真实步骤上限/数据行和未来边界说明，不机械盲建50个。旧16标签、4食材卡、3便签、卡02选择绑定均复用，已有标签不再新增。新几何仅候选，不是假造已存在组件ID。
通用便签先核同步素材/工程已有资产身份与原设计适用性；可明确复用时列真实GUID与为何合适，不能确定时列可审选项/精确缺口，不能下载/导入/生成或用MissingSprite静默填充。新增星标用已核eea87b3c50681d34aadb55f258a7396e/21300000候选。字体/材料不改，不以查看图片触发写入或图板生成。
生成一份用户可读布局候选表/简图（仅报告内手工坐标示意，不冒充Editor截图）及机器对象清单，区分确定复用/候选新建/待设计，估算实际新节点组件数量、验证与回退。列phase2精确场景路径、每类操作和批准后ENGINE步骤、需要CODE/设计前置。若无法在保持旧Transform前提内给出可用候选，完整报告原因，不擅自生产调整。无原生验收结论/不代视觉批准。完整文本包≤1MB目标硬10MB，无新TK/测试/生产脚本。
本卡Complete只意味着候选材料就绪；HumanGateAfter=true仍未批准，CONTROL审阅后请用户集中批准阶段②，不能执行器自行创建后继。

精确允许范围见AI283_SCOPE.json；领取及Checkpoint带Scope。验收合同inputs/acceptance/AI-000283.json，只能以真实证据填值。全部输出到.ai-workspace/outputs/DETACH-B13-20261019/AI-000283，每项合同必须能回到实际文件/日志。

> SUPERVISED POLICY at 2026-10-20T00:44:05.2097889+08:00: {"taskId":"AI-000283","policy":{"resourceKeys":["unity-project","documentation","data-batch"],"reviewDependsOn":[],"humanGateAfter":true,"gateApproved":false,"reason":"用户call_a8f7addab9304ac69c44dc698807bff9:0明确批准阶段①；仅源码离线或只读候选，原278阶段②生产gate不批准。","updatedAt":"2026-10-20T00:44:05.1611311+08:00","supervisionLevel":"full"}}

> SUPERVISED EXTEND at 2026-10-20T00:44:05.3999466+08:00: {"reason":"窗口②阶段①及算法调整获用户直接批准；4张明确阶段卡，保留278与283后续人工门，不扩大至生产场景/导表/Build。","taskIds":["AI-000280","AI-000281","AI-000282","AI-000283"]}

> Verification rejected for AI-000281 at 2026-10-20T01:42:42.4885082+08:00: decision=REJECTED; reason=CONTROL实际复现：原18探针清理到None并清空菜谱，新增19/20未重建会话前置，零次load即双失败；39离线用例没有覆盖串联探针。
## AI-000284 | [CODE] [SC] P2S追加探针在旧18清理后的会话前置修复

- 序号：284
- 提交时间：2026-10-20T01:43:44.2884221+08:00
- 模块 ID：SC
- 初始状态：QUEUED
- 前置依赖：无
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：M / 80

### 指令

AI284：AI281同根P2S追加19/20探针前置修复，固定CODE，REPAIR/M80，稳定key P2S_PROBE_POST_TEARDOWN_PREREQUISITE，cycle AI281:CODE284:retry1。原281 SUCCEEDED但CONTROL正式拒验，原两次内部失败/成功日志/结果和报告均保留，不对281重新Poll/ApproveResume。用户阶段①源码/离线批准见窗口②阶段①用户批准.json；SUPERVISED默认5同根周期来源inputs/续行许可分模式_用户长期授权_20261019.json，POLICY不是一次新的人类批准。初次实际领取计本根第1修复周期，执行器不自重启。
先CORE、CODE worker与两项目入口页首及本卡模块；读outputs/control/DETACH-B13-20261019/independent-review-281.json和AI281-probe-prerequisite-repro.json。原18末尾三层ESC至None，Session.HandleStateChanged调用RecipeBook.Clear；新增RunPhase2HandoffChecks要求非空CurrentRecipe并直接StartCooking，未BeginSession/选择。CONTROL用真实交付源码+原Harness执行FSM.Teardown，再执行新增helper，隔离/adapter/inventory都具备时仍零load、两条失败。39新用例只验证adapter，未验证实际新增协程和18尾串联。
只修新增19/20自身的前置和必要清理，保留原1..18整个源字节/调用顺序/断言，不能删旧cleanup或移调用到旧18中间，不能跳过失败/硬编码PASS。从实际旧18结束的None/空RecipeBook建立会话、使用真实绑定及当前已解锁配置菜谱、经过既有API/Router并等待实际状态/镜头/层前置，再开始19/20。不得以反射直接设置生产状态或绕过原数据/隔离契约。进入失败明确报告并仅清理本段所有的临时状态；不能把不执行当成功。验证选菜不擅改生产CSV/解锁存档。
既有ShortCyclePhase2TransitionTests中增加针对实际RunPhase2HandoffChecks的定向离线回归：必须从None/菜谱被清空的真实18尾状态开始，用既有隔离Session与受控loader/fixture驱动实际协程（不是复制被测实现），证明19交接与20返回真的发生、原18源字节保留、数量守恒/所有权释放；保留未配置/不满足条件的明确失败反例及既有相关39。可最小复用runner现有CompileOnly入口；不新夹具文件/独立runner/续行入口。只必要的受影响编译/行为验证；不重跑全域Final/全仓哈希或旧数字矩阵。
精确Scope见AI284_SCOPE.json；领取/每批Checkpoint带Scope。禁止修改原281输出、Session/adapter/库存生产逻辑、Unity资源/meta/Build/ProjectSettings/数据/字体/真实存档，禁止Unity Editor/CLI/MCP/Play/Build/导表/Publish。独立API生命周期与真实场景仍未验。若窄范围确实不足先证据回报CONTROL，不自行扩权。
交付本卡REPORT、changed-files/before快照、原18保留证明、实际新增协程定向日志/失败史、evidence/acceptance及简短ENGINE补充；原281文档不覆盖，只在项目入口更正已拒验/本次修复与原生待验状态，不提升完整核验日期。合同inputs/acceptance/AI-000284.json。包目标1MB硬10MB，保留完整文本，不归档未结任务。
Complete/Fail/Yield即Prepare→一次真实Send绑定控制台01a13ef5-eda9-7882-b1c9-eca47a6470d9→Record，UNKNOWN/NO_RESEND/拒绝不重发。CONTROL独立验收并以原拒验与双方result精确SHA正式ResolveControlRejection后才能释放281。禁止自行派后继，阶段②/A+C保持，无提交推送。

> SUPERVISED POLICY at 2026-10-20T01:44:03.1378881+08:00: {"policy":{"resourceKeys":["unity-project","documentation"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"原281 CONTROL拒验的同根窄范围源码/既有离线修复，阶段②门不变。","updatedAt":"2026-10-20T01:44:03.0508380+08:00","supervisionLevel":"full"},"taskId":"AI-000284"}

> SUPERVISED EXTEND at 2026-10-20T01:44:03.4059373+08:00: {"reason":"按已批准阶段①及SUPERVISED5政策修复281追加探针真实前置缺陷，保留同根历史。","taskIds":["AI-000284"]}

> Dependency correction for AI-000282 at 2026-10-20T01:44:03.6446852+08:00: AI-000280, AI-000274, AI-000269, AI-000279, AI-000281

> Dependency correction for AI-000283 at 2026-10-20T01:44:03.8092956+08:00: AI-000280, AI-000279, AI-000281
> Control rejection resolved AI-000281 : {"at":"2026-10-20T02:05:50.1892312+08:00","decision":"CONTROL_REJECTION_RESOLVED","problemKey":"P2S_PROBE_POST_TEARDOWN_PREREQUISITE","repairTaskId":"AI-000284","controllerThreadId":"01a13ef5-eda9-7882-b1c9-eca47a6470d9","userDecision":"批准本次修复和续行，另外，对相关的续行许可次数限制进行放宽：手动模式时，每次需要人工续行不变，上限保持为1；自动模式时，允许3次的重复修复尝试后，再进行人工询问和额外次数的批准；监管模式下则默认允许5次自主重复尝试","verification":"CONTROL独立6合同/14复核及实际6协程场景13断言通过；原18字节一致，五反例明确失败，已提交prep不丢失。仅解除机器拒验，不代窗口②/A+C。","evidencePath":"D:\\GameProject\\Eat-What\\.ai-workspace\\outputs\\control\\DETACH-B13-20261019\\AI281-control-rejection-resolution.json","evidenceSha256":"5D5F5DD64C566BD9DD6A3607350E78FB78005CFEAB8930CE36C190FBFEFD16CE","rejectedResultSha256":"38B6FDE1F2420808C86B20EA1974331A8B16B3515910DD16C10E9C4AAC972293","repairResultSha256":"30FCFCAC4793BB7373A08C7567F4C49C2250801A82458DE071E105D76BF8B21B"}

> SUPERVISED RESUME_APPROVED at 2026-10-20T02:57:13.1807944+08:00: {"remaining":4,"used":0,"authorizationSource":"POLICY","cycles":["AI282:retry1"],"reserved":1,"scope":{"paths":[".ai-workspace/ENGINEERING_CONVENTIONS.md",".ai-workspace/outputs/DETACH-B13-20261019/AI-000282",".ai-workspace/toolkit/DAT.md",".ai-workspace/toolkit/SEC.md","项目整体阅读理解与推进基线_2026-08-14.md","Assets/Editor/Build","Assets/Scripts/DataBase/CookingData","Assets/Scripts/Tools/Persistence","Assets/Scripts/Tools/Security","CODEBASE_MAP.md","DevTools/DataTables","DevTools/Persistence","DevTools/Security"],"operations":["documentation-update","evidence-write","idempotence-check","offline-validation","source-edit"]},"sourceApprovalId":"call_37410ba043344e9f81b4ce3ef3472a18:0","taskId":"AI-000282","rootTaskId":"AI-000282","grantId":"1b5b11db15ff4c9b82170b59e821b677","verification":"fresh app idle revision181 saved code-idle-before-282-resume1.json; independent failure review8 passed, exact BOM native stdout parsing reproduced without business rerun; 0/5 used; original phase1 scope only"}

> SUPERVISED RESUME_APPROVED at 2026-10-20T03:05:51.3363140+08:00: {"verification":"app idle revision182; independent failure8 checks and exact PSParameterizedProperty/monoPath.Chars synthetic replay; source unedited, no business rerun; root1/5; original phase1 scope only","cycles":["AI282:retry2"],"used":1,"scope":{"paths":[".ai-workspace/ENGINEERING_CONVENTIONS.md",".ai-workspace/outputs/DETACH-B13-20261019/AI-000282",".ai-workspace/toolkit/DAT.md",".ai-workspace/toolkit/SEC.md","项目整体阅读理解与推进基线_2026-08-14.md","Assets/Editor/Build","Assets/Scripts/DataBase/CookingData","Assets/Scripts/Tools/Persistence","Assets/Scripts/Tools/Security","CODEBASE_MAP.md","DevTools/DataTables","DevTools/Persistence","DevTools/Security"],"operations":["documentation-update","evidence-write","idempotence-check","offline-validation","source-edit"]},"reserved":1,"authorizationSource":"POLICY","remaining":3,"grantId":"f12da61a1caf47eb99119de25070ef2b","taskId":"AI-000282","sourceApprovalId":"call_37410ba043344e9f81b4ce3ef3472a18:0","rootTaskId":"AI-000282"}

> Verification rejected for AI-000282 at 2026-10-20T03:20:08.2341135+08:00: decision=REJECTED; reason=CONTROL source review: build Cleanup blanket-skips unknown .meta in both file ownership and nonempty-directory guards, hands directory with foreign metadata to DeleteAsset; companion meta hashes absent. Data/crypto/report direct12 passed but cleanup contract fails.
## AI-000285 | [CODE] [SEC02] SEC02 构建清理归属修复

- 序号：285
- 提交时间：2026-10-20T03:21:06.4000188+08:00
- 模块 ID：SEC02
- 初始状态：QUEUED
- 前置依赖：无
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：M / 80

### 指令

AI-000285 / AI282构建清理归属修复（同根REPAIR，源码/隔离离线）
用户阶段①授权保持；POLICY来源call_37410ba043344e9f81b4ce3ef3472a18:0，父/根AI-000282，cycle AI282:CODE285:retry3，M80。原282已SUCCEEDED但被CONTROL正式REJECTED，不能Poll/ApproveResume重跑282；本卡成功后仍需CONTROL独立验收+ResolveControlRejection绑定精确结果。根已真实消费2/5，本卡首次claim消耗第3/5，非新增人批、不重置key或旧两失败。

必读CONTROL independent-review-282.json及AI282-CatalogBlobBuildProcessor-reviewed.txt（outputs/control/DETACH-B13-20261019），原AI282_SCOPE对应本卡更小scope、AI282 attempt3报告仅相关构建清理部分。数据/认证/三菜/报表12项CONTROL直接验证通过；7合同自报匹配，拒验仅清理归属边界。当前Cleanup行153把所有.meta跳过unknown guard，154把仅含.meta目录视为空并交DeleteAsset；Track只记主文件SHA，元数据漂移也未保护。未知foreign.asset.meta可落入删除路径是实际源码判断，不声称已实际发生Unity删除。

在原Build实现最小修复：每个创建的资产/文件/目录及其真正由本次创建的伴随.meta都有明确归属与验证依据，元数据不能凭后缀放行。先完整只读预检所有将受删除影响的路径与内容（含folder.meta、orphan.meta、未知子目录/文件、修改过的已知meta、链接/越界），发现未知/漂移就保留并报具体项，不能先删一半再失败。最终删除目录必须真正空，不能过滤.meta假装空；既有目录/未知目录不当成owned。正常Cleanup与RecoverOwnedInputs共用同一实际校验，Journal需保留真实归属、预期字节/哈希（不信任泛后缀）；旧journal缺证据应明确拒绝/保留，不猜测升级或覆盖。不要把此修复扩为场景架构/加密/数据改写。

可把纯归属/删除计划计算放入现有CatalogBuildPlan以让真实Cleanup及Recovery调用，既有Fixtures/CatalogBlobTests.cs加入有意义定向用例，必须测实际共用代码，不复制实现自证。覆盖：完整owned资产+meta/目录可清理；纯未知orphan.meta、已知.meta漂移、未知普通文件/子目录、folder.meta漂移、缺少journal归属/旧journal、路径逃逸/重定向均在首次删除前拒绝；失败保留旧报告/未知字节。目录可为空或已移除的幂等边界须明确，不能允许未知meta丢失。Fixture仅本卡输出内合成文件；若删合成文件，先核绝对目标仍在本卡目录，不递归链接目标。不得真实Unity DeleteAsset/Recover/Build/Refresh。

按2个实际源码+既有fixture/runner定向验证，真实Unity引用API表面只编译受影响面；可用既有Initialize-CatalogHost/现有编译入口，不新建专属续行脚本/夹具，不重跑已过Catalog234双组/Security305/306或全域Final/全仓SHA。仅本修改影响项与必要编译、原guard不回退。输出6合同与真实日志/改动及before证据，原AI282包/所有失败/最后成功结果SHA保持，不覆盖。复杂度M80，预算未知app轮次记unknown。

范围见AI285_SCOPE.json，合同inputs/acceptance/AI-000285.json。原生阶段②/场景/BuildReport/Player/170SO实际包内排除/A+C仍未批准；本卡不能宣称原生验证。禁止Unity/CLI/MCP/Play/Build、生产meta/场景/Generated/工作簿CSV/真实存档/导表Publish/提交推送。每批写前正式Checkpoint；本卡失败先正式结算回传，再由CONTROL用余量经ApproveResume原285续行；不能新卡/换key自行重启。成功Complete后立即Prepare→一次实际Send CONTROL→Record，停止不接后继。CODEBASE_MAP/项目基线和原工具说明仅维护必要完成度/限制，无新TK/新架构；完整核验日期不提升。整包目标1MB硬10MB，旧282证据引用不复制。

本卡关闭条件：实际guard正确且定向离线通过；CONTROL再核原282受影响合同后以ResolveControlRejection解除，AI283依然由CONTROL按全scope/锁派发。

> SUPERVISED POLICY at 2026-10-20T03:21:31.8033129+08:00: {"policy":{"resourceKeys":["unity-project","documentation","data-batch"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"原282控制拒验同根POLICY修复；只源码/隔离离线，原阶段②人工门不变。","updatedAt":"2026-10-20T03:21:31.7143884+08:00","supervisionLevel":"full"},"taskId":"AI-000285"}

> SUPERVISED EXTEND at 2026-10-20T03:21:32.0797801+08:00: {"reason":"用户已批阶段①原范围同根清理归属修复，保留282拒验与真实重试消费","taskIds":["AI-000285"]}

> Dependency correction for AI-000283 at 2026-10-20T03:21:32.3114360+08:00: AI-000280, AI-000279, AI-000281, AI-000282
> Control rejection resolved AI-000282 : {"at":"2026-10-20T03:41:15.2305150+08:00","decision":"CONTROL_REJECTION_RESOLVED","problemKey":"SEC02-CATALOG-SOURCE-BUILD-OFFLINE","repairTaskId":"AI-000285","controllerThreadId":"01a13ef5-eda9-7882-b1c9-eca47a6470d9","userDecision":"CONTROL independent machine review under approved stage1; not human/native approval.","verification":"6 contracts/15 independent checks; actual shared guard42 checks,18 negatives before first delete,6 positive pairs;9 changes/18 snapshots/12 original evidence matched. No native operations; stage2 gate remains.","evidencePath":"D:\\GameProject\\Eat-What\\.ai-workspace\\outputs\\control\\DETACH-B13-20261019\\AI282-control-rejection-resolution.json","evidenceSha256":"D9A7A3C49D3C4D27B9E57EE23ED0BB88A0BAA1ADA07D5D4634B77C2616024E9B","rejectedResultSha256":"7872C9D292150ED9120BFCFFE0D6D22B68EDFBE527DE059F3EF4EB658E74EA1A","repairResultSha256":"F7A6FB6D76065E17C74F07706A9F4EC3E5F133D1C170183531CBBA75B012FEAE"}

> Verification rejected for AI-000283 at 2026-10-20T04:04:26.2092287+08:00: decision=REJECTED; reason=Actual step_qt_04 uses prd_fried_egg third ingredient and tool_pot first tool concurrently. Candidate x=4.95 and5.45; actual1x1/PPU100 center pivot sprites times scale60 produce widths0.6 each. Bounds[4.65,5.25] and[5.15,5.75] overlap0.1x0.6 with same local y/z/order203. StepDetailRowPiece.Fill does not rescale/reposition. Seven conflict entries omit this deterministic collision; clearances only check outer Modal bounds.
## AI-000286 | [ENGINE_CLI] [D-T2] D-T2 候选图标间隙修复

- 序号：286
- 提交时间：2026-10-20T04:05:30.5191276+08:00
- 模块 ID：D-T2
- 初始状态：QUEUED
- 前置依赖：无
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：M / 80

### 指令

AI-000286 / D-T2候选图标间隙修复（同根AI283 REPAIR/POLICY，固定ENGINE_CLI，M80）
当前用户批准仍仅窗口②阶段①源码/离线与磁盘只读候选（call_a8f7addab9304ac69c44dc698807bff9:0）；本卡为原AI283独立拒验后的必要报告修复。长期SUPERVISED5来源call_37410ba043344e9f81b4ce3ef3472a18:0，cycle AI283:ENGINE286:retry1；首次FEATURE不计，本修复实际claim消耗第1/5，剩4，不是新增人批。原283已成功但CONTROL REJECTED，不能重Poll/ApproveResume283；只本卡输出，原283所有文件/正式结果不改。

先CORE/本车道相关节及CONTROL outputs/control/DETACH-B13-20261019/{independent-review-283.json,AI283-independent-candidate-check.json}；只按相关条复用原283 layout-candidates.json/layout-review.md/clearance-source.json。已核sceneSHA、46Transform字段、5Sprite元数据、18实际数据引用、10scriptGUID、49节点/124新组件+12补挂均匹配，不重跑旧16项全组或重新扫描全工程。

具体拒验：原候选step-row-04-ingredients-2 x4.95、step-row-04-tools-0 x5.45，实际各Sprite1x1/PPU100、中心pivot，scale60，故各0.6宽高，范围[4.65,5.25]和[5.15,5.75]相交0.1x0.6。真实清汤面step_qt_04同时用prd_fried_egg第三食材和tool_pot第一工具；StepDetailRowPiece.Fill不自动缩放/挪位。两节点同y/z/order203，原7冲突未记此项，外包围框检查不能证明内部间隙。是磁盘计算反例，未声称真实Editor已渲染重叠。

在原49节点候选上最小修正新增图标位置/间距（例如工具起点右移0.2为5.65；统一工具列若选此方案则其余相应列同步，可自行论证合理更小修订），不改既有Transform/_TUNE、Description/Backdrop/Close、49节点/13图标容量或生产数据。用原真Sprite rect/PPU/pivot和实际每条菜谱同行同时显示列表，复核受影响图标之间正间隙及内容外界；列修改前/后坐标、实际最小间隙与边距和哪些仅保守矩形/仍待视觉。不新增脚本/测试fixture、不建新TK、不执行原collector。只小范围数值/文档diff核验，至多必要只读素材观察，无全域Final/全仓哈希。

输出本卡独立完整修正版机器图、与之逐项相符的用户表/手工示意、受影响clearance说明及4合同；旧17件原283包只引用不改，其SHA见CONTROL既有artifactHashes，可定向证实不变。保留12MissingSprite、番茄/白菜章鱼图、placeholder scale60/混槽极小、便签排序/角标越纸边及未来容量等未决项，不静默美化/改非空值。修正根入口旧句时只说明当前CONTROL状态，不越scope改根文档。SOURCE/APIs/生产资源均只读。

严格Unity/CLI/MCP/身份心跳/Console/Refresh/Compile/Play/Build/导表/Publish/真实存档调用0；ENGINE_CLI只是固定队列身份，不因此使用引擎。输出目标1MB硬10MB；本卡报告用真实工具/额度unknown，不能把provider样本当账单。每写入批次正式Checkpoint带本卡Scope。

完整完成4合同后Complete并立即Prepare→一次真实Send绑定CONTROL→Record，停止不领后继。正式失败先结算回传，CONTROL按真实余量经唯一ApproveResume原286组织续行，不执行器自行重启/换key/chat/card清零。CONTROL独立通过后才以精确拒验/双方result SHA ResolveControlRejection283；原HumanGateAfter和阶段②/A+C仍未批，不进入生产、不提交推送。精确范围AI286_SCOPE.json，合同inputs/acceptance/AI-000286.json。

> SUPERVISED POLICY at 2026-10-20T04:05:30.9719375+08:00: {"policy":{"resourceKeys":["unity-project","documentation"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"Stage1 authorized same-root disk-only candidate repair; original283 human gate remains, no native operations.","updatedAt":"2026-10-20T04:05:30.9114069+08:00","supervisionLevel":"full"},"taskId":"AI-000286"}

> SUPERVISED EXTEND at 2026-10-20T04:05:31.0812448+08:00: {"reason":"Original283 CONTROL rejection DT2_CANDIDATE_ICON_CLEARANCE; authorized phase1 same-root POLICY repair cycle1.","taskIds":["AI-000286"]}
> Control rejection resolved AI-000283 : {"at":"2026-10-20T04:14:07.6593109+08:00","decision":"CONTROL_REJECTION_RESOLVED","problemKey":"DT2_CANDIDATE_ICON_CLEARANCE","repairTaskId":"AI-000286","controllerThreadId":"01a13ef5-eda9-7882-b1c9-eca47a6470d9","userDecision":"CONTROL independent machine acceptance under approved phase1; no human layout/window approval.","verification":"Four contracts/ten bounded independent checks. Exact7 x positions;11 actual steps/22 simultaneous pairs min0.1;outer8.75/margin2.324/cell0.45. Original17 artifacts and scene unchanged. No native/visual/phase2 approval.","evidencePath":"D:\\GameProject\\Eat-What\\.ai-workspace\\outputs\\control\\DETACH-B13-20261019\\AI283-control-rejection-resolution.json","evidenceSha256":"B7D4E63E888658D5A0D98E7AB4D5F302D2C78CA850F58923231668251466532C","rejectedResultSha256":"8842B4A6EE29A2D56A1EB6655E6EF06372650BF0ED5B6320513273D6A6B76891","repairResultSha256":"ECD7ECBB8BA390A43DFC6BDB0E5E9095E18B48159DDD04AAD94EB882CD1595E1"}
## AI-000287 | [CODE] [SC] 窗口②A 显式Editor/Player存档隔离宿主（源码）

- 序号：287
- 提交时间：2026-10-20T14:00:16.9852313+08:00
- 模块 ID：SC
- 初始状态：QUEUED
- 前置依赖：AI-000280, AI-000281, AI-000284, AI-000282, AI-000285, AI-000278, AI-000283, AI-000286
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：M / 80

### 指令

AI-000287 / 窗口②A 显式Editor/Player存档隔离宿主（源码）

用户已批准窗口②阶段②A及A通过后B。唯一原件 inputs/派发/DETACH-B13-20261019/窗口②阶段②用户批准.json，A=call_86b591ca373e4267a15ff68639d064c1:0，B=:1；审定范围/逐对象表/SHA索引见 outputs/control/DETACH-B13-20261019/窗口②阶段②控制审查单.md（按本卡节读取）。固定chat/同session/共享unity-project互斥。先CORE、对应worker、两项目入口页首/本模块；只凭本卡票据领取一次，带本卡Scope，每批写前Checkpoint。首次FEATURE不计重试；正式失败结算回传，CONTROL再按同根SUPERVISED5/实际余量经ApproveResume，执行器不自行重启或换工具。120软/240临时硬限保持，unknown额度不猜。
Complete/Fail/Yield立即Prepare→一次真实app Send→Record到绑定CONTROL；只有CONTROL独立验收才派后继。平台拒绝/未知写入/用户stop保持，非空配表、真实用户存档、IL2CPP安装、批次2/波次C、提交推送均不在范围。完整文本报告目标1MB/硬10MB，超1MB说明；失败/许可/history保留，不以拆包绕限。A+C视觉/USER-U5仍待人工，不把执行批准作产品验收。旧已过业务不重跑；定向验证与改动相称，不全仓/纹理JSON/内存保护。


本卡只实现审查单§三的具体宿主/启动与释放，不执行Unity、CLI/MCP、场景/原生Compile/Play/Build或生产数据。AI280 API已验，读其ENGINE-HANDOFF.json（39新增断言是准确数）、当前ShortCycleSaveIsolation/Session Awake真实代码；原AI284真实追加19/20及原1～18保持。不要重做隔离API/codec/key/两保存点/本地化。
优先复用既有长期验证体系，允许新增Debug/ShortCycleValidationBootstrap.cs与Editor/ProjectValidation/ShortCycleValidationLauncher.cs，必要时仅接既有ShortCycleAutomatedProbe.cs启动清理。SceneValidationSession旧入口含历史字体SHA/备份，不直接复用其旧MIG验证路径或重启旧工具；保留旧类，采用本次明确入口。现Editor.ProjectValidation已引用ShortCycle/Toolkit，不新增asmdef架构。新源码无需手写meta。
Editor和受控Release/Dev Player都需明确启用且默认关闭；核真实Unity6000.3 API。ForTarget在BeforeSceneLoad精确唯一未Awake目标，或持久空宿主ForScene→同步加载；sceneLoaded太晚。关闭Domain/Scene Reload时不能复用已Awake对象；不靠ExecutionOrder猜安全。目标固定Assets/Scenes/Cooking/ShortCycle_P0P1.unity及Managers/SessionManager，绝对隔离根只在获批任务outputs/DETACH-B13-20261019/AI-xxxxxx/isolated-saves/<run>/，绑定本次任务/运行标识。拒绝路径越界、根目录、重定向与目标错配，不枚举真实persistentDataPath。无显式启用时不改变生产默认行为；显式启用但参数错误、目标错误/重复、晚安装、取消/异常必须阻断生产fallback，不能只在RuntimeInitialize抛异常后让Awake继续使用默认factory。原API是复用边界，若做不到则真实报告缺口，不暗改API/生产逻辑。
租约覆盖完整生命周期，先目标销毁/卸载再释放；正常、异常、取消、退出Play、域重载/禁用重载保持可解释且无泄漏。启动/关闭须可由ENGINE确切操作，交付可核目标身份、启用状态、isolated RootPath、写入归属及未调用默认factory的证据路径。Player需显式受控入口/参数及失败关闭行为，不能伪造Unity内置-persistentDataPath标志或持久改全局Define/环境；验收开关不能影响普通发行默认。
离线复用DevTools/ShortCycle的既有runner/stubs/IsolationAndTextTests，按本次宿主分支做真实行为测试并编译实际UnityEditor/UnityEngine API表面；测试调用交付宿主，不能复制实现假测。覆盖未启用默认不变、正确早安装、晚/错/重复/无效root拒绝且不回生产、生命周期释放、Editor/Player启用入口与实际1～20探针驱动兼容。离线不代native。只受影响范围Final，原Security/数字矩阵不重跑。交付ENGINE-HANDOFF.json：精确API/menu/CLI调用参数、Editor/Player安装顺序、退场/恢复/异常、后续每张任务独立目录、未验原生项。更新实际入口说明，复用TK-DAT-09与既有ShortCycle验证族，不新建平行工具。


本卡M，FEATURE。输出只在.ai-workspace/outputs/DETACH-B13-20261019/AI-000287。精确Scope=AI287_SCOPE.json，合同inputs/acceptance/AI-000287.json，真实evidence.json逐项可追溯。实际耗时/原始日志/变更路径及前后状态写报告；CODE不生成meta，ENGINE经Editor生成并记录归属。

> SUPERVISED POLICY at 2026-10-20T14:00:17.5300944+08:00: {"policy":{"resourceKeys":["unity-project","documentation","data-batch"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"用户阶段②A/B明确批准，B依赖CONTROL实际A验收；本卡只执行审定阶段。AI292保留最终A+C人工门，旧错误与消费不变。","updatedAt":"2026-10-20T14:00:17.4575533+08:00","supervisionLevel":"full"},"taskId":"AI-000287"}
## AI-000288 | [ENGINE_CLI] [SC] D-T2 原生接线与隔离前置（联合探针后置P2S）

- 序号：288
- 提交时间：2026-10-20T14:00:17.6295828+08:00
- 模块 ID：SC
- 初始状态：QUEUED
- 前置依赖：AI-000287, AI-000279, AI-000280, AI-000282, AI-000285, AI-000283, AI-000286
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：M / 80

### 指令

AI-000288 / D-T2 原生接线与隔离前置（联合探针后置P2S）

用户已批准窗口②阶段②A及A通过后B。唯一原件 inputs/派发/DETACH-B13-20261019/窗口②阶段②用户批准.json，A=call_86b591ca373e4267a15ff68639d064c1:0，B=:1；审定范围/逐对象表/SHA索引见 outputs/control/DETACH-B13-20261019/窗口②阶段②控制审查单.md（按本卡节读取）。固定chat/同session/共享unity-project互斥。先CORE、对应worker、两项目入口页首/本模块；只凭本卡票据领取一次，带本卡Scope，每批写前Checkpoint。首次FEATURE不计重试；正式失败结算回传，CONTROL再按同根SUPERVISED5/实际余量经ApproveResume，执行器不自行重启或换工具。120软/240临时硬限保持，unknown额度不猜。
Complete/Fail/Yield立即Prepare→一次真实app Send→Record到绑定CONTROL；只有CONTROL独立验收才派后继。平台拒绝/未知写入/用户stop保持，非空配表、真实用户存档、IL2CPP安装、批次2/波次C、提交推送均不在范围。完整文本报告目标1MB/硬10MB，超1MB说明；失败/许可/history保留，不以拆包绕限。A+C视觉/USER-U5仍待人工，不把执行批准作产品验收。旧已过业务不重跑；定向验证与改动相称，不全仓/纹理JSON/内存保护。


仅在CONTROL独立通过287后派发，先读287 ENGINE-HANDOFF及审查单§二/三/六。本卡负责D-T2真实接线与隔离原生前置；完整原1～20探针统一由后继289在P2空壳建好后执行，避免先后重复整组。本卡成功不独立宣称原D-T2完整运行验收。
先既有官方CLI核项目/版本/实例与磁盘/Editor未保存状态；正常权限升级，不换MCP重试。只用现行CLI/已登记验证入口，经Editor API修改保存，不手改YAML。保护用户dirty；先保存本次允许范围的恢复点到DevTools/Scenes/backup/D-T2_pre/AI-000288，不覆盖旧备份。原本已改变的源代码/资产不恢复。
按AI279 D-T2-approval-bindings.json + AI286 layout-candidates.json/layout-review.md接线，原scene SHA以审查单及当前实况核漂移；复用16标签LocalizedText/4食材卡/3便签/卡02绑定1859361184，49新GO/124新节点组件+12补挂总136。每个新对象实际fileID与候选映射留档，旧Transform/_TUNE/排序/字体及Prefab源不变，新节点按已批几何/Sorting。七工具x5.65/6.35修正版，不再用原283。新TMP先fontSharedMaterial再font，现Static/材质资产不改；9空/5MissingSprite和12/18缺图/番茄白菜章鱼图保持真实，零非空配表修改/导入。只引用现有素材。
允许原生Refresh/Compile已独立通过阶段①及287源码；编译前列本次确需生成的.cs.meta（由Editor生成，附属已批准源码），不是任意源码/资源改写许可。若发现范围外未验证源码、用户不明变更或未知资源写入先保全反馈。以287宿主完成真实pre-Awake隔离前置，显式本卡isolated-saves根且默认factory不被调用，核正常关闭/取消/目标错配原生边界；任何Play必须先满足该入口，真实用户目录不可访问。涉及预期反例的拒绝日志与普通Console错误分开，不假零错误。
本卡记录实际内容/引用、旧布局字段diff0、新增表匹配与Console；提供P0_Catalog/Browse/StepDetail可见内容截图或明确将需要P2后的联合运行证据后置289。不重跑历史Unity2022审计工具或全仓哈希，不能用旧v2报告冒充当前。临时验收组件按G26记录/移除，最后场景留审定已保存成果、恢复自身临时Play状态。若新本地化缺字交DATA，不自改字体。交付NATIVE-ISOLATION.json、binding-map、REPORT/evidence、给289的精确当前状态与未验项。A级最终放行需288与289共同CONTROL独立通过。


本卡M，FEATURE。输出只在.ai-workspace/outputs/DETACH-B13-20261019/AI-000288。精确Scope=AI288_SCOPE.json，合同inputs/acceptance/AI-000288.json，真实evidence.json逐项可追溯。实际耗时/原始日志/变更路径及前后状态写报告；CODE不生成meta，ENGINE经Editor生成并记录归属。

> SUPERVISED POLICY at 2026-10-20T14:00:17.8435102+08:00: {"policy":{"resourceKeys":["unity-project","documentation","data-batch"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"用户阶段②A/B明确批准，B依赖CONTROL实际A验收；本卡只执行审定阶段。AI292保留最终A+C人工门，旧错误与消费不变。","updatedAt":"2026-10-20T14:00:17.7783158+08:00","supervisionLevel":"full"},"taskId":"AI-000288"}
## AI-000289 | [ENGINE_CLI] [SC] P2S 空壳与A阶段原生联合验证

- 序号：289
- 提交时间：2026-10-20T14:00:17.9446827+08:00
- 模块 ID：SC
- 初始状态：QUEUED
- 前置依赖：AI-000288, AI-000287, AI-000281, AI-000284, AI-000273
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：M / 80

### 指令

AI-000289 / P2S 空壳与A阶段原生联合验证

用户已批准窗口②阶段②A及A通过后B。唯一原件 inputs/派发/DETACH-B13-20261019/窗口②阶段②用户批准.json，A=call_86b591ca373e4267a15ff68639d064c1:0，B=:1；审定范围/逐对象表/SHA索引见 outputs/control/DETACH-B13-20261019/窗口②阶段②控制审查单.md（按本卡节读取）。固定chat/同session/共享unity-project互斥。先CORE、对应worker、两项目入口页首/本模块；只凭本卡票据领取一次，带本卡Scope，每批写前Checkpoint。首次FEATURE不计重试；正式失败结算回传，CONTROL再按同根SUPERVISED5/实际余量经ApproveResume，执行器不自行重启或换工具。120软/240临时硬限保持，unknown额度不猜。
Complete/Fail/Yield立即Prepare→一次真实app Send→Record到绑定CONTROL；只有CONTROL独立验收才派后继。平台拒绝/未知写入/用户stop保持，非空配表、真实用户存档、IL2CPP安装、批次2/波次C、提交推送均不在范围。完整文本报告目标1MB/硬10MB，超1MB说明；失败/许可/history保留，不以拆包绕限。A+C视觉/USER-U5仍待人工，不把执行批准作产品验收。旧已过业务不重跑；定向验证与改动相称，不全仓/纹理JSON/内存保护。


CONTROL独立通过288后才派发；沿用用户原P2S M80（本卡涉及已有/新增场景，较GOV默认L差异据原卡保留）。先读审查单§三/四/六、P2指导书v0.1.1§二/三、281/284交付及287/288隔离证据。每次新的运行用本卡显式隔离目录，不沿用已释放租约，不访问真实用户存档。
Editor API新建Assets/Scenes/ShortCycle_P2.unity空壳及meta，Build保留0/1仅追加2；P0P1 viewTable原行/枚举不变只增Phase2Transition=5，收尾P1所有Set/线索板。严格按指导书空Managers/Group/Cam_P2树，现Phase2SceneEntry/Phase2=3和InventoryManager单一所有权；无波次C内容/四未来P2动作/厨具分区美术。未来S0/S1/S2a/S2b矩阵超实际代码表达能力时如实列未落地，不伪造新enum或把空壳当交互完成。
真实原1～20探针一次联合运行：原1～18保持，按284由None/空RecipeBook重建会话/选菜/镜头层后实际19/20；核Additive加载/consume/unload各目标、prep/recipe守恒、隐藏、返回P1与owner释放。D-T2实际收藏/换菜/LocalizedText/步骤按原探针核实，12MissingSprite等既有缺口原样报告，不声称三菜完整可玩。交付P0_Catalog/P0_Browse/P0_StepDetail三张实际截图与A+C四列表，补齐288后置运行证据；没有实测就是待验不能填true。正常Console新增Error0，预期拒绝反例单列。自己的临时状态按G26清理，未知dirty不覆盖，保留已审接线/空壳。
A最终CONTROL独立审288+289后写A-stage-native-acceptance.json，确认前290/B不得派发。A+C视觉仍待用户，但本次B条件是A隔离/场景机器验证真实通过，不把机器证据填作A+C。最终人工门统一保留于292，不全局Defer。报告标明完整D-T2/P2原生达成与残余产品待办。


本卡M，FEATURE。输出只在.ai-workspace/outputs/DETACH-B13-20261019/AI-000289。精确Scope=AI289_SCOPE.json，合同inputs/acceptance/AI-000289.json，真实evidence.json逐项可追溯。实际耗时/原始日志/变更路径及前后状态写报告；CODE不生成meta，ENGINE经Editor生成并记录归属。

> SUPERVISED POLICY at 2026-10-20T14:00:18.2048511+08:00: {"policy":{"resourceKeys":["unity-project","documentation","data-batch"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"用户阶段②A/B明确批准，B依赖CONTROL实际A验收；本卡只执行审定阶段。AI292保留最终A+C人工门，旧错误与消费不变。","updatedAt":"2026-10-20T14:00:18.1294793+08:00","supervisionLevel":"full"},"taskId":"AI-000289"}
## AI-000290 | [ENGINE_CLI] [SC] SEC02 B阶段当前Catalog原生快照交DATA

- 序号：290
- 提交时间：2026-10-20T14:00:18.2949158+08:00
- 模块 ID：SC
- 初始状态：QUEUED
- 前置依赖：AI-000289, AI-000288, AI-000287, AI-000282, AI-000285
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：S / 40

### 指令

AI-000290 / SEC02 B阶段当前Catalog原生快照交DATA

用户已批准窗口②阶段②A及A通过后B。唯一原件 inputs/派发/DETACH-B13-20261019/窗口②阶段②用户批准.json，A=call_86b591ca373e4267a15ff68639d064c1:0，B=:1；审定范围/逐对象表/SHA索引见 outputs/control/DETACH-B13-20261019/窗口②阶段②控制审查单.md（按本卡节读取）。固定chat/同session/共享unity-project互斥。先CORE、对应worker、两项目入口页首/本模块；只凭本卡票据领取一次，带本卡Scope，每批写前Checkpoint。首次FEATURE不计重试；正式失败结算回传，CONTROL再按同根SUPERVISED5/实际余量经ApproveResume，执行器不自行重启或换工具。120软/240临时硬限保持，unknown额度不猜。
Complete/Fail/Yield立即Prepare→一次真实app Send→Record到绑定CONTROL；只有CONTROL独立验收才派后继。平台拒绝/未知写入/用户stop保持，非空配表、真实用户存档、IL2CPP安装、批次2/波次C、提交推送均不在范围。完整文本报告目标1MB/硬10MB，超1MB说明；失败/许可/history保留，不以拆包绕限。A+C视觉/USER-U5仍待人工，不把执行批准作产品验收。旧已过业务不重跑；定向验证与改动相称，不全仓/纹理JSON/内存保护。


B仅在CONTROL独立288/289并落盘A-stage-native-acceptance.json为PASSED后可派；不存在或失败就不执行业务，回CONTROL。此卡是跨ENGINE→DATA所有权交接，不是再次导表。读取审查单§五及现PrepareSnapshot实现，核目标Unity/版本/空闲/clean，不改变场景/生产数据。
用既有CatalogBlobBuildProcessor.PrepareSnapshot仅捕获当前Catalog、169数据SO/14表及Sprite精确GUID-fileID并导出到本卡fresh staging（已有同名/未知项拒绝）；记录来源清单、实际指纹、文件SHA、capture前后真实源状态。PrepareSnapshot内部已有认证封装/回读，不将其当DATA环节已完成；实际API行为如实报告，不另造导出工具。输出catalog.graph.json/catalog.bin/CatalogBuildFingerprint.g.cs/export.json及DATA-HANDOFF，全部task outputs。当前元数据基线变化需解释，不能复用旧离线合成图。
不写Assets/StreamingAssets、生产Fingerprint源码、Build/Play或xlsx/CSV/Generated，不Publish、不访问用户存档；只取当前真实快照与磁盘证据。DATA291将对同一真实graph在其独立staging显式opt-in导出，ENGINE292最后核当前Catalog仍与快照一致再Build，漂移不静默重采。


本卡S，FEATURE。输出只在.ai-workspace/outputs/DETACH-B13-20261019/AI-000290。精确Scope=AI290_SCOPE.json，合同inputs/acceptance/AI-000290.json，真实evidence.json逐项可追溯。实际耗时/原始日志/变更路径及前后状态写报告；CODE不生成meta，ENGINE经Editor生成并记录归属。

> SUPERVISED POLICY at 2026-10-20T14:00:18.5190021+08:00: {"policy":{"resourceKeys":["unity-project","documentation","data-batch"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"用户阶段②A/B明确批准，B依赖CONTROL实际A验收；本卡只执行审定阶段。AI292保留最终A+C人工门，旧错误与消费不变。","updatedAt":"2026-10-20T14:00:18.4473943+08:00","supervisionLevel":"full"},"taskId":"AI-000290"}
## AI-000291 | [DATA] [SC] SEC02 B阶段DATA显式隔离blob导出与回读

- 序号：291
- 提交时间：2026-10-20T14:00:18.6017156+08:00
- 模块 ID：SC
- 初始状态：QUEUED
- 前置依赖：AI-000290, AI-000289, AI-000288, AI-000282, AI-000285
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：S / 40

### 指令

AI-000291 / SEC02 B阶段DATA显式隔离blob导出与回读

用户已批准窗口②阶段②A及A通过后B。唯一原件 inputs/派发/DETACH-B13-20261019/窗口②阶段②用户批准.json，A=call_86b591ca373e4267a15ff68639d064c1:0，B=:1；审定范围/逐对象表/SHA索引见 outputs/control/DETACH-B13-20261019/窗口②阶段②控制审查单.md（按本卡节读取）。固定chat/同session/共享unity-project互斥。先CORE、对应worker、两项目入口页首/本模块；只凭本卡票据领取一次，带本卡Scope，每批写前Checkpoint。首次FEATURE不计重试；正式失败结算回传，CONTROL再按同根SUPERVISED5/实际余量经ApproveResume，执行器不自行重启或换工具。120软/240临时硬限保持，unknown额度不猜。
Complete/Fail/Yield立即Prepare→一次真实app Send→Record到绑定CONTROL；只有CONTROL独立验收才派后继。平台拒绝/未知写入/用户stop保持，非空配表、真实用户存档、IL2CPP安装、批次2/波次C、提交推送均不在范围。完整文本报告目标1MB/硬10MB，超1MB说明；失败/许可/history保留，不以拆包绕限。A+C视觉/USER-U5仍待人工，不把执行批准作产品验收。旧已过业务不重跑；定向验证与改动相称，不全仓/纹理JSON/内存保护。


B真实人批见阶段②原件，不消费或伪造新的USER-U1 DONE轮次。仅在CONTROL独立A通过及290实际快照验收后派；不重新CurrentCsvValidation/Excel规范化/普通导表/生成物Publish。读取290 DATA-HANDOFF、graph与SHA，核来源不是离线夹具，当前相符由ENGINE后继Build前再次核。
既有DevTools/DataTables/Run-DataPipeline.ps1 -ExportCatalogBlob -CatalogSnapshotPath <290真实graph> -CatalogBlobStaging <本卡fresh staging>，核此参数集原有拒绝Publish/PlanOnly/字体操作机制，不改工具。只创建本卡四件catalog.graph.json/catalog.bin/CatalogBuildFingerprint.g.cs/export.json，认证回读与指纹/完整数据及Sprite身份一致。已有输出/漂移拒绝不覆盖，保留实际调用及退出码/最小日志。无需Unity授权操作，不开Editor/Play/Build，不读写真实存档、xlsx/CSV/Generated/字体/非空单元格。
交付BUILD-HANDOFF：精确fresh路径、文件SHA、指纹、真实origin任务/采集时间/版本、导出与回读证据；native包内排除/Player仍待292。失败正式Fail回传后CONTROL决定同根续行，不擅自用别的参数集或先Publish修正。


本卡S，FEATURE。输出只在.ai-workspace/outputs/DETACH-B13-20261019/AI-000291。精确Scope=AI291_SCOPE.json，合同inputs/acceptance/AI-000291.json，真实evidence.json逐项可追溯。实际耗时/原始日志/变更路径及前后状态写报告；CODE不生成meta，ENGINE经Editor生成并记录归属。

> SUPERVISED POLICY at 2026-10-20T14:00:18.8326429+08:00: {"policy":{"resourceKeys":["unity-project","documentation","data-batch"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"用户阶段②A/B明确批准，B依赖CONTROL实际A验收；本卡只执行审定阶段。AI292保留最终A+C人工门，旧错误与消费不变。","updatedAt":"2026-10-20T14:00:18.7557359+08:00","supervisionLevel":"full"},"taskId":"AI-000291"}
## AI-000292 | [ENGINE_CLI] [SC] SEC02 Windows Mono本地构建与隔离Player验收

- 序号：292
- 提交时间：2026-10-20T14:00:49.4342761+08:00
- 模块 ID：SC
- 初始状态：QUEUED
- 前置依赖：AI-000291, AI-000290, AI-000289, AI-000288, AI-000287, AI-000282, AI-000285
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：L / 120

### 指令

AI-000292 / SEC02 Windows Mono本地构建与隔离Player验收

用户已批准窗口②阶段②A及A通过后B。唯一原件 inputs/派发/DETACH-B13-20261019/窗口②阶段②用户批准.json，A=call_86b591ca373e4267a15ff68639d064c1:0，B=:1；审定范围/逐对象表/SHA索引见 outputs/control/DETACH-B13-20261019/窗口②阶段②控制审查单.md（按本卡节读取）。固定chat/同session/共享unity-project互斥。先CORE、对应worker、两项目入口页首/本模块；只凭本卡票据领取一次，带本卡Scope，每批写前Checkpoint。首次FEATURE不计重试；正式失败结算回传，CONTROL再按同根SUPERVISED5/实际余量经ApproveResume，执行器不自行重启或换工具。120软/240临时硬限保持，unknown额度不猜。
Complete/Fail/Yield立即Prepare→一次真实app Send→Record到绑定CONTROL；只有CONTROL独立验收才派后继。平台拒绝/未知写入/用户stop保持，非空配表、真实用户存档、IL2CPP安装、批次2/波次C、提交推送均不在范围。完整文本报告目标1MB/硬10MB，超1MB说明；失败/许可/history保留，不以拆包绕限。A+C视觉/USER-U5仍待人工，不把执行批准作产品验收。旧已过业务不重跑；定向验证与改动相称，不全仓/纹理JSON/内存保护。


仅在CONTROL独立A/290/291通过后派发。读审查单§三/五/六、287宿主、291 BUILD-HANDOFF、282/285真实处理器。该卡L120，Build与实际包体验收；A+C人工门HumanGateAfter=true，只用户实际视觉/产品验收才可批准，不能据B执行授权代验。
核实时项目/6000.3.24f1/实例/Windows x64 Mono模块，缺模块停报、不安装IL2CPP。当前Catalog真实指纹须与290/291一致，无漂移才将291生成指纹精确更新到Assets/Scripts/DataBase/CookingData/Build/CatalogBuildFingerprint.g.cs（前版保全、diff限定），Editor导入编译完成后才BuildPrepared，不在回调重生成或触发reload。Build列表沿A的0/1/2；只本次Build extraScriptingDefines CK01_ENCRYPTED_DATA，Player验收宿主如需显式开关按287实际说明与批准边界，不持久改ProjectSettings、全局环境或普通生产默认。
Release/Dev各一次，输出本卡release/dev。Release临时Assets/__CatalogBlobBuild_<32hex>、StreamingAssets/data/catalog.bin与伴随meta按AI285 schema2完整归属/预检/期望SHA，场景副本remap既有4处直引；保留真实源scene及Generated SO，不手改YAML。已有同名/未知meta/漂移/链接/未结journal拒绝并保全，不强制Cleanup/Recovery；只删本次完整归属pair和真正空目录。具体临时资产/日志清单先记，Library/CatalogBlobBuild.journal.json生命周期可核。
真实BuildReport和包体证170 Generated SO排除、blob存在、13已知Sprite身份及原生MissingSprite保留（按当前真实图核数），单ICatalogSource，三菜数据基线一致；不把数据读取正确说成三菜完整可玩。Release/Dev Player全部通过287显式隔离本卡根先于Awake，正常路径拒绝访问用户存档，再运行相关原探针/回读；无隔离证明禁止启动。只在本卡构建副本篡改blob一字节并单次启动负例，明确报错拒绝且不崩溃，与正常Console Error0分开。记录实际用时，可信增量无基线则unknown不额外重复Build。
保留Dev/Editor SO路径行为；结束核临时归属清理/源diff/Console/资源释放，无用户成果覆盖。输出文本报告/BuildReport提取/身份与认证/隔离日志/三菜结果/坏blob副本失败证据/必要截图及人工A+C四列表。大可再生Build作为活动验证产物留本卡，最终归档前按1MB目标/10MB硬限精简，不能分包或外移绕限。成功仅机器验收，CONTROL复核后停最终人工门并暂停WATCH，不自动提交推送或MANUAL。


本卡L，FEATURE。输出只在.ai-workspace/outputs/DETACH-B13-20261019/AI-000292。精确Scope=AI292_SCOPE.json，合同inputs/acceptance/AI-000292.json，真实evidence.json逐项可追溯。实际耗时/原始日志/变更路径及前后状态写报告；CODE不生成meta，ENGINE经Editor生成并记录归属。

Scope补充：queue只支持字面祖先，不支持通配符；Assets根仅承载已获批处理器运行内随机生成的Assets/__CatalogBlobBuild_<32hex>及其meta、本卡明确StreamingAssets输入与Fingerprint精确文件，绝不授权其它Assets写入。Build前核原处理器源码/允许模式，实际路径由其新建前日志和归属清单绑定；scope字面宽度不能扩大业务许可，也不能被续行借作任意资产范围。原生未知写入仍停。

> SUPERVISED POLICY at 2026-10-20T14:00:49.8881623+08:00: {"policy":{"resourceKeys":["unity-project","documentation","data-batch"],"reviewDependsOn":[],"humanGateAfter":true,"gateApproved":false,"reason":"用户已批阶段②B，依赖CONTROL实际A验收；本卡保留最终A+C人工视觉/产品门，执行批准不代验。","updatedAt":"2026-10-20T14:00:49.8349029+08:00","supervisionLevel":"full"},"taskId":"AI-000292"}

> SUPERVISED EXTEND at 2026-10-20T14:00:50.1986928+08:00: {"taskIds":["AI-000287","AI-000288","AI-000289","AI-000290","AI-000291","AI-000292"],"reason":"用户call_86b591ca373e4267a15ff68639d064c1:0/1批准阶段②A及A实际通过后的B；六张跨线可验收交付阶段，先隔离宿主，B依赖实际A独立验收，最终A+C人工门保留。"}

> SUPERVISED USER_GATE_APPROVED at 2026-10-20T14:00:50.3215035+08:00: {"userDecision":"用户2026-10-20 call_86b591ca373e4267a15ff68639d064c1:0 原话：批准 A，按审查单执行（推荐）；:1 原话：批准 B，在 A 通过后执行（推荐）。原件inputs/派发/DETACH-B13-20261019/窗口②阶段②用户批准.json绑定审查单SHA B8E95DC2FA8D1933C8DD60EFF2934DC650852CDD505F658263AEA20DBF24A2D5。本门只批准该审查单阶段②执行及布局选择，B条件不变；不代表A+C视觉/USER-U5验收，最终人工门保留AI292。","taskId":"AI-000278"}

> SUPERVISED USER_GATE_APPROVED at 2026-10-20T14:00:50.6567926+08:00: {"userDecision":"用户2026-10-20 call_86b591ca373e4267a15ff68639d064c1:0 原话：批准 A，按审查单执行（推荐）；:1 原话：批准 B，在 A 通过后执行（推荐）。原件inputs/派发/DETACH-B13-20261019/窗口②阶段②用户批准.json绑定审查单SHA B8E95DC2FA8D1933C8DD60EFF2934DC650852CDD505F658263AEA20DBF24A2D5。本门只批准该审查单阶段②执行及布局选择，B条件不变；不代表A+C视觉/USER-U5验收，最终人工门保留AI292。","taskId":"AI-000283"}

> SUPERVISED RESUME_APPROVED at 2026-10-20T15:33:21.9432736+08:00: {"grantId":"9bfcb2fefa654d2b91486071c855f346","taskId":"AI-000288","remaining":4,"rootTaskId":"AI-000288","reserved":1,"verification":"fresh app idle/completed revision47; independent-failure-review-288-attempt1.json10 checks confirm failure/cleanup, scene+meta equal restore point, no native Play, exact two-line payload repair. Same original scope; detailed next execution inputs/派发/DETACH-B13-20261019/AI288_ENGINE续行1.txt; preserve first attempt and unique backup. Root0/5 before reserve.","sourceApprovalId":"call_37410ba043344e9f81b4ce3ef3472a18:0","used":0,"cycles":["AI288:retry1"],"authorizationSource":"POLICY","scope":{"paths":[".ai-workspace/codebase/shortcycle.md",".ai-workspace/outputs/DETACH-B13-20261019/AI-000288","项目整体阅读理解与推进基线_2026-08-14.md","Assets/Editor/Build","Assets/Editor/ProjectValidation","Assets/Scenes/Cooking/ShortCycle_P0P1.unity","Assets/Scripts/Cooking/ShortCycle","Assets/Scripts/Core","Assets/Scripts/DataBase/CookingData","Assets/Scripts/Tools/Persistence","Assets/Scripts/Tools/Security","CODEBASE_MAP.md","DevTools/Scenes/backup/D-T2_pre/AI-000288"],"operations":["documentation-update","evidence-write","idempotence-check","unity-approved-new-node-create","unity-approved-source-import-compile","unity-isolated-play-validation","unity-owned-source-meta-create","unity-owned-temporary-cleanup","unity-scene-bindings","unity-target-check"]}}

> SUPERVISED RESUME_APPROVED at 2026-10-20T15:58:50.2519942+08:00: {"sourceApprovalId":"call_37410ba043344e9f81b4ce3ef3472a18:0","reserved":1,"taskId":"AI-000288","used":1,"authorizationSource":"POLICY","remaining":3,"cycles":["AI288:retry2"],"scope":{"paths":[".ai-workspace/codebase/shortcycle.md",".ai-workspace/outputs/DETACH-B13-20261019/AI-000288","项目整体阅读理解与推进基线_2026-08-14.md","Assets/Editor/Build","Assets/Editor/ProjectValidation","Assets/Scenes/Cooking/ShortCycle_P0P1.unity","Assets/Scripts/Cooking/ShortCycle","Assets/Scripts/Core","Assets/Scripts/DataBase/CookingData","Assets/Scripts/Tools/Persistence","Assets/Scripts/Tools/Security","CODEBASE_MAP.md","DevTools/Scenes/backup/D-T2_pre/AI-000288"],"operations":["documentation-update","evidence-write","idempotence-check","unity-approved-new-node-create","unity-approved-source-import-compile","unity-isolated-play-validation","unity-owned-source-meta-create","unity-owned-temporary-cleanup","unity-scene-bindings","unity-target-check"]},"rootTaskId":"AI-000288","grantId":"c4b74b7fffc54c61aae3c4560e55f1bd","verification":"Fresh app idle revision49/latest completed; independent-failure-review-288-attempt2.json 10 checks; same original scope; preserve saved49 nodes; only10 owned Description anchors and remaining original native isolation; known unchanged-color TMP cache proven. Supplement inputs/派发/DETACH-B13-20261019/AI288_ENGINE续行2.txt. No new human approval; POLICY source verified."}
## AI-000293 | [CODE] [SC] AI288启动取消恢复的同根源码修复

- 序号：293
- 提交时间：2026-10-20T16:21:48.0593885+08:00
- 模块 ID：SC
- 初始状态：QUEUED
- 前置依赖：无
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：M / 80

### 指令

AI-000293 / AI288 同根启动取消恢复修复（固定 CODE，M80，REPAIR）

来源：用户已批准窗口②阶段②A及A通过后的B，原件inputs/派发/DETACH-B13-20261019/窗口②阶段②用户批准.json；本次修复属于A隔离宿主原范围。根AI288 attempt3正式Fail，CONTROL已Receive及独立10项复核，independent-failure-review-288-attempt3.json与原288/attempt3/REPORT.md、NATIVE-ISOLATION.json为精确证据。长期POLICY来源inputs/续行许可分模式_用户长期授权_20261019.json，ApprovalId call_37410ba043344e9f81b4ce3ef3472a18:0。根已用2/5，本卡cycle AI288:CODE293:retry3，实际claim才消费第三周期；不是新的人工许可，不叠加次数。此修复成功后CONTROL独立复核，再将原288续行与本卡最新成功execution/cycle配对，不双扣。执行器不自行启动原288或后继。

只按正式派发票据领取本卡一次，带AI293_SCOPE.json。先CORE/workers/CODE、两项目入口页首和本模块、toolkit/PST的隔离宿主/恢复节；读取精确源码而不全仓通读。原AI287已经验证的API/Bootstrap/Session/SaveBoundary/codec/store/1～20探针、已有scene/meta和13新meta保持；原三次失败/几何与正常/错目标证据不覆盖。任务只源码/既有离线，不Unity/CLI/MCP/Refresh/原生Compile/Play/Build/场景写入/真实用户存档/配表/发布。

故障事实：ShortCycleValidationLauncher.ArmAndPlay保存原setup并切空bridge、请求Play；同一命令立即Stop撤销启动，未发生EnteredEditMode，restore记录与clean空场景遗留。Stop当前只Stop Bootstrap、disarm和isPlaying=false；Restore仅Changed(EnteredEditMode)或Arm异常调用。原正常退出/错目标拒绝恢复已原生通过；取消的自有bridge已经ENGINE显式清理，现保存scene SHA4C4B4BF7A56E96848F64D496562B25D584B0010934950CF4A9FD22CF233BF982，clean/7根1062对象。源码修复不能把该手动清理写成自动取消通过。

最小修复只现有Launcher的待进入Play取消/恢复时机，复用同一Restore及所有权检查，不新建公开续行入口/生产脚本/平行恢复工具。必须区分尚未进入Play的请求取消与实际Play退出；只在确实回到可恢复的Edit状态、且现有guard证明临时bridge干净/无路径/无根时恢复已保存setup。不能在还运行或切换的Play中恢复；必要的延后回调应可撤销/去重，不能在后续用户场景上迟到执行。保留restore记录直到恢复真正成功；dirty、额外对象、用户已开场景、记录损坏或恢复失败须拒绝覆盖并保留记录/现场，不能清标记掩盖失败。Stop重复调用及迟到EnteredEditMode不得二次恢复/重复打开；无本任务pending记录时不得打开/替换任何场景。正常Stop、宿主目标先销毁后租约释放、原错误/禁用重载设计边界保持。无需为本次修改Bootstrap/API/存档架构；发现不能在范围内完成则正式说明停点。

验证：只扩已有ShortCycleIsolationAndTextTests、UnityStubs和必要既有Test-CK01CShortCycle.ps1分支，不能新增独立runner/专用夹具或复写实现当测试。直接驱动交付的ArmAndPlay/Stop及真实Changed回调入口：待启动立即取消且不触发EnteredEditMode时仍恢复原setup并消费记录；覆盖重复Stop/晚到事件、实际Play退出不能提前Restore、未知dirty/非空/已命名场景拒绝覆盖且保留记录、恢复失败保留记录/无pending无动作。stub明确区分pending和actual Play以及事件，不用强制发EnteredEditMode掩盖原生缺口。保留原host行为与默认路径读取0。用已有validation-host-only模式跑受影响宿主行为与真实UnityEditor引用表面编译，不全ShortCycle/P2L/安全/数据/Final套娃；该离线通过不代原生时序。无必要反复编译未变的模块。失败按原稳定问题key记录，正式Fail并回传，CONTROL再决定原卡续行；不自行重试新周期。

交付：本卡REPORT/evidence/acceptance、实际改动清单与有限前后源码、原始编译/行为日志和ENGINE-HANDOFF。5合同见inputs/acceptance/AI-000293.json，值必须有真实证据。HANDOFF列需ENGINE补验的具体“待启动立即Stop→恢复原scene”的一次原生路径及失败保全边界；10锚点/49节点/原正常和错目标证据不无故重跑；若更改实际运行退出路径须明确最小相关复核而非伪称已验。文档仅更新当前Launcher取消行为/未验事项，不提高全工程核验日期，不宣告A/B/A+C或USER-U5通过。完整包目标1MB/硬10MB，保留必要失败，超1MB说明，不拆包绕限。

每批写前Checkpoint；Complete/Fail/Yield立即Prepare→一次真实app Send→Record至CONTROL01a13ef5-eda9-7882-b1c9-eca47a6470d9。未知写入、真实平台拒绝、stop/exit、120/240硬门保持。无提交推送。根原288仍NEEDS_USER直到修复独立通过并正式ApproveResume；不以本卡成功代原288原生验收。

> SUPERVISED POLICY at 2026-10-20T16:22:12.1953652+08:00: {"policy":{"resourceKeys":["unity-project","documentation"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"AI288真实取消恢复失败的同根POLICY源码/既有离线修复；A已批，原生补验仍回原288且最终A+C门保持。","updatedAt":"2026-10-20T16:22:12.1080980+08:00","supervisionLevel":"full"},"taskId":"AI-000293"}

> SUPERVISED EXTEND at 2026-10-20T16:22:12.5116362+08:00: {"taskIds":["AI-000293"],"reason":"同session原根288取消路径必要修复，不新增业务范围；CODE修复后独立验收与原根续行配对。"}

> SUPERVISED RESUME_APPROVED at 2026-10-20T16:51:33.3375505+08:00: {"remaining":2,"taskId":"AI-000288","sourceApprovalId":"call_37410ba043344e9f81b4ce3ef3472a18:0","grantId":"436476595df449de82a3eb3cb9e5a16e","scope":{"paths":[".ai-workspace/codebase/shortcycle.md",".ai-workspace/outputs/DETACH-B13-20261019/AI-000288","项目整体阅读理解与推进基线_2026-08-14.md","Assets/Editor/Build","Assets/Editor/ProjectValidation","Assets/Scenes/Cooking/ShortCycle_P0P1.unity","Assets/Scripts/Cooking/ShortCycle","Assets/Scripts/Core","Assets/Scripts/DataBase/CookingData","Assets/Scripts/Tools/Persistence","Assets/Scripts/Tools/Security","CODEBASE_MAP.md","DevTools/Scenes/backup/D-T2_pre/AI-000288"],"operations":["documentation-update","evidence-write","idempotence-check","unity-approved-new-node-create","unity-approved-source-import-compile","unity-isolated-play-validation","unity-owned-source-meta-create","unity-owned-temporary-cleanup","unity-scene-bindings","unity-target-check"]},"rootTaskId":"AI-000288","reserved":0,"used":3,"cycles":["AI288:CODE293:retry3"],"verification":"Fresh ENGINE idle revision50; CODE293 latest success b9bff21dc70f4ae09618c6e722881766 independent-review-293.json PASSED5contracts/13checks,actual37cancel+4direct boundaries; pair cycle AI288:CODE293:retry3, not new budget. Only pending-cancel and affected normal exit native replay under originalscope; preserve accepted geometry/wrong-target/history. AI288_ENGINE续行3.txt.","authorizationSource":"POLICY"}

> SUPERVISED RESUME_APPROVED at 2026-10-20T17:24:24.3583084+08:00: {"sourceApprovalId":"call_37410ba043344e9f81b4ce3ef3472a18:0","taskId":"AI-000289","remaining":4,"cycles":["AI289:retry1"],"rootTaskId":"AI-000289","scope":{"paths":[".ai-workspace/codebase/shortcycle.md",".ai-workspace/outputs/DETACH-B13-20261019/AI-000289","项目整体阅读理解与推进基线_2026-08-14.md","Assets/Scenes/Cooking/ShortCycle_P0P1.unity","Assets/Scenes/ShortCycle_P2.unity","Assets/Scenes/ShortCycle_P2.unity.meta","CODEBASE_MAP.md","DevTools/Scenes/backup/D-T2_pre/AI-000289","ProjectSettings/EditorBuildSettings.asset"],"operations":["documentation-update","evidence-write","idempotence-check","unity-approved-viewtable-bindings","unity-build-list-append","unity-isolated-play-validation","unity-owned-temporary-cleanup","unity-scene-create","unity-target-check"]},"used":0,"authorizationSource":"POLICY","verification":"Fresh app ENGINE idle/completed revision52 turn01a15893-2f60-7ee2-bfd2-5da9a3f7e194. CONTROL independent-failure-review-289-attempt1 confirms10checks, dynamic SceneAsset namespace error before execution; original scene/meta/Build and unique backups unchanged. Original scope same-task one resume, no production source changes; policy source SHA verified, root0of5, no human/stop/permission hard gate. AI289_ENGINE续行1.txt exact instruction; A/B/final gates retained.","grantId":"758b872b61494fd38d6aa61bec37e6fb","reserved":1}

> SUPERVISED RESUME_APPROVED at 2026-10-20T17:45:18.0511292+08:00: {"grantId":"258df3177152420aa81629fe92b4f642","taskId":"AI-000289","scope":{"paths":[".ai-workspace/codebase/shortcycle.md",".ai-workspace/outputs/DETACH-B13-20261019/AI-000289","项目整体阅读理解与推进基线_2026-08-14.md","Assets/Scenes/Cooking/ShortCycle_P0P1.unity","Assets/Scenes/ShortCycle_P2.unity","Assets/Scenes/ShortCycle_P2.unity.meta","CODEBASE_MAP.md","DevTools/Scenes/backup/D-T2_pre/AI-000289","ProjectSettings/EditorBuildSettings.asset"],"operations":["documentation-update","evidence-write","idempotence-check","unity-approved-viewtable-bindings","unity-build-list-append","unity-isolated-play-validation","unity-owned-temporary-cleanup","unity-scene-create","unity-target-check"]},"remaining":3,"reserved":1,"sourceApprovalId":"call_37410ba043344e9f81b4ce3ef3472a18:0","used":1,"cycles":["AI289:retry2"],"verification":"Fresh ENGINE app idle/completed revision53; independent-failure-review-289-attempt2 confirms11checks, known partial19nodes/view5 and Build memory3/disk2; actual serializer API precondition error, not platform permission denial. Root1of5/remaining4 CONTROL_REPAIR_ELIGIBLE; POLICY SHA verified. AI289_ENGINE续行2.txt limits one targeted settings save path with staged native candidate verification, no global save/manual YAML/source/scene rebuild; then original unrun isolated validation. Original scope and all A/B/final gates remain.","authorizationSource":"POLICY","rootTaskId":"AI-000289"}
## AI-000294 | [CODE] [SC] AI289联合探针前置与转场组件同根源码修复

- 序号：294
- 提交时间：2026-10-20T18:24:20.0876149+08:00
- 模块 ID：SC
- 初始状态：QUEUED
- 前置依赖：无
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：M / 80

### 指令

AI-000294 / AI289 同根联合探针前置与转场组件源码修复（固定 CODE，M80，REPAIR）

授权与身份：窗口②阶段② A 已获用户批准，B 仅在 A 实际通过后执行。原件为本目录窗口②阶段②用户批准.json；重试 POLICY 来源 inputs/续行许可分模式_用户长期授权_20261019.json，ApprovalId call_37410ba043344e9f81b4ce3ef3472a18:0。原 AI289 attempt3 已 Fail/Receive，CONTROL 独立17项复核确认。同根已用2/5，本卡 cycle AI289:CODE294:retry3，实际领取才消费第三周期；之后原289续行须与本卡最新成功周期正式配对，不双扣。不是新的人批或额外次数。

只领取本卡一次；RUNNING 不再次 Poll。按 CORE、固定 CODE worker、AGENTS 四硬上限、TK-WF 续行条目及 AI294_SCOPE.json 执行。共享 unity-project 锁。仅源码/既有离线验证及必要文档；不调用 Unity/CLI/MCP/Refresh/Play/Build，不修改 scene/prefab/meta/BuildSettings/生产数据/字体/真实用户存档。CODE 不代领原289。

必读（按相关字段读取）：outputs/control/DETACH-B13-20261019/independent-failure-review-289-attempt3.json；outputs/DETACH-B13-20261019/AI-000289/attempt3 下 probe-result.json、probe-failure-diagnosis-receipt.json及其stdoutPath、NATIVE-ISOLATION.json、ENGINE-HANDOFF.json。实际 Probe、ActionRegistry/Router、SessionManager、Phase2HandoffPresenter、Phase2SceneEntry/SceneLoader；既有 Phase2TransitionTests/RecipeBookTests/runner。项目文档页首及ShortCycle模块；AI284既有实际追加协程夹具。原生与离线结论分开。

本卡工作：
1. AI289已完成P2 19空节点、P0P1 view5、Build0/1保留并加2，Build原生保存与磁盘一致；本次fresh AI289/a3-joint隔离pre-Awake/factory1/Stop恢复成立。保留这些成果，不重做或回退。
2. 原生联合探针一次：1～14 PASS、15～20 FAIL。14重启后为Cover；15直接OpenCatalog，但实际Registry仅允许OpenCover从Cover到Catalog、OpenCatalog从Browse到Catalog。修复14～18合法状态前置及衔接，包含已经Catalog、锁定/未使用卡；不放宽生产动作状态机。
3. 17目前只接受Catalog前置，最后一张有效卡点击后可能已Browse。按实际状态等待镜头/锁/层再进入步骤详情。保留选菜ID/锁定拒绝、LocalizedText标题、步骤行数、收藏profile/两处星标一致性及还原语义。不得删卡、跳过断言、硬编码PASS、改数据或靠ENGINE额外注入动作掩盖。
4. 原生sourceAdapterCount=0、Presenter.transitionAdapter未绑；ShortCyclePhase2TransitionAdapter是SessionManager.cs内第二个MonoBehaviour，无独立同名源码。最小拆出同目录ShortCyclePhase2TransitionAdapter.cs，完整类型名/命名空间/API/方法行为不变，Session本体/序列化字段不变；核runner源码包含方式。CODE不生成meta/接场景。交ENGINE由Editor生成新源码meta，在既有P2S对象挂唯一adapter、绑定原presenter。若发现超过此拆分的必要生产行为改动，正式说明后停止扩展。

验证与保全：
- 保全修改前源码。原1～13逻辑/断言/标签/顺序保持，19/20已验前置及清理逻辑保持。14～18只修前置/衔接和必要可测性。允许提取这段为生产与测试共同调用的IEnumerator，禁止复制测试实现；保留原全部20项断言/标签/顺序。旧卡“18源字节不改”不阻止本次明确的必要探针修复，也不授予重写整段探针。
- 仅复用既有Phase2TransitionTests/RecipeBookTests/UnityStubs/Test-CK01CShortCycle，不建新runner/夹具。从实际原13尾None/空RecipeBook，经实际会话/Router/卡绑定驱动真实待修协程覆盖14～18。覆盖Cover起始、已Catalog/已Browse衔接、锁定/空卡后有效卡、无可用菜谱明确失败、缺绑定/错误层明确失败；保留19/20既有6场景回归。不得只静态匹配。
- 做必要离线编译及上述定向行为验证；可CompileOnly加既有测试类入口，不重复整套Final/全路径哈希。如runner原断言硬绑Session文件，改查实际独立文件并保留等价断言。Unity DLL API编译仅表面兼容，原生挂载/序列化仍待ENGINE。
- ENGINE-HANDOFF.json列新源码路径/类型/源码SHA、拟挂的既有对象和原presenter字段、Session.Configure链及原生待补验。旧审计hook两次NRE保留，下一原生观察须判空记录明确失败；CODE不自行修原289输出/调用Unity。
- 仅维护现有CODEBASE_MAP、推进基线、shortcycle模块相关事实，不提高完整核验日期。原289三失败、原件、唯一备份不改；本卡输出目标1MB、硬限10MB，超1MB说明。
- 5合同见inputs/acceptance/AI-000294.json，对应evidence.json checks。报告六要素、真实编译/定向日志、源码保全/diff/交接。源码通过不是A通过；原生1～20/接线/截图、A+C/USER-U5仍待验证。

成功Complete后按长期授权立即Prepare→一次真实app Send→Record回传绑定CONTROL。真实失败按稳定key正式结算停止并回传，由CONTROL按剩余根额度处理；不自重启/换key或卡清零。CONTROL独立验收本卡后，唯一ApproveResume将原289与最新成功cycle配对。290～292继续等待，不提前B/Build/发布。

> SUPERVISED POLICY at 2026-10-20T18:24:40.4109217+08:00: {"taskId":"AI-000294","policy":{"resourceKeys":["unity-project","documentation"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"AI289实际原生联合失败的同根POLICY源码及既有离线修复；原生接线回原289续行，B仍须A实际验收；最终A+C保持。","updatedAt":"2026-10-20T18:24:40.3078229+08:00","supervisionLevel":"full"}}

> SUPERVISED EXTEND at 2026-10-20T18:25:09.2663141+08:00: {"taskIds":["AI-000294"],"reason":"用户已批A执行的必要同根CODE修复；保留原289三失败及SUP5政策，本卡成功后配对原289补验，不扩大B或人工门。"}

> SUPERVISED APPROVED_CONTINUATION_REFUSED at 2026-10-20T18:47:19.7860743+08:00: {"reason":"Requested path exceeds approved scope.","taskId":"AI-000289","controllerThreadId":"01a13ef5-eda9-7882-b1c9-eca47a6470d9","problemKey":""}

> SUPERVISED RESUME_APPROVED at 2026-10-20T22:20:22.9345574+08:00: {"authorizationSource":"HUMAN","rootTaskId":"AI-000289","taskId":"AI-000289","scope":{"paths":[".ai-workspace/codebase/shortcycle.md",".ai-workspace/outputs/DETACH-B13-20261019/AI-000289","项目整体阅读理解与推进基线_2026-08-14.md","Assets/Scenes/Cooking/ShortCycle_P0P1.unity","Assets/Scenes/ShortCycle_P2.unity","Assets/Scenes/ShortCycle_P2.unity.meta","Assets/Scripts/Cooking/ShortCycle/ShortCyclePhase2TransitionAdapter.cs.meta","CODEBASE_MAP.md","DevTools/Scenes/backup/D-T2_pre/AI-000289","ProjectSettings/EditorBuildSettings.asset"],"operations":["documentation-update","evidence-write","idempotence-check","unity-approved-adapter-bindings","unity-approved-viewtable-bindings","unity-build-list-append","unity-isolated-play-validation","unity-owned-temporary-cleanup","unity-reviewed-source-import","unity-scene-create","unity-target-check"]},"used":3,"remaining":2,"sourceApprovalId":"call_dfcbfc1d52b54f86969b0765d4edc0e3:0","reserved":0,"cycles":["AI289:CODE294:retry3"],"grantId":"8ad7c1c3294c419c93646e52ab0c7fa0","verification":"Actual app snapshot revision55 notLoaded, latest turn completed, no active turn; fresh-engine-289-approved3.json. CONTROL independent-review-294.json PASSED 5 contracts/17 checks. Direct human scope approval call_dfcbfc1d52b54f86969b0765d4edc0e3:0 permits exact adapter meta/bindings and one paired original run; no extra retry cycle."}

> SUPERVISED RESUME_APPROVED at 2026-10-20T22:47:22.9530125+08:00: {"cycles":["AI289:retry4"],"verification":"Fresh ENGINE app revision57 idle/completed; no active lane owner. AI289 attempt4 formal Fail received, independent-failure-review-289-attempt4.json 11/11 confirms payload guard escape failure before mutations, native clean Edit and approved meta retained. Same HUMAN-approved scope from call_dfcbfc1d52b54f86969b0765d4edc0e3:0, no new path; repair only existing command regex literals and original native validation. Root used3of5 before new actual claim; POLICY retry4 no reuse/pair with completed retry3.","sourceApprovalId":"call_37410ba043344e9f81b4ce3ef3472a18:0","scope":{"paths":[".ai-workspace/codebase/shortcycle.md",".ai-workspace/outputs/DETACH-B13-20261019/AI-000289","项目整体阅读理解与推进基线_2026-08-14.md","Assets/Scenes/Cooking/ShortCycle_P0P1.unity","Assets/Scenes/ShortCycle_P2.unity","Assets/Scenes/ShortCycle_P2.unity.meta","Assets/Scripts/Cooking/ShortCycle/ShortCyclePhase2TransitionAdapter.cs.meta","CODEBASE_MAP.md","DevTools/Scenes/backup/D-T2_pre/AI-000289","ProjectSettings/EditorBuildSettings.asset"],"operations":["documentation-update","evidence-write","idempotence-check","unity-approved-adapter-bindings","unity-approved-viewtable-bindings","unity-build-list-append","unity-isolated-play-validation","unity-owned-temporary-cleanup","unity-reviewed-source-import","unity-scene-create","unity-target-check"]},"reserved":1,"remaining":1,"used":3,"grantId":"05242d0b6de74df2ad43ab93ae463117","authorizationSource":"POLICY","rootTaskId":"AI-000289","taskId":"AI-000289"}
## AI-000295 | [ENGINE_CLI] [SC] [DETACH-B13-20261019] AI289失效组件槽身份与公开API只读诊断

- 序号：295
- 提交时间：2026-10-20T23:00:49.9736260+08:00
- 模块 ID：无
- 初始状态：QUEUED
- 前置依赖：AI-000294
- 允许越过：False
- 最大尝试次数：1
- 复杂度/轮次预算：M / 80

### 指令

原AI289 attempt5同根READONLY_DIAGNOSIS，固定ENGINE_CLI，M80（单场景只读，生产修改0）。当前根used4/5余1；首次只读诊断不消耗重试周期，不为原289发ApproveResume，不复活已验294，不改根历史/许可。按CORE→ENGINE_CLI/SUPERVISED/HANDOFF及点名材料读取。只写本卡outputs/DETACH-B13-20261019/<TaskId>/及正式结算，不修改项目入口文档/生产文件。

材料：CONTROL independent-failure-review-289-attempt5.json（9事实复核非验收）；AI289/attempt5/REPORT.md、原binding payload/Console与前后只读回执；CONTROL AI289-stale-slot-primary-sources.json；真人已批范围原件AI289续行3_用户范围批准.json。正则已正确；Unity实际拒绝SerializedProperty.DeleteArrayElementAtIndex修改GameObject.m_Component（seq582），Undo后原字节/槽/clean态不变，当前Console1Error10Warning保留。不是操作者批准缺失，不伪称能直接续跑。

仅两项诊断目标：
1. 精确旧槽的真实身份与公开API可达性。先核官方CLI目标D:/GameProject/Eat-What/Unity6000.3.24f1/原实例新鲜心跳，读取本任务安全检查点；仅一个合并的只读eval（status确必要至多1，eval至多1，Console只读至多1），不得重复试探调用。当前P0P1 Managers/Phase2TransitionAdapter GO1205938950/Transform1205938952，槽1旧fileID1205938951/nativeInstance116368（native必须现场读，不硬编码）；m_Script指本scene内MonoScript611693423，Presenter1833023478.transitionAdapter原fileID1205938951。通过现有SerializedObject只读字段、明确GlobalObjectId解析以及安装版本已有public EditorUtility InstanceIDToObject/EntityIdToObject（核实际签名，选择单一适用公开查找；禁止反射private/internal、native指针、SerializedObject写入）尝试解析这两个精确已载对象。对ReferenceEquals(null)、Unity null、实际managedType、GlobalObjectId、是否persistent/assetPath、若是Component的go/scene和仅该对象必要字段分别报告；null须先判空，不抛NRE，不扩大Resources全扫。只读读取当前m_Component.editable/引用以及public Undo.DestroyObjectImmediate签名元数据，不Invoke任何删除/Undo。无已知public可达handle也可作为有效诊断结论。
2. 依据真实handle/归属及官方文档，交付可执行的最小候选与停止条件。如果有合法可定位对象，只给精确公开API候选、受影响引用/如何保留Transform及如何Undo恢复，说明尚未生产验证；如果无法通过公开API精确处理，明确不可行和需要用户选择的范围差异。不制造/执行假对象测试，不调用Destroy/删除数组/RemoveMissingScripts/ExecuteMenuItem/反射非公开写API，不手改YAML、不重建GameObject或复制替换场景，不以此诊断卡试跑修复。官方UUM-141893仅相似情形，不能直接定性本项目同缺陷或使用批量RemoveMonoBehavioursWithMissingScript；本例missingCount0不构成可删证明。优先调查准确身份，未知保留，不给未经证据支持的成功保证。

绝对禁止生产修改、调用Undo注册/回滚/删除/添加组件/赋值/Save、OpenScene/Refresh/Compile/Import/Play/Build/导表、源码/meta/字体/数据/真实存档访问、清Console、切换MCP/换工具重试。输出payload是只读表达式，不建立新工具/runner/夹具。完成后用同一只读eval前后摘要证明clean/场景/组件槽/Console计数基线保持；磁盘只核本次涉及scene/meta的允许diff，不重复全仓哈希。拿不到可用handle不是任务失败：如实交付明确候选不可行及缺口，diagnosisRecorded/planBounded为true仅表示证据和边界交付，绝不表示修复通过。

原289保持NEEDS_USER，B继续等待A真实独立通过，A+C/USER-U5保持。本卡合同4项：checks.identityAndApiEvidenceRecorded=true；checks.planAndUncertaintiesBounded=true；checks.productionWrites=0；checks.mutatingUnityCalls=0。完整REPORT/evidence/acceptance/ENGINE-HANDOFF和原始CLI回执，包目标1MB硬限10MB。Complete/Fail立即Prepare→一次真实app Send→Record给CONTROL；不自行领取原289或动用余1，不自动重试，不提交推送。

> SUPERVISED POLICY at 2026-10-20T23:01:39.0169474+08:00: {"policy":{"resourceKeys":["unity-project"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"原289同根必要只读身份/API诊断；最多一次只读eval，仅本卡输出，无Unity/生产修改，不消费剩余1次重试。","updatedAt":"2026-10-20T23:01:38.9127018+08:00","supervisionLevel":"full"},"taskId":"AI-000295"}

> SUPERVISED EXTEND at 2026-10-20T23:01:39.3453341+08:00: {"taskIds":["AI-000295"],"reason":"同批AI289正式失败5后只读诊断，为最后一次原范围续行形成依据，不启动根任务或B"}

> SUPERVISED RESUME_APPROVED at 2026-10-20T23:30:40.3829316+08:00: {"verification":"Direct human scope/method approval call_cbb6abcbc1994847ba0b9fbfc63db19e:0 bound to review SHA52621E3E1C4FA3B64B88D915A8FD78515B8451FDC9ED340FC5FA79138ACC25ED and candidate original. AI295 independent4contracts11checks PASSED_DIAGNOSIS_ONLY, no paired repair. Actual ENGINE app idle/completed revision60 saved fresh-engine-289-resume5-approved.json. Same SUPERVISED ACTIVE session no stop/exit; existing root4of5, this is final existing cycle not extra quota. Exact production scene SHA unchanged.","remaining":0,"rootTaskId":"AI-000289","sourceApprovalId":"call_cbb6abcbc1994847ba0b9fbfc63db19e:0","used":4,"grantId":"5ce4585236584f6a8edbc3e04381a349","scope":{"paths":[".ai-workspace/codebase/shortcycle.md",".ai-workspace/outputs/DETACH-B13-20261019/AI-000289","项目整体阅读理解与推进基线_2026-08-14.md","Assets/Scenes/Cooking/ShortCycle_P0P1.unity","Assets/Scenes/ShortCycle_P2.unity","Assets/Scenes/ShortCycle_P2.unity.meta","Assets/Scripts/Cooking/ShortCycle/ShortCyclePhase2TransitionAdapter.cs.meta","CODEBASE_MAP.md","DevTools/Scenes/backup/D-T2_pre/AI-000289","ProjectSettings/EditorBuildSettings.asset"],"operations":["documentation-update","evidence-write","idempotence-check","serialized-scene-script-reference-repair","unity-approved-adapter-bindings","unity-approved-viewtable-bindings","unity-build-list-append","unity-isolated-play-validation","unity-owned-scene-unload-reopen","unity-owned-temporary-cleanup","unity-reviewed-source-import","unity-scene-create","unity-target-check"]},"authorizationSource":"HUMAN","reserved":1,"cycles":["AI289:retry5"],"taskId":"AI-000289"}

> SUPERVISED RESUME_APPROVED at 2026-10-21T00:12:38.3953649+08:00: {"used":5,"taskId":"AI-000289","authorizationSource":"HUMAN","cycles":["grant:916a257bb8a44396a6121f99d9eaa78e","grant:ed5b305dac8f40c78c1daf19724ec237","grant:3645f2c42b3942958251d3b4bccea739"],"grantId":"7613eadf06ae4a938f0bbabb027bc0d5","rootTaskId":"AI-000289","sourceApprovalId":"call_b2af02a0a6de42e993f613200ed47c02:0","reserved":3,"scope":{"paths":[".ai-workspace/codebase/shortcycle.md",".ai-workspace/outputs/DETACH-B13-20261019/AI-000289","项目整体阅读理解与推进基线_2026-08-14.md","CODEBASE_MAP.md"],"operations":["documentation-update","evidence-write","idempotence-check","unity-game-view-capture","unity-isolated-play-validation","unity-owned-temporary-cleanup","unity-target-check"]},"verification":"Actual app wait snapshot revision62 idle/completed saved fresh-engine-289-extra3.json; CONTROL independent attempt6 review16 facts confirms only screenshots missing; user extra3 original bound planSHA2CD55E0956A7634B5EC56CE7A091AD5325C698E68936E03FACDB639022D7443C and same screenshots-only Scope; no repair pairing, full session unchanged","remaining":0}

> SUPERVISED RECONCILE at 2026-10-21T14:53:05.3888822+08:00: {"repairs":["REQUEUE_UNCLAIMED_DISPATCH:AI-000289"],"inspection":[],"evidence":"User explicitly approved actual AI289 prompt sending call_ce06f9d85bce4483973e49333137d563:0; fresh ENGINE app revision63 notLoaded latest completed no active turn, queue RUNNING0/old289 attempt6 queued with HUMAN grant3 unused; prior reservation expired; reconcile entire session and locks, no new approval grant"}

> SUPERVISED RESUME_APPROVED at 2026-10-21T15:39:46.2389692+08:00: {"taskId":"AI-000289","cycles":["grant:916a257bb8a44396a6121f99d9eaa78e","grant:ed5b305dac8f40c78c1daf19724ec237","grant:3645f2c42b3942958251d3b4bccea739"],"authorizationSource":"HUMAN","scope":{"paths":[".ai-workspace/codebase/shortcycle.md",".ai-workspace/outputs/DETACH-B13-20261019/AI-000289","项目整体阅读理解与推进基线_2026-08-14.md","CODEBASE_MAP.md"],"operations":["documentation-update","evidence-write","idempotence-check","unity-game-view-capture","unity-isolated-play-validation","unity-owned-temporary-cleanup","unity-target-check"]},"reserved":2,"grantId":"7613eadf06ae4a938f0bbabb027bc0d5","sourceApprovalId":"call_b2af02a0a6de42e993f613200ed47c02:0","remaining":0,"verification":"Same unused HUMAN grant remaining2; no new quota. Formal Receive attempt7 plus independent14/14 facts; known own PNG path excursion cleaned, no actual platform denial/unknown drift; same official capture_game_view screen omit save_path documented inline branch, output-only existingScope unchanged. Fresh ENGINE revision65 idle/completed no active run; see fresh-engine-after-289-fail7.json and independent-failure-review-289-attempt7.json","used":6,"rootTaskId":"AI-000289"}

> SUPERVISED RESUME_APPROVED at 2026-10-21T16:20:40.4378120+08:00: {"remaining":0,"used":7,"sourceApprovalId":"call_b2af02a0a6de42e993f613200ed47c02:0","authorizationSource":"HUMAN","reserved":1,"verification":"CONTROL independent attempt8 review14of14 formalFail HTTP400 and clean native state; no platform refusal/blockReason; fresh ENGINE app revision66 idle/completed saved fresh-engine-289-after-fail8.json; same HUMAN original grant used2of3 reserve1; exact scope unchanged; bounded fresh host-log Ready before firstRPC candidate only, no new grant or pairing","scope":{"paths":[".ai-workspace/codebase/shortcycle.md",".ai-workspace/outputs/DETACH-B13-20261019/AI-000289","项目整体阅读理解与推进基线_2026-08-14.md","CODEBASE_MAP.md"],"operations":["documentation-update","evidence-write","idempotence-check","unity-game-view-capture","unity-isolated-play-validation","unity-owned-temporary-cleanup","unity-target-check"]},"taskId":"AI-000289","cycles":["grant:916a257bb8a44396a6121f99d9eaa78e","grant:ed5b305dac8f40c78c1daf19724ec237","grant:3645f2c42b3942958251d3b4bccea739"],"grantId":"7613eadf06ae4a938f0bbabb027bc0d5","rootTaskId":"AI-000289"}
## AI-000296 | [CODE] [WF] REPAIR-CLI-01 队列落盘原子化

- 序号：296
- 提交时间：2026-10-21T17:56:33.8756902+08:00
- 模块 ID：WF
- 初始状态：QUEUED
- 前置依赖：无
- 允许越过：True
- 最大尝试次数：3
- 复杂度/轮次预算：S / 40

### 指令

用户已批准独立CLI管线修复轨。精确执行卡：.ai-workspace/inputs/派发/AI289-CLI-REPAIR-20261021/AI296_REPAIR-CLI-01.txt；Scope：.ai-workspace/inputs/派发/AI289-CLI-REPAIR-20261021/AI296_SCOPE.json；预声明合同：.ai-workspace/inputs/acceptance/AI-000296.json。固定CODE，S40，CLI(磨合)，pipeline-repair；仅执行本卡，结算即时回CONTROL。

> SUPERVISED POLICY at 2026-10-21T17:56:34.4134763+08:00: {"taskId":"AI-000296","policy":{"resourceKeys":["workflow-devtools","documentation"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"用户明确独立管线修复轨；只工作流源码/隔离夹具，不启动Unity，不消耗289根","updatedAt":"2026-10-21T17:56:34.3333731+08:00","supervisionLevel":"full"}}

> SUPERVISED EXTEND at 2026-10-21T17:57:02.6384677+08:00: {"reason":"用户2026-10-21重新授权监管并下发独立AI289管线修复轨；首卡队列共享保存函数离线修复","taskIds":["AI-000296"]}
## AI-000297 | [CODE] [WF] REPAIR-CLI-02 载荷字节传递

- 序号：297
- 提交时间：2026-10-21T18:08:59.9096273+08:00
- 模块 ID：WF
- 初始状态：QUEUED
- 前置依赖：AI-000296
- 允许越过：True
- 最大尝试次数：3
- 复杂度/轮次预算：S / 40

### 指令

用户2026-10-21授权独立CLI磨合修复轨；精确卡：.ai-workspace/inputs/派发/AI289-CLI-REPAIR-20261021/AI297_REPAIR-CLI-02.txt；Scope：.ai-workspace/inputs/派发/AI289-CLI-REPAIR-20261021/AI297_SCOPE.json；合同：.ai-workspace/inputs/acceptance/AI-000297.json。共同来源：.ai-workspace/inputs/派发/AI289-CLI-REPAIR-20261021/用户授权与覆盖关系.json。semanticCategory=pipeline-repair，独立根；不得自领后继。

> SUPERVISED POLICY at 2026-10-21T18:09:00.4295906+08:00: {"taskId":"AI-000297","policy":{"resourceKeys":["workflow-devtools","documentation"],"reviewDependsOn":["AI-000296"],"humanGateAfter":false,"gateApproved":false,"reason":"按用户独立修复轨登记；前卡CONTROL独立验收后推进，共享文件顺序修改；原289计数保持","updatedAt":"2026-10-21T18:09:00.3563366+08:00","supervisionLevel":"full"}}
## AI-000298 | [CODE] [WF] REPAIR-CLI-03 证据序列化器

- 序号：298
- 提交时间：2026-10-21T18:09:00.6106473+08:00
- 模块 ID：WF
- 初始状态：QUEUED
- 前置依赖：AI-000297
- 允许越过：True
- 最大尝试次数：3
- 复杂度/轮次预算：S / 40

### 指令

用户2026-10-21授权独立CLI磨合修复轨；精确卡：.ai-workspace/inputs/派发/AI289-CLI-REPAIR-20261021/AI298_REPAIR-CLI-03.txt；Scope：.ai-workspace/inputs/派发/AI289-CLI-REPAIR-20261021/AI298_SCOPE.json；合同：.ai-workspace/inputs/acceptance/AI-000298.json。共同来源：.ai-workspace/inputs/派发/AI289-CLI-REPAIR-20261021/用户授权与覆盖关系.json。semanticCategory=pipeline-repair，独立根；不得自领后继。

> SUPERVISED POLICY at 2026-10-21T18:09:00.9101133+08:00: {"taskId":"AI-000298","policy":{"resourceKeys":["workflow-devtools","documentation"],"reviewDependsOn":["AI-000297"],"humanGateAfter":false,"gateApproved":false,"reason":"按用户独立修复轨登记；前卡CONTROL独立验收后推进，共享文件顺序修改；原289计数保持","updatedAt":"2026-10-21T18:09:00.7891072+08:00","supervisionLevel":"full"}}
## AI-000299 | [CODE] [WF] REPAIR-CLI-04 截图保存路径与no-save通道

- 序号：299
- 提交时间：2026-10-21T18:09:01.0942703+08:00
- 模块 ID：WF
- 初始状态：QUEUED
- 前置依赖：AI-000298
- 允许越过：True
- 最大尝试次数：3
- 复杂度/轮次预算：S / 40

### 指令

用户2026-10-21授权独立CLI磨合修复轨；精确卡：.ai-workspace/inputs/派发/AI289-CLI-REPAIR-20261021/AI299_REPAIR-CLI-04.txt；Scope：.ai-workspace/inputs/派发/AI289-CLI-REPAIR-20261021/AI299_SCOPE.json；合同：.ai-workspace/inputs/acceptance/AI-000299.json。共同来源：.ai-workspace/inputs/派发/AI289-CLI-REPAIR-20261021/用户授权与覆盖关系.json。semanticCategory=pipeline-repair，独立根；不得自领后继。

> SUPERVISED POLICY at 2026-10-21T18:09:01.3979656+08:00: {"taskId":"AI-000299","policy":{"resourceKeys":["unity-project","workflow-devtools","documentation"],"reviewDependsOn":["AI-000298"],"humanGateAfter":false,"gateApproved":false,"reason":"按用户独立修复轨登记；前卡CONTROL独立验收后推进，共享文件顺序修改；原289计数保持","updatedAt":"2026-10-21T18:09:01.3017060+08:00","supervisionLevel":"full"}}
## AI-000300 | [CODE] [WF] REPAIR-CLI-05 宿主Ready握手

- 序号：300
- 提交时间：2026-10-21T18:09:01.5668715+08:00
- 模块 ID：WF
- 初始状态：QUEUED
- 前置依赖：AI-000299
- 允许越过：True
- 最大尝试次数：3
- 复杂度/轮次预算：M / 80

### 指令

用户2026-10-21授权独立CLI磨合修复轨；精确卡：.ai-workspace/inputs/派发/AI289-CLI-REPAIR-20261021/AI300_REPAIR-CLI-05.txt；Scope：.ai-workspace/inputs/派发/AI289-CLI-REPAIR-20261021/AI300_SCOPE.json；合同：.ai-workspace/inputs/acceptance/AI-000300.json。共同来源：.ai-workspace/inputs/派发/AI289-CLI-REPAIR-20261021/用户授权与覆盖关系.json。semanticCategory=pipeline-repair，独立根；不得自领后继。

> SUPERVISED POLICY at 2026-10-21T18:09:01.8374374+08:00: {"taskId":"AI-000300","policy":{"resourceKeys":["workflow-devtools","documentation"],"reviewDependsOn":["AI-000299"],"humanGateAfter":false,"gateApproved":false,"reason":"按用户独立修复轨登记；前卡CONTROL独立验收后推进，共享文件顺序修改；原289计数保持","updatedAt":"2026-10-21T18:09:01.7606572+08:00","supervisionLevel":"full"}}
## AI-000301 | [CODE] [WF] G87-METRICS CLI磨合期分类与任务简表接入

- 序号：301
- 提交时间：2026-10-21T18:09:02.0005786+08:00
- 模块 ID：WF
- 初始状态：QUEUED
- 前置依赖：AI-000300
- 允许越过：True
- 最大尝试次数：3
- 复杂度/轮次预算：M / 80

### 指令

用户2026-10-21授权独立CLI磨合修复轨；精确卡：.ai-workspace/inputs/派发/AI289-CLI-REPAIR-20261021/AI301_G87-METRICS.txt；Scope：.ai-workspace/inputs/派发/AI289-CLI-REPAIR-20261021/AI301_SCOPE.json；合同：.ai-workspace/inputs/acceptance/AI-000301.json。共同来源：.ai-workspace/inputs/派发/AI289-CLI-REPAIR-20261021/用户授权与覆盖关系.json。semanticCategory=pipeline-repair，独立根；不得自领后继。

> SUPERVISED POLICY at 2026-10-21T18:09:02.2796923+08:00: {"taskId":"AI-000301","policy":{"resourceKeys":["workflow-devtools","documentation"],"reviewDependsOn":["AI-000300"],"humanGateAfter":false,"gateApproved":false,"reason":"按用户独立修复轨登记；前卡CONTROL独立验收后推进，共享文件顺序修改；原289计数保持","updatedAt":"2026-10-21T18:09:02.1953298+08:00","supervisionLevel":"full"}}
## AI-000302 | [ENGINE_CLI] [WF] SMOKE-CLI-01 CLI截图链冒烟

- 序号：302
- 提交时间：2026-10-21T18:09:02.5022911+08:00
- 模块 ID：WF
- 初始状态：QUEUED
- 前置依赖：AI-000296, AI-000297, AI-000298, AI-000299, AI-000300, AI-000301
- 允许越过：True
- 最大尝试次数：3
- 复杂度/轮次预算：S / 40

### 指令

用户2026-10-21授权独立CLI磨合修复轨；精确卡：.ai-workspace/inputs/派发/AI289-CLI-REPAIR-20261021/AI302_SMOKE-CLI-01.txt；Scope：.ai-workspace/inputs/派发/AI289-CLI-REPAIR-20261021/AI302_SCOPE.json；合同：.ai-workspace/inputs/acceptance/AI-000302.json。共同来源：.ai-workspace/inputs/派发/AI289-CLI-REPAIR-20261021/用户授权与覆盖关系.json。semanticCategory=pipeline-repair，独立根；不得自领后继。

> SUPERVISED POLICY at 2026-10-21T18:09:02.7757191+08:00: {"taskId":"AI-000302","policy":{"resourceKeys":["unity-project","workflow-devtools","documentation"],"reviewDependsOn":["AI-000296","AI-000297","AI-000298","AI-000299","AI-000300","AI-000301"],"humanGateAfter":false,"gateApproved":false,"reason":"按用户独立修复轨登记；前卡CONTROL独立验收后推进，共享文件顺序修改；原289计数保持","updatedAt":"2026-10-21T18:09:02.7275681+08:00","supervisionLevel":"full"}}

> SUPERVISED EXTEND at 2026-10-21T18:09:03.1669985+08:00: {"reason":"用户要求维持监管并完成独立CLI修复/冒烟后续行289与已批B；原卡历史/最终人审门保持","taskIds":["AI-000297","AI-000298","AI-000299","AI-000300","AI-000301","AI-000302"]}

> SUPERVISED USER_GATE_APPROVED at 2026-10-21T18:23:35.7995066+08:00: {"taskId":"AI-000296","userDecision":"用户2026-10-21原话：接收相关的修复派发指令，维持回到监管模式，修复轨任务都完成后续行原本的289任务并继续推进后续的待完成任务链。派发原文明确五修复由CONTROL独立核差异与回归后顺序推进；本次只登记该已授条件式执行门，AI296独立6合同/18断言通过，证据outputs/control/AI289-CLI-REPAIR-20261021/independent-review-296.json。不是新的人类视觉/产品验收，不解除最终A+C或USER-U5。"}

> SUPERVISED USER_GATE_APPROVED at 2026-10-21T18:44:22.4756037+08:00: {"taskId":"AI-000297","userDecision":"用户2026-10-21原话：接收相关的修复派发指令，维持回到监管模式，修复轨任务都完成后续行原本的289任务并继续推进后续的待完成任务链。派发原文五修复由CONTROL独立核差异及回归后顺序推进；本次仅登记该已授条件式执行门，AI297已经CONTROL独立4合同/76断言及12项事实核查通过，证据.ai-workspace/outputs/control/AI289-CLI-REPAIR-20261021/independent-review-297.json。不是新的人类视觉/产品验收，不解除最终A+C或USER-U5，也不续行原289。"}

> SUPERVISED RESUME_APPROVED at 2026-10-21T19:18:05.3996080+08:00: {"verification":"固定CODE app revision190真实idle/最新轮completed；AI298原CODE_STOP已Receive，CONTROL独立12事实复核，root used0/reserved0/limit5，CONTROL_REPAIR_ELIGIBLE，原scope内分流编码/帧和未完成caller验证。证据independent-failure-review-298-attempt1.json与fresh-code-idle-298-resume1.json；精确指令inputs/派发/AI289-CLI-REPAIR-20261021/AI298_CODE续行1.txt。原289不消费。","reserved":1,"sourceApprovalId":"call_37410ba043344e9f81b4ce3ef3472a18:0","used":0,"taskId":"AI-000298","grantId":"522700570a784c1982ccbd3505459494","cycles":["AI298:retry1"],"rootTaskId":"AI-000298","remaining":4,"authorizationSource":"POLICY","scope":{"paths":[".ai-workspace/outputs/AI289-CLI-REPAIR-20261021/AI-000298",".ai-workspace/toolkit/WF.md","DevTools/Workflow/Test-EngineCli.ps1","DevTools/Workflow/UnityEvidenceJson.cs"],"operations":["documentation-update","evidence-write","idempotence-check","offline-validation","source-edit"]}}

> SUPERVISED USER_GATE_APPROVED at 2026-10-21T19:42:05.0155316+08:00: {"userDecision":"用户2026-10-21原话：接收相关的修复派发指令，维持回到监管模式，修复轨任务都完成后续行原本的289任务并继续推进后续的待完成任务链。按已授条件式修复顺序，AI298 attempt2 CONTROL独立4合同/15复核/165真实结构体+14驱动断言通过，证据outputs/control/AI289-CLI-REPAIR-20261021/independent-review-298.json。本次仅登记具体内部执行gate，不伪造新人类回复或视觉验收，不解除最终A+C/USER-U5，不续289。","taskId":"AI-000298"}

> SUPERVISED RESUME_APPROVED at 2026-10-21T20:13:26.2419754+08:00: {"grantId":"276f64725d784c22a92738e3b3a48136","reserved":1,"remaining":4,"verification":"CODE app revision193 idle/latest completed; independent-failure-review-299-attempt1.json 14 facts; same original scope; CONTROL_REPAIR_ELIGIBLE root0/5; original broken PNG header00DA independently confirmed, no native rerun. See fresh-code-idle-299-resume1.json.","sourceApprovalId":"call_37410ba043344e9f81b4ce3ef3472a18:0","used":0,"cycles":["AI299:retry1"],"scope":{"paths":[".ai-workspace/outputs/AI289-CLI-REPAIR-20261021/AI-000299",".ai-workspace/toolkit/WF.md","DevTools/Workflow/Invoke-TaskTool.ps1","DevTools/Workflow/Test-EngineCli.ps1","Library/PackageCache/com.unity.pipeline@62c08c808737/Editor/Commands/Capture/CaptureCommands.cs"],"operations":["documentation-update","evidence-write","idempotence-check","offline-validation","source-edit"]},"authorizationSource":"POLICY","rootTaskId":"AI-000299","taskId":"AI-000299"}

> SUPERVISED USER_GATE_APPROVED at 2026-10-21T20:57:33.3681234+08:00: {"taskId":"AI-000299","userDecision":"用户原始条件式执行指令：接收相关的修复派发指令，维持回到监管模式，修复轨任务都完成后续行原本的289任务并继续推进后续的待完成任务链；取消重载：不进行重载，忽略这个重载要求，向后正常推进其余内容。本卡AI299已CONTROL独立5合同/16复核/181断言通过，reviewSHA=71A2BC86EA082496CCDAD76C275B610E902958CC121217BF9E64D1272B3EA47F。仅登记本批修复顺序的ReviewDependsOn执行门；非新人工回复、非视觉/产品验收，不涉及292 A+C/USER-U5。"}

> SUPERVISED USER_GATE_APPROVED at 2026-10-21T21:49:17.8519874+08:00: {"taskId":"AI-000300","userDecision":"接收相关的修复派发指令，维持回到监管模式，修复轨任务都完成后续行原本的289任务并继续推进后续的待完成任务链；本卡既定CONTROL独立通过条件已满足，证据D:\\GameProject\\Eat-What\\.ai-workspace\\outputs\\control\\AI289-CLI-REPAIR-20261021/independent-review-300.json SHA256=B7B8EE21791E9A0995EBCDE26A3C0183FF6056138C8BF81F8192DAB864249813；仅内部执行顺序门，非新增人工视觉批准。"}
## AI-000303 | [ENGINE_CLI] [WF] SMOKE-CLI-01 CLI截图链冒烟（替代未领取302，隔离路径校正）

- 序号：303
- 提交时间：2026-10-21T22:22:09.6049515+08:00
- 模块 ID：WF
- 初始状态：QUEUED
- 前置依赖：AI-000296, AI-000297, AI-000298, AI-000299, AI-000300, AI-000301
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：S / 40

### 指令

# AI-000303 · SMOKE-CLI-01 CLI截图链冒烟
固定ENGINE_CLI原chat；SUPERVISED；用户明确S/40（有Play但保留原卡等级并解释）；独立管线成熟度样本，usageCategory=pipeline-repair。用户最新“不进行重载，忽略这个重载要求，向后正常推进其余内容”覆盖派发文档新线/重载条件。不创建chat、不伪报重载。
前置AI296～300五修复及AI301计量接入均CONTROL独立通过；CONTROL派发前汇总真实共享调用面与scope。用户本轮执行请求已批准原文限定本卡Scope，不重复索取同一许可。
只一次fresh task/run隔离，源Assets/Scenes/Cooking/ShortCycle_P0P1.unity只读；已有287/293宿主，任何Play前精确task/run输出根、pre-Awake/target/root/lease核实，不真实用户存档。
同官方CLI status→editor_status（受管RecordTaskProgress）→Arm/Play→共享Ready等待→Cover/Catalog/Browse各一次capture_game_view(source=screen,1024x576,no-save)→原流与PNG→Stop→G26清理→新增Console Error0。不跑原1～20，不改P2/Build/view5/adapter/meta，不替AI289交付三图，不发布。
调用必须使用已通过296～301真实helper和wrapper；源码修复不是本卡权限，原生装载不明/类型未加载/未知dirty先停止。三个PNG为真实原生输出，不用编辑重绘；正证据不能覆盖错误历史。失败正式Fail回CONTROL判对应修复轨，不消费AI289。
Scope仅本卡输出与已批准临时隔离生命周期；旧场景只读，任何新的资产/源码应用方案须CONTROL具体核实，不能凭字面scope扩大。
输出REPORT六要素、evidence/acceptance、调用回执、隔离/Console/清理、三原图与size/hash、machine usage（实际工具CLI；未知null），包≤10MB，>1MB说明。C1-C4保持原scene结构；不新建交互UI/生产节点。
Complete/Fail/Yield后Prepare→一次实际app Send→Record回CONTROL 01a13ef5-eda9-7882-b1c9-eca47a6470d9，结束不自领289。


本卡仅替代从未领取的AI302；同一SMOKE目标/五合同/固定ENGINE_CLI/S40/一次fresh运行，非第二份冒烟。原卡attempt0、失败0、许可消费0完整保留并通过正式SupersedeTask行政关闭，不是机器验收或次数重置。queue无修改已登记businessScope接口，用既有行政替代流程纠正输出路径。实际隔离根为D:/GameProject/Eat-What/.ai-workspace/outputs/DETACH-B13-20261019/AI-000303/isolated-saves/<fresh-run>，正式报告/原流/PNG仍本批AI-000303目录；除此无新路径。宿主源码/原场景/P2/Build/meta均不改。明确不消费原289次数。
依次读取AI300/CALL-EXAMPLE.md（Ready）、AI299/attempt2/CALL-EXAMPLE.md（capture）及AI298调用证据，位于本批outputs各卡。健康快照来自本次已有status/editor_status，必须绑定实际task/attempt/execution/owner/project/version/PID/port/timestamps；不新增健康轮询。共享wrapper显式UsageCategory pipeline-repair，ToolSource保持unity-official-cli。按AI300共享Ready模式后再业务导航，不自己复制重试循环。截图强制screen1024x576/no-save，用CapturePng批准路径输出；禁止旧save_path分支。原UnityEvidenceJson helper按已验反射caller加载不复制类；原生支持不明如实失败回传。

> SUPERVISED POLICY at 2026-10-21T22:22:10.1477361+08:00: {"taskId":"AI-000303","policy":{"resourceKeys":["unity-project","workflow-devtools","documentation"],"reviewDependsOn":["AI-000296","AI-000297","AI-000298","AI-000299","AI-000300","AI-000301"],"humanGateAfter":false,"gateApproved":false,"reason":"Same originally authorized SMOKE; replaces never-claimed302 solely to add exact existing host output root. All5contracts/S40/shared locks/review edges preserved; no retry reset or extra business run.","updatedAt":"2026-10-21T22:22:10.0987916+08:00","supervisionLevel":"full"}}

> SUPERVISED EXTEND at 2026-10-21T22:22:10.3406020+08:00: {"taskIds":["AI-000303"],"reason":"Same user-authorized single SMOKE; correct fresh task isolation root, preserve302 via administrative supersession."}

> SUPERVISED PauseSupervision at 2026-10-21T22:22:10.4141963+08:00: {"userDecision":"接收相关的修复派发指令，维持回到监管模式，修复轨任务都完成后续行原本的289任务并继续推进后续的待完成任务链","reason":"No running tasks/reservations. Brief dispatch pause required by official SupersedeTask to replace unclaimed AI302 with same-goal303; restore same session immediately under existing continue-supervision request."}

> Superseded AI-000302 : {"at":"2026-10-21T22:22:10.5209672+08:00","decision":"CLOSED_SUPERSEDED","previousStatus":"QUEUED","accepted":false,"reason":"Replaced by AI-000303: original302 was never claimed, attempts0/failures0/consumption0; same single SMOKE/S40/five contracts, correcting exact host isolated-saves root via existing Enqueue. No failed task reset.","userDecision":"接收相关的修复派发指令，维持回到监管模式，修复轨任务都完成后续行原本的289任务并继续推进后续的待完成任务链","verification":"D:\\GameProject\\Eat-What\\.ai-workspace\\outputs\\control\\AI289-CLI-REPAIR-20261021/AI302-to-303-administration-plan.json; fresh CODE196 idle/ENGINE68 notLoaded last completed; no running task/reservation."}

> SUPERVISED ResumeSupervision at 2026-10-21T22:22:10.7519766+08:00: {"userDecision":"接收相关的修复派发指令，维持回到监管模式，修复轨任务都完成后续行原本的289任务并继续推进后续的待完成任务链","reason":"Administrative unclaimed-card replacement complete. Resume same session and originally authorized single SMOKE chain; final human gates unchanged."}

> SUPERVISED USER_GATE_APPROVED at 2026-10-21T22:22:53.1284112+08:00: {"userDecision":"接收相关的修复派发指令，维持回到监管模式，修复轨任务都完成后续行原本的289任务并继续推进后续的待完成任务链；301具体CONTROL独立验收条件现已满足，D:\\GameProject\\Eat-What\\.ai-workspace\\outputs\\control\\AI289-CLI-REPAIR-20261021/independent-review-301.json SHA256=7D2FF204BE1E17355A3513ED091C1FC2940A049C665114FC8C73737B3E0D1088；仅原执行顺序门，非新增人工视觉验收。","taskId":"AI-000301"}
## AI-000304 | [CODE] [WF] AI303 Ready时限与真实CLI封装定向修复

- 序号：304
- 提交时间：2026-10-21T22:47:24.5323454+08:00
- 模块 ID：WF
- 初始状态：QUEUED
- 前置依赖：无
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：M / 80

### 指令

精确输入 .ai-workspace/inputs/派发/AI289-CLI-REPAIR-20261021/AI304_原生Ready与CLI协议修复.txt；Scope AI304_SCOPE.json；合同 inputs/acceptance/AI-000304.json。独立pipeline-repair内同根303 REPAIR/POLICY；不消费289；CONTROL独立通过后原303同cycle配对。

> SUPERVISED POLICY at 2026-10-21T22:47:25.0723319+08:00: {"taskId":"AI-000304","policy":{"resourceKeys":["unity-project","workflow-devtools","documentation"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"303首轮真实SMOKE15秒等待超时及原生CLI封装差异，CONTROL14事实复核；同303根第1周期CODE源码/既有离线修复，先独立通过再配对303，不消耗289。","updatedAt":"2026-10-21T22:47:24.9933295+08:00","supervisionLevel":"full"}}

> SUPERVISED EXTEND at 2026-10-21T22:47:25.1662049+08:00: {"reason":"用户已批修复轨及SUP5政策，增补同独立SMOKE根303必要CODE修复，不触发289或B。","taskIds":["AI-000304"]}

> SUPERVISED RESUME_APPROVED at 2026-10-21T23:32:50.5162168+08:00: {"grantId":"6beeb25358194b3884e978c5f065c269","rootTaskId":"AI-000303","sourceApprovalId":"call_37410ba043344e9f81b4ce3ef3472a18:0","used":1,"remaining":4,"cycles":["AI303:CODE304:retry1"],"reserved":0,"verification":"ENGINE app idle revision69 latest turn completed; CONTROL304 independent5 contracts16facts274assertions PASSED evidenceSHA=50D73A1F7B2D76FCA15FFC64884E88E19D4F7B0B088DCAC0FCB625E0ACE672E4; pair latest successful304 execution efc98ab6fd714097aa1f74c2bc41ebd0 samecycle1/5 no doublecharge; original303 Scope unchanged.","scope":{"paths":[".ai-workspace/outputs/AI289-CLI-REPAIR-20261021/AI-000303",".ai-workspace/outputs/DETACH-B13-20261019/AI-000303/isolated-saves"],"operations":["evidence-write","idempotence-check","isolated-validation-play","native-readonly-observation","native-screen-capture","owned-temporary-cleanup"]},"authorizationSource":"POLICY","taskId":"AI-000303"}
## AI-000305 | [CODE] [WF] AI303冒烟两处动作常量参数修正

- 序号：305
- 提交时间：2026-10-21T23:49:44.7607959+08:00
- 模块 ID：WF
- 初始状态：QUEUED
- 前置依赖：无
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：S / 40

### 指令

精确输入 .ai-workspace/inputs/派发/AI289-CLI-REPAIR-20261021/AI305_冒烟动作参数修正.txt、AI305_SCOPE.json及inputs/acceptance/AI-000305.json。仅自有输出的两候选参数/静态diff；不生产源码/Unity/新runner。原303同根第2周期CODE修正，独立通过后配对303，不消费289。

> SUPERVISED POLICY at 2026-10-21T23:49:45.2835024+08:00: {"policy":{"resourceKeys":["unity-project","workflow-devtools"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"原303a2真实载荷动作参数错误，经CONTROL14事实核查；仅两处候选常量修正/有限静态diff，原生产及失败载荷只读，CODE先独立通过后同cycle配对303，根额度累计。","updatedAt":"2026-10-21T23:49:45.2153434+08:00","supervisionLevel":"full"},"taskId":"AI-000305"}

> SUPERVISED EXTEND at 2026-10-21T23:49:45.4644543+08:00: {"taskIds":["AI-000305"],"reason":"用户已批准修复轨继续及SUP5；303自身管线payload修复第2周期，不消费289，保留后继和最终人工门。"}

> SUPERVISED RESUME_APPROVED at 2026-10-22T00:09:50.5437586+08:00: {"grantId":"fbf21d5c868b493e9b969388289d4ce5","used":2,"taskId":"AI-000303","cycles":["AI303:CODE305:retry2"],"rootTaskId":"AI-000303","verification":"CONTROL独立AI305三合同/8事实通过，review SHA D9F2FCA3A7F3FAD1170BFAF658DAA42439DE20200EF921F16E80866F9FDB2C25；实际app ENGINE revision70 idle/latest completed无活动轮，证据ENGINE-idle-before-303-resume2.json。仅原303 Scope与五合同，cycle AI303:CODE305:retry2配对不双扣。","remaining":3,"reserved":0,"authorizationSource":"POLICY","sourceApprovalId":"call_37410ba043344e9f81b4ce3ef3472a18:0","scope":{"paths":[".ai-workspace/outputs/AI289-CLI-REPAIR-20261021/AI-000303",".ai-workspace/outputs/DETACH-B13-20261019/AI-000303/isolated-saves"],"operations":["evidence-write","idempotence-check","isolated-validation-play","native-readonly-observation","native-screen-capture","owned-temporary-cleanup"]}}

> SUPERVISED USER_GATE_APPROVED at 2026-10-22T00:25:43.3138962+08:00: {"taskId":"AI-000303","userDecision":"AI289-CLI-REPAIR-20261021-user-request 用户原话：接收相关的修复派发指令，维持回到监管模式，修复轨任务都完成后续行原本的289任务并继续推进后续的待完成任务链；条件已由CONTROL独立5合同/13事实验收原生SMOKE满足，review SHA4E2D11CF5970FC6883FCBA250071450D3F5F4541EBAFCEED8EEA527950BFC9B4。仅修复轨内部执行门，非A+C/USER-U5人工视觉验收。"}

> SUPERVISED RESUME_APPROVED at 2026-10-22T00:25:44.8834542+08:00: {"scope":{"paths":[".ai-workspace/codebase/shortcycle.md",".ai-workspace/outputs/DETACH-B13-20261019/AI-000289","项目整体阅读理解与推进基线_2026-08-14.md","CODEBASE_MAP.md"],"operations":["documentation-update","evidence-write","idempotence-check","unity-game-view-capture","unity-isolated-play-validation","unity-owned-temporary-cleanup","unity-target-check"]},"reserved":1,"sourceApprovalId":"AI289-CLI-REPAIR-20261021-user-request","grantId":"e18671a184bc4c3c834ffc4054ba9e37","rootTaskId":"AI-000289","taskId":"AI-000289","cycles":["grant:5649470492f5473fb2f0d7562c49038e"],"used":8,"authorizationSource":"HUMAN","remaining":0,"verification":"用户条件式许可原件SHA D63C20820D078630031A264681705F6A8292C09F8094AC50D7EFB62B9F15CB02；296至301及304305独立通过，SMOKE303原生五合同13事实独立通过review SHA4E2D11CF5970FC6883FCBA250071450D3F5F4541EBAFCEED8EEA527950BFC9B4。fresh固定ENGINE app revision72 idle/latest completed无活动轮。原289同补三截图Scope只追加1次，原9Fail/used8保持；独立管线卡不伪PairedRepairTaskId。"}

> Dependency correction for AI-000289 at 2026-10-22T00:26:54.2888800+08:00: AI-000288, AI-000287, AI-000281, AI-000284, AI-000273, AI-000303

> SUPERVISED POLICY at 2026-10-22T00:26:55.4866605+08:00: {"policy":{"resourceKeys":["unity-project","documentation","data-batch"],"reviewDependsOn":["AI-000303"],"humanGateAfter":false,"gateApproved":false,"reason":"用户2026-10-21条件式原范围续行已满足：独立修复296至301及原生SMOKE303 CONTROL通过；保留旧依赖与共享锁，仅追加303执行验收边，最终292 A+C和USER-U5不变。","updatedAt":"2026-10-22T00:26:55.3825893+08:00","supervisionLevel":"full"},"taskId":"AI-000289"}
## AI-000306 | [CODE] [WF] AI289管线修复 未消费许可严格重新核验

- 序号：306
- 提交时间：2026-10-22T14:18:36.8035236+08:00
- 模块 ID：WF
- 初始状态：QUEUED
- 前置依赖：AI-000303
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：M / 80

### 指令

AI-000306 独立管线修复：未消费许可的显式重新核验
固定CODE，WF，M80。技术WorkKind=FEATURE、semanticKind/usageCategory=pipeline-repair、线标CLI(磨合)，关联原AI289但不是同根repair，不消耗/退款289。前置303已独立通过。用户直接批准call_c239e3e041ab4eccbfbbaf707a7c6b8b:0“批准绑定修复并使用原许可继续”，原件本目录AI289未消费许可绑定修复_用户批准.json绑定控制审查单SHA。正式领取后读CORE/CODE及WF approve-resume相关节、项目两入口页首/工作流相关节，git定向状态；不全仓重读。

只改AI306_SCOPE.json列5源码/文档及本卡输出，原文件已有前卡改动必须保留。先保全5文件精确before、SHA，再查看原函数和既有回归。CONTROL审查单与AI289-unused-grant-binding-proof.json都在outputs/control/AI289-CLI-REPAIR-20261021。原289领取前被拒，仍QUEUED9attempt/9failure、原grant used0/1，不能伪Fail或代为操作原289。

实现仅在既有ApproveResume显式opt-in增加严格重新核验分支，不新入口、不修改普通claim的指纹检查，不自动随Configure/依赖变化更新许可。参数命名由最小兼容实现决定，交接给出完整精确调用例。显式绑定本次真实人批文件/ID/原话/审查单和前后证据SHA；原业务ApprovalId/UserDecision保持，不冒充新次数。读候选当前定义时在正式queue锁内重新核实。

必须保持同一原grantId/cycleIds/runs/usedRuns/claims/expectedAttempt/failureHistorySha256/原来源历史与root计数。仅未消费：QUEUED、usedRuns0、claims空、原cycle未消费且仍预留，无RUNNING/活跃owner/预约，原session ACTIVE/stopfalse/exitfalse，固定原route/owner/Scope全相同。旧定义重新计算须匹配原grant fingerprint，当前定义重新计算须匹配审定新定义。只可增加已独立通过的dependsOn/reviewDependsOn及相应reason/updatedAt；原普通依赖/review边必须保留，其他定义字段精确保持。新增review必须是直接依赖，且原生/独立验收证据实际通过及正式gate成立，不能信任调用者自写true。禁止扩大instruction/title/pipeline/art字段/resourceKeys/supervisionLevel/业务scope/人工门或更改已批范围；缺证据/未知字段变更拒绝。不要硬编码AI289/303为绕过规则。

保全旧fingerprint到正式审计，记录本次revalidation批准来源/证据SHA与新fingerprint；幂等重复不得追加次数或重复消费。已消费或历史变动、错身份、停机、未验依赖、缺批准/旧新证据漂移均拒绝。不得放宽Assert-BoundedResume；若仅修改metadata说明时间，应只和已批新增依赖一起允许，不泛化为任意政策编辑。

只沿用DevTools/Workflow/Test-ApprovedResume.ps1既有隔离fixture，不建新runner/续行入口，不读写生产runtime。增加真实正式API情景：旧grant→正式追加已验依赖→旧Poll拒绝→显式同grant重新核验→唯一claim消费；幂等及以上反例；旧普通续行保留。Final用现有Verify-All Custom/该runner，按真实失败次数RecordCodeValidation同稳定key AI306_UNUSED_GRANT_REBIND，首次失败最多两次局部修复，达到门限正式停止并即时回传，不换key/task自动续行。不要把测试代码复制生产实现当独立断言。

交付REPORT.md、evidence.json六合同、原始runner日志、5文件before/candidate与SHA、明确CONTROL-CALL-EXAMPLE含所需新证据schema/参数、只读的本次289匹配判断。不得对生产289调用ApproveResume/Configure/Dispatch/Poll；CONTROL独立验收后才执行。无需Unity/CLI/MCP/Play/Build/资产、账户采样、任务外Git操作或历史用量回填。任务生命周期走正式API，Complete/Fail/Stop后按Prepare→一次真实发绑定CONTROL→Record。包目标1MB硬10MB，完整文本/失败留存，超1MB说明必要性。

> SUPERVISED POLICY at 2026-10-22T14:18:37.4268910+08:00: {"taskId":"AI-000306","policy":{"resourceKeys":["workflow-devtools","documentation"],"reviewDependsOn":["AI-000303"],"humanGateAfter":false,"gateApproved":false,"reason":"用户call_c239e3e041ab4eccbfbbaf707a7c6b8b:0批准独立CODE绑定修复；303已独立通过，保留原289未消费许可，CONTROL验后才正式重新核验原grant","updatedAt":"2026-10-22T14:18:37.3487841+08:00","supervisionLevel":"full"}}

> SUPERVISED EXTEND at 2026-10-22T14:18:37.5823460+08:00: {"taskIds":["AI-000306"],"reason":"用户明确批准绑定修复与使用原许可继续，同session追加独立pipeline-repair卡306，原289次数范围不变"}

> SUPERVISED RECONCILE at 2026-10-22T14:19:14.0157847+08:00: {"repairs":["REQUEUE_UNCLAIMED_DISPATCH:AI-000289"],"evidence":"真实app当前CODE revision198 notLoaded/末轮completed；ENGINE revision74 idle/末轮completed。289唯一领取前指纹拒绝、没有claim，旧dispatch已过期。用户已批准306绑定修复；此次仅释放过期派发，不重发289。","inspection":[]}

> SUPERVISED APPROVED_CONTINUATION_REFUSED at 2026-10-22T15:19:42.7072594+08:00: {"controllerThreadId":"01a13ef5-eda9-7882-b1c9-eca47a6470d9","reason":"Exception calling \"GetBytes\" with \"1\" argument(s): \"Value cannot be null. (Parameter 'chars')\"","problemKey":"","taskId":"AI-000289"}

> Verification rejected for AI-000306 at 2026-10-22T15:23:00.9451536+08:00: decision=REJECTED; reason=Explicit revalidation cannot handle actual approved null collections; valid unused original grant fails before write.
## AI-000307 | [CODE] [WF] AI306未消费许可空值与集合摘要修复

- 序号：307
- 提交时间：2026-10-22T15:26:33.0997242+08:00
- 模块 ID：WF
- 初始状态：QUEUED
- 前置依赖：无
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：M / 80

### 指令

精确输入 inputs/派发/AI289-CLI-REPAIR-20261021/AI307_未消费许可空值集合修复.txt；Scope AI307_SCOPE.json；合同 inputs/acceptance/AI-000307.json。同根306 REPAIR/POLICY，pipeline-repair，实际旧定义[null]/空数组摘要兼容，原289未消费许可和请求保持；不操作原289生产API。

> SUPERVISED POLICY at 2026-10-22T15:26:33.6717441+08:00: {"taskId":"AI-000307","policy":{"resourceKeys":["workflow-devtools","documentation"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"Within approved binding repair; same-root306 null collection integration rejection, no new289 quota or scope.","updatedAt":"2026-10-22T15:26:33.6002538+08:00","supervisionLevel":"full"}}

> SUPERVISED EXTEND at 2026-10-22T15:26:33.8151962+08:00: {"taskIds":["AI-000307"],"reason":"Necessary same-root306 pipeline repair after actual unchanged-state revalidation failure."}
> Control rejection resolved AI-000306 : {"at":"2026-10-22T16:26:38.7385183+08:00","decision":"CONTROL_REJECTION_RESOLVED","problemKey":"UNUSED_GRANT_NULL_COLLECTION_DIGEST","repairTaskId":"AI-000307","controllerThreadId":"01a13ef5-eda9-7882-b1c9-eca47a6470d9","userDecision":"批准绑定修复并使用原许可继续","verification":"5 contracts, 14 independent facts, 334 existing-runner checks / 390 events; strict structural digest preserves null/empty/single-null collection shape; historical digest/fingerprint/claim unchanged. Empty [] caused missing JSON; [null] collision separately preserved in correction. Original approved grant/proof/request SHA unchanged.","evidencePath":"D:\\GameProject\\Eat-What\\.ai-workspace\\outputs\\control\\AI289-CLI-REPAIR-20261021\\AI306-control-rejection-resolution.json","evidenceSha256":"627CAD85EEEEACCD0DA9695B94BA764E56E03983AE96149A7BA51F6B978900AA","rejectedResultSha256":"1F1773C2DF206BD534C2CF9342D1EA85A14520836381414EA1C96EBE66470C27","repairResultSha256":"C233A494DBC4BE65348C9EE65086DF92352CDDC364B4E37A838589FD8D181AF2"}

> SUPERVISED UNUSED_GRANT_REVALIDATED at 2026-10-22T16:27:52.3931925+08:00: {"approvalId":"call_c239e3e041ab4eccbfbbaf707a7c6b8b:0","grantId":"e18671a184bc4c3c834ffc4054ba9e37","approvalPath":"D:\\GameProject\\Eat-What\\.ai-workspace\\inputs\\派发\\AI289-CLI-REPAIR-20261021\\AI289未消费许可绑定修复_用户批准.json","approvalSha256":"2010A47482052D9BA7E4E423EF58C54A47669CCB5A645A973D2D596EA715AA33","evidencePath":"D:\\GameProject\\Eat-What\\.ai-workspace\\outputs\\control\\AI289-CLI-REPAIR-20261021\\AI289-unused-grant-revalidation-request.json","evidenceSha256":"0F99A59364D730E99D78BDCEB686E3A784148E29623B931ECE748013198BE8A7","proofPath":"D:\\GameProject\\Eat-What\\.ai-workspace\\outputs\\control\\AI289-CLI-REPAIR-20261021\\AI289-unused-grant-binding-proof.json","proofSha256":"C6B9730190C718EFEFB596DF7A768918BFA52C5E72A7AC988F60F5586803B535","oldFingerprint":"DA-B4-DD-3E-57-29-49-4C-02-CE-B0-6D-FE-32-E2-3F-30-87-47-2F-80-26-A3-BB-8C-46-66-42-5C-00-9A-9D","newFingerprint":"91-61-A1-89-C4-25-A6-E5-87-A7-00-15-43-68-76-45-4F-8B-9F-6E-3D-8B-EF-AD-CA-FA-0F-C1-F6-C8-39-08","at":"2026-10-22T16:27:52.3231832+08:00","userQuote":"批准绑定修复并使用原许可继续","reviewPath":"D:\\GameProject\\Eat-What\\.ai-workspace\\outputs\\control\\AI289-CLI-REPAIR-20261021\\AI289_未消费许可绑定修复审查单.md","reviewSha256":"FF104E928E8B9AA31EC4732E59E13FEC37B249C655B3E6E584FF840AB13FD384","stateSha256":"43841216D835E5E4953723F3D91B3275F34A6C705BEC7E24625BF0FF8545AE73","controllerThreadId":"01a13ef5-eda9-7882-b1c9-eca47a6470d9","verification":"Fresh app ENGINE_CLI revision74 idle/completed; engine-idle-before-revalidation-after-307.json. AI307 independently PASSED 5 contracts/14 facts/334 checks; formal resolve-306.json completed. Same original immutable request and user approval; no new quota."}

> SUPERVISED RECONCILE at 2026-10-22T16:43:29.6156835+08:00: {"repairs":["REQUEUE_UNCLAIMED_DISPATCH:AI-000289"],"evidence":"Fresh app revision76 latest turn failed capacity, inactive/systemError no active turn. Actual289 stillQUEUED attempt9 grant0/1. Original ticket expired; user call_9c659d1224a3457e9e7d05d5bf9d0bb9:0 explicitly approved same-model resend once. No model override or new business grant.","inspection":[]}

> SUPERVISED RECONCILE at 2026-10-22T18:08:38.0776982+08:00: {"repairs":["REQUEUE_UNCLAIMED_DISPATCH:AI-000290"],"evidence":"Actual app wait_threads revision84 idle/completed, prior turn01a162f3-256d-7261-b748-42a45fd9fcc1 ended with no platform error. Evidence heartbeat-20261022T100433776Z/engine-fresh-idle.json. AI290 QUEUED attempt0 executionnull errors0, workerENGINE_CLI IDLE/no active task. Old token b01cd05a43b7441fb459eca789a87fe4 expired before single Poll; ingress file preserved. A independent PASSED and prior B approval remain. No business Fail, no quota consumption, no runtime edits.","inspection":[]}
## AI-000308 | [CODE] [WF] G87 CLI SceneHandle证据调用修复（关联AI290，独立pipeline-repair）

- 序号：308
- 提交时间：2026-10-22T19:05:55.6942631+08:00
- 模块 ID：WF
- 初始状态：QUEUED
- 前置依赖：AI-000298, AI-000304
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：S / 40

### 指令

AI-000308 · CLI SceneHandle证据调用修复 · 固定CODE · S/40 · semanticKind=pipeline-repair · lineLabel=CLI(磨合)
原业务来源AI-000290；本卡是G87独立管线修复根，用FEATURE技术类型承载，不伪造同290 repairContext，不消费/退款290实际历史。用户已授权继续既有B链、G87管线失败独立CODE→必要SMOKE→原卡正式续行；长期SUP5和所有硬门保持。scope见本目录AI308_SCOPE.json，五合同inputs/acceptance/AI-000308.json。

先读CORE、CODE worker、WF相关证据/离线/结算条目及P056，不全量重读历史。事实入口outputs/control/DETACH-B13-20261019/independent-failure-review-290-attempt1.json，15项事实复核，不是任务通过；原290 attempt1 execution6bb887ca407d4dcf9d2049090ccbcfe9，root0/5，只有2/5合同。四原生调用仅status/editor_status/preflight编译失败/console_status；PrepareSnapshot0/staging无/生产写0，API是否已加载未知，不是已证产品导出或项目编译缺陷。原始未结输出不可覆盖。
原件outputs/DETACH-B13-20261019/AI-000290/attempt1/native-preflight.cs.txt（SHA612D4508A02119B34679FF601C5208D2D365AF7E7B0667F59095C16D2F754F8A）把s.handle直接赋JObject；未执行prepare-snapshot.cs.txt的sourceState同样scene.handle直接赋值。实际Unity6000.3.24f1 Scene.handle返回UnityEngine.SceneManagement.SceneHandle，原编译拒绝隐式转JToken。

任务范围：
1. 只读核实际Unity6引用/公开SceneHandle标量API，选择保持真实场景身份的显式标量投影；不能用GetHashCode/对象ToString不明语义/丢字段/固定值/默认null/ReflectionLoopIgnore掩盖。把确证API、类型及最小正反例写交接。
2. 在本卡输出生成两份原调用载荷的最小修正版，不修改290旧原件。除了SceneHandle标量兼容以及明确必要的当前task/attempt/execution/output/staging参数化，原业务代码/前置/未知dirty/journal/fresh-staging/认证回读/实际Catalog-Sprite捕获/前后无漂移断言完整保留。两份完整语句体均需按现有官方eval Execute上下文、真实UnityEditor/CoreModule及Newtonsoft引用离线编译，不只测孤立一行。原失败载荷作为离线预期编译失败负例，不能当实际重新调用Unity。仅静态核反射类型/方法名与当前实际源码一致；发现另一个实质范围问题给明确证据，不顺手改产品实现。
3. 复用既有Test-EngineCli.ps1 EvidenceJson分支，必要最小扩充同文件用例和已有UnityEvidenceJson.cs；若直接公开标量转换已足够，无需为此新增helper API。禁止新runner/夹具入口/恢复入口，不全量All/ProtocolRepair重跑；保全所有旧组/断言，新增最少实际SceneHandle身份投影与两份完整caller编译验证，证明原坏例失败且修正版通过。不得复制实现代替原载荷测试、硬编码PASS、绕过严格JSON/PNG/授权门。仅本卡源码与相称离线验证；记录原真实失败，不为一次性载荷建专用长期runner。
4. WF同原条目补“Unity6 SceneHandle需先投影标量，JObject构建期错误无法由后续converter兜底”的精确用法。无需改root项目核验日期、生产Assets/Exporter/Catalog/SAVE/scene/meta、queue/Importer/运行态或PackageCache。API定义/默认行为/原已验helper功能保持。
5. 交付可直接复用ENGINE-HANDOFF和最小只读SMOKE方案：使用既有官方eval及实际helper载入方式；只核场景handle标量/已加载PrepareSnapshot签名/只读前置，不执行PrepareSnapshot/Play/Build/Refresh。CODE不调用Unity/CLI/MCP，也不替SMOKE写PASS。CONTROL先独立通过，再正式安排固定ENGINE最小SMOKE；其通过后才按长期POLICY唯一ApproveResume原290原范围，独立FEATURE修复不能伪作PairedRepairTaskId。

治理：三个允许共享文件最多，S40与任务规模相称；改动前保全，受保护快照源码只读不修改；不全仓哈希/内存导出，不部署/提交/推送。正式生命周期API可用，测试状态仅隔离fixture，禁止生产runtime手改。稳定key SCENEHANDLE_CALLER_OFFLINE；首次验证失败最多再两次局部修正验证，达到既有CODE_VALIDATION_STOP即正式结算回传，不执行器自行续行或换key。包目标1MB/硬10MB，原文本/失败/必要快照完整保留，>1MB说明。实际工具与pipeline-repair分类、预算/真实provider、app turns未知写unknown，不伪造账单。
输出.ai-workspace/outputs/AI290-CLI-CALLER-REPAIR-20261022/AI-000308：REPORT六要素、原/候选小快照、真实离线日志、evidence/acceptance、完整最小差异、可复用调用与ENGINE-HANDOFF。Complete后立即Prepare→一次实际app Send→Record给绑定CONTROL，Receive不等于验收；不得自领后继或原290。

> SUPERVISED POLICY at 2026-10-22T19:05:56.0951398+08:00: {"taskId":"AI-000308","policy":{"resourceKeys":["workflow-devtools","documentation","unity-project"],"reviewDependsOn":["AI-000298","AI-000304"],"humanGateAfter":false,"gateApproved":false,"reason":"G87 independent pipeline caller repair under existing B task/standing SUP policy, exact source/offline scope only. Original290 failure and root0of5 retained; CODE independent pass then minimum readonly ENGINE smoke, then original unique ApproveResume.","updatedAt":"2026-10-22T19:05:56.0398220+08:00","supervisionLevel":"full"}}

> SUPERVISED EXTEND at 2026-10-22T19:05:56.2303593+08:00: {"taskIds":["AI-000308"],"reason":"Authorized B continuation necessary G87 independent pipeline repair for exact AI290 SceneHandle caller compile failure; no exporter/production scope expansion, no human gate waiver."}

> SUPERVISED USER_GATE_APPROVED at 2026-10-22T19:12:34.0893482+08:00: {"userDecision":"依据用户2026-10-21原话：接收相关的修复派发指令，维持回到监管模式，修复轨任务都完成后续行原本的289任务并继续推进后续的待完成任务链。仅登记已有304独立5合同16事实274断言通过后的内部执行依赖门，review SHA50D73A1F7B2D76FCA15FFC64884E88E19D4F7B0B088DCAC0FCB625E0ACE672E4；非新增人类视觉或产品验收，不触及292/A+C/USER-U5。来源inputs/派发/AI289-CLI-REPAIR-20261021/用户授权与覆盖关系.json及AI290-CLI-CALLER-REPAIR-20261022/控制范围依据.json。","taskId":"AI-000304"}

> SUPERVISED USER_GATE_APPROVED at 2026-10-22T19:34:34.5637392+08:00: {"taskId":"AI-000308","userDecision":"依据用户2026-10-21原话：接收相关的修复派发指令，维持回到监管模式，修复轨任务都完成后续行原本的289任务并继续推进后续的待完成任务链；G87管线修复/必要SMOKE顺序。AI308 CONTROL独立五合同15项、一次EvidenceJson18检查/168真实结构体与句柄断言通过，详见outputs/control/AI290-CLI-CALLER-REPAIR-20261022/independent-review-308.json。本门仅具体已批内部执行验收依赖，不是新的人工视觉/产品批准，不解除292/A+C/USER-U5。"}
## AI-000309 | [ENGINE_CLI] [WF] AI290管线SceneHandle修复最小只读SMOKE

- 序号：309
- 提交时间：2026-10-22T19:38:23.1578554+08:00
- 模块 ID：WF
- 初始状态：QUEUED
- 前置依赖：AI-000308
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：S / 40

### 指令

AI309 · SMOKE-CLI-SCENEHANDLE · 固定 ENGINE_CLI · S40 · 独立 pipeline-repair / CLI(磨合)，关联原业务AI290。此卡只读，无Play/场景变更，使用现有固定chat，用户免重载有效。
授权依据：既有用户继续修复轨及后继/G87必要SMOKE，控制范围依据.json；AI308已Complete/Receive并CONTROL独立5合同、15事实通过（independent-review-308.json SHA B24DCC9B0303C58D14A12D986B3FB0CF6998E469308E2B695178E2EB4C66082F）。原290首Fail保持，未签原卡续行。此独立FEATURE技术根不作为PairedRepair，不退款/消耗289，不伪新人批。
先唯一Poll正式领取，复用已读共同入口，不全量重读历史；仅补本卡Scope/4合同与AI308 ENGINE-HANDOFF.md、candidate-preflight.eval.cs。使用原已验官方CLI和Invoke-TaskTool，UsageCategory=pipeline-repair / ToolSource=unity-official-cli，真实TaskId/OwnerThreadId/ExecutionId及监管checkpoint绑定本卡Scope；输出根本卡attempt1，fresh不覆盖。
1. status和editor_status各至多一次，保持现有30秒命令/35秒进程超时及无重试。只选D:/GameProject/Eat-What、Unity6000.3.24f1、当前唯一PID/port/新鲜heartbeat，clean idle Edit；若身份/版本/新鲜度不匹配或无法判定停止，不靠旧7800/PID猜测。既有command eval用同一目标，不换MCP/工具。
2. 核AI308 candidate-preflight.eval.cs SHA7FDB37BEBA27EF7C14D4CDF528E3DA2B7F8F519FFDCE463D988EF278B8DA707E，复制到本卡输出绑定且仅绑定头部__TASK_ID__、attempt=0、__EXECUTION_ID__为本次真实claim。完整原业务body保持，保存来源SHA/精确diff；不要运行candidate-snapshot.eval.cs。经既有官方eval_file/file参数调用完整载荷一次（沿原290调用方式，只有已批准标量修复）。不改原载荷文件、helper、wrapper或项目源码。不要启动验证宿主、Arm/Play或StartupReady（本卡是Edit只读检查，无宿主启动期重试）。
3. 必须核CLI默认完整外层success/command/data/errors/warnings、data.command/parameters/result/target及eval内层success/result/output/diagnostics，退出码0/单JSON/真实项目port/回显载荷；不能递归猜result或用--result-only。原双流和SHA/长度保存；核返回claim三项、pid/project/unityVersion，playing/compiling/building=false、scenes各path/Int32 handle/dirty=false、prepareSnapshotLoaded=true、method精确Void PrepareSnapshot(System.String)、assembly和MVID非空。句柄只当前快照内有效，不与历史句柄匹配；不得GetHashCode/固定常量/删字段或忽略错误。preflight直接返回标量DTO，不需要加载helper DLL或复制serializer。
4. 现有dirty场景/未结journal/缺已加载类型或方法/动态编译错误立即原始保全并正式Fail，不Refresh/Compile/Import/Play/Build/Open/Save/尝试API修复。不执行PrepareSnapshot（含测试性调用）、不触碰Catalog导出/生产Fingerprint/StreamingAssets/真实用户存档或旧已验P2/289探针截图。既有载荷只读已加载scene/API/journal；所有文件写入只本卡输出。
5. 一次console_status核当前compile状态和错误事实；无连续起点不声称新增Error0，不清Console。记录总调用数和实证读取结果、无业务API调用/输出staging；不能用CLI退出0或离线结果代本卡4合同，不能把“只读冒烟成功”当290快照成功。结果仍有不确定项须如实失败或待CONTROL判断，不删减合同完成。
交付：REPORT六要素、evidence.json、acceptance.json、实际bound preflight与SHA/精确diff、全部原CLI双流/回执、原生API/句柄标量与干净Edit证据；输出目标1MB硬10MB，完整文本/失败保留。无需截图/全路径哈希/重复导表/临时探针或新runner。实际tools/provider与appturn未知写unknown。正式失败先结算回传，不自行续行或原290；Complete/Fail立即Prepare→一次真实appSend→Record给绑定CONTROL。CONTROL独立4合同通过后才按现有SUP5唯一ApproveResume原290原范围，后继DATA/Build均不由本线领取。

> SUPERVISED POLICY at 2026-10-22T19:38:55.0210152+08:00: {"policy":{"resourceKeys":["unity-project","workflow-devtools","documentation"],"reviewDependsOn":["AI-000308"],"humanGateAfter":false,"gateApproved":false,"reason":"G87独立管线只读SMOKE，308五合同/15事实独立通过；只现有完整preflight、真实句柄标量及loaded API签名/clean Edit，不调用快照/Play/Refresh。","updatedAt":"2026-10-22T19:38:54.9260364+08:00","supervisionLevel":"full"},"taskId":"AI-000309"}

> SUPERVISED EXTEND at 2026-10-22T19:39:30.3036143+08:00: {"taskIds":["AI-000309"],"reason":"AI308独立通过后的精确G87只读SMOKE；关联290，仅loaded scenehandle/API preflight，不执行快照/产品写入，原290待其独立通过后唯一ApproveResume。"}

> SUPERVISED RESUME_APPROVED at 2026-10-22T20:10:27.6033784+08:00: {"cycles":["AI290:retry1"],"taskId":"AI-000290","reserved":1,"used":0,"sourceApprovalId":"call_37410ba043344e9f81b4ce3ef3472a18:0","grantId":"b2b092e5a4674eb383c51582902059c2","verification":"本轮fresh app ENGINE revision89 idle/latestTurn completed/error null，原290首轮正式Fail/Receive且root used0/limit5，CONTROL独立308五合同15事实及309四合同13事实通过，review309 SHA926C6C9BAA99D4787439255BD40195D90BB0592E686CD54CD3E3ADAE24B97F43。全部scope无运行/预约；只原卡原Scope一次真实快照。独立FEATURE308/309不伪PairedRepair，不改任务定义或历史。","rootTaskId":"AI-000290","scope":{"paths":[".ai-workspace/outputs/DETACH-B13-20261019/AI-000290"],"operations":["evidence-write","idempotence-check","isolated-catalog-export","unity-catalog-readonly-snapshot","unity-target-check"]},"remaining":4,"authorizationSource":"POLICY"}
## AI-000310 | [CODE] [WF] AI290快照反射失败离线只读诊断

- 序号：310
- 提交时间：2026-10-22T20:48:14.3860078+08:00
- 模块 ID：WF
- 初始状态：QUEUED
- 前置依赖：AI-000308
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：S / 40

### 指令

AI-000310 · 原AI290快照反射失败离线只读诊断 · 固定CODE · S/40 · READONLY_DIAGNOSIS/ParentTaskId AI-000290；usageCategory=pipeline-repair，CLI(磨合)，不是已确证管线根因，不新增原业务续行、不计诊断为重试、不退款旧1of5。
授权为现有监管继续B任务链、必要只读诊断/G87分层；不需要Unity/真实快照执行。正式Scope仅本卡输出，禁止生产源码、共享工具、runtime手改。先读CORE/CODE worker和相关WF；只按需读原始证据，不全量历史。
CONTROL事实：outputs/control/DETACH-B13-20261019/independent-failure-review-290-attempt2.json SHA37D6ABB6169AABBBCFF332D8EB9B4BA95D06AD6E55500809208AE9E433546E54，16事实/五合同2过。原attempt2 execution715dbecb4acf493fa3e9dbe0581f5389，5原生调用，前置成功，PrepareSnapshot反射一次CLI exit6，只外层TargetInvocation错误；source-before/after原字节相同170SO/14表/dirty0，staging-a2与四件不存在。不能把缺产物直接判Capture前失败，Export也有自有失败清理；不能推断无调用/无瞬时克隆。原两失败/所有许可保持。任务目录outputs/DETACH-B13-20261019/AI-000290/attempt2含完整调用/双流/两个原载荷，原件只读。
1. 只读完整原始失败、当前CatalogBlobBuildProcessor.PrepareSnapshot→Capture/Rehydrate→CatalogBlobStaging.Export→认证路径，以及实际CatalogBlobSerializer与用到的数据类型。核真实现有SDK反射异常处理仅解一层的事实，读取必要现成Editor日志中的同时间段错误（仅已存在磁盘，不Unity调用）。确证与推测分开，不把SDK外层Runtime Error误判编译、权限拒绝或特定产品缺陷。无证根因可以明确unknown并列排除边界，不能为Complete猜根因。
2. 根据实际代码/序列化资源只读定位最小候选与影响。尤其实际Unity Object null语义、已知MissingSprite保留契约、稳定GUID/fileID、全量字段支持、fresh路径/认证等边界按证据核对，不默认哪一项就是根因，不改非空配表/素材、不删除MissingSprite/字段/认证来绕过。原内层错误不可恢复时，准备一个只增加异常链观测的完整snapshot候选：本卡输出内复制原完整载荷，仅阶段标识/反射inner类型消息stack最小保全，并仍抛失败、保留原finally和所有原业务guards；成功路径/调用次数/认证/产物集合不能变，不能加业务重试/吞异常/输出PASS。不应用任何生产/共享源码修复。
3. 有可验证假设时使用已经存在的离线入口/可调用compiler，隔离输出作最小复核。无需强造回归；不能新建独立长期runner/fixture或改已有共享脚本。候选完整eval语句体以实际Unity6 Editor/CoreModule/Newtonsoft引用编译，明确这只是离线、不调用任何Unity API入口；为验证异常透传可用小型隔离反射合成例，证明一次调用、真实inner保留、failure仍失败/finally仍执行，不能将模拟报错说成原Editor根因。保留负例和实错，不全量All/全仓hash/内存导出。若现有入口不能完成且需共享实现改动，仅给具体差异候选供CONTROL另行定范围，不现场扩大。
4. 交付ROOT-CAUSE-ASSESSMENT.json（confirmed/unknown/hypotheses逐项证据）、REPORT六要素、原SHA/候选精确diff/必要离线日志、NEXT-STEP.json。后一文件给最小可审下一步：若需native诊断或原290续行，列唯一调用/读写/产物/权限/停止点；本卡不注册/执行下一步、不发grant、不自领后继。生产修复、原生SMOKE/PrepareSnapshot仍须CONTROL基于实际结论正式派发；不得把READONLY_DIAGNOSIS改成REPAIR实施。本卡成功条件为诊断材料可靠和边界完整，不是已解决原290或导出成功。
正式结算Complete/Fail立即Prepare→一次真实appSend→Record回绑定CONTROL。CODE不用Unity官方CLI/MCP/Refresh/Play/Build/导表/用户存档；实际appturn/bill不可观测写unknown，actualTool如实不以CODE车道推定code-cli。包目标1MB硬10MB、完整报告与失败保留。输出.ai-workspace/outputs/AI290-CLI-CALLER-REPAIR-20261022/AI-000310。稳定离线验证key AI290_REFLECTION_DIAG_OFFLINE，既有CODE三次局部失败门保持，正式失败先回CONTROL不能自重启。

> SUPERVISED POLICY at 2026-10-22T20:49:12.5653184+08:00: {"policy":{"resourceKeys":["unity-project","workflow-devtools","documentation"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"原290第二次失败根因未知，当前授权内固定CODE只读离线诊断；仅本卡输出候选，不修改生产或共享源码、不Unity、不续原业务。诊断不计修复周期；原290根1of5与历史保持。","updatedAt":"2026-10-22T20:49:12.4599035+08:00","supervisionLevel":"full"},"taskId":"AI-000310"}

> SUPERVISED EXTEND at 2026-10-22T20:49:12.8504145+08:00: {"taskIds":["AI-000310"],"reason":"同一已批B监管链必要READONLY_DIAGNOSIS，保留原290两失败/根1of5；无新增业务/原卡grant。"}

> SUPERVISED RESUME_APPROVED at 2026-10-22T21:17:36.0827141+08:00: {"authorizationSource":"POLICY","grantId":"bf7b94ef5c4a467c8e171273c2dbb26e","taskId":"AI-000290","scope":{"paths":[".ai-workspace/outputs/DETACH-B13-20261019/AI-000290"],"operations":["evidence-write","idempotence-check","isolated-catalog-export","unity-catalog-readonly-snapshot","unity-target-check"]},"rootTaskId":"AI-000290","remaining":3,"cycles":["AI290:retry2"],"verification":"ENGINE fresh revision92 idle/latestTurn completed/error null，实际AI290 attempt2已Fail/Receive root used1/limit5余4；AI310正式Complete/Receive且CONTROL独立四合同13事实 PASSED_DIAGNOSIS_NOT_ROOT_CAUSE_OR_REPAIR，review SHA E666F71997B7BF03D6EBA735E77A72CC836B0F825DA0DFAE5331ED280F13B541。只原Scope一次PrepareSnapshot，候选14行异常链观察已完整引用编译且原业务/finally保留。实际根因未知，不伪PairedRepair、不改定义。精确指令AI290_ENGINE续行2.txt。","reserved":1,"used":1,"sourceApprovalId":"call_37410ba043344e9f81b4ce3ef3472a18:0"}
## AI-000311 | [CODE] [WF] AI290 Sprite身份拒绝对象定位与只读观测准备

- 序号：311
- 提交时间：2026-10-22T21:50:28.3060981+08:00
- 模块 ID：WF
- 初始状态：QUEUED
- 前置依赖：AI-000310
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：S / 40

### 指令

AI-000311 · AI290 Sprite身份拒绝对象定位与只读观测准备 · 固定CODE · S40 · READONLY_DIAGNOSIS/ParentTaskId AI-000290。原根实际已用2/5，三Fail完整保留，本诊断不计重试、不签原290新grant。usageCategory=pipeline-repair/CLI(磨合)是排障成本，不能据此预判产品或管线根因。授权沿用户监管B继续及必要诊断/G87，Scope仅本卡输出，生产源码/共享工具/资产/runtime不修改。
CONTROL已正式Receive原attempt3并独立16事实复核，review outputs/control/DETACH-B13-20261019/independent-failure-review-290-attempt3.json SHA1F9373CBE65EE4D60838B9704D0156C6013F407D2D25201DCB0B526D2E6BED0A，原五合同仅2/5。原execution7bf8f58a5dd54cb6abd1070e4169242d，精确异常链位于outputs/DETACH-B13-20261019/AI-000290/attempt3/snapshot-exception-chain.json SHA454E2959669F72A794DE6AA6089B7FF421D979750948425CECA6BEF3B7614717；stage PrepareSnapshot.Invoke，TargetInvocationException→BuildFailedException Unstable catalog asset/Sprite identity。原生栈CatalogBlobSerializer.Encode:84 Sprite分支→BuildProcessor.Identity:38，guard asset==null或TryGetGUIDAndLocalFileIdentifier失败。具体asset/field/Sprite和哪个guard项未知，不能把MissingSprite名字、托管null语义猜测当确证。Capture未完成，未到Rehydrate/Export；本卡不得调用这些业务函数或再跑快照。原attempt2当时inner未知不回填。
1. 只读相关现源码CatalogBlobSerializer/CatalogBlobBuildProcessor、真实序列化数据类型与Generated受影响Sprite引用/必要meta及旧282的完整数据契约。定位磁盘候选资产、字段路径、序列化GUID/fileID/type与实际文件/子资产声明是否相符。以当前读到的引用为准，不全仓hash、不导纹理/内存快照。明确托管null、Unity fake-null、无稳定身份/非持久Sprite、既有MissingSprite本体等候选的可证与未知；GUID路径不存在或子资产未找到也不能静默清空生产非空值。已知原生MissingSprite本体必须保留，不弱化identity/auth/字段/数据集合。
2. 若磁盘不足以证明当前Editor对象身份，交一个完整、可编译的只读eval_file候选，供CONTROL审后另派ENGINE诊断；本CODE不执行native。复用308已验preflight的目标/clean Edit/无journal/SceneHandle显式int/当次task-attempt-execution绑定方法，新增观测只读取当前169dataSO的Sprite字段与必要List/嵌套字段（遵循真实Fields序列化规则），准确附asset path/GUID/localID/类型及字段路径。分别记录ReferenceEquals托管空、Unity null、instanceID（仅合法时）、AssetDatabase身份获取成功与GUID/fileID、asset path/持久性，结合必要SerializedProperty只读PPtr/instanceID和已核磁盘引用。不能用GetHashCode、无证ToString、任意类型递归或把Unity对象直接塞JSON。仅具体异常/不稳定引用明细和计数、必要正常对照；默认最多20条异常明细，更多以计数/明确截断标识和可定位索引保留，不能把截断当全通过。针对这些Sprite只读诊断不属于常规内存保护/纹理导出。
3. 完整候选必须同既有eval Execute上下文、真实Unity6 Editor/CoreModule/Newtonsoft引用离线编译；优先复用308/310已有compiler方式，不新增长期runner/fixture、不修改共享Test-EngineCli。保存完整语句体、wrapped源码、真实编译双流/来源SHA及明确待绑定头部。候选禁止PrepareSnapshot/Capture/Encode/Rehydrate/Export/创建销毁Unity对象/Refresh/Compile/Import/Play/Build/Save/SerializedObject.ApplyModifiedProperties等写入。字段读取异常仅如实记录为诊断失败或unknown，不改对象/吞错伪通过，不现场循环重试。
4. 输出ROOT-CAUSE-ASSESSMENT.json、SERIALIZED-SPRITE-CANDIDATES.json、完整native只读候选与编译证据、ENGINE-HANDOFF/NEXT-STEP（唯一eval范围/读写/精确输出根/停止条件）及REPORT六要素、evidence/acceptance。若有足够实证可提出最小修复diff仅候选，但不能应用生产，不能猜授权清空非空源值。诊断可明确待native，不用强造根因；成功条件是可靠对象候选、可审只读观测和真实编译。原生诊断后，CONTROL再定CODE产品修复或DATA/USER-U5门；本卡不注册后继/ApproveResume/业务重试。
正式输入/输出限定 .ai-workspace/outputs/AI290-CLI-CALLER-REPAIR-20261022/AI-000311；先读CORE/CODE worker及相关P/TK入口。CODE不Unity/CLI/MCP调用、不新chat/重载；包目标1MB硬10MB，不复制完整旧证据重复占包，旧失败原件保留。稳定验证key AI290_SPRITE_IDENTITY_DIAG_OFFLINE，既有CODE局部三失败停止门保持，正式失败先结算回传不得自续。Complete/Fail即时Prepare→一次真实appSend→Record给CONTROL，不自行领取后继。actualTool按观测、未知null，不以CODE车道写code-cli。

> SUPERVISED POLICY at 2026-10-22T21:51:17.0709395+08:00: {"policy":{"resourceKeys":["unity-project","workflow-devtools","documentation"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"原290第三失败已独立16事实：Sprite身份guard拒绝，具体对象/字段未知。仅已授权范围内固定CODE离线只读定位和候选编译，原根2of5保留，不执行原生/快照/产品或共享源码修复，不新增原290许可。310独立诊断已通过。","updatedAt":"2026-10-22T21:51:16.9663595+08:00","supervisionLevel":"full"},"taskId":"AI-000311"}

> SUPERVISED EXTEND at 2026-10-22T21:51:17.4433935+08:00: {"taskIds":["AI-000311"],"reason":"现有B继续及必要诊断授权，纳入Sprite对象定位311；只读诊断不重跑290业务，原范围/次数和最终人审保持。"}

> SUPERVISED RESUME_APPROVED at 2026-10-22T22:18:03.9117867+08:00: {"authorizationSource":"POLICY","rootTaskId":"AI-000290","sourceApprovalId":"call_37410ba043344e9f81b4ce3ef3472a18:0","cycles":["AI290:DIAG311:retry3"],"used":2,"reserved":1,"verification":"CONTROL independently reviewed AI311 CODE_STOP: 13 facts review SHA EB30C7C5654D1CCAD3A876F05B74C76E48CD27B56B7B1C27632AF0B34F54E13C. Current SUPERVISED ACTIVE/no stop, same original output scope. Fixed CODE fresh app revision250 idle/completed, no active turn, evidence code-before-311-resume1.json. Only failed diagnosis continuation compiler references and bounded readonly candidate; no original290 business run/production writes. Root290 used2of5 remaining3 verified, retain failures and actual API cycle consumption.","scope":{"paths":[".ai-workspace/outputs/AI290-CLI-CALLER-REPAIR-20261022/AI-000311"],"operations":["evidence-write","idempotence-check","offline-candidate-validation","readonly-source-and-evidence-inspection"]},"grantId":"300fab85db4343f892b6f65fa8db65de","taskId":"AI-000311","remaining":2}
## AI-000312 | [ENGINE_CLI] [WF] AI290 Sprite身份拒绝原生只读定位

- 序号：312
- 提交时间：2026-10-22T22:37:56.5641902+08:00
- 模块 ID：WF
- 初始状态：QUEUED
- 前置依赖：AI-000311
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：S / 40

### 指令

AI-000312 · AI290 Sprite身份拒绝的原生只读对象观测 · 固定ENGINE_CLI · S40 READONLY_DIAGNOSIS Parent AI-000290。用户原监管B与继续修复链已授权范围内必要诊断；同session，不新chat/模型/重载。AI311 attempt2已Complete/Receive且CONTROL独立4合同14事实通过，review outputs/control/AI290-CLI-CALLER-REPAIR-20261022/independent-review-311.json SHA FD26F1DAE8BED579067E41B9B69FC946DE7A884D5DB1BA9D72678E9BDC518899。完整候选已按真实Unity6/Mono/Newtonsoft七引用由worker及CONTROL各一次离线编译通过，未native。原290仍三正式失败/原五合同2/5；共享根现3of5余2包含已失败311续行，旧2业务周期与311首次0消费保留。本卡首次只读诊断不签原290新grant，不伪repair配对、不重新快照/退款。usageCategory pipeline-repair只表示排障成本，不预判产品或管线根因。
候选与交接：outputs/AI290-CLI-CALLER-REPAIR-20261022/AI-000311/attempt2/ENGINE-HANDOFF.md、candidate-sprite-observe.eval.cs SHA3009BBBF603562E2A8CE21CCF0687663FF252C6E7359C024A6CE6A93FB620CB7；相关原清单SERIALIZED-SPRITE-CANDIDATES.json SHA15811E360F02AFCBA8CE7B5CD5BC5A6D47C7A4D8DBF53EB9F80BE0FAF5B3819F。磁盘98字段78非空20空，13组都有对应文件，57引用本地MissingSprite已有localID，但不证明实际Editor引用/根因。必须保留全部非空数据和占位，不改数据/源码/资产。

唯一正式写入根 .ai-workspace/outputs/AI290-CLI-CALLER-REPAIR-20261022/AI-000312；本轮fresh输出根即该目录，不已有结果/失败文件/链接。status/editor_status各一次取得真实项目D:/GameProject/Eat-What/6000.3.24f1/当前实例PID/port/heartbeat与clean idle Edit，无忙/dirty/journal；只目标核对无Refresh/Compile/Import。按既有官方CLI共享wrapper权限/互斥/当前身份处理，UsageCategory pipeline-repair / ToolSource unity-official-cli。非启动期普通eval不启Ready400重试。
只替换已审候选前两行五常量taskId=本卡/attempt=实际claim/executionId=实际claim/output=本卡绝对精确根/approvedOutputRoot=同根；其余完整body逐字保存、核SHA/差异。不能携带311/290身份或自行修改逻辑/复制一半表达式。执行前正式SupervisionCheckpoint，Scope本卡只读/evidence；然后同官方eval_file执行一次完整候选，保存全载荷+wrapper回执+原stdout/stderr+原生结果/失败JSON。不得复制自定义runner、改共享工具或PackageCache/全局配置/端口。

候选读取169dataSO及真实Fields/List/LocTableEntry，记录owner path/GUID/localID、Sprite managedReferenceNull与UnityNull、合法instanceID/PPtr、TryGetGUID/fileID/path/persistent，最多20明细+总数/坐标/截断。只调用Fields和公开只读API；GetMethod PrepareSnapshot仅签名检查，绝不调用PrepareSnapshot/Capture/Encode/Rehydrate/Export/Create/Destroy/Refresh/Compile/Import/Play/Build/Save/ApplyModifiedProperties。不RunOriginalProbe/原1..20/旧289/快照，不实际存档/生产Fingerprint/StreamingAssets/数据；不加载场景或创建Unity对象。原serializedProperty读取与SerializedObject Dispose允许，不序列化内存快照/纹理。
正常OBSERVATION_ONLY不等于290通过。逐条把实际异常asset/field/owner与原磁盘清单对应，区分真正managednull、Unityfake-null、非持久或GUID查询失败；列实际观察和未知。没有异常也如实报告原因仍未明，不能为拿根因再跑snapshot或扩扫描。若异常>20，保留计数和坐标且明示未逐条全证，不二次eval补证。若当前文件实际漂移/类型未载/候选运行失败/平台拒绝/未知dirty，保全后立即正式Fail，同轮不修载荷/换工具/重试。

完成后console_status一次保留原生当前groundTruth与历史计数；没有连续起点不可声称新增错误0。候选原生scenes和入口状态、只读调用清单/受限git diff（相关Catalog源码/数据/源场景范围）的前后事实足够，不全仓路径hash、全texture/内存JSON。输出REPORT六要素/evidence/acceptance、NATIVE-SPRITE-IDENTITY.json或明确引用实际原结果、FIELD-CORRELATION.json（实际对象vs磁盘/原因unknown）、SOURCE-BOUNDARY.json、NEXT-STEP。结果/原流/失败完整保留，包目标1MB硬10MB，原生结构结果可无损压缩但原SHA/映射保留。四合同只验只读诊断有效，不验原290导出或A+C。
Complete/Fail当轮Prepare→一次实际appSend完整handoff给CONTROL→Record，执行器不自续/领取后继。CONTROL独立核实际观察后，才定CODE最小修复或DATA/USER-U5人工门及原290正式续行，尚未批准具体生产修复；291/292继续等待。未知账单/appturnactualTool保持null+原因，不以车道推断产品。

> SUPERVISED POLICY at 2026-10-22T22:38:34.3419582+08:00: {"policy":{"resourceKeys":["unity-project","workflow-devtools"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"311 independent4contracts14facts passed; exactly one native read-only Sprite identity observation, no snapshot/export or production writes. User supervised B and G87 necessary diagnosis authorization; scope only312 output; root290 actual3of5 unchanged by first diagnosis.","updatedAt":"2026-10-22T22:38:34.2423287+08:00","supervisionLevel":"full"},"taskId":"AI-000312"}

> SUPERVISED EXTEND at 2026-10-22T22:38:34.6256285+08:00: {"taskIds":["AI-000312"],"reason":"Same authorized B chain necessary precise native readonly Sprite diagnostic after311 independent acceptance; no business snapshot retry/no extra root allowance/no production repair authority."}
## AI-000313 | [CODE] [SEC-02] AI290已空Sprite的Unity空值语义修复

- 序号：313
- 提交时间：2026-10-22T23:07:58.7955770+08:00
- 模块 ID：SEC-02
- 初始状态：QUEUED
- 前置依赖：AI-000312
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：M / 80

### 指令

精确输入 inputs/派发/AI290-CLI-CALLER-REPAIR-20261022/AI313_CODE空Sprite修复.txt；Scope AI313_SCOPE.json；五合同 inputs/acceptance/AI-000313.json。同根290 REPAIR/POLICY cycle AI290:CODE313:retry4，312原生诊断独立4合同16事实通过；只最小空Sprite语义与非空身份保护、既有离线回归，不Unity/资产/原290业务。

> SUPERVISED POLICY at 2026-10-22T23:08:48.0958129+08:00: {"taskId":"AI-000313","policy":{"resourceKeys":["unity-project","workflow-devtools"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"312 independent4contracts16facts passed; narrow product null-Sprite repair under original290 SUPERVISED policy, cycle4, fixedCODE offline only. No new business or human gate approval.","updatedAt":"2026-10-22T23:08:47.9910788+08:00","supervisionLevel":"full"}}

> SUPERVISED EXTEND at 2026-10-22T23:08:48.4074169+08:00: {"reason":"Existing supervised B necessary same-root repair after312 actual diagnosis; original290/291/292 and final human gates retained.","taskIds":["AI-000313"]}
## AI-000314 | [ENGINE_CLI] [WF] AI313修复已加载接口只读核对

- 序号：314
- 提交时间：2026-10-22T23:40:06.7160000+08:00
- 模块 ID：WF
- 初始状态：QUEUED
- 前置依赖：AI-000313
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：S / 40

### 指令

AI-000314 · AI313修复的已加载接口只读核对 · 固定ENGINE_CLI · S40 READONLY_DIAGNOSIS Parent AI-000290。用户原监管B及后继修复链已授权必要诊断；同session/原chat，不重载/换模型。AI313已正式Complete/Receive且CONTROL独立5合同15事实通过，review outputs/control/AI290-CLI-CALLER-REPAIR-20261022/independent-review-313.json SHA28124AC615B00D2198C3A1E80176AFE4F705C88DCA6C710B2AD2089E787F314A。本卡仅确定Editor是否已自动装载新签名，不执行修复或快照，不签原290续行许可。原root290实际4/5余1（313成功REPAIR cycle AI290:CODE313:retry4已消费，之后原290须配对不双扣）；首次只读诊断不新增周期，历史失败保持。

唯一输出根 .ai-workspace/outputs/AI290-CLI-CALLER-REPAIR-20261022/AI-000314。精确候选 inputs/派发/AI290-CLI-CALLER-REPAIR-20261022/AI314-loaded-api.eval.cs SHA9B8FA3175CB46BA22C30A8DA11B5AC2880182B9EA52533B22431DE8F9F4F8B1E；CONTROL沿既有完整eval Execute载体、实际7引用离线编译exit0/stdout0/stderr0，证据outputs/control/AI290-CLI-CALLER-REPAIR-20261022/AI314-candidate/compile.json，未执行DLL/native。候选保留308完整preflight，追加只读GetMethod签名和程序集MVID；不复制半段表达式或自建runner。只替换第1行taskId/attempt/executionId为本次正式claim，其余body逐字保持，保存实际载荷和差异。

按既有官方CLI共享wrapper、当前task/execution/owner/Scope执行。UsageCategory task，ToolSource unity-official-cli；无启动Ready或普通eval400重试。status/editor_status各一次，核D:/GameProject/Eat-What、6000.3.24f1、当前PID/port/fresh heartbeat、clean idle Edit与无journal；若busy/unknown dirty/平台拒绝保全并正式Fail，不现场编译或重试。执行前正式SupervisionCheckpoint后官方eval_file一次完整候选，再console_status一次；保留4调用8原始双流/完整严格JSON及命令回执。没有连续Console起点不得声称新增Error0；只区分当前和历史。

观察CatalogBlobBuildProcessor私有static bool IsSerializedEmptySprite(ScriptableObject,string,Sprite)，CatalogBlobSerializer保留3参数Capture与新增4参数Capture，返回JObject且第4参数Func<ScriptableObject,string,Sprite,bool>。记录完整签名、各程序集名称/MVID及repairSignaturesLoaded真假，不调用任何这些方法。旧processor MVID fb204dbd-7941-405b-b1c3-635c06089b4e只历史；新签名能说明接口加载，不单凭签名推断整个程序集字节/本次业务必成功。新接口未加载或部分未加载而完整观测有效可Complete为OBSERVED_NOT_LOADED，不伪失败/不伪PASSED_LOAD；原既有PrepareSnapshot缺失/实际eval失败按真实Fail。不得为证明已加载调用proof/Capture/Encode/PrepareSnapshot/Rehydrate/Export，不读169SO/98字段、不重新快照或跑1..20。

仅两份相关生产源在前后核精确SHA，使用AI313清单：CatalogBlobSerializer.cs B2825BA990CD02A9BC0945F79C6228BEF9FA670AE2D92E185EF0E0D9DA763FDD；CatalogBlobBuildProcessor.cs D8CC12CB20E36910B7A5E92BD261B360B9F399A4C06CBB4B676152255D9E2C2B。受限git状态/diff及原生cleanEdit事实，不全仓hash或内存/纹理JSON。无Refresh/Compile/Import/Play/Build/场景加载保存/Apply/CreateDestroy/Assets与meta或数据写入、生产Fingerprint/StreamingAssets/真实存档；不改SDK/PackageCache/端口/全局配置。原场景已有用户修改保持。

输出REPORT六要素、evidence.json/acceptance、LOADED-API.json（实际原流对应）、SOURCE-BOUNDARY.json、NEXT-STEP；4合同验观测真实性/边界，不要求repairSignaturesLoaded必true。若未加载只提精确装载需求给CONTROL，不能自行扩Scope；若加载也停止等CONTROL独立验收和原290正式配对，不能自领取原卡。包目标1MB硬10MB，未结失败/原流不删。Complete/Fail当轮Prepare→一次实际appSend完整handoff至CONTROL→Record，UNKNOWN/NO_RESEND/平台拒绝不重发/换通道。

> SUPERVISED POLICY at 2026-10-22T23:40:07.1911269+08:00: {"policy":{"resourceKeys":["unity-project","workflow-devtools"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"AI313 independently accepted5contracts15facts; read-only loaded signatures/MVID observation only. No native compile or snapshot. Parent290 root4of5 unchanged by first diagnosis; eventual original resume paired to latest successful313 cycle.","updatedAt":"2026-10-22T23:40:07.1261996+08:00","supervisionLevel":"full"},"taskId":"AI-000314"}

> SUPERVISED EXTEND at 2026-10-22T23:40:07.3329017+08:00: {"taskIds":["AI-000314"],"reason":"Necessary read-only load observation after independent313 acceptance; original290 grant remains unissued."}

> SUPERVISED RESUME_APPROVED at 2026-10-23T00:11:49.1941561+08:00: {"cycles":["AI290:CODE313:retry4"],"scope":{"paths":[".ai-workspace/outputs/DETACH-B13-20261019/AI-000290"],"operations":["evidence-write","idempotence-check","isolated-catalog-export","unity-catalog-readonly-snapshot","unity-loaded-api-readonly","unity-recompile-status-readonly","unity-script-refresh-compile","unity-target-check"]},"verification":"用户直接批准一次官方recompile内部全项目Refresh及原290配对续行；精确用户批准/审查单/Scope在本批inputs和outputs。ENGINE fresh revision96 idle/latestTurn completed/error null，workerIDLE无lease/activeTask/预约。313正式Complete/Receive与独立5合同15事实通过，最新成功cycle AI290:CODE313:retry4；314实际独立四合同15事实有效观察新接口未加载，原290三Fail/root4of5保持。同周期配对不新增根周期，不改task定义/依赖。完整批准执行AI290_ENGINE编译装载与配对续行_已批准.txt，实际一次recompile/有界readonly状态/loaded再原snapshot一次。","grantId":"44a1c352f10a4cacab730758ea0d70f5","remaining":1,"taskId":"AI-000290","authorizationSource":"HUMAN","reserved":0,"used":4,"sourceApprovalId":"call_1023ba3eaee945eabd38968be1dc030d:0","rootTaskId":"AI-000290"}
## AI-000315 | [ENGINE_CLI] [WF] AI290编译断连后当前装载只读核对

- 序号：315
- 提交时间：2026-10-23T00:38:57.9814305+08:00
- 模块 ID：WF
- 初始状态：QUEUED
- 前置依赖：AI-000313, AI-000314
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：S / 40

### 指令

AI-000315 · AI290编译状态断连后的当前装载只读核对 · 固定ENGINE_CLI · READONLY_DIAGNOSIS Parent AI-000290 · S40。
用户在明确获知290 attempt4已正式失败、单次编译已触发但状态连接重置且快照未执行后，直接说“现在继续推进任务链的执行”。真实原件 inputs/派发/AI290-CLI-CALLER-REPAIR-20261022/AI290失败后继续推进_用户指令_20261023.json；CONTROL失败复核 independent-failure-review-290-attempt4.json（15事实，原五合同2/5，未验收）。本卡是这一新直接指令下的只读当前状态观察，不重放已失败业务，不再次触发recompile/Refresh，不重开旧status轮询，不声称断连原因已证。原290四失败、同根4/5余1保持；首次只读诊断0消费，不伪REPAIR配对，不在本卡ApproveResume或执行原快照。

固定原ENGINE_CLI 01a14087-d8ea-7561-b9b5-260798096a19，同session beefe968cb5e48e0bea0b8b47260abd9；免chat重载/不换模型或工具。先CORE、WORKFLOW/SUPERVISED及ENGINE_CLI当前规则、两项目文档页首与本模块。精确Scope AI315_SCOPE.json、四合同 inputs/acceptance/AI-000315.json。唯一输出根 .ai-workspace/outputs/AI290-CLI-CALLER-REPAIR-20261022/AI-000315；fresh结果拒已有/链接。所有旧290/314原件保持。

复用已验完整候选 inputs/派发/AI290-CLI-CALLER-REPAIR-20261022/AI314-loaded-api.eval.cs，SHA9B8FA3175CB46BA22C30A8DA11B5AC2880182B9EA52533B22431DE8F9F4F8B1E；只第1行taskId/attempt/executionId绑定315正式claim，body逐字不变，不复制片段/新runner/改helper。该候选已经真实引用编译并在314实际执行，仅观察GetMethod签名与MVID，不调用proof/Capture/PrepareSnapshot。禁止现场载荷修正或再次编译该候选以规避失败。

通过当前共享Invoke-TaskTool和同官方CLI，UsageCategory pipeline-repair（本卡为诊断成本，不预断产品根因），ToolSource unity-official-cli。status/editor_status各一次形成真实新health：项目D:/GameProject/Eat-What、6000.3.24f1、实际PID/port/freshheartbeat、clean idle Edit、没有现存未结journal。不能复用314/290旧health。若busy/未知dirty/项目不符/权限/连接失败，保原样正式Fail，不重试、不重载、不切工具；没有第二次status。可只读保存Temp/pipeline_recompile_status.json（存在时）的原始字节/SHA/mtime，缺失明确null；只辅助线索，不覆盖真实Editor状态或宣称恢复了旧请求。

核已审2源码及2meta精确SHA，和本轮允许清单git diff/status基线；不要全仓hash。Serializer B2825BA990CD02A9BC0945F79C6228BEF9FA670AE2D92E185EF0E0D9DA763FDD；Processor D8CC12CB20E36910B7A5E92BD261B360B9F399A4C06CBB4B676152255D9E2C2B；对应meta122656FC272E7C5E8B0476324854D1C6CBDF39EE649254B5D9D8C5AEF28D2A9D和4EFE33F695C777DAF1DB66C56ADFC9362FEE041EA7BD427018224A6FB87AB55A。限定diff至少含这4件及源scene/Generated/StreamingAssets/Fingerprint/BuildSettings；证据不能将Git状态等同全内容或Importer活动已查完。

正式SupervisionCheckpoint后官方eval_file一次完整候选，最后console_status一次。成功总4调用8双流；严格UTF8完整JSON、SHA/长度、target/命令/实际载荷与完整返回保全。观察 PrepareSnapshot(string)、Capture3兼容、新Capture4与private bool IsSerializedEmptySprite(ScriptableObject,string,Sprite)真实签名/MVID及repairSignaturesLoaded。旧processorMVID fb204dbd-7941-405b-b1c3-635c06089b4e、serializer185d312e-300e-4d7d-a4be-1a98feab8547仅比较基线。当前接口已加载不等于快照行为成功；加载否都可以作为完整观察结果Complete，明确readyForOriginal290=true/false及理由。运行错误则Fail。不能仅凭后采Console0error说旧编译或快照成功，无连续起点不得宣称新增Error0。

禁止recompile/recompile_status循环/Refresh/Import/RequestScriptCompilation/Compile/Play/Build/PrepareSnapshot/Capture/Encode/Rehydrate/Export/创建销毁对象/Apply/SaveScene/MarkClean/清Console/源码场景数据meta写入；不执行旧289探针或三图，不读取真实存档，不清理用户脏数据。两源/meta和限定diff前后保全。包目标1MB硬10MB，完整失败/文本留原位，可无损压缩双流并保存原SHA/长度。没有对生产写入的直接证据不得泛称OS-wide无写入。

Complete/Fail/Yield本轮立即Prepare→一次真实app Send完整handoff到CONTROL 01a13ef5-eda9-7882-b1c9-eca47a6470d9→Record。UNKNOWN/NO_RESEND/平台拒绝不重发。执行器停止，不自行领290/291。CONTROL独立四合同及本次真实loaded/clean后，才决定290唯一ApproveResume剩余1周期、原scope一次fresh快照；不重用313已消费且290失败的cycle、不退款、不增额，最终A+C/USER-U5保留。

> SUPERVISED POLICY at 2026-10-23T00:38:58.5002620+08:00: {"policy":{"resourceKeys":["unity-project","workflow-devtools"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"用户已获知290a4正式失败后直接要求继续；仅已验完整候选的当前loaded只读诊断，无第二次编译/快照/状态重试，无新根消费，最终人工门保持。","updatedAt":"2026-10-23T00:38:58.3770313+08:00","supervisionLevel":"full"},"taskId":"AI-000315"}

> SUPERVISED EXTEND at 2026-10-23T00:38:58.6547811+08:00: {"taskIds":["AI-000315"],"reason":"用户当前继续指令下，290a4新失败已经CONTROL15事实核实；必要首次只读诊断315确认现装载后才决定原卡剩余1周期，不触发新编译或业务。"}

> SUPERVISED RESUME_APPROVED at 2026-10-23T00:51:51.3951838+08:00: {"authorizationSource":"POLICY","taskId":"AI-000290","remaining":0,"scope":{"paths":[".ai-workspace/outputs/DETACH-B13-20261019/AI-000290"],"operations":["evidence-write","idempotence-check","isolated-catalog-export","unity-catalog-readonly-snapshot","unity-target-check"]},"verification":"AI315 independent 4 contracts/16 facts PASSED_CURRENT_LOADED_OBSERVATION SHA A64DADBEA42FE18992DD6802003FCAA929759658BA398A014DD42C5948252340; fresh ENGINE app revision103 idle/completed, no active turn. AI290 fail4 reviewed, root4/5 remaining1, user latest continue. Original snapshot scope only; no second compile or failed-cycle reuse.","grantId":"aa8c6029662f4daa92cdcf64d354817e","reserved":1,"sourceApprovalId":"call_37410ba043344e9f81b4ce3ef3472a18:0","used":4,"cycles":["AI290:retry5"],"rootTaskId":"AI-000290"}
## AI-000316 | [CODE] [WF] G87显式Player数据验收宿主补齐（关联292，独立pipeline-repair）

- 序号：316
- 提交时间：2026-10-23T01:59:03.4702738+08:00
- 模块 ID：WF
- 初始状态：QUEUED
- 前置依赖：AI-000287, AI-000293, AI-000291
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：M / 80

### 指令

AI-000316 · G87显式Player数据验收宿主补齐 · 固定CODE · M/80 · semanticKind/usageCategory=pipeline-repair · businessOrigin AI-000292 · CLI(磨合)
技术WorkKind=FEATURE独立管线根；不是292同根REPAIR，不扣/退292历史，不伪repairContext。依据AGENTS用户G87、2026-10-21继续原链及2026-10-23“现在继续推进任务链的执行”，仅完成已批B验证必需管线缺口，不新增产品功能。固定原CODE chat；不自动重载/换模型/新chat。精确Scope=同目录AI316_SCOPE.json，预声明五合同inputs/acceptance/AI-000316.json。

事实入口 outputs/control/AI292-PLAYER-VALIDATION-REPAIR-20261023/independent-failure-review-292-attempt1.json（SHA01DB6FC9CAB9D7472939C5BF08E1A6DABFE49688E568F22F3406184A890DE26C），16事实复核。292 a1正式Fail/Receive，2/7，不是Build或产品编译失败；4只读CLI/8流，无源漂移，Windows64 Mono可用，但当前Bootstrap只隔离/Ready，没有Player自动数据断言+受控完成。Build/Player/Fingerprint写/Refresh均0。原292根0/5、失败/原证据保留，不自续292。
先CORE/CODE worker、两项目入口页首与SEC02/隔离相关节、WF既有验证/失败结算，实际源码优先。只按需读：292 PLAYER-READINESS-GAP.json/REPAIR-PROPOSAL.json与build-readiness原件；287/293宿主及已有RunValidationHost；291 BUILD-HANDOFF/真实staging/catalog.graph.json及认证blob；原A-stage。不要全量读289历史或全仓hash。

唯一实现目标：在已有ShortCycleValidationBootstrap内最小扩充显式启用的Player数据验收选项和有限生命周期，真正驱动/观察现有Catalog source读取，输出可归属机器证据，随后目标先销毁/lease后释放并受控退出。普通启动与原四参数仅隔离模式保持；未显式启用新数据验证时不执行数据断言/新增文件/自动退出。沿精确task/run/root校验、fresh输出/CreateNew/拒覆盖与祖先链接/无生产默认save回退。参数缺失/重复/未知/错根/超时/生命周期错目标均fail-closed；新增数据验收期有明确上界、失败退出码和证据，不能挂起。原before-Awake、取消/场景恢复/默认关语义及原1～20源码字节不改，也不自动RunOriginalProbe。

数据合同：验证实际当前Session绑定的唯一Catalog及ICatalogSource，Release真实EncryptedBlob/Dev真实SO策略，当前三菜实际记录/14表/169数据对象及完整关系保持；需要Sprite registry/key/对象事实，不用Sprite.name/GetInstanceID冒充GUID/fileID。从已独立通过291真实graph建立显式绑定的验证基线/身份映射（校验哈希/格式和完整性），只放本次验证输出，不能把固定数值或期望文件直接抄成观察PASS。允许使用已有serializer/graph公开API和只读对象观察，不复制生产实现作假验证。Player无AssetDatabase；为观察私有绑定若确有必要，精确允许Session与CK01GeneratedDataCatalog仅增加最少只读、非序列化验证访问器，不改变Awake/初始化/Source选择、资产字段/导表/codec/serializer/BuildProcessor/默认路径。优先现有API，不能顺手修产品或改配表非空值。图/章鱼/番茄/白菜等已有缺口保持，数据读取一致不代表三菜完整可玩。

坏blob合同：只在显式本卡验证模式，实际当前Release来源读取被篡改副本时，鉴别真实认证拒绝/无SO回退/无部分接受，明确期望拒绝结果与意外失败分别记录，受控清理/退出，不能笼统catch异常即PASS。处理实际Awake可能先失败的时序：不得用孤立Authenticate替代当前source，不能吞产品错误或改变正常路径；有界等待与事件/退出归属必须能验证。CODE只隔离fixture坏副本；原生只在未来ENGINE292的本卡构建副本，不能修改291原件/生产StreamingAssets。禁止新增Runtime Pipeline HTTP/RPC服务、ENABLE_RUNTIME_PIPELINE、ProjectSettings配置、PackageCache/manifest变更或部署。

验证与交接：只扩既有DevTools/ShortCycle/ShortCycleIsolationAndTextTests.cs、UnityStubs.cs、Test-CK01CShortCycle.ps1中的RunValidationHost与明确opt-in分支。保留原所有断言主体/正常默认行为，新增相称实际宿主/真实Catalog数据读取正反例（正常Release/Dev、损坏认证、错误期望/关系/重复或缺失身份、错root/重复run/超时/清理失败/未启用）。不要新runner、复制算法模拟PASS或重跑无关全量Final。原宿主41及必要新用例调用真实改后实现，默认persistent路径读取0。既有真实Unity引用编译必须覆盖Player Release/Development/Editor分支和新依赖；API shim只能解释接口编译，不能冒充完整生产依赖/Player运行。需要修runner的Catalog引用就在同runner最小接入，不能改Serializer/导出器。原验证结果/失败保留，稳定key PLAYER_DATA_HOST_VALIDATION；首次失败至多两次局部修正，达到正式CODE_VALIDATION_STOP立即结算回传，不能换key自续。

输出主根 outputs/AI292-PLAYER-VALIDATION-REPAIR-20261023/AI-000316；已有host固定根还允许 outputs/DETACH-B13-20261019/AI-000316（仅本卡离线fixture/isolated-saves），不能借此访问AI292或真实存档。最多9个共享文件，禁止新生产cs/meta（需实质新增路径先交具体方案CONTROL核）；CODE不调用Unity/CLI/MCP/Play/Build/Refresh/Compile原生，不生成meta/场景、生产Fingerprint/StreamingAssets/数据。只最少WF与项目模块定位说明，不能提升完整核验日期。

交付REPORT六要素、before/candidate最小快照、原日志/失败、完整差异、evidence/acceptance、可复用ENGINE-HANDOFF及参数例：Release/Dev/坏副本的明确预期与退出码、精确task/run根与基线来源SHA、观察输出字段及清理顺序、必要原生装载/验证前置。交接原生验证不得假称本卡已完成。CONTROL独立通过后才安排必要固定ENGINE就绪验证及原292唯一ApproveResume；独立FEATURE不得伪配对，本卡不预发/消费业务许可。292七项合同/最终A+C/USER-U5全保留。

生产变更前正式Checkpoint，保用户已有改动；无提交推送。包目标1MB硬10MB，>1MB说明，未结报告/原失败完整留原位，新冗余原流可核SHA后无损压缩。actualTool逐响应未知留null，不由CODE分类推工具/账单，原用量不重写。Complete/Fail/Yield本轮立即Prepare→一次实际appSend→Record给CONTROL 01a13ef5-eda9-7882-b1c9-eca47a6470d9，Receive非验收，不自行领后继。

> SUPERVISED POLICY at 2026-10-23T01:59:04.0213969+08:00: {"policy":{"resourceKeys":["unity-project","workflow-devtools","documentation"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"G87 independent validation-pipeline repair under approved B continuation. Dependencies287/293/291 already independently accepted; no new human product gate. CODE offline only, original292 untouched; CONTROL independent pass and necessary ENGINE readiness before formal292 resume.","updatedAt":"2026-10-23T01:59:03.9409390+08:00","supervisionLevel":"full"},"taskId":"AI-000316"}

> SUPERVISED EXTEND at 2026-10-23T01:59:04.1739241+08:00: {"reason":"Continue approved B through G87 independent CODE validation-entry repair; original292 failed receipt retained, no new292 grant.","taskIds":["AI-000316"]}
## AI-000317 | [ENGINE_CLI] [WF] G87显式Player宿主原生装载与参数冒烟（关联292）

- 序号：317
- 提交时间：2026-10-23T02:30:26.2438760+08:00
- 模块 ID：WF
- 初始状态：QUEUED
- 前置依赖：AI-000316
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：S / 40

### 指令

AI317 · PLAYER-DATA-HOST原生装载/参数只读SMOKE · 固定ENGINE_CLI · S40 · FEATURE独立技术根 / semanticKind与usageCategory=pipeline-repair / businessOrigin=AI292。
依据用户G87必要SMOKE、已批准B的unity-import-compile和“现在继续推进任务链的执行”。这是已批Player验证管线必要装载，不是新业务/额外292许可，也不复用已消费的旧AI290单次编译批准。只登记本卡精确Scope；Library仅正常Unity编译缓存与已加载程序集观察，不允许手改/删除/恢复Library、Assets或配置。原292仍Fail/根0of5；本卡不是其PairedRepairTaskId。免chat重载/不换模型/不新chat有效。
前置AI316 Complete/Receive+CONTROL独立5合同16事实PASSED，review outputs/control/AI292-PLAYER-VALIDATION-REPAIR-20261023/independent-review-316.json SHA F165902C61085E0495866AE07B19A8C63837A62579EAB60523C34D82A9450E32。只补读316 ENGINE-HANDOFF.md、source-manifest及本卡4合同。41原+28新离线断言和5引用编译不代表原生。Dev仅155可达/169基线边界必须保留，原292完整7合同不减。
先唯一Poll携真实OwnerThreadId领取；运行中不二次Poll。所有官方CLI沿既有Invoke-TaskTool和正式checkpoint，实际task/execution绑定；UsageCategory=pipeline-repair、ToolSource=unity-official-cli。新鲜本卡输出attempt1，原流/身份/载荷/异常完整保全，既有严格完整JSON门、不使用--result-only/猜递归result/非幂等重试。
1.status/editor_status各一次核实际唯一项目D:/GameProject/Eat-What、Unity6000.3.24f1、PID/port/heartbeat、clean idle Edit和无未结CatalogBuild journal。只核316两个改动生产cs当前SHA及现meta存在/归属；未知dirty/漂移/平台拒绝立即停止。无全仓hash、内存快照或纹理导出。
2.一次只读eval_file检查当前已加载类型EatWhat.Cooking.ShortCycle.ShortCycleValidationBootstrap与ShortCycleSessionManager，程序集名称/MVID；Request嵌套public类型的DataMode/BaselinePath/BaselineSha256(string)/TimeoutSeconds(int)，ParsePlayerArguments(string[])和ValidateRequest(Request)方法，Session内部只读ValidationCatalog属性，以及新VerifyData实例私有方法。通过反射检查字段/签名；缺新字段时只报告loaded=false，不能动态直接引用缺失字段造成编译失败。只输出scalar DTO/JObject，不直接序列化Unity结构体。scene handle用实际(int)转换。
3.若上述新接口已全部加载，不触发编译。仅当旧程序集尚未加载修复且项目groundTruth没有编译错误、dirty/journal/未知写入时，正式Checkpoint后允许同官方recompile至多一次完成316已有两源码装载。此命令内部全项目AssetDatabase.Refresh/域重载，属于已批B编译必要准备；禁止另外Refresh/ImportAsset/RequestScriptCompilation/修改EditorPrefs或绕工具重试。按现有官方命令一次执行及其正式状态/完成回执判终态；连接重置/UNKNOWN/拒绝或超时即保全正式Fail，不再触发也不换工具。若需要后续读取仅完成回执允许的状态读取，不能启动通用轮询/400重试。编译后独立一次同读只读payload核实际新签名/MVID与项目编译状态。
4.已加载后只在一次只读eval载荷中调用纯ParsePlayerArguments（允许创建普通托管Request/数组，不创建Unity对象、不Begin/Arm/Play）。真实参数解析release/development/reject-tampered三个模式，给本卡AI-000317/task/run与预期本卡路径字符串、291基线SHA和timeout60，检查返回模式/三身份/path/hash/timeout不丢失；解析不读取或创建这些目录。再核普通无验证参数返回null、原四参数模式DataMode为空、重复data/未知validation参数各确切拒绝。只能捕获精确预期ArgumentException，异常链/type/message保存；其它异常不是PASS。不要调用ValidateRequest（本卡不创建baseline）、Begin/Stop/VerifyData/任何Catalog Source或业务函数，不读真实存档。观察State/IsRunning/factory前后完全相同，clean场景未改。
5.console_status一次保留当前编译/错误和历史，无连续起点不声称新增Error0、不清Console。记录总调用与compile次数，两个源码/原meta前后及限定Git diff，不能把Git状态字节等于全内容证明。源码写入0/Fingerprint写0/Build0/Player0/Play0/原1～20及P2调用0；仅必要条件compile可能1。输出完整报告/evidence/acceptance及原流与精准SHA，目标1MB硬10MB。原316CODE验证的native0边界不改写。
本卡4合同必须均真实满足，不能用离线替代native。新接口缺失且编译不可安全执行/签名或解析错误/未知dirty/平台拒绝立即原卡正式Fail→Prepare一次实际Send→Record，执行器不修源码/自续/代领292。成功同样立即完整handoff给CONTROL；CONTROL独立通过后才唯一ApproveResume原292 Runs1按原Scope，保留首Fail/root消费与最终A+C/USER-U5人审门。实际Build/Player与Fingerprint编译仍在原292，不在本卡提前执行。

> SUPERVISED POLICY at 2026-10-23T02:30:42.1887373+08:00: {"policy":{"resourceKeys":["unity-project","workflow-devtools","documentation"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"G87必要独立SMOKE，316已CONTROL独立通过。只精确目标/纯参数解析与必要一次已批B编译装载；无292业务/无新许可/无最终人审。","updatedAt":"2026-10-23T02:30:42.0874852+08:00","supervisionLevel":"full"},"taskId":"AI-000317"}

> SUPERVISED EXTEND at 2026-10-23T02:30:42.4624265+08:00: {"reason":"316独立通过后推进必要SMOKE，不恢复旧完成业务；同session","taskIds":["AI-000317"]}

> SUPERVISED RESUME_APPROVED at 2026-10-23T02:54:53.8671712+08:00: {"cycles":["AI292:retry1"],"taskId":"AI-000292","reserved":1,"verification":"app fresh ENGINE revision110 idle/completed/errornull; fresh-engine-before-292-resume1.json. AI316 five contracts16facts and AI317 four contracts14facts independently PASSED; review317 SHA F05BE6D1316925236C450746FEA3B2004011EBB4BA432CED500EB13C509F55C3. Independent FEATURE pipeline roots are not paired repair; original292 scope/7contracts/L120/final gate unchanged, root0of5, initial failure retained.","scope":{"paths":[".ai-workspace/codebase/data.md",".ai-workspace/outputs/DETACH-B13-20261019/AI-000292",".ai-workspace/toolkit/SEC.md","项目整体阅读理解与推进基线_2026-08-14.md","Assets","Assets/Scripts/DataBase/CookingData/Build/CatalogBuildFingerprint.g.cs","Assets/StreamingAssets/data","CODEBASE_MAP.md","Library/CatalogBlobBuild.journal.json"],"operations":["approved-generated-fingerprint-update","controlled-build-copy-tamper","documentation-update","evidence-write","idempotence-check","isolated-player-validation","unity-import-compile","unity-owned-build-staging","unity-owned-temporary-cleanup","unity-target-check","unity-windows-mono-build"]},"used":0,"rootTaskId":"AI-000292","remaining":4,"sourceApprovalId":"call_37410ba043344e9f81b4ce3ef3472a18:0","grantId":"25a9613588f6449eaba8aa38acd79804","authorizationSource":"POLICY"}
## AI-000318 | [CODE] [WF] G87 Player编译空输出与预处理残留定位（关联292，独立pipeline-repair）

- 序号：318
- 提交时间：2026-10-23T03:17:39.7791214+08:00
- 模块 ID：WF
- 初始状态：QUEUED
- 前置依赖：AI-000317
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：M / 80

### 指令

AI-000318 · G87 Player编译空输出与预处理残留定位 · 固定CODE · M/80 · technical FEATURE独立管线根；semanticKind/usageCategory=pipeline-repair，businessOrigin AI-000292。纯定位与隔离编译，不改生产/共享源码，不直接修复或重试原292；不伪repairContext/PairedRepairTaskId，不消费退款292。依据用户G87、已批B及最新“现在继续推进任务链的执行”，只继续不受影响的离线调查。原292仍停止，额外4件尚未获清理/再建造放行，不用本卡绕过硬门。

精确Scope同目录AI318_SCOPE.json，预声明4合同inputs/acceptance/AI-000318.json。先CORE/CODE worker/WF和两项目入口页首/本模块，按需读下面原证据，不全量读历史或扫仓。固定CODE不新chat/换模型/重载。正式领取一次，写前Checkpoint，结算立即Prepare→一次appSend→Record绑定CONTROL 01a13ef5-eda9-7882-b1c9-eca47a6470d9，不自行领后继。

事实：292attempt2/execution072a801a76d94effb8926f556a9fd261，原首败后唯一POLICYretry1已消费，root1of5余4。正式Fail-7d4fca97cf1d SHA7DB383F00550ECFB10301825F443A855ACC06CA391731209F4A6DC7B25F88109；CONTROL independent-failure-review-292-attempt2.json SHA F0F306DACE4FD7C40C594F4901E2C70F90C3E6977E096BD991C1CEBFE608236B（outputs/control/AI292-PLAYER-VALIDATION-REPAIR-20261023）。16事实、3/7业务合同，不是通过。已审291指纹生产更新/Editor编译真实成功；一次Release真实Failed/3Error12Warning/0bytes，Dev/Player/tamper0。9CLI18流无CLI退出错误不表示Build成功。原额外4件与源码/已保存场景不得改变。
原证据目录 outputs/DETACH-B13-20261019/AI-000292/attempt2：release-build-result.json、release-failed-BuildReport.json、LastBuild.buildreport、editor-log-bounded-tail.gz及来源SHA、editor-build-error-context.txt、failed-compiler-response-files.json+原4rsp/rsp2、PREFLIGHT/FAILURE-READBACK、git-status-before/after、performance-preprocessor-paths.json、release-ownership-observations与原4journal。完整log为已保全2MiB片段，不叫完整Editor日志；需要额外日志只按对应失败时段/路径有界只读，写出处/时间/缺失原因，不扫全机器。
控制更正AI292-fail2-report-corrections.json：两个Csc目标MCPForUnity.Runtime.dll和spine-unity-examples.dll输出为空；ExitCode3在编译graph级，未证明各Csc进程各自退出3。实际Build job19:04:07..19:04:35Z，SDK投影UTC早8小时保留，不改原件。5组AI285 ownedpair磁盘都不存在/journal无，但AssetDatabase仍返回5个非空GUID；不得写成GUID空或据此删缓存。具体空输出根因未知，不默认AI316/数据/权限/插件不兼容。

目标一：核失败的真实完整编译命令、2个rsp+2个rsp2、编译器/引用/源文件路径和受控输出参数，结合已有Bee/Compiler日志证据定位空输出。允许只读实际对应Unity6000.3.24f1 dotnet/csc版本与响应文件，不调用Unity/官方CLI/MCP/Build/Play/Refresh/Import/域重载，不清删Library/Bee/PackageCache、不重启Editor/编译服务、不变全局环境、权限或配置、不安装工具。
若静态证据不足，可在本卡输出下准备完整响应文件诊断副本，仅把所有输出位置（-out/-refout/-pdb/-errorlog/generatedfiles等，包含间接@rsp）改到本卡fresh目录，先输出逐参数diff、原/副本SHA、所有读写路径清单；保留全部原源/引用/define/analyzer/编译选项，不能删插件/Sample/源码或去掉analyzer来“通过”。确认所有显式输出均在本卡根、原路径无并发漂移/链接后，同一原Unity自带dotnet+csc可各目标离线运行至多一次（最多2次总计），保全准确argv/退出/双流/时间与本卡结果，不使用原Library输出、不启动任何新runner/fixture/生产程序集装载。不能安全重定向的选项或隐含写入不明则不运行，给具体阻断和候选。无权限拒绝则同工具隔离诊断可进行；遇真实平台拒绝/UNKNOWN立即正式Fail，不换工具/变参数绕过。两个诊断若通过只能说明本次隔离编译成功，不证明原Build瞬时故障已修，也不得据此新增Build次数。本卡不做共享csc参数变体循环，不二次重试失败目标。

目标二：核4件性能预处理输出的精确来源、是否本次创建、业务引用和恢复边界，交具体候选而不删除。实际已安装 Library/PackageCache/com.unity.test-framework.performance@8b56083ec0f0/Editor/TestRunBuilder.cs 的OnPreprocessBuild无测试Build条件即SaveToStorage；OnPostprocessBuild/Cleanup会删2JSON/meta，可能根据EditorPrefs递归删Resources并Refresh，禁止盲调用该Cleanup或改EditorPrefs/包缓存。仅精确4件：Assets/Resources/PerformanceTestRunInfo.json及meta（04A830...2594/F94A2A...9E5）、PerformanceTestRunSettings.json及meta（600C5C...55B/1C2EF5...CC8），完整SHA用原manifest。CONTROL已在AI292-fail2-extra-preserved保全当前字节。原日志CreateNew/新meta导入/前未列后untracked是归属证据，不把整Assets/Resources目录当owned。用目标GUID/路径必要的定向引用搜索和包源码核业务引用；不扫全纹理/整仓hash。符合G26需精确EditorAPI清理两asset及meta的候选参数/前置/当前SHA/禁止跟随链接/Resources目录保持/失败停；不生成执行成功回执。未来Build还会生成同文件，提出明确限定的新边界/保全与清理候选，不静默扩大原292 Scope或直接修改BuildProcessor。未知部分明确留unknown。

交付：REPORT六要素、evidence/四合同、原失败证据索引SHA、实际静态发现和最多2次隔离编译原流（若未执行写原因，不伪要求必须成功）、完整原因/尚未确定项、四件归属/引用与仅候选清理方案、可供CONTROL安排最小修复/必要SMOKE/原292续行的精确下一步。可以完成“根因未定但证据边界和安全下一步明确”的诊断，不能用标签说修复完成。无需改共享文档，本卡只有自己的输出可写；不修改两项目入口/AGENTS/WF/源码/asmdef/meta/PackageCache/manifest/ProjectSettings/runtime/原292结果。原Spine Sample/MCP包必须保留。
稳定自验key AI292_BUILD_DIAGNOSIS；可修自身文本/输出脚本，但不得重执行已失败隔离csc或业务；原自验失败按既有CODE至多两次局部修正/三次正式STOP，不能换key续跑。包目标1MB硬10MB，超1MB说明，原失败完整留原位可无损压缩新大日志且保原SHA/映射，不复制全仓/Unity运行库。actualTool逐响应未知null，不由CODE车道推账单。原292七合同/1of5失败史、最终A+C/USER-U5全保持。成功Receive后CONTROL独立诊断，不自动授予新生产写/清理/原业务许可。

> SUPERVISED POLICY at 2026-10-23T03:17:40.4068797+08:00: {"policy":{"resourceKeys":["unity-project","workflow-devtools"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"G87 independent offline diagnostic evidence only, businessOrigin292. Original292 remains stopped after unplanned preprocessor outputs and failedBuild. No production/source/package/cache write or cleanup; no new292grant. Review317 passed. CONTROL must independently examine root cause and precise restoration candidate before future production work.","updatedAt":"2026-10-23T03:17:40.2913005+08:00","supervisionLevel":"full"},"taskId":"AI-000318"}

> SUPERVISED EXTEND at 2026-10-23T03:17:40.5980730+08:00: {"taskIds":["AI-000318"],"reason":"User G87 and latest continue authorize unaffected isolated diagnosis for292; no expansion of product/asset write; same session."}

> SUPERVISED RESUME_APPROVED at 2026-10-23T13:55:37.6926705+08:00: {"rootTaskId":"AI-000292","scope":{"paths":[".ai-workspace/codebase/data.md",".ai-workspace/outputs/DETACH-B13-20261019/AI-000292",".ai-workspace/toolkit/SEC.md","项目整体阅读理解与推进基线_2026-08-14.md","Assets","Assets/Resources/PerformanceTestRunInfo.json","Assets/Resources/PerformanceTestRunInfo.json.meta","Assets/Resources/PerformanceTestRunSettings.json","Assets/Resources/PerformanceTestRunSettings.json.meta","Assets/Scripts/DataBase/CookingData/Build/CatalogBuildFingerprint.g.cs","Assets/StreamingAssets/data","CODEBASE_MAP.md","Library/CatalogBlobBuild.journal.json"],"operations":["approved-generated-fingerprint-update","controlled-build-copy-tamper","documentation-update","evidence-write","idempotence-check","isolated-player-validation","unity-import-compile","unity-owned-build-staging","unity-owned-temporary-cleanup","unity-performance-preprocessor-exact-cleanup","unity-performance-preprocessor-owned-output","unity-target-check","unity-windows-mono-build"]},"authorizationSource":"HUMAN","sourceApprovalId":"call_e46b2ba4e5914dd68a38e343b1d04531:0","remaining":3,"cycles":["grant:dbe48d94597148a5960c5ad46028df58"],"verification":"fresh-engine-before-292-four-files.json: app revision112 idle/latest turn completed/error null; live worker IDLE/no activeTask/no RUNNING/reservation; same SUPERVISED ACTIVE session. independent-review-318.json four contracts16 facts PASSED_DIAGNOSIS_NOT_ROOT_CAUSE_OR_REPAIR. AI292性能预处理四件_用户批准.json binds user call_e46b2ba4e5914dd68a38e343b1d04531:0 and exact review/scope. Original root1/5 remaining4, two failures retained; new four paths approved, no new limit, no paired diagnosis or definition change. Evidence under .ai-workspace/outputs/control/AI292-PLAYER-VALIDATION-REPAIR-20261023.","grantId":"874c6128ce7e482abc60fd1d2fae55eb","reserved":1,"used":1,"taskId":"AI-000292"}
## AI-000319 | [CODE] [SC] [CK01C] 人工缺陷Catalog单一锁定提示与空卡装饰显隐

- 序号：319
- 提交时间：2026-10-23T23:28:28.8498674+08:00
- 模块 ID：SC
- 初始状态：QUEUED
- 前置依赖：AI-000289
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：M / 80

### 指令

AI-000319 | A+C人工缺陷：Catalog单一锁定提示与空卡装饰显隐 | 固定CODE | M/80
本轮真实用户原话与部分签收见同目录用户反馈与部分验收.json。WorkKind=FEATURE技术独立产品缺陷卡，semanticKind=human-acceptance-defect，usageCategory=task，origin=AI289/D-T2；不是管线修复/原任务重试/退款，原已成功任务与历史保持。Scope=AI319_SCOPE.json，四合同inputs/acceptance/AI-000319.json。后继AI320已拆分，仅CONTROL独立通过才派ENGINE。

只修两个已反馈显示问题：
1. 当前CatalogCard.Apply把lockedRecipeSprite写入iconRenderer并令placeholderRenderer启用，造成两问号。查实际prefab+scene只读绑定，选择唯一既有锁定提示，保持原序列化字段/引用兼容；已解锁显示原菜图，空卡无问号，不把未解锁变可选择，不变配表/解锁逻辑。保留明确旧ConfigureRecipe调用兼容；不可用全面隐藏图标掩盖正常菜图。问号尺寸由ENGINE在既有场景实例调整，本卡不要生产资源文本编辑。
2. 空view除已有基本显示外必须隐藏该CatalogCard所属名称底/胶带等装饰；重新绑定非空后恢复应有装饰。优先精确序列化装饰引用、或能保留原状态的等价最小实现；不要无差别开启所有子Renderer而复活空tag/未收藏star/占位图，不把整个脚本宿主/交互所有者永久SetActive(false)导致不可重绑。若增加装饰引用只列CatalogCard所需字段，默认兼容，ENGINE对八实例精确绑定。其他GridPiece/Ingredient/Sticker显隐语义不扩大。

允许现有CatalogCard.cs及ShortCycleGridPieceBase.cs必要最小改动；首选Catalog局部，不顺手重构。只读Content/RecipeBookBinder、StepDetailRowPiece、现CatalogCard.prefab/scene/AI288 binding-map确认实际链。StepDetail颜色归ENGINE，本卡不通过运行时每帧重设颜色压制用户后续调整。

验证与改动相称：既有ShortCycleRecipeBookTests/原Test-CK01CShortCycle入口定向调用真实改后实现；仅必要锁定/解锁/空/重新非空、装饰恢复及收藏/tag不被误开。允许给既有runner加显式窄选择或复用CompileOnly后实际既有Run，不新runner、不重跑旧整套Final/Phase2/安全/隔离宿主。需要的UnityStubs最小接口真实编译，不以stub视为原生效果；不为纯颜色/尺寸另写镜像测试。旧断言保留、实际源差异及编译/定向行为日志交付，失败按稳定CATALOG_HUMAN_FEEDBACK键登记，不能换key绕止损。

交付REPORT、evidence/acceptance与ENGINE-HANDOFF：选定单问号路径和理由、原图与占位的互斥规则、八实例所需具体字段/已有对象、名称底/胶带的对象范围、StepDetail十Description现白色与需黑色（只读定位），允许场景diff清单与验证动作。不要虚构原生观察；不改prefab/scene/meta/Unity/CLI/MCP。本卡输出outputs/AC-HUMAN-FEEDBACK-20261023/AI-000319。固定原chat、PowerShell7及正式queue。读取CORE/对应worker/模块入口和已知坑账，保留用户已有未提交修改。源码/原生事实优先，严禁全仓重复哈希/旧1～20/Build/导表/真实用户存档。写入前正式SupervisionCheckpoint。失败立即按正式规则结算回传，不自续/换工具；Complete/Fail/Yield本轮Prepare→一次实际appSend→Record给CONTROL01a13ef5-eda9-7882-b1c9-eca47a6470d9。CONTROL Receive后独立核验，不自行派后继。包目标1MB硬10MB，保留失败和未结原件。无Git提交推送、素材导入、非空配表修改。源码/资源完整核验日期不提高。

> SUPERVISED POLICY at 2026-10-23T23:28:29.3337806+08:00: {"taskId":"AI-000319","policy":{"resourceKeys":["unity-project","documentation"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"2026-10-23用户人工反馈指定显示缺陷，新产品缺陷卡；旧289/292机器验收和所有历史保持。319独立通过后按同一修复指令登记内部执行门才派320；320最终修复画面待人审，不解除292整体A+C或USER-U5。","updatedAt":"2026-10-23T23:28:29.2321212+08:00","supervisionLevel":"full"}}
## AI-000320 | [ENGINE_CLI] [SC] [CK01C] 人工缺陷Catalog场景落地与StepDetail黑字

- 序号：320
- 提交时间：2026-10-23T23:28:29.4628817+08:00
- 模块 ID：SC
- 初始状态：QUEUED
- 前置依赖：AI-000319, AI-000287, AI-000293
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：M / 80

### 指令

AI-000320 | A+C人工缺陷场景落地：单问号/空卡/步骤黑字 | 固定ENGINE_CLI | M/80
用户要求见同目录用户反馈与部分验收.json；Scope=AI320_SCOPE.json；五合同inputs/acceptance/AI-000320.json。产品人工缺陷，usageCategory=task，semanticKind=human-acceptance-defect，origin=AI289/D-T2。只在319 CONTROL独立通过和具体内部执行gate后由CONTROL正式派发。最终humanGateAfter=true，不代用户确认修复后画面。

目标仅Assets/Scenes/Cooking/ShortCycle_P0P1.unity：原八CatalogCard实例采用319已验互斥/空卡逻辑，按交接精确绑定名称标签底和胶带等装饰；复用现有主图/placeholder与问号素材，禁止新导入或改原prefab。原生先读实际对象与Renderer边界，再选唯一锁定问号；测原大小两者实际可见/world尺寸，在二者之间选中间量并限制卡框内部、保持素材纵横比和居中，不使用过大算术均值导致仍出框。记录具体before/after数值与取值依据。只改保留问号的局部尺寸/位置必要字段，不改变卡本身布局、其它菜图或全局scale。八实例一致规则，原16SpriteRenderer+LocalizedText标签/卡02绑定/4食材/便签/收藏保留。

StepDetail十个现Description TextMeshPro通过原Editor API .color设纯黑RGBA(0,0,0,1)，保持font/fontSharedMaterial/文本/LocalizedText/字号/RectTransform/排序/_TUNE不变，允许一致Color32派生缓存。检查是否有其他实际可见步骤字体仍白，仅本步骤文本范围；若扩大至不明标题/材质先报告精确差异不改。用户明确Browse和StepDetail排版留人工，本卡不重排/调字体尺寸或修品红素材。

status/editor_status各一次核真实项目/clean基线/当前绑定；官方CLI/EditorAPI/Undo/SerializedObject保存本卡改动，不手编Unity YAML。319新源码装载确有必要时按已授权显示修复执行一次官方recompile（正常内部Refresh/域重载），未知写入/终态unknown/平台拒绝停止不换工具；已载则不重复。Library仅正常缓存观察不改缓存。若用户未保存改动不覆盖，G26仅清自有临时变动。复读允许diff；禁止重建49节点、原scene/Build/P2/view5/adapter/meta或整批旧备份恢复。

验证只本卡fresh task/run，先独立核287/293显式隔离pre-Awake入口，隔离根outputs/DETACH-B13-20261019/AI-000320/isolated-saves/<fresh-run>，报告原流/PNG根outputs/AC-HUMAN-FEEDBACK-20261023/AI-000320。用304当前Ready及298/299/305已验调用方案，官方capture_game_view(source=screen,1024x576,no-save)，原流+原生PNG严格门保存。只必要Catalog已锁白菜一个问号/空区域背景胶带都隐藏，已配置卡/标签/收藏不被误开或隐藏；同run打开清汤面StepDetail核黑色文本，正常返回与Stop/G26。最多两张必要原生图（Catalog/StepDetail），不重拍Browse、不跑RunOriginalProbe/原1～20/P2往返/Build/Player/数据导出。若实际改动≤10字段则按AGENTS硬限只保留一张必要截图，另一用精确允许diff和字段证据，不能为凑图扩大修改。

保留旧已验数据/解锁语义，原生实际显示及新场景保存结果才可Complete，离线交接不能代验。Console当前与历史分开，无连续起点不声称新增0。清理目标先毁后释lease，恢复原clean Edit/宿主关闭。最终修复画面仍交用户签收，原AI292整体人工门和USER-U5不变。固定原chat、PowerShell7及正式queue。读取CORE/对应worker/模块入口和已知坑账，保留用户已有未提交修改。源码/原生事实优先，严禁全仓重复哈希/旧1～20/Build/导表/真实用户存档。写入前正式SupervisionCheckpoint。失败立即按正式规则结算回传，不自续/换工具；Complete/Fail/Yield本轮Prepare→一次实际appSend→Record给CONTROL01a13ef5-eda9-7882-b1c9-eca47a6470d9。CONTROL Receive后独立核验，不自行派后继。包目标1MB硬10MB，保留失败和未结原件。无Git提交推送、素材导入、非空配表修改。源码/资源完整核验日期不提高。

> SUPERVISED POLICY at 2026-10-23T23:28:29.8746531+08:00: {"taskId":"AI-000320","policy":{"resourceKeys":["unity-project","documentation"],"reviewDependsOn":["AI-000319"],"humanGateAfter":true,"gateApproved":false,"reason":"2026-10-23用户人工反馈指定显示缺陷，新产品缺陷卡；旧289/292机器验收和所有历史保持。319独立通过后按同一修复指令登记内部执行门才派320；320最终修复画面待人审，不解除292整体A+C或USER-U5。","updatedAt":"2026-10-23T23:28:29.7101254+08:00","supervisionLevel":"full"}}

> SUPERVISED EXTEND at 2026-10-23T23:28:30.1245017+08:00: {"taskIds":["AI-000319","AI-000320"],"reason":"用户2026-10-23人工验收反馈明确要求Catalog单问号/尺寸/空卡装饰与StepDetail黑字，新增CODE→ENGINE缺陷卡；未重开任何已成功卡，仍同session。"}
## AI-000321 | [CODE] [WF] GIT-CP-01 当前阶段本地检查点提交

- 序号：321
- 提交时间：2026-10-24T00:03:24.7041064+08:00
- 模块 ID：WF
- 初始状态：QUEUED
- 前置依赖：AI-000319
- 允许越过：False
- 最大尝试次数：3
- 复杂度/轮次预算：S / 40

### 指令

AI-000321 | GIT-CP-01 当前阶段检查点提交 | 固定CODE | 原用户S20目标，API技术S40上限（保持原目标，不因大量已有文件扩大验证）
用户已直接明确“先在当前阶段做一轮git的提交…包内…都进行显性的授权”。授权原件本目录用户授权与接收.json，原始派发包/cards/GIT-CP-01.md与索引§〇。任务仅当前工作树本地检查点，不push/合main/切分支，不改业务代码/资源/恢复用户成果。已完成AI263～318及AI319源码可纳入；319 CODE已CONTROL独立通过但场景/视觉仍延后，提交不声明人工验收通过。当前已有原批次/管线/文档归档改动整体保全纳入合理检查点，不通过只提交几文件隐藏大量已完成工作。原12逻辑卡与G87轨在提交说明列明；清楚指出布局/USER-U5及320场景修复待验。

先读CORE/CODE worker/Git hook约束和实际git状态。CONTROL预核1165条路径、index空、branch codex/unity6-followup；原始git-status-before.z/git-change-inventory.json位于outputs/control/BATCH2-INDEPENDENT-20261023。此只是预核，实际边界以本卡开始状态为准。CODE319已结算、ENGINE闲置，新卡/场景未执行；本卡持unity-project+git-index+documentation锁。只接收本任务，不自行派后继。
允许正式git add/commit所必需index/object/HEAD更新，是用户明确提交授权，不手工编辑.git、重置/改写历史或创建隔离index/tree/ref；queue Scope禁止.git字面目录故只列工作树目标与git操作，不能误当绕保护读写.git文件许可。正常git过程需平台升级权限时使用正式审批入口，明确已有用户授权；不禁hook、不--no-verify、不改core.hooksPath/字体白名单绕过。现pre-commit原字体/LFS保护必须正常运行并保存真实stdout/stderr/exit。

盘点并合理暂存当前已授权阶段源码/scene/asm/meta/Packages/ProjectSettings/项目入口及既有完整文档历史归档（旧tracked删除须与归档保全映射对应）。读取.gitignore/attributes，先用path列表/体积排除Build约2.83GB、isolated-saves、临时Assets/__CatalogBlobBuild_*、性能四件残留、runtime/log/cache、Library/Temp、活输出、凭证和用户同步美术；不force-add被忽略运行物。历史.ai-workspace/archive已有原件为文档归档，不能一律当临时删掉；ZIP按已有LFS规则，不改属性逃检查。保留未关闭活动原件，不移动/删除本卡外文件。过大未知binary/疑似密钥须具体定位后停止，不打印密钥。无需全仓内容哈希、旧全套Final或Unity，只索引路径/体积、相关diff、现字体提交hook。原目标SHA可核但禁止替用户回滚任何无关变化。

提交前正式SupervisionCheckpoint，再以正常git stage明确审核文件列表（路径含中文用安全文件列表/参数）；核cached name-status和excluded目录没有进入索引，原字体hook无漂移。git commit一次成功检查点（若hook真实拒绝正式Fail，不自行改保护/强行二提交）。提交message体说明12业务逻辑卡+G87修复+319源码和未验项，可写本卡临时message文件，不命令插值。保留commit前HEAD/branch、暂存清单、排除清单/理由、真实hook证据、commit SHA/父提交/提交后status。仅本轮checkpoint，不替后续修复提交，不push。

交付REPORT.md、evidence.json checks四合同：checkpointCommitCreated、existingHooksPassedWithoutBypass、reproducibleAndRuntimeArtifactsExcluded、historyAndPendingAcceptancePreserved。receipt绑定真实SHA与产物；状态若非clean准确解释剩余（queue生命周期INSTRUCTIONS等），不为clean清空文件。所有证据写outputs/BATCH2-INDEPENDENT-20261023/AI-000321。临时包目标1MB硬10MB只报告索引不复制全仓。结算本轮Prepare→一次真实appSend→Record回CONTROL 01a13ef5-eda9-7882-b1c9-eca47a6470d9，不等WATCH。失败保留现场、只正式Fail，不自行ApproveResume；本批仍SUPERVISED，人工门延后不代签。

> SUPERVISED POLICY at 2026-10-24T00:03:25.2773331+08:00: {"policy":{"resourceKeys":["unity-project","git-index","documentation"],"reviewDependsOn":[],"humanGateAfter":false,"gateApproved":false,"reason":"用户本轮明确先提交；319源码独立通过；旧320及A+C延后不放行。","updatedAt":"2026-10-24T00:03:25.1894562+08:00","supervisionLevel":"full"},"taskId":"AI-000321"}

> SUPERVISED EXTEND at 2026-10-24T00:03:25.4430959+08:00: {"reason":"接收20261023批次2独立项，用户要求检查点先行，同session监管。","taskIds":["AI-000321"]}