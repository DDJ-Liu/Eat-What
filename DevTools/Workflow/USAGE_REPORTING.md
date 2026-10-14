# 用量日报与周报（TK-WF-03）

## PRE-5b 增量：WATCH、provider 与 Coplay（TK-WF-04）

原 ai 监管 heartbeat 当前保持 PAUSED；本工具交付不擅自启用或更改应用日程。watch.ps1 以正常 PowerShell 7 无模型运行，单轮默认、-Continuous 默认5分钟；也可由 Manage-WatchHost.ps1 所示受控宿主定时。状态源为队列/worker/mode、可选线程 cursor 与 Unity 实例心跳、USER_WORK_STATUS；源缺失/陈旧降级，不把 WATCH 自身日志计作任务进度。六类增量事件及证据写入持久 inbox，相同事件去重、单批次仅调用一次 -DeliveryScript。批次ID、事件摘要、创建时mode/session/scope冻结；外部调用前先原子写DISPATCHING与attempt ID，进程中断后只读核回执并转UNKNOWN，不自动重投。UNKNOWN（包括抛错或回执不匹配）只读检查同名 .receipt.json 的 batchId/eventDigest/eventCount/delivered 四字段；新的事件另立批次，不混入旧 UNKNOWN。仅适配器明确返回 NOT_SENT 且 attempted=false 才记确定失败；旧会话PENDING转EXPIRED_SCOPE保留、不在新链补发，旧 pending/failed 需控制端核定。STOP/MANUAL/非scope/迟到结果禁止派发，light 不发 full 专属租约/无进度警报。桌面端没有正式事件 run-now API，目前只有可测试适配接口；控制台需另行部署、接入并核真实回执，不把夹具算实机唤醒。

USER_DONE 去重键包含持久的同一 USER 条目完成转换序号；只有实际观察到非DONE→DONE才递增。DOING→DONE→DOING→DONE 两次完成各一事件，重复读取DONE不新增事件。任务人工验收状态回到待审后再次确认也独立计轮。

Import-ProviderUsage.ps1 只接受精确 task/attempt/claim-complete 时间窗的原始 provider 响应，不读取凭证或别的聊天；按 responseId 去重，单响应 usage/token/model 进入原 runtime JSONL。2026-09-28至10-04 的控制端原始事件导入240条、区间外拒绝1条；2026-10-07周报只是隔离 OutputRoot 预览，正式应出前不发布。get_usage_limits 的 usedPercent 仅账号共享配额变化，不能记作任务 token；无 normalModelSlug 时 model=null 与原因保留。

Summarize-CoplayLog.ps1 要求 taskId/attempt/时间窗/requestId/response 的实际带标签 JSONL，按请求 ID 去重计数与返回 UTF-8 字节；缺日志为 null，不能以0替代。Import-CoplayUsage.ps1 可登记经控制端明确绑定的实际样本，必须标明客户端观察还是服务端 RPC，不能拿夹具冒充真实 Unity 操作。领取时上下文只统计 CORE、任务卡及点名输入的原文字节/SHA，完成时配额原值另存。日报/周报的 WATCH 次数与 provider 覆盖字段不改变原北京周三上整周、补报及已发布报告不可覆盖规则。

最后专项核验：2026-10-04。此入口不触发业务任务或改变运行模式。

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
