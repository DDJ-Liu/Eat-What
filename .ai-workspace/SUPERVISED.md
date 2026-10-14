# 监管模式（SUPERVISED）· 核心入口

> 2026-10-09当前：用户“按你提出的合理处理进行操作，切回手动模式”已落实。正式ExitSupervision返回MANUAL，原session为PAUSED_MODE、stopRequested/exitRequested=true；watch-60已暂停，独立ART日程保持。AI207机器11/11与R4人工已通过；AI214旧NEEDS_USER、错误/许可/全部待办保留。下文旧ACTIVE与恢复安排属于历史，不授权自动恢复。

当前实际 mode-state 是权威；本入口不授权自动恢复。2026-10-05 DATA-R1/AI194 已收口，原监管 ai PAUSED，旧AI170 NEEDS_USER 的七失败/六恢复、171/172冻结，P2与Excel人工待验均保留。此前逐次派发、恢复、错误与人工结论全文在[本任务保全快照](outputs/PRE-5b/AI-000200/history/SUPERVISED_before_slim.md)，相对链接以原 .ai-workspace 位置为基准。

监管执行入口为[完整协议](protocol/SUPERVISED_FULL.md)；每批写入前 SupervisionCheckpoint，Complete/Fail/Yield 的 supervisorHandoff 只回控制台。PRE-5b 的 light/full、根修复预算与无模型 WATCH 在完整协议及[TK-WF-04](toolkit/WF.md#tk-wf-04)；WATCH 宿主和真实唤醒回执待控制台部署，不恢复旧 ai。

PRE-5c：本 session 控制台 heartbeat 为 60 分钟，以 mode-state 与 app automation 实际绑定为准。CODE 自验证按稳定 key 在任务内最多两次重试；ENGINE/DATA 生产写与控制台验收均首败任务级停止。WATCH 读嵌套 `supervisionPolicy.supervisionLevel`、真实 worker 进度和已核 Unity 只读心跳；宿主部署与桌面唤醒回执待控制台完成版。

2026-10-05 回传修复：本轮用户明确要求修复 AI205 完成未回传后再继续工作流。当前批执行器结算后按 DevTools/Workflow/HANDOFF.md 向绑定控制台一次性发送完整 supervisorHandoff，记录原始投递结果；控制台接收登记后独立验收。历史 AI191/195 的已拒绝回传不重试、不换通道；暂停/退出不重启。 实现与收件流程见 [HANDOFF](../DevTools/Workflow/HANDOFF.md)。


AI208：120软限保存SHA绑定检查点并交接后继续；240临时硬停/软限后30分钟无实际进度硬停。控制拒验只阻断任务/依赖，显式取消可收口但不记成功。实现及离线证据见outputs/FONT-R-PRE5c/AI-000208/；2026-10-05 20:39独立控制验收通过并经正式ResolveControlRejection解决AI201本次拒验；旧REJECTED记录保留。208证据、真实交接核心复核、206编译与资源核对见outputs/control/FONT-R-PRE5c-20261005/ai208-review-20261005/。

ResumeApprovedTask 的原生产根获批续行、20分钟一次凭证、三份独立结果及原错误/预算保护，详见[完整协议](protocol/SUPERVISED_FULL.md)。不得 Manual Reset 或新任务替代原任务。

原只读诊断工具中断续行：正式 ResumeApprovedTask/APPROVED_VALIDATION_RESUME 绑定人类原话、独立环境证据及 SHA；原失败和预算保留。完整边界见 protocol/SUPERVISED_FULL.md“2026-10-06 原只读诊断补验续行”。
