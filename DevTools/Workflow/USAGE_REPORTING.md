# 用量日报与周报（TK-WF-03）

2026-10-15 AI-000246回执兼容：`Write-Result`先将`PSCustomObject`快照转为保留原字段的有序字典，再沿用上下文写入；`DataLaneStatus -ControllerThreadId`与Owner回执均支持正常、unknown及读取异常兜底。原动作/路由字段、阈值、去重、Status体积和计费口径保持。定向覆盖在既有`Test-ContextWarning.ps1`，不需要恢复监管或运行导表。

2026-10-15 AI-000245当前口径：Get-ThreadContext优先读取精确threadId原生token_usage_record.payload.usage.input_tokens（有界发现/尾读/增量缓存，30分钟有效，未来超过5分钟拒绝），次用新鲜旧provider，显式fresh UTF-8字节才作粗估；均缺则contextTokens=null/contextSource=unknown。正式队列普通回执显式传ControllerThreadId/OwnerThreadId，MANUAL Status也可提示；UsageMetrics按响应键锁定去重。ADVISE>=150K、STRONG>=200K只提醒手动重载。WATCH不作模型轮询，只消费新鲜记录；STRONG转入可产生worker或console的最小CONTEXT_HIGH，未知不冒充低量重布防；Read/Ack核session/digest/线程。单次输入不是完整窗口或任务计费，旧报告不重发。下述10-14诊断/首次实现口径保留为历史。

2026-10-14实测限制：runtime provider最近仍停在10-04，GOV04重复取旧137875；精确原生10-14样本控制台190300、CODE197981已为ADVISE。当前reader无陈旧淘汰/自动provider导入，console心跳无真实采样且WATCH未完整消费console记录。不能将NONE当实时低量；本轮只留诊断，不改变模式或新派任务。见[报告](../../.ai-workspace/outputs/control/STAGE-GOV-G74-COMMIT-20261014/提交前检查与上下文提醒诊断.md)。

## PRE-5b 增量：WATCH、provider 与 Coplay（TK-WF-04）

原 ai 监管 heartbeat 当前保持 PAUSED；本工具交付不擅自启用或更改应用日程。watch.ps1 以正常 PowerShell 7 无模型运行，单轮默认、-Continuous 默认5分钟；也可由 Manage-WatchHost.ps1 所示受控宿主定时。状态源为队列/worker/mode、可选线程 cursor 与 Unity 实例心跳、USER_WORK_STATUS；源缺失/陈旧降级，不把 WATCH 自身日志计作任务进度。六类增量事件及证据写入持久 inbox，相同事件去重、单批次仅调用一次 -DeliveryScript。批次ID、事件摘要、创建时mode/session/scope冻结；外部调用前先原子写DISPATCHING与attempt ID，进程中断后只读核回执并转UNKNOWN，不自动重投。UNKNOWN（包括抛错或回执不匹配）只读检查同名 .receipt.json 的 batchId/eventDigest/eventCount/delivered 四字段；新的事件另立批次，不混入旧 UNKNOWN。仅适配器明确返回 NOT_SENT 且 attempted=false 才记确定失败；旧会话PENDING转EXPIRED_SCOPE保留、不在新链补发，旧 pending/failed 需控制端核定。STOP/MANUAL/非scope/迟到结果禁止派发，light 不发 full 专属租约/无进度警报。桌面端没有正式事件 run-now API，目前只有可测试适配接口；控制台需另行部署、接入并核真实回执，不把夹具算实机唤醒。

USER_DONE 去重键包含持久的同一 USER 条目完成转换序号；只有实际观察到非DONE→DONE才递增。DOING→DONE→DOING→DONE 两次完成各一事件，重复读取DONE不新增事件。任务人工验收状态回到待审后再次确认也独立计轮。

