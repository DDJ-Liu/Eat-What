# AI 工作流协议 · 核心入口

先读 [CORE.md](CORE.md)。队列动作、状态机、问题分级、熔断、租约、恢复、完成门槛和测试移交的完整现行规则在 [protocol/WORKFLOW_FULL.md](protocol/WORKFLOW_FULL.md)。本文件是兼容入口，不替代完整协议。

## 执行最短路径

PRE-5b 新门：新领取任务的 Complete 必须附任务卡预声明的 acceptance.json 与同任务证据；Compare-Acceptance.ps1 拒绝缺项、假 pass、错归属与路径逃逸。Poll/Complete/Checkpoint 的命令行回执是 <=2KB 摘要，全文保存在返回的 receiptPath；跨线程接力必须读取 messagePath 原文。修复/诊断卡必须携机器父任务与性质字段，跨模式共用两次根修复预算。详见完整协议与 [PRE-5b 用量/监管入口](../DevTools/Workflow/USAGE_REPORTING.md)。

1. 按任务卡指定的模式、车道、TaskId、TriggerSource 与 DispatchToken 领取；非 `CLAIMED/CLAIMED_RETRY` 不执行。
2. 领取后只处理该任务；按任务卡和 worker 限制写入，长任务按协议续租。
3. 成功且完成相称验证后 `Complete`；真实阻断按分级 `Fail`；监管任务先执行 `SupervisionCheckpoint`。
4. `Complete/Fail/Yield` 返回 handoff 时，把原消息发送到指定线程，然后停止；不得自行改模式或派发未授权后继。

DATA 车道沿用同一队列/token/lease/停止门。旧注册态仅兼容 `executorKind=CONTROL_SUBAGENT`；控制台用 `DataLaneStatus` 核查闲置/预约，再以 `BindDataPipeline -PipelineThreadId <id> -UserDecision <授权> -Verification <应用可达与闲置证据>` 原子绑定可见线程。绑定后所有路由必须为 `THREAD` 与精确 `threadId`，缺失/损坏时拒绝回退；轮次与隔离发布入口见 [workers/DATA.md](workers/DATA.md) 和 [DATA_PIPELINE.md](../DevTools/DataTables/DATA_PIPELINE.md)。

详细节索引：运行模式 §0；不变量 §1；车道 §2；状态 §3；领取 §4；问题分级 §5；熔断 §6；维护 §7；完成门槛 §8。

PRE-5c R-ST：ENGINE/DATA生产写首败停；CODE同稳定key首败后重试≤2；控制拒验只阻断任务/依赖链。120真实app turn先保存交接后继续；240临时硬停或软限后30分钟无实际进度硬停。完整合同见WORKFLOW_FULL PRE-5c；旧AI201 REJECTED与失败史保留。
