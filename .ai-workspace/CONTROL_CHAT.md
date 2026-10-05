# 控制任务约定 · 核心入口

当前模式、scope、任务状态和错误/恢复次数以生产队列为准，不从旧时间线推断许可。完整的逐次控制记录和证据链接在[保全快照](outputs/PRE-5b/AI-000200/history/CONTROL_CHAT_before_slim.md)；其中相对链接以原 .ai-workspace 位置为基准。

2026-10-05 已核：正式 DATA-R1 / AI194 SUCCEEDED/VERIFIED，相关 scope 已完成，原 ai PAUSED；旧 AI170 NEEDS_USER 七失败六恢复及 171/172 停止线保留。P2 场景/玩法与 Excel 显示待人工验收；生产配表和发布结果不可由本工具任务再写。新的 PRE-5b 工具不表示 WATCH 宿主已部署或桌面事件唤醒已验证。

监管当前入口：[完整协议](protocol/SUPERVISED_FULL.md)、[WORKFLOW](WORKFLOW.md)、[计划任务映射](PIPELINES.md)、[TK-WF-04](toolkit/WF.md#tk-wf-04)。用量周报规则在 DevTools/Workflow/USAGE_REPORTING.md。接力前核实际任务卡、mode-state、queue-state、worker-state、依赖、scope、锁和真实路由；失败、恢复及人工待验历史只通过正式 API 管理，不直接清空或覆盖。