Import-ProviderUsage.ps1 只接受精确 task/attempt/claim-complete 时间窗的原始 provider 响应，不读取凭证或别的聊天；按 responseId 去重，单响应 usage/token/model 进入原 runtime JSONL。2026-09-28至10-04 的控制端原始事件导入240条、区间外拒绝1条；2026-10-07周报只是隔离 OutputRoot 预览，正式应出前不发布。get_usage_limits 的 usedPercent 仅账号共享配额变化，不能记作任务 token；无 normalModelSlug 时 model=null 与原因保留。

GOV-04：Get-ThreadContext.ps1 对精确 threadId 仅取最新一条 provider 响应的 inputTokens，记录观测时间/responseId；该单次输入并非真正的完整存活窗口。缺精确响应时只允许传入明确 fresh UTF-8 字节作四字节每 token 粗估，仍缺则 contextTokens=null/contextSource=unknown。Poll 领取与 Complete 各在 usage JSONL 追加一次 role=worker、event=thread_context，按 executionId+生命周期去重；Complete 回执附 NONE/ADVISE(≥150K)/STRONG(≥200K)，NONE+unknown 不表示已测低量。

活跃控制台每个实际 60 分钟心跳最多调用一次 queue.ps1 -Action RecordSupervisionHeartbeatContext -ControllerThreadId <绑定线程> -HeartbeatId <本轮唯一ID> -UsageLimitsSamplePath <工作区内真实get_usage_limits原始输出路径>；若 app 用量不可得，则改传 -UsageLimitsUnavailableReason <原因>，不重试。可选 -ContextFreshBytes 仅供明确新鲜字节粗估。该队列入口不调用 app，也不扫描聊天；样本引用和SHA只说明全账号用量快照，非任务成本。非活动/停止态拒绝采样；EMPTY只做此一次采样，不派业务。WATCH原无模型5分钟轮询不调用此入口。STRONG在WATCH首次跨阈值时产生最小CONTEXT_HIGH入箱，不自动重载、Yield或改模式。旧报告仍不可覆盖；新周报按worker/console和provider/estimated/unknown列覆盖、范围与STRONG次数。

Summarize-CoplayLog.ps1 要求 taskId/attempt/时间窗/requestId/response 的实际带标签 JSONL，按请求 ID 去重计数与返回 UTF-8 字节；缺日志为 null，不能以0替代。Import-CoplayUsage.ps1 可登记经控制端明确绑定的实际样本，必须标明客户端观察还是服务端 RPC，不能拿夹具冒充真实 Unity 操作。领取时上下文只统计 CORE、任务卡及点名输入的原文字节/SHA，完成时配额原值另存。日报/周报的 WATCH 次数与 provider 覆盖字段不改变原北京周三上整周、补报及已发布报告不可覆盖规则。

最后专项核验：2026-10-14（AI230状态摘要、验证复用、WATCH停止门禁与报表到期检查；不代表全项目复核）。此入口不触发业务任务或改变运行模式。

## 2026-10-14 用量优化（AI-000230）

控制收尾已观察既有宿主2026-10-14 16:41真实轮次进入STOPPED_OR_MANUAL，并保持原pending；16:40真实报表检查sourceScan=SKIPPED_ALREADY_PUBLISHED、下一周报应出10-21。旧桌面WATCH仍暂停，宿主注册查询未重试；这不是桌面即时唤醒验收。完整证据见`.ai-workspace/outputs/control/USAGE-OPT-20261014/independent-review.json`。

`queue.ps1 -Action Status` 默认只返回≤2 KiB UTF-8摘要：模式（含监管停止位）、各车道忙闲/租约/熔断、总匹配数、最多20条需关注任务与`overflowCount`。`-TaskId`精确查已成功任务，`-FromTaskId/-ToTaskId`查闭区间；完整机器对象必须显式`-FullStatus`（可与过滤参数合用），不能把摘要`queue.tasks`当全部任务或拿`fullStatus`指令当已落盘的三状态快照。旧机器消费者已切为显式全文。状态回执仍走队列统一计量出口；不把未暴露token/model由返回字节推算。

