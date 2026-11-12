# AI 工作流协议

2026-10-19 续行政策：MANUAL每次真实人批1；AUTOMATIC默认3、SUPERVISED默认5个同根周期；首次执行和诊断不计，同周期修复与根续行不双扣。来源、消费和停止门统一见[TK-WF](../toolkit/WF.md#approve-resume)，实现验收状态见任务报告；不授权执行器自行重启业务。


> 2026-10-12 下一批次优先规则：执行 [AGENTS.md 四条硬上限](../../AGENTS.md#下一批次四条硬上限2026-10-12-用户决定)。≤10 字段只验允许清单 diff + 一张截图；全路径哈希每批末一次；资产 SHA 仅提交 hook，任务中无内存保护；一次性工具仅离线编译 + Editor 单次执行，不进 Final。失败续行统一 [ApproveResume](../toolkit/WF.md#approve-resume)，幂等算同一批准。下文 FONT-R 专用许可/内存保护/冷启动测试是历史，禁止按旧规则新增续行入口或夹具。当前 MANUAL/watch-60 暂停，不再做字体返工。

> 2026-09-22 H后安排是历史时点，完整原文见[本批页首快照](../archive/2026-10-17-workspace-correction/readable/.ai-workspace/outputs/control/DOC-SLIM-20261016/entry-before/.ai-workspace/protocol/WORKFLOW_FULL.md)。当前入口为[CORE](../CORE.md)及[E2收束](../MIG63/E2_收束与提交.md)；固定Unity 6000.3.24f1，main只接验收集成。设计指导以[输入索引](../inputs/README.md)登记的最新有效版本为准，不从旧页首取得新业务授权。

> 2026-09-15验证清理修订（用户明确）：MCP/Play自身操作产生且归属已核实的临时dirty由执行器自动恢复，不新增人工审批或仅凭dirty熔断。执行前记录恢复点及临时操作，执行后核对并清理；未知/用户已有未保存变化及实际平台拒绝仍保护。完整流程见`.ai-workspace/UNITY_SCENE_CLEANUP.md`，ENGINE_MCP必须执行并交清理证据。

> 2026-09-15美术导入顺序修订（用户明确）：ART同步/内容差异 → ENGINE_MCP只读识别与工程比对 → 控制台提交逐文件最终路径/命名/复用替换/暂缓及配置草案 → 用户审阅 → ENGINE_MCP直接导入正式目录并按批准范围配置。禁止默认先放入ArtImports后才识别规划。详细规范见AGENTS.md“美术资产比对、命名与导入顺序”和`.ai-workspace/templates/ART_IMPORT_REVIEW_TEMPLATE.md`。既有资产迁移保GUID，源素材保留；这项工作习惯不改变当前模式。

## 现行验收规则（2026-11-11 用户 v2）
<a id="acceptance-20261111"></a>

本节是所有角色的现行执行口径，覆盖下文旧验收次数、探针门、自动配对续行和后台回归安排。来源为 [用户原话及原件](../inputs/派发/ACCEPTANCE-REVISION-20261111/用户本轮原话与适用范围.json) 与同目录《验收规则调整说明_发开发端_v2_20261111.md》。只局部维护，不提高工程完整核验日期；不解除用户停止或平台权限边界。

### 七条硬规则

1. 开发先保证逻辑与数据：针对生产源码的离线编译、单测、数据/配表校验及静态检查；此段未过，不进入引擎验收。
2. 引擎验收从首次起累计最多 **3轮**，不是首次加3次重试；可人工检查的功能也不能超过此数。
3. 第3轮仍未通过即停止；可人工检查的功能转“待人工检查”，不新记产品失败，不再向用户询问追加验收轮次、不执行第4轮。
4. 交人工必须交检查说明、已有证据、未经机器验证的内容和逐轮坑账；有表现效果的任务无论机器通过或熔断都交说明。
5. 三轮用于发现更深的逻辑/数据问题与积累管线坑账，不用来反复机器评判手感。
6. 计划内与临时反馈、需求漂移修正、重构、修复包一律规划低难度验收粒度；一张卡只承接一个可验证目标，不把完整复杂行为规则塞进实机验收。
7. 接线、增删组件、保存、导入、构建等 **操作轮不适用三轮验收熔断**；按原分模式及已批准额度执行，超额再向用户申请。

### 三段验收与断言边界

顺序固定为“离线逻辑与数据 → 必要引擎检查 → 用户功能/表现检查”。机器卡断言≤3；版本级仍只有一张S40冒烟、一次Play、≤5断言，两者均只能使用以下三类：

| 允许类别 | 内容 |
| --- | --- |
| 加载与接线存在 | 场景可打开；对象、组件、引用存在；旧组件已移除；操作保存后读回相符 |
| 运行中无新增报错 | 一次Play期间Console新增Error为0；历史警告/错误与覆盖缺口如实分列 |
| 数据到位 | 驱动/配置输出可读取且有限；按一次键数值有变化，只判断“变了” |

禁止引擎内断言：行为时序/先后、数值阈值或误差范围、逐帧采样/连续性、速度/加速度/曲线形状、手感/观感、截图比对、用户输入监视。相应行为正确性放生产源码单测，表现交用户；不能用“数据到位”变相判断变化多少或多快。
验收卡仍写明“不截图、不CI消费判定、不Build判定、不新写门/比较器/身份校验、不在验收卡内修复”；不得在卡内加断言、守卫、比较器或adapter。Build操作卡可以检查构建结果，这不授权在验收卡中追加Build门。

### 分类计数与停止

| 类型 | 分类时点与计数 | 上限与停止 |
| --- | --- | --- |
| 操作轮 | 本轮结束时操作仍未完成；纯接线/保存/导入/构建卡始终按操作轮 | 沿现分模式：MANUAL每次真实人批1，AUTOMATIC默认3次续行，SUPERVISED默认5次续行；首次不计重试，明确卡级批准及既有累计不清零；超额才申请 |
| 验收轮 | 操作已完成后，为检查功能执行；纯冒烟/探针卡每轮均计验收 | 从首次累计3轮，产品/管线/守卫/超时/租约/中断等所有原因均计；模式切换、修复卡、探针/封装更新不重置 |
| 混合卡 | 两类分列，以实际执行与操作完成边界记账，不把已完成操作后的验证改名为操作 | 两类互不占用；不要把整卡attempt或rootRetry冒充验收累计。历史实际attempt/错误/许可不删改退款 |

可人工检查功能验收满3轮转“待人工检查”；不可由用户画面判断的内部状态/基础设施验收同样满3轮停下报用户，**不能自动转人工判通过，也不能继续或主动申请追加验收次数**。用户stop、权限拒绝和未知未保存成果仍独立停止；本节不允许换入口绕过。
管线修复可独立修管线本身，先离线再必要的三类SMOKE；对可人工检查功能取消“修复后配对原卡再验收”，不得为已熔断原卡派出修复→冒烟→继续原验收链。修复不退款、不增加原验收轮次。后台全量32项探针及定时回归暂停，不新建不执行，恢复由用户明确决定。

### 接卡与交付

设计端每卡（含临时包）写清三段；多步卡附“步骤→实现位置→状态所有者”。CONTROL在领取/派发前核断言类别与数量、操作/验收分类、累计轮次及人工交接范围；不符停驻回设计端，不自行扩充或改写验收。可独立离线检查的行为不能转给引擎。执行器只执行已核清单。
人工检查说明使用五列：**检查什么｜怎么操作｜预期看到｜不符合时先调哪个参数｜属于调参还是实现问题**。表后写调参入口（资产/字段/运行时是否即时生效）、未经机器验证内容、已知限制；用用户语言，不塞内部卡号或证据路径。不要求用户截图，不代签最终体验。
每轮引擎失败在坑账记录：现象；层级（产品/封装/守卫/宿主/环境，未知写未知）；当时处置；以后如何避免；附任务/实际轮次和证据定位。失败史留存不等于当前产品验收判失败。

### 正式API与实现边界

纯验收新卡可用现有Enqueue `MaxAttempts=3` 并经ConfigureSupervisionTask `FailureStopLimit=3`收紧；CONTROL每次派发仍核跨模式累计，不能只靠可被追加许可覆盖的旧MaxAttempts。混合卡的两类计数在控制计划/报告中分别记录；现有API没有独立双计数设置，不能谎称运行时已实现，也不手改runtime或为本次规则立新机制卡。
尚在运行且满足真实完成条件的卡可沿现RecordIssue/Complete保留PENDING_USER；**已经NEEDS_USER且result为空的历史失败卡，现API不能直接行政转换为PENDING_USER**（ResolveVerification/CloseWithoutAcceptance均要求SUCCEEDED）。不得重Poll凑结算、伪造Complete或删失败；CONTROL记录新的“待人工检查/机器验收撤销”处置，并明确技术队列字段暂保原值。后继依赖只用现UpdateDependencies按用户明确范围调整；该差异不是恢复验收或扩大授权的理由。

## 0. 运行模式

- `SCHEDULED`：保留原有任务清单、三条流水线和每 10 分钟轮询/维护机制。
- `MANUAL`：没有周期巡检或任务接力，所有执行都由用户当次指令触发。
- `AUTOMATIC`：进入时按依赖和序号构建线性链；成功任务直接发送下一棒，失败时停在原任务等待人工。
- `SUPERVISED`：控制台按依赖图和资源互斥多线派发，接收完成/阻断回报并限次恢复；人工闸门不代过。完整协议见 `.ai-workspace/SUPERVISED.md`；仅在此模式下覆盖本文旧 FIFO、失败只等用户和执行器接力规则。切模式不隐式重置任务。

2026-09-09：SUPERVISED 当前scope全部完成后自动回 MANUAL（无失败/REJECTED/未通过人审依赖闸门；普通非闸门测试待验保留）。最后 Complete 与 SupervisionStatus 兜底结算并要求控制台暂停原监管日程。已取消控制台ART日报轮询/回传/12小时催报与自动拆解，ART午夜生成及红项消费安全门不变；详见 SUPERVISED/CONTROL_CHAT。

当前模式以 `.ai-workspace/runtime/mode-state.json` 为准，完整切换和接力协议见 `.ai-workspace/MODES.md`。`Poll` 的 `TriggerSource` 必须与当前模式一致；`Maintain` 仅在 `SCHEDULED` 生效。

## 1. 不变量

1. `runtime/queue-state.json` 是机器状态的唯一真相来源，`INSTRUCTIONS.md` 是日常只追加的提交账本。文档维护时，依 `DOC_MAINTENANCE.md` 第16条对已关闭历史做原字节归档后轮转；稳定路径、未关闭完整内容和正常追加能力保留，历史通过归档索引追溯，不改机器状态或原历史证据。
2. 只有控制任务可以入队或改写任务定义；执行器只能认领、记录非阻碍问题、续租、完成或报告阻断失败。
3. 每个执行器同一时间最多持有一个任务。
4. 任务只有在所有 `dependsOn` 任务均为 `SUCCEEDED` 时才可执行。
5. 默认按每条流水线的 `sequence` 严格 FIFO；受阻的队首任务会阻止同流水线后续任务越过。SUPERVISED 是显式例外：在依赖、人审、资源互斥均满足的候选中按 sequence 选择，独立分支可越过受阻队首。
6. 未完成验证不得写入 `SUCCEEDED`。
7. 只有阻断任务目标、安全边界或可信验收的失败才触发熔断。无副作用、可确定修正或已有安全后备路径的非阻碍问题必须记录后继续，不得因第一次工具参数错误、临时探针编译错误、陈旧只读快照、普通 Warning 或可选验证失败立刻停工。
8. 验证范围以当前任务阶段为界：纯代码任务只负责源码编译、静态/单元验证与接线契约；由后置引擎任务创建的 Animator、场景引用或 Play Mode 结果，不得反向作为纯代码任务的完成前置，否则会形成伪依赖或循环依赖。
9. `runtime/queue-state.json` 与 Worker 的 `activeTaskId` 是跨上下文压缩的任务身份真相。发生上下文压缩后必须先恢复 `activeTaskId` 对应指令，禁止让更早的用户消息或旧接力覆盖正在运行的任务。只要任务仍为 `RUNNING` 且租约未到期，不得仅凭 Worker=`BUSY` 判为 `ORPHANED_RUN`；当前轮应恢复同一任务，外部新唤醒则只观察并结束。只有已确认任务线程在上一轮结束后处于 idle，且租约已到期或控制端明确提供孤儿证据时，才可记录 `ORPHANED_RUN`。
10. 默认任务租约为 30 分钟。预计超过 20 分钟的任务必须主动 `RenewLease -LeaseMinutes 30`，不得依赖长租约掩盖失联。
11. Unity MCP 的成功/失败必须依据完整响应判断：先读 `structuredContent.success/code/error`，缺少结构化字段时再解析 `content[].text` 中的 JSON。不能只因 `structuredContent.data` 缺失就判定 `MCP_UNAVAILABLE`；`success:false`、对象不存在、空结果等都属于已收到的业务响应。
12. 使用 Unity `instanceID` 做多轮场景遍历时，必须把它视为仅在当前 Editor/场景快照内有效。遍历前后以及对象不存在时复查 Editor sequence、Domain Reload 时间、活动场景与 Play/Compile 状态；快照失效时丢弃旧 ID 并从根重新取数，不得继续混用新旧快照。
13. 只读查询、截图或验证工具若明确返回“动作/参数不受支持”并同时给出支持值，且调用没有产生部分写入，可以先读取当前工具 schema，修正参数后在同一任务内重试一次。这类可确定、无副作用的调用错误不应在第一次出现时直接熔断；若重试仍失败，或调用可能已产生部分修改，仍须按真实错误留痕停止。
14. 无副作用的截图/只读验证若返回 `TimeoutError` 且明确提示 `retry`，先检查目标文件或响应是否其实已经产生，再复查 Editor 是否处于稳定状态。没有产物时允许降载重试一次（例如关闭内联图片、指定 Camera 直接渲染、避开 `Assets` 导入目录）；不能在 Play Mode 正在切换时立即发起依赖渲染帧的截图。降载重试或已批准的确定性截图后备仍失败时才熔断。
15. Unity MCP 的 `editor/state.is_changing` 必须按当前插件实现解释。项目现用版本把它映射为 `EditorApplication.isPlayingOrWillChangePlaymode`，稳定 Play Mode 中也可能为 `true`；禁止把 `is_playing=true && is_changing=false` 设为硬门槛。稳定运行应以 `is_playing`、暂停/编译状态和两次运行时帧数或时间递增共同确认。
16. 配置数据的唯一权威来源是仓库根目录 `DataTables/` 下的 UTF-8 CSV。Unity `.asset` 配表由统一导表框架生成；除一次性紧急修复外不得手工逐字段维护，紧急修复后也必须回写 CSV。
17. `ModuleId` 是需求模块与任务对账的可选不透明标识；队列只负责保存、过滤和展示，不解释或校验其命名格式。
18. 任务指令引用 AI 工作区文件时必须写完整显式相对路径 `.ai-workspace/<文件名>`。执行器对 `.ai-workspace` 一律先用 `Test-Path` / `Get-Content -LiteralPath` 直接访问；只有需要枚举时才用 `rg --hidden --no-ignore`，不得通过 `git ls-files`、默认 `rg` 或 IDE 默认搜索判断文件存在性。

## 2. 流水线分类

| 标识 | 工作范围 | 禁止事项 |
| --- | --- | --- |
| `ENGINE_MCP` | Unity Editor、场景、Prefab、序列化资源、组件接线、Console、Play Mode、执行统一 CSV 导入并生成/更新 `.asset` | MCP 未连通时继续猜测或手改可由 Unity 安全修改的 YAML；手工逐字段填写由导表管理的配置 |
| `CODE` | C#、PowerShell、编辑器外脚本、测试、静态分析、文档化代码产物；`DataTables/` CSV 内容、schema、统一导表框架与静态校验器 | 打开/操作 Unity Editor，修改场景、Prefab、`.asset`、`.meta` 等序列化资源；把 xlsx 当作权威源 |
| `DATA` | 消费已 `DONE` 的 USER-U1 轮次；Quick 校验、隔离 batch 导表、生成物 diff/catalog、字形清单和事务发布 | 修改业务 `.cs`、手工编辑序列化资源、把 EXPORTED 当 DONE、在原工程跑 batch、自动改字体或提交 |
| `ART_AIGC` | AI 生成/编辑的临时位图、草图、贴图、概念图和变体 | 操作 Unity、直接部署 Assets、修改代码；不得领取新增的同步或日常盘点任务 |
| `ART_ASSET` | 美术资产同步、盘点、文件完整性审查及独立工作日午夜差异审查 | 操作 Unity/AIGC；未经范围授权覆盖冲突或删除本地保留项；日常审查不得写源或同步 |

2026-09-08 起美术车道对外名ART，内部ART_ASSET（同线程/Worker，-Pipeline ART为别名）。显式同步任务用AI编号，日常规范审查用ART-DAILY-YYYYMMDD，不调用AI Poll/Complete、不进批次完成率；详见 `.ai-workspace/workers/ART.md`、`.ai-workspace/templates/ART_DAILY_TEMPLATE.md` 和CONTROL_CHAT增补。部署逻辑依赖ART同步或指定日报，日报外部依赖保存为artDailyIds，消费源路径artSourcePaths受未处置红项拦截（不能按测试问题降级）。当前任务安全收尾后优先待审，不抢占RUNNING、不随批次结束停用。历史ASSET-AUDIT与ART_AIGC记录不迁移或删除。

一个复合需求必须拆成多个单流水线任务。例如“写脚本 A，再在 Unity 中接线 B”应拆成：

```text
AI-000001 [CODE] 编写并静态验证脚本 A
    ↓ dependsOn
AI-000002 [ENGINE_MCP] 在 Unity 中配置脚本 A 并做 Console / Play Mode 验证
```

“同步美术并导入 Unity”必须拆成：

```text
AI-000003 [ART_ASSET] 从 NAS 同步到根目录美术资产区并输出报告
    ↓ dependsOn
AI-000004 [ENGINE_MCP] 只读识别/工程比对，提交最终路径、命名和逐项处置草案
    ↓ 用户审核最终映射/命名/配置范围（不先建立ArtImports）
后继任务 [ENGINE_MCP] 按已审清单直接导入正式目录、配置Importer与获准使用点并验证
```

“新增或修改配表”固定拆成统一导表链：

```text
USER-U1-Rn 明确 DONE（轮次、CSV摘要、用户决定）
    ↓ dependsOn
DATA-Rn [DATA] Quick 校验 → 隔离 Unity batch → diff/catalog/字形 → 事务发布与报告
    ↓ 需要正式场景/运行验收时
AI-yyyyyy [ENGINE_MCP] 在 Unity 检查引用、Console 与运行行为
```

每张表一个 CSV，按系统放在 `DataTables/Cooking/`、`DataTables/NPC/` 等目录；新增表只增加 schema 与 CSV，不新建一套独立导入脚本。Excel 只可作为编辑前端，入库前必须导出为 CSV。

DATA 的旧注册态明确返回 `executorKind=CONTROL_SUBAGENT` 与 `threadId=null`，只作迁移兼容，不能把 null thread 当成功发送或用内部子代理代执行新任务。控制台在应用层核实可见 DATA 任务可达/闲置后，通过 `DataLaneStatus` 与 `BindDataPipeline` 把同一 worker 原子绑定为 `THREAD`/精确 threadId；绑定有 routeRevision 和前后值/UserDecision/Verification 审计，预约 route key 不匹配即失效，绑定损坏时 fail closed。注册、绑定、轮次消费和重分类都是控制端动作，执行器不得调用。DATA 与 ENGINE 对导表源、生成物或相关 Unity batch 必须共同独占 `data-publication.lock`，且从 Unity 启动前持有到退出和结果核对完成。

## 3. 状态

- `QUEUED`：等待执行。
- `BLOCKED_DEPENDENCY`：前置任务尚未全部成功。
- `RUNNING`：已由对应执行器认领。
- `PAUSED_CIRCUIT`：执行失败，任务保留，等待熔断冷却后重试。
- `NEEDS_USER`：达到最大尝试次数或需要人工决定；保留并阻塞该流水线的 FIFO 队首。
- `SUCCEEDED`：已执行并完成验证。
- `CANCELLED`：仅由用户明确取消后设置。

`verificationStatus` 与任务状态正交：`PENDING_USER` 表示成功任务等待人工验收，`REJECTED` 表示用户已验收不通过但原任务仍保留 `SUCCEEDED` 和证据；返工由控制任务另行修订/重置或拆分新任务。

## 4. 执行器每次领取

`SUPERVISED` 唤醒先读 `.ai-workspace/SUPERVISED.md`：固定 TaskId + DispatchToken 领取；每批写入前检查 SupervisionCheckpoint；Complete/Fail/Yield 的 supervisorHandoff 只回控制台，不接力后继。阻断先 Fail 保留，控制台有任务内授权和限次恢复权；明确人工点使用 Fail -FailureScope HUMAN，生产写入结果不明/数据安全风险使用 Fail -FailureScope GLOBAL（原子禁止全部新派发）。普通 TASK 失败只阻断该分支，不锁死整条流水线。

1. 开始时先调用 `queue.ps1 -Action Status`，不要自行编辑 JSON，并确认触发消息声明的来源与当前模式一致。
2. 若本流水线为 `BUSY`，读取 `activeTaskId`、任务状态与 `leaseUntil`。若当前执行轮在上下文压缩前已经领取该任务，或 `activeTaskId` 等于本轮 AUTOMATIC 当前节点，则直接按显式路径重读该任务指令并继续，不再次 `Poll`，也不得解释更早的用户消息。租约未到期时，外部新唤醒只观察并结束，不调用 `Fail`。仅当控制端/调度消息已明确确认上一轮任务线程结束且 idle，并且租约已到期或附带等价孤儿证据时，才调用 `Fail -ErrorKind ORPHANED_RUN -CooldownMinutes 10`。
3. `SCHEDULED` 调用 `Poll -TriggerSource SCHEDULED`；`MANUAL` 和 `AUTOMATIC` 必须同时传入消息指定的 `TaskId` 与对应 `TriggerSource`。不得自行改变来源或任务 ID。
4. 若返回 `SKIP_CIRCUIT`、`SKIP_BUSY`、`BLOCKED_DEPENDENCY`、`BLOCKED_NEEDS_USER` 或 `EMPTY`，立即结束本次运行。
5. 若返回 `CLAIMED` 或 `CLAIMED_RETRY`，保存返回的任务 ID，只执行该任务；本轮不得再次 `Poll`。唯一例外是该任务已成功 `Complete`，且返回的 AUTOMATIC `autoHandoff.threadId` 与当前任务线程相同：此时上一任务的 Worker 已释放，可按返回的下一 `TaskId` 在同一轮再 `Poll` 一次继续接力。
6. 通读 `.ai-workspace/KNOWN_PITFALLS.md` 中适用于本流水线的条目，再严格遵守任务指令、项目 `AGENTS.md` 和对应 `.ai-workspace/workers/*.md`。任务引用的其它 `.ai-workspace/...` 文档同样按显式路径直接读取，不先做仓库检索。
7. 任务涉及新建脚本、扩充公开面或表现/交互行为实现时，读 `.ai-workspace/TOOLKIT_REGISTRY.md` 对应域，按条目状态决定：`沿用`=直接消费（唯一构件，绕开须 `PENDING_USER` 声明）；`扩展中`=按备注的扩展需求做；`规划中`=按拟定公开面建并在完成时改状态为沿用；`冻结/弃用`=不碰。完成报告附“工具登记影响”。
8. 执行时间接近 20 分钟时调用 `RenewLease -LeaseMinutes 30`，之后每 20 分钟内至少续租一次。
9. 成功后调用 `Complete`，写入结果摘要、产物路径和实际验证证据。
10. 遇到问题先自行诊断、搜索现有实现/schema/文档并尝试安全的最小修复或后备方案；修复失败后才按第 5 节分级。测试表现异常或测试触发失败只记录并移交人工验证；其它非阻碍问题记录后继续；只有阻断问题调用 `Fail`。
11. `AUTOMATIC` 下，`Complete` 返回 `autoHandoff.action=TRIGGER_TASK` 时先比较目标 `threadId`：若目标是其它流水线线程，发送完整 `message`；若目标就是当前任务线程，禁止活动轮次自发消息，直接在本轮用返回的 `taskId/pipeline` 和 `TriggerSource AUTOMATIC` 领取并执行下一项。返回 `CHAIN_COMPLETE` 时结束。`Fail` 后禁止发送或领取任何后续任务。

## 4.1 流水线线程中的验收回复

流水线线程收到用户直接消息时，只识别以下两种固定格式；其余消息不执行、不入队，只回复“请在控制台下达”后结束：

- `验收通过 AI-xxxxxx[：备注]`：仅当 TaskId 属于本流水线且为 `SUCCEEDED + PENDING_USER` 时，调用 `ResolveVerification`；备注缺省为“用户线程内确认通过”，验证证据写“用户在流水线线程直接验收”。
- `验收不通过 AI-xxxxxx：<原因>`：仅当 TaskId 属于本流水线且为 `SUCCEEDED + PENDING_USER` 时，调用 `RejectVerification`。不得修改任务定义、重置、返工或触发后续任务。

TaskId 不属于本流水线时提示用户转对应线程或控制台。执行器仍禁止 `Enqueue`、`ReviseInstruction`、`ResetTask`、`ReclassifyTask` 等控制端动作。

待人工验收统一通过 `queue.ps1 -Action PendingReview` 或 `.ai-workspace/PENDING_REVIEW.md` 查询；其中同时列出 `PENDING_USER` 与 `REJECTED`。

## 5. 自修复、问题分级与延迟汇报

### 固定处理顺序

引擎验收依[现行验收规则](#acceptance-20261111)，不在验收卡内修探针/守卫或使用下列旧内部重试扩大三轮；下列一般修复流程仅用于获准开发/操作。

1. 先确认现场和错误边界，判断失败调用是否产生写入。
2. 优先自行查找解决办法：读取当前工具 schema、搜索项目已有用法、缩小复现、修正临时探针/参数，或采用任务已批准的安全后备。
3. 对同一根因最多做两次有依据的最小修复/后备尝试，禁止不改变条件的盲目重试。
4. 修复成功则继续任务，并在最终摘要中简要说明；修复仍失败才判断为“测试待人工”“其它非阻碍”或“阻断”。
5. 修复尝试若会扩大任务范围、可能破坏生产资源或需要新的用户决策，不自行执行，直接进入分级。

### 可恢复的非阻碍问题

同时满足下列条件时，不调用 `Fail`：

1. 已确认失败调用没有修改生产资源，或部分结果已被完整回滚并复核；
2. 不影响任务核心目标、安全边界和最终验收可信度；
3. 存在确定的参数修正、局部代码修正、重新获取只读快照或任务已批准的安全后备路径；
4. 修正不需要越过流水线边界或改变用户需求。

典型例子：临时验证探针的语法/API 拼写错误、无副作用工具参数错误、可重建的只读快照、普通 Warning、可选截图路径失败但仍有批准的确定性捕获路径。

每个这类问题先记录：

```powershell
queue.ps1 -Action RecordIssue -Pipeline <PIPELINE> -TaskId <TASK_ID> -IssueKind RECOVERABLE_EXECUTION -IssueMessage "问题、无副作用证据和采用的修正"
```

`RecordIssue` 用于修复失败后仍需保留的非阻碍问题；已在同一轮完全修复且没有残余风险的问题可以只在完成摘要中汇总。

### 测试问题固定移交人工验证

轮次、三类断言及当前历史失败卡的交接以[现行验收规则](#acceptance-20261111)为准；以下API示例仅用于仍在RUNNING且具备真实完成条件的卡，不授予失败卡新领取。

测试过程中出现的表现异常、视觉观察不确定、自动测试未触发、输入/动画/截图/探针未能稳定触发，或测试工具自身产生的异常，在自行修复仍失败后不得升级为阻断问题。使用：

```powershell
queue.ps1 -Action RecordIssue -Pipeline <PIPELINE> -TaskId <TASK_ID> -IssueKind TEST_OBSERVATION -IssueMessage "测试表现、已尝试的修复、现有证据和人工复核步骤"
queue.ps1 -Action RecordIssue -Pipeline <PIPELINE> -TaskId <TASK_ID> -IssueKind TEST_TRIGGER -IssueMessage "未触发的测试、已尝试的修复和人工触发步骤"
```

然后完成其余工作并调用 `Complete`。队列会把 `result.verificationStatus` 设为 `PENDING_USER`、`manualVerificationRequired` 设为 `true`，但任务状态仍为 `SUCCEEDED`，因此依赖它的后续任务可以继续领取，`AUTOMATIC` 模式也必须照常发送下一棒。

后续由用户或控制任务完成针对性复核并取得明确证据后，使用 `ResolveVerification -TaskId <TASK_ID> -Summary <结论> -Verification <证据>` 把结果转为 `VERIFIED`。此操作不重开、不重置成功任务，原测试问题保留在 `nonBlockingIssues`，复核结论追加到 `verificationResolutionHistory` 与指令账本。

测试暴露出的产品崩溃、生产编译错误、数据损坏或不明确写入不属于单纯测试表现/触发问题，仍按安全影响判断是否阻断。

### 可降级但不阻断完成

仅影响可选证据、附加体验或非硬性产物时，可以继续并最终 `Complete`。必须在 `RecordIssue` 和完成摘要中说明未完成项、替代证据与风险。实现产物、产品编译、数据安全等非测试硬性条件不能降级；测试表现或测试触发证据缺失则按上一节标记 `PENDING_USER` 后完成。

### 必须立即停止的阻断问题

以下情况调用 `Fail`：

- MCP/工具/权限不可用，且没有安全后备路径；
- 写入调用结果不明确、可能部分修改生产资源，现场无法安全确认或回滚；
- 产品源码或 Unity 工程出现无法在本任务边界内修复的编译错误；
- 核心实现产物、依赖、产品编译或非测试硬性验收无法成立；
- 同一必需的非测试步骤修正/后备已经耗尽；
- 上下文丢失、租约或外部状态使执行器无法可靠判断当前现场。

`Complete` 会把 `issueHistory` 写入 `result.nonBlockingIssues`，并根据测试类问题生成 `verificationStatus/manualVerificationRequired/manualVerificationIssues`；若最终升级为阻断，`Fail` 返回的任务中也保留全部问题记录。因此中途不需要逐条打断用户，任务完成或真正受阻时统一汇报。

## 6. 熔断

引擎验收的当前规则见[累计三轮与人工交接](#acceptance-20261111)；以下旧冷却/续行行为只适用于允许继续的开发/操作，不能触发验收第四轮。

- 默认冷却：30 分钟。
- `ORPHANED_RUN` 使用 10 分钟冷却，便于下一个 heartbeat 重试，同时保留完整错误历史。
- 阻断失败把当前任务置为 `PAUSED_CIRCUIT`，把执行器置为 `OPEN`，并保存错误记录。单纯 `RecordIssue` 不改变任务或 Worker 状态。
- 熔断期间轮询只能读取执行器状态，不读取或认领新指令。
- 冷却不是重试授权。只有CONTROL核实时授权、分类累计及停止门后才走正式续行；验收三轮熔断不再续。
- 技术状态保留既有正式API行为；业务验收满3轮按本节新规则交接，操作额度按分模式。原始指令、历史错误和依赖保留。
- 问题解决后，仅由控制端按用户明确次数/范围通过 `ApproveResume` 续原卡；不能用ResetTask或新任务绕过失败记录。
- `AUTOMATIC` 中失败不会因冷却到期自行重试；链进入 `WAITING_USER`。只有人工确认并重置当前节点后才能再次发送该节点 handoff。

## 7. 控制端定时维护（仅 `SCHEDULED`）

- 控制端每 10 分钟调用一次 `queue.ps1 -Action Maintain -PollOverdueMinutes 20`。该动作只回收到期租约、报告三端心跳和未完成任务，不领取业务任务。
- 控制端必须同时检查三个目标任务线程和三个 heartbeat 自动化；线程仍在活动时不得将其任务判为孤儿。
- 流水线空闲、熔断关闭、存在依赖已满足的待办且连续 20 分钟没有轮询时，控制端可向对应任务线程发送一次明确的单轮轮询指令。
- 自动化缺失或被暂停时，控制端恢复原自动化；不得创建重复的流水线自动化。
- `NEEDS_USER`、连续失败或无法安全自动恢复时通知用户并继续保留巡检，不擅自重置尝试次数。
- 当前受监控批次全部为 `SUCCEEDED` 后，控制端汇总结果并删除或停用自己的巡检自动化；三条通用流水线 heartbeat 可保留为 `ACTIVE` 等待后续新指令。
- `MANUAL` 与 `AUTOMATIC` 不执行 `Maintain`，三个通用 heartbeat 保持暂停；自动模式只依靠成功回执的消息接力。

## 8. 完成门槛

视觉效果类任务另遵 `.ai-workspace/ENGINEERING_CONVENTIONS.md` C8：统一在 `Assets/Scenes/ToolTests/VisualEffectsLab.unity` 先完成试样、接线和独立运行验证，交用户视觉验收后再安排正式场景接入。首次建设未交付时标为待建；不能用静态编译、截图存在或工具返回成功替代视觉验收，也不得由测试任务擅自覆盖正式场景/共享渲染配置。排布指导书及代码清单审查保持有效。

完成记录必须包含：

- 做了什么；
- 改动或产物路径；
- 实际执行的验证，以及本轮记录的非阻碍问题与采用的修正/替代证据；
- 仍需人工验证的事项、`verificationStatus` 和人工复核步骤（如有）；
- 文档影响：是否影响 `CODEBASE_MAP.md`、项目推进基线或其它活文档；
- 工具登记影响：引用/新增/扩充/更名/弃用的 `TOOLKIT_REGISTRY.md` 条目；无则明确写“无”。

分阶段交付时，链路的最终运行时验收由最后一个 `ENGINE_MCP` 任务承担。前置 `CODE` 任务可以在尚无 Animator/Prefab/场景实例时完成，但必须保证：代码可编译、缺失序列化引用时行为安全、Inspector 接线契约明确，并把运行时验证显式交给后置任务。`[SerializeField]` 引用尚未在 Inspector 赋值本身不是代码失败；静态验证器若把此类诊断提升为错误，应修正字段初始化或验证策略，而不是要求提前创建序列化资产。
## PRE-5b · 完成门、根修复与响应

新任务以 .ai-workspace/inputs/acceptance/<TaskId>.json 预设每个必需检查的 id、expected、evidencePath、valuePath；Complete 传 -AcceptancePath，由 DevTools/Workflow/Compare-Acceptance.ps1 对同任务证据逐项核对。缺项、重复、类型、假 pass、错任务、路径逃逸或重解析点均拒绝；旧 SUCCEEDED 不回写。Poll/Complete/SupervisionCheckpoint 仅输出 <=2KB 身份/状态/路由/receiptPath/messagePath，详细回执写 outputs/task-receipts/<TaskId>/；消费方按路径读完整 handoff。

新修复卡用 -WorkKind REPAIR -ParentTaskId <root>，只读诊断用 READONLY_DIAGNOSIS。同根自动修复总预算 2，跨编号、模式、problemKey 不清零；已配对的监管恢复与修复卡按同一轮计，历史恢复数原样保留。常见先修复后恢复顺序：原任务依赖已成功的修复卡，RecoverSupervisionTask 传 -PairedRepairTaskId <card>，核同根、成功、晚于最新失败、未配对后才生成恢复ID并回写卡片，计同一个slot；不能故意先Recover以获取ID。先恢复后建卡保留 -PairedRecoveryId 路径。第 2 次失败只允许只读诊断，下一修复须关联诊断且有明确用户决定；Enqueue/ReviseInstruction/ResetTask/RecoverSupervisionTask 共用该门，不借新编号绕过。


## PRE-5c · 分级停止与真实回合

AI208基础与GOV-02补齐：真实 app turn 按事件去重、跨attempt累计；120只登记一次 TURN_SOFT_LIMIT 历史事件，对执行器正式返回 HANDOFF_REQUIRED。固定职能chat当前轮调用WriteHandoff保存≤4 KiB的已完成/剩余/关键状态/证据路径/下一命令到runtime/handoffs/<task>-<attempt>.json，再YieldSupervisionTask；下一轮同chat领取时校验SHA并注入handoffFrom，先读后续行。路由变更也不清累计；无有效交接时CheckManagedTool/Invoke-TaskTool拒绝受管执行，但不声称拦截任意平台外部工具。WATCH在软限后30分钟无交接报HANDOFF_STALLED，TURN_LIMIT带交接路径。累计240或软限后距最后实际进度30分钟触发锁存硬停；240只是临时保护。Heartbeat不算实际进度。Initialize、Checkpoint、RecordTaskProgress和Complete共用Sync-TaskTurnGate，不信任可被重置的limitReached布尔值。缺计数保留null及原因，不能抵消已记录的软限/硬停。
进度默认Observation；WorkCompleted必须有落盘证据，重复ProgressId或同SHA证据、Heartbeat、只读观察、领取和交接均不刷新实际进度。Invoke-TaskTool只在成功完成工作后记录进度；只读调用显式传-ProgressKind Observation，开始/失败均不充进度。
控制拒验只阻断本任务及其依赖；独立链继续。RejectVerification在队列锁内绑定原结果快照/SHA、CONTROL_ACCEPTANCE、稳定key及证据SHA，保留原报告和历史。只有显式ResolveControlRejection，绑定后续已完成的同根修复、独立控制台、两结果SHA及最新拒验证据后，才追加解决记录；不自动修改AI201 REJECTED，也不代过人工gate。仅有正式SupersedeTask取消依据的CLOSED_SUPERSEDED可作为scope已取消收口，状态不改SUCCEEDED，后继仍须满足真实依赖。
正式硬限待瘦身措施落地后连续实测满两周再定。本卡将独立读取批处理、紧凑回执与全文证据分离、一次Workflow聚合和重复事件去重落地；本次只有离线样本，未满足两周，不宣称240为正式最终值。WATCH宿主部署仍待验。
ENGINE/DATA生产写失败首败停并只读诊断；CODE交付前同一稳定key首败后重试≤2，用RecordCodeValidation追加失败/解决证据，不改预算。旧AI201 UTC误停审定和所有历史保留。JSON读入继续DateKind String，时间必须有显式时区，比较UTC瞬时值。Poll/Complete/Checkpoint摘要目标≤2KB，Top5合计<4KB，全文按receiptPath/messagePath读取。
