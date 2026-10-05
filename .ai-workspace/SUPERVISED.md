# 监管模式（SUPERVISED）· 核心入口

当前实际 mode-state 是权威；本入口不授权自动恢复。2026-10-05 DATA-R1/AI194 已收口，原监管 ai PAUSED，旧AI170 NEEDS_USER 的七失败/六恢复、171/172冻结，P2与Excel人工待验均保留。此前逐次派发、恢复、错误与人工结论全文在[本任务保全快照](outputs/PRE-5b/AI-000200/history/SUPERVISED_before_slim.md)，相对链接以原 .ai-workspace 位置为基准。

监管执行入口为[完整协议](protocol/SUPERVISED_FULL.md)；每批写入前 SupervisionCheckpoint，Complete/Fail/Yield 的 supervisorHandoff 只回控制台。PRE-5b 的 light/full、根修复预算与无模型 WATCH 在完整协议及[TK-WF-04](toolkit/WF.md#tk-wf-04)；WATCH 宿主和真实唤醒回执待控制台部署，不恢复旧 ai。