AI-000231 增量：摘要另含按任务状态与验收状态的精确计数；注意项优先运行、拒验/待验、需人工及熔断，单项最多200 UTF-8字节（id、module、lane、status、verification、updatedAt），超长模块以占位标明截断并通过全文查看。`snapshotPath` 指向 `runtime/status-snapshots/<时间戳>.json` 的未过滤三状态全文；源状态文件元数据未变时直接复用，元数据漂移但三源文件字节未变时核哈希后复用，避免空轮或无操作重写反复落盘MB全文。`-Full` 是 `-FullStatus` 别名，显式全文保留旧结构；`PendingReview` 默认同样返回≤2 KiB摘要与可读全文快照，需要旧items/groups明细时显式加 `-Full`。不存在 ListTasks 动作，未新增。`Get-UsageReport.ps1` 的 daily/weekly 增 `actionMaxResultBytes`，按观测到的动作返回字节取最大，`queue:Status` 即使未观测也列 null 与原因；历史已发布周报不重发。源码/夹具不能证明真实宿主后续报告运行。

`Verify-All.ps1`优先单次Custom运行受影响runner，显式`-ReuseManifestPath`才启用逐runner成功复用。清单schemaVersion=2，每个runner单独列`path`、`dependencyMode: closed`、`sideEffectFree: true`、`scope`、`parameters`、`inputPaths`（相对仓库的明确文件/目录；目录含新增/移除成员）；runner、聚合器、PowerShell环境、声明的输入闭包共同成指纹。无清单、闭包不明、路径缺失/跳转、最新同runner失败、输入变化或运行中漂移都执行，不凭较早成功绕过失败。结果`PASSED/REUSED`和来源写入同TaskId历史；清单是人工逐runner审定的opt-in，不自动推断任意脚本依赖，也不为本卡全Workflow域默认启用。

WATCH先从mode判停：MANUAL、监管暂停/退出、自动COMPLETE/BLOCKED/WAITING_USER等非活动态不做递归任务输出扫描、线程读取或Unity采集/派发；保留inbox未确认票据，不制造ACK。相同空/缺源轮次仅状态变化或北京时间跨日写一次无事件记录；新事件仍按原持久去重机制处理。每轮末的报表调用独立于业务停门。报告器仅在日报首次到期、周三周报到期或漏报时扫描用量JSONL；已发布的同日报/周报通过轻量文件签名索引跳过源扫描，签名变化先重新校验证据元数据和报告SHA，损坏原件保留并抛错，修复后可重新校验。既有正式周报不因本次重写；宿主是否真实调度仍以新run记录为证，离线夹具不算上线。

## 运行与产物

使用项目固定 PowerShell 7.6.5，在项目根执行：

    & './DevTools/Workflow/Invoke-UsageReporting.ps1'

沿用原15分钟监管 heartbeat ai；每轮读取 mode-state 后、观察 worker/判断无工作或退出本轮之前调用。只写报告，业务停止、授权、派发和资源规则保持独立。更改自动化提示时必须保留这条命令；模板存在并不等于线上提示已同步。

- 北京时间每周三到期，汇总上一完整周（周一00:00至次周一00:00），首份应出2026-09-30、对应2026-09-21至27；下一份应出2026-10-07、对应2026-09-28至10-04。
- 每次扫描2026-09-30起所有到期周报，缺失则补齐，保留实际生成时间和isBackfill。不能把补发时间改成应出时间。
- 周报：.ai-workspace/outputs/control/usage/weekly/YYYY-MM-DD_YYYY-MM-DD/report.md 与 evidence.json。
- 日报：.ai-workspace/outputs/control/usage/daily/YYYY-MM-DD/report.md 与旁证；当日首次执行的部分快照，周报重新按整周记录汇总，不拼接这些不完整日报。
- 返回 USAGE_REPORTS_CHECKED、created、existingCount、nextWeeklyDue；新增weekly为本轮交付。仅daily或created为空不算新周报。
- 原ai暂停、应用未运行或平台额度不可用时无法保证准时；不自动解除暂停。恢复后的首次调用自动补齐。没有额外定时器、OS任务或新自动化。
- 输出失败要报告真实错误。已存在但缺文件、JSON损坏或报告SHA改变，保留现场并报错，不能当成已完成或自动覆盖。

