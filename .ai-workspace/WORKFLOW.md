# AI 工作流协议 · 核心入口

先读 [CORE.md](CORE.md)。队列动作、状态机、问题分级、熔断、租约、恢复、完成门槛和测试移交的完整现行规则在 [protocol/WORKFLOW_FULL.md](protocol/WORKFLOW_FULL.md)。本文件是兼容入口，不替代完整协议。

## 执行最短路径

PRE-5b 新门：新领取任务的 Complete 必须附任务卡预声明的 acceptance.json 与同任务证据；Compare-Acceptance.ps1 拒绝缺项、假 pass、错归属与路径逃逸。Poll/Complete/Checkpoint 的命令行回执是 <=2KB 摘要，全文保存在返回的 receiptPath；跨线程接力必须读取 messagePath 原文。修复/诊断卡必须携机器父任务与性质字段，跨模式共用两次根修复预算。详见完整协议与 [PRE-5b 用量/监管入口](../DevTools/Workflow/USAGE_REPORTING.md)。

GOV-05：新Enqueue必须传`-Complexity S|M|L`，预算由40/80/120派生；未领取旧卡由控制端用`ReviseInstruction -Complexity`正式补登记。已运行旧卡按原规划来源兼容结算，历史不回填。分级卡Complete须传四类`-IdleTurnsJson`（真实app turn ID或null+原因），超预算另传`-BudgetExplanation`；轮次预算仅作提示，GOV-02软/硬门不变。详见[CORE分级表](CORE.md)与[TK-WF](toolkit/WF.md)。

1. 按任务卡指定的模式、车道、TaskId、TriggerSource 与 DispatchToken 领取；非 `CLAIMED/CLAIMED_RETRY` 不执行。
2. 领取后只处理该任务；按任务卡和 worker 限制写入，长任务按协议续租。
3. 成功且完成相称验证后 `Complete`；真实阻断按分级 `Fail`；监管任务先执行 `SupervisionCheckpoint`。
4. `Complete/Fail/Yield` 返回 handoff 时，把原消息发送到指定线程，然后停止；不得自行改模式或派发未授权后继。

DATA 车道沿用同一队列/token/lease/停止门。旧注册态仅兼容 `executorKind=CONTROL_SUBAGENT`；控制台用 `DataLaneStatus` 核查闲置/预约，再以 `BindDataPipeline -PipelineThreadId <id> -UserDecision <授权> -Verification <应用可达与闲置证据>` 原子绑定可见线程。绑定后所有路由必须为 `THREAD` 与精确 `threadId`，缺失/损坏时拒绝回退；轮次与隔离发布入口见 [workers/DATA.md](workers/DATA.md) 和 [DATA_PIPELINE.md](../DevTools/DataTables/DATA_PIPELINE.md)。

详细节索引：运行模式 §0；不变量 §1；车道 §2；状态 §3；领取 §4；问题分级 §5；熔断 §6；维护 §7；完成门槛 §8。

PRE-5c R-ST：ENGINE/DATA生产写首败停；CODE同稳定key首败后重试≤2；控制拒验只阻断任务/依赖链。120真实app turn先保存交接后继续；240临时硬停或软限后30分钟无实际进度硬停。完整合同见WORKFLOW_FULL PRE-5c；旧AI201 REJECTED与失败史保留。

GOV-02 软限交接：累计真实 app turn 到 120 时正式返回 `HANDOFF_REQUIRED`。执行器在当前固定职能 chat 用 `queue.ps1 -Action WriteHandoff -Pipeline <线> -TaskId <ID> -OwnerThreadId <当前路由> -ExecutionId <本次执行> -HandoffJson <JSON>` 保存已完成、剩余、关键状态、证据路径及下一命令（完整文件 ≤4 KiB），再 `YieldSupervisionTask`；同一固定 chat 下一轮领取时从 `handoffFrom` 先读该 SHA 绑定文件，累计回合、错误与修复预算不清零。未经有效交接，`CheckManagedTool`/`Invoke-TaskTool` 拒绝执行；记录观察、交接、Yield/Fail 等结算入口仍可用。该门禁只覆盖受管入口，不声称拦截任意平台外部工具。WATCH 对软限后 30 分钟无交接报 `HANDOFF_STALLED`，硬限 `TURN_LIMIT` 附交接路径；Heartbeat 不算实际进度。240 和无实际进度 30 分钟硬停不变。失败续行仍只用 `ApproveResume`。

2026-10-14 用量优化：普通 `Status` 是≤2 KiB摘要、最多20条需关注任务并含溢出数；恢复任务、查询成功旧卡或需要完整 mode/worker/queue 字段时显式 `-TaskId <ID> -FullStatus`，不能把摘要误作全文。验证优先受影响 Custom runner；只有逐 runner 闭包清单显式传给 `Verify-All -ReuseManifestPath` 才考虑同任务成功复用，未知输入一律执行。WATCH 先判停，再做输出/线程/Unity观察；周报/日报到期或漏报时独立补齐，不因业务暂停消失，也不每次空轮重扫用量源。旧监管日程和 ART 日程未由此改动。
