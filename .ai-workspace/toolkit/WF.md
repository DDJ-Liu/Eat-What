# 工具库 · WF

## 下一批次通用续行（2026-10-12）
<a id="approve-resume"></a>

四条硬上限以 [AGENTS.md](../../AGENTS.md#下一批次四条硬上限2026-10-12-用户决定) 为准。当前保持 MANUAL、watch-60 暂停；交付本入口不代表批准任何生产重试。

唯一批准入口 `ApproveResume(task, runs=N, scope)` 对应 `.ai-workspace/queue.ps1 -Action ApproveResume`，实现共用 `approved-resume.ps1`。MANUAL/AUTOMATIC/SCHEDULED/SUPERVISED 共用同一份授权逻辑；监管暂停时须先有明确的恢复监管决定。`ResumeApprovedTask`、`ResumeCodeValidationStop` 已停止发放许可；`ResetTask` 不允许清除失败任务历史。新入队任务自动带 `resumePolicy=BOUNDED_APPROVAL_V1`，禁止从旧 `RecoverSupervisionTask` 或普通 Poll 绕过授权；该旧恢复 API 仅保留前批次记录的兼容行为。旧 `APPROVED_RESTORE` 专用卡停止新建。旧专用许可/读证据代码只用于历史记录，不是下一批次入口。

`Scope` 为 JSON：`{"paths":["Assets/Prefabs/Example.prefab"],"operations":["bind"]}`。paths 为明确的项目相对路径/目录（目录包含子路径），拒绝通配符、越界、控制状态和重定向路径；operations 为本次实际操作名称。自动包含同范围 `idempotence-check`，不授权范围外写入。用户原话、稳定消息ID `ApprovalId`、原线程 `OwnerThreadId`、控制台身份和已有的 fresh idle 证据一并记录，不要求源码/资产全量哈希或 Editor 内存证据。

- `Runs` 是允许新领取的轮数，不是每个 API 调用次数；领取时消耗一轮，幂等检查和该轮验收不再额外发许可。一次性工具仍受单次 Editor 执行上限约束。
- 同一 `ApprovalId` 重复提交不会增加次数或扩大范围；尚有次数时可经同入口重新排队，次数耗尽须新的用户批准。普通失败仍先结算停止，不自动重放；用户明确“失败即停”时不得使用余量擅自继续。新的平台权限拒绝或 GLOBAL 停止封存余量，不能借旧批准绕过。
- 领取 `Poll` 必须传实际 `-Scope`；监管任务每批 `SupervisionCheckpoint` 也传实际范围。派发和自动接力消息已包含领取范围。队列检查次数、范围、原线程/路由、任务定义和失败史；这是入口约束，执行器仍须按用户批准操作，不宣称队列能拦截任意 Editor/外部工具写入。
- 不改 maxAttempts、累计失败、修复预算或人工验收；新增修复额度仍须用户决定，续行许可本身不生成修复卡。成功任务不能靠剩余次数再领取。

示例参数形态（占位值须替换为真实任务与人类决定，不能直接执行）：

```powershell
$scope = '{"paths":["Assets/Prefabs/Example.prefab"],"operations":["bind"]}'
& './.ai-workspace/queue.ps1' -Action ApproveResume -TaskId '<原任务ID>' -Runs 1 -Scope $scope -ApprovalId '<用户消息ID>' -UserDecision '<批准1轮及上述范围的原话>' -ControllerThreadId '<控制台ID>' -OwnerThreadId '<原执行线程ID>' -IdlePipelines ENGINE_MCP -Verification '<实际闲置证据>'
```

验证只维护共享 `DevTools/Workflow/Test-ApprovedResume.ps1` 及既有模式回归，不再为任务、错误或单次许可增加专属夹具。历史 `Test-ApprovedRestore.ps1`、`Test-UserCodeResume.ps1` 已退出 Workflow Final；`Test-FontSwapOnce.ps1` 已退出 Fonts Final。Final 的 Custom runner 同样检查 `DevTools/one-time-tools.json`，不能把已登记的一次性 runner 手动塞回聚合。

> 从 [INDEX.md](INDEX.md) 定位；状态与公开面沿用原登记。

<a id="tk-wf-01"></a>
<a id="tk-wf-02"></a>
<a id="tk-wf-03"></a>
<a id="tk-wf-04"></a>

## 10. WF · 工作区与留档

| ID | 名称/路径 | 能力 | 状态 | 使用边界 |
| --- | --- | --- | --- | --- |
| TK-WF-01 | DevTools/Workflow/Archive-Stage.ps1 | 已闭合阶段的计划、打包、逐文件哈希验证、精确清理和历史提取；`PermanentInputPatterns` 保留跨阶段常驻 inputs，`SelfTest` 回归默认与自定义白名单 | 沿用 | PowerShell7；仅MANUAL闭合队列；Prune只删除已验证清单中的未变化文件且不清理常驻输入，Extract只写归档缓存；不恢复生产资产或更改任务状态 |
| TK-WF-02 | `.ai-workspace/queue.ps1` DATA API；`DevTools/DataTables/DataRound.Common.ps1`、`DataPipeline.Common.ps1`、`Run-DataPipeline.ps1` | DATA 注册、可见线程状态/原子绑定、route-key 票据防串线、U1 DONE 轮次幂等消费、共享锁、隔离 batch、明确GUI进程等待与当前run完成凭据、源/目标/完整复制范围快照冲突门和保全式回滚发布 | 沿用 | AI-000160/163/164/166/174；注册/绑定/重分类/消费仅控制端；绑定前需应用可达/闲置证据，绑定后无效路由拒绝回退；当前CSV验证模式强制不发布且不伪造DONE；隔离 batch 接 PRE-1 字体构建但字体不进入生成物事务；AI164正常宿主两真实用例0/1差异及独立nonce已通过；DATA/ENGINE 共享全程锁；不提交 |
| TK-WF-03 | `DevTools/Workflow/UsageMetrics.ps1`、`Initialize-TaskUsage.ps1`、`Invoke-TaskTool.ps1`、`Get-UsageReport.ps1`、`Invoke-UsageReporting.ps1`；`DevTools/Verify-All.ps1` | 任务生命周期真实/缺失用量 JSONL、可选工具包装、日/周聚合与按 profile 的统一验证入口 | 沿用 | AI-000157/173；不建第二队列，不估算 token/容量；`runtime/usage-YYYYMM.jsonl` 只追加；离线测试必须传临时 RuntimeRoot/StateRoot；Final 默认两次，超限留原因；子runner固定正常PS7 `-NoProfile -File`，不传ExecutionPolicy绕过参数 |

2026-09-17：增加阶段归档工具；配表v1.2兼容夹具固定到DevTools/DataTables/Fixtures，AI65历史审计改为按需提取图证。完整旧登记表在阶段ZIP中，所有原有TK编号及当前能力表保留。

TK-WF-04（PRE-5b）：DevTools/Workflow/watch.ps1、Manage-WatchHost.ps1、Compare-Acceptance.ps1、Import-ProviderUsage.ps1、Import-CoplayUsage.ps1、Summarize-CoplayLog.ps1 与队列修复预算门。WATCH 只观察、不领取；单轮或5分钟宿主定时，持久增量 inbox 与单批次适配器回执；桌面事件唤醒尚无正式接口，部署由控制台负责。Complete 强制预声明机器验收，根修复跨模式最多2轮；provider仅精确关联真实事件，Coplay无日志为null。隔离夹具见 Test-Watch/Test-Acceptance/Test-RepairBudget/Test-CoplayUsage。

2026-09-22：TK-WF-01 增加可配置常驻输入白名单与 `SelfTest`，避免排布指导书、Schema、需求基准、模块/路径主本和 USER 人工状态在阶段归档时被移动。

2026-09-23：AI-000157 新增 TK-WF-03，用现有 queue/supervised 生命周期挂钩记录实际可见数据；provider 未暴露项保持 null+原因，并由统一验证入口复用既有 runner。

2026-09-23：AI-000160 新增 TK-WF-02；沿用同一队列与监管生命周期，不建立第二队列。AI-000174 在同一 worker 状态中增加可查询、幂等、带前后值/UserDecision/Verification 审计的固定 DATA 线程绑定；预约保存 route key，换绑/旧票据/忙碌状态均安全拒绝。

2026-09-23：AI-000163 加固 TK-WF-02：事务按“未动live/已移动/已提升/回退失败”显式保全，增加目标与输入快照漂移门、固定Unity版本/隔离目录和不带USER轮次的只验证入口；真实Unity结果不由离线回归代签。

2026-09-23：AI-000166 再加固 TK-WF-02：Windows GUI batch 使用本次 Process 句柄等待/取码，锁保持到退出与凭据核验；报告拆分真实可空进程码和Runner码，完整输入快照对齐五个复制根，真实GUI延迟夹具覆盖非零/成功/缺旧凭据与锁生命周期。未启动Unity，AI164仍需DATA恢复复验。

2026-09-23：AI164已在两个精确命令各经正式审批的正常宿主通过Unity6000.3.24f1真实baseline/overlay，退出0/0、不同nonce、0/1生成差异、主Editor共存和生产不变。受限宿主LocalLow/许可失败的旧记录保留；零diff时空JSON缺失记录WARNING，独立清单补证，不新增工具或擅改运行器。详见 `outputs/U1-PRE/AI-000164/host-context-recovery-20260923/`。

2026-10-04：TK-WF-03增加 `DevTools/Workflow/Invoke-UsageReporting.ps1`、`USAGE_REPORTING.md`、`Test-UsageReporting.ps1`，原ai固定前置调用；北京时间周三上一整周、漏报补齐、真实补发标记、源快照和幂等原子发布。Get-UsageReport支持同一Records快照并将单次/合计排名分开。只写outputs，暂停后不自行Resume；保持原计量null口径。

2026-10-05 PRE-5c / AI-000201：TK-WF-03 增真实 app turn 绑定计数、跨 attempt 120 上限、provider 响应单列、明确分母 cache 比率；`RecordCodeValidation` 按稳定 key 留首败/重试/解决证据。TK-WF-04 增 `Get-UnityHeartbeat.ps1`、`Read-WatchInbox.ps1`；WATCH 读真实 worker 进度和嵌套监管等级，新增 WORKER_PROGRESS/UNITY_STATE_CHANGED/TURN_LIMIT 幂等事件。`Manage-WatchHost.ps1` 可选只读采集但未部署。长期回归 `Test-Pre5c.ps1`、`Test-ControlAdministration.ps1`、`Test-UserCodeResume.ps1` 纳入 Workflow profile。

TK-WF-04 回传修复（2026-10-05）：`Manage-SupervisionHandoff.ps1` 的 Prepare/Record/Receive 绑定、一次发送与控制接收去重；`Test-SupervisionHandoff.ps1` 纳入 Workflow。使用 `DevTools/Workflow/HANDOFF.md`；WATCH事件、app发送、控制验收分别计量。


AI208扩展TK-WF-03/04：queue.ps1 Sync-TaskTurnGate累计120软限/240临时保护/30分钟无实际进度；WorkCompleted证据SHA去重，观察不刷新计时。RejectVerification绑定原结果，ResolveControlRejection绑定完成修复与独立证据；取消scope不改成功。Test-TurnSoftRepair及Test-ControlCompletedReview纳入Workflow，保留UTC/预算/回传回归；正式硬限待瘦身后两周实测。旧AI201的120硬停为历史实现，已被本合同替代；其REJECTED历史保留，2026-10-05 20:39经独立控制复核后正式ResolveControlRejection解决本次阻断。

TK-WF-04 2026-10-06：ResumeApprovedTask + approved-resume.ps1 为原ENGINE/DATA失败任务提供一次证据绑定的获批续行，卡/恢复配对预算与失败历史不变；Test-ApprovedResume纳入Workflow。不是自动重试、不是清失败/人工签收。

2026-10-06 原只读诊断补验续行：ResumeApprovedTask 另接 APPROVED_VALIDATION_RESUME，仅对同根 ENGINE/DATA 的一次 TOOL_UNAVAILABLE 只读诊断中断；需人类续行原话、独立环境/编译/清理方案证据、全部失败史/原修复结果/源码与全序列化快照 SHA、fresh idle 与同 session/route 绑定。一次凭证只续原卡，保留 attempt/maxAttempts/错误/人工gate，不另占修复slot。只有该补验实际完成且独立 VERIFIED、凭证已消费且失败史未变，才可在根生产恢复时排除这条工具中断的时间屏障；原错误仍永久保留，新生产/验收失败不豁免。实现 .ai-workspace/approved-resume.ps1，真实 queue 回归 DevTools/Workflow/Test-ApprovedResume.ps1；不替代生产写阶段或用户清理。
2026-10-07 TK-WF-04：明确授权的APPROVED_COLD_PROCESS_VALIDATION仅补原214一次独立进程首载；120项真实队列隔离回归，原错误/次数/旧许可/预算保持，第三次失败拒绝再续。完整合同见SUPERVISED_FULL同日节。

2026-10-07 TK-WF-04宿主续行：新用户授权对应APPROVED_HOST_PROCESS_VALIDATION；同卡三失败/旧许可链与两种宿主只读证据绑定，166项queue隔离回归。Unity两启动各需独立平台审批；新失败停。见完整协议同日宿主节。

2026-10-09 TK-WF-04：原AI217探针场景一次补验入口及220项queue回归，保留全部失败/预算；边界见完整监管协议同日节。

2026-10-09：已批准Static恢复/恢复后只读预检入口与49项回归见 protocol/SUPERVISED_FULL.md 最新节、DevTools/Workflow/Test-ApprovedRestore.ps1；保持同根修复预算。