## 数据与完整性

读取 runtime/usage-YYYYMM.jsonl；与追加器使用同一mutex获取一致快照。报告器不修改runtime；目录发布使用独立mutex和同目录原子重命名，避免重复发布与半份产物。失败的.pending-*保留诊断，不自动清理未知文件。

旁证包含源文件捕获时SHA/大小、所选原始记录、统计区间、实时时间、覆盖日期、聚合和report.md SHA。源日志以后继续追加，复核历史应以报告旁证的记录快照为准。每份完整报告只发布一次；后续纠错需要保留旧版并明确修订，不能静默覆盖。

Get-UsageReport.ps1保留旧接口（本日/周一至AsOfDate），新增内存Records参数供同一快照复用，并固定北京时间。周报由Invoke入口传上一周日，不能在周三直接用默认AsOfDate冒充上一整周。JSON读取显式保留时间字符串，避免当前PS7.6默认日期转换丢失原offset。

不可观测token/model继续null+原原因。无记录不是零消耗；不同包装的bytes口径保留source/coverage，不把字节估算为token、不分摊账号百分比。Top5单次调用只取toolCallCount=1，多次合计另列Top5记录；跨车道任务列出全部车道。原始聚合未经跨包装去重，只能作为观测量，不能等同账单或全账号用量。

## 验证

DevTools/Verify-All.ps1的Workflow profile已登记Test-UsageReporting.ps1。仅验证计量模块时用Custom同时运行它与Test-UsageMetrics.ps1，StateRoot指向任务证据目录。夹具仅使用临时RuntimeRoot/OutputRoot，不写在线状态。

覆盖北京时间到期边界、跨月和多周漏报补齐、未到期不生成、空记录/未来记录、provider null、多次合计分离、重复执行、真实双进程并发、损坏产物拒绝和源记录不变。

本次首轮Final暴露测试/报告反序列化的日期类型问题，已显式保留字符串；第二轮Final通过13+23项检查，两个历史结果均保留在outputs/control/usage-repair-20261004/verification。

## 2026-10-04漏报诊断

1. 原模板有每日生成命令，但线上ai只保留“09-30周报覆盖不足实报”的描述，没有执行入口、产物校验或到期扫描；AI157工具交付通过不能证明周报实际被触发。
2. 原Get-UsageReport只处理某一日及当周累计，没有周报计划/补漏。恢复工作只接续开发队列，未检查错过的报告日期。
3. 原首报日期跨过周一；09-30按默认区间只会读取09-28起记录，遗漏09-23至25的实际测量。修复明确为每周三报告上一完整周。
4. 10-03恢复记录表明原ai此前处于PAUSED；09-26至10-02无用量记录且未找到09-30周报。无法仅凭无记录证明每一缺口均由额度造成；额度/暂停是运行可用性限制，缺乏补报则是已确认的工作流缺陷。

首份已明确以补报交付；历史记录和既有日报不重写。

## PRE-5c

任务 Codex app turn 与 provider 响应分别计数；120软限、240临时硬保护跨attempt保留；120保存交接后继续，软限后30分钟无实际进度硬停。WorkCompleted须有新证据，Observation/Heartbeat不刷新计时。正式硬限待瘦身落地后实测满两周。Get-UsageReport 的 cache 比率只在已观测 inputTokens 分母上计算，缺分母为 null+原因。WATCH 可选真实 worker 进度和 Unity MCP 心跳；绑定控制台以 `Read-WatchInbox.ps1` 读/ack，空箱立即结束。宿主部署未核，不把夹具唤醒计作实机。